using System.Text;

namespace WayCoder.Infra;

/// <summary>
/// 文本行来源 —— 编辑器渲染层只认这个抽象，不关心打开的是 1KB 的配置还是 100MB 的日志。
///
/// **行号与字节偏移全程 <c>long</c>**：100MB 的「1 字节行」是 1 亿行，int 装不下；
/// 而 100MB / 40B 的常规代码是 250 万行，<c>long[]</c> 索引约 20MB，可接受。
///
/// **取行不做 IO**（<see cref="GetLine"/> 只读缓存）：渲染在 Draw 里跑，一旦在这里阻塞读盘，
/// 滚动就会一顿一顿的。没命中返回 null，调用方画占位、另起 <see cref="PrefetchAsync"/> 补。
/// </summary>
public interface ITextSource : IDisposable
{
    /// <summary>总行数（索引建完后确定）。</summary>
    long LineCount { get; }

    /// <summary>编码显示名（状态栏用）。</summary>
    string EncodingName { get; }

    /// <summary>写回用的编码 —— 保存必须按原编码写，别把 GB18030 文件悄悄转成 UTF-8。</summary>
    Encoding Encoding { get; }

    /// <summary>是否 CRLF 行尾（保存时按原样还原）。</summary>
    bool UsesCrlf { get; }

    /// <summary>文件字节数。</summary>
    long ByteLength { get; }

    /// <summary>取一行。**只读缓存、不做 IO**；null = 未加载（调用方画占位并预取）。</summary>
    string? GetLine(long index);

    /// <summary>该行的原始字节数（用于判断超长行、显示截断提示）。</summary>
    long LineBytes(long index);

    /// <summary>批量预取到缓存（后台线程读盘）。返回本次新加载的行数。</summary>
    Task<int> PrefetchAsync(long fromLine, long toLine, CancellationToken ct = default);

    /// <summary>整份文本（仅内存型来源支持；索引型大文件抛 <see cref="NotSupportedException"/>）。</summary>
    string ReadAll();
}

/// <summary>
/// 内存行表 —— 用于小文件，以及 UTF-16/32 这类**换行不是单字节**的编码
/// （大文件走 <see cref="IndexedTextSource"/>，那条路按 0x0A 扫字节，对 UTF-16 不成立）。
/// </summary>
public sealed class MemoryTextSource : ITextSource
{
    private readonly string[] _lines;

    public long LineCount => _lines.Length;
    public string EncodingName { get; }
    public Encoding Encoding { get; }
    public bool UsesCrlf { get; }
    public long ByteLength { get; }

    public MemoryTextSource(string text, string encodingName, bool usesCrlf, long byteLength,
        Encoding? encoding = null)
    {
        EncodingName = encodingName;
        Encoding = encoding ?? new UTF8Encoding(false);
        UsesCrlf = usesCrlf;
        ByteLength = byteLength;
        _lines = SplitLines(text);
    }

    /// <summary>按 \n 切分并去掉行尾 \r —— 与索引型来源的行语义保持一致。</summary>
    internal static string[] SplitLines(string text)
    {
        var parts = text.Split('\n');
        for (int i = 0; i < parts.Length; i++)
            if (parts[i].Length > 0 && parts[i][^1] == '\r')
                parts[i] = parts[i][..^1];
        return parts;
    }

    public string? GetLine(long index)
        => index >= 0 && index < _lines.Length ? _lines[index] : null;

    public long LineBytes(long index) => GetLine(index)?.Length ?? 0;

    public Task<int> PrefetchAsync(long fromLine, long toLine, CancellationToken ct = default)
        => Task.FromResult(0); // 全在内存里，无需预取

    public string ReadAll() => string.Join(UsesCrlf ? "\r\n" : "\n", _lines);

    public void Dispose() { }
}

