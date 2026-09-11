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
    /// <summary>SnipToolOutputs 完整测试</summary>
    private static void TestSnipToolOutputs(Action<string, bool> Check)
    {
        // ── 1. 短内容不裁剪（≤4000 字符）──
        var shortMsgs = new List<JNode>
        {
            JNode.Object().Set("role", "tool").Set("content", "短输出\n只有几行\n内容很少"),
        };
        var shortBefore = shortMsgs[0]["content"]!.AsString();
        ContextManager.SnipToolOutputs(shortMsgs);
        Check("Snip: 短内容不裁剪", shortMsgs[0]["content"]!.AsString() == shortBefore);

        // ── 2. 非 tool 消息不裁剪 ──
        var userMsgs = new List<JNode>
        {
            JNode.Object().Set("role", "user").Set("content", new string('x', 5000)),
        };
        var userBefore = userMsgs[0]["content"]!.AsString();
        ContextManager.SnipToolOutputs(userMsgs);
        Check("Snip: 非tool消息不裁剪", userMsgs[0]["content"]!.AsString() == userBefore);

        // ── 3. 长内容裁剪（>4000 字符 + >10 行）──
        var lines = new List<string>();
        for (int i = 0; i < 200; i++)
            lines.Add($"第 {i:D4} 行：{new string('y', 30)}");
        var longContent = string.Join("\n", lines);
        Check("Snip: 输入内容 >4000 字符", longContent.Length > 4000);

        var longMsgs = new List<JNode>
        {
            JNode.Object().Set("role", "tool").Set("content", longContent),
        };
        var longBefore = ContextManager.EstimateTokens(longMsgs);
        ContextManager.SnipToolOutputs(longMsgs);
        var longAfter = ContextManager.EstimateTokens(longMsgs);
        Check("Snip: 长内容被裁剪", longAfter < longBefore);
        var snipped = longMsgs[0]["content"]!.AsString()!;
        Check("Snip: 裁剪后包含省略标记", snipped.Contains("省略") || snipped.Contains("裁剪"));

        // ── 4. 错误行保留 ──
        var errorLines = new List<string>();
        for (int i = 0; i < 10; i++)
            errorLines.Add($"普通行 {i}");
        errorLines.Add("Program.cs(45,12): error CS0103: 当前上下文中不存在名称 'doesNotExist'");
        errorLines.Add("Program.cs(67,3): error CS0246: 未能找到类型或命名空间名 'UnknownType'");
        for (int i = 0; i < 150; i++)
            errorLines.Add($"后续行 {i}：{new string('z', 30)}");

        var errorContent = string.Join("\n", errorLines);
        Check("Snip(错误): 输入内容 >4000 字符", errorContent.Length > 4000);

        var errMsgs = new List<JNode>
        {
            JNode.Object().Set("role", "tool").Set("content", errorContent),
        };
        ContextManager.SnipToolOutputs(errMsgs);
        var errSnipped = errMsgs[0]["content"]!.AsString()!;
        Check("Snip: 错误行 CS0103 被保留", errSnipped.Contains("CS0103"));
        Check("Snip: 错误行 CS0246 被保留", errSnipped.Contains("CS0246"));
        Check("Snip: 裁剪后包含错误统计", errSnipped.Contains("错误"));

        // ── 5. 首5尾5保留 ──
        var seqMsgs = new List<JNode>
        {
            JNode.Object().Set("role", "tool").Set("content", string.Join("\n", Enumerable.Range(0, 100).Select(i => $"LINE_{i:D3}: {new string('x', 50)}"))),
        };
        ContextManager.SnipToolOutputs(seqMsgs);
        var seqSnipped = seqMsgs[0]["content"]!.AsString()!;
        Check("Snip: 首部 LINE_000 被保留", seqSnipped.Contains("LINE_000"));
        Check("Snip: 首部 LINE_004 被保留", seqSnipped.Contains("LINE_004"));
        Check("Snip: 尾部 LINE_099 被保留", seqSnipped.Contains("LINE_099"));
        Check("Snip: 尾部 LINE_095 被保留", seqSnipped.Contains("LINE_095"));
        Check("Snip: 首行前无虚假省略标记", !seqSnipped.Contains("省略 1 行"));

        // ── 6. 多消息混合（部分裁剪）──
        var mixedMsgs = new List<JNode>
        {
            JNode.Object().Set("role", "user").Set("content", "请编译项目"),
            JNode.Object().Set("role", "tool").Set("content", new string('a', 200)), // 短输出不裁剪
            JNode.Object().Set("role", "tool").Set("content", string.Join("\n", Enumerable.Range(0, 100).Select(i => $"L{i:D3}: {new string('y', 50)}"))), // 长输出裁剪
        };
        var mixedChanged = ContextManager.SnipToolOutputs(mixedMsgs);
        Check("Snip: 混合消息有裁剪发生", mixedChanged);
        Check("Snip: 用户消息不变", mixedMsgs[0]["content"]!.AsString() == "请编译项目");
        Check("Snip: 短tool不裁剪", mixedMsgs[1]["content"]!.AsString()!.Length < 300);
        Check("Snip: 长tool被裁剪", mixedMsgs[2]["content"]!.AsString()!.Contains("省略") || mixedMsgs[2]["content"]!.AsString()!.Contains("裁剪"));

        // ── 7. TruncateByRunes：按码点截断，不切半代理对 ──
        Check("TruncateByRunes: 代理对不切半", ContextManager.TruncateByRunes("a😀b", 2) == "a😀");
        Check("TruncateByRunes: 未超限原样返回", ContextManager.TruncateByRunes("你好世界", 4) == "你好世界");
        Check("TruncateByRunes: 截断到码点边界", ContextManager.TruncateByRunes("abcdef", 3) == "abc");
        Check("TruncateByRunes: 截断点落在代理对中间仍完整", ContextManager.TruncateByRunes("xx😀yy", 3) == "xx😀");

        // ── 8. TruncateTailByRunes：按码点截断尾部，不切半代理对 ──
        Check("TruncateTailByRunes: 尾部代理对不切半", ContextManager.TruncateTailByRunes("a😀b", 2) == "😀b");
        Check("TruncateTailByRunes: 截断到码点边界", ContextManager.TruncateTailByRunes("你好世界", 2) == "世界");
        Check("TruncateTailByRunes: 单代理对整体保留", ContextManager.TruncateTailByRunes("😀", 1) == "😀");
        Check("TruncateTailByRunes: maxRunes=0 返回空", ContextManager.TruncateTailByRunes("abc", 0) == "");
        Check("TruncateTailByRunes: 未超限原样返回", ContextManager.TruncateTailByRunes("abc", 5) == "abc");
    }

    /// <summary>压缩保真度测试：超多需求压缩后关键信息仍保留（无 LLM 回退路径）</summary>
    private static void TestCompressionFidelity(Action<string, bool> Check)
    {
        // 构造"超多需求"消息：30 条需求 + 关联文件路径/命名空间/API 签名/错误码
        var msgs = new List<JNode>
        {
            JNode.Object().Set("role", "user").Set("content", "为 WayCoder 实现 30 个新工具，每个工具一个文件，全部完成后编译。")
        };
        for (int i = 1; i <= 30; i++)
        {
            msgs.Add(JNode.Object()
                .Set("role", "user")
                .Set("content", $"需求 {i}：实现 Tools/{i:D2}Tool.cs 工具，namespace WayCoder.Tools，" +
                              $"提供 public async Task<string> Execute(Dictionary<string, object?> args) 方法，处理业务逻辑。"));
        }
        // 冗余长工具输出（触发第 1 层裁剪）
        msgs.Add(JNode.Object()
            .Set("role", "tool")
            .Set("content", string.Join("\n", Enumerable.Range(0, 150).Select(i => $"冗余输出行 {i:D4}：{new string('x', 60)}"))));
        // 编译错误信息
        msgs.Add(JNode.Object()
            .Set("role", "tool")
            .Set("content", "编译失败：Program.cs(45,12): error CS0103: 当前上下文中不存在名称 'doesNotExist'"));

        var before = msgs.Count;
        var cm = new ContextManager(2000); // 极小 maxTokens 压低三层阈值
        // 不固定档位：三层压缩在任何省钱档位下都应触发（极致档曾因百分比误用 2000 下限而完全压不动）
        var compressed = cm.MaybeCompressAsync(msgs, null).GetAwaiter().GetResult();

        Check("压缩保真: 压缩确实发生", compressed);
        Check("压缩保真: 消息数减少", msgs.Count < before);

        var flat = string.Join("\n", msgs.Select(m => m["content"]?.AsString() ?? ""));

        // 保真度：文件路径 / 命名空间 / 错误码保留
        Check("压缩保真: 保留文件路径", flat.Contains("Tool.cs"));
        Check("压缩保真: 保留命名空间", flat.Contains("WayCoder.Tools"));
        Check("压缩保真: 保留错误码 CS0103", flat.Contains("CS0103"));
        // 保真度：需求条目保留（A2 增强）
        Check("压缩保真: 保留待完成需求段", flat.Contains("待完成需求"));
        Check("压缩保真: 保留具体需求条目", flat.Contains("需求 1"));
    }

    /// <summary>上下文窗口按模型切换测试</summary>
    private static void TestContextWindowSwitch(Action<string, bool> Check)
    {
        // ── 1. ResolveContextWindow 按模型解析窗口 ──
        Check("窗口: deepseek-v4-pro = 1M", ModelCatalog.ResolveContextWindow("deepseek-v4-pro") == 1_048_576);
        Check("窗口: deepseek-chat = 64K", ModelCatalog.ResolveContextWindow("deepseek-chat") == 64_000);
        Check("窗口: ollama 本地模型 = 128K", ModelCatalog.ResolveContextWindow("deepseek-coder-v2:latest") == 128_000);
        Check("窗口: 未知模型回退 128K", ModelCatalog.ResolveContextWindow("no-such-model") == 128_000);
        Check("窗口: null 回退 128K", ModelCatalog.ResolveContextWindow(null) == 128_000);
        Check("窗口: 空字符串回退 128K", ModelCatalog.ResolveContextWindow("") == 128_000);
        Check("窗口: 自定义回退值生效", ModelCatalog.ResolveContextWindow("unknown", 64_000) == 64_000);

        // ── 3. ImportOpenCodeApi：OpenAI 兼容 /models 端点解析（opencode 在线导入）──
        var ocApiJson = """{"object":"list","data":[{"id":"deepseek-v4-pro","object":"model","created":1,"owned_by":"opencode"},{"id":"qwen3.7-max","object":"model","created":1,"owned_by":"opencode"},{"id":"glm-5.2","object":"model","created":1,"owned_by":"opencode"}]}""";
        var ocApiModels = ModelCatalog.ImportOpenCodeApi(ocApiJson, "https://opencode.ai/zen/go/v1");
        Check("OpenCode在线: 解析 3 个模型", ocApiModels.Count == 3);
        Check("OpenCode在线: id 正确", ocApiModels[0].Id == "deepseek-v4-pro");
        Check("OpenCode在线: provider=opencode-go(Go 网关)", ocApiModels[0].ProviderId == "opencode-go");
        Check("OpenCode在线: baseUrl 保留 opencode 网关", ocApiModels[0].DefaultBaseUrl == "https://opencode.ai/zen/go/v1");
        Check("OpenCode在线: minimax 推断", ModelCatalog.InferProviderFromId("minimax-m3").ProviderId == "minimax");
        Check("OpenCode在线: kimi 推断", ModelCatalog.InferProviderFromId("kimi-k3").ProviderId == "moonshot");
        Check("OpenCode在线: glm 推断", ModelCatalog.InferProviderFromId("glm-5.2").ProviderId == "zhipu");
        Check("OpenCode在线: qwen 推断", ModelCatalog.InferProviderFromId("qwen3.7-max").ProviderId == "qwen");
        Check("OpenCode在线: openrouter 推断", ModelCatalog.InferProviderFromId("openrouter/auto").ProviderId == "openrouter");
        Check("OpenCode在线: 空 data 返回空", ModelCatalog.ImportOpenCodeApi("""{"data":[]}""", "http://x").Count == 0);
        Check("OpenCode在线: 畸形返回空", ModelCatalog.ImportOpenCodeApi("not json", "http://x").Count == 0);

        // ── 3.5 按服务地址(base_url)归类服务商（导入的模型按「请求打到哪」分类，而非模型名/配置 pid）──
        Check("按地址: go 网关→opencode-go", ModelCatalog.InferProviderFromBaseUrl("https://opencode.ai/zen/go/v1") == "opencode-go");
        Check("按地址: zen 网关→opencode-zen", ModelCatalog.InferProviderFromBaseUrl("https://opencode.ai/zen/v1") == "opencode-zen");
        Check("按地址: openrouter→openrouter", ModelCatalog.InferProviderFromBaseUrl("https://openrouter.ai/api/v1") == "openrouter");
        Check("按地址: deepseek.com→deepseek", ModelCatalog.InferProviderFromBaseUrl("https://api.deepseek.com") == "deepseek");
        Check("按地址: openai.com→openai", ModelCatalog.InferProviderFromBaseUrl("https://api.openai.com/v1") == "openai");
        Check("按地址: anthropic→anthropic", ModelCatalog.InferProviderFromBaseUrl("https://api.anthropic.com") == "anthropic");
        Check("按地址: localhost→local", ModelCatalog.InferProviderFromBaseUrl("http://localhost:11434") == "local");
        Check("按地址: 空/不可识别→null", ModelCatalog.InferProviderFromBaseUrl(null) == null && ModelCatalog.InferProviderFromBaseUrl("") == null);

        // ── 3.6 providerId 生成去前端 ai./ai-/www./www- 前缀（同网关子域变体归并成同一服务商 id）──
        // 注意：NormalizeId 会把 . / 转成 '-'（ai.deepseek.com → ai-deepseek-com），故前缀剥离认「-」/「.」两种，
        // 断言按 NormalizeId 后的短横线形式（含原有 -com 后缀剥离）。
        Check("providerId: 去 ai. 前缀", ModelCatalog.NormalizeProviderId("ai.deepseek.com") == "deepseek");
        Check("providerId: 去 ai- 前缀", ModelCatalog.NormalizeProviderId("ai-siliconflow-ai") == "siliconflow");
        Check("providerId: 去 www. 前缀", ModelCatalog.NormalizeProviderId("www.openai.com") == "openai");
        Check("providerId: 去 www- 前缀", ModelCatalog.NormalizeProviderId("www-openai.com") == "openai");
        Check("providerId: 多层 api-ai- 链", ModelCatalog.NormalizeProviderId("api-ai.openai.com") == "openai");
        Check("providerId: 去 api-inference. 前缀", ModelCatalog.NormalizeProviderId("api-inference.deepseek.com") == "deepseek");
        Check("providerId: 去 inference. 前缀", ModelCatalog.NormalizeProviderId("inference.siliconflow.com") == "siliconflow");
        Check("providerId: ResolveProviderId 自定义网关去 ai./www-",
            ModelCatalog.ResolveProviderId("https://ai.gw.example/v1", "x") == "gw-example"
            && ModelCatalog.ResolveProviderId("https://www-my.example.com/v1", "x") == "my-example");
        Check("providerId: ResolveProviderId 去 api-inference./inference.",
            ModelCatalog.ResolveProviderId("https://api-inference.gw.example/v1", "x") == "gw-example"
            && ModelCatalog.ResolveProviderId("https://inference-gw.example.com/v1", "x") == "gw-example");

        // 本地 opencode.json：provider 配了 opencode 网关地址 → 归 opencode（而非配置里的 deepseek pid）
        var ocLocal = ModelCatalog.ImportOpenCode(
            """{"provider":{"deepseek":{"name":"DeepSeek","options":{"baseURL":"https://opencode.ai/zen/go/v1"},"models":{"deepseek-v4-flash":{"name":"deepseek-v4-flash"}}}}}""");
        Check("本地opencode: Go 网关地址归 opencode-go", ocLocal.Count == 1 && ocLocal[0].ProviderId == "opencode-go");
        // 本地 codex：provider 配 deepseek 官方地址 → 归 deepseek
        var cxLocal = ModelCatalog.ImportCodex(
            "[model_providers.deepseek]\nname=\"DeepSeek\"\nbase_url=\"https://api.deepseek.com\"\n\n[profiles.deepseek]\nmodel_provider=\"deepseek\"\nmodel=\"deepseek-chat\"");
        Check("本地codex: deepseek 官方地址归 deepseek", cxLocal.Count > 0 && cxLocal.All(m => m.ProviderId == "deepseek"));
        // Claude Code 配 opencode 网关地址 → 归 opencode
        var clLocal = ModelCatalog.ImportClaude(
            """{"env":{"ANTHROPIC_BASE_URL":"https://opencode.ai/zen/go/v1","ANTHROPIC_MODEL":"claude-sonnet-5"}}""");
        Check("本地claude: Go 网关地址归 opencode-go", clLocal.Count > 0 && clLocal.All(m => m.ProviderId == "opencode-go"));

        // ── 4. 模型按供应商分类存储：ProviderGroupName 分组 ──
        Check("Provider分组: opencode→opencode", ModelCatalog.ProviderGroupName("opencode") == "opencode");
        Check("Provider分组: deepseek→deepseek", ModelCatalog.ProviderGroupName("DeepSeek") == "deepseek");
        Check("Provider分组: openai→openai", ModelCatalog.ProviderGroupName("openai") == "openai");
        Check("Provider分组: local→locals", ModelCatalog.ProviderGroupName("local") == "locals");
        Check("Provider分组: custom→locals", ModelCatalog.ProviderGroupName("custom") == "locals");
        Check("Provider分组: ollama→locals", ModelCatalog.ProviderGroupName("ollama") == "locals");
        Check("Provider分组: 特殊字符剥离(空格/斜杠→连字符)", ModelCatalog.ProviderGroupName("my provider/2") == "my-provider-2");
        Check("Provider分组: null→locals", ModelCatalog.ProviderGroupName(null) == "locals");

        // ── 2. UpdateMaxTokens 重算阈值：小窗口压缩、放大后不再压缩 ──
        var longTool = new List<JNode>
        {
            JNode.Object().Set("role", "tool").Set("content", string.Join("\n", Enumerable.Range(0, 100).Select(i => $"行{i}: {new string('x', 60)}")))
        };

        var smallCm = new ContextManager(200);
        var smallCopy = new List<JNode>
        {
            JNode.Object().Set("role", "tool").Set("content", longTool[0]["content"]!.AsString())
        };
        // 不固定档位：小窗口在任何省钱档位下都该触发压缩
        var compressedSmall = smallCm.MaybeCompressAsync(smallCopy, null).GetAwaiter().GetResult();
        Check("窗口: 小窗口触发压缩", compressedSmall);

        smallCm.UpdateMaxTokens(100_000);
        var largeCopy = new List<JNode>
        {
            JNode.Object().Set("role", "tool").Set("content", longTool[0]["content"]!.AsString())
        };
        var compressedLarge = smallCm.MaybeCompressAsync(largeCopy, null).GetAwaiter().GetResult();
        Check("窗口: 放大后不再压缩", !compressedLarge);

        // ── 3. UpdateMaxTokens 边界：非正值忽略 ──
        var cm = new ContextManager(1000);
        cm.UpdateMaxTokens(0);
        Check("窗口: UpdateMaxTokens(0) 忽略", cm.MaxTokens == 1000);
        cm.UpdateMaxTokens(-5);
        Check("窗口: UpdateMaxTokens(-5) 忽略", cm.MaxTokens == 1000);
        cm.UpdateMaxTokens(2048);
        Check("窗口: UpdateMaxTokens(2048) 生效", cm.MaxTokens == 2048);
    }

    /// <summary>
    /// 上下文预算判断（ShouldStopAndSummarize）测试：验证用「最近一次真实 prompt」而非「累计用量」判断。
    /// v0.53.2 修复：此前用累计用量（单调递增）判断，上下文远未满时误触发压缩，且压缩层用消息估算
    /// （远低于阈值）实际不压缩，累计值不重置形成循环刷屏。
    /// </summary>
    private static void TestContextStopWhen(Action<string, bool> Check)
    {
        // ── 1. LastPromptTokens 记录最近一次（覆盖而非累加）──
        var cm = new ContextManager(1_048_576);
        cm.AddUsage(50_000, 5_000, 40_000);
        cm.AddUsage(80_000, 8_000, 60_000);
        Check("StopWhen: LastPromptTokens 记录最近一次（非累加）", cm.LastPromptTokens == 80_000);
        Check("StopWhen: 累计 prompt 仍累加（花费追踪）", cm.CumulativePromptTokens == 130_000);

        // ── 2. 大窗口（>200K）：累计超窗口但最近 prompt 小 → 不触发 ──
        var cm2 = new ContextManager(1_048_576);
        cm2.AddUsage(1_000_000, 50_000, 0);   // 累计 100 万
        cm2.AddUsage(100_000, 5_000, 0);      // 累计 110 万（超窗口），但最近 prompt 仅 10 万
        Check("StopWhen: 累计超窗口但最近 prompt 小 → 不触发", !cm2.ShouldStopAndSummarize());

        // ── 3. 大窗口：最近 prompt 接近窗口 → 触发 ──
        var cm3 = new ContextManager(1_048_576);
        cm3.AddUsage(1_030_000, 0, 0);        // 剩余 18576 <= 20K buffer
        Check("StopWhen: 最近 prompt 接近窗口 → 触发", cm3.ShouldStopAndSummarize());

        // ── 4. 小窗口（≤200K）：比例阈值 20% ──
        var cm4 = new ContextManager(100_000);
        cm4.AddUsage(85_000, 0, 0);           // 剩余 15K <= 20K（20% 比例）
        Check("StopWhen: 小窗口最近 prompt 到 85% → 触发", cm4.ShouldStopAndSummarize());
        var cm4b = new ContextManager(100_000);
        cm4b.AddUsage(30_000, 0, 0);          // 剩余 70K > 20K → 不触发
        Check("StopWhen: 小窗口最近 prompt 30% → 不触发", !cm4b.ShouldStopAndSummarize());

        // ── 5. ResetUsage 重置 LastPromptTokens ──
        var cm5 = new ContextManager(1_048_576);
        cm5.AddUsage(900_000, 0, 0);
        Check("StopWhen: Reset 前 LastPromptTokens 已记录", cm5.LastPromptTokens == 900_000);
        cm5.ResetUsage();
        Check("StopWhen: ResetUsage 重置 LastPromptTokens", cm5.LastPromptTokens == 0);
        Check("StopWhen: Reset 后不触发压缩", !cm5.ShouldStopAndSummarize());
    }

    /// <summary>
    /// 上下文压缩可观测（预告 + 回看）：CompactionWarning / CompactionOccurred 事件 + CompactionHistory 有界历史。
    /// 竞品（Codex 静默砍窗口 / Claude 上下文腐烂）对压缩黑盒，WayCoder 补「压缩前预告将丢 N 条 + 压缩后回看 N→M 条」。
    /// </summary>
    private static void TestContextCompactionAudit(Action<string, bool> Check)
    {
        // 第 1 层（裁剪工具输出）无需 LLM：纯字符串截断即可触发，且单条消息不进入第 2/3 层
        int warned = 0;
        ContextManager.CompactionEntry? occurred = null;
        Action<string> onWarn = _ => warned++;
        Action<ContextManager.CompactionEntry> onOccurred = e => occurred = e;
        ContextManager.CompactionWarning += onWarn;
        ContextManager.CompactionOccurred += onOccurred;
        try
        {
            var bigTool = JNode.Object().Set("role", "tool")
                .Set("content", string.Join("\n", Enumerable.Range(0, 200).Select(i => $"行{i}: {new string('x', 80)}")));
            var cm = new ContextManager(200);
            var copy = new List<JNode> { JNode.Object().Set("role", "tool").Set("content", bigTool["content"]!.AsString()) };
            var compressed = cm.MaybeCompressAsync(copy, null).GetAwaiter().GetResult();
            Check("压缩审计: 裁剪层触发压缩", compressed);
            Check("压缩审计: CompactionOccurred 事件已触发", occurred != null && occurred.Layer == 1);
            Check("压缩审计: 历史有记录", ContextManager.CompactionHistory.Count >= 1);
            var last = ContextManager.CompactionHistory[^1];
            Check("压缩审计: 记录层号为裁剪层", last.Layer == 1);
            Check("压缩审计: 压缩后 tokens 下降", last.AfterTokens < last.BeforeTokens);

            // 第 2 层（摘要）：触发预告事件「将丢 N 条」。>20 条消息 + 超阈值 + null LLM 走 ExtractKeyInfo 回退
            warned = 0;
            var manyMsgs = new List<JNode>();
            for (int i = 0; i < 25; i++)
                manyMsgs.Add(JNode.Object().Set("role", "user").Set("content", $"这是第 {i} 条消息" + new string('x', 40)));
            var cm2 = new ContextManager(600);
            var compressed2 = cm2.MaybeCompressAsync(manyMsgs, null).GetAwaiter().GetResult();
            Check("压缩审计: 摘要层触发压缩", compressed2);
            Check("压缩审计: 压缩预告事件已触发", warned >= 1);
            Check("压缩审计: 历史含摘要/折叠层记录", ContextManager.CompactionHistory.Any(e => e.Layer >= 2));
        }
        finally
        {
            ContextManager.CompactionWarning -= onWarn;
            ContextManager.CompactionOccurred -= onOccurred;
        }
    }

    /// <summary>省 token 模式（EconomyMode 三态 + 优先级）测试</summary>
    private static void TestEconomyMode(Action<string, bool> Check)
    {
        Check("Economy: 默认关闭", new Config().EconomyMode == EconomyMode.Off);
        Check("Economy: 默认优先级=质量优先", new Config().EconomyPriority == EconomyPriority.Quality);
        Check("Economy: 输出上限常量 = 8192", Config.Instance.EconomyMaxTokens == 8192);
        Check("Economy: snip 阈值常量 = 2000", Config.Instance.EconomySnipChars == 2000);
        Check("Economy: 正常 snip 阈值常量 = 4000", Config.Instance.SnipCharsNormal == 4000);
        Check("Economy: 复杂任务轮数基准 = 30", Config.Instance.EconomyComplexRounds == 30);

        var savedEconomy = Config.Instance.EconomyMode;
        var savedPriority = Config.Instance.EconomyPriority;

        // ResolveRatio 三态：Off 用正常值，On 取更小值，Auto 按复杂度插值
        Config.Instance.EconomyMode = EconomyMode.Off;
        Check("Economy: Off 用正常值", ContextManager.ResolveRatio(50, 35, 0.5) == 50);
        Config.Instance.EconomyMode = EconomyMode.On;
        Check("Economy: On 取更小值", ContextManager.ResolveRatio(50, 35, 0.5) == 35);
        Check("Economy: On 尊重更低配置", ContextManager.ResolveRatio(30, 35, 0.5) == 30);

        // Auto + 质量优先（默认）：简单任务省、复杂任务保质量
        Config.Instance.EconomyMode = EconomyMode.Auto;
        Config.Instance.EconomyPriority = EconomyPriority.Quality;
        Check("Economy: 质量优先-简单任务省(复杂度0→省 token 值)", ContextManager.ResolveRatio(50, 35, 0.0) == 35);
        Check("Economy: 质量优先-复杂任务不省(复杂度1→正常值)", ContextManager.ResolveRatio(50, 35, 1.0) == 50);
        var midR = ContextManager.ResolveRatio(50, 35, 0.5);
        Check("Economy: 质量优先-中复杂度介于两者之间", midR > 35 && midR < 50);
        Check("Economy: 质量优先-简单收紧系数=1", ContextManager.AutoAggressiveness(0.0) == 1);
        Check("Economy: 质量优先-复杂收紧系数=0", ContextManager.AutoAggressiveness(1.0) == 0);

        // Auto + 费用优先：复杂任务仍省
        Config.Instance.EconomyPriority = EconomyPriority.Cost;
        Check("Economy: 费用优先-复杂任务仍省", ContextManager.ResolveRatio(50, 35, 1.0) == 35);
        Check("Economy: 费用优先-收紧系数恒=1", ContextManager.AutoAggressiveness(0.9) == 1);

        // Extreme：在 On 基础上再收紧 20%；下限按调用方量纲传入，不得污染百分比
        Config.Instance.EconomyMode = EconomyMode.Extreme;
        Check("Economy: Extreme 百分比再收紧 20%", ContextManager.ResolveRatio(50, 35, 0.5) == 28);
        Check("Economy: Extreme 百分比无 2000 下限(压缩线不失效)",
            ContextManager.ResolveRatio(50, 35, 0.5) < 100);
        Check("Economy: Extreme 字符数收紧至下限",
            ContextManager.ResolveRatio(4000, 2000, 0.5, extremeFloor: 2000) == 2000);

        // Auto + 均衡：复杂任务保留一半省钱
        Config.Instance.EconomyMode = EconomyMode.Auto;
        Config.Instance.EconomyPriority = EconomyPriority.Balanced;
        Check("Economy: 均衡-简单任务省", ContextManager.ResolveRatio(50, 35, 0.0) == 35);
        Check("Economy: 均衡-复杂任务保留一半省钱", ContextManager.ResolveRatio(50, 35, 1.0) == 42);

        // 系统提示词精简（仅 On 生效）。两侧都显式指定档位——不能拿「当前环境」当完整版基线
        var economyPrompt = PromptWithMode(EconomyMode.On);
        var fullPrompt = PromptWithMode(EconomyMode.Off);
        Check("Economy: 提示词比完整版更短", economyPrompt.Length < fullPrompt.Length);
        Check("Economy: 含工具列表", economyPrompt.Contains("bash"));
        Check("Economy: 含先读后改规则", economyPrompt.Contains("先读后改"));
        Check("Economy: 含工作目录", economyPrompt.Contains("工作目录"));
        Check("Economy: 不含 10 阶段流水线", !economyPrompt.Contains("systematic_phases"));

        // Auto 模式用完整提示词（不清简，仅动态调节压缩阈值）
        Config.Instance.EconomyMode = EconomyMode.Auto;
        var autoPrompt = SystemPrompt.Generate(ToolRegistry.AllTools);
        Check("Economy: Auto 用完整提示词(含流水线)", autoPrompt.Contains("systematic_phases"));

        // SnipToolOutputs 阈值：约 3300 字符（介于 2000 与 4000 之间），关闭不截断、打开截断
        var midContent = string.Join("\n", Enumerable.Range(0, 60).Select(i => new string('x', 50) + $"_{i:D3}"));
        Config.Instance.EconomyMode = EconomyMode.Off;
        var msgsOff = new List<JNode> { JNode.Object().Set("role", "tool").Set("content", midContent) };
        ContextManager.SnipToolOutputs(msgsOff);
        Check("Economy: 关闭时 3300 字符不截断", msgsOff[0]["content"]!.AsString() == midContent);

        Config.Instance.EconomyMode = EconomyMode.On;
        var msgsOn = new List<JNode> { JNode.Object().Set("role", "tool").Set("content", midContent) };
        ContextManager.SnipToolOutputs(msgsOn);
        Check("Economy: 打开时 3300 字符被截断", msgsOn[0]["content"]!.AsString()!.Length < midContent.Length);

        Config.Instance.EconomyMode = savedEconomy;
        Config.Instance.EconomyPriority = savedPriority;
    }

    /// <summary>省钱模式工具精简（Off=全量 / 开=去重复 / 开的越大越精简）</summary>
    private static void TestEconomyToolTrim(Action<string, bool> Check)
    {
        var all = ToolRegistry.AllTools;
        Check($"工具精简: 全量工具 = {all.Count}", all.Count == 49);

        var off = Agent.TrimToolsForEconomy(all, EconomyMode.Off);
        var auto = Agent.TrimToolsForEconomy(all, EconomyMode.Auto);
        var on = Agent.TrimToolsForEconomy(all, EconomyMode.On);
        var extreme = Agent.TrimToolsForEconomy(all, EconomyMode.Extreme);

        // 档位工具数单调递减：全量 > Auto > On > Extreme
        Check($"工具精简: Off({off.Count}) > Auto({auto.Count}) > On({on.Count}) > Extreme({extreme.Count})",
            off.Count > auto.Count && auto.Count > on.Count && on.Count > extreme.Count);

        // 精确数量：Off=全量 / Auto=去 bash 可替代重复 / On=再搜编辑冗余 / Extreme=核心集
        Check("工具精简: Auto 去掉 bash 可替代重复",
            auto.Count == all.Count - Agent.BashRedundantTools.Count);
        Check("工具精简: On 再搜编辑冗余",
            on.Count == all.Count - Agent.BashRedundantTools.Count - Agent.EditRedundantTools.Count);
        Check("工具精简: Extreme 只留核心集",
            extreme.Count == Agent.ExtremeCoreTools.Count);

        // Extreme 保留写代码核心 + 联网查资料，去掉 bash 基础
        var names = extreme.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Check("工具精简: Extreme 核心集与实际工具名一致", names.SetEquals(Agent.ExtremeCoreTools));
        Check("工具精简: Extreme 去掉 bash 基础 cd/ls",
            !extreme.Any(t => t.Name is "cd" or "ls"));

        // Off 返回原集（不换实例）
        Check("工具精简: Off 返回原集不动", ReferenceEquals(off, all));
    }

    /// <summary>Token 估算测试</summary>
    private static void TestTokenEstimation(Action<string, bool> Check)
    {
        // ── CJK 字符权重更高 ──
        var cjkMsg = new List<JNode>
        {
            JNode.Object().Set("role", "user").Set("content", "你好世界这是一个测试"),
        };
        var asciiMsg = new List<JNode>
        {
            JNode.Object().Set("role", "user").Set("content", "hello world this is a test"),
        };
        var cjkTokens = ContextManager.EstimateTokens(cjkMsg);
        var asciiTokens = ContextManager.EstimateTokens(asciiMsg);
        // CJK 10 字符 × 1.5 = 15, ASCII 27 字符 × 0.25 ≈ 7
        Check("TokenEst: CJK tokens > ASCII tokens (等长)", cjkTokens > asciiTokens);

        // ── 空消息列表 → 0 tokens ──
        var empty = ContextManager.EstimateTokens(new List<JNode>());
        Check("TokenEst: 空列表=0", empty == 0);

        // ── 混合内容估算 ──
        var mixed = new List<JNode>
        {
            JNode.Object().Set("role", "user").Set("content", "帮我编译WayCoder项目"),
            JNode.Object().Set("role", "assistant").Set("content", "好的，我来编译项目"),
            JNode.Object().Set("role", "tool").Set("content", "Build succeeded. 0 errors."),
        };
        var mixedTokens = ContextManager.EstimateTokens(mixed);
        Check("TokenEst: 混合消息 > 0", mixedTokens > 0);
        Check("TokenEst: 混合消息 > 单条消息", mixedTokens > ContextManager.EstimateTokens(cjkMsg));

        // ── tool_calls 也被计入 ──
        var withToolCalls = new List<JNode>
        {
            JNode.Object().Set("role", "assistant").Set("content", "我来执行命令")
                .Set("tool_calls", JNode.Array()
                    .Add(JNode.Object()
                        .Set("function", JNode.Object().Set("name", "bash").Set("arguments", "dotnet build")))),
        };
        var withToolTokens = ContextManager.EstimateTokens(withToolCalls);
        var withoutToolTokens = ContextManager.EstimateTokens(new List<JNode>
        {
            JNode.Object().Set("role", "assistant").Set("content", "我来执行命令"),
        });
        Check("TokenEst: tool_calls 增加估计值", withToolTokens > withoutToolTokens);

        // ── 真实 API 用量校准（固定开销：system prompt + 工具定义 + 元数据）──
        var calCm = new ContextManager(128_000);
        var calMsgs = new List<JNode>
        {
            JNode.Object().Set("role", "user").Set("content", "hello world"),
        };
        var calEst = ContextManager.EstimateTokens(calMsgs);
        // 未采集真实用量前，校准值退化为原始估算
        Check("TokenCalib: 无真实数据时校准=估算", calCm.EstimateCalibratedTokens(calMsgs) == calEst);

        // 真实 prompt tokens 含固定开销，校准值应加上开销
        const int overhead1 = 1000;
        calCm.AddUsage(calEst + overhead1, 200, calEst);
        Check("TokenCalib: 校准后含固定开销", calCm.EstimateCalibratedTokens(calMsgs) == calEst + overhead1);

        // 固定开销平滑收敛：第二次 AddUsage 取移动平均
        const int overhead2 = 1200;
        calCm.AddUsage(calEst + overhead2, 200, calEst);
        var expectedAvg = (overhead1 + overhead2) / 2;
        Check("TokenCalib: 开销平滑收敛", calCm.EstimateCalibratedTokens(calMsgs) == calEst + expectedAvg);

        // 有真实开销时校准值必然大于原始估算
        Check("TokenCalib: 校准值 > 原始估算", calCm.EstimateCalibratedTokens(calMsgs) > calEst);
    }

    /// <summary>Prompt 缓存追踪（PromptCache）单元测试：哈希命中/未命中/命中率/节省 token/禁用。</summary>
    private static void TestPromptCache(Action<string, bool> Check)
    {
        PromptCache.Enabled = true;
        PromptCache.ClearStats();

        // 1. 首次未命中
        Check("PromptCache: 首次未命中", !PromptCache.RecordRequest("sys1", "tools1", 100, 50));
        Check("PromptCache: 首次后 1 请求 0 命中", PromptCache.TotalRequests == 1 && PromptCache.CacheHits == 0);

        // 2. 相同请求命中 + 节省 token 累计
        Check("PromptCache: 相同请求命中", PromptCache.RecordRequest("sys1", "tools1", 100, 50));
        Check("PromptCache: 命中后 2 请求 1 命中", PromptCache.TotalRequests == 2 && PromptCache.CacheHits == 1);
        Check("PromptCache: 节省 token=150", PromptCache.SavedTokens == 150);

        // 3. 不同 system 未命中
        Check("PromptCache: 不同 system 未命中", !PromptCache.RecordRequest("sys2", "tools1", 100, 50));

        // 4. 不同 tools 未命中
        Check("PromptCache: 不同 tools 未命中", !PromptCache.RecordRequest("sys2", "tools2", 100, 50));

        // 5. HitRate = 1/4 = 25%
        Check("PromptCache: HitRate 计算", Math.Abs(PromptCache.HitRate - 25.0) < 0.01);

        // 6. Reset 后相同请求又未命中
        PromptCache.Reset();
        Check("PromptCache: Reset 后未命中", !PromptCache.RecordRequest("sys2", "tools2", 100, 50));

        // 7. Enabled=false 短路 + 摘要「关闭」
        PromptCache.ClearStats();
        PromptCache.Enabled = false;
        Check("PromptCache: 禁用后未命中", !PromptCache.RecordRequest("sys1", "tools1", 100, 50));
        Check("PromptCache: 禁用摘要含关闭", PromptCache.Summary().Contains("关闭"));
        PromptCache.Enabled = true;

        // 8. Summary 命中率 + K 格式（1500 tokens → 1.5K）
        PromptCache.ClearStats();
        PromptCache.RecordRequest("sys1", "tools1", 1000, 500);
        PromptCache.RecordRequest("sys1", "tools1", 1000, 500);
        var summary = PromptCache.Summary();
        Check("PromptCache: Summary 含命中率", summary.Contains("命中率"));
        Check("PromptCache: Summary 含 K 格式", summary.Contains("1.5K"));

        PromptCache.ClearStats();
    }

}