using System.Text;

namespace WayCoder;

/// <summary>
/// 项目初始化 —— 对标 Claude Code /init：扫描项目，生成 CLAUDE.md 指导文件。
///
/// 复用 <see cref="ProjectContext.DetectProject"/> 的检测结果（语言/框架/构建工具/Git），
/// 再补充构建、测试、lint 命令的精确探测，拼装成中文项目指导。
/// 纯逻辑、无 IO 副作用（文件写入由 InitCommand 负责），便于自测。
/// </summary>
public static class ProjectInitializer
{
    /// <summary>
    /// 生成 CLAUDE.md 内容（对标 Claude Code /init 的默认结构）。
    /// </summary>
    public static string GenerateClaudeMd(ProjectInfo info)
    {
        var projectName = Path.GetFileName(info.ProjectRoot.TrimEnd('/', '\\'));
        if (string.IsNullOrWhiteSpace(projectName)) projectName = "项目";

        var sb = new StringBuilder();
        sb.AppendLine("# CLAUDE.md");
        sb.AppendLine();
        sb.AppendLine("本文件为 WayCoder（道码）在此仓库中工作时提供指导。");
        sb.AppendLine();
        sb.AppendLine("## 项目概述");
        sb.AppendLine();
        sb.AppendLine($"- 项目名: {projectName}");
        sb.AppendLine($"- 主语言: {info.PrimaryLanguage}");
        if (info.Languages.Count > 0)
            sb.AppendLine($"- 文件分布: {string.Join(", ", info.Languages)}");
        if (info.Frameworks.Count > 0)
            sb.AppendLine($"- 框架: {string.Join(", ", info.Frameworks)}");
        if (info.BuildTools.Count > 0)
            sb.AppendLine($"- 构建工具: {string.Join(", ", info.BuildTools)}");
        if (info.GitBranch != null)
            sb.AppendLine($"- Git 分支: {info.GitBranch}");
        sb.AppendLine();
        sb.AppendLine("## 常用命令");
        sb.AppendLine();
        sb.AppendLine("```bash");
        foreach (var line in DetectCommands(info.ProjectRoot))
            sb.AppendLine(line);
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## 架构");
        sb.AppendLine();
        sb.AppendLine("<在此补充目录结构与关键模块说明，可运行 /repomap 查看仓库地图>");
        sb.AppendLine();
        sb.AppendLine("## 开发规范");
        sb.AppendLine();
        sb.AppendLine("- 提交信息使用 conventional commits（feat/fix/docs/refactor/chore…）");
        sb.AppendLine("- 每次修改后运行测试与 lint，确保无回归");
        sb.AppendLine("- 保持注释风格与现有代码一致");
        sb.AppendLine();
        sb.AppendLine("## 注意事项");
        sb.AppendLine();
        sb.AppendLine("<在此补充项目特有的坑、约定与边界条件>");
        return sb.ToString();
    }

    /// <summary>
    /// 拼装构建/测试/lint 命令块（带中文注释分组）。
    /// </summary>
    internal static List<string> DetectCommands(string root)
    {
        var lines = new List<string>();

        var build = DetectBuildCommand(root);
        var test = DetectTestCommand(root);
        var lint = DetectLintCommand(root);

        if (!string.IsNullOrEmpty(build))
        {
            lines.Add("# 构建");
            lines.Add(build);
            lines.Add("");
        }
        if (!string.IsNullOrEmpty(test))
        {
            lines.Add("# 测试");
            lines.Add(test);
            lines.Add("");
        }
        if (!string.IsNullOrEmpty(lint))
        {
            lines.Add("# 静态检查 / 格式化");
            lines.Add(lint);
        }

        // 没有任何可识别的命令时，给一个兜底占位
        if (lines.Count == 0)
            lines.Add("# 未识别到构建系统，请手动补充常用命令");

        return lines;
    }

    /// <summary>检测构建命令。</summary>
    internal static string? DetectBuildCommand(string root)
    {
        if (Has(root, ".csproj") || Has(root, ".sln")) return "dotnet build";
        if (File.Exists(Path.Combine(root, "package.json"))) return "npm install && npm run build";
        if (File.Exists(Path.Combine(root, "go.mod"))) return "go build ./...";
        if (File.Exists(Path.Combine(root, "Cargo.toml"))) return "cargo build";
        if (File.Exists(Path.Combine(root, "pyproject.toml")) ||
            File.Exists(Path.Combine(root, "requirements.txt")))
            return "pip install -r requirements.txt";
        if (File.Exists(Path.Combine(root, "Makefile"))) return "make";
        return null;
    }

    /// <summary>检测测试命令（对标 Agent.DetectTestCommand 的思路）。</summary>
    internal static string? DetectTestCommand(string root)
    {
        // WayCoder 自测（内置 SelfTest）
        if (File.Exists(Path.Combine(root, "SelfTest.cs")))
            return "dotnet run -c Release -- --test";

        // .NET 测试项目
        if (GlobAny(root, "*.Tests.csproj") || GlobAny(root, "*.Test.csproj"))
            return "dotnet test";

        // Node.js
        if (File.Exists(Path.Combine(root, "package.json")))
        {
            try
            {
                var pkg = Json.Parse(
                    File.ReadAllText(Path.Combine(root, "package.json")));
                if (pkg?["scripts"]?["test"] != null)
                    return "npm test";
            }
            catch { /* 解析失败则跳过 */ }
        }

        // Go
        if (File.Exists(Path.Combine(root, "go.mod"))) return "go test ./...";

        // Rust
        if (File.Exists(Path.Combine(root, "Cargo.toml"))) return "cargo test";

        // Python
        if (GlobAny(root, "test_*.py") || GlobAny(root, "*_test.py"))
            return "pytest";

        return null;
    }

    /// <summary>检测 lint / 格式化命令。</summary>
    internal static string? DetectLintCommand(string root)
    {
        if (Has(root, ".csproj")) return "dotnet format";
        if (File.Exists(Path.Combine(root, "go.mod"))) return "go vet ./...";
        if (File.Exists(Path.Combine(root, "Cargo.toml"))) return "cargo clippy";
        if (File.Exists(Path.Combine(root, "pyproject.toml"))) return "ruff check .";
        if (File.Exists(Path.Combine(root, "package.json")))
        {
            try
            {
                var pkg = Json.Parse(
                    File.ReadAllText(Path.Combine(root, "package.json")));
                if (pkg?["scripts"]?["lint"] != null)
                    return "npm run lint";
            }
            catch { /* 解析失败则跳过 */ }
        }
        return null;
    }

    /// <summary>root 下是否存在指定扩展名的文件（递归，最多 50 个）。</summary>
    private static bool Has(string root, string ext)
        => GlobAny(root, "*" + ext);

    /// <summary>root 下是否存在匹配 pattern 的文件（递归，跳过 bin/obj/node_modules/.git）。</summary>
    private static bool GlobAny(string root, string pattern)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(root, file);
                if (rel.StartsWith(".git") || rel.StartsWith("node_modules") ||
                    rel.StartsWith("bin") || rel.StartsWith("obj") || rel.StartsWith(".venv"))
                    continue;
                return true;
            }
        }
        catch { /* 无权限等异常视为不存在 */ }
        return false;
    }
}
