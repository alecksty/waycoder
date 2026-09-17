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
- **连接层三层模型（connect / provider / connection）**：`ConnectionConfig`（`~/.waycoder/connections.json` 分类存储 connects / connections / fallbackChain）——connect = {providerId, modelId} 命名条目（大/小模型各一个），provider = {name, baseUrl, apikey} 逻辑一体（name+base_url 在 providers.json、apikey 在 api_keys.json），connection = 大 connect 名 + 小 connect 名（切换连接大/小一起切，可不同服务商）。**「每次切换模型 = 切换 connect」**：`ApplyModelChoice`/`SetActiveConnect` 是统一入口，ModelPicker/ModelCli/Web/GUI/CLI 全部路由到它；`/connect <spec>` 双分隔符解析（connect名 / providerId.modelId / providerId/modelId / baseUrl:model / 裸模型名，`TryParseSpec` 纯逻辑可测）；`Ctrl+N` 循环切换（原 `Ctrl+Shift+M`：组合键在 Windows 上不可靠，终端抢键 + VT 字节流丢 Shift 修饰键）；旧配置自动迁移；`WithModelOverrideAsync` 按小 connect 的 provider 重配 endpoint（跨服务商大小模型）；模型栏 `(provider)model` 且显示实际生效模型（回退标 `(回退)`）
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
- **行内权限确认**：`ChatScreen.InlineChoice`（一个 `TuiPromptBar` 实例）钉在**输入框下方**，❯ 箭头 + 黄底高亮 + 每项一行说明；键位 ↑↓/Home/End 移动、Enter 确认、Esc 拒绝、Y/N/A 单键、1-9 直选、多选 Space 勾选、多题 ←→/Tab 翻页，栏下方常驻键位提示行。**权限确认 / 计划审批 / 粘贴确认 / 通用确认 / 退出确认 / 设置页 select 全走它，CLI/TUI 不再弹框**（Web/GUI/MAUI 仍弹框，分界点是 `TuiManager.ActiveScreen is ChatScreen`，见 CLAUDE.md）。旧 `InlinePermission`（聊天流内嵌黄块）是死代码，已无生产调用
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
- **移动端十三期：文件页接上 VML 编译链（v0.96.171）**：用户定的是「**在文件管理界面直接操作**，比在命令行页敲命令简单好用多了」——文件页负责挑文件与做决策，命令行页负责执行与**显示输出**。① **文件名按「在 VML 这条线上是什么」分色**：能编译的源文件 **绿**、`.vml` **橙**、`.vmb` **红**，**其余文件（含 README.md 这类 VML 编不了的源码）一律不变色** —— 颜色只用来标记「这个能编 / 能跑」，满屏彩色反而看不出重点（用户纠正过一次配色，别凭「源码就该是绿的」想当然）。「能编译的扩展名」直接问上游那 22 个编译器的注册表（`PluginManager.GetAllFrontendCompilers().SupportedExtensions`），**不另列一张表**；判据落在 `SandboxFsService.DetectVmlRole/FsEntry.Vml`（四档 `None/Compilable/Assembly/Binary`），菜单项与配色都从它推。颜色值在 `Colors.xaml`（亮/暗成对），C# 侧只做「角色 → 资源名」映射。② **菜单按角色给入口**：源文件 →「VML 编译」+「VML 运行」；`.vml` → 两项都有（编出 `.vmb`）；`.vmb` → 只有运行（已是终态）。产物名走唯一那份 `MauiVml.NextArtifact`（`main.c`→`main.vml`→`main.vmb`，与上游 CLI 默认产物同名同形）。③ **同名产物已存在问「覆盖/重命名/取消」三选**（不是「确定要覆盖吗」两选：产物是用户可能手改的文件，静默覆盖不可逆；只给两选则想保旧产物的人只能退出去改名再回来）。**询问留在文件页**（得在看得见文件列表的地方问），命令行页只执行。④ **跨页交接交「作业对象」不交命令文本**：`ShellPage.PendingVmlJob`（`record`，含 Compile/SourcePath/OutputRel），`OnAppearing` 里 `Interlocked.Exchange` 取走。**别拼 `vml run <路径>` 再喂给自己** —— 命令是按空白切分的（`ShellCommandRegistry.Split` 不做引号解析），路径带空格就断成两截。没切过去要把信箱清掉，否则用户下次碰巧进命令行页会莫名其妙跑起一个程序。⑤ **运行时报错必须看得见**：VML 运行时的报错（`内存错误(PC=…)`/`标签错误`/`未预期崩溃` + 16 个寄存器 dump）全走 `Console.Error`、`Permission denied: syscall N` 与 `VM execution cancelled` 走 `Console.Out`，而手机上那是**一个看不见的流** —— 这正是「程序明明崩了、用户只看到没有输出」的根因。现在 `RunProgram` 在运行期间把两个流接到 `StringWriter` 并进返回值（`Console.SetOut/SetError` 是**进程级**的，故用静态锁串行化；收进来的内容是**并进输出**而不是丢掉，只多不少）。⑥ **运行中不许直接离开**：`ShellPage.OnBackButtonPressed` 拦下来先问「是否强制停止」，「继续运行」= 不停止 = 不返回。强制停止走 `VmRuntime.Run(ct)`（主循环**每条指令**查一次 token，实测死循环 207ms 内停住）。**「等它收干净再重发返回」的信号必须是 `finally` 最末尾置位的 TCS**，不能 `await` 那个运行 Task —— 它和 `ExecVmlAsync` 里那句 `await task` 挂在**同一个完成回调**上，谁先恢复没有保证，抢跑就会看到 `_runCts` 还没清、撞上同一个拦截弹第二次框（套娃）。重发返回走 `OnBackPressedDispatcher`（`#if ANDROID`），是"已经停了"的状态下让系统按它原本的规矩办。**只拦 VML 运行**：普通 shell 命令没有中断入口，弹一个停不掉的"强制停止"是骗人（`_runCts is null` 就放行）。⑦ **VML 窗口去掉重复标题**：Shell 标题栏已显示 `scene.Title`，页面里那个自绘 `HeaderLabel` 写着同一句话 ⇒ 屏幕上两个一样的标题，删掉页面里那个、标题只留 `Page.Title` 一处。⑧ **手柄区可收起**：画布与手柄之间一条折叠条（左右细线 + 中间「▲ 收起手柄」），隐藏整块 `Auto` 行自然塌成 0，画布立刻多出约 150dp；箭头旁带一句话说明，免得"▲ 是收起还是展开"要靠点一次才知道。⚠ 画布高度变了但 VML 程序**在开窗那一刻**就问过 `SCREEN_W/H` 排好版了（协议里没有"尺寸变化"消息），别指望跑着的游戏跟着重排。⑨ **真机挖出来的两个坑（都是「点了没反应」，构建全绿、只有上手机才暴露）**：**(a) `Shell.Current.GoToAsync("//shell")` 直接抛 `ArgumentOutOfRangeException`** —— 用户看到的是点「VML 运行」弹「无法打开命令行页」。Shell 的绝对路由串要一路穿过 `TabBar → Tab → ShellContent` 三层，而 `AppShell.xaml` 里那几个 `<Tab>` **没有显式 `Route`**（MAUI 自动生成的），`//<ShellContent 的 Route>` 这种写法在这里解析不到 —— 报的还不是「路由不存在」，是**路由解析器内部越界**，光看错误信息根本猜不到。正解是**别用路由字符串**：`ShellPage.SwitchToShellTab` 遍历 `shell.Items → item.Items → section.Items` 找到 `Route == "shell"` 的 ShellContent，把三层依次设为 `CurrentItem` —— 这就是点 Tab 时系统做的事。**(b) `async void` 处理器里的异常会被静默吞掉**：`FilesPage.OnSelectionChanged` 是 `async void`（事件签名定死），里面任何一处抛出都是进程级未处理异常；MAUI 在几条路径上还会先吞掉，现场只剩应用自己 `FirstChance` 日志里一行 `ArgumentOutOfRangeException`，**连是哪一步炸的都看不出来**。整个处理器必须自己 try/catch + 把**堆栈**落进 `ErrorLog`。同理文件页递来的作业是 `Dispatcher.Dispatch(() => _ = RunPendingVmlJobAsync(job))` 起的 —— 那个 `_ = ` 把 Task 丢掉了，异常只变成「未观察的任务异常」（实测：一个例子编译时抛 `未找到标签: asm`，屏幕上一片空白）。⑩ **「运行中 / 已结束」的边界**（用户报「摸不着头脑」）：`路径>` 提示符 —— 起点写 `~/examples> vml run c/gomoku.c`，终点再写一行空的 `~/examples>` 表示「等下一个命令」，运行中输入框那一格显示 `⋯`。三个入口（手敲 / 文件页运行 / 文件页编译）共用 `RunWithPromptAsync` 一个外壳，免得谁漏掉收尾提示符。⑪ **路径缩写成 `~/…`**（工作区根 = `~`，`SandboxFsService.Abbreviate` 复用 `ToRelative`）：手机上 `/storage/emulated/0/waycoder/workspace/examples/gomoku.c` 一行放不下、换行后更看不出重点；顶栏那行 `cwd:` 同时去掉 —— **同一屏说两遍同一个信息**也是用户会立刻挑出来的。
- **VML 手感接口：先查 VM 内置有没有，别急着加平行接口（v0.96.172）**：用户要「播放声音、震动」让游戏更丰富。**先做了一遍「有没有现成的」** —— 结果 VM 的 `SyscallNumber` 里已经有 `#50 Random` / `#51 Seed` / `#53 GetTick` / `#54 GetDateTime` / `#55`/`#56` 日期时间串 / **`#57 SpeakerBeep`** / `#106`/`#107 EEPROM`。于是原计划的 12 个接口砍成 9 个：**随机数与取时间不做**（`ui_rand` 就是包着 `#50` 的），**音效也不新增 `AUDIO_TONE(540)`，而是把 `#57` 接通**。① `#57` 之所以「调了没反应」，是因为它交给 `VmSpeakerDevice` —— 那个设备**只把样本记进内存 / 写 WAV 给测试用，不发出任何声音**；而宿主处理器**在内置 switch 之前**被调用，所以截得住（这是该设计允许的用法）。**只截这一个内置号**，`Handles()` 仍只认 500–599 —— 放宽会把别的内置 syscall 一并吞掉，那是最难查的一类故障。② 音效是**现场合成**的（频率/时长/波形 → PCM → `AudioTrack` / `AVAudioEngine`）：游戏不用带音频素材、不涉版权、没有解码器兼容问题；合成必须加 **3ms 淡入淡出**，否则方波头尾有「咔」的爆音。③ `VIBRATE` 是 normal 级权限（装上即生效），**漏了声明的表现是「调了没反应」且 `Vibrate` 静默失败**；模式震动 Android 走 `VibrationEffect.CreateWaveform`，iOS 没有公开 API、退化成「按总时长振一下」。④ **`STORE_*` 的键要加 `vml.` 前缀并清洗**（与 App 自己的 Preferences 共用一个存储，不隔离就是「用户改个设置把游戏存档冲了」）。⑤ **iOS 与 Android 各写一份实现**：平台 API 完全不同，抽一层接口只是把 `#if` 挪个地方；两边必须保持**同样的钳位/包络/单通道语义**。⑥ **宿主处理器里凡是碰 View 的都要 marshal 回主线程** —— 真机实测 `DeviceDisplay.KeepScreenOn` 会抛 `Only the original thread that created a view hierarchy can touch its views`（它最终调 `Window.AddFlags`）；只碰数据的（Preferences / Vibrator / AudioTrack）才能直接调。接口清单与设计约束**存档在 `docs/VML宿主接口.md`**，加接口先改那份。
- **「卡住出不来」要三个出口（v0.96.172）**：① **编译看门狗** —— 前端编译是同步的、`IFrontendCompiler` 上**没有任何取消入口**，源码里有让编译器自己陷进去的写法时，**从前的链上没有任何出口**：界面永远停在「正在编译…」，连「强制停止」都按不动（那个 token 只作用于运行阶段）。现在把编译丢到独立线程、主线程**带超时地等**（180 秒 —— 手机上一份 C 程序实测一分多钟，值必须明显大于合法耗时否则误杀正常程序）；⚠ 超时后**那个编译线程还在跑**（.NET 没法中止线程），这只是「把控制权还给用户」，不是「杀掉编译」。② **取消源改成静态**（`ShellPage.CancelRunningVml`）—— 游戏窗口才够得着它；退出窗口 = 先发 `WindowClose`（优雅收场）+ **1.5 秒守望**，到点还在跑就取消 token。只发消息不兜底的话，一个不理会该消息的程序会一直烧着 CPU 活在后台，而用户以为已经退出。③ `Abbreviate` 里「是不是根」要**统一按前缀判断**，别用 `ToRelative`（它要求 `根+分隔符` 严格前缀 ⇒ **根自己**返回 null，于是不得不加一条等值分支单独兜 —— 实测就出过「`vml build ~/examples/x.c` 缩写对了、`~>` 提示符却打出完整路径」，**同一个函数两条分支只对一边**）；缩写失败时记一行日志把两个根都打出来，别靠猜。
- **应用身份：显示名按语言、包名归公司（v0.96.172）**：显示名要按系统语言变就走**字符串资源**（Android `Resources/values*/strings.xml` + 清单的 `android:label="@string/app_name"`；iOS `zh-Hans.lproj/InfoPlist.strings` 覆盖 `CFBundleDisplayName`）—— csproj 的 `ApplicationTitle` 是**写死的字面量**、只能当兜底，写成字面量还会和清单里的 label 打架。APK 实测：默认 `WayCoder`、`zh`/`zh-CN` → `道码`。**包名 `com.companyname.waycoder.maui` → `com.tanso.waycoder`**（公司：深圳市探索智能科技有限公司 / tanso），⚠ **改包名 = 换一个 App**：新包不覆盖旧包（两个图标并存）、旧包的**私有目录**（Preferences 里的最高分/设置、解压出来的 vml 库）不跟过来，工作区与 config 在外部存储不受影响，keystore 没变所以旧包随时可卸。
- **内置标准库的解压位置与判据（v0.96.171）**：用户报「每次都解压」。查下来 `EnsureLibExtracted` **本来就有**「标记对得上 + `Lib/` 在 → 直接返回」的闸门，真正的坑在**标记文件的位置**：它原先在 `Global.Home/vml`，而 `Global.Home` 在 Android 上**会随「所有文件访问」权限在私有目录 ↔ `sdcard/waycoder/config` 之间跳**（`MauiBootstrap.ResolveHomeDir`）⇒ 用户在系统设置里授权/撤销一次，新位置没标记，**39 MB / 5245 个文件白解压一遍**。两条改动：① **解压根钉在 App 私有目录 `FileSystem.AppDataDirectory/vml`**（不随任何权限变），**老位置里已有一份且内容对得上的就地接着用**（不为搬家白解压；真换了内容自然解压到新位置）——顺带对齐本仓库自己的移动端铁律第 3 条；② **判据从「哈希整个 6 MB zip」换成随包的 `vml_lib.hash`**（`scripts/make-vml-lib.sh` 末尾生成）：读几十字节而不是 6 MB，而且它按「条目名 + 长度 + 内容」算、**不看时间戳**——zip 里带着文件时间戳，直接哈希 zip 会因"重新打包"而变，于是内容没变也白解压（脚本里 `-X` / 固定时间戳只治得住一部分）。哈希缺失时退回哈希整包，不会更糟。③ **静默等待要有状态**：解压与前端编译各要好几秒而屏幕上一个字不变，和卡死没区别 ⇒ `MauiVml.OnProgress` 静态钩子，命令行页在**本页发起的运行/编译期间**装上（`InstallVmlProgress`，`finally` 里清掉 —— 留着的话聊天那边跑 VML 会往这页冒提示），回调在后台线程触发、接收方自己 `BeginInvokeOnMainThread`（用 Begin 不用 Invoke，免得反过来把正在解压的线程拖住）。
- **`prog.ToString()` 的产物能不能独立跑：能，但必须"编完就存、不先跑"（v0.96.171）**：手机上的「VML 编译」把 `VmlProgram.ToString()`（= 链接之后的整份程序）写成 `<源名>.vml`，与上游 CLI 的默认产物同形（`vmltool main.c` → `main.vml`，见 `Program.Compile.cs` 的 `Path.ChangeExtension`）。这条链端到端验过（`.scratch/vmlround`：A 直接跑 vs C 存盘后再独立汇编跑，**输出逐字符相同**，含 `#include`/`.include` 残留 0 行）。**唯一一条规矩**：同一个 `VmlProgram` **先 `Run` 过再 `ToString`，产物再汇编出来不等价** —— 实测一个打印 3 行的程序变成只输出一个换行（`ToString` 会就地做死代码消除，而跑过的程序已带上运行期痕迹）。产品里 `CompileToVml` 与 `CompileAndRun` 各建各的程序，天然是对的；**别图省事把两条路合成"编一次、又能跑又能存"**。**上游 VMB 两个缺口（宿主侧只绕得开一个）**：① `ToVmbBytes()` 的数据段不认 `long`（只认 string/int/float/double/IList/DataString），碰到就抛 `Unsupported data type: System.Int64` —— 而 64 位常量在 C 标准库里到处都是（hello 级程序的数据段 160 项里 7 项是 Int64，全来自 `conv.vml`/`convert64.vml`），不处理则「.vml 编 .vmb」对几乎所有 C 程序都失败；`MauiVml.NormalizeLongConstants` 按位换成 `double` 绕开（编码侧 `Write(double)`/`Write(long)` 都是 8 字节小端、解码侧 tag 0x10 就 `ReadDouble()`、装载侧 `BitConverter.GetBytes(doubleValue)` 逐字节写 VM 内存 —— **三处全是字节搬运，没有一处按数值语义解释它**，故等价）。**当时没动 `third_party/vml`**（那会儿它还跟着上游走）；现在已分家，见 ⑱ 与 `third_party/vml/FORK.md`，可以直接改。② **`.vmb` 读回来这一半还断着**：手写的小汇编 `.vml` 编成 `.vmb` 装载运行**输出逐字符一致**，但编译器产物（35k 条指令那种）写出的 `.vmb` 自己读不回来（`InvalidDataException: Unknown operand type tag: 0x00`，在**代码段**的 operand 编码上，与上面数据段的缺口无关）。这条宿主侧绕不过去，得在上游 VML 仓库补。
- **GUI（Avalonia）三条坑（v0.96.82 ~ v0.96.84）**：①**输入框 Enter 收不到**——`MainWindow.axaml` 上挂的普通（冒泡）`KeyDown` 处理器会被 `TextBox` 自己的**类处理器**跳过：`AcceptsReturn=True` 时它先消费 Enter（插换行 + `Handled=true`），`SendAsync` 永远走不到，表现为「按回车没反应、只能点发送按钮」。**别靠事件阶段（隧道）绕**，正解是 `ChatInputBox : TextBox` **覆写 `OnKeyDown`**（自己就是那个类处理器，无顺序歧义）+ `StyleKeyOverride => typeof(TextBox)`（否则按 StyleKey 查不到 `ControlTheme`，输入框退化成无边框无光标的裸控件）。②**聚焦变黑/有外框压不住**——控件上设 `Background="Transparent"`/`BorderThickness="0"` 是本地值，压不住 Fluent 焦点态：焦点视觉是**带伪类的样式触发**（优先级高于本地值）且画在**模板内部 Border** 上。正解：`App.axaml` 里对 `local|ChatInputBox` 及其 `:focus`/`:pointerover` 态、再加 `/template/ Border` 各覆盖一遍 + `FocusAdorner="{x:Null}"`。③**cwd 显示的取值**——GUI 无 AgentSlot 体系（`CoreStubs.GetSlots()` 返回空数组）、`/cd` 是只读信息命令，故工作目录即进程启动目录，与 `/cd` 报告值、`SandboxManager.AllowedDirectory` 同源；呈现复用 core 的 `PathStatus.FormatCwd`（与 TUI 状态栏同一套）
- **移动端编辑器重做：虚拟化自绘 + 单行编辑（v0.96.113）**：`WayCoder.Maui/Pages/EditorPage` 从「透明 `<Editor>` + 垫底高亮 `<Label>`」整条链路全量处理（`ReadAllBytes` 全量解码、每次击键全文重高亮 + `Split('\n')` + 逐 rune 算宽 + 拼行号串、撤销栈存**全文快照** × 200）换成**自绘 + 单行编辑**。**数据层** `Infra/LargeTextFile.cs`：`ITextSource` 抽象（`EditableLines` 小文件可编辑 / `IndexedTextSource` 大文件只读 + 字节级行索引 + LRU 行缓存 / `MemoryTextSource` 空文件与 UTF-16），实测 **82MB、180 万行：打开 35ms、内存增量 14MB**。**渲染层** `WayCoder.Maui/Controls/CodeCanvasView.cs`（`GraphicsView` + `IDrawable`）：虚拟滚动、行号栏、彩色高亮、波浪线、超长行窗口化，每帧代价与文件大小无关。**单行编辑**：所有行自绘，光标行浮一个**文字透明**的 `Entry` —— 它只做 IME/软键盘/系统复制粘贴，**文字与光标都由画布画**（显示层只有一套 ⇒ 不可能错位）。**六条硬坑**：① **`AttributedTextRun` 范围越界会直接崩**（Android 喂给 `SpannableString.setSpan` 抛 `IndexOutOfBoundsException`）—— 空行时 `Syntax.Tokenize("")` 返回的是**空格 token** 而文本长度为 0，run 必须**夹在文本长度内**，空行干脆不调 `DrawText`；② **`Color.FromArgb` 的 8 位十六进制是 `#AARRGGBB`（alpha 在前）**，写成 `#RRGGBBAA` 会把「白色 6%」变成 **alpha=FF 的不透明黄色**（当前行顶出刺眼黄条）、「黑色 6%」变成全透明等于没画；③ **高亮条与文字必须同一个 y**（文字落笔带基线补偿，条少它就是整体偏上一截）；④ **`DrawText` 的 y 落在基线上**（不是行顶）—— 第 1 行会被画到画布上方看不见，而后续行因行高 18 > 字号 13 看不出来，故统一加 `EditorTypography.TextBaselineOffset`；⑤ **惯性速度不能用「总位移」估**（`-(总位移)×4` 既不是速度也无意义：轻扫位移小 ⇒ 速度≈0 ⇒ 几乎不滑，重拖反而窜出去），要用**最近 100ms 的位移 ÷ 时间**，摩擦 0.98 ⇒ 滑行约 50 倍单帧位移；⑥ **`DiagnosticManager` 在移动端没有数据源**（依赖 `LintTool`，MAUI 里是桩）⇒ 波浪线绘制代码在但**永远不显示**，要做实得补轻量诊断。另：**按 `0x0A` 扫字节切行对 UTF-8/GB18030/Big5/SJIS/EUC-KR 都安全**（续字节范围都不含 0x0A）⇒ 按行解码不需要跨块状态机，这条由自测钉住；编码探测用 `Decoder.Convert(flush: false)` **采样**，不会因采样切断多字节字符而误判成 GB18030；`MainActivity` 必须加 `WindowSoftInputMode = AdjustResize`（否则 `adjustPan` 整窗上推会让滚动偏移↔屏幕 y 全错）与 `ConfigChanges.Keyboard`。**v1 未做**：跨行退格、>2000 万行块索引、单行 >4MB 截断、设置页编辑器入口
- **移动端编辑器定位：自建网格模型，尺子只有一把（v0.96.114 ~ v0.96.121）**：点击定位的偏差折腾了八轮，前七轮都在做同一件错事 —— **让「测量出来的宽度」去追上「渲染落笔的位置」**，也就是同时维护两把尺子。只要字体度量、平台取整、字形 fallback 里任何一处不同源，两边就必然差一点，v0.96.117 把偏差从 24.5px 压到 1.5px 仍然是两把尺子。**最终换立场：位置一律自己算，不看字体度量。**① **宽度真源 = `CodeCanvasView.MeasureColumns/ColumnsToX/XToColumn/ColumnToCharIndex`**（半角 1 列、全角 2 列、tab 补到 4 的整数倍），`MeasurePrefixWidth` 与 `CharIndexAtX` 都收成一行网格运算；② **绘制也自己定**：`DrawGridRuns` 不再把整行交给平台排版，而是**逐语法 token 段按网格列定位**，每段起点 = 段首字符列号 × 列宽（段复用上色已有的 token run，不额外分词）——即使某处字形与列宽有差，**误差也不跨段累积**；③ **列宽用字体设计值 `FontSize × 0.5`，不用实测值** —— Android 把行宽**取整**（13pt 时拉丁真值 6.5 报成 7，`GetStringSize` 整行报 114 而网格是 110），照实测值定位每个拉丁多算 0.5pt；④ **`XToColumn` 不要取整**，返回连续列位置、由 `ColumnToCharIndex` 做中点判定（前半归它、后半归它后面）——取整会把「格子内部靠右的一点」推到下一格边界上，表现是**点哪儿都往后跳一格**；⑤ **宽度判定必须用 `AnsiString.CharWidth`**（全仓唯一真源），本地另写一张表漏了 emoji 段（0x1F000–0x1FAFF）⇒ emoji 判成 1 列、一行 114 个 emoji 累计偏 114 列 —— 又是「同一规则两处实现」。**字体那条是真正的根因**：`AttributedText` 的 run 上**绝不能写 `TextAttribute.FontName`** ——MAUI 会把它变成 Android 的 `TypefaceSpan(族名)`，而**那个 API 只认系统字体族名、没有 asset 重载**（`dotnet/maui` 的 `Graphics/Platforms/Android/Text/AttributedTextExtensions.cs`），资产名喂进去**静默回落成平台默认的比例字体**，中文与拉丁的宽度比立刻不再是 2:1；而不写 FontName 时布局回落用 `FontPaint` 的字体（= `canvas.Font`），走 `FontExtensions.ToTypeface` 的 **`CreateFromAsset` 分支，打包字体在这里能加载**。所以内置的 Sarasa Mono SC 是「不写 FontName」之后才真正用得上的 —— 它拉丁恰好 0.5em、汉字恰好 1em，「汉字 = 2 列」的网格与字体设计天然对齐。**诊断方法也换了**：临时加了 `#if DEBUG` 的**红标尺**（在测量出来的行尾画竖线，配 `adb exec-out screencap` 原始截屏取墨迹列算比值）——**把「偏了多少」从目测变成可量**，此前正是靠肉眼估出过一个错得离谱的「19%」；探针（`LogWidthProbe`，logcat tag `WCW`）保留但挂 `ShowDebugHud`，默认关。**两条平台守卫教训**：⑥ 给 MAUI 加 `#if DEBUG` 诊断代码时**必须同时带平台守卫** ——探针套在 `#if DEBUG` 里却没 `#if ANDROID`，里面全是 `Android.*` 与 Android-only 的 `FontExtensions.ToTypeface`，`net10.0-ios` 下 15 个编译错误，而**桌面构建全绿看不出来**（同 `CoreStubs.cs` 那条）；⑦ **`ICanvas.FontSize` 是只写的**（没有 get），诊断时读不到，只能读 `PlatformCanvasState` 侧。**验证**：1K 行（1029 字符，重复 `aB3中文，。！😀`）网格行宽 `W11134` 与独立算出的 1713 列 × 6.5 **完全吻合**；点 `x79/x193/x307` → `i8/i20/i32` 全中（含 emoji 的 UTF-16 代理对）；插入 `X` 落在 `！` 与 `😀` 之间、退格依次删对，emoji 未被劈开、整行无漂移。**iOS 只验到编译通过**（0 错误），模拟器运行崩在 static registrar 哈希不匹配（工作负载/运行时包不同步的**环境**问题，与代码无关）。**iOS 验证补充（v0.96.123）**：上面那条「环境问题」的结论**是错的** —— 注册器哈希不匹配的真因是 **`obj/` 里残留着别的 SDK 版本编出的中间产物**，`rm -rf obj/Debug/net10.0-ios bin/Debug/net10.0-ios` 重编即好（秒级；`dotnet workload repair` 是分钟级且多半治不了）。**遇到「看起来像环境损坏」的报错先清 obj**。iOS 模拟器触控自动化：`xcrun simctl` 没有触控命令，用 `osascript -e 'tell application "System Events" to click at {x,y}'`（**需辅助功能权限**，未授权报 `-25204`）；iPhone 17 = 402×874 pt @3x，**Simulator 窗口内容区在窗口内居中** ⇒ `屏幕坐标 = 窗口原点 + ((窗口宽−402)/2, (窗口高−874)/2) + 设备px÷3`，实测标定成立。**`click at` 会返回被点中的 UI 元素，是现成的落点验证手段**（返回 `of application process Terminal` 就是点到本会话窗口上了）。iOS 沙箱测试文件走 `xcrun simctl get_app_container <udid> <bundleid> data` → `<data>/Library/workspace`。**字体名一坑三吃（v0.96.127）**：内置的 Sarasa Mono SC **有三个互不相同的名字，写错不报错、只静默回落**——而回落成系统**比例字体**后，「汉字 = 2 列」的网格立刻不成立，症状是**渲染按比例、定位按网格 ⇒ 光标对不上位置**。三个名字（都从 TTF 的 `name` 表读出，别互相顶替）：家族名（nameID 1/16）`Sarasa Mono SC`（带空格）、**PostScript 名（nameID 6）`Sarasa-Mono-SC-Regular`（带连字符，iOS 要这个）**、Android 资产文件名 `SarasaMonoSC-Regular.ttf`（不带连字符、带扩展名，走 `CreateFromAsset`）。MAUI 在 iOS 上三条路**都要 PostScript 名**（`Graphics/Platforms/MaciOS/FontExtensions.cs`：`CGFont.CreateWithFontName` / `new CTFont(name,…)` / `UIFont.FromName`）。此前 iOS 分支写的是 `SarasaMonoSC-Regular`——**看着像，其实三个都不是**，于是 `UIFont.FromName` 拿到 null、静默回落，**编译全绿、运行才出错**。**教训**：注释里写着「写错会静默回落」的警告，当时只验了 Android 一支就以为两头都好了 —— 所以自检要做成**跨平台**的：`CodeCanvasView.Draw` 里一次性量半角/全角实宽与 `字号÷2`/`字号` 比（不符就打 `[字体自检] ❌`），GUI 侧同款 `GuiFonts.Verify()`（在 Avalonia 里量到 `a=6.50 / 中=13.00`，顺带证明了「0.5em/1em」在两端都精确成立）。**这类事情构建通过证明不了任何东西，只有主动量才算数。**
- **MAUI 的 handler mapper 是全局静态的 / 平台 API 的返回值要查语义（v0.96.135）**：对 v0.96.128~134 整段 diff 跑了一遍独立代码审查，10 条里最重的一条是**「改动的作用域远大于我以为的那个控件」**。① **`EntryHandler.Mapper` / `EditorHandler.Mapper` 是全局静态字典** —— `AppendToMapping("NoSelectionToolbar", …)` 无条件挂上去，就是给 **App 里每一个 Entry** 都设了 `CustomSelectionActionModeCallback`；受害者是聊天输入框 / API Key / 仓库地址等 18 处，而它们**只有**系统那一套粘贴入口（`Clipboard.*` 在 `EditorPage` 之外没有出现）⇒ 全 App 的复制粘贴被编辑器的需求一起废掉。**修法是判 `StyleId`**（同文件的 `TransparentText` 本来就判 `code-editor`，照它写即可）——**给 mapper 加东西之前先问一句「这个 mapper 会作用到哪些控件」**，答案永远是「所有」。② **`ActionMode.ICallback.OnCreateActionMode` 返回 `true` 是「创建这个模式」，不是「已消费」** —— 写反了于是工具条照弹，只是每个菜单项都被 `OnActionItemClicked` 吃掉，变成**一条点不动的工具条，比不弹还糟**；**不创建要返回 `false`**。③ **「跳帧优化」必须自带「保证还有下一帧」**：`DrawGutter` 在「本帧视口还在动」时跳过行号数字，但没人保证之后还会再画一帧 —— 惯性滚动的**最后一帧恰好「还在动」**时，行号栏就永久停在一条没有数字的灰边上，要等某个无关事件（点击、进编辑）恰好重画才回来。先试的两版「停止时补一帧」（`_gutterSkipped` 标志 + `StopFling` 钩子）都不成立：**惯性计时器在被节流掉的那些 tick 里仍在移动位姿**，补的那一帧量出来还是「在动」，于是又跳过一次、然后再也没有下一帧。**正解是在 Draw 里 `if (viewMoving) Dispatcher.Dispatch(Invalidate)` 再排一帧**，下一帧位姿没再变就自然收敛 —— **凡是「满足条件就跳过绘制」的优化，都要回答「不满足条件的那一刻，谁负责画」**。④ **一次性的收尾动作（落盘 / 提示）挂在「结束」事件上时，「被打断」也是一条结束路径**：`CancelInteraction` 只清了捏合状态却没发 `PinchEnded`，而「写 `MauiEditorStore` + Toast」恰好只在 `PinchEnded` 里做 ⇒ 来电 / 切走 App / 父容器截走触摸打断捏合时，**屏幕上字号明明变了、下次打开又变回去**（用户视角就是「改了没保存」），连提示都没有。清状态与发结束事件是两件事，别只做前者。⑤ **比例换算的基准要跟着更新**：`ResetTypography` 用 `_scrollFontSize` 当基准按新字号换算横向偏移，却**没把基准更新成新字号**，而捏合期间每接受一档就调一次本函数 ⇒ 比例变成 `∏(sᵢ/s₀)` 而不是 `s/s₀`，缩放几下就被 `ClampScroll` 甩到行尾、缩回去也回不来 —— 这类「连乘 vs 单次」在只缩放一次的测试里永远看不出来。⑥ **缓存键忘了带上「会影响它的那个输入」**：`_lineWidths` 存的是与字号相关的点宽，`ResetTypography` 只清了 `_charWidthMeasured` 没清它 ⇒ 缩放后 `MaxScrollX` 还是旧值（长行尾巴滚不到、缩小时又能滚进一片空白）。⑦ **改了视口的路径要发「视口变了」事件**：页面订阅 `ViewChanged` 来重摆选区操作条，而滚动路径只调了 `ThrottledInvalidate()` ⇒ 那条修复**从未被触发**，条子停在原地「乱飘」、滚出视口也不隐藏；把 `ViewChanged` 挂进 `ThrottledInvalidate` 即可（同一道 16ms 闸门，触摸 240Hz 也扛得住）。⑧ **重写一段逻辑时用 grep 数一遍旧触发点**：长按分支改写后 `LineLongPressed` 一个触发点都没有了，而它正是「把浮动输入框里正在编辑的那一行提交回文档」的唯一入口 ⇒ 长按复制到的是**编辑前的旧文本**（屏幕上却是新的）。⑨ **`char.IsLetterOrDigit('中')` 是 `true`**（汉字是 Unicode 字母类 Lo）—— `WordClass` 把 `char.IsLetterOrDigit` 写在 CJK 判断**前面**，于是第 2 类永远不可达，长按 `value中文名` 把整串当成一个词；**汉字必须在通用字母判断之前判**。⑩ **只读大文件的内容不在内存里**（`IndexedTextSource` 对 LRU 窗口外的行返回 null）⇒ 全选+复制会拼出「行数对、内容几乎全空」的**假文本**还报「已复制 N 字符」；拿不到的行要**如实标记截断**并提示，不能假装复制全了。⑪ **版本号曾是「手工同步的平行表」，已漂 8 个版本**：`scripts/release.sh` 的 VERSION 是从 **`Config/Global.cs` 的 `Global.Version`** sed 出来的，而 Android 包版本来自 **`WayCoder.Maui.csproj` 的 `ApplicationDisplayVersion`** —— v0.96.128~134 连续 8 版只改了 csproj（这几版都在改 MAUI 侧）⇒ 手机上「首页/关于页/TUI 标题栏显示 v0.96.127、Android 包是 0.96.135、桌面端打出来也是 v0.96.127」，**构建全绿，只有装上手机用眼睛看才发现**。**真源只留 `Global.Version`**，csproj 用 MSBuild 属性函数从 `Global.cs` 正则解析（`System.IO.File::ReadAllText` + `Regex::Match`，MSBuild 属性函数**不支持索引器**，所以三个捕获组要各写一次 `Regex::Match`）；`ApplicationVersion`（Android versionCode）一并推导为 `major*1000000+minor*10000+patch` —— **每段的位宽必须大于该段的取值上限**（最初写 `minor*100+patch`，而 patch 已到 135 > 100 ⇒ 0.96.135 算成 9735、**高于** 0.97.0 的 9700，一次 minor 升级就让 versionCode 变小，Android 拒绝覆盖安装）；解析失败要在 `BeforeTargets="Build;Publish"` 的 Target 里**报可读错误**（否则静默产出空版本号的包），且喂给 `[MSBuild]::Multiply` 的值要先做非空兜底 —— 空串会在**属性求值阶段**就抛 MSB4186，那条报错完全看不出是版本号没解析出来。**新增任何「同一个值写在两个地方」的字段前，先问一句「它们靠什么保持同步」**——答案是「靠人记得」就迟早会漂。
- **平台「排版报的宽度」≠「绘制时用的推进量」，只在整数号下重合（v0.96.139）**：用户报「光标压在字母上」，且给出关键线索 **「24 字号没问题，不是整数的却有问题」**。量出来的机制是：**平台绘制时把每个字形的推进量取整到整数设备像素**（未开亚像素定位），而 `GetStringSize`（= `Layout.GetLineWidth`）报的是**未取整的小数** —— 模拟器 420dpi、字号 14 下排版报 18.375px，而画出来的栅距**精确 18.000px**（一行 40 个 H，相邻墨迹起点差全是 18）。两者只在 `字号 × 0.5 × 屏幕密度` 落到整数上时重合 ⇒ **整数号对齐、小数号沿行累积偏差**（第 16 个字差 6px、行尾差 15px），光标于是落进字格里。**验证这种「差一点点」的问题，用同形字的墨迹间距量栅距**（`HHHH…` 一行，相邻起点差就是真实栅距）——它比「看光标在不在格线上」干净得多，因为后者的「格线在哪」要靠字形左留白去推，而不同字形的留白不同（`H`/`P` 这类左竖笔几乎贴着格线，`(`/`:` 却缩进很多），推错了就会得出相反结论（本次就先被 `H` 带偏过一轮）。**解法上别急着改宽度模型** —— 把我们的尺子也取整 = 替用户决定缩放粒度，而用户明确要「无极缩放」（这是他第二次拒绝对齐粒度，第一次是拒「只允许偶数号」）。最终按用户提的折中：**缩放保持无极，只在进入编辑态时把字号对齐到最近的整数**（`SnapFontSizeForEditing`，落点 `BeginEditLine` + `PinchEnded`）—— 「光标对得准」只在打字时是硬需求。另：`paint.SubpixelText = true` 是「不按整数像素吸附字形」的那个标志，与诉求同向，已加上；但在整数号下实测它**不改变栅距**（那本来就没有可取的整），所以当时那次「加了没变化」的测量**不能**说明它无效 —— **在「没有可观测差异」的场景里做 A/B，得到的是「无结论」而不是「无效果」**。
- **`ICanvas.DrawText` 每次调用都重新排版一次 —— 大文本量场景必须自己缓存（v0.96.136）**：移动端编辑器「滑动卡顿」的真身，读 MAUI 源码确认：`PlatformCanvas.DrawText` = `new SpannableString` → 逐 run `SetSpan` → **`new StaticLayout(...)`** → `layout.Draw` → **`Dispose()`**，**每次调用都从头排版、画完立刻销毁，MAUI Graphics 里没有任何缓存缝合点**（`AttributedText` 是纯数据类、`PlatformCanvas` 只有 `_canvas`/`_shader` 两个字段、`TextLayoutUtils` 与 `AttributedTextExtensions` 都是 internal）。一屏 55 行就是 55 次完整排版。**① 先分段量、别猜**：在 `Draw` 里给「底色 / 正文 / 行号栏」三段各读一次 `Stopwatch` 做差，HUD 上报 —— 实测 `底0.4 / 文78.0 / 号0.0`（55 行），而 `gfxinfo` 的 GPU 只占 2ms ⇒ 瓶颈 100% 在正文那一段。**交错实验能分辨「调用的固定开销」与「run 的边际开销」**：同一文件只把后缀换成 `.txt`（走 Plain，每行 1 个 run）→ 正文段 25.2ms，**3 倍差距全在 run 上**。**② 缓存要建在自己这层，而 `PlatformCanvas.Canvas` 是 public 的**（`get => _canvas;`）⇒ 把编译好的 `StaticLayout` 挂在按行缓存的对象上，绘制时取原生画布 `Save → Translate(x,y) → layout.Draw → Restore` 即可 —— **同一张画布、同一个 z 位置**，外层绘制顺序一点不用动，不需要自定义 handler、不需要改造渲染管线。真机实测正文段 **78.0ms → 21.9ms（3.6 倍）**，比 MAUI 自己那条单 run 路径还快。**③ `CurrentState.FontPaint` 是 protected、`TextLayoutUtils`/`AttributedTextExtensions` 是 internal，而本仓禁用反射 ⇒ paint 与 span 只能照 MAUI 源码逐字复刻**：`new TextPaint() + SetARGB(1,0,0,0) + AntiAlias + SetTypeface(font.ToTypeface()) + TextSize = 字号`；`ScaleX` **恒为 1**（只被 `canvas.Scale()` 改写，本控件从不调），所以 `TextSize` 就等于字号、与测量路径同源 —— **别想当然往这里塞个 density 缩放**。绑定上两条小坑：`ForegroundColorSpan` 的 ctor 收的是 `Android.Graphics.Color` 而不是 int（喂 int 会被解析成 `Parcel` 重载，报「无法从 int 转换为 Android.OS.Parcel」）；`FontExtensions.ToTypeface` 在 `Microsoft.Maui.Graphics.Platform` 命名空间下，没 using 时全限定调用。**④ 加了缓存就多一条失效条件，这是最容易漏的地方**：行缓存原先写着「与字号无关、调字号不必清」，挂了排版之后**必须清**（排版是按字号编出来的）；释放走**唯一出口** `DisposeLine()`，**四个丢弃点**（FIFO 淘汰 / `InvalidateLine` / `InvalidateAll` / 换字号）全得走它，漏一个就是原生对象泄漏 —— 而且**先释放再 `Clear()`**，反了就是遍历一个空字典、一个都没释放。**⑤ 渲染路径的替换必须逐像素验证，不能目测**：同一文件、同字号、同滚动位置（强制停止后重新打开，位置才确定）取**原始帧缓冲**（`adb exec-out screencap` 裸 RGBA，绕开 PNG 解码依赖）逐像素比 —— 实测 **259 万像素只有 581 个不同（0.0224%），且全部落在 Android 状态栏的时钟/图标**，正文与行号栏 0 差异。**⑥ 留一条「形态一变就回退」的安全网**：只认我们自己产出的「纯颜色 run」（出现字体名/粗体/斜体/下划线/背景/上下标/删除线/列表就整行走平台原路）—— 将来谁往 run 上加了别的属性而忘了同步，是**变慢**而不是**静默丢样式**。



