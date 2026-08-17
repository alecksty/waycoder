using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace WayCoder;

/// <summary>
/// 发布信息 —— 从 GitHub/Gitee release 拉取的最新版本元数据。
/// </summary>
public sealed class ReleaseInfo
{
    /// <summary>版本标签（如 v0.49.0）</summary>
    public string TagName { get; set; } = "";
    /// <summary>更新日志正文（Markdown）</summary>
    public string Body { get; set; } = "";
    /// <summary>匹配当前平台的资产下载 URL</summary>
    public string AssetUrl { get; set; } = "";
    /// <summary>资产文件名</summary>
    public string AssetName { get; set; } = "";
    /// <summary>SHA256 校验文件下载 URL（release 里若附带 SHA256SUMS，否则为空）</summary>
    public string ChecksumUrl { get; set; } = "";
    /// <summary>来源（GitHub / Gitee）</summary>
    public string Source { get; set; } = "";
}

/// <summary>
/// 自动升级 —— 版本检查 + 自替换。
///
/// 对标 Claude Code 的 `claude update` / `npm update -g`，让 WayCoder 能一条命令自升级：
///   - 版本检查：优先 Gitee Releases（国内快），失败回退 GitHub Releases（仓库均可用环境变量覆盖）
///   - 自替换：下载匹配当前平台（RID）的压缩包 → 解压出单文件二进制 → 覆盖当前可执行文件
///   - Windows：exe 占用锁无法直接覆盖，落 `.new` + `upgrade.bat` 重试脚本，退出后自动替换并重启
///   - Unix：原子 rename 覆盖运行中二进制（旧 inode 继续服务当前进程），提示重启
///
/// 纯逻辑（版本比较 / RID 探测 / 资产匹配）与网络/文件操作分离，便于确定性自测。
/// </summary>
public static class UpdateChecker
{
    // ════════════════════════════════════════════════════════════════
    // 配置 —— release 仓库（环境变量可覆盖，默认 Gitee 同名仓库镜像到 GitHub）
    // ════════════════════════════════════════════════════════════════

    /// <summary>GitHub 仓库（owner/repo），环境变量 WAYCODER_GITHUB_REPO 可覆盖</summary>
    public static string GitHubRepo =>
        Environment.GetEnvironmentVariable("WAYCODER_GITHUB_REPO") ?? "alecksty/waycoder";

    /// <summary>Gitee 仓库（owner/repo），环境变量 WAYCODER_GITEE_REPO 可覆盖</summary>
    public static string GiteeRepo =>
        Environment.GetEnvironmentVariable("WAYCODER_GITEE_REPO") ?? "aleckstygit/my-coder";

    // ════════════════════════════════════════════════════════════════
    // 纯逻辑（可自测）
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 探测当前平台的 .NET RID（win-x64 / win-arm64 / linux-x64 / linux-arm64 / osx-x64 / osx-arm64）。
    /// 用于匹配 release 资产文件名（package.sh 产物命名 waycoder-&lt;版本&gt;-&lt;RID&gt;.tar.gz/.zip）。
    /// </summary>
    public static string DetectCurrentRid()
    {
        var os = OperatingSystem.IsWindows() ? "win"
            : OperatingSystem.IsMacOS() ? "osx"
            : "linux";
        var arch = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "x64";
        return $"{os}-{arch}";
    }

    /// <summary>
    /// 语义版本比较：a 小于 b 返回负数，相等返回 0，a 大于 b 返回正数。
    /// 支持可选 "v"/"V" 前缀与不同段数（如 "1.0" 与 "1.0.0" 相等），非数字后缀忽略。
    /// </summary>
    public static int CompareVersions(string a, string b)
    {
        var pa = ParseVersion(a);
        var pb = ParseVersion(b);
        var len = Math.Max(pa.Count, pb.Count);
        for (var i = 0; i < len; i++)
        {
            var va = i < pa.Count ? pa[i] : 0;
            var vb = i < pb.Count ? pb[i] : 0;
            if (va != vb) return va.CompareTo(vb);
        }
        return 0;
    }

