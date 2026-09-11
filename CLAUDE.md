# CLAUDE.md

本文件为 Claude Code（claude.ai/code）在此仓库中工作时提供指导。

## 项目概述

WayCoder（道码）是一个中文版易用编程智能体，C# (.NET 10) 实现，AOT 编译为单文件 exe。原名 CoreCoder，因商标冲突更名。

## 常用命令

```bash
# C# 版
cd WayCoder
dotnet publish -c Release            # AOT 编译
dotnet run -- --test                 # 5122 自测
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
- **配置架构**：全局 `~/.waycoder/config.json` 保存全部配置（Key 格式），**config.json 为唯一权威源，默认不使用环境变量**（含 .env 文件）；仅首次启动（无 config.json）时从 .env + 环境变量读取并**导入固化到 config.json**（此后环境变量不再被引用；删除 config.json 即回到首次启动状态）；**环境变量只保留引导级约 14 个**（服务商/模型/密钥/经济/鼠标/预算上限/工具白黑名单/Whisper 三项），其余配置项 EnvVar 置 null = 仅走 config.json（对齐竞品 Claude Code/Codex 的少量环境变量）；.env 仅 5 项基本引导配置（服务商/地址/API_KEY/经济模式/鼠标，作为删除 config.json 后的恢复引导源，普通启动不再读取）；API Key 走全局 `api_keys.json`（首次启动从环境变量 `ImportFromEnvironment` 导入，只补空不覆盖）；每次启动 config.json 有更新则同步一份到项目 `.waycoder/config.json` 本地备份（先验证文件正常才备份）
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
- **多行输入 + 历史**：`TuiDialog.Input()` 升级为 TuiTextArea 多行，`TuiInputHistory` 按字段名 50 条历史 + AOT 安全文本持久化；聊天输入框 `MaxColumnWidth=终端宽` 超宽自动折行、高度动态 1~5 视觉行超 5 行上滚、`MaxLength=32K` 上限（Rune 安全截断 `TuiTextArea.TruncateRunes`）
- **粘贴确认**：ChatScreen 和 TuiChatInput 粘贴超长(>500字符)或多行(>3行)时弹出确认
- **结构化记忆**：`.corecoder/memory/*.md` frontmatter 多文件 + MEMORY.md 索引，`memory` 工具与系统提示词注入均走结构化格式，首次使用自动从旧 memory.md 迁移
- **Diff 预览**：`/config DiffPreview true` 开启，write_file/edit_file 写前逐 hunk 确认（Y/N/A/Q），非交互模式（管道/重定向/测试）自动跳过
- **Bash 安全防护**：`BashGuard` 三层拦截（命令名 + 参数 + 安全白名单），70+ 禁止命令，47 安全只读命令免确认
- **`!` shell 直通**：TUI 输入 `!命令` 在操作系统 shell 执行（Win=cmd / 非 Win=bash），**输出加到聊天**（tool 纯文本，截取最后 500 行 `Program.TailLines`），裸 `!` 弹提示；`BashTool.ExecuteUserShellAsync` 跳过 Agent 黑名单（用户主动、红框警示）仅留绝对红线；输入框上下横线按前缀变色（`!`红 `/`青 `@`品红 `#`灰）——`ChatScreen.InputBorderColorFor` 纯逻辑
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
- **移动端五期（v0.96.15）**：**供应商/模型管理页** `ModelManagerPage`——供应商卡片图标（本地🌿/有Key🔑/无Key⚠️）、点供应商右滑进 `ProviderModelsPage`（模型列表显示上下文+价格、免费绿色、右上角大/小切换决定点模型设大或小模型、`大✓/小✓` 双选中勾）、📡 扫描连通性、**多源导入**（Web 版同款：内置/Claude Code/Codex/OpenCode/Crush/OpenClaw/自定义，文件选择器+正则启发式 → `ModelCatalog.AddCustom`）；**模型选择页** `ModelPickerPage`（TUI ModelPicker 移植：分组+搜索+大/小切换）；**文件类型路由** `SandboxFsService.DetectCategory`（源码→编辑器，图片/音频/视频/未知→系统应用）；聊天代码块等宽字体 `MonoFont`
- **移动端六期（v0.96.63）**：**多会话历史** `MauiSessions`（复用桌面 SessionManager file-per-session JSON，slot=-1 桌面可互读；Preferences 记当前会话 id 重启回上次；首启自动迁移旧单会话 `maui_session.txt`；每轮结束/退出自动落盘）；**左右抽屉**（ChatPage 左上 `≡`=会话历史左抽屉：固定头「＋ 新会话」+可滚动会话卡片首句摘要/相对时间/当前高亮、点击 `SwitchToSessionAsync`（跑中先停）；右上 `☰`=侧边栏右抽屉：模型横幅/命令区/模式区值行点按循环即时重建；`DrawerLayer` scrim 点击关闭 + 左右缘 Pan 开合 + 动画防重入同屏单侧，用 `TranslateToAsync`/`FadeToAsync` 新 API）；**工具调用分组 + 详情子页**（`ChatMessage.ToolCalls` 流式累积 Detail；按正文间断合并成组，流里一行 `🔧 工具调用:N 次` → `ToolCallsDetailPage`；`file_path=` 推语言）；**思考默认隐藏 + 子页**（「💭 查看思考」→ `ReasoningDetailPage`；思考只累积 reasoningSb 不污染正文流）；**正文/工具按时间交错**（AI 正文按工具边界切段独立气泡、工具组插段间 AI1/工具1/AI2…；段惰性创建、工具到先 FreezeSeg；每轮 `_toolGroup` 独立分组）；**Android 网络修复**（LLM/Transcribe/WebSearch 在 Android 强制纯托管 `SocketsHttpHandler`——默认 handler=Java HttpURLConnection 主线程读流抛 NetworkOnMainThreadException；`AgentService.ChatAsync` 改 `Task.Run` 后台 + 回调 `BeginInvokeOnMainThread` 泵回）；**Entry 去 Android 底线**（`MauiProgram` 给 `EntryHandler` 补 RemoveUnderline，原仅 Editor）
- **移动端七期（v0.96.64）**：**思考独立泡泡**（`ChatRole.Thinking` + 一行胶囊「已思考 N 秒」点开 `ReasoningDetailPage`；`ChatMessage.ThinkingSeconds` 计时；AI 气泡旧「💭查看思考」入口移除）；**工具组按思考/正文都交错**（间断判据 `interruptSinceTool` = 新思考块或正文段任一 → 下个工具新开组，连续工具同组不碎）；**抽屉浮层化**（宽度每次 `OnSizeAllocated` 按屏宽重算 62% clamp 200-300dp，勿一次性锁死——首帧 width 非终值会把抽屉顶到上限过宽；遮罩 `#33` 淡 + `DrawerLayer ZIndex=10` 置顶，聊天在遮罩下可见）；**模式/权限值读全局** `WorkModeManager.CurrentMode`/`PermissionManager.CurrentMode`（勿走 `AgentService.GetStatus()`——agent 未创建时 null fallback 写死「建造/Ask」，循环后 UI 不更新像「切不了」）；思考/工具消息不落盘存档（仅 User/Assistant）
- **移动端八期（v0.96.65）**：**弃抽屉浮层，会话历史/侧栏改 Shell 独立页**——MAUI Android 上把 CollectionView 聊天被抽屉覆盖或让位（Padding/Margin 改宽）都会导致内容不渲染「聊天空白」，覆盖又盖左对齐文字，抽屉方案反复踩坑；改为 `≡`→`SessionHistoryPage`、`☰`→`CommandPanelPage` 两个 Shell push 独立页，聊天页始终全宽内容永不丢；跨页会话切换经 `ChatPage.PendingOpenSessionId` 静态桥，聊天页 `OnAppearing` 消费并 `SwitchToSessionAsync`（抽屉死代码 v0.96.73 已清理，净删 355 行）
- **移动端九期（v0.96.66）**：**斜杠命令打开全部界面** `MauiCommands`（经 `CoreStubs.PluginRegistry.CollectCommands` 注入 `SlashCommandRegistry`）——`/home /chat /files /settings` Tab 切换（`//` 绝对路由）+ `/sessions /panel /modelpicker /providers /gitsync /about` 独立页 push + `/open <页面>` 主命令；命名规避桌面同名（`/model` 等仍桌面语义，另用 `/modelpicker`）；**/help 界面导航分组**：`HelpCommand` 把「描述以『打开』开头 / `/open`」命令从总表抽出顶部「📱 打开界面」成组（桌面无此类命令分组为空不受影响）；导航命令不进 `CommandBar.Favorites`（四端共享防污染桌面建议）
- **移动端十期（v0.96.69）**：**会话/切换 code-review 修复**——切换/新建会话必须先经 `AwaitActiveRoundEndAsync` 等旧轮 finally 彻底结束（`_activeRound` 完成信号；finally 已在旧 `_currentSessionId` 下 FreezeSeg+落盘）再清空/切 id，防残余写目标会话；`EnsureAgent` 建 agent 应用全局 `WorkModeManager.CurrentMode`（勿默认 Build 吞预设模式）；回调改道主线程后异常要包 try/catch（否则脱离 agent 控制流在主线程崩）；`FromNodes` 只取 user/assistant（跳过 tool/system 防假 AI 气泡+破坏共享 schema）；富文本节流状态段切换时重置；ToolCallsDetailPage 渲染设总预算并实时跟随
- **移动端十一期（v0.96.71）**：**会话持久化/队列复核修复**——`OnDisappearing` 只在非运行轮保存（运行中离页由轮末 finally 统一落盘，勿中途重置 `_appAddCount` 基线否则 finally 走 SaveRaw 丢回复）；`ProcessQueueAsync` 不设会话守卫 break（切换已 drain 清队，wind-down 新入队消息才有消费者）；`/clear`=先停轮再 `MauiSessions.Delete` 盘文件（防 finally 复活）；`DrainSendQueue` 直接移除排队气泡（勿留 Role=User+❌ 伪消息入库）；ToolCallsDetailPage 预算**按工具均分** `ShareFor(count)`（勿先到先得饿死后序关键工具）
- **移动端十二期（v0.96.72）**：**Android 恢复系统网络能力**——LLM/Transcribe/WebSearch 撤销强制 `SocketsHttpHandler`、恢复默认 handler（AndroidMessageHandler 保留系统代理/VPN/cleartext/用户 CA；前提网络不在主线程——agent 已 Task.Run 后台、UI 转录入口也 Task.Run）；**`MauiUi` 共享助手**（`Services/MauiUi.cs`：Res/ResOrNull/IsDark/PermName/EconomyName/FormatK/ModelText），移动端各页颜色/格式 helper 收敛到它（新页面取值一律走 MauiUi，勿各自 Resources 直取）；`Ensure/Switch` 载入渲染合一 `AppendHistoryNodes`
- **GUI（Avalonia）三条坑（v0.96.82 ~ v0.96.84）**：①**输入框 Enter 收不到**——`MainWindow.axaml` 上挂的普通（冒泡）`KeyDown` 处理器会被 `TextBox` 自己的**类处理器**跳过：`AcceptsReturn=True` 时它先消费 Enter（插换行 + `Handled=true`），`SendAsync` 永远走不到，表现为「按回车没反应、只能点发送按钮」。**别靠事件阶段（隧道）绕**，正解是 `ChatInputBox : TextBox` **覆写 `OnKeyDown`**（自己就是那个类处理器，无顺序歧义）+ `StyleKeyOverride => typeof(TextBox)`（否则按 StyleKey 查不到 `ControlTheme`，输入框退化成无边框无光标的裸控件）。②**聚焦变黑/有外框压不住**——控件上设 `Background="Transparent"`/`BorderThickness="0"` 是本地值，压不住 Fluent 焦点态：焦点视觉是**带伪类的样式触发**（优先级高于本地值）且画在**模板内部 Border** 上。正解：`App.axaml` 里对 `local|ChatInputBox` 及其 `:focus`/`:pointerover` 态、再加 `/template/ Border` 各覆盖一遍 + `FocusAdorner="{x:Null}"`。③**cwd 显示的取值**——GUI 无 AgentSlot 体系（`CoreStubs.GetSlots()` 返回空数组）、`/cd` 是只读信息命令，故工作目录即进程启动目录，与 `/cd` 报告值、`SandboxManager.AllowedDirectory` 同源；呈现复用 core 的 `PathStatus.FormatCwd`（与 TUI 状态栏同一套）
- **CLI 参数两条语义铁律（v0.96.85 / v0.96.86）**：①**有错即报错退出，绝不静默忽略**——`CliArgRegistry.Parse` 对未知/拼错选项（`--modle`）、位置参数（漏 `-p`）、必需值缺失一律 `✘` 到 stderr + 退出码 1；旧行为是 `continue` 跳过，用户以为参数生效了、实际以默认配置进了交互界面。裸位置参数本就不在用法（`waycoder [选项]`）内。**注意 `ExitCode` 有两种语义**：「解析出错」（1）与「终结型动作已处理、正常退出」（`--model list` 返回 0）——**别用 `ExitCode != null` 当错误判据**（写测试时因此假红过一次）。批量子进程 `BatchRunner.SpawnSelf` 以 `-p <任务> -y --model/--base-url/--api-key/--max-budget-usd` 启动自身，改名时务必同步（漏认一个则批量整批起不来，已有断言锁住）。②**CLI 启动参数一律「本次启动覆盖」，不写用户配置**——`--model`/`--base-url`/`--api-key` 走 `Global.PersistDisabled`（只改内存、不写盘），闸门设在三个写出口 `ConnectionConfig.Save()` / `Config.SaveToConfigJson()` / `Config.SaveToEnvFile()`。此前它们经 `ApplyModelChoice → SetActiveConnect` 连带写出 `connections.json` + `config.json` + **`.env`**，`--api-key` 还会永久写 `api_keys.json`。**顺序依赖**：`base-url` 必须在 `ApplyModelChoice` **之后**落到 `_config`，否则被 connect 推导地址覆盖——别把这几个赋值散出去。要永久保存走 `/model`、`/connect`、`/model key <供应商> <key>`
- **无参数启动全屏界面 = 「有画布 + 拿得到键盘」，不是「stdin 没被重定向」（v0.96.88）**：`RunReplAsync` 此前用 `Console.IsInputRedirected` 当「非交互」判据直接切管道模式，而 stdin 早被 `Main.ReadToEnd` 读过（有内容就成了一次性提示词）⇒ 那条分支只剩「stdin 是空的」，于是 `waycoder < /dev/null` **静默退出（零输出、退出码 0）**。被别的程序拉起、脚本调用、双击启动器、`waycoder < 文件` 都可能让 stdin 是管道而进程仍挂着可用控制台 —— 现在判据是 `ConsoleDevice.CanUseFullScreen(stdin重定向, stdout重定向, 能否开控制台设备, 是否Windows)`（纯逻辑可测）：stdout 被重定向=没有画布→不开；stdin 被重定向但 Windows 能开 `CONIN$` → **照常开 TUI**，读键走 `ConsoleDevice.OpenInput()` 返回的 `CONIN$` 流，**句柄必须交给 `WinConsoleMode.Enable(handle)`**（stdin 是管道时 `GetStdHandle(STD_INPUT)` 拿到的是管道、拿不到控制台模式）。真没有控制台时打印说明 + 退出码 1；**退出码要走返回值**（`RunReplAsync` 返回 `Task<int>`），`Environment.ExitCode` 会被 `Main` 末尾的 `return 0` 盖掉。Unix 不算这条路（`Console.ReadKey` 的 raw mode 绑在 stdin，单开 `/dev/tty` 没进 raw mode）
- **「助手已存在但被绕过」是本仓库最主要的重复形态（v0.96.90）**：全仓 137K 行过了一遍 6 路区域审查 + 机械检测（479 非测试文件 / 8 行窗口 / 跨文件重复 409 组），33 条里去重价值最高的那批**不是「没人写过」**而是「写过了、调用点绕过去了」——`GitRunner` 类注释写着「所有 git 调用都应通过此类」被 4 处绕过；`PathSafety.Guard` 被 6 处绕过；`UiText` 建来就是为消重、**零生产调用点**；`RunModalDialog` 注释写着「收敛约 8 份」而同文件 40 行外有 6 个私有方法没用它。**动手写新助手之前先 grep 有没有现成的**；**同一份数据/文件被两个工具读写时，读写必须单一实现**（`todo`/`struct_todo` 共用 `todos.json` 各写一套，已漂移成：状态词表分裂 + 一个原子写一个裸写 + 文案骗模型三处）。本版落地的单一真源：① `Tools/TodoStore.cs`（todos.json 唯一读写 = 锁 + `Global.WriteAllTextAtomic` + `ValidStatuses` 词表 + 依赖图）；② `Tools/FileWalker.cs`（递归遍历唯一实现，跳过判断**以 `FileIgnoreManager` 权威表为底**、各工具用 `extraSkipDirs` 显式追加噪音项——此前三份表互不相同导致「grep 查不到、find_replace 却改得到」）；③ `GitRunner`（**所有** git 调用走它：自带 `RedirectStandardInput` 隔离 + `GitTimeoutSec` + `ProcUtil.AwaitReadWithTimeoutAsync` 读超时；`SystemPrompt.RunGitCommand` 原来超时后仍无界 `GetResult()`，而那条路径每次构建系统提示词都走）；④ `ProjectInitializer.DetectTestCommand(root, userOverride)` / `DetectBuildCommand(root)`（构建/测试命令唯一真源，`Agent.Feedback` 转调——此前 `/init` 写进 AGENT.md 的命令与 Agent 实际执行的**给出两种答案**）；⑤ `UI/TUI/ChatRoleStyle.cs`（角色显示名/正文色/图标色唯一真源，此前四张平行表把 user/system 硬编码成亮白，使主题 6 变体 × 4 个 `Chat*Fg`/`Icon*Fg` 全成**死键**）；⑥ `SandboxManager.ContainmentReason` + `IsUnder`（路径包含判断唯一实现，**含 symlink 解析 + 路径段边界**——`CheckDirectoryEscape` 原来不解析 symlink，`cd` 逃逸可用「项目内 symlink 指向项目外」绕过；裸 `StartsWith` 还让 `/proj-evil` 通过 `/proj`）；⑦ `PktLine.ReadFrame`（pkt-line 非数据帧判据是 **`len < 4`** 不是 `len <= 1`：v2 的 response-end-pkt 是 `0002`，旧的宽容分支会 `new byte[-2]` 抛 OverflowException 打挂 clone）。另两条易踩：**`RenderBuffer.Write` 的 `fg:`/`bg:` 在 1..9 是样式码、不是颜色码**——`fg: 8` 是 SGR 8 conceal（字符直接不显示）而非 dim，要暗用 `AnsiTty.StyleDim`（=2），`TuiMenu` 的滚动条与分隔线就这么隐形过；**`WayCoder.Maui/CoreStubs.cs` 是桩类**——MAUI 排除 `UI/TUI/**` 却编译 `Tools/**` 与 `Agent/**`，真实现每加一个被这些文件用到的 public 成员都要同步补桩，漏了**只在 MAUI 上 CS0117**（桌面构建全绿看不出来）。
- **自测失败行必须直写真实 stdout（v0.96.94）**：`SelfTest.Report` 走 `Console.WriteLine`，而不少用例用 `Console.SetOut(StringWriter)` 捕获输出（渲染/对话框/编码类）—— **期间若有 Check 失败，❌ 落进那个 StringWriter 就被丢掉**，表现为「汇总 `失败: N` 但输出里没有任何 ❌ 行」，排查时完全无从下手（实测偶发过两次）。现在 `RunWithFilter` 在入口捕获真实 stdout，`Report` 在当前 `Console.Out` 已不是它时**额外直写真实 stdout**。**新写捕获输出的用例不必管**；但**新增任何「用 SetOut 捕获」的块时，别把 Check 放进捕获区**，或者接受失败行会走两遍。
- **`SkillsManager.FindSkillDirs` 的向上查找边界失效（既有，未修，待决策）**：它从 cwd 向上收集 `.waycoder/skills` / `.claude/skills`，终止条件是 `dir == Global.Home`；**只要 cwd 不在 `Global.Home` 之下，这个边界永不触发**，循环一路爬到盘根。后果：自测临时目录里做技能发现会扫到开发机真实的 `~/.claude/skills`，使「恰好发现 N 个技能」这类断言在环境里有该目录后稳定失败。这与 `ProjectContext.FindProjectRoot` 是**同一类坑**（CLAUDE.md 那条：「边界判定必须先于标志检测」）——写任何「从 cwd 向上找」的循环时，先想清楚**边界在什么条件下才会触发**。本版只把断言改成环境无关（断言创建的两个技能被发现 + 无重名），**没有改动发现语义**（对 `~/.claude/skills` 的兼容可能是有意的）。
- **重复代码清理的落地清单（v0.96.90 ~ v0.96.101）**：全仓过了一遍（6 路区域审查 + 机械检测，33 条），判据是「重复是否已在产生风险」，不是行数。**新增共享代码前先 grep 有没有现成的**（详见上一条）。已落地的单一真源：`Tools/TodoStore.cs`（todos.json 唯一读写）、`Tools/FileWalker.cs`（递归遍历 + 跳过判断，以 `FileIgnoreManager` 权威表为底）、`Tools/SsgfRedirect.cs`（跟随重定向 + 每跳 SSRF 校验；**重定向预算必须显式传** —— 此前三处是 5/10/5 无声地不同）、`Tools/WritePipeline.cs`（`ConfirmDiff` 逐 hunk 确认 + `RestoreCrlf`）、`Git/GitRunner.cs`（所有 git 调用）、`UI/Shared/UnifiedDiff.cs`（行级 diff 引擎）、`UI/TUI/ChatRoleStyle.cs`（角色配色）、`SandboxManager.ContainmentReason` + `IsUnder`（路径包含判断）、`PktLine.ReadFrame`、`TextEncoding.MatchBom`（BOM 表）、`UiText`（权限/经济文案）、`ProjectInitializer.Detect*`（构建/测试命令）、`Agent.ApplyRuntimeModel`（模型切换收尾）、`Global.WalkUpDirectories`（从 cwd 逐级上溯找配置，**终止要同时兜住 `parent == dir` 与 `parent == null`** —— `Path.GetDirectoryName` 对盘根返回 null）、`Maui/Markup/MarkdownTable.cs` + `MarkupToFormattedString.AppendCodeLines`（移动端表格判定与代码块高亮；合并时发现三处**并不等价** —— 聊天流那版缺「超大代码块降级纯文本」护栏，而那道护栏正是防移动端 ANR 的）、`ToolErrors.ErrorOpPrefix`（五个工具手拼的 `错误：{op}: …`）。
**续（v0.96.97 ~ v0.96.101）**：`UiText.RelativeTime`（相对时间阶梯；移动端那版漏了「N 周前」分支已漂移 —— 10 天前手机上「10 天前」、桌面上「1 周前」）、`Infra/Logging/RotatingFileWriter.cs`（日志文件句柄生命周期，**漏 `Flush` 丢日志、漏 `FileShare.Read` 把文件独占锁死**，两处各写一份时改一处忘另一处无编译期提示）、`UI/TUI/Base/TuiScrollMath.Paint`（滚动条落笔唯一实现，四处内联连 `"█"`/`"│"` 字面量都逐字相同）、`UI/TUI/Base/TuiScrollable.cs`（滚动状态机：`ScrollOffset` + 四个滚动方法 + 跟底标志 + `OnResize` 钳制；`TuiScrollView`/`TuiListView` 只剩 `ScrollStepLines` 一处不同 —— 这套是「跟底状态 × 边界 no-op × 标脏」交织的状态机，漏一处就是「滚一下永久失去自动跟底」或「手动上翻又被拽回底部」）、`TuiView.SetTreeDirty`（树标脏唯一遍历，`MarkDirtyTree`/`MarkDirtyTreeQuiet`/`MarkItemContentDirty` 共用 —— 后两者的差别只是**要不要叫醒帧闸门**）、`TuiWindow.Close(result, callback)`（关模态窗唯一出口，见下）、`ModelCatalog.RemoveProviderFromFile`。
**两条新踩坑（同批）**：① **`TuiScrollMath.Bar` 在 `total <= vis` 时滑块会长过视口**，`(long)(vis - thumb)` 变负 ⇒ `Math.Clamp(value, 0, 负数)` 的 min > max **抛 `ArgumentException`**。四处调用点此前各判一次 `total > vis`，属「四份守卫、漏一处即崩」—— 判据已收进 `Paint` 内部；**凡是收口几何计算的辅助函数，边界守卫也要一起收进去**，别留给调用方。② **关一个模态窗永远要按序做三件事**：写 `Result` → 跑调用方回调 → 触发 `OnClosed`（驱动出栈 + 唤醒渲染等待循环）。**顺序不可换**（回调正是唤醒等待线程的那一下，被唤醒的调用方可能立刻读 `win.Result`）。此前这三行在 `TuiDialog` 的 8 个构建器里手抄 47 处，漏 `OnClosed` ⇒ 窗口永不关闭、调用方一直挂着；漏 `Result` ⇒ 把「取消」读成「确认」。现在只有 `TuiWindow.Close(result, callback)`（+ 无结果的 `Close()`）。**`TuiScreen.OnKey` 的 Esc 兜底故意不走 `Close()`** —— 消息框（Info/Success/Warn/Error）**没注册 Esc 快捷键**，兜底关窗不该编造一个「结果」（保持 `Result = -1`）。这条事实**只有走「屏幕真实路径」（`ShowWindow` + `screen.OnKey`）的测试才测得到**，直接跑快捷键体会抛 `KeyNotFound`。
**三条反复出现的模式，下次直接按此排查**：① **「共享助手已存在但调用点绕过」占多数** —— `UxHelper.RunModalDialog` 的注释写着「收敛约 8 份」，同文件 40 行外 6 个私有方法没用它；`TuiControl.MouseInBounds` 有 13 处在用，还有 2 处手写 `GetAbsoluteX/Y` 边界判断（而那两处**恰好都在弹窗内**，正踩 `HitAbsX` 注释里记的「弹窗内点击错位」）。② **同一规则两处实现、只修了其中一处** —— `Detect` 缺 UTF-32 BOM 分支而 `Decode` 有（同一文件、同一张表，UTF-32 文件两条路径两种结果）；`todo`/`struct_todo` 共享 `todos.json` 却是两套读写。③ **必须手工同步的平行表** —— 权限/经济文案 6 套、MCP 状态图标 3 套、README/命令表。
**两条硬约束**：**跨端共享的纯逻辑必须放在 MAUI 也编译的目录**（`UI/Shared/`）—— MAUI 排除 `UI/TUI/**`，放错地方 Tools/Agent 在 MAUI 构建下引用不到，只能各抄一份（`UnifiedDiff` 从 `UI/TUI/Custom/DiffPreview.cs` 下沉正是因为踩了这个）；**迁移模态对话框到 `RunModalDialog` 时 `TResult` 取 `int?` 而非 `int`** —— 无约束泛型的 `TResult?` 对值类型不退化为 `Nullable<T>`，用 `int` 会让「回调未触发」（超时/异常）静默变成 `default(int) = 0`，而权限确认框的 0 是**允许**（`ShowConfirmDialog` 的 `?? 2` 就是保住默认拒绝）。
- **判据换了一半 = 没换（v0.96.89，v0.96.88 的收尾）**：v0.96.88 把「能不能开全屏界面」从 `Console.IsInputRedirected` 换成 `ConsoleDevice.CanUseFullScreen`，但**代码里还有一批地方把 `IsInputRedirected` 当「非交互」**，于是新开的那条路从读键、队列消费、逐 hunk 确认一路漏到控制台模式：①**读键泵闸门是另一条判据**（致命）——`InputManager.EnsurePumpStarted`/泵循环守卫仍判重定向 ⇒ `waycoder < NUL` 会进 TUI、建好 `WindowsCharSource(CONIN$)`、翻掉控制台模式，然后**按键全无反应**（泵是字符源唯一读者，且挂在它上面的心跳一起停摆，只有 Ctrl+C 能逃）。**「能开界面」与「会读键」是两条独立契约**，判据 `ConsoleDevice.HasKeyboard(stdin重定向, 是否来自设备)`，`Init` 按**实际建成的源**算 `_hasKeySource`；**纯谓词测试全绿也证明不了泵会启动**（自测进程自己就是重定向 stdin），必须用 `InputManager.SetSourceForTest` + `IsPumpRunningForTest` 驱动真实启动路径 —— 已用「把闸门改回 `Console.IsInputRedirected`」反证过：谓词全绿、驱动用例立刻红。②**`RunReplAsync` 是 `_pendingSlotQueues` 的唯一消费者**，它要画布 ⇒ stdout 被重定向会让 `-p1 "任务" > run.log` 的任务**静默不执行**。无界面路径（`RunSlotQueuesHeadlessAsync`）另有三条坑，都已修：**每槽位身份绑定**（`EnsureSlotAgent` 统一 `AgentId=F{n}` + `StructuredMemory.CurrentSlotIndex`，漏了会把 F4 的记忆写进 slot_0、`_agent_id` 全报 "main"）、**退出码与落盘**（`ProcessTextInput` 返回成败，任一条失败退 1 + `AutoSaveSession()`，否则 key 过期时「打一行失败、退出码 0」违反 CLI 铁律①）、**`--json` 契约**（`-p1`~`-p0` 时 `prompt` 为 null，Main 的 jsonMode 分支覆盖不到 ⇒ 会往 stdout 吐 ANSI 文本；现在 `--json` + 槽位任务不进 TUI，走 `RunOneJsonCoreAsync`，进度提示走 stderr）。**投递循环只有一份**（`RunSlotQueuesAsync`）—— 上面「身份绑定漏了」正是 headless 手抄一份时抄丢的。③**`Console.In.ReadToEnd()` 等 EOF** ⇒ 父进程给了管道却不写不关（`spawn` 默认 stdio / IDE 集成 / 启动器）会永远卡住（没窗口没提示没退出码）。判据改成「**首字节** 或 **EOF** 谁先到算谁」（`WaitAny`）：EOF 立即返回不白等；首字节到了就**此后不再设限**（慢生产方不被截断）；两者都没到（5s）才放弃并**在 stderr 说明**。**时限无条件生效**——按「有没有界面可回退」加条件会让 Unix（`CanUseFullScreen` 在重定向下恒 false）保留原挂死。读放后台线程、时限加在等待侧（`OpenStandardInput()` 非 overlapped，`ReadAsync(token)` 取消不了已发出的同步读）；两个事件**故意不 Dispose**（放弃后线程仍可能跑，对已释放的 `ManualResetEventSlim` 调 `Set()` 抛在线程 finally 里 = 未捕获异常 = 进程挂）。④**控制台模式是「每次进界面」的事**——裸 `!` 跑完 shell 命令 Exit→Enter 往返后没人重施 raw 模式（键要按回车才到、回显叠在自绘上），`TuiManager.Enter` 每次调 `InputManager.ReapplyConsoleMode()`（走 `Init` 记下的**设备句柄**，重定向下无参 `WinConsoleMode.Enable()` 返回 false 够不到 CONIN$）；`Dispose` **先还原模式再关流**（记的就是这条流句柄，顺序反了是往已关闭/已复用的句柄 `SetConsoleMode`）；`CanUseFullScreen()` 的探测句柄**必须释放**；`WinConsoleMode` 静态构造挂 `ProcessExit` 兜底；**`Dispose()` 里的 `Console.CursorVisible` 也要守卫**（`Init` 有、`Dispose` 没有 ⇒ 重定向 stdout 的进程里抛 `IOException: 句柄无效`，实测掀掉整个自测套件）。⑤**确认类弹窗（DiffPreview 逐 hunk / 向用户提问）判「有没有交互界面」用 `UxHelper.CanConfirmInline`**（= TUI 界面 ‖ 交互式终端），**别再裸判重定向**；⚠ 它在 `WayCoder.Maui/CoreStubs.cs` 有**同名桩类**——MAUI 排除 `UI/TUI/**` 却编译 `Tools/**`，真类每加一个被 Tools 用到的成员都要同步补桩，漏了只在 MAUI 上 CS0117（桌面构建全绿看不出来）。⑥`TuiDynamicBar` 的「内容变了」与「显式标脏」是两个**名字即意图**的方法（`MarkContentDirty` / `MarkRowInvalidated`），别再靠 `base.` 前缀区分 —— 那正是 v0.96.88 闪烁 bug 的成因（见下一条）。
- **动态栏直写必须「框架统一登记」（v0.96.88，致命）**：直写 spinner 与段级增量全靠 owner 门控（owner 必须是当前活跃屏幕），而 `RegisterDirectWrite` 原先只写着手写版 `ChatScreen.BuildLayout` 里 —— 默认界面却是**标记版 `MarkupChatScreen`**（覆写 `BuildLayout` 且不调 base，手写版只是 chat.tui 加载失败的兜底）⇒ 默认界面 `_owner` 恒 null、`CanDirectWrite()` 恒 false、`wholeRow` 恒 true、**整行每帧重写**（就是「空闲一直闪」本身），且 `--keypad` 与自测建的都是手写版 → 量到的「39 字节/帧」不代表用户那道界面。修法：`TuiScreen.RegisterDirectWriters()` 遍历控件树认领，`TuiManager.PushScreen/PopScreen` 在 `Activate()` **之后**调（标记版在 BuildLayout 里才建树）。**新增屏幕不必手写这一句**；新增整行控件别忘了同类坑。同轮 code-review 其余修复：`OnRender` 补过的段要**同步刷新段缓存**（否则同帧 `RenderDirect` 重写一遍）；整行判据要含**几何位移**（输入区变高会把动态栏挪一行，内容没变也必须整行重画）；遮挡期间**不**强制整行（改为「遮挡解除后的首帧」整行一次，否则模态遮罩上打亮行且又是逐帧整行重写）；覆写 `Invalidate()`（`MarkDirtyInRect`/`RootView.IsDirty`/主题切换绕过 `MarkDirty`，漏了会让「已脏但段没变」写出零字节）
- **刷新必须由「变化」驱动，绝不由「节拍」驱动（v0.96.79 / v0.96.80）**：两条实测踩坑，都是「内容没变却一直在闪」——
  ①**动态栏**：`TuiDynamicBar` 的 `Status`/`LeftText`/`TokenDisplay`/`CostDisplay`/`ContextPercent`… 原是普通自动属性（赋值不标脏），只能靠 `ChatScreen.SyncDynamicBar` 里「每 250ms 无条件 `MarkDirty()`」硬刷 ⇒ 按 spinner 动画节拍把整屏反复拖进渲染路径。**改法**：删掉定时器，spinner 动画由 `TuiDynamicBar.RenderDirect` 直写终端（不依赖脏标记，空闲也转）；内容属性走 `SetContent` —— **优先交给段级直写**，只有直写不可用（被遮挡/非活跃屏幕）才退回 `MarkContentDirty()` 走段级补写（**不是** `MarkRowInvalidated`/`MarkDirty`——那两个置整行标志，会在模态遮罩上打亮一行）。**粒度到段**（v0.96.81）：动态栏分 spinner/左段(状态)/中段(工具)/右段(📊⚡🔤¥)，内容与布局只有一份实现（`BuildLeftSegment`/`BuildMiddleSegment`/`BuildRightItems`，`OnRender` 与 `RenderDirect` 共用；右段必须连**绝对列**一起产出——各分支列步进不同，只给文本还原不了布局）；`OnRender` 记录各段已写内容，`RenderDirect` 只重写与之不同的段。**分区域刷新**（v0.96.87）：段级直写只管「内容变了写哪段」，管不住「内容没变却整行被重画」——增量渲染里叶子重绘判据是 `child.IsDirty || parentDirty`，**别的控件重绘会把本栏当父容器脏顺带带进来**，`OnRender` 就整行重写一遍（空闲态每秒约 20 次 = 整行持续闪烁）。**动态栏按区段刷新、各段时机不同**（spinner 每帧 / 左段只在状态变 / 中段只在工具变 / 右段 📊⚡🔤¥ 思考流式期间持续跳变）：`OnRender` 只在**四种情形**整行重写——**显式 `MarkDirty`/`Invalidate`**（无法确定本行是否被浮层/窗口擦过）、**全屏重绘/切屏**（`IsIncrementalUpdate == false`，清屏后段缓存坐标失效）、**首帧/几何位移**（输入区变高、压缩行增删会把本栏挪一行，段缓存的绝对列随之失效）、**遮挡解除后的首帧**；其余增量帧**只补变化段**，内容一字未变则整帧零写入。（**已废止**：早期版本的「直写不可用就整行重写」——遮挡期间 `SetContent` 只 `base.MarkDirty()`，走段级补写而非置 `_rowInvalidated`，否则每个 token 变化都整行重刷底色、在模态遮罩上打亮一行，见 v0.96.89 §6。注意 `SetContent` **不能**调覆写版 `MarkDirty()`，那个会置整行标志。）`MarkDirty` 覆写为「整行待重写」标志，遮挡期间保持置位 ⇒ **遮挡解除后的首帧必定整行重写一次**；`RenderDirect` 与 `OnRender` **共用段写入器与段缓存**，`OnRender` 补过的段会刷新缓存 ⇒ 紧随的 `RenderDirect` 只写 spinner，同帧不重复写。**右段签名须含颜色 + 绝对列**（📊 跨阈值是绿→黄同文本换色；CPU% 从 9%→100% 会把后续项整体推移，只比文本会漏）。**另一条**：`SyncDynamicBar` 里**每个内容属性每帧只许赋值一次**——「值变了就算内容变化」的语义下，同帧先置 A 再置 B 会被记成两次变化，直写不可用时即逐帧整行重绘。
  ②**聊天区**：流式追加时 `FlushStreamingLayout` 调 `ChatList.MarkTreeDirty()` → `TuiListView.OnRender` 整视口 `Fill` 擦除再重绘 ⇒ **每个流式 token（每渲染帧）把整片聊天区擦一遍**。**改法**：新增内容级脏窄路径 `TuiListView.MarkItemContentDirty(index)`，只擦该条目自己的行区间 + 末项底下的空档。
  **写任何"定时/每帧刷新"之前先问：内容真的变了吗？** 另两条硬约束：**擦了就必须重画**——`MarkItemContentDirty` 必须 `SetTreeDirty`（整棵子树）而非只标容器，因为 `TuiView` 的 `parentDirty` **只向下传播一层**，只标容器会让标题等叶子被擦掉后补不回来（实测表现：流式消息的「● 智能体」标题行变空白）；**位移就必须全量**——滚动偏移本帧变化（流式触发自动滚到底）时可视条目整体位移，必须退回整视口擦除，否则未标脏的条目留错位残影。诊断工具：`--keypad` 的 `FRAMES:<n>`（逐帧报告字节数 + 光标定位行），健康帧应只碰动画行与光标行（约 40 字节）
