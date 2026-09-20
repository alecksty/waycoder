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

    /// <summary>
    /// 去重键。**同一条诊断被两条路径重复添加是常态**（C 前端四个 `throw` 点、
    /// Kotlin 的重复遍历、以及将来"收集并继续"之后同一句被扫到两次），
    /// 不去重的话 50 条上限会被同一条消息瞬间吃光，真正的其它错误反而被挤掉。
    /// 键里含 `message`：同一位置的两条**不同**消息都要留着。
    /// </summary>
    private readonly HashSet<(ErrorCode, string, int, int, string)> _seen = new();

    public DiagnosticBag(int maxErrors = 50)
    {
        _maxErrors = maxErrors;
    }

    public int ErrorCount => _errors.Count;
    public int WarningCount => _warnings.Count;
    public bool HasErrors => _errors.Count > 0;
    public IReadOnlyList<CompilerError> Errors => _errors;
    public IReadOnlyList<CompilerError> Warnings => _warnings;

    /// <summary>
    /// 错误数触到上限、后面的被**丢掉**了。
    ///
    /// ⚠ 原先达到上限时只是插一条哨兵然后静默 `return` —— 调用方**分不出**
    /// 「刚好 50 条」和「还有 200 条没报」。而"一次多报"这个特性里，
    /// 「被截断了」恰恰是用户必须知道的信息（否则他会以为只剩这些错）。
    /// </summary>
    public bool TooManyErrors { get; private set; }

    /// <summary>第一条错误的错误码 —— `CompilationException` 需要带一个 code。</summary>
    public ErrorCode FirstErrorCode => _errors.Count > 0 ? _errors[0].Code : ErrorCode.Unknown;

    public void AddError(string file, int line, int col, ErrorCode code, string message,
                         string? hint = null, string? sourceLine = null)
    {
        if (!_seen.Add((code, file, line, col, message))) return;   // 重复的，丢掉

        if (_errors.Count >= _maxErrors)
        {
            TooManyErrors = true;
            if (_errors.Count == _maxErrors)
                _errors.Add(new CompilerError(file, line, col, DiagnosticLevel.Error,
                    ErrorCode.Unknown, "错误太多，停止编译"));
            return;
        }
        _errors.Add(new CompilerError(file, line, col, DiagnosticLevel.Error, code, message, hint, sourceLine));
    }

    public void AddWarning(string file, int line, int col, ErrorCode code, string message,
                           string? hint = null)
    {
        if (!_seen.Add((code, file, line, col, message))) return;
        _warnings.Add(new CompilerError(file, line, col, DiagnosticLevel.Warning, code, message, hint));
    }

    /// <summary>
    /// 把另一个 bag 的**异常路径**诊断并进来。
    ///
    /// 用途：`CompileWithDiagnostics` 里，收集到的若干条 + 最后抛出来的那一条
    /// 属于同一个文件，要**一起**给用户（而不是"要么全给收集的、要么只给抛的那条"）。
    /// 走 `AddError` 而不是直接 AddRange —— 顺手享受去重与上限。
    /// </summary>
    public void Merge(DiagnosticBag other)
    {
        foreach (var e in other._errors)
        {
            if (e.Code == ErrorCode.Unknown && e.Message.StartsWith("错误太多")) continue;
            AddError(e.File, e.Line, e.Column, e.Code, e.Message, e.Hint);
        }
        foreach (var w in other._warnings)
            AddWarning(w.File, w.Line, w.Column, w.Code, w.Message, w.Hint);
        if (other.TooManyErrors) TooManyErrors = true;
    }

    /// <summary>GCC 风格完整输出</summary>
    public string FormatAll()
    {
        var sb = new StringBuilder();
        foreach (var e in _errors)
            sb.AppendLine(e.ToString());
        foreach (var w in _warnings)
            sb.AppendLine(w.ToString());

        if (TooManyErrors)
            sb.AppendLine($"（错误太多，只报了前 {_maxErrors} 条；修完这些再编一次）");
        if (_errors.Count > 0)
            sb.AppendLine($"生成了 {_errors.Count} 个错误。");
        if (_warnings.Count > 0)
            sb.AppendLine($"生成了 {_warnings.Count} 个警告。");

        return sb.ToString();
    }

    public void Clear() { _errors.Clear(); _warnings.Clear(); _seen.Clear(); TooManyErrors = false; }
}
