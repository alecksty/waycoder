using System.Text;

namespace WayCoder.Infra;

/// <summary>
/// 可编辑的行集合 —— 小文本文件走这条（超过只读阈值的大文件走 <see cref="IndexedTextSource"/>）。
///
/// 实现 <see cref="ITextSource"/>，所以**渲染层对小文件和大文件是同一套代码**，
/// 差别只在「能不能改」由页面按字节阈值决定，而不是两套渲染路径。
/// </summary>
public sealed class EditableLines : ITextSource
{
    private readonly List<string> _lines;

    public long LineCount => _lines.Count;
    public string EncodingName { get; }
    public Encoding Encoding { get; }
    public bool UsesCrlf { get; }
    public long ByteLength { get; }

    public EditableLines(string text, string encodingName, bool usesCrlf, long byteLength,
        Encoding? encoding = null)
    {
        _lines = [.. MemoryTextSource.SplitLines(text)];
        EncodingName = encodingName;
        Encoding = encoding ?? new UTF8Encoding(false);
        UsesCrlf = usesCrlf;
        ByteLength = byteLength;
    }

    public string? GetLine(long index)
        => index >= 0 && index < _lines.Count ? _lines[(int)index] : null;

    public long LineBytes(long index) => GetLine(index)?.Length ?? 0;

    public Task<int> PrefetchAsync(long fromLine, long toLine, CancellationToken ct = default)
        => Task.FromResult(0);   // 全在内存里，无需预取

    public string ReadAll() => string.Join(UsesCrlf ? "\r\n" : "\n", _lines);

    // ── 编辑（唯一的写入入口）──

    /// <summary>
    /// 把第 <paramref name="start"/> 行起的 <paramref name="removeCount"/> 行替换成
    /// <paramref name="newLines"/>。撤销历史由调用方经 <see cref="EditHistory"/> 维护 ——
    /// 本类只管「当前是什么」，不管「之前是什么」。
    /// </summary>
    public void ReplaceRange(long start, int removeCount, IReadOnlyList<string> newLines)
    {
        int s = (int)Math.Clamp(start, 0, _lines.Count);
        int rm = Math.Clamp(removeCount, 0, _lines.Count - s);
        if (rm > 0) _lines.RemoveRange(s, rm);
        if (newLines.Count > 0) _lines.InsertRange(s, newLines);
    }

    /// <summary>取一段行的副本（保存 / 复制 / 大纲分析用）。</summary>
    public string[] Snapshot(long start, int count)
    {
        int s = (int)Math.Clamp(start, 0, _lines.Count);
        int n = Math.Clamp(count, 0, _lines.Count - s);
        var result = new string[n];
        for (int i = 0; i < n; i++) result[i] = _lines[s + i];
        return result;
    }

    public void Dispose() { }
}
