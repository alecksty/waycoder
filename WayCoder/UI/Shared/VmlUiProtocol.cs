using System.Globalization;
using System.Text;
using WayCoder.Infra;   // 蒙版形状/表达式（DrawMask.cs）—— 运行时镜像与 `ui_mask_test` 要用

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
    /// <summary>
    /// 开**电脑屏窗口**：R0=标题* R1=宽 R2=高 R3=方向声明 R4=要屏幕键盘 → 句柄，失败 -1。
    ///
    /// <para>
    /// 给**老程序**（DOS / 早期 PC 那种"固定分辨率 + 键盘 + 鼠标"的程序）用的第三种窗口。
    /// 与 <see cref="WinOpenEx"/> 的差别是**三件**：
    /// </para>
    ///
    /// <list type="number">
    /// <item>**坐标系固定**为程序声明的 `w×h`，**永不**跟随旋转重排 ——
    ///   老程序按固定分辨率排的版，换空间就会画到框外；</item>
    /// <item>触摸**只发鼠标消息**（`MouseDown/Move/Up`），**不发** `TouchDown/Move/Up` ——
    ///   老程序处理的是鼠标，收到触摸事件反而会乱；</item>
    /// <item>带**屏幕键盘**（PC 布局）而不是手柄区。</item>
    /// </list>
    ///
    /// <para>
    /// ⚠ **为什么不给 <see cref="WinOpenEx"/> 的 R3 再加一档**：R3 是**转屏声明**，
    /// 加一档就是给同一只寄存器第二语义 —— "一个值两个含义"正是本仓反复踩的那类；
    /// 而且三档窗口的**实现根本不在同一层**（图形 = 开页 + 场景；电脑屏 = 固定坐标系 +
    /// 不同输入 + 键盘）。新能力一律走新号。
    /// </para>
    ///
    /// <para>
    /// ⚠ **命名里的 `Pc` 是"电脑屏"，与 PC 喇叭（`#57`）无关。**
    /// </para>
    ///
    /// ## R3 方向声明（语义与 <see cref="WinOpenEx"/> 同）
    /// 只是这里**没有 Rotatable 那一档** —— 电脑屏的坐标系**固定**为程序声明的 `w×h`，
    /// 永远不重排。所以：
    /// · `0` / `VML_WIN_PORTRAIT` = 锁在竖屏；· `2` / `VML_WIN_LANDSCAPE` = 锁在横屏；
    /// · `1` / 其它 = **不锁**（跟着设备转，但**坐标系不动**，宿主等比缩放着显示）。
    ///
    /// ## R4 要屏幕键盘（<see cref="NeedKeyboard"/> / <see cref="NoKeyboard"/>）
    /// 电脑屏的输入是**键盘 + 鼠标**，所以这一位管的是**屏幕键盘**而不是手柄 ——
    /// 位置与 <see cref="WinOpenEx"/> 的 R4 对称（那里是手柄）。默认要（=1）。
    /// 只做鼠标交互的程序可以关掉它，画布就吃满整屏。
    ///
    /// ⚠ **参数布局与 <see cref="WinOpenEx"/> 逐位对齐，但 R3/R4 的含义各自定义** ——
    /// 宿主**分开读**（老号不读 R3/R4，见那边的说明），不是同一套 switch。
    /// </summary>
    public const int WinOpenPc = 582;

    // ── 像素读回（583–585）────────────────────────────────────────────────
    //
    // 老 graphics.h 程序要做**填充**与**精灵**就绕不开读像素：
    //   · `floodfill(x, y, border)` —— 从种子点灌色，碰到边界色停；
    //   · `getimage` / `putimage`   —— 存一块画面 / 贴回去（精灵保存-恢复）。
    // 而场景是**保留模式**的（只有图元、没有像素缓冲），所以这三个号在宿主侧
    // 都要先**光栅化一次**才能回答"这个像素是什么颜色"。
    //
    // ⚠ **`FloodFill` 的返回值不是像素数，是"落了多少条游程"** —— 消费者是场景，
    //   而场景只有图元：逐像素输出意味着一次填充塞几十万个 `ui_pixel`。
    //   扫描线填充天生按行产出段，**一段 = 一个矩形**（见 `FloodFill.Runs`）。

    /// <summary>`FLOOD_FILL`：R0=x R1=y R2=填充色(0xAARRGGBB) R3=边界色 → R0=落了几个矩形（0=没填）。</summary>
    public const int FloodFill = 583;

    /// <summary>
    /// 读一个像素的颜色：R0=x R1=y → R0=**0xRRGGBB**（越界或光栅化失败返回 -1）。
    ///
    /// <para>
    /// <b>为什么要有它</b>：老 BASIC 游戏的精灵动画靠 <c>PUT …, XOR</c> 擦除 ——
    /// 异或要**先读目的像素**。宿主原先只有"写像素"（<see cref="DrawPixel"/>）
    /// 与"整块句柄"（<see cref="GetImage"/>）两条路，逐像素的异或做不出来。
    /// </para>
    ///
    /// <para>
    /// ⚠ <b>每次调用要光栅化一次</b>（场景是保留模式的，没有像素缓冲，与
    /// <see cref="GetImage"/> 同源）⇒ **别放进每帧的密集大循环**。精灵只有几十个像素，
    /// 老程序那种"每帧两次 PUT"的量级没问题；真要做像素级碰撞检测要另想办法。
    /// </para>
    ///
    /// <para>
    /// ⚠ 返回的是 **RGB 不是调色板索引** —— 与 CALLJSON 的 <c>pixel</c> 同一个实现
    /// （见 <c>VmlHostRuntime.ReadPixelRgb</c>），"RGB 反查索引"那一步归各自的垫层。
    /// </para>
    /// </summary>
    public const int DrawGetPixel = 587;

    /// <summary>
    /// **主动截屏**：R0=相对路径* → R0=**写入的字节数**，失败 -1。
    ///
    /// 把**画布**（`VmlScene` 里那些图元）光栅化成 PNG，写进
    /// <see cref="SanitizeShotPath"/> 清洗过的相对路径（空串 = 默认名 `shot.png`；无扩展名补 `.png`）。
    /// 失败码一律 -1，三种原因**不区分**：路径非法 / 没有画布 / 写不进去
    /// —— 程序该做的事都一样（提示一句、继续跑）。
    ///
    /// ⚠ **只有一个参数**：不设"回写实际路径"的缓冲区。程序最自然的写法是
    /// `ui_screenshot("shot.png")`，而字面量在 `.data` 里与别的字符串共享，
    /// 往里写就是静默破坏别的字面量。规范化的规则是确定性的（空→`shot.png`、
    /// 无扩展名→补 `.png`），程序自己算得出来。
    ///
    /// ## 为什么截的是**画布**而不是"整个 App 窗口"
    ///
    /// 走过一版"截窗口"（Android `DecorView.Draw`、Apple `CALayer`、Windows
    /// `RenderTargetBitmap`），**真机上被否掉了**：`View.Draw` 是软件绘制，
    /// 而 MAUI 的 `GraphicsView` 内容在硬件加速的 RenderNode 上 ⇒ 截出来
    /// **标题栏正常、画布整块纯黑**（程序画的颜色一个像素都没有）。
    ///
    /// 换成光栅化画布之后：四端**逐字同一份代码**（`DrawRunner` 在共享 `Infra/`），
    /// 没有平台分支、没有权限、也不可能"某一端画不出来"。
    /// 代价是只有画面本身（不含屏幕手柄与标题栏）—— 那正是要的：
    /// 当战绩图/分享图用，干净的画面比带一圈 UI 更好。
    ///
    /// **与 <see cref="GetImage"/>(#584)、<see cref="DrawGetPixel"/>(#587) 的分工**：
    /// 那两个是给**程序自己**在运行中读像素用的（句柄 / 单点）；
    /// 本号是"存一张图给用户看"，落盘、返回长度。
    /// </summary>
    public const int Screenshot = 588;

    // ── 矢量图块 589–592 ────────────────────────────────────────────────────
    //
    // 把一串绘图指令**录制成图块**，之后带变换反复贴。与像素那层
    // （`GetImage`/`PutImage`）是**两条并存的路**：那层存光栅像素（老程序用），
    // 这层存指令 —— 放大不糊、旋转免费、内存与图块尺寸无关。

    /// <summary>
    /// **图块的五个操作共用一个号** —— `R0 = op`（见 <see cref="BlockOp"/>），
    /// `R1..R6 = 参数`，返回写回 `R0`。
    ///
    /// <para>
    /// ⚠ v0.96.481 起合并（原先 `CreateBlock`/`EndBlock`/`DrawBlock`/`DrawBlockAt`/`FreeBlock`
    /// **各占一个号 589–593**）。它们本来就是**同一件事的五种动作** ——
    /// 正是"相同或相似功能合并一个号、用第一个参数区别"这条规则的典型，一次省下 4 个号。
    /// </para>
    ///
    /// <para>
    /// ⚠ **每个 op 的参数都 ≤6，加上 op 自己只占 7 个槽**，卡在 `R0–R7` 的容纳上限内。
    /// `Draw`/`DrawAt` 是最满的（6 个参数）；**再给它们加一个参数就塞不下了** ——
    /// 那时只能开新号或走内存缓冲，别再往这个 op 上加。
    /// </para>
    ///
    /// <para>各 op 的语义与返回（**与合并前逐字一致**）：</para>
    /// <list type="bullet">
    /// <item>`Create(w,h,color)` → 句柄（≥1），失败 0。**`color` 是保留位**：图块永远透明叠加，
    ///   传什么都没有区别。`ui_clear` 在录制期 = **从零开始录**（只清录制缓冲，不动场景）。</item>
    /// <item>`End()` → 句柄（与 create 给的一致）；没在录时 0。</item>
    /// <item>`Draw(block,x,y,sx,sy,rot)` → 1 成功 / 0 失败。缩放千分比（1000 = 原尺寸）、角度用度。
    ///   **(x,y) 是中心、绕中心转**。</item>
    /// <item>`DrawAt(block,x,y,sx,sy,rot)` → 同上，但 **(x,y) 是左上角、绕左上角转**。</item>
    /// <item>`Free(block)` → 1 成功 / 0 失败（句柄不存在或已释放）。**先释放旧的、再录新的**，
    ///   是"内容会变"那种用法的标准写法 —— 块表有 <see cref="MaxBlocks"/>（128）上限，而"改一个块"
    ///   只能重录（块的内容与尺寸都不可变），不释放就是**每次漏一个句柄**。
    ///   实测 `Examples/basic/gorilla_pro.bas`：城市/星空/云三块每次配色变化重录 ⇒ **每秒 3 个**
    ///   ⇒ 43 秒撑满，之后 `ui_create_block` 一律返回 0、画面**悄悄**退回逐帧画
    ///   （帧率 25 → 18，屏幕上完全看不出"图块没了"）。
    ///   释放过的句柄会被**回收复用**，所以别在别处留旧句柄当"以后再贴"；
    ///   贴一个已失效的句柄是**静默 no-op**（与"句柄 0 = 没有块"同一套）。</item>
    /// </list>
    ///
    /// <para>
    /// ⚠ **认不出的 op 返回 -1 且什么都不做** —— **不是 0**。因为 0 在这套里是**合法的失败码**
    /// （`Create` 失败就返回 0），拿 0 当"认不出"就分不清"参数错了"和"op 打错了"。
    /// （`ui_gfx`(#595) 那边用 0 是因为它的每个 op 本来就不返回业务值；两处约定不同，各有各的理由。）
    /// </para>
    /// </summary>
    public const int Block = 589;

    /// <summary>
    /// <see cref="Block"/>(#589) 的**操作码** —— **跨语言契约**
    /// （C 头文件里的 `VML_BLOCK_*` 按这些数值写死）。
    ///
    /// ⚠ **只能末尾追加**：数值会编进程序的机器码里，改值等于改 ABI。
    /// </summary>
    public static class BlockOp
    {
        /// <summary>开始录制 → 句柄。参数：`w, h, color`（color 是保留位）。</summary>
        public const int Create = 0;
        /// <summary>结束录制 → 句柄。无参数。</summary>
        public const int End = 1;
        /// <summary>贴块（**(x,y) 是中心**）。参数：`block, x, y, sx, sy, rot`。</summary>
        public const int Draw = 2;
        /// <summary>贴块（**(x,y) 是左上角**）。参数：`block, x, y, sx, sy, rot`。</summary>
        public const int DrawAt = 3;
        /// <summary>释放块。参数：`block`。</summary>
        public const int Free = 4;
    }

    // ⚠ `EndBlock(590)` / `DrawBlock(591)` / `DrawBlockAt(592)` / `FreeBlock(593)` 四个号
    //   已在 v0.96.481 **并入上面的 `Block`(589) + `BlockOp`**，四个值随即空出。

    /// <summary>
    /// `ui_free_image(handle)` —— **手动释放**一张 `ui_get_image` 存下的图像。→ 1 成功 / 0 失败。
    ///
    /// <para>
    /// 图像表是**宿主级**的（不是场景级），所以它不会随场景释放 —— 从前只能一路膨胀到
    /// `MaxImages` 为止。程序退出时由 `VmlHostRuntime.Reset()` 统一清掉（用户定的：
    /// 「ui 绘图资源都要做成可以手动释放的，如果退出程序，也能自动释放」）。
    /// </para>
    /// </summary>
    public const int FreeImage = 594;

    /// <summary>
    /// **绘图状态**（图层 / 裁剪 / 蒙版 / 透明度）—— `ui_gfx(op, a, b, c, d)`。
    ///
    /// ⚠ **用一个号做多路复用，而不是每个操作占一个号**：宿主认的号段是
    ///   `500–599`（见 <see cref="Handles"/>），而这套结构到 594 已经**只剩 595–599 五个**，
    ///   状态操作却有八九个。放宽 `Handles` 到 600+ 是不行的 —— 那条注释记着
    ///   「放宽会把别的内置 syscall 一并吞掉，那是最难查的一类故障」。
    ///   多路复用在本题材上也更贴切：它们本来就是**同一个状态机**的几种动作。
    ///
    /// **已实现的 op 全在 <see cref="GfxOp"/> 里**（0–16：裁剪压弹 / 透明度 /
    /// 蒙版三种与布尔运算 / 图层 / 画刷重置 / 裁剪栈清空 / 资源计数 / 蒙版导出路径）——
    /// 都接好了，C 头文件里也都有声明。
    ///
    /// ⚠ 这里原先写着「目前实现三个操作，`3..7`（蒙版/图层）留号未实现」——
    /// **那句是过期的**（v0.96.481 逐条核实：`VmlHostRuntime` 的 `GfxState` 里
    /// `MaskBegin`/`MaskEnd`/`MaskEnd2`/`MaskClear`/`LayerBegin`/`LayerEnd` 六个 case 都在）。
    /// 留着的害处是让人以为"蒙版/图层不能用"，从而绕开本职工具自己造一套。
    /// </summary>
    public const int GfxState = 595;

    /// <summary>`ui_gfx` 的操作码。见 <see cref="GfxState"/>。</summary>
    public static class GfxOp
    {
        public const int ClipPush = 0;
        public const int ClipPop = 1;
        public const int Alpha = 2;

        /// <summary>
        /// 释放**本窗口累积的全部画刷 / 渐变定义**。
        ///
        /// ⚠ 画刷的句柄就是 `_brushes` 的**下标**（`handle = Count`），所以没法像
        ///   图像/图块那样"按句柄删一个"——删中间那个会让后面的句柄全部错位。
        ///   于是这里给的是**整体重置**：每帧开头调一次，就能让"按需造渐变的程序"
        ///   不随帧数累积。**图像与图块不受影响**（它们有各自的句柄回收，见 593/594）。
        ///
        /// ⚠ 重置之后，之前发出的 `@名字` 引用会**解析不到**，按既有约定退化成纯色
        ///   （见 `Parse` 里"悬空引用退化为纯色"那一段）—— 不会崩，只是变成纯色。
        /// </summary>
        public const int BrushReset = 8;

        /// <summary>
        /// 清空**整个裁剪栈**（不是弹一级）。
        ///
        /// 给"出错恢复"用：程序中途 `return` / 走了别的分支，压进去的那几级没人弹，
        /// 后面的东西就全画不出来，而且**看不出原因**。有了它，一进主循环（或按 Esc）
        /// 调一次就能回到干净的整屏状态。与 `ui_alpha(255)` 配成一对"复位"。
        /// </summary>
        public const int ClipReset = 9;

        /// <summary>开始收集蒙版形状（`ui_mask_begin`）。这期间画的形状**不上屏**。</summary>
        public const int MaskBegin = 3;
        /// <summary>结束收集并启用蒙版（`ui_mask_end`，`a` = 1 只在里面画 / 0 只在外面画）。</summary>
        public const int MaskEnd = 4;
        /// <summary>取消蒙版（`ui_mask_clear`）——实现是"开一个空的再收"。见 `AddMaskClear`。</summary>
        public const int MaskClear = 5;

        /// <summary>
        /// 结束收集，并把新形状与**当前蒙版**按布尔运算符组合（`ui_mask_end2(op)`，`a` = 运算符）。
        ///
        /// 运算符见 <see cref="MaskOp"/>：`Replace`(0，= 老 `ui_mask_end` 的行为) / `Union` /
        /// `Intersect` / `Subtract` / `Xor`。
        ///
        /// ⚠ **为什么不给 `ui_mask_end(inside)` 加个参数**：宿主是从 `registers[n]` 读参数的，
        ///   而只传一个参数的老程序后面那几只寄存器里是**它自己上一句留下的值**
        ///   （可能是个指针、可能是个计数）—— 宿主无从判断"这是不是真给了"。
        ///   所以照铁律新开一个操作码，老号语义一个字不动（与 `WIN_OPEN_EX` #570 同一处置）。
        ///
        /// ⚠ 它**不碰 `inside`**（保持当前值，默认 1）：`inside` 是"最后要不要整体取反"，
        ///   与形状之间的布尔运算正交 —— 见 <see cref="MaskExpr"/> 的说明。
        /// </summary>
        public const int MaskEnd2 = 11;

        /// <summary>图层：开始离屏收集（`ui_layer_begin`）。见 <see cref="MaskOp"/> 同级的规划。</summary>
        public const int LayerBegin = 6;
        /// <summary>图层：结束并按 alpha 整层合成（`ui_layer_end`）。</summary>
        public const int LayerEnd = 7;

        /// <summary>
        /// 查"这一点在不在**当前蒙版**里"（`ui_mask_test(x, y)`，`a`=x `b`=y）→ 1 / 0。
        ///
        /// 蒙版既然已经是"可见区域的几何定义"，它**顺手就是一份碰撞体** ——
        /// 建筑层"炸一块缺一块"之后，程序不必再自己维护一份洞的坐标表来判
        /// "香蕉撞墙了没有 / 这一枪能不能穿过破洞"。这是玩家点名的用法。
        ///
        /// | 返回 | 含义 |
        /// |---|---|
        /// | 1 | 这一点在蒙版**内**（= 会画出来的地方，`inside=0` 时是"蒙版外"）|
        /// | 0 | 在外面 |
        /// | 1 | **没有蒙版**时恒为 1（处处可见 —— 与 `InMask` 的"没有蒙版恒真"同一条口径）|
        ///
        /// ⚠ 坐标是**场景坐标**（与 `ui_rect` 那些一致），不是放大后的画布坐标 ——
        ///   缩放只发生在出图那一刻，查询走的是原始几何。
        /// ⚠ **别每像素调**：一次查询要在形状表上跑一遍（洞多时是几十次命中测试）。
        ///   拿它判"香蕉/子弹"这类每帧几个点的问题正合适，逐像素扫描请走 `ui_get_pixel`。
        /// </summary>
        public const int MaskTest = 12;

        /// <summary>`ui_mask_seg_count()` —— 当前蒙版有几段（没有蒙版返回 0）。</summary>
        public const int MaskSegCount = 13;

        /// <summary>`ui_mask_shape_count(seg)` —— 第 `a` 段里有几个形状（越界返回 0）。</summary>
        public const int MaskShapeCount = 14;

        /// <summary>
        /// `ui_mask_path(seg, shape, buf*, cap)` —— 把第 `a` 段第 `b` 个形状导出成
        /// **SVG 路径字符串**写进 `c` 指向的缓冲区（`d` = 容量），返回写入的**字节数**
        /// （不含结尾 NUL）；越界或放不下返回 -1。
        ///
        /// 这是"蒙版 → 路径"那个方向：程序拿到路径就能**描洞口的边**
        /// （画断面、做外发光），或者把洞的形状再拿去做别的运算。
        /// 圆导出的是**真圆弧**（`A` 命令），不是折线近似 —— 保持它是圆的。
        ///
        /// 典型用法（描当前蒙版里"被减掉"那一层的每个洞）：
        /// ```c
        /// int n = ui_mask_seg_count();
        /// if (n >= 2 && ui_mask_seg_op(1) == VML_MASK_SUBTRACT) {   // 见 ui_mask_seg_op
        ///     for (int i = 0; i < ui_mask_shape_count(1); i++)
        ///         if (ui_mask_path(1, i, buf, sizeof(buf)) > 0) ui_path(buf, 色, 2, 0, "", 1, 0);
        /// }
        /// ```
        /// ⚠ 段与形状都按**当前蒙版**取 —— 蒙版一改（下一条 `ui_mask_end*`）就变了。
        /// </summary>
        public const int MaskPath = 15;

        /// <summary>
        /// `ui_mask_seg_op(seg)` —— 第 `a` 段用的运算符（`MaskOp` 之一；越界返回 -1）。
        ///
        /// 程序要靠它认出"哪一段是**被减掉的洞**"（`SUBTRACT`）才能去描洞口边 ——
        /// 只有形状列表而没有运算符的话，"这是底还是洞"是猜不出来的。
        /// </summary>
        public const int MaskSegOp = 16;

        /// <summary>
        /// 查**当前占用**（`a` = 种类，返回计数；未知种类返回 -1）。
        ///
        /// | a | 种类 |
        /// |---|---|
        /// | 0 | 场景图元数（上一帧的线条数，`ui_clear` 之后归零） |
        /// | 1 | 图像数（`ui_free_image` 释放） |
        /// | 2 | 矢量图块数（`ui_free_block` 释放） |
        /// | 3 | 画刷/渐变定义数（`ui_brush_reset` 释放） |
        ///
        /// 给程序**自己判断什么时候该释放**用（"防资源爆炸"）—— 光有释放口还不够，
        /// 程序得看得见"快满了"。各类都有硬上限（见 `MaxFigures` / `MaxBrushes` …），
        /// 而且是**到了上限就静默丢弃**，所以提前查、提前放，比撞上限再猜有用得多。
        /// </summary>
        public const int ResCount = 10;
    }

    /// <summary><see cref="Screenshot"/>(#588) 没给路径时，图落在工作区里的这个子目录。</summary>
    public const string DefaultShotDir = "shot";

    /// <summary>窗口标题清洗后什么都不剩时，用它当文件名主干。</summary>
    public const string FallbackShotName = "shot";

    /// <summary>文件名主干（来自窗口标题）最多保留多少**码点** —— 标题可以很长，
    /// 但目录里排起来要看得清。按 Rune 计，避免把代理对切半。</summary>
    public const int MaxShotStemRunes = 32;

    /// <summary>
    /// `GET_IMAGE`：R0=x R1=y R2=w R3=h → R0=**图像句柄**（≥1），失败 0。
    ///
    /// 句柄由**宿主**保管（不是 VML 内存里的缓冲区）—— 与 `ui_brush`/`ui_gradient`
    /// 同一套思路。老程序的 `p = malloc(imagesize(...))` 照写不误，只是那块内存
    /// 我们不用（BGI 的 `imagesize` 返回 `4 + 2*w*h` 这类字节数，程序只拿它喂 malloc）。
    /// </summary>
    public const int GetImage = 584;

    /// <summary>
    /// `PUT_IMAGE`：R0=x R1=y R2=句柄 R3=模式（0=COPY 直接贴 / 1=XOR 异或）→ R0=1 成功、0 失败。
    ///
    /// ⚠ 两种模式**实现代价差很多**：COPY 只要把存下的图元贴上去；
    /// **XOR 必须先光栅化目的区域**（异或要读目的像素），再算、再编码。精灵动画
    /// （每帧一次 getimage + 两次 putimage）走 XOR 时这笔开销是实打实的。
    /// </summary>
    public const int PutImage = 585;

    /// <summary>
    /// `ui_set_valign` —— 设**当前文字**的竖对齐（见 <see cref="VmlScene.VAlignTop"/> 等四档）。
    ///
    /// 为什么是新号而不是给 <see cref="SetFont"/>（#532）加第 5 个参数：**给老 syscall 加参数
    /// 就是静默的未定义行为** —— 宿主从 `registers[n]` 读，而只传前几个参数的老程序，
    /// 后面那只寄存器里是**它自己上一句留下的值**（可能是个指针），宿主无从判断"这是不是真给了"。
    /// 与 `WIN_OPEN_EX` / `MSG_POLL_EX` / `CALLJSON` 同一处置。
    /// </summary>
    public const int SetVAlign = 586;

    /// <summary>`WIN_OPEN_EX` 的 R4：显示屏幕手柄（默认）。</summary>
    public const int NeedGamepad = 1;
    /// <summary>`WIN_OPEN_EX` 的 R4：不要手柄区，画布吃满整屏。</summary>
    public const int NoGamepad = 0;
    /// <summary>`WIN_OPEN_PC` 的 R4：显示屏幕键盘（默认）。取值与 <see cref="NeedGamepad"/> 同。</summary>
    public const int NeedKeyboard = 1;
    /// <summary>`WIN_OPEN_PC` 的 R4：不要屏幕键盘（只做鼠标交互的程序），画布吃满整屏。</summary>
    public const int NoKeyboard = 0;
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

    /// <summary>
    /// 文字（**带竖对齐**）：x y 文本* 颜色 字号 横锚点(0左/1中/2右) **竖对齐**(0顶/1中/2底) **样式位**(1粗 2斜) → 0。
    ///
    /// ⚠ 寄存器**排布与老号 <see cref="DrawText"/> 不同**：老号 R6 是样式位，本号 R6 是竖对齐、
    /// R7 才是样式位。这正是走新号的原因（老程序 R6 里可能是任何东西），但**两条包装函数
    /// 的参数序要照着各自那行写**，别把一个抄到另一个上。
    ///
    /// ## 为什么是**新号**而不是给 <see cref="DrawText"/> 加个参数
    ///
    /// 老程序只传前六个参数，**第七只寄存器里是它自己上一句留下的值**（可能是个计数、
    /// 可能是个指针）—— 宿主无从判断"这是不是真给了"。加参数 = 让老程序的行为
    /// 变成**未定义的**，而它们本来跑得好好的。这条铁律在本仓记过多次
    ///（`WIN_OPEN_EX` / `MSG_POLL_EX` / `MSG_WAIT_EX` 当初也是这么分的）。
    ///
    /// ## 缺的到底是什么
    ///
    /// 原先只有**横**锚点：程序写"横中"时文字确实居中了，**纵向却仍然顶着 `y`**
    /// ⇒ 摆在方框/按钮正中看着偏上。修之前只能由程序自己按字号估半个行高，
    /// 每个例子各估一次、还估不准。默认（不传这一位）= **顶对齐 = 老行为**，
    /// 所以老程序一个像素都不变。
    /// </summary>
    public const int DrawTextEx = 581;
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
    /// <summary>
    /// **消息队列**（一个号 + 操作码，见 <see cref="MsgOp"/>）。
    ///
    /// `R0` = 操作码、参数从 `R1` 起。原先这里是 **560/561/562/568/571/572/596 七个号**
    /// （v0.96.483 合并）—— 而宿主侧 `Poll`/`Wait` **本来就是同一份实现**、
    /// 只差一个 `ex` 布尔（保留位），拆成七个号纯粹是历史增长（每加一个能力就占一个）。
    ///
    /// ⚠ **认不出的操作码返回 `-1`**，不是 0 —— `0` 在这组里表示"没有消息"，
    /// 是**合法结果**；都用 0 就分不出"op 写错了"与"队列是空的"。
    /// </summary>
    public const int Msg = 560;

    /// <summary>
    /// <see cref="Msg"/> 的操作码 —— **跨语言契约**（C 头文件的 `VML_MSG_OP_*` 按这些数写死）。
    ///
    /// ⚠ **只能末尾追加**：数值会编进程序的机器码里，改值或插队 = 改 ABI。
    /// </summary>
    public static class MsgOp
    {
        /// <summary>非阻塞取一条消息：R1=消息缓冲地址 → 消息类型，无消息返回 0。</summary>
        public const int Poll = 0;

        /// <summary>阻塞取一条消息：R1=消息缓冲地址 R2=超时毫秒(0=无限) → 消息类型，超时返回 0。</summary>
        public const int Wait = 1;

        /// <summary>队列里待处理消息数（非阻塞）→ 条数。</summary>
        public const int Count = 2;

        /// <summary>
        /// 丢掉队列里**所有待处理消息**（非阻塞）→ 丢弃条数。
        ///
        /// **为什么需要它**：一次点击往往产生**多条**消息（按下/抬起/移动各一条），
        /// 游戏主循环通常只读它要的那一条，剩下的就留在队列里 —— 于是"重开一局"时
        /// `ui_poll` 又把**上一局的残留**读出来，黑子立刻落到上次最后点的位置上。
        /// 程序应在**重新开始 / 切关 / 暂停恢复**这类状态断点上调用它，把历史输入清干净。
        ///
        /// 与 <see cref="Drop"/> 的 `All` 等价，走同一条实现。
        /// </summary>
        public const int Clear = 3;

        /// <summary>
        /// `ui_msg_drop(kind)` —— **丢掉队列里某一类还没被消费的消息** → 丢掉的条数。
        ///
        /// <para>
        /// 与 <see cref="Clear"/> 的差别是**"只丢一类"**：清空是"把历史全部扔掉"，
        /// 那对"我正在拖动、但中间那几百条移动事件已经过期了"这种情形**太狠** ——
        /// 会把同一时间排着的键盘、定时器一起丢掉（程序那边表现为"按键丢了/物理卡了一拍"）。
        /// </para>
        ///
        /// <para>
        /// **为什么需要它**：移动类消息是"追最新位置"的语义，旧的位置毫无价值。
        /// 而主循环是"一次取一条"（`ui_wait_msg`），手指拖动一秒产生几百条 ⇒
        /// **产生的比消费的快，队列只涨不落**。真机实测（gorilla 连续拖滑条 6 轮）：
        /// 队列 285 → 596 → … → **2280** 条且完全不回落，而同一时间 fps 全程 19~21 ——
        /// 用户看到的就是「**背景绘图不卡、但触摸要等一下才反应**」。
        /// 在 `TOUCHMOVE` 分支里调一次 `ui_msg_drop(VML_MSG_KIND_TOUCH)`，队列立刻回到个位数。
        /// </para>
        ///
        /// <para>
        /// ⚠ 类别见 <see cref="VmlMsgKind"/>。**别拿它丢键盘/定时器** ——
        /// 那两类是离散语义，丢一条就少一次事件。
        /// </para>
        /// </summary>
        public const int Drop = 4;

        /// <summary>
        /// 读一条消息（**非阻塞，带"读完之后留不留"**）：R1=消息缓冲地址 R2=保留位 → 消息类型。
        ///
        /// 保留位的语义（<see cref="Consume"/> / <see cref="Keep"/>）：
        /// - **消费（0，默认）** = 读完就没了，下一条 poll 拿到的是再下一条 —— 与
        ///   <see cref="Poll"/> 完全一致。
        /// - **保留（1）** = 只**看**队头那一条，队列里一个都不少 —— 下一次 poll/wait 还是它，
        ///   直到程序**明确地**消费掉它（再调一次消费模式的 poll）。
        ///
        /// 什么时候要"保留"：程序想**先看一眼再决定谁处理**（比如"是触摸就自己吃掉、
        /// 是按键就留给下一层"），或者一帧里要按同一条消息做几件事。**别拿它当循环条件** ——
        /// 保留模式下 poll 永远返回同一条，写成 `while (ui_poll_ex(...) != 0)` 就是死循环。
        ///
        /// ⚠ 当初走新号（而不是给 <see cref="Poll"/> 加参数）的理由：老程序只传 R0，
        /// R1 里是**它自己上一句留下的值**，宿主无从判断那是"保留"还是垃圾。
        /// 合并进 `Msg` 之后这条约束由**操作码本身**承担 —— `Poll` 与 `PollEx` 是两个 op，
        /// 老程序发出来的仍是那个不带保留位的 op。
        /// </summary>
        public const int PollEx = 5;

        /// <summary>
        /// 读一条消息（**阻塞，带"读完之后留不留"**）：R1=缓冲地址 R2=超时毫秒(0=无限) R3=保留位 → 类型。
        /// 保留位语义见 <see cref="PollEx"/>。
        /// </summary>
        public const int WaitEx = 6;
    }


    /// <summary>
    /// `ui_msg_drop(kind)` 的**类别** —— **跨语言契约**（C 头文件里的
    /// `VML_MSG_KIND_*` 按这些数值写死）。
    ///
    /// ⚠ **只能末尾追加**：数值编进了程序的机器码里，改值等于改 ABI。
    /// </summary>
    public enum VmlMsgKind
    {
        /// <summary>定时器消息（`Timer`）。`ui_timer_kill` 是"把表停掉"，这个是"清掉已经排队的到点通知"。</summary>
        Timer = 0,
        /// <summary>触摸：按下 / 移动 / 抬起三类一起。</summary>
        Touch = 1,
        /// <summary>鼠标：移动 / 按下 / 抬起三类一起。</summary>
        Mouse = 2,
        /// <summary>键盘：按下 / 抬起。</summary>
        Key = 3,
        /// <summary>全部待处理消息（等价于 <see cref="MsgOp.Clear"/>，走同一条实现）。</summary>
        All = 4,
    }

    /// <summary>读消息的 R? 保留位：读完就没了（默认行为）。</summary>
    public const int Consume = 0;
    /// <summary>读消息的 R? 保留位：只读**队头**那一条，队列里一个都不少。</summary>
    public const int Keep = 1;

    /// <summary>
    /// **定时器**（一个号 + 操作码，见 <see cref="TimerOp"/>）。`R0` = 操作码、参数从 `R1` 起。
    ///
    /// 原先这里是 **563/564 两个号**（v0.96.483 合并）—— 装与删是同一件事的两面，
    /// 合起来只花一个号。
    /// </summary>
    public const int Timer = 563;

    /// <summary>`Timer` 的操作码 —— **跨语言契约**，只能末尾追加。</summary>
    public static class TimerOp
    {
        /// <summary>装定时器：R1=间隔毫秒 R2=用户标记 → 定时器 id；消息以 <see cref="VmlMsgType.Timer"/> 入队。</summary>
        public const int Set = 0;

        /// <summary>删定时器：R1=id → 0。</summary>
        public const int Kill = 1;
    }
    /// <summary>
    /// 窗口状态 —— **三值**，不是布尔：`0` 开着 / `1` 被关掉（返回箭头）/ `2` **从来没开过窗口**。
    ///
    /// ⚠ `2` 这一档是后加的，**必须有**：`conio` 的 `getch()` 要靠它判断"这次按键该等
    /// 窗口消息还是读 stdin"（见 `Lib/shared/src/conio.c`）。只有 0/1 两档的话，
    /// "程序从没开过窗口"与"窗口正开着"都报 0，**分不出来** —— 于是图形程序的按键
    /// 会去读命令行页的按行 stdin（永远等不到"按一下"）。
    ///
    /// 老的两值用法（`while (ui_win_closed() == 0)`）不受影响：从没开过窗口的程序
    /// 本来也不会写这个循环。
    /// </summary>
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
    /// **开窗号 → 窗口种类**。
    ///
    /// <para>
    /// 种类**由走的哪个号决定，不是参数**：`WIN_OPEN_PC`(#582) 开出来的**就是**电脑屏，
    /// 别的号（#520/#570）开出来的一律是图形窗口。
    /// </para>
    ///
    /// <para>
    /// ⚠ **为什么不做一个统一的"种类"参数**（即让一个号承担三种）：三种窗口的**实现
    /// 根本不在同一层**（图形 = 开页 + 场景；电脑屏 = 固定坐标系 + 不同输入 + 键盘），
    /// 用一个号绑住三者，等于把"分类"永久写进跨语言契约，以后加第四种又要动所有人。
    /// 而且 R1–R4 已经占满，再塞一个种类位就得扩参数 —— 那正是"新能力一律走新号"要避免的。
    /// </para>
    ///
    /// <para>
    /// 认不出来的号一律当 <see cref="VmlWinKind.Graphic"/>：那是**老窗口那一档**，
    /// 认错时退回到"能跑"的行为，而不是让程序撞进一个它没写过的模式
    /// （与 `WIN_OPEN_EX` 的 R3 "认不出的转屏值当 Follow" 是同一方向）。
    /// </para>
    /// </summary>
    public static VmlWinKind KindOfWinOpen(int syscall)
        => syscall == WinOpenPc ? VmlWinKind.PcScreen : VmlWinKind.Graphic;

    /// <summary>
    /// 这种窗口要不要**抑制触摸消息**。
    ///
    /// <para>
    /// 电脑屏窗口只发鼠标（`Mouse*`）—— 老程序处理的是鼠标事件，同时再收到一对
    /// `Touch*` 会让它们把每一次触摸当两次输入（点一下动两下）。
    /// **不是"不发消息"，是"换一种消息"**：触摸本身照常当鼠标用。
    /// </para>
    ///
    /// <para>
    /// ⚠ 放在这里而不是写进页面：页面里那几个 `#if ANDROID` 之外的地方桌面测不到，
    /// 而这一条是**纯逻辑**，一个断言就能钉住。
    /// </para>
    /// </summary>
    public static bool SuppressTouch(VmlWinKind kind) => kind == VmlWinKind.PcScreen;

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
            ? (dpW - VmlUiLimits.LandscapeSideChromeDp, dpH - VmlUiLimits.LandscapeChromeHeightDp)
            : (dpW - 16, dpH - chromeHeightDp);          // 竖屏：左右各 8dp 留白 + 底部手柄
        // 下限给足（太小的话程序没法布局）；上限防止异常设备算出离谱值
        return (Math.Clamp(w, 120, 2048), Math.Clamp(h, 120, 4096));
    }

    // ⚠ 横屏的两个布局标定常量（`LandscapeSideChromeDp = 518` / `LandscapeChromeHeightDp`）
    //   已挪到 **`VmlUiLimits`** —— 518 落在 500–599 里，而 `Handles()` 是纯数值判据、
    //   会把它当 syscall 号认领（理由见那个类的注释）。

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
    /// `DRAW_SHAPE`：**一个号画所有形状** —— R0=形状码（见 <see cref="VmlShape"/>）
    /// R1..R7=七个通用参数（**每个形状码自己定义这几个槽的含义**）。
    ///
    /// ## 为什么是「一个号 + 操作码」而不是「一个形状一个号」
    ///
    /// VML 程序**不直接调 syscall** —— 它调 `Lib/` 里的包装函数（`ui_star` 之类），
    /// 22 门语言各有 GenLib 生成的绑定。**号是给库用的，不是给程序作者用的**，
    /// 所以一个号完全可以承担多个"版本"，只要库侧按形状码分派。
    ///
    /// 好处是实打实的：
    /// ① **加形状再也不用占号**（加一个形状码 + 一行 C 包装），也就不会再出现
    ///    "号段用尽"，以及"规划表里写着某个号、其实早被别的功能占了"那类事；
    /// ② **参数个数可控**：通用槽固定 7 个，各形状按需取前 k 个 —— 绕开
    ///    "星形要 9 个参数"这个全仓没有先例的形态（现有内联汇编最多 8 个实参）；
    /// ③ 宿主侧只有**一个** switch 分支。
    ///
    /// ⚠ 代价：**形状码是跨语言契约**（像 `VML_MSG_*` / `VML_ORIENT_*`）——
    ///   发布后只能**末尾追加**，不能改值、不能插队。
    ///
    /// **样式取自当前状态**（<see cref="SetStyle"/>），除了 <see cref="VmlShape.EllipseGrad"/>
    /// 那一个自带渐变名的特例（理由见它自己的注释）。
    /// </summary>
    public const int DrawShape = 574;

    /// <summary>
    /// `BRUSH`：造一个刷子 → **句柄**（≥1；0 = 失败）。
    ///
    /// R0=种类（见 <see cref="VmlBrushKind"/>）R1..R6=参数。
    ///
    /// ## 为什么要有句柄，而不是到处传颜色/名字
    ///
    /// 状态式接口每帧都要引用刷子；传字符串要穿内存 + 清洗 + 查表，句柄就是一次 int 比较。
    /// **句柄与颜色值不共用命名空间**：句柄是小整数（1..<see cref="MaxBrushes"/>），
    /// 颜色是 `0xAARRGGBB`（≥ 0x01000000）—— 所以 <see cref="SetStyle"/> 那类地方
    /// **直接传颜色也是合法的**（"颜色 = 只有一个色标的刷子"），不必强制先 `BRUSH` 一下。
    /// 两者值域天然不重叠，不需要魔法位。
    ///
    /// ⚠ **生命周期与 `ui_gradient` 不同**：老的 `ui_gradient` 往图元表里追加一行，
    /// **`ui_clear` 会把它清掉**；刷子表是**窗口态**，`ui_clear` 不清，窗口关闭才丢。
    /// （`BRUSH_BY_NAME` 造的引用型刷子依赖程序当帧自己 `ui_gradient`，是第三种。）
    /// </summary>
    public const int Brush = 575;

    /// <summary>
    /// `SET_STYLE`：设置**当前样式** —— R0=槽位（见 <see cref="VmlStyleSlot"/>）
    /// R1=刷子句柄**或颜色** R2..R5=线宽/线帽/虚线/箭头（只有画笔槽用得上）。
    ///
    /// "前景色、背景色都是刷子，线条是画笔"落在这里：
    /// · 填充槽 = 刷子（纯色或渐变都行）；
    /// · 画笔槽 = 刷子 + 线宽 + 线帽 + 虚线 + 箭头；
    /// · 文字槽 = 刷子；
    /// · 底色槽 = 刷子（配合 `ui_clear` 之外的整屏铺底）。
    ///
    /// 默认值（没调过 `SET_STYLE` 时）：填充 = 不透明白、画笔 = **无**（不描边）、
    /// 文字 = 跟随 `ui_set_font` 给的颜色。
    ///
    /// ⚠ **本批只支持纯色画笔与纯色文字刷子**：渐变画笔/渐变文字要等引擎侧把
    /// "可采样描边"与"渐变文字"做出来（DSL 与三条后端都要动）。给渐变句柄时宿主会
    /// **记一次警告并退回该渐变的起始色** —— 不静默。
    /// </summary>
    public const int SetStyle = 576;

    // ── 通用宿主调用口（577–580，v0.96.326）──
    //
    // 四个号 = 「按**数字 id** 调宿主函数」的带类型快通道，与 `CALLJSON`(#573) 并列：
    //   · `CALLJSON` 走两个字符串 + 一次 JSON 编解码，加一个能力**不用占号**（注册一行即可），
    //     代价是每次调用都要序列化/解析两趟、还穿一次内存缓冲；
    //   · 这四个号把类型写死在**调用口**上（int8 / float8 / long4 / double4），
    //     参数直接躺在寄存器里、返回值直接写回第 0 号寄存器 —— **一次调用零编解码**。
    //
    // **分派靠 id**：第 0 个槽既是**调用号（进）**又是**返回值（出）**（`R0` / `F0` / `L0` / `D0`），
    // 于是"调用方拿不到它原来传进去的 `val[0]`"—— 这是确认过的预期，不是缺陷。
    // id → 实现由宿主侧的注册表查（<see cref="VmlCallRegistry"/>），
    // 两个宿主（手机 App / 桌面 CLI）**注册同一批 id**。
    //
    // ⚠ **id 是跨语言契约**（像 `VML_MSG_*` / `VML_ORIENT_*`）：发布后只能**末尾追加**，
    //   不能改值、不能插队；C 侧的宏在 `Lib/c/waycoder_ui.h`。
    //
    // 为什么不合成一个号（像 `DRAW_SHAPE` 那样"一个号 + 操作码"）：**类型的差别宿主看不出来** ——
    // 同一个 `R0` 里放的是 1 还是 1.0f 的位模式，只有调用口自己知道。合成一个号就必须
    // 额外传一个"类型"参数，而那一个参数会挤掉一个真正有用的参数槽（8 个槽本来就紧）。

    /// <summary>
    /// `CALLWITHINT8`：**8 个 int** —— R0=调用号 R1..R7=7 个参数 → **R0=返回值**。
    ///
    /// C 侧是 `int callwithint8(int* v)`（v[0]=调用号，v[1..7]=参数）。
    /// 调用号见 <see cref="VmlCallIds"/>，宿主按它查注册表；
    /// 未注册/类型不符/实现抛异常都**不崩**，写回一个可读的失败码（见
    /// <see cref="VmlCallRegistry.ErrorNotFound"/> 那一族）。
    /// </summary>
    public const int CallWithInt8 = 577;

    /// <summary>
    /// `CALLWITHFLOAT8`：**8 个 float** —— F0=调用号 F1..F7=7 个参数 → **F0=返回值**。
    ///
    /// ⚠ **调用号是浮点槽里的一个整数值**（`F0` = `v[0]`），所以它必须能被 `float`
    /// 精确表示（id &lt; 2²⁴ 就绝对安全）；宿主另外在 `R0` 里拿到它的**整数视图**
    /// （C 包装函数把 `(int)v[0]` 也装进 R0），于是四个调用口读 id 的方式完全一致。
    /// </summary>
    public const int CallWithFloat8 = 578;

    /// <summary>
    /// `CALLWITHLONG4`：**4 个 long** —— L0=调用号 L1..L3=3 个参数 → **L0=返回值**。
    ///
    /// `L0`–`L7` 是运行时的**长整数寄存器组**（`longRegisters[0..7]`，
    /// 操作数编码 24–31；`R0`–`R7` 在长整数指令语境下也映射到同一组 —— 见
    /// `VMLRuntime.Float.cs` 的 `GetLongValue`）。64 位值**只有这一组寄存器装得下**：
    /// 通用整数寄存器是 32 位的，`MOVEL` 写 `R0` 时只把低 32 位镜像进 `registers[0]`。
    /// 所以宿主读参数必须读 `LongRegisters`，光看 `int[] registers` 只能拿到低半截。
    /// </summary>
    public const int CallWithLong4 = 579;

    /// <summary>
    /// `CALLWITHDOUBLE4`：**4 个 double** —— D0=调用号 D1..D3=3 个参数 → **D0=返回值**。
    ///
    /// `D0`–`D7` 是**双精度寄存器组**（`doubleRegisters[0..7]`，操作数编码 16–23），
    /// 与 `long4` 同理：`int[] registers` 里只有低 32 位镜像，宿主必须读 `DoubleRegisters`。
    /// </summary>
    public const int CallWithDouble4 = 580;

    // ⚠ `MaxPolyPoints = 512` 已挪到 **`VmlUiLimits`** —— 512 落在 500–599 里，
    //   而 `Handles()` 是纯数值判据、会把它当 syscall 号认领（理由见那个类的注释）。

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

    /// <summary>
    /// **音频**（一个号 + 操作码，见 <see cref="AudioOp"/>）。`R0` = 操作码、参数从 `R1` 起。
    ///
    /// 原先这里是 **541/542/543/547 四个号**（v0.96.484 合并）—— 它们是**同一台音频设备**
    /// 上的几个动作（播 / 停 / 调音量 / 查状态），拆成四个号没有道理。
    /// </summary>
    public const int Audio = 541;

    /// <summary>`Audio` 的操作码 —— **跨语言契约**，只能末尾追加。</summary>
    public static class AudioOp
    {
        /// <summary>播放一个音频文件（循环 BGM 用它）：R1=路径* R2=1 表示循环 → 0 / -1。</summary>
        public const int Play = 0;

        /// <summary>停掉正在播的音频 → 0。</summary>
        public const int Stop = 1;

        /// <summary>设置整体音量：R1=音量(0–100) → 0（对之后播放的音生效）。</summary>
        public const int Volume = 2;

        /// <summary>还在播吗 → 1/0（BGM 播完没有）。</summary>
        public const int IsPlaying = 3;

        // ── 复音发声（v0.96.485 追加；上面四个都是**文件播放**，这四个是**现场合成**）──
        //
        // ⚠ 追加在**末尾**是硬要求：op 数值会编进程序的机器码里，插队或改值 = 改 ABI。
        //    本批**没有新号**（还在 `Audio` = 541 里）⇒ `AllNumbers` 一个字不改。

        /// <summary>
        /// 起一个音：R1=通道(0–15) R2=音符号(0–127) R3=力度(0–127，**0 等同 NoteOff**) → 0 / -1。
        ///
        /// <para>
        /// 音符号是**真 MIDI 语义**（A4 = 69 = 440Hz）——网上现成的乐谱数据能直接喂进来。
        /// 频率换算见 <see cref="VmlToneSynth.NoteToHz"/>，**全仓唯一实现**。
        /// </para>
        ///
        /// <para>
        /// 同一 (通道, 音符号) 重复起音是**重触发**（旧的滑出、新的起音），不是叠加
        /// —— 两个完全同频的声部只会拍频。
        /// </para>
        ///
        /// <para>
        /// ⚠ 返回值里 **`0` = 成功、`-1` 只是"结构性拒绝"**（参数非法）。
        /// **"这台机器放不出声"不返回 -1** —— 与 `PlayAudio` 在桌面恒返回 true 同一个理由：
        /// 说"放不了"会把程序的分支带偏。
        /// </para>
        /// </summary>
        public const int NoteOn = 4;

        /// <summary>关掉一个音：R1=通道 R2=音符号（**-1 = 该通道全部**）→ 0 / -1（那个音没在响）。</summary>
        public const int NoteOff = 5;

        /// <summary>杂项控制：R1=控制码（见 <see cref="AudioCtl"/>）R2/R3=参数 → 见各控制码。</summary>
        public const int Control = 6;
    }

    /// <summary>
    /// <see cref="AudioOp.Control"/> 的控制码 —— **跨语言契约**，只能末尾追加。
    /// </summary>
    public static class AudioCtl
    {
        /// <summary>所有音走释放（**不硬切** —— 硬切会"咔"）：R2=释放毫秒(0–1000) → 0。</summary>
        public const int AllNotesOff = 0;

        /// <summary>设通道的默认波形：R2=通道 R3=波形(0 正弦 / 1 方波 / 2 锯齿 / 3 三角) → 0 / -1。</summary>
        public const int Wave = 1;

        /// <summary>同时允许的声部上限：R2=上限(1–32) → 0 / -1（越界被钳）。</summary>
        public const int MaxVoices = 2;

        /// <summary>此刻在响的声部数 → 0–32（**只读查询**；桌面日志的判据就是它）。</summary>
        public const int Voices = 3;

        /// <summary>立刻全停（不进释放）：→ 0。用于"用户强制停止 / 页面被销毁"这类场合。</summary>
        public const int Panic = 4;
    }

    /// <summary>
    /// 音符号 / 通道 / 力度的钳位 —— **一律钳、不拒**（与 <see cref="ClampTone"/> 同一教条：
    /// 程序传个越界值不该让整个音消失，钳到边界继续响才是它想要的）。
    /// </summary>
    public static int ClampNote(int note) => Math.Clamp(note, 0, 127);

    /// <summary>通道号 0–15（<see cref="VmlToneSynth.LegacyLane"/> 是宿主内部用的，程序不该传）。</summary>
    public static int ClampChannel(int ch) => Math.Clamp(ch, 0, 15);

    /// <summary>力度 0–127（0 会被解释成 <see cref="AudioOp.NoteOff"/>）。</summary>
    public static int ClampVelocity(int vel) => Math.Clamp(vel, 0, 127);

    /// <summary>
    /// **震动**（一个号 + 操作码，见 <see cref="VibrateOp"/>）。`R0` = 操作码、参数从 `R1` 起。
    ///
    /// 原先这里是 **545/546 两个号**（v0.96.484 合并）—— "震一下"与"按节奏震"
    /// 是同一个设备的同一个动作、只差模式参数。
    /// </summary>
    public const int Vibrate = 545;

    /// <summary>`Vibrate` 的操作码 —— **跨语言契约**，只能末尾追加。</summary>
    public static class VibrateOp
    {
        /// <summary>震动一下：R1=时长ms R2=强度(0–255，0=用系统默认) → 0。</summary>
        public const int Simple = 0;

        /// <summary>按节奏震动：R1=模式*(int 数组) R2=段数 → 0（奇数下标=静、偶数下标=动，同 Android 语义）。</summary>
        public const int Pattern = 1;
    }

    // ── 持久化与常亮（550–553）──
    //
    // ⚠ **这一组刻意不含"随机数"与"取时间"** —— VM 已经有了，别再实现一遍：
    //   · `#50` Random / `#51` Seed → `ui_rand` 就是包着它（任何语言都能直接调）
    //   · `#53` GetTick（VM 启动至今毫秒）/ `#54` GetDateTime（unix **秒**）/ `#55`/`#56` 日期时间串
    // 同样是"先 grep 有没有现成的"，只不过这里的"仓"是 VM 的内置 syscall 表。

    /// <summary>
    /// **持久化键值**（一个号 + 操作码，见 <see cref="StoreOp"/>）。`R0` = 操作码、参数从 `R1` 起。
    ///
    /// 原先这里是 **550/551/552 三个号**（v0.96.484 合并）—— 增 / 查 / 删是同一张表的三个动作。
    /// </summary>
    public const int Store = 550;

    /// <summary>`Store` 的操作码 —— **跨语言契约**，只能末尾追加。</summary>
    public static class StoreOp
    {
        /// <summary>写入一条：R1=键* R2=值* → 0（键会加 `vml.` 前缀，见 <see cref="StoreKey"/>）。</summary>
        public const int Set = 0;

        /// <summary>读一条：R1=键* R2=缓冲* R3=容量 → 写入长度；没有这个键返回 -1。</summary>
        public const int Get = 1;

        /// <summary>删一条：R1=键* → 0。</summary>
        public const int Delete = 2;
    }

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
    /// 截屏路径的**清洗**（<see cref="Screenshot"/>(#588)）：返回干净的**相对**路径，
    /// 返回 <c>null</c> = 拒绝（调用方当失败处理，**不要落到文件系统**）。
    ///
    /// ## 为什么钳制收在这里，而不是各端自己去防
    ///
    /// 宿主接口的 `ResolvePath()` **两端都没有做沙箱钳制**
    /// （手机是 `CwdContext.Resolve`、桌面是 `Path.GetFullPath(Combine(WorkDir, rel))`）
    /// —— 程序传 `"../../../etc/passwd"` 就真会写到外面去。而这条清洗是**纯逻辑**，
    /// 两端编的是同一份（`WayCoder/UI/Shared/` 被主工程与 `scripts/vmlcli` 同时编译），
    /// 所以规则只有一处实现，不会出现"手机上拒绝、桌面上放行"这种最难查的分叉。
    /// （`SandboxManager.IsUnder` 是另一条路，但 `scripts/vmlcli` **不编译** `SandboxManager.cs`，
    /// 共享层调不到它。）
    ///
    /// ## 它凭什么够用
    ///
    /// 返回的路径**既非绝对、又不含任何回退段** ⇒ `ResolvePath(rel)` 无论怎么拼，
    /// 结果都落在工作目录内。也就是说：**这里是唯一的屏障，规则改松了就是逃逸**。
    ///
    /// ## 规则
    /// · `\` 一律归成 `/`（程序在 Windows 上习惯写反斜杠）；
    /// · 拒绝含 `:` 的（盘符 `C:\`、scheme `http:`）；
    /// · 逐段检查，任一段是 `..` 或 `.` **即整体拒绝**（不做"就地消解"——
    ///   悄悄改掉用户给的路径比直接拒绝更难排查）；
    /// · 空串/纯空白 → 返回**空串**（合法，含义是"程序没指定，调用方去生成默认名"，
    ///   见 <see cref="DefaultShotPath"/>）—— ⚠ 别把它当失败，`null` 才是失败；
    /// · 无扩展名 → 补 `.png`（不认扩展名会让用户在文件页看不出这是什么）。
    /// </summary>
    public static string? SanitizeShotPath(string? raw)
    {
        var s = (raw ?? "").Trim().Replace('\\', '/');
        if (s.Length == 0) return "";                  // 没指定 ⇒ 交给调用方生成默认名
        if (s.Contains(':')) return null;              // 盘符 / scheme

        var parts = s.Split('/');
        var kept = new List<string>();
        foreach (var p in parts)
        {
            if (p.Length == 0) continue;               // 首尾/重复分隔符
            if (p == ".." || p == ".") return null;    // ← 唯一的逃逸屏障，别放宽
            kept.Add(p);
        }
        if (kept.Count == 0) return "";                // 只给了分隔符 ⇒ 同"没指定"

        var last = kept[^1];
        if (!last.Contains('.')) kept[^1] = last + ".png";
        return string.Join('/', kept);
    }

    /// <summary>
    /// 程序**没给路径**时的默认落点：
    /// `shot/&lt;窗口标题&gt;_&lt;日期&gt;_&lt;时间&gt;.png`
    ///（五子棋那局就存成 `workspace/shot/五子棋_20260924_102801.png`）。
    ///
    /// 为什么要带标题与时间戳（而不是固定的 `shot.png`）：截屏是**给人看/给人找**的，
    /// 固定名字只会互相覆盖 —— 而这个接口最常见的用法就是"玩到一半连存几张"。
    /// 带时间戳还顺带按时间排序，一眼看出先后。
    ///
    /// ⚠ `now` 由调用方传进来（**不在里面调 `DateTime.Now`**）：这样这个函数是纯的、
    ///   自测能钉死输出，不必等真实时钟。这是本仓一贯的做法（时间/随机都要能从外面钉）。
    ///
    /// ⚠ 标题会过 <see cref="SafeFileStem"/>：窗口标题是**程序自己起的**，
    ///   里面可能有 `/`、`..`、冒号 —— 原样拼进路径就是一条逃逸通道。
    /// </summary>
    public static string DefaultShotPath(string? title, DateTime now)
        => $"{DefaultShotDir}/{SafeFileStem(title)}_{now:yyyyMMdd}_{now:HHmmss}.png";

    /// <summary>
    /// 把窗口标题清洗成**一个安全的文件名主干**（不含路径分隔符、不含 `.`/`..`、非空）。
    ///
    /// 规则：只留字母/数字（含中文等 CJK —— `char.IsLetterOrDigit('中')` 为真，
    /// 这里正是要它真）、`-`、`_`；其余字符压成一个 `_`；主干上限
    /// <see cref="MaxShotStemRunes"/> 个**码点**（`Rune` 计，不按 `char` ——
    /// 否则 emoji/扩展 B 汉字会被切半成 U+FFFD，本仓有这条硬规矩）。
    /// 清洗后什么都不剩（标题是纯符号、空标题）就退回 <see cref="FallbackShotName"/>。
    ///
    /// ⚠ 结果永远不会是 `.` / `..` / 空 —— 只保留字母数字与 `-_` 就结构性地排除了它们，
    ///   所以拼进路径不会变成回退段。**别为"允许更多字符"放宽这条**。
    /// </summary>
    public static string SafeFileStem(string? title)
    {
        var sb = new StringBuilder();
        // ⚠ 上限要数**码点**，不能数 `sb.Length`（那是 UTF-16 单元数）——
        //   代理对每个码点占 2 个单元，按 `Length` 卡会让扩展 B 汉字/emoji
        //   只留下一半（实测：80 个扩展 B 汉字本该留 32 个，按 Length 卡只留 16 个）。
        //   这条正是本仓"截断必须按码点"的硬规矩，**代码与注释都要对得上**。
        var runes = 0;
        var lastWasUnderscore = false;
        foreach (var rune in (title ?? "").EnumerateRunes())
        {
            if (runes >= MaxShotStemRunes) break;
            var keep = Rune.IsLetterOrDigit(rune) || rune.Value == '-' || rune.Value == '_';
            if (keep)
            {
                sb.Append(rune.ToString());
                runes++;
                lastWasUnderscore = false;
            }
            else if (!lastWasUnderscore && sb.Length > 0)
            {
                sb.Append('_');            // 连续的非保留字符压成一个下划线
                runes++;
                lastWasUnderscore = true;
            }
        }
        // 末尾那个下划线（来自标题尾部的一串符号）去掉。
        // `_` 是 ASCII，代理对的低代理不可能是它 ⇒ 这个循环不会切进代理对。
        while (sb.Length > 0 && sb[^1] == '_') sb.Length--;
        return sb.Length > 0 ? sb.ToString() : FallbackShotName;
    }

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

    // ── 手机特有的操作方式（§3 P1）────────────────────────────────────────
    //
    // 这一批的共同点：**桌面端没有对应的东西**，但接口本身是跨端的 ——
    // 桌面缺的是"输入源"（没有手指、没有重力），不是"语义"。
    // 所以宿主层照常实现，两端各给一个平台实现（桌面用脚本化输入喂）。

    /// <summary>
    /// `TOUCH_QUERY`：`R0`=槽位(0–9) → `R0`=x `R1`=y `R2`=按下(1/0)。
    ///
    /// **多点触控** —— 虚拟摇杆、双指缩放、双人同屏都靠它。
    ///
    /// ⚠ **轮询式**而不是新消息类型：16 字节的消息塞不下"手指 id + x + y"
    ///   （`[类型][A][B][时间]` 只有两个自由值），把它扩成 20 字节会**打断所有现有程序**。
    ///   轮询是在现有布局之外新增，**零破坏**。
    /// ⚠ 平台要**同时**做两件事：更新这份状态（`PostTouch`）+ 投一条消息 ——
    ///   只更新状态的话，用 `ui_wait_msg` 驱动主循环的程序再也收不到触摸。
    /// </summary>
    public const int TouchQuery = 556;

    /// <summary>
    /// `KEY_QUERY`：`R0`=虚拟键码 → 0/1（**此刻**按住没有）。
    ///
    /// "持续按住左移"这类逻辑不必自己维护一张状态表 —— 但**它治不了丢 KeyUp**：
    /// 手指划出按键范围、系统吃掉 CANCEL 都会让 `KeyUp` 永远不来，
    /// 而查询只会一直报"按着"。**连发逻辑仍然要自带刹车**（见 tetris 的连发那一段）。
    /// </summary>
    public const int KeyQuery = 557;

    /// <summary>`ORIENTATION_LOCK`：`R0` = 0 竖屏 / 1 横屏 / 2 自动。</summary>
    public const int OrientationLock = 558;

    /// <summary>`IMMERSIVE`：`R0` = 0/1，隐藏状态栏与导航栏（全屏游戏用）。</summary>
    public const int Immersive = 559;

    /// <summary>
    /// `SENSOR`（v0.96.492）：`R0`=操作码（见 <see cref="SensorOp"/>），参数从 `R1` 起。
    ///
    /// <para>
    /// **一个号 + 第一个参数区分三种传感器**（<see cref="SensorKind"/>）—— 和
    /// `Audio`/`Vibrate`/`Store` 一样，同类的东西收成一个号。
    /// </para>
    ///
    /// <para>
    /// **为什么是轮询式，不是"值变了投一条消息"**：姿态是**连续量**，一秒钟几十个采样，
    /// 投消息只会把队列淹掉（而主循环一次只取一条，见 `ui_msg_drop` 那段）。
    /// 与 `TOUCH_QUERY` 同一套分工：**队列记"发生过什么"，这里记"此刻是什么样"**。
    /// 另外 16 字节的消息也塞不下三个值 —— 那是 `TOUCH_QUERY` 当初就不走消息的同一个理由。
    /// </para>
    ///
    /// <para>
    /// ⚠ **值是整数定标**，不是浮点：加速度单位 **毫克**（`1000` = 1g）、角速度单位
    /// **千分之一度/秒**、姿态角单位 **千分之一度**。理由与图元里的"千分比"一样 ——
    /// 整数让 ABI 不必管浮点寄存器，而且够用（手机的陀螺仪满量程约 ±2000°/s
    /// ⇒ 定标后约 ±200 万，`int` 放得下）。
    /// </para>
    ///
    /// <para>
    /// ⚠ **没有传感器的设备要有确定的返回**：`AVAILABLE` 报 0、`QUERY` 返回 0。
    /// 桌面（vmlcli）就是这一类 —— 它那边靠脚本注入来模拟，见 `CliVmlHost`。
    /// </para>
    /// </summary>
    public const int Sensor = 554;

    /// <summary>`SENSOR` 的操作码（`R0`）。**跨语言契约，只能末尾追加。**</summary>
    public static class SensorOp
    {
        /// <summary>
        /// 读最新值：`R1`=种类 `R2`=输出缓冲区（三个 `int32`）→ 1=有效 / 0=没有该传感器。
        ///
        /// <para>
        /// ⚠ **写进调用方给的缓冲区**，不是"回三个寄存器" —— C 那边 `asm()` 只能拿到 `R0`，
        /// 与 `ui_touch` / `ui_wait` 同一套约定。
        /// </para>
        /// </summary>
        public const int Query = 0;

        /// <summary>这台设备有没有该传感器：`R1`=种类 → 1/0。**开窗之前就能问。**</summary>
        public const int Available = 1;

        /// <summary>设采样间隔：`R1`=种类 `R2`=毫秒（0 = 用平台默认）→ 1/0（没有该传感器）。</summary>
        public const int Rate = 2;

        /// <summary>
        /// 把**当前姿态**当成新的零点（"校准水平"）：`R1`=种类 → 1/0。
        ///
        /// <para>
        /// 游戏里很需要它 —— 玩家躺在沙发上玩的，不校准的话"水平"一直是错的。
        /// 只对 <see cref="SensorKind.Rotation"/> 与 <see cref="SensorKind.Accel"/> 有意义。
        /// </para>
        /// </summary>
        public const int Calibrate = 3;
    }

    /// <summary>
    /// 传感器种类（`SENSOR` 的 `R1`）。**跨语言契约**，与 `waycoder_ui.h` 的 `VML_SENS_*` 同值。
    ///
    /// <para>
    /// ⚠ **三档的分工要分清楚**（这是给写游戏的人最重要的一段）：
    /// <list type="bullet">
    /// <item><see cref="Accel"/> —— **倾斜控制用这个**。静止时它读到的是重力方向，
    /// 也就是"手机往哪边歪"。**不漂移**，而且几乎所有设备都有。</item>
    /// <item><see cref="Gyro"/> —— **角速度**（转得多快）。要"当前角度"得自己积分，
    /// 而积分会**漂移**（几十秒就偏出可用的范围）。它适合做"甩一下"这类瞬时判断。</item>
    /// <item><see cref="Rotation"/> —— **融合姿态**，平台把加速度计+陀螺仪（+磁力计）融好的角度，
    /// 直接给俯仰/翻滚/方位。要精确、长时间稳定的角度就用它；代价是**有的设备没有**
    /// （没有磁力计时方位角会慢慢转，平台通常也会报"可用性打折"，这里一律按"有"处理）。</item>
    /// </list>
    /// </para>
    /// </summary>
    public static class SensorKind
    {
        /// <summary>加速度（含重力）。单位**毫克**（`1000` = 1g）。x=右 y=上 z=**屏幕朝外**。</summary>
        public const int Accel = 0;

        /// <summary>角速度。单位**千分之一度/秒**。绕 x=俯仰 y=翻滚 z=方位。</summary>
        public const int Gyro = 1;

        /// <summary>融合姿态角。单位**千分之一度**。x=俯仰(抬低头) y=翻滚(左右歪) z=方位(指南针)。</summary>
        public const int Rotation = 2;

        /// <summary>合法种类数（`R1` 的范围是 `0 .. Count-1`）。</summary>
        public const int Count = 3;
    }

    /// <summary>`TOUCH_QUERY` 的槽位数（`R0` 的合法范围是 `0 .. MaxTouchSlots-1`）。</summary>
    public const int MaxTouchSlots = 10;

    /// <summary>
    /// 本协议**已占用**的全部号，只给自测查重用 —— 没有运行时消费方。
    ///
    /// 为什么要有它：号是**在同一个类里一个个加**上去的（`public const int Xxx = 5xx;`），
    /// 而 AOT 禁反射 ⇒ 没有任何办法在运行时把它们枚举出来。没有这张清单，
    /// "两个特性抢同一个号"只能靠人眼比对 —— 而它的症状是**一个功能静默变成另一个功能**
    /// （`switch` 里后写的 `case` 赢不了，先写的赢；两者都"能跑"，只是行为是别人的），
    /// 属于最难发现的一类。有了它，自测里一条断言就能挡住。
    ///
    /// ⚠ **新增一个号必须同时加进这里**，否则这条护栏形同虚设。清单本身不参与任何逻辑，
    /// 所以忘了加不会让程序出错 —— 只会让这道网漏掉新号（自测第 12 条查的是"这张表里有没有重复"，
    /// 查不出"表里少了一个"）。开发新号时把这一步和"加常量"当成同一个动作。
    /// </summary>
    public static readonly int[] AllNumbers =
    [
        // 对话框 500–503
        DlgMsg, DlgSelect, DlgMulti, DlgInput,
        // 窗体与绘图 520–533
        WinOpen, WinClose, DrawClear, DrawPixel, DrawLine, DrawRect, DrawCircle, DrawEllipse,
        DrawText, DrawIcon, DrawImage, DrawPresent, SetFont, Text,
        // 像素读回 583–585、587 + 竖对齐 586 + 主动截屏 588
        // ⚠ `SetVAlign`(586) 此前**漏在这张表外**（上面这行注释也把它跳过去了）——
        //   漏登记**不报错**，只是查重网漏掉它（那张网查"重复/越界"，**查不出"少一个"**）。
        //   与 `DrawTextEx`(581) 那次是同一个坑，见下面那段注释。
        FloodFill, GetImage, PutImage, SetVAlign, DrawGetPixel, Screenshot, FreeImage,
        // 矢量图块 589（**一个号 + 操作码**，见 `BlockOp`；原先 589–593 五个号）
        Block,
        // 绘图增强 534–539
        Gradient, DrawPath, DrawPolygon, DrawPolyline, DrawRectGrad, DrawCircleGrad,
        // 手感与存档 541–553（音频 / 震动 / 存档**各收成一个号 + 操作码**，
        // 见 `AudioOp` / `VibrateOp` / `StoreOp`；`ScreenKeepOn` 单个、无同类可并）
        Audio, Vibrate, Store, ScreenKeepOn,
        // 输入与屏幕 560–569（消息与定时器**各收成一个号 + 操作码**，见 `MsgOp` / `TimerOp`）
        Msg, Timer, WinClosed, ScrW, ScrH, ScrOrient,
        // 手机特有的操作方式 556–559
        TouchQuery, KeyQuery, OrientationLock, Immersive,
        // 传感器 554（**一个号 + 操作码**，见 `SensorOp` / `SensorKind`；
        // 加速度计 / 陀螺仪 / 融合姿态三档用 R1 区分）
        Sensor,
        // 扩展 570–573
        WinOpenEx, CallJson,
        // ⚠ `DrawTextEx = 581` 是**补进来的**（v0.96.353）：它早就定义了，
        //   却一直没进这张表 —— 正是上面那段注释警告的"护栏形同虚设"。
        //   没有它，581 与别的号撞了也查不出来（症状是一个功能静默变成另一个功能）。
        DrawTextEx,
        // 电脑屏窗口（第三种窗口）
        WinOpenPc,
        // 绘图扩展 574–576（一个号 + 操作码，见 VmlShape / VmlBrushKind / VmlStyleSlot）
        DrawShape, Brush, SetStyle,
        // ⚠ `GfxState = 595` 是**补进来的**（v0.96.481）：它早就定义了，却一直没进这张表 ——
        //   **这是第三次**（前两次是 `DrawTextEx(581)`、`SetVAlign(586)`，上面两处注释各记了一次）。
        //   漏登记**不报错**：这张表只被自测拿去查"重复/越界"，漏一个号 = 那道网漏掉一个号，
        //   它与别的号撞了也查不出来（症状是一个功能静默变成另一个功能）。
        //   ⇒ 本版另加了一条**拿源码文本与这张表交叉比对**的自测，专治"漏一个"，
        //     见 `SelfTest.Chunk26` 的"号段清单与常量定义一致"。
        GfxState,
        // 通用宿主调用口 577–580（带类型快通道，见 VmlCallRegistry）
        CallWithInt8, CallWithFloat8, CallWithLong4, CallWithDouble4,
    ];

    /// <summary>
    /// 号 → 调用口种类；**不是这四个号就返回 false**（交回宿主原样处理）。
    ///
    /// 放在这里而不是注册表里：号段表就在本文件，注册表只该管"id → 实现"。
    /// 四个号的映射是**跨语言契约的一半**（另一半是 C 头文件里的包装函数），
    /// 只有一处实现，宿主两边共用（<see cref="VmlCallRegistry.TryHandle"/>）。
    /// </summary>
    public static bool TryCallCast(int syscallNumber, out VmlCallCast cast)
    {
        switch (syscallNumber)
        {
            case CallWithInt8: cast = VmlCallCast.Int8; return true;
            case CallWithFloat8: cast = VmlCallCast.Float8; return true;
            case CallWithLong4: cast = VmlCallCast.Long4; return true;
            case CallWithDouble4: cast = VmlCallCast.Double4; return true;
            default: cast = VmlCallCast.Int8; return false;
        }
    }

    /// <summary>
    /// 文字锚点 → 平台 <c>DrawString</c> 要的矩形。
    ///
    /// 平台只提供「**在给定矩形内**对齐」的重载，没有"只给一个锚点"的版本，所以锚点只能靠
    /// **摆矩形**表达：让 <paramref name="x"/> 落在矩形的左缘（start）/ 中心（middle）/
    /// 右缘（end）上。
    ///
    /// ⚠ 别图省事拿「x 到画布右边」（<c>sceneWidth - x</c>）当矩形 —— 那样 middle 居中的是
    ///   `[x, sceneWidth]` 的**中点**而不是 x。锚点越靠左偏得越多：整屏按键的字会被一起拉向
    ///   画布中心（实测 col0 的字跑到屏幕中间、文字列间距只剩按键间距的一半），end 同样会歪。
    ///
    /// 矩形取 **2×画布宽**以保证装得下任何一行字；允许为负坐标，平台按裁剪处理。
    /// </summary>
    public static (double X, double Width) TextAnchorBox(double x, double sceneWidth, string? anchor)
    {
        var w = Math.Max(1, sceneWidth * 2);
        var left = anchor switch
        {
            "middle" => x - w / 2,
            "end" => x - w,
            _ => x,
        };
        return (left, w);
    }

    /// <summary>号段上界（宿主启动时把这整段加进 <c>SyscallConstants.UserAllowed</c> 用）。</summary>
    public static IEnumerable<int> ReservedRange()
    {
        for (var n = 500; n <= 599; n++) yield return n;
    }
}

