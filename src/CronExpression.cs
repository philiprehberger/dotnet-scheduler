namespace Philiprehberger.Scheduler;

/// <summary>
/// Parses and evaluates standard 5-field cron expressions (minute hour day month weekday).
/// Supports wildcards (*), specific values, ranges (1-5), steps (*/5), and lists (1,3,5).
/// </summary>
public sealed class CronExpression
{
    private readonly HashSet<int> _minutes;
    private readonly HashSet<int> _hours;
    private readonly HashSet<int> _days;
    private readonly HashSet<int> _months;
    private readonly HashSet<int> _weekdays;
    private readonly string _expression;

    private CronExpression(
        string expression,
        HashSet<int> minutes,
        HashSet<int> hours,
        HashSet<int> days,
        HashSet<int> months,
        HashSet<int> weekdays)
    {
        _expression = expression;
        _minutes = minutes;
        _hours = hours;
        _days = days;
        _months = months;
        _weekdays = weekdays;
    }

    /// <summary>
    /// Parses a standard 5-field cron expression.
    /// </summary>
    /// <param name="expression">A cron expression in the format: minute hour day month weekday.</param>
    /// <returns>A parsed <see cref="CronExpression"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the expression is invalid.</exception>
    public static CronExpression Parse(string expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var parts = expression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5)
        {
            throw new ArgumentException(
                "Cron expression must have exactly 5 fields: minute hour day month weekday.",
                nameof(expression));
        }

        var minutes = ParseField(parts[0], 0, 59, "minute");
        var hours = ParseField(parts[1], 0, 23, "hour");
        var days = ParseField(parts[2], 1, 31, "day");
        var months = ParseField(parts[3], 1, 12, "month");
        var weekdays = ParseField(parts[4], 0, 6, "weekday");

        return new CronExpression(expression, minutes, hours, days, months, weekdays);
    }

    /// <summary>
    /// Gets the next occurrence after the specified time.
    /// </summary>
    /// <param name="from">The time to search from (exclusive).</param>
    /// <returns>The next matching occurrence, or <c>null</c> if none found within a reasonable range.</returns>
    public DateTimeOffset? GetNextOccurrence(DateTimeOffset from)
    {
        var candidate = new DateTimeOffset(
            from.Year, from.Month, from.Day, from.Hour, from.Minute, 0, from.Offset)
            .AddMinutes(1);

        var limit = from.AddYears(2);

        while (candidate <= limit)
        {
            if (Matches(candidate))
            {
                return candidate;
            }

            candidate = candidate.AddMinutes(1);

            // Skip ahead when month doesn't match
            if (!_months.Contains(candidate.Month))
            {
                candidate = new DateTimeOffset(
                    candidate.Year, candidate.Month, 1, 0, 0, 0, candidate.Offset)
                    .AddMonths(1);
                continue;
            }

            // Skip ahead when day doesn't match
            if (!_days.Contains(candidate.Day) ||
                !_weekdays.Contains((int)candidate.DayOfWeek))
            {
                candidate = new DateTimeOffset(
                    candidate.Year, candidate.Month, candidate.Day, 0, 0, 0, candidate.Offset)
                    .AddDays(1);
                continue;
            }

            // Skip ahead when hour doesn't match
            if (!_hours.Contains(candidate.Hour))
            {
                candidate = new DateTimeOffset(
                    candidate.Year, candidate.Month, candidate.Day, candidate.Hour, 0, 0, candidate.Offset)
                    .AddHours(1);
            }
        }

        return null;
    }

    /// <summary>
    /// Determines whether the specified time matches this cron expression.
    /// </summary>
    /// <param name="time">The time to check.</param>
    /// <returns><c>true</c> if the time matches all fields; otherwise, <c>false</c>.</returns>
    public bool Matches(DateTimeOffset time)
    {
        return _minutes.Contains(time.Minute)
            && _hours.Contains(time.Hour)
            && _days.Contains(time.Day)
            && _months.Contains(time.Month)
            && _weekdays.Contains((int)time.DayOfWeek);
    }

    /// <inheritdoc />
    public override string ToString() => _expression;

    private static HashSet<int> ParseField(string field, int min, int max, string fieldName)
    {
        var values = new HashSet<int>();

        foreach (var part in field.Split(','))
        {
            if (part == "*")
            {
                for (var i = min; i <= max; i++)
                    values.Add(i);
            }
            else if (part.Contains('/'))
            {
                var stepParts = part.Split('/');
                if (stepParts.Length != 2 || !int.TryParse(stepParts[1], out var step) || step <= 0)
                {
                    throw new ArgumentException(
                        $"Invalid step value in {fieldName} field: '{part}'.",
                        fieldName);
                }

                int start;
                int end = max;

                if (stepParts[0] == "*")
                {
                    start = min;
                }
                else if (stepParts[0].Contains('-'))
                {
                    var rangeParts = stepParts[0].Split('-');
                    start = ParseValue(rangeParts[0], min, max, fieldName);
                    end = ParseValue(rangeParts[1], min, max, fieldName);
                }
                else
                {
                    start = ParseValue(stepParts[0], min, max, fieldName);
                }

                for (var i = start; i <= end; i += step)
                    values.Add(i);
            }
            else if (part.Contains('-'))
            {
                var rangeParts = part.Split('-');
                if (rangeParts.Length != 2)
                {
                    throw new ArgumentException(
                        $"Invalid range in {fieldName} field: '{part}'.",
                        fieldName);
                }

                var rangeStart = ParseValue(rangeParts[0], min, max, fieldName);
                var rangeEnd = ParseValue(rangeParts[1], min, max, fieldName);

                for (var i = rangeStart; i <= rangeEnd; i++)
                    values.Add(i);
            }
            else
            {
                values.Add(ParseValue(part, min, max, fieldName));
            }
        }

        return values;
    }

    private static int ParseValue(string value, int min, int max, string fieldName)
    {
        if (!int.TryParse(value, out var result))
        {
            throw new ArgumentException(
                $"Invalid value in {fieldName} field: '{value}'.",
                fieldName);
        }

        if (result < min || result > max)
        {
            throw new ArgumentException(
                $"Value {result} is out of range ({min}-{max}) for {fieldName} field.",
                fieldName);
        }

        return result;
    }
}
