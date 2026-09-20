using Avalonia.Controls.Documents;
using Avalonia.Media;
using WayCoder.UI.Shared;   // MarkdownParser —— 行内格式的共享真源
// ⚠ 用**类型别名**而不是 `using WayCoder.UI.Shared.Terminal;` —— 那个命名空间里也有一个
//    `Color`，整包导入会和 `Avalonia.Media.Color` 撞成 CS0104（MAUI 那边全限定写也是同一原因）。
using AnsiTty = WayCoder.UI.Shared.Terminal.AnsiTty;

namespace WayCoder.UI.Gui;

/// <summary>
/// 行内格式渲染：把 <c>«tag»…«/»</c> 中间格式 + Markdown 行内语法渲染为 Avalonia Inline 列表。
///
/// ⚠ **一律走共享 <see cref="MarkdownParser.ParseInline"/>**（与 TUI / MAUI / Web 同源）。
/// 此前这里是**自建的第四份行内口径**（自己一套 `«»` 标签表 + 开关式 `**`/`` ` `` 解析），
/// 三个后果：
///   ① 标签集最小 ⇒ `«fg:#rrggbb»` 真彩色与 `«strike»` **字面泄漏**成标签文本
///      （而 TUI/MAUI/Web 三端都认十六进制）；
///   ② `**` / `` ` `` 是**开关式**的 ⇒ 正文里一个落单的反引号会被**直接吞掉**；
///   ③ 下划线强调 `_x_` / `__x__`、反斜杠转义 `\*`、HTML 实体 `&amp;`、自动链接一概不认。
/// 收敛之后这三条一次性消失，且**补一个语法四端同时生效**。
/// </summary>
public static class MarkdownInlines
{
    private static readonly FontFamily MonoFont = GuiFonts.Mono;

    /// <summary>把一行文本渲染为 Inline 列表。</summary>
    /// <param name="defaultFg">正文色 —— 由调用方传**主题色**（切浅色主题时否则文字仍是深色主题的浅色、看不见）</param>
    /// <param name="dimFg">淡色（`«dim»` 用）</param>
    public static List<Inline> RenderInline(string text, Color? defaultFg = null, Color? dimFg = null)
    {
        var fg = defaultFg ?? GuiColors.TextColor;
        var dim = dimFg ?? GuiColors.DimColor;
        var fgBrush = new SolidColorBrush(fg);
        var result = new List<Inline>();

        foreach (var (seg, color, bg) in MarkdownParser.ParseInline(text))
        {
            if (seg.Length == 0) continue;

            // ⚠ **先剥 BoldFlag 再解析颜色**：0x2000000 是加粗位（AnsiTty.BoldFlag），不剥的话
            //    `«bold»«orange»` 解析出的 `BoldFlag|色码` 会落进「≥0x1000000 ⇒ 当真彩色」那条
            //    分支、解出乱色。MAUI 的 ResolveFg 有同一个坑，两边判据必须同源。
            var code = color & ~AnsiTty.BoldFlag;
            var run = new Run(seg)
            {
                Foreground = code == 2 ? new SolidColorBrush(dim) : SyntaxBrushMap.ForFg(code, fgBrush),
            };

            if ((color & AnsiTty.BoldFlag) != 0 || code == 1)
                run.FontWeight = FontWeight.Bold;

            switch (code)
            {
                case 1: run.FontWeight = FontWeight.Bold; break;
                case 3: run.FontStyle = FontStyle.Italic; break;
                case 4: run.TextDecorations = TextDecorations.Underline; break;
                case 9: run.TextDecorations = TextDecorations.Strikethrough; break;
                case 33: run.FontFamily = MonoFont; break;   // 行内代码用等宽
            }

            if (bg >= 30) run.Background = SyntaxBrushMap.ForBg(bg);
            result.Add(run);
        }

        // 解析器对空文本会给一段 ("",0,0)，上面按 seg.Length==0 跳过 ⇒ 这里补一个空 Run 保持原契约
        if (result.Count == 0) result.Add(new Run("") { Foreground = fgBrush });
        return result;
    }
}
