using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Philiprehberger.Scheduler;

/// <summary>
/// Extension methods for registering the scheduler with dependency injection.
/// </summary>
public static class SchedulerServiceCollectionExtensions
{
    /// <summary>
    /// Adds the cron scheduler and registers jobs via the configuration delegate.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configure">A delegate to configure scheduled jobs.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddScheduler(
        this IServiceCollection services,
        Action<SchedulerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new SchedulerOptions();
        configure(options);

        foreach (var registration in options.Registrations)
        {
            services.AddTransient(registration.JobType);
        }

        services.AddSingleton(options);
        services.AddSingleton<InMemoryJobHistory>();
        services.AddSingleton<IJobHistory>(sp => sp.GetRequiredService<InMemoryJobHistory>());
        services.AddSingleton<CronScheduler>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<CronScheduler>());

        return services;
    }
}

/// <summary>
/// Configuration options for the scheduler, used to register jobs, one-time tasks, and lifecycle callbacks.
/// </summary>
public sealed class SchedulerOptions
{
    private readonly List<JobRegistration> _registrations = [];
    private readonly List<OneTimeJobRegistration> _oneTimeJobs = [];

    /// <summary>
    /// Gets the list of registered recurring jobs.
    /// </summary>
    internal IReadOnlyList<JobRegistration> Registrations => _registrations;

    /// <summary>
    /// Gets the list of registered one-time jobs.
    /// </summary>
    internal IReadOnlyList<OneTimeJobRegistration> OneTimeJobs => _oneTimeJobs;

    /// <summary>
    /// Gets or sets a callback invoked when a job starts executing.
    /// The parameter is the job name.
    /// </summary>
    public Action<string>? OnJobStarted { get; set; }

    /// <summary>
    /// Gets or sets a callback invoked when a job completes successfully.
    /// The parameters are the job name and the execution duration.
    /// </summary>
    public Action<string, TimeSpan>? OnJobCompleted { get; set; }

    /// <summary>
    /// Gets or sets a callback invoked when a job fails with an exception.
    /// The parameters are the job name and the exception that occurred.
    /// </summary>
    public Action<string, Exception>? OnJobFailed { get; set; }

    /// <summary>
    /// Registers a job to run on the specified cron schedule.
    /// </summary>
    /// <typeparam name="TJob">The job type implementing <see cref="IScheduledJob"/>.</typeparam>
    /// <param name="cronExpression">A standard 5-field cron expression (minute hour day month weekday).</param>
    /// <returns>The options instance for chaining.</returns>
    public SchedulerOptions AddJob<TJob>(string cronExpression) where TJob : class, IScheduledJob
    {
        return AddJob<TJob>(cronExpression, timeZone: null);
    }

    /// <summary>
    /// Registers a job to run on the specified cron schedule in the given timezone.
    /// </summary>
    /// <typeparam name="TJob">The job type implementing <see cref="IScheduledJob"/>.</typeparam>
    /// <param name="cronExpression">A standard 5-field cron expression (minute hour day month weekday).</param>
    /// <param name="timeZone">An IANA timezone identifier (e.g. "America/New_York"). Pass <c>null</c> for UTC.</param>
    /// <returns>The options instance for chaining.</returns>
    /// <exception cref="TimeZoneNotFoundException">Thrown when the specified timezone is not found.</exception>
    public SchedulerOptions AddJob<TJob>(string cronExpression, string? timeZone) where TJob : class, IScheduledJob
    {
        var cron = CronExpression.Parse(cronExpression);
        var jobType = typeof(TJob);

        var attr = (ScheduledJobAttribute?)Attribute.GetCustomAttribute(jobType, typeof(ScheduledJobAttribute));
        var name = attr?.Name ?? jobType.Name;
        var preventOverlap = attr?.PreventOverlap ?? true;
        var tz = timeZone ?? attr?.TimeZone;

        TimeZoneInfo? tzInfo = null;
        if (tz is not null)
        {
            tzInfo = TimeZoneInfo.FindSystemTimeZoneById(tz);
        }

        _registrations.Add(new JobRegistration(name, jobType, cron, preventOverlap, tzInfo));
        return this;
    }

    /// <summary>
    /// Schedules a one-time job to execute at the specified time and then auto-remove from the schedule.
    /// </summary>
    /// <param name="name">A unique name identifying this one-time job.</param>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <param name="runAt">The time at which to execute the job.</param>
    /// <returns>The options instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> or <paramref name="action"/> is null.</exception>
    public SchedulerOptions ScheduleOnce(string name, Func<CancellationToken, Task> action, DateTimeOffset runAt)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(action);

        _oneTimeJobs.Add(new OneTimeJobRegistration(name, action, runAt));
        return this;
    }
}

/// <summary>
/// Represents a registered scheduled job with its cron schedule and configuration.
/// </summary>
internal sealed record JobRegistration(
    string Name,
    Type JobType,
    CronExpression Cron,
    bool PreventOverlap,
    TimeZoneInfo? TimeZone);

/// <summary>
/// Represents a one-time scheduled job that executes once at a specified time and is then removed.
/// </summary>
internal sealed record OneTimeJobRegistration(
    string Name,
    Func<CancellationToken, Task> Action,
    DateTimeOffset RunAt)
{
    /// <summary>
    /// Gets or sets whether this one-time job has been executed.
    /// </summary>
    internal bool Executed { get; set; }
}
