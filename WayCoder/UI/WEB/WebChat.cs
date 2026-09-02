using System.Collections.Concurrent;
using System.Text;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Edit;

namespace WayCoder.UI.Web;

/// <summary>
/// 浏览器聊天桥接层：把 <see cref="Agent.ChatAsync"/> 的流式回调（onToken/onTool/onToolOutput）
/// 转为 SSE 事件广播给浏览器，接收浏览器 POST 的输入入队，支持中断。
/// 对标 DeepSeek Harness Web UI：多槽位（F1-F10）、换模型、输 key、设置、黑白主题。
/// </summary>
public sealed partial class WebChatServer : UxHelper.IWebInteraction
{
    private const int SlotCount = 10;

    /// <summary>SSE 长连接上限（防连接/线程耗尽）。</summary>
    public const int MaxSseClients = 16;

    /// <summary>待处理输入队列上限（防内存耗尽，统一引用 Global）。</summary>
    public const int MaxPendingInput = Global.MaxPendingInput;

    private readonly HttpServer _server;
    private readonly WebSlot[] _slots = new WebSlot[SlotCount];
    private readonly object _lock = new();
    private readonly List<SseClient> _clients = new();
    /// <summary>客户端 → 槽位绑定（每个浏览器页面绑定一个槽位，开始/停止按页面作用，互不干扰）。</summary>
    private readonly Dictionary<string, int> _clientSlot = new(StringComparer.Ordinal);

    /// <summary>当前执行槽位（AsyncLocal：交互桥 ask 按发起提问的槽位路由给对应页面）。</summary>
    private readonly AsyncLocal<int> _currentSlot = new();

    /// <summary>Web 交互桥：requestId → 等待中的提问应答源。</summary>
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingAnswers = new();
    private int _answerId;

    /// <summary>槽位运行时状态（Agent 懒建 + 忙碌标记 + 取消令牌）。</summary>
    private sealed class WebSlot
    {
        // volatile：EnsureSlot 的 double-checked locking 在 lock 外首检读该字段，
        // ARM 弱内存模型下需 volatile 保证「Agent 构造完成」先于「引用发布」的可见性。
        public volatile Agent? Agent;
        public volatile bool IsBusy;
        public CancellationTokenSource? Cts;
        /// <summary>运行中的后台 Agent 任务（换模型/存 key 时 await 收尾，避免与退场 ChatAsync 竞态）。</summary>
        public Task? RunningTask;
        /// <summary>串行化 StartSlotTask 的 check-then-act 与 Interrupt 的 Cts 摘除，防同槽位并发请求双启动 / 中断被丢。</summary>
        public readonly object StartLock = new();
        /// <summary>串行化 EnsureSlot 的懒建 check-then-act，防并发首建产生双 Agent 相互覆盖。</summary>
        public readonly object AgentLock = new();
        /// <summary>待处理指令队列：Agent 忙碌时输入入队，当前批次完成后自动取下一个执行（输入排队机制）。</summary>
        public readonly System.Collections.Concurrent.ConcurrentQueue<string> PendingInputs = new();
        /// <summary>换模型暂停期标志（StartLock 保护）：置位时 TryStartNextPending 跳过启动新任务，
        /// 消除「旧批次 finally 启动排队指令的新 ChatAsync」与「Reconfigure 换模型」的并发竞态。</summary>
        public bool Quiescing;
        /// <summary>最近一次广播的状态 key（去重：仅状态变化才发 status SSE，防每 token 刷屏）。</summary>
        public string LastStatus = "";
        /// <summary>本轮任务完成时间戳（完成瞬态显示用，TickCount64）。</summary>
        public long CompleteAtTicks;
    }

    /// <summary>SSE 客户端（写失败 = 断开）。</summary>
    private sealed class SseClient
    {
        public string ClientId = "";
        public int SlotIndex = -1;
        public StreamWriter Writer = null!;
        public readonly TaskCompletionSource Closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <summary>异步写串行门：广播（fire-and-forget 异步写）与连接建立初始回放、断开释放并发，
        /// SemaphoreSlim FIFO 保证同一客户端写序不乱且不阻塞调用线程（原 object 锁只串行了同步 Write）。</summary>
        public readonly SemaphoreSlim WriteGate = new(1, 1);
    }

    public WebChatServer(Agent agent, int port)
    {
        for (int i = 0; i < SlotCount; i++) _slots[i] = new WebSlot();
        _slots[0].Agent = agent;
        _server = new HttpServer(port);
    }

    /// <summary>实际绑定端口（传入 0 时由系统分配）。</summary>
    public int Port => _server.ActualPort;

    public void Start()
    {
        // 启动时清理历史上传临时文件（上次运行残留未清理的图片/音频）
        try { if (Directory.Exists(UploadDir)) Directory.Delete(UploadDir, true); } catch { }
        _server.OnRequest = HandleRequest;
        _server.OnSse = HandleSseAsync;
        _server.Start();
        UxHelper.WebInteraction = this;
        // 订阅上下文压缩事件：经 AsyncLocal 当前槽位把「压缩中」进度广播给对应页面
        ContextManager.CompressProgress += OnCompressProgress;
        ContextManager.CompressFinished += OnCompressFinished;
        // 订阅权限等待事件：动态状态栏显示「等待确认中」
        PermissionManager.PermissionPromptStarted += OnPermissionStarted;
        PermissionManager.PermissionPromptResolved += OnPermissionResolved;
    }

    public void Stop()
    {
        // 中断所有槽位的后台 Agent 并释放取消令牌（复用 Interrupt，保持 Cts 摘除与 StartSlotTask 互斥）
        for (int i = 0; i < SlotCount; i++)
            Interrupt(i);
        ContextManager.CompressProgress -= OnCompressProgress;
        ContextManager.CompressFinished -= OnCompressFinished;
        PermissionManager.PermissionPromptStarted -= OnPermissionStarted;
        PermissionManager.PermissionPromptResolved -= OnPermissionResolved;
        UxHelper.WebInteraction = null;
        _server.Stop();
    }

    /// <summary>任务完成瞬态窗口（毫秒）：done 后短暂显示「任务完成 ✓」再回落。</summary>
    private const long CompleteWindowMs = 2500;

    /// <summary>压缩进度（压缩线程内同步触发）：按当前槽位广播 compress 事件给对应页面 + 状态栏。</summary>
    private void OnCompressProgress(int layer, string label, double percent)
    {
        var slotIdx = _currentSlot.Value;
        BroadcastTo(slotIdx, "compress", SerializeCompress(layer, label, percent, done: false));
        BroadcastStatus(slotIdx); // 压缩中 → 动态状态栏
    }

    /// <summary>压缩结束（无论是否实际压缩）：广播 done 事件，前端据此隐藏指示条。</summary>
    private void OnCompressFinished()
    {
        var slotIdx = _currentSlot.Value;
        BroadcastTo(slotIdx, "compress", SerializeCompress(0, "", 0, done: true));
        BroadcastStatus(slotIdx); // 压缩结束回落
    }

    private void OnPermissionStarted(string toolName) => BroadcastStatus(_currentSlot.Value);   // 等待确认中
    private void OnPermissionResolved(string toolName) => BroadcastStatus(_currentSlot.Value);  // 回落

