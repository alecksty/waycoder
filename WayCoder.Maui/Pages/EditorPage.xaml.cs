using System.Text;
// 用**别名**而不是 using：`Microsoft.Maui.Controls.Shapes` 里也有一个 `Path`，
// 直接 using 会和 `System.IO.Path` 撞成 CS0104（本文件大量用 Path.xxx）。别名只把
// 我们要的那个类型引进作用域，不牵连别的。
using Shapes = Microsoft.Maui.Controls.Shapes;
// ⚠ **别名**而不是 using：`WayCoder.UI.Shared.Terminal` 里也有一个 `Color`（终端色码那条链的
// 类型），直接 using 会与本文件大量使用的 `Microsoft.Maui.Graphics.Color` 撞成 CS0104
// （命令行页那块控件上踩过同一个坑）。别名只把要用的几个类型引进作用域。
using Term = WayCoder.UI.Shared.Terminal;
using WayCoder.Infra;
using WayCoder.UI.Shared;          // TextEditAssist：自动缩进与括号配对的纯逻辑（可自测）
using WayCoder.Maui.Controls;
using WayCoder.Maui.Markup;
using WayCoder.Maui.Services;
using WayCoder.UI.Tui.Edit;

namespace WayCoder.Maui.Pages;

/// <summary>
/// 内置代码编辑器 —— **自绘 + 单行编辑**。
///
/// 所有行由 <see cref="CodeCanvasView"/> 虚拟化自绘（只画可见的几十行，代价与文件大小无关），
/// 只有**光标所在行在编辑时**浮一个原生 <see cref="Entry"/> 叠在该行上：IME、软键盘、选区、
/// 系统复制粘贴全部复用平台能力，而渲染始终是虚拟化的。这是「100MB 能打开」与「能不丢功能地编辑」
/// 之间唯一同时成立的组合。
///
/// 打开走 <see cref="TextSourceFactory"/>：
/// - ≤ 只读阈值（默认 2MB）→ <see cref="EditableLines"/>，可编辑；
/// - 更大 → <see cref="IndexedTextSource"/>，字节级行索引 + 按需读行，**只读**（编辑要求整份进内存）。
/// </summary>
[QueryProperty(nameof(FilePath), "path")]
public partial class EditorPage : ContentPage
{
    private string _relPath = "";
    private string _fullPath = "";
    private ITextSource? _doc;
    private EditableLines? _editable;
    private readonly EditHistory _history = new();

    private bool _canEdit;                 // 文件够小，允许切到编辑模式
    private bool _fileReadOnly;            // 文件自身的只读属性（文件系统决定，非用户可切）
    private bool _readOnly = true;         // 当前是否只读模式（默认只读）
    private bool _modified;
    private bool _committing;              // 提交编辑时抑制 TextChanged 回写
    private long _editLine = -1;

    /// <summary>
    /// **进入编辑态那一刻**，这一行的原始内容。
    ///
    /// 存在的理由（原来这里是个真 bug）：打字路径（<c>OnLineEditorTextChanged</c>）是**逐键
    /// 直接写模型**的，不压撤销栈；等到 <c>CommitEditingLine</c> 才补压。而它原先是从**模型**里
    /// 读「旧值」的 —— 那时模型早被逐键改写过了，于是 `oldText == newText` 恒成立、
    /// 函数在第一道判断就提前返回 ⇒ **打字这一段编辑压根没进撤销历史**（撤销退不回打字前）。
    /// 旧值只有在进入编辑态时才拿得到，所以必须在这里记下来。
    /// </summary>
    private string _editLineStart = "";           // 正在编辑的行（0-based）
    private long _fileBytes;

    /// <summary>沙箱内相对路径（Shell 路由参数 "path"）。</summary>
    public string FilePath
    {
        set => _ = LoadAsync(value);
    }

    public EditorPage()
    {
        InitializeComponent();

        Canvas.LineTapped += OnLineTapped;
        Canvas.LineLongPressed += OnLineLongPressed;

        // 辅助条的拖动面 —— 走 GraphicsView 的原始触摸（与画布选区手柄同一条路）。
        // 空 Drawable 与「期望尺寸报 0」都在 AssistDragSurface 自己身上（见那个类）。
        AssistDrag.StartInteraction += OnAssistDragStart;
        AssistDrag.DragInteraction += OnAssistDragMove;
        AssistDrag.EndInteraction += OnAssistDragEnd;
        // 被系统打断（来电 / 切走 App / 父容器截走触摸）也是一条结束路径 ——
        // 只清标志不发结束的话，10 秒收表的计时器就永远不再起了。
        AssistDrag.CancelInteraction += (_, _) => { if (_assistPanActive) EndAssistDrag(); };
        // 第一次打开时条子还没被测量过（Width 是 -1）⇒ 弹出动画要等这一下拿到真实宽高再补
        AssistBar.SizeChanged += OnAssistBarSizeChanged;
        // 选区一变就同时刷状态栏与**选区操作条**（选词/扩选/全选/清除都从画布发这个事件）
        Canvas.SelectionChanged += (_, _) => { UpdateStatus(); UpdateSelectionBar(); };
        // 视口一变，条子要跟着选区走（滚出视口时收起来）—— 不跟就会停在原地，
        // 而选区已经滚走了，看着像「操作条在乱飘」
        Canvas.ViewChanged += () =>
        {
            UpdateStatus();
            UpdateSelectionBar();
            // 诊断气泡**不需要在这里跟**：它是画布自己画的，锚点、可见性全在 Draw 里现算
            // （滚动本来就带着一次 Invalidate）。这类「页面替画布算坐标」的活就该没有。
            // 编辑中滚动**不挪输入框**：它是全透明的，只用来换出软键盘，
            // 自带光标一概不用 ⇒ 它浮在屏幕哪个位置都不影响观感。
            // 每帧去挪它反而要更新一个原生控件布局，长列表滚动会掉帧。
            // 输入法候选窗因此可能浮在旧位置 —— 纯外观，可接受。
        };
        // 双指捏合缩放字号，与菜单里的加大/缩小走**同一个** ApplyFontSize
        // （此前这两处是两份几乎逐行相同的拷贝，只改一处就会出现「捏合好了、菜单还是老样子」）。
        //
        // 两个关键点：
        // ① 手势期间**不落盘、不弹提示** —— 触摸事件在 Android 上可达 120~240Hz，而
        //    SetFontSize 会同步写文件（JSON + 临时文件 + File.Move）、ShowToast 会排一个
        //    延迟任务。这类一次性收尾统一挪到 PinchEnded。
        // ② ApplyFontSize 内部「字号没变就直接返回」—— 捏合给的是一串密集的浮点值，
        //    相邻两次常常只差千分之几，在那里被挡掉才是缩放不卡的关键。
        //    （夹取区间由 ApplyFontSize 统一做，这里不必再处理。）
        //    字号**连续可取**：定位走的是平台实测推进量，不是「列号 × 半列宽」，
        //    所以小数号也能做到渲染与光标逐字对齐（见 CodeCanvasView.MeasureAdvances）。
        Canvas.PinchZoomed += size => ApplyFontSize(size, persist: false, toast: false);
        // 输出小窗（自绘命令行网格）的两条接线：
        //   · 捏合 = 缩字号 —— 面板窄而老程序写死 80 列，不缩小就只能横向滚着看半行；
        //     宽度一变要重折（`TerminalGrid` 自己滚，视口一变"能放几列"就变了）。
        //     ⚠ 这里**不落盘**：面板字号是"临时看清这一屏"的取景动作，不该被记住。
        //
        // ⚠ 捏合**分两拍做**（照命令行页）：手势进行中只换字号、**不重折** ——
        //   重折要遍历缓冲里每一行的每个 rune，而触摸事件在 Android 上可达 120~240Hz，
        //   每一拍都重折等于把 UI 线程占满（命令行页那边还实测过"捏一下就没反应了"）。
        //   松手（含被系统打断，`TerminalGrid` 会补发 `PinchEnded`）才重折一次。
        PanelOutputGrid.PinchScaled += size =>
        {
            _panelFont = MauiShellStore.ClampFont(size);
            PanelOutputGrid.SetFontSize(_panelFont);   // 只换字号，内容不动
        };
        PanelOutputGrid.PinchEnded += () => RenderPanelOutput();   // 松手：按新字号重折一次
        PanelOutputGrid.SizeChanged += (_, _) => RefoldPanelOutput();
        _panelChunkHook = OnPanelChunk;    // 装/摘钩子都用这一个实例（见 StopPanelStream）
        Canvas.PinchEnded += () =>
        {
            // 正在编辑时缩放 ⇒ 松手就把字号对齐到整数（见 SnapFontSizeForEditing）。
            // 不在这里弹提示：紧接着那条「字号 N」报的就是对齐后的值，弹两条反而乱。
            if (_editLine >= 0) SnapFontSizeForEditing(toast: false);

            MauiEditorStore.SetFontSize(EditorTypography.FontSize);   // 手势结束才落盘一次
            ShowToast($"字号 {EditorTypography.FontSize:0.#}");
        };
        // 一滑动就结束编辑：编辑态下浮着一个输入框，滚动会让它和自绘的行对不上；
        // 而且滑动本身就意味着「我要浏览」——先把这一行提交掉再滚，最省心。
        // **滚动不再结束编辑**（原来这行是 CommitEditingLine）。
        // 光标已经是自绘的、跟着文字走，没有「控件叠加上去对不上」的顾虑了：
        // 点一下把光标放好之后，滚多远、缩多少，光标都还在它该在的位置；
        // 滚出屏幕自然看不见，滚回来又在了。浮动的输入框由 PositionEditor 跟着走。
        Canvas.ShowDebugHud = MauiEditorStore.ShowDebugHud;

        // 输入框只是个「换出软键盘」的代理，**它自带的光标要关掉** ——
        // 那是平台画的（Android 上红色/蓝色竖条），时不时冒出来看着像「光标乱跳」。
        // Handler 是懒创建的，所以挂在 HandlerChanged 上而不是这里。
        LineEditor.HandlerChanged += (_, _) => HidePlatformCaret();

        // 输入框与画布共用同一份排版常量：字体族/字号/行高/内边距只要有一处不同，
        // 切换编辑的瞬间就会跳一下。
        LineEditor.FontFamily = EditorTypography.FontFamilyName;
        LineEditor.FontSize = EditorTypography.FontSize;
        LineEditor.HeightRequest = 1;
        // ⚠ HeightRequest 只是「请求」，**不是上限**：Entry 在 VerticalOptions=Start 下会按内容
        // 自然高度撑开（13pt 加 EditText 默认内边距实测约 3 个行高），于是它的选区高亮变成
        // 一条跨 3 行的矩形、两个选择手柄落到编辑行下方两行去。文字与光标都是画布画的，
        // 所以只有高亮/手柄会暴露这个失真。MaximumHeightRequest 才是真正的钳制。
        LineEditor.MaximumHeightRequest = 1;
        LineEditor.BackgroundColor = Colors.Transparent;
        // 文字也透明：这一行由画布自绘（见 CodeCanvasView.EditingLine 的注释）。
        // Entry 保留下来只为了三件事——IME 组合输入、软键盘、系统复制粘贴菜单。
        // 两层都显示文字的话，各自的行高/内边距规则不同，必然错位。
        LineEditor.TextColor = Colors.Transparent;
#if IOS || MACCATALYST
        // iOS/MacCatalyst 的**硬件键盘** Tab：UIKit 里 Tab 是「命令键」（不是字符），
        // 只能通过 `UIKeyCommand` 接，而 key command 的 selector 必须**由控件自己的类实现** ——
        // 没法给现成的 MauiTextField 实例挂。所以给编辑器**这一个**输入框换成专用 handler
        // （不动全局 EntryHandler.Mapper，那会波及全 App 的 Entry）。
        // 软键盘不需要它（没有 Tab 键），Android 那条走 KeyPress，Windows 走 PreviewKeyDown。
        LineEditor.Handler = new TabAwareEntryHandler();
#endif

        // 系统光标也藏掉：它由平台按自己的内边距/行内对齐绘制，我们算不出它的位置，
        // 实测就是「在光标行的下方乱飘」。光标改由画布自绘（见 CodeCanvasView.DrawCaret），
        // 于是它的位置和文字出自同一套计算。Entry 仍然负责 IME 与软键盘。
        LineEditor.HandlerChanged += (_, _) =>
        {
#if ANDROID
            if (LineEditor.Handler?.PlatformView is Android.Widget.EditText et)
            {
                HidePlatformCaret(et);
                // **左右内边距清零**：输入框的文字原点是「外边距 + 内边距」，而画布正文的原点是
                // 「行号栏 + 正文左内边距 − 横向滚动」。内边距留着，系统的光标/选择手柄/复制粘贴
                // 浮层就会整体再多偏一个内边距的量。上下不动（行高另有一套钳制）。
                et.SetPadding(0, et.PaddingTop, 0, et.PaddingBottom);
                // 选区底色也交给画布画（画布按整行铺底色）。系统再画一层的话，
                // 两层底色会错开一点，看着像重影。
                et.SetHighlightColor(Android.Graphics.Color.Transparent);   // 绑定里 HighlightColor 只有 getter

                // 硬件键盘的 Tab / Shift+Tab（**软键盘没有这两个键**，所以手机上是接外接键盘才用得到）。
                // 平台默认把 Tab 当**焦点导航**（焦点一走，正在编辑的这一行就断了）——
                // 在 OnKeyListener 里截下来，与 Windows 那条 `PreviewKeyDown` 是同一个语义。
                et.KeyPress -= OnAndroidLineEditorKeyPress;
                et.KeyPress += OnAndroidLineEditorKeyPress;

                // 行首退格 / 选区删除由 `BackspaceAwareEditText` 自己经 `EditorPage.Active`
                // 回调过来（不再在这里「认类型再接线」）。
                // 但**认不出来这件事必须看得见** —— 否则症状就是「擦除键没反应」且毫无线索。
                if (LineEditor.Handler?.PlatformView is not BackspaceAwareEditText)
                {
                    ErrorLog.Warning("EditorPage",
                        "行输入框不是 BackspaceAwareEditText —— 行首退格 / 选区删除会失效", null);
                    ShowToast("⚠ 键盘退格增强未生效（输入框类型不符），请反馈", 5000);
                }
            }
#elif IOS || MACCATALYST
            if (LineEditor.Handler?.PlatformView is UIKit.UITextField tf)
                tf.TintColor = UIKit.UIColor.Clear;
            if (LineEditor.Handler?.PlatformView is TabAwareTextField ttf)
            {
                ttf.TabPressed -= OnIosTabPressed;
                ttf.TabPressed += OnIosTabPressed;
            }
#endif
#if WINDOWS
            // 【Windows】键盘上下键得自己接：平台的单行 TextBox 对 Up/Down **什么都不做**
            // （手机上是靠平台 EditText 那条线把硬件键带进编辑逻辑的，Windows 没有这条线）。
            // 用 PreviewKeyDown（隧道）而不是 KeyDown：先于 TextBox 的类处理器拿到。
            if (LineEditor.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox tb)
            {
                _winKeySink = tb;
                tb.PreviewKeyDown -= OnLineEditorPreviewKeyDown;
                tb.PreviewKeyDown += OnLineEditorPreviewKeyDown;

                // ⚠ **焦点必须等 `Loaded` 再拿**：Handler 刚建好时控件还没进可视树，此刻
                // `Focus()` 返回 false（实测 `loaded=False state=Unfocused`），键盘永远收不到。
                tb.Loaded -= OnKeySinkLoaded;
                tb.Loaded += OnKeySinkLoaded;

                // ⚠ **禁掉 BringIntoView**：这个输入框是我们借来收键盘的「1px 透明代理」，
                // 它一旦被聚焦，WinUI 默认会把它滚进视野 —— 那会连带把整页/画布滚一下，
                // 表现就是「点一下光标闪一下就没了」。聚焦是我们要的，滚动不是。
                tb.BringIntoViewRequested -= OnKeySinkBringIntoView;
                tb.BringIntoViewRequested += OnKeySinkBringIntoView;
            }
#endif
        };
#if ANDROID
        // 光标**只由画布画**（见 CodeCanvasView.DrawCaret）—— 系统的插入光标一个都不留。
        // 每次获得焦点都重申一遍：`setCursorVisible(false)` 会被平台的焦点流程重新打开
        // （EditText 拿到焦点默认就要显示光标），只在 HandlerChanged 里设一次是不够的。
        LineEditor.Focused += (_, _) =>
        {
            if (LineEditor.Handler?.PlatformView is Android.Widget.EditText et) HidePlatformCaret(et);
        };

        // 键盘遮挡 → 压矮内容区。Handler 同样是懒创建的。
        Canvas.HandlerChanged += (_, _) => HookImeInsets();

        // 面板拖动条：Android 上**必须走原生触摸**（拿屏幕绝对坐标），
        // 不能用 MAUI 的 `PanGestureRecognizer` —— 理由见 `HookPanelDragBar` 的长注释。
        PanelDragBar.HandlerChanged += (_, _) => HookPanelDragBar();
#endif
    }

#if IOS || MACCATALYST
    /// <summary>
    /// iOS / MacCatalyst 硬件键盘的 Tab / Shift+Tab —— 与 Windows、Android 同一个语义
    /// （Tab 插制表符、Shift+Tab 退一级缩进）。只读态不动（iOS 上 Tab 本来也不做焦点导航）。
    /// </summary>
    private void OnIosTabPressed(bool shift)
    {
        if (_editLine < 0) return;
        if (shift) DedentLine();
        else InsertIndent();
    }
#endif

#if ANDROID
    /// <summary>
    /// 把平台的插入光标彻底藏掉：**光禁显示不够，还要把光标画笔换成全透明** ——
    /// 平台在获得焦点/重新布局时会按自己的规则把 `CursorVisible` 置回 true，
    /// 那时如果光标画笔还在，屏幕上就会冒出**第二个**光标（与画布自绘的那个错开一点，
    /// 看着就是「光标位置不对」）。两道一起上才稳。
    /// </summary>
    private static void HidePlatformCaret(Android.Widget.EditText et)
    {
        et.SetCursorVisible(false);
        et.SetBackgroundColor(Android.Graphics.Color.Transparent);
        if (OperatingSystem.IsAndroidVersionAtLeast(29) && et.TextCursorDrawable != null)
        {
            var blank = new Android.Graphics.Drawables.ColorDrawable(Android.Graphics.Color.Transparent);
            blank.SetBounds(0, 0, 0, 0);
            et.TextCursorDrawable = blank;   // API 29+ 才有 setTextCursorDrawable
        }
    }
    /// <summary>
    /// Android 硬件键盘的 Tab / Shift+Tab —— 与 Windows 的 <c>OnLineEditorPreviewKeyDown</c>
    /// 同一条语义（Tab 插制表符、Shift+Tab 退缩进、只读态只吞掉不移焦点）。
    ///
    /// 走 `OnKeyListener`（<c>View.KeyPress</c> 事件）而不是 `DispatchKeyEvent`：这个回调**早于
    /// View 的默认处理**，所以平台那套「Tab 移焦点」还没发生就被我们吃掉了。
    /// </summary>
    private void OnAndroidLineEditorKeyPress(object? sender, Android.Views.View.KeyEventArgs e)
    {
        var ke = e.Event;
        if (ke is null) return;
        // Down 与长按重复(Multiple)都算；Up 忽略，否则一次按键会做两遍
        if (ke.Action != Android.Views.KeyEventActions.Down &&
            ke.Action != Android.Views.KeyEventActions.Multiple) return;
        if (ke.KeyCode != Android.Views.Keycode.Tab) return;

        e.Handled = true;                                  // 先吃掉，别让它做焦点导航
        if (_editLine < 0) return;                         // 只读态只吞掉
        if (ke.IsShiftPressed) DedentLine();
        else InsertIndent();
    }

    /// <summary>
    /// 软键盘 inset 的接收器 —— **Android 15+ 上「键盘避让」只能自己接**。
    ///
    /// 起因（真机实测，Android 16 / targetSdk 36）：<c>MainActivity</c> 上的
    /// `WindowSoftInputMode=AdjustResize` **在 edge-to-edge 下已经失效** ——
    /// 键盘弹出前后 dump 出来的页面平台视图是同一个 `(0,0)-(1080,2202)`，画布一点没被压矮，
    /// 于是「点一条靠下的行 → 键盘盖住光标 → 什么都不滚」。官方说法是 15 起 `adjustResize`
    /// 被 deprecated，应用要自己读 `WindowInsets.Type.ime()`。
    ///
    /// ⚠ 返回值**原样传下去、不消费**：inset 是窗口级的，吃掉它会让别的控件一起失去自己的内边距。
    /// </summary>
    private sealed class ImeInsetListener : Java.Lang.Object, AndroidX.Core.View.IOnApplyWindowInsetsListener
    {
        private readonly Action<int> _onImeBottomPx;
        public ImeInsetListener(Action<int> onImeBottomPx) => _onImeBottomPx = onImeBottomPx;

        public AndroidX.Core.View.WindowInsetsCompat? OnApplyWindowInsets(
            Android.Views.View? v, AndroidX.Core.View.WindowInsetsCompat? insets)
        {
            if (insets == null) return null;
            // Java 绑定类型的可空性流分析在这儿不收敛（下面两处 `GetInsets` 会报 CS8602），
            // 上面那行已经判过了，这里显式收一个非空别名。
            var ins = insets!;

            int bottom = 0;
            try
            {
                // ⚠ `WindowInsetsCompat.Type` 在这里是个**嵌套类型**，`Ime()` 是它上面的
                // 静态方法 —— 先 `var t = ...Type;` 再 `t.Ime()` 编不过（CS0119「是一个类型」），
                // 只能全限定写。`GetInsets` 返回的是**可空的** `AndroidX.Core.Graphics.Insets?`
                // （不是 int），直接点 `.Bottom` 会报 CS8602。
                bottom = ins.GetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.Ime())?.Bottom ?? 0;

                // ⚠ **这个值不要再「顺手扣掉导航栏」**。
                // 网上的通行做法是 `ime().bottom - systemBars().bottom`（官方文档也写了
                // ime「may include」导航栏），我照做了一版，结果**反而错了**：
                // 实测（模拟器 Android 16）行号栏结束于 y=1453、状态栏 1453~1517、键盘上沿 1517
                // —— `ime()` 报的就是 1517，**本来就没算进导航栏**；扣掉 64px 之后内容区被多顶
                // 上去一截，状态栏直接掉到键盘底下（截图逐像素比对确认）。
                // 判断依据别靠肉眼看缩放截图：扫一行像素看行号栏底色 (#F2F2F4) 在哪一行结束，
                // 就得到内容区的真实下沿。
            }
            catch { /* 取不到就当没有键盘 —— 不该因为避让失败把页面搞崩 */ }
            try { _onImeBottomPx(bottom); } catch { }
            return insets;
        }
    }

    private ImeInsetListener? _imeListener;
    private double _imePadPt = -1;

    /// <summary>
    /// **没有键盘时**页面平台视图的高度（px）—— 用来判断「系统自己有没有把页面压矮」，
    /// 见 <see cref="ApplyImePad"/>。初值 -1 = 还没量到基线。
    /// </summary>
    private int _pageHeightNoIme = -1;

    /// <summary>
    /// 挂上软键盘 inset 监听，并把遮挡高度换算成根布局的**底部内边距** ——
    /// 也就是把「窗口被键盘压矮」这件事还原出来。
    ///
    /// 之所以选「压矮布局」而不是「只在滚动数学里减去键盘高度」：压矮之后画布的高度、
    /// 命中测试、滚动边界、绘制范围**全部照旧**，只多了一条
    /// <c>CodeCanvasView.OnSizeAllocated</c>（变矮 → 把光标行顶回视口）。
    /// 若改成在滚动数学里减，就得同时维护「两个高度」——画的时候用大的、算边界用小的，
    /// 正是本仓库反复踩的「同一件事两处实现」。
    ///
    /// 挂在**画布**的平台视图上而不是页面自己的：页面视图上已经有 MAUI 自己的 inset 监听
    /// （安全区那套），覆盖它会连带把页面的 inset 处理弄坏；画布是叶子视图，MAUI 不管它。
    /// </summary>
    private void HookImeInsets()
    {
        if (Canvas.Handler?.PlatformView is not Android.Views.View plat) return;

        _imeListener ??= new ImeInsetListener(ApplyImePad);
        AndroidX.Core.View.ViewCompat.SetOnApplyWindowInsetsListener(plat, _imeListener);
        // 监听器是布局之后才挂上的，主动请求一次派发；不然要等下一次窗口变化才拿得到 inset。
        AndroidX.Core.View.ViewCompat.RequestApplyInsets(plat);
    }

    /// <summary>把键盘遮挡的高度（px）换成根布局的底部内边距（pt）。</summary>
    private void ApplyImePad(int imeBottomPx)
    {
        if (Handler?.PlatformView is not Android.Views.View pageView) return;

        double density = DeviceDisplay.MainDisplayInfo.Density;
        if (density <= 0) return;

        // 没有键盘：记下「满高」基线，并把内边距归零。
        if (imeBottomPx <= 0)
        {
            _pageHeightNoIme = pageView.Height;
            SetPad(0);
            return;
        }

        // **系统自己压矮了没有？**
        //
        // `adjustResize` 只在 **Android 15+（targetSdk ≥ 35，强制 edge-to-edge）** 上失效；
        // 同一份 APK 装到 Android 14 及更早的机器上，那条老路**照常生效**、页面已经被系统
        // 压矮了 —— 这时我们再补一次就是**压两遍**（编辑区被挤成一条缝）。
        //
        // 所以判据不写「系统版本 ≥ N」（那是在猜系统的行为），而是**直接量**：
        // 键盘弹出后页面还是满高 ⇒ 系统没管，我们自己补；已经明显矮了 ⇒ 系统管了，一个字不加。
        if (_pageHeightNoIme > 0 && pageView.Height < _pageHeightNoIme - 8) { SetPad(0); return; }

        // 键盘上沿在窗口里的 y = 窗口高 − 键盘高。
        var root = pageView.RootView as Android.Views.View;
        double windowH = (root?.Height ?? pageView.Height) / density;
        double imeTop = windowH - imeBottomPx / density;

        // 页面自己下沿在窗口里的 y。**必须用它、不能用窗口高** —— 页面底下还压着 Shell 的
        // 标签栏（实测 210px ≈ 76pt），它本来就不属于页面、本来就被键盘盖着，
        // 算进内边距等于白白丢掉一截编辑区。
        var loc = new int[2];
        pageView.GetLocationInWindow(loc);
        double pageBottom = (loc[1] + pageView.Height) / density;

        SetPad(Math.Max(0, pageBottom - imeTop));
    }

    /// <summary>设根布局的底部内边距（pt）。值没变就一个字都不动 —— 键盘动画会连发很多次回调。</summary>
    private void SetPad(double pad)
    {
        if (Math.Abs(pad - _imePadPt) < 0.5) return;
        _imePadPt = pad;

        // 排到下一拍再改布局：回调本身处在 inset 派发过程里，就地改布局会嵌套触发一次布局。
        MainThread.BeginInvokeOnMainThread(() => RootGrid.Padding = new Thickness(0, 0, 0, pad));
    }
