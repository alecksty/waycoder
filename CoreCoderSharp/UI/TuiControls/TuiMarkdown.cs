using System.Text;
using OldMd = CoreCoderSharp.UI.TuiMarkdown;

namespace CoreCoderSharp.UI.TuiControls;

/// <summary>
/// Markdown 渲染控件 —— 将 Markdown 文本渲染为格式化的终端输出。
/// 支持标题、代码块（语法高亮）、表格、列表、分割线、内联格式。
/// 作为 TuiListView 的子项使用。
/// </summary>
public class TuiMarkdown : TuiControl
{
    /// <summary>Markdown 源文本</summary>
    public string Content { get; set; } = "";

    /// <summary>Markdown 渲染控件是展示控件，不可获得焦点</summary>
    public override bool CanFocus => false;

    /// <summary>角色（用于默认颜色：user/assistant/system）</summary>
    public string Role { get; set; } = "assistant";

    /// <summary>纯文本模式：逐行渲染，不走 Markdown 解析</summary>
    public bool IsPlainText { get; set; }

    /// <summary>内容横向对齐（默认左对齐，欢迎消息用居中）</summary>
    public HAlign ContentAlign { get; set; } = HAlign.Left;

    /// <summary>最大渲染宽度（自动折行）</summary>
    public int MaxWidth { get; set; } = 80;

    /// <summary>渲染后的行数据</summary>
    private List<List<(string Text, int Fg, int Bg)>> _rendered = [];

    /// <summary>内容是否已解析</summary>
    private bool _parsed;

    private string _lastContent = "";
    private int _lastMaxWidth;

    public TuiMarkdown()
    {
        Height = 1;
        Width = 80;
    }

    public TuiMarkdown(string content, string role = "assistant")
    {
        Content = content;
        Role = role;
        Height = 1;
        Width = 80;
    }

    // ── 渲染 ──

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        // 延迟/增量解析
        EnsureParsed();

        if (_rendered.Count == 0) return;

        // 逐行渲染
        for (int i = 0; i < _rendered.Count; i++)
        {
            int row = absY + i;
            if (row >= ClipBottom) break;
            if (row < ClipTop) continue;

            var segments = _rendered[i];
            int col = absX;

            // 横向居中：计算本行总视觉宽度，偏移到居中位置
            if (ContentAlign == HAlign.Center)
            {
                int totalVw = 0;
                foreach (var (text, _, _) in segments)
                    totalVw += TuiHelper.DisplayWidth(text);
                col += Math.Max(0, (Width - totalVw) / 2);
            }

            foreach (var (text, fg, bg) in segments)
            {
                if (string.IsNullOrEmpty(text)) continue;
                int effFg = fg > 0 ? fg : (Fg > 0 ? Fg : TuiTheme.Current.ControlFg);
                int effBg = bg > 0 ? bg : (Bg > 0 ? Bg : 0);

                WriteAt(sb, row, col, text, effFg, effBg);
                col += TuiHelper.DisplayWidth(text);
            }
        }
    }

    // ── 解析 ──

    public void EnsureParsed()
    {
        int effectiveMaxW = MaxWidth > 0 ? MaxWidth : Width;
        if (_parsed && _lastContent == Content && _lastMaxWidth == effectiveMaxW) return;

        _lastContent = Content;
        _lastMaxWidth = effectiveMaxW;

        if (string.IsNullOrEmpty(Content))
        {
            _rendered = [];
            Height = 0;
            _parsed = true;
            return;
        }

        // 使用旧的静态渲染器生成带色段列表
        _rendered = OldMd.RenderMessage(Content, Role, effectiveMaxW, IsPlainText);
        Height = Math.Max(1, _rendered.Count);
        _parsed = true;
    }

    /// <summary>强制重新解析</summary>
    public override void Invalidate()
    {
        _parsed = false;
    }

    /// <summary>尺寸变化时重新以新宽度解析</summary>
    public override void OnResize(int newParentW, int newParentH)
    {
        MaxWidth = newParentW - 2; // 减去边距
        Width = MaxWidth;
        _parsed = false;           // 触发重新解析
    }

    // ── 静态缓存（避免重复解析） ──

    /// <summary>快速创建 Markdown 控件</summary>
    public static TuiMarkdown Create(string content, string role = "assistant", int maxWidth = 80, bool plainText = false)
    {
        var md = new TuiMarkdown(content, role)
        {
            MaxWidth = maxWidth,
            Width = maxWidth,
            IsPlainText = plainText
        };
        md.EnsureParsed();
        return md;
    }
}
