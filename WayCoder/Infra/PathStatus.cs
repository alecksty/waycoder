namespace WayCoder.Infra;

/// <summary>
/// 状态栏路径信息助手：当前工作目录格式化（home 展开为 ~）+ git 分支探测。
/// 纯逻辑（仅文件读取，无 UI/进程依赖），供底部状态栏显示与自测复用。
/// </summary>
public static class PathStatus
{
    /// <summary>
    /// 探测指定目录的 git 分支名。支持普通仓库（.git 目录）与 worktree / 子模块
    /// （.git 是指向 gitdir 的文本文件）。非 git 仓库返回 null。
    /// detached HEAD 返回短哈希（前 8 位）。
    /// </summary>
    public static string? TryGetBranch(string dir)
    {
        try
        {
            var gitPath = Path.Combine(dir, ".git");
            string? gitDir;
            if (Directory.Exists(gitPath))
            {
                gitDir = gitPath;
            }
            else if (File.Exists(gitPath))
            {
                // worktree / 子模块：.git 是文本文件，内容为 "gitdir: <路径>"
                var content = File.ReadAllText(gitPath).Trim();
                const string prefix = "gitdir:";
                if (!content.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
                var p = content[prefix.Length..].Trim();
                gitDir = Path.IsPathRooted(p) ? p : Path.GetFullPath(Path.Combine(dir, p));
            }
            else
            {
                return null;
            }

            var headPath = Path.Combine(gitDir, "HEAD");
            if (!File.Exists(headPath)) return null;
            var head = File.ReadAllText(headPath).Trim();

            if (head.StartsWith("ref: ", StringComparison.Ordinal))
            {
                var refName = head["ref: ".Length..];
                return refName.StartsWith("refs/heads/", StringComparison.Ordinal)
                    ? refName["refs/heads/".Length..]
                    : refName[(refName.LastIndexOf('/') + 1)..];
            }
            return head.Length >= 8 ? head[..8] : head; // detached HEAD（短哈希）
        }
        catch
        {
            return null;
        }
    }

    /// <summary>把绝对路径格式化为状态栏友好的短形式：home 目录展开为 ~（其余原样返回）。</summary>
    public static string FormatCwd(string dir)
    {
        try
        {
            var home = WayCoder.Global.Home;
            if (!string.IsNullOrEmpty(home))
            {
                var normDir = dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var normHome = home.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (normDir.Equals(normHome, StringComparison.OrdinalIgnoreCase))
                    return "~";
                if (normDir.StartsWith(normHome + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return "~" + normDir[normHome.Length..];
            }
            return dir;
        }
        catch
        {
            return dir;
        }
    }
}
