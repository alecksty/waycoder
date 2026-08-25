using Avalonia.Input;
using WayCoder.UI.Tui.Edit;

namespace WayCoder.UI.Gui;

/// <summary>
/// 键盘分发：Avalonia KeyDown → EditorCore 编辑操作。
/// 与 TuiRichEditor/TuiEditBase 键位对齐；文本输入（含中文 IME）走 EditorView.OnTextInput，
/// C/X/V/P 剪贴板键返回 false 交给 EditorView 用系统剪贴板。
/// </summary>
public static class EditorKeyMap
{
    public static bool Handle(EditorCore core, KeyEventArgs e)
    {
        var ctrl = (e.KeyModifiers & KeyModifiers.Control) != 0;
        var shift = (e.KeyModifiers & KeyModifiers.Shift) != 0;

        // 移动前若 shift：无选区则开锚点，有选区则扩展（锚点不动、光标移动 = 选区伸缩）
        if (shift && !core.HasSelection) core.StartSelection();
        else if (shift) core.ExtendSelection();

        switch (e.Key)
        {
            // 先匹配带 Ctrl 的（无 when 的裸箭头会吞掉 Ctrl+箭头）
            case Key.Left  when ctrl: core.MoveWord(-1); return true;
            case Key.Right when ctrl: core.MoveWord(1); return true;
            case Key.Z when ctrl: core.Undo(); return true;
            case Key.Y when ctrl: core.Redo(); return true;
            case Key.A when ctrl: core.SelectAll(); return true;
            case Key.D when ctrl: core.DeleteLine(); return true;
            case Key.K when ctrl: core.DeleteToLineEnd(); return true;
            case Key.U when ctrl: core.DeleteToLineEnd(); return true;
            // 剪贴板（C/X/V）交给 EditorView 用系统剪贴板
            case Key.C when ctrl:
            case Key.X when ctrl:
            case Key.V when ctrl:
                return false;

            case Key.Up:      core.MoveCursor(0, -1); return true;
            case Key.Down:    core.MoveCursor(0, 1); return true;
            case Key.Left:    core.MoveCursor(-1, 0); return true;
            case Key.Right:   core.MoveCursor(1, 0); return true;
            case Key.Home:    core.MoveHome(); return true;
            case Key.End:     core.MoveEnd(); return true;
            case Key.PageUp:   core.MovePageUp(20); return true;
            case Key.PageDown: core.MovePageDown(20); return true;
            case Key.Back:   if (core.HasSelection) core.DeleteSelection(); else core.Backspace(); return true;
            case Key.Delete: if (core.HasSelection) core.DeleteSelection(); else core.Delete(); return true;
            case Key.Enter:  if (core.HasSelection) core.DeleteSelection(); core.NewLine(); return true;
            case Key.Tab:
                if (shift) core.IndentBlock(-1);
                else if (core.HasSelection) core.IndentBlock(1);
                else core.InsertTab();
                return true;
        }
        return false;
    }
}
