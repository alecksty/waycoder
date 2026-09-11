namespace WayCoder.UI.Tui;

/// <summary>
/// 聊天角色的「显示名 / 正文色 / 图标色」**唯一真源**。
///
/// 此前这套表有四份平行实现 —— <c>TuiListItem</c> 的 RoleName / RoleColor / IconColor 加
/// <c>TuiMarkdown.FgForRole</c> —— 已经漂移：
/// - <c>tool</c>：TuiMarkdown 取 <c>ChatToolFg</c>，TuiListItem 落 <c>_ =&gt;</c> 取 <c>ControlFg</c>
///   ⇒ **同一条工具消息的正文与角色名颜色不同源**；
/// - <c>agent</c>：TuiListItem 三张表都显式映射到 assistant 色，FgForRole 靠 <c>_</c> 兜底成 ControlFg。
///
/// 更实证的一处：主题里 6 个变体共 24 处给 <c>ChatUserFg</c>/<c>ChatSystemFg</c>/<c>IconUserFg</c>/
/// <c>IconSystemFg</c> 赋了值，却**没有任何渲染器读它们** —— 因为两张表都把 user/system 硬编码成
/// <c>BrightWhite</c>。即「重复表压过单一真源」：换主题时用户消息颜色不跟随。
/// 现在三处统一从这里取，主题键真正生效；新增角色只改这一处。
/// </summary>
public static class ChatRoleStyle
{
    /// <summary>角色显示名（对齐模板角色头）。未知角色原样返回。</summary>
    public static string DisplayName(string role) => role switch
    {
        "user" => "用户",
        "assistant" or "agent" => "智能体",
        "system" => "系统",
        "tool" => "工具",
        _ => role,
    };

    /// <summary>角色**正文与角色名**色。</summary>
    public static int Fg(string role) => role switch
    {
        "user" => TuiTheme.Current.ChatUserFg,
        "assistant" or "agent" => TuiTheme.Current.ChatAssistantFg,
        "system" => TuiTheme.Current.ChatSystemFg,
        "tool" => TuiTheme.Current.ChatToolFg,
        _ => TuiTheme.Current.ControlFg,
    };

    /// <summary>角色**图标**色。</summary>
    public static int IconFg(string role) => role switch
    {
        "user" => TuiTheme.Current.IconUserFg,
        "assistant" or "agent" => TuiTheme.Current.IconAssistantFg,
        "system" => TuiTheme.Current.IconSystemFg,
        "tool" => TuiTheme.Current.IconToolFg,
        _ => TuiTheme.Current.ControlFg,
    };
}
