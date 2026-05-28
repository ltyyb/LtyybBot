namespace LtyybBot;

internal sealed class ScheduledTasksService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    private readonly ILogger<ScheduledTasksService> _logger;
    private readonly ScheduledTaskStore _taskStore;
    private readonly ScheduledTaskRunner _taskRunner;
    private readonly TimeProvider _timeProvider;

    public ScheduledTasksService(
        ILogger<ScheduledTasksService> logger,
        ScheduledTaskStore taskStore,
        ScheduledTaskRunner taskRunner,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _taskStore = taskStore;
        _taskRunner = taskRunner;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScheduledTasksService 已启动。任务目录: {TaskDirectory}", _taskStore.TaskDirectory);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTasksAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "扫描定时任务时发生异常。");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessTasksAsync(CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var taskFiles = _taskStore.GetTaskFiles();

        foreach (var taskFile in taskFiles)
        {
            var loadResult = await _taskStore.TryLoadAsync(taskFile, cancellationToken);
            if (!loadResult.Success || loadResult.Definition is null || loadResult.State is null)
            {
                continue;
            }

            var task = loadResult.Definition;
            if (!task.Enabled)
            {
                continue;
            }

            var dueTime = ScheduledTaskTiming.CalculateNextRunUtc(task, loadResult.State, now);
            if (dueTime is null || dueTime > now)
            {
                continue;
            }

            await _taskRunner.RunAsync(taskFile, task, loadResult.State, now, cancellationToken);
        }
    }
}
