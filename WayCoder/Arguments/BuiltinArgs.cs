namespace WayCoder.Arguments;

// ═══════════════════════════════════════════════════════════════
// 模型参数
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// --model 模型管理（对标 /model 斜杠命令）。
///   --model                        → 显示当前模型
///   --model list [关键词]          → 列出模型目录
///   --model name &lt;id&gt;        → 选中并持久化（自动 base-url + 写 .env）
///   --model key &lt;供应商&gt; &lt;key&gt; → 保存 API key（无参列出已存 keys）
///   --model connect &lt;base-url&gt; → 设置连接地址（写 .env）
///   --model &lt;模型ID&gt;          → 快捷选中（本次会话，不持久化，向后兼容）
/// </summary>
public class ModelArg : CliArg
{
    public override string Description => "模型管理（list / name <id> / key <供应商> <key> / connect <url>，或 --model <模型ID> 快捷选中）";
    public override int ValueCount => -1;
    public override bool Greedy => true;
    public override string? ValueLabel => "模型ID/子命令";
    public ModelArg() : base("model", "-m", "--model") { }

    public override int? OnMatch(List<string> values)
    {
        if (values.Count == 0)
        {
            Console.WriteLine(ModelCli.Current());
            return 0;
        }

        var first = values[0].ToLowerInvariant();
        var rest = values.Skip(1).ToArray();

        string result;
        switch (first)
        {
            case "list":
            case "ls":
                result = ModelCli.List(rest.Length > 0 ? rest[0] : null);
                break;
            case "name":
                result = rest.Length == 0 ? "用法: --model name <模型ID>" : ModelCli.Select(rest[0]);
                break;
            case "key":
            case "keys":
                result = DispatchKey(rest);
                break;
            case "connect":
                result = rest.Length == 0 ? "用法: --model connect <base-url>" : ModelCli.Connect(rest[0]);
                break;
            default:
                // 裸模型名：本次会话快捷选中，交给 Program 继续运行
                return null;
        }

        Console.WriteLine(result);
        return 0;
    }

    static string DispatchKey(string[] rest)
    {
        if (rest.Length == 0) return ModelCli.ListKeys();
        if (rest.Length >= 3 && rest[0].Equals("set", StringComparison.OrdinalIgnoreCase))
            return ModelCli.SetKey(rest[1], string.Join(" ", rest.Skip(2)));
        if (rest.Length >= 2)
            return ModelCli.SetKey(rest[0], string.Join(" ", rest.Skip(1)));
        return "用法: --model key [set] <供应商> <key>";
    }
}

public class BaseUrlArg : CliArg
{
    public override string Description => "API 基础 URL";
    public override int ValueCount => 1;
    public override string? ValueLabel => "URL";
    public BaseUrlArg() : base("base-url", "-b", "--base-url") { }
}

public class ApiKeyArg : CliArg
{
    public override string Description => "API 密钥";
    public override int ValueCount => 1;
    public override string? ValueLabel => "密钥";
    public ApiKeyArg() : base("api-key", "-k", "--api-key") { }
}

// ═══════════════════════════════════════════════════════════════
// 会话参数
// ═══════════════════════════════════════════════════════════════

public class PromptArg : CliArg
{
    public override string Description => "一次性提示词。-p1~-p0 投递槽位, -pa 共享前缀, 同槽位可排队";
    public override int ValueCount => 1;
    public override string? ValueLabel => "文本";
    public PromptArg() : base("prompt", "-p", "--prompt") { }
}

public class ResumeArg : CliArg
{
    public override string Description => "恢复会话（-c 无参=最近会话，-c 名称=指定会话）";
    public override int ValueCount => -1; // 可选值：无参时恢复最近会话
    public override string? ValueLabel => "会话名";
    public ResumeArg() : base("resume", "-r", "--resume", "-c", "--continue") { }
}

public class MaxBudgetArg : CliArg
{
    public override string Description => "费用上限（美元），超支自动停止";
    public override int ValueCount => 1;
    public override string? ValueLabel => "金额";
    public MaxBudgetArg() : base("max-budget-usd", "-B", "--max-budget-usd") { }
}

// ═══════════════════════════════════════════════════════════════
// 标志参数
// ═══════════════════════════════════════════════════════════════

public class VersionArg : CliArg
{
    public override string Description => "显示版本信息";
    public VersionArg() : base("version", "-v", "--version") { }
}

public class InitArg : CliArg
{
    public override string Description => "初始化项目配置（.waycoder/ 目录）";
    public InitArg() : base("init", "-i", "--init") { }
}

