using Arora.Workflow.Domain.Aggregates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Arora.Workflow.EntityFramework.Interceptors;

/// <summary>
/// Intercepts EF Core save operations to extract and dispatch domain events
/// via MediatR before committing the transaction.
/// </summary>
public sealed class DomainEventDispatcherInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatcherInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken cancellationToken)
    {
        // Find all WorkflowInstance entities tracked by the DbContext
        var workflowInstances = context.ChangeTracker
            .Entries<WorkflowInstance>()
            .Where(x => x.Entity.DomainEvents.Any())
            .Select(x => x.Entity)
            .ToList();

        // Extract events and clear them from the aggregate
        var domainEvents = workflowInstances
            .SelectMany(x => x.DomainEvents)
            .ToList();

        foreach (var instance in workflowInstances)
        {
            instance.ClearDomainEvents();
        }

        // Dispatch all events
        foreach (var domainEvent in domainEvents)
        {
            await Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IPublisher>(_serviceProvider).Publish(domainEvent, cancellationToken);
        }
    }
}
