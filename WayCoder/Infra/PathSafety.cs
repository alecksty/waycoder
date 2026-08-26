namespace WayCoder;

/// <summary>
/// 路径安全防护：拦截对敏感文件/目录的读写，防提示注入通过工具
/// 读 ~/.ssh/id_rsa 泄露密钥、写 ~/.bashrc / authorized_keys 植入后门。
///
/// 对标 Claude Code 的敏感文件保护。所有路径型工具（read_file/write_file/
/// edit_file/rm/mv/cp/download/export）在解析出绝对路径后都应调用 <see cref="CheckSensitive"/>。
/// </summary>
public static class PathSafety
{
    /// <summary>敏感文件名（跨目录精确匹配，如任意位置下的 id_rsa / authorized_keys）。</summary>
    private static readonly HashSet<string> SensitiveFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // SSH 密钥与授权
        "id_rsa", "id_dsa", "id_ecdsa", "id_ed25519", "id_xmss",
        "authorized_keys", "known_hosts",
        // shell 配置（写后门载体）
        ".bashrc", ".zshrc", ".bash_profile", ".profile", ".zprofile",
        // 凭据文件
        ".git-credentials", ".netrc",
    };

    /// <summary>敏感扩展名（私钥/证书）。</summary>
    private static readonly HashSet<string> SensitiveExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pem", ".key", ".p12", ".pfx", ".pgp", ".gpg", ".asc",
    };

    /// <summary>敏感目录段（带斜杠子串匹配，覆盖目录下任意文件）。</summary>
    private static readonly string[] SensitiveDirSegments =
    {
        "/.ssh/", "/.aws/", "/.azure/", "/.kube/", "/.gnupg/",
        "/.config/gcloud/", "/.config/gh/",
    };

    /// <summary>敏感绝对路径前缀（系统凭据）。</summary>
    private static readonly string[] SensitiveAbsolutePaths =
    {
        "/etc/passwd", "/etc/shadow", "/etc/master.passwd", "/etc/sudoers",
        "/var/root/", "/root/.ssh",
    };

    /// <summary>
    /// 检查绝对路径是否命中敏感文件/目录。命中返回拦截原因，未命中返回 null。
    /// </summary>
    /// <param name="fullPath">已由 Path.GetFullPath 归一化的绝对路径。</param>
    public static string? CheckSensitive(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath)) return null;

        // 两个候选路径都做敏感匹配：原始路径（未解析 symlink）+ 解析符号链接后的真实路径。
        // - 原始路径：macOS 上 /etc 是指向 /private/etc 的 symlink，若只解析后再匹配，
        //   "/etc/passwd" 会变成 "/private/etc/passwd" 导致敏感绝对路径前缀失配（拦截失效）。
        // - 解析后路径：防「项目内 symlink → ~/.ssh/id_rsa」绕过文件名/目录段检查
        //   （Path.GetFullPath 只折叠 . / .. ，不解析 symlink，File.ReadAllText/WriteAllText 会跟随链接）。
        var original = fullPath.Replace('\\', '/');
        var resolved = ResolveSymlinks(fullPath);

        foreach (var candidate in new[] { original, resolved })
        {
            var hit = MatchSensitive(candidate);
            if (hit != null) return hit;
        }
        return null;
    }

    /// <summary>对单个归一化路径跑敏感匹配，命中返回拦截原因，未命中返回 null。</summary>
    private static string? MatchSensitive(string path)
    {
        var normalized = path.Replace('\\', '/');
        var withSlash = normalized.EndsWith('/') ? normalized : normalized + "/";

        // 1. 系统凭据绝对路径
        foreach (var sensitive in SensitiveAbsolutePaths)
        {
            if (withSlash.StartsWith(sensitive, StringComparison.OrdinalIgnoreCase))
                return $"敏感系统路径 {sensitive.TrimEnd('/')}";
        }

        // 2. 文件名精确匹配
        var fileName = Path.GetFileName(normalized);
        if (!string.IsNullOrEmpty(fileName) && SensitiveFileNames.Contains(fileName))
            return $"敏感文件 {fileName}";

        // 3. 扩展名（私钥/证书）
        var ext = Path.GetExtension(normalized);
        if (!string.IsNullOrEmpty(ext) && SensitiveExtensions.Contains(ext))
            return $"敏感文件类型 {ext}";

        // 4. 目录段匹配（含 .ssh / .aws 等目录下任意文件）
        foreach (var segment in SensitiveDirSegments)
        {
            if (withSlash.Contains(segment, StringComparison.OrdinalIgnoreCase))
                return $"敏感目录 {segment.Trim('/')}";
        }

        return null;
    }

    /// <summary>
    /// 逐段解析符号链接到最终真实路径。正确处理「父目录是链接」「文件本身是链接」「嵌套链接」，
    /// 且对不存在的路径（如写文件的目标）也能解析其已存在的祖先目录链。
    /// 无法解析（权限/异常）时返回原路径。复杂度 O(路径深度)。
    /// </summary>
    public static string ResolveSymlinks(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath)) return fullPath;
        try
        {
            var root = Path.GetPathRoot(fullPath);
            if (string.IsNullOrEmpty(root)) return fullPath;

            var current = root;
            var remaining = fullPath[root.Length..];
            foreach (var segment in remaining.Split(
                new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries))
            {
                current = Path.Combine(current, segment);
                try
                {
                    FileSystemInfo? target = null;
                    var di = new DirectoryInfo(current);
                    if (di.Exists) target = di.ResolveLinkTarget(returnFinalTarget: true);
                    else
                    {
                        var fi = new FileInfo(current);
                        if (fi.Exists) target = fi.ResolveLinkTarget(returnFinalTarget: true);
                    }
                    if (target != null) current = target.FullName;
                }
                catch
                {
                    // 该段无法解析（权限/IO），保留 current，继续下一段
                }
            }
            return Path.GetFullPath(current);
        }
        catch
        {
            return fullPath;
        }
    }
}
