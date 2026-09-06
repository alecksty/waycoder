namespace WayCoder.Infra;

/// <summary>QR 解码结果。</summary>
public sealed class QrDecodeResult
{
    /// <summary>解码出的文本（字节模式 → UTF-8 解码）。</summary>
    public string Text { get; }
    /// <summary>版本（1-40）。</summary>
    public int Version { get; }
    /// <summary>纠错级别。</summary>
    public QrEcLevel EcLevel { get; }

    public QrDecodeResult(string text, int version, QrEcLevel ecl)
    {
        Text = text;
        Version = version;
        EcLevel = ecl;
    }
}

/// <summary>
/// 手写标准 QR（Model 2）解码器：RGBA 图像 → 模块矩阵 → 数据位 → RS 纠错 → 字节模式文本。
/// 与 <see cref="QrEncoder"/> 严格对称（零 ZXing、零依赖、纯托管、AOT 安全）。
/// 定位使用 1:1:3:1:1 finder 比率扫描（行 RLE + 连通聚类 + 纵/横交叉校验），
/// 采样使用 3 finder 定义的两条模轴做仿射（平行四边形）网格采样，正对 + 轻微缩放/偏移/少量旋转可容。
/// 图像输入约定：黑色模块为暗、白色为亮（标准黑底白面），含 quiet zone 更佳。
/// 已知限制（后续增强）：极端透视梯形、强反光/模糊/低对比照明、倒置（白底黑面）与 90° 旋转暂不支持。
/// </summary>
public static class QrDecoder
{
    /// <summary>解码 RGBA 像素缓冲（每像素 4 字节 RGBA）。失败返回 null。</summary>
    public static QrDecodeResult? Decode(byte[] rgba, int width, int height)
    {
        if (rgba == null) throw new ArgumentNullException(nameof(rgba));
        if (width <= 0 || height <= 0) throw new ArgumentException("宽高必须为正整数");
        if (rgba.Length < (long)width * height * 4) throw new ArgumentException("像素缓冲长度不足");

        bool[,] dark = Binarize(rgba, width, height);
        var finders = FindFinderCandidates(dark, width, height);
        if (finders == null || finders.Count < 3) return null;

        // 按几何合法性打分排序，优先尝试最优三元组（防御少量虚假候选）。
        var triples = ScoreTriples(finders);
        if (triples.Count == 0) return null;
        int attempts = Math.Min(triples.Count, 6);
        for (int t = 0; t < attempts; t++)
        {
            var g = triples[t];
            foreach (int version in CandidateVersions(g))
            {
                var matrix = SampleMatrix(dark, width, height, g, version);
                if (matrix == null) continue;
                var res = DecodeMatrix(matrix);
                if (res != null) return res;
            }
        }
        return null;
    }

    /// <summary>便捷：解码 PNG 文件路径。文件读取异常向上抛，解码失败返回 null。</summary>
    public static QrDecodeResult? DecodePngFile(string path)
    {
        if (string.IsNullOrEmpty(path)) throw new ArgumentException("路径为空");
        var img = PngDecoder.Decode(System.IO.File.ReadAllBytes(path));
        return Decode(img.Rgba, img.Width, img.Height);
    }

    /// <summary>便捷：解码 <see cref="RasterImage"/>（PngDecoder/抓屏等产出的通用位图载体）。</summary>
    public static QrDecodeResult? Decode(RasterImage image)
    {
        if (image == null) throw new ArgumentNullException(nameof(image));
        return Decode(image.Rgba, image.Width, image.Height);
    }

    /// <summary>便捷：直接解码模块矩阵（[y,x]，true=黑，尺寸 = 版本尺寸，无 quiet zone）。供测试/已定位场景。</summary>
    public static QrDecodeResult? DecodeMatrix(bool[,] matrix)
    {
        if (matrix == null) return null;
        int size = matrix.GetLength(0);
        if (size != matrix.GetLength(1)) return null;
        if (size < QrCodec.SizeOfVersion(1) || size > QrCodec.SizeOfVersion(40)) return null;
        if ((size - 17) % 4 != 0) return null;
        int version = (size - 17) / 4;
        if (version < 1 || version > 40) return null;
        return DecodeMatrixAtSize(matrix, version);
    }

