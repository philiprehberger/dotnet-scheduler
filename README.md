# Philiprehberger.Scheduler

[![CI](https://github.com/philiprehberger/dotnet-scheduler/actions/workflows/ci.yml/badge.svg)](https://github.com/philiprehberger/dotnet-scheduler/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Philiprehberger.Scheduler.svg)](https://www.nuget.org/packages/Philiprehberger.Scheduler)
[![Last updated](https://img.shields.io/github/last-commit/philiprehberger/dotnet-scheduler)](https://github.com/philiprehberger/dotnet-scheduler/commits/main)

Lightweight in-process job scheduler with cron expressions, timezone support, execution history, and lifecycle callbacks.

## Installation

```bash
dotnet add package Philiprehberger.Scheduler
```

## Usage

### Define a Job

```csharp
using Philiprehberger.Scheduler;

public class CleanupJob : IScheduledJob
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        // Clean up old records
        await Task.CompletedTask;
    }
}
```

### Register with DI

```csharp
using Philiprehberger.Scheduler;

builder.Services.AddScheduler(options =>
{
    options.AddJob<CleanupJob>("*/5 * * * *"); // every 5 minutes
});
```

### Using Attributes

```csharp
using Philiprehberger.Scheduler;

[ScheduledJob("cleanup", "*/5 * * * *", TimeZone = "America/New_York")]
public class CleanupJob : IScheduledJob
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        // Runs every 5 minutes in Eastern time
        await Task.CompletedTask;
    }
}
```

### Timezone-Aware Scheduling

```csharp
using Philiprehberger.Scheduler;

builder.Services.AddScheduler(options =>
{
    // Evaluate cron in a specific timezone instead of UTC
    options.AddJob<ReportJob>("0 9 * * 1-5", timeZone: "Europe/London");
});
```

### One-Time Scheduled Jobs

```csharp
using Philiprehberger.Scheduler;

builder.Services.AddScheduler(options =>
{
    options.ScheduleOnce("send-welcome", async ct =>
    {
        // Runs once at the specified time, then auto-removes
        await SendWelcomeEmailAsync(ct);
    }, DateTimeOffset.UtcNow.AddHours(1));
});
```

### Job Execution History

```csharp
using Philiprehberger.Scheduler;

// Inject IJobHistory to query past executions
app.MapGet("/jobs/history", (IJobHistory history) =>
{
    var all = history.GetAll();
    var cleanup = history.GetHistory("cleanup");
    return Results.Ok(new { all, cleanup });
});
```

### Job Event Callbacks

```csharp
using Philiprehberger.Scheduler;

builder.Services.AddScheduler(options =>
{
    options.AddJob<CleanupJob>("*/5 * * * *");

    options.OnJobStarted = name =>
        Console.WriteLine($"Job started: {name}");

    options.OnJobCompleted = (name, duration) =>
        Console.WriteLine($"Job completed: {name} in {duration.TotalMilliseconds}ms");

    options.OnJobFailed = (name, ex) =>
        Console.WriteLine($"Job failed: {name} - {ex.Message}");
});
```

### Cron Expressions

```csharp
using Philiprehberger.Scheduler;

var cron = CronExpression.Parse("*/5 * * * *");
var next = cron.GetNextOccurrence(DateTimeOffset.UtcNow);
var matches = cron.Matches(DateTimeOffset.UtcNow);
```

## API

### `IScheduledJob`

| Method | Description |
|--------|-------------|
| `ExecuteAsync(CancellationToken)` | Execute the scheduled job |

### `CronExpression`

| Method | Description |
|--------|-------------|
| `Parse(string expression)` | Parse a 5-field cron expression |
| `GetNextOccurrence(DateTimeOffset)` | Get the next matching time |
| `Matches(DateTimeOffset)` | Check if a time matches |

### `ScheduledJobAttribute`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Name` | `string` | — | Job name |
| `CronExpression` | `string` | — | Cron schedule |
| `PreventOverlap` | `bool` | `true` | Skip if previous run is still executing |
| `TimeZone` | `string?` | `null` | IANA timezone ID for cron evaluation |

### `SchedulerOptions`

| Method | Description |
|--------|-------------|
| `AddJob<TJob>(string cronExpression)` | Register a recurring job with a cron schedule |
| `AddJob<TJob>(string cronExpression, string? timeZone)` | Register a recurring job with timezone |
| `ScheduleOnce(string name, Func<CancellationToken, Task> action, DateTimeOffset runAt)` | Schedule a one-time job |

| Property | Type | Description |
|----------|------|-------------|
| `OnJobStarted` | `Action<string>?` | Callback when a job starts |
| `OnJobCompleted` | `Action<string, TimeSpan>?` | Callback when a job completes |
| `OnJobFailed` | `Action<string, Exception>?` | Callback when a job fails |

### `IJobHistory`

| Method | Description |
|--------|-------------|
| `GetHistory(string jobName)` | Get execution records for a specific job |
| `GetAll()` | Get all execution records across all jobs |
| `Record(JobExecutionRecord record)` | Store an execution record |

### `JobExecutionRecord`

| Property | Type | Description |
|----------|------|-------------|
| `JobName` | `string` | Name of the executed job |
| `StartTime` | `DateTimeOffset` | When the execution started |
| `Duration` | `TimeSpan` | How long the execution took |
| `Success` | `bool` | Whether the job succeeded |
| `ErrorMessage` | `string?` | Error message if failed |

### `CronScheduler`

| Method | Description |
|--------|-------------|
| `StartAsync(CancellationToken)` | Start the scheduler |
| `StopAsync(CancellationToken)` | Stop gracefully |

## Development

```bash
dotnet build src/Philiprehberger.Scheduler.csproj --configuration Release
```

## Support

If you find this project useful:

⭐ [Star the repo](https://github.com/philiprehberger/dotnet-scheduler)

🐛 [Report issues](https://github.com/philiprehberger/dotnet-scheduler/issues?q=is%3Aissue+is%3Aopen+label%3Abug)

💡 [Suggest features](https://github.com/philiprehberger/dotnet-scheduler/issues?q=is%3Aissue+is%3Aopen+label%3Aenhancement)

❤️ [Sponsor development](https://github.com/sponsors/philiprehberger)

🌐 [All Open Source Projects](https://philiprehberger.com/open-source-packages)

💻 [GitHub Profile](https://github.com/philiprehberger)

🔗 [LinkedIn Profile](https://www.linkedin.com/in/philiprehberger)

## License

[MIT](LICENSE)
