using Xunit;
using Philiprehberger.Scheduler;

namespace Philiprehberger.Scheduler.Tests;

public class CronExpressionTests
{
    [Fact]
    public void Parse_WithValidExpression_ReturnsInstance()
    {
        var cron = CronExpression.Parse("*/5 * * * *");

        Assert.NotNull(cron);
        Assert.Equal("*/5 * * * *", cron.ToString());
    }

    [Fact]
    public void Parse_WithNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CronExpression.Parse(null!));
    }

    [Fact]
    public void Parse_WithInvalidFieldCount_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CronExpression.Parse("* * *"));
    }

    [Fact]
    public void Matches_WithMatchingTime_ReturnsTrue()
    {
        var cron = CronExpression.Parse("30 14 * * *");
        var time = new DateTimeOffset(2026, 3, 28, 14, 30, 0, TimeSpan.Zero);

        Assert.True(cron.Matches(time));
    }

    [Fact]
    public void Matches_WithNonMatchingTime_ReturnsFalse()
    {
        var cron = CronExpression.Parse("30 14 * * *");
        var time = new DateTimeOffset(2026, 3, 28, 14, 31, 0, TimeSpan.Zero);

        Assert.False(cron.Matches(time));
    }

    [Fact]
    public void GetNextOccurrence_ReturnsCorrectTime()
    {
        var cron = CronExpression.Parse("0 12 * * *");
        var from = new DateTimeOffset(2026, 3, 28, 10, 0, 0, TimeSpan.Zero);

        var next = cron.GetNextOccurrence(from);

        Assert.NotNull(next);
        Assert.Equal(12, next.Value.Hour);
        Assert.Equal(0, next.Value.Minute);
    }

    [Theory]
    [InlineData("0 0 1 1 *", 1, 1)]
    [InlineData("0 0 15 6 *", 6, 15)]
    public void Matches_WithSpecificDayAndMonth_MatchesCorrectly(string expr, int month, int day)
    {
        var cron = CronExpression.Parse(expr);
        var time = new DateTimeOffset(2026, month, day, 0, 0, 0, TimeSpan.Zero);

        Assert.True(cron.Matches(time));
    }
}