/// <summary>
/// <see cref="VmlUi.DrawShape"/> 的**形状码** —— 跨语言契约，与 C 头文件的 `VML_SHAPE_*` 宏
/// 一一对应，改了等于改 ABI。
///
/// 每个码定义 `R1..R7` 这七个通用槽里**前几个**的含义（其余忽略）。选"通用槽 + 各自解释"
/// 而不是"一个形状一个号"，理由见 <see cref="VmlUi.DrawShape"/>。
///
/// ⚠ **只能末尾追加**：不能改数值、不能插队。22 门语言的绑定是按名字生成的（GenLib），
///   但 C 头文件里的宏、以及程序里已经写下的字面量，都是按**值**编译的。
/// </summary>
public static class VmlShape
{
    /// <summary>R1..R5 = x y w h 半径（半径 &gt; 0 即圆角）。</summary>
    public const int Rect = 0;
    /// <summary>R1..R3 = cx cy r。</summary>
    public const int Circle = 1;
    /// <summary>R1..R4 = cx cy rx ry。</summary>
    public const int Ellipse = 2;
    /// <summary>R1..R4 = x1 y1 x2 y2。</summary>
    public const int Line = 3;
    /// <summary>R1=点数组\* R2=**点数**（x,y 交替的 int32；自动闭合）。</summary>
    public const int Polygon = 4;
    /// <summary>R1=点数组\* R2=点数（不闭合）。</summary>
    public const int Polyline = 5;
    /// <summary>R1=SVG path 的 d\*。</summary>
    public const int Path = 6;
    /// <summary>R1..R3 = x y 文本\*（用 <see cref="VmlUi.SetFont"/> 设的文字属性）。</summary>
    public const int Text = 7;
    /// <summary>R1..R6 = cx cy 外半径 内半径 角数 旋转角(度)。</summary>
    public const int Star = 8;
    /// <summary>R1..R5 = cx cy 半径 边数 旋转角(度)。</summary>
    public const int Regular = 9;
    /// <summary>R1..R4 = cx cy 外半径 内半径。</summary>
    public const int Ring = 10;
    /// <summary>R1..R5 = cx cy 半径 起始角 结束角（度）。</summary>
    public const int Pie = 11;
    /// <summary>R1..R3 = cx cy 尺寸。</summary>
    public const int Heart = 12;
    /// <summary>
    /// R1..R4 = cx cy rx ry **R5=渐变名\***（UTF-8，NUL 结尾）。
    ///
    /// ⚠ 这是**唯一一个自带刷子**的形状码 —— 其余码的填充/描边一律取当前样式状态
    /// （<see cref="VmlUi.SetStyle"/>）。它破例是为了与既有的 `ui_rect_grad` / `ui_circle_grad`
    /// 同形：那两条也是"渐变名直接传给绘制调用"，混用两种写法比破一次例更难记。
    /// 没有渐变名时**什么都不画**（与那两条一致 —— 退化成黑椭圆只会让人以为渐变没生效）。
    /// </summary>
    public const int EllipseGrad = 13;
}