#endif

    // ── 右上角菜单 ──

    private async void OnMenuClicked(object? sender, EventArgs e)
    {
        CommitEditingLine();   // 先把正在编辑的行落盘，再弹菜单（弹菜单会失焦）

        // 工具栏那 7 个图标也一并收进来：工具栏是「一眼可见」，菜单是「全都在这里」——
        // 功能一多，图标按钮就会挤成一片看不出谁是谁，不如给一个完整的清单入口。
        //
        // ⚠ **字号缩放不在这里**（用户要求去掉：「菜单太长，反正可以手指操作」）。
        //   捏合缩放本身就够用，而且它给的是连续值 —— 菜单里再摆一对「加大/缩小」
        //   既重复又只能一磅一磅挪，纯属占位置。
        //   但**「重置字号」留着**：捏合能缩放、**不能**精确回到默认值，而字号是持久化的
        //   （MauiEditorStore），捏到 8 号或 96 号之后没有这条路就回不来了。
        var items = new List<string>
        {
            "💾  保存",
            "📄  另存为…",
            "✨  新建文件…",
            "✎  切换 编辑/只读",
            "↶  撤销",
            "↷  重做",
            "🔍  查找…",
            "🔁  替换…",
            "🔢  跳到行…",
            "📖  大纲…",
        };

        // 「预览 / 运行」这一项**跟着文件类型走**，而且与工具栏那一格**同源**
        // （都问 ActionForCurrentFile）—— 各写一份判据迟早出现「工具栏是 ▶、菜单里还是 👁」。
        var actionKind = ActionForCurrentFile();
        var actionLabel = actionKind switch
        {
            EditorAction.Preview => "👁  Markdown 预览",
            EditorAction.Run => "▶  运行",
            _ => null,
        };
        if (actionLabel != null) items.Add(actionLabel);

        items.Add(AssistMenuLabel);
        items.Add("📋  全选并复制");
        items.Add($"↩️  重置字号（当前 {EditorTypography.FontSize:0.#}）");
#if DEBUG
        // 调试构建专用：灌一批假诊断，让气泡的观感/堆叠/避让/关闭**不必等一分钟的编译**
        // 就能在真机上验收。与 ShowDebugHud 同类，是长期的验收工具。
        items.Add(InjectDiagnosticsLabel);
#endif

        var choice = await DisplayActionSheetAsync("编辑器", "取消", null, [.. items]);

        switch (choice)
        {
            case "💾  保存": await SaveAsync(); break;
            case "📄  另存为…": await SaveAsAsync(); break;
            case "✨  新建文件…": await NewFileAsync(); break;
            case "✎  切换 编辑/只读": OnEditClicked(this, EventArgs.Empty); break;
            case "↶  撤销": OnUndoClicked(this, EventArgs.Empty); break;
            case "↷  重做": OnRedoClicked(this, EventArgs.Empty); break;
            case "🔍  查找…": OnFindClicked(this, EventArgs.Empty); break;
            case "🔁  替换…": OnReplaceClicked(this, EventArgs.Empty); break;
            case "🔢  跳到行…": await GoToLineAsync(); break;
            case "📖  大纲…": OnOutlineClicked(this, EventArgs.Empty); break;
            case AssistMenuLabel: ToggleAssistBar(); break;
            case "📋  全选并复制": await CopyAllAsync(); break;
            case var c when c == actionLabel && actionKind == EditorAction.Preview:
                OnPreviewClicked(this, EventArgs.Empty); break;
            case var c when c == actionLabel && actionKind == EditorAction.Run:
                await RunCurrentFileAsync(); break;
#if DEBUG
            case InjectDiagnosticsLabel: InjectFakeDiagnostics(); break;
#endif
            case var c when c != null && c.StartsWith("↩️"): ResetFontSize(); break;
        }
    }

    /// <summary>调试构建才有的一项（菜单与 switch 两处引用同一个常量，别各写字面量）。</summary>
    private const string InjectDiagnosticsLabel = "🧪  注入测试诊断";

#if DEBUG
    /// <summary>
    /// 灌一批覆盖三种严重度、含「同一行两条」「无锚一条」以及三个**边界位置**的假诊断 ——
    /// 气泡的颜色/合并/✕/滚动跟随、以及「尖有没有被裁」都能靠它验收，不必真的等一次编译。
    ///
    /// 三个边界是用户点名的（它们正是「尾巴居中挂在气泡边上」那套老做法的死穴）：
    /// · **第 1 列**：锚点已在正文最左，气泡整体向右展开即可，什么都不该被裁；
    /// · **第 1 列 + 最后一行**：气泡往下展开会超出内容底部（规格：允许，滚动看得见）；
    /// · **同一行里最左（第 1 列）与最右（第 200 列）各一条**：两泡各自向右展开、
    ///   允许互相压住（规格：不做任何避让）。
    /// </summary>
    private void InjectFakeDiagnostics()
    {
        if (_editable == null) { ShowToast("大文件只读，没有可注入的行"); return; }

        long mid = Math.Max(1, _editable.LineCount / 2);
        long last = Math.Max(1, _editable.LineCount);
        long edge = Math.Max(1, _editable.LineCount / 4);   // 同一行里放「最左 + 最右」两条
        var list = new List<Diagnostic>
        {
            new((int)mid, 1, Severity.Error, "语法错误：未预期的 token（测试用）", "Parser_UnexpectedToken"),
            new((int)mid, 8, Severity.Warning, "变量已声明但未被使用（测试用）", null),
            new((int)Math.Min(_editable.LineCount, mid + 3), 3, Severity.Info, "这里是提示信息（测试用）", null),
            // 同一位置两条 —— 必须合并成一个气泡、正文排成 `1. …` / `2. …`
            new((int)Math.Min(_editable.LineCount, mid + 6), 5, Severity.Error, "同一格的第二条（测试用）", "E2"),
            new((int)Math.Min(_editable.LineCount, mid + 6), 5, Severity.Warning, "同一格的第三条（测试用）", null),
            // 边界①第 1 列 / ②第 1 列 + 最后一行 / ③同一行最左与最右
            new((int)edge, 1, Severity.Error, "第 1 列：气泡尖应贴着正文左缘、不许被裁（测试用）", null),
            new((int)last, 1, Severity.Warning, "最后一行第 1 列：气泡往下超出内容底部也照画（测试用）", null),
            new((int)edge, 200, Severity.Info, "同一行最右：气泡向右展开（测试用）", null),
            new(0, 0, Severity.Error, "这条没有位置信息，气泡不画尖（测试用）", null),
        };
        try { DiagnosticManager.Inject(_relPath, list); } catch { }
        Canvas.InvalidateAll();
        UpdateStatus();
        ShowToast($"已注入 {list.Count} 条测试诊断");
    }
#endif

    /// <summary>
    /// 字号重置为默认值（菜单里唯一剩下的字号入口）。
    ///
    /// 菜单里原来的「加大 / 缩小」已按用户要求去掉（菜单太长，捏合缩放足够）；
    /// 它们当时按 <see cref="EditorTypography.FontStep"/> 一磅一磅地挪，
    /// 而捏合给的是连续值 —— 两者并存纯属重复。这条路保留是因为**捏合回不到精确的默认值**。
    /// </summary>
    private void ResetFontSize()
        => ApplyFontSize(EditorTypography.DefaultFontSize, persist: true, toast: true);

    /// <summary>
    /// **改字号的唯一入口** —— 捏合缩放、菜单「重置字号」、编辑时对齐整数号全部走这里。
    ///
    /// 排版常量是全局的，改完要让画布重测字宽、浮动的输入框跟着变；这套收尾只此一份，
    /// 免得「捏合」与「菜单」各写一遍然后漂移（此前就是两份几乎逐行相同的拷贝，
    /// 而注释还写着「共用同一套收尾」）。
    ///
    /// <paramref name="persist"/> = 是否立刻落盘：捏合期间传 false（每个触摸事件写一次文件会卡），
    /// 由 <c>Canvas.PinchEnded</c> 在手势结束补一次。
    /// </summary>
    private void ApplyFontSize(float size, bool persist, bool toast)
    {
        // ⚠ **先夹取、再判断「变了没有」** —— 顺序不能反。
        // 拿未夹取的值去比：越界时（比如已到 96 还继续放大）会与当前值不等而放行，
        // 排版层却夹回 96 ⇒ 画布没变、输入框却真的被写成越界值，两边错开。
        // 夹取规则的真源在 EditorTypography.ClampFontSize（那边 setter 也用它），这里不要另写一份。
        float snapped = EditorTypography.ClampFontSize(size);

        // **没变就直接返回** —— 缩放不卡的关键。捏合事件频率可达 120~240Hz，相邻两次
        // 常常只差千分之几。不挡掉的话，下面那串「三个布局属性 + 整屏重排 + 重测字宽」
        // 会照着触摸频率白做几百遍。
        if (Math.Abs(snapped - EditorTypography.FontSize) < 0.01f) return;

        EditorTypography.FontSize = snapped;
        if (persist) MauiEditorStore.SetFontSize(snapped);

        // 输入框与画布必须同步：两者字号/行高不一致就会错位（这正是当初改成单层自绘要解决的问题）
        LineEditor.FontSize = snapped;
        LineEditor.HeightRequest = 1;
        // ⚠ HeightRequest 只是「请求」，**不是上限**：Entry 在 VerticalOptions=Start 下会按内容
        // 自然高度撑开（13pt 加 EditText 默认内边距实测约 3 个行高），于是它的选区高亮变成
        // 一条跨 3 行的矩形、两个选择手柄落到编辑行下方两行去。文字与光标都是画布画的，
        // 所以只有高亮/手柄会暴露这个失真。MaximumHeightRequest 才是真正的钳制。
        LineEditor.MaximumHeightRequest = 1;
        Canvas.ResetTypography();

        // 气泡也跟着一起放大缩小：它的字号、每行字数、尖的尺寸全是从字号算出来的，
        // 而 `Canvas.ResetTypography()` 已经触发重绘 ⇒ 下一帧按新字号重算（见 EnsureBubbleGroups
        // 的缓存键里那个字号）。这里不需要任何收尾。

        // 菜单路径弹轻提示（它过 2 秒会自己把状态栏恢复成 UpdateStatus）；
        // 捏合路径没有提示，得自己刷一下状态栏 —— 否则要等下一次光标/滚动事件才看到新字号。
        if (toast) ShowToast($"字号 {snapped:F0}");
        else UpdateStatus();
    }

    private async Task SaveAsAsync()
    {
        if (_editable == null) { await DisplayAlertAsync("另存为", "只读文件不能另存", "关闭"); return; }

        var current = Path.GetFileName(_relPath);
        var name = await DisplayPromptAsync("另存为", "新文件名（可带子目录）",
            accept: "保存", cancel: "取消", initialValue: current, maxLength: 200);
        if (string.IsNullOrWhiteSpace(name)) return;
        name = name.Trim();

        try
        {
            SandboxFsService.WriteTextAtomic(name, _editable.ReadAll(),
                _doc?.Encoding ?? new UTF8Encoding(false), _doc?.UsesCrlf ?? false);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("另存为失败", ex.Message, "关闭");
            return;
        }

        _relPath = name;
        _fullPath = SandboxFsService.ResolveInSandbox(name) ?? "";
        _fileReadOnly = IsFileReadOnly(_fullPath);   // 另存为换了个文件，只读属性跟着换（刚写的这份必然可写）
        _modified = false;
        FileLabel.Text = name;
        Title = Path.GetFileName(name);
        UpdateStatus();
        ShowToast($"已另存为 {name}");
    }

    private async Task NewFileAsync()
    {
        // 建新文件会 LoadAsync 覆盖当前文档（`_modified` 随之清零）⇒ 有未保存改动必须先问，
        // 否则「新建」一下当前那份改动就无声没了。问在**输入文件名之前**：先要到名字再被拦下
        // 等于白填一次。
        if (!await ConfirmUnsavedAsync()) return;

        var name = await DisplayPromptAsync("新建文件", "文件名（可带子目录，如 src/a.cs）",
            accept: "创建", cancel: "取消", maxLength: 200);
        if (string.IsNullOrWhiteSpace(name)) return;
        name = name.Trim();

        try
        {
            SandboxFsService.WriteTextAtomic(name, "", new UTF8Encoding(false), crlf: false);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("新建失败", ex.Message, "关闭");
            return;
        }
        await LoadAsync(name);
    }

    private async Task GoToLineAsync()
    {
        if (_doc == null) return;
        var input = await DisplayPromptAsync("跳到行", $"行号（1 ~ {_doc.LineCount:N0}）",
            accept: "跳转", cancel: "取消", keyboard: Keyboard.Numeric, maxLength: 12);
        if (!long.TryParse(input, out var line)) return;

        line = Math.Clamp(line, 1, Math.Max(1, _doc.LineCount));
        Canvas.ScrollToLine(line, center: true);
        Canvas.SetCaretLine(line);
        UpdateStatus();
    }

    // ── 加载 ──

    private async Task LoadAsync(string relPath)
    {
        _relPath = relPath;
        FileLabel.Text = relPath;
        Title = Path.GetFileName(relPath);

        _fullPath = SandboxFsService.ResolveInSandbox(relPath) ?? "";
        if (_fullPath.Length == 0 || !File.Exists(_fullPath))
        {
            await DisplayAlertAsync("无法打开", $"文件不存在或路径越界：{relPath}", "关闭");
            return;
        }

        _fileBytes = new FileInfo(_fullPath).Length;
        _fileReadOnly = IsFileReadOnly(_fullPath);

        // 打开中遮罩：大文件要顺序扫一遍建行索引（100MB 在百毫秒~数秒级）。没有反馈的话，
        // 用户会以为是「没点到」或者「卡死了」——这也是打开大文件时最容易让人困惑的一段。
        var (src, reason) = await ShowLoadingWhileOpeningAsync();

        if (src == null)
        {
            await DisplayAlertAsync("无法打开", reason, "关闭");
            return;
        }

        _doc?.Dispose();
        _doc = src;
        _editable = src as EditableLines;
        _canEdit = _editable != null;
        _history.Clear();
        _modified = false;
        _editLine = -1;
        _editLineStart = "";
        _forcedEncodingName = null;   // 换了文件，上一个文件强制过的编码名作废
        LineEditor.IsVisible = false;

        EditorTypography.FontSize = MauiEditorStore.FontSize;   // 套用上次调的字号
        LineEditor.FontSize = EditorTypography.FontSize;
        LineEditor.HeightRequest = 1;
        // ⚠ HeightRequest 只是「请求」，**不是上限**：Entry 在 VerticalOptions=Start 下会按内容
        // 自然高度撑开（13pt 加 EditText 默认内边距实测约 3 个行高），于是它的选区高亮变成
        // 一条跨 3 行的矩形、两个选择手柄落到编辑行下方两行去。文字与光标都是画布画的，
        // 所以只有高亮/手柄会暴露这个失真。MaximumHeightRequest 才是真正的钳制。
        LineEditor.MaximumHeightRequest = 1;

        bool dark = IsDarkTheme;   // 判据只有一处（见 IsDarkTheme 的说明）
        Canvas.SetDocument(_doc, relPath, dark, _canEdit);
        // ⚠ **这里原来写的是 `Inject(relPath, [])`「把上个文件留下的诊断清掉」，那是个错判，已删。**
        //   理由：诊断表是**按文件路径做键**的（`SnapshotDiagnostics` → `GetAll(_filePath)`、
        //   `DiagnosticsForThisFile` → `GetAll(_relPath)`），所以**别的文件的诊断根本画不到这个文件上**
        //   —— "串到新文件"这个担心不成立 ⇒ 那行清的是**正在打开的这个文件**的诊断。
        //   后果是实打实的：命令行页/文件页刚跑完一次**编译失败**、把结构化诊断注入进来
        //   （`ShellPage.PublishDiagnosticsToEditor`），用户随手「打开」看代码 ⇒ **当场被擦掉**，
        //   屏幕上既没有气泡也没有波浪线、错误列表也是空的 —— 正是用户报的那句
        //   「明明有报错，错误列表是空的，编辑器也不出错误泡泡」。
        //   实测（Windows 版 MAUI + UI Automation 驱动）：留着这行时**一个气泡都没有**，
        //   删掉后同一操作立刻出气泡与波浪线。
        //   「编辑之后诊断作废」那条另有人在管（`ClearDiagnosticsOnEdit`），职责不重叠。
        SetReadOnly(true);   // 打开一律先进只读（对齐旧行为：默认只读，手动解锁编辑）
        // 「预览 / 运行」那一格的图标与可见性 —— 换文件后必须重设（.md → 👁、.c → ▶、.txt → 隐藏）
        ApplyActionButton();
        UpdateStatus();
#if WINDOWS
        // 打开即把键盘入口接上（只读态也一样）——否则在 Windows 上打开文件后按上下键毫无反应。
        // 初始光标落在第 1 行，用户按上下就是从那里开始走。
        Canvas.SetCaretLine(1);
        EnsureKeySinkFocused();
#endif

        if (!_canEdit)
            ShowToast($"大文件以只读方式打开（{FormatSize(_fileBytes)}），可流畅滚动查看");
    }

    /// <summary>显示「文件打开中…」遮罩并在其间完成打开（含大文件的索引建立）。</summary>
    private async Task<(ITextSource? Source, string Reason)> ShowLoadingWhileOpeningAsync()
    {
        bool big = _fileBytes > 4L * 1024 * 1024;
        LoadingText.Text = big
            ? $"文件打开中…\n{FormatSize(_fileBytes)}，正在建立行索引"
            : "文件打开中…";
        LoadingMask.IsVisible = true;
        Canvas.IsVisible = false;

        try
        {
            // 让遮罩先真正画出来再开始建索引：索引是 CPU/IO 密集的，同步跑在这里会把
            // 这次布局与绘制一起堵住，遮罩根本来不及显示（那正是「点了没反应」的样子）。
            await Task.Yield();
            return await TextSourceFactory.OpenAsync(_fullPath, MauiEditorStore.ReadOnlyMaxBytes);
        }
        finally
        {
            LoadingMask.IsVisible = false;
            Canvas.IsVisible = !PreviewScroll.IsVisible;
        }
    }

    private static string FormatSize(long b) => b switch
    {
        >= 1 << 20 => $"{b / (double)(1 << 20):0.#}MB",
        >= 1 << 10 => $"{b / (double)(1 << 10):0.#}KB",
        _ => $"{b}B"
    };

    /// <summary>
    /// 文件的**只读属性**（不是「本页当前处于只读模式」——那是 <see cref="_readOnly"/>，
    /// 是用户自己切的，随时能切回去；这个是文件系统说了算的，切不动）。
    ///
    /// 走 <see cref="File.GetAttributes"/> 就够，**不要另按平台写一套权限判断**：
    /// Windows 上它就是「只读」属性；Unix（Android/iOS 也走这条）上 .NET 用
    /// <c>access(W_OK)</c> 反推，文件没有写权限时报 <see cref="FileAttributes.ReadOnly"/> ——
    /// 两边语义刚好对齐我们想要的「这文件能不能写」。
    ///
    /// 属性读不出来（路径越界/被 SELinux 挡）时**当可写**：宁可让保存那一步报真正的错，
    /// 也不要凭一次读属性失败就把用户的编辑入口锁掉。
    /// </summary>
    private static bool IsFileReadOnly(string fullPath)
    {
        try { return File.GetAttributes(fullPath).HasFlag(FileAttributes.ReadOnly); }
        catch { return false; }
    }

    // ── 离页前的未保存拦截 ──

    /// <summary>本次导航已经问过并放行（避免「取消后再 Pop」被自己再拦一次）。</summary>
    private bool _leaving;

    /// <summary>当前是否处于全屏（收起所有栏）。</summary>
    private bool _fullscreen;

    /// <summary>
    /// 全屏 / 还原：收掉**导航栏 + 文件名行 + 工具条 + 状态栏**，把整屏让给画布。
    ///
    /// 两条实现要点：
    /// ① 行高不用手工调 —— `RootGrid` 的行定义是 `Auto,Auto,*,Auto`，
    ///    子元素 `IsVisible=False` 时 Auto 行自然塌成 0，画布那一行（`*`）自动吃掉空间。
    /// ② 进全屏后**导航栏整条消失，全屏按钮自己也没了** ⇒ 恢复入口只能另放一个
    ///    浮在画布上的按钮（`RestoreBtn`）。这是这类「隐藏所有 UI」功能最容易漏的地方：
    ///    只做了"进去"、忘了"出来"，用户就被困在无栏界面里（只剩系统返回键能救）。
    ///
    /// 画布尺寸会跟着变（页面可用高度变高），由 `CodeCanvasView.OnSizeAllocated`
    /// 自己收口边界 —— 与 IME inset 那条走的是同一条路，这里不必手工算高度。
    /// </summary>
    private void SetFullscreen(bool on)
    {
        _fullscreen = on;
        if (Shell.Current != null)
        {
            Shell.SetNavBarIsVisible(this, !on);
            // 底部 TabBar（首页/对话/命令行/文件/设置）也一起收掉 —— 它同样占着一整条的高度，
            // 全屏的意义就是把这些都让给画布。与上面 NavigateBar 一样是**附加属性**，
            // 设在页面自身上，只影响本页。
            // TabBar **恒不显示**（不再跟全屏开关走）：编辑器是全屏工作的页面，
            // 底部那条切页栏既没用又占一行高度。NavBar 仍随全屏开关收放。
            Shell.SetTabBarIsVisible(this, false);
        }
        FileRow.IsVisible = !on;
        ToolRow.IsVisible = !on;
        StatusLabel.IsVisible = !on;
        FullscreenBar.IsVisible = on;
        // 浮层里那个「运行/预览」按钮的可见性**与工具栏那一格同源**（都问 ActionForCurrentFile）
        // —— 非源码文件时浮层里只剩还原，不是第二份判据。
        ApplyActionButton();
    }

    private void OnFullscreenEnterClicked(object? sender, EventArgs e) => SetFullscreen(true);

    private void OnFullscreenExitClicked(object? sender, EventArgs e) => SetFullscreen(false);

    /// <summary>
    /// 主题一变：**画布与气泡都要重建**。
    ///
    /// 两处的颜色都是**烘进去的**，不是每次绘制现取：
    /// · 画布 —— `_isDark` 只在 `SetDocument` 时传进来一次，且行缓存里每个段的颜色
    ///   （正文色、行号色、高亮 span）在建的时候就按当时的 `_isDark` 取好了。
    ///   所以原来「系统白天↔黑夜，编辑器内容不变色，要退出文件重开才变」——
    ///   重开走的是 `SetDocument`，那是**唯一**会更新 `_isDark` 的路径。
    ///   `SetDark` 早就写好了（连清缓存都写对了），只是**从来没有人调它**。
    /// · 气泡 —— 底色/文字色在建的时候取。
    /// </summary>
    private void OnAppThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        try
        {
            Canvas.SetDark(IsDarkTheme);   // 气泡的底色/文字色也在这条路上重建（按需缓存会整体作废）
            // 面板 Tab 的选中态是**代码赋值**的（见 SwitchPanelTab），换主题不会自己变
            SwitchPanelTab(_panelTab);
            // 输出小窗的正文色/底色也是**烘进去的**（`SetLines` 收一个 `isDark`，行缓存按它取色）
            // —— 与画布同一条理由：不重放一次，换了主题要重新打开这个文件才变色。
            RenderPanelOutput();
            // 「没有错误。」那行与各诊断行的颜色也是建的时候取的
            RefreshErrorList();
        }
        catch { /* 换主题不该把界面搞崩 */ }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // 整页占满的页面不该带底部 tab 栏（编辑器 / 游戏窗口里没有切页的需求，返回箭头就够）。
        // ⚠ XAML 上的 Shell.TabBarIsVisible 对这种 push 出来的页面**不生效**（实机验过：tab 栏照旧），
        //   必须在 code-behind 设。
        Shell.SetTabBarIsVisible(this, false);
        if (Shell.Current != null) Shell.Current.Navigating += OnShellNavigating;
        if (Application.Current != null) Application.Current.RequestedThemeChanged += OnAppThemeChanged;
        Active = this;   // 给平台输入连接回调用（见 Active 的说明）
        // 每次进来都回到「有栏」状态：全屏是靠隐藏导航栏实现的，若带着全屏状态重新进入，
        // 用户第一眼看到的是没有返回箭头的界面，容易以为进了死路。
        if (_fullscreen) SetFullscreen(false);

        // 设置页改的编辑器项（气泡每行字数 / 气泡总开关）要在**回到本页时**生效：
        // 那些值是在绘制时现读的（见 CodeCanvasView），所以只要重画一帧即可 —— 不重画的话
        // 屏幕上留着的还是进来之前那张画面，看着就是「设置没生效」。
        Canvas.Invalidate();
    }

    protected override void OnDisappearing()
    {
        if (Shell.Current != null) Shell.Current.Navigating -= OnShellNavigating;
        if (Application.Current != null) Application.Current.RequestedThemeChanged -= OnAppThemeChanged;
        if (ReferenceEquals(Active, this)) Active = null;

        // 编译可能正在进行（手机上要一分多钟），而用户随时可能按返回走人。
        // 不停的话那个前台线程会一直烧着 CPU，用户以为已经离开了。
        // ⚠ 与命令行页那边的说明一致：这停的是「等待」，编译线程本身没法中止。
        try { _compileCts?.Cancel(); } catch { }

        base.OnDisappearing();
    }

    /// <summary>
    /// 返回（导航栏箭头 / 系统返回键）时若文件改过而没保存，**先问一句**。
    ///
    /// 为什么挂 <c>Shell.Navigating</c> 而不是页面的 <c>OnBackButtonPressed</c>：后者只管
    /// **系统返回键**，而屏幕上那个返回箭头走的是 Shell 自己的导航 —— 只挂前者的话，
    /// 用户点箭头照样一路退出去，改动静默消失（这正是要防的那种丢数据）。
    ///
    /// <c>Navigating</c> 是**同步**事件，所以顺序必须是「先 <c>e.Cancel()</c> 拦下，再 await 问」：
    /// 一旦 await 完再取消就已经晚了，页面早就退掉了。<c>CanCancel</c> 为假时拦不住
    /// （Shell 明说这次不让取消），此时**不弹框** —— 弹了也留不住人，只会让用户以为「点了没用」。
    /// </summary>
    private async void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        if (_leaving || !_modified) return;
        if (e.Source != ShellNavigationSource.Pop && e.Source != ShellNavigationSource.PopToRoot) return;
        if (!e.CanCancel) return;

        e.Cancel();

        if (!await ConfirmUnsavedAsync()) return;   // 取消 ⇒ 留在编辑器里

        _leaving = true;
        try { await Shell.Current.Navigation.PopAsync(); }
        finally { _leaving = false; }
    }

    // ── 工具条 ──

    private void SetReadOnly(bool readOnly)
    {
        _readOnly = readOnly;
        if (readOnly) CommitEditingLine();
#if WINDOWS
        // 【Windows】只读态下那个「键盘代理」输入框必须置为 IsReadOnly：
        // 它是拿来收上下键的（画布不可聚焦），但**能打字的输入框会招来输入法** ——
        // 实测只读浏览时中文 IME 的候选框直接弹在正文上（截图里那排「1正常 2支持…」）。
        // 只读的 TextBox 照样能聚焦、照样收按键，只是不接受文本/不组合 IME。
        LineEditor.IsReadOnly = readOnly;
        if (readOnly) LineEditor.Text = "";   // 顺手清掉之前误落的字符（只读态不承载文本）
#endif

        // ⚠ **扩展名不能省**：Windows 上 MAUI 直接拿这个名字去 `ms-appx:///<名>` 找文件，
        // 而 resizetizer 产出的实际文件名是 `icon_edit.scale-100.png` —— 少了 `.png` 就什么都找不到
        // （Android 的资源查找不看扩展名，所以手机上一直是好的）。实测这就是「工具条没有图标」的根因。
        EditBtn.Source = readOnly ? "icon_edit.png" : "icon_lock.png";
        // **按钮始终可点**：不能编辑的两种情形（超上限 / 文件只读）都要能**说清原因**。
        // 禁用的话点击根本不触发，用户只会觉得「按了没反应」——那比一句拒绝的提示更糟。
        // 视觉上仍按可编辑与否变淡，一眼能看出「这支笔现在不顶用」。
        EditBtn.IsEnabled = true;
        EditBtn.Opacity = _canEdit && !_fileReadOnly ? 1 : 0.35;
        UndoBtn.IsEnabled = _canEdit && !readOnly;
        RedoBtn.IsEnabled = _canEdit && !readOnly;
        SaveBtn.IsEnabled = _canEdit && !readOnly;
        Canvas.EditingLine = -1;
        UpdateStatus();
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (!_canEdit)
        {
            _ = DisplayAlertAsync("只读",
                $"文件 {FormatSize(_fileBytes)} 超过可编辑上限（{MauiEditorStore.ReadOnlyMaxBytes / (1024 * 1024)}MB）。\n" +
                "编辑需要把内容装进内存，再大就无法保证不闪退 —— 可在「设置 › 编辑器」里调高上限。", "知道了");
            return;
        }

        // **文件自身是只读属性 ⇒ 直接拒绝**（用户要求）：这不是「本页当前只读」那种自己切的模式，
        // 是文件系统不允许写。放进去编辑只会让人白改一场，最后存的时候才报错。
        // 只拦「进编辑」这一个方向：已经在编辑态时按它是想退出，退出不该被拦。
        if (!_readOnly && _fileReadOnly)
        {
            _ = DisplayAlertAsync("只读文件",
                $"{_relPath}\n\n该文件是只读文件（文件属性为只读），不可编辑。", "知道了");
            return;
        }

        // **退出编辑前先问「要不要保存」**（用户要求）。只读 → 编辑是进门，不必问。
        // 选「取消」就留在编辑态（没问出结果就不许走），否则一次误触就丢掉整段改动。
        if (!_readOnly && !await ConfirmUnsavedAsync()) return;

        SetReadOnly(!_readOnly);
        // ⚠ 用工具栏这支笔切进编辑态时**也要对齐字号**（v0.96.142）。
        // 上一版只把对齐加在 `BeginEditLine`（点某一行那条路）里，于是「先点笔、再点行」或者
        // 「笔切进来时字号还是小数」这两种走法全都漏过去了 —— 实测截图上就是「编辑模式开着、
        // 字号仍是 13.7」，而小数号下光标会有一个恒定的几像素偏移、切进前一个字形里。
        if (!_readOnly) SnapFontSizeForEditing();
    }

    /// <summary>
    /// 有未保存改动时问一句「要不要保存」。**返回 true = 可以继续往下走**（已保存 / 用户明确放弃），
    /// false = 用户取消（调用方必须原样停下）。
    ///
    /// 三态而不是两态：<c>DisplayAlert</c> 只有两个按钮，而这里「保存」和「不保存」之外**必须**
    /// 还能反悔 —— 弹窗本身可能是一次误触（点错图标、滑动碰到），没有「取消」就只能二选一，
    /// 用户被迫在两个都会丢东西的选项里挑一个。
    ///
    /// ⚠ 「不保存」**不清 `_modified`**：这里只是离开编辑模式，缓冲区里那份改动还在，
    /// 状态栏的 ● 也还亮着，回头再进编辑就能接着改、接着存。**只丢屏幕上那份、不丢内存里那份**
    /// 是安全的方向；真把内存也清了，用户以为「没保存等于撤销了」，实际是「数据没了」。
    /// </summary>
    private async Task<bool> ConfirmUnsavedAsync()
    {
        if (!_modified) return true;
        CommitEditingLine();   // 正在编辑的那一行还在输入框里，先落进缓冲区，否则「保存」会漏掉它

        var choice = await DisplayActionSheetAsync("文件已修改", "取消", null, "保存", "不保存");
        return choice switch
        {
            "保存" => await WriteBackAsync(),   // 存失败要拦下（WriteBackAsync 已弹过原因）
            "不保存" => true,
            _ => false,                          // 「取消」或被点遮罩关掉
        };
    }

    private async void OnSaveClicked(object? sender, EventArgs e) => await SaveAsync();

    private async Task SaveAsync()
    {
        if (await WriteBackAsync())
            await DisplayAlertAsync("已保存", _relPath, "确定");
    }

    /// <summary>把缓冲区写回文件（<b>不弹成功提示</b>）。成功返回 true；失败弹一次原因并返回 false。</summary>
    private async Task<bool> WriteBackAsync()
    {
        if (_editable == null || _fullPath.Length == 0) return false;
        CommitEditingLine();
        try
        {
            // 原子写（旧实现是 File.WriteAllText 直接覆盖，中途失败会留半截文件）。
            // 编码/换行按「只对源码生效」的规则解析，见 ResolveSaveFormat。
            var (encoding, crlf) = ResolveSaveFormat();
            SandboxFsService.WriteTextAtomic(_relPath, _editable.ReadAll(), encoding, crlf);
            _modified = false;
            UpdateStatus();
            return true;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("保存失败", ex.Message, "关闭");
            return false;
        }
    }

    /// <summary>
    /// 这次保存该用哪个编码 / 哪种换行。
    ///
    /// **只对「源码」生效**（用户明确要求）：Markdown、纯文本、数据文件一律沿用打开时的原样 ——
    /// 免得「打开看一眼、随手保存」就把一个 GBK 的数据文件悄悄转成了 UTF-8。
    /// 源码文件里，用户选的若是「保持原样」（默认值）也沿用原样。
    ///
    /// 编码的真源是 <see cref="MauiEditorStore.SaveAsEncoding"/>，OEM 走既有的
    /// <see cref="ProcEncoding.OemEncoding"/>（不在这里另写一份代码页探测）。
    /// </summary>
    private (Encoding Encoding, bool Crlf) ResolveSaveFormat()
    {
        var docEnc = _doc?.Encoding ?? new UTF8Encoding(false);
        bool docCrlf = _doc?.UsesCrlf ?? false;
        if (!IsSourceFile()) return (docEnc, docCrlf);

        var enc = MauiEditorStore.SaveAsEncoding switch
        {
            MauiEditorStore.SaveEncoding.Utf8NoBom => new UTF8Encoding(false),
            MauiEditorStore.SaveEncoding.Utf8Bom => new UTF8Encoding(true),
            MauiEditorStore.SaveEncoding.Utf16Le => new UnicodeEncoding(false, true),
            // 探测不到就退回原样，不硬转 —— 转错编码比不转难查得多
            MauiEditorStore.SaveEncoding.Oem => ProcEncoding.OemEncoding ?? docEnc,
            _ => docEnc,
        };

        bool crlf = MauiEditorStore.SaveAsNewline switch
        {
            MauiEditorStore.SaveNewline.Crlf => true,
            MauiEditorStore.SaveNewline.Lf => false,
            _ => docCrlf,
        };

        // 状态栏的编码名来自「打开时探测的那个」，用户强制换成别的编码后它就**过期**了 ——
        // 记下实际写出去的，免得屏幕上显示的和文件里躺着的不是一回事。
        // 名称用用户在下拉里看到的写法（不是 Encoding.WebName，那个大小写与措辞都对不上）。
        _forcedEncodingName = MauiEditorStore.SaveAsEncoding switch
        {
            MauiEditorStore.SaveEncoding.Utf8NoBom => "UTF-8",
            MauiEditorStore.SaveEncoding.Utf8Bom => "UTF-8 BOM",
            MauiEditorStore.SaveEncoding.Utf16Le => "UTF-16LE",
            MauiEditorStore.SaveEncoding.Oem => ProcEncoding.OemEncoding != null ? "OEM" : null,
            _ => null,
        };

        return (enc, crlf);
    }

    /// <summary>强制改过编码之后状态栏要显示的名字（null = 沿用打开时探测的）。换文件时清空。</summary>
    private string? _forcedEncodingName;

    private void OnUndoClicked(object? sender, EventArgs e)
    {
        CommitEditingLine();
        var op = _history.Undo();
        if (op == null || _editable == null) return;
        ApplyOp(op.Value, undo: true);
    }

    private void OnRedoClicked(object? sender, EventArgs e)
    {
        CommitEditingLine();
        var op = _history.Redo();
        if (op == null || _editable == null) return;
        ApplyOp(op.Value, undo: false);
    }

    private void ApplyOp(EditOp op, bool undo)
    {
        if (_editable == null) return;
        var remove = undo ? op.NewLines.Length : op.OldLines.Length;
        var insert = undo ? op.OldLines : op.NewLines;
        _editable.ReplaceRange(op.Line, remove, insert);

        _modified = true;
        Canvas.InvalidateAll();          // 行数可能变了 → 渲染缓存整份作废
        Canvas.ScrollToLine(op.Line + 1);
        Canvas.SetCaretLine(op.Line + 1);
        UpdateBracketMatch();           // 撤销/重做可能把光标连同括号一起挪走了
        UpdateStatus();
    }

    // ── 单击 / 长按 ──

    private void OnLineTapped(long line, float xInLine)
    {
        Canvas.SetCaretLine(line);
        if (_canEdit && !_readOnly) BeginEditLine(line, xInLine);
#if WINDOWS
        else EnsureKeySinkFocused();   // 只读态点一下也要把键盘入口拿回来（点别处会丢焦点）
#endif
        UpdateStatus();
    }

