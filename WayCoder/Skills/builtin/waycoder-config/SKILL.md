---
name: waycoder-config
description: 当用户需要配置 WayCoder 时使用 — 包括模型设置、API Key、MCP 服务器、LSP、技能管理、权限规则、沙箱配置等。
license: MIT
compatibility: waycoder>=0.31.0
metadata:
  category: configuration
  builtin: true
---

# WayCoder 配置指南

WayCoder 通过环境变量和配置文件进行配置。

## 配置文件层次

1. `.waycoder/config.json`（项目本地）
2. `~/.waycoder/config.json`（全局）
3. 环境变量（`WAYCODER_*` / `CORECODER_*`，优先级最高）

## 核心配置

### 模型与 API

| 环境变量 | 说明 |
|----------|------|
| `WAYCODER_BASE_URL` | API 基础地址 |
| `WAYCODER_API_KEY` | API 密钥 |
| `WAYCODER_MODEL` | 大模型名称 |
| `WAYCODER_SMALL_MODEL` | 小模型名称（摘要/压缩用） |
| `WAYCODER_FALLBACK_CHAIN` | 回退链（逗号分隔模型列表） |
| `WAYCODER_LLM_HTTP_TIMEOUT_SEC` | HTTP 超时（默认 600） |
| `WAYCODER_LLM_MAX_RETRIES` | 最大重试次数（默认 3） |

### Agent 行为

| 环境变量 | 说明 |
|----------|------|
| `WAYCODER_MAX_ROUNDS` | 最大轮次（默认 50） |
| `WAYCODER_SUB_AGENT_MAX_PARALLEL` | 子 Agent 最大并行数（默认 4） |
| `WAYCODER_MAX_BUDGET_USD` | 金额上限（美元） |
| `WAYCODER_TOOL_TIMEOUT_SEC` | 工具超时（默认 120） |
| `WAYCODER_ALLOWED_TOOLS` | 工具白名单（逗号分隔，空=全部允许） |
| `WAYCODER_DISABLED_TOOLS` | 工具黑名单（逗号分隔，空=不禁用） |

### 沙箱

| 环境变量 | 说明 |
|----------|------|
| `WAYCODER_SANDBOX_MAX_MEMORY_MB` | 最大内存（MB） |
| `WAYCODER_SANDBOX_MAX_CPU_SECONDS` | 最大 CPU 时间（秒） |
| `WAYCODER_SANDBOX_ALLOW_NETWORK` | 允许网络（默认 false） |

### 上下文压缩

| 环境变量 | 说明 |
|----------|------|
| `WAYCODER_CONTEXT_SNIP_RATIO` | 裁剪比例（默认 0.5） |
| `WAYCODER_CONTEXT_SUMMARIZE_RATIO` | 摘要比例（默认 0.7） |
| `WAYCODER_CONTEXT_COLLAPSE_RATIO` | 折叠比例（默认 0.9） |

### 文件与编辑器

| 环境变量 | 说明 |
|----------|------|
| `WAYCODER_DIFF_PREVIEW` | Diff 预览开关（0/1） |
| `WAYCODER_EDITOR_LINT` | 编辑器 Lint（0/1，默认 1） |
| `WAYCODER_ENABLE_NOTIFICATIONS` | 桌面通知（0/1，默认 0） |
| `WAYCODER_FILE_LOCK_TIMEOUT_SEC` | 文件锁超时（默认 30） |
| `WAYCODER_BASH_OUTPUT_MAX_CHARS` | Bash 输出最大字符（默认 50000） |

## 技能管理

技能存放在以下目录中（按优先级）：
1. `.waycoder/skills/`（项目本地，优先）
2. `.corecoder/skills/`（旧目录，兼容）
3. `.claude/skills/`（行业标准）
4. `.cursor/skills/`（Cursor 兼容）

每个技能是一个包含 `SKILL.md` 文件的子目录，格式为：

```markdown
---
name: my-skill
description: 技能简要描述
---

# 技能详细内容
...
```

## 权限模式

通过 `/perm` 命令切换：
- `Ask`：每次确认（默认）
- `Auto`：首次确认后记住
- `SmartAuto`：智能分级（Safe 放行/Cautious 记一次/Dangerous 每次确认）
- `Yolo`：不确认

## MCP 配置

MCP 服务器通过 `~/.waycoder/mcp_servers.json` 配置：

```json
{
  "mcpServers": {
    "filesystem": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-filesystem", "."]
    }
  }
}
```

工具名称格式：`mcp_<server>_<tool>`

## 常用工作流

- 快速问答：`waycoder -p "问题"`
- Watch 模式：`waycoder --watch`（在文件中写 `AI! 指令`）
- 自测：`waycoder --test`
- 切换模型：`/model set large deepseek-v4-pro`
- 模型列表：`/model list`
- 查看配置：`/config`
- 设置项：`/config set KEY VALUE`
