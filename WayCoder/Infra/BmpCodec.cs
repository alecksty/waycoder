namespace WayCoder.Infra;

/// <summary>
/// 手搓 BMP 编解码（BI_RGB 无压缩）。编码 24 位 BGR 自底向上；解码 24/32 位、自底/自顶。
/// 零反射、零依赖、AOT 安全、跨平台。
/// </summary>
public static class BmpCodec
{
    /// <summary>解码像素数上限（防不可信宽高字段导致 OOM），约 25MP。</summary>
    private const int MaxPixels = 25_000_000;

    public static byte[] Encode(RasterImage img)
    {
        if (img == null) throw new ArgumentNullException(nameof(img));
        int width = img.Width, height = img.Height;
        // 防整数溢出：width*3、rowSize*height 均可能溢出 int（如 10 万×10 万）
        if (width <= 0 || height <= 0) throw new ArgumentException("宽高必须为正整数");
        if ((long)width * height > MaxPixels) throw new ArgumentException("图像尺寸过大");
        int rowSize = ((width * 3 + 3) / 4) * 4; // 4 字节对齐
        int imageSize = rowSize * height;
        int dataOffset = 14 + 40;
        int fileSize = dataOffset + imageSize;
        var buf = new byte[fileSize];
        buf[0] = (byte)'B'; buf[1] = (byte)'M';
        WriteI32(buf, 2, fileSize);
        WriteI32(buf, 10, dataOffset);
        WriteI32(buf, 14, 40); // BITMAPINFOHEADER 大小
        WriteI32(buf, 18, width);
        WriteI32(buf, 22, height); // 正值 = 自底向上
        WriteU16(buf, 26, 1);      // planes
        WriteU16(buf, 28, 24);     // bpp
        WriteI32(buf, 30, 0);      // BI_RGB
        WriteI32(buf, 34, imageSize);

        int p = dataOffset;
        for (int y = height - 1; y >= 0; y--)
        {
            int rowStart = p;
            for (int x = 0; x < width; x++)
            {
                int si = (y * width + x) * 4;
                buf[p++] = img.Rgba[si + 2]; // B
                buf[p++] = img.Rgba[si + 1]; // G
                buf[p++] = img.Rgba[si];     // R
            }
            while (p - rowStart < rowSize) buf[p++] = 0;
        }
        return buf;
    }

    public static RasterImage Decode(byte[] data)
    {
        if (data == null || data.Length < 54) throw new FormatException("BMP 数据过短");
        if (data[0] != 'B' || data[1] != 'M') throw new FormatException("非法 BMP 签名");
        int dataOffset = ReadI32(data, 10);
        if (dataOffset < 0 || dataOffset >= data.Length) throw new FormatException("BMP 像素数据偏移越界");
        int dibSize = ReadI32(data, 14);
        if (dibSize < 40) throw new FormatException("不支持的 DIB 头");
        int width = ReadI32(data, 18);
        int height = ReadI32(data, 22);
        int bpp = ReadU16(data, 28);
        int compression = ReadI32(data, 30);
        if (compression != 0) throw new FormatException("仅支持 BI_RGB 无压缩");
        if (bpp != 24 && bpp != 32) throw new FormatException("仅支持 24/32 位 BMP");
        if (width <= 0 || height == 0) throw new FormatException("非法宽高");

        bool topDown = height < 0;
        long absHeight = Math.Abs((long)height);
        if (absHeight > int.MaxValue) throw new FormatException("非法宽高"); // height == int.MinValue 时 |height| 超出 int 范围
        int h = (int)absHeight;
        if ((long)width * h > MaxPixels) throw new FormatException("BMP 尺寸过大");
        int bytesPerPixel = bpp / 8;
        int rowSize = ((width * bytesPerPixel + 3) / 4) * 4;
        var rgba = new byte[width * h * 4];
        for (int ry = 0; ry < h; ry++)
        {
            int y = topDown ? ry : h - 1 - ry;
            int rowOff = dataOffset + ry * rowSize;
            if (rowOff + rowSize > data.Length) throw new FormatException("像素数据越界");
            for (int x = 0; x < width; x++)
            {
                int si = rowOff + x * bytesPerPixel;
                byte b = data[si], g = data[si + 1], r = data[si + 2];
                byte a = 255; // 32 位 BI_RGB 第 4 字节为保留位（XRGB，常为 0），非 alpha；读它会把图解码成全透明
                int di = (y * width + x) * 4;
                rgba[di] = r; rgba[di + 1] = g; rgba[di + 2] = b; rgba[di + 3] = a;
            }
        }
        return new RasterImage(width, h, rgba);
    }

    static void WriteI32(byte[] b, int off, int v)
    {
        b[off] = (byte)v; b[off + 1] = (byte)(v >> 8); b[off + 2] = (byte)(v >> 16); b[off + 3] = (byte)(v >> 24);
    }
    static void WriteU16(byte[] b, int off, int v) { b[off] = (byte)v; b[off + 1] = (byte)(v >> 8); }
    static int ReadI32(byte[] b, int off) => b[off] | (b[off + 1] << 8) | (b[off + 2] << 16) | (b[off + 3] << 24);
    static int ReadU16(byte[] b, int off) => b[off] | (b[off + 1] << 8);
}
