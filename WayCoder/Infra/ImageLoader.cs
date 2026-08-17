namespace WayCoder.Infra;

/// <summary>
/// 图片格式探测 + 解码/编码（PNG/JPG/BMP），供「贴图」（image 指令）与「格式互转」（convert_image 工具）共用。
/// 手搓编解码（零反射、零依赖、AOT 安全、跨平台）；SVG 无法栅格化，仅作路径透传。
/// </summary>
public static class ImageLoader
{
    /// <summary>按文件扩展名识别格式（jpg/jpeg 归一为 jpg；svg 单列）。</summary>
    public static string FormatOfPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        return ext switch
        {
            "jpeg" or "jpg" => "jpg",
            "png" => "png",
            "bmp" => "bmp",
            "svg" => "svg",
            _ => "",
        };
    }

    /// <summary>按魔数探测位图格式（png/jpg/bmp）；无法识别返回空串。</summary>
    public static string Detect(byte[] data)
    {
        if (data == null || data.Length < 4) return "";
        if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47) return "png";
        if (data[0] == 0xFF && data[1] == 0xD8) return "jpg";
        if (data[0] == 'B' && data[1] == 'M') return "bmp";
        return "";
    }

    static string Normalize(string ext)
    {
        var e = (ext ?? "").TrimStart('.').ToLowerInvariant();
        return e is "jpeg" ? "jpg" : e;
    }

    /// <summary>解码位图数据为 RasterImage；未知/损坏返回 null（不抛异常）。</summary>
    public static RasterImage? Decode(byte[] data, string? extHint)
    {
        if (data == null) return null;
        string fmt = !string.IsNullOrEmpty(extHint) ? Normalize(extHint) : Detect(data);
        try
        {
            return fmt switch
            {
                "png" => PngDecoder.Decode(data),
                "jpg" => JpegCodec.Decode(data),
                "bmp" => BmpCodec.Decode(data),
                _ => null,
            };
        }
        catch { return null; }
    }

    /// <summary>从文件加载位图（svg 或失败返回 null，不抛异常）。</summary>
    public static RasterImage? Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
        string fmt = FormatOfPath(path);
        if (fmt == "svg" || fmt == "") return null;
        try { return Decode(File.ReadAllBytes(path), fmt); }
        catch { return null; }
    }

    /// <summary>编码位图为指定格式字节流（png/jpg/bmp）；jpg 用 quality(1-100)。未知格式回退 png。</summary>
    public static byte[] Encode(RasterImage img, string format, int quality = 85)
    {
        return Normalize(format) switch
        {
            "png" => PngEncoder.Encode(img.Width, img.Height, img.Rgba),
            "jpg" => JpegCodec.Encode(img, Math.Clamp(quality, 1, 100)),
            "bmp" => BmpCodec.Encode(img),
            _ => PngEncoder.Encode(img.Width, img.Height, img.Rgba),
        };
    }
}
