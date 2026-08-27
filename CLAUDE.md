# CLAUDE.md

本文件为 Claude Code（claude.ai/code）在此仓库中工作时提供指导。

## 项目概述

WayCoder（道码）是一个中文版易用编程智能体，C# (.NET 10) 实现，AOT 编译为单文件 exe。原名 CoreCoder，因商标冲突更名。

## 常用命令

```bash
# C# 版
cd WayCoder
dotnet publish -c Release            # AOT 编译
dotnet run -- --test                 # 4106 自测
dotnet run -- -p "提示词"            # 一次性模式
dotnet run -- --watch                # Watch 模式 (监听 AI! 注释)
dotnet run -- --update               # 自动升级 (检查并自替换)
```

## 架构

```
WayCoder/
├── Program.cs         入口 + CLI + REPL (ANSI 全屏 TUI)
├── Agent.cs           主循环 (Stop Hook + WorkReporter + 10 阶段流水线)
├── AgentSlot.cs       多 Agent 工作区 (F1-F10 槽位切换 + 后台并行)
├── LLM.cs             LLM 客户端 (流式 + 渐进超时重试 + 任务花费追踪)
├── ContextManager.cs  Crush 风格上下文管理 (token 追踪 + 自动摘要 + 进度事件)
├── SessionManager.cs  会话持久化
├── SystemPrompt.cs    系统提示词 (对标 Crush coder.md.tpl，15 个结构化区块)
├── Config.cs          配置 (全局 ~/.waycoder/config.json 权威源 + .env 5 项最小引导)
├── WatchMode.cs        Watch 模式 (文件监听 + AI! 注释)
├── PermissionManager.cs 权限确认系统
├── ProjectContext.cs  项目检测 + CLAUDE.md 加载
├── ProjectInitializer.cs /init 项目初始化 (生成 AGENT.md，/init claude 生成 CLAUDE.md)
├── ReviewMode.cs      代码审查模式
├── FallbackLLM.cs     模型回退链
├── MemoryStore.cs     记忆系统 (旧格式, 迁移源)
├── StructuredMemory.cs 结构化记忆 (frontmatter 多文件 + MEMORY.md 索引)
├── MemoryRetrieval.cs  跨会话记忆检索 (TF-IDF + 时间衰减)
├── BackgroundTask.cs  后台任务
├── DebugLog.cs        调试日志
├── Test/              测试/调试/演示代码（SelfTest 自测 14 partial 文件 + Benchmark/Keypad/TuiAudit/TuiDemo）
├── Batch/             批量任务引擎 (BatchSpec 清单模型 + BatchRunner 多仓库并行/worktree 隔离)
├── Plugins/           编译期插件系统 (IPlugin SDK + PluginRegistry + [ModuleInitializer] 自动注册)
├── WorkReporter.cs    工作总结报告生成器
├── TaskProgress.cs    任务进度追踪
├── FileLockManager.cs 文件锁 (防并发修改冲突)
├── UI/                 终端 TUI 控件库 (36+ 文件)
│   ├── TuiCust/              自定义控件 + 对话框 (8 文件)
│   │   ├── ToolRenderers/      工具输出渲染器 (7 文件)
│   │   ├── ModelPicker.cs      模型选择对话框 (全屏 ANSI)
│   │   ├── FilePicker.cs       文件选择对话框
│   │   ├── CommandPalette.cs   命令面板
│   │   ├── DialogAction.cs     类型化 Action 结果
│   │   ├── DialogOverlay.cs    栈式对话框管理器
│   │   ├── DiffPreview.cs      diff 预览 + 逐 hunk 确认
│   │   └── DiffRenderer.cs     统一 diff 渲染
│   ├── TuiControls/          基础控件库 (17+ 文件)
│   │   ├── TuiButton.cs        增强按钮 (快捷键下划线/悬停)
│   │   ├── TuiButtonGroup.cs  按钮组 (水平/垂直/Tab导航)
│   │   ├── TuiScrollbar.cs    独立滚动条 (拖拽/滑块/自动隐藏)
│   │   ├── TuiDynamicBar.cs   动态状态栏 (Agent状态/工具/压缩进度)
│   │   ├── TuiKeybindHelp.cs  键盘快捷键帮助面板
│   │   ├── TuiToastQueue.cs   Toast 通知队列
│   │   ├── TuiMarkdown.cs     Markdown→ANSI 渲染 (ILazyItem)
│   │   ├── TuiListView.cs     懒列表 (二分查找+提前终止)
│   │   ├── TuiInput.cs        多行输入区 + 智能提示面板
│   │   ├── TuiComboBox.cs     下拉选择框
│   │   ├── TuiGrid.cs         网格布局
│   │   ├── TuiList.cs         列表选单
│   │   ├── TuiTable.cs        表格控件
│   │   ├── TuiTableList.cs    表格列表 (列头/选中/滚动钳制)
│   │   ├── TuiSpace.cs        空白占位 (布局留白/隔行, 不渲染不聚焦)
│   │   ├── TuiBox.cs          对话框
│   │   ├── TuiPrompt.cs       输入框
│   │   ├── TuiProgress.cs     进度条
│   │   ├── TuiBanner.cs       欢迎横幅
│   │   └── ILazyItem.cs       懒渲染项接口
│   ├── ScreenManager.cs 全屏缓冲 + 弹窗菜单 + 侧栏
│   ├── SettingsPage.cs  设置界面 (Schema 自动布局)
│   ├── WindowManager.cs 窗口管理器 (Z-order/模态/Toast)
│   ├── InputManager.cs  键盘+鼠标+resize 输入拦截
│   ├── MarkdownRenderer.cs Markdown 解析引擎
│   ├── TuiHelper.cs     CJK 宽度计算 + 文本工具
│   ├── TuiColors.cs     统一配色常量
│   ├── BoxBuffer.cs     矩形缓冲区基类
│   └── Gui/            GUI 占位（预留扩展）
├── Edit/               终端源码编辑器 (4 文件)
│   ├── Editor.cs       编辑器引擎 (光标/缓冲/渲染)
│   ├── Syntax.cs       语法高亮 (14 种语言)
│   ├── DiagnosticManager.cs Lint 诊断集成
│   └── Gui/            GUI 编辑器占位（预留扩展）
├── Infra/              基础设施 (16+ 文件)
│   ├── BashGuard.cs     命令安全防护 (70+ 禁止 + 47 安全白名单)
│   ├── FileTracker.cs   文件追踪 (SHA256 + 变更检测)
│   ├── ErrorLog.cs      统一错误日志 (四级 + 自动轮转)
│   ├── FileIgnoreManager.cs .gitignore + .waycoderignore 规则引擎
│   ├── DesktopNotifier.cs  桌面通知 (终端闪烁 + 响铃 + Toast)
│   ├── PdfExtractor.cs  PDF 文本提取 (PdfPig, AOT 兼容，分页)
│   ├── OfficeExtractor.cs  Office 文档提取 (DOCX/XLSX/PPTX, 零依赖)
│   ├── HooksManager.cs    Hook 系统 (8 事件 + JSON 协议 + 匹配器)
│   ├── IdGenerator.cs     加密安全 ID 生成
│   ├── LruCache.cs        线程安全 LRU 缓存 (TTL 过期)
│   ├── RetryPolicy.cs     智能重试策略 (指数退避 + 异常过滤)
│   ├── SnippetStore.cs    代码片段管理
│   ├── UpdateChecker.cs   自动升级 (版本比较 + RID 探测 + GitHub/Gitee 源 + 自替换)
│   ├── Logging/           结构化日志系统 (9 文件: ILogSink/Console/File/JSON)
│   ├── DrawEngine.cs     手搓绘图引擎 (文本 DSL → SVG/PNG，零反射，指令可扩展)
│   ├── DrawCanvas.cs    光栅化画布 (Bresenham 线/扫描线填充/粗线描边/渐变采样)
│   ├── DrawCommands.cs  内置绘图指令 (20+ 形状 + 变换 + 贴图/裁剪/图标模板)
│   ├── PngEncoder.cs / PngDecoder.cs / BmpCodec.cs / JpegCodec.cs  手搓图片编解码
│   ├── RasterImage.cs / ImageLoader.cs  像素缓冲 + 格式检测/编解码分发
│   └── TrueTypeFont.cs / FontFinder.cs  手搓字体解析 + 系统字体探测
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
    ├── StatTool.cs    PwdTool.cs     SkillTool.cs
    ├── DocTool.cs      DownloadTool.cs MultiEditTool.cs
    ├── AskUserQuestionTool.cs ExportTool.cs StructTodoTool.cs
    ├── JobOutputTool.cs JobKillTool.cs NotebookEditTool.cs 后台任务管理
    ├── ScreenshotTool.cs 抓屏（终端文本 / 桌面 PNG + OCR）
    ├── ViewImageTool.cs 查看图片（附加到下一轮，vision 模型「看图」）
    ├── TranscribeAudioTool.cs 音频转录（Whisper 兼容，补齐多模态音频输入）
    ├── DrawTool.cs 绘图（文本 DSL → SVG/PNG，零反射，指令可扩展）
    └── ImageConvertTool.cs 图片格式互转（PNG/JPG/BMP）
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
- **子智能体 shell 权限**：`bash` 不再列入 `SubAgentDeniedTools` 禁令（「工具层禁令」转「确认层管控」）——YOLO 模式直接放行（子智能体可跑 `dotnet build`/`run` 自测，不再盲写）；非 YOLO 模式逐条弹行内确认框提问申请（只读命令仍由 `BashGuard.IsSafeReadOnly` 自动放行）；`PermissionManager.ConfirmLock`（SemaphoreSlim）串行化并发弹框防抢键盘/渲染竞态；`rm`/`git`/`kill` 等危险工具禁令保留
- **多 Agent 工作区**：F1-F10 切换 10 个独立会话槽位，各占各的屏幕；状态栏 10 数字指示条（白底=当前屏，灰=空闲 绿=工作 黄=等权限 红=出错）；AgentTool.ParentAgent 切槽位时重绑
- **多会话真并行**：槽位 Agent 后台线程执行不阻塞主循环（`StartSlotTask`/`RunSlotAgentAsync`），运行中可自由切换；输出按槽位路由（活跃=实时写屏 `ChatScreen` 流式方法、非活跃=缓冲到 `AgentSlot.ChatMessages`，`RestoreTo` 展示）；路由决策与切换共享槽位 `AgentSlot.Sync` 锁原子完成（杜绝切换瞬间丢 token）；`Esc` 中断当前槽位 / `Ctrl+Z` 优雅暂停；退出/崩溃保存全部非空槽位（`_auto`/`_auto_slotN`）；`UseGlobal` 槽位经 `GetSlotLlm` 返回 `_llm.Clone()` 独立实例而非共享 `_llm`（共享实例并发 `ChatAsync` 会竞态读写 `ModelOverride`/`_reasoningBuffer`/`_reasoningShown` 等非线程安全字段，导致切槽位后任务「停止」）；**Web 版同构**：每个浏览器页面（SSE 客户端）用 `?client=<id>` 绑定一个槽位，`WebChatServer` 的 `WebSlot{Agent,IsBusy,Cts}` + `StartSlotTask` 后台 `Task.Run` 并发执行，`BroadcastTo(slot)` 只写该槽位页面，`AsyncLocal<int> _currentSlot` 把交互桥 `ask` 只发给发起该轮任务的页面——「开始/停止只对当前页面自己的 agent 有效」；**会话记录按槽位隔离**：`SessionManager` 各方法新增可选 `slot` 参数（默认 -1=全局，一次性 CLI `--resume`/`-c`/`--session-list` 沿用），传 0-9 时记录写入 `sessions/slot{N}/` 子目录；Web 层 `/session` 与 `/sessions/*` 按当前页面绑定槽位作用（`SerializeSessions(slot)` + `BroadcastTo(slot,"sessions")`）；TUI 层 `SessionPicker`/`/session` 命令/退出自动保存按 `ActiveSlotIndex` 作用（`_currentSessionId` per-slot 数组化，Ctrl+S 切换会话只改 `_agent.LlmClient.Model` 不污染全局默认模型，恢复 `_auto` 走「槽位 0 优先、回退全局」兼容旧会话）——「每个 slot 有自己的用户和智能体，各自保存自己的会话记录」
- **实例级工作模式**：`Agent.WorkMode` 实例字段替代全局 `WorkModeManager.CurrentMode`（全局仅作 UI 镜像），每个槽位 Agent 持有自己的模式，混合模式并行（A 槽 Plan + B 槽 Build）各自正确；`Agent.OnWorkModeChanged` 回调携带槽位索引——后台槽位批准计划后切回 Build 只通知正确槽位，不污染活跃槽位
- **内置自动升级**：`UpdateChecker` 版本检查优先 Gitee Releases（国内快）、回退 GitHub（`WAYCODER_GITHUB_REPO`/`WAYCODER_GITEE_REPO` 覆盖）；`/update` 检查、`/update now`/`--update` 自替换；纯逻辑（`CompareVersions`/`DetectCurrentRid`/`FindAssetName`）与网络/文件操作分离便于自测；Windows 落 `.new`+`upgrade.bat` 退出后替换重启、Unix 原子 `rename` 覆盖运行中二进制；`packaging/` 提供 winget manifest / brew formula / apt deb 打包 + GitHub Actions 发布工作流
- **AOT 编译：JSON 手写序列化**，`JsonHelper.SerializeArgs` 替代 `JsonSerializer`
- **权限系统**：bash/write/edit/agent 默认行内确认（三行黄底渲染），`/perm yolo` 跳过
- **计划审批门**：`WorkMode.Plan`（Shift+Tab 计划模式）下模型产出计划（文本、无工具调用）后不自动催促执行，而是就地弹审批框——批准则 `SetMode(Build)` 切回建造模式继续执行，拒绝则停止；`Agent.ShouldPromptPlanApproval(mode, contentLen)` 纯逻辑判定 + `ChatScreen.ShowPlanApproval` 对话框；`WorkModeManager.ModeChanged` 统一同步槽位持久模式与状态栏
- **项目初始化 `/init`**：`ProjectInitializer.GenerateAgentMd()` 扫描项目生成中文 AGENT.md（默认；`/init claude` 传 `fileName="CLAUDE.md"` 生成 CLAUDE.md 兼容 Claude Code；复用 `ProjectContext.DetectProject` + 构建/测试/lint 命令探测）；`InitCommand` 斜杠命令负责覆盖确认与写文件，生成后下次启动经 `ProjectContext.LoadInstructions` 自动注入系统提示词
- **MCP 状态管理 `/mcp`**：`McpManager` 结构化状态模型（`McpServerStatus` Connecting/Connected/Failed + `McpServerInfo` 不可变快照 + `McpServerState` 运行时状态）+ `ReloadAsync` 热重连（断开旧连接→移除旧工具→重连）；`McpCommand` 查看/重连，侧栏 MCP 区结构化显示，对标 Claude Code /mcp
- **MCP 三传输**：`McpTransport` 抽象基类 + `StdioMcpTransport`（子进程 stdio）/ `HttpMcpTransport`（Streamable HTTP：POST + SSE 响应流）/ `SseMcpTransport`（legacy HTTP+SSE 双端点：GET /sse 事件流 + POST /message，响应经 SSE 流推送回）；`McpManager.DetectTransport` 纯逻辑识别 + `SseMcpTransport.ResolveEndpointUrl` 相对端点解析；工具自动发现注册为 `mcp__<server>__<tool>`
- **MCP 资源/提示词**：`resources/list` + `resources/read` 注册为 `mcp__<server>__resources` 读取工具（省略 `uri` 列出、传 `uri` 读取）；`prompts/list` + `prompts/get` 每个模板注册为 `mcp__<server>__prompt__<name>` 工具（参数从模板 `arguments` 数组生成 inputSchema）；发现响应统一从 JSON-RPC `result` 字段读取（修复此前顶层读取导致工具发现为空的 bug）
- **双模型架构**：大模型做复杂任务，小模型做压缩/摘要，自动分工省钱
- **模型回退链**：一串 connect 名（`/connect chain <c1> <c2> ...` 设置），回退时 model+key+baseUrl 一起换（可跨服务商）；**开关默认关**（`FallbackEnabled` / `/connect chain on|off`）——关 = 只用当前模型失败即停，开 = 按链自动回退且消息明确去向 + 剩余链；真实运行时回退在 `Program.Repl`（`BuildFallbackChain`），`FallbackLLM` 供库调用
- **配置架构**：全局 `~/.waycoder/config.json` 保存全部配置（Key 格式，优先级 config.json > .env > 环境变量）；**环境变量只保留引导级约 14 个**（服务商/模型/密钥/经济/鼠标/预算上限/工具白黑名单/Whisper 三项），其余配置项 EnvVar 置 null = 仅走 config.json（对齐竞品 Claude Code/Codex 的少量环境变量）；.env 仅 5 项基本引导配置（服务商/地址/API_KEY/经济模式/鼠标）；首次启动无 config.json 时自动从 .env 迁移生成并精简 .env；每次启动 config.json 有更新则同步一份到项目 `.waycoder/config.json` 本地备份（先验证文件正常才备份）
- **模型唯一性按 (id, baseUrl)**：地址不同 = 不同服务商——同 id 不同网关地址的模型都保留显示（如 deepseek-v4-pro 分属内置 DeepSeek 与 OpenCode Go/Zen）；选择模型时保存所选模型的 `DefaultBaseUrl` + `ProviderId` 到槽位/配置（请求走对应网关）；`Find(id)` 内置官方优先兜底、`Find(id, baseUrl)` 精确匹配；gemini 内置地址走 `/v1beta/openai` OpenAI 兼容端点（LLM 端点拼接对 `/openai` 结尾去 `/v1` 前缀）
- **连接层三层模型（connect / provider / connection）**：`ConnectionConfig`（`~/.waycoder/connections.json` 分类存储 connects / connections / fallbackChain）——connect = {providerId, modelId} 命名条目（大/小模型各一个），provider = {name, baseUrl, apikey} 逻辑一体（name+base_url 在 providers.json、apikey 在 api_keys.json），connection = 大 connect 名 + 小 connect 名（切换连接大/小一起切，可不同服务商）。**「每次切换模型 = 切换 connect」**：`ApplyModelChoice`/`SetActiveConnect` 是统一入口，ModelPicker/ModelCli/Web/GUI/CLI 全部路由到它；`/connect <spec>` 双分隔符解析（connect名 / providerId.modelId / providerId/modelId / baseUrl:model / 裸模型名，`TryParseSpec` 纯逻辑可测）；`Ctrl+Shift+M` 循环切换；旧配置自动迁移；`WithModelOverrideAsync` 按小 connect 的 provider 重配 endpoint（跨服务商大小模型）；模型栏 `(provider)model` 且显示实际生效模型（回退标 `(回退)`）
- **文件锁**：FileLockManager 防止多 Agent 并发修改冲突，30s 超时自动释放；`Agent.AgentId`（F1-F10）+ `ExecuteToolAsync` 注入 `_agent_id` 到工具参数，跨槽位冲突按槽位归属检测（WriteFile/EditFile 读 `_agent_id` 报「文件被锁定」提醒，而非同源续期）
- **工具取消令牌**：`ICancellableTool` 接口——bash（流式 + 杀子进程）/ fetch / web_search / download / git（`WaitForExitAsync(ct)` + 取消时 `Kill(entireProcessTree)`）/ agent（子智能体透传 ct）中断时真正终止在途操作，取消抛 `OperationCanceledException` 向上传播（不吞）；区分「中断」与「超时」：`OperationCanceledException when ct.IsCancellationRequested` 重抛 vs `TaskCanceledException` 返回超时文案
- **Watch 模式**：FileSystemWatcher 监听文件变更 → 提取 AI! / AI? 注释 → 线程安全队列 → REPL 轮询执行
- **全屏缓冲 UI**：备用屏 + 每帧重绘 + 行内权限块 + 弹窗菜单 + 侧栏面板 + 居中对话框
- **UI 控件库**：`UI/` 目录封装 TUI 控件（未来拆分 Tty 底层 + View 视图），`UI/Gui/` 预留 GUI 扩展
- **工具输出渲染器**：`IToolRenderer` 接口 + `ToolRendererFactory` 工厂，每种工具独立渲染器（对标 Crush ToolMessageItem），bash/edit/write/agent 各有 emoji + ANSI 着色
- **Dialog Overlay 栈**：`DialogOverlay` 栈式对话框管理 + `DialogAction` 类型化结果（对标 Crush overlay + typed actions），Push/Pop/按 ID 替换 + Esc 关闭栈顶
- **懒渲染列表**：`ILazyItem` 接口（`MeasureHeight`/`IsRenderCached`）+ `TuiListView` 二分查找首可见项 O(log n)（对标 Crush List + Item 接口）
- **渲染缓存**：`TuiMarkdown._parsed` + `_lastContent` + `_lastMaxWidth` 三级缓存，`EnsureParsed()` 仅在内容/宽度变更时重解析（对标 Crush cachedMessageItem）
- **自定义单元格**：`TuiDataList`/`TuiTreeView`/`TuiTableList` 支持 `CellMarkup` 用 `.tui` 片段做单元格模板，`TuiMarkup.LoadCell(markup, vars)` 替换 `{key}` 占位符（AsyncLocal 并发安全）——向「布局写 `.tui`、逻辑写 code-behind」架构靠拢；`Load` 对叶子根自动包装 `TuiVBox`（任意控件可当 cell 模板）；cell 渲染前 `OnResize` 触发布局 + `ClampCellWidths` 递归钳宽防 DrawLine 直接写屏串列（超宽不裁剪）；TreeView `items="文档>概览"` 路径语法建树自动展开中间节点
- **模型选择对话框**：`ModelPicker.Show()` 全屏 ANSI 直写，21+ 模型按供应商分组，Tab 切大/小模型，实时搜索过滤，Ctrl+M 打开（对标 Crush models.go）
- **按钮组 + 独立滚动条**：`TuiButtonGroup` 水平/垂直布局 + Tab 导航 + 字母快捷键（对标 Crush button.go）；`TuiScrollbar` 拖拽滑块 + 鼠标滚轮 + 自动隐藏（对标 Crush scrollbar.go）
- **文件选择 + 命令面板**：`FilePicker.Show()` 目录浏览 + 文件搜索（对标 Crush filepicker）；`CommandPalette.Show()` 分类分组 + 模糊搜索 + 快捷键显示
- **行内权限确认**：`InlinePermission` 控件在聊天流中嵌入黄色交互确认块，Y/N/A/D 快捷键，参数着色（bash绿/path青），展开折叠详情（对标 Crush inline permission）
- **多行输入 + 历史**：`TuiDialog.Input()` 升级为 TuiTextArea 多行，`TuiInputHistory` 按字段名 50 条历史 + AOT 安全文本持久化
- **粘贴确认**：ChatScreen 和 TuiChatInput 粘贴超长(>500字符)或多行(>3行)时弹出确认
- **结构化记忆**：`.corecoder/memory/*.md` frontmatter 多文件 + MEMORY.md 索引，`memory` 工具与系统提示词注入均走结构化格式，首次使用自动从旧 memory.md 迁移
- **Diff 预览**：`/config DiffPreview true` 开启，write_file/edit_file 写前逐 hunk 确认（Y/N/A/Q），非交互模式（管道/重定向/测试）自动跳过
- **Bash 安全防护**：`BashGuard` 三层拦截（命令名 + 参数 + 安全白名单），70+ 禁止命令，47 安全只读命令免确认
- **文件追踪 + Stale-Read 保护**：`FileTracker` SHA256 哈希记录 + 外部变更检测 + LRU 淘汰 + Agent 主循环注入变更警告（对标 Crush），防止 Agent 基于过期文件内容做决策
- **自动续写**：检测"口述代码"（content >300 字符 + 代码标记）→ 追问使其写文件；首轮只分析不动手 → 追问执行
- **自动摘要**：Crush 风格上下文预算检查 → 触发小模型压缩 → 注入继续提示 → 重置计数器
- **文档读取**：PDF 文本提取（PdfPig，AOT 兼容）+ Office 文档提取（DOCX/XLSX/PPTX，ZipArchive + XmlReader 零依赖）+ Markdown 结构化渲染 + CSV 表格解析 + HTML 标签剥离
- **SystemPrompt 对标 Crush**：`$"""` 原始字符串改用无 `$` 前缀+`.Replace()` 注入，避免代码示例中 `{` / `{{` 花括号导致 C# 插值解析错误。15 个结构化 XML 区块覆盖编辑/测试/错误恢复/任务完成完整指南
- **SHA256 循环检测**：每轮对（assistant 消息 + 工具结果）做哈希，8 轮窗口内相同哈希出现 3+ 次触发 3 级递进式反循环提示（换方法→重新评估→严重警告重置）
- **工具白名单/黑名单**：`WAYCODER_ALLOWED_TOOLS` / `WAYCODER_DISABLED_TOOLS` 环境变量控制 Agent 可用工具集合，构造函数中过滤，对主 Agent 和子 Agent 均生效
- **Tiny 模式**：`--tiny [窗口]`（如 `--tiny 8k`）精简提示词 + 小窗口；无参自动探测（Ollama `/api/show` 真实 `context_length` → 目录 → 4K 兜底）
- **省 Token 模式**：`--economy [on|auto|off]` / `WAYCODER_ECONOMY` 三态开关，保持正常窗口——关=完整；开=精简提示词（砍 RepoMap/Git/记忆/10 阶段流水线）+ 压缩阈值 50/70/90→35/55/75 + 工具输出裁剪 4000→2000 字符 + `max_tokens` 32768→8192；自动=保持完整提示词，压缩/裁剪阈值按任务轮数复杂度动态插值（简单省、复杂保质量），配合 `/config EconomyPriority quality|balanced|cost`（默认 quality，先保质量再省费用）；与 Tiny 的区别是保留正常窗口、面向云端大模型省钱
- **视觉（多模态）支持**：`view_image` 工具把本地图片加入 `LLM.PendingImages` 队列，Agent 主循环下一轮在 `FullMessages()` 末尾注入为多模态 user 消息（OpenAI 格式 `content` 数组，base64 data URL）；`LLM.ModelSupportsVision` 门控——仅 gpt-4o/gpt-5/claude/gemini 等 vision 模型才注入，DeepSeek 等文本模型自动跳过避免 400；配合 `screenshot` 抓屏实现「看图修 bug」
- **音频（多模态）支持**：`transcribe` 工具把本地音频文件上传到 Whisper 兼容端点（`/v1/audio/transcriptions`，multipart）转成文字，补齐 Codex CLI/Gemini CLI 的音频输入短板；配置 `WAYCODER_WHISPER_MODEL`/`WAYCODER_WHISPER_BASE_URL`/`WAYCODER_WHISPER_API_KEY`（空 key 回退主 `WAYCODER_API_KEY`），支持 OpenAI Whisper / Groq / faster-whisper 任意兼容服务
- **批量任务引擎**：`--batch <JSON|文件>` / `--batch-repo <仓库> --batch-task <任务>` 多仓库并行处理——每个任务 `git clone` 到 `.waycoder/batch/jobs/<名>_<随机>` 独立副本，子进程以 `-p` 一次性模式 + `-y` 放行执行（进程级隔离 cwd/状态），`SemaphoreSlim` 控并行（1–16 默认 4）、单任务超时可配（默认 1800s，超时杀整个进程树），子进程复用父进程已解析的 `--model`/`--base-url`/`--api-key`/`--max-budget-usd`（避免 clone 目录无 `.env` 丢 key）；跑完输出聚合 Markdown 报告落盘 `batch-report.md` + 退出码（对标 Cursor 批量修复 / Aider 多仓库脚本）
- **编译期插件系统**：`IPlugin`/`Plugin`/`PluginRegistry`——`WayCoder/Plugins/` 目录放 `.cs` 文件 + `[ModuleInitializer]` 自动注册（AOT 无反射、随单文件 exe 分发），插件可贡献工具（并入 `ToolRegistry.AllTools`）与斜杠命令（并入 `SlashCommandRegistry.RegisterAll`）；与 SKILL.md/Hooks/MCP 三种扩展机制互补，同名覆盖、null 防御、按名卸载，详见 docs/插件系统.md
- **JSON 输出模式（IDE 桥接）**：`--json -p "任务"`（或 `echo "任务" | waycoder --json`）一次性模式静默执行 Agent（onToken/onTool/onToolOutput 全 null、不流式、不写 ANSI），stdout 只输出一个 `JsonResult.Build` 结构化 JSON 对象——`schema`/`success`/`answer`/`error`/`model`/`usage{prompt,completion,total_tokens}`/`cost_usd`/`duration_ms`/`changed_files`，退出码 0 成功 1 失败；供 VS Code 扩展、CI 脚本、外部工具直接解析，纯函数构建器便于自测（对标 Claude Code `--output-format json`）
- **移动端二轮（v0.96.12）**：代码片段语法高亮（`Markup/ToolOutputFormatter.cs` 按「«» 标记 → diff → 代码 → 纯文本」优先级渲染，`file_path=` 推语言；修复 write/edit diff 裸显示 `«bright green»` 字面标签 bug）；`MarkupToFormattedString.Convert` 支持 ```围栏 + markdown 表格（列宽补齐等宽 + 表头加粗）；编辑器「透明叠加」高亮（透明文字 Editor handler + 垫底高亮 Label，`SetHorizontallyScrolling` 不换行 + `SetOnScrollChangeListener` 横向平移同步 + 行号栏）+ markdown「预览」模式（`Markup/MarkdownPreview.cs`，表格 Grid 渲染）；输入框上方动态状态栏（`IDispatcherTimer` 100ms Braille 旋转 + 思考/执行工具/等待确认多态）；任务完成摘要（`LLM.Task*` + Stopwatch 追加大聊天）；首页模式/权限一键切换（`WorkModeManager.CycleNext`/`PermissionManager.CycleMode`）；应用图标 app.png（四角透明不设 Color 背景）；文件「用外部应用打开」（FileProvider `file_paths.xml` 只暴露 workspace + `Launcher`）；**ANR 修复**——相邻同色 Span 合并 + 超大降级纯文本 + 流式富文本节流（≥300 字符/120ms），否则大代码块拆出上万 Span 滚动卡死
- **移动端三修（v0.96.13）**：编辑器「只显示首行」根因 = `HighlightLayer` 的 `LineBreakMode="NoWrap"` 让 FormattedString 的 `\n` 不换行（行号未设 NoWrap 所以正常）→ 移除 NoWrap + `UpdateHighlight` 按最大行宽（CJK 双宽）设 `WidthRequest` 约束测量宽度；编辑器**只读默认**（`CodeEditor.IsReadOnly` + 「✎ 编辑/🔒锁定」切换，只读时禁撤销/重做/保存）+ `HorizontalScrollBarEnabled` 横向滚动条（行号独立列不覆盖）；工具栏改 `ImageButton` + SVG 图标（`Resources/Images/icons/`）；**会话持久化** `Services/MauiSessionStore.cs`——只存 User/Assistant 对话正文（RawText，AOT 手写长度前缀格式），**不存思考过程与工具返回结果**，进入弹「继续会话/新的会话」，每轮结束/离页自动落盘；聊天右上角 **☰ 菜单键**——模型选择/模式切换/权限切换/会话管理/任务管理（todo 列表）集中入口
- **移动端四修（v0.96.14）**：关于页改内嵌使用说明（不再读长日志）；**会话恢复空气泡修复**——`MauiSessionStore.Load` 不能用 `ReadAllLines` 分行读长度前缀（多行 RawText 被 `\n` 拆散截断），改 `ReadAllText` 整串索引按 `len` 精确读取

## 模式体系（三分钟版，竞品对标）

WayCoder 的模式参考 Claude Code / OpenAI Codex / Crush / Aider 划分为**四个正交轴**（完整版见 [docs/模式体系.md](docs/模式体系.md)）：

- **确认轴**（权限模式 `PermissionManager.Mode`，Ctrl+P · `/permit`）：管「何时打断确认」——Ask(必问)/Auto(改必问≈Ask)/SmartAuto(危必问)/Yolo(不问)
- **边界轴**（沙箱 `SandboxManager`，`/perm`）：管「能碰什么」（可写范围/网络）——对齐 Codex `sandbox_mode`，现状与确认轴纠缠（full-auto→Yolo 联动），待解耦
- **行为轴**（工作模式 `WorkMode`，Shift+Tab · `/mode`）：管「工具有没有 + 干什么活」——Build 全量（受经济模式管）/ Plan 只读白名单+精简提示词（有审批门）/ **Chat 纯聊天（0 工具 0 提示词）**；**槽位实例级**（`Agent.cs:91`）
- **省钱轴**（经济模式 `EconomyMode`，Ctrl+E · `/config economy`）：管「花多少 token」——提示词档位 + 压缩阈值 + 输出上限（Build 档删工具=用户既定特色，Chat/Plan 不受影响）

**决策链**：工具有没有 = 工作模式（Chat=0 / Plan=只读白名单 `WorkModeManager.PlanReadOnlyTools` / Build=白名单或经济精简）> 黑名单 > 全量；物理边界看边界轴；确认只看确认轴；省钱只看省钱轴。
**注意**：确认轴全局静态（多槽位共享）、行为轴槽位实例、边界/省钱轴全局 config；同名异义（Auto×4、`--permit tiny`→Chat 工作模式 vs 窗口 `--tiny`）见 docs/模式体系.md §5。

## 非显而易见的约束

- **孤立的工具消息是非法的**：压缩时必须保持 tool 消息紧跟其 assistant 消息
- **AOT 禁止反射**：不能用 `GetMethod`/`GetType` 等运行时反射
- **中间格式渲染**：所有格式消息（text/markdown/code/…）的颜色与文字特征统一用 `«»` 书名号表达（`«color»text«/»`、`«bold»`、`«dim»`、`«underline»`、`«italic»`），**禁止在内容层硬写 ANSI**；由各平台渲染器决定呈现——CLI/TUI→ANSI（`SpectreToAnsi`）、Web→HTML（`markupToHtml`，颜色值同源 `TuiColors`）、GUI→富文本。Shell 命令产生的裸 ANSI 属外部数据，Web 端经 `ansiToHtml` 解码，CLI/TUI 直接透传终端
- **异步上下文**：`AsyncLocal<string>` 替代 `threading.local()` 用于 bash cwd 跟踪
- **每个重试独立 CTS**：渐进超时要求每 attempt 创建新的 `CancellationTokenSource`，不能用外部传入的单一 CTS
- **Hook 脚本兼容性**：stdout 非 JSON 时视为纯文本 `SystemMessage`，JSON 时按 `HookOutput` 协议解析；Decision 仅 PreToolUse 事件生效
- **DynamicBar 动画无定时器**：Braille 帧基于 `DateTime.UtcNow` 计算（不依赖定时器），ChatScreen 30ms 渲染循环确保动画流畅
- **Snip 阈值 4000 字符**：裁剪工具输出时保留首尾各 2000 字符 + 错误行（编译错误、异常堆栈），确保 Agent 能看到关键诊断信息
- **字符串截断必须按码点（Rune）**：禁止用 `[..N]`/`[^N..]` 任意索引切片或逐 `char` 遍历截断——emoji/CJK 扩展 B 是 UTF-16 代理对（2 个 `char`），会切半产生 U+FFFD。统一走 `ContextManager.TruncateByRunes`（截头）/`TruncateTailByRunes`（截尾）/`AnsiString.TruncateByWidth`（按显示宽度）或 `text.EnumerateRunes()`；空格等 BMP 字符定位的 `[..lastSpace]` 切片天然安全，无需改

## Android 项目强制编码约束

> 移动端（`WayCoder.Maui`，.NET MAUI，Android + iOS）涉及权限 / 路径 / 私有存储的代码必须遵守以下 8 条铁律。MAUI 用 `Permissions` / `FileSystem` API 封装了 Android 原生 `checkSelfPermission` / `registerForActivityResult` / `Context.FilesDir`，落地映射随条标注。

1. **路径语义总则——桌面端可用绝对路径，手机端只能相对路径**：桌面端用户与 Agent 直接操作真实绝对路径；手机端（Android/iOS）沙箱模型下一切路径都以 app 私有目录（`Global.Home` = `AppDataDirectory`）为根、**只按相对路径工作**——UI 层经 `SandboxFsService.ResolveInSandbox` 把相对/绝对路径统一钳制进 workspace 根内，绝对路径一律不得作为用户可操作对象流出。跨平台代码涉及「绝对 vs 相对」语义差异时用平台分支区分，**不得把桌面端绝对路径假设带到移动端**（这是 `75794b9` 批量 UserProfile→Global.Home、以及 SandboxManager 边界轴 mobile 语义的根因）。

2. **危险权限必须双管齐下**：凡用到危险权限（麦克风 / 相机 / 存储 / 通知 / 定位…），必须同时做两件事——① 在 `Platforms/Android/AndroidManifest.xml` 添加对应 `<uses-permission>`；② 写完整运行时权限检查：`Permissions.CheckStatusAsync<T>()`（对应 `checkSelfPermission`）→ 未授权则 `Permissions.RequestAsync<T>()`（MAUI 内部走 `ActivityResult`，对应 `registerForActivityResult`，**禁止直接调废弃的 `requestPermissions`**）→ 处理「拒绝」「不再询问」两种失败场景。示例见 `ChatPage.StartRecordingAsync()`（麦克风）。

3. **禁止硬编码路径**：禁止写死 `/data/data`、`/sdcard` 等字符串路径，一律通过 API 取真实路径——`FileSystem.Current.AppDataDirectory`（对应 `Context.FilesDir`）、`FileSystem.Current.CacheDirectory`（对应 `Context.CacheDir`）、`Platform.AppContext.GetExternalFilesDir(null)`（对应 `Context.ExternalFilesDir`）。

4. **私有存储父目录不可越界**：app 私有目录 `/data/data/[pkg]` 只能访问自己的 `files`/`cache` 子目录，**不能直接枚举 / 访问父目录 `/data/data`**（那是所有 app 的私有数据父目录，无权限，`Directory.GetFiles` 会抛 `UnauthorizedAccessException`）。向上遍历路径时必须在 `Global.Home`（app 私有目录）处停止，或对枚举加 try-catch 兜底（见 `ProjectContext.FindProjectRoot()`）。

5. **权限申请时机**：在调用受保护功能**之前**先校验权限，不要假设权限已授予。每次进入相关功能都要 `CheckStatusAsync`，未授权先申请、再操作。

6. **拒绝权限要有降级逻辑**：用户拒绝权限不能直接崩溃，要给出可用的降级路径（如提示「已取消」、禁用按钮、回退到备选方案）；「不再询问」时提供**跳转系统应用设置页**入口（`AppInfo.Current.ShowSettingsUI()`）。

7. **Android 13+ 用新版权限常量**：通知用 `Permissions.PostNotifications`、照片 / 视频 / 媒体用 `Permissions.Media` / `Permissions.Photos`（对应 Android 13 的 `READ_MEDIA_IMAGES` 等新常量），**不要**用旧的 `READ_EXTERNAL_STORAGE` / `WRITE_EXTERNAL_STORAGE` 存储权限。

8. **输出代码必须完整可编译**：涉及权限的代码必须输出完整样板（manifest 声明 + 运行时检查 + 拒绝降级），**不得省略**任何权限相关样板。

## 添加新工具 (C# 版)

1. 在 `Tools/` 创建类，实现 `ITool` 接口
2. 在 `ToolRegistry.cs` 注册
3. 在 `PermissionManager.cs` 决定是否需要确认
4. 在 `Test/SelfTest*.cs` 添加测试
