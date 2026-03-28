using Xunit;
using Philiprehberger.Scheduler;

namespace Philiprehberger.Scheduler.Tests;

public class InMemoryJobHistoryTests
{
    [Fact]
    public void Record_StoresExecutionRecord()
    {
        var history = new InMemoryJobHistory();
        var record = new JobExecutionRecord("test-job", DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1), true, null);

        history.Record(record);

        var records = history.GetHistory("test-job");
        Assert.Single(records);
        Assert.Equal("test-job", records[0].JobName);
    }

    [Fact]
    public void GetHistory_WithUnknownJob_ReturnsEmptyList()
    {
        var history = new InMemoryJobHistory();

        var records = history.GetHistory("unknown");

        Assert.Empty(records);
    }

    [Fact]
    public void GetHistory_ReturnsRecordsOrderedByMostRecentFirst()
    {
        var history = new InMemoryJobHistory();
        var time1 = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var time2 = new DateTimeOffset(2026, 1, 1, 11, 0, 0, TimeSpan.Zero);
        var time3 = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        history.Record(new JobExecutionRecord("job", time1, TimeSpan.FromSeconds(1), true, null));
        history.Record(new JobExecutionRecord("job", time3, TimeSpan.FromSeconds(1), true, null));
        history.Record(new JobExecutionRecord("job", time2, TimeSpan.FromSeconds(1), true, null));

        var records = history.GetHistory("job");
        Assert.Equal(3, records.Count);
        Assert.Equal(time3, records[0].StartTime);
        Assert.Equal(time2, records[1].StartTime);
        Assert.Equal(time1, records[2].StartTime);
    }

    [Fact]
    public void GetAll_ReturnsRecordsAcrossAllJobs()
    {
        var history = new InMemoryJobHistory();
        history.Record(new JobExecutionRecord("job-a", DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1), true, null));
        history.Record(new JobExecutionRecord("job-b", DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2), false, "error"));

        var all = history.GetAll();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void Record_EvictsOldestWhenMaxExceeded()
    {
        var history = new InMemoryJobHistory(maxRecordsPerJob: 2);
        var time1 = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var time2 = new DateTimeOffset(2026, 1, 1, 11, 0, 0, TimeSpan.Zero);
        var time3 = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        history.Record(new JobExecutionRecord("job", time1, TimeSpan.FromSeconds(1), true, null));
        history.Record(new JobExecutionRecord("job", time2, TimeSpan.FromSeconds(1), true, null));
        history.Record(new JobExecutionRecord("job", time3, TimeSpan.FromSeconds(1), true, null));

        var records = history.GetHistory("job");
        Assert.Equal(2, records.Count);
        Assert.DoesNotContain(records, r => r.StartTime == time1);
    }

    [Fact]
    public void Constructor_WithZeroMaxRecords_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new InMemoryJobHistory(maxRecordsPerJob: 0));
    }

    [Fact]
    public void Record_WithNull_ThrowsArgumentNullException()
    {
        var history = new InMemoryJobHistory();
        Assert.Throws<ArgumentNullException>(() => history.Record(null!));
    }

    [Fact]
    public void GetHistory_WithNull_ThrowsArgumentNullException()
    {
        var history = new InMemoryJobHistory();
        Assert.Throws<ArgumentNullException>(() => history.GetHistory(null!));
    }

    [Fact]
    public void JobExecutionRecord_StoresErrorMessage()
    {
        var record = new JobExecutionRecord("job", DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1), false, "Something broke");

        Assert.False(record.Success);
        Assert.Equal("Something broke", record.ErrorMessage);
    }
}