    /// <summary>广播 Agent 状态到指定槽位页面（去重：仅状态变化才发 status SSE，防每 token 刷屏）。
    /// explicitToolName 用于 onTool 回调（工具名已确定，不等 CurrentToolName 轮询）。</summary>
    private void BroadcastStatus(int slotIdx, string? explicitToolName = null)
    {
        if (slotIdx < 0 || slotIdx >= _slots.Length) return;
        var slot = _slots[slotIdx];
        var a = slot.Agent;
        var tool = explicitToolName ?? a?.CurrentToolName;
        var view = AgentStatusResolver.Resolve(new AgentStatusInput(
            Busy: slot.IsBusy,
            ToolName: tool,
            Compressing: ContextManager.IsCompressing,
            WaitingPermission: PermissionManager.PendingPermissionTool != null,
            WaitingUser: tool == "ask_user_question",
            WaitingSubagent: tool == "agent",
            Mode: a?.WorkMode ?? WorkMode.Build,
            RecentComplete: slot.CompleteAtTicks != 0 &&
                            Environment.TickCount64 - slot.CompleteAtTicks < CompleteWindowMs));
        var key = AgentStatusResolver.StatusKey(view.Status);
        if (key != slot.LastStatus)
        {
            slot.LastStatus = key;
            BroadcastTo(slotIdx, "status", SerializeStatus(view));
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  路由
    // ═══════════════════════════════════════════════════════════

    private async Task<HttpResponse?> HandleRequest(HttpRequest req)
    {
        // CSRF 防护：状态变更请求（非 GET）必须来自本服务来源。
        // 浏览器跨源 fetch 必带 Origin（攻击者域名），curl/SSE/同源导航不带 Origin 放行；
        // Sec-Fetch-Site: cross-site 兜底拦截漏带 Origin 的跨站请求（纵深防御）。
        if (req.Method != "GET" && (!IsTrustedOrigin(req.Header("Origin"), Port) || IsCrossSite(req.Header("Sec-Fetch-Site"))))
            return new HttpResponse { Status = 403, Reason = "Forbidden", Body = Encoding.UTF8.GetBytes("403 Forbidden") };

        // 客户端身份 + 槽位绑定（每个页面一个 client，绑定一个槽位，开始/停止按此槽位作用）
        var clientId = ParseClientQuery(req.Query);
        var slot = ResolveSlot(clientId);
        _currentSlot.Value = slot;

        // 页面
        if (req.Method == "GET" && req.Path == "/")
            return HttpResponse.Html(WebAssets.Html.Replace("__VERSION__", Global.Version));

        // 聊天
        if (req.Method == "POST" && req.Path == "/chat")
        {
            if (!string.IsNullOrWhiteSpace(req.Body)) StartSlotTask(slot, req.Body);
            return HttpResponse.Text("ok");
        }
        if (req.Method == "POST" && req.Path == "/interrupt")
        {
            Interrupt(slot);
            return HttpResponse.Text("ok");
        }
        if (req.Method == "GET" && req.Path == "/history")
            return HttpResponse.JsonBody(HistoryOf(slot));

        // 模型 / 状态
        if (req.Method == "GET" && req.Path == "/models")
            return HttpResponse.JsonBody(SerializeModels());
        if (req.Method == "POST" && req.Path == "/models/scan")
        {
            var probes = ModelCli.TestList();
            return HttpResponse.JsonBody(JNode.Object()
                .Set("ok", true)
                .Set("results", Json.Parse(SerializeScan(probes)) ?? JNode.Array())
                .ToJson());
        }

        // ── 供应商（provider）管理：列表 / 添加 / 改名 / 改地址 / 删除 / 设Key / 清Key / 测试连通 ──
        if (req.Method == "GET" && req.Path == "/provider")
            return HttpResponse.JsonBody(SerializeProviders());
        if (req.Method == "POST" && req.Path == "/provider/add")
        {
            var body = Json.Parse(req.Body);
            var id = (body?["id"]?.AsString() ?? "").Trim();
            var name = (body?["name"]?.AsString() ?? "").Trim();
            var url = (body?["baseUrl"]?.AsString() ?? "").Trim();
            if (string.IsNullOrEmpty(id)) return HttpResponse.JsonBody(Err("缺少供应商 ID"));
            if (!ModelCatalog.RegisterProvider(id, string.IsNullOrEmpty(name) ? id : name, url))
            {
                var owner = ModelCatalog.FindProviderByBaseUrl(url);
                return HttpResponse.JsonBody(Err($"地址已被服务商「{owner}」占用（同地址 = 同供应商，不允许重复）"));
            }
            BroadcastStateForAll();
            return HttpResponse.JsonBody(Ok());
        }
        if (req.Method == "POST" && req.Path == "/provider/rename")
        {
            var body = Json.Parse(req.Body);
            var pid = (body?["providerId"]?.AsString() ?? "").Trim();
            var name = (body?["name"]?.AsString() ?? "").Trim();
            if (string.IsNullOrEmpty(pid) || string.IsNullOrEmpty(name)) return HttpResponse.JsonBody(Err("缺少 providerId 或 name"));
            ModelCatalog.RenameProvider(pid, name);
            BroadcastStateForAll();
            return HttpResponse.JsonBody(Ok());
        }
        if (req.Method == "POST" && req.Path == "/provider/url")
        {
            var body = Json.Parse(req.Body);
            var pid = (body?["providerId"]?.AsString() ?? "").Trim();
            var url = (body?["baseUrl"]?.AsString() ?? "").Trim();
            if (string.IsNullOrEmpty(pid)) return HttpResponse.JsonBody(Err("缺少 providerId"));
            if (!ModelCatalog.UpdateProviderUrl(pid, url))
            {
                var owner = ModelCatalog.FindProviderByBaseUrl(url);
                return HttpResponse.JsonBody(Err($"新地址已被服务商「{owner}」占用（同地址 = 同供应商，不允许重复）"));
            }
            BroadcastStateForAll();
            return HttpResponse.JsonBody(Ok());
        }
        if (req.Method == "POST" && req.Path == "/provider/delete")
        {
            var body = Json.Parse(req.Body);
            var pid = (body?["providerId"]?.AsString() ?? "").Trim();
            if (string.IsNullOrEmpty(pid)) return HttpResponse.JsonBody(Err("缺少 providerId"));
            ModelCatalog.RemoveProvider(pid);
            ApiKeyStore.Remove(pid); // 删除供应商连带清 key
            BroadcastStateForAll();
            return HttpResponse.JsonBody(Ok());
        }
        if (req.Method == "POST" && req.Path == "/provider/key")
        {
            var body = Json.Parse(req.Body);
            var pid = (body?["providerId"]?.AsString() ?? "").Trim();
            var apiKey = (body?["apiKey"]?.AsString() ?? "").Trim();
            if (string.IsNullOrEmpty(pid)) return HttpResponse.JsonBody(Err("缺少 providerId"));
            if (!ApiKeyStore.IsValidApiKey(apiKey)) return HttpResponse.JsonBody(Err("Key 含非法字符或环境变量引用（只允许英文字母数字 + - _ . ,）"));
            SetProviderKey(pid, apiKey);
            BroadcastStateForAll();
            return HttpResponse.JsonBody(Ok());
        }
        if (req.Method == "POST" && req.Path == "/provider/key/remove")
        {
            var body = Json.Parse(req.Body);
            var pid = (body?["providerId"]?.AsString() ?? "").Trim();
            if (string.IsNullOrEmpty(pid)) return HttpResponse.JsonBody(Err("缺少 providerId"));
            ApiKeyStore.Remove(pid);
            BroadcastStateForAll();
            return HttpResponse.JsonBody(Ok());
        }
        if (req.Method == "POST" && req.Path == "/provider/test")
        {
            var probes = ModelCli.TestList();
            return HttpResponse.JsonBody(JNode.Object()
                .Set("ok", true)
                .Set("results", Json.Parse(SerializeScan(probes)) ?? JNode.Array())
                .ToJson());
        }

        // 模型归并：别名 providerId → 同 base_url 的已注册供应商（dryRun 只预览不落盘）
        if (req.Method == "POST" && req.Path == "/provider/reconcile")
        {
            var body = Json.Parse(req.Body);
            var dryRun = body?["dryRun"]?.Kind == JKind.Bool && body!["dryRun"]!.AsBool();
            var rep = ModelCatalog.ReconcileModels(dryRun);
            if (!dryRun && (rep.Moved > 0 || rep.DeletedFiles.Count > 0)) BroadcastStateForAll();
            return HttpResponse.JsonBody(JNode.Object()
                .Set("ok", true)
                .Set("dryRun", rep.DryRun)
                .Set("moved", rep.Moved)
                .Set("duplicateSkip", rep.DuplicateSkip)
                .Set("alreadyCanonical", rep.AlreadyCanonical)
                .Set("noUrl", rep.NoUrl)
                .Set("unresolved", rep.Unresolved)
                .Set("backups", StringArray(rep.Backups.ToList()))
                .Set("deletedFiles", StringArray(rep.DeletedFiles.ToList()))
                .Set("failedFiles", StringArray(rep.FailedFiles.ToList()))
                .Set("changes", SerializeReconcileChanges(rep.Changes))
                .ToJson());
        }

        if (req.Method == "POST" && req.Path == "/models/import")
            return ImportExternalModels(req.Body);
        if (req.Method == "POST" && req.Path == "/models/import-opencode")
            return await ImportOpenCodeOnline(req.Body);
        if (req.Method == "POST" && req.Path == "/models/clear")
        {
            var cleared = ModelCatalog.ClearAll();
            return HttpResponse.JsonBody(JNode.Object()
                .Set("ok", true)
                .Set("cleared", cleared)
                .Set("modelReport", $"🗑 已清空全部模型（删除 {cleared} 个自定义模型文件，内置目录已隐藏）")
                .ToJson());
        }
        // 编辑单个模型（两层架构：服务商/地址/key 在 provider 层，模型/上下文/价格在模型层）
        if (req.Method == "POST" && req.Path == "/models/edit")
        {
            var body = Json.Parse(req.Body);
            var id = body?["id"]?.AsString() ?? "";
            var providerId = body?["providerId"]?.AsString() ?? "";
            var baseUrl = body?["baseUrl"]?.AsString() ?? "";
            var apiKey = body?["apiKey"]?.AsString() ?? "";
            var context = body?["context"]?.AsNumber() ?? 0;
            var price = body?["price"]?.AsNumber() ?? 0;
            var priceOff = body?["priceOff"]?.AsNumber() ?? 0;
            if (string.IsNullOrWhiteSpace(id))
                return HttpResponse.JsonBody(Err("缺少模型 id"));
            if (string.IsNullOrWhiteSpace(providerId)) providerId = "custom";
            // key 按服务商存（一个服务商一个 key）
            if (!string.IsNullOrWhiteSpace(apiKey))
                ApiKeyStore.Set(providerId, apiKey);
            ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
                id, id, providerId, providerId, "*", "Custom",
                (int)context, price, 0,
                string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl,
                "手动编辑", 0, priceOff, 0));
            BroadcastStateForAll();
            return HttpResponse.JsonBody(Ok());
        }
        if (req.Method == "GET" && req.Path == "/state")
            return HttpResponse.JsonBody(SerializeState(slot, AgentView(), SlotBusyFlags()));

        // 槽位切换（改本客户端绑定的槽位）
        if (req.Method == "POST" && req.Path == "/slot")
        {
            var body = Json.Parse(req.Body);
            var idx = body != null ? (int)Math.Round(body["slot"]?.AsNumber() ?? -1) : -1;
            if (idx < 0 || idx >= SlotCount)
                return HttpResponse.JsonBody(Err("槽位索引须在 0~9 之间"));
            if (!BindClientSlot(clientId, idx))
                return HttpResponse.JsonBody(Err("槽位已被其他页面占用"));
            // 切换后刷新该页面自己的 state（activeSlot 高亮 + 该槽位运行态），并按新槽位回放历史
            BroadcastTo(idx, "state", SerializeState(idx, AgentView(), SlotBusyFlags()));
            return HttpResponse.JsonBody(HistoryOf(idx));
        }

        // 换模型
        if (req.Method == "POST" && req.Path == "/model")
        {
            var body = Json.Parse(req.Body);
            var modelId = body?["modelId"]?.AsString() ?? "";
            var providerId = body?["providerId"]?.AsString() ?? "";
            var baseUrl = body?["baseUrl"]?.AsString() ?? "";
            var apiKey = body?["apiKey"]?.AsString();
            if (string.IsNullOrWhiteSpace(modelId))
                return HttpResponse.JsonBody(Err("缺少 modelId"));
            var webSlot = _slots[slot];
            lock (webSlot.StartLock)
            {
                webSlot.Quiescing = true; // 先暂停排队启动（阻止旧批次 finally 启动新任务与 Reconfigure 并发）
                while (webSlot.PendingInputs.TryDequeue(out _)) { } // 清空排队（旧指令按旧模型排队已无意义）
            }
            try
            {
                Interrupt(slot);
                await WaitForSlotIdleAsync(webSlot); // 等退场 ChatAsync 收尾，避免与 Reconfigure 竞态
                var agent = EnsureSlot(slot);
                var error = ApplyModel(agent, modelId, apiKey, providerId, baseUrl);
                if (error != null) return HttpResponse.JsonBody(Err(error));
                BroadcastStateForAll();
                return HttpResponse.JsonBody(Ok());
            }
            finally
            {
                lock (webSlot.StartLock) webSlot.Quiescing = false;
            }
        }

        // 切换工作模式（行为轴 Build/Plan/Chat，槽位实例级）：改绑定槽位 Agent 实例 + 同步全局镜像。
        // 工具约束 / 模式提示词读取的是 agent.WorkMode，改属性即时生效（无需重建工具集）。
        if (req.Method == "POST" && req.Path == "/mode")
        {
            var body = Json.Parse(req.Body);
            var action = body?["action"]?.AsString() ?? "";
            var modeStr = body?["mode"]?.AsString() ?? "";
            // set 解析失败 → 直接报错不改模式（绝不回退 CycleNext，否则 malformed set 会意外循环切换）
            WorkMode newMode;
            if (action == "set")
            {
                if (!Enum.TryParse<WorkMode>(modeStr, true, out var parsed))
                    return HttpResponse.JsonBody(Err($"未知工作模式「{modeStr}」（build/plan/chat）"));
                newMode = parsed;
            }
            else newMode = WorkModeManager.CycleNext(); // action=cycle 或未指定 → 循环切换
            var agent = EnsureSlot(slot);
            agent.WorkMode = newMode;
            WorkModeManager.CurrentMode = newMode; // 全局镜像（与 TUI 状态栏等一致）
            BroadcastStateForAll();
            return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("mode", newMode.ToString().ToLowerInvariant()).ToJson());
        }

