namespace WayCoder.UI.TUI.Custom;

/// <summary>列表类控件共享的按键判据。</summary>
internal static class TuiListNav
{
    /// <summary>
    /// 是否为「列表导航键」：上下方向键 / Home / End / PageUp / PageDown。
    ///
    /// 这张键表此前在 <c>ModelPicker.ClassifyKey</c> 与 <c>SearchableListPicker</c> 的 KeyHook
    /// 里各写一遍 —— 漂移的后果很具体：同一个键在一个选择器里能移动选择，
    /// 在另一个选择器里却落进搜索框变成过滤词（用户会以为「按键失灵」）。
    /// </summary>
    public static bool IsNavKey(ConsoleKeyInfo key) => key.Key
        is ConsoleKey.UpArrow or ConsoleKey.DownArrow
        or ConsoleKey.Home or ConsoleKey.End
        or ConsoleKey.PageUp or ConsoleKey.PageDown;
}
