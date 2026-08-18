using System.Windows.Media;
using WayCoder.UI.Shared.Terminal;
// 消除与 WayCoder.UI.Shared.Terminal.Color 的类型歧义
using Color = System.Windows.Media.Color;

namespace WayCoder.Preview.Render;

/// <summary>
/// ANSI 色码 → WPF Color/Brush 映射。
/// 覆盖：16 色 VGA（30-37/40-47/90-97/100-107）、256 色 xterm（16-255 立方体+灰阶）、TrueColor（≥0x1000000）。
/// </summary>
public static class AnsiToColor
{
    /// <summary>ANSI 色码 → Color。0 = 透明（用面板默认底色）。</summary>
    public static Color ToColor(int code)
    {
        if (code <= 0) return Colors.Transparent;
        if (code >= 0x1000000)
        {
            var (r, g, b) = AnsiTty.DecodeRgb(code);
            return Color.FromRgb((byte)r, (byte)g, (byte)b);
        }
        if (code is >= 16 and <= 255) return Xterm256(code);
        return Vga(code);
    }

    /// <summary>取（可缓存的）实心画刷。</summary>
    public static SolidColorBrush GetBrush(int code, Dictionary<int, SolidColorBrush> cache)
    {
        if (!cache.TryGetValue(code, out var brush))
        {
            brush = new SolidColorBrush(ToColor(code));
            brush.Freeze();
            cache[code] = brush;
        }
        return brush;
    }

    // ── 16 色 ── 标准 xterm 16 色板（现代终端 Windows Terminal / xterm 默认，比经典 VGA 更亮）
    private static readonly (byte R, byte G, byte B)[] VgaTable =
    [
        (0, 0, 0), (205, 0, 0), (0, 205, 0), (205, 205, 0), (0, 0, 238), (205, 0, 205), (0, 205, 205), (229, 229, 229),
        (127, 127, 127), (255, 0, 0), (0, 255, 0), (255, 255, 0), (92, 92, 255), (255, 0, 255), (0, 255, 255), (255, 255, 255),
    ];

    private static Color Vga(int code)
    {
        int idx = code switch
        {
            >= 30 and <= 37 => code - 30,
            >= 40 and <= 47 => code - 40,
            >= 90 and <= 97 => code - 90 + 8,
            >= 100 and <= 107 => code - 100 + 8,
            _ => -1,
        };
        if (idx >= 0 && idx < VgaTable.Length)
        {
            var (r, g, b) = VgaTable[idx];
            return Color.FromRgb(r, g, b);
        }
        return Colors.Transparent;
    }

    // ── 256 色 xterm ──
    private static readonly byte[] CubeLevels = [0, 95, 135, 175, 215, 255];

    private static Color Xterm256(int code)
    {
        if (code is >= 16 and <= 231)
        {
            int n = code - 16;
            int r = n / 36, g = (n / 6) % 6, b = n % 6;
            return Color.FromRgb(CubeLevels[r], CubeLevels[g], CubeLevels[b]);
        }
        if (code is >= 232 and <= 255)
        {
            int v = 8 + 10 * (code - 232);
            return Color.FromRgb((byte)v, (byte)v, (byte)v);
        }
        return Colors.Transparent;
    }
}
