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
    /// <summary>超过此大小的对象走原生内存路径（安卓托管堆上限装不下几百 MB 单对象）。</summary>
    internal const long LargeThreshold = 16L * 1024 * 1024;
    /// <summary>
    /// 解码整个 packfile，逐个对象回调 <paramref name="onObject"/>（调用方立即写盘）。
    /// 内存关键：不回收集全部对象——base 只经「有字节上限的 LRU 缓存」暂存，miss 时由
    /// <paramref name="externalBase"/> 从盘上已写入的 loose 对象按 sha 读回。否则 4 万对象
    /// 大仓库会把全部解压内容堆进内存 → 手机 OOM 直接闪退（实测 VML.git 解到 3 万对象崩溃；
    /// 本仓 my-coder 仅 12k 对象，delta base 即有 233MB）。
    /// <paramref name="externalBase"/>：按 sha 返回 (type, content)，查不到返回 null。
    /// 返回写入回调的对象数。
    /// </summary>
    public static int Read(
        byte[] pack,
        Func<string, (string Type, byte[] Content)?>? externalBase,
        Action<string, string, byte[]?> onObject,
        Action<int, int>? onProgress = null,
        Action<string, string, IntPtr, long>? onLargeObject = null,
        Func<string, (string Type, IntPtr Ptr, long Len)?>? readBaseNative = null)
    {
        if (pack.Length < 12) throw new InvalidDataException("packfile 过短");
        if (pack[0] != 'P' || pack[1] != 'A' || pack[2] != 'C' || pack[3] != 'K')
            throw new InvalidDataException("非 packfile（缺 PACK 魔数）");

        int count = ReadInt32BE(pack, 8);
        if (count <= 0) return 0;

        // offset → sha：ofs-delta 引用 base 时先取其 sha，再由 externalBase 读回内容
        var shaByOffset = new Dictionary<long, string>();
        // base 内容缓存：有字节上限的 LRU（内存有界）；miss 走 externalBase（盘上 loose 对象）
        var cache = new BaseCache(maxBytes: 24 * 1024 * 1024);

        int pos = 12;
        int written = 0;
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
            string? refBaseSha = null;
            if (type == 6) // ofs-delta：变长负偏移
            {
                byte c = pack[pos++];
                long off = c & 0x7F;
                while ((c & 0x80) != 0)
                {
                    c = pack[pos++];
                    off = ((off + 1) << 7) | (long)(c & 0x7F);
                }
                baseOffset = objOffset - off;
            }
            else if (type == 7) // ref-delta：20 字节 base sha
            {
                refBaseSha = Convert.ToHexString(pack, pos, 20).ToLowerInvariant();
                pos += 20;
            }

            // ── 大对象原生路径：内容写入原生内存，不占托管堆（安卓堆上限装不下几百 MB 单对象，
            //    实测 VML 单对象 593MB，largeHeap 512MB 也装不下）──
            if (type is >= 1 and <= 4 && size > LargeThreshold && onLargeObject != null)
            {
                unsafe
                {
                    byte* ptr = (byte*)NativeMemory.Alloc((nuint)size);
                    try
                    {
                        int consumedN = Inflater.DecompressInto(new Span<byte>(ptr, (int)size), pack, pos);
                        pos += consumedN;
                        var gitTypeN = TypeName(type);
                        var shaN = ObjectShaNative(gitTypeN, ptr, size);
                        shaByOffset[objOffset] = shaN;
                        onLargeObject(gitTypeN, shaN, (IntPtr)ptr, size);
                        onObject(gitTypeN, shaN, null);   // content null = 已原生写盘
                        written++;
                    }
                    finally { NativeMemory.Free(ptr); }
                }
                continue;
            }

            // 解压该对象的 zlib 数据（按头部声明 size 精确分配，单遍解压，不翻倍物化）
            if (size > int.MaxValue) throw new InvalidDataException($"对象过大（{size} 字节）");
            var inflated = new byte[(int)size];
            int consumed = Inflater.DecompressInto(inflated, pack, pos);
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
                (gitType, content) = ApplyDeltaObject(type, inflated, baseOffset, refBaseSha, shaByOffset, cache, externalBase);
            }

            var sha = ObjectSha(gitType, content);
            shaByOffset[objOffset] = sha;        // 记录 offset→sha（供后续 ofs-delta 定位 base）
            cache.Put(sha, gitType, content);    // 最近对象入缓存；超上限自动淘汰，miss 读盘
            onObject(gitType, sha, content);     // 立即写盘
            written++;
        }

        return written;
    }

    /// <summary>解析 delta 的 base 并应用 delta（ofs → offset 索引；ref → sha；缓存未命中走 externalBase 读盘）。</summary>
    static (string Type, byte[] Content) ApplyDeltaObject(
        int type, byte[] inflated, long baseOffset, string? refBaseSha,
        Dictionary<long, string> shaByOffset, BaseCache cache,
        Func<string, (string Type, byte[] Content)?>? externalBase)
    {
        string baseSha;
        if (type == 6)
        {
            if (!shaByOffset.TryGetValue(baseOffset, out var bs))
                throw new InvalidDataException($"delta 对象的 base 未找到（ofs@{baseOffset}）");
            baseSha = bs;
        }
        else
        {
            baseSha = refBaseSha!;
        }

        var bm = cache.Get(baseSha);
        if (bm == null && externalBase != null) bm = externalBase(baseSha);
        if (bm == null)
            throw new InvalidDataException($"delta 对象的 base 未找到（{baseSha}）");

        return (bm.Value.Type, ApplyDelta(bm.Value.Content, inflated));
    }

    /// <summary>从 packfile 临时文件读取对象数（大端序 @8）。</summary>
    public static int ReadObjectCount(string packPath)
    {
        using var fs = File.OpenRead(packPath);
        var h = new byte[12];
        if (fs.Read(h, 0, 12) != 12) return 0;
        return (h[8] << 24) | (h[9] << 16) | (h[10] << 8) | h[11];
    }

    /// <summary>
    /// 从文件解码 packfile（大仓库 pack 数百 MB，整包读进内存会 OOM 闪退——安卓堆上限 256MB，
    /// 实测 VML.git 单次 ~284MB 分配即崩）。逐对象从文件读块解压，内存上界 ≈
    /// 单对象解压大小 + base LRU（24MB）+ 索引；其余语义同 <see cref="Read"/>。
    /// </summary>
    public static int ReadFile(
        string packPath,
        Func<string, (string Type, byte[] Content)?>? externalBase,
        Action<string, string, byte[]?> onObject,
        Action<int, int>? onProgress = null,
        Action<string, string, IntPtr, long>? onLargeObject = null,
        Func<string, (string Type, IntPtr Ptr, long Len)?>? readBaseNative = null)
    {
        const long MaxObjectBytes = 64L * 1024 * 1024;   // 单对象压缩数据读取上限（防病态超大对象）
        using var fs = File.OpenRead(packPath);
        var header = new byte[12];
        if (fs.Read(header, 0, 12) != 12) throw new InvalidDataException("packfile 过短");
        if (header[0] != 'P' || header[1] != 'A' || header[2] != 'C' || header[3] != 'K')
            throw new InvalidDataException("非 packfile（缺 PACK 魔数）");
        int count = (header[8] << 24) | (header[9] << 16) | (header[10] << 8) | header[11];
        if (count <= 0) return 0;

        var shaByOffset = new Dictionary<long, string>();
        var cache = new BaseCache(maxBytes: 24 * 1024 * 1024);

        long objOffset = 12;
        int written = 0;
        var headBuf = new byte[80];   // 对象头最大约 25 字节（varint + ofs/ref 定位）
        for (int i = 0; i < count; i++)
        {
            if ((i & 0x3F) == 0 || i + 1 == count)
                onProgress?.Invoke(i + 1, count);
            long currentOffset = objOffset;

            // 读对象头：首字节 bit7=续位、bit6-4=类型、bit3-0=size 低 4 位
            fs.Seek(objOffset, SeekOrigin.Begin);
            int hn = fs.Read(headBuf, 0, headBuf.Length);
            if (hn < 2) throw new InvalidDataException("packfile 数据截断");
            int pos = 0;
            byte b = headBuf[pos++];
            int type = (b >> 4) & 0x07;
            long size = b & 0x0F;
            int shift = 4;
            while ((b & 0x80) != 0) { b = headBuf[pos++]; size |= (long)(b & 0x7F) << shift; shift += 7; }

            long baseOffset = -1;
            string? refBaseSha = null;
            if (type == 6) // ofs-delta：变长负偏移
            {
                byte c = headBuf[pos++];
                long off = c & 0x7F;
                while ((c & 0x80) != 0) { c = headBuf[pos++]; off = ((off + 1) << 7) | (long)(c & 0x7F); }
                baseOffset = objOffset - off;
            }
            else if (type == 7) // ref-delta：20 字节 base sha
            {
                refBaseSha = Convert.ToHexString(headBuf, pos, 20).ToLowerInvariant();
                pos += 20;
            }

            // ── 大对象原生路径：内容写入原生内存（压缩数据也从文件读原生缓冲），不占托管堆。
            //    安卓堆上限（256MB 或 largeHeap 512MB）装不下几百 MB 单对象（实测 VML 593MB）──
            if (type is >= 1 and <= 4 && size > LargeThreshold && onLargeObject != null)
            {
                unsafe
                {
                    byte* ptr = (byte*)NativeMemory.Alloc((nuint)size);
                    try
                    {
                        int consumedN = InflateAtInto(new Span<byte>(ptr, (int)size), fs, objOffset + pos, MaxObjectBytes);
                        objOffset += pos + consumedN;
                        var gitTypeN = TypeName(type);
                        var shaN = ObjectShaNative(gitTypeN, ptr, size);
                        shaByOffset[currentOffset] = shaN;
                        onLargeObject(gitTypeN, shaN, (IntPtr)ptr, size);
                        onObject(gitTypeN, shaN, null);   // content null = 已原生写盘
                        written++;
                    }
                    finally { NativeMemory.Free(ptr); }
                }
                continue;
            }

            // 解压该对象：小对象走托管 InflateAt；大对象（stored 块压缩数据也大）压缩数据读入原生缓冲，
            // 不占托管堆（安卓堆 256MB 上限，几百 MB 对象物化 byte[] 会 OOM）
            long compressedFileOffset = objOffset + pos;
            byte[] inflated;
            int consumed;
            if (size > LargeThreshold)
            {
                if (size > int.MaxValue) throw new InvalidDataException($"对象过大（{size} 字节）");
                inflated = new byte[(int)size];
                consumed = InflateAtInto(inflated, fs, compressedFileOffset, MaxObjectBytes);
            }
            else
            {
                var (data, c) = InflateAt(fs, compressedFileOffset, MaxObjectBytes);
                inflated = data;
                consumed = c;
            }
            objOffset = compressedFileOffset + consumed;

            string gitType;
            byte[] content;
            if (type is >= 1 and <= 4)
            {
                gitType = TypeName(type);
                content = inflated;
            }
            else
            {
                (gitType, content) = ApplyDeltaObject(type, inflated, baseOffset, refBaseSha, shaByOffset, cache, externalBase);
            }

            var sha = ObjectSha(gitType, content);
            shaByOffset[currentOffset] = sha;      // 记录 offset→sha（供后续 ofs-delta 定位 base）
            cache.Put(sha, gitType, content);      // 最近对象入缓存；超上限自动淘汰，miss 读盘
            onObject(gitType, sha, content);       // 立即写盘
            written++;
        }

        return written;
    }

    /// <summary>从文件偏移处读取并解压一个对象的 zlib 数据（块不够大时自动加大重读，不整包读入内存）。</summary>
    static (byte[] Data, int Consumed) InflateAt(FileStream fs, long fileOffset, long maxBytes)
    {
        int cap = (int)Math.Min(maxBytes, 1 << 20);
        while (true)
        {
            fs.Seek(fileOffset, SeekOrigin.Begin);
            var buf = new byte[cap];
            int n = fs.Read(buf, 0, cap);
            if (n <= 0) throw new InvalidDataException("packfile 数据截断");
            byte[] input = n == cap ? buf : buf[..n];
            try
            {
                return Inflater.Decompress(input, 0);
            }
            catch (EndOfStreamException) when (n == cap && cap < maxBytes)
            {
                cap = (int)Math.Min(cap * 2L, maxBytes);   // 块不够：加倍重读
            }
        }
    }

    /// <summary>
    /// 大对象解压：压缩数据读入原生缓冲（不占托管堆），解压到 output。
    /// 安卓托管堆 256MB 上限——几百 MB 对象（尤其 stored 块，压缩数据≈内容）若压缩数据也物化
    /// 托管 byte[] 会二次占内存 OOM。
    /// </summary>
    static unsafe int InflateAtInto(Span<byte> output, FileStream fs, long fileOffset, long maxBytes)
    {
        int cap = (int)Math.Min(maxBytes, 1 << 20);
        while (true)
        {
            byte* inputPtr = (byte*)NativeMemory.Alloc((nuint)cap);
            try
            {
                fs.Seek(fileOffset, SeekOrigin.Begin);
                int n = 0;
                while (n < cap)
                {
                    int r = fs.Read(new Span<byte>(inputPtr + n, cap - n));
                    if (r <= 0) break;
                    n += r;
                }
                if (n <= 0) throw new InvalidDataException("packfile 数据截断");
                try
                {
                    return Inflater.DecompressInto(output, new ReadOnlySpan<byte>(inputPtr, n), 0);
                }
                catch (EndOfStreamException) when (n == cap && cap < maxBytes)
                {
                    cap = (int)Math.Min(cap * 2L, maxBytes);   // 块不够：加倍重读
                }
            }
            finally { NativeMemory.Free(inputPtr); }
        }
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
        // 流式 sha：不拼 header+content 大数组（几百 MB 对象会二次占内存）
        var header = Encoding.UTF8.GetBytes($"{type} {content.Length}\0");
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        hash.AppendData(header);
        hash.AppendData(content);
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    /// <summary>原生内容计算 git 对象 sha（流式 IncrementalHash，不物化托管数组）。</summary>
    internal static unsafe string ObjectShaNative(string type, byte* content, long length)
    {
        var header = Encoding.UTF8.GetBytes($"{type} {length}\0");
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        hash.AppendData(header);
        // 分块 AppendData：安卓 Mono 对非数组背书的原生 Span 会物化托管副本（实测 593MB 一次 AppendData → OOM）
        const int Chunk = 1 << 20;   // 1MB
        long off = 0;
        while (off < length)
        {
            int n = (int)Math.Min(Chunk, length - off);
            hash.AppendData(new ReadOnlySpan<byte>(content + off, n));
            off += n;
        }
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
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

/// <summary>
/// delta base 内容缓存（LRU，按字节上限）：主扫描把最近解出的对象入缓存，
/// delta 引用 base 时优先命中；超限淘汰最久未用，miss 由调用方经 externalBase 从盘上读回。
/// 内存上界 = maxBytes，保证 4 万对象大仓库解压不 OOM。
/// </summary>
sealed class BaseCache
{
    private readonly int _maxBytes;
    private readonly Dictionary<string, (string Type, byte[] Content, LinkedListNode<string> Node)> _map = new(StringComparer.Ordinal);
    private readonly LinkedList<string> _order = new();
    private int _bytes;

    public BaseCache(int maxBytes) => _maxBytes = maxBytes;

    /// <summary>取 base 内容并提升为最近使用；未缓存返回 null。</summary>
    public (string Type, byte[] Content)? Get(string sha)
    {
        if (_map.TryGetValue(sha, out var e))
        {
            _order.Remove(e.Node);
            _order.AddFirst(e.Node);
            return (e.Type, e.Content);
        }
        return null;
    }

    /// <summary>放入 base 内容；超过字节上限时淘汰最久未用（单对象超过上限则不缓存）。</summary>
    public void Put(string sha, string type, byte[] content)
    {
        if (content.Length > _maxBytes) return;
        if (_map.TryGetValue(sha, out var ex))
        {
            _order.Remove(ex.Node);
            _bytes -= ex.Content.Length;
            _map.Remove(sha);
        }
        var node = _order.AddFirst(sha);
        _map[sha] = (type, content, node);
        _bytes += content.Length;
        while (_bytes > _maxBytes && _order.Count > 1)
        {
            var lastKey = _order.Last!.Value;
            _order.RemoveLast();
            _bytes -= _map[lastKey].Content.Length;
            _map.Remove(lastKey);
        }
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
        byte b = (byte)(((long)type << 4) | (size & 0x0F));
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
    public static (byte[] Data, int Consumed) Decompress(ReadOnlySpan<byte> input, int offset)
    {
        // 两遍：先推进拿大小，再分配精确数组解第二遍（避免 MemoryStream 翻倍物化大对象）
        var (outLen, consumed) = InflateCore(default, input, offset);
        var data = new byte[outLen];
        InflateCore(data, input, offset);
        return (data, consumed);
    }

    /// <summary>解压到指定缓冲（可为原生 Span，大对象不占托管堆）。返回消耗的压缩字节数。</summary>
    public static int DecompressInto(Span<byte> output, ReadOnlySpan<byte> input, int offset)
    {
        var (_, consumed) = InflateCore(output, input, offset);
        return consumed;
    }

    /// <summary>只推进偏移、不解出内容（packfile 预扫描定位对象边界用，省去物化输出内存）。</summary>
    public static int Skip(ReadOnlySpan<byte> input, int offset)
        => InflateCore(default, input, offset).Consumed;

    static (int OutLen, int Consumed) InflateCore(Span<byte> output, ReadOnlySpan<byte> input, int offset)
    {
        int pos = offset;
        byte cmf = input[pos++];
        byte flg = input[pos++];
        if ((cmf & 0x0F) != 8) throw new InvalidDataException("非 deflate 压缩方法");
        if ((cmf >> 4) > 7) throw new InvalidDataException("zlib 窗口过大");
        if ((((cmf << 8) | flg) % 31) != 0) throw new InvalidDataException("zlib header 校验失败");
        if ((flg & 0x20) != 0) pos += 4; // FDICT：跳过 4 字节 dictid（git 不用 preset dictionary）

        var br = new BitReader(input, pos);
        int outLen = 0;   // output 为空（Skip）时仅推进长度，不落内容

        bool final;
        do
        {
            final = br.ReadBit() != 0;
            int btype = br.ReadBits(2);
            switch (btype)
            {
                case 0: DecodeStored(ref br, output, ref outLen); break;
                case 1: DecodeHuffman(ref br, output, FixedLitLen, FixedDist, ref outLen); break;
                case 2: DecodeDynamic(ref br, output, ref outLen); break;
                default: throw new InvalidDataException($"非法 deflate 块类型 {btype}");
            }
        } while (!final);

        br.AlignToByte();
        pos = br.BytePosition + 4; // 跳过 adler32
        return (outLen, pos - offset);
    }

    // ── stored 块（BTYPE=00）──
    static void DecodeStored(ref BitReader br, Span<byte> output, ref int outLen)
    {
        br.AlignToByte();
        int len = br.ReadByte() | (br.ReadByte() << 8);
        int nlen = br.ReadByte() | (br.ReadByte() << 8);
        if ((len ^ 0xFFFF) != nlen) throw new InvalidDataException("stored 块长度校验失败");
        if (output.IsEmpty) { outLen += len; br.SkipBytes(len); }
        else
        {
            for (int i = 0; i < len; i++) output[outLen++] = (byte)br.ReadByte();
        }
    }

    // ── Huffman 块（BTYPE=01 固定 / 02 动态解码后共用）──
    static void DecodeHuffman(ref BitReader br, Span<byte> output, HuffmanDecoder litlen, HuffmanDecoder dist, ref int outLen)
    {
        while (true)
        {
            int sym = litlen.Decode(ref br);
            if (sym < 256)
            {
                if (output.IsEmpty) outLen++;
                else output[outLen++] = (byte)sym;
            }
            else if (sym == 256)
            {
                return; // 块结束
            }
            else
            {
                int length = LengthBase[sym - 257] + br.ReadBits(LengthExtra[sym - 257]);
                int distSym = dist.Decode(ref br);
                int distance = DistBase[distSym] + br.ReadBits(DistExtra[distSym]);
                if (output.IsEmpty) outLen += length;
                else
                {
                    // 历史回读（可重叠：读到的字节写回同缓冲，后续 copy 能引用到）
                    for (int i = 0; i < length; i++)
                    {
                        output[outLen] = output[outLen - distance];
                        outLen++;
                    }
                }
            }
        }
    }

    static void DecodeDynamic(ref BitReader br, Span<byte> output, ref int outLen)
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
            int sym = codeLenDecoder.Decode(ref br);
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
        DecodeHuffman(ref br, output, litlen, dist, ref outLen);
    }

    // ── 位读取器（LSB-first，deflate Huffman 位序）；ref struct 支持原生 Span 输入（大对象压缩数据不占托管堆）──
    ref struct BitReader
    {
        private readonly ReadOnlySpan<byte> _data;
        private int _pos;
        private uint _buf;
        private int _bits;

        public BitReader(ReadOnlySpan<byte> data, int pos) { _data = data; _pos = pos; }

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

        /// <summary>直接跳过 n 个字节（Skip 模式下 stored 块不读内容）。</summary>
        public void SkipBytes(int n)
        {
            if (_pos + n > _data.Length) throw new EndOfStreamException("数据截断");
            _pos += n;
        }

        public byte[] ReadBytes(int n)
        {
            // 必须与 SkipBytes 一致抛 EndOfStreamException：分块解码靠它识别「块不够大」自动加大重读；
            // Array.Copy 的 ArgumentException 会逃逸成诡异崩溃。仅小对象 stored 块用（大对象走 span 直接拷贝）
            if (_pos + n > _data.Length) throw new EndOfStreamException("数据截断");
            var b = new byte[n];
            _data.Slice(_pos, n).CopyTo(b);
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

        public int Decode(ref BitReader br)
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
