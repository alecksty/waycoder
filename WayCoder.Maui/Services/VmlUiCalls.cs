using System.Collections.Concurrent;
using VMLRuntime;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;

namespace WayCoder.Maui.Services;

/// <summary>
/// 手机端 VML 的**宿主 syscall 处理器** —— 把 <see cref="VmlUi"/> 那段号（500–599）翻成真实 UI。
///
/// ## 为什么走 <see cref="ISystemCallHandler"/> 而不是改运行时
/// 运行时的 dispatch 是「先问宿主处理器，再走内置 switch」（<c>VMLRuntime.Syscall.cs:23</c>），
/// 所以宿主可以完全拥有一个号段；再配合「宿主把号段加进 <c>SyscallConstants.UserAllowed</c>」
/// 过掉 mcu 模式那道白名单门（<see cref="EnsureReservedSyscallsAllowed"/>），
/// **`third_party/vml` 一行都不用改** —— 这很重要：那个目录是从用户的 VML 仓库 sync 来的，
/// 本地改动会在下次同步时被冲掉。
///
/// ## 线程模型（唯一需要小心的地方）
/// VM 跑在**后台线程**（<c>VmlTool</c> 是 Exclusive，见 MauiVml），UI 只能在主线程动。
/// 因此：
///   · 弹窗这类**要等用户**的调用 —— 在 VM 线程上同步等 <c>IWebInteraction</c> 的 Task
///     （桥内部自己 <c>MainThread.InvokeOnMainThreadAsync</c>，不会死锁，见 MauiWebInteraction 注释）；
///   · 开窗口/更新场景 —— 主线程投递，不阻塞 VM（保留模式，VM 只管往场景里追加图元）；
///   · 输入 —— VM 线程在 <see cref="VmlMessageQueue.Take"/> 上阻塞，UI 线程负责投递消息。
/// 与本仓库既有的「VM 线程阻塞等宿主输入」模型完全一致（<c>CaptureIo.ReadLine</c>）。
/// </summary>
internal sealed class VmlUiCalls : ISystemCallHandler
{
    /// <summary>当前场景（保留模式）。每次 <see cref="VmlUi.WinOpen"/> 新建一份。</summary>
    private VmlScene? _scene;

    /// <summary>输入消息队列 —— 页面手势/键盘/定时器投递，程序经 syscall 取走。</summary>
    private readonly VmlMessageQueue _queue = new();

    /// <summary>活着的定时器：id → (Timer, 用户标记)。</summary>
    private readonly ConcurrentDictionary<int, (System.Threading.Timer Timer, int Tag)> _timers = new();
    private int _nextTimerId = 1;

    /// <summary>用户是否已关闭窗口（点返回箭头）。</summary>
    private volatile bool _windowClosed;

    /// <summary>最近一次运行用的处理器实例 —— 绘图页靠它把用户的触摸/按键投回队列。</summary>
    internal static VmlUiCalls? Current;

    /// <summary>当前场景变化时通知绘图页重绘。</summary>
    internal static Action<VmlScene>? OnSceneChanged;

    /// <summary>请求打开绘图页（由宿主在启动时注入，避免本类直接依赖 Shell 导航）。</summary>
    internal static Func<VmlScene, Task>? OpenWindowAsync;

    /// <summary>请求关闭绘图页。</summary>
    internal static Func<Task>? CloseWindowAsync;

    /// <summary>一次 VML 运行开始的清理：清队列、关掉残留定时器、丢弃上一轮的场景。</summary>
    internal void Reset()
    {
        Current = this; // 绘图页据此把输入投回本实例的队列
        _queue.Clear();
        foreach (var kv in _timers) kv.Value.Timer.Dispose();
        _timers.Clear();
        _nextTimerId = 1;
        _windowClosed = false;
        _scene = null;
    }

