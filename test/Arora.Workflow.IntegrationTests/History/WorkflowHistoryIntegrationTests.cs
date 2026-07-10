using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.Aggregates;
using Arora.Workflow.Domain.Entities;
using Arora.Workflow.Domain.ValueObjects;
using Arora.Workflow.EntityFramework.Context;
using Arora.Workflow.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arora.Workflow.IntegrationTests.History;

public class WorkflowHistoryIntegrationTests : IAsyncLifetime
{
    private class TestDbContext : DbContext
    {
        public DbSet<WorkflowInstance> Instances { get; set; } = null!;
        public DbSet<Arora.Workflow.EntityFramework.Entities.WorkflowHistoryEntity> Histories { get; set; } = null!;

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyAroraWorkflowMappings();
            base.OnModelCreating(modelBuilder);
        }
    }

    private ServiceProvider _serviceProvider = null!;
    private string _dbName = null!;

    public Task InitializeAsync()
    {
        var services = new ServiceCollection();

        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AroraWorkflowBuilder).Assembly));

        // Arora Workflow EF Core extensions
        var builder = new AroraWorkflowBuilder(services);
        builder.UseEntityFramework<TestDbContext>();

        // DbContext
        _dbName = Guid.NewGuid().ToString();
        services.AddDbContext<TestDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase(_dbName);
        });

        _serviceProvider = services.BuildServiceProvider();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return _serviceProvider.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task SaveChangesAsync_ProjectsDomainEventsToHistory_Atomically()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var historyRepo = scope.ServiceProvider.GetRequiredService<IWorkflowHistoryRepository>();
        var instanceRepo = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceRepository>();

        var instance = WorkflowInstance.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "HistoryTest",
            "CORR-01",
            "IDEMP-01",
            new WorkflowState("Initial", WorkflowStateType.Initial),
            null,
            new ActorInfo("tester", "Tester"),
            DateTimeOffset.UtcNow);

        // Act
        await instanceRepo.AddAsync(instance);
        
        // This should project the events to WorkflowHistoryEntity inline without clearing them
        await uow.SaveChangesAsync(); 

        // Assert
        var histories = await historyRepo.GetByInstanceIdAsync(instance.Id);
        Assert.Single(histories);
        var record = histories[0];
        Assert.Equal("Started", record.Action);
        Assert.Equal("tester", record.Actor?.Id);

        // Verify the domain event was NOT cleared (so WorkflowService can publish it later)
        Assert.Single(instance.DomainEvents);
    }

    [Fact]
    public async Task SaveChangesAsync_MultipleCalls_DoesNotCreateDuplicateHistory()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var historyRepo = scope.ServiceProvider.GetRequiredService<IWorkflowHistoryRepository>();
        var instanceRepo = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceRepository>();

        var instance = WorkflowInstance.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "HistoryTest",
            "CORR-01",
            "IDEMP-01",
            new WorkflowState("Initial", WorkflowStateType.Initial),
            null,
            new ActorInfo("tester", "Tester"),
            DateTimeOffset.UtcNow);

        await instanceRepo.AddAsync(instance);
        
        // Act - Simulate an EF Core Execution Strategy retry
        await uow.SaveChangesAsync(); 
        await uow.SaveChangesAsync(); // Called again, events are still on the aggregate!

        // Assert - Should only have ONE history record because it deduplicates by OccurredAt
        var histories = await historyRepo.GetByInstanceIdAsync(instance.Id);
        Assert.Single(histories);
    }

    [Fact]
    public async Task MultipleEvents_CreateExactlyOneHistoryRowEach()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var historyRepo = scope.ServiceProvider.GetRequiredService<IWorkflowHistoryRepository>();
        var instanceRepo = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceRepository>();

        var instance = WorkflowInstance.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "HistoryTest",
            "CORR-01",
            "IDEMP-01",
            new WorkflowState("Initial", WorkflowStateType.Initial),
            null,
            new ActorInfo("tester", "Tester"),
            DateTimeOffset.UtcNow);

        instance.TransitionTo(
            new WorkflowState("Pending", WorkflowStateType.PendingApproval),
            "Submitted",
            DateTimeOffset.UtcNow.AddMinutes(1),
            new ActorInfo("actor", "Actor"));

        await instanceRepo.AddAsync(instance);
        
        // Act
        await uow.SaveChangesAsync(); 

        // Assert
        var histories = await historyRepo.GetByInstanceIdAsync(instance.Id);
        Assert.Equal(2, histories.Count);
        
        // Assert Ordering
        Assert.Equal("Started", histories[0].Action);
        Assert.Equal("Transitioned", histories[1].Action);
    }
}
