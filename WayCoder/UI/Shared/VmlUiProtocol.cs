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

    /// <summary>
    /// 开窗口（**带两个声明**）：R0=标题* R1=宽 R2=高 R3=可旋转 R4=要手柄 → 句柄，失败 -1。
    ///
    /// 为什么不直接把 <see cref="WinOpen"/> 扩成 5 个参数：**老程序会静默走进未定义行为**。
    /// 宿主是从 `registers[3]/[4]` 读的，而只传 3 个参数的程序那两只寄存器里是**它自己上一句
    /// 留下的值**（可能是个指针、可能是个计数），宿主无从判断"这是不是真给了"。
    /// 所以新能力一律走新号 —— 与 `MSG_CLEAR`(#568) 当初的处理一致。
    /// C 侧的老名字 `ui_win_open(t,w,h)` 保留、语义一字不改（走 #520）。
    ///
    /// ## R3 转屏（三档：<see cref="PortraitOnly"/> / <see cref="Rotatable"/> / <see cref="LandscapeOnly"/>）
    /// · `0` / `VML_WIN_PORTRAIT` = **只支持竖屏**（棋盘类游戏）：屏幕锁在竖屏，怎么转都不动；
    /// · `1` / `VML_WIN_ROTATABLE` = **支持旋转**（默认）：两种排版都写好了 ⇒ 视口一变，
    ///   宿主把**新的坐标空间**（可用绘图区）整个给到场景，并先发 `WINDOWORIENT` 再发 `WINDOWRESIZE`；
    /// · `2` / `VML_WIN_LANDSCAPE` = **只支持横屏**（赛车 / 横版过关）。
    ///
    /// **老接口没有这一位**，所以"没声明"是独立的一档（见 <see cref="WindowRotation"/>）：
    /// 跟随旋转但**坐标系不动**（宿主等比缩放着显示）—— 老行为，一字不改。
    ///
    /// ⚠ `ROTATABLE` 那一档才换坐标系，是**故意的**：不处理 `WINDOWRESIZE` 的程序
    /// 被换了空间之后会继续按老坐标画，空间变小 ⇒ 内容被裁掉一大截（比"等比缩小"更糟）。
    ///
    /// ## R4 要手柄（<see cref="NeedGamepad"/> / <see cref="NoGamepad"/>）
    /// 0 = 这个程序不用手柄（画图表、写文档、放幻灯片）⇒ **整块手柄区连同折叠条一起不显示**，
    /// 画布直接吃满整屏；1 = 显示（默认）。这一条比"开完窗再点收起"强在**开窗前就生效** ——
    /// 程序按 `SCR_W/H` 排的版一开始就是对的，不会先按小画布排一次、再收到 resize 重排。
    /// </summary>
    public const int WinOpenEx = 570;

    /// <summary>`WIN_OPEN_EX` 的 R3：**只支持竖屏**（把屏幕锁在竖屏）。</summary>
    public const int PortraitOnly = 0;
    /// <summary>`WIN_OPEN_EX` 的 R3：**支持旋转**（默认）；视口变了宿主会换掉坐标系。</summary>
    public const int Rotatable = 1;
    /// <summary>`WIN_OPEN_EX` 的 R3：**只支持横屏**（把屏幕锁在横屏）。</summary>
    public const int LandscapeOnly = 2;
    /// <summary>`WIN_OPEN_EX` 的 R4：显示屏幕手柄（默认）。</summary>
    public const int NeedGamepad = 1;
    /// <summary>`WIN_OPEN_EX` 的 R4：不要手柄区，画布吃满整屏。</summary>
    public const int NoGamepad = 0;
    /// <summary>
    /// **全能接口**：R0=函数名* R1=参数 JSON* R2=输出缓冲 R3=缓冲容量 → 写入字节数，失败 -1。
    ///
    /// 两个字符串进、一个 JSON 字符串出（结果写进调用方给的缓冲区，与
    /// <see cref="DlgInput"/> 返回文本同一套 —— VM 里没有宿主能"交还"的堆）。
    /// 用来承载**不要求性能**的可扩展功能：加一个能力 = 宿主侧
    /// <see cref="VmlJsonApi.Register"/> 一行，**不用占号、不用重生成 22 种语言的绑定**。
    ///
    /// ⚠ 绘图/输入这类每帧都发生的调用**别走这里**：一次调用要序列化+解析两趟 JSON
    /// 再穿一次内存缓冲。专用号仍然更快也更明确。
    /// 返回值的信封格式（成功 `{"ok":true,"result":…}` / 失败 `{"ok":false,"error":"…"}`）、
    /// 以及"缓冲区太小"怎么处理，见 <see cref="VmlJsonApi"/>。
    /// </summary>
    public const int CallJson = 573;

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

    /// <summary>
    /// 设置**当前文字属性**（供 <see cref="Text"/> 用）：R0=字号 R1=样式位(1=粗 2=斜) R2=颜色 R3=锚点 → 0。
    ///
    /// 与 <see cref="DrawText"/> 是"状态式 vs 一次性"两种写法，两者都留着：
    /// · 状态式（本号 + <see cref="Text"/>）适合一次设好、连画很多行（菜单、对话框、计分板）；
    /// · 一次性（<see cref="DrawText"/>）适合每行属性都不同的场合。
    /// 属性存在**宿主侧**（不在 VML 内存里），所以程序不必自己维护这几个变量。
    /// </summary>
    public const int SetFont = 532;

    /// <summary>用**当前文字属性**画一行字：R0=x R1=y R2=文本*(UTF-8, NUL 结尾) → 0（属性由 <see cref="SetFont"/> 设定）。</summary>
    public const int Text = 533;

    // ── 输入（统一消息队列）──
    /// <summary>非阻塞取一条消息：R0=消息缓冲地址 → 消息类型，无消息返回 0。</summary>
    public const int MsgPoll = 560;
    /// <summary>阻塞取一条消息：R0=消息缓冲地址 R1=超时毫秒(0=无限) → 消息类型，超时返回 0。</summary>
    public const int MsgWait = 561;
    /// <summary>队列里待处理消息数（非阻塞）→ 条数。</summary>
    public const int MsgCount = 562;
    /// <summary>
    /// 丢掉队列里**所有待处理消息**（非阻塞）→ 丢弃条数。
    ///
    /// **为什么需要它**：一次点击往往产生**多条**消息（按下/抬起/移动各一条），
    /// 游戏主循环通常只读它要的那一条，剩下的就留在队列里 —— 于是"重开一局"时
    /// `ui_poll` 又把**上一局的残留**读出来，黑子立刻落到上次最后点的位置上。
    /// 程序应在**重新开始 / 切关 / 暂停恢复**这类状态断点上调用它，把历史输入清干净。
    /// </summary>
    public const int MsgClear = 568;
    /// <summary>
    /// 读一条消息（**非阻塞，带"读完之后留不留"**）：R0=消息缓冲地址 R1=保留位 → 消息类型。
    ///
    /// <paramref name="keep"/> 的语义（<see cref="Consume"/> / <see cref="Keep"/>）：
    /// - **消费（0，默认）** = 读完就没了，下一条 poll 拿到的是再下一条 —— 与
    ///   <see cref="MsgPoll"/> 完全一致。
    /// - **保留（1）** = 只**看**队头那一条，队列里一个都不少 —— 下一次 poll/wait 还是它，
    ///   直到程序**明确地**消费掉它（再调一次消费模式的 poll）。
    ///
    /// 什么时候要"保留"：程序想**先看一眼再决定谁处理**（比如"是触摸就自己吃掉、
    /// 是按键就留给下一层"），或者一帧里要按同一条消息做几件事。**别拿它当循环条件** ——
    /// 保留模式下 poll 永远返回同一条，写成 `while (ui_poll_ex(...) != 0)` 就是死循环。
    ///
    /// ⚠ 与 <see cref="WinOpenEx"/> 同样的理由走新号：老程序只传 R0，R1 里是**它自己
    /// 上一句留下的值**，宿主无从判断那是"保留"还是垃圾。
    /// </summary>
    public const int MsgPollEx = 571;

    /// <summary>
    /// 读一条消息（**阻塞，带"读完之后留不留"**）：R0=缓冲地址 R1=超时毫秒(0=无限) R2=保留位 → 类型。
    /// 保留位语义见 <see cref="MsgPollEx"/>。
    /// </summary>
    public const int MsgWaitEx = 572;

    /// <summary>读消息的 R? 保留位：读完就没了（默认行为）。</summary>
    public const int Consume = 0;
    /// <summary>读消息的 R? 保留位：只读**队头**那一条，队列里一个都不少。</summary>
    public const int Keep = 1;

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
    /// **屏幕方向**（0 = 竖屏，1 = 横屏）→ 方向。
    ///
    /// ## 为什么单独给一个号，而不是让程序拿 `SCR_W > SCR_H` 去推
    ///
    /// 那两个数报的是**可用绘图区**（实测横屏 396×301、竖屏 411×525），形状确实跟着方向走，
    /// 但它是"宿主排版算出来"的二手信息 —— 手柄收起/展开、页面留白、以后再加什么 chrome 都会动它，
    /// 而**方向是设备本身的属性**，不随宿主怎么排版而变。程序要分支排版（棋盘放左还是放上、
    /// 信息面板横排还是竖排）时，问的应该是"机器横着还是竖着拿"，不是"我这块画布是不是扁的"。
    ///
    /// ## 用法（开窗**之前**就能问）
    /// <code>
    /// SYSCALL #569 → LAND      ; 0 竖屏 / 1 横屏
    /// W = SCR_W(); H = SCR_H()
    /// WIN_OPEN(title, W, H)
    /// </code>
    /// 运行中方向变了：宿主**直接发** <see cref="VmlMsgType.WindowOrient"/>（A=新方向），
    /// 不必让程序自己去猜或轮询；同一次变化还会紧跟一条
    /// <see cref="VmlMsgType.WindowResize"/>（A=新宽 B=新高）。
    /// </summary>
    public const int ScrOrient = 569;

    /// <summary>`SCR_ORIENT` / <see cref="VmlUi.OrientationOf"/> 的返回值：竖屏。</summary>
    public const int Portrait = 0;
    /// <summary>`SCR_ORIENT` / <see cref="VmlUi.OrientationOf"/> 的返回值：横屏。</summary>
    public const int Landscape = 1;

    /// <summary>
    /// 方向判定的**纯逻辑**（宽 > 高 = 横屏）—— 宿主与自测**共用这一处**，
    /// 别在别处再写一遍比较（"同一规则两处实现"是本仓库头号坑）。
    /// 边长相等（正方形）算竖屏：那是"还没量到"或异常设备的兜底，取保守的一档。
    /// </summary>
    public static int OrientationOf(double width, double height)
        => width > height ? Landscape : Portrait;

    /// <summary>
    /// 一块**实测视口**（宽高）是不是当前方向下量出来的。
    ///
    /// 判据就是"它自己的形状与方向一致"（<see cref="OrientationOf"/>），
    /// 宿主拿它决定要不要沿用上次量到的那个值。用它的地方见
    /// `VmlUiCalls.ScrArea()` 的注释 —— 那里的坑是**跨方向陈旧**：
    /// 竖屏里关掉窗口之后转屏，转屏期间没有绘图页在跑，那个实测值没人更新，
    /// 横屏开的第一局就会拿到竖屏尺寸（实测 `scene=411x525` 塞进横屏画布，
    /// 画面只剩中间一条）。**这类"上次量到的值"用之前一定要问一句"它还算数吗"。**
    /// </summary>
    public static bool ViewportMatchesOrientation(double width, double height, int orientation)
        => OrientationOf(width, height) == orientation;

    /// <summary>
    /// 估算的固定占用 —— **只在"还没量到真实视口"时用**（见下）。
    ///
    /// v0.96.173 由 170 调到 262：那 170 只算了导航栏 + 方向键，**漏了绘图窗口页自己的
    /// 折叠条（26dp）与画布留白（16dp）**，于是每次会话里**第一个** VML 窗口会比可用视口高
    /// 约 90dp —— 用户看到的是「内容超出绘图区，下面被键盘区挡住」（实测）。
    /// 真实值由 `DrawWindowPage.PublishViewport`（跑在 40ms 定时器那拍，**布局落定之后**）
    /// 量到后写进 `MeasuredViewport`，那之后的窗口一律用它；
    /// 这个常数只是"第一次开窗之前"的兜底。
    ///
    /// ⚠ 那个常数是**竖屏**下的实测值：横屏时页面总高本来就只有 300 多 dp，
    /// 再扣 262 只剩几十 —— 所以横屏里"第一次开窗"必然偏小，
    /// 得靠上面那个实测值兜（第二局起就贴了）。
    /// </summary>
    public const int DefaultChromeHeightDp = 262;

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
        double displayWidthPx, double displayHeightPx, double density,
        int chromeHeightDp = DefaultChromeHeightDp)
    {
        if (density <= 0) density = 1;
        var dpW = (int)Math.Floor(displayWidthPx / density);
        var dpH = (int)Math.Floor(displayHeightPx / density);

        // **横竖屏要扣的不是同一个方向**：竖屏手柄在底部（吃高度），横屏手柄分成左右两列
        // （吃宽度、几乎不吃高度）、而且 Shell 的 TabBar 也是收起来的。
        // 拿竖屏那套常数去扣横屏，会算出一个「898 × 149」的畸形区域 ——
        // 程序照着开窗就是一扇又宽又扁的窗，再按比例塞回中间的画布只剩几十 dp 高，
        // 等于"横屏画面还是小"。下面的两个常数是**照实际布局量出来的**（模拟器实测
        // 画布 396.4×301.0，屏幕 914.3×411.4）：高度扣的是状态栏 + 导航栏 + 折叠条，
        // 宽度扣的是左右两列手柄 + 间距 + 页边距。
        // ⚠ 改了 `DrawWindowPage.xaml` 里手柄的尺寸/间距就要回来改 `LandscapeSideChromeDp`。
        // 这只是**第一次开窗之前**的兜底：只要开出过一次窗口，
        // `MeasuredViewport` 就把真实值接管了（见 `DrawWindowPage.PublishViewport`）。
        var (w, h) = dpW > dpH
            ? (dpW - LandscapeSideChromeDp, dpH - LandscapeChromeHeightDp)
            : (dpW - 16, dpH - chromeHeightDp);          // 竖屏：左右各 8dp 留白 + 底部手柄
        // 下限给足（太小的话程序没法布局）；上限防止异常设备算出离谱值
        return (Math.Clamp(w, 120, 2048), Math.Clamp(h, 120, 4096));
    }

    /// <summary>横屏时**左右两列手柄 + 间距 + 页边距**占掉的宽度（dp）。实测标定，见上。</summary>
    public const int LandscapeSideChromeDp = 518;

    /// <summary>横屏时**状态栏 + 导航栏 + 折叠条**占掉的高度（dp）。实测标定，见上。</summary>
    public const int LandscapeChromeHeightDp = 110;

    // ── 手感：音效 / 震动（540–546）──
    //
    // 手机上的游戏没有音效和震动就是"没有手感"。这一组刻意**全部零素材、零权限**：
    // 音效是**现场合成**的（不是播放音频文件），所以不用打包任何资源、不涉及版权；
    // 震动只要 VIBRATE 这一个 normal 级权限（装上即生效，不用运行时申请）。
    // 完整接口表见 docs/VML宿主接口.md。

    /// <summary>
    /// VM **内置**的 PC 喇叭蜂鸣（`SyscallNumber.SpeakerBeep`）—— 宿主把它**接住**，接到真实音频。
    ///
    /// ## 为什么是"接住它"而不是新增一个 `AUDIO_TONE(540)`
    /// 运行时的 `#57` 本来就有（`R0=频率 R1=时长`），但它的实现是交给 `VmSpeakerDevice` ——
    /// 那个设备**只把样本记进内存/写 WAV 给测试用，不发出任何声音**。手机上跑就是"调了没反应"。
    ///
    /// 于是这里有两个选择：**接通它**，或者**再加一个平行的音效接口**。选前者：
    ///   · VM 的文档/示例/任何语言的前端都已经认这个号，接通一次全都活了；
    ///   · 加一个平行接口就是"同一件事两处实现"（本仓库头号坑），而且以后两边必然行为不一致。
    ///
    /// 宿主处理器**在内置 switch 之前**被调用，所以 `#57` 能在这里被截走 —— 这是该设计允许的用法，
    /// 而不是绕开它。⚠ 但**只截这一个内置号**：`Handles()` 仍然只认 500–599，
    /// 免得把别的内置 syscall 一并吞掉（那是最难查的一类故障）。
    /// </summary>
    public const int VmSpeakerBeep = 57;

    /// <summary>播放音频文件（BGM）：R0=路径*(沙箱相对) R1=循环(0/1) → 0，失败 -1。</summary>
    // ── 绘图增强（534–539，v0.96.176）──
    //
    // 为什么成组加在这里：534–539 是 500–599 号段里**唯一还没被占的一段**（540 起是音频、
    // 550 起是持久化、560 起是输入与屏幕）。号段约定与冲突检查见 docs/VML宿主接口.md。

    /// <summary>`GRADIENT`：定义渐变刷子。R0=id\* R1=类型(0线性/1径向) R2=色A R3=色B
    /// R4..R7=几何（线性 x1 y1 x2 y2；径向 cx cy r）。坐标归一化 0..1。</summary>
    public const int Gradient = 534;

    /// <summary>`DRAW_PATH`：R0=SVG path 的 d\* R1=描边色 R2=线宽 R3=填充色(0=不填) R4=渐变id\*
    /// R5=线帽(0butt/1round/2square) R6=虚线(0/1)。</summary>
    public const int DrawPath = 535;

    /// <summary>`DRAW_POLYGON`：R0=点数组\*（int32 的 x,y 对）R1=点数 R2=填充色 R3=描边色
    /// R4=线宽 R5=渐变id\*。自动闭合。</summary>
    public const int DrawPolygon = 536;

    /// <summary>`DRAW_POLYLINE`：同多边形但不闭合。</summary>
    public const int DrawPolyline = 537;

    /// <summary>`DRAW_RECT_GRAD`：R0..R3=x y w h R4=渐变id\* R5=圆角半径。**渐变填充的矩形**
    /// （渐变按钮/背景这类最常用）。</summary>
    public const int DrawRectGrad = 538;

    /// <summary>`DRAW_CIRCLE_GRAD`：R0..R2=cx cy r R3=渐变id\*。</summary>
    public const int DrawCircleGrad = 539;

    /// <summary>
    /// 多边形/折线的**点数上限**。程序传的是内存里的点数组，点数由它自己给 ——
    /// 不设上限的话，一个写错的大数会让宿主去读几十万个点（每次读还要做越界检查），
    /// 界面直接卡住。512 个点足够画任何真实图形（一张地图轮廓也不过几百个点）。
    /// </summary>
    public const int MaxPolyPoints = 512;

    /// <summary>
    /// 渐变 id 清洗：只留字母数字与 `_ - .`，其余换成 `_`；空/全非法返回空串。
    ///
    /// **为什么必须清洗**：id 会被拼进绘图 DSL 的一个**裸词**位置（`gradient &lt;id&gt; …` 与
    /// 形状的 `@id` 引用），而 id 来自程序内存里的字符串 —— 里面一个空格或引号就能把 DSL
    /// 那一行拆坏（轻则渐变失效，重则整行解析失败、这张图后面的东西全丢）。
    /// 放协议层是为了**能被自测覆盖**（宿主与场景两边都调它，不留第二份实现）。
    /// </summary>
    public static string SafeId(string? id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        var sb = new StringBuilder(id.Length);
        foreach (var c in id)
            sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.' ? c : '_');
        return sb.ToString();
    }

    public const int AudioPlay = 541;
    /// <summary>停掉正在播的音频：→ 0。</summary>
    public const int AudioStop = 542;
    /// <summary>设置整体音量：R0=音量(0–100) → 0（对之后播放的音生效）。</summary>
    public const int AudioVolume = 543;

    /// <summary>震动一下：R0=时长ms R1=强度(0–255，0=用系统默认) → 0。</summary>
    public const int Vibrate = 545;
    /// <summary>按节奏震动：R0=模式*(int 数组) R1=段数 → 0（奇数下标=静、偶数下标=动，同 Android 语义）。</summary>
    public const int VibratePattern = 546;

    // ── 持久化与常亮（550–553）──
    //
    // ⚠ **这一组刻意不含"随机数"与"取时间"** —— VM 已经有了，别再实现一遍：
    //   · `#50` Random / `#51` Seed → `ui_rand` 就是包着它（任何语言都能直接调）
    //   · `#53` GetTick（VM 启动至今毫秒）/ `#54` GetDateTime（unix **秒**）/ `#55`/`#56` 日期时间串
    // 同样是"先 grep 有没有现成的"，只不过这里的"仓"是 VM 的内置 syscall 表。

    /// <summary>写入一条持久化键值：R0=键* R1=值* → 0（键会加 `vml.` 前缀，见 <see cref="StoreKey"/>）。</summary>
    public const int StoreSet = 550;
    /// <summary>读一条：R0=键* R1=缓冲* R2=容量 → 写入长度；没有这个键返回 -1。</summary>
    public const int StoreGet = 551;
    /// <summary>删一条：R0=键* → 0。</summary>
    public const int StoreDel = 552;

    /// <summary>玩游戏时别熄屏：R0=0 关 / 1 开 → 0。</summary>
    public const int ScreenKeepOn = 553;

    // ── 参数钳位与解析（纯逻辑，放这里是为了能被主工程自测覆盖）──

    /// <summary>可听频率下限（Hz）。低于它的震动人耳听不到，还会让某些设备的音频栈行为异常。</summary>
    public const int ToneMinHz = 20;
    /// <summary>可听频率上限（Hz）。</summary>
    public const int ToneMaxHz = 20000;
    /// <summary>单次发声时长上限（ms）—— 再长就不是"音效"而是 BGM 了，用 <see cref="AudioPlay"/>。</summary>
    public const int ToneMaxMs = 5000;
    /// <summary>震动模式最多几段（防呆：模式数组是程序给的，不设上限就是"程序能让手机抖一分钟"）。</summary>
    public const int VibrateMaxSegments = 16;
    /// <summary>单段震动/静默时长上限（ms）。</summary>
    public const int VibrateMaxSegmentMs = 10000;

    /// <summary>
    /// 把 <see cref="AudioTone"/> 的参数钳到安全范围。
    ///
    /// **必须在宿主侧钳，不能让下游自己去防**：传 0 或负数会让合成器算出零/负周期，
    /// 症状是"没声音"甚至"卡住"，而程序那边完全看不出是参数问题
    /// （这类"看起来更周到、实际更糟"的坑本仓库踩过好几次）。
    /// </summary>
    public static (int Hz, int Ms, int Wave, int Volume) ClampTone(int hz, int ms, int wave, int volume)
        => (Math.Clamp(hz, ToneMinHz, ToneMaxHz),
            Math.Clamp(ms, 1, ToneMaxMs),
            Math.Clamp(wave, 0, 3),
            Math.Clamp(volume, 0, 100));

    /// <summary>音量钳位（BGM 与合成音共用一份口径）。</summary>
    public static int ClampVolume(int volume) => Math.Clamp(volume, 0, 100);

    /// <summary>
    /// 持久化键的**加前缀 + 清洗**：空键返回 null（调用方当失败处理）。
    ///
    /// 加 `vml.` 前缀是必须的 —— 这些键和 App 自己的 Preferences（主题、模式、编辑器设置）
    /// 共用一个存储，不隔离就会互相覆盖，而且是"用户改个设置把游戏存档冲了"这种最难查的形态。
    /// 只留字母/数字/`._-`：Preferences 的键最终落到平台存储（Android 是 XML），
    /// 塞进奇怪字符出问题时**报错在平台层**，根本看不出是谁写的。
    /// </summary>
    public static string? StoreKey(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var sb = new StringBuilder("vml.");
        foreach (var ch in raw.Trim())
        {
            if (char.IsLetterOrDigit(ch) || ch is '.' or '_' or '-') sb.Append(ch);
            else if (ch == ' ') sb.Append('_');
            // 其余字符直接丢：键是程序自己起的，丢了也不会歧义（不像值那样影响语义）
        }
        return sb.Length > 4 ? sb.ToString() : null;   // 只有前缀 = 空键
    }

    /// <summary>
    /// 把程序给的震动模式（int 数组）钳成平台能接受的毫秒序列。
    /// 段数截到 <see cref="VibrateMaxSegments"/>、每段截到 <see cref="VibrateMaxSegmentMs"/>。
    /// </summary>
    public static long[] ClampVibratePattern(IReadOnlyList<int> segments)
    {
        var n = Math.Min(segments.Count, VibrateMaxSegments);
        var result = new long[n];
        for (var i = 0; i < n; i++)
            result[i] = Math.Clamp((long)segments[i], 0, VibrateMaxSegmentMs);
        return result;
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
    /// <summary>
    /// 屏幕方向变化；A = 新方向（0 竖屏 / 1 横屏），B = 0（预留）。
    ///
    /// **和 <see cref="WindowResize"/> 一样是消息，不是让程序自己去猜** ——
    /// 一条"屏幕变了"的事实，程序收到才知道要重排版。
    /// ⚠ 宿主**先发方向、后发尺寸**（同一个 tick 里）：这样程序在收到尺寸那条时，
    /// 已经知道新的方向了，不必再插一次查询、也不会用旧方向配新尺寸排一次版。
    /// </summary>
    WindowOrient = 12,
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

    /// <summary>非阻塞读一条；无消息返回 null。</summary>
    /// <param name="keep">
    /// true = **只看队头，不取走**（下一次还是它）；false = 取走（默认）。
    /// 两个模式共用这一处实现 —— 分成 `TryTake`/`TryPeek` 两份的话，
    /// 信号量那套"投递—唤醒"迟早只修一边。
    /// </param>
    public VmlMessage? TryRead(bool keep)
    {
        lock (_lock)
        {
            if (_queue.Count == 0) return null;
            return keep ? _queue.Peek() : _queue.Dequeue();
        }
    }

    /// <summary>非阻塞取一条（消费）；无消息返回 null。</summary>
    public VmlMessage? TryTake() => TryRead(keep: false);

    /// <summary>
    /// 阻塞读一条，最多等 <paramref name="timeoutMs"/> 毫秒（0 = 无限等）。
    /// 超时返回 null。**阻塞方是 VM 线程**，不要从 UI 线程调。
    /// <paramref name="keep"/> 见 <see cref="TryRead"/>。
    /// </summary>
    public VmlMessage? Read(int timeoutMs, bool keep)
    {
        // 先看队列：有就直接拿走，不走信号量（信号量的计数与队列长度不是一对一的 ——
        // 连投两条只 Release 一次，靠信号量判断会漏消息）
        if (TryRead(keep) is { } first) return first;

        var waited = timeoutMs <= 0
            ? _signal.Wait(Timeout.Infinite)
            : _signal.Wait(timeoutMs);
        if (!waited) return null;
        return TryRead(keep);
    }

    /// <summary>阻塞取一条（消费），最多等 <paramref name="timeoutMs"/> 毫秒。超时返回 null。</summary>
    public VmlMessage? Take(int timeoutMs) => Read(timeoutMs, keep: false);

    /// <summary>清空（每次 VML 运行开始前调用，避免上一轮的消息串到这一轮）。</summary>
    public void Clear()
    {
        lock (_lock) _queue.Clear();
        while (_signal.Wait(0)) { /* 把信号量计数也归零 */ }
    }
}

