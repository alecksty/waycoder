# AGENTS.md

本文件为 Code Agents 在此仓库中工作时提供指导。

## 项目概述

WayCoder (道码) 是一个中文编程智能体,C#开发(.NET 10),吸收过很多竞品的优点.

## 常用命令

```bash
# C# 版
cd WayCoder
dotnet publish -c Release            # AOT 编译
dotnet run -- --test                 # 6232 自测
dotnet run -- -p "提示词"            # 一次性模式
dotnet run -- --watch                # Watch 模式 (监听 AI! 注释)
```

## 架构

```
WayCoder/
├── Program.cs         入口 + CLI + REPL (ANSI 全屏 TUI)
├── Agent/             智能体核心 (20 文件)
│   ├── Agent.cs           主循环 (Stop Hook + WorkReporter + 10 阶段流水线)
│   ├── AgentSlot.cs       多 Agent 工作区 (F1-F10 槽位切换 + 后台并行)
│   ├── LLM.cs             LLM 客户端 (流式 + 渐进超时重试 + 任务花费追踪)
│   ├── ContextManager.cs  Crush 风格上下文管理 (token 追踪 + 自动摘要 + 进度事件)
│   ├── SystemPrompt.cs    系统提示词 (15 个结构化区块)
│   ├── WorkModeManager.cs 工作模式 (Build/Plan/Chat)
│   └── FallbackLLM.cs / BackgroundTask.cs / WorkReporter.cs / TaskProgress.cs
├── Memory/            记忆与会话 (9 文件: StructuredMemory + MEMORY.md 索引 / MemoryRetrieval / SessionManager / ProjectKnowledge)
├── Config/            配置 (24 文件: Global.cs 全局 ~/.waycoder/config.json 权威源 / Config.Schema 110 项 / ConnectionConfig / ModelCatalog / ModelCli)
├── Infra/             基础设施 (86 文件: BashGuard / FileTracker / SandboxManager / HooksManager / UpdateChecker / DrawEngine 绘制 + 图片编解码 + Logging/)
├── Git/               Git 集成 (8 文件: GitRunner / GitCore / PackFile / RepoMapGenerator / WorktreeIsolation)
├── Watch/             Watch 模式 + ReviewMode
├── Sql/               手搓 SQL 引擎 (SqlEngine.cs)
├── Skills/            技能 + 权限 (SkillsManager / PermissionManager / AutoModeClassifier / builtin/)
├── Test/              测试/调试/演示代码（SelfTest 自测 40 partial 文件 + Benchmark/Keypad/TuiAudit/TuiDemo，共 6232 项）
├── Batch/             批量任务引擎 (BatchSpec 清单模型 + BatchRunner 多仓库并行/worktree 隔离)
├── Plugins/           编译期插件系统 (IPlugin SDK + PluginRegistry + [ModuleInitializer] 自动注册)
├── Tools/             49 个工具
│   ├── BashTool.cs    GitTool.cs    LspTool.cs
│   ├── ReadFileTool.cs FetchTool.cs MemoryTool.cs
│   ├── WriteFileTool.cs TodoTool.cs  LintTool.cs
│   ├── EditFileTool.cs AgentTool.cs  WebSearchTool.cs
│   ├── GlobTool.cs    GrepTool.cs    GitPRTool.cs
│   ├── PsTool.cs      KillTool.cs    LsTool.cs
│   ├── MkdirTool.cs   RmTool.cs      CdTool.cs
│   ├── FindReplaceTool.cs CpTool.cs  MvTool.cs
│   ├── DiffTool.cs    TreeTool.cs    WcTool.cs
│   ├── StatTool.cs    PwdTool.cs     SkillTool.cs
│   ├── DocTool.cs     KbTool.cs      TestTool.cs
│   ├── DownloadTool.cs JobOutputTool.cs JobKillTool.cs
│   ├── NotebookEditTool.cs MultiEditTool.cs
│   ├── AskUserQuestionTool.cs ScreenshotTool.cs
│   ├── ViewImageTool.cs
│   ├── TranscribeAudioTool.cs
│   ├── DrawTool.cs 绘图（文本 DSL → SVG/PNG，零反射）
│   └── ImageConvertTool.cs / ConvertEncodingTool.cs / SqliteTool.cs / SymbolsTool.cs
└── UI/                 五端界面层
    ├── TUI/            终端界面 (220 文件)
    │   ├── Base/           控件基座 (TuiBase→TuiControl→TuiView / TuiManager / TuiScreen / TuiWindow / InputManager / 滚动数学)
    │   ├── Controls/       基础控件库 (45 文件: TuiButton / TuiListView / TuiDynamicBar / TuiKeybindHelp / TuiMarkdown …)
    │   ├── Custom/         自定义控件 + 对话框 (15 文件: ModelPicker / FilePicker / CommandPalette / DiffPreview / UxHelper …)
    │   ├── Screens/        全屏界面 (ChatScreen / MarkupChatScreen / SettingsScreen / EditorScreen …)
    │   ├── Edit/           终端源码编辑器 (EditorCore / Syntax 语法高亮 / DiagnosticManager Lint 诊断)
    │   ├── Renderers/      工具输出渲染器 (7 文件)
    │   └── Raw/            `.tui` 声明式布局
    ├── Shared/         跨端共享纯逻辑 (MarkdownRenderer / UnifiedDiff / AnsiHelper / AnsiColors / Terminal 缓冲与 ANSI / Vml* 协议与宿主接口)
    ├── WEB/            Web 端 (WebServer / WebChat / www 前端资源)
    ├── CLI/            命令行端 (Arguments 参数注册 / Commands 斜杠命令)
    └── GUI/            Avalonia GUI 占位（预留扩展）
```

