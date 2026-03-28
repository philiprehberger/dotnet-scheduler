namespace Philiprehberger.Scheduler;

/// <summary>
/// Provides access to job execution history records.
/// </summary>
public interface IJobHistory
{
    /// <summary>
    /// Gets the execution history for a specific job, ordered by most recent first.
    /// </summary>
    /// <param name="jobName">The name of the job to retrieve history for.</param>
    /// <returns>A read-only list of execution records for the specified job.</returns>
    IReadOnlyList<JobExecutionRecord> GetHistory(string jobName);

    /// <summary>
    /// Gets all execution records across all jobs, ordered by most recent first.
    /// </summary>
    /// <returns>A read-only list of all execution records.</returns>
    IReadOnlyList<JobExecutionRecord> GetAll();

    /// <summary>
    /// Records a job execution.
    /// </summary>
    /// <param name="record">The execution record to store.</param>
    void Record(JobExecutionRecord record);
}
