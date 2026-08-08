# 更新日志

## v0.18.0 (2026-08-08) — 窗口系统 + 主题引擎 + TUI 控件库 + 终端抽象层

### 🔥 重大变更

**窗口管理器** (`UI/WindowManager.cs`)
- Z-order 层叠窗口、裁剪渲染、模态对话框、弹出菜单、Toast 提示框
- UIControl 体系：UILabel / UIButton / UIInput，Tab/Shift+Tab 焦点切换
- 11 种边框风格：single/double/rounded/thick/solid/dotted/dashed/slash/triangle/ascii/custom
- 菜单满行高亮条、分隔线、滚动指示器 + 滚动条
- 窗口关闭自动还原背景

**终端抽象层** (`Terminal/`)
- `TTY` — 屏幕切换/清屏/光标/鼠标/颜色/样式快捷方式
- `RenderBuffer` — Write/Segment/Fill/SegmentBold/SegmentDim/Blink 零转义符渲染
- `BoxChars` — 13 种预设 + 自定义边框字符集
- `AnsiString` — ANSI 检测/剥离/截断/宽度计算
- `Color` — 命名颜色管理（Color.Cyan 替代数字 36）

**主题引擎** (`ThemeConfig.cs`)
- 6 个预设主题：default/ocean/forest/sunset/midnight/mono
- 自定义边框/背景/前景/选中色，持久化到 `~/.corecoder/theme.json`
- `/theme` 命令 + 设置页面主题选择
- ScreenManager 主题自动同步

**Markdown 渲染** (`UI/MarkdownRenderer.cs`, `UI/TuiMarkdown.cs`)
- 标题/段落/代码块(14 语言语法高亮)/表格/列表/内联格式

**Diff 渲染** (`UI/DiffRenderer.cs`)
- 红绿背景 + 行号 + 语法高亮

**InputManager** (`UI/InputManager.cs`)
- 全键盘拦截 + 鼠标滚轮 + resize 即时重绘

### ✨ 新增功能
- 子智能体递归：支持最多 5 层嵌套（`AgentTool.MaxDepth`）
- `/undo` 按文件精确 revert（`CheckpointManager.UndoAsync(id, filePath)`）
- TF-IDF 语义记忆（`SemanticMemory.cs`，零依赖纯 C#）
- Lint/Tool 超时可配置（`ToolTimeoutSec` / `LintTimeoutSec`）
- `/todo` 命令接入 UI
- Logo 补齐 "WAY**CODER**" 完整拼写

### 🔧 修复
- `DisplayWidth` 不计入 ANSI 转义码宽度
- 窗口裁剪 `Clip()` 修复顶框/左框被误裁
- 右框绝对定位，不再因 CJK 文字宽度错位
- 空窗口竖边始终渲染
- Damerau-Levenshtein 支持字符换位纠错
- 历史上限 while 循环修复
- 建议面板半宽+滚动+不溢出
- 滚动菜单选中项不被指示器挤出

### 📝 自测
- **636 项**（+35 新测试：主题/边框/Diff/InputManager/窗口）

## v0.17.5 (2026-08-07) — 移除 Terminal.Gui v2，恢复 AOT，三项短板清零

### 🔥 重大变更
- **移除 Terminal.Gui v2** — 回退到 ANSI 全屏 TUI（ScreenManager），Terminal.Gui v2 库体验不佳
- **恢复 AOT 编译** — `PublishAot` 重新启用，恢复零依赖单文件 exe 部署（~8MB）
- **子智能体递归** — 支持最多 5 层嵌套子智能体，深度可配置，自动移除 agent 工具防止无限递归
- **`/undo` 按文件精确 revert** — 支持 `/undo [N] <file>` 选择性恢复，`/undo -l` 列出检查点文件
- **工具/Lint 超时可配置** — 新增 `ToolTimeoutSec`（默认 120s）和 `LintTimeoutSec`（默认 60s）

### 🗑️ 移除
- 删除 `TerminalGuiRepl.cs` / `Controls.cs` / `ScrollMenu.cs`
- 删除 `--tui-v1` CLI 参数（仅保留 ANSI TUI）
- 删除 `TERMINAL_GUI` 编译常量和 Terminal.Gui NuGet 依赖
- 删除 `RunReplV2Async()` 方法

