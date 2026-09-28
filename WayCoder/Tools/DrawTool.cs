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
        L.Pick("用文本指令绘制图形，输出 SVG 或 PNG 图片。指令：canvas W H [bg] 设画布，" +
        "rect/roundrect/circle/ellipse/line/arrow/polygon/polyline/path/text 绘图元，以及 " +
        "star/regular/ring/pie/heart 形状、image x y w h \"路径\" 贴图（PNG/JPG/BMP 图片拉伸贴入画布，" +
        "可加 crop sx sy sw sh 裁源图子矩形、round r 裁目标圆角、rect 直角矩形裁剪）、" +
        "icon mac|ios|android|windows [颜色] [字形] 一键生成应用图标模板（预设尺寸/圆角/安全区）、" +
        "translate/rotate/scale/push/pop 变换、gradient 渐变定义、" +
        "antialias 消除锯齿（PNG）。" +
        "flowchart \"A[开始]-->B{判断}-->C((结束))\" 语义流程图（节点 [方] (圆角) {菱形} ((圆))，连线 -->/-.->/==>/---，自动分层布局）。" +
        "线宽在线/箭头/折线/路径尾部追加数值即可（如 \"line 0 0 100 0 #f00 5\"），" +
        "线头形状追加 butt/round/square、虚线追加 dash（如 \"line 0 0 100 0 #f00 2 dash\"）。" +
        "text 支持多行：文本内用 \\n 换行。" +
        "颜色支持 #hex 与命名色（red/green/blue...）。示例：\"canvas 400 300 #fff\\ncircle 200 150 60 #4a90d9\\ntext 200 20 \\\"标题\\\" 24 #333 middle\"。" +
        "format 选 png 时需给 output 路径，否则返回 SVG 文本。" +
        "另可「看图」：给 image 参数（png/jpg/bmp 路径）则进入像素采样模式，返回颜色而非绘图——" +
        "配合 points \"x,y;x,y\" 逐点取色，或 grid \"cols,rows\" 均匀网格取色（供非 vision 模型推断图像内容）。",
        "Draw shapes from text commands and output an SVG or PNG image. Commands: canvas W H [bg] sets the canvas, " +
        "rect/roundrect/circle/ellipse/line/arrow/polygon/polyline/path/text draw primitives, plus " +
        "star/regular/ring/pie/heart shapes, image x y w h \"path\" embeds an image (PNG/JPG/BMP stretched onto the canvas; " +
        "add crop sx sy sw sh to crop a sub-rectangle of the source, round r for rounded target corners, rect for a rectangular crop), " +
        "icon mac|ios|android|windows [color] [glyph] generates an app icon template in one command (preset sizes/rounded corners/safe area), " +
        "translate/rotate/scale/push/pop transforms, gradient definitions, " +
        "antialias removes jagged edges (PNG). " +
        "flowchart \"A[Start]-->B{Check}-->C((End))\" draws a semantic flowchart (nodes [box] (rounded) {diamond} ((circle)), edges -->/-.->/==>/---, automatic layered layout). " +
        "Append a number at the end of a line/arrow/polyline/path to set its width (e.g. \"line 0 0 100 0 #f00 5\"), " +
        "append butt/round/square for the cap style and dash for a dashed line (e.g. \"line 0 0 100 0 #f00 2 dash\"). " +
        "text supports multiple lines: use \\n inside the text to break lines. " +
        "Colors accept #hex and named colors (red/green/blue...). Example: \"canvas 400 300 #fff\\ncircle 200 150 60 #4a90d9\\ntext 200 20 \\\"Title\\\" 24 #333 middle\". " +
        "When format is png an output path is required; otherwise the SVG text is returned. " +
        "It can also read images: passing the image parameter (a png/jpg/bmp path) enters pixel-sampling mode, returning colors instead of drawing - " +
        "combine with points \"x,y;x,y\" to sample individual points, or grid \"cols,rows\" to sample an even grid (lets non-vision models infer image content).");

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("code", JNode.Param("string", L.Pick("绘图指令文本，每行一条（canvas/rect/circle/line/text 等）", "Drawing command text, one command per line (canvas/rect/circle/line/text, etc.)")))
            .Set("format", JNode.Object()
                .Set("type", "string")
                .Set("enum", JNode.Array("svg", "png"))
                .Set("description", L.Pick("输出格式，默认 svg", "Output format. Default svg")))
            .Set("output", JNode.Param("string", L.Pick("输出文件路径；png 时必填，svg 时缺省则返回内容文本", "Output file path; required for png, and when omitted for svg the content text is returned")))
            .Set("image", JNode.Param("string", L.Pick("要采样的图片路径（png/jpg/bmp）。给了此项则进入像素采样模式，返回颜色而非绘图", "Path of the image to sample (png/jpg/bmp). When given, enters pixel-sampling mode and returns colors instead of drawing")))
            .Set("points", JNode.Object()
                .Set("type", "string")
                .Set("description", L.Pick("点采样坐标列表，格式 \"x,y;x,y\"（如 \"10,20;30,40\"），需配合 image", "List of point-sampling coordinates, format \"x,y;x,y\" (e.g. \"10,20;30,40\"). Requires image")))
            .Set("grid", JNode.Object()
                .Set("type", "string")
                .Set("description", L.Pick("网格采样，格式 \"cols,rows\"（如 \"4,3\"），需配合 image", "Grid sampling, format \"cols,rows\" (e.g. \"4,3\"). Requires image"))))
        .Set("required", JNode.Array("code"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var code = arguments.GetValueOrDefault("code")?.ToString() ?? "";
        var format = arguments.GetValueOrDefault("format")?.ToString() ?? "svg";
        var output = arguments.GetValueOrDefault("output")?.ToString();
        var image = arguments.GetValueOrDefault("image")?.ToString();
        var points = arguments.GetValueOrDefault("points")?.ToString();
        var grid = arguments.GetValueOrDefault("grid")?.ToString();

        try
        {
            // 像素采样模式（看图）：给了 image 则读取图片返回颜色，不走绘图
            if (!string.IsNullOrWhiteSpace(image))
                return Task.FromResult(Sample(image, points, grid));

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
            return Task.FromResult(ToolErrors.Error("draw ", ex));
        }
    }

    /// <summary>像素采样（看图）：读图片，按 points 逐点或 grid 网格返回 #rrggbb 颜色。</summary>
    private static string Sample(string image, string? points, string? grid)
    {
        var img = ImageLoader.Load(image);
        if (img == null)
            return $"draw 错误：无法读取图片 {image}（仅支持 png/jpg/bmp）";

        // 点采样：points "x,y;x,y"
        if (!string.IsNullOrWhiteSpace(points))
        {
            var list = new List<(int x, int y)>();
            foreach (var part in points.Split(';'))
            {
                var p = part.Trim();
                if (p.Length == 0) continue;
                var xy = p.Split(',');
                if (xy.Length != 2 || !int.TryParse(xy[0].Trim(), out var x) || !int.TryParse(xy[1].Trim(), out var y))
                    return $"draw 错误：坐标点格式非法 '{p}'（应为 x,y）";
                list.Add((x, y));
            }
            if (list.Count == 0) return "draw 错误：points 为空";
            var colors = img.SamplePoints(list);
            var sb = new StringBuilder();
            sb.Append($"✅ 采样 {list.Count} 个点（图像 {img.Width}×{img.Height}）：");
            for (int i = 0; i < list.Count; i++)
                sb.Append($"\n({list[i].x},{list[i].y}) {colors[i]}");
            return sb.ToString();
        }

        // 网格采样：grid "cols,rows"
        if (!string.IsNullOrWhiteSpace(grid))
        {
            var parts = grid.Split(',');
            if (parts.Length < 2 || !int.TryParse(parts[0].Trim(), out var cols) || !int.TryParse(parts[1].Trim(), out var rows))
                return $"draw 错误：grid 格式非法 '{grid}'（应为 cols,rows）";
            if (cols <= 0 || rows <= 0) return "draw 错误：grid 行列必须为正整数";
            // 防整数溢出/OOM：cols*rows 与 cx*Width 均按 int 相乘，超大网格溢出为负或分配数十 GB
            if (cols > 256 || rows > 256) return "draw 错误：grid 行列过大（上限 256×256）";
            var colors = img.SampleGrid(cols, rows);
            var sb = new StringBuilder();
            sb.Append($"✅ 网格采样 {cols}×{rows}（图像 {img.Width}×{img.Height}）：");
            for (int ry = 0; ry < rows; ry++)
            {
                sb.Append('\n');
                for (int cx = 0; cx < cols; cx++)
                {
                    if (cx > 0) sb.Append(' ');
                    sb.Append(colors[ry * cols + cx]);
                }
            }
            return sb.ToString();
        }

        return "draw 错误：给了 image 参数但缺少 points 或 grid（采样方式）";
    }
}
