namespace WayCoder.Infra;

/// <summary>QR 纠错级别（与 ISO/IEC 18004 一致）。</summary>
public enum QrEcLevel
{
    /// <summary>L —— 约 7% 码字纠错。</summary>
    Low = 0,
    /// <summary>M —— 约 15% 码字纠错。</summary>
    Medium = 1,
    /// <summary>Q —— 约 25% 码字纠错。</summary>
    Quartile = 2,
    /// <summary>H —— 约 30% 码字纠错。</summary>
    High = 3,
}

/// <summary>单个错误纠正块的数据/EC 布局（encode 分块 + decode 反交错均需要）。</summary>
public readonly struct QrBlockLayout
{
    /// <summary>版本（1-40）。</summary>
    public readonly int Version;
    /// <summary>每个块附加的 Reed-Solomon 校验码字数。</summary>
    public readonly int EcLen;
    /// <summary>总块数。</summary>
    public readonly int NumBlocks;
    /// <summary>短块（组 1）数量；其余为长块（组 2，每组多 1 个数据码字）。</summary>
    public readonly int NumShortBlocks;
    /// <summary>短块总码字（数据+EC）数 = rawCodewords / NumBlocks 下取整。</summary>
    public readonly int ShortTotalLen;

    /// <summary>长块数量（组 2）。</summary>
    public int NumLongBlocks => NumBlocks - NumShortBlocks;
    /// <summary>短块数据码字数。</summary>
    public int ShortDataLen => ShortTotalLen - EcLen;
    /// <summary>长块数据码字数（比短块多 1）。</summary>
    public int LongDataLen => ShortTotalLen + 1 - EcLen;
    /// <summary>总数据码字数（不含 EC）。</summary>
    public int TotalDataLen => NumShortBlocks * ShortDataLen + NumLongBlocks * LongDataLen;

    public QrBlockLayout(int version, int ecLen, int numBlocks, int numShortBlocks, int shortTotalLen)
    {
        Version = version;
        EcLen = ecLen;
        NumBlocks = numBlocks;
        NumShortBlocks = numShortBlocks;
        ShortTotalLen = shortTotalLen;
    }
}

/// <summary>
/// QR Code（Model 2）编码/解码共用静态基础：GF(256) 表、Reed-Solomon、掩码 pattern、
/// 格式/版本信息 BCH、容量/几何表。全部为纯函数无反射，AOT 安全。
/// 模块矩阵统一约定：bool[,] 且 [y, x]（首维 y=行，次维 x=列），true=黑（dark），
/// 坐标左上角为 (0,0)。矩阵尺寸 = SizeOfVersion(version) = 21+4*(version-1)，不含 quiet zone。
/// </summary>
public static class QrCodec
{
    /// <summary>QR Model 2 最小版本。</summary>
    public const int MinVersion = 1;
    /// <summary>QR Model 2 最大版本。</summary>
    public const int MaxVersion = 40;

    // ───────────────────────── EC 级别映射 ─────────────────────────

