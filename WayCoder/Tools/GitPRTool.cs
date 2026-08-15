using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WayCoder.Tools;

/// <summary>
/// Git PR 工具 — 创建分支、推送、生成 PR 链接。
/// 自动检测 GitHub / Gitee，无需额外配置。
///
/// 工作流程：
///   1. git_pr create "标题" "描述"  → 创建分支 → 提交变更 → 推送 → 生成 PR 链接
///   2. git_pr push                     → 仅推送当前分支
///   3. git_pr url "base分支"           → 仅生成 PR 创建链接
/// </summary>
public class GitPRTool : ITool
{
    public string Name => "git_pr";
    public string Description => "创建 Pull Request：自动创建分支、推送并生成 PR 链接。支持 GitHub / Gitee。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("action", JNode.Object()
                .Set("type", "string")
                .Set("description", "操作：create（创建 PR）、push（仅推送）、url（仅生成链接）"))
            .Set("title", JNode.Object()
                .Set("type", "string")
                .Set("description", "PR 标题（create 操作需要）"))
            .Set("description", JNode.Object()
                .Set("type", "string")
                .Set("description", "PR 描述（可选，支持 Markdown）"))
            .Set("base_branch", JNode.Object()
                .Set("type", "string")
                .Set("description", "目标分支（默认 master 或 main）")))
        .Set("required", JNode.Array().Add("action"));

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var action = arguments.GetValueOrDefault("action")?.ToString() ?? "create";
        var title = arguments.GetValueOrDefault("title")?.ToString() ?? "";
        var description = arguments.GetValueOrDefault("description")?.ToString() ?? "";
        var baseBranch = arguments.GetValueOrDefault("base_branch")?.ToString();

        return await Task.Run(() => Execute(action, title, description, baseBranch));
    }

    private static string Execute(string action, string title, string description, string? baseBranch)
    {
        return action switch
        {
            "create" => CreatePR(title, description, baseBranch),
            "push" => PushCurrentBranch(),
            "url" => GeneratePRUrl(baseBranch),
            _ => "错误：未知操作。支持：create, push, url",
        };
    }

    // ---- PR 创建 ----

    private static string CreatePR(string title, string description, string? baseBranch)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "错误：create 操作需要 --title 参数";

        var repoRoot = FindGitRoot();
        if (repoRoot == null)
            return "错误：未找到 Git 仓库";

        // 1. 检测远程和平台
        var (remoteUrl, platform, owner, repo) = DetectRemote(repoRoot);
        if (remoteUrl == null)
            return "错误：未配置 Git 远程仓库";

        // 2. 获取当前分支
        var currentBranch = RunGit(repoRoot, "rev-parse --abbrev-ref HEAD").Trim();
        if (string.IsNullOrEmpty(currentBranch) || currentBranch == "HEAD")
            return "错误：无法获取当前分支名（可能是 detached HEAD）";

        baseBranch ??= DetectDefaultBranch(repoRoot);

        if (currentBranch == baseBranch)
            return $"错误：当前在 {baseBranch} 分支，请先创建功能分支：`git checkout -b feature/xxx`";

        // 3. 检查是否有未推送的提交
        var unpushed = RunGit(repoRoot, $"log {baseBranch}..{currentBranch} --oneline").Trim();
        if (string.IsNullOrEmpty(unpushed))
            return $"提示：{currentBranch} 分支没有相对于 {baseBranch} 的新提交。\n是否已推送？使用 `git push -u origin {currentBranch}`";

        // 4. 推送
        var pushResult = RunGit(repoRoot, $"push -u origin {currentBranch} 2>&1");
        var pushed = !pushResult.Contains("fatal") && !pushResult.Contains("error");

        // 5. 生成 PR URL
        var prUrl = BuildPRUrl(platform, owner, repo, currentBranch, baseBranch, title, description);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(pushed
            ? $"✅ 已推送到 origin/{currentBranch}"
            : $"⚠ 推送可能失败：\n{pushResult}");
        sb.AppendLine();
        sb.AppendLine("## 创建 Pull Request");
        sb.AppendLine();
        sb.AppendLine($"🔗 {prUrl}");
        sb.AppendLine();
        sb.AppendLine($"源分支: {currentBranch} → 目标分支: {baseBranch}");
        sb.AppendLine($"标题: {title}");
        if (!string.IsNullOrEmpty(description))
            sb.AppendLine($"描述: {description[..Math.Min(description.Length, 200)]}");

        return sb.ToString();
    }

    // ---- 仅推送 ----

    private static string PushCurrentBranch()
    {
        var repoRoot = FindGitRoot();
        if (repoRoot == null) return "错误：未找到 Git 仓库";

        var branch = RunGit(repoRoot, "rev-parse --abbrev-ref HEAD").Trim();
        var result = RunGit(repoRoot, $"push -u origin {branch} 2>&1");

        return result.Contains("fatal") || result.Contains("error")
            ? $"推送失败：\n{result}"
            : $"✅ 已推送到 origin/{branch}";
    }

    // ---- 仅生成 URL ----

    private static string GeneratePRUrl(string? baseBranch)
    {
        var repoRoot = FindGitRoot();
        if (repoRoot == null) return "错误：未找到 Git 仓库";

        var (remoteUrl, platform, owner, repo) = DetectRemote(repoRoot);
        if (remoteUrl == null) return "错误：未配置 Git 远程仓库";

        var currentBranch = RunGit(repoRoot, "rev-parse --abbrev-ref HEAD").Trim();
        baseBranch ??= DetectDefaultBranch(repoRoot);

        var url = BuildPRUrl(platform, owner, repo, currentBranch, baseBranch, "", "");
        return $"🔗 PR 创建链接:\n{url}";
    }

    // ---- 内部辅助方法 ----

    private static string? FindGitRoot()
    {
        var dir = Environment.CurrentDirectory;
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, ".git")))
                return dir;
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return null;
    }

    private static string DetectDefaultBranch(string repoRoot)
    {
        // 检查本地分支
        var branches = RunGit(repoRoot, "branch").Trim();
        if (branches.Contains("main")) return "main";
        if (branches.Contains("master")) return "master";

        // 检查远程默认分支
        try
        {
            var remote = RunGit(repoRoot, "remote show origin 2>&1");
            var m = Regex.Match(remote, @"HEAD branch:\s*(\S+)");
            if (m.Success) return m.Groups[1].Value;
        }
        catch { }

        return "master";
    }

    private static (string? url, string platform, string owner, string repo) DetectRemote(string repoRoot)
    {
        var remoteUrl = RunGit(repoRoot, "remote get-url origin 2>&1").Trim();
        if (string.IsNullOrEmpty(remoteUrl) || remoteUrl.Contains("fatal"))
            return (null, "", "", "");

        // 解析平台和仓库信息
        // GitHub: https://github.com/owner/repo.git 或 git@github.com:owner/repo.git
        // Gitee:  https://gitee.com/owner/repo.git 或 git@gitee.com:owner/repo.git

        var platform = "";
        var owner = "";
        var repo = "";

        // SSH 格式: git@platform:owner/repo.git
        var sshMatch = Regex.Match(remoteUrl, @"git@([^:]+):(.+?)/(.+?)(?:\.git)?$");
        if (sshMatch.Success)
        {
            var host = sshMatch.Groups[1].Value;
            platform = host.Contains("gitee") ? "gitee" : "github";
            owner = sshMatch.Groups[2].Value;
            repo = sshMatch.Groups[3].Value;
        }
        else
        {
            // HTTPS 格式: https://platform/owner/repo.git
            var httpsMatch = Regex.Match(remoteUrl, @"https?://([^/]+)/(.+?)/(.+?)(?:\.git)?$");
            if (httpsMatch.Success)
            {
                var host = httpsMatch.Groups[1].Value;
                platform = host.Contains("gitee") ? "gitee" : "github";
                owner = httpsMatch.Groups[2].Value;
                repo = httpsMatch.Groups[3].Value;
            }
        }

        return (remoteUrl, platform, owner, repo);
    }

    private static string BuildPRUrl(string platform, string owner, string repo,
        string head, string baseBranch, string title, string description)
    {
        var encodedTitle = Uri.EscapeDataString(title);
        var encodedDesc = string.IsNullOrEmpty(description) ? "" : Uri.EscapeDataString(description);
        var encodedHead = Uri.EscapeDataString(head);

        return platform switch
        {
            "gitee" => $"https://gitee.com/{owner}/{repo}/compare/{baseBranch}...{head}",
            _ => $"https://github.com/{owner}/{repo}/compare/{baseBranch}...{head}?" +
                 $"title={encodedTitle}&body={encodedDesc}",
        };
    }

    private static string RunGit(string workingDir, string arguments)
    {
        return GitRunner.Output(arguments, workingDir);
    }
}
