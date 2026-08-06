<div align="center">

# CoreCoder

**极简 AI 编程智能体，C# (.NET 10) 实现，AOT 编译为单文件 exe（7.8 MB），无需运行时。**

*一个 while 循环 + 大模型 + 12 个工具，就是全部*

[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![AOT](https://img.shields.io/badge/AOT-native-blue)](https://learn.microsoft.com/dotnet/core/deploying/native-aot)

</div>

## 这是什么

CoreCoder 是一个极简 AI 编程智能体。把 Claude Code、Cursor 这类工具扒到底，核心是一个 while 循环套着一个大模型，外加十几个让它能真正动手的工具。CoreCoder 就是把这个核心老老实实写出来的最小版本。

C# 版完整移植了 Python 原版的全部功能，并新增了权限确认、Git 集成、Web 抓取、LSP 代码导航、记忆系统、后台任务、代码审查等 12 项功能。AOT 编译为单文件 exe，拷贝到任何 Windows 机器上直接运行，无需安装 .NET 运行时。

## 先跑一次

```bash
# 克隆仓库
git clone https://github.com/he-yufeng/CoreCoder
cd CoreCoder/CoreCoderSharp

# AOT 编译（生成单文件 exe）
dotnet publish -c Release

# 或直接运行
dotnet run

# 一次性模式
dotnet run -- -p "修复一个 bug"

# 运行自测（44 项）
dotnet run -- --test
```

给它一个模型加一把 key 就能动。默认走 OpenAI 兼容接口，在项目根目录扔个 `.env`，启动时自动加载：

| Provider | 环境变量示例 |
|---|---|
| DeepSeek（默认 `deepseek-v4-flash`） | `CORECODER_API_KEY=sk-...` |
| OpenAI | `CORECODER_MODEL=gpt-5.5` `CORECODER_API_KEY=sk-...` |
| 本地 Ollama | `OPENAI_BASE_URL=http://localhost:11434/v1` `CORECODER_MODEL=qwen2.5-coder` |

## 架构

```
CoreCoderSharp/
├── Program.cs         入口 + CLI + REPL (彩色 TUI)
├── Agent.cs           主循环
├── LLM.cs             LLM 客户端 (流式 + 回退)
├── ContextManager.cs  三层上下文压缩
├── SessionManager.cs  会话持久化
├── SystemPrompt.cs    系统提示词 (含项目检测)
├── Config.cs          配置 (.env 加载)
├── PermissionManager.cs 权限确认系统
├── ProjectContext.cs  项目检测 + CLAUDE.md 加载
├── ReviewMode.cs      代码审查模式
├── FallbackLLM.cs     模型回退链
├── MemoryStore.cs     记忆系统
├── BackgroundTask.cs  后台任务
├── DebugLog.cs        调试日志
├── SelfTest.cs        44 项自测
└── Tools/             12 个工具
    ├── BashTool.cs    GitTool.cs    LspTool.cs
    ├── ReadFileTool.cs FetchTool.cs MemoryTool.cs
    ├── WriteFileTool.cs TodoTool.cs
    ├── EditFileTool.cs AgentTool.cs
    ├── GlobTool.cs    GrepTool.cs
```

## 12 个工具

| 工具 | 用途 |
|---|---|
| `bash` | 执行 Shell 命令，跟踪 cwd，检测危险命令 |
| `read_file` | 读取文件，显示行号、偏移量、限制行数 |
| `write_file` | 创建/覆盖文件（自动创建目录） |
| `edit_file` | 精确匹配查找替换，输出 diff |
| `glob` | 文件模式匹配，按修改时间排序 |
| `grep` | 正则表达式内容搜索，跳过垃圾目录 |
| `agent` | 生成子智能体（独立上下文，禁止递归） |
| `git` | Git 操作（status/log/diff） |
| `fetch` | Web 抓取，自动提取网页纯文本 |
| `lsp` | LSP 代码导航（go-to-def, references, hover） |
| `memory` | 读写项目记忆 |
| `todo` | 结构化任务列表 |

## REPL 命令

```
/model <名称>    切换模型
/compact         手动压缩上下文
/tokens          查看 token 用量和费用估算
/diff            查看本次会话改过的文件
/review          多维度代码审查
/plan            计划模式，先规划再执行
/jobs            查看后台任务
/git-status      Git 状态
/git-log         Git 日志
/git-diff        Git 差异
/perm ask|auto|yolo  权限模式切换
/save  /sessions 保存 / 列出会话
quit / exit      退出
```

## 关键设计决策

- **edit_file 使用唯一子串匹配**，不用行号，安全可审查
- **上下文压缩三层让步**：50% 裁剪 → 70% LLM 摘要 → 90% 硬折叠
- **子智能体通过不给 agent 工具来约束**，不靠规则
- **AOT 编译：JSON 手写序列化**，不依赖反射
- **权限系统**：bash/write/edit/agent 默认需确认，`/perm yolo` 跳过
- **模型回退链**：失败时自动尝试备用模型

## 贡献 / License

MIT License，欢迎 fork 拿去造更好的东西。

---

作者 [何宇峰](https://github.com/he-yufeng)，曾任职 Moonshot AI (Kimi)。
