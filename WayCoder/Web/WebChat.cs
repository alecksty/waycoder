using System.Collections.Concurrent;
using System.Text;

namespace WayCoder.Web;

/// <summary>
/// 浏览器聊天桥接层：把 <see cref="Agent.ChatAsync"/> 的流式回调（onToken/onTool/onToolOutput）
/// 转为 SSE 事件广播给浏览器，接收浏览器 POST 的输入入队，支持中断。
/// 对标 DeepSeek Harness Web UI：多槽位（F1-F10）、换模型、输 key、设置、黑白主题。
/// </summary>
public sealed class WebChatServer
{
    private const int SlotCount = 10;

    private readonly HttpServer _server;
    private readonly Agent?[] _slots = new Agent?[SlotCount];
    private int _activeSlot;
    private readonly ConcurrentQueue<(int Slot, string Input)> _input = new();
    private readonly object _lock = new();
    private readonly List<SseClient> _clients = new();
    private readonly CancellationTokenSource _serverCts = new();
    private CancellationTokenSource? _roundCts;
    private Task? _loopTask;

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
        _loopTask = Task.Run(() => MainLoopAsync(_serverCts.Token));
    }

    public void Stop()
    {
        try { _serverCts.Cancel(); } catch { }
        try { _roundCts?.Cancel(); } catch { }
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
        lock (_lock) _clients.Add(client);
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
        => JNode.Object().Set("name", name).Set("args", brief).ToJson();

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
}
[data-theme="light"] {
  --bg:#f5f6f8; --panel:#ffffff; --panel2:#f0f2f6; --border:#e2e5ec; --text:#1a1d24; --dim:#6b7280;
  --accent:#2f6bff; --user:#e3ecff; --tool:#fff4dc; --danger:#ffe2e2; --shadow:0 4px 20px rgba(0,0,0,.12);
}
* { box-sizing:border-box; margin:0; padding:0; }
body { background:var(--bg); color:var(--text); font:15px/1.6 -apple-system,"PingFang SC","Microsoft YaHei",sans-serif; height:100vh; display:flex; flex-direction:column; transition:background .2s,color .2s; }
header { padding:10px 14px; border-bottom:1px solid var(--border); background:var(--panel); display:flex; align-items:center; gap:10px; flex-wrap:wrap; }
.logo { font-weight:700; color:var(--text); font-size:15px; white-space:nowrap; }
.logo span { color:var(--accent); }
.slots { display:flex; gap:4px; align-items:center; }
.slot { min-width:28px; height:28px; padding:0 6px; border-radius:8px; border:1px solid var(--border); background:var(--panel2); color:var(--dim); font-size:12px; cursor:pointer; display:flex; align-items:center; justify-content:center; transition:all .15s; }
.slot:hover { border-color:var(--accent); color:var(--text); }
.slot.active { background:var(--accent); border-color:var(--accent); color:#fff; font-weight:600; }
.slot.has { color:var(--text); border-color:var(--dim); }
.spacer { flex:1; }
select, .btn { height:34px; border-radius:10px; border:1px solid var(--border); background:var(--panel2); color:var(--text); font:inherit; padding:0 12px; cursor:pointer; outline:none; }
select:focus { border-color:var(--accent); }
select optgroup { background:var(--panel); color:var(--text); }
.btn { display:flex; align-items:center; gap:6px; font-weight:600; }
.btn:hover { border-color:var(--accent); }
.btn.ghost { background:transparent; border:none; font-size:18px; padding:0 8px; }
#messages { flex:1; overflow-y:auto; padding:18px; display:flex; flex-direction:column; gap:12px; }
.msg { max-width:82%; padding:10px 15px; border-radius:14px; white-space:pre-wrap; word-break:break-word; }
.msg.user { align-self:flex-end; background:var(--user); border-bottom-right-radius:4px; }
.msg.assistant { align-self:flex-start; background:var(--panel); border:1px solid var(--border); border-bottom-left-radius:4px; }
.msg.system { align-self:center; color:var(--dim); font-size:13px; background:transparent; }
.tool { align-self:flex-start; background:var(--tool); border:1px solid var(--border); border-radius:12px; padding:7px 13px; font-size:13px; color:var(--dim); }
.tool b { color:#e8b34b; }
#input-bar { display:flex; gap:8px; padding:12px 14px; border-top:1px solid var(--border); background:var(--panel); }
#input { flex:1; resize:none; background:var(--bg); color:var(--text); border:1px solid var(--border); border-radius:14px; padding:11px 14px; font:inherit; min-height:44px; max-height:200px; outline:none; }
#input:focus { border-color:var(--accent); }
#send { background:var(--accent); color:#fff; border:none; }
#send:hover { opacity:.9; }
#stop { background:var(--danger); color:#ff9a9a; border:none; }
#stop:hover { opacity:.9; }

/* 设置抽屉 */
#drawer { position:fixed; top:0; right:0; bottom:0; width:360px; max-width:90vw; background:var(--panel); border-left:1px solid var(--border); box-shadow:var(--shadow); transform:translateX(100%); transition:transform .25s; z-index:50; display:flex; flex-direction:column; }
#drawer.open { transform:translateX(0); }
#drawer-head { padding:14px 18px; border-bottom:1px solid var(--border); display:flex; align-items:center; }
#drawer-head b { flex:1; }
#drawer-body { flex:1; overflow-y:auto; padding:12px 18px 24px; }
.set-group { margin-top:16px; }
.set-group h3 { font-size:13px; color:var(--accent); margin-bottom:8px; font-weight:600; }
.set-row { margin-bottom:12px; }
.set-row label { display:block; font-size:13px; color:var(--text); margin-bottom:4px; font-weight:500; }
.set-row .desc { font-size:11px; color:var(--dim); margin-bottom:5px; }
.set-row input, .set-row select { width:100%; height:34px; border-radius:10px; border:1px solid var(--border); background:var(--panel2); color:var(--text); font:inherit; padding:0 10px; outline:none; }
.set-row input:focus, .set-row select:focus { border-color:var(--accent); }
.set-row input[type="checkbox"] { width:auto; height:auto; }

/* key 弹窗 */
#key-modal { position:fixed; inset:0; background:rgba(0,0,0,.5); display:none; align-items:center; justify-content:center; z-index:60; }
#key-modal.open { display:flex; }
#key-card { background:var(--panel); border:1px solid var(--border); border-radius:18px; padding:22px 24px; width:420px; max-width:90vw; box-shadow:var(--shadow); }
#key-card h2 { font-size:16px; margin-bottom:6px; }
#key-card p { font-size:13px; color:var(--dim); margin-bottom:14px; }
#key-card input { width:100%; height:38px; border-radius:12px; border:1px solid var(--border); background:var(--panel2); color:var(--text); font:inherit; padding:0 12px; outline:none; margin-bottom:14px; }
#key-card input:focus { border-color:var(--accent); }
#key-card .row { display:flex; gap:8px; justify-content:flex-end; }
</style>
</head>
<body>
<header>
  <div class="logo">🤖 Way<span>Coder</span></div>
  <div class="slots" id="slots"></div>
  <div class="spacer"></div>
  <select id="model-select" title="切换模型"></select>
  <button class="btn ghost" id="theme-btn" title="切换主题">🌙</button>
  <button class="btn" id="settings-btn" title="设置">⚙ 设置</button>
</header>

<div id="messages"></div>

<div id="input-bar">
  <textarea id="input" placeholder="输入消息，Enter 发送，Shift+Enter 换行" rows="1"></textarea>
  <button class="btn" id="send">发送</button>
  <button class="btn" id="stop">停止</button>
</div>

<div id="drawer">
  <div id="drawer-head"><b>⚙ 设置</b><button class="btn ghost" id="drawer-close" style="font-size:20px;">×</button></div>
  <div id="drawer-body"></div>
</div>

<div id="key-modal">
  <div id="key-card">
    <h2>🔑 输入 API Key</h2>
    <p id="key-hint">当前模型需要 API Key（将按供应商保存到本地）。</p>
    <input id="key-input" type="password" placeholder="sk-...">
    <div class="row">
      <button class="btn" id="key-cancel">取消</button>
      <button class="btn" id="key-save" style="background:var(--accent);color:#fff;border:none;">保存</button>
    </div>
  </div>
</div>

<script>
const messages = document.getElementById('messages');
const input = document.getElementById('input');
const slotsEl = document.getElementById('slots');
const modelSel = document.getElementById('model-select');
const drawer = document.getElementById('drawer');
const drawerBody = document.getElementById('drawer-body');
const keyModal = document.getElementById('key-modal');
let streamEl = null;
let currentProvider = '';
let hasKey = false;

function scroll() { messages.scrollTop = messages.scrollHeight; }
function addMsg(role, text) {
  const el = document.createElement('div');
  el.className = 'msg ' + role;
  el.textContent = text;
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
function ensureStream() {
  if (!streamEl) streamEl = addMsg('assistant', '');
  return streamEl;
}
function endStream() { streamEl = null; }
function clearMessages() { messages.innerHTML = ''; streamEl = null; }

// ── 主题 ──
function applyTheme(t) {
  document.documentElement.dataset.theme = t;
  localStorage.setItem('waycoder-theme', t);
  document.getElementById('theme-btn').textContent = t === 'light' ? '☀️' : '🌙';
}
document.getElementById('theme-btn').onclick = () =>
  applyTheme(document.documentElement.dataset.theme === 'light' ? 'dark' : 'light');

// ── 槽位条 ──
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
    .then(list => { clearMessages(); list.forEach(m => addMsg(m.role === 'user' ? 'user' : 'assistant', m.content)); });
}

// ── 模型下拉 ──
let modelMap = {};
let pendingModelId = '';
function renderModels(models, state) {
  modelMap = {};
  const byProvider = {};
  models.forEach(m => { modelMap[m.id] = m; (byProvider[m.providerId] = byProvider[m.providerId] || []).push(m); });
  modelSel.innerHTML = '';
  Object.keys(byProvider).forEach(pid => {
    const og = document.createElement('optgroup');
    og.label = pid;
    byProvider[pid].forEach(m => {
      const op = document.createElement('option');
      op.value = m.id;
      op.textContent = m.name + (m.inputPrice > 0 ? ('  ($' + m.inputPrice + ')') : '');
      if (m.id === state.model) op.selected = true;
      og.appendChild(op);
    });
    modelSel.appendChild(og);
  });
}
modelSel.onchange = () => switchModel(modelSel.value);
function switchModel(modelId) {
  const m = modelMap[modelId];
  if (!m) return;
  if (!m.hasKey && m.providerId !== 'local' && m.providerId !== 'custom') {
    pendingModelId = modelId;
    currentProvider = m.providerId;
    document.getElementById('key-hint').textContent = '为 ' + m.providerId + ' 输入 API Key（保存后切换到 ' + m.name + '）。';
    document.getElementById('key-input').value = '';
    keyModal.classList.add('open');
    return;
  }
  fetch('/model', { method: 'POST', body: JSON.stringify({ modelId: modelId }) }).catch(() => {});
}

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
        fetch('/model', { method: 'POST', body: JSON.stringify({ modelId: id }) }).catch(() => {});
      }
    });
}
document.getElementById('key-save').onclick = saveKey;
document.getElementById('key-cancel').onclick = () => keyModal.classList.remove('open');
document.getElementById('key-input').onkeydown = e => { if (e.key === 'Enter') saveKey(); };

