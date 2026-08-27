using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace WayCoder.Git;

/// <summary>
/// git 传输协议底层：pkt-line 帧 + packfile 编解码。
/// 纯逻辑（不碰网络），便于确定性自测。
///
/// 关键取舍：packfile 的每个对象是一个独立 zlib 流，且对象紧密排列（无显式长度）。
/// .NET 的 <see cref="ZLibStream"/> 内部有 4KB 预读缓冲，解压后底层流的 Position 会越过
/// 当前对象、污染到下一个对象，因此无法用「底层流位置」定位对象边界。这里自实现
/// <see cref="Inflater"/>（RFC 1950/1951）来精确返回「该 zlib 流消耗了多少压缩字节」，
/// 从而逐对象推进偏移。压缩端（写 pack）仍用 <see cref="ZLibStream"/>（无边界问题）。
/// </summary>
internal static class PktLine
{
    private static readonly byte[] FlushPkt = "0000"u8.ToArray();

    /// <summary>写一行；payload 为 null/空 时写 flush-pkt（0000）。</summary>
    public static void Write(Stream s, byte[]? payload)
    {
        if (payload == null || payload.Length == 0)
        {
            s.Write(FlushPkt, 0, 4);
            return;
        }
        var len = payload.Length + 4;
        var header = Encoding.ASCII.GetBytes(len.ToString("x4"));
        s.Write(header, 0, 4);
        s.Write(payload, 0, payload.Length);
    }

    public static void WriteString(Stream s, string text)
        => Write(s, Encoding.UTF8.GetBytes(text));

    /// <summary>读一行，返回 payload；flush-pkt 返回 null；EOF 返回 null。</summary>
    public static byte[]? Read(Stream s)
    {
        var header = new byte[4];
        if (!ReadExact(s, header, 4)) return null;
        var len = Convert.ToInt32(Encoding.ASCII.GetString(header), 16);
        if (len == 0) return null;             // flush-pkt
        if (len < 4) throw new InvalidDataException($"非法 pkt-line 长度 {len}");
        var payload = new byte[len - 4];
        if (!ReadExact(s, payload, payload.Length))
            throw new EndOfStreamException("pkt-line 数据截断");
        return payload;
    }

    public static string? ReadString(Stream s)
    {
        var p = Read(s);
        return p == null ? null : Encoding.UTF8.GetString(p);
    }

    /// <summary>
    /// 宽容读一行（protocol v2 用）：len&lt;=1 返回 null —— flush(0000)、delim(0001)、
    /// side-band 的 channel-0 结束标记等非数据帧一律视为流结束。EOF 返回 null。
    /// </summary>
    public static byte[]? ReadTolerant(Stream s)
    {
        var header = new byte[4];
        if (!ReadExact(s, header, 4)) return null;
        var len = Convert.ToInt32(Encoding.ASCII.GetString(header), 16);
        if (len <= 1) return null;
        var payload = new byte[len - 4];
        if (!ReadExact(s, payload, payload.Length))
            throw new EndOfStreamException("pkt-line 数据截断");
        return payload;
    }

    private static bool ReadExact(Stream s, byte[] buf, int count)
    {
        int off = 0;
        while (off < count)
        {
            var n = s.Read(buf, off, count - off);
            if (n <= 0) return false;
            off += n;
        }
        return true;
    }
}

