namespace WayCoder;

using System.Text;

/// <summary>
/// 代码知识库 —— 扫描项目源码，提取「符号（函数/方法/类/接口）+ 文档注释」分块，
/// 复用 <see cref="SemanticMemory"/> 的 TF-IDF 检索，把与当前任务最相关的代码段注入系统提示词，
/// 让智能体「语义级召回代码」而不仅靠 grep/glob 精确匹配。
///
/// 设计约束：AOT 零反射、零 Regex 依赖，纯文本启发式（定义行识别 + 注释回溯），
/// 分块粒度「够用即可」——不追求精确 AST，召回的是可读的代码片段而非编译单元。
/// 摄入自带缓存：扫描文件算指纹（路径 + mtime），指纹不变则复用已提取的符号块，避免每轮重读磁盘。
/// </summary>
public static class CodeKnowledge
{
    /// <summary>要摄入的源码扩展名白名单。</summary>
    private static readonly HashSet<string> SrcExts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".py", ".js", ".mjs", ".ts", ".jsx", ".tsx", ".go", ".rs", ".java",
        ".kt", ".kts", ".c", ".cc", ".cpp", ".h", ".hpp", ".swift", ".rb", ".php",
        ".sh", ".sql", ".vue", ".scala", ".dart", ".lua", ".zig", ".ex", ".exs",
    };

    /// <summary>补充跳过的目录（FileIgnoreManager 未覆盖的 .NET/项目产物目录）。</summary>
    private static readonly HashSet<string> ExtraSkipDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "logs", "publish", ".waycoder", ".corecoder",
    };

    // 摄入上限（防大仓库拖慢每轮检索）
    const int MaxFiles = 400;          // 最多摄入的文件数
    const int MaxFileBytes = 150_000;  // 单文件大小上限（150KB）
    const int MaxFileLines = 3000;     // 单文件行数上限
    const int MaxSymbolsPerFile = 40;  // 单文件符号数上限
    const int MaxChunks = 1500;        // 总块数上限
    const int MaxCommentLines = 8;     // 每个符号回溯注释行数上限
    const int MaxBodyLines = 8;        // 定义行后追加的函数体/成员行数上限
    const int MaxChunkChars = 800;     // 单块内容上限（rune）

    private static List<SemanticMemory.MemoryDocument> _docs = [];
    private static string _fingerprint = "";
    private static string _cwd = "";

    /// <summary>已摄入的代码块数。</summary>
    public static int ChunkCount => _docs.Count;

    /// <summary>最近一次摄入的指纹（供 ProjectKnowledge 合并缓存判断）。</summary>
    public static string Fingerprint => _fingerprint;

    /// <summary>
    /// 摄入项目源码（幂等：扫描文件算指纹，未变则复用缓存）。返回代码符号块列表。
    /// </summary>
    public static List<SemanticMemory.MemoryDocument> Ingest(string cwd)
    {
        var (files, fp) = ScanAndFingerprint(cwd);
        if (fp == _fingerprint && _docs.Count > 0 && _cwd == cwd) return _docs;

        var docs = new List<SemanticMemory.MemoryDocument>();
        int idx = 0;
        foreach (var f in files)
        {
            if (docs.Count >= MaxChunks) break;
            try { ExtractSymbols(f, cwd, docs, ref idx); }
            catch { /* 单文件解析失败跳过 */ }
        }

        _docs = docs;
        _fingerprint = fp;
        _cwd = cwd;
        return _docs;
    }

    // ─────────────────────────────────────────────────────────────
    // 扫描 + 指纹
    // ─────────────────────────────────────────────────────────────

    /// <summary>扫描源码文件（惰性剪枝），同时拼指纹（路径 + mtime），一次遍历完成。</summary>
    private static (List<string> Files, string Fp) ScanAndFingerprint(string cwd)
    {
        var files = new List<string>();
        var fp = new StringBuilder();
        Walk(cwd, cwd, files, fp);
        return (files, fp.ToString());
    }

    private static void Walk(string dir, string cwd, List<string> files, StringBuilder fp)
    {
        if (files.Count >= MaxFiles) return;
        string[] entries;
        try { entries = Directory.GetFileSystemEntries(dir); }
        catch { return; }

        foreach (var e in entries)
        {
            if (files.Count >= MaxFiles) break;
            var name = Path.GetFileName(e);
            if (ExtraSkipDirs.Contains(name)) continue;

            if (Directory.Exists(e))
            {
                if (FileIgnoreManager.ShouldSkipDirectory(e, cwd)) continue;
                Walk(e, cwd, files, fp);
            }
            else
            {
                if (!SrcExts.Contains(Path.GetExtension(e))) continue;
                if (FileIgnoreManager.IsIgnored(e, cwd)) continue;
                files.Add(e);
                try { fp.Append(e).Append(':').Append(File.GetLastWriteTimeUtc(e).Ticks).Append(';'); }
                catch { /* mtime 读不到就跳过指纹 */ }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 符号提取
    // ─────────────────────────────────────────────────────────────

    private static void ExtractSymbols(string path, string cwd, List<SemanticMemory.MemoryDocument> docs, ref int idx)
    {
        var info = new FileInfo(path);
        if (info.Length > MaxFileBytes) return;

        string[] lines;
        try { lines = File.ReadAllLines(path); }
        catch { return; }
        if (lines.Length > MaxFileLines) return;

        var rel = Path.GetRelativePath(cwd, path).Replace('\\', '/');
        int symCount = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            if (docs.Count >= MaxChunks || symCount >= MaxSymbolsPerFile) return;
            var trimmed = lines[i].Trim();
            if (!IsDefinitionLine(trimmed)) continue;

            var sym = ExtractSymbolName(trimmed);
            if (sym.Length == 0) continue;

            var comment = CollectDocComment(lines, i - 1);
            var body = CollectBody(lines, i + 1);

            var sb = new StringBuilder();
            if (comment.Length > 0) sb.Append(comment).Append('\n');
            sb.Append(trimmed);
            if (body.Length > 0) sb.Append('\n').Append(body);

            var content = sb.ToString();
            if (content.Length > MaxChunkChars)
                content = ContextManager.TruncateByRunes(content, MaxChunkChars);

            docs.Add(new SemanticMemory.MemoryDocument
            {
                Title = $"{rel} › {sym}",
                Content = content,
                Index = idx++,
            });
            symCount++;
        }
    }

    /// <summary>声明关键字（类/函数等），按「独立词」（前后非标识符字符）匹配，兼容 public/private/static/export 等前缀。</summary>
    private static readonly string[] DeclKeywords =
        ["class", "interface", "struct", "enum", "record", "trait", "def", "func", "fn", "function"];

    /// <summary>判断一行是否为「定义行」（函数/方法/类/接口/结构/枚举等），排除注释与语句。</summary>
    private static bool IsDefinitionLine(string t)
    {
        if (t.Length == 0) return false;
        char c0 = t[0];

        // 注释 / 花括号 / 预处理（# 开头）
        if (c0 == '/' || c0 == '#' || c0 == '*' || c0 == ';') return false;
        if (t == "{" || t == "}") return false;

        // 控制流 / 语句 / 导入关键字（后跟空格或标点），这些不是定义
        if (StartsAny(t,
            "if ", "else", "for ", "while ", "switch ", "return ", "return;", "return(",
            "catch", "finally", "try", "do ", "case ", "break", "continue",
            "throw ", "yield ", "await ", "using ", "import ", "from ", "#include",
            "include ", "require ", "package ", "namespace ", "print", "echo ",
            "console.", "assert", "raise ", "pass", "goto ", "delete ", "typeof ",
            "instanceof ", "typedef ", "define ", "select ", "insert ", "update ",
            "set ", "local ", "elif ", "elseif", "with ", "lambda ", "new "))
            return false;

        // 声明关键字（独立词，含 public class / export function 等带前缀形式）
        foreach (var kw in DeclKeywords)
            if (FindKeyword(t, kw) >= 0) return true;

        // 含 ( 且以标识符/析构符开头 → 函数 / 方法 / 构造器
        if (t.Contains('(') && (char.IsLetter(c0) || c0 == '_' || c0 == '~'))
            return true;

        return false;
    }

    /// <summary>从定义行提取符号名（声明关键字后的标识符，或 ( 前最后一个标识符）。</summary>
    private static string ExtractSymbolName(string t)
    {
        foreach (var kw in DeclKeywords)
        {
            int pos = FindKeyword(t, kw);
            if (pos >= 0)
            {
                var name = ReadIdentifier(t[(pos + kw.Length)..].TrimStart());
                if (name.Length > 0) return name;
            }
        }

        int lp = t.IndexOf('(');
        if (lp > 0)
        {
            var name = ReadLastIdentifier(t[..lp].TrimEnd());
            if (name.Length > 0) return name;
        }
        return "";
    }

    /// <summary>在 t 中找关键字 kw 作为「独立词」的位置（前后都不是标识符字符），找不到返回 -1。</summary>
    private static int FindKeyword(string t, string kw)
    {
        int pos = t.IndexOf(kw, StringComparison.Ordinal);
        while (pos >= 0)
        {
            bool leftOk = pos == 0 || !IsIdentChar(t[pos - 1]);
            int end = pos + kw.Length;
            bool rightOk = end >= t.Length || !IsIdentChar(t[end]);
            if (leftOk && rightOk) return pos;
            pos = t.IndexOf(kw, pos + 1, StringComparison.Ordinal);
        }
        return -1;
    }

    private static bool IsIdentChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    /// <summary>从定义行向前回溯文档注释（/// // # /* * 及多行字符串引号）。</summary>
    private static string CollectDocComment(string[] lines, int endIdx)
    {
        var sb = new StringBuilder();
        int count = 0;
        for (int i = endIdx; i >= 0 && count < MaxCommentLines; i--)
        {
            var t = lines[i].Trim();
            if (t.Length == 0) continue;
            if (!IsCommentLine(t)) break;
            sb.Insert(0, t + "\n");
            count++;
        }
        return sb.ToString().Trim();
    }

    /// <summary>收集定义行后的函数体/成员行（跳过空行与纯花括号），帮助覆盖函数体开头。</summary>
    private static string CollectBody(string[] lines, int startIdx)
    {
        var sb = new StringBuilder();
        int count = 0;
        for (int i = startIdx; i < lines.Length && count < MaxBodyLines; i++)
        {
            var t = lines[i].Trim();
            if (t.Length == 0 || t == "{" || t == "}") continue;
            sb.AppendLine(t);
            count++;
        }
        return sb.ToString().Trim();
    }

    private static bool IsCommentLine(string t)
        => t.StartsWith("//") || t.StartsWith("#") || t.StartsWith("/*") || t.StartsWith("*")
        || t.StartsWith("'''") || t.StartsWith("\"\"\"");

    private static bool StartsAny(string t, params string[] prefixes)
    {
        foreach (var p in prefixes)
            if (t.StartsWith(p, StringComparison.Ordinal)) return true;
        return false;
    }

    private static string ReadIdentifier(string s)
    {
        int i = 0;
        while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
        return s[..i];
    }

    private static string ReadLastIdentifier(string s)
    {
        int i = s.Length - 1;
        while (i >= 0 && !(char.IsLetterOrDigit(s[i]) || s[i] == '_')) i--;
        int end = i + 1;
        while (i >= 0 && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i--;
        return s[(i + 1)..end];
    }
}
