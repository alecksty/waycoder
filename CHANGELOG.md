# 更新日志

## v0.16.3 (2026-08-07) — Watch 模式

### ✨ 新增功能
- **Watch 模式 (`--watch` / `-w`)** — 监听外部编辑器文件变更，检测代码注释中 `AI!` / `AI?` 标记自动触发 Agent
  - 兼容 Aider 的 AI 注释语法：`// AI!`、`# AI!`、`-- AI!`、`/* AI? */`、`<!-- AI! -->`
  - 支持 40+ 种编程语言（.cs .py .ts .js .go .rs .java .kt .swift .c .cpp .rb .php .vue .svelte 等）
  - 500ms 防抖合并快速连续修改 + 线程安全 `ConcurrentQueue` 队列
  - `/watch` REPL 命令切换 + `CORECODER_WATCH` 环境变量 + 设置界面集成
  - 3 处退出路径自动清理（F10 / quit / Ctrl+C）

## v0.16.2 (2026-08-07) — /stats 用量统计面板

### ✨ 新增功能
- **`/stats` 用量统计** — 表格面板展示模型/Token/花费/延迟/速度/消息/会话/权限等全维度用量

## v0.16.1 (2026-08-07) — 滚动修复 + /loop + /test + 界面主题

### 🐛 修复
- **滚动区域重构** — ChatScrollUp 下限 clamp + ChatScrollDown 延迟判底机制
- **ShowMenu 重写** — 长列表自动滚动 + PgUp/PgDn/Home/End 导航 + 选中项背景填充修复 + 滚动指示器
- **RenderSuggestions 越界修复** — 边框/填充改用 `chatW` 替代 `TW`，右侧面板激活时不再视觉错乱
- **ShowDialog 边距修正** — 标题/内容/提示行 fill 计算公式统一
- **箭头键聊天滚动** — 输入为空时 ↑↓ 滚动聊天区；Ctrl+↑/↓ 随时可滚动

### ✨ 新增功能
- **`/loop` 循环执行** — `/loop [最大轮次] 提示词` 重复执行 Agent 直到输出含成功标记（SUCCESS/✅/通过 等）
- **`/test` 分模块测试** — `/test all|tools|ui|git|config|memory|agent|review|mcp|system` 只跑相关模块
- **界面主题系统** — 6 套预设配色（default/ocean/forest/sunset/mono/cyberpunk）+ 4 种边框类型（rounded/single/double/bold）+ 独立边框色/强调色设置
- **SettingsPage 新增 `🎨 界面` 分类** — 配色方案/边框类型/边框颜色/强调色 四设置项，选色立即生效

### 🔧 增强
- 快捷键栏新增 `PgUp/Dn 滚动` 提示
- SelfTest 新增 Section/RunWithFilter 过滤机制
- ScreenManager 全面主题化：顶栏/分隔线/输入区/建议面板/菜单/对话框 均使用主题边框和颜色

## v0.16.0 (2026-08-07) — 20 项综合增强

### ✨ 新增功能
- **MCP 环境变量传递** — `mcp_servers.json` 支持 `env` 字段，可向 MCP 进程注入 API Key 等环境变量
- **标准输入管道模式** — `echo "prompt" | corecoder` 管道输入自动一次性模式 + yolo
- **记忆自动注入** — MemoryStore 内容自动注入系统提示词，跨会话项目知识持久化
- **Sub-Agent 增强** — 子智能体使用小模型（省钱）+ 注入父上下文（最近 6 条消息）
- **自定义提示词模板** — 扫描 `.corecoder/prompt.md` 及 `.corecoder/*.md`（排除 memory.md）自动注入
- **对话历史搜索** — `/history <关键词>` 搜索 + `Ctrl+R` 交互搜索，含上下文预览
- **Diff 预览确认增强** — write_file 显示行数/新建/覆盖 + 内容预览，edit_file 显示 +/- 对比
- **项目初始化向导** — `corecoder --init` 创建 `.corecoder/` + 模板文件（mcp_servers.json, prompt.md, memory.md）
- **命令别名** — `/c→/compact, /m→/model, /r→/reset, /h→/help, /t→/tokens, /d→/diff, /s→/save, /q→quit`
- **Token 性能统计** — 每次请求延迟 + tok/s 显示在 `/tokens` 和右下角状态栏
- **Agent 完成通知** — 耗时显示 + 终端响铃 `\a`
- **Agent 错误自恢复** — 工具报错时追加修正提示，引导 LLM 自我纠正
- **对话导出** — `/export` → `.corecoder/export_<date>.md`，Markdown 格式含角色和时间戳
- **输入历史** — `↑↓` 键浏览历史输入（单行模式），最多 200 条，去重相邻重复
- **模型热键切换** — `Ctrl+M` 循环切换大模型：deepseek-v4-flash → pro → gpt-5.4-mini → gpt-5.4
- **HTTP 代理支持** — 读取 `HTTPS_PROXY` / `HTTP_PROXY` / `ALL_PROXY` 环境变量配置代理
- **Tab 路径补全** — 输入像文件路径时 Tab 智能补全（最长公共前缀 + 候选列表）
- **Git 分支显示** — 启动时检测 `.git/HEAD`，顶栏显示当前分支名
- **最近文件列表** — `/recent` 显示最近修改文件（最多 50），按时间排序
- **快捷键完善** — `F1` 帮助，`F10` 自动保存 + 退出