### ✨ 新增功能
- **Lint/Tool 超时可配置** — `WAYCODER_TOOL_TIMEOUT` / `WAYCODER_LINT_TIMEOUT` 环境变量
- **`/undo` 按文件恢复** — `CheckpointManager.UndoAsync(id, filePath)` + `GetCheckpointFiles()`
- **子智能体递归** — `AgentTool.MaxDepth` + `AsyncLocal<int>` 深度追踪
- **子智能体深度配置** — `WAYCODER_SUBAGENT_DEPTH`（默认 3，范围 1-5）
- **`/undo` / `/checkpoint` / `/checkpoints` 命令接入 UI** — 修复死代码问题

### 🔧 保留
- ScreenManager ANSI TUI 作为唯一交互式 REPL 实现
- 所有现有功能：侧栏面板、输入历史、彩色渲染、建议补全、弹窗菜单

## v0.17.4 (2026-08-07) — Terminal.Gui v2 默认 TUI

### 🔥 重大变更
- **Terminal.Gui v2 成为默认 TUI** — 替代手写 ANSI 转义码的 ScreenManager
  - 原 `--tui-v2` 标志改为 `--tui-v1`（回退到旧版 ANSI TUI）
  - 移除全部 `#if TERMINAL_GUI` 条件编译守卫
  - **AOT 编译暂时禁用**：Terminal.Gui v2 不支持 NativeAOT，待 v2 正式版后恢复
  - PublishAot 设为 false，仍保留单文件发布

### ✨ 新增功能
- **聊天区彩色多角色渲染** — `ChatView`（View + Label 组合）替代单色 `TextView`
  - User=亮青 `BrightCyan`、Assistant=白 `White`、System=灰 `Gray`、Tool=亮黄 `BrightYellow`、Welcome=青 `Cyan`
  - 每行独立 `ColorScheme`，手动 Y 坐标滚动
  - 流式输出实时追加到最后一行
- **侧边面板** — F2 切换 32 列右侧面板，4 个标签页：
  - 任务（Todo 列表）、文件（修改文件列表）、锁（活跃文件锁）、MCP（服务器状态）
- **输入历史导航** — ↑↓ 键浏览历史输入（单行模式），Ctrl+Enter 插入换行，Esc 清空
- **自动会话恢复** — 启动时检测 `_auto` 会话，提示 `/resume` 恢复
- **Scroll 快捷键** — PageUp/Down 滚动聊天区，Ctrl+Home/End 跳转首尾

### 🐛 修复
- 管道模式误判：`Console.IsInputRedirected` 在 bash 下始终为 true，修复为空 stdin 不触发一次性模式
- `Tab.View` 为 null 导致 NRE 崩溃（v2 API 变更：Tab 即 View）
- `Application.Top` 在 `Run()` 之前为 null 导致 NRE 崩溃
- `Colors.Base` / `Colors.TopLevel` / `Colors.Menu` 在 v2 不存在，改为视图级 `ColorScheme`
- `verify_build/` 目录混入 git 提交，已清理并加入 `.gitignore`

### 🔧 增强
- 状态栏快捷键：F1 帮助 / F2 面板 / F5 设置 / F6 编辑 / F10 退出
- 窗口标题实时显示当前模型名称
- CJK 字符在 Label 中正常显示（Terminal.Gui 内部处理）

## v0.17.3 (2026-08-07) — MCP 协议完善

### ✨ 新增功能
- **MCP 传输抽象层** — 解耦通信方式与协议层
  - 新增 `McpTransport` 抽象类：`SendRequestAsync` / `SendNotification` / `DisconnectAsync` / `IsConnected`
  - `StdioMcpTransport`：从 `McpConnection` 提取子进程管理代码（向后兼容）
  - `HttpMcpTransport`：HTTP POST + SSE 响应流解析，支持 Streamable HTTP 传输
  - `McpConnection` 重构为协议层，持有 `McpTransport` 实例委托通信
- **MCP 配置格式增强**
  - `mcp_servers.json` 新增 `transport` / `url` / `headers` 字段
  - 自动检测传输类型：有 `url` → HTTP，否则 → stdio（向后兼容）
  - `headers` 和 `url` 中的 `${VAR}` 语法自动展开为环境变量
  - `RunInit()` 模板添加 HTTP MCP 服务器注释示例
