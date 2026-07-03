using Arora.Workflow.Application.Interfaces;

namespace Arora.Workflow.Testing;

/// <summary>
/// A controllable <see cref="IWorkflowClock"/> for use in unit tests.
/// Time does not advance automatically — you set it explicitly.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// var clock = new FakeClock(new DateTimeOffset(2024, 1, 15, 9, 0, 0, TimeSpan.Zero));
///
/// // Advance time before the next assertion
/// clock.Advance(TimeSpan.FromDays(3));
/// </code>
/// </remarks>
public sealed class FakeClock : IWorkflowClock
{
    private DateTimeOffset _current;

    /// <summary>
    /// Initializes the clock to the specified time.
    /// </summary>
    /// <param name="startTime">The initial UTC time.</param>
    public FakeClock(DateTimeOffset startTime)
    {
        _current = startTime;
    }

    /// <summary>
    /// Initializes the clock to a known, fixed default time:
    /// <c>2024-01-15 09:00:00 UTC</c>.
    /// </summary>
    public FakeClock() : this(new DateTimeOffset(2024, 1, 15, 9, 0, 0, TimeSpan.Zero)) { }

    /// <inheritdoc />
    public DateTimeOffset UtcNow => _current;

    /// <summary>
    /// Advances the clock by the specified duration.
    /// </summary>
    /// <param name="duration">How far to advance time.</param>
    public void Advance(TimeSpan duration) => _current += duration;

    /// <summary>
    /// Sets the clock to a specific point in time.
    /// </summary>
    /// <param name="time">The new UTC time.</param>
    public void SetTo(DateTimeOffset time) => _current = time;
}