### 🔧 增强
- CJK 感知 Token 估算（CJK ~1.5 tok/char，ASCII ~0.25 tok/char）
- Checkpoint 持久化加载（重启后 `/undo` 不丢失）
- Review 模式改为 git-diff-based（聚焦实际改动）
- 会话自动保存（退出时保存，启动时 `/resume` 恢复）
- 欢迎屏 ASCII Logo 注入聊天区
- 快捷键栏精简：`F1帮助 F2面板 F5设置 F6编辑 ↑↓历史 Ctrl+R搜索 Ctrl+M切模型`
- 版本号 v0.15.1 → v0.16.0

## v0.9.0 (2026-08-06) — 14 个内置工具 + 6 个新命令 + 进程管理

### ✨ 新增功能
- **仓库地图 (Repository Map)** — 自动扫描项目结构，17 种语言符号提取，ASCII 树状图
- **14 个纯 C# 内部工具** — 替代 Shell 依赖，可控缓冲区/超时/无转义问题：
  - 进程管理：`ps`（进程列表）、`kill`（终止进程，含系统进程保护）
  - 目录操作：`ls`（目录列表）、`mkdir`（创建目录）、`rm`（安全删除）、`cd`（切换目录）、`pwd`（打印路径）
  - 文件操作：`cp`（复制）、`mv`（移动）、`diff`（差异比对）、`stat`（元数据）
  - 文本统计：`wc`（行/词/字符计数）、`tree`（ASCII 目录树）、`find_replace`（跨文件查找替换）
- **6 个新 REPL 命令**：`/cost`（费用统计）、`/commit`（Git 提交）、`/doctor`（环境诊断）、`/config`（配置管理）、`/test`（运行测试）、`/status`（项目状态）
- **Git PR 工具** — `git_pr` 自动创建分支、推送、生成 GitHub/Gitee PR 链接
- **打包脚本** — `package.sh` / `package.bat` 一键 AOT 发布 + zip 打包
- **`/repomap` REPL 命令** — 手动刷新仓库地图

### 🔧 增强
- 仓库地图自动集成到系统提示词，LLM 时刻了解代码库布局
- `write_file`/`edit_file` 后自动使仓库地图缓存失效
- GitTool 异步输出读取，修复大输出死锁问题
- ITool Schema() 深克隆 Parameters，修复 "node already has a parent" 崩溃
- 工具总数：15 → 29
- 245 项自测（+49 项新工具/命令测试）

## v0.8.0 (2026-08-06) — 预算控制 + Hooks 生命周期 + MCP 协议

### ✨ 新增功能
- **硬预算上限** — `--max-budget-usd` CLI 标志 + `CORECODER_MAX_BUDGET_USD` 环境变量，超支自动停止
- **Hooks 生命周期** — PreToolUse / PostToolUse 事件，Shell 脚本处理器，退出码 2=阻止
- **MCP 协议支持** — Stdio 传输，自动发现 MCP 服务器工具，命名空间 `mcp__<server>__<tool>`
- **MCP 配置** — `.corecoder/mcp_servers.json` 配置服务器