- **移动端编辑器性能：真身是「字体被压缩进 APK」+ 定位改用平台实测推进量（v0.96.128 ~ v0.96.129）**：滑动/缩放每帧 ~250ms（4fps）的**根因不是绘制，是字体资产被打包成了 Deflate**。① **诊断靠分段计时 + 排除法**：在 `Draw` 里插 `Stopwatch` 分段打 logcat，读出 `canvas.Font = X` 花 **0.0ms** 而紧跟的 `canvas.FontSize = X` 花 **~110ms**、**同一个字号再设一遍又只有 0.0ms** ⇒ 是**一次性**开销（字体族解析）而非 `setTextSize`。源头：`PlatformCanvasState.FontPaint` 的 getter 在 `_typefaceInvalid` 时调 `Microsoft.Maui.Graphics.Platform.FontExtensions.ToTypeface()`，**那条路没有任何缓存**（每次都 `Typeface.CreateFromAsset`）；而 25.5MB 的 CJK 字体在 APK 里是 **`Defl:N` 压缩**的 ⇒ **每次调用解压 25.5MB**，一帧两次 ≈ 220ms。**修法：`<AndroidStoreUncompressedFileExtensions>.ttf;.otf</AndroidStoreUncompressedFileExtensions>`**（打包成 `Stored` 后可 mmap）—— 112.8ms → **0.3ms**、行号栏 115.3ms → **2.4ms**、整帧 **~250ms → ~31–56ms**。`unzip -v` 看压缩方式是关键一步，**别只看「命令跑成功了」**。② **`GetStringSize` 的真相**：走 `PlatformStringSizeService`（**无界排版**，`boundedWidth: null` ⇒ 宽 `int.MaxValue`，**不折行**）取 `GetLineWidth(i)` 的**真实浮点宽** —— 早期把它误当成「有界 512、会折行」绕了弯路（长行量出恒定 26.5px 的假偏差）。③ **平台逐字形取整**：同一行在偶数号偏差 **0.00px**、奇数 13 号差 **53.5px**（= 每个半角字形 +0.5），且**与字号无关**（11/13/17 号都是同一个 26.5/53.5，因为都是 x.5 半列宽）—— 差值与半角字形数成正比、与字号无关，正是「每个字形取整」的指纹。④ **定位改成逐字形累加平台实测推进量**（`MeasureAdvances` 量 `"0"`/`"中"` 各一个），`MeasurePrefixWidth` 与 `CharIndexAtX` 互为逆、共用同一套量 ⇒ **撤销「只允许偶数号」**（那条限制会让捏合每档 2 磅、手感发跳），字号连续可取。不变量自检（`logcat -s WCFONT`）：「逐字累加 vs 平台整段排版」在 8/12/**13**/16/28/30 号下**最大偏差 0.00px**。⑤ **整行一次 `DrawText`**（语法色 = 同一串里的多个 run），取代「逐语法段各画一次」——每可见行从十几次调用降到 1 次。⑥ **横屏别弹全屏输入法**：Android 的抽取式编辑（extract mode）会整屏盖住输入区，`Entry`/`Editor` handler 加 `flagNoExtractUi` + `flagNoFullscreen`，且**用 `|=` 不能赋值**（MAUI 拿 `ImeOptions` 表达 `ReturnType`，赋值会把 Done 抹掉）；绑定把 `ImeOptions` 暴露成 `ImeAction`、flag 与动作位共用同一个 int ⇒ 只能转 `int` 再或。⑦ **浮动输入框的横向原点必须与画布正文逐项对齐**（v0.96.130）：那层 `Entry` 的文字与光标是**透明**的，但**「光标 / 选择手柄 / 复制粘贴浮层」是系统按输入框自己的内部坐标画的** —— 我们只是让它看不见，没让它不存在。所以 `Margin.Left` 必须是 `行号栏 + 正文左内边距 − 横向滚动`（与 `CodeCanvasView` 的 `textX` 同式），并且要 `SetPadding(0, top, 0, bottom)` 清掉 EditText 的左右内边距；少一项，系统浮层就整体偏那么多（横向一滚差出整个滚动量）。同理 **`ResetTypography()` 里不能 `_scrollX = 0`** —— 横向偏移是像素、字号一变含义就变，但正解是**按字号比例换算**（`_scrollX × 新字号 ÷ 旧字号`）而不是清零，否则「滚到行中间一缩放就跳回最左」。⑧ **滚动中不画行号数字**（v0.96.130）：判据是「本帧视口位姿与上帧是否相同」（首个可见行 + 横向偏移），不依赖手势状态机 ⇒ 拖拽/惯性/程序滚动自动覆盖，停下后下一帧数字回来；**底色照画只跳数字**，否则滚动时左边缘露出与正文同色的空白像界面在抖
⑨ **选中/复制/粘贴全部自己做（v0.96.132）—— 平台只留 IME 与剪贴板数据**：平台的选区 UI（长按弹出的 复制/粘贴/全选 工具条 + 两个水滴选择手柄）**所有坐标都按它自己那层输入框算**，而正文是自绘的 ⇒ 差一点就「选中的位置和看到的位置对不上」。与其追平它的坐标系，不如**把它请出去**。画布侧：选区是**字符级**（端点 = 行 + 行内码元下标），长按 = 选词（同类字符段，空白处选整行）、长按后拖动 = 扩选，底色**按字符跨度**铺（起止都取字符格左缘，与 `MeasurePrefixWidth` 同源）、自己画；页面侧：自己的操作条（复制/全选/粘贴/✕），`CustomSelectionActionModeCallback` 三个回调**全返回 true** 关掉平台浮层，剪贴板仍走 `Clipboard`（**那是数据通道不是 UI**）。**两条硬坑**：① **长按必须在「手指还按着」时判定**（加 500ms 单次定时器）—— 只在抬手时按耗时判断的话**永远做不出「长按选中再拖着扩选」**（抬手=手势结束）；② 一个手势里「长按」与「拖动」的语义靠 `_selecting` 标志分开：为真时拖动改的是选区端点而不是滚动视口。
⑩ **仍未解决**：小字号（≤10）滑动偏卡 —— `framestats` 拆出 `布局 0.1ms / 绘制 51.9ms / GPU 5.6ms`，卡在我们的绘制路径；每可见行两次平台文本绘制而 `DrawText` 每次新建 `StaticLayout`（`ICanvas` 无缓存入口），字号 8 一屏行数是 14 号的约 2 倍，**把每行成本减半的收益又吃了回去**（1.8ms/行 → 0.96ms/行，但 24 行 → 54 行）。可选的下一步：滑动中先不画行号、停下再补（约省一半）
- **TUI 思考折叠 + 聊天行数上限（v0.96.112）**：推理正文**不再留在聊天流里** —— 思考中实时滚动可见（观感不变），一旦定稿（正文开始 / 工具调用 / 本轮结束）立刻折叠成一行「💭 已思考 N 秒」，正文行整项从 `ChatList` 移除、只留内存（`ChatMsg.Reasoning`，50K 尾部窗口）供点开看全文（**鼠标点那一行**，或 `Alt+T` 看最近一条）。**收益不在少显示几行，而在正文彻底不进渲染层**：一段 50K 推理 ≈ 上千行，留在 `TuiListView` 里就是每次 `ReLayout`/滚动/重解析都要付的钱。配套 `Config.MaxChatLines`（默认 500）按**总行数**裁剪到 80% 低水位 —— 条数上限（1000）对「一条消息顶几百行」无感；流式进行中跳过行数裁剪（否则会把正在读的内容整段抽走，最后一项就是流式项时等于清空历史）。**五条必须记住的坑**：① **解析规则单源是 Web 的 `app.js` `handleToken`**（C# 版 `UI/Shared/ThinkStreamParser.cs` 逐条对应）：`«dim»` 开、块内新开标记**逐层配对**（LLM 超长时注入的 `«orange3»…«/»` 不该结束思考）、**块外的 `«/»` 必须原样进正文**（它是所有 «» 标记的统一结束符，剥掉会让渲染器失配）；② **`StartThinkBlock` 里那句防御性 `FoldThink()` 必须传 `resetParser: false`** —— 它是解析器**刚把深度置 1 之后**才发的回调，顺手 `Reset()` 就把「正在思考」抹平了，紧随其后的整段推理全被判成正文（实测「思考行建了、正文却跑进 assistant 项」就是这个）；③ **思考消息要 `Insert(ChatMessages.Count - 1, msg)`**，不能 `Add` 到末尾 —— `AppendToken` 依赖 `ChatMessages[^1]` 是流式 assistant，插错位置后所有 token 被**静默丢弃**；④ **正文项要懒创建 + 用引用追加**（`_streamItem`），不能沿用「取 ChatList 最后一项」：思考行会插在正文项前面，按末项追加会把答案写进「已思考 N 秒」那行（`StartAgentMsg` 因此也不再建空白占位项）；⑤ **`_pendingToolBody` 是陈旧标志**（`AddToolProgress` 置位，只有 `AppendToLast` 会清），而 `onToolOutput` 只对 bash/写文件类工具发 —— `read_file`/`grep` 之后它会一直挂着；**改造前**它会把下一轮首个 token 当成工具输出另起一条 tool 消息（真实踩过的形态），**现在正文改走 `AppendToStreamItem` 已不再经 `AppendToLast`**，该误伤消失，但思考开始处仍显式清一次（收尾语义 + 防止将来正文路径改回去时重蹈）。槽位缓冲路径（`AgentSlot.BufferedAppendToken`）做同一套分流，否则非活跃槽位切回来正文里躺着裸 `«dim»/«/»`，且 `RestoreTo` 重建出的项集与活跃槽位不一致。**UI 侧两条**：**详情窗要自己折行**（`TuiMarkdown` 的纯文本分支 `AddContentLine` 不折行，超宽行会被 `WriteAt` 的右侧裁剪补成「…」—— 复用 `TuiMarkdown.WrapText`，已提为 public），且**模板里 `ScrollView` 必须写 `flex="1"`**（漏了会塌成 1 行，只显示第一行正文）
- **行内问答：只 CLI/TUI 不弹窗，四端分界在 `ActiveScreen`（v0.96.111）**：权限确认 / 计划审批 / 粘贴确认 / 通用确认 / 退出确认 / 设置页 select 全改「输入框**下方**的文字选择栏」（`ChatScreen.InlineChoice`，❯ 箭头 + 黄底 + 每项一行说明）；**不放上方**是因为那里与 `/ @ ! #` 前缀提示共用 `InputArea.KeyHook`、且是「Enter 回填输入框」的语义，放一起必打架。**只要终端行内，Web/GUI/MAUI 仍弹框（有意为之，不是没做）**：分界点是 `TuiManager.Instance.ActiveScreen is ChatScreen` —— Web/GUI 不 `PushScreen`、MAUI 的 `TuiManager` 桩 `ActiveScreen` 恒 null，三端一律落到 `UxHelper.WebInteraction` 桥（GUI 用 `GuiInteraction`、MAUI 用 `MauiWebInteraction`）。**四种形态一套机制**（`SurveyQuestion` × `MultiSelect` × `showTabs`）：多选一 / 多选多（`Space` 勾选，`[x]`/`[ ]`）/ 横向多页（页头标签行 + `←→`）/ 分步骤（页头「步骤 k/n」+ `Enter` 推进）；题目末尾自动附「其他（自行输入）」与「跳过此题」（单选页才加，多选页空选本身就是跳过）。**「其他」复用输入框**（KeyHook 对普通键返回 false 放行、只拦 Enter/Esc）—— 它的早退判据**必须含 `_surveyOtherInput`**：输入态时选项栏已收起（`InlineChoiceVisible=false`），若只判可见性，Enter 会被 `HandleSpecial` 的「发送消息」先截走，用户打完自定义答案一按回车就当成聊天消息发了出去。**不阻塞主循环**：`UxHelper.RunInlineChoiceOnScreen` 走 `RenderWait(win: null)`（后台线程只 Sleep 等事件，渲染读键归常驻主循环，顺带绕开 `win.Screen == null` 那条过早返回的判据）。**高度一变必须 `TuiManager.RequestFullRefresh()`** —— 行内栏换页/显隐会改 `Height`，下方兄弟控件整体位移，而增量渲染只重绘「自己标脏」的控件，被挪走位置的旧像素留在屏上（实测上边框被上一帧页头文本啃出豁口 `╭─── ─ ───── ─ ───╮`）。**`TuiPromptBar` 的填充右界要含右框内侧列**：只填到 `Width-3` 会漏掉 `Width-2` 那一列，旧帧画在那里的 `─` 没人覆盖；**页头行也必须按普通内容行渲染**（左右边框 + 整行填充），只写文本会让上一帧的残留（动态栏 spinner/百分比）粘在页头上
- **语法高亮的 token 类别与两套实现（v0.96.111）**：`Syntax.Tokenize` 认 **10 类** —— 注释 / 字符串 / 字符字面量 / 数字 / 关键字 / 运算符 / 括号标点 / 函数名 / 类型名 / 变量。其中**标识符只挑两类上色**（后跟 `(` = 函数、首字母大写 = 类型名），其余给**明确的亮灰 `Identifier`(253) 而不是 `Default(0)`** —— `0` 是「用终端默认前景」，而暗色终端的默认前景本身就偏暗，会和注释（`Comment` 241 暗灰）糊成一片（用户实测「标识符和注释一样」）。`Plain`（`.txt`）的 `HighlightSymbols=false`：散文里的括号、破折号、冒号不该变色。**Web 端是独立的 JS 实现**（`app.js` 的 `highlightCode` + `style.css` 的 `--tok-*`），跨语言无法共享代码 —— **改一边记得改另一边**（本仓库反复出现的「同一规则两处实现」，这层无法靠共享代码消除）。语言识别两条路：语言标签（23 条分支、60+ 别名含 `c#`/`jsx`/`python3`）与文件扩展名（38 个）；语言标签不认识时走 `Generic()` 通用表（不是 `Plain` —— 后者的关键字表是空的，整块只剩字符串有色）
- **「粗体 + 颜色」编码进颜色高位（v0.96.111）**：中间格式的段模型是 `(Text, Fg, Bg)` 三元组，**没有独立的样式通道** —— `«bold»«orange»Edit«/»«/»` 里内层的颜色码会把外层 `«bold»` 的样式码 1 **覆盖**掉（解析栈只存 Fg/Bg）。修法：`AnsiTty.BoldFlag = 0x2000000`，`«bold»` 置该位、颜色码保留该位，`FgCode`/`FgBgCode` 见到就先发 `SGR 1` 再发颜色。这样不用改动所有段的消费方（TUI/MAUI/Web 各自的渲染器）；MAUI 侧 `ResolveFg` 要**先剥位再解析**（否则 `≥0x1000000` 的真彩分支会把它当成巨大 RGB 解出乱色）
- **工具行统一格式（v0.96.111）**：`💡 «bold»«orange»Edit«/»«/»«grey»(参数)«/»` —— 图标统一 `💡`、名称去 `_file` 后缀再 snake→Pascal（`edit_file`→`Edit`、`multi_edit`→`MultiEdit`）、**参数只给值不带 `key=`**（`ToolDisplay.Brief` 第 87 行原拼 `k=v`）、**长度不截断**（默认 `maxLen=0`）改为**按控件宽折行**。折行三要点：在 `«»` 标记**之外**折（硬切会切出字面量）、每行**各自闭合**（plainText 逐行解析标记，跨行对不上）、按显示宽折且**不切断宽字符**；优先在空格/路径分隔符处断行，回退超行宽 1/3 则放弃回退。**bash 的参数按 shell 语法上色**（它就是一条命令行），其他工具的 brief 是路径/描述，统一灰
- **代码围栏的容错（v0.96.111）**：模型常把三反引号围栏写成一个或两个反引号（形态是「反引号 + 语言名」独占一行开栏、「纯反引号」独占一行收尾），按标准 markdown 既不是围栏又不是行内代码 —— 抽 `UI/Shared/CodeFence` 做唯一判据（TUI + MAUI 共用），收得很紧：只认「整行只有反引号 + 标识符形态的语言名」。**真正的坑在段落累积**：它的终止条件硬编码了 `StartsWith("```")`，于是「先一句说明、再贴代码」（模型最常这么写）时围栏被当段落续行吃掉；**首行即围栏的形态一直正常**，所以只测那种形态的断言全绿、实际却不对。闭栏判定放宽为「标准形态满 3 个就算闭合」（4 开 3 闭是模型常见写法，按标准「闭栏不少于开栏」会找不到闭合、把后面所有正文吞进代码块）
- **代码块/ diff 的底色铺满整行（v0.96.111）**：`TuiMarkdown.RenderMessage` 出口统一 `FillRowBackgrounds`（取行内第一个非零背景色补空格到渲染宽度）—— 竞品的代码块与 diff 底色都是铺满整行的，只裹住文字会在右侧留断口。**工具输出按文件后缀上色**：`ContentDiffFormatter` 用 `Syntax.ForFile(file_path)` 定语言（这类代码没有语言标注，但路径一定有），格式是「行号与 `+`/`-` 标记保持 diff 语义色 + 代码逐 token 上真彩 `«fg:#rrggbb»`」（256 色→hex 的换算在 `AnsiTty.Xterm256ToRgb/ToHex`）
- **动态栏三段与子智能体数（v0.96.111）**：左段（状态）`1/3`→`1/5`、中段（工具命令）吃掉剩余全部宽度、右段（`📊⚡🔤¥`）**右对齐**（实现是「按优先级左起排 → 整体右移贴边」，保持原有的丢项优先级）。几何抽成 `ComputeGeometry`，`OnRender` 与 `RenderDirect` **共用一份** —— 此前两处各硬算 `Width/3`，改一处忘另一处就串位/闪烁。新增 `AgentTool.ActiveSubAgents`（`Interlocked`，包在整个重试周期外）→ 右段 `🤖N`，仅 >0 时占位。**状态栏**：路径在「槽位条之后 / 右侧 Token 之前」区间居中（保尾部截断）；槽位用 **`[N]` 方括号**框住当前（白底/纯颜色在浅色主题与色盲下都不够明确），第 10 槽显示 `0` 而非 `10`（个位等宽，切槽位时后续内容不左右抖）
- **代码配色 256 色 + 语义名，MAUI 走 xterm 算法（v0.96.111）**：`Syntax` 从标准 16 色换成 256 色柔和调（关键字紫 `#c678dd`、字符串柔绿 `#98c379`、注释暗灰 `#5c6370`…，对标 One Dark / Crush 的 glamour），常量改**语义名**（`Keyword`/`Str`/`Comment`/`Key`/`Number`/`Type`，旧颜色名保留为别名）—— 调色只动一处，不再出现「`Cyan` 其实是紫色」这类名不符实。**MAUI 的 `ColorForToken` 原来只认 16 色 + 真彩，256 色一律 fallback 成默认色**（移动端代码高亮全灰），且此前是「用到哪个色往表里补哪个」必然漏；改成 `FromXterm256`（6×6×6 立方 + 24 级灰阶）全覆盖。跨端契约断言（`Tokenize 色值 ∈ {0,2,16..255}`）随之放宽
- **权限确认的记忆语义与 diff（v0.96.111）**：`ShowConfirmDialog` 返回 `(bool Allowed, int Code)`，**只有 `code == 1`（全部允许）才写 `AutoAllowed`** —— 此前只判 bool，选「**仅本次允许**」也会被记住整个会话，与选项文案正好相反。`PermissionManager.AllowEditsThisSession`（计划审批选「批准并自动接受编辑」时置位）**只放开** `edit_file`/`write_file`/`multi_edit`，bash / rm / kill 照旧逐次确认（对齐竞品 auto-accept edits 的语义）。权限确认的文件级 diff 走 `UI/Shared/UnifiedDiff.Generate`（读原文件 → 应用替换 → 统一 diff，取代原来各截 80 字符的 `-old/+new`），**这份文案四端共享**，Web/GUI/MAUI 的弹框一并受益
- **keypad 的 `INLINE:` 指令（v0.96.111）**：`Test/Keypad.cs` 加 `INLINE:perm / permdanger / survey / step`，配 `Test/scripts/inline_{perm,survey}.txt`。**刻意直接调 `ChatScreen.ShowInlineChoice/Survey` 而不走 UxHelper 的 `RunInline*`** —— 后者 `RenderWait` 阻塞到用户作答，脚本再也走不到后面的 `SNAP`（这正是「非阻塞挂栏」与「阻塞等待」两种入口要分开的原因，`ShowInline*` 本身就是非阻塞的，阻塞只发生在 UxHelper 那层）。**它抓出的三个缺陷都是自测断言覆盖不到的**：右侧列漏写、页头行缺边框/填充、换页位移未全屏重绘 —— 三者的共性是「**内容对了但某一帧的画面不对**」，只有逐帧看画面才会暴露
- **CLI 参数两条语义铁律（v0.96.85 / v0.96.86）**：①**有错即报错退出，绝不静默忽略**——`CliArgRegistry.Parse` 对未知/拼错选项（`--modle`）、位置参数（漏 `-p`）、必需值缺失一律 `✘` 到 stderr + 退出码 1；旧行为是 `continue` 跳过，用户以为参数生效了、实际以默认配置进了交互界面。裸位置参数本就不在用法（`waycoder [选项]`）内。**注意 `ExitCode` 有两种语义**：「解析出错」（1）与「终结型动作已处理、正常退出」（`--model list` 返回 0）——**别用 `ExitCode != null` 当错误判据**（写测试时因此假红过一次）。批量子进程 `BatchRunner.SpawnSelf` 以 `-p <任务> -y --model/--base-url/--api-key/--max-budget-usd` 启动自身，改名时务必同步（漏认一个则批量整批起不来，已有断言锁住）。②**CLI 启动参数一律「本次启动覆盖」，不写用户配置**——`--model`/`--base-url`/`--api-key` 走 `Global.PersistDisabled`（只改内存、不写盘），闸门设在三个写出口 `ConnectionConfig.Save()` / `Config.SaveToConfigJson()` / `Config.SaveToEnvFile()`。此前它们经 `ApplyModelChoice → SetActiveConnect` 连带写出 `connections.json` + `config.json` + **`.env`**，`--api-key` 还会永久写 `api_keys.json`。**顺序依赖**：`base-url` 必须在 `ApplyModelChoice` **之后**落到 `_config`，否则被 connect 推导地址覆盖——别把这几个赋值散出去。要永久保存走 `/model`、`/connect`、`/model key <供应商> <key>`
- **无参数启动全屏界面 = 「有画布 + 拿得到键盘」，不是「stdin 没被重定向」（v0.96.88）**：`RunReplAsync` 此前用 `Console.IsInputRedirected` 当「非交互」判据直接切管道模式，而 stdin 早被 `Main.ReadToEnd` 读过（有内容就成了一次性提示词）⇒ 那条分支只剩「stdin 是空的」，于是 `waycoder < /dev/null` **静默退出（零输出、退出码 0）**。被别的程序拉起、脚本调用、双击启动器、`waycoder < 文件` 都可能让 stdin 是管道而进程仍挂着可用控制台 —— 现在判据是 `ConsoleDevice.CanUseFullScreen(stdin重定向, stdout重定向, 能否开控制台设备, 是否Windows)`（纯逻辑可测）：stdout 被重定向=没有画布→不开；stdin 被重定向但 Windows 能开 `CONIN$` → **照常开 TUI**，读键走 `ConsoleDevice.OpenInput()` 返回的 `CONIN$` 流，**句柄必须交给 `WinConsoleMode.Enable(handle)`**（stdin 是管道时 `GetStdHandle(STD_INPUT)` 拿到的是管道、拿不到控制台模式）。真没有控制台时打印说明 + 退出码 1；**退出码要走返回值**（`RunReplAsync` 返回 `Task<int>`），`Environment.ExitCode` 会被 `Main` 末尾的 `return 0` 盖掉。Unix 不算这条路（`Console.ReadKey` 的 raw mode 绑在 stdin，单开 `/dev/tty` 没进 raw mode）
- **「助手已存在但被绕过」是本仓库最主要的重复形态（v0.96.90）**：全仓 137K 行过了一遍 6 路区域审查 + 机械检测（479 非测试文件 / 8 行窗口 / 跨文件重复 409 组），33 条里去重价值最高的那批**不是「没人写过」**而是「写过了、调用点绕过去了」——`GitRunner` 类注释写着「所有 git 调用都应通过此类」被 4 处绕过；`PathSafety.Guard` 被 6 处绕过；`UiText` 建来就是为消重、**零生产调用点**；`RunModalDialog` 注释写着「收敛约 8 份」而同文件 40 行外有 6 个私有方法没用它。**动手写新助手之前先 grep 有没有现成的**；**同一份数据/文件被两个工具读写时，读写必须单一实现**（`todo`/`struct_todo` 共用 `todos.json` 各写一套，已漂移成：状态词表分裂 + 一个原子写一个裸写 + 文案骗模型三处）。本版落地的单一真源：① `Tools/TodoStore.cs`（todos.json 唯一读写 = 锁 + `Global.WriteAllTextAtomic` + `ValidStatuses` 词表 + 依赖图）；② `Tools/FileWalker.cs`（递归遍历唯一实现，跳过判断**以 `FileIgnoreManager` 权威表为底**、各工具用 `extraSkipDirs` 显式追加噪音项——此前三份表互不相同导致「grep 查不到、find_replace 却改得到」）；③ `GitRunner`（**所有** git 调用走它：自带 `RedirectStandardInput` 隔离 + `GitTimeoutSec` + `ProcUtil.AwaitReadWithTimeoutAsync` 读超时；`SystemPrompt.RunGitCommand` 原来超时后仍无界 `GetResult()`，而那条路径每次构建系统提示词都走）；④ `ProjectInitializer.DetectTestCommand(root, userOverride)` / `DetectBuildCommand(root)`（构建/测试命令唯一真源，`Agent.Feedback` 转调——此前 `/init` 写进 AGENT.md 的命令与 Agent 实际执行的**给出两种答案**）；⑤ `UI/TUI/ChatRoleStyle.cs`（角色显示名/正文色/图标色唯一真源，此前四张平行表把 user/system 硬编码成亮白，使主题 6 变体 × 4 个 `Chat*Fg`/`Icon*Fg` 全成**死键**）；⑥ `SandboxManager.ContainmentReason` + `IsUnder`（路径包含判断唯一实现，**含 symlink 解析 + 路径段边界**——`CheckDirectoryEscape` 原来不解析 symlink，`cd` 逃逸可用「项目内 symlink 指向项目外」绕过；裸 `StartsWith` 还让 `/proj-evil` 通过 `/proj`）；⑦ `PktLine.ReadFrame`（pkt-line 非数据帧判据是 **`len < 4`** 不是 `len <= 1`：v2 的 response-end-pkt 是 `0002`，旧的宽容分支会 `new byte[-2]` 抛 OverflowException 打挂 clone）。另两条易踩：**`RenderBuffer.Write` 的 `fg:`/`bg:` 在 1..9 是样式码、不是颜色码**——`fg: 8` 是 SGR 8 conceal（字符直接不显示）而非 dim，要暗用 `AnsiTty.StyleDim`（=2），`TuiMenu` 的滚动条与分隔线就这么隐形过；**`WayCoder.Maui/CoreStubs.cs` 是桩类**——MAUI 排除 `UI/TUI/**` 却编译 `Tools/**` 与 `Agent/**`，真实现每加一个被这些文件用到的 public 成员都要同步补桩，漏了**只在 MAUI 上 CS0117**（桌面构建全绿看不出来）。
- **自测失败行必须直写真实 stdout（v0.96.94）**：`SelfTest.Report` 走 `Console.WriteLine`，而不少用例用 `Console.SetOut(StringWriter)` 捕获输出（渲染/对话框/编码类）—— **期间若有 Check 失败，❌ 落进那个 StringWriter 就被丢掉**，表现为「汇总 `失败: N` 但输出里没有任何 ❌ 行」，排查时完全无从下手（实测偶发过两次）。现在 `RunWithFilter` 在入口捕获真实 stdout，`Report` 在当前 `Console.Out` 已不是它时**额外直写真实 stdout**。**新写捕获输出的用例不必管**；但**新增任何「用 SetOut 捕获」的块时，别把 Check 放进捕获区**，或者接受失败行会走两遍。
- **「从 cwd 向上找」的循环：边界必须锚在「一定在祖先链上」的目录（v0.96.105 结清）**：`SkillsManager.FindSkillDirs` 原先只靠上溯顺带命中 `Global.Home`，而 `dir == Global.Home` **只在 cwd 位于 home 之下时才成立** —— **cwd 与 home 不同盘**（D 盘项目 / C 盘用户目录）时该边界永不触发，循环直落盘根，用户自己的 `~/.claude/skills`、`~/.waycoder/skills` **静默不加载**（现象：换到别的盘建项目，个人技能就「消失」了）。而且 `Global.Home` 会被 `HomeOverride` 改成临时目录（自测/嵌入式），它**根本不在 cwd 的祖先链上** ⇒ 等不到相等 ⇒ 上溯无界。**正解两条一起上**：① 用户的**个人级**技能目录**无条件纳入**（`Global.Home` 不在链上就显式补进链尾 —— 链尾在 `Reverse()` 之后排最前 = 最先加载 = 优先级最低，保持「本地覆盖通用」语义不变）；② 上溯边界锚在 `ProjectContext.UserProfileDir`（**不随 `HomeOverride` 变化**，`internal` 出去了）与盘根，两个都兜 —— 与 `ProjectContext.FindProjectRoot` 同一处置。**写任何「从 cwd 向上找」的循环时先问：这个边界在什么条件下才会触发？锚在被覆写/被重定向的变量上就永远等不到。** 仍未解决的一条（**有意不改**）：自测临时目录若恰好落在真实用户主目录之下，上溯经过 profile 那一级仍会收进开发机真实的 `~/.claude/skills` —— 那是「用户在自己 home 下跑」的正常语义，要修只能迁测试目录，不该往生产代码里加测试专用的特例。
- **重复代码清理的落地清单（v0.96.90 ~ v0.96.101）**：全仓过了一遍（6 路区域审查 + 机械检测，33 条），判据是「重复是否已在产生风险」，不是行数。**新增共享代码前先 grep 有没有现成的**（详见上一条）。已落地的单一真源：`Tools/TodoStore.cs`（todos.json 唯一读写）、`Tools/FileWalker.cs`（递归遍历 + 跳过判断，以 `FileIgnoreManager` 权威表为底）、`Tools/SsgfRedirect.cs`（跟随重定向 + 每跳 SSRF 校验；**重定向预算必须显式传** —— 此前三处是 5/10/5 无声地不同）、`Tools/WritePipeline.cs`（`ConfirmDiff` 逐 hunk 确认 + `RestoreCrlf`）、`Git/GitRunner.cs`（所有 git 调用）、`UI/Shared/UnifiedDiff.cs`（行级 diff 引擎）、`UI/TUI/ChatRoleStyle.cs`（角色配色）、`SandboxManager.ContainmentReason` + `IsUnder`（路径包含判断）、`PktLine.ReadFrame`、`TextEncoding.MatchBom`（BOM 表）、`UiText`（权限/经济文案）、`ProjectInitializer.Detect*`（构建/测试命令）、`Agent.ApplyRuntimeModel`（模型切换收尾）、`Global.WalkUpDirectories`（从 cwd 逐级上溯找配置，**终止要同时兜住 `parent == dir` 与 `parent == null`** —— `Path.GetDirectoryName` 对盘根返回 null）、`Maui/Markup/MarkdownTable.cs` + `MarkupToFormattedString.AppendCodeLines`（移动端表格判定与代码块高亮；合并时发现三处**并不等价** —— 聊天流那版缺「超大代码块降级纯文本」护栏，而那道护栏正是防移动端 ANR 的）、`ToolErrors.ErrorOpPrefix`（五个工具手拼的 `错误：{op}: …`）。
**续（v0.96.97 ~ v0.96.101）**：`UiText.RelativeTime`（相对时间阶梯；移动端那版漏了「N 周前」分支已漂移 —— 10 天前手机上「10 天前」、桌面上「1 周前」）、`Infra/Logging/RotatingFileWriter.cs`（日志文件句柄生命周期，**漏 `Flush` 丢日志、漏 `FileShare.Read` 把文件独占锁死**，两处各写一份时改一处忘另一处无编译期提示）、`UI/TUI/Base/TuiScrollMath.Paint`（滚动条落笔唯一实现，四处内联连 `"█"`/`"│"` 字面量都逐字相同）、`UI/TUI/Base/TuiScrollable.cs`（滚动状态机：`ScrollOffset` + 四个滚动方法 + 跟底标志 + `OnResize` 钳制；`TuiScrollView`/`TuiListView` 只剩 `ScrollStepLines` 一处不同 —— 这套是「跟底状态 × 边界 no-op × 标脏」交织的状态机，漏一处就是「滚一下永久失去自动跟底」或「手动上翻又被拽回底部」）、`TuiView.SetTreeDirty`（树标脏唯一遍历，`MarkDirtyTree`/`MarkDirtyTreeQuiet`/`MarkItemContentDirty` 共用 —— 后两者的差别只是**要不要叫醒帧闸门**）、`TuiWindow.Close(result, callback)`（关模态窗唯一出口，见下）、`ModelCatalog.RemoveProviderFromFile`。
**再续（v0.96.105 后）**：`Tools/McpConfigStore.cs`（`.waycoder/mcp_servers.json` 的唯一读写 —— 此前三处各写一套「读→去重→写」，**去重口径相反**（一处忽略大小写、一处区分）⇒ 同一份配置两边判定不同、导入写进重复条目；且三处都是 `File.WriteAllText(..., Encoding.UTF8)`，**非原子 + 凭空带 BOM**，与 NotebookEditTool 写坏 `.ipynb` 同类；现在原子写 + 无 BOM + 统一口径）、`Infra/ProcUtil.BuildPsi`（进程启动样板唯一实现 —— `KillTool`/`PsTool` 两份**逐字相同**，连「本工具此前漏了 `ProcEncoding.Apply`」这个修复都各做一遍）、`Infra/ProcEncoding.IsConsoleWrapperName` + `ApplyIfConsoleWrapper`（**「该不该套 OEM 解码」的判据唯一真源**：此前散在十几个启动点各判各的，8 处该 Apply 的漏了 —— 自更新跑 `cmd.exe`、LintTool 跑 `npx`、McpTransport 起 npx 型 server。**判据是「启动的是什么」**：cmd 系包装器（cmd.exe/.bat/.cmd/npm shim）才套 OEM，原生程序（git/dotnet/gcc/语言服务器）输出 UTF-8，套上反而乱码 —— 「所有启动点都调 Apply」是错的）。
**两条新踩坑（同批）**：① **`TuiScrollMath.Bar` 在 `total <= vis` 时滑块会长过视口**，`(long)(vis - thumb)` 变负 ⇒ `Math.Clamp(value, 0, 负数)` 的 min > max **抛 `ArgumentException`**。四处调用点此前各判一次 `total > vis`，属「四份守卫、漏一处即崩」—— 判据已收进 `Paint` 内部；**凡是收口几何计算的辅助函数，边界守卫也要一起收进去**，别留给调用方。② **关一个模态窗永远要按序做三件事**：写 `Result` → 跑调用方回调 → 触发 `OnClosed`（驱动出栈 + 唤醒渲染等待循环）。**顺序不可换**（回调正是唤醒等待线程的那一下，被唤醒的调用方可能立刻读 `win.Result`）。此前这三行在 `TuiDialog` 的 8 个构建器里手抄 47 处，漏 `OnClosed` ⇒ 窗口永不关闭、调用方一直挂着；漏 `Result` ⇒ 把「取消」读成「确认」。现在只有 `TuiWindow.Close(result, callback)`（+ 无结果的 `Close()`）。**`TuiScreen.OnKey` 的 Esc 兜底故意不走 `Close()`** —— 消息框（Info/Success/Warn/Error）**没注册 Esc 快捷键**，兜底关窗不该编造一个「结果」（保持 `Result = -1`）。这条事实**只有走「屏幕真实路径」（`ShowWindow` + `screen.OnKey`）的测试才测得到**，直接跑快捷键体会抛 `KeyNotFound`。
**三条反复出现的模式，下次直接按此排查**：① **「共享助手已存在但调用点绕过」占多数** —— `UxHelper.RunModalDialog` 的注释写着「收敛约 8 份」，同文件 40 行外 6 个私有方法没用它；`TuiControl.MouseInBounds` 有 13 处在用，还有 2 处手写 `GetAbsoluteX/Y` 边界判断（而那两处**恰好都在弹窗内**，正踩 `HitAbsX` 注释里记的「弹窗内点击错位」）。② **同一规则两处实现、只修了其中一处** —— `Detect` 缺 UTF-32 BOM 分支而 `Decode` 有（同一文件、同一张表，UTF-32 文件两条路径两种结果）；`todo`/`struct_todo` 共享 `todos.json` 却是两套读写。③ **必须手工同步的平行表** —— 权限/经济文案 6 套、~~MCP 状态图标 3 套~~（**已收敛，且实际查到 5 套**：TUI 侧栏/`/mcp`/命令行/连接状态汇总/Web 各一份，汇总那处还用 ASCII 的 `✓✗?` 与其余 `✅⏳❌` 不一致 —— 现统一 `McpStatusIcon.Text`/`.Dot`）、README/命令表。
   **平行表的典型来源**：同一份「清单」在两处各列一遍（`/model import` 与 `/provider import` 的源解析逐字相同、共享工具清单在桌面与 MAUI 各列一遍、写文件工具的锁提示文案各写一份）。判据不是「像不像」，而是**漏改一处会不会有用户可见后果**：会 ⇒ 收敛成真源 + 加护栏自测（工具清单那种「刻意分开、无法合并」的，就钉死差集）。
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
- **自绘层与平台输入框的叠放次序 = 「点哪儿」的判据归属（v0.96.144，用户实测的「点击偏差」真根因）**：`EditorPage.xaml` 里画布 `CodeCanvasView` 声明在**前**、浮动 `Entry` 在**后** —— MAUI 的 Grid **后声明者在上层**，于是编辑行那个 Entry（一个真 Android `EditText`）**盖在画布上**。后果不是「样式不好看」，而是**判据被整个交给了平台**：① 点它的触摸它接走，画布根本收不到（实测在编辑行点 5 下，画布自己的触摸日志**一条都没有**；点别的行立刻有）；② 「横坐标 → 字符下标」由**平台自己的排版 + 它内部自己的横向滚动**决定（那个滚动随光标位置变），与自绘层「逐字形推进量」的网格无关；③ 每格只差不到 1px，但**沿行累积**，到第 80 列就是一整格，叠加内部滚动还会跳变 ⇒ 用户看到的就是「手点和落点差好几格、**越靠右越明显**、而且不单调」。**修法：把 Entry 挪到画布之前（压在底下）** —— 触摸全归画布，点哪儿由 `CharIndexAtX` 一把尺子决定；它仍能被 `Focus()` 聚焦，**IME/软键盘/剪贴板照常**（它本来就只管这三件事），而**它的系统光标、选择手柄、放大镜正好被不透明的画布一并盖住**。配套：同一行分支里画布光标**直接取我们算出的列**（不回读平台值），`SyncCaret` 轮询从此只管**输入法改光标**（如打完一个字往右挪）。**验证方法（不用肉眼）**：临时把链路里每个量连同单位假设一起打日志（一次就看出哪个量不同源），再配合「洋红三角标记」把自绘光标变成**截屏里可程序化测量**的东西（`scripts/_taptest.py`：点 → 截屏 → 取三角尖端 x → 与该行墨迹栅距算出的格线比），闭环判据两条 —— **光标必须落在字形格边界上**、**该边界必须含住手指的 x（差 < 半格）**。同批还发现 `EnsureCaretVisible` 直接给 `_scrollX` 赋值**没有边界**，点一次行尾就永久「滚过头」（HUD `X3504/3412`，屏幕上留空白）；既有 `ClampScroll()` 就是那个唯一收口处，**几何计算的辅助函数要把边界守卫一起收进去**，别留给调用方。

