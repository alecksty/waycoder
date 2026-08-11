namespace WayCoder.Arguments;

// ═══════════════════════════════════════════════════════════════
// 模型参数
// ═══════════════════════════════════════════════════════════════

public class ModelArg : CliArg
{
    public override string Description => "模型名称（默认: deepseek-v4-flash）";
    public override int ValueCount => 1;
    public override string? ValueLabel => "名称";
    public ModelArg() : base("model", "-m", "--model") { }
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
    public override string Description => "一次性提示词（非交互模式）";
    public override int ValueCount => 1;
    public override string? ValueLabel => "文本";
    public PromptArg() : base("prompt", "-p", "--prompt") { }
}

public class ResumeArg : CliArg
{
    public override string Description => "恢复已保存的会话";
    public override int ValueCount => 1;
    public override string? ValueLabel => "ID";
    public ResumeArg() : base("resume", "-r", "--resume") { }
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

public class DebugArg : CliArg
{
    public override string Description => "开启调试日志（记录到 logs/ 目录）";
    public DebugArg() : base("debug", "-d", "--debug") { }
    public override int? OnMatch(List<string> values) { DebugLog.Enable(); return null; }
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
    public BenchmarkArg() : base("bench", "--bench", "--benchmark", "--perf") { }
    public override int? OnMatch(List<string> values) { Benchmark.Run(); return 0; }
}

public class LimitsArg : CliArg
{
    public override string Description => "运行系统上限报告（扫描所有硬编码上限）";
    public LimitsArg() : base("limits", "--limits") { }
    public override int? OnMatch(List<string> values) { Benchmark.LimitsReport(); return 0; }
}

public class ScreenshotArg : CliArg
{
    public override string Description => "截图模式";
    public override bool Internal => true;
    public ScreenshotArg() : base("screenshot", "--screenshot") { }
    public override int? OnMatch(List<string> values) { Program.RunScreenshot(); return 0; }
}

public class TuiDemoArg : CliArg
{
    public override string Description => "TUI 控件演示";
    public override bool Internal => true;
    public TuiDemoArg() : base("tui-demo", "--tui-demo") { }
    public override int? OnMatch(List<string> values) { TuiDemo.Run(); return 0; }
}

public class ThemeVerifyArg : CliArg
{
    public override string Description => "主题配色验证";
    public override bool Internal => true;
    public ThemeVerifyArg() : base("theme-verify", "--theme-verify") { }
    public override int? OnMatch(List<string> values) { ThemeVerify.Run(); return 0; }
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
        CliArgRegistry.Register(new MaxBudgetArg());
        CliArgRegistry.Register(new VersionArg());
        CliArgRegistry.Register(new InitArg());
        CliArgRegistry.Register(new YoloArg());
        CliArgRegistry.Register(new WatchArg());
        CliArgRegistry.Register(new DebugArg());
        CliArgRegistry.Register(new HelpArg());
        CliArgRegistry.Register(new TestArg());
        CliArgRegistry.Register(new BenchmarkArg());
        CliArgRegistry.Register(new LimitsArg());
        CliArgRegistry.Register(new ScreenshotArg());
        CliArgRegistry.Register(new TuiDemoArg());
        CliArgRegistry.Register(new ThemeVerifyArg());
    }
}