/// <summary>
/// 窗口对"屏幕旋转"的声明 —— **三种**，因为"老程序"必须能和"声明了两者之一的程序"分开。
///
/// 为什么不能只有两态：宿主**改了场景的坐标空间**之后，不处理 `WINDOWRESIZE` 的程序
/// 会继续按老坐标画，而空间变小了 ⇒ 内容被裁掉一大截（比原来的"等比缩小"更糟）。
/// 所以那条能力只能给**明确声明过"我会重排版"**的程序；
/// 老接口（`WIN_OPEN` #520）一个字的声明都没有，就得保持它原来的样子。
/// </summary>
public enum WindowRotation
{
    /// <summary>
    /// 老窗口（`ui_win_open`）：**跟随旋转，但坐标系不动** —— 宿主把整份场景等比缩放着显示，
    /// 内容完整但会变小。这是 v0.96.230 之前对所有程序的行为，**保持一字不改**。
    /// </summary>
    Legacy = 0,
    /// <summary>
    /// `VML_WIN_ROTATABLE`：程序自己会按新尺寸重排版 ⇒ 视口一变，宿主就把
    /// **新的坐标空间**（可用绘图区）整个给到场景，并先发 `WINDOWORIENT` 再发 `WINDOWRESIZE`。
    /// </summary>
    Follow = 1,
    /// <summary>
    /// `VML_WIN_PORTRAIT`：程序**只写了竖屏一种排版** ⇒ 宿主把屏幕锁在竖屏，怎么转都不动。
    /// 锁比"跟着转再缩放"省事，也不会让程序遇到它没写过的形状。
    /// </summary>
    PortraitOnly = 2,
    /// <summary>`VML_WIN_LANDSCAPE`：只支持横屏（赛车 / 横版过关），锁在横屏。</summary>
    LandscapeOnly = 3,
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
/// <summary>
/// 一个 VML 绘图窗口的场景（保留模式）。
/// </summary>
public sealed class VmlScene
{
    private readonly List<string> _figures = new();

