namespace WayCoder.UI.TuiControls;

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

    /// <summary>角色图标</summary>
    public TuiIcon Icon { get; set; } = null!;

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
    public HAlign ContentAlign { get; set; } = HAlign.Left;

    public TuiListItem()
    {
        Width = 60;
    }

    /// <summary>从角色和内容构建完整列表项</summary>
    public TuiListItem(string role, string content, int maxWidth = 80, bool continuation = false, bool isPlainText = false, HAlign contentAlign = HAlign.Left)
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
            RoleLabel.Fg = Role switch
            {
                "user" => TuiTheme.Current.ChatUserFg,
                "assistant" => TuiTheme.Current.ChatAssistantFg,
                "system" => TuiTheme.Current.ChatSystemFg,
                _ => TuiTheme.Current.ControlFg
            };
        if (TimeLabel != null) TimeLabel.Fg = TuiTheme.Current.ChatTimeFg;
        if (Footer != null) Footer.Fg = TuiTheme.Current.ChatFooterFg;
        Body?.Invalidate(); // 正文缓存了旧主题色，标记重解析
        MarkDirty();
    }

    /// <summary>构建内部控件树</summary>
    public void BuildContent(int maxWidth)
    {
        Clear();
        int innerW = maxWidth - PaddingLeft - PaddingRight;

        // ── Header 行: Icon + Role + Time（续接消息/嵌套子消息跳过）──
        if (!Continuation && Indent == 0)
        {
            var header = new TuiHBox
            {
                Width = innerW,
                Height = 1,
                ChildVAlign = VAlign.Middle
            };

            Icon = Role switch
            {
                "user" => TuiIcon.User(),
                "assistant" => TuiIcon.Assistant(),
                "system" => TuiIcon.System(),
                "tool" => TuiIcon.Tool(),
                _ => TuiIcon.Info()
            };
            header.Add(Icon);

            var roleName = Role switch
            {
                "user" => "You",
                "assistant" => "Assistant",
                "system" => "System",
                "tool" => "Tool",
                _ => Role
            };
            RoleLabel = new TuiLabel(roleName)
            {
                Width = 12,
                Height = 1,
                Fg = Role switch { "user" => TuiTheme.Current.ChatUserFg, "assistant" => TuiTheme.Current.ChatAssistantFg, "system" => TuiTheme.Current.ChatSystemFg, _ => TuiTheme.Current.ControlFg }
            };
            header.Add(RoleLabel);

            TimeLabel = new TuiLabel(DateTime.Now.ToString("HH:mm"))
            {
                Width = 8,
                Height = 1,
                Fg = TuiTheme.Current.ChatTimeFg // Dim
            };
            header.Add(TimeLabel);

            header.Layout();
            Add(header);
        }
        else
        {
            // 续接消息占位：空 Icon/Name/Time（仅用于布局兼容）
            Icon = TuiIcon.System();
            RoleLabel = new TuiLabel("") { Width = 0, Height = 1 };
            TimeLabel = new TuiLabel("") { Width = 0, Height = 1 };
        }

        // ── Body: Markdown 正文，Padding.Left = 2 格对齐标题；嵌套子消息额外左缩进 ──
        Body = TuiMarkdown.Create(MarkdownContent, Role, innerW, IsPlainText);
        Body.Width = innerW;
        Body.ContentAlign = ContentAlign;
        Body.Padding = new EdgeInsets(0, 0, 0, 2 + Indent * 2);
        Add(Body);

        // ── 重新计算整体高度 ──
        Layout();
    }

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
