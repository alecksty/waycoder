using System.Collections.Concurrent;
using System.Text;
using WayCoder.Tools;
using WayCoder.UI;

namespace WayCoder.Web;

/// <summary>
/// 浏览器聊天桥接层：把 <see cref="Agent.ChatAsync"/> 的流式回调（onToken/onTool/onToolOutput）
/// 转为 SSE 事件广播给浏览器，接收浏览器 POST 的输入入队，支持中断。
/// 对标 DeepSeek Harness Web UI：多槽位（F1-F10）、换模型、输 key、设置、黑白主题。
/// </summary>
public sealed class WebChatServer : UxHelper.IWebInteraction
{
    private const int SlotCount = 10;

    /// <summary>SSE 长连接上限（防连接/线程耗尽）。</summary>
    public const int MaxSseClients = 16;

    /// <summary>待处理输入队列上限（防内存耗尽，超出返回 429）。</summary>
    public const int MaxPendingInput = 100;

    private readonly HttpServer _server;
    private readonly Agent?[] _slots = new Agent?[SlotCount];
    private int _activeSlot;
    private readonly ConcurrentQueue<(int Slot, string Input)> _input = new();
    private readonly object _lock = new();
    private readonly List<SseClient> _clients = new();
    private readonly CancellationTokenSource _serverCts = new();
    private CancellationTokenSource? _roundCts;
    private Task? _loopTask;

    /// <summary>Web 交互桥：requestId → 等待中的提问应答源。</summary>
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingAnswers = new();
    private int _answerId;

    /// <summary>SSE 客户端（写失败 = 断开）。</summary>
    private sealed class SseClient
    {
        public StreamWriter Writer = null!;
        public readonly TaskCompletionSource Closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public WebChatServer(Agent agent, int port)
    {
        _slots[0] = agent;
        _server = new HttpServer(port);
    }

    /// <summary>实际绑定端口（传入 0 时由系统分配）。</summary>
    public int Port => _server.ActualPort;

    public void Start()
    {
        _server.OnRequest = HandleRequest;
        _server.OnSse = HandleSseAsync;
        _server.Start();
        UxHelper.WebInteraction = this;
        _loopTask = Task.Run(() => MainLoopAsync(_serverCts.Token));
    }

    public void Stop()
    {
        try { _serverCts.Cancel(); } catch { }
        try { _roundCts?.Cancel(); } catch { }
        UxHelper.WebInteraction = null;
        _server.Stop();
    }

    // ═══════════════════════════════════════════════════════════
    //  路由
    // ═══════════════════════════════════════════════════════════

    private HttpResponse? HandleRequest(HttpRequest req)
    {
        // CSRF 防护：状态变更请求（非 GET）必须来自本服务来源。
        // 浏览器跨源 fetch 必带 Origin（攻击者域名），curl/SSE/同源导航不带 Origin 放行。
        if (req.Method != "GET" && !IsTrustedOrigin(req.Header("Origin"), Port))
            return new HttpResponse { Status = 403, Reason = "Forbidden", Body = Encoding.UTF8.GetBytes("403 Forbidden") };

        // 页面
        if (req.Method == "GET" && req.Path == "/")
            return HttpResponse.Html(Html);

        // 聊天
        if (req.Method == "POST" && req.Path == "/chat")
        {
            if (InputQueueFull(_input.Count))
                return new HttpResponse { Status = 429, Reason = "Too Many Requests", Body = Encoding.UTF8.GetBytes("429 Too Many Requests") };
            if (!string.IsNullOrWhiteSpace(req.Body)) _input.Enqueue((_activeSlot, req.Body));
            return HttpResponse.Text("ok");
        }
        if (req.Method == "POST" && req.Path == "/interrupt")
        {
            Interrupt();
            return HttpResponse.Text("ok");
        }
        if (req.Method == "GET" && req.Path == "/history")
            return HttpResponse.JsonBody(SerializeHistory(_slots[_activeSlot]!));

        // 模型 / 状态
        if (req.Method == "GET" && req.Path == "/models")
            return HttpResponse.JsonBody(SerializeModels());
        if (req.Method == "GET" && req.Path == "/state")
            return HttpResponse.JsonBody(SerializeState(_activeSlot, _slots));

        // 槽位切换
        if (req.Method == "POST" && req.Path == "/slot")
        {
            var body = Json.Parse(req.Body);
            var idx = body != null ? (int)Math.Round(body["slot"]?.AsNumber() ?? -1) : -1;
            if (idx < 0 || idx >= SlotCount)
                return HttpResponse.JsonBody(Err("槽位索引须在 0~9 之间"));
            SwitchSlot(idx);
            return HttpResponse.JsonBody(SerializeHistory(_slots[idx]!));
        }

        // 换模型
        if (req.Method == "POST" && req.Path == "/model")
        {
            var body = Json.Parse(req.Body);
            var modelId = body?["modelId"]?.AsString() ?? "";
            var apiKey = body?["apiKey"]?.AsString();
            if (string.IsNullOrWhiteSpace(modelId))
                return HttpResponse.JsonBody(Err("缺少 modelId"));
            Interrupt();
            var agent = EnsureSlot(_activeSlot);
            var error = ApplyModel(agent, modelId, apiKey);
            if (error != null) return HttpResponse.JsonBody(Err(error));
            Broadcast("state", SerializeState(_activeSlot, _slots));
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
            SetProviderKey(providerId, apiKey);
            Broadcast("state", SerializeState(_activeSlot, _slots));
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
            var ok = Config.TrySetPropValue(key, value, out var error);
            if (!ok) return HttpResponse.JsonBody(Err(error ?? "设置失败"));
            Config.Instance.SaveToEnvFile();
            Broadcast("state", SerializeState(_activeSlot, _slots));
            return HttpResponse.JsonBody(Ok());
        }

        // 权限模式切换（Web 版从「强制 YOLO」改为用户可选）
        if (req.Method == "POST" && req.Path == "/perm")
        {
            var body = Json.Parse(req.Body);
            var mode = body?["mode"]?.AsString() ?? "";
            if (string.IsNullOrWhiteSpace(mode))
                return HttpResponse.JsonBody(Err("缺少 mode"));
            PermissionManager.SetMode(mode);
            Broadcast("state", SerializeState(_activeSlot, _slots));
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

            // 中断副作用在路由层执行（需访问实例 _roundCts）
            if (cmdLower is "/interrupt" or "/stop")
            {
                Interrupt();
                return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("handled", true).Set("output", "⏹ 已请求中断").ToJson());
            }

            var (handled, output) = HandleCommand(trimmed, _slots[_activeSlot]);
            if (!handled)
                return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("handled", false).ToJson());

            // 副作用命令（清空/加载会话）刷新历史与会话列表
            if (cmdLower is "/reset" or "/clear" or "/session")
            {
                var agent = _slots[_activeSlot];
                if (agent != null)
                {
                    Broadcast("history", SerializeHistory(agent));
                    Broadcast("sessions", SerializeSessions());
                    Broadcast("state", SerializeState(_activeSlot, _slots));
                }
            }

