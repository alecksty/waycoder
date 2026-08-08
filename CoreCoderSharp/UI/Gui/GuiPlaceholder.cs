namespace CoreCoderSharp.UI.Gui;

/// <summary>
/// GUI 实现占位 —— 未来扩展到跨平台 GUI 界面。
/// 当前终端 TUI 实现位于 UI/Tui/ 目录。
///
/// 可能的 GUI 实现：
///   - Avalonia (跨平台 XAML)
///   - MAUI (.NET 多平台应用 UI)
///   - Photino (轻量 WebView 壳)
/// </summary>
public static class GuiPlaceholder
{
    /// <summary>GUI 是否可用（当前始终返回 false）</summary>
    public static bool IsAvailable => false;

    /// <summary>保留的 GUI 入口（当前抛出 NotSupportedException）</summary>
    public static void Launch()
    {
        throw new NotSupportedException(
            "GUI 模式尚未实现。请使用 TUI 终端模式。\n" +
            "未来将支持 Avalonia / MAUI / Photino 中的一种。");
    }
}
