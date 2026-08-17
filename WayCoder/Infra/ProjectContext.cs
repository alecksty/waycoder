using System.Text.RegularExpressions;

namespace WayCoder;

/// <summary>
/// 项目上下文检测 —— 启动时自动分析项目结构和指令文件。
///
/// 1. 向上查找 CLAUDE.md / AGENTS.md 注入自定义指令
/// 2. 检测项目类型、语言、框架、构建工具
/// 3. 汇总 .gitignore、README 等关键文件
/// </summary>
public static class ProjectContext
{
    /// <summary>
    /// 从当前目录向上查找并读取 CLAUDE.md / AGENTS.md。
    /// 返回 Markdown 格式的注入文本（可直接附加到系统提示词）。
    /// </summary>
    public static string LoadInstructions()
    {
        var files = FindUpward([".claude", ".waycoder", ".corecoder", "CLAUDE.md", "AGENTS.md", ".cursorrules"]);
        if (files.Count == 0) return "";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("\n# 项目指令");
        foreach (var file in files)
        {
            try
            {
                var content = File.ReadAllText(file);
                var relative = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
                sb.AppendLine($"\n## {relative}\n");
                // 截断过长文件
                if (content.Length > 4000)
                    sb.AppendLine(ContextManager.TruncateByRunes(content, 4000) + "\n\n... (已截断)");
                else
                    sb.AppendLine(content);
            }
            catch (Exception ex) { DebugLog.Log("ProjectContext", $"读取指令文件失败 {file}: {ex.Message}"); }
        }
        return sb.ToString();
    }

    /// <summary>
    /// 检测当前项目的技术栈。
    /// </summary>
    public static ProjectInfo DetectProject()
    {
        var info = new ProjectInfo();
        var root = FindProjectRoot();
        info.ProjectRoot = root;

        // 语言检测
        DetectLanguages(root, info);

        // 框架检测
        DetectFrameworks(root, info);

        // 构建工具
        DetectBuildTools(root, info);

        // Git 信息
        DetectGitInfo(info);

        return info;
    }

    private static List<string> FindUpward(string[] names)
    {
        var results = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var current = Directory.GetCurrentDirectory();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        while (true)
        {
            foreach (var name in names)
            {
                // 也检查 .claude/CLAUDE.md 和 .waycoder/prompt.md 路径
                if (name is ".claude" or ".waycoder" or ".corecoder")
                {
                    var dir = Path.Combine(current, name);
                    if (Directory.Exists(dir))
                    {
                        foreach (var md in Directory.GetFiles(dir, "*.md"))
                        {
                            // 排除 memory.md（记忆系统专用，不注入系统提示词）
                            var fileName = Path.GetFileName(md);
                            if (fileName.Equals("memory.md", StringComparison.OrdinalIgnoreCase))
                                continue;
                            if (seen.Add(md)) results.Add(md);
                        }
                    }
                    continue;
                }

                var path = Path.Combine(current, name);
                if (File.Exists(path) && seen.Add(path))
                    results.Add(path);
            }

            if (current == home || current == Path.GetPathRoot(current) || string.IsNullOrEmpty(current))
                break;
            current = Path.GetDirectoryName(current)!;
        }

        return results;
    }

    private static string FindProjectRoot()
    {
        var current = Directory.GetCurrentDirectory();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        while (true)
        {
            // 检测项目标志文件（.git 在普通仓库是目录、在 worktree/submodule 是文件，需两者都判）
            if (Directory.Exists(Path.Combine(current, ".git"))
                || File.Exists(Path.Combine(current, ".git"))
                || File.Exists(Path.Combine(current, "package.json"))
                || File.Exists(Path.Combine(current, "go.mod"))
                || File.Exists(Path.Combine(current, "Cargo.toml"))
                || File.Exists(Path.Combine(current, "pyproject.toml"))
                || Directory.GetFiles(current, "*.csproj").Length > 0
                || Directory.GetFiles(current, "*.sln").Length > 0)
            {
                return current;
            }

            if (current == home || current == Path.GetPathRoot(current) || string.IsNullOrEmpty(current))
                return Directory.GetCurrentDirectory();
            current = Path.GetDirectoryName(current)!;
        }
    }

    private static void DetectLanguages(string root, ProjectInfo info)
    {
        var allFiles = SafeGetFiles(root, 200);
        var exts = new Dictionary<string, int>();
        foreach (var f in allFiles)
        {
            var ext = Path.GetExtension(f).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) continue;
            exts[ext] = exts.GetValueOrDefault(ext) + 1;
        }

        var topExts = exts.OrderByDescending(kv => kv.Value).Take(5)
            .Select(kv => $"{kv.Key}({kv.Value}文件)").ToList();
        info.Languages = topExts;

