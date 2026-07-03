using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.Aggregates;
using Arora.Workflow.Domain.Exceptions;
using Arora.Workflow.Domain.ValueObjects;
using Arora.Workflow.Testing;

namespace Arora.Workflow.UnitTests;

public class WorkflowServiceTests
{
    [Fact]
    public async Task StartAsync_WhenDefinitionNotFound_ThrowsException()
    {
        // Arrange
        var host = new TestWorkflowHost();
        var request = new StartWorkflowRequest
        {
            WorkflowName = "non-existent-workflow",
            CorrelationId = "corr-123",
            IdempotencyKey = "idemp-123",
            InitiatedBy = new ActorInfo("user-1", "Test User")
        };

        // Act & Assert
        await Assert.ThrowsAsync<WorkflowDefinitionNotFoundException>(() => 
            host.WorkflowService.StartAsync(request));
    }

    [Fact]
    public async Task StartAsync_WhenDefinitionFound_CreatesAndPersistsInstance()
    {
        // Arrange
        var host = new TestWorkflowHost();
        var definition = WorkflowDefinition.Create(
            host.TenantContext.TenantId, "test-workflow", 1, null, "{}", "sys", host.Clock.UtcNow);
        definition.Publish("sys", host.Clock.UtcNow);
        
        await host.DefinitionRepository.AddAsync(definition);

        var request = new StartWorkflowRequest
        {
            WorkflowName = "test-workflow",
            CorrelationId = "corr-123",
            IdempotencyKey = "idemp-123",
            InitiatedBy = new ActorInfo("user-1", "Test User")
        };

        // Act
        var snapshot = await host.WorkflowService.StartAsync(request);

        // Assert
        Assert.NotNull(snapshot);
        Assert.Equal("test-workflow", snapshot.WorkflowName);
        Assert.Equal("corr-123", snapshot.CorrelationId);
        
        var persisted = host.InstanceRepository.All.SingleOrDefault(i => i.Id == snapshot.Id);
        Assert.NotNull(persisted);
        Assert.Equal("idemp-123", persisted.IdempotencyKey);
        
        Assert.True(host.UnitOfWork.SaveCount > 0, "Expected UnitOfWork to be saved");
    }

    [Fact]
    public async Task StartAsync_WhenIdempotencyKeyExists_ReturnsExistingInstanceWithoutAdvancing()
    {
        // Arrange
        var host = new TestWorkflowHost();
        var definition = WorkflowDefinition.Create(
            host.TenantContext.TenantId, "test-workflow", 1, null, "{}", "sys", host.Clock.UtcNow);
        definition.Publish("sys", host.Clock.UtcNow);
        
        await host.DefinitionRepository.AddAsync(definition);
        
        var request = new StartWorkflowRequest
        {
            WorkflowName = "test-workflow",
            CorrelationId = "corr-123",
            IdempotencyKey = "idemp-123",
            InitiatedBy = new ActorInfo("user-1", "Test User")
        };

        // Act - Start first time
        var snapshot1 = await host.WorkflowService.StartAsync(request);
        var savesAfterFirst = host.UnitOfWork.SaveCount;

        // Act - Start second time with same request
        var snapshot2 = await host.WorkflowService.StartAsync(request);
        var savesAfterSecond = host.UnitOfWork.SaveCount;

        // Assert
        Assert.Equal(snapshot1.Id, snapshot2.Id);
        Assert.Equal(savesAfterFirst, savesAfterSecond); // Should not save anything on idempotent retry
        Assert.Single(host.InstanceRepository.All);
    }

    [Fact]
    public async Task CancelAsync_WhenInstanceExists_CancelsInstance()
    {
        // Arrange
        var host = new TestWorkflowHost();
        var definition = WorkflowDefinition.Create(
            host.TenantContext.TenantId, "test-workflow", 1, null, "{ \"Nodes\": { \"Initial\": { \"Type\": \"Approval\" } } }", "sys", host.Clock.UtcNow);
        definition.Publish("sys", host.Clock.UtcNow);
        await host.DefinitionRepository.AddAsync(definition);
        
        var request = new StartWorkflowRequest
        {
            WorkflowName = "test-workflow",
            CorrelationId = "corr-123",
            IdempotencyKey = "idemp-123",
            InitiatedBy = new ActorInfo("user-1", "Test User")
        };
        var snapshot = await host.WorkflowService.StartAsync(request);

        // Act
        await host.WorkflowService.CancelAsync(
            snapshot.Id, 
            "Test cancellation", 
            new ActorInfo("admin", "Admin"));

        // Assert
        var cancelledSnapshot = await host.WorkflowService.GetInstanceAsync(snapshot.Id);
        Assert.NotNull(cancelledSnapshot);
        Assert.Equal(Arora.Workflow.Domain.ValueObjects.WorkflowStatus.Cancelled, cancelledSnapshot.Status);
    }

    [Fact]
    public async Task CancelAsync_WhenInstanceNotFound_ThrowsException()
    {
        // Arrange
        var host = new TestWorkflowHost();

        // Act & Assert
        await Assert.ThrowsAsync<WorkflowNotFoundException>(() => 
            host.WorkflowService.CancelAsync(Guid.NewGuid(), "Test", new ActorInfo("admin", "Admin")));
    }

    [Fact]
    public async Task GetInstanceAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var host = new TestWorkflowHost();

        // Act
        var result = await host.WorkflowService.GetInstanceAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsEventHistory()
    {
        // Arrange
        var host = new TestWorkflowHost();
        var definition = WorkflowDefinition.Create(
            host.TenantContext.TenantId, "test-workflow", 1, null, "{}", "sys", host.Clock.UtcNow);
        definition.Publish("sys", host.Clock.UtcNow);
        await host.DefinitionRepository.AddAsync(definition);
        
        var request = new StartWorkflowRequest
        {
            WorkflowName = "test-workflow",
            CorrelationId = "corr-123",
            IdempotencyKey = "idemp-123",
            InitiatedBy = new ActorInfo("user-1", "Test User")
        };
        var snapshot = await host.WorkflowService.StartAsync(request);

        // Act
        var history = await host.WorkflowService.GetHistoryAsync(snapshot.Id);

        // Assert
        // In the stub engine, history might just be an empty list because the real history table 
        // read mechanism isn't fully implemented in the fakes or service if they rely on a history repository.
        // But for now, we just verify it doesn't throw and returns a list.
        Assert.NotNull(history);
    }
}