/// <summary>packfile 解码器 —— 解析服务器下发的 packfile（含 ofs-delta / ref-delta）。</summary>
public static class PackFileReader
{
    /// <summary>
    /// 解码整个 packfile，返回 sha → (type, content) 字典。
    /// <paramref name="externalBase"/>：按 sha 查找 pack 外已存在的 base 对象（thin pack 用），
    /// 返回 (type, content)；查不到返回 null。默认 null（假设 base 都在 pack 内）。
    /// </summary>
    public static Dictionary<string, (string Type, byte[] Content)> Read(
        byte[] pack, Func<string, (string Type, byte[] Content)?>? externalBase = null,
        Action<int, int>? onProgress = null)
    {
        if (pack.Length < 12) throw new InvalidDataException("packfile 过短");
        if (pack[0] != 'P' || pack[1] != 'A' || pack[2] != 'C' || pack[3] != 'K')
            throw new InvalidDataException("非 packfile（缺 PACK 魔数）");

        int count = ReadInt32BE(pack, 8);
        var result = new Dictionary<string, (string, byte[])>();
        var bySha = new Dictionary<string, (string Type, byte[] Content)>();
        var byOffset = new Dictionary<long, string>();   // offset → sha（ofs-delta 定位 base）

        int pos = 12;
        for (int i = 0; i < count; i++)
        {
            if ((i & 0x3F) == 0 || i + 1 == count)
                onProgress?.Invoke(i + 1, count);
            long objOffset = pos;

            // 对象头：首字节 bit7=续位、bit6-4=类型、bit3-0=size 低 4 位
            byte b = pack[pos++];
            int type = (b >> 4) & 0x07;
            long size = b & 0x0F;
            int shift = 4;
            while ((b & 0x80) != 0)
            {
                b = pack[pos++];
                size |= (long)(b & 0x7F) << shift;
                shift += 7;
            }

            long baseOffset = -1;
            string? baseSha = null;
            if (type == 6) // ofs-delta：变长负偏移
            {
                byte c = pack[pos++];
                long off = c & 0x7F;
                while ((c & 0x80) != 0)
                {
                    c = pack[pos++];
                    off = ((off + 1) << 7) | (c & 0x7F);
                }
                baseOffset = objOffset - off;
            }
            else if (type == 7) // ref-delta：20 字节 base sha
            {
                baseSha = Convert.ToHexString(pack, pos, 20).ToLowerInvariant();
                pos += 20;
            }

            // 解压该对象的 zlib 数据（精确返回消耗字节数，推进 pos）
            var (inflated, consumed) = Inflater.Decompress(pack, pos);
            pos += consumed;

            string gitType;
            byte[] content;
            if (type is >= 1 and <= 4)
            {
                gitType = TypeName(type);
                content = inflated;
            }
            else
            {
                // delta：定位 base（ofs → byOffset；ref → bySha 或 externalBase）
                byte[]? baseContent = null;
                string baseType = "";
                if (type == 6)
                {
                    if (byOffset.TryGetValue(baseOffset, out var bSha) &&
                        bySha.TryGetValue(bSha, out var bm))
                    {
                        baseContent = bm.Content;
                        baseType = bm.Type;
                    }
                }
                else if (baseSha != null)
                {
                    if (bySha.TryGetValue(baseSha, out var bm))
                    {
                        baseContent = bm.Content;
                        baseType = bm.Type;
                    }
                    else if (externalBase != null)
                    {
                        var ext = externalBase(baseSha);
                        if (ext != null)
                        {
                            baseContent = ext.Value.Content;
                            baseType = ext.Value.Type;
                        }
                    }
                }

                if (baseContent == null)
                    throw new InvalidDataException($"delta 对象的 base 未找到（{(type == 6 ? $"ofs@{baseOffset}" : baseSha)}）");

                content = ApplyDelta(baseContent, inflated);
                gitType = baseType;
            }

            var sha = ObjectSha(gitType, content);
            byOffset[objOffset] = sha;
            bySha[sha] = (gitType, content);
            result[sha] = (gitType, content);
        }

        return result;
    }

    static string TypeName(int type) => type switch
    {
        1 => "commit",
        2 => "tree",
        3 => "blob",
        4 => "tag",
        _ => "blob",
    };

    internal static string ObjectSha(string type, byte[] content)
    {
        var header = Encoding.UTF8.GetBytes($"{type} {content.Length}\0");
        var full = new byte[header.Length + content.Length];
        Array.Copy(header, 0, full, 0, header.Length);
        Array.Copy(content, 0, full, header.Length, content.Length);
        return Convert.ToHexString(SHA1.HashData(full)).ToLowerInvariant();
    }

    static int ReadInt32BE(byte[] b, int off)
        => (b[off] << 24) | (b[off + 1] << 16) | (b[off + 2] << 8) | b[off + 3];

