using System.Text;

namespace CompilerBase;

/// <summary>
/// GCC 风格诊断收集器 — 统一收集编译错误/警告/提示。
/// 支持错误上限控制，避免级联错误淹没输出。
/// </summary>
public class DiagnosticBag
{
    private readonly List<CompilerError> _errors = new();
    private readonly List<CompilerError> _warnings = new();
    private readonly int _maxErrors;

    public DiagnosticBag(int maxErrors = 50)
    {
        _maxErrors = maxErrors;
    }

    public int ErrorCount => _errors.Count;
    public int WarningCount => _warnings.Count;
    public bool HasErrors => _errors.Count > 0;
    public IReadOnlyList<CompilerError> Errors => _errors;
    public IReadOnlyList<CompilerError> Warnings => _warnings;

    public void AddError(string file, int line, int col, ErrorCode code, string message,
                         string? hint = null, string? sourceLine = null)
    {
        if (_errors.Count >= _maxErrors)
        {
            if (_errors.Count == _maxErrors)
                _errors.Add(new CompilerError(file, line, col, DiagnosticLevel.Error,
                    ErrorCode.Unknown, "too many errors; stopping compilation"));
            return;
        }
        _errors.Add(new CompilerError(file, line, col, DiagnosticLevel.Error, code, message, hint, sourceLine));
    }

    public void AddWarning(string file, int line, int col, ErrorCode code, string message,
                           string? hint = null)
    {
        _warnings.Add(new CompilerError(file, line, col, DiagnosticLevel.Warning, code, message, hint));
    }

    /// <summary>GCC 风格完整输出</summary>
    public string FormatAll()
    {
        var sb = new StringBuilder();
        foreach (var e in _errors)
            sb.AppendLine(e.ToString());
        foreach (var w in _warnings)
            sb.AppendLine(w.ToString());

        if (_errors.Count > 0)
            sb.AppendLine($"{_errors.Count} error(s) generated.");
        if (_warnings.Count > 0)
            sb.AppendLine($"{_warnings.Count} warning(s) generated.");

        return sb.ToString();
    }

    public void Clear() { _errors.Clear(); _warnings.Clear(); }
}
