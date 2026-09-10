namespace WayCoder;

/// <summary>
/// 跨端纯文本显示逻辑（无 UI 依赖）——供 MAUI/GUI/Web 等前端复用同一文案与格式化，
/// 并可在主自测（桌面）中直接断言。前端层的 UI helper 应委托到这里而非各自复写。
/// </summary>
public static class UiText
{
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

    /// <summary>会话节点角色是否为「正文」（user/assistant）——tool/system 等非正文不渲染为聊天气泡。</summary>
    public static bool IsSessionBodyRole(string role) => role is "user" or "assistant";
}
