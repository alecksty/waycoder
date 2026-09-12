using System.IO.Compression;
using System.Text;
using System.Text.Json;
using WayCoder.Infra;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Screens;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>浏览器聊天（--web）测试：HTTP 解析纯函数 + SSE 格式化 + 端到端冒烟</summary>
    private static void TestWeb(Action<string, bool> Check)
    {
        // ── 1. ParseHttpRequest 纯函数 ──
        var get = WayCoder.UI.Web.HttpServer.ParseHttpRequest("GET / HTTP/1.1\r\nHost: localhost\r\n\r\n");
        Check("Web: GET 方法", get?.Method == "GET");
        Check("Web: GET 路径", get?.Path == "/");
        Check("Web: 头解析", get?.Header("Host") == "localhost");

        var post = WayCoder.UI.Web.HttpServer.ParseHttpRequest("POST /chat HTTP/1.1\r\nContent-Length: 5\r\n\r\nhello");
        Check("Web: POST 方法", post?.Method == "POST");
        Check("Web: POST 正文", post?.Body == "hello");

        var query = WayCoder.UI.Web.HttpServer.ParseHttpRequest("GET /x?a=1&b=2 HTTP/1.1\r\n\r\n");
        Check("Web: 查询串", query?.Path == "/x" && query?.Query == "a=1&b=2");

        Check("Web: 畸形请求 null", WayCoder.UI.Web.HttpServer.ParseHttpRequest("") == null);
        Check("Web: 畸形请求行 null", WayCoder.UI.Web.HttpServer.ParseHttpRequest("GARBAGE\r\n\r\n") == null);

        // ── 2. SseEvent 格式化 ──
        var sse = WayCoder.UI.Web.HttpServer.SseEvent("token", "\"hi\"");
        Check("Web: SSE event 前缀", sse.StartsWith("event: token\ndata: "));
        Check("Web: SSE 空行结尾", sse.EndsWith("\n\n"));

        // ── 3. FindHeaderEnd / ParseContentLength ──
        var hb = Encoding.UTF8.GetBytes("GET / HTTP/1.1\r\nContent-Length: 5\r\n\r\nbody");
        Check("Web: FindHeaderEnd 定位", WayCoder.UI.Web.HttpServer.FindHeaderEnd(hb) > 0);
        Check("Web: ParseContentLength", WayCoder.UI.Web.HttpServer.ParseContentLength("Content-Length: 123\r\nX: y") == 123);
        Check("Web: ParseContentLength 缺省 0", WayCoder.UI.Web.HttpServer.ParseContentLength("X: y") == 0);

        // ── 4. 前端 HTML 含关键元素 ──
        var html = WayCoder.UI.Web.WebAssets.Html;
        Check("Web: HTML 含 EventSource", html.Contains("EventSource('/events?client=' + clientId)"));
        Check("Web: HTML 含 /chat", html.Contains("/chat"));
        Check("Web: HTML 含 /interrupt", html.Contains("/interrupt"));
        Check("Web: HTML 含 Markdown 渲染器", html.Contains("function mdToHtml"));
        Check("Web: HTML 含轮次收口 finishRound（原 finalizeAssistant）", html.Contains("function finishRound"));
        Check("Web: HTML 含流式态样式", html.Contains(".msg.assistant.streaming"));
        Check("Web: HTML 含权限模式下拉", html.Contains("id=\"perm-select\""));
        Check("Web: HTML 含权限模式切换", html.Contains("/perm"));

        // ── 5. 端到端冒烟：HTTP 服务 GET / ──
        var server = new WayCoder.UI.Web.HttpServer(0);
        server.OnRequest = req => Task.FromResult<WayCoder.UI.Web.HttpResponse?>(req.Path == "/" ? WayCoder.UI.Web.HttpResponse.Html("<html>ok</html>") : null);
        server.Start();
        try
        {
            using var client = new HttpClient();
            var resp = client.GetStringAsync($"http://127.0.0.1:{server.ActualPort}/").Result;
            Check("Web: 端到端 GET / 返回 HTML", resp.Contains("ok"));
        }
        catch { Check("Web: 端到端 GET / 返回 HTML", false); }
        finally { server.Stop(); }
    }

    /// <summary>Web 界面完整化：换模型/换 key/设置/槽位切换/序列化纯函数 + 端点冒烟。</summary>
    /// <summary>
    /// 离线化外壳：本段会调用 <c>ModelCli.TestList()</c>（TestList 与 POST /models/scan 内部都走它），
    /// 而它默认会带着**真实 API key** 去探测**真实服务商**（api.deepseek.com 等）。
    /// 这里把探测端点整体重定向到本地 mock：保留探针代码路径的覆盖率，同时保证自测绝不触网、
    /// 绝不出网真实凭据。配合 <see cref="Global.OfflineMode"/> 构成双保险。
    /// </summary>
    private static void TestWebFull(Action<string, bool> Check)
    {
        var mock = new WayCoder.UI.Web.HttpServer(0);
        mock.OnRequest = _ => Task.FromResult<WayCoder.UI.Web.HttpResponse?>(
            WayCoder.UI.Web.HttpResponse.JsonBody("{\"data\":[]}"));
        mock.Start();
        var savedOverride = WayCoder.ModelCli.ProbeBaseUrlOverride;
        WayCoder.ModelCli.ProbeBaseUrlOverride = $"http://127.0.0.1:{mock.ActualPort}";
        try
        {
            TestWebFullCore(Check);
        }
        finally
        {
            WayCoder.ModelCli.ProbeBaseUrlOverride = savedOverride;
            mock.Stop();
        }
    }

    private static void TestWebFullCore(Action<string, bool> Check)
    {
        // ── 1. LLM.Reconfigure（运行时换 key/baseUrl）──
        var llm = new LLM("deepseek-v4-flash", "old-key", "https://old.example.com");
        Check("WebFull: Reconfigure 前 key", llm.ApiKey == "old-key");
        llm.Reconfigure("new-key", "https://new.example.com");
        Check("WebFull: Reconfigure 换 key", llm.ApiKey == "new-key");
        Check("WebFull: Reconfigure 换 baseUrl", llm.BaseUrl == "https://new.example.com");
        Check("WebFull: Endpoint 随 baseUrl 更新", llm.Endpoint == "https://new.example.com/v1/chat/completions");
        llm.Model = "deepseek-v4-pro";
        Check("WebFull: Model 直接改生效", llm.Model == "deepseek-v4-pro" && llm.EffectiveModel == "deepseek-v4-pro");

        // ── 2. SerializeModels ──
        var modelsJson = WayCoder.UI.Web.WebChatServer.SerializeModels();
        var models = Json.Parse(modelsJson);
        Check("WebFull: models 是数组", models != null && models.Kind == JKind.Array);
        Check("WebFull: models 含 deepseek-v4-pro", modelsJson.Contains("deepseek-v4-pro"));
        Check("WebFull: models 含 gpt-5.5", modelsJson.Contains("gpt-5.5"));
        Check("WebFull: models 元素含 providerId", models![0]?["providerId"]?.AsString() != null);
        Check("WebFull: models 元素含 hasKey", models[0]?.Get("hasKey") != null);

        // ── 3. SerializeSettings ──
        var settingsJson = WayCoder.UI.Web.WebChatServer.SerializeSettings();
        var settings = Json.Parse(settingsJson);
        Check("WebFull: settings 是分组数组", settings != null && settings.Kind == JKind.Array);
        bool hasSecret = false, hasModel = false;
        foreach (var g in settings!.Items)
        {
            var items = g["items"];
            if (items == null) continue;
            foreach (var it in items.Items)
            {
                if (it["type"]?.AsString() == "secret") hasSecret = true;
                if (it["key"]?.AsString() == "Model") hasModel = true;
            }
        }
        Check("WebFull: settings 含 secret 字段", hasSecret);
        Check("WebFull: settings 含 Model 字段", hasModel);

        // ── 4. SerializeState / SerializeHistory ──
        var a0 = new Agent(new LLM("test", "sk-test"));
        var slots = new Agent?[10];
        slots[0] = a0;
        var stateJson = WayCoder.UI.Web.WebChatServer.SerializeState(0, slots);
        Check("WebFull: state 含 activeSlot=0", stateJson.Contains("\"activeSlot\":0"));
        Check("WebFull: state 含 slots", stateJson.Contains("\"slots\":"));
        Check("WebFull: state 含 permMode", stateJson.Contains("\"permMode\":"));
        Check("WebFull: history 空数组", WayCoder.UI.Web.WebChatServer.SerializeHistory(a0).Trim() == "[]");

        // ── 5. ApplyModel 非法模型（安全分支，不触发持久化）──
        Check("WebFull: ApplyModel 非法模型报错",
            WayCoder.UI.Web.WebChatServer.ApplyModel(a0, "no-such-model-xyz", null) != null);

        // ── 6. ProviderHasKey ──
        Check("WebFull: local 无需 key", WayCoder.UI.Web.WebChatServer.ProviderHasKey("local"));
        Check("WebFull: custom 无需 key", WayCoder.UI.Web.WebChatServer.ProviderHasKey("custom"));

        // ── 6b. IsTrustedOrigin（CSRF 防护）──
        Check("WebFull: 无 Origin（curl/SSE）放行", WayCoder.UI.Web.WebChatServer.IsTrustedOrigin(null, 8123));
        Check("WebFull: 本服务 Origin 放行", WayCoder.UI.Web.WebChatServer.IsTrustedOrigin("http://127.0.0.1:8123", 8123));
        Check("WebFull: localhost Origin 放行", WayCoder.UI.Web.WebChatServer.IsTrustedOrigin("http://localhost:8123", 8123));
        Check("WebFull: 攻击者 Origin 拒绝", !WayCoder.UI.Web.WebChatServer.IsTrustedOrigin("https://evil.example.com", 8123));
        Check("WebFull: Origin null 拒绝", !WayCoder.UI.Web.WebChatServer.IsTrustedOrigin("null", 8123));
        Check("WebFull: 端口不匹配拒绝", !WayCoder.UI.Web.WebChatServer.IsTrustedOrigin("http://127.0.0.1:9999", 8123));

        // ── 6c2. AgentStatusResolver 统一状态解析（纯函数，四端动态状态栏共用）──
        {
            var busy = new WayCoder.UI.Shared.AgentStatusInput(
                Busy: true, ToolName: null, Compressing: false, WaitingPermission: false,
                WaitingUser: false, WaitingSubagent: false, Mode: WayCoder.WorkMode.Build);
            var idle = busy with { Busy = false };
            Check("Status: 忙且无工具 → 思考中",
                WayCoder.UI.Shared.AgentStatusResolver.Resolve(busy).Status == WayCoder.UI.Shared.AgentStatus.Thinking);
            Check("Status: 忙+工具 → 工具执行",
                WayCoder.UI.Shared.AgentStatusResolver.Resolve(busy with { ToolName = "bash" }).Status == WayCoder.UI.Shared.AgentStatus.ToolRunning);
            Check("Status: 忙+ask_user_question → 等待用户",
                WayCoder.UI.Shared.AgentStatusResolver.Resolve(busy with { ToolName = "ask_user_question" }).Status == WayCoder.UI.Shared.AgentStatus.WaitingUser);
            Check("Status: 忙+agent → 等待子代理",
                WayCoder.UI.Shared.AgentStatusResolver.Resolve(busy with { ToolName = "agent" }).Status == WayCoder.UI.Shared.AgentStatus.WaitingSubagent);
            Check("Status: 压缩优先于忙",
                WayCoder.UI.Shared.AgentStatusResolver.Resolve(busy with { Compressing = true }).Status == WayCoder.UI.Shared.AgentStatus.Compressing);
            Check("Status: 等待确认优先于工具",
                WayCoder.UI.Shared.AgentStatusResolver.Resolve(busy with { WaitingPermission = true, ToolName = "bash" }).Status == WayCoder.UI.Shared.AgentStatus.WaitingPermission);
            Check("Status: 空闲",
                WayCoder.UI.Shared.AgentStatusResolver.Resolve(idle).Status == WayCoder.UI.Shared.AgentStatus.Idle);
            Check("Status: 完成瞬态",
                WayCoder.UI.Shared.AgentStatusResolver.Resolve(idle with { RecentComplete = true }).Status == WayCoder.UI.Shared.AgentStatus.Complete);
            Check("Status: 计划模式",
                WayCoder.UI.Shared.AgentStatusResolver.Resolve(idle with { Mode = WayCoder.WorkMode.Plan }).Status == WayCoder.UI.Shared.AgentStatus.Planning);
            Check("StatusKey: thinking",
                WayCoder.UI.Shared.AgentStatusResolver.StatusKey(WayCoder.UI.Shared.AgentStatus.Thinking) == "thinking");
            Check("StatusKey: waiting_user",
                WayCoder.UI.Shared.AgentStatusResolver.StatusKey(WayCoder.UI.Shared.AgentStatus.WaitingUser) == "waiting_user");
            Check("StatusKey: idle",
                WayCoder.UI.Shared.AgentStatusResolver.StatusKey(WayCoder.UI.Shared.AgentStatus.Idle) == "idle");
            Check("SpinnerFrames: 10 帧统一", WayCoder.UI.Shared.AgentStatusResolver.SpinnerFrames.Length == 10);
        }

        // ── 6c3. ModelPrice 多重价格统一格式化（四端动态价格显示）──
        Check("ModelPrice: 免费 → Free",
            WayCoder.UI.Shared.ModelPrice.Format(0, 0) == "Free");
        Check("ModelPrice: 输入/输出",
            WayCoder.UI.Shared.ModelPrice.Format(1.2, 4.5) == "$1.20/$4.50");
        Check("ModelPrice: 有闲时 → 附闲",
            WayCoder.UI.Shared.ModelPrice.Format(1.2, 4.5, 0.9, 3.0) == "$1.20/$4.50 闲$0.90/$3.00");
        Check("ModelPrice: 闲时与忙时相同 → 不附闲",
            WayCoder.UI.Shared.ModelPrice.Format(1.2, 4.5, 1.2, 4.5) == "$1.20/$4.50");
        Check("ModelPrice: 输出免费 → $in/Free",
            WayCoder.UI.Shared.ModelPrice.Format(1.2, 0) == "$1.20/Free");
        Check("ModelPrice: 微小价 → <$0.01",
            WayCoder.UI.Shared.ModelPrice.Format(0.005, 0.01) == "<$0.01/$0.01");

        // ── SerializeProviders（Web /provider 供应商列表）──
        var provJson = WayCoder.UI.Web.WebChatServer.SerializeProviders();
        Check("SerializeProviders: 含 deepseek 供应商", provJson.Contains("deepseek"));
        Check("SerializeProviders: 含 providerId/name/hasKey", provJson.Contains("providerId") && provJson.Contains("name") && provJson.Contains("hasKey"));

        // ── CommandBar（promptbar 精选常用命令）──
        var favNames = WayCoder.UI.Shared.CommandBar.Favorites.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Check("CommandBar: 含 /help /model /provider /review /reset",
            favNames.Contains("/help") && favNames.Contains("/model") && favNames.Contains("/provider")
            && favNames.Contains("/review") && favNames.Contains("/reset"));
        Check("CommandBar: 精选 ≤ 12 条（一行放得下）", WayCoder.UI.Shared.CommandBar.Favorites.Length <= 12);

        // ── 6c. HasKeyFor / SerializeScan / TestList（模型 key 检测 + 连通性扫描）──
        Check("HasKeyFor: local 无需 key", WayCoder.ApiKeyStore.HasKeyFor("local", "qwen2.5-coder:latest"));
        Check("HasKeyFor: custom 无需 key", WayCoder.ApiKeyStore.HasKeyFor("custom", "my-custom-model"));
        Check("HasKeyFor: 无 key 供应商返回 false", !WayCoder.ApiKeyStore.HasKeyFor("__selftest_nokey__", "gpt-5.5"));
        ApiKeyStore.Set("__selftest_probe__", "sk-probe-123");
        Check("HasKeyFor: 已存 key 返回 true", WayCoder.ApiKeyStore.HasKeyFor("__selftest_probe__", "any-model"));
        ApiKeyStore.Remove("__selftest_probe__");
        Check("HasKeyFor: 删除后返回 false", !WayCoder.ApiKeyStore.HasKeyFor("__selftest_probe__", "any-model"));

        Check("ProviderFromEnvVarName: ANTHROPIC_API_KEY → anthropic",
            ApiKeyStore.ProviderFromEnvVarName("ANTHROPIC_API_KEY") == "anthropic");
        Check("ProviderFromEnvVarName: DEEPSEEK_API_KEY → deepseek",
            ApiKeyStore.ProviderFromEnvVarName("DEEPSEEK_API_KEY") == "deepseek");
        Check("ProviderFromEnvVarName: 无关变量返回 null",
            ApiKeyStore.ProviderFromEnvVarName("FOO_BAR") == null);

        var probes = new List<WayCoder.ModelCli.EndpointProbe>
        {
            new("openai", "OpenAI", "https://api.openai.com", true, "已连接（200）", new[] { "gpt-5.5", "gpt-5.5-mini" }),
            new("bad", "Bad", "https://bad.example.com", false, "无法连接", Array.Empty<string>()),
        };
        var scanJson = WayCoder.UI.Web.WebChatServer.SerializeScan(probes);
        var scanArr = Json.Parse(scanJson);
        Check("SerializeScan: 是数组", scanArr?.Kind == JKind.Array);
        Check("SerializeScan: 含 providerId/ok/detail",
            scanJson.Contains("\"providerId\"") && scanJson.Contains("\"ok\"") && scanJson.Contains("\"detail\""));
        Check("SerializeScan: ok 字段正确",
            scanArr![0]?["ok"]?.AsBool() == true && scanArr[1]?["ok"]?.AsBool() == false);

        var testList = WayCoder.ModelCli.TestList();
        Check("TestList: 返回列表（不抛异常）", testList != null);
        // 外壳已把探测端点重定向到本地 mock：断言「真实探测代码路径」仍被跑通（比原来只判非 null 更实）
        Check("TestList: 探测经本地 mock 全部连通",
            testList is { Count: > 0 } && testList.All(p => p.Ok));

        // ── 7. 端点冒烟：WebChatServer + HttpClient ──
        var web = new WayCoder.UI.Web.WebChatServer(a0, 0);
        web.Start();
        try
        {
            using var client = new HttpClient();
            var baseUrl = $"http://127.0.0.1:{web.Port}";

            var m = client.GetStringAsync(baseUrl + "/models").Result;
            Check("WebFull: GET /models 数组", Json.Parse(m)?.Kind == JKind.Array);

            var s = client.GetStringAsync(baseUrl + "/state").Result;
            Check("WebFull: GET /state 含 activeSlot", s.Contains("\"activeSlot\":"));

            var st = client.GetStringAsync(baseUrl + "/settings").Result;
            Check("WebFull: GET /settings 数组", Json.Parse(st)?.Kind == JKind.Array);

            var slotResp = client.PostAsync(baseUrl + "/slot",
                new StringContent("{\"slot\":3}", Encoding.UTF8, "application/json")).Result;
            var slotBody = slotResp.Content.ReadAsStringAsync().Result;
            Check("WebFull: POST /slot 返回历史数组", Json.Parse(slotBody)?.Kind == JKind.Array);

            // 双客户端绑不同槽位（页面作用域隔离核心：各页开始/停止只作用自己的槽位）
            var stA = client.GetStringAsync(baseUrl + "/state?client=aaa").Result;
            var stB = client.GetStringAsync(baseUrl + "/state?client=bbb").Result;
            int slotA = (int)Math.Round(Json.Parse(stA)?["activeSlot"]?.AsNumber() ?? -1);
            int slotB = (int)Math.Round(Json.Parse(stB)?["activeSlot"]?.AsNumber() ?? -1);
            Check("WebFull: 两个客户端分配不同槽位", slotA >= 0 && slotB >= 0 && slotA != slotB);

            client.PostAsync(baseUrl + "/slot?client=aaa",
                new StringContent("{\"slot\":3}", Encoding.UTF8, "application/json")).Wait();
            var stA2 = client.GetStringAsync(baseUrl + "/state?client=aaa").Result;
            var stB2 = client.GetStringAsync(baseUrl + "/state?client=bbb").Result;
            int slotA2 = (int)Math.Round(Json.Parse(stA2)?["activeSlot"]?.AsNumber() ?? -1);
            int slotB2 = (int)Math.Round(Json.Parse(stB2)?["activeSlot"]?.AsNumber() ?? -1);
            Check("WebFull: clientA 切到槽 3", slotA2 == 3);
            Check("WebFull: clientB 不受 clientA 切槽影响", slotB2 == slotB);

            var badModel = client.PostAsync(baseUrl + "/model",
                new StringContent("{\"modelId\":\"no-such\"}", Encoding.UTF8, "application/json")).Result;
            Check("WebFull: POST /model 非法模型报错", badModel.Content.ReadAsStringAsync().Result.Contains("\"ok\":false"));

            var badSetting = client.PostAsync(baseUrl + "/settings",
                new StringContent("{\"key\":\"Nope\",\"value\":\"x\"}", Encoding.UTF8, "application/json")).Result;
            Check("WebFull: POST /settings 未知项报错", badSetting.Content.ReadAsStringAsync().Result.Contains("\"ok\":false"));

            var scan = client.PostAsync(baseUrl + "/models/scan",
                new StringContent("{}", Encoding.UTF8, "application/json")).Result;
            var scanBody = scan.Content.ReadAsStringAsync().Result;
            Check("WebFull: POST /models/scan 返回 ok+results", scanBody.Contains("\"ok\":true") && scanBody.Contains("\"results\":"));
        }
        catch { Check("WebFull: 端点冒烟", false); }
        finally { web.Stop(); }
    }

    /// <summary>P4-2 Web 资源耗尽防护 + XSS：请求正文大小上限/连接上限/SSE+输入队列上限/HtmlEscape。</summary>
    private static void TestP4WebResource(Action<string, bool> Check)
    {
        // ── 1. 请求正文大小上限（纯逻辑）──
        Check("WebRes: 上限内不拒绝", !WayCoder.UI.Web.HttpServer.IsRequestTooLarge(WayCoder.UI.Web.HttpServer.MaxRequestBytes));
        Check("WebRes: 超上限拒绝", WayCoder.UI.Web.HttpServer.IsRequestTooLarge(WayCoder.UI.Web.HttpServer.MaxRequestBytes + 1));

        // ── 2. 超大 Content-Length → 413（端到端，服务端读完头立即拒绝不等待正文）──
        var bigServer = new WayCoder.UI.Web.HttpServer(0);
        bigServer.OnRequest = req => Task.FromResult<WayCoder.UI.Web.HttpResponse?>(req.Path == "/chat" ? WayCoder.UI.Web.HttpResponse.Text("ok") : null);
        bigServer.Start();
        try
        {
            using var tcp = new System.Net.Sockets.TcpClient();
            tcp.Connect("127.0.0.1", bigServer.ActualPort);
            var raw = "POST /chat HTTP/1.1\r\nHost: localhost\r\nContent-Length: 999999999\r\n\r\n";
            tcp.GetStream().Write(Encoding.UTF8.GetBytes(raw));
            var buf = new byte[4096];
            int rn = tcp.GetStream().Read(buf, 0, buf.Length);
            var resp = Encoding.UTF8.GetString(buf, 0, rn);
            Check("WebRes: 超大 Content-Length 返回 413", resp.StartsWith("HTTP/1.1 413"));
        }
        catch { Check("WebRes: 超大 Content-Length 返回 413", false); }
        finally { bigServer.Stop(); }

        // ── 3. 连接槽位上限（SemaphoreSlim 机制）──
        var capServer = new WayCoder.UI.Web.HttpServer(0);
        int got = 0;
        for (int i = 0; i < WayCoder.UI.Web.HttpServer.MaxConnections; i++)
            if (capServer.TryAcquireConnectionSlot()) got++;
        Check("WebRes: 连接槽位全部可获取", got == WayCoder.UI.Web.HttpServer.MaxConnections);
        Check("WebRes: 连接槽位满后拒绝", !capServer.TryAcquireConnectionSlot());
        capServer.ReleaseConnectionSlot();
        Check("WebRes: 释放后可再获取", capServer.TryAcquireConnectionSlot());
        // 清理占用的槽位（不留满槽位状态）
        for (int i = 0; i < WayCoder.UI.Web.HttpServer.MaxConnections; i++) capServer.ReleaseConnectionSlot();

        // ── 4. SSE 客户端 / 输入队列上限（纯逻辑）──
        Check("WebRes: SSE 未满", !WayCoder.UI.Web.WebChatServer.SseClientsFull(WayCoder.UI.Web.WebChatServer.MaxSseClients - 1));
        Check("WebRes: SSE 满", WayCoder.UI.Web.WebChatServer.SseClientsFull(WayCoder.UI.Web.WebChatServer.MaxSseClients));
        Check("WebRes: 输入队列未满", !WayCoder.UI.Web.WebChatServer.InputQueueFull(WayCoder.UI.Web.WebChatServer.MaxPendingInput - 1));
        Check("WebRes: 输入队列满", WayCoder.UI.Web.WebChatServer.InputQueueFull(WayCoder.UI.Web.WebChatServer.MaxPendingInput));

        // ── 4b. 客户端身份解析 + 槽位分配（页面作用域隔离）──
        Check("WebSlot: client 从 query 取出", WayCoder.UI.Web.WebChatServer.ParseClientQuery("client=abc123") == "abc123");
        Check("WebSlot: client 多参数排序无关", WayCoder.UI.Web.WebChatServer.ParseClientQuery("a=1&client=xyz&b=2") == "xyz");
        Check("WebSlot: client 大小写不敏感", WayCoder.UI.Web.WebChatServer.ParseClientQuery("CLIENT=Abc") == "Abc");
        Check("WebSlot: client 含 URL 编码", WayCoder.UI.Web.WebChatServer.ParseClientQuery("client=c%201%2B2") == "c 1+2");
        Check("WebSlot: 无 client 返回 null", WayCoder.UI.Web.WebChatServer.ParseClientQuery("a=1&b=2") == null);
        Check("WebSlot: 空 query 返回 null", WayCoder.UI.Web.WebChatServer.ParseClientQuery("") == null);
        Check("WebSlot: null query 返回 null", WayCoder.UI.Web.WebChatServer.ParseClientQuery(null) == null);
        Check("WebSlot: client 无值返回空串", WayCoder.UI.Web.WebChatServer.ParseClientQuery("client=") == "");
        Check("WebSlot: 空闲槽位取首个", WayCoder.UI.Web.WebChatServer.PickFreeSlot(new[] { true, false, false, false }, 4) == 1);
        Check("WebSlot: 全空取 0", WayCoder.UI.Web.WebChatServer.PickFreeSlot(new[] { false, false, false }, 3) == 0);
        Check("WebSlot: 全满回退 0", WayCoder.UI.Web.WebChatServer.PickFreeSlot(new[] { true, true, true }, 3) == 0);
        Check("WebSlot: 前段占用跳过", WayCoder.UI.Web.WebChatServer.PickFreeSlot(new[] { true, true, false, true }, 4) == 2);

        // ── 5. XSS 转义（工具名/参数注入 innerHTML 前转义）──
        Check("WebRes: HtmlEscape 脚本标签", WayCoder.UI.Web.WebChatServer.HtmlEscape("<script>alert(1)</script>") == "&lt;script&gt;alert(1)&lt;/script&gt;");
        Check("WebRes: HtmlEscape 引号", WayCoder.UI.Web.WebChatServer.HtmlEscape("\"'") == "&quot;&#39;");
        Check("WebRes: HtmlEscape 与号优先", WayCoder.UI.Web.WebChatServer.HtmlEscape("&") == "&amp;");
        Check("WebRes: HtmlEscape 空串透传", WayCoder.UI.Web.WebChatServer.HtmlEscape("") == "");
        Check("WebRes: HtmlEscape 正常文本不变", WayCoder.UI.Web.WebChatServer.HtmlEscape("echo hello") == "echo hello");
    }

    /// <summary>Web 交互桥 mock 实现（测试 AskUserQuestionTool 走桥而非 Console）。</summary>
    private sealed class MockInteraction : UxHelper.IWebInteraction
    {
        public string? SelectResult;
        public bool SelectCalled;
        public DiffConfirmResult? DiffResult;
        public bool DiffCalled;

        public Task<string?> AskAsync(string prompt, string? defaultValue, int timeoutMs) => Task.FromResult((string?)null);
        public Task<string?> SelectAsync(string title, List<string> choices, int timeoutMs) { SelectCalled = true; return Task.FromResult(SelectResult); }
        public Task<List<string>?> MultiSelectAsync(string title, List<string> choices, int timeoutMs) => Task.FromResult((List<string>?)null);
        public Task<int> ConfirmAsync(string title, string message, bool allowAll, int timeoutMs) => Task.FromResult(2);
        public Task<DiffConfirmResult?> DiffConfirmAsync(string filePath, List<DiffPreview.Hunk> hunks, int timeoutMs)
        { DiffCalled = true; return Task.FromResult(DiffResult); }
    }

    /// <summary>Web 三栏面板：SerializePanel/SerializeSessions/LspTool.ActiveSessions/交互桥/端点冒烟。</summary>
    private static void TestWebPanelSessions(Action<string, bool> Check)
    {
        // ── 1. SerializePanel（右栏六类数据）──
        var a = new Agent(new LLM("test", "sk-test"));
        var slots = new Agent?[10];
        slots[0] = a;
        var panel = Json.Parse(WayCoder.UI.Web.WebChatServer.SerializePanel(0, slots));
        Check("WebPanel: 含 todos", panel?["todos"] != null);
        Check("WebPanel: 含 tokens", panel?["tokens"] != null);
        Check("WebPanel: 含 cost", panel?["cost"] != null);
        Check("WebPanel: 含 files", panel?["files"] != null);
        Check("WebPanel: 含 mcp", panel?["mcp"] != null);
        Check("WebPanel: 含 lsp", panel?["lsp"] != null);
        Check("WebPanel: 活跃槽位 token 字段", panel?["tokens"]?["totalPrompt"] != null);
        var emptyPanel = Json.Parse(WayCoder.UI.Web.WebChatServer.SerializePanel(0, new Agent?[10]));
        Check("WebPanel: 全空槽位不抛异常", emptyPanel != null && emptyPanel["tokens"] != null);
        var oobPanel = Json.Parse(WayCoder.UI.Web.WebChatServer.SerializePanel(99, slots));
        Check("WebPanel: 越界槽位不抛异常", oobPanel != null);

        // ── 2. SerializeSessions（历史会话列表）──
        var savedId = SessionManager.SaveSession(
            new List<JNode> { JNode.Object().Set("role", "user").Set("content", "测试会话预览") },
            "test-model");
        try
        {
            var sj = WayCoder.UI.Web.WebChatServer.SerializeSessions();
            Check("WebPanel: sessions 含刚保存会话", sj.Contains(savedId));
            var parsed = Json.Parse(sj);
            bool previewOk = false;
            foreach (var it in parsed!.Items)
            {
                if (it["id"]?.AsString() == savedId)
                {
                    previewOk = it["preview"]?.AsString() == "测试会话预览";
                    break;
                }
            }
            Check("WebPanel: sessions preview 正确", previewOk);
        }
        finally { SessionManager.DeleteSession(savedId); }

        // ── 2b. 槽位隔离：各槽位各自保存/列出/加载会话，互不干扰 ──
        var slot0Id = SessionManager.SaveSession(
            new List<JNode> { JNode.Object().Set("role", "user").Set("content", "槽位0的会话") },
            "model-0", "slot0-session", 0);
        var slot1Id = SessionManager.SaveSession(
            new List<JNode> { JNode.Object().Set("role", "user").Set("content", "槽位1的会话") },
            "model-1", "slot1-session", 1);
        try
        {
            var list0 = SessionManager.ListSessions(50, 0, 0);
            var list1 = SessionManager.ListSessions(50, 0, 1);
            Check("SessionSlot: 槽位0 只含自己会话", list0.Any(s => s.Id == "slot0-session") && list0.All(s => s.Id != "slot1-session"));
            Check("SessionSlot: 槽位1 只含自己会话", list1.Any(s => s.Id == "slot1-session") && list1.All(s => s.Id != "slot0-session"));
            Check("SessionSlot: 跨槽位加载返回 null", SessionManager.LoadSession("slot0-session", 1) == null);
            Check("SessionSlot: 同槽位加载命中", SessionManager.LoadSession("slot1-session", 1) != null);
            var sj0 = WayCoder.UI.Web.WebChatServer.SerializeSessions(0);
            var sj1 = WayCoder.UI.Web.WebChatServer.SerializeSessions(1);
            Check("SessionSlot: SerializeSessions(0) 含槽位0", sj0.Contains("slot0-session") && !sj0.Contains("slot1-session"));
            Check("SessionSlot: SerializeSessions(1) 含槽位1", sj1.Contains("slot1-session") && !sj1.Contains("slot0-session"));
            // 清空单槽位只影响该槽位
            var del0 = SessionManager.DeleteAllSessions(0);
            Check("SessionSlot: 清空槽位0 至少1条", del0 >= 1);
            Check("SessionSlot: 清空后槽位1 仍保留", SessionManager.LoadSession("slot1-session", 1) != null);
            Check("SessionSlot: 清空后槽位0 会话消失", SessionManager.LoadSession("slot0-session", 0) == null);
        }
        finally
        {
            SessionManager.DeleteSession("slot0-session", 0);
            SessionManager.DeleteSession("slot1-session", 1);
        }

        // ── 2c. TUI 退出自动保存 `_auto` 按槽位隔离（`_auto` 归一化后同 ID 存各槽位子目录）──
        SessionManager.SaveSession(
            new List<JNode> { JNode.Object().Set("role", "user").Set("content", "槽位2的自动保存") },
            "model-2", "_auto", 2);
        try
        {
            Check("SessionSlot: _auto 存槽位2 可恢复", SessionManager.LoadSession("_auto", 2) != null);
            Check("SessionSlot: _auto 跨槽位不可见(槽位0)", SessionManager.LoadSession("_auto", 0) == null);
            Check("SessionSlot: _auto 跨槽位不可见(槽位3)", SessionManager.LoadSession("_auto", 3) == null);
        }
        finally
        {
            SessionManager.DeleteSession("_auto", 2);
        }

        // ── 2d. 会话网关元数据 + 空 model 遗留兼容（修复 code-review 项）──
        var gwId = SessionManager.SaveSession(
            new List<JNode> { JNode.Object().Set("role", "user").Set("content", "网关会话") },
            "test-model", "gw-session", -1, "deepseek", "https://api.deepseek.com");
        try
        {
            var gwRec = SessionManager.LoadSessionDetailed("gw-session");
            Check("SessionMeta: provider 元数据落盘", gwRec?.Provider == "deepseek");
            Check("SessionMeta: base_url 元数据落盘", gwRec?.BaseUrl == "https://api.deepseek.com");
        }
        finally { SessionManager.DeleteSession("gw-session"); }

        // 空 model 遗留会话（旧格式 "model":""）不应被判 null——消息必须可恢复，Model 字段为空由调用方跳过赋值
        var emptyModelId = SessionManager.SaveSession(
            new List<JNode> { JNode.Object().Set("role", "user").Set("content", "空模型会话") },
            "", "emptymodel-session");
        try
        {
            var emptyLoaded = SessionManager.LoadSession("emptymodel-session");
            Check("SessionMeta: 空 model 遗留会话仍加载消息", emptyLoaded is { Messages.Count: 1 });
            Check("SessionMeta: 空 model 回读为空串", emptyLoaded is { Model.Length: 0 });
        }
        finally { SessionManager.DeleteSession("emptymodel-session"); }

        // provider 推导（ResolveProviderForModel：目录精确匹配 / 无地址用模型默认 / 无法判定 null）
        var dm = ModelCatalog.Find("deepseek-v4-flash");
        if (dm != null)
        {
            Check("SessionMeta: provider 精确匹配(地址)", ModelCatalog.ResolveProviderForModel(dm.Id, dm.DefaultBaseUrl) == dm.ProviderId);
            Check("SessionMeta: provider 无地址用模型默认", ModelCatalog.ResolveProviderForModel(dm.Id, null) == dm.ProviderId);
        }
        Check("SessionMeta: provider 无法判定返回 null",
            ModelCatalog.ResolveProviderForModel("nonexistent-model-xyz", "https://nonexistent.invalid") == null);

        // ── 3. LspTool.ActiveSessions 访问器（空态不抛异常）──
        var lspSessions = LspTool.ActiveSessions;
        Check("WebPanel: LspTool.ActiveSessions 返回列表", lspSessions != null);
        Check("WebPanel: 空 LSP 会话不抛异常", lspSessions!.Count >= 0);

        // ── 4. 交互桥：AskUserQuestionTool 走 WebInteraction 而非 Console ──
        var mock = new MockInteraction { SelectResult = "A  —  desc A" };
        UxHelper.WebInteraction = mock;
        try
        {
            var tool = new AskUserQuestionTool();
            var q = JNode.Object().Set("question", "选哪个").Set("header", "choice")
                .Set("options", JNode.Array().Add(JNode.Object().Set("label", "A").Set("description", "desc A")));
            var args = new Dictionary<string, object?> { ["questions"] = JNode.Array().Add(q) };
            var result = tool.ExecuteAsync(args).Result;
            Check("WebAsk: AskUserQuestionTool 走桥", mock.SelectCalled);
            var resNode = Json.Parse(result);
            Check("WebAsk: 单选 label 解析正确", resNode?["choice"]?.AsString() == "A");
        }
        finally { UxHelper.WebInteraction = null; }

        // ── 4b. YOLO 模式仍应弹窗询问（畅通只跳过权限确认，不跳过向用户提问）──
        // 若 tool 在 YOLO 下直接自动选第一项，SelectCalled 恒为 false —— 此测试即失败，专捕该回归。
        var oldAskMode = PermissionManager.CurrentMode;
        try
        {
            PermissionManager.CurrentMode = PermissionManager.Mode.Yolo;
            var mockYolo = new MockInteraction { SelectResult = "A  —  desc A" };
            UxHelper.WebInteraction = mockYolo;
            try
            {
                var tool = new AskUserQuestionTool();
                var qy = JNode.Object().Set("question", "选哪个").Set("header", "choice")
                    .Set("options", JNode.Array().Add(JNode.Object().Set("label", "A").Set("description", "desc A")));
                var argsY = new Dictionary<string, object?> { ["questions"] = JNode.Array().Add(qy) };
                var resultY = tool.ExecuteAsync(argsY).Result;
                Check("WebAsk: YOLO 下仍走交互桥弹窗", mockYolo.SelectCalled);
                Check("WebAsk: YOLO 下取用户所选而非默认首项", Json.Parse(resultY)?["choice"]?.AsString() == "A");
            }
            finally { UxHelper.WebInteraction = null; }
        }
        finally { PermissionManager.CurrentMode = oldAskMode; }

        // ── 5. 端点冒烟：/panel /sessions /sessions/load /answer ──
        var web = new WayCoder.UI.Web.WebChatServer(a, 0);
        web.Start();
        try
        {
            using var client = new HttpClient();
            var baseUrl = $"http://127.0.0.1:{web.Port}";

            var p = Json.Parse(client.GetStringAsync(baseUrl + "/panel").Result);
            Check("WebPanel: GET /panel 六字段", p?["todos"] != null && p?["lsp"] != null);

            var ss = Json.Parse(client.GetStringAsync(baseUrl + "/sessions").Result);
            Check("WebPanel: GET /sessions 数组", ss?.Kind == JKind.Array);

            var loadResp = client.PostAsync(baseUrl + "/sessions/load",
                new StringContent("{\"id\":\"no-such-session-xyz\"}", Encoding.UTF8, "application/json")).Result;
            Check("WebPanel: POST /sessions/load 非法 id 报错",
                loadResp.Content.ReadAsStringAsync().Result.Contains("\"ok\":false"));

            var ansResp = client.PostAsync(baseUrl + "/answer",
                new StringContent("{\"requestId\":\"999999\",\"value\":\"x\"}", Encoding.UTF8, "application/json")).Result;
            Check("WebPanel: POST /answer 无匹配报错",
                ansResp.Content.ReadAsStringAsync().Result.Contains("\"ok\":false"));

            // /permit 权限模式切换（确认轴）：设置后 state 反映，且恢复 ask 避免污染其它测试
            var permitResp = client.PostAsync(baseUrl + "/permit",
                new StringContent("{\"mode\":\"yolo\"}", Encoding.UTF8, "application/json")).Result;
            Check("WebPanel: POST /permit 成功", permitResp.Content.ReadAsStringAsync().Result.Contains("\"ok\":true"));
            var stAfterPermit = client.GetStringAsync(baseUrl + "/state").Result;
            Check("WebPanel: /permit 后 state 反映 yolo", stAfterPermit.Contains("\"permMode\":\"yolo\""));
            var permitBad = client.PostAsync(baseUrl + "/permit",
                new StringContent("{}", Encoding.UTF8, "application/json")).Result;
            Check("WebPanel: POST /permit 缺 mode 报错", permitBad.Content.ReadAsStringAsync().Result.Contains("\"ok\":false"));
            client.PostAsync(baseUrl + "/permit",
                new StringContent("{\"mode\":\"ask\"}", Encoding.UTF8, "application/json")).Wait();

            // /perm 沙箱边界切换（边界轴，独立于权限）
            var permResp = client.PostAsync(baseUrl + "/perm",
                new StringContent("{\"mode\":\"hard\"}", Encoding.UTF8, "application/json")).Result;
            Check("WebPanel: POST /perm 成功", permResp.Content.ReadAsStringAsync().Result.Contains("\"ok\":true"));
            client.PostAsync(baseUrl + "/perm",
                new StringContent("{\"mode\":\"off\"}", Encoding.UTF8, "application/json")).Wait();
        }
        catch { Check("WebPanel: 端点冒烟", false); }
        finally { web.Stop(); }
    }

    /// <summary>Web 斜杠命令：HandleCommand 纯函数 + /command 端点冒烟 + HTML 结构。</summary>
    private static void TestWebCommands(Action<string, bool> Check)
    {
        var a = new Agent(new LLM("test", "sk-test"));

        // ── 1. HandleCommand 纯函数 ──
        var (hHelp, oHelp) = WayCoder.UI.Web.WebChatServer.HandleCommand("/help", a);
        Check("WebCmd: /help 处理", hHelp && oHelp.Contains("Web 命令"));

        var (hPerm, oPerm) = WayCoder.UI.Web.WebChatServer.HandleCommand("/perm", a);
        Check("WebCmd: /perm 无参显示边界", hPerm && oPerm.Contains("沙箱边界"));

        var (hPermSet, oPermSet) = WayCoder.UI.Web.WebChatServer.HandleCommand("/perm hard", a);
        Check("WebCmd: /perm hard 切换", hPermSet && oPermSet.Contains("已切换"));
        WayCoder.UI.Web.WebChatServer.HandleCommand("/perm off", a); // 恢复默认

        var (hPermit, oPermit) = WayCoder.UI.Web.WebChatServer.HandleCommand("/permit", a);
        Check("WebCmd: /permit 无参显示权限", hPermit && oPermit.Contains("权限模式"));

        var (hModelList, oModelList) = WayCoder.UI.Web.WebChatServer.HandleCommand("/model list", a);
        Check("WebCmd: /model list 列模型", hModelList && oModelList.Contains("模型列表"));

        var (hModel, _) = WayCoder.UI.Web.WebChatServer.HandleCommand("/model", a);
        Check("WebCmd: /model 无参不处理（前端弹窗）", !hModel);

        var (hReset, _) = WayCoder.UI.Web.WebChatServer.HandleCommand("/reset", a);
        Check("WebCmd: /reset 处理", hReset);

        var (hTokens, oTokens) = WayCoder.UI.Web.WebChatServer.HandleCommand("/tokens", a);
        Check("WebCmd: /tokens 统计", hTokens && oTokens.Contains("Token"));

        var (hMcp, _) = WayCoder.UI.Web.WebChatServer.HandleCommand("/mcp", a);
        Check("WebCmd: /mcp 状态", hMcp);

        var (hTodo, _) = WayCoder.UI.Web.WebChatServer.HandleCommand("/todo", a);
        Check("WebCmd: /todo 任务", hTodo);

        var (hUnknown, _) = WayCoder.UI.Web.WebChatServer.HandleCommand("/foobar-xyz", a);
        Check("WebCmd: 未知命令不处理", !hUnknown);

        var (hPlain, _) = WayCoder.UI.Web.WebChatServer.HandleCommand("hello world", a);
        Check("WebCmd: 非斜杠不处理", !hPlain);

        var (hNull, oNull) = WayCoder.UI.Web.WebChatServer.HandleCommand("/tokens", null);
        Check("WebCmd: 空槽位 /tokens 提示", hNull && oNull.Contains("无活跃槽位"));

        // ── 2. /command 端点冒烟 ──
        var web = new WayCoder.UI.Web.WebChatServer(a, 0);
        web.Start();
        try
        {
            using var client = new HttpClient();
            var baseUrl = $"http://127.0.0.1:{web.Port}";

            var helpResp = client.PostAsync(baseUrl + "/command",
                new StringContent("{\"input\":\"/help\"}", Encoding.UTF8, "application/json")).Result;
            var helpBody = helpResp.Content.ReadAsStringAsync().Result;
            Check("WebCmd: POST /command /help 处理", helpBody.Contains("\"handled\":true") && helpBody.Contains("Web 命令"));

            var unknownResp = client.PostAsync(baseUrl + "/command",
                new StringContent("{\"input\":\"/nope-xyz\"}", Encoding.UTF8, "application/json")).Result;
            Check("WebCmd: POST /command 未知回退", unknownResp.Content.ReadAsStringAsync().Result.Contains("\"handled\":false"));

            var interruptResp = client.PostAsync(baseUrl + "/command",
                new StringContent("{\"input\":\"/interrupt\"}", Encoding.UTF8, "application/json")).Result;
            Check("WebCmd: POST /command /interrupt 处理", interruptResp.Content.ReadAsStringAsync().Result.Contains("\"handled\":true"));

            var missingResp = client.PostAsync(baseUrl + "/command",
                new StringContent("{}", Encoding.UTF8, "application/json")).Result;
            Check("WebCmd: POST /command 缺 input 报错", missingResp.Content.ReadAsStringAsync().Result.Contains("\"ok\":false"));
        }
        catch { Check("WebCmd: 端点冒烟", false); }
        finally { web.Stop(); }

        // ── 3. HTML 含斜杠命令路由 ──
        var html = WayCoder.UI.Web.WebAssets.Html;
        Check("WebCmd: HTML 含 handleUiCommand", html.Contains("function handleUiCommand"));
        Check("WebCmd: HTML 含 /command 路由", html.Contains("/command"));
        Check("WebCmd: HTML 含 cmd 样式", html.Contains(".msg.cmd"));
    }

    /// <summary>Web 特殊前缀输入 + 中间格式渲染：SerializeFileList 纯函数 + /test 分支 + /shell//fileref//filelist 端点冒烟 + 前端渲染器结构。</summary>
    private static void TestWebPrefixInput(Action<string, bool> Check)
    {
        var a = new Agent(new LLM("test", "sk-test"));

        // ── 1. SerializeFileList 纯函数 ──
        var all = Json.Parse(WayCoder.UI.Web.WebChatServer.SerializeFileList(""));
        Check("Prefix: 空前缀返回 JSON 数组", all?.Kind == JKind.Array);
        if (all != null && all.Kind == JKind.Array)
        {
            Check("Prefix: 条目数 ≤ 40", all.Count <= 40);
            bool shapeOk = true, dirFirst = true;
            var seenFile = false;
            foreach (var it in all.Items)
            {
                var isDir = it.GetBool("isDir");
                var name = it.GetString("name") ?? "";
                if (isDir && !name.EndsWith("/")) shapeOk = false;
                if (!isDir && name.EndsWith("/")) shapeOk = false;
                if (isDir && seenFile) dirFirst = false;
                if (!isDir) seenFile = true;
            }
            Check("Prefix: 目录项带 / 后缀且 isDir 一致", shapeOk);
            Check("Prefix: 目录项排在文件项前", dirFirst);
        }

        var noMatch = Json.Parse(WayCoder.UI.Web.WebChatServer.SerializeFileList("__waycoder_no_such_prefix_xyz__"));
        Check("Prefix: 无匹配前缀返回空数组", noMatch?.Kind == JKind.Array && noMatch.Count == 0);

        // ── 1b. ResolveWithinRoot 路径穿越防护 ──
        var cwd = Directory.GetCurrentDirectory();
        var innerFile = Path.Combine(cwd, "__waycoder_inner__.tmp");
        var inner = WayCoder.UI.Web.WebChatServer.ResolveWithinRoot(innerFile);
        Check("Root: 项目内路径放行", inner != null && Path.GetFullPath(inner!) == Path.GetFullPath(innerFile));
        Check("Root: ../ 穿越返回 null", WayCoder.UI.Web.WebChatServer.ResolveWithinRoot(Path.Combine(cwd, "..", "..", "etc", "passwd")) == null);
        Check("Root: 绝对越界路径返回 null", WayCoder.UI.Web.WebChatServer.ResolveWithinRoot(Path.GetTempPath()) == null);
        Check("Root: 根目录本身放行", WayCoder.UI.Web.WebChatServer.ResolveWithinRoot(cwd) != null);

        // ── 1c. IsCrossSite CSRF 兜底 ──
        Check("CSRF: cross-site 判跨站", WayCoder.UI.Web.WebChatServer.IsCrossSite("cross-site"));
        Check("CSRF: same-origin 非跨站", !WayCoder.UI.Web.WebChatServer.IsCrossSite("same-origin"));
        Check("CSRF: none 非跨站", !WayCoder.UI.Web.WebChatServer.IsCrossSite("none"));
        Check("CSRF: 空头非跨站（curl 放行）", !WayCoder.UI.Web.WebChatServer.IsCrossSite(null));

        // ── 2. HandleCommand /test 中间格式分支 ──
        var (hMarkup, oMarkup) = WayCoder.UI.Web.WebChatServer.HandleCommand("/test markup", a);
        Check("WebTest: /test markup 中间格式", hMarkup && oMarkup.Contains("«red»") && oMarkup.Contains("中间格式"));

        var (hColor, oColor) = WayCoder.UI.Web.WebChatServer.HandleCommand("/test color", a);
        Check("WebTest: /test color 别名=markup", hColor && oColor.Contains("«green»"));

        var (hStyle, oStyle) = WayCoder.UI.Web.WebChatServer.HandleCommand("/test 样式", a);
        Check("WebTest: /test 样式 中文别名", hStyle && oStyle.Contains("«bold»"));

        var (hMid, _) = WayCoder.UI.Web.WebChatServer.HandleCommand("/test 中间", a);
        Check("WebTest: /test 中间 中文别名", hMid);

        var (hTable, oTable) = WayCoder.UI.Web.WebChatServer.HandleCommand("/test table", a);
        Check("WebTest: /test table 对齐冒号", hTable && oTable.Contains("对齐冒号") && oTable.Contains("---:") && oTable.Contains(":---:"));

        var (hTableCn, oTableCn) = WayCoder.UI.Web.WebChatServer.HandleCommand("/test 表格", a);
        Check("WebTest: /test 表格 中文别名", hTableCn && oTableCn.Contains("对齐冒号"));

        var (hList, oList) = WayCoder.UI.Web.WebChatServer.HandleCommand("/test list", a);
        Check("WebTest: /test list 列表", hList && oList.Contains("可用测试项"));

        var (hEmpty, oEmpty) = WayCoder.UI.Web.WebChatServer.HandleCommand("/test", a);
        Check("WebTest: /test 无参=列表", hEmpty && oEmpty.Contains("可用测试项"));

        var (hUnknown, oUnknown) = WayCoder.UI.Web.WebChatServer.HandleCommand("/test blah-xyz", a);
        Check("WebTest: /test 未知提示", hUnknown && oUnknown.Contains("未知测试项"));

        // ── 3. 端点冒烟：/shell //fileref //filelist ──
        var web = new WayCoder.UI.Web.WebChatServer(a, 0);
        web.Start();
        try
        {
            using var client = new HttpClient();
            var baseUrl = $"http://127.0.0.1:{web.Port}";

            var shellBody = JNode.Object().Set("command", "echo hello-prefix").ToJson();
            var shellResp = client.PostAsync(baseUrl + "/shell",
                new StringContent(shellBody, Encoding.UTF8, "application/json")).Result;
            var shellTxt = shellResp.Content.ReadAsStringAsync().Result;
            Check("Prefix: POST /shell 执行回显", shellTxt.Contains("\"ok\":true") && shellTxt.Contains("hello-prefix"));

            var shellBad = client.PostAsync(baseUrl + "/shell",
                new StringContent("{}", Encoding.UTF8, "application/json")).Result;
            Check("Prefix: POST /shell 缺 command 报错", shellBad.Content.ReadAsStringAsync().Result.Contains("\"ok\":false"));

            var flResp = client.PostAsync(baseUrl + "/filelist",
                new StringContent(JNode.Object().Set("prefix", "").ToJson(), Encoding.UTF8, "application/json")).Result;
            var flTxt = flResp.Content.ReadAsStringAsync().Result;
            Check("Prefix: POST /filelist 返回 files 数组", flTxt.Contains("\"ok\":true") && flTxt.Contains("\"files\""));

            // 项目内文件可读，项目外文件被路径穿越防护拒绝
            var tmpIn = Path.Combine(Directory.GetCurrentDirectory(), "__waycoder_ref_test__.tmp");
            File.WriteAllText(tmpIn, "file-ref-content-123");
            var frResp = client.PostAsync(baseUrl + "/fileref",
                new StringContent(JNode.Object().Set("path", tmpIn).ToJson(), Encoding.UTF8, "application/json")).Result;
            var frTxt = frResp.Content.ReadAsStringAsync().Result;
            Check("Prefix: POST /fileref 读取注入", frTxt.Contains("\"ok\":true") && frTxt.Contains("file-ref-content-123"));
            File.Delete(tmpIn);

            var tmpOut = Path.GetTempFileName();
            File.WriteAllText(tmpOut, "outside-root");
            var frEscape = client.PostAsync(baseUrl + "/fileref",
                new StringContent(JNode.Object().Set("path", tmpOut).ToJson(), Encoding.UTF8, "application/json")).Result;
            var frEscapeTxt = frEscape.Content.ReadAsStringAsync().Result;
            Check("Prefix: POST /fileref 越界拒绝", frEscapeTxt.Contains("\"ok\":false") && frEscapeTxt.Contains("项目根目录"));
            File.Delete(tmpOut);
        }
        catch { Check("Prefix: 端点冒烟", false); }
        finally { web.Stop(); }

        // ── 4. 前端渲染器结构（markupToHtml / ansiToHtml / splitRow / 表格对齐）──
        var html = WayCoder.UI.Web.WebAssets.Html;
        Check("Prefix: HTML 含 markupToHtml", html.Contains("function markupToHtml"));
        Check("Prefix: HTML 含 ansiToHtml", html.Contains("function ansiToHtml"));
        Check("Prefix: HTML 含 splitRow", html.Contains("function splitRow"));
        Check("Prefix: HTML 含 MARKUP_STYLES", html.Contains("MARKUP_STYLES"));
        Check("Prefix: HTML 含表格对齐 text-align", html.Contains("text-align:"));

        // shell 类工具（bash/ps/git…）输出带裸 ANSI，Web 端必须解码：此前工具气泡只走 markupToHtml，
        // ESC 序列被当正文印出 → 整个气泡一坨乱码。`!命令` 走 addShellOutput→ansiToHtml 所以正常，
        // 两条路显示不一致正是用户看到的现象。这里钉住分支存在、且**排在 diff/markdown 之前**。
        // 按大括号配平取函数体（不要用固定长度窗口 —— 函数一长就截断，断言会静默失效）
        static string JsBody(string source, string name)
        {
            var i = source.IndexOf("function " + name, StringComparison.Ordinal);
            if (i < 0) return "";
            var b = source.IndexOf('{', i);
            if (b < 0) return "";
            int depth = 0;
            for (int j = b; j < source.Length; j++)
            {
                if (source[j] == '{') depth++;
                else if (source[j] == '}') { depth--; if (depth == 0) return source.Substring(i, j - i + 1); }
            }
            return "";
        }
        var toolFn = JsBody(html, "renderToolOutput");
        Check("Prefix: renderToolOutput 含 ANSI 分支（ESC → ansiToHtml）",
            toolFn.Contains("ansiToHtml") && toolFn.Contains("indexOf('\\x1b')"));
        Check("Prefix: ANSI 分支排在 diff/markdown 判别之前",
            toolFn.Contains("ansiToHtml") && toolFn.Contains("highlightDiff")
            && toolFn.IndexOf("ansiToHtml", StringComparison.Ordinal)
               < toolFn.IndexOf("highlightDiff", StringComparison.Ordinal));

        // ── 流式追加不得让主线程卡死（「页面无响应」）──
        // 旧路径每个 token 一次 `el.textContent += s`（重建整块文本节点）+ 紧跟 `scroll()`（读
        // scrollHeight 强制同步重排）：一次 2000 token 的回复 = 2000 次整块重建 + 2000 次整页重排。
        // 纯 JS 各步实测都不贵（见 scripts/_bench_web_render.cjs），贵的就是这两处 DOM 操作。
        // 修复：文本节点 appendData（只追加新片段）+ 滚动合帧（每帧至多一次）+ 历史重放分帧。
        var appendFn = JsBody(html, "appendCapped");
        Check("Prefix: 流式追加走文本节点 appendData（不整块重建文本）",
            html.Contains("function streamTextNode") && appendFn.Contains("node.appendData(s)")
            && !appendFn.Contains("textContent +="));
        Check("Prefix: 滚动合帧 + 自动跟底闸门",
            html.Contains("function scheduleScroll")
            && html.Contains("if (followBottom) messages.scrollTop = messages.scrollHeight"));
        var tokenFn = JsBody(html, "handleToken");
        Check("Prefix: 逐 token 路径不再直接 scroll()", tokenFn.Length > 0 && !tokenFn.Contains("scroll();"));
        // ⚠ `«/»` 是**所有** «» 标记（颜色…）的统一结束符，不是思考块专用：
        // 不在思考块里必须**原样保留**（否则标记失配、颜色错位），更不能把正文当思考吞掉 ——
        // 曾经无条件 thinkAppend，用户实测「完全不聊天了，所有内容都是已思考 n 秒」。
        Check("Prefix: 只在思考块内吃 «/»，否则原样留给 Markdown 配对",
            tokenFn.Contains("else if (think) { endThink();") && tokenFn.Contains("emitTokenPiece('«/»')"));
        var histFn = JsBody(html, "renderHistoryChunked");
        Check("Prefix: 历史重放分帧（每帧 15 条 + rAF 续帧）",
            histFn.Contains("requestAnimationFrame(step)") && histFn.Contains("i + 15")
            && html.Contains("renderHistoryChunked(list);"));
        // ── 折叠：思考一行 / 工具调用一行（点开弹详情浮层）──
        // 对齐 MAUI（ChatPage.xaml.cs 的 thinkMsg/_toolGroup/interruptSinceTool）：
        // 聊天流里只留一行，明细（可能几十万字符）只在点开时渲染一次。
        // 这同时把「工具输出逐 chunk 写 DOM」这条最重的路径整个拿掉了。
        var toolOutFn = JsBody(html, "onToolOutput");
        Check("Prefix: 工具输出只进内存、不写 DOM（折叠的核心收益）",
            toolOutFn.Contains("it.out =") && !toolOutFn.Contains("innerHTML")
            && !toolOutFn.Contains("appendChild"));
        var toolStartFn = JsBody(html, "onToolStart");
        Check("Prefix: 工具调用折叠成一行 + 分组判据（interruptSinceTool → 新开一组）",
            toolStartFn.Contains("工具调用:")
            && toolStartFn.Contains("interruptSinceTool")
            && toolStartFn.Contains("endSeg()"));
        var thinkFn = JsBody(html, "thinkAppend");
        var endThinkFn = JsBody(html, "endThink");
        Check("Prefix: 思考折叠成一行（思考中 Ns → 已思考 N 秒）",
            thinkFn.Contains("思考中 ") && endThinkFn.Contains("已思考 "));
        Check("Prefix: raw 文本先剥 «» 内部标记再上色",
            html.Contains("function stripMarkupTags")
            && html.Contains("ansiToHtml(stripMarkupTags(text))"));
        // 详情浮层：预算按项均分（对齐 MAUI ShareFor）+ 搜索过滤 + 点开才渲染
        var detailFn = JsBody(html, "detailItemHtml");
        Check("Prefix: 详情浮层按服务端 raw 标记渲染工具输出",
            detailFn.Contains("renderToolOutput(out, it.raw)"));
        Check("Prefix: 详情输出按项预算均分（不吃满 DOM）",
            html.Contains("DETAIL_TOTAL_BUDGET") && detailFn.Contains("perItem"));
        Check("Prefix: 详情浮层可搜索（工具按项过滤 / 思考按行过滤）",
            html.Contains("function renderDetailBody") && html.Contains("function showToolDetail")
            && JsBody(html, "showThinkDetail").Contains("indexOf(q) >= 0"));
        Check("Prefix: 一轮结束收口（finishRound 定稿思考/正文、解绑组）",
            JsBody(html, "finishRound").Contains("endThink()")
            && html.Contains("es.addEventListener('done', () => { setBusy(false); finishRound();"));
        // raw 标记必须真的发到浏览器（否则前端只能猜）：bash→true、read_file→false
        var toolEvBash = WayCoder.UI.Web.WebChatServer.JsonTool("bash", "ls");
        var toolEvRead = WayCoder.UI.Web.WebChatServer.JsonTool("read_file", "a.cs");
        Check("Prefix: tool 事件带 raw 标记（bash=true / read_file=false）",
            toolEvBash.Contains("\"raw\":true") && toolEvRead.Contains("\"raw\":false"));
        // 显示名单独发：前端显示 `edit(main.c)`，而按真实名的判断（raw）仍走 name
        Check("Prefix: tool 事件带显示名 short（edit_file→edit）",
            WayCoder.UI.Web.WebChatServer.JsonTool("edit_file", "main.c").Contains("\"short\":\"edit\""));
        // `!` 直通气泡版式（工具输出气泡已折叠，只剩它一个等宽气泡）
        var shellCss = html.Substring(Math.Max(0, html.IndexOf(".shell-output", StringComparison.Ordinal)), 400);
        Check("Prefix: shell 气泡等宽 + pre-wrap + 13px",
            shellCss.Contains("ui-monospace") && shellCss.Contains("pre-wrap") && shellCss.Contains("font-size:13px"));
        // 服务端超时必须让前端把模态收掉（否则浮层永远挂着 = 用户眼里的「卡死」）
        Check("Prefix: ask 超时广播 ask_closed + 前端收口",
            html.Contains("ask_closed") && html.Contains("该提问已超时，已按默认继续"));
    }

    /// <summary>Web Diff 预览：ParseDiffAnswer/SerializeHunks 纯函数 + DiffPreview.Show Web 分支。</summary>
    private static void TestWebDiffPreview(Action<string, bool> Check)
    {
        // ── 1. ParseDiffAnswer 纯函数 ──
        var acc = WayCoder.UI.Web.WebChatServer.ParseDiffAnswer("{\"decision\":\"accept\"}");
        Check("WebDiff: accept → AcceptAll", acc != null && acc.Decision == DiffPreview.Decision.AcceptAll && acc.AcceptedHunks == null);

        var rej = WayCoder.UI.Web.WebChatServer.ParseDiffAnswer("{\"decision\":\"reject\"}");
        Check("WebDiff: reject → RejectAll", rej != null && rej.Decision == DiffPreview.Decision.RejectAll);

        var part = WayCoder.UI.Web.WebChatServer.ParseDiffAnswer("{\"decision\":\"partial\",\"accepted\":[0,2]}");
        Check("WebDiff: partial → Partial + 索引集", part != null && part.Decision == DiffPreview.Decision.Partial
            && part.AcceptedHunks != null && part.AcceptedHunks.SetEquals(new HashSet<int> { 0, 2 }));

        var partEmpty = WayCoder.UI.Web.WebChatServer.ParseDiffAnswer("{\"decision\":\"partial\",\"accepted\":[]}");
        Check("WebDiff: partial 空集", partEmpty != null && partEmpty.Decision == DiffPreview.Decision.Partial
            && partEmpty.AcceptedHunks != null && partEmpty.AcceptedHunks.Count == 0);

        Check("WebDiff: null → null", WayCoder.UI.Web.WebChatServer.ParseDiffAnswer(null) == null);
        Check("WebDiff: 空串 → null", WayCoder.UI.Web.WebChatServer.ParseDiffAnswer("") == null);
        Check("WebDiff: 非法 JSON → null", WayCoder.UI.Web.WebChatServer.ParseDiffAnswer("not json") == null);
        Check("WebDiff: 未知 decision → RejectAll", WayCoder.UI.Web.WebChatServer.ParseDiffAnswer("{\"decision\":\"huh\"}")?.Decision == DiffPreview.Decision.RejectAll);

        // ── 2. SerializeHunks 纯函数 ──
        var hunks = DiffPreview.BuildHunks("line1\nline2\n", "line1\nCHANGED\nline2\n");
        Check("WebDiff: BuildHunks 产出 hunk", hunks.Count >= 1);
        var hunksNode = Json.Parse(WayCoder.UI.Web.WebChatServer.SerializeHunks(hunks));
        Check("WebDiff: SerializeHunks 是数组", hunksNode?.Kind == JKind.Array);
        bool hunkValid = hunksNode != null && hunksNode.Kind == JKind.Array && hunksNode.Items.Any();
        if (hunkValid)
        {
            var first = hunksNode!.Items.First();
            Check("WebDiff: hunk 含 header", first["header"] != null);
            Check("WebDiff: hunk 含 lines 数组", first["lines"]?.Kind == JKind.Array);
            bool hasDelOrAdd = first["lines"]!.Items.Any(l => l["kind"]?.AsString() == "-" || l["kind"]?.AsString() == "+");
            Check("WebDiff: hunk 行含 +/- 标记", hasDelOrAdd);
        }
        else Check("WebDiff: hunk 结构有效", false);

        // ── 3. DiffPreview.Show Web 分支（mock 桥，不阻塞 Console）──
        var mock = new MockInteraction();
        UxHelper.WebInteraction = mock;
        try
        {
            var old = "a\nb\nc\n";
            var nw = "a\nB\nc\n";

            mock.DiffResult = new DiffConfirmResult { Decision = DiffPreview.Decision.AcceptAll };
            var r = DiffPreview.Show(old, nw, "test.cs");
            Check("WebDiff: Show 走 Web 桥 AcceptAll", r.Decision == DiffPreview.Decision.AcceptAll);
            Check("WebDiff: Show 调用 DiffConfirmAsync", mock.DiffCalled);

            mock.DiffResult = new DiffConfirmResult { Decision = DiffPreview.Decision.Partial, AcceptedHunks = new HashSet<int> { 0 } };
            var rp = DiffPreview.Show(old, nw, "test.cs");
            Check("WebDiff: Show Partial 返回索引集", rp.Decision == DiffPreview.Decision.Partial
                && rp.AcceptedHunks != null && rp.AcceptedHunks.SetEquals(new HashSet<int> { 0 }));

            mock.DiffResult = null;
            var rn = DiffPreview.Show(old, nw, "test.cs");
            Check("WebDiff: Show null（取消/超时）→ RejectAll", rn.Decision == DiffPreview.Decision.RejectAll);

            // 无实际变更时即使 Web 桥存在也应直接放行（不弹框）
            mock.DiffCalled = false;
            var rSame = DiffPreview.Show(old, old, "test.cs");
            Check("WebDiff: 无变更直接放行且不弹框", rSame.Decision == DiffPreview.Decision.AcceptAll && !mock.DiffCalled);
        }
        finally { UxHelper.WebInteraction = null; }
    }

    private static void TestWebUpload(Action<string, bool> Check)
    {
        // ── 1. ParseUploadKind 纯函数 ──
        Check("WebUp: kind=image → image", WayCoder.UI.Web.WebChatServer.ParseUploadKind("kind=image") == "image");
        Check("WebUp: kind=audio → audio", WayCoder.UI.Web.WebChatServer.ParseUploadKind("kind=audio") == "audio");
        Check("WebUp: 大小写不敏感 → image", WayCoder.UI.Web.WebChatServer.ParseUploadKind("kind=IMAGE") == "image");
        Check("WebUp: 非法 kind → null", WayCoder.UI.Web.WebChatServer.ParseUploadKind("kind=huh") == null);
        Check("WebUp: 缺少 kind → null", WayCoder.UI.Web.WebChatServer.ParseUploadKind("a=1") == null);
        Check("WebUp: null → null", WayCoder.UI.Web.WebChatServer.ParseUploadKind(null) == null);
        Check("WebUp: 空串 → null", WayCoder.UI.Web.WebChatServer.ParseUploadKind("") == null);

        // ── 2. IsImageExtension 纯函数 ──
        Check("WebUp: png 是图片", WayCoder.UI.Web.WebChatServer.IsImageExtension("png"));
        Check("WebUp: .jpg 是图片", WayCoder.UI.Web.WebChatServer.IsImageExtension(".jpg"));
        Check("WebUp: JPG 是图片", WayCoder.UI.Web.WebChatServer.IsImageExtension("JPG"));
        Check("WebUp: txt 非图片", !WayCoder.UI.Web.WebChatServer.IsImageExtension("txt"));
        Check("WebUp: 空非图片", !WayCoder.UI.Web.WebChatServer.IsImageExtension(""));

        // ── 3. SafeExtension 纯函数 ──
        Check("WebUp: a.png → png", WayCoder.UI.Web.WebChatServer.SafeExtension("a.png", "image") == "png");
        Check("WebUp: a.PNG → png", WayCoder.UI.Web.WebChatServer.SafeExtension("a.PNG", "image") == "png");
        Check("WebUp: .JPG → jpg", WayCoder.UI.Web.WebChatServer.SafeExtension(".JPG", "image") == "jpg");
        Check("WebUp: 图片缺扩展回退 png", WayCoder.UI.Web.WebChatServer.SafeExtension("", "image") == "png");
        Check("WebUp: 音频缺扩展回退 bin", WayCoder.UI.Web.WebChatServer.SafeExtension("", "audio") == "bin");
        Check("WebUp: 无扩展回退 png", WayCoder.UI.Web.WebChatServer.SafeExtension("noext", "image") == "png");
        Check("WebUp: 超长扩展回退 png", WayCoder.UI.Web.WebChatServer.SafeExtension("a.verylongextension", "image") == "png");

        // ── 4. IsTranscribeError 纯函数 ──
        Check("WebUp: 错误前缀", WayCoder.UI.Web.WebChatServer.IsTranscribeError("错误：无 API Key"));
        Check("WebUp: 转录失败前缀", WayCoder.UI.Web.WebChatServer.IsTranscribeError("转录失败"));
        Check("WebUp: 转录出错前缀", WayCoder.UI.Web.WebChatServer.IsTranscribeError("转录出错"));
        Check("WebUp: 空文本前缀", WayCoder.UI.Web.WebChatServer.IsTranscribeError("转录返回空文本"));
        Check("WebUp: 正常内容非错误", !WayCoder.UI.Web.WebChatServer.IsTranscribeError("你好，这是转录结果"));
        Check("WebUp: 空串非错误", !WayCoder.UI.Web.WebChatServer.IsTranscribeError(""));

        // ── 5. ParseHttpRequest(byte[]) 二进制正文（RawBody 保留原始字节）──
        var header = Encoding.UTF8.GetBytes("POST /upload?kind=image HTTP/1.1\r\nContent-Length: 4\r\nX-File-Name: a.png\r\n\r\n");
        var raw = new byte[header.Length + 4];
        Array.Copy(header, 0, raw, 0, header.Length);
        raw[header.Length] = 0x89; raw[header.Length + 1] = 0x50; raw[header.Length + 2] = 0x4E; raw[header.Length + 3] = 0x47; // PNG 魔数
        var req = WayCoder.UI.Web.HttpServer.ParseHttpRequest(raw);
        Check("WebUp: 二进制正文 RawBody 长度 4", req != null && req.RawBody.Length == 4);
        Check("WebUp: RawBody 首字节保留 0x89", req != null && req.RawBody[0] == 0x89);
        Check("WebUp: 头解析 X-File-Name", req?.Header("X-File-Name") == "a.png");
        Check("WebUp: Path 为 /upload", req?.Path == "/upload");

        // ── 6. ParsePath 纯函数 ──
        Check("WebUp: ParsePath /upload", WayCoder.UI.Web.HttpServer.ParsePath("POST /upload?kind=image HTTP/1.1\r\nHost: x\r\n\r\n") == "/upload");
        Check("WebUp: ParsePath /chat", WayCoder.UI.Web.HttpServer.ParsePath("POST /chat HTTP/1.1\r\n\r\n") == "/chat");
        Check("WebUp: ParsePath 空", WayCoder.UI.Web.HttpServer.ParsePath("") == "");
    }

}