            return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("handled", true).Set("output", output).ToJson());
        }

        // 右栏信息面板
        if (req.Method == "GET" && req.Path == "/panel")
            return HttpResponse.JsonBody(SerializePanel(_activeSlot, _slots));

        // 会话记录
        if (req.Method == "GET" && req.Path == "/sessions")
            return HttpResponse.JsonBody(SerializeSessions());
        if (req.Method == "POST" && req.Path == "/sessions/new")
        {
            var agent = EnsureSlot(_activeSlot);
            var id = SessionManager.SaveSession(agent.Messages, agent.LlmClient.Model);
            Broadcast("sessions", SerializeSessions());
            return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("id", id).ToJson());
        }
        if (req.Method == "POST" && req.Path == "/sessions/load")
        {
            var body = Json.Parse(req.Body);
            var id = body?["id"]?.AsString() ?? "";
            if (string.IsNullOrWhiteSpace(id))
                return HttpResponse.JsonBody(Err("缺少会话 id"));
            var loaded = SessionManager.LoadSession(id);
            if (loaded == null)
                return HttpResponse.JsonBody(Err("会话不存在"));
            Interrupt();
            var agent = EnsureSlot(_activeSlot);
            agent.Messages.Clear();
            agent.Messages.AddRange(loaded.Value.Messages);
            if (!string.IsNullOrWhiteSpace(loaded.Value.Model))
                agent.LlmClient.Model = loaded.Value.Model;
            Broadcast("history", SerializeHistory(agent));
            Broadcast("state", SerializeState(_activeSlot, _slots));
            return HttpResponse.JsonBody(Ok());
        }
        if (req.Method == "POST" && req.Path == "/sessions/delete")
        {
            var body = Json.Parse(req.Body);
            var id = body?["id"]?.AsString() ?? "";
            if (string.IsNullOrWhiteSpace(id))
                return HttpResponse.JsonBody(Err("缺少会话 id"));
            SessionManager.DeleteSession(id);
            Broadcast("sessions", SerializeSessions());
            return HttpResponse.JsonBody(Ok());
        }
        if (req.Method == "POST" && req.Path == "/sessions/rename")
        {
            var body = Json.Parse(req.Body);
            var id = body?["id"]?.AsString() ?? "";
            var newId = body?["newId"]?.AsString() ?? "";
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(newId))
                return HttpResponse.JsonBody(Err("缺少 id 或 newId"));
            if (!SessionManager.RenameSession(id, newId))
                return HttpResponse.JsonBody(Err("重命名失败"));
            Broadcast("sessions", SerializeSessions());
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

        return null;
    }

    // ═══════════════════════════════════════════════════════════
    //  槽位管理
    // ═══════════════════════════════════════════════════════════

    /// <summary>惰性创建槽位 Agent（每槽位独立 LLM + 历史），复用全局模型/密钥配置。</summary>
    private Agent EnsureSlot(int idx)
    {
        if (_slots[idx] == null)
        {
            var cfg = Config.Instance;
            var info = ModelCatalog.Find(cfg.Model);
            var providerId = info?.ProviderId ?? cfg.Provider;
            var key = ApiKeyStore.Get(providerId) ?? cfg.ApiKey;
            var baseUrl = ResolveBaseUrl(info, providerId, cfg.BaseUrl);
            var llm = new LLM(cfg.Model, key, baseUrl, cfg.MaxTokens, cfg.Temperature)
            {
                SmallModel = cfg.SmallModel,
            };
            _slots[idx] = new Agent(llm,
                maxContextTokens: ModelCatalog.ResolveContextWindow(cfg.Model, cfg.MaxContextTokens),
                maxBudgetUsd: cfg.MaxBudgetUsd, autoCommit: cfg.AutoGitCommit);
        }
        return _slots[idx]!;
    }

    private void SwitchSlot(int idx)
    {
        if (idx < 0 || idx >= SlotCount) return;
        Interrupt();
        _activeSlot = idx;
        var agent = EnsureSlot(idx);
        Broadcast("history", SerializeHistory(agent));
        Broadcast("state", SerializeState(_activeSlot, _slots));
    }

    // ═══════════════════════════════════════════════════════════
    //  换模型 / 换 key
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 运行时换模型：更新 Config + 重配置当前 Agent 的 LLM（key/baseUrl/Model/上下文窗口）+ 持久化。
    /// 返回 null=成功，否则返回错误信息。纯逻辑，便于自测。
    /// </summary>
    public static string? ApplyModel(Agent agent, string modelId, string? apiKey)
    {
        var info = ModelCatalog.Find(modelId);
        if (info == null) return $"未知模型「{modelId}」";

        var providerId = info.ProviderId;

        // key 解析：显式 apiKey > ApiKeyStore(provider) > 全局 Config.ApiKey
        string key;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            key = apiKey.Trim();
            ApiKeyStore.Set(providerId, key);
        }
        else
        {
            key = ApiKeyStore.Get(providerId) ?? Config.Instance.ApiKey;
        }

        var cfg = Config.Instance;
        var baseUrl = ResolveBaseUrl(info, providerId, cfg.BaseUrl);

        cfg.Model = modelId;
        cfg.Provider = providerId;
        if (baseUrl != null) cfg.BaseUrl = baseUrl;

        agent.LlmClient.Reconfigure(key, baseUrl);
        agent.LlmClient.Model = modelId;
        agent.UpdateContextWindow(ModelCatalog.ResolveContextWindow(modelId, cfg.MaxContextTokens));
        cfg.SaveToEnvFile();
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
        // 若当前槽位模型属于该供应商，同步重配 key
        var agent = _slots[_activeSlot];
        if (agent != null)
        {
            var cur = ModelCatalog.Find(agent.LlmClient.Model);
            if (cur != null && cur.ProviderId == providerId)
            {
                var baseUrl = ResolveBaseUrl(cur, providerId, Config.Instance.BaseUrl);
                agent.LlmClient.Reconfigure(apiKey.Trim(), baseUrl);
            }
        }
    }

    /// <summary>BaseUrl 优先级：模型目录默认 Url > 全局 Config.BaseUrl（与 AgentSlotConfig.ResolveBaseUrl 一致）。</summary>
    private static string? ResolveBaseUrl(ModelCatalog.ModelInfo? info, string providerId, string? globalBaseUrl)
    {
        if (info?.DefaultBaseUrl != null) return info.DefaultBaseUrl;
        if (ModelCatalog.Providers.TryGetValue(providerId, out var p) && !string.IsNullOrEmpty(p.DefaultBaseUrl))
            return p.DefaultBaseUrl;
        return globalBaseUrl;
    }

    // ═══════════════════════════════════════════════════════════
    //  Agent 桥接
    // ═══════════════════════════════════════════════════════════

    private void Interrupt()
    {
        try { _roundCts?.Cancel(); } catch { }
    }

    private async Task MainLoopAsync(CancellationToken serverToken)
    {
        while (!serverToken.IsCancellationRequested)
        {
            if (_input.TryDequeue(out var item))
            {
                var (slot, userInput) = item;
                if (slot != _activeSlot) SwitchSlot(slot);
                var agent = EnsureSlot(slot);
                _roundCts = new CancellationTokenSource();
                var token = _roundCts.Token;
                try
                {
                    var final = await agent.ChatAsync(
                        userInput,
                        onToken: t => Broadcast("token", JsonStr(t)),
                        onTool: (name, brief) => Broadcast("tool", JsonTool(name, brief)),
                        onToolOutput: o => Broadcast("tool_output", JsonStr(o)),
                        cancellationToken: token);
                    Broadcast("done", JsonStr(final));
                }
                catch (OperationCanceledException)
                {
                    Broadcast("interrupted", "null");
                }
                catch (Exception ex)
                {
                    Broadcast("failed", JsonStr(ex.Message));
                }
                finally
                {
                    _roundCts.Dispose();
                    _roundCts = null;
                }
            }
            else
            {
                try { await Task.Delay(50, serverToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    private async Task HandleSseAsync(StreamWriter writer)
    {
        var client = new SseClient { Writer = writer };
        lock (_lock)
        {
            if (SseClientsFull(_clients.Count))
            {
                try { writer.Write(HttpServer.SseEvent("failed", "\"连接数已达上限\"")); } catch { }
                return; // 拒绝超出上限的 SSE 连接（writer 由调用方 WriteSseAsync 的 using 释放）
            }
            _clients.Add(client);
        }
        try
        {
            // 连接即回放历史 + 状态，前端初始化渲染
            writer.Write(HttpServer.SseEvent("history", SerializeHistory(_slots[_activeSlot]!)));
            writer.Write(HttpServer.SseEvent("state", SerializeState(_activeSlot, _slots)));
            await client.Closed.Task;
        }
        catch { /* 连接断开 */ }
        finally
        {
            lock (_lock) _clients.Remove(client);
            try { writer.Dispose(); } catch { }
        }
    }

    private void Broadcast(string type, string dataJson)
    {
        var sse = HttpServer.SseEvent(type, dataJson);
        List<SseClient> snapshot;
        lock (_lock) snapshot = _clients.ToList();
        foreach (var c in snapshot)
        {
            try { c.Writer.Write(sse); }
            catch { c.Closed.TrySetResult(); }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  序列化（纯函数，便于自测）
    // ═══════════════════════════════════════════════════════════

    /// <summary>序列化模型目录（前端按 provider 分组下拉）。</summary>
    public static string SerializeModels()
    {
        var arr = JNode.Array();
        foreach (var m in ModelCatalog.All)
        {
            arr.Add(JNode.Object()
                .Set("id", m.Id)
                .Set("name", m.DisplayName)
                .Set("provider", m.Provider)
                .Set("providerId", m.ProviderId)
                .Set("category", m.Category)
                .Set("context", m.ContextWindow)
                .Set("inputPrice", m.InputPrice)
                .Set("outputPrice", m.OutputPrice)
                .Set("hasKey", ProviderHasKey(m.ProviderId)));
        }
        return arr.ToJson();
    }

    /// <summary>供应商是否有可用 key（local/custom 无需 key）。</summary>
    public static bool ProviderHasKey(string providerId)
    {
        if (providerId is "local" or "custom") return true;
        if (!string.IsNullOrEmpty(ApiKeyStore.Get(providerId))) return true;
        if (!string.IsNullOrEmpty(Config.Instance.ApiKey)) return true;
        return false;
    }

    /// <summary>序列化当前会话状态（活跃槽位、各槽位模型/是否有历史、当前模型/供应商、是否有 key）。</summary>
    public static string SerializeState(int activeSlot, Agent?[] slots)
    {
        var cfg = Config.Instance;
        var info = ModelCatalog.Find(cfg.Model);
        var providerId = info?.ProviderId ?? cfg.Provider;
        var hasKey = !string.IsNullOrEmpty(ApiKeyStore.Get(providerId))
                     || !string.IsNullOrEmpty(cfg.ApiKey)
                     || providerId is "local" or "custom";

        var slotArr = JNode.Array();
        for (int i = 0; i < slots.Length; i++)
        {
            var a = slots[i];
            slotArr.Add(JNode.Object()
                .Set("slot", i)
                .Set("model", a?.LlmClient.EffectiveModel ?? "")
                .Set("hasHistory", a != null && HasHistory(a)));
        }

        return JNode.Object()
            .Set("activeSlot", activeSlot)
            .Set("model", cfg.Model)
            .Set("provider", providerId)
            .Set("providerName", ModelCatalog.Providers.TryGetValue(providerId, out var p) ? p.DisplayName : providerId)
            .Set("hasKey", hasKey)
            .Set("permMode", PermissionManager.CurrentMode.ToString().ToLowerInvariant())
            .Set("slots", slotArr)
            .ToJson();
    }

    /// <summary>序列化设置面板数据（SettingSchema 按 Category 分组，secret 显示 masked）。</summary>
    public static string SerializeSettings()
    {
        var schema = Config.SettingSchema();
        var groups = new List<(string Category, JNode Items)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var s in schema.OrderBy(s => s.Category).ThenBy(s => s.Order))
        {
            if (!seen.Add(s.Key)) continue; // 去重（防御重复 Key）
            var value = Config.GetPropValue(s.Key) ?? "";
            // secret 类型不泄露明文
            if (s.Type == "secret")
                value = string.IsNullOrEmpty(value) ? "" : "••••••••";

            JNode? items = null;
            foreach (var g in groups)
            {
                if (g.Category == s.Category) { items = g.Items; break; }
            }
            if (items == null)
            {
                items = JNode.Array();
                groups.Add((s.Category, items));
            }
            items.Add(JNode.Object()
                .Set("key", s.Key)
                .Set("label", s.Label)
                .Set("desc", s.Desc)
                .Set("type", s.Type)
                .Set("options", OptionsToJson(s.Options))
                .Set("value", value));
        }

        var groupArr = JNode.Array();
        foreach (var (cat, items) in groups)
        {
            groupArr.Add(JNode.Object().Set("category", cat).Set("items", items));
        }
        return groupArr.ToJson();
    }

    private static JNode OptionsToJson(string[]? options)
    {
        var arr = JNode.Array();
        if (options != null)
            foreach (var o in options) arr.Add(o);
        return arr;
    }

    /// <summary>序列化指定 Agent 的历史消息（role=user/assistant 且内容非空）。</summary>
    public static string SerializeHistory(Agent agent)
    {
        var arr = JNode.Array();
        foreach (var m in agent.Messages)
        {
            var role = m["role"]?.AsString();
            if (role != "user" && role != "assistant") continue;
            var content = m["content"]?.AsString();
            if (string.IsNullOrEmpty(content)) continue;
            arr.Add(JNode.Object().Set("role", role).Set("content", content));
        }
        return arr.ToJson();
    }

    private static bool HasHistory(Agent agent)
    {
        foreach (var m in agent.Messages)
        {
            var role = m["role"]?.AsString();
            if (role != "user" && role != "assistant") continue;
            if (!string.IsNullOrEmpty(m["content"]?.AsString())) return true;
        }
        return false;
    }

    /// <summary>序列化右栏信息面板（任务/token/费用/修改文件/MCP/LSP）。纯静态便于自测。</summary>
    public static string SerializePanel(int activeSlot, Agent?[] slots)
    {
        // ── 任务（全局共享）──
        var todos = JNode.Array();
        try
        {
            foreach (var t in TodoTool.Items)
            {
                var deps = JNode.Array();
                foreach (var d in t.DependsOn) deps.Add(d);
                todos.Add(JNode.Object()
                    .Set("id", t.Id)
                    .Set("title", t.Title)
                    .Set("status", t.Status)
                    .Set("deps", deps));
            }
        }
        catch { /* todo 读取失败不阻塞面板 */ }

        // ── token / 费用（当前活跃槽位实例级）──
        var llm = (activeSlot >= 0 && activeSlot < slots.Length) ? slots[activeSlot]?.LlmClient : null;
        var tokens = JNode.Object();
        if (llm != null)
        {
            tokens.Set("totalPrompt", llm.TotalPromptTokens)
                  .Set("totalCompletion", llm.TotalCompletionTokens)
                  .Set("taskPrompt", llm.TaskPromptTokens)
                  .Set("taskCompletion", llm.TaskCompletionTokens)
                  .Set("totalRequests", llm.TotalRequests)
                  .Set("tokensPerSec", llm.LastTokensPerSec);
        }

        var cost = JNode.Object();
        if (llm != null)
        {
            cost.Set("task", llm.TaskCost.HasValue ? JNode.Num(llm.TaskCost.Value) : JNode.Null());
            cost.Set("estimated", llm.EstimatedCost.HasValue ? JNode.Num(llm.EstimatedCost.Value) : JNode.Null());
        }

        // ── 修改文件（全局共享）──
        var files = JNode.Array();
        try
        {
            foreach (var f in EditFileTool.ChangedFiles.ToList()) files.Add(f);
        }
        catch { }

        // ── MCP 服务器（全局共享）──
        var mcp = JNode.Array();
        try
        {
            foreach (var s in McpManager.Servers)
            {
                mcp.Add(JNode.Object()
                    .Set("name", s.Name)
                    .Set("transport", s.Transport)
                    .Set("status", s.Status.ToString().ToLowerInvariant())
                    .Set("toolCount", s.ToolCount)
                    .Set("resourceCount", s.ResourceCount)
                    .Set("promptCount", s.PromptCount)
                    .Set("error", s.Error));
            }
        }
        catch { }

        // ── LSP 会话（全局共享）──
        var lsp = JNode.Array();
        try
        {
            foreach (var s in LspTool.ActiveSessions)
            {
                lsp.Add(JNode.Object()
                    .Set("command", s.Command)
                    .Set("root", s.Root)
                    .Set("initialized", s.Initialized)
                    .Set("hasExited", s.HasExited));
            }
        }
        catch { }

        return JNode.Object()
            .Set("todos", todos)
            .Set("tokens", tokens)
            .Set("cost", cost)
            .Set("files", files)
            .Set("mcp", mcp)
            .Set("lsp", lsp)
            .ToJson();
    }

    /// <summary>序列化历史会话列表（左栏）。纯静态便于自测。</summary>
    public static string SerializeSessions()
    {
        var arr = JNode.Array();
        try
        {
            foreach (var s in SessionManager.ListSessions(50))
            {
                arr.Add(JNode.Object()
                    .Set("id", s.Id)
                    .Set("model", s.Model)
                    .Set("savedAt", s.SavedAt)
                    .Set("preview", s.Preview)
                    .Set("msgCount", s.MessageCount));
            }
        }
        catch { }
        return arr.ToJson();
    }

    // ═══════════════════════════════════════════════════════════
    //  Web 斜杠命令分发（纯逻辑，便于自测）
    //  覆盖 Web 有意义的命令子集；未识别返回 (false, "")，由调用方回退为普通消息。
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 分发 Web 斜杠命令。返回 (是否已处理, 输出 Markdown 文本)。
    /// /interrupt、/stop 的实际中断副作用由路由层执行（需访问实例 _roundCts）。
    /// </summary>
    public static (bool Handled, string Output) HandleCommand(string input, Agent? agent)
    {
        var text = input.Trim();
        if (!text.StartsWith('/')) return (false, "");

        var space = text.IndexOf(' ');
        var cmd = (space < 0 ? text : text[..space]).ToLowerInvariant();
        var args = space < 0 ? "" : text[(space + 1)..].Trim();

        switch (cmd)
        {
            case "/help" or "/h":
                return (true, WebHelpText());

            case "/perm" or "/permissions":
                return (true, WebPermText(args));

            case "/model":
                if (args.Equals("list", StringComparison.OrdinalIgnoreCase)
                    || args.Equals("ls", StringComparison.OrdinalIgnoreCase))
                    return (true, WebModelListText());
                return (false, ""); // /model 无参 → 前端打开模型选择窗口

            case "/reset" or "/clear":
                if (agent != null) agent.Messages.Clear();
                return (true, "🗑 已清空当前会话");

            case "/session":
                return (true, WebSessionText(args, agent));

            case "/tokens":
                return (true, WebTokensText(agent));

            case "/mcp":
                return (true, WebMcpText());

            case "/todo":
                return (true, WebTodoText());

            case "/interrupt" or "/stop":
                return (true, "⏹ 已请求中断");

            default:
                return (false, "");
        }
    }

    private static string WebHelpText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("📋 **Web 命令**");
        sb.AppendLine();
        sb.AppendLine("| 命令 | 说明 |");
        sb.AppendLine("|---|---|");
        sb.AppendLine("| /help | 显示帮助 |");
        sb.AppendLine("| /perm [ask\\|auto\\|smartauto\\|yolo] | 切换权限模式 |");
        sb.AppendLine("| /model | 打开模型选择窗口 |");
        sb.AppendLine("| /model list | 列出模型 |");
        sb.AppendLine("| /theme | 切换明暗主题 |");
        sb.AppendLine("| /settings | 打开设置 |");
        sb.AppendLine("| /reset | 清空当前会话 |");
        sb.AppendLine("| /session [list\\|save\\|load <id>] | 会话管理 |");
        sb.AppendLine("| /tokens | Token 统计 |");
        sb.AppendLine("| /mcp | MCP 服务器状态 |");
        sb.AppendLine("| /todo | 任务列表 |");
        sb.AppendLine("| /interrupt | 中断当前任务 |");
        return sb.ToString();
    }

    private static string WebPermLabel()
        => PermissionManager.CurrentMode switch
        {
            PermissionManager.Mode.Yolo => "YOLO（直接执行）",
            PermissionManager.Mode.SmartAuto => "SmartAuto（智能分级）",
            PermissionManager.Mode.Auto => "Auto（首次确认后自动）",
            _ => "Ask（每次确认）",
        };

    private static string WebPermText(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return $"当前权限模式: **{WebPermLabel()}**";
        PermissionManager.SetMode(args);
        return $"权限模式已切换: **{WebPermLabel()}**";
    }

    private static string WebFormatContext(int ctx)
        => ctx >= 1024 ? $"{Math.Round(ctx / 1024.0)}k" : ctx.ToString();

    private static string WebModelListText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("🧠 **模型列表**");
        sb.AppendLine();
        sb.AppendLine("| 模型 | 供应商 | 上下文 |");
        sb.AppendLine("|---|---|---|");
        foreach (var m in ModelCatalog.All)
            sb.AppendLine($"| {m.DisplayName} | {m.ProviderId} | {WebFormatContext(m.ContextWindow)} |");
        return sb.ToString();
    }

    private static string WebSessionText(string args, Agent? agent)
    {
        var parts = args.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var sub = parts.Length > 0 ? parts[0].ToLowerInvariant() : "list";
        var rest = parts.Length > 1 ? parts[1].Trim() : "";

        switch (sub)
        {
            case "save":
                if (agent == null) return "⚠ 无活跃槽位";
                var id = SessionManager.SaveSession(agent.Messages, agent.LlmClient.Model);
                return $"💾 会话已保存: **{id}**";

            case "load":
                if (string.IsNullOrWhiteSpace(rest)) return "用法: /session load <会话ID>";
                var loaded = SessionManager.LoadSession(rest);
                if (loaded == null) return $"❌ 会话不存在: {rest}";
                if (agent == null) return "⚠ 无活跃槽位";
                agent.Messages.Clear();
                agent.Messages.AddRange(loaded.Value.Messages);
                return $"📂 已加载会话: **{rest}**（{loaded.Value.Messages.Count} 条消息）";

            case "list":
            default:
                var sessions = SessionManager.ListSessions(20);
                if (sessions.Count == 0) return "📂 没有已保存的会话";
                var sb = new StringBuilder();
                sb.AppendLine($"📂 **已保存的会话**（{sessions.Count} 条）");
                sb.AppendLine();
                foreach (var s in sessions)
                    sb.AppendLine($"- `{s.Id}` · {s.Model} · {s.SavedAt}");
                return sb.ToString();
        }
    }

    private static string WebTokensText(Agent? agent)
    {
        var llm = agent?.LlmClient;
        if (llm == null) return "⚠ 无活跃槽位";
        var sb = new StringBuilder();
        sb.AppendLine("💰 **Token 统计**");
        sb.AppendLine();
        sb.AppendLine($"- 本轮：prompt {llm.TaskPromptTokens} / completion {llm.TaskCompletionTokens}");
        sb.AppendLine($"- 累计：prompt {llm.TotalPromptTokens} / completion {llm.TotalCompletionTokens}");
        sb.AppendLine($"- 请求数：{llm.TotalRequests}");
        if (llm.LastTokensPerSec > 0) sb.AppendLine($"- 速率：{llm.LastTokensPerSec:F1} tok/s");
        if (llm.TaskCost.HasValue) sb.AppendLine($"- 本轮费用：${llm.TaskCost.Value:F4}");
        return sb.ToString();
    }

    private static string WebMcpText()
    {
        var servers = McpManager.Servers;
        if (servers.Count == 0) return "🔌 未配置 MCP 服务器";
        var sb = new StringBuilder();
        sb.AppendLine("🔌 **MCP 服务器**");
        sb.AppendLine();
        foreach (var s in servers)
        {
            var icon = s.Status == McpServerStatus.Connected ? "🟢"
                : s.Status == McpServerStatus.Connecting ? "🟡" : "🔴";
            sb.AppendLine($"- {icon} `{s.Name}`（{s.Transport}）· {s.ToolCount} 工具");
            if (!string.IsNullOrEmpty(s.Error)) sb.AppendLine($"  - ⚠ {s.Error}");
        }
        return sb.ToString();
    }

    private static string WebTodoText()
    {
        var items = TodoTool.Items;
        if (items.Count == 0) return "📋 无任务";
        var sb = new StringBuilder();
        sb.AppendLine("📋 **任务列表**");
        sb.AppendLine();
        foreach (var t in items)
            sb.AppendLine($"- `{t.Status}` {t.Title}");
        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════
    //  Web 交互桥（UxHelper.IWebInteraction）
    //  生成 requestId → 广播 SSE "ask" → 等待 POST /answer 应答
    // ═══════════════════════════════════════════════════════════

    private string NextId() => Interlocked.Increment(ref _answerId).ToString();

    /// <summary>文本输入。</summary>
    public Task<string?> AskAsync(string prompt, string? defaultValue, int timeoutMs)
    {
        var payload = JNode.Object()
            .Set("requestId", NextId())
            .Set("kind", "text")
            .Set("title", prompt)
            .Set("default", defaultValue);
        return WaitAnswerAsync(payload, timeoutMs);
    }

    /// <summary>单选。</summary>
    public Task<string?> SelectAsync(string title, List<string> choices, int timeoutMs)
    {
        var payload = JNode.Object()
            .Set("requestId", NextId())
            .Set("kind", "select")
            .Set("title", title)
            .Set("choices", StringArray(choices));
        return WaitAnswerAsync(payload, timeoutMs);
    }

    /// <summary>多选。</summary>
    public Task<List<string>?> MultiSelectAsync(string title, List<string> choices, int timeoutMs)
    {
        var payload = JNode.Object()
            .Set("requestId", NextId())
            .Set("kind", "multi")
            .Set("title", title)
            .Set("choices", StringArray(choices));
        return WaitAnswerMultiAsync(payload, timeoutMs);
    }

    private static JNode StringArray(List<string> items)
    {
        var arr = JNode.Array();
        foreach (var s in items) arr.Add(s);
        return arr;
    }

    /// <summary>确认框。返回 0=是 1=总是允许 2=否（与 UxHelper.Confirm 对齐）。</summary>
    public async Task<int> ConfirmAsync(string title, string message, bool allowAll, int timeoutMs)
    {
        var payload = JNode.Object()
            .Set("requestId", NextId())
            .Set("kind", "confirm")
            .Set("title", title)
            .Set("message", message)
            .Set("allowAll", allowAll);
        var result = await WaitAnswerAsync(payload, timeoutMs);
        // 前端回传字符串 "0"/"1"/"2" 或 "yes"/"all"/"no"
        return result switch
        {
            "0" or "yes" => 0,
            "1" or "all" => 1,
            _ => 2,
        };
    }

    /// <summary>Diff 预览：广播逐 hunk diff，等待用户返回「接受/拒绝/部分接受」。超时/取消返回 null（视为拒绝）。</summary>
    public async Task<DiffConfirmResult?> DiffConfirmAsync(string filePath, List<DiffPreview.Hunk> hunks, int timeoutMs)
    {
        var payload = JNode.Object()
            .Set("requestId", NextId())
            .Set("kind", "diff")
            .Set("title", $"Diff 预览: {filePath}")
            .Set("hunks", Json.Parse(SerializeHunks(hunks)) ?? JNode.Array());
        var raw = await WaitAnswerAsync(payload, timeoutMs);
        return ParseDiffAnswer(raw);
    }

    /// <summary>把 hunk 列表序列化为前端可渲染的 JSON 数组。纯逻辑便于自测。</summary>
    public static string SerializeHunks(List<DiffPreview.Hunk> hunks)
    {
        var arr = JNode.Array();
        foreach (var h in hunks)
        {
            var lines = JNode.Array();
            foreach (var l in h.Lines)
            {
                lines.Add(JNode.Object()
                    .Set("kind", l.Kind.ToString())
                    .Set("text", l.Text)
                    .Set("oldLine", l.OldLine)
                    .Set("newLine", l.NewLine));
            }
            arr.Add(JNode.Object()
                .Set("header", h.Header)
                .Set("lines", lines));
        }
        return arr.ToJson();
    }

    /// <summary>
    /// 解析 diff 确认应答。应答为 JSON 字符串：{"decision":"accept|reject|partial","accepted":[索引]}。
    /// 纯逻辑便于自测；null/空/非法 → null（调用方视为拒绝）。
    /// </summary>
    public static DiffConfirmResult? ParseDiffAnswer(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        if (!Json.TryParse(json, out var node) || node == null) return null;

        var decision = node["decision"]?.AsString() ?? "";
        var result = new DiffConfirmResult();
        switch (decision)
        {
            case "accept":
                result.Decision = DiffPreview.Decision.AcceptAll;
                break;
            case "partial":
                result.Decision = DiffPreview.Decision.Partial;
                var acc = node["accepted"];
                if (acc != null && acc.Kind == JKind.Array)
                {
                    var set = new HashSet<int>();
                    foreach (var item in acc.Items)
                        if (item.Kind == JKind.Number) set.Add((int)Math.Round(item.AsNumber()));
                    result.AcceptedHunks = set;
                }
                break;
            default:
                result.Decision = DiffPreview.Decision.RejectAll;
                break;
        }
        return result;
    }

    /// <summary>广播提问并等待应答。超时返回 null（调用方视为取消/拒绝）。</summary>
    private async Task<string?> WaitAnswerAsync(JNode payload, int timeoutMs)
    {
        var id = payload["requestId"]!.AsString()!;
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingAnswers[id] = tcs;
        try
        {
            Broadcast("ask", payload.ToJson());
            var delay = Task.Delay(timeoutMs > 0 ? timeoutMs : 60_000);
            var winner = await Task.WhenAny(tcs.Task, delay);
            if (winner == delay) return null; // 超时
            return await tcs.Task;
        }
        finally
        {
            _pendingAnswers.TryRemove(id, out _);
        }
    }

    /// <summary>多选：应答为逗号分隔的选中项，拆成列表返回。</summary>
    private async Task<List<string>?> WaitAnswerMultiAsync(JNode payload, int timeoutMs)
    {
        var result = await WaitAnswerAsync(payload, timeoutMs);
        if (result == null) return null;
        if (result.Length == 0) return new List<string>();
        return result.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    /// <summary>处理 POST /answer：把应答回填给对应提问，返回 JSON 结果。</summary>
    private string AnswerQuestion(string requestId, JNode? value)
    {
        if (!_pendingAnswers.TryGetValue(requestId, out var tcs))
            return Err("提问已超时或不存在");
        string answer;
        if (value == null || value.Kind == JKind.Null)
            answer = ""; // 空 = 取消
        else if (value.Kind == JKind.Array)
            answer = string.Join("\n", value.Items.Select(i => i.AsString() ?? ""));
        else
            answer = value.AsString() ?? "";
        tcs.TrySetResult(answer);
        return Ok();
    }

    // ═══════════════════════════════════════════════════════════
    //  JSON 辅助
    // ═══════════════════════════════════════════════════════════

    private static string JsonStr(string s) => JNode.Str(s).ToJson();

    /// <summary>校验 Origin 是否为本服务合法来源（CSRF 防护）。纯逻辑便于自测。</summary>
    public static bool IsTrustedOrigin(string? origin, int port)
    {
        if (string.IsNullOrEmpty(origin)) return true; // 非浏览器客户端（curl/SSE/同源导航）放行
        return origin.Equals($"http://127.0.0.1:{port}", StringComparison.OrdinalIgnoreCase)
            || origin.Equals($"http://localhost:{port}", StringComparison.OrdinalIgnoreCase);
    }

    private static string JsonTool(string name, string brief)
        => JNode.Object().Set("name", HtmlEscape(name)).Set("args", HtmlEscape(brief)).ToJson();

    /// <summary>SSE 客户端是否已满（纯逻辑，便于自测）。</summary>
    public static bool SseClientsFull(int count) => count >= MaxSseClients;

    /// <summary>待处理输入队列是否已满（纯逻辑，便于自测）。</summary>
    public static bool InputQueueFull(int count) => count >= MaxPendingInput;

    /// <summary>HTML 实体转义（防 XSS）：工具名/参数注入 innerHTML 前转义 &lt; &gt; &amp; " '。</summary>
    public static string HtmlEscape(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
    }

    private static string Ok() => JNode.Object().Set("ok", true).ToJson();

    private static string Err(string message) => JNode.Object().Set("ok", false).Set("error", message).ToJson();

    // ═══════════════════════════════════════════════════════════
    //  内嵌前端（单 HTML，无构建，无外部 CDN）
    //  对标 DeepSeek Harness：黑白主题 + 圆角 + 模型下拉 + F1-F10 槽位 + 设置抽屉 + key 弹窗
    // ═══════════════════════════════════════════════════════════

    internal const string Html = """
<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>WayCoder 聊天</title>
<style>
:root {
  --bg:#0f1117; --panel:#171a23; --panel2:#1d2230; --border:#262b3a; --text:#e6e8ee; --dim:#8b93a7;
  --accent:#4f8cff; --user:#1f3a5f; --tool:#2a2416; --danger:#3a2a2a; --shadow:0 4px 20px rgba(0,0,0,.4);
  --diff-del:#ff7b72; --diff-del-bg:rgba(248,81,73,.14); --diff-add:#7ee787; --diff-add-bg:rgba(46,160,67,.14);
}
[data-theme="light"] {
  --bg:#f5f6f8; --panel:#ffffff; --panel2:#f0f2f6; --border:#e2e5ec; --text:#1a1d24; --dim:#6b7280;
  --accent:#2f6bff; --user:#e3ecff; --tool:#fff4dc; --danger:#ffe2e2; --shadow:0 4px 20px rgba(0,0,0,.12);
  --diff-del:#d73a49; --diff-del-bg:rgba(255,129,130,.15); --diff-add:#1a7f37; --diff-add-bg:rgba(63,185,80,.15);
}
* { box-sizing:border-box; margin:0; padding:0; }
body { background:var(--bg); color:var(--text); font:14px/1.6 -apple-system,"PingFang SC","Microsoft YaHei",sans-serif; height:100vh; display:flex; flex-direction:column; transition:background .2s,color .2s; overflow:hidden; }
header { padding:9px 14px; border-bottom:1px solid var(--border); background:var(--panel); display:flex; align-items:center; gap:10px; flex-wrap:nowrap; }
.logo { font-weight:700; color:var(--text); font-size:15px; white-space:nowrap; }
.logo span { color:var(--accent); }
.spacer { flex:1; }
select, .btn { height:32px; border-radius:9px; border:1px solid var(--border); background:var(--panel2); color:var(--text); font:inherit; padding:0 11px; cursor:pointer; outline:none; }
select:focus { border-color:var(--accent); }
select optgroup { background:var(--panel); color:var(--text); }
.btn { display:inline-flex; align-items:center; gap:6px; font-weight:600; }
.btn:hover { border-color:var(--accent); }
.btn.ghost { background:transparent; border:none; font-size:17px; padding:0 8px; }
.btn.primary { background:var(--accent); color:#fff; border:none; }
.btn.danger { background:var(--danger); color:#ff9a9a; border:none; }

/* ── 三栏布局 ── */
.layout { flex:1; display:grid; grid-template-columns:236px minmax(0,1fr) 300px; min-height:0; }
#sidebar-left { background:var(--panel); border-right:1px solid var(--border); overflow-y:auto; display:flex; flex-direction:column; }
#sidebar-right { background:var(--panel); border-left:1px solid var(--border); overflow-y:auto; }
#chat-col { display:flex; flex-direction:column; min-width:0; min-height:0; }

.panel-head { padding:11px 14px 7px; font-size:12px; font-weight:700; color:var(--dim); text-transform:uppercase; letter-spacing:.5px; }
#slot-list { display:grid; grid-template-columns:repeat(5,1fr); gap:5px; padding:2px 12px 8px; }
.slot { height:30px; border-radius:8px; border:1px solid var(--border); background:var(--panel2); color:var(--dim); font-size:11px; cursor:pointer; display:flex; align-items:center; justify-content:center; transition:all .15s; }
.slot:hover { border-color:var(--accent); color:var(--text); }
.slot.active { background:var(--accent); border-color:var(--accent); color:#fff; font-weight:700; }
.slot.has { color:var(--text); border-color:var(--dim); }
#new-session { margin:8px 12px; }

#session-list { flex:1; overflow-y:auto; padding:0 8px; }
.session-item { padding:8px 8px; border-radius:9px; cursor:pointer; position:relative; transition:background .12s; }
.session-item:hover { background:var(--panel2); }
.session-item .preview { font-size:13px; white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }
.session-item .meta { font-size:11px; color:var(--dim); margin-top:1px; }
.session-item .ops { position:absolute; top:6px; right:6px; display:none; gap:4px; }
.session-item:hover .ops { display:flex; }
.session-item .ops button { width:22px; height:22px; border-radius:6px; border:1px solid var(--border); background:var(--panel); color:var(--dim); cursor:pointer; font-size:11px; line-height:1; }
.session-item .ops button:hover { color:var(--text); border-color:var(--accent); }
.empty { color:var(--dim); font-size:12px; padding:8px 12px; text-align:center; }

/* ── 聊天 ── */
#messages { flex:1; overflow-y:auto; padding:16px; display:flex; flex-direction:column; gap:12px; }
.msg { max-width:82%; padding:10px 15px; border-radius:14px; white-space:pre-wrap; word-break:break-word; }
.msg.user { align-self:flex-end; background:var(--user); border-bottom-right-radius:4px; }
.msg.assistant { align-self:flex-start; background:var(--panel); border:1px solid var(--border); border-bottom-left-radius:4px; }
.msg.system { align-self:center; color:var(--dim); font-size:13px; background:transparent; }
.msg.cmd { align-self:stretch; max-width:100%; background:var(--panel); border:1px solid var(--border); border-left:3px solid var(--accent); border-radius:10px; white-space:normal; }
.tool { align-self:flex-start; background:var(--tool); border:1px solid var(--border); border-radius:12px; padding:7px 13px; font-size:13px; color:var(--dim); }
.tool b { color:#e8b34b; }
.tool-output { align-self:stretch; background:var(--panel2); border:1px solid var(--border); border-radius:12px; padding:9px 13px; font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace; font-size:12px; white-space:pre-wrap; word-break:break-word; color:var(--text); max-height:320px; overflow-y:auto; }

/* ── Markdown 渲染 ── */
.msg.assistant { white-space:normal; }
.msg.assistant.streaming { white-space:pre-wrap; }
.msg h1,.msg h2,.msg h3,.msg h4,.msg h5,.msg h6 { margin:10px 0 4px; line-height:1.3; font-weight:700; }
.msg h1 { font-size:1.35em; } .msg h2 { font-size:1.25em; } .msg h3 { font-size:1.15em; } .msg h4,.msg h5,.msg h6 { font-size:1.05em; }
.msg p { margin:4px 0; }
.msg ul,.msg ol { margin:4px 0 4px 22px; }
.msg li { margin:2px 0; }
.msg blockquote { border-left:3px solid var(--accent); padding:2px 0 2px 11px; margin:6px 0; color:var(--dim); }
.msg blockquote p { margin:2px 0; }
.msg a { color:var(--accent); text-decoration:none; }
.msg a:hover { text-decoration:underline; }
.msg hr { border:none; border-top:1px solid var(--border); margin:12px 0; }
.msg .md-code { background:var(--bg); border:1px solid var(--border); border-radius:9px; padding:10px 13px; margin:8px 0; overflow-x:auto; font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace; font-size:12.5px; white-space:pre; }
.msg .md-code code { font-family:inherit; background:none; border:none; padding:0; }
.msg .md-inline { background:var(--panel2); border:1px solid var(--border); border-radius:5px; padding:0 5px; font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace; font-size:.9em; }
.msg .md-table { border-collapse:collapse; margin:8px 0; font-size:12.5px; max-width:100%; display:block; overflow-x:auto; }
.msg .md-table th,.msg .md-table td { border:1px solid var(--border); padding:5px 10px; text-align:left; white-space:normal; }
.msg .md-table th { background:var(--panel2); font-weight:700; }
#input-bar { display:flex; gap:8px; padding:11px 14px; border-top:1px solid var(--border); background:var(--panel); }
#input { flex:1; resize:none; background:var(--bg); color:var(--text); border:1px solid var(--border); border-radius:14px; padding:10px 14px; font:inherit; min-height:42px; max-height:200px; outline:none; }
#input:focus { border-color:var(--accent); }

/* ── 右栏卡片 ── */
.card { border-bottom:1px solid var(--border); padding:11px 14px; }
.card-head { font-size:12px; font-weight:700; color:var(--accent); margin-bottom:7px; }
.card .row { font-size:12.5px; padding:2px 0; display:flex; gap:6px; align-items:flex-start; }
.card .row .k { color:var(--dim); flex-shrink:0; }
.card .row .v { word-break:break-all; }
.card .item { font-size:12.5px; padding:3px 0; border-bottom:1px dashed var(--border); }
.card .item:last-child { border-bottom:none; }
.dot { display:inline-block; width:8px; height:8px; border-radius:50%; margin-right:5px; vertical-align:middle; }
.dot.pending { background:#8b93a7; }
.dot.in_progress { background:#4f8cff; }
.dot.completed { background:#3fb950; }
.dot.cancelled { background:#e5534b; }
.dot.blocked { background:#e8b34b; }
.dot.on { background:#3fb950; }
.dot.off { background:#e5534b; }
.dot.connecting { background:#e8b34b; }
.token-grid { display:grid; grid-template-columns:1fr 1fr; gap:3px 10px; font-size:12px; }
.token-grid .v { text-align:right; font-variant-numeric:tabular-nums; }

/* ── 设置窗口（两列，居中弹出）── */
#drawer { position:fixed; top:50%; left:50%; width:780px; max-width:94vw; height:82vh; max-height:92vh; background:var(--panel); border:1px solid var(--border); border-radius:16px; box-shadow:var(--shadow); transform:translate(-50%,-50%) scale(.96); opacity:0; pointer-events:none; transition:transform .2s,opacity .2s; z-index:50; display:flex; flex-direction:column; }
#drawer.open { transform:translate(-50%,-50%) scale(1); opacity:1; pointer-events:auto; }
#drawer-head { padding:13px 18px; border-bottom:1px solid var(--border); display:flex; align-items:center; }
#drawer-head b { flex:1; }
#drawer-body { flex:1; overflow:hidden; display:flex; }
#settings-nav { width:180px; border-right:1px solid var(--border); overflow-y:auto; padding:8px; }
#settings-nav .nav-item { display:block; width:100%; text-align:left; padding:9px 12px; border-radius:9px; background:transparent; border:none; color:var(--text); font:inherit; cursor:pointer; margin-bottom:3px; }
#settings-nav .nav-item:hover { background:var(--panel2); }
#settings-nav .nav-item.active { background:var(--panel2); color:var(--accent); font-weight:600; }
#settings-detail { flex:1; overflow-y:auto; padding:14px 18px 24px; }
.set-row { margin-bottom:13px; }
.set-row label { display:block; font-size:13px; color:var(--text); margin-bottom:4px; font-weight:500; }
.set-row .desc { font-size:11px; color:var(--dim); margin-bottom:5px; }
.set-row input, .set-row select { width:100%; height:33px; border-radius:9px; border:1px solid var(--border); background:var(--panel2); color:var(--text); font:inherit; padding:0 10px; outline:none; }
.set-row input:focus, .set-row select:focus { border-color:var(--accent); }
.set-row input[type="checkbox"] { width:auto; height:auto; }

/* ── 模态框（key / ask）── */
.modal { position:fixed; inset:0; background:rgba(0,0,0,.5); display:none; align-items:center; justify-content:center; z-index:60; }
.modal.open { display:flex; }
.modal-card { background:var(--panel); border:1px solid var(--border); border-radius:16px; padding:20px 22px; width:440px; max-width:92vw; max-height:86vh; overflow-y:auto; box-shadow:var(--shadow); }
.modal-card.diff { width:680px; }
.modal-card h2 { font-size:16px; margin-bottom:6px; }
.modal-card p { font-size:13px; color:var(--dim); margin-bottom:12px; }
.modal-card input[type="text"], .modal-card input[type="password"] { width:100%; height:37px; border-radius:11px; border:1px solid var(--border); background:var(--panel2); color:var(--text); font:inherit; padding:0 12px; outline:none; margin-bottom:12px; }
.modal-card input:focus { border-color:var(--accent); }
.modal-card .row { display:flex; gap:8px; justify-content:flex-end; flex-wrap:wrap; }
.ask-option { display:block; width:100%; text-align:left; padding:10px 13px; margin-bottom:7px; border-radius:10px; border:1px solid var(--border); background:var(--panel2); color:var(--text); font:inherit; cursor:pointer; }
.ask-option:hover { border-color:var(--accent); }
.ask-message { background:var(--bg); border:1px solid var(--border); border-radius:10px; padding:10px 12px; font-family:ui-monospace,Menlo,Consolas,monospace; font-size:12px; white-space:pre-wrap; word-break:break-all; margin-bottom:12px; max-height:220px; overflow-y:auto; }
.ask-multi { display:block; padding:6px 2px; font-size:13.5px; }
.ask-multi input { margin-right:8px; }
/* ── Diff 预览 ── */
.diff-hunk { border:1px solid var(--border); border-radius:10px; margin-bottom:9px; overflow:hidden; }
.diff-hunk-head { display:flex; align-items:center; gap:8px; padding:7px 11px; background:var(--panel2); font-family:ui-monospace,Menlo,Consolas,monospace; font-size:12px; color:var(--dim); cursor:pointer; }
.diff-hunk-head input { margin:0; }
.diff-hunk-lines { margin:0; padding:8px 11px; font-family:ui-monospace,Menlo,Consolas,monospace; font-size:12px; line-height:1.55; white-space:pre-wrap; word-break:break-all; background:var(--bg); }
.diff-line { display:block; }
.diff-line.del { color:var(--diff-del); background:var(--diff-del-bg); }
.diff-line.add { color:var(--diff-add); background:var(--diff-add-bg); }
.diff-line.ctx { color:var(--dim); }
/* ── 模型选择窗口 ── */
.model-card { width:560px; }
#model-search { margin-bottom:0; }
.model-group .gname { font-size:12px; color:var(--dim); font-weight:700; margin:10px 0 5px; text-transform:uppercase; letter-spacing:.4px; }
.model-item { display:flex; align-items:center; gap:8px; padding:9px 12px; border-radius:10px; border:1px solid var(--border); background:var(--panel2); cursor:pointer; margin-bottom:6px; }
.model-item:hover { border-color:var(--accent); }
.model-item.selected { border-color:var(--accent); background:var(--panel); }
.model-item .name { font-weight:600; }
.model-item .meta { font-size:11px; color:var(--dim); white-space:nowrap; margin-left:auto; }
.tag { font-size:10px; padding:1px 7px; border-radius:8px; white-space:nowrap; }
.tag.cat { background:var(--panel); border:1px solid var(--border); color:var(--dim); }
.tag.nokey { background:var(--danger); color:#ff9a9a; }
</style>
</head>
<body>
<header>
  <div class="logo">🤖 Way<span>Coder</span></div>
  <div class="spacer"></div>
  <button class="btn" id="model-btn" title="选择模型">🧠 <span id="model-btn-label">模型</span></button>
  <select id="perm-select" title="权限模式（YOLO=直接执行 / Ask=每次确认）">
    <option value="ask">🛡 Ask</option>
    <option value="auto">✅ Auto</option>
    <option value="smartauto">🧭 SmartAuto</option>
    <option value="yolo">⚡ YOLO</option>
  </select>
  <button class="btn ghost" id="theme-btn" title="切换主题">🌙</button>
  <button class="btn" id="settings-btn" title="设置">⚙ 设置</button>
</header>

<div class="layout">
  <aside id="sidebar-left">
    <div class="panel-head">🗂 槽位</div>
    <div id="slot-list"></div>
    <div class="panel-head">📜 历史会话</div>
    <button class="btn" id="new-session">＋ 新建会话</button>
    <div id="session-list"><div class="empty">加载中…</div></div>
  </aside>

  <main id="chat-col">
    <div id="messages"></div>
    <div id="input-bar">
      <textarea id="input" placeholder="输入消息，Enter 发送，Shift+Enter 换行" rows="1"></textarea>
      <button class="btn primary" id="send">发送</button>
      <button class="btn danger" id="stop">停止</button>
    </div>
  </main>

  <aside id="sidebar-right">
    <div class="card"><div class="card-head">📋 任务</div><div id="panel-todos"><div class="empty">无任务</div></div></div>
    <div class="card"><div class="card-head">💰 Token / 费用</div><div id="panel-tokens"></div></div>
    <div class="card"><div class="card-head">🔧 修改文件</div><div id="panel-files"><div class="empty">无</div></div></div>
    <div class="card"><div class="card-head">🔌 MCP 服务器</div><div id="panel-mcp"><div class="empty">未配置</div></div></div>
    <div class="card"><div class="card-head">🧠 LSP 会话</div><div id="panel-lsp"><div class="empty">无活动会话</div></div></div>
  </aside>
</div>

<div id="drawer">
  <div id="drawer-head"><b>⚙ 设置</b><button class="btn ghost" id="drawer-close" style="font-size:20px;">×</button></div>
  <div id="drawer-body">
    <div id="settings-nav"></div>
    <div id="settings-detail"></div>
  </div>
</div>

<div class="modal" id="model-modal">
  <div class="modal-card model-card">
    <h2>🧠 选择模型</h2>
    <div class="row" style="margin-bottom:10px;">
      <input id="model-search" type="text" placeholder="搜索模型名称 / 供应商…">
      <button class="btn ghost" id="model-close" style="font-size:20px;">×</button>
    </div>
    <div id="model-list"></div>
  </div>
</div>

<div class="modal" id="key-modal">
  <div class="modal-card">
    <h2>🔑 输入 API Key</h2>
    <p id="key-hint">当前模型需要 API Key（将按供应商保存到本地）。</p>
    <input id="key-input" type="password" placeholder="sk-...">
    <div class="row">
      <button class="btn" id="key-cancel">取消</button>
      <button class="btn primary" id="key-save">保存</button>
    </div>
  </div>
</div>

<div class="modal" id="ask-modal">
  <div class="modal-card">
    <h2 id="ask-title"></h2>
    <div id="ask-body"></div>
    <div class="row" id="ask-actions"></div>
  </div>
</div>

<script>
const messages = document.getElementById('messages');
const input = document.getElementById('input');
const slotsEl = document.getElementById('slot-list');
const sessionListEl = document.getElementById('session-list');
const drawer = document.getElementById('drawer');
const drawerBody = document.getElementById('drawer-body');
const settingsNav = document.getElementById('settings-nav');
const settingsDetail = document.getElementById('settings-detail');
const keyModal = document.getElementById('key-modal');

// ── 流指针（滚动 bug 修复：assistant 文本流 与 工具输出流 分离）──
let assistantStreamEl = null;
let toolOutputEl = null;
let currentProvider = '';
let hasKey = false;

function scroll() { messages.scrollTop = messages.scrollHeight; }
function addMsg(role, text) {
  const el = document.createElement('div');
  el.className = 'msg ' + role;
  if ((role === 'assistant' || role === 'cmd') && text) {
    el.innerHTML = mdToHtml(text);
  } else {
    el.textContent = text;
  }
  messages.appendChild(el);
  scroll();
  return el;
}
function addTool(name, args) {
  const el = document.createElement('div');
  el.className = 'tool';
  el.innerHTML = '🔧 <b>' + name + '</b> ' + (args || '');
  messages.appendChild(el);
  scroll();
}
function ensureAssistantStream() {
  if (!assistantStreamEl) {
    assistantStreamEl = addMsg('assistant', '');
    assistantStreamEl.classList.add('streaming');
  }
  return assistantStreamEl;
}
function endAssistantStream() { assistantStreamEl = null; }
function finalizeAssistant() {
  if (assistantStreamEl) {
    assistantStreamEl.classList.remove('streaming');
    if (assistantStreamEl.textContent) {
      assistantStreamEl.innerHTML = mdToHtml(assistantStreamEl.textContent);
    }
  }
}
function ensureToolOutput() {
  if (!toolOutputEl) {
    toolOutputEl = document.createElement('div');
    toolOutputEl.className = 'tool-output';
    messages.appendChild(toolOutputEl);
    scroll();
  }
  return toolOutputEl;
}
function endToolOutput() { toolOutputEl = null; }
function clearMessages() { messages.innerHTML = ''; assistantStreamEl = null; toolOutputEl = null; }

// ── 主题 ──
function applyTheme(t) {
  document.documentElement.dataset.theme = t;
  localStorage.setItem('waycoder-theme', t);
  document.getElementById('theme-btn').textContent = t === 'light' ? '☀️' : '🌙';
}
document.getElementById('theme-btn').onclick = () =>
  applyTheme(document.documentElement.dataset.theme === 'light' ? 'dark' : 'light');

// ── 权限模式（顶栏下拉）──
const permSelect = document.getElementById('perm-select');
function applyPermMode(mode) {
  if (mode && permSelect.value !== mode) permSelect.value = mode;
}
permSelect.onchange = () =>
  fetch('/perm', { method: 'POST', body: JSON.stringify({ mode: permSelect.value }) }).catch(() => {});

// ── 槽位（左栏）──
function renderSlots(state) {
  slotsEl.innerHTML = '';
  for (let i = 0; i < state.slots.length; i++) {
    const s = state.slots[i];
    const b = document.createElement('div');
    b.className = 'slot' + (i === state.activeSlot ? ' active' : '') + (s.hasHistory ? ' has' : '');
    b.textContent = 'F' + (i + 1);
    b.title = s.model ? ('F' + (i + 1) + ' · ' + s.model) : ('F' + (i + 1) + ' · 空');
    b.onclick = () => switchSlot(i);
    slotsEl.appendChild(b);
  }
}
function switchSlot(i) {
  fetch('/slot', { method: 'POST', body: JSON.stringify({ slot: i }) })
    .then(r => r.json())
    .then(list => { clearMessages(); list.forEach(m => addMsg(m.role === 'user' ? 'user' : 'assistant', m.content)); })
    .then(fetchPanel);
}

// ── 历史会话（左栏）──
function fetchSessions() {
  fetch('/sessions').then(r => r.json()).then(renderSessions).catch(() => {});
}
function renderSessions(list) {
  sessionListEl.innerHTML = '';
  if (!list || !list.length) { sessionListEl.innerHTML = '<div class="empty">暂无历史会话</div>'; return; }
  list.forEach(s => {
    const item = document.createElement('div');
    item.className = 'session-item';
    item.title = s.id;
    const p = document.createElement('div');
    p.className = 'preview';
    p.textContent = s.preview || s.id;
    const m = document.createElement('div');
    m.className = 'meta';
    m.textContent = (s.model || '?') + ' · ' + (s.savedAt || '') + (s.msgCount ? (' · ' + s.msgCount + ' 条') : '');
    const ops = document.createElement('div');
    ops.className = 'ops';
    const rb = document.createElement('button'); rb.textContent = '✎'; rb.title = '重命名';
    rb.onclick = e => { e.stopPropagation(); renameSession(s.id); };
    const db = document.createElement('button'); db.textContent = '✕'; db.title = '删除';
    db.onclick = e => { e.stopPropagation(); deleteSession(s.id); };
    ops.appendChild(rb); ops.appendChild(db);
    item.appendChild(p); item.appendChild(m); item.appendChild(ops);
    item.onclick = () => loadSession(s.id);
    sessionListEl.appendChild(item);
  });
}
function loadSession(id) {
  fetch('/sessions/load', { method: 'POST', body: JSON.stringify({ id: id }) })
    .then(r => r.json())
    .then(res => { if (res && res.ok === false) { alert(res.error || '加载失败'); return; } })
    .catch(() => {});
}
function deleteSession(id) {
  if (!confirm('删除会话 ' + id + ' ?')) return;
  fetch('/sessions/delete', { method: 'POST', body: JSON.stringify({ id: id }) }).then(fetchSessions).catch(() => {});
}
function renameSession(id) {
  const newId = prompt('重命名会话（ID）:', id);
  if (!newId || newId === id) return;
  fetch('/sessions/rename', { method: 'POST', body: JSON.stringify({ id: id, newId: newId }) })
    .then(r => r.json())
    .then(res => { if (res && res.ok === false) alert(res.error || '重命名失败'); })
    .then(fetchSessions)
    .catch(() => {});
}
document.getElementById('new-session').onclick = () => {
  fetch('/sessions/new', { method: 'POST' })
    .then(r => r.json())
    .then(res => { if (res && res.ok) alert('已保存新会话：' + res.id); })
    .then(fetchSessions)
    .catch(() => {});
};

// ── 右栏面板 ──
function fetchPanel() {
  if (document.hidden) return;
  fetch('/panel').then(r => r.json()).then(renderPanel).catch(() => {});
}
function renderPanel(p) {
  renderTodos(p.todos);
  renderTokens(p.tokens, p.cost);
  renderFiles(p.files);
  renderMcp(p.mcp);
  renderLsp(p.lsp);
}
function statusDot(status) {
  const map = { pending:'pending', in_progress:'in_progress', completed:'completed', cancelled:'cancelled', blocked:'blocked' };
  return '<span class="dot ' + (map[status] || 'pending') + '"></span>';
}
function renderTodos(todos) {
  const el = document.getElementById('panel-todos');
  if (!todos || !todos.length) { el.innerHTML = '<div class="empty">无任务</div>'; return; }
  el.innerHTML = todos.map(t => '<div class="item">' + statusDot(t.status) + escapeHtml(t.title || t.id) + '</div>').join('');
}
function renderTokens(tokens, cost) {
  const el = document.getElementById('panel-tokens');
  if (!tokens) { el.innerHTML = ''; return; }
  const tp = tokens.totalPrompt || 0, tc = tokens.totalCompletion || 0;
  const tP = tokens.taskPrompt || 0, tC = tokens.taskCompletion || 0;
  const fmt = n => (n == null ? '—' : Number(n).toLocaleString());
  const usd = n => (n == null ? '—' : '$' + Number(n).toFixed(4));
  el.innerHTML =
    '<div class="row"><span class="k">本轮</span><span class="v">' + fmt(tP) + ' / ' + fmt(tC) + '</span></div>' +
    '<div class="row"><span class="k">累计</span><span class="v">' + fmt(tp) + ' / ' + fmt(tc) + '</span></div>' +
    '<div class="row"><span class="k">本轮费用</span><span class="v">' + usd(cost && cost.task) + '</span></div>' +
    '<div class="row"><span class="k">累计估计</span><span class="v">' + usd(cost && cost.estimated) + '</span></div>' +
    (tokens.tokensPerSec ? '<div class="row"><span class="k">速率</span><span class="v">' + Number(tokens.tokensPerSec).toFixed(1) + ' tok/s</span></div>' : '');
}
function renderFiles(files) {
  const el = document.getElementById('panel-files');
  if (!files || !files.length) { el.innerHTML = '<div class="empty">无</div>'; return; }
  el.innerHTML = files.map(f => '<div class="item">' + escapeHtml(f) + '</div>').join('');
}
function renderMcp(mcp) {
  const el = document.getElementById('panel-mcp');
  if (!mcp || !mcp.length) { el.innerHTML = '<div class="empty">未配置</div>'; return; }
  el.innerHTML = mcp.map(s => {
    const dot = s.status === 'connected' ? 'on' : (s.status === 'connecting' ? 'connecting' : 'off');
    const extra = s.toolCount ? (' · ' + s.toolCount + ' 工具') : '';
    return '<div class="item"><span class="dot ' + dot + '"></span>' + escapeHtml(s.name) + extra +
      (s.error ? '<div style="color:var(--dim);font-size:11px;">' + escapeHtml(s.error) + '</div>' : '') + '</div>';
  }).join('');
}
function renderLsp(lsp) {
  const el = document.getElementById('panel-lsp');
  if (!lsp || !lsp.length) { el.innerHTML = '<div class="empty">无活动会话</div>'; return; }
  el.innerHTML = lsp.map(s => {
    const dot = s.hasExited ? 'off' : (s.initialized ? 'on' : 'connecting');
    return '<div class="item"><span class="dot ' + dot + '"></span>' + escapeHtml(s.command) +
      (s.root ? '<div style="color:var(--dim);font-size:11px;">' + escapeHtml(s.root) + '</div>' : '') + '</div>';
  }).join('');
}

// ── 模型选择窗口 ──
let modelMap = {};
let allModels = [];
let currentModelId = '';
let pendingModelId = '';
function renderModels(models, state) {
  modelMap = {};
  allModels = models;
  models.forEach(m => { modelMap[m.id] = m; });
  currentModelId = state.model;
  updateModelBtn();
}
function updateModelBtn() {
  const m = modelMap[currentModelId];
  document.getElementById('model-btn-label').textContent = m ? m.name : (currentModelId || '模型');
}
function formatContext(ctx) {
  if (!ctx) return '';
  return ctx >= 1024 ? (Math.round(ctx / 1024)) + 'k' : ctx;
}
function openModelModal() {
  document.getElementById('model-search').value = '';
  renderModelList('');
  document.getElementById('model-modal').classList.add('open');
}
function renderModelList(filter) {
  const el = document.getElementById('model-list');
  const f = (filter || '').trim().toLowerCase();
  const byProvider = {};
  allModels.forEach(m => {
    if (f && !(m.name.toLowerCase().includes(f) || m.provider.toLowerCase().includes(f) || m.providerId.toLowerCase().includes(f))) return;
    (byProvider[m.providerId] = byProvider[m.providerId] || []).push(m);
  });
  el.innerHTML = '';
  const pids = Object.keys(byProvider);
  if (pids.length === 0) { el.innerHTML = '<div class="empty">无匹配模型</div>'; return; }
  pids.forEach(pid => {
    const g = document.createElement('div');
    g.className = 'model-group';
    const gn = document.createElement('div');
    gn.className = 'gname';
    gn.textContent = pid;
    g.appendChild(gn);
    byProvider[pid].forEach(m => {
      const item = document.createElement('div');
      item.className = 'model-item' + (m.id === currentModelId ? ' selected' : '');
      const name = document.createElement('span');
      name.className = 'name';
      name.textContent = m.name;
      const cat = document.createElement('span');
      cat.className = 'tag cat';
      cat.textContent = m.category || pid;
      item.appendChild(name);
      item.appendChild(cat);
      if (!m.hasKey) {
        const nk = document.createElement('span');
        nk.className = 'tag nokey';
        nk.textContent = '需 key';
        item.appendChild(nk);
      }
      const meta = document.createElement('span');
      meta.className = 'meta';
      meta.textContent = formatContext(m.context) + (m.inputPrice > 0 ? (' · $' + m.inputPrice) : '');
      item.appendChild(meta);
      item.onclick = () => chooseModel(m);
      g.appendChild(item);
    });
    el.appendChild(g);
  });
}
function chooseModel(m) {
  if (!m.hasKey && m.providerId !== 'local' && m.providerId !== 'custom') {
    pendingModelId = m.id;
    currentProvider = m.providerId;
    document.getElementById('key-hint').textContent = '为 ' + m.providerId + ' 输入 API Key（保存后切换到 ' + m.name + '）。';
    document.getElementById('key-input').value = '';
    document.getElementById('model-modal').classList.remove('open');
    keyModal.classList.add('open');
    return;
  }
  fetch('/model', { method: 'POST', body: JSON.stringify({ modelId: m.id }) })
    .then(() => { currentModelId = m.id; updateModelBtn(); renderModelList(document.getElementById('model-search').value); })
    .catch(() => {});
}
document.getElementById('model-btn').onclick = openModelModal;
document.getElementById('model-search').oninput = e => renderModelList(e.target.value);
document.getElementById('model-close').onclick = () => document.getElementById('model-modal').classList.remove('open');

// ── key 弹窗 ──
function saveKey() {
  const k = document.getElementById('key-input').value.trim();
  if (!k) return;
  fetch('/key', { method: 'POST', body: JSON.stringify({ providerId: currentProvider, apiKey: k }) })
    .then(() => {
      hasKey = true;
      keyModal.classList.remove('open');
      if (pendingModelId) {
        const id = pendingModelId; pendingModelId = '';
        fetch('/model', { method: 'POST', body: JSON.stringify({ modelId: id }) })
          .then(() => { currentModelId = id; updateModelBtn(); })
          .catch(() => {});
      }
    });
}
document.getElementById('key-save').onclick = saveKey;
document.getElementById('key-cancel').onclick = () => keyModal.classList.remove('open');
document.getElementById('key-input').onkeydown = e => { if (e.key === 'Enter') saveKey(); };

// ── 设置（两列：左类别导航 + 右详细设置）──
let settingsGroups = [];
function renderSettingsNav() {
  settingsNav.innerHTML = '';
  settingsGroups.forEach((g, i) => {
    const b = document.createElement('button');
    b.className = 'nav-item' + (i === 0 ? ' active' : '');
    b.textContent = g.category;
    b.onclick = () => {
      settingsNav.querySelectorAll('.nav-item').forEach(x => x.classList.remove('active'));
      b.classList.add('active');
      renderSettingsDetail(g);
    };
    settingsNav.appendChild(b);
  });
  if (settingsGroups.length > 0) renderSettingsDetail(settingsGroups[0]);
}
function renderSettingsDetail(g) {
  settingsDetail.innerHTML = '';
  g.items.forEach(it => {
    const row = document.createElement('div');
    row.className = 'set-row';
    const label = document.createElement('label');
    label.textContent = it.label;
    const desc = document.createElement('div');
    desc.className = 'desc';
    desc.textContent = it.desc;
    row.appendChild(label);
    row.appendChild(desc);
    let ctrl;
    if (it.type === 'select' && it.options && it.options.length) {
      ctrl = document.createElement('select');
      it.options.forEach(o => { const op = document.createElement('option'); op.value = o; op.textContent = o || '(默认)'; if (o === it.value) op.selected = true; ctrl.appendChild(op); });
    } else if (it.type === 'toggle') {
      ctrl = document.createElement('input'); ctrl.type = 'checkbox';
      ctrl.checked = it.value === 'true' || it.value === '1';
    } else if (it.type === 'secret') {
      ctrl = document.createElement('input'); ctrl.type = 'password';
      ctrl.placeholder = it.value ? '已设置（留空则不修改）' : '未设置';
      ctrl.dataset.secret = '1';
    } else {
      ctrl = document.createElement('input');
      ctrl.type = it.type === 'number' ? 'number' : 'text';
      ctrl.value = it.value;
    }
    ctrl.dataset.key = it.key;
    ctrl.dataset.type = it.type;
    row.appendChild(ctrl);
    settingsDetail.appendChild(row);
  });
}
function saveSetting(ctrl) {
  let value;
  if (ctrl.dataset.type === 'toggle') value = ctrl.checked ? 'true' : 'false';
  else if (ctrl.dataset.secret === '1' && ctrl.value === '') return;
  else value = ctrl.value;
  fetch('/settings', { method: 'POST', body: JSON.stringify({ key: ctrl.dataset.key, value: value }) })
    .then(r => r.json())
    .then(res => { if (res && res.ok === false) alert(res.error || '设置失败'); });
}
document.getElementById('settings-btn').onclick = () => {
  fetch('/settings').then(r => r.json()).then(g => { settingsGroups = g; renderSettingsNav(); drawer.classList.add('open'); });
};
document.getElementById('drawer-close').onclick = () => drawer.classList.remove('open');

// ── Web 交互对话框（ask）──
let pendingAsk = null;
function showAsk(d) {
  pendingAsk = d;
  const title = document.getElementById('ask-title');
  const body = document.getElementById('ask-body');
  const actions = document.getElementById('ask-actions');
  title.textContent = d.title || '';
  body.innerHTML = '';
  actions.innerHTML = '';
  document.querySelector('#ask-modal .modal-card').classList.remove('diff');
  if (d.kind === 'select') {
    d.choices.forEach(c => {
      const b = document.createElement('button');
      b.className = 'ask-option';
      b.textContent = c;
      b.onclick = () => answerAsk(c);
      body.appendChild(b);
    });
  } else if (d.kind === 'multi') {
    const selected = new Set();
    d.choices.forEach(c => {
      const lbl = document.createElement('label');
      lbl.className = 'ask-multi';
      const cb = document.createElement('input'); cb.type = 'checkbox'; cb.value = c;
      cb.onchange = () => { cb.checked ? selected.add(c) : selected.delete(c); };
      lbl.appendChild(cb); lbl.appendChild(document.createTextNode(c));
      body.appendChild(lbl);
    });
    const ok = document.createElement('button'); ok.className = 'btn primary'; ok.textContent = '确定';
    ok.onclick = () => answerAsk([...selected].join('\n'));
    actions.appendChild(ok);
  } else if (d.kind === 'text') {
    const inp = document.createElement('input'); inp.type = 'text'; inp.id = 'ask-input';
    if (d.default) inp.value = d.default;
    body.appendChild(inp);
    const ok = document.createElement('button'); ok.className = 'btn primary'; ok.textContent = '确定';
    ok.onclick = () => answerAsk(inp.value);
    actions.appendChild(ok);
    inp.onkeydown = e => { if (e.key === 'Enter') answerAsk(inp.value); };
    setTimeout(() => inp.focus(), 50);
  } else if (d.kind === 'confirm') {
    title.textContent = d.title || '确认操作';
    const msg = document.createElement('div'); msg.className = 'ask-message'; msg.textContent = d.message || '';
    body.appendChild(msg);
    const yes = document.createElement('button'); yes.className = 'btn primary'; yes.textContent = '是';
    yes.onclick = () => answerAsk('0');
    const no = document.createElement('button'); no.className = 'btn danger'; no.textContent = '否';
    no.onclick = () => answerAsk('2');
    actions.appendChild(yes);
    if (d.allowAll) {
      const all = document.createElement('button'); all.className = 'btn'; all.textContent = '总是允许';
      all.onclick = () => answerAsk('1');
      actions.appendChild(all);
    }
    actions.appendChild(no);
  } else if (d.kind === 'diff') {
    title.textContent = d.title || 'Diff 预览';
    document.querySelector('#ask-modal .modal-card').classList.add('diff');
    (d.hunks || []).forEach((h, hi) => {
      const block = document.createElement('div');
      block.className = 'diff-hunk';
      const head = document.createElement('label');
      head.className = 'diff-hunk-head';
      const cb = document.createElement('input');
      cb.type = 'checkbox';
      cb.className = 'diff-hunk-check';
      cb.checked = true;
      cb.dataset.idx = hi;
      const hdr = document.createElement('span');
      hdr.textContent = h.header || ('Hunk ' + (hi + 1));
      head.appendChild(cb);
      head.appendChild(hdr);
      const pre = document.createElement('pre');
      pre.className = 'diff-hunk-lines';
      (h.lines || []).forEach(l => {
        const ln = document.createElement('span');
        ln.className = 'diff-line ' + (l.kind === '-' ? 'del' : l.kind === '+' ? 'add' : 'ctx');
        ln.textContent = (l.kind === ' ' ? ' ' : l.kind) + (l.text || '');
        pre.appendChild(ln);
      });
      block.appendChild(head);
      block.appendChild(pre);
      body.appendChild(block);
    });
    const acceptAll = document.createElement('button'); acceptAll.className = 'btn primary'; acceptAll.textContent = '全部接受';
    acceptAll.onclick = () => answerDiff('accept', null);
    const applySel = document.createElement('button'); applySel.className = 'btn'; applySel.textContent = '应用选中';
    applySel.onclick = () => {
      const acc = [];
      body.querySelectorAll('.diff-hunk-check').forEach(c => { if (c.checked) acc.push(Number(c.dataset.idx)); });
      answerDiff('partial', acc);
    };
    const rejectAll = document.createElement('button'); rejectAll.className = 'btn danger'; rejectAll.textContent = '全部拒绝';
    rejectAll.onclick = () => answerDiff('reject', null);
    actions.appendChild(acceptAll);
    actions.appendChild(applySel);
    actions.appendChild(rejectAll);
  }
  document.getElementById('ask-modal').classList.add('open');
}
function answerDiff(decision, accepted) {
  if (!pendingAsk) return;
  const id = pendingAsk.requestId;
  const value = JSON.stringify({ decision: decision, accepted: accepted || [] });
  fetch('/answer', { method: 'POST', body: JSON.stringify({ requestId: id, value: value }) })
    .then(() => { document.getElementById('ask-modal').classList.remove('open'); pendingAsk = null; })
    .catch(() => {});
}
function answerAsk(value) {
  if (!pendingAsk) return;
  const id = pendingAsk.requestId;
  fetch('/answer', { method: 'POST', body: JSON.stringify({ requestId: id, value: value }) })
    .then(() => { document.getElementById('ask-modal').classList.remove('open'); pendingAsk = null; })
    .catch(() => {});
}

// ── 发送 / 停止 ──
function handleUiCommand(text) {
  const lower = text.toLowerCase();
  if (lower === '/theme') {
    applyTheme(document.documentElement.dataset.theme === 'light' ? 'dark' : 'light');
    return true;
  }
  if (lower === '/settings') {
    fetch('/settings').then(r => r.json()).then(g => { settingsGroups = g; renderSettingsNav(); drawer.classList.add('open'); });
    return true;
  }
  if (lower === '/model' || lower === '/m') {
    openModelModal();
    return true;
  }
  return false;
}
function send() {
  const text = input.value.trim();
  if (!text) return;
  input.value = '';
  input.style.height = 'auto';

  // 纯 UI 斜杠命令（操作 DOM，不进入聊天流）
  if (handleUiCommand(text)) return;

  addMsg('user', text);

  // 斜杠命令 → 后端路由（未识别回退为普通 Agent 消息）
  if (text.startsWith('/') && text.length > 1) {
    fetch('/command', { method: 'POST', body: JSON.stringify({ input: text }) })
      .then(r => r.json())
      .then(res => {
        if (res && res.ok && res.handled) {
          addMsg('cmd', res.output || '');
        } else {
          fetch('/chat', { method: 'POST', body: text }).catch(() => {});
        }
      })
      .catch(() => { fetch('/chat', { method: 'POST', body: text }).catch(() => {}); });
    return;
  }

  fetch('/chat', { method: 'POST', body: text }).catch(() => {});
}
document.getElementById('send').onclick = send;
document.getElementById('stop').onclick = () => fetch('/interrupt', { method: 'POST' }).catch(() => {});
input.addEventListener('keydown', e => {
  if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send(); }
});
input.addEventListener('input', () => { input.style.height = 'auto'; input.style.height = Math.min(input.scrollHeight, 200) + 'px'; });

// ── 工具函数 ──
function escapeHtml(s) {
  return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
}

// ── Markdown 渲染（手搓、XSS 安全：先转义再结构化）──
function mdToHtml(src) {
  if (!src) return '';
  const lines = src.split('\n');
  const out = [];
  let paragraph = [];
  let listType = null;   // 'ul' | 'ol' | null
  let quote = false;

  function inline(s) {
    s = escapeHtml(s);
    s = s.replace(/`([^`]+)`/g, '<code class="md-inline">$1</code>');
    s = s.replace(/\[([^\]]+)\]\((https?:\/\/[^)\s]+)\)/g, '<a href="$2" target="_blank" rel="noopener noreferrer">$1</a>');
    s = s.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
    s = s.replace(/(^|[^*\w])\*([^*\n]+)\*(?!\*)/g, '$1<em>$2</em>');
    return s;
  }
  function flushParagraph() {
    if (paragraph.length) { out.push('<p>' + paragraph.map(inline).join('<br>') + '</p>'); paragraph = []; }
  }
  function flushList() {
    if (listType) { out.push('</' + listType + '>'); listType = null; }
  }
  function flushQuote() {
    if (quote) { out.push('</blockquote>'); quote = false; }
  }

  let i = 0;
  while (i < lines.length) {
    const line = lines[i];

    // 围栏代码块
    if (/^```/.test(line)) {
      flushParagraph(); flushList(); flushQuote();
      const lang = line.slice(3).trim();
      const code = [];
      i++;
      while (i < lines.length && !/^```/.test(lines[i])) { code.push(lines[i]); i++; }
      if (i < lines.length) i++; // 跳过结束 ```
      out.push('<pre class="md-code"><code' + (lang ? ' class="lang-' + escapeHtml(lang) + '"' : '') + '>' + escapeHtml(code.join('\n')) + '</code></pre>');
      continue;
    }

    // 表格（分隔行含 -，且上一行与分隔行都含 |）
    if (line.includes('|') && i + 1 < lines.length && /^\s*\|?[\s:|-]+\|[\s:|-]*$/.test(lines[i + 1]) && lines[i + 1].includes('-')) {
      flushParagraph(); flushList(); flushQuote();
      const headers = splitRow(line);
      i += 2; // 跳过表头与分隔行
      const rows = [];
      while (i < lines.length && lines[i].includes('|')) { rows.push(splitRow(lines[i])); i++; }
      let t = '<table class="md-table"><thead><tr>' + headers.map(h => '<th>' + inline(h) + '</th>').join('') + '</tr></thead><tbody>';
      t += rows.map(r => '<tr>' + r.map(c => '<td>' + inline(c) + '</td>').join('') + '</tr>').join('');
      t += '</tbody></table>';
      out.push(t);
      continue;
    }

    // 水平线
    if (/^\s*(-{3,}|\*{3,}|_{3,})\s*$/.test(line)) {
      flushParagraph(); flushList(); flushQuote();
      out.push('<hr>');
      i++;
      continue;
    }

    // 标题
    const h = /^(#{1,6})\s+(.*)$/.exec(line);
    if (h) {
      flushParagraph(); flushList(); flushQuote();
      const lv = h[1].length;
      out.push('<h' + lv + '>' + inline(h[2]) + '</h' + lv + '>');
      i++;
      continue;
    }

    // 引用
    const q = /^>\s?(.*)$/.exec(line);
    if (q) {
      flushParagraph(); flushList();
      if (!quote) { out.push('<blockquote>'); quote = true; }
      out.push('<p>' + inline(q[1]) + '</p>');
      i++;
      continue;
    }

    // 无序列表
    const ul = /^\s*[-*+]\s+(.*)$/.exec(line);
    if (ul) {
      flushParagraph(); flushQuote();
      if (listType !== 'ul') { flushList(); out.push('<ul>'); listType = 'ul'; }
      out.push('<li>' + inline(ul[1]) + '</li>');
      i++;
      continue;
    }

    // 有序列表
    const ol = /^\s*\d+[.)]\s+(.*)$/.exec(line);
    if (ol) {
      flushParagraph(); flushQuote();
      if (listType !== 'ol') { flushList(); out.push('<ol>'); listType = 'ol'; }
      out.push('<li>' + inline(ol[1]) + '</li>');
      i++;
      continue;
    }

    // 空行 → 段落/列表/引用收尾
    if (/^\s*$/.test(line)) {
      flushParagraph(); flushList(); flushQuote();
      i++;
      continue;
    }

    // 普通文本行 → 段落
    flushList(); flushQuote();
    paragraph.push(line);
    i++;
  }
  flushParagraph(); flushList(); flushQuote();
  return out.join('\n');
}

function splitRow(line) {
  let s = line.trim();
  if (s.startsWith('|')) s = s.slice(1);
  if (s.endsWith('|')) s = s.slice(0, -1);
  return s.split('|').map(c => c.trim());
}

// ── SSE ──
const es = new EventSource('/events');
es.addEventListener('token', e => { endToolOutput(); ensureAssistantStream().textContent += JSON.parse(e.data); scroll(); });
es.addEventListener('tool', e => { endAssistantStream(); const d = JSON.parse(e.data); addTool(d.name, d.args); });
es.addEventListener('tool_output', e => { ensureToolOutput().textContent += JSON.parse(e.data); scroll(); });
es.addEventListener('done', () => { finalizeAssistant(); endAssistantStream(); endToolOutput(); fetchPanel(); });
es.addEventListener('interrupted', () => { finalizeAssistant(); endAssistantStream(); endToolOutput(); addMsg('system', '⚠ 已中断'); fetchPanel(); });
es.addEventListener('failed', e => { finalizeAssistant(); endAssistantStream(); endToolOutput(); addMsg('system', '✘ ' + JSON.parse(e.data)); fetchPanel(); });
es.addEventListener('history', e => {
  const list = JSON.parse(e.data);
  if (messages.children.length === 0)
    list.forEach(m => addMsg(m.role === 'user' ? 'user' : 'assistant', m.content));
});
es.addEventListener('state', e => {
  const state = JSON.parse(e.data);
  currentProvider = state.provider;
  hasKey = state.hasKey;
  applyPermMode(state.permMode);
  renderSlots(state);
  if (state.model && state.model !== currentModelId) { currentModelId = state.model; updateModelBtn(); }
  fetchPanel();
});
es.addEventListener('sessions', () => fetchSessions());
es.addEventListener('ask', e => showAsk(JSON.parse(e.data)));

// ── 初始化 ──
applyTheme(localStorage.getItem('waycoder-theme') || 'dark');
fetch('/state').then(r => r.json()).then(state => {
  currentProvider = state.provider;
  hasKey = state.hasKey;
  applyPermMode(state.permMode);
  renderSlots(state);
});
fetch('/models').then(r => r.json()).then(models =>
  fetch('/state').then(r => r.json()).then(state => renderModels(models, state)));
fetchSessions();
fetchPanel();
setInterval(fetchPanel, 2000);
</script>
</body>
</html>
""";
}
