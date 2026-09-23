using System.Collections.Concurrent;
using System.Text;
using WayCoder.Infra;   // JNode / VmlJsonApi（syscall 结果的 JSON 信封）

namespace WayCoder.UI.Shared;

/// <summary>
/// VML 宿主（500–599 号段）**与平台打交道的那几条缝**。
///
/// ## 为什么要有这一层
///
/// 这套 syscall 的**逻辑**（坐标钳位、字符串读取、消息队列语义、定时器暂停、刷子/样式状态机…）
/// 与**平台**（弹不弹框、画到哪个窗口、声音从哪出）是两件事。原先把它们写在同一个类里
/// （`WayCoder.Maui/Services/VmlUiCalls.cs`，1042 行），于是**桌面端只能重抄一遍或者空着** ——
/// 而 `scripts/vmlcli` 选了空着（`NullUiCalls`），结果是"手机能跑的游戏在桌面上什么也证明不了"，
/// 每改一行都得打 APK + 上模拟器（一次一分多钟）。
///
/// 现在分成两半：
///   · **本文件这一族**（`VmlHostRuntime`）= 全部逻辑，两端**编同一份**；
///   · <see cref="IVmlHost"/> = 只有平台真的不一样的那十几件事，两端各给一个实现。
///
/// ⚠ **判据是"这件事两个平台的做法是不是同一件事"**，不是"它看起来像不像 UI"：
///   手机与桌面**都要**弹对话框、都要发声、都要读写存档 —— 那些进接口；
///   而"把 `registers[0..7]` 翻成一条 `rect` DSL 行"两端逐字相同 —— 那留在共享层。
///   分错的代价是本仓库的头号坑：**同一规则两处实现**，症状是"手机上对、桌面上错"。
///
/// ## 实现的约定（两端都必须守）
///
/// · **不许抛**：VM 线程上的异常会把整台虚拟机带走，而程序那边只看到"窗口没了"。
///   共享层已经包了 try/catch 兜底（记日志 + 回 -1），但接口实现自己也该尽力而为。
/// · **阻塞是允许的**：`Dlg*` 四个方法在 VM 线程上**同步等用户**（手机端本来就是这样，
///   `UxHelper.WebInteraction` 内部自己 marshal 回主线程）。桌面端用脚本化的答案立即返回。
/// · **碰 View 的要自己 marshal 回主线程**（手机端实测：`DeviceDisplay.KeepScreenOn` 最终动的是
///   `Window.AddFlags`，从 VM 线程调直接抛 `Only the original thread that created a view
///   hierarchy can touch its views`）。只碰数据的（Preferences / Vibrator / AudioTrack）可以直接调。
/// </summary>
public interface IVmlHost
{
    // ── 屏幕 ────────────────────────────────────────────────────────────────

    /// <summary>可用绘图区（单位 = dp）—— `SCR_W`(#566) / `SCR_H`(#567) 报的就是它。</summary>
    (int Width, int Height) ScreenArea();

    /// <summary>屏幕方向 —— 见 <see cref="VmlUi.Portrait"/> / <see cref="VmlUi.Landscape"/>。
    /// 判定规则只有 <see cref="VmlUi.OrientationOf"/> 一处实现，两端都调它。</summary>
    int Orientation();

    // ── 窗口与绘图 ──────────────────────────────────────────────────────────

    /// <summary>开绘图窗口。返回 false = 这一端没有绘图能力（老桌面 CLI 的行为）。</summary>
    bool OpenWindow(VmlScene scene);

    /// <summary>关绘图窗口（用户按返回箭头 / 程序调 `ui_win_close`）。</summary>
    void CloseWindow();

    /// <summary>场景内容变了（每一条绘制调用都会走到这里）——宿主据此决定要不要重绘/重编码。</summary>
    void SceneChanged(VmlScene scene);

    // ── 对话框（**阻塞**：在 VM 线程上等答案）────────────────────────────────

    /// <summary>消息框。<paramref name="style"/> 0 信息 / 1 警告 / 2 错误 / 3 询问。
    /// 返回 0 = 确定/是，1 = 否/取消。</summary>
    int DlgMsg(string title, string body, int style);

    /// <summary>单选 → 选中下标；取消 -1。</summary>
    int DlgSelect(string title, string prompt, IReadOnlyList<string> options);

    /// <summary>多选 → 选中位掩码；取消 -1。</summary>
    int DlgMulti(string title, string prompt, IReadOnlyList<string> options);

    /// <summary>文本输入 → 用户输入；取消返回 null。</summary>
    string? DlgInput(string title, string prompt);

    // ── 手感：音效 / 震动 ───────────────────────────────────────────────────

    /// <summary>现场合成的音效（也是 VM 内置 `#57` 喇叭蜂鸣的落点）。
    /// <paramref name="hz"/>/<paramref name="ms"/> 已由共享层钳过范围。</summary>
    void Tone(int hz, int ms, int wave, int volume);

    /// <summary>播放 BGM 文件（<paramref name="fullPath"/> 已由 <see cref="ResolvePath"/> 解析成绝对路径）。
    /// 返回 false = 这一端放不了（含"文件不存在"，由共享层先判）。</summary>
    bool PlayAudio(string fullPath, bool loop);

    /// <summary>停掉 BGM。</summary>
    void StopAudio();

    /// <summary>设置整体音量（已钳到 0–100）。</summary>
    void SetAudioVolume(int volume);

    /// <summary>震动一下。</summary>
    bool Vibrate(int ms, int amplitude);

    /// <summary>按节奏震动（已由 <see cref="VmlUi.ClampVibratePattern"/> 钳过）。</summary>
    bool VibratePattern(long[] pattern);

    // ── 持久化与设备 ────────────────────────────────────────────────────────

    /// <summary>读一条键值；没有这个键返回 null。键已由 <see cref="VmlUi.StoreKey"/> 加过前缀并清洗。</summary>
    string? StoreGet(string key);

    /// <summary>写一条键值。</summary>
    void StoreSet(string key, string value);

    /// <summary>删一条键值。</summary>
    void StoreDel(string key);

    /// <summary>玩游戏时别熄屏。</summary>
    void KeepScreenOn(bool on);

