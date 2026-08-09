using System.Text;
using CoreCoderSharp.Terminal;

namespace CoreCoderSharp.UI.Controls;

/// <summary>
/// 增强版富文本编辑控件 —— 语法高亮、行号、诊断 Gutter、CJK 感知光标。
/// 绑定 EditorCore 数据模型，负责渲染和键盘交互。
///
/// 键盘：
///   ↑↓←→ — 光标移动
///   Home/End — 行首/行尾
///   PgUp/PgDn — 翻页
///   Backspace/Delete — 删除
///   Enter — 换行
///   Tab — 4 空格
///   Ctrl+Z — 撤销
///   Ctrl+X/C/V — 剪切/复制/粘贴
///   Ctrl+Y — 删除行
///   Ctrl+G — 跳转行（触发 OnJumpRequested）
///   Ctrl+S — 保存（触发 OnSaveRequested）
///   可打印字符 — 插入
/// </summary>
public class TuiRichEditor : TuiControl
{
    // ── 数据模型 ──
    public EditorCore Core { get; set; } = new();

    // ── 外观 ──
    public int LineNumberWidth { get; set; } = 5;
    public int GutterWidth { get; set; } = 1;
    public int CursorFg { get; set; } = 30;
    public int CursorBg { get; set; } = 46;
    public int TitleFg { get; set; } = 33;
    public int SeparatorFg { get; set; } = 33;
    public int GutterErrorFg { get; set; } = 31;
    public int GutterWarningFg { get; set; } = 33;
    public int LineNumFg { get; set; } = 2;
    public int BorderFg { get; set; } = 33;

    // ── 事件 ──
    public event Action? OnSaveRequested;
    public event Action? OnJumpRequested;
    public event Action? OnExitRequested;

    /// <summary>可见行数（从 Height 推导）</summary>
    public int VisibleLines => Height > 0 ? Height : 10;

    /// <summary>内容区起始列（跳过行号和 Gutter）</summary>
    private int ContentStart => LineNumberWidth + GutterWidth;

    public TuiRichEditor()
    {
        Width = 80;
        Height = 24;
        Focused = true;
        Fg = 37;
    }

    // ── 渲染 ──

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        // 调整滚动确保光标可见
        AdjustScroll();
        int vh = VisibleLines;
        int prefixW = LineNumberWidth + GutterWidth;

