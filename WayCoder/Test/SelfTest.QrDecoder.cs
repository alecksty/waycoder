using WayCoder.Infra;
using WayCoder.UI.Cli.Commands;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>手写 QrDecoder 闭环自测：encode → 模块矩阵 / RGBA / PNG 文件 → decode 回读原文（零 ZXing）。</summary>
    private static void TestQrDecoder(Action<string, bool> Check)
    {
        // ── 直接模块矩阵解码（数据读取 / 格式信息 / 功能地图 / 反交错 / 字节模式）──
        var samples = new (string Text, QrEcLevel Ec, string Name)[]
        {
            ("", QrEcLevel.Medium, "空载荷"),
            ("Hello, QR!", QrEcLevel.Low, "短文本-L"),
            ("https://example.com/a?b=1&c=中文", QrEcLevel.Medium, "URL+中文-M"),
            ("你好，世界！QR 二维码解码测试。", QrEcLevel.High, "中文-H"),
            ("Special: !@#$%^&*()_+-=[]{};':\",./<>?|\\~\n\t", QrEcLevel.Quartile, "特殊字符-Q"),
            ("🎉emoji🚀和中文混排的测试载荷。", QrEcLevel.Medium, "emoji+中文"),
        };
        foreach (var (text, ec, name) in samples)
        {
            var qr = QrEncoder.EncodeText(text, ec);
            var dec = QrDecoder.DecodeMatrix(qr.Matrix);
            bool ok = dec != null && dec.Text == text && dec.Version == qr.Version && dec.EcLevel == ec;
            Check($"QRd: 矩阵解码[{name}] v{qr.Version}-{ec}", ok);
        }

        // 版本边界（M 级）：14B→v1 / 15B→v2 / 26B→v2 / 27B→v3 / 300B→高版本。
        var boundary = new (int Len, int MinV)[]
        {
            (14, 1), (15, 2), (26, 2), (27, 3), (300, 10),
        };
        foreach (var (len, minV) in boundary)
        {
            var text = new string('A', len);
            var qr = QrEncoder.EncodeText(text, QrEcLevel.Medium);
            var dec = QrDecoder.DecodeMatrix(qr.Matrix);
            bool ok = dec != null && dec.Text == text && dec.Version == qr.Version && qr.Version >= minV;
            Check($"QRd: 矩阵解码 {len}B v{qr.Version} (≥v{minV})", ok);
        }

        // ── RGBA 渲染闭环（含 quiet zone + scale），覆盖「真实图像」定位/仿射采样路径 ──
        // 覆盖 L/M/Q/H 各级 + v2/v3/v13 不同版本 + 中文/长文。
        var pngPayloads = new (string Text, QrEcLevel Ec)[]
        {
            ("sync-qr decode via rgba", QrEcLevel.Medium),
            ("PNG+相机目标：正对清晰扫码", QrEcLevel.Low),
            (new string('x', 300), QrEcLevel.Medium), // 高版本 v≥10
            (new string('B', 26), QrEcLevel.Medium),  // v2 边界
            (new string('C', 27), QrEcLevel.Medium),  // v3 边界
            ("二维码解码测试（H 级高纠错，中文与 ASCII 混排 mixed-content 0123456789）", QrEcLevel.High),
            ("QR Quartile 纠错级别下解码器应稳定工作", QrEcLevel.Quartile),
        };
        foreach (var (text, ec) in pngPayloads)
        {
            var qr = QrEncoder.EncodeText(text, ec);
            var (w, h) = RenderQrRgba(qr.Matrix, scale: 4, quiet: 4, 0, 0, out byte[] rgba);
            var dec = QrDecoder.Decode(rgba, w, h);
            bool ok = dec != null && dec.Text == text;
            Check($"QRd: RGBA 解码 v{qr.Version}-{ec} ({(text.Length > 20 ? "长文" : "短文")})", ok);
        }

        // ── 轻微偏移（整图平移进更大白画布）与缩放（scale 不同）容错 ──
        var geo = QrEncoder.EncodeText("偏移+缩放容错定位", QrEcLevel.Medium);
        foreach (var (scale, marginX, marginY) in new[] { (3, 0, 0), (5, 0, 0), (6, 7, 11), (4, 3, 5), (2, 1, 1) })
        {
            var (w, h) = RenderQrRgba(geo.Matrix, scale, quiet: 4, marginX, marginY, out byte[] rgba);
            var dec = QrDecoder.Decode(rgba, w, h);
            bool ok = dec != null && dec.Text == "偏移+缩放容错定位";
            Check($"QRd: 几何容错 scale={scale} 偏移=({marginX},{marginY})", ok);
        }

        // ── 真实 PNG 文件路径（PngEncoder 落盘 → DecodePngFile 回读）──
        var fileQr = QrEncoder.EncodeText("PNG 文件路径回读：waycoder 手写解码器", QrEcLevel.Quartile);
        string pngPath = Path.Combine(Path.GetTempPath(), "waycoder_qr_decode_" + Guid.NewGuid().ToString("N")[..8] + ".png");
        try
        {
            var (w, h) = RenderQrRgba(fileQr.Matrix, scale: 5, quiet: 4, 0, 0, out byte[] rgba);
            System.IO.File.WriteAllBytes(pngPath, PngEncoder.Encode(w, h, rgba));
            var dec = QrDecoder.DecodePngFile(pngPath);
            Check("QRd: DecodePngFile 回读原文", dec != null && dec.Text == "PNG 文件路径回读：waycoder 手写解码器");
        }
        finally { try { System.IO.File.Delete(pngPath); } catch { } }

        // ── 生产路径产物：SyncQrCommand.RenderPng（/sync-qr 实际落盘图）→ PngDecoder → Decode ──
        var cmdQr = QrEncoder.EncodeText("sync-qr 命令 PNG 产物实测解码", QrEcLevel.Medium);
        var cmdPng = SyncQrCommand.RenderPng(cmdQr.Matrix, scale: 5, quiet: 4);
        var cmdImg = PngDecoder.Decode(cmdPng);
        var cmdDec = QrDecoder.Decode(cmdImg);
        Check("QRd: SyncQrCommand.RenderPng 产物解码回读", cmdDec != null && cmdDec.Text == "sync-qr 命令 PNG 产物实测解码");

        // ── 功能图形地图与数据模块数一致性（对照 QrCodec.NumRawDataModules）──
        bool fnMapOk = true;
        for (int v = 1; v <= 40; v++)
        {
            int size = QrCodec.SizeOfVersion(v);
            var f = QrDecoder.BuildFunctionMap(v);
            int fnCount = 0;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (f[y, x]) fnCount++;
            int expectFn = size * size - QrCodec.NumRawDataModules(v);
            if (fnCount != expectFn) { fnMapOk = false; break; }
        }
        Check("QRd: 功能地图函数模块数 = size² - NumRawDataModules (v1-40)", fnMapOk);

        // ── RS 纠错（Berlekamp-Massey + Chien + Forney）：直接块级损坏测试 ──
        TestQrDecoderRs(Check);

        // ── 全 8 掩码 × 多版本矩阵解码（覆盖所有掩码条件坐标）──
        bool allMasks = true;
        for (int m = 0; m < 8; m++)
        {
            // 用不同长度文本让编码器自选版本/掩码，验证各掩码都可反掩码。
            var text = "mask#" + m + " " + new string((char)('a' + m), 5 + m * 3);
            var qr = QrEncoder.EncodeText(text, QrEcLevel.Medium);
            var dec = QrDecoder.DecodeMatrix(qr.Matrix);
            if (!(dec != null && dec.Text == text)) allMasks = false;
        }
        Check("QRd: 8 掩码矩阵解码往返一致", allMasks);
    }

    /// <summary>RS 块级损坏测试：各 (数据长度, EC 长度) 组合损坏 ≤ecLen/2 字节应全部纠回。</summary>
    private static void TestQrDecoderRs(Action<string, bool> Check)
    {
        var rnd = new Random(20240831);
        var configs = new (int DataLen, int EcLen, int Corrupt)[]
        {
            (16, 10, 5),   // v1-M
            (26, 10, 5),   // v2-L 形状
            (22, 22, 11),  // v2-Q / v4-M 类
            (9, 17, 8),    // v1-H
            (44, 18, 9),   // 高版本块形状
            (50, 30, 15),  // EC 上限 30
        };
        foreach (var (dataLen, ecLen, corrupt) in configs)
        {
            byte[] divisor = QrCodec.ComputeRsDivisor(ecLen);
            byte[] data = new byte[dataLen];
            rnd.NextBytes(data);
            byte[] ecc = QrCodec.ComputeRsRemainder(data, divisor);
            byte[] original = new byte[dataLen + ecLen];
            Array.Copy(data, 0, original, 0, dataLen);
            Array.Copy(ecc, 0, original, dataLen, ecLen);

            // 无损坏：应直接成功。
            byte[] clean = (byte[])original.Clone();
            bool cleanOk = QrDecoder.CorrectBlock(clean, ecLen) && CleanEq(clean, original);

            // 损坏 ≤ecLen/2 个随机字节（每处翻随机若干位）。
            byte[] damaged = (byte[])original.Clone();
            var positions = new HashSet<int>();
            while (positions.Count < corrupt) positions.Add(rnd.Next(original.Length));
            foreach (int p in positions)
            {
                int flips = 1 + rnd.Next(3);
                for (int f = 0; f < flips; f++) damaged[p] ^= (byte)(1 << rnd.Next(8));
            }
            bool fixOk = QrDecoder.CorrectBlock(damaged, ecLen) && CleanEq(damaged, original);

            // 损坏超过纠错能力：应报告失败（不误报成功）。
            byte[] over = (byte[])original.Clone();
            var pos2 = new HashSet<int>();
            while (pos2.Count < Math.Min(original.Length, ecLen + 1)) pos2.Add(rnd.Next(original.Length));
            foreach (int p in pos2) over[p] ^= (byte)(1 << rnd.Next(8));
            bool rejectOk = !QrDecoder.CorrectBlock(over, ecLen);

            Check($"QRd: RS 纠错 data={dataLen} ec={ecLen} 损坏{corrupt}≤{ecLen / 2} 纠回 + 超能力拒绝", cleanOk && fixOk && rejectOk);
        }
    }

    private static bool CleanEq(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    /// <summary>把模块矩阵渲染为 RGBA 图像（白底黑模块，四周 quiet+margin 模块，整体 scale 放大）。返回 (宽, 高)。</summary>
    private static (int W, int H) RenderQrRgba(bool[,] m, int scale, int quiet, int margin, out byte[] rgba)
        => RenderQrRgba(m, scale, quiet, margin, margin, out rgba);

    private static (int W, int H) RenderQrRgba(bool[,] m, int scale, int quiet, int marginX, int marginY, out byte[] rgba)
    {
        int n = m.GetLength(0);
        int qx = quiet + marginX;
        int qy = quiet + marginY;
        int fullX = n + qx * 2;
        int fullY = n + qy * 2;
        int px = fullX * scale;
        int py = fullY * scale;
        rgba = new byte[px * py * 4];
        for (int y = 0; y < fullY; y++)
            for (int x = 0; x < fullX; x++)
            {
                int mx = x - qx, my = y - qy;
                bool dark = mx >= 0 && mx < n && my >= 0 && my < n && m[my, mx];
                for (int dy = 0; dy < scale; dy++)
                    for (int dx = 0; dx < scale; dx++)
                    {
                        int xx = x * scale + dx, yy = y * scale + dy;
                        int i = (yy * px + xx) * 4;
                        rgba[i] = rgba[i + 1] = rgba[i + 2] = (byte)(dark ? 0 : 255);
                        rgba[i + 3] = 255;
                    }
            }
        return (px, py);
    }
}
