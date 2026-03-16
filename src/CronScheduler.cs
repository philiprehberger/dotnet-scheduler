using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Philiprehberger.Scheduler;

/// <summary>
/// A hosted service that runs registered jobs on their cron schedules.
/// Supports overlap prevention and graceful shutdown.
/// </summary>
public sealed class CronScheduler : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SchedulerOptions _options;
    private readonly ConcurrentDictionary<string, bool> _runningJobs = new();
    private CancellationTokenSource? _cts;
    private Task? _executingTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="CronScheduler"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve job instances.</param>
    /// <param name="options">The scheduler options containing registered jobs.</param>
    public CronScheduler(IServiceProvider serviceProvider, SchedulerOptions options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
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

            foreach (var registration in _options.Registrations)
            {
                if (ct.IsCancellationRequested)
                    break;

                if (!registration.Cron.Matches(checkTime))
                    continue;

                var jobKey = registration.Name;

                if (registration.PreventOverlap && !_runningJobs.TryAdd(jobKey, true))
                    continue;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var job = (IScheduledJob)scope.ServiceProvider.GetRequiredService(registration.JobType);
                        await job.ExecuteAsync(ct);
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
        }
    }
}
