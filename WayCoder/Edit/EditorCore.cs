using System.Text;
using System.Text.RegularExpressions;

namespace WayCoder;

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
        _outlineDirty = true;
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
        _outlineDirty = true;
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
        _outlineDirty = true;
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
        _outlineDirty = true;
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
        _outlineDirty = true;
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
        _outlineDirty = true;
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
        _outlineDirty = true;
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

    // ================================================================
    // 大纲 / 符号提取
    // ================================================================

    /// <summary>代码大纲项</summary>
    public record OutlineItem(string Name, int Line, string Kind, string Icon);

    private List<OutlineItem>? _cachedOutline;
    private bool _outlineDirty = true;

    /// <summary>提取当前文件的大纲（带缓存）</summary>
    public List<OutlineItem> ExtractOutline()
    {
        if (!_outlineDirty && _cachedOutline != null)
            return _cachedOutline;

        var ext = Path.GetExtension(FilePath).ToLowerInvariant();
        _cachedOutline = OutlineExtractor.Extract(Lines, ext);
        _outlineDirty = false;
        return _cachedOutline;
    }

    /// <summary>标记大纲缓存失效（内容变更时调用）</summary>
    public void InvalidateOutline()
    {
        _outlineDirty = true;
    }
}

/// <summary>
/// 大纲提取器 —— 按文件扩展名用正则提取函数/类/方法等符号。
/// 模式借鉴自 RepoMapGenerator.SymbolPatterns。
/// </summary>
public static class OutlineExtractor
{
    private static readonly Dictionary<string, (Regex Regex, string Kind, string Icon)[]> Patterns = new()
    {
        [".cs"] = new[]
        {
            (new Regex(@"^\s*(?:public|private|protected|internal|static|sealed|abstract|partial|readonly|async|override|virtual)?\s*class\s+(\w+)"), "class", "📦"),
            (new Regex(@"^\s*(?:public|private|protected|internal|static|sealed|abstract|partial|readonly|async|override|virtual)?\s*interface\s+(\w+)"), "interface", "📐"),
            (new Regex(@"^\s*(?:public|private|protected|internal|static|sealed|abstract|partial|readonly|async|override|virtual)?\s*struct\s+(\w+)"), "struct", "🏗"),
            (new Regex(@"^\s*(?:public|private|protected|internal|static|sealed|abstract|partial|readonly|async|override|virtual)?\s*enum\s+(\w+)"), "enum", "📋"),
            (new Regex(@"^\s*(?:public|private|protected|internal|static|sealed|abstract|partial|readonly|async|override|virtual)?\s*record\s+(\w+)"), "record", "📋"),
            (new Regex(@"^\s*(?:public|private|protected|internal|static|sealed|abstract|partial|readonly|async|override|virtual)?\s*(?:\w+(?:<[^>]*>)?\s+)?(\w+)\s*\("), "method", "🔧"),
            (new Regex(@"^\s*(?:public|private|protected|internal|static|sealed|abstract|partial|readonly|async|override|virtual)?\s*(?:\w+(?:<[^>]*>)?\s+)(\w+)\s*\{?\s*(?:get|set|=>)"), "property", "🔑"),
        },
        [".py"] = new[]
        {
            (new Regex(@"^\s*class\s+(\w+)"), "class", "📦"),
            (new Regex(@"^\s*(?:async\s+)?def\s+(\w+)"), "method", "🔧"),
        },
        [".js"] = new[]
        {
            (new Regex(@"^\s*(?:export\s+)?class\s+(\w+)"), "class", "📦"),
            (new Regex(@"^\s*(?:export\s+)?(?:async\s+)?function\s+(\w+)"), "function", "𝑓"),
            (new Regex(@"^\s*(?:export\s+)?(?:const|let|var)\s+(\w+)\s*=\s*(?:async\s*)?\("), "function", "𝑓"),
            (new Regex(@"^\s*(?:export\s+)?(?:const|let|var)\s+(\w+)\s*="), "variable", "📌"),
        },
        [".ts"] = new[]
        {
            (new Regex(@"^\s*(?:export\s+)?(?:abstract\s+)?class\s+(\w+)"), "class", "📦"),
            (new Regex(@"^\s*(?:export\s+)?interface\s+(\w+)"), "interface", "📐"),
            (new Regex(@"^\s*(?:export\s+)?(?:async\s+)?function\s+(\w+)"), "function", "𝑓"),
            (new Regex(@"^\s*(?:export\s+)?(?:const|let|var)\s+(\w+)\s*=\s*(?:async\s*)?\("), "function", "𝑓"),
            (new Regex(@"^\s*(?:export\s+)?(?:const|let|var)\s+(\w+)\s*="), "variable", "📌"),
        },
        [".go"] = new[]
        {
            (new Regex(@"^\s*type\s+(\w+)\s+struct"), "struct", "🏗"),
            (new Regex(@"^\s*type\s+(\w+)\s+interface"), "interface", "📐"),
            (new Regex(@"^\s*func\s+(?:\([^)]*\)\s+)?(\w+)"), "function", "𝑓"),
        },
        [".rs"] = new[]
        {
            (new Regex(@"^\s*(?:pub\s+)?struct\s+(\w+)"), "struct", "🏗"),
            (new Regex(@"^\s*(?:pub\s+)?enum\s+(\w+)"), "enum", "📋"),
            (new Regex(@"^\s*(?:pub\s+)?trait\s+(\w+)"), "trait", "📐"),
            (new Regex(@"^\s*(?:pub\s+)?(?:async\s+)?fn\s+(\w+)"), "function", "𝑓"),
            (new Regex(@"^\s*(?:pub\s+)?impl\s+(\w+)"), "impl", "🔧"),
        },
        [".java"] = new[]
        {
            (new Regex(@"^\s*(?:public|private|protected)?\s*(?:static|final|abstract)?\s*class\s+(\w+)"), "class", "📦"),
            (new Regex(@"^\s*(?:public|private|protected)?\s*(?:static|final|abstract)?\s*interface\s+(\w+)"), "interface", "📐"),
            (new Regex(@"^\s*(?:public|private|protected)?\s*(?:static|final|abstract)?\s*enum\s+(\w+)"), "enum", "📋"),
            (new Regex(@"^\s*(?:public|private|protected)?\s*(?:static|final|abstract)?\s*(?:\w+(?:<[^>]*>)?\s+)?(\w+)\s*\("), "method", "🔧"),
        },
        [".c"] = new[] { (new Regex(@"^\s*\w[\w\s*]+\s+(\w+)\s*\("), "function", "𝑓"), },
        [".cpp"] = new[]
        {
            (new Regex(@"^\s*(?:class|struct)\s+(\w+)"), "class", "📦"),
            (new Regex(@"^\s*\w[\w\s*:]+\s+(\w+)\s*\("), "function", "𝑓"),
        },
        [".h"] = new[]
        {
            (new Regex(@"^\s*(?:class|struct)\s+(\w+)"), "class", "📦"),
            (new Regex(@"^\s*\w[\w\s*]+\s+(\w+)\s*\("), "function", "𝑓"),
        },
        [".swift"] = new[]
        {
            (new Regex(@"^\s*(?:public\s+)?class\s+(\w+)"), "class", "📦"),
            (new Regex(@"^\s*(?:public\s+)?struct\s+(\w+)"), "struct", "🏗"),
            (new Regex(@"^\s*(?:public\s+)?func\s+(\w+)"), "function", "𝑓"),
        },
        [".kt"] = new[]
        {
            (new Regex(@"^\s*(?:data\s+)?class\s+(\w+)"), "class", "📦"),
            (new Regex(@"^\s*object\s+(\w+)"), "object", "📦"),
            (new Regex(@"^\s*(?:suspend\s+)?fun\s+(\w+)"), "function", "𝑓"),
        },
        [".sh"] = new[]
        {
            (new Regex(@"^\s*function\s+(\w+)"), "function", "𝑓"),
            (new Regex(@"^(\w+)\s*\(\s*\)"), "function", "𝑓"),
        },
        [".sql"] = new[]
        {
            (new Regex(@"^\s*CREATE\s+(?:TABLE|INDEX|VIEW|PROCEDURE|FUNCTION)\s+(\w+)", RegexOptions.IgnoreCase), "ddl", "📋"),
        },
        [".md"] = new[]
        {
            (new Regex(@"^#+\s+(.+)"), "heading", "📝"),
        },
        // .tsx / .jsx 复用 .ts / .js
        [".tsx"] = null!, [".jsx"] = null!,
    };

