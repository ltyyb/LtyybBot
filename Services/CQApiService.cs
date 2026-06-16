namespace LtyybBot;

using Sisters.WudiLib;
using Sisters.WudiLib.Posts;
using Sisters.WudiLib.WebSocket.Reverse;
internal class CQApiService : BackgroundService, ICQApiService
{
    private readonly ILogger<CQApiService> _logger;
    private readonly ClientOptions _options;
    private readonly BotOptions _botOptions;

    public HttpApiClient? OnebotApi { get; private set; } = null;
    public bool IsAvailable { get; private set; } = false;

    public CQApiService(ILogger<CQApiService> logger, IOptions<ClientOptions> clientOpt, IOptions<BotOptions> botOpt)
    {
        _logger = logger;
        _options = clientOpt.Value;
        _botOptions = botOpt.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🏃‍➡️| CQApiService 正启动。");
        _logger.LogInformation($"""
        ⚙️| 当前连接配置:
            WsServerPort: {_options.WsServerPort}
            WsServerToken: {(string.IsNullOrWhiteSpace(_options.WsServerToken) ? "（未设置）" : "（已设置）")}
        ==================================
        """);
        if (_options.WsServerPort <= 0 || _options.WsServerPort > 65535)
        {
            _logger.LogError("❌| 配置错误: WsServerPort 必须在 1-65535 之间。请检查配置文件。");
            return;
        }
        _logger.LogInformation($"🆕| 请在 NapCat 网络配置中新增一个『WebSocket 客户端』，配置 URL 为 ws://<your_ip>:{_options.WsServerPort} , 消息格式为 Array, 并设置好 Token。详见文档。");


        var reverseWSServer = new ReverseWebSocketServer(_options.WsServerPort);

        reverseWSServer.SetListenerAuthenticationAndConfiguration((listener, selfId) =>
        {
            OnebotApi = listener.ApiClient; // 当建立反向 WebSocket 连接时，把 API 客户端赋值到外面。

            listener.SocketDisconnected += () =>
            {
                _logger.LogWarning("WebSocket连接已断开");
                OnebotApi = null;
                IsAvailable = false;
            };
            listener.EventPosted += (e) =>
            {
                if (!IsAvailable)
                {
                    _logger.LogInformation("WebSocket连接已建立, 收到事件。\n{e}", e.ToString());
                }
                IsAvailable = true;
            };
            listener.OnExceptionWithRawContent += (ex, rawContent) =>
            {
                _logger.LogError(ex, "OneBot 上报发生异常，原始内容: {rawContent}", rawContent);
            };
            listener.MessageEvent += async (api, e) =>
            {
                // 别急。

                
                // try
                // {
                //     var cmdResponse = await _commandRegistry.TryExecuteSurveyCommandAsync(e, stoppingToken);
                //     if (cmdResponse is not null)
                //     {
                //         if (cmdResponse.Message is not null)
                //         {
                //             await ReplyMessageWithAtAsync(e, cmdResponse.Message);
                //         }
                //         else if (cmdResponse.Success)
                //         {
                //             await ReplyMessageWithAtAsync(e, "指令执行成功。");
                //         }
                //         else
                //         {
                //             await ReplyMessageWithAtAsync(e, "指令执行失败。无更多信息，如必要请询问管理员索要日志。");
                //         }
                //     }
                // }
                // catch (Exception ex)
                // {
                //     _logger.LogError(ex, "处理消息事件时发生异常。");
                //     await ReplyMessageWithAtAsync(e, $"处理消息时发生异常: {ex.Message}，请稍后再试或联系管理员。");
                // }

            };
        }, _options.WsServerToken);
        reverseWSServer.Start(stoppingToken);
        _logger.LogInformation("✅| CQApiService 已启动并等待客户端连接。");




    }


}
