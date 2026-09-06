namespace WayCoder.Infra;

/// <summary>QR 编码结果。Matrix 为 [y, x] 模块矩阵（true=黑），尺寸 = 21+4*(version-1)，不含 quiet zone。</summary>
public readonly struct QrCodeResult
{
    /// <summary>模块矩阵 [y, x]（首维 y=行，次维 x=列），true=黑。</summary>
    public readonly bool[,] Matrix;
    /// <summary>选定版本（1-40）。</summary>
    public readonly int Version;
    /// <summary>纠错级别。</summary>
    public readonly QrEcLevel EcLevel;
    /// <summary>选定掩码号（0-7）。</summary>
    public readonly int Mask;

    /// <summary>矩阵边长（模块数）。</summary>
    public int Size => Matrix.GetLength(0);

    public QrCodeResult(bool[,] matrix, int version, QrEcLevel ecl, int mask)
    {
        Matrix = matrix;
        Version = version;
        EcLevel = ecl;
        Mask = mask;
    }
}

/// <summary>
/// 手写标准 QR（Model 2）编码器：UTF-8 字节 + 纠错级别 → 模块矩阵（ISO/IEC 18004）。
/// 零依赖、纯托管、AOT 安全。版本自动选择（1-40，字节模式），Reed-Solomon 纠错，
/// 8 种掩码惩罚择优，格式/版本信息 BCH 写入。矩阵不含 quiet zone（调用方按需外扩）。
/// </summary>
public static class QrEncoder
{
    /// <summary>
    /// 编码 UTF-8 字节为 QR 模块矩阵（true=黑）。自动选最小版本（1-40），给定纠错级别原样采用（不抬升）。
    /// 数据超出版本 40 容量抛 <see cref="ArgumentException"/>。
    /// </summary>
    public static QrCodeResult Encode(byte[] data, QrEcLevel ecl = QrEcLevel.Medium)
    {
        data ??= Array.Empty<byte>();
        if ((int)ecl < 0 || (int)ecl > 3)
            throw new ArgumentOutOfRangeException(nameof(ecl));

        // 1. 版本自动选择：字节模式 = 4bit 模式指示 + countbits + 8bit/字节。
        int version = 0;
        for (int v = QrCodec.MinVersion; v <= QrCodec.MaxVersion; v++)
        {
            long bits = 4L + (v <= 9 ? 8 : 16) + (long)data.Length * 8;
            if (bits <= (long)QrCodec.NumDataCodewords(ecl, v) * 8)
            {
                version = v;
                break;
            }
        }
        if (version == 0)
            throw new ArgumentException($"QR 载荷过长：{data.Length} 字节超出版本 40-{ecl} 字节模式容量。");

        int dataCodewords = QrCodec.NumDataCodewords(ecl, version);
        int size = QrCodec.SizeOfVersion(version);

        // 2. 数据位流：模式 0100 + 字符计数 + 数据 + 终止符 + 填充码字。
        var bb = new List<bool>(dataCodewords * 8);
        AppendBits(bb, 0b0100, 4);
        AppendBits(bb, data.Length, version <= 9 ? 8 : 16);
        foreach (byte b in data)
            AppendBits(bb, b, 8);
        int capBits = dataCodewords * 8;
        int rem = capBits - bb.Count;
        AppendBits(bb, 0, Math.Min(4, rem));
        while (bb.Count % 8 != 0)
            bb.Add(false);
        int pi = 0;
        while (bb.Count < capBits)
            AppendBits(bb, (pi++ & 1) == 0 ? 0xEC : 0x11, 8);

        var dataBytes = new byte[dataCodewords];
        for (int i = 0; i < bb.Count; i++)
        {
            if (bb[i])
                dataBytes[i >> 3] |= (byte)(1 << (7 - (i & 7)));
        }

        // 3. 画功能图形（模块/函数标注矩阵 [y,x]）。
        var modules = new bool[size, size];
        var isFunction = new bool[size, size];
        DrawFunctionPatterns(modules, isFunction, version, size, ecl);

        // 4. 分块 + Reed-Solomon 校验 + 交错。
        byte[] allCodewords = AddEccAndInterleave(dataBytes, version, ecl);

        // 5. 之字形放置数据位。
        DrawCodewords(modules, isFunction, allCodewords, size);

        // 6. 掩码择优。
        int bestMask = ChooseBestMask(modules, isFunction, size, ecl);

        return new QrCodeResult(modules, version, ecl, bestMask);
    }

