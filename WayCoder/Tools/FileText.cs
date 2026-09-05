using System.Text;

namespace WayCoder.Tools;

/// <summary>
/// 文件文本读取辅助 —— 读原始字节 + UTF-8 校验 + CRLF 检测/归一化，收敛 Edit/MultiEdit 等工具重复的读取样板。
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
}
