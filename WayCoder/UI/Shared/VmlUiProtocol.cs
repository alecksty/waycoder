using System.Text;

namespace WayCoder.UI.Shared;

/// <summary>
/// 手机端 VML「对话框 + 窗体绘图 + 输入」的**协议层** —— 纯逻辑、无 MAUI 依赖，
/// 放在 <c>UI/Shared</c> 是为了让主工程（含自测）与 MAUI 工程**编译同一份**
/// （跨端共享的纯逻辑必须放这里，放错目录 MAUI 就引用不到，只能各抄一份 —— 本仓库头号坑）。
///
/// 分工：
///   · 本文件 = syscall 号、消息模型、消息队列、场景（图元）→ 绘图 DSL 的生成、内存串读取；
///   · <c>WayCoder.Maui/Services/VmlUiCalls.cs</c> = <c>ISystemCallHandler</c> 实现，把寄存器/内存
///     翻成本文件的模型，再驱动真正的 UI（弹窗、开窗口页）；
///   · <c>WayCoder.Maui/Pages/DrawWindowPage</c> = 把场景渲染出来（**复用现成的
///     <c>DrawRunner.Parse</c> + <c>ToPng</c>**，不另写渲染器 —— 见 <see cref="VmlScene.BuildDsl"/>）。
///
/// ## 为什么这些 syscall 号选在 500–599
/// 运行时现用最大号是 402（TTY_WriteChar 那一族，见 <c>VMLRuntime/SyscallNumber.cs</c>），
/// 500 起留足余量。宿主处理器在**内置 switch 之前**被调用（<c>VMLRuntime.Syscall.cs:23</c>），
/// 所以这个号段完全由宿主解释、**运行时一行不用改**。
///
/// ⚠ 但有**一道门必须先过**：dispatch 顶部有
/// <c>if (privilegeLevel &gt; 0 &amp;&amp; !UserAllowedSyscalls.Contains(n)) → 拒绝</c>，
/// 而手机端跑的是 mcu 模式（privilege &gt; 0）。<c>SyscallConstants.UserAllowed</c> 是
/// <c>public static readonly HashSet&lt;int&gt;</c>（可增）且与运行时的
/// <c>UserAllowedSyscalls</c> 是**同一个对象引用**，所以宿主启动时把 500–599 加进去即可 ——
/// 门一过，处理器就会被调用。漏了这一步的现象是「处理器明明注册了却永远不被调用」。
/// </summary>
public static class VmlUi
{
    // ── 对话框 ──
    /// <summary>消息框：R0=标题* R1=正文* R2=样式(0信息/1警告/2错误/3询问) → 0。</summary>
    public const int DlgMsg = 500;
    /// <summary>单选：R0=标题* R1=提示* R2=选项块* R3=选项数 R4=默认项 → 选中索引，取消 -1。</summary>
    public const int DlgSelect = 501;
    /// <summary>多选：参数同单选 → 选中位掩码，取消 -1。</summary>
    public const int DlgMulti = 502;
    /// <summary>文本输入：R0=标题* R1=提示* R2=缓冲* R3=容量 → 写入缓冲，返回长度，取消 -1。</summary>
    public const int DlgInput = 503;

    // ── 窗体与绘图（保留模式）──
    /// <summary>开窗口：R0=标题* R1=宽 R2=高 → 句柄，失败 -1。</summary>
    public const int WinOpen = 520;
    /// <summary>关窗口：R0=句柄 → 0。</summary>
    public const int WinClose = 521;
    /// <summary>清屏：R0=颜色(ARGB) → 0（同时清空图元表）。</summary>
    public const int DrawClear = 522;
    /// <summary>点：x y 颜色 → 0。</summary>
    public const int DrawPixel = 523;
    /// <summary>线：x1 y1 x2 y2 颜色 线宽 → 0。</summary>
    public const int DrawLine = 524;
    /// <summary>矩形：x y w h 颜色 填充(0/1) 线宽 圆角半径 → 0（半径 &gt; 0 即圆角矩形）。</summary>
    public const int DrawRect = 525;
    /// <summary>圆：cx cy r 颜色 填充 线宽 → 0。</summary>
    public const int DrawCircle = 526;
    /// <summary>椭圆：cx cy rx ry 颜色 填充 线宽 → 0。</summary>
    public const int DrawEllipse = 527;
    /// <summary>文字：x y 文本* 颜色 字号 锚点(0左/1中/2右) → 0。</summary>
    public const int DrawText = 528;
    /// <summary>图标：x y 图标名* 尺寸 颜色 → 0（走 emoji 文本，见 <see cref="VmlScene.AddIcon"/>）。</summary>
    public const int DrawIcon = 529;
    /// <summary>图片：x y 路径* w h → 0。</summary>
    public const int DrawImage = 530;
    /// <summary>提交本帧：→ 0（保留模式下宿主定时器也会刷，此调用用于让程序显式标记帧边界）。</summary>
    public const int DrawPresent = 531;