    // ── 像素读回（583–585：floodfill / getimage / putimage）──────────────────
    //
    // 场景是**保留模式**的（只有图元、没有像素缓冲），所以"这个像素是什么颜色"
    // 只能靠**当场光栅化**一次来回答。光栅器两端都有
    // （`DrawRunner.Rasterize`，桌面与手机编的是同一份 `Infra/`），
    // 但**放在宿主接口上**而不是共享层直接调：渲染是宿主的职责
    //（`IVmlHost.OpenWindow(VmlScene)` 已经是这个分工）。

    /// <summary>
    /// 把当前场景的一块区域光栅化成像素，写进 <paramref name="dest"/>
    /// （**RGBA 行优先**，长度须为 w*h*4）。返回 false = 这一端没有光栅化能力。
    /// </summary>
    bool Rasterize(int x, int y, int w, int h, byte[] dest);

    /// <summary>
    /// 把一块像素存成宿主的一个**临时图片文件**，返回路径（供场景的 `image` 图元引用）。
    /// 失败返回 null。
    ///
    /// <para>
    /// ⚠ **为什么要经过文件**：场景与宿主之间的契约是**一段 DSL 文本**
    /// （`ui_image` 走的也是 `image x y "路径" w h`）。把几万像素塞进文本不现实，
    /// 而复用现成的 image 图元连渲染器都不用改。代价是每次 `putimage` 编一次 PNG
    /// —— 精灵动画（每帧两次）会实打实吃到这笔开销。
    /// </para>
    /// </summary>
    string? SaveTempImage(int w, int h, byte[] rgba);

    // ── 杂项 ────────────────────────────────────────────────────────────────

    /// <summary>把程序给的相对路径解析成绝对路径（沙箱规则各端不同：
    /// 手机是 app 的 workspace，桌面是源文件所在目录）。</summary>
    string ResolvePath(string relative);

    /// <summary>
    /// 注册**这一端特有**的 `CALLJSON` 函数（`version` / `sysinfo` 之类）。
    ///
    /// 共享层已经注册了语义必须逐字一致的那些（`echo` / `screen`），本方法在其后调用。
    /// </summary>
    void RegisterJsonHandlers();

    /// <summary>宿主日志出口（两端各接到自己的日志设施上；实现必须容忍高频调用）。</summary>
    void Log(string message);
}

/// <summary>
/// VML 宿主 syscall（500–599 号段 + VM 内置的 `#57` 喇叭）的**唯一实现** —— 手机与桌面共用这一份。
///
/// ## 与运行时的关系
///
/// 运行时的 dispatch 是「**先问宿主处理器，再走内置 switch**」（`VMLRuntime.Syscall.cs:23`），
/// 所以宿主可以完全拥有一个号段；再配合「把号段加进 `SyscallConstants.UserAllowed`」过掉
/// mcu 模式那道白名单门（<see cref="EnsureReservedSyscallsAllowed"/>），
/// **`third_party/vml` 一行都不用改**。⚠ 漏了后一步的现象是
/// 「处理器明明挂上了却永远不被调用」—— 因为 dispatch 顶部先把未知号拒掉了。
///
/// ## 线程模型（唯一需要小心的地方）
///
/// VM 跑在**后台线程**，UI 只能在主线程动。因此：
///   · 弹窗这类**要等用户**的调用 —— 在 VM 线程上同步等 <see cref="IVmlHost"/> 的返回
///     （平台实现自己负责 marshal，见接口注释）；
///   · 开窗口 / 更新场景 —— 不阻塞 VM（保留模式：VM 只管往场景里追加图元）；
///   · 输入 —— VM 线程在 <see cref="VmlMessageQueue.Read"/> 上阻塞，
///     UI 线程（或桌面端的脚本投递线程）负责 <see cref="Post"/>/<see cref="PostInput"/>。
/// </summary>
public sealed class VmlHostRuntime
{
    private readonly IVmlHost _host;

    public VmlHostRuntime(IVmlHost host)
    {
        _host = host;
        EnsureJsonHandlers();
        _host.RegisterJsonHandlers();
    }

    // ══════════════════════════════════════════════════════════════════════
    // 状态
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>当前场景（保留模式）。每次 <see cref="VmlUi.WinOpen"/> 新建一份。</summary>
    private VmlScene? _scene;

    /// <summary>输入消息队列 —— 平台的指针/键盘事件与定时器投递，程序经 syscall 取走。</summary>
    private readonly VmlMessageQueue _queue = new();

    /// <summary>活着的定时器：id → (Timer, 用户标记, 间隔毫秒)。间隔要留着，模态弹框暂停后靠它恢复。</summary>
    private readonly ConcurrentDictionary<int, (System.Threading.Timer Timer, int Tag, int Interval)> _timers = new();
    private int _nextTimerId = 1;

    /// <summary>模态弹框期间置位：此时新建的定时器直接以「暂停」状态建出来。</summary>
    private volatile bool _timersPaused;

    /// <summary>用户是否已关闭窗口（点返回箭头，或程序自己调了 `ui_win_close`）。</summary>
    private volatile bool _windowClosed;

    /// <summary>
    /// **有没有开过**绘图窗口。与 <see cref="_windowClosed"/> 一起构成
    /// <see cref="VmlUi.WinClosed"/> 的三值语义（0 开着 / 1 关掉了 / 2 从没开过）——
    /// `conio` 的 `getch()` 靠它区分"图形程序按键"与"控制台程序按行读 stdin"。
    /// </summary>
    private volatile bool _windowOpened;

    /// <summary>当前场景（供宿主查询，比如"程序画完了没有"）。没开窗时为 null。</summary>
    public VmlScene? Scene() => _scene;

    /// <summary>当前消息队列（平台把输入投进来）。</summary>
    public VmlMessageQueue Queue => _queue;

