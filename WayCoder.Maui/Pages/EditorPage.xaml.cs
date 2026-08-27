using System.Text;
using WayCoder.Maui.Markup;
using WayCoder.Maui.Services;
using WayCoder.UI.Tui.Edit;

namespace WayCoder.Maui.Pages;

/// <summary>
/// 内置代码编辑器（M4）。
///
/// 编辑 UI 用 MAUI <see cref="Editor"/> 原生控件（触控软键盘 + 系统级撤销/重做），
/// 文件读写走 <see cref="SandboxFsService"/>（沙箱钳制），大纲/符号提取复用
/// <see cref="EditorCore"/>（纯数据模型，无渲染/键盘依赖）。
///
/// 编辑与分析的桥接：<see cref="BuildCoreFromEditor"/> 用 LoadFile 设置 FilePath+Syntax
/// （正确推断扩展名/语法），再把编辑器当前文本覆盖进 Lines，供 ExtractOutline 分析——
/// 这样未保存的编辑也能出大纲，而非只能分析落盘旧内容。
/// </summary>
[QueryProperty(nameof(FilePath), "path")]
public partial class EditorPage : ContentPage
{
    /// <summary>沙箱内相对路径（Shell 路由参数 "path"）。</summary>
    public string FilePath
    {
        set => _ = LoadAsync(value);
    }

    private string _filePath = "";
    private bool _modified;
    private bool _loading; // 装载时抑制 TextChanged 把初值误标为「已修改」

    // 撤销/重做历史（基于文本快照；MAUI Editor 无原生 Undo/Redo，自维护栈）
    private readonly List<string> _undoStack = new();
    private readonly List<string> _redoStack = new();
    private bool _applyingHistory; // 程序化设置文本时置位，避免污染历史
    private const int MaxHistory = 200;

    public EditorPage()
    {
        InitializeComponent();

#if ANDROID
        // 代码编辑器：禁用软换行（横向滚动）+ 高亮 Label 随 EditText 横向平移同步
        // （透明文字叠加方案：Editor 内部横向滚动，Label 用 TranslationX 跟随，行号栏固定不滚）
        CodeEditor.HandlerChanged += (_, _) =>
        {
            if (CodeEditor.Handler?.PlatformView is Android.Widget.EditText et)
            {
                et.SetHorizontallyScrolling(true);
                if (OperatingSystem.IsAndroidVersionAtLeast(23))
                    et.SetOnScrollChangeListener(new EditorScrollListener(
                        sx => this.Dispatcher.Dispatch(() => HighlightLayer.TranslationX = -sx)));
            }
        };
#endif
    }

#if ANDROID
    /// <summary>EditText 横向滚动监听 → 同步高亮 Label 平移（透明叠加方案）。</summary>
    private sealed class EditorScrollListener : Java.Lang.Object, Android.Views.View.IOnScrollChangeListener
    {
        private readonly Action<int> _onScrollX;
        public EditorScrollListener(Action<int> onScrollX) => _onScrollX = onScrollX;

        public void OnScrollChange(Android.Views.View? v, int scrollX, int scrollY, int oldScrollX, int oldScrollY)
            => _onScrollX(scrollX);
    }
#endif

    /// <summary>从沙箱相对路径加载文件内容到编辑器。</summary>
    private async Task LoadAsync(string relPath)
    {
        _loading = true;
        _filePath = relPath;
        var content = SandboxFsService.ReadText(relPath);
        if (content == null)
        {
            CodeEditor.Text = "";
            FileLabel.Text = Path.GetFileName(relPath);
            Title = Path.GetFileName(relPath);
            _modified = false;
            _loading = false;
            UpdateHighlight();
            UpdateStatus();
            await DisplayAlertAsync("无法打开", $"文件不存在或为二进制：{relPath}", "关闭");
            return;
        }

        CodeEditor.Text = content;
        FileLabel.Text = relPath;
        Title = Path.GetFileName(relPath);
        _modified = false;
        _undoStack.Clear();
        _redoStack.Clear();
        _loading = false;
        UpdateHighlight();
        UpdateStatus();
    }

    /// <summary>按文件扩展名重算语法高亮并刷新垫底 Label 与行号栏。</summary>
    private void UpdateHighlight()
    {
        var text = CodeEditor.Text ?? "";
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        HighlightLayer.FormattedText = ToolOutputFormatter.RenderEditor(text, _filePath, isDark);
        HighlightLayer.IsVisible = text.Length > 0;

        // 行号栏：统计行数生成 "1\n2\n3..."（与代码同 FontFamily/FontSize 对齐）
        int lines = 1;
        foreach (var c in text)
            if (c == '\n') lines++;
        LineNumbers.Text = lines > 0 ? string.Join("\n", Enumerable.Range(1, lines)) : "1";
        LineNumbers.IsVisible = lines > 1;
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_loading) return;

        // 程序化设置文本（撤销/重做/装载）不记录历史，避免栈污染
        if (!_applyingHistory)
        {
            _undoStack.Add(e.OldTextValue ?? "");
            if (_undoStack.Count > MaxHistory) _undoStack.RemoveAt(0);
            _redoStack.Clear(); // 新编辑清空重做分支
        }

