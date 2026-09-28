using System.Text;

namespace WayCoder.Git;

/// <summary>
/// git 分支管理：list / create / delete / checkout（切换）/ merge（fast-forward）。
/// 复用 GitCore 的 loose-object 对象模型与内部 helper（ReadCommitInfo/ResolveBranch/
/// ListBranches/SetHeadBranch/FlattenTree/CheckoutWorktree）。
///
/// MVP 取舍：
///   - merge 仅支持 fast-forward（当前分支是目标分支的祖先时直接前移），
///     已分叉的情况返回提示 + 建议用 /git diff 对比差异，不做 3-way 自动合并。
/// </summary>
public static class GitBranch
{
    // ═══════════════════════════════════════════════════════════
    //  branch —— list / create / delete
    // ═══════════════════════════════════════════════════════════

    public static string Branch(string repoRoot, string[] rest)
    {
        var gitDir = Path.Combine(repoRoot, ".git");
        if (rest.Length == 0) return List(gitDir);
        if (rest[0] is "-d" or "--delete" or "-D")
            return rest.Length < 2 ? L.Pick("用法：/git branch -d <name>", "Usage: /git branch -d <name>") : Delete(gitDir, rest[1]);
        return Create(gitDir, rest[0]);
    }

    static string List(string gitDir)
    {
        var current = GitCore.ReadHeadBranch(gitDir);
        var branches = GitCore.ListBranches(gitDir);
        if (branches.Count == 0) return L.Pick("（尚无分支）", "(no branches yet)");

        var sb = new StringBuilder();
        foreach (var name in branches)
        {
            var refPath = Path.Combine(gitDir, "refs", "heads", name);
            var sha = File.Exists(refPath) ? File.ReadAllText(refPath).Trim() : null;
            var marker = name == current ? "* " : "  ";
            var info = sha != null ? GitCore.ReadCommitInfo(gitDir, sha) : null;
            var msg = info != null ? info.Value.Message.Split('\n')[0] : L.Pick("（无提交）", "(no commit)");
            sb.Append(marker).Append(name);
            if (sha != null) sb.Append(" @ ").Append(sha[..7]);
            sb.Append("  ").Append(TruncateByRunes(msg, 40)).Append('\n');
        }
        return sb.ToString().TrimEnd('\n');
    }

    static string Create(string gitDir, string name)
    {
        var head = GitCore.ReadHeadCommit(gitDir);
        if (head == null) return L.Pick("⚠ 当前无提交，无法创建分支（先 git commit）。",
            "⚠ No commits yet, cannot create a branch (run git commit first).");

        var headsDir = Path.Combine(gitDir, "refs", "heads");
        Directory.CreateDirectory(headsDir);
        var refPath = Path.Combine(headsDir, name);
        if (File.Exists(refPath)) return L.Pick($"⚠ 分支 {name} 已存在。", $"⚠ Branch {name} already exists.");
        File.WriteAllText(refPath, head + "\n", new UTF8Encoding(false));
        return L.Pick($"已创建分支 {name} @ {head[..7]}", $"Created branch {name} @ {head[..7]}");
    }

    static string Delete(string gitDir, string name)
    {
        if (name == GitCore.ReadHeadBranch(gitDir)) return L.Pick($"⚠ 不能删除当前分支 {name}。", $"⚠ Cannot delete the current branch {name}.");
        var refPath = Path.Combine(gitDir, "refs", "heads", name);
        if (!File.Exists(refPath)) return L.Pick($"⚠ 分支 {name} 不存在。", $"⚠ Branch {name} does not exist.");
        File.Delete(refPath);
        return L.Pick($"已删除分支 {name}", $"Deleted branch {name}");
    }

    // ═══════════════════════════════════════════════════════════
    //  checkout —— switch / create+switch
    // ═══════════════════════════════════════════════════════════

    public static string Checkout(string repoRoot, string[] rest)
    {
        var gitDir = Path.Combine(repoRoot, ".git");
        if (rest.Length == 0) return L.Pick("用法：/git checkout <branch> | /git checkout -b <new> [<base>]",
            "Usage: /git checkout <branch> | /git checkout -b <new> [<base>]");

        if (rest[0] is "-b" or "--branch")
        {
            if (rest.Length < 2) return L.Pick("用法：/git checkout -b <new> [<base>]", "Usage: /git checkout -b <new> [<base>]");
            var newBranch = rest[1];
            var baseSha = rest.Length > 2 ? GitCore.ResolveBranch(gitDir, rest[2]) : GitCore.ReadHeadCommit(gitDir);
            if (baseSha == null) return L.Pick("⚠ 当前无提交，无法创建分支（先 git commit）。",
                "⚠ No commits yet, cannot create a branch (run git commit first).");

            var headsDir = Path.Combine(gitDir, "refs", "heads");
            Directory.CreateDirectory(headsDir);
            var refPath = Path.Combine(headsDir, newBranch);
            if (File.Exists(refPath)) return L.Pick($"⚠ 分支 {newBranch} 已存在。", $"⚠ Branch {newBranch} already exists.");

            File.WriteAllText(refPath, baseSha + "\n", new UTF8Encoding(false));
            SwitchTo(gitDir, repoRoot, newBranch, baseSha);
            return L.Pick($"已创建并切换到分支 {newBranch} @ {baseSha[..7]}",
                $"Created and switched to branch {newBranch} @ {baseSha[..7]}");
        }

        var branch = rest[0];
        var target = GitCore.ResolveBranch(gitDir, branch);
        if (target == null) return L.Pick($"⚠ 分支 {branch} 不存在。", $"⚠ Branch {branch} does not exist.");
        SwitchTo(gitDir, repoRoot, branch, target);
        return L.Pick($"已切换到分支 {branch} @ {target[..7]}", $"Switched to branch {branch} @ {target[..7]}");
    }

