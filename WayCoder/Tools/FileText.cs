using System.Text;

namespace WayCoder.Tools;

/// <summary>
/// 文件文本辅助 —— 读原始字节 + UTF-8 校验 + CRLF 检测/归一化 + 子串计数，收敛 Edit/MultiEdit 等工具重复的样板。
/// </summary>
public static class FileText
{
    /// <summary>
    /// 读文本文件：先取原始字节做 UTF-8 校验 + CRLF 检测，再按 UTF-8 读内容并把 CRLF 归一化为 LF。
    /// 成功返回 null，失败返回「错误：…」统一文案。输出 <paramref name="content"/>（归一化后）与
    /// <paramref name="hasCrlf"/>（原始是否为 CRLF）。
    /// </summary>
    public static string? ReadUtf8File(string path, out string content, out bool hasCrlf)
    {
        content = "";
        hasCrlf = false;
        byte[] raw;
        try { raw = File.ReadAllBytes(path); }
        catch { return $"错误：无法读取 {path}"; }

        try { _ = new UTF8Encoding(false, true).GetString(raw); }
        catch { return $"错误：{path} 不是 UTF-8 文本文件"; }

        hasCrlf = raw.AsSpan().IndexOf("\r\n"u8) >= 0;
        content = File.ReadAllText(path, Encoding.UTF8);
        if (hasCrlf) content = content.Replace("\r\n", "\n");
        return null;
    }

    /// <summary>子串在文本中出现次数（非重叠、区分大小写）。
    ///
    /// EditFileTool 与 MultiEditTool 各有一份**逐字相同**的私有实现 —— 而它是「唯一子串匹配」
    /// 这一核心机制的合法性判据（0 次 = 找不到、≥2 次 = 要求更多上下文），判据分家迟早一处改一处漏。</summary>
    public static int CountOccurrences(string text, string substring)
    {
        if (string.IsNullOrEmpty(substring)) return 0;
        int count = 0, idx = 0;
        while ((idx = text.IndexOf(substring, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += substring.Length;
        }
        return count;
    }
}
