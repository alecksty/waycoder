using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using WayCoder.UI.Tui.Edit;

namespace WayCoder.UI.Gui;

/// <summary>
/// Avalonia 自定义编辑器控件：绑定共享 EditorCore（纯数据模型），
/// 渲染语法高亮 + 行号 + 诊断 gutter + 光标 + 选区；中文 IME 走 OnTextInput。
/// 与 TUI TuiRichEditor、Web 编辑器共享同一 EditorCore 模型，键位对齐。
/// </summary>
public class EditorView : Control
{
    private static readonly FontFamily Mono = new("Menlo,Consolas,monospace");
    private static readonly Typeface Typeface = new(Mono);
    private const double FontSize = 13;
    private const double LineHeight = 19.5;
    private const double Padding = 8;
    private const int GutterWidth = 52;
    private static readonly IBrush CaretBrush = new SolidColorBrush(Color.Parse("#4f8cff"));
    private static readonly IBrush SelectionBrush = new SolidColorBrush(Color.Parse("#33518c"));

    private EditorCore? _core;
    private IBrush _bg = Brushes.Transparent;
    private IBrush _text = Brushes.White;
    private IBrush _gutter = Brushes.Gray;
    private IBrush _gutterBg = Brushes.Transparent;
    private IBrush _border = Brushes.Gray;
    private double _blinkPhase;

    /// <summary>Core 更换时触发（EditorWindow 借此重订阅状态栏/脏标记）。</summary>
    public event Action? CoreChanged;

    /// <summary>编辑器核心（绑定后订阅事件）。</summary>
    public EditorCore? Core
    {
        get => _core;
        set
        {
            if (_core != null)
            {
                _core.OnContentChanged -= OnCoreChanged;
                _core.OnDiagnosticsReady -= OnCoreChanged;
            }
            _core = value;
            if (_core != null)
            {
                _core.OnContentChanged += OnCoreChanged;
                _core.OnDiagnosticsReady += OnCoreChanged;
            }
            CoreChanged?.Invoke();
            InvalidateVisual();
        }
    }

    public EditorView()
    {
        Focusable = true;
        ClipToBounds = true;
        ResolveThemeBrushes();
    }

    /// <summary>从主题资源取画刷（深/浅主题切换后由 EditorWindow 重新调用）。</summary>
    public void ResolveThemeBrushes()
    {
        IBrush Theme(string key, string fallback)
        {
            if (Application.Current?.Resources.TryGetResource(key, null, out var v) == true
                && v is IBrush b) return b;
            return new SolidColorBrush(Color.Parse(fallback));
        }
        _bg = Theme("WindowBgBrush", "#0f1117");
        _text = Theme("TextBrush", "#e6e8ee");
        _gutter = Theme("DimTextBrush", "#8b93a7");
        _gutterBg = Theme("PanelBgBrush", "#171a23");
        _border = Theme("BorderBrush", "#262b3a");
        InvalidateVisual();
    }

    private void OnCoreChanged() => InvalidateVisual();

    /// <summary>加载文件（对齐 EditorScreen.LoadAndBuild：IndentMode 跟随配置）。</summary>
    public void LoadFile(string? path)
    {
        var core = new EditorCore();
        if (!string.IsNullOrEmpty(path)) core.LoadFile(path);
        core.IndentMode = Config.Instance.EditorIndent;
        Core = core;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Core = null; // 解绑事件，防窗口关闭后泄漏
        base.OnDetachedFromVisualTree(e);
    }

    // ════════════════════════ 渲染 ════════════════════════

    public override void Render(DrawingContext dc)
    {
        base.Render(dc);
        dc.FillRectangle(_bg, new Rect(Bounds.Size));

        if (_core == null) return;

        var first = Math.Max(0, _core.Scroll);
        var visible = (int)(Bounds.Height / LineHeight) + 2;

        // gutter 背景 + 边框
        dc.FillRectangle(_gutterBg, new Rect(0, 0, GutterWidth, Bounds.Height));
        dc.DrawLine(new Pen(_border), new Point(GutterWidth, 0), new Point(GutterWidth, Bounds.Height));

        for (var idx = first; idx < first + visible && idx < _core.TotalLines; idx++)
        {
            var y = Padding + (idx - first) * LineHeight;
            var lineText = _core.Lines[idx].ToString();

            // 诊断行背景
            var diags = _core.GetDiagnosticsAtLine(idx + 1);
            if (diags.Count > 0)
                dc.FillRectangle(SyntaxBrushMap.DiagBg(diags[0].Severity),
                    new Rect(GutterWidth, y, Bounds.Width - GutterWidth, LineHeight));

            // 行号
            dc.DrawText(MakeText((idx + 1).ToString(CultureInfo.InvariantCulture), _gutter),
                new Point(GutterWidth - Padding - 4, y));

            // 语法高亮（tab 原样渲染，token 偏移与缓冲 1:1；count 钳制防越界）
            var ft = MakeText(lineText, _text);
            var offset = 0;
            foreach (var (text, color) in _core.Syntax.Tokenize(lineText))
            {
                var count = Math.Min(text.Length, lineText.Length - offset);
                if (count > 0)
                    ft.SetForegroundBrush(SyntaxBrushMap.ForFg(color, _text), offset, count);
                offset += text.Length;
            }
            dc.DrawText(ft, new Point(GutterWidth + Padding, y));

            // 选区
            DrawSelection(dc, idx, y);
        }

        DrawCaret(dc);
    }

