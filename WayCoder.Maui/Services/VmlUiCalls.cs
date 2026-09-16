using System.Collections.Concurrent;
using VMLRuntime;
using WayCoder.Tools;          // CwdContext：BGM 路径要与文件工具用同一把尺子解析
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
        // **VM 内置的 PC 喇叭蜂鸣（#57）：截住它，接到真实音频。**
        //
        // 运行时的 #57 本来就有，但实现是交给 `VmSpeakerDevice` —— 那个设备只把样本记进内存 /
        // 写 WAV 给测试用，**不发出任何声音**，手机上跑就是"调了没反应"。
        // 与其新增一个平行的 `AUDIO_TONE`，不如把这条已经存在、所有前端都认的接口接通
        //（理由详见 VmlUi.VmSpeakerBeep 的注释）。
        //
        // ⚠ **只截这一个号** —— 下面那行 `Handles()` 仍然只认 500–599，别顺手把它放宽。
        if (syscallNumber == VmlUi.VmSpeakerBeep)
        {
            // 波形固定方波：PC 喇叭本来就是方波，音色也正是"8 位机音效"那个味道。
            VmlAudio.Tone(registers[0], registers[1], wave: 1);
            registers[0] = 0;
            return true;
        }

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
                // 「这一帧画完了」——**不是**普通的一次内容变化：窗口靠它决定什么时候出图，
                // 见 VmlScene.PresentVersion。原先这里只是 TouchScene()（= 当作"变了"），
                // 于是定时器会把画到一半的场景贴上去（棋盘一闪一闪就是它）。
                case VmlUi.DrawPresent: Scene()?.Present(); TouchScene(); break;

                // ── 手感：音效 / 震动 ──
                case VmlUi.AudioPlay: registers[0] = AudioPlay(registers, memory); break;
                case VmlUi.AudioStop: VmlAudio.StopBgm(); registers[0] = 0; break;
                case VmlUi.AudioVolume: VmlAudio.SetVolume(registers[0]); registers[0] = 0; break;
                case VmlUi.Vibrate: registers[0] = Vibrate(registers); break;
                case VmlUi.VibratePattern: registers[0] = VibratePattern(registers, memory); break;

                // ── 绘图增强（534–539）──
                case VmlUi.Gradient: Gradient(registers, memory); registers[0] = 0; break;
                case VmlUi.DrawPath: DrawPath(registers, memory); registers[0] = 0; break;
                case VmlUi.DrawPolygon: DrawPolyline(registers, memory, close: true); registers[0] = 0; break;
                case VmlUi.DrawPolyline: DrawPolyline(registers, memory, close: false); registers[0] = 0; break;
                case VmlUi.DrawRectGrad:
                {
                    // 没有渐变 id 就什么都不画 —— 这个号的全部意义就是"用渐变填充"，
                    // 没有渐变时退化成"画一个黑色矩形"只会让人以为渐变没生效。
                    var g = GradientIdOrNull(registers, 4, memory);
                    if (g != null)
                        Scene()?.AddRect(registers[0], registers[1], registers[2], registers[3],
                            0, filled: true, width: 0, radius: Math.Max(0, registers[5]), fillGradient: g);
                    TouchScene(); break;
                }
                case VmlUi.DrawCircleGrad:
                {
                    var g = GradientIdOrNull(registers, 3, memory);
                    if (g != null) Scene()?.AddCircle(registers[0], registers[1], registers[2], 0, true, 0, g);
                    TouchScene(); break;
                }

                // ── 持久化与常亮 ──
                case VmlUi.StoreSet: registers[0] = StoreSet(registers, memory); break;
                case VmlUi.StoreGet: registers[0] = StoreGet(registers, memory); break;
                case VmlUi.StoreDel: registers[0] = StoreDel(registers, memory); break;
                case VmlUi.ScreenKeepOn: ScreenKeepOn(registers[0] != 0); registers[0] = 0; break;

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

    // ── 手感：音效 / 震动 ──────────────────────────────────────

    /// <summary>
    /// BGM：路径**沙箱相对 → 绝对**（`CwdContext` 与文件工具同一把尺子），
    /// 文件不存在直接回 -1 —— 让程序自己看得见"没播成"，而不是静默没声音。
    /// </summary>
    private static int AudioPlay(int[] r, byte[] mem)
    {
        var rel = Str(mem, r[0]);
        if (rel.Length == 0) return -1;
        var full = CwdContext.Resolve(rel);
        if (!File.Exists(full)) return -1;
        return VmlAudio.Play(full, r[1] != 0) == null ? 0 : -1;
    }

    /// <summary>震动一下：时长先钳到上限（负数/超长都拦掉），强度交给平台层。</summary>
    private static int Vibrate(int[] r)
        => VmlAudio.Vibrate(Math.Clamp(r[0], 1, VmlUi.VibrateMaxSegmentMs), Math.Clamp(r[1], 0, 255)) ? 0 : -1;

    /// <summary>
    /// 按节奏震动：把内存里的 int 数组读出来 → 协议层钳段数/段长 → 交给平台层。
    /// **读内存要防越界**：地址与段数都是程序给的，越界就地停（宁可少振几段，不要读坏内存）。
    /// </summary>
    private static int VibratePattern(int[] r, byte[] mem)
    {
        var count = Math.Clamp(r[1], 0, VmlUi.VibrateMaxSegments);
        if (count <= 0) return -1;

        var raw = new List<int>(count);
        for (var i = 0; i < count; i++)
        {
            var at = r[0] + i * 4;
            if (at < 0 || at + 4 > mem.Length) break;
            raw.Add(BitConverter.ToInt32(mem, at));
        }

        var pattern = VmlUi.ClampVibratePattern(raw);
        return pattern.Length > 0 && VmlAudio.VibratePattern(pattern) ? 0 : -1;
    }

    // ── 绘图增强（v0.96.176）──────────────────────────────────
    //
    // 这六个号只是把**本来就在绘图 DSL 里**的能力接到 VML 侧：曲线展平、渐变采样、
    // 多边形填充在 Infra 那层早就有了（桌面 draw 工具一直在用），此前只是没有 syscall 入口。
    // 所以这里的方法都很薄 —— 真正干活的是 `VmlScene` 的 `AddXxx` 与 `Infra/DrawPath.cs`。

    /// <summary>
    /// 渐变刷子：把 id 与几何交给场景，之后形状用 `fillGradient` 按 id 引用。
    /// **同名覆盖**（重定义同一个 id 就是改它），与 DSL 里 `gradient` 指令的语义一致。
    /// </summary>
    private void Gradient(int[] r, byte[] mem)
    {
        var id = VmlUi.SafeId(Str(mem, r[0]));
        if (id.Length == 0) return;
        // 几何原样传寄存器：**千分之一的换算在 VmlScene.AddGradient 一处做**，
        // 别在这边再除一次（两端各换算一次就是"沉默的错"，见那边的注释）。
        Scene()?.AddGradient(id, radial: r[1] != 0, (uint)r[2], (uint)r[3], r[4], r[5], r[6], r[7]);
        TouchScene();
    }

    /// <summary>路径：d 字符串 + 描边/填充/渐变/线帽/虚线。曲线展平在 `Infra/DrawPath.cs`。</summary>
    private void DrawPath(int[] r, byte[] mem)
    {
        var d = Str(mem, r[0]);
        if (d.Length == 0) return;
        var grad = GradientIdOrNull(r, 4, mem);
        Scene()?.AddPath(d, (uint)r[1], r[2],
            cap: r[5],
            fillColor: (uint)r[3], fillSet: r[3] != 0,
            fillGradient: grad,
            dashed: r[6] != 0);
        TouchScene();
    }

    /// <summary>多边形 / 折线：点数组是内存里的 **int32 的 x,y 对**。</summary>
    private void DrawPolyline(int[] r, byte[] mem, bool close)
    {
        var count = r[1];
        if (count < 2 || count > VmlUi.MaxPolyPoints) return;

        // 读内存要防越界：地址与点数都是程序给的，越界就地停（宁可少画几个点，不要读坏内存）
        var pts = new List<double>(count * 2);
        for (var i = 0; i < count; i++)
        {
            var at = r[0] + i * 8;
            if (at < 0 || at + 8 > mem.Length) break;
            pts.Add(BitConverter.ToInt32(mem, at));
            pts.Add(BitConverter.ToInt32(mem, at + 4));
        }
        if (pts.Count < 4) return;

        var grad = GradientIdOrNull(r, 5, mem);
        if (close) Scene()?.AddPolygon(pts, (uint)r[3], filled: r[2] != 0 || grad != null, r[4], grad);
        else Scene()?.AddPolyline(pts, (uint)r[3], r[4], grad);
        TouchScene();
    }

    /// <summary>渐变 id 指针 → 清洗后的 id；指针为 0（或读出来是空串）返回 null（= 用纯色填充）。</summary>
    internal static string? GradientIdOrNull(int[] r, int reg, byte[] mem)
    {
        if (r[reg] == 0) return null;
        var id = VmlUi.SafeId(Str(mem, r[reg]));
        return id.Length == 0 ? null : id;
    }

    // ── 持久化与常亮 ──────────────────────────────────────────
    //
    // ⚠ 这一组里**没有**"取时间"与"随机数"——VM 内置已有（`#53`/`#54` 与 `#50`），
    // 另立接口就是同一件事两处实现。

    /// <summary>
    /// 「别熄屏」**必须回主线程**。
    ///
    /// 真机实测：`DeviceDisplay.KeepScreenOn` 在 Android 上最终调的是
    /// `Window.AddFlags(FLAG_KEEP_SCREEN_ON)` —— 那是**动 View 层级**，从 VM 线程调直接抛
    /// `RuntimeException: Only the original thread that created a view hierarchy can touch its views`。
    ///
    /// ⚠ 宿主处理器**所有**分支都得按这条尺子过一遍：只碰数据的（Preferences / Vibrator / AudioTrack）
    /// 可以在 VM 线程上直接调，**凡是碰 View 的一律要 marshal**。
    /// 用 `BeginInvokeOnMainThread` 而不是 `InvokeOnMainThread`：前者不阻塞，
    /// 免得把 VM 线程挂在一次 UI 派发上。
    /// </summary>
    private static void ScreenKeepOn(bool on)
        => MainThread.BeginInvokeOnMainThread(() =>
        {
            try { DeviceDisplay.KeepScreenOn = on; }
            catch (Exception ex) { ErrorLog.Error("VmlUi", "设置常亮失败", ex); }
        });

    private static int StoreSet(int[] r, byte[] mem)
    {
        var key = VmlUi.StoreKey(Str(mem, r[0]));
        if (key == null) return -1;
        Preferences.Default.Set(key, Str(mem, r[1]));
        return 0;
    }

    private static int StoreGet(int[] r, byte[] mem)
    {
        var key = VmlUi.StoreKey(Str(mem, r[0]));
        if (key == null) return -1;
        if (!Preferences.Default.ContainsKey(key)) return -1;

        var value = Preferences.Default.Get(key, "");
        var capacity = Math.Max(0, r[2]);
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        var n = Math.Min(bytes.Length, Math.Max(0, capacity - 1));   // 留一个字节给结尾 \0
        if (r[1] >= 0 && r[1] + n + 1 <= mem.Length)
        {
            Array.Copy(bytes, 0, mem, r[1], n);
            mem[r[1] + n] = 0;
        }
        return n;
    }

    private static int StoreDel(int[] r, byte[] mem)
    {
        var key = VmlUi.StoreKey(Str(mem, r[0]));
        if (key == null) return -1;
        Preferences.Default.Remove(key);
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