/// <summary>
/// 大文件行来源 —— 字节级行索引 + 按行解码 + LRU 行缓存，**永不把整份读进内存**。
///
/// **为什么按 <c>0x0A</c> 扫字节是安全的**：UTF-8 的多字节序列、GB18030（含 GBK/GB2312）的
/// 双字节与四字节序列、Big5 / Shift-JIS / EUC-KR 的续字节范围**都不含 0x0A**。所以每个 0x0A
/// 一定是行分隔符，且每行的字节区间本身就是一个完整的编码序列 ⇒ **按行解码永远不需要
/// 跨块状态机**。这条性质由自测钉住（否则整个数据层都建立在猜测上）。
///
/// **索引一次性建完**（在后台线程，100MB 顺序扫在百毫秒级）：行数不确定的「渐进索引」会让
/// 滚动条、行号栏、跳行全部要处理「未知」态，复杂度远超收益。
/// </summary>
public sealed class IndexedTextSource : ITextSource
{
    /// <summary>每块行数。65536 行 ≈ 512KB 的 long[]；100MB 常规文件约 40 块。</summary>
    private const int ChunkLines = 1 << 16;

    /// <summary>块表容量：512 × 65536 ≈ 3355 万行，够 <see cref="MaxIndexedLines"/> 用且无需扩容
    /// （扩容时 <c>Array.Resize</c> 会替换数组引用，读线程可能看到复制到一半的数组）。</summary>
    private const int MaxChunks = 512;

    /// <summary>行缓存字节上限（按 UTF-16 粗估：每字符 2 字节）。</summary>
    private const long CacheByteBudget = 8L * 1024 * 1024;

    /// <summary>超过这个长度的行不进缓存 —— 一条巨型 minified 行会把整个缓存冲掉。</summary>
    private const int MaxCachedLineChars = 4096;

    /// <summary>单行最多读取的字节数。再长就截断（见 <see cref="IsLineTruncated"/>）：
    /// 一条 100MB 的行若整条读出来，光 <c>byte[]</c> 就是 100MB，前功尽弃。</summary>
    public const int MaxLineReadBytes = 4 << 20;

    /// <summary>行数上限：再往上 <c>long[]</c> 索引自身就会吃掉几百 MB（100MB 的 1 字节行 = 1 亿行 ≈ 800MB）。</summary>
    public const long MaxIndexedLines = 20_000_000;

    private readonly FileStream _stream;
    private readonly Encoding _encoding;
    private readonly int _dataStart;
    private readonly long _fileLength;

    /// <summary>块表（固定大小，只写元素不换引用 —— 见 <see cref="MaxChunks"/> 的说明）。</summary>
    private readonly long[]?[] _chunks = new long[]?[MaxChunks];

    private long _lineCount;
    private bool _tooManyLines;

    /// <summary>行缓存与其 LRU 顺序。预取在后台线程写、GetLine 在 UI 线程读 ⇒ 全部走 <see cref="_cacheLock"/>。</summary>
    private readonly object _cacheLock = new();
    private readonly Dictionary<long, string> _cache = new();
    private readonly LinkedList<long> _cacheOrder = new();
    private readonly Dictionary<long, LinkedListNode<long>> _cacheNodes = new();
    private long _cacheBytes;

    /// <summary>超长行的单条专用槽位（见 <see cref="CachePutLocked"/>）：不进 LRU，但保证同一行能反复命中。</summary>
    private long _bigLineIndex = -1;
    private string? _bigLine;

    public string EncodingName { get; }
    public Encoding Encoding => _encoding;
    public bool UsesCrlf { get; private set; }
    public long ByteLength => _fileLength;
    public long LineCount => _lineCount;

    /// <summary>索引时是否因行数超限而中止（调用方据此拒绝打开）。</summary>
    public bool TooManyLines => _tooManyLines;

    internal IndexedTextSource(FileStream stream, Encoding encoding, int bomLength,
        string encodingName, long fileLength)
    {
        _stream = stream;
        _encoding = encoding;
        _dataStart = bomLength;
        _fileLength = fileLength;
        EncodingName = encodingName;
    }

    // ── 索引（后台线程调用一次）──

