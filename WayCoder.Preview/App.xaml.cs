using System.Windows;
using WayCoder.Preview.Render;
using WayCoder.UI.TUI.Base;

namespace WayCoder.Preview;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 无头自检：--selftest <file.tui> → 渲染并统计非空格格子数，退出码 0=成功 1=失败。
        // 用 Environment.Exit 强制退出：渲染控件可能起定时器，Application.Shutdown 偶发不退出导致挂起。
        if (e.Args.Length == 2 && e.Args[0] == "--selftest")
        {
            int code = RenderSelfTest(e.Args[1]);
            Environment.Exit(code);
            return;
        }
        // 诊断转储：--dump <file.tui> [cols rows] → 渲染并把网格逐行写入当前目录 tuidump.txt（核对宽字符数据）
        if (e.Args.Length >= 2 && e.Args[0] == "--dump")
        {
            int dc = 80, dr = 25;
            if (e.Args.Length == 4)
            {
                int.TryParse(e.Args[2], out dc);
                int.TryParse(e.Args[3], out dr);
            }
            DumpGrid(e.Args[1], dc, dr);
            Environment.Exit(0);
            return;
        }
        // 渲染成 PNG：--png <file.tui> <out.png> → 看 WPF 实际绘制效果
        if (e.Args.Length == 3 && e.Args[0] == "--png")
        {
            RenderPng(e.Args[1], e.Args[2]);
            Environment.Exit(0);
            return;
        }
        // 字形诊断：--glyph <字符> → 渲染单个字符到位图 glyph.png，量实际像素宽度
        if (e.Args.Length == 2 && e.Args[0] == "--glyph")
        {
            GlyphTest(e.Args[1]);
            Environment.Exit(0);
            return;
        }

        var win = new MainWindow();
        // 命令行参数 = 待预览 .tui 路径（可选）
        if (e.Args.Length > 0 && File.Exists(e.Args[0]))
            win.LoadFile(e.Args[0]);
        MainWindow = win;
        win.Show();
    }

    /// <summary>诊断转储：渲染并把网格逐行写入当前目录 tuidump.txt（核对宽字符数据与 IsWide 判定）。</summary>
    private static void DumpGrid(string path, int colsArg, int rowsArg)
    {
        string outPath = Path.Combine(Directory.GetCurrentDirectory(), "tuidump.txt");
        try
        {
            var content = File.ReadAllText(path);
            var (frame, cols, rows) = TuiFrameRenderer.Render(content, colsArg, rowsArg);
            // 原始帧（剥离 ANSI 版 + 原始 ANSI 版）另存，便于核对窗口实际渲染内容
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "tuiframe.txt"),
                WayCoder.UI.Shared.Terminal.AnsiString.Strip(frame));
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "tuiframe_raw.txt"), frame);
            var snap = FrameSnapshot.Capture(frame, 0, 0, cols, rows);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"尺寸 {cols}x{rows}");
            // 宽字符判定诊断 + WPF 字体度量（等宽、CJK、cellW）
            var typeface = new System.Windows.Media.Typeface("Consolas");
            double ascii = new System.Windows.Media.FormattedText("M",
                System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight,
                typeface, 14, System.Windows.Media.Brushes.Black, 1.0).Width;
            double cjk = new System.Windows.Media.FormattedText("中",
                System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight,
                typeface, 14, System.Windows.Media.Brushes.Black, 1.0).Width;
            double ascii125 = new System.Windows.Media.FormattedText("M",
                System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight,
                typeface, 14, System.Windows.Media.Brushes.Black, 1.25).Width;
            double cjk125 = new System.Windows.Media.FormattedText("中",
                System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight,
                typeface, 14, System.Windows.Media.Brushes.Black, 1.25).Width;
            sb.AppendLine($"metric ascii(M)={ascii:F2} cjk(中)={cjk:F2} cjk/2={cjk / 2:F2} cellW=max={Math.Max(ascii, cjk / 2):F2}");
            sb.AppendLine($"metric125 ascii={ascii125:F2} cjk={cjk125:F2} (dpi 1.25 vs 1.0 差值={ascii125 - ascii:F2}/{cjk125 - cjk:F2})");
            foreach (string probe in new[] { "中", "A", "面", "板" })
            {
                var r0 = probe.EnumerateRunes().FirstOrDefault();
                int w = WayCoder.UI.Shared.Terminal.AnsiString.CharWidth(r0);
                bool wide = w >= 2;
                sb.AppendLine($"probe '{probe}' CharWidth={w} IsWide={wide}");
            }
            for (int r = 0; r < snap.H; r++)
            {
                var line = new System.Text.StringBuilder();
                for (int c = 0; c < snap.W; c++)
                    line.Append(snap.CharAt(r, c) == " " ? "·" : snap.CharAt(r, c));
                sb.AppendLine(line.ToString());
            }
            File.WriteAllText(outPath, sb.ToString());
        }
        catch (Exception ex) { File.WriteAllText(outPath, "EX: " + ex); }
    }

    /// <summary>字形诊断：渲染单个字符到 glyph.png，量实际像素宽度（排除字体回退把 CJK 渲染成半宽）。</summary>
    private static void GlyphTest(string ch)
    {
        try
        {
            var typeface = new System.Windows.Media.Typeface("Consolas");
            var ft = new System.Windows.Media.FormattedText(ch,
                System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight,
                typeface, 14, System.Windows.Media.Brushes.White, 1.0);
            double w = ft.Width, h = ft.Height;
            int pw = (int)Math.Ceiling(w) + 8, ph = (int)Math.Ceiling(h) + 8;
            var dv = new System.Windows.Media.DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawRectangle(System.Windows.Media.Brushes.Black, null, new Rect(0, 0, pw, ph));
                dc.DrawText(ft, new Point(4, 4));
            }
            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(pw, ph, 96, 96,
                System.Windows.Media.PixelFormats.Pbgra32);
            rtb.Render(dv);
            var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
            enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
            using var fs = File.Create("glyph.png");
            enc.Save(fs);
            File.WriteAllText("glyph.txt", $"char='{ch}' layoutWidth={w:F2} layoutHeight={h:F2} bitmap={pw}x{ph}");
        }
        catch (Exception ex) { File.WriteAllText("glyph.txt", "EX: " + ex); }
    }

    /// <summary>把网格渲染成 PNG（诊断 WPF 实际绘制效果）。</summary>
    private static void RenderPng(string path, string outPng)
    {
        try
        {
            var content = File.ReadAllText(path);
            var (frame, cols, rows) = TuiFrameRenderer.Render(content);
            var snap = FrameSnapshot.Capture(frame, 0, 0, cols, rows);

            // 诊断：标题行每格的字符/span/裁剪宽度
            var log = new System.Text.StringBuilder();
            for (int c = 0; c < snap.W; c++)
            {
                var ch = snap.CharAt(1, c);
                if (ch == " ") continue;
                bool wide = WayCoder.UI.Shared.Terminal.AnsiString.CharWidth(
                    ch.EnumerateRunes().FirstOrDefault()) >= 2;
                log.AppendLine($"row1 col{c} '{ch}' wide={wide} span={(wide ? 2 : 1)}");
            }
            File.WriteAllText(outPng + ".cells", log.ToString());

            var panel = new Render.TuiGridPanel { Background = System.Windows.Media.Brushes.Black };
            panel.SetGrid(snap);
            panel.Measure(new System.Windows.Size(2000, 2000));
            panel.Arrange(new System.Windows.Rect(0, 0, panel.DesiredSize.Width, panel.DesiredSize.Height));
            panel.UpdateLayout();
            int w = (int)Math.Ceiling(panel.DesiredSize.Width);
            int h = (int)Math.Ceiling(panel.DesiredSize.Height);
            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                Math.Max(1, w), Math.Max(1, h), 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtb.Render(panel);
            var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
            enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
            using var fs = File.Create(outPng);
            enc.Save(fs);
        }
        catch (Exception ex) { File.WriteAllText(outPng + ".log", "EX: " + ex); }
    }

    /// <summary>无头渲染自检：返回 0 表示成功（网格非空）。</summary>
    private static int RenderSelfTest(string path)
    {
        try
        {
            if (!File.Exists(path)) return 1;
            var content = File.ReadAllText(path);
            var (frame, cols, rows) = TuiFrameRenderer.Render(content);
            var snap = FrameSnapshot.Capture(frame, 0, 0, cols, rows);
            if (snap == null) return 1;
            int nonEmpty = 0;
            for (int r = 0; r < snap.H; r++)
                for (int c = 0; c < snap.W; c++)
                    if (snap.CharAt(r, c) is { Length: > 0 } ch && ch != " ")
                        nonEmpty++;
            return nonEmpty > 0 ? 0 : 1;
        }
        catch { return 1; }
    }
}