- **VT 字节流丢键修复（v0.96.78）**：Windows 读键自 v0.96.74 改走 VT 字节流后**丢失修饰键信息**，凡是「按字节还原按键」的映射漏一处就整键失效——已修三处：①**Backspace** 在 VT 下发 **DEL(0x7F)** 而非 BS(0x08)，漏映射 → `Key=NoName`，而编辑控件都按 `Key==Backspace` 判 → **退格擦不掉输入**；②终端未协商 Kitty（conhost/旧终端忽略 `CSI >1u`）时 **Ctrl+字母 = 控制字节 0x01..0x1A**，不还原成 Ctrl 修饰键则 Ctrl+P/E/M/B/S 全静默失效（`Program.Repl` 判 `Modifiers.HasFlag(Control)`）；③**F1-F4 走 SS3 形态 `ESC O P/Q/R/S`**（无 `[`），`TryParseEscapeSequence` 只认 `[` 则落进「Alt+字符」分支 → F1-F10 槽位键整排失效。**收敛点**：`WindowsCharSource.ToConsoleKeyInfo(char)` 是字节→ConsoleKeyInfo 的唯一实现（`TryReadKey` 与 `InputManager.ToConsoleKeyInfo` 共用）、`InputManager.MapSs3Key` 是 SS3 唯一映射；歧义码位 0x08(BS)/0x09(Tab)/0x0A(LF)/0x0D(CR)/0x1B(ESC) **保持既有语义不动**（Unix 上与 Ctrl+H/I/J/M/[ 同码，见 `TuiKeybindHelp`）。**测试铁律**：`KEY:`/`INJECT` 直接注入 `ConsoleKeyInfo`、**绕过了字节映射层**，这类问题只有 `--keypad` 的 **`RAWKEY:<hex>`**（真机字节路径）或直接喂字节的字节级自测能复现——新增按键必两者都覆盖
- **自测硬离线 + 项目根解析边界（v0.96.77）**：`Global.OfflineMode` 是自测/CI 的**硬护栏**（`SelfTest.RunWithFilter` 置位、`finally` 还原，生产恒 false）——①`LLM` 在**真正发包处**拒绝非本机端点（只拦发送，`Endpoint` 等展示路径不受影响）⇒ 跑测试不可能产生 token 费用；②`Config.Env.FindEnvFile` 不再发现 `.env`（临时 home 不在 cwd 祖先链上，「上溯到 home 为止」护栏会失效、一路走到盘根命中仓库根 `.env` 把真实密钥导进测试进程）；③`ModelCli.ProbeEndpointAsync` 跳过外部探测。**新写测试必须遵守此约定**——真要联网的用例走 `ProbeBaseUrlOverride` 之类的本地 mock 接缝，不要直连真实服务商。另一条铁律：`ProjectContext.FindProjectRoot()` 的**边界判定（home / 用户主目录 / 盘根）必须在项目标志检测之前**——home 下有个 `package.json`（很常见）就会让 home 被当成项目根，`DetectLanguages` 随即递归遍历整个 home（几十万文件），实测 `DetectProject` 从 88ms 恶化到 **12~36s**（生产路径每次构建系统提示词都要吃）；`UserProfileDir` 兜住 `HomeOverride` 场景，`WalkFiles` 的 `MaxDirsPerScan` 目录预算兜底
- **TUI Windows 输入统一字符源（v0.96.74）**：TUI 读键链路统一到 `UI/TUI/Base/CharSource.cs`（`WindowsCharSource`=OpenStandardInput VT 字节流 / `UnixCharSource`=ReadKey）+ `WinConsoleMode` P/Invoke 开 `ENABLE_VIRTUAL_TERMINAL_INPUT`；code-review 修复要点（桌面自测 5083）：①VT 下方向/功能键是裸 CSI（`ESC[A`、`1~..6~`）须在 `ParseCsiFuncKey` 显式映射，否则退化成裸 ESC 取消 agent；②字节流前提要清 `LINE_INPUT|ECHO_INPUT`（否则回显叠加 TUI 自绘=「鼠标乱码」疑因+行缓冲）；③`WindowsCharSource` 解码须状态化（跨读边界缓存续字节、代理对高位先返）防中文 emoji 乱码；④鼠标乱码/motion 泛滥仍待 Windows 真机复验

## 模式体系（三分钟版，竞品对标）

WayCoder 的模式参考 Claude Code / OpenAI Codex / Crush / Aider 划分为**四个正交轴**（完整版见 [docs/模式体系.md](docs/模式体系.md)）：

- **确认轴**（权限模式 `PermissionManager.Mode`，Ctrl+P · `/permit`）：管「何时打断确认」——Ask(必问)/Auto(改必问≈Ask)/SmartAuto(危必问)/Yolo(不问)
- **边界轴**（沙箱 `SandboxManager`，`/perm`）：管「能碰什么」（可写范围/网络）——对齐 Codex `sandbox_mode`，现状与确认轴纠缠（full-auto→Yolo 联动），待解耦
- **行为轴**（工作模式 `WorkMode`，Shift+Tab · `/mode`）：管「工具有没有 + 干什么活」——Build 全量（受经济模式管）/ Plan 只读白名单+精简提示词（有审批门）/ **Chat 纯聊天（0 工具 0 提示词）**；**槽位实例级**（`Agent.cs:91`）
- **省钱轴**（经济模式 `EconomyMode`，Ctrl+E · `/config economy`）：管「花多少 token」——提示词档位 + 压缩阈值 + 输出上限（Build 档删工具=用户既定特色，Chat/Plan 不受影响）

**决策链**：工具有没有 = 工作模式（Chat=0 / Plan=只读白名单 `WorkModeManager.PlanReadOnlyTools` / Build=白名单或经济精简）> 黑名单 > 全量；物理边界看边界轴；确认只看确认轴；省钱只看省钱轴。
**注意**：确认轴全局静态（多槽位共享）、行为轴槽位实例、边界/省钱轴全局 config；同名异义（Auto×4、`--permit tiny`→Chat 工作模式 vs 窗口 `--tiny`）见 docs/模式体系.md §5。

**快捷键一键一义**（v0.96.58 统一）：Ctrl+P=权限循环、Ctrl+E=经济循环（轴向层，主循环 `Program.Repl` 414/474/484 截走），**编辑器→`/edit`、输入建议条→输入 `/`·`!`·`#`·`@` 前缀自动弹出**；ChatScreen 不再绑 Ctrl+E/P/Q（防双重绑定「同一键两种含义」）。完整键表唯一事实源 = `UI/TUI/Controls/TuiKeybindHelp.cs` 的 `Groups`，底部行/文档据此维护。

**快捷键跨平台铁律**（Win/Linux/Mac 通用）：功能键用 `Ctrl+字母`（非信号/控制码）与 `Ctrl+方向/Home/End`；**禁用这些 Unix 坑键**——`Ctrl+C`(SIGINT 信号，系统键=退出)、`Ctrl+Z`(Unix 默认 SIGTSTP 挂起进程，已注册 `PosixSignal.SIGTSTP` 转「优雅暂停」且 `ctx.Cancel=true`)、`Ctrl+M`/`Ctrl+H`(Unix ≡回车/退格)、`Ctrl+S`/`Ctrl+Q`(终端流控 XOFF/XON)。终端拿不准的键一定配斜杠兜底（`/model` `/help` `/session`）；复制 `Ctrl+Insert`（Mac 无 Insert 键，用终端原生复制）。

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
- **Windows 进程输出编码**：cmd.exe 对**重定向管道**始终写系统 OEM 代码页字节（中文系统 GBK），`chcp 65001` 只改控制台代码页**不改管道字节**（实测）——「强制 UTF-8」对 cmd 不可行。正确做法是 `ProcEncoding.Apply(psi)` 按 `GetOemCP` 解码（所有 cmd 启动点必须调用）；剪贴板走 PowerShell 可显式 `[Console]::OutputEncoding=UTF8` 强制。新建任何启动 cmd/bash 子进程的代码都要调 `ProcEncoding.Apply`
- **本仓库工作区是 CRLF（`git ls-files --eol` = `i/crlf w/crlf`）**：`.gitattributes` 只写了 `* text=auto` + `core.autocrlf=true`，尚未 renormalize，所以**任何批量改文件的脚本都必须保住 CRLF**——Python `open(p, encoding=...).read()` 会把 CRLF 读成 LF，再 `.write()` 就写出 LF，结果 `git diff --stat` 显示整文件全变（实测 TuiDialog.cs 的 180 行改动变成 1944 行），真实改动被行尾噪音淹没。**写完立刻核对 `git diff --stat`**，插入/删除行数远超预期就先 CRLF 化：`open(p,'wb').write(open(p,'rb').read().replace(b'\r\n',b'\n').replace(b'\n',b'\r\n'))`。同理，**批量替换要按子串换而不是整行换** —— 整行换会把 `cancelBtn.OnClick = _ => win.OnClosed?.Invoke();` 这类单行 lambda 的接收者一并吃掉（实测踩过）

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
