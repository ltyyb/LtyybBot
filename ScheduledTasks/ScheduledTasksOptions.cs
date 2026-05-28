namespace LtyybBot;

internal sealed class ScheduledTasksOptions
{
    public const string Position = "ScheduledTasks";

    public string TaskDirectory { get; init; } = "data/scheduled-tasks";
    public string TaskFilePattern { get; init; } = "*.task.json";
}
