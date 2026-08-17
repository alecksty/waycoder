using System.IO.Compression;
using System.Text;

namespace WayCoder.Infra;

/// <summary>
/// 手搓 PNG 解码器（AOT 安全，零反射）。与 PngEncoder 对称。
/// 支持 8 位 灰度/RGB/调色板/灰度+alpha/RGBA + 5 种行滤波（None/Sub/Up/Average/Paeth）；
/// Adam7 交错不支持（返回明确错误）；tRNS 仅调色板 alpha 生效。
/// </summary>
public static class PngDecoder
{
    /// <summary>解码像素数上限（防不可信 IHDR 尺寸字段导致 OOM），约 25MP。</summary>
    private const int MaxPixels = 25_000_000;

    public static RasterImage Decode(byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (data.Length < 8) throw new FormatException("PNG 数据过短");
        var sig = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        for (int i = 0; i < 8; i++) if (data[i] != sig[i]) throw new FormatException("非法 PNG 签名");

        int width = 0, height = 0, bitDepth = 0, colorType = 0, interlace = 0;
        var palette = new byte[0];
        var trns = new byte[0];
        var idat = new MemoryStream();

        int off = 8;
        bool ended = false;
        while (off + 8 <= data.Length)
        {
            int len = BE32(data, off);
            if (len < 0) throw new FormatException("PNG chunk 长度非法"); // 负数长度会让 off 回退，造成死循环
            string type = Encoding.ASCII.GetString(data, off + 4, 4);
            off += 8;
            if ((long)off + len > data.Length) throw new FormatException("PNG chunk 越界"); // 用 long 防 len=int.MaxValue 时 off+len 溢出为负、绕过检查
            switch (type)
            {
                case "IHDR":
                    if (len < 13) throw new FormatException("IHDR 长度错误");
                    width = BE32(data, off); height = BE32(data, off + 4);
                    if (width <= 0 || height <= 0) throw new FormatException("非法 PNG 尺寸");
                    if ((long)width * height > MaxPixels) throw new FormatException("PNG 尺寸过大");
                    bitDepth = data[off + 8]; colorType = data[off + 9];
                    interlace = data[off + 12];
                    break;
                case "PLTE": palette = new byte[len]; Array.Copy(data, off, palette, 0, len); break;
                case "tRNS": trns = new byte[len]; Array.Copy(data, off, trns, 0, len); break;
                case "IDAT": idat.Write(data, off, len); break;
                case "IEND": ended = true; break;
            }
            off += len + 4;
            if (ended) break;
        }
        if (width <= 0 || height <= 0) throw new FormatException("缺少 IHDR");
        if (interlace != 0) throw new FormatException("不支持 Adam7 交错 PNG");
        if (bitDepth != 8) throw new FormatException("仅支持 8 位深");

        int channels = colorType switch
        {
            0 => 1, // gray
            2 => 3, // RGB
            3 => 1, // palette index
            4 => 2, // gray + alpha
            6 => 4, // RGBA
            _ => throw new FormatException("不支持的颜色类型 " + colorType),
        };
        int bpp = channels;
        int stride = width * bpp;

        // 期望解压大小 = (每行 stride + 1 filter 字节) * height。解压前先算并限制输出上限：
        // IDAT 内嵌可解压出 GB 级 zlib 流（小文件触发 OOM），先解压后校验会先物化全部内存
        long expected = ((long)stride + 1) * height;
        long maxRaw = expected * 2 + 65536; // 宽松余量（合法 PNG 解压后应精确等于 expected，*2 防误杀）

        byte[] raw;
        idat.Position = 0;
        using (var z = new ZLibStream(idat, CompressionMode.Decompress))
        using (var outMs = new MemoryStream())
        {
            var buffer = new byte[81920];
            int read;
            while ((read = z.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (outMs.Length + read > maxRaw)
                    throw new FormatException("PNG IDAT 解压数据超出预期大小（解压炸弹？）");
                outMs.Write(buffer, 0, read);
            }
            raw = outMs.ToArray();
        }
        var scan = new byte[stride * height];
        var prev = new byte[stride];
        for (int y = 0; y < height; y++)
        {
            int rowOff = y * (stride + 1);
            if (rowOff + stride >= raw.Length) throw new FormatException("像素数据不足");
            int filter = raw[rowOff];
            var cur = new byte[stride];
            Array.Copy(raw, rowOff + 1, cur, 0, stride);
            var line = Unfilter(cur, y > 0 ? prev : null, filter, bpp);
            Array.Copy(line, 0, scan, y * stride, stride);
            prev = line;
        }

        var rgba = new byte[width * height * 4];
        int pi = 0;
        for (int i = 0; i < width * height; i++)
        {
            byte r, g, b, a = 255;
            switch (colorType)
            {
                case 0: r = g = b = scan[i]; break;
                case 2:
                    r = scan[i * 3]; g = scan[i * 3 + 1]; b = scan[i * 3 + 2];
                    break;
                case 3:
                    int idx = scan[i];
                    if (idx * 3 + 2 < palette.Length) { r = palette[idx * 3]; g = palette[idx * 3 + 1]; b = palette[idx * 3 + 2]; }
                    else { r = g = b = 0; }
                    if (idx < trns.Length) a = trns[idx];
                    break;
                case 4: r = g = b = scan[i * 2]; a = scan[i * 2 + 1]; break;
                case 6: r = scan[i * 4]; g = scan[i * 4 + 1]; b = scan[i * 4 + 2]; a = scan[i * 4 + 3]; break;
                default: r = g = b = 0; break;
            }
            rgba[pi++] = r; rgba[pi++] = g; rgba[pi++] = b; rgba[pi++] = a;
        }
        return new RasterImage(width, height, rgba);
    }

    static byte[] Unfilter(byte[] cur, byte[]? prev, int filter, int bpp)
    {
        int n = cur.Length;
        var outBuf = (byte[])cur.Clone();
        for (int i = 0; i < n; i++)
        {
            int raw = outBuf[i];
            int left = i >= bpp ? outBuf[i - bpp] : 0;
            int up = prev != null ? prev[i] : 0;
            int upLeft = prev != null && i >= bpp ? prev[i - bpp] : 0;
            int v = filter switch
            {
                0 => raw,
                1 => raw + left,
                2 => raw + up,
                3 => raw + ((left + up) >> 1),
                4 => raw + Paeth(left, up, upLeft),
                _ => throw new FormatException("非法 filter " + filter),
            };
            outBuf[i] = (byte)(v & 0xFF);
        }
        return outBuf;
    }

    static int Paeth(int a, int b, int c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc) return a;
        if (pb <= pc) return b;
        return c;
    }

    static int BE32(byte[] d, int off) => (d[off] << 24) | (d[off + 1] << 16) | (d[off + 2] << 8) | d[off + 3];
}
