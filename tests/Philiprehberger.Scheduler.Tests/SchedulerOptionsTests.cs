using Xunit;
using Philiprehberger.Scheduler;

namespace Philiprehberger.Scheduler.Tests;

public class SchedulerOptionsTests
{
    [Fact]
    public void ScheduleOnce_RegistersOneTimeJob()
    {
        var options = new SchedulerOptions();
        var called = false;

        options.ScheduleOnce("one-time", async ct =>
        {
            called = true;
            await Task.CompletedTask;
        }, DateTimeOffset.UtcNow.AddMinutes(5));

        // The registration is internal, but we can verify it doesn't throw
        Assert.False(called);
    }

    [Fact]
    public void ScheduleOnce_WithNullName_ThrowsArgumentNullException()
    {
        var options = new SchedulerOptions();

        Assert.Throws<ArgumentNullException>(() =>
            options.ScheduleOnce(null!, ct => Task.CompletedTask, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ScheduleOnce_WithNullAction_ThrowsArgumentNullException()
    {
        var options = new SchedulerOptions();

        Assert.Throws<ArgumentNullException>(() =>
            options.ScheduleOnce("test", null!, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void OnJobStarted_CanBeSetAndInvoked()
    {
        var options = new SchedulerOptions();
        string? receivedName = null;

        options.OnJobStarted = name => receivedName = name;
        options.OnJobStarted.Invoke("test-job");

        Assert.Equal("test-job", receivedName);
    }

    [Fact]
    public void OnJobCompleted_CanBeSetAndInvoked()
    {
        var options = new SchedulerOptions();
        string? receivedName = null;
        TimeSpan receivedDuration = TimeSpan.Zero;

        options.OnJobCompleted = (name, duration) =>
        {
            receivedName = name;
            receivedDuration = duration;
        };
        options.OnJobCompleted.Invoke("job", TimeSpan.FromSeconds(5));

        Assert.Equal("job", receivedName);
        Assert.Equal(TimeSpan.FromSeconds(5), receivedDuration);
    }

    [Fact]
    public void OnJobFailed_CanBeSetAndInvoked()
    {
        var options = new SchedulerOptions();
        string? receivedName = null;
        Exception? receivedException = null;

        options.OnJobFailed = (name, ex) =>
        {
            receivedName = name;
            receivedException = ex;
        };
        var testEx = new InvalidOperationException("test error");
        options.OnJobFailed.Invoke("job", testEx);

        Assert.Equal("job", receivedName);
        Assert.Same(testEx, receivedException);
    }

    [Fact]
    public void EventCallbacks_DefaultToNull()
    {
        var options = new SchedulerOptions();

        Assert.Null(options.OnJobStarted);
        Assert.Null(options.OnJobCompleted);
        Assert.Null(options.OnJobFailed);
    }

    [Fact]
    public void AddJob_WithTimezone_ValidatesTimezoneId()
    {
        var options = new SchedulerOptions();

        Assert.Throws<TimeZoneNotFoundException>(() =>
            options.AddJob<TestJob>("* * * * *", "Invalid/Timezone"));
    }

    [Fact]
    public void AddJob_WithNullTimezone_UsesUtc()
    {
        var options = new SchedulerOptions();

        // Should not throw
        options.AddJob<TestJob>("* * * * *", timeZone: null);
    }

    [Fact]
    public void AddJob_WithValidTimezone_DoesNotThrow()
    {
        var options = new SchedulerOptions();

        // Should not throw - "Etc/UTC" is always available
        options.AddJob<TestJob>("* * * * *", "Etc/UTC");
    }

    private class TestJob : IScheduledJob
    {
        public Task ExecuteAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
