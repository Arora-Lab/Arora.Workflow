namespace Arora.Workflow.Domain.ValueObjects;

/// <summary>
/// Controls how the delay between retry attempts grows over time.
/// </summary>
public enum BackoffStrategy
{
    /// <summary>
    /// Every retry waits the same duration as <c>InitialDelay</c>.
    /// Use for fast, predictable retries on transient errors.
    /// Example (5s delay): 5s → 5s → 5s
    /// </summary>
    Fixed,

    /// <summary>
    /// Each retry waits longer by one additional <c>InitialDelay</c> unit.
    /// Use when you want gradual backpressure.
    /// Example (5s delay): 5s → 10s → 15s
    /// </summary>
    Linear,

    /// <summary>
    /// Each retry doubles the previous delay.
    /// Use for external API calls where you want to avoid overwhelming a recovering service.
    /// Example (5s delay): 5s → 10s → 20s → 40s
    /// </summary>
    Exponential
}