    /// <summary>页面把输入事件投进来（UI 线程调用）。</summary>
    internal void PostInput(VmlMsgType type, int a = 0, int b = 0)
        => _queue.Post(new VmlMessage(type, a, b, Environment.TickCount));

    /// <summary>页面被用户关闭时调用（返回箭头）。</summary>
    internal void MarkWindowClosed()
    {
        _windowClosed = true;
        _queue.Post(new VmlMessage(VmlMsgType.WindowClose, 0, 0, Environment.TickCount));
    }

    /// <summary>
    /// 把保留号段加进运行时的用户态白名单。
    /// **漏了这一步的现象是「处理器注册了却永远不被调用」** —— 因为 mcu 模式下
    /// dispatch 顶部那道 `UserAllowedSyscalls.Contains` 会先把未知号拒掉，根本走不到处理器。
    /// <c>SyscallConstants.UserAllowed</c> 是公开的可变集合，且与运行时内部那份是**同一对象引用**
    /// （<c>VMLRuntime.cs:179</c>），所以在这里加一次，之后所有 VmRuntime 实例都放行。
    /// 用 <see cref="HashSet{T}.Add"/> 的重复添加是幂等的，多跑几次也无害。
    /// </summary>
    internal static void EnsureReservedSyscallsAllowed()
    {
        foreach (var n in VmlUi.ReservedRange()) SyscallConstants.UserAllowed.Add(n);
    }

    public bool HandleSyscall(int syscallNumber, int[] registers, byte[] memory, ref int pc)
    {
        if (!VmlUi.Handles(syscallNumber)) return false; // 不认识必须放行，否则吞掉内置 syscall

        // 入参诊断（临时脚手架，定位完就撤）：真机实测「对话框字符串大多是空的、只有一个 hello」
        // 时，唯一能分清"程序没把指针放进寄存器"还是"我读错了内存"的办法就是把
        // **寄存器原值与按它读出来的字符串**一起记下来。只记这几类（每次运行每号最多 3 条），
        // 不记每一条绘制调用 —— 一帧可能有上千条，会把日志刷爆。
        LogCalls(syscallNumber, registers, memory);

        try
        {
            switch (syscallNumber)
            {
                case VmlUi.DlgMsg: registers[0] = DlgMsg(registers, memory); break;
                case VmlUi.DlgSelect: registers[0] = DlgSelect(registers, memory); break;
                case VmlUi.DlgMulti: registers[0] = DlgMulti(registers, memory); break;
                case VmlUi.DlgInput: registers[0] = DlgInput(registers, memory); break;

                case VmlUi.WinOpen: registers[0] = WinOpen(registers, memory); break;
                case VmlUi.WinClose: registers[0] = WinClose(); break;
                case VmlUi.DrawClear: Scene()?.Clear((uint)registers[0]); TouchScene(); break;
                case VmlUi.DrawPixel: Scene()?.AddPixel(registers[0], registers[1], (uint)registers[2]); TouchScene(); break;
                case VmlUi.DrawLine: Scene()?.AddLine(registers[0], registers[1], registers[2], registers[3], (uint)registers[4], registers[5]); TouchScene(); break;
                case VmlUi.DrawRect: Scene()?.AddRect(registers[0], registers[1], registers[2], registers[3], (uint)registers[4], registers[5] != 0, registers[6], registers[7]); TouchScene(); break;
                case VmlUi.DrawCircle: Scene()?.AddCircle(registers[0], registers[1], registers[2], (uint)registers[3], registers[4] != 0, registers[5]); TouchScene(); break;
                case VmlUi.DrawEllipse: Scene()?.AddEllipse(registers[0], registers[1], registers[2], registers[3], (uint)registers[4], registers[5] != 0, registers[6]); TouchScene(); break;
                // 一次性文字：R6=样式位（粗/斜）。**R6 是后加的**，老程序不传就是 0=常规，
                // 所以加它不破坏既有调用（寄存器默认 0）。
                case VmlUi.DrawText: Scene()?.AddText(registers[0], registers[1], Str(memory, registers[2]), (uint)registers[3], registers[4], registers[5], registers[6]); TouchScene(); break;
                case VmlUi.SetFont: SetFont(registers); break;
                case VmlUi.Text: Scene()?.AddTextCurrent(registers[0], registers[1], Str(memory, registers[2])); TouchScene(); break;
                case VmlUi.DrawIcon: Scene()?.AddIcon(registers[0], registers[1], Str(memory, registers[2]), registers[3], (uint)registers[4]); TouchScene(); break;
                case VmlUi.DrawImage: Scene()?.AddImage(registers[0], registers[1], Str(memory, registers[2]), registers[3], registers[4]); TouchScene(); break;
                case VmlUi.DrawPresent: TouchScene(); break;

                case VmlUi.MsgPoll: registers[0] = Poll(registers, memory); break;
                case VmlUi.MsgWait: registers[0] = Wait(registers, memory); break;
                case VmlUi.MsgCount: registers[0] = _queue.Count; break;
                case VmlUi.TimerSet: registers[0] = TimerSet(registers); break;
                case VmlUi.TimerKill: registers[0] = TimerKill(registers); break;
                case VmlUi.WinClosed: registers[0] = _windowClosed ? 1 : 0; break;
                case VmlUi.ScrW: registers[0] = ScrArea().Width; break;
                case VmlUi.ScrH: registers[0] = ScrArea().Height; break;

                default: return false; // 号段内但未实现 → 交回运行时（保持"不认领"语义）
            }
        }
        catch (Exception ex)
        {
            // 宿主 UI 出错不能把 VM 打挂：记错日志并回一个失败码，程序自己能看见
            ErrorLog.Error("VmlUi", $"syscall {syscallNumber} 失败", ex);
            registers[0] = -1;
        }
        return true;
    }

