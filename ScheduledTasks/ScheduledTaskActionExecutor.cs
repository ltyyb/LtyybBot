using Newtonsoft.Json.Linq;
using Sisters.WudiLib;

namespace LtyybBot;

internal sealed class ScheduledTaskActionExecutor
{
    private readonly ILogger<ScheduledTaskActionExecutor> _logger;
    private readonly ICQApiService _cqApiService;

    public ScheduledTaskActionExecutor(ILogger<ScheduledTaskActionExecutor> logger, ICQApiService cqApiService)
    {
        _logger = logger;
        _cqApiService = cqApiService;
    }

    public async Task ExecuteAsync(ScheduledTaskDefinition task, CancellationToken cancellationToken)
    {
        foreach (var action in task.Actions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ExecuteActionAsync(task, action, cancellationToken);
        }
    }

    private async Task ExecuteActionAsync(
        ScheduledTaskDefinition task,
        ScheduledTaskActionDefinition action,
        CancellationToken cancellationToken)
    {
        var api = _cqApiService.OnebotApi;
        if (!_cqApiService.IsAvailable || api is null)
        {
            throw new InvalidOperationException("CQ API 当前不可用，无法执行定时任务。");
        }

        switch (action.Type)
        {
            case ScheduledTaskActionTypes.SendGroupMessage:
                await ExecuteSendGroupMessageAsync(api, action, cancellationToken);
                return;
            case ScheduledTaskActionTypes.SendPrivateMessage:
                await ExecuteSendPrivateMessageAsync(api, action, cancellationToken);
                return;
            case ScheduledTaskActionTypes.CallApi:
                await ExecuteCallApiAsync(task, api, action, cancellationToken);
                return;
            default:
                throw new InvalidOperationException($"任务 {task.Id} 包含未知动作类型: {action.Type}");
        }
    }

    private static async Task ExecuteSendGroupMessageAsync(
        HttpApiClient api,
        ScheduledTaskActionDefinition action,
        CancellationToken cancellationToken)
    {
        if (action.GroupId is null)
        {
            throw new InvalidOperationException("send_group_message 动作缺少 groupId。");
        }

        if (string.IsNullOrWhiteSpace(action.Message))
        {
            throw new InvalidOperationException("send_group_message 动作缺少 message。");
        }

        cancellationToken.ThrowIfCancellationRequested();
        await api.SendGroupMessageAsync(action.GroupId.Value, action.Message);
    }

    private static async Task ExecuteSendPrivateMessageAsync(
        HttpApiClient api,
        ScheduledTaskActionDefinition action,
        CancellationToken cancellationToken)
    {
        if (action.UserId is null)
        {
            throw new InvalidOperationException("send_private_message 动作缺少 userId。");
        }

        if (string.IsNullOrWhiteSpace(action.Message))
        {
            throw new InvalidOperationException("send_private_message 动作缺少 message。");
        }

        cancellationToken.ThrowIfCancellationRequested();
        await api.SendPrivateMessageAsync(action.UserId.Value, action.Message);
    }

    private async Task ExecuteCallApiAsync(
        ScheduledTaskDefinition task,
        HttpApiClient api,
        ScheduledTaskActionDefinition action,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(action.ApiAction))
        {
            throw new InvalidOperationException("call_api 动作缺少 apiAction。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var payload = action.ApiParams.HasValue
            ? JToken.Parse(action.ApiParams.Value.GetRawText())
            : new JObject();

        var success = await api.CallAsync(action.ApiAction, payload);
        if (!success)
        {
            _logger.LogWarning("任务 {TaskId} 的通用 API 调用返回 false。Action={ApiAction}", task.Id, action.ApiAction);
        }
    }
}
