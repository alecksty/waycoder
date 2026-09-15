using System.Text;

namespace WayCoder.Maui;

/// <summary>
/// WinUI 侧未处理异常落盘（对标 TUI 的 <c>ErrorLog</c> catch-all —— 那边一直有，这边此前没有）。
///
/// 为什么必须有：WinUI 会把 UI 线程上逃逸的托管异常包成 **stowed exception**
/// （事件日志里是 <c>0xc000027b</c> / 故障模块 Microsoft.UI.Xaml.dll）然后直接终止进程 ——
/// 应用自己的 catch-all 够不着；WER 里那条 <c>0x80004001</c>（E_NOTIMPL，故障模块 combase）
/// 只是「有异常逃出了原生回调」的通用包装码，看不出真凶；崩溃转储里异常也早已展开完
/// （<c>clrstack -all</c> 只剩主线程的 Application.Start）。不在这里拦就什么都留不下。
///
/// 落盘位置：<c>%LOCALAPPDATA%\WayCoder.Maui.Diag\diag.log</c>。
/// 首抛异常（更啰嗦、能定位到「异常从哪一行抛的」）要 <c>WAYCODER_MAUI_DIAG=1</c> 才开。
/// </summary>
internal static class DiagLog
{
    private static readonly object Gate = new();
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WayCoder.Maui.Diag");
    private static readonly string LogPath = Path.Combine(Dir, "diag.log");
    private static readonly Dictionary<string, int> Counters = new();
    private static int _lines;
    private const int MaxLines = 3000;   // 面包屑会刷屏（绘制循环），到顶就停

    public static string FilePath => LogPath;

    private static void Append(string tag, string body)
    {
        try
        {
            lock (Gate)
            {
                if (_lines++ > MaxLines) return;
                Directory.CreateDirectory(Dir);
                File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] [{tag}] {body}\n");
            }
        }
        catch { /* 诊断代码绝不反过来把进程搞挂 */ }
    }

    public static void Write(string tag, string text) => Append(tag, text);

    public static void Write(string tag, Exception? ex)
    {
        if (ex == null) { Append(tag, "(null)"); return; }
        // 异常信息也可能刷屏，截断栈
        var st = ex.StackTrace ?? "";
        if (st.Length > 3000) st = st[..3000] + "\n…（截断）";
        Append(tag, $"{ex.GetType().FullName}: {ex.Message}\n{st}");
    }

    /// <summary>每 <paramref name="every"/> 次调用记一条 —— 用来判「是不是死循环」：
    /// 日志里 Draw/OnSizeAllocated 的计数飞快上涨就是无限重绘/重排。</summary>
    public static void Tick(string tag, int every = 100)
    {
        int n;
        lock (Gate)
        {
            Counters.TryGetValue(tag, out n);
            Counters[tag] = ++n;
        }
        if (n % every == 0) Append(tag, $"第 {n} 次");
    }

    public static void Hook()
    {
        Append("boot", $"=== 启动 pid={Environment.ProcessId} ===");

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Write("‼ AppDomain.UnhandledException", e.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (_, e) =>
            Write("‼ TaskScheduler.UnobservedTaskException", e.Exception);

        // 首抛异常：**默认关闭**，`WAYCODER_MAUI_DIAG=1` 才开。
        // 它很有用（崩溃前最后几条首抛记录通常就是真凶 —— 被 WinUI 吞掉后转成 stowed exception），
        // 但每条异常都要算一次调用栈，正常人用不着，别让它常驻在热路径上。
        if (Environment.GetEnvironmentVariable("WAYCODER_MAUI_DIAG") != "1") return;

        Write("diag", "首抛异常记录已开启（WAYCODER_MAUI_DIAG=1）");
        AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
        {
            try
            {
                // 只记栈里出现我们自己命名空间的（WinUI/MAUI 内部本来就抛一堆正常异常）
                var st = e.Exception.StackTrace;
                if (string.IsNullOrEmpty(st)) return;
                if (!st.Contains("WayCoder.Maui", StringComparison.Ordinal)) return;
                Write("FirstChance", e.Exception);
            }
            catch { }
        };
    }
}
