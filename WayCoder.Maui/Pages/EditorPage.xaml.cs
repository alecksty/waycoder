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
        Canvas.SelectionChanged += (_, _) => UpdateStatus();
        Canvas.ViewChanged += UpdateStatus;
        Canvas.ShowDebugHud = MauiEditorStore.ShowDebugHud;

        // 输入框与画布共用同一份排版常量：字体族/字号/行高/内边距只要有一处不同，
        // 切换编辑的瞬间就会跳一下。
        LineEditor.FontFamily = EditorTypography.FontFamilyName;
        LineEditor.FontSize = EditorTypography.FontSize;
        LineEditor.HeightRequest = EditorTypography.LineHeight;
        LineEditor.BackgroundColor = Colors.Transparent;
        // 文字也透明：这一行由画布自绘（见 CodeCanvasView.EditingLine 的注释）。
        // Entry 保留下来只为了三件事——IME 组合输入、软键盘、系统复制粘贴菜单。
        // 两层都显示文字的话，各自的行高/内边距规则不同，必然错位。
        LineEditor.TextColor = Colors.Transparent;
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

    private async void OnSaveClicked(object? sender, EventArgs e)
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

    private void OnLineTapped(long line)
    {
        Canvas.SetCaretLine(line);
        if (_canEdit && !_readOnly) BeginEditLine(line);
        UpdateStatus();
    }

    private async void OnLineLongPressed(long line)
    {
        CommitEditingLine();
        if (_canEdit && !_readOnly)
        {
            BeginEditLine(line);   // 编辑模式下长按交给系统选择菜单
            return;
        }

        var choice = await DisplayActionSheetAsync($"第 {line} 行", "取消", null,
            "复制此行", "选择行范围", "全选并复制");
        switch (choice)
        {
            case "复制此行":
                await CopyTextAsync(Canvas.GetLineText(line) ?? "");
                break;
            case "选择行范围":
                Canvas.BeginSelection();
                ShowToast("点起始行，再点结束行", 2500);
                break;
            case "全选并复制":
                await CopyAllAsync();
                break;
        }
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

    private void BeginEditLine(long oneBased)
    {
        if (_editable == null || _readOnly) return;
        if (_editLine == oneBased - 1 && LineEditor.IsVisible)
        {
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
        Canvas.EditingCursor = 0;
        Canvas.SetCaretLine(oneBased);
        PositionEditor(oneBased);
        LineEditor.IsVisible = true;
        LineEditor.Focus();
        StartCaretSync();
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

    /// <summary>把输入框对齐到该行位置（用同一份行高与行号栏宽度算，避免错位）。</summary>
    private void PositionEditor(long oneBased)
    {
        float y = Canvas.LineScreenY(oneBased, (float)Canvas.Height) ?? -1;
        if (y < 0) return;
        LineEditor.TranslationY = y;
        LineEditor.Margin = new Thickness(Canvas.GutterWidthPx, 0, 0, 0);
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
        StatusLabel.Text = $"{mark}{_doc.EncodingName} · {ro} · {_doc.LineCount:N0} 行 · "
                         + $"{FormatSize(_fileBytes)} · 光标 L{Math.Max(1, Canvas.CaretLine)}{sel}"
                         + $" · 帧 {Canvas.LastDrawMs:F1}ms";
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
