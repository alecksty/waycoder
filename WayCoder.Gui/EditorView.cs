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
using WayCoder.Infra;
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

    /// <summary>
    /// 半角列宽 —— **用字体的设计值（字号 ÷ 2），不用实测值**。
    ///
    /// 位置一律由列号算出、不看字体度量：只要测量与渲染是两条路径就必然差一点
    /// （平台会把行宽取整、字形 fallback 也各走各的），误差只能压小压不掉。
    /// MAUI 那边为此折腾了八轮，最终结论就是这条（见 TextEditorMath 的网格段）。
    /// </summary>
    private const double HalfWidth = FontSize / 2;
    private const int GutterWidth = 52;
    private const int TabSize = 4;
    private EditorCore? _core;
    private IBrush _bg = Brushes.Transparent;
    private IBrush _text = Brushes.White;
    private IBrush _gutter = Brushes.Gray;
    private IBrush _gutterBg = Brushes.Transparent;
    private IBrush _border = Brushes.Gray;
    private double _blinkPhase;

    /// <summary>横向滚动偏移（px）。</summary>
    private double _hScroll;

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

    /// <summary>从主题资源取画刷（深/浅主题切换后由 EditorWindow 重新调用，GuiColors 按当前变体解析）。</summary>
    public void ResolveThemeBrushes()
    {
        _bg = GuiColors.WindowBg;
        _text = GuiColors.Text;
        _gutter = GuiColors.DimText;
        _gutterBg = GuiColors.PanelBg;
        _border = GuiColors.Border;
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

            // 语法高亮：tab 展开 4 空格渲染（不改缓冲），token 偏移映射到展开串；横向滚动裁剪
            var (display, spans) = ExpandLine(lineText, _core.Syntax.Tokenize(lineText));
            var ft = MakeText(display, _text);
            // 裁剪判据**两边必须同单位**：横向滚动是像素，而 span 给的是**列号**。
            // 此前拿字符下标去比像素值，横滚之后该跳的不跳、该画的不画。
            var firstCol = TextEditorMath.XToColumn((float)_hScroll, (float)HalfWidth);
            var lastCol = TextEditorMath.XToColumn((float)(_hScroll + Bounds.Width), (float)HalfWidth);
            foreach (var (start, len, col, cols, color) in spans)
            {
                if (col + cols <= firstCol) continue;      // 完全在视口左侧
                if (col >= lastCol) break;                 // 已过视口右侧
                ft.SetForegroundBrush(SyntaxBrushMap.ForFg(color, _text), start, len);
            }
            dc.DrawText(ft, new Point(TextX, y));

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
        var x1 = TextX + TextWidth(line, from);
        var x2 = TextX + TextWidth(line, to);
        dc.FillRectangle(GuiColors.Selection, new Rect(x1, y, Math.Max(0, x2 - x1), LineHeight));
    }

    private void DrawCaret(DrawingContext dc)
    {
        if (_core == null) return;
        _blinkPhase += 0.06;
        if ((int)_blinkPhase % 2 == 0) return; // 闪烁

        var y = Padding + (_core.Cy - Math.Max(0, _core.Scroll)) * LineHeight;
        var x = TextX + TextWidth(_core.Lines[_core.Cy].ToString(), _core.Cx);
        dc.FillRectangle(GuiColors.Caret, new Rect(x, y, 2, LineHeight));
    }

    /// <summary>文本起始 x（gutter 后 + 左内边距 - 横向滚动）。</summary>
    private double TextX => GutterWidth + Padding - _hScroll;

    /// <summary>
    /// 第 <paramref name="col"/> 个码元之前的显示宽度 —— 走共享网格，**不量字体**。
    ///
    /// 原先是对前缀建一个 <see cref="FormattedText"/> 取 <c>Width</c>：一条 1029 字符的行，
    /// 光光标定位每帧就要建上千个 FormattedText。网格换算是纯算术，且与 MAUI 自绘画布共用
    /// 同一份实现（<see cref="TextEditorMath"/>），两端不可能各算各的。
    /// </summary>
    private static double TextWidth(string line, int col)
        => TextEditorMath.ColumnsToX(
               TextEditorMath.MeasureColumns(line, Math.Clamp(col, 0, line.Length), TabSize),
               (float)HalfWidth);

    /// <summary>
    /// 把缓冲行 + token 展开为「显示串 + (起点, 长度, 起始列, 占几列, 色值) 跨度」。
    ///
    /// 三处修正（都是原来按 <c>char</c> 遍历留下的）：
    /// ① **tab 按列位展开**（原来一律当 4 个空格，列位 2 上的 tab 该补 2 格却补了 4 格）；
    /// ② **按 Rune 走**，代理对（emoji）不再被当成两个字符各算一列；
    /// ③ 顺带产出**起始列与列数** —— 渲染侧要靠它做同单位的裁剪（见 Render），
    ///    而列号在推进时顺手就算出来了，不必再量一遍。
    /// </summary>
    private static (string Display, List<(int Start, int Len, int Col, int Cols, int Color)> Spans) ExpandLine(
        string line, List<(string Text, int Color)> tokens)
    {
        var display = new System.Text.StringBuilder(line.Length + 8);
        var spans = new List<(int, int, int, int, int)>(tokens.Count);
        var dispPos = 0;
        var col = 0;
        foreach (var (text, color) in tokens)
        {
            var startPos = dispPos;
            var startCol = col;
            foreach (var r in text.EnumerateRunes())
            {
                if (r.Value == '\t')
                {
                    var next = (col / TabSize + 1) * TabSize;
                    display.Append(' ', next - col);
                    dispPos += next - col;
                    col = next;
                }
                else
                {
                    display.Append(r.ToString());
                    dispPos += r.Utf16SequenceLength;
                    col += WayCoder.UI.Shared.Terminal.AnsiString.CharWidth(r);
                }
            }
            if (dispPos > startPos) spans.Add((startPos, dispPos - startPos, startCol, col - startCol, color));
        }
        return (display.ToString(), spans);
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
        _core.Cx = VisualToCol(_core.Lines[lineIdx].ToString(), pos.X - TextX);
        InvalidateVisual();
        e.Handled = true;
    }

    /// <summary>滚轮：水平滚动（Shift+纵向或触控板横向）→ 横向滚动；纵向 → 翻页/行滚动。</summary>
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (_core == null) return;
        if (Math.Abs(e.Delta.X) > Math.Abs(e.Delta.Y))
        {
            _hScroll = Math.Max(0, _hScroll - e.Delta.X * 40);
            InvalidateVisual();
            e.Handled = true;
        }
        else
        {
            var lines = e.Delta.Y > 0 ? -3 : 3;
            _core.Scroll = Math.Clamp(_core.Scroll + lines, 0, Math.Max(0, _core.TotalLines - 1));
            InvalidateVisual();
            e.Handled = true;
        }
    }

    /// <summary>
    /// 把点击 x 折算为缓冲下标 —— 走共享网格，**不量字体**。
    ///
    /// 这里原来是「第二把尺子」：对**每个字符**单独建一个 <see cref="FormattedText"/> 累加宽度，
    /// 与渲染那条路必然差一点（CJK 宽窄、fallback 字形、tab 展开规则各走各的）。
    /// 现在与绘制同用一个列号模型：落在字符前半归它、后半归它之后。
    /// </summary>
    private static int VisualToCol(string line, double x)
        => TextEditorMath.ColumnToCharIndex(line,
               TextEditorMath.XToColumn((float)x, (float)HalfWidth), TabSize);

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
