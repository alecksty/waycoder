using System.Text;
using WayCoder.Infra;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>
    /// 从当前目录往上找「Resources/Raw/help」（自测可能跑在 bin/ 下）。
    /// 找不到返回 null —— 由用例自己报红，而不是静默跳过（静默跳过 = 这条测试永远不生效）。
    /// </summary>
    private static string? FindHelpDir()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            var probe = Path.Combine(dir.FullName, "WayCoder.Maui", "Resources", "Raw", "help");
            if (Directory.Exists(probe)) return probe;
        }
        return null;
    }

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

        // ── 文字**竖对齐**（v0.96.336）──
        // 缺这一档时，程序写"横中"只能得到"横向居中、纵向顶着 y" —— 摆在方框正中看着偏上。
        // 判据钉在**三后端共用的那一份偏移**上（SVG / 光栅 / 矢量都走它，只有一份）。
        Section("[Draw.竖对齐]");
        DrawFigure Txt(string v, int n)
        {
            var f = new DrawFigure { Kind = "text", Text = n <= 1 ? "x" : string.Join("\n", Enumerable.Repeat("x", n)), FontSize = 20, VAnchor = v };
            f.Args.Add(0); f.Args.Add(100);
            return f;
        }
        // 盒高 = 行距(字号×1.3) ×(行数-1) + 字号 ⇒ 单行 20、两行 46
        Check("VAlign: 顶（默认）= 老行为、偏移 0",
            DrawParse.TextVOffset(Txt("top", 1)) == 0 && DrawParse.TextVOffset(Txt("top", 2)) == 0);
        Check("VAlign: 竖中 = 上移半个盒高（单行 20 → -10）",
            Math.Abs(DrawParse.TextVOffset(Txt("center", 1)) + 10) < 1e-9);
        Check("VAlign: 竖中按**行数**算盒高（两行 46 → -23）",
            Math.Abs(DrawParse.TextVOffset(Txt("center", 2)) + 23) < 1e-9);
        Check("VAlign: 底 = 上移一个盒高（两行 46 → -46）",
            Math.Abs(DrawParse.TextVOffset(Txt("bottom", 2)) + 46) < 1e-9);

        // DSL：`vtop/vcenter/vbottom` 要认，且**不与横锚点的 middle 重名**
        var vfig = DrawCommandRegistry.Get("text")!.Parse(DrawTokenizer.Tokenize("text 5 6 \"hi\" 20 vcenter"));
        Check("VAlign: DSL 认 vcenter", vfig != null && vfig.VAnchor == "center");
        var hfig = DrawCommandRegistry.Get("text")!.Parse(DrawTokenizer.Tokenize("text 5 6 \"hi\" 20 middle"));
        Check("VAlign: 横锚点的 middle **不**被当成竖中",
            hfig != null && hfig.Anchor == "middle" && hfig.VAnchor == "top");
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
        // 越界钳制：负坐标/超大尺寸只填画布内交集，不再数十亿次迭代（v0.71.29 修复）
        var cvClamp = new Canvas(10, 10, 0xFFFFFFFF);
        cvClamp.FillRect(8, 8, 5, 5, 0xFF0000FF);
        Check("FillRect 越界钳制只填界内", PixelAt(cvClamp, 9, 9) == 0xFF0000FF && PixelAt(cvClamp, 0, 0) == 0xFFFFFFFF);
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

        // ── 线头形状（LineCap）──
        Section("[Draw.LineCap]");
        var lcRound = DrawRunner.Parse("canvas 40 40 #fff\nline 0 0 30 0 #000 4 round");
        Check("linecap round 解析", lcRound.Figures[0].LineCap == "round");
        Check("linecap round SVG", DrawRunner.ToSvg(lcRound).Contains("stroke-linecap=\"round\""));
        Check("linecap square 解析", DrawRunner.Parse("line 0 0 30 0 #000 4 square").Figures[0].LineCap == "square");
        var lcButt = DrawRunner.Parse("canvas 40 40 #fff\nline 0 0 30 0 #000 4");
        Check("linecap 默认 butt", lcButt.Figures[0].LineCap == "butt");
        Check("linecap 默认 SVG butt", DrawRunner.ToSvg(lcButt).Contains("stroke-linecap=\"butt\""));
        // 光栅化：宽 4 水平线 (2,10)-(18,10)，square 两端外延、round 半圆收窄、butt 不外延
        var sqc = new Canvas(21, 21, 0xFFFFFFFF);
        sqc.DrawLine(2, 10, 18, 10, 0xFF000000, 4, "square");
        Check("linecap square 外延", PixelAt(sqc, 0, 10) == 0xFF000000 && PixelAt(sqc, 0, 8) == 0xFF000000);
        var rdc = new Canvas(21, 21, 0xFFFFFFFF);
        rdc.DrawLine(2, 10, 18, 10, 0xFF000000, 4, "round");
        Check("linecap round 中心外延角收窄", PixelAt(rdc, 0, 10) == 0xFF000000 && PixelAt(rdc, 0, 8) == 0xFFFFFFFF);
        var btc = new Canvas(21, 21, 0xFFFFFFFF);
        btc.DrawLine(2, 10, 18, 10, 0xFF000000, 4, "butt");
        Check("linecap butt 不外延", PixelAt(btc, 0, 10) == 0xFFFFFFFF && PixelAt(btc, 2, 10) == 0xFF000000);
        Console.WriteLine();

        // ── 虚线（dash）──
        Section("[Draw.Dash]");
        Check("line dash 解析", DrawRunner.Parse("line 0 0 30 0 #000 2 dash").Figures[0].Dashed);
        Check("line dash 别名 dashed", DrawRunner.Parse("line 0 0 30 0 #000 2 dashed").Figures[0].Dashed);
        Check("line 无 dash", !DrawRunner.Parse("line 0 0 30 0 #000 2").Figures[0].Dashed);
        Check("line dash SVG", DrawRunner.ToSvg(DrawRunner.Parse("line 0 0 30 0 #000 2 dash")).Contains("stroke-dasharray=\"6 4\""));
        Check("arrow dash 解析", DrawRunner.Parse("arrow 0 0 30 0 #000 2 dash").Figures[0].Dashed);
        Check("polyline dash 解析", DrawRunner.Parse("polyline 0 0 10 0 10 10 #000 dash").Figures[0].Dashed);
        var dashPng = DrawRunner.ToPng(DrawRunner.Parse("canvas 40 40 #fff\nline 0 0 30 0 #000 2 dash"));
        Check("dash PNG 签名", dashPng[0] == 0x89 && BE32(dashPng, 16) == 40 && BE32(dashPng, 20) == 40);
        Console.WriteLine();

        // ── 多行文字（\n）──
        Section("[Draw.MultilineText]");
        var ml = DrawRunner.Parse("text 10 10 \"第一行\\n第二行\" 14");
        Check("多行 text 解析", ml.Figures.Count == 1 && ml.Figures[0].Text == "第一行\n第二行");
        var mlSvg = DrawRunner.ToSvg(ml);
        Check("多行 SVG 两个 tspan", mlSvg.Contains("<tspan") && mlSvg.Split("</tspan>").Length == 3);
        var mlPng = DrawRunner.ToPng(DrawRunner.Parse("canvas 100 60 #fff\ntext 10 10 \"A\\nB\" 14"));
        Check("多行 PNG 签名", mlPng[0] == 0x89 && BE32(mlPng, 16) == 100 && BE32(mlPng, 20) == 60);
        Console.WriteLine();

        // ── 语义流程图（flowchart）──
        Section("[Draw.Flowchart]");
        var fcDoc = DrawRunner.Parse("flowchart \"A[开始]-->B{判断}-->C((结束))\"");
        Check("flowchart 无错误", fcDoc.Error == null);
        Check("flowchart 生成 8 图元(3 节点+3 文字+2 连线)", fcDoc.Figures.Count == 8);
        Check("flowchart 含箭头", fcDoc.Figures.Any(f => f.Kind == "arrow"));
        Check("flowchart 含菱形", fcDoc.Figures.Any(f => f.Kind == "polygon"));
        Check("flowchart 含圆形", fcDoc.Figures.Any(f => f.Kind == "circle"));
        var fcSvg = DrawRunner.ToSvg(fcDoc);
        Check("flowchart SVG 生成", fcSvg.Contains("<svg") && fcSvg.Contains("<polyline") == false);
        var fcPng = DrawRunner.ToPng(fcDoc);
        Check("flowchart PNG 生成", fcPng[0] == 0x89);
        var fcDash = DrawRunner.Parse("flowchart \"A-->B\"");
        Check("flowchart 单边 5 图元(2 节点+2 文字+1 连线)", fcDash.Figures.Count == 5);
        Check("flowchart 错误报错", DrawRunner.Parse("flowchart \"A-->\"").Error != null);
        Console.WriteLine();

        // ── 消除锯齿（Antialias）──
        Section("[Draw.Antialias]");
        Check("Affine 恒等缩放因子 1", Affine.Identity.ScaleFactor == 1);
        Check("Affine 缩放因子 3", Affine.Scale(3, 3).ScaleFactor == 3);
        var aaDoc = DrawRunner.Parse("antialias\ncanvas 40 40 #fff\ncircle 20 20 10 #000");
        Check("aa 指令置位", aaDoc.Antialias);
        var noAaDoc = DrawRunner.Parse("canvas 40 40 #fff\ncircle 20 20 10 #000");
        Check("默认无 aa", !noAaDoc.Antialias);
        var aaPng = DrawRunner.ToPng(aaDoc);
        var noAaPng = DrawRunner.ToPng(noAaDoc);
        Check("aa PNG 签名", aaPng[0] == 0x89 && BE32(aaPng, 16) == 40 && BE32(aaPng, 20) == 40);
        bool aaDiff = aaPng.Length != noAaPng.Length;
        if (!aaDiff) for (int i = 0; i < aaPng.Length; i++) if (aaPng[i] != noAaPng[i]) { aaDiff = true; break; }
        Check("aa 改变边缘像素", aaDiff);
        Console.WriteLine();

        // ── TrueType 字体 + 字形抗锯齿 ──
        Section("[Draw.Font]");
        Check("FontFinder 返回列表", FontFinder.Find() != null);
        Check("FontFinder 归一化", FontFinder.Normalize("PingFang SC") == "pingfangsc");
        Check("TTF 空数据 null", TrueTypeFont.Load(Array.Empty<byte>()) == null);
        Check("TTF 短数据 null", TrueTypeFont.Load(new byte[] { 1, 2, 3, 4 }) == null);
        var otto = new byte[20]; System.Text.Encoding.ASCII.GetBytes("OTTO").CopyTo(otto, 0);
        Check("TTF OTTO 拒绝", TrueTypeFont.Load(otto) == null);
        Check("TTF 不存在路径 null", TrueTypeFont.Load("/no/such/font.ttf") == null);
        var bc = new Canvas(4, 4, 0xFFFFFFFF);
        bc.BlendPixel(1, 1, 0xFF000000, 0.5);
        var bpx = PixelAt(bc, 1, 1);
        Check("BlendPixel 半覆盖灰", (bpx & 0xFF) >= 126 && (bpx & 0xFF) <= 128 && (bpx >> 24) == 0xFF);
        var font = TrueTypeFont.Resolve(null);
        if (font != null)
        {
            Check("TTF UnitsPerEm > 0", font.UnitsPerEm > 0);
            Check("TTF NumGlyphs > 0", font.NumGlyphs > 0);
            int gi = font.GlyphIndex('A');
            Check("TTF 'A' 有字形", gi > 0);
            var outline = font.GetOutline(gi);
            Check("TTF 'A' 轮廓非空", outline.Count > 0 && outline[0].Length >= 6);
            Check("TTF 测量宽度 > 0", font.Measure("A", 16) > 0);
            var fc = new Canvas(100, 40, 0xFFFFFFFF);
            font.Render(fc, "A", 5, 5, 32, 0xFF000000, "start", false, false);
            int dark = 0;
            for (int y = 0; y < 40; y++) for (int x = 0; x < 100; x++) if (PixelAt(fc, x, y) != 0xFFFFFFFF) dark++;
            Check("TTF 渲染产生像素", dark > 10);
        }
        else
        {
            Check("TTF 无系统字体（跳过）", true);
        }
        Console.WriteLine();

        // ── DrawTool ──
        Section("[Draw.Tool]");
        var tool = new DrawTool();
        Check("DrawTool 名称", tool.Name == "draw");
        var r1 = tool.ExecuteAsync(new Dictionary<string, object?> { ["code"] = "canvas 10 10\ncircle 5 5 2" }).Result;
        Check("DrawTool svg 返回", r1.Contains("<svg"));
        var r2 = tool.ExecuteAsync(new Dictionary<string, object?> { ["code"] = "bogus 1" }).Result;
        Check("DrawTool 错误返回", r2.Contains("错误"));

        // DrawTool 像素采样（看图）：image + points / grid
        var sampleRgba = new byte[2 * 2 * 4];
        sampleRgba[0] = 255; sampleRgba[3] = 255;                              // (0,0) 红
        sampleRgba[4] = 255; sampleRgba[5] = 255; sampleRgba[7] = 255;        // (1,0) 绿
        sampleRgba[8] = 255; sampleRgba[9] = 255; sampleRgba[11] = 255;       // (0,1) 蓝
        sampleRgba[12] = 255; sampleRgba[13] = 255; sampleRgba[14] = 255; sampleRgba[15] = 255; // (1,1) 白
        var samplePng = Path.Combine(Path.GetTempPath(), "wc_sample_" + Guid.NewGuid().ToString("N") + ".png");
        File.WriteAllBytes(samplePng, PngEncoder.Encode(2, 2, sampleRgba));

        var sp = tool.ExecuteAsync(new Dictionary<string, object?> { ["image"] = samplePng, ["points"] = "0,0;1,1" }).Result;
        Check("DrawTool 采样 点", sp.Contains("#ff0000") && sp.Contains("#ffffff") && sp.Contains("采样 2 个点"));
        var sg = tool.ExecuteAsync(new Dictionary<string, object?> { ["image"] = samplePng, ["grid"] = "2,2" }).Result;
        Check("DrawTool 采样 网格", sg.Contains("#ff0000") && sg.Contains("#ffffff") && sg.Contains("网格采样 2×2"));
        var sBad = tool.ExecuteAsync(new Dictionary<string, object?> { ["image"] = samplePng }).Result;
        Check("DrawTool 采样 缺方式", sBad.Contains("缺少 points 或 grid"));
        var sMiss = tool.ExecuteAsync(new Dictionary<string, object?> { ["image"] = "/no/such.png", ["points"] = "0,0" }).Result;
        Check("DrawTool 采样 缺文件", sMiss.Contains("无法读取"));
        var sFmt = tool.ExecuteAsync(new Dictionary<string, object?> { ["image"] = samplePng, ["points"] = "1,2,3" }).Result;
        Check("DrawTool 采样 坐标非法", sFmt.Contains("坐标点格式非法"));
        try { File.Delete(samplePng); } catch { }
        Console.WriteLine();

        // ── 图片编解码（PNG/BMP/JPEG）──
        Section("[Codec.RasterImage]");
        var testRgba = new byte[2 * 2 * 4];
        testRgba[0] = 255; testRgba[1] = 0; testRgba[2] = 0; testRgba[3] = 255;     // 红
        testRgba[4] = 0; testRgba[5] = 255; testRgba[6] = 0; testRgba[7] = 255;     // 绿
        testRgba[8] = 0; testRgba[9] = 0; testRgba[10] = 255; testRgba[11] = 255;   // 蓝
        testRgba[12] = 255; testRgba[13] = 255; testRgba[14] = 255; testRgba[15] = 255; // 白
        var ri = new RasterImage(2, 2, testRgba);
        Check("RasterImage ColorAt 红", ri.ColorAt(0, 0) == 0xFFFF0000);
        Check("RasterImage HexAt 绿", ri.HexAt(1, 0) == "#00ff00");
        Check("RasterImage 越界返回 0", ri.ColorAt(9, 9) == 0);
        var grid = ri.SampleGrid(2, 2);
        Check("SampleGrid 数量", grid.Length == 4);
        Check("SampleGrid 首末", grid[0] == "#ff0000" && grid[3] == "#ffffff");
        var pts = ri.SamplePoints(new (int, int)[] { (0, 0), (1, 1) });
        Check("SamplePoints 批量", pts.Length == 2 && pts[0] == "#ff0000" && pts[1] == "#ffffff");
        Console.WriteLine();

        Section("[Codec.Png]");
        var pngData = PngEncoder.Encode(2, 2, testRgba);
        var pngDec = PngDecoder.Decode(pngData);
        Check("PngDecoder 尺寸", pngDec.Width == 2 && pngDec.Height == 2);
        Check("PngDecoder 红", pngDec.HexAt(0, 0) == "#ff0000");
        Check("PngDecoder 绿", pngDec.HexAt(1, 0) == "#00ff00");
        Check("PngDecoder 蓝", pngDec.HexAt(0, 1) == "#0000ff");
        Check("PngDecoder 白", pngDec.HexAt(1, 1) == "#ffffff");
        bool pngThrew = false; try { PngDecoder.Decode(new byte[] { 1, 2, 3 }); } catch { pngThrew = true; }
        Check("PngDecoder 垃圾数据报错", pngThrew);
        Console.WriteLine();

        Section("[Codec.Bmp]");
        var bmpData = BmpCodec.Encode(ri);
        var bmpDec = BmpCodec.Decode(bmpData);
        Check("Bmp 签名", bmpData[0] == 'B' && bmpData[1] == 'M');
        Check("Bmp 尺寸", bmpDec.Width == 2 && bmpDec.Height == 2);
        Check("Bmp 红", bmpDec.HexAt(0, 0) == "#ff0000");
        Check("Bmp 绿", bmpDec.HexAt(1, 0) == "#00ff00");
        Check("Bmp 蓝", bmpDec.HexAt(0, 1) == "#0000ff");
        Check("Bmp 白", bmpDec.HexAt(1, 1) == "#ffffff");
        bool bmpThrew = false; try { BmpCodec.Decode(new byte[] { 1, 2, 3 }); } catch { bmpThrew = true; }
        Check("Bmp 垃圾数据报错", bmpThrew);
        Console.WriteLine();

        Section("[Codec.Jpeg]");
        var jpegImg = new RasterImage(16, 16, MakeSolid(16, 16, 200, 40, 30));
        var jpegData = JpegCodec.Encode(jpegImg, 90);
        Check("Jpeg 签名", jpegData[0] == 0xFF && jpegData[1] == 0xD8);
        Check("Jpeg 结尾 EOI", jpegData[jpegData.Length - 2] == 0xFF && jpegData[jpegData.Length - 1] == 0xD9);
        var jpegDec = JpegCodec.Decode(jpegData);
        Check("Jpeg 尺寸", jpegDec.Width == 16 && jpegDec.Height == 16);
        uint jp = jpegDec.ColorAt(8, 8);
        int jr = (int)((jp >> 16) & 0xFF), jg = (int)((jp >> 8) & 0xFF), jb = (int)(jp & 0xFF);
        Check("Jpeg 近红", jr > 190 && jg < 70 && jb < 70);
        // 非 8 倍数尺寸（含边缘填充）
        var jpegImg2 = new RasterImage(13, 9, MakeSolid(13, 9, 30, 90, 210));
        var jpegDec2 = JpegCodec.Decode(JpegCodec.Encode(jpegImg2, 85));
        Check("Jpeg 非对齐尺寸", jpegDec2.Width == 13 && jpegDec2.Height == 9);
        uint jp2 = jpegDec2.ColorAt(12, 8);
        Check("Jpeg 非对齐近蓝", (jp2 & 0xFF) > 190 && ((jp2 >> 16) & 0xFF) < 70);
        bool jpegThrew = false; try { JpegCodec.Decode(new byte[] { 1, 2, 3, 4 }); } catch { jpegThrew = true; }
        Check("Jpeg 垃圾数据报错", jpegThrew);
        // 编码尺寸守卫：超 65535 宽 / 超大像素数须拒绝（防 SOF0 ushort 静默截断与整数溢出）
        bool jpegWideThrew = false; try { JpegCodec.Encode(new RasterImage(65536, 1, new byte[65536 * 4])); } catch (ArgumentException) { jpegWideThrew = true; }
        Check("Jpeg 宽>65535 拒绝", jpegWideThrew);
        bool pngBigThrew = false; try { PngEncoder.Encode(6000, 6000, new byte[0]); } catch (ArgumentException) { pngBigThrew = true; }
        Check("Png 超大尺寸拒绝", pngBigThrew);
        Console.WriteLine();

        // ── 图片加载（魔数探测 + 解码/编码）──
        Section("[Image.Loader]");
        Check("ImageLoader 探测 PNG", ImageLoader.Detect(new byte[] { 0x89, 0x50, 0x4E, 0x47 }) == "png");
        Check("ImageLoader 探测 JPG", ImageLoader.Detect(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }) == "jpg");
        Check("ImageLoader 探测 BMP", ImageLoader.Detect(new byte[] { (byte)'B', (byte)'M', 0, 0 }) == "bmp");
        Check("ImageLoader 探测未知", ImageLoader.Detect(new byte[] { 1, 2, 3, 4 }) == "");
        Check("ImageLoader 扩展名 jpeg 归一", ImageLoader.FormatOfPath("a.jpeg") == "jpg");
        Check("ImageLoader 扩展名 png", ImageLoader.FormatOfPath("a.PNG") == "png");
        Check("ImageLoader svg 单列", ImageLoader.FormatOfPath("a.svg") == "svg");
        var loaderDec = ImageLoader.Decode(pngData, "png");
        Check("ImageLoader 解码 png", loaderDec != null && loaderDec!.HexAt(0, 0) == "#ff0000");
        var bmpOut = ImageLoader.Encode(ri, "bmp");
        Check("ImageLoader 编码 bmp 签名", bmpOut[0] == 'B' && bmpOut[1] == 'M');
        var jpgOut = ImageLoader.Encode(ri, "jpg", 90);
        Check("ImageLoader 编码 jpg 签名", jpgOut[0] == 0xFF && jpgOut[1] == 0xD8);
        Check("ImageLoader 垃圾数据 null", ImageLoader.Decode(new byte[] { 1, 2, 3 }, "") == null);
        Check("ImageLoader 不存在文件 null", ImageLoader.Load("/no/such/file.png") == null);
        Console.WriteLine();

        // ── 格式互转工具（convert_image）──
        Section("[Image.Convert]");
        var conv = new ImageConvertTool();
        Check("ImageConvertTool 名称", conv.Name == "convert_image");
        var srcPng = Path.Combine(Path.GetTempPath(), "wc_cvt_" + Guid.NewGuid().ToString("N") + ".png");
        var dstBmp = Path.Combine(Path.GetTempPath(), "wc_cvt_" + Guid.NewGuid().ToString("N") + ".bmp");
        File.WriteAllBytes(srcPng, PngEncoder.Encode(2, 2, testRgba));
        var cr = conv.ExecuteAsync(new Dictionary<string, object?> { ["input"] = srcPng, ["output"] = dstBmp }).Result;
        Check("转换返回成功", cr.Contains("已转换"));
        var convDec = ImageLoader.Load(dstBmp);
        Check("转换结果解码", convDec != null && convDec!.HexAt(0, 0) == "#ff0000");
        var bad = conv.ExecuteAsync(new Dictionary<string, object?> { ["input"] = srcPng, ["output"] = "/tmp/x.txt" }).Result;
        Check("非法输出格式报错", bad.Contains("错误"));
        var miss = conv.ExecuteAsync(new Dictionary<string, object?> { ["input"] = "/no/such.png", ["output"] = "/tmp/x.png" }).Result;
        Check("输入不存在报错", miss.Contains("错误"));
        try { File.Delete(srcPng); File.Delete(dstBmp); } catch { }
        Console.WriteLine();

        // ── 贴图指令（image）──
        Section("[Image.Paste]");
        Check("注册表含 image", DrawCommandRegistry.Contains("image"));
        var pasteSrc = Path.Combine(Path.GetTempPath(), "wc_paste_" + Guid.NewGuid().ToString("N") + ".png");
        File.WriteAllBytes(pasteSrc, PngEncoder.Encode(2, 2, testRgba));
        var pd = DrawRunner.Parse("canvas 10 10 #000\nimage 0 0 10 10 \"" + pasteSrc + "\"");
        Check("image 图元解析", pd.Figures.Count == 1 && pd.Figures[0].Kind == "image");
        Check("image 加载位图", pd.Figures[0].Image != null && pd.Figures[0].Image!.Width == 2);
        Check("image SVG data URI", DrawRunner.ToSvg(pd).Contains("data:image/png;base64,"));
        var pastePng = DrawRunner.ToPng(pd);
        Check("image PNG 非空", pastePng.Length > 0);
        var pasteDec = PngDecoder.Decode(pastePng);
        Check("image PNG 贴图红像素", pasteDec.HexAt(0, 0) == "#ff0000");
        var pc = new Canvas(10, 10, 0xFF000000);
        pc.DrawImage(pd.Figures[0].Image!, Affine.Identity, 0, 0, 10, 10);
        Check("image 贴图红", PixelAt(pc, 0, 0) == 0xFFFF0000);
        Check("image 贴图绿", PixelAt(pc, 5, 0) == 0xFF00FF00);
        Check("image 贴图蓝", PixelAt(pc, 0, 5) == 0xFF0000FF);
        Check("image 贴图白", PixelAt(pc, 9, 9) == 0xFFFFFFFF);
        // 加载失败：SVG 回退引用路径，PNG 不崩
        var pd2 = DrawRunner.Parse("canvas 10 10 #fff\nimage 0 0 5 5 \"/no/such.png\"");
        Check("image 失败 Image null", pd2.Figures[0].Image == null);
        Check("image 失败 SVG 引用路径", DrawRunner.ToSvg(pd2).Contains("/no/such.png"));
        Check("image 失败 PNG 不崩", DrawRunner.ToPng(pd2).Length > 0);
        try { File.Delete(pasteSrc); } catch { }
        Console.WriteLine();

        // ── 贴图裁剪（crop / round / rect）──
        Section("[Image.Crop]");
        var cropSrc = Path.Combine(Path.GetTempPath(), "wc_crop_" + Guid.NewGuid().ToString("N") + ".png");
        File.WriteAllBytes(cropSrc, PngEncoder.Encode(2, 2, testRgba));
        // 源图子矩形裁剪：crop 1 0 1 1 = 仅绿色像素，全链路 ToPng 验证
        var cd = DrawRunner.Parse("canvas 4 4 #000\nimage 0 0 4 4 \"" + cropSrc + "\" crop 1 0 1 1");
        Check("crop 解析 SrcX/SrcW", cd.Figures[0].SrcX == 1 && cd.Figures[0].SrcW == 1);
        var cdDec = PngDecoder.Decode(DrawRunner.ToPng(cd));
        Check("crop PNG 绿色像素", cdDec.HexAt(2, 2) == "#00ff00");
        // 圆角裁剪：round 3 裁角，角落透明露出背景、中心保留
        var rd = DrawRunner.Parse("canvas 10 10 #000\nimage 0 0 10 10 \"" + cropSrc + "\" round 3");
        Check("round 解析 CornerRadius", rd.Figures[0].CornerRadius == 3);
        var rdDec = PngDecoder.Decode(DrawRunner.ToPng(rd));
        Check("round 圆角透明（角落为背景）", rdDec.HexAt(0, 0) == "#000000");
        Check("round 中心保留（白）", rdDec.HexAt(5, 5) == "#ffffff");
        var rsvg = DrawRunner.ToSvg(rd);
        Check("round SVG 含 clipPath", rsvg.Contains("<clipPath") && rsvg.Contains("clip-path=\"url(#"));
        // 组合 crop + round 全链路不崩
        var combo = DrawRunner.Parse("canvas 8 8 #fff\nimage 0 0 8 8 \"" + cropSrc + "\" crop 0 0 1 2 round 2");
        var comboDec = PngDecoder.Decode(DrawRunner.ToPng(combo));
        Check("crop+round PNG 尺寸", comboDec.Width == 8 && comboDec.Height == 8);
        // rect 显式直角（与默认一致，无 clipPath）
        var rectDoc = DrawRunner.Parse("canvas 8 8 #fff\nimage 0 0 8 8 \"" + cropSrc + "\" rect");
        Check("rect 无圆角", rectDoc.Figures[0].CornerRadius == 0);
        Check("rect SVG 无 clipPath", !DrawRunner.ToSvg(rectDoc).Contains("<clipPath"));
        try { File.Delete(cropSrc); } catch { }
        Console.WriteLine();

        // ── 应用图标模板（icon）──
        Section("[Draw.Icon]");
        var mac = DrawRunner.Parse("icon mac");
        Check("icon mac 尺寸 1024", mac.Width == 1024 && mac.Height == 1024);
        Check("icon mac 圆角矩形背景", mac.Figures.Count == 2 && mac.Figures[0].Kind == "roundrect");
        var ios = DrawRunner.Parse("icon ios");
        Check("icon ios 方形背景", ios.Figures[0].Kind == "rect" && ios.Width == 1024);
        var andr = DrawRunner.Parse("icon android");
        Check("icon android 圆形背景", andr.Figures[0].Kind == "circle" && andr.Width == 512);
        var win = DrawRunner.Parse("icon windows");
        Check("icon windows 尺寸 256", win.Width == 256 && win.Figures[0].Kind == "roundrect");
        var custom = DrawRunner.Parse("icon mac #ff0000 道");
        Check("icon 自定义字形", custom.Figures[1].Text == "道");
        Check("icon 自定义颜色", custom.Figures[0].Fill == 0xFFFF0000);
        Check("icon 未知平台报错", DrawRunner.Parse("icon linux").Error != null);
        Check("icon SVG 含 rect", DrawRunner.ToSvg(mac).Contains("<rect"));
        var winPng = PngDecoder.Decode(DrawRunner.ToPng(win));
        Check("icon windows PNG 尺寸", winPng.Width == 256 && winPng.Height == 256);
        Check("icon windows 角落露白", winPng.HexAt(5, 5) == "#ffffff");
        Check("icon windows 顶部中部蓝", winPng.HexAt(128, 20) == "#0078d4");
        Console.WriteLine();

        TestVmlUiProtocol(Section, Check);
        Console.WriteLine();
    }

    // ═══ VML 手机端 UI 协议（对话框 / 窗体绘图 / 输入消息队列）═══
    // 这一层是纯逻辑（UI/Shared/VmlUiProtocol.cs），宿主只负责把寄存器/内存翻成这里的模型。
    // 真正容易出错的不是"能不能画出来"，而是三件事：① 号段的认领边界；② 生成的 DSL 是否落在
    // 既有解析器的语义上（错了窗口里就是一片空白，且不报错）；③ 消息队列在"连投两条"时会不会漏。

    static void TestVmlUiProtocol(Action<string> Section, Action<string, bool> Check)
    {
        Section("[VML UI 协议]");

        // ── 号段与认领判据 ──
        Check("VmlUi: 认领 500–599", VmlUi.Handles(500) && VmlUi.Handles(599));
        // **不认识必须放行** —— 处理器返回 true 就等于把那条内置 syscall 吞了
        Check("VmlUi: 不认领内置号（1/320/402）", !VmlUi.Handles(1) && !VmlUi.Handles(320) && !VmlUi.Handles(402));
        Check("VmlUi: 不认领段外相邻号（499/600）", !VmlUi.Handles(499) && !VmlUi.Handles(600));
        Check("VmlUi: 保留段恰为 100 个且都在 500–599",
            VmlUi.ReservedRange().Count() == 100 && VmlUi.ReservedRange().All(n => n is >= 500 and <= 599));

        // ── 场景 → 绘图 DSL ──
        // 关键不是"字符串长得对"，而是**生成的 DSL 能被既有解析器吃下**：
        // 场景是保留模式的图元表，最终由 DrawRunner.Parse + ToPng 出图，
        // 若这里生成的语法与解析器对不上，窗口里会是一片空白且**不报任何错**。
        var scene = new VmlScene { Width = 100, Height = 80, Background = 0xFF112233 };
        scene.AddLine(1, 2, 3, 4, 0xFFAABBCC, 3);
        scene.AddRect(10, 10, 20, 20, 0xFF00FF00, filled: true, width: 0, radius: 0);
        scene.AddRect(5, 5, 8, 8, 0xFFFF0000, filled: false, width: 2, radius: 4);
        scene.AddCircle(50, 40, 10, 0xFF0000FF, filled: true, width: 0);
        scene.AddText(7, 8, "标题", 0xFFFFFFFF, 16, 1);

        var dsl = scene.BuildDsl();
        Check("VmlScene: canvas 头带尺寸与背景", dsl.StartsWith("canvas 100 80 #FF112233"));
        Check("VmlScene: line 带颜色与线宽", dsl.Contains("line 1 2 3 4 #FFAABBCC 3"));
        // DSL 的样式规则是「第一个颜色=填充，第二个=描边」，所以空心图形必须先给一个全透明填充，
        // 否则描边色会被解析成填充 → 画出一个"实心但颜色像描边"的图形
        Check("VmlScene: 实心矩形只有一个颜色", dsl.Contains("rect 10 10 20 20 #FF00FF00"));
        Check("VmlScene: 空心矩形先透明填充再描边",
            dsl.Contains("roundrect 5 5 8 8 4 #00000000 #FFFF0000 2"));
        Check("VmlScene: 圆角矩形走 roundrect", dsl.Contains("roundrect"));

        var doc = DrawRunner.Parse(dsl);
        Check("VmlScene: 生成的 DSL 可被既有解析器完整吃下（图元数一致、无解析错误）",
            doc.Error == null && doc.Figures.Count == 5);

        // 文字里的引号/换行必须转义 —— 原文里的引号会把一行 DSL 拆坏（后续图元整条消失）
        var esc = new VmlScene();
        esc.AddText(0, 0, "a\"b\nc", 0xFFFFFFFF, 12, 0);
        Check("VmlScene: 文本引号/换行已转义（仍解析出 1 个图元）",
            DrawRunner.Parse(esc.BuildDsl()).Figures.Count == 1);

        // ── 参数防护：宿主 syscall 的参数是**程序给的**，可以是任何 int ──
        // 目标是「让异常参数最多画不出来，绝不崩」，所以这里量的正是"垃圾值进不来"。
        // 这些值若不拦，会一路进 DSL、进光栅器（int 转换、缓冲区分配），是崩宿主的入口。

        var guard = new VmlScene { Width = 200, Height = 100 };
        guard.AddRect(int.MaxValue, 0, 10, 10, 0xFFFFFFFF, true, 0, 0);
        guard.AddCircle(0, int.MinValue, 5, 0xFFFFFFFF, true, 0);
        guard.AddLine(0, 0, 0, 3_000_000, 0xFFFFFFFF, 1);
        guard.AddText(int.MaxValue, 0, "x", 0xFFFFFFFF, 12, 0);
        // ⚠ `AddImage` 的签名是 `(x, y, path, w, h)` —— 这里要测的是**越界坐标**，
        //   所以越界的必须是第 1 个参数（原先写在第 4 位 = 宽，而**宽是"钳制"不是"丢弃"**，
        //   见下面 `防护: 超大尺寸钳到窗口` 那条 —— 两条用例对同一件事的要求正好相反）。
        //   写成宽的话这条会有 1 个图元混进来，断言 `长度 == 3` 必红。
        //   （这个断言此前一直是红的，只是 `WAYCODER_TEST` 下 `SelfTest.Chunk10.cs` 编不过、
        //     整套自测根本跑不起来，所以没人看见。）
        guard.AddImage(int.MaxValue, 0, "p", 10, 10);
        // 一条都没进来 ⇒ DSL 只剩 canvas 头与 antialias 两行（Split 后还有个尾空串）
        Check("防护: 越界坐标的图元整个丢弃",
            guard.BuildDsl().Split('\n').Length == 3);

        var dim = new VmlScene { Width = 200, Height = 100 };
        dim.AddCircle(10, 10, -5, 0xFFFFFFFF, true, 0);
        Check("防护: 负半径钳成 0", dim.BuildDsl().Contains("circle 10 10 0"));
        dim.AddRect(0, 0, 99_000_000, 10, 0xFFFFFFFF, true, 0, 0);
        Check("防护: 超大尺寸钳到窗口",
            dim.BuildDsl().Contains($"rect 0 0 {VmlScene.CoordLimit} 10"));

        // 图元表封顶 —— 这是**程序漏了 ui_clear 时的宿主兜底**（v0.96.255 前 plane.c 就这么崩的）
        var capped = new VmlScene();
        for (var i = 0; i < VmlScene.MaxFigures + 500; i++) capped.AddPixel(1, 1, 0xFFFFFFFF);
        Check("防护: 图元数封顶（漏 ui_clear 也拖不垮宿主）",
            capped.BuildDsl().Split('\n').Count(l => l.StartsWith("rect ")) == VmlScene.MaxFigures);

        // 文本封顶且**按码点**截断 —— 切碎代理对会在渲染端变成 U+FFFD
        var longText = new string('a', VmlScene.MaxTextLength - 1) + "😀" + new string('b', 200);
        var tscene = new VmlScene();
        tscene.AddText(0, 0, longText, 0xFFFFFFFF, 12, 0);
        var tdsl = tscene.BuildDsl();
        Check("防护: 超长文本被截断", tdsl.Length < longText.Length);
        // ⚠ `Any` **没有带下标的替身**（那是 `Select`/`Where` 的）——
        //   这里原先写成 `tdsl.Any((c, i) => …)`，只在 `WAYCODER_TEST` 下编译，
        //   于是 `dotnet build`（Release）全绿、`dotnet run -- --test`（Debug）直接
        //   CS1593 编不过 ⇒ **整套自测跑不起来**。要下标就用 `Select((c, i) => …).Any(x => x)`。
        Check("防护: 截断不切碎代理对（无孤立代理）",
            !tdsl.Select((c, i) => char.IsHighSurrogate(c)
                                && (i + 1 >= tdsl.Length || !char.IsLowSurrogate(tdsl[i + 1]))).Any(x => x)
            && !tdsl.Select((c, i) => char.IsLowSurrogate(c)
                                && (i == 0 || !char.IsHighSurrogate(tdsl[i - 1]))).Any(x => x));

        // path 串里的换行必须抹掉：DSL 是**按行**解析的，一个 \n 就能伪造出整条指令
        var pscene = new VmlScene();
        pscene.AddPath("M0 0 L10 10\nrect 0 0 999 999 #FFFF0000", 0xFFFFFFFF);
        Check("防护: path 里的换行不产生额外图元",
            DrawRunner.Parse(pscene.BuildDsl()).Figures.Count == 1);

        // 文字锚点矩形：x 必须落在矩形的左缘 / 中心 / 右缘 —— 矢量后端靠它表达锚点。
        // ⚠ 这里锁的是"别拿 `[x, 画布右边]` 当矩形"那个 bug：那样 middle 居中的是中点而非 x，
        //   整屏按键的字会一起被拉向画布中心（真机上文字列间距只剩按键间距的一半）。
        foreach (var (anchor, frac) in new[] { ("start", 0.0), ("middle", 0.5), ("end", 1.0) })
        {
            var (bx, bw) = VmlUi.TextAnchorBox(37, 200, anchor);
            Check($"锚点矩形: {anchor} 时锚点落在矩形的 {frac:P0} 处",
                Math.Abs((37 - bx) / bw - frac) < 1e-9);
        }

        // ── 真渲染：把场景出图后逐像素验（**这条才能抓住"空心图形被画成实心白"这类问题**）──
        // 只验 DSL 字符串是不够的：语法对了但样式语义错了（例如透明填充被当成不透明），
        // 字符串看不出任何异常，真机上却是"背景被糊成白色"。所以这里走完整渲染链再量像素。
        var px = new VmlScene { Width = 64, Height = 48, Background = 0xFF101020 };
        px.AddRect(8, 8, 20, 20, 0xFFFFCC00, filled: false, width: 2, radius: 0); // 空心黄框
        px.AddCircle(44, 32, 8, 0xFF00C8FF, filled: true, width: 0);              // 实心青圆
        var raster = PngDecoder.Decode(DrawRunner.ToPng(DrawRunner.Parse(px.BuildDsl())));
        Check("VmlScene 渲染: 空心矩形内部是背景色（透明填充未被画成白色）",
            raster.HexAt(18, 18).ToLowerInvariant() == "#101020");
        // ⚠ 这条取的是**边框边缘**的像素，而场景现在开了抗锯齿（3× 超采样 + 盒式降采样）
        // —— 边缘像素必然与背景按覆盖率混色，**再断言精确十六进制就等于在断言"没有抗锯齿"**，
        // 方向反了。这里改判"黄占绝对主导"：既仍然抓得住"边框被糊成白色/背景色"，
        // 又不与抗锯齿冲突（混色后黄色分量依旧是压倒性的）。
        // ⚠ **不要写死像素坐标。** 这条原来是 `HexAt(8, 18) == "#ffcc00"` —— 一个"描边正好压在
        // 第 8 列"的假设，只在**没有抗锯齿**时成立。场景现在开了抗锯齿（3× 超采样 + 盒式降采样），
        // 描边边缘按覆盖率与背景混色，实测 x=8 是 `#af8d0a`（约 69% 覆盖）、x=9 已经是纯背景
        // ⇒ 写死坐标的断言就变成在断言"没有抗锯齿"，方向反了。
        //
        // 本意是"**这个空心矩形确实描了一圈黄边**"，所以扫一行找最黄的那个像素即可 ——
        // 与描边落在哪一列无关，也照样抓得住"边框被糊成白色/背景色"。
        int bestR = 0, bestG = 0, bestB = 0;
        for (int sx = 0; sx < raster.Width; sx++)
        {
            var h = raster.HexAt(sx, 18);
            int r = Convert.ToInt32(h.Substring(1, 2), 16);
            int g = Convert.ToInt32(h.Substring(3, 2), 16);
            int b = Convert.ToInt32(h.Substring(5, 2), 16);
            if (r + g - b > bestR + bestG - bestB) { bestR = r; bestG = g; bestB = b; }
        }
        // 抗锯齿下细描边**可能没有任何一个像素是满覆盖的**（实测这一行最黄的也只有约 69% 覆盖），
        // 所以判据是"黄占绝对主导"而非"等于纯黄"。这仍然能抓住原本要抓的两类错：
        // 边框被画成白色（三通道都高）、或根本没描边（取到的是背景 #101020）。
        Check($"VmlScene 渲染: 空心矩形边框是黄色（扫行取最黄像素，实测 #{bestR:x2}{bestG:x2}{bestB:x2}）",
            bestR >= 150 && bestG >= 110 && bestB <= 40);
        Check("VmlScene 渲染: 实心圆圆心是青色", raster.HexAt(44, 32).ToLowerInvariant() == "#00c8ff");
        Check("VmlScene 渲染: 圆外仍是背景色", raster.HexAt(44, 8).ToLowerInvariant() == "#101020");

        // ── 可用绘图区（程序据此开窗，避免超出屏幕）──
        // 单位是 dp：设备像素 ÷ 密度，再扣掉导航栏/标题/方向键/留白。
        // 扣减规则只有 VmlUi.AvailableArea 一处实现，宿主不许自己再算一份。
        var area = VmlUi.AvailableArea(1080, 2400, 2.75);
        Check("VmlUi.AvailableArea: 宽 = dp 宽 − 左右留白",
            area.Width == (int)Math.Floor(1080 / 2.75) - 16);
        // ⚠ 引用常数而**不是**再写一个字面量 —— 平行表正是本仓库头号坑
        //    （测试里写着 170、实现里改掉，两边各说各话）
        Check("VmlUi.AvailableArea: 高 = dp 高 − 固定占用（导航+标题+方向键+折叠条）",
            area.Height == (int)Math.Floor(2400 / 2.75) - VmlUi.DefaultChromeHeightDp);
        Check("VmlUi.AvailableArea: 密度非法时不炸（回退 1）",
            VmlUi.AvailableArea(300, 400, 0).Width > 0);
        Check("VmlUi.AvailableArea: 极小屏有下限（程序仍能布局）",
            VmlUi.AvailableArea(10, 10, 4).Width >= 120 && VmlUi.AvailableArea(10, 10, 4).Height >= 120);

        // **横屏扣的是宽度、不是高度**（手柄分成左右两列，TabBar 也收起来了）。
        // 拿竖屏那套常数去扣会算出一个又宽又扁的畸形区（实测 898×149），
        // 程序照着开窗 ⇒ 按比例塞回中间画布只剩几十 dp 高 = "横屏画面还是小"。
        // 判据照**实测**的横屏布局来：屏幕 914.3×411.4 → 画布 396.4×301.0。
        var land = VmlUi.AvailableArea(2400, 1080, 2.625);
        Check("VmlUi.AvailableArea: 横屏宽 = dp 宽 − 左右两列手柄",
            land.Width == (int)Math.Floor(2400 / 2.625) - VmlUi.LandscapeSideChromeDp);
        Check("VmlUi.AvailableArea: 横屏高 = dp 高 − 状态栏/导航栏/折叠条",
            land.Height == (int)Math.Floor(1080 / 2.625) - VmlUi.LandscapeChromeHeightDp);
        // 横屏那块区域必须**装得下**实测的真实画布（396×301）—— 估小了程序就会开一个
        // 偏小的窗，画面跟着小；估大一点没关系（FitSize 会等比缩回画布）。
        Check("VmlUi.AvailableArea: 横屏估算不小于实测画布（396×301）",
            land.Width >= 396 && land.Height >= 301);

        // ── 屏幕方向（`SCR_ORIENT` #569）──
        // 判定规则**只有 VmlUi.OrientationOf 一处实现**：宿主直接调它，别处不许再写一遍比较。
        Check("VmlUi.OrientationOf: 宽 > 高 = 横屏",
            VmlUi.OrientationOf(2400, 1080) == VmlUi.Landscape);
        Check("VmlUi.OrientationOf: 高 > 宽 = 竖屏",
            VmlUi.OrientationOf(1080, 2400) == VmlUi.Portrait);
        // 正方形（以及"还没量到"的 0×0）取保守的一档，不能随缘
        Check("VmlUi.OrientationOf: 正方形算竖屏（保守档）",
            VmlUi.OrientationOf(500, 500) == VmlUi.Portrait
            && VmlUi.OrientationOf(0, 0) == VmlUi.Portrait);
        // ── 场景换尺寸（转屏/收起手柄时宿主调用）──
        // `BuildDsl()` 会把 `canvas W H` 写进这一帧，而它可能在 VM 线程上被 `Present()` 调用
        // ⇒ 两个数必须一起换（分开赋值会被拍到"新宽 + 旧高"，那一帧整幅被拉伸）。
        var rs = new VmlScene { Width = 320, Height = 240 };
        rs.Resize(396, 301);
        Check("VmlScene.Resize: 宽高一起换",
            rs.Width == 396 && rs.Height == 301);
        Check("VmlScene.Resize: 新尺寸进了 DSL 的 canvas 头",
            rs.BuildDsl().StartsWith("canvas 396 301 "));
        rs.Resize(0, 100);
        Check("VmlScene.Resize: 非法尺寸被忽略（不把场景改成 0 宽）",
            rs.Width == 396 && rs.Height == 301);

        // ── 使用说明的目录 ↔ 正文文件 ──
        // ⚠ 这两边**分别**写在目录表（代码）与 `Resources/Raw/help/*.md`（文件）里，
        //   谁都编译不过才怪 —— 写错一个字母的后果是**真机上那一页一片空白**，
        //   而"空白"是最难查的一种现象。所以在这里把它们对上。
        var helpRoot = FindHelpDir();
        Check("使用说明: 找得到 Resources/Raw/help 目录（自测自己也得能定位仓库）",
            helpRoot != null);
        if (helpRoot != null)
        {
            var root = helpRoot;   // 收成非空局部量：局部函数里捕获可空变量会丢掉可空分析
            var missing = new List<string>();
            var ids = new List<string>();
            var dupCat = new List<string>();
            var seenCat = new HashSet<string>();

            foreach (var cat in HelpCatalog.Categories)
            {
                if (!seenCat.Add(cat.Key)) dupCat.Add(cat.Key);
                Walk(cat.Topics);
            }

            // 分类 key / 主题 id 都不能重 —— 重了的后果是「点了 A 打开 B」，
            // 两边都编译得过、界面也不报错，只有用户点下去才发现。
            // （⚠ 这里原先写的是 `Check(..., seen.Add(...) || true)` —— 恒为真，
            //   等于一条永远绿的断言；`FindTopic` 只返回第一个匹配，重复 id 正是靠它兜底的。）
            Check($"使用说明: 分类 key 不重复（重 {dupCat.Count}：{string.Join("/", dupCat)}）",
                dupCat.Count == 0);
            var dupId = ids.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            Check($"使用说明: 主题 id 不重复（重 {dupId.Count}：{string.Join("/", dupId)}）",
                dupId.Count == 0);

            Check($"使用说明: 目录里的每一篇都有对应的 .md（缺 {missing.Count} 篇：{string.Join("/", missing)}）",
                missing.Count == 0);

            // 目录表里的每一篇都要有正文文件（**深层的那些不在表里** ——
            // 它们由正文里的 `help:` 链接指到，见 HelpCatalog 类注释）
            void Walk(HelpCatalog.Topic[] topics)
            {
                foreach (var t in topics)
                {
                    ids.Add(t.Id);
                    if (!File.Exists(Path.Combine(root, t.Id + ".md"))) missing.Add(t.Id);
                }
            }

            // 反方向：包里有、目录里没有 = 写了没人看得到
            var onDisk = Directory.GetFiles(helpRoot, "*.md", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(helpRoot, f).Replace('\\', '/'))
                .Select(f => f[..^3])
                .ToHashSet();
            // 「能不能被看到」有**两条**路：在目录表里（关于页点得到），
            // 或被某篇正文的 `help:` 链接指着。只认其中一条就会把另一条的页面误判成孤儿。
            var linked = new HashSet<string>(StringComparer.Ordinal);
            foreach (var f in Directory.GetFiles(root, "*.md", SearchOption.AllDirectories))
                foreach (System.Text.RegularExpressions.Match m in
                         System.Text.RegularExpressions.Regex.Matches(
                             File.ReadAllText(f), @"\]\(\s*help:([^)\s]+)\s*\)"))
                    linked.Add(m.Groups[1].Value);

            var orphan = onDisk
                .Where(id => HelpCatalog.FindTopic(id) is null && !linked.Contains(id))
                .ToList();
            Check($"使用说明: 没有「放了却没人能看到」的 .md（{orphan.Count} 篇：{string.Join("/", orphan)}）",
                orphan.Count == 0);

            // 链接指向的页面**必须真的存在** —— 打错一个字母，用户看到的是"点进去一片空白"
            var deadLinks = linked.Where(id => !onDisk.Contains(id)).ToList();
            Check($"使用说明: 正文里 help: 链接的目标都存在（坏链 {deadLinks.Count}：{string.Join("/", deadLinks)}）",
                deadLinks.Count == 0);

            // ── 全局护栏：CollectionView 想收点击就必须显式写 SelectionMode ──
            // MAUI 的默认值是 `None`，而 `None` 下 `SelectionChanged` **一次都不会触发** ——
            // 现象是"点了没反应"：不报错、不崩、列表看着完全正常，只有手点下去才知道。
            // HelpListPage 与 ChatPage 的 `/` 建议列表都这么坏过（前者的跳转逻辑为此白改了一轮）。
            var pagesDir = Path.Combine(Path.GetDirectoryName(root)!, "..", "Pages");
            pagesDir = Path.GetFullPath(pagesDir);
            var offenders = new List<string>();
            if (Directory.Exists(pagesDir))
            {
                foreach (var xaml in Directory.GetFiles(pagesDir, "*.xaml"))
                {
                    var text = File.ReadAllText(xaml);
                    foreach (System.Text.RegularExpressions.Match m in
                             System.Text.RegularExpressions.Regex.Matches(
                                 text, @"<CollectionView\b[^>]*>", System.Text.RegularExpressions.RegexOptions.Singleline))
                    {
                        var tag = m.Value;
                        if (tag.Contains("SelectionChanged=") && !tag.Contains("SelectionMode="))
                            offenders.Add(Path.GetFileName(xaml));
                    }
                }
            }
            Check($"使用说明: 绑了 SelectionChanged 的 CollectionView 都写了 SelectionMode（漏 {offenders.Count}：{string.Join("/", offenders)}）",
                offenders.Count == 0);

            // 一级标题：正文自己写的那个（页面标题以它为准）
            Check("使用说明: 能从正文取到一级标题",
                HelpCatalog.HeadingOf("# C\n\n正文") == "C");
            Check("使用说明: 代码块里的 # 不算标题",
                HelpCatalog.HeadingOf("```c\n#include <x>\n```\n# 真标题") == null);
        }

        // ── 编辑器手感（TextEditAssist：自动缩进 / 括号配对 / 自动配对）──
        // 这三条下沉到 UI/Shared 就是为了**能在这里测** —— 留在 MAUI 的 EditorPage 里
        // 桌面自测一行都碰不到，而它们全是"边界一多、肉眼看不出来"的东西。
        Check("自动缩进: 继承前导空白",
            TextEditAssist.IndentForNewLine("    foo();", 4) == "    ");
        Check("自动缩进: 以 { 收尾再多一级（空格缩进）",
            TextEditAssist.IndentForNewLine("    if (x) {", 4) == "        ");
        Check("自动缩进: { 后面带空格也算（TrimEnd 之后再判）",
            TextEditAssist.IndentForNewLine("if (x) {   ", 4) == "    ");
        // ⚠ **用 tab 缩进的就加 tab**：把它换成空格，一份 tab 文件按一次回车就变味了，
        //    diff 里全是噪音 —— 这种"改一下看不出来、一提交就炸"的最坑
        Check("自动缩进: 本来用 tab 的就加 tab（不换成空格）",
            TextEditAssist.IndentForNewLine("\tif (x) {", 4) == "\t\t");
        Check("自动缩进: 顶层语句不加级",
            TextEditAssist.IndentForNewLine("foo();", 4) == "");
        Check("自动缩进: 空行 / 纯空白行不炸",
            TextEditAssist.IndentForNewLine("", 4) == ""
            && TextEditAssist.IndentForNewLine("     ", 4) == "     ");

        Check("括号配对: 光标在左括号后面 → 认左边那个（向后扫）",
            TextEditAssist.BracketAtCaret("foo()", 4) is { Col: 3, Open: '(', Close: ')', Forward: true });
        // 贴在**右**括号后面 ⇒ 认左边那个，且方向是**向前**扫（去找它的左半边）
        Check("括号配对: 光标在右括号后面 → 认左边那个（向前扫）",
            TextEditAssist.BracketAtCaret("foo()", 5) is { Col: 4, Open: ')', Close: '(', Forward: false });
        // 左边不是括号、**正下方**才是 ⇒ 认正下方那个。
        // `foo( )` 里 `)` 在下标 5：光标停在它前面（col=5）时左边是空格、不是括号。
        Check("括号配对: 正下方是括号也认（左边不是括号时）",
            TextEditAssist.BracketAtCaret("foo( )", 5) is { Col: 5, Open: ')', Close: '(', Forward: false });
        Check("括号配对: 不在括号旁返回 null",
            TextEditAssist.BracketAtCaret("foo(x)", 2) is null);
        // 嵌套：从最外层左括号出发要找**最外层**的右括号（深度计数），不是第一个遇到的 ')'
        // `f(g(x))`：下标 f0 (1 g2 (3 x4 )5 )6 —— 从最外层 ( (下标 1) 出发要找到下标 **6**，
        // 而不是第一个遇到的 `)`（下标 5）。这才是"深度计数"与"找下一个同款字符"的分水岭。
        var nested = new[] { "f(g(x))" };
        Check("括号配对: 嵌套里找的是配对的那个（深度计数）",
            TextEditAssist.MatchBracket(i => i == 0 ? nested[0] : null, 1, 0, 1, '(', ')', true, 1000)
                is { Line: 0, Col: 6 });
        // 反过来：从最外层右括号（下标 6）向前扫，要回到下标 1
        Check("括号配对: 反向也是一样的深度计数",
            TextEditAssist.MatchBracket(i => i == 0 ? nested[0] : null, 1, 0, 6, ')', '(', false, 1000)
                is { Line: 0, Col: 1 });
        // 跨行：`{` 在第 0 行、配对在第 2 行
        var lines3 = new[] { "if (x) {", "    y();", "}" };
        Check("括号配对: 跨行找得到",
            TextEditAssist.MatchBracket(i => i < lines3.Length ? lines3[i] : null, 3, 0, 7, '{', '}', true, 1000)
                is { Line: 2, Col: 0 });
        // 扫描上限：上限之内找不到就放弃（**不能变成"为了高亮把输入卡住"**）
        Check("括号配对: 超过扫描上限就放弃",
            TextEditAssist.MatchBracket(_ => new string('.', 5000) + "(", 2, 0, 0, '(', ')', true, 100) is null);
        // 取不到行（大文件 LRU 窗口外）也要安全放弃，不能当成空行继续扫
        Check("括号配对: 取不到行就放弃（不当成空行）",
            TextEditAssist.MatchBracket(_ => null, 10, 0, 0, '(', ')', true, 1000) is null);

        Check("自动配对: 开括号补右半边",
            TextEditAssist.AutoCloseFor('(', '\0', '\0') == ')'
            && TextEditAssist.AutoCloseFor('[', '\0', '\0') == ']'
            && TextEditAssist.AutoCloseFor('{', '\0', '\0') == '}');
        // ⚠ 右边紧挨着字母数字 ⇒ 是在已有内容中间插入，补上去等于把后面的劈开
        Check("自动配对: 右边紧挨着标识符不补",
            TextEditAssist.AutoCloseFor('(', '\0', 'x') is null);
        // 引号在词中间 = 撇号（don't），不是要开字符串
        Check("自动配对: 词中间的撇号不补",
            TextEditAssist.AutoCloseFor('\'', 'n', 't') is null
            && TextEditAssist.AutoCloseFor('\'', ' ', '\0') == '\'');
        Check("自动配对: 不认识的字符不补",
            TextEditAssist.AutoCloseFor('a', '\0', '\0') is null
            && TextEditAssist.AutoCloseFor(')', '\0', '\0') is null);

        // ── 全能接口（`CALLJSON` #573）──
        // 信封是**跨语言契约**：22 个前端的程序都按 {"ok":…,"result":…} 判断成败，
        // 格式一改就等于改 ABI。逐条钉住。
        VmlJsonApi.ClearForTest();
        VmlJsonApi.Register("echoTest", a => a ?? JNode.Null());
        Check("calljson: 成功信封是 {\"ok\":true,\"result\":…}",
            VmlJsonApi.Invoke("echoTest", "{\"n\":7}") == "{\"ok\":true,\"result\":{\"n\":7}}");
        Check("calljson: 不传参数时 result 为 null（不是少一个键）",
            VmlJsonApi.Invoke("echoTest", "") == "{\"ok\":true,\"result\":null}");
        // ⚠ 认不出的函数**必须回信封**，不能抛 —— 抛出去会把 VM 打挂，
        // 而程序那边只会看到"窗口没了"
        Check("calljson: 未知函数回 ok:false（不抛）",
            VmlJsonApi.Invoke("nope", "").Contains("\"ok\":false")
            && VmlJsonApi.Invoke("nope", "").Contains("未知函数"));
        Check("calljson: 参数不是合法 JSON 当场报错",
            VmlJsonApi.Invoke("echoTest", "{不是json}").Contains("参数不是合法 JSON"));
        // 实现自己抛异常同样翻成信封 —— 这是"宿主出错不能把 VM 打挂"那条约定的落点
        VmlJsonApi.Register("boomTest", _ => throw new InvalidOperationException("炸了"));
        Check("calljson: 实现抛异常翻成 ok:false（不把 VM 打挂）",
            VmlJsonApi.Invoke("boomTest", "").Contains("\"ok\":false")
            && VmlJsonApi.Invoke("boomTest", "").Contains("炸了"));
        Check("calljson: 缓冲区放不下的信封由 VmlJsonApi 一处出",
            VmlJsonApi.TooLongEnvelope(999).Contains("999")
            && VmlJsonApi.TooLongEnvelope(999).Contains("\"ok\":false"));
        // 号段查重已收口到 SelfTest.Chunk26（`VmlUi.AllNumbers` 那张清单一次性查全部号）。
        // 这里原来手写着「CallJson ≠ MsgWaitEx ≠ WinOpenEx ≠ ScrOrient」那几条 —— 是**平行表**：
        // 每加一个号就手抄一遍比对名单，而捏造漏掉的那几个永远查不到。已删。
        VmlJsonApi.ClearForTest();   // 别把测试用的函数留给后面的用例

        // ── 读消息的"读完之后留不留"（`MSG_POLL_EX` #571 / `MSG_WAIT_EX` #572）──
        var mq = new VmlMessageQueue();
        mq.Post(new VmlMessage(VmlMsgType.TouchDown, 11, 22, 0));
        mq.Post(new VmlMessage(VmlMsgType.KeyDown, 33, 0, 0));
        var keepFirst = mq.TryRead(keep: true);
        Check("消息队列 keep: 只看队头，不取走",
            keepFirst is { } k1 && k1.A == 11 && mq.Count == 2);
        var keepAgain = mq.TryRead(keep: true);
        Check("消息队列 keep: 再看还是同一条（队列一条没少）",
            keepAgain is { } k2 && k2.A == 11 && mq.Count == 2);
        // **消费一次才真的走掉** —— 这正是"先看一眼再决定谁处理"的用法
        var consumed = mq.TryRead(keep: false);
        Check("消息队列 consume: 取走队头，队列少一条",
            consumed is { } c && c.A == 11 && mq.Count == 1
            && mq.TryRead(keep: true) is { A: 33 });
        Check("VmlUi: 保留位常量为 0/1（跨语言契约）",
            VmlUi.Consume == 0 && VmlUi.Keep == 1);

        // ── 开窗的两个声明（`WIN_OPEN_EX` #570）──
        // **默认必须是"老行为"**：不做任何声明的场景 = Legacy 转屏 + 要手柄。
        // 默认写反的话，所有老程序（走的还是 #520）会突然没有手柄区、
        // 或者更糟 —— 被换掉坐标系（它们不处理 resize，内容会被裁）。
        var defScene = new VmlScene();
        Check("VmlScene: 默认声明 = Legacy（老接口行为）+ 要手柄",
            defScene.Rotation == WindowRotation.Legacy && defScene.NeedGamepad);
        // 四档必须互不相同：**"老程序"和"声明了 ROTATABLE 的程序"要能分开** ——
        // 只有"跟随/不跟随"两档的话，"跟随旋转"会被老程序也吃到，
        // 而它们不会重排版 ⇒ 坐标系被换掉、内容被裁。
        var modes = new[] { WindowRotation.Legacy, WindowRotation.Follow,
                            WindowRotation.PortraitOnly, WindowRotation.LandscapeOnly };
        Check("WindowRotation: 老/跟随/只竖屏/只横屏 四档互不相同",
            modes.Distinct().Count() == 4);
        // 只竖屏 / 只横屏 是两档**不同的**锁（不是"锁在当前方向"那种含糊语义）
        Check("WindowRotation: 只竖屏与只横屏是两档不同的锁",
            WindowRotation.PortraitOnly != WindowRotation.LandscapeOnly);
        // 三档声明值与 C 头文件 VML_WIN_* 一一对应 —— 跨语言契约，改了等于改 ABI
        Check("VmlUi: 转屏声明三档为 0/1/2（跨语言契约）",
            VmlUi.PortraitOnly == 0 && VmlUi.Rotatable == 1 && VmlUi.LandscapeOnly == 2);
        // 手柄声明同样是跨语言契约（C 头文件的 VML_WIN_* 宏按这两个数写死）
        Check("VmlUi: 手柄声明常量为 0/1（跨语言契约）",
            VmlUi.NoGamepad == 0 && VmlUi.NeedGamepad == 1);

        // 实测视口**跨方向陈旧**是实测踩到的：竖屏里打完一局退出、转到横屏再开一局，
        // 转屏期间没有绘图页在跑 ⇒ `MeasuredViewport` 还留着竖屏的 411×525，
        // 那一局照它开窗（`scene=411x525` 塞进 396×301 的画布，画面只剩中间一条）。
        // 判据：**量到的值的形状与当前方向一致**才算数，否则退回分方向的估算。
        Check("VmlUi.ViewportMatchesOrientation: 竖屏量到的值在竖屏下算数",
            VmlUi.ViewportMatchesOrientation(411, 525, VmlUi.Portrait));
        Check("VmlUi.ViewportMatchesOrientation: 竖屏量到的值在横屏下**不算数**",
            !VmlUi.ViewportMatchesOrientation(411, 525, VmlUi.Landscape));
        Check("VmlUi.ViewportMatchesOrientation: 横屏量到的值在横屏下算数",
            VmlUi.ViewportMatchesOrientation(396, 301, VmlUi.Landscape));

        // 号不能在号段外 —— 出了 500–599 就是"认领不到"（宿主根本收不到这个 syscall）。
        // 这条判据已连号段查重一起收口到 SelfTest.Chunk26。
        // 返回值 0/1 是**跨语言契约**：22 个前端的 `shared.*` 绑定、C 头文件的
        // VML_ORIENT_* 宏都按这两个数写死，改了它们等于改了 ABI。
        Check("VmlUi 方向常量为 0/1（跨语言契约）",
            VmlUi.Portrait == 0 && VmlUi.Landscape == 1);
        // 「屏幕变了」是**两条**消息：尺寸一条、方向一条。消息号也是跨语言契约
        // （C 头文件 VML_MSG_WINDOWORIENT 写死 12），不能随手挪。
        Check("VmlMsgType: 尺寸与方向是两条不同的消息",
            (int)VmlMsgType.WindowResize == 11 && (int)VmlMsgType.WindowOrient == 12);

        // ── 消息结构 ──
        var mem = new byte[64];
        new VmlMessage(VmlMsgType.TouchDown, 12, 34, 5678).WriteTo(mem, 4);
        Check("VmlMessage: 小端写入 类型/A/B/时间戳",
            System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(mem.AsSpan(4)) == (int)VmlMsgType.TouchDown
            && System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(mem.AsSpan(8)) == 12
            && System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(mem.AsSpan(12)) == 34
            && System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(mem.AsSpan(16)) == 5678);
        new VmlMessage(VmlMsgType.KeyDown, 1, 0, 0).WriteTo(mem, 60); // 末尾只剩 4 字节，放不下 16
        Check("VmlMessage: 越界写入不抛异常", true);

        // ── 消息队列 ──
        var q = new VmlMessageQueue();
        Check("VmlMessageQueue: 空队列 Count=0 / TryTake=null", q.Count == 0 && q.TryTake() == null);
        q.Post(new VmlMessage(VmlMsgType.KeyDown, VmlKeys.Left, 0, 0));
        Check("VmlMessageQueue: Post 后 Count=1 且 FIFO 取回", q.Count == 1 && q.TryTake()?.A == VmlKeys.Left);
        Check("VmlMessageQueue: Take 超时返回 null（不永久阻塞）", q.Take(10) == null);

        // 连投两条只 Release 一次信号量：**按信号量计数判断会漏掉第二条**
        var q2 = new VmlMessageQueue();
        q2.Post(new VmlMessage(VmlMsgType.KeyDown, 1, 0, 0));
        q2.Post(new VmlMessage(VmlMsgType.KeyUp, 1, 0, 0));
        var t1 = q2.Take(50);
        var t2 = q2.Take(50);
        Check("VmlMessageQueue: 连投两条都能取到（不漏消息）",
            t1?.Type == VmlMsgType.KeyDown && t2?.Type == VmlMsgType.KeyUp);
        q2.Clear();
        Check("VmlMessageQueue: Clear 后为空且不残留信号量", q2.Count == 0 && q2.Take(5) == null);

        // ── 键码约定（跨端唯一来源：绘制窗口的屏幕按键与包装库都引用它）──
        Check("VmlKeys: 沿用 Win32 虚拟键值",
            VmlKeys.Left == 37 && VmlKeys.Up == 38 && VmlKeys.Right == 39 && VmlKeys.Down == 40
            && VmlKeys.Enter == 13 && VmlKeys.Space == 32 && VmlKeys.Escape == 27);

        TestVmlFeel(Section, Check);

        TestShellCommands(Section, Check);
        TestScrollBarMath(Section, Check);
        TestAnsiMarkup(Section, Check);
        TestShellWrap(Section, Check);
        TestShellControls(Section, Check);
        TestShellSize(Section, Check);
        TestScreenOutput(Section, Check);
    }

    // ═══ 全屏输出判据：这段输出是"一屏一屏画"还是"一路往下堆" ═══
    //
    // 这条判据决定**整块输出的呈现方式**（网格 vs 折行堆叠），选错任何一边都是灾难性的：
    // 把线性输出判成全屏 ⇒ 编译日志被塞进 80×25 的格子里、前面全丢；
    // 把全屏判成线性 ⇒ 一屏画面被折行堆成几十屏（正是 nyancat 被"吃掉光标序列"后的样子）。
    static void TestScreenOutput(Action<string> Section, Action<string, bool> Check)
    {
        Section("[命令行·全屏输出判据]");

        // ── 认：光标定位 / 擦屏 ──
        Check("ScreenOut: \\x1b[H（回原点重画）算全屏", ScreenOutput.LooksFullScreen("\x1b[H"));
        Check("ScreenOut: \\x1b[2J（清屏）算全屏", ScreenOutput.LooksFullScreen("\x1b[2J"));
        Check("ScreenOut: \\x1b[10;20H（行列定位）算全屏", ScreenOutput.LooksFullScreen("\x1b[10;20H"));
        Check("ScreenOut: \\x1b[3A（光标上移）算全屏", ScreenOutput.LooksFullScreen("\x1b[3A"));
        Check("ScreenOut: \\x1b[s（存光标）算全屏", ScreenOutput.LooksFullScreen("\x1b[s"));

        // ── 不认（这几条是"别误伤"的那一侧，比认的那一侧更要紧）──
        // `ESC[K`（擦到行尾）是 `\r` 进度条的常客 —— 认了会把普通构建输出也拽进网格
        Check("ScreenOut: \\x1b[K（擦行尾）**不**算全屏（进度条靠它）",
            !ScreenOutput.LooksFullScreen("progress\x1b[K"));
        // 纯 SGR（颜色）当然不算：绝大多数输出都带颜色
        Check("ScreenOut: 纯 SGR 颜色不算全屏",
            !ScreenOutput.LooksFullScreen("\x1b[31mred\x1b[0m"));
        Check("ScreenOut: 256 色/真彩 SGR 不算全屏",
            !ScreenOutput.LooksFullScreen("\x1b[38;5;208mx\x1b[48;2;1;2;3my\x1b[0m"));
        // 私有模式（隐藏光标）单独出现说明不了什么
        Check("ScreenOut: 私有模式 \\x1b[?25l 不算全屏",
            !ScreenOutput.LooksFullScreen("\x1b[?25lhello"));
        Check("ScreenOut: 纯文本不算全屏", !ScreenOutput.LooksFullScreen("plain text\nline2"));
        Check("ScreenOut: 空/空串不算全屏",
            !ScreenOutput.LooksFullScreen("") && !ScreenOutput.LooksFullScreen(null));

        // ⚠ 输出是**按块**拿到的，最后一段可能是半个序列 ⇒ 按"还没定论"处理，
        //   绝不能把半个序列当成命中（那会让线性输出在分块边界上偶发地变成网格）。
        Check("ScreenOut: 截断的 \\x1b[ 不算全屏", !ScreenOutput.LooksFullScreen("text\x1b["));
        Check("ScreenOut: 截断的 \\x1b[10;2 不算全屏", !ScreenOutput.LooksFullScreen("text\x1b[10;2"));
        // 半个 ESC 同理
        Check("ScreenOut: 结尾孤立的 \\x1b 不算全屏", !ScreenOutput.LooksFullScreen("text\x1b"));

        // 混合：前面一堆线性输出 + 后面来一次定位 ⇒ 整体按全屏（程序确实在按坐标画）
        Check("ScreenOut: 线性输出里夹一次定位 ⇒ 算全屏",
            ScreenOutput.LooksFullScreen("building...\n\x1b[2J\x1b[Hredraw"));

        // ── 色块画面的**上游**必须保留空白 ──
        //
        // nyancat 那类程序"画"的是**带背景色的空格**（`printf("  ")` 前挂 `ESC[48;5;Nm`）。
        // 真机上整屏空白，根因在**平台渲染**（Android 的 Label 吃掉行尾空格 ⇒ 纯空格行
        // 什么都没画），修法是移动端把这种段换成不换行空格。
        // 但那条修法成立的前提是**上游一个字都没丢** —— 这里把前提钉住：
        // 转标记 → 解析回来，必须仍是"两格空格 + 那个背景色"。
        // 上游真丢了的话，移动端怎么换 NBSP 都没用（那正是本仓"改对一处 ≠ 修好了"）。
        var ansi = "\x1b[0m\x1b[48;5;17m  \x1b[0m\x1b[48;5;15m  \x1b[0m";
        var markup = AnsiMarkup.ToMarkup(ansi);
        var segs = MarkdownParser.ParseInline(markup);
        var spaceSegs = segs.Where(s => s.Text.Length > 0 && s.Text.Trim().Length == 0).ToList();
        Check("ScreenOut: 带底色的空格段在标记链上不被丢",
            spaceSegs.Count == 2 && spaceSegs.All(s => s.Text.Length == 2));
        Check("ScreenOut: 那些空格段的**背景色**还在（bg ≥ 30 才画得出色块）",
            spaceSegs.Count == 2 && spaceSegs.All(s => s.Bg >= 30));
        Check("ScreenOut: 两段背景色**不同**（同色的话两格会并成一块，画面就没形状了）",
            spaceSegs.Count == 2 && spaceSegs[0].Bg != spaceSegs[1].Bg);

        // ── 网格必须记下 **256 色 / 真彩** ──
        //
        // 这条是本轮真机空白的**真根因**：`ApplySgr` 原先对 `38/48;5;N` 与 `38/48;2;…`
        // **跳过不记**（注释写着"不做精确记录"）⇒ 一切用 256 色作画的程序（nyancat 就是）
        // 在网格里一个颜色都没有。而它们"画"的是**带背景色的空格**，底色一丢只剩空格，
        // 转成标记后整段被 TrimEnd 掉 —— 屏幕上什么都没有，连"有没有内容"都看不出来。
        var fb256 = new FrameBuffer(1, 2);
        fb256.Apply("\x1b[48;5;17m  ");
        var d256 = fb256.DumpAnsi()[0];
        // 17 → xterm256 立方 1 → rgb(0,0,95)，且**必须写成 48;2;0;0;95 这个形状**
        // （把 0x1000000|rgb 当裸码写出去是另一个意思，颜色会丢）
        Check("ScreenOut: 256 色背景被记下并转成真彩 SGR（48;2;0;0;95）",
            d256.Contains("[48;2;0;0;95m"));

        var fbTc = new FrameBuffer(1, 1);
        fbTc.Apply("\x1b[38;2;1;2;3mX");
        Check("ScreenOut: 真彩前景原样记下（38;2;1;2;3）",
            fbTc.DumpAnsi()[0].Contains("[38;2;1;2;3m"));

        // 反方向：16 色**不许**被改写成真彩（否则老程序的配色会漂）
        var fb16 = new FrameBuffer(1, 1);
        fb16.Apply("\x1b[41mX");
        Check("ScreenOut: 16 色背景仍是 41（不被改写成真彩）",
            fb16.DumpAnsi()[0].Contains("[41m") && !fb16.DumpAnsi()[0].Contains("48;2;"));
    }

    // ═══ 命令行窗口的三个尺寸模式（都不固定 / 横向固定 / 都固定） ═══
    //
    // 为什么值得单钉：这两条都是「错了也看不出来」的逻辑 ——
    // 派生错了表现为「切了档没反应」，迁移错了表现为「升级之后模式莫名其妙变了」。
    // 而它们又都长在 MAUI 那一侧（`MauiShellStore` 依赖 `Preferences`、桌面自测碰不到），
    // 所以判据落在 `UI/Shared/Terminal/ShellSize`（纯逻辑、四端同源）。
    static void TestShellSize(Action<string> Section, Action<string, bool> Check)
    {
        Section("[命令行·尺寸模式]");

        // ── 三档的派生：**生效值由模式推**，不另存一份 ──
        // 都不固定：列行都是 0（= 交给布局）
        Check("ShellSize: 都不固定 → 行列都自适应",
            ShellSize.EffectiveCols(ShellSizeMode.Auto, 80) == 0
            && ShellSize.EffectiveRows(ShellSizeMode.Auto, 25) == 0);

        // 横向固定：**列钉死、行自适应** —— 这一档正是原先两档模型里缺的那个组合
        Check("ShellSize: 横向固定 → 列钉死、行自适应",
            ShellSize.EffectiveCols(ShellSizeMode.WidthFixed, 80) == 80
            && ShellSize.EffectiveRows(ShellSizeMode.WidthFixed, 25) == 0);

        // 都固定：两轴都钉死
        Check("ShellSize: 都固定 → 行列都钉死",
            ShellSize.EffectiveCols(ShellSizeMode.Fixed, 80) == 80
            && ShellSize.EffectiveRows(ShellSizeMode.Fixed, 25) == 25);

        // ── 老配置迁移（只有"自动/固定"两档的版本）──
        // 存了 `0/0` = 老版的"自适应"
        Check("ShellSize: 老配置 0/0 → 都不固定",
            ShellSize.Migrate(0, 0) == ShellSizeMode.Auto);
        // 存了真实行列 = 老版的"固定"
        Check("ShellSize: 老配置 80/25 → 都固定",
            ShellSize.Migrate(80, 25) == ShellSizeMode.Fixed);
        // 只存了列数（不该出现，但读到了要有个确定的去处）⇒ 横向固定
        Check("ShellSize: 老配置 80/0 → 横向固定",
            ShellSize.Migrate(80, 0) == ShellSizeMode.WidthFixed);

        // 循环切换要能回到原点（否则有一档永远切不到）
        var m = ShellSizeMode.Auto;
        for (int i = 0; i < 3; i++) m = ShellSize.Next(m);
        Check("ShellSize: 循环切换三档后回到起点", m == ShellSizeMode.Auto);

        // 三档文案互不相同 —— 相同的话用户根本分不出自己在哪一档
        var texts = new[] { ShellSize.ModeText(ShellSizeMode.Auto),
                            ShellSize.ModeText(ShellSizeMode.WidthFixed),
                            ShellSize.ModeText(ShellSizeMode.Fixed) };
        Check("ShellSize: 三档文案互不相同",
            texts.Distinct().Count() == 3 && texts.All(t => t.Length > 0));
    }

    // ═══ 命令行输出里的标准控制字符（\r / \t / \b） ═══
    //
    // 这三条是**老 CLI 工具依赖的终端语义**，不是"格式化"：
    //   进度条靠 `\r` 覆写、表格靠 `\t` 对齐、老手册页的粗体靠 `\b` 叠打。
    // 不实现的话症状很显眼却容易被当成"程序输出就是这样"：一个 5 步进度条刷出 5 行、
    // 表格整片歪、正文里冒出可见的退格符。
    static void TestShellControls(Action<string> Section, Action<string, bool> Check)
    {
        Section("[命令行·控制字符]");

        // 没有控制字符时原样返回（快路径，也证明不会平白改动正文）
        Check("ShellCtl: 无控制字符原样返回",
            ShellControls.Apply("plain text") == "plain text");

        // `\r` 回行首覆写 —— 进度条就是反复 `\r` 打同一行
        Check("ShellCtl: \\r 覆写本行（进度条只留最后一帧）",
            ShellControls.Apply("aaa\rbbb") == "bbb");
        Check("ShellCtl: 连续 \\r 只留最后一段",
            ShellControls.Apply("1\r2\r3\r4") == "4");

        // ⚠ 行尾的 `\r` 是 CRLF 残留，**不是**回行首（当错了会白丢一整行输出）
        Check("ShellCtl: 行尾 \\r（CRLF）不清空本行",
            ShellControls.Apply("hello\r") == "hello");

        // ⚠ 多行输入时 `\r` **只清当前行** —— 不能把前面几行一起清掉。
        // 实测踩过：`MauiVml` 递进来的是整段多行输出，而本函数原按"单行"写的、
        // 不结算 `\n`，于是进度条那节把**自己的标题行也吃了**。
        Check("ShellCtl: 多行输入时 \\r 只清本行（不吃前面的行）",
            ShellControls.Apply("A\nB\rC") == "A\nC");

        // `\t` 跳到下一个制表位
        Check("ShellCtl: \\t 补到下一个制表位（8 的整数倍）",
            ShellControls.Apply("a\tb") == "a       b");
        Check("ShellCtl: \\t 已对齐时补满一整档",
            ShellControls.Apply("12345678\tx") == "12345678        x");

        // `\b` 退一格（老手册页的粗体 `X\bX`）
        Check("ShellCtl: \\b 吃掉前一个字符",
            ShellControls.Apply("abc\b") == "ab");
        Check("ShellCtl: 空行上的 \\b 被忽略（不抛、不产生怪字符）",
            ShellControls.Apply("\b\bx") == "x");

        // ⚠ ANSI 转义序列**零宽**：算进列数的话制表位与退格全会错位
        Check("ShellCtl: ANSI 序列不计入列宽（\\t 仍跳到第 8 列）",
            ShellControls.Apply("\x1b[31mab\x1b[0m\tc") == "\x1b[31mab\x1b[0m      c");

        // `\b` 退的是**可见字符**（`c`），ANSI 标记原样留着 —— 这正是"老手册页粗体"的形态。
        // ⚠ 必须**跳过尾部的转义序列**再退：不跳就退掉 `\x1b[0m` 里的 `m`，
        //   既没退对字符、又把颜色标记弄坏了（实测踩过）。
        Check("ShellCtl: \\b 跳过尾部 ANSI 序列、退掉可见字符",
            ShellControls.Apply("\x1b[31mabc\x1b[0m\b") == "\x1b[31mab\x1b[0m");

        // 折行**不在这里做** —— 那是 `ShellWrap` 在呈现时的活（它要避开 `«»` 标记、
        // 按显示宽度算）。这里再折一遍就是二次折行，列对齐会散。
        Check("ShellCtl: 不在这里折行（折行归 ShellWrap）",
            ShellControls.Apply("abcdef") == "abcdef");
    }

    // ═══ 命令行输出按固定列数硬折行（老程序 80×25 兼容） ═══
    static void TestShellWrap(Action<string> Section, Action<string, bool> Check)
    {
        Section("[命令行·固定列宽折行]");

        // 0 / 负数 = 不折（自适应宽度那条路，交给显示层）
        Check("ShellWrap: cols<=0 原样返回",
            ShellWrap.WrapMarkup("abcdefg", 0).Count == 1
            && ShellWrap.WrapMarkup("abcdefg", -1)[0] == "abcdefg");

        // 基本折行
        var w = ShellWrap.WrapMarkup("abcdefg", 5);
        Check("ShellWrap: 按列数折行", w.Count == 2 && w[0] == "abcde" && w[1] == "fg");

        // 全角占 2 格 —— 按**显示宽度**折，不是按字符个数
        var cjk = ShellWrap.WrapMarkup("中中中", 4);
        Check("ShellWrap: 全角按 2 格算", cjk.Count == 2 && cjk[0] == "中中" && cjk[1] == "中");

        // ⚠ 绝不能折在 «…» 标签内部 —— 劈开会让渲染端认不出，整段标记原样显示
        var tag = ShellWrap.WrapMarkup("«red»abcdef«/»", 3);
        Check("ShellWrap: 标签不被劈开（每行标记自成对）",
            tag.All(l => CountMarkup(l, '«') == CountMarkup(l, '»')));
        Check("ShellWrap: 标签不计入宽度（3 列装得下 abc）",
            tag.Count >= 2 && tag[0].EndsWith("abc", StringComparison.Ordinal));

        // «« / »» 是转义的字面量，占 1 格、不算标签开闭
        var esc = ShellWrap.WrapMarkup("a««b»»c", 10);
        Check("ShellWrap: 转义的字面书名号占 1 格且不误判为标签",
            esc.Count == 1 && esc[0] == "a««b»»c");

        // 空串与刚好一整行
        Check("ShellWrap: 空串返回一个空行", ShellWrap.WrapMarkup("", 5).Count == 1);
        Check("ShellWrap: 刚好填满不产生空尾行", ShellWrap.WrapMarkup("abcde", 5).Count == 1);

        // 字号适配：字符推进量按**实测的 0.6 倍字号**算（不是理论值 0.5，见 CharAspect 注释）
        Check("ShellWrap: 80 列铺满 480dp → 10 号字",
            Math.Abs(ShellWrap.FontSizeForColumns(480, 80) - 10) < 0.01);
        Check("ShellWrap: 列数为 0（自适应）时不改字号", ShellWrap.FontSizeForColumns(480, 0) == 0);
        Check("ShellWrap: 宽度未落定（<=0）时不改字号", ShellWrap.FontSizeForColumns(0, 80) == 0);
        Check("ShellWrap: 字号钳在下限（列数太多时不无限缩小）",
            ShellWrap.FontSizeForColumns(300, 400) == 7);

        // 自适应列数：按屏宽算，且**硬底线 32 列**（一行至少 32 个字，装不下就横向滚）
        Check("ShellWrap: 自适应列数按屏宽算（393dp / 12 号 ≈ 53 列）",
            ShellWrap.ColumnsForWidth(393, 12) is >= 50 and <= 55);
        Check("ShellWrap: 自适应列数不为 0（屏够宽时）", ShellWrap.ColumnsForWidth(393, 12) > 0);
        Check("ShellWrap: 字号放大到装不下时仍保底 32 列（而不是压到十几个字）",
            ShellWrap.ColumnsForWidth(393, 30) == ShellWrap.MinAdaptiveCols);
        Check("ShellWrap: 宽度未落定时返回 0（交给显示层，不瞎折）",
            ShellWrap.ColumnsForWidth(0, 12) == 0);

        // 内容宽度 = 列数 × 字符宽：给 Label 一个**显式宽度**，让它不再按显示宽度二次折行。
        // ⚠ 不能改用 `LineBreakMode.NoWrap` —— 那在 Android 上会 `setSingleLine(true)`，
        //   **整段只剩第一行**（实测踩过）。
        Check("ShellWrap: 内容宽度与列数成正比",
            Math.Abs(ShellWrap.WidthForColumns(80, 12) - 2 * ShellWrap.WidthForColumns(40, 12)) < 0.01);
        Check("ShellWrap: 内容宽度随字号缩放（缩放要跟着变）",
            ShellWrap.WidthForColumns(80, 24) > ShellWrap.WidthForColumns(80, 12));
        Check("ShellWrap: 列数为 0 时不给宽度（交给布局）",
            ShellWrap.WidthForColumns(0, 12) == 0);
    }

    /// <summary>数一个字符在串里出现几次（判"标记有没有被劈开"用）。</summary>
    static int CountMarkup(string s, char c)
    {
        var n = 0;
        foreach (var ch in s) if (ch == c) n++;
        return n;
    }

    // ═══ 外部命令输出的 ANSI → «» 中间格式 ═══
    //
    // 判据有两条，缺一不可：
    //  ① **颜色留下来了** —— 否则就是把原来的 StripAnsi 换个名字；
    //  ② **可见文字一字不差** —— 这条才是真判据。转义吃掉之后，正文必须与 StripAnsi
    //     的结果**逐字相同**。`«` 的转义、非 SGR 序列（光标/OSC）的剔除出错时，
    //     症状都是"少字/多字"，而看一段彩色输出时肉眼几乎发现不了。
    static void TestAnsiMarkup(Action<string> Section, Action<string, bool> Check)
    {
        Section("[ANSI→标记]");

        // 没有转义的普通文本原样返回（快路径，也证明不会平白无故加标记）
        Check("ANSI: 无转义原样返回", AnsiMarkup.ToMarkup("普通文本 abc") == "普通文本 abc");

        // 16 色前景：**走命名色**，这样能吃到渲染端那张终端标准色表
        Check("ANSI: 红色 → «red»",
            AnsiMarkup.ToMarkup("\x1b[31m红\x1b[0m") == "«red»红«/»");
        Check("ANSI: 亮色 → «bright …»",
            AnsiMarkup.ToMarkup("\x1b[92m亮\x1b[0m") == "«bright green»亮«/»");

        // 样式与颜色叠加（SGR 1;32 是最常见的一种）
        Check("ANSI: 粗体+颜色",
            AnsiMarkup.ToMarkup("\x1b[1;32m好\x1b[0m") == "«bold»«green»好«/»«/»");

        // 背景色
        Check("ANSI: 背景红 → «bg:red»",
            AnsiMarkup.ToMarkup("\x1b[41m底\x1b[0m") == "«bg:red»底«/»");

        // 256 色 / 真彩 → 十六进制（markup 只认命名色与 #rrggbb）
        Check("ANSI: 256 色 → #rrggbb",
            AnsiMarkup.ToMarkup("\x1b[38;5;208m橙\x1b[0m") == "«#ff8700»橙«/»");
        Check("ANSI: 真彩 → #rrggbb",
            AnsiMarkup.ToMarkup("\x1b[38;2;18;52;86m深蓝\x1b[0m") == "«#123456»深蓝«/»");

        // 非 SGR 的转义**吃掉、不落到正文**（光标定位 / 清屏 / OSC 标题）
        Check("ANSI: 光标与清屏序列不出现在正文",
            AnsiMarkup.ToMarkup("\x1b[2J\x1b[Ha\x1b[31mb") == "a«red»b«/»");
        Check("ANSI: OSC 标题被吃掉",
            AnsiMarkup.ToMarkup("\x1b]0;标题\x07正文") == "正文");

        // 正文里本来就是书名的字面量要被转义，否则渲染层会把 `«red»` 当成真标签吃掉
        Check("ANSI: 正文里的 «» 被转义",
            AnsiMarkup.ToMarkup("a«b»c") == "a««b»»c");

        // ── 真判据：往返 ──
        // 转成标记、再过一遍真正的解析器，可见文字必须与 StripAnsi 的结果逐字相同。
        // 这条把「转义对不对」「有没有漏字」「标记有没有被误当标签」一次全兜住。
        foreach (var (name, raw) in new[]
                 {
                     ("带色", "\x1b[31m红\x1b[0m普通\x1b[1;36m青\x1b[0m"),
                     ("带光标", "\x1b[2J\x1b[1;1H第一行\x1b[K"),
                     ("带书名号", "文件：\x1b[34m«奇怪»的名字\x1b[0m.txt"),
                     ("多行", "a\x1b[32mb\x1b[0m\nc\x1b[33md\x1b[0m"),
                 })
        {
            var got = string.Concat(MarkdownParser.ParseInline(AnsiMarkup.ToMarkup(raw))
                .Select(s => s.Text));
            Check($"ANSI 往返: 可见文字不变（{name}）", got == AnsiHelper.StripAnsi(raw));
        }

        // ── 颜色那一半：上面那条往返**只比了文字**（`Select(s => s.Text)` 把颜色丢掉了）──
        // 「转出来了、颜色丢了」这种半截故障它一条都拦不住。实测手机命令行页正是这样：
        // `AnsiMarkup` 老老实实转出了 `«red»`，而屏幕上显示的是**字面的标记文本**
        // （`«black»30 黑«/»`）。断点在「标记 → 带色段」这一段 —— 而它住在 UI/Shared、
        // 本来就桌面可测，只因为判据把颜色丢了才一直没人发现。
        var colorCase = MarkdownParser.ParseInline(
            AnsiMarkup.ToMarkup("\x1b[31m红\x1b[0m 普通 \x1b[1;32m绿\x1b[0m"));
        var lit = new StringBuilder();
        var colored = 0;
        var bolded = 0;
        foreach (var s in colorCase)
        {
            lit.Append(s.Text);
            if (s.Color != 0) colored++;
            if ((s.Color & AnsiTty.BoldFlag) != 0) bolded++;
        }
        Check("ANSI 颜色存活: 可见文字一字不差", lit.ToString() == "红 普通 绿");
        Check("ANSI 颜色存活: 至少两段带前景色", colored >= 2);
        Check("ANSI 颜色存活: 粗体位保留（«bold»+色 的编码位）", bolded >= 1);

        // 命名色是 `AnsiMarkup` 的**特意选择**（走渲染端那张终端标准色表，而不是十六进制），
        // 所以渲染端必须认识它 —— 认不出就会原样吐出来。
        var named = MarkdownParser.ParseInline("«bright red»亮红«/»");
        Check("ANSI 命名色: «bright red» 被认成一段带色文字",
            named.Count == 1 && named[0].Text == "亮红" && named[0].Color != 0);
        var namedBg = MarkdownParser.ParseInline("«bg:bright yellow»底«/»");
        Check("ANSI 命名底色: «bg:bright yellow» 被认成一段带底文字",
            namedBg.Count == 1 && namedBg[0].Text == "底" && namedBg[0].Bg != 0);
    }

    // ═══ VML 手感接口（音效 / 震动 / 持久化）的纯逻辑 ═══
    //
    // 这一组的意义全在**钳位与清洗**：号段本身没什么可测的（宿主认了号就会调到），
    // 但参数是 VML 程序给的 —— 频率传 0、时长传负数、键里塞奇怪字符，
    // 下游要么静默没反应、要么把平台存储写坏，而且**都看不出是参数问题**。
    static void TestVmlFeel(Action<string> Section, Action<string, bool> Check)
    {
        Section("VML 手感接口（音效 / 震动 / 持久化）");

        // 号：这一段用的都是 541–553，别和已有的撞（撞了 handle 里 switch 会先命中先写的那个）
        var feel = new[] { VmlUi.AudioPlay, VmlUi.AudioStop, VmlUi.AudioVolume,
                           VmlUi.Vibrate, VmlUi.VibratePattern,
                           VmlUi.StoreSet, VmlUi.StoreGet, VmlUi.StoreDel, VmlUi.ScreenKeepOn };
        Check("VmlUi: 手感接口号唯一且都在保留段内",
            feel.Distinct().Count() == feel.Length && feel.All(n => n is >= 500 and <= 599));

        // 音效走的是 **VM 内置的 #57**（宿主截住它接到真实音频），不是新号 ——
        // 这条断言钉住"只截这一个内置号"：`Handles()` 必须仍然只认 500–599，
        // 否则会把别的内置 syscall 一并吞掉（那是最难查的一类故障）。
        Check("VmlUi: 音效复用 VM 内置 #57（不新增平行接口）", VmlUi.VmSpeakerBeep == 57);
        Check("VmlUi: 截 #57 不等于把号段扩到内置区",
            !VmlUi.Handles(VmlUi.VmSpeakerBeep) && VmlUi.Handles(500) && VmlUi.Handles(599));
        Check("VmlUi: 随机/时间复用 VM 内置（#50/#53/#54），未另立接口",
            feel.All(n => n is not (50 or 51 or 53 or 54)));

        // ── 音效参数钳位 ──
        var t = VmlUi.ClampTone(440, 120, 1, 80);
        Check("ClampTone: 正常值原样通过", t == (440, 120, 1, 80));

        var lo = VmlUi.ClampTone(0, 0, -5, -1);
        Check("ClampTone: 0/负数被抬到下限（不是静默不响）",
            lo.Hz == VmlUi.ToneMinHz && lo.Ms == 1 && lo.Wave == 0 && lo.Volume == 0);

        var hi = VmlUi.ClampTone(999999, 999999, 99, 999);
        Check("ClampTone: 超上限被压回（频率/时长/波形/音量各一档）",
            hi.Hz == VmlUi.ToneMaxHz && hi.Ms == VmlUi.ToneMaxMs
            && hi.Wave == 3 && hi.Volume == 100);

        Check("ClampVolume: 只有 0–100 两档边界内", VmlUi.ClampVolume(-9) == 0 && VmlUi.ClampVolume(101) == 100
            && VmlUi.ClampVolume(50) == 50);

        // ── 震动模式钳位 ──
        var pat = VmlUi.ClampVibratePattern([0, 50, 100, 50]);
        Check("ClampVibratePattern: 正常模式原样（静/动交替）",
            pat.Length == 4 && pat.SequenceEqual(new long[] { 0, 50, 100, 50 }));

        var many = VmlUi.ClampVibratePattern(Enumerable.Repeat(10, 100).ToArray());
        Check("ClampVibratePattern: 段数截到上限（否则程序能让手机抖一分钟）",
            many.Length == VmlUi.VibrateMaxSegments);

        var longSeg = VmlUi.ClampVibratePattern([-5, 999999]);
        Check("ClampVibratePattern: 单段钳到 0..上限",
            longSeg[0] == 0 && longSeg[1] == VmlUi.VibrateMaxSegmentMs);

        Check("ClampVibratePattern: 空模式不抛（返回空数组）",
            VmlUi.ClampVibratePattern([]).Length == 0);

        // ── 持久化键清洗（与 App 自己的 Preferences 隔离）──
        Check("StoreKey: 加 vml. 前缀", VmlUi.StoreKey("highscore") == "vml.highscore");
        Check("StoreKey: 保留字母数字与 . _ -", VmlUi.StoreKey("a.b_c-d1") == "vml.a.b_c-d1");
        Check("StoreKey: 空格转下划线", VmlUi.StoreKey("my key") == "vml.my_key");
        Check("StoreKey: 丢掉落字符（键是程序起的，丢了不歧义）",
            VmlUi.StoreKey("a/b\\c:d*e") == "vml.abcde");
        Check("StoreKey: 空/纯空白/只有非法字符 → null（调用方当失败）",
            VmlUi.StoreKey("") == null && VmlUi.StoreKey("   ") == null
            && VmlUi.StoreKey(null) == null && VmlUi.StoreKey("///") == null);
        Check("StoreKey: 前后空白先裁掉", VmlUi.StoreKey("  hi  ") == "vml.hi");
    }

    // ═══ 命令行页：命令注册表 / 滚动条几何 ═══
    // 两块都是纯逻辑（UI/Shared/ShellCommands.cs、ScrollBarMath.cs），页面只负责注册与摆位置。
    // 这类代码的坑不在于"能不能跑"，而在于**边界**：帮助文本与命令表漂开、参数校验漏一边、
    // 滑块能拖出轨道、内容刚好不超屏时滚动条赖着不走。

    /// <summary>造一个注册表，执行体只把收到的参数记下来，便于断言"分派到了谁、给了什么参数"。</summary>
    static ShellCommandRegistry MakeRegistry(List<string> calls)
    {
        var reg = new ShellCommandRegistry();
        reg.Register(new ShellCommand("ping", "<文本>", "回显", "把参数原样回显。",
            a => { calls.Add("ping:" + string.Join('|', a)); return Task.FromResult("pong"); },
            MinArgs: 1, MaxArgs: 2));
        reg.Register(new ShellCommand("noop", "", "什么都不做", "占用零参数场景。",
            _ => { calls.Add("noop"); return Task.FromResult(""); }, MaxArgs: 0));
        reg.Register(new ShellCommand("any", "[...]", "参数不限", "任意个参数都收。",
            a => { calls.Add("any:" + a.Count); return Task.FromResult("ok"); }));
        // 模拟 `vml`：**返回值已经是 «» 标记**（见 ProducesMarkup 的说明）
        reg.Register(new ShellCommand("mark", "", "产出标记", "执行体自己产出 markup。",
            _ => { calls.Add("mark"); return Task.FromResult("«red»红«/»"); },
            MaxArgs: 0, ProducesMarkup: true));
        return reg;
    }

    static void TestShellCommands(Action<string> Section, Action<string, bool> Check)
    {
        Section("命令行页 · 命令注册表");

        var calls = new List<string>();
        var reg = MakeRegistry(calls);

        // 拆行
        var (name, args) = ShellCommandRegistry.Split("  vml   run   a.c  ");
        Check("ShellCmd.Split: 去多余空白，参数逐个拆开",
            name == "vml" && args.Length == 2 && args[0] == "run" && args[1] == "a.c");
        var (empty, none) = ShellCommandRegistry.Split("   ");
        Check("ShellCmd.Split: 空行拆出空命令而不是抛", empty == "" && none.Length == 0);

        // 认领边界：**不认识的必须返回 null**，页面据此交给 shell
        Check("ShellCmd: 不认识的命令返回 null（放行给 shell）",
            reg.Find("ls") == null && reg.Find(null) == null);

        // ── 输出要不要再过一遍 ANSI 转换 ──
        // 两条分支**相反**：注册表命令（`vml`）自己产出 «» 标记 ⇒ 不能再转；
        // 交给 shell 的（`ls --color`）是裸 ANSI ⇒ 必须转。
        // 实测踩过（2026-09-21）：命令行页敲 `vml run xx.c`，彩色输出被转了两遍
        // （`«` 转义成 `««`），渲染端只还原一层，**屏幕上剩下字面的 `«red»红«/»`**。
        // 断点在 MAUI 页面的调用点、桌面测不到，所以判据钉在这里：**判据本身**。
        Check("ShellCmd: 未声明产出标记的命令 → 按裸 ANSI 处理",
            !reg.ResultIsMarkup("help") && !reg.ResultIsMarkup("ls -l --color"));
        Check("ShellCmd: 声明了 ProducesMarkup 的命令 → 不再转换",
            reg.ResultIsMarkup("mark") && reg.ResultIsMarkup("mark extra-arg"));
        Check("ShellCmd: 带前导空格/CJK 参数不影响判定",
            reg.ResultIsMarkup("  mark") &&
            !reg.ResultIsMarkup("  ls -l"));
        Check("ShellCmd: 空与纯空白不算 markup",
            !reg.ResultIsMarkup(null) && !reg.ResultIsMarkup("") && !reg.ResultIsMarkup("   "));

        // 帮助开关
        Check("ShellCmd: 识别 -h / --help / /?",
            ShellCommandRegistry.IsHelpFlag("-h") && ShellCommandRegistry.IsHelpFlag("--help")
            && ShellCommandRegistry.IsHelpFlag("/?"));
        Check("ShellCmd: 普通参数不当成帮助开关（`ls -l` 不能被拦）",
            !ShellCommandRegistry.IsHelpFlag("-l") && !ShellCommandRegistry.IsHelpFlag("run")
            && !ShellCommandRegistry.HasHelpFlag(["run", "a.c"]));
        Check("ShellCmd: 参数列表里能找到帮助开关",
            ShellCommandRegistry.HasHelpFlag(["run", "-h"]));

        // 用法文本从同一条记录推出来 —— 名字/参数格式/参数个数三处都不许另写
        var ping = reg.Find("ping")!;
        Check("ShellCmd: 用法行 = 名字 + 参数格式", ping.Usage == "ping <文本>");
        Check("ShellCmd: 参数个数描述按上下界生成",
            ping.ArgsRequirement == "1~2 个参数"
            && reg.Find("noop")!.ArgsRequirement == "不带参数"
            && reg.Find("any")!.ArgsRequirement == "参数不限");

        var help = reg.HelpText();
        Check("ShellCmd: 帮助列表覆盖每一条命令",
            reg.Commands.All(c => help.Contains(c.Name, StringComparison.Ordinal)));
        Check("ShellCmd: 帮助列表说明其余输入交给 shell（不冒充认识全部命令）",
            help.Contains("shell", StringComparison.Ordinal));
        Check("ShellCmd: 单条用法含参数格式与个数说明",
            reg.UsageOf(ping).Contains("ping <文本>", StringComparison.Ordinal)
            && reg.UsageOf(ping).Contains("1~2 个参数", StringComparison.Ordinal));

        // 参数个数校验：少给/多给都要给出**带用法**的提示
        Check("ShellCmd: 参数太少报错且带用法",
            reg.Validate(ping, []) is { } e1 && e1.Contains("至少", StringComparison.Ordinal) && e1.Contains("用法", StringComparison.Ordinal));
        Check("ShellCmd: 参数太多报错",
            reg.Validate(ping, ["a", "b", "c"]) != null);
        Check("ShellCmd: 参数个数合法时放行", reg.Validate(ping, ["a"]) == null);
        Check("ShellCmd: MaxArgs=-1 表示不限，给多少都放行",
            reg.Validate(reg.Find("any")!, ["1", "2", "3", "4", "5"]) == null);

        // 分派链路
        calls.Clear();
        Check("ShellCmd: 分派执行并把参数原样交给执行体",
            reg.DispatchAsync("ping hello").GetAwaiter().GetResult() == "pong"
            && calls.Count == 1 && calls[0] == "ping:hello");
        Check("ShellCmd: 不认识的行整条放行（返回 null 而不是空串）",
            reg.DispatchAsync("ls -l /").GetAwaiter().GetResult() == null);

        // helf 开关先于参数校验：`noop -h` 有 1 个参数、而 noop 不收参数，
        // 先校验就会把"想看用法"报成"参数错了"
        calls.Clear();
        var noopHelp = reg.DispatchAsync("noop -h").GetAwaiter().GetResult();
        Check("ShellCmd: -h 先于参数校验（零参命令也能看用法）",
            noopHelp != null && noopHelp.Contains("noop", StringComparison.Ordinal) && calls.Count == 0);

        calls.Clear();
        var tooMany = reg.DispatchAsync("noop x").GetAwaiter().GetResult();
        Check("ShellCmd: 参数超限时执行体不被调用",
            tooMany != null && tooMany.Contains("不带参数", StringComparison.Ordinal) && calls.Count == 0);

        // 重名注册后者覆盖 —— 插件/测试要能顶掉内置命令
        reg.Register(new ShellCommand("ping", "<x>", "覆盖后", "覆盖后的说明。",
            _ => Task.FromResult("v2")));
        Check("ShellCmd: 重名注册以后注册的为准",
            reg.DispatchAsync("ping z").GetAwaiter().GetResult() == "v2"
            && reg.Commands.Count(c => c.Name == "ping") == 1);
    }

    static void TestScrollBarMath(Action<string> Section, Action<string, bool> Check)
    {
        Section("命令行页 · 滚动条几何");

        // 不超屏 → 不显示（用户明确要的语义）
        Check("ScrollBarMath: 内容不超视口 → 不显示",
            !ScrollBarMath.ShouldShow(100, 100) && !ScrollBarMath.ShouldShow(99, 100));
        Check("ScrollBarMath: 容量差 <1px 也算不超（浮点抖动不该冒出滚动条）",
            !ScrollBarMath.ShouldShow(100.5, 100));
        Check("ScrollBarMath: 内容超视口 → 显示", ScrollBarMath.ShouldShow(200, 100));

        // 滑块：两头都要夹住，绝不能超出轨道
        var (top0, h0) = ScrollBarMath.Thumb(1000, 100, 0, 200);
        Check("ScrollBarMath: 滚到顶 → 滑块贴顶", top0 == 0);
        // 比例要挑一个**算出来大于 MinThumb** 的场合，否则量到的是夹住的 24 而不是比例
        var (_, hProp) = ScrollBarMath.Thumb(1000, 400, 0, 200);
        Check("ScrollBarMath: 滑块长度按视口/内容比例", Math.Abs(hProp - 200 * 400.0 / 1000) < 0.001);

        var (topEnd, hEnd) = ScrollBarMath.Thumb(1000, 100, 900, 200);
        Check("ScrollBarMath: 滚到底 → 滑块贴底不越界",
            Math.Abs(topEnd + hEnd - 200) < 0.001);

        // 内容只超一点点：比例接近 1，不夹上界滑块会比轨道还长
        var (_, hTiny) = ScrollBarMath.Thumb(101, 100, 0, 200);
        Check("ScrollBarMath: 刚超一点时滑块不超出轨道", hTiny <= 200);

        // 超长内容：比例算出来会短到点不住，夹在最小长度
        var (_, hHuge) = ScrollBarMath.Thumb(1_000_000, 100, 0, 200);
        Check("ScrollBarMath: 极长内容时滑块不短于最小长度", hHuge >= ScrollBarMath.MinThumb);

        // 逆运算：拖动滑块要能换算回滚动偏移（否则只能显示、拖不动）
        Check("ScrollBarMath: 滑块顶端 ↔ 滚动偏移 互为逆运算",
            Math.Abs(ScrollBarMath.OffsetForThumbTop(topEnd, 1000, 100, 200) - 900) < 0.001);
        Check("ScrollBarMath: 逆运算在轨道外不炸（夹住而不是抛）",
            ScrollBarMath.OffsetForThumbTop(-50, 1000, 100, 200) == 0
            && Math.Abs(ScrollBarMath.OffsetForThumbTop(9999, 1000, 100, 200) - 900) < 0.001);

        // 轨道还没量出来（转屏首帧 Height=0）不该除零
        var (zTop, zH) = ScrollBarMath.Thumb(1000, 100, 0, 0);
        Check("ScrollBarMath: 轨道长为 0 时返回 0（不除零）", zTop == 0 && zH == 0);

        // 跟底判据：决定"新输出要不要自动滚" —— 用户往上翻时不能再拽他回底部
        Check("ScrollBarMath: 贴底判为跟底", ScrollBarMath.IsAtBottom(1000, 100, 900));
        Check("ScrollBarMath: 往上翻一屏就不跟底（用户正在看历史）",
            !ScrollBarMath.IsAtBottom(1000, 100, 0));
    }

    static byte[] MakeSolid(int w, int h, int r, int g, int b)
    {
        var a = new byte[w * h * 4];
        for (int i = 0; i < w * h; i++)
        {
            a[i * 4] = (byte)r; a[i * 4 + 1] = (byte)g; a[i * 4 + 2] = (byte)b; a[i * 4 + 3] = 255;
        }
        return a;
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