## 关键设计决策

- **系统化流水线**：复杂任务自动走 10 阶段（调查→分析→规划→拆分→分工→执行→调试→审核→提交→总结），`<systematic_phases>` 内部流水线不向用户叙述
- **渐进超时重试**：LLM 超时逐次加长（1x→1.5x→2x→3x→4x→6x→8x 倍率），每次重试独立 CTS，最多 5 次
- **任务级花费追踪**：`LLM.TaskPromptTokens/TaskCompletionTokens/TaskCost`，每轮对话独立统计
- **Hook 系统**：8 种事件（PreToolUse/PostToolUse/PostToolUseFailure/SessionStart/SessionEnd/Stop/PreCompact/Notification），JSON 结构化输出协议（Decision/Reason/SystemMessage/AdditionalContext）
- **动态状态栏**：`TuiDynamicBar` 实时显示 Agent 状态/工具执行/上下文压缩进度，Braille 旋转动画，6 种状态
- **终端协议增强**：Bracketed Paste + Kitty Keyboard 协议，CSI 统一解析器
- **槽位任务队列**：`-p1`~`-p0` 槽位专项任务 + `-pa` 共享前缀，同一槽位多次排队
- **跨会话记忆检索**：`MemoryRetrieval` TF-IDF + 时间衰减排序，系统提示词自动注入匹配记忆
- **edit_file 使用唯一子串匹配**，不用行号，安全可审查
- **上下文压缩三层让步**：50% 裁剪 → 70% LLM 摘要 → 90% 硬折叠；Crush 风格真实 token 追踪（AddUsage/ShouldStopAndSummarize），大窗口 20K buffer / 小窗口 20% 比例
- **推理内容处理**：`reasoning_content`（DeepSeek V4）/ `reasoning`（Ollama/qwen）实时显示但不存入对话历史 — 显示=让用户看到思考过程，不存=不污染 API 调用
- **子智能体通过不给 agent 工具来约束**，不靠规则
- **多 Agent 工作区**：F1-F10 切换 10 个独立会话槽位，各占各的屏幕；状态栏 10 数字指示条（白底=当前屏，灰=空闲 绿=工作 黄=等权限 红=出错）；**Agent 运行中也能切**（后台线程执行不阻塞主循环）；AgentTool.ParentAgent 切槽位时重绑
- **AOT 编译：JSON 手写序列化**，`JsonHelper.SerializeArgs` 替代 `JsonSerializer`
- **权限系统**：bash/write/edit/agent 默认行内确认（输入框下方的文字选择栏，见下条），`/perm yolo` 跳过
- **双模型架构**：大模型做复杂任务，小模型做压缩/摘要，自动分工省钱
- **模型回退链**：一串 connect 名（`/connect chain <c1> <c2> ...` 设置），回退时 model+key+baseUrl 一起换（可跨服务商）；**开关默认关**（`FallbackEnabled` / `/connect chain on|off`）
- **文件锁**：FileLockManager 防止多 Agent 并发修改冲突，30s 超时自动释放
- **Watch 模式**：FileSystemWatcher 监听文件变更 → 提取 AI! / AI? 注释 → 线程安全队列 → REPL 轮询执行
- **全屏缓冲 UI**：备用屏 + 每帧重绘 + 行内权限块 + 弹窗菜单 + 侧栏面板 + 居中对话框
- **UI 控件库**：`UI/` 目录封装 TUI 控件（未来拆分 Tty 底层 + View 视图），`UI/Gui/` 预留 GUI 扩展
- **工具输出渲染器**：`IToolRenderer` 接口 + `ToolRendererFactory` 工厂，每种工具独立渲染器（对标 Crush ToolMessageItem），bash/edit/write/agent 各有 emoji + ANSI 着色
- **Dialog Overlay 栈**：`DialogOverlay` 栈式对话框管理 + `DialogAction` 类型化结果（对标 Crush overlay + typed actions），Push/Pop/按 ID 替换 + Esc 关闭栈顶
- **懒渲染列表**：`ILazyItem` 接口（`MeasureHeight`/`IsRenderCached`）+ `TuiListView` 二分查找首可见项 O(log n)（对标 Crush List + Item 接口）
- **渲染缓存**：`TuiMarkdown._parsed` + `_lastContent` + `_lastMaxWidth` 三级缓存，`EnsureParsed()` 仅在内容/宽度变更时重解析（对标 Crush cachedMessageItem）
- **模型选择对话框**：`ModelPicker.Show()` 全屏 ANSI 直写，21+ 模型按供应商分组，Tab 切大/小模型，实时搜索过滤，Ctrl+M 打开（对标 Crush models.go）
- **按钮组 + 独立滚动条**：`TuiButtonGroup` 水平/垂直布局 + Tab 导航 + 字母快捷键（对标 Crush button.go）；`TuiScrollbar` 拖拽滑块 + 鼠标滚轮 + 自动隐藏（对标 Crush scrollbar.go）
- **文件选择 + 命令面板**：`FilePicker.Show()` 目录浏览 + 文件搜索（对标 Crush filepicker）；`CommandPalette.Show()` 分类分组 + 模糊搜索 + 快捷键显示
- **行内权限确认**：`ChatScreen.InlineChoice`（一个 `TuiPromptBar` 实例）钉在**输入框下方**，❯ 箭头 + 黄底高亮 + 每项一行说明；键位 ↑↓/Home/End 移动、Enter 确认、Esc 拒绝、Y/N/A 单键、1-9 直选、多选 Space 勾选、多题 ←→/Tab 翻页，栏下方常驻键位提示行。**权限确认 / 计划审批 / 粘贴确认 / 通用确认 / 退出确认 / 设置页 select 全走它，CLI/TUI 不再弹框**（Web/GUI/MAUI 仍弹框，分界点是 `TuiManager.ActiveScreen is ChatScreen`，见 CLAUDE.md）。旧 `InlinePermission`（聊天流内嵌黄块）是死代码，已无生产调用
- **多行输入 + 历史**：`TuiDialog.Input()` 升级为 TuiTextArea 多行，`TuiInputHistory` 按字段名 50 条历史 + AOT 安全文本持久化
- **粘贴确认**：ChatScreen 和 TuiChatInput 粘贴超长(>500字符)或多行(>3行)时弹出确认
- **结构化记忆**：`.waycoder/memory/*.md` frontmatter 多文件 + MEMORY.md 索引，`memory` 工具与系统提示词注入均走结构化格式，首次使用自动从旧 memory.md 迁移
- **Diff 预览**：`WAYCODER_DIFF_PREVIEW=1` 开启，write_file/edit_file 写前逐 hunk 确认（Y/N/A/Q），非交互模式（管道/重定向/测试）自动跳过
- **Bash 安全防护**：`BashGuard` 三层拦截（命令名 + 参数 + 安全白名单），87 禁止命令，70 安全只读命令免确认
- **文件追踪 + Stale-Read 保护**：`FileTracker` SHA256 哈希记录 + 外部变更检测 + LRU 淘汰 + Agent 主循环注入变更警告（对标 Crush），防止 Agent 基于过期文件内容做决策
- **自动续写**：检测"口述代码"（content >300 字符 + 代码标记）→ 追问使其写文件；首轮只分析不动手 → 追问执行
- **自动摘要**：Crush 风格上下文预算检查 → 触发小模型压缩 → 注入继续提示 → 重置计数器
- **文档读取**：PDF 文本提取（PdfPig，AOT 兼容）+ Office 文档提取（DOCX/XLSX/PPTX，ZipArchive + XmlReader 零依赖）+ Markdown 结构化渲染 + CSV 表格解析 + HTML 标签剥离
- **SystemPrompt 对标 Crush**：`$"""` 原始字符串改用无 `$` 前缀+`.Replace()` 注入，避免代码示例中 `{` / `{{` 花括号导致 C# 插值解析错误。15 个结构化 XML 区块覆盖编辑/测试/错误恢复/任务完成完整指南
- **SHA256 循环检测**：每轮对（assistant 消息 + 工具结果）做哈希，8 轮窗口内相同哈希出现 3+ 次触发 3 级递进式反循环提示（换方法→重新评估→严重警告重置）
- **工具白名单/黑名单**：`WAYCODER_ALLOWED_TOOLS` / `WAYCODER_DISABLED_TOOLS` 环境变量控制 Agent 可用工具集合，构造函数中过滤，对主 Agent 和子 Agent 均生效