#if WINDOWS
    /// <summary>
    /// 只读态也要有键盘入口。
    ///
    /// 画布（<c>GraphicsView</c>）在 WinUI 上不可聚焦，而 <c>LineEditor</c> 是这个页面上**唯一**
    /// 收得到键盘的元素 —— 只读时它本来是隐藏的 ⇒ 上下键/翻页按下去没有任何东西收到，
    /// 键盘导航整个失效（用户实测：「上下键无法移动光标」）。
    /// 这里让它以「1px 高、文字与背景全透明」的形态保持可见并持有焦点：用户看不见它，
    /// 只是借它拿按键（IME/软键盘只在编辑态用得上，只读态不牵扯）。
    /// </summary>
    private void EnsureKeySinkFocused()
    {
        if (!LineEditor.IsVisible)
        {
            LineEditor.Text = "";      // 只读态不承载文本；_editLine < 0 时 TextChanged 会早退
            LineEditor.IsVisible = true;
        }
        // MAUI 的 Focus() 在控件已 Loaded 后就能成功；原生 Focus 再兜一道
        // （Handler 刚建好时那一次实测两者都返回 False —— 那时控件还没进可视树）。
        LineEditor.Focus();
        _winKeySink?.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
    }
#endif

    /// <summary>
    /// 长按回调 —— 现在**只用来收尾**：选词与扩选都由画布自己完成（见 <c>CodeCanvasView.OnLongPressTick</c>），
    /// 这里弹我们自己的选区操作条。<c>line</c> 用不上（选区是画布的状态，不是这一行的）。
    /// </summary>
    private void OnLineLongPressed(long line)
    {
        CommitEditingLine();
        UpdateSelectionBar();
    }

    /// <summary>把选区操作条摆到选区上方；没有选区就收起来。</summary>
    private void UpdateSelectionBar()
    {
        if (!Canvas.HasSelection)
        {
            SelectionBar.IsVisible = false;
            return;
        }

        SelPasteBtn.IsVisible = _canEdit && !_readOnly;   // 只读文件不给粘贴（免得看着像能改）
        SelDeleteBtn.IsVisible = _canEdit && !_readOnly;  // 删除同口径

        // 选区被滚出视口就把条子收起来 —— 内容区**不裁剪子控件**，条子会浮到工具栏/状态栏上去
        float top = Canvas.SelectionTopY;
        if (top < -EditorTypography.LineHeight || top > (float)Canvas.Height + EditorTypography.LineHeight)
        {
            SelectionBar.IsVisible = false;
            return;
        }

        // 量一次条子宽度，再按选区左端摆放（靠边时贴边）
        double barW = SelectionBar.Width > 0 ? SelectionBar.Width : 240;
        double x = Math.Max(4, Math.Min(Canvas.GutterWidthPx + 8, Canvas.Width - barW - 4));
        double y = top - (SelectionBar.Height > 0 ? SelectionBar.Height : 40) - 6;
        if (y < 4) y = top + EditorTypography.LineHeight + 6;   // 顶部放不下就摆到下方
        SelectionBar.TranslationX = x;
        SelectionBar.TranslationY = y;
        SelectionBar.IsVisible = true;
    }

    private async void OnSelCopyClicked(object? sender, EventArgs e)
    {
        var text = Canvas.GetSelectedText();
        if (text.Length == 0) { ShowToast("没有选中内容"); return; }
        // 剪贴板仍走系统（那是**数据**通道，不是 UI）；复制完收起选区，与桌面编辑器一致
        // ⚠ 大文件里没读进内存的行拿不到 —— 如实说，别报一个「已复制 N 字符」让人以为复制全了
        if (Canvas.SelectedTextTruncated) ShowToast("选区太大，只复制了能读到的部分", 2600);
        await CopyTextAsync(text);
        Canvas.ClearSelection();
        UpdateSelectionBar();
    }

    private void OnSelAllClicked(object? sender, EventArgs e)
    {
        Canvas.SelectAll();
        UpdateSelectionBar();
    }

    /// <summary>
    /// 删掉当前选区（跨行也行）。返回「是不是真的删了」。
    ///
    /// **选区条的「删除」按钮与软键盘上的退格（Android 那条 InputConnection 路径）
    /// 共用这一个实现** —— 两边各写一份的话，跨行/单行的边界处理迟早只改对一边。
    /// </summary>
    private bool DeleteSelectionCore()
    {
        if (_editable == null || _readOnly || !Canvas.HasSelection) return false;

        CommitEditingLine();          // 正在编辑的那一行先落定（与粘贴、查找同口径）

        // 端点用画布那一份**已经归一化过的** —— 页面不许自己再算一遍「谁在前」
        var (la, ca, lb, cb) = Canvas.SelectionRange;
        la = Math.Clamp(la, 0, _editable.LineCount - 1);
        lb = Math.Clamp(lb, 0, _editable.LineCount - 1);
        if (la > lb || (la == lb && ca > cb)) { (la, ca, lb, cb) = (lb, cb, la, ca); }

        if (la == lb)
        {
            var t = _editable.GetLine(la) ?? "";
            int a = Math.Clamp(ca, 0, t.Length), b = Math.Clamp(cb, a, t.Length);
            var nt = t[..a] + t[b..];
            _editable.ReplaceRange(la, 1, [nt]);
            _history.Push(new EditOp(la, [t], [nt], 0, a, Environment.TickCount64));
        }
        else
        {
            var first = _editable.GetLine(la) ?? "";
            var last = _editable.GetLine(lb) ?? "";
            int a = Math.Clamp(ca, 0, first.Length), b = Math.Clamp(cb, 0, last.Length);
            // ⚠ 旧值必须在 ReplaceRange **之前**取 —— 多行粘贴那处就是先替换后读，
            //    取回来的其实是新内容，撤销一次会丢原始行。
            var olds = _editable.Snapshot(la, (int)(lb - la + 1));
            var nt = first[..a] + last[b..];
            _editable.ReplaceRange(la, (int)(lb - la + 1), [nt]);
            // OldLines 是 2 行以上 ⇒ EditHistory.CanMerge 天然为 false（它要求两端各 1 行）
            // ⇒ 跨行删除永远是独立的一步撤销，不会和相邻打字并成一步。
            _history.Push(new EditOp(la, olds, [nt], 0, a, Environment.TickCount64));
        }

        _modified = true;
        Canvas.ClearSelection();
        Canvas.InvalidateAll();       // 行数可能变了 ⇒ 整份渲染缓存作废（撤销/粘贴都这么做）
        Canvas.SetCaretLine(la + 1);
        Canvas.ScrollToLine(la + 1);
        UpdateStatus();
        UpdateSelectionBar();
        return true;
    }

    private void OnSelDeleteClicked(object? sender, EventArgs e)
    {
        if (!_canEdit || _readOnly) { ShowToast("只读文件不能删除"); return; }
        if (!DeleteSelectionCore()) ShowToast("没有选中内容");
    }

    /// <summary>
    /// 行首退格 = 与**上一行**合行。返回 true = 已处理（这一次退格被我们吃掉）。
    ///
    /// 不适用时返回 **false**，把事件让回平台 —— 第 1 行的行首退格本来就什么都不做，
    /// 不该被我们吞掉。
    /// </summary>
    /// <summary>
    /// 当前活跃的编辑器页 —— 给 <c>BackspaceAwareEditText</c> 用。
    ///
    /// 为什么走静态钩子而不是在 <c>HandlerChanged</c> 里「认类型再接线」：
    /// 后者依赖 `LineEditor.Handler?.PlatformView is BackspaceAwareEditText` 成立，
    /// 一旦不成立就**静默**退回普通输入框，症状是「擦除键没反应」而没有任何线索。
    /// 静态钩子在 <c>OnAppearing</c>/<c>OnDisappearing</c> 里维护，路径短、可断言。
    /// </summary>
    internal static EditorPage? Active { get; private set; }

    /// <summary>行首退格（给平台输入连接调）—— 返回 true = 已处理。</summary>
    internal bool TryJoinWithPreviousLine() => JoinWithPreviousLine();

    /// <summary>行尾 Delete（给平台输入连接调）。</summary>
    internal bool TryJoinWithNextLine() => JoinWithNextLine();

    /// <summary>删掉当前选区（给平台输入连接调）。</summary>
    internal bool TryDeleteSelection() => DeleteSelectionCore();

    private bool JoinWithPreviousLine()
    {
        if (_editable == null || _readOnly || _editLine <= 0) return false;
        // 「当前行」取输入框里**用户正看着的**内容（不是 _editLineStart —— 那是进入编辑态时的原始内容）
        var cur = CurrentEditedText();
        var prev = _editable.GetLine(_editLine - 1) ?? "";
        return JoinLines(_editLine - 1, prev, cur, prev.Length);
    }

    /// <summary>行尾 Delete = 与**下一行**合行。与 <see cref="JoinWithPreviousLine"/> 对称。</summary>
    private bool JoinWithNextLine()
    {
        if (_editable == null || _readOnly || _editLine < 0 || _editLine + 1 >= _editable.LineCount) return false;
        var cur = CurrentEditedText();
        var next = _editable.GetLine(_editLine + 1) ?? "";
        return JoinLines(_editLine, cur, next, cur.Length);
    }

    /// <summary>
    /// 把相邻两行合成一行 —— **唯一实现**，退格（向上）与 Delete（向下）两个方向共用。
    /// </summary>
    /// <param name="atLine">合并后的行号（= 上面那一行的行号，0-based）。</param>
    /// <param name="caretCol">合并后光标落在接缝处的哪一列。</param>
    private bool JoinLines(long atLine, string upper, string lower, int caretCol)
    {
        var joined = upper + lower;

        // 一次「两行 → 一行」：撤销是一步。EditHistory.CanMerge 要求新旧两端各 1 行，
        // 这里是 2 → 1，天然不会和相邻打字并成一步。
        _editable!.ReplaceRange(atLine, 2, [joined]);
        _history.Push(new EditOp(atLine, [upper, lower], [joined], 0, caretCol, Environment.TickCount64));
        _modified = true;

        // ── 状态迁移，顺序要紧 ──
        _editLine = atLine;
        _editLineStart = joined;          // 用户接着编辑的是**合并后**这一行
        _committing = true;               // 挡掉下面这次赋值引发的 TextChanged 回写（同 BeginEditLine）
        LineEditor.Text = joined;
        _committing = false;
        LineEditor.CursorPosition = caretCol;
        Canvas.EditingLine = atLine + 1;
        Canvas.EditingText = joined;
        Canvas.EditingCursor = caretCol;
        Canvas.SetCaretLine(atLine + 1);
        Canvas.InvalidateAll();           // 行数变了 ⇒ 整份渲染缓存作废（撤销/粘贴都这么做）
        PositionEditor(atLine + 1);
        EnsureEditorVisible(atLine + 1);
        UpdateStatus();
        return true;
    }

    private async void OnSelPasteClicked(object? sender, EventArgs e)
    {
        // 粘贴到**正在编辑的那一行**的光标处；没在编辑就先进入该行编辑
        var text = await Clipboard.GetTextAsync();
        if (string.IsNullOrEmpty(text)) { ShowToast("剪贴板是空的"); return; }
        if (!_canEdit || _readOnly) { ShowToast("只读文件不能粘贴"); return; }

        // ⚠ 用上面算好的 `line`（已经夹到 ≥1），**别把 `Canvas.CaretLine` 直接传进去** ——
        // 它可能是 -1（比如刚 SetDocument 过、选区被静默清掉），那样 `BeginEditLine(-1)` 会把
        // `_editLine` 设成 -2，随后 TextChanged/Commit 都因为 `_editLine < 0` 提前返回 ——
        // 文字被静默丢掉，而输入框是透明的，屏幕上什么异常都看不到。
        long line = Canvas.CaretLine > 0 ? Canvas.CaretLine : 1;
        if (_editLine != line - 1) BeginEditLine(line);
        await Task.Delay(60);   // 等输入框就位（BeginEditLine 里也有等待，这里兜一层）
        int at = Math.Clamp(LineEditor.CursorPosition, 0, (LineEditor.Text ?? "").Length);
        var cur = LineEditor.Text ?? "";
        LineEditor.Text = cur[..at] + text + cur[at..];
        LineEditor.CursorPosition = at + text.Length;
        Canvas.EditingCursor = LineEditor.CursorPosition;
        Canvas.ClearSelection();
        UpdateSelectionBar();
    }

    private void OnSelCloseClicked(object? sender, EventArgs e)
    {
        Canvas.ClearSelection();
        UpdateSelectionBar();
    }

    /// <summary>
    /// 当前是不是暗色主题 —— **判据只有这一处**（打开文件时传给画布的 <c>dark</c>、面板 Tab
    /// 的选中态、选区操作条都问它）。与 <c>EditorTypography</c> 的 <c>Xxx / XxxDark</c> 配对惯例一致。
    /// </summary>
    private static bool IsDarkTheme => Application.Current?.RequestedTheme == AppTheme.Dark;

    /// <summary>本文件当前的诊断（读不到就当没有，绝不因为诊断查询把编辑器搞崩）。</summary>
    private List<Diagnostic> DiagnosticsForThisFile()
    {
        if (_relPath.Length == 0) return [];
        try { return DiagnosticManager.GetAll(_relPath); }
        catch { return []; }
    }

    /// <summary>
    /// 用户一动手就把这一批诊断清掉 —— 否则改完源码之后，屏幕上还赖着一批**已经不对**的
    /// 报错（气泡与行下波浪线都画在代码上），而它们指的行号多半也偏了。
    /// </summary>
    private void ClearDiagnosticsOnEdit()
    {
        if (DiagnosticsForThisFile().Count == 0) return;
        try { DiagnosticManager.Inject(_relPath, []); } catch { }
        Canvas.Invalidate();
        RefreshErrorList();      // 三处（气泡/波浪线/错误列表）读同一份，必须一起刷
        UpdateStatus();
    }

    // ══════════════════════════════════════════════════════════════════
    // 浮动代码辅助输入条
    //
    // 手机上打字，括号分号这些符号要切到符号键盘、关键字要一个字一个字敲。
    // 这条浮条把「最常用的那批」摊在手边：符号 / 运算符 / **按当前语言自动填的关键字**。
    // ══════════════════════════════════════════════════════════════════

    /// <summary>不碰它多久就缩小半透明（毫秒）。用户指定 10 秒。</summary>
    private const int AssistIdleMs = 10_000;

    /// <summary>菜单里那一项的文案 —— 菜单列表与 switch 两处引用同一个常量，别各写字面量。</summary>
    private const string AssistMenuLabel = "⌨  辅助输入条（开关）";

    private IDispatcherTimer? _assistIdle;
    private bool _assistPlaced;                  // 首次显示时给一个初始位置

    /// <summary>
    /// **停靠位**（用户把它拖到了哪儿）—— 它是 `TranslationX/Y` 的**真源**。
    ///
    /// 为什么不能直接拿 `TranslationX/Y` 当停靠位：弹出/收回动画会**临时**把它改成起手位
    /// （图标那边），动画途中读到的是"路过"的值 —— 下一次打开就从半路上起飞、位置一路漂。
    /// </summary>
    private double _assistRestX, _assistRestY;

    /// <summary>第一次打开时条子还没被测量过（`Width` 是 -1）⇒ 起手位算不出来，等尺寸到了再补动画。</summary>
    private bool _assistPopPending;

    /// <summary>收回动画正在放。放完才真的 `IsVisible = false`；这期间再点开关是"别收了、弹回来"。</summary>
    private bool _assistHiding;

    /// <summary>本次拖动是否已经开始（`StartInteraction` 到过）。</summary>
    private bool _assistPanActive;

    /// <summary>正在拖动 —— 拖动期间**不许**收小（收小会同时改 Scale/Opacity 触发重排）。</summary>
    private bool _assistDragging;

    /// <summary>
    /// 符号表（括号/引号/标点）。**与运算符分开** —— 两者混在一张表里反而难找。
    /// ⚠ 斜杠 `/` 与反斜杠 `\` 是用户点名要加的：路径、注释、转义都用得到，
    /// 而手机符号键盘上它们藏在很后面。C# 里反斜杠写成 `"\\"`。
    /// </summary>
    private static readonly string[] AssistSymbols =
        ["(", ")", "[", "]", "{", "}", "<", ">", "\"", "'", "`", ";", ":", ",", ".", "_",
         "|", "/", "\\", "@", "#", "$", "?", "!"];

    /// <summary>
    /// 运算符表。
    ///
    /// ⚠ **分不清归属的字符两边都加**（用户定的规则）：单个 `?` 是标点，而 `(a)?b:c` 里
    /// `?` 与 `:` 是**三元运算符**的两个半边；`!` 单独看像标点、`!x` 里是逻辑非；
    /// `<` `>` 既是泛型/模板的尖括号、又是比较运算符；`|` 既作分隔又作按位或。
    /// 分类本来就不是非此即彼的 —— 硬归一边，总有一种写法在表里找不到。
    /// </summary>
    private static readonly string[] AssistOperators =
        ["=", "==", "!=", "<=", ">=", "<", ">", "+", "-", "*", "/", "%", "+=", "-=", "*=", "/=",
         "&&", "||", "&", "|", "^", "~", "<<", ">>", "++", "--", "->", "=>", "::",
         "!", "?", ":"];

    /// <summary>关键字表为空时的兜底（认不出语言的扩展名会落到这里）。</summary>
    private static readonly string[] AssistFallbackKeywords =
        ["if", "else", "for", "while", "return", "break", "continue", "int", "char", "void"];

    /// <summary>
    /// **高频关键字**（跨语言共用的一份优先级表）—— 只用来**筛选和排序各语言自己的词表**，
    /// **不是第二份关键字表**：里面没有的、语言里也不会有（那种平行表迟早和 `Syntax` 漂开，
    /// 本仓库的头号坑）。顺序即优先级，控制流放最前。
    ///
    /// 用户的原话是「关键字应该分类，首页是高频关键字，那样才会输入快，现在要去找，输入麻烦」——
    /// 原来直接按字典序排，`auto`/`break`/`byte`… 挤在前面，要找个 `while` 得扫一遍。
    /// </summary>
    private static readonly string[] HotKeywords =
        ["if", "else", "for", "while", "do", "switch", "case", "default", "break", "continue", "return",
         "then", "end", "begin", "function", "def", "fn",
         "int", "char", "float", "double", "bool", "void", "var", "let", "const", "string",
         "struct", "class", "enum", "typedef", "static", "public", "private",
         "nil", "null", "true", "false", "print", "puts"];

    private void OnAssistMenuClicked() => ToggleAssistBar();

    private void ToggleAssistBar()
    {
        if (AssistBar.IsVisible && !_assistHiding) { CloseAssistBar(); return; }

        // 走到这儿有两种情形：还没打开；或者**收回动画放到一半又要打开** ——
        // 后者直接接着弹回去（OpenAssistBar 会把 _assistHiding 清掉，
        // 于是收回动画收尾那一下就不会再把它藏起来了）。
        OpenAssistBar();
    }

    private void OpenAssistBar()
    {
        _assistHiding = false;

        if (!_assistPlaced)
        {
            // 初始位置：左侧偏下（避开右上角的工具栏与浮层），之后由用户拖
            _assistRestX = 8;
            _assistRestY = Math.Max(8, Canvas.Height - 240);
            _assistPlaced = true;
        }

        // 掐掉在途动画（可能是上一次没放完的弹出，也可能是正在放的收回）
        AssistBar.CancelAnimations();
        // 起手位是**相对停靠位**算出来的（见 AssistPopStart），所以先把 Translation 摆回停靠位
        AssistBar.TranslationX = _assistRestX;
        AssistBar.TranslationY = _assistRestY;
        AssistBar.IsVisible = true;

        PlayAssistPop();
        // ⚠ 这里**不能**顺手调 AssistTouch()：它内含 RestoreAssistScale()，会把 Scale/Opacity
        // 立刻动画到 1/1 —— 与刚起头的弹出动画抢同一个属性，弹出就没了。
        RestartAssistIdle();
        UpdateAssistButtonState();
    }

    /// <summary>
    /// 关闭浮条 —— **倒着放一遍弹出动画**（缩回图标大小、飞回图标位置、淡出），演完才真隐藏。
    /// 用户要求「关闭动画就反过来」。
    /// </summary>
    private void CloseAssistBar()
    {
        HideAssistPopup();
        _assistIdle?.Stop();
        UpdateAssistButtonState();

        if (!AssistBar.IsVisible) return;

        // 还在等测量（弹出动画根本没开始）⇒ 没有可倒放的，直接收掉
        if (_assistPopPending || AssistBar.Width <= 0 || AssistBar.Height <= 0)
        {
            _assistPopPending = false;
            AssistBar.Opacity = 1;
            AssistBar.IsVisible = false;
            return;
        }

        AssistBar.CancelAnimations();
        AssistBar.TranslationX = _assistRestX;    // 同上：起手位按停靠位算
        AssistBar.TranslationY = _assistRestY;
        var (x, y, s) = AssistPopStart();

        _assistHiding = true;
        _assistTargetScale = s;
        _assistTargetOpacity = 0;
        _ = HideWhenDoneAsync(
            AssistBar.TranslateTo(x, y, AssistAnimMs, Easing.CubicIn),
            AssistBar.ScaleTo(s, AssistAnimMs, Easing.CubicIn),
            AssistBar.FadeTo(0, AssistAnimMs, Easing.CubicIn));
    }

    /// <summary>
    /// 收回动画放完再真隐藏。
    /// 演到一半又被打开时 `_assistHiding` 已被 <see cref="OpenAssistBar"/> 清掉 —— 那就什么都不做
    /// （否则会在用户刚打开的一瞬间把它藏掉）。
    /// </summary>
    private async Task HideWhenDoneAsync(Task moving, Task scaling, Task fading)
    {
        await Task.WhenAll(moving, scaling, fading);
        if (!_assistHiding) return;
        _assistHiding = false;

        AssistBar.IsVisible = false;
        // 复位：下次打开从干净状态起（否则会从"图标大小 + 已淡出"开始，头几帧是空的）
        AssistBar.Scale = 1;
        AssistBar.Opacity = 1;
        AssistBar.TranslationX = _assistRestX;
        AssistBar.TranslationY = _assistRestY;
        _assistTargetScale = 1;
        _assistTargetOpacity = 1;
    }

    private void OnAssistToggleClicked(object? sender, EventArgs e) => ToggleAssistBar();

    /// <summary>
    /// 工具栏那颗开关按钮带**状态**：开着时满不透明、关着时压到 0.45。
    /// 不带状态的话，「点了没反应」与「它本来就开着」在屏幕上分不出来。
    /// 放在 Toggle/Close 里而不是点击处理器里 —— 菜单那条路也走这两个方法，状态才不会漏更新。
    /// </summary>
    private void UpdateAssistButtonState()
        => AssistBtn.Opacity = AssistBar.IsVisible ? 1.0 : 0.45;

    private void OnAssistCloseClicked(object? sender, EventArgs e) => CloseAssistBar();

    /// <summary>「有人在操作」—— 恢复大小并把 10 秒的计时重新开始。所有交互入口都调它。</summary>
    private void AssistTouch()
    {
        RestoreAssistScale();
        RestartAssistIdle();
    }

    /// <summary>
    /// 重起 10 秒收表 —— **不碰 Scale/Opacity**。
    /// 与 <see cref="AssistTouch"/> 分开是因为弹出动画期间只该重起计时、不该动那两个属性
    /// （动了就和弹出动画抢，见 <see cref="OpenAssistBar"/>）。
    /// </summary>
    private void RestartAssistIdle()
    {
        _assistIdle ??= CreateAssistIdleTimer();
        _assistIdle.Stop();
        _assistIdle.Start();
    }

    private IDispatcherTimer CreateAssistIdleTimer()
    {
        var t = Dispatcher.CreateTimer();
        t.Interval = TimeSpan.FromMilliseconds(AssistIdleMs);
        t.IsRepeating = false;                 // 只缩一次；下一次交互重新起表
        t.Tick += (_, _) => ShrinkAssist();
        return t;
    }

    /// <summary>闲置缩小后的比例与不透明度。</summary>
    private const double AssistShrinkScale = 0.72;
    private const double AssistShrinkOpacity = 0.45;

    /// <summary>缩放动画时长（毫秒）。够短才不像「卡了一下」，够长才看得出是「缩」不是「跳」。</summary>
    private const uint AssistAnimMs = 170;

    /// <summary>动画的**目标值**（不是当前值）—— 用它做防重入，见 <see cref="AnimateAssist"/>。</summary>
    private double _assistTargetScale = 1, _assistTargetOpacity = 1;

    private void ShrinkAssist()
    {
        if (!AssistBar.IsVisible) return;
        if (_assistDragging) return;   // 拖动中不收：那一刻改 Scale/Opacity 就是「闪缩」本身
        HideAssistPopup();                     // 缩小了还把弹表支着，看着像没收干净
        AnimateAssist(AssistShrinkScale, AssistShrinkOpacity);
    }

    private void RestoreAssistScale() => AnimateAssist(1, 1);

    /// <summary>
    /// **无动画**地恢复原大小 —— 只在**拖动开始**那一刻用。
    ///
    /// 为什么不在这里也放动画：`Scale` 会改变「父容器坐标 → 控件内坐标」的换算
    /// （平台按 `(父 − 位移) ÷ 缩放` 折算），而拖动的位置公式 `T += (t − t₀)` 吃的就是这个坐标。
    /// 缩放一边动画一边跟手，等于**尺子本身在被拉伸** —— 手指没动、报出来的坐标却在变，
    /// 条子就会在起手那一两帧滑一段（抓得越靠边滑得越远）。
    /// 起手瞬间**直落**到 1 就没有这段扰动；手指正在动，跳跃本来也看不出来。
    /// 其余路径（点一下恢复、闲置缩小）都走动画。
    /// </summary>
    private void RestoreAssistScaleNow()
    {
        _assistTargetScale = 1;
        _assistTargetOpacity = 1;
        AssistBar.CancelAnimations();    // 掐掉在途的缩小动画，否则它还会继续改 Scale
        AssistBar.Scale = 1;
        AssistBar.Opacity = 1;
    }

    /// <summary>
    /// 缩放 + 淡出，**带动画**（用户要求）。
    ///
    /// **支点在左上角**（`AssistBar.AnchorX/AnchorY = 0`，写在 XAML 里）：默认是围绕中心缩放，
    /// 缩一次左上角就往右下跑一截、放大又跑回来 —— 用户的原话是「居中就会感觉拖动位置乱跑」。
    /// 支在左上角则**位置一动不动、只改大小**，下次还能照着原位置去抓。
    ///
    /// 防重入比对的是**目标值**而不是当前值：拖动开始的每一帧都会调
    /// <see cref="RestoreAssistScale"/>，若拿「当前 Scale」判（动画途中它在 0.72 与 1 之间），
    /// 每一帧都会判定「还没到、再起一次动画」，几十个动画叠在一起就是抖动本身。
    /// </summary>
    private void AnimateAssist(double scale, double opacity)
    {
        if (Math.Abs(_assistTargetScale - scale) < 0.001 &&
            Math.Abs(_assistTargetOpacity - opacity) < 0.001) return;

        _assistTargetScale = scale;
        _assistTargetOpacity = opacity;
        _ = AssistBar.ScaleTo(scale, AssistAnimMs, Easing.CubicOut);
        _ = AssistBar.FadeTo(opacity, AssistAnimMs, Easing.CubicOut);
    }

    // ── 弹出 / 收回 ──
    //
    // 用户要求：**打开时从工具栏那颗图标的位置与大小"弹"到停靠位与正常大小，关闭就反过来**。
    // 所以这是一条**可逆**的动画：两边共用 AssistPopStart() 算同一组起手状态，
    // 只是一个用 CubicOut 往外弹、一个用 CubicIn 倒着收回去。

    /// <summary>
    /// 起手那一下的不透明度。**不取 0** —— 起点只占头一两帧，全透明就"看不出是从哪儿冒出来的"，
    /// 而这条动画的全部意义就是让人看见它从图标那儿出来。
    /// </summary>
    private const double AssistPopStartOpacity = 0.15;

    /// <summary>
    /// **弹出**：先把条子摆到「图标位置 + 图标大小」，再动画到停靠位与原始大小。
    ///
    /// `Scale`/`Opacity` 这一段与 <see cref="AnimateAssist"/> 共用防重入口径（比对**目标值**），
    /// 所以这里要自己把目标值写成 1/1：否则紧随其后的 <see cref="RestoreAssistScale"/>
    /// （点一下条子会走它）会以为"已经在 1 了"而提前返回。
    /// </summary>
    private void PlayAssistPop()
    {
        // 第一次打开：条子还没被测量过（Width 是 -1），起手位算不出来。
        // 先整个藏起来（Opacity=0），等 SizeChanged 量到真实宽高再补 —— 见 OnAssistBarSizeChanged。
        if (AssistBar.Width <= 0 || AssistBar.Height <= 0)
        {
            _assistPopPending = true;
            _assistTargetScale = 1;
            _assistTargetOpacity = 1;
            AssistBar.Opacity = 0;
            return;
        }

        _assistPopPending = false;
        var (x, y, s) = AssistPopStart();

        AssistBar.CancelAnimations();
        AssistBar.Scale = s;
        AssistBar.Opacity = AssistPopStartOpacity;
        AssistBar.TranslationX = x;
        AssistBar.TranslationY = y;

        _assistTargetScale = 1;
        _assistTargetOpacity = 1;
        _ = AssistBar.ScaleTo(1, AssistAnimMs, Easing.CubicOut);
        _ = AssistBar.FadeTo(1, AssistAnimMs, Easing.CubicOut);
        _ = AssistBar.TranslateTo(_assistRestX, _assistRestY, AssistAnimMs, Easing.CubicOut);
    }

    /// <summary>
    /// 第一次打开时尺寸还量不到（`Width` 是 -1）—— 等它到了再把弹出动画补上
    /// （等待位由 <see cref="PlayAssistPop"/> 置）。
    ///
    /// ⚠ 这条路**只对"还没量过"生效**：`SizeChanged` 只在尺寸**变了**才发，
    /// 第二次打开尺寸没变、不会触发，所以已量到尺寸时必须在 <see cref="PlayAssistPop"/> 里当场起动画，
    /// 不能一律挂在事件上等（那样第二次打开会永远等不到、条子停在 Opacity=0 上等于没开）。
    /// </summary>
    private void OnAssistBarSizeChanged(object? sender, EventArgs e)
    {
        if (!_assistPopPending) return;
        if (AssistBar.Width <= 0 || AssistBar.Height <= 0) return;   // 隐藏期间平台可能报 0，那不是有效尺寸
        _assistPopPending = false;
        PlayAssistPop();
    }

    /// <summary>
    /// 弹出动画的**起手状态**：条子缩到"跟工具栏那颗图标差不多大"、中心压在图标中心上，
    /// 再钳进画布范围。返回 (TranslationX, TranslationY, Scale)。
    ///
    /// ⚠ 调用前必须先把 `AssistBar.TranslationX/Y` 摆回**停靠位**（`_assistRestX/Y`）——
    /// 下面的公式是「停靠位 + 图标相对条子的偏移」，读到动画途中的值就会一路算歪。
    /// </summary>
    private (double X, double Y, double Scale) AssistPopStart()
    {
        double barW = AssistBar.Width, barH = AssistBar.Height;

        // 「缩到图标那么大」按**面积**折算成等比缩放：这条子又长又扁 ——
        // 按宽度比会小成一根线，按高度比反倒比原图还大；面积比既保住长宽比，
        // 又确实"跟那颗图标差不多大"。
        double iconW = AssistBtn.Width > 0 ? AssistBtn.Width : barH;
        double iconH = AssistBtn.Height > 0 ? AssistBtn.Height : barH;
        double s = Math.Clamp(Math.Sqrt(iconW * iconH / (barW * barH)), 0.12, 0.9);

        // 起点 = 图标中心（缩完的条子**中心**压在图标中心上，才像"从那颗按钮里冒出来"）。
        // 图标在上一行的工具条里、条子的父容器是下面那一格 ——
        // 两者先换算到同一个坐标系（整页）再作差，不能拿各自的 X/Y 直接减。
        var icon = PosIn(this, AssistBtn);
        var bar = PosIn(this, AssistBar);          // 此刻 Translation 就是停靠位
        double left = icon.X + iconW / 2 - barW * s / 2;
        double top = icon.Y + iconH / 2 - barH * s / 2;

        // ⚠ 图标在工具条的**最右一格**，而条子缩完仍有一百多宽 ⇒ 照图标居中会顶出屏幕右缘；
        // 纵向更是整颗图标都在条子容器的**上方**（工具条是上一行）。钳进画布范围之后，
        // 起手位落在"工具条正下方那一角"—— 仍看得出是从那颗按钮出来的，又不会被画到没有画布的地方。
        double x = Math.Clamp(_assistRestX + left - bar.X, 0, Math.Max(0, Canvas.Width - barW * s));
        double y = Math.Clamp(_assistRestY + top - bar.Y, 0, Math.Max(0, Canvas.Height - barH * s));
        return (x, y, s);
    }

    /// <summary>元素左上角在 <paramref name="root"/> 坐标系里的位置（逐级累加布局位置与位移）。</summary>
    private static (double X, double Y) PosIn(VisualElement root, VisualElement element)
    {
        double x = 0, y = 0;
        for (VisualElement? e = element; e is not null && e != root; e = e.Parent as VisualElement)
        {
            x += e.X + e.TranslationX;
            y += e.Y + e.TranslationY;
        }
        return (x, y);
    }

    // ── 拖动（GraphicsView 原始触摸）──
    //
    // **为什么不用 PanGestureRecognizer**（真机实测，当时挂了个 logcat 探针量的）：
    // 它上报的 `TotalX` 是**两条各自有效、相差一个恒定偏移（~40~50 DIP）的流交替出现**——
    // 手指快速拖动时是两条平滑轨迹在跳（各自都在正确地跟手），
    // **程序化的匀速慢划（1200ms、纯注入、没有人手）同样是 ±39 的来回** ⇒ 与速度、与人手都无关。
    // 成因是「两个各记各的起点的监听器」，而 `PanUpdatedEventArgs` 只有 TotalX/TotalY，
    // **没有任何字段能把它们区分开** ⇒ 这条路修不动。
    //
    // 换成 `GraphicsView` 亲手接原始触摸：它是**绝对坐标**（不是「相对某个起点」），
    // 所以天生免疫「起点不同」这类问题。画布的选区手柄走的就是这条路，
    // 用户实测「手柄拖动不乱晃」—— 这是有对照的选择，不是猜的。

    /// <summary>起手时手指在拖动面里的坐标（拖动面自己的坐标系）。</summary>
    private PointF _assistGrabT0;

    /// <summary>最近一次收到的手指坐标 —— 抬手那一刻用它判「点到了哪一格」。</summary>
    private PointF _assistLastT;

    /// <summary>手指是否已经越过门槛。没过 = 这次手势是**点击**，不是拖动。</summary>
    private bool _assistGrabMoved;

    /// <summary>起手点落在 ✕ 上 —— 用户指定「关闭那一格不可拖动」，整次手势都不拖。</summary>
    private bool _assistGrabOnClose;

    /// <summary>
    /// 「算拖动」的位移门槛（DIP）。Android 自己的 touch slop 也是这个量级。
    ///
    /// 有它才能**同时**做到两件用户都要求的事：「点按钮就是点按钮」与「整条都能拖」——
    /// 不看门槛的话，手指按上按钮那一瞬间的抖动就会被当成拖动，按钮再也点不准。
    /// </summary>
    private const double AssistDragSlop = 8;

    private void OnAssistDragStart(object? sender, TouchEventArgs e)
    {
        if (e.Touches.Length == 0) return;
        if (_assistHiding) return;      // 收回动画中：这条正在消失，别接手势（接了会和收回动画抢属性）
        _assistGrabT0 = _assistLastT = e.Touches[0];
        _assistGrabMoved = false;
        _assistGrabOnClose = AssistCloseRect.Contains(_assistGrabT0.X, _assistGrabT0.Y);
    }

    /// <summary>
    /// 拖动面**自己会跟着条子一起动**，所以它报出来的坐标是「手指相对条子」的位置 ——
    /// 直接拿它当位移会变成一个闭环（条子跟手 → 相对位置不变 → 不动）。
    ///
    /// 正确的形式是**每次把「当前偏移误差」补上去**：`T += (t − t₀)`。
    /// 推导：设手指在父容器里的位置 F，条子位移 T，则 t = F − T（忽略常量），
    /// t₀ = F₀ − T₀；代入得 T_new = T + (t − t₀) —— 每帧补一次，一帧就收敛（有一帧跟随延迟，看不出来）。
    /// **不要**写成 `T = T₀ + (t − t₀)`（那是把误差当成绝对位移，手指一动条子就卡住不动了），
    /// 也**不要**按纯增量 `T += (t − t_prev)`（那会退化成「动一帧停一帧」的半速抖动）。
    ///
    /// 拖动中**每次都钳进可视区**（不是只在松手时）：万一上面的推导不成立（比如平台某天改成
    /// 报父容器坐标），闭环会退化成平方增长，钳位能让它「贴在边上」而不是飞出去，
    /// 现场也还看得见，不至于变成「条子没了」。
    /// </summary>
    private void OnAssistDragMove(object? sender, TouchEventArgs e)
    {
        if (e.Touches.Length == 0) return;
        var p = _assistLastT = e.Touches[0];
        if (_assistGrabOnClose) return;      // ✕ 那一格：整次手势都不拖（点它照旧关闭）

        if (!_assistGrabMoved)
        {
            if (Math.Abs(p.X - _assistGrabT0.X) < AssistDragSlop &&
                Math.Abs(p.Y - _assistGrabT0.Y) < AssistDragSlop)
                return;                      // 还没过门槛 —— 仍按点击处理
            _assistGrabMoved = true;
            BeginAssistDrag();               // 确定是拖动了，这时才置拖动态
        }

        AssistBar.TranslationX += p.X - _assistGrabT0.X;
        AssistBar.TranslationY += p.Y - _assistGrabT0.Y;
        ClampAssistIntoView();
    }

    private void OnAssistDragEnd(object? sender, TouchEventArgs e)
    {
        if (_assistGrabMoved)
        {
            EndAssistDrag();
            return;
        }

        // 没越过门槛 = 一次点击：**按坐标判定点到了哪一格**。
        // 为什么由拖动面代派：按钮设了 `InputTransparent`（不设的话它们会吞掉触摸、
        // 整条就只有 ⣿ 能拖），既然触摸到不了按钮，点击也就只能在这一层判。
        // 判据只有 AssistHitTargets 一份，与按钮的实际位置同源（都取自 Bounds）。
        foreach (var (rect, act) in AssistHitTargets())
        {
            if (!rect.Contains(_assistLastT.X, _assistLastT.Y)) continue;
            act();
            return;
        }
        AssistTouch();      // 点在空白处：什么也不做，只把 10 秒收表重新起
    }

    /// <summary>
    /// 某个子控件在**拖动面坐标系**里的矩形。
    ///
    /// 拖动面填满整条，按钮在它上层的 `AssistRow` 里 ⇒ 按钮的 `Bounds` 是相对那个布局的，
    /// 加上布局自身的 `Bounds` 就落到拖动面的坐标系（两者是同一个 Grid 单元，原点相同）。
    ///
    /// ⚠ 用**布局坐标**而不是屏幕坐标是对的：闲置缩小（`Scale=0.72`）时整条带一个缩放变换，
    /// 而平台会把触摸换算回子视图的**未缩放**坐标 —— 两边同源，所以缩放态下也不会判错格。
    /// </summary>
    private RectF RectInAssistRow(VisualElement child)
    {
        var row = AssistRow.Bounds;
        var b = child.Bounds;
        return new RectF((float)(row.X + b.X), (float)(row.Y + b.Y), (float)b.Width, (float)b.Height);
    }

    private RectF AssistCloseRect => RectInAssistRow(AssistCloseBtn);

    /// <summary>
    /// 「点到了哪一格 → 做什么」的**唯一一份**表。
    /// 每个动作与按钮自己的 `Clicked` 处理器调**同一个 `AssistDo*` 方法**，别两边各写一遍。
    /// </summary>
    private IEnumerable<(RectF Rect, Action Fire)> AssistHitTargets()
    {
        yield return (RectInAssistRow(AssistBackspaceBtn), AssistDoBackspace);
        yield return (RectInAssistRow(AssistTabBtn), AssistDoTab);
        yield return (RectInAssistRow(AssistSymBtn), AssistDoSymbols);
        yield return (RectInAssistRow(AssistOpBtn), AssistDoOperators);
        yield return (RectInAssistRow(AssistKwBtn), AssistDoKeywords);
        yield return (RectInAssistRow(AssistCloseBtn), AssistDoClose);
    }

    // ── 每一格「做什么」——按钮的 Clicked 与拖动面的坐标判定**共用这一份** ──

    private void AssistDoBackspace() => AssistBackspaceAsync();

    /// <summary>点空白处之外的分派入口；每个 `AssistDo*` 自己负责「有人在操作」的记账。</summary>
    private void AssistDoTab() { AssistTouch(); AssistInsertAsync("\t"); }

    private void AssistDoSymbols() { AssistTouch(); ShowAssistPopup(AssistSymbols, "符号"); }

    private void AssistDoOperators() { AssistTouch(); ShowAssistPopup(AssistOperators, "运算符"); }

    /// <summary>
    /// 关键字表 —— **按当前语言自动填**：直接问 `Syntax.ForFile` 的 <c>Keywords</c>
    /// （与语法高亮同一份词表），**不另立一张表**。认不出语言时用兜底表。
    /// </summary>
    private void AssistDoKeywords()
    {
        AssistTouch();
        var kw = Canvas.SyntaxForFile?.Keywords;
        if (kw is not { Count: > 0 })
        {
            ShowAssistPopup(AssistFallbackKeywords, "关键字");
            return;
        }

        // 排序 = **先高频、再其余**，各自按字典序（用户要求「首页是高频关键字」）。
        // ⚠ 匹配高频表必须**不区分大小写**：词表按各语言的书写惯例存
        // （BASIC/Fortran 大写、别的语言小写），用区分大小写的比较会让 BASIC 的 IF 一个都排不到前面。
        var rank = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < HotKeywords.Length; i++) rank.TryAdd(HotKeywords[i], i);

        var hot = kw.Where(rank.ContainsKey).OrderBy(k => rank[k]).ToList();
        var rest = kw.Where(k => !rank.ContainsKey(k)).OrderBy(k => k, StringComparer.Ordinal).ToList();

        ShowAssistPopup([.. hot, .. rest], "关键字", hot.Count);
    }

    private void AssistDoClose() => CloseAssistBar();

    private void BeginAssistDrag()
    {
        _assistPanActive = true;
        _assistDragging = true;

        // 拖动期间**不碰 Scale/Opacity**：这两个属性会让容器重新测量，
        // 而拖动每秒来几十上百次事件 —— 每帧重排就是「不顺滑」。
        // 起手这一次用**无动画**版本（缩放动画会一边改坐标尺子一边跟手，见 RestoreAssistScaleNow）。
        // 顺手把 10 秒的收表停掉：拖动中每次重置计时器本身也是白做的功。
        RestoreAssistScaleNow();
        _assistIdle?.Stop();
    }

    private void EndAssistDrag()
    {
        _assistPanActive = false;
        _assistDragging = false;
        ClampAssistIntoView();
        AssistTouch();          // 松手才重新起 10 秒的收表
    }

    /// <summary>拖出屏幕就回不来（那条浮条没有别的入口）—— 拖动中与松手时都钳一次。</summary>
    private void ClampAssistIntoView()
    {
        // ⚠ 宽度取**未缩放**的布局宽：`Scale` 只是绘制变换，钳位用的是布局几何。
        double w = AssistBar.Width > 0 ? AssistBar.Width : 220;
        double h = AssistBar.Height > 0 ? AssistBar.Height : 40;
        double maxX = Math.Max(0, Canvas.Width - w);
        double maxY = Math.Max(0, Canvas.Height - h);
        AssistBar.TranslationX = Math.Clamp(AssistBar.TranslationX, 0, maxX);
        AssistBar.TranslationY = Math.Clamp(AssistBar.TranslationY, 0, maxY);

        // 停靠位跟着走 —— 它是 TranslationX/Y 的**真源**（弹出/收回动画会临时改写 Translation），
        // 拖动每帧都过这里，所以同步放这一处就够。
        _assistRestX = AssistBar.TranslationX;
        _assistRestY = AssistBar.TranslationY;
    }

    // ── 分类表 ──

    // ⚠ 这几个 `Clicked` 处理器的接线**保留**（按钮虽然设了 `InputTransparent`、点击实际由
    // 拖动面按坐标派发），理由是：万一哪天平台行为变了、透明不再生效，按钮还能自己工作 ——
    // 而两边调的是**同一个 `AssistDo*`**，不存在「改一处忘另一处」。
    private void OnAssistBackspaceClicked(object? sender, EventArgs e) => AssistDoBackspace();

    /// <summary>
    /// **自己实现的退格**（不依赖输入法）。
    ///
    /// 为什么需要：用户实测发现**部分输入法的退格只作用于它自己的联想词库、从不回调编辑框**
    /// （症状：候选区随退格变化、文本一个字不动）。那种情况下 `InputConnection` 这条路
    /// 根本够不着 —— 它压根不调用我们，拦也没得拦。应用只能自己给一个退格键。
    ///
    /// 语义与系统退格一致：有选区先删选区 → 行中删一个字符 → 行首与上一行合行。
    /// 三条路都复用页面上已有的实现（与平台那条输入连接走的是同一批方法）。
    /// </summary>
    private async void AssistBackspaceAsync()
    {
        AssistTouch();
        if (_editable == null) { ShowToast("大文件以只读方式打开，不能编辑"); return; }
        if (_readOnly)
        {
            if (!_canEdit || _fileReadOnly) { ShowToast("这个文件不能编辑"); return; }
            SetReadOnly(false);
        }

        if (DeleteSelectionCore()) return;   // 有可见选区 → 删选区

        long line = Canvas.CaretLine > 0 ? Canvas.CaretLine : 1;
        if (_editLine != line - 1)
        {
            BeginEditLine(line);
            await Task.Delay(60);
        }
        if (_editLine < 0) { ShowToast("先点一下要编辑的那一行"); return; }

        int at = Math.Clamp(LineEditor.CursorPosition, 0, (LineEditor.Text ?? "").Length);
        if (at == 0)
        {
            JoinWithPreviousLine();          // 行首 → 与上一行合行
            return;
        }

        // 行中：删掉光标前一个字符。
        // ⚠ 按**码元**删会有代理对问题（emoji/CJK 扩展 B 是两个 char）—— 退格时要整个删掉，
        // 不能只删半个。所以先判断是不是低位代理，是就多删一个。
        var cur = LineEditor.Text ?? "";
        int del = 1;
        if (at >= 2 && char.IsLowSurrogate(cur[at - 1]) && char.IsHighSurrogate(cur[at - 2])) del = 2;

        LineEditor.Text = cur[..(at - del)] + cur[at..];
        LineEditor.CursorPosition = at - del;
        Canvas.EditingCursor = at - del;
        Canvas.EnsureCaretVisible();
    }

    private void OnAssistTabClicked(object? sender, EventArgs e) => AssistDoTab();

    private void OnAssistSymbolsClicked(object? sender, EventArgs e) => AssistDoSymbols();

    private void OnAssistOperatorsClicked(object? sender, EventArgs e) => AssistDoOperators();

    private void OnAssistKeywordsClicked(object? sender, EventArgs e) => AssistDoKeywords();

    /// <summary>
    /// 弹出分类表。
    /// </summary>
    /// <param name="items">格子内容，**前面 <paramref name="hotCount"/> 个是高频项**（底色更实）。</param>
    /// <param name="owner">
    /// 分类标签（「符号」/「运算符」/「关键字」）—— 用来判「再点一次同一个分类就收起」。
    /// ⚠ 判据必须用**标签**而不是列表引用：关键字那一路每次都现排一个新 List，
    /// 用 `== items` 比引用的话永远不相等，那个「再点一次收起」对它从来没生效过。
    /// </param>
    /// <param name="hotCount">高频项个数（0 = 不分档）。</param>
    private void ShowAssistPopup(IReadOnlyList<string> items, string owner, int hotCount = 0)
    {
        // 再点同一个分类 = 收起（与「点一下按钮弹出、再点一下收回」的直觉一致）
        if (AssistPopup.IsVisible && _assistPopupOwnerTag == owner)
        {
            HideAssistPopup();
            return;
        }
        _assistPopupOwnerTag = owner;

        AssistPopupItems.Clear();
        for (int i = 0; i < items.Count; i++)
        {
            string item = items[i];
            // 尺寸按用户要求**整体缩到 3/4**（「给屏幕节省点空间」）：
            // 字号 / 内边距 / 外边距 / **最小宽高** 一起缩。只缩其中一两项会立刻变形
            // （只缩字号 → 按钮还是那么大、字变小了；只缩内边距 → 长短不一）。
            // ⚠ `MinimumHeightRequest` 不能漏：App 的全局 Button 隐式样式把它钉在 44
            // （`Resources/Styles/Styles.xaml:36`），不显式覆盖的话「缩到 3/4」只剩宽度生效、
            // 高度还是 44 —— 看着**一点没变小**。
            //
            // 配色跟随系统主题（用户要求）：浅色主题下「白 20% 的底 + 白字」在白底上等于看不见，
            // 所以浅色换成「黑 8% 的底 + 深色字」。这些格子是**每次打开弹表现建**的，
            // 用 `IsDarkTheme` 现取即可 —— 不必像浮条本体那样挂 `AppThemeBinding`。
            //
            // 高频项底色**更实一档**（用户要「高频关键字一眼可见」）：不靠位置暗示，
            // 因为一屏排下来「前几个」和「后面几个」看起来是一样的。
            bool dark = IsDarkTheme;
            bool hot = i < hotCount;
            var btn = new Button
            {
                Text = item,
                FontSize = 10,
                Padding = new Thickness(8, 2),
                Margin = new Thickness(1.5),
                MinimumWidthRequest = 34,
                MinimumHeightRequest = 30,
                BackgroundColor = (dark, hot) switch
                {
                    (true, true) => Color.FromArgb("#59FFFFFF"),
                    (true, false) => Color.FromArgb("#33FFFFFF"),
                    (false, true) => Color.FromArgb("#2E000000"),
                    (false, false) => Color.FromArgb("#14000000"),
                },
                TextColor = dark ? Colors.White : Color.FromArgb("#1A1A1A"),
            };
            var captured = item;
            btn.Clicked += (_, _) => { HideAssistPopup(); AssistInsertAsync(captured); };
            AssistPopupItems.Add(btn);
        }

        AssistPopup.WidthRequest = Math.Max(180, Math.Min(340, Canvas.Width - 16));
        AssistPopup.IsVisible = true;
        PositionAssistPopup();
    }

    /// <summary>当前弹表的分类标签（判「再点一次同一个分类就收起」，见 <see cref="ShowAssistPopup"/>）。</summary>
    private string? _assistPopupOwnerTag;

    private void HideAssistPopup()
    {
        AssistPopup.IsVisible = false;
        _assistPopupOwnerTag = null;
    }

    /// <summary>
    /// 弹表高度上限（DIP）——**与 XAML 里 `AssistPopupScroll.MaximumHeightRequest` 必须一致**。
    /// 只用在「量不出真实高度」的兜底分支上（量不出来是极小概率），但两处若漂开，
    /// 兜底那天就会重现「弹表离浮条老远」的老毛病。
    /// </summary>
    private const double AssistPopupMaxHeight = 165;

    /// <summary>弹表紧贴浮条的**上方或下方** —— 哪边放得下放哪边（用户要求「下方或者上方」）。</summary>
    private void PositionAssistPopup()
    {
        double barX = AssistBar.TranslationX;
        double barY = AssistBar.TranslationY;
        double barH = AssistBar.Height > 0 ? AssistBar.Height : 40;
        double popW = AssistPopup.WidthRequest > 0 ? AssistPopup.WidthRequest : 300;

        // 高度**量出来**，不要估。
        // 原来这里写死 220（注释还写着「与 ScrollView 的 MaximumHeightRequest 一致」），
        // 但那个上限后来调成了 165，而且真实高度取决于**排了几行** —— 符号二十来个、
        // 关键字上百个，行数差好几倍。写死一个值的后果是**往上弹时离浮条老远**：
        // 浮条在屏幕下方时 y = barY − 220 − 6，而弹表实际只有百来高，中间空出一大截
        // （用户实测：「往上弹出的按键矩阵距离输入条有点远，错位了」）。
        // `IView.Measure` 是同步的，还没上屏也能量出它要占多高。
        double popH = ((IView)AssistPopup).Measure(popW, Math.Max(1, Canvas.Height)).Height;
        if (popH <= 0) popH = AssistPopupMaxHeight;   // 量不出来才退回上限

        double x = Math.Clamp(barX, 4, Math.Max(4, Canvas.Width - popW - 4));
        double y = barY - popH - 6;              // 默认上方
        if (y < 4) y = barY + barH + 6;          // 上方放不下 → 下方

        AssistPopup.TranslationX = x;
        AssistPopup.TranslationY = Math.Max(4, y);
    }

    // ── 插入 ──

    /// <summary>
    /// 把一段文本插到光标处。**与「粘贴到光标处」同一条路**：没在编辑就先落到光标那一行，
    /// 再往 Entry 里插 —— 插入本身会经 TextChanged 写进模型（所以全角归一化等规则也一并生效）。
    /// </summary>
    private async void AssistInsertAsync(string text)
    {
        AssistTouch();
        if (text.Length == 0) return;

        if (_editable == null) { ShowToast("大文件以只读方式打开，不能输入"); return; }
        if (_readOnly)
        {
            // ⚠ 编辑器**打开时默认是只读**，所以原来那条「只读就拒绝」的守卫会让辅助条
            // 在默认状态下**完全没反应**（只弹一句很容易没看见的 Toast）——用户实测报的就是这个。
            // 辅助输入条本来就是**打字辅助**：点它就是要输入，不该先逼用户去按一下工具栏的 ✎。
            // 这里隐式切到编辑态，与「点某一行就开始编辑」是同一个语义。
            // 只有**真的改不了**的两种情形才拒绝（超可编辑上限 / 文件属性只读）。
            if (!_canEdit || _fileReadOnly) { ShowToast("这个文件不能编辑"); return; }
            SetReadOnly(false);
        }

        long line = Canvas.CaretLine > 0 ? Canvas.CaretLine : 1;
        if (_editLine != line - 1)
        {
            BeginEditLine(line);
            await Task.Delay(60);   // 等输入框就位（与 OnSelPasteClicked 同一个理由与同一个时长）
        }
        // 到这一步还没进编辑态 ⇒ 说明上面哪一环没成。**别静默失败**：
        // 「点了没反应」是手机上最难查的一类症状，这里直接把原因说出来。
        if (_editLine < 0) { ShowToast("没能进入编辑态，先点一下要插入的那一行"); return; }

        int at = Math.Clamp(LineEditor.CursorPosition, 0, (LineEditor.Text ?? "").Length);
        var cur = LineEditor.Text ?? "";
        LineEditor.Text = cur[..at] + text + cur[at..];
        LineEditor.CursorPosition = at + text.Length;
        Canvas.EditingCursor = LineEditor.CursorPosition;
        Canvas.EnsureCaretVisible();
    }

    private async Task CopyTextAsync(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        await Clipboard.SetTextAsync(text);
        ShowToast($"已复制 {text.Length} 字符");
    }

    private async Task CopyAllAsync()
    {
        if (_doc == null) return;
        var sb = new StringBuilder();
        for (long i = 0; i < _doc.LineCount; i++)
        {
            var l = _doc.GetLine(i);
            if (l == null) { await _doc.PrefetchAsync(i, Math.Min(i + 200, _doc.LineCount - 1)); l = _doc.GetLine(i) ?? ""; }
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(l);
            if (sb.Length > 2_000_000) break;   // 别把剪贴板撑爆
        }
        await CopyTextAsync(sb.ToString());
    }

    // ── 单行编辑 ──

    /// <summary>
    /// 插入制表符（**所有平台的 Tab 都走这里**，标准一致；键入口见
    /// <c>OnLineEditorPreviewKeyDown</c>（Windows）与 <c>OnAndroidLineEditorKeyPress</c>）。
    ///
    /// **插的是制表符这一个字符，不是 4 个空格** —— 语义上必须是真制表符：Python 的缩进与
    /// 三引号字符串里的制表符都有意义，换成空格是**改坏代码**而不只是改显示。
    /// 视觉上仍是 4 列：展开由绘制侧的 <c>ExpandTabs</c>（对齐到
    /// <see cref="EditorTypography.TabColumns"/>）负责；删除也天然只删 1 个字符。
    /// </summary>
    private void InsertIndent() => ReplaceEditing((cur, at) => (cur[..at] + "\t" + cur[at..], at + 1));

    /// <summary>
    /// 退一级缩进（Shift+Tab，同样全平台共用）。
    ///
    /// 规则与主流编辑器一致、且**只动行首那一段**：行首是制表符就删它一个；否则删掉最多
    /// <see cref="EditorTypography.TabColumns"/> 个前导空格（不足就删光）。没有缩进可退时
    /// **原样返回**（不报错、也不插别的东西）—— 空按一下不该改变文件。
    /// </summary>
    private void DedentLine()
    {
        ReplaceEditing((cur, at) =>
        {
            int remove = cur.StartsWith("\t") ? 1 : 0;
            if (remove == 0)
                while (remove < EditorTypography.TabColumns && remove < cur.Length && cur[remove] == ' ')
                    remove++;
            if (remove == 0) return (cur, at);
            return (cur[remove..], Math.Max(0, at - remove));
        });
    }

    /// <summary>
    /// 改「正在编辑的那一行」的文本并同步光标 —— Tab / Shift+Tab 共用的**唯一出口**。
    ///
    /// 走的是和粘贴同一条路（改 `LineEditor.Text` ⇒ `TextChanged` 回写模型 + 画布重绘），
    /// 不自己动 `_editable`，否则两处各写一份迟早不一致。
    /// 光标列在改文本**之后**按实际长度夹一次：越界会让画布与输入框各算各的，画到行外去。
    /// </summary>
    private void ReplaceEditing(Func<string, int, (string Text, int Caret)> change)
    {
        var cur = LineEditor.Text ?? "";
        int at = Math.Clamp(LineEditor.CursorPosition, 0, cur.Length);
        var (text, caret) = change(cur, at);
        LineEditor.Text = text;
        LineEditor.CursorPosition = Math.Clamp(caret, 0, text.Length);
        Canvas.EditingCursor = LineEditor.CursorPosition;
        Canvas.EnsureCaretVisible();
    }


    /// <summary>
    /// **要打字了，就把字号落到最近的整数**（用户提的折中，v0.96.139）。
    ///
    /// 起因（用户实测）：**整数号下光标正好落在字与字的格线上，非整数号下会压进字里**
    /// （「24 字号没问题，不是整数的却有问题」）。根因是平台**绘制**时对每个字形的推进量
    /// 取了整，而我们的尺子（`MeasureAdvances`）用的是排版报的小数 —— 两者只在
    /// 「字号 × 0.5 × 屏幕密度」落到整数上时重合。详见那个方法的注释。
    ///
    /// 用户给的解法比「去跟平台的取整较劲」干净得多：**缩放保持无极**（阅读/浏览时任意小数号，
    /// 这也是他明确要的），**只在进入编辑态时对齐到整数** —— 编辑时「光标落在格线上」是硬需求，
    /// 读书时不是。于是既不用限制缩放粒度，也不用改宽度模型。
    /// </summary>
    private void SnapFontSizeForEditing(bool toast = true)
    {
        float cur = EditorTypography.FontSize;
        float snapped = MathF.Round(cur);
        if (MathF.Abs(snapped - cur) < 0.01f) return;

        ApplyFontSize(snapped, persist: true, toast: false);
        // 明确告诉用户字号被对齐了 —— 否则「一点编辑文字就变大/变小」会像 bug。
        // 捏合路径传 false：那边紧接着会报「字号 N」，已经把结果说清楚了，别弹两条。
        if (toast) ShowToast($"字号已对齐到 {snapped:F0}（编辑时用整数号，光标才对得准）");
    }

    private void BeginEditLine(long oneBased, float xInLine = -1f)
    {
        if (_editable == null || _readOnly) return;

        // ⚠ 放在所有分支之前：已经在编辑这一行、只是挪光标时同样要对齐
        SnapFontSizeForEditing();

        if (_editLine == oneBased - 1 && LineEditor.IsVisible)
        {
            // 已经在编辑这一行：只把光标挪到点到的位置（不重建、不打断 IME）
            if (xInLine > 0)
            {
                int tapCol = ClickXToCharIndex(xInLine, LineEditor.Text ?? "");
                LineEditor.CursorPosition = Math.Clamp(tapCol, 0, (LineEditor.Text ?? "").Length);
                Canvas.EditingCursor = tapCol;       // 用我们算出的列，不回读平台值
                Canvas.EnsureCaretVisible();
            }
            LineEditor.Focus();
            return;
        }
        CommitEditingLine();

        _editLine = oneBased - 1;
        var text = _editable.GetLine(_editLine) ?? "";
        // 记下这一行的**原始内容**，供退出编辑时压撤销栈用（见 _editLineStart 的说明）。
        // ⚠ 只能在这一处赋值 —— 上面「已在编辑本行、只是挪光标」的早退分支不能重置它，
        //    否则挪一下光标就把「进入编辑态时是什么样」抹掉了。
        _editLineStart = text;

        _committing = true;
        // **原样带进去，不展开 tab**（原来这里 ExpandTabs 成空格）。
        // 展开的后果是「编辑一下，文件里的制表符就永久变成空格了」—— 用户明确要的是
        // **真制表符**：1 个字符、删一次删掉整格、显示宽度走 TabColumns（4 列）。
        // 展开这件事由**绘制侧**负责：`BuildLineRuns` 开头就是 ExpandTabs，所以 `	` 画出来
        // 仍是 4 列宽；`MeasurePrefixWidth` / `CharIndexAtX` 也都先展开再映射，光标与点击同源。
        LineEditor.Text = text;
        _committing = false;
#if ANDROID
        // **程序化写完输入框文本之后，必须清掉输入法因此留下的残留组合区**。
        // 不清的话它把自己当成「还在组合中」，此后把退格全吃在自己的缓冲里，
        // 应用层一条回调都收不到 —— 真机 logcat 实测：按退格时只有 `OnCreateInputConnection`
        // 一条日志，`DeleteSurroundingText` / `SendKeyEvent` 三条路一条都没触发。
        if (LineEditor.Handler?.PlatformView is BackspaceAwareEditText bae) bae.ClearStaleComposing();
#endif

        Canvas.EditingLine = oneBased;
        Canvas.EditingText = LineEditor.Text ?? "";

        // 把「点在哪一格」换算成字符下标。
        // 先把横坐标换成**视觉列**，再经 VisualColToSourceIndex 换成字符下标 ——
        // 后者认 CJK 占两列（中文行里直接按字符数算会偏出好几格）。
        int col = ClickXToCharIndex(xInLine, LineEditor.Text ?? "");
        LineEditor.CursorPosition = Math.Clamp(col, 0, (LineEditor.Text ?? "").Length);
        Canvas.EditingCursor = LineEditor.CursorPosition;
        Canvas.SetCaretLine(oneBased);
        // 自动配对的基线：**进编辑态时**取一次当前文本。不初始化的话，
        // 第一下按键会拿上一行的旧文本去比，长度差恰好为 1 时就会认出一个不存在的插入。
        _lastEditText = LineEditor.Text ?? "";
        UpdateBracketMatch();
        PositionEditor(oneBased);
        LineEditor.IsVisible = true;
        LineEditor.Focus();
        StartCaretSync();
#if WINDOWS
        // 上面这次同步 Focus 会被「点击自身的焦点处理」覆盖（见 OnLineEditorUnfocused 的注释），
        // 所以下一拍再要一次 —— 那一次才真正拿得到。
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(80), () =>
        {
            if (_editLine >= 0) EnsureKeySinkFocused();
        });
