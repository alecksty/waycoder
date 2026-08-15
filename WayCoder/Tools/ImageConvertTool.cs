using WayCoder.Infra;

namespace WayCoder.Tools;

/// <summary>
/// 图片格式互转工具 —— PNG/JPG/BMP 之间互相转换。
/// 手搓编解码（零反射、零依赖、AOT 安全、跨平台），按魔数识别输入、按扩展名决定输出。
/// </summary>
public class ImageConvertTool : ITool
{
    public string Name => "convert_image";
    public string Description =>
        "把图片在 PNG/JPG/BMP 之间互相转换。读取 input 路径（按扩展名或魔数识别格式），" +
        "按 output 路径扩展名决定目标格式写入。支持 png/jpg(jpeg)/bmp；jpg 可用 quality 控制质量(1-100，默认 85)。" +
        "示例：把 a.png 转成 a.jpg —— input=\"a.png\" output=\"a.jpg\"。" +
        "也支持同格式重编码压缩（如 jpg 转 jpg 降质量）。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("input", JNode.Object()
                .Set("type", "string")
                .Set("description", "输入图片路径（png/jpg/jpeg/bmp）"))
            .Set("output", JNode.Object()
                .Set("type", "string")
                .Set("description", "输出图片路径，扩展名决定格式（png/jpg/jpeg/bmp）"))
            .Set("quality", JNode.Object()
                .Set("type", "integer")
                .Set("description", "JPEG 质量 1-100，默认 85（仅 jpg 输出生效）")))
        .Set("required", JNode.Array().Add("input").Add("output"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var input = arguments.GetValueOrDefault("input")?.ToString();
        var output = arguments.GetValueOrDefault("output")?.ToString();
        int quality = 85;
        if (arguments.TryGetValue("quality", out var q) && int.TryParse(q?.ToString(), out var qv))
            quality = Math.Clamp(qv, 1, 100);

        try
        {
            if (string.IsNullOrWhiteSpace(input) || !File.Exists(input))
                return Task.FromResult("convert_image 错误：输入文件不存在");
            var outFmt = ImageLoader.FormatOfPath(output);
            if (outFmt is not ("png" or "jpg" or "bmp"))
                return Task.FromResult("convert_image 错误：输出扩展名必须是 png/jpg/bmp");

            var img = ImageLoader.Load(input);
            if (img == null)
                return Task.FromResult($"convert_image 错误：无法解码 {input}（格式不受支持或文件损坏）");

            var bytes = ImageLoader.Encode(img, outFmt, quality);
            File.WriteAllBytes(output!, bytes);
            return Task.FromResult(
                $"✅ 已转换：{input} → {output}（{img.Width}×{img.Height}，{outFmt}，{bytes.Length:N0} 字节）");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"convert_image 错误：{ex.GetType().Name}: {ex.Message}");
        }
    }
}
