<div align="center">

# WayCoder（道码）

**中文版易用编程智能体，C# (.NET 10) 实现，AOT 编译为单文件 exe（~8 MB），无需运行时。**

*一个 while 循环 + 大模型 + 29 个工具 + Watch 模式，就是全部*

[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![AOT](https://img.shields.io/badge/AOT-native-blue)](https://learn.microsoft.com/dotnet/core/deploying/native-aot)

</div>

## 改名说明

本项目原名 **CoreCoder**，因发现与现有商标/产品名称冲突，为规避侵权风险，自 v0.16.3 起更名为 **WayCoder（道码）**。

- 代码命名空间暂保留 `CoreCoderSharp`（内部不影响用户）
- 可执行文件：`corecoder.exe` → `waycoder.exe`
- 环境变量前缀：`CORECODER_*` → `WAYCODER_*`（旧名仍兼容）
- 目录名：仓库内部目录暂不重命名，不影响安装使用

## 这是什么

WayCoder（道码）是一个中文版易用编程智能体。把 Claude Code、Cursor 这类工具扒到底，核心是一个 while 循环套着一个大模型，外加二十几个让它能真正动手的工具。WayCoder 就是把这个核心老老实实写出来的开箱即用版本。

C# 版完整移植了 Python 原版的全部功能，并新增了权限确认、Git 集成、Web 抓取、LSP 代码导航、记忆系统、后台任务、代码审查、Watch 模式等多项功能。AOT 编译为单文件 exe，拷贝到任何 Windows 机器上直接运行，无需安装 .NET 运行时。

## 先跑一次

```bash
# 克隆仓库
git clone https://github.com/he-yufeng/WayCoder
cd WayCoder/CoreCoderSharp

# AOT 编译（生成单文件 waycoder.exe）
dotnet publish -c Release

# 或直接运行
dotnet run

# 一次性模式
dotnet run -- -p "修复一个 bug"

# Watch 模式 (监听 AI! 注释自动触发 Agent)
dotnet run -- --watch

# 运行自测（380 项）
dotnet run -- --test
```

给它一个模型加一把 key 就能动。默认走 OpenAI 兼容接口，在项目根目录扔个 `.env`，启动时自动加载：

| Provider | 环境变量示例 |
|---|---|
| DeepSeek（默认 `deepseek-v4-flash`） | `WAYCODER_API_KEY=sk-...` |
| OpenAI | `WAYCODER_MODEL=gpt-5.5` `WAYCODER_API_KEY=sk-...` |
| 本地 Ollama | `OPENAI_BASE_URL=http://localhost:11434/v1` `WAYCODER_MODEL=qwen2.5-coder` |

> 💡 兼容旧名：`CORECODER_*` 环境变量仍可正常使用，建议逐步迁移至新前缀。

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
├── WatchMode.cs        Watch 模式 (文件监听 + AI! 注释)
├── PermissionManager.cs 权限确认系统
├── ProjectContext.cs  项目检测 + CLAUDE.md 加载
├── ReviewMode.cs      代码审查模式
├── FallbackLLM.cs     模型回退链
├── MemoryStore.cs     记忆系统
├── BackgroundTask.cs  后台任务
├── DebugLog.cs        调试日志
├── SelfTest.cs        380 项自测
└── Tools/             29 个工具
    ├── BashTool.cs    GitTool.cs    LspTool.cs
    ├── ReadFileTool.cs FetchTool.cs MemoryTool.cs
    ├── WriteFileTool.cs TodoTool.cs  LintTool.cs
    ├── EditFileTool.cs AgentTool.cs  WebSearchTool.cs
    ├── GlobTool.cs    GrepTool.cs    GitPRTool.cs
    ├── PsTool.cs      KillTool.cs    LsTool.cs
    ├── MkdirTool.cs   RmTool.cs      CdTool.cs
    ├── FindReplaceTool.cs CpTool.cs  MvTool.cs
    ├── DiffTool.cs    TreeTool.cs    WcTool.cs
    ├── StatTool.cs    PwdTool.cs
```

## 29 个工具

| 工具 | 用途 |
|---|---|
| `bash` | 执行 Shell 命令，跟踪 cwd，检测危险命令 |
| `read_file` | 读取文件，显示行号、偏移量、限制行数 |
| `write_file` | 创建/覆盖文件（自动创建目录） |
| `edit_file` | 精确匹配查找替换，输出 diff |
| `glob` | 文件模式匹配，按修改时间排序 |
| `grep` | 正则表达式内容搜索，跳过垃圾目录 |
| `agent` | 生成子智能体（独立上下文，禁止递归） |
| `git` | Git 操作（status/log/diff/commit） |
| `fetch` | Web 抓取，自动提取网页纯文本 |
| `lsp` | LSP 代码导航（go-to-def, references, hover） |
| `memory` | 读写项目记忆 |
| `todo` | 结构化任务列表 |
| `lint` | 代码静态检查，25+ 种语言 |
| `web_search` | 网页搜索（通过 DuckDuckGo） |
| `git_pr` | Git PR 创建/推送/链接 |
| `ps` | 进程列表（纯 C#） |
| `kill` | 终止进程，保护系统进程 |
| `ls` | 目录列表，递归/过滤/深度 |
| `mkdir` | 递归创建目录 |
| `rm` | 安全删除文件/目录 |
| `cd` | 切换工作目录 |
| `cp` | 复制文件/目录 |
| `mv` | 移动/重命名文件 |
| `diff` | 逐行文件差异比对 |
| `tree` | ASCII 目录树生成 |
| `wc` | 行/词/字符/字节计数 |
| `stat` | 文件/目录元数据 |
| `find_replace` | 跨文件正则查找替换 |
| `pwd` | 打印工作目录 |

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
/watch           切换 Watch 模式
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
- **Watch 模式**：文件监听 + AI! 注释解析 → 线程安全队列 → REPL 自动执行

## 贡献 / License

MIT License，欢迎 fork 拿去造更好的东西。

---

作者 [何宇峰](https://github.com/he-yufeng)，曾任职 Moonshot AI (Kimi)。

**深圳市探索智能科技有限公司**
