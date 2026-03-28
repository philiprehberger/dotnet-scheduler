using Xunit;
using Philiprehberger.Scheduler;

namespace Philiprehberger.Scheduler.Tests;

public class ScheduledJobAttributeTests
{
    [Fact]
    public void Constructor_SetsNameAndCronExpression()
    {
        var attr = new ScheduledJobAttribute("test-job", "*/5 * * * *");

        Assert.Equal("test-job", attr.Name);
        Assert.Equal("*/5 * * * *", attr.CronExpression);
    }

    [Fact]
    public void PreventOverlap_DefaultsToTrue()
    {
        var attr = new ScheduledJobAttribute("job", "* * * * *");

        Assert.True(attr.PreventOverlap);
    }

    [Fact]
    public void TimeZone_DefaultsToNull()
    {
        var attr = new ScheduledJobAttribute("job", "* * * * *");

        Assert.Null(attr.TimeZone);
    }

    [Fact]
    public void TimeZone_CanBeSet()
    {
        var attr = new ScheduledJobAttribute("job", "* * * * *")
        {
            TimeZone = "America/New_York"
        };

        Assert.Equal("America/New_York", attr.TimeZone);
    }

    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ScheduledJobAttribute(null!, "* * * * *"));
    }

    [Fact]
    public void Constructor_WithNullCron_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ScheduledJobAttribute("job", null!));
    }
}
