using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using CompilerBase;
using VMLPlugins;

namespace VMLTool;

/// <summary>
/// VML Language Server Protocol 基础实现。
/// 通过 stdin/stdout JSON-RPC 2.0 与编辑器通信，提供实时诊断。
/// 用法: vmltool --lsp
/// </summary>
public static class LspServer
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private static Dictionary<string, string> _documents = new();
    private static string? _rootUri;
    private static bool _shuttingDown;

    public static int Run()
    {
        // 重定向 stderr 到文件避免干扰 JSON-RPC 通信
        var logPath = Path.Combine(Path.GetTempPath(), "vml-lsp.log");
        using var logWriter = new StreamWriter(logPath, append: true) { AutoFlush = true };
        Console.SetError(logWriter);

        Log("VML Language Server started");

        try
        {
            var reader = new StreamReader(Console.OpenStandardInput());
            while (!_shuttingDown)
            {
                // 读取 Content-Length 头
                var header = reader.ReadLine();
                if (header == null) break; // EOF
                if (!header.StartsWith("Content-Length: ")) continue;

                if (!int.TryParse(header["Content-Length: ".Length..].Trim(), out int contentLength))
                    continue;

                // 跳过空行
                while (true)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrEmpty(line)) break;
                }

                // 读取消息体
                var buffer = new char[contentLength];
                int total = 0;
                while (total < contentLength)
                {
                    int read = reader.Read(buffer, total, contentLength - total);
                    if (read == 0) break;
                    total += read;
                }

                var message = new string(buffer, 0, total);
                var response = HandleMessage(message);
                if (response != null)
                    SendMessage(response);
            }
        }
        catch (Exception ex)
        {
            Log($"Fatal: {ex}");
            return 1;
        }

        return 0;
    }

    private static string? HandleMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var method = root.TryGetProperty("method", out var m) ? m.GetString() : null;
            var id = root.TryGetProperty("id", out var i) ? i.Clone() : default;

            if (method == null && id.ValueKind == JsonValueKind.Undefined)
                return null; // 无效消息

            Log($"→ {method ?? "(response)"}");

            return method switch
            {
                "initialize" => HandleInitialize(id, root),
                "initialized" => null, // 无需回复
                "textDocument/didOpen" => HandleDidOpen(root),
                "textDocument/didChange" => HandleDidChange(root),
                "textDocument/didClose" => HandleDidClose(root),
                "shutdown" => HandleShutdown(id),
                "exit" => HandleExit(),
                _ => id.ValueKind != JsonValueKind.Undefined
                    ? JsonSerializer.Serialize(new { jsonrpc = "2.0", id, error = new { code = -32601, message = $"Method not found: {method}" } }, _jsonOpts)
                    : null,
            };
        }
        catch (Exception ex)
        {
            Log($"Error handling message: {ex}");
            return null;
        }
    }

    private static string HandleInitialize(JsonElement id, JsonElement root)
    {
        var caps = root.GetProperty("params").TryGetProperty("capabilities", out var c)
            ? c.GetRawText() : "{}";
        _rootUri = root.GetProperty("params").TryGetProperty("rootUri", out var r)
            ? r.GetString() : null;

        var result = new
        {
            jsonrpc = "2.0",
            id,
            result = new
            {
                capabilities = new
                {
                    textDocumentSync = new { openClose = true, change = 1 }, // 全量同步
                    diagnosticProvider = new
                    {
                        interFileDependencies = false,
                        workspaceDiagnostics = false,
                    },
                },
                serverInfo = new
                {
                    name = "VML Language Server",
                    version = VersionInfo.Version,
                },
            },
        };

        return JsonSerializer.Serialize(result, _jsonOpts);
    }

    private static string? HandleDidOpen(JsonElement root)
    {
        var td = root.GetProperty("params").GetProperty("textDocument");
        var uri = td.GetProperty("uri").GetString()!;
        var text = td.GetProperty("text").GetString() ?? "";
        _documents[uri] = text;
        PublishDiagnostics(uri);
        return null;
    }

    private static string? HandleDidChange(JsonElement root)
    {
        var td = root.GetProperty("params").GetProperty("textDocument");
        var uri = td.GetProperty("uri").GetString()!;
        var changes = root.GetProperty("params").GetProperty("contentChanges");
        if (changes.GetArrayLength() > 0)
        {
            var text = changes[changes.GetArrayLength() - 1].GetProperty("text").GetString() ?? "";
            _documents[uri] = text;
        }
        PublishDiagnostics(uri);
        return null;
    }

    private static string? HandleDidClose(JsonElement root)
    {
        var uri = root.GetProperty("params").GetProperty("textDocument").GetProperty("uri").GetString()!;
        _documents.Remove(uri);
        // 清除该文件的诊断
        SendNotification("textDocument/publishDiagnostics", new { uri, diagnostics = Array.Empty<object>() });
        return null;
    }

    private static string HandleShutdown(JsonElement id)
    {
        return JsonSerializer.Serialize(new { jsonrpc = "2.0", id, result = (object?)null }, _jsonOpts);
    }

    private static string? HandleExit()
    {
        _shuttingDown = true;
        Log("Server exiting");
        return null;
    }

    /// <summary>对文档运行编译器诊断并通过 LSP 通知推送</summary>
    private static void PublishDiagnostics(string uri)
    {
        if (!_documents.TryGetValue(uri, out var source)) return;

        var filePath = UriToPath(uri);
        var diagnostics = new List<object>();

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

            if (lang != null)
            {
                var diag = new DiagnosticBag();
                try
                {
                    CompileForDiagnostics(lang, source, filePath, diag);
                }
                catch (CompilationException ex)
                {
                    ParseGccMessage(ex.Message, diagnostics);
                }

                foreach (var err in diag.Errors)
                {
                    diagnostics.Add(new
                    {
                        range = ToLspRange(err.Line, err.Column),
                        severity = err.Level switch
                        {
                            DiagnosticLevel.Error => 1,
                            DiagnosticLevel.Warning => 2,
                            DiagnosticLevel.Note => 3,
                            _ => 1,
                        },
                        code = err.Code.ToString(),
                        source = "vml",
                        message = err.Message,
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Diagnostics error: {ex.Message}");
        }

        SendNotification("textDocument/publishDiagnostics", new { uri, diagnostics });
    }

    private static void CompileForDiagnostics(string lang, string source, string filePath, DiagnosticBag diag)
    {
        // 使用 CompileWithDiagnostics 进行诊断收集
        CompilerHelper.CompileWithDiagnostics(filePath, _ =>
        {
            return lang switch
            {
                "c" => CCompiler.CCompiler.Compile(source),
                "basic" => BasicCompiler.BasicCompiler.Compile(source),
                "python" => PythonCompiler.PythonCompiler.Compile(source),
                "java" => new JavaCompiler.JavaCompiler().Compile(source),
                "csharp" => new CSharpCompiler.CSharpCompiler().Compile(source),
                "javascript" => new JavaScriptCompiler.JavaScriptCompiler().Compile(source),
                "swift" => new SwiftCompiler.SwiftCompiler().Compile(source),
                "cpp" => CppCompiler.CppCompiler.Compile(source),
                "go" => GoCompiler.GoCompiler.Compile(source),
                "rust" => RustCompiler.RustCompiler.Compile(source),
                "pascal" => PascalCompiler.PascalCompiler.Compile(source),
                "lua" => LuaCompiler.LuaCompiler.Compile(source),
                "kotlin" => KotlinCompiler.KotlinCompiler.Compile(source),
                "scheme" => SchemeCompiler.SchemeCompiler.Compile(source),
                "forth" => ForthCompiler.ForthCompiler.Compile(source),
                "ruby" => RubyCompiler.RubyCompiler.Compile(source),
                "dart" => DartCompiler.DartCompiler.Compile(source),
                "objc" => ObjCCompiler.ObjCCompiler.Compile(source),
                "r" => RCompiler.RCompiler.Compile(source),
                "d" => DCompiler.DCompiler.Compile(source),
                "fortran" => FortranCompiler.FortranCompiler.Compile(source),
                "ladder" => LadderCompiler.LadderCompiler.Compile(source),
                _ => throw new NotSupportedException($"LSP diagnostics for '{lang}' not supported")
            };
        });
    }

    private static void ParseGccMessage(string message, List<object> diagnostics)
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
                    int sev = 1; // error by default
                    if (msg.StartsWith("warning:")) { sev = 2; msg = msg[8..].Trim(); }
                    else if (msg.StartsWith("note:")) { sev = 3; msg = msg[5..].Trim(); }
                    else if (msg.StartsWith("error:")) { msg = msg[6..].Trim(); }
                    else if (msg.StartsWith("internal error:")) { msg = msg[15..].Trim(); }

                    diagnostics.Add(new
                    {
                        range = ToLspRange(ln, col),
                        severity = sev,
                        source = "vml",
                        message = msg,
                    });
                }
            }
        }
    }

    /// <summary>将 1-based 行/列转换为 LSP 0-based Range</summary>
    private static object ToLspRange(int line1, int col1)
    {
        int l = Math.Max(0, line1 - 1);
        int c = Math.Max(0, col1 - 1);
        return new
        {
            start = new { line = l, character = c },
            end = new { line = l, character = c + 1 },
        };
    }

    private static string UriToPath(string uri)
    {
        if (uri.StartsWith("file://"))
            return Uri.UnescapeDataString(uri[7..]);
        return uri;
    }

    private static void SendMessage(string json)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var header = $"Content-Length: {bytes.Length}\r\n\r\n";
        var headerBytes = System.Text.Encoding.UTF8.GetBytes(header);

        using var stdout = Console.OpenStandardOutput();
        stdout.Write(headerBytes, 0, headerBytes.Length);
        stdout.Write(bytes, 0, bytes.Length);
        stdout.Flush();
    }

    private static void SendNotification(string method, object @params)
    {
        var notification = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            method,
            @params,
        }, _jsonOpts);
        SendMessage(notification);
    }

    private static void Log(string msg)
    {
        Console.Error.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {msg}");
    }
}