    /// <summary>
    /// Reed-Solomon 纠错（就地修正）：给定块码字（数据||校验）与校验码字数 nsym，
    /// 成功（含无需纠错）返回 true 并就地修正；错误超过 nsym/2 或不可纠返回 false。
    /// 与 <see cref="QrCodec.ComputeRsDivisor"/>（生成元根 α^0..α^(nsym-1)）严格对称。
    /// </summary>
    public static bool CorrectBlock(byte[] codewords, int nsym)
    {
        if (codewords == null || nsym < 1 || nsym >= codewords.Length) return false;
        int n = codewords.Length;

        // ── syndromes S_i = r(α^i)，i=0..nsym-1 ──
        var synd = new byte[nsym];
        bool hasError = false;
        for (int i = 0; i < nsym; i++)
        {
            int s = 0;
            for (int j = 0; j < n; j++)
            {
                if (codewords[j] == 0) continue;
                int expo = (i * (n - 1 - j)) % 255;
                s ^= QrCodec.GfMul(codewords[j], QrCodec.GfExp(expo));
            }
            synd[i] = (byte)s;
            if (s != 0) hasError = true;
        }
        if (!hasError) return true;

        // ── Berlekamp–Massey：求错误位置多项式 Λ(x) ──
        byte[]? lambda = BerlekampMassey(synd);
        if (lambda == null) return false;
        int L = lambda.Length - 1;
        if (L == 0) return false;          // syndromes 有错但定位零次 = 不可纠
        if (L > nsym / 2) return false;    // 超过纠错能力

        // ── Chien 搜索：找出错误码字下标 ──
        int[] errPos = new int[L];
        int errCnt = 0;
        for (int e = 0; e < n; e++)
        {
            // 候选错误位置指数 e（= x 的幂）。校验 Λ(α^-e) == 0。
            int invE = (255 - e) % 255;
            if (EvalPolyAt(lambda, invE) == 0)
            {
                if (errCnt >= L) return false;
                errPos[errCnt++] = n - 1 - e;
            }
        }
        if (errCnt != L) return false;

        // ── Forney：错误幅度。Ω(x) = S(x)·Λ(x) mod x^nsym；Λ'(x) 形式导数 ──
        byte[] omega = new byte[nsym];
        for (int i = 0; i < nsym; i++)
        {
            if (synd[i] == 0) continue;
            for (int j = 0; j < lambda.Length && i + j < nsym; j++)
            {
                if (lambda[j] == 0) continue;
                omega[i + j] ^= QrCodec.GfMul(synd[i], lambda[j]);
            }
        }
        byte[] lambdaPrime = new byte[Math.Max(0, lambda.Length - 1)];
        for (int i = 1; i < lambda.Length; i++)
            if ((i & 1) == 1)
                lambdaPrime[i - 1] = lambda[i];

        for (int k = 0; k < errCnt; k++)
        {
            int j = errPos[k];
            int e = n - 1 - j;               // 该码字对应的 x 幂
            int invE = (255 - e) % 255;
            int xVal = QrCodec.GfExp(e % 255);
            int om = EvalPolyAt(omega, invE);
            int lp = EvalPolyAt(lambdaPrime, invE);
            if (lp == 0) return false;
            // GF(2^8) 特征 2：e_l = X_l·Ω(X_l^-1)/Λ'(X_l^-1)（负号即加法）
            int mag = QrCodec.GfMul(xVal, om);
            mag = QrCodec.GfMul(mag, GfInv((byte)lp));
            codewords[j] ^= (byte)mag;
        }
        return true;
    }

    // ═══════════════════════ 二值化 ═══════════════════════

