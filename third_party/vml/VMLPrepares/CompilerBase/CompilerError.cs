namespace CompilerBase;

/// <summary>诊断级别</summary>
public enum DiagnosticLevel
{
    Error,
    Warning,
    Note
}

/// <summary>编译器诊断信息 — GCC 风格格式化</summary>
public readonly record struct CompilerError(
    string File,
    int Line,
    int Column,
    DiagnosticLevel Level,
    ErrorCode Code,
    string Message,
    string? Hint = null,
    string? SourceLine = null
)
{
    public string LevelTag => Level switch
    {
        DiagnosticLevel.Error   => "error",
        DiagnosticLevel.Warning => "warning",
        DiagnosticLevel.Note    => "note",
        _ => "info"
    };

    /// <summary>GCC 风格位置: file:line:col: level: message</summary>
    /// <summary>
    /// GCC 风格位置 `file:line:col`；**位置未知时退化成只有文件名**。
    ///
    /// ⚠ 别在不知道的时候编一个 `:-1:0` 出来 —— 那不是 GCC 语法，
    ///   编辑器/IDE 的位置解析器（`WayCoder.Maui/Services/VmlDiagnostics` 那 4 条正则）
    ///   对 `line <= 0` 是**直接跳过**的，结果是**几条错误被合成一个气泡**，
    ///   正好把"一次多报"毁掉。`file: error: …`（不带位置）才是标准里"位置未知"的写法。
    /// </summary>
    public string LocationString =>
        Line <= 0
            ? (string.IsNullOrEmpty(File) ? "<input>" : File)
            : (string.IsNullOrEmpty(File) ? $"<input>:{Line}:{Column}" : $"{File}:{Line}:{Column}");

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        // 第1行: file:line:col: error: message [CODE]
        sb.Append(LocationString);
        sb.Append(": ");
        sb.Append(LevelTag);
        sb.Append(": ");
        sb.Append(Message);
        if (Code != ErrorCode.Unknown)
            sb.Append($" [{Code}]");
        sb.AppendLine();

        // 第2行: 源码行 (如果提供)
        if (SourceLine != null)
        {
            sb.Append("  ");
            sb.Append(Line.ToString().PadLeft(4));
            sb.Append(" | ");
            sb.AppendLine(SourceLine);

            // 第3行: 插入符号指示
            sb.Append("      | ");
            if (Column > 1)
                sb.Append(new string(' ', Column - 1));
            sb.AppendLine("^");
        }

        // 提示 (如果提供)
        if (Hint != null)
        {
            sb.AppendLine($"      | {Hint}");
        }

        return sb.ToString();
    }
}
