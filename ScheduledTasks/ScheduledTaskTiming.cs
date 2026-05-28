namespace LtyybBot;

internal static class ScheduledTaskTiming
{
    public static DateTimeOffset? CalculateNextRunUtc(
        ScheduledTaskDefinition task,
        ScheduledTaskState state,
        DateTimeOffset nowUtc)
    {
        return task.Schedule.Type switch
        {
            ScheduledTaskScheduleTypes.Interval => GetNextIntervalUtc(task, state, nowUtc),
            ScheduledTaskScheduleTypes.Once => GetNextOnceUtc(task, state),
            ScheduledTaskScheduleTypes.Daily => GetNextDailyUtc(task, state, nowUtc),
            _ => null
        };
    }

    private static DateTimeOffset? GetNextIntervalUtc(
        ScheduledTaskDefinition task,
        ScheduledTaskState state,
        DateTimeOffset nowUtc)
    {
        var intervalSeconds = task.Schedule.IntervalSeconds;
        if (intervalSeconds is null || intervalSeconds <= 0)
        {
            return null;
        }

        if (state.LastAttemptUtc is null)
        {
            return nowUtc;
        }

        return state.LastAttemptUtc.Value.AddSeconds(intervalSeconds.Value);
    }

    private static DateTimeOffset? GetNextOnceUtc(ScheduledTaskDefinition task, ScheduledTaskState state)
    {
        if (state.LastSuccessUtc is not null)
        {
            return null;
        }

        return task.Schedule.RunAtUtc;
    }

    private static DateTimeOffset? GetNextDailyUtc(
        ScheduledTaskDefinition task,
        ScheduledTaskState state,
        DateTimeOffset nowUtc)
    {
        if (task.Schedule.DailyTimes.Count == 0)
        {
            return null;
        }

        var timezone = FindTimeZone(task.TimeZone);
        var nowLocal = TimeZoneInfo.ConvertTime(nowUtc, timezone);
        var today = DateOnly.FromDateTime(nowLocal.DateTime);
        var lastSuccessLocalDate = state.LastSuccessUtc is null
            ? default(DateOnly?)
            : DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(state.LastSuccessUtc.Value, timezone).DateTime);

        foreach (var time in task.Schedule.DailyTimes.Select(ParseTimeOnly).OrderBy(static value => value))
        {
            var localDateTime = nowLocal.Date + time.ToTimeSpan();
            var localOffset = new DateTimeOffset(localDateTime, timezone.GetUtcOffset(localDateTime));
            var utc = TimeZoneInfo.ConvertTime(localOffset, TimeZoneInfo.Utc);

            if (utc > nowUtc)
            {
                return utc;
            }

            if (utc <= nowUtc && lastSuccessLocalDate != today)
            {
                return utc;
            }
        }

        var nextDate = nowLocal.Date.AddDays(1);
        var nextTime = ParseTimeOnly(task.Schedule.DailyTimes.MinBy(static value => ParseTimeOnly(value))!);
        var nextLocalDateTime = nextDate + nextTime.ToTimeSpan();
        var nextLocalOffset = new DateTimeOffset(nextLocalDateTime, timezone.GetUtcOffset(nextLocalDateTime));
        return TimeZoneInfo.ConvertTime(nextLocalOffset, TimeZoneInfo.Utc);
    }

    private static TimeZoneInfo FindTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    private static TimeOnly ParseTimeOnly(string value)
    {
        if (!TimeOnly.TryParse(value, out var result))
        {
            throw new InvalidOperationException($"无法解析 daily 调度时间: {value}");
        }

        return result;
    }
}