/// <summary>
/// <see cref="VmlUi.Brush"/> 的刷子种类 —— 跨语言契约（C 头文件 `VML_BRUSH_*`）。
/// ⚠ 只能末尾追加。
/// </summary>
public static class VmlBrushKind
{
    /// <summary>R1=颜色(ARGB)。**同色会复用同一个句柄**，循环里反复造也安全。</summary>
    public const int Solid = 0;
    /// <summary>R1=色A R2=色B R3..R6 = x1 y1 x2 y2（**千分之一**，与 <c>ui_gradient</c> 同口径）。</summary>
    public const int Linear = 1;
    /// <summary>R1=色A R2=色B R3..R5 = cx cy r（千分之一）。</summary>
    public const int Radial = 2;
    /// <summary>R1=渐变名\*（引一个已经用 `ui_gradient` 定义过的）→ 句柄，没有则 0。</summary>
    public const int ByName = 3;
}

/// <summary>
/// 画笔的**线帽** —— 跨语言契约（C 头文件 `VML_CAP_*`）。⚠ 只能末尾追加。
/// </summary>
public static class VmlCap
{
    /// <summary>平头（默认）。</summary>
    public const int Butt = 0;
    /// <summary>圆头。</summary>
    public const int Round = 1;
    /// <summary>方头。</summary>
    public const int Square = 2;
}

