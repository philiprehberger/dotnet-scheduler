using System.Collections.Concurrent;

namespace Philiprehberger.Scheduler;

/// <summary>
/// An in-memory implementation of <see cref="IJobHistory"/> that stores the last N execution records per job.
/// Thread-safe for concurrent reads and writes.
/// </summary>
public sealed class InMemoryJobHistory : IJobHistory
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<JobExecutionRecord>> _history = new();
    private readonly int _maxRecordsPerJob;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryJobHistory"/> class.
    /// </summary>
    /// <param name="maxRecordsPerJob">The maximum number of execution records to retain per job. Defaults to 100.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxRecordsPerJob"/> is less than 1.</exception>
    public InMemoryJobHistory(int maxRecordsPerJob = 100)
    {
        if (maxRecordsPerJob < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRecordsPerJob), "Must be at least 1.");
        }

        _maxRecordsPerJob = maxRecordsPerJob;
    }

    /// <inheritdoc />
    public IReadOnlyList<JobExecutionRecord> GetHistory(string jobName)
    {
        ArgumentNullException.ThrowIfNull(jobName);

        if (_history.TryGetValue(jobName, out var queue))
        {
            return queue.ToArray().OrderByDescending(r => r.StartTime).ToList();
        }

        return [];
    }

    /// <inheritdoc />
    public IReadOnlyList<JobExecutionRecord> GetAll()
    {
        return _history.Values
            .SelectMany(q => q)
            .OrderByDescending(r => r.StartTime)
            .ToList();
    }

    /// <inheritdoc />
    public void Record(JobExecutionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var queue = _history.GetOrAdd(record.JobName, _ => new ConcurrentQueue<JobExecutionRecord>());
        queue.Enqueue(record);

        while (queue.Count > _maxRecordsPerJob)
        {
            queue.TryDequeue(out _);
        }
    }
}