    /// <summary>每个系统调用号最多记几条诊断（防刷屏）。</summary>
    private readonly Dictionary<int, int> _logged = new();

    /// <summary>
    /// 入参诊断：把**寄存器原值**与**按它读出来的字符串**一起落一条日志。
    /// 这两样缺一不可 —— 只记寄存器看不出"指针指向的内容对不对"，只记字符串则分不清
    /// "指针没传进来"和"内存读错了"。写到 <c>config/logs/error_YYYYMMDD.log</c>（adb 可直接读）。
    /// </summary>
    private void LogCalls(int n, int[] r, byte[] memory)
    {
        if (n is not (VmlUi.DlgMsg or VmlUi.DlgSelect or VmlUi.DlgMulti or VmlUi.DlgInput
            or VmlUi.WinOpen or VmlUi.DrawText or VmlUi.DrawIcon or VmlUi.DrawImage)) return;

        var seen = _logged.TryGetValue(n, out var c) ? c : 0;
        if (seen >= 3) return;
        _logged[n] = seen + 1;

        var regs = string.Join(",", r.Take(8));
        var reads = string.Join(" | ", new[] { 0, 1, 2 }.Select(i =>
            $"R{i}={r[i]}→'{Str(memory, r[i])}'"));
        ErrorLog.Info("VmlUi", $"#{n} regs=[{regs}] {reads} memLen={memory.Length}");
    }

    /// <summary>
    /// **实测的绘图视口**（dp）—— 由 <c>DrawWindowPage</c> 量到自己的画布区之后写进来。
    ///
    /// 为什么要实测而不是按屏幕算：`AvailableArea` 只能扣一个**固定**的 chrome 高度，
    /// 而真实占用（标题栏 + 手柄区 + 页面内边距）随设备与排版变。实测（1080×2400 模拟器）：
    /// 按屏幕算出来 744dp，而画布实际只有 578dp —— 于是程序按 744 排版，**底部一百多 dp
    /// 的内容（状态文字、计分板）全落在可视区外**，看着就像"没画出来"。
    ///
    /// 自纠正：第一次运行还没有实测值、先用估算；页面一量到就把真实值记下来，
    /// 之后每次运行都按真实值排版（因此**第二次运行起就完全贴合**）。
    /// </summary>
    internal static (int Width, int Height)? MeasuredViewport;

