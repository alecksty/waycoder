<div align="center">

# WayCoder（道码）

**中文版易用编程智能体，C# (.NET 10) 实现，AOT 编译为单文件 exe（~8 MB），无需运行时。**

*一个 while 循环 + 大模型 + 31 个工具 + Watch 模式，就是全部*

[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![AOT](https://img.shields.io/badge/AOT-native-blue)](https://learn.microsoft.com/dotnet/core/deploying/native-aot)

</div>

> 📖 **使用手册**：[docs/使用手册.md](docs/使用手册.md) — 快速上手、命令速查、快捷键、配置、Watch 模式、FAQ

## 改名说明

本项目原名 **CoreCoder**，因发现与现有商标/产品名称冲突，为规避侵权风险，自 v0.16.3 起更名为 **WayCoder（道码）**。

- 代码命名空间已重命名为 `WayCoder`
- 可执行文件：`corecoder.exe` → `waycoder.exe`
- 环境变量前缀：`CORECODER_*` → `WAYCODER_*`（旧名仍兼容）
- 目录名：仓库内部目录已同步重命名

## 这是什么

WayCoder（道码）是一个中文版易用编程智能体。把 Claude Code、Cursor 这类工具扒到底，核心是一个 while 循环套着一个大模型，外加二十几个让它能真正动手的工具。WayCoder 就是把这个核心老老实实写出来的开箱即用版本。

C# 版完整移植了 Python 原版的全部功能，并新增了权限确认、Git 集成、Web 抓取、LSP 代码导航、记忆系统、后台任务、代码审查、Watch 模式等多项功能。AOT 编译为单文件 exe，拷贝到任何 Windows 机器上直接运行，无需安装 .NET 运行时。

## 先跑一次

```bash
# 克隆仓库
git clone https://github.com/he-yufeng/WayCoder
cd WayCoder

# AOT 编译（生成单文件 waycoder.exe）
dotnet publish -c Release

# 或直接运行
dotnet run

# 一次性模式
dotnet run -- -p "修复一个 bug"

# Watch 模式 (监听 AI! 注释自动触发 Agent)
dotnet run -- --watch

# Tiny 模式（本地小模型 / 省 token；窗口 <128K 自动进入，也可 --tiny 8k 指定）
dotnet run -- --tiny
dotnet run -- --tiny 8k

# 省 Token 模式（保持正常窗口；--economy [on|auto|off]，缺省 on）
dotnet run -- --economy          # 开：精简提示词 + 更早压缩 + 输出上限
dotnet run -- --economy auto     # 自动：按任务复杂度动态调节阈值（简单省、复杂保质量）
# 优先级偏好（仅 auto 生效）：WAYCODER_ECONOMY_PRIORITY=quality|balanced|cost（默认 quality）

# 运行自测（1680 项）
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
WayCoder/
├── Program.cs         入口 + CLI + REPL (ANSI 全屏 TUI)
├── Agent.cs           主循环 (Stop Hook + 10 阶段流水线)
├── AgentSlot.cs       多 Agent 工作区 (F1-F10 槽位切换)
├── LLM.cs             LLM 客户端 (流式 + 渐进超时重试 + 花费追踪)
├── ContextManager.cs  Crush 风格三层上下文压缩 + 进度事件
├── SessionManager.cs  会话持久化
├── SystemPrompt.cs    系统提示词 (15 结构化区块 + 10 阶段流水线)
├── Config.cs          配置 (.env 加载, 67 项全部可配)
├── WatchMode.cs        Watch 模式 (文件监听 + AI! 注释)
├── PermissionManager.cs 权限确认系统
├── ProjectContext.cs  项目检测 + CLAUDE.md 加载
├── ReviewMode.cs      代码审查模式
├── FallbackLLM.cs     模型回退链 (6 模型, 跨供应商 Key 解析)
├── MemoryStore.cs     记忆系统 (旧格式, 迁移源)
├── StructuredMemory.cs 结构化记忆 (frontmatter 多文件 + MEMORY.md 索引)
├── MemoryRetrieval.cs  跨会话记忆检索
├── SkillsManager.cs   技能系统 (SKILL.md 发现与解析)
├── HooksManager.cs    Hook 系统 (8 事件 + JSON 协议)
├── BackgroundTask.cs  后台任务
├── DebugLog.cs        调试日志
├── SelfTest.cs        1680 项自测
├── Infra/             基础设施 (12+ 文件)
│   ├── BashGuard.cs     命令安全防护 (70+ 禁止 + 47 安全白名单)
│   ├── FileTracker.cs   文件追踪 (SHA256 + 变更检测)
│   ├── ErrorLog.cs      统一错误日志
│   ├── IdGenerator.cs   加密安全 ID 生成
│   ├── LruCache.cs      线程安全 LRU 缓存
│   ├── RetryPolicy.cs   智能重试策略
│   ├── SnippetStore.cs  代码片段管理
│   └── Logging/         结构化日志系统 (9 文件)
├── UI/                终端 TUI 控件库 (40+ 文件)
│   ├── TuiCust/           自定义控件 + 对话框
│   │   ├── ToolRenderers/   工具输出渲染器 (7 文件)
│   │   ├── ModelPicker.cs   模型选择对话框
│   │   ├── FilePicker.cs    文件选择对话框
│   │   └── CommandPalette.cs 命令面板
│   ├── TuiControls/       基础控件库
│   │   ├── TuiDynamicBar.cs 动态状态栏 (Agent 状态 + 工具 + 压缩进度)
│   │   ├── TuiMarkdown.cs   Markdown→ANSI 渲染
│   │   ├── TuiListView.cs   懒列表 (二分查找 + 提前终止)
│   │   └── TuiButton.cs     增强按钮 (快捷键下划线/悬停)
│   └── TuiScreens/        全屏界面
└── Tools/             39 个工具
    ├── BashTool.cs    GitTool.cs    LspTool.cs
    ├── ReadFileTool.cs FetchTool.cs MemoryTool.cs
    ├── WriteFileTool.cs TodoTool.cs  LintTool.cs
    ├── EditFileTool.cs AgentTool.cs  WebSearchTool.cs
    ├── GlobTool.cs    GrepTool.cs    GitPRTool.cs
    ├── PsTool.cs      KillTool.cs    LsTool.cs
    ├── MkdirTool.cs   RmTool.cs      CdTool.cs
    ├── FindReplaceTool.cs CpTool.cs  MvTool.cs
    ├── DiffTool.cs    TreeTool.cs    WcTool.cs
    ├── StatTool.cs    PwdTool.cs    SkillTool.cs
    ├── DocTool.cs     ExportTool.cs StructTodoTool.cs
    ├── DownloadTool.cs JobOutputTool.cs JobKillTool.cs
    ├── NotebookEditTool.cs MultiEditTool.cs
    └── AskUserQuestionTool.cs
```

## 39 个工具

| 工具 | 用途 |
|---|---|
| `bash` | 执行 Shell 命令，跟踪 cwd，检测危险命令，支持后台运行 |
| `read_file` | 读取文件，显示行号、偏移量、限制行数，支持 PDF/Markdown |
| `write_file` | 创建/覆盖文件（自动创建目录，diff 预览确认） |
| `edit_file` | 精确匹配查找替换，输出 diff，支持 replace_all |
| `multi_edit` | 批量编辑，一次操作多个替换 |
| `glob` | 文件模式匹配，按修改时间排序，自动过滤忽略文件 |
| `grep` | 正则表达式内容搜索，支持 literal_text，自动过滤忽略文件 |
| `agent` | 生成子智能体（独立上下文，禁止递归；支持 tasks 数组并行） |
| `git` | Git 操作（status/log/diff/commit/branch） |
| `fetch` | Web 抓取，HTML 净化 + Markdown 提取 |
| `lsp` | LSP 代码导航（go-to-def, references, hover, symbols） |
| `memory` | 读写项目记忆（结构化 .waycoder/memory/ 格式，支持 read/write/search/delete/share） |
| `todo` | 结构化任务列表 |
| `struct_todo` | 增强版 Todo（优先级、依赖关系、状态追踪） |
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
| `skill` | 按需加载 SKILL.md 技能完整内容（名称+描述见系统提示词） |
| `doc` | 查最新库/框架文档（搜索+抓取），获取最新 API 和用法 |
| `download` | HTTP GET 下载文件到本地（安全检查，最大 500MB） |
| `notebook_edit` | Jupyter Notebook (.ipynb) 编辑（replace/insert/delete cell） |
| `export` | 对话导出（Markdown / JSON / HTML） |
| `job_output` | 读取后台 bash 任务输出 |
| `job_kill` | 终止后台 bash 任务 |
| `ask_user` | 向用户提问确认（单/多选 + 文本输入） |

## REPL 命令

```
/model <名称>    切换模型 / 打开模型选择器 (Ctrl+M)
/compact         手动压缩上下文
/tokens          查看 token 用量和费用估算 (含任务级花费)
/stats           查看会话统计 (token/PromptCache/LLM指标)
/diff            查看本次会话改过的文件
/review          多维度代码审查
/plan            计划模式，先规划再执行
/jobs            查看后台任务
/git-status /git-log /git-diff  Git 状态/日志/差异
/perm ask|auto|yolo  权限模式切换
/mode build|plan|review|auto  工作模式切换 (Shift+Tab)
/auto            自动模式切换
/watch           切换 Watch 模式
/save  /sessions 保存 / 列出会话 (Ctrl+S)
/export          导出对话历史
quit / exit      退出 (Ctrl+Q)
```

## 多 Agent 工作区

**F1-F10** 一键切换 10 个独立 Agent 槽位，各占各的屏幕（聊天历史、输入草稿、状态栏各自独立），互不干扰。状态栏左侧 10 个数字实时显示各槽位状态：

| 显示 | 含义 |
|---|---|
| 底色白 | 当前显示的屏幕 |
| 灰色 | 空闲 |
| 绿色 | 工作中 |
| 黄色 | 等待权限确认 |
| 红色 | 出错 |

> 热键迁移：帮助 `Ctrl+H`、面板 `Ctrl+B`、设置 `Ctrl+O`、退出 `Ctrl+Q`

## 关键设计决策

- **系统化流水线**：复杂任务自动走 10 阶段（调查→分析→规划→拆分→分工→执行→调试→审核→提交→总结）
- **edit_file 使用唯一子串匹配**，不用行号，安全可审查
- **上下文压缩三层让步**：50% 裁剪 → 70% LLM 摘要 → 90% 硬折叠，实时进度事件
- **渐进超时重试**：LLM 超时逐次加长（1x→1.5x→2x→3x→4x→6x→8x），最多 5 次重试
- **子智能体并行**：tasks 数组最多 4 并发，聚合返回；通过不给 agent 工具约束递归
- **多 Agent 工作区**：F1-F10 切换 10 个独立会话槽位，状态栏实时显示工作状态，支持槽位任务队列
- **Hook 系统**：8 种事件（PreToolUse/PostToolUse/Stop/PreCompact 等），JSON 结构化输出协议
- **动态状态栏**：实时显示 Agent 状态/工具执行/压缩进度，Braille 旋转动画
- **任务级花费追踪**：每轮对话独立统计 prompt/completion tokens 和费用
- **技能系统**：标准 SKILL.md 格式，系统提示词只给名称+描述，`skill` 工具按需加载完整内容
- **Git 自动提交质量校验**：conventional-commit 前缀强制 + 不合格重试一次 + 兜底默认信息
- **Worktree 隔离**：bash 自动检测 worktree 路径并切换 cwd
- **AOT 编译：JSON 手写序列化**，不依赖反射
- **权限系统**：bash/write/edit/agent 默认行内确认（3 行黄色渲染块），`/perm yolo` 跳过
- **双模型架构**：大模型做复杂任务，小模型做压缩/摘要，自动分工省钱
- **模型回退链**：失败自动尝试备选（6 模型链条，跨供应商 API Key 自动解析）
- **Watch 模式**：文件监听 + AI! 注释解析 → 线程安全队列 → REPL 自动执行
- **结构化记忆**：`.waycoder/memory/*.md` frontmatter 多文件 + MEMORY.md 索引，支持跨会话检索
- **Diff 预览**：`WAYCODER_DIFF_PREVIEW=1` 开启写文件前逐 hunk 确认，非交互模式自动跳过
- **Bash 安全防护**：70+ 禁止命令 + 47 安全白名单，管道中每个命令独立检查

## 贡献 / License

MIT License，欢迎 fork 拿去造更好的东西。

---

作者 [何宇峰](https://github.com/he-yufeng)，曾任职 Moonshot AI (Kimi)。

**深圳市探索智能科技有限公司**