- **软键盘遮挡光标：`adjustResize` 在 Android 15+ 已失效，要自己接 IME inset（v0.96.150）**：`MainActivity` 上写着 `WindowSoftInputMode=AdjustResize` 也没用 —— **Android 15（API 35）起 targetSdk ≥ 35 的应用强制 edge-to-edge，`adjustResize` 不再缩放窗口**，它现在只负责「让你能收到 IME inset」，剩下要应用自己按 `WindowInsetsCompat.Type.ime()` 调整。真机 `uiautomator dump` 前后一比：页面平台视图始终是 `(0,0)-(1080,2202)`，键盘只是**盖上来**、布局一点没动 ⇒ 症状是「点一条靠下的行 → 键盘盖住光标 → 什么都不滚」。修法：`EditorPage` 在**画布的平台视图**上挂 `ViewCompat.SetOnApplyWindowInsetsListener`（**不挂页面视图** —— 那上面已有 MAUI 的安全区监听，覆盖会连带弄坏；画布是叶子视图，MAUI 不管它），**inset 原样传下去、不消费**（它是窗口级的，吃掉会让别的控件一起失去内边距），把键盘高度换算成**根布局的底部内边距**。**选「压矮布局」而不是「在滚动数学里减去键盘高度」**：压矮之后画布高度/命中测试/滚动边界/绘制范围全部照旧，只多一条 `CodeCanvasView.OnSizeAllocated`（**变矮**时把光标行顶回视口、最小滚动；变高只收口边界、不无端跳一下）；后者要同时维护「两个高度」（画用大的、算边界用小的），正是本仓库反复踩的「同一件事两处实现」。**触发点必须是「真实的高度变化」，不能猜键盘动画时长** —— 原来那版是「点完**等 260ms** 再滚一次」，而键盘动画在 200~400ms 之间：猜早了算的还是旧视口（那行判定为可见 ⇒ 一个字不滚 = 没做），猜晚了用户已经看着自己被挡住 —— 症状恰好就是用户报的「**刚好弹出键盘时挡住光标**」。**三条踩坑**：① **`ime()` 不要再「顺手扣掉导航栏」** —— 网上通行做法（也是官方文档里 ime「may include」导航栏那句话）在这里**是错的**：实测行号栏结束 y=1453、状态栏 1453~1517、键盘上沿 **1517**，`ime()` 报的就是 1517，本机**没**算进导航栏；扣掉 64px 后内容区被多顶上去、状态栏直接掉到键盘底下。**判断依据别靠肉眼看缩放截图**（第一版就是这么误判的）：**扫一列像素看行号栏底色 `#F2F2F4` 在哪一行结束**，就得到内容区的真实下沿；② **跨版本会压两遍** —— `adjustResize` 只在 15+ 失效，同一份 APK 装到 Android 14 及更早的机器上那条老路**照常生效**，再补一次就是压两遍（编辑区被挤成一条缝）⇒ 判据**不写「系统版本 ≥ N」**（那是在猜系统行为），而是**直接量**：键盘弹出后页面还是满高 ⇒ 系统没管、我们补；已经明显矮了 ⇒ 系统管了、一个字不加；③ 三处绑定细节：`WindowInsetsCompat.Type` 是**嵌套类型**（`var t = ...Type;` 再 `t.Ime()` 报 CS0119，只能全限定写）、`GetInsets()` 返回**可空的** `Insets?`（点 `.Bottom` 报 CS8602，要 `?.Bottom ?? 0`）、`OnApplyWindowInsets` 的参数与返回在绑定里都可空（CS8767）。**验证**（Android 16 模拟器，UI 树 + 逐像素双读数）：点靠下（y=2000 → 光标 L28）画布 `2126→1453`、状态栏完整可见、L28 被滚进视口底部，内容区下沿与键盘上沿严丝合缝（1516 / 1517）；点靠上（y=700 → 光标 L15）画布同样变矮但**一行都没滚**（最小滚动原则保住）；返回键收键盘完整复位。
- **VML 游戏示例：手柄交给系统、手感交给音效接口（v0.96.173）**：用户对 `Examples/c/tetris.c` 的要求是「**俄罗斯方块本来系统有游戏按键，自己右画了一套，多此一举**，使用系统的手柄即可，然后加上声音效果、震动效果」。① **删掉自绘手柄 + 触摸命中**（净少约 140 行）：自绘那套的代价是三重的 —— 占约 140px 窗口高度（棋盘矮一截）、几何要在「画」与「命中判定」两处各算一遍（改个间距就「看着在键上、点下去没反应」）、每个游戏各画一套风格。**屏幕上已有的东西不要在程序里再画一遍**：绘图窗口底部本来就有一排屏幕手柄，程序只该收 `VML_MSG_KEYDOWN`，窗口就是一块显示区。② **手机手柄必须发真按下/抬起**（`DrawWindowPage` 由 `Clicked` 改 `Pressed`/`Released`）：`Clicked` 是**抬手才触发一次**，按住不放没有任何后续事件 ⇒ 程序只收到一次 `KeyDown`，「按住 ← 连续左移」根本做不出来（连发是程序拿定时器做的 DAS）。改完 `Pressed` 发 `KeyDown`、`Released` 发 `KeyUp`，单点仍是「先 Down 后 Up」只是中间隔了真实按压时长，向后兼容。**手指从一个键滑到另一个键时 Android 只发新键的 `Pressed`、旧键的 `Released` 会丢** ⇒ 按下新键前先替旧键补一条 `KeyUp`（否则程序以为两个键同时按着、连发一直挂在旧方向上）；`OnDisappearing` 也要补（页面走了不可能再有 `Released`）。③ **长按连发必须自带刹车，不能把「一定会收到 KeyUp」当前提**（v0.96.173）：手指划出按键范围、系统吃掉 CANCEL、页面被切走都可能让 KeyUp 永远不来，而 `ui_timer_set` 是**重复**定时器 ⇒ 现象是「方块自己一直往左移」。三道刹车：换键即接管 / 按了没动两次就停 / 总拍数上限（40 拍 ≈ 5 秒，而横穿棋盘只要 10 拍）。**凡是重复定时器都要问一句「谁来停它」**，并且要能在脚手架里验（本版给最小宿主加了「这个连发定时器跑了几拍才被杀」的观测点：`[repeat] #N 连发 M 拍后停`，实测丢 KeyUp 时 ← 是 4 拍自停、↓ 是 40 拍封顶）。④ **音效是单通道的，所以一次事件只发一个音**（`VmlAudio.ToneCore` 开头就 `StopTone()`）：连发一串琶音**只有最后一个听得见**，等于白写 ⇒ 改成「用频率高低表达好坏」（消行 1→880 / 2→1046 / 3→1318 / 4→1568 Hz，升级 1760 盖过消行音，结束 220Hz 长音）。⑤ **`${}` 展开总是先载入 R0** ⇒ `asm("MOVE R0 ${freq}"); asm("MOVE R1 ${ms}")` 生成 `move R0 [R12+12]; move R0 [R12+16]; move R1 R0`，**第二个参数把第一个覆盖掉**；多参数 syscall 一律走 `Lib/shared/vmlui.vml` 的包装函数（照 `ui_timer_set` 的模板：形参在栈上、`arg_i = [R12+8+4*i]`）。新加的五个 `ui_beep`/`ui_vibrate`/`ui_keep_on`/`ui_store_set`/`ui_store_get` 已在 `Lib/shared/vmlui.vml` 里（**分家后直接改即可，不必再提上游**，见 ⑱）。⑥ **⚠ 这一条的第一版结论是错的，已更正（v0.96.174）**：当时写的是「带缓冲区的接口在 C 里有**两条**既有缺陷（局部数组地址传参错 + 全局 `char` 数组下标读成 32 位）」。**前者不存在** —— 局部数组传参一直是好的。真因只有一条：`InferExpressionType(ArrayAccess)` 只查 `variableTypes`（只装局部变量），**全局** `char`/`short` 数组查不到就退化成 `ExprType.Int` ⇒ 元素访问走 32 位 `MOVE`（应 `MOVEB`/`MOVEH`），于是「长度对、内容不对」，写还会越界。**局部数组反而正常**，所以这个坑只在全局数组上冒头。已修成 `patches/0004-c-global-array-elem-type.patch`（那是**分家前**的约定：不改上游源码、在 `sync.sh` 的【B】清单里加一条 patch + 一个复现用例；**分家后直接在 C 前端源码里改**，见 ⑱）。**误判的来源是判定方法**：第一版拿 `puts` 的输出当判据，而本环境的 `puts` 在字面量多的程序里输出会**串行/重复**（同一个字符串打出两种结果）⇒ 把 stdio 的毛病看成了 codegen 的毛病。**改成不经过 stdio 的判据**（每条结论用一个 `ui_beep` 频率报出来、宿主原样打印；`ui_dlg_msg` 也是现成的"宿主从内存里读到的字符串"探针）之后，六个格子一次就量清了：局部下标读/写 ✓✓、全局下标读/写 ✗✗。**凡是"猜编译器"的结论，先在判定链上把 stdio 摘出去。**⑦ **两条「估算/缩放只做了一半」的坑（v0.96.173，都是用户实测报出来的）**：
   ① **只缩一个维度 = 另一个维度必然被切**。`DrawWindowPage.FitCanvas` 原来写「宽 = 视口宽，
   高 = 宽 × 场景高宽比」—— 宽度装得下，**高度完全没管**，场景一高就被外面那层 `ScrollView`
   截在可视区外（用户："内容超出绘图区，下面被键盘区挡住"；游戏要滚动才看得全 = 已经不能玩了）。
   改成 `min(视口宽/场景宽, 视口高/场景高)` 之后，"被切掉"从结构上不可能出现；估算准的时候
   缩放比恰好 1、与原来完全一致。**配套的那条同样重要**：`OnSizeAllocated` 里的重排判据要从
   「宽度变没变」改成「`FitSize` 算出来的两个数变没变」—— 视口变矮时宽度可能没变而高度变了，
   只比宽度就漏掉这次重排。**"只处理一个维度"这种半截活，下次照这个模式先问"另一个维度呢"。**
   ② **扣减项漏了一项 = 每次会话的第一个窗口必定偏大**。`VmlUi.AvailableArea` 的固定占用
   写着 170，只算了导航栏 + 方向键，**漏了绘图窗口页自己的折叠条（26dp）与画布留白（16dp）**；
   而 `SCREEN_W/H` 在**第一次开窗之前**只能靠这个估算（真实视口要等页面布局完才由
   `MeasuredViewport` 量到）⇒ 每次会话的第一个 VML 窗口都高约 90dp。**这类"估算值"要拿
   实际布局的每一项去对**（XAML 里 `HeightRequest` 一项一项加），不能凭印象写个整数。
   同批：自测里那个写死的 170 改成引用新常量 `VmlUi.DefaultChromeHeightDp` —— 平行表正是头号坑。
