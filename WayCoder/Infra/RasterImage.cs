namespace WayCoder.Infra;

/// <summary>
/// 通用 RGBA 位图：解码结果的公共载体 + 像素采样 API（供非 vision 模型「看图」）。
/// 零反射、零依赖、AOT 安全。
/// </summary>
public sealed class RasterImage
{
    public int Width { get; }
    public int Height { get; }
    public byte[] Rgba { get; } // 每像素 4 字节，RGBA 顺序

    public RasterImage(int width, int height, byte[] rgba)
    {
        if (width <= 0 || height <= 0) throw new ArgumentException("宽高必须为正整数");
        // 用 long 计算防整数溢出：width*height*4 若按 int 会溢出为负，绕过长度检查。
        long required = (long)width * height * 4;
        if (rgba == null || rgba.Length < required) throw new ArgumentException("像素缓冲长度不足");
        if (required > int.MaxValue) throw new ArgumentException("图像尺寸过大（像素缓冲超 2GB）");
        Width = width; Height = height; Rgba = rgba;
    }

    /// <summary>取指定像素 ARGB；越界返回 0。</summary>
    public uint ColorAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return 0;
        int i = (y * Width + x) * 4;
        return ((uint)Rgba[i + 3] << 24) | ((uint)Rgba[i] << 16) | ((uint)Rgba[i + 1] << 8) | Rgba[i + 2];
    }

    /// <summary>取指定像素十六进制 #rrggbb（越界返回 #000000）。</summary>
    public string HexAt(int x, int y)
    {
        uint c = ColorAt(x, y);
        return "#" + ((c >> 16) & 0xFF).ToString("x2") + ((c >> 8) & 0xFF).ToString("x2") + (c & 0xFF).ToString("x2");
    }

    /// <summary>批量点采样，返回 #rrggbb 列表。</summary>
    public string[] SamplePoints(IReadOnlyList<(int x, int y)> points)
    {
        var r = new string[points.Count];
        for (int i = 0; i < points.Count; i++) r[i] = HexAt(points[i].x, points[i].y);
        return r;
    }

    /// <summary>均匀网格采样（缩略图式「看图」），返回 cols×rows 个 #rrggbb。</summary>
    public string[] SampleGrid(int cols, int rows)
    {
        if (cols <= 0 || rows <= 0) return Array.Empty<string>();
        // 防整数溢出：cols*rows 与 cx*Width 均按 int 相乘，超大网格溢出为负（new string[负] 抛异常或分配数十 GB）
        if ((long)cols * rows > int.MaxValue) throw new ArgumentException("网格采样规模过大");
        var r = new string[cols * rows];
        int idx = 0;
        for (int ry = 0; ry < rows; ry++)
            for (int cx = 0; cx < cols; cx++)
            {
                int x = (int)((long)cx * Width / cols);
                int y = (int)((long)ry * Height / rows);
                r[idx++] = HexAt(x, y);
            }
        return r;
    }
}
