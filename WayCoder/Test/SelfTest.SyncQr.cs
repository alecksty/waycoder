using WayCoder.Infra;
using WayCoder.UI.Cli.Commands;
using WayCoder.UI.Tui.Screens;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>同步二维码：手写 QrEncoder 编码矩阵 + PngEncoder PNG 渲染（/sync-qr 命令核心逻辑，零 ZXing）。</summary>
    private static void TestSyncQr(Action<string, bool> Check)
    {
        var payload = "{\"url\":\"https://gitee.com/a/b.git\",\"user\":\"u\",\"token\":\"t\"}";
        var qr = QrEncoder.EncodeText(payload, QrEcLevel.Medium);
        int size = qr.Size;
        Check("QR: 尺寸 = 21+4*(版本-1)", size == 21 + 4 * (qr.Version - 1));
        Check("QR: 版本自动选择", qr.Version >= 1 && qr.Version <= 40);
        Check("QR: 掩码在 0-7", qr.Mask is >= 0 and <= 7);
        Check("QR: EC 级别为 M", qr.EcLevel == QrEcLevel.Medium);

        var m = qr.Matrix;
        Check("QR: 矩阵含深色模块", HasDarkModule(m));

        // ── 结构自检：finder / timing / 恒黑模块 / 格式信息（不依赖 ZXing）──
        // finder 左上角中心 (3,3)：9x9 区内 dark 当 max(|dx|,|dy|) ∉ {2,4}。
        Check("QR: finder 中心黑", m[3, 3]);
        Check("QR: finder 外边框黑(顶部)", m[0, 3]);
        Check("QR: finder 白环(距 2)", !m[1, 3]);
        Check("QR: finder 核心黑(2,2)", m[2, 2]);
        // 3 个 finder 分布：右上中心 (size-4,3)、左下中心 (3,size-4)。
        Check("QR: 右上 finder 中心黑", m[3, size - 4]);
        Check("QR: 左下 finder 中心黑", m[size - 4, 3]);

        // 时序图案：行 y=6 与列 x=6 从 (6,6) 起黑亮交替（偶数黑）。
        Check("QR: 水平时序 x=8 黑", m[6, 8]);
        Check("QR: 水平时序 x=9 白", !m[6, 9]);
        Check("QR: 垂直时序 y=8 黑", m[8, 6]);
        Check("QR: 垂直时序 y=9 白", !m[9, 6]);

        // 恒黑模块：位于 (8, size-8)（格式信息副本 2 末尾）。
        Check("QR: 恒黑模块(8,size-8) 黑", m[size - 8, 8]);

        // 格式信息第一副本 15 位 + 恒黑模块位置与 BCH 计算一致。
        Check("QR: 格式信息 15 位位置正确", FormatFirstCopyMatches(m, qr));

        // ── 真实渲染方法（SyncQrCommand.RenderAscii / RenderPng）验证：窄版 + 小 PNG ──
        string ascii = SyncQrCommand.RenderAscii(m);
        string[] asciiLines = ascii.TrimEnd('\n').Split('\n');
        Check("QR: ASCII 半块宽度 = 模块+安静区4", asciiLines.Length > 0 && asciiLines[0].Length == size + 4);
        Check("QR: ASCII 行数半高", asciiLines.Length == (size + 5) / 2);
        Check("QR: ASCII 半块宽度 < 旧 2 字符宽", asciiLines[0].Length < size * 2);
        Check("QR: ASCII 含黑半块字符", ascii.Contains('█') || ascii.Contains('▀'));

        var png = SyncQrCommand.RenderPng(m, scale: 5, quiet: 4);
        Check("QR: PNG 头有效", png.Length > 100 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47);
        int w = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        Check("QR: PNG 宽 = (模块+8)*scale", w == (size + 8) * 5);
        Check("QR: PNG 数据合理", png.Length > 1000);
    }

    /// <summary>手写 QR 编码器纯函数/容量/版本边界自检（零 ZXing）。</summary>
    private static void TestQrEncoder(Action<string, bool> Check)
    {
        // ── QrCodec 容量表（ISO/IEC 18004 已知锚点）──
        Check("QRc: v1-L 数据码字 19", QrCodec.NumDataCodewords(QrEcLevel.Low, 1) == 19);
        Check("QRc: v1-M 数据码字 16", QrCodec.NumDataCodewords(QrEcLevel.Medium, 1) == 16);
        Check("QRc: v1-Q 数据码字 13", QrCodec.NumDataCodewords(QrEcLevel.Quartile, 1) == 13);
        Check("QRc: v1-H 数据码字 9", QrCodec.NumDataCodewords(QrEcLevel.High, 1) == 9);
        Check("QRc: v40-L 数据码字 2956", QrCodec.NumDataCodewords(QrEcLevel.Low, 40) == 2956);
        Check("QRc: v1 原始码字 26 / v40 3706", QrCodec.NumRawCodewords(1) == 26 && QrCodec.NumRawCodewords(40) == 3706);
        Check("QRc: v1 尺寸 21 / v40 177", QrCodec.SizeOfVersion(1) == 21 && QrCodec.SizeOfVersion(40) == 177);

        // ── 版本字节容量边界（M 级，字节模式）──
        Check("QRc: v1-M 字节容量 14", QrCodec.ByteModeCapacity(QrEcLevel.Medium, 1) == 14);
        Check("QRc: v2-M 字节容量 26", QrCodec.ByteModeCapacity(QrEcLevel.Medium, 2) == 26);
        Check("QRc: 14B→v1-M", QrEncoder.Encode(new byte[14], QrEcLevel.Medium).Version == 1);
        Check("QRc: 15B→v2-M", QrEncoder.Encode(new byte[15], QrEcLevel.Medium).Version == 2);
        Check("QRc: 26B→v2-M", QrEncoder.Encode(new byte[26], QrEcLevel.Medium).Version == 2);
        Check("QRc: 27B→v3-M", QrEncoder.Encode(new byte[27], QrEcLevel.Medium).Version == 3);
        Check("QRc: 空载荷→v1", QrEncoder.EncodeText("", QrEcLevel.Medium).Version == 1);

        // 300 字节超过 8-bit 字符计数（255）→ 必须走 16-bit 计数（版本 ≥ 10）。
        Check("QRc: 300B 版本≥10", QrEncoder.Encode(new byte[300], QrEcLevel.Medium).Version >= 10);

        // 超出版本 40-M 容量抛异常。
        bool threw = false;
        try { QrEncoder.Encode(new byte[3000], QrEcLevel.Medium); }
        catch (ArgumentException) { threw = true; }
        Check("QRc: 3000B 超容量抛 ArgumentException", threw);

        // ── 对齐图案坐标（已知锚点）──
        Check("QRc: v1 无对齐图案", QrCodec.GetAlignmentPatternPositions(1).Length == 0);
        var p2 = QrCodec.GetAlignmentPatternPositions(2);
        Check("QRc: v2 对齐 {6,18}", p2.Length == 2 && p2[0] == 6 && p2[1] == 18);
        var p7 = QrCodec.GetAlignmentPatternPositions(7);
        Check("QRc: v7 对齐 {6,22,38}", p7.Length == 3 && p7[0] == 6 && p7[1] == 22 && p7[2] == 38);

        // ── 格式信息 BCH 往返（每个 EC × 掩码 0-7）──
        bool fmtAll = true;
        for (int e = 0; e < 4; e++)
            for (int mk = 0; mk < 8; mk++)
            {
                var ecl = (QrEcLevel)e;
                if (!QrCodec.TryDecodeFormatInfo(QrCodec.FormatInfoBits(ecl, mk), out var de, out var dm)
                    || de != ecl || dm != mk)
                    fmtAll = false;
            }
        Check("QRc: 32 组格式信息 BCH 往返一致", fmtAll);

        // 格式信息值 < 2^15 且唯一。
        var seen = new HashSet<int>();
        bool unique = true;
        for (int mk = 0; mk < 8; mk++)
            if (!seen.Add(QrCodec.FormatInfoBits(QrEcLevel.Medium, mk)))
                unique = false;
        Check("QRc: 同 EC 8 掩码格式码唯一", unique && QrCodec.FormatInfoBits(QrEcLevel.Medium, 0) < (1 << 15));

        // ── 掩码 pattern 已知坐标 ──
        Check("QRc: mask0 (x+y)%2 在(1,0)翻转", QrCodec.MaskCondition(1, 0, 0) == ((1 + 0) % 2 == 0));
        Check("QRc: mask2 x%3 在(0,9)翻转", QrCodec.MaskCondition(0, 9, 2) == (0 % 3 == 0));
        Check("QRc: mask3 (x+y)%3 在(3,0)不翻转", QrCodec.MaskCondition(3, 0, 3) == ((3 + 0) % 3 == 0));

        // ── quiet zone 外扩 ──
        var tiny = QrEncoder.EncodeText("a", QrEcLevel.Medium);
        var qz = QrEncoder.AddQuietZone(tiny.Matrix, 4);
        Check("QRc: quiet zone 外扩尺寸 +8", qz.GetLength(0) == tiny.Size + 8 && qz.GetLength(1) == tiny.Size + 8);
        Check("QRc: quiet zone 边角白", !qz[0, 0] && !qz[0, qz.GetLength(1) - 1] && !qz[qz.GetLength(0) - 1, 0]);
        Check("QRc: quiet zone 内部保留黑", qz[4 + 3, 4 + 3] == tiny.Matrix[3, 3]);
    }

    /// <summary>全屏扫码屏（QrScanScreen）：布局计算纯函数 + 半块字符 + quiet zone（Esc/q 返回交互属 UI 目视，不自动测）。</summary>
    private static void TestQrScanScreen(Action<string, bool> Check)
    {
        Check("QR屏: quiet zone 常量 = 4", QrScanScreen.Quiet == 4);

        // ── ComputeLayout：全块方形（模块 2s 列 × s 行）──
        var full = QrScanScreen.ComputeLayout(29, 80, 30); // grid=29（v1+8 quiet）
        Check("QR屏: 80x30 下 29 网格走全块 s=1",
            full is { Mode: QrScanScreen.QrRenderMode.FullBlock, Scale: 1, BoxCols: 58, BoxRows: 29 });

        var fullUp = QrScanScreen.ComputeLayout(29, 200, 60); // 等比放大到 s=2
        Check("QR屏: 200x60 下 29 网格全块放大 s=2",
            fullUp is { Mode: QrScanScreen.QrRenderMode.FullBlock, Scale: 2, BoxCols: 116, BoxRows: 58 });
        Check("QR屏: 全块放大不超屏", fullUp!.BoxCols <= 200 && fullUp.BoxRows <= 60);

        // ── ComputeLayout：全块放不下 → 半块兜底（1 列/模块，2 行压 1 行）──
        var half = QrScanScreen.ComputeLayout(45, 80, 30); // grid=45 全块需 90 列或 45 行 → 放不下
        Check("QR屏: 80x30 下 45 网格退化半块",
            half is { Mode: QrScanScreen.QrRenderMode.HalfBlock, Scale: 1, BoxCols: 45, BoxRows: 23 });

        // ── ComputeLayout：超屏放不下 → null（不渲染残缺 QR）──
        Check("QR屏: 100 网格 80x30 放不下返回 null",
            QrScanScreen.ComputeLayout(100, 80, 30) == null);
        Check("QR屏: 极小终端返回 null",
            QrScanScreen.ComputeLayout(29, 10, 5) == null);
        Check("QR屏: 非法入参返回 null",
            QrScanScreen.ComputeLayout(0, 80, 30) == null);

        // ── HalfBlockChar：半块单格字符映射（█/▀/▄/空格）──
        Check("QR屏: 半块 上下皆黑=█", QrScanScreen.HalfBlockChar(true, true) == '█');
        Check("QR屏: 半块 上黑下白=▀", QrScanScreen.HalfBlockChar(true, false) == '▀');
        Check("QR屏: 半块 上白下黑=▄", QrScanScreen.HalfBlockChar(false, true) == '▄');
        Check("QR屏: 半块 上下皆白=空格", QrScanScreen.HalfBlockChar(false, false) == ' ');

        // ── CanFit：/sync-qr 预判（v1 矩阵 21 → grid 29，80x24 半块可容）──
        var qr = QrEncoder.EncodeText("{\"url\":\"https://gitee.com/a/b.git\",\"user\":\"u\",\"token\":\"t\"}", QrEcLevel.Medium);
        Check("QR屏: v1 矩阵 80x24 可容", QrScanScreen.CanFit(qr.Matrix, 80, 24));
        Check("QR屏: v1 矩阵 10x5 不可容", !QrScanScreen.CanFit(qr.Matrix, 10, 5));
        Check("QR屏: 含 quiet 网格 = size+8", QrScanScreen.ComputeLayout(qr.Size + 8, 80, 24) != null);

        // ── 渲染冒烟（白底黑块，ANSI 背景色填充，与 PNG 同对比——不依赖终端默认背景/不反色）──
        RenderQrScreenSmoke(Check, qr.Matrix, (100, 30));          // 全块方形：白底+黑底序列
        RenderQrScreenSmoke(Check, CheckerMatrix(37), (100, 30));  // 37 网格 → 半块兜底：黑前景+白底序列
        RenderQrScreenSmoke(Check, qr.Matrix, (40, 8));            // 放不下：屏内提示（不渲染残缺 QR）
    }

    /// <summary>棋盘矩阵（非合法 QR，仅渲染冒烟用）：(x+y)%2==0 为黑。</summary>
    private static bool[,] CheckerMatrix(int n)
    {
        var m = new bool[n, n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                m[y, x] = (x + y) % 2 == 0;
        return m;
    }

    /// <summary>QrScanScreen 无头渲染冒烟：Activate → Render，断言输出含 TrueColor 白底/黑块 ANSI。</summary>
    private static void RenderQrScreenSmoke(Action<string, bool> Check, bool[,] matrix, (int W, int H) term)
    {
        string frame = "";
        var saved = WayCoder.UI.Shared.Terminal.Tty.SizeOverride;
        try
        {
            WayCoder.UI.Shared.Terminal.Tty.SizeOverride = term;
            var sc = new QrScanScreen(matrix);
            sc.Activate();
            var sb = new System.Text.StringBuilder();
            sc.Render(sb);
            frame = sb.ToString();
        }
        catch (Exception ex)
        {
            Check($"QR屏 渲染不崩溃 @{term.W}x{term.H}: {ex.GetType().Name}", false);
            WayCoder.UI.Shared.Terminal.Tty.SizeOverride = saved;
            return;
        }
        finally { WayCoder.UI.Shared.Terminal.Tty.SizeOverride = saved; }

        bool white = frame.Contains("[48;2;255;255;255"); // 白底背景填充
        bool black = frame.Contains("[48;2;0;0;0");       // 黑模块背景填充
        bool blackFg = frame.Contains("[38;2;0;0;0");     // 半块黑字形
        bool fits = QrScanScreen.CanFit(matrix, term.W, term.H);

        if (fits)
        {
            Check($"QR屏 渲染 {term.W}x{term.H} 含白底 ANSI", white);
            // 全块方形用黑背景铺块、半块用黑前景字形；两种都要求白底（不反色/不靠终端默认背景）
            bool blockMode = QrScanScreen.ComputeLayout(matrix.GetLength(0) + 8, term.W, term.H) is { Mode: QrScanScreen.QrRenderMode.FullBlock };
            Check($"QR屏 渲染 {term.W}x{term.H} 黑模块 ANSI", blockMode ? black : blackFg);
        }
        else
        {
            // 放不下：屏内提示而非残缺图形 → 不应出现大面积白底填充，但要有提示文本
            Check($"QR屏 放不下 {term.W}x{term.H} 不画白底块", !white);
            Check($"QR屏 放不下 {term.W}x{term.H} 含提示", frame.Contains("二维码") && frame.Contains("sync-qr.png"));
        }
    }

    private static bool HasDarkModule(bool[,] m)
    {
        int n = m.GetLength(0);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                if (m[y, x]) return true;
        return false;
    }

    /// <summary>断言格式信息第一副本 15 位 + 恒黑模块与 BCH 计算值逐位一致。</summary>
    private static bool FormatFirstCopyMatches(bool[,] m, QrCodeResult qr)
    {
        int size = qr.Size;
        int fb = QrCodec.FormatInfoBits(qr.EcLevel, qr.Mask);
        bool Bit(int i) => ((fb >> i) & 1) != 0;

        for (int i = 0; i <= 5; i++)
            if (m[i, 8] != Bit(i)) return false;
        if (m[7, 8] != Bit(6)) return false;
        if (m[8, 8] != Bit(7)) return false;
        if (m[8, 7] != Bit(8)) return false;
        for (int i = 9; i <= 14; i++)
            if (m[8, 14 - i] != Bit(i)) return false;
        return true;
    }
}
