using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.TUI.Base;

namespace WayCoder;

/// <summary>
/// Windows 鼠标 VT 字节流实机探针 —— 验证「开启 ENABLE_VIRTUAL_TERMINAL_INPUT 后，
/// .NET 的 Console.OpenStandardInput() 能读到鼠标字节流」这一核心假设，并诊断终端实际发的序列格式。
///
/// 用途：Windows/终端鼠标失效时定位「字节流是否到达 + 终端发的是 SGR 还是 X10」。
/// 跑法（Debug 构建）：waycoder --mouse-probe
///
/// 之后在终端里移动/点击/滚动鼠标（Esc 或 Ctrl+C 退出，窗口 15s）。探针用真实的 WindowsCharSource
/// 读 stdin 字节流，检测 SGR（\x1b[&lt;...M/m）与 X10（\x1b[M + 3 字节）两种鼠标序列并给出解析结果。
///
/// 可移植性：WinConsoleMode.Enable 在非 Windows no-op，探针代码跨平台可编译运行，只在 Debug
/// （WAYCODER_TEST）构建下注册为 --mouse-probe，不影响 Release/生产。
/// </summary>
public static class TuiMouseProbe
{
    public static int Run()
    {
        Console.WriteLine("WayCoder 鼠标 VT 字节流探针（终端实机验证）");
        Console.WriteLine("=================================================");
        Console.WriteLine($"平台: {(OperatingSystem.IsWindows() ? "Windows" : OperatingSystem.IsMacOS() ? "macOS" : "Linux")}");
        Console.WriteLine($"stdin 重定向: {Console.IsInputRedirected}");
        Console.WriteLine("先发基本鼠标启用序列 + 开 VT 输入…");

        Tty.EnableMouse();
        var vtEnabled = WinConsoleMode.Enable();
        Console.WriteLine($"WinConsoleMode.Enable: {(vtEnabled ? "成功（VT 输入已开）" : "跳过/失败（非 Windows 或 stdin 重定向）")}");
        Console.WriteLine();
        Console.WriteLine("请在此终端移动 / 点击 / 滚动鼠标（Esc 或 Ctrl+C 退出，窗口 15s）…");

        Console.CancelKeyPress += (_, e) => { e.Cancel = true; };

        try
        {
            using var src = new WindowsCharSource();
            var acc = new StringBuilder();          // 累积已读字节，供跨序列检测
            var sgrCount = 0;
            var x10Count = 0;
            var start = Environment.TickCount64;

            while (Environment.TickCount64 - start < 15_000)
            {
                if (!src.HasInput) { Thread.Sleep(15); continue; }
                if (!src.TryReadKey(out var key)) { Thread.Sleep(15); continue; }
                acc.Append(key.KeyChar);
                var s = acc.ToString();

                // SGR：\x1b[<Cb;Cx;CyM/m —— 从累积里提取完整序列（到 M/m 为止）
                var sgrIdx = s.IndexOf("\x1b[<");
                if (sgrIdx >= 0)
                {
                    var tail = s[(sgrIdx + 3)..];
                    var mPos = tail.IndexOf('M');
                    var mPosL = tail.IndexOf('m');
                    var end = mPos >= 0 ? (mPosL >= 0 ? Math.Min(mPos, mPosL) : mPos) : mPosL;
                    if (end >= 0)
                    {
                        sgrCount++;
                        var seqBody = tail[..end];
                        var ev = InputManager.ParseSgrMouse(seqBody);
                        Console.WriteLine();
                        Console.WriteLine($"\n✅ SGR 鼠标序列: body=\"{seqBody}\" 解析: type={ev?.Type} x={ev?.MouseX} y={ev?.MouseY} left={ev?.MouseLeft} scrollUp={ev?.MouseScrollUp} release={ev?.MouseRelease}");
                        acc.Clear();
                    }
                }
                // X10：\x1b[M + 3 字节（旧终端，未开 1006h 时）
                else if (s.Contains("\x1b[M"))
                {
                    var idx = s.IndexOf("\x1b[M");
                    var tail = s[(idx + 3)..];
                    if (tail.Length >= 3)
                    {
                        x10Count++;
                        Console.WriteLine();
                        Console.WriteLine($"\n✅ X10 鼠标序列: cb={(int)tail[0] - 32} x={(int)tail[1] - 32} y={(int)tail[2] - 32}（X10 无法区分哪键 release）");
                        acc.Clear();
                    }
                }
                else if (s.Length > 64)
                {
                    acc.Clear(); // 防累积无限增长
                }
            }

            Console.WriteLine();
            if (sgrCount > 0)
                Console.WriteLine($"\n▶ 共捕获 {sgrCount} 个 SGR 鼠标事件 —— 字节流可达、SGR 解析正常。");
            else if (x10Count > 0)
                Console.WriteLine($"\n▶ 共捕获 {x10Count} 个 X10 鼠标事件（非 SGR）—— 终端未认 ?1006h；需在 AnsiTty 强制 1006h 或按 X10 解析。");
            else
                Console.WriteLine("\n⚠ 未捕获到鼠标序列。若上方有 \\x1B 或 RFC 字节显示，说明有字节流但未匹配 SGR/X10 —— 请补全诊断。");
        }
        finally
        {
            Tty.DisableMouse();
            WinConsoleMode.Disable();
        }

        return 0;
    }
}
