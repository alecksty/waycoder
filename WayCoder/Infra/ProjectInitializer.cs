using System.Text;

namespace WayCoder;

/// <summary>
/// 项目初始化 —— 扫描项目，生成 AGENT.md 指导文件（默认，/init claude 可改生成 CLAUDE.md）。
///
/// 复用 <see cref="ProjectContext.DetectProject"/> 的检测结果（语言/框架/构建工具/Git），
/// 再补充构建、测试、lint 命令的精确探测，拼装成中文项目指导。
/// 纯逻辑、无 IO 副作用（文件写入由 InitCommand 负责），便于自测。
/// </summary>
public static class ProjectInitializer
{
    /// <summary>
    /// 生成 AGENT.md 内容（默认；fileName 传入 "CLAUDE.md" 时生成同名标题，供 /init claude 用）。
    /// </summary>
    public static string GenerateAgentMd(ProjectInfo info, string fileName = "AGENT.md")
    {
        var projectName = Path.GetFileName(info.ProjectRoot.TrimEnd('/', '\\'));
        if (string.IsNullOrWhiteSpace(projectName)) projectName = L.Pick("项目", "Project");

        var sb = new StringBuilder();
        sb.AppendLine($"# {fileName}");
        sb.AppendLine();
        sb.AppendLine(L.Pick("本文件为都来码在此仓库中工作时提供指导。", "This file gives guidance for working in this repository with Dolaima."));
        sb.AppendLine();
        sb.AppendLine(L.Pick("## 项目概述", "## Project overview"));
        sb.AppendLine();
        sb.AppendLine(L.Pick($"- 项目名: {projectName}", $"- Project name: {projectName}"));
        sb.AppendLine(L.Pick($"- 主语言: {info.PrimaryLanguage}", $"- Primary language: {info.PrimaryLanguage}"));
        if (info.Languages.Count > 0)
            sb.AppendLine(L.Pick($"- 文件分布: {string.Join(", ", info.Languages)}", $"- File breakdown: {string.Join(", ", info.Languages)}"));
        if (info.Frameworks.Count > 0)
            sb.AppendLine(L.Pick($"- 框架: {string.Join(", ", info.Frameworks)}", $"- Frameworks: {string.Join(", ", info.Frameworks)}"));
        if (info.BuildTools.Count > 0)
            sb.AppendLine(L.Pick($"- 构建工具: {string.Join(", ", info.BuildTools)}", $"- Build tools: {string.Join(", ", info.BuildTools)}"));
        if (info.GitBranch != null)
            sb.AppendLine(L.Pick($"- Git 分支: {info.GitBranch}", $"- Git branch: {info.GitBranch}"));
        sb.AppendLine();
        sb.AppendLine(L.Pick("## 常用命令", "## Common commands"));
        sb.AppendLine();
        sb.AppendLine("```bash");
        foreach (var line in DetectCommands(info.ProjectRoot))
            sb.AppendLine(line);
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine(L.Pick("## 架构", "## Architecture"));
        sb.AppendLine();
        sb.AppendLine(L.Pick("<在此补充目录结构与关键模块说明，可运行 /repomap 查看仓库地图>", "<Describe the directory layout and key modules here; run /repomap to see the repo map>"));
        sb.AppendLine();
        sb.AppendLine(L.Pick("## 开发规范", "## Development conventions"));
        sb.AppendLine();
        sb.AppendLine(L.Pick("- 提交信息使用 conventional commits（feat/fix/docs/refactor/chore…）", "- Use conventional commits for commit messages (feat/fix/docs/refactor/chore...)"));
        sb.AppendLine(L.Pick("- 每次修改后运行测试与 lint，确保无回归", "- Run tests and lint after every change to avoid regressions"));
        sb.AppendLine(L.Pick("- 保持注释风格与现有代码一致", "- Keep the comment style consistent with the existing code"));
        sb.AppendLine();
        sb.AppendLine(L.Pick("## 注意事项", "## Notes"));
        sb.AppendLine();
        sb.AppendLine(L.Pick("<在此补充项目特有的坑、约定与边界条件>", "<Add project-specific pitfalls, conventions and boundary conditions here>"));
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
            lines.Add(L.Pick("# 构建", "# Build"));
            lines.Add(build);
            lines.Add("");
        }
        if (!string.IsNullOrEmpty(test))
        {
            lines.Add(L.Pick("# 测试", "# Test"));
            lines.Add(test);
            lines.Add("");
        }
        if (!string.IsNullOrEmpty(lint))
        {
            lines.Add(L.Pick("# 静态检查 / 格式化", "# Lint / format"));
            lines.Add(lint);
        }

        // 没有任何可识别的命令时，给一个兜底占位
        if (lines.Count == 0)
            lines.Add(L.Pick("# 未识别到构建系统，请手动补充常用命令", "# No build system detected; add the common commands manually"));

