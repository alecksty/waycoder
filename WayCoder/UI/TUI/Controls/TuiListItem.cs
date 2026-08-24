using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 列表项 —— 聊天消息的结构化容器。
///
/// 布局（垂直）：
///   ┌─ Header: TuiIcon + TuiLabel (角色名) + 时间戳 ─┐
///   ├─ Body:   TuiMarkdown (Markdown 正文)          ─┤
///   └─ Footer: TuiLabel (token 计数等元信息, 可选) ─┘
/// </summary>
public class TuiListItem : TuiVBox
{
    // ── 子控件 ──

    /// <summary>角色图标（模板 chat-item.tui 的 Label，code-behind 填字符+色）</summary>
    public TuiLabel Icon { get; set; } = null!;

    /// <summary>角色名称标签</summary>
    public TuiLabel RoleLabel { get; set; } = null!;

    /// <summary>时间戳标签</summary>
    public TuiLabel TimeLabel { get; set; } = null!;

    /// <summary>Markdown 正文</summary>
    public TuiMarkdown Body { get; set; } = null!;

    /// <summary>底部元信息（可选）</summary>
    public TuiLabel? Footer { get; set; }

    // ── 数据 ──

    public string Role { get; set; } = "assistant";
    public string MarkdownContent { get; set; } = "";

    /// <summary>内边距</summary>
    public int PaddingLeft { get; set; } = 1;
    public int PaddingRight { get; set; } = 1;

    /// <summary>续接消息（不渲染角色头部，直接追加内容）</summary>
    public bool Continuation { get; set; }

    /// <summary>纯文本模式：逐行渲染，不走 Markdown 解析（避免行合并）</summary>
    public bool IsPlainText { get; set; }

    /// <summary>嵌套层级（0=顶层；>0 时作为子消息续接无角色头并左缩进）</summary>
    public int Indent { get; set; }

    /// <summary>内容横向对齐（默认左对齐）</summary>
    public EHAlign ContentAlign { get; set; } = EHAlign.Left;

    public TuiListItem()
    {
        Width = 60;
    }

    /// <summary>从角色和内容构建完整列表项</summary>
    public TuiListItem(string role, string content, int maxWidth = 80, bool continuation = false, bool isPlainText = false, EHAlign contentAlign = EHAlign.Left)
    {
        Role = role;
        MarkdownContent = content;
        Width = maxWidth;
        Continuation = continuation;
        IsPlainText = isPlainText;
        ContentAlign = contentAlign;
        BuildContent(maxWidth);
    }

    /// <summary>主题切换后刷新角色标签/时间戳/页脚颜色（正文由 TuiMarkdown 渲染时动态读主题）</summary>
    public void ApplyTheme()
    {
        if (RoleLabel != null)
            RoleLabel.Fg = RoleColor(Role);
        if (Icon != null) Icon.Fg = IconColor(Role);
        if (TimeLabel != null) TimeLabel.Fg = TuiTheme.Current.ChatTimeFg;
        if (Footer != null) Footer.Fg = TuiTheme.Current.ChatFooterFg;
        Body?.Invalidate(); // 正文缓存了旧主题色，标记重解析
        MarkDirty();
    }