/// <summary>
/// 画笔的**箭头** —— 跨语言契约（C 头文件 `VML_ARROW_*`）。⚠ 只能末尾追加。
///
/// ⚠ **本批只做末端箭头**，且只对 <see cref="VmlShape.Line"/> 生效：
/// DSL 的 `arrow` 指令几何就是"从起点指向终点"，起端/两端要让它支持反向，还没做。
/// <see cref="Start"/> / <see cref="Both"/> 现在与 <see cref="End"/> 同义。
/// </summary>
public static class VmlArrow
{
    /// <summary>不画箭头（默认）。</summary>
    public const int None = 0;
    /// <summary>末端箭头。</summary>
    public const int End = 1;
    /// <summary>起端箭头（⚠ 本批与 End 同义）。</summary>
    public const int Start = 2;
    /// <summary>两端箭头（⚠ 本批与 End 同义）。</summary>
    public const int Both = 3;
}

/// <summary>
/// <see cref="VmlUi.SetStyle"/> 的槽位 —— 跨语言契约（C 头文件 `VML_STYLE_*`）。
/// ⚠ 只能末尾追加。
/// </summary>
public static class VmlStyleSlot
{
    /// <summary>填充刷子。这是默认槽（不传槽位就等于设它）。</summary>
    public const int Fill = 0;
    /// <summary>画笔（描边）：刷子 + 线宽 + 线帽 + 虚线 + 箭头。传 0 = 不描边。</summary>
    public const int Pen = 1;
    /// <summary>文字刷子。传 0 = 回到 <see cref="VmlUi.SetFont"/> 给的颜色。</summary>
    public const int Text = 2;
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