        // 保存模型配置（仅持久化默认模型，不中断当前会话、不重配 Agent）
        if (req.Method == "POST" && req.Path == "/model/save")
        {
            var body = Json.Parse(req.Body);
            var modelId = body?["modelId"]?.AsString() ?? "";
            var providerId = body?["providerId"]?.AsString() ?? "";
            var baseUrl = body?["baseUrl"]?.AsString() ?? "";
            var info = string.IsNullOrWhiteSpace(baseUrl) ? ModelCatalog.Find(modelId) : ModelCatalog.Find(modelId, baseUrl);
            if (info == null) return HttpResponse.JsonBody(Err($"未知模型「{modelId}」"));
            var effProviderId = !string.IsNullOrWhiteSpace(providerId) ? providerId : info.ProviderId;
            // baseUrl 未显式传 → 用 provider 注册表地址（Find 内置官方优先会取错网关，与 ApplyModel /settings 同理）
            var effBaseUrl = !string.IsNullOrWhiteSpace(baseUrl) ? baseUrl
                : ConnectionConfig.ResolveBaseUrl(effProviderId) ?? info.DefaultBaseUrl;
            ConnectionConfig.ApplyModelChoice(effProviderId, modelId, isLarge: true, out _, effBaseUrl);
            BroadcastStateForAll();
            return HttpResponse.JsonBody(Ok());
        }

        // 输入 / 更新 key（按供应商存 ApiKeyStore）
        if (req.Method == "POST" && req.Path == "/key")
        {
            var body = Json.Parse(req.Body);
            var providerId = body?["providerId"]?.AsString() ?? "";
            var apiKey = body?["apiKey"]?.AsString() ?? "";
            if (string.IsNullOrWhiteSpace(providerId))
                return HttpResponse.JsonBody(Err("缺少 providerId"));
            // 合法性校验：环境变量引用（$VAR / %VAR%）与非法字符一律拒绝
            if (ApiKeyStore.IsEnvVarRef(apiKey) || !ApiKeyStore.IsValidApiKey(apiKey))
                return HttpResponse.JsonBody(Err("Key 含非法字符或环境变量引用（只允许英文字母数字 + - _ . ,）"));
            SetProviderKey(providerId, apiKey);
            BroadcastStateForAll();
            return HttpResponse.JsonBody(Ok());
        }

        // 设置
        if (req.Method == "GET" && req.Path == "/settings")
            return HttpResponse.JsonBody(SerializeSettings());
        if (req.Method == "POST" && req.Path == "/settings")
        {
            var body = Json.Parse(req.Body);
            var key = body?["key"]?.AsString() ?? "";
            var value = body?["value"]?.AsString() ?? "";
            if (string.IsNullOrWhiteSpace(key))
                return HttpResponse.JsonBody(Err("缺少设置项 key"));
            // 模型类设置项走 connect 统一入口（携带 providerId/baseUrl——修掉小模型丢字段的 bug）
            if (key.Equals("Model", StringComparison.OrdinalIgnoreCase)
                || key.Equals("SmallModel", StringComparison.OrdinalIgnoreCase))
            {
                var providerId = body?["providerId"]?.AsString() ?? "";
                var baseUrl = body?["baseUrl"]?.AsString() ?? "";
                var isLarge = key.Equals("Model", StringComparison.OrdinalIgnoreCase);
                var info = string.IsNullOrWhiteSpace(baseUrl) ? ModelCatalog.Find(value) : ModelCatalog.Find(value, baseUrl);
                var effProviderId = !string.IsNullOrWhiteSpace(providerId)
                    ? providerId : (info?.ProviderId ?? Config.Instance.Provider);
                // baseUrl 未显式传 → 用 provider 注册表地址（Find 内置官方优先会取错网关，与 ApplyModel 同理）
                var effBaseUrl = !string.IsNullOrWhiteSpace(baseUrl) ? baseUrl
                    : ConnectionConfig.ResolveBaseUrl(effProviderId) ?? info?.DefaultBaseUrl;
                ConnectionConfig.ApplyModelChoice(effProviderId, value, isLarge, out _, effBaseUrl);
                // 运行时重配当前槽位 Agent（与 /model 大模型切换一致，避免「显示切了、实际模型没换」）
                try
                {
                    var agent = EnsureSlot(slot);
                    if (isLarge)
                    {
                        agent.LlmClient.Model = value;
                        agent.UpdateContextWindow(ModelCatalog.ResolveContextWindow(value, Config.Instance.MaxContextTokens));
                    }
                    else agent.LlmClient.SmallModel = value;
                }
                catch { /* 槽位未就绪忽略，配置已持久化 */ }
                Config.Instance.SaveToEnvFile();
                BroadcastStateForAll();
                return HttpResponse.JsonBody(Ok());
            }
            var ok = Config.TrySetPropValue(key, value, out var error);
            if (!ok) return HttpResponse.JsonBody(Err(error ?? "设置失败"));
            Config.Instance.SaveToEnvFile();
            BroadcastStateForAll();
            return HttpResponse.JsonBody(Ok());
        }

        // 沙箱边界切换（与 CLI /perm 对齐：边界轴独立于权限轴）
        if (req.Method == "POST" && req.Path == "/perm")
        {
            var body = Json.Parse(req.Body);
            var mode = body?["mode"]?.AsString() ?? "";
            if (string.IsNullOrWhiteSpace(mode))
                return HttpResponse.JsonBody(Err("缺少 mode"));
            SandboxManager.SetLevel(mode);
            BroadcastStateForAll();
            return HttpResponse.JsonBody(Ok());
        }

        // 权限模式切换（确认轴，与 CLI /permit 对齐）
        if (req.Method == "POST" && req.Path == "/permit")
        {
            var body = Json.Parse(req.Body);
            var mode = body?["mode"]?.AsString() ?? "";
            if (string.IsNullOrWhiteSpace(mode))
                return HttpResponse.JsonBody(Err("缺少 mode"));
            if (PermissionManager.IsChatModeAlias(mode))
                WorkModeManager.SetMode(WorkMode.Chat);
            else
                PermissionManager.SetMode(mode);
            BroadcastStateForAll();
            return HttpResponse.JsonBody(Ok());
        }

