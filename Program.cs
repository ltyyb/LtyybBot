using LtyybBot;
using Microsoft.Extensions.DependencyInjection;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// 绑定配置
builder.Services.Configure<ClientOptions>(builder.Configuration.GetSection(ClientOptions.Position));
builder.Services.Configure<BotOptions>(builder.Configuration.GetSection(BotOptions.Position));
builder.Services.Configure<ScheduledTasksOptions>(builder.Configuration.GetSection(ScheduledTasksOptions.Position));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ScheduledTaskStore>();
builder.Services.AddSingleton<ScheduledTaskActionExecutor>();
builder.Services.AddSingleton<ScheduledTaskRunner>();
builder.Services.AddSingleton<CQApiService>();
builder.Services.AddSingleton<ICQApiService>(static sp => sp.GetRequiredService<CQApiService>());
builder.Services.AddHostedService(static sp => sp.GetRequiredService<CQApiService>());
builder.Services.AddHostedService<ScheduledTasksService>();

using IHost host = builder.Build();
await host.RunAsync();

