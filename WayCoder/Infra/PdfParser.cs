using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace WayCoder.Infra;

// ═══════════════════════════════════════════════════════════════
//  PDF 对象模型（纯手搓，替代 PdfPig）
// ═══════════════════════════════════════════════════════════════

internal abstract class PdfObj { }

internal sealed class PdfNull : PdfObj { public static readonly PdfNull Instance = new(); }

internal sealed class PdfBool : PdfObj { public bool Value; }

internal sealed class PdfNum : PdfObj { public double Value; }

/// <summary>PDF 名字对象（/Name，不含前导斜杠）。</summary>
internal sealed class PdfName : PdfObj { public string Name = ""; }

/// <summary>PDF 字符串对象（原始字节，解码取决于字体编码）。</summary>
internal sealed class PdfStr : PdfObj { public byte[] Bytes = []; }

internal sealed class PdfArray : PdfObj { public List<PdfObj> Items = new(); }

internal sealed class PdfDict : PdfObj
{
    public Dictionary<string, PdfObj> Entries = new();
    public PdfObj? Get(string key) => Entries.TryGetValue(key, out var v) ? v : null;
}

/// <summary>PDF 间接引用（N G R）。</summary>
internal sealed class PdfRef : PdfObj { public long Num; public long Gen; }

/// <summary>PDF 流对象（Dict 为流字典，Data 为未解压的原始流字节）。</summary>
internal sealed class PdfStream : PdfObj { public PdfDict Dict = new(); public byte[] Data = []; }

/// <summary>裸关键字（如 obj/endobj/stream/BT/Tj 等，不属于对象值的符号）。</summary>
internal sealed class PdfKeyword : PdfObj { public string Word = ""; }

/// <summary>ToUnicode CMap：字节码到 Unicode 的映射表。</summary>
internal sealed class PdfCMap
{
    public int CodeSpaceLen = 1;                                   // 单字节码长度（字节）
    public Dictionary<int, string> CharMap = new();                // 单码 → Unicode 字符串
    public List<(int Lo, int Hi, string Dst)> RangeMap = new();    // 范围 → 起始 Unicode（按序递增）
}

/// <summary>
/// 手搓 PDF 解析器（纯 BCL，AOT 安全，零第三方依赖）——替代 PdfPig。
///
/// 实现 PDF 文本提取所需的最小能力集：
///   文件结构（xref 表 / xref 流 + 间接对象）、FlateDecode/ASCIIHex/ASCII85 解压、
///   页面树遍历、内容流文本操作（Tj/TJ/Tf/Td/Tm）、
///   常见字体编码（WinAnsi=CP1252 / UTF-16BE / Identity-H / ToUnicode CMap / Differences）。
///
/// 已知边界（诚实标注，非崩溃）：
///   - 不解析加密 PDF；不解析 LZW 过滤（罕见）；不支持 object stream 内的压缩对象（type 2 xref）。
///   - 预定义 CJK CMap（如 UniGB-UCS2-H）无内置表，仅支持 /ToUnicode 与 /Identity-H 的 CJK。
///   - MacRoman / PDFDoc / Standard 编码以 Latin-1 近似。
/// </summary>
public sealed class PdfParser
{
    private readonly byte[] _buf;
    private readonly Dictionary<long, long> _objOffsets = new();   // 对象号 → 文件偏移
    private readonly Dictionary<long, PdfObj?> _objCache = new();  // 对象号 → 已解析对象（缓存）
    private long _rootRef = -1;
    private long _infoRef = -1;
    private readonly List<long> _pageRefs = new();                 // 页面对象号（1-based 顺序）
    private readonly HashSet<long> _pagesVisited = new();          // CollectPages 环检测（防循环 Kids 递归）
    private int _nestingDepth;                                     // ParseDict/ParseArray 递归深度（防深层嵌套栈溢出）
    private const int MaxNestingDepth = 128;

    private PdfParser(byte[] data) => _buf = data;

    /// <summary>打开 PDF 文件。解析失败返回 null。</summary>
    public static PdfParser? Open(string filePath)
    {
        try { return Open(File.ReadAllBytes(filePath)); }
        catch { return null; }
    }

    /// <summary>从字节数组打开 PDF。解析失败返回 null。</summary>
    public static PdfParser? Open(byte[] data)
    {
        if (data.Length < 8) return null;
        try
        {
            var p = new PdfParser(data);
            return p.Init() ? p : null;
        }
        catch { return null; }
    }

    /// <summary>总页数。</summary>
    public int NumberOfPages => _pageRefs.Count;

    /// <summary>文档标题（来自 /Info /Title）。</summary>
    public string? Title
    {
        get
        {
            try
            {
                var info = Resolve(GetObject(_infoRef)) as PdfDict;
                if (info?.Get("Title") is PdfStr t)
                    return DecodeInfoString(t.Bytes);
                return null;
            }
            catch { return null; }
        }
    }