    /// <summary>
    /// 可用绘图区（绘图单位 = dp）。**优先用实测视口**，没有才退回按屏幕估算 ——
    /// 估算的扣减规则只有 <see cref="VmlUi.AvailableArea"/> 一处实现，这里不许再算一份
    /// （"同一规则两处实现"是本仓库的头号坑）。
    /// </summary>
    private static (int Width, int Height) ScrArea()
    {
        if (MeasuredViewport is { } vp && vp.Width > 0 && vp.Height > 0) return vp;
        try
        {
            var info = DeviceDisplay.MainDisplayInfo;
            return VmlUi.AvailableArea(info.Width, info.Height, info.Density);
        }
        catch
        {
            // 取不到显示信息时给一个保守的手机尺寸，而不是抛 —— 程序至少还能跑
            return (320, 480);
        }
    }

    // ── 对话框 ────────────────────────────────────────────────

    /// <summary>消息框。返回 0=确定/是，1=否/取消。样式只影响标题前缀（原生弹框都是一种形态）。</summary>
    private static int DlgMsg(int[] r, byte[] mem)
    {
        var title = Str(mem, r[0]);
        var body = Str(mem, r[1]);
        var prefix = r[2] switch { 1 => "⚠️ ", 2 => "⛔ ", 3 => "❓ ", _ => "ℹ️ " };
        var bridge = UxHelper.WebInteraction;
        if (bridge == null) return 0;
        // 询问样式走「是/否」，其余走「确定」—— 用同一个确认框，就不再新造一个只有 OK 的原生弹框
        var code = bridge.ConfirmAsync(prefix + title, body, allowAll: false, timeoutMs: 0)
            .GetAwaiter().GetResult();
        return code == 2 ? 1 : 0;
    }

    private static int DlgSelect(int[] r, byte[] mem)
    {
        var options = StrBlock(mem, r[2], r[3]);
        if (options.Count == 0) return -1;
        var bridge = UxHelper.WebInteraction;
        if (bridge == null) return -1;
        var picked = bridge.SelectAsync(Str(mem, r[0]), options, timeoutMs: 0)
            .GetAwaiter().GetResult();
        // 原生桥回的是 label（可能重复），取**第一个**匹配的下标 —— 与选项表顺序一致
        return picked == null ? -1 : options.IndexOf(picked);
    }

    private static int DlgMulti(int[] r, byte[] mem)
    {
        var options = StrBlock(mem, r[2], r[3]);
        if (options.Count == 0) return -1;
        var bridge = UxHelper.WebInteraction;
        if (bridge == null) return -1;
        var picked = bridge.MultiSelectAsync(Str(mem, r[0]), options, timeoutMs: 0)
            .GetAwaiter().GetResult();
        if (picked == null) return -1;
        var mask = 0;
        foreach (var p in picked)
        {
            var idx = options.IndexOf(p);
            if (idx >= 0 && idx < 31) mask |= 1 << idx;
        }
        return mask;
    }

    private static int DlgInput(int[] r, byte[] mem)
    {
        var bridge = UxHelper.WebInteraction;
        if (bridge == null) return -1;
        var text = bridge.AskAsync(Str(mem, r[1]), null, timeoutMs: 0).GetAwaiter().GetResult();
        if (text == null) return -1;

        var capacity = r[3];
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        var n = Math.Min(bytes.Length, Math.Max(0, capacity - 1)); // 留一个字节给结尾 \0
        var dst = r[2];
        if (dst >= 0 && dst + n + 1 <= mem.Length)
        {
            Array.Copy(bytes, 0, mem, dst, n);
            mem[dst + n] = 0;
        }
        return n;
    }

    // ── 窗体与绘图 ────────────────────────────────────────────

