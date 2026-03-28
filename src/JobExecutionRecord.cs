namespace Philiprehberger.Scheduler;

/// <summary>
/// Represents a single execution of a scheduled job, including timing and outcome information.
/// </summary>
/// <param name="JobName">The name of the job that was executed.</param>
/// <param name="StartTime">The UTC time when the job execution started.</param>
/// <param name="Duration">The elapsed time of the job execution.</param>
/// <param name="Success">Whether the job completed successfully without exceptions.</param>
/// <param name="ErrorMessage">The error message if the job failed, or <c>null</c> if it succeeded.</param>
public sealed record JobExecutionRecord(
    string JobName,
    DateTimeOffset StartTime,
    TimeSpan Duration,
    bool Success,
    string? ErrorMessage);
