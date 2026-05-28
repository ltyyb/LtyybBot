# Scheduled Tasks 模块说明

本目录存放定时任务相关代码。

目录职责：

- `ScheduledTasksService`：后台轮询调度入口。
- `ScheduledTaskStore`：任务文件和状态文件的读取、保存、校验。
- `ScheduledTaskTiming`：调度时间计算。
- `ScheduledTaskRunner`：单个任务的执行与状态更新。
- `ScheduledTaskActionExecutor`：动作分发与实际调用 CQ API。
- `ScheduledTaskDefinition`：任务定义、动作定义、调度定义和 JSON 配置选项。
- `ScheduledTasksOptions`：运行时目录和匹配规则配置。

运行时任务文件默认不放在本目录，而是放在 `data/scheduled-tasks/`。这样可以避免把源码里的文档、示例或其他 JSON 文件误识别为任务。
