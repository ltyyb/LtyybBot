namespace LtyybBot;

internal sealed class ScheduledTaskRunner
{
    private readonly ILogger<ScheduledTaskRunner> _logger;
    private readonly ScheduledTaskStore _taskStore;
    private readonly ScheduledTaskActionExecutor _executor;

    public ScheduledTaskRunner(
        ILogger<ScheduledTaskRunner> logger,
        ScheduledTaskStore taskStore,
        ScheduledTaskActionExecutor executor)
    {
        _logger = logger;
        _taskStore = taskStore;
        _executor = executor;
    }

    public async Task RunAsync(
        string taskFilePath,
        ScheduledTaskDefinition task,
        ScheduledTaskState state,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始执行定时任务: {TaskId}", task.Id);

        state.LastAttemptUtc = startedAtUtc;
        state.LastError = null;
        await _taskStore.SaveStateAsync(taskFilePath, state, cancellationToken);

        try
        {
            await _executor.ExecuteAsync(task, cancellationToken);
            state.LastSuccessUtc = startedAtUtc;
            state.LastError = null;
            state.ConsecutiveFailures = 0;
            await _taskStore.SaveStateAsync(taskFilePath, state, cancellationToken);
            _logger.LogInformation("定时任务执行成功: {TaskId}", task.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            state.LastError = ex.Message;
            state.ConsecutiveFailures += 1;
            await _taskStore.SaveStateAsync(taskFilePath, state, cancellationToken);
            _logger.LogError(ex, "定时任务执行失败: {TaskId}", task.Id);
        }
    }
}
