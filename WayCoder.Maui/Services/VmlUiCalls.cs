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

    /// <summary>活着的定时器：id → (Timer, 用户标记, 间隔毫秒)。间隔要留着，模态弹框暂停后靠它恢复。</summary>
    private readonly ConcurrentDictionary<int, (System.Threading.Timer Timer, int Tag, int Interval)> _timers = new();
    private int _nextTimerId = 1;

    /// <summary>模态弹框期间置位：此时新建的定时器直接以「暂停」状态建出来。</summary>
    private volatile bool _timersPaused;

    /// <summary>
    /// **模态弹框期间把定时器全部停掉**，弹完按原间隔恢复。
    ///
    /// <para>
    /// 为什么非做不可：`ui_dlg_msg` / `ui_dlg_select` / `ui_dlg_multi` / `ui_dlg_input`
    /// 都是**阻塞**的（在 VM 线程上同步等用户回答），而 `ui_timer_set` 是**重复**定时器 ——
    /// 弹框挂多久，队列里就积压多少条 `Timer` 消息。程序一恢复就把这些积压**瞬间抽干**：
    /// </para>
    /// <list type="bullet">
    ///   <item>打地鼠：每局开始时 `left = 90` 刚重置，几十条积压把倒计时一次抽到 0
    ///     ⇒ 新一局立刻又「时间到」，**弹框再也关不掉**（用户实测报的「时间太短、打不着」）。</item>
    ///   <item>接方块 / 俄罗斯方块：积压的每一拍都推进一步物理 ⇒ 球/方块瞬移出界、
    ///     立刻又结束 —— 同一类症状。</item>
    /// </list>
    /// <para>
    /// 语义上也更对：程序在弹框期间**根本没在跑**，它的时钟本来就不该走。
    /// 放在宿主这一处，20 份例程都不必各自打补丁（那种修法漏一个就是同一个 bug 再来一次）。
    /// </para>
    /// </summary>
    private T WithTimersPaused<T>(Func<T> body)
    {
        SetTimersPaused(true);
        try { return body(); }
        finally { SetTimersPaused(false); }
    }

    private void SetTimersPaused(bool paused)
    {
        _timersPaused = paused;
        foreach (var kv in _timers)
        {
            var (timer, _, interval) = kv.Value;
            try
            {
                if (paused) timer.Change(Timeout.Infinite, Timeout.Infinite);
                else timer.Change(interval, interval);
            }
            catch (ObjectDisposedException) { /* 弹框期间程序自己 kill 掉了 —— 正常 */ }
        }
    }

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
        _timersPaused = false;
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
                // 四个弹框都是**阻塞**的 —— 期间要把定时器停掉，否则积压的 Timer 消息
                // 会在程序恢复时被瞬间抽干（详见 WithTimersPaused 的说明）
                case VmlUi.DlgMsg: registers[0] = WithTimersPaused(() => DlgMsg(registers, memory)); break;
                case VmlUi.DlgSelect: registers[0] = WithTimersPaused(() => DlgSelect(registers, memory)); break;
                case VmlUi.DlgMulti: registers[0] = WithTimersPaused(() => DlgMulti(registers, memory)); break;
                case VmlUi.DlgInput: registers[0] = WithTimersPaused(() => DlgInput(registers, memory)); break;

                case VmlUi.WinOpen: registers[0] = WinOpen(registers, memory, ex: false); break;
                case VmlUi.WinOpenEx: registers[0] = WinOpen(registers, memory, ex: true); break;
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

                case VmlUi.MsgPoll: registers[0] = Poll(registers, memory, ex: false); break;
                case VmlUi.MsgWait: registers[0] = Wait(registers, memory, ex: false); break;
                case VmlUi.MsgPollEx: registers[0] = Poll(registers, memory, ex: true); break;
                case VmlUi.MsgWaitEx: registers[0] = Wait(registers, memory, ex: true); break;
                case VmlUi.MsgCount: registers[0] = _queue.Count; break;
                // 清空待处理消息 → 丢弃条数。程序在"重新开始/切关"时调用，防上一局的残留输入
                // 被新一局读出来（一次点击常有多条：按下/抬起/移动）。
                case VmlUi.MsgClear: _queue.Clear(); registers[0] = 0; break;
                case VmlUi.TimerSet: registers[0] = TimerSet(registers); break;
                case VmlUi.TimerKill: registers[0] = TimerKill(registers); break;
                case VmlUi.WinClosed: registers[0] = _windowClosed ? 1 : 0; break;
                case VmlUi.CallJson: registers[0] = CallJson(registers, memory); break;
                case VmlUi.ScrW: registers[0] = ScrArea().Width; break;
                case VmlUi.ScrH: registers[0] = ScrArea().Height; break;
                case VmlUi.ScrOrient: registers[0] = ScreenOrientation(); break;

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
        // ⚠ **实测值要先问一句"它还算不算数"**：`MeasuredViewport` 只在绘图页活着时更新，
        // 而"在竖屏里打完一局 → 退出 → 转到横屏 → 再开一局"这条路上，转屏期间没有绘图页在跑，
        // 它还留着竖屏的 411×525 —— 横屏那一局照它开窗，画面就只剩中间一条
        // （实测：`orient=LANDSCAPE` 但 `wh=TALL`，`scene=411x525` 塞进 396×301 的画布）。
        // 方向对不上就不用它，退回 `AvailableArea`（那边**已经是分方向**算的）。
        if (MeasuredViewport is { } vp && vp.Width > 0 && vp.Height > 0
            && VmlUi.ViewportMatchesOrientation(vp.Width, vp.Height, ScreenOrientation()))
            return vp;
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

    /// <summary>宿主侧的 JSON 函数只注册一次（进程内）。</summary>
    private static bool _jsonHandlersReady;

    /// <summary>
    /// 注册走 <see cref="VmlUi.CallJson"/> 的那些函数。
    ///
    /// **加一个能力 = 这里一行** —— 这正是这个"全能接口"存在的理由：
    /// 不占 syscall 号、不用碰 C 包装、不用重生成 22 种语言的绑定。
    /// 只放**不要求性能**、也不是每帧都发生的东西；绘图/输入仍旧走专用号。
    /// </summary>
    private static void EnsureJsonHandlers()
    {
        if (_jsonHandlersReady) return;
        _jsonHandlersReady = true;

        // `echo`：参数原样返回。**管线自检 + 程序自己的调试口** ——
        // 能把"参数到底有没有原样传进 VM"和"结果有没有写回缓冲区"一次问清楚。
        VmlJsonApi.Register("echo", args => args ?? JNode.Null());

        // `version`：让程序知道自己在哪个 App / 哪个版本上跑（写兼容分支时有用）。
        VmlJsonApi.Register("version", _ => JNode.Object()
            .Set("app", Global.AppName)
            .Set("cn", Global.AppNameCN)
            .Set("version", Global.Version)
            .Set("platform", DeviceInfo.Current.Platform.ToString().ToLowerInvariant()));

        // `screen`：与 `SCR_W`/`SCR_H`/`SCR_ORIENT` **同源**（就调那几个函数），
        // 免得出现"JSON 里报的尺寸和 syscall 报的不一样"这种最难查的分叉。
        VmlJsonApi.Register("screen", _ =>
        {
            var area = ScrArea();
            var orient = ScreenOrientation();
            return JNode.Object()
                .Set("w", area.Width)
                .Set("h", area.Height)
                .Set("orientation", orient)
                .Set("landscape", orient == VmlUi.Landscape);
        });
    }

    /// <summary>
    /// `CALLJSON`（#573）：函数名 + 参数 JSON → 结果 JSON 写进调用方的缓冲区。
    ///
    /// 返回**写入的字节数**（不含结尾 NUL）；-1 = 失败（函数不认识 / 参数非法 / 缓冲区放不下）。
    /// ⚠ 缓冲区放不下时**回一个说明原因的短信封**（能放下的话）—— 让程序看得见"为什么没结果"，
    /// 而不是拿到一段被截断的、解析不出来的 JSON。
    /// </summary>
    private int CallJson(int[] r, byte[] mem)
    {
        EnsureJsonHandlers();

        var fn = Str(mem, r[0]);
        // R1 允许是 0（没传参数）—— 空指针读出来就是空串，实现那侧按"没参数"处理
        var argsJson = r[1] > 0 ? Str(mem, r[1]) : "";
        var json = VmlJsonApi.Invoke(fn, argsJson);

        var n = WriteString(mem, r[2], r[3], json);
        if (n >= 0) return n;

        // 装不下 ⇒ **回一个说明原因的短信封**（能放下的话），别让程序拿到一段被截断的、
        // 解析不出来的 JSON 还以为是"程序自己写坏了"。两层都放不下才返回 -1。
        var needed = System.Text.Encoding.UTF8.GetByteCount(json);
        return WriteString(mem, r[2], r[3], VmlJsonApi.TooLongEnvelope(needed));
    }

    /// <summary>
    /// 把字符串按 UTF-8 写进 VM 内存的缓冲区，返回写入字节数（不含结尾 NUL）。
    /// 放不下就改回一个说明原因的信封再试一次，仍放不下则返回 -1。
    /// </summary>
    private static int WriteString(byte[] mem, int dst, int cap, string text)
    {
        if (dst < 0 || cap <= 1 || dst + cap > mem.Length) return -1;

        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        var n = Math.Min(bytes.Length, cap - 1);          // 留一个字节给结尾 NUL
        Array.Copy(bytes, 0, mem, dst, n);
        mem[dst + n] = 0;
        return bytes.Length <= cap - 1 ? n : -1;
    }

    /// <summary>
    /// 屏幕方向（<see cref="VmlUi.Portrait"/> 竖屏 / <see cref="VmlUi.Landscape"/> 横屏）。
    ///
    /// **优先问设备**（`DeviceDisplay`），取不到才退回按实测视口推 ——
    /// 方向是"机器横着还是竖着拿"，不该被宿主怎么排版影响（手柄收起会改画布形状，
    /// 但机器并不会因此翻个身）。退回那一条在真机上不会走到，只是异常设备的兜底。
    /// 判定规则只有 <see cref="VmlUi.OrientationOf"/> 一处实现。
    ///
    /// ⚠ **这一处是唯一真源**：`SCR_ORIENT`（#569）查询走它，
    /// 方向变化时发的 <see cref="VmlMsgType.WindowOrient"/> 消息也走它 ——
    /// 两处各算一次的话，"查到的"和"收到的"会在某个边界上不一致，那是最难查的一类。
    /// </summary>
    internal static int ScreenOrientation()
    {
        try
        {
            var info = DeviceDisplay.MainDisplayInfo;
            if (info.Width > 0 && info.Height > 0) return VmlUi.OrientationOf(info.Width, info.Height);
        }
        catch { /* 取不到就往下退 */ }
        if (MeasuredViewport is { } vp && vp.Width > 0 && vp.Height > 0)
            return VmlUi.OrientationOf(vp.Width, vp.Height);
        return VmlUi.Portrait;
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

    /// <summary>
    /// 开窗口。<paramref name="ex"/> = 走的是 <see cref="VmlUi.WinOpenEx"/>（多两个声明参数）。
    ///
    /// ⚠ **两个号分成两条路读，不能合并成"从 r[3]/r[4] 里取默认值"**：只传 3 个参数的老程序，
    /// r[3]/r[4] 里是**它自己上一句留下的值**（可能是个指针、也可能是个计数），宿主无从判断
    /// 那是不是"真给的"。老号就按老语义（可旋转=1、要手柄=1 = 今天的行为）走。
    /// </summary>
    private int WinOpen(int[] r, byte[] mem, bool ex)
    {
        var scene = new VmlScene
        {
            Title = Str(mem, r[0]) is { Length: > 0 } t ? t : "VML",
            Width = r[1] > 0 ? r[1] : 320,
            Height = r[2] > 0 ? r[2] : 240,
            // 老号（#520）一个字的声明都没有 ⇒ Legacy（跟随旋转但**不动坐标系**，= 老行为）。
            // 新号 R3 三档：0=只竖屏 / 1=支持旋转 / 2=只横屏；**其余值一律当"支持旋转"**
            // （宽容：将来加档位时老宿主至少不会把它当成"锁死"而卡住程序）。
            // R4=0 才是"不要手柄"，非 0 一律当要。
            Rotation = !ex ? WindowRotation.Legacy : r[3] switch
            {
                VmlUi.PortraitOnly => WindowRotation.PortraitOnly,
                VmlUi.LandscapeOnly => WindowRotation.LandscapeOnly,
                _ => WindowRotation.Follow,
            },
            NeedGamepad = !ex || r[4] != VmlUi.NoGamepad,
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
        // 程序**自己**关窗也要把「窗口已关」置位。
        // 否则 `ui_win_closed()` 仍报 0 ⇒ 那套「主循环靠 `while (ui_win_closed() == 0)` 退出」的
        // 游戏在程序主动关窗后**出不来**（用户实测报的「很难退出游戏」）。
        // 有了它，「对话框里选『否/拒绝』→ `ui_win_close()` → 主循环自然退出」成立，
        // 各语言例程就不必自己再加一个退出标志位。
        // 幂等：正常的收尾路径本来就会再关一次，第二次直接返回。
        if (_windowClosed) return 0;
        _windowClosed = true;
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

    /// <summary>
    /// 取当前绘图窗口**最新呈现帧**的 DSL（即程序调 <c>ui_present</c> 时拍的快照）；
    /// 没开过窗、或程序从没调过 <c>ui_present</c> 时返回 null。
    ///
    /// 存在的理由：**让 AI 看得见自己写的图形程序**。<c>vml</c> 工具原先只回控制台文本，
    /// 而游戏是画出来的 —— 没有画面，AI 只能靠猜，而「程序没崩」根本不等于「画对了」。
    /// 有出口之后 <c>VmlTool</c> 就能把它渲染成 PNG 交给 <c>view_image</c>。
    ///
    /// 用 <see cref="VmlScene.PresentedDsl"/> 而**不是** <c>BuildDsl()</c>：后者是"当前图元"的
    /// 实时拼装，可能拍到画到一半的场景（道理同 <c>VmlScene.Present</c> 的注释），
    /// 而前者是程序自己声明"这一帧画完了"的那份。
    /// </summary>
    public string? TryGetPresentedDsl() => Scene()?.PresentedDsl;

    private void TouchScene()
    {
        if (_scene is { } s) OnSceneChanged?.Invoke(s);
    }

    // ── 输入 ──────────────────────────────────────────────────

    /// <summary>
    /// 读一条消息（非阻塞）。<paramref name="ex"/> = 走 <see cref="VmlUi.MsgPollEx"/>：
    /// 多一个 R1=保留位（<see cref="VmlUi.Keep"/> 时**只看队头、不取走**）。
    ///
    /// ⚠ 老号只读 R0 —— 不把两个号合成"从 r[1] 取默认值"的理由与 `WinOpen` 同一处：
    /// 只传 R0 的老程序，r[1] 里是它自己上一句留下的值。
    /// </summary>
    private int Poll(int[] r, byte[] mem, bool ex)
    {
        var msg = ex ? _queue.TryRead(r[1] == VmlUi.Keep) : _queue.TryTake();
        if (msg is not { } m) return 0;
        m.WriteTo(mem, r[0]);
        return (int)m.Type;
    }

    /// <summary>
    /// 读一条消息（阻塞）。<paramref name="ex"/> = 走 <see cref="VmlUi.MsgWaitEx"/>：
    /// 多一个 R2=保留位。理由同 <see cref="Poll"/>。
    ///
    /// ⚠ 保留模式**必须阻塞等待**吗？不必 —— 队头那条一直在，`TryRead(keep)` 立刻就能返回。
    /// 换句话说保留模式下这个"阻塞"只在**队列空**时才起作用（等的还是"来第一条"）。
    /// </summary>
    private int Wait(int[] r, byte[] mem, bool ex)
    {
        var msg = ex ? _queue.Read(r[1], r[2] == VmlUi.Keep) : _queue.Take(r[1]);
        if (msg is not { } m) return 0;
        m.WriteTo(mem, r[0]);
        return (int)m.Type;
    }

    private int TimerSet(int[] r)
    {
        var interval = Math.Clamp(r[0], 1, 3_600_000);
        var tag = r[1];
        var id = _nextTimerId++;
        // 正处于模态弹框期间（程序在弹框里又装了个定时器）⇒ 同样以暂停状态建出来，
        // 免得它成为下一个"积压源"
        var paused = _timersPaused;
        var timer = new System.Threading.Timer(
            _ => _queue.Post(new VmlMessage(VmlMsgType.Timer, id, tag, Environment.TickCount)),
            null,
            paused ? Timeout.Infinite : interval,
            paused ? Timeout.Infinite : interval);
        _timers[id] = (timer, tag, interval);
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

        // ⚠ **参数顺序按 C 头文件读，别按"看起来像"读**（v0.96.182 修）：
        //   `ui_polygon (pts, count, fill色, stroke色, width, grad)`
        //   `ui_polyline(pts, count, stroke色, width, grad)`
        //   原先把 r[2] 当"填充开关"、r[3] 当颜色 —— 而 C 侧两个都是**颜色**（与 `ui_path` 的
        //   `stroke`/`fill` 同一套口径）。后果是 `ui_polygon(pts,3,绿,0,0,"")` 取到颜色 0
        //   （全透明）⇒ 多边形**什么都不画**；polyline 更离谱：把"线宽"当成了颜色。
        //   真机图元体检抓到的（桌面映射自测看不出来：DSL 与几何全是对的）。
        if (close)
        {
            var fillColor = (uint)r[2];
            var strokeColor = (uint)r[3];
            var grad = GradientIdOrNull(r, 5, mem);
            // 只填不描的常见写法（stroke 传 0）也要能画：颜色取"给了的那个"
            Scene()?.AddPolygon(pts, fillColor != 0 ? fillColor : strokeColor,
                filled: fillColor != 0 || grad != null, r[4], grad);
        }
        else
        {
            Scene()?.AddPolyline(pts, (uint)r[2], r[3], GradientIdOrNull(r, 4, mem));
        }
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
