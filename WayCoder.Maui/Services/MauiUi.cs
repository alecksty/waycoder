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

    /// <summary>
    /// 开关类按钮的配色对（选中 / 未选中）—— **唯一真源**，各页的「大/小模型」「编辑模式」等
    /// 切换键共用，避免每页各写一套十六进制色（此前是各页自己一堆蓝色系字面量，风格不统一）。
    /// 语义：不用品牌蓝，选中态用**反色**（亮色模式近黑底白字、暗色模式近白底黑字），
    /// 未选中态用中性表面色 —— 既与系统深/浅色一致，又仍能一眼分出哪个是选中的。
    /// </summary>
    public static (Color Bg, Color Text) ToggleColors(bool selected) => selected
        ? (Res(IsDark ? "ButtonSelectedBgDark" : "ButtonSelectedBgLight"),
           Res(IsDark ? "ButtonSelectedTextDark" : "ButtonSelectedTextLight"))
        : (Res(IsDark ? "ButtonBgDark" : "ButtonBgLight"),
           Res(IsDark ? "ButtonTextDark" : "ButtonTextLight"));

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