    private int WinOpen(int[] r, byte[] mem)
    {
        var scene = new VmlScene
        {
            Title = Str(mem, r[0]) is { Length: > 0 } t ? t : "VML",
            Width = r[1] > 0 ? r[1] : 320,
            Height = r[2] > 0 ? r[2] : 240,
        };
        _scene = scene;
        _windowClosed = false;

        // 开页面必须回主线程；VM 线程在这里等页面真正显示出来再继续，
        // 否则程序可能已经画完并退出、页面才姗姗来迟（用户什么都看不到）
        var open = OpenWindowAsync;
        if (open == null) return -1;
        MainThread.InvokeOnMainThreadAsync(() => open(scene)).GetAwaiter().GetResult();
        return 1;
    }

    private int WinClose()
    {
        var close = CloseWindowAsync;
        if (close != null) MainThread.InvokeOnMainThreadAsync(close).GetAwaiter().GetResult();
        _scene = null;
        return 0;
    }

    /// <summary>
    /// 设置当前文字属性（字号 / 样式位 / 颜色 / 锚点）——`SET_FONT` 号段。
    /// 属性存在**场景对象**上（每次开窗重置），程序不必自己维护这几个变量。
    /// 字号钳到 [6, 200]：传 0 或负数会让排版算出零/负行高，后面整段文字都画不出来。
    /// </summary>
    private void SetFont(int[] r)
    {
        var s = Scene();
        if (s == null) return;
        s.FontSize = Math.Clamp(r[0], 6, 200);
        s.FontStyle = r[1];
        s.FontColor = (uint)r[2];
        s.FontAnchor = r[3];
    }

    private VmlScene? Scene() => _scene;

    private void TouchScene()
    {
        if (_scene is { } s) OnSceneChanged?.Invoke(s);
    }

    // ── 输入 ──────────────────────────────────────────────────

    private int Poll(int[] r, byte[] mem)
    {
        var msg = _queue.TryTake();
        if (msg is not { } m) return 0;
        m.WriteTo(mem, r[0]);
        return (int)m.Type;
    }

    private int Wait(int[] r, byte[] mem)
    {
        var msg = _queue.Take(r[1]);
        if (msg is not { } m) return 0;
        m.WriteTo(mem, r[0]);
        return (int)m.Type;
    }

    private int TimerSet(int[] r)
    {
        var interval = Math.Clamp(r[0], 1, 3_600_000);
        var tag = r[1];
        var id = _nextTimerId++;
        var timer = new System.Threading.Timer(
            _ => _queue.Post(new VmlMessage(VmlMsgType.Timer, id, tag, Environment.TickCount)),
            null, interval, interval);
        _timers[id] = (timer, tag);
        return id;
    }

    private int TimerKill(int[] r)
    {
        if (_timers.TryRemove(r[0], out var t)) t.Timer.Dispose();
        return 0;
    }

    // ── 内存读取 ──────────────────────────────────────────────

    /// <summary>读 VML 内存里的 NUL 结尾字符串（UTF-8）。越界/非法一律返回空串，不抛。</summary>
    internal static string Str(byte[] memory, int address)
    {
        if (address < 0 || address >= memory.Length) return "";
        var end = address;
        while (end < memory.Length && memory[end] != 0) end++;
        return System.Text.Encoding.UTF8.GetString(memory, address, end - address);
    }

    /// <summary>读「选项块」：<paramref name="count"/> 个 \0 分隔的字符串。</summary>
    internal static List<string> StrBlock(byte[] memory, int address, int count)
    {
        var list = new List<string>();
        if (count <= 0 || address < 0 || address >= memory.Length) return list;
        var at = address;
        for (var i = 0; i < count && at < memory.Length; i++)
        {
            var s = Str(memory, at);
            list.Add(s);
            at += System.Text.Encoding.UTF8.GetByteCount(s) + 1;
        }
        return list;
    }
}
