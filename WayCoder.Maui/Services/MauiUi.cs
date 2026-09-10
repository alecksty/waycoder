namespace WayCoder.Maui.Services;

/// <summary>
/// MAUI 各页共享的 UI 取值助手——收敛 ChatPage / CommandPanelPage / SessionHistoryPage /
/// ReasoningDetailPage 等各自重复实现的：主题色取资源、深色判断、确认权限名、经济模式名、
/// 千分位格式化、当前模型栏文本。改一处全端生效（DRY 提炼）。
/// </summary>
public static class MauiUi
{
    public static bool IsDark => Application.Current?.RequestedTheme == AppTheme.Dark;

    /// <summary>取命名资源色（缺失回退 DimGray）。</summary>
    public static Color Res(string key)
        => Application.Current?.Resources.TryGetValue(key, out var v) == true
            ? (v as Color) ?? Colors.DimGray
            : Colors.DimGray;

    /// <summary>取命名资源色（可为 null 语义，供某些场景 fallback 到其它色）。</summary>
    public static Color? ResOrNull(string key)
        => Application.Current?.Resources.TryGetValue(key, out var v) == true ? v as Color : null;

    /// <summary>确认权限显示名（与 AgentService.GetStatus PermMode 一致）。</summary>
    /// <summary>确认权限显示名（实现上移 core UiText，便于主自测覆盖）。</summary>
    public static string PermName(PermissionManager.Mode m) => UiText.PermName(m);

    /// <summary>经济模式显示名（上移 core UiText）。</summary>
    public static string EconomyName(EconomyMode m) => UiText.EconomyName(m);

    /// <summary>千分位/K 缩写（上移 core UiText）。</summary>
    public static string FormatK(int n) => UiText.FormatK(n);

    /// <summary>当前模型栏文本（通道前缀 + (provider)model），顶栏与侧栏共用。</summary>
    public static string ModelText()
    {
        var cfg = Config.Instance;
        return ConnectionConfig.FormatModelChannel(ConnectionConfig.CurrentMainChannel(), cfg.Provider, cfg.Model);
    }
}
