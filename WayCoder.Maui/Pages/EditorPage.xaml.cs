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
        Canvas.ViewChanged += UpdateStatus;
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
            MauiEditorStore.SetFontSize(EditorTypography.FontSize);   // 手势结束才落盘一次
            ShowToast($"字号 {EditorTypography.FontSize:F0}");
        };
        // 一滑动就结束编辑：编辑态下浮着一个输入框，滚动会让它和自绘的行对不上；
        // 而且滑动本身就意味着「我要浏览」——先把这一行提交掉再滚，最省心。
        Canvas.ScrollingStarted += () => { if (_editLine >= 0) CommitEditingLine(); };
        Canvas.ShowDebugHud = MauiEditorStore.ShowDebugHud;

        // 输入框与画布共用同一份排版常量：字体族/字号/行高/内边距只要有一处不同，
        // 切换编辑的瞬间就会跳一下。
        LineEditor.FontFamily = EditorTypography.FontFamilyName;
        LineEditor.FontSize = EditorTypography.FontSize;
        LineEditor.HeightRequest = EditorTypography.LineHeight;
        // ⚠ HeightRequest 只是「请求」，**不是上限**：Entry 在 VerticalOptions=Start 下会按内容
        // 自然高度撑开（13pt 加 EditText 默认内边距实测约 3 个行高），于是它的选区高亮变成
        // 一条跨 3 行的矩形、两个选择手柄落到编辑行下方两行去。文字与光标都是画布画的，
        // 所以只有高亮/手柄会暴露这个失真。MaximumHeightRequest 才是真正的钳制。
        LineEditor.MaximumHeightRequest = EditorTypography.LineHeight;
        LineEditor.BackgroundColor = Colors.Transparent;
        // 文字也透明：这一行由画布自绘（见 CodeCanvasView.EditingLine 的注释）。
        // Entry 保留下来只为了三件事——IME 组合输入、软键盘、系统复制粘贴菜单。
        // 两层都显示文字的话，各自的行高/内边距规则不同，必然错位。
        LineEditor.TextColor = Colors.Transparent;

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
            }
