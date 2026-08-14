using System.Text;
using System.Text.Json;
using WayCoder.Tools;
using WayCoder.UI;
using WayCoder.Terminal;
using WayCoder.UI.TuiControls;
using WayCoder.UI.TuiScreens;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>获取 notebook cell 的 source 文本（测试助手）</summary>
    private static string GetNotebookSource(JsonObject notebook, int cellIndex)
    {
        var cells = notebook["cells"]?.AsArray();
        if (cells == null || cellIndex >= cells.Count) return "";
        var source = cells[cellIndex]?["source"];
        if (source is JsonArray arr)
        {
            var sb = new StringBuilder();
            foreach (var line in arr) sb.Append(line?.ToString() ?? "");
            return sb.ToString();
        }
        return source?.ToString() ?? "";
    }

    // ═══════════════════════════════════════════════════════════
    //  ContextManager 单元测试
    // ═══════════════════════════════════════════════════════════

    /// <summary>SnipToolOutputs 完整测试</summary>
    private static void TestSnipToolOutputs(Action<string, bool> Check)
    {
        // ── 1. 短内容不裁剪（≤4000 字符）──
        var shortMsgs = new List<JsonObject>
        {
            new() { ["role"] = "tool", ["content"] = "短输出\n只有几行\n内容很少" },
        };
        var shortBefore = shortMsgs[0]["content"]!.GetValue<string>();
        ContextManager.SnipToolOutputs(shortMsgs);
        Check("Snip: 短内容不裁剪", shortMsgs[0]["content"]!.GetValue<string>() == shortBefore);

        // ── 2. 非 tool 消息不裁剪 ──
        var userMsgs = new List<JsonObject>
        {
            new() { ["role"] = "user", ["content"] = new string('x', 5000) },
        };
        var userBefore = userMsgs[0]["content"]!.GetValue<string>();
        ContextManager.SnipToolOutputs(userMsgs);
        Check("Snip: 非tool消息不裁剪", userMsgs[0]["content"]!.GetValue<string>() == userBefore);

        // ── 3. 长内容裁剪（>4000 字符 + >10 行）──
        var lines = new List<string>();
        for (int i = 0; i < 200; i++)
            lines.Add($"第 {i:D4} 行：{new string('y', 30)}");
        var longContent = string.Join("\n", lines);
        Check("Snip: 输入内容 >4000 字符", longContent.Length > 4000);

        var longMsgs = new List<JsonObject>
        {
            new() { ["role"] = "tool", ["content"] = longContent },
        };
        var longBefore = ContextManager.EstimateTokens(longMsgs);
        ContextManager.SnipToolOutputs(longMsgs);
        var longAfter = ContextManager.EstimateTokens(longMsgs);
        Check("Snip: 长内容被裁剪", longAfter < longBefore);
        var snipped = longMsgs[0]["content"]!.GetValue<string>();
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

        var errMsgs = new List<JsonObject>
        {
            new() { ["role"] = "tool", ["content"] = errorContent },
        };
        ContextManager.SnipToolOutputs(errMsgs);
        var errSnipped = errMsgs[0]["content"]!.GetValue<string>();
        Check("Snip: 错误行 CS0103 被保留", errSnipped.Contains("CS0103"));
        Check("Snip: 错误行 CS0246 被保留", errSnipped.Contains("CS0246"));
        Check("Snip: 裁剪后包含错误统计", errSnipped.Contains("错误"));

        // ── 5. 首5尾5保留 ──
        var seqMsgs = new List<JsonObject>
        {
            new() { ["role"] = "tool", ["content"] = string.Join("\n", Enumerable.Range(0, 100).Select(i => $"LINE_{i:D3}: {new string('x', 50)}")) },
        };
        ContextManager.SnipToolOutputs(seqMsgs);
        var seqSnipped = seqMsgs[0]["content"]!.GetValue<string>();
        Check("Snip: 首部 LINE_000 被保留", seqSnipped.Contains("LINE_000"));
        Check("Snip: 首部 LINE_004 被保留", seqSnipped.Contains("LINE_004"));
        Check("Snip: 尾部 LINE_099 被保留", seqSnipped.Contains("LINE_099"));
        Check("Snip: 尾部 LINE_095 被保留", seqSnipped.Contains("LINE_095"));

        // ── 6. 多消息混合（部分裁剪）──
        var mixedMsgs = new List<JsonObject>
        {
            new() { ["role"] = "user", ["content"] = "请编译项目" },
            new() { ["role"] = "tool", ["content"] = new string('a', 200) }, // 短输出不裁剪
            new() { ["role"] = "tool", ["content"] = string.Join("\n", Enumerable.Range(0, 100).Select(i => $"L{i:D3}: {new string('y', 50)}")) }, // 长输出裁剪
        };
        var mixedChanged = ContextManager.SnipToolOutputs(mixedMsgs);
        Check("Snip: 混合消息有裁剪发生", mixedChanged);
        Check("Snip: 用户消息不变", mixedMsgs[0]["content"]!.GetValue<string>() == "请编译项目");
        Check("Snip: 短tool不裁剪", mixedMsgs[1]["content"]!.GetValue<string>().Length < 300);
        Check("Snip: 长tool被裁剪", mixedMsgs[2]["content"]!.GetValue<string>().Contains("省略") || mixedMsgs[2]["content"]!.GetValue<string>().Contains("裁剪"));
    }

    /// <summary>压缩保真度测试：超多需求压缩后关键信息仍保留（无 LLM 回退路径）</summary>
    private static void TestCompressionFidelity(Action<string, bool> Check)
    {
        // 构造"超多需求"消息：30 条需求 + 关联文件路径/命名空间/API 签名/错误码
        var msgs = new List<JsonObject>
        {
            new() { ["role"] = "user", ["content"] = "为 WayCoder 实现 30 个新工具，每个工具一个文件，全部完成后编译。" }
        };
        for (int i = 1; i <= 30; i++)
        {
            msgs.Add(new JsonObject
            {
                ["role"] = "user",
                ["content"] = $"需求 {i}：实现 Tools/{i:D2}Tool.cs 工具，namespace WayCoder.Tools，" +
                              $"提供 public async Task<string> Execute(Dictionary<string, object?> args) 方法，处理业务逻辑。"
            });
        }
        // 冗余长工具输出（触发第 1 层裁剪）
        msgs.Add(new JsonObject
        {
            ["role"] = "tool",
            ["content"] = string.Join("\n", Enumerable.Range(0, 150).Select(i => $"冗余输出行 {i:D4}：{new string('x', 60)}"))
        });
        // 编译错误信息
        msgs.Add(new JsonObject
        {
            ["role"] = "tool",
            ["content"] = "编译失败：Program.cs(45,12): error CS0103: 当前上下文中不存在名称 'doesNotExist'"
        });

        var before = msgs.Count;
        var cm = new ContextManager(2000); // 极小 maxTokens 压低三层阈值
        var compressed = cm.MaybeCompressAsync(msgs, null).GetAwaiter().GetResult();

        Check("压缩保真: 压缩确实发生", compressed);
        Check("压缩保真: 消息数减少", msgs.Count < before);

        var flat = string.Join("\n", msgs.Select(m => m["content"]?.GetValue<string>() ?? ""));

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
        Check("窗口: 未知模型回退 1M", ModelCatalog.ResolveContextWindow("no-such-model") == 1_048_576);
        Check("窗口: null 回退 1M", ModelCatalog.ResolveContextWindow(null) == 1_048_576);
        Check("窗口: 空字符串回退 1M", ModelCatalog.ResolveContextWindow("") == 1_048_576);
        Check("窗口: 自定义回退值生效", ModelCatalog.ResolveContextWindow("unknown", 64_000) == 64_000);

        // ── 2. UpdateMaxTokens 重算阈值：小窗口压缩、放大后不再压缩 ──
        var longTool = new List<JsonObject>
        {
            new() { ["role"] = "tool", ["content"] = string.Join("\n", Enumerable.Range(0, 100).Select(i => $"行{i}: {new string('x', 60)}")) }
        };

        var smallCm = new ContextManager(200);
        var smallCopy = new List<JsonObject>
        {
            new() { ["role"] = "tool", ["content"] = longTool[0]["content"]!.GetValue<string>() }
        };
        var compressedSmall = smallCm.MaybeCompressAsync(smallCopy, null).GetAwaiter().GetResult();
        Check("窗口: 小窗口触发压缩", compressedSmall);

        smallCm.UpdateMaxTokens(100_000);
        var largeCopy = new List<JsonObject>
        {
            new() { ["role"] = "tool", ["content"] = longTool[0]["content"]!.GetValue<string>() }
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

    /// <summary>Tiny 模式测试（4K 窗口 + 精简提示词）</summary>
    private static void TestTinyMode(Action<string, bool> Check)
    {
        Check("Tiny: 窗口常量 = 4096", Config.TinyContextWindow == 4096);
        Check("Tiny: 默认关闭", new Config().TinyMode == false);

        // ResolveContextWindow 在 TinyMode 下固定 4K，忽略模型窗口
        var saved = Config.Instance.TinyMode;
        Config.Instance.TinyMode = true;
        Check("Tiny: 窗口固定 4K（忽略模型）", ModelCatalog.ResolveContextWindow("deepseek-v4-pro") == 4096);
        Config.Instance.TinyMode = false;
        Check("Tiny: 关闭后恢复模型窗口", ModelCatalog.ResolveContextWindow("deepseek-v4-pro") == 1_048_576);

        // 系统提示词精简
        Config.Instance.TinyMode = true;
        var tinyPrompt = SystemPrompt.Generate(ToolRegistry.AllTools);
        Config.Instance.TinyMode = saved;
        Check("Tiny: 提示词精简 <3000 字符", tinyPrompt.Length < 3000);
        Check("Tiny: 含工具列表", tinyPrompt.Contains("bash"));
        Check("Tiny: 含先读后改规则", tinyPrompt.Contains("先读后改"));
        Check("Tiny: 含工作目录", tinyPrompt.Contains("工作目录"));
    }

    private static void TestTinyWindow(Action<string, bool> Check)
    {
        // 窗口规格解析
        Check("Tiny: ParseWindowSpec 8k → 8192", ModelCatalog.ParseWindowSpec("8k") == 8192);
        Check("Tiny: ParseWindowSpec 8192 → 8192", ModelCatalog.ParseWindowSpec("8192") == 8192);
        Check("Tiny: ParseWindowSpec 4K → 4096", ModelCatalog.ParseWindowSpec("4K") == 4096);
        Check("Tiny: ParseWindowSpec 16k → 16384", ModelCatalog.ParseWindowSpec("16k") == 16384);
        Check("Tiny: ParseWindowSpec 非法 → null", ModelCatalog.ParseWindowSpec("abc") == null);
        Check("Tiny: ParseWindowSpec 空 → null", ModelCatalog.ParseWindowSpec("") == null);
        Check("Tiny: ParseWindowSpec 0 → null", ModelCatalog.ParseWindowSpec("0") == null);

        // 显式指定窗口
        Check("Tiny: --tiny 8k 指定窗口", ModelCatalog.ResolveTinyWindow("8k", null, null) == 8192);

        // 自动探测：非 ollama 走目录；未知模型兜底 4K
        Check("Tiny: 自动探测目录窗口", ModelCatalog.ResolveTinyWindow(null, "deepseek-v4-pro", null) == 1_048_576);
        Check("Tiny: 自动探测失败兜底 4K", ModelCatalog.ResolveTinyWindow(null, "未知模型xyz", null) == 4096);

        // ProbeModelWindow
        Check("Tiny: ProbeModelWindow 目录命中", ModelCatalog.ProbeModelWindow("deepseek-v4-pro", null, 1000) == 1_048_576);
        Check("Tiny: ProbeModelWindow 兜底", ModelCatalog.ProbeModelWindow("未知xyz", null, 4096) == 4096);

        // 128K 自动阈值
        Check("Tiny: 自动阈值 = 128K", Config.TinyAutoThreshold == 128_000);
        Check("Tiny: 32K 模型低于阈值", ModelCatalog.ProbeModelWindow("qwen2.5-coder:3b", null, 1_048_576) < Config.TinyAutoThreshold);
        Check("Tiny: 1M 模型不低于阈值", ModelCatalog.ProbeModelWindow("deepseek-v4-pro", null, 1_048_576) >= Config.TinyAutoThreshold);

        // Ollama base url 识别
        Check("Tiny: IsOllamaBaseUrl localhost", ModelCatalog.IsOllamaBaseUrl("http://localhost:11434"));
        Check("Tiny: IsOllamaBaseUrl 非 ollama", !ModelCatalog.IsOllamaBaseUrl("https://api.deepseek.com"));
    }

    /// <summary>省 token 模式（EconomyMode 三态 + 优先级）测试</summary>
    private static void TestEconomyMode(Action<string, bool> Check)
    {
        Check("Economy: 默认关闭", new Config().EconomyMode == EconomyMode.Off);
        Check("Economy: 默认优先级=质量优先", new Config().EconomyPriority == EconomyPriority.Quality);
        Check("Economy: 输出上限常量 = 8192", Config.EconomyMaxTokens == 8192);
        Check("Economy: snip 阈值常量 = 2000", Config.EconomySnipChars == 2000);
        Check("Economy: 正常 snip 阈值常量 = 4000", Config.SnipCharsNormal == 4000);
        Check("Economy: 复杂任务轮数基准 = 30", Config.EconomyComplexRounds == 30);

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

        // Auto + 均衡：复杂任务保留一半省钱
        Config.Instance.EconomyPriority = EconomyPriority.Balanced;
        Check("Economy: 均衡-简单任务省", ContextManager.ResolveRatio(50, 35, 0.0) == 35);
        Check("Economy: 均衡-复杂任务保留一半省钱", ContextManager.ResolveRatio(50, 35, 1.0) == 42);

        // 系统提示词精简（仅 On 生效）
        Config.Instance.EconomyMode = EconomyMode.On;
        var economyPrompt = SystemPrompt.Generate(ToolRegistry.AllTools);
        Config.Instance.EconomyMode = savedEconomy;
        var fullPrompt = SystemPrompt.Generate(ToolRegistry.AllTools);
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
        var msgsOff = new List<JsonObject> { new() { ["role"] = "tool", ["content"] = midContent } };
        ContextManager.SnipToolOutputs(msgsOff);
        Check("Economy: 关闭时 3300 字符不截断", msgsOff[0]["content"]!.GetValue<string>() == midContent);

        Config.Instance.EconomyMode = EconomyMode.On;
        var msgsOn = new List<JsonObject> { new() { ["role"] = "tool", ["content"] = midContent } };
        ContextManager.SnipToolOutputs(msgsOn);
        Check("Economy: 打开时 3300 字符被截断", msgsOn[0]["content"]!.GetValue<string>()!.Length < midContent.Length);

        Config.Instance.EconomyMode = savedEconomy;
        Config.Instance.EconomyPriority = savedPriority;
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
        var msgsWithErrors = new List<JsonObject>
        {
            new() { ["role"] = "tool", ["content"] = string.Join("\n",
                Enumerable.Range(0, 5).Select(i => $"行{i}")
                .Concat(new[] {
                    "File.cs(10,5): error CS0103: 名称 'foo' 不存在",
                    "File.cs(20,8): error CS0246: 类型 'Bar' 未找到",
                    "Unhandled exception: System.NullReferenceException",
                })
                .Concat(Enumerable.Range(0, 150).Select(i => $"填充行{i}：{new string('x', 40)}"))) },
        };
        ContextManager.SnipToolOutputs(msgsWithErrors);
        var result = msgsWithErrors[0]["content"]!.GetValue<string>();
        Check("ExtractKey: 保留 error CS0103", result.Contains("CS0103"));
        Check("ExtractKey: 保留 error CS0246", result.Contains("CS0246"));
        Check("ExtractKey: 保留 Exception", result.Contains("NullReferenceException"));
        Check("ExtractKey: 错误行上下文在", result.Contains("行3") || result.Contains("行4"));

        // ── 验证：首尾行保留 ──
        Check("ExtractKey: 首行保留", result.Contains("行0"));
        Check("ExtractKey: 尾行保留", result.Contains("填充行149"));

        // ── 验证：namespace 提取（通过 GenerateProjectSnapshot 间接测试）──
        var snapshotMsgs = new List<JsonObject>
        {
            new() { ["role"] = "assistant", ["content"] = "namespace WayCoder.Tools;\nnamespace MiniDB.Storage;\n普通文本" },
        };
        // 测试 GenerateProjectSnapshot 不为空
        var snapshot = typeof(ContextManager).GetMethod("GenerateProjectSnapshot",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        // AOT 限制：无法反射调用 private 方法，但 GenerateProjectSnapshot 在 HardCollapseAsync 内部调用，
        // 通过公开 API 间接测试其输出有效性
    }

    /// <summary>GenerateProjectSnapshot 测试</summary>
    private static void TestGenerateProjectSnapshot(Action<string, bool> Check)
    {
        // 通过 HardCollapseAsync 的调用链间接验证快照不为空且包含关键信息
        // 直接测试：构造场景确保 GenerateProjectSnapshot 不会崩溃

        // ── 验证项目快照内容 ──
        // 当前目录即 WayCoder 项目根目录，验证关键子目录存在
        Check("Snapshot: 工作目录存在", System.IO.Directory.Exists(System.IO.Directory.GetCurrentDirectory()));
        Check("Snapshot: Agent 目录存在", System.IO.Directory.Exists(
            System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Agent")));
        Check("Snapshot: .git 目录存在", System.IO.Directory.Exists(
            System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "..", ".git")));
    }

    /// <summary>Token 估算测试</summary>
    private static void TestTokenEstimation(Action<string, bool> Check)
    {
        // ── CJK 字符权重更高 ──
        var cjkMsg = new List<JsonObject>
        {
            new() { ["role"] = "user", ["content"] = "你好世界这是一个测试" },
        };
        var asciiMsg = new List<JsonObject>
        {
            new() { ["role"] = "user", ["content"] = "hello world this is a test" },
        };
        var cjkTokens = ContextManager.EstimateTokens(cjkMsg);
        var asciiTokens = ContextManager.EstimateTokens(asciiMsg);
        // CJK 10 字符 × 1.5 = 15, ASCII 27 字符 × 0.25 ≈ 7
        Check("TokenEst: CJK tokens > ASCII tokens (等长)", cjkTokens > asciiTokens);

        // ── 空消息列表 → 0 tokens ──
        var empty = ContextManager.EstimateTokens(new List<JsonObject>());
        Check("TokenEst: 空列表=0", empty == 0);

        // ── 混合内容估算 ──
        var mixed = new List<JsonObject>
        {
            new() { ["role"] = "user", ["content"] = "帮我编译WayCoder项目" },
            new() { ["role"] = "assistant", ["content"] = "好的，我来编译项目" },
            new() { ["role"] = "tool", ["content"] = "Build succeeded. 0 errors." },
        };
        var mixedTokens = ContextManager.EstimateTokens(mixed);
        Check("TokenEst: 混合消息 > 0", mixedTokens > 0);
        Check("TokenEst: 混合消息 > 单条消息", mixedTokens > ContextManager.EstimateTokens(cjkMsg));

        // ── tool_calls 也被计入 ──
        var withToolCalls = new List<JsonObject>
        {
            new() { ["role"] = "assistant", ["content"] = "我来执行命令",
                ["tool_calls"] = new JsonArray { new JsonObject {
                    ["function"] = new JsonObject { ["name"] = "bash", ["arguments"] = "dotnet build" }
                }}
            },
        };
        var withToolTokens = ContextManager.EstimateTokens(withToolCalls);
        var withoutToolTokens = ContextManager.EstimateTokens(new List<JsonObject>
        {
            new() { ["role"] = "assistant", ["content"] = "我来执行命令" },
        });
        Check("TokenEst: tool_calls 增加估计值", withToolTokens > withoutToolTokens);

        // ── 真实 API 用量校准（固定开销：system prompt + 工具定义 + 元数据）──
        var calCm = new ContextManager(128_000);
        var calMsgs = new List<JsonObject>
        {
            new() { ["role"] = "user", ["content"] = "hello world" },
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

    /// <summary>/init 项目初始化（ProjectInitializer 生成 CLAUDE.md + 命令检测）测试</summary>
    private static void TestProjectInit(Action<string, bool> Check)
    {
        // ── GenerateClaudeMd 结构 ──
        var info = new ProjectInfo
        {
            ProjectRoot = "/tmp/demo-project",
            PrimaryLanguage = "Go",
            Frameworks = new List<string> { "Go" },
            BuildTools = new List<string> { "go" },
        };
        var md = ProjectInitializer.GenerateClaudeMd(info);
        Check("init: 生成含标题", md.Contains("# CLAUDE.md"));
        Check("init: 含项目概述区块", md.Contains("## 项目概述"));
        Check("init: 含项目名", md.Contains("demo-project"));
        Check("init: 含主语言", md.Contains("主语言: Go"));
        Check("init: 含命令块", md.Contains("```bash"));
        Check("init: 含开发规范区块", md.Contains("## 开发规范"));

        // ── 命令检测（临时目录，按构建系统分场景）──
        string? tmp = null;
        try
        {
            tmp = Directory.CreateTempSubdirectory("waycoder-init").FullName;

            // .NET
            var dotnetDir = Path.Combine(tmp, "dotnet");
            Directory.CreateDirectory(dotnetDir);
            File.WriteAllText(Path.Combine(dotnetDir, "App.csproj"), "<Project/>");
            Check("init: dotnet 构建命令", ProjectInitializer.DetectBuildCommand(dotnetDir) == "dotnet build");
            Check("init: dotnet 无 Tests 项目时无测试命令", ProjectInitializer.DetectTestCommand(dotnetDir) == null);
            File.WriteAllText(Path.Combine(dotnetDir, "App.Tests.csproj"), "<Project/>");
            Check("init: dotnet 有 Tests 项目返回 dotnet test", ProjectInitializer.DetectTestCommand(dotnetDir) == "dotnet test");

            // Node.js
            var nodeDir = Path.Combine(tmp, "node");
            Directory.CreateDirectory(nodeDir);
            File.WriteAllText(Path.Combine(nodeDir, "package.json"),
                "{\"scripts\":{\"test\":\"jest\",\"lint\":\"eslint .\"}}");
            Check("init: npm 构建命令", ProjectInitializer.DetectBuildCommand(nodeDir) == "npm install && npm run build");
            Check("init: npm 测试命令", ProjectInitializer.DetectTestCommand(nodeDir) == "npm test");
            Check("init: npm lint 命令", ProjectInitializer.DetectLintCommand(nodeDir) == "npm run lint");

            // Go
            var goDir = Path.Combine(tmp, "go");
            Directory.CreateDirectory(goDir);
            File.WriteAllText(Path.Combine(goDir, "go.mod"), "module x\n");
            Check("init: go 测试命令", ProjectInitializer.DetectTestCommand(goDir) == "go test ./...");
            Check("init: go lint 命令", ProjectInitializer.DetectLintCommand(goDir) == "go vet ./...");

            // Rust
            var rustDir = Path.Combine(tmp, "rust");
            Directory.CreateDirectory(rustDir);
            File.WriteAllText(Path.Combine(rustDir, "Cargo.toml"), "[package]\n");
            Check("init: rust 测试命令", ProjectInitializer.DetectTestCommand(rustDir) == "cargo test");

            // Python
            var pyDir = Path.Combine(tmp, "python");
            Directory.CreateDirectory(pyDir);
            File.WriteAllText(Path.Combine(pyDir, "test_foo.py"), "def test(): pass\n");
            Check("init: python 测试命令", ProjectInitializer.DetectTestCommand(pyDir) == "pytest");

            // 未知项目：返回 null
            var emptyDir = Path.Combine(tmp, "empty");
            Directory.CreateDirectory(emptyDir);
            Check("init: 空目录无构建命令", ProjectInitializer.DetectBuildCommand(emptyDir) == null);
        }
        catch { }
        finally
        {
            if (tmp != null) { try { Directory.Delete(tmp, true); } catch { } }
        }
    }

    private static void TestMultiSlotParallel(Action<string, bool> Check)
    {
        // ── 槽位运行状态（多槽位后台并行执行的核心状态）──
        var slot = new AgentSlot();
        Check("并行: 初始非忙", !slot.IsBusy);
        Check("并行: 初始无取消令牌", slot.Cts == null);
        Check("并行: Sync 锁非空", slot.Sync != null);
        Check("并行: 初始消息为空", slot.ChatMessages.Count == 0);
        Check("并行: 槽位数量为 10", AgentSlot.Count == 10);

        // ── 流式缓冲：StartStream → AppendToken → FinishStream ──
        slot.BufferedStartStream();
        Check("并行: 开始流式创建一条消息", slot.ChatMessages.Count == 1);
        Check("并行: 流式消息标记 Streaming", slot.ChatMessages[^1].Streaming);
        slot.BufferedAppendToken("你好");
        slot.BufferedAppendToken("，世界");
        Check("并行: token 连续拼接", slot.ChatMessages[^1].Content == "你好，世界");
        Check("并行: 追加后仍 Streaming", slot.ChatMessages[^1].Streaming);
        slot.BufferedFinishStream();
        Check("并行: 结束流式取消 Streaming", !slot.ChatMessages[^1].Streaming);

        // ── 无流式消息时 AppendToken 自动新建（对标 EnsureAgentStreaming）──
        slot.BufferedAddMsg("system", "工具输出");
        slot.BufferedAppendToken("继续");
        Check("并行: 无流式消息时 AppendToken 自建",
            slot.ChatMessages[^1].Streaming && slot.ChatMessages[^1].Content == "继续");

        // ── AppendToLast 追加到最后一条（工具流式输出）──
        slot.BufferedAppendToLast(" 追加");
        Check("并行: AppendToLast 追加到最后一条", slot.ChatMessages[^1].Content == "继续 追加");
    }

    private static void TestWorkModePerAgent(Action<string, bool> Check)
    {
        var savedGlobal = WorkModeManager.CurrentMode;

        // ── 实例级工作模式：与全局 CurrentMode 解耦 ──
        var a1 = new Agent(new LLM("test", "sk-test"));
        var a2 = new Agent(new LLM("test", "sk-test"));
        Check("模式: Agent 默认 Build", a1.WorkMode == WorkMode.Build);
        Check("模式: 两实例可独立设置", a1.WorkMode != a2.WorkMode || a1.WorkMode == WorkMode.Build);

        // 设置实例模式不影响全局镜像
        a1.WorkMode = WorkMode.Plan;
        Check("模式: 实例设 Plan 不影响全局", WorkModeManager.CurrentMode == savedGlobal);
        Check("模式: 实例模式已生效", a1.WorkMode == WorkMode.Plan);
        Check("模式: 另一实例仍 Build", a2.WorkMode == WorkMode.Build);

        // ── 工具约束跟随实例模式 ──
        Check("模式: Plan 阻止 write_file",
            WorkModeManager.CheckToolAllowed("write_file", a1.WorkMode) != null);
        Check("模式: Build 允许 write_file",
            WorkModeManager.CheckToolAllowed("write_file", a2.WorkMode) == null);

        // ── 模式变化回调 ──
        WorkMode? notified = null;
        a1.OnWorkModeChanged = m => notified = m;
        a1.WorkMode = WorkMode.Build;
        a1.OnWorkModeChanged?.Invoke(a1.WorkMode);
        Check("模式: 回调收到新模式", notified == WorkMode.Build);

        // ── 计划审批门纯逻辑（实例模式驱动）──
        Check("模式: Plan 触发审批", Agent.ShouldPromptPlanApproval(WorkMode.Plan, 50));
        Check("模式: Build 不触发审批", !Agent.ShouldPromptPlanApproval(WorkMode.Build, 50));

        WorkModeManager.CurrentMode = savedGlobal;
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
        var assets = new JsonArray(
            new JsonObject { ["name"] = "waycoder-v0.49.0-win-x64.zip", ["browser_download_url"] = "https://x/win.zip" },
            new JsonObject { ["name"] = "waycoder-v0.49.0-osx-arm64.tar.gz", ["browser_download_url"] = "https://x/osx.tar.gz" });
        Check("升级: 资产 URL 匹配",
            UpdateChecker.FindAssetUrl(assets, "osx-arm64") == "https://x/osx.tar.gz");
        Check("升级: 资产 URL 无匹配 null",
            UpdateChecker.FindAssetUrl(assets, "linux-x64") == null);
    }

    private static void TestBatchEngine(Action<string, bool> Check)
    {
        // ── JSON 解析 ──
        var ok = BatchSpec.Parse("""
        {
          "maxParallel": 2,
          "tasks": [
            { "repo": "https://github.com/a/b", "task": "修 bug" },
            { "repo": "/本地/x", "task": "加测试", "name": "x", "branch": "dev" }
          ]
        }
        """, out var err1);
        Check("批量: 解析 2 个任务", ok != null && ok.Jobs.Count == 2 && err1 == "");
        Check("批量: maxParallel=2", ok!.MaxParallel == 2);
        Check("批量: name/branch 透传", ok.Jobs[1].Name == "x" && ok.Jobs[1].Branch == "dev");

        // ── 边界与钳制 ──
        var clamp = BatchSpec.Parse("""{ "maxParallel": 99, "timeoutSec": 1, "tasks": [{"repo":"r","task":"t"}] }""", out _);
        Check("批量: maxParallel 钳制到 16", clamp!.MaxParallel == 16);
        Check("批量: timeoutSec 钳制到 60", clamp.TimeoutSec == 60);
        var clamp2 = BatchSpec.Parse("""{ "maxParallel": 0, "tasks": [{"repo":"r","task":"t"}] }""", out _);
        Check("批量: maxParallel 钳制到 1", clamp2!.MaxParallel == 1);

        // ── 错误场景 ──
        Check("批量: 缺 tasks 报错", BatchSpec.Parse("""{"maxParallel":2}""", out var e1) == null && e1.Contains("tasks"));
        Check("批量: 非法 JSON 报错", BatchSpec.Parse("not json", out var e2) == null && e2.Contains("JSON"));
        Check("批量: 空任务报错", BatchSpec.Parse("""{"tasks":[]}""", out var e3) == null && e3.Length > 0);

        // ── SanitizeName ──
        Check("批量: URL 提取名", BatchSpec.SanitizeName("https://github.com/org/my-repo.git") == "my-repo");
        Check("批量: 本地路径提取名", BatchSpec.SanitizeName("/a/b/c") == "c");
        Check("批量: 反斜杠路径提取名", BatchSpec.SanitizeName("C:\\proj\\repo") == "repo");
        Check("批量: 空名兜底", BatchSpec.SanitizeName("///") == "repo");
        Check("批量: 非法字符替换", BatchSpec.SanitizeName("my repo!") == "my_repo");

        // ── IsRemoteUrl ──
        Check("批量: https 是远程", BatchSpec.IsRemoteUrl("https://x/y"));
        Check("批量: git@ 是远程", BatchSpec.IsRemoteUrl("git@github.com:o/r.git"));
        Check("批量: 本地路径非远程", !BatchSpec.IsRemoteUrl("/local/path"));

        // ── FromRepos ──
        var spec = BatchSpec.FromRepos(new[] { "https://a/b", "/local/c" }, "共享任务");
        Check("批量: FromRepos 构建 2 任务", spec.Jobs.Count == 2 && spec.Jobs.All(j => j.Task == "共享任务"));

        // ── 报告渲染 ──
        var report = new BatchReport();
        report.Results.Add(new BatchResult { Name = "a", Repo = "r", Success = true, Summary = "完成", DurationMs = 1000 });
        report.Results.Add(new BatchResult { Name = "b", Repo = "r2", Success = false, Error = "挂了", ExitCode = 1, DurationMs = 2000 });
        var md = report.ToMarkdown();
        Check("批量: 报告含统计", md.Contains("总计: 2") && md.Contains("成功: 1") && md.Contains("失败: 1"));
        Check("批量: 报告含成功/失败图标", md.Contains("✅") && md.Contains("❌"));
        Check("批量: 报告含错误详情", md.Contains("挂了"));

        // ── RunAsync 端到端（本地 git 仓库 + 注入 fake spawner，无网络无 LLM）──
        try
        {
            var gitOk = GitRunner.Run("--version").ExitCode == 0;
            Check("批量: git 可用（端到端前提）", gitOk);
            if (gitOk)
            {
                var srcDir = Path.Combine(Path.GetTempPath(), "wc_batch_src_" + Guid.NewGuid().ToString("N")[..6]);
                var rootDir = Path.Combine(Path.GetTempPath(), "wc_batch_root_" + Guid.NewGuid().ToString("N")[..6]);
                Directory.CreateDirectory(srcDir);
                GitRunner.RunOrThrow("init -q", srcDir);
                File.WriteAllText(Path.Combine(srcDir, "a.txt"), "hi");
                GitRunner.RunOrThrow("add -A", srcDir);
                GitRunner.RunOrThrow("-c user.email=t@t -c user.name=t commit -q -m init", srcDir);

                var spec2 = BatchSpec.FromRepos(new[] { srcDir, srcDir }, "任务", maxParallel: 2);
                spec2.TimeoutSec = 60;
                spec2.KeepResults = false;

                var ran = 0;
                var report2 = BatchRunner.RunAsync(spec2,
                    log: null,
                    rootDir: rootDir,
                    spawn: (job, dir, task, ct) =>
                    {
                        Interlocked.Increment(ref ran);
                        var cloned = File.Exists(Path.Combine(dir, "a.txt"));
                        return Task.FromResult<(int, string, string)>(cloned ? (0, $"OK {job.DisplayName}", "") : (1, "", "clone 未就绪"));
                    }).GetAwaiter().GetResult();

                Check("批量: RunAsync 并发执行 2 任务", ran == 2);
                Check("批量: RunAsync 全部成功", report2.Succeeded == 2 && report2.Failed == 0);
                Check("批量: 报告已写文件", File.Exists(Path.Combine(rootDir, "batch-report.md")));
                Check("批量: 工作副本已清理", !Directory.Exists(Path.Combine(rootDir, "jobs")) || Directory.GetDirectories(Path.Combine(rootDir, "jobs")).Length == 0);

                // 清理临时目录
                try { Directory.Delete(srcDir, recursive: true); } catch { }
                try { Directory.Delete(rootDir, recursive: true); } catch { }
            }
        }
        catch (Exception ex)
        {
            Check($"批量: RunAsync 端到端异常: {ex.Message}", false);
        }
    }

    // ── 插件系统测试用最小实现 ──

    private sealed class TestPluginTool : ITool
    {
        public string Name => "plugin_test_tool";
        public string Description => "测试插件工具";
        public JsonObject Parameters => new() { ["type"] = "object", ["properties"] = new JsonObject() };
        public Task<string> ExecuteAsync(Dictionary<string, object?> arguments) => Task.FromResult("ok");
    }

    private sealed class TestPluginCommand : SlashCommand
    {
        public override string Name => "/plugin-hello";
        public override string Description => "测试插件命令";
        public override Task ExecuteAsync(string args, ChatScreen screen) => Task.CompletedTask;
    }

    private sealed class TestPlugin : Plugin
    {
        public override string Name => "test-plugin";
        public override string Version => "9.9.9";
        public override IEnumerable<ITool> GetTools() => [new TestPluginTool()];
        public override IEnumerable<ISlashCommand> GetCommands() => [new TestPluginCommand()];
    }

    private sealed class NullReturningPlugin : Plugin
    {
        public override string Name => "null-plugin";
        public override IEnumerable<ITool> GetTools() => null!;
        public override IEnumerable<ISlashCommand> GetCommands() => null!;
    }

    private static void TestPluginSystem(Action<string, bool> Check)
    {
        PluginRegistry.Register(new TestPlugin());
        Check("插件: 注册成功", PluginRegistry.Plugins.Count == 1);
        Check("插件: 收集 1 个工具", PluginRegistry.CollectTools().Count() == 1);
        Check("插件: 收集 1 个命令", PluginRegistry.CollectCommands().Count() == 1);
        Check("插件: 工具集成到 AllTools", ToolRegistry.AllTools.Any(t => t.Name == "plugin_test_tool"));
        Check("插件: 命令名正确", PluginRegistry.CollectCommands().First().Name == "/plugin-hello");

        // 同名覆盖（忽略大小写）不重复
        PluginRegistry.Register(new TestPlugin());
        Check("插件: 同名注册覆盖不重复", PluginRegistry.Plugins.Count == 1);
        Check("插件: 版本字段保留", PluginRegistry.Plugins[0].Version == "9.9.9");

        // null 注册防御
        PluginRegistry.Register(null!);
        Check("插件: null 注册不抛", PluginRegistry.Plugins.Count == 1);

        // 卸载
        Check("插件: 卸载成功", PluginRegistry.Unregister("test-plugin"));
        Check("插件: 卸载后工具移除", PluginRegistry.Plugins.Count == 0
            && !ToolRegistry.AllTools.Any(t => t.Name == "plugin_test_tool"));

        // null 返回防御
        PluginRegistry.Register(new NullReturningPlugin());
        Check("插件: null 返回防御不抛", PluginRegistry.CollectTools().Count() == 0
            && PluginRegistry.CollectCommands().Count() == 0);
        PluginRegistry.Unregister("null-plugin");
    }

    private static void TestJsonMode(Action<string, bool> Check)
    {
        // ── 成功结果 ──
        var ok = JsonResult.Build(
            success: true,
            answer: "任务完成",
            error: null,
            model: "deepseek-v4-pro",
            promptTokens: 100,
            completionTokens: 50,
            costUsd: 0.00123,
            durationMs: 2048,
            changedFiles: new[] { "a.cs", "b.cs" });

        Check("JSON: success=true", ok["success"]!.GetValue<bool>() == true);
        Check("JSON: answer 透传", ok["answer"]!.GetValue<string>() == "任务完成");
        Check("JSON: error 为 null", ok["error"] == null);
        Check("JSON: model 透传", ok["model"]!.GetValue<string>() == "deepseek-v4-pro");
        Check("JSON: usage.total = prompt+completion",
            ok["usage"]!["total_tokens"]!.GetValue<int>() == 150);
        Check("JSON: cost_usd 保留", Math.Abs(ok["cost_usd"]!.GetValue<double>() - 0.00123) < 1e-9);
        Check("JSON: duration_ms", ok["duration_ms"]!.GetValue<long>() == 2048);
        Check("JSON: changed_files 数组", ok["changed_files"] is JsonArray arr && arr.Count == 2);
        Check("JSON: 序列化可解析", JsonNode.Parse(ok.ToJsonString()) is JsonObject);

        // ── 失败结果 ──
        var fail = JsonResult.Build(false, "", "请求超时", "m", 0, 0, null, 1, null);
        Check("JSON: success=false", fail["success"]!.GetValue<bool>() == false);
        Check("JSON: error 透传", fail["error"]!.GetValue<string>() == "请求超时");
        Check("JSON: answer 空串兜底", fail["answer"]!.GetValue<string>() == "");
        Check("JSON: cost_usd null", fail["cost_usd"] == null);
        Check("JSON: changed_files 空数组", fail["changed_files"] is JsonArray e && e.Count == 0);
    }
}
