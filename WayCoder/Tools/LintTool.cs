using System.Diagnostics;
using System.Text.RegularExpressions;
using Terminal = WayCoder.UI.Shared.Terminal;

namespace WayCoder.Tools;

/// <summary>
/// Lint / 静态检查工具 — 对指定文件或目录运行对应语言的 linter，
/// 返回检查结果，供 Agent 形成修复闭环。
/// </summary>
public class LintTool : ITool
{
    public string Name => "lint";
    public string Description => "对指定文件或目录运行静态检查（lint/编译检查），返回错误和警告列表。支持 C#、Python、JS/TS、Go、Rust、Java、C/C++、Ruby、PHP、Swift、Kotlin、Lua、Shell、CSS、Vue 等。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("path", JNode.Object()
                .Set("type", "string")
                .Set("description", "要检查的文件或目录路径。留空则检查当前目录。")));

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var rawPath = arguments.GetValueOrDefault("path")?.ToString();
        var path = string.IsNullOrWhiteSpace(rawPath) ? (CwdContext.Current.Value ?? Environment.CurrentDirectory) : rawPath;

        // 解析相对路径
        if (!Path.IsPathRooted(path))
            path = Path.GetFullPath(path, CwdContext.Current.Value ?? Directory.GetCurrentDirectory()); // cd 后相对路径基于被跟踪工作目录

        if (!File.Exists(path) && !Directory.Exists(path))
            return $"错误: 路径不存在: {path}";

        var lang = DetectLanguage(path);
        if (lang == null)
            return $"未能识别语言: {path}\n支持的文件类型: .cs .py .js .ts .go .rs .java .c .cpp .rb .php .swift .kt .lua .sh .html .css .vue .yaml .json .md .dart .r .toml .sql";

        try
        {
            return await RunLinter(lang, path);
        }
        catch (Exception ex)
        {
            return $"Lint 执行异常: {ex.GetType().Name}: {ex.Message}";
        }
    }

    /// <summary>
    /// 根据文件扩展名或项目文件检测编程语言。
    /// 返回语言标识符，未识别时返回 null。
    /// </summary>
    public static string? DetectLanguage(string path)
    {
        // 如果是目录，检测项目文件
        if (Directory.Exists(path))
        {
            var files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly)
                .Select(f => Path.GetFileName(f).ToLowerInvariant())
                .ToHashSet();

            if (files.Any(f => f.EndsWith(".csproj") || f.EndsWith(".sln"))) return "cs";
            if (files.Contains("pyproject.toml") || files.Contains("setup.py") || files.Contains("requirements.txt")) return "py";
            if (files.Contains("package.json")) return "js";
            if (files.Contains("go.mod")) return "go";
            if (files.Contains("cargo.toml")) return "rs";
            if (files.Contains("pom.xml") || files.Contains("build.gradle") || files.Contains("build.gradle.kts")) return "java";
            if (files.Contains("makefile") || files.Contains("cmakelists.txt")) return "c";
            if (files.Contains("gemfile")) return "ruby";
            if (files.Contains("composer.json")) return "php";
            if (files.Contains("package.swift")) return "swift";
            if (files.Contains("pubspec.yaml")) return "dart";
            if (files.Contains("cmakelists.txt")) return "cpp";
            if (files.Contains("mix.exs")) return "elixir";

            // 回退：按目录中的主要扩展名判断
            var exts = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly)
                .Select(f => Path.GetExtension(f).ToLowerInvariant())
                .Where(e => e.Length > 0)
                .GroupBy(e => e)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;

            if (exts != null)
                return ExtToLang(exts);

            return null;
        }

        // 文件：按扩展名
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ExtToLang(ext);
    }

    /// <summary>
    /// 扩展名 → 语言标识符映射。支持 25+ 种常见语言。
    /// </summary>
    private static string? ExtToLang(string ext)
    {
        return ext switch
        {
            // C# / .NET
            ".cs" => "cs",
            ".csproj" => "cs",
            ".sln" => "cs",
            ".csx" => "cs",
            ".vb" => "cs",       // VB.NET → dotnet build

            // Python
            ".py" => "py",
            ".pyi" => "py",
            ".pyx" => "py",

            // JavaScript
            ".js" => "js",
            ".jsx" => "js",
            ".mjs" => "js",
            ".cjs" => "js",

            // TypeScript
            ".ts" => "ts",
            ".tsx" => "ts",
            ".mts" => "ts",
            ".cts" => "ts",

            // Go
            ".go" => "go",

            // Rust
            ".rs" => "rs",

            // Java
            ".java" => "java",
            ".kt" => "kotlin",
            ".kts" => "kotlin",

            // C / C++
            ".c" => "c",
            ".h" => "c",
            ".cpp" => "cpp",
            ".cc" => "cpp",
            ".cxx" => "cpp",
            ".c++" => "cpp",
            ".hpp" => "cpp",
            ".hxx" => "cpp",
            ".h++" => "cpp",

            // Ruby
            ".rb" => "ruby",

            // PHP
            ".php" => "php",
            ".phtml" => "php",

            // Swift
            ".swift" => "swift",

            // Lua
            ".lua" => "lua",

            // Shell
            ".sh" => "shell",
            ".bash" => "shell",
            ".zsh" => "shell",

            // Web
            ".html" => "html",
            ".htm" => "html",
            ".css" => "css",
            ".scss" => "css",
            ".sass" => "css",
            ".less" => "css",
            ".vue" => "vue",
            ".svelte" => "vue",

            // Data / Config
            ".yaml" => "yaml",
            ".yml" => "yaml",
            ".json" => "json",
            ".xml" => "xml",
            ".toml" => "toml",
            ".ini" => "toml",
            ".cfg" => "toml",

            // Markdown
            ".md" => "markdown",
            ".mdx" => "markdown",

            // Dart
            ".dart" => "dart",

            // R
            ".r" => "r",
            ".rmd" => "r",

            // SQL
            ".sql" => "sql",

            // Objective-C
            ".m" => "c",
            ".mm" => "cpp",

            // Perl
            ".pl" => "perl",
            ".pm" => "perl",

            // Scala
            ".scala" => "java",

            // Elixir
            ".ex" => "elixir",
            ".exs" => "elixir",

            // Haskell
            ".hs" => "haskell",

            // Clojure
            ".clj" => "lisp",
            ".cljs" => "lisp",
            ".edn" => "lisp",

            // Zig
            ".zig" => "zig",

            _ => null
        };
    }

    /// <summary>
    /// 运行对应语言的 linter，返回 stdout + stderr（截断后）。
    /// </summary>
    private static async Task<string> RunLinter(string lang, string target)
    {
        var (cmd, args) = lang switch
        {
            "cs" => ("dotnet", $"build --nologo -v q \"{FindAnyProjectUp(target)}\""),
            "py" => ("ruff", $"check \"{target}\""),
            "js" or "ts" => ("npx", $"eslint \"{target}\" --format stylish"),
            "go" => ("go", $"vet \"{target}\""),
            "rs" => ("cargo", $"check --manifest-path \"{FindProjectFile(target, "Cargo.toml")}\""),
            "java" => FindJavaLinter(target),
            "c" => ("gcc", $"-fsyntax-only -Wall \"{target}\""),
            "cpp" => ("g++", $"-fsyntax-only -Wall \"{target}\""),
            "ruby" => ("ruby", $"-c \"{target}\""),
            "php" => ("php", $"-l \"{target}\""),
            "swift" => ("swift", $"-typecheck \"{target}\""),
            "kotlin" => ("kotlinc", $"-Werror \"{target}\""),
            "lua" => ("luac", $"-p \"{target}\""),
            "shell" => ("shellcheck", $"\"{target}\""),
            "html" => ("npx", $"htmlhint \"{target}\""),
            "css" => ("npx", $"stylelint \"{target}\""),
            "vue" => ("npx", $"eslint \"{target}\" --format stylish"),
            "yaml" => ("yamllint", $"\"{target}\""),
            "json" => CheckJson(target),
            "xml" => ("xmllint", $"--noout \"{target}\""),
            "markdown" => ("npx", $"markdownlint \"{target}\""),
            "toml" => CheckToml(target),
            "dart" => ("dart", $"analyze \"{target}\""),
            "r" => ("R", $"-e 'parse(file=\"{target}\")'"),
            "sql" => CheckSql(target),
            "perl" => ("perl", $"-c \"{target}\""),
            "elixir" => ("mix", $"format --check-formatted \"{target}\""),
            "haskell" => ("ghc", $"-fno-code \"{target}\""),
            "zig" => ("zig", $"ast-check \"{target}\""),
            _ => ("echo", "\"（无可用 linter）\"")
        };

        return await RunProcess(cmd, args, lang);
    }

    /// <summary>从目标文件/目录向上找任意 .csproj（C# 项目文件名不固定，不能按精确名匹配）。未找到返回空串。</summary>
    private static string FindAnyProjectUp(string target)
    {
        var dir = File.Exists(target) ? Path.GetDirectoryName(target)! : target;
        while (dir != null)
        {
            var csproj = Directory.GetFiles(dir, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (csproj != null) return csproj;
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return "";
    }

    private static string FindProjectFile(string target, string filename)
    {
        if (File.Exists(target) && Path.GetFileName(target).Equals(filename, StringComparison.OrdinalIgnoreCase))
            return target;

        var dir = File.Exists(target) ? Path.GetDirectoryName(target)! : target;
        while (dir != null)
        {
            var candidate = Path.Combine(dir, filename);
            if (File.Exists(candidate)) return candidate;
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        // Fallback：返回空串表示未找到（不能返回 target——File.Exists(target) 对文件目标恒为 true，
        // 会使 FindJavaLinter 永远命中 gradle 分支、maven/javac 回退不可达）
        return "";
    }

    private static (string, string) FindJavaLinter(string target)
    {
        // Try gradle first, then maven, then javac
        var buildFile = FindProjectFile(target, "build.gradle");
        if (File.Exists(buildFile) || FindProjectFile(target, "build.gradle.kts") is string gk && File.Exists(gk))
            return ("gradle", $"check -x test -q");
        var pom = FindProjectFile(target, "pom.xml");
        if (File.Exists(pom))
            return ("mvn", $"compile -q");
        return ("javac", $"-Xlint:all -proc:none \"{target}\"");
    }

    private static (string, string) CheckJson(string target)
    {
        // Use python for JSON validation if available
        // Windows 路径含反斜杠，在 Python 字符串字面量里 \f/\b/\U 等是转义序列 → 路径错乱，
        // 统一转正斜杠（Python 在 Windows 下接受）
        return (CrossPlatform.PythonExecutable, $"-c \"import json, sys; json.load(open('{target.Replace('\\', '/').Replace("'", "\\'")}', encoding='utf-8')); print('✅ JSON 有效')\"");
    }

    private static (string, string) CheckToml(string target)
    {
        return (CrossPlatform.PythonExecutable, $"-c \"import sys; sys.path.insert(0,'.'); __import__('tomllib').load(open('{target.Replace('\\', '/').Replace("'", "\\'")}', 'rb'))\"");
    }

    private static (string, string) CheckSql(string target)
    {
        // Best-effort: use sqlite3 to parse
        return ("sqlite3", $":memory: \".read '{target.Replace("'", "\\'")}'\"");
    }

    private static async Task<string> RunProcess(string cmd, string args, string lang)
    {
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = cmd,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true, // 不共享主控台 stdin（ProcUtil 启动后置 EOF，防 TUI ReadKey 竞态）
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                }
            };

            var r = await WayCoder.Infra.ProcUtil.RunAsync(proc.StartInfo, Config.Instance.LintTimeoutSec * 1000);
            if (r == null) return $"Lint 超时（{Config.Instance.LintTimeoutSec} 秒）: {cmd} {args}";
            var (exitCode, output, errors) = r.Value;

            // 去除 ANSI 转义序列
            output = Terminal.AnsiString.StripWithRegex(output);
            errors = Terminal.AnsiString.StripWithRegex(errors);

            var combined = (output + "\n" + errors).Trim();
            if (combined.Length == 0)
                return $"✅ {lang}: 无问题";

            // 截断（项目级构建输出量大，4000 会把目标文件的错误行截掉导致 lint 静默失效；
            // 提到 20000 保证 ParseLintOutput 按文件名能捞到目标诊断）
            if (combined.Length > 20000)
                combined = ContextManager.TruncateByRunes(combined, 20000) + "\n... (输出已截断)";

            // 注意：proc 是"未启动的占位 Process"（ProcUtil.RunAsync 内部另建并启动 Process），
            // 读 proc.ExitCode 会抛 "No process is associated" → 用 ProcUtil 返回的 exitCode。
            return exitCode == 0
                ? $"✅ {lang}: 检查通过\n{combined}"
                : $"❌ {lang}: 发现问题 ({exitCode})\n{combined}";
        }
        catch (Exception ex)
        {
            // 工具未安装时的友好提示
            return $"⚠ {lang}: 无法运行 {cmd} — {ex.Message}\n提示: 请确认 {cmd} 已安装并在 PATH 中。";
        }
    }
}
