# CoreCoder — 智能体指南

**CoreCoder** 是一个极简的开源 AI 编程智能体（引擎约 1,081 行代码），灵感来自 Claude Code。支持任何兼容 OpenAI 的 LLM 或 LiteLLM。

## 快速开始

```bash
pip install -e .                    # 可编辑安装
pip install -e ".[dev,litellm]"     # 包含开发依赖 + LiteLLM
corecoder                           # 交互式 REPL
corecoder -p "修复 foo.py 中的 bug"  # 一次性模式
corecoder -r <session_id>           # 恢复已保存的会话
```

## 常用命令

| 命令 | 用途 |
|---|---|
| `pytest` | 运行全部 86 个测试（无需配置，自动发现 tests/） |
| `pytest tests/test_tools.py -v` | 运行指定测试文件 |
| `ruff check .` | 代码检查（line-length=130，目标 py310） |
| `corecoder` | 启动交互式 REPL |
| `corecoder -p "提示词"` | 一次性模式（非交互式） |

## 应用架构

```
cli.py          → argparse + REPL 循环（prompt_toolkit, Rich）
agent.py        → 主循环：用户消息 → LLM → 工具调用 → 执行 → 循环
llm.py          → LLM 提供层（默认 OpenAI SDK，LiteLLM 备选）
context.py      → 3 层上下文压缩（裁剪 → 摘要 → 硬折叠）
session.py      → JSON 持久化（保存/加载/列出会话）
prompt.py       → 系统提示词生成（注入 OS、cwd、工具列表）
config.py       → 基于环境变量的配置（.env 加载、API 密钥解析）
tools/          → 7 个工具实现
```

### 控制流程

```
用户输入 → Agent.chat() → 追加到消息列表 → maybe_compress()
  → LLM.chat() 流式响应
    → 文本回复？ → 返回给用户，完成
    → 工具调用？ → 执行（多个并行，单个串行）
      → 追加工具结果 → maybe_compress() → 循环回 LLM
  → 最多 50 轮 → "(reached maximum tool-call rounds)"
```

### 工具系统

7 个工具，全部位于 `corecoder/tools/`：

| 工具 | 文件 | 用途 |
|---|---|---|
| `read_file` | `read.py` | 读取文件，显示行号、偏移量、限制行数 |
| `write_file` | `write.py` | 创建/覆盖文件（自动创建目录） |
| `edit_file` | `edit.py` | **首选** — 精确匹配查找替换，输出 diff |
| `bash` | `bash.py` | 执行 Shell 命令，跟踪 cwd，检测危险命令 |
| `glob` | `glob_tool.py` | 文件模式匹配（glob），按修改时间排序 |
| `grep` | `grep.py` | 正则表达式内容搜索，跳过垃圾目录 |
| `agent` | `agent.py` | 生成子智能体（独立上下文，禁止递归） |

## 非显而易见的陷阱与模式

### edit_file 优先于 write_file
系统提示词告诉 LLM 优先使用 `edit_file`（精确匹配查找替换）进行针对性编辑。`write_file` 仅用于新建文件或完全重写。编辑工具会生成 unified diff，并验证 `old_string` 在文件中恰好出现一次。

### 工具调用并行执行
当 LLM 返回多个工具调用时，它们通过 `ThreadPoolExecutor(max_workers=8)` 并发执行。每个工具调用的 `execute()` 在自己的线程中运行。单个工具调用则同步执行。

### Bash cwd 是线程本地的
bash 工具通过 `threading.local()` 按线程跟踪 `cd` 命令。每个线程维护自己的工作目录，这样并行的 bash 调用不会在共享全局变量上产生竞态。这是唯一具有线程本地状态的工具。

### 危险 bash 命令被阻止
bash 工具在执行前会检查 `_DANGEROUS_PATTERNS`：`rm -rf /`、`rm -fr /`、fork 炸弹、`curl|sh`、`wget|sh`、`mkfs`、`dd of=/dev/`、`chmod 777 /`。会返回清晰的错误信息而非执行。

### 上下文压缩有 3 层
- **第 1 层（50% 阈值）**：将冗长的工具输出裁剪为首尾各 3 行
- **第 2 层（70% 阈值）**：LLM 驱动的旧对话摘要（保留最近 8 条）
- **第 3 层（90% 阈值）**：硬折叠 — 仅保留最后 4 条消息 + 摘要

