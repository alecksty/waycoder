using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VMLTool;

/// <summary>
/// VML 诊断命令 — JSON 格式诊断输出，供编辑器/LSP 集成使用。
/// 用法: vmltool --diagnostics input.c
/// 输出: {"file":"input.c","diagnostics":[{"line":12,"column":34,"severity":"error","message":"..."}]}
/// </summary>
public static class DiagnosticsCommand
{
    public class DiagnosticResult
    {
        [JsonPropertyName("file")] public string File { get; set; } = "";
        [JsonPropertyName("diagnostics")] public System.Collections.Generic.List<DiagnosticEntry> Items { get; set; } = new();
    }

    public class DiagnosticEntry
    {
        [JsonPropertyName("line")] public int Line { get; set; }
        [JsonPropertyName("column")] public int Column { get; set; }
        [JsonPropertyName("severity")] public string Severity { get; set; } = "error";
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; } = "";
    }

    public static int Run(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: vmltool --diagnostics <file>");
            return 1;
        }

        var filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.WriteLine(JsonSerializer.Serialize(new { error = "file not found" }));
            return 1;
        }

        var result = new DiagnosticResult { File = filePath };

        try
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            var lang = ext switch
            {
                ".c" => "c", ".bas" or ".qbs" => "basic", ".py" => "python",
                ".lua" => "lua", ".go" => "go", ".rs" => "rust", ".pas" => "pascal",
                ".cpp" or ".cc" => "cpp", ".java" => "java", ".cs" => "csharp",
                ".js" => "javascript", ".swift" => "swift", ".kt" => "kotlin",
                ".f90" or ".f" => "fortran", ".d" => "d", ".dart" => "dart",
                ".m" => "objc", ".r" => "r", ".rb" => "ruby",
                ".scm" or ".ss" => "scheme", ".fth" => "forth", ".ld" => "ladder",
                _ => null
            };

            if (lang == null)
            {
                result.Items.Add(new DiagnosticEntry { Line = 0, Column = 0, Severity = "error", Message = $"Unsupported extension: {ext}" });
            }
            else
            {
                try
                {
                    CompilerBase.CompilerHelper.CompileWithDiagnostics(filePath, _ =>
                    {
                        return lang switch
                        {
                            "c" => CCompiler.CCompiler.CompileFile(filePath),
                            "basic" => BasicCompiler.BasicCompiler.CompileFile(filePath),
                            "python" => PythonCompiler.PythonCompiler.CompileFile(filePath),
                            "java" => JavaCompiler.JavaCompiler.CompileFile(filePath),
                            "csharp" => CSharpCompiler.CSharpCompiler.CompileFile(filePath),
                            "javascript" => JavaScriptCompiler.JavaScriptCompiler.CompileFile(filePath),
                            "swift" => SwiftCompiler.SwiftCompiler.CompileFile(filePath),
                            "cpp" => CppCompiler.CppCompiler.CompileFile(filePath),
                            "go" => GoCompiler.GoCompiler.CompileFile(filePath),
                            "rust" => RustCompiler.RustCompiler.CompileFile(filePath),
                            "pascal" => PascalCompiler.PascalCompiler.CompileFile(filePath),
                            "lua" => LuaCompiler.LuaCompiler.CompileFile(filePath),
                            "kotlin" => KotlinCompiler.KotlinCompiler.CompileFile(filePath),
                            "scheme" => SchemeCompiler.SchemeCompiler.CompileFile(filePath),
                            "forth" => ForthCompiler.ForthCompiler.CompileFile(filePath),
                            "ruby" => RubyCompiler.RubyCompiler.CompileFile(filePath),
                            "dart" => DartCompiler.DartCompiler.CompileFile(filePath),
                            "objc" => ObjCCompiler.ObjCCompiler.CompileFile(filePath),
                            "r" => RCompiler.RCompiler.CompileFile(filePath),
                            "d" => DCompiler.DCompiler.CompileFile(filePath),
                            "fortran" => FortranCompiler.FortranCompiler.CompileFile(filePath),
                            "ladder" => LadderCompiler.LadderCompiler.CompileFile(filePath),
                            _ => throw new NotSupportedException($"Diagnostics for '{lang}' not yet wired")
                        };
                    });
                }
                catch (CompilerBase.CompilationException ex)
                {
                    ParseGccDiagnostics(ex.Message, result);
                }
                catch (NotSupportedException ex)
                {
                    result.Items.Add(new DiagnosticEntry { Line = 0, Column = 0, Severity = "info", Message = ex.Message });
                }
            }
        }
        catch (Exception ex)
        {
            result.Items.Add(new DiagnosticEntry { Line = 0, Column = 0, Severity = "error", Message = $"Internal: {ex.Message}" });
        }

        Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        return result.Items.Count > 0 ? 1 : 0;
    }

    private static void ParseGccDiagnostics(string message, DiagnosticResult result)
    {
        foreach (var line in message.Split('\n'))
        {
            var t = line.Trim();
            if (string.IsNullOrEmpty(t) || t.Contains("个错误。")) continue;

            var c1 = t.IndexOf(':');
            if (c1 < 0) continue;
            var c2 = t.IndexOf(':', c1 + 1);
            if (c2 < 0) continue;

            if (int.TryParse(t[(c1 + 1)..c2], out int ln))
            {
                var rest = t[(c2 + 1)..].TrimStart();
                var c3 = rest.IndexOf(':');
                if (c3 < 0) continue;
                if (int.TryParse(rest[..c3], out int col))
                {
                    var msg = rest[(c3 + 1)..].TrimStart();
                    var sev = "error";
                    if (msg.StartsWith("error:")) { sev = "error"; msg = msg[6..].Trim(); }
                    else if (msg.StartsWith("warning:")) { sev = "warning"; msg = msg[8..].Trim(); }
                    else if (msg.StartsWith("note:")) { sev = "info"; msg = msg[5..].Trim(); }
                    else if (msg.StartsWith("internal error:")) { sev = "error"; msg = msg[15..].Trim(); }

                    result.Items.Add(new DiagnosticEntry { Line = ln, Column = col, Severity = sev, Message = msg });
                }
            }
        }
    }
}