    // ── 输入（统一消息队列）──
    /// <summary>非阻塞取一条消息：R0=消息缓冲地址 → 消息类型，无消息返回 0。</summary>
    public const int MsgPoll = 560;
    /// <summary>阻塞取一条消息：R0=消息缓冲地址 R1=超时毫秒(0=无限) → 消息类型，超时返回 0。</summary>
    public const int MsgWait = 561;
    /// <summary>队列里待处理消息数（非阻塞）→ 条数。</summary>
    public const int MsgCount = 562;
    /// <summary>装定时器：R0=间隔毫秒 R1=用户标记 → 定时器 id；消息以 <see cref="VmlMsgType.Timer"/> 入队。</summary>
    public const int TimerSet = 563;
    /// <summary>删定时器：R0=id → 0。</summary>
    public const int TimerKill = 564;
    /// <summary>窗口是否被用户关掉（返回箭头）：R0=句柄 → 1/0。程序据此退出主循环。</summary>
    public const int WinClosed = 565;

    /// <summary>
    /// 可用绘图区**宽**（绘图单位）→ 宽度。
    /// </summary>
    public const int ScrW = 566;
    /// <summary>
    /// 可用绘图区**高**（绘图单位）→ 高度。
    ///
    /// ## 为什么必须有这两个接口
    /// 原来只能由程序自己拍一个尺寸（`WIN_OPEN(title, 320, 240)`），而**宿主拿到什么尺寸都得显示**：
    /// 屏幕比它大就放大、比它小就缩小，且缩放时若没保住纵横比就会**非等比拉伸**（实测过：
    /// 320×240 被拉成 320×395 的视口，画在靠下位置的图元直接被挤出可视区）。
    /// 程序对此毫无办法 —— 它根本不知道设备长什么样。
    ///
    /// 现在的用法是**先问后开**：
    /// <code>
    /// SYSCALL #566 → W        ; 问可用宽
    /// SYSCALL #567 → H        ; 问可用高
    /// WIN_OPEN(title, W, H)   ; 按它开窗
    /// </code>
    /// 这样绘图单位与屏幕 1:1（单位 = 密度无关像素 dp），既不缩放也不出界。
    /// 单位取 dp 而不是物理像素：dp 在不同 DPI 上观感一致，"画一个 40 单位的按钮"
    /// 在高低分屏手机上看起来一样大。
    /// </summary>
    public const int ScrH = 567;

    /// <summary>
    /// 可用绘图区尺寸的纯计算（宿主把设备参数喂进来）—— 放这里是为了**可自测**：
    /// 设备像素 / 密度 = dp；再扣掉导航栏、标题、底部方向键与四周留白，
    /// 剩下的才是能安全绘制的区域。所有扣减都在这一处，宿主不许自己再算一份。
    /// </summary>
    /// <param name="displayWidthPx">屏幕宽（物理像素）</param>
    /// <param name="displayHeightPx">屏幕高（物理像素）</param>
    /// <param name="density">像素密度（dp → px 的倍率）</param>
    /// <param name="chromeHeightDp">非绘图区的固定占用（导航栏 + 标题 + 方向键 + 上下留白），dp</param>
    public static (int Width, int Height) AvailableArea(
        double displayWidthPx, double displayHeightPx, double density, int chromeHeightDp = 170)
    {
        if (density <= 0) density = 1;
        var w = (int)Math.Floor(displayWidthPx / density) - 16;  // 左右各 8dp 留白
        var h = (int)Math.Floor(displayHeightPx / density) - chromeHeightDp;
        // 下限给足（太小的话程序没法布局）；上限防止异常设备算出离谱值
        return (Math.Clamp(w, 120, 2048), Math.Clamp(h, 120, 4096));
    }

    /// <summary>本协议是否认领该 syscall 号。**不认识必须返回 false**，否则会把内置 syscall 吞掉。</summary>
    public static bool Handles(int syscallNumber) => syscallNumber is >= 500 and <= 599;

    /// <summary>号段上界（宿主启动时把这整段加进 <c>SyscallConstants.UserAllowed</c> 用）。</summary>
    public static IEnumerable<int> ReservedRange()
    {
        for (var n = 500; n <= 599; n++) yield return n;
    }
}

