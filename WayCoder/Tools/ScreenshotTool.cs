using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI.Base;

namespace WayCoder.Tools;

/// <summary>
/// 抓屏工具 —— 让 Agent 自己「看到」当前画面。
///
/// 支持三种目标：
///   - console：导出当前终端 TUI 画面（剥离 ANSI 后的纯文本），直接可读
///   - screen ：抓取整个桌面（图形界面），保存 PNG，附带 OCR（若装了 tesseract）
///   - region ：抓取指定矩形区域（x/y/width/height），保存 PNG，附带 OCR
///
/// 跨平台 GUI 抓屏实现：
///   - Windows：powershell + System.Drawing.CopyFromScreen（Windows PowerShell 5.1 内置）
///   - macOS  ：/usr/sbin/screencapture（系统内置）
///   - Linux  ：grim(Wayland) → import(ImageMagick) → scrot → maim 依次回退
///
/// 说明：LLM 无法直接「看」图片，因此 GUI 抓屏会把 PNG 存到文件并尽力 OCR 成文本，
/// 让 Agent 能读取屏幕上的文字内容；PNG 路径则留给用户查看。
/// </summary>
public class ScreenshotTool : ITool
{
    public string Name => "screenshot";
    public string Description =>
        "抓取屏幕画面供自己查看。target 可选：console（默认，导出当前终端 TUI 纯文本画面，直接可读）；" +
        "screen（抓取整个桌面保存 PNG 并尝试 OCR 文字）；region（抓取矩形区域，需 x/y/width/height 参数）。" +
        "GUI 抓屏结果会保存为 PNG 文件并尽力 OCR 成文字返回，因为模型无法直接看图片。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("target", JNode.Object()
                .Set("type", "string")
                .Set("enum", JNode.Array().Add("console").Add("screen").Add("region"))
                .Set("description", "抓取目标：console=终端画面纯文本（默认）；screen=整个桌面；region=指定区域"))
            .Set("x", JNode.Object()
                .Set("type", "integer")
                .Set("description", "region 模式：区域左上角 X 坐标（像素）"))
            .Set("y", JNode.Object()
                .Set("type", "integer")
                .Set("description", "region 模式：区域左上角 Y 坐标（像素）"))
            .Set("width", JNode.Object()
                .Set("type", "integer")
                .Set("description", "region 模式：区域宽度（像素）"))
            .Set("height", JNode.Object()
                .Set("type", "integer")
                .Set("description", "region 模式：区域高度（像素）"))
            .Set("save_path", JNode.Object()
                .Set("type", "string")
                .Set("description", "GUI 抓屏 PNG 的保存路径（默认 ~/.waycoder/screenshots/ 自动命名）")))
        .Set("required", JNode.Array());

    // 完整 ANSI 转义序列匹配：CSI / OSC / 两字符转义
    internal static readonly Regex AnsiEscape = new(
        @"\x1B(?:\[[0-9;?]*[ -/]*[@-~]|\][^\x07]*(?:\x07|\x1B\\)|[@-Z\\-_])",
        RegexOptions.None);

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var target = (arguments.GetValueOrDefault("target")?.ToString() ?? "console").ToLowerInvariant();

        return Task.FromResult(target switch
        {
            "screen" => CaptureScreen(arguments, full: true),
            "region" => CaptureScreen(arguments, full: false),
            _ => CaptureConsole(),
        });
    }

    /// <summary>剥离 ANSI 转义序列，得到纯文本（internal 供自测）</summary>
    internal static string StripAnsi(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        // 先替换掉 OSC / CSI / 两字符转义
        var text = AnsiEscape.Replace(input, "");
        // 逐行去掉行尾空白，保留大体排版
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
            lines[i] = lines[i].TrimEnd();
        return string.Join('\n', lines).Trim();
    }

    /// <summary>导出当前终端 TUI 画面的纯文本</summary>
    private static string CaptureConsole()
    {
        try
        {
            var frame = TuiManager.Instance.LastCleanFrame;
            if (string.IsNullOrWhiteSpace(frame))
                return "（终端 TUI 尚未渲染或当前处于非交互模式，无可用画面。）";

            var text = StripAnsi(frame);
            if (string.IsNullOrWhiteSpace(text))
                return "（画面剥离 ANSI 后为空。）";

            return "📺 当前终端画面（纯文本）：\n" + text;
        }
        catch (Exception ex)
        {
            return $"抓取终端画面出错：{ex.GetType().Name}: {ex.Message}";
        }
    }

    /// <summary>抓取桌面 / 区域，保存 PNG，返回路径 + 尺寸 + 尽力 OCR</summary>
    private static string CaptureScreen(Dictionary<string, object?> arguments, bool full)
    {
        try
        {
            // 先校验 region 参数（与平台无关），再按平台分派抓屏，避免报错信息被平台守卫截胡
            var x = 0; var y = 0; var w = 0; var h = 0;
            if (!full)
            {
                x = ToolArgs.GetInt(arguments, "x");
                y = ToolArgs.GetInt(arguments, "y");
                w = ToolArgs.GetInt(arguments, "width");
                h = ToolArgs.GetInt(arguments, "height");
                if (w <= 0 || h <= 0)
                    return "错误：region 模式需要 width/height 为正整数，且 x/y 需指定。";
            }

            // 确定保存路径
            var savePath = arguments.GetValueOrDefault("save_path")?.ToString();
            if (string.IsNullOrWhiteSpace(savePath))
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".waycoder", "screenshots");
                Directory.CreateDirectory(dir);
                savePath = Path.Combine(dir, $"shot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            }
            else
            {
                var fullPath = Path.GetFullPath(savePath, BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory()); // cd 后相对路径基于被跟踪工作目录
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                savePath = fullPath;
            }

            var (ok, err) = TryCapture(full, x, y, w, h, savePath);
            if (!ok) return err;

            var fi = new FileInfo(savePath);
            var (wpx, hpx) = ReadPngDimensions(savePath);
            var sb = new StringBuilder();
            sb.AppendLine("📸 截图已保存");
            sb.AppendLine($"  路径: {savePath}");
            sb.AppendLine($"  大小: {fi.Length:N0} bytes");
            if (wpx > 0 && hpx > 0) sb.AppendLine($"  尺寸: {wpx} × {hpx} px");

            var ocr = TryOcr(savePath);
            if (!string.IsNullOrWhiteSpace(ocr))
            {
                sb.AppendLine();
                sb.AppendLine("🔍 OCR 识别到的文字：");
                sb.Append(ocr.Trim());
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine("（未检测到 tesseract，无法 OCR。安装：macOS brew install tesseract / Debian apt install tesseract-ocr）");
            }

            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            return $"图形抓屏出错：{ex.GetType().Name}: {ex.Message}";
        }
    }

    /// <summary>Linux 可用截图工具回退链（Wayland→X11）</summary>
    internal static readonly string[] LinuxCaptureTools = ["grim", "import", "scrot", "maim"];

    /// <summary>按平台分派抓屏。返回 (是否成功, 错误信息)。</summary>
    private static (bool Ok, string Err) TryCapture(bool full, int x, int y, int w, int h, string savePath)
    {
        if (OperatingSystem.IsWindows())
            return RunCapture(BuildWindowsCapture(full, x, y, w, h, savePath), savePath);
        if (OperatingSystem.IsMacOS())
            return RunCapture(BuildMacCapture(full, x, y, w, h, savePath), savePath);
        if (OperatingSystem.IsLinux())
            return CaptureLinux(full, x, y, w, h, savePath);
        return (false, "错误：当前操作系统不支持图形抓屏（仅支持 Windows / macOS / Linux）。");
    }

    /// <summary>执行一次抓屏命令并校验产物文件已生成。</summary>
    private static (bool Ok, string Err) RunCapture((string Tool, string Args) cmd, string savePath)
    {
        try
        {
            var (exitCode, output) = RunProcess(cmd.Tool, cmd.Args);
            if (exitCode != 0 || !File.Exists(savePath))
                return (false, $"抓屏失败（{cmd.Tool} exit={exitCode}）：{(string.IsNullOrWhiteSpace(output) ? "无输出" : output.Trim())}");
            return (true, "");
        }
        catch (Exception ex)
        {
            return (false, $"{cmd.Tool} 不可用：{ex.GetType().Name}");
        }
    }

    /// <summary>Linux：依次尝试各截图工具，首个成功即返回。</summary>
    private static (bool Ok, string Err) CaptureLinux(bool full, int x, int y, int w, int h, string savePath)
    {
        string lastErr = "";
        foreach (var tool in LinuxCaptureTools)
        {
            var (ok, err) = RunCapture(BuildLinuxCommandFor(tool, full, x, y, w, h, savePath), savePath);
            if (ok) return (true, "");
            lastErr = err;
        }
        return (false, "错误：Linux 抓屏失败，已尝试 " + string.Join(" / ", LinuxCaptureTools) +
            "。请安装其一（如：sudo apt install scrot 或 sudo apt install imagemagick）。最后错误：" + lastErr);
    }

    /// <summary>Windows：powershell + System.Drawing.CopyFromScreen（Windows PowerShell 5.1 内置，纯逻辑供自测）。</summary>
    internal static (string Tool, string Args) BuildWindowsCapture(bool full, int x, int y, int w, int h, string savePath)
    {
        var psPath = savePath.Replace("'", "''");
        var sb = new StringBuilder();
        sb.Append("Add-Type -AssemblyName System.Windows.Forms;Add-Type -AssemblyName System.Drawing;");
        if (full)
        {
            sb.Append("$b=[System.Windows.Forms.SystemInformation]::VirtualScreen;");
            sb.Append("$bmp=New-Object System.Drawing.Bitmap($b.Width,$b.Height);");
            sb.Append("$g=[System.Drawing.Graphics]::FromImage($bmp);");
            sb.Append("$g.CopyFromScreen($b.X,$b.Y,0,0,$bmp.Size);");
        }
        else
        {
            sb.Append($"$bmp=New-Object System.Drawing.Bitmap({w},{h});");
            sb.Append("$g=[System.Drawing.Graphics]::FromImage($bmp);");
            sb.Append($"$g.CopyFromScreen({x},{y},0,0,$bmp.Size);");
        }
        sb.Append($"$bmp.Save('{psPath}',[System.Drawing.Imaging.ImageFormat]::Png);");
        sb.Append("$g.Dispose();$bmp.Dispose();");
        var args = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{sb}\"";
        return ("powershell", args);
    }

    /// <summary>macOS：screencapture（系统内置，纯逻辑供自测）。</summary>
    internal static (string Tool, string Args) BuildMacCapture(bool full, int x, int y, int w, int h, string savePath)
    {
        var flags = "-x"; // 静默，无快门声
        if (!full) flags += $" -R{x},{y},{w},{h}";
        return ("/usr/sbin/screencapture", $"{flags} \"{savePath}\"");
    }

    /// <summary>Linux：单个工具的参数构造（纯逻辑供自测）。</summary>
    internal static (string Tool, string Args) BuildLinuxCommandFor(string tool, bool full, int x, int y, int w, int h, string savePath)
    {
        switch (tool)
        {
            case "grim": // Wayland：-g "<x>,<y> <w>x<h>"
                return full ? ("grim", $"\"{savePath}\"")
                            : ("grim", $"-g \"{x},{y} {w}x{h}\" \"{savePath}\"");
            case "import": // ImageMagick：-crop WxH+X+Y
                return full ? ("import", $"-window root \"{savePath}\"")
                            : ("import", $"-window root -crop {w}x{h}+{x}+{y} \"{savePath}\"");
            case "scrot": // -a X,Y,W,H
                return full ? ("scrot", $"\"{savePath}\"")
                            : ("scrot", $"-a {x},{y},{w},{h} \"{savePath}\"");
            case "maim": // -g WxH+X+Y
                return full ? ("maim", $"\"{savePath}\"")
                            : ("maim", $"-g {w}x{h}+{x}+{y} \"{savePath}\"");
            default:
                return (tool, $"\"{savePath}\"");
        }
    }

    /// <summary>读取 PNG 宽高（IHDR 块内 big-endian，无需第三方库）</summary>
    internal static (int Width, int Height) ReadPngDimensions(string path)
    {
        try
        {
            var header = new byte[24];
            using var fs = File.OpenRead(path);
            if (fs.Read(header, 0, header.Length) < 24) return (0, 0);
            // PNG 签名 8 字节 + IHDR 长度(4) + "IHDR"(4) + width(4) + height(4)
            if (header[0] != 0x89 || header[1] != 'P' || header[2] != 'N' || header[3] != 'G')
                return (0, 0);
            var width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
            var height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
            return (width, height);
        }
        catch
        {
            return (0, 0);
        }
    }

    /// <summary>若系统装有 tesseract，则 OCR 出图片文字；否则返回 null</summary>
    private static string? TryOcr(string imagePath)
    {
        try
        {
            var (exitCode, output) = RunProcess("tesseract", $"\"{imagePath}\" stdout 2>/dev/null");
            if (exitCode == 0 && !string.IsNullOrWhiteSpace(output))
                return output;
        }
        catch
        {
            // tesseract 不存在或调用失败，忽略
        }
        return null;
    }

    /// <summary>运行一个外部命令，返回（退出码，合并输出）</summary>
    private static (int ExitCode, string Output) RunProcess(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var proc = Process.Start(psi);
        if (proc == null) return (-1, "");
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stdout + (string.IsNullOrEmpty(stderr) ? "" : "\n" + stderr));
    }
}
