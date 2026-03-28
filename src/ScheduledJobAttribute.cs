namespace Philiprehberger.Scheduler;

/// <summary>
/// Marks a class as a scheduled job and specifies its cron schedule.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ScheduledJobAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledJobAttribute"/> class.
    /// </summary>
    /// <param name="name">A unique name identifying this job.</param>
    /// <param name="cronExpression">A standard 5-field cron expression (minute hour day month weekday).</param>
    public ScheduledJobAttribute(string name, string cronExpression)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        CronExpression = cronExpression ?? throw new ArgumentNullException(nameof(cronExpression));
    }

    /// <summary>
    /// Gets the unique name identifying this job.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the cron expression that defines when this job runs.
    /// </summary>
    public string CronExpression { get; }

    /// <summary>
    /// Gets or sets a value indicating whether overlapping executions should be prevented.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool PreventOverlap { get; set; } = true;

    /// <summary>
    /// Gets or sets the IANA timezone identifier for cron evaluation.
    /// When set, the cron expression is evaluated in the specified timezone instead of UTC.
    /// Defaults to <c>null</c> (UTC).
    /// </summary>
    /// <example>"America/New_York", "Europe/London", "Asia/Tokyo"</example>
    public string? TimeZone { get; set; }
}
