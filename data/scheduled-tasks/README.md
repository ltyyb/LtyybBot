# 定时任务编写指南

本目录用于存放运行时定时任务文件。

默认约定：

- 每个任务使用一个独立文件。
- 默认仅扫描 `*.task.json` 文件。
- 每个任务执行状态会写入同目录下的 `*.state.json` 文件。
- 新建任务时建议先写 `enabled: false`，确认无误后再启用。

## 一、任务文件整体结构

任务文件由以下部分组成：

- `id`：任务唯一标识，建议全局唯一。
- `description`：可选描述。
- `enabled`：是否启用。
- `timeZone`：调度使用的时区。
- `schedule`：调度规则。
- `actions`：动作列表，按顺序执行。

最小可用示例：

```json
{
  "id": "sample-task",
  "enabled": false,
  "timeZone": "Asia/Shanghai",
  "schedule": {
    "type": "interval",
    "intervalSeconds": 300
  },
  "actions": [
    {
      "type": "send_group_message",
      "groupId": 123456789,
      "message": "测试消息"
    }
  ]
}
```

## 二、schedule 写法

当前支持 3 种调度方式。

### 1. interval

按固定秒数反复执行。

```json
"schedule": {
  "type": "interval",
  "intervalSeconds": 600
}
```

说明：

- `intervalSeconds`：间隔秒数，必须大于 0。

### 2. once

在指定 UTC 时间执行一次。

```json
"schedule": {
  "type": "once",
  "runAtUtc": "2026-06-01T12:00:00Z"
}
```

说明：

- `runAtUtc`：UTC 时间，建议使用 `Z` 结尾。
- 一旦执行成功，该任务之后不会再次执行。

### 3. daily

每天在指定时区的固定时间执行。

```json
"schedule": {
  "type": "daily",
  "dailyTimes": [ "09:00", "18:30" ]
}
```

说明：

- `dailyTimes`：时间列表，格式为 `HH:mm`。
- 此模式请明确写 `timeZone`，避免部署环境时区变化导致偏移。

## 三、actions 写法

当前支持 3 种动作。

### 1. send_group_message

发送群消息。

```json
{
  "type": "send_group_message",
  "groupId": 123456789,
  "message": "大家早上好"
}
```

字段：

- `groupId`：目标群号。
- `message`：消息内容。

### 2. send_private_message

发送私聊消息。

```json
{
  "type": "send_private_message",
  "userId": 123456789,
  "message": "这是一条私聊提醒"
}
```

字段：

- `userId`：目标 QQ 号。
- `message`：消息内容。

### 3. call_api

直接调用 `Sisters.WudiLib.HttpApiClient.CallAsync(action, data)`。

```json
{
  "type": "call_api",
  "apiAction": "set_group_whole_ban",
  "apiParams": {
    "group_id": 123456789,
    "enable": true
  }
}
```

字段：

- `apiAction`：OneBot 动作名。
- `apiParams`：原样传入的参数对象。

## 四、完整示例

### 示例 1：每天定时发群消息

```json
{
  "id": "daily-group-greeting",
  "description": "每天固定时间向群里发送问候",
  "enabled": true,
  "timeZone": "Asia/Shanghai",
  "schedule": {
    "type": "daily",
    "dailyTimes": [ "09:00", "18:00" ]
  },
  "actions": [
    {
      "type": "send_group_message",
      "groupId": 123456789,
      "message": "定时问候：大家好。"
    }
  ]
}
```

### 示例 2：一次执行多个动作

```json
{
  "id": "release-notice",
  "enabled": true,
  "timeZone": "Asia/Shanghai",
  "schedule": {
    "type": "once",
    "runAtUtc": "2026-06-01T12:00:00Z"
  },
  "actions": [
    {
      "type": "send_group_message",
      "groupId": 123456789,
      "message": "版本已发布。"
    },
    {
      "type": "send_private_message",
      "userId": 987654321,
      "message": "版本发布提醒已发送。"
    }
  ]
}
```

### 示例 3：定时调用通用 API

```json
{
  "id": "mute-group-at-night",
  "enabled": false,
  "timeZone": "Asia/Shanghai",
  "schedule": {
    "type": "daily",
    "dailyTimes": [ "23:00" ]
  },
  "actions": [
    {
      "type": "call_api",
      "apiAction": "set_group_whole_ban",
      "apiParams": {
        "group_id": 123456789,
        "enable": true
      }
    }
  ]
}
```

## 五、状态文件说明

运行过程中会自动生成状态文件，例如：

- 任务文件：`daily-group-greeting.task.json`
- 状态文件：`daily-group-greeting.task.json.state.json`

状态文件一般包含：

- `lastAttemptUtc`
- `lastSuccessUtc`
- `lastError`
- `consecutiveFailures`

通常不建议手动编辑状态文件。若要重置某个任务的执行状态，删除其对应 `.state.json` 文件即可。

## 六、推荐命名方式

- 文件名建议：`daily-report.task.json`
- `id` 建议与文件名语义一致。
- 任务描述建议写清楚用途和目标对象，方便后续排查日志。

## 七、注意事项

- 若 `CQApiService` 尚未连上 OneBot，消息类动作会执行失败，并把错误写入状态文件。
- `once` 任务只有在执行成功后才会停止再次执行；如果持续失败，会继续尝试。
- `daily` 任务是按任务自己的 `timeZone` 计算，而不是机器当前时区。
- 如果后续要扩展新动作类型，建议直接在 `ScheduledTaskActionExecutor` 中增加新的 `type` 分支，并同步更新本说明。