⑧ **胜负这类"这一局唯一必须让玩家知道的事"，一行小字等于没交代（v0.96.173）**：
   `gomoku.c` 原来只在棋盘下面写一行 15px 的「你赢了！点任意处再来一局」，玩家盯着的是棋盘
   ⇒ 用户的原话是「赢了输了都没看到输赢的提示框，只是棋盘清空了，重新开始了」。
   现在四种结束方式**汇到同一处收尾**（先画终局棋盘 → 出声 → `ui_dlg_msg` 问「再来一局？」→
   选「否」退出窗口）。**收尾逻辑收在一处**这件事本身也有价值：原来"结束"散在四个 `continue`
   分支里，加上重开就是五处各写一遍。同批给落子/胜负配了音效，**赢与输的音高差别要大到
   不看屏幕也分得出**（合成音是单通道的，只能用音高表达情绪）。
⑨ **「同一段代码有时对有时错」的根：`Lib/` 里两套栈清理约定并存（v0.96.175）**：
   C 前端生成的函数是**调用方清参数**（`move R13 R12; pop R12; pop R15; ret`，调用点后面跟
   `add R13 #4/#8`），而 `Lib/` 里 **764 个函数是"被调用方自己清"**
   （`…; pop R15; move R1 @13; add R13 #N; push R1; ret`），另有 446 个与前端一致。
   ⇒ **每调一次那 764 个之一，调用方的栈指针就多释放一次**（`strlen` 一次多 4 字节）。
   漂了之后凡是**用 `pop` 取临时值**的地方都读错 —— 实参槽（`move R0 [R13+0]`）与
   **数组下标的中间量**都在此列，于是"判据没错、读到的是错的数"。
   **最小复现**：`ui_beep(10000 + strlen(loc), 1)` 实测报 `6`（应 `10005`，`.scratch/dup_a.c`）；
   而把 `strlen` 单独调用（不嵌在实参里）四次之后，纯字面量调用仍正常（`.scratch/drift.c`）
   —— **影响面是"漂了之后谁用 pop"**，不是"调了就坏"。
   **只诊断不改**：库函数之间也互相调，A 调 B 时 A 是否清参数取决于 B 的约定，一刀切统一会改坏
   另一半；正解是**用当前前端把 `Lib/` 整个重新生成**（分家后这已是本仓的常规操作：
   `touch Lib/shared/src/*.c` + `GenLib -b`，见 ⑱ 与 `third_party/vml/FORK.md`）。**只调 `waycoder_ui.h` 包装函数的程序不受影响**（那些是"调用方清"，
   与前端一致）—— `Examples/c/tetris.c`、`gomoku.c` 都是这一类，实测正常。
   ⚠ 同时更正 v0.96.173~174 期间的一句错话：**没有任何单个库调用会踩坏调用方的局部数组**
   （逐条验过，掩码恒为"一个都没坏"，`.scratch/clob2.c`）—— 坏的是**栈指针**，不是那块内存。
