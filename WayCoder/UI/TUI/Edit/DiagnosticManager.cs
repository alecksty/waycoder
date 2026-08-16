using System.Text.RegularExpressions;
using WayCoder.Tools;

namespace WayCoder.UI.Tui.Edit;

/// <summary>
/// 诊断严重级别
/// </summary>
public enum Severity { Error, Warning, Info }

/// <summary>
/// 单条诊断信息
/// </summary>
public record Diagnostic(int Line, int Column, Severity Severity, string Message, string? Code);

/// <summary>
/// TUI 编辑器 Lint 诊断管理器 —— 保存时运行 lint，解析输出为结构化诊断，
/// 供编辑器渲染行内错误标注和状态栏信息。
/// </summary>
public static class DiagnosticManager
{
    /// <summary>每个文件的诊断结果缓存</summary>
    private static readonly Dictionary<string, List<Diagnostic>> _diagnostics = [];

    /// <summary>是否启用自动 lint（由 Config.EditorLint 控制）</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>
    /// 运行 lint 并解析输出为结构化诊断列表。
    /// </summary>
    public static async Task<List<Diagnostic>> RunLintAsync(string filePath)
    {
        if (!Enabled)
        {
            _diagnostics.Remove(filePath);
            return [];
        }

        try
        {
            var lang = LintTool.DetectLanguage(filePath);
            if (lang == null)
            {
                _diagnostics.Remove(filePath);
                return [];
            }

            // 直接运行 linter（复用 LintTool 内部的 RunLinter 逻辑）
            var lintTool = new LintTool();
            var rawResult = await lintTool.ExecuteAsync(new Dictionary<string, object?>
            {
                ["path"] = filePath,
            });

            var diagnostics = ParseLintOutput(rawResult, lang, filePath);
            _diagnostics[filePath] = diagnostics;
            return diagnostics;
        }
        catch
        {
            _diagnostics.Remove(filePath);
            return [];
        }
    }

    /// <summary>
    /// 获取指定文件某行的所有诊断（1-based 行号）。
    /// </summary>
    public static List<Diagnostic> GetForLine(string filePath, int line)
    {
        if (!_diagnostics.TryGetValue(filePath, out var list))
            return [];
        return list.Where(d => d.Line == line).ToList();
    }

    /// <summary>
    /// 获取文件诊断汇总。
    /// </summary>
    public static (int errors, int warnings) GetSummary(string filePath)
    {
        if (!_diagnostics.TryGetValue(filePath, out var list))
            return (0, 0);
        return (
            list.Count(d => d.Severity == Severity.Error),
            list.Count(d => d.Severity == Severity.Warning)
        );
    }