#endif
        // 把这一行带到可视区中部：软键盘占掉下半屏，贴着底部编辑会看不见自己在打什么，
        // 系统也可能为了「让焦点控件可见」而自行滚动页面（那会让画布坐标和实际显示错开）。
        EnsureEditorVisible(oneBased);
        Canvas.Invalidate();
        UpdateStatus();
    }

    private IDispatcherTimer? _caretSync;

    /// <summary>
    /// 光标位置同步：<c>Entry</c> 没有「光标移动」事件，**输入法**改光标（打完一个字往右挪、
    /// 组合态取消等）时自绘光标不会动。轻量轮询解决，且**只在值真的变了才重绘**，不会变成定时刷屏。
    ///
    /// ⚠ 手指点位置**不走这条路**（输入框压在画布底下、收不到触摸，见 EditorPage.xaml）：
    /// 点哪儿由画布自己算，算完直接写 <c>Canvas.EditingCursor</c>。这里只管输入法那一侧。
    /// </summary>
    private void StartCaretSync()
    {
        _caretSync ??= Dispatcher.CreateTimer();
        _caretSync.Interval = TimeSpan.FromMilliseconds(120);
        _caretSync.Tick -= SyncCaret;
        _caretSync.Tick += SyncCaret;
        _caretSync.Start();
    }

    private void SyncCaret(object? sender, EventArgs e)
    {
        if (_editLine < 0) { _caretSync?.Stop(); return; }
        int pos = LineEditor.CursorPosition;
        if (pos == Canvas.EditingCursor) return;   // 没变就不重绘
#if WINDOWS
        // 输入法/左右键把光标横向挪了 ⇒ 上下键的「目标列」作废，下次按上下要重新取当前列。
        // （我们自己在 MoveCaretVertical 里写光标时，EditingCursor 同步写过 ⇒ 走上面的早退，不会误清。）
        _verticalCol = -1;
#endif
        Canvas.EditingCursor = pos;
        Canvas.EnsureCaretVisible();               // 长行时把光标带进视野
    }