⑩ **完善绘图接口：先摸清"哪些能力 DSL 里本来就有"（v0.96.176）**：用户要"渐变刷子 / 路径 /
   曲线"。动手前把链摸了一遍 —— **大半能力在绘图 DSL 里早就有了**（`gradient` 定义 + `@id` 引用、
   `path`（SVG 语法）、`polygon`/`polyline`/`star`/`pie`/`ring`、`translate`/`rotate`/`scale`/
   `push`/`pop`、线帽、虚线、抗锯齿），桌面 `draw` 工具一直在用，**缺的只是 VML 侧的 syscall 入口**。
   于是这一版没新造图元，只加了 6 个号（534–539，号段里唯一空着的一段）把它们接出来 ——
   **"加接口"之前先问一遍"这个能力是不是已经在了，只是没接出来"**。真正的缺口是另一处：
   `path` 的光栅化只认 `M`/`L`/`Z`、**曲线段被静默丢掉**，而 `EmitSvg` 把整条 `d` 原样交给矢量
   后端 ⇒ **同一份 DSL 导出 PNG 与导出 SVG 图形不一样**（只在导出位图时才发现）。补
   `Infra/DrawPath.cs`（纯数学、可自测）：完整 SVG 语法 + 曲线按控制多边形长度自适应分段 +
   圆弧走规范 F.6.5 的端点→中心参数化换算（含半径不够时按规范放大），展平后直接复用现成的
   `FillTransformed`/`StrokePolyline`（自带变换与渐变），**不必给光栅器再加一套曲线求值**。
   **三条踩坑**：① **单位只在边界换算一次**：渐变几何对外是"千分之一"整数、DSL 里是归一化 0..1，
   换算只放在 `VmlScene.AddGradient` 一处 —— 两端各算一次就是**沉默的错**（方向向量变 1000 倍长 ⇒
   `t` 恒 0 ⇒ 整块只剩色 A，现象像"渐变没生效"，而 DSL/解析全都正常，实测踩过一次）；
   ② **弧形方向那一位**：SVG 的 y 轴向下，`sweep=1`（正角方向）= 屏幕上顺时针 ⇒ 从左点到右点
   **向上**鼓（y 为负）——断言写反会把对的实现判成错的（这一版就先写反了）；
   ③ **抗锯齿后的"黑"不是纯黑**：3× 超采样再盒式降采样，3px 宽的线中心也只有 `#555555` 左右，
   按"接近纯黑"（R<80）判会把"确实画上了"判成没画 —— **像素判据要相对背景**，不是绝对黑。
   另：`RasterImage.ColorAt` 的序是 **0xAARRGGBB**（`(A<<24)|(R<<16)|(G<<8)|B`），别看反。