    /// <summary>
    /// 格式化诊断信息供 LLM 使用（紧凑文本格式）。
    /// 仅当缓存中有诊断时返回非空字符串。
    /// </summary>
    public static string? FormatForLLM(string filePath)
    {
        if (!_diagnostics.TryGetValue(filePath, out var list) || list.Count == 0)
            return null;

        var fileName = Path.GetFileName(filePath);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📋 **诊断信息 ({fileName})：**");

        var errors = list.Where(d => d.Severity == Severity.Error).Take(8).ToList();
        var warnings = list.Where(d => d.Severity == Severity.Warning).Take(8).ToList();

        foreach (var d in errors)
            sb.AppendLine($"  ❌ 行 {d.Line}: {d.Message}{(d.Code != null ? $" [{d.Code}]" : "")}");
        foreach (var d in warnings)
            sb.AppendLine($"  ⚠ 行 {d.Line}: {d.Message}{(d.Code != null ? $" [{d.Code}]" : "")}");

        var totalErrors = list.Count(d => d.Severity == Severity.Error);
        var totalWarnings = list.Count(d => d.Severity == Severity.Warning);
        if (totalErrors > 8 || totalWarnings > 8)
            sb.AppendLine($"  ... 及其他（共 {totalErrors} 错误, {totalWarnings} 警告）");

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// 带超时的 lint 运行（用于工具集成）。
    /// 在超时内完成的 lint 结果会更新到缓存。
    /// </summary>
    public static async Task<string?> TryRunLintWithTimeout(string filePath, int timeoutMs = 3000)
    {
        try
        {
            using var cts = new CancellationTokenSource(timeoutMs);
            var lintTask = RunLintAsync(filePath);
            var completed = await Task.WhenAny(lintTask, Task.Delay(timeoutMs, cts.Token));

            if (completed == lintTask)
            {
                await lintTask; // 等待完成以获取异常
                return FormatForLLM(filePath);
            }

            // 超时：不等待
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 清除指定文件的诊断缓存。
    /// </summary>
    public static void Clear(string filePath)
    {
        _diagnostics.Remove(filePath);
    }

    /// <summary>
    /// 清除所有诊断缓存。
    /// </summary>
    public static void ClearAll()
    {
        _diagnostics.Clear();
    }

    // ================================================================
    // 解析层：将各语言 linter 输出解析为 Diagnostic 列表
    // ================================================================

    /// <summary>
    /// 根据语言解析 lint 原始输出为结构化诊断。
    /// </summary>
    internal static List<Diagnostic> ParseLintOutput(string rawOutput, string lang, string filePath)
    {
        var diagnostics = new List<Diagnostic>();

        // 只处理发现问题的情况（跳过通过/无 linter 的情况）
        if (rawOutput.StartsWith("✅") || rawOutput.StartsWith("⚠") || rawOutput.Contains("（无可用 linter）"))
            return diagnostics;

        var fileName = Path.GetFileName(filePath);

        switch (lang)
        {
            case "cs":
                ParseDotnetBuild(rawOutput, fileName, diagnostics);
                break;
            case "py":
                ParseRuff(rawOutput, fileName, diagnostics);
                break;
            case "js":
            case "ts":
            case "vue":
                ParseEslint(rawOutput, diagnostics);
                break;
            case "go":
                ParseGoVet(rawOutput, fileName, diagnostics);
                break;
            case "rs":
                ParseRustCargo(rawOutput, fileName, diagnostics);
                break;
            case "c":
            case "cpp":
                ParseGcc(rawOutput, fileName, diagnostics);
                break;
            case "shell":
                ParseShellcheck(rawOutput, diagnostics);
                break;
            case "ruby":
                ParseRuby(rawOutput, diagnostics);
                break;
            case "php":
                ParsePhp(rawOutput, diagnostics);
                break;
            case "java":
            case "kotlin":
                ParseJava(rawOutput, diagnostics);
                break;
            default:
                // 通用回退：按 `file:line:col:` 格式解析
                ParseGeneric(rawOutput, fileName, diagnostics);
                break;
        }

        return diagnostics;
    }

    // ---- dotnet build ----
    // 格式: file(line,col): error CS1234: message
    //       file(line,col): warning CS1234: message
    private static readonly Regex DotnetRx = new(
        @"^(.+?)\((\d+),(\d+)\):\s*(error|warning)\s+([A-Z]{2}\d+):\s*(.+)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static void ParseDotnetBuild(string output, string fileName, List<Diagnostic> diagnostics)
    {
        foreach (Match m in DotnetRx.Matches(output))
        {
            var file = m.Groups[1].Value.Trim();
            if (!file.EndsWith(fileName, StringComparison.OrdinalIgnoreCase)
                && !file.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                continue;

            var line = int.TryParse(m.Groups[2].Value, out var l) ? l : 0;
            var col = int.TryParse(m.Groups[3].Value, out var c) ? c : 0;
            var sev = m.Groups[4].Value == "error" ? Severity.Error : Severity.Warning;
            var code = m.Groups[5].Value;
            var msg = m.Groups[6].Value.Trim();

            diagnostics.Add(new Diagnostic(line, col, sev, msg, code));
        }
    }

    // ---- eslint ----
    // 格式: /path/to/file
    //         line:col  error/warning  message  rule-name
    private static readonly Regex EslintRx = new(
        @"^\s+(\d+):(\d+)\s+(error|warning)\s+(.+?)\s{2,}(\S+)\s*$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static void ParseEslint(string output, List<Diagnostic> diagnostics)
    {
        foreach (Match m in EslintRx.Matches(output))
        {
            var line = int.TryParse(m.Groups[1].Value, out var l) ? l : 0;
            var col = int.TryParse(m.Groups[2].Value, out var c) ? c : 0;
            var sev = m.Groups[3].Value == "error" ? Severity.Error : Severity.Warning;
            var msg = m.Groups[4].Value.Trim();
            var code = m.Groups[5].Value.Trim();

            diagnostics.Add(new Diagnostic(line, col, sev, msg, code));
        }
    }

    // ---- ruff ----
    // 格式: file:line:col: CODE message
    private static readonly Regex RuffRx = new(
        @"^.+?:(\d+):(\d+):\s*([A-Z]+\d*)\s+(.+)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static void ParseRuff(string output, string fileName, List<Diagnostic> diagnostics)
    {
        foreach (Match m in RuffRx.Matches(output))
        {
            var line = int.TryParse(m.Groups[1].Value, out var l) ? l : 0;
            var col = int.TryParse(m.Groups[2].Value, out var c) ? c : 0;
            var code = m.Groups[3].Value;
            var msg = m.Groups[4].Value.Trim();

            diagnostics.Add(new Diagnostic(line, col, Severity.Warning, msg, code));
        }
    }

    // ---- go vet ----
    // 格式: file:line:col: message
    private static readonly Regex GoVetRx = new(
        @"^.+?:(\d+):(\d+):\s+(.+)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static void ParseGoVet(string output, string fileName, List<Diagnostic> diagnostics)
    {
        foreach (Match m in GoVetRx.Matches(output))
        {
            var line = int.TryParse(m.Groups[1].Value, out var l) ? l : 0;
            var col = int.TryParse(m.Groups[2].Value, out var c) ? c : 0;
            var msg = m.Groups[3].Value.Trim();

            diagnostics.Add(new Diagnostic(line, col, Severity.Error, msg, null));
        }
    }

    // ---- Rust cargo check ----
    // 格式: error[E0001]: message
    //         --> file:line:col
    private static readonly Regex RustErrRx = new(
        @"^(error|warning)\[([A-Z]\d+)\]:\s+(.+)$",
        RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex RustLocRx = new(
        @"-->\s+.+?:(\d+):(\d+)",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static void ParseRustCargo(string output, string fileName, List<Diagnostic> diagnostics)
    {
        var errMatches = RustErrRx.Matches(output);
        var locMatches = RustLocRx.Matches(output);

        for (int i = 0; i < Math.Min(errMatches.Count, locMatches.Count); i++)
        {
            var errM = errMatches[i];
            var locM = locMatches[i];

            var sev = errM.Groups[1].Value == "error" ? Severity.Error : Severity.Warning;
            var code = errM.Groups[2].Value;
            var msg = errM.Groups[3].Value.Trim();
            var line = int.TryParse(locM.Groups[1].Value, out var l) ? l : 0;
            var col = int.TryParse(locM.Groups[2].Value, out var c) ? c : 0;

            diagnostics.Add(new Diagnostic(line, col, sev, msg, code));
        }
    }

    // ---- gcc / g++ ----
    // 格式: file:line:col: error: message
    //       file:line:col: warning: message
    private static readonly Regex GccRx = new(
        @"^.+?:(\d+):(\d+):\s*(error|warning):\s*(.+)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static void ParseGcc(string output, string fileName, List<Diagnostic> diagnostics)
    {
        foreach (Match m in GccRx.Matches(output))
        {
            var line = int.TryParse(m.Groups[1].Value, out var l) ? l : 0;
            var col = int.TryParse(m.Groups[2].Value, out var c) ? c : 0;
            var sev = m.Groups[3].Value == "error" ? Severity.Error : Severity.Warning;
            var msg = m.Groups[4].Value.Trim();

            diagnostics.Add(new Diagnostic(line, col, sev, msg, null));
        }
    }

    // ---- shellcheck ----
    // 格式: In /path/file line XX:
    //       message
    private static readonly Regex ShellRx = new(
        @"^In\s+.+?\s+line\s+(\d+):",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static void ParseShellcheck(string output, List<Diagnostic> diagnostics)
    {
        var matches = ShellRx.Matches(output);
        for (int i = 0; i < matches.Count; i++)
        {
            var m = matches[i];
            var line = int.TryParse(m.Groups[1].Value, out var l) ? l : 0;

            // 下一行是消息
            var msgStart = m.Index + m.Length;
            var msgEnd = i + 1 < matches.Count ? matches[i + 1].Index : output.Length;
            var msg = output[msgStart..msgEnd].Trim();
            if (msg.StartsWith("^--")) msg = "语法错误";

            diagnostics.Add(new Diagnostic(line, 0, Severity.Warning, msg.Trim(), "SC"));
        }
    }

    // ---- ruby -c ----
    // 格式: file:line: syntax error, ...
    private static readonly Regex RubyRx = new(
        @"^.+?:(\d+):\s*(.+)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static void ParseRuby(string output, List<Diagnostic> diagnostics)
    {
        foreach (Match m in RubyRx.Matches(output))
        {
            if (output.Contains("Syntax OK")) continue;

            var line = int.TryParse(m.Groups[1].Value, out var l) ? l : 0;
            var msg = m.Groups[2].Value.Trim();
            if (msg.Length == 0) continue;

            diagnostics.Add(new Diagnostic(line, 0, Severity.Error, msg, null));
        }
    }

    // ---- php -l ----
    // 格式: Parse error: ... in file on line N
    //       error: ... on line N
    private static readonly Regex PhpRx = new(
        @"(?:Parse )?(?:error|warning):\s*(.+?)\s*(?:in\s+.+?\s+)?on\s+line\s+(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    private static void ParsePhp(string output, List<Diagnostic> diagnostics)
    {
        foreach (Match m in PhpRx.Matches(output))
        {
            var line = int.TryParse(m.Groups[2].Value, out var l) ? l : 0;
            var msg = m.Groups[1].Value.Trim();
            var sev = output.Contains("warning") ? Severity.Warning : Severity.Error;

            diagnostics.Add(new Diagnostic(line, 0, sev, msg, null));
        }
    }

    // ---- Java / Kotlin (javac / gradle / kotlinc) ----
    // javac: file:line: error: message
    // gradle: file:line: error: message
    private static readonly Regex JavaRx = new(
        @"^.+?:(\d+):\s*(error|warning):\s*(.+)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static void ParseJava(string output, List<Diagnostic> diagnostics)
    {
        foreach (Match m in JavaRx.Matches(output))
        {
            var line = int.TryParse(m.Groups[1].Value, out var l) ? l : 0;
            var sev = m.Groups[2].Value == "error" ? Severity.Error : Severity.Warning;
            var msg = m.Groups[3].Value.Trim();

            diagnostics.Add(new Diagnostic(line, 0, sev, msg, null));
        }
    }

    // ---- 通用回退 ----
    // 匹配任何 `file:line:col:` 或 `file:line:` 格式
    private static readonly Regex GenericRx = new(
        @"^.+?:(\d+)(?::(\d+))?:\s*(error|warning|ERROR|WARNING)?:?\s*(.+)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static void ParseGeneric(string output, string fileName, List<Diagnostic> diagnostics)
    {
        foreach (Match m in GenericRx.Matches(output))
        {
            var line = int.TryParse(m.Groups[1].Value, out var l) ? l : 0;
            var col = int.TryParse(m.Groups[2].Value, out var c) ? c : 0;
            var sev = m.Groups[3].Value.ToLowerInvariant() switch
            {
                "error" => Severity.Error,
                "warning" => Severity.Warning,
                _ => Severity.Error,
            };
            var msg = m.Groups[4].Value.Trim();

            diagnostics.Add(new Diagnostic(line, col, sev, msg, null));
        }
    }
}