#elif IOS
            if (LineEditor.Handler?.PlatformView is UIKit.UITextField tf)
                tf.TintColor = UIKit.UIColor.Clear;
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
#endif
    }

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
#endif

    // ── 右上角菜单 ──

    private async void OnMenuClicked(object? sender, EventArgs e)
    {
        CommitEditingLine();   // 先把正在编辑的行落盘，再弹菜单（弹菜单会失焦）

        // 字号可以是小数（捏合给的是连续值），所以这里按一位小数显示，别截成整数
        string fs = $"{EditorTypography.FontSize:0.#}";
        // 工具栏那 7 个图标也一并收进来：工具栏是「一眼可见」，菜单是「全都在这里」——
        // 功能一多，图标按钮就会挤成一片看不出谁是谁，不如给一个完整的清单入口。
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
            $"🔠  加大字体（当前 {fs}）",
            "🔡  缩小字体",
            "↩️  重置字号");

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
            case var c when c != null && c.StartsWith("🔠"): AdjustFontSize(+1); break;
            case "🔡  缩小字体": AdjustFontSize(-1); break;
            case "↩️  重置字号": AdjustFontSize(0); break;
        }
    }

    /// <summary>调字号（0 = 重置为默认）。步长 <see cref="EditorTypography.FontStep"/>；捏合那条路没有档位、连续取值。</summary>
    private void AdjustFontSize(int delta)
        => ApplyFontSize(delta == 0
                ? EditorTypography.DefaultFontSize
                : EditorTypography.FontSize + delta * EditorTypography.FontStep,
            persist: true, toast: true);

    /// <summary>
    /// **改字号的唯一入口** —— 捏合、菜单「加大/缩小/重置」全部走这里。
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
        LineEditor.HeightRequest = EditorTypography.LineHeight;
        // ⚠ HeightRequest 只是「请求」，**不是上限**：Entry 在 VerticalOptions=Start 下会按内容
        // 自然高度撑开（13pt 加 EditText 默认内边距实测约 3 个行高），于是它的选区高亮变成
        // 一条跨 3 行的矩形、两个选择手柄落到编辑行下方两行去。文字与光标都是画布画的，
        // 所以只有高亮/手柄会暴露这个失真。MaximumHeightRequest 才是真正的钳制。
        LineEditor.MaximumHeightRequest = EditorTypography.LineHeight;
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
        _modified = false;
        FileLabel.Text = name;
        Title = Path.GetFileName(name);
        UpdateStatus();
        ShowToast($"已另存为 {name}");
    }

    private async Task NewFileAsync()
    {
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
        LineEditor.HeightRequest = EditorTypography.LineHeight;
        // ⚠ HeightRequest 只是「请求」，**不是上限**：Entry 在 VerticalOptions=Start 下会按内容
        // 自然高度撑开（13pt 加 EditText 默认内边距实测约 3 个行高），于是它的选区高亮变成
        // 一条跨 3 行的矩形、两个选择手柄落到编辑行下方两行去。文字与光标都是画布画的，
        // 所以只有高亮/手柄会暴露这个失真。MaximumHeightRequest 才是真正的钳制。
        LineEditor.MaximumHeightRequest = EditorTypography.LineHeight;

        bool dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        Canvas.SetDocument(_doc, relPath, dark, _canEdit);
        SetReadOnly(true);   // 打开一律先进只读（对齐旧行为：默认只读，手动解锁编辑）
        UpdateStatus();

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

    // ── 工具条 ──

    private void SetReadOnly(bool readOnly)
    {
        _readOnly = readOnly;
        if (readOnly) CommitEditingLine();

        EditBtn.Source = readOnly ? "icon_edit" : "icon_lock";
        EditBtn.IsEnabled = _canEdit;
        EditBtn.Opacity = _canEdit ? 1 : 0.35;
        UndoBtn.IsEnabled = _canEdit && !readOnly;
        RedoBtn.IsEnabled = _canEdit && !readOnly;
        SaveBtn.IsEnabled = _canEdit && !readOnly;
        Canvas.EditingLine = -1;
        UpdateStatus();
    }

    private void OnEditClicked(object? sender, EventArgs e)
    {
        if (!_canEdit)
        {
            _ = DisplayAlertAsync("只读",
                $"文件 {FormatSize(_fileBytes)} 超过可编辑上限（{MauiEditorStore.ReadOnlyMaxBytes / (1024 * 1024)}MB）。\n" +
                "编辑需要把内容装进内存，再大就无法保证不闪退 —— 可在「设置 › 编辑器」里调高上限。", "知道了");
            return;
        }
        SetReadOnly(!_readOnly);
    }

    private async void OnSaveClicked(object? sender, EventArgs e) => await SaveAsync();

    private async Task SaveAsync()
    {
        if (_editable == null || _fullPath.Length == 0) return;
        CommitEditingLine();
        try
        {
            // 原子写 + 保留原编码/换行风格（旧实现是 File.WriteAllText 直接覆盖，中途失败会留半截文件）
            SandboxFsService.WriteTextAtomic(_relPath, _editable.ReadAll(),
                _doc?.Encoding ?? new UTF8Encoding(false), _doc?.UsesCrlf ?? false);
            _modified = false;
            UpdateStatus();
            await DisplayAlertAsync("已保存", _relPath, "确定");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("保存失败", ex.Message, "关闭");
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
        UpdateStatus();
    }

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

        // 量一次条子宽度，再按选区左端摆放（尽量居中于选区所在行，靠边时贴边）
        double barW = SelectionBar.Width > 0 ? SelectionBar.Width : 240;
        double x = Math.Max(4, Math.Min(Canvas.GutterWidthPx + 8, Canvas.Width - barW - 4));
        double y = Canvas.SelectionTopY - (SelectionBar.Height > 0 ? SelectionBar.Height : 40) - 6;
        if (y < 4) y = Canvas.SelectionTopY + EditorTypography.LineHeight + 6;   // 顶部放不下就摆到下方
        SelectionBar.TranslationX = x;
        SelectionBar.TranslationY = y;
        SelectionBar.IsVisible = true;
    }

    private async void OnSelCopyClicked(object? sender, EventArgs e)
    {
        var text = Canvas.GetSelectedText();
        if (text.Length == 0) { ShowToast("没有选中内容"); return; }
        // 剪贴板仍走系统（那是**数据**通道，不是 UI）；复制完收起选区，与桌面编辑器一致
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

        long line = Canvas.CaretLine > 0 ? Canvas.CaretLine : 1;
        if (_editLine != line - 1) BeginEditLine(Canvas.CaretLine);
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

    private void BeginEditLine(long oneBased, float xInLine = -1f)
    {
        if (_editable == null || _readOnly) return;
        if (_editLine == oneBased - 1 && LineEditor.IsVisible)
        {
            // 已经在编辑这一行：只把光标挪到点到的位置（不重建、不打断 IME）
            if (xInLine > 0)
            {
                LineEditor.CursorPosition = ClickXToCharIndex(xInLine, LineEditor.Text ?? "");
                Canvas.EditingCursor = LineEditor.CursorPosition;
                Canvas.EnsureCaretVisible();
            }
            LineEditor.Focus();
            return;
        }
        CommitEditingLine();

        _editLine = oneBased - 1;
        var text = _editable.GetLine(_editLine) ?? "";

        _committing = true;
        // tip：制表符在 Entry 里无法与自绘保持一致的宽度，编辑时先展开成空格
        LineEditor.Text = TextEditorMath.ExpandTabs(text, EditorTypography.TabColumns);
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
        // 把这一行带到可视区中部：软键盘占掉下半屏，贴着底部编辑会看不见自己在打什么，
        // 系统也可能为了「让焦点控件可见」而自行滚动页面（那会让画布坐标和实际显示错开）。
        _ = EnsureEditorVisibleAsync(oneBased);
        Canvas.Invalidate();
        UpdateStatus();
    }

    private IDispatcherTimer? _caretSync;

    /// <summary>
    /// 光标位置同步：<c>Entry</c> 没有「光标移动」事件，用户在框内点一下换位置时自绘光标不会动。
    /// 轻量轮询解决，且**只在值真的变了才重绘**，不会变成定时刷屏。
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
        Canvas.EditingCursor = pos;
        Canvas.EnsureCaretVisible();               // 长行时把光标带进视野
    }

    /// <summary>
    /// 编辑期间保证该行可见：先滚到中部，等软键盘把布局撑开（AdjustResize）之后再校一次。
    /// 键盘高度在 Focus 那一刻还没到，只做一次的话算出来的是「键盘弹出前」的位置。
    /// </summary>
    private async Task EnsureEditorVisibleAsync(long oneBased)
    {
        Canvas.ScrollToLine(oneBased, center: true);
        await Task.Delay(260);
        if (_editLine != oneBased - 1) return;   // 期间已经切走/提交了
        PositionEditor(oneBased);
        Canvas.ScrollToLine(oneBased, center: true);
    }

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
        float y = Canvas.LineScreenY(oneBased, (float)Canvas.Height) ?? -1;
        if (y < 0) return;
        LineEditor.TranslationY = y;
        LineEditor.Margin = new Thickness(
            Canvas.GutterWidthPx + EditorTypography.TextLeftPad - Canvas.ScrollX, 0, 0, 0);
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

    private void OnLineEditorUnfocused(object? sender, FocusEventArgs e) => CommitEditingLine();

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
        var sel = Canvas.HasSelection ? $" · 已选 {Canvas.SelectionChangedRange}" : "";
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
}