#if WINDOWS
    /// <summary>上下键跨行时保留的目标列（字符下标）。-1 = 还没开始上下移动。</summary>
    private int _verticalCol = -1;

    /// <summary>承载键盘的输入框平台视图（页面上唯一收得到按键的元素）。</summary>
    private Microsoft.UI.Xaml.Controls.TextBox? _winKeySink;

    private void OnKeySinkLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => EnsureKeySinkFocused();

    private static void OnKeySinkBringIntoView(
        Microsoft.UI.Xaml.UIElement sender, Microsoft.UI.Xaml.BringIntoViewRequestedEventArgs e)
        => e.Handled = true;   // 借来收键盘的代理不许把页面滚走

    private void OnLineEditorPreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Up:
                MoveCaretVertical(-1);
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Down:
                MoveCaretVertical(+1);
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.PageUp:
                MoveCaretVertical(-(int)Math.Max(1, Canvas.VisibleLines - 1));
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.PageDown:
                MoveCaretVertical(+(int)Math.Max(1, Canvas.VisibleLines - 1));
                e.Handled = true;
                break;
            // 左右/Home/End **只在只读态自己处理**：编辑态那根是平台的输入框光标，
            // 由 TextBox 自己管（SyncCaret 会把它的位置同步到画布），抢过来反而打断 IME。
            case Windows.System.VirtualKey.Left:
                if (_editLine < 0) { MoveBrowseCaret(-1); e.Handled = true; }
                break;
            case Windows.System.VirtualKey.Right:
                if (_editLine < 0) { MoveBrowseCaret(+1); e.Handled = true; }
                break;
            case Windows.System.VirtualKey.Home:
                if (_editLine < 0) { MoveBrowseCaret(0, toLineEdge: true); e.Handled = true; }
                break;
            case Windows.System.VirtualKey.End:
                if (_editLine < 0) { MoveBrowseCaret(0, toLineEnd: true); e.Handled = true; }
                break;
            // 行首退格 / 行尾 Delete 的**跨行合并**：平台的单行 TextBox 在边界上什么都不做
            // （光标在第 0 列时文本不变 ⇒ TextChanged 不触发），只能自己接。
            // 与 Android 那条 InputConnection 路径指向同一对方法，行为一致。
            case Windows.System.VirtualKey.Back:
                if (_editLine >= 0)
                {
                    if (LineEditor.CursorPosition == 0 && JoinWithPreviousLine()) e.Handled = true;
                }
                else { e.Handled = true; ToastWhyNotEditable(); }
                break;
            case Windows.System.VirtualKey.Delete:
                if (_editLine >= 0)
                {
                    if (LineEditor.CursorPosition >= (LineEditor.Text ?? "").Length && JoinWithNextLine())
                        e.Handled = true;
                }
                else { e.Handled = true; ToastWhyNotEditable(); }
                break;
            // Tab **必须自己吃掉**：平台的 TextBox 拿它做焦点导航（AcceptsReturn=false 时 Tab
            // 移到下一个控件）—— 一按焦点就跑了，连打字都断。这里改成插入缩进。
            case Windows.System.VirtualKey.Tab:
                e.Handled = true;                     // 只读态只吞掉，不移焦点
                if (_editLine >= 0)
                {
                    if (IsShiftDown()) DedentLine();  // Shift+Tab = 退一级缩进
                    else InsertIndent();
                }
                break;
        }
    }

    /// <summary>
    /// 浏览态（没有在编辑任何一行）按退格/删除时，**把「没反应」变成「说清为什么没反应」**。
    ///
    /// 刻意**不做**「按一下退格就自动进编辑态」：那等于让退格键顺手把只读切成可写 ——
    /// 是用户没要求的模式切换，风险大于收益。这里只补一句提示，与 <c>OnEditClicked</c>
    /// 「按钮始终可点、只是说清原因」是同一条既有原则。
    /// </summary>
    private void ToastWhyNotEditable()
        => ShowToast(_canEdit ? "只读模式：点这支笔切到编辑" : "大文件以只读方式打开，不能修改");

    /// <summary>
    /// Shift 是否按下（判断 Shift+Tab）。
    ///
    /// 用 `InputKeyboardSource` 而不是看 `KeyRoutedEventArgs` —— 后者**不带修饰键信息**
    /// （只有 Key 与 KeyStatus），光看它分不出 Tab 与 Shift+Tab。
    /// </summary>
    private static bool IsShiftDown()
        => Microsoft.UI.Input.InputKeyboardSource
            .GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

    /// <summary>只读态浏览光标的列长度（该行字符数）。</summary>
    private int LineLength(long oneBased)
        => _doc?.GetLine(Math.Max(0, oneBased - 1))?.Length ?? 0;

    /// <summary>只读态的横向移动 / Home / End —— 移动那根浏览竖线（编辑态不走这里）。</summary>
    private void MoveBrowseCaret(int delta, bool toLineEdge = false, bool toLineEnd = false)
    {
        long line = Canvas.CaretLine;
        if (line < 1) line = 1;

        int len = LineLength(line);
        int col = toLineEdge ? 0 : toLineEnd ? len : Math.Clamp(Canvas.CaretCol + delta, 0, len);
        Canvas.SetCaretLine(line, col);
        _verticalCol = -1;   // 横向动过 ⇒ 上下键的目标列重新取
        UpdateStatus();
    }

    /// <summary>
    /// 上下键跨行移动光标（Windows）。
    ///
    /// **两种模式都要管**：编辑态挪自绘的编辑光标（`BeginEditLine` 换行 + `EditingCursor` 定位），
    /// 只读态挪浏览光标（`Canvas.SetCaretLine`）。只读态**不能**照抄编辑态那条路 ——
    /// `BeginEditLine` 在 `_readOnly` 时第一行就 return，表现就是「按了没反应」。
    ///
    /// 列位用 <see cref="_verticalCol"/> 记住**开始上下移动时的那一列**：走过一行短行时它只被
    /// 夹着用、不改写 —— 否则「长行 → 短行 → 再往下」会退化成短行的列，这是桌面编辑器的通行语义。
    /// </summary>
    private void MoveCaretVertical(int delta)
    {
        var doc = _doc;
        if (doc == null || doc.LineCount == 0 || delta == 0) return;

        long from = _editLine >= 0 ? _editLine + 1 : Canvas.CaretLine;
        if (from < 1) from = 1;

        if (_verticalCol < 0)
            _verticalCol = _editLine >= 0
                ? Math.Clamp(LineEditor.CursorPosition, 0, (LineEditor.Text ?? "").Length)
                : Canvas.CaretCol;   // 只读态从浏览光标当前列接着走（不是恒从 0 开始）

        long target = Math.Clamp(from + delta, 1L, doc.LineCount);
        if (target == from) return;

        if (_editLine >= 0)
        {
            BeginEditLine(target);                      // 会先提交当前行，再在新行上开编辑
            var t = LineEditor.Text ?? "";
            int col = Math.Clamp(_verticalCol, 0, t.Length);
            LineEditor.CursorPosition = col;
            Canvas.EditingCursor = col;
        }
        else
        {
            // 只读态：换行 + 目标列（短行上夹住，但 **_verticalCol 不改写** —— 这样
            // 「长行 → 短行 → 再走回长行」能回到原来那一列，是桌面编辑器的通行语义）
            Canvas.SetCaretLine(target, Math.Clamp(_verticalCol, 0, LineLength(target)));
        }

        Canvas.ScrollToLine(target);
        Canvas.EnsureCaretVisible();
        UpdateStatus();   // 状态栏那行「光标 L?:」要跟着键盘走，否则它一直停在旧行
    }
