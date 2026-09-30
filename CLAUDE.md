# CLAUDE.md

本文件为 Claude Code（claude.ai/code）在此仓库中工作时提供指导。

## 项目概述

**都来码（Dolaima）** 是一个中文版易用编程智能体，C# (.NET 10) 实现，AOT 编译为单文件 exe。

> **产品名 vs 内部代号（别搞反）**：**`都来码 / Dolaima` 是产品名** —— 凡**用户看得见**的地方一律用它
> （`Global.AppName` / `Global.AppNameCN`、`--version`、App 显示名、文档、商店清单、AI 自我介绍）。
> **`WayCoder` 只是内部开发代号**：仓库名、`WayCoder/` 目录、命名空间 `WayCoder.*`、`~/.waycoder/`、`.waycoder/`、
> `WAYCODER_*` 环境变量、`waycoder` 命令名**全都沿用它，一个字都别改** —— 改目录/环境变量会**丢用户的
> API Key、会话与记忆**，改命名空间是约 2400 处的无谓改动。历史：CoreCoder → WayCoder（道码，撞韩国
> Waycoder co., ltd.）→ **都来码 / Dolaima**（2026-09-28 定，v0.96.569 落地，范围清单见 CHANGELOG）。

> **本文件只放"长期有效"的东西**：架构、设计决策、可执行规则（「开发铁律」）。历次踩坑的**完整过程**（现象、复现、测量数据、当时的错误结论）归档在 [`docs/开发教训.md`](docs/开发教训.md)，按 `E001`… 编号；「开发铁律」里每条末尾的 `→E042` 就是指向它。新增规则**只往「开发铁律」加一行**，细节进归档。

## 常用命令

```bash
# C# 版
cd WayCoder
dotnet publish -c Release            # AOT 编译
dotnet run -- --test                 # 6803 自测（⚠ 别加 -c Release：WAYCODER_TEST 仅在 Debug 定义，
                                     #   Release 下 TestArg 整个不编译进去，CLI 按「有错即报」直接退出 1）
dotnet run -- -p "提示词"            # 一次性模式
dotnet run -- --watch                # Watch 模式 (监听 AI! 注释)
dotnet run -- --update               # 自动升级 (检查并自替换)
```

## 架构

```
WayCoder/
├── Program.cs         入口 + CLI + REPL (ANSI 全屏 TUI)
├── Agent/             智能体核心 (19 文件)
│   ├── Agent.cs           主循环 (Stop Hook + WorkReporter + 10 阶段流水线)
│   ├── AgentSlot.cs       多 Agent 工作区 (F1-F10 槽位切换 + 后台并行)
│   ├── LLM.cs             LLM 客户端 (流式 + 渐进超时重试 + 任务花费追踪)
│   ├── ContextManager.cs  Crush 风格上下文管理 (token 追踪 + 自动摘要 + 进度事件)
│   ├── SystemPrompt.cs    系统提示词 (15 个结构化区块)
│   ├── WorkModeManager.cs 工作模式 (Build/Plan/Chat)
│   └── FallbackLLM.cs / BackgroundTask.cs / WorkReporter.cs / TaskProgress.cs
├── Memory/            记忆与会话 (9 文件: StructuredMemory + MEMORY.md 索引 / MemoryRetrieval / SessionManager / ProjectKnowledge)
├── Config/            配置 (23 文件: Global.cs 全局 ~/.waycoder/config.json 权威源 / Config.Schema 110 项 / ConnectionConfig / ModelCatalog / ModelCli)
├── Infra/             基础设施 (87 文件: BashGuard / FileTracker / SandboxManager / HooksManager / UpdateChecker / DrawEngine 绘制 + 图片编解码 + Logging/)
├── Git/               Git 集成 (8 文件: GitRunner / GitCore / PackFile / RepoMapGenerator / WorktreeIsolation)
├── Watch/             Watch 模式 + ReviewMode
├── Sql/               手搓 SQL 引擎 (SqlEngine.cs)
├── Skills/            技能 + 权限 (SkillsManager / PermissionManager / AutoModeClassifier / builtin/)
├── Test/              测试/调试/演示代码（SelfTest 自测 46 partial 文件 + Benchmark/Keypad/TuiAudit/TuiDemo，共 6803 项）
├── Batch/             批量任务引擎 (BatchSpec 清单模型 + BatchRunner 多仓库并行/worktree 隔离)
├── Plugins/           编译期插件系统 (IPlugin SDK + PluginRegistry + [ModuleInitializer] 自动注册)
├── Tools/             49 个工具
│   ├── BashTool.cs    GitTool.cs    LspTool.cs
│   ├── ReadFileTool.cs FetchTool.cs MemoryTool.cs
│   ├── WriteFileTool.cs TodoTool.cs  LintTool.cs
│   ├── StructTodoTool.cs ExportTool.cs
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
    ├── TUI/            终端界面 (136 文件)
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
    └── GUI/            ⚠ **只剩 README** —— Avalonia GUI 已经是**独立工程 `WayCoder.Gui/`**
                        （35 个 .cs/.axaml：MainWindow / EditorWindow / ModelWindow /
                         SettingsWindow / App + `ChatInputBox`，见 `docs/开发教训.md` E018）
```

### 仓库一级目录（上面那棵树只画了 `WayCoder/` 主工程）

