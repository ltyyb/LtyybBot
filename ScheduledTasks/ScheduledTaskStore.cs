using System.Text.Json;

namespace LtyybBot;

internal sealed class ScheduledTaskStore
{
    private readonly ILogger<ScheduledTaskStore> _logger;
    private readonly ScheduledTasksOptions _options;

    public ScheduledTaskStore(ILogger<ScheduledTaskStore> logger, IOptions<ScheduledTasksOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public string TaskDirectory => Path.GetFullPath(_options.TaskDirectory);

    public IReadOnlyList<string> GetTaskFiles()
    {
        Directory.CreateDirectory(TaskDirectory);
        return Directory
            .EnumerateFiles(TaskDirectory, _options.TaskFilePattern, SearchOption.TopDirectoryOnly)
            .Where(path => !path.EndsWith(".state.json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<ScheduledTaskLoadResult> TryLoadAsync(string taskFilePath, CancellationToken cancellationToken)
    {
        try
        {
            await using var definitionStream = File.OpenRead(taskFilePath);
            var definition = await JsonSerializer.DeserializeAsync<ScheduledTaskDefinition>(
                definitionStream,
                ScheduledTaskJson.SerializerOptions,
                cancellationToken);

            if (definition is null)
            {
                _logger.LogWarning("任务文件为空或无法解析: {TaskFilePath}", taskFilePath);
                return ScheduledTaskLoadResult.CreateFailure();
            }

            ValidateDefinition(taskFilePath, definition);
            var state = await LoadStateAsync(taskFilePath, cancellationToken);
            return ScheduledTaskLoadResult.CreateSuccess(definition, state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载任务文件失败: {TaskFilePath}", taskFilePath);
            return ScheduledTaskLoadResult.CreateFailure();
        }
    }

    public async Task SaveStateAsync(string taskFilePath, ScheduledTaskState state, CancellationToken cancellationToken)
    {
        var stateFilePath = GetStateFilePath(taskFilePath);
        Directory.CreateDirectory(Path.GetDirectoryName(stateFilePath)!);

        await using var stream = File.Create(stateFilePath);
        await JsonSerializer.SerializeAsync(stream, state, ScheduledTaskJson.SerializerOptions, cancellationToken);
    }

    private async Task<ScheduledTaskState> LoadStateAsync(string taskFilePath, CancellationToken cancellationToken)
    {
        var stateFilePath = GetStateFilePath(taskFilePath);
        if (!File.Exists(stateFilePath))
        {
            return new ScheduledTaskState();
        }

        await using var stream = File.OpenRead(stateFilePath);
        return await JsonSerializer.DeserializeAsync<ScheduledTaskState>(
                   stream,
                   ScheduledTaskJson.SerializerOptions,
                   cancellationToken)
               ?? new ScheduledTaskState();
    }

    private static string GetStateFilePath(string taskFilePath)
    {
        return $"{taskFilePath}.state.json";
    }

    private static void ValidateDefinition(string taskFilePath, ScheduledTaskDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Id))
        {
            throw new InvalidOperationException($"任务文件缺少 id: {taskFilePath}");
        }

        if (definition.Actions.Count == 0)
        {
            throw new InvalidOperationException($"任务文件未定义任何 actions: {taskFilePath}");
        }
    }
}

internal readonly record struct ScheduledTaskLoadResult(
    bool Success,
    ScheduledTaskDefinition? Definition,
    ScheduledTaskState? State)
{
    public static ScheduledTaskLoadResult CreateFailure() => new(false, null, null);

    public static ScheduledTaskLoadResult CreateSuccess(ScheduledTaskDefinition definition, ScheduledTaskState state)
        => new(true, definition, state);
}