    internal void BuildIndex(CancellationToken ct)
    {
        var buffer = new byte[1 << 20];   // 1MB 顺序读：ReadOnlySpan.IndexOf 走 SIMD
        long pos = _dataStart;
        _stream.Position = pos;

        var chunk = NewChunk();
        chunk[0] = _dataStart;            // 第 0 行 = 内容起点（NewChunk 填的是 -1，必须显式写回）
        int fill = 1;
        int chunkIdx = 0;
        long lines = 0;

        int read;
        while ((read = _stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            ct.ThrowIfCancellationRequested();
            var span = buffer.AsSpan(0, read);
            int search = 0;
            while (true)
            {
                int rel = span[search..].IndexOf((byte)'\n');
                if (rel < 0) break;
                int abs = search + rel;

                // 行尾风格只看第一处换行（混合行尾按首行算，与主流编辑器一致）
                if (!_crlfChecked && abs > 0)
                {
                    UsesCrlf = span[abs - 1] == (byte)'\r';
                    _crlfChecked = true;
                }

                if (fill == ChunkLines)
                {
                    _chunks[chunkIdx++] = chunk;
                    lines += fill;
                    if (lines > MaxIndexedLines) { _tooManyLines = true; _lineCount = lines; return; }
                    chunk = NewChunk();
                    fill = 0;
                }
                chunk[fill++] = pos + abs + 1;   // 下一行的起点
                search = abs + 1;
            }
            pos += read;
        }

        _chunks[chunkIdx] = chunk;
        _lineCount = lines + fill;
    }

    private bool _crlfChecked;

    /// <summary>新块：全部填 -1（0 是合法的行起点 —— 无 BOM 文件的第 0 行就从字节 0 开始）。</summary>
    private static long[] NewChunk()
    {
        var c = new long[ChunkLines];
        Array.Fill(c, -1L);
        return c;
    }

    // ── 行定位 ──

    private long LineStart(long line)
    {
        if (line < 0 || line >= _lineCount) return -1;
        var chunk = _chunks[(int)(line / ChunkLines)];
        if (chunk == null) return -1;
        return chunk[(int)(line % ChunkLines)];   // -1 = 无效
    }

    private long LineEnd(long line)
    {
        long next = LineStart(line + 1);
        return next >= 0 ? next : _fileLength;
    }

    public long LineBytes(long index)
    {
        long s = LineStart(index), e = LineEnd(index);
        if (s < 0) return 0;
        long n = e - s;
        return n > 0 ? n : 0;
    }

    /// <summary>该行是否因为超过 <see cref="MaxLineReadBytes"/> 而被截断显示。</summary>
    public bool IsLineTruncated(long index) => LineBytes(index) > MaxLineReadBytes;

    // ── 取行 ──

    public string? GetLine(long index)
    {
        lock (_cacheLock)
        {
            if (_bigLine is not null && _bigLineIndex == index) return _bigLine;
            if (_cache.TryGetValue(index, out var cached))
            {
                TouchLocked(index);
                return cached;
            }
        }
        return null;   // 未加载：交给 PrefetchAsync 补，绝不在渲染路径上读盘
    }

    /// <summary>真正读盘解码一行（后台线程用）。</summary>
    private string? ReadLineNow(long index)
    {
        long start = LineStart(index);
        if (start < 0) return null;
        long end = LineEnd(index);
        if (end <= start) return "";

        long want = end - start;
        bool truncated = want > MaxLineReadBytes;
        if (truncated) want = MaxLineReadBytes;

        var bytes = new byte[(int)want];
        _stream.Position = start;
        int got = 0;
        while (got < want)
        {
            int n = _stream.Read(bytes, got, (int)(want - got));
            if (n <= 0) break;
            got += n;
        }
        int len = got;

        // 未截断时才剥行尾换行 —— 截断时尾部是任意字节，剥了反而错
        if (!truncated)
        {
            if (len > 0 && bytes[len - 1] == (byte)'\n') len--;
            if (len > 0 && bytes[len - 1] == (byte)'\r') len--;
        }
        return _encoding.GetString(bytes, 0, len);
    }

    public async Task<int> PrefetchAsync(long fromLine, long toLine, CancellationToken ct = default)
    {
        if (fromLine < 0) fromLine = 0;
        long last = Math.Min(toLine, _lineCount - 1);
        if (last < fromLine) return 0;

        return await Task.Run(() =>
        {
            int loaded = 0;
            for (long i = fromLine; i <= last; i++)
            {
                ct.ThrowIfCancellationRequested();
                lock (_cacheLock)
                {
                    if (_bigLine is not null && _bigLineIndex == i) continue;
                    if (_cache.ContainsKey(i)) { TouchLocked(i); continue; }
                }
                var line = ReadLineNow(i);
                if (line == null) continue;
                lock (_cacheLock) { CachePutLocked(i, line); }
                loaded++;
            }
            return loaded;
        }, ct).ConfigureAwait(false);
    }

    private void TouchLocked(long index)
    {
        if (_cacheNodes.TryGetValue(index, out var node))
        {
            _cacheOrder.Remove(node);
            _cacheOrder.AddFirst(node);
        }
    }

    private void CachePutLocked(long index, string line)
    {
        // 超长行不进 LRU（一条 4MB 的行会把整个缓存预算吃光），但**也不丢弃** ——
        // 用单条专用槽位兜住它：渲染一帧内会对同一行反复取，读一次就该能命中，
        // 否则每条超长行都要每帧重读一遍盘。
        if (line.Length > MaxCachedLineChars)
        {
            _bigLineIndex = index;
            _bigLine = line;
            return;
        }
        if (_bigLineIndex == index) { _bigLineIndex = -1; _bigLine = null; }

        if (_cache.TryGetValue(index, out var old))
        {
            _cacheBytes -= (long)old.Length * 2;
            _cache.Remove(index);
            if (_cacheNodes.Remove(index, out var n)) _cacheOrder.Remove(n);
        }

        _cache[index] = line;
        _cacheNodes[index] = _cacheOrder.AddFirst(index);
        _cacheBytes += (long)line.Length * 2;

        while (_cacheBytes > CacheByteBudget && _cacheOrder.Last is { } tail)
        {
            _cacheOrder.RemoveLast();
            _cacheNodes.Remove(tail.Value);
            if (_cache.Remove(tail.Value, out var evicted)) _cacheBytes -= (long)evicted.Length * 2;
        }
    }

    public string ReadAll() => throw new NotSupportedException(
        "索引型大文件不提供全量读取 —— 正是这一点让它能打开 100MB 而不 OOM");

    public void Dispose()
    {
        lock (_cacheLock)
        {
            _cache.Clear();
            _cacheNodes.Clear();
            _cacheOrder.Clear();
        }
        _stream.Dispose();
    }
}

/// <summary>打开入口：按大小与编码决定用哪种行来源。</summary>
public static class TextSourceFactory
{
    /// <summary>编码探测的采样字节数（只看头部，不读全文）。</summary>
    private const int ProbeBytes = 4096;

