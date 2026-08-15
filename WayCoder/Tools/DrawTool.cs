using System.Text;
using WayCoder.Infra;

namespace WayCoder.Tools;

/// <summary>
/// 绘图工具 —— 用文本指令绘制图形，输出 SVG（矢量）或 PNG（位图）。
/// 手搓渲染（零反射、AOT 安全、跨平台），指令可经插件系统扩展。
/// </summary>
public class DrawTool : ITool
{
    public string Name => "draw";
    public string Description =>
        "用文本指令绘制图形，输出 SVG 或 PNG 图片。指令：canvas W H [bg] 设画布，" +
        "rect/roundrect/circle/ellipse/line/arrow/polygon/polyline/path/text 绘图元，以及 " +
        "star/regular/ring/pie/heart 形状、image x y w h \"路径\" 贴图（PNG/JPG/BMP 图片拉伸贴入画布，" +
        "可加 crop sx sy sw sh 裁源图子矩形、round r 裁目标圆角、rect 直角矩形裁剪）、" +
        "icon mac|ios|android|windows [颜色] [字形] 一键生成应用图标模板（预设尺寸/圆角/安全区）、" +
        "translate/rotate/scale/push/pop 变换、gradient 渐变定义、" +
        "antialias 消除锯齿（PNG）。线宽在线/箭头/折线/路径尾部追加数值即可（如 \"line 0 0 100 0 #f00 5\"），" +
        "线头形状追加 butt/round/square（如 \"line 0 0 100 0 #f00 5 round\"）。" +
        "颜色支持 #hex 与命名色（red/green/blue...）。示例：\"canvas 400 300 #fff\\ncircle 200 150 60 #4a90d9\\ntext 200 20 \\\"标题\\\" 24 #333 middle\"。" +
        "format 选 png 时需给 output 路径，否则返回 SVG 文本。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("code", JNode.Object()
                .Set("type", "string")
                .Set("description", "绘图指令文本，每行一条（canvas/rect/circle/line/text 等）"))
            .Set("format", JNode.Object()
                .Set("type", "string")
                .Set("enum", JNode.Array().Add("svg").Add("png"))
                .Set("description", "输出格式，默认 svg"))
            .Set("output", JNode.Object()
                .Set("type", "string")
                .Set("description", "输出文件路径；png 时必填，svg 时缺省则返回内容文本")))
        .Set("required", JNode.Array().Add("code"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var code = arguments.GetValueOrDefault("code")?.ToString() ?? "";
        var format = arguments.GetValueOrDefault("format")?.ToString() ?? "svg";
        var output = arguments.GetValueOrDefault("output")?.ToString();

        try
        {
            var doc = DrawRunner.Parse(code);
            if (doc.Figures.Count == 0 && doc.Error != null)
                return Task.FromResult($"draw 错误：{doc.Error}");

            var isPng = format.Equals("png", StringComparison.OrdinalIgnoreCase);

            if (isPng)
            {
                var bytes = DrawRunner.ToPng(doc);
                var path = string.IsNullOrWhiteSpace(output)
                    ? Path.Combine(Environment.CurrentDirectory, "waycoder_draw.png")
                    : output;
                File.WriteAllBytes(path, bytes);
                var warn = doc.Error != null ? $"（部分指令有误：{doc.Error}）" : "";
                return Task.FromResult($"✅ 已生成 PNG：{path}（{doc.Width}×{doc.Height}，{bytes.Length:N0} 字节）{warn}");
            }

            var svg = DrawRunner.ToSvg(doc);
            if (!string.IsNullOrWhiteSpace(output))
            {
                File.WriteAllText(output, svg, Encoding.UTF8);
                var warn = doc.Error != null ? $"（部分指令有误：{doc.Error}）" : "";
                return Task.FromResult($"✅ 已生成 SVG：{output}（{doc.Width}×{doc.Height}）{warn}");
            }
            return Task.FromResult(svg);
        }
        catch (Exception ex)
        {
            return Task.FromResult($"draw 错误：{ex.GetType().Name}: {ex.Message}");
        }
    }
}
