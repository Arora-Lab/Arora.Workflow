namespace Arora.Workflow.Application.Interfaces;

/// <summary>
/// An abstraction over the system clock that enables deterministic time
/// in unit tests without mocking static calls to <c>DateTimeOffset.UtcNow</c>.
/// </summary>
/// <remarks>
/// The default production implementation returns <c>DateTimeOffset.UtcNow</c>.
/// In tests, inject a <c>FakeClock</c> from <c>Arora.Workflow.Testing</c>
/// to control time precisely.
/// </remarks>
public interface IWorkflowClock
{
    /// <summary>Returns the current UTC time.</summary>
    DateTimeOffset UtcNow { get; }
}