/// <summary>
/// 键码约定 —— 沿用 **Win32 虚拟键值**（与 HTML <c>keyCode</c> 也基本一致）：
/// 方向键 37–40、回车 13、空格 32、ESC 27、字母/数字直接用其 ASCII 大写。
/// 选这套而不是自造一套编号，是因为程序作者多半已经熟悉它，跨 Win32/浏览器/VML 的键码表能直接照搬；
/// 包装库与绘图窗口的屏幕按键都引用这里，**不留第二份键码表**。
/// </summary>
public static class VmlKeys
{
    public const int Backspace = 8;
    public const int Enter = 13;
    public const int Escape = 27;
    public const int Space = 32;
    public const int Left = 37;
    public const int Up = 38;
    public const int Right = 39;
    public const int Down = 40;

    // ── 手柄按键（绘图窗口底部按游戏机布局排布）──
    // 取值刻意**映射到自然键盘等价键**，而不是另造一套手柄编号：
    //   · A/B/X/Y 就是字母键本身（程序里写 `key == VmlKeys.PadA`，用 'A' 也对得上）
    //   · START/SELECT/PAUSE 映射到 回车 / Shift / Pause
    // 这样同一份 VML 程序在手机（屏幕手柄）与桌面（物理键盘）上都能操作，
    // 不会出现"手机上能玩、PC 上按哪个键都不知道"。
    public const int PadA = 65;   // 'A'
    public const int PadB = 66;   // 'B'
    public const int PadX = 88;   // 'X'
    public const int PadY = 89;   // 'Y'
    public const int Start = Enter;   // 13
    public const int Select = 16;     // VK_SHIFT
    public const int Pause = 19;      // VK_PAUSE
}

/// <summary>
/// 输入消息类型。设计成**统一队列**（而不是一堆 <c>read_key</c>/<c>pointer_x</c> 之类分散接口）：
/// 键盘、鼠标、触摸、定时器、窗口事件在这里是同一种东西，程序的循环只有一种写法 ——
/// 「取消息 → switch 类型 → 处理」，与 Win32/SDL 那套事件循环同构。
/// 分开的读接口在**同时有触摸和键盘**时没法表达"先来的先处理"，做游戏必然要自己再拼一个队列，
/// 那不如宿主就给一个。
/// </summary>
public enum VmlMsgType
{
    None = 0,
    KeyDown = 1,
    KeyUp = 2,
    MouseMove = 3,
    MouseDown = 4,
    MouseUp = 5,
    TouchDown = 6,
    TouchMove = 7,
    TouchUp = 8,
    /// <summary>定时器到期；A = 定时器 id，B = 装机时给的用户标记。</summary>
    Timer = 9,
    /// <summary>用户点了窗口的返回箭头/关闭。</summary>
    WindowClose = 10,
    /// <summary>窗口尺寸变化；A = 新宽，B = 新高。</summary>
    WindowResize = 11,
}

/// <summary>
/// 一条输入消息 —— 固定 **16 字节**的内存布局，程序按 4 个 int 读：
/// <c>[0]=类型 [1]=A [2]=B [3]=时间戳毫秒</c>。
/// 定长布局是为了让任何语言的 VML 前端（C 用 struct、Basic 用 PEEK）都能直接读，
/// 不依赖宿主的序列化格式。
/// </summary>
public readonly record struct VmlMessage(VmlMsgType Type, int A, int B, int TimeMs)
{
    /// <summary>消息在程序内存里的字节数（4 个 int）。</summary>
    public const int SizeBytes = 16;

    /// <summary>按小端写入程序内存（VML 是 32 位小端）。越界直接不写，由调用方判断。</summary>
    public void WriteTo(byte[] memory, int address)
    {
        if (address < 0 || address + SizeBytes > memory.Length) return;
        WriteInt(memory, address, (int)Type);
        WriteInt(memory, address + 4, A);
        WriteInt(memory, address + 8, B);
        WriteInt(memory, address + 12, TimeMs);
    }

    private static void WriteInt(byte[] memory, int at, int value)
    {
        memory[at] = (byte)value;
        memory[at + 1] = (byte)(value >> 8);
        memory[at + 2] = (byte)(value >> 16);
        memory[at + 3] = (byte)(value >> 24);
    }
}

