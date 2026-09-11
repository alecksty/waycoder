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

    /// <summary>模板根视图（chat-item.tui 的 VBox，存引用供 resize 复用，避免重新 LoadResource）。</summary>
    private TuiVBox? _root;

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

    /// <summary>Shell/命令输出块：每行加 │ 竖线前缀，等宽呈现（模拟终端滚动区）。</summary>
    public bool IsShellBlock { get; set; }

    /// <summary>错误模式：正文使用红色渲染（用于工具错误输出）。</summary>
    public bool IsError { get; set; }

    /// <summary>嵌套层级（0=顶层；>0 时作为子消息续接无角色头并左缩进）</summary>
    public int Indent { get; set; }

    /// <summary>内容横向对齐（默认左对齐）</summary>
    public EHAlign ContentAlign { get; set; } = EHAlign.Left;

    public TuiListItem()
    {
        Width = 60;
    }

    /// <summary>从角色和内容构建完整列表项</summary>
    public TuiListItem(string role, string content, int maxWidth = 80, bool continuation = false, bool isPlainText = false, EHAlign contentAlign = EHAlign.Left, bool isShellBlock = false, bool isError = false)
    {
        Role = role;
        MarkdownContent = content;
        Width = maxWidth;
        Continuation = continuation;
        IsPlainText = isPlainText;
        ContentAlign = contentAlign;
        IsShellBlock = isShellBlock;
        IsError = isError;
        // shellBlock/isError 必须在 BuildContent 前生效：否则 Body 首次 EnsureParsed 用的是默认 false，
        // 外部再赋值会触发二次重解析（每次 bash 消息双次解析、首解析白费）。
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
        _root = root;
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
        Body.IsShellBlock = IsShellBlock;
        Body.IsError = IsError;
        Body.Width = innerW;
        Body.MaxWidth = innerW;
        Body.ContentAlign = ContentAlign;
        Body.Padding = new EdgeInsets(0, 0, 0, 2 + Indent * 2);
        Body.EnsureParsed();

        Add(root);
        Layout();
    }

    /// <summary>角色显示名 —— 单一真源见 <see cref="ChatRoleStyle"/>。</summary>
    private static string RoleName(string role) => ChatRoleStyle.DisplayName(role);

    /// <summary>角色文字色 —— 单一真源见 <see cref="ChatRoleStyle"/>。
    /// 此前这里把 user/system 硬编码成亮白，使主题的 ChatUserFg/ChatSystemFg 成了无人读的死键。</summary>
    private static int RoleColor(string role) => ChatRoleStyle.Fg(role);

    /// <summary>角色图标色 —— 单一真源见 <see cref="ChatRoleStyle"/>。</summary>
    private static int IconColor(string role) => ChatRoleStyle.IconFg(role);

    /// <summary>
    /// 更新 Markdown 内容（用于流式追加）。超 <see cref="Global.MaxSingleMessageChars"/> 保留尾部窗口 + 滚动标记，
    /// 每次追加滚动更新（旧内容被挤出，始终显示最新内容——思考/工具输出滚动可见），
    /// 防止一条超长流式消息把渲染项（MarkdownContent/Body.Content）与解析结果无限撑爆。
    ///
    /// 性能：只追加 + 标脏 + 失效解析缓存，不做同步 <c>EnsureParsed()</c>/<c>ReLayout()</c> ——
    /// 重解析交给 <see cref="TuiMarkdown.EnsureParsed"/> 的渲染帧惰性调用（<c>ChatScreen.FlushStreamingLayout</c>
    /// 在每帧统一 flush），把同帧内到达的多个流式 delta 合并成一次解析，消除每 delta 全量重解析 + 全量重布局。
    /// </summary>
    public void AppendContent(string delta)
    {
        int max = Global.MaxSingleMessageChars;
        if (max > 0 && MarkdownContent.Length + delta.Length > max)
        {
            var combined = MarkdownContent + delta;
            var tail = ContextManager.TruncateTailByRunes(combined, max);
            var capped = $"… 已截断（显示最近内容，旧内容滚动省略）…\n{tail}";
            MarkdownContent = capped;
            Body.Content = capped;
        }
        else
        {
            MarkdownContent += delta;
            Body.Content += delta;
        }
        Body.Invalidate();          // 解析缓存失效（_parsed=false），重解析交给下一渲染帧
        Body.MarkDirty();           // 标脏正文叶子，增量渲染才重画新内容
        Body.Width = Width - PaddingLeft - PaddingRight;
        Body.MaxWidth = Width - PaddingLeft - PaddingRight;
        MarkDirty();                // 条目自身标脏（重排/滚动后需重绘），同时唤醒渲染帧
    }

    /// <summary>
    /// 仅按新宽度重排内容（resize / 侧栏开合路径）：复用已有模板控件树，不重新 LoadResource("chat-item.tui")、
    /// 不重建 Header/Body —— 只更新宽度 + 用新宽度同步重解析正文。相比 <see cref="BuildContent"/>
    /// 省去模板文件读取 + XML 解析 + 控件树构建（N 条消息 × 每次 resize 的成本）。
    /// </summary>
    public void ResizeContent(int newParentW)
    {
        Width = newParentW;
        int innerW = Math.Max(1, newParentW - PaddingLeft - PaddingRight);
        if (_root != null) _root.Width = innerW;
        if (Body != null)
        {
            Body.Width = innerW;
            Body.MaxWidth = innerW;
            Body.Invalidate();      // 失效解析缓存 → EnsureParsed 用新宽度重解析
            Body.EnsureParsed();
            Body.MarkDirty();
        }
        Layout();                   // 条目高度按新正文行数重算
        MarkDirty();
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
        ResizeContent(newParentW); // 复用模板控件树只改宽度（免重复 LoadResource/XML 解析）
    }
}
