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

    /// <summary>运行中的 LSP 会话快照（供 Web 面板/侧栏展示）。</summary>
    public record ActiveLspInfo(string Command, string Root, bool Initialized, bool HasExited);

    /// <summary>当前运行中的 LSP 会话列表（在会话锁内快照，线程安全）。
    /// 有超时的锁：LSP 握手最坏持锁 ~10s，UI 线程（Web 面板/侧栏刷新）不能为此卡死，
    /// 超时则返回当前缓存快照（可能略旧），下次刷新再读。</summary>
    public static List<ActiveLspInfo> ActiveSessions
    {
        get
        {
            if (!_sessionLock.Wait(100)) return _cachedSnapshot ?? [];
            try
            {
                var list = new List<ActiveLspInfo>(_sessions.Count);
                foreach (var s in _sessions.Values)
                    list.Add(new ActiveLspInfo(s.Command, s.Root, s.Initialized, s.Process.HasExited));
                _cachedSnapshot = list;
                return list;
            }
            finally
            {
                _sessionLock.Release();
            }
        }
    }

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
        var line = ToolArgs.GetInt(arguments, "line", 1);
        var character = ToolArgs.GetInt(arguments, "character", 1);
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

        // 串行化：LSP 会话是单连接，不能并发读写，全程持锁避免多槽位抢同一进程。
        // 3s 超时防死锁（另一槽位持锁卡死时本请求不永久挂起，回退错误提示）
        if (!await _sessionLock.WaitAsync(TimeSpan.FromSeconds(3)))
            return $"错误：LSP 会话忙（3s 超时，可能被其他槽位占用）";
        try
        {
            var root = FindProjectRoot(filePath);
            var key = $"{root}|{config.Value.Command}";
            CleanupStaleSessions();

            if (!_sessions.TryGetValue(key, out var session) || session!.Process.HasExited)
            {
                if (session != null) KillAndDispose(session.Process);
                var proc = StartServer(config.Value.Command, config.Value.Args, root);
                if (proc == null) return $"错误：无法启动 LSP 服务器 ({config.Value.Command})";
                session = new LspSession { Process = proc, Command = config.Value.Command, Root = root, Initialized = false };
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
        var root = Path.GetDirectoryName(Path.GetFullPath(file, BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory())) ?? "."; // cd 后相对路径基于被跟踪工作目录
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
            .Where(kv => kv.Value.Process.HasExited || (now - kv.Value.LastUsedTicks) > SessionIdleTimeoutMs)
            .Select(kv => kv.Key)
            .ToList();
        foreach (var key in stale)
        {
            var s = _sessions[key];
            _sessions.Remove(key);
            KillAndDispose(s.Process);
        }
    }

    /// <summary>关闭所有缓存的 LSP 会话（进程退出/测试清理时调用）。</summary>
    public static void ShutdownAllSessions()
    {
        // 3s 超时防死锁：会话锁被占用时跳过清理（进程退出路径不阻塞）
        if (!_sessionLock.Wait(TimeSpan.FromSeconds(3)))
            return;
        try
        {
            foreach (var s in _sessions.Values)
            {
                KillAndDispose(s.Process);
            }
            _sessions.Clear();
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    /// <summary>终止并释放 LSP 服务器进程（防进程句柄泄漏）。</summary>
    private static void KillAndDispose(Process? proc)
    {
        if (proc == null) return;
        try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); } catch { }
        try { proc.Dispose(); } catch { }
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
            var psi = new ProcessStartInfo
            {
                FileName = command,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            // 用 ArgumentList 逐个传参：运行时正确加引号，root 路径含空格时不再被拆断
            foreach (var a in args) psi.ArgumentList.Add(a);
            psi.ArgumentList.Add(root);

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
    /// <summary>上次成功快照（锁被慢操作持有时供 UI 返回缓存，避免卡死）</summary>
    private static List<ActiveLspInfo>? _cachedSnapshot;
    // 单位毫秒，与 Environment.TickCount64 一致。此前用 TimeSpan.FromMinutes(5).Ticks（100 纳秒=3e9）
    // 与毫秒差值比较，5 分钟回收实际变成约 34.7 天，空闲 LSP 进程长期不释放。
    private static readonly long SessionIdleTimeoutMs = 5L * 60 * 1000;

    private sealed class LspSession
    {
        public Process Process = null!;
        public string Command = "";
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
        // initialized 是单向通知，服务器不回包；此前 await ReadResponse 会白等 10s 超时
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
            // 直接用底层字节流读，避免 StreamReader 缓冲混用；头是 ASCII，逐字节拼
            var stream = proc.StandardOutput.BaseStream;
            var header = new StringBuilder();
            var one = new byte[1];
            while (!header.ToString().EndsWith("\r\n\r\n"))
            {
                var n = await stream.ReadAsync(one.AsMemory(0, 1), cts.Token);
                if (n == 0) break; // EOF：服务器提前关闭
                header.Append((char)one[0]);
                if (header.Length > 200) break;
            }
            var headerStr = header.ToString();
            if (!headerStr.StartsWith("Content-Length:")) return null;

            var lenStr = headerStr["Content-Length:".Length..].Split('\r')[0].Trim();
            // 长度上限防御：恶意/损坏的 Content-Length 不应触发巨大缓冲区分配
            if (!int.TryParse(lenStr, out var len) || len <= 0 || len > 10_000_000) return null;

            // Content-Length 是「字节数」，必须按字节读取；此前按「字符数」读，
            // 遇到非 ASCII 内容时字节数 > 字符数 → 读不满 len 个字符 → 10s 超时 → hover/symbols 失效
            var buffer = new byte[len];
            var total = 0;
            while (total < len)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(total, len - total), cts.Token);
                if (read == 0) break; // EOF
                total += read;
            }
            return Json.Parse(Encoding.UTF8.GetString(buffer, 0, total));
        }
        catch { return null; }
    }

    private static string FileToUri(string path)
    {
        // 用 Uri 统一转义空格/#/%/非 ASCII；旧实现直接拼路径，含空格/中文时生成非法 URI 导致 LSP 请求失败
        try { return new Uri(Path.GetFullPath(path)).AbsoluteUri; }
        catch { return "file:///" + Path.GetFullPath(path).Replace('\\', '/').TrimStart('/'); }
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
