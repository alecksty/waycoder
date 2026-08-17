namespace WayCoder.Infra;

/// <summary>
/// 手搓 baseline JPEG 编解码：YCbCr 4:2:0、8×8 DCT、标准量化/哈夫曼表（ITU-T T.81 Annex K）。
/// 解码支持 1/3 分量、4:4:4/4:2:2/4:2:0、8 位；不支持算术编码/渐进/重启标记（RST 跳过）。
/// 零反射、零依赖、AOT 安全、跨平台。
/// </summary>
public static class JpegCodec
{
    /// <summary>解码像素数上限（防不可信 SOF0 尺寸字段导致 OOM），约 25MP。</summary>
    private const int MaxPixels = 25_000_000;

    // ── 量化表（zigzag 序）──
    static readonly byte[] QY =
    {
        16,11,10,16,24,40,51,61, 12,12,14,19,26,58,60,55,
        14,13,16,24,40,57,69,56, 14,17,22,29,51,87,80,62,
        18,22,37,56,68,109,103,77, 24,35,55,64,81,104,113,92,
        49,64,78,87,103,121,120,101, 72,92,95,98,112,100,103,99,
    };
    static readonly byte[] QC =
    {
        17,18,24,47,99,99,99,99, 18,21,26,66,99,99,99,99,
        24,26,56,99,99,99,99,99, 47,66,99,99,99,99,99,99,
        99,99,99,99,99,99,99,99, 99,99,99,99,99,99,99,99,
        99,99,99,99,99,99,99,99, 99,99,99,99,99,99,99,99,
    };

    // ── 哈夫曼表 ──
    static readonly byte[] DcBitsY = { 0,1,5,1,1,1,1,1,1,0,0,0,0,0,0,0 };
    static readonly byte[] DcValY = { 0,1,2,3,4,5,6,7,8,9,10,11 };
    static readonly byte[] DcBitsC = { 0,3,1,1,1,1,1,1,1,1,1,0,0,0,0,0 };
    static readonly byte[] DcValC = { 0,1,2,3,4,5,6,7,8,9,10,11 };
    static readonly byte[] AcBitsY = { 0,2,1,3,3,2,4,3,5,5,4,4,0,0,1,0x7d };
    static readonly byte[] AcValY =
    {
        0x01,0x02,0x03,0x00,0x04,0x11,0x05,0x12,0x21,0x31,0x41,0x06,0x13,0x51,0x61,0x07,
        0x22,0x71,0x14,0x32,0x81,0x91,0xa1,0x08,0x23,0x42,0xb1,0xc1,0x15,0x52,0xd1,0xf0,
        0x24,0x33,0x62,0x72,0x82,0x09,0x0a,0x16,0x17,0x18,0x19,0x1a,0x25,0x26,0x27,0x28,
        0x29,0x2a,0x34,0x35,0x36,0x37,0x38,0x39,0x3a,0x43,0x44,0x45,0x46,0x47,0x48,0x49,
        0x4a,0x53,0x54,0x55,0x56,0x57,0x58,0x59,0x5a,0x63,0x64,0x65,0x66,0x67,0x68,0x69,
        0x6a,0x73,0x74,0x75,0x76,0x77,0x78,0x79,0x7a,0x83,0x84,0x85,0x86,0x87,0x88,0x89,
        0x8a,0x92,0x93,0x94,0x95,0x96,0x97,0x98,0x99,0x9a,0xa2,0xa3,0xa4,0xa5,0xa6,0xa7,
        0xa8,0xa9,0xaa,0xb2,0xb3,0xb4,0xb5,0xb6,0xb7,0xb8,0xb9,0xba,0xc2,0xc3,0xc4,0xc5,
        0xc6,0xc7,0xc8,0xc9,0xca,0xd2,0xd3,0xd4,0xd5,0xd6,0xd7,0xd8,0xd9,0xda,0xe1,0xe2,
        0xe3,0xe4,0xe5,0xe6,0xe7,0xe8,0xe9,0xea,0xf1,0xf2,0xf3,0xf4,0xf5,0xf6,0xf7,0xf8,
        0xf9,0xfa,
    };
    static readonly byte[] AcBitsC = { 0,2,1,2,4,4,3,4,7,5,4,4,0,1,2,0x77 };
    static readonly byte[] AcValC =
    {
        0x00,0x01,0x02,0x03,0x11,0x04,0x05,0x21,0x31,0x06,0x12,0x41,0x51,0x07,0x61,0x71,
        0x13,0x22,0x32,0x81,0x08,0x14,0x42,0x91,0xa1,0xb1,0xc1,0x09,0x23,0x33,0x52,0xf0,
        0x15,0x62,0x72,0xd1,0x0a,0x16,0x24,0x34,0xe1,0x25,0xf1,0x17,0x18,0x19,0x1a,0x26,
        0x27,0x28,0x29,0x2a,0x35,0x36,0x37,0x38,0x39,0x3a,0x43,0x44,0x45,0x46,0x47,0x48,
        0x49,0x4a,0x53,0x54,0x55,0x56,0x57,0x58,0x59,0x5a,0x63,0x64,0x65,0x66,0x67,0x68,
        0x69,0x6a,0x73,0x74,0x75,0x76,0x77,0x78,0x79,0x7a,0x82,0x83,0x84,0x85,0x86,0x87,
        0x88,0x89,0x8a,0x92,0x93,0x94,0x95,0x96,0x97,0x98,0x99,0x9a,0xa2,0xa3,0xa4,0xa5,
        0xa6,0xa7,0xa8,0xa9,0xaa,0xb2,0xb3,0xb4,0xb5,0xb6,0xb7,0xb8,0xb9,0xba,0xc2,0xc3,
        0xc4,0xc5,0xc6,0xc7,0xc8,0xc9,0xca,0xd2,0xd3,0xd4,0xd5,0xd6,0xd7,0xd8,0xd9,0xda,
        0xe2,0xe3,0xe4,0xe5,0xe6,0xe7,0xe8,0xe9,0xea,0xf2,0xf3,0xf4,0xf5,0xf6,0xf7,0xf8,
        0xf9,0xfa,
    };