        _modified = true;
        UpdateHighlight();
        UpdateStatus();
    }

    private bool _previewMode;

    /// <summary>点「预览/源码」切换 markdown 预览（源码编辑 ⇄ MarkdownPreview 渲染视图）。</summary>
    private async void OnPreviewClicked(object? sender, EventArgs e)
    {
        var isMarkdown = _filePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            || _filePath.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase);
        if (!isMarkdown)
        {
            await DisplayAlertAsync("预览", "仅 Markdown 文件支持预览", "关闭");
            return;
        }

        _previewMode = !_previewMode;
        ApplyPreviewMode();
    }

    private void ApplyPreviewMode()
    {
        if (_previewMode)
        {
            var text = CodeEditor.Text ?? "";
            var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
            PreviewContent.Clear();
            PreviewContent.Add(MarkdownPreview.Render(text, isDark));
            EditorScroll.IsVisible = false;
            PreviewScroll.IsVisible = true;
            PreviewBtn.Text = "源码";
        }
        else
        {
            EditorScroll.IsVisible = true;
            PreviewScroll.IsVisible = false;
            PreviewBtn.Text = "预览";
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_filePath)) return;
        try
        {
            SandboxFsService.WriteText(_filePath, CodeEditor.Text ?? "");
            _modified = false;
            UpdateStatus();
            await DisplayAlertAsync("已保存", _filePath, "确定");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("保存失败", ex.Message, "关闭");
        }
    }

    /// <summary>撤销：弹出上一快照恢复，当前文本入重做栈。</summary>
    private void OnUndoClicked(object? sender, EventArgs e)
    {
        if (_undoStack.Count == 0) return;
        _applyingHistory = true;
        _redoStack.Add(CodeEditor.Text ?? "");
        var prev = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        CodeEditor.Text = prev;
        _applyingHistory = false;
        _modified = true;
        UpdateStatus();
    }

    /// <summary>重做：弹出下一快照恢复，当前文本入撤销栈。</summary>
    private void OnRedoClicked(object? sender, EventArgs e)
    {
        if (_redoStack.Count == 0) return;
        _applyingHistory = true;
        _undoStack.Add(CodeEditor.Text ?? "");
        var next = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        CodeEditor.Text = next;
        _applyingHistory = false;
        _modified = true;
        UpdateStatus();
    }

    /// <summary>查找：从光标处向后搜索，未命中则从头循环，选中首个匹配并聚焦。</summary>
    private async void OnFindClicked(object? sender, EventArgs e)
    {
        var text = CodeEditor.Text ?? "";
        if (string.IsNullOrEmpty(text)) return;

        var query = await DisplayPromptAsync("查找", "输入要查找的文本", accept: "查找", cancel: "取消", maxLength: 100);
        if (string.IsNullOrWhiteSpace(query)) return;

        var from = Math.Clamp(CodeEditor.CursorPosition, 0, text.Length);
        var idx = text.IndexOf(query, from, StringComparison.Ordinal);
        if (idx < 0) idx = text.IndexOf(query, 0, StringComparison.Ordinal); // 循环回开头
        if (idx < 0)
        {
            await DisplayAlertAsync("查找", $"未找到「{query}」", "关闭");
            return;
        }

        CodeEditor.CursorPosition = idx;
        CodeEditor.SelectionLength = query.Length;
        CodeEditor.Focus();
    }

    /// <summary>提取大纲并让用户点选跳行。</summary>
    private async void OnOutlineClicked(object? sender, EventArgs e)
    {
        var outline = BuildCoreFromEditor().ExtractOutline();
        if (outline.Count == 0)
        {
            await DisplayAlertAsync("大纲", "当前文件没有可识别的符号", "关闭");
            return;
        }

        var names = outline.Select(o => $"{o.Icon} {o.Name}  (L{o.Line})").ToArray();
        var choice = await DisplayActionSheetAsync("大纲", "取消", null, names);
        if (string.IsNullOrEmpty(choice) || choice == "取消") return;

        var idx = Array.IndexOf(names, choice);
        if (idx < 0) return;
        JumpToLine(outline[idx].Line);
    }

    /// <summary>把编辑器当前内容构造为一个 EditorCore（供大纲/诊断分析，含未保存编辑）。</summary>
    private EditorCore BuildCoreFromEditor()
    {
        var core = new EditorCore();
        // LoadFile 设置 FilePath（推断扩展名）+ Syntax；文件不存在时仍能正确设定二者
        var full = SandboxFsService.ResolveInSandbox(_filePath) ?? Path.GetFileName(_filePath);
        core.LoadFile(full);

        // 覆盖 Lines 为编辑器当前文本（含未保存内容），Normalize 行尾避免 CRLF 干扰
        core.Lines.Clear();
        var text = (CodeEditor.Text ?? "").Replace("\r\n", "\n");
        foreach (var ln in text.Split('\n'))
            core.Lines.Add(new StringBuilder(ln));
        if (core.Lines.Count == 0)
            core.Lines.Add(new StringBuilder());
        return core;
    }

    /// <summary>跳转到 1-based 行号（计算该行首字符偏移并聚焦）。</summary>
    private void JumpToLine(int oneBasedLine)
    {
        var text = CodeEditor.Text ?? "";
        int line = 1, offset = 0;
        while (offset < text.Length && line < oneBasedLine)
        {
            if (text[offset] == '\n') line++;
            offset++;
        }
        CodeEditor.CursorPosition = offset;
        CodeEditor.Focus();
    }

    private void UpdateStatus()
    {
        var text = CodeEditor.Text ?? "";
        int lines = 1, chars = text.Length;
        foreach (var c in text)
            if (c == '\n') lines++;

        var mark = _modified ? "● " : "";
        StatusLabel.Text = $"{mark}{lines} 行 · {chars} 字符";
    }
}
