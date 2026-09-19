using System.Text;
using WayCoder.Infra;
using WayCoder.Tools;
using WayCoder.UI.Shared;

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
        Check("VmlUi: MsgPollEx/MsgWaitEx 在号段内且互不撞车",
            VmlUi.Handles(VmlUi.MsgPollEx) && VmlUi.Handles(VmlUi.MsgWaitEx)
            && VmlUi.MsgPollEx != VmlUi.MsgPoll && VmlUi.MsgWaitEx != VmlUi.MsgWait
            && VmlUi.MsgPollEx != VmlUi.MsgWaitEx
            && VmlUi.MsgPollEx != VmlUi.WinOpenEx && VmlUi.MsgWaitEx != VmlUi.ScrOrient);
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
        Check("VmlUi: WinOpenEx 在号段内、与新老号都不撞车",
            VmlUi.Handles(VmlUi.WinOpenEx) && VmlUi.WinOpenEx != VmlUi.WinOpen
            && VmlUi.WinOpenEx != VmlUi.ScrOrient && VmlUi.WinOpenEx != VmlUi.MsgClear);
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

        // 号不能在号段外 —— 出了 500–599 就是"认领不到"（宿主根本收不到这个 syscall）
        Check("VmlUi.ScrOrient 在号段内且不与其它号撞车",
            VmlUi.Handles(VmlUi.ScrOrient)
            && VmlUi.ScrOrient != VmlUi.ScrW && VmlUi.ScrOrient != VmlUi.ScrH
            && VmlUi.ScrOrient != VmlUi.MsgClear && VmlUi.ScrOrient != VmlUi.WinClosed);
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