    static readonly int[] Zigzag =
    {
        0,1,8,16,9,2,3,10,17,24,32,25,18,11,4,5,12,19,26,33,40,48,41,34,27,20,13,6,7,14,21,28,35,42,49,56,
        57,50,43,36,29,22,15,23,30,37,44,51,58,59,52,45,38,31,39,46,53,60,61,54,47,55,62,63,
    };

    // ── 编码 ──
    public static byte[] Encode(RasterImage img, int quality = 80)
    {
        quality = Math.Clamp(quality, 1, 100);
        int w = img.Width, h = img.Height;
        int cw = (w + 1) / 2, ch = (h + 1) / 2;

        double qscale = quality < 50 ? 5000.0 / quality : 200.0 - 2.0 * quality;
        var qY = ScaleQuant(QY, qscale);
        var qC = ScaleQuant(QC, qscale);

        var Y = new byte[w * h];
        var Cb = new byte[cw * ch];
        var Cr = new byte[cw * ch];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int si = (y * w + x) * 4;
                int r = img.Rgba[si], g = img.Rgba[si + 1], b = img.Rgba[si + 2];
                Y[y * w + x] = (byte)Clamp(0, 255, (int)Math.Round(0.299 * r + 0.587 * g + 0.114 * b));
            }
        for (int cy = 0; cy < ch; cy++)
            for (int cx = 0; cx < cw; cx++)
            {
                int sr = 0, sg = 0, sb = 0, cnt = 0;
                for (int dy = 0; dy < 2; dy++)
                    for (int dx = 0; dx < 2; dx++)
                    {
                        int xx = cx * 2 + dx, yy = cy * 2 + dy;
                        if (xx >= w || yy >= h) continue;
                        int si = (yy * w + xx) * 4;
                        sr += img.Rgba[si]; sg += img.Rgba[si + 1]; sb += img.Rgba[si + 2]; cnt++;
                    }
                if (cnt == 0) continue;
                int r = sr / cnt, g = sg / cnt, b = sb / cnt;
                int ci = cy * cw + cx;
                Cb[ci] = (byte)Clamp(0, 255, (int)Math.Round(-0.1687 * r - 0.3313 * g + 0.5 * b + 128));
                Cr[ci] = (byte)Clamp(0, 255, (int)Math.Round(0.5 * r - 0.4187 * g - 0.0813 * b + 128));
            }

