namespace Arora.Workflow.Domain.ValueObjects;

/// <summary>
/// Defines the retry behavior for a workflow step that fails during execution.
/// </summary>
/// <param name="MaxAttempts">
/// The maximum number of times the step will be executed, including the first attempt.
/// A value of 1 means no retries. Must be greater than or equal to 1.
/// </param>
/// <param name="InitialDelay">
/// The delay before the first retry. Subsequent delays are calculated based
/// on the <see cref="Backoff"/> strategy.
/// </param>
/// <param name="Backoff">
/// Controls how the delay grows between retry attempts.
/// </param>
public sealed record RetryPolicy(
    int MaxAttempts,
    TimeSpan InitialDelay,
    BackoffStrategy Backoff = BackoffStrategy.Fixed)
{
    /// <summary>The default retry policy: 3 attempts with 5-second fixed delays.</summary>
    public static readonly RetryPolicy Default = new(
        MaxAttempts: 3,
        InitialDelay: TimeSpan.FromSeconds(5),
        Backoff: BackoffStrategy.Fixed);

    /// <summary>A no-retry policy: execute once, fail immediately on error.</summary>
    public static readonly RetryPolicy None = new(
        MaxAttempts: 1,
        InitialDelay: TimeSpan.Zero,
        Backoff: BackoffStrategy.Fixed);

    /// <summary>
    /// Calculates the delay to wait before the specified attempt number.
    /// </summary>
    /// <param name="attemptNumber">
    /// The 1-based attempt number. Attempt 1 is the first retry (after the initial failure).
    /// </param>
    /// <returns>The delay duration before this attempt.</returns>
    public TimeSpan GetDelay(int attemptNumber)
    {
        if (attemptNumber <= 1) return InitialDelay;

        return Backoff switch
        {
            BackoffStrategy.Fixed       => InitialDelay,
            BackoffStrategy.Linear      => InitialDelay * attemptNumber,
            BackoffStrategy.Exponential => InitialDelay * Math.Pow(2, attemptNumber - 1),
            _                           => InitialDelay
        };
    }
}
