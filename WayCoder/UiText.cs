namespace WayCoder;

/// <summary>
/// 跨端纯文本显示逻辑（无 UI 依赖）——供 MAUI/GUI/Web 等前端复用同一文案与格式化，
/// 并可在主自测（桌面）中直接断言。前端层的 UI helper 应委托到这里而非各自复写。
///
/// <para>
/// ⚠ <b>双语化规则（改造时务必遵守）</b>：
/// <list type="number">
/// <item><b>中文分支逐字不动</b> —— 全仓 400+ 条自测断言钉着这些中文串，改一个字就红一片。
///   英文分支是新加的孪生，两边**各自独立成形**（英文的语序、复数、标点都不一样，
///   不要试图用模板把中文串"缝"出英文）。</item>
/// <item>所有取文案的成员写成<b>表达式体属性 / switch 表达式里的 <see cref="L.Pick"/></b>，
///   <b>绝不写 <c>static readonly</c> 字段</b>（那会把语言冻在首次访问那一刻，见 <see cref="L"/>）。</item>
/// <item>英文分支禁止出现全角标点（<c>：（）、「」，。！？</c>）与 CJK —— 自测有护栏。</item>
/// </list>
/// </para>
/// </summary>
public static partial class UiText
{
    /// <summary>确认权限显示名（英文标识，**语言中性**——本来就是英文标识，两语言同形）。</summary>
    public static string PermName(PermissionManager.Mode m) => m switch
    {
        PermissionManager.Mode.Yolo => "Yolo",
        PermissionManager.Mode.SmartAuto => "SmartAuto",
        PermissionManager.Mode.Auto => "Auto",
        _ => "Ask",
    };

    /// <summary>经济模式显示名（设置项取值）。</summary>
    public static string EconomyName(EconomyMode m) => m switch
    {
        EconomyMode.On => L.Pick("开", "On"),
        EconomyMode.Auto => L.Pick("自动", "Auto"),
        EconomyMode.Extreme => L.Pick("极致", "Extreme"),
        _ => L.Pick("关", "Off"),
    };

    /// <summary>经济模式的两字短语名（TUI 状态行 / 切换提示用）：省钱 / 自动 / 极致 / 关闭。
    /// 与 <see cref="EconomyName"/>（设置项取值：开/自动/极致/关）刻意并存 —— 一个是叙述句里的词，
    /// 一个是下拉取值；但**每套都只有这一份**，别再各端硬编码 switch。</summary>
    public static string EconomyShortName(EconomyMode m) => m switch
    {
        EconomyMode.On => L.Pick("省钱", "Saver"),
        EconomyMode.Auto => L.Pick("自动", "Auto"),
        EconomyMode.Extreme => L.Pick("极致", "Extreme"),
        _ => L.Pick("关闭", "Off"),
    };

    // ── 权限模式文案唯一真源 ──
    // 此前同一个枚举有 5 种叫法（Ask / 必问 / Ask（每次确认）/ 问答ACK / YOLO (上帝模式)…），
    // 散在 PermissionManager / AutoCommand / WebChat / GuiCommands / Maui 五处各写一遍 switch。

    /// <summary>权限模式显示名（四档）。
    /// ⚠ 原名叫 <c>PermNameZh</c> —— 那个名字在双语化后已不成立（它按语言返回中/英），
    /// 故改名；中文取值逐字未动（畅通/智能/自动/必问）。</summary>
    public static string PermDisplayName(PermissionManager.Mode m) => m switch
    {
        PermissionManager.Mode.Yolo => L.Pick("畅通", "Yolo"),
        PermissionManager.Mode.SmartAuto => L.Pick("智能", "Smart"),
        PermissionManager.Mode.Auto => L.Pick("自动", "Auto"),
        _ => L.Pick("必问", "Ask"),
    };

    /// <summary>英文里可读的权限名（比 <see cref="PermName"/> 那个驼峰标识更像人话）。</summary>
    private static string PermReadableName(PermissionManager.Mode m) => m switch
    {
        PermissionManager.Mode.Yolo => "Yolo",
        PermissionManager.Mode.SmartAuto => "Smart Auto",
        PermissionManager.Mode.Auto => "Auto",
        _ => "Ask",
    };

    /// <summary>权限模式一句话说明。</summary>
    public static string PermDesc(PermissionManager.Mode m) => m switch
    {
        PermissionManager.Mode.Yolo => L.Pick(
            "不确认，直接执行",
            "Never asks — runs everything"),
        PermissionManager.Mode.SmartAuto => L.Pick(
            "智能分级：只读放行，危险操作每次确认",
            "Read-only allowed; risky operations confirmed every time"),
        PermissionManager.Mode.Auto => L.Pick(
            "改必问：只读放行，危险/修改操作逐次确认",
            "Read-only allowed; writes and risky operations confirmed every time"),
        _ => L.Pick(
            "必问：危险/修改操作每次都确认",
            "Asks before every risky or modifying operation"),
    };