#endif

    /// <summary>
    /// 编辑期间保证该行可见。
    ///
    /// **不需要在这里等软键盘** —— 键盘把画布压矮的那一刻由 <c>CodeCanvasView.OnSizeAllocated</c>
    /// 兜住：视口一变矮它就把光标行顶回视口内。这里只管「键盘已经在屏幕上」的那种情况
    /// （点了另一条本来就露着的行 → 一个字都不动）。
    ///
    /// 此前这里是「等 260ms 再滚一次」：那是在猜键盘动画的时长，猜早了算的还是旧视口
    /// （等于没做），猜晚了用户已经看着自己被挡住 —— 实测就是「刚好弹出键盘时挡住光标」。
    /// 触发点换成真实的高度变化之后，这条就没有存在理由了。
    /// </summary>
    private void EnsureEditorVisible(long oneBased)
        => Canvas.ScrollToLine(oneBased);   // 最小滚动：露得全就一个字都不动

    /// <summary>
    /// 点击的横坐标（pt）→ 该行内的字符下标。
    /// 两步：像素 → 视觉列 → 字符下标（后者认 CJK 占两列，中文行里按字符数直算会偏出几格）。
    /// </summary>
    private int ClickXToCharIndex(float xInLine, string line)
        => Math.Clamp(Canvas.CharIndexAtX(line, xInLine), 0, line.Length);

    /// <summary>
    /// 把输入框对齐到该行位置（用同一份行高与行号栏宽度算，避免错位）。
    ///
    /// ⚠ 横向要**和画布正文的起点逐项对齐**：`行号栏 + 正文左内边距 − 横向滚动`
    /// （见 <c>CodeCanvasView</c> 里 `textX` 的算法）。只写「行号栏宽度」是不够的 ——
    /// 横向一滚，输入框的文字原点就与画布差出整个滚动量，而**系统自己的光标、选择手柄、
    /// 复制/粘贴浮层都是按输入框内部坐标定位的**（我们只把它的文字和光标画成透明的），
    /// 于是它们全都跟着偏 —— 这正是「选择/复制粘贴位置不对」的来源。
    /// </summary>
    private void PositionEditor(long oneBased)
    {
        // **输入框钉死在顶部，不跟任何东西走。**
        //
        // 它只是个「换出软键盘」的代理：全透明、1px 高、自带光标已关掉（见 HidePlatformCaret）。
        // 既然自带光标一概不用、内容也全透明，那它浮在屏幕哪个位置就都不影响观感 ——
        // 而**跟着文字走反而有害**：它的原生光标会时不时冒出来（用户实测红色光标乱跳），
        // 而且每帧挪它要更新原生控件布局，长列表滚动会掉帧。钉死就都没了。
        LineEditor.TranslationY = 0;
        LineEditor.Margin = new Thickness(0);
    }

    /// <summary>
    /// 输入框当前的文本 —— **全角转半角归一化之后的**。
    ///
    /// ⚠ **凡是「把输入框内容写进模型」的地方都读它，不要再直接读 `LineEditor.Text`。**
    /// 这条纪律为的是「同一规则两处实现」那个老坑：归一化若只在打字路径生效、粘贴或提交时漏掉，
    /// 就会出现「打着打着转过去了、一切换行又变回全角」这种一半生效的怪状。
    /// 下面那四个写回点（TextChanged / CommitEditingLine / 回车 / 粘贴）**共用这一个访问器**，
    /// 将来出现第五个读点也只能走它。
    ///
    /// **为什么可以只写模型与画布、不回写 `LineEditor.Text`**（<c>OnLineEditorTextChanged</c>
    /// 的硬约束要求不得回写）：那层输入框是全透明、1px 高、文字与光标都不显示的 ——
    /// 用户看到的每一个字都来自画布自绘；而全角→半角是**严格 1 字符换 1 字符**（长度不变），
    /// 所以 `LineEditor.CursorPosition` 的下标在任何时候都对得上模型与画布。
    /// </summary>
    private string CurrentEditedText()
    {
        var raw = LineEditor.Text ?? "";
        if (raw.Length == 0) return raw;
        if (!MauiEditorStore.FullWidthToHalf) return raw;
        if (!IsSourceFile()) return raw;

        // 短路：绝大多数按键（英文代码、退格、方向键）这一行里压根没有全角字符，
        // 这一趟扫描比 tokenize 便宜得多。
        if (!FullWidthText.MayContainCandidate(raw)) return raw;

        var syntax = Canvas.SyntaxForFile;
        if (syntax == null) return FullWidthText.Normalize(raw);

        var spans = syntax.ProtectedSpans(raw);

        // 保守兜底：受保护区间占了大半行 ⇒ 这多半是「整行中文注释 / 整行字符串」，
        // 而逐行 tokenize 判不出跨行的块注释与三引号字符串（见 ProtectedSpans 的局限说明）。
        // 宁可这一行不转，也不要在注释正文里乱改标点。
        int covered = 0;
        foreach (var (_, len) in spans) covered += len;
        if (covered * 2 >= raw.Length) return raw;

        var keep = new bool[raw.Length];
        foreach (var (start, len) in spans)
            for (int i = start; i < start + len && i < keep.Length; i++) keep[i] = true;

        return FullWidthText.Normalize(raw, keep);
    }

    /// <summary>
    /// 这个文件算不算**源码** —— 两条规则都问它：① 编辑时要不要做全角转半角；
    /// ② 保存时要不要套用用户设的编码/换行。**一份判据、两个消费者**。
    ///
    /// 判据分两层，**不另列扩展名表**：
    /// ① Markdown 是散文，全角标点在散文里本来就是对的 ⇒ 不是源码；
    /// ② 语法定义是「纯文本」的（.txt/.log/认不出的扩展名）默认也不是 —— 但 **VML 能编译**的
    ///    例外（.lua/.pas/.bas/.r/.d/.f90 这些没有专用语法定义，<c>ForFile</c> 会落到纯文本，
    ///    可它们是正经源码）⇒ 这一层**问编译器**，而不是问语法表。
    /// </summary>
    private bool IsSourceFile()
    {
        if (Canvas.SyntaxForFile is not { } s) return false;          // 无文档（_doc == null）
        if (s.Name == Syntax.MarkdownName) return false;
        if (s.Name == Syntax.PlainName) return MauiVml.CanCompile(_relPath);
        return true;
    }

    private void CommitEditingLine()
    {
        if (_editLine < 0 || _editable == null) return;
        var newText = CurrentEditedText();
        // 旧值取进入编辑态时记下的那一份，**不是**从模型读 —— 模型早被逐键改写过了
        // （详见 _editLineStart 的说明：从模型读会让 oldText == newText 恒成立，撤销失效）
        var oldText = _editLineStart;
        long line = _editLine;
        _editLine = -1;
        Canvas.EditingLine = -1;
        Canvas.EditingText = null;
        LineEditor.IsVisible = false;
        // 离开编辑态 ⇒ 自动配对的基线作废（下次进编辑态会重新取）
        _lastEditText = null;
        // 光标不在任何一行里了，配对高亮跟着收掉（否则它会留在屏幕上指着上次那两个括号）
        Canvas.SetBracketMatch(null, null);
        _caretSync?.Stop();

        if (newText == oldText) { Canvas.Invalidate(); return; }

        _editable.ReplaceRange(line, 1, [newText]);
        _history.Push(new EditOp(line, [oldText], [newText], 0, newText.Length, Environment.TickCount64));
        _modified = true;
        Canvas.InvalidateLine(line + 1);
        UpdateStatus();
    }

    private void OnLineEditorTextChanged(object? sender, TextChangedEventArgs e)
    {
        // ⚠ 这里**只读** Entry 的文本、只写模型；绝不回写 Text/CursorPosition/SelectionLength ——
        // 那会打断 IME 的组合态（拼音还没上屏时 TextChanged 已经触发了）。
        if (_committing || _editLine < 0 || _editable == null) return;

        // 走 CurrentEditedText() 而**不是** e.NewTextValue —— 全角→半角在这里生效，
        // 且与另外三个写回点共用同一个入口。归一化不动 `\n`，所以下面那个多行粘贴的
        // 判断结果与原来一致，只是拆出来的每一段都已经是半角了。
        var text = CurrentEditedText();

        // 一动手就把上一轮编译留下的诊断清掉 —— 那些行号多半已经偏了，
        // 继续画在代码上等于给用户看假信息（没有诊断时是一次极便宜的早退）。
        ClearDiagnosticsOnEdit();

        // 光标可能刚贴上一个括号（也可能刚离开）—— 配对高亮跟着走。
        // 不在括号旁边时这个函数是**常数级早退**，所以放在每次按键的路径上也不心疼。
        UpdateBracketMatch();

        // 多行粘贴：Entry 是单行控件，各平台对含换行的粘贴处理不一致（替换成空格 / 截断），
        // 自己拆更可靠 —— 首行留在当前行，其余插入到下面。
        if (text.Contains('\n') || text.Contains('\r'))
        {
            var parts = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            long line = _editLine;
            // 旧值走 _editLineStart —— 原来写成「替换之后再 GetLine」，取回来的是**新内容**，
            // 撤销一次会把新内容又写一遍 = 等于撤销不了（多行粘贴这条路的原始 bug）
            var oldLine = _editLineStart;
            _editable.ReplaceRange(line, 1, parts);
            _history.Push(new EditOp(line, [oldLine], parts, 0, 0, Environment.TickCount64));
            _modified = true;
            Canvas.InvalidateAll();
            LineEditor.IsVisible = false;
            _editLine = -1;
            Canvas.EditingLine = -1;
            Canvas.EditingText = null;
            Canvas.ScrollToLine(line + parts.Length);
            UpdateStatus();
            return;
        }

        _editable.ReplaceRange(_editLine, 1, [text]);
        _modified = true;
        Canvas.EditingText = text;                       // 画布据此自绘这一行
        Canvas.EditingCursor = LineEditor.CursorPosition; // 以及光标位置
        Canvas.EnsureCaretVisible();                     // 长行时把光标带进视野
        Canvas.InvalidateLine(_editLine + 1);
        UpdateStatus();

        MaybeAutoPair(text);
    }

    /// <summary>
    /// 上一次见到的编辑行文本（用来认出"刚刚敲进去的是哪一个字符"）。
    /// 每次进编辑态与每次文本变化都要更新，否则中途换行会拿旧值去比、认出一个假的插入。
    /// </summary>
    private string? _lastEditText;

    /// <summary>
    /// **自动配对**：敲了 `(` `[` `{` 就补上右半边、光标停在中间；敲的正好是已经在那儿的那半个
    /// 右括号时**跨过去**（否则会变成 `())`）。
    ///
    /// ## 两条硬约束（都踩过或差点踩）
    ///
    /// ① **绝不在这条回调栈里回写 `LineEditor.Text`** —— 那会打断 IME 的组合态
    ///   （拼音还没上屏时 `TextChanged` 已经触发了，回写等于把半成品拍死）。
    ///   所以真正的写入走 `Dispatcher.Dispatch` 推到下一拍 —— 与 `AssistInsertAsync`
    ///   那条"从点击处理器里改文本"的路同一个模式。
    /// ② **判断"刚插进去的是哪一个字符"要能证伪**：`text.Remove(col-1, 1) == 上一次的文本`
    ///   才算数。只比长度（长了 1）会在**粘贴、自动更正、输入法整词上屏**这些情形下认错，
    ///   然后我们就会往一段不是我们插入的文本里再塞一个字符。行太长时直接放弃（那条比较是 O(n)）。
    ///
    /// ⚠ 真机上只用 ASCII 验过（`adb shell input text` 发不了中文）——
    /// 中文输入法的组合态下会不会误判，需要人工敲一次中文确认。
    /// </summary>
    private void MaybeAutoPair(string text)
    {
        var prev = _lastEditText;
        _lastEditText = text;

        if (prev == null || _committing || _readOnly) return;
        if (text.Length != prev.Length + 1) return;              // 只处理"多了一个字符"
        if (LineEditor.SelectionLength != 0) return;            // 有选区时输入是**替换**，不是插入
        if (text.Length > MaxAutoPairLine) return;              // 超长行不比对了（Remove+比较是 O(n)）

        int col = Math.Clamp(LineEditor.CursorPosition, 0, text.Length);
        if (col < 1) return;
        if (text.Remove(col - 1, 1) != prev) return;            // 插的不在光标左边那一位 ⇒ 不认

        var inserted = text[col - 1];
        var before = col >= 2 ? text[col - 2] : '\0';
        var after = col < text.Length ? text[col] : '\0';

        // ① 补右半边：在光标处**插入**，不删任何东西
        if (TextEditAssist.AutoCloseFor(inserted, before, after) is { } closer)
        {
            DeferLineEdit(at: col, removeCount: 0, insert: closer.ToString(), caret: col);
            return;
        }

        // ② 跨过已存在的那半个（否则 `(` 自动补成 `()` 之后，用户再敲 `)` 会得到 `())`）：
        //    把光标处那一个删掉，就等于"跨过去了"
        if (after == inserted && TextEditAssist.BracketPairs.ContainsKey(inserted))
            DeferLineEdit(at: col, removeCount: 1, insert: "", caret: col);
    }

    /// <summary>自动配对的行长上限 —— 超过就不做（每次按键都要比一次全文，长行上不划算）。</summary>
    private const int MaxAutoPairLine = 2000;

    /// <summary>
    /// 把编辑行里 `[at, at+removeCount)` 这一段换成 <paramref name="insert"/>，并把光标放到
    /// <paramref name="caret"/>。
    ///
    /// 自动配对的两个动作都是它的特例：补右半边 = 在光标处插入（removeCount 0）；
    /// 跨过已存在的右半边 = 把光标那一个删掉（insert 空串）。
    ///
    /// **推到下一拍再写**（见 `MaybeAutoPair` 的第 ① 条约束）：`_committing` 是那次程序化写入的
    /// 重入闸门 —— `OnLineEditorTextChanged` 见到它就早退，否则我们自己那次写入会再触发一轮配对。
    /// </summary>
    private void DeferLineEdit(int at, int removeCount, string insert, int caret)
    {
        Dispatcher.Dispatch(() =>
        {
            if (_editLine < 0 || _committing || _editable == null) return;
            var cur = LineEditor.Text ?? "";
            if (at < 0 || at > cur.Length || at + removeCount > cur.Length) return;

            var next = cur.Remove(at, removeCount).Insert(at, insert);
            _committing = true;
            try
            {
                LineEditor.Text = next;
                var pos = Math.Clamp(caret, 0, next.Length);
                LineEditor.CursorPosition = pos;
                Canvas.EditingCursor = pos;
                _lastEditText = next;      // 这次程序化改动不该被当成"用户敲了一个字符"
            }
            finally { _committing = false; }
            UpdateBracketMatch();
        });
    }

    private void OnLineEditorCompleted(object? sender, EventArgs e)
    {
        if (_editLine < 0 || _editable == null) return;

        // Enter：**在光标处把这一行劈成两半**（左半留在原行、右半落到新行），编辑器移到新行。
        //
        // ⚠ 原来这里是无条件「下面插一个空行」，于是**光标跑到下一行、整行文字却一个字没动**
        // （用户实测报的就是这个：「输入 one two，光标移到中间回车，光标下去了、单词还在同一行」）。
        // 真正的编辑器回车是断行，不是「另起一行」。
        var text = CurrentEditedText();
        long line = _editLine;
        var oldLine = _editLineStart;      // 旧值不能在替换之后取
        int at = Math.Clamp(LineEditor.CursorPosition, 0, text.Length);
        var left = text[..at];
        var right = text[at..];

        // **自动缩进**：新行继承左半段的前导空白；左半段以开括号收尾时再多一级。
        // 没有它的话，写嵌套代码每按一次回车都要自己敲一遍空格 —— 这是"手感像不像 IDE"
        // 最直接的一条。**顺带把右半段的前导空白去掉**：那多半是光标后面的分隔空格，
        // 留着就等于在新行的缩进上又叠了一层（用户看到的是"越缩越深"）。
        var indent = TextEditAssist.IndentForNewLine(left, MauiEditorStore.TabColumns);
        right = indent + right.TrimStart(' ', '\t');

        // 一次「一行 → 两行」：撤销是一步（OldLines/NewLines 都不是单行 ⇒ CanMerge 天然为假）
        _editable.ReplaceRange(line, 1, [left, right]);
        _history.Push(new EditOp(line, [oldLine], [left, right], at, 0, Environment.TickCount64));

        _modified = true;
        _editLine = -1;
        Canvas.EditingLine = -1;
        Canvas.EditingText = null;
        LineEditor.IsVisible = false;
        Canvas.InvalidateAll();
        BeginEditLine(line + 2);           // 1-based：新行是 (line+1)，即第 line+2 行
        // 光标落在**缩进之后**（即新行内容的第一列）—— 这才是「在这里断行」的语义：
        // 用户接下来敲的字应该紧跟着缩进，而不是被顶到行首、还得自己再挪一次。
        // 不显式设的话 BeginEditLine 会按默认的 xInLine=-1 去猜一个列。
        LineEditor.CursorPosition = indent.Length;
        Canvas.EditingCursor = indent.Length;
        Canvas.EnsureCaretVisible();
    }

    // ── 配对括号高亮 ──

    /// <summary>配对扫描的字符上限 —— 超出就放弃（宁可不标，也不要为了一个高亮把输入卡住）。</summary>
    private const int MaxBracketScan = 20000;

    /// <summary>
    /// 光标贴着一个括号时，把**与它配对的那一个**交给画布高亮；否则清掉。
    ///
    /// 规则本身（认哪个括号、怎么跨行找配对）在 `TextEditAssist` 里 —— 那是纯逻辑，
    /// 放那边才有自测（这个类在 MAUI 工程里，桌面自测碰不到）。这里只做两件这里才有的事：
    /// ① **取实时文本**：编辑中那一行还没提交，`_editable` 里存的可能是旧的；
    /// ② 把结果喂给画布。
    /// </summary>
    private void UpdateBracketMatch()
    {
        if (_editable == null || _editLine < 0)
        {
            Canvas.SetBracketMatch(null, null);
            return;
        }

        var text = CurrentEditedText();
        int col = Math.Clamp(LineEditor.CursorPosition, 0, text.Length);
        if (TextEditAssist.BracketAtCaret(text, col) is not { } b)
        {
            Canvas.SetBracketMatch(null, null);
            return;
        }

        var editable = _editable;
        // 编辑中那行的实时文本与 `_editable` 里存的可能不同（还没提交），必须取实时那份
        string? LineAt(long i) => i == _editLine ? text : editable.GetLine(i);

        var match = TextEditAssist.MatchBracket(
            LineAt, editable.LineCount, _editLine, b.Col, b.Open, b.Close, b.Forward, MaxBracketScan);

        if (match is { } m) Canvas.SetBracketMatch((_editLine, b.Col), m);
        else Canvas.SetBracketMatch(null, null);
    }

    private void OnLineEditorUnfocused(object? sender, FocusEventArgs e)
    {
#if WINDOWS
        // 【Windows 真根因】点画布时，平台把焦点给页面里的 ScrollViewer —— 而且这步发生在
        // 我们那次 `Focus()` **之后**（点击自身的焦点处理晚于回调）。于是输入框立刻失焦，
        // 若在这里就地提交，编辑态会被自己拆掉：`LineEditor.IsVisible` 被置回 false（平台侧变
        // Collapsed）、`_editLine=-1`，**此后所有按键都落空**（实测日志：真实焦点=ScrollViewer、
        // entryIsVisible=False、按键一条不进）。
        // 所以延后一拍再提交：这一拍里焦点若回到输入框（BeginEditLine 那边会再要一次），就撤销提交。
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(150), () =>
        {
            if (_editLine >= 0 && _winKeySink?.FocusState == Microsoft.UI.Xaml.FocusState.Unfocused)
                CommitEditingLine();
        });
#else
        CommitEditingLine();