    public string Title { get; set; } = "VML";
    public int Width { get; set; } = 320;
    public int Height { get; set; } = 240;

    /// <summary>转屏声明（`WIN_OPEN_EX` 的 R3；不声明就是 <see cref="WindowRotation.Legacy"/>）。</summary>
    public WindowRotation Rotation { get; set; } = WindowRotation.Legacy;

    /// <summary>是否需要屏幕手柄区（`WIN_OPEN_EX` 的 R4；默认 true = 今天的行为）。</summary>
    public bool NeedGamepad { get; set; } = true;

    /// <summary>
    /// 换一块坐标空间（转屏 / 收起手柄之后宿主调用）。
    ///
    /// **两个数一起换**：`BuildDsl()` 会把 `canvas W H` 写进这一帧的 DSL，
    /// 而它可能在 VM 线程上被 `Present()` 调用（拍快照）—— 分开赋值就有机会被读到
    /// "新宽 + 旧高"的中间态，那一帧整幅会被拉伸。所以读写都在 `_figures` 这把锁里成对做。
    /// </summary>
    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0) return;
        lock (_figures)
        {
            Width = width;
            Height = height;
        }
    }

    /// <summary>背景色（0xAARRGGBB）。</summary>
    public uint Background { get; set; } = 0xFF000000;

    /// <summary>用户是否已请求关闭窗口（点了返回箭头）。程序据此退出主循环。</summary>
    public bool Closed { get; set; }

    /// <summary>图元条数（宿主用它判断要不要重绘）。</summary>
    public int FigureCount { get { lock (_figures) return _figures.Count; } }

    /// <summary>每次内容变化 +1 —— 宿主缓存渲染结果时比对它，避免无变化也重编码 PNG。</summary>
    public int Version { get; private set; }

    /// <summary>
    /// **「一帧画完了」标记** —— 只由 <see cref="Present"/>（= VML 的 `ui_present()`）递增。
    ///
    /// ## 为什么必须有它（v0.96.178）
    ///
    /// 窗口原来按 <see cref="Version"/> 出图，而那个数**每个图元都 +1**：一帧 200 个图元就是
    /// 200 次"变了"。定时器（40ms）撞上哪一次就把**当时那一刻**的场景贴上去 —— 而那多半是
    /// **画到一半的画面**（`ui_clear()` 刚清完、棋子还没画）。用户看到的就是
    /// 「俄罗斯方块有时抖动闪烁」：棋盘一闪一闪地清空又出现。
    ///
    /// 判据应当与程序对齐：`ui_present()` 就是"这一帧画完了"（`waycoder_ui.h` 的用法示例里
    /// 每帧末尾都调它，两个游戏也都调）。**"内容变了"和"一帧画完了"是两件事**，
    /// 拿前者当后者用就会贴出半成品。
    /// </summary>
    public int PresentVersion { get; private set; }

    /// <summary>程序调过 `ui_present()` 没有 —— 没调过的老程序由宿主退到"静下来了再出图"。</summary>
    public bool EverPresented => PresentVersion > 0;

    /// <summary>一帧画完了（VML `ui_present()`）。</summary>
    public void Present()
    {
        // **当场把这一帧拍下来**（v0.96.178）：宿主收到 present 之后，未必立刻渲染 ——
        // 页面是"定时器醒来才发现有 present"（最多隔 40ms），而这段时间里程序早已开始
        // 画下一帧（先 `ui_clear()` 再重画）。若等到那时才 `BuildDsl()`，拍到的是**半成品**：
        // 真机实测日志里图元数 17/28/30/36/60 参差不齐，就是这个竞态。
        // 在 present 这一刻拍，拍到的必然是程序刚画完的那一帧；之后隔多久渲染都不影响。
        PresentedDsl = BuildDsl();
        PresentVersion++;
        Version++;
    }

    /// <summary>最近一次 `ui_present()` 时**当场拍下**的那一帧（DSL 文本）。</summary>
    public string? PresentedDsl { get; private set; }

    /// <summary>取走那一帧快照（取走即清 —— 同一帧只该被渲染一次）。</summary>
    public string? TakePresentedDsl()
    {
        var d = PresentedDsl;
        PresentedDsl = null;
        return d;
    }

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

    /// <summary>矩形；<paramref name="radius"/> &gt; 0 时走 DSL 的 roundrect（圆角矩形）。
    /// <paramref name="fillGradient"/> 非空时用**渐变刷子**填充（DSL 的 `@id` 引用）。</summary>
    public void AddRect(int x, int y, int w, int h, uint color, bool filled, int width, int radius,
        string? fillGradient = null)
    {
        var name = radius > 0 ? "roundrect" : "rect";
        var extra = radius > 0 ? $" {radius}" : "";
        Add($"{name} {x} {y} {w} {h}{extra}{Style(color, filled, width, fillGradient)}");
    }

    public void AddCircle(int cx, int cy, int r, uint color, bool filled, int width, string? fillGradient = null)
        => Add($"circle {cx} {cy} {r}{Style(color, filled, width, fillGradient)}");

    public void AddEllipse(int cx, int cy, int rx, int ry, uint color, bool filled, int width, string? fillGradient = null)
        => Add($"ellipse {cx} {cy} {rx} {ry}{Style(color, filled, width, fillGradient)}");

    /// <summary>
    /// **渐变刷子**定义（v0.96.176）。形状用 <c>fillGradient</c> 参数按 <paramref name="id"/> 引用。
    ///
    /// 几何坐标是**归一化的 0..1**（SVG `objectBoundingBox` 约定，与 DSL 一致）：
    ///   · 线性：<paramref name="a1"/>..<paramref name="a4"/> = x1 y1 x2 y2（起点→终点）
    ///   · 径向：<paramref name="a1"/>..<paramref name="a3"/> = cx cy r
    /// 不传就用默认（线性从左到右、径向居中）。
    ///
    /// 之所以坐标归一化而不是绝对像素：同一个"左上到右下"的渐变套在按钮和套在整屏上
    /// 写法一样，程序不必为每个尺寸重算 —— 这也是 SVG 选这个约定的原因。
    /// </summary>
    public void AddGradient(string id, bool radial, uint colorA, uint colorB,
        int a1 = 0, int a2 = 0, int a3 = 1000, int a4 = 0)
    {
        var safe = VmlUi.SafeId(id);
        if (safe.Length == 0) return;
        // ⚠ **单位只在这一处换算**：对外（C# API 与 VML 的 syscall）统一用**千分之一**的整数
        //   0..1000，进 DSL 前除以 1000 变成 SVG 的归一化 0..1。
        //   两端各写一次换算就会出现"我按千分之一传、它按 0..1 收"这种**沉默的错**：
        //   方向变成 (0,0)→(1000,0)，t 恒等于约 0 ⇒ 整块只剩 ColorA（实测踩过，
        //   现象是"渐变完全不生效、颜色是纯色"，而 DSL 与解析全都正常，很难看出来）。
        //   默认值是 x1=0 y1=0 x2=1000 y2=0：线性从左到右（径向则是 cx=cy=500 r=0 由调用方给）。
        var geo = radial
            ? $" {N(a1)} {N(a2)} {N(a3)}"
            : $" {N(a1)} {N(a2)} {N(a3)} {N(a4)}";
        Add($"gradient {safe} {(radial ? "radial" : "linear")} {Hex(colorA)} {Hex(colorB)}{geo}");
    }

    /// <summary>千分之一 → 归一化（渐变几何专用，唯一一处换算）。</summary>
    private static string N(int perMille)
        => (Math.Clamp(perMille, -1000, 1000) / 1000.0).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// 路径（v0.96.176）。<paramref name="d"/> 是 **SVG path 语法**
    /// （`M/m L/l H/h V/v C/c S/s Q/q T/t A/a Z/z`），光栅化那侧现在会展平曲线
    /// （见 <c>Infra/DrawPath.cs</c>），与导出 SVG 形状一致。
    ///
    /// <paramref name="fillColor"/> 与 <paramref name="fillGradient"/> 都不给就是**只描边**
    /// （与老写法一致）。给了渐变就用渐变填，忽略 <paramref name="fillColor"/>。
    /// </summary>
    public void AddPath(string d, uint strokeColor, double width = 1, int cap = 0,
        uint fillColor = 0, bool fillSet = false, string? fillGradient = null, bool dashed = false)
    {
        if (string.IsNullOrWhiteSpace(d)) return;
        var sb = new StringBuilder();
        sb.Append("path \"").Append(d.Replace("\"", " ")).Append('"');
        sb.Append(' ').Append(Hex(strokeColor));
        if (width > 0) sb.Append(' ').Append(Num(width));
        sb.Append(" ").Append(CapName(cap));
        if (dashed) sb.Append(" dash");
        if (!string.IsNullOrEmpty(fillGradient)) sb.Append(" fill @").Append(VmlUi.SafeId(fillGradient));
        else if (fillSet) sb.Append(" fill ").Append(Hex(fillColor));
        Add(sb.ToString());
    }

    /// <summary>
    /// 多边形（v0.96.176）。<paramref name="points"/> 是**扁平坐标数组** `x0,y0,x1,y1,…`。
    /// 自动闭合（多边形）—— 不闭合的用 <see cref="AddPolyline"/>。
    /// </summary>
    public void AddPolygon(IReadOnlyList<double> points, uint color, bool filled, int width,
        string? fillGradient = null, bool dashed = false)
    {
        var pts = FlatPoints(points);
        if (pts == null) return;
        Add($"polygon {pts}{Style(color, filled, width, fillGradient)}{(dashed ? " dash" : "")}");
    }

    /// <summary>折线（不闭合）。坐标同上。</summary>
    public void AddPolyline(IReadOnlyList<double> points, uint color, int width,
        string? fillGradient = null, bool dashed = false)
    {
        var pts = FlatPoints(points);
        if (pts == null) return;
        Add($"polyline {pts}{Style(color, filled: false, width, fillGradient)}{(dashed ? " dash" : "")}");
    }

    /// <summary>扁平坐标数组 → DSL 的 `x,y x,y …` 串；点数不足 2 个返回 null（画不出东西）。</summary>
    private static string? FlatPoints(IReadOnlyList<double> points)
    {
        if (points == null || points.Count < 4) return null;
        var n = points.Count / 2 * 2;
        var sb = new StringBuilder();
        for (var i = 0; i < n; i += 2)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(Num(points[i])).Append(',').Append(Num(points[i + 1]));
        }
        return sb.ToString();
    }

    /// <summary>数字格式化：**固定小点、不进科学计数法**（DSL 的分词器不认 `1E-05` 这种写法）。</summary>
    private static string Num(double v)
        => double.IsFinite(v) ? v.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) : "0";

    private static string CapName(int cap) => cap switch { 1 => "round", 2 => "square", _ => "butt" };

    /// <summary>
    /// 文字；<paramref name="anchor"/> 0=左 1=中 2=右，<paramref name="style"/> 见 <see cref="TextBold"/>/<see cref="TextItalic"/>。
    ///
    /// 文本按 **UTF-8** 从程序内存读出（宿主侧 <c>Str()</c> 就是 UTF-8 解码），
    /// 这里只做两件事：转义成 DSL 字符串、把样式位翻成 DSL 关键字（`bold`/`italic`/`bi`）。
    /// </summary>
    public void AddText(int x, int y, string text, uint color, int fontSize, int anchor, int style = 0)
    {
        var w = (style & TextBold) != 0 ? "bold" : "";
        var i = (style & TextItalic) != 0 ? "italic" : "";
        var bi = (w.Length > 0 && i.Length > 0) ? " bi" : (w.Length > 0 ? " bold" : (i.Length > 0 ? " italic" : ""));
        Add($"text {x} {y} \"{Escape(text)}\" {fontSize} {Hex(color)} {AnchorName(anchor)}{bi}");
    }

    /// <summary>当前文字属性（<see cref="SetFont"/> 设、<see cref="Text"/> 用）。宿主侧状态，不占 VML 内存。</summary>
    public int FontSize { get; set; } = 16;
    /// <summary>当前文字样式位（见 <see cref="TextBold"/>/<see cref="TextItalic"/>）。</summary>
    public int FontStyle { get; set; }
    /// <summary>当前文字颜色（0xAARRGGBB）。</summary>
    public uint FontColor { get; set; } = 0xFFFFFFFF;
    /// <summary>当前文字锚点（0=左 1=中 2=右）。</summary>
    public int FontAnchor { get; set; }

    /// <summary>按当前属性画一行字（<see cref="Text"/> 号段的实现体，放这里便于自测）。</summary>
    public void AddTextCurrent(int x, int y, string text)
        => AddText(x, y, text, FontColor, FontSize, FontAnchor, FontStyle);

    /// <summary>文字样式位：粗体。</summary>
    public const int TextBold = 1;
    /// <summary>文字样式位：斜体。</summary>
    public const int TextItalic = 2;

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

        // **必须开抗锯齿。** 光栅器本身支持（`DrawDocument.Antialias` → 3× 超采样再盒式降采样），
        // 但**默认是关的**，得由 DSL 显式打开。不开的后果全在"斜的、圆的、细的"东西上：
        // 圆角矩形的四个角是锯齿、斜线是台阶、文字笔画边缘发毛 —— 画棋盘这种满屏圆角+斜线的
        // 场景一眼就能看出来（用户报的"圆角需要做平滑处理"就是它）。
        // ⚠ **尺寸与图元在同一把锁里取**：换尺寸走 `Resize()`（也在这把锁里改），
        // 分开的话会被 VM 线程上的 `Present()` 拍到"新宽 + 旧高"，那一帧整幅被拉伸。
        lock (_figures)
        {
            sb.Append("canvas ").Append(Width).Append(' ').Append(Height).Append(' ')
              .Append(Hex(Background)).Append('\n');
            sb.Append("antialias\n");
            foreach (var f in _figures)
                sb.Append(f).Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>
    /// 形状样式串。DSL 的规则是「**第一个颜色 = 填充，第二个 = 描边**，裸数字 = 线宽」
    /// （见 <c>DrawCommands.ParseStyle</c>）。所以空心图形要把填充显式写成全透明色 ——
    /// 这里传 <c>#00000000</c> 而不是省略，否则颜色位会被描边占用、变成"填充了描边的颜色"。
    /// </summary>
    private static string Style(uint color, bool filled, int width, string? fillGradient = null)
    {
        var w = width > 0 ? $" {width}" : "";
        // 渐变填充：把 DSL 的 `@id` 放在**填充位**（DSL 规定"第一个颜色 = 填充，第二个 = 描边"，
        // 渐变引用与颜色占同一个位置，见 DrawParse.TryParseStyle）。
        if (!string.IsNullOrEmpty(fillGradient))
            return $" @{VmlUi.SafeId(fillGradient)} {Hex(color)}{w}";
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
