using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.Entities;
using Arora.Workflow.Domain.Events;
using MediatR;

namespace Arora.Workflow.Application.Handlers.History;

public class WorkflowStartedHistoryHandler : INotificationHandler<WorkflowStarted>
{
    private readonly IWorkflowHistoryRepository _repository;

    public WorkflowStartedHistoryHandler(IWorkflowHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(WorkflowStarted notification, CancellationToken cancellationToken)
    {
        var history = WorkflowHistory.Create(
            notification.WorkflowInstanceId,
            "Started",
            notification.OccurredAt,
            notification.InitiatedBy,
            new { notification.WorkflowName, notification.CorrelationId });

        await _repository.AddAsync(history, cancellationToken);
    }
}

public class WorkflowTransitionedHistoryHandler : INotificationHandler<WorkflowTransitioned>
{
    private readonly IWorkflowHistoryRepository _repository;

    public WorkflowTransitionedHistoryHandler(IWorkflowHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(WorkflowTransitioned notification, CancellationToken cancellationToken)
    {
        var history = WorkflowHistory.Create(
            notification.WorkflowInstanceId,
            "Transitioned",
            notification.OccurredAt,
            notification.Actor,
            new { notification.FromState, notification.ToState, notification.StepName });

        await _repository.AddAsync(history, cancellationToken);
    }
}

public class WorkflowCancelledHistoryHandler : INotificationHandler<WorkflowCancelled>
{
    private readonly IWorkflowHistoryRepository _repository;

    public WorkflowCancelledHistoryHandler(IWorkflowHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(WorkflowCancelled notification, CancellationToken cancellationToken)
    {
        var history = WorkflowHistory.Create(
            notification.WorkflowInstanceId,
            "Cancelled",
            notification.OccurredAt,
            notification.CancelledBy,
            new { notification.Reason });

        await _repository.AddAsync(history, cancellationToken);
    }
}
