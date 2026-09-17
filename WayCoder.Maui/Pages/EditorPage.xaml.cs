using System.Text;
using WayCoder.Infra;
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
    private long _editLine = -1;           // 正在编辑的行（0-based）
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
        // 选区一变就同时刷状态栏与**选区操作条**（选词/扩选/全选/清除都从画布发这个事件）
        Canvas.SelectionChanged += (_, _) => { UpdateStatus(); UpdateSelectionBar(); };
        // 视口一变，条子要跟着选区走（滚出视口时收起来）—— 不跟就会停在原地，
        // 而选区已经滚走了，看着像「操作条在乱飘」
        Canvas.ViewChanged += () =>
        {
            UpdateStatus();
            UpdateSelectionBar();
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
        var choice = await DisplayActionSheetAsync("编辑器", "取消", null,
            "💾  保存",
            "📄  另存为…",
            "✨  新建文件…",
            "✎  切换 编辑/只读",
            "↶  撤销",
            "↷  重做",
            "🔍  查找…",
            "🔢  跳到行…",
            "📖  大纲…",
            "👁  Markdown 预览",
            "📋  全选并复制",
            $"↩️  重置字号（当前 {EditorTypography.FontSize:0.#}）");

        switch (choice)
        {
            case "💾  保存": await SaveAsync(); break;
            case "📄  另存为…": await SaveAsAsync(); break;
            case "✨  新建文件…": await NewFileAsync(); break;
            case "✎  切换 编辑/只读": OnEditClicked(this, EventArgs.Empty); break;
            case "↶  撤销": OnUndoClicked(this, EventArgs.Empty); break;
            case "↷  重做": OnRedoClicked(this, EventArgs.Empty); break;
            case "🔍  查找…": OnFindClicked(this, EventArgs.Empty); break;
            case "🔢  跳到行…": await GoToLineAsync(); break;
            case "📖  大纲…": OnOutlineClicked(this, EventArgs.Empty); break;
            case "👁  Markdown 预览": OnPreviewClicked(this, EventArgs.Empty); break;
            case "📋  全选并复制": await CopyAllAsync(); break;
            case var c when c != null && c.StartsWith("↩️"): ResetFontSize(); break;
        }
    }

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
        LineEditor.IsVisible = false;

        EditorTypography.FontSize = MauiEditorStore.FontSize;   // 套用上次调的字号
        LineEditor.FontSize = EditorTypography.FontSize;
        LineEditor.HeightRequest = 1;
        // ⚠ HeightRequest 只是「请求」，**不是上限**：Entry 在 VerticalOptions=Start 下会按内容
        // 自然高度撑开（13pt 加 EditText 默认内边距实测约 3 个行高），于是它的选区高亮变成
        // 一条跨 3 行的矩形、两个选择手柄落到编辑行下方两行去。文字与光标都是画布画的，
        // 所以只有高亮/手柄会暴露这个失真。MaximumHeightRequest 才是真正的钳制。
        LineEditor.MaximumHeightRequest = 1;

        bool dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        Canvas.SetDocument(_doc, relPath, dark, _canEdit);
        SetReadOnly(true);   // 打开一律先进只读（对齐旧行为：默认只读，手动解锁编辑）
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
            Shell.SetTabBarIsVisible(this, !on);
        }
        FileRow.IsVisible = !on;
        ToolRow.IsVisible = !on;
        StatusLabel.IsVisible = !on;
        RestoreBtn.IsVisible = on;
    }

    private void OnFullscreenEnterClicked(object? sender, EventArgs e) => SetFullscreen(true);

    private void OnFullscreenExitClicked(object? sender, EventArgs e) => SetFullscreen(false);

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (Shell.Current != null) Shell.Current.Navigating += OnShellNavigating;
        // 每次进来都回到「有栏」状态：全屏是靠隐藏导航栏实现的，若带着全屏状态重新进入，
        // 用户第一眼看到的是没有返回箭头的界面，容易以为进了死路。
        if (_fullscreen) SetFullscreen(false);
    }

    protected override void OnDisappearing()
    {
        if (Shell.Current != null) Shell.Current.Navigating -= OnShellNavigating;
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
            // 原子写 + 保留原编码/换行风格（旧实现是 File.WriteAllText 直接覆盖，中途失败会留半截文件）
            SandboxFsService.WriteTextAtomic(_relPath, _editable.ReadAll(),
                _doc?.Encoding ?? new UTF8Encoding(false), _doc?.UsesCrlf ?? false);
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

        _committing = true;
        // **原样带进去，不展开 tab**（原来这里 ExpandTabs 成空格）。
        // 展开的后果是「编辑一下，文件里的制表符就永久变成空格了」—— 用户明确要的是
        // **真制表符**：1 个字符、删一次删掉整格、显示宽度走 TabColumns（4 列）。
        // 展开这件事由**绘制侧**负责：`BuildLineRuns` 开头就是 ExpandTabs，所以 `	` 画出来
        // 仍是 4 列宽；`MeasurePrefixWidth` / `CharIndexAtX` 也都先展开再映射，光标与点击同源。
        LineEditor.Text = text;
        _committing = false;

        Canvas.EditingLine = oneBased;
        Canvas.EditingText = LineEditor.Text ?? "";

        // 把「点在哪一格」换算成字符下标。
        // 先把横坐标换成**视觉列**，再经 VisualColToSourceIndex 换成字符下标 ——
        // 后者认 CJK 占两列（中文行里直接按字符数算会偏出好几格）。
        int col = ClickXToCharIndex(xInLine, LineEditor.Text ?? "");
        LineEditor.CursorPosition = Math.Clamp(col, 0, (LineEditor.Text ?? "").Length);
        Canvas.EditingCursor = LineEditor.CursorPosition;
        Canvas.SetCaretLine(oneBased);
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

    private void CommitEditingLine()
    {
        if (_editLine < 0 || _editable == null) return;
        var newText = LineEditor.Text ?? "";
        var oldText = _editable.GetLine(_editLine) ?? "";
        long line = _editLine;
        _editLine = -1;
        Canvas.EditingLine = -1;
        Canvas.EditingText = null;
        LineEditor.IsVisible = false;
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

        var text = e.NewTextValue ?? "";

        // 多行粘贴：Entry 是单行控件，各平台对含换行的粘贴处理不一致（替换成空格 / 截断），
        // 自己拆更可靠 —— 首行留在当前行，其余插入到下面。
        if (text.Contains('\n') || text.Contains('\r'))
        {
            var parts = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            long line = _editLine;
            _editable.ReplaceRange(line, 1, parts);
            _history.Push(new EditOp(line, [_editable.GetLine(line) ?? ""], parts, 0, 0, Environment.TickCount64));
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
    }

    private void OnLineEditorCompleted(object? sender, EventArgs e)
    {
        if (_editLine < 0 || _editable == null) return;

        // Enter：提交当前行 → 下方插入空行 → 编辑器移到新行（比让 Entry 吞掉 Enter 更可控）
        var text = LineEditor.Text ?? "";
        long line = _editLine;
        _editable.ReplaceRange(line, 1, [text]);
        _history.Push(new EditOp(line, [_editable.GetLine(line) ?? ""], [text], 0, 0, Environment.TickCount64));
        _editable.ReplaceRange(line + 1, 0, [""]);
        _history.Push(new EditOp(line + 1, [], [""], 0, 0, Environment.TickCount64));

        _modified = true;
        _editLine = -1;
        Canvas.EditingLine = -1;
        Canvas.EditingText = null;
        LineEditor.IsVisible = false;
        Canvas.InvalidateAll();
        BeginEditLine(line + 2);
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

    private async void OnPreviewClicked(object? sender, EventArgs e)
    {
        var isMarkdown = _relPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            || _relPath.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase);
        if (!isMarkdown)
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
            var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
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
        // 字号紧跟在光标行右边：捏合缩放时要能**看着数字调**（「到底放大到几号了」此前只能靠手感）。
        StatusLabel.Text = $"{mark}{_doc.EncodingName} · {ro} · {_doc.LineCount:N0} 行 · "
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