    private void DrawSelection(DrawingContext dc, int lineIdx, double y)
    {
        var anchor = _core!.SelectionAnchor;
        if (anchor == null) return;
        var (aL, aC) = anchor.Value;
        var (cL, cC) = (_core.Cy, _core.Cx);
        if (aL == cL && aC == cC) return;

        var lo = (aL, aC);
        var hi = (cL, cC);
        if (aL > cL || (aL == cL && aC > cC)) { lo = (cL, cC); hi = (aL, aC); }

        if (lineIdx < lo.Item1 || lineIdx > hi.Item1) return;
        if (lineIdx == lo.Item1 && lineIdx == hi.Item1)
        {
            // 单行
            DrawSelectionRange(dc, lineIdx, lo.Item2, hi.Item2, y);
        }
        else if (lineIdx == lo.Item1)
        {
            DrawSelectionRange(dc, lineIdx, lo.Item2, _core.Lines[lineIdx].Length, y);
        }
        else if (lineIdx == hi.Item1)
        {
            DrawSelectionRange(dc, lineIdx, 0, hi.Item2, y);
        }
        else
        {
            DrawSelectionRange(dc, lineIdx, 0, _core.Lines[lineIdx].Length, y);
        }
    }

    private void DrawSelectionRange(DrawingContext dc, int lineIdx, int from, int to, double y)
    {
        var line = _core!.Lines[lineIdx].ToString();
        var x1 = TextWidth(line, from);
        var x2 = TextWidth(line, to);
        dc.FillRectangle(SelectionBrush, new Rect(GutterWidth + Padding + x1, y, Math.Max(0, x2 - x1), LineHeight));
    }

    private void DrawCaret(DrawingContext dc)
    {
        if (_core == null) return;
        _blinkPhase += 0.06;
        if ((int)_blinkPhase % 2 == 0) return; // 闪烁

        var y = Padding + (_core.Cy - Math.Max(0, _core.Scroll)) * LineHeight;
        var x = TextWidth(_core.Lines[_core.Cy].ToString(), _core.Cx);
        dc.FillRectangle(CaretBrush, new Rect(GutterWidth + Padding + x, y, 2, LineHeight));
    }

    private double TextWidth(string line, int col)
    {
        col = Math.Clamp(col, 0, line.Length);
        var prefix = line[..col];
        return MakeText(prefix, _text).Width;
    }

    private static FormattedText MakeText(string text, IBrush brush)
        => new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface, FontSize, brush);

    // ════════════════════════ 输入 ════════════════════════

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_core == null) return;

        var ctrl = (e.KeyModifiers & KeyModifiers.Control) != 0;

        // 系统剪贴板 C/X/V
        if (ctrl)
        {
            switch (e.Key)
            {
                case Key.C when _core.HasSelection:
                    CopySelectionAsync(); e.Handled = true; return;
                case Key.X when _core.HasSelection:
                    CopySelectionAsync(); _core.DeleteSelection(); e.Handled = true; return;
                case Key.V:
                    PasteAsync(); e.Handled = true; return;
            }
        }

        if (EditorKeyMap.Handle(_core, e)) e.Handled = true;
        else if (e.Key == Key.Space) { _core.InsertText(" "); e.Handled = true; }
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (_core == null || string.IsNullOrEmpty(e.Text)) return;
        if (_core.HasSelection) _core.DeleteSelection();
        _core.InsertText(e.Text!);
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        if (_core == null) return;
        var pos = e.GetPosition(this);
        var lineIdx = Math.Clamp((int)((pos.Y - Padding) / LineHeight) + Math.Max(0, _core.Scroll), 0, _core.TotalLines - 1);
        _core.Cy = lineIdx;
        _core.Cx = VisualToCol(_core.Lines[lineIdx].ToString(), pos.X - GutterWidth - Padding);
        InvalidateVisual();
        e.Handled = true;
    }

    /// <summary>把点击 x 折算为缓冲列（按字符宽度走，CJK 双宽）。</summary>
    private int VisualToCol(string line, double x)
    {
        if (x <= 0) return 0;
        double acc = 0;
        for (var i = 0; i < line.Length; i++)
        {
            var w = CharWidth(line[i]);
            if (acc + w / 2 >= x) return i;
            acc += w;
        }
        return line.Length;
    }

    private double CharWidth(char c)
    {
        var ft = MakeText(c.ToString(), _text);
        return ft.Width;
    }

    private async void CopySelectionAsync()
    {
        if (_core?.GetSelectedText() is { } sel && TopLevel.GetTopLevel(this)?.Clipboard is { } cb)
            await cb.SetTextAsync(sel);
    }

    private async void PasteAsync()
    {
        if (_core == null || TopLevel.GetTopLevel(this)?.Clipboard is not { } cb) return;
        var text = await cb.TryGetTextAsync(); // Avalonia 12：GetTextAsync → TryGetTextAsync（扩展方法）
        if (string.IsNullOrEmpty(text)) return;
        if (_core.HasSelection) _core.DeleteSelection();
        _core.InsertText(text);
    }
}