    /// <summary>便捷：UTF-8 文本 → QR。先编码为 UTF-8 字节再调用 <see cref="Encode"/>。</summary>
    public static QrCodeResult EncodeText(string text, QrEcLevel ecl = QrEcLevel.Medium)
        => Encode(string.IsNullOrEmpty(text) ? Array.Empty<byte>() : System.Text.Encoding.UTF8.GetBytes(text), ecl);

    /// <summary>在矩阵四周外扩 quiet zone（白边，默认 4 模块）返回更大矩阵。方便 ASCII/PNG 渲染前加白边。</summary>
    public static bool[,] AddQuietZone(bool[,] matrix, int quiet = 4)
    {
        if (quiet < 0)
            throw new ArgumentOutOfRangeException(nameof(quiet));
        int n = matrix.GetLength(0);
        int m = matrix.GetLength(1);
        var result = new bool[n + quiet * 2, m + quiet * 2];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < m; x++)
                result[y + quiet, x + quiet] = matrix[y, x];
        return result;
    }

    // ───────────────────────── 功能图形 ─────────────────────────

    private static void DrawFunctionPatterns(bool[,] modules, bool[,] isFunction, int version, int size, QrEcLevel ecl)
    {
        // 时序图案（先画整行/列，finder 之后覆盖部分）。
        for (int i = 0; i < size; i++)
        {
            SetFunction(modules, isFunction, 6, i, i % 2 == 0);   // 列 x=6（垂直时序）
            SetFunction(modules, isFunction, i, 6, i % 2 == 0);   // 行 y=6（水平时序）
        }

        // 3 个 finder（左上/右上/左下）。
        DrawFinderPattern(modules, isFunction, 3, 3, size);
        DrawFinderPattern(modules, isFunction, size - 4, 3, size);
        DrawFinderPattern(modules, isFunction, 3, size - 4, size);

        // 对齐图案（除 3 个 finder 角落外）。
        int[] pos = QrCodec.GetAlignmentPatternPositions(version);
        int numAlign = pos.Length;
        for (int i = 0; i < numAlign; i++)
        {
            for (int j = 0; j < numAlign; j++)
            {
                bool skip = (i == 0 && j == 0) || (i == 0 && j == numAlign - 1) || (i == numAlign - 1 && j == 0);
                if (!skip)
                    DrawAlignmentPattern(modules, isFunction, pos[i], pos[j], size);
            }
        }

        // 格式信息（掩码号先用 0 占位，掩码择优后重写）。
        DrawFormatBits(modules, isFunction, size, ecl, 0);
        // 版本信息（≥7）。
        DrawVersionBits(modules, isFunction, version, size);
    }

    private static void DrawFinderPattern(bool[,] modules, bool[,] isFunction, int cx, int cy, int size)
    {
        for (int dy = -4; dy <= 4; dy++)
        {
            for (int dx = -4; dx <= 4; dx++)
            {
                int x = cx + dx;
                int y = cy + dy;
                if ((uint)x < (uint)size && (uint)y < (uint)size)
                {
                    int m = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    SetFunction(modules, isFunction, x, y, m != 2 && m != 4);
                }
            }
        }
    }

    private static void DrawAlignmentPattern(bool[,] modules, bool[,] isFunction, int cx, int cy, int size)
    {
        for (int dy = -2; dy <= 2; dy++)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                int x = cx + dx;
                int y = cy + dy;
                if ((uint)x < (uint)size && (uint)y < (uint)size)
                    SetFunction(modules, isFunction, x, y, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
            }
        }
    }

    private static void DrawFormatBits(bool[,] modules, bool[,] isFunction, int size, QrEcLevel ecl, int mask)
    {
        int bits = QrCodec.FormatInfoBits(ecl, mask);
        for (int i = 0; i <= 5; i++) SetFunction(modules, isFunction, 8, i, GetBit(bits, i));
        SetFunction(modules, isFunction, 8, 7, GetBit(bits, 6));
        SetFunction(modules, isFunction, 8, 8, GetBit(bits, 7));
        SetFunction(modules, isFunction, 7, 8, GetBit(bits, 8));
        for (int i = 9; i <= 14; i++) SetFunction(modules, isFunction, 14 - i, 8, GetBit(bits, i));

        for (int i = 0; i <= 7; i++) SetFunction(modules, isFunction, size - 1 - i, 8, GetBit(bits, i));
        for (int i = 8; i <= 14; i++) SetFunction(modules, isFunction, 8, size - 15 + i, GetBit(bits, i));
        SetFunction(modules, isFunction, 8, size - 8, true); // 恒黑模块
    }

    private static void DrawVersionBits(bool[,] modules, bool[,] isFunction, int version, int size)
    {
        if (version < 7)
            return;
        int bits = QrCodec.VersionInfoBits(version);
        for (int i = 0; i < 18; i++)
        {
            bool bit = GetBit(bits, i);
            int a = size - 11 + i % 3;
            int b = i / 3;
            SetFunction(modules, isFunction, a, b, bit);
            SetFunction(modules, isFunction, b, a, bit);
        }
    }

    private static void SetFunction(bool[,] modules, bool[,] isFunction, int x, int y, bool dark)
    {
        modules[y, x] = dark;
        isFunction[y, x] = true;
    }

    private static bool GetBit(int value, int i) => ((value >> i) & 1) != 0;

    // ───────────────────────── RS 分块 + 交错 ─────────────────────────

    private static byte[] AddEccAndInterleave(byte[] data, int version, QrEcLevel ecl)
    {
        var layout = QrCodec.GetBlockLayout(ecl, version);
        byte[] rsDiv = QrCodec.ComputeRsDivisor(layout.EcLen);
        int rawCodewords = QrCodec.NumRawCodewords(version);
        int dataRegion = layout.ShortTotalLen - layout.EcLen + 1; // 块缓冲数据区长度（含短块尾部 0 填充位）

        var blocks = new byte[layout.NumBlocks][];
        int k = 0;
        for (int i = 0; i < layout.NumBlocks; i++)
        {
            int realDataLen = layout.ShortDataLen + (i < layout.NumShortBlocks ? 0 : 1);
            var block = new byte[layout.ShortTotalLen + 1];
            // 数据区（短块 last 字节保持 0 = 填充占位）。
            for (int j = 0; j < realDataLen; j++)
                block[j] = data[k + j];
            k += realDataLen;
            // 对真实数据算 EC（短块不含填充 0）。
            byte[] ecc = QrCodec.ComputeRsRemainder(block.AsSpan(0, realDataLen), rsDiv);
            for (int j = 0; j < layout.EcLen; j++)
                block[dataRegion + j] = ecc[j];
            blocks[i] = block;
        }

        // 逐列交错（短块跳过填充占位列）。
        var result = new byte[rawCodewords];
        int idx = 0;
        int padCol = layout.ShortTotalLen - layout.EcLen;
        for (int i = 0; i <= layout.ShortTotalLen; i++)
        {
            for (int j = 0; j < layout.NumBlocks; j++)
            {
                if (i != padCol || j >= layout.NumShortBlocks)
                    result[idx++] = blocks[j][i];
            }
        }
        return result;
    }

    private static void DrawCodewords(bool[,] modules, bool[,] isFunction, byte[] data, int size)
    {
        int bitCount = data.Length * 8;
        int i = 0;
        int right = size - 1;
        while (right > 0)
        {
            int r = right; // 本轮列对右列（与 Python range 语义一致：循环变量内部调整不影响下一轮）
            if (r <= 6)
                r -= 1;
            for (int vert = 0; vert < size; vert++)
            {
                bool upward = ((r + 1) & 2) == 0;
                int y = upward ? size - 1 - vert : vert;
                for (int j = 0; j < 2; j++)
                {
                    int x = r - j;
                    if (!isFunction[y, x] && i < bitCount)
                    {
                        modules[y, x] = GetBit(data[i >> 3], 7 - (i & 7));
                        i++;
                    }
                }
            }
            right -= 2;
        }
    }

    // ───────────────────────── 掩码 ─────────────────────────

    private static int ChooseBestMask(bool[,] modules, bool[,] isFunction, int size, QrEcLevel ecl)
    {
        int bestMask = 0;
        int minPenalty = int.MaxValue;
        for (int mask = 0; mask < 8; mask++)
        {
            ApplyMask(modules, isFunction, size, mask);
            DrawFormatBits(modules, isFunction, size, ecl, mask);
            int penalty = GetPenaltyScore(modules, size);
            if (penalty < minPenalty)
            {
                minPenalty = penalty;
                bestMask = mask;
            }
            ApplyMask(modules, isFunction, size, mask); // 撤销
        }
        ApplyMask(modules, isFunction, size, bestMask);  // 施加最终掩码
        DrawFormatBits(modules, isFunction, size, ecl, bestMask); // 写最终格式信息
        return bestMask;
    }

    private static void ApplyMask(bool[,] modules, bool[,] isFunction, int size, int mask)
    {
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (!isFunction[y, x] && QrCodec.MaskCondition(x, y, mask))
                    modules[y, x] = !modules[y, x];
            }
        }
    }

    // ───────────────────────── 掩码惩罚分（仅编码器择掩码用）─────────────────────────

    private const int PenaltyN1 = 3;
    private const int PenaltyN2 = 3;
    private const int PenaltyN3 = 40;
    private const int PenaltyN4 = 10;

    private static int GetPenaltyScore(bool[,] modules, int size)
    {
        int result = 0;

        // 行扫描（N1 连续同色 + N3 finder 相似）。
        for (int y = 0; y < size; y++)
        {
            var hist = new int[7];
            bool runColor = false;
            int runLen = 0;
            for (int x = 0; x < size; x++)
            {
                if (modules[y, x] == runColor)
                {
                    runLen++;
                    if (runLen == 5) result += PenaltyN1;
                    else if (runLen > 5) result += 1;
                }
                else
                {
                    FinderAddHistory(hist, runLen, size);
                    if (!runColor)
                        result += FinderCountPatterns(hist) * PenaltyN3;
                    runColor = modules[y, x];
                    runLen = 1;
                }
            }
            result += FinderTerminateAndCount(hist, runColor, runLen, size) * PenaltyN3;
        }

        // 列扫描。
        for (int x = 0; x < size; x++)
        {
            var hist = new int[7];
            bool runColor = false;
            int runLen = 0;
            for (int y = 0; y < size; y++)
            {
                if (modules[y, x] == runColor)
                {
                    runLen++;
                    if (runLen == 5) result += PenaltyN1;
                    else if (runLen > 5) result += 1;
                }
                else
                {
                    FinderAddHistory(hist, runLen, size);
                    if (!runColor)
                        result += FinderCountPatterns(hist) * PenaltyN3;
                    runColor = modules[y, x];
                    runLen = 1;
                }
            }
            result += FinderTerminateAndCount(hist, runColor, runLen, size) * PenaltyN3;
        }

        // N2：2x2 同色块。
        for (int y = 0; y < size - 1; y++)
        {
            for (int x = 0; x < size - 1; x++)
            {
                bool c = modules[y, x];
                if (c == modules[y, x + 1] && c == modules[y + 1, x] && c == modules[y + 1, x + 1])
                    result += PenaltyN2;
            }
        }

        // N4：黑白比例失衡。
        int dark = 0;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                if (modules[y, x]) dark++;
        int total = size * size;
        int k = (Math.Abs(dark * 20 - total * 10) + total - 1) / total - 1;
        result += k * PenaltyN4;
        return result;
    }

    private static void FinderAddHistory(int[] hist, int runLen, int size)
    {
        if (hist[0] == 0)
            runLen += size; // 起始前的亮边
        for (int i = 6; i > 0; i--)
            hist[i] = hist[i - 1];
        hist[0] = runLen;
    }

    private static int FinderCountPatterns(int[] hist)
    {
        int n = hist[1];
        bool core = n > 0 && hist[2] == n && hist[4] == n && hist[5] == n && hist[3] == n * 3;
        int count = 0;
        if (core && hist[0] >= n * 4 && hist[6] >= n) count++;
        if (core && hist[6] >= n * 4 && hist[0] >= n) count++;
        return count;
    }

    private static int FinderTerminateAndCount(int[] hist, bool runColor, int runLen, int size)
    {
        if (runColor)
        {
            FinderAddHistory(hist, runLen, size);
            runLen = 0;
        }
        runLen += size; // 行尾亮边
        FinderAddHistory(hist, runLen, size);
        return FinderCountPatterns(hist);
    }

    private static void AppendBits(List<bool> bb, int value, int count)
    {
        for (int i = count - 1; i >= 0; i--)
            bb.Add(((value >> i) & 1) != 0);
    }
}