    /// <summary>提取第 pageNumber 页（1-based）的纯文本。</summary>
    public string ExtractPageText(int pageNumber)
    {
        if (pageNumber < 1 || pageNumber > _pageRefs.Count) return "";
        var page = Resolve(GetObject(_pageRefs[pageNumber - 1])) as PdfDict;
        if (page == null) return "";

        var resources = Resolve(page.Get("Resources")) as PdfDict;
        var fonts = Resolve(resources?.Get("Font")) as PdfDict;

        var sb = new StringBuilder();
        foreach (var data in CollectContentStreams(page.Get("Contents")))
            ExtractTextFromStream(data, fonts, sb);
        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════
    //  结构解析
    // ═══════════════════════════════════════════════════════════

    private bool Init()
    {
        // 校验 %PDF 头
        if (!(_buf[0] == '%' && _buf[1] == 'P' && _buf[2] == 'D' && _buf[3] == 'F')) return false;

        var startxref = FindLastKeyword("startxref");
        if (startxref < 0) return false;

        int pos = startxref + "startxref".Length;
        SkipWs(ref pos);
        var numStart = pos;
        ReadNumber(ref pos);
        long xrefOffset = (long)ParseDouble(Ascii(numStart, pos));

        ParseXrefAt((int)xrefOffset, new HashSet<long>());
        if (_rootRef < 0) return false;

        // 收集页面（Catalog → Pages → Kids）
        var catalog = Resolve(GetObject(_rootRef)) as PdfDict;
        if (catalog?.Get("Pages") is PdfRef pagesRef)
            CollectPages(pagesRef.Num);
        return _pageRefs.Count > 0;
    }

    private void ParseXrefAt(int offset, HashSet<long> visited) => ParseXrefAt(offset, visited, 0);

    private void ParseXrefAt(int offset, HashSet<long> visited, int depth)
    {
        // 深度上限防恶意 PDF 超长 Prev 链递归栈溢出；visited 防循环
        if (depth > 32 || offset < 0 || offset >= _buf.Length || !visited.Add(offset)) return;
        int pos = offset;
        SkipWs(ref pos);

        if (MatchWord(ref pos, "xref"))
        {
            var trailer = ParseXrefTable(ref pos);
            if (trailer != null)
            {
                if (trailer.Get("Root") is PdfRef r) _rootRef = r.Num;
                if (trailer.Get("Info") is PdfRef i) _infoRef = i.Num;
                if (trailer.Get("Prev") is PdfNum pv)
                    ParseXrefAt((int)pv.Value, visited, depth + 1);
            }
        }
        else
        {
            // xref 流（PDF 1.5+）：startxref 指向一个 stream 对象
            pos = offset;
            var obj = ParseIndirectObjectAt(ref pos);
            if (obj is PdfStream st && (st.Dict.Get("Type") as PdfName)?.Name == "XRef")
            {
                ParseXrefStream(st, visited);
                if (st.Dict.Get("Prev") is PdfNum pv)
                    ParseXrefAt((int)pv.Value, visited, depth + 1);
            }
        }
    }

    private PdfDict? ParseXrefTable(ref int pos)
    {
        while (true)
        {
            SkipWs(ref pos);
            if (pos >= _buf.Length) return null;
            if (MatchWord(ref pos, "trailer"))
            {
                SkipWs(ref pos);
                return ParseValue(ref pos) as PdfDict;
            }
            // 子段：start count，然后 count 行 "offset gen flag"
            var s1 = pos; ReadNumber(ref pos);
            long start = (long)ParseDouble(Ascii(s1, pos));
            SkipWs(ref pos);
            var s2 = pos; ReadNumber(ref pos);
            int count = (int)ParseDouble(Ascii(s2, pos));
            // 钳制 xref 子段条目数：条目数不可能超过文件字节数，声明值（如 0 2147483647）超出即损坏，
            // 否则 for 循环数十亿次空转（每轮数据耗尽后 pos 不再前进仍继续）。
            if (count < 0 || count > _buf.Length) count = _buf.Length;
            for (int i = 0; i < count; i++)
            {
                SkipWs(ref pos);
                var o1 = pos; ReadNumber(ref pos);
                long off = (long)ParseDouble(Ascii(o1, pos));
                SkipWs(ref pos);
                var o2 = pos; ReadNumber(ref pos);
                SkipWs(ref pos);
                if (pos < _buf.Length && (_buf[pos] == 'n' || _buf[pos] == 'f'))
                {
                    bool inUse = _buf[pos] == 'n';
                    pos++;
                    if (inUse && off > 0) _objOffsets[start + i] = off;
                }
                // 跳到行尾
                while (pos < _buf.Length && _buf[pos] != '\n' && _buf[pos] != '\r') pos++;
                while (pos < _buf.Length && (_buf[pos] == '\n' || _buf[pos] == '\r')) pos++;
            }
        }
    }

    private void ParseXrefStream(PdfStream st, HashSet<long> visited)
    {
        var data = DecodeStream(st.Dict, st.Data);
        var w = st.Dict.Get("W") as PdfArray;
        if (w == null || w.Items.Count < 3) return;
        int w0 = (int)((w.Items[0] as PdfNum)?.Value ?? 0);
        int w1 = (int)((w.Items[1] as PdfNum)?.Value ?? 0);
        int w2 = (int)((w.Items[2] as PdfNum)?.Value ?? 0);
        if (w0 + w1 + w2 == 0) return;

        // 每条目至少消耗 1 字节（w0+w1+w2 ≥ 1），据此钳制 count，防不可信 Size/Index 字段超长死循环
        int maxEntries = data.Length + 1;

        long size = (long)((st.Dict.Get("Size") as PdfNum)?.Value ?? 0);
        var subsections = new List<(long start, int count)>();
        if (st.Dict.Get("Index") is PdfArray idx)
        {
            for (int i = 0; i + 1 < idx.Items.Count; i += 2)
            {
                double cnt = (idx.Items[i + 1] as PdfNum)?.Value ?? 0;
                subsections.Add(((long)((idx.Items[i] as PdfNum)?.Value ?? 0),
                                 cnt <= 0 ? 0 : (int)Math.Min(cnt, (double)maxEntries)));
            }
        }
        else if (size > 0)
        {
            subsections.Add((0, (int)Math.Min(size, (long)maxEntries)));
        }

        int p = 0;
        foreach (var (start, count) in subsections)
        {
            for (int i = 0; i < count; i++)
            {
                if (p >= data.Length) break; // 双保险：即使钳制不足也因数据耗尽而终止
                long f1 = ReadBigEndian(data, p, w0); p += w0;
                long f2 = ReadBigEndian(data, p, w1); p += w1;
                p += w2; // f3 未用
                if (f1 == 1 && f2 > 0) _objOffsets[start + i] = f2; // 未压缩对象
                // f1==2 为 object stream 压缩对象，MVP 不支持
            }
        }
    }

    private int FindLastKeyword(string keyword)
    {
        var needle = Encoding.ASCII.GetBytes(keyword);
        for (int i = _buf.Length - needle.Length; i >= 0; i--)
        {
            bool match = true;
            for (int j = 0; j < needle.Length; j++)
                if (_buf[i + j] != needle[j]) { match = false; break; }
            if (match) return i;
        }
        return -1;
    }

    // ═══════════════════════════════════════════════════════════
    //  对象解析
    // ═══════════════════════════════════════════════════════════

    private PdfObj? GetObject(long num)
    {
        if (_objCache.TryGetValue(num, out var cached)) return cached;
        if (!_objOffsets.TryGetValue(num, out var off)) return null;
        int pos = (int)off;
        var obj = ParseIndirectObjectAt(ref pos);
        _objCache[num] = obj;
        return obj;
    }

    private PdfObj? Resolve(PdfObj? obj)
    {
        int guard = 0;
        while (obj is PdfRef r && guard++ < 32)
            obj = GetObject(r.Num);
        return obj;
    }

    private PdfObj? ParseIndirectObjectAt(ref int pos)
    {
        SkipWs(ref pos);
        ReadNumber(ref pos); // 对象号
        SkipWs(ref pos);
        ReadNumber(ref pos); // gen
        SkipWs(ref pos);
        if (!MatchWord(ref pos, "obj")) return null;
        SkipWs(ref pos);
        return ParseValue(ref pos);
    }

    private PdfObj? ParseValue(ref int pos)
    {
        SkipWs(ref pos);
        if (pos >= _buf.Length) return null;
        byte c = _buf[pos];

        if (c == '<' && pos + 1 < _buf.Length && _buf[pos + 1] == '<') // 字典（可能后接 stream）
        {
            var dict = ParseDict(ref pos);
            int save = pos;
            SkipWs(ref pos);
            if (pos + 5 < _buf.Length && MatchWordAt(pos, "stream"))
            {
                pos += "stream".Length;
                if (pos < _buf.Length && _buf[pos] == '\r') pos++;
                if (pos < _buf.Length && _buf[pos] == '\n') pos++;
                long length = GetLong(dict, "Length");
                int avail = _buf.Length - pos;
                int len = (int)Math.Min(length < 0 ? 0 : length, avail);
                var data = new byte[len];
                Array.Copy(_buf, pos, data, 0, len);
                pos += len;
                return new PdfStream { Dict = dict, Data = data };
            }
            pos = save;
            return dict;
        }
        if (c == '<') return ParseHexString(ref pos);
        if (c == '[') return ParseArray(ref pos);
        if (c == '(') return ParseLiteralString(ref pos);
        if (c == '/') return ParseName(ref pos);
        if (IsNumberStart(c)) return ParseNumberOrRef(ref pos);
        if (c == '+' || c == '-' || c == '.') return ParseNumberOrRef(ref pos);
        if (IsRegular(c)) return ParseKeyword(ref pos);

        pos++;
        return null;
    }

    private PdfDict ParseDict(ref int pos)
    {
        // 递归深度护栏：恶意深层嵌套 <<...>> 字典防栈溢出，超深则返回空字典中止
        if (_nestingDepth >= MaxNestingDepth) { pos++; return new PdfDict(); }
        _nestingDepth++;
        try
        {
            pos += 2; // 跳过 <<
            var dict = new PdfDict();
            while (true)
            {
                SkipWs(ref pos);
                if (pos + 1 < _buf.Length && _buf[pos] == '>' && _buf[pos + 1] == '>') { pos += 2; break; }
                if (pos >= _buf.Length) break;
                if (_buf[pos] == '/')
                {
                    var name = ParseName(ref pos);
                    SkipWs(ref pos);
                    var val = ParseValue(ref pos);
                    if (val != null) dict.Entries[name.Name] = val;
                }
                else pos++;
            }
            return dict;
        }
        finally { _nestingDepth--; }
    }

    private PdfArray ParseArray(ref int pos)
    {
        // 递归深度护栏：恶意深层嵌套 [...] 数组防栈溢出，超深则返回空数组中止
        if (_nestingDepth >= MaxNestingDepth) { pos++; return new PdfArray(); }
        _nestingDepth++;
        try
        {
            pos++; // 跳过 [
            var arr = new PdfArray();
            while (true)
            {
                SkipWs(ref pos);
                if (pos < _buf.Length && _buf[pos] == ']') { pos++; break; }
                if (pos >= _buf.Length) break;
                var val = ParseValue(ref pos);
                if (val == null) pos++;
                else arr.Items.Add(val);
            }
            return arr;
        }
        finally { _nestingDepth--; }
    }

    private PdfName ParseName(ref int pos)
    {
        pos++; // 跳过 /
        var sb = new StringBuilder();
        while (pos < _buf.Length && IsRegular(_buf[pos]))
        {
            if (_buf[pos] == '#' && pos + 2 < _buf.Length)
            {
                int v = HexVal(_buf[pos + 1]) * 16 + HexVal(_buf[pos + 2]);
                if (v >= 0) { sb.Append((char)v); pos += 3; continue; }
            }
            sb.Append((char)_buf[pos]);
            pos++;
        }
        return new PdfName { Name = sb.ToString() };
    }

    private PdfStr ParseLiteralString(ref int pos)
    {
        pos++; // 跳过 (
        var ms = new MemoryStream();
        int depth = 1;
        while (pos < _buf.Length && depth > 0)
        {
            byte c = _buf[pos++];
            if (c == '\\' && pos < _buf.Length)
            {
                byte e = _buf[pos++];
                switch (e)
                {
                    case (byte)'n': ms.WriteByte((byte)'\n'); break;
                    case (byte)'r': ms.WriteByte((byte)'\r'); break;
                    case (byte)'t': ms.WriteByte((byte)'\t'); break;
                    case (byte)'b': ms.WriteByte((byte)'\b'); break;
                    case (byte)'f': ms.WriteByte((byte)'\f'); break;
                    case (byte)'(': ms.WriteByte((byte)'('); break;
                    case (byte)')': ms.WriteByte((byte)')'); break;
                    case (byte)'\\': ms.WriteByte((byte)'\\'); break;
                    default:
                        if (e >= '0' && e <= '7')
                        {
                            int val = e - '0', n = 1;
                            while (n < 3 && pos < _buf.Length && _buf[pos] >= '0' && _buf[pos] <= '7')
                            { val = val * 8 + (_buf[pos++] - '0'); n++; }
                            ms.WriteByte((byte)(val & 0xFF));
                        }
                        else ms.WriteByte(e);
                        break;
                }
            }
            else if (c == '(') { depth++; ms.WriteByte(c); }
            else if (c == ')') { depth--; if (depth > 0) ms.WriteByte(c); }
            else ms.WriteByte(c);
        }
        return new PdfStr { Bytes = ms.ToArray() };
    }

    private PdfStr ParseHexString(ref int pos)
    {
        pos++; // 跳过 <
        var ms = new MemoryStream();
        int hi = -1;
        while (pos < _buf.Length)
        {
            byte c = _buf[pos];
            if (c == '>') { pos++; break; }
            int v = HexVal(c);
            if (v >= 0)
            {
                if (hi < 0) hi = v;
                else { ms.WriteByte((byte)((hi << 4) | v)); hi = -1; }
            }
            pos++;
        }
        if (hi >= 0) ms.WriteByte((byte)(hi << 4));
        return new PdfStr { Bytes = ms.ToArray() };
    }

    private PdfObj ParseKeyword(ref int pos)
    {
        int start = pos;
        while (pos < _buf.Length && IsRegular(_buf[pos])) pos++;
        string word = Encoding.ASCII.GetString(_buf, start, pos - start);
        return word switch
        {
            "true" => new PdfBool { Value = true },
            "false" => new PdfBool { Value = false },
            "null" => PdfNull.Instance,
            _ => new PdfKeyword { Word = word },
        };
    }

    private PdfObj ParseNumberOrRef(ref int pos)
    {
        int start = pos;
        ReadNumber(ref pos);
        double num1 = ParseDouble(Ascii(start, pos));
        int save = pos;
        SkipWs(ref pos);
        int genStart = pos;
        if (pos < _buf.Length && IsNumberStart(_buf[pos]))
        {
            ReadNumber(ref pos);
            int genEnd = pos;
            SkipWs(ref pos);
            if (pos < _buf.Length && _buf[pos] == 'R' && (pos + 1 >= _buf.Length || !IsRegular(_buf[pos + 1])))
            {
                pos++;
                double gen = ParseDouble(Ascii(genStart, genEnd));
                return new PdfRef { Num = (long)num1, Gen = (long)gen };
            }
        }
        pos = save;
        return new PdfNum { Value = num1 };
    }

    // ═══════════════════════════════════════════════════════════
    //  流解压
    // ═══════════════════════════════════════════════════════════

    private static byte[] DecodeStream(PdfDict dict, byte[] data)
    {
        if (dict.Get("Filter") is PdfName name) return DecodeOne(name.Name, data);
        if (dict.Get("Filter") is PdfArray arr)
        {
            var current = data;
            foreach (var f in arr.Items)
                if (f is PdfName fn) current = DecodeOne(fn.Name, current);
            return current;
        }
        return data;
    }

    private static byte[] DecodeOne(string filter, byte[] data)
    {
        switch (filter)
        {
            case "FlateDecode" or "Fl":
                try
                {
                    using var ms = new MemoryStream(data);
                    using var z = new ZLibStream(ms, CompressionMode.Decompress);
                    using var outMs = new MemoryStream();
                    // 限制解压输出上限，防 zip bomb（恶意小文件声明高压缩比 → 解压出数 GB 内存尖峰/OOM）。
                    // 64MB 对齐 OfficeExtractor 条目上限，正常 PDF 内容流远小于此。
                    const int maxOut = 64 * 1024 * 1024;
                    var buffer = new byte[64 * 1024];
                    int total = 0, n;
                    while ((n = z.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (total + n > maxOut) return data; // 解压超限，返回原始压缩数据（视为损坏）
                        outMs.Write(buffer, 0, n);
                        total += n;
                    }
                    return outMs.ToArray();
                }
                catch { return data; }
            case "ASCIIHexDecode" or "AHx":
                return DecodeAsciiHex(data);
            case "ASCII85Decode" or "A85":
                return DecodeAscii85(data);
            default:
                return data; // LZW 等罕见 filter，原样返回
        }
    }

    private static byte[] DecodeAsciiHex(byte[] data)
    {
        var ms = new MemoryStream();
        int hi = -1;
        foreach (var b in data)
        {
            if (b == '>') break;
            int v = HexVal(b);
            if (v < 0) continue;
            if (hi < 0) hi = v;
            else { ms.WriteByte((byte)((hi << 4) | v)); hi = -1; }
        }
        return ms.ToArray();
    }

    private static byte[] DecodeAscii85(byte[] data)
    {
        var ms = new MemoryStream();
        int i = 0;
        while (i < data.Length)
        {
            byte c = data[i];
            if (c == '~') break; // ~> 结束
            if (c == 'z') { ms.Write(new byte[4]); i++; continue; }
            if (c is >= (byte)'!' and <= (byte)'u')
            {
                // 收集最多 5 个字符
                int count = 0;
                ulong val = 0;
                while (count < 5 && i < data.Length && data[i] is >= (byte)'!' and <= (byte)'u')
                {
                    val = val * 85 + (ulong)(data[i] - '!');
                    count++; i++;
                }
                if (count == 5)
                {
                    ms.WriteByte((byte)(val >> 24));
                    ms.WriteByte((byte)(val >> 16));
                    ms.WriteByte((byte)(val >> 8));
                    ms.WriteByte((byte)val);
                }
                else if (count > 0)
                {
                    // 不足 5 个，补 'u'(84) 再解码，输出 count-1 字节
                    for (int k = count; k < 5; k++) val = val * 85 + 84;
                    var tmp = new byte[4];
                    for (int k = 3; k >= 0; k--) { tmp[k] = (byte)(val & 0xFF); val >>= 8; }
                    for (int k = 0; k < count - 1; k++) ms.WriteByte(tmp[k]);
                }
                continue;
            }
            i++; // 空白或非法字符
        }
        return ms.ToArray();
    }

    // ═══════════════════════════════════════════════════════════
    //  页面树
    // ═══════════════════════════════════════════════════════════

    private void CollectPages(long nodeRef) => CollectPages(nodeRef, 0);

    private void CollectPages(long nodeRef, int depth)
    {
        // 深度上限 + 已访问集合：防循环 Kids / 超深页面树导致的栈溢出
        if (depth > 64 || !_pagesVisited.Add(nodeRef)) return;
        var node = Resolve(GetObject(nodeRef)) as PdfDict;
        if (node == null) return;
        var type = (node.Get("Type") as PdfName)?.Name;
        if (type == "Page") { _pageRefs.Add(nodeRef); return; }
        if (type == "Pages" && node.Get("Kids") is PdfArray kids)
        {
            foreach (var kid in kids.Items)
                if (kid is PdfRef kr) CollectPages(kr.Num, depth + 1);
        }
    }

    private List<byte[]> CollectContentStreams(PdfObj? contents)
    {
        var result = new List<byte[]>();
        var resolved = Resolve(contents);
        if (resolved is PdfStream st)
            result.Add(DecodeStream(st.Dict, st.Data));
        else if (resolved is PdfArray arr)
        {
            foreach (var item in arr.Items)
            {
                if (Resolve(item) is PdfStream st2)
                    result.Add(DecodeStream(st2.Dict, st2.Data));
            }
        }
        return result;
    }

    // ═══════════════════════════════════════════════════════════
    //  内容流文本提取
    // ═══════════════════════════════════════════════════════════

    private void ExtractTextFromStream(byte[] data, PdfDict? fonts, StringBuilder sb)
    {
        int pos = 0;
        var operands = new List<PdfObj>();
        string? currentFont = null;
        double lastY = double.NaN;

        while (pos < data.Length)
        {
            SkipStreamWs(data, ref pos);
            if (pos >= data.Length) break;
            var obj = ParseStreamValue(data, ref pos);
            if (obj == null) { pos++; continue; }
            if (obj is PdfNull) continue; // 跳过字典占位

            if (obj is PdfKeyword kw)
            {
                HandleTextOperator(kw.Word, operands, ref currentFont, fonts, sb, ref lastY);
                operands.Clear();
            }
            else operands.Add(obj);
        }
    }

    // ── 内容流独立解析（静态，操作流字节数组，与文件结构解析解耦）──

    private static void SkipStreamWs(byte[] d, ref int pos)
    {
        while (pos < d.Length)
        {
            byte c = d[pos];
            if (c == '%') { while (pos < d.Length && d[pos] != '\n' && d[pos] != '\r') pos++; }
            else if (IsWs(c)) pos++;
            else break;
        }
    }

    private static PdfObj? ParseStreamValue(byte[] d, ref int pos, int depth = 0)
    {
        // 嵌套数组深度护栏：[[[[[ 恶意嵌套会在此处递归 ParseStreamArray↔ParseStreamValue，
        // 无上限时触发 StackOverflowException（不可捕获，直接杀进程）。
        if (depth >= 128) return null;
        if (pos >= d.Length) return null;
        byte c = d[pos];
        if (c == '<' && pos + 1 < d.Length && d[pos + 1] == '<')
        {
            pos += 2;
            while (pos + 1 < d.Length)
            {
                if (d[pos] == '>' && d[pos + 1] == '>') { pos += 2; break; }
                pos++;
            }
            return PdfNull.Instance; // 字典（罕见，如 BDC）跳过
        }
        if (c == '<') return ParseStreamHex(d, ref pos);
        if (c == '[') return ParseStreamArray(d, ref pos, depth + 1);
        if (c == '(') return ParseStreamLiteral(d, ref pos);
        if (c == '/') return ParseStreamName(d, ref pos);
        if (IsNumberStart(c)) return ParseStreamNumber(d, ref pos);
        if (IsRegular(c)) return ParseStreamKeyword(d, ref pos);
        pos++;
        return null;
    }

    private static PdfName ParseStreamName(byte[] d, ref int pos)
    {
        pos++;
        var sb = new StringBuilder();
        while (pos < d.Length && IsRegular(d[pos]))
        {
            if (d[pos] == '#' && pos + 2 < d.Length)
            {
                int v = HexVal(d[pos + 1]) * 16 + HexVal(d[pos + 2]);
                if (v >= 0) { sb.Append((char)v); pos += 3; continue; }
            }
            sb.Append((char)d[pos]);
            pos++;
        }
        return new PdfName { Name = sb.ToString() };
    }

    private static PdfStr ParseStreamLiteral(byte[] d, ref int pos)
    {
        pos++;
        var ms = new MemoryStream();
        int depth = 1;
        while (pos < d.Length && depth > 0)
        {
            byte c = d[pos++];
            if (c == '\\' && pos < d.Length)
            {
                byte e = d[pos++];
                switch (e)
                {
                    case (byte)'n': ms.WriteByte((byte)'\n'); break;
                    case (byte)'r': ms.WriteByte((byte)'\r'); break;
                    case (byte)'t': ms.WriteByte((byte)'\t'); break;
                    case (byte)'(': ms.WriteByte((byte)'('); break;
                    case (byte)')': ms.WriteByte((byte)')'); break;
                    case (byte)'\\': ms.WriteByte((byte)'\\'); break;
                    default:
                        if (e >= '0' && e <= '7')
                        {
                            int val = e - '0', n = 1;
                            while (n < 3 && pos < d.Length && d[pos] >= '0' && d[pos] <= '7')
                            { val = val * 8 + (d[pos++] - '0'); n++; }
                            ms.WriteByte((byte)(val & 0xFF));
                        }
                        else ms.WriteByte(e);
                        break;
                }
            }
            else if (c == '(') { depth++; ms.WriteByte(c); }
            else if (c == ')') { depth--; if (depth > 0) ms.WriteByte(c); }
            else ms.WriteByte(c);
        }
        return new PdfStr { Bytes = ms.ToArray() };
    }

    private static PdfStr ParseStreamHex(byte[] d, ref int pos)
    {
        pos++;
        var ms = new MemoryStream();
        int hi = -1;
        while (pos < d.Length)
        {
            byte c = d[pos];
            if (c == '>') { pos++; break; }
            int v = HexVal(c);
            if (v >= 0)
            {
                if (hi < 0) hi = v;
                else { ms.WriteByte((byte)((hi << 4) | v)); hi = -1; }
            }
            pos++;
        }
        if (hi >= 0) ms.WriteByte((byte)(hi << 4));
        return new PdfStr { Bytes = ms.ToArray() };
    }

    private static PdfArray ParseStreamArray(byte[] d, ref int pos, int depth)
    {
        pos++;
        var arr = new PdfArray();
        while (true)
        {
            SkipStreamWs(d, ref pos);
            if (pos < d.Length && d[pos] == ']') { pos++; break; }
            if (pos >= d.Length) break;
            var val = ParseStreamValue(d, ref pos, depth);
            if (val == null) pos++;
            else if (val is not PdfNull) arr.Items.Add(val);
        }
        return arr;
    }

    private static PdfNum ParseStreamNumber(byte[] d, ref int pos)
    {
        int start = pos;
        if (pos < d.Length && (d[pos] == '+' || d[pos] == '-')) pos++;
        while (pos < d.Length && d[pos] >= '0' && d[pos] <= '9') pos++;
        if (pos < d.Length && d[pos] == '.')
        {
            pos++;
            while (pos < d.Length && d[pos] >= '0' && d[pos] <= '9') pos++;
        }
        return new PdfNum { Value = ParseDouble(Encoding.ASCII.GetString(d, start, pos - start)) };
    }

    private static PdfKeyword ParseStreamKeyword(byte[] d, ref int pos)
    {
        int start = pos;
        while (pos < d.Length && IsRegular(d[pos])) pos++;
        return new PdfKeyword { Word = Encoding.ASCII.GetString(d, start, pos - start) };
    }

    private void HandleTextOperator(string op, List<PdfObj> ops, ref string? font,
        PdfDict? fonts, StringBuilder sb, ref double lastY)
    {
        switch (op)
        {
            case "Tf": // font size
                if (ops.Count >= 1 && ops[0] is PdfName fn) font = fn.Name;
                break;
            case "Tj":
                if (ops.Count >= 1 && ops[0] is PdfStr s) sb.Append(DecodeText(s.Bytes, font, fonts));
                break;
            case "TJ":
                if (ops.Count >= 1 && ops[0] is PdfArray arr) HandleTJ(arr, font, fonts, sb);
                break;
            case "'":
                if (ops.Count >= 1 && ops[0] is PdfStr s2) { sb.Append('\n'); sb.Append(DecodeText(s2.Bytes, font, fonts)); }
                break;
            case "\"":
                if (ops.Count >= 3 && ops[2] is PdfStr s3) { sb.Append('\n'); sb.Append(DecodeText(s3.Bytes, font, fonts)); }
                break;
            case "Td" or "TD":
                if (ops.Count >= 2 && ops[1] is PdfNum ty)
                {
                    if (ty.Value < 0 && !double.IsNaN(lastY)) sb.Append('\n');
                    lastY = double.IsNaN(lastY) ? ty.Value : lastY + ty.Value;
                }
                break;
            case "T*":
                if (!double.IsNaN(lastY)) sb.Append('\n');
                break;
            case "Tm":
                if (ops.Count >= 6 && ops[5] is PdfNum f)
                {
                    if (!double.IsNaN(lastY) && f.Value < lastY - 0.5) sb.Append('\n');
                    lastY = f.Value;
                }
                break;
        }
    }

    private void HandleTJ(PdfArray arr, string? font, PdfDict? fonts, StringBuilder sb)
    {
        foreach (var item in arr.Items)
        {
            if (item is PdfStr s) sb.Append(DecodeText(s.Bytes, font, fonts));
            else if (item is PdfNum n && n.Value < -100) sb.Append(' '); // 大负 gap = 词间空格
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  字体编码解码
    // ═══════════════════════════════════════════════════════════

    private string DecodeText(byte[] bytes, string? fontName, PdfDict? fonts)
    {
        if (bytes.Length == 0) return "";
        // UTF-16BE BOM
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return DecodeUtf16Be(bytes, 2);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) // UTF-16LE（少见）
            return DecodeUtf16Le(bytes, 2);

        var fontDict = ResolveFont(fontName, fonts);

        // ToUnicode CMap 优先级最高
        if (Resolve(fontDict?.Get("ToUnicode")) is PdfStream cmapStream)
        {
            var cmap = ParseCMap(DecodeStream(cmapStream.Dict, cmapStream.Data));
            var mapped = MapWithCMap(bytes, cmap);
            if (mapped != null) return mapped;
        }

        var subtype = (fontDict?.Get("Subtype") as PdfName)?.Name;
        if (subtype == "Type0")
        {
            // 复合字体：Identity-H/V → UTF-16BE；否则按 2 字节尝试
            var encName = (Resolve(fontDict?.Get("Encoding")) as PdfName)?.Name;
            if (encName is "Identity-H" or "Identity-V") return DecodeUtf16Be(bytes, 0);
            if (bytes.Length % 2 == 0 && bytes.Length >= 2) return DecodeUtf16Be(bytes, 0);
            return Latin1(bytes);
        }

        // 简单字体：按 /Encoding
        return DecodeSimpleFont(bytes, fontDict?.Get("Encoding"));
    }

    private string DecodeSimpleFont(byte[] bytes, PdfObj? encoding)
    {
        var enc = Resolve(encoding);
        var encName = (enc as PdfName)?.Name;
        var diff = enc as PdfDict;
        var sb = new StringBuilder(bytes.Length);
        foreach (var b in bytes) sb.Append(DecodeChar(b, encName, diff));
        return sb.ToString();
    }

    private static char DecodeChar(byte b, string? encName, PdfDict? diff)
    {
        // /Differences 覆盖
        if (diff?.Get("Differences") is PdfArray diffs)
        {
            int code = -1;
            foreach (var item in diffs.Items)
            {
                if (item is PdfNum n) code = (int)n.Value;
                else if (item is PdfName nm && code >= 0)
                {
                    if (code == b) return GlyphToChar(nm.Name, b);
                    code++;
                }
            }
        }
        return encName switch
        {
            "WinAnsiEncoding" => Cp1252(b),
            _ => (char)b, // Latin-1 近似（MacRoman/PDFDoc/Standard 均以此近似）
        };
    }

    private static char GlyphToChar(string glyphName, byte fallback)
    {
        // 精简 glyph 名 → Unicode（覆盖 /Differences 常见情况；完整 AGL 过长，其余回退 Latin-1）
        if (glyphName.Length == 1) return glyphName[0];
        return glyphName switch
        {
            "space" => ' ',
            "comma" => ',',
            "period" => '.',
            "hyphen" or "minus" => '-',
            "colon" => ':',
            "semicolon" => ';',
            "slash" => '/',
            "underscore" => '_',
            "parenleft" => '(',
            "parenright" => ')',
            _ => (char)fallback,
        };
    }

    private PdfDict? ResolveFont(string? fontName, PdfDict? fonts)
    {
        if (fontName == null || fonts == null) return null;
        return Resolve(fonts.Get(fontName)) as PdfDict;
    }

    private static string? MapWithCMap(byte[] bytes, PdfCMap cmap)
    {
        var sb = new StringBuilder();
        int codeLen = cmap.CodeSpaceLen;
        bool any = false;
        for (int i = 0; i < bytes.Length;)
        {
            int len = Math.Min(codeLen, bytes.Length - i);
            int code = 0;
            for (int j = 0; j < len; j++) code = (code << 8) | bytes[i + j];

            if (cmap.CharMap.TryGetValue(code, out var ch)) { sb.Append(ch); any = true; i += len; continue; }

            bool found = false;
            foreach (var (lo, hi, dst) in cmap.RangeMap)
            {
                if (code >= lo && code <= hi)
                {
                    int uni = (dst.Length > 0 ? Char.ConvertToUtf32(dst, 0) : 0) + (code - lo);
                    sb.Append(char.ConvertFromUtf32(uni));
                    found = true; any = true;
                    break;
                }
            }
            if (found) i += len;
            else i += Math.Max(1, len);
        }
        return any ? sb.ToString() : null;
    }

    private static PdfCMap ParseCMap(byte[] data)
    {
        var cmap = new PdfCMap();
        var text = Encoding.ASCII.GetString(data);

        int csStart = text.IndexOf("begincodespacerange");
        int csEnd = text.IndexOf("endcodespacerange", csStart >= 0 ? csStart : 0);
        if (csStart >= 0 && csEnd > csStart)
        {
            var hexes = ExtractHexStrings(text.Substring(csStart, csEnd - csStart));
            if (hexes.Count > 0) cmap.CodeSpaceLen = hexes[0].Length / 2;
        }

        ParseBfchar(text, cmap);
        ParseBfrange(text, cmap);
        return cmap;
    }

    private static void ParseBfchar(string text, PdfCMap cmap)
    {
        int start = text.IndexOf("beginbfchar");
        int end = start >= 0 ? text.IndexOf("endbfchar", start) : -1;
        if (start < 0 || end < 0) return;
        var hexes = ExtractHexStrings(text.Substring(start, end - start));
        for (int i = 0; i + 1 < hexes.Count; i += 2)
        {
            int code = HexToInt(hexes[i]);
            cmap.CharMap[code] = HexToUtf16(hexes[i + 1]);
        }
    }

    private static void ParseBfrange(string text, PdfCMap cmap)
    {
        int start = text.IndexOf("beginbfrange");
        int end = start >= 0 ? text.IndexOf("endbfrange", start) : -1;
        if (start < 0 || end < 0) return;
        var hexes = ExtractHexStrings(text.Substring(start, end - start));
        // 形式：<lo> <hi> <dst> （三连）——[<d1> <d2>...] 数组形式 MVP 不解析
        for (int i = 0; i + 2 < hexes.Count; i += 3)
        {
            int lo = HexToInt(hexes[i]);
            int hi = HexToInt(hexes[i + 1]);
            cmap.RangeMap.Add((lo, hi, HexToUtf16(hexes[i + 2])));
        }
    }

    private static List<string> ExtractHexStrings(string s)
    {
        var list = new List<string>();
        int i = 0;
        while (i < s.Length)
        {
            if (s[i] == '<')
            {
                int j = s.IndexOf('>', i);
                if (j < 0) break;
                list.Add(s.Substring(i + 1, j - i - 1));
                i = j + 1;
            }
            else i++;
        }
        return list;
    }

    private static int HexToInt(string hex)
    {
        int v = 0;
        foreach (var c in hex)
        {
            int d = HexVal((byte)c);
            if (d < 0) return v;
            v = (v << 4) | d;
        }
        return v;
    }

    private static string HexToUtf16(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i + 1 < hex.Length; i += 2)
            bytes[i / 2] = (byte)((HexVal((byte)hex[i]) << 4) | HexVal((byte)hex[i + 1]));
        return DecodeUtf16Be(bytes, 0);
    }

    private static string DecodeUtf16Be(byte[] bytes, int offset)
    {
        var sb = new StringBuilder();
        for (int i = offset; i + 1 < bytes.Length; i += 2)
        {
            int u = (bytes[i] << 8) | bytes[i + 1];
            if (u >= 0xD800 && u <= 0xDBFF && i + 3 < bytes.Length)
            {
                int lo = (bytes[i + 2] << 8) | bytes[i + 3];
                if (lo >= 0xDC00 && lo <= 0xDFFF)
                {
                    sb.Append(char.ConvertFromUtf32(0x10000 + ((u - 0xD800) << 10) + (lo - 0xDC00)));
                    i += 2;
                    continue;
                }
            }
            sb.Append((char)u);
        }
        return sb.ToString();
    }

    private static string DecodeUtf16Le(byte[] bytes, int offset)
    {
        var sb = new StringBuilder();
        for (int i = offset; i + 1 < bytes.Length; i += 2)
            sb.Append((char)((bytes[i + 1] << 8) | bytes[i]));
        return sb.ToString();
    }

    private static string Latin1(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length);
        foreach (var b in bytes) sb.Append((char)b);
        return sb.ToString();
    }

    private static string DecodeInfoString(byte[] bytes)
    {
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) return DecodeUtf16Be(bytes, 2);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) return DecodeUtf16Le(bytes, 2);
        return Latin1(bytes);
    }

    private static char Cp1252(byte b)
    {
        if (b < 0x80) return (char)b;
        if (b < 0xA0) return Cp1252High[b - 0x80];
        return (char)b;
    }

    private static readonly char[] Cp1252High =
    [
        '€', '', '‚', 'ƒ', '„', '…', '†', '‡',
        'ˆ', '‰', 'Š', '‹', 'Œ', '', 'Ž', '',
        '', '‘', '’', '“', '”', '•', '–', '—',
        '˜', '™', 'š', '›', 'œ', '', 'ž', 'Ÿ',
    ];

    // ═══════════════════════════════════════════════════════════
    //  低级 helper
    // ═══════════════════════════════════════════════════════════

    private static bool IsWs(byte b) => b == ' ' || b == '\t' || b == '\r' || b == '\n' || b == '\f' || b == 0;
    private static bool IsDelim(byte b) => IsWs(b) || b is (byte)'(' or (byte)')' or (byte)'<' or (byte)'>' or (byte)'[' or (byte)']' or (byte)'{' or (byte)'}' or (byte)'/' or (byte)'%';
    private static bool IsRegular(byte b) => !IsDelim(b);
    private static bool IsNumberStart(byte b) => b is (byte)'+' or (byte)'-' or (byte)'.' || (b >= '0' && b <= '9');

    private static int HexVal(byte b) =>
        b >= '0' && b <= '9' ? b - '0' :
        b >= 'a' && b <= 'f' ? b - 'a' + 10 :
        b >= 'A' && b <= 'F' ? b - 'A' + 10 : -1;

    private void SkipWs(ref int pos)
    {
        while (pos < _buf.Length)
        {
            byte c = _buf[pos];
            if (c == '%') { while (pos < _buf.Length && _buf[pos] != '\n' && _buf[pos] != '\r') pos++; }
            else if (IsWs(c)) pos++;
            else break;
        }
    }

    private bool MatchWord(ref int pos, string word) => MatchWordAt(pos, word) ? (pos += word.Length) == pos : false;

    private bool MatchWordAt(int pos, string word)
    {
        if (pos + word.Length > _buf.Length) return false;
        for (int i = 0; i < word.Length; i++)
            if (_buf[pos + i] != (byte)word[i]) return false;
        int after = pos + word.Length;
        if (after < _buf.Length && IsRegular(_buf[after])) return false;
        return true;
    }

    private void ReadNumber(ref int pos)
    {
        if (pos < _buf.Length && (_buf[pos] == '+' || _buf[pos] == '-')) pos++;
        while (pos < _buf.Length && _buf[pos] >= '0' && _buf[pos] <= '9') pos++;
        if (pos < _buf.Length && _buf[pos] == '.')
        {
            pos++;
            while (pos < _buf.Length && _buf[pos] >= '0' && _buf[pos] <= '9') pos++;
        }
    }

    private string Ascii(int start, int end) => Encoding.ASCII.GetString(_buf, start, end - start);

    private static double ParseDouble(string s) => double.Parse(s, CultureInfo.InvariantCulture);

    private long GetLong(PdfDict d, string key)
    {
        var v = Resolve(d.Get(key));
        return v is PdfNum n ? (long)n.Value : -1;
    }

    private static long ReadBigEndian(byte[] d, int p, int w)
    {
        if (w <= 0 || p < 0 || p + w > d.Length) return 0;
        long v = 0;
        for (int i = 0; i < w; i++) v = (v << 8) | d[p + i];
        return v;
    }
}