    /// <summary>应用 delta 数据（RFC git pack）：{src-size}{dst-size} + copy/insert 指令序列。</summary>
    internal static byte[] ApplyDelta(byte[] baseContent, byte[] delta)
    {
        int pos = 0;

        long srcSize = 0;
        int shift = 0;
        while (true)
        {
            byte b = delta[pos++];
            srcSize |= (long)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) break;
            shift += 7;
        }

        long dstSize = 0;
        shift = 0;
        while (true)
        {
            byte b = delta[pos++];
            dstSize |= (long)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) break;
            shift += 7;
        }

        var result = new byte[dstSize];
        int rpos = 0;
        while (pos < delta.Length)
        {
            byte cmd = delta[pos++];
            if ((cmd & 0x80) != 0)
            {
                // copy：bit0-3 = offset 字节存在位，bit4-6 = size 字节存在位
                long copyOffset = 0, copySize = 0;
                if ((cmd & 0x01) != 0) copyOffset |= delta[pos++];
                if ((cmd & 0x02) != 0) copyOffset |= (long)delta[pos++] << 8;
                if ((cmd & 0x04) != 0) copyOffset |= (long)delta[pos++] << 16;
                if ((cmd & 0x08) != 0) copyOffset |= (long)delta[pos++] << 24;
                if ((cmd & 0x10) != 0) copySize |= delta[pos++];
                if ((cmd & 0x20) != 0) copySize |= (long)delta[pos++] << 8;
                if ((cmd & 0x40) != 0) copySize |= (long)delta[pos++] << 16;
                if (copySize == 0) copySize = 0x10000;
                Array.Copy(baseContent, copyOffset, result, rpos, copySize);
                rpos += (int)copySize;
            }
            else
            {
                // insert：cmd 值即长度（0 → 128）
                int len = cmd;
                Array.Copy(delta, pos, result, rpos, len);
                pos += len;
                rpos += len;
            }
        }
        return result;
    }
}

/// <summary>packfile 编码器 —— push 时用「非 delta 全量对象」编码（实现简单，接收端接受）。</summary>
public static class PackFileWriter
{
    public static byte[] Write(IEnumerable<(string Type, string Sha, byte[] Content)> objects)
    {
        var list = objects.ToList();
        using var ms = new MemoryStream();

        ms.Write("PACK"u8.ToArray(), 0, 4);
        WriteInt32BE(ms, 2);              // version
        WriteInt32BE(ms, list.Count);     // object count

        foreach (var (type, _, content) in list)
        {
            WriteObjectHeader(ms, TypeCode(type), content.Length);
            using (var z = new ZLibStream(ms, CompressionLevel.Fastest, leaveOpen: true))
                z.Write(content, 0, content.Length);
        }

        // trailer：整个 pack 的 SHA1
        var data = ms.ToArray();
        var sha = SHA1.HashData(data);
        ms.Write(sha, 0, sha.Length);
        return ms.ToArray();
    }

    static int TypeCode(string type) => type switch
    {
        "commit" => 1,
        "tree" => 2,
        "blob" => 3,
        "tag" => 4,
        _ => 3,
    };

    static void WriteObjectHeader(Stream s, int type, long size)
    {
        byte b = (byte)((type << 4) | (size & 0x0F));
        size >>= 4;
        if (size > 0) b |= 0x80;
        s.WriteByte(b);
        while (size > 0)
        {
            b = (byte)(size & 0x7F);
            size >>= 7;
            if (size > 0) b |= 0x80;
            s.WriteByte(b);
        }
    }

    static void WriteInt32BE(Stream s, int v)
    {
        s.WriteByte((byte)(v >> 24));
        s.WriteByte((byte)(v >> 16));
        s.WriteByte((byte)(v >> 8));
        s.WriteByte((byte)v);
    }
}