```
├── WayCoder/           主工程（TUI / Web / CLI / 智能体 / 工具 / Sql / Skills …）
├── WayCoder.Gui/       Avalonia GUI（桌面第二前端，独立 csproj）
├── WayCoder.Maui/      移动端（.NET MAUI，Android + iOS；`CoreStubs.cs` 是桩，见「开发铁律·三」）
├── third_party/vml/    vendored 的 VML（22 个前端 + 汇编器 + 运行时 + Lib + GenLib，**已与上游分家**）
├── scripts/            探针与工具（vml-*-probe / vmlcli / vml-asm-probe / make-vml-lib.sh …）
├── docs/               设计文档（模式体系 / VML调用约定统一 / VML宿主接口 / 插件系统 …）
├── packaging/          winget / brew / apt 打包 + 发布工作流
├── vscode-extension/   VS Code 扩展（走 `--json` 桥接）
└── Examples/           ⚠ 实际在 `third_party/vml/Examples/`（随 `vml_lib.zip` 进 APK）
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
- **内置自动升级**：`UpdateChecker` 版本检查**优先 GitHub Releases**、失败回退 Gitee（`WAYCODER_GITHUB_REPO`/`WAYCODER_GITEE_REPO` 覆盖）。⚠ **顺序别写反**（2026-09-28 定案，见 `UpdateChecker.cs:231` 的注释）：分工是「Gitee 存代码、GitHub 发行」，而 **Gitee 仓库是私有的**（匿名 403、`releases/latest` 返 `Not Found Project`）⇒ 它永远不会把资产发给终端用户；原先写「优先 Gitee」的实际效果是**每次检查先发一个注定失败的请求再回退**，白等一次往返，「国内快」这个理由也不成立；`/update` 检查、`/update now`/`--update` 自替换；纯逻辑（`CompareVersions`/`DetectCurrentRid`/`FindAssetName`）与网络/文件操作分离便于自测；Windows 落 `.new`+`upgrade.bat` 退出后替换重启、Unix 原子 `rename` 覆盖运行中二进制；`packaging/` 提供 winget manifest / brew formula / apt deb 打包，发行由本机 `scripts/release.sh` 出（**仓库已无 CI**，见「开发铁律·五」）
- **AOT 编译：JSON 手写序列化**，`JsonHelper.SerializeArgs` 替代 `JsonSerializer`
- **权限系统**：bash/write/edit/agent 默认行内确认（输入框下方的文字选择栏，见下条），`/perm yolo` 跳过
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
- **UI 控件库**：`UI/` 目录封装 TUI 控件（未来拆分 Tty 底层 + View 视图）；GUI **不是**这里的预留目录，而是独立工程 `WayCoder.Gui/`（Avalonia）
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
- **结构化记忆**：`.waycoder/memory/*.md` frontmatter 多文件 + MEMORY.md 索引，`memory` 工具与系统提示词注入均走结构化格式，首次使用自动从旧 memory.md 迁移
- **Diff 预览**：`/config DiffPreview true` 开启，write_file/edit_file 写前逐 hunk 确认（Y/N/A/Q），非交互模式（管道/重定向/测试）自动跳过
- **Bash 安全防护**：`BashGuard` 三层拦截（命令名 + 参数 + 安全白名单），87 禁止命令，70 安全只读命令免确认
- **`!` shell 直通**：TUI 输入 `!命令` 在操作系统 shell 执行（Win=cmd / 非 Win=bash），**输出加到聊天**（tool 纯文本，截取最后 500 行 `Program.TailLines`），裸 `!` 弹提示；`BashTool.ExecuteUserShellAsync` 跳过 Agent 黑名单（用户主动、红框警示）仅留绝对红线；输入框上下横线按前缀变色（`!`红 `/`青 `@`品红 `#`灰）——`ChatScreen.InputBorderColorFor` 纯逻辑
- **文件追踪 + Stale-Read 保护**：`FileTracker` SHA256 哈希记录 + 外部变更检测 + LRU 淘汰 + Agent 主循环注入变更警告（对标 Crush），防止 Agent 基于过期文件内容做决策
- **自动续写**：检测"口述代码"（content >300 字符 + 代码标记）→ 追问使其写文件；首轮只分析不动手 → 追问执行
- **自动摘要**：Crush 风格上下文预算检查 → 触发小模型压缩 → 注入继续提示 → 重置计数器
- **文档读取**：PDF 文本提取（PdfPig，AOT 兼容）+ Office 文档提取（DOCX/XLSX/PPTX，ZipArchive + XmlReader 零依赖）+ Markdown 结构化渲染 + CSV 表格解析 + HTML 标签剥离
- **SystemPrompt 对标 Crush**：`$"""` 原始字符串改用无 `$` 前缀+`.Replace()` 注入，避免代码示例中 `{` / `{{` 花括号导致 C# 插值解析错误。15 个结构化 XML 区块覆盖编辑/测试/错误恢复/任务完成完整指南
- **SHA256 循环检测**：每轮对（assistant 消息 + 工具结果）做哈希，8 轮窗口内相同哈希出现 3+ 次触发 3 级递进式反循环提示（换方法→重新评估→严重警告重置）
- **工具白名单/黑名单**：`WAYCODER_ALLOWED_TOOLS` / `WAYCODER_DISABLED_TOOLS` 环境变量控制 Agent 可用工具集合，构造函数中过滤，对主 Agent 和子 Agent 均生效
- **Tiny 模式**：`--tiny [窗口]`（如 `--tiny 8k`）精简提示词 + 小窗口；无参自动探测（Ollama `/api/show` 真实 `context_length` → 目录 → 4K 兜底）
- **省 Token 模式**：`--economy [on|auto|off|extreme]` / `WAYCODER_ECONOMY` 四态开关，保持正常窗口——关=完整；开=精简提示词（砍 RepoMap/Git/记忆/10 阶段流水线）+ 压缩阈值 50/70/90→35/55/75 + 工具输出裁剪 4000→2000 字符 + `max_tokens` 32768→8192；自动=保持完整提示词，压缩/裁剪阈值按任务轮数复杂度动态插值（简单省、复杂保质量），配合 `/config EconomyPriority quality|balanced|cost`（默认 quality，先保质量再省费用）；与 Tiny 的区别是保留正常窗口、面向云端大模型省钱
- **视觉（多模态）支持**：`view_image` 工具把本地图片加入 `LLM.PendingImages` 队列，Agent 主循环下一轮在 `FullMessages()` 末尾注入为多模态 user 消息（OpenAI 格式 `content` 数组，base64 data URL）；`LLM.ModelSupportsVision` 门控——仅 gpt-4o/gpt-5/claude/gemini 等 vision 模型才注入，DeepSeek 等文本模型自动跳过避免 400；配合 `screenshot` 抓屏实现「看图修 bug」
- **音频（多模态）支持**：`transcribe` 工具把本地音频文件上传到 Whisper 兼容端点（`/v1/audio/transcriptions`，multipart）转成文字，补齐 Codex CLI/Gemini CLI 的音频输入短板；配置 `WAYCODER_WHISPER_MODEL`/`WAYCODER_WHISPER_BASE_URL`/`WAYCODER_WHISPER_API_KEY`（空 key 回退主 `WAYCODER_API_KEY`），支持 OpenAI Whisper / Groq / faster-whisper 任意兼容服务
- **批量任务引擎**：`--batch <JSON|文件>` / `--batch-repo <仓库> --batch-task <任务>` 多仓库并行处理——每个任务 `git clone` 到 `.waycoder/batch/jobs/<名>_<随机>` 独立副本，子进程以 `-p` 一次性模式 + `-y` 放行执行（进程级隔离 cwd/状态），`SemaphoreSlim` 控并行（1–16 默认 4）、单任务超时可配（默认 1800s，超时杀整个进程树），子进程复用父进程已解析的 `--model`/`--base-url`/`--api-key`/`--max-budget-usd`（避免 clone 目录无 `.env` 丢 key）；跑完输出聚合 Markdown 报告落盘 `batch-report.md` + 退出码（对标 Cursor 批量修复 / Aider 多仓库脚本）
- **编译期插件系统**：`IPlugin`/`Plugin`/`PluginRegistry`——`WayCoder/Plugins/` 目录放 `.cs` 文件 + `[ModuleInitializer]` 自动注册（AOT 无反射、随单文件 exe 分发），插件可贡献工具（并入 `ToolRegistry.AllTools`）与斜杠命令（并入 `SlashCommandRegistry.RegisterAll`）；与 SKILL.md/Hooks/MCP 三种扩展机制互补，同名覆盖、null 防御、按名卸载，详见 docs/插件系统.md
- **JSON 输出模式（IDE 桥接）**：`--json -p "任务"`（或 `echo "任务" | waycoder --json`）一次性模式静默执行 Agent（onToken/onTool/onToolOutput 全 null、不流式、不写 ANSI），stdout 只输出一个 `JsonResult.Build` 结构化 JSON 对象——`schema`/`success`/`answer`/`error`/`model`/`usage{prompt,completion,total_tokens}`/`cost_usd`/`duration_ms`/`changed_files`，退出码 0 成功 1 失败；供 VS Code 扩展、CI 脚本、外部工具直接解析，纯函数构建器便于自测（对标 Claude Code `--output-format json`）

## 开发铁律

> 这一节是历次实战踩坑后沉淀的**可执行规则**。每条末尾的 `→E042` 指向 [`docs/开发教训.md`](docs/开发教训.md)
> 里对应事件的完整记录（现象、复现、测量数据、当时的错误结论）；需要细节时去那里查，别在这里堆。
> **动手改代码前扫一遍本组规则**，比事后返工便宜。

### 一、判断与验证（通用方法论）

- **先量再猜**：报一个总耗时/总现象时任何归因都是猜；在可疑链路上插**分段计时**，量完把插桩删干净。→E100
- **每个判据都要能「响」，并且被反证过**：把闸门改回错误实现，必须立刻红；不响的自测比没有更糟。→E071
- **冒烟（没崩/没挂/没超时）单列一档，不计入 PASS** —— 拿"跑通了"冒充"正确"给的是假信心。→E070
- **「助手已存在但被绕过」是本仓头号重复形态**：动手写新助手前先 `grep` 有没有现成的，顺手把绕过点收编。→E039
- **「同一规则两处实现、只修了一处」是第二号形态**：改判据/常量/文案时先问"还有谁判同一件事"。→E042
- **「必须手工同步的平行表」是第三号**：判据不是像不像，而是**漏改一处会不会有用户可见后果**。→E042
- **判据换了一半 = 没换**：换了"能不能开界面"的判据，就还得换"会不会读键""要不要确认"那几处。→E043
- **「上一轮量到的值」用之前先问它还算不算数**：布局期回调、定时器上报、跨方向的陈旧测量值都是中间态，会被当成真值。→E054
- **「没复现 ≠ 已修复」**：改完症状纹丝不动就继续往下怀疑，别停在第一层。→E062
- **退出码不是判据**：VM/编译器的错误可能打在 stdout 中间而退出码照样 0；`| grep` 之后的退出码是 `grep` 的。→E113
- **「源码看着对、跑起来不对」时，把程序实际读到的数据打出来**，比盯源码推理两轮强。→E071
- **要逐字节比对两条执行路径时，先把随机数钉死**（`#50 Random` 是时间播种的）。→E051
- **看到"像环境损坏"的报错先清 `obj/`**（残留中间产物）；别急着 `dotnet workload repair`。→E020
- **动手前核实前提**：「桌面是这么做的」不等于「移动端能这么做」—— 先看那一段是不是 `CoreStubs.cs` 里的桩。→E058
- **脚本改文件一律「按字节替换」**（`open(p,'rb')` → `replace(b'旧',b'新')` → `open(p,'wb')`），**绝不用文本模式读写**（Python 会把 CRLF 洗成 LF，整文件进 diff）；改完**必须 `grep` 回读确认**并核 `git diff --numstat` —— 某文件 `+N -N` 且 N = 总行数就是行尾被洗。→E116
- **同一处布局两套算法必然漂**：参数/口径只留一个真源（如 BASIC 实参区、几何计算的边界守卫）。→E079

### 二、VML 编译前端与运行时

- **新能力一律开新号**：给老 syscall 加参数 = 静默的未定义行为（宿主读到的可能是上一句的残留值）。→E056
- **「要不要动老程序的东西」由程序自己声明**（如四档 `WindowRotation`）；声明要**在开窗之前**生效。→E056
- **除汇编与 C 外，前端一律不许用固定地址**（DOS 显存/端口/中断不是本平台语义）；该是个变量就该是变量。→E076
- **库分层**：新版文字 `tty_*`、新版图像 `ui_*`；`conio`/`crt`/`graphics`(BGI) 只服务老程序，新程序别直接用。→E077
- **发射寄存器一律走 `CodeGeneratorBase.RegOf`** —— 寄存器**类**由助记符裁决（`MOVE`/`MOVED`/`MOVEL` 不一样）。→E088
- **寄存器一律写 `@`**（`@R0`），间接寻址写 `[@R0]` / `[@R14-4]` / `[标签]`；序列化器必须给寄存器加 `@`。→E080
- **函数名/变量名与寄存器形同名（`f1`/`d1`）由「同文件有没有定义该标签」裁定**，裁定推迟到解析完（含前向引用），运行时同规则。→E080
- **实参区一格 4 字节**，形参 i 在 `R12+8+4i`；**BYREF 或 8 字节形参（Double/Long）传地址**，其余传内联值。→E079
- **「有没有第二份实现」按「链接进哪个模块」查**，不能按"源码里搜同名" —— `#param lib()` / `.linked` / 前端「函数名→模块」映射是三条独立入口。→E068
- **名字里含另一个函数名的函数是雷区**（`_printf_itoa` 被 `EndsWith("_itoa")` 认成 `itoa`）；匹配判据要带模块边界。→E067
- **`GenLib` 是唯一生成器**（`build_libs.sh` 已删）；判断"哪个生成器该留"的判据 = 跑一遍看 `git status` 有没有 diff。→E069
- **改 `Lib/` 后必须重跑 `GenLib` 重生成**，判据是「重生成后逐字节相同」——别去改生成物本身。→E061
- **别靠记忆枚举改动，机械求差**（`git diff <基准>..HEAD -- <路径>`）；按**内容**算覆盖，不能按文件名。→E061
- **`third_party/vml/` 已与上游分家**：直接改就是最终状态，不存在"改完还要做成补丁"这一步（见 `FORK.md`）。→E060
- **死代码消除的规矩是「可以保留多，不能多删除」**：别名认全 + 间接跳转/中断向量非 0 就放弃 + 取地址走不动点。→E112
- **见到间接跳转就整体放弃太狠**：虚调用目标写在数据段 vtable 里 ⇒ 把「数据段引用的代码地址」也当可达性来源。→E114
- **上游那五个优化 pass（常量折叠/跳转链/死存储/复写传播/窥孔）恒关** —— 打开会让 22 门语言的输出全线出错。→E112
- **单元测试的判据是「副作用可不可观测」，不是「有没有返回值」**：void + 指针形参（缓冲区就是返回值）一样能断言。→E070
- **判据不经过 stdio**（本环境 `puts`/`printf` 会串行），且**同时查长度和逐字节内容** ——只查长度会漏掉"长度对、内容是 0"。→E070
- **前端「编得过、跑起来才错」的静默坑**：兜底分支吃掉整类节点（`-3` 是 `UnaryOp` 不是 `NumberLiteral`）、`(int[]){…}` 复合字面量不产出地址、全局 char 数组元素访问退化成 32 位。→E071
- **BASIC 的「裸调函数」当语句**：有返回值 + 当语句裸调那一种会被解析器静默丢弃（症状＝"只弹对话框、不弹绘图窗口"）。→E065
- **BASIC 的「调用括号」与「实参里的坐标元组」判据只能是**：匹配的 `)` 之后是不是语句边界。→E103
- **QBasic 的原生图形语句（`SCREEN`/`LINE`/`CIRCLE`/`PAINT`/`PUT`）写的是 DOS 固定地址**，手机上跑不了；画图只能走 `ui_*`。→E075
- **移植官方经典程序前先问许可**（随包分发）；玩法可参考、代码必须自己写。→E078
- **骨架全绿只证明"这条路径没坏"，不证明这门语言能用**：补例程要刻意把每种形态各用一遍（顶层变量/多参数/函数内调库/多形式 body）。→E064
- **vtable/`this` 这类"上个函数的残留"**：循环体里给实例字段赋值，先问"循环零次时它是什么"。→E101
- **`.vml` 文本格式改动会带着一批工具一起红**，要逐条分清"格式变了要跟着改"与"真回归"。→E080

### 三、移动端 / MAUI / 自绘界面

- **平台画布的 `FillColor` 清不掉渐变的 shader**（shader 优先级高于颜色）：纯色也走 `SetFillPaint`，不要出现裸的 `canvas.FillColor = …`。→E024
- **`ICanvas.DrawText` 每次调用都重新排版**：大文本量场景自己缓存 `StaticLayout`，走 `PlatformCanvas.Canvas` 原生画布。→E023
- **平台「排版报的宽度」≠「绘制时用的推进量」**（绘制把字形推进取整），只在整数号下重合 —— 定位一律自己算网格。→E022
- **字体资产别被打包成 Deflate**：`<AndroidStoreUncompressedFileExtensions>.ttf;.otf</…>`，否则每次解析字体解压 25MB。→E025
- **自绘层与平台输入框的叠放次序决定"点哪儿"归谁**：画布要压在 `Entry` 之上（MAUI Grid 后声明者在上层）。→E049
- **触摸命中看的是「有没有背景」**：`Transparent` 不参与命中（手势收不到）；子元素有背景会吃掉父级手势，要 `InputTransparent`。→E104
- **拖动/跟手交互**：`Started` 会被重复发（整段手势只认第一下）、基准取**实际值**不取请求值、上限按所在容器算。→E105
- **别用相对视图的坐标去驱动这个视图自己的尺寸**（反馈回路，比值恒 0.49）⇒ 走原生触摸的 `RawX/RawY`，并**除一次屏幕密度**。→E106
- **Android 15+ `adjustResize` 已失效**，要自己接 IME inset（压矮布局，不在滚动数学里减键盘高度）。→E050
- **MAUI 的 handler mapper 是全局静态字典** —— 加东西前先问"这个 mapper 会作用到哪些控件"（答案永远是"所有"），用 `StyleId` 收窄。→E021
- **平台回调的返回值要查语义**（`OnCreateActionMode` 返回 `true` 是"创建"不是"已消费"）；一次性收尾动作挂在"结束"事件上时，"被打断"也是一条结束路径。→E021
- **`CoreStubs.cs` 是桩类**：真实现每加一个被 `Tools/`、`Agent/` 用到的 public 成员都要同步补桩，漏了**只在 MAUI 上 CS0117**。→E058
- **自绘编辑器只有一把尺子**：宽度真源是网格模型（半角 1 列/全角 2 列），别让"测量值"去追"渲染落笔位置"。→E020
- **字体名一坑三吃**：家族名 / PostScript 名（iOS 要这个）/ 资产文件名三者不能互替，写错**静默回落成比例字体**。→E020
- **`AttributedText` 的 run 上绝不能写 `TextAttribute.FontName`**（Android 的 `TypefaceSpan` 不认 asset）。→E020
- **绘窗出图看「一帧画完了」而不是「内容变了」**：按 `ui_present` 的 `PresentVersion` 出图，并在 `Present()` **当刻**拍快照。→E052
- **`FitSize` 两个维度都要管**（只缩宽 ⇒ 高被切）；估算值要拿实际布局逐项对，"只处理一个维度"先问另一个维度。→E051
- **「屏幕变了」是两条消息 + 一个查询**：尺寸说"能画多大"、方向说"机器横竖"；**别拿 `w > h` 推方向**。→E055
- **屏幕上已有的东西不要在程序里再画一遍**（自绘手柄 = 多此一举）；长按连发定时器必须自带刹车。→E051
- **位置模型：判定、吸附、停下必须是同一个位置**，三件事分家就会出现"意图在变但位置不动"。→E071
- **移动端会话落盘只在轮末 `finally` 一处**：中途离页不写（否则丢回复），切换/新建会话要先等旧轮彻底结束。→E009
- **模式/权限等值读全局**（`WorkModeManager.CurrentMode`），别读可能为 null 的 agent 状态，否则"切不了"。→E006
- **不要强制 `SocketsHttpHandler`**：恢复系统默认 handler 才有代理/VPN/用户 CA（前提是网络不在主线程）。→E011
- **文件页只隐藏点开头的**目录**、不隐藏文件**（`.env`/`.gitignore` 是用户真会改的）；面向用户显示的路径统一走 `SandboxFsService.Abbreviate` 缩成 `~/…`，但**设置 → 存储**那行的绝对路径有意保留（那是用户在文件管理器里找代码的唯一线索）。→E118
- **浮层"可拖可关"与"不挡游戏"对立**，只能靠分层收窄：把柄/✕ 带背景（能点），正文块 `InputTransparent`。→E107

### 四、TUI / 桌面端

- **刷新必须由「变化」驱动，绝不由「节拍」驱动**：内容没变却整行重绘就是"空闲一直闪"。→E045
- **直写屏的控件必须由框架统一登记**（`TuiScreen.RegisterDirectWriters`），否则默认界面 `CanDirectWrite()` 恒 false、每帧整行重写。→E044
- **增量渲染里"别的控件重绘会把自己当父容器带进来"**：按区段刷新，只在显式标脏/全屏重绘/几何位移/遮挡解除四情形整行重写。→E045
- **擦了就必须重画、位移就必须全量**（`SetTreeDirty` 整棵子树，`parentDirty` 只向下传播一层）。→E045
- **VT 字节流会丢修饰键**：`Backspace` 是 DEL(0x7F)、Ctrl+字母是 0x01–0x1A、F1-F4 走 SS3 —— 字节→`ConsoleKeyInfo` 只许一个实现。→E046
- **TUI 输入字节的编码是控制台属性**：`WinConsoleMode.Enable` 要同时 `SetConsoleCP(65001)`，不能"先试 UTF-8 再换页"。→E048
- **「能开界面」与「会读键」是两条独立契约**（`CanUseFullScreen` / `HasKeyboard`），判据别再用 `Console.IsInputRedirected`。→E038
- **CLI 参数两条铁律**：① 有错即报错退出（未知选项/漏 `-p` 一律 stderr + 退出码 1）；② 启动参数**一律"本次启动覆盖"**，不写用户配置。→E037
- **「从 cwd 向上找」的边界必须锚在"一定在祖先链上"的目录**（锚在被覆写/重定向的变量上就永远等不到）。→E041
- **自动化自测必须硬离线**（`Global.OfflineMode`）：真要联网的用例走本地 mock 接缝。→E047
- **自测失败行必须直写真实 stdout**（`Console.SetOut` 捕获区里的 ❌ 会被丢掉）。→E040
- **行内问答只在 CLI/TUI**，四端分界是 `TuiManager.ActiveScreen is ChatScreen`；Web/GUI/MAUI 仍弹框（有意为之）。→E027
- **折叠块按"层数归零"配对**（`«/»` 是所有标记的统一结束符）；点击回调要**闭包捕获自己那块**，不能引用"当前块"。→E026
- **代码围栏容错**的坑在"段落累积"：终止条件硬编码 `StartsWith("```")` 会把"先说一句、再贴代码"吃进段落。→E031
- **粗体 + 颜色**要编码进颜色高位（`AnsiTty.BoldFlag`），否则内层颜色码覆盖外层样式。→E029
- **语法高亮有两套实现**（C# 的 `Syntax.Tokenize` 与 Web 的 `highlightCode`），改一边记得改另一边。→E028
- **权限确认只有 `code == 1`（全部允许）才写 `AutoAllowed`**；文件级 diff 走 `UI/Shared/UnifiedDiff` 四端共享。→E035
- **`AnsiMarkup` 的真彩码（`38;2;r;g;b`）必须排在 256 色之前判断**（真彩数值 > 255）；转义还原要排在"找闭合 `»`"之前。→E072
- **Markdown 预览要显式两套配色**（`Ink(isDark, 暗, 亮)`）；表格/代码块的横向溢出用横向 `ScrollView`，不能折行。→E074

### 五、构建、发布与流程

- **产品名与内部代号别搞反**：用户可见串一律 `都来码 / Dolaima`；`WayCoder` 只留在仓库/目录/命名空间/`~/.waycoder/`/`WAYCODER_*`/`waycoder` 命令名上，**别去改**（改了丢用户的 Key、会话与记忆）。
- **旧名清理的判据是「改掉之后那句话还成不成立」，不是"像不像品牌名"**：**库文件名/显示文本** ✅ 改（生成物要改生成器再重跑，不手改产物）；**路径 / 工程名 / 可执行名 / 命名空间 / 环境变量 / keystore** ❌ 仍然成立、改了文档就撒谎；**CHANGELOG / patches / 版本表** ❌ 改了历史失真。⇒ **旧名不可能清零**，先分成这两类再说"清完了没有"。→E115
- **`Global.Version` 是版本号唯一真源**，csproj 用 MSBuild 属性函数从 `Global.cs` 解析；**新增"同一个值写在两个地方"的字段前先问"它们靠什么保持同步"**。→E021
- **「改完代码重打包上传 App Store」里，升 `Global.Version` 不是可选项** —— 同一 `CFBundleVersion` 的第二次上传会被 App Store Connect 拒收（Android 的 `versionCode` 同源推导）。→E117
- **写用户文件一律 `Global.WriteAllTextPreserveBom`**（`Encoding.UTF8` 无条件加 BOM）、**自己产的状态文件一律 `Global.WriteAllTextAtomic`**。
- **打 APK 必须带签名参数**（见「Android 项目强制编码约束」末的完整命令）；**所有机器共用同一份 keystore**，判据是证书指纹不是文件名。
- **内置标准库解压根钉在 App 私有目录**（不随权限跳），判据用随包的 `vml_lib.hash`（不看时间戳）。→E016
- **`prog.ToString()` 的产物能独立跑，但必须"编完就存、不先跑"**（同一个 `VmlProgram` 跑过再序列化不等价）。→E017
- **免费版/全能版：口径与门分家、门各收一处**；优化器的门必须在 **getter**（执行与摘要显示同源）。→E102
- **「null 有第二种含义」的判据要分开写**：`.vml`/`.vmb` 没有语言 ⇒ 文件页那道门管不着，而编译入口那道门仍要拦。→E102
- **批量推进必须配「防漏翻闸门」**：不在台账里 ⇒ 红，在台账里但零命中 ⇒ **也红**（台账腐烂比不全更坏）；扫不出东西必须报错不能算通过。→E090
- **把调用机械改写成"基类助手"之前，先查这个类有没有 `new` 遮蔽它**（遮蔽成员的绑定目标会悄悄分叉）。→E091
- **批量改注释用三层判据**：硬判据（剥注释后哈希不变，一票否决）+ 软判据（进度条，不是证明）+ 真编译/产物对比。→E092
- **UI 驱动的验收三个坑**：`keyevent 4` 是回桌面不是返回、软键盘的回车键是 ✓、键盘弹起后截图坐标全部失效。→E058
- **跨端验证手段**：Windows 上跑免打包 WinUI 的 `WayCoder.Maui`（`dotnet build -f net10.0-windows…`）用 UI Automation 按**名字**驱动 —— 它同时是"安卓专有 API 漏守卫"的编译闸。→E059
- **两台机器/多个并行任务按文件划片**（不按语言、不按批次），任务书里写清格式约定与验收命令。→E098
- **一个属性写错能让整套机制从没生效过**（`NeutralLanguage` 声明成 `zh-CN`：文件都在、就是读不出来）；**同批统一两套语言源**，去掉"首访把语言冻死"的静态缓存。→E089
- **使用说明从源码生成、不手写**；`HelpCatalog.FindTopic` 必须递归进 `Children`（拿不到只退化成兜底标题，页面照开）。→E073
- **`ErrorLog` 的命名空间是 `WayCoder`**（不是 `WayCoder.Infra`），写全限定名会 CS0234。→E110
- **列对齐这类断言写的时候人一定会数错空格** —— 看着对、差一格的东西，不跑一次判不出来。→E111
- **VML 超时是「连续执行」的不是墙钟**（等消息/人给输入才续期，定时器与 resize **不续期**；对话框期间要停表）；**程序结束后宿主必须收尾关窗**。→E107
- **「卡住出不来」要三个出口**：编译看门狗（带超时地等，不是杀线程）、静态取消源、退出时的优雅收场 + 兜底取消。→E014
- **加接口之前先问一遍"这个能力是不是已经在了，只是没接出来"**（先查 VM 内置 syscall，别急着加平行接口）。→E013
- **跨页交接交"作业对象"不交命令文本**（命令按空白切分，路径带空格就断成两截）；没切过去要把信箱清掉。→E012
- **GitHub 上不放 CI / 自动化构建**（2026-09-30 用户定，`.github/workflows/` 已删）：远端分工是
  **源码只放 Gitee、GitHub 只放文档与发行程序** ⇒ GitHub master 长期落后是**有意为之**，
  别因为"落后 N 条"去同步它。发版走**本机** `scripts/release.sh`（它自己编 6 平台包 + 算 sha +
  生成 winget/brew 清单 + 打印上传命令）。原先那套「CI 出包再用本地 dist 覆盖」是自相矛盾的
  —— CI 在 runner 上重新 AOT，产物哈希与本地清单必然不同、还缺 `win-arm64` ⇒
  **覆盖那一步才是真正生效的一步**，等于 CI 产物从没被用过。
- **仓库「只放文档与发行」的例外要按文件挑着推**：要往 GitHub 加东西时（如 App Store 的两份文档），
  在 GitHub master 之上建一个**只含那几个文件**的提交再推（`GIT_INDEX_FILE` 临时索引 +
  `read-tree` + `add` + `write-tree` + `commit-tree -p` + `push github <sha>:master`），
  **不要把整棵树推上去**；推前先 `git grep -n -I -E 'sk-[A-Za-z0-9]{20,}'` 查密钥（目标是公开仓库）。

## 模式体系（三分钟版，竞品对标）

WayCoder 的模式参考 Claude Code / OpenAI Codex / Crush / Aider 划分为**四个正交轴**（完整版见 [docs/模式体系.md](docs/模式体系.md)）：

- **确认轴**（权限模式 `PermissionManager.Mode`，Ctrl+P · `/permit`）：管「何时打断确认」——Ask(必问)/Auto(改必问≈Ask)/SmartAuto(危必问)/Yolo(不问)
- **边界轴**（沙箱 `SandboxManager`，`/perm`）：管「能碰什么」（可写范围/网络）——对齐 Codex `sandbox_mode`；与确认轴**已解耦**（`SetLevel` 只改边界，`yolo`/`god` 不再映射沙箱）
- **行为轴**（工作模式 `WorkMode`，Shift+Tab · `/mode`）：管「工具有没有 + 干什么活」——Build 全量（受经济模式管）/ Plan 只读白名单+精简提示词（有审批门）/ **Chat 纯聊天（0 工具 0 提示词）**；**槽位实例级**（`Agent.cs:91`）
- **省钱轴**（经济模式 `EconomyMode`，Ctrl+E · `/config economy`）：管「花多少 token」——提示词档位 + 压缩阈值 + 输出上限（Build 档删工具=用户既定特色，Chat/Plan 不受影响）

**决策链**：工具有没有 = 工作模式（Chat=0 / Plan=只读白名单 `WorkModeManager.PlanReadOnlyTools` / Build=白名单或经济精简）> 黑名单 > 全量；物理边界看边界轴；确认只看确认轴；省钱只看省钱轴。
**注意**：确认轴全局静态（多槽位共享）、行为轴槽位实例、边界/省钱轴全局 config；同名异义（Auto×4、`--permit tiny`→Chat 工作模式 vs 窗口 `--tiny`）见 docs/模式体系.md §5。

**快捷键一键一义**（v0.96.58 统一）：Ctrl+P=权限循环、Ctrl+E=经济循环（轴向层，主循环 `Program.Repl.cs` 590/600 截走），**编辑器→`/edit`、输入建议条→输入 `/`·`!`·`#`·`@` 前缀自动弹出**；ChatScreen 不再绑 Ctrl+E/P/Q（防双重绑定「同一键两种含义」）。完整键表唯一事实源 = `UI/TUI/Controls/TuiKeybindHelp.cs` 的 `Groups`，底部行/文档据此维护。

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
> **⚠ 两台机器两张证书 —— 必须共用同一份 keystore（2026-09-20 实测踩到）**：`waycoder.keystore` 是私钥、被 `.gitignore` 排除（**是对的**），但代价是**每台机器都会各自生成一份同名文件**。于是同一份代码在两台机器上打出来的包**签名不同**，A 机器打的包装不上 B 机器装过的手机，报：
> ```
> INSTALL_FAILED_UPDATE_INCOMPATIBLE: signatures do not match newer version
> ```
> 而**这个报错看不出是"两台机器两张证书"**，唯一"官方"的补救是 `adb uninstall` —— 那会删掉用户手机上的 API Key 与会话（私有目录里；工作区在外部存储、不受影响）。实测那次：Mac 的 keystore 生成于 9-13（`SHA256withRSA`，`14:AB:38:…`）、Windows 的生成于 9-14（`SHA384withRSA`，`75:98:F1:…`），手机装的是 Windows 那份 ⇒ Mac 这边怎么打都装不上去。**判据是证书指纹，不是文件名**（两份都叫 `waycoder.keystore`、DN 也一模一样）。
>
> **规矩**：所有机器共用**同一份** keystore，离线传（U 盘 / 密码管理器 / 安全通道），**绝不走 git**。以**手机上正在生效的那一份**为准（换钥匙否则要再卸一次）。指纹钉在 `WayCoder.Maui/keystore.sha256` —— 指纹是**公开信息**（印在 APK 签名里），可以进仓库；私钥不行，这个分工就是那个文件的意义。`build-apk.sh` 构建前会校验指纹，**对不上直接失败**（好过打出一个装不上去的包）。
>
> ⚠ **Windows 上走的是裸 `dotnet publish`（没有这个校验）** —— 在那台机器上打之前，先手动对一次：
> `keytool -list -v -keystore WayCoder.Maui/waycoder.keystore -storepass waycoder | grep SHA256`

> **示例（`Examples/`）进包的规则**：`vml_lib.zip` 里打 `Lib/` + `vmltool.config.xml` + **`Examples/` 的 1~3 层**（`Examples/README.md`、`Examples/<语言>/<文件>`、`Examples/<语言>/<子目录>/<文件>`）。手机端 `MauiBootstrap.EnsureExamples()` **按 zip 条目保留目录结构解包**（v0.96.184 起：`examples/<语言>/<文件>`；此前是**平铺**的，十来种语言堆在一个目录里只能靠文件名猜），所以：① 递归整棵树会把上游上千个文件糊进包里（那批已按 `PRUNED.txt` 清掉，但仍不该放开递归）；② 加新示例/游戏放 `Examples/<语言>/` 下，**要分类可以再建一层子目录**（如 `Examples/c/old/*.c`）—— ⚠ 这一层是 2026-09-22 才加的：此前只收 1~2 层，于是「按类型分目录」的示例**静默不进包**，而桌面直接读仓库、完全看不出来；③ 光把文件放进 `Examples/` 不会自动到手机上，要重跑 `scripts/make-vml-lib.sh` 再重打 APK —— ⚠ **而且必须同时升 `Global.Version`**（v0.96.212 实测踩到）：`EnsureExamples()` 的闸门是 `File.ReadAllText(marker).Trim() == Global.Version`，**只判版本、不看内容指纹**（与 `MauiVml.EnsureLibExtracted` 用 `vml_lib.hash` 判内容**不是一回事**）⇒ **版本没变就不会重新解压**，包里的新示例永远到不了手机，而 `adb` 看 `examples/` 目录"确实没有新文件"，很容易误判成"打包漏了"。改示例 = 改版本号，这两件事要一起做；④ 命令带语言名：`vml run examples/c/tetris.c`；⑤ 重新解包时**只清"包里有的那些"**（顶层散文件 + 我们管理的语言子目录），用户自己在 `examples/` 下建的目录不碰 —— 标记文件 `.unpacked` 里存的是**版本号**，所以每次发版都会重解压一次（迁移旧布局就靠它，不用另写迁移代码）。真机验收：`adb shell ls /storage/emulated/0/waycoder/workspace/examples`（**工作区在外部存储，adb 直接可读**，比翻私有目录省事）。
>
> **真机跑 VML 程序不用写代码**：App 的「命令行」页敲 `vml run examples/c/tetris.c`（走 `VmlTool`，与 AI 调工具同一条流水线）。手机自带手柄（方向键 + SELECT/START + X/Y/A/B → Win32 虚拟键），**VML 程序的键盘分支直接认**。⚠ C 前端 + 汇编 + 链接 3.7 万条指令在手机上要**一分多钟**，别当成卡死。

## 添加新工具 (C# 版)

1. 在 `Tools/` 创建类，实现 `ITool` 接口
2. 在 `ToolRegistry.cs` 注册
3. 在 `PermissionManager.cs` 决定是否需要确认
4. 在 `Test/SelfTest*.cs` 添加测试
