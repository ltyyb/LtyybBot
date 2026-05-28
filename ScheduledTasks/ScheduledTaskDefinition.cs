using System.Text.Json;
using System.Text.Json.Serialization;

namespace LtyybBot;

internal sealed class ScheduledTaskDefinition
{
    public required string Id { get; init; }
    public string? Description { get; init; }
    public bool Enabled { get; init; } = true;
    public string TimeZone { get; init; } = "Asia/Shanghai";
    public ScheduledTaskScheduleDefinition Schedule { get; init; } = new();
    public List<ScheduledTaskActionDefinition> Actions { get; init; } = [];
}

internal sealed class ScheduledTaskScheduleDefinition
{
    public string Type { get; init; } = ScheduledTaskScheduleTypes.Interval;
    public int? IntervalSeconds { get; init; }
    public DateTimeOffset? RunAtUtc { get; init; }
    public List<string> DailyTimes { get; init; } = [];
}

internal sealed class ScheduledTaskActionDefinition
{
    public required string Type { get; init; }
    public long? GroupId { get; init; }
    public long? UserId { get; init; }
    public string? Message { get; init; }
    public string? ApiAction { get; init; }
    public JsonElement? ApiParams { get; init; }
}

internal sealed class ScheduledTaskState
{
    public DateTimeOffset? LastAttemptUtc { get; set; }
    public DateTimeOffset? LastSuccessUtc { get; set; }
    public string? LastError { get; set; }
    public int ConsecutiveFailures { get; set; }
}

internal static class ScheduledTaskScheduleTypes
{
    public const string Interval = "interval";
    public const string Once = "once";
    public const string Daily = "daily";
}

internal static class ScheduledTaskActionTypes
{
    public const string SendGroupMessage = "send_group_message";
    public const string SendPrivateMessage = "send_private_message";
    public const string CallApi = "call_api";
}

internal static class ScheduledTaskJson
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