    /// <summary>UTF-16/32 大文件走内存行表的体积上限。</summary>
    public const long MemoryModeMaxBytes = 4L * 1024 * 1024;

    /// <summary>
    /// 打开一个文本文件。<c>Source</c> 为 null = 打不开，<c>Reason</c> 给出原因（供 UI 显示）。
    /// （不能用 <c>out</c> 参数：本方法要 await 后台建索引。）
    /// </summary>
    /// <param name="editableMaxBytes">小于等于这个字节数就走**可编辑**的内存行表
    /// （<see cref="EditableLines"/>）；超过则走只读的索引型来源。
    /// 编辑需要可写模型，而索引型的整个设计前提就是「不把文件装进内存」——两者不能兼得，
    /// 所以这条线同时也是「编辑按钮什么时候置灰」的线。</param>
    public static async Task<(ITextSource? Source, string Reason)> OpenAsync(
        string path, long editableMaxBytes = 2L * 1024 * 1024, CancellationToken ct = default)
    {
        if (!File.Exists(path)) return (null, "文件不存在");

        long length = new FileInfo(path).Length;
        if (length == 0) return (new MemoryTextSource("", "UTF-8", false, 0), "");

        var probe = ReadProbe(path, length);
        var (bomLen, bomName, bomEnc) = TextEncoding.MatchBom(probe);

        // ① UTF-16/32：换行是 2/4 字节，按 0x0A 扫字节的索引路径不成立。
        //    小文件走内存行表；大文件明确拒绝，而不是默默解成一堆 NUL 或乱码。
        if (bomEnc != null && bomEnc.CodePage is 12000 or 12001 or 1201 or 1200)
        {
            if (length > MemoryModeMaxBytes)
                return (null, $"{bomName} 编码的大文件暂不支持（{FormatSize(length)}）");
            var detected = TextEncoding.ReadFile(path);
            return (new MemoryTextSource(detected.Text, detected.EncodingName,
                detected.Text.Contains("\r\n"), length, detected.Encoding), "");
        }

        // ② 二进制：前 4KB 含 NUL 即判非文本（复用既有判据）
        if (TextEncoding.IsBinaryContent(probe))
            return (null, "这是二进制文件");

        // ③ 定编码：BOM 优先；无 BOM 时用**采样试解**（见 IsValidUtf8Prefix 的说明）
        Encoding encoding;
        string encodingName;
        if (bomEnc != null) { encoding = bomEnc; encodingName = bomName; }
        else if (IsValidUtf8Prefix(probe, bomLen)) { encoding = new UTF8Encoding(false); encodingName = "UTF-8"; }
        else { encoding = TextEncoding.GB18030; encodingName = "GB18030"; }

        // ④ 小文件：全量读成**可编辑**的内存行表（编辑需要可写模型，而索引型的整个前提
        //    就是「不装进内存」；小文件全量读的代价可以忽略）
        if (length <= editableMaxBytes && editableMaxBytes > 0)
        {
            var detected = TextEncoding.ReadFile(path);
            return (new EditableLines(detected.Text, detected.EncodingName,
                detected.Text.Contains("\r\n"), length, detected.Encoding), "");
        }

        // ⑤ 大文件：建索引型来源（索引在后台线程一次建完 —— 100MB 顺序扫在百毫秒级）
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite, bufferSize: 1 << 16, useAsync: false);
        var src = new IndexedTextSource(stream, encoding, bomLen, encodingName, length);
        try
        {
            await Task.Run(() => src.BuildIndex(ct), ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            src.Dispose();
            throw;
        }

        if (src.TooManyLines)
        {
            src.Dispose();
            return (null, $"行数超过 {IndexedTextSource.MaxIndexedLines / 1_000_000 * 100} 万行，无法建立索引");
        }
        return (src, "");
    }

