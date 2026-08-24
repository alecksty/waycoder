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