    /// <summary>RGBA → 二值 [y,x]（true=黑）。Otsu 全局阈值（双峰稳健；均匀光照下相机帧亦可）。</summary>
    private static bool[,] Binarize(byte[] rgba, int width, int height)
    {
        int count = width * height;
        var lum = new byte[count];
        var hist = new int[256];
        for (int i = 0; i < count; i++)
        {
            int o = i * 4;
            int luma = (rgba[o] * 299 + rgba[o + 1] * 587 + rgba[o + 2] * 114) / 1000;
            lum[i] = (byte)luma;
            hist[luma]++;
        }

        // Otsu：最大化类间方差。
        long total = count;
        long sum = 0;
        for (int i = 0; i < 256; i++) sum += (long)hist[i] * i;
        long sumB = 0, wB = 0;
        double maxVar = -1;
        int threshold = 128;
        for (int t = 0; t < 256; t++)
        {
            wB += hist[t];
            if (wB == 0) continue;
            long wF = total - wB;
            if (wF == 0) break;
            sumB += (long)t * hist[t];
            double mB = (double)sumB / wB;
            double mF = (double)(sum - sumB) / wF;
            double between = wB * wF * (mB - mF) * (mB - mF);
            // 用 >= 取「类间方差最大」中偏后的阈值：纯黑白两值直方图对所有 0..254
            // 截断方差相等，若用 > 会永远停在 t=0 导致所有像素被判白（本仓库自渲染 PNG 即纯两值）。
            if (between >= maxVar) { maxVar = between; threshold = t; }
        }
        if (maxVar <= 0) threshold = 128; // 均匀图兜底

        var dark = new bool[height, width];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                dark[y, x] = lum[y * width + x] < threshold;
        return dark;
    }

    // ═══════════════════════ finder 定位 ═══════════════════════

    private readonly struct PointMatch
    {
        public readonly int X, Y;
        public readonly double Module;
        public PointMatch(int x, int y, double module) { X = x; Y = y; Module = module; }
    }

    /// <summary>finder 候选中心。</summary>
    private readonly struct FinderCandidate
    {
        public readonly double X, Y, Module;
        public readonly int Count;
        public FinderCandidate(double x, double y, double module, int count) { X = x; Y = y; Module = module; Count = count; }
    }

    /// <summary>几何合法的 finder 三元组（TL=左上，TR=右上，BL=左下）。</summary>
    private sealed class QrGeometry
    {
        public double TlX, TlY, TrX, TrY, BlX, BlY;
        public double Module;
        public double Score;
    }

    private static List<FinderCandidate>? FindFinderCandidates(bool[,] dark, int width, int height)
    {
        // ── 1) 水平扫描：行 RLE 找 1:1:3:1:1（B W B W B），记录中心 x ──
        var matches = new List<PointMatch>();
        for (int y = 0; y < height; y++)
            ScanRow(dark, width, y, matches);

        if (matches.Count == 0) return null;

        // ── 2) 二维连通聚类（finder 中心列会给出连续 ~3 模块高的匹配带）──
        var set = new HashSet<long>();
        var moduleAt = new Dictionary<long, double>();
        foreach (var pm in matches)
        {
            long key = (long)pm.Y * width + pm.X;
            set.Add(key);
            moduleAt[key] = pm.Module;
        }

        var visited = new HashSet<long>();
        var comps = new List<FinderCandidate>();
        foreach (long seed in set)
        {
            if (visited.Contains(seed)) continue;
            var stack = new Stack<long>();
            stack.Push(seed);
            visited.Add(seed);
            double sx = 0, sy = 0, sm = 0;
            int minY = int.MaxValue, maxY = int.MinValue, cnt = 0;
            while (stack.Count > 0)
            {
                long key = stack.Pop();
                int px = (int)(key % width);
                int py = (int)(key / width);
                sx += px; sy += py; sm += moduleAt[key];
                cnt++;
                if (py < minY) minY = py;
                if (py > maxY) maxY = py;
                // 5×5 邻域找同 finder 点（dx≤2, dy≤2）
                for (int dy = -2; dy <= 2; dy++)
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        long nk = (long)(py + dy) * width + (px + dx);
                        if (nk >= 0 && set.Contains(nk) && visited.Add(nk))
                            stack.Push(nk);
                    }
            }
            if (cnt < 3) continue; // 真 finder 中心带至少 3 行
            double cx = sx / cnt;
            double cy = (minY + maxY) / 2.0;
            comps.Add(new FinderCandidate(cx, cy, sm / cnt, cnt));
        }