    // 需要过滤的关键字（防止把类型名当方法名）
    private static readonly HashSet<string> FilterWords = new()
    {
        "if", "for", "while", "return", "class", "struct", "interface", "enum",
        "public", "private", "protected", "static", "void", "int", "string",
        "bool", "var", "let", "const", "function", "export", "import", "from",
        "async", "await", "new", "this", "super", "extends", "implements",
        "override", "virtual", "abstract", "record", "sealed", "internal",
        "readonly", "partial", "get", "set", "throw", "throws", "catch", "try",
        "switch", "case", "default", "break", "continue", "typeof", "instanceof",
        "namespace", "using", "yield", "Task", "void", "object", "dynamic",
    };

    public static List<EditorCore.OutlineItem> Extract(List<System.Text.StringBuilder> lines, string ext)
    {
        var results = new List<EditorCore.OutlineItem>();

        // .tsx/.jsx 复用 .ts/.js 模式
        if (ext == ".tsx") ext = ".ts";
        if (ext == ".jsx") ext = ".js";

        if (!Patterns.TryGetValue(ext, out var patterns) || patterns == null!)
            return results;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i].ToString();
            if (string.IsNullOrWhiteSpace(line)) continue;

            foreach (var (regex, kind, icon) in patterns)
            {
                var m = regex.Match(line);
                if (!m.Success) continue;

                // 取第一个命名组
                foreach (Group g in m.Groups)
                {
                    if (!g.Success || g.Index == 0 || string.IsNullOrEmpty(g.Value)) continue;
                    var name = g.Value;
                    if (name.Length <= 1) continue;
                    if (FilterWords.Contains(name)) continue;
                    // 跳过以 _ 开头的私有成员（可选：保留 _ 开头的）
                    results.Add(new EditorCore.OutlineItem(name, i + 1, kind, icon));
                    break; // 每行只取一个匹配
                }
                break; // 匹配到第一个模式就停止
            }
        }

        return results;
    }
}