⑪ **绘图性能：先立基准，再按数据优化（2.1×）（v0.96.176）**：用户要"写个 benchmark 专门测绘图
   性能，优化绘图性能"。① **复用既有 `Benchmark` 骨架加一个「绘图」类别**，不另写一套报告；
   场景取自**真程序**（俄罗斯方块满盘 / 五子棋满盘 / 路径曲线渐变）而不是玩具场景，并把每帧拆成
   **图纸→DSL / DSL→文档 / 光栅化+PNG** 三段 —— **只报一个"每帧 X 毫秒"没法指导优化**，
   分段之后一眼看出前两段各 1–2ms、瓶颈全在第三段。② **再拆一层才找到杠杆**：把 `Antialias`
   关掉再量一遍，发现**抗锯齿占光栅那一段的 87～89%**（俄罗斯方块 222ms 里 194ms 是它，关掉只要
   29ms）—— 固定 3× 超采样在手机全屏上就是"把 183 万像素画一遍再缩回来"。③ **优化：超采样倍率
   按画布面积自适应**（`DrawRunner.ChooseSupersample`，预算 120 万像素；小画布仍 3×、手机全屏 2×、
   超大画布 1×）—— 代价是 `O(W·H·s²)`，而画质收益是固定的观感改善、**不随画布变大而变大**，
   这正是该自适应的理由。实测 **225→108 / 145→71 / 177→87ms**。④ **诚实的差距**：目标仍是 33ms
   （30fps），当前满盘 71–108ms，抗锯齿仍占 71–75%；基准阈值因此设成"当前实测的两倍上下"，
   作用是**抓回归**而不是假装达标 —— **别为了让报告变绿去调阈值**。
⑫ **桌面脚手架的三条既有事实**（写进 `docs/VML宿主接口.md §8`；其中 `printf` 崩与 `puts` 串行两条**至今未查清**，写桌面验证程序时绕开它们）：VM 的 `#50` Random 是 **`new Random()` 时间播种**的 ⇒ 同一程序两遍跑方块序列不同（实测两次跑出 171/179 两套分），**凡是要逐字节比对两条执行路径，随机数必须先在宿主侧钉死**；`ResolveLibPath` 只搜 `Lib/shared` 与 `Lib`、**不搜 `Lib/c`** ⇒ `crt.vml` 里 `.linked "stdio_funcs.vml"` 永远解析不到（只是一条警告，链接照跑）；**桌面脚手架里 `printf` 的格式化路径会崩**（`MOVEB @2, R0`，地址是垃圾值；`puts` 正常），写验证程序用 `puts`。⑬ **验证方式**：给 `.scratch/vmlround` 加了脚本化陪玩（`--play` 跑完整局：连发/暂停/重开/游戏结束/退出；`--lines` 用钉死的随机数专造一次消行；`--best N` 预置最高分验读取路径），判据仍是「直接编译运行」与「存成 .vml 再独立汇编运行」**逐字节相同** —— 全部通过。**要造消行必须专门摆**：`spawn` 永远把方块放第 3 列，光直落只堆中间，20 块随机方块一次都消不掉（实测），所以钉死成 O 块（`ui_piece_cell(0,0,*)` = (1,0)(2,0)(1,1)(2,1)，**方块号 0 是 O、不是 1**）再按列对 px = -1/1/3/5/7 摆满。


⑭ **绘窗出图的两条判据都错过一次：先是"变了就出图"，再是"快照拍晚了"（v0.96.178~179）**：
用户报「俄罗斯方块有时抖动闪烁」，根因**是两个**，而且都在这条链上：
① **出图判据用错了标记** —— 窗口原来按 `VmlScene.Version` 出图，而那个数**每个图元 +1**
（一帧上百个图元 = 上百次"变了"）⇒ 40ms 的定时器撞上"`ui_clear()` 刚清完、棋子还没画"的
那一刻，就把**半成品**贴上去；真机上光栅化+PNG 要上百毫秒，空棋盘在屏上停到肉眼可见，
这就是「有时」（取决于重画跨度与节拍相位）。**程序其实一直在说"这帧画完了"**：`ui_present()`
（531 号 `DrawPresent`）两个游戏每帧末尾都调，只是宿主收到后把它当成了又一次"内容变化"。
修法：场景加 `PresentVersion`（只由 `ui_present` 递增），窗口按它出图；没调过 present 的老程序
退到「**这一拍内容没再变**」（画完再说，晚一拍）。**「内容变了」与「一帧画完了」是两件事。**
② **快照拍晚了**（改完①仍然闪）——真正 `BuildDsl()` 拍快照发生在**下一次定时器醒来时**
（最多 40ms 后），而那 40ms 里 VM 早已开始画下一帧（先 `ui_clear()` 再重画）⇒ 拍到的还是半成品。
修法：`Present()` **当刻就把这一帧定下来**（`PresentedDsl`），渲染只负责取走。
③ 配套的**抖动**另有其因：`ShowFrame` 每帧都调 `FitCanvas`，而拟合尺寸是按视口比例算的
**带小数的值**，视口测量一浮动画布就每帧微调一次 ⇒ 整块棋盘跟着缩放（连拍实测抓到
`73876/73290/73166` 三个像素量级，1% 漂移）。改成**只在"还没有尺寸"时兜底设一次**，
之后交给 `OnSizeAllocated`（带 0.5dp 容差）。**判据换成了日志里的图元数**：修好后每帧恒为
`60 矩形+7 刻度+1 条 = 68`，而不是 5/17/28/29/34/37/47/51/59 那样参差 —— 比截图数像素干净得多。
⑮ **真机分段计时（`adb logcat -s WCVML`）+ 我自己两次"量错了"（v0.96.179）**：绘窗每 30 帧打一行
`DSL/解析/光栅+PNG = 后台｜解码/贴图 = UI｜图元 N｜**实际 fps**`。三条教训：
① **`ui_wait(msg, 0)` 是「无限等」，不是「不阻塞」**（宿主 `Take(0) → Wait(Timeout.Infinite)`）——
我拿它当轮询写帧率探针，量出来的"1 fps"其实是定时器频率，白绕一圈；**要轮询用 `ui_poll`**。
游戏该用哪个看主循环形状：事件驱动（常态省电）用 `ui_wait`，连续动画用 `ui_poll`+自己节流。
② **"图元数参差 = 快照没修好"是我的仪表错了**：`scene.FigureCount` 是在 UI 回调里读的，那时后台
已经光栅完一整趟、VM 又画过好几轮，读到的是"此刻"而不是"这一帧"；改成**从这一帧的 DSL 里数**
（数换行减 2 行表头）才对。**"某个分段很慢"也可能是被污染的量。**
③ **`实际 fps` 与 `单帧耗时` 必须同时报**：单帧 10ms 也可能因为节拍只出 2 帧/秒；反过来
`FinishRender` 会在渲染完成后**立刻再查一次**，所以实际出帧并不完全受 40ms 节拍限制
（实测 25.6 → 35fps 的差异就来自这里）。
⑯ **绘图「直接写屏」：加第三条画法，而不是安卓专用实现（v0.96.180）**：用户问「直接写屏是不是更快」，
并定「先做安卓，后面支持所有平台」。**账先算清**：真机每帧 `后台 26ms（光栅 25~30 + PNG 编码）
+ UI 54ms（PNG 解码）`，每帧新建 333KB 位图 ⇒ 25fps ≈ **10MB/s 垃圾**、10 秒 GC **355 次**
—— 帧率当时并没被卡住（25.6fps 顶在节拍上），**收益在 CPU/耗电/发热与那 355 次 GC 停顿**
（"抖动"里属于平台的那一半）；**PNG 那一段是纯浪费：编出来立刻解回去上屏**。
做法顺着既有形状长：`DrawCommand` 本来就有两个画法（`Rasterize`/`EmitSvg`），加**必需成员**
`Vector`（**不是默认空实现** —— 漏一个指令会静默少画东西、只有上手机才看得出，**编不过最省事**）；
`IVectorTarget` + `MauiVectorTarget`（MAUI `ICanvas`，**一套代码两端跑**，安卓验完 iOS/桌面只开开关）；
16 个指令的矢量画法**几何全部取自同一个 `DrawGeo`**（与 SVG 同源，一行没重写）；**变换先落到点上**
（同一个 `Canvas.TransformPoints`）⇒ 平台侧不做变换、两条后端坐标语义一致。
**安全网**：画不了的图元 `MarkUnsupported` ⇒ **整个窗口回退光栅**（宁可慢，别少画）。
真机对照：**后台 26→0.8ms、UI 54→0.0ms、25.6→92~107fps、GC 355 次/10 秒 → 295 次/40 秒**，
俄罗斯方块整屏截图逐项核对无误（含中文文字与配色），抗锯齿由平台做、比 3× 超采样更好。
**自测**用**记录型落笔面**（桌面没有平台画布，就断言"文档→落笔"的映射）+ **表驱动**一条：
16 个内置指令每个都要能画出东西（新增指令忘写矢量画法时会红）。**遗留**：新瓶颈变成每帧
拼 DSL + 解析（0.8ms 与相应分配，GC 主要来自它），再省就得让矢量后端直接吃场景图元。

**跨端验证的手段（Windows 没有 adb，但可以用 UI Automation 驱动真机之外的第二个平台）**：
`WayCoder.Maui` 在 Windows 上是免打包的 WinUI（`WindowsPackageType=None`），
`dotnet build -f net10.0-windows10.0.19041.0` 之后直接跑 exe 就能验观感 —— 这一步值得做，
因为**它正是"跨端编译检查"的那道闸**（任何安卓专有 API、`#if ANDROID` 漏守卫都会在这里现形；
本仓已有"桌面构建全绿看不出"的教训）。驱动界面走 UI Automation：按**名字**找元素
（`TabItem` 用 `SelectionItemPattern.Select`、输入框用 `ValuePattern.SetValue`、按钮用
`InvokePattern.Invoke`）—— 比按坐标点稳（DPI 缩放与窗口位置都会让坐标漂）。
两条实测要注意：**按钮名常带 emoji 前缀**（`▶  运行`），精确匹配会落空、要按子串找；
**`.ps1` 里的中文必须存成带 BOM 的 UTF-8**（PowerShell 5.1 否则按 ANSI 读，中文标识符直接变解析错误）。
矢量后端两个平台的观感都核对过（Android 真机 + Windows 桌面），iOS/MacCatalyst 只到"我加的文件零错误"。

⑱ **`third_party/vml` 已与上游分家：改了就改了，不需要补丁（v0.96.212 定案）**：
**2026-09-17 起本副本是「移动设备手机端专用」的独立分支**，`sync.sh`（`rsync -a --delete`
整目录覆盖 + 重放补丁）**与它的 `WAYCODER_VML_FORCE_SYNC=1` 逃生口已删除** ——
不存在任何同步通道，因此**不存在「改完还要做成补丁」这一步**；直接改 `third_party/vml/` 下的
文件，改完就是最终状态。分家原因（调用约定统一 / `Lib` 就地重生成 / GenLib 包装规则 /
模块集合排掉 PC-DOS-单片机）、**35 个 csproj 必须手工维护的 4 条适配**（`OutputType`→Library、
去 `StartupObject`/`RuntimeIdentifiers`/`PublishAot`；不做则 NETSDK1150 / CS2017 / NETSDK1047）、
以及 `patches/` 的现状**都记在 `third_party/vml/FORK.md`**，动手前先读它。
`patches/` 是**分家前的历史记录，没有功能作用**，别照着老习惯往里加新补丁。

**但分家前那批补丁攒下的四条判据仍然通用**（它们与「有没有同步」无关，是"改动可不可信"的判据）：
① **别靠记忆枚举改动**，直接机械求差：`git diff <vendor 提交>..HEAD -- <八个 synced 路径>`
（把 VML 并进来那次提交就是现成的"上游基准"）；
② **按内容算覆盖，不能按文件名** —— 第一版兜底补丁把"已被 0002 提到的文件"排除了，
而 0002 只包了该文件"那次修复"的几处、文件本身还有别的改动 ⇒ **两头都没兜住**；
③ **判据要能跑**：`scripts/check-vml-patches.sh` 把补丁依次打到 vendor 提交的树上（临时工作树），
再与当前工作区**逐目录逐字节**比对。它现在验两件事：判据①（历史补丁 == 工作区，
`VMLPrepares` 等手写目录）与**判据②a（`Lib/` 用本仓 GenLib 重生成后逐字节相同）** ——
**后者才是分家后真正要紧的那条**：「`Lib` == f(源码, 前端, GenLib)」，改坏任何一环都会在这里现形。
看到「与重生成结果不一致（跑一次 GenLib -A/-b 就好）」**就去跑那个生成阶段，别去改生成物本身**
（实测 17 个 `<语言>/shared.*` 绑定就是这么发现陈旧的：内容一字未变、只是 `atoi` 三份合一后
它在清单里的**位置**变了，而绑定是那之前的产物）。
④ **补丁"能打上"必须验**：`--check --reverse` 只能证明"与我们工作区一致"，
证明不了"打得进上游"（0001 就曾上下文漂到严格 `git apply` 打不上）；
同理 **`apply` 的退出码只证明"补丁打上了"**，证明不了"该有的东西都在"
（上游整段删掉或改名时 apply 照样成功）⇒ 按名字逐个查标签定义与头文件声明。