#endif
    }

    // ── 查找 / 大纲 / 预览 ──

    private async void OnFindClicked(object? sender, EventArgs e)
    {
        CommitEditingLine();
        if (_doc == null) return;

        var query = await DisplayPromptAsync("查找", "输入要查找的文本", accept: "查找", cancel: "取消", maxLength: 200);
        if (string.IsNullOrWhiteSpace(query)) return;

        long start = Math.Max(0, Canvas.CaretLine < 0 ? 0 : Canvas.CaretLine);
        ShowToast("查找中…", 1500);

        // 后台流式扫描：大文件上对全文 IndexOf 会把 UI 线程锁死
        var hit = await Task.Run(() => FindLine(query, start));
        if (hit < 0)
        {
            await DisplayAlertAsync("查找", $"未找到「{query}」", "关闭");
            return;
        }
        Canvas.ScrollToLine(hit + 1, center: true);
        Canvas.SetCaretLine(hit + 1);
        UpdateStatus();
    }

    /// <summary>
    /// 查找替换。
    ///
    /// 交互走**对话框串**（手机上没有"查找栏"那一行的地方，这套与既有的「查找」保持一致）：
    /// 问查找内容 → 问替换为 → 选「替换全部 / 只替换下一个」。
    ///
    /// ⚠ **一次替换全部 = 一步撤销**：所有改动合成**一个** `EditOp`（行区间替换天生支持），
    /// 否则用户改错一次要按几十下撤销 —— 那是"不敢用"的典型。
    /// ⚠ 与「查找」一样，替换**只作用于可编辑的文档**（`_editable`）：大文件是只读的，
    /// 没有 `_editable`，直接告诉用户而不是静默什么都不做。
    /// </summary>
    private async void OnReplaceClicked(object? sender, EventArgs e)
    {
        CommitEditingLine();
        if (_doc == null) return;
        if (_editable == null)
        {
            await DisplayAlertAsync("替换", "这个大文件是只读打开的，不能替换。", "关闭");
            return;
        }

        var query = await DisplayPromptAsync("替换", "查找内容", accept: "下一步", cancel: "取消", maxLength: 200);
        if (string.IsNullOrWhiteSpace(query)) return;
        var replacement = await DisplayPromptAsync("替换", $"把「{query}」替换为", accept: "下一步", cancel: "取消", maxLength: 200);
        if (replacement == null) return;          // 取消（空串是合法输入 = 删除）

        var choice = await DisplayActionSheet("替换", "取消", null, "替换全部", "只替换下一个");
        if (choice == "替换全部") ReplaceAll(query, replacement);
        else if (choice == "只替换下一个") ReplaceNext(query, replacement);
    }

    /// <summary>替换从光标处开始遇到的第一处（与「查找」同一套定位：逐行扫、绕回开头）。</summary>
    private void ReplaceNext(string query, string replacement)
    {
        if (_editable == null) return;

        long start = Math.Max(0, Canvas.CaretLine < 0 ? 0 : Canvas.CaretLine);
        var hit = FindLine(query, start);
        if (hit < 0)
        {
            ShowToast($"未找到「{query}」", 2000);
            return;
        }

        var line = _editable.GetLine(hit) ?? "";
        var col = line.IndexOf(query, StringComparison.Ordinal);
        if (col < 0) return;                       // FindLine 刚找到的，理论上不会

        var newLine = line[..col] + replacement + line[(col + query.Length)..];
        _editable.ReplaceRange(hit, 1, [newLine]);
        _history.Push(new EditOp(hit, [line], [newLine], col, col + replacement.Length, Environment.TickCount64));
        _modified = true;

        Canvas.InvalidateAll();
        Canvas.ScrollToLine(hit + 1, center: true);
        Canvas.SetCaretLine(hit + 1);
        UpdateStatus();
        ShowToast($"已替换 1 处", 1500);
    }

    /// <summary>
    /// 全文替换（**只改真正含关键词的那些行**，其余整段原样搬回去）。
    ///
    /// 全部改动压成**一个** `EditOp`：`ReplaceRange` 本来就是"行区间替换"，
    /// 首尾之间的行一并写回即可 —— 中间没变的行写回原值，语义与"只改那几行"完全一致，
    /// 但撤销只有一步。⛔ 别改成"每行一个 EditOp"，那样一次替换全部会压进去几十上百步。
    /// </summary>
    private void ReplaceAll(string query, string replacement)
    {
        if (_editable == null) return;

        long count = _doc?.LineCount ?? 0;
        long first = -1, last = -1;
        var hits = 0;

        // 先把首尾范围找出来（只读一遍），再整段取出/写回 —— 避免边扫边改导致的索引漂移
        for (long i = 0; i < count; i++)
        {
            var line = _editable.GetLine(i) ?? _doc?.GetLine(i);
            if (line == null || !line.Contains(query, StringComparison.Ordinal)) continue;
            if (first < 0) first = i;
            last = i;
            hits++;
            if (hits > MaxReplaceLines)
            {
                ShowToast($"匹配太多（超过 {MaxReplaceLines} 行），请缩小范围", 3000);
                return;
            }
        }
        if (first < 0) { ShowToast($"未找到「{query}」", 2000); return; }

        var span = (int)(last - first + 1);
        var oldLines = _editable.Snapshot(first, span);
        var newLines = new string[span];
        var changed = 0;
        for (var i = 0; i < span; i++)
        {
            var l = oldLines[i];
            if (!l.Contains(query, StringComparison.Ordinal)) { newLines[i] = l; continue; }
            newLines[i] = l.Replace(query, replacement, StringComparison.Ordinal);
            changed++;
        }

        _editable.ReplaceRange(first, span, newLines);
        _history.Push(new EditOp(first, oldLines, newLines, 0, 0, Environment.TickCount64));
        _modified = true;
        Canvas.InvalidateAll();
        UpdateStatus();
        ShowToast($"已替换 {changed} 行", 2000);
    }

    /// <summary>替换全部的行数上限 —— 超大范围一把改掉既慢又难回退，超过就请用户缩小范围。</summary>
    private const int MaxReplaceLines = 2000;

    /// <summary>后台逐行查找（从 <paramref name="fromLine"/> 开始，绕回开头；带上限防大文件卡死）。</summary>
    private long FindLine(string query, long fromLine)
    {
        if (_doc == null) return -1;
        long count = _doc.LineCount;

        long Search(long from, long to)
        {
            for (long i = from; i < to; i++)
            {
                var line = _doc.GetLine(i);
                if (line == null)
                {
                    _doc.PrefetchAsync(i, Math.Min(i + 500, count - 1)).GetAwaiter().GetResult();
                    line = _doc.GetLine(i);
                }
                if (line != null && line.Contains(query, StringComparison.Ordinal)) return i;
            }
            return -1;
        }

        var hit = Search(fromLine, count);
        return hit >= 0 ? hit : Search(0, Math.Min(fromLine, count));
    }

    private async void OnOutlineClicked(object? sender, EventArgs e)
    {
        CommitEditingLine();
        if (_editable == null)
        {
            await DisplayAlertAsync("大纲", "大文件（只读模式）暂不提供大纲分析", "关闭");
            return;
        }

        List<EditorCore.OutlineItem> outline;
        try { outline = BuildCoreForOutline().ExtractOutline(); }
        catch (Exception ex) { await DisplayAlertAsync("大纲", ex.Message, "关闭"); return; }

        if (outline.Count == 0)
        {
            await DisplayAlertAsync("大纲", "当前文件没有可识别的符号", "关闭");
            return;
        }

        var names = outline.Select(o => $"{o.Icon} {o.Name}  (L{o.Line})").ToArray();
        var choice = await DisplayActionSheetAsync("大纲", "取消", null, names);
        if (string.IsNullOrEmpty(choice) || choice == "取消") return;
        int idx = Array.IndexOf(names, choice);
        if (idx < 0) return;

        Canvas.ScrollToLine(outline[idx].Line, center: true);
        Canvas.SetCaretLine(outline[idx].Line);
    }

    /// <summary>用编辑器当前内容构造 <see cref="EditorCore"/> 供大纲/符号分析（仅小文件可编辑时）。</summary>
    private EditorCore BuildCoreForOutline()
    {
        var core = new EditorCore();
        core.LoadFile(_fullPath.Length > 0 ? _fullPath : Path.GetFileName(_relPath));
        core.Lines.Clear();
        for (long i = 0; i < _editable!.LineCount; i++)
            core.Lines.Add(new StringBuilder(_editable.GetLine(i) ?? ""));
        if (core.Lines.Count == 0) core.Lines.Add(new StringBuilder());
        return core;
    }

    // ══════════════════════════════════════════════════════════════════
    // 「预览 / 运行」那一格 —— 按文件类型变形，三处共用一份判据
    // ══════════════════════════════════════════════════════════════════

    private enum EditorAction { None, Preview, Run }

    /// <summary>
    /// 「这一格按钮现在是什么」—— **唯一判据**。工具栏那一格、☰ 菜单项、全屏浮层按钮
    /// 三处都调它。各写一份的后果很具体：迟早出现「工具栏点得动、全屏点不动」
    /// 或者「工具栏是 ▶、菜单里还是 👁」这种自己跟自己不一致的状态。
    /// </summary>
    private EditorAction ActionForCurrentFile()
    {
        if (_relPath.Length == 0) return EditorAction.None;
        if (IsMarkdown(_relPath)) return EditorAction.Preview;

        // 判据问 DetectVmlRole（→ MauiVml.CanCompile → 上游 22 个编译器的注册表），
        // 再问 RoleCanRun —— 与文件页那条「VML 运行」**完全同源**，这边不重列一遍角色。
        // 隐藏这一格的两种情况：None 的文件（.txt/.json/未知扩展名），
        // 以及**头文件**（它在链上，但不是完整的翻译单元、跑不了；用户定的"只能打开编辑"）
        // —— 点了没用的按钮比没有更让人困惑。
        return SandboxFsService.RoleCanRun(SandboxFsService.DetectVmlRole(_relPath))
            ? EditorAction.Run : EditorAction.None;
    }

    /// <summary>Markdown 判定 —— 抽出来是因为预览那处也要用，别在两边各写一遍后缀比较。</summary>
    private static bool IsMarkdown(string path)
        => path.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 把「这一格按钮」的全部外观收口到一处（图标 + 提示语 + 可见性）。
    /// 工具栏与全屏浮层是**同一个人格** —— 全屏里那个按钮的语义与工具栏上完全一致。
    /// </summary>
    private void ApplyActionButton()
    {
        var kind = ActionForCurrentFile();
        var (icon, hint) = kind switch
        {
            EditorAction.Preview => ("icon_preview.png", "Markdown 预览"),
            EditorAction.Run => ("icon_run.png", "运行"),
            _ => ("", ""),
        };
        bool show = kind != EditorAction.None;

        PreviewBtn.IsVisible = show;
        if (show)
        {
            PreviewBtn.Source = icon;
            SemanticProperties.SetHint(PreviewBtn, hint);
        }

        FullscreenActionBtn.IsVisible = _fullscreen && show;
        // 分隔线跟着「运行」那格走：非源码文件时那格是隐藏的，留着一条孤零零的竖线看着像坏了。
        // 同步只放在这里 —— 这个函数本来就是「那一格长什么样」的唯一出口（工具栏与全屏浮层共用）。
        FullscreenDivider.IsVisible = FullscreenActionBtn.IsVisible;
        if (FullscreenActionBtn.IsVisible)
        {
            FullscreenActionBtn.Source = icon;
            SemanticProperties.SetHint(FullscreenActionBtn, hint);
        }
    }

    /// <summary>工具栏那一格 / 全屏浮层那一格点下去 —— 按文件类型分流，两个入口共用。</summary>
    private async void OnActionButtonClicked(object? sender, EventArgs e)
    {
        if (ActionForCurrentFile() == EditorAction.Preview)
        {
            OnPreviewClicked(this, EventArgs.Empty);
            return;
        }
        await RunCurrentFileAsync();
    }

    /// <summary>
    /// 跑这个文件 —— **在编辑器里就地跑，底部面板出结果，不跳到命令行页**。
    ///
    /// 为什么不另搭一套：绘图窗口（`DrawWindowPage`）是 Shell 路由、由 `MauiBootstrap` 打开，
    /// `VmlUiCalls` 也不引用 `ShellPage` —— 也就是说**宿主能力全都是进程级的**，谁发起运行都一样。
    /// 而执行路径用的是同一个 `MauiVml.Run`（命令行页那条），不是第二份实现。
    /// 编辑器这条只是先编译一遍（为了拿诊断画气泡），成功时把编好的产物直接喂给它，**不重复编译**
    /// （手机上编译一次一分多钟）。
    /// </summary>
    private async Task RunCurrentFileAsync()
    {
        CommitEditingLine();
        if (_fullPath.Length == 0) return;

        // **一次只跑一件事**。原来没有这道闸门，而流式输出把代价放大了：连点两次「运行」，
        // 第二次会把 `MauiVml.OnOutputChunk` 这个**静态槽**指向新的流，第一次那趟的输出于是
        // 投给一个已经换掉的缓冲（画面互相踩）。编译同样占着这个位置（`_compileCts` 非空）。
        if (_runActive || _compileCts != null)
        {
            ShowToast("正在运行/编译中，先停止再试");
            return;
        }

        // 有未保存改动 → 先落盘（用户确认的行为：自动保存 + 提示）。
        // 不保存就跑的必然是**旧代码**，那种「改了没生效」比多等一会儿难查得多。
        if (_modified)
        {
            if (!await WriteBackAsync()) return;   // 存失败 WriteBackAsync 已经弹过原因
            ShowToast("已保存，正在运行…");
        }

        ShowPanel(PanelTab.Output);
        ClearPanelOutput();
        AppendOutput($"▶ {_relPath}");

        string? prebuilt = null;
        var role = SandboxFsService.DetectVmlRole(_relPath);

        // 只有「前端能编译的源文件 + 内容在内存里（可编辑）」才在编辑器里先编一遍。
        // 大文件只读与 .vml/.vmb 不编 —— 前者改都改不了，后者本就是汇编/字节码，直接跑。
        if (role == SandboxFsService.VmlRole.Compilable && _editable != null)
        {
            var (vmlText, diags, error) = await CompileInEditorAsync();

            if (vmlText == null)
            {
                // 失败：诊断**就地显示**（气泡、行下波浪线、错误列表读的是同一份数据）
                try { DiagnosticManager.Inject(_relPath, diags); } catch { }
                Canvas.InvalidateAll();
                RefreshErrorList();
                UpdateStatus();
                SwitchPanelTab(PanelTab.Errors);
                AppendOutput(error ?? "编译失败");
                return;
            }

            // 成功也要注入**这份列表** —— 编译通过不等于没问题：未使用符号这类**警告**
            // 正是「编得过但有问题」，它就在 `diags` 里（见 `MauiVml.BuildProgram` 的
            // `compileWarnings`）。这里曾写死 `Inject(_relPath, [])`，于是「只有错误有效、
            // 警告不显示」—— 有错时走上面那支注入真表、只有警告时走这支把表清空。
            // ⚠ 不能写成 `if (diags.Count > 0)`：空表必须照注，否则「上次有警告、这次没了」
            //   会清不掉（旧气泡留在屏幕上）。
            try { DiagnosticManager.Inject(_relPath, diags); } catch { }
            // 用 `InvalidateAll`（与失败那支一致）而不是 `Invalidate`：诊断层其实两者都够
            // （波浪线与气泡在 `Draw` 里每帧从 `SnapshotDiagnostics()` 现取，不在行缓存里，
            //   见 CodeCanvasView 的 1371 / 1392 两行），但这里顺带把行排版缓存也丢掉 ——
            // 一次编译只发生一次，代价可忽略，而「少失效一样东西」正是本仓反复踩的坑。
            Canvas.InvalidateAll();
            RefreshErrorList();
            UpdateStatus();
            prebuilt = vmlText;
        }

        await RunInEditorAsync(prebuilt);
    }

    // 运行超时（秒）现在**可配置**：设置页 →「虚拟机」→ 编辑器运行超时（`MauiVmStore.EditorTimeoutSec`，
    // 默认 120）。口径的说明留在这里：
    //   · 与命令行页同一个量级 —— 崩掉或死循环的程序不该把编辑器永久钉住；
    //   · ⚠ **这条超时是"连续执行"的，不是墙钟**（`VmRuntime.ResetTimeout`：等消息 / 等弹框 /
    //     等用户输入都会续期）。所以现在它只在"程序真的连跑 N 秒一次都没等过"时才生效 ——
    //     从前那版按墙钟算，玩家在开场说明框上读两分钟就把超时耗光了，必死。

    private CancellationTokenSource? _runCts;
    private volatile bool _runActive;
    private TaskCompletionSource<string>? _stdinTcs;

    private async Task RunInEditorAsync(string? prebuiltVml)
    {
        _runCts = new CancellationTokenSource();
        var cts = _runCts;
        _runActive = true;
        PanelStopBtn.IsVisible = true;
        SwitchPanelTab(PanelTab.Output);
        AppendOutput("（运行中…）");
        // ⚠ **必须在 `MauiVml.Run` 之前装钩子** —— `LastRunStreamed` 是"装没装"的判据，
        // 跑完再装等于没流式。装钩子这件事同时决定了 `MauiVml` 会不会把输出边跑边交出来。
        StartPanelStream();

        try
        {
            // 编好的产物直接喂 `MauiVml.Run(source:)`（命令行页的 `vml test` 走的就是这条），
            // 省掉第二次前端编译；没有产物（.vml/.vmb/大文件）就按路径派发。
            //
            // `markup: true` = **按命令行页那一档来**：裸 ANSI 翻成 `«»` 标记、stderr 整段套红、
            // 控制字符（`\r`/`\t`/`\b`）先解释掉、全屏程序给成网格而不是折行往下堆。
            // 默认的 `false` 是给 AI 那条路的（它要的是"最终结果那一整段纯文本"）。
            var output = await Task.Run(() =>
                prebuiltVml is { Length: > 0 }
                    ? MauiVml.Run(prebuiltVml, null, MauiVmStore.EditorTimeoutSec, ReadLineFromProgram, cts.Token, markup: true)
                    : MauiVml.Run(null, _fullPath, MauiVmStore.EditorTimeoutSec, ReadLineFromProgram, cts.Token, markup: true));

            if (MauiVml.LastRunStreamed)
            {
                // 正文**已经边跑边交出去了**，这里只补两件事：
                //   ① 排空队列 + `Finish()`（末尾没换行的那一截全靠它）；
                //   ② 补**诊断尾巴**（运行时报错 / 被强制停止 / syscall 被拒）—— 它是跑完才知道的，
                //      而"程序崩了、用户只看到没有输出"正是这个平台反复修过的故障。
                // ⚠ `output` 在这里**必须丢掉**：它是全量，再放一遍就是整段重复
                //   （命令行页同款，注释也写着这一条）。
                FinishPanelStream();
                if (MauiVml.LastDiagnostics.Length > 0)
                    AppendOutputMarkup(MauiVml.LastDiagnostics);
            }
            else
            {
                // 兜底那条路（钩子没装上时）：整段呈现 —— 全屏程序连光标一起接。
                SetPanelOutput(output.TrimEnd());
            }
            AppendOutput("（已结束）");
        }
        catch (OperationCanceledException)
        {
            AppendOutput("⏹ 已停止。");
        }
        catch (Exception ex)
        {
            // 与命令行页同一条理由：异常不接住就跑进 Task.Run，变成「未观察的任务异常」，
            // 屏幕上一个字都没有 —— 用户看到的就是「点了运行，然后什么都没发生」。
            ErrorLog.Error("EditorPage", "运行失败", ex);
            AppendOutput($"⚠️ 运行失败：{ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            StopPanelStream();              // 幂等：流式那支已经收过尾，这里兜"抛异常提前退出"
            _runActive = false;
            _runCts = null;
            cts.Dispose();
            PanelStopBtn.IsVisible = false;
            PanelStdinRow.IsVisible = false;
            _stdinTcs?.TrySetResult("");    // 万一还卡在等输入，放它走
            _stdinTcs = null;
        }
    }

    /// <summary>
    /// 程序读 stdin 时被 VM 线程调用（**它会阻塞在这个方法里**）。与命令行页的
    /// <c>ReadLineFromProgram</c> 同一语义：亮出输入行、等用户敲、再放行。
    /// </summary>
    private string ReadLineFromProgram()
    {
        if (!_runActive) return "";     // 非交互轮次（不该发生）给空行，绝不死等

        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _stdinTcs = tcs;
            PanelStdinRow.IsVisible = true;
            PanelStdinEntry.Text = "";
            SwitchPanelTab(PanelTab.Output);
            PanelStdinEntry.Focus();
        });
        return tcs.Task.GetAwaiter().GetResult();
    }

    private void OnPanelStdinSubmitted(object? sender, EventArgs e) => SubmitStdin();

    private void OnPanelStdinSendClicked(object? sender, EventArgs e) => SubmitStdin();

    private void SubmitStdin()
    {
        var tcs = _stdinTcs;
        if (tcs == null) return;
        _stdinTcs = null;
        PanelStdinRow.IsVisible = false;
        tcs.TrySetResult(PanelStdinEntry.Text ?? "");
    }

    private void OnPanelStopClicked(object? sender, EventArgs e)
    {
        // **编译与运行共用一个停止键** —— 编译在手机上要一分多钟，比运行更需要这个出口。
        try { _compileCts?.Cancel(); } catch { }
        try { _runCts?.Cancel(); } catch { }
        // **卡在等输入时也要能停下**：只取消 token 是不够的 —— VM 线程正阻塞在
        // ReadLineFromProgram 里，它看不到 token。先把它放走，它下一拍就会观察到取消。
        _stdinTcs?.TrySetResult("");
    }

    // ── 运行面板（底部小窗：错误列表 / 输出）──

    private enum PanelTab { Errors, Output }

    private void ShowPanel(PanelTab tab)
    {
        OutputPanel.IsVisible = true;
        ApplyPanelHeight();
        SwitchPanelTab(tab);
        // 气泡**不为面板让位**（规格：不做任何避让 —— 位置只由错误行决定）。
        // 面板的 ZIndex 比画布高，本来就压在气泡上面。
    }

    // ── 面板高度可拖（拖面板最上沿那条） ──────────────────────────────────────
    //
    // 用户要的：「需要一个地方拖动改变占用高度，现在高度固定，有些大屏幕我可以拖动占比多一些」。
    //
    // 三条约束：
    //   · **往上拖 = 变高**（面板贴底，`VerticalOptions="End"`）⇒ 高度 = 起点 − TotalY；
    //   · 拖动**过程中只改 `HeightRequest`**，松手才落盘一次 —— 写盘是原子替换，
    //     跟手每帧写一次纯属浪费；
    //   · 上限按**当屏**算（`RootGrid` 的 80%），因为存的是绝对高度：换屏/转屏之后
    //     原来那个值可能已经超过新屏的可用高度，得重新夹一次。
    private double _panelDragStartH;

    /// <summary>
    /// 这一次手势是不是**已经在处理中**。
    ///
    /// <para><b>为什么需要它（v0.96.430，用户报「来回抖动」）</b>：MAUI 的手势被父容器
    /// 或别的识别器打断时会走 `Canceled` 然后**重新发一次 `Started`**。若每次都重取基准，
    /// 而此刻面板高度**已经被这一次拖动改过了**，就等于把"已经拖走的距离"重新算成 0 ——
    /// 高度于是跳回起点再跟着手指走，来回之间正是那个抖动。基准只认**整段手势的第一下**。</para>
    /// </summary>
    private bool _panelDragging;

    /// <summary>
    /// 面板可用高度的**上限**（留两成给编辑器 —— 拖满就没法看代码了）。
    ///
    /// <para>⚠ 取的是**面板所在容器**的高度，不是整页：面板挂在内容区那一行里，
    /// 用整页高算上限会让"拖到 80%"超出内容区、被容器裁掉一截（用户报的
    /// **「拖动位置也有错位」**里就有这一份）。</para>
    /// </summary>
    private double MaxPanelHeight
    {
        get
        {
            double h = OutputPanel.Parent is VisualElement p ? p.Height : 0;
            if (h <= 0) h = RootGrid.Height;
            // ⚠ 布局还没跑完时 `Height` 是 -1：退回**屏幕高度**再算，别拿 -1 去 clamp
            //   （那会把上限算成 MinPanelHeight，面板一进去就被压到最矮）。
            if (h <= 0)
            {
                var d = DeviceDisplay.MainDisplayInfo;
                h = d.Density > 0 ? d.Height / d.Density : MauiEditorStore.DefaultPanelHeight * 3;
            }
            return Math.Max(MauiEditorStore.MinPanelHeight, h * 0.8);
        }
    }

    private double ClampPanelHeight(double h)
        => Math.Clamp(h, MauiEditorStore.MinPanelHeight, MaxPanelHeight);

    /// <summary>把存下来的高度应用到面板（每次显示面板时调 —— 顺带按当屏重新夹一次）。</summary>
    private void ApplyPanelHeight()
        => OutputPanel.HeightRequest = ClampPanelHeight(MauiEditorStore.PanelHeight);

    private void OnPanelDragPanUpdated(object sender, PanUpdatedEventArgs e)
    {
#if ANDROID
        // Android 走原生触摸（见 `HookPanelDragBar`）—— 这里直接让位。
        // 一句话理由：MAUI 的 `TotalY` 是**相对视图**的坐标，而面板一变高、拖动条自己就在上移
        // ⇒ 反馈回路，量级恒偏小且来回抖（实测手指移 152dp、`TotalY` 只报 74.7dp）。
        return;
#endif
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                // 整段手势只认第一下 —— 见 `_panelDragging` 的说明。
                if (_panelDragging) return;
                _panelDragging = true;
                // ⚠ **基准必须是"实际高度" `Height`，不是"请求高度" `HeightRequest`** ——
                //   这一条实测踩过：`HeightRequest` 只是我们**请求**的值，面板内容有自己的
                //   最小高度（Tab 行 + 终端网格），实际渲染出来会**比请求值大**
                //   （实测请求 220dp、实际 323dp）。拿请求值当基准，等于一开始就少算了那
                //   103dp 的差，整段拖动**恒差这么多** ⇒ 手感是「手指和面板边缘对不上」
                //   （用户报的**「拖动位置也有错位」**）。
                //   用 `Height` 则「手指移多少 dp，面板就变多少 dp」，边缘**贴着手指走**。
                //   （时延问题另由下面的 `_panelDragging` 解决 —— 重复 `Started` 才是抖动的真因。）
                _panelDragStartH = OutputPanel.Height > 0
                    ? OutputPanel.Height
                    : ClampPanelHeight(MauiEditorStore.PanelHeight);
                break;

            case GestureStatus.Running:
                if (!_panelDragging) return;
                OutputPanel.HeightRequest = ClampPanelHeight(_panelDragStartH - e.TotalY);
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (!_panelDragging) return;
                _panelDragging = false;
                // 松手落盘。存的是**当屏**夹过的值；`SetPanelHeight` 自己还会再夹一次
                // （那一道是防"存进去一个连自己都认不得的数"）。
                MauiEditorStore.SetPanelHeight(OutputPanel.HeightRequest);
                break;
        }
    }

#if ANDROID
    // ── Android：拖动条走**原生触摸**，用屏幕绝对坐标 ──────────────────────────
    //
    // **为什么不能用 MAUI 的 `PanGestureRecognizer`**：它给的 `TotalY` 是**相对视图**的
    // 坐标变化，而这段交互恰恰是"用位移去改这个视图自己的高度" —— 面板一变高，拖动条
    // （连同整个面板）就往上升。设手指上移 `ΔF`、拖动条上移 `ΔH`（都取上移为正）：
    //
    //     相对坐标的变化  TotalY = ΔF − ΔH      （手指远离了视图，但视图也迎上来了）
    //     而我们想要的     ΔH = ΔF
    //     代进去           ΔF = ΔF − ΔH  ⇒  **只有 ΔH ≡ 0 才成立**
    //
    // 即「用相对坐标驱动自身尺寸」是个**会自己抵消自己的回路** —— 实测手指移 152dp、
    // `TotalY` 只报 74.7dp（约一半，正是被 ΔH 抵消掉的那部分），而且中途来回抖
    // （`-38.5 → -32.7`）。用户报的「**来回抖动** + **位置错位**」两条都是它。
    //
    // **正解：脱离视图坐标系**，用 `MotionEvent.RawY`（屏幕绝对坐标）—— 手指的绝对位移
    // 与视图怎么动无关 ⇒ 回路断开，`ΔH = ΔF` 严格成立。
    private float _dragRawStartY;
    private double _dragRawStartH;

    private void HookPanelDragBar()
    {
        if (PanelDragBar.Handler?.PlatformView is not Android.Views.View v) return;
        v.Touch -= OnPanelDragBarTouch;   // Handler 可能重建，防重复挂
        v.Touch += OnPanelDragBarTouch;
    }

    private void OnPanelDragBarTouch(object sender, Android.Views.View.TouchEventArgs e)
    {
        var ev = e.Event;
        if (ev == null) return;
        switch (ev.ActionMasked)
        {
            case Android.Views.MotionEventActions.Down:
                _dragRawStartY = ev.RawY;
                _dragRawStartH = OutputPanel.Height > 0
                    ? OutputPanel.Height
                    : ClampPanelHeight(MauiEditorStore.PanelHeight);
                e.Handled = true;
                break;

            case Android.Views.MotionEventActions.Move:
                // `RawY` 是**屏幕像素**，而 `HeightRequest` 是**设备无关单位** ⇒ 除密度换算。
                double movedPx = _dragRawStartY - ev.RawY;      // 上移为正
                double density = DeviceDisplay.MainDisplayInfo.Density;
                double movedDp = density > 0 ? movedPx / density : movedPx;
                OutputPanel.HeightRequest = ClampPanelHeight(_dragRawStartH + movedDp);
                e.Handled = true;
                break;

            case Android.Views.MotionEventActions.Up:
            case Android.Views.MotionEventActions.Cancel:
                MauiEditorStore.SetPanelHeight(OutputPanel.HeightRequest);
                e.Handled = true;
                break;
        }
    }
