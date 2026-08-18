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

        var normalized = fullPath.Replace('\\', '/');
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
}
