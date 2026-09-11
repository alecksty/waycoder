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

    /// <summary>经济模式的两字短语名（TUI 状态行 / 切换提示用）：省钱 / 自动 / 极致 / 关闭。
    /// 与 <see cref="EconomyName"/>（设置项取值：开/自动/极致/关）刻意并存 —— 一个是叙述句里的词，
    /// 一个是下拉取值；但**每套都只有这一份**，别再各端硬编码 switch。</summary>
    public static string EconomyShortName(EconomyMode m) => m switch
    {
        EconomyMode.On => "省钱",
        EconomyMode.Auto => "自动",
        EconomyMode.Extreme => "极致",
        _ => "关闭",
    };

    // ── 权限模式文案唯一真源 ──
    // 此前同一个枚举有 5 种叫法（Ask / 必问 / Ask（每次确认）/ 问答ACK / YOLO (上帝模式)…），
    // 散在 PermissionManager / AutoCommand / WebChat / GuiCommands / Maui 五处各写一遍 switch。

    /// <summary>权限模式中文档名（四档）。</summary>
    public static string PermNameZh(PermissionManager.Mode m) => m switch
    {
        PermissionManager.Mode.Yolo => "畅通",
        PermissionManager.Mode.SmartAuto => "智能",
        PermissionManager.Mode.Auto => "自动",
        _ => "必问",
    };

    /// <summary>权限模式一句话说明。</summary>
    public static string PermDesc(PermissionManager.Mode m) => m switch
    {
        PermissionManager.Mode.Yolo => "不确认，直接执行",
        PermissionManager.Mode.SmartAuto => "智能分级：只读放行，危险操作每次确认",
        PermissionManager.Mode.Auto => "改必问：只读放行，危险/修改操作逐次确认",
        _ => "必问：危险/修改操作每次都确认",
    };

    /// <summary>中文档名 + 英文标识（下拉/状态行）：如「畅通 YOLO」。</summary>
    public static string PermLabel(PermissionManager.Mode m) => $"{PermNameZh(m)} {PermName(m)}";

    /// <summary>带说明的完整标签（下拉项）：如「畅通 YOLO（不确认，直接执行）」。</summary>
    public static string PermFull(PermissionManager.Mode m) => $"{PermLabel(m)}（{PermDesc(m)}）";

    /// <summary>紧凑标签（状态栏用，无空格）：必问ASK / 自动AUTO / 智能SMART / 畅通YOLO。</summary>
    public static string PermCompact(PermissionManager.Mode m) => m switch
    {
        PermissionManager.Mode.Yolo => "畅通YOLO",
        PermissionManager.Mode.SmartAuto => "智能SMART",
        PermissionManager.Mode.Auto => "自动AUTO",
        _ => "必问ASK",
    };

    /// <summary>相对时间（刚刚 / N 秒前 / N 分钟前 / N 小时前 / N 天前 / N 周前 / MM-dd HH:mm）。
    ///
    /// 三处各写一遍（TUI 侧边栏会话区 / 移动端会话列表 / 检查点列表）且**已经漂移**：
    /// 移动端那份漏了「N 周前」分支 —— 10 天前在手机上是「10 天前」、在桌面是「1 周前」。
    /// 秒级分支按调用方保留为可选（检查点列表要「N 秒前」，会话列表不需要）。
    /// </summary>
    /// <param name="withSeconds">是否显示「N 秒前」档（<10s 仍为「刚刚」）。</param>
    public static string RelativeTime(DateTime now, DateTime past, bool withSeconds = false)
    {
        var d = now - past;
        if (d.TotalSeconds < (withSeconds ? 10 : 60)) return "刚刚";
        if (withSeconds && d.TotalSeconds < 60) return $"{(int)d.TotalSeconds} 秒前";
        if (d.TotalMinutes < 60) return $"{(int)d.TotalMinutes} 分钟前";
        if (d.TotalHours < 24) return $"{(int)d.TotalHours} 小时前";
        if (d.TotalDays < 7) return $"{(int)d.TotalDays} 天前";
        if (d.TotalDays < 30) return $"{(int)(d.TotalDays / 7)} 周前";
        return past.ToString("MM-dd HH:mm");
    }

    /// <summary>千分位/K 缩写（todo/上下文/token 统计）。</summary>
    public static string FormatK(int n) => n >= 1000 ? $"{n / 1000.0:F1}k" : n.ToString();

    /// <summary>会话节点角色是否为「正文」（user/assistant）——tool/system 等非正文不渲染为聊天气泡。</summary>
    public static bool IsSessionBodyRole(string role) => role is "user" or "assistant";
}