        var dcY = new HuffTable(DcBitsY, DcValY);
        var acY = new HuffTable(AcBitsY, AcValY);
        var dcC = new HuffTable(DcBitsC, DcValC);
        var acC = new HuffTable(AcBitsC, AcValC);

        var ms = new MemoryStream();
        WriteU16(ms, 0xFFD8); // SOI

        // DQT
        ms.WriteByte(0xFF); ms.WriteByte(0xDB);
        WriteU16(ms, 2 + 1 + 64 + 1 + 64);
        ms.WriteByte(0x00); foreach (var v in qY) ms.WriteByte(v);
        ms.WriteByte(0x01); foreach (var v in qC) ms.WriteByte(v);

        // SOF0
        ms.WriteByte(0xFF); ms.WriteByte(0xC0);
        WriteU16(ms, 2 + 1 + 2 + 2 + 1 + 3 * 3);
        ms.WriteByte(8);
        WriteU16(ms, (ushort)h); WriteU16(ms, (ushort)w);
        ms.WriteByte(3);
        ms.WriteByte(1); ms.WriteByte(0x22); ms.WriteByte(0); // Y 2x2 qt0
        ms.WriteByte(2); ms.WriteByte(0x11); ms.WriteByte(1); // Cb 1x1 qt1
        ms.WriteByte(3); ms.WriteByte(0x11); ms.WriteByte(1); // Cr 1x1 qt1

        // DHT
        WriteHuffTable(ms, 0x00, DcBitsY, DcValY);
        WriteHuffTable(ms, 0x10, AcBitsY, AcValY);
        WriteHuffTable(ms, 0x01, DcBitsC, DcValC);
        WriteHuffTable(ms, 0x11, AcBitsC, AcValC);

        // SOS
        ms.WriteByte(0xFF); ms.WriteByte(0xDA);
        WriteU16(ms, 2 + 1 + 3 * 2 + 3);
        ms.WriteByte(3);
        ms.WriteByte(1); ms.WriteByte(0x00);
        ms.WriteByte(2); ms.WriteByte(0x11);
        ms.WriteByte(3); ms.WriteByte(0x11);
        ms.WriteByte(0); ms.WriteByte(63); ms.WriteByte(0);

        var bw = new BitWriter(ms);
        int prevDcY = 0, prevDcCb = 0, prevDcCr = 0;
        for (int by = 0; by < ch; by += 8)
            for (int bx = 0; bx < cw; bx += 8)
            {
                for (int dy = 0; dy < 2; dy++)
                    for (int dx = 0; dx < 2; dx++)
                        EncodeBlock(bw, Y, w, h, bx * 2 + dx * 8, by * 2 + dy * 8, qY, dcY, acY, ref prevDcY);
                EncodeBlock(bw, Cb, cw, ch, bx, by, qC, dcC, acC, ref prevDcCb);
                EncodeBlock(bw, Cr, cw, ch, bx, by, qC, dcC, acC, ref prevDcCr);
            }
        bw.Flush();