    private static byte[] ReadProbe(string path, long length)
    {
        var probe = new byte[(int)Math.Min(ProbeBytes, length)];
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        int got = 0;
        while (got < probe.Length)
        {
            int n = fs.Read(probe, got, probe.Length - got);
            if (n <= 0) break;
            got += n;
        }
        if (got < probe.Length) Array.Resize(ref probe, got);
        return probe;
    }

    /// <summary>
    /// 采样是否是一段合法的 UTF-8 **前缀** —— 允许尾部半个多字节序列（采样在字符中间切断造成的），
    /// 但不允许中途出现真正的非法序列。`flush: false` 让 Decoder 保留未完成的序列而不是报错，
    /// 这正是「采样探测」与「全量试解」的区别：后者会把采样切断当成编码错误，误判成 GB18030。
    /// </summary>
    public static bool IsValidUtf8Prefix(ReadOnlySpan<byte> bytes, int start)
    {
        if (start >= bytes.Length) return true;
        var strict = new UTF8Encoding(false, throwOnInvalidBytes: true);
        var decoder = strict.GetDecoder();
        var input = bytes[start..].ToArray();
        var chars = new char[strict.GetMaxCharCount(input.Length)];
        try
        {
            decoder.Convert(input, 0, input.Length, chars, 0, chars.Length,
                flush: false, out _, out _, out _);
            return true;
        }
        catch (DecoderFallbackException) { return false; }
    }

    private static string FormatSize(long b) => b switch
    {
        >= 1 << 30 => $"{b / (double)(1 << 30):0.#}GB",
        >= 1 << 20 => $"{b / (double)(1 << 20):0.#}MB",
        >= 1 << 10 => $"{b / (double)(1 << 10):0.#}KB",
        _ => $"{b}B"
    };
}
