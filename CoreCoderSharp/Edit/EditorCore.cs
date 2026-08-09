using System.Text;

namespace CoreCoderSharp;

/// <summary>
/// 编辑器纯数据模型 —— 从 Editor.cs 提取，无渲染、无键盘、无 TUI 依赖。
/// 管理文本缓冲区、光标、滚动、撤销、剪贴板、语法、诊断。
/// </summary>
public class EditorCore
{
    // ── 缓冲区 ──
    public List<StringBuilder> Lines { get; private set; } = [];
    public string FilePath { get; private set; } = "";
    public bool Modified { get; private set; }

    // ── 光标 (0-based) ──
    public int Cy { get; set; }
    public int Cx { get; set; }

    // ── 滚动偏移（可见区域第一行的行索引） ──
    public int Scroll { get; set; }

    // ── 语法 ──
    public Syntax Syntax { get; private set; } = Syntax.ForFile("untitled.txt");

    // ── 撤销 ──
    private readonly Stack<EditAction> _undo = new();
    private record EditAction(int Line, int Col, string OldLine, string NewLine, int OldCount);

    // ── 剪贴板 ──
    private static string _clipboard = "";

    // ── 事件 ──
    /// <summary>内容改变时触发（用于 UI 更新状态栏）</summary>
    public event Action? OnContentChanged;

    /// <summary>Lint 诊断完成后触发（用于 UI 刷新）</summary>
    public event Action? OnDiagnosticsReady;

    // ================================================================
    // 文件操作
    // ================================================================

    /// <summary>加载文件内容（不存在则创建空缓冲区）</summary>
    public void LoadFile(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
        Syntax = Syntax.ForFile(FilePath);
        Lines.Clear();

        if (File.Exists(FilePath))
        {
            foreach (var line in File.ReadAllLines(FilePath, Encoding.UTF8))
                Lines.Add(new StringBuilder(line));
        }

        if (Lines.Count == 0)
            Lines.Add(new StringBuilder());

        Cy = 0; Cx = 0; Scroll = 0; Modified = false;
        _undo.Clear();
    }

    /// <summary>同步保存（写文件）</summary>
    public void Save()
    {
        var dir = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var content = string.Join("\n", Lines.Select(sb => sb.ToString()));
        File.WriteAllText(FilePath, content, Encoding.UTF8);
        Modified = false;
    }

    /// <summary>异步保存 + 触发 Lint 诊断（fire-and-forget）</summary>
    public async Task SaveAsync()
    {
        Save();
        // 异步运行 lint 诊断
        await Task.Run(async () =>
        {
            await DiagnosticManager.RunLintAsync(FilePath);
            OnDiagnosticsReady?.Invoke();
        });
    }

    // ================================================================
    // 光标移动
    // ================================================================

    public void MoveCursor(int dx, int dy)
    {
        Cy = Math.Clamp(Cy + dy, 0, Lines.Count - 1);
        Cx = Math.Clamp(Cx + dx, 0, Lines[Cy].Length);
        Cx = Math.Min(Cx, Lines[Cy].Length);
    }

    public void MoveHome() => Cx = 0;
    public void MoveEnd() => Cx = Lines[Cy].Length;

    public void MovePageUp(int visibleLines)
    {
        Cy = Math.Max(0, Cy - visibleLines);
        Cx = 0;
    }

    public void MovePageDown(int visibleLines)
    {
        Cy = Math.Min(Lines.Count - 1, Cy + visibleLines);
        Cx = 0;
    }

    /// <summary>跳到指定行（1-based）</summary>
    public bool JumpToLine(int lineNumber)
    {
        if (lineNumber < 1 || lineNumber > Lines.Count) return false;
        Cy = lineNumber - 1;
        Cx = 0;
        return true;
    }

    // ================================================================
    // 编辑操作
    // ================================================================

    public void InsertText(string text)
    {
        PushUndo();
        Lines[Cy].Insert(Cx, text);
        Cx += text.Length;
        Modified = true;
        OnContentChanged?.Invoke();
    }

    public void Backspace()
    {
        if (Cx > 0)
        {
            PushUndo();
            Lines[Cy].Remove(Cx - 1, 1);
            Cx--;
            Modified = true;
        }
        else if (Cy > 0)
        {
            // 合并到上一行
            PushUndo();
            Cx = Lines[Cy - 1].Length;
            Lines[Cy - 1].Append(Lines[Cy]);
            Lines.RemoveAt(Cy);
            Cy--;
            Modified = true;
        }
        OnContentChanged?.Invoke();
    }

    public void Delete()
    {
        if (Cx < Lines[Cy].Length)
        {
            PushUndo();
            Lines[Cy].Remove(Cx, 1);
            Modified = true;
        }
        else if (Cy < Lines.Count - 1)
        {
            // 合并下一行
            PushUndo();
            Lines[Cy].Append(Lines[Cy + 1]);
            Lines.RemoveAt(Cy + 1);
            Modified = true;
        }
        OnContentChanged?.Invoke();
    }

    public void NewLine()
    {
        PushUndo();
        var rest = Lines[Cy].ToString()[Cx..];
        Lines[Cy].Remove(Cx, Lines[Cy].Length - Cx);
        Lines.Insert(Cy + 1, new StringBuilder(rest));
        Cy++;
        Cx = 0;
        Modified = true;
        OnContentChanged?.Invoke();
    }

    public void InsertTab()
    {
        InsertText("    ");
    }

    // ── 剪贴板操作 ──

    public void CopyLine()
    {
        _clipboard = Lines[Cy].ToString();
    }

    public void CutLine()
    {
        _clipboard = Lines[Cy].ToString();
        if (Lines.Count > 1)
        {
            PushUndo();
            Lines.RemoveAt(Cy);
            if (Cy >= Lines.Count) Cy = Lines.Count - 1;
            Cx = 0;
            Modified = true;
        }
        else
        {
            PushUndo();
            Lines[Cy].Clear();
            Cx = 0;
            Modified = true;
        }
        OnContentChanged?.Invoke();
    }

    public void PasteClipboard()
    {
        if (!string.IsNullOrEmpty(_clipboard))
            InsertText(_clipboard);
    }

    public void DeleteLine()
    {
        CutLine();
    }

    // ================================================================
    // 撤销
    // ================================================================

    private void PushUndo()
    {
        _undo.Push(new EditAction(Cy, Cx,
            Lines[Cy].ToString(), "", Lines.Count));
    }

    public void Undo()
    {
        if (!_undo.TryPop(out var act)) return;
        Lines[act.Line] = new StringBuilder(act.OldLine);
        // Remove inserted lines (e.g., from NewLine which inserts at Line+1)
        while (Lines.Count > act.OldCount)
            Lines.RemoveAt(act.Line + 1);
        Cy = act.Line;
        Cx = act.Col;
        Modified = true;
        OnContentChanged?.Invoke();
    }

    // ================================================================
    // 诊断委托
    // ================================================================

    public List<Diagnostic> GetDiagnosticsAtLine(int lineNumber)
        => DiagnosticManager.GetForLine(FilePath, lineNumber);

    public (int errors, int warnings) GetDiagSummary()
        => DiagnosticManager.GetSummary(FilePath);

    // ================================================================
    // 统计
    // ================================================================

    public int TotalChars => Lines.Sum(l => l.Length);
    public int TotalLines => Lines.Count;

    public long FileSizeBytes =>
        Encoding.UTF8.GetByteCount(string.Join("\n", Lines.Select(l => l.ToString())));

    public static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024):F1} MB",
    };
}
