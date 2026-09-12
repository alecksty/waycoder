namespace WayCoder.UI.Shared;

/// <summary>
/// 「这段文本有没有可见内容」的唯一判据 —— 只有空白或**不可见字符**（零宽空格/连接词、BOM、
/// 软连字符、词连接符、控制字符…）的正文气泡**不发**。
///
/// 为什么需要它：LLM 在每段正文开头会送一口换行（思考结束那一下就是 `"«/»\n"`）。渲染端若
/// 光凭「来了一个 token」就建气泡，「模型想完直接调工具」（这一轮没有正文）就会在工具行前留下
/// 一个空气泡 —— 用户实测 Web 端「很多空泡泡，没有任何内容」「只有空格或者不可见字符的泡泡，不发」。
///
/// ⚠ 这张表与 Web 端 `UI/WEB/www/app.js` 的 `isBlankText` 是**同一份**（跨语言只能各写一遍，
/// 改动必须两处同步；自测 SelfTest.Helpers.Infra.cs 的 TestVisibleText 两侧都钉）。
/// 跨端共享：Web 端是 JS，同一规则在 `UI/WEB/www/app.js` 有孪生实现（<c>isBlankText</c>），
/// 两边判据必须一致（Web 侧还多一道「渲染完只剩空标签」的判据 <c>visibleText</c>）；
/// GUI/MAUI 与本类共用（本文件在 UI/Shared/ 下，MAUI 的 Compile Include 覆盖得到）。
/// </summary>
public static class VisibleText
{
    /// <summary>是否存在至少一个可见字符（空白与不可见字符不算）。空/null 一律视为「没有内容」。</summary>
    public static bool HasVisible(string? s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        foreach (var c in s)
            if (IsVisible(c)) return true;
        return false;
    }

    /// <summary>可见字符：既不是空白，也不是那些「占了码位却看不见」的字符。
    /// 注意 .NET 的 <see cref="char.IsWhiteSpace(char)"/> **不含** U+200B / U+FEFF 这几个 ——
    /// 必须显式列出，否则「只有零宽空格」的气泡会被判成有内容。</summary>
    private static bool IsVisible(char c)
    {
        if (char.IsWhiteSpace(c)) return false;
        // 「占了码位却看不见」的字符：char.IsWhiteSpace 只认其中的空白类，其余必须显式列出
        if (c >= '\u0000' && c <= '\u001F') return false;                 // C0 控制字符
        if (c >= '\u007F' && c <= '\u009F') return false;                 // DEL + C1 控制字符
        if (c == '\u00AD') return false;                                  // 软连字符
        if (c == '\u061C') return false;                                  // 阿拉伯字母标记（不可见）
        if (c >= '\u115F' && c <= '\u1160') return false;                 // 谚文填充符
        if (c >= '\u17B4' && c <= '\u17B5') return false;                 // 高棉语固有元音（不可见）
        if (c >= '\u180B' && c <= '\u180E') return false;                 // 蒙古文变体选择符 / 元音分隔符
        if (c >= '\u200B' && c <= '\u200F') return false;                 // 零宽空格/零宽连接词/方向标记
        if (c >= '\u2060' && c <= '\u2064') return false;                 // 词连接符/不可见运算符
        if (c == '\u3164') return false;                                  // 谚文填充符（常被当空白用）
        if (c >= '\uFE00' && c <= '\uFE0F') return false;                 // 变体选择符
        if (c == '\uFEFF') return false;                                  // BOM（多出现在流首）
        if (c == '\uFFA0') return false;                                  // 半宽谚文填充符
        return true;
    }
}
