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
    /// <summary>模型/厂商调用参数约束（reasoning_effort 允许集 + temperature 精度）测试</summary>
    private static void TestModelParams(Action<string, bool> Check)
    {
        // ── 1. 纯函数：两级合并（模型级 > 厂商级 > 全局默认）──
        var (a1, p1) = ModelCatalog.MergeModelProviderConstraints("low,high,max", 3, "low,high", 2);
        Check("模型参数: 模型级>厂商级", a1 == "low,high,max" && p1 == 3);
        var (a2, p2) = ModelCatalog.MergeModelProviderConstraints(null, null, "low,high", 2);
        Check("模型参数: 模型null→厂商级", a2 == "low,high" && p2 == 2);
        var (a3, p3) = ModelCatalog.MergeModelProviderConstraints(null, null, null, null);
        Check("模型参数: 都null→默认2", a3 == null && p3 == 2);
        var (a4, p4) = ModelCatalog.MergeModelProviderConstraints("low", 0, null, 3);
        Check("模型参数: 模型0不被厂商覆盖", a4 == "low" && p4 == 0);

        // ── 2. 纯函数：reasoning_effort 越界跳过（glm-5.3 场景）──
        Check("模型参数: 无约束→原样返回", ModelCatalog.ResolveReasoningEffort(null, "medium") == "medium");
        Check("模型参数: 越界→跳过", ModelCatalog.ResolveReasoningEffort("low,high,max", "medium") == null);
        Check("模型参数: 命中→返回", ModelCatalog.ResolveReasoningEffort("low,high,max", "high") == "high");
        Check("模型参数: 全局空→null", ModelCatalog.ResolveReasoningEffort("low,high,max", "") == null);
        Check("模型参数: 大小写不敏感", ModelCatalog.ResolveReasoningEffort("Low,High,Max", "medium") == null);

        // ── 3. 往返：约束字段序列化保留 ──
        var prevLocalExists = File.Exists(ModelCatalog.LocalModelsPath);
        ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
            "__selftest_params__", "__selftest_params__", "SelfTest", "selftestp", "T", "Imported",
            128_000, 1.5, 3.0, "https://selftest.example/v1", "params 描述",
            ReasoningEffortAllowed: "low,high,max", TemperaturePrecision: 3), local: true);
        var rt = ModelCatalog.Find("__selftest_params__");
        Check("模型参数: 往返允许集保留", rt?.ReasoningEffortAllowed == "low,high,max");
        Check("模型参数: 往返精度保留", rt?.TemperaturePrecision == 3);
        ModelCatalog.RemoveCustom("__selftest_params__");
        if (!prevLocalExists && File.Exists(ModelCatalog.LocalModelsPath))
        {
            var leftover = File.ReadAllText(ModelCatalog.LocalModelsPath).Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "");
            if (leftover == "[]") File.Delete(ModelCatalog.LocalModelsPath);
        }

        // ── 4. 集成：LLM 请求体按模型应用（HttpServer 捕获）──
        var savedReasoning = Config.Instance.ReasoningEffort;
        var server = new WayCoder.UI.Web.HttpServer(0);
        string? lastBody = null;
        server.OnRequest = async req =>
        {
            lastBody = req.Body;
            var sse = "data: {\"choices\":[{\"delta\":{\"content\":\"ok\"}}]}\n\n" +
                      "data: {\"choices\":[{\"delta\":{\"content\":\"!\"}}]}\n\n" +
                      "data: [DONE]\n\n";
            return WayCoder.UI.Web.HttpResponse.JsonBody(sse);
        };
        server.Start();
        try
        {
            var url = $"http://127.0.0.1:{server.ActualPort}";
            var glmMi = new ModelCatalog.ModelInfo(
                "__selftest_glm__", "__selftest_glm__", "SelfTest", "selftestp", "T", "Imported",
                0, 0, 0, url, "test", ReasoningEffortAllowed: "low,high,max", TemperaturePrecision: 3);
            ModelCatalog.AddCustom(glmMi, local: true);
            try
            {
                // glm 场景：全局 medium 越界 → 请求体无 reasoning_effort；temp 按模型 3 位
                Config.Instance.ReasoningEffort = "medium";
                new LLM("__selftest_glm__", "k", url, temperature: 0.123456f)
                    .ChatAsync(new List<JNode> { JNode.Object().Set("role", "user").Set("content", "hi") })
                    .GetAwaiter().GetResult();
                Check("模型参数: 越界→请求体无 reasoning_effort",
                    lastBody != null && !lastBody.Contains("reasoning_effort"));
                Check("模型参数: temperature 按模型精度3位",
                    lastBody != null && lastBody.Contains("\"temperature\":0.123"));

                // 命中 → 发送 reasoning_effort
                Config.Instance.ReasoningEffort = "high";
                new LLM("__selftest_glm__", "k", url)
                    .ChatAsync(new List<JNode> { JNode.Object().Set("role", "user").Set("content", "hi") })
                    .GetAwaiter().GetResult();
                Check("模型参数: 命中→请求体含 reasoning_effort high",
                    lastBody != null && lastBody.Contains("\"reasoning_effort\":\"high\""));

                // 无约束但支持思考 → 原样发 medium（v0.87.5 门控：reasoning_effort 仅支持思考的模型发）
                ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
                    "__selftest_plain__", "__selftest_plain__", "SelfTest", "selftestp", "T", "Imported",
                    0, 0, 0, url, "test", SupportsThinking: true), local: true);
                Config.Instance.ReasoningEffort = "medium";
                new LLM("__selftest_plain__", "k", url)
                    .ChatAsync(new List<JNode> { JNode.Object().Set("role", "user").Set("content", "hi") })
                    .GetAwaiter().GetResult();
                Check("模型参数: 无约束但支持思考→原样发 medium",
                    lastBody != null && lastBody.Contains("\"reasoning_effort\":\"medium\""));

                // 不支持思考的模型 → 一律不发（本地/chat-only 模型避免 HTTP 400）
                ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
                    "__selftest_nothink__", "__selftest_nothink__", "SelfTest", "selftestp", "T", "Imported",
                    0, 0, 0, url, "test"), local: true);
                Config.Instance.ReasoningEffort = "medium";
                new LLM("__selftest_nothink__", "k", url)
                    .ChatAsync(new List<JNode> { JNode.Object().Set("role", "user").Set("content", "hi") })
                    .GetAwaiter().GetResult();
                Check("模型参数: 不支持思考→不发 reasoning_effort",
                    lastBody != null && !lastBody.Contains("reasoning_effort"));
            }
            finally
            {
                ModelCatalog.RemoveCustom("__selftest_glm__");
                ModelCatalog.RemoveCustom("__selftest_plain__");
                ModelCatalog.RemoveCustom("__selftest_nothink__");
                Config.Instance.ReasoningEffort = savedReasoning;
            }
        }
        catch (Exception ex)
        {
            DebugLog.Log("selftest", "TestModelParams: " + ex.Message);
            Check("模型参数: 集成测试不崩溃", false);
        }
        finally
        {
            server.Stop();
        }
    }

    /// <summary>非 OpenAI 格式兼容（Anthropic /v1/messages + Gemini streamGenerateContent）测试</summary>
    private static void TestApiFormat(Action<string, bool> Check)
    {
        // ── 1. 纯函数：ResolveApiFormat / IsPlausibleApiKey ──
        Check("ApiFormat: 未知模型默认 openai", ModelCatalog.ResolveApiFormat("__nope__", "http://x") == "openai");
        Check("ApiFormat: 内置 anthropic=anthropic", ModelCatalog.ResolveApiFormat("claude-sonnet-5", "https://api.anthropic.com") == "anthropic");
        Check("ApiFormat: 内置 gemini=gemini", ModelCatalog.ResolveApiFormat("gemini-2.5-pro", "https://generativelanguage.googleapis.com") == "gemini");
        Check("ApiKey: 合法 key 通过", ConnectionConfig.IsPlausibleApiKey("sk-abcdef1234567890"));
        Check("ApiKey: URL 拒绝", !ConnectionConfig.IsPlausibleApiKey("https://example.com/v1"));
        Check("ApiKey: 含空白拒绝", !ConnectionConfig.IsPlausibleApiKey("sk-ab cd"));
        Check("ApiKey: 过短拒绝", !ConnectionConfig.IsPlausibleApiKey("short"));
        // 地址反查兜底：用户在 providers.json 配了新网关（带 apiFormat）但没重新导入模型时，
        // 目录里没有「该模型 + 该地址」的组合 —— 少了这条兜底就静默降级成 openai 格式。
        ModelCatalog.Providers["__antprov_url__"] = new ModelCatalog.ProviderInfo(
            "UrlProbe", "https://x.example.com", ApiFormat: "anthropic",
            SupportsThinking: false, SupportsTools: false);
        Check("ApiFormat: 地址反查 provider（目录无该模型+地址组合时不降级 openai）",
            ModelCatalog.ResolveApiFormat("claude-sonnet-5", "https://x.example.com") == "anthropic");
        // 网关级能力开关同样要能被反查到：否则会被「claude 家族 → 支持思考」的推断覆盖，
        // 给不支持 thinking 的网关照发 thinking 参数（实测 opencode-zen 会因此请求失败）。
        var consByUrl = ModelCatalog.ResolveModelCallConstraints("claude-sonnet-5", "https://x.example.com");
        Check("能力约束: 地址反查的 supportsThinking=false 生效（不被模型家族推断覆盖）",
            !consByUrl.SupportsThinking);
        Check("能力约束: 地址反查的 supportsTools=false 生效", !consByUrl.SupportsTools);
        ModelCatalog.Providers.Remove("__antprov_url__");
        Check("ApiFormat: 地址反查不影响普通 OpenAI 兼容网关",
            ModelCatalog.ResolveApiFormat("claude-sonnet-5", "https://openrouter.ai/api/v1") == "openai");

        // ── 2. 集成：Anthropic 原生（模拟 message_*/content_block_* SSE）──
        var server = new WayCoder.UI.Web.HttpServer(0);
        string? antBody = null, antKey = null, antVer = null, antPath = null;
        server.OnRequest = async req =>
        {
            antBody = req.Body; antKey = req.Headers.GetValueOrDefault("x-api-key");
            antVer = req.Headers.GetValueOrDefault("anthropic-version"); antPath = req.Path;
            var sse = string.Join("\n\n",
                "data: {\"type\":\"message_start\",\"message\":{\"usage\":{\"input_tokens\":5}}}",
                "data: {\"type\":\"content_block_delta\",\"index\":0,\"delta\":{\"type\":\"text_delta\",\"text\":\"hello\"}}",
                "data: {\"type\":\"content_block_delta\",\"index\":0,\"delta\":{\"type\":\"text_delta\",\"text\":\" world\"}}",
                "data: {\"type\":\"message_delta\",\"usage\":{\"output_tokens\":3}}",
                "data: {\"type\":\"message_stop\"}") + "\n\n";
            return WayCoder.UI.Web.HttpResponse.JsonBody(sse);
        };
        server.Start();
        try
        {
            var url = $"http://127.0.0.1:{server.ActualPort}";
            var savedProv = ModelCatalog.Providers.TryGetValue("__antprov__", out var sp) ? sp : null;
            ModelCatalog.Providers["__antprov__"] = new ModelCatalog.ProviderInfo("AntTest", url, ApiFormat: "anthropic");
            ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
                "__selftest_ant__", "__selftest_ant__", "AntTest", "__antprov__", "A", "Imported",
                0, 0, 0, url, "test"), local: true);
            try
            {
                var msgs = new List<JNode>
                {
                    JNode.Object().Set("role", "system").Set("content", "你是助手"),
                    JNode.Object().Set("role", "user").Set("content", "hi"),
                };
                var resp = new LLM("__selftest_ant__", "sk-ant-test", url).ChatAsync(msgs).GetAwaiter().GetResult();
                Check("Anthropic: 内容解析", resp.Content == "hello world");
                Check("Anthropic: x-api-key 头", antKey == "sk-ant-test");
                Check("Anthropic: anthropic-version 头", antVer == "2023-06-01");
                Check("Anthropic: 端点 /v1/messages", antPath != null && antPath.Contains("/v1/messages"));
                Check("Anthropic: system 提取到顶层", antBody != null && antBody.Contains("\"system\":\"你是助手\""));
                Check("Anthropic: max_tokens 必填", antBody != null && antBody.Contains("max_tokens"));
                Check("Anthropic: 无 stream_options", antBody != null && !antBody.Contains("stream_options"));
                Check("Anthropic: usage 解析", resp.PromptTokens == 5 && resp.CompletionTokens == 3);
            }
            finally
            {
                ModelCatalog.RemoveCustom("__selftest_ant__");
                if (savedProv != null) ModelCatalog.Providers["__antprov__"] = savedProv; else ModelCatalog.Providers.Remove("__antprov__");
            }
        }
        finally { server.Stop(); }

        // ── 2b. Anthropic 与 OpenAI 的**硬差异**（踩中必 400，不是风格问题）──
        // ① 角色必须严格交替：一次发多个工具 → 多个 tool_result **必须合进同一条 user 消息**。
        //    每条 tool 消息各生成一条 user 消息的话，会出现连续两条 user → Anthropic 直接 400。
        // ② 空 text 块被拒（text content blocks must be non-empty）：assistant 只带 tool_calls、
        //    content 为空串时，不能再补一个 text:"" 的块。
        // ③ 开启 extended thinking 时 temperature 必须为 1。
        var serverAnt2 = new WayCoder.UI.Web.HttpServer(0);
        string? antBody2 = null;
        serverAnt2.OnRequest = async req =>
        {
            antBody2 = req.Body;
            return WayCoder.UI.Web.HttpResponse.JsonBody("data: {\"type\":\"message_stop\"}\n\n");
        };
        serverAnt2.Start();
        try
        {
            var urlAnt2 = $"http://127.0.0.1:{serverAnt2.ActualPort}";
            var savedProv2 = ModelCatalog.Providers.TryGetValue("__antprov2__", out var spAnt2) ? spAnt2 : null;
            ModelCatalog.Providers["__antprov2__"] = new ModelCatalog.ProviderInfo("AntTest2", urlAnt2, ApiFormat: "anthropic");
            ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
                "__selftest_ant2__", "__selftest_ant2__", "AntTest2", "__antprov2__", "A", "Imported",
                0, 0, 0, urlAnt2, "test"), local: true);
            try
            {
                static JNode ToolCall(string id, string name, string args) =>
                    JNode.Object().Set("id", id).Set("type", "function")
                        .Set("function", JNode.Object().Set("name", name).Set("arguments", args));

                var antMsgs = new List<JNode>
                {
                    JNode.Object().Set("role", "user").Set("content", "hi"),
                    // assistant 只有工具调用、正文为空（真实主循环里最常见的一轮）
                    JNode.Object().Set("role", "assistant").Set("content", "")
                        .Set("tool_calls", JNode.Array()
                            .Add(ToolCall("call_1", "bash", "{\"command\":\"ls\"}"))
                            .Add(ToolCall("call_2", "read_file", "{\"path\":\"a.txt\"}"))),
                    JNode.Object().Set("role", "tool").Set("tool_call_id", "call_1").Set("content", "out1"),
                    JNode.Object().Set("role", "tool").Set("tool_call_id", "call_2").Set("content", "out2"),
                };
                _ = new LLM("__selftest_ant2__", "sk-ant-test", urlAnt2).ChatAsync(antMsgs).GetAwaiter().GetResult();

                var body = antBody2 == null ? null : Json.Parse(antBody2);
                var am = body?["messages"]?.Items.ToList();
                Check("Anthropic: 连发多个工具 → 角色严格交替（user/assistant/user 共 3 条，不出现连续 user）",
                    am is { Count: 3 });
                var mergedUser = am is { Count: 3 } ? am[2]["content"] : null;
                var asstBlocks = am is { Count: 3 } ? am[1]["content"] : null;
                Check("Anthropic: 多个 tool_result 合并进同一条 user 消息",
                    mergedUser?.Items.Count() == 2
                    && mergedUser.Items.All(b => b["type"]?.AsString() == "tool_result"));
                Check("Anthropic: assistant 空正文不产出空 text 块（只留 tool_use）",
                    asstBlocks != null && asstBlocks.Items.All(b => b["type"]?.AsString() == "tool_use"));
            }
            finally
            {
                ModelCatalog.RemoveCustom("__selftest_ant2__");
                if (savedProv2 != null) ModelCatalog.Providers["__antprov2__"] = savedProv2; else ModelCatalog.Providers.Remove("__antprov2__");
            }
        }
        finally { serverAnt2.Stop(); }

        // ── 2c. Anthropic extended thinking 的硬约束 ──
        // budget_tokens 必须 ≥1024 且 < max_tokens，且**开启 thinking 时 temperature 只能为 1**
        // （给别的值直接 400）。此前 budget 用 Math.Min(maxTok,1024)（max_tokens 小时给非法值）、
        // temperature 照发原值，两个都会踩。
        var serverAnt3 = new WayCoder.UI.Web.HttpServer(0);
        string? antBody3 = null;
        serverAnt3.OnRequest = async req =>
        {
            antBody3 = req.Body;
            return WayCoder.UI.Web.HttpResponse.JsonBody("data: {\"type\":\"message_stop\"}\n\n");
        };
        serverAnt3.Start();
        var savedEffort = Config.Instance.ReasoningEffort;
        try
        {
            var urlAnt3 = $"http://127.0.0.1:{serverAnt3.ActualPort}";
            var savedProv3 = ModelCatalog.Providers.TryGetValue("__antprov3__", out var spAnt3) ? spAnt3 : null;
            ModelCatalog.Providers["__antprov3__"] = new ModelCatalog.ProviderInfo(
                "AntTest3", urlAnt3, ApiFormat: "anthropic", SupportsThinking: true);
            ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
                "__selftest_ant3__", "__selftest_ant3__", "AntTest3", "__antprov3__", "A", "Imported",
                0, 0, 0, urlAnt3, "test"), local: true);
            try
            {
                Config.Instance.ReasoningEffort = "high";
                var antMsgs3 = new List<JNode> { JNode.Object().Set("role", "user").Set("content", "hi") };
                _ = new LLM("__selftest_ant3__", "sk-ant-test", urlAnt3).ChatAsync(antMsgs3).GetAwaiter().GetResult();

                var b3 = antBody3 == null ? null : Json.Parse(antBody3);
                var budget = (int?)(b3?["thinking"]?["budget_tokens"]?.AsNumber()) ?? 0;
                var maxTok3 = (int?)(b3?["max_tokens"]?.AsNumber()) ?? 0;
                Check("Anthropic: 开启 thinking 时 temperature 必须为 1",
                    (b3?["temperature"]?.AsNumber() ?? 0) == 1);
                Check("Anthropic: thinking budget_tokens ≥1024 且 < max_tokens",
                    budget >= 1024 && budget < maxTok3);
            }
            finally
            {
                Config.Instance.ReasoningEffort = savedEffort;
                ModelCatalog.RemoveCustom("__selftest_ant3__");
                if (savedProv3 != null) ModelCatalog.Providers["__antprov3__"] = savedProv3; else ModelCatalog.Providers.Remove("__antprov3__");
            }
        }
        finally { serverAnt3.Stop(); }

        // ── 3. 集成：Gemini 原生（模拟 candidates[0].content.parts SSE）──
        var server2 = new WayCoder.UI.Web.HttpServer(0);
        string? gBody = null, gKey = null, gPath = null;
        server2.OnRequest = async req =>
        {
            gBody = req.Body; gKey = req.Headers.GetValueOrDefault("x-goog-api-key"); gPath = req.Path;
            var sse = "data: {\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"你好\"}]},\"finishReason\":\"STOP\"}],\"usageMetadata\":{\"promptTokenCount\":3,\"candidatesTokenCount\":2}}\n\n";
            return WayCoder.UI.Web.HttpResponse.JsonBody(sse);
        };
        server2.Start();
        try
        {
            var url2 = $"http://127.0.0.1:{server2.ActualPort}";
            var savedProv2 = ModelCatalog.Providers.TryGetValue("__gemprov__", out var sp2) ? sp2 : null;
            ModelCatalog.Providers["__gemprov__"] = new ModelCatalog.ProviderInfo("GemTest", url2, ApiFormat: "gemini");
            ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
                "__selftest_gem__", "__selftest_gem__", "GemTest", "__gemprov__", "G", "Imported",
                0, 0, 0, url2, "test"), local: true);
            try
            {
                var resp = new LLM("__selftest_gem__", "AIza-test-key", url2)
                    .ChatAsync(new List<JNode> { JNode.Object().Set("role", "user").Set("content", "hi") })
                    .GetAwaiter().GetResult();
                Check("Gemini: 内容解析", resp.Content == "你好");
                Check("Gemini: x-goog-api-key 头", gKey == "AIza-test-key");
                Check("Gemini: URL 嵌模型名+流式端点", gPath != null && gPath.Contains("__selftest_gem__:streamGenerateContent"));
                Check("Gemini: generationConfig+maxOutputTokens", gBody != null && gBody.Contains("generationConfig") && gBody.Contains("maxOutputTokens"));
                Check("Gemini: 无 stream 字段", gBody != null && !gBody.Contains("\"stream\":true"));
                Check("Gemini: usage 解析", resp.PromptTokens == 3 && resp.CompletionTokens == 2);
            }
            finally
            {
                ModelCatalog.RemoveCustom("__selftest_gem__");
                if (savedProv2 != null) ModelCatalog.Providers["__gemprov__"] = savedProv2; else ModelCatalog.Providers.Remove("__gemprov__");
            }
        }
        finally { server2.Stop(); }
    }

    /// <summary>ExtractKeyInfo 增强版测试</summary>
    private static void TestExtractKeyInfo(Action<string, bool> Check)
    {
        // 反射调用 private 方法 ExtractKeyInfo
        var method = typeof(ContextManager).GetMethod("ExtractKeyInfo",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        // AOT 不支持反射，使用公开的 SnipToolOutputs 侧面验证 + 直接构造场景
        // 通过 SnipToolOutputs 的错误保留逻辑覆盖错误提取路径

        // ── 验证：错误行中的 CS 错误码被识别 ──
        var msgsWithErrors = new List<JNode>
        {
            JNode.Object().Set("role", "tool").Set("content", string.Join("\n",
                Enumerable.Range(0, 5).Select(i => $"行{i}")
                .Concat(new[] {
                    "File.cs(10,5): error CS0103: 名称 'foo' 不存在",
                    "File.cs(20,8): error CS0246: 类型 'Bar' 未找到",
                    "Unhandled exception: System.NullReferenceException",
                })
                .Concat(Enumerable.Range(0, 150).Select(i => $"填充行{i}：{new string('x', 40)}")))),
        };
        ContextManager.SnipToolOutputs(msgsWithErrors);
        var result = msgsWithErrors[0]["content"]!.AsString()!;
        Check("ExtractKey: 保留 error CS0103", result.Contains("CS0103"));
        Check("ExtractKey: 保留 error CS0246", result.Contains("CS0246"));
        Check("ExtractKey: 保留 Exception", result.Contains("NullReferenceException"));
        Check("ExtractKey: 错误行上下文在", result.Contains("行3") || result.Contains("行4"));

        // ── 验证：首尾行保留 ──
        Check("ExtractKey: 首行保留", result.Contains("行0"));
        Check("ExtractKey: 尾行保留", result.Contains("填充行149"));

        // ── 验证：namespace 提取（通过 GenerateProjectSnapshot 间接测试）──
        var snapshotMsgs = new List<JNode>
        {
            JNode.Object().Set("role", "assistant").Set("content", "namespace WayCoder.Tools;\nnamespace MiniDB.Storage;\n普通文本"),
        };
        // 测试 GenerateProjectSnapshot 不为空
        var snapshot = typeof(ContextManager).GetMethod("GenerateProjectSnapshot",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        // AOT 限制：无法反射调用 private 方法，但 GenerateProjectSnapshot 在 HardCollapseAsync 内部调用，
        // 通过公开 API 间接测试其输出有效性
    }

    private static void TestUpdateChecker(Action<string, bool> Check)
    {
        // ── 语义版本比较 ──
        Check("升级: v0.48.6 < v0.49.0", UpdateChecker.CompareVersions("v0.48.6", "v0.49.0") < 0);
        Check("升级: v0.49.0 > v0.48.6", UpdateChecker.CompareVersions("v0.49.0", "v0.48.6") > 0);
        Check("升级: 相同版本相等", UpdateChecker.CompareVersions("v0.48.6", "v0.48.6") == 0);
        Check("升级: 不同段数相等 (v1.0 vs v1.0.0)", UpdateChecker.CompareVersions("v1.0", "v1.0.0") == 0);
        Check("升级: 后缀忽略 (v2.0.0-beta > v1.9.9)", UpdateChecker.CompareVersions("v2.0.0-beta", "v1.9.9") > 0);
        Check("升级: 大写 V 前缀 (V1.2 < 1.3)", UpdateChecker.CompareVersions("V1.2", "1.3") < 0);
        Check("升级: 数值比较非字典序 (1.10.0 > 1.9.0)", UpdateChecker.CompareVersions("1.10.0", "1.9.0") > 0);

        // ── 当前平台 RID 探测 ──
        var rid = UpdateChecker.DetectCurrentRid();
        Check("升级: RID 非空含连字符", !string.IsNullOrEmpty(rid) && rid.Contains('-'));
        var knownRids = new[] { "win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64" };
        Check("升级: RID 属于已知平台", knownRids.Contains(rid));

        // ── 资产名匹配 ──
        var names = new[]
        {
            "waycoder-v0.49.0-win-x64.zip",
            "waycoder-v0.49.0-osx-arm64.tar.gz",
            "waycoder-v0.49.0-linux-x64.tar.gz",
        };
        Check("升级: 匹配 osx-arm64 tar.gz",
            UpdateChecker.FindAssetName(names, "osx-arm64") == "waycoder-v0.49.0-osx-arm64.tar.gz");
        Check("升级: 匹配 win-x64 zip",
            UpdateChecker.FindAssetName(names, "win-x64") == "waycoder-v0.49.0-win-x64.zip");
        Check("升级: 无匹配返回 null",
            UpdateChecker.FindAssetName(names, "linux-arm64") == null);

        // ── 资产 URL 匹配（JSON assets 数组）──
        var assets = JNode.Array()
            .Add(JNode.Object().Set("name", "waycoder-v0.49.0-win-x64.zip").Set("browser_download_url", "https://x/win.zip"))
            .Add(JNode.Object().Set("name", "waycoder-v0.49.0-osx-arm64.tar.gz").Set("browser_download_url", "https://x/osx.tar.gz"));
        Check("升级: 资产 URL 匹配",
            UpdateChecker.FindAssetUrl(assets, "osx-arm64") == "https://x/osx.tar.gz");
        Check("升级: 资产 URL 无匹配 null",
            UpdateChecker.FindAssetUrl(assets, "linux-x64") == null);

        // ── 供应链校验：下载 URL 受信白名单（防 release 注入恶意链接）──
        Check("升级: 受信 URL github.com", UpdateChecker.IsTrustedDownloadUrl("https://github.com/a/b/releases/download/v1/x.tar.gz"));
        Check("升级: 受信 URL gitee.com", UpdateChecker.IsTrustedDownloadUrl("https://gitee.com/a/b/releases/download/v1/x.zip"));
        Check("升级: 受信 URL objects.githubusercontent.com", UpdateChecker.IsTrustedDownloadUrl("https://objects.githubusercontent.com/x/y"));
        Check("升级: http 明文拒绝", !UpdateChecker.IsTrustedDownloadUrl("http://github.com/a/b/x.tar.gz"));
        Check("升级: 非受信 host 拒绝", !UpdateChecker.IsTrustedDownloadUrl("https://evil.example.com/payload.tar.gz"));
        Check("升级: null/空串拒绝", !UpdateChecker.IsTrustedDownloadUrl(null) && !UpdateChecker.IsTrustedDownloadUrl(""));

        // ── 供应链校验：checksums 资产定位 ──
        var assetsWithSum = JNode.Array()
            .Add(JNode.Object().Set("name", "waycoder-v0.49.0-osx-arm64.tar.gz").Set("browser_download_url", "https://github.com/a/b/download/x.tar.gz"))
            .Add(JNode.Object().Set("name", "SHA256SUMS.txt").Set("browser_download_url", "https://github.com/a/b/download/SHA256SUMS.txt"));
        Check("升级: 定位 SHA256SUMS.txt", UpdateChecker.FindChecksumUrl(assetsWithSum) == "https://github.com/a/b/download/SHA256SUMS.txt");
        Check("升级: 无校验文件返回 null", UpdateChecker.FindChecksumUrl(assets) == null);

        // ── 供应链校验：SHA256SUMS 解析 ──
        var sums =
            "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad  waycoder-a.tar.gz\n" +
            "# 注释行应被忽略\n" +
            "0000000000000000000000000000000000000000000000000000000000000000 *waycoder-b.zip\n" +
            "\n" +
            "BADLINE\n";
        var sumMap = UpdateChecker.ParseChecksums(sums);
        Check("升级: 解析出两条记录", sumMap.Count == 2);
        Check("升级: 普通格式键值正确", sumMap["waycoder-a.tar.gz"] == "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
        Check("升级: 二进制 * 前缀剥离", sumMap["waycoder-b.zip"] == "0000000000000000000000000000000000000000000000000000000000000000");
        Check("升级: 忽略注释/空行/坏行", !sumMap.ContainsKey("BADLINE") && !sumMap.ContainsKey(""));

        // ── 供应链校验：文件 SHA256 计算 ──
        var shaTmp = Path.Combine(Path.GetTempPath(), "waycoder-test-sha-" + rid + ".bin");
        File.WriteAllText(shaTmp, "abc");
        try
        {
            Check("升级: SHA256('abc') 正确",
                UpdateChecker.ComputeSha256Hex(shaTmp) == "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
        }
        finally { try { File.Delete(shaTmp); } catch { } }
    }

    /// <summary>
    /// 导入助手（ImportHelper）纯逻辑单元测试：JSONC 注释剥离（行/块/字符串内转义）+
    /// 文件大小格式化（B/KB/MB 三档 + 边界）。
    /// 这些方法原先 private，改为 internal 后成为可测的零依赖纯函数。
    /// </summary>
    private static void TestImportHelper(Action<string, bool> Check)
    {
        // ---- StripJsonComments：行注释 ----
        var noLine = Json.StripComments("{\"a\": 1} // 行注释");
        Check("Import: 行注释移除", !noLine.Contains("行注释"));
        Check("Import: 行注释后仍可解析", Json.Parse(noLine) is JNode { Kind: JKind.Object });

        // ---- StripJsonComments：块注释 ----
        var noBlock = Json.StripComments("{\"a\": /* 块注释 */ 1}");
        Check("Import: 块注释移除", !noBlock.Contains("块注释"));
        Check("Import: 块注释后字段值正确", (int?)Json.Parse(noBlock)?["a"]?.AsNumber() == 1);

        // ---- StripJsonComments：字符串内注释标记不误删 ----
        var url = Json.StripComments("{\"url\": \"http://example.com\"}");
        Check("Import: 字符串内 // 不误删", url.Contains("http://example.com"));

        var star = Json.StripComments("{\"s\": \"a/*b*/c\"}");
        Check("Import: 字符串内 /* */ 不误删", star.Contains("a/*b*/c"));

        // ---- StripJsonComments：字符串内转义引号不破坏解析 ----
        var esc = Json.StripComments("{\"s\": \"a\\\"b\"}");
        Check("Import: 转义引号保留", Json.Parse(esc)?["s"]?.AsString() == "a\"b");

        // ---- FormatSize：三档 + 边界 ----
        Check("Import: FormatSize 0 B", FormatUtil.FormatSize(0) == "0 B");
        Check("Import: FormatSize 512 B", FormatUtil.FormatSize(512) == "512 B");
        Check("Import: FormatSize 1023 B 边界", FormatUtil.FormatSize(1023) == "1023 B");
        Check("Import: FormatSize 1KB 边界", FormatUtil.FormatSize(1024) == "1.0 KB");
        Check("Import: FormatSize 2KB", FormatUtil.FormatSize(2048) == "2.0 KB");
        Check("Import: FormatSize 1MB 边界", FormatUtil.FormatSize(1024L * 1024) == "1.0 MB");
        Check("Import: FormatSize 5MB", FormatUtil.FormatSize(5L * 1024 * 1024) == "5.0 MB");
    }

}