## 非显而易见的约束

- **孤立的工具消息是非法的**：压缩时必须保持 tool 消息紧跟其 assistant 消息
- **AOT 禁止反射**：不能用 `GetMethod`/`GetType` 等运行时反射
- **Markup 标记**：使用 `«»` 书名号 (`«color»text«/»`)，不与方括号 `[` `]` 冲突，无需双写转义
- **异步上下文**：`AsyncLocal<string>` 替代 `threading.local()` 用于 bash cwd 跟踪
- **每个重试独立 CTS**：渐进超时要求每 attempt 创建新的 `CancellationTokenSource`，不能用外部传入的单一 CTS
- **Hook 脚本兼容性**：stdout 非 JSON 时视为纯文本 `SystemMessage`，JSON 时按 `HookOutput` 协议解析；Decision 仅 PreToolUse 事件生效
- **DynamicBar 动画无定时器**：Braille 帧基于 `DateTime.UtcNow` 计算（不依赖定时器），ChatScreen 30ms 渲染循环确保动画流畅
- **Snip 阈值 4000 字符**：裁剪工具输出时保留首尾各 2000 字符 + 错误行（编译错误、异常堆栈），确保 Agent 能看到关键诊断信息

## 添加新工具 (C# 版)

1. 在 `Tools/` 创建类，实现 `ITool` 接口
2. 在 `ToolRegistry.cs` 注册
3. 在 `PermissionManager.cs` 决定是否需要确认
4. 在 `Test/SelfTest*.cs` 添加测试