    /* ── 电脑屏窗口的**屏幕键盘**要用的键 ──
     *
     * 取值一律照 **Win32 虚拟键码**（与上面那批同源）。理由和"手柄映射到自然键"一样：
     * 程序里写 `key == VML_KEY_F1` 拿到的必须与 PC 上真按 F1 一致，
     * 否则同一份老程序在手机和电脑上要写两套判断。
     *
     * ⚠ 这里只列**屏幕键盘上画得出来的**。老程序用得到的键远不止这些
     *   （`VK_OEM_*` 那一堆标点、多媒体键…），缺的走**外接物理键盘**那条路，
     *   不必也不该把所有 VK 都塞进一张手机键盘。
     */
    public const int Tab = 9;
    public const int Ctrl = 17;       // VK_CONTROL
    public const int Alt = 18;        // VK_MENU
    public const int PageUp = 33;
    public const int PageDown = 34;
    public const int End = 35;
    public const int Home = 36;
    public const int Insert = 45;
    public const int Delete = 46;
    public const int F1 = 112;        // VK_F1 … VK_F12 = 112..123（连号）
    public const int F12 = 123;

    /// <summary>F1–F12 的键码（连号 112..123）—— 键盘布局表按序号取。</summary>
    public static int F(int n) => F1 + (n - 1);

    /* ── 标点键：**Win32 的 OEM 码**（不是 ASCII）──
     *
     * ⚠ 这一点很容易想当然：屏幕键盘发的是**虚拟键码**（与 PC 上真按键一致），
     * 不是字符。所以 `-` 是 **189** 不是 `'-'`(45)，`;` 是 186 不是 59。
     * 程序要拿到字符得自己映射（就像 Win32 里 `WM_KEYDOWN` 与 `WM_CHAR` 是两条消息）。
     *
     * 字母数字**恰好**与 ASCII 大写重合（`VK_A` = 65 = `'A'`），所以那部分看不出来 ——
     * 只有标点会暴露这条约定，**别照着字母那半边的巧合去推标点**。
     */
    public const int OemMinus = 189;      // -
    public const int OemPlus = 187;       // =
    public const int OemOpenBracket = 219;   // [
    public const int OemCloseBracket = 221;  // ]
    public const int OemBackslash = 220;     // \
    public const int OemSemicolon = 186;     // ;
    public const int OemQuotes = 222;        // '
    public const int OemComma = 188;         // ,
    public const int OemPeriod = 190;        // .
    public const int OemQuestion = 191;      // /
    public const int OemTilde = 192;         // `

    /// <summary>
    /// 虚拟键码 → 它代表的**字符**（给 `INKEY$` / `getch()` 这类"字符输入"接口用）；
    /// 没有可打印字符的键（方向键、功能键、Shift/Ctrl…）返回 <c>'\0'</c>。
    ///
    /// <para>
    /// ⚠ 这张表**只在"消息 → 老接口"这一处**用，且刻意只覆盖常用键。它与
    /// 「程序侧 `WM_KEYDOWN` 拿到的 keycode」是**两件事**（后者是虚拟键码、标点是 OEM 码，
    /// 见上面 `OemMinus` 那段说明）。老程序写 `INKEY$` 要的是**字符**，
    /// 所以这里做这一层映射；不想在这层做映射的程序应当直接用 `ui_poll_msg`。
    /// </para>
    /// </summary>
    public static char ToChar(int vk)
    {
        if (vk >= 32 && vk <= 126) return (char)vk;   // 字母数字与标点（字母恰好与 ASCII 大写重合）
        if (vk == Enter) return '\r';
        if (vk == Backspace) return '\b';
        if (vk == Tab) return '\t';
        if (vk == Escape) return (char)27;
        return '\0';
    }
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
/// <summary>
/// 虚拟机给 VML 程序的**内存与栈**（用户 2026-09-25 定：内存 16M、栈 1M）。
///
/// <para><b>为什么放这里</b>：桌面（`scripts/vmlcli/Program.cs`）与手机
/// （`WayCoder.Maui/Services/MauiVml.cs`）**各有一份 `MakeConfig()`** ——
/// 值写在两处就迟早漂（本仓头号坑）。两端都编这个文件，所以这里是**唯一真源**。</para>
///
/// <para><b>⚠ 两个值有依赖，顺序不能反</b>：<see cref="VmConfig.StackSize"/> 的 setter 是
/// <c>Math.Clamp(value, 1024, _memorySize / 2)</c> —— 它按 **`VmConfig.MemorySize`**
/// 钳位。所以必须**先设内存、再设栈**：反过来的话，内存还是默认的 1MB 时设 1MB 栈，
/// 会被**静默钳成 512KB**（不报错、只是栈小了一半）。</para>
///
/// <para><b>⚠ 传给 `VmRuntime` 的内存必须是 0</b>：构造函数的第一个参数会**覆盖**
/// `config.MemorySize`（`if (memorySize &lt;= 0) memorySize = config.MemorySize;`）。
/// 传一个具体数字就等于又开了一处真源。传 0 ⇒ 一切以本类为准。</para>
/// </summary>
/// <summary>
/// **不是 syscall 号、却被放在了 500–599 那个数值区间里**的常量 —— 集中到这里。
///
/// <para>
/// ⚠ **为什么要搬走**：<see cref="VmlUi.Handles"/> 是**纯数值判据**（`500..599`），
/// 它会把落在这个区间的一切都当成 syscall 号认领 —— 包括下面这两个根本不是号的常量。
/// 目前靠 `VmlHostRuntime.HandleSyscall` 末尾的 `default: return false` 放行才没出事，
/// 但那是"程序只能靠巧合才碰不到"的**伪 syscall**：将来谁给 512 或 518 补一个 `case`，
/// 就会出现一个谁都想不到的内置调用，而且**不会有任何报错**。
/// </para>
///
/// <para>
/// ⚠ 值是**实测标定**（布局）与**设计上限**（点数），不能为了"避开区间"而改数值 ——
/// 所以搬位置，不搬值。搬出来之后这两个号在 <see cref="VmlUi.Handles"/> 眼里就干干净净地
/// 是"空号"了。
/// </para>
/// </summary>
public static class VmlUiLimits
{
    /// <summary>横屏时**左右两列手柄 + 间距 + 页边距**占掉的宽度（dp）。实测标定，见 <c>AvailableArea</c>。</summary>
    public const int LandscapeSideChromeDp = 518;

    /// <summary>横屏时**状态栏 + 导航栏 + 折叠条**占掉的高度（dp）。实测标定，与上一条成对。</summary>
    public const int LandscapeChromeHeightDp = 110;

    /// <summary>
    /// 多边形/折线的**点数上限**。程序传的是内存里的点数组、点数由它自己给 ——
    /// 不设上限的话，一个写错的大数会让宿主去读几十万个点（每次读还要做越界检查），
    /// 界面直接卡住。512 个点足够画任何真实图形（一张地图轮廓也不过几百个点）。
    /// </summary>
    public const int MaxPolyPoints = 512;
}

public static class VmlVmDefaults
{
    /// <summary>虚拟机内存（字节）。</summary>
    public const int MemoryBytes = 16 * 1024 * 1024;

    /// <summary>栈上限（字节）。见类注释里那条"先内存后栈"的顺序要求。</summary>
    public const int StackBytes = 1024 * 1024;

}

public sealed class VmlMessageQueue
{
    /// <summary>
    /// 队列上限。
    ///
    /// <para><b>为什么要有它（v0.96.433）</b>：这个队列原来**没有上限** —— 一条 `Queue`，
    /// 投递方是定时器回调与 UI 事件（都在别的线程上跑，**不看程序读得多快**），
    /// 消费方是 VML 程序自己（`ui_wait_msg` / `ui_poll`）。一旦程序某段时间读得慢
    /// （比如正在跑一段长循环、或被模态弹框挡住），投递就会一直堆：
    /// **内存涨、每条消息的处理开销也跟着涨**，而且**程序那边完全看不出**
    /// （它只会觉得"消息怎么越读越多"）。</para>
    ///
    /// <para>**挑大值、丢最旧**：4096 条在日常（键盘/触摸/定时器）根本到不了 ——
    /// 真到了就说明程序已经不正常了，这时**保新的**比保旧的更有用
    /// （输入类消息里，后到的那个才是"现在的手指在哪"）。</para>
    ///
    /// <para>⚠ **丢一条必须同时把它的许可吃掉**，否则就破坏了本类反复强调的那条不变量
    /// 「许可数 == 队列长度」（见 <see cref="Post"/> 的注释）—— 那会造出"计数 &gt; 0
    /// 而队列为空"的假信号，让 `ui_wait_msg` 空转。做法与 <see cref="TryRead"/> 一致。</para>
    /// </summary>
    public const int MaxMessages = 4096;

    private readonly Queue<VmlMessage> _queue = new();
    private readonly Lock _lock = new();
    private readonly SemaphoreSlim _signal = new(0);
    private bool _overflowWarned;

    /// <summary>当前待处理条数。</summary>
    public int Count
    {
        get { lock (_lock) return _queue.Count; }
    }

    /// <summary>
    /// 投递一条消息（UI 线程调用）。
    ///
    /// ⚠ **每条消息一个许可，无条件 Release** —— 这行原来是
    /// `if (_signal.CurrentCount == 0) _signal.Release();`（"信号量只是个唤醒开关"），
    /// 而那个写法与下面 <see cref="TryRead"/> 的"不消费许可"配在一起就**破坏了不变量**：
    /// 队列可以被 TryRead 取空而信号量计数还留着，于是
    /// 「计数 &gt; 0 但队列是空的」成立 ⇒ 一次**空唤醒**，<see cref="Read"/> 立刻返回 null。
    ///
    /// 实测症状（`scripts/vmlcli-verify/run.sh` 抓住的）：程序连读两条消息之后装一个定时器，
    /// 60ms 后定时器明明投了消息，`ui_wait(msg, 2000)` 却**当场返回 0（"超时"）** ——
    /// 程序那边完全看不出是宿主的账没对上，只会以为"这一拍没有事件"。
    /// 电话端同一份代码同一套账，症状是 `ui_wait(msg, 0)` 偶发空转（该睡的时候在空跑）。
    /// </summary>
    public void Post(VmlMessage msg)
    {
        lock (_lock)
        {
            // 到顶了就**丢最旧的一条**再进新的 —— 见 `MaxMessages` 的说明。
            // ⚠ 丢弃必须连着吃掉它的许可：队列长度减一，许可也得减一，不然就破了
            //   「许可数 == 队列长度」这条不变量（下面 Post 的注释与 TryRead 都在守它）。
            if (_queue.Count >= MaxMessages)
            {
                _queue.Dequeue();
                _signal.Wait(0);
                if (!_overflowWarned)
                {
                    _overflowWarned = true;
                    ErrorLog.Warning("VmlMessageQueue",
                        $"消息队列已达上限 {MaxMessages} 条，最旧的被丢弃 —— " +
                        "程序很可能有一段时间没在取消息（长循环 / 模态弹框 / 卡住）");
                }
            }
            _queue.Enqueue(msg);
        }
        _signal.Release();
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
            if (keep) return _queue.Peek();     // 只看不取：不消费许可（队头那条还在）
            var msg = _queue.Dequeue();
            // **取走一条 = 用掉一个许可**：许可数与队列长度必须一一对应，
            // 否则会留下"计数 > 0 而队列为空"的假信号（见 Post 的注释）。
            // `Wait(0)` 永不阻塞，且不进 `_lock`，所以放在锁里不会死锁。
            _signal.Wait(0);
            return msg;
        }
    }

    /// <summary>非阻塞取一条（消费）；无消息返回 null。</summary>
    public VmlMessage? TryTake() => TryRead(keep: false);

    /// <summary>
    /// **丢掉队列里所有满足条件的、还没被消费的**消息，返回丢掉的条数。
    ///
    /// <para>
    /// 存在的理由：移动类消息（`TouchMove` / `MouseMove`）是**"追最新位置"**的语义 ——
    /// 旧的位置**毫无价值**，而程序的主循环是"一次取一条"（`ui_wait_msg`）——
    /// 手指拖动一秒能产生几百条 ⇒ **产生的比消费的快，队列只涨不落**。
    /// 真机实测（gorilla 连续拖滑条 6 轮）：队列 285 → 596 → 1050 → 1481 → 2010 → **2280**
    /// 条，**完全不回落**；而同一时间 fps 全程 19~21。用户看到的正是
    /// 「**背景绘图不卡、但触摸要等一下才反应**」—— 他的触摸事件排在那两千多条后面。
    /// </para>
    ///
    /// <para>
    /// ⚠ **只该丢移动类**。键盘 / 定时器 / 触摸的按下抬起都承载**离散语义**：
    /// 丢一条就少一次按键、少一拍物理 —— 那是换一种 bug。
    /// </para>
    ///
    /// <para>
    /// ⚠ 许可（<see cref="_signal"/>）的账与 <see cref="TryRead"/> 同一条不变量：
    /// **丢一条就 `Wait(0)` 一次**。队列长度减一而许可不减，就会留下"计数 &gt; 0 而队列为空"
    /// 的假信号，让 `ui_wait_msg` 空转（见 <see cref="Post"/> 的注释）。
    /// </para>
    /// </summary>
    public int DropPending(Func<VmlMessage, bool> predicate)
    {
        lock (_lock)
        {
            int n = _queue.Count;
            if (n == 0) return 0;
            int dropped = 0;
            for (int i = 0; i < n; i++)
            {
                var msg = _queue.Dequeue();
                if (predicate(msg)) { dropped++; _signal.Wait(0); }
                else _queue.Enqueue(msg);      // 不匹配的**保持原顺序**放回去
            }
            return dropped;
        }
    }

    /// <summary>
    /// 非阻塞取**第一条满足条件**的消息（消费），其余消息保持原顺序不动；没有则返回 null。
    ///
    /// <para>
    /// 用途只有一个：老的 BASIC「字符输入」接口（`INKEY$` / `getch()`）要的是**键盘**那一路，
    /// 而不是"队头那一条"。直接 <see cref="TryTake"/> 会把排在键盘前面的**定时器/鼠标**消息
    /// 一起吃掉 —— 那些消息的程序侧消费者（`ui_poll_msg`）再也看不到，
    /// 表现是"装好的定时器偶尔不响"，而排查时会先去怀疑程序。
    /// </para>
    /// <para>
    /// ⚠ 许可（<see cref="_signal"/>）的账：**取走一条就 `Wait(0)` 一次**，与
    /// <see cref="TryRead"/> 同一条不变量（许可数与队列长度一一对应，见 <see cref="Post"/>）。
    /// 被跳过的消息不消费许可 —— 它们还在队列里，许可数就该还留着。
    /// </para>
    /// </summary>
    public VmlMessage? TryTakeWhere(Func<VmlMessage, bool> predicate)
    {
        lock (_lock)
        {
            int n = _queue.Count;
            if (n == 0) return null;
            VmlMessage? found = null;
            for (int i = 0; i < n; i++)
            {
                var msg = _queue.Dequeue();
                if (found is null && predicate(msg)) { found = msg; continue; }
                _queue.Enqueue(msg);
            }
            if (found is null) return null;
            _signal.Wait(0);
            return found;
        }
    }

    /// <summary>
    /// 阻塞读一条，最多等 <paramref name="timeoutMs"/> 毫秒（0 = 无限等）。
    /// 超时返回 null。**阻塞方是 VM 线程**，不要从 UI 线程调。
    /// <paramref name="keep"/> 见 <see cref="TryRead"/>。
    /// </summary>
    public VmlMessage? Read(int timeoutMs, bool keep, CancellationToken ct = default)
    {
        // 先看队列：有就直接拿走，不走信号量（比等一趟再醒更省）
        if (TryRead(keep) is { } first) return first;

        // **醒了不等于有货**：`keep`（只看队头）不消费许可，多个读者并发时也会互相抢，
        // 所以醒来之后要回头再看一眼，没有就继续等**剩下的**时间。
        // 从前这里等一次、看一次就返回 —— 一次空唤醒会被上层读成"超时/没有事件"，
        // 而调用方（比如 `ui_wait(msg, 2000)`）完全看不出是宿主的账没对上。
        var deadline = timeoutMs <= 0 ? long.MaxValue : Environment.TickCount64 + timeoutMs;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var remaining = timeoutMs <= 0
                ? Timeout.Infinite
                : (int)Math.Max(0, deadline - Environment.TickCount64);
            if (remaining == 0) return null;
            if (!WaitPostOrCancel(remaining, ct)) return null;
            if (TryRead(keep) is { } msg) return msg;
        }
    }

    /// <summary>
    /// 等一条消息被投递（最多 <paramref name="timeoutMs"/> 毫秒；&lt;=0 表示无限），
    /// **同时盯着取消令牌** —— 令牌一响就抛 <see cref="OperationCanceledException"/>。
    /// 返回 true = 信号量到手，false = 超时。
    ///
    /// ⚠ 为什么不能只写 `_signal.Wait(remaining)`：**令牌响了它也不知道**。VM 的令牌检查是
    ///   **每条指令一次**，而此刻 VM 线程根本不在执行指令 —— 它正睡在这里等消息。
    ///   于是"取消一个卡在宿主等待里的程序"完全无效：用户实测就是
    ///   「旧 BGI 程序，退出弹窗后程序还没结束」—— 那类程序结尾是 `getch()`，
    ///   就停在这个等待上，关窗口 / 强制停止都叫不醒它。`WaitAny` 把令牌一起等，
    ///   取消才能真正落地（抛出去 → 穿出宿主 → 终止整个运行）。
    /// </summary>
    private bool WaitPostOrCancel(int timeoutMs, CancellationToken ct)
    {
        // 没有令牌时**一个字都不改**（老路径：桌面自测、非取消场景）
        if (!ct.CanBeCanceled) return _signal.Wait(timeoutMs);
        // ⚠ `SemaphoreSlim` **不是** `WaitHandle`（CS0826：两者没有公共隐式类型）——
        //   能进 `WaitAny` 的是它的 `AvailableWaitHandle`（计数 > 0 时有信号，且**不消费计数**）。
        //   这里等它、再由上面的 `TryRead` 用 `_signal.Wait(0)` 消费一个许可，语义与老路径一致。
        var idx = WaitHandle.WaitAny(new[] { _signal.AvailableWaitHandle, ct.WaitHandle }, timeoutMs);
        if (idx == WaitHandle.WaitTimeout) return false;
        // idx==1 = 取消令牌那一头醒了。CancellationToken 的等待句柄只在真的取消时才置位，
        // 所以这里直接抛 —— 不要"当成可能来消息了回去再看"，那会变成自旋。
        if (idx == 1) throw new OperationCanceledException(ct);
        return true;
    }

    /// <summary>阻塞取一条（消费），最多等 <paramref name="timeoutMs"/> 毫秒。超时返回 null。</summary>
    public VmlMessage? Take(int timeoutMs, CancellationToken ct = default) => Read(timeoutMs, keep: false, ct);

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
/// **窗口种类** —— 宿主拿它决定"这一扇窗要按哪套规矩来"。
///
/// <para>
/// ⚠ **跨语言契约，只能末尾追加**（与 <see cref="VmlShape"/> 同规矩）：改值或插队
/// 会让已经编出来的程序行为漂移。C 侧对应 `waycoder_ui.h` 的 `VML_WINKIND_*`。
/// </para>
/// </summary>
public enum VmlWinKind
{
    /// <summary>
    /// **图形窗口**（默认）：触摸 + 手柄，画布自适应缩放、可跟随旋转。
    /// `WIN_OPEN`(#520) / `WIN_OPEN_EX`(#570) 开出来的都是这一种。
    /// </summary>
    Graphic = 0,

