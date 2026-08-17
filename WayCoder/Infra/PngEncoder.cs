using System.IO.Compression;
using System.Text;

namespace WayCoder.Infra;

/// <summary>
/// 手搓 PNG 编码器（AOT 安全：零反射，不依赖 System.Drawing）。
/// 8-bit RGBA 像素缓冲 → PNG 字节流。DEFLATE 复用 BCL 的 ZLibStream（AOT 安全），
/// 手写 chunk 布局 + CRC32 + IHDR/IDAT/IEND，对标 ScreenshotTool 的手写 PNG 解析。
/// </summary>
public static class PngEncoder
{
    static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    /// <summary>编码像素数上限（防宽高乘积溢出 int / 分配数十 GB），约 25MP。</summary>
    private const int MaxPixels = 25_000_000;

    /// <summary>把 RGBA 像素缓冲编码为 PNG 字节流。rgba 长度须 ≥ width*height*4。</summary>
    public static byte[] Encode(int width, int height, byte[] rgba)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("宽高必须为正整数");
        // 防整数溢出：width*height*4 与 stride*height 可能溢出 int（如 10 万×10 万）
        if ((long)width * height > MaxPixels)
            throw new ArgumentException("图像尺寸过大");
        if (rgba == null || rgba.Length < (long)width * height * 4)
            throw new ArgumentException("像素缓冲长度不足");

        // 每行前置 filter 字节 0（None）
        int stride = width * 4;
        var raw = new byte[(stride + 1) * height];
        for (int y = 0; y < height; y++)
        {
            raw[y * (stride + 1)] = 0;
            Array.Copy(rgba, y * stride, raw, y * (stride + 1) + 1, stride);
        }

        // DEFLATE（zlib 封装，IDAT 所需）
        byte[] compressed;
        using (var ms = new MemoryStream())
        {
            using (var z = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                z.Write(raw, 0, raw.Length);
            compressed = ms.ToArray();
        }

        using var outMs = new MemoryStream();
        outMs.Write(Signature, 0, Signature.Length);

        // IHDR
        var ihdr = new byte[13];
        WriteU32(ihdr, 0, (uint)width);
        WriteU32(ihdr, 4, (uint)height);
        ihdr[8] = 8;   // bit depth
        ihdr[9] = 6;   // color type: RGBA
        ihdr[10] = 0;  // compression
        ihdr[11] = 0;  // filter
        ihdr[12] = 0;  // interlace
        WriteChunk(outMs, "IHDR", ihdr);

        WriteChunk(outMs, "IDAT", compressed);
        WriteChunk(outMs, "IEND", Array.Empty<byte>());
        return outMs.ToArray();
    }

    static void WriteChunk(Stream s, string type, byte[] data)
    {
        var typeBytes = Encoding.ASCII.GetBytes(type);
        WriteU32(s, (uint)data.Length);
        s.Write(typeBytes, 0, typeBytes.Length);
        if (data.Length > 0) s.Write(data, 0, data.Length);
        uint crc = 0xFFFFFFFF;
        foreach (var b in typeBytes) crc = CrcStep(crc, b);
        foreach (var b in data) crc = CrcStep(crc, b);
        WriteU32(s, crc ^ 0xFFFFFFFF);
    }

    static uint CrcStep(uint crc, byte b)
    {
        crc ^= b;
        for (int i = 0; i < 8; i++)
            crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        return crc;
    }

    static void WriteU32(Stream s, uint v)
    {
        s.WriteByte((byte)(v >> 24));
        s.WriteByte((byte)(v >> 16));
        s.WriteByte((byte)(v >> 8));
        s.WriteByte((byte)v);
    }

    static void WriteU32(byte[] buf, int offset, uint v)
    {
        buf[offset] = (byte)(v >> 24);
        buf[offset + 1] = (byte)(v >> 16);
        buf[offset + 2] = (byte)(v >> 8);
        buf[offset + 3] = (byte)v;
    }
}
