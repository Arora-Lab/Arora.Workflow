using Arora.Workflow.Application.Interfaces;

namespace Arora.Workflow.Internal.Engine;

/// <summary>
/// The production implementation of <see cref="IWorkflowClock"/>.
/// Returns <c>DateTimeOffset.UtcNow</c>.
/// </summary>
internal sealed class SystemClock : IWorkflowClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