    /// <summary>显示名 + 英文标识（下拉/状态行）：如「畅通 YOLO」。
    /// 中文侧保持「中文名 + 大写标识」的双写形态；英文侧重复两遍没有意义，只留可读名。</summary>
    public static string PermLabel(PermissionManager.Mode m) => L.Pick(
        $"{PermDisplayName(m)} {PermName(m)}",
        PermReadableName(m));

    /// <summary>带说明的完整标签（下拉项）：如「畅通 YOLO（不确认，直接执行）」。
    /// 英文里全角括号换成破折号 —— 全角括号在英文句子里很突兀。</summary>
    public static string PermFull(PermissionManager.Mode m) => L.Pick(
        $"{PermLabel(m)}（{PermDesc(m)}）",
        $"{PermLabel(m)} — {PermDesc(m)}");

    /// <summary>紧凑标签（状态栏用，无空格）：必问ASK / 自动AUTO / 智能SMART / 畅通YOLO。</summary>
    public static string PermCompact(PermissionManager.Mode m) => m switch
    {
        PermissionManager.Mode.Yolo => L.Pick("畅通YOLO", "YOLO"),
        PermissionManager.Mode.SmartAuto => L.Pick("智能SMART", "SMART"),
        PermissionManager.Mode.Auto => L.Pick("自动AUTO", "AUTO"),
        _ => L.Pick("必问ASK", "ASK"),
    };

    /// <summary>「中文名 + 英文标识」**带空格**版（TUI 权限横幅用）：畅通 YOLO / 智能 SMART /
    /// 自动 AUTO / 问答 ACK。
    ///
    /// <para>
    /// ⚠ 与 <see cref="PermCompact"/> **刻意并存、且不可互换**：那个无空格，且第三档是
    /// 「必问」而非「问答」（`必问ASK` vs `问答 ACK`）—— 词与空格都不同。
    /// 本档这四串是 TUI 横幅的历史形态，**逐字不可改**（改了就改桌面输出）。
    /// 原先 <c>PermissionManager.SetPermissionMode</c> 里手写了一份逐字相同的 switch，
    /// 属"同一事实两处实现"。
    /// </para>
    /// </summary>
    public static string PermLabelSpaced(PermissionManager.Mode m) => m switch
    {
        PermissionManager.Mode.Yolo => L.Pick("畅通 YOLO", "Yolo"),
        PermissionManager.Mode.SmartAuto => L.Pick("智能 SMART", "Smart Auto"),
        PermissionManager.Mode.Auto => L.Pick("自动 AUTO", "Auto"),
        _ => L.Pick("问答 ACK", "Ask"),
    };

    // ── 对话框按钮的共享符号 ──
    // 判定方引用**同一个符号**而不是同一段中文 —— 这是"标记与文案分离"里
    // 「比自己传进去的变量」那个形态的收口处（弹窗返回值只能按文案反查，
    // 所以必须保证「显示用的串」与「判定用的串」是同一个来源）。
    // 摆在这里而不是各页面各写一份：翻文案时只需改这一处。
    public static string BtnAllow => L.Pick("允许", "Allow");
    public static string BtnAlwaysAllow => L.Pick("总是允许", "Always allow");
    public static string BtnDeny => L.Pick("拒绝", "Deny");
    public static string BtnCancel => L.Pick("取消", "Cancel");
    public static string BtnOk => L.Pick("确定", "OK");

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
        // 中文档无复数概念（「1 分钟前」「2 分钟前」同形）；英文档要处理单复数，
        // 故两侧各自成形，不共用模板。
        if (d.TotalSeconds < (withSeconds ? 10 : 60)) return L.Pick("刚刚", "just now");
        if (withSeconds && d.TotalSeconds < 60)
        {
            var sec = (int)d.TotalSeconds;
            return L.Pick($"{sec} 秒前", $"{sec} second{Plural(sec)} ago");
        }
        if (d.TotalMinutes < 60)
        {
            var min = (int)d.TotalMinutes;
            return L.Pick($"{min} 分钟前", $"{min} minute{Plural(min)} ago");
        }
        if (d.TotalHours < 24)
        {
            var hr = (int)d.TotalHours;
            return L.Pick($"{hr} 小时前", $"{hr} hour{Plural(hr)} ago");
        }
        if (d.TotalDays < 7)
        {
            var day = (int)d.TotalDays;
            return L.Pick($"{day} 天前", $"{day} day{Plural(day)} ago");
        }
        if (d.TotalDays < 30)
        {
            var wk = (int)(d.TotalDays / 7);
            return L.Pick($"{wk} 周前", $"{wk} week{Plural(wk)} ago");
        }
        return past.ToString("MM-dd HH:mm");
    }

    /// <summary>英文复数后缀（中文档用不到，只在英文分支里调）。</summary>
    private static string Plural(int n) => n == 1 ? "" : "s";

    /// <summary>千分位/K 缩写（todo/上下文/token 统计）。**语言中性**，无翻译。</summary>
    public static string FormatK(int n) => n >= 1000 ? $"{n / 1000.0:F1}k" : n.ToString();

    /// <summary>会话节点角色是否为「正文」（user/assistant）——tool/system 等非正文不渲染为聊天气泡。</summary>
    public static bool IsSessionBodyRole(string role) => role is "user" or "assistant";
}