/// <summary>
/// 自实现 zlib/deflate 解压（RFC 1950 + RFC 1951）。
/// 相比 <see cref="ZLibStream"/>，能精确返回「消耗了多少压缩字节」，用于逐对象推进 packfile 偏移。
/// 压缩端仍用 <see cref="ZLibStream"/>（无边界问题），故此处只需 inflate，不需 deflate。
/// </summary>
internal static class Inflater
{
    public static (byte[] Data, int Consumed) Decompress(byte[] input, int offset)
    {
        int pos = offset;
        byte cmf = input[pos++];
        byte flg = input[pos++];
        if ((cmf & 0x0F) != 8) throw new InvalidDataException("非 deflate 压缩方法");
        if ((cmf >> 4) > 7) throw new InvalidDataException("zlib 窗口过大");
        if ((((cmf << 8) | flg) % 31) != 0) throw new InvalidDataException("zlib header 校验失败");
        if ((flg & 0x20) != 0) pos += 4; // FDICT：跳过 4 字节 dictid（git 不用 preset dictionary）

        var br = new BitReader(input, pos);
        using var output = new MemoryStream();

        bool final;
        do
        {
            final = br.ReadBit() != 0;
            int btype = br.ReadBits(2);
            switch (btype)
            {
                case 0: DecodeStored(br, output); break;
                case 1: DecodeHuffman(br, output, FixedLitLen, FixedDist); break;
                case 2: DecodeDynamic(br, output); break;
                default: throw new InvalidDataException($"非法 deflate 块类型 {btype}");
            }
        } while (!final);

        br.AlignToByte();
        pos = br.BytePosition + 4; // 跳过 adler32
        return (output.ToArray(), pos - offset);
    }

    // ── stored 块（BTYPE=00）──
    static void DecodeStored(BitReader br, Stream output)
    {
        br.AlignToByte();
        int len = br.ReadByte() | (br.ReadByte() << 8);
        int nlen = br.ReadByte() | (br.ReadByte() << 8);
        if ((len ^ 0xFFFF) != nlen) throw new InvalidDataException("stored 块长度校验失败");
        output.Write(br.ReadBytes(len), 0, len);
    }

    // ── Huffman 块（BTYPE=01 固定 / 02 动态解码后共用）──
    static void DecodeHuffman(BitReader br, MemoryStream output, HuffmanDecoder litlen, HuffmanDecoder dist)
    {
        while (true)
        {
            int sym = litlen.Decode(br);
            if (sym < 256)
            {
                output.WriteByte((byte)sym);
            }
            else if (sym == 256)
            {
                return; // 块结束
            }
            else
            {
                int length = LengthBase[sym - 257] + br.ReadBits(LengthExtra[sym - 257]);
                int distSym = dist.Decode(br);
                int distance = DistBase[distSym] + br.ReadBits(DistExtra[distSym]);
                CopyFromHistory(output, distance, length);
            }
        }
    }

    static void DecodeDynamic(BitReader br, MemoryStream output)
    {
        int hlit = br.ReadBits(5) + 257;
        int hdist = br.ReadBits(5) + 1;
        int hclen = br.ReadBits(4) + 4;

        int[] order = { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };
        var codeLenLengths = new byte[19];
        for (int i = 0; i < hclen; i++)
            codeLenLengths[order[i]] = (byte)br.ReadBits(3);
        var codeLenDecoder = new HuffmanDecoder(codeLenLengths);

        var lengths = new byte[hlit + hdist];
        int idx = 0;
        while (idx < lengths.Length)
        {
            int sym = codeLenDecoder.Decode(br);
            if (sym < 16)
            {
                lengths[idx++] = (byte)sym;
            }
            else if (sym == 16)
            {
                byte prev = idx > 0 ? lengths[idx - 1] : (byte)0;
                int repeat = br.ReadBits(2) + 3;
                for (int i = 0; i < repeat; i++) lengths[idx++] = prev;
            }
            else if (sym == 17)
            {
                idx += br.ReadBits(3) + 3;
            }
            else // 18
            {
                idx += br.ReadBits(7) + 11;
            }
        }

        var litlen = new HuffmanDecoder(lengths.AsSpan(0, hlit).ToArray());
        var dist = new HuffmanDecoder(lengths.AsSpan(hlit, hdist).ToArray());
        DecodeHuffman(br, output, litlen, dist);
    }

    static void CopyFromHistory(MemoryStream output, int distance, int length)
    {
        for (int i = 0; i < length; i++)
        {
            int historyLen = (int)output.Length;
            var buf = output.GetBuffer();
            output.WriteByte(buf[historyLen - distance]);
        }
    }

