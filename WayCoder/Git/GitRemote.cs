using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace WayCoder.Git;

/// <summary>
/// git smart HTTP 传输（协议 v1）+ remote/凭证配置 + pull/push/fetch/clone 编排。
/// 移动端无 git 进程，靠纯 C# 实现拉取/推送；桌面端走系统 git（GitCommand 透传），
/// 本类主要服务移动端（GitCore.Run 分发进来），但纯 C# 在桌面端同样可用。
///
/// 协议要点：
///   - advertisement：GET /info/refs?service=git-{upload|receive}-pack，pkt-line 列表（v1）
///   - fetch（v2）：POST /git-upload-pack + Git-Protocol: version=2，
///     body = command=fetch + 0001 定界 + want/have/done + flush，
///     响应 = side-band-64k（channel1=pack / channel2=进度），先解码再找 PACK 魔数。
///     注意：无 v2 头时 Gitee/GitHub 返回空响应（「upload-pack 响应无 packfile」）。
///   - push（v1）：POST /git-receive-pack，body = update 命令 pkt-line + flush + packfile
///   - 认证：HTTP Basic（username:password 或 username:token，二选一）
/// </summary>
public static class GitRemote
{
    // git remote 是用户显式配置的目标（非 AI 诱导），用普通 handler；允许内网自建 git 服务。
    // AutomaticDecompression 必需：Gitee/GitHub 对大响应（upload-pack packfile）gzip 压缩，
    // 不自动解压会拿到原始 gzip 字节，FindPackMarker 找不到 PACK 魔数 → 「upload-pack 响应无 packfile」。
    static readonly Lazy<HttpClient> _client = new(() => new HttpClient(new SocketsHttpHandler
    {
        AllowAutoRedirect = true,
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
    })
    { Timeout = TimeSpan.FromSeconds(60) });

    /// <summary>protocol v2 的 delimiter pkt（0001）：command 段与参数段的分隔。</summary>
    static readonly byte[] DelimPkt = "0001"u8.ToArray();

    // ═══════════════════════════════════════════════════════════
    //  同步入口（供 GitCore.Run 调用，匹配其同步签名）
    // ═══════════════════════════════════════════════════════════

    public static string Remote(string repoRoot, string[] rest)
        => RunSync(() => RemoteAsync(repoRoot, rest));

    public static string Credential(string repoRoot, string[] rest)
        => RunSync(() => CredentialAsync(repoRoot, rest));

    public static string Pull(string repoRoot, string[] rest, Action<string>? progress = null)
        => RunSync(() => FetchCoreAsync(repoRoot, rest, updateLocal: true, progress));

    public static string Fetch(string repoRoot, string[] rest, Action<string>? progress = null)
        => RunSync(() => FetchCoreAsync(repoRoot, rest, updateLocal: false, progress));

    public static string Push(string repoRoot, string[] rest)
        => RunSync(() => PushAsync(repoRoot, rest));

    public static string Clone(string repoRoot, string[] rest, Action<string>? progress = null)
        => RunSync(() => CloneAsync(repoRoot, rest, progress));

    static string RunSync(Func<Task<string>> fn)
    {
        try { return fn().GetAwaiter().GetResult(); }
        catch (Exception ex) { return $"错误：git 远程操作: {ex.GetType().Name}: {ex.Message}"; }
    }