public class YoloArg : CliArg
{
    public override string Description => "跳过所有权限确认（非交互模式自动开启）";
    public YoloArg() : base("yolo", "-y", "--yolo") { }
}

public class WatchArg : CliArg
{
    public override string Description => "Watch 模式（监听文件中的 AI! 注释）";
    public WatchArg() : base("watch", "-w", "--watch") { }
}

public class TinyArg : CliArg
{
    public override string Description => "Tiny 模式（精简提示词 + 小窗口；可指定如 --tiny 8k，缺省自动探测，失败回退 4K）";
    public override int ValueCount => -1;
    public override string? ValueLabel => "窗口";
    public TinyArg() : base("tiny", "-tt", "--test-tiny", "--tiny") { }
}

public class EconomyArg : CliArg
{
    public override string Description => "省 Token 模式（--economy [on|auto|off]，缺省 on；auto 按上下文占用率动态调节阈值）";
    public override int ValueCount => -1;
    public override string? ValueLabel => "模式";
    public EconomyArg() : base("economy", "-e", "--economy") { }
}

public class DebugArg : CliArg
{
    public override string Description => "开启调试日志（记录到 logs/ 目录）";
    public DebugArg() : base("debug", "-d", "--debug") { }
    public override int? OnMatch(List<string> values) { DebugLog.Enable(); return null; }
}

/// <summary>
/// --config 命令行配置（对标 /config 斜杠命令），无需进界面即可读写所有设置项。
///   --config                      → 列出全部
///   --config list                 → 同列出
///   --config get &lt;key&gt;      → 读取
///   --config set &lt;key&gt; &lt;v&gt; → 设置并写入 .env
///   --config &lt;key&gt; &lt;v&gt;  → set 简写
///   --config &lt;key&gt;           → get 简写
/// </summary>
public class ConfigArg : CliArg
{
    public override string Description => "命令行配置（list / get <key> / set <key> <value> 或 <key> [value]）";
    public override int ValueCount => -1;
    public override bool Greedy => true;
    public override string? ValueLabel => "项 [值]";
    public ConfigArg() : base("config", "-C", "--config") { }

    public override int? OnMatch(List<string> values)
    {
        string result;

        if (values.Count == 0)
            result = ConfigCli.List();
        else
        {
            var first = values[0].ToLowerInvariant();
            var rest = values.Skip(1).ToArray();
            switch (first)
            {
                case "list":
                case "ls":
                    result = ConfigCli.List();
                    break;
                case "get":
                    result = rest.Length == 0 ? "用法: --config get <key>" : ConfigCli.Get(rest[0]);
                    break;
                case "set":
                    result = rest.Length < 2 ? "用法: --config set <key> <value>" : ConfigCli.Set(rest[0], string.Join(" ", rest.Skip(1)));
                    break;
                default:
                    // 简写：--config <key> [value]
                    result = rest.Length == 0 ? ConfigCli.Get(values[0]) : ConfigCli.Set(values[0], string.Join(" ", rest));
                    break;
            }
        }

        Console.WriteLine(result);
        return 0;
    }
}

public class HelpArg : CliArg
{
    public override string Description => "显示此帮助";
    public HelpArg() : base("help", "-h", "--help") { }
}

// ═══════════════════════════════════════════════════════════════
// 工具/动作参数（匹配后立即执行并退出）
// ═══════════════════════════════════════════════════════════════

public class TestArg : CliArg
{
    public override string Description => "运行自测（可选指定模块名）";
    public override int ValueCount => -1;
    public override string? ValueLabel => "模块名";
    public TestArg() : base("test", "-t", "--test") { }
    public override int? OnMatch(List<string> values)
    {
        if (values.Count > 0)
            Console.WriteLine(SelfTest.RunModule(values[0]));
        else
            SelfTest.Run();
        return 0;
    }
}

public class BenchmarkArg : CliArg
{
    public override string Description => "运行性能测评";
    public BenchmarkArg() : base("bench", "-tb", "--test-benchmark", "--bench", "--perf") { }
    public override int? OnMatch(List<string> values) { Benchmark.Run(); return 0; }
}

public class LimitsArg : CliArg
{
    public override string Description => "运行系统上限报告（扫描所有硬编码上限）";
    public LimitsArg() : base("limits", "-tl", "--test-limits", "--limits") { }
    public override int? OnMatch(List<string> values) { Benchmark.LimitsReport(); return 0; }
}

public class ScreenshotArg : CliArg
{
    public override string Description => "截图模式";
    public override bool Internal => true;
    public ScreenshotArg() : base("screenshot", "-x", "--screenshot") { }
    public override int? OnMatch(List<string> values) { Program.RunScreenshot(); return 0; }
}