        ms.WriteByte(0xFF); ms.WriteByte(0xD9); // EOI
        return ms.ToArray();
    }

    static void EncodeBlock(BitWriter bw, byte[] data, int w, int h, int x0, int y0, byte[] q, HuffTable dcT, HuffTable acT, ref int prevDc)
    {
        var block = new double[64];
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
            {
                int px = x0 + x, py = y0 + y;
                double v = (px < w && py < h) ? data[py * w + px] : 0;
                block[y * 8 + x] = v - 128.0;
            }
        Dct(block);
        var zz = new int[64];
        for (int i = 0; i < 64; i++) zz[i] = (int)Math.Round(block[Zigzag[i]] / q[i]);

        int diff = zz[0] - prevDc;
        prevDc = zz[0];
        int s = Size(diff);
        var (c, l) = dcT.Get(s);
        bw.Write(c, l);
        if (s > 0) WriteAmplitude(bw, diff, s);

        int run = 0;
        for (int i = 1; i < 64; i++)
        {
            if (zz[i] == 0) { run++; continue; }
            while (run >= 16) { var (c16, l16) = acT.Get(0xF0); bw.Write(c16, l16); run -= 16; }
            int sz = Size(zz[i]);
            var (c2, l2) = acT.Get((run << 4) | sz);
            bw.Write(c2, l2);
            WriteAmplitude(bw, zz[i], sz);
            run = 0;
        }
        if (run > 0) { var (ce, le) = acT.Get(0x00); bw.Write(ce, le); }
    }

    // ── 解码 ──
    public static RasterImage Decode(byte[] data)
    {
        if (data == null || data.Length < 4) throw new FormatException("JPEG 数据过短");
        if (data[0] != 0xFF || data[1] != 0xD8) throw new FormatException("非法 JPEG 签名");

        int pos = 2;
        int width = 0, height = 0;
        var comps = new List<JpegComponent>();
        var qt = new List<byte[]>();
        var dcTables = new HuffDecoder?[16];
        var acTables = new HuffDecoder?[16];
        byte[]? entropy = null;
        int maxH = 1, maxV = 1;

        while (pos < data.Length - 1)
        {
            if (data[pos] != 0xFF) { pos++; continue; }
            int marker = data[pos + 1];
            if (marker == 0x00) { pos += 2; continue; }
            if (marker == 0xFF) { pos++; continue; }
            pos += 2;
            if (marker == 0xD9) break; // EOI
            if (marker == 0xD8) continue; // SOI
            if (marker >= 0xD0 && marker <= 0xD7) continue; // RST
            if (marker == 0x01 || (marker >= 0xD0 && marker <= 0xD9)) continue;
            if (pos + 2 > data.Length) break;
            int len = ReadU16(data, pos); pos += 2;
            int end = pos + len - 2;
            if (end > data.Length) throw new FormatException("JPEG 段越界");

            switch (marker)
            {
                case 0xC0: // SOF0
                case 0xC1: case 0xC2:
                    if (marker != 0xC0) throw new FormatException("仅支持 baseline JPEG");
                    height = ReadU16(data, pos + 1);
                    width = ReadU16(data, pos + 3);
                    int nComp = data[pos + 5];
                    if (nComp < 1 || nComp > 4) throw new FormatException("非法分量数"); // 限制分量数，防恶意 nComp 放大采样数组
                    if (pos + 6 + nComp * 3 > end) throw new FormatException("SOF 段越界");
                    for (int i = 0; i < nComp; i++)
                    {
                        int id = data[pos + 6 + i * 3];
                        int hv = data[pos + 7 + i * 3];
                        int qid = data[pos + 8 + i * 3];
                        int hh = hv >> 4, vv = hv & 0x0F;
                        if (hh == 0 || vv == 0 || hh > 4 || vv > 4) throw new FormatException("非法采样因子");
                        comps.Add(new JpegComponent { Id = id, H = hh, V = vv, QtId = qid });
                        if (hh > maxH) maxH = hh;
                        if (vv > maxV) maxV = vv;
                    }
                    break;
                case 0xDB: // DQT
                {
                    int p = pos;
                    while (p < end)
                    {
                        int info = data[p++];
                        int id = info & 0x0F;
                        var table = new byte[64];
                        for (int i = 0; i < 64; i++) table[i] = data[p++];
                        while (qt.Count <= id) qt.Add(new byte[64]);
                        qt[id] = table;
                    }
                    break;
                }
                case 0xC4: // DHT
                {
                    int p = pos;
                    while (p < end)
                    {
                        int info = data[p++];
                        int cls = info >> 4, id = info & 0x0F;
                        var bits = new byte[16];
                        int total = 0;
                        for (int i = 0; i < 16; i++) { bits[i] = data[p++]; total += bits[i]; }
                        var vals = new byte[total];
                        for (int i = 0; i < total; i++) vals[i] = data[p++];
                        var dec = HuffDecoder.Build(bits, vals);
                        if (cls == 0) dcTables[id] = dec; else acTables[id] = dec;
                    }
                    break;
                }
                case 0xDA: // SOS
                {
                    int n = data[pos];
                    var order = new List<(int compIdx, int dc, int ac)>();
                    int p = pos + 1;
                    for (int i = 0; i < n; i++)
                    {
                        int id = data[p]; int hf = data[p + 1]; p += 2;
                        int ci = comps.FindIndex(c => c.Id == id);
                        order.Add((ci, hf >> 4, hf & 0x0F));
                    }
                    p += 3; // Ss Se AhAl
                    // 读熵数据（去填充），直到遇到非填充标记
                    var ent = new MemoryStream();
                    int q = p;
                    while (q < data.Length - 1)
                    {
                        if (data[q] == 0xFF)
                        {
                            byte b2 = data[q + 1];
                            if (b2 == 0x00) { ent.WriteByte(0xFF); q += 2; }
                            else if (b2 >= 0xD0 && b2 <= 0xD7) { q += 2; }
                            else break;
                        }
                        else { ent.WriteByte(data[q]); q++; }
                    }
                    entropy = ent.ToArray();
                    // 解码
                    foreach (var c in comps)
                    {
                        c.Dc = order.Find(o => o.compIdx == comps.IndexOf(c)).dc;
                        c.Ac = order.Find(o => o.compIdx == comps.IndexOf(c)).ac;
                    }
                    break;
                }
            }
            pos = end;
        }

        if (width <= 0 || height <= 0 || comps.Count == 0 || entropy == null)
            throw new FormatException("JPEG 缺少必要段");
        if ((long)width * height > MaxPixels) throw new FormatException("JPEG 尺寸过大");

        // 分量采样数组
        int mcuCols = (width + 8 * maxH - 1) / (8 * maxH);
        int mcuRows = (height + 8 * maxV - 1) / (8 * maxV);
        foreach (var c in comps)
        {
            c.BlocksX = mcuCols * c.H;
            c.BlocksY = mcuRows * c.V;
            c.SampleW = c.BlocksX * 8;
            c.SampleH = c.BlocksY * 8;
            c.Samples = new double[c.SampleW * c.SampleH];
        }

        var br = new BitReader(entropy);
        var prevDc = new int[comps.Count];
        for (int my = 0; my < mcuRows; my++)
            for (int mx = 0; mx < mcuCols; mx++)
                foreach (var c in comps)
                    for (int vy = 0; vy < c.V; vy++)
                        for (int hx = 0; hx < c.H; hx++)
                        {
                            int bx = mx * c.H + hx, by = my * c.V + vy;
                            if (bx >= c.BlocksX || by >= c.BlocksY) continue;
                            var dcT = dcTables[c.Dc] ?? throw new FormatException("缺少 DC 表");
                            var acT = acTables[c.Ac] ?? throw new FormatException("缺少 AC 表");
                            var qtTable = c.QtId < qt.Count ? qt[c.QtId] : new byte[64];
                            DecodeBlock(br, c, bx, by, qtTable, dcT, acT, ref prevDc[comps.IndexOf(c)]);
                        }

        // YCbCr/RGB → RGBA
        var rgba = new byte[width * height * 4];
        int pi = 0;
        if (comps.Count == 1)
        {
            var g = comps[0];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int v = Clamp(0, 255, (int)Math.Round(g.Samples[y * g.SampleW + x]));
                    rgba[pi++] = (byte)v; rgba[pi++] = (byte)v; rgba[pi++] = (byte)v; rgba[pi++] = 255;
                }
        }
        else
        {
            var Yc = comps.Find(c => c.Id == 1) ?? comps[0];
            var Cbc = comps.Find(c => c.Id == 2) ?? comps[1];
            var Crc = comps.Find(c => c.Id == 3) ?? comps[2];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    double yy = Yc.Samples[y * Yc.SampleW + x];
                    int cx = x * Cbc.H / maxH, cy = y * Cbc.V / maxV;
                    int cx2 = x * Crc.H / maxH, cy2 = y * Crc.V / maxV;
                    double cb = Cbc.Samples[cy * Cbc.SampleW + cx];
                    double cr = Crc.Samples[cy2 * Crc.SampleW + cx2];
                    int r = Clamp(0, 255, (int)Math.Round(yy + 1.402 * (cr - 128)));
                    int g = Clamp(0, 255, (int)Math.Round(yy - 0.34414 * (cb - 128) - 0.71414 * (cr - 128)));
                    int b = Clamp(0, 255, (int)Math.Round(yy + 1.772 * (cb - 128)));
                    rgba[pi++] = (byte)r; rgba[pi++] = (byte)g; rgba[pi++] = (byte)b; rgba[pi++] = 255;
                }
        }
        return new RasterImage(width, height, rgba);
    }

    static void DecodeBlock(BitReader br, JpegComponent c, int bx, int by, byte[] qt, HuffDecoder dcT, HuffDecoder acT, ref int prevDc)
    {
        var zz = new int[64];
        int s = dcT.Decode(br);
        if (s < 0) throw new FormatException("Huffman DC 解码失败");
        int diff = s > 0 ? ReadAmplitude(br.ReadBits(s), s) : 0;
        prevDc += diff;
        zz[0] = prevDc;
        int i = 1;
        while (i < 64)
        {
            int rs = acT.Decode(br);
            if (rs < 0) throw new FormatException("Huffman AC 解码失败");
            if (rs == 0) break;
            int run = rs >> 4, sz = rs & 0x0F;
            i += run;
            if (i >= 64) break;
            zz[i] = sz > 0 ? ReadAmplitude(br.ReadBits(sz), sz) : 0;
            i++;
        }
        var block = new double[64];
        for (int k = 0; k < 64; k++) block[Zigzag[k]] = zz[k] * qt[k];
        Idct(block);
        int x0 = bx * 8, y0 = by * 8;
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
            {
                int px = x0 + x, py = y0 + y;
                if (px >= c.SampleW || py >= c.SampleH) continue;
                c.Samples[py * c.SampleW + px] = block[y * 8 + x] + 128.0;
            }
    }

    sealed class JpegComponent
    {
        public int Id, H, V, QtId, Dc, Ac;
        public int BlocksX, BlocksY, SampleW, SampleH;
        public double[] Samples = [];
    }

    // ── 工具 ──
    static int Size(int v)
    {
        int s = 0, a = Math.Abs(v);
        while (a > 0) { s++; a >>= 1; }
        return s;
    }
    static void WriteAmplitude(BitWriter bw, int v, int s)
    {
        if (s <= 0) return;
        if (v < 0) v = v - 1;
        bw.Write(v, s);
    }
    static int ReadAmplitude(int bits, int s)
    {
        if (s == 0) return 0;
        return bits < (1 << (s - 1)) ? bits - ((1 << s) - 1) : bits;
    }
    static int Clamp(int lo, int hi, int v) => v < lo ? lo : v > hi ? hi : v;

    static byte[] ScaleQuant(byte[] q, double scale)
    {
        var r = new byte[q.Length];
        for (int i = 0; i < q.Length; i++)
            r[i] = (byte)Clamp(1, 255, (int)Math.Round(q[i] * scale / 100.0));
        return r;
    }

    static void Dct(double[] block)
    {
        var tmp = new double[8];
        for (int r = 0; r < 8; r++) { Dct1D(block, r * 8, tmp, 0); Array.Copy(tmp, 0, block, r * 8, 8); }
        for (int c = 0; c < 8; c++)
        {
            var col = new double[8];
            for (int r = 0; r < 8; r++) col[r] = block[r * 8 + c];
            Dct1D(col, 0, tmp, 0);
            for (int r = 0; r < 8; r++) block[r * 8 + c] = tmp[r];
        }
    }
    static void Dct1D(double[] src, int srcOff, double[] dst, int dstOff)
    {
        for (int k = 0; k < 8; k++)
        {
            double sum = 0;
            for (int n = 0; n < 8; n++) sum += src[srcOff + n] * Math.Cos((2.0 * n + 1) * k * Math.PI / 16.0);
            double cc = k == 0 ? 1.0 / Math.Sqrt(2.0) : 1.0;
            dst[dstOff + k] = 0.5 * cc * sum;
        }
    }
    static void Idct(double[] block)
    {
        var tmp = new double[8];
        for (int c = 0; c < 8; c++)
        {
            var col = new double[8];
            for (int r = 0; r < 8; r++) col[r] = block[r * 8 + c];
            Idct1D(col, 0, tmp, 0);
            for (int r = 0; r < 8; r++) block[r * 8 + c] = tmp[r];
        }
        for (int r = 0; r < 8; r++) { Idct1D(block, r * 8, tmp, 0); Array.Copy(tmp, 0, block, r * 8, 8); }
    }
    static void Idct1D(double[] src, int srcOff, double[] dst, int dstOff)
    {
        for (int n = 0; n < 8; n++)
        {
            double sum = 0;
            for (int k = 0; k < 8; k++)
            {
                double cc = k == 0 ? 1.0 / Math.Sqrt(2.0) : 1.0;
                sum += cc * src[srcOff + k] * Math.Cos((2.0 * n + 1) * k * Math.PI / 16.0);
            }
            dst[dstOff + n] = 0.5 * sum;
        }
    }

    static void WriteHuffTable(MemoryStream ms, byte id, byte[] bits, byte[] vals)
    {
        ms.WriteByte(0xFF); ms.WriteByte(0xC4);
        WriteU16(ms, 2 + 1 + 16 + vals.Length);
        ms.WriteByte(id);
        foreach (var b in bits) ms.WriteByte(b);
        foreach (var v in vals) ms.WriteByte(v);
    }

    static void WriteU16(MemoryStream ms, int v) { ms.WriteByte((byte)(v >> 8)); ms.WriteByte((byte)v); }
    static void WriteU16(Stream s, int v) { s.WriteByte((byte)(v >> 8)); s.WriteByte((byte)v); }
    static int ReadU16(byte[] d, int off) => (d[off] << 8) | d[off + 1];

    sealed class BitWriter
    {
        readonly MemoryStream _ms;
        uint _acc; int _n;
        public BitWriter(MemoryStream ms) { _ms = ms; }
        public void Write(int code, int len)
        {
            for (int i = len - 1; i >= 0; i--) WriteBit((code >> i) & 1);
        }
        void WriteBit(int bit)
        {
            _acc = (_acc << 1) | (uint)bit;
            if (++_n == 8) { Emit((byte)_acc); _acc = 0; _n = 0; }
        }
        void Emit(byte b) { _ms.WriteByte(b); if (b == 0xFF) _ms.WriteByte(0x00); }
        public void Flush()
        {
            if (_n > 0) { _acc <<= (8 - _n); _acc |= (uint)((1 << (8 - _n)) - 1); Emit((byte)_acc); _acc = 0; _n = 0; }
        }
    }

    sealed class BitReader
    {
        readonly byte[] _data; int _pos, _bit;
        public BitReader(byte[] data) { _data = data; }
        public int ReadBit()
        {
            if (_pos >= _data.Length) return -1;
            int b = (_data[_pos] >> (7 - _bit)) & 1;
            if (++_bit == 8) { _bit = 0; _pos++; }
            return b;
        }
        public int ReadBits(int n)
        {
            int v = 0;
            for (int i = 0; i < n; i++) { int b = ReadBit(); if (b < 0) return v; v = (v << 1) | b; }
            return v;
        }
    }

    sealed class HuffTable
    {
        readonly int[] _code = new int[256];
        readonly int[] _len = new int[256];
        public HuffTable(byte[] bits, byte[] vals)
        {
            int code = 0, k = 0;
            for (int len = 1; len <= 16; len++)
            {
                int cnt = bits[len - 1];
                for (int i = 0; i < cnt; i++) { int s = vals[k++]; _code[s] = code; _len[s] = len; code++; }
                code <<= 1;
            }
        }
        public (int code, int len) Get(int sym) => (_code[sym], _len[sym]);
    }

    sealed class HuffDecoder
    {
        readonly Dictionary<int, int>[] _byLen;
        HuffDecoder(Dictionary<int, int>[] byLen) { _byLen = byLen; }
        public static HuffDecoder Build(byte[] bits, byte[] vals)
        {
            var byLen = new Dictionary<int, int>[17];
            int code = 0, k = 0;
            for (int len = 1; len <= 16; len++)
            {
                int cnt = bits[len - 1];
                if (cnt > 0)
                {
                    byLen[len] = new Dictionary<int, int>();
                    for (int i = 0; i < cnt; i++) byLen[len][code++] = vals[k++];
                }
                code <<= 1;
            }
            return new HuffDecoder(byLen);
        }
        public int Decode(BitReader br)
        {
            int code = 0;
            for (int len = 1; len <= 16; len++)
            {
                int b = br.ReadBit();
                if (b < 0) return -1;
                code = (code << 1) | b;
                if (_byLen[len] != null && _byLen[len].TryGetValue(code, out int sym)) return sym;
            }
            return -1;
        }
    }
}