/// <summary>
/// 输入消息队列 —— 宿主侧（页面手势/键盘/定时器）投递，VML 程序经 syscall 取走。
///
/// 线程模型：投递方是 **UI 线程**（MAUI 事件回调），取走方是 **VM 线程**（VmlTool 在后台跑），
/// 所以这里用锁 + 队列；阻塞取用 <see cref="SemaphoreSlim"/> 放行（与 <c>MauiVml</c> 里
/// 「VM 线程阻塞等宿主输入」的既有模型一致，见 <c>CaptureIo.ReadLine</c>）。
/// </summary>
public sealed class VmlMessageQueue
{
    private readonly Queue<VmlMessage> _queue = new();
    private readonly Lock _lock = new();
    private readonly SemaphoreSlim _signal = new(0);

    /// <summary>当前待处理条数。</summary>
    public int Count
    {
        get { lock (_lock) return _queue.Count; }
    }

    /// <summary>投递一条消息（UI 线程调用）。</summary>
    public void Post(VmlMessage msg)
    {
        lock (_lock) _queue.Enqueue(msg);
        if (_signal.CurrentCount == 0) _signal.Release();
    }

    /// <summary>非阻塞取一条；无消息返回 null。</summary>
    public VmlMessage? TryTake()
    {
        lock (_lock) return _queue.Count > 0 ? _queue.Dequeue() : null;
    }

    /// <summary>
    /// 阻塞取一条，最多等 <paramref name="timeoutMs"/> 毫秒（0 = 无限等）。
    /// 超时返回 null。**阻塞方是 VM 线程**，不要从 UI 线程调。
    /// </summary>
    public VmlMessage? Take(int timeoutMs)
    {
        // 先看队列：有就直接拿走，不走信号量（信号量的计数与队列长度不是一对一的 ——
        // 连投两条只 Release 一次，靠信号量判断会漏消息）
        if (TryTake() is { } first) return first;

        var waited = timeoutMs <= 0
            ? _signal.Wait(Timeout.Infinite)
            : _signal.Wait(timeoutMs);
        if (!waited) return null;
        return TryTake();
    }

    /// <summary>清空（每次 VML 运行开始前调用，避免上一轮的消息串到这一轮）。</summary>
    public void Clear()
    {
        lock (_lock) _queue.Clear();
        while (_signal.Wait(0)) { /* 把信号量计数也归零 */ }
    }
}

/// <summary>
/// 一个 VML 绘图窗口的场景（保留模式）。
///
/// **保留**是关键：程序把图元一条条追加进来，宿主按帧把整份场景渲染出来。
/// 若改成「立即模式」（每条绘图 syscall 立刻画），那么绘制就发生在 VM 线程上、
/// 而 UI 必须在主线程 —— 每一笔都要跨线程往返一次，画面还会跟 UI 的重绘节奏打架。
/// 保留模式下 VM 只追加、UI 只渲染，两者彻底解耦，程序里画一个圆不依赖 UI 此刻在不在重绘。
///
/// 场景**不自己渲染**：最终交给既有的 <c>DrawRunner.Parse</c> + <c>ToPng</c>（或 SVG）出图。
/// 也就是说图元的语义只有一处实现（<c>DrawCommandRegistry</c> 那 16 条指令），
/// 这里只负责把 syscall 参数**翻译成 DSL 行**。
/// </summary>
public sealed class VmlScene
{
    private readonly List<string> _figures = new();

    public string Title { get; set; } = "VML";
    public int Width { get; set; } = 320;
    public int Height { get; set; } = 240;

    /// <summary>背景色（0xAARRGGBB）。</summary>
    public uint Background { get; set; } = 0xFF000000;

    /// <summary>用户是否已请求关闭窗口（点了返回箭头）。程序据此退出主循环。</summary>
    public bool Closed { get; set; }

    /// <summary>图元条数（宿主用它判断要不要重绘）。</summary>
    public int FigureCount { get { lock (_figures) return _figures.Count; } }

    /// <summary>每次内容变化 +1 —— 宿主缓存渲染结果时比对它，避免无变化也重编码 PNG。</summary>
    public int Version { get; private set; }

    /// <summary>清屏：清空图元并置背景色。</summary>
    public void Clear(uint background)
    {
        lock (_figures) _figures.Clear();
        Background = background;
        Version++;
    }

    /// <summary>点 —— DSL 没有单像素指令，用 1×1 的填充矩形表达（语义等价，且复用现成光栅路径）。</summary>
    public void AddPixel(int x, int y, uint color) => Add($"rect {x} {y} 1 1 {Hex(color)}");

    public void AddLine(int x1, int y1, int x2, int y2, uint color, int width)
        => Add($"line {x1} {y1} {x2} {y2} {Hex(color)}{(width > 0 ? " " + width : "")}");

