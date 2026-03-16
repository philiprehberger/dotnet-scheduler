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
        services.AddSingleton<CronScheduler>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<CronScheduler>());

        return services;
    }
}

/// <summary>
/// Configuration options for the scheduler, used to register jobs and their schedules.
/// </summary>
public sealed class SchedulerOptions
{
    private readonly List<JobRegistration> _registrations = [];

    /// <summary>
    /// Gets the list of registered jobs.
    /// </summary>
    internal IReadOnlyList<JobRegistration> Registrations => _registrations;

    /// <summary>
    /// Registers a job to run on the specified cron schedule.
    /// </summary>
    /// <typeparam name="TJob">The job type implementing <see cref="IScheduledJob"/>.</typeparam>
    /// <param name="cronExpression">A standard 5-field cron expression (minute hour day month weekday).</param>
    /// <returns>The options instance for chaining.</returns>
    public SchedulerOptions AddJob<TJob>(string cronExpression) where TJob : class, IScheduledJob
    {
        var cron = CronExpression.Parse(cronExpression);
        var jobType = typeof(TJob);

        var attr = (ScheduledJobAttribute?)Attribute.GetCustomAttribute(jobType, typeof(ScheduledJobAttribute));
        var name = attr?.Name ?? jobType.Name;
        var preventOverlap = attr?.PreventOverlap ?? true;

        _registrations.Add(new JobRegistration(name, jobType, cron, preventOverlap));
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
    bool PreventOverlap);
