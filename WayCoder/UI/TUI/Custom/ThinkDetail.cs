using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.TUI;         // TuiMarkup（命名空间是大写 TUI，与 WayCoder.UI.Tui 不是同一个）
using WayCoder.UI.TUI.Base;    // TuiScreen / TuiScrollView
// 别名：`WayCoder.UI.Tui`（本类的父命名空间）里另有一个同名静态类 TuiMarkdown（旧的文本渲染器），
// 不加别名会把 new TuiMarkdown(...) 解析到那个静态类上
using MdControl = WayCoder.UI.Tui.Controls.TuiMarkdown;
using TuiMdRenderer = WayCoder.UI.Tui.TuiMarkdown; // 旧的静态文本渲染器（折行工具在它上面）

namespace WayCoder.UI.Tui.Custom;

/// <summary>
/// 思考详情窗口 —— 聊天流里思考只留一行「💭 已思考 N 秒」，正文（可能上万字符）不参与
/// 渲染与布局；点那一行才在这里滚动查看。
///
/// 与 Web 的思考浮层（<c>app.js</c> <c>showThinkDetail</c>）、MAUI 的 <c>ReasoningDetailPage</c>
/// 是同一形态：**正文只在用户主动要求时才渲染**，这就是折叠带来的性能收益本身。
/// 正文进窗口前先剥掉中间格式标记（<c>«red»…«/»</c>）—— 详情里要的是推理原文，
/// 不是它的着色（对齐 Web 的 <c>stripMarkupTags</c>）。
/// </summary>
public static class ThinkDetail
{
    /// <summary>弹出思考详情窗口。</summary>
    /// <param name="screen">宿主屏幕（决定窗口栈与重绘）</param>
    /// <param name="reasoning">思考正文（原始中间格式）</param>
    /// <param name="seconds">已思考秒数（0 = 未知，标题退回中性文案）</param>
    public static void Show(TuiScreen screen, string reasoning, int seconds)
    {
        // 尺寸：别逼近全屏 —— 正文可滚动，窗口不必为显示全部而顶满（与 DiffPreview 同一取法）
        int winW = Math.Clamp((int)(Tty.Cols * 0.75), 40, Math.Max(40, Tty.Cols - 4));
        int winH = Math.Clamp((int)(Tty.Rows * 0.7), 8, Math.Max(8, Tty.Rows - 4));

        var res = TuiMarkup.LoadResource("dialogs/thinkdetail.tui");
        var win = res.Window ?? throw new InvalidOperationException("thinkdetail.tui 根应为 Dialog");
        win.Title = seconds >= 1 ? $"💭 已思考 {seconds} 秒" : "💭 思考内容";
        win.Width = winW;
        win.Height = winH;

        var scroll = res.Find<TuiScrollView>("thinkScroll")
                     ?? throw new InvalidOperationException("thinkdetail.tui 缺少 id=\"thinkScroll\"");

        string raw = StripMarkup(reasoning ?? "");

        var md = new MdControl
        {
            Role = "think",      // 与聊天区折叠行同色（暗灰）
            IsPlainText = true,  // 推理是原始文本：走 Markdown 解析既慢，又会被正文里的符号带偏
            Focused = true,
        };

        void Refit()
        {
            // 必须**自己折行**：TuiMarkdown 的纯文本分支（AddContentLine）不折行，
            // 超宽行会被 WriteAt 的右侧裁剪成「…」，推理长段落就只剩半句。
            int w = Math.Max(20, win.Width - 4); // 减两侧边框 + 2 列余量
            md.Width = w;
            md.MaxWidth = w;
            md.Content = string.Join("\n", TuiMdRenderer.WrapText(raw, w));
            md.Invalidate();
            md.EnsureParsed();
            scroll.Layout();
            scroll.ClampScroll();
            scroll.MarkDirty();
        }

        scroll.Add(md);
        win.XScale = 0;             // 宽度唯一来源是内容/屏幕，别让 TuiWindow.OnResize 的比例缩放覆盖
        win.OnResizeContent = Refit; // 终端缩放时正文按新宽重排（否则只有外框跟着动）
        Refit();

        win.RegisterShortcut(ConsoleKey.Escape, () => win.Close());
        win.RegisterShortcut(ConsoleKey.Enter, () => win.Close());
        UxHelper.Wire(res, "btnClose", () => win.Close());

        screen.ShowWindow(win);
        // ShowWindow 会把窗口钳进屏幕（ClampTree），真实宽度此时才定下来 ——
        // 按钳后的宽度再折一次行，否则长段落仍会超出内容区被裁成「…」。
        Refit();
        win.MarkDirty();
    }

    /// <summary>
    /// 剥掉中间格式标记（<c>«red»</c> / <c>«/»</c> …）只留文字。
    /// 规则与 Web 的 <c>stripMarkupTags</c>（<c>s.replace(/«[^»]*»/g, '')</c>）一致：
    /// 从 <c>«</c> 到其后第一个 <c>»</c> 整段丢弃；未闭合的 <c>«</c> 原样保留（正则同样匹配不到）。
    /// </summary>
    public static string StripMarkup(string text)
    {
        if (text.Length == 0 || text.IndexOf('«') < 0) return text;
        var sb = new StringBuilder(text.Length);
        int i = 0;
        while (i < text.Length)
        {
            int open = text.IndexOf('«', i);
            if (open < 0)
            {
                sb.Append(text, i, text.Length - i);
                break;
            }
            if (open > i) sb.Append(text, i, open - i);
            int close = text.IndexOf('»', open + 1);
            if (close < 0)
            {
                sb.Append(text, open, text.Length - open); // 未闭合：原样留着
                break;
            }
            i = close + 1;
        }
        return sb.ToString();
    }
}
