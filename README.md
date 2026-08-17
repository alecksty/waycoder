<div align="center">

# WayCoder（道码）

**中文编程智能体,Vibe Coding Agent CLI**

*支持多模型 + 44个工具 + Watch 模式 + 单文件 + 多智能体*

[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![AOT](https://img.shields.io/badge/AOT-native-blue)](https://learn.microsoft.com/dotnet/core/deploying/native-aot)

</div>

> 📖 **使用手册**：[docs/使用手册.md](docs/使用手册.md) — 快速上手、命令速查、快捷键、配置、Watch 模式、FAQ
> ⬆️ **安装与升级**：[docs/安装与升级.md](docs/安装与升级.md) — 直接下载 / winget / brew / apt + 内置自动升级
> 🔌 **插件系统**：[docs/插件系统.md](docs/插件系统.md) — 编译期 C# 插件，贡献工具与斜杠命令

## 改名说明

本项目源自 **CoreCoder**，因与现有商标/产品名称冲突，为规避侵权风险，自v0.16.3 起更名为 **WayCoder（道码）**。

- 代码命名空间已重命名为 `WayCoder`
- 可执行文件：`corecoder.exe` → `waycoder.exe`
- 环境变量前缀：`CORECODER_*` → `WAYCODER_*`
- 目录名：仓库内部目录已同步重命名

## 这是什么

WayCoder（道码）是一个中文版多智能体经济型编程智能体。把 Claude Code、OpenCode、Crush、Codex、Cursor 这类工具吸取各家特长有点，综合制作的本智能体软件。本代码完全使用 C# .NET10 Native AOT 构建，包含了权限确认、Git 集成、Web 抓取、LSP 代码导航、记忆系统、后台任务、代码审查、Watch 模式等多项功能。拷贝到任何 Windows 机器上直接运行，无需安装 .NET 运行时。

## 先跑一次

```bash
# Watch 模式 (监听 AI! 注释自动触发 Agent)
WayCoder --watch

# Tiny 模式（本地小模型 / 省 token；窗口 <128K 自动进入，也可 --tiny 8k 指定）
WayCoder --tiny
WayCoder --tiny 8k

# 省 Token 模式（保持正常窗口；--economy [on|auto|off]，缺省 on）
WayCoder --economy          # 开：精简提示词 + 更早压缩 + 输出上限
WayCoder --economy auto     # 自动：按任务复杂度动态调节阈值（简单省、复杂保质量）
# 优先级偏好（仅 auto 生效）：WAYCODER_ECONOMY_PRIORITY=quality|balanced|cost（默认 quality）

# 自动升级（检查并自替换，优先 Gitee、回退 GitHub）
WayCoder --update

# 批量任务引擎（多仓库并行处理，worktree 隔离）
WayCoder --batch batch.json                       # 从 JSON 清单读任务
WayCoder --batch-repo https://x/r1 --batch-repo https://x/r2 --batch-task "修复登录 bug"

# JSON 输出模式（IDE / 脚本桥接，stdout 只输出一个结构化 JSON 对象）
WayCoder --json -p "修复一个 bug"

# 浏览器 Web UI（三栏：会话记录 + 聊天 + 信息面板；Markdown 渲染 + 权限模式切换）
WayCoder --web          # 默认端口 8123
WayCoder --web 9000     # 指定端口

# 运行自测（3070 项）
WayCoder  --test
```

给它一个模型加一把 key 就能动。默认走 OpenAI 兼容接口，在项目根目录扔个 `.env`，启动时自动加载：

| Provider | 环境变量示例 |
|---|---|
| DeepSeek（默认 `deepseek-v4-flash`） | `WAYCODER_API_KEY=sk-...` |
| OpenAI | `WAYCODER_MODEL=gpt-5.5` `WAYCODER_API_KEY=sk-...` |
| 本地 Ollama | `OPENAI_BASE_URL=http://localhost:11434/v1` `WAYCODER_MODEL=qwen2.5-coder` |

## 架构

```
WayCoder/
├── Program.cs         入口 + CLI + REPL (ANSI 全屏 TUI)
├── Agent.cs           主循环 (Stop Hook + 10 阶段流水线)
├── AgentSlot.cs       多 Agent 工作区 (F1-F10 槽位切换 + 后台并行)
├── LLM.cs             LLM 客户端 (流式 + 渐进超时重试 + 花费追踪)
├── ContextManager.cs  Crush 风格三层上下文压缩 + 进度事件
├── SessionManager.cs  会话持久化
├── SystemPrompt.cs    系统提示词 (15 结构化区块 + 10 阶段流水线)
├── Config.cs          配置 (.env 加载, 67 项全部可配)
├── WatchMode.cs        Watch 模式 (文件监听 + AI! 注释)
├── PermissionManager.cs 权限确认系统
├── ProjectContext.cs  项目检测 + CLAUDE.md 加载
├── ProjectInitializer.cs /init 项目初始化 (生成 CLAUDE.md)
├── ReviewMode.cs      代码审查模式
├── FallbackLLM.cs     模型回退链 (6 模型, 跨供应商 Key 解析)
├── MemoryStore.cs     记忆系统 (旧格式, 迁移源)
├── StructuredMemory.cs 结构化记忆 (frontmatter 多文件 + MEMORY.md 索引)
├── MemoryRetrieval.cs  跨会话记忆检索
├── Skills/            技能系统 (SkillsManager.cs SKILL.md 发现与解析)
├── BackgroundTask.cs  后台任务
├── DebugLog.cs        调试日志
├── Test/              测试/调试/演示代码（SelfTest 自测 13 partial 文件 + Benchmark/Keypad/TuiAudit/TuiDemo，共 3070 项）
├── Batch/             批量任务引擎 (2 文件)
│   ├── BatchSpec.cs     任务清单模型 + JSON 解析 + 名称消毒
│   └── BatchRunner.cs   多仓库并行执行 + worktree 隔离 + 聚合报告
├── Plugins/           编译期插件系统 (IPlugin SDK + PluginRegistry)
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
└── Tools/             44 个工具
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
    ├── AskUserQuestionTool.cs ScreenshotTool.cs
    ├── ViewImageTool.cs
    ├── TranscribeAudioTool.cs
    ├── DrawTool.cs 绘图（文本 DSL → SVG/PNG，零反射）
    └── ImageConvertTool.cs 图片互转（PNG/JPG/BMP）
```

## 44 个工具

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
| `lsp` | LSP 代码导航（go-to-def, references, hover, symbols），14 种语言 |
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
| `screenshot` | 抓屏（终端文本 / 桌面 PNG + OCR） |
| `view_image` | 查看本地图片，附加到下一轮请求让 vision 模型读取 |
| `transcribe` | 转录音频文件为文字（Whisper 兼容 API），补齐多模态音频输入 |
| `draw` | 用文本指令绘制图形（变换/新形状/描边/渐变/贴图/裁剪/图标模板，20+ 指令），输出 SVG 矢量或 PNG 位图 |
| `convert_image` | 图片格式互转（PNG/JPG/BMP），按魔数识别输入、按扩展名决定输出 |

## REPL 命令

```
/model <名称>    切换模型 / 打开模型选择器 (Ctrl+M)
/compact         手动压缩上下文
/tokens          查看 token 用量和费用估算 (含任务级花费)
/stats           查看会话统计 (token/PromptCache/LLM指标)
/recent          查看本次会话改过的文件 (别名 /diff)
/plan            计划模式，先规划再执行
/init            分析项目并生成 CLAUDE.md（对标 Claude Code /init）
/mcp             查看 MCP 服务器状态 / 重连
/git              Git 操作（status/log/diff/commit/branch）
/perm ask|auto|smartauto|yolo  权限模式切换
/mode build|plan|review|auto  工作模式切换 (Shift+Tab)
/update [check|now]  检查/自动升级到最新版本
/auto            自动模式切换
/watch           切换 Watch 模式
/session         会话管理 (list/save/load/resume)
/export          导出对话历史
/history         搜索对话历史
/settings        图形化设置界面
/theme           切换主题
quit / exit      退出 (Ctrl+Q)
```

## 多 Agent 工作区

**F1-F10** 一键切换 10 个独立 Agent 槽位，各占各的屏幕（聊天历史、输入草稿、状态栏各自独立），互不干扰。**槽位支持后台并行执行**——在 F1 跑任务时可直接切到 F2 开新任务，后台槽位的输出自动缓冲，切回时完整展示。状态栏左侧 10 个数字实时显示各槽位状态：

| 显示 | 含义 |
|---|---|
| 底色白 | 当前显示的屏幕 |
| 灰色 | 空闲 |
| 绿色 | 工作中 |
| 黄色 | 等待权限确认 |
| 红色 | 出错 |

> 运行中热键：`Esc` 中断当前槽位 Agent，`Ctrl+Z` 优雅暂停（当前批次完成后提交停机）
> 热键迁移：帮助 `Ctrl+H`、面板 `Ctrl+B`、设置 `Ctrl+T`、退出 `Ctrl+Q`

## 关键设计决策

- **系统化流水线**：复杂任务自动走 10 阶段（调查→分析→规划→拆分→分工→执行→调试→审核→提交→总结）
- **edit_file 使用唯一子串匹配**，不用行号，安全可审查
- **上下文压缩三层让步**：50% 裁剪 → 70% LLM 摘要 → 90% 硬折叠，实时进度事件
- **渐进超时重试**：LLM 超时逐次加长（1x→1.5x→2x→3x→4x→6x→8x），最多 5 次重试
- **子智能体并行**：tasks 数组支持多并发，聚合返回；通过不给 agent 工具约束递归
- **多 Agent 工作区**：F1-F10 切换 10 个独立会话槽位，状态栏实时显示工作状态，支持槽位任务队列
- **多会话真并行**：槽位任务后台线程执行不阻塞主循环，运行中可自由切换；活跃槽位实时流式、非活跃槽位缓冲输出，切换与路由共享槽位锁杜绝丢 token（对标 Claude Code 多窗口）
- **Hook 系统**：8 种事件（PreToolUse/PostToolUse/Stop/PreCompact 等），JSON 结构化输出协议
- **动态状态栏**：实时显示 Agent 状态/工具执行/压缩进度，Braille 旋转动画
- **任务级花费追踪**：每轮对话独立统计 prompt/completion tokens 和费用
- **技能系统**：标准 SKILL.md 格式，系统提示词只给名称+描述，`skill` 工具按需加载完整内容
- **Git 自动提交质量校验**：conventional-commit 前缀强制 + 不合格重试一次 + 兜底默认信息
- **Worktree 隔离**：bash 自动检测 worktree 路径并切换 cwd
- **AOT 编译：JSON 手写序列化**，不依赖反射
- **权限系统**：bash/write/edit/agent 默认行内确认，`/perm yolo` 跳过
- **双模型架构**：大模型做复杂任务，小模型做压缩/摘要，自动分工省钱
- **模型回退链**：失败自动尝试备选（6 模型链条，跨供应商 API Key 自动解析）
- **Watch 模式**：文件监听 + AI! 注释解析 → 线程安全队列 → REPL 自动执行
- **结构化记忆**：`.waycoder/memory/*.md` frontmatter 多文件 + MEMORY.md 索引，支持跨会话检索
- **Diff 预览**：`WAYCODER_DIFF_PREVIEW=1` 开启写文件前逐 hunk 确认，非交互模式自动跳过
- **Bash 安全防护**：70+ 禁止命令 + 47 安全白名单，管道中每个命令独立检查
- **计划审批门**：`计划` 模式（Shift+Tab）下模型产出计划后不自动执行，就地弹出审批框——批准则切回 `建造` 模式继续执行，拒绝则停止（对标 Claude Code Plan Mode）
- **项目初始化 `/init`**：扫描项目生成中文 CLAUDE.md（语言/框架/构建工具 + 构建/测试/lint 命令探测），对标 Claude Code /init；下次启动自动注入系统提示词
- **MCP 状态管理 `/mcp`**：结构化状态模型（Connecting/Connected/Failed）+ 热重连，`/mcp` 查看服务器状态、`/mcp reload [name]` 重连，对标 Claude Code /mcp
- **MCP 资源/提示词**：`resources/list` + `resources/read` 注册为 `mcp__<server>__resources` 读取工具、`prompts/list` + `prompts/get` 每个模板注册为 `mcp__<server>__prompt__<name>` 工具，对标 Claude Code MCP resources/prompts
- **内置自动升级**：`/update` 检查、`/update now`/`--update` 自替换；版本检查优先 Gitee Releases（国内快）、回退 GitHub（环境变量可覆盖）；Windows 落 `.new`+`upgrade.bat` 退出后自动替换重启、Unix 原子 rename 覆盖运行中二进制（对标 Claude Code `claude update`）
- **分发渠道**：`packaging/` 提供 winget manifest / Homebrew formula / apt `.deb` 打包脚本 + GitHub Actions 发布工作流，详见 [docs/安装与升级.md](docs/安装与升级.md)
- **多模态（图片 + 音频）**：`view_image` 附加本地图片让 vision 模型「看图」、`transcribe` 把音频转成文字（Whisper 兼容 API）——补齐图片与音频两种多模态输入，对标 Codex CLI / Gemini CLI
- **批量任务引擎**：`--batch`/`--batch-repo` 多仓库并行处理，每个任务 `git clone` 到独立副本 + 子进程 `-p` 一次性模式执行（worktree 隔离），聚合报告 + 退出码，对标 Cursor 批量修复 / Aider 多仓库脚本
- **编译期插件系统**：`IPlugin` SDK——`WayCoder/Plugins/` 目录放一个 `.cs` 文件 + `[ModuleInitializer]` 自动注册，即可贡献工具（`ITool`）与斜杠命令（`ISlashCommand`），AOT 无反射、随单文件 exe 分发，详见 [docs/插件系统.md](docs/插件系统.md)
- **JSON 输出模式（IDE 桥接）**：`--json -p "任务"` 一次性模式静默执行 Agent，stdout 只输出一个结构化 JSON 对象（schema/success/answer/error/model/usage/cost_usd/duration_ms/changed_files），供 VS Code 扩展、CI 脚本、外部工具直接 `JsonNode.Parse` 解析，无需剥离 ANSI 动画；`JsonResult.Build` 纯函数便于自测（对标 Claude Code `--output-format json`）

## 贡献 / License

深圳市探索智能科技有限公司

---

作者 [施探宇(aleck)](https://gitee.com/aleckstygit)，中国 · 深圳