    /// <summary>构建内部控件树：布局来自 chat-item.tui 声明式模板，code-behind 填充数据。</summary>
    public void BuildContent(int maxWidth)
    {
        Clear();
        int innerW = maxWidth - PaddingLeft - PaddingRight;

        // 模板化：每条消息按 {role} 占位符加载布局（布局写标记，逻辑写 code-behind）
        var res = TuiMarkup.LoadResource("chat-item.tui",
            new Dictionary<string, string> { ["role"] = Role });
        var root = (TuiVBox)res.View!;
        root.Width = innerW;

        // ── Header: Icon + Role + Time（模板声明，此处填数据）──
        if (!Continuation && Indent == 0)
        {
            Icon = res.Find<TuiLabel>("icon") ?? new TuiLabel("●") { Width = 2, Height = 1 };
            RoleLabel = res.Find<TuiLabel>("roleLabel") ?? new TuiLabel("") { Width = 12, Height = 1 };
            TimeLabel = res.Find<TuiLabel>("timeLabel") ?? new TuiLabel("") { Width = 8, Height = 1 };
            Icon.Text = "●";
            Icon.Fg = IconColor(Role);
            RoleLabel.Text = RoleName(Role);
            RoleLabel.Fg = RoleColor(Role);
            TimeLabel.Text = DateTime.Now.ToString("HH:mm");
            TimeLabel.Fg = TuiTheme.Current.ChatTimeFg;
        }
        else
        {
            // 续接/嵌套消息：隐藏模板 header 行，占位控件保持布局兼容
            var header = res.Find<TuiHBox>("header");
            if (header != null) header.Visible = false;
            Icon = new TuiLabel("") { Width = 0, Height = 1 };
            RoleLabel = new TuiLabel("") { Width = 0, Height = 1 };
            TimeLabel = new TuiLabel("") { Width = 0, Height = 1 };
        }

        // ── Body: Markdown 正文（模板 Markdown 标签，此处设内容/宽度/缩进）──
        Body = res.Find<TuiMarkdown>("body") ?? throw new InvalidOperationException("chat-item.tui 缺少 body 控件");
        Body.Content = MarkdownContent;
        Body.Role = Role;
        Body.IsPlainText = IsPlainText;
        Body.Width = innerW;
        Body.MaxWidth = innerW;
        Body.ContentAlign = ContentAlign;
        Body.Padding = new EdgeInsets(0, 0, 0, 2 + Indent * 2);
        Body.EnsureParsed();

        Add(root);
        Layout();
    }

    /// <summary>角色显示名（对齐模板角色头）</summary>
    private static string RoleName(string role) => role switch
    {
        "user" => "用户",
        "assistant" => "智能体",
        "agent" => "智能体",
        "system" => "系统",
        "tool" => "工具",
        _ => role
    };

    /// <summary>角色文字色</summary>
    private static int RoleColor(string role) => role switch
    {
        "user" => TuiTheme.Current.ChatUserFg,
        "assistant" => TuiTheme.Current.ChatAssistantFg,
        "agent" => TuiTheme.Current.ChatAssistantFg,
        "system" => TuiTheme.Current.ChatSystemFg,
        _ => TuiTheme.Current.ControlFg
    };

    /// <summary>角色图标色</summary>
    private static int IconColor(string role) => role switch
    {
        "user" => TuiTheme.Current.IconUserFg,
        "assistant" => TuiTheme.Current.IconAssistantFg,
        "agent" => TuiTheme.Current.IconAssistantFg,
        "system" => TuiTheme.Current.IconSystemFg,
        "tool" => TuiTheme.Current.IconToolFg,
        _ => TuiTheme.Current.ControlFg
    };

    /// <summary>更新 Markdown 内容（用于流式追加）</summary>
    public void AppendContent(string delta)
    {
        MarkdownContent += delta;
        Body.Content += delta;
        Body.Invalidate();
        Body.Width = Width - PaddingLeft - PaddingRight;
        Body.MaxWidth = Width - PaddingLeft - PaddingRight;
        Body.EnsureParsed();
        ReLayout();
    }

    /// <summary>设置时间戳</summary>
    public void SetTime(DateTime time)
    {
        TimeLabel.Text = time.ToString("HH:mm");
    }

    /// <summary>设置底部元信息</summary>
    public void SetFooter(string text)
    {
        if (Footer == null)
        {
            Footer = new TuiLabel(text)
            {
                Width = Width - PaddingLeft - PaddingRight,
                Height = 1,
                Fg = TuiTheme.Current.ChatFooterFg
            };
            Add(Footer);
        }
        else
        {
            Footer.Text = text;
        }
        Layout();
    }

    /// <summary>重新布局所有子控件</summary>
    public void ReLayout()
    {
        Layout();
    }

    /// <summary>尺寸变化时以新宽度重建内容布局</summary>
    public override void OnResize(int newParentW, int newParentH)
    {
        Width = newParentW;
        BuildContent(newParentW); // 重建整个控件树以适应新宽度
    }
}