    /// <summary>
    /// 当前绘图窗口**最新呈现帧**的 DSL（即程序调 <c>ui_present</c> 时拍的快照）；
    /// 没开过窗、或程序从没调过 <c>ui_present</c> 时返回 null。
    ///
    /// 存在的理由：**让 AI 看得见自己写的图形程序**。<c>vml</c> 工具原先只回控制台文本，
    /// 而游戏是画出来的 —— 没有画面，AI 只能靠猜，而「程序没崩」根本不等于「画对了」。
    /// 有出口之后就能把它渲染成 PNG 交给 <c>view_image</c>。
    ///
    /// 用 <see cref="VmlScene.PresentedDsl"/> 而**不是** <c>BuildDsl()</c>：后者是"当前图元"的
    /// 实时拼装，可能拍到画到一半的场景（道理同 <see cref="VmlScene.Present"/> 的注释），
    /// 而前者是程序自己声明"这一帧画完了"的那份。
    /// </summary>
    public string? PresentedDsl => _scene?.PresentedDsl;

    /// <summary>
    /// 通用宿主调用口（577–580）里 `float8`/`long4`/`double4` 的参数**躺在别的寄存器组里**，
    /// 而 <see cref="HandleSyscall"/> 拿到的 `int[] registers` 只有 32 位通用整数寄存器
    /// （64 位值在其中只剩低半截镜像）。宿主把运行时那三组数组接进来（**活引用**，
    /// 写进去就是写进 VM），见 <see cref="VmlCallRegistry.TryHandle"/>。
    /// </summary>
    public float[]? FloatRegisters { get; set; }
    /// <summary>双精度寄存器组（`D0`–`D7`）。见 <see cref="FloatRegisters"/>。</summary>
    public double[]? DoubleRegisters { get; set; }
    /// <summary>长整数寄存器组（`L0`–`L7`）。见 <see cref="FloatRegisters"/>。</summary>
    public long[]? LongRegisters { get; set; }

    /// <summary>
    /// 「无限阻塞等待」的兜底时限（毫秒；0 = 不设，默认）。
    ///
    /// ## 为什么必须有它（实测踩到）
    ///
    /// VM 的 `TimeoutSeconds` 是在**指令循环**里查的，而 `ui_wait(msg, 0)`（0 = 无限等）
    /// 会让 VM 线程**阻塞在宿主 syscall 里** —— 超时根本没机会被评估。
    /// 于是"等一个永远不来的输入"的程序能把宿主**挂死**：实测 `--timeout 30` 的桌面 CLI
    /// 跑了 5 分钟还在那里（`Ctrl+C` 才出得来）。
    ///
    /// ⚠ 真机上这件事是**另一个机制**在兜：手机端有 ShellPage 的看门狗与返回键，
    ///   而且用户随时可能真的点一下。桌面脚手架两样都没有，必须自己兜住。
    ///
    /// 做法是把无限等切成**有时限的片**：等不到就返回 0（"这一拍没有消息"）交回 VM，
    /// 让指令循环重新转起来 —— 于是运行时的超时看门狗又能生效了
    /// （现象与跑 `draw_colors.c` 那种"用有限超时轮询"的程序完全一致）。
    /// **语义没有被改**：`ui_wait(msg, 0)` 仍然是"一直等到有消息为止"，
    /// 只是这个"一直"在桌面脚手架上有个上限。
    /// </summary>
    public int BlockingWaitLimitMs { get; set; }

    /// <summary>
    /// 每一条 syscall 进来时的观察钩子（**可选**，平台自己接）。
    ///
    /// 手机端有一个"入参诊断"脚手架（真机实测"对话框字符串大多是空的"时，唯一能分清
    /// "程序没把指针放进寄存器"还是"宿主读错了内存"的办法，就是把**寄存器原值与按它读出来的
    /// 字符串**一起记下来）。那种脚手架属于**某一端的调试设施**，不进共享层 —— 用这个钩子挂。
    /// </summary>
    public Action<int, int[], byte[]>? OnSyscall { get; set; }

    // ══════════════════════════════════════════════════════════════════════
    // 生命周期
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>一次 VML 运行开始的清理：清队列、关掉残留定时器、丢弃上一轮的场景。</summary>
    public void Reset()
    {
        _queue.Clear();
        _timersPaused = false;
        foreach (var kv in _timers) kv.Value.Timer.Dispose();
        _timers.Clear();
        _nextTimerId = 1;
        _windowClosed = false;
        _scene = null;
    }

    /// <summary>平台把输入事件投进来。</summary>
    public void PostInput(VmlMsgType type, int a = 0, int b = 0)
        => _queue.Post(new VmlMessage(type, a, b, Environment.TickCount));

    /// <summary>投递一条任意的消息（脚本化输入直接用它）。</summary>
    public void Post(VmlMessage msg) => _queue.Post(msg);

    /// <summary>
    /// 平台报告「用户把窗口关掉了」（返回箭头）。
    ///
    /// ⚠ 置位与投消息**两件事都要做**：只投消息的话，`ui_win_closed()`（它读的是
    /// <see cref="_windowClosed"/>）仍报 0，那套「主循环靠 `while (ui_win_closed() == 0)` 退出」
    /// 的游戏就出不来（用户实测报的「很难退出游戏」）。
    /// </summary>
    public void MarkWindowClosed()
    {
        _windowClosed = true;
        PostInput(VmlMsgType.WindowClose);
    }