    /// <summary>切 HEAD 到 branch、删旧分支独有文件、把新 tree 写入工作区。</summary>
    static void SwitchTo(string gitDir, string repoRoot, string branch, string sha)
    {
        var oldFiles = new Dictionary<string, (string Mode, string Sha)>();
        var oldHead = GitCore.ReadHeadCommit(gitDir);
        if (oldHead != null)
        {
            var info = GitCore.ReadCommitInfo(gitDir, oldHead);
            if (info?.Tree != null) GitCore.FlattenTree(gitDir, info.Value.Tree, "", oldFiles);
        }
        var newFiles = new Dictionary<string, (string Mode, string Sha)>();
        var newInfo = GitCore.ReadCommitInfo(gitDir, sha);
        if (newInfo?.Tree != null) GitCore.FlattenTree(gitDir, newInfo.Value.Tree, "", newFiles);

        foreach (var (rel, _) in oldFiles)
        {
            if (newFiles.ContainsKey(rel)) continue;
            var full = Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(full)) File.Delete(full);
        }

        GitCore.SetHeadBranch(gitDir, branch);
        GitCore.CheckoutWorktree(gitDir, repoRoot, sha);
    }

    // ═══════════════════════════════════════════════════════════
    //  merge —— fast-forward（分叉返回提示）
    // ═══════════════════════════════════════════════════════════

    public static string Merge(string repoRoot, string[] rest)
    {
        var gitDir = Path.Combine(repoRoot, ".git");
        if (rest.Length == 0) return L.Pick("用法：/git merge <branch>", "Usage: /git merge <branch>");

        var srcBranch = rest[0];
        var srcSha = GitCore.ResolveBranch(gitDir, srcBranch);
        if (srcSha == null) return L.Pick($"⚠ 分支 {srcBranch} 不存在。", $"⚠ Branch {srcBranch} does not exist.");

        var curBranch = GitCore.ReadHeadBranch(gitDir);
        var curSha = GitCore.ReadHeadCommit(gitDir);
        if (curSha == null) return L.Pick("⚠ 当前分支无提交，无法合并。", "⚠ The current branch has no commits, cannot merge.");
        if (srcBranch == curBranch) return L.Pick("⚠ 不能合并自己。", "⚠ Cannot merge a branch into itself.");

        if (IsAncestor(gitDir, srcSha, curSha))
            return L.Pick($"已是最新：{srcBranch} 是 {curBranch} 的后代，无需合并。",
                $"Already up to date: {srcBranch} is a descendant of {curBranch}, nothing to merge.");

        if (IsAncestor(gitDir, curSha, srcSha))
        {
            var headsDir = Path.Combine(gitDir, "refs", "heads");
            File.WriteAllText(Path.Combine(headsDir, curBranch), srcSha + "\n", new UTF8Encoding(false));
            var n = GitCore.CheckoutWorktree(gitDir, repoRoot, srcSha);
            return L.Pick($"Fast-forward 合并 {srcBranch} → {curBranch} @ {srcSha[..7]}（写入 {n} 个文件）",
                $"Fast-forward merged {srcBranch} → {curBranch} @ {srcSha[..7]} ({n} files written)");
        }

        return L.Pick($"⚠ {curBranch} 与 {srcBranch} 已分叉，暂不支持自动合并。\n  可 /git diff {curBranch} {srcBranch} 查看差异后手动处理。",
            $"⚠ {curBranch} and {srcBranch} have diverged; automatic merging is not supported yet.\n  Run /git diff {curBranch} {srcBranch} to inspect the differences and resolve them manually.");
    }

    static bool IsAncestor(string gitDir, string ancestorSha, string descendantSha)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var cur = descendantSha;
        while (cur != null && seen.Add(cur))
        {
            if (cur == ancestorSha) return true;
            var info = GitCore.ReadCommitInfo(gitDir, cur);
            if (info == null) return false;
            cur = info.Value.Parent;
        }
        return false;
    }

    static string TruncateByRunes(string s, int max)
    {
        var sb = new StringBuilder();
        int n = 0;
        foreach (var r in s.EnumerateRunes())
        {
            if (n >= max) { sb.Append('…'); return sb.ToString(); }
            sb.Append(r);
            n++;
        }
        return sb.ToString();
    }
}