    // ── 位读取器（LSB-first，deflate Huffman 位序）──
    sealed class BitReader
    {
        private readonly byte[] _data;
        private int _pos;
        private uint _buf;
        private int _bits;

        public BitReader(byte[] data, int pos) { _data = data; _pos = pos; }

        public int ReadBit()
        {
            if (_bits == 0)
            {
                if (_pos >= _data.Length) throw new EndOfStreamException("deflate 数据截断");
                _buf = _data[_pos++];
                _bits = 8;
            }
            int bit = (int)(_buf & 1);
            _buf >>= 1;
            _bits--;
            return bit;
        }

        public int ReadBits(int n)
        {
            int v = 0;
            for (int i = 0; i < n; i++) v |= ReadBit() << i;
            return v;
        }

        public void AlignToByte() { _buf = 0; _bits = 0; }

        public int BytePosition => _pos;

        public int ReadByte()
        {
            if (_pos >= _data.Length) throw new EndOfStreamException("数据截断");
            return _data[_pos++];
        }

        public byte[] ReadBytes(int n)
        {
            var b = new byte[n];
            Array.Copy(_data, _pos, b, 0, n);
            _pos += n;
            return b;
        }
    }

    // ── canonical Huffman 解码器 ──
    sealed class HuffmanDecoder
    {
        private readonly int[] _counts;   // 每个码长的符号数
        private readonly int[] _symbols;  // 按码长分组、码值升序的符号

        public HuffmanDecoder(byte[] lengths)
        {
            var blCount = new int[16];
            foreach (var l in lengths)
                if (l > 0 && l < 16) blCount[l]++;
            blCount[0] = 0;

            // _symbols 按「码长分组、符号升序」排列（位置索引），
            // Decode 用 index + (code - first) 定位 —— 与规范码值无关。
            // 注意：不能用 nextCode[码长]（规范码值，fixed litlen 可达 511）索引，
            // 否则 288 长的 _symbols 直接越界。
            _symbols = new int[lengths.Length];
            int pos = 0;
            for (int bits = 1; bits <= 15; bits++)
                for (int sym = 0; sym < lengths.Length; sym++)
                    if (lengths[sym] == bits) _symbols[pos++] = sym;

            _counts = blCount;
        }

        public int Decode(BitReader br)
        {
            int code = 0;
            int first = 0;
            int index = 0;
            for (int len = 1; len <= 15; len++)
            {
                // 码位 MSB 先传（RFC 1951 §3.2.2），逐位读取时先读位是最高位：
                // code = (code << 1) | bit 才能还原规范码值，与下方 first 的规范累加一致。
                // （旧实现 code |= bit << (len-1) 是 LSB 优先构建 → 码值错位 → 解出错误符号）
                code = (code << 1) | br.ReadBit();
                int count = _counts[len];
                if (code - first < count)
                    return _symbols[index + (code - first)];
                index += count;
                first = (first + count) << 1;
            }
            throw new InvalidDataException("Huffman 解码失败（码表不完整）");
        }
    }

    // ── fixed Huffman 表（BTYPE=01）──
    static readonly HuffmanDecoder FixedLitLen = BuildFixedLitLen();
    static readonly HuffmanDecoder FixedDist = BuildFixedDist();

    static HuffmanDecoder BuildFixedLitLen()
    {
        var lengths = new byte[288];
        for (int i = 0; i < 144; i++) lengths[i] = 8;
        for (int i = 144; i < 256; i++) lengths[i] = 9;
        for (int i = 256; i < 280; i++) lengths[i] = 7;
        for (int i = 280; i < 288; i++) lengths[i] = 8;
        return new HuffmanDecoder(lengths);
    }

    static HuffmanDecoder BuildFixedDist()
    {
        var lengths = new byte[32];
        for (int i = 0; i < 32; i++) lengths[i] = 5;
        return new HuffmanDecoder(lengths);
    }

    // ── 长度/距离表（RFC 1951）──
    static readonly int[] LengthBase = {
        3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31,
        35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258
    };
    static readonly int[] LengthExtra = {
        0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2,
        3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0
    };
    static readonly int[] DistBase = {
        1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193,
        257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577
    };
    static readonly int[] DistExtra = {
        0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6,
        7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13
    };
}
