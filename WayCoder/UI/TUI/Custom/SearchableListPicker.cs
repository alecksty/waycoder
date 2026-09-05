using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.Tui.Controls;

namespace WayCoder.UI.Tui;

/// <summary>
/// Picker 搜索框宿主 —— 统一「搜索输入控件」的样式与 KeyHook 样板：
/// 导航键（↑/↓ Home End PgUp PgDn）转发到列表、Enter 触发动作、其余键留给输入框编辑。
/// ReasoningPicker / FilePicker（及后续同类）此前各自内联重复这段接线，收敛到本宿主单一来源。
/// </summary>
public static class SearchableListPicker
{
    /// <summary>
    /// 给搜索输入框套上统一行为：
    ///   - 样式：Fg=White / Bg=BgBlack（各 Picker 搜索框视觉一致）
    ///   - 导航键（↑↓ Home End PgUp PgDn）转发到 list（OnKey + MarkDirty）
    ///   - Enter 触发 activate
    ///   - onExtraKey 优先裁决（如 FilePicker Backspace 空搜索词上父目录）：返回 true 表示已处理，
    ///     否则落到默认行为（导航/Enter）或输入框（普通字符/Backspace）
    /// </summary>
    public static void WireSearchInput(
        TuiInput search,
        TuiListControl list,
        TuiScreen? screen,
        Action activate,
        Func<ConsoleKeyInfo, bool>? onExtraKey = null)
    {
        search.Fg = AnsiColors.White;
        search.Bg = AnsiColors.BgBlack;

        search.KeyHook = key =>
        {
            // 调用方扩展键优先（如 Backspace 空搜索词上父目录）
            if (onExtraKey != null && onExtraKey(key))
                return true;

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.DownArrow:
                case ConsoleKey.Home:
                case ConsoleKey.End:
                case ConsoleKey.PageUp:
                case ConsoleKey.PageDown:
                    list.OnKey(key);
                    list.MarkDirty();
                    screen?.MarkDirty();
                    return true;
                case ConsoleKey.Enter:
                    activate();
                    return true;
            }

            return false; // 普通字符 / Backspace 交给输入框默认编辑
        };
    }
}
