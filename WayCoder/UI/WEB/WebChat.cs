using System.Collections.Concurrent;
using System.Text;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;

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
        /// <summary>串行化对该客户端 StreamWriter 的写（Broadcast 与连接建立时的初始回放可能并发）。</summary>
        public readonly object WriteLock = new();
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
        var cts = Interlocked.Exchange(ref _roundCts, null);
        if (cts != null)
        {
            try { cts.Cancel(); } catch { }
            cts.Dispose();
        }
        UxHelper.WebInteraction = null;
        _server.Stop();
    }

    // ═══════════════════════════════════════════════════════════
    //  路由
    // ═══════════════════════════════════════════════════════════

    private HttpResponse? HandleRequest(HttpRequest req)
    {
        // CSRF 防护：状态变更请求（非 GET）必须来自本服务来源。
        // 浏览器跨源 fetch 必带 Origin（攻击者域名），curl/SSE/同源导航不带 Origin 放行；
        // Sec-Fetch-Site: cross-site 兜底拦截漏带 Origin 的跨站请求（纵深防御）。
        if (req.Method != "GET" && (!IsTrustedOrigin(req.Header("Origin"), Port) || IsCrossSite(req.Header("Sec-Fetch-Site"))))
            return new HttpResponse { Status = 403, Reason = "Forbidden", Body = Encoding.UTF8.GetBytes("403 Forbidden") };

        // 页面
        if (req.Method == "GET" && req.Path == "/")
            return HttpResponse.Html(WebAssets.Html.Replace("__VERSION__", Global.Version));

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
        if (req.Method == "POST" && req.Path == "/models/scan")
        {
            var probes = ModelCli.TestList();
            return HttpResponse.JsonBody(JNode.Object()
                .Set("ok", true)
                .Set("results", Json.Parse(SerializeScan(probes)) ?? JNode.Array())
                .ToJson());
        }
        if (req.Method == "POST" && req.Path == "/models/import")
            return ImportExternalModels();
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

        // 保存模型配置（仅持久化默认模型，不中断当前会话、不重配 Agent）
        if (req.Method == "POST" && req.Path == "/model/save")
        {
            var body = Json.Parse(req.Body);
            var modelId = body?["modelId"]?.AsString() ?? "";
            var info = ModelCatalog.Find(modelId);
            if (info == null) return HttpResponse.JsonBody(Err($"未知模型「{modelId}」"));
            var cfg = Config.Instance;
            cfg.Model = modelId;
            cfg.Provider = info.ProviderId;
            var baseUrl = ResolveBaseUrl(info, info.ProviderId, cfg.BaseUrl);
            if (baseUrl != null) cfg.BaseUrl = baseUrl;
            cfg.SaveToEnvFile();
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

            // 换模型副作用（需访问实例 Interrupt + EnsureSlot）
            if (cmdLower == "/model" && spaceIdx >= 0)
            {
                Interrupt();
                var name = trimmed[(spaceIdx + 1)..].Trim();
                var matches = ModelCatalog.Search(name);
                if (matches.Length == 0)
                    return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("handled", true)
                        .Set("output", $"❌ 未知模型「{name}」").ToJson());
                var err = ApplyModel(EnsureSlot(_activeSlot), matches[0].Id, null);
                Broadcast("state", SerializeState(_activeSlot, _slots));
                return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("handled", true)
                    .Set("output", err == null ? $"✅ 已切换到 **{matches[0].DisplayName}**" : $"❌ {err}").ToJson());
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
            // 「新建会话」= 清空当前对话、开新对话（不再落盘保存，避免攒出大量临时会话文件）
            Interrupt();
            var agent = EnsureSlot(_activeSlot);
            agent.Messages.Clear();
            Broadcast("history", SerializeHistory(agent));
            Broadcast("state", SerializeState(_activeSlot, _slots));
            return HttpResponse.JsonBody(Ok());
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
        if (req.Method == "POST" && req.Path == "/sessions/clear")
        {
            var deleted = SessionManager.DeleteAllSessions();
            Broadcast("sessions", SerializeSessions());
            return HttpResponse.JsonBody(JNode.Object().Set("ok", true).Set("deleted", deleted).ToJson());
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

        // 多模态上传：图片（入 vision 队列）/ 音频（转录为文字）
        if (req.Method == "POST" && req.Path == "/upload")
            return HandleUpload(req);

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
                var allowed = PermissionManager.CheckAsync("bash", new Dictionary<string, object?> { ["command"] = command })
                    .GetAwaiter().GetResult();
                if (!allowed)
                    return HttpResponse.JsonBody(JNode.Object().Set("ok", false).Set("output", "已拒绝执行").ToJson());
                var result = new BashTool()
                    .ExecuteAsync(new Dictionary<string, object?> { ["command"] = command })
                    .GetAwaiter().GetResult();
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
            var content = new ReadFileTool()
                .ExecuteAsync(new Dictionary<string, object?> { ["file_path"] = safePath })
                .GetAwaiter().GetResult();
            var agent = EnsureSlot(_activeSlot);
            agent.Messages.Add(JNode.Object().Set("role", "user")
                .Set("content", $"【文件引用】{path}\n\n{content}"));
            Broadcast("history", SerializeHistory(agent));
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
    {
        try
        {
            var root = Path.GetFullPath(Directory.GetCurrentDirectory());
            var full = Path.GetFullPath(path);
            var rel = Path.GetRelativePath(root, full);
            var outside = rel == ".."
                || rel.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                || rel.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal)
                || Path.IsPathRooted(rel);
            return outside ? null : full;
        }
        catch { return null; }
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

    /// <summary>
    /// 自动导入其他软件的模型列表 + API Key：复用 ModelCli.Import（写全局模型库）+ ApiKeyStore.ImportFromKnownSources（写 key）。
    /// 返回 JSON：{ok, modelReport, keys:[{providerId, source}]}。
    /// </summary>
    private static HttpResponse ImportExternalModels()
    {
        try
        {
            var modelReport = ModelCli.Import(null);
            var keys = ApiKeyStore.ImportFromKnownSources();
            ModelCatalog.Invalidate();
            ApiKeyStore.ClearCache();

            var keyArr = JNode.Array();
            foreach (var (pid, src) in keys)
                keyArr.Add(JNode.Object().Set("providerId", pid).Set("source", src));

            return HttpResponse.JsonBody(JNode.Object()
                .Set("ok", true)
                .Set("modelReport", modelReport)
                .Set("keys", keyArr)
                .ToJson());
        }
        catch (Exception ex)
        {
            return HttpResponse.JsonBody(Err(ex.Message));
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
        // 原子取出并清空当前轮的 CTS，避免与 MainLoopAsync 的 finally 并发 Dispose/Cancel 竞态
        var cts = Interlocked.Exchange(ref _roundCts, null);
        if (cts != null)
        {
            try { cts.Cancel(); } catch { }
            cts.Dispose();
        }
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
                var roundCts = new CancellationTokenSource();
                _roundCts = roundCts;
                var token = roundCts.Token;
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
                    // 仅当仍指向本轮 CTS 时才清空并释放；若并发 Interrupt 已 Exchange 走该 CTS，
                    // 则 CompareExchange 失败、交回给 Interrupt 处理，避免 Dispose 后再 Cancel 抛 ObjectDisposedException。
                    if (ReferenceEquals(Interlocked.CompareExchange(ref _roundCts, null, roundCts), roundCts))
                        roundCts.Dispose();
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
            // 连接即回放历史 + 状态，前端初始化渲染（与 Broadcast 共用写锁，避免与并发广播交错）
            lock (client.WriteLock)
            {
                writer.Write(HttpServer.SseEvent("history", SerializeHistory(_slots[_activeSlot]!)));
                writer.Write(HttpServer.SseEvent("state", SerializeState(_activeSlot, _slots)));
            }
            await client.Closed.Task;
        }
        catch { /* 连接断开 */ }
        finally
        {
            lock (_lock) _clients.Remove(client);
            // 与 Broadcast 的写共用锁，避免对已释放 writer 的并发写
            lock (client.WriteLock) { try { writer.Dispose(); } catch { } }
        }
    }

    private void Broadcast(string type, string dataJson)
    {
        var sse = HttpServer.SseEvent(type, dataJson);
        List<SseClient> snapshot;
        lock (_lock) snapshot = _clients.ToList();
        foreach (var c in snapshot)
        {
            // 每客户端串行写：防止主循环流式回调与 HandleRequest 并发 Broadcast 时帧交错损坏
            lock (c.WriteLock)
            {
                try { c.Writer.Write(sse); }
                catch { c.Closed.TrySetResult(); }
            }
        }
    }
}
