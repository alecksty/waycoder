using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;

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
            list.Add(digits.Length > 0 ? int.Parse(digits) : 0);
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

            return new ReleaseInfo
            {
                TagName = tag,
                Body = body,
                AssetUrl = assetUrl,
                AssetName = Path.GetFileName(assetUrl),
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

            return new ReleaseInfo
            {
                TagName = tag,
                Body = body,
                AssetUrl = assetUrl,
                AssetName = Path.GetFileName(assetUrl),
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

            // 1. 下载
            var archive = Path.Combine(tmpDir, latest.AssetName);
            using (var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) })
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("WayCoder");
                using var resp = await client.GetAsync(latest.AssetUrl, HttpCompletionOption.ResponseHeadersRead);
                resp.EnsureSuccessStatusCode();
                using var fs = File.Create(archive);
                await (await resp.Content.ReadAsStreamAsync()).CopyToAsync(fs);
            }

            // 2. 解压出可执行文件
            var exeName = OperatingSystem.IsWindows() ? "waycoder.exe" : "waycoder";
            if (latest.AssetName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                ZipFile.ExtractToDirectory(archive, tmpDir);
            else
                ExtractTarGzSingleFile(archive, tmpDir, exeName);

            var newExe = Directory.GetFiles(tmpDir, exeName, SearchOption.AllDirectories).FirstOrDefault();
            if (newExe == null)
                return "⚠ 压缩包中未找到可执行文件（产物结构异常）";

            // 3. 覆盖当前二进制
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

            var batPath = Path.Combine(dir, "waycoder.upgrade.bat");
            var bat =
                "@echo off\r\n" +
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

            return $"✅ 已下载新版本 {newVersion}。退出 WayCoder 后自动完成替换并重启。";
        }

        // Unix：rename 原子覆盖运行中二进制（旧 inode 继续服务当前进程），随后提示重启
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

        return $"✅ 已升级到 {newVersion}。请退出后重新运行 WayCoder（Ctrl+Q 退出）。";
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