⑲ **真机图元体检抓出的三层缺陷：宿主参数、前端代码生成、以及"相对谁归一化"（v0.96.182）**：
矢量后端在**构建全绿 + 自测 5841 全绿 + Windows 桌面也验过**的情况下，上手机逐格体检
（12 格，见 `Examples/c/draw_prims.c`）才发现 polygon / polyline 整格空白、线性渐变渲染成纯红。
① **宿主按"C 头文件看着像"读参数**：`ui_polygon(pts,count,fill色,stroke色,width,grad)` 被读成
`(pts,count,填充开关,颜色,…)` ⇒ 颜色取到 0（全透明）什么都不画；`ui_polyline` 更离谱，
把线宽当成了颜色、把 `grad` 指针当成了线宽。**判据是 `Lib/shared/vmlui.vml` 里的包装函数**
（`move R0 [R12+12]` … 逐条对应，比头文件还权威 —— 两边这次是一致的，但先看包装更保险）。
② **`(int[]){…}` 复合字面量：前端认得语法，却不产出地址，而且不报错。** `Parser.Expressions.cs`
里明写着 `if (Current().Type == LBRACE) return ParseInitializerList(); // compound literal`，
但 `ArrayInitializer` 的代码生成只挂在**变量声明**上（`varDecl.Initializer is ArrayInitializer`）
⇒ 表达式位置的地址没人算，编出来的代码把上一个寄存器（正好是"点数"）当指针推下去，
宿主去地址 3 读坐标、越界就地停。**桌面把汇编打出来一眼可辨**：具名数组是
`move R0 R12 / sub R0 #28 / push R0`，复合字面量是 `move R0 #3 / push R0`。
同类"编得过、跑起来才错"的还有 0002/0003/0004 三条（都已成 patch）——**这条已修成 patch 0008**：
解析到该分支就 `Error(...)`，并把顶层的容错恢复 catch 里的 `Parser_UnexpectedToken` 约定为
**不可恢复**（v0.96.183）——**只加一句报错是不够的**，那个 catch 会把异常吞成一行
`[RECOVER]` 日志然后继续编，用户拿到"能跑但少一段"的程序，等于没报。
配套：`MauiVml.BuildProgram` 必须接住编译器异常（原来只接 `OperationCanceledException`），
否则它落在 `Task.Run` 里就成了"未观察的任务异常"、手机上表现为"点了没反应"。
③ **"这个数相对谁归一化"必须先查再算**：渐变几何我连续错了两轮 —— 先给绝对场景坐标
（平台只认 0..1 ⇒ 塌成纯色），再"修"成"场景归一化 → 绝对 → 按包围盒归一化"（数对了、
但语义变成"整幅渐变的一小段"，实测紫→蓝，而程序要的是"这块左红右蓝"）。
正解是**原样透传**：我方 `Gradient` 本来就是"相对形状包围盒"的 0..1，与 MAUI 刷子要的
"相对刷子矩形的 0..1"同源。**三处真源一直摆着**：光栅 `nx=(lx-minX)/spanX`、
SVG `objectBoundingBox`、以及自测里早就钉住的"矩形左端偏红右端偏蓝"。
**先问"相对谁归一化"，再看那行除法** —— 按想象算两轮，比查一次贵得多。
④ **两条流程事实**：**`Examples/` 里加文件必须重跑 `scripts/make-vml-lib.sh` 再重打 APK**
（`adb push` 进去的会被 App 下一次解压覆盖掉 —— 实测推完能跑、App 重启后文件就没了，
时间戳整目录变成解压时刻）；**"改对一处"不等于修好**：宿主参数改完之后画面纹丝不动，
这正是"没复现 ≠ 已修复"，要继续往下怀疑（这次是靠把汇编打出来才到底的）。

⑰ **两条"脚本改代码"的坑（本会话各踩一次，都是静默失败）**：① **`python` 的 `re.sub` 替换串漏了
关键字** —— `("internal sealed ") + "partial " + ("RectCommand : IDrawCommand")` 把 `class` 吃掉了，
文件写下去才发现（`grep` 回读立刻可见）；② **CRLF 没匹配上、替换静默失败** —— 本仓工作区是 CRLF
（见上文非显而易见约束），脚本里用 `\n` 拼的 `old` 永远匹配不到，而我只看了自己 `print("ok")`
就当成落地了，结果真机上跑的还是老路径（日志显示仍在走光栅）。**两条同一个解法：脚本改完文件
必须 `grep` 回读确认，别信自己的打印。**

⑳ **Scheme 前端的六条缺陷是「各自独立」的，骨架一条都照不出来（v0.96.203）**：给 Scheme 补游戏例程时
写不出来，查下来是**六处互不相干**的帧/调用错误。**它们共用一个盲区**：`skel.scm` 的函数只有**一个参数**、
循环全在**顶层**、且**从不从函数里调库** —— 正好绕开全部六条。逐条（都是"能编译、跑起来才错"）：
① **`main` 不占帧 ⇒ 顶层变量只能放 3 个** —— 顶层绑定按 `R12+(12-off)` 寻址（`R12+8/+4/+0` 之后**继续往下**
到 `R12-4 …`），而 `main` 此前不 `sub R13`、`R13 == R12`，**压栈正好写在那一片**（12 个顶层变量求和得 24 应 78；
5 个变量夹几次调用得 65536 应 15）。② **≥2 个形参读错实参** —— 形参槽按 `--varOff*4` 递减分配（4、0、-4…），
**只在单参数时**恰好对；命名 let 那处写 `nlParams.Count-1`（0、-4…）则**只在两个参数时**恰好对 ⇒ 统一成
**`off_i = -4i`**（`arg_i` 在 `R12+12+4i`）。③ **从用户函数里调库函数必崩** —— 尾位置一律走
`GenTailRecursive`（搬实参进**当前帧**、释放当前帧、`jmp <名>_body`），那是**自递归**的尾调用优化，
对别的函数等于**跳过它的序言**；顶层调库正常（`_currentFunc == null` 本就不走这条路）⇒ 判据收紧成
`tailPos && op == _currentFunc`。④ **函数体 / `let` body 只编译第一个形式**（`(define (f) a b c)` 只编 `a`；
命名 let 只编 `l.Items[3]`；语句位 `let`/`let*`/`letrec` 只编 `l.Items[2]`）⇒ 四处统一成 `begin` 口径。
⑤ **命名 let 完全不占帧**（`__nl_N` 里一条 `sub R13` 都没有），体内局部量直接写进压栈区。
⑥ **帧大小按生成结束时的 `varOff` 算**，而 `let`/`do` 收尾会把 `varOff` 还原 ⇒ 低估（20 个局部量时帧算成 64、
局部量已写到 `R12-80`）⇒ `varOff` 改属性、顺带记 `_peakVarOff`、帧按峰值算。
**另有一条没法修、只能绕的**：**用户函数看不见顶层变量** —— 二者都按 `R12+(12-off)` 寻址，而函数里的 `R12`
是它自己的帧指针（`(define g 0)(define (w1)(set! g 5))(w1)` 会把保存的 R12 踩成 8）⇒ 游戏写成**扁平顶层程序**，
只把「参数全传、不碰全局」的纯函数抽出去。**教训：骨架全绿只证明"这条路径没坏"，不证明这门语言能用** ——
骨架的最小性本身就是盲区，补例程时要**刻意把每种形态各用一遍**（顶层变量 / 多参数函数 / 函数内调库 /
多形式 body / 命名 let / 多局部量）。

㉑ **BASIC「裸调函数」的语句被静默丢弃 —— 症状是「只弹对话框、不弹绘图窗口」（v0.96.204）**：用户真机报的。
`Parser.Core.cs` 的标识符语句分支**只认 `declaredSubs`**（`if (declaredSubs.Contains(tok)) ParseImplicitCallStatement();
return ParseLetStatement();`），而 `ui_win_open` 声明的是 `NATIVE FUNCTION`（进 `declaredFunctions`）⇒ 落到
`ParseLetStatement()`、那一行又没有 `=` ⇒ **静默丢掉**（连 `line_N` 行标都不发 —— 汇编里 `line_80` 直接跳到
`line_82`，**根本看不出少了东西**）。**为什么只有 `ui_win_open` 中招**：`ui_dlg_msg`/`ui_clear`/`ui_rect`/`ui_text`/
`ui_present` 全是 `NATIVE SUB`（裸调用正常），`ui_win_open` 是唯一「**有返回值 + 当语句裸调**」的那个 ⇒
窗口从没被打开、绘制全画在不存在的画布上，**对话框照弹**（「只弹对话框」这个症状正是这么来的）。
**两处修，缺一不可**：① 解析器认「声明的函数名 + 后一个 token 不是 `=`」⇒ 当隐式调用（排除赋值：`x = ...` 左边
也可能是与函数同名的变量）；② **`GenerateCallStatement` 此前只查 `subMap`**（函数在 `funcMap`）⇒ `subDecl == null`
⇒ 标签被编成 `sub_ui_win_open`（永远解析不到）—— 判据要与表达式路径 `GenerateSubFunctionCall` 对齐（native 用裸名、
否则 `sub_`/`func_`），BYREF 判据对两种声明都成立。**判据**：`grep -c 'call .*ui_win_open'` 从 0 → 1（两份游戏各一处）。
**教训同 ⑳**：「能编译、能跑、连对话框都弹了」不等于这条路径通了；BASIC 是**唯一**会把「函数当语句用」的语言，
前 20 门语言全绿也照不出来。

㉒ **`Lib/` 里 109 组函数被定义了两遍 —— 而且「哪一份赢」是不确定的（v0.96.207）**：用户追问
「为啥 printf 有 2 份实现？只要一份就行」「相同的函数只要保留一份，多的删掉」。机械扫一遍
`Lib/` 下 136 个 `.c`：**1090 个函数名里 109 组重复定义**（`strcpy` ×5、`printf`/`atoi`/`memcpy`/
`strlen`/`putchar` ×4…）。**四条可复用的做法**：
① **判「重复」要按「谁会被同时链接」，不是按「名字一样」** —— 重复定义只有在**同一个程序里
同时链上**时才有后果，而汇编器对重名标签是**静默容忍**的（取先/取后取决于实现）
⇒ 症状是「改了一处却没生效」，没有任何报错。查法：三条一起用 —— **编译日志**（`成功编译: X.vml`
会逐条列出真正装载的模块，这是最硬的一条）、`.linked` 全图（`grep -rn 'linked "x.vml"'`）、
以及**前端的「函数名 → 模块」映射表**（`CompilerBase/CompilerHelper.cs`）。本次 `c/stdio.c` 与
`c/vmlib.c` 就是靠「映射表里没有 `stdio`/`vmlib` 这两个键 + 编译日志里根本不出现」判定不可达的。
② **`printf` 那件事的真相比「2 份」更绕**：`shared/src/printf.c`（活）+ `c/printf.vml`（159 条指令的
**shim**，把 shared 的 `static` 助手导出成 `c_emit`/`func_emit` 这类跨模块名 —— **它不是第二份实现，
别删**）+ `c/stdio.c` + `c/vmlib.c` + `c/src/printf.c`（三份不可达）。**判断「谁是真实现」要看
编译日志里谁的指令数配得上那门语言**（shared 4100 条 vs shim 159 条）。
③ **最危险的不是重复本身，是「重复 + 会覆盖」**：`build_libs.sh` 的 Phase 3 是
`c/src/*.c → c/<name>.vml`，而 `c/src/` 里**只有 `printf.c` 一个文件** ⇒ 谁跑一次构建脚本，
它就把那个**能工作的 shim（3146 字节）覆盖成自己的编译产物（7915 字节）**。一份**从没被链接过**的
源码，唯一的作用就是等地雷式地毁掉旁边能跑的文件 —— **加「生成物」目录时要问一句「同名的源会不会
盖掉我手写的东西」**。
④ **映射表（C# 集合初始化器）里的重复键是另一种毒**：`["sleep"] = "builtins"` 与
`["sleep"] = "time"` 并存时**后写覆盖先写**，而 `sleep`/`get_tick` 的**唯一实现在
`shared/src/builtins.c`**（`SYSCALL #52`/`#53`）、`time` 里根本没有 ⇒ 实际生效的是那条**错的**，
编译器会去链一个不含它们的模块（平时被 `builtins` 恰好也在链上掩盖住）。**清理这类表的判据**：
**键和值都相同**才删（纯冗余，保留最先出现的那个）；**同键不同值是真冲突，必须报出来人工判断**，
绝不能静默留一个 —— 本版清了 24 个纯冗余条目（`printf` 同行写两遍、`ltoa/dtoa/atol/atod` 整组重复），
两处冲突单独标出。改这类文件记得**保 CRLF**并核对 `git diff --stat` 不是整文件。
⑤ **顺序不能反**：C++ 前端那条 `printf→print_str` 捷径这次**没删** —— 它遮住了「1 实参」的情形，
而库里的 printf 眼下 `%` 转换产出零个字符（`sprintf(b,"d=[%d]",42)` 打出 `d=[` 后崩在
`MOVEB R0, @0`）。**先修库、再删捷径**；反了就是拿一个可见的回归去换一个看不见的整洁。
（`%` 那件事根因与 ⑨ 的「`Lib/` 两套栈清理约定」同源，正解是用当前前端重新生成整个 `Lib/`。）

㉓ **「CALL 目标重定向」用纯后缀匹配 ⇒ `_printf_itoa` 被认成 `itoa`，printf 的所有 `%` 全废（v0.96.208）**：
用户报 C 的 `printf` **一个字都不输出**（但**执行继续**，后面的 `puts` 照常）。查到底是一个
**后缀启发式**在链接期把调用换掉了。`LibraryLinker` 的「情况2」（已解析到包装器 → 找更好的实现体）：
```csharp
if (target.EndsWith("_" + bareName) && target.StartsWith("lib_") && ...) operand.Value = bestImpl;
```
`shared/src/printf.c` 的 static 助手叫 `_printf_itoa`，链接后是 `lib_printf__printf_itoa`
（前缀 `lib_<库文件名>_`）—— 它 **`EndsWith("_itoa")`** ⇒ 被当成「itoa 的包装器」，
**整个调用被重定向到 `convert.c` 的 `itoa(int value, char* dst)`**（**参数顺序完全相反**）
⇒ `itoa(42, tmp)` 把 42 当目标地址去写、`%d` 返回垃圾长度且 `tmp` 未被填
⇒ **所有 `%` 转换静默失效**，而字面量正常（那条路只经过 `emit`，不经过它）。
**四条可复用做法**：① **名字里含另一个函数名的函数是雷区** —— 源码里那句注释
「前缀 `_printf_` 避免与其他库冲突」正是前人给这个碰撞打的补丁，而 `EndsWith` 把规避**整个架空**；
② **判据要带边界**：target 得**恰好**是 `lib_<模块>_<裸名>`（模块名 = 库文件名 basename，
与 `libPrefix` 同源，为此新增 `moduleNames` 收集），这样 `lib_builtins_itoa` 照旧重定向、
`lib_printf__printf_itoa` 需要模块名 `printf__printf`（不存在）⇒ 不误伤；
③ **「break 在字典遍历顺序第一个匹配」本身就是不确定行为** —— 改成取最长匹配；
④ **诊断靠"函数入口插桩"**：在 `_printf_itoa` 内部插桩**一个字都不打**、在调用它的
`vsnprintf` 里插桩正常 ⇒ 一句话断定「调用根本没进那个函数」；再插桩读数
`len=5 val=42 t0=0 t1=0` ⇒ 实参对、缓冲区没被填。**中途我改过一版"取最长匹配"没修好**
（`globalLabelMapping` 里只有 `itoa`、没有更长的 `_printf_itoa`），那是**未经验证的链接器
行为变更**，已撤回 —— **没修好就先撤，别留一个说不清效果的改动**。

㉔ **同一个东西的"第二份实现"往往不在你以为的地方 —— `#include <stdio.h>` 把 C 导到了另一份 printf（v0.96.208）**：
用户最早那句「为啥 printf 有 2 份实现」的真身是 `Lib/c/stdio.h` 里一句
`#param lib("stdio_funcs")` —— 它把 `c/stdio_funcs.vml` 拉进链接，而那份文件
（41763 字节、**连 `.c` 源都没有**）里**只有 printf 家族**（`printf`/`sprintf`/`snprintf`/
`shared_vsnprintf`/`_vformat_buf`/`_put*`），**一个独有的 stdio 函数都没有**，
且是坏的。**隔离判据极其干净**：同一份 C 代码，唯一差别是那个 include ——
不带 → `call lib_printf_printf` 三行全对；带 → `call lib_stdio_funcs_printf` 一字全无。
⇒ 按「相同的函数只留一份」删掉，并把 **d/dart/fortran/objc/r/ruby 六门**（它们的
`stdio.vml` 也在链它）改为链 `../shared/printf.vml`。
**两条通用教训**：① **「有没有第二份实现」要按"链接进哪个模块"查，不能按"源码里搜同名"** ——
`#param lib(...)` / `.linked` / 前端的「函数名→模块」映射是三条独立的入口，任一条都能把调用导走；
② **顶着 `Auto-generated` 注释的文件也可能是陈旧件** —— 那 6 个 `<lang>/stdio.vml`
链着 GenLib 源码里**早已不存在**的 `stdio_funcs`，是更早版本的输出。

㉕ **GenLib 是唯一生成器，`build_libs.sh` 才是冗余的那个；但 GenLib 的产物里有陈旧件（v0.96.208）**：
用户问「GenLib 是不是也没有用处？」。**实测**：让 GenLib 重新生成 `c/` 的 74 个模块
→ 与签入的**零差异**（幂等、权威）⇒ **GenLib 必须留**（`c/` 下 84/87 个 `.vml` 头一行就写着
`Auto-generated by GenLib`）。真正冗余的是 `build_libs.sh`/`.ps1`：它重复实现 GenLib 的
`-b`/`-m`/`-a` 三阶段（**更旧的语义**、缺 `-g`/`-n`），除历史 CHANGELOG 外无人引用；
**而且它的 Phase 3（`<lang>/*.c → <lang>/<name>.vml` 无差别遍历）会覆盖 GenLib 的 shim**
—— 这是本仓记过两次的地雷源头。**「哪个生成器该留」的判据 = 跑一遍看 diff**：
零差异的那个是权威，另一个是冗余。
⚠ **GenLib 产物里确实有陈旧件**：那 6 个 `<lang>/stdio.vml` 顶着 `Auto-generated` 却链着
GenLib 源码里早已不存在的 `stdio_funcs` ⇒ **下一步：对全部 23 门语言重跑 GenLib，
判据是 `git status` 应几乎无改动；凡有改动处就是一处「签入产物与生成器不一致」的陈旧件。**

㉖ **单元测试的判据是「副作用可不可观测」，不是「有没有返回值」；冒烟不许冒充 PASS（v0.96.208）**：
用户判断「单元测试估计也只能测有返回值的，没返回值的判不了好坏，只能判有没有」——
**对了一半**。判据是**有没有可观测的副作用**。扫 959 个导出函数的实测分布：
**A 有返回值 638(66%)** / **B void + 指针形参 178(18%)**（**从被写穿的内存断言** ——
`sprintf`/`strcpy`/`memcpy`/`memset` 全在这类，**缓冲区就是那个"返回值"**）/
**C void + 往 stdout 写 32(3%)**（断言**程序自己的 stdout 字节**）/
**D void + 无指针形参 111(11%)** ⇒ **89% 可以真正判好坏**。
**D 类大半还能救**：不是"没有副作用"而是"副作用写到设备上去了" —— `graph.*`/`graphics.*`
走**场景图元可数**（`VmlScene`）或宿主**截屏逐像素比对**、`crt.CRT_*` 断言写出的字符序列。
**最重要的一条**：冒烟（`SMOKE`）**只证明没崩/没挂/没超时**，runner 把它**单列一档、
不计入 PASS** 并单独打警告和名单 —— **不许拿"没崩"冒充"正确"**（111 个都写成"跑通了就算过"
的话报告 100% 绿而质量是零，比不做更糟，因为它给的是假的信心）。
新建 `third_party/vml/test_shared/`（判据是**自己算一遍再和库的返回比**；
**同时查返回长度和逐字节内容** —— 只查长度会漏掉「长度对、内容写了个 0」这种本次故障的
原始形态；**不经过 stdio 做判据**）。⚠ 用例**不能放 `Lib/` 里面**（那是 rsync `--delete` 目标）。

