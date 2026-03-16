# Changelog

## 0.1.3

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