public class TuiDemoArg : CliArg
{
    public override string Description => "TUI 控件演示";
    public override bool Internal => true;
    public TuiDemoArg() : base("tui-demo", "-u", "--tui-demo") { }
    public override int? OnMatch(List<string> values) { TuiDemo.Run(); return 0; }
}

public class ThemeVerifyArg : CliArg
{
    public override string Description => "主题配色验证";
    public override bool Internal => true;
    public ThemeVerifyArg() : base("theme-verify", "-z", "--theme-verify") { }
    public override int? OnMatch(List<string> values) { ThemeVerify.Run(); return 0; }
}

// ═══════════════════════════════════════════════════════════════
// 槽位任务参数 — -p1 ~ -p0 对应 F1~F10
// ═══════════════════════════════════════════════════════════════

/// <summary>所有槽位任务的共享前缀（-pa "前缀" → 自动拼到每个 -pN 任务前面）</summary>
public class SlotPromptAllArg : CliArg
{
    public override string Description => "所有槽位任务的共享前缀（自动拼到每个 -pN 前面）";
    public override int ValueCount => 1;
    public override string? ValueLabel => "前缀";
    public SlotPromptAllArg() : base("prompt-all", "-pa", "--prompt-all") { }
}

public class SlotPromptArg : CliArg
{
    /// <summary>目标槽位索引（0-based，-p1→0, -p2→1, ..., -p0→9）</summary>
    public int SlotIndex { get; }
    public override string Description => $"投递任务到槽位 F{SlotIndex + 1}（-p1~-p9, -p0=F10）";
    public override int ValueCount => 1;
    public override string? ValueLabel => "文本";
    public override bool Internal => true; // 10 个参数不逐行显示
    public override bool AllowMultiple => true; // 同一槽位多次 -pN 可排队

    /// <param name="slotNum">用户输入的槽位号（1-9, 0=10），内部转为 0-based 索引</param>
    public SlotPromptArg(int slotNum) : base(
        $"slot-prompt-{slotNum}",
        $"-p{slotNum}",
        $"--prompt-slot-{slotNum}")
    {
        SlotIndex = slotNum switch
        {
            0 => 9,       // -p0 → F10 → 索引 9
            _ => slotNum - 1,  // -p1 → F1 → 索引 0, ...
        };
    }
}

public class SessionListArg : CliArg
{
    public override string Description => "列出所有已保存会话";
    public override int ValueCount => 0;
    public SessionListArg() : base("session-list", "-s", "--session-list", "--sessions") { }
}

// ═══════════════════════════════════════════════════════════════
// 注册入口 —— 应用启动时调用一次
// ═══════════════════════════════════════════════════════════════

public static class BuiltinArgs
{
    static bool _registered;

    /// <summary>注册所有内置 CLI 参数（幂等）。重复名称自动报错。</summary>
    public static void RegisterAll()
    {
        if (_registered) return;
        _registered = true;

        CliArgRegistry.Register(new ModelArg());
        CliArgRegistry.Register(new BaseUrlArg());
        CliArgRegistry.Register(new ApiKeyArg());
        CliArgRegistry.Register(new PromptArg());
        CliArgRegistry.Register(new ResumeArg());
        CliArgRegistry.Register(new SessionListArg());
        CliArgRegistry.Register(new MaxBudgetArg());
        CliArgRegistry.Register(new VersionArg());
        CliArgRegistry.Register(new InitArg());
        CliArgRegistry.Register(new YoloArg());
        CliArgRegistry.Register(new WatchArg());
        CliArgRegistry.Register(new TinyArg());
        CliArgRegistry.Register(new EconomyArg());
        CliArgRegistry.Register(new ConfigArg());
        CliArgRegistry.Register(new DebugArg());
        CliArgRegistry.Register(new HelpArg());
        CliArgRegistry.Register(new TestArg());
        CliArgRegistry.Register(new BenchmarkArg());
        CliArgRegistry.Register(new LimitsArg());
        CliArgRegistry.Register(new ScreenshotArg());
        CliArgRegistry.Register(new TuiDemoArg());
        CliArgRegistry.Register(new ThemeVerifyArg());

        // 槽位任务参数：-pa 共享前缀 + -p1 ~ -p9, -p0(=F10)
        CliArgRegistry.Register(new SlotPromptAllArg());
        for (int n = 0; n <= 9; n++)
            CliArgRegistry.Register(new SlotPromptArg(n));
    }
}