        // 斜杠命令（Web 版精简路由：/help /perm /model list /reset /session /tokens /mcp /todo /interrupt）
        if (req.Method == "POST" && req.Path == "/command")
        {
            var body = Json.Parse(req.Body);
            var input = body?["input"]?.AsString() ?? "";
            if (string.IsNullOrWhiteSpace(input))
                return HttpResponse.JsonBody(Err("缺少 input"));

            var trimmed = input.Trim();
            var spaceIdx = trimmed.IndexOf(' ');
            var cmdLower = (spaceIdx < 0 ? trimmed : trimmed[..spaceIdx]).ToLowerInvariant();

            // 中断副作用在路由层执行（需访问实例 _slots[slot].Cts）
            if (cmdLower is "/interrupt" or "/stop")
            {
                Interrupt(slot);
                return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("handled", true).Set("output", "⏹ 已请求中断").ToJson());
            }

            // /review — 代码审查：生成审查 prompt 投递为普通消息（后台 Agent 执行审查，结果流式显示）
            if (cmdLower is "/review" or "/审查")
            {
                var prompt = ReviewMode.BuildReviewPrompt();
                StartSlotTask(slot, prompt);
                return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("handled", true)
                    .Set("output", "🔍 开始代码审查（git diff + 正确性/安全/性能/可维护性/测试覆盖）… 结果流式显示").ToJson());
            }

            // 换模型副作用（需访问实例 Interrupt + EnsureSlot）
            if (cmdLower == "/model" && spaceIdx >= 0)
            {
                var name = trimmed[(spaceIdx + 1)..].Trim();
                // /model list 列出模型（避免被当成模型名搜索返回「未知模型」，也不打断运行中的任务）
                if (name is "list" or "ls" or "-l")
                    return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("handled", true)
                        .Set("output", WebModelListText()).ToJson());
                var webSlot = _slots[slot];
                lock (webSlot.StartLock)
                {
                    webSlot.Quiescing = true; // 先暂停排队启动，防 finally 启动新任务与 Reconfigure 并发
                    while (webSlot.PendingInputs.TryDequeue(out _)) { }
                }
                try
                {
                    Interrupt(slot);
                    await WaitForSlotIdleAsync(webSlot); // 等退场 ChatAsync 收尾再 Reconfigure
                    var matches = ModelCatalog.Search(name);
                    if (matches.Length == 0)
                        return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("handled", true)
                            .Set("output", $"❌ 未知模型「{name}」").ToJson());
                    var err = ApplyModel(EnsureSlot(slot), matches[0].Id, null);
                    BroadcastStateForAll();
                    return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("handled", true)
                        .Set("output", err == null ? $"✅ 已切换到 **{matches[0].DisplayName}**" : $"❌ {err}").ToJson());
                }
                finally
                {
                    lock (webSlot.StartLock) webSlot.Quiescing = false;
                }
            }

            var (handled, output) = HandleCommand(trimmed, _slots[slot].Agent, slot);
            if (!handled)
                return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("handled", false).ToJson());

            // 副作用命令（清空/加载会话）刷新历史与会话列表
            if (cmdLower is "/reset" or "/clear" or "/session")
            {
                var agent = _slots[slot].Agent;
                if (agent != null)
                {
                    BroadcastTo(slot, "history", SerializeHistory(agent));
                    BroadcastTo(slot, "sessions", SerializeSessions(slot));
                    BroadcastStateForAll();
                }
            }

            return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("handled", true).Set("output", output).ToJson());
        }

        // 右栏信息面板
        if (req.Method == "GET" && req.Path == "/panel")
            return HttpResponse.JsonBody(SerializePanel(slot, AgentView()));

        // 会话记录（槽位隔离：每个页面只看自己槽位的会话）
        if (req.Method == "GET" && req.Path == "/sessions")
            return HttpResponse.JsonBody(SerializeSessions(slot));
        if (req.Method == "POST" && req.Path == "/sessions/new")
        {
            // 「新建会话」= 清空当前对话、开新对话（不再落盘保存，避免攒出大量临时会话文件）
            Interrupt(slot);
            var agent = EnsureSlot(slot);
            agent.ClearMessages();
            BroadcastTo(slot, "history", SerializeHistory(agent));
            BroadcastStateForAll();
            return HttpResponse.JsonBody(Ok());
        }
        if (req.Method == "POST" && req.Path == "/sessions/load")
        {
            var body = Json.Parse(req.Body);
            var id = body?["id"]?.AsString() ?? "";
            if (string.IsNullOrWhiteSpace(id))
                return HttpResponse.JsonBody(Err("缺少会话 id"));
            var loaded = SessionManager.LoadSession(id, slot);
            if (loaded == null)
                return HttpResponse.JsonBody(Err("会话不存在"));
            Interrupt(slot);
            var agent = EnsureSlot(slot);
            agent.ReplaceMessages(loaded.Value.Messages);
            if (!string.IsNullOrWhiteSpace(loaded.Value.Model))
                agent.LlmClient.Model = loaded.Value.Model;
            BroadcastTo(slot, "history", SerializeHistory(agent));
            BroadcastStateForAll();
            return HttpResponse.JsonBody(Ok());
        }
        if (req.Method == "POST" && req.Path == "/sessions/delete")
        {
            var body = Json.Parse(req.Body);
            var id = body?["id"]?.AsString() ?? "";
            if (string.IsNullOrWhiteSpace(id))
                return HttpResponse.JsonBody(Err("缺少会话 id"));
            SessionManager.DeleteSession(id, slot);
            BroadcastTo(slot, "sessions", SerializeSessions(slot));
            return HttpResponse.JsonBody(Ok());
        }
        if (req.Method == "POST" && req.Path == "/sessions/clear")
        {
            var deleted = SessionManager.DeleteAllSessions(slot);
            BroadcastTo(slot, "sessions", SerializeSessions(slot));
            return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("deleted", deleted).ToJson());
        }
        if (req.Method == "POST" && req.Path == "/sessions/rename")
        {
            var body = Json.Parse(req.Body);
            var id = body?["id"]?.AsString() ?? "";
            var newId = body?["newId"]?.AsString() ?? "";
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(newId))
                return HttpResponse.JsonBody(Err("缺少 id 或 newId"));
            if (!SessionManager.RenameSession(id, newId, slot))
                return HttpResponse.JsonBody(Err("重命名失败"));
            BroadcastTo(slot, "sessions", SerializeSessions(slot));
            return HttpResponse.JsonBody(Ok());
        }

        // Web 交互桥：回答 Agent 的提问/确认
        if (req.Method == "POST" && req.Path == "/answer")
        {
            var body = Json.Parse(req.Body);
            var requestId = body?["requestId"]?.AsString() ?? "";
            if (string.IsNullOrWhiteSpace(requestId))
                return HttpResponse.JsonBody(Err("缺少 requestId"));
            return HttpResponse.JsonBody(AnswerQuestion(requestId, body?["value"]));
        }

        // 多模态上传：图片（入 vision 队列）/ 音频（转录为文字）
        if (req.Method == "POST" && req.Path == "/upload")
            return await HandleUpload(req);

        // ── 特殊前缀输入 ──
        // !Shell指令：直接执行 bash 并返回输出（不回传 Agent，对标 Claude Code `!`）
        if (req.Method == "POST" && req.Path == "/shell")
        {
            var body = Json.Parse(req.Body);
            var command = body?["command"]?.AsString() ?? "";
            if (string.IsNullOrWhiteSpace(command))
                return HttpResponse.JsonBody(Err("缺少 command"));
            try
            {
                // 权限确认：YOLO/只读命令自动放行，危险命令走交互桥弹浏览器确认框（与 Agent 工具一致，不再绕过权限）
                var allowed = await PermissionManager.CheckAsync("bash", new Dictionary<string, object?> { ["command"] = command });
                if (!allowed)
                    return HttpResponse.JsonBody(JNode.Object().Set("ok", false).Set("output", "已拒绝执行").ToJson());
                var result = await new BashTool()
                    .ExecuteAsync(new Dictionary<string, object?> { ["command"] = command });
                return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("output", result).ToJson());
            }
            catch (Exception ex)
            {
                return HttpResponse.JsonBody(JNode.Object().Set("ok", false).Set("output", ex.Message).ToJson());
            }
        }

        // #文件引用：读取文件内容并注入当前对话上下文（对标 Claude Code `#`）
        if (req.Method == "POST" && req.Path == "/fileref")
        {
            var body = Json.Parse(req.Body);
            var path = body?["path"]?.AsString() ?? "";
            if (string.IsNullOrWhiteSpace(path))
                return HttpResponse.JsonBody(Err("缺少 path"));
            // 路径穿越防护：限制在项目根目录内，拒绝 ../ 逃逸到任意文件
            var safePath = ResolveWithinRoot(path);
            if (safePath == null)
                return HttpResponse.JsonBody(JNode.Object().Set("ok", false).Set("error", "路径超出项目根目录").ToJson());
            var content = await new ReadFileTool()
                .ExecuteAsync(new Dictionary<string, object?> { ["file_path"] = safePath });
            var agent = EnsureSlot(slot);
            agent.AddMessage(JNode.Object().Set("role", "user")
                .Set("content", $"【文件引用】{path}\n\n{content}"));
            BroadcastTo(slot, "history", SerializeHistory(agent));
            return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("path", path).Set("content", content).ToJson());
        }

        // #文件引用补全：按前缀列出文件/目录
        if (req.Method == "POST" && req.Path == "/filelist")
        {
            var body = Json.Parse(req.Body);
            var prefix = body?["prefix"]?.AsString() ?? "";
            return HttpResponse.JsonBody(JNode.Object().Set("ok", true)
                .Set("files", Json.Parse(SerializeFileList(prefix)) ?? JNode.Array()).ToJson());
        }

        // ═══════════════ 内置编辑器（三端共享 EditorCore 模型，Web 端只做文件 IO + lint）═══════════════
        // 文件树：列一级目录（dir 为空 = 项目根）
        if (req.Method == "GET" && req.Path == "/editor/list")
        {
            var dir = QueryParam(req.Query, "dir") ?? "";
            var safeDir = ResolveWithinRoot(string.IsNullOrEmpty(dir) ? "." : dir);
            if (safeDir == null)
                return HttpResponse.JsonBody(JNode.Object().Set("ok", false).Set("error", "路径超出项目根目录").ToJson());
            return HttpResponse.JsonBody(JNode.Object().Set("ok", true)
                .Set("path", dir)
                .Set("entries", Json.Parse(SerializeEditorList(safeDir)) ?? JNode.Array()).ToJson());
        }
        // 读文件（缺失文件返回空 content，对齐 EditorCore.LoadFile 建空缓冲）
        if (req.Method == "GET" && req.Path == "/editor/file")
        {
            var path = QueryParam(req.Query, "path") ?? "";
            if (string.IsNullOrWhiteSpace(path))
                return HttpResponse.JsonBody(Err("缺少 path"));
            var safe = ResolveWithinRoot(path);
            if (safe == null)
                return HttpResponse.JsonBody(JNode.Object().Set("ok", false).Set("error", "路径超出项目根目录").ToJson());
            var content = "";
            var encoding = "UTF-8";
            if (File.Exists(safe))
            {
                try { var d = TextEncoding.ReadFile(safe); content = d.Text; encoding = d.EncodingName; }
                catch { return HttpResponse.JsonBody(JNode.Object().Set("ok", false).Set("error", "读取失败（编码/权限）").ToJson()); }
            }
            return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("path", path).Set("content", content).Set("encoding", encoding).ToJson());
        }
        // 保存（写文件 + fire-and-forget lint，前端稍后轮询 /editor/diags）
        if (req.Method == "POST" && req.Path == "/editor/save")
        {
            var body = Json.Parse(req.Body);
            var path = body?["path"]?.AsString() ?? "";
            var content = body?["content"]?.AsString() ?? "";
            var encoding = body?["encoding"]?.AsString();
            if (string.IsNullOrWhiteSpace(path))
                return HttpResponse.JsonBody(Err("缺少 path"));
            var full = SaveEditorFile(Directory.GetCurrentDirectory(), path, content, encoding);
            if (full == null)
                return HttpResponse.JsonBody(JNode.Object().Set("ok", false).Set("error", "路径超出项目根目录").ToJson());
            // fire-and-forget lint（前端稍后轮询 /editor/diags；lint 失败不影响保存）
            _ = Task.Run(async () => { try { await DiagnosticManager.RunLintAsync(full); } catch { /* lint 失败不影响保存 */ } });
            return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("path", path).ToJson());
        }
        // 诊断（读 DiagnosticManager 缓存，按行分组）
        if (req.Method == "GET" && req.Path == "/editor/diags")
        {
            var path = QueryParam(req.Query, "path") ?? "";
            if (string.IsNullOrWhiteSpace(path))
                return HttpResponse.JsonBody(Err("缺少 path"));
            var safe = ResolveWithinRoot(path);
            if (safe == null)
                return HttpResponse.JsonBody(JNode.Object().Set("ok", false).Set("error", "路径超出项目根目录").ToJson());
            return HttpResponse.JsonBody(SerializeEditorDiags(safe));
        }

        return null;
    }

    /// <summary>按前缀列出当前目录下的文件/目录（供 # 文件引用补全）。纯静态便于自测。</summary>
    public static string SerializeFileList(string prefix)
    {
        var arr = JNode.Array();
        try
        {
            var searchDir = Directory.GetCurrentDirectory();
            var filePrefix = prefix ?? "";
            var lastSep = filePrefix.LastIndexOfAny(['/', '\\']);
            if (lastSep >= 0)
            {
                var sub = filePrefix[..(lastSep + 1)];
                // 路径穿越防护：目录限定在项目根目录内，越界回退到根目录
                searchDir = ResolveWithinRoot(sub) ?? Directory.GetCurrentDirectory();
                filePrefix = filePrefix[(lastSep + 1)..];
            }
            if (!Directory.Exists(searchDir)) return arr.ToJson();
            // 先按前缀过滤再 Take(40)：避免「取前 40 条再过滤」导致后续匹配项被丢弃
            var matches = Directory.EnumerateFileSystemEntries(searchDir)
                .Select(e => (Entry: e, Name: Path.GetFileName(e)))
                .Where(x => string.IsNullOrEmpty(filePrefix) || x.Name.StartsWith(filePrefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => Directory.Exists(x.Entry) ? 0 : 1)
                .ThenBy(x => x.Entry)
                .Take(40);
            foreach (var x in matches)
            {
                var isDir = Directory.Exists(x.Entry);
                arr.Add(JNode.Object()
                    .Set("name", isDir ? x.Name + "/" : x.Name)
                    .Set("path", Path.Combine(searchDir, x.Name))
                    .Set("isDir", isDir));
            }
        }
        catch { /* 权限不足忽略 */ }
        return arr.ToJson();
    }

    /// <summary>把用户提供的相对/绝对路径解析为绝对路径并限制在项目根目录（cwd）内；越界或非法返回 null。纯静态便于自测。</summary>
    public static string? ResolveWithinRoot(string path)
        => SafeResolveWithinRoot(Directory.GetCurrentDirectory(), path);

    /// <summary>root 感知版路径守卫：full 须落在 root 内（含 root 自身）。越界/非法返回 null。纯静态便于自测。</summary>
    /// <remarks>相对路径基于 root 解析（而非进程 cwd）——web 编辑器保存相对路径时 root 可能与 cwd 不同。</remarks>
    public static string? SafeResolveWithinRoot(string root, string path)
    {
        try
        {
            var rootFull = Path.GetFullPath(root);
            var combined = Path.IsPathRooted(path) ? path : Path.Combine(rootFull, path);
            var full = Path.GetFullPath(combined);
            var rel = Path.GetRelativePath(rootFull, full);
            var outside = rel == ".."
                || rel.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                || rel.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal)
                || Path.IsPathRooted(rel);
            return outside ? null : full;
        }
        catch { return null; }
    }

    // ═══════════════ 内置编辑器辅助（纯静态，便于自测）═══════════════

    /// <summary>从 query string 提取参数（URL 解码，安全回退）。纯静态便于自测。</summary>
    public static string? QueryParam(string? query, string name)
    {
        if (string.IsNullOrEmpty(query)) return null;
        foreach (var kv in query.Split('&'))
        {
            int eq = kv.IndexOf('=');
            if (eq <= 0) continue;
            if (kv[..eq].Equals(name, StringComparison.OrdinalIgnoreCase))
                return SafeUnescape(kv[(eq + 1)..]);
        }
        return null;
    }

    /// <summary>列出目录条目（dirs 优先 + 名称排序 + 过滤隐藏/构建产物）。dir 为已守卫的绝对路径。纯静态便于自测。</summary>
    public static string SerializeEditorList(string dir)
    {
        var arr = JNode.Array();
        try
        {
            if (!Directory.Exists(dir)) return arr.ToJson();
            var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { ".git", "node_modules", "bin", "obj", "dist", ".vs", ".idea", ".waycoder", ".svn", "__pycache__", ".claude" };
            var entries = Directory.EnumerateFileSystemEntries(dir)
                .Select(e => (Entry: e, Name: Path.GetFileName(e)))
                .Where(x => !x.Name.StartsWith('.'))
                .Where(x => !skip.Contains(x.Name))
                .OrderBy(x => Directory.Exists(x.Entry) ? 0 : 1)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var x in entries)
            {
                var isDir = Directory.Exists(x.Entry);
                arr.Add(JNode.Object()
                    .Set("name", x.Name + (isDir ? "/" : ""))
                    .Set("path", Path.Combine(dir, x.Name))
                    .Set("isDir", isDir));
            }
        }
        catch { /* 权限不足忽略 */ }
        return arr.ToJson();
    }

    /// <summary>编辑器保存：路径守卫 + 建父目录 + 按编码写回。越界/非法返回 null（不写盘）。纯静态便于自测。
    /// encoding 为前端加载时回传的编码名（保真 GB18030/BOM）；空则沿用旧 BOM 保留策略（兼容旧前端）。</summary>
    public static string? SaveEditorFile(string root, string relPath, string content, string? encoding = null)
    {
        var full = SafeResolveWithinRoot(root, relPath);
        if (full == null) return null;
        Global.EnsureDir(full);
        if (string.IsNullOrEmpty(encoding)) Global.WriteAllTextPreserveBom(full, content);
        else TextEncoding.WriteFile(full, content, TextEncoding.GetByName(encoding));
        return full;
    }

    /// <summary>按行分组输出诊断 JSON：{ok, summary:{errors,warnings}, lines:{ "行号":[Diagnostic] }}。纯静态便于自测。</summary>
    public static string SerializeEditorDiags(string filePath)
    {
        var (errors, warnings) = DiagnosticManager.GetSummary(filePath);
        var byLine = new Dictionary<int, List<Diagnostic>>();
        foreach (var d in DiagnosticManager.GetAll(filePath))
        {
            if (!byLine.TryGetValue(d.Line, out var list))
                byLine[d.Line] = list = [];
            list.Add(d);
        }
        var lines = JNode.Object();
        foreach (var kv in byLine)
        {
            var arr = JNode.Array();
            foreach (var d in kv.Value)
            {
                arr.Add(JNode.Object()
                    .Set("line", d.Line).Set("col", d.Column)
                    .Set("severity", d.Severity.ToString().ToLowerInvariant())
                    .Set("message", d.Message)
                    .Set("code", d.Code ?? ""));
            }
            lines.Set(kv.Key.ToString(), arr);
        }
        return JNode.Object()
            .Set("ok", true)
            .Set("summary", JNode.Object().Set("errors", errors).Set("warnings", warnings))
            .Set("lines", lines)
            .ToJson();
    }

    // ═══════════════════════════════════════════════════════════
    //  槽位管理
    // ═══════════════════════════════════════════════════════════

    /// <summary>Web 槽位 Agent 的唯一标识（供 PendingImages 按 agentId 分队列，隔离多槽位图片）。</summary>
    private static string WebSlotAgentId(int idx) => $"web-slot-{idx}";

    /// <summary>惰性创建槽位 Agent（每槽位独立 LLM + 历史），复用全局模型/密钥配置。</summary>
    private Agent EnsureSlot(int idx)
    {
        var slot = _slots[idx];
        if (slot.Agent == null)
        {
            // double-checked locking：多个路由（/model、/sessions/new、/fileref 等）并发首建时，
            // 若无锁两个线程会各 new 一个 Agent，后写者覆盖前者，拿旧 Agent 的请求落到孤儿实例上。
            lock (slot.AgentLock)
            {
                if (slot.Agent == null)
                {
                    var cfg = Config.Instance;
                    var info = ConnectionConfig.ResolveActiveModel(cfg); // 精确匹配当前供应商，防同名模型误配 baseUrl/key
                    var providerId = ConnectionConfig.ResolveActiveProviderId(cfg);
                    var key = ApiKeyStore.Get(providerId) ?? cfg.ApiKey;
                    // key 绑定的 baseURL 优先（key 与其调用地址一致）
                    var baseUrl = ApiKeyStore.GetBaseUrl(providerId) ?? ResolveBaseUrl(info, providerId, cfg.BaseUrl);
                    var llm = new LLM(cfg.Model, key, baseUrl, cfg.MaxTokens, cfg.Temperature)
                    {
                        SmallModel = cfg.SmallModel,
                    };
                    slot.Agent = new Agent(llm,
                        maxContextTokens: ModelCatalog.ResolveContextWindow(cfg.Model, cfg.MaxContextTokens),
                        maxBudgetUsd: cfg.MaxBudgetUsd, autoCommit: cfg.AutoGitCommit)
                    {
                        // 槽位唯一标识：文件锁跨槽位检测 + PendingImages 按 agentId 分队列（此前默认 "main" 导致多槽位图片串扰）
                        AgentId = WebSlotAgentId(idx),
                    };
                }
            }
        }
        return slot.Agent!;
    }

    /// <summary>各槽位忙碌标志数组（供 SerializeState 上报，前端据此切换发送/停止态）。</summary>
    private bool[] SlotBusyFlags()
    {
        var flags = new bool[SlotCount];
        for (int i = 0; i < SlotCount; i++)
            flags[i] = _slots[i].IsBusy;
        return flags;
    }

    /// <summary>把 <see cref="WebSlot"/> 数组映射为 <see cref="Agent"/> 数组视图（供 SerializeState/SerializePanel 等签名保持 Agent?[] 的方法使用）。</summary>
    private Agent?[] AgentView()
    {
        var view = new Agent?[SlotCount];
        for (int i = 0; i < SlotCount; i++) view[i] = _slots[i].Agent;
        return view;
    }

    /// <summary>序列化指定槽位的历史（无 Agent 时返回空数组）。</summary>
    private string HistoryOf(int slot)
    {
        var agent = _slots[slot].Agent;
        return agent == null ? "[]" : SerializeHistory(agent);
    }

    /// <summary>中断指定槽位的后台 Agent（与 StartSlotTask 共享 StartLock 原子摘除 Cts + Cancel + Dispose）。</summary>
    private void Interrupt(int slotIdx)
    {
        var slot = _slots[slotIdx];
        lock (slot.StartLock)
        {
            var cts = slot.Cts;
            slot.Cts = null;
            if (cts != null)
            {
                try { cts.Cancel(); } catch { }
                cts.Dispose();
            }
        }
    }

    /// <summary>
    /// 槽位并发执行入口（镜像终端 StartSlotTask）：槽位空闲则懒建 Agent 后台 Task.Run 跑 ChatAsync，
    /// 流式回调按槽位路由到该槽位绑定的页面；忙则广播 system 提示不排队。
    /// </summary>
    private void StartSlotTask(int slotIdx, string userInput)
    {
        var slot = _slots[slotIdx];
        Agent agent;
        CancellationTokenSource roundCts;
        // 原子抢占：check + 懒建 Agent + 写 Cts/IsBusy 全部在 StartLock 内完成。
        // 1) EnsureSlot 失败时 IsBusy 尚未置位，无需回滚（消除「槽位永久 busy」）；
        // 2) Cts 赋值先于 IsBusy=true 且均在锁内，Interrupt 要么等到 Cts 就绪后 Cancel（正确中断），
        //    要么在锁释放后进来（Cts 已就绪）——彻底消除「启动窗口内中断被 Exchange 取 null 丢弃」竞态。
        lock (slot.StartLock)
        {
            if (slot.IsBusy)
            {
                // 排队：不打断当前任务 —— 指令入队，当前批次完成后由 TryStartNextPending 自动取下一个执行。
                // 队列防无限增长：满则丢最旧保最新（与 TUI MaxPendingSubmissions 同语义）。
                if (InputQueueFull(slot.PendingInputs.Count))
                {
                    slot.PendingInputs.TryDequeue(out _);
                    BroadcastTo(slotIdx, "system", JsonStr("⚠️ 排队已满，丢弃最旧指令"));
                }
                slot.PendingInputs.Enqueue(userInput);
                BroadcastTo(slotIdx, "system", JsonStr("⏳ Agent 忙碌中 — 指令已排队，当前批次完成后自动执行"));
                return;
            }
            try
            {
                agent = EnsureSlot(slotIdx);
                roundCts = new CancellationTokenSource();
            }
            catch (Exception ex)
            {
                // EnsureSlot（配置读取 / 模型目录 / new LLM / new Agent）抛异常：广播失败，不置 IsBusy
                BroadcastTo(slotIdx, "failed", JsonStr($"启动失败：{ex.Message}"));
                return;
            }
            slot.Cts = roundCts;
            slot.IsBusy = true;
        }
        BroadcastStatus(slotIdx); // 开始执行 → 思考中
        var token = roundCts.Token;
        slot.RunningTask = Task.Run(async () =>
        {
            _currentSlot.Value = slotIdx;
            StructuredMemory.CurrentSlotIndex = slotIdx; // 绑定本槽位记忆目录（AsyncLocal）
            try
            {
                var final = await agent.ChatAsync(
                    userInput,
                    onToken: t =>
                    {
                        BroadcastStatus(slotIdx); // 思考中（LastStatus 去重，非每 token 刷）
                        BroadcastTo(slotIdx, "token", JsonStr(t));
                    },
                    onTool: (name, brief) =>
                    {
                        BroadcastStatus(slotIdx, name); // 工具/等待用户/等待子代理
                        BroadcastTo(slotIdx, "tool", JsonTool(name, brief));
                    },
                    onToolOutput: o => BroadcastTo(slotIdx, "tool_output", JsonStr(o)),
                    cancellationToken: token);
                slot.CompleteAtTicks = Environment.TickCount64; // 完成瞬态时间戳
                BroadcastTo(slotIdx, "done", JsonStr(final));
            }
            catch (OperationCanceledException)
            {
                BroadcastTo(slotIdx, "interrupted", "null");
            }
            catch (Exception ex)
            {
                BroadcastTo(slotIdx, "failed", JsonStr(ex.Message));
            }
            finally
            {
                // 摘除本轮 CTS 并置空闲也在 StartLock 内，与 Interrupt/下一次 StartSlotTask 互斥，
                // 保证 slot.Cts 读写一致；若 Cts 已被 Interrupt 摘除（中断路径）则不重复 Dispose。
                lock (slot.StartLock)
                {
                    if (ReferenceEquals(slot.Cts, roundCts))
                    {
                        slot.Cts = null;
                        roundCts.Dispose();
                    }
                    slot.IsBusy = false;
                }
                slot.RunningTask = null;
                _currentSlot.Value = 0;
                BroadcastStatus(slotIdx); // 回落：done→完成瞬态 / 中断·失败→空闲
                // 批次完成：取队列中的下一条指令继续执行（输入排队机制）
                TryStartNextPending(slotIdx);
            }
        });
    }

    /// <summary>Agent 批次完成后，若该槽位有待处理指令则取下一个自动执行（排队机制）。
    /// 换模型暂停期（Quiescing）跳过启动，防新任务与 Reconfigure 并发。</summary>
    private void TryStartNextPending(int slotIdx)
    {
        if (slotIdx < 0 || slotIdx >= _slots.Length) return;
        var slot = _slots[slotIdx];
        lock (slot.StartLock)
        {
            if (slot.Quiescing) return; // 换模型暂停：等 Reconfigure 完成后再恢复排队消费
            if (slot.PendingInputs.TryDequeue(out var next))
            {
                // 排队消息开始执行：通知前端清除该消息的「⏳ 排队中…」标记
                BroadcastTo(slotIdx, "queued_started", "null");
                StartSlotTask(slotIdx, next);
            }
        }
    }

    /// <summary>等待槽位的后台 Agent 任务收尾（换模型/存 key 前调用，避免与退场的 ChatAsync 竞态读写 LLM 配置）。</summary>
    private static async Task WaitForSlotIdleAsync(WebSlot slot, int timeoutMs = 5000)
    {
        var task = slot.RunningTask;
        if (task == null) return;
        try { await Task.WhenAny(task, Task.Delay(timeoutMs)); } catch { /* 任务异常不影响换模型 */ }
    }

    // ═══════════════════════════════════════════════════════════
    //  换模型 / 换 key
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 运行时换模型：更新 Config + 重配置当前 Agent 的 LLM（key/baseUrl/Model/上下文窗口）+ 持久化。
    /// 返回 null=成功，否则返回错误信息。纯逻辑，便于自测。
    /// </summary>
    public static string? ApplyModel(Agent agent, string modelId, string? apiKey, string? providerId = null, string? baseUrl = null)
    {
        // 显式传 baseUrl（Web 分组点选）→ Find 精确匹配所选网关；否则内置官方优先
        var info = string.IsNullOrWhiteSpace(baseUrl) ? ModelCatalog.Find(modelId) : ModelCatalog.Find(modelId, baseUrl);
        if (info == null) return $"未知模型「{modelId}」";

        var effProviderId = !string.IsNullOrWhiteSpace(providerId) ? providerId : info.ProviderId;

        // key 解析：显式 apiKey > ApiKeyStore(provider) > 全局 Config.ApiKey
        string key;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            key = apiKey.Trim();
            ApiKeyStore.Set(effProviderId, key);
        }
        else
        {
            key = ApiKeyStore.Get(effProviderId) ?? Config.Instance.ApiKey;
        }

        var cfg = Config.Instance;
        // baseUrl 未显式传 → 用 effProviderId 的注册表地址（含 providers.json 覆盖），而非 info.DefaultBaseUrl——
        // info 来自 Find(modelId, baseUrl)，baseUrl 为空时 Find 内置官方优先，选 baseUrl 为空的模型
        // （如 AIHubMix 网关下的 deepseek-v4-flash）会误取官方网关地址，导致「显示切了、实际连接打错网关」
        var effBaseUrl = !string.IsNullOrWhiteSpace(baseUrl) ? baseUrl
            : ConnectionConfig.ResolveBaseUrl(effProviderId) ?? info?.DefaultBaseUrl;

        // 「切换模型 = 切换 connect」：统一入口同步扁平字段 + 持久化
        ConnectionConfig.ApplyModelChoice(effProviderId, modelId, isLarge: true, out _, effBaseUrl);

        agent.LlmClient.Reconfigure(key, effBaseUrl);
        agent.LlmClient.Model = modelId;
        agent.UpdateContextWindow(ModelCatalog.ResolveContextWindow(modelId, cfg.MaxContextTokens));
        return null;
    }

    private void SetProviderKey(string providerId, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            ApiKeyStore.Remove(providerId);
        }
        else
        {
            ApiKeyStore.Set(providerId, apiKey.Trim());
        }
        // 若某槽位模型属于该供应商，同步重配 key（全局 key，遍历所有已建槽位）
        foreach (var slot in _slots)
        {
            var agent = slot.Agent;
            if (agent == null) continue;
            var cur = ModelCatalog.Find(agent.LlmClient.Model);
            if (cur != null && cur.ProviderId == providerId)
            {
                var baseUrl = ResolveBaseUrl(cur, providerId, Config.Instance.BaseUrl);
                agent.LlmClient.Reconfigure(apiKey.Trim(), baseUrl);
            }
        }
    }

    /// <summary>
    /// 本地导入：按 body.sources 勾选的来源导入（builtin/opencode/openclaw/crush/claude/codex 逗号串或数组；缺省=全部）。
    /// 复用 ModelCli.Import（写全局模型库）+ ApiKeyStore.ImportFromKnownSources（写 key）。
    /// 返回 JSON：{ok, modelReport, keys:[{providerId, source}]}。
    /// </summary>
    private static HttpResponse ImportExternalModels(string? body)
    {
        try
        {
            string? source = null;
            var bodyObj = Json.Parse(body ?? "");
            if (bodyObj != null && bodyObj["sources"] is { } sourcesNode)
                source = sourcesNode.Kind == JKind.Array
                    ? string.Join(",", sourcesNode.Items.Select(x => x?.AsString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)))
                    : sourcesNode.AsString();
            // 本地服务（Ollama/LM Studio/CC Switch 路由）从本地接口实时拉取真实模型；其余从第三方库导入
            bool IsLocalService(string s) => s.Equals("ollama", StringComparison.OrdinalIgnoreCase)
                || s.Equals("lmstudio", StringComparison.OrdinalIgnoreCase)
                || s.Equals("cc-switch", StringComparison.OrdinalIgnoreCase);
            var hasLocalService = source != null && source.Split(',').Any(IsLocalService);
            var report = new StringBuilder();
            if (hasLocalService)
            {
                var nonLocal = string.Join(",", (source ?? "").Split(',').Select(s => s.Trim()).Where(s =>
                    s.Length > 0 && !IsLocalService(s)));
                if (!string.IsNullOrWhiteSpace(nonLocal))
                    report.AppendLine(ModelCli.Import(nonLocal).Trim());
                report.AppendLine(ModelCli.ImportLocalServices().Trim());
            }
            else
            {
                report.AppendLine(ModelCli.Import(source).Trim());
            }
            var modelReport = report.ToString().Trim();
            ModelCatalog.Invalidate();
            ApiKeyStore.ClearCache();
            // key 仅由 api_keys.json + 环境变量决定；导入来源文件的 key 不自动同步（避免导入模型后冒出无关 key）

            return HttpResponse.JsonBody(JNode.Object()
                .Set("ok", true)
                .Set("modelReport", modelReport)
                .Set("keys", JNode.Array())
                .ToJson());
        }
        catch (Exception ex)
        {
            return HttpResponse.JsonBody(Err(ex.Message));
        }
    }

    /// <summary>从 opencode 在线 /models 端点导入模型列表（OpenAI 兼容格式）。
    /// body.mode = "go"（zen/go/v1，订阅制）默认 / "zen"（zen/v1，按量付费）——两个服务商地址不同。</summary>
    private async Task<HttpResponse> ImportOpenCodeOnline(string? body)
    {
        try
        {
            // 在线导入：选择端点（OpenCode Go/Zen / OpenRouter / Groq / SiliconFlow / Together / DeepSeek / OpenAI / Moonshot / models.dev 等）
            string? name = null;
            var bodyObj = Json.Parse(body ?? "");
            if (bodyObj != null && bodyObj["mode"] is { } modeNode) name = modeNode.AsString()?.Trim();
            var src = ModelCli.OnlineSources.FirstOrDefault(s =>
                s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? ModelCli.OnlineSources[0];
            var slot = _currentSlot.Value; // 先捕获（Task.Run 后台线程沿用 AsyncLocal，但显式捕获更稳）
            // 后台线程执行（models.dev 等大源 4.4MB/7000 模型，同步会阻塞请求线程）；
            // 进度经 SSE「import-progress」实时广播给发起页面（拉取→解析→写入→完成，防「卡死感」）
            var report = await Task.Run(() => ModelCli.ImportOnline(src, msg =>
            {
                try { BroadcastTo(slot, "import-progress", msg); } catch { /* 客户端断开忽略 */ }
            }));
            return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("modelReport", report).ToJson());
        }
        catch (Exception ex)
        {
            return HttpResponse.JsonBody(Err($"在线导入失败：{ex.Message}"));
        }
    }

    /// <summary>
    /// BaseUrl 优先级（两层架构：provider 承载唯一地址）：provider 唯一地址 &gt; 模型目录 Url &gt; 全局 Config.BaseUrl。
    /// 地址不同 = 不同服务商，模型用所属 provider 的地址连接。
    /// </summary>
    private static string? ResolveBaseUrl(ModelCatalog.ModelInfo? info, string providerId, string? globalBaseUrl)
    {
        if (ModelCatalog.Providers.TryGetValue(providerId, out var p) && !string.IsNullOrEmpty(p.DefaultBaseUrl))
            return p.DefaultBaseUrl;
        if (info?.DefaultBaseUrl != null) return info.DefaultBaseUrl;
        return globalBaseUrl;
    }

    // ═══════════════════════════════════════════════════════════
    //  SSE 连接 + 广播
    // ═══════════════════════════════════════════════════════════

    private async Task HandleSseAsync(HttpRequest req, StreamWriter writer)
    {
        var clientId = ParseClientQuery(req.Query);
        // 先查满再 ResolveSlot：满员提前 return 不写 _clientSlot（ResolveSlot 在下方才调用），
        // 无新绑定需回滚——删除此处的 _clientSlot.Remove（它误删的是更早 POST /chat 建立的合法绑定，
        // 导致客户端重连后任务输出路由错槽）。
        lock (_lock)
        {
            if (SseClientsFull(_clients.Count))
            {
                try { writer.Write(HttpServer.SseEvent("failed", "\"连接数已达上限\"")); } catch { }
                return; // 拒绝超出上限的 SSE 连接（writer 由调用方 WriteSseAsync 的 using 释放）
            }
        }
        var slot = ResolveSlot(clientId);
        var client = new SseClient { Writer = writer, ClientId = clientId ?? "", SlotIndex = slot };
        lock (_lock)
        {
            _clients.Add(client);
        }
        try
        {
            // 连接即回放历史 + 状态，前端初始化渲染（与 Broadcast 共用写门，避免与并发异步写交错）
            await client.WriteGate.WaitAsync();
            try
            {
                writer.Write(HttpServer.SseEvent("history", HistoryOf(slot)));
                writer.Write(HttpServer.SseEvent("state", SerializeState(slot, AgentView(), SlotBusyFlags())));
            }
            finally { client.WriteGate.Release(); }
            // 同时监听「写失败（Closed）」与「底层流 EOF（客户端主动断开）」：
            // 服务端从不读 SSE 流，若只等 Closed.Task，客户端关标签页后若无后续广播，
            // 连接会永久阻塞在 Closed.Task 上，泄漏 Task/StreamWriter/TcpClient 与连接槽位。
            await Task.WhenAny(client.Closed.Task, WaitForDisconnectAsync(writer.BaseStream));
        }
        catch { /* 连接断开 */ }
        finally
        {
            lock (_lock) _clients.Remove(client);
            // 标记该客户端已关闭：正常断连（WaitForDisconnectAsync EOF）不经过 WriteClient 的写失败分支，
            // 若此处不 Complete，则 Broadcast 已拿到快照里的该 client 会因 Closed.Task.IsCompleted=false
            // 继续写已释放的 writer，退化为「写已释放流 → 吞 ObjectDisposedException」的脏路径。
            client.Closed.TrySetResult();
            // 延迟清理槽位绑定：立即删除会导致断线重连（同 clientId 自动重连）槽位跳变、
            // 页面静默切到另一个槽位；30 秒后仍无该 clientId 的连接才释放（防字典无界增长）。
            if (clientId != null)
            {
                _ = Task.Run(async () =>
                {
                    try { await Task.Delay(30_000); } catch { return; }
                    lock (_lock)
                    {
                        if (!_clients.Any(c => c.ClientId == clientId))
                            _clientSlot.Remove(clientId);
                    }
                });
            }
            // 等异步写排空后释放 writer（WriteGate 保证无在途写），避免对已释放流的并发写
            try { await client.WriteGate.WaitAsync(TimeSpan.FromSeconds(5)); } catch { }
            try { writer.Dispose(); } catch { }
            client.WriteGate.Release();
        }
    }

    /// <summary>读 SSE 底层流直到 EOF（客户端断开时 ReadAsync 返回 0 或抛异常），用于检测空闲断连。</summary>
    private static async Task WaitForDisconnectAsync(Stream stream)
    {
        var buffer = new byte[256];
        try
        {
            while (await stream.ReadAsync(buffer) > 0) { }
        }
        catch { /* 连接断开 / 流已释放 */ }
    }

    /// <summary>只写绑定到指定槽位的客户端（页面作用域事件：token/tool/done/history 等）。</summary>
    private void BroadcastTo(int slotIdx, string type, string dataJson)
    {
        var sse = HttpServer.SseEvent(type, dataJson);
        List<SseClient> snapshot;
        lock (_lock) snapshot = _clients.Where(c => c.SlotIndex == slotIdx).ToList();
        foreach (var c in snapshot) WriteClient(c, sse);
    }

    /// <summary>写所有客户端（全局事件：sessions 列表等）。</summary>
    private void BroadcastAll(string type, string dataJson)
    {
        var sse = HttpServer.SseEvent(type, dataJson);
        List<SseClient> snapshot;
        lock (_lock) snapshot = _clients.ToList();
        foreach (var c in snapshot) WriteClient(c, sse);
    }

    /// <summary>向每个客户端按其绑定槽位刷新 state（模型/权限/设置变更后）。</summary>
    private void BroadcastStateForAll()
    {
        var view = AgentView();
        List<SseClient> snapshot;
        lock (_lock) snapshot = _clients.ToList();
        foreach (var c in snapshot)
            WriteClient(c, HttpServer.SseEvent("state", SerializeState(c.SlotIndex, view, SlotBusyFlags())));
    }

    private static void WriteClient(SseClient c, string sse)
    {
        // 已剔除（慢客户端超时）则跳过；fire-and-forget 异步写——绝不阻塞 Agent 线程
        // （原同步 Wait 在客户端缓冲满（后台标签页暂停读 SSE）时每次 token 卡 5s/客户端，
        // 多个慢客户端串行可让 Web 端「死机」）。写序由 WriteGate FIFO 保证，超时剔除逻辑不变。
        if (c.Closed.Task.IsCompleted) return;
        _ = WriteAsync(c, sse);
    }

    private static async Task WriteAsync(SseClient c, string sse)
    {
        await c.WriteGate.WaitAsync();
        try
        {
            if (c.Closed.Task.IsCompleted) return;
            try
            {
                // 带超时写：客户端停止读 SSE 时 WriteAsync 不完成 → 超时即剔除（后续广播跳过）
                var writeTask = c.Writer.WriteAsync(sse);
                if (await Task.WhenAny(writeTask, Task.Delay(5000)) != writeTask)
                {
                    c.Closed.TrySetResult();
                    // 观察被放弃的写任务：writer 随后被 HandleSseAsync finally 释放时会抛
                    // ObjectDisposedException，若不观察则成为未观察异常
                    try { await writeTask; } catch { }
                    return;
                }
            }
            catch { c.Closed.TrySetResult(); }
        }
        finally { c.WriteGate.Release(); }
    }

    // ═══════════════════════════════════════════════════════════
    //  客户端身份 → 槽位绑定
    // ═══════════════════════════════════════════════════════════

    /// <summary>从 query 里解析 client 标识（如 "client=abc"）。纯静态便于自测。</summary>
    public static string? ParseClientQuery(string? query)
    {
        if (string.IsNullOrEmpty(query)) return null;
        foreach (var kv in query.Split('&'))
        {
            int eq = kv.IndexOf('=');
            if (eq <= 0) continue;
            var name = kv[..eq];
            if (name.Equals("client", StringComparison.OrdinalIgnoreCase))
                return SafeUnescape(kv[(eq + 1)..]);
        }
        return null;
    }

    /// <summary>安全 URL 解码：畸形百分号序列（如 "%zz"）会让 Uri.UnescapeDataString 抛 UriFormatException，回退原串。</summary>
    public static string SafeUnescape(string s)
    {
        try { return Uri.UnescapeDataString(s); }
        catch (UriFormatException) { return s; }
    }

    /// <summary>从占用标记数组（true=占用）挑第一个空闲槽位，全满回退 0。纯静态便于自测。</summary>
    public static int PickFreeSlot(bool[] occupied, int slotCount)
    {
        for (int i = 0; i < slotCount; i++)
            if (!occupied[i]) return i;
        return 0;
    }

    /// <summary>解析客户端对应的槽位：已绑定复用，新客户端分配第一个空闲槽位（0-9）；无 client 标识时固定 0。</summary>
    private int ResolveSlot(string? clientId)
    {
        if (string.IsNullOrEmpty(clientId)) return 0;
        lock (_lock)
        {
            if (_clientSlot.TryGetValue(clientId, out var idx)) return idx;
            // 防无限增长：只 POST /chat 不建 SSE 连接的 clientId 会永久占用字典（且占槽位）。
            // 绑定总数达上限后不再写入新 clientId，按无 clientId 处理（固定槽 0，不占字典）。
            if (_clientSlot.Count >= Global.MaxClientSlotEntries) return 0;
            // 分配空闲槽位：跳过已被其他客户端绑定、或已建 Agent / 忙碌的槽位，全满则回退 0。
            // 不能只看 Agent/IsBusy——新客户端分配后不会立刻建 Agent，否则多个页面会被分到同一个槽位互相干扰。
            var occupied = new bool[SlotCount];
            foreach (var kv in _clientSlot)
                if (kv.Value >= 0 && kv.Value < SlotCount) occupied[kv.Value] = true;
            for (int i = 0; i < SlotCount; i++)
                occupied[i] = occupied[i] || _slots[i].Agent != null || _slots[i].IsBusy;
            int free = PickFreeSlot(occupied, SlotCount);
            _clientSlot[clientId] = free;
            return free;
        }
    }

    /// <summary>显式绑定客户端到指定槽位（前端点槽位切换时调用）。同步更新已建 SSE 客户端的槽位，否则切换后输出仍路由到旧槽位。返回 false 表示目标槽位已被其他页面占用。</summary>
    private bool BindClientSlot(string? clientId, int idx)
    {
        // 无 client（curl/测试等）不参与槽位绑定，放行（保持旧行为：/slot 返回目标槽位历史）
        if (string.IsNullOrEmpty(clientId)) return true;
        if (idx < 0 || idx >= SlotCount) return false;
        lock (_lock)
        {
            // 绑定总数上限：超过 MaxClientSlotEntries 拒绝新 clientId（与 ResolveSlot 一致，防字典无限增长）
            if (!_clientSlot.ContainsKey(clientId) && _clientSlot.Count >= Global.MaxClientSlotEntries)
                return false;
            // 拒绝绑定到已被其他客户端占用的槽位（每页面一个槽位、互不干扰）；
            // 两个页面都切到同一槽位会让 BroadcastTo 同时发给两者，互相看到对方对话。
            foreach (var kv in _clientSlot)
            {
                if (kv.Value == idx && !string.Equals(kv.Key, clientId, StringComparison.Ordinal))
                    return false;
            }
            _clientSlot[clientId] = idx;
            // 关键：该页面已建立的 SSE 连接（SseClient）也要改 SlotIndex，
            // 否则 BroadcastTo(新槽位) 永远匹配不到它 → 页面收不到 token/停止态，表现为「切换后停止按钮失效」。
            foreach (var c in _clients)
            {
                if (c.ClientId == clientId) c.SlotIndex = idx;
            }
            return true;
        }
    }
}