// ── 设置抽屉 ──
function renderSettings(groups) {
  drawerBody.innerHTML = '';
  groups.forEach(g => {
    const h = document.createElement('h3');
    h.className = 'set-group';
    h.textContent = g.category;
    drawerBody.appendChild(h);
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
      drawerBody.appendChild(row);
    });
  });
}
function saveSetting(ctrl) {
  let value;
  if (ctrl.dataset.type === 'toggle') value = ctrl.checked ? 'true' : 'false';
  else if (ctrl.dataset.secret === '1' && ctrl.value === '') return; // 未修改 secret
  else value = ctrl.value;
  fetch('/settings', { method: 'POST', body: JSON.stringify({ key: ctrl.dataset.key, value: value }) })
    .then(r => r.json())
    .then(res => { if (res && res.ok === false) alert(res.error || '设置失败'); });
}
document.getElementById('settings-btn').onclick = () => {
  fetch('/settings').then(r => r.json()).then(g => { renderSettings(g); drawer.classList.add('open'); });
};
document.getElementById('drawer-close').onclick = () => drawer.classList.remove('open');

// ── 发送 / 停止 ──
function send() {
  const text = input.value.trim();
  if (!text) return;
  addMsg('user', text);
  input.value = '';
  input.style.height = 'auto';
  fetch('/chat', { method: 'POST', body: text }).catch(() => {});
}
document.getElementById('send').onclick = send;
document.getElementById('stop').onclick = () => fetch('/interrupt', { method: 'POST' }).catch(() => {});
input.addEventListener('keydown', e => {
  if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send(); }
});
input.addEventListener('input', () => { input.style.height = 'auto'; input.style.height = Math.min(input.scrollHeight, 200) + 'px'; });

