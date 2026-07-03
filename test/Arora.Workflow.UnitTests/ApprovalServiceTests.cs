using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.Aggregates;
using Arora.Workflow.Domain.Exceptions;
using Arora.Workflow.Domain.ValueObjects;
using Arora.Workflow.Testing;

namespace Arora.Workflow.UnitTests;

public class ApprovalServiceTests
{
    [Fact]
    public async Task ApproveAsync_WhenApprovalIsPending_RecordsApprovalAndAdvancesEngine()
    {
        // Arrange
        var host = new TestWorkflowHost();
        var defId = Guid.NewGuid();
        var def = Arora.Workflow.Domain.Aggregates.WorkflowDefinition.Create(
            host.TenantContext.TenantId,
            "test-workflow",
            1,
            null,
            "{ \"Nodes\": { \"PendingApproval\": { \"Type\": \"Approval\" } } }",
            "sys",
            host.Clock.UtcNow);
        typeof(Arora.Workflow.Domain.Aggregates.WorkflowDefinition).GetProperty("Id")!.SetValue(def, defId);
        await host.DefinitionRepository.AddAsync(def);

        var instanceId = Guid.NewGuid();
        var initialState = new WorkflowState("PendingApproval", WorkflowStateType.PendingApproval);
        var instance = Arora.Workflow.Domain.Aggregates.WorkflowInstance.Start(
            host.TenantContext.TenantId,
            defId, // definitionId
            1,
            "test-workflow",
            "corr-123",
            "idemp-123",
            initialState,
            "{ \"Nodes\": { \"PendingApproval\": { \"Type\": \"Approval\" } } }",
            new ActorInfo("user-1", "User"),
            host.Clock.UtcNow);
        
        // Use reflection to set the ID so it matches the approval
        typeof(Arora.Workflow.Domain.Aggregates.WorkflowInstance).GetProperty("Id")!.SetValue(instance, instanceId);
        
        await host.InstanceRepository.AddAsync(instance);

        var approval = Approval.Create(
            host.TenantContext.TenantId,
            instanceId,
            "test-workflow",
            "corr-123",
            "manager-approval",
            new ActorInfo("manager", "Manager"),
            host.Clock.UtcNow);

        await host.ApprovalRepository.AddAsync(approval);

        // Act
        var startSaveCount = host.UnitOfWork.SaveCount;
        await host.ApprovalService.ApproveAsync(
            approval.Id,
            new ActorInfo("manager", "Manager"),
            "LGTM");
        
        // Assert
        Assert.Equal(startSaveCount + 1, host.UnitOfWork.SaveCount);
        
        var updatedApproval = await host.ApprovalRepository.GetByIdAsync(approval.Id);
        Assert.NotNull(updatedApproval);
        Assert.Equal(ApprovalStatus.Approved, updatedApproval.Status);
        Assert.Equal("LGTM", updatedApproval.Comment);
    }

    [Fact]
    public async Task RejectAsync_WhenApprovalIsPending_RecordsRejectionAndAdvancesEngine()
    {
        // Arrange
        var host = new TestWorkflowHost();
        var defId = Guid.NewGuid();
        var def = Arora.Workflow.Domain.Aggregates.WorkflowDefinition.Create(
            host.TenantContext.TenantId,
            "test-workflow",
            1,
            null,
            "{ \"Nodes\": { \"PendingApproval\": { \"Type\": \"Approval\" } } }",
            "sys",
            host.Clock.UtcNow);
        typeof(Arora.Workflow.Domain.Aggregates.WorkflowDefinition).GetProperty("Id")!.SetValue(def, defId);
        await host.DefinitionRepository.AddAsync(def);

        var instanceId = Guid.NewGuid();
        var initialState = new WorkflowState("PendingApproval", WorkflowStateType.PendingApproval);
        var instance = Arora.Workflow.Domain.Aggregates.WorkflowInstance.Start(
            host.TenantContext.TenantId,
            defId,
            1,
            "test-workflow",
            "corr-123",
            "idemp-123",
            initialState,
            "{ \"Nodes\": { \"PendingApproval\": { \"Type\": \"Approval\" } } }",
            new ActorInfo("user-1", "User"),
            host.Clock.UtcNow);
        
        typeof(Arora.Workflow.Domain.Aggregates.WorkflowInstance).GetProperty("Id")!.SetValue(instance, instanceId);

        await host.InstanceRepository.AddAsync(instance);

        var approval = Approval.Create(
            host.TenantContext.TenantId,
            instanceId,
            "test-workflow",
            "corr-123",
            "manager-approval",
            new ActorInfo("manager", "Manager"),
            host.Clock.UtcNow);

        await host.ApprovalRepository.AddAsync(approval);

        // Act
        var startSaveCount = host.UnitOfWork.SaveCount;
        await host.ApprovalService.RejectAsync(
            approval.Id,
            new ActorInfo("manager", "Manager"),
            "Needs work");
        
        // Assert
        Assert.Equal(startSaveCount + 1, host.UnitOfWork.SaveCount);
        
        var updatedApproval = await host.ApprovalRepository.GetByIdAsync(approval.Id);
        Assert.NotNull(updatedApproval);
        Assert.Equal(ApprovalStatus.Rejected, updatedApproval.Status);
        Assert.Equal("Needs work", updatedApproval.Comment);
    }

    [Fact]
    public async Task ApproveAsync_WhenApprovalAlreadyDecided_ThrowsDuplicateApprovalException()
    {
        // Arrange
        var host = new TestWorkflowHost();
        var defId = Guid.NewGuid();
        var def = Arora.Workflow.Domain.Aggregates.WorkflowDefinition.Create(
            host.TenantContext.TenantId,
            "test-workflow",
            1,
            null,
            "{ \"Nodes\": { \"PendingApproval\": { \"Type\": \"Approval\" } } }",
            "sys",
            host.Clock.UtcNow);
        typeof(Arora.Workflow.Domain.Aggregates.WorkflowDefinition).GetProperty("Id")!.SetValue(def, defId);
        await host.DefinitionRepository.AddAsync(def);

        var instanceId = Guid.NewGuid();
        var initialState = new WorkflowState("PendingApproval", WorkflowStateType.PendingApproval);
        var instance = Arora.Workflow.Domain.Aggregates.WorkflowInstance.Start(
            host.TenantContext.TenantId,
            defId,
            1,
            "test-workflow",
            "corr-123",
            "idemp-123",
            initialState,
            "{ \"Nodes\": { \"PendingApproval\": { \"Type\": \"Approval\" } } }",
            new ActorInfo("user-1", "User"),
            host.Clock.UtcNow);
        
        typeof(Arora.Workflow.Domain.Aggregates.WorkflowInstance).GetProperty("Id")!.SetValue(instance, instanceId);

        await host.InstanceRepository.AddAsync(instance);

        var approval = Approval.Create(
            host.TenantContext.TenantId,
            instanceId,
            "test-workflow",
            "corr-123",
            "manager-approval",
            new ActorInfo("manager", "Manager"),
            host.Clock.UtcNow);
        approval.Approve(new ActorInfo("manager", "Manager"), "Done", host.Clock.UtcNow);

        await host.ApprovalRepository.AddAsync(approval);

        // Act & Assert
        await Assert.ThrowsAsync<DuplicateApprovalException>(() => 
            host.ApprovalService.ApproveAsync(approval.Id, new ActorInfo("manager", "Manager")));
    }
}
