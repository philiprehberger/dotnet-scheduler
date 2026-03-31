# Changelog

## 0.2.1 (2026-03-31)

- Standardize README to 3-badge format with emoji Support section
- Update CI actions to v5 for Node.js 24 compatibility

## 0.2.0 (2026-03-28)

- Add timezone-aware scheduling via `TimeZone` property on `ScheduledJobAttribute` and `AddJob` overload
- Add job execution history with `IJobHistory`, `JobExecutionRecord`, and `InMemoryJobHistory`
- Add one-time scheduled jobs via `ScheduleOnce` method on `SchedulerOptions`
- Add job event callbacks: `OnJobStarted`, `OnJobCompleted`, `OnJobFailed` on `SchedulerOptions`
- Add unit test project with xUnit
- Add GitHub issue templates, dependabot config, and PR template
- Add missing README badges (GitHub release, Last updated, Bug Reports, Feature Requests)
- Add Support section to README
- Add test step to CI workflow
- Update description to reflect new features

## 0.1.6 (2026-03-24)

- Sync .csproj description with README

## 0.1.5 (2026-03-22)

- Add dates to changelog entries

## 0.1.4 (2026-03-17)

- Rename Install section to Installation in README per package guide

## 0.1.3 (2026-03-16)

- Add Development section to README
- Add GenerateDocumentationFile, RepositoryType, PackageReadmeFile to .csproj

## 0.1.0 (2026-03-15)

- Initial release
- Cron expression parser with support for wildcards, ranges, steps, and lists
- Hosted service scheduler with automatic job execution
- Overlap prevention per job
- Graceful shutdown support
- Attribute-based job metadata with `[ScheduledJob]`
- Dependency injection integration via `AddScheduler()`