        return lines;
    }

    /// <summary>
    /// 检测构建命令 —— **唯一真源**（<c>Agent.Feedback</c> 与 <c>/init</c> 都转调这里）。
    /// 此前两份各写一遍且答案不同（`npm install &amp;&amp; npm run build` 无条件跑 vs 仅当声明了
    /// build 脚本），于是 `/init` 写进 AGENT.md 的命令 ≠ Agent 实际执行的命令。
    /// 取**要求 npm 显式声明 build 脚本**的一侧（更准，避免对纯库项目乱跑 build），并统一静默形式。
    /// </summary>
    internal static string? DetectBuildCommand(string root)
    {
        if (Has(root, ".csproj") || Has(root, ".sln")) return "dotnet build --nologo -v q";
        if (File.Exists(Path.Combine(root, "package.json")))
        {
            try
            {
                if (Json.Parse(File.ReadAllText(Path.Combine(root, "package.json")))?["scripts"]?["build"] != null)
                    return "npm run build --silent";
            }
            catch { /* 解析失败则跳过 */ }
            return null; // 没声明 build 脚本 = 无构建步骤
        }
        if (File.Exists(Path.Combine(root, "go.mod"))) return "go build ./...";
        if (File.Exists(Path.Combine(root, "Cargo.toml"))) return "cargo build -q";
        if (File.Exists(Path.Combine(root, "pyproject.toml")) ||
            File.Exists(Path.Combine(root, "requirements.txt")))
            return "pip install -r requirements.txt";
        if (File.Exists(Path.Combine(root, "Makefile"))) return "make";
        return null;
    }

    /// <summary>检测测试命令（无用户覆盖）。</summary>
    internal static string? DetectTestCommand(string root) => DetectTestCommand(root, null);

    /// <summary>
    /// 检测测试命令 —— **唯一真源**（<c>Agent.Feedback</c> 与 <c>/init</c> 都转调这里）。
    ///
    /// 此前两份各写一遍且**已经给出不同答案**（`dotnet test` vs `dotnet test --nologo -v q`、
    /// `npm test` vs `npm test --silent`、`pytest` vs `python -m pytest -q`、`cargo test` vs
    /// `cargo test -q`），于是 `/init` 写进 AGENT.md 的命令与 Agent 自检实际执行的命令各说各话。
    /// 统一取**静默形式**（Agent 侧原本就是它，且输出噪音最小、最省 token）。
    /// </summary>
    /// <param name="userOverride">用户显式指定的测试命令（Config.TestCommand，测试驱动修复用）——最高优先级。</param>
    internal static string? DetectTestCommand(string root, string? userOverride)
    {
        if (!string.IsNullOrWhiteSpace(userOverride)) return userOverride;

        // WayCoder 自测（内置 SelfTest）
        if (File.Exists(Path.Combine(root, "SelfTest.cs")))
            return "dotnet run -c Release -- --test";

        // .NET 测试项目
        if (GlobAny(root, "*.Tests.csproj") || GlobAny(root, "*.Test.csproj"))
            return "dotnet test --nologo -v q";

        // Node.js
        if (File.Exists(Path.Combine(root, "package.json")))
        {
            try
            {
                var pkg = Json.Parse(
                    File.ReadAllText(Path.Combine(root, "package.json")));
                if (pkg?["scripts"]?["test"] != null)
                    return "npm test --silent";
            }
            catch { /* 解析失败则跳过 */ }
        }

        // Go
        if (File.Exists(Path.Combine(root, "go.mod"))) return "go test ./...";

        // Rust
        if (File.Exists(Path.Combine(root, "Cargo.toml"))) return "cargo test -q";

        // Python
        if (GlobAny(root, "test_*.py") || GlobAny(root, "*_test.py"))
            return "python -m pytest -q";

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
    /// <summary>目录树里是否存在匹配 <paramref name="pattern"/> 的文件。
    ///
    /// 走 <see cref="FileWalker"/> 而非 <c>EnumerateFiles(..., AllDirectories)</c>：后者**无预算**，
    /// 且原先用 `rel.StartsWith("bin")` 前缀匹配（漏 `src/bin`、误伤 `binary…`）——
    /// 正是 CLAUDE.md 记的「home 下 12~36s」那类无界递归。收满 1 个即停。</summary>
    private static bool GlobAny(string root, string pattern)
    {
        var hits = new List<string>();
        try
        {
            Tools.FileWalker.Walk(root, hits, 1, (d, res) =>
            {
                foreach (var _ in Directory.EnumerateFiles(d, pattern))
                {
                    res.Add(_);
                    return;
                }
            });
        }
        catch { /* 无权限等异常视为不存在 */ }
        return hits.Count > 0;
    }
}
