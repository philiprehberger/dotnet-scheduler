# Philiprehberger.Scheduler

[![CI](https://github.com/philiprehberger/dotnet-scheduler/actions/workflows/ci.yml/badge.svg)](https://github.com/philiprehberger/dotnet-scheduler/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Philiprehberger.Scheduler.svg)](https://www.nuget.org/packages/Philiprehberger.Scheduler)
[![License](https://img.shields.io/github/license/philiprehberger/dotnet-scheduler)](LICENSE)

Lightweight in-process job scheduler with cron expressions — no external infrastructure required.

## Install

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
builder.Services.AddScheduler(options =>
{
    options.AddJob<CleanupJob>("*/5 * * * *"); // every 5 minutes
});
```

### Using Attributes

```csharp
[ScheduledJob("cleanup", "*/5 * * * *")]
public class CleanupJob : IScheduledJob
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        // ...
    }
}
```

### Cron Expressions

```csharp
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

| Property | Description |
|----------|-------------|
| `Name` | Job name |
| `CronExpression` | Cron schedule |
| `PreventOverlap` | Skip if previous run is still executing (default: true) |

### `CronScheduler`

| Method | Description |
|--------|-------------|
| `StartAsync(CancellationToken)` | Start the scheduler |
| `StopAsync(CancellationToken)` | Stop gracefully |

## License

MIT
