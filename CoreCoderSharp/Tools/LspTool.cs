using System.Diagnostics;
using System.Text;

namespace CoreCoderSharp.Tools;

/// <summary>
/// LSP 代码导航工具 —— 通过 Language Server Protocol 提供代码智能。
/// 支持 go-to-definition、find-references、hover、document-symbols。
/// 自动检测项目语言并启动对应的 LSP 服务器。
/// </summary>
public class LspTool : ITool
{
    public string Name => "lsp";
    public string Description => "代码智能导航：跳转定义(definition)、查找引用(references)、类型悬停(hover)、文档符号(symbols)。支持 C#/Python/JS/TS/Go/Rust。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["action"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "操作: definition | references | hover | symbols",
            },
            ["file_path"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "文件路径",
            },
            ["line"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "行号 (1-based)",
            },
            ["character"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "列号 (1-based)",
            },
            ["query"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "符号搜索关键词 (symbols 操作时用)",
            },
        },
        ["required"] = new JsonArray("action", "file_path", "line", "character"),
    };

    /// <summary>支持的语言服务器列表（供 UI 展示）</summary>
    public static IReadOnlyDictionary<string, (string Command, string[] Args)> SupportedServers => ServerConfigs;

    // 已知的语言服务器配置
    private static readonly Dictionary<string, (string Command, string[] Args)> ServerConfigs = new()
    {
        ["csharp"] = ("dotnet", ["tool", "run", "--project", ".", "csharp-ls"]),
        ["python"] = ("pyright-langserver", ["--stdio"]),
        ["typescript"] = ("typescript-language-server", ["--stdio"]),
        ["go"] = ("gopls", []),
        ["rust"] = ("rust-analyzer", []),
    };

    // 扩展名 -> 语言 ID 映射
    private static readonly Dictionary<string, string> ExtToLang = new()
    {
        [".cs"] = "csharp",
        [".py"] = "python",
        [".ts"] = "typescript", [".tsx"] = "typescript",
        [".js"] = "typescript", [".jsx"] = "typescript",
        [".go"] = "go",
        [".rs"] = "rust",
    };

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var action = arguments.GetValueOrDefault("action")?.ToString() ?? "hover";
        var filePath = arguments.GetValueOrDefault("file_path")?.ToString() ?? "";
        var line = arguments.TryGetValue("line", out var l) && l is int li ? li : 1;
        var character = arguments.TryGetValue("character", out var c) && c is int ci ? ci : 1;
        var query = arguments.GetValueOrDefault("query")?.ToString() ?? "";

        return await Execute(action, filePath, line, character, query);
    }

    private static async Task<string> Execute(string action, string filePath, int line, int charPos, string query)
    {
        if (!File.Exists(filePath))
            return $"错误：文件不存在 - {filePath}";

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var config = FindServer(ext);
        if (config == null)
            return $"错误：未找到 {ext} 的 LSP 服务器。已安装? (支持: {string.Join(", ", ServerConfigs.Keys)})";

        try
        {
            var proc = StartServer(config.Value.Command, config.Value.Args, filePath);
            if (proc == null) return $"错误：无法启动 LSP 服务器 ({config.Value.Command})";

            using (proc)
            {
                await Task.Delay(500); // 等待初始化
                var result = action switch
                {
                    "definition" => await GoToDefinition(proc, filePath, line, charPos),
                    "references" => await FindReferences(proc, filePath, line, charPos),
                    "hover" => await Hover(proc, filePath, line, charPos),
                    "symbols" => await DocumentSymbols(proc, filePath, query),
                    _ => "错误：未知操作",
                };
                return result;
            }
        }
        catch (Exception ex)
        {
            return $"LSP 错误：{ex.GetType().Name}: {ex.Message}";
        }
    }

    private static (string Command, string[] Args)? FindServer(string ext)
    {
        if (ExtToLang.TryGetValue(ext, out var lang) && ServerConfigs.TryGetValue(lang, out var cfg))
            return cfg;
        return null;
    }

    private static Process? StartServer(string command, string[] args, string rootFile)
    {
        try
        {
            var root = Path.GetDirectoryName(rootFile) ?? ".";
            // 向上查找项目根目录
            while (root != null && !Directory.GetFiles(root, "*.sln").Any()
                   && !Directory.GetFiles(root, "*.csproj").Any()
                   && !File.Exists(Path.Combine(root, "package.json"))
                   && !File.Exists(Path.Combine(root, "go.mod"))
                   && !File.Exists(Path.Combine(root, "Cargo.toml"))
                   && !File.Exists(Path.Combine(root, "pyproject.toml")))
            {
                var parent = Path.GetDirectoryName(root);
                if (parent == root) break;
                root = parent;
            }

            var allArgs = new List<string>(args) { root! };
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = string.Join(" ", allArgs),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            return Process.Start(psi);
        }
        catch
        {
            return null;
        }
    }

    // ---- LSP 消息构造 ----

    private static async Task<string> GoToDefinition(Process proc, string file, int line, int ch)
    {
        var req = BuildRequest("textDocument/definition", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = FileToUri(file) },
            ["position"] = new JsonObject { ["line"] = line - 1, ["character"] = ch - 1 },
        });
        Initialize(proc, file);
        SendMessage(proc, req);
        var resp = await ReadResponse(proc);
        return FormatLocationResult(resp, "定义");
    }

    private static async Task<string> FindReferences(Process proc, string file, int line, int ch)
    {
        var req = BuildRequest("textDocument/references", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = FileToUri(file) },
            ["position"] = new JsonObject { ["line"] = line - 1, ["character"] = ch - 1 },
            ["context"] = new JsonObject { ["includeDeclaration"] = true },
        });
        Initialize(proc, file);
        SendMessage(proc, req);
        var resp = await ReadResponse(proc);
        return FormatLocationResult(resp, "引用");
    }

    private static async Task<string> Hover(Process proc, string file, int line, int ch)
    {
        var req = BuildRequest("textDocument/hover", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = FileToUri(file) },
            ["position"] = new JsonObject { ["line"] = line - 1, ["character"] = ch - 1 },
        });
        Initialize(proc, file);
        SendMessage(proc, req);
        var resp = await ReadResponse(proc);
        if (resp?["result"]?["contents"]?["value"]?.GetValue<string>() is { } text)
            return text;
        if (resp?["result"]?.GetValue<string>() is { } str)
            return str;
        return "（无类型信息）";
    }

    private static async Task<string> DocumentSymbols(Process proc, string file, string query)
    {
        var req = BuildRequest("textDocument/documentSymbol", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = FileToUri(file) },
        });
        Initialize(proc, file);
        SendMessage(proc, req);
        var resp = await ReadResponse(proc);

        var symbols = resp?["result"]?.AsArray();
        if (symbols == null || symbols.Count == 0) return "（无符号）";

        var lines = new List<string>();
        FormatSymbols(symbols, lines, 0, query);
        return lines.Count > 0 ? string.Join("\n", lines) : "（无匹配符号）";
    }

    // ---- LSP 协议辅助 ----

    private static int _msgId;
    private static string BuildRequest(string method, JsonObject @params)
    {
        var id = Interlocked.Increment(ref _msgId);
        var msg = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = method,
            ["params"] = @params,
        };
        return $"Content-Length: {Encoding.UTF8.GetByteCount(msg.ToJsonString())}\r\n\r\n{msg.ToJsonString()}";
    }

    private static void Initialize(Process proc, string rootFile)
    {
        var root = Path.GetDirectoryName(rootFile) ?? ".";
        var initReq = BuildRequest("initialize", new JsonObject
        {
            ["processId"] = Environment.ProcessId,
            ["rootUri"] = FileToUri(root),
            ["capabilities"] = new JsonObject(),
        });
        SendMessage(proc, initReq);
        ReadResponse(proc).Wait();
        // 发送 initialized 通知
        var notif = new JsonObject { ["jsonrpc"] = "2.0", ["method"] = "initialized", ["params"] = new JsonObject() };
        var notifStr = $"Content-Length: {Encoding.UTF8.GetByteCount(notif.ToJsonString())}\r\n\r\n{notif.ToJsonString()}";
        SendMessage(proc, notifStr);
        ReadResponse(proc).Wait();

        // 打开文档
        var didOpen = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"] = "textDocument/didOpen",
            ["params"] = new JsonObject
            {
                ["textDocument"] = new JsonObject
                {
                    ["uri"] = FileToUri(rootFile),
                    ["languageId"] = GetLanguageId(rootFile),
                    ["version"] = 1,
                    ["text"] = File.ReadAllText(rootFile),
                },
            },
        };
        var openStr = $"Content-Length: {Encoding.UTF8.GetByteCount(didOpen.ToJsonString())}\r\n\r\n{didOpen.ToJsonString()}";
        SendMessage(proc, openStr);
        ReadResponse(proc).Wait();
    }

    private static void SendMessage(Process proc, string message)
    {
        try { proc.StandardInput.Write(message); proc.StandardInput.Flush(); }
        catch { /* 忽略写入错误 */ }
    }

    private static async Task<JsonNode?> ReadResponse(Process proc)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            // 读 Content-Length 头
            var header = "";
            while (!header.EndsWith("\r\n\r\n"))
            {
                var ch = (char)proc.StandardOutput.Read();
                header += ch;
                if (header.Length > 200) break;
            }
            if (!header.StartsWith("Content-Length:")) return null;

            var lenStr = header["Content-Length:".Length..].Split('\r')[0].Trim();
            if (!int.TryParse(lenStr, out var len)) return null;

            var buffer = new char[len];
            var read = await proc.StandardOutput.ReadBlockAsync(buffer, 0, len);
            return JsonNode.Parse(new string(buffer, 0, read));
        }
        catch { return null; }
    }

    private static string FileToUri(string path)
    {
        return "file:///" + Path.GetFullPath(path).Replace('\\', '/').TrimStart('/');
    }

    private static string GetLanguageId(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".cs" => "csharp",
            ".py" => "python",
            ".ts" or ".tsx" => "typescript",
            ".js" or ".jsx" => "javascript",
            ".go" => "go",
            ".rs" => "rust",
            _ => "plaintext",
        };
    }

    private static string FormatLocationResult(JsonNode? resp, string label)
    {
        var result = resp?["result"];
        if (result == null) return $"（无{label}）";

        if (result is JsonArray arr)
        {
            var lines = new List<string> { $"{label} ({arr.Count} 处):" };
            foreach (var loc in arr.Take(20))
            {
                var uri = loc?["uri"]?.GetValue<string>() ?? "?";
                var range = loc?["range"];
                var startLine = (int?)range?["start"]?["line"] ?? 0;
                var startCh = (int?)range?["start"]?["character"] ?? 0;
                lines.Add($"  {UriToPath(uri)}:{startLine + 1}:{startCh + 1}");
            }
            if (arr.Count > 20) lines.Add($"  ... 还有 {arr.Count - 20} 处");
            return string.Join("\n", lines);
        }

        if (result is JsonObject obj && obj["uri"] != null)
        {
            var uri = obj["uri"]?.GetValue<string>() ?? "?";
            var range = obj["range"];
            var sl = (int?)range?["start"]?["line"] ?? 0;
            var sc = (int?)range?["start"]?["character"] ?? 0;
            return $"{label}: {UriToPath(uri)}:{sl + 1}:{sc + 1}";
        }

        return $"（{label}结果格式未知）";
    }

    private static void FormatSymbols(JsonArray symbols, List<string> lines, int depth, string filter)
    {
        foreach (var s in symbols)
        {
            if (s == null) continue;
            var name = s["name"]?.GetValue<string>() ?? "?";
            var kind = (int?)s["kind"] ?? 0;
            var kindStr = KindToString(kind);
            var indent = new string(' ', depth * 2);

            if (string.IsNullOrEmpty(filter) || name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                lines.Add($"{indent}{kindStr} {name}");

            if (s["children"]?.AsArray() is { } children)
                FormatSymbols(children, lines, depth + 1, filter);
        }
    }

    private static string KindToString(int kind) => kind switch
    {
        5 => "[class]", 6 => "[method]", 7 => "[property]", 9 => "[ctor]",
        12 => "[func]", 13 => "[var]", 14 => "[const]",
        _ => "[?]",
    };

    private static string UriToPath(string uri) =>
        uri.Replace("file:///", "").Replace("file://", "");
}