### 🔧 增强
- `Config.MaxBudgetUsd` — 预算配置字段
- `ToolRegistry.AllTools` → 动态属性，自动合并 MCP 工具
- 166+ 项自测（+3 项预算/Hooks/MCP 测试）

## v0.7.0 (2026-08-06) — 自定义命令 + 自动 Lint 闭环 + YOLO 模式

### ✨ 新增功能
- **`--yolo` CLI 标志** — 一次性模式跳过所有权限确认，非交互环境可用
- **自动 Lint 反馈闭环** — `write_file`/`edit_file` 后自动运行 lint，错误注入 LLM 上下文
- **自定义斜杠命令** — `.corecoder/commands/*.md` 用户可扩展，支持 YAML frontmatter + `$ARGUMENTS`
- **示例命令** — `.corecoder/commands/review-code.md` 代码审查模板

### 🐛 Bug 修复
- **JsonNode Parent 冲突** — `FullMessages()` 深克隆消息，修复 "already has a parent" 崩溃

### 🔧 内部
- `CustomCommands.cs` — 自定义命令加载器
- 163+ 项自测（+3 项自定义命令测试）

## v0.6.1 (2026-08-06) — 三个新工具 + 增强语言支持

### ✨ 新增工具
- **Lint 工具** — 静态检查反馈闭环，支持 25+ 种编程语言（C#、Python、JS/TS、Go、Rust、Java、C/C++、Ruby、PHP、Swift、Kotlin、Lua、Shell、HTML/CSS、Vue、YAML、JSON、Markdown、Dart、R、SQL、Perl、Elixir、Haskell、Zig）
- **Web 搜索工具** — 通过 DuckDuckGo 进行网页搜索，无需 API 密钥
- **Checkpoint 系统** — Git stash + 文件备份双轨检查点，支持 `/checkpoint` `/undo` `/checkpoints` 命令

### 🔧 增强
- Lint 工具自动检测项目类型（通过扩展名和项目文件）
- AOT 安全的 JSON 序列化（无反射，手写 JSON 构建）
- 160 项自测全通过（+15 项新增测试）

## v0.6.0 (2026-08-06) — 纯 C# 版

### 🔥 重大变更
- **移除 Python 版** — 删除 `corecoder/`、`tests/`、`pyproject.toml` 等全部 Python 代码
- **C# 版成为唯一版本** — `CoreCoderSharp/` 是项目唯一实现
- **文档中文化** — README、CLAUDE.md、CHANGELOG 全部改为中文

## v0.5.0 (2026-08-05) — C# 重构版

### 🚀 C# 移植 (CoreCoderSharp)
- 完整移植 Python 版全部功能到 C# (.NET 10)
- AOT 原生编译为单文件 exe (7.8 MB)，无需运行时
- 12 个工具，44 项自测全通过

### ✨ 新增功能 (C# 版)
- **权限确认系统** — 危险操作前弹窗确认，支持 Ask/Auto/Yolo 三种模式
- **Git 智能集成** — git 工具 + `/git-status` `/git-log` `/git-diff` 命令
- **Web 抓取** — fetch 工具，自动提取网页纯文本
- **计划模式** — `/plan` 命令，Agent 先规划再执行
- **Todo 任务追踪** — Agent 可管理结构化任务列表
- **LSP 代码导航** — go-to-definition, find-references, hover, symbols
- **后台任务** — bash 命令后台执行，`/jobs` `/job-output` 查看
- **记忆系统** — Agent 可读写项目记忆 (`.corecoder/memory.md`)
- **流式工具执行** — LLM 边生成边执行工具，降低延迟
- **调试日志** — `--debug` 记录完整通信内容到 logs/
- **彩色 TUI** — Spectre.Console 美化界面 + ASCII Art 欢迎屏
- **项目指令加载** — 自动读取 CLAUDE.md/AGENTS.md
- **代码审查** — `/review` 命令多维度审查修改
- **模型回退** — LLM 失败时自动尝试备用模型
- **项目检测** — 自动识别语言/框架/构建工具

### 🔄 Python 版更新
- 默认模型改为 `deepseek-v4-flash`
- 新增 DeepSeek V4 Flash/Pro 定价
- CLI 自动识别 DeepSeek 模型并设置 base URL
- 所有注释和 docstring 翻译为中文

## v0.4.1 (2026-08-01)
- 添加 AGENTS.md 双语智能体指南
- 初始版本
