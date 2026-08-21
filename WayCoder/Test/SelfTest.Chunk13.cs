using WayCoder.UI.Shared.Terminal;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>
    /// [终端实测宽度] —— 用「+字符*」+ CPR 光标位置查询实测当前终端字体的真实字符列宽，
    /// 与静态宽度表 AnsiString.CharWidth 逐一比对。不一致即静态表需按当前终端校准。
    /// 非真实 TTY（管道/CI）下 CPR 不可用，跳过并提示。
    /// </summary>
    private static void TestChunk13(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[终端实测宽度]");

        // ── 源码字符扫描（纯函数，无需 TTY，始终执行）──
        var dir = Path.Combine(Path.GetTempPath(), "wcp_scan_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "a.cs"), "中 文+ASCII+中文");
        File.WriteAllText(Path.Combine(dir, "b.md"), "Hello ✓ 世界 ⏰");
        Directory.CreateDirectory(Path.Combine(dir, "bin"));
        File.WriteAllText(Path.Combine(dir, "bin", "gen.cs"), "📦"); // bin 目录应被排除
        var chars = TerminalWidthProbe.ScanNonAsciiChars(dir);
        // 中/文/✓/世/界/⏰ = 6 个（bin 目录的 📦 被排除）
        Check("ScanNonAsciiChars 提取非 ASCII 并去重", chars.Count == 6);
        Check("ScanNonAsciiChars 含汉字", chars.Contains("中") && chars.Contains("文") && chars.Contains("世") && chars.Contains("界"));
        Check("ScanNonAsciiChars 含符号", chars.Contains("✓") && chars.Contains("⏰"));
        Check("ScanNonAsciiChars 排除 bin 目录", !chars.Contains("📦"));
        Directory.Delete(dir, recursive: true);

        // ── 终端实测校准（需真实 TTY：管道/CI 下 CPR 无响应，跳过）──
        if (!TerminalWidthProbe.CanProbe)
        {
            Console.WriteLine("  （跳过：非真实 TTY，CPR 光标查询不可用。终端里跑 `waycoder --width-probe` 可校准）");
            return;
        }

        var results = TerminalWidthProbe.ProbeAll();
        foreach (var r in results)
        {
            if (r.ActualWidth is not int a) continue;
            Check($"实测=静态 {r.Char} {r.Label}（静态{r.StaticWidth}/实测{a}）", r.Consistent);
        }
        Check("终端实测项全部可测", results.Count > 0 && results.All(r => r.ActualWidth != null));
    }
}
