using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WayCoder.Tools;

/// <summary>
/// Lint / 静态检查工具 — 对指定文件或目录运行对应语言的 linter，
/// 返回检查结果，供 Agent 形成修复闭环。
/// </summary>
public class LintTool : ITool
{
    public string Name => "lint";
    public string Description => "对指定文件或目录运行静态检查（lint/编译检查），返回错误和警告列表。支持 C#、Python、JS/TS、Go、Rust、Java、C/C++、Ruby、PHP、Swift、Kotlin、Lua、Shell、CSS、Vue 等。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["path"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "要检查的文件或目录路径。留空则检查当前目录。"
            }
        }
    };

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var rawPath = arguments.GetValueOrDefault("path")?.ToString();
        var path = string.IsNullOrWhiteSpace(rawPath) ? Environment.CurrentDirectory : rawPath;

        // 解析相对路径
        if (!Path.IsPathRooted(path))
            path = Path.GetFullPath(path);

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
            "cs" => ("dotnet", $"build --nologo -v q \"{target}\""),
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
        // Fallback: return target as-is
        return target;
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
        return ("python", $"-c \"import json, sys; json.load(open('{target.Replace("'", "\\'")}', encoding='utf-8')); print('✅ JSON 有效')\"");
    }

    private static (string, string) CheckToml(string target)
    {
        return ("python", $"-c \"import sys; sys.path.insert(0,'.'); __import__('tomllib').load(open('{target.Replace("'", "\\'")}', 'rb'))\"");
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
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                }
            };

            proc.Start();
            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();

            var lintTimeout = Config.FromEnv().LintTimeoutSec * 1000;
            var exitTask = proc.WaitForExitAsync();
            var delayTask = Task.Delay(lintTimeout);
            var completed = await Task.WhenAny(exitTask, delayTask);
            if (completed != exitTask || !exitTask.IsCompletedSuccessfully)
            {
                try { proc.Kill(); } catch { }
                return $"Lint 超时（{Config.FromEnv().LintTimeoutSec} 秒）: {cmd} {args}";
            }

            var output = await stdoutTask;
            var errors = await stderrTask;

            // 去除 ANSI 转义序列
            output = Terminal.AnsiString.StripWithRegex(output);
            errors = Terminal.AnsiString.StripWithRegex(errors);

            var combined = (output + "\n" + errors).Trim();
            if (combined.Length == 0)
                return $"✅ {lang}: 无问题";

            // 截断
            if (combined.Length > 4000)
                combined = combined[..4000] + "\n... (输出已截断)";

            return proc.ExitCode == 0
                ? $"✅ {lang}: 检查通过\n{combined}"
                : $"❌ {lang}: 发现问题 ({proc.ExitCode})\n{combined}";
        }
        catch (Exception ex)
        {
            // 工具未安装时的友好提示
            return $"⚠ {lang}: 无法运行 {cmd} — {ex.Message}\n提示: 请确认 {cmd} 已安装并在 PATH 中。";
        }
    }
}