        for (int i = 0; i < vh; i++)
        {
            int li = Core.Scroll + i;
            int row = absY + i;
            if (row < ClipTop || row >= ClipBottom) continue;

            bool isCursor = li == Core.Cy;
            int contentW = Math.Max(0, Width - prefixW);

            // ── 光标行整行高亮 ──
            if (isCursor)
            {
                var rbBg = new RenderBuffer();
                rbBg.Write(row, absX, new string(' ', Width), bg: CursorBg);
                sb.Append(rbBg.ToString());
            }

            if (li < Core.Lines.Count)
            {
                var lineDiags = Core.GetDiagnosticsAtLine(li + 1);
                var hasError = lineDiags.Any(d => d.Severity == Severity.Error);
                var hasWarning = !hasError && lineDiags.Any(d => d.Severity == Severity.Warning);

                // ── 诊断指示符（Gutter） ──
                int gutterFg;
                string gutterSymbol;
                if (hasError) { gutterFg = GutterErrorFg; gutterSymbol = "●"; }
                else if (hasWarning) { gutterFg = GutterWarningFg; gutterSymbol = "▲"; }
                else { gutterFg = 90; gutterSymbol = "·"; }

                // 诊断背景色
                int diagBg = hasError ? 41 : hasWarning ? 103 : 0;

                // ── 行号 ──
                var lnText = (li + 1).ToString().PadLeft(4);
                int lnFg = isCursor ? CursorFg : LineNumFg;
                int lnBg = isCursor ? CursorBg : (Bg > 0 ? Bg : 0);
                WriteAt(sb, row, absX, lnText, lnFg, lnBg);

                // 行号后空格
                if (diagBg > 0 && isCursor)
                    WriteAt(sb, row, absX + 4, " ", lnFg, diagBg);
                else if (isCursor)
                    WriteAt(sb, row, absX + 4, " ", lnFg, CursorBg);
                else
                    WriteAt(sb, row, absX + 4, " ", lnFg, lnBg);

                // ── Gutter 符号 ──
                WriteAt(sb, row, absX + LineNumberWidth, gutterSymbol, gutterFg,
                    isCursor ? (diagBg > 0 ? diagBg : CursorBg) : Bg);

                // ── 语法高亮内容 ──
                RenderSyntaxLine(sb, row, absX + prefixW, Core.Lines[li].ToString(),
                    contentW, isCursor ? CursorBg : (diagBg > 0 ? diagBg : Bg));
            }
            else
            {
                // 空行（缓冲区末尾之后）
                var tildeFg = isCursor ? CursorFg : 2;
                var tildeBg = isCursor ? CursorBg : Bg;
                WriteAt(sb, row, absX, "    ~", tildeFg, tildeBg);
            }
        }
    }

    /// <summary>渲染语法高亮的一行内容，CJK 宽度感知截断</summary>
    private void RenderSyntaxLine(StringBuilder sb, int row, int col, string line,
        int maxVw, int bg)
    {
        if (string.IsNullOrEmpty(line))
        {
            if (bg > 0)
            {
                var rb = new RenderBuffer();
                rb.Write(row, col, " ", bg: bg);
                sb.Append(rb.ToString());
            }
            return;
        }

        var tokens = Core.Syntax.Tokenize(line);
        int vw = 0;
        foreach (var (text, ansiColor) in tokens)
        {
            int textVw = TuiHelper.DisplayWidth(text);
            if (vw + textVw > maxVw)
            {
                int remain = maxVw - vw;
                if (remain > 0)
                {
                    var truncated = TruncateByVw(text, remain);
                    int c = ansiColor > 0 ? ansiColor : (bg > 0 ? 37 : Fg);
                    WriteAt(sb, row, col + vw, truncated, c, bg);
                }
                break;
            }
            int color = ansiColor > 0 ? ansiColor : (bg > 0 ? 37 : Fg);
            WriteAt(sb, row, col + vw, text, color, bg);
            vw += textVw;
        }
    }

    /// <summary>按视觉宽度截断文本（CJK 安全）</summary>
    private static string TruncateByVw(string text, int maxVw)
    {
        int vw = 0;
        int bytePos = 0;
        var runes = text.EnumerateRunes().ToList();
        for (int i = 0; i < runes.Count; i++)
        {
            int w = runes[i].Value > 127 ? 2 : 1;
            if (vw + w > maxVw)
                return text.Substring(0, bytePos);
            vw += w;
            bytePos += runes[i].Utf16SequenceLength;
        }
        return text;
    }

    /// <summary>确保光标在可见区域内</summary>
    private void AdjustScroll()
    {
        int vh = VisibleLines;
        if (Core.Cy < Core.Scroll) Core.Scroll = Core.Cy;
        if (Core.Cy >= Core.Scroll + vh) Core.Scroll = Core.Cy - vh + 1;
        Core.Scroll = Math.Clamp(Core.Scroll, 0, Math.Max(0, Core.Lines.Count - vh));
    }

    // ── 键盘处理 ──

    public override bool HandleKey(ConsoleKeyInfo key)
    {
        bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);

        switch (key.Key)
        {
            // ── 光标移动 ──
            case ConsoleKey.UpArrow:    Core.MoveCursor(0, -1); return true;
            case ConsoleKey.DownArrow:  Core.MoveCursor(0, 1); return true;
            case ConsoleKey.LeftArrow:  Core.MoveCursor(-1, 0); return true;
            case ConsoleKey.RightArrow: Core.MoveCursor(1, 0); return true;
            case ConsoleKey.Home:       Core.MoveHome(); return true;
            case ConsoleKey.End:        Core.MoveEnd(); return true;
            case ConsoleKey.PageUp:     Core.MovePageUp(VisibleLines); return true;
            case ConsoleKey.PageDown:   Core.MovePageDown(VisibleLines); return true;

            // ── 编辑 ──
            case ConsoleKey.Backspace:  Core.Backspace(); return true;
            case ConsoleKey.Delete:     Core.Delete(); return true;
            case ConsoleKey.Enter:      Core.NewLine(); return true;
            case ConsoleKey.Tab:        Core.InsertTab(); return true;

            // ── Ctrl 组合键 ──
            case ConsoleKey.Z when ctrl:
                Core.Undo();
                return true;
            case ConsoleKey.X when ctrl:
                Core.CutLine();
                return true;
            case ConsoleKey.C when ctrl:
                Core.CopyLine();
                return true;
            case ConsoleKey.V when ctrl:
                Core.PasteClipboard();
                return true;
            case ConsoleKey.Y when ctrl:
                Core.DeleteLine();
                return true;
            case ConsoleKey.G when ctrl:
                OnJumpRequested?.Invoke();
                return true;
            case ConsoleKey.S when ctrl:
                OnSaveRequested?.Invoke();
                return true;

            // ── 退出 ──
            case ConsoleKey.Escape:
            case ConsoleKey.Q when ctrl:
                OnExitRequested?.Invoke();
                return true;

            default:
                // 可打印字符插入
                if ((key.KeyChar >= ' ' && key.KeyChar <= '~') || key.KeyChar > 127)
                {
                    Core.InsertText(key.KeyChar.ToString());
                    return true;
                }
                return false;
        }
    }

    public override void OnResize(int newParentW, int newParentH)
    {
        Width = Math.Max(40, newParentW);
        Height = Math.Max(5, newParentH);
    }

    // ── 便捷方法 ──

    /// <summary>加载文件并准备编辑</summary>
    public void LoadFile(string? filePath)
    {
        filePath ??= "untitled.txt";
        if (!File.Exists(filePath) && !filePath.Contains('.'))
            filePath += ".txt";
        Core.LoadFile(filePath);
    }
}