    // ══════════════════════════════════════════════════════════════════════
    // syscall 入口
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 处理一条 syscall。**认领了返回 true，不是自己的返回 false**（宿主必须原样交回运行时，
    /// 否则会吞掉内置 syscall）。
    ///
    /// ⚠ **绝不抛**：任何异常都被接住、记一条日志、写回 -1。在宿主 syscall 处理器里抛出去
    /// 会把 VM 打挂，而程序那边只看到"窗口没了"。
    /// </summary>
    public bool HandleSyscall(int syscallNumber, int[] registers, byte[] memory)
    {
        // **VM 内置的 PC 喇叭蜂鸣（#57）：截住它，接到真实音频。**
        //
        // 运行时的 #57 本来就有，但实现交给 `VmSpeakerDevice` —— 那个设备只把样本记进内存 /
        // 写 WAV 给测试用，**不发出任何声音**（手机上跑就是"调了没反应"）。
        // 与其新增一个平行的 `AUDIO_TONE`，不如把这条已经存在、所有前端都认的接口接通。
        //
        // ⚠ **只截这一个号** —— 下面那行 `Handles()` 仍然只认 500–599，别顺手把它放宽：
        //   放宽会把别的内置 syscall 一并吞掉，那是最难查的一类故障。
        if (syscallNumber == VmlUi.VmSpeakerBeep)
        {
            // 波形固定方波：PC 喇叭本来就是方波，音色也正是"8 位机音效"那个味道。
            var (hz, ms, wave, vol) = VmlUi.ClampTone(registers[0], registers[1], wave: 1, volume: 100);
            _host.Tone(hz, ms, wave, vol);
            registers[0] = 0;
            return true;
        }

        if (!VmlUi.Handles(syscallNumber)) return false; // 不认识必须放行，否则吞掉内置 syscall

        OnSyscall?.Invoke(syscallNumber, registers, memory);

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
                case VmlUi.WinOpenPc: registers[0] = WinOpenPc(registers, memory); break;
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
                // 带**竖对齐**的文字：多一个第七参数（0顶/1中/2底）。老号 528 语义一字未动。
                // R6=竖对齐(0顶/1中/2底) R7=样式位(1粗 2斜) —— 与老号的 R6=样式**排布不同**，
                // 靠号区分（这正是走新号的原因：老程序 R6 里可能是任何东西）。
                case VmlUi.DrawTextEx: Scene()?.AddTextEx(registers[0], registers[1], Str(memory, registers[2]), (uint)registers[3], registers[4], registers[5], registers[6], registers[7]); TouchScene(); break;
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
                case VmlUi.AudioStop: _host.StopAudio(); registers[0] = 0; break;
                case VmlUi.AudioVolume: _host.SetAudioVolume(VmlUi.ClampVolume(registers[0])); registers[0] = 0; break;
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

                // ── 刷子 / 样式 / 一个号画所有形状（574–576）──
                //
                // 这一组是"一个号 + 操作码"：R0 是种类/槽位/形状码，后面几个寄存器
                // 是**通用槽**，由那张表各自解释。加新形状**不用再占号** —— 见
                // `VmlUi.DrawShape` 的注释（号是给库用的，不是给程序作者用的）。
                case VmlUi.Brush:
                    registers[0] = Brush(registers, memory); TouchScene(); break;
                case VmlUi.SetStyle:
                    registers[0] = Scene()?.SetStyle(registers[0], registers[1],
                        registers[2], registers[3], registers[4], registers[5]) == true ? 1 : 0;
                    TouchScene(); break;
                case VmlUi.DrawShape:
                    registers[0] = DrawShape(registers, memory) ? 1 : 0;
                    TouchScene(); break;

                // ── 持久化与常亮 ──
                case VmlUi.StoreSet: registers[0] = StoreSet(registers, memory); break;
                case VmlUi.StoreGet: registers[0] = StoreGet(registers, memory); break;
                case VmlUi.StoreDel: registers[0] = StoreDel(registers, memory); break;
                case VmlUi.ScreenKeepOn: _host.KeepScreenOn(registers[0] != 0); registers[0] = 0; break;

                // 像素读回（583–585）—— 详见各方法上的注释
                case VmlUi.FloodFill: registers[0] = DoFloodFill(registers); TouchScene(); break;
                case VmlUi.GetImage:  registers[0] = DoGetImage(registers); break;
                case VmlUi.PutImage:  registers[0] = DoPutImage(registers) ? 1 : 0; TouchScene(); break;

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
                case VmlUi.WinClosed: registers[0] = _windowClosed ? 1 : (_windowOpened ? 0 : 2); break;
                case VmlUi.CallJson: registers[0] = CallJson(registers, memory); break;

                // 通用宿主调用口（577–580）：**整批交给 UI/Shared 的注册表**
                //（与另一端同一个 TryHandle —— 号 → 种类、id → 实现、寄存器 ↔ 参数、
                //  失败码，这四件事各写一份就会分叉）。
                case VmlUi.CallWithInt8:
                case VmlUi.CallWithFloat8:
                case VmlUi.CallWithLong4:
                case VmlUi.CallWithDouble4:
                    DispatchCall(syscallNumber, registers); break;
                case VmlUi.ScrW: registers[0] = _host.ScreenArea().Width; break;
                case VmlUi.ScrH: registers[0] = _host.ScreenArea().Height; break;
                case VmlUi.ScrOrient: registers[0] = _host.Orientation(); break;

                default: return false; // 号段内但未实现 → 交回运行时（保持"不认领"语义）
            }
        }
        catch (Exception ex)
        {
            // 宿主 UI 出错不能把 VM 打挂：记错日志并回一个失败码，程序自己能看见
            _host.Log($"[VmlUi] syscall {syscallNumber} 失败：{ex.GetType().Name}: {ex.Message}");
            registers[0] = -1;
        }
        return true;
    }

    /// <summary>
    /// 四个**通用宿主调用口**（577–580）的分派。
    ///
    /// 逻辑全在 <see cref="VmlCallRegistry.TryHandle"/>（两端共用一份）；这里只负责
    /// **把 VM 的四组寄存器递进去** —— 通用整数用处理器参数里那份，浮点/长整数/双精度
    /// 由宿主从运行时上取（见 <see cref="FloatRegisters"/>）。
    ///
    /// 拿不到运行时时**回一个可读失败码 + 记日志**，不抛：正常路径上不会发生
    ///（宿主在跑之前就回填了），真发生了也必须是"这次调用失败"，而不是把 VM 打挂。
    /// </summary>
    private void DispatchCall(int syscallNumber, int[] registers)
    {
        if (FloatRegisters is null || DoubleRegisters is null || LongRegisters is null)
        {
            _host.Log($"[VmlCall] #{syscallNumber} 宿主没有接上 VmRuntime，读不到浮点/长整数寄存器组");
            if (registers.Length > 0) registers[0] = VmlCallRegistry.ErrorInternal;
            return;
        }
        VmlCallRegistry.TryHandle(syscallNumber, registers, FloatRegisters, DoubleRegisters, LongRegisters);
    }

    // ══════════════════════════════════════════════════════════════════════
    // 定时器
    // ══════════════════════════════════════════════════════════════════════

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
    ///     ⇒ 新一局立刻又「时间到」，**弹框再也关不掉**（真机实测报的「时间太短、打不着」）。</item>
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

    // ══════════════════════════════════════════════════════════════════════
    // 对话框
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>消息框。样式只影响标题前缀（各端都是同一个确认框，不另造只有 OK 的形态）。</summary>
    private int DlgMsg(int[] r, byte[] mem)
    {
        var prefix = r[2] switch { 1 => "⚠️ ", 2 => "⛔ ", 3 => "❓ ", _ => "ℹ️ " };
        return _host.DlgMsg(prefix + Str(mem, r[0]), Str(mem, r[1]), r[2]);
    }

    private int DlgSelect(int[] r, byte[] mem)
    {
        var options = StrBlock(mem, r[2], r[3]);
        if (options.Count == 0) return -1;
        return _host.DlgSelect(Str(mem, r[0]), Str(mem, r[1]), options);
    }

    private int DlgMulti(int[] r, byte[] mem)
    {
        var options = StrBlock(mem, r[2], r[3]);
        if (options.Count == 0) return -1;
        return _host.DlgMulti(Str(mem, r[0]), Str(mem, r[1]), options);
    }

    private int DlgInput(int[] r, byte[] mem)
    {
        var text = _host.DlgInput(Str(mem, r[0]), Str(mem, r[1]));
        if (text == null) return -1;

        var capacity = r[3];
        var bytes = Encoding.UTF8.GetBytes(text);
        var n = Math.Min(bytes.Length, Math.Max(0, capacity - 1)); // 留一个字节给结尾 \0
        var dst = r[2];
        if (dst >= 0 && dst + n + 1 <= mem.Length)
        {
            Array.Copy(bytes, 0, mem, dst, n);
            mem[dst + n] = 0;
        }
        return n;
    }

    // ══════════════════════════════════════════════════════════════════════
    // 窗体与绘图
    // ══════════════════════════════════════════════════════════════════════

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
        _windowOpened = true;
        _windowClosed = false;
        return _host.OpenWindow(scene) ? 1 : -1;
    }

    /// <summary>
    /// 开**电脑屏窗口**（`WIN_OPEN_PC` #582，第三种窗口）—— 给老程序用的。
    ///
    /// <para>
    /// 与 <see cref="WinOpen"/> 的差别只有三处，都在 <see cref="VmlScene"/> 上表达：
    /// **种类**（`PcScreen`）、**不要手柄**、**要屏幕键盘**。
    /// 页面的行为分支（固定坐标系 / 触摸只当鼠标 / 显示键盘）全按这几个字段走。
    /// </para>
    ///
    /// <para>
    /// ⚠ **`Rotation` 取不到 `Follow`**：电脑屏的坐标系**固定**为程序声明的 `w×h`，
    /// 老程序按那个分辨率排的版，换空间就会画到框外。所以 R3 在这里**只决定锁不锁方向**，
    /// "不锁"那一档落到 <see cref="WindowRotation.Legacy"/>（跟随旋转但**坐标系不动**，
    /// 宿主等比缩放着显示）—— 那正是我们要的语义，也是老窗口一直在用的那一档。
    /// </para>
    ///
    /// <para>
    /// ⚠ **参数分开读**（与 `#570` 各读各的）：这一批 R3/R4 的含义是
    /// "方向声明 / 要屏幕键盘"，而 `#570` 是"方向声明 / 要手柄" —— 位置对称、语义不同。
    /// </para>
    /// </summary>
    private int WinOpenPc(int[] r, byte[] mem)
    {
        var scene = new VmlScene
        {
            Title = Str(mem, r[0]) is { Length: > 0 } t ? t : "VML",
            // 兜底取 PC 上最眼熟的那一档（老程序不传尺寸时用）
            Width = r[1] > 0 ? r[1] : 640,
            Height = r[2] > 0 ? r[2] : 480,
            Kind = VmlWinKind.PcScreen,
            Rotation = r[3] switch
            {
                VmlUi.PortraitOnly => WindowRotation.PortraitOnly,
                VmlUi.LandscapeOnly => WindowRotation.LandscapeOnly,
                _ => WindowRotation.Legacy,     // 不锁：跟着转，但坐标系不动
            },
            NeedGamepad = false,                // 电脑屏的输入是键盘 + 鼠标，没有手柄区
            NeedKeyboard = r[4] != VmlUi.NoKeyboard,
        };
        _scene = scene;
        _windowOpened = true;
        _windowClosed = false;
        return _host.OpenWindow(scene) ? 1 : -1;
    }

    private int WinClose()
    {
        // 程序**自己**关窗也要把「窗口已关」置位。
        // 否则 `ui_win_closed()` 仍报 0 ⇒ 那套「主循环靠 `while (ui_win_closed() == 0)` 退出」的
        // 游戏在程序主动关窗后**出不来**（真机实测报的「很难退出游戏」）。
        // 有了它，「对话框里选『否/拒绝』→ `ui_win_close()` → 主循环自然退出」成立，
        // 各语言例程就不必自己再加一个退出标志位。
        // 幂等：正常的收尾路径本来就会再关一次，第二次直接返回。
        if (_windowClosed) return 0;
        _windowClosed = true;
        _host.CloseWindow();
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

    private void TouchScene() => _host.SceneChanged(_scene!);

    // ══════════════════════════════════════════════════════════════════════
    // 输入
    // ══════════════════════════════════════════════════════════════════════

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
        // 无限等要被切成人有上限的片（理由见 BlockingWaitLimitMs）——
        // 宿主没设上限时一个字不改，仍是"一直等"。
        var timeout = r[1];
        if (timeout <= 0 && BlockingWaitLimitMs > 0) timeout = BlockingWaitLimitMs;
        var msg = ex ? _queue.Read(timeout, r[2] == VmlUi.Keep) : _queue.Take(timeout);
        if (msg is not { } m) return 0;
        m.WriteTo(mem, r[0]);
        return (int)m.Type;
    }

    // ══════════════════════════════════════════════════════════════════════
    // CALLJSON
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>与宿主无关的那个 JSON 函数（`echo`）只注册一次（进程内）。</summary>
    private static bool _echoRegistered;

    /// <summary>
    /// 注册**语义必须逐字一致**的那几个 `CALLJSON` 函数。
    ///
    /// **加一个能力 = 这里一行**（或者宿主那侧的 <see cref="IVmlHost.RegisterJsonHandlers"/>）——
    /// 这正是这个"全能接口"存在的理由：不占 syscall 号、不用碰 C 包装、
    /// 不用重生成 22 种语言的绑定。只放**不要求性能**、也不是每帧都发生的东西。
    ///
    /// ⚠ `VmlJsonApi` 的注册表是**进程级**的（与手机端一致）—— 一个进程里只该有一个
    /// `VmlHostRuntime` 在跑。`echo` 与宿主无关，注册一次就够；`screen` 闭包在本实例上，
    /// **每次构造都重新注册**（否则第二个实例会被第一个的宿主答案串了）。
    /// </summary>
    private void EnsureJsonHandlers()
    {
        if (!_echoRegistered)
        {
            _echoRegistered = true;

            // `echo`：参数原样返回。**管线自检 + 程序自己的调试口** ——
            // 能把"参数到底有没有原样传进 VM"和"结果有没有写回缓冲区"一次问清楚。
            VmlJsonApi.Register("echo", args => args ?? JNode.Null());
        }

        // `text`：**文字量测**（BGI 的 `textwidth`/`textheight` 走它）。
        //
        // ⚠ **宽度必须与渲染同源** —— 这里用的是 `Terminal.AnsiString.CharWidth`，
        //   全仓唯一的宽度判据（半角 1 列、全角 2 列、零宽 0），列宽 = 字号/2，
        //   与移动端编辑器那套网格是同一份规则。
        //   在前端（`graphics.h`）里自己写一张 CJK 区间表就是"同一规则两处实现"，
        //   而老程序拿 `textwidth` 干的是**居中排版**
        //   （`outtextxy((getmaxx() - textwidth(s)) / 2, y, s)` 是标准写法）——
        //   差几个像素在屏幕上就是"看着不在中间"，而那种偏差最难归因。
        //
        // 参数：`{"s":"HELLO","size":16}` ⇒ `{"w":40,"h":16}`。`size` 缺省/非法取 16
        // （与前端 `_BGI_CHAR_BASE` 同值；字号非法时回一个能用的值，比回 0 好排查）。
        VmlJsonApi.Register("text", args =>
        {
            var s = args?.GetString("s") ?? "";
            var size = (int)(args?.GetNumber("size") ?? 0);
            if (size <= 0) size = 16;

            var cols = 0;
            foreach (var rune in s.EnumerateRunes()) cols += CharMetrics.Width(rune);
            return JNode.Object().Set("w", cols * size / 2).Set("h", size);
        });

        // `pixel`：**单个像素的颜色**（BGI 的 `getpixel` 走它）。
        //
        // 为什么不占一个 syscall 号：这是"加一个能力"，而 CALLJSON 就是为这种事建的
        // （见 `VmlJsonApi` 的类注释）—— 占号还要加 C 包装、重生成 22 份绑定。
        //
        // ⚠ 返回的是 **0xRRGGBB**，不是 BGI 的调色板索引 —— 场景里存的就是 RGB，
        //   而"RGB 反查索引"要靠**垫层那张 `_bgi_pal`**（真源在 `graphics.h`，
        //   这里再抄一张表就是本仓头号坑）。所以这一步分工是：
        //   **宿主给颜色，垫层给索引**。
        //
        // ⚠ 每次调用要**光栅化一次**（场景是保留模式的，没有像素缓冲，与
        //   `getimage` 同源）。所以它不适合放进每帧的密集循环 ——
        //   老程序里 `getpixel` 一般也就几次（碰撞检测 / 判断某格是否已占）。
        VmlJsonApi.Register("pixel", args =>
        {
            var x = (int)(args?.GetNumber("x") ?? -1);
            var y = (int)(args?.GetNumber("y") ?? -1);
            if (x < 0 || y < 0) return JNode.Object().Set("rgb", -1);

            var buf = new byte[4];
            if (!_host.Rasterize(x, y, 1, 1, buf)) return JNode.Object().Set("rgb", -1);
            // ⚠ 缓冲是 **RGBA**（见 IVmlHost.Rasterize 的约定），别按 BGRA 读
            var rgb = (buf[0] << 16) | (buf[1] << 8) | buf[2];
            return JNode.Object().Set("rgb", rgb);
        });

        // `screen`：与 `SCR_W`/`SCR_H`/`SCR_ORIENT` **同源**（就调宿主那几个函数），
        // 免得出现"JSON 里报的尺寸和 syscall 报的不一样"这种最难查的分叉。
        VmlJsonApi.Register("screen", _ =>
        {
            var area = _host.ScreenArea();
            var orient = _host.Orientation();
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
        var needed = Encoding.UTF8.GetByteCount(json);
        return WriteString(mem, r[2], r[3], VmlJsonApi.TooLongEnvelope(needed));
    }

    // ══════════════════════════════════════════════════════════════════════
    // 手感：音效 / 震动
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// BGM：路径交给宿主按它那把沙箱尺子解析（手机是 workspace、桌面是源文件所在目录），
    /// 文件不存在直接回 -1 —— 让程序自己看得见"没播成"，而不是静默没声音。
    /// </summary>
    private int AudioPlay(int[] r, byte[] mem)
    {
        var rel = Str(mem, r[0]);
        if (rel.Length == 0) return -1;
        var full = _host.ResolvePath(rel);
        if (!File.Exists(full)) return -1;
        return _host.PlayAudio(full, r[1] != 0) ? 0 : -1;
    }

    /// <summary>震动一下：时长先钳到上限（负数/超长都拦掉），强度交给平台层。</summary>
    private int Vibrate(int[] r)
        => _host.Vibrate(Math.Clamp(r[0], 1, VmlUi.VibrateMaxSegmentMs), Math.Clamp(r[1], 0, 255)) ? 0 : -1;

    /// <summary>
    /// 按节奏震动：把内存里的 int 数组读出来 → 协议层钳段数/段长 → 交给平台层。
    /// **读内存要防越界**：地址与段数都是程序给的，越界就地停（宁可少振几段，不要读坏内存）。
    /// </summary>
    private int VibratePattern(int[] r, byte[] mem)
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
        return pattern.Length > 0 && _host.VibratePattern(pattern) ? 0 : -1;
    }

    // ══════════════════════════════════════════════════════════════════════
    // 绘图增强（v0.96.176）
    //
    // 这六个号只是把**本来就在绘图 DSL 里**的能力接到 VML 侧：曲线展平、渐变采样、
    // 多边形填充在 Infra 那层早就有了（桌面 draw 工具一直在用），此前只是没有 syscall 入口。
    // 所以这里的方法都很薄 —— 真正干活的是 `VmlScene` 的 `AddXxx` 与 `Infra/DrawPath.cs`。
    // ══════════════════════════════════════════════════════════════════════

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

    /// <summary>
    /// `BRUSH`（#575）：造一个刷子 → 句柄（≥1；0 = 失败）。
    ///
    /// R0=种类（<see cref="VmlBrushKind"/>）R1..R6=参数。几何是**千分之一**的整数，
    /// 与 `ui_gradient` 同一口径 —— 换算**只在 `VmlScene.AddGradient` 一处**做，
    /// 这里不许再除一次（两端各换算一次就是沉默的错，那边注释记着踩过的坑）。
    /// </summary>
    private int Brush(int[] r, byte[] mem)
    {
        var s = Scene();
        if (s == null) return 0;
        switch (r[0])
        {
            case VmlBrushKind.Solid:
                return s.AddSolidBrush((uint)r[1]);
            case VmlBrushKind.Linear:
                return s.AddGradientBrush(radial: false, (uint)r[1], (uint)r[2], r[3], r[4], r[5], r[6]);
            case VmlBrushKind.Radial:
                return s.AddGradientBrush(radial: true, (uint)r[1], (uint)r[2], r[3], r[4], r[5], 0);
            case VmlBrushKind.ByName:
                return s.AddNamedBrush(Str(mem, r[1]));
            default:
                return 0;
        }
    }

    /// <summary>
    /// `DRAW_SHAPE`（#574）：一个号画所有形状。
    ///
    /// R0=形状码（<see cref="VmlShape"/>）R1..R7=七个通用槽。
    /// **样式取自当前状态**（`SET_STYLE`），只有 <see cref="VmlShape.EllipseGrad"/> 自带渐变名
    /// （与既有的 `ui_rect_grad` / `ui_circle_grad` 同形）。
    /// </summary>
    private bool DrawShape(int[] r, byte[] mem)
    {
        var s = Scene();
        if (s == null) return false;
        var shape = r[0];

        // 点数组 / 路径 / 文本 这三种要读内存，其余都是纯数值槽 —— 分开处理，
        // 免得把 "R1 是坐标" 和 "R1 是地址" 混进同一个 switch 里。
        switch (shape)
        {
            case VmlShape.Polygon:
            case VmlShape.Polyline:
            {
                var count = r[2];
                if (count < 2 || count > VmlUi.MaxPolyPoints) return false;
                var pts = new List<double>(count * 2);
                for (var i = 0; i < count; i++)
                {
                    var at = r[1] + i * 8;
                    if (at < 0 || at + 8 > mem.Length) break;      // 越界就地停，不读坏内存
                    pts.Add(BitConverter.ToInt32(mem, at));
                    pts.Add(BitConverter.ToInt32(mem, at + 4));
                }
                return pts.Count >= 4 && s.AddShapePoly(pts, close: shape == VmlShape.Polygon);
            }
            case VmlShape.Path:
                return s.AddShapePath(Str(mem, r[1]));
            case VmlShape.Text:
                return s.AddShapeText(r[1], r[2], Str(mem, r[3]));
            case VmlShape.EllipseGrad:
            {
                // 唯一自带刷子的形状码：没有渐变名就什么都不画（与 ui_rect_grad 一致）。
                var g = GradientIdOrNull(r, 5, mem);
                if (g == null) return false;
                s.AddEllipse(r[1], r[2], r[3], r[4], 0, filled: true, width: 0, fillGradient: g);
                return true;
            }
            default:
                return s.AddShape(shape, r[1], r[2], r[3], r[4], r[5], r[6], r[7]);
        }
    }

    /// <summary>渐变 id 指针 → 清洗后的 id；指针为 0（或读出来是空串）返回 null（= 用纯色填充）。</summary>
    public static string? GradientIdOrNull(int[] r, int reg, byte[] mem)
    {
        if (r[reg] == 0) return null;
        var id = VmlUi.SafeId(Str(mem, r[reg]));
        return id.Length == 0 ? null : id;
    }

    // ══════════════════════════════════════════════════════════════════════
    // 持久化与常亮
    //
    // ⚠ 这一组里**没有**"取时间"与"随机数"——VM 内置已有（`#53`/`#54` 与 `#50`），
    // 另立接口就是同一件事两处实现。
    // ══════════════════════════════════════════════════════════════════════

    private int StoreSet(int[] r, byte[] mem)
    {
        var key = VmlUi.StoreKey(Str(mem, r[0]));
        if (key == null) return -1;
        _host.StoreSet(key, Str(mem, r[1]));
        return 0;
    }

    private int StoreGet(int[] r, byte[] mem)
    {
        var key = VmlUi.StoreKey(Str(mem, r[0]));
        if (key == null) return -1;
        var value = _host.StoreGet(key);
        if (value == null) return -1;

        var capacity = Math.Max(0, r[2]);
        var bytes = Encoding.UTF8.GetBytes(value);
        var n = Math.Min(bytes.Length, Math.Max(0, capacity - 1));   // 留一个字节给结尾 \0
        if (r[1] >= 0 && r[1] + n + 1 <= mem.Length)
        {
            Array.Copy(bytes, 0, mem, r[1], n);
            mem[r[1] + n] = 0;
        }
        return n;
    }

    private int StoreDel(int[] r, byte[] mem)
    {
        var key = VmlUi.StoreKey(Str(mem, r[0]));
        if (key == null) return -1;
        _host.StoreDel(key);
        return 0;
    }

    // ══════════════════════════════════════════════════════════════════════
    // 内存读取
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>读 VML 内存里的 NUL 结尾字符串（UTF-8）。越界/非法一律返回空串，不抛。</summary>
    public static string Str(byte[] memory, int address)
    {
        if (address < 0 || address >= memory.Length) return "";
        var end = address;
        while (end < memory.Length && memory[end] != 0) end++;
        return Encoding.UTF8.GetString(memory, address, end - address);
    }

    /// <summary>读「选项块」：<paramref name="count"/> 个 \0 分隔的字符串。</summary>
    public static List<string> StrBlock(byte[] memory, int address, int count)
    {
        var list = new List<string>();
        if (count <= 0 || address < 0 || address >= memory.Length) return list;
        var at = address;
        for (var i = 0; i < count && at < memory.Length; i++)
        {
            var s = Str(memory, at);
            list.Add(s);
            at += Encoding.UTF8.GetByteCount(s) + 1;
        }
        return list;
    }

    /// <summary>
    /// 把字符串按 UTF-8 写进 VM 内存的缓冲区，返回写入字节数（不含结尾 NUL）；放不下返回 -1。
    /// </summary>
    public static int WriteString(byte[] mem, int dst, int cap, string text)
    {
        if (dst < 0 || cap <= 1 || dst + cap > mem.Length) return -1;

        var bytes = Encoding.UTF8.GetBytes(text);
        var n = Math.Min(bytes.Length, cap - 1);          // 留一个字节给结尾 NUL
        Array.Copy(bytes, 0, mem, dst, n);
        mem[dst + n] = 0;
        return bytes.Length <= cap - 1 ? n : -1;
    }

    // ══════════════════════════════════════════════════════════════════════
    // 像素读回（583–585）：floodfill / getimage / putimage
    //
    // 老 graphics.h 程序做**填充**与**精灵**绕不开读像素，而场景是保留模式的
    // ⇒ 三个号在宿主侧都要先**光栅化一次**。三条硬约定：
    //
    //   ① **坐标同源**：光栅化的画布尺寸就是场景自己的 `W×H`（DSL 里 `canvas W H`
    //      那一条），所以程序给的坐标**直接用**，不做任何换算。
    //   ② **alpha 一律补 255**：BGI 的屏幕是不透明的，而 XOR 会把 alpha 也异或成 0
    //      ⇒ 那一块会变成全透明、贴上去什么都看不见。读回与异或之后都强制不透明。
    //   ③ **句柄由宿主保管**：老程序的 `p = malloc(imagesize(...))` 照写不误，
    //      只是那块内存我们不用 —— 与 `ui_brush`/`ui_gradient` 同一套思路。
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>一块存下来的画面。<see cref="Rgba"/> 行优先、长度 = W*H*4。</summary>
    private sealed record ImageBlock(int W, int H, byte[] Rgba);

    private readonly Dictionary<int, ImageBlock> _images = new();
    private int _nextImageHandle = 1;

    /// <summary>
    /// `FLOOD_FILL`（BGI 的 `floodfill`）—— 从种子点灌色，碰到边界色停。
    ///
    /// 光栅化一次 → 扫描线求游程 → **每条游程落成一个矩形**（见 `FloodFill.Runs`）。
    /// 返回落笔了几条（0 = 没填，例如种子点本身就在边界上）。
    /// </summary>
    private int DoFloodFill(int[] r)
    {
        var scene = Scene();
        if (scene is null || scene.Width <= 0 || scene.Height <= 0) return 0;

        int x = r[0], y = r[1], fill = r[2], border = r[3];
        var rgba = new byte[scene.Width * scene.Height * 4];
        if (!_host.Rasterize(0, 0, scene.Width, scene.Height, rgba)) return 0;

        var px = RgbaToArgb(rgba);
        var runs = FloodFill.Runs(px, scene.Width, scene.Height, x, y, border);
        foreach (var run in runs)
            scene.AddFilledRun(run.X, run.Y, run.W, 1, (uint)fill);
        return runs.Count;
    }

    /// <summary>`GET_IMAGE` → 句柄（≥1），失败 0。</summary>
    private int DoGetImage(int[] r)
    {
        int x = r[0], y = r[1], w = r[2], h = r[3];
        if (w <= 0 || h <= 0) return 0;

        var buf = new byte[w * h * 4];
        if (!_host.Rasterize(x, y, w, h, buf)) return 0;

        Opaque(buf);                       // 见上面第 ② 条
        int handle = _nextImageHandle++;
        _images[handle] = new ImageBlock(w, h, buf);
        return handle;
    }

    /// <summary>`PUT_IMAGE`（0=COPY 直贴 / 1=XOR 异或）。</summary>
    private bool DoPutImage(int[] r)
    {
        var scene = Scene();
        if (scene is null) return false;

        int x = r[0], y = r[1], handle = r[2], mode = r[3];
        if (!_images.TryGetValue(handle, out var blk)) return false;

        var pixels = blk.Rgba;

        if (mode != 0)
        {
            // **XOR 必须先读目的像素** —— 这是它比 COPY 贵的地方（多一次光栅化 + 一次编码）。
            // 老程序拿它做"画上去再画一次就还原"（精灵保存-恢复）。
            var dst = new byte[blk.W * blk.H * 4];
            if (!_host.Rasterize(x, y, blk.W, blk.H, dst)) return false;
            for (int i = 0; i < pixels.Length; i++) pixels[i] ^= dst[i];
            Opaque(pixels);                // ⚠ 异或也会把 alpha 打成 0 ⇒ 必须补回来（第 ② 条）
        }

        var path = _host.SaveTempImage(blk.W, blk.H, pixels);
        if (path is null) return false;

        scene.AddImage(x, y, path, blk.W, blk.H);
        return true;
    }

    /// <summary>RGBA 字节流 → `0xAARRGGBB` 的 int 数组（`FloodFill` 要的形态）。</summary>
    private static int[] RgbaToArgb(byte[] rgba)
    {
        var px = new int[rgba.Length / 4];
        for (int i = 0; i < px.Length; i++)
            px[i] = (rgba[i * 4 + 3] << 24) | (rgba[i * 4] << 16)
                  | (rgba[i * 4 + 1] << 8) | rgba[i * 4 + 2];
        return px;
    }

    /// <summary>把 alpha 一律置 255（BGI 的屏幕不透明；异或之后尤其要补，否则整块透明）。</summary>
    private static void Opaque(byte[] rgba)
    {
        for (int i = 3; i < rgba.Length; i += 4) rgba[i] = 255;
    }

}
