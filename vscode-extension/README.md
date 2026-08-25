# WayCoder (道码) VS Code 扩展

把 [WayCoder](https://github.com/alecksty/waycoder) 学习型智能体带到 VS Code：流式对话、解释/修复选中代码。基于 WayCoder 的 `--web` SSE 协议（本地 127.0.0.1，不经过云端代理）。

## 安装

### 方式一：打包 .vsix（推荐）
```bash
cd vscode-extension
npm install
npm run package          # 生成 waycoder-vscode-0.1.0.vsix
# VS Code → 扩展 → ... → 从 VSIX 安装
```

### 方式二：开发模式
```bash
cd vscode-extension
npm install
code .                   # 按 F5 启动 Extension Development Host
```

**前置**：WayCoder 可执行文件 `waycoder` 在 PATH 中（winget/brew/直接下载均可，见 [docs/安装与升级.md](../docs/安装与升级.md)）。

## 使用

| 命令 | 快捷键 | 作用 |
|---|---|---|
| `WayCoder: 打开对话` | Ctrl+Alt+W | 打开聊天面板（流式对话） |
| `WayCoder: 解释选中代码` | — | 把选中的代码作为「解释」发送 |
| `WayCoder: 修复选中代码` | — | 把选中的代码作为「修复」发送 |
| `WayCoder: 中断当前任务` | — | 中断（聊天面板内也有按钮） |

聊天面板支持流式 token 渲染、工具调用提示、失败/中断反馈。

## 配置

| 配置 | 默认 | 说明 |
|---|---|---|
| `waycoder.path` | `waycoder` | waycoder 可执行文件路径（可设绝对路径） |
| `waycoder.port` | `0` | 本地服务端口（0=自动选空闲端口） |
| `waycoder.model` | 空 | 模型 ID（留空用 WayCoder 默认配置；经 `WAYCODER_MODEL` 传给子进程） |

## 工作原理

1. 首次打开对话时，扩展在**工作区根目录**启动 `waycoder --web <空闲端口>`（`WAYCODER_WEB_NO_OPEN=1`，不弹浏览器）。
2. 聊天面板 → 扩展 → `POST /chat`（纯文本）；扩展消费 `GET /events?client=<id>` 的 SSE 事件（token/tool/tool_output/done/failed/interrupted）逐事件转发到面板。
3. 关闭面板/停用扩展时 kill 子进程。
4. 若 `--web` 启动失败，会提示确认 `waycoder` 已安装（可配置 `waycoder.path`）。

## 协议参考

- 服务仅绑定 `127.0.0.1`（本地回环）。
- SSE 端点 `GET /events`；`POST /interrupt` 取消当前任务。
- 一次性回退：`waycoder --json -p "<prompt>"`（单 JSON 对象）。
