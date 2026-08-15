// =============================================================
// Cjk.cs —— CJK 文本宽度计算
//
// 终端中 CJK 全角字符（中文、日文假名、韩文、全角标点）占据 2 个
// 单元格。此工具函数用于布局与渲染，保证多列对齐正确。
// 零依赖，AOT 安全，不涉及反射。
// =============================================================

namespace QBasic.Tui;

/// <summary>CJK 宽度计算工具。</summary>
public static class Cjk
{
    /// <summary>判断 Unicode 字符是否为全角（宽 2 列）。</summary>
    public static bool IsWide(char c)
    {
        int v = c;
        // CJK 统一表意文字
        if (v >= 0x2E80 && v <= 0x9FFF) return true;
        // 全角标点 / 日文假名
        if (v >= 0x3000 && v <= 0x303F) return true;
        if (v >= 0x3040 && v <= 0x30FF) return true;
        // 扩展 A/B/C/D
        if (v >= 0x3400 && v <= 0x4DBF) return true;
        if (v >= 0xF900 && v <= 0xFAFF) return true;
        if (v >= 0xFF01 && v <= 0xFF60) return true; // 全角半角形态
        if (v >= 0xFFE0 && v <= 0xFFE6) return true;
        if (v >= 0x20000 && v <= 0x2FA1F) return true; // 扩展 B-D
        return false;
    }

    /// <summary>计算字符串占用终端列数（全角计 2，其余计 1）。</summary>
    public static int Width(string? s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        int w = 0;
        foreach (char c in s) w += IsWide(c) ? 2 : 1;
        return w;
    }

    /// <summary>把字符串按目标列宽裁剪到不超过 maxWidth 列（含省略号），并补齐到恰好 width 列。
    /// 返回 (裁剪后文本, 实际宽度)。</summary>
    public static string Fit(string s, int width)
    {
        if (width <= 0) return "";
        if (Width(s) <= width) return s + new string(' ', width - Width(s));
        // 需要裁剪
        var sb = new System.Text.StringBuilder();
        int w = 0;
        foreach (char c in s)
        {
            int cw = IsWide(c) ? 2 : 1;
            if (w + cw > width) break;
            sb.Append(c);
            w += cw;
        }
        // 留出 1 个省略号位置
        while (Width(sb.ToString()) + 1 > width && sb.Length > 0)
        {
            int last = sb.Length - 1;
            sb.Length = last; // 去掉最后一个字符（可能宽2）
        }
        sb.Append('…');
        int total = Width(sb.ToString());
        if (total < width) sb.Append(new string(' ', width - total));
        return sb.ToString();
    }
}