- **工具发现缓存** — 持久化 `tools/list` 结果，加速启动
  - 新增 `McpCache.cs`：SHA256 缓存键 + 24h TTL
  - `CachedMcpTool`：缓存命中时立即可用，后台异步刷新
  - 配置变更自动失效（基于命令/参数/URL 的 SHA256 哈希）
- **MCP 面板状态显示** — F2 面板 MCP 页显示服务器连接状态和工具数量
  - `McpManager.Info` 属性 + `ScreenManager` 自动读取渲染

### 🔧 增强
- MCP 客户端版本号更新为 0.17.3
- `DiscoveredTools` 支持按服务器前缀移除旧条目（缓存刷新时替换）

### 📝 自测
- 10+ 项 MCP HTTP 传输测试（传输检测、环境变量展开、headers 解析）
- 6 项 MCP 缓存测试（缓存键稳定性、规范标识符、格式验证）

## v0.17.2 (2026-08-07) — TUI 编辑器 Lint 诊断集成

### ✨ 新增功能
- **编辑器 Lint 诊断** — 保存文件时自动运行 lint 检查，行内标注错误和警告
  - 新增 `DiagnosticManager.cs`：解析 10+ 种 linter 输出格式（dotnet build / eslint / ruff / go vet / gcc / shellcheck / ruby / php / java / rust cargo）
  - 编辑器 gutter 指示器：有错误的行显示红色 `●`，有警告的行显示黄色 `▲`
  - 错误行红色背景、警告行黄色背景高亮
  - 状态栏显示错误/警告计数和当前行诊断消息
  - 配置开关：`WAYCODER_EDITOR_LINT`（默认开启）
- 新增 `ErrorBg` / `WarningBg` 颜色常量（ANSI 41 / 103）

### 📝 自测
- 10 组诊断解析测试（dotnet/eslint/ruff/go vet/gcc/shellcheck/ruby/php/java/rust）
- 3 组查询/配置/枚举测试

## v0.17.1 (2026-08-07) — 沙箱执行

### ✨ 新增功能
- **三级沙箱执行** — suggest（确认）/ auto-edit（自动编辑）/ full-auto（全自动沙箱）
  - 新增 `SandboxManager.cs`：环境变量清理、工作目录锁定、系统目录写保护、内存监控（1GB 限制）
  - bash 工具集成：沙箱模式下自动创建受保护的进程、异步内存监控
  - 权限系统联动：full-auto 模式 bash 直接放行
  - 配置开关：`WAYCODER_SANDBOX_LEVEL`（默认 suggest）

### 📝 自测
- 30 项沙箱管理测试

## v0.17.0 (2026-08-07) — Prompt 缓存 + 竞品 P0 清零

### ✨ 新增功能
- **Prompt 缓存追踪** — SHA256 本地检测系统提示词和工具定义是否重复发送
  - 新增 `PromptCache.cs`：静态追踪类，每次 LLM 请求前记录哈希值
  - `/stats` 面板展示缓存命中率、节省 Token 数和估算费用
  - 配置开关：`WAYCODER_PROMPT_CACHE`（默认开启）
  - 不影响实际 LLM 请求内容，仅在监控层面追踪
- **竞品 P0 差距全部清零** — 自动 Test 修复循环（v0.16.3 已有）+ Prompt 缓存（v0.17.0）

### 📝 文档更新
- ROADMAP.md：自动 Test 循环 ✅、Prompt 缓存 ✅、重新排布优先级
- competitor-analysis.md：P0 差距清零、功能矩阵更新、代码自愈评级提升

## v0.16.3 (2026-08-07) — 更名 WayCoder（道码）+ Watch 模式

### 🔥 重大变更
- **软件更名** — CoreCoder → **WayCoder（道码）**，因发现与现有商标名称冲突，规避侵权风险
  - 可执行文件：`corecoder.exe` → `waycoder.exe`
  - 环境变量前缀：`CORECODER_*` → `WAYCODER_*`（旧名仍兼容）
  - 配置目录：`.corecoder/` → `.waycoder/`（旧目录仍兼容）
  - 展示文本全部更新：UI 标题栏 / 帮助文本 / 测试输出 / 调试日志 / User-Agent / MCP 客户端名

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
