using System.Text;
using WayCoder.Infra;
using WayCoder.Tools;

namespace WayCoder;

public static partial class SelfTest
{
    private static void TestChunk10(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        // ── ColorUtil ──
        Section("[Draw.Color]");
        Check("ColorUtil #rgb 解析", ColorUtil.Parse("#f00", 0) == 0xFFFF0000);
        Check("ColorUtil #rrggbb 解析", ColorUtil.Parse("#4a90d9", 0) == 0xFF4A90D9);
        Check("ColorUtil #rrggbbaa 解析", ColorUtil.Parse("#ff000080", 0) == 0xFF000080);
        Check("ColorUtil 命名色", ColorUtil.Parse("red", 0) == 0xFFFF0000);
        Check("ColorUtil 命名色忽略大小写", ColorUtil.Parse("BLUE", 0) == 0xFF0000FF);
        Check("ColorUtil 非法回退", ColorUtil.Parse("notacolor", 0x12345678) == 0x12345678);
        Check("ColorUtil TryParse 颜色", ColorUtil.TryParse("#abc", out _));
        Check("ColorUtil TryParse 非颜色", !ColorUtil.TryParse("123", out _));
        Check("ColorUtil ToHex 往返", ColorUtil.ToHex(0xFF4A90D9) == "#4a90d9");
        Console.WriteLine();

        // ── DrawTokenizer ──
        Section("[Draw.Tokenizer]");
        Check("分词 空白分隔", DrawTokenizer.Tokenize("rect 1 2 3 4").Count == 5);
        var q = DrawTokenizer.Tokenize("text 1 2 \"Hello World\"");
        Check("分词 引号字符串", q.Count == 4 && q[3].Quoted && q[3].Value == "Hello World");
        Check("分词 逗号分隔", DrawTokenizer.Tokenize("polygon 0,0 10,0 10,10").Count == 7);
        Check("分词 空串", DrawTokenizer.Tokenize("").Count == 0);
        Console.WriteLine();

        // ── DrawCommandRegistry ──
        Section("[Draw.Registry]");
        Check("注册表含 rect", DrawCommandRegistry.Contains("rect"));
        Check("注册表含 circle", DrawCommandRegistry.Contains("circle"));
        Check("注册表含 text", DrawCommandRegistry.Contains("text"));
        Check("注册表含 arrow", DrawCommandRegistry.Contains("arrow"));
        Check("注册表含 polygon", DrawCommandRegistry.Contains("polygon"));
        Console.WriteLine();

        // ── DrawRunner.Parse ──
        Section("[Draw.Parse]");
        var doc = DrawRunner.Parse("canvas 400 300 #000\nrect 0 0 10 10\ncircle 5 5 3");
        Check("Parse canvas 宽", doc.Width == 400);
        Check("Parse canvas 高", doc.Height == 300);
        Check("Parse canvas 背景", doc.Background == 0xFF000000);
        Check("Parse 图元计数", doc.Figures.Count == 2);
        Check("Parse 图元类型", doc.Figures[0].Kind == "rect" && doc.Figures[1].Kind == "circle");
        Check("Parse 未知指令报错", DrawRunner.Parse("foo 1 2").Error != null);
        Check("Parse 参数不足报错", DrawRunner.Parse("rect 1 2").Error != null);
        var tdoc = DrawRunner.Parse("text 10 10 \"Hi\" 20 #333 middle");
        Check("Parse text 引号内容", tdoc.Figures.Count == 1 && tdoc.Figures[0].Text == "Hi");
        Check("Parse text 字号", tdoc.Figures[0].FontSize == 20);
        var pdoc = DrawRunner.Parse("polygon 0,0 10,0 10,10");
        Check("Parse polygon 点数", pdoc.Figures.Count == 1 && pdoc.Figures[0].Args.Count == 6);
        Console.WriteLine();

        // ── SVG ──
        Section("[Draw.SVG]");
        var svg = DrawRunner.ToSvg(DrawRunner.Parse("canvas 200 100\ncircle 50 50 20 #ff0000\ntext 10 20 \"Hi\" 16"));
        Check("SVG 根元素", svg.Contains("<svg"));
        Check("SVG 圆标签", svg.Contains("<circle"));
        Check("SVG 文本标签", svg.Contains("<text"));
        Check("SVG 文本内容", svg.Contains("Hi"));
        Check("SVG 颜色十六进制", svg.Contains("#ff0000"));
        Console.WriteLine();

        // ── PNG ──
        Section("[Draw.PNG]");
        var pngDoc = DrawRunner.Parse("canvas 100 100 #fff\nrect 0 0 50 50 #ff0000");
        var png = DrawRunner.ToPng(pngDoc);
        Check("PNG 非空", png.Length > 100);
        Check("PNG 签名", png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47);
        Check("PNG IHDR 宽度", BE32(png, 16) == 100);
        Check("PNG IHDR 高度", BE32(png, 20) == 100);
        Check("PNG 结尾 IEND", Encoding.ASCII.GetString(png, png.Length - 8, 4) == "IEND");
        var tiny = PngEncoder.Encode(2, 2, new byte[16]);
        Check("PngEncoder 2x2 签名", tiny.Length > 0 && tiny[0] == 0x89 && BE32(tiny, 16) == 2 && BE32(tiny, 20) == 2);
        Console.WriteLine();

        // ── Canvas 光栅化 ──
        Section("[Draw.Canvas]");
        var cv = new Canvas(20, 20, 0xFFFFFFFF);
        cv.FillRect(0, 0, 2, 2, 0xFFFF0000);
        Check("FillRect 设像素", PixelAt(cv, 0, 0) == 0xFFFF0000);
        Check("FillRect 界外为背景", PixelAt(cv, 5, 5) == 0xFFFFFFFF);
        var cv2 = new Canvas(20, 20, 0xFFFFFFFF);
        cv2.FillCircle(10, 10, 4, 0xFF000000);
        Check("FillCircle 圆心", PixelAt(cv2, 10, 10) == 0xFF000000);
        Check("FillCircle 圆外", PixelAt(cv2, 0, 0) == 0xFFFFFFFF);
        var cv3 = new Canvas(20, 20, 0xFFFFFFFF);
        cv3.DrawLine(0, 0, 9, 9, 0xFF000000, 1);
        Check("DrawLine 起点", PixelAt(cv3, 0, 0) == 0xFF000000);
        Check("DrawLine 终点", PixelAt(cv3, 9, 9) == 0xFF000000);
        var cv4 = new Canvas(10, 10, 0xFFFFFFFF);
        cv4.DrawText(0, 0, "A", 7, 0xFF000000, "start");
        Check("DrawText 亮像素", PixelAt(cv4, 2, 0) == 0xFF000000);
        Check("DrawText 空像素", PixelAt(cv4, 0, 0) == 0xFFFFFFFF);
        Console.WriteLine();

        // ── DrawTool ──
        Section("[Draw.Tool]");
        var tool = new DrawTool();
        Check("DrawTool 名称", tool.Name == "draw");
        var r1 = tool.ExecuteAsync(new Dictionary<string, object?> { ["code"] = "canvas 10 10\ncircle 5 5 2" }).Result;
        Check("DrawTool svg 返回", r1.Contains("<svg"));
        var r2 = tool.ExecuteAsync(new Dictionary<string, object?> { ["code"] = "bogus 1" }).Result;
        Check("DrawTool 错误返回", r2.Contains("错误"));
    }

    static uint BE32(byte[] b, int off)
        => ((uint)b[off] << 24) | ((uint)b[off + 1] << 16) | ((uint)b[off + 2] << 8) | b[off + 3];

    static uint PixelAt(Canvas c, int x, int y)
    {
        if (x < 0 || y < 0 || x >= c.Width || y >= c.Height) return 0;
        var i = (y * c.Width + x) * 4; // 存储顺序 RGBA
        return ((uint)c.Pixels[i + 3] << 24) | ((uint)c.Pixels[i] << 16) | ((uint)c.Pixels[i + 1] << 8) | c.Pixels[i + 2];
    }
}