#endif

    /// <summary>
    /// 面板 Tab 的「选中 / 未选中」文字色。**必须跟随主题**。
    ///
    /// 这两行原来写死 `Colors.White` 与 `#999999`，恰好把 XAML 里挂的 `AppThemeBinding` 盖掉：
    /// 日间主题下面板底色是近白 ⇒ **选中的那一格变成白底白字、整条看不见**
    /// （用户实测报的正是这个）。注意它只影响**当前选中**的那一格 ——
    /// 未选中的 `#999999` 在白底上还算看得见，所以现象是「时有时无」，很容易被当成偶发。
    /// </summary>
    private static Color PanelTabActive => IsDarkTheme ? Colors.White : Color.FromArgb("#1A1A1A");
    private static Color PanelTabIdle => IsDarkTheme ? Color.FromArgb("#999999") : Color.FromArgb("#6B6B6B");

    /// <summary>当前选中的面板 Tab —— 换主题时要用它把选中态重刷一遍（见 <see cref="OnAppThemeChanged"/>）。</summary>
    private PanelTab _panelTab = PanelTab.Errors;

    private void SwitchPanelTab(PanelTab tab)
    {
        _panelTab = tab;
        bool errs = tab == PanelTab.Errors;
        PanelErrorsScroll.IsVisible = errs;
        PanelOutputArea.IsVisible = !errs;
        PanelTabErrors.TextColor = errs ? PanelTabActive : PanelTabIdle;
        PanelTabOutput.TextColor = errs ? PanelTabIdle : PanelTabActive;
        // 输出区在隐藏期间**不被布局**（`Width` 停在 0 ⇒ 折行列数算成 0）⇒ 刚亮出来时
        // 要按现在的宽度重折一次。不重折的话，第一次运行看到的是"整行横向滚"而不是折行，
        // 得等下一次 `SizeChanged` 才对 —— 而那一次不一定会来。
        if (!errs) RefoldPanelOutput();
    }

    private void OnPanelTabErrorsClicked(object? sender, EventArgs e) => SwitchPanelTab(PanelTab.Errors);

    private void OnPanelTabOutputClicked(object? sender, EventArgs e) => SwitchPanelTab(PanelTab.Output);

    private void OnPanelCloseClicked(object? sender, EventArgs e)
        => OutputPanel.IsVisible = false;

    // ═══ 面板输出：**自绘命令行网格**（`TerminalGrid`）═══
    //
    // 这一块与命令行页的 `ShellPage` 是**兄弟实现**：同一块画布、同一套规则
    // （`ShellStream` 判"这一块是追加还是整屏替换"、`ShellWrap` 折行、`AnsiMarkup` 把裸 ANSI
    // 翻成 `«»` 标记）。规则**一处都没在这里重写** —— 这里只有"缓冲与呈现"这层簿记，
    // 而簿记的写法照 `ShellPage` 抄（各写一套的结果是两边观感不一样，本仓头号坑）。
    //
    // 与命令行页**有意**不同的只有三点，都是"面板本来就没有那个概念"：
    //   · 没有尺寸档（固定 80×25 / 横向固定 / 自适应那一套）⇒ 列数一律按面板实际宽度算；
    //   · 没有回滚行数的设置项 ⇒ 用 `PanelScrollback` 常量兜底；
    //   · 全屏程序的网格尺寸沿用宿主的 `MauiVml.TermRows/TermCols`（与命令行页同一个来源），
    //     面板不另立一个 —— 否则同一段输出在两个页面会被判成不同形状的画面。

    /// <summary>面板缓冲：markup 文本 + 「这一行折不折」—— 与 `ShellPage._lines` 同形。</summary>
    private readonly List<(string Text, bool NoWrap)> _panelLines = [];

    /// <summary>上一段停在**半行**上（没以 `\n` 收尾）—— 下一段要接上去而不是另起一行。</summary>
    private bool _panelPartial;

    /// <summary>
    /// 面板网格字号 —— 捏合可改（会话内有效，**不落盘**：那是"临时看清这一屏"的取景动作，
    /// 与编辑器正文字号各管各的）。
    ///
    /// 为什么面板需要它：面板只有两百来 dp 高、几十 dp 宽，而老程序写死 80 列 ——
    /// 不缩小就只能横向滚着看半行。
    /// </summary>
    private double _panelFont = MauiShellStore.DefaultFont;

    /// <summary>上次折行用的列数（面板宽度一变就得重折，`SizeChanged` 里比对）。</summary>
    private int _panelCols = -1;

    /// <summary>回滚上限（行）—— 长跑程序（日志刷屏那种）不至于把缓冲吃爆。</summary>
    private const int PanelScrollback = 2000;

    /// <summary>竖直滚动条压在内容上的宽度（`TerminalGrid.BarThickness + BarInset`）。</summary>
    private const double PanelBarChrome = 8;

    /* ── 全屏程序的光标 ──
       与命令行页同一套：记"最近一块画面"的光标。`_panelCursorBase` = 那一块的第一行在
       `_panelLines` 里的下标（-1 = 当前没有画面）；到 `RenderPanelOutput` 里再翻译成**显示行号**
       （折行会挪行号，只有一处换算才不会错位）。 */
    private int _panelCursorBase = -1;
    private int _panelCursorRow;
    private int _panelCursorCol;
    private bool _panelCursorVisible;
    private int _panelCursorDisplay = -1;

    /// <summary>清空面板输出（每次运行 / 编译开始时调）。</summary>
    private void ClearPanelOutput()
    {
        _panelLines.Clear();
        _panelPartial = false;
        _panelCursorBase = -1;
        _panelCursorDisplay = -1;
        RenderPanelOutput();
    }

    /// <summary>
    /// **一行自己的话**（进度、提示、错误尾巴）—— 不是程序输出，所以按普通文本转标记。
    /// 编译进度那几行走的就是这里。
    ///
    /// ⚠ 两处与"程序输出"不同的处理，都是**必须**的：
    ///   · **自己补一个换行**：这些调用给的都是"一整句话"（`（运行中…）`），不带 `\n`。
    ///     不补的话下一句会被当成**半行的延续**接到它后面（`（运行中…）（已结束）`）。
    ///   · **先把半行收尾**：程序末尾那半行（`printf` 没换行就崩了）还挂着 `_panelPartial`，
    ///     不收拾就轮到我们的提示被粘在它屁股后面。
    /// </summary>
    private void AppendOutput(string text)
    {
        ClosePendingLine();
        AppendPanelText(text + "\n", noWrap: false, alreadyMarkup: false);
    }

    /// <summary>同上，但内容**已经是 markup**（`MauiVml.LastDiagnostics` 就是）。</summary>
    private void AppendOutputMarkup(string markup)
    {
        ClosePendingLine();
        AppendPanelText(markup + "\n", noWrap: false, alreadyMarkup: true);
    }

    /// <summary>给"没以换行收尾的那半行"补一个结尾（没有半行时什么都不做）。</summary>
    private void ClosePendingLine()
    {
        // 空串追加到已存在的最后一行 = 只是把 `_panelPartial` 摘掉，**不新增行**
        if (_panelPartial) AddPanelSegment("", partial: false, noWrap: false);
    }

    /// <summary>
    /// **整段替换**（运行结束时把完整输出放进来）。
    ///
    /// ⚠ 全屏程序要连**光标基准**一起记：`LastCursor` 报的是画面内的行列，基准行号必须在
    ///   放内容**之前**取（放在之后取会偏一整块）—— 这一条命令行页那边踩过，注释也写着。
    /// </summary>
    private void SetPanelOutput(string markupText)
    {
        _panelLines.Clear();
        _panelPartial = false;
        bool grid = MauiVml.LastOutputWasGrid;
        _panelCursorBase = grid ? 0 : -1;
        if (grid) (_panelCursorRow, _panelCursorCol, _panelCursorVisible) = MauiVml.LastCursor;
        AppendPanelText(markupText, noWrap: grid, alreadyMarkup: true);
        RenderPanelOutput();     // 空输出（程序一个字都没打）也要走一遍，把清空落到屏上
    }

    /// <summary>追加一段文本（按 `\n` 切段，末尾没换行的那截算半行）。</summary>
    private void AppendPanelText(string text, bool noWrap, bool alreadyMarkup)
    {
        if (text.Length == 0) return;
        if (!alreadyMarkup) text = AnsiMarkup.ToMarkup(text);

        int start = 0;
        while (start < text.Length)
        {
            int nl = text.IndexOf('\n', start);
            if (nl < 0)
            {
                AddPanelSegment(text[start..], partial: true, noWrap: noWrap);
                break;
            }
            AddPanelSegment(text[start..nl], partial: false, noWrap: noWrap);
            start = nl + 1;
        }

        TrimPanelScrollback();
        RenderPanelOutput();
    }

    private void AddPanelSegment(string segment, bool partial, bool noWrap)
    {
        if (_panelPartial && _panelLines.Count > 0)
        {
            var last = _panelLines[^1];
            _panelLines[^1] = (last.Text + segment, last.NoWrap || noWrap);
        }
        else _panelLines.Add((segment, noWrap));
        _panelPartial = partial;
    }

    /// <summary>面板实际能放几列（宽度没落定前返回 0 = 不折，交给画布横向滚）。</summary>
    private int PanelCols()
    {
        double w = PanelOutputGrid.Width - PanelBarChrome;
        return w <= 0 ? 0 : Term.ShellWrap.ColumnsForWidth(w, _panelFont);
    }

    /// <summary>宽度变了要重折（折行是**呈现**这一层的事，缓冲里存的始终是逻辑行）。</summary>
    private void RefoldPanelOutput()
    {
        if (PanelCols() != _panelCols) RenderPanelOutput();
    }

    /// <summary>
    /// 把缓冲呈现到画布上 —— **唯一的呈现出口**（折行、光标换算、字号都只在这里落一次）。
    /// </summary>
    private void RenderPanelOutput()
    {
        int cols = PanelCols();
        _panelCols = cols;

        var display = new List<string>(_panelLines.Count);
        _panelCursorDisplay = -1;

        if (cols <= 0)
        {
            // 宽度还没落定：原样放进去（画布自己横向滚），光标也不画 —— 此刻"画面"还没有形状
            foreach (var (text, _) in _panelLines) display.Add(text);
        }
        else
        {
            int cursorBuf = _panelCursorBase >= 0 ? _panelCursorBase + _panelCursorRow : -1;
            for (int i = 0; i < _panelLines.Count; i++)
            {
                var (line, noWrap) = _panelLines[i];
                if (i == cursorBuf) _panelCursorDisplay = display.Count;
                // ⚠ **画面行不折**（`ShellWrap.IsPictureLine`）：全屏程序的每一行就是屏幕上的一行，
                //   折一下整幅画就斜切了。标志优先（全屏输出那一整块），次之才是逐行的判据。
                if (noWrap || Term.ShellWrap.IsPictureLine(line)) display.Add(line);
                else display.AddRange(Term.ShellWrap.WrapMarkup(line, cols));
            }
        }

        PanelOutputGrid.SetLines(display, _panelFont, IsDarkTheme);
        PanelOutputGrid.SetCursor(_panelCursorDisplay, _panelCursorCol, _panelCursorVisible);
    }

    private void TrimPanelScrollback()
    {
        int over = _panelLines.Count - PanelScrollback;
        if (over > 0) DropPanelHead(over);
    }

    /// <summary>
    /// 从**头部**丢掉若干行 —— 面板缓冲唯一的裁剪出口。
    ///
    /// ⚠ 收成一处的理由与命令行页同款：丢头会让所有"按行号记着位置"的东西一起前移，
    ///   眼下是**画面光标**。两处各写一句 `RemoveRange` 的话，加了光标之后就得两处都记住要减，
    ///   漏一处的症状是"光标画到别的行上去"，只在大输出之后才复现。
    /// </summary>
    private void DropPanelHead(int count)
    {
        if (count <= 0) return;
        _panelLines.RemoveRange(0, count);
        if (_panelCursorBase >= 0)
        {
            _panelCursorBase -= count;
            if (_panelCursorBase + _panelCursorRow < 0) _panelCursorBase = -1;   // 连光标那行都丢了
        }
    }

    // ── 流式：**边跑边画**（同一块画布，规则在 `ShellStream`）──
    //
    // 为什么面板也要流式：`top`/`sl`/`cmatrix` 那一整类**不退出就一直画**。整段缓冲的话，
    // 屏幕上要等到程序结束（或超时）才出现东西 —— 而"在编辑器里跑自己写的游戏"正是面板的用途。
    // 命令行页那两个版本（v0.96.351/352）解决的就是这件事，这里走的是**同一个** `ShellStream`。

    private Term.ShellStream? _stream;

    /// <summary>当前画面块的行数（整屏替换时按它 `RemoveRange`）。</summary>
    private int _panelGridRows;

    private readonly object _chunkGate = new();
    private readonly List<string> _chunkQueue = [];
    private bool _chunkScheduled;

    /// <summary>本页装上去的那个钩子（`StopPanelStream` 靠它判断"现在挂的还是不是我的"）。</summary>
    private readonly Action<string> _panelChunkHook;

    private void StartPanelStream()
    {
        // 网格尺寸**取自宿主那对静态量**（命令行页在播报它们）——与"整段"那条路
        // （`RunProgram` 里建 `FrameBuffer` 的同一对值）同源，两条路不会给出不同形状的画面。
        _stream = new Term.ShellStream(MauiVml.TermRows, MauiVml.TermCols);
        _panelGridRows = 0;
        lock (_chunkGate) { _chunkQueue.Clear(); _chunkScheduled = false; }
        MauiVml.OnOutputChunk = _panelChunkHook;
    }

    /// <summary>
    /// 摘钩子。⚠ **每次运行收尾都必须调** —— 留着的话下一次运行的输出会被投给一个已经结束的流
    /// （而且没人排空队列），表现是"这次跑的输出跑到上次那块去了"。
    ///
    /// ⚠ 摘之前**先确认挂着的还是自己那个**。`MauiVml.OnOutputChunk` 是**单个静态槽**，
    ///   命令行页也在用（两页各自的"正在运行"闸门只挡得住自己那一页，挡不住对方）——
    ///   无条件清掉的话，本页收尾会把命令行页正在用的钩子一并摘掉，那边表现为"画面停住不再刷新"。
    ///   （同一条纪律也适用于 `MauiVml.OnProgress`，那里是"谁用谁负责摘"。）
    /// </summary>
    private void StopPanelStream()
    {
        if (MauiVml.OnOutputChunk == _panelChunkHook) MauiVml.OnOutputChunk = null;
        _stream = null;
        _panelGridRows = 0;
        lock (_chunkGate) { _chunkQueue.Clear(); _chunkScheduled = false; }
    }

    /// <summary>
    /// 收尾。⚠ **先同步排空队列**再 `Finish()`：队列里的块是靠 `Dispatcher.Dispatch` 排上来的，
    /// `await` 恢复之后它们**不保证**已经跑过 —— 直接 `Finish` 会把它们排到收尾提示后面去。
    /// </summary>
    private void FinishPanelStream()
    {
        ApplyPanelChunks();
        if (_stream != null) ApplyPanelStreamRender(_stream.Finish());
        StopPanelStream();
    }

    /// <summary>
    /// VM 线程上的回调（`MauiVml.OnOutputChunk`）。**只入队 + 排一次 UI 更新**，
    /// 绝不在这个线程上碰 `_panelLines`（画布是 UI 线程的东西）。
    ///
    /// `_chunkScheduled` 是那道闸门：全屏程序每秒能写几十次，每次都切线程会把 UI 拖死。
    /// </summary>
    private void OnPanelChunk(string chunk)
    {
        lock (_chunkGate)
        {
            _chunkQueue.Add(chunk);
            if (_chunkScheduled) return;
            _chunkScheduled = true;
        }
        Dispatcher.Dispatch(ApplyPanelChunks);
    }

    private void ApplyPanelChunks()
    {
        string[] chunks;
        lock (_chunkGate)
        {
            if (_chunkQueue.Count == 0) { _chunkScheduled = false; return; }
            chunks = [.. _chunkQueue];
            _chunkQueue.Clear();
            _chunkScheduled = false;   // 先放开闸门：处理期间新来的块要能再排一次
        }
        if (_stream == null) return;
        foreach (var c in chunks) ApplyPanelStreamRender(_stream.Feed(c));
    }

    /// <summary>
    /// 把一次呈现请求落到 `_panelLines` 上 —— **全线唯一决定"追加还是替换"的地方**
    /// （追加与替换混了会把画面搅烂：网格是一整屏，追加就是几十屏残影）。
    /// </summary>
    private void ApplyPanelStreamRender(Term.ShellRender? r)
    {
        if (r == null) return;

        if (!r.IsGrid)
        {
            _panelCursorBase = -1;      // 线性输出没有"画面光标"，上一块的就此作废
            _panelGridRows = 0;
            AppendPanelText(r.Text, noWrap: false, alreadyMarkup: true);
            return;
        }

        // ── 网格：**替换那一块**（这就是"实时"与"整段缓冲"最大的区别）──
        var rows = r.Text.Length == 0 ? [] : r.Text.Split('\n');
        if (_panelCursorBase < 0)
        {
            _panelCursorBase = _panelLines.Count;
            _panelGridRows = 0;
        }
        if (_panelGridRows > 0 && _panelCursorBase + _panelGridRows <= _panelLines.Count)
            _panelLines.RemoveRange(_panelCursorBase, _panelGridRows);
        foreach (var line in rows) _panelLines.Add((line, true));
        _panelGridRows = rows.Length;

        (_panelCursorRow, _panelCursorCol, _panelCursorVisible) = (r.CursorRow, r.CursorCol, r.CursorVisible);
        TrimPanelScrollback();
        RenderPanelOutput();
    }

    /// <summary>
    /// 重建错误列表。数据源与气泡、行下波浪线**是同一份**（`DiagnosticManager`）——
    /// 三处各建一个数据源的话，迟早出现「气泡说 1 个错、列表说 2 个」。
    /// 点一条跳到那一行。
    /// </summary>
    private void RefreshErrorList()
    {
        PanelErrorsList.Clear();
        var diags = DiagnosticsForThisFile();

        if (diags.Count == 0)
        {
            PanelTabErrors.Text = "错误列表";
            PanelErrorsList.Add(new Label
            {
                Text = "没有错误。",
                FontSize = 12,
                TextColor = PanelTabIdle,     // 与未选中的 Tab 同一个弱化色，跟随主题
            });
            return;
        }

        PanelTabErrors.Text = $"错误列表 ({diags.Count})";
        foreach (var d in diags.OrderBy(x => x.Line).ThenBy(x => x.Column))
        {
            var (mark, color) = d.Severity switch
            {
                Severity.Error => ("❌", EditorTypography.ErrorWave),
                Severity.Warning => ("⚠", EditorTypography.WarnWave),
                _ => ("ℹ", EditorTypography.InfoWave),
            };
            var where = d.Line > 0 ? $"第 {d.Line} 行" : "（无位置）";
            var code = d.Code is { Length: > 0 } c ? $"  [{c}]" : "";

            var row = new Label
            {
                Text = $"{mark} {where}：{d.Message}{code}",
                FontSize = 12,
                TextColor = color,
                LineBreakMode = LineBreakMode.WordWrap,
            };

            long line = d.Line;   // 闭包捕获局部副本（别捕获循环变量）
            if (line > 0)
            {
                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) =>
                {
                    Canvas.SetCaretLine(line);
                    Canvas.ScrollToLine(line, center: true);
                    Canvas.InvalidateAll();
                    UpdateStatus();
                };
                row.GestureRecognizers.Add(tap);
            }
            PanelErrorsList.Add(row);
        }
    }

    private CancellationTokenSource? _compileCts;

    /// <summary>
    /// 在编辑器里编一次（为了拿到产物与诊断）。**复用打开文件那个遮罩**，不新建第二套
    /// 「等待中」界面；进度钩子也用既有的 <see cref="MauiVml.OnProgress"/>（与命令行页同一机制）。
    /// </summary>
    private async Task<(string? Vml, List<Diagnostic> Diags, string? Error)> CompileInEditorAsync()
    {
        _compileCts = new CancellationTokenSource();
        var cts = _compileCts;

        // **编译期间锁编辑、但保留滚动**（用户要求）：编译在手机上要一分多钟，
        // 这期间改了代码，结果回来时气泡与错误列表指向的就是**已经不对的行号**。
        // 保留滚动是有意的 —— 等的时候还能翻代码看，只是不能改。
        bool wasReadOnly = _readOnly;
        SetReadOnly(true);

        // **不走全屏遮罩**（用户反馈：为了显示一行字闪一整屏，体验不好）。
        // 现在有底部面板了，进度就滚在「输出」里 —— 代码始终看得见，还能边等边看。
        PanelStopBtn.IsVisible = true;        // 编译也给了停止入口（原来只有运行有，而编译更慢）
        // 先自己写一行：`OnProgress` 最早也要等解压标准库那一步才开口，而那之前可能已经过去几秒，
        // 屏幕上什么都不动会让人以为没点上。
        AppendOutput("⏳ 正在编译…（手机上要一两分钟）");
        // ⚠ `MauiVml.OnProgress` 是**单个静态槽**：命令行页也在用同一个。现实中两者互斥
        //   （一个在编辑器、一个在命令行），且 ShellPage 那边有 _busy 守卫。
        MauiVml.OnProgress = msg => MainThread.BeginInvokeOnMainThread(() => AppendOutput(msg));

        try
        {
            return await Task.Run(() => MauiVml.CompileForEditor(_fullPath, cts.Token));
        }
        catch (OperationCanceledException)
        {
            return (null, [], "⏹ 编译已停止。");
        }
        catch (Exception ex)
        {
            ErrorLog.Error("EditorPage", "编译失败", ex);
            return (null, [], $"⚠️ 编译失败：{ex.Message}");
        }
        finally
        {
            MauiVml.OnProgress = null;        // 静态钩子必须摘（同 ShellPage 那边的注释）
            _compileCts = null;
            cts.Dispose();
            PanelStopBtn.IsVisible = false;
            SetReadOnly(wasReadOnly);         // 解锁：回到编译前的编辑/只读状态
        }
    }

    private async void OnPreviewClicked(object? sender, EventArgs e)
    {
        if (!IsMarkdown(_relPath))
        {
            await DisplayAlertAsync("预览", "仅 Markdown 文件支持预览", "关闭");
            return;
        }
        if (_editable == null)
        {
            await DisplayAlertAsync("预览", "大文件暂不支持预览渲染", "关闭");
            return;
        }

        CommitEditingLine();
        bool show = !PreviewScroll.IsVisible;
        if (show)
        {
            var isDark = IsDarkTheme;
            PreviewContent.Clear();
            PreviewContent.Add(MarkdownPreview.Render(_editable.ReadAll(), isDark));
        }
        Canvas.IsVisible = !show;
        LineEditor.IsVisible = false;
        PreviewScroll.IsVisible = show;
    }

    // ── 状态栏 ──

    private void UpdateStatus()
    {
        if (_doc == null) { StatusLabel.Text = ""; return; }

        var mark = _modified ? "● " : "";
        var ro = _canEdit ? (_readOnly ? "只读" : "编辑") : "只读";
        // 加 "L" 前缀：`SelectionChangedRange` 给的是**行区间**（"398" 或 "398-405"），
        // 光写「已选 398」会被读成「选了 398 个字符」—— 紧挨着的「光标 L398」就是这个格式。
        var sel = Canvas.HasSelection ? $" · 已选 L{Canvas.SelectionChangedRange}" : "";

        // 诊断计数。**放在前面**：状态栏这一行本来就长，排到末尾多半先被挤出屏幕，
        // 而「有几个错」正是用户扫一眼就想要的信息。
        var diag = "";
        var (errs, warns, infos) = _relPath.Length == 0 ? (0, 0, 0) : DiagnosticManager.Counts(_relPath);
        if (errs + warns + infos > 0)
        {
            diag = $" · {(errs > 0 ? "❌" : "⚠")} 警告：{warns}个，错误：{errs}个";
            if (infos > 0) diag += $"，提示：{infos}个";
        }

        // 字号紧跟在光标行右边：捏合缩放时要能**看着数字调**（「到底放大到几号了」此前只能靠手感）。
        StatusLabel.Text = $"{mark}{_forcedEncodingName ?? _doc.EncodingName} · {ro}{diag} · {_doc.LineCount:N0} 行 · "
                         + $"{FormatSize(_fileBytes)} · 光标 L{Math.Max(1, Canvas.CaretLine)}"
                         + $" · 字号{EditorTypography.FontSize:F1}{sel}"
#if DEBUG
                         // 定位「点击位置与渲染不一致」用的读数：只在调试构建里出现
                         + $" · X{Canvas.ScrollX:F0}/{Canvas.MaxScrollX:F0}"
                         + $" · {Canvas.TapProbe}"
#endif
                         ;
    }

    /// <summary>
    /// 轻提示 —— MAUI 没有跨平台的 Toast 抽象（引入 CommunityToolkit 只为这一句话不划算），
    /// 就借用状态栏：它不阻塞、不弹框，且下一次 <see cref="UpdateStatus"/> 会自然覆盖回去。
    /// </summary>
    private void ShowToast(string message, int ms = 2000)
    {
        StatusLabel.Text = message;
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(ms), UpdateStatus);
    }
    /// <summary>
    /// 把输入框在平台侧彻底抹掉：**关自带光标 + 透明背景 + 透明选中高亮**。
    ///
    /// 这个 <c>Entry</c> 只负责换出软键盘，文字与光标**全部由画布自绘** ——
    /// 它的任何原生视觉残留都会和画布上的自绘光标打架（用户实测「红色光标跳出来」）。
    /// 高度也固定成 1px（见 XAML；不用 0，怕平台侧算布局时报错），钉在顶部不动。
    /// </summary>
    private void HidePlatformCaret()
    {
#if ANDROID
        if (LineEditor.Handler?.PlatformView is Android.Widget.EditText et)
        {
            et.SetCursorVisible(false);
            et.SetBackgroundColor(Android.Graphics.Color.Transparent);
            et.SetHighlightColor(Android.Graphics.Color.Transparent);
        }
#endif
    }
}

#if IOS || MACCATALYST
/// <summary>
/// 带 Tab 键命令的输入框。UIKit 里 Tab 是**命令键**（不走 `ShouldChangeCharacters`），
/// 要在 `KeyCommands` 里声明 `UIKeyCommand`，并由**控件自己的类**实现对应 selector ——
/// 这就是它必须是子类、而不能靠 Mapper 打在现成实例上的原因。
/// 修饰键要分别声明：`Shift+Tab` 是另一条 key command。
/// </summary>
internal sealed class TabAwareTextField : Microsoft.Maui.Platform.MauiTextField
{
    /// <summary>参数 = 是否按着 Shift。由页面订阅（见 EditorPage.OnIosTabPressed）。</summary>
    public event Action<bool>? TabPressed;

    public override UIKit.UIKeyCommand[]? KeyCommands =>
    [
        // ⚠ 没有 `UIKeyCommand.InputTab` 这个常量（.NET 绑定里只有 InputEscape/方向键那几个），
        // 直接用字面量制表符。修饰键必须**分别声明**一条：Shift+Tab 是另一条 key command。
        UIKit.UIKeyCommand.Create(new Foundation.NSString("\t"), 0,
            new ObjCRuntime.Selector("wcTab:")),
        UIKit.UIKeyCommand.Create(new Foundation.NSString("\t"), UIKit.UIKeyModifierFlags.Shift,
            new ObjCRuntime.Selector("wcShiftTab:")),
    ];

    [Foundation.Export("wcTab:")]
    public void OnTab(UIKit.UIKeyCommand command) => TabPressed?.Invoke(false);

    [Foundation.Export("wcShiftTab:")]
    public void OnShiftTab(UIKit.UIKeyCommand command) => TabPressed?.Invoke(true);
}

/// <summary>只服务编辑器那个输入框的 handler（用 CreatePlatformView 换成 TabAwareTextField）。</summary>
internal sealed class TabAwareEntryHandler : Microsoft.Maui.Handlers.EntryHandler
{
    protected override Microsoft.Maui.Platform.MauiTextField CreatePlatformView() => new TabAwareTextField();
}
#endif
