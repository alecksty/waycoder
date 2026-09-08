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
    public static string PermName(PermissionManager.Mode m) => m switch
    {
        PermissionManager.Mode.Yolo => "Yolo",
        PermissionManager.Mode.SmartAuto => "SmartAuto",
        PermissionManager.Mode.Auto => "Auto",
        _ => "Ask",
    };

    /// <summary>经济模式显示名（枚举顺序 Off→Auto→On→Extreme 非直觉序）。</summary>
    public static string EconomyName(EconomyMode m) => m switch
    {
        EconomyMode.On => "开",
        EconomyMode.Auto => "自动",
        EconomyMode.Extreme => "极致",
        _ => "关",
    };

    /// <summary>千分位/K 缩写（todo/上下文/token 统计）。</summary>
    public static string FormatK(int n) => n >= 1000 ? $"{n / 1000.0:F1}k" : n.ToString();

    /// <summary>当前模型栏文本（通道前缀 + (provider)model），顶栏与侧栏共用。</summary>
    public static string ModelText()
    {
        var cfg = Config.Instance;
        return ConnectionConfig.FormatModelChannel(ConnectionConfig.CurrentMainChannel(), cfg.Provider, cfg.Model);
    }
}