### 孤立的工具消息是关键的不可变约定
`_safe_split()` 方法确保压缩永远不会将 `tool` 角色的消息与产生它的包含 `tool_calls` 的 `assistant` 消息分离。OpenAI 兼容的 API 会拒绝包含孤立工具消息的请求。相同的约定也适用于中断处理 — `_answer_pending_tool_calls()` 在 Ctrl+C 中断执行时回填缺失的工具回复。

### API 密钥解析顺序
`CORECODER_API_KEY` > `OPENAI_API_KEY` > `DEEPSEEK_API_KEY`。`.env` 文件从当前目录或任意父目录（直到 home 目录）加载。

### LLM 提供者选择
- **默认**：使用 `openai` SDK（适用于任何兼容 OpenAI 的端点：DeepSeek、Qwen、Kimi、Ollama 等）
- **LiteLLM**：设置 `CORECODER_PROVIDER=litellm` 并 `pip install corecoder[litellm]` — 用于不兼容 OpenAI 的提供者（AWS Bedrock、Google Vertex 等）

### LiteLLM 模型字符串格式
使用 LiteLLM 后端时，模型字符串格式为 `provider/model` — 例如 `anthropic/claude-3-haiku`、`bedrock/anthropic.claude-v2`、`vertex_ai/gemini-pro`。

### 成本估算因模型而异
`llm.py:55-81` 中的定价表覆盖约 20 个模型（OpenAI、DeepSeek、Claude、Qwen、Kimi）。未知模型返回 `None`。数值为每百万令牌的价格（输入，输出）。

### 会话 ID 被净化处理
会话 ID 中的路径遍历尝试（`../../etc/passwd`、`\..\..\secret`）会被中和 — 仅提取文件名部分，非字母数字字符替换为 `-`。长度限制为 100 个字符。

### 子智能体没有递归的 agent 工具
生成子智能体时，`agent` 工具会从子智能体的工具列表中移除，以防止无限递归。子智能体的轮次上限为 20 轮（主智能体为 50 轮）。

### /diff 跨会话跟踪文件
`corecoder/tools/edit.py` 中的 `_changed_files` 集合跟踪所有通过 `edit_file` 和 `write_file` 修改的文件。它是模块级全局变量（非每个智能体实例），因此 `/diff` 显示当前进程中所有修改过的文件。

## 测试约定

- 测试使用 `pytest`，配合 `tmp_path` 和 `monkeypatch` 夹具
- 依赖 LLM 的测试使用 `_Chunk`/`_Delta`/`_Usage` 辅助类模拟流式响应
- 会话测试将 `SESSIONS_DIR` monkeypatch 到 `tmp_path`，避免触碰真实文件
- Bash 测试使用 `sys.executable` 实现跨平台子进程测试
- 所有工具通过 `get_tool("name")` 从注册表中查找并测试
- `test_compress_never_leaves_an_orphan_tool_reply` 是最重要的不变性检查

## 环境变量

| 变量 | 默认值 | 用途 |
|---|---|---|
| `CORECODER_MODEL` | `gpt-5.5` | 模型名称 |
| `CORECODER_API_KEY` | — | API 密钥（优先检查） |
| `OPENAI_API_KEY` | — | API 密钥（备选） |
| `DEEPSEEK_API_KEY` | — | API 密钥（备选） |
| `OPENAI_BASE_URL` | — | OpenAI 兼容 API 的基础 URL |
| `CORECODER_BASE_URL` | — | 基础 URL（备选） |
| `CORECODER_MAX_TOKENS` | `4096` | 每次响应的最大令牌数 |
| `CORECODER_TEMPERATURE` | `0` | 温度参数（0 = 确定性的） |
| `CORECODER_MAX_CONTEXT` | `128000` | 上下文窗口大小 |
| `CORECODER_PROVIDER` | `openai` | 提供者：`openai` 或 `litellm` |

## CLI 参数

```
corecoder [-m MODEL] [--base-url URL] [--api-key KEY]
          [-p PROMPT] [-r SESSION_ID] [-v]
```

## REPL 命令

| 命令 | 操作 |
|---|---|
| `/help` | 显示帮助 |
| `/reset` | 清除对话历史 |
| `/model` | 显示当前模型 |
| `/model <name>` | 在对话中切换模型 |
| `/tokens` | 显示令牌使用量 + 成本估算 |
| `/compact` | 强制压缩上下文 |
| `/diff` | 显示本次会话修改的文件 |
| `/save` | 将会话保存到磁盘 |
| `/sessions` | 列出已保存的会话 |
| `quit` | 退出 |