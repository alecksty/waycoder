using System.Diagnostics;
using System.Text;

namespace WayCoder.Tools;

/// <summary>
/// LSP 代码导航工具 —— 通过 Language Server Protocol 提供代码智能。
/// 支持 go-to-definition、find-references、hover、document-symbols。
/// 自动检测项目语言并启动对应的 LSP 服务器。
/// </summary>
public class LspTool : ITool
{
    public string Name => "lsp";
    public ToolExecutionMode ExecutionMode => ToolExecutionMode.Exclusive;
    public string Description => "代码智能导航：跳转定义(definition)、查找引用(references)、类型悬停(hover)、文档符号(symbols)。支持 C#/Python/JS/TS/Go/Rust/C/C++/Java/Kotlin/Ruby/PHP/Lua/Bash/Swift/Zig。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("action", JNode.Object()
                .Set("type", "string")
                .Set("description", "操作: definition | references | hover | symbols"))
            .Set("file_path", JNode.Object()
                .Set("type", "string")
                .Set("description", "文件路径"))
            .Set("line", JNode.Object()
                .Set("type", "integer")
                .Set("description", "行号 (1-based)"))
            .Set("character", JNode.Object()
                .Set("type", "integer")
                .Set("description", "列号 (1-based)"))
            .Set("query", JNode.Object()
                .Set("type", "string")
                .Set("description", "符号搜索关键词 (symbols 操作时用)")))
        .Set("required", JNode.Array().Add("action").Add("file_path").Add("line").Add("character"));

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
        ["cpp"] = ("clangd", []),
        ["java"] = ("jdtls", []),
        ["kotlin"] = ("kotlin-language-server", []),
        ["ruby"] = ("solargraph", ["stdio"]),
        ["php"] = ("intelephense", ["--stdio"]),
        ["lua"] = ("lua-language-server", []),
        ["bash"] = ("bash-language-server", ["start"]),
        ["swift"] = ("sourcekit-lsp", []),
        ["zig"] = ("zls", []),
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
        [".c"] = "cpp", [".cpp"] = "cpp", [".cc"] = "cpp", [".cxx"] = "cpp",
        [".h"] = "cpp", [".hpp"] = "cpp", [".hh"] = "cpp",
        [".java"] = "java",
        [".kt"] = "kotlin", [".kts"] = "kotlin",
        [".rb"] = "ruby",
        [".php"] = "php",
        [".lua"] = "lua",
        [".sh"] = "bash", [".bash"] = "bash",
        [".swift"] = "swift",
        [".zig"] = "zig",
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

        // 串行化：LSP 会话是单连接，不能并发读写，全程持锁避免多槽位抢同一进程
        await _sessionLock.WaitAsync();
        try
        {
            var root = FindProjectRoot(filePath);
            var key = $"{root}|{config.Value.Command}";
            CleanupStaleSessions();

            if (!_sessions.TryGetValue(key, out var session) || session!.Process.HasExited)
            {
                if (session != null) { try { session.Process.Kill(entireProcessTree: true); } catch { } }
                var proc = StartServer(config.Value.Command, config.Value.Args, root);
                if (proc == null) return $"错误：无法启动 LSP 服务器 ({config.Value.Command})";
                session = new LspSession { Process = proc, Root = root, Initialized = false };
                _sessions[key] = session;
            }

            session.LastUsedTicks = Environment.TickCount64;

            // 首次初始化握手（initialize + initialized），后续复用跳过
            if (!session.Initialized)
            {
                await Task.Delay(500); // 等待服务器就绪
                await InitializeHandshake(session.Process, root);
                session.Initialized = true;
            }

            // 每次针对当前文件打开文档（通知，无响应）
            DidOpen(session.Process, filePath);

            return action switch
            {
                "definition" => await GoToDefinition(session.Process, filePath, line, charPos),
                "references" => await FindReferences(session.Process, filePath, line, charPos),
                "hover" => await Hover(session.Process, filePath, line, charPos),
                "symbols" => await DocumentSymbols(session.Process, filePath, query),
                _ => "错误：未知操作",
            };
        }
        catch (Exception ex)
        {
            return $"LSP 错误：{ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    /// <summary>向上查找项目根目录（.sln/.csproj/package.json/go.mod/Cargo.toml/pyproject.toml）。</summary>
    internal static string FindProjectRoot(string file)
    {
        var root = Path.GetDirectoryName(Path.GetFullPath(file)) ?? ".";
        while (root != null && !HasProjectMarker(root))
        {
            var parent = Path.GetDirectoryName(root);
            if (parent == root) break;
            root = parent;
        }
        return root ?? ".";
    }

    /// <summary>判断目录是否包含项目标记文件（对不存在目录返回 false）。</summary>
    private static bool HasProjectMarker(string dir)
    {
        if (!Directory.Exists(dir)) return false;
        if (Directory.GetFiles(dir, "*.sln").Length > 0) return true;
        if (Directory.GetFiles(dir, "*.csproj").Length > 0) return true;
        if (File.Exists(Path.Combine(dir, "package.json"))) return true;
        if (File.Exists(Path.Combine(dir, "go.mod"))) return true;
        if (File.Exists(Path.Combine(dir, "Cargo.toml"))) return true;
        if (File.Exists(Path.Combine(dir, "pyproject.toml"))) return true;
        return false;
    }

    /// <summary>回收空闲超时的会话，释放 LSP 服务器进程。</summary>
    private static void CleanupStaleSessions()
    {
        var now = Environment.TickCount64;
        var stale = _sessions
            .Where(kv => kv.Value.Process.HasExited || (now - kv.Value.LastUsedTicks) > SessionIdleTimeoutTicks)
            .Select(kv => kv.Key)
            .ToList();
        foreach (var key in stale)
        {
            var s = _sessions[key];
            _sessions.Remove(key);
            try { if (!s.Process.HasExited) s.Process.Kill(entireProcessTree: true); } catch { }
        }
    }

    /// <summary>关闭所有缓存的 LSP 会话（进程退出/测试清理时调用）。</summary>
    public static void ShutdownAllSessions()
    {
        _sessionLock.Wait();
        try
        {
            foreach (var s in _sessions.Values)
            {
                try { if (!s.Process.HasExited) s.Process.Kill(entireProcessTree: true); } catch { }
            }
            _sessions.Clear();
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    private static (string Command, string[] Args)? FindServer(string ext)
    {
        if (ExtToLang.TryGetValue(ext, out var lang) && ServerConfigs.TryGetValue(lang, out var cfg))
            return cfg;
        return null;
    }

    private static Process? StartServer(string command, string[] args, string root)
    {
        try
        {
            var allArgs = new List<string>(args) { root };
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
        var req = BuildRequest("textDocument/definition", JNode.Object()
            .Set("textDocument", JNode.Object().Set("uri", FileToUri(file)))
            .Set("position", JNode.Object().Set("line", line - 1).Set("character", ch - 1)));
        SendMessage(proc, req);
        var resp = await ReadResponse(proc);
        return FormatLocationResult(resp, "定义");
    }

    private static async Task<string> FindReferences(Process proc, string file, int line, int ch)
    {
        var req = BuildRequest("textDocument/references", JNode.Object()
            .Set("textDocument", JNode.Object().Set("uri", FileToUri(file)))
            .Set("position", JNode.Object().Set("line", line - 1).Set("character", ch - 1))
            .Set("context", JNode.Object().Set("includeDeclaration", true)));
        SendMessage(proc, req);
        var resp = await ReadResponse(proc);
        return FormatLocationResult(resp, "引用");
    }

    private static async Task<string> Hover(Process proc, string file, int line, int ch)
    {
        var req = BuildRequest("textDocument/hover", JNode.Object()
            .Set("textDocument", JNode.Object().Set("uri", FileToUri(file)))
            .Set("position", JNode.Object().Set("line", line - 1).Set("character", ch - 1)));
        SendMessage(proc, req);
        var resp = await ReadResponse(proc);
        if (resp?["result"]?["contents"]?["value"]?.AsString() is { } text)
            return text;
        if (resp?["result"]?.AsString() is { } str)
            return str;
        return "（无类型信息）";
    }

    private static async Task<string> DocumentSymbols(Process proc, string file, string query)
    {
        var req = BuildRequest("textDocument/documentSymbol", JNode.Object()
            .Set("textDocument", JNode.Object().Set("uri", FileToUri(file))));
        SendMessage(proc, req);
        var resp = await ReadResponse(proc);

        var symbols = resp?["result"];
        if (symbols == null || symbols.Count == 0) return "（无符号）";

        var lines = new List<string>();
        FormatSymbols(symbols, lines, 0, query);
        return lines.Count > 0 ? string.Join("\n", lines) : "（无匹配符号）";
    }

    // ---- LSP 协议辅助 ----

    // ---- 会话缓存 ----
    // 复用已启动的 LSP 服务器进程，避免每次导航都重启 + 重新初始化（数百 ms 开销）。
    // 按 (项目根, 命令) 区分会话；空闲超时自动回收；进程崩溃自动重建。
    private static readonly Dictionary<string, LspSession> _sessions = new();
    private static readonly SemaphoreSlim _sessionLock = new(1, 1);
    private static readonly long SessionIdleTimeoutTicks = TimeSpan.FromMinutes(5).Ticks;

    private sealed class LspSession
    {
        public Process Process = null!;
        public string Root = "";
        public bool Initialized;
        public long LastUsedTicks;
    }

    private static int _msgId;
    private static string BuildRequest(string method, JNode @params)
    {
        var id = Interlocked.Increment(ref _msgId);
        var msg = JNode.Object()
            .Set("jsonrpc", "2.0")
            .Set("id", id)
            .Set("method", method)
            .Set("params", @params);
        return $"Content-Length: {Encoding.UTF8.GetByteCount(msg.ToJson())}\r\n\r\n{msg.ToJson()}";
    }

    private static async Task InitializeHandshake(Process proc, string root)
    {
        var initReq = BuildRequest("initialize", JNode.Object()
            .Set("processId", Environment.ProcessId)
            .Set("rootUri", FileToUri(root))
            .Set("capabilities", JNode.Object()));
        SendMessage(proc, initReq);
        await ReadResponse(proc);
        // 发送 initialized 通知
        var notif = JNode.Object().Set("jsonrpc", "2.0").Set("method", "initialized").Set("params", JNode.Object());
        var notifStr = $"Content-Length: {Encoding.UTF8.GetByteCount(notif.ToJson())}\r\n\r\n{notif.ToJson()}";
        SendMessage(proc, notifStr);
        await ReadResponse(proc);
    }

    private static void DidOpen(Process proc, string filePath)
    {
        // 打开文档（通知，无响应）
        var didOpen = JNode.Object()
            .Set("jsonrpc", "2.0")
            .Set("method", "textDocument/didOpen")
            .Set("params", JNode.Object()
                .Set("textDocument", JNode.Object()
                    .Set("uri", FileToUri(filePath))
                    .Set("languageId", GetLanguageId(filePath))
                    .Set("version", 1)
                    .Set("text", File.ReadAllText(filePath))));
        var openStr = $"Content-Length: {Encoding.UTF8.GetByteCount(didOpen.ToJson())}\r\n\r\n{didOpen.ToJson()}";
        SendMessage(proc, openStr);
    }

    private static void SendMessage(Process proc, string message)
    {
        try { proc.StandardInput.Write(message); proc.StandardInput.Flush(); }
        catch { /* 忽略写入错误 */ }
    }

    private static async Task<JNode?> ReadResponse(Process proc)
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
            return Json.Parse(new string(buffer, 0, read));
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
            ".c" or ".h" => "c",
            ".cpp" or ".cc" or ".cxx" or ".hpp" or ".hh" => "cpp",
            ".java" => "java",
            ".kt" or ".kts" => "kotlin",
            ".rb" => "ruby",
            ".php" => "php",
            ".lua" => "lua",
            ".sh" or ".bash" => "shellscript",
            ".swift" => "swift",
            ".zig" => "zig",
            _ => "plaintext",
        };
    }

    private static string FormatLocationResult(JNode? resp, string label)
    {
        var result = resp?["result"];
        if (result == null) return $"（无{label}）";

        if (result.Kind == JKind.Array)
        {
            var lines = new List<string> { $"{label} ({result.Count} 处):" };
            foreach (var loc in result.Items.Take(20))
            {
                var uri = loc["uri"]?.AsString() ?? "?";
                var range = loc["range"];
                var startLine = (int)(range?["start"]?["line"]?.AsNumber() ?? 0);
                var startCh = (int)(range?["start"]?["character"]?.AsNumber() ?? 0);
                lines.Add($"  {UriToPath(uri)}:{startLine + 1}:{startCh + 1}");
            }
            if (result.Count > 20) lines.Add($"  ... 还有 {result.Count - 20} 处");
            return string.Join("\n", lines);
        }

        if (result.Kind == JKind.Object && result["uri"] != null)
        {
            var uri = result["uri"]?.AsString() ?? "?";
            var range = result["range"];
            var sl = (int)(range?["start"]?["line"]?.AsNumber() ?? 0);
            var sc = (int)(range?["start"]?["character"]?.AsNumber() ?? 0);
            return $"{label}: {UriToPath(uri)}:{sl + 1}:{sc + 1}";
        }

        return $"（{label}结果格式未知）";
    }

    private static void FormatSymbols(JNode symbols, List<string> lines, int depth, string filter)
    {
        foreach (var s in symbols.Items)
        {
            if (s == null) continue;
            var name = s["name"]?.AsString() ?? "?";
            var kind = (int)(s["kind"]?.AsNumber() ?? 0);
            var kindStr = KindToString(kind);
            var indent = new string(' ', depth * 2);

            if (string.IsNullOrEmpty(filter) || name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                lines.Add($"{indent}{kindStr} {name}");

            if (s["children"] is { Kind: JKind.Array } children)
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