// ── SSE ──
const es = new EventSource('/events');
es.addEventListener('token', e => { ensureStream().textContent += JSON.parse(e.data); scroll(); });
es.addEventListener('tool', e => { const d = JSON.parse(e.data); addTool(d.name, d.args); });
es.addEventListener('tool_output', e => { ensureStream().textContent += JSON.parse(e.data); scroll(); });
es.addEventListener('done', () => endStream());
es.addEventListener('interrupted', () => { endStream(); addMsg('system', '⚠ 已中断'); });
es.addEventListener('failed', e => { endStream(); addMsg('system', '✘ ' + JSON.parse(e.data)); });
es.addEventListener('history', e => {
  const list = JSON.parse(e.data);
  if (messages.children.length === 0)
    list.forEach(m => addMsg(m.role === 'user' ? 'user' : 'assistant', m.content));
});
es.addEventListener('state', e => {
  const state = JSON.parse(e.data);
  currentProvider = state.provider;
  hasKey = state.hasKey;
  renderSlots(state);
});

// ── 初始化 ──
applyTheme(localStorage.getItem('waycoder-theme') || 'dark');
fetch('/state').then(r => r.json()).then(state => {
  currentProvider = state.provider;
  hasKey = state.hasKey;
  renderSlots(state);
});
fetch('/models').then(r => r.json()).then(models =>
  fetch('/state').then(r => r.json()).then(state => renderModels(models, state)));
</script>
</body>
</html>
""";
}