        if (exts.ContainsKey(".cs")) info.PrimaryLanguage = "C# (.NET)";
        else if (exts.ContainsKey(".py")) info.PrimaryLanguage = "Python";
        else if (exts.ContainsKey(".ts") || exts.ContainsKey(".tsx")) info.PrimaryLanguage = "TypeScript";
        else if (exts.ContainsKey(".js")) info.PrimaryLanguage = "JavaScript";
        else if (exts.ContainsKey(".go")) info.PrimaryLanguage = "Go";
        else if (exts.ContainsKey(".rs")) info.PrimaryLanguage = "Rust";
        else if (exts.ContainsKey(".java")) info.PrimaryLanguage = "Java";
        else info.PrimaryLanguage = "未知";
    }

    private static void DetectFrameworks(string root, ProjectInfo info)
    {
        var frameworks = new List<string>();

        if (File.Exists(Path.Combine(root, "package.json")))
        {
            try
            {
                var json = File.ReadAllText(Path.Combine(root, "package.json"));
                if (json.Contains("\"next\"")) frameworks.Add("Next.js");
                if (json.Contains("\"react\"")) frameworks.Add("React");
                if (json.Contains("\"vue\"")) frameworks.Add("Vue");
                if (json.Contains("\"express\"")) frameworks.Add("Express");
            }
            catch (Exception ex) { DebugLog.Log("ProjectContext", $"解析 package.json 失败: {ex.Message}"); }
        }

        var csprojFiles = SafeGetFiles(root, 100).Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)).ToList();
        if (csprojFiles.Count > 0)
        {
            try
            {
                var content = File.ReadAllText(csprojFiles.First());
                if (content.Contains("Microsoft.NET.Sdk.Web")) frameworks.Add("ASP.NET Core");
                if (content.Contains("Microsoft.NET.Sdk")) frameworks.Add(".NET SDK");
                if (content.Contains("Microsoft.NET.Sdk.Blazor")) frameworks.Add("Blazor");
            }
            catch (Exception ex) { DebugLog.Log("ProjectContext", $"解析 csproj 失败: {ex.Message}"); }
        }

        if (File.Exists(Path.Combine(root, "go.mod")))
        {
            try
            {
                var gomod = File.ReadAllText(Path.Combine(root, "go.mod"));
                var m = Regex.Match(gomod, @"module\s+(\S+)");
                if (m.Success) frameworks.Add($"Go ({m.Groups[1].Value})");
            }
            catch (Exception ex) { DebugLog.Log("ProjectContext", $"解析 go.mod 失败: {ex.Message}"); }
        }

        info.Frameworks = frameworks;
    }

    private static void DetectBuildTools(string root, ProjectInfo info)
    {
        var tools = new List<string>();
        if (File.Exists(Path.Combine(root, "Makefile"))) tools.Add("make");
        if (SafeGetFiles(root, 100).Any(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))) tools.Add("dotnet");
        if (File.Exists(Path.Combine(root, "package.json"))) tools.Add("npm/yarn/pnpm");
        if (File.Exists(Path.Combine(root, "go.mod"))) tools.Add("go");
        if (File.Exists(Path.Combine(root, "Cargo.toml"))) tools.Add("cargo");
        if (File.Exists(Path.Combine(root, "pyproject.toml"))) tools.Add("pip/poetry/hatch");
        info.BuildTools = tools;
    }

    private static void DetectGitInfo(ProjectInfo info)
    {
        try
        {
            var gitDir = Path.Combine(info.ProjectRoot, ".git");
            if (!Directory.Exists(gitDir) && !File.Exists(gitDir)) return;

            var headPath = Path.Combine(gitDir, "HEAD");
            if (File.Exists(headPath))
            {
                var head = File.ReadAllText(headPath).Trim();
                // 符号引用：优先 refs/heads/ 分支名；其他 ref（tags/remotes）取最后一段。
                // 此前只判 "ref: " 却固定切 "ref: refs/heads/".Length（17），对 refs/remotes/origin/main 返回 "/origin/main"；
                // detached HEAD 的 head[..8] 无长度守卫，短 HEAD 抛 ArgumentOutOfRangeException 被吞、分支静默丢失。
                if (head.StartsWith("ref: ", StringComparison.Ordinal))
                {
                    var refName = head["ref: ".Length..];
                    info.GitBranch = refName.StartsWith("refs/heads/", StringComparison.Ordinal)
                        ? refName["refs/heads/".Length..]
                        : refName[(refName.LastIndexOf('/') + 1)..];
                }
                else
                {
                    info.GitBranch = head.Length >= 8 ? head[..8] : head;
                }
            }

            // 读取 git remote
            var configPath = Path.Combine(gitDir, "config");
            if (File.Exists(configPath))
            {
                var match = Regex.Match(File.ReadAllText(configPath), @"url\s*=\s*(\S+)");
                if (match.Success) info.GitRemote = match.Groups[1].Value;
            }
        }
        catch (Exception ex) { DebugLog.Log("ProjectContext", $"读取 Git 信息失败: {ex.Message}"); }
    }

    private static List<string> SafeGetFiles(string root, int maxFiles)
    {
        var files = new List<string>();
        try
        {
            foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(root, file);
                if (rel.StartsWith(".git") || rel.StartsWith("node_modules") ||
                    rel.StartsWith("bin") || rel.StartsWith("obj") || rel.StartsWith(".venv"))
                    continue;
                files.Add(file);
                if (files.Count >= maxFiles) break;
            }
        }
        catch (Exception ex) { DebugLog.Log("ProjectContext", $"SafeGetFiles 失败: {ex.Message}"); }
        return files;
    }
}

/// <summary>
/// 项目检测结果。
/// </summary>
public class ProjectInfo
{
    public string ProjectRoot { get; set; } = Directory.GetCurrentDirectory();
    public string PrimaryLanguage { get; set; } = "未知";
    public List<string> Languages { get; set; } = [];
    public List<string> Frameworks { get; set; } = [];
    public List<string> BuildTools { get; set; } = [];
    public string? GitBranch { get; set; }
    public string? GitRemote { get; set; }

    public string ToMarkdown()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"- 语言: {PrimaryLanguage}");
        if (Languages.Count > 0) sb.AppendLine($"- 文件分布: {string.Join(", ", Languages)}");
        if (Frameworks.Count > 0) sb.AppendLine($"- 框架: {string.Join(", ", Frameworks)}");
        if (BuildTools.Count > 0) sb.AppendLine($"- 构建: {string.Join(", ", BuildTools)}");
        if (GitBranch != null) sb.AppendLine($"- Git 分支: {GitBranch}");
        if (GitRemote != null) sb.AppendLine($"- Git Remote: {GitRemote}");
        return sb.ToString();
    }
}