㉗ **「初始化器里的负数」被静默编成 0 —— 正数全对，所以只看源码永远看不出来（v0.96.212）**：
用户真机报「吃豆人按键有反应、人不动」。**根因不在游戏，在 C 前端**：
```c
int A[4] = {  0,  1,  0, -1 };   /* 程序实际读到  0  1  0  0 */
int B[3] = { -5,  7, -9 };       /* 程序实际读到  0  7  0    */
```
① **真身是兜底分支吃掉了整类节点**：摊平初始化器的两条路
（全局 `FlattenArrayInitializer`、static 局部 `FlattenArrayInitValues`）只认
`NumberLiteral`/`CharLiteral`/`StringLiteral`/`Identifier`，**其余 `result.Add(0)`**；
而 `-3` 根本不是 `NumberLiteral`，是 `UnaryOp("-", NumberLiteral(3))`
（`Parser.Expressions.cs` 的一元分支）⇒ **负数整类变 0**。同类还有 `1+2`、`~0`、`1<<3`。
② **影响面按"谁用负数常量数组"算**：**方向表首当其冲** —— `pacman.c` 的
`int DX[4] = {0,1,0,-1}` / `int DY[4] = {-1,0,1,0}` 编成了 `{0,1,0,0}` / `{0,0,1,0}`
=> **只剩「右」「下」两个方向存在**（`col_of` 一路向右，正是症状）。
③ **修法**：新增 `VMLPrepares/CCompiler/ConstFold.cs`（一元/二元常量折叠），两处摊平函数接上。
整数运算走 **`unchecked`**（= C 的 int 回绕语义，`int x = 0xFFFFFFFF` 就是 -1），
**与 `Parser.TryConstInt` 刻意不同** —— 那个用于**数组维度**必须 `checked` 报溢出（patches/0018），
两处需求相反 ⇒ 是两份实现、不是重复。
④ **闸门要能响，且要反证**：`vml-out-probe` 的 `nat.c` 里 `OUT-INT` **不写字面量 42**，
而由 `int BASE[2] = { 45, -3 }` 加出来 —— 负数一丢就变 45。**已把闸门改回去反证过**：
`nat.c FAIL OUT-INT=45` ✓（不响的自测比没有更糟）。
⑤ **排查手法（可复用）**：**"源码看着对、跑起来不对"时，把程序\*\*实际读到的数据\*\*打出来** ——
一段循环 `print_int` 一遍数组，一眼就看到 `DX0..3=0,1,0,0`；
盯源码推理两轮都不如这一下。（同一个迷宫我还先被"看着像连通"骗过一轮，
真正的判据是**泛洪验证**：`豆子总数=193 可达=193`，写完就红了才对。）
⑥ **同批修掉 pacman 的位置模型**（另一条独立缺陷，症状同样是"人不动"）：
`near_center` 错了两轮 —— 先判成"坐标是 `TILE` 的整数倍"（那是**格线**，吃豆人停在**格心**），
再改对判据却仍"一拍直接走 spd 像素"⇒ `col_of()` 一越过格线就报下一格、
"前面是墙"随即在**离格心半格**处把人钉死（实测 `pdir` 恒 1、`pxp` 恒 **316**，而格心是 **325**）。
**正解：逐像素推进 + 精确格心判定** —— 从格心出发、每次 1px ⇒ 必然精确经过格心，
**与速度无关**（旧写法在 `spd` 不整除 `TILE` 的关卡还会累积漂移）；鬼必须用同一套走法。
⑦ **"位置模型"要一次想清楚**：**判定、吸附、停下必须是同一个位置** —— 这三件事分家，
就会出现"意图在变（`pwant` 对）但位置不动"这种最容易被误判成"按键没收到"的症状。

## 模式体系（三分钟版，竞品对标）

WayCoder 的模式参考 Claude Code / OpenAI Codex / Crush / Aider 划分为**四个正交轴**（完整版见 [docs/模式体系.md](docs/模式体系.md)）：

- **确认轴**（权限模式 `PermissionManager.Mode`，Ctrl+P · `/permit`）：管「何时打断确认」——Ask(必问)/Auto(改必问≈Ask)/SmartAuto(危必问)/Yolo(不问)
- **边界轴**（沙箱 `SandboxManager`，`/perm`）：管「能碰什么」（可写范围/网络）——对齐 Codex `sandbox_mode`，现状与确认轴纠缠（full-auto→Yolo 联动），待解耦
- **行为轴**（工作模式 `WorkMode`，Shift+Tab · `/mode`）：管「工具有没有 + 干什么活」——Build 全量（受经济模式管）/ Plan 只读白名单+精简提示词（有审批门）/ **Chat 纯聊天（0 工具 0 提示词）**；**槽位实例级**（`Agent.cs:91`）
- **省钱轴**（经济模式 `EconomyMode`，Ctrl+E · `/config economy`）：管「花多少 token」——提示词档位 + 压缩阈值 + 输出上限（Build 档删工具=用户既定特色，Chat/Plan 不受影响）

**决策链**：工具有没有 = 工作模式（Chat=0 / Plan=只读白名单 `WorkModeManager.PlanReadOnlyTools` / Build=白名单或经济精简）> 黑名单 > 全量；物理边界看边界轴；确认只看确认轴；省钱只看省钱轴。
**注意**：确认轴全局静态（多槽位共享）、行为轴槽位实例、边界/省钱轴全局 config；同名异义（Auto×4、`--permit tiny`→Chat 工作模式 vs 窗口 `--tiny`）见 docs/模式体系.md §5。

**快捷键一键一义**（v0.96.58 统一）：Ctrl+P=权限循环、Ctrl+E=经济循环（轴向层，主循环 `Program.Repl` 414/474/484 截走），**编辑器→`/edit`、输入建议条→输入 `/`·`!`·`#`·`@` 前缀自动弹出**；ChatScreen 不再绑 Ctrl+E/P/Q（防双重绑定「同一键两种含义」）。完整键表唯一事实源 = `UI/TUI/Controls/TuiKeybindHelp.cs` 的 `Groups`，底部行/文档据此维护。

**快捷键跨平台铁律**（Win/Linux/Mac 通用）：功能键用 `Ctrl+字母`（非信号/控制码）与 `Ctrl+方向/Home/End`；**禁用这些 Unix 坑键**——`Ctrl+C`(SIGINT 信号，系统键=退出)、`Ctrl+Z`(Unix 默认 SIGTSTP 挂起进程，已注册 `PosixSignal.SIGTSTP` 转「优雅暂停」且 `ctx.Cancel=true`)、`Ctrl+M`/`Ctrl+H`(Unix ≡回车/退格)、`Ctrl+S`/`Ctrl+Q`(终端流控 XOFF/XON)。终端拿不准的键一定配斜杠兜底（`/model` `/help` `/session`）；复制 `Ctrl+Insert`（Mac 无 Insert 键，用终端原生复制）。**v0.96.107 起另加三条**：①**不用三键组合** —— `Ctrl+Shift+字母` 在 Windows 上按不到（Windows Terminal 把 `Ctrl+Shift+P` 抢去开它自己的命令面板；即便不被抢，VT 字节流拿不到 Shift 修饰键（`CharSource.ToConsoleKeyInfo`）会退化成同名的纯 Ctrl 键）⇒ 命令面板 `Ctrl+U`、换 connect `Ctrl+N`、主题 `Ctrl+W` 全是纯 Ctrl 键；②**不占用系统的复制/粘贴/剪切键与输入框自己的编辑键** —— `Ctrl+X`（原=交换大小模型，让回系统剪切）、`Ctrl+Y`（原=搜索历史，让回输入框重做）、`Ctrl+K`（原=切模式别名，让回输入框删到行尾）；③**`Alt+字母/数字/符号` 可用**（xterm 系终端对 Alt 组合发 `ESC`+字符，`InputManager.TryParseEscapeSequence` 现在带 Alt 修饰键返回 —— 此前是「字符退回 pending + ESC 单独成键」，按 `Alt+T` 会变成「中断 Agent + 打出 t」，Alt 组合键全部不可用；歧义窗口 20ms 与 `WaitForChar` 同一处）。

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
- **写用户文件一律 `Global.WriteAllTextPreserveBom`，绝不用 `Encoding.UTF8`（v0.96.103，已致真实损坏）**：.NET 的 `Encoding.UTF8` 静态实例是**带 BOM** 的（`encoderShouldEmitUTF8Identifier: true`），`File.WriteAllText(path, text, Encoding.UTF8)` 会给文件凭空加 `EF BB BF`；两参重载与 `Global.Utf8NoBom`/`WriteAllTextPreserveBom` 才是无 BOM。**实测后果**：`NotebookEditTool` 用它写 `.ipynb`，而 Jupyter/nbformat 读 notebook 走 `open(path, encoding='utf-8')` + `json.load`、**对 BOM 不容忍** —— 直接抛 `JSONDecodeError: Unexpected UTF-8 BOM (decode using utf-8-sig)`，即「用 WayCoder 改一下 notebook，Jupyter 就打不开了」。**语义方向别搞反**：preserve-bom 是「原有 BOM 则保留、没有就不加」，`Encoding.UTF8` 是「无条件加」。编辑路径的标准是前者（`EditFileTool`/`MultiEditTool` 都走它）。**判 BOM 有无要看 `File.ReadAllText(path, encoding)` 的单参/双参区别**：双参走 `StreamReader(detectEncodingFromByteOrderMarks: true)` 会剥 BOM，所以「自读自写」看不出问题，**只有外部消费者（Jupyter / 严格 JSON 解析器 / git diff）才暴露** —— 新增任何「写用户文件」的路径，先问一句「谁会读它、容不容忍 BOM」。
- **自己产的状态/配置文件一律 `Global.WriteAllTextAtomic`（先写 `.tmp` 再 `File.Move` 覆盖）**：`File.WriteAllText` 是 `FileMode.Create`（先截断再写），崩溃 / 磁盘满 / 断电会留下**半截文件覆盖掉全量数据** —— 对 `~/.waycoder/*.json`（供应商、连接、槽位、主题）和智能体自己的记忆正文 + `MEMORY.md` 索引，半截就等于整份读不回来。已有 18 个调用点；**判据是「这份文件丢了用户是否要重配」**，不是文件大小。同理，同一份数据被多处读写时必须「锁 + 原子写」成对（见 `Tools/TodoStore.cs`）。
- **本仓库工作区是 CRLF（`git ls-files --eol` = `i/crlf w/crlf`）**：`.gitattributes` 只写了 `* text=auto` + `core.autocrlf=true`，尚未 renormalize，所以**任何批量改文件的脚本都必须保住 CRLF**——Python `open(p, encoding=...).read()` 会把 CRLF 读成 LF，再 `.write()` 就写出 LF，结果 `git diff --stat` 显示整文件全变（实测 TuiDialog.cs 的 180 行改动变成 1944 行），真实改动被行尾噪音淹没。**写完立刻核对 `git diff --stat`**，插入/删除行数远超预期就先 CRLF 化：`open(p,'wb').write(open(p,'rb').read().replace(b'\r\n',b'\n').replace(b'\n',b'\r\n'))`。同理，**批量替换要按子串换而不是整行换** —— 整行换会把 `cancelBtn.OnClick = _ => win.OnClosed?.Invoke();` 这类单行 lambda 的接收者一并吃掉（实测踩过）

- **TUI 输入字节的编码 = 控制台属性，不是猜出来的（v0.96.109）**：`WindowsCharSource` 按 UTF-8 解字节流，而 conhost 在 VT 输入模式下**用 `GetConsoleCP()` 编码非 ASCII 按键**（中文系统 936/GBK）⇒ 打中文得到 U+FFFD 乱码。`Program.Main` 的 `Console.OutputEncoding = UTF8` **只调 `SetConsoleOutputCP`，输入侧管不着** —— 症状正是「界面文字正常、自己打的字乱码」。修法：`WinConsoleMode.Enable` 同时 `SetConsoleCP(65001)`（`Disable` 还原；每次进界面重施，因为 `!chcp` 会改回去）。**关键教训：「先试 UTF-8、不合法再换页」是错的** —— ① GBK 前导字节落在 UTF-8 的 3 字节区间（0xE0-0xEF）时会白等一个永远不来的第三字节（最后一个字卡住不出，自测当场红了才发现）；② 大量 GBK 双字节**恰好是合法 UTF-8**（`一` = D2 BB → U+04BB），先试 UTF-8 会静静地把它解成别的字符。**编码是控制台的属性**：按该页自己的字节规则（DBCS = 前导 + 1 尾，`IsDBCSLeadByteEx`）切分再解；两条都兜不住时丢字节，绝不把 U+FFFD 塞进输入框。同族坑：v0.96.74 前 Windows 走 `Console.ReadKey`（拿输入记录的 UTF-16 字符、不经字节编码），所以那时打中文是好的 —— **改字节流时必须回头审「原先由 API 帮我们做的事」**。
- **折叠块的三条状态机规则（v0.96.108）**：①**收尾按层数配对，不是「见到 `«/»` 就关」** —— `«/»` 是**所有** «» 标记的统一结束符，而推理正文里可以嵌别的标记（LLM 超长时注入 `«orange3»… 思考内容过长…«/»`），照旧判会让其后的推理漏进正文段、还多出一个裸 `«/»`。判据 = 层数归零（`«dim»` 算第 1 层，块内嵌套标记各占一层）：Web `handleToken` 的 `thinkDepth`、GUI `AppendToken` 的 `_reasoningDepth`，四端同规则。②**点击回调必须闭包捕获自己那个块对象**，不能引用「当前块」那个可变变量 —— 块一结束变量被置空，老胶囊点开就是 `null` 抛错（若已有新块则会打开最新那块），用户看到的是「各段连在一起、老的点不开」；**工具组本来就捕获局部对象，所以那半边正常 —— 「同类控件一半好一半坏」正是指向这类共享变量闭包的线索**。③**「来了 token 就建气泡」也是错的** —— LLM 每段正文开头送一口换行（思考结束就是 `"«/»\n"`），「想完直接调工具」就在工具行前留一个空泡；判据收在 `UI/Shared/VisibleText.HasVisible`（空白 + 零宽空格/连接词、BOM、软连字符、词连接符、谚文填充符、变体选择符、控制字符；**`.NET char.IsWhiteSpace` 不认其中大半，必须显式列出**），Web 侧 `app.js` 有孪生 `isBlankText`（跨语言只能各写一遍，改动**两处同步**，自测两侧都钉）。诊断脚本 `scripts/_check_web_collapse.cjs` 用最小 DOM 桩驱动**真实** app.js 函数 —— 桩的 `innerHTML`/`textContent` 语义要与真实 DOM 一致（渲染过的元素要读得出渲染后的文字），否则「已渲染的泡」会被读成空、空泡检测全线误判。

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

> **打 APK 必须带签名参数，否则装不上已装的 App（v0.96.136 实测）**：`WayCoder.Maui/build-apk.sh` 是 **Mac 专用**，Windows 上直接照抄会失败；而**裸 `dotnet publish -f net10.0-android -c Release` 用的是默认 debug 密钥**，与仓库里 `waycoder.keystore` 签出来的**不是同一个证书** ⇒ `adb install -r` 报 `INSTALL_FAILED_UPDATE_INCOMPATIBLE: signatures do not match`。**此时千万别顺手 `adb uninstall`** —— 那会把手机上的 API Key、会话、workspace 一起删掉（数据全在 app 私有目录里，外部存储那套要用户先在设置里开）。正解是**照 `build-apk.sh` 的参数重打**（密钥/alias/密码都是 `waycoder`，见该脚本注释）。Windows 上可用的一条完整命令：
> ```
> dotnet publish WayCoder.Maui/WayCoder.Maui.csproj -f net10.0-android -c Release \
>   -p:AndroidPackageFormat=apk -p:AndroidKeyStore=true \
>   -p:AndroidSigningKeyStore="<仓库>/WayCoder.Maui/waycoder.keystore" \
>   -p:AndroidSigningKeyAlias=waycoder -p:AndroidSigningKeyPass=waycoder -p:AndroidSigningStorePass=waycoder
> ```
>
> **本机的 JDK / SDK 路径**（`JAVA_HOME` 与 `ANDROID_HOME` 都没设，必须显式给）：
> `JAVA_HOME="C:\Program Files\Android\openjdk\jdk-21.0.8"`、`ANDROID_HOME=D:\Android\Sdk`（`apksigner`/`aapt2` 在 `D:\Android\Sdk\build-tools\37.0.0\`，它们也要 `JAVA_HOME`）。**改过 APK 里任何资产就要先删旧 APK 再 publish**，否则不会重签。
>
> **示例（`Examples/`）进包的规则**：`vml_lib.zip` 里打 `Lib/` + `vmltool.config.xml` + **`Examples/` 的 1~2 层**（`Examples/README.md` 与 `Examples/<语言>/<文件>`）。手机端 `MauiBootstrap.EnsureExamples()` **保持这个层形解包**（v0.96.184 起：`examples/<语言>/<文件>`；此前是**平铺**的，十来种语言堆在一个目录里只能靠文件名猜），所以：① 递归整棵树会把上游 stb/stm32 上千个文件糊进 `examples/` 一个目录、还重名互覆；② **加新示例/游戏就放 `Examples/<语言>/` 下，别建子目录**（子目录不会进包）；③ 光把文件放进 `Examples/` 不会自动到手机上，要重跑 `scripts/make-vml-lib.sh` 再重打 APK；④ 命令带语言名：`vml run examples/c/tetris.c`；⑤ 重新解包时**只清"包里有的那些"**（顶层散文件 + 我们管理的语言子目录），用户自己在 `examples/` 下建的目录不碰 —— 标记文件 `.unpacked` 里存的是**版本号**，所以每次发版都会重解压一次（迁移旧布局就靠它，不用另写迁移代码）。真机验收：`adb shell ls /storage/emulated/0/waycoder/workspace/examples`（**工作区在外部存储，adb 直接可读**，比翻私有目录省事）。
>
> **真机跑 VML 程序不用写代码**：App 的「命令行」页敲 `vml run examples/c/tetris.c`（走 `VmlTool`，与 AI 调工具同一条流水线）。手机自带手柄（方向键 + SELECT/START + X/Y/A/B → Win32 虚拟键），**VML 程序的键盘分支直接认**。⚠ C 前端 + 汇编 + 链接 3.7 万条指令在手机上要**一分多钟**，别当成卡死。

## 添加新工具 (C# 版)

1. 在 `Tools/` 创建类，实现 `ITool` 接口
2. 在 `ToolRegistry.cs` 注册
3. 在 `PermissionManager.cs` 决定是否需要确认
4. 在 `Test/SelfTest*.cs` 添加测试