    /// <summary>解析版本号 "v0.48.6" → [0, 48, 6]（忽略非数字后缀）</summary>
    private static List<int> ParseVersion(string v)
    {
        var list = new List<int>();
        if (string.IsNullOrWhiteSpace(v)) return list;
        v = v.Trim().TrimStart('v', 'V');
        foreach (var part in v.Split('.'))
        {
            var digits = new string(part.TakeWhile(char.IsDigit).ToArray());
            // 超大数字段（如日期型 tag "2024010112345"）int.Parse 会抛 OverflowException，
            // 用 long 解析并饱和到 int.MaxValue，避免恶意/畸形 tag 让更新检查崩溃。
            list.Add(digits.Length > 0 && long.TryParse(digits, out var n) ? (int)Math.Min(n, int.MaxValue) : 0);
        }
        return list;
    }

    /// <summary>
    /// 从资产名列表中挑出匹配指定 RID 的资产名（.tar.gz / .zip）。
    /// 返回 null 表示无匹配。
    /// </summary>
    public static string? FindAssetName(IEnumerable<string> names, string rid)
    {
        var tarSuffix = $"-{rid}.tar.gz";
        var zipSuffix = $"-{rid}.zip";
        foreach (var n in names)
        {
            if (n.EndsWith(tarSuffix, StringComparison.OrdinalIgnoreCase) ||
                n.EndsWith(zipSuffix, StringComparison.OrdinalIgnoreCase))
                return n;
        }
        // 兜底：名称含 RID 且是归档
        foreach (var n in names)
        {
            if (n.Contains(rid, StringComparison.OrdinalIgnoreCase) &&
                (n.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) ||
                 n.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)))
                return n;
        }
        return null;
    }

    /// <summary>从 release 的 assets JSON 数组里挑出匹配 RID 的下载 URL。</summary>
    public static string? FindAssetUrl(JNode? assets, string rid)
    {
        if (assets == null) return null;
        foreach (var a in assets.Items)
        {
            var name = a["name"]?.AsString();
            var url = a["browser_download_url"]?.AsString();
            if (name != null && url != null && name.Contains(rid, StringComparison.OrdinalIgnoreCase) &&
                (name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) ||
                 name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)))
                return url;
        }
        return null;
    }

    /// <summary>release 中可能附带的校验文件名（发布流程产物 SHA256SUMS.txt，兼容常见别名）。</summary>
    private static readonly string[] ChecksumNames = ["SHA256SUMS.txt", "SHA256SUMS", "checksums.txt", "sha256sums.txt"];

    /// <summary>从 assets JSON 数组里挑出 SHA256 校验文件的下载 URL，无则返回 null。</summary>
    public static string? FindChecksumUrl(JNode? assets)
    {
        if (assets == null) return null;
        foreach (var a in assets.Items)
        {
            var name = a["name"]?.AsString();
            var url = a["browser_download_url"]?.AsString();
            if (name == null || url == null) continue;
            foreach (var n in ChecksumNames)
                if (name.Equals(n, StringComparison.OrdinalIgnoreCase))
                    return url;
        }
        return null;
    }

    /// <summary>
    /// 校验下载 URL 是否受信：必须 HTTPS 且 host 属于官方发布源。
    /// 防止 release JSON 被篡改后注入指向攻击者服务器的恶意下载链接（供应链攻击）。
    /// </summary>
    public static bool IsTrustedDownloadUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) return false;
        var host = uri.Host.ToLowerInvariant();
        return host == "github.com"
            || host == "objects.githubusercontent.com"
            || host == "gitee.com";
    }

    /// <summary>
    /// 解析 SHA256SUMS 内容（GNU sha256sum 格式：`&lt;64位hex&gt;  &lt;文件名&gt;`，二进制模式文件名前缀 `*`），
    /// 返回 文件名→小写哈希 的字典（大小写不敏感键）。
    /// </summary>
    public static Dictionary<string, string> ParseChecksums(string content)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in content.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var idx = line.IndexOfAny([' ', '\t']);
            if (idx < 0) continue;
            var hash = line[..idx].Trim().ToLowerInvariant();
            var name = line[(idx + 1)..].Trim().TrimStart('*');
            if (hash.Length != 64 || name.Length == 0) continue;
            if (!map.ContainsKey(name)) map[name] = hash;
        }
        return map;
    }

    /// <summary>计算文件 SHA256 并返回小写十六进制字符串（AOT 安全的静态 HashData）。</summary>
    public static string ComputeSha256Hex(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexStringLower(hash);
    }

    // ════════════════════════════════════════════════════════════════
    // 网络 —— 拉取最新版本
    // ════════════════════════════════════════════════════════════════

    /// <summary>拉取最新 release（Gitee 优先，失败回退 GitHub）。无更新/失败返回 null。</summary>
    public static async Task<ReleaseInfo?> FetchLatestAsync()
    {
        var gitee = await FetchFromGiteeAsync(GiteeRepo);
        if (gitee != null) return gitee;
        return await FetchFromGithubAsync(GitHubRepo);
    }

    /// <summary>从 GitHub Releases 拉取最新版本，匹配当前平台资产。</summary>
    public static async Task<ReleaseInfo?> FetchFromGithubAsync(string repo)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("WayCoder");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            var url = $"https://api.github.com/repos/{repo}/releases/latest";
            var node = Json.Parse(await client.GetStringAsync(url));
            if (node == null) return null;

            var tag = node["tag_name"]?.AsString() ?? "";
            if (string.IsNullOrEmpty(tag)) return null;
            var body = node["body"]?.AsString() ?? "";
            var assetUrl = FindAssetUrl(node["assets"], DetectCurrentRid());
            if (assetUrl == null) return null; // 无匹配平台资产
            if (!IsTrustedDownloadUrl(assetUrl)) return null; // 资产 URL 非受信来源（防 release 注入恶意链接）

            return new ReleaseInfo
            {
                TagName = tag,
                Body = body,
                AssetUrl = assetUrl,
                AssetName = Path.GetFileName(assetUrl),
                ChecksumUrl = FindChecksumUrl(node["assets"]) ?? "",
                Source = "GitHub",
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>从 Gitee Releases 拉取最新版本，匹配当前平台资产。</summary>
    public static async Task<ReleaseInfo?> FetchFromGiteeAsync(string repo)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("WayCoder");
            var url = $"https://gitee.com/api/v5/repos/{repo}/releases/latest";
            var node = Json.Parse(await client.GetStringAsync(url));
            if (node == null) return null;

            var tag = node["tag_name"]?.AsString() ?? "";
            if (string.IsNullOrEmpty(tag)) return null;
            var body = node["body"]?.AsString() ?? "";
            var assetUrl = FindAssetUrl(node["assets"], DetectCurrentRid());
            if (assetUrl == null) return null;
            if (!IsTrustedDownloadUrl(assetUrl)) return null; // 资产 URL 非受信来源（防 release 注入恶意链接）

            return new ReleaseInfo
            {
                TagName = tag,
                Body = body,
                AssetUrl = assetUrl,
                AssetName = Path.GetFileName(assetUrl),
                ChecksumUrl = FindChecksumUrl(node["assets"]) ?? "",
                Source = "Gitee",
            };
        }
        catch
        {
            return null;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // 检查 + 自替换
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 检查是否有新版本，返回简短状态文本（用于启动检查）。
    /// 无法获取/已最新/有新版本三种情况分别返回不同文案。
    /// </summary>
    public static async Task<string> CheckAsync()
    {
        var latest = await FetchLatestAsync();
        if (latest == null)
            return "⚠ 无法获取最新版本信息（检查网络或 WAYCODER_GITHUB_REPO / WAYCODER_GITEE_REPO 配置）";

        var cmp = CompareVersions(latest.TagName, Global.Version);
        if (cmp <= 0)
            return $"✅ 已是最新版本 {Global.Version}（{latest.Source} 最新 {latest.TagName}）";

        return $"🆕 发现新版本 {latest.TagName}（当前 {Global.Version}，来源 {latest.Source}）。输入 /update 查看详情并升级。";
    }

    /// <summary>
    /// 执行自动升级：拉取最新版 → 下载匹配平台压缩包 → 解压 → 覆盖当前二进制。
    /// 返回用户可读的结果文案。已最新 / 无法获取 / 升级失败 / 成功分别处理。
    /// </summary>
    public static async Task<string> SelfUpdateAsync()
    {
        var latest = await FetchLatestAsync();
        if (latest == null)
            return "⚠ 无法获取最新版本信息（检查网络或仓库配置）";

        if (CompareVersions(latest.TagName, Global.Version) <= 0)
            return $"✅ 已是最新版本 {Global.Version}，无需升级。";

        var rid = DetectCurrentRid();
        var tmpDir = Path.Combine(Path.GetTempPath(), $"waycoder-update-{rid}-{latest.TagName}");
        try
        {
            if (Directory.Exists(tmpDir)) Directory.Delete(tmpDir, true);
            Directory.CreateDirectory(tmpDir);

            // 1. 下载归档（下载 URL 已在 Fetch 阶段通过 IsTrustedDownloadUrl 校验）
            var archive = Path.Combine(tmpDir, latest.AssetName);
            await DownloadToFileAsync(latest.AssetUrl, archive);

            // 2. 供应链校验：release 附带 SHA256SUMS 时，下载并比对归档哈希（不匹配则拒绝替换）
            string? checksumContent = null;
            if (!string.IsNullOrEmpty(latest.ChecksumUrl) && IsTrustedDownloadUrl(latest.ChecksumUrl))
            {
                var checksumFile = Path.Combine(tmpDir, "SHA256SUMS");
                await DownloadToFileAsync(latest.ChecksumUrl, checksumFile);
                checksumContent = File.ReadAllText(checksumFile);
            }
            var verifyError = VerifyArchive(archive, checksumContent, latest.AssetName);
            if (verifyError != null)
                return verifyError;

            // 3. 解压出可执行文件
            var exeName = OperatingSystem.IsWindows() ? "waycoder.exe" : "waycoder";
            if (latest.AssetName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                ZipFile.ExtractToDirectory(archive, tmpDir);
            else
                ExtractTarGzSingleFile(archive, tmpDir, exeName);

            var newExe = Directory.GetFiles(tmpDir, exeName, SearchOption.AllDirectories).FirstOrDefault();
            if (newExe == null)
                return "⚠ 压缩包中未找到可执行文件（产物结构异常）";

            // 4. 覆盖当前二进制（覆盖前备份旧版本，失败可回滚）
            return ApplyReplacement(newExe, latest.TagName);
        }
        catch (Exception ex)
        {
            return $"❌ 升级失败：{ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            try { if (Directory.Exists(tmpDir)) Directory.Delete(tmpDir, true); } catch { }
        }
    }

    /// <summary>下载 URL 到本地文件（流式写入，5 分钟超时）。</summary>
    private static async Task DownloadToFileAsync(string url, string destPath)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("WayCoder");
        using var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();
        using var fs = File.Create(destPath);
        await (await resp.Content.ReadAsStreamAsync()).CopyToAsync(fs);
    }

    /// <summary>
    /// 校验下载归档的 SHA256 是否匹配 checksums 文件。返回 null 表示通过（或无校验文件），非 null 为失败文案。
    /// 无校验文件时向后兼容放行（旧 release 未附带 SHA256SUMS）；有但缺条目/哈希不符时拒绝替换。
    /// </summary>
    private static string? VerifyArchive(string archivePath, string? checksumContent, string assetName)
    {
        if (string.IsNullOrEmpty(checksumContent)) return null; // 无校验文件，跳过（向后兼容）
        var map = ParseChecksums(checksumContent);
        if (!map.TryGetValue(assetName, out var expected))
            return $"⚠ 校验文件中未找到 {assetName} 的哈希记录，已中止升级以策安全";
        var actual = ComputeSha256Hex(archivePath);
        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            return $"⚠ SHA256 校验失败：{assetName} 哈希不匹配（下载内容可能被篡改），已中止升级";
        return null;
    }

    /// <summary>用新二进制覆盖当前可执行文件（平台相关）。</summary>
    private static string ApplyReplacement(string newExe, string newVersion)
    {
        var target = Environment.ProcessPath;
        if (string.IsNullOrEmpty(target))
            return "❌ 无法确定当前可执行文件路径";

        var dir = Path.GetDirectoryName(target)!;
        var exeName = Path.GetFileName(target);

        if (OperatingSystem.IsWindows())
        {
            // Windows：运行中的 exe 被锁，无法直接覆盖 → 落 .new + 重试脚本，退出后自动替换并重启
            var newPath = Path.Combine(dir, exeName + ".new");
            File.Copy(newExe, newPath, overwrite: true);

            var backupPath = target + ".bak";
            var batPath = Path.Combine(dir, "waycoder.upgrade.bat");
            var bat =
                "@echo off\r\n" +
                // 回滚备份：替换前把旧 exe 备份为 waycoder.exe.bak，升级失败可手动恢复
                $"copy /y \"{target}\" \"{backupPath}\" >nul 2>&1\r\n" +
                ":retry\r\n" +
                "timeout /t 1 /nobreak >nul\r\n" +
                $"move /y \"{newPath}\" \"{target}\" >nul 2>&1\r\n" +
                $"if exist \"{newPath}\" goto retry\r\n" +
                $"start \"\" \"{target}\"\r\n" +
                "del \"%~f0\"\r\n";
            File.WriteAllText(batPath, bat);

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
            }
            catch { /* 脚本启动失败时用户可手动运行 */ }

            return $"✅ 已下载新版本 {newVersion}。退出 WayCoder 后自动完成替换并重启（旧版本已备份为 .bak）。";
        }

        // Unix：rename 原子覆盖运行中二进制（旧 inode 继续服务当前进程），随后提示重启
        // 回滚备份：覆盖前把当前二进制备份为 waycoder.bak，升级失败可手动恢复
        try { File.Copy(target, target + ".bak", overwrite: true); } catch { /* 备份失败不阻塞升级 */ }

        var tmpNew = target + ".new";
        File.Copy(newExe, tmpNew, overwrite: true);
        try
        {
            File.SetUnixFileMode(tmpNew,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
        catch { /* 某些文件系统不支持 chmod，忽略 */ }
        File.Move(tmpNew, target, overwrite: true);

        return $"✅ 已升级到 {newVersion}。请退出后重新运行 WayCoder（Ctrl+Q 退出），旧版本已备份为 .bak。";
    }

    // ════════════════════════════════════════════════════════════════
    // 工具 —— 极简 tar.gz 单文件解压（仅 GZipStream + FileStream，AOT 零风险）
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// 从 tar.gz 归档中解压出指定文件名的单文件到 destDir。
    /// 仅面向本仓库 package.sh 的简单产物（单文件 + 短名，无 GNU 长名/PAX 头）。
    /// </summary>
    private static void ExtractTarGzSingleFile(string archivePath, string destDir, string targetFileName)
    {
        using var file = File.OpenRead(archivePath);
        using var gz = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new BinaryReader(gz);

        Span<byte> header = stackalloc byte[512];
        while (true)
        {
            var read = ReadFully(reader, header, 512);
            if (read < 512) break;
            if (IsAllZero(header, 512)) break; // 结束块

            var name = ReadCString(header, 0, 100);
            var size = ParseOctal(ReadCString(header, 124, 12));
            var typeflag = header[156];

            if (typeflag == (byte)'0' || typeflag == 0)
            {
                var entryName = Path.GetFileName(name.TrimEnd('/'));
                if (entryName == targetFileName)
                {
                    using var outFile = File.Create(Path.Combine(destDir, targetFileName));
                    CopyBytes(reader, outFile, size);
                }
                else
                {
                    SkipBytes(reader, size);
                }
            }
            else
            {
                SkipBytes(reader, size); // 目录 / 符号链接 / 其它类型：跳过数据
            }

            // 跳到 512 字节边界
            var padding = (int)((512 - (size % 512)) % 512);
            SkipBytes(reader, padding);
        }
    }

    private static int ReadFully(BinaryReader reader, Span<byte> buffer, int count)
    {
        var total = 0;
        while (total < count)
        {
            var n = reader.BaseStream.Read(buffer[total..count]);
            if (n <= 0) break;
            total += n;
        }
        return total;
    }

    private static bool IsAllZero(Span<byte> buffer, int count)
    {
        for (var i = 0; i < count; i++)
            if (buffer[i] != 0) return false;
        return true;
    }

    private static string ReadCString(Span<byte> buffer, int offset, int maxLen)
    {
        var end = offset;
        while (end < offset + maxLen && buffer[end] != 0) end++;
        return System.Text.Encoding.UTF8.GetString(buffer[offset..end]);
    }

    private static long ParseOctal(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        long value = 0;
        foreach (var c in s)
        {
            if (c == ' ' || c == 0) continue;
            if (c < '0' || c > '7') break;
            value = value * 8 + (c - '0');
        }
        return value;
    }

    private static void CopyBytes(BinaryReader reader, FileStream outFile, long size)
    {
        var buffer = new byte[64 * 1024];
        long remaining = size;
        while (remaining > 0)
        {
            var toRead = (int)Math.Min(buffer.Length, remaining);
            var read = reader.BaseStream.Read(buffer, 0, toRead);
            if (read <= 0) break;
            outFile.Write(buffer, 0, read);
            remaining -= read;
        }
    }

    private static void SkipBytes(BinaryReader reader, long size)
    {
        var buffer = new byte[64 * 1024];
        long remaining = size;
        while (remaining > 0)
        {
            var toRead = (int)Math.Min(buffer.Length, remaining);
            var read = reader.BaseStream.Read(buffer, 0, toRead);
            if (read <= 0) break;
            remaining -= read;
        }
    }
}
