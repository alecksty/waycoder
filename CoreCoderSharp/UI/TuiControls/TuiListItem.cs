namespace CoreCoderSharp.UI.Controls;

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

    public TuiListItem()
    {
        Width = 60;
    }

    /// <summary>从角色和内容构建完整列表项</summary>
    public TuiListItem(string role, string content, int maxWidth = 80)
    {
        Role = role;
        MarkdownContent = content;
        Width = maxWidth;
        BuildContent(maxWidth);
    }

    /// <summary>构建内部控件树</summary>
    public void BuildContent(int maxWidth)
    {
        Clear();
        int innerW = maxWidth - PaddingLeft - PaddingRight;

        // ── Header 行: Icon + Role + Time ──
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
            Fg = Role switch { "user" => 32, "assistant" => 36, "system" => 33, _ => 37 }
        };
        header.Add(RoleLabel);

        TimeLabel = new TuiLabel(DateTime.Now.ToString("HH:mm"))
        {
            Width = 8,
            Height = 1,
            Fg = 90 // Dim
        };
        header.Add(TimeLabel);

        header.Layout();
        Add(header);

        // ── Body: Markdown 正文 ──
        Body = TuiMarkdown.Create(MarkdownContent, Role, innerW);
        Body.Width = innerW;
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
                Fg = 90
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
        foreach (var child in Children)
        {
            child.X = PaddingLeft;
            child.Width = Math.Min(child.Width, Width - PaddingLeft - PaddingRight);
        }
        Layout();
    }

    /// <summary>尺寸变化时以新宽度重建内容布局</summary>
    public override void OnResize(int newParentW, int newParentH)
    {
        Width = newParentW;
        BuildContent(newParentW); // 重建整个控件树以适应新宽度
    }
}