        // ── 3) 纵向交叉校验：列 RLE 在该 (cx,cy) 处应见同比率，细化中心 ──
        var result = new List<FinderCandidate>();
        foreach (var c in comps)
        {
            int cxInt = (int)Math.Round(c.X);
            var vert = CheckFinderVertical(dark, width, height, cxInt, c.Y, c.Module);
            if (vert == null) continue;
            double cyV = vert.Value.Item1;
            double m = vert.Value.Item2;
            result.Add(new FinderCandidate(c.X, cyV, m, c.Count));
        }
        return result;
    }

    private static double? TryFinderRuns(int[] runColors, int[] runLens, int i)
    {
        // 需要 runs[i..i+4] = B W B W B；左右邻须为白（quiet zone / 隔离）
        if (!(runColors[i] == 1 && runColors[i + 1] == 0 && runColors[i + 2] == 1 &&
              runColors[i + 3] == 0 && runColors[i + 4] == 1)) return null;
        if (i > 0 && runColors[i - 1] != 0) return null;
        if (i + 5 < runColors.Length && runColors[i + 5] != 0) return null;

        long total = runLens[i] + runLens[i + 1] + runLens[i + 2] + runLens[i + 3] + runLens[i + 4];
        if (total < 7) return null;
        int module = (int)(total / 7);
        if (module < 1) return null;
        int tol = module / 2;
        if (tol < 1) tol = 1;
        int c0 = runLens[i], c1 = runLens[i + 1], c2 = runLens[i + 2], c3 = runLens[i + 3], c4 = runLens[i + 4];
        if (Math.Abs(c0 - module) > tol) return null;
        if (Math.Abs(c1 - module) > tol) return null;
        if (Math.Abs(c3 - module) > tol) return null;
        if (Math.Abs(c4 - module) > tol) return null;
        if (Math.Abs(c2 - 3 * module) > tol * 3) return null;
        return total / 7.0;
    }

    /// <summary>对行 y 做 RLE 并找出全部 finder 水平匹配，追加到 matches。</summary>
    private static void ScanRow(bool[,] dark, int width, int y, List<PointMatch> matches)
    {
        var starts = new List<int>(64);
        var lens = new List<int>(64);
        var colors = new List<int>(64);
        int s = 0;
        bool cur = dark[y, 0];
        for (int x = 1; x <= width; x++)
        {
            bool c = x < width && dark[y, x];
            if (x == width || c != cur)
            {
                starts.Add(s); lens.Add(x - s); colors.Add(cur ? 1 : 0);
                s = x; cur = c;
            }
        }
        int[] rl = lens.ToArray();
        int[] rc = colors.ToArray();
        for (int i = 0; i + 4 < rc.Length; i++)
        {
            double? m = TryFinderRuns(rc, rl, i);
            if (m != null)
            {
                int cx = starts[i + 2] + rl[i + 2] / 2;
                matches.Add(new PointMatch(cx, y, m.Value));
            }
        }
    }

    /// <summary>列交叉校验：在列 x 上找含 cy 附近的 finder 纵向比率，返回 (细化中心 y, 模块尺寸)。</summary>
    private static (double, double)? CheckFinderVertical(bool[,] dark, int width, int height, int x, double cy, double m)
    {
        if (x < 0 || x >= width) return null;
        var starts = new List<int>(64);
        var lens = new List<int>(64);
        var colors = new List<int>(64);
        int s = 0;
        bool cur = dark[0, x];
        for (int y = 1; y <= height; y++)
        {
            bool c = y < height && dark[y, x];
            if (y == height || c != cur)
            {
                starts.Add(s); lens.Add(y - s); colors.Add(cur ? 1 : 0);
                s = y; cur = c;
            }
        }
        int[] rl = lens.ToArray();
        int[] rc = colors.ToArray();
        double best = double.MaxValue;
        double bestCy = 0, bestM = 0;
        int tol = Math.Max(3, (int)Math.Round(m * 1.5));
        for (int i = 0; i + 4 < rc.Length; i++)
        {
            double? mm = TryFinderRuns(rc, rl, i);
            if (mm == null) continue;
            int midStart = starts[i + 2], midEnd = starts[i + 2] + rl[i + 2];
            double center = (midStart + midEnd) / 2.0;
            double dist = Math.Abs(center - cy);
            if (dist <= tol && dist < best)
            {
                best = dist;
                bestCy = center;
                bestM = mm.Value;
            }
        }
        if (best == double.MaxValue) return null;
        return (bestCy, bestM);
    }

    // ═══════════════════════ 三元组选择 / 版本 ═══════════════════════

    private static List<QrGeometry> ScoreTriples(List<FinderCandidate> finders)
    {
        var result = new List<QrGeometry>();
        int n = finders.Count;
        for (int a = 0; a < n; a++)
        {
            for (int b = a + 1; b < n; b++)
            {
                for (int c = b + 1; c < n; c++)
                {
                    var f = new[] { finders[a], finders[b], finders[c] };
                    // 枚举哪个是直角角点（TL），其余为 TR/BL。
                    for (int cornerIdx = 0; cornerIdx < 3; cornerIdx++)
                    {
                        var cor = f[cornerIdx];
                        var p = f[(cornerIdx + 1) % 3];
                        var q = f[(cornerIdx + 2) % 3];
                        double leg1 = Dist(p.X - cor.X, p.Y - cor.Y);
                        double leg2 = Dist(q.X - cor.X, q.Y - cor.Y);
                        double legMax = Math.Max(leg1, leg2);
                        if (legMax < 8) continue;
                        // QR 方形：两腰应近似相等
                        double errLen = Math.Abs(leg1 - leg2) / legMax;
                        if (errLen > 0.15) continue;
                        // 两腰近似垂直（正对 QR 两腰分别水平/垂直）
                        double dot = (p.X - cor.X) * (q.X - cor.X) + (p.Y - cor.Y) * (q.Y - cor.Y);
                        double errDot = Math.Abs(dot) / (legMax * legMax);
                        if (errDot > 0.12) continue;
                        double score = errLen + errDot;

                        // 划分 TR / BL：QR 直立时 TR 的 X 更大、BL 的 Y 更大。
                        double trx, try_, blx, bly;
                        if (Math.Abs(p.X - cor.X) >= Math.Abs(q.X - cor.X))
                        {
                            trx = p.X; try_ = p.Y; blx = q.X; bly = q.Y;
                        }
                        else
                        {
                            trx = q.X; try_ = q.Y; blx = p.X; bly = p.Y;
                        }
                        var geom = new QrGeometry
                        {
                            TlX = cor.X, TlY = cor.Y,
                            TrX = trx, TrY = try_,
                            BlX = blx, BlY = bly,
                            Module = (cor.Module + p.Module + q.Module) / 3.0,
                            Score = score,
                        };
                        result.Add(geom);
                    }
                }
            }
        }
        result.Sort((g1, g2) => g1.Score.CompareTo(g2.Score));
        return result;
    }

    /// <summary>由两腰像素长度与模块尺寸推断候选版本（含 ± 容差）。</summary>
    private static IEnumerable<int> CandidateVersions(QrGeometry g)
    {
        var list = new List<int>();
        double legH = Dist(g.TrX - g.TlX, g.TrY - g.TlY);
        double legV = Dist(g.BlX - g.TlX, g.BlY - g.TlY);
        double m = g.Module;
        if (m < 0.5) m = Math.Max(legH, legV) / 25.0; // 兜底估计
        int sH = (int)Math.Round(legH / m) + 7;
        int sV = (int)Math.Round(legV / m) + 7;
        var seeds = new[] { sH, sV };
        foreach (int s in seeds)
        {
            int v = (int)Math.Round((s - 17) / 4.0);
            for (int dv = -1; dv <= 1; dv++)
            {
                int vv = v + dv;
                if (vv >= 1 && vv <= 40 && !list.Contains(vv)) list.Add(vv);
            }
        }
        list.Sort();
        return list;
    }

    private static double Dist(double dx, double dy) => Math.Sqrt(dx * dx + dy * dy);

    // ═══════════════════════ 仿射采样 ═══════════════════════

    /// <summary>
    /// 由 TL/TR/BL 定义两条模轴，仿射采样出 size×size 模块矩阵。
    /// TL 中心位于矩阵模块 (3,3)，TR 位于 (size-4,3)，BL 位于 (3,size-4)。
    /// </summary>
    private static bool[,]? SampleMatrix(bool[,] dark, int width, int height, QrGeometry g, int version)
    {
        int size = QrCodec.SizeOfVersion(version);
        double denom = size - 7;
        // 水平模步（TL→TR）
        double sx = (g.TrX - g.TlX) / denom;
        double sy = (g.TrY - g.TlY) / denom;
        // 垂直模步（TL→BL）
        double ux = (g.BlX - g.TlX) / denom;
        double uy = (g.BlY - g.TlY) / denom;

        var m = new bool[size, size];
        for (int yy = 0; yy < size; yy++)
        {
            for (int xx = 0; xx < size; xx++)
            {
                double px = g.TlX + (xx - 3.0) * sx + (yy - 3.0) * ux;
                double py = g.TlY + (xx - 3.0) * sy + (yy - 3.0) * uy;
                int x = (int)Math.Round(px);
                int y = (int)Math.Round(py);
                if (x < 0 || y < 0 || x >= width || y >= height) return null;
                m[yy, xx] = dark[y, x];
            }
        }
        return m;
    }

    // ═══════════════════════ 模块矩阵 → 数据位 ═══════════════════════

    private static QrDecodeResult? DecodeMatrixAtSize(bool[,] matrix, int version)
    {
        int size = matrix.GetLength(0);

        // ── 格式信息（两副本任取，容 1 位错）──
        if (!TryReadFormatInfo(matrix, size, out QrEcLevel ecl, out int mask)) return null;

        // ── 功能图形地图（与编码器严格同构）──
        bool[,] isFunction = BuildFunctionMap(version);

        // ── 之字形读取数据位 + 反掩码 → 全部码字（数据||EC 交错）──
        int rawCodewords = QrCodec.NumRawCodewords(version);
        byte[]? all = ReadCodewords(matrix, isFunction, size, rawCodewords, mask);
        if (all == null) return null;

        // ── 反交错 → 块 → RS 纠错 ──
        var layout = QrCodec.GetBlockLayout(ecl, version);
        byte[]? data = DeinterleaveAndCorrect(all, layout);
        if (data == null) return null;

        // ── 字节模式解析 ──
        return ParseByteMode(data, version, ecl);
    }

    private static bool TryReadFormatInfo(bool[,] matrix, int size, out QrEcLevel ecl, out int mask)
    {
        int copy1 = 0;
        for (int i = 0; i <= 5; i++) if (matrix[i, 8]) copy1 |= 1 << i;
        if (matrix[7, 8]) copy1 |= 1 << 6;
        if (matrix[8, 8]) copy1 |= 1 << 7;
        if (matrix[8, 7]) copy1 |= 1 << 8;
        for (int i = 9; i <= 14; i++) if (matrix[8, 14 - i]) copy1 |= 1 << i;

        int copy2 = 0;
        for (int i = 0; i <= 7; i++) if (matrix[8, size - 1 - i]) copy2 |= 1 << i;
        for (int i = 8; i <= 14; i++) if (matrix[size - 15 + i, 8]) copy2 |= 1 << i;

        if (QrCodec.TryDecodeFormatInfo(copy1, out ecl, out mask)) return true;
        if (QrCodec.TryDecodeFormatInfo(copy2, out ecl, out mask)) return true;
        return false;
    }

    /// <summary>读数据位：之字形逆序遍历（与 <see cref="QrEncoder"/> 的 DrawCodewords 同序），跳过功能图形，反掩码。</summary>
    private static byte[]? ReadCodewords(bool[,] matrix, bool[,] isFunction, int size, int rawCodewords, int mask)
    {
        int totalBits = rawCodewords * 8;
        var all = new byte[rawCodewords];
        int i = 0;
        int right = size - 1;
        while (right > 0)
        {
            int r = right;
            if (r <= 6) r -= 1; // 跳过垂直时序列 x=6
            for (int vert = 0; vert < size; vert++)
            {
                bool upward = ((r + 1) & 2) == 0;
                int y = upward ? size - 1 - vert : vert;
                for (int j = 0; j < 2; j++)
                {
                    int x = r - j;
                    if (isFunction[y, x]) continue;
                    if (i >= totalBits) continue; // remainder 位忽略
                    bool bit = matrix[y, x];
                    if (QrCodec.MaskCondition(x, y, mask)) bit = !bit;
                    if (bit) all[i >> 3] |= (byte)(1 << (7 - (i & 7)));
                    i++;
                }
            }
            right -= 2;
        }
        if (i < totalBits) return null; // 功能地图与数据模块数不一致
        return all;
    }

    /// <summary>反交错成块 → RS 纠错 → 拼接数据码字。失败返回 null。</summary>
    private static byte[]? DeinterleaveAndCorrect(byte[] all, QrBlockLayout layout)
    {
        int ecLen = layout.EcLen;
        int numBlocks = layout.NumBlocks;
        int shortData = layout.ShortDataLen;
        int padCol = layout.ShortTotalLen - ecLen; // == shortData

        var dataBlocks = new byte[numBlocks][];
        var eccBlocks = new byte[numBlocks][];
        for (int j = 0; j < numBlocks; j++)
        {
            int dataLen = shortData + (j < layout.NumShortBlocks ? 0 : 1);
            dataBlocks[j] = new byte[dataLen];
            eccBlocks[j] = new byte[ecLen];
        }

        int idx = 0;
        for (int i = 0; i <= layout.ShortTotalLen; i++)
        {
            for (int j = 0; j < numBlocks; j++)
            {
                if (i == padCol && j < layout.NumShortBlocks) continue; // 短块填充列不入流
                if (idx >= all.Length) return null;
                byte b = all[idx++];
                if (i < padCol) dataBlocks[j][i] = b;
                else if (i == padCol) dataBlocks[j][shortData] = b; // 仅长块
                else eccBlocks[j][i - padCol - 1] = b;
            }
        }

        var total = new byte[layout.TotalDataLen];
        int tp = 0;
        for (int j = 0; j < numBlocks; j++)
        {
            int dataLen = dataBlocks[j].Length;
            var block = new byte[dataLen + ecLen];
            Array.Copy(dataBlocks[j], 0, block, 0, dataLen);
            Array.Copy(eccBlocks[j], 0, block, dataLen, ecLen);
            if (!CorrectBlock(block, ecLen)) return null;
            Array.Copy(block, 0, total, tp, dataLen);
            tp += dataLen;
        }
        return total;
    }

    /// <summary>解析字节模式（0100）：4bit 模式 + 8/16bit 字符计数 + 字节 → UTF-8。</summary>
    private static QrDecodeResult? ParseByteMode(byte[] data, int version, QrEcLevel ecl)
    {
        int bitPos = 0;
        int mode = ReadBits(data, ref bitPos, 4);
        if (mode == 0b0000) return new QrDecodeResult("", version, ecl); // 立即终止符
        if (mode != 0b0100) return null; // 本批仅字节模式（numeric/alnum/kanzi 后续）

        int countBits = version <= 9 ? 8 : 16;
        int length = ReadBits(data, ref bitPos, countBits);
        if (length < 0 || length > 2956) return null;
        if (length > data.Length) return null; // 字节模式计数 = 数据字节数

        var payload = new byte[length];
        for (int i = 0; i < length; i++)
        {
            int v = ReadBits(data, ref bitPos, 8);
            payload[i] = (byte)v;
        }
        string text = System.Text.Encoding.UTF8.GetString(payload);
        return new QrDecodeResult(text, version, ecl);
    }

    private static int ReadBits(byte[] data, ref int bitPos, int count)
    {
        int v = 0;
        for (int k = 0; k < count; k++)
        {
            int byteIdx = bitPos >> 3;
            if (byteIdx >= data.Length) return -1;
            int bit = (data[byteIdx] >> (7 - (bitPos & 7))) & 1;
            v = (v << 1) | bit;
            bitPos++;
        }
        return v;
    }

    // ═══════════════════════ 功能图形地图（与编码器同构）══════════════════════

    internal static bool[,] BuildFunctionMap(int version)
    {
        int size = QrCodec.SizeOfVersion(version);
        var f = new bool[size, size];

        // 时序行/列（先标记整行整列，后续 finder/对齐覆盖同格无碍——并集一致）。
        for (int i = 0; i < size; i++)
        {
            f[i, 6] = true;  // 列 x=6
            f[6, i] = true;  // 行 y=6
        }

        MarkFinder(f, 3, 3, size);
        MarkFinder(f, size - 4, 3, size);
        MarkFinder(f, 3, size - 4, size);

        int[] pos = QrCodec.GetAlignmentPatternPositions(version);
        int na = pos.Length;
        for (int i = 0; i < na; i++)
            for (int j = 0; j < na; j++)
            {
                bool skip = (i == 0 && j == 0) || (i == 0 && j == na - 1) || (i == na - 1 && j == 0);
                if (!skip) MarkAlignment(f, pos[i], pos[j], size);
            }

        // 格式信息第一副本
        for (int i = 0; i <= 5; i++) f[i, 8] = true;
        f[7, 8] = true;
        f[8, 8] = true;
        f[8, 7] = true;
        for (int i = 9; i <= 14; i++) f[8, 14 - i] = true;
        // 格式信息第二副本
        for (int i = 0; i <= 7; i++) f[8, size - 1 - i] = true;
        for (int i = 8; i <= 14; i++) f[size - 15 + i, 8] = true;
        f[size - 8, 8] = true; // 恒黑模块

        // 版本信息（≥7）
        if (version >= 7)
        {
            for (int i = 0; i < 18; i++)
            {
                int a = size - 11 + i % 3;
                int b = i / 3;
                f[b, a] = true;
                f[a, b] = true;
            }
        }
        return f;
    }

    private static void MarkFinder(bool[,] f, int cx, int cy, int size)
    {
        for (int dy = -4; dy <= 4; dy++)
            for (int dx = -4; dx <= 4; dx++)
            {
                int x = cx + dx, y = cy + dy;
                if ((uint)x < (uint)size && (uint)y < (uint)size) f[y, x] = true;
            }
    }

    private static void MarkAlignment(bool[,] f, int cx, int cy, int size)
    {
        for (int dy = -2; dy <= 2; dy++)
            for (int dx = -2; dx <= 2; dx++)
            {
                int x = cx + dx, y = cy + dy;
                if ((uint)x < (uint)size && (uint)y < (uint)size) f[y, x] = true;
            }
    }

    // ═══════════════════════ Reed-Solomon 子算法 ═══════════════════════

    private static byte[]? BerlekampMassey(byte[] synd)
    {
        int nsym = synd.Length;
        int L = 0, m = 1;
        byte bVal = 1;
        var lambda = new byte[] { 1 };
        var bArr = new byte[] { 1 };

        for (int n = 0; n < nsym; n++)
        {
            int delta = synd[n];
            for (int i = 1; i <= L; i++)
            {
                if (i >= lambda.Length) break;
                if (lambda[i] == 0 || n - i < 0 || synd[n - i] == 0) continue;
                delta ^= QrCodec.GfMul(lambda[i], synd[n - i]);
            }
            if (delta == 0) { m++; continue; }

            byte[] t = (byte[])lambda.Clone();
            byte coef = GfDiv((byte)delta, bVal);
            int need = bArr.Length + m;
            if (lambda.Length < need) Array.Resize(ref lambda, need);
            for (int i = 0; i < bArr.Length; i++)
            {
                if (bArr[i] == 0) continue;
                int gi = i + m;
                if (gi >= lambda.Length) Array.Resize(ref lambda, Math.Max(lambda.Length * 2, gi + 1));
                lambda[gi] ^= QrCodec.GfMul(coef, bArr[i]);
            }
            if (2 * L <= n)
            {
                L = n + 1 - L;
                bArr = t;
                bVal = (byte)delta;
                m = 1;
            }
            else m++;
        }

        if (L + 1 > lambda.Length) return null;
        var trimmed = new byte[L + 1];
        Array.Copy(lambda, trimmed, L + 1);
        return trimmed;
    }

    private static int EvalPolyAt(byte[] poly, int expX)
    {
        int sum = 0;
        for (int i = 0; i < poly.Length; i++)
        {
            if (poly[i] == 0) continue;
            int pow = (expX * i) % 255;
            sum ^= QrCodec.GfMul(poly[i], QrCodec.GfExp(pow));
        }
        return sum;
    }

    private static byte GfDiv(byte a, byte b)
    {
        if (a == 0) return 0;
        int e = (QrCodec.GfLog(a) - QrCodec.GfLog(b)) % 255;
        if (e < 0) e += 255;
        return QrCodec.GfExp(e);
    }

    private static byte GfInv(byte b)
    {
        if (b == 0) return 0;
        return QrCodec.GfExp(255 - QrCodec.GfLog(b));
    }
}