    /// <summary>
    /// 列出远程分支名（ls-refs 的 refs/heads/*）。网络失败/超时返回空数组。
    /// 用独立短超时 client（5s），避免在 UI 线程同步调用时慢网络导致界面卡死。
    /// </summary>
    public static string[] ListRemoteBranches(string url, GitCredential? cred)
    {
        try
        {
            using var http = new HttpClient(new SocketsHttpHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            })
            { Timeout = TimeSpan.FromSeconds(5) };

            var req = new HttpRequestMessage(HttpMethod.Get, $"{url}/info/refs?service=git-upload-pack");
            if (cred != null)
            {
                var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{cred.Value.User}:{cred.Value.Secret}"));
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
            }
            using var resp = http.Send(req);
            resp.EnsureSuccessStatusCode();
            var body = resp.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

            var result = new List<string>();
            using var ms = new MemoryStream(body);
            while (ms.Position < ms.Length)
            {
                var line = PktLine.ReadString(ms);
                if (line == null || line.StartsWith("# service=", StringComparison.Ordinal)) continue;
                var nul = line.IndexOf('\0');
                if (nul >= 0) line = line[..nul];
                var sp = line.IndexOf(' ');
                if (sp < 0) continue;
                var refName = line[(sp + 1)..].Trim();
                if (refName.StartsWith("refs/heads/", StringComparison.Ordinal))
                    result.Add(refName["refs/heads/".Length..]);
            }
            return result.Distinct().ToArray();
        }
        catch { return Array.Empty<string>(); }
    }

    // ═══════════════════════════════════════════════════════════
    //  高层编排
    // ═══════════════════════════════════════════════════════════

    static Task<string> RemoteAsync(string repoRoot, string[] rest)
    {
        var gitDir = Path.Combine(repoRoot, ".git");
        if (rest.Length == 0 || rest[0] == "-v" || rest[0] == "--verbose")
        {
            var url = GitCore.ReadRemoteUrl(gitDir);
            return Task.FromResult(url == null
                ? "尚未配置远程（/git remote add origin <url>）"
                : $"origin\t{url} (fetch)");
        }

        var op = rest[0].ToLowerInvariant();
        return op switch
        {
            "add" => rest.Length < 3
                ? Task.FromResult("用法：/git remote add <name> <url>")
                : Task.FromResult(AddRemote(gitDir, rest[1], rest[2])),
            "set-url" => rest.Length < 3
                ? Task.FromResult("用法：/git remote set-url <name> <url>")
                : Task.FromResult(AddRemote(gitDir, rest[1], rest[2])),
            _ => Task.FromResult("用法：/git remote add <name> <url> | /git remote set-url <name> <url> | /git remote"),
        };
    }

    static string AddRemote(string gitDir, string name, string url)
    {
        GitCore.WriteRemoteUrl(gitDir, name, url);
        return $"已添加远程 {name} → {url}（凭证用 /git credential <username> <password|token> 单独设置）";
    }

    /// <summary>
    /// 凭证命令（密码 / token 二选一）：
    ///   /git credential                             查看（脱敏）
    ///   /git credential <username> <password>       账号密码方式
    ///   /git credential --token <username> <token>  Token 方式（推荐）
    /// </summary>
    static Task<string> CredentialAsync(string repoRoot, string[] rest)
    {
        var gitDir = Path.Combine(repoRoot, ".git");
        if (rest.Length == 0)
        {
            var cred = GitCore.ReadCredential(gitDir);
            return Task.FromResult(cred == null
                ? "未配置凭证"
                : $"凭证已配置：{cred.Value.User}（{ModeName(cred.Value.IsToken)}，密钥不显示）");
        }

        bool isToken = rest[0] is "--token" or "-t";
        var args = isToken ? rest.Skip(1).ToArray() : rest;
        if (args.Length < 2)
            return Task.FromResult("用法：/git credential <username> <password> | /git credential --token <username> <token>");

        GitCore.WriteCredential(gitDir, args[0], args[1], isToken);
        return Task.FromResult($"已保存凭证：{args[0]}（{ModeName(isToken)}，密钥不显示）");
    }

    static string ModeName(bool isToken) => isToken ? "Token" : "密码";

    static async Task<string> FetchCoreAsync(string repoRoot, string[] rest, bool updateLocal, Action<string>? progress = null)
    {
        var (origin, branch) = ParseRefSpec(repoRoot, rest);
        var gitDir = Path.Combine(repoRoot, ".git");
        var url = GitCore.ReadRemoteUrl(gitDir, origin);
        if (url == null) return $"⚠ 未配置远程 {origin}。请先 /git remote add {origin} <url>";
        var cred = GitCore.ReadCredential(gitDir);

        var (newSha, objects) = await FetchObjectsAsync(gitDir, url, cred, branch, progress);
        if (newSha == null) return $"远端 {origin}/{branch} 不存在或已是最新。";

        // 更新 remote-tracking ref
        var remoteRefDir = Path.Combine(gitDir, "refs", "remotes", origin);
        Directory.CreateDirectory(remoteRefDir);
        File.WriteAllText(Path.Combine(remoteRefDir, branch), newSha + "\n", new UTF8Encoding(false));

        if (updateLocal)
        {
            var localBranch = GitCore.ReadHeadBranch(gitDir);
            var headsDir = Path.Combine(gitDir, "refs", "heads");
            var localRef = Path.Combine(headsDir, branch);
            // 当前分支 = 目标分支，或目标分支尚未在本地（远程新分支）→ 建本地 ref + 切 HEAD + 检出
            if (localBranch == branch || !File.Exists(localRef))
            {
                Directory.CreateDirectory(headsDir);
                File.WriteAllText(localRef, newSha + "\n", new UTF8Encoding(false));
                if (localBranch != branch) GitCore.SetHeadBranch(gitDir, branch);
                progress?.Invoke("检出工作区…");
                var written = GitCore.CheckoutWorktree(gitDir, repoRoot, newSha,
                    (done, total) => progress?.Invoke($"检出文件 {done}/{total}"));
                return $"已拉取 {branch} @ {newSha[..7]}（{objects} 个对象，写入 {written} 个文件）";
            }
            return $"已拉取 {branch} @ {newSha[..7]}（{objects} 个对象，未合并到当前分支 {localBranch}）";
        }
        return $"已抓取 {branch} @ {newSha[..7]}（{objects} 个对象）";
    }

    static async Task<string> PushAsync(string repoRoot, string[] rest)
    {
        var (origin, branch) = ParseRefSpec(repoRoot, rest);
        var gitDir = Path.Combine(repoRoot, ".git");
        var url = GitCore.ReadRemoteUrl(gitDir, origin);
        if (url == null) return $"⚠ 未配置远程 {origin}。请先 /git remote add {origin} <url>";
        var cred = GitCore.ReadCredential(gitDir);

        var newSha = GitCore.ReadHeadCommit(gitDir);
        if (newSha == null) return "⚠ 本地尚无提交，无法推送。";

        // 远端 refs（拿 old sha + 作打包边界）
        var remoteRefs = await LsRefsAsync(url, "git-receive-pack", cred);
        var refName = $"refs/heads/{branch}";
        var old = remoteRefs.FirstOrDefault(r => r.Ref == refName).Sha;
        if (old == null) old = new string('0', 40); // 新分支

        // 打包：本地可达对象，远端 ref tips 作边界剪枝
        var stop = new HashSet<string>(remoteRefs.Select(r => r.Sha), StringComparer.Ordinal);
        var objects = GitCore.WalkReachableObjects(gitDir, newSha, stop);
        var pack = PackFileWriter.Write(objects);

        var result = await ReceivePackAsync(url, cred, old, newSha, refName, pack);
        return $"推送 {branch} @ {newSha[..7]}（{objects.Count} 个对象）：{result}";
    }

    static async Task<string> CloneAsync(string repoRoot, string[] rest, Action<string>? progress = null)
    {
        if (rest.Length == 0) return "用法：/git clone <url> [branch]";
        var url = rest[0];
        var branch = rest.Length > 1 ? rest[1] : "master";

        progress?.Invoke("初始化仓库…");
        GitCore.Init(repoRoot);
        var gitDir = Path.Combine(repoRoot, ".git");
        GitCore.WriteRemoteUrl(gitDir, "origin", url);
        var cred = GitCore.ReadCredential(gitDir);

        var (newSha, objects) = await FetchObjectsAsync(gitDir, url, cred, branch, progress);
        if (newSha == null) return $"克隆失败：远端无 {branch} 分支（或需要凭证，请先 /git credential <user> <pass|token>）";

        var headsDir = Path.Combine(gitDir, "refs", "heads");
        Directory.CreateDirectory(headsDir);
        File.WriteAllText(Path.Combine(headsDir, branch), newSha + "\n", new UTF8Encoding(false));
        progress?.Invoke("检出工作区…");
        var written = GitCore.CheckoutWorktree(gitDir, repoRoot, newSha,
            (done, total) => progress?.Invoke($"检出文件 {done}/{total}"));
        return $"已克隆 {url} → {branch} @ {newSha[..7]}（{objects} 个对象，{written} 个文件）";
    }

    // ═══════════════════════════════════════════════════════════
    //  底层协议
    // ═══════════════════════════════════════════════════════════

    /// <summary>拉取：ls-refs → upload-pack → 解码 packfile → 写 loose objects。返回 (新 sha, 对象数)。</summary>
    static async Task<(string? NewSha, int ObjectCount)> FetchObjectsAsync(
        string gitDir, string url, GitCredential? cred, string branch, Action<string>? progress = null)
    {
        progress?.Invoke("连接远端…");
        var refs = await LsRefsAsync(url, "git-upload-pack", cred);
        var want = refs.FirstOrDefault(r => r.Ref == $"refs/heads/{branch}");
        if (want.Sha == null) return (null, 0);

        var localSha = GitCore.ReadHeadCommit(gitDir);
        if (localSha == want.Sha) return (want.Sha, 0); // 已最新

        var haves = localSha != null ? new[] { localSha } : Array.Empty<string>();
        var pack = await UploadPackAsync(url, cred, new[] { want.Sha }, haves, progress);

        progress?.Invoke("pack 下载完成，解码对象…");
        Func<string, (string, byte[])?> externalBase = sha =>
        {
            var obj = GitCore.ReadObject(gitDir, sha);
            return obj == null ? null : (obj.Value.Type, obj.Value.Content);
        };
        var objects = PackFileReader.Read(pack, externalBase,
            (done, total) => progress?.Invoke($"解码对象 {done}/{total}"));
        int written = 0;
        var count = objects.Count;
        foreach (var (sha, (type, content)) in objects)
        {
            GitCore.WriteObject(gitDir, type, content);
            written++;
            if (written % 100 == 0 || written == count)
                progress?.Invoke($"写入对象 {written}/{count}");
        }
        return (want.Sha, written);
    }

    /// <summary>GET /info/refs?service=... 解析远端 refs。</summary>
    static async Task<List<(string Sha, string Ref)>> LsRefsAsync(string url, string service, GitCredential? cred)
    {
        var body = await GetAsync($"{url}/info/refs?service={service}", cred);
        var result = new List<(string, string)>();
        using var ms = new MemoryStream(body);
        while (ms.Position < ms.Length)
        {
            var line = PktLine.ReadString(ms);
            if (line == null || line.StartsWith("# service=", StringComparison.Ordinal)) continue;
            var nul = line.IndexOf('\0');
            if (nul >= 0) line = line[..nul];
            var sp = line.IndexOf(' ');
            if (sp < 0) continue;
            var sha = line[..sp];
            var refName = line[(sp + 1)..].Trim();
            if (sha.Length == 40) result.Add((sha, refName));
        }
        return result;
    }

    /// <summary>
    /// POST /git-upload-pack（protocol v2 fetch），返回裸 packfile。
    /// Gitee/GitHub 现仅响应 v2 请求：无 Git-Protocol: version=2 头会返回空响应 →
    /// 「upload-pack 响应无 packfile」。请求体 = command=fetch + 0001 定界 +
    /// want/have/done + 0000 flush；响应为 side-band-64k 多路复用
    /// （channel 1 = pack 数据、channel 2 = 进度），先解码再找 PACK 魔数。
    /// </summary>
    static async Task<byte[]> UploadPackAsync(string url, GitCredential? cred, string[] wants, string[] haves,
        Action<string>? progress = null)
    {
        using var body = new MemoryStream();
        PktLine.WriteString(body, "command=fetch\n");
        body.Write(DelimPkt, 0, 4); // 0001：command 段结束，进入 fetch 参数段
        foreach (var w in wants) PktLine.WriteString(body, $"want {w}\n");
        foreach (var h in haves) PktLine.WriteString(body, $"have {h}\n");
        PktLine.WriteString(body, "done\n");
        PktLine.Write(body, null); // 0000 flush 结束请求

        var resp = await PostAsync($"{url}/git-upload-pack", "application/x-git-upload-pack-request",
            body.ToArray(), cred, protocolV2: true,
            onDownload: (read, total) => progress?.Invoke(
                total > 0
                    ? $"下载 pack {read / (1024 * 1024)}/{total / (1024 * 1024)} MB"
                    : $"下载 pack {read / (1024 * 1024)} MB"));
        var pack = DeSideband(resp);
        int idx = FindPackMarker(pack);
        if (idx >= pack.Length) throw new InvalidDataException("upload-pack 响应无 packfile");
        return pack[idx..];
    }

    /// <summary>side-band-64k 解码：拼接 channel-1（pack 数据），跳过 channel-2（进度）/3（错误）。</summary>
    internal static byte[] DeSideband(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var pack = new MemoryStream();
        // 首行是无 channel 的 "packfile\n" 裸头（channel = 'p' 非 1，自然被跳过），
        // 其余每帧首字节为 channel：1=pack、2=进度、3=错误。
        while (true)
        {
            var pkt = PktLine.ReadTolerant(ms);
            if (pkt == null) break; // flush / 结束标记 / EOF
            if (pkt.Length <= 1) continue;
            if (pkt[0] == 1) pack.Write(pkt, 1, pkt.Length - 1);
        }
        return pack.ToArray();
    }

    /// <summary>POST /git-receive-pack，返回服务端 pkt-line 状态文本。</summary>
    static async Task<string> ReceivePackAsync(
        string url, GitCredential? cred, string oldSha, string newSha, string refName, byte[] pack)
    {
        using var body = new MemoryStream();
        PktLine.WriteString(body, $"{oldSha} {newSha} {refName}\0report-status");
        PktLine.Write(body, null); // flush
        body.Write(pack, 0, pack.Length);

        var resp = await PostAsync($"{url}/git-receive-pack", "application/x-git-receive-pack-request", body.ToArray(), cred);
        var lines = new List<string>();
        using var ms = new MemoryStream(resp);
        while (ms.Position < ms.Length)
        {
            var line = PktLine.ReadString(ms);
            if (line != null) lines.Add(line);
        }
        return lines.Count == 0 ? "（服务端无状态返回）" : string.Join("；", lines);
    }

    // ═══════════════════════════════════════════════════════════
    //  HTTP 辅助
    // ═══════════════════════════════════════════════════════════

    static async Task<byte[]> GetAsync(string url, GitCredential? cred)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyAuth(req, cred);
        using var resp = await _client.Value.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsByteArrayAsync();
    }

    static async Task<byte[]> PostAsync(string url, string contentType, byte[] body, GitCredential? cred, bool protocolV2 = false,
        Action<long, long>? onDownload = null)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        ApplyAuth(req, cred);
        if (protocolV2) req.Headers.TryAddWithoutValidation("Git-Protocol", "version=2");
        req.Content = new ByteArrayContent(body);
        req.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        // 大 pack 需流式读取以报下载进度（大仓库克隆/拉取时用户可见，避免误以为卡死）
        using var resp = await _client.Value.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();
        if (onDownload == null)
            return await resp.Content.ReadAsByteArrayAsync();

        var total = resp.Content.Headers.ContentLength ?? -1;
        using var ms = new MemoryStream();
        using var stream = await resp.Content.ReadAsStreamAsync();
        var buf = new byte[64 * 1024];
        long read = 0;
        while (true)
        {
            var n = await stream.ReadAsync(buf, 0, buf.Length);
            if (n <= 0) break;
            ms.Write(buf, 0, n);
            read += n;
            onDownload(read, total);
        }
        return ms.ToArray();
    }

    // 密码与 token 统一走 HTTP Basic：base64(user:password) 或 base64(user:token)，
    // Gitee/GitHub 的 git-over-HTTPS 均接受这两种形式。
    static void ApplyAuth(HttpRequestMessage req, GitCredential? cred)
    {
        if (cred == null) return;
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{cred.Value.User}:{cred.Value.Secret}"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
    }

    internal static int FindPackMarker(byte[] data)
    {
        for (int i = 0; i + 4 <= data.Length; i++)
            if (data[i] == 'P' && data[i + 1] == 'A' && data[i + 2] == 'C' && data[i + 3] == 'K')
                return i;
        return data.Length;
    }

    static (string Origin, string Branch) ParseRefSpec(string repoRoot, string[] rest)
    {
        var origin = "origin";
        var branch = GitCore.ReadHeadBranch(Path.Combine(repoRoot, ".git"));
        if (rest.Length >= 1) origin = rest[0];
        if (rest.Length >= 2) branch = rest[1];
        return (origin, branch);
    }
}
