using System.Text;
using System.Text.RegularExpressions;

namespace WayCoder.UI.Tui.Edit;

/// <summary>查找/替换选项：区分大小写 / 正则（捕获组 $1/${name}）/ 整词匹配。</summary>
public readonly record struct FindOptions(bool CaseSensitive = false, bool UseRegex = false, bool WholeWord = false);

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

    /// <summary>只读模式：不允许修改缓冲区（编辑方法拒绝），只能查看/滚动/查找。</summary>
    public bool ReadOnly { get; set; }

    // ── 光标 (0-based) ──
    public int Cy { get; set; }
    public int Cx { get; set; }

    // ── 滚动偏移（可见区域第一行的行索引） ──
    public int Scroll { get; set; }

    // ── 语法 ──
    public Syntax Syntax { get; private set; } = Syntax.ForFile("untitled.txt");

    // ── 缩进模式（tab=制表符 / space=4 空格，由 EditorScreen 从 Config 注入）──
    public string IndentMode { get; set; } = "tab";

    // ── 撤销 / 重做（双栈）──
    private readonly Stack<EditAction> _undo = new();
    private readonly Stack<EditAction> _redo = new();
    private const int MaxUndo = 100;
    /// <summary>'I'=插入文本（含换行/缩进）'D'=删除文本（含换行）'R'=整块替换（OldText=旧块）</summary>
    private record EditAction(char Type, int Line, int Col, string Text, string OldText = "");

    // ── 选择（锚点 = 固定端，光标 = 移动端）──
    private int _selAnchorLine = -1;
    private int _selAnchorCol = -1;

    // ── 剪贴板（内部兜底缓存，优先系统剪贴板）──
    private static string _clipboard = "";

    // ── 统计缓存（内容变更失效，避免状态栏每帧全量计算）──
    private int _cachedTotalChars = -1;
    private long _cachedFileSize = -1;
    private bool _statsDirty = true;

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
        _redo.Clear();
        ClearSelection();
        _outlineDirty = true;
        _statsDirty = true;
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

        // 光标不落在代理对中间（emoji/CJK 扩展 B）：左右/上下移动后统一修正。
        // 旧代码仅 dx!=0 修正 → 上下移动（dx==0）落到代理对中间后，Backspace/插入
        // 会把 emoji 切成两半，保存时 Encoding.UTF8 替换回退成 U+FFFD 破坏文件。
        var line = Lines[Cy].ToString();
        if (Cx > 0 && Cx < line.Length
            && char.IsHighSurrogate(line[Cx - 1]) && char.IsLowSurrogate(line[Cx]))
            Cx += dx > 0 ? 1 : -1;
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

    /// <summary>跳到指定行+列（0-based），列钳制到行长度。</summary>
    public bool JumpToLineCol(int line, int col)
    {
        if (line < 0 || line >= Lines.Count) return false;
        Cy = line;
        Cx = Math.Clamp(col, 0, Lines[line].Length);
        // 光标不落在代理对中间（对齐 MoveCursor）：列落在 emoji/CJK 扩展 B 中间时向右移 1，
        // 否则后续 InsertText 会把代理对切成两半、保存时编码回退成 U+FFFD 破坏文件
        var ln = Lines[line].ToString();
        if (Cx > 0 && Cx < ln.Length && char.IsHighSurrogate(ln[Cx - 1]) && char.IsLowSurrogate(ln[Cx]))
            Cx++;
        return true;
    }

    /// <summary>
    /// 解析跳转输入 "行" 或 "行:列"（1-based，与状态栏 L/C 一致）。
    /// 空段保留当前值：":列" = 当前行指定列；"行:" = 指定行第 0 列。
    /// 返回 0-based (line, col)；非法（非数字 / 行越界）返回 null。
    /// </summary>
    public static (int Line, int Col)? ParseLineCol(string input, int currentLine, int totalLines)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        input = input.Trim();

        string linePart, colPart = "";
        int sep = input.IndexOf(':');
        if (sep < 0) { linePart = input; }
        else { linePart = input[..sep]; colPart = input[(sep + 1)..]; }

        int line;
        if (linePart.Length == 0)
            line = currentLine;
        else if (!int.TryParse(linePart, out line))
            return null;
        else
            line -= 1; // 1-based → 0-based

        if (line < 0 || line >= totalLines) return null;

        int col = 0;
        if (colPart.Length > 0)
        {
            if (!int.TryParse(colPart, out col)) return null;
            col -= 1; // 1-based → 0-based
            if (col < 0) col = 0;
        }
        return (line, col);
    }

    // ================================================================
    // 编辑操作
    // ================================================================

    public void InsertText(string text)
    {
        if (ReadOnly) return; // 只读模式：禁止修改
        text = text.Replace("\r\n", "\n");
        if (string.IsNullOrEmpty(text)) return;
        if (HasSelection) DeleteSelection();
        int beforeLine = Cy, beforeCol = Cx;
        InsertTextAt(Cy, Cx, text);
        // 光标移到插入文本末尾
        int newLines = text.Count(c => c == '\n');
        int lastNl = text.LastIndexOf('\n');
        if (newLines > 0) { Cy += newLines; Cx = text.Length - lastNl - 1; }
        else Cx += text.Length;

        // 撤销合并：连续键入单字符（无换行、长度 1）归并为同一个 undo 单元，
        // 避免逐键入栈、100 上限很快打满（对齐 vim「一次插入」为一个撤销单元）。
        bool coalesce = newLines == 0 && text.Length == 1
            && _undo.Count > 0
            && _undo.Peek() is { Type: 'I', Line: var al, Col: var ac, Text: var at }
            && al == beforeLine && ac + at.Length == beforeCol;
        if (coalesce)
        {
            var top = _undo.Pop();
            _undo.Push(top with { Text = top.Text + text });
        }
        else
        {
            Record('I', beforeLine, beforeCol, text);
        }
        MarkChanged();
    }

    public void Backspace()
    {
        if (ReadOnly) return; // 只读模式：禁止修改
        if (HasSelection) { DeleteSelection(); return; }
        if (Cx > 0)
        {
            // 光标前若是代理对（emoji/CJK 扩展 B），删除整个码点而非半个
            int width = Cx >= 2 && char.IsHighSurrogate(Lines[Cy][Cx - 2]) && char.IsLowSurrogate(Lines[Cy][Cx - 1])
                ? 2 : 1;
            var ch = Lines[Cy].ToString().Substring(Cx - width, width);
            Record('D', Cy, Cx - width, ch);
            DeleteTextAt(Cy, Cx - width, ch);
            Cx -= width;
        }
        else if (Cy > 0)
        {
            // 删除与上一行的换行符（合行）
            int joinCol = Lines[Cy - 1].Length;
            Record('D', Cy - 1, joinCol, "\n");
            DeleteTextAt(Cy - 1, joinCol, "\n");
            Cx = joinCol;
            Cy--;
        }
        MarkChanged();
    }

    public void Delete()
    {
        if (ReadOnly) return; // 只读模式：禁止修改
        if (HasSelection) { DeleteSelection(); return; }
        if (Cx < Lines[Cy].Length)
        {
            // 光标处若是代理对（emoji/CJK 扩展 B），删除整个码点而非半个
            int width = Cx + 1 < Lines[Cy].Length && char.IsHighSurrogate(Lines[Cy][Cx]) && char.IsLowSurrogate(Lines[Cy][Cx + 1])
                ? 2 : 1;
            var ch = Lines[Cy].ToString().Substring(Cx, width);
            Record('D', Cy, Cx, ch);
            DeleteTextAt(Cy, Cx, ch);
        }
        else if (Cy < Lines.Count - 1)
        {
            // 删除与下一行的换行符（合行）
            Record('D', Cy, Lines[Cy].Length, "\n");
            DeleteTextAt(Cy, Lines[Cy].Length, "\n");
        }
        MarkChanged();
    }

    public void NewLine()
    {
        if (ReadOnly) return; // 只读模式：禁止修改
        if (HasSelection) DeleteSelection();
        var indent = GetIndent(Lines[Cy].ToString());
        var text = "\n" + indent;
        Record('I', Cy, Cx, text);
        InsertTextAt(Cy, Cx, text);
        Cy++;
        Cx = indent.Length;
        MarkChanged();
    }

    public void InsertTab()
    {
        if (ReadOnly) return; // 只读模式：禁止修改
        // 缩进模式可配置：默认 tab（制表符），space 模式用 4 空格
        InsertText(IndentMode == "space" ? "    " : "\t");
    }

    // ── 剪贴板操作（系统剪贴板优先，内部缓存兜底）──

    public void CopyLine()
    {
        var text = HasSelection ? GetSelectedText()! : Lines[Cy].ToString();
        _clipboard = text;
        try { ClipboardHelper.SetText(text); } catch { /* 系统剪贴板不可用时回退内部缓存 */ }
    }

    public void CutLine()
    {
        CopyLine();
        if (HasSelection) DeleteSelection();
        else DeleteLine();
    }

    public void PasteClipboard()
    {
        // 内部剪贴板优先（刚复制/剪切的）——CLI 无 GUI 剪贴板会话（Keypad 测试/SSH）时
        // 系统剪贴板读到残留内容，内部兜底保证项目内复制→粘贴一致；内部空才回退系统。
        var text = _clipboard;
        if (string.IsNullOrEmpty(text))
        {
            try { text = ClipboardHelper.GetText(); } catch { }
        }
        if (!string.IsNullOrEmpty(text)) InsertText(text);
    }

    public void DeleteLine()
    {
        if (ReadOnly) return; // 只读模式：禁止修改
        if (HasSelection) { DeleteSelection(); return; }
        var content = Lines[Cy].ToString();
        if (Lines.Count == 1)
        {
            // 唯一一行：仅清空
            Record('D', Cy, 0, content);
            DeleteTextAt(Cy, 0, content);
        }
        else if (Cy < Lines.Count - 1)
        {
            // 非末行：连换行符一起删除，整行移除
            var text = content + "\n";
            Record('D', Cy, 0, text);
            DeleteTextAt(Cy, 0, text);
        }
        else
        {
            // 末行：删除前导换行符 + 本行内容，整行移除，光标上移
            var text = "\n" + content;
            int joinCol = Lines[Cy - 1].Length;
            Record('D', Cy - 1, joinCol, text);
            DeleteTextAt(Cy - 1, joinCol, text);
            Cy--;
        }
        Cx = 0;
        MarkChanged();
    }

    // ================================================================
    // 词级操作 + 行操作 + 块缩进（vim/edit 补强）
    // ================================================================

    /// <summary>单词级光标移动。dir&lt;0 左移一词，dir&gt;0 右移一词（跳过空白 + 连续非空白，单行内）。</summary>
    public void MoveWord(int dir)
    {
        var line = Lines[Cy].ToString();
        if (dir < 0)
        {
            int p = Cx;
            while (p > 0 && char.IsWhiteSpace(line[p - 1])) p--;
            while (p > 0 && !char.IsWhiteSpace(line[p - 1])) p--;
            Cx = p;
        }
        else
        {
            int len = line.Length;
            int p = Cx;
            while (p < len && !char.IsWhiteSpace(line[p])) p++;
            while (p < len && char.IsWhiteSpace(line[p])) p++;
            Cx = p;
        }
        ClearSelection();
    }

    /// <summary>删除光标前一个词（空白 + 连续非空白）。</summary>
    public void DeleteWordBefore()
    {
        if (ReadOnly) return; // 只读模式：禁止修改
        if (HasSelection) { DeleteSelection(); return; }
        var line = Lines[Cy].ToString();
        int p = Cx;
        while (p > 0 && char.IsWhiteSpace(line[p - 1])) p--;
        while (p > 0 && !char.IsWhiteSpace(line[p - 1])) p--;
        if (p == Cx) return; // 行首无词可删
        var text = line[p..Cx];
        Record('D', Cy, p, text);
        DeleteTextAt(Cy, p, text);
        Cx = p;
        MarkChanged();
    }

    /// <summary>删除光标后一个词（连续非空白，不含后续空白）。</summary>
    public void DeleteWordAfter()
    {
        if (ReadOnly) return; // 只读模式：禁止修改
        if (HasSelection) { DeleteSelection(); return; }
        var line = Lines[Cy].ToString();
        int len = line.Length;
        int start = Cx;
        while (start < len && char.IsWhiteSpace(line[start])) start++;
        int end = start;
        while (end < len && !char.IsWhiteSpace(line[end])) end++;
        if (end == start) return;
        var text = line[start..end];
        Record('D', Cy, start, text);
        DeleteTextAt(Cy, start, text);
        MarkChanged();
    }

    /// <summary>删除从光标到行尾的文本。</summary>
    public void DeleteToLineEnd()
    {
        if (ReadOnly) return; // 只读模式：禁止修改
        if (HasSelection) { DeleteSelection(); return; }
        var line = Lines[Cy].ToString();
        int col = Math.Min(Cx, line.Length);
        if (col >= line.Length) return;
        var text = line[col..];
        Record('D', Cy, col, text);
        DeleteTextAt(Cy, col, text);
        MarkChanged();
    }

    /// <summary>复制当前行到下一行（重复行，Ctrl+D）。</summary>
    public void DuplicateLine()
    {
        if (HasSelection) DeleteSelection();
        var content = Lines[Cy].ToString();
        Record('I', Cy, Lines[Cy].Length, "\n" + content);
        InsertTextAt(Cy, Lines[Cy].Length, "\n" + content);
        Cy++;
        Cx = Math.Min(Cx, Lines[Cy].Length);
        MarkChanged();
    }

    /// <summary>
    /// 整块缩进 / 反缩进。选中多行时作用于整块；无选区时作用于当前行。
    /// dir&gt;0 缩进（首插 tab/4 空格），dir&lt;0 反缩进（去一级缩进）。
    /// 作为单个撤销单元（'R' 整块替换）记录。
    /// </summary>
    public void IndentBlock(int dir)
    {
        int first, last;
        if (HasSelection)
        {
            var (sl, _, el, _) = NormalizedSelection();
            first = sl;
            last = el; // 块缩进按整行处理：选区覆盖到的每一行（含光标所在末行）都缩进（对齐 vim 可视行 >）
        }
        else
        {
            first = last = Cy;
        }
        if (last < first) last = first;

        var unit = IndentMode == "space" ? "    " : "\t";
        var oldLines = new List<string>(last - first + 1);
        var newLines = new List<string>(last - first + 1);
        for (int i = first; i <= last; i++)
        {
            var line = Lines[i].ToString();
            oldLines.Add(line);
            newLines.Add(dir > 0 ? unit + line : RemoveIndentUnit(line));
        }
        var oldBlock = string.Join("\n", oldLines);
        var newBlock = string.Join("\n", newLines);
        if (oldBlock == newBlock) return;

        Record('R', first, 0, newBlock, oldBlock);
        ReplaceBlockAt(first, newLines);
        Cx = Math.Min(Cx, Lines[Cy].Length);
        ClearSelection();
        MarkChanged();
    }

    /// <summary>移除行首一级缩进（一个 tab 或最多 4 个空格）。</summary>
    private static string RemoveIndentUnit(string line)
    {
        if (line.Length == 0) return line;
        if (line[0] == '\t') return line[1..];
        int n = 0;
        while (n < line.Length && n < 4 && line[n] == ' ') n++;
        return line[n..];
    }

    /// <summary>整块替换：删除 firstLine 起的 oldLines.Count 整行，插入 newLines。</summary>
    private void ReplaceBlockAt(int firstLine, List<string> newLines)
    {
        TrackChange(firstLine, firstLine + newLines.Count - 1);
        for (int i = 0; i < newLines.Count; i++) Lines.RemoveAt(firstLine);
        for (int i = 0; i < newLines.Count; i++) Lines.Insert(firstLine + i, new StringBuilder(newLines[i]));
    }

    /// <summary>删除 line 起的 block.Split('\n').Length 整行。</summary>
    private void DeleteBlockAt(int line, string block)
    {
        TrackChange(line, line + CountLines(block));
        var parts = block.Split('\n');
        for (int i = parts.Length - 1; i >= 0; i--) Lines.RemoveAt(line + i);
    }

    /// <summary>在 line 处插入 block.Split('\n') 整行。</summary>
    private void InsertBlockAt(int line, string block)
    {
        TrackChange(line, line + CountLines(block));
        var parts = block.Split('\n');
        for (int i = 0; i < parts.Length; i++) Lines.Insert(line + i, new StringBuilder(parts[i]));
    }

    // ================================================================
    // 撤销 / 重做（双栈）
    // ================================================================

    private void Record(char type, int line, int col, string text, string oldText = "")
    {
        _undo.Push(new EditAction(type, line, col, text, oldText));
        if (_undo.Count > MaxUndo) TrimBottom(_undo, MaxUndo);
        _redo.Clear();
    }

    internal static void TrimBottom<T>(Stack<T> stack, int max)
    {
        if (stack.Count <= max) return;
        // Stack<T>.ToArray() 栈顶在前：arr[0]=最新入栈、arr[^1]=最旧。保留最新的 max 条（丢弃最旧）。
        var arr = stack.ToArray();
        stack.Clear();
        for (int i = max - 1; i >= 0; i--)
            stack.Push(arr[i]);
    }

    public void Undo()
    {
        if (!_undo.TryPop(out var act)) return;
        _redo.Push(act);
        ClearSelection();
        switch (act.Type)
        {
            case 'I':
                DeleteTextAt(act.Line, act.Col, act.Text);
                Cy = act.Line; Cx = act.Col;
                break;
            case 'D':
                InsertTextAt(act.Line, act.Col, act.Text);
                MoveCursorAfter(act);
                break;
            case 'R':
                DeleteBlockAt(act.Line, act.Text);      // 删新块
                InsertBlockAt(act.Line, act.OldText);   // 恢复旧块
                Cy = act.Line; Cx = act.Col;
                break;
        }
        MarkChanged();
    }

    public void Redo()
    {
        if (!_redo.TryPop(out var act)) return;
        _undo.Push(act);
        ClearSelection();
        switch (act.Type)
        {
            case 'I':
                InsertTextAt(act.Line, act.Col, act.Text);
                MoveCursorAfter(act);
                break;
            case 'D':
                DeleteTextAt(act.Line, act.Col, act.Text);
                Cy = act.Line; Cx = act.Col;
                break;
            case 'R':
                DeleteBlockAt(act.Line, act.OldText);   // 删旧块
                InsertBlockAt(act.Line, act.Text);      // 恢复新块
                Cy = act.Line; Cx = act.Col;
                break;
        }
        MarkChanged();
    }

    /// <summary>将光标置于插入/删除文本的末尾位置。</summary>
    private void MoveCursorAfter(EditAction act)
    {
        int newLines = act.Text.Count(c => c == '\n');
        int lastNl = act.Text.LastIndexOf('\n');
        if (newLines > 0) { Cy = act.Line + newLines; Cx = act.Text.Length - lastNl - 1; }
        else { Cy = act.Line; Cx = act.Col + act.Text.Length; } // 单行删除也须还原行号，否则连续撤销跨行时光标停在错行
    }

    // ================================================================
    // 底层编辑原语（供撤销/重做与公共操作复用）
    // ================================================================

    /// <summary>在 (line, col) 插入文本（支持多行，按 \n 拆行）。</summary>
    private void InsertTextAt(int line, int col, string text)
    {
        if (text.Length == 0) return;
        TrackChange(line, line + CountLines(text));
        var parts = text.Split('\n');
        var cur = Lines[line].ToString();
        col = Math.Min(col, cur.Length);
        var left = cur[..col];
        var right = cur[col..];
        if (parts.Length == 1)
        {
            Lines[line] = new StringBuilder(left + parts[0] + right);
            return;
        }
        Lines[line] = new StringBuilder(left + parts[0]);
        for (int i = 1; i < parts.Length - 1; i++)
            Lines.Insert(line + i, new StringBuilder(parts[i]));
        Lines.Insert(line + parts.Length - 1, new StringBuilder(parts[parts.Length - 1] + right));
    }

    /// <summary>删除 (line, col) 处的文本（支持多行，跨行时连中间行一起移除）。</summary>
    private void DeleteTextAt(int line, int col, string text)
    {
        if (text.Length == 0) return;
        TrackChange(line, line + CountLines(text));
        var parts = text.Split('\n');
        if (parts.Length == 1)
        {
            Lines[line].Remove(col, text.Length);
            return;
        }
        var first = Lines[line].ToString();
        var last = Lines[line + parts.Length - 1].ToString();
        int lastPartLen = parts[parts.Length - 1].Length;
        Lines[line] = new StringBuilder(
            first[..Math.Min(col, first.Length)] + last[Math.Min(lastPartLen, last.Length)..]);
        for (int i = parts.Length - 1; i >= 1; i--)
            Lines.RemoveAt(line + i);
    }

    /// <summary>提取行首缩进（空格 + Tab）。</summary>
    private static string GetIndent(string line)
    {
        int i = 0;
        while (i < line.Length && (line[i] == ' ' || line[i] == '\t')) i++;
        return line[..i];
    }

    // ================================================================
    // 选择（锚点模型：锚点 = 固定端，光标 = 移动端）
    // ================================================================

    public bool HasSelection => _selAnchorLine >= 0 && !(_selAnchorLine == Cy && _selAnchorCol == Cx);
    public void StartSelection() { _selAnchorLine = Cy; _selAnchorCol = Cx; }
    public void ExtendSelection() { /* 锚点不动，范围 = 锚点..光标 */ }
    public void ClearSelection() { _selAnchorLine = -1; _selAnchorCol = -1; }

    public void SelectAll()
    {
        _selAnchorLine = 0; _selAnchorCol = 0;
        Cy = Lines.Count - 1; Cx = Lines[^1].Length;
    }

    /// <summary>归一化选区为 (startLine, startCol, endLine, endCol)，start ≤ end。</summary>
    private (int sl, int sc, int el, int ec) NormalizedSelection()
    {
        if (_selAnchorLine < 0) return (Cy, Cx, Cy, Cx);
        var a = (_selAnchorLine, _selAnchorCol);
        var b = (Cy, Cx);
        return a.CompareTo(b) <= 0 ? (a.Item1, a.Item2, b.Item1, b.Item2)
                                   : (b.Item1, b.Item2, a.Item1, a.Item2);
    }

    public string? GetSelectedText()
    {
        if (!HasSelection) return null;
        var (sl, sc, el, ec) = NormalizedSelection();
        if (sl == el)
        {
            var line = Lines[sl].ToString();
            return line.Substring(Math.Min(sc, line.Length), Math.Max(0, Math.Min(ec, line.Length) - sc));
        }
        var sb = new StringBuilder();
        sb.Append(Lines[sl].ToString()[Math.Min(sc, Lines[sl].Length)..]).Append('\n');
        for (int i = sl + 1; i < el; i++) sb.Append(Lines[i]).Append('\n');
        var last = Lines[el].ToString();
        sb.Append(last[..Math.Min(ec, last.Length)]);
        return sb.ToString();
    }

    public void DeleteSelection()
    {
        if (ReadOnly) return; // 只读模式：禁止修改
        if (!HasSelection) return;
        var (sl, sc, el, ec) = NormalizedSelection();
        var text = GetSelectedText()!;
        Record('D', sl, sc, text);
        DeleteTextAt(sl, sc, text);
        Cy = sl; Cx = sc;
        ClearSelection();
        MarkChanged();
    }

    // ================================================================
    // 搜索 / 替换
    // ================================================================

    /// <summary>从 (fromLine, fromCol) 向后查找，返回命中位置；未找到返回 (-1, -1)。</summary>
    public (int Line, int Col) FindNext(string query, int fromLine, int fromCol, FindOptions opts = default)
    {
        var (line, col, _) = FindMatch(query, fromLine, fromCol, opts);
        return (line, col);
    }

    /// <summary>从 (fromLine, fromCol) 向后查找，返回命中起始位置与匹配长度（正则命中长度 ≠ 模式长度）。</summary>
    public (int Line, int Col, int Length) FindMatch(string query, int fromLine, int fromCol, FindOptions opts = default)
    {
        if (string.IsNullOrEmpty(query) || Lines.Count == 0) return (-1, -1, 0);
        var regex = BuildFindRegex(query, opts);
        if (regex == null) return (-1, -1, 0);
        for (int li = fromLine; li < Lines.Count; li++)
        {
            var line = Lines[li].ToString();
            int startCol = li == fromLine ? Math.Min(fromCol, line.Length) : 0;
            var m = regex.Match(line, startCol);
            if (m.Success) return (li, m.Index, m.Length);
        }
        return (-1, -1, 0);
    }

    /// <summary>替换全部匹配，返回替换次数。支持正则捕获组（$1/${name}）与整词匹配。</summary>
    public int ReplaceAll(string find, string replace, FindOptions opts = default)
    {
        if (string.IsNullOrEmpty(find)) return 0;
        var regex = BuildFindRegex(find, opts);
        if (regex == null) return 0;
        var replacement = ToReplacement(replace, opts.UseRegex);
        // 替换串含换行会破坏"一行一条目"不变量，且撤销（DeleteBlockAt 按行块删除）会越界崩溃 —— 拒绝
        if (replacement.Contains('\n')) return 0;
        int firstLine = -1, lastLine = -1, count = 0;
        for (int li = 0; li < Lines.Count; li++)
        {
            var line = Lines[li].ToString();
            var matches = regex.Matches(line);
            if (matches.Count == 0) continue;
            if (firstLine < 0) firstLine = li;
            lastLine = li;
            count += matches.Count;
        }
        if (count == 0) return 0;

        var oldBlock = string.Join("\n", Enumerable.Range(firstLine, lastLine - firstLine + 1).Select(i => Lines[i].ToString()));
        for (int li = firstLine; li <= lastLine; li++)
            Lines[li] = new StringBuilder(regex.Replace(Lines[li].ToString(), replacement));
        var newBlock = string.Join("\n", Enumerable.Range(firstLine, lastLine - firstLine + 1).Select(i => Lines[i].ToString()));
        Record('R', firstLine, 0, newBlock, oldBlock); // 支持 Ctrl+Z 撤销整块替换
        TrackChange(firstLine, lastLine);
        MarkChanged();
        return count;
    }

    /// <summary>替换从光标起的下一处匹配（单个），返回是否替换成功。支持正则捕获组与整词匹配。</summary>
    public bool ReplaceNext(string find, string replace, FindOptions opts = default)
    {
        if (string.IsNullOrEmpty(find)) return false;
        var (line, col, _) = FindMatch(find, Cy, Cx, opts);
        if (line < 0) return false;
        var regex = BuildFindRegex(find, opts);
        if (regex == null) return false;
        var replacement = ToReplacement(replace, opts.UseRegex);
        var text = Lines[line].ToString();
        var m = regex.Match(text, col);
        if (!m.Success) return false;
        var expanded = m.Result(replacement);
        // 替换结果含换行会破坏"一行一条目"不变量，且撤销会越界 —— 拒绝
        if (expanded.Contains('\n')) return false;
        var newLine = text[..m.Index] + expanded + text[(m.Index + m.Length)..];
        Record('R', line, m.Index, newLine, text); // 支持 Ctrl+Z 撤销单处替换
        Lines[line] = new StringBuilder(newLine);
        TrackChange(line, line);
        Cy = line; Cx = m.Index + expanded.Length;
        ClearSelection();
        MarkChanged();
        return true;
    }

    /// <summary>构造查找正则：正则原样使用，否则转义；整词匹配加 \b 边界。</summary>
    private static Regex? BuildFindRegex(string find, FindOptions opts)
    {
        try
        {
            var pattern = opts.UseRegex ? find : Regex.Escape(find);
            if (opts.WholeWord) pattern = $@"\b(?:{pattern})\b";
            var options = opts.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            return new Regex(pattern, options);
        }
        catch (ArgumentException)
        {
            return null; // 用户输入无效正则 → 视为无匹配，不崩溃
        }
    }

    /// <summary>字面替换把 $ 转义为 $$（避免被 .NET 正则当作反向引用）；正则替换保留 $1/${name}。</summary>
    private static string ToReplacement(string replace, bool useRegex)
        => useRegex ? replace : replace.Replace("$", "$$");

    // ================================================================
    // 括号匹配 / 光标处词
    // ================================================================

    /// <summary>
    /// 若 (line,col) 处字符为括号，返回其配对括号位置；否则返回 null。
    /// 支持 ()/[]/{} 跨行嵌套，跳过字符串字面量与行注释内的括号。
    /// </summary>
    public (int Line, int Col)? MatchingBracketAt(int line, int col)
    {
        if (line < 0 || line >= Lines.Count) return null;
        var text = Lines[line].ToString();
        if (col < 0 || col >= text.Length) return null;
        char c = text[col];
        if (!IsBracket(c)) return null;
        if (!CodeMask(text)[col]) return null;   // 字符串/注释内的括号不参与匹配

        bool forward = IsOpenBracket(c);
        char target = MatchingBracketOf(c);
        int depth = 0;

        if (forward)
        {
            for (int li = line; li < Lines.Count; li++)
            {
                var t = Lines[li].ToString();
                var code = CodeMask(t);
                int i = li == line ? col + 1 : 0;
                for (; i < t.Length; i++)
                {
                    if (!code[i]) continue;
                    char ch = t[i];
                    if (ch == c) depth++;
                    else if (ch == target) { if (depth == 0) return (li, i); depth--; }
                }
            }
        }
        else
        {
            for (int li = line; li >= 0; li--)
            {
                var t = Lines[li].ToString();
                var code = CodeMask(t);
                int i = li == line ? col - 1 : t.Length - 1;
                for (; i >= 0; i--)
                {
                    if (!code[i]) continue;
                    char ch = t[i];
                    if (ch == c) depth++;
                    else if (ch == target) { if (depth == 0) return (li, i); depth--; }
                }
            }
        }
        return null;
    }

    /// <summary>光标处的标识符词（字母/数字/下划线连续段），无词返回空串。</summary>
    public string WordAt(int line, int col)
    {
        if (line < 0 || line >= Lines.Count) return "";
        var text = Lines[line].ToString();
        if (col < 0 || col >= text.Length || !IsWordChar(text[col])) return "";
        int start = col;
        while (start > 0 && IsWordChar(text[start - 1])) start--;
        int end = col;
        while (end < text.Length && IsWordChar(text[end])) end++;
        return text[start..end];
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';
    private static bool IsBracket(char c) => c is '(' or ')' or '[' or ']' or '{' or '}';
    private static bool IsOpenBracket(char c) => c is '(' or '[' or '{';
    private static char MatchingBracketOf(char c) => c switch
    {
        '(' => ')', '[' => ']', '{' => '}',
        ')' => '(', ']' => '[', '}' => '{',
        _ => '\0',
    };

    /// <summary>构造 code-only 掩码：true=代码字符（括号参与匹配），false=字符串/行注释内字符。</summary>
    private static bool[] CodeMask(string line)
    {
        var mask = new bool[line.Length];
        Array.Fill(mask, true);
        int idx = 0;
        while (idx < line.Length)
        {
            char c = line[idx];
            if (c is '"' or '\'')
            {
                int end = SkipStringLiteral(line, idx, c);
                for (int j = idx; j < end && j < line.Length; j++) mask[j] = false;
                idx = end;
                continue;
            }
            if (c == '/' && idx + 1 < line.Length && line[idx + 1] == '/')
            {
                for (int j = idx; j < line.Length; j++) mask[j] = false;
                break;
            }
            idx++;
        }
        return mask;
    }

    /// <summary>从引号下标 i 向后跳过字符串字面量（处理反斜杠转义），返回闭合引号之后的下标；未闭合则到行尾。</summary>
    private static int SkipStringLiteral(string text, int i, char quote)
    {
        i++;
        while (i < text.Length)
        {
            if (text[i] == '\\') { i += 2; continue; }
            if (text[i] == quote) return i + 1;
            i++;
        }
        return text.Length;
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

    public int TotalLines => Lines.Count;

    public int TotalChars
    {
        get { if (_statsDirty) RecalcStats(); return _cachedTotalChars; }
    }

    public long FileSizeBytes
    {
        get { if (_statsDirty) RecalcStats(); return _cachedFileSize; }
    }

    private void RecalcStats()
    {
        _cachedTotalChars = Lines.Sum(l => l.Length);
        _cachedFileSize = Encoding.UTF8.GetByteCount(string.Join("\n", Lines.Select(l => l.ToString())));
        _statsDirty = false;
    }

    /// <summary>内容变更统一出口：置脏标记 + 触发 UI 刷新。</summary>
    private void MarkChanged()
    {
        Modified = true;
        _outlineDirty = true;
        _statsDirty = true;
        OnContentChanged?.Invoke();
        _changeStart = int.MaxValue;
        _changeEnd = -1;
    }

    // ── 变更范围追踪（供 UI 按行增量重绘：只刷新受影响的行）──
    private int _changeStart = int.MaxValue;
    private int _changeEnd = -1;

    /// <summary>最近一次内容变更涉及的缓冲区行范围 [Start, End]（含端点）；无变更为 null。</summary>
    public (int Start, int End)? LastChange =>
        _changeStart == int.MaxValue ? null : (_changeStart, _changeEnd);

    private void TrackChange(int start, int end)
    {
        if (start < _changeStart) _changeStart = start;
        if (end > _changeEnd) _changeEnd = end;
    }

    private static int CountLines(string s)
    {
        int n = 0;
        foreach (var c in s) if (c == '\n') n++;
        return n;
    }

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

                // 取第一个命名组（跳过整段匹配组 Group 0，而非按索引 0 判断——否则起始于第 0 列的名称组被误跳过）
                foreach (Group g in m.Groups)
                {
                    if (!g.Success || g == m.Groups[0] || string.IsNullOrEmpty(g.Value)) continue;
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
