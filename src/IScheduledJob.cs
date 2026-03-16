namespace Philiprehberger.Scheduler;

/// <summary>
/// Defines a job that can be scheduled for recurring execution.
/// </summary>
public interface IScheduledJob
{
    /// <summary>
    /// Executes the job asynchronously.
    /// </summary>
    /// <param name="ct">A cancellation token to signal that execution should stop.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync(CancellationToken ct);
}