    /// <summary>
    /// **电脑屏窗口**：坐标系固定为程序声明的分辨率（永不重排）、触摸只当鼠标、
    /// 带屏幕键盘代替手柄区。由 `WIN_OPEN_PC`(#582) 开出来。
    /// </summary>
    PcScreen = 1,
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
    /// 窗口种类（见 <see cref="VmlWinKind"/>）。默认 <see cref="VmlWinKind.Graphic"/> ——
    /// **老窗口开出来就是它**，行为一字不改。
    /// </summary>
    public VmlWinKind Kind { get; set; } = VmlWinKind.Graphic;

    /// <summary>
    /// 要不要**屏幕键盘**（`WIN_OPEN_PC` 的 R4）。默认 false ——
    /// 只有电脑屏窗口会把它置上，图形窗口那条路一个字都不变。
    /// </summary>
    public bool NeedKeyboard { get; set; } = false;

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

    /// <summary>
    /// 清屏：清空图元并置背景色。
    ///
    /// <para><b>录制期（`ui_create_block` 之后、`ui_end_block` 之前）的语义 = 「从零开始录」</b>：
    /// 只清录制缓冲，**不动场景**。理由：图块是**透明底叠加**，块里那句 clear 若在贴出时
    /// 执行就会擦掉整幅帧 —— 那不是任何人的意图。唯一合理的读法就是"重录"。
    /// ⚠ 反过来说，**录制期清场景**是这条路上最毒的一种错：用户在"开始录一个块"时
    /// 会看到**整幅画面消失**，`ui_end_block` 之后只重画增量 ⇒ 少一整层背景。</para>
    /// </summary>
    public void Clear(uint background)
    {
        bool recording;
        lock (_figures)
        {
            recording = _recording is not null;
            if (recording) { _recording!.Clear(); _recordingWarned = false; }
            else           { _figures.Clear(); _overflowWarned = false; }
        }
        var bgChanged = Background != background;
        Background = background;
        // 录制期画面**没变** ⇒ 不动 `Version`；唯一例外是背景色真的换了 ——
        // 那是看得见的变化，而老程序（不调 `ui_present`）的出图判据就是 `Version`，
        // 不涨的话"开窗 → 录制期换底 → 从不贴块"的程序会永远停在旧底色上。
        if (!recording || bgChanged) Version++;
    }

    // ── 参数防护 ──────────────────────────────────────────────────────────
    //
    // 宿主 syscall 的参数是**程序给的**，可以是任何 int：坐标可以到 ±20 亿、半径可以是负数、
    // 文本可以指向垃圾内存。这些值会一路进 DSL、再进光栅 / 矢量后端（那里有 int 转换、缓冲区
    // 分配、路径展平、字形排版），是「异常值把宿主搞崩」的主要入口。
    //
    // **统一收口放在这一层**（而不是各宿主各写一遍）：所有端 —— 手机 / 桌面 / Web / GUI ——
    // 的图元都从这里进场景，而且这一层在桌面上可自测。
    //
    // 策略是「**让异常参数最多画不出来，绝不崩**」：
    //   · 坐标超出窗口 → 整个图元丢弃（屏幕外的东西本来也看不见，丢掉还省内存）
    //   · 尺寸 / 半径 / 线宽 / 字号 → 负数钳 0、超大钳到窗口（保住"很大"的语义，不撑爆）
    //   · 计数与字符串长度 → 封顶
    // 正常程序传的都是合理值，一个也不会被丢。

    /// <summary>坐标窗口（±100 万）：比任何合理画布坐标大几个数量级，又远小于 int 溢出边界。</summary>
    public const int CoordLimit = 1_000_000;

    /// <summary>图元表上限。程序漏了 <c>ui_clear</c> 时图元只增不减（每帧几十上百条），
    /// 这是**宿主侧的兜底** —— 到顶丢弃后续图元并记一次警告，绝不让程序的一行疏忽拖垮宿主。</summary>
    public const int MaxFigures = 12_000;

    /// <summary>
    /// 刷子表上限（<see cref="Brush"/> 造的句柄）。
    ///
    /// 句柄是**小整数**（1..本值），与颜色值（`0xAARRGGBB`）不共用命名空间 ——
    /// 这条边界要求本值**远小于 0x01000000**；改大到那个量级就会开始把颜色误判成句柄。
    /// </summary>
    public const int MaxBrushes = 128;

    // ── 矢量图块（ui_create_block / ui_end_block / ui_draw_block[_at]）──────────
    //
    // 图块 = **录制下来的一串绘图指令**（不是像素）。贴的时候用 DSL 的
    // `push/translate/rotate/scale` 套一层变换重放 ⇒ 放大不糊、旋转免费、
    // 内存与图块尺寸无关。像素那层（GET_IMAGE/PUT_IMAGE）保留给老程序，两者并存。

    /// <summary>
    /// 图块表上限（<see cref="CreateBlock"/> 录出来的句柄）。
    ///
    /// 与 <see cref="MaxBrushes"/> 同值**不是巧合**：句柄都是"小整数、0 = 没有"，
    /// 同值让这条约定只有一处解释。块比刷子重（一个块 = N 行字符串），
    /// 但 128 个精灵对游戏绰绰有余。
    ///
    /// ⚠ **块表不随 `ui_clear` 清**（与匿名刷子同族）。所以"每帧 create 一遍"的程序
    /// 会在 128 帧后拿不到句柄 —— 那正是这道上限要挡的形状。告警文案里点明了这一条，
    /// 否则用户只看到"超过上限 128"而不知道病根。
    /// </summary>
    public const int MaxBlocks = 128;

    /// <summary>
    /// 单个图块能录多少行。
    ///
    /// <para><b>这是防呆，不是预算</b>：录了忘了 `ui_end_block` 会让录制缓冲**无上限地涨**，
    /// 而 `_figures` 那道 <see cref="MaxFigures"/> 兜底**根本管不到它**（录制期不走那条分支）。
    /// 2048 行够画复杂精灵，且"2048 行 × 贴 5 次"正好落在图元预算边缘、能被软阈值提醒到。</para>
    /// </summary>
    public const int MaxBlockLines = 2_048;

    /// <summary>
    /// 缩放上限（千分比）。1000 = 原尺寸，本值 = 100 倍。
    ///
    /// 3000 的翻转：**下限不是 1 而是"必须 &gt; 0"** —— `scale 0` 会让矢量后端的
    /// `TryAxisScale`（要求 `A&gt;0 &amp;&amp; D&gt;0`）失败而掉进逐像素的慢路径，
    /// 所以 `≤0` 直接判为"不画"并返回失败码，让程序自己能看见。
    /// </summary>
    public const int MaxBlockScalePerMille = 100_000;

    /// <summary>
    /// 一个录好的图块：块内 DSL 行 + 两个用于预警的缓存位。**句柄 = 下标 + 1**（0 = 没有）。
    ///
    /// 行是 `string` 引用、贴 N 次只花 N 个指针（不是 N 份拷贝）—— 所以"贴 200 次"的
    /// 内存代价是 200×8 字节，不是 200×N 行。
    /// </summary>
    private sealed class Block
    {
        public string[] Lines = [];

        /// <summary>块尺寸（`ui_create_block` 给的）—— **中心版贴图要用它算 `-w/2 -h/2`**。</summary>
        public int W;
        public int H;

        /// <summary>块里有没有 `image` 图元 —— 见 <see cref="DrawBlock"/> 里那条预警。</summary>
        public bool HasImage;
    }

    /// <summary>
    /// 图块表：**句柄 → 块**。
    ///
    /// ⚠ 从前是 `List&lt;Block&gt;` 而句柄 = 下标+1 —— 那样**没法释放**：删掉中间一个，
    /// 后面所有句柄都会平移（旧句柄指向别的块，静默画错东西）。
    /// 所以换成"句柄字典 + 回收栈"：句柄只增不减、释放过的**回收再用**，
    /// 任何一个已发出的句柄要么指向它那块，要么已经失效（贴的时候静默 no-op）。
    /// </summary>
    private readonly Dictionary<int, Block> _blocks = new();

    /// <summary>下一个新句柄（从 1 开始，0 恒为"没有"）。</summary>
    private int _nextBlockHandle = 1;

    /// <summary>释放出来的句柄，下次 `CreateBlock` 优先复用。</summary>
    private readonly Stack<int> _freeBlockHandles = new();

    /// <summary>`CreateBlock` 已经占下、等 `EndBlock` 落表的那个句柄（没在录时 0）。</summary>
    private int _pendingHandle;

    /// <summary>正在录制的内容（非 null = 处于录制期）。见 <see cref="Add"/> 的分流。</summary>
    private List<string>? _recording;

    /// <summary>本次录制的图块尺寸（`EndBlock` 时才连同内容一起落表）。</summary>
    private int _recordingW;
    private int _recordingH;

    private bool _recordingWarned;       // 录制缓冲超限
    private bool _blockOverflowWarned;   // 块表满
    private bool _nestedBlockWarned;     // 录制期又开了一个
    private bool _orphanEndWarned;       // 没在录就 end
    private bool _blockBudgetWarned;     // 本帧图元预算被贴块占满
    private bool _blockImageWarned;      // 旋转贴含 image 的块（会掉出矢量后端）

    /// <summary>单条文本 / 路径串的长度上限（DSL 文本随它线性膨胀）。</summary>
    public const int MaxTextLength = 4_096;

    /// <summary>坐标是否在窗口内。越界的坐标一律视为无效图元。</summary>
    public static bool InCoordRange(int v) => v > -CoordLimit && v < CoordLimit;

    /// <summary>尺寸类参数（宽 / 高 / 半径 / 线宽 / 字号）：负数钳 0，超大钳到窗口。</summary>
    public static int Dim(int v) => v < 0 ? 0 : (v > CoordLimit ? CoordLimit : v);

    /// <summary>按**码点**截断，别把代理对切成两半（emoji / CJK 扩展 B 占两个 char）。</summary>
    private static string CapText(string s)
    {
        if (s.Length <= MaxTextLength) return s;
        var end = MaxTextLength;
        if (char.IsHighSurrogate(s[end - 1]) && char.IsLowSurrogate(s[end])) end--;
        return s[..end];
    }

    /// <summary>图元超限只报警一次（否则每个被丢的图元一条日志，反而把日志刷爆）。</summary>
    private bool _overflowWarned;

    /// <summary>点 —— DSL 没有单像素指令，用 1×1 的填充矩形表达（语义等价，且复用现成光栅路径）。</summary>
    public void AddPixel(int x, int y, uint color)
    {
        if (!InCoordRange(x) || !InCoordRange(y)) return;
        Add($"rect {x} {y} 1 1 {Hex(color)}");
    }

    public void AddLine(int x1, int y1, int x2, int y2, uint color, int width)
    {
        if (!InCoordRange(x1) || !InCoordRange(y1) || !InCoordRange(x2) || !InCoordRange(y2)) return;
        width = Dim(width);
        Add($"line {x1} {y1} {x2} {y2} {Hex(color)}{(width > 0 ? " " + width : "")}");
    }

    /// <summary>矩形；<paramref name="radius"/> &gt; 0 时走 DSL 的 roundrect（圆角矩形）。
    /// <paramref name="fillGradient"/> 非空时用**渐变刷子**填充（DSL 的 `@id` 引用）。</summary>
    // ── 绘图状态：全局透明度（`ui_gfx` 的 Alpha 操作）──────────────────────
    //
    // ⚠ 它**在宿主侧就乘进颜色了**（见 `Style`），所以后端三条路都不必知道它的存在。
    //   代价是**渐变与图片不受它影响**（那两样的颜色不经过 `Style`）—— 这条限制
    //   写在 `docs/VML宿主接口.md` 里，别指望它。
    private int _alpha = 255;

    /// <summary>设置全局透明度 0..255（对**之后**的图元生效）。</summary>
    public void SetAlpha(int a)
    {
        if (a < 0) { a = 0; }
        if (a > 255) { a = 255; }
        _alpha = a;
        Add($"alpha {a}");
    }

    /// <summary>
    /// 释放本窗口累积的**全部画刷 / 渐变定义**（`ui_brush_reset`）。
    ///
    /// 与图像（`FreeImage` 594）/ 图块（`FreeBlock` 593）不同，画刷的句柄是**下标**，
    /// 删单个会让后面的错位 ⇒ 只能整体重置。**调用时机是程序自己的事**：
    /// 按需造渐变的程序应当在每帧开头调一次，否则定义会一直累积到上限。
    /// </summary>
    /// <summary>
    /// 清空整个裁剪栈（见 `GfxOp.ClipReset`）。
    ///
    /// ⚠ **不新造一条 DSL 指令**，而是**按当前深度补发 N 个 `clippop`** ——
    ///   `Canvas` / `IVectorTarget` / SVG 那边一个字都不用改，语义天然一致
    ///   （"栈空了再弹"三处都已经是 no-op）。多一条新指令就要三处各实现一遍，
    ///   而那正是"同一规则三处实现"的老坑。
    /// </summary>
    /// <summary>`mask_begin` —— 开始收集蒙版形状（这期间的形状不上屏）。</summary>
    public void AddMaskBegin()
    {
        _maskBuf = new List<MaskShape>();
        Add("mask_begin");
    }

    /// <summary>`mask_end [inside]` —— 收下蒙版并**取代**当前蒙版（老语义，画面必须逐像素不变）。</summary>
    public void AddMaskEnd(int inside)
    {
        _maskInside = inside != 0;
        ApplyMaskSegment(MaskOp.Replace);
        Add($"mask_end {(inside != 0 ? 1 : 0)}");
    }

    /// <summary>
    /// `mask_end2 [op]` —— 收下蒙版并与**当前蒙版**按布尔运算符组合（`Replace` 等价于老的那条）。
    ///
    /// ⚠ 出图那条路的段累积在 `DrawRunner.Parse`（解析期）—— 这里**另记一份**，
    ///   只服务 `ui_mask_test`（运行时问"这个点在不在蒙版里"，那时解析期还没跑）。
    ///   两处**共用** `MaskExpr.ApplySegment` 的累积语义与 `MaskShape` 的构造，
    ///   差异只在"形状从哪来"：那边从解析好的 `DrawFigure` 提取，这边从方法参数直接构造。
    /// </summary>
    public void AddMaskEnd2(int op)
    {
        ApplyMaskSegment(op);
        Add($"mask_end2 {op}");
    }

    /// <summary>
    /// 取消蒙版。**不新造指令**：开一个空的再收 —— 空形状在 `MaskExpr.IsEmpty` 里
    /// 判为"没有蒙版"，于是"后续图元全部可见"。与 `ResetClips` 补发 `clippop` 同一路数。
    /// </summary>
    public void AddMaskClear()
    {
        AddMaskBegin();
        AddMaskEnd(1);
    }

    /// <summary>
    /// `layer_begin` —— 开始**离屏收集**：这期间画的图元先落到一块临时画布上
    /// （`LayerCommand` 的注释里有完整说明）。
    ///
    /// ⚠ 与蒙版**方向相反**：层内图元**是要上屏的**（只是晚一步、经过一次合成），
    ///   所以它需要一块真的临时画布 + 一次逐像素合成 —— 不是"一组判定用的几何"。
    /// </summary>
    public void AddLayerBegin() => Add("layer_begin");

    /// <summary>`layer_end [alpha]` —— 结束收集，**整层**按 `alpha`（0..255）合成上去。</summary>
    public void AddLayerEnd(int alpha)
        => Add($"layer_end {Math.Clamp(alpha, 0, 255).ToString(System.Globalization.CultureInfo.InvariantCulture)}");

    // ── 蒙版的**运行时镜像**（只服务 `ui_mask_test`）─────────────────────
    //
    // 出图时，蒙版的段累积发生在 `DrawRunner.Parse`；而"这一点在不在蒙版里"是
    // **运行时**的问题，那时场景还没被解析。宿主手里只有自己刚发出去的那几行文本，
    // 所以必须自己记一份。
    //
    // ⚠ 记账**只在 `lock (_figures)` 里做** —— 与 `Add` 用的是同一把锁，
    //   否则"收集到一半被别的线程读走"会给出一个半成品的蒙版。
    private List<MaskShape>? _maskBuf;                 // `mask_begin` 之后收集的形状
    private readonly List<MaskExpr.Segment> _maskSegs = new();
    private bool _maskInside = true;

    /// <summary>`mask_begin` 期间画的形状**顺手记一份**（图形与解析期同源，见上面那段）。</summary>
    private void NoteMaskShape(MaskShape s)
    {
        lock (_figures) { _maskBuf?.Add(s); }
    }

    /// <summary>收一段：`Replace` 清掉历史，其余追加（与 `DrawRunner.Parse` 同一语义）。</summary>
    private void ApplyMaskSegment(int op)
    {
        lock (_figures)
        {
            MaskExpr.ApplySegment(_maskSegs, op, _maskBuf);
            _maskBuf = null;
        }
    }

    /// <summary>
    /// 当前生效的蒙版（没有就返回 null = 处处可见）。给 `ui_mask_test` 用。
    /// ⚠ 返回的是**快照**（段与形状列表都另建一份）—— 调用方拿去算碰撞时，
    ///   程序可能正在改蒙版，共用同一个 `List` 会在枚举中途被改。
    /// </summary>
    public MaskExpr? CurrentMask()
    {
        lock (_figures)
        {
            if (_maskSegs.Count == 0) return null;
            var e = new MaskExpr { Inside = _maskInside };
            foreach (var seg in _maskSegs)
            {
                var copy = new MaskExpr.Segment { Op = seg.Op };
                copy.Shapes.AddRange(seg.Shapes);
                e.Segments.Add(copy);
            }
            return e.IsEmpty ? null : e;
        }
    }

    public void ResetClips()
    {
        while (_clipDepth > 0) { AddClipPop(); }
    }

    /// <summary>查当前占用；未知种类返回 -1。见 `GfxOp.ResCount`。</summary>
    public int ResCount(int what)
    {
        switch (what)
        {
            case 0: lock (_figures) { return _figures.Count; }
            case 1: return -2;          // 图像在宿主那一层（`VmlHostRuntime._images`）
            case 2: lock (_figures) { return _blocks.Count; }
            case 3: lock (_figures) { return _brushes.Count; }
            default: return -1;
        }
    }

    public void ResetBrushes()
    {
        lock (_figures)
        {
            _brushes.Clear();
            _gradientNames.Clear();
            _solidBrushCache.Clear();
        }
    }

    /// <summary>把当前的全局透明度乘进一个颜色的 alpha 通道。</summary>
    private uint ApplyAlpha(uint c)
    {
        if (_alpha >= 255) return c;
        var a = (uint)((c >> 24) & 0xFF);
        a = a * (uint)_alpha / 255u;
        return (c & 0x00FFFFFFu) | (a << 24);
    }

    /// <summary>`clip` —— 压入一级矩形裁剪（与上一级求交），直到对应的 `clippop`。</summary>
    /// <summary>当前压了几级裁剪 —— 只为 `ResetClips` 补弹用（**别拿它当"后端已压几级"**）。</summary>
    private int _clipDepth;

    public void AddClipPush(int x, int y, int w, int h)
    {
        w = Dim(w); h = Dim(h);
        _clipDepth++;
        Add($"clip {x} {y} {w} {h}");
    }

    /// <summary>`clippop` —— 弹出一级裁剪。</summary>
    public void AddClipPop()
    {
        if (_clipDepth > 0) { _clipDepth--; }
        Add("clippop");
    }

    public void AddRect(int x, int y, int w, int h, uint color, bool filled, int width, int radius,
        string? fillGradient = null)
    {
        if (!InCoordRange(x) || !InCoordRange(y)) return;
        w = Dim(w); h = Dim(h); radius = Dim(radius); width = Dim(width);
        var name = radius > 0 ? "roundrect" : "rect";
        var extra = radius > 0 ? $" {radius}" : "";
        Add($"{name} {x} {y} {w} {h}{extra}{Style(color, filled, width, fillGradient)}");
        // 蒙版收集期：顺手记一份（`roundrect` 当矩形，圆角忽略 —— 与解析期的口径一致）。
        // ⚠ 先判 `_maskBuf` 再构造形状：这两个方法是**每帧几百次**的热路径，
        //   不加判断就是每次白构造一个小对象（收集期才是少数）。
        if (_maskBuf != null) NoteMaskShape(MaskShape.Rect(x, y, w, h));
    }