    /// <summary>EC 级别 → 格式信息 2-bit 值（L=1, M=0, Q=3, H=2）。decode 解析格式信息亦用。</summary>
    public static int EccFormatBits(QrEcLevel lvl) => lvl switch
    {
        QrEcLevel.Low => 1,
        QrEcLevel.Medium => 0,
        QrEcLevel.Quartile => 3,
        QrEcLevel.High => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(lvl)),
    };

    /// <summary>版本的边模块数（版本尺寸）。</summary>
    public static int SizeOfVersion(int version)
    {
        if (version < MinVersion || version > MaxVersion)
            throw new ArgumentOutOfRangeException(nameof(version));
        return version * 4 + 17;
    }

    // ───────────────────────── GF(256) 对数/反对数表 ─────────────────────────
    // 本原多项式 0x11D (x^8 + x^4 + x^3 + x^2 + 1)，生成元 α=2。
    // ExpTable[i] = α^i（i 达 511，对数相加后可直查）；LogTable[v] = log_α(v)。

    private static readonly byte[] _exp;
    private static readonly byte[] _log;

    static QrCodec()
    {
        _exp = new byte[512];
        _log = new byte[256];
        int x = 1;
        for (int i = 0; i < 255; i++)
        {
            _exp[i] = (byte)x;
            _log[x] = (byte)i;
            x <<= 1;
            if (x >= 256)
                x ^= 0x11D;
        }
        for (int i = 255; i < 512; i++)
            _exp[i] = _exp[i - 255];
    }

    /// <summary>α^i（GF(256)）。</summary>
    public static byte GfExp(int i) => _exp[i];

    /// <summary>log_α(v)（v≠0）。</summary>
    public static int GfLog(int v) => _log[v];

    /// <summary>GF(256) 乘法。</summary>
    public static byte GfMul(int x, int y) =>
        (x == 0 || y == 0) ? (byte)0 : _exp[_log[x] + _log[y]];

    // ───────────────────────── Reed-Solomon ─────────────────────────

    /// <summary>
    /// 计算 Reed-Solomon 生成多项式系数（最高次项 x^degree 省略，隐含为 1）。
    /// 返回长度为 degree 的数组，coef[0] 为 x^(degree-1) 系数。
    /// 即 (x - α^0)(x - α^1)...(x - α^(degree-1))。
    /// </summary>
    public static byte[] ComputeRsDivisor(int degree)
    {
        if (degree < 1 || degree > 255)
            throw new ArgumentOutOfRangeException(nameof(degree));
        var result = new byte[degree];
        result[degree - 1] = 1; // 起始为 x^0（存于末位，随后乘根累加）
        int root = 1;
        for (int i = 0; i < degree; i++)
        {
            for (int j = 0; j < degree; j++)
            {
                result[j] = GfMul(result[j], root);
                if (j + 1 < degree)
                    result[j] ^= result[j + 1];
            }
            root = GfMul(root, 0x02);
        }
        return result;
    }

    /// <summary>
    /// 计算 data 多项式除以 divisor 的余式（即校验码字）。返回长度 = divisor.Length 的数组。
    /// divisor 由 <see cref="ComputeRsDivisor"/> 生成。decode 时 syndromes 反算可复用本 GF 域。
    /// </summary>
    public static byte[] ComputeRsRemainder(ReadOnlySpan<byte> data, byte[] divisor)
    {
        var result = new byte[divisor.Length];
        foreach (byte b in data)
        {
            int factor = b ^ result[0];
            // 左移一位：把 result 视为前导为零的寄存器
            for (int i = 0; i + 1 < result.Length; i++)
                result[i] = result[i + 1];
            result[result.Length - 1] = 0;
            if (factor != 0)
            {
                for (int i = 0; i < divisor.Length; i++)
                    result[i] ^= GfMul(divisor[i], factor);
            }
        }
        return result;
    }

    // ───────────────────────── 容量表（版本/EC）─────────────────────────

    // 每版本每个块的 EC 码字数（表行 = EC 级，索引 0 为非法占位，1..40 为版本）。
    private static readonly sbyte[,] EcCodewordsPerBlockTable =
    {
        //v:0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30 31 32 33 34 35 36 37 38 39 40
        { -1, 7,10,15,20,26,18,20,24,30,18,20,24,26,30,22,24,28,30,28,28,28,28,30,30,26,28,30,30,30,30,30,30,30,30,30,30,30,30,30,30 }, // Low
        { -1,10,16,26,18,24,16,18,22,22,26,30,22,22,24,24,28,28,26,26,26,26,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28 }, // Medium
        { -1,13,22,18,26,18,24,18,22,20,24,28,26,24,20,30,24,28,28,26,30,28,30,30,30,30,28,30,30,30,30,30,30,30,30,30,30,30,30,30,30 }, // Quartile
        { -1,17,28,22,16,22,28,26,26,24,28,24,28,22,24,24,30,28,28,26,28,30,24,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30 }, // High
    };

    // 每版本 EC 块数（表行 = EC 级，索引 0 非法占位）。
    private static readonly sbyte[,] NumBlocksTable =
    {
        //v:0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30 31 32 33 34 35 36 37 38 39 40
        { -1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 4, 4, 4, 4, 4, 6, 6, 6, 6, 7, 8, 8, 9, 9,10,12,12,12,13,14,15,16,17,18,19,19,20,21,22,24,25 }, // Low
        { -1, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9,10,10,11,13,14,16,17,17,18,20,21,23,25,26,28,29,31,33,35,37,38,40,43,45,47,49 }, // Medium
        { -1, 1, 1, 2, 2, 4, 4, 6, 6, 8, 8, 8,10,12,16,12,17,16,18,21,20,23,23,25,27,29,34,34,35,38,40,43,45,48,51,53,56,59,62,65,68 }, // Quartile
        { -1, 1, 1, 2, 4, 4, 4, 5, 6, 8, 8,11,11,16,16,18,16,19,21,25,25,25,34,30,32,35,37,40,42,45,48,51,54,57,60,63,66,70,74,77,81 }, // High
    };

    /// <summary>该版本+EC 下每个块附加的 RS 校验码字数。</summary>
    public static int EcCodewordsPerBlock(QrEcLevel ecl, int version)
    {
        CheckVersion(version);
        return EcCodewordsPerBlockTable[(int)ecl, version];
    }

    /// <summary>该版本+EC 下总纠错块数。</summary>
    public static int NumErrorCorrectionBlocks(QrEcLevel ecl, int version)
    {
        CheckVersion(version);
        return NumBlocksTable[(int)ecl, version];
    }

    /// <summary>该版本可存放的数据模块总数（含 remainder 位，可能非 8 的倍数）。</summary>
    public static int NumRawDataModules(int version)
    {
        CheckVersion(version);
        int result = (16 * version + 128) * version + 64;
        if (version >= 2)
        {
            int numAlign = version / 7 + 2;
            result -= (25 * numAlign - 10) * numAlign - 55;
            if (version >= 7)
                result -= 36;
        }
        return result;
    }

    /// <summary>该版本总码字数（数据+EC，= NumRawDataModules/8，向下取整）。</summary>
    public static int NumRawCodewords(int version) => NumRawDataModules(version) / 8;

    /// <summary>该版本+EC 下数据码字容量（不含 EC）。</summary>
    public static int NumDataCodewords(QrEcLevel ecl, int version) =>
        NumRawCodewords(version) - EcCodewordsPerBlock(ecl, version) * NumErrorCorrectionBlocks(ecl, version);

    /// <summary>字节模式（mode 0100）可容纳的最大字节数——版本容量选择/解码长度校验用。</summary>
    public static int ByteModeCapacity(QrEcLevel ecl, int version)
    {
        int capBits = NumDataCodewords(ecl, version) * 8;
        int countBits = version <= 9 ? 8 : 16;
        return (capBits - 4 - countBits) / 8;
    }

    /// <summary>返回版本的 EC 块布局（encode 分块/交错、decode 反交错共用）。</summary>
    public static QrBlockLayout GetBlockLayout(QrEcLevel ecl, int version)
    {
        int numBlocks = NumErrorCorrectionBlocks(ecl, version);
        int ecLen = EcCodewordsPerBlock(ecl, version);
        int raw = NumRawCodewords(version);
        int numShort = numBlocks - raw % numBlocks;
        int shortTotal = raw / numBlocks;
        return new QrBlockLayout(version, ecLen, numBlocks, numShort, shortTotal);
    }

    // ───────────────────────── 几何（功能图形位置）─────────────────────────

    /// <summary>对齐图案中心坐标（x 与 y 轴共用）。版本 1 返回空数组。decode 还原功能图形亦用。</summary>
    public static int[] GetAlignmentPatternPositions(int version)
    {
        CheckVersion(version);
        if (version == 1)
            return Array.Empty<int>();
        int numAlign = version / 7 + 2;
        int step = (version * 8 + numAlign * 3 + 5) / (numAlign * 4 - 4) * 2;
        int size = SizeOfVersion(version);
        var result = new int[numAlign];
        for (int i = 0; i < numAlign - 1; i++)
            result[numAlign - 1 - i] = size - 7 - i * step;
        result[0] = 6;
        return result;
    }

    // ───────────────────────── 掩码 pattern ─────────────────────────

    /// <summary>
    /// 返回坐标 (x,y) 处的数据模块是否需要被掩码反转（pattern 值 == 0）。encode 施加/撤销掩码、
    /// decode 反掩码共用同一函数。mask ∈ [0,7]。
    /// </summary>
    public static bool MaskCondition(int x, int y, int mask)
    {
        int v = mask switch
        {
            0 => (x + y) % 2,
            1 => y % 2,
            2 => x % 3,
            3 => (x + y) % 3,
            4 => (x / 3 + y / 2) % 2,
            5 => x * y % 2 + x * y % 3,
            6 => (x * y % 2 + x * y % 3) % 2,
            7 => ((x + y) % 2 + x * y % 3) % 2,
            _ => throw new ArgumentOutOfRangeException(nameof(mask)),
        };
        return v == 0;
    }

    // ───────────────────────── 格式信息 BCH(15,5) ─────────────────────────

    // 32 个合法格式码（(EC 2-bit + mask 3-bit) 经 BCH(15,5)，含异或掩码 0x5412），
    // decode 读 15-bit 后找最近码即可得 (ecl, mask)。
    private static readonly FormatInfoEntry[] _formatTable = BuildFormatTable();

    private readonly struct FormatInfoEntry
    {
        public readonly int Code;     // 15-bit 落盘值
        public readonly int EccIndex; // 0..3
        public readonly int Mask;     // 0..7
        public FormatInfoEntry(int code, int eccIndex, int mask)
        {
            Code = code;
            EccIndex = eccIndex;
            Mask = mask;
        }
    }

    private static FormatInfoEntry[] BuildFormatTable()
    {
        var list = new FormatInfoEntry[32];
        int p = 0;
        for (int ec = 0; ec < 4; ec++)
            for (int m = 0; m < 8; m++)
                list[p++] = new FormatInfoEntry(MakeFormatBitsRaw(ec, m), ec, m);
        return list;
    }

    private static int MakeFormatBitsRaw(int eccIndex, int mask)
    {
        int fb = eccIndex switch { 0 => 1, 1 => 0, 2 => 3, _ => 2 };
        int data = (fb << 3) | mask;
        int rem = data;
        for (int i = 0; i < 10; i++)
            rem = (rem << 1) ^ ((rem >> 9) * 0x537);
        return (data << 10 | rem) ^ 0x5412;
    }

    /// <summary>由 EC 级与掩码计算 15-bit 格式信息落盘值（格式信息 BCH 编码）。</summary>
    public static int FormatInfoBits(QrEcLevel ecl, int mask)
    {
        if (mask < 0 || mask > 7)
            throw new ArgumentOutOfRangeException(nameof(mask));
        return MakeFormatBitsRaw((int)ecl, mask);
    }

    /// <summary>
    /// 解析 15-bit 格式信息值（两副本任取一份读出，已含异或掩码）→ 返回 (EC 级, 掩码号)。
    /// 与 32 个合法码比汉明距离取最近，实现错误容错。
    /// </summary>
    public static bool TryDecodeFormatInfo(int value15, out QrEcLevel ecl, out int mask)
    {
        int bestDist = 16;
        int bestIdx = -1;
        for (int i = 0; i < _formatTable.Length; i++)
        {
            int d = HammingWeight(value15 ^ _formatTable[i].Code);
            if (d < bestDist)
            {
                bestDist = d;
                bestIdx = i;
            }
        }
        if (bestIdx < 0)
        {
            ecl = QrEcLevel.Medium;
            mask = 0;
            return false;
        }
        ecl = (QrEcLevel)_formatTable[bestIdx].EccIndex;
        mask = _formatTable[bestIdx].Mask;
        return bestDist <= 3; // BCH(15,5) 可纠 1 位；>3 视为不可信
    }

    private static int HammingWeight(int x)
    {
        int n = 0;
        while (x != 0)
        {
            n += x & 1;
            x >>= 1;
        }
        return n;
    }

    // ───────────────────────── 版本信息 BCH(18,6) ─────────────────────────

    /// <summary>版本（≥7）→ 18-bit 版本信息落盘值。decode 读两副本校验版本用。</summary>
    public static int VersionInfoBits(int version)
    {
        if (version < 7 || version > 40)
            throw new ArgumentOutOfRangeException(nameof(version));
        int rem = version;
        for (int i = 0; i < 12; i++)
            rem = (rem << 1) ^ ((rem >> 11) * 0x1F25);
        return version << 12 | rem;
    }

    /// <summary>把坐标写入功能图形（仅编码器用内部标注用，公开便于测试几何）。</summary>
    internal static void CheckVersion(int version)
    {
        if (version < MinVersion || version > MaxVersion)
            throw new ArgumentOutOfRangeException(nameof(version), "QR 版本必须在 1-40");
    }
}
