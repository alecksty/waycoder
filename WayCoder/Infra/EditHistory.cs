namespace WayCoder.Infra;

/// <summary>
/// 一次编辑 —— 把第 <see cref="Line"/> 行起的 <c>OldLines</c> 若干行替换成 <c>NewLines</c>。
///
/// 用「行区间替换」而不是「整份文本快照」：旧实现每敲一个键就往撤销栈压一份**完整文本**，
/// 200 层上限意味着最坏情况持有 200 份文件内容 —— 稍微大点的文件直接爆内存。
/// 行区间模型下，一次编辑的代价与文件大小无关。
/// </summary>
public readonly record struct EditOp(
    long Line,
    string[] OldLines,
    string[] NewLines,
    int CaretBefore,
    int CaretAfter,
    long AtMs);

/// <summary>
/// 行级撤销/重做历史（纯逻辑，可桌面自测）。
/// 连续的「打字 / 退格」会按时间窗合并成一步，否则撤销要一个字符一个字符地退。
/// </summary>
public sealed class EditHistory
{
    public const int DefaultMaxDepth = 500;

    /// <summary>相邻两次单字符编辑在这个时间窗内就合并成一步。</summary>
    public const long MergeWindowMs = 800;

    private readonly List<EditOp> _undo = [];
    private readonly List<EditOp> _redo = [];

    public int MaxDepth { get; init; } = DefaultMaxDepth;

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public int UndoDepth => _undo.Count;
    public int RedoDepth => _redo.Count;

    /// <summary>压入一步编辑（新编辑会清空重做分支）。</summary>
    public void Push(EditOp op)
    {
        if (_undo.Count > 0 && CanMerge(_undo[^1], op))
        {
            var last = _undo[^1];
            _undo[^1] = last with
            {
                NewLines = op.NewLines,
                CaretAfter = op.CaretAfter,
                AtMs = op.AtMs,
            };
            _redo.Clear();
            return;
        }

        _undo.Add(op);
        _redo.Clear();
        while (_undo.Count > MaxDepth) _undo.RemoveAt(0);
    }

    /// <summary>取一步可撤销的编辑（调用方据此把行区间换成 <see cref="EditOp.OldLines"/>）。</summary>
    public EditOp? Undo()
    {
        if (_undo.Count == 0) return null;
        var op = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(op);
        return op;
    }

    /// <summary>取一步可重做的编辑（调用方据此换成 <see cref="EditOp.NewLines"/>）。</summary>
    public EditOp? Redo()
    {
        if (_redo.Count == 0) return null;
        var op = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(op);
        return op;
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }

    /// <summary>
    /// 能否合并：同一行、时间窗内、且是**单行上的单字符增删**（打字或退格）。
    /// 换行、粘贴、跨行操作一律独立成步 —— 混在一起撤销时会连正确的内容一起退掉。
    /// </summary>
    internal static bool CanMerge(EditOp a, EditOp b)
    {
        if (b.AtMs - a.AtMs > MergeWindowMs) return false;
        if (a.Line != b.Line) return false;
        if (a.NewLines.Length != 1 || b.NewLines.Length != 1) return false;
        if (a.OldLines.Length != 1 || b.OldLines.Length != 1) return false;

        var prev = a.NewLines[0];
        var next = b.NewLines[0];
        if (next.Length == prev.Length + 1 && next.StartsWith(prev, StringComparison.Ordinal)) return true;  // 追加
        if (prev.Length == next.Length + 1 && prev.StartsWith(next, StringComparison.Ordinal)) return true;  // 删除
        return false;
    }
}
