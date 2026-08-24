using WayCoder.Tools;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Cli.Arguments;

// ═══════════════════════════════════════════════════════════════
// 工具/动作参数（匹配后立即执行并退出）
// ═══════════════════════════════════════════════════════════════

#if WAYCODER_TEST
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
#endif // WAYCODER_TEST

public class ScreenshotArg : CliArg
{
    public override string Description => "截图模式";
    public override bool Internal => true;
    public ScreenshotArg() : base("screenshot", "-x", "--screenshot") { }
    public override int? OnMatch(List<string> values) { Program.RunScreenshot(); return 0; }
}

/// <summary>--sysprompt-size：对比各模式 SystemPrompt + 工具 schema 大小（省钱模式效果验证）。</summary>
public class SyspromptSizeArg : CliArg
{
    public override string Description => "对比 SystemPrompt 各模式大小（省钱验证）";
    public override bool Internal => true;
    public SyspromptSizeArg() : base("sysprompt-size", "--sysprompt-size") { }
    public override int? OnMatch(List<string> values)
    {
        Program.RunSyspromptSize();
        return 0;
    }
}

/// <summary>
/// --width-probe [目录]：终端实测字符宽度，与静态宽度表 AnsiString.CharWidth 比对，输出不一致项供校准。
/// 用「+字符*」+ CPR 光标位置查询测量真实显示列宽（需真实 TTY）。
/// 无目录 → 测内置代表字符集（ProbeSet）；带目录 → 扫描该目录源码中全部非 ASCII 字符逐个实测。
/// </summary>
public class WidthProbeArg : CliArg
{
    public override string Description => "终端实测字符宽度，与静态宽度表比对校准（可带目录扫描源码字符）";
    public override bool Internal => true;
    public override int ValueCount => -1;
    public override string? ValueLabel => "目录";
    public WidthProbeArg() : base("width-probe", "--width-probe", "-wp") { }
    public override int? OnMatch(List<string> values)
        => UI.Shared.Terminal.TerminalWidthProbe.PrintReport(values.Count > 0 ? values[0] : null);
}

#if WAYCODER_TEST
public class TuiDemoArg : CliArg
{
    public override string Description => "TUI 控件演示";
    public override bool Internal => true;
    public TuiDemoArg() : base("tui-demo", "-u", "--tui-demo") { }
    public override int? OnMatch(List<string> values) { TuiDemo.Run(); return 0; }
}

public class TuiAuditArg : CliArg
{
    public override string Description => "TUI 对话框/控件渲染审计（输出纯文本帧）";
    public override bool Internal => true;
    public TuiAuditArg() : base("tui-audit", "--tui-audit") { }
    public override int? OnMatch(List<string> values) { TuiAudit.Run(); return 0; }
}

public class DialogShowArg : CliArg
{
    public override string Description => "对话框仅绘制演示（1~6 行消息 + 指定位置，抓屏核对布局）";
    public override bool Internal => true;
    public DialogShowArg() : base("dialog-show", "--dialog-show") { }
    public override int? OnMatch(List<string> values) { DialogShow.Run(); return 0; }
}

public class TuiPreviewArg : CliArg
{
    public override string Description => "预览 .tui 标记文件（声明式 TUI 布局）";
    public override bool Internal => true;
    public override int ValueCount => 1;
    public override string? ValueLabel => "标记文件";
    public TuiPreviewArg() : base("tui-preview", "--tui-preview") { }
    public override int? OnMatch(List<string> values)
    {
        var path = values.Count > 0 ? values[0] : null;
        return TuiPreview.Run(path ?? "");
    }
}

public class TuiWatchArg : CliArg
{
    public override string Description => "实时预览 .tui（保存即刷新，边写边预览）";
    public override bool Internal => true;
    public override int ValueCount => 1;
    public override string? ValueLabel => "标记文件";
    public TuiWatchArg() : base("tui-watch", "--tui-watch") { }
    public override int? OnMatch(List<string> values)
    {
        var path = values.Count > 0 ? values[0] : null;
        return TuiPreview.Watch(path ?? "");
    }
}

public class TuiMarkupDemoArg : CliArg
{
    public override string Description => "声明式 TUI 演示（tuidemo/*.tui 重构聊天界面与对话框）";
    public override bool Internal => true;
    public TuiMarkupDemoArg() : base("tui-markup-demo", "--tui-markup-demo") { }
    public override int? OnMatch(List<string> values) { TuiMarkupDemo.Run(); return 0; }
}
#endif // WAYCODER_TEST

/// <summary>用 .tui 标记版聊天界面启动（等价 WAYCODER_MARKUP_UI=1，供测试标记版界面）。</summary>
public class TuiChatArg : CliArg
{
    public override string Description => "用 .tui 标记版聊天界面启动（等价 WAYCODER_MARKUP_UI=1）";
    public override bool Internal => true;
    public TuiChatArg() : base("tui-chat", "--tui-chat") { }
    public override int? OnMatch(List<string> values) { Program.MarkupChatOverride = true; return null; }
}

public class GuiArg : CliArg
{
    public override string Description => "启动图形界面（独立 Avalonia 进程）";
    public GuiArg() : base("gui", "-g", "--gui") { }
    public override int? OnMatch(List<string> values)
    {
        // GUI 是独立 JIT 进程（WayCoder.Gui 项目），主程序 AOT 无法内嵌，需拉起其可执行文件
        var exeName = OperatingSystem.IsWindows() ? "waycoder-gui.exe" : "waycoder-gui";
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, exeName),                       // 已发布（与主程序同级）
            Path.Combine(baseDir, "waycoder-gui.dll"),            // 开发态（dotnet 运行）
            Path.Combine(baseDir, "..", "..", "WayCoder.Gui", "bin", "Debug", "net10.0", "waycoder-gui.dll"),
        };

        string? target = null;
        foreach (var c in candidates)
        {
            if (File.Exists(c)) { target = c; break; }
        }

        if (target == null)
        {
            Console.Error.WriteLine("未找到 GUI 可执行文件。请先构建 GUI 项目：dotnet build WayCoder.Gui");
            return 1;
        }

        try
        {
            // dll 走 dotnet 宿主；可执行文件直接启动
            var psi = target.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                ? new System.Diagnostics.ProcessStartInfo("dotnet", $"\"{target}\"") { UseShellExecute = false }
                : new System.Diagnostics.ProcessStartInfo(target) { UseShellExecute = true };
            System.Diagnostics.Process.Start(psi);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"启动 GUI 失败: {ex.Message}");
            return 1;
        }
    }
}

#if WAYCODER_TEST
public class KeypadArg : CliArg
{
    public override string Description => "按键脚本驱动 TUI（KEY/TEXT/DELAY/SNAP/DIALOG）+ 帧截图";
    public override int ValueCount => 1;
    public override string? ValueLabel => "脚本文件";
    public KeypadArg() : base("keypad", "--keypad") { }
    public override int? OnMatch(List<string> values)
    {
        var path = values.Count > 0 ? values[0] : null;
        if (string.IsNullOrWhiteSpace(path))
        {
            Console.Error.WriteLine("用法: waycoder --keypad <脚本文件>");
            return 1;
        }
        return Keypad.Run(path);
    }
}
#endif // WAYCODER_TEST

public class ThemeVerifyArg : CliArg
{
    public override string Description => "主题配色验证";
    public override bool Internal => true;
    public ThemeVerifyArg() : base("theme-verify", "-z", "--theme-verify") { }
    public override int? OnMatch(List<string> values) { ThemeVerify.Run(); return 0; }
}