    public void AddCircle(int cx, int cy, int r, uint color, bool filled, int width, string? fillGradient = null)
    {
        if (!InCoordRange(cx) || !InCoordRange(cy)) return;
        var rr = Dim(r);
        Add($"circle {cx} {cy} {rr}{Style(color, filled, Dim(width), fillGradient)}");
        if (_maskBuf != null) NoteMaskShape(MaskShape.Circle(cx, cy, rr));
    }

    public void AddEllipse(int cx, int cy, int rx, int ry, uint color, bool filled, int width, string? fillGradient = null)
    {
        if (!InCoordRange(cx) || !InCoordRange(cy)) return;
        Add($"ellipse {cx} {cy} {Dim(rx)} {Dim(ry)}{Style(color, filled, Dim(width), fillGradient)}");
    }

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
        _gradientNames.Add(safe);   // 防 BRUSH 的匿名名（_b{n}）与用户起的名字撞车
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
        d = CapText(d);
        var sb = new StringBuilder();
        // ⚠ 引号与**换行**都要抹掉：DSL 是**按行**解析的，`d` 里一个 `\n` 就能伪造出一条
        //   完整指令；引号则会提前结束字符串、把后面的坐标挤成新的 token。
        sb.Append("path \"").Append(d.Replace('"', ' ').Replace('\n', ' ').Replace('\r', ' ')).Append('"');
        sb.Append(' ').Append(Hex(strokeColor));
        if (width > 0) sb.Append(' ').Append(Num(width));
        sb.Append(" ").Append(CapName(cap));
        if (dashed) sb.Append(" dash");
        if (!string.IsNullOrEmpty(fillGradient)) sb.Append(" fill @").Append(VmlUi.SafeId(fillGradient));
        else if (fillSet) sb.Append(" fill ").Append(Hex(fillColor));
        Add(sb.ToString());
        // 蒙版收集期：路径按**展平后的多边形**计入（与解析期同一个展平器）
        NoteMaskPath(d);
    }

    /// <summary>
    /// 把一条 `d` 展平后逐条子路径记成多边形形状（蒙版运行时镜像用）。
    /// ⚠ 与 `DrawRunner.ExtractMaskShapes` 走的是**同一个** `DrawPath.Flatten`，
    ///   所以"路径当蒙版"画出来的形状和这里是同一个。
    /// </summary>
    private void NoteMaskPath(string d)
    {
        if (_maskBuf == null) return;
        foreach (var sp in DrawPath.Flatten(d))
        {
            if (sp.Points.Count < 3) continue;
            var flat = new List<double>(sp.Points.Count * 2);
            foreach (var (px, py) in sp.Points) { flat.Add(px); flat.Add(py); }
            NoteMaskShape(MaskShape.Polygon(flat));
        }
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
        // 蒙版收集期：多边形原样计入（`FlatPoints` 已经钳过坐标，与上面发出去的同一份）
        if (_maskBuf != null)
        {
            var flat = new List<double>(points.Count);
            foreach (var v in points) flat.Add(v);
            if (flat.Count >= 6) NoteMaskShape(MaskShape.Polygon(flat));
        }
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
        if (!InCoordRange(x) || !InCoordRange(y)) return;
        if (string.IsNullOrEmpty(text)) return;
        text = CapText(text);
        fontSize = Dim(fontSize);
        var w = (style & TextBold) != 0 ? "bold" : "";
        var i = (style & TextItalic) != 0 ? "italic" : "";
        var bi = (w.Length > 0 && i.Length > 0) ? " bi" : (w.Length > 0 ? " bold" : (i.Length > 0 ? " italic" : ""));
        Add($"text {x} {y} \"{Escape(text)}\" {fontSize} {Hex(color)} {AnchorName(anchor)}{bi}");
    }

    /// <summary>
    /// 带**刷子 token** 的文字：`#AARRGGBB`（纯色）或 `@渐变id`（渐变）。文字渐变的入口。
    ///
    /// ⚠ 这是**新增的重载**，不是给上面那个换参数 —— `AddText(…, uint color, …)` 是
    /// `VmlScene` 的公开 API（`AddIcon`、以及将来别的自定义控件都在用），
    /// 把 `uint` 换成 `string` 是**破坏性改动**。重载之后两边都留得下：
    /// 传 `uint` 的落老的那个，传 `string` 的落这个。
    /// </summary>
    public void AddText(int x, int y, string text, string colorToken, int fontSize, int anchor, int style = 0, int vAlign = 0)
    {
        if (!InCoordRange(x) || !InCoordRange(y)) return;
        if (string.IsNullOrEmpty(text)) return;
        text = CapText(text);
        fontSize = Dim(fontSize);
        var w = (style & TextBold) != 0 ? "bold" : "";
        var i = (style & TextItalic) != 0 ? "italic" : "";
        var bi = (w.Length > 0 && i.Length > 0) ? " bi" : (w.Length > 0 ? " bold" : (i.Length > 0 ? " italic" : ""));
        Add($"text {x} {y} \"{Escape(text)}\" {fontSize} {colorToken} {AnchorName(anchor)}{VAnchorName(vAlign)}{bi}");
    }

    /// <summary>
    /// 带**竖对齐**的文字（<see cref="DrawTextEx"/> 的落点）。
    /// <paramref name="vAlign"/>：0=顶（= 老行为）1=中 2=底。
    /// </summary>
    public void AddTextEx(int x, int y, string text, uint color, int fontSize, int anchor, int vAlign, int style = 0)
        // 落 `AddText` 那条（它管 DSL 拼装）—— 粗/斜体的记号拼装**只有那一份**，
        // 从前这里抄了一份，两边迟早会不同步（本仓头号坑）。
        => AddText(x, y, text, Hex(color), fontSize, anchor, style, vAlign);

    /// <summary>竖对齐的 DSL 记号 —— **`v` 前缀**，与横锚点的 `middle` 不重名（同名两义只能靠猜）。</summary>
    private static string VAnchorName(int v) => v switch
    {
        1 => " vcenter",
        2 => " vbottom",
        3 => " vtop",                  // 盒顶落在 y（要显式写：它与 0 的渲染相差一个"上升"）
        _ => "",                       // 0 = 基线 = 老行为：**不写这个词**，产物与从前逐字相同
    };

    /// <summary>当前文字属性（<see cref="SetFont"/> 设、<see cref="Text"/> 用）。宿主侧状态，不占 VML 内存。</summary>
    public int FontSize { get; set; } = 16;
    /// <summary>当前文字样式位（见 <see cref="TextBold"/>/<see cref="TextItalic"/>）。</summary>
    public int FontStyle { get; set; }
    /// <summary>当前文字颜色（0xAARRGGBB）。</summary>
    public uint FontColor { get; set; } = 0xFFFFFFFF;
    /// <summary>当前文字锚点（0=左 1=中 2=右）。</summary>
    public int FontAnchor { get; set; }

    /// <summary>
    /// 当前文字**竖对齐**（见 <see cref="VAlignTop"/> 等）—— 由 `ui_set_valign` 设（号段 #586）。
    /// 与 <see cref="FontSize"/> 那几个同一套：状态式，`ui_text_cur` 用。
    /// </summary>
    public int FontVAlign { get; set; }

    // 四档的编号是**跨语言契约**（C 头文件 `VML_VANCHOR_*` 与各语言绑定都按这几个数写死）。
    // 编号按"历史行为优先"排：**0 留给老行为**，新档位往后加 —— 这样任何不设它的老程序
    // 渲染逐字不变（改 0 的含义 = 悄悄挪动所有既有程序的文字，那是不可接受的）。
    /// <summary>竖对齐：**y 就是基线**（字形坐在基线上）。**默认 = 老行为**（手机端一直如此渲染）。</summary>
    public const int VAlignBase = 0;
    /// <summary>竖对齐：盒竖直中心落在 y（"在方框/圆里居中"用这一档）。</summary>
    public const int VAlignMiddle = 1;
    /// <summary>竖对齐：盒底落在 y。</summary>
    public const int VAlignBottom = 2;
    /// <summary>竖对齐：盒顶落在 y（与基线相差一个"上升"，≈0.8×字号）。</summary>
    public const int VAlignTop = 3;

    /// <summary>按当前属性画一行字（<see cref="Text"/> 号段的实现体，放这里便于自测）。</summary>
    public void AddTextCurrent(int x, int y, string text)
        // 颜色走**颜色 token**（纯色 `#AARRGGBB` 或刷子的 `@渐变id`）⇒ 落 `AddText` 那个
        // 字符串重载，不是收 `uint` 的 `AddTextEx`（两者不通用）。
        => AddText(x, y, text,
            _hasTextBrush && _textToken != null ? _textToken : Hex(FontColor),
            FontSize, FontAnchor, FontStyle, FontVAlign);

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
        if (!InCoordRange(x) || !InCoordRange(y)) return;
        size = Dim(size);
        if (!Icons.TryGetValue(name.Trim().ToLowerInvariant(), out var ch))
        {
            AddRect(x, y, size, size, color, filled: false, width: 2, radius: 0);
            return;
        }
        Add($"text {x} {y} \"{ch}\" {size} {Hex(color)} start");
    }

    /// <summary>
    /// 一条**实心水平游程** —— 种子填充（`FLOOD_FILL`）的落笔方式。
    ///
    /// <para>
    /// 与 <see cref="AddShape"/> 走当前样式不同，这里**把颜色写死在这一行里**：
    /// 填充算出来的每条游程颜色都一样，但**不该受"当前填充刷子"影响** ——
    /// 老程序的 `floodfill(x,y,border)` 灌的是 `setfillstyle` 设的那个色，
    /// 而那条状态在共享层已经解析成了具体颜色传下来，再走一次样式反而会串。
    /// </para>
    /// </summary>
    public void AddFilledRun(int x, int y, int w, int h, uint color)
    {
        if (w <= 0 || h <= 0) return;
        if (!InCoordRange(x) || !InCoordRange(y)) return;
        Add($"rect {x} {y} {Dim(w)} {Dim(h)} {Hex(color)}");
    }

    public void AddImage(int x, int y, string path, int w, int h)
    {
        if (!InCoordRange(x) || !InCoordRange(y)) return;
        w = Dim(w); h = Dim(h);
        if (w == 0 || h == 0) return;                 // 0 尺寸画不出东西，直接丢（也挡住退化调用）
        // ⚠ **字段序是 `x y w h "路径"`**（尺寸在路径**前**）—— 以 `ImageCommand.Parse`
        //   与它在 `DrawCommands.cs` 的头注释为准（自测 `SelfTest.Chunk10/23` 也按这个序写）。
        //   此前这里发的是 `x y "路径" w h`，与解析侧**对不上** ⇒ `Parse` 在第 3 个字段上
        //   读到字符串、`DrawParse.Num` 返回 NaN ⇒ 返回 null ⇒ **这张图静默消失**，
        //   只在 stderr 留一行"这一帧的 DSL 有解析问题"。
        //   实测：BGI 的 `getimage`/`putimage`（精灵保存-贴回）画面上什么都不出，
        //   而 `imagesize` 返回正常、`getpixel` 也读得到原位置 —— 一个错误都不报。
        Add($"image {x} {y} {w} {h} \"{Escape(path)}\"");
    }

    private void Add(string line)
    {
        lock (_figures)
        {
            // ── 录制期：行进**录制缓冲**，不进场景、**也不 `Version++`** ─────────────
            //
            // 这是整个矢量图块功能**唯一**的侵入点。选这里是因为 `_figures.Add` 全文件
            // 只出现一次，而所有 `AddXxx`（`AddRect`/`AddCircle`/`AddText`/`AddImage`…）
            // 都经它 —— 在这一个地方分流，**全部绘图指令自动支持录制**，既有逻辑一行不改。
            //
            // ⚠ **不能 `Version++`**：`Version` 是"画面变了"的判据（老程序不调 `ui_present`
            //   时宿主的出图信号）。录制期画的东西**没有上屏**，涨了会让宿主白刷几十次
            //   —— 表现为闪烁 + 耗电，而 `FigureCount` 一切正常、**没有任何断言抓得到**。
            //
            // ⚠ 录制期**不做** `MaxFigures` 那道检查：那条数的是场景图元，与录制缓冲无关。
            //   录制缓冲有自己的上限（`MaxBlockLines`），否则"开了录却忘了 end"会让它无上限地涨，
            //   而 `_figures` 那道兜底**根本管不到它**。
            if (_recording is not null)
            {
                if (_recording.Count >= MaxBlockLines) { WarnRecordingOverflow(); return; }
                _recording.Add(line);
                return;
            }

            // **宿主侧兜底**：程序漏了 `ui_clear` 时图元只增不减，不能让它把宿主拖垮。
            if (_figures.Count >= MaxFigures)
            {
                if (!_overflowWarned)
                {
                    _overflowWarned = true;
                    ErrorLog.Warning("VmlScene",
                        $"场景图元数超过上限 {MaxFigures}，后续绘制被丢弃 —— 程序很可能漏了 ui_clear()");
                }
                return;
            }
            _figures.Add(line);
        }
        Version++;
    }

    /// <summary>
    /// 拼 DSL 用的**复用缓冲区**。
    ///
    /// <para><b>为什么要有它（v0.96.433）</b>：真机 logcat 实测**每 ~200ms 就一次
    /// `Explicit concurrent mark compact GC`**（每次回收 ~350KB，约 1.75MB/s 的分配速率），
    /// 而每帧的渲染耗时只有 1ms —— 分配全在这个函数里。
    /// 原来每帧 `new StringBuilder()`（默认容量 16）再一路拼到 ~7KB，
    /// **中间要反复扩容 16→32→…→8192**，那些中间 `char[]` 全是要 GC 的垃圾。
    /// 现在容量一次给足并跨帧复用：**一帧只分配最后那一个字符串**。</para>
    ///
    /// <para>⚠ 只在 <see cref="_figures"/> 那把锁里用（`BuildDsl` 已经持有它）——
    /// 复用的缓冲区**不是线程安全的**，多线程同时拼会互相踩内容。</para>
    /// </summary>
    private readonly StringBuilder _dslBuf = new(64 * 1024);

    /// <summary>把场景翻成绘图 DSL（首行是 <c>canvas</c> 头）。宿主把它交给 DrawRunner 出图。</summary>
    public string BuildDsl()
    {
        // 复用缓冲区 —— 见 `_dslBuf` 的说明（跨帧不 new，省掉扩容产生的中间数组）
        var sb = _dslBuf;
        sb.Clear();

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
    private string Style(uint color, bool filled, int width, string? fillGradient = null)
    {
        color = ApplyAlpha(color);   // 全局透明度在这里一次生效（形状的填充与描边都走它）
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

    // ══════════════════════════════════════════════════════════════════════
    // 刷子与样式状态（`BRUSH` #575 / `SET_STYLE` #576 / `DRAW_SHAPE` #574）
    //
    // 骨架只有三件事：**一张刷子表**（句柄 → 进 DSL 的那个 token）、
    // **三个样式槽**（填充 / 画笔 / 文字）、**每个形状按当前样式拼一行 DSL**。
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 刷子表：下标 + 1 = 句柄（句柄从 1 起，**0 一律表示"没有"**）。
    /// <c>Token</c> 是进 DSL 的那个词（纯色是 <c>#AARRGGBB</c>、渐变是 <c>@名字</c>）；
    /// <c>Fallback</c> 是"这个刷子退化成一个颜色时会是什么"—— 渐变取起始色。
    /// </summary>
    private readonly List<(string Token, uint Fallback)> _brushes = new();
    private readonly Dictionary<uint, int> _solidBrushCache = new();
    private int _anonSeq;
    private bool _brushOverflowWarned;

    // 样式状态。默认：填充不透明白、**不描边**、文字跟随 ui_set_font 的颜色。
    private string? _fill = "#FFFFFFFF";
    private string? _pen;
    /// <summary>文字刷子的 token（`#AARRGGBB` 或 `@渐变id`）；null = 没设过，跟随 `FontColor`。</summary>
    private string? _textToken;
    private uint _textColor;
    private bool _hasTextBrush;
    private int _penWidth = 1, _penCap, _penDash, _penArrow;
    private readonly HashSet<string> _degradedWarned = new(StringComparer.Ordinal);

    /// <summary>当前填充 token（<c>@id</c> 或 <c>#AARRGGBB</c>）；null = 不填充。</summary>
    public string? FillToken => _fill;

    /// <summary>纯色刷子（**同色复用**，循环里反复造也安全）。满了返回 0。</summary>
    public int AddSolidBrush(uint argb)
    {
        if (_solidBrushCache.TryGetValue(argb, out var hit)) return hit;
        if (_brushes.Count >= MaxBrushes) { WarnBrushOverflow(); return 0; }
        _brushes.Add((Hex(argb), argb));
        var handle = _brushes.Count;
        _solidBrushCache[argb] = handle;
        return handle;
    }

    /// <summary>渐变刷子：定义一条匿名渐变（名字 <c>_b{n}</c>），再返回引用它的句柄。</summary>
    public int AddGradientBrush(bool radial, uint colorA, uint colorB, int a1, int a2, int a3, int a4)
    {
        if (_brushes.Count >= MaxBrushes) { WarnBrushOverflow(); return 0; }
        string name;
        do { _anonSeq++; name = "_b" + _anonSeq; } while (_gradientNames.Contains(name));
        _gradientNames.Add(name);
        AddGradient(name, radial, colorA, colorB, a1, a2, a3, a4);
        _brushes.Add(("@" + name, colorA));
        return _brushes.Count;
    }

    /// <summary>
    /// 按名字引用一个**已经用 `ui_gradient` 定义过**的渐变。
    ///
    /// ⚠ 这是"引用型"刷子：它自己不定义渐变，所以**依赖程序当帧先调过 `ui_gradient`**，
    /// 而 `ui_clear` 会把那条定义清掉 —— 与 `BRUSH_LINEAR/RADIAL` 造的匿名刷子
    /// （窗口态、`ui_clear` 不清）**生命周期不同**。三种生命周期都写在这里了，别混。
    /// </summary>
    public int AddNamedBrush(string name)
    {
        var safe = VmlUi.SafeId(name);
        if (safe.Length == 0) return 0;
        for (var i = 0; i < _brushes.Count; i++)
            if (_brushes[i].Token == "@" + safe) return i + 1;   // 同名复用，不产生重复定义
        if (_brushes.Count >= MaxBrushes) { WarnBrushOverflow(); return 0; }
        _brushes.Add(("@" + safe, 0xFF000000u));
        return _brushes.Count;
    }

    /// <summary>句柄 → 样式 token；0 或越界返回 null（= 该槽"没有"）。</summary>
    public string? BrushToken(int handle)
        => handle >= 1 && handle <= _brushes.Count ? _brushes[handle - 1].Token : null;

    /// <summary>
    /// 「句柄**或**颜色」→ 样式 token。
    ///
    /// 两者值域**天然不重叠**（句柄 1..<see cref="MaxBrushes"/>，颜色 ≥ 0x01000000），
    /// 所以 `ui_set_fill(0xFF2A3346)` 直接传颜色是合法的 —— 这正是
    /// 「颜色 = 只有一个色标的刷子」在**实现**上的落点，不必强制先 `ui_brush_solid`。
    /// </summary>
    private string TokenFor(int handleOrColor)
    {
        if (handleOrColor >= 1 && handleOrColor <= _brushes.Count) return _brushes[handleOrColor - 1].Token;
        return Hex((uint)handleOrColor);
    }


    /// <summary>设置一个样式槽。<paramref name="slot"/> 见 <see cref="VmlStyleSlot"/>。</summary>
    public bool SetStyle(int slot, int brush, int width, int cap, int dash, int arrow)
    {
        switch (slot)
        {
            case VmlStyleSlot.Fill:
                _fill = brush == 0 ? null : TokenFor(brush);
                return true;
            case VmlStyleSlot.Pen:
                // 画笔槽**收渐变刷子**（描边渐变已落地）：`_pen` 直接带刷子 token，
                // 纯色是 `#...`、渐变是 `@_bn` —— 解析侧两条路都认。
                _pen = brush == 0 ? null : TokenFor(brush);
                _penWidth = width <= 0 ? 1 : Math.Min(width, CoordLimit);
                _penCap = cap;
                _penDash = dash != 0 ? 1 : 0;
                _penArrow = arrow;
                return true;
            case VmlStyleSlot.Text:
                // 文字槽**收渐变**（v0.96.311 起渐变文字已落地）—— 与填充/画笔两个槽一致。
                _textToken = brush == 0 ? null : TokenFor(brush);
                _hasTextBrush = _textToken != null;
                // `TextBrushColor` 是给"只认纯色"的老消费方留的**向后兼容**读取口；
                // 拿到渐变时取它的起始色（不再告警 —— 从前那句"该槽只支持纯色"现在不成立了）。
                if (_hasTextBrush)
                    _textColor = ParseHex(_textToken!.StartsWith('@')
                        ? Hex(brush >= 1 && brush <= _brushes.Count ? _brushes[brush - 1].Fallback : 0xFF000000u)
                        : _textToken);
                return true;
            default:
                return false;
        }
    }

    /// <summary>当前画笔（描边）token；null = 不描边。</summary>
    public string? PenToken => _pen;

    /// <summary>当前文字刷子的颜色；没设过时 <see cref="_hasTextBrush"/> 为假，跟随 ui_set_font。</summary>
    public uint TextBrushColor => _textColor;
    /// <summary>是否设过文字刷子。</summary>
    public bool HasTextBrush => _hasTextBrush;

    /// <summary>`#AARRGGBB` → uint。只喂我们自己拼出来的串（<see cref="Hex"/> 的产物）。</summary>
    private static uint ParseHex(string s)
        => uint.TryParse(s.TrimStart('#'), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v) ? v : 0;

    private readonly HashSet<string> _gradientNames = new(StringComparer.Ordinal);

    private void WarnBrushOverflow()
    {
        if (_brushOverflowWarned) return;
        _brushOverflowWarned = true;
        ErrorLog.Warning("VmlScene", $"刷子数超过上限 {MaxBrushes}，后续 BRUSH 返回 0（句柄 0 = 没有刷子）。");
    }

    // ══════════════════════════════════════════════════════════════════════
    // 矢量图块：录制 / 重放
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// `ui_create_block(w, h, color)` —— **开始录制**后续绘图指令。返回句柄（≥1），失败 0。
    ///
    /// <para>**`color` 是保留位**：图块永远**透明叠加**（只画你画的东西，没画到的地方透出背景）。
    /// 传什么都没有区别 —— 不铺底是有意的，块要能叠在任何东西上面。</para>
    ///
    /// <para>失败两种情况：**已经在录了**（嵌套 create，返回 0，已录的内容不打断）、
    /// **块表满**（<see cref="MaxBlocks"/>）。</para>
    /// </summary>
    public int CreateBlock(int w, int h, uint color)
    {
        w = Dim(w); h = Dim(h);
        lock (_figures)
        {
            if (_recording is not null) { WarnNestedBlock(); return 0; }
            if (_blocks.Count >= MaxBlocks) { WarnBlockOverflow(); return 0; }
            _recording = new List<string>();
            _recordingW = w;
            _recordingH = h;
            _recordingWarned = false;
            // 句柄**先占好**、`EndBlock` 时才连同内容落表 ——
            // 这样程序可以拿着句柄立刻传给别的函数，而表里不会出现"半成品块"。
            // ⚠ 上限判据用 `_blocks.Count`（**活着的块**）而不是句柄值：
            //   释放过的句柄会被回收复用，句柄值可以无限涨。
            _pendingHandle = _freeBlockHandles.Count > 0 ? _freeBlockHandles.Pop() : _nextBlockHandle++;
            return _pendingHandle;
        }
    }

    /// <summary>`ui_end_block()` —— 结束录制。返回句柄（与 create 给的一致）；没在录时返回 0。</summary>
    public int EndBlock()
    {
        lock (_figures)
        {
            if (_recording is null) { WarnOrphanEnd(); return 0; }
            var lines = _recording;
            _recording = null;
            // 记下"块里有没有 image" —— 旋转/非等比贴它会让整个窗口掉出矢量后端，
            // 那件事没有别的时机能发现（见 `Stamp` 里那条预警）。
            var hasImage = false;
            foreach (var l in lines)
                if (l.StartsWith("image ", StringComparison.Ordinal)) { hasImage = true; break; }
            _blocks[_pendingHandle] = new Block
            {
                Lines = lines.ToArray(),
                W = _recordingW,
                H = _recordingH,
                HasImage = hasImage,
            };
            return _pendingHandle;   // 与 `CreateBlock` 返回的那个一致
        }
    }

    /// <summary>`ui_free_block(block)` —— 释放一个图块（句柄回收，见 <see cref="VmlUi.BlockOp.Free"/>）。</summary>
    public bool FreeBlock(int block)
    {
        lock (_figures)
        {
            if (!_blocks.Remove(block)) return false;   // 句柄不存在 / 已释放：静默失败
            _freeBlockHandles.Push(block);
            return true;
        }
    }

    /// <summary>
    /// `ui_draw_block(block, x, y, sx, sy, rot)` —— 贴图块。**(x, y) 是图块中心，绕中心旋转。**
    ///
    /// 缩放用**千分比**（1000 = 原尺寸、2000 = 两倍），角度用**度**。成功 1 / 失败 0。
    /// </summary>
    public bool DrawBlock(int block, int x, int y, int sx, int sy, int rotate)
        => Stamp(block, x, y, sx, sy, rotate, center: true);

    /// <summary>
    /// `ui_draw_block_at(block, x, y, sx, sy, rot)` —— 同上，但 **(x, y) 是图块左上角、绕左上角转**。
    ///
    /// 与 <see cref="DrawBlock"/> 的差别只在多一条平移（把"局部原点在左上角"挪成"原点在中心"），
    /// 见 `Stamp` 里的推导。
    /// </summary>
    public bool DrawBlockAt(int block, int x, int y, int sx, int sy, int rotate)
        => Stamp(block, x, y, sx, sy, rotate, center: false);

    /// <summary>
    /// 贴图块的共同实现。
    ///
    /// <para><b>变换序列（顺序不能改，改了在非等比缩放下才显形）</b>：</para>
    /// <code>
    /// 左上角版：  push / translate X Y / rotate R / scale SX SY / …块内行… / pop
    /// 中心版  ：  push / translate X Y / rotate R / scale SX SY / translate -W/2 -H/2 / …块内行… / pop
    /// </code>
    /// <para>解析器是 `current = current.Compose(X)`（最后发的最外层），所以行序就是"从外到内"。
    /// 中心版那条额外平移把"局部左上角原点"挪到中心：局部 `(W/2, H/2)` 经它变成 `(0,0)`，
    /// 于是缩放旋转都不动它，最终正好落在 `(X,Y)` —— 这才是"绕中心转"。</para>
    ///
    /// <para>⚠ **不能用 3 参的 `rotate deg px py`**：它作用在**缩放之前**的那个中心上，
    /// `sx == sy` 时碰巧对，`sx != sy` 时绕错点 —— 而画面看起来只是"整体偏了一点"，
    /// 最容易被当成"程序坐标算错了"。</para>
    ///
    /// <para>⚠ **必须"先缩后转"**（`T∘R∘S`）：反过来是"沿旋转后的轴缩放"，
    /// `sx != sy` 时会把精灵拉成**平行四边形（错切）**。</para>
    /// </summary>
    private bool Stamp(int block, int x, int y, int sx, int sy, int rotate, bool center)
    {
        if (!InCoordRange(x) || !InCoordRange(y)) return false;
        // `scale 0` 会让矢量后端的 `TryAxisScale`（要求 A>0 && D>0）失败而掉进逐像素慢路径；
        // "缩到看不见"直接判失败，让程序自己能看见（见 MaxBlockScalePerMille 的说明）。
        //
        // ⚠ **负值是合法的**（水平/垂直**镜像**）—— 原来这里写的是 `sx <= 0`，把负值
        //   连同 0 一起拒了，于是"同一个精灵朝左朝右"只能录两个图块、两段近乎重复的形状代码。
        //   镜像在矢量后端同样过不了 `TryAxisScale`（它不认负数）⇒ 那一帧回退整窗光栅，
        //   但**光栅那边是对的**（`FillTransformed` 走逆变换，负缩放照样可逆）——
        //   拿"慢一点"换掉一整类重复代码，划算。钳制要**两侧都夹**（见下）。
        if (sx == 0 || sy == 0) return false;
        sx = Math.Max(-MaxBlockScalePerMille, Math.Min(sx, MaxBlockScalePerMille));
        sy = Math.Max(-MaxBlockScalePerMille, Math.Min(sy, MaxBlockScalePerMille));
        rotate = ((rotate % 360) + 360) % 360;

        int need;
        Block blk;
        lock (_figures)
        {
            // 句柄 0 / 越界 ⇒ **静默 no-op**（与"句柄 0 = 没有刷子"同一套：不为它告警）
            if (!_blocks.TryGetValue(block, out blk!)) return false;   // 句柄 0 / 已释放 / 越界

            // **整块原子化**：先算这次要占多少行，超预算就整块不贴。
            // 逐行 `Add` 到顶会出现「`push` 进了、`pop` 被丢」的**悬空 push** ——
            // 今天看不出来纯属巧合（到顶后所有 `Add` 都被丢），一旦将来给 `Add` 加了
            // "到顶兜底"，悬空 push 会让**后面所有图元继承块的局部变换**（画面整体歪掉）。
            need = blk.Lines.Length + (center ? 6 : 5);
            if (_figures.Count + need > MaxFigures) { WarnBlockBudget(); return false; }
        }

        if (blk.HasImage && (rotate != 0 || sx != sy)) WarnRotatedImageBlock();

        // 缩放换算**只在这一处**（千分比 → 倍数）。别在别处再写一遍 ——
        // `AddGradient` 那段注释记的就是"两端各写一次换算"的血账。
        Add("push");
        Add($"translate {Num(x)} {Num(y)}");
        if (rotate != 0) Add($"rotate {Num(rotate)}");
        Add($"scale {Num(sx / 1000.0)} {Num(sy / 1000.0)}");
        // ⚠ `W/2.0` 不能写成整数除法：W = 101 时整数给 -50，中心偏 0.5px
        if (center) Add($"translate {Num(-blk.W / 2.0)} {Num(-blk.H / 2.0)}");
        foreach (var l in blk.Lines) Add(l);
        Add("pop");
        return true;
    }

    private void WarnRecordingOverflow()
    {
        if (_recordingWarned) return;
        _recordingWarned = true;
        ErrorLog.Warning("VmlScene",
            $"图块录制超过上限 {MaxBlockLines} 行，后续绘制被丢弃 —— 很可能忘了 ui_end_block()。");
    }

    private void WarnBlockOverflow()
    {
        if (_blockOverflowWarned) return;
        _blockOverflowWarned = true;
        ErrorLog.Warning("VmlScene",
            $"图块数超过上限 {MaxBlocks}，后续 ui_create_block 返回 0。"
            + "⚠ 图块表**不随 ui_clear 清** —— 若程序是每帧 create 一遍，请改成造一次、之后一直贴。");
    }

    private void WarnNestedBlock()
    {
        if (_nestedBlockWarned) return;
        _nestedBlockWarned = true;
        ErrorLog.Warning("VmlScene",
            "已经在录制一个图块时又调了 ui_create_block —— 本次返回 0，原来的录制不受影响。");
    }

    private void WarnOrphanEnd()
    {
        if (_orphanEndWarned) return;
        _orphanEndWarned = true;
        ErrorLog.Warning("VmlScene", "没在录制时调了 ui_end_block —— 返回 0。");
    }

    private void WarnBlockBudget()
    {
        if (_blockBudgetWarned) return;
        _blockBudgetWarned = true;
        ErrorLog.Warning("VmlScene",
            $"本帧贴图块已占满图元预算（上限 {MaxFigures}），这一块整块未贴 —— "
            + "把块做小一点，或减少同屏数量。");
    }

    private void WarnRotatedImageBlock()
    {
        if (_blockImageWarned) return;
        _blockImageWarned = true;
        ErrorLog.Warning("VmlScene",
            "旋转或非等比缩放一个**含图片（image）**的图块：矢量后端不支持这个组合，"
            + "整个窗口会**永久回退光栅后端**（帧耗时约 1ms → 80ms）。"
            + "要旋转的精灵请用矢量图元拼，或预先生成多角度贴图。");
    }

    /// <summary>
    /// 形状的样式尾巴：`[fill] [stroke] [width] [cap] [dash]`。
    ///
    /// 用的是 DSL 的**位置约定**（第一个颜色 = 填充、第二个 = 描边，见 <c>DrawParse.ParseStyle</c>）——
    /// 这条约定早于刷子模型，且被 10 条指令共用，不动它。
    /// 空心图形必须把填充写成全透明色而不是省略 —— 省了的话颜色位会被描边占去。
    /// </summary>
    private string StyleTail()
    {
        var sb = new StringBuilder();
        sb.Append(' ').Append(_fill ?? "#00000000");
        if (_pen != null)
        {
            sb.Append(' ').Append(_pen);
            if (_penWidth > 0) sb.Append(' ').Append(_penWidth);
            if (_penCap == 1) sb.Append(" round");
            else if (_penCap == 2) sb.Append(" square");
            if (_penDash != 0) sb.Append(" dash");
        }
        return sb.ToString();
    }

    /// <summary>只管描边的图元（`line` / `polyline`）的样式尾巴：`[色] [width] [cap] [dash]`。</summary>
    private string PenTail()
    {
        var sb = new StringBuilder();
        sb.Append(' ').Append(_pen ?? "#00000000");
        if (_penWidth > 0) sb.Append(' ').Append(_penWidth);
        if (_penCap == 1) sb.Append(" round");
        else if (_penCap == 2) sb.Append(" square");
        if (_penDash != 0) sb.Append(" dash");
        return sb.ToString();
    }

    /// <summary>按 <see cref="VmlShape"/> 的码画一个形状（样式取当前状态）。未知码返回 false。</summary>
    public bool AddShape(int shape, int a1, int a2, int a3, int a4, int a5, int a6, int a7)
    {
        switch (shape)
        {
            case VmlShape.Rect:
                if (!InCoordRange(a1) || !InCoordRange(a2)) return false;
                // ⚠ 判据是**半径 > 0**（`a5`），不是"高 > 0"（`a4`）—— 后者恒真，
                // 于是 `rect` 那条分支**永远不会走到**（一直是死代码）。改成 a5 之后
                // 半径 0 走 `rect`、半径 > 0 走 `roundrect`，与两条 DSL 指令各自的
                // ParseStyle 起点（4 / 5）也就对上了。几何上两者等价：RoundRect 把 r 钳到 ≥0，
                // r=0 时退化成"四个角点各重复 13 次"的退化多边形，四条实边正是矩形四边。
                Add(a5 > 0
                    ? $"roundrect {a1} {a2} {Dim(a3)} {Dim(a4)} {Dim(a5)}{StyleTail()}"
                    : $"rect {a1} {a2} {Dim(a3)} {Dim(a4)}{StyleTail()}");
                return true;
            case VmlShape.Circle:
                if (!InCoordRange(a1) || !InCoordRange(a2)) return false;
                Add($"circle {a1} {a2} {Dim(a3)}{StyleTail()}");
                return true;
            case VmlShape.Ellipse:
                if (!InCoordRange(a1) || !InCoordRange(a2)) return false;
                Add($"ellipse {a1} {a2} {Dim(a3)} {Dim(a4)}{StyleTail()}");
                return true;
            case VmlShape.Line:
                if (!InCoordRange(a1) || !InCoordRange(a2)
                    || !InCoordRange(a3) || !InCoordRange(a4)) return false;
                // 画笔要箭头时改发 `arrow` —— 那是 DSL 里**另一条指令**（主干 + 两条箭头边），
                // 几何在 `ArrowCommand.Head` 里、三条后端共用。这是"一个绘制调用两种形态"，
                // 所以放在这里选指令名，而不是让 `line` 自己长出箭头参数。
                // ⚠ 本批只做**末端**箭头：`arrow` 指令的几何就是"从起点指向终点"，
                //   起端/两端箭头要 `ArrowCommand.Head` 支持反向，还没做。
                Add($"{(_penArrow != 0 ? "arrow" : "line")} {a1} {a2} {a3} {a4}{PenTail()}");
                return true;
            case VmlShape.Star:
                return AddStar(a1, a2, a3, a4, a5, a6);
            case VmlShape.Regular:
                return AddRegular(a1, a2, a3, a4, a5);
            case VmlShape.Ring:
                return AddRing(a1, a2, a3, a4);
            case VmlShape.Pie:
                return AddPie(a1, a2, a3, a4, a5);
            case VmlShape.Heart:
                return AddHeart(a1, a2, a3);
            default:
                return false;
        }
    }

    private static int NormDeg(int deg) => ((deg % 360) + 360) % 360;

    /// <summary>星形（`star cx cy 外半径 内半径 角数 旋转角`）。</summary>
    public bool AddStar(int cx, int cy, int rOut, int rIn, int points, int rot)
    {
        if (!InCoordRange(cx) || !InCoordRange(cy)) return false;
        if (points < 2) points = 2;
        if (points > 4096) points = 4096;
        Add($"star {cx} {cy} {Dim(rOut)} {Dim(rIn)} {points} {NormDeg(rot)}{StyleTail()}");
        return true;
    }

    /// <summary>正多边形（`regular cx cy 半径 边数 旋转角`）。边数 &lt; 3 画不出面。</summary>
    public bool AddRegular(int cx, int cy, int r, int n, int rot)
    {
        if (!InCoordRange(cx) || !InCoordRange(cy)) return false;
        if (n < 3) n = 3;
        if (n > 4096) n = 4096;
        Add($"regular {cx} {cy} {Dim(r)} {n} {NormDeg(rot)}{StyleTail()}");
        return true;
    }

    /// <summary>圆环（`ring cx cy 外半径 内半径`）—— 中间的洞靠奇偶规则挖。</summary>
    public bool AddRing(int cx, int cy, int rOut, int rIn)
    {
        if (!InCoordRange(cx) || !InCoordRange(cy)) return false;
        Add($"ring {cx} {cy} {Dim(rOut)} {Dim(rIn)}{StyleTail()}");
        return true;
    }

    /// <summary>扇形（`pie cx cy 半径 起始角 结束角`，角度制）。</summary>
    public bool AddPie(int cx, int cy, int r, int a0, int a1)
    {
        if (!InCoordRange(cx) || !InCoordRange(cy)) return false;
        Add($"pie {cx} {cy} {Dim(r)} {a0} {a1}{StyleTail()}");
        return true;
    }

    /// <summary>心形（`heart cx cy 尺寸`）。</summary>
    public bool AddHeart(int cx, int cy, int size)
    {
        if (!InCoordRange(cx) || !InCoordRange(cy)) return false;
        Add($"heart {cx} {cy} {Dim(size)}{StyleTail()}");
        return true;
    }

    /// <summary>多边形 / 折线：点数组来自 VML 内存，调用方已读成扁平坐标。</summary>
    public bool AddShapePoly(IReadOnlyList<double> points, bool close)
    {
        var pts = FlatPoints(points);
        if (pts == null) return false;
        Add(close ? $"polygon {pts}{StyleTail()}" : $"polyline {pts}{PenTail()}");
        return true;
    }

    /// <summary>
    /// 路径（SVG 语法）。DSL 里 `path` 的**裸颜色是描边**、填充要写 `fill &lt;色|@id&gt;` ——
    /// 与其它形状"第一个颜色 = 填充"相反，是历史遗留，这里按它的规矩拼。
    /// </summary>
    public bool AddShapePath(string d)
    {
        if (string.IsNullOrEmpty(d)) return false;
        Add($"path \"{Escape(d)}\" stroke {_pen ?? "#00000000"} {(_penWidth > 0 ? _penWidth : 1)} fill {_fill ?? "#00000000"}");
        return true;
    }

    /// <summary>文字（用当前文字属性；文字刷子非空时覆盖颜色）。</summary>
    public bool AddShapeText(int x, int y, string text)
    {
        if (!InCoordRange(x) || !InCoordRange(y)) return false;
        AddTextCurrent(x, y, text);
        return true;
    }


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
