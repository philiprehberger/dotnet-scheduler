using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Philiprehberger.Scheduler;

/// <summary>
/// A hosted service that runs registered jobs on their cron schedules.
/// Supports overlap prevention, timezone-aware scheduling, one-time jobs,
/// job execution history, event callbacks, and graceful shutdown.
/// </summary>
public sealed class CronScheduler : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SchedulerOptions _options;
    private readonly IJobHistory _jobHistory;
    private readonly ConcurrentDictionary<string, bool> _runningJobs = new();
    private CancellationTokenSource? _cts;
    private Task? _executingTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="CronScheduler"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve job instances.</param>
    /// <param name="options">The scheduler options containing registered jobs.</param>
    /// <param name="jobHistory">The job history tracker for recording execution results.</param>
    public CronScheduler(IServiceProvider serviceProvider, SchedulerOptions options, IJobHistory jobHistory)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _jobHistory = jobHistory ?? throw new ArgumentNullException(nameof(jobHistory));
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = RunSchedulerLoopAsync(_cts.Token);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
        }

        if (_executingTask is not null)
        {
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private async Task RunSchedulerLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var nextMinute = new DateTimeOffset(
                now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, TimeSpan.Zero)
                .AddMinutes(1);

            var delay = nextMinute - now;
            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            var checkTime = DateTimeOffset.UtcNow;

            // Process recurring cron jobs
            foreach (var registration in _options.Registrations)
            {
                if (ct.IsCancellationRequested)
                    break;

                var evalTime = ConvertToTimeZone(checkTime, registration.TimeZone);

                if (!registration.Cron.Matches(evalTime))
                    continue;

                var jobKey = registration.Name;

                if (registration.PreventOverlap && !_runningJobs.TryAdd(jobKey, true))
                    continue;

                _ = Task.Run(async () =>
                {
                    var sw = Stopwatch.StartNew();
                    _options.OnJobStarted?.Invoke(jobKey);

                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var job = (IScheduledJob)scope.ServiceProvider.GetRequiredService(registration.JobType);
                        await job.ExecuteAsync(ct);

                        sw.Stop();
                        _options.OnJobCompleted?.Invoke(jobKey, sw.Elapsed);
                        _jobHistory.Record(new JobExecutionRecord(jobKey, checkTime, sw.Elapsed, true, null));
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        sw.Stop();
                        _options.OnJobFailed?.Invoke(jobKey, ex);
                        _jobHistory.Record(new JobExecutionRecord(jobKey, checkTime, sw.Elapsed, false, ex.Message));
                    }
                    finally
                    {
                        if (registration.PreventOverlap)
                        {
                            _runningJobs.TryRemove(jobKey, out _);
                        }
                    }
                }, ct);
            }

            // Process one-time jobs
            foreach (var oneTime in _options.OneTimeJobs)
            {
                if (ct.IsCancellationRequested)
                    break;

                if (oneTime.Executed)
                    continue;

                if (checkTime >= oneTime.RunAt)
                {
                    oneTime.Executed = true;

                    _ = Task.Run(async () =>
                    {
                        var sw = Stopwatch.StartNew();
                        _options.OnJobStarted?.Invoke(oneTime.Name);

                        try
                        {
                            await oneTime.Action(ct);

                            sw.Stop();
                            _options.OnJobCompleted?.Invoke(oneTime.Name, sw.Elapsed);
                            _jobHistory.Record(new JobExecutionRecord(oneTime.Name, checkTime, sw.Elapsed, true, null));
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            sw.Stop();
                            _options.OnJobFailed?.Invoke(oneTime.Name, ex);
                            _jobHistory.Record(new JobExecutionRecord(oneTime.Name, checkTime, sw.Elapsed, false, ex.Message));
                        }
                    }, ct);
                }
            }
        }
    }

    /// <summary>
    /// Converts a UTC time to the specified timezone for cron evaluation.
    /// Returns the original time if no timezone is specified.
    /// </summary>
    private static DateTimeOffset ConvertToTimeZone(DateTimeOffset utcTime, TimeZoneInfo? timeZone)
    {
        if (timeZone is null)
        {
            return utcTime;
        }

        return TimeZoneInfo.ConvertTime(utcTime, timeZone);
    }
}