    /// <summary>矩形；<paramref name="radius"/> &gt; 0 时走 DSL 的 roundrect（圆角矩形）。</summary>
    public void AddRect(int x, int y, int w, int h, uint color, bool filled, int width, int radius)
    {
        var name = radius > 0 ? "roundrect" : "rect";
        var extra = radius > 0 ? $" {radius}" : "";
        Add($"{name} {x} {y} {w} {h}{extra}{Style(color, filled, width)}");
    }

    public void AddCircle(int cx, int cy, int r, uint color, bool filled, int width)
        => Add($"circle {cx} {cy} {r}{Style(color, filled, width)}");

    public void AddEllipse(int cx, int cy, int rx, int ry, uint color, bool filled, int width)
        => Add($"ellipse {cx} {cy} {rx} {ry}{Style(color, filled, width)}");

    /// <summary>文字；<paramref name="anchor"/> 0=左 1=中 2=右。</summary>
    public void AddText(int x, int y, string text, uint color, int fontSize, int anchor)
        => Add($"text {x} {y} \"{Escape(text)}\" {fontSize} {Hex(color)} {AnchorName(anchor)}");

    /// <summary>
    /// 图标 —— 走 emoji 文本（DSL 里没有 sprite/图标指令）。
    /// 好处是不引入第二套资源管线、任何字体支持的 emoji 立刻可用；代价是形状取决系统 emoji 字体。
    /// 未知名字退化成「画一个方块」，至少让程序看出"这里该有个图标"。
    /// </summary>
    public void AddIcon(int x, int y, string name, int size, uint color)
    {
        if (!Icons.TryGetValue(name.Trim().ToLowerInvariant(), out var ch))
        {
            AddRect(x, y, size, size, color, filled: false, width: 2, radius: 0);
            return;
        }
        Add($"text {x} {y} \"{ch}\" {size} {Hex(color)} start");
    }

    public void AddImage(int x, int y, string path, int w, int h)
        => Add($"image {x} {y} \"{Escape(path)}\"{(w > 0 ? " " + w : "")}{(h > 0 ? " " + h : "")}");

    private void Add(string line)
    {
        lock (_figures) _figures.Add(line);
        Version++;
    }

    /// <summary>把场景翻成绘图 DSL（首行是 <c>canvas</c> 头）。宿主把它交给 DrawRunner 出图。</summary>
    public string BuildDsl()
    {
        var sb = new StringBuilder();
        sb.Append("canvas ").Append(Width).Append(' ').Append(Height).Append(' ').Append(Hex(Background)).Append('\n');
        lock (_figures)
            foreach (var f in _figures)
                sb.Append(f).Append('\n');
        return sb.ToString();
    }

    /// <summary>
    /// 形状样式串。DSL 的规则是「**第一个颜色 = 填充，第二个 = 描边**，裸数字 = 线宽」
    /// （见 <c>DrawCommands.ParseStyle</c>）。所以空心图形要把填充显式写成全透明色 ——
    /// 这里传 <c>#00000000</c> 而不是省略，否则颜色位会被描边占用、变成"填充了描边的颜色"。
    /// </summary>
    private static string Style(uint color, bool filled, int width)
    {
        var w = width > 0 ? $" {width}" : "";
        return filled
            ? $" {Hex(color)}{w}"
            : $" {Hex(0x00000000u)} {Hex(color)}{w}";
    }

    /// <summary>0xAARRGGBB → <c>#AARRGGBB</c>（<c>ColorUtil</c> 的 8 位分支按此解析）。</summary>
    private static string Hex(uint argb) => "#" + argb.ToString("X8");

    private static string AnchorName(int anchor) => anchor switch
    {
        1 => "middle",
        2 => "end",
        _ => "start",
    };

    /// <summary>DSL 里文本用双引号包裹，所以引号与反斜杠要转义；换行会把一行拆成两行，直接换成空格。</summary>
    private static string Escape(string s)
        => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");

    /// <summary>图标名 → emoji（小表，够用即可；未命中退化画方框）。</summary>
    private static readonly Dictionary<string, string> Icons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ok"] = "✅", ["cancel"] = "❌", ["warn"] = "⚠️", ["info"] = "ℹ️",
        ["star"] = "⭐", ["heart"] = "❤️", ["up"] = "⬆️", ["down"] = "⬇️",
        ["left"] = "⬅️", ["right"] = "➡️", ["player"] = "🙂", ["enemy"] = "👾",
        ["coin"] = "🪙", ["bomb"] = "💣", ["rocket"] = "🚀", ["ball"] = "⚽",
    };
}
