using System.Net;
using System.Text.RegularExpressions;

namespace WayCoder.Infra;

/// <summary>
/// HTML → 纯文本公共转换（收敛 DocTool/FetchTool/WebSearchTool/LegacyOffice 四处重复的 StripHtml）。
/// </summary>
public static class HtmlText
{
    /// <summary>stripNoise=true 时整段移除的噪音元素（脚本/样式/导航/页脚等；FetchTool/DocTool 语义的超集）。</summary>
    private static readonly string[] NoiseElements =
        ["script", "style", "nav", "footer", "header", "aside", "noscript", "iframe", "svg"];

    /// <summary>
    /// 把 HTML 转纯文本。
    /// stripNoise=true（默认）：去 script/style/nav/footer/header/aside 等噪音元素整段 + 块级标签换行
    ///   （div/p/h[1-6]/li/tr/br/hr/section/article）+ 去标签 + HtmlDecode + 折叠空白（至多保留一个空行）。
    /// stripNoise=false（Web 搜索摘要用最简）：仅去标签 + 折叠所有空白为单空格，不做实体解码
    ///   （WebSearchTool 调用方已对片段单独 HtmlDecode，此处再解会双重解码）。
    /// maxChars&gt;0 时按字符截断（Rune 安全，防 CJK/emoji 代理对被切半）。
    /// </summary>
    public static string StripHtml(string html, bool stripNoise = true, int maxChars = 0)
    {
        if (string.IsNullOrEmpty(html)) return "";
        var t = html;

        if (stripNoise)
        {
            t = t.Replace("\r\n", "\n");
            foreach (var el in NoiseElements)
                t = Regex.Replace(t, $@"<{el}[^>]*>.*?</{el}>", "",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);
            // 块级标签 → 换行（保留段落/列表/表格行结构）
            t = Regex.Replace(t, @"</?(div|p|h[1-6]|li|tr|br|hr|section|article)[^>]*/?>", "\n",
                RegexOptions.IgnoreCase);
            // 去剩余标签
            t = Regex.Replace(t, @"<[^>]+>", " ");
            t = WebUtility.HtmlDecode(t);
            // 折叠横向空白 + 去掉行首/行尾残留空格
            t = Regex.Replace(t, @"[ \t]+", " ");
            t = Regex.Replace(t, @"[ \t]+(?=\n)", "");
            t = Regex.Replace(t, @"(?<=\n)[ \t]+", "");
            // 连续空行（3+ 换行）→ 至多一个空行
            t = Regex.Replace(t, @"\n{3,}", "\n\n");
        }
        else
        {
            t = Regex.Replace(t, @"<[^>]+>", "");
            t = Regex.Replace(t, @"\s+", " ");
        }

        t = t.Trim();
        if (maxChars > 0 && t.Length > maxChars)
            t = ContextManager.TruncateByRunes(t, maxChars);
        return t;
    }
}
