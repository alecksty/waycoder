using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace WayCoder.Git;

/// <summary>
/// 纯 C# git 核心 —— 不依赖 git 进程、不依赖 native 库（libgit2），AOT 安全。
///
/// 面向移动端（iOS 禁止 Process.Start，无法调 git 子进程）与任何无 git 环境。
/// 实现 git 对象模型的最小子集（loose objects 格式，无 packfile），覆盖日常
/// 「初始化 → 暂存 → 提交 → 查看状态/差异/历史」闭环：
///
///   init / add / commit / status / diff / log
///
/// 关键取舍（移动端沙箱独立仓库，不与桌面真 git 共享 index）：
///   - 对象存储：SHA-1 + ZLibStream（.NET 内置，AOT 安全），loose objects 落 .git/objects/xx/yyy
///   - 暂存区：简化文本 index（mode\tsha\tpath 每行一条），非真 git v2 二进制格式
///   - 只写 loose objects，不产 packfile；读现有仓库遇到 packfile 会优雅降级
///   - diff 用「窗口同步点」启发式逐行比较，代码文件足够
///
/// 远程操作（clone/pull/push）涉及 git 传输协议 + packfile 编解码 + 身份认证，
/// 超出本子集范围，留待后续阶段（Android 可走 Runtime.exec 调系统 git 或接开源绑定）。
/// </summary>
public static class GitCore
{
    // ═══════════════════════════════════════════════════════════
    //  仓库定位
    // ═══════════════════════════════════════════════════════════

    /// <summary>从 startDir 向上找含 .git 的仓库根，找不到返回 null。</summary>
    public static string? FindRepoRoot(string? startDir)
    {
        var dir = startDir;
        if (string.IsNullOrEmpty(dir)) dir = Directory.GetCurrentDirectory();
        try
        {
            var full = Path.GetFullPath(dir);
            while (full != null)
            {
                if (Directory.Exists(Path.Combine(full, ".git"))) return full;
                var parent = Path.GetDirectoryName(full);
                if (parent == full) break;
                full = parent;
            }
        }
        catch { /* 路径无效 */ }
        return null;
    }

    // ═══════════════════════════════════════════════════════════
    //  命令入口
    // ═══════════════════════════════════════════════════════════

    /// <summary>解析并执行一条 git 命令（子命令 + 参数），返回人类可读结果。</summary>
    public static string Run(string repoRoot, string commandLine)
    {
        var tokens = commandLine.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return "用法：git <status|log|diff|add|commit|init>";
        var sub = tokens[0].ToLowerInvariant();
        var rest = tokens.Skip(1).ToArray();
        try
        {
            return sub switch
            {
                "init" => Init(repoRoot),
                "add" => Add(repoRoot, rest.Length > 0 ? string.Join(' ', rest) : "."),
                "commit" => Commit(repoRoot, ParseCommitMessage(rest)),
                "status" => Status(repoRoot),
                "diff" => DiffCommand(repoRoot, rest),
                "log" => Log(repoRoot, ParseMaxCount(rest)),
                "branch" => GitBranch.Branch(repoRoot, rest),
                "checkout" => GitBranch.Checkout(repoRoot, rest),
                "merge" => GitBranch.Merge(repoRoot, rest),
                "remote" => GitRemote.Remote(repoRoot, rest),
                "pull" => GitRemote.Pull(repoRoot, rest),
                "push" => GitRemote.Push(repoRoot, rest),
                "fetch" => GitRemote.Fetch(repoRoot, rest),
                "clone" => GitRemote.Clone(repoRoot, rest),
                "credential" => GitRemote.Credential(repoRoot, rest),
                _ => $"⚠ 不支持的 git 子命令：{sub}（支持 init/add/commit/status/diff/log/branch/checkout/merge/pull/push/fetch/remote/clone/credential）",
            };
        }
        catch (Exception ex)
        {
            return $"错误：git {sub}: {ex.GetType().Name}: {ex.Message}";
        }
    }

    static string ParseCommitMessage(string[] rest)
    {
        // 支持 -m "msg" / -m msg / --message=msg（消息带引号时剥离首尾引号）
        for (int i = 0; i < rest.Length; i++)
        {
            if (rest[i] is "-m" or "--message")
            {
                if (i + 1 < rest.Length) return TrimQuotes(string.Join(' ', rest[(i + 1)..]).Trim());
                return "";
            }
            if (rest[i].StartsWith("-m", StringComparison.Ordinal) && rest[i].Length > 2)
                return TrimQuotes(rest[i][2..]);
            if (rest[i].StartsWith("--message=", StringComparison.Ordinal))
                return TrimQuotes(rest[i]["--message=".Length..]);
        }
        return string.Join(' ', rest);
    }

    static string TrimQuotes(string s)
    {
        s = s.Trim();
        if (s.Length >= 2 && ((s[0] == '"' && s[^1] == '"') || (s[0] == '\'' && s[^1] == '\'')))
            return s[1..^1];
        return s;
    }

    static int ParseMaxCount(string[] rest)
    {
        for (int i = 0; i + 1 < rest.Length; i++)
            if ((rest[i] == "-n" || rest[i] == "--max-count") && int.TryParse(rest[i + 1], out var n))
                return n;
        return 20;
    }

    // ═══════════════════════════════════════════════════════════
    //  init
    // ═══════════════════════════════════════════════════════════

    /// <summary>在 repoRoot 创建 .git 目录结构（幂等）。</summary>
    public static string Init(string repoRoot)
    {
        var gitDir = Path.Combine(repoRoot, ".git");
        if (!Directory.Exists(gitDir))
        {
            Directory.CreateDirectory(Path.Combine(gitDir, "objects"));
            Directory.CreateDirectory(Path.Combine(gitDir, "refs", "heads"));
            Directory.CreateDirectory(Path.Combine(gitDir, "refs", "tags"));
            File.WriteAllText(Path.Combine(gitDir, "HEAD"), "ref: refs/heads/master\n", new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(gitDir, "config"), "[core]\n\trepositoryformatversion = 0\n\tfilemode = true\n[user]\n\tname = WayCoder\n\temail = waycoder@local\n", new UTF8Encoding(false));
            return $"已初始化空 git 仓库：{repoRoot}（默认分支 master）";
        }
        return $"git 仓库已存在：{repoRoot}";
    }

    // ═══════════════════════════════════════════════════════════
    //  add —— 把文件写入暂存区（index）
    // ═══════════════════════════════════════════════════════════

    /// <summary>暂存指定路径（文件 / 目录 / . = 全部）。返回暂存统计。</summary>
    public static string Add(string repoRoot, string pathSpec)
    {
        var gitDir = Path.Combine(repoRoot, ".git");
        var index = ReadIndex(gitDir);

        var targets = ResolveAddPaths(repoRoot, pathSpec);
        int added = 0;
        foreach (var file in targets)
        {
            var rel = Path.GetRelativePath(repoRoot, file).Replace('\\', '/');
            var content = File.ReadAllBytes(file);
            var sha = WriteObject(gitDir, "blob", content);
            index[rel] = ("100644", sha);
            added++;
        }

        WriteIndex(gitDir, index);
        return added > 0
            ? $"已暂存 {added} 个文件"
            : "没有可暂存的文件（路径不存在或为空）";
    }

    static List<string> ResolveAddPaths(string repoRoot, string pathSpec)
    {
        var result = new List<string>();
        var root = Path.GetFullPath(repoRoot);

        if (pathSpec == "." || string.IsNullOrEmpty(pathSpec))
        {
            // 全部文件（排除 .git）
            foreach (var f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                if (IsInsideGit(f, root)) continue;
                if (Path.GetFileName(f).StartsWith(".waycoder", StringComparison.Ordinal)) continue;
                result.Add(f);
            }
            return result;
        }

        var full = Path.GetFullPath(Path.Combine(root, pathSpec));
        if (Directory.Exists(full))
        {
            foreach (var f in Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories))
            {
                if (IsInsideGit(f, root)) continue;
                result.Add(f);
            }
        }
        else if (File.Exists(full))
        {
            result.Add(full);
        }
        return result;
    }

    static bool IsInsideGit(string path, string repoRoot)
    {
        var gitDir = Path.Combine(repoRoot, ".git");
        return path.StartsWith(gitDir, StringComparison.Ordinal);
    }

    // ═══════════════════════════════════════════════════════════
    //  commit —— 从 index 构建 tree + commit，更新 ref
    // ═══════════════════════════════════════════════════════════

    /// <summary>把当前暂存区提交为一个 commit。</summary>
    public static string Commit(string repoRoot, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "⚠ 请提供提交信息（commit -m \"...\"）";

        var gitDir = Path.Combine(repoRoot, ".git");
        var index = ReadIndex(gitDir);
        if (index.Count == 0)
            return "⚠ 暂存区为空，先 git add 再 commit";

        // 1) 从 index 构建 tree
        var treeSha = BuildTree(gitDir, index);

        // 2) 读 parent（当前 HEAD）
        var parent = ReadHeadCommit(gitDir);

        // 3) 构建 commit 对象
        var (name, email) = ReadUserConfig(gitDir);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var sb = new StringBuilder();
        sb.Append("tree ").Append(treeSha).Append('\n');
        if (parent != null) sb.Append("parent ").Append(parent).Append('\n');
        sb.Append("author ").Append(name).Append(" <").Append(email).Append("> ").Append(now).Append(" +0000\n");
        sb.Append("committer ").Append(name).Append(" <").Append(email).Append("> ").Append(now).Append(" +0000\n");
        sb.Append('\n').Append(message.TrimEnd('\n')).Append('\n');

        var commitSha = WriteObject(gitDir, "commit", Encoding.UTF8.GetBytes(sb.ToString()));

        // 4) 更新当前分支 ref
        var branch = ReadHeadBranch(gitDir);
        File.WriteAllText(Path.Combine(gitDir, "refs", "heads", branch), commitSha + "\n", new UTF8Encoding(false));

        return $"已提交 {commitSha[..7]}：{message.Split('\n')[0]}\n（{index.Count} 个文件，分支 {branch}）";
    }

    // ═══════════════════════════════════════════════════════════
    //  status —— 对比 worktree / index / HEAD
    // ═══════════════════════════════════════════════════════════

    /// <summary>返回三组状态：未暂存变更 / 已暂存变更 / 未跟踪文件。</summary>
    public static string Status(string repoRoot)
    {
        var gitDir = Path.Combine(repoRoot, ".git");
        var index = ReadIndex(gitDir);                          // path -> (mode, blobSha)
        var headFiles = ReadHeadTreeFiles(gitDir);              // path -> (mode, blobSha)
        var workFiles = ScanWorktree(repoRoot);                 // path -> blobSha（worktree 内容哈希）

        var staged = new List<string>();
        var unstaged = new List<string>();
        var untracked = new List<string>();

        foreach (var (path, wsha) in workFiles.OrderBy(k => k.Key))
        {
            bool inIndex = index.TryGetValue(path, out var istaged);
            bool inHead = headFiles.TryGetValue(path, out var ihead);

            if (!inIndex && !inHead)
            {
                untracked.Add(path);
                continue;
            }
            if (inIndex && istaged.Sha != wsha)
            {
                unstaged.Add($"  modified: {path}");
                continue;
            }
        }

        foreach (var (path, stagedEntry) in index.OrderBy(k => k.Key))
        {
            bool inHead = headFiles.TryGetValue(path, out var headEntry);
            if (!inHead)
                staged.Add($"  new file: {path}");
            else if (headEntry.Sha != stagedEntry.Sha)
                staged.Add($"  modified: {path}");
            else if (!File.Exists(Path.Combine(repoRoot, path.Replace('/', Path.DirectorySeparatorChar))))
                staged.Add($"  deleted:  {path}");
        }

        var sb = new StringBuilder();
        string branch = ReadHeadBranch(gitDir);
        var headSha = ReadHeadCommit(gitDir);
        sb.Append(branch == "master" && headSha == null
            ? "当前分支 master（无提交）\n"
            : $"当前分支 {branch} @ {headSha?[..7] ?? "（无提交）"}\n");

        if (staged.Count > 0)
        {
            sb.Append("\n「已暂存」\n");
            foreach (var s in staged) sb.Append(s).Append('\n');
        }
        if (unstaged.Count > 0)
        {
            sb.Append("\n「未暂存」\n");
            foreach (var s in unstaged) sb.Append(s).Append('\n');
        }
        if (untracked.Count > 0)
        {
            sb.Append("\n「未跟踪」\n");
            foreach (var s in untracked) sb.Append("  ").Append(s).Append('\n');
        }
        if (staged.Count == 0 && unstaged.Count == 0 && untracked.Count == 0)
            sb.Append("\n工作区干净，无变更。\n");

        return sb.ToString().TrimEnd('\n');
    }

    // ═══════════════════════════════════════════════════════════
    //  diff —— 对比 HEAD（或 index）与 worktree 的内容
    // ═══════════════════════════════════════════════════════════

    /// <summary>输出变更文件与 HEAD（暂存区内容优先）之间的 unified diff。</summary>
    public static string Diff(string repoRoot, string? pathFilter)
    {
        var gitDir = Path.Combine(repoRoot, ".git");
        var index = ReadIndex(gitDir);
        var headFiles = ReadHeadTreeFiles(gitDir);
        var workFiles = ScanWorktree(repoRoot);

        var sb = new StringBuilder();
        int shown = 0;

        foreach (var (path, wsha) in workFiles.OrderBy(k => k.Key))
        {
            if (pathFilter != null && !path.Contains(pathFilter, StringComparison.Ordinal)) continue;

            // 旧内容优先取暂存区，其次 HEAD，都没有 = 新文件
            string? oldContent = null;
            string oldSha = "";
            if (index.TryGetValue(path, out var ista)) { oldContent = ReadBlobText(gitDir, ista.Sha); oldSha = ista.Sha; }
            else if (headFiles.TryGetValue(path, out var hd)) { oldContent = ReadBlobText(gitDir, hd.Sha); oldSha = hd.Sha; }

            var newContent = File.ReadAllText(Path.Combine(repoRoot, path.Replace('/', Path.DirectorySeparatorChar)));
            var newSha = HashBlob(Encoding.UTF8.GetBytes(newContent));

            if (oldSha == newSha) continue; // 无变更
            shown++;
            sb.Append(GenerateUnifiedDiff(oldContent ?? "", newContent, path, oldSha, newSha)).Append('\n');
        }

        if (shown == 0) return "没有变更。";
        return sb.ToString().TrimEnd('\n');
    }

    // ═══════════════════════════════════════════════════════════
    //  log —— 沿 parent 链遍历提交历史
    // ═══════════════════════════════════════════════════════════

    /// <summary>列出提交历史（最多 maxCount 条）。</summary>
    public static string Log(string repoRoot, int maxCount = 20)
    {
        var gitDir = Path.Combine(repoRoot, ".git");
        var sha = ReadHeadCommit(gitDir);
        if (sha == null) return "尚无提交历史。";

        var sb = new StringBuilder();
        int count = 0;
        while (sha != null && count < maxCount)
        {
            var (type, content) = ReadObject(gitDir, sha)!.Value;
            if (type != "commit") break;
            var text = Encoding.UTF8.GetString(content);
            var parsed = ParseCommit(text);
            sb.Append("commit ").Append(sha).Append('\n');
            if (parsed.Author != null) sb.Append("Author: ").Append(parsed.Author).Append('\n');
            if (parsed.Date != null) sb.Append("Date:   ").Append(parsed.Date).Append('\n');
            sb.Append('\n').Append(parsed.Message).Append('\n');
            sb.Append('\n');
            count++;
            sha = parsed.Parent;
        }
        return sb.ToString().TrimEnd('\n');
    }

    // ═══════════════════════════════════════════════════════════
    //  对象存储（loose objects）
    // ═══════════════════════════════════════════════════════════

    internal static string WriteObject(string gitDir, string type, byte[] content)
    {
        // 流式 sha + 流式压缩：不拼 header+content 大数组（几百 MB 对象会二次占内存）
        var header = Encoding.UTF8.GetBytes($"{type} {content.Length}\0");
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        hash.AppendData(header);
        hash.AppendData(content);
        var sha = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
        var dir = Path.Combine(gitDir, "objects", sha[..2]);
        var file = Path.Combine(dir, sha[2..]);
        if (File.Exists(file)) return sha; // 已存在，跳过

        Directory.CreateDirectory(dir);
        using var fs = File.Create(file);
        using (var z = new ZLibStream(fs, CompressionLevel.Fastest, leaveOpen: true))
        {
            z.Write(header, 0, header.Length);
            z.Write(content, 0, content.Length);
        }
        return sha;
    }

    // ── pack 对象缓存（thin pack 的 base 可能落在本地 pack 里，非 loose）──
    static readonly object _packStoreLock = new();
    static readonly Dictionary<string, PackStore?> _packStores = new(StringComparer.Ordinal);

    /// <summary>读对象：先 loose（.git/objects/xx/yyy），再回退本地 pack（.git/objects/pack/*.pack）。</summary>
    internal static (string Type, byte[] Content)? ReadObject(string gitDir, string sha)
    {
        var loose = ReadLooseObject(gitDir, sha);
        if (loose != null) return loose;
        return GetPackStore(gitDir)?.Read(sha);
    }

    static (string Type, byte[] Content)? ReadLooseObject(string gitDir, string sha)
    {
        var file = Path.Combine(gitDir, "objects", sha[..2], sha[2..]);
        if (!File.Exists(file)) return null;
        using var fs = File.OpenRead(file);
        using var z = new ZLibStream(fs, CompressionMode.Decompress);

        // 先解出 header（type size\0），按 contentLen 精确分配，避免 MemoryStream 翻倍物化大对象
        var prefix = new byte[128];
        int n = 0;
        while (n < prefix.Length)
        {
            int r = z.Read(prefix, n, prefix.Length - n);
            if (r <= 0) break;
            n += r;
        }
        int nul = Array.IndexOf(prefix, (byte)0, 0, n);
        if (nul < 0) return null;
        var header = Encoding.UTF8.GetString(prefix, 0, nul);
        var sp = header.IndexOf(' ');
        if (sp < 0 || !long.TryParse(header[(sp + 1)..], out var contentLen) || contentLen > int.MaxValue) return null;

        var content = new byte[contentLen];
        long copied = n - (nul + 1);
        if (copied > 0) Array.Copy(prefix, nul + 1, content, 0, (int)Math.Min(copied, contentLen));
        while (copied < contentLen)
        {
            int r = z.Read(content, (int)copied, (int)(contentLen - copied));
            if (r <= 0) break;
            copied += r;
        }
        if (copied != contentLen) return null;
        return (header[..sp], content);
    }

    // ── 大对象原生路径（安卓托管堆 256MB 上限，单对象几百 MB 不能物化 byte[]）──

    /// <summary>原生内容计算 git 对象 sha（流式 IncrementalHash，不拼 header+content 大数组）。</summary>
    internal static unsafe string ObjectShaNative(string type, byte* content, long length)
    {
        var header = Encoding.UTF8.GetBytes($"{type} {length}\0");
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        hash.AppendData(header);
        hash.AppendData(new ReadOnlySpan<byte>(content, (int)length));
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    /// <summary>从原生内容流式写 loose 对象（header + 压缩，不拼大数组）；sha 已由调用方算好。</summary>
    internal static unsafe void WriteLooseObjectNative(string gitDir, string type, string sha, byte* content, long length)
    {
        var header = Encoding.UTF8.GetBytes($"{type} {length}\0");
        var dir = Path.Combine(gitDir, "objects", sha[..2]);
        var file = Path.Combine(dir, sha[2..]);
        if (File.Exists(file)) return; // 已存在，跳过
        Directory.CreateDirectory(dir);
        using var fs = File.Create(file);
        using (var z = new ZLibStream(fs, CompressionLevel.Fastest, leaveOpen: true))
        {
            z.Write(header, 0, header.Length);
            // 分块写：安卓 Mono 的 ZLibStream 对原生 Span 可能物化托管副本（大对象整块写会 OOM）
            const int Chunk = 1 << 20;   // 1MB
            long off = 0;
            while (off < length)
            {
                int n = (int)Math.Min(Chunk, length - off);
                z.Write(new ReadOnlySpan<byte>(content + off, n));
                off += n;
            }
        }
    }

    /// <summary>
    /// 读 loose 对象到原生内存（大 base 用，不物化托管 byte[]）。返回 null = 不存在。
    /// 调用方负责 <see cref="NativeMemory.Free"/>。
    /// </summary>
    internal static unsafe (string Type, IntPtr Ptr, long Len)? ReadLooseObjectNative(string gitDir, string sha)
    {
        var file = Path.Combine(gitDir, "objects", sha[..2], sha[2..]);
        if (!File.Exists(file)) return null;
        using var fs = File.OpenRead(file);
        using var z = new ZLibStream(fs, CompressionMode.Decompress);

        // 读头部（type size\0），只读前 128 字节足够
        var prefix = new byte[128];
        int n = 0;
        while (n < prefix.Length)
        {
            int r = z.Read(prefix, n, prefix.Length - n);
            if (r <= 0) break;
            n += r;
        }
        int nul = Array.IndexOf(prefix, (byte)0, 0, n);
        if (nul < 0) return null;
        var header = Encoding.UTF8.GetString(prefix, 0, nul);
        var sp = header.IndexOf(' ');
        if (sp < 0 || !long.TryParse(header[(sp + 1)..], out var contentLen)) return null;

        var ptr = (byte*)NativeMemory.Alloc((nuint)contentLen);
        try
        {
            long copied = n - (nul + 1);
            if (copied > 0) Marshal.Copy(prefix, nul + 1, (IntPtr)ptr, (int)Math.Min(copied, contentLen));
            while (copied < contentLen)
            {
                int r = z.Read(new Span<byte>(ptr + copied, (int)(contentLen - copied)));
                if (r <= 0) break;
                copied += r;
            }
            if (copied != contentLen) { NativeMemory.Free(ptr); return null; }
            return (header[..sp], (IntPtr)ptr, copied);
        }
        catch { NativeMemory.Free(ptr); throw; }
    }

    /// <summary>惰性加载本地 pack 索引（sha→offset）。key = gitDir|pack 目录最新 mtime，变更后重建。</summary>
    static PackStore? GetPackStore(string gitDir)
    {
        var packDir = Path.Combine(gitDir, "objects", "pack");
        if (!Directory.Exists(packDir)) return null;
        long stamp = 0;
        foreach (var f in Directory.EnumerateFiles(packDir))
            stamp = Math.Max(stamp, File.GetLastWriteTimeUtc(f).Ticks);
        var key = gitDir + "|" + stamp;
        lock (_packStoreLock)
        {
            if (_packStores.TryGetValue(key, out var s)) return s;
            var store = PackStore.Load(gitDir);
            _packStores[key] = store;
            return store;
        }
    }

    internal static string HashBlob(byte[] content)
    {
        var header = Encoding.UTF8.GetBytes($"blob {content.Length}\0");
        var full = new byte[header.Length + content.Length];
        Array.Copy(header, 0, full, 0, header.Length);
        Array.Copy(content, 0, full, header.Length, content.Length);
        return Convert.ToHexString(SHA1.HashData(full)).ToLowerInvariant();
    }

    internal static string? ReadBlobText(string gitDir, string sha)
    {
        var obj = ReadObject(gitDir, sha);
        if (obj == null) return null;
        try { return Encoding.UTF8.GetString(obj.Value.Content); }
        catch { return null; }
    }

    // ═══════════════════════════════════════════════════════════
    //  index（简化暂存区）
    // ═══════════════════════════════════════════════════════════

    static Dictionary<string, (string Mode, string Sha)> ReadIndex(string gitDir)
    {
        var map = new Dictionary<string, (string, string)>();
        var indexPath = Path.Combine(gitDir, "index");
        if (!File.Exists(indexPath)) return map;
        foreach (var line in File.ReadLines(indexPath))
        {
            var parts = line.Split('\t');
            if (parts.Length < 3) continue;
            map[parts[2]] = (parts[0], parts[1]);
        }
        return map;
    }

    static void WriteIndex(string gitDir, Dictionary<string, (string Mode, string Sha)> index)
    {
        var indexPath = Path.Combine(gitDir, "index");
        var sb = new StringBuilder();
        foreach (var (path, entry) in index.OrderBy(k => k.Key))
            sb.Append(entry.Mode).Append('\t').Append(entry.Sha).Append('\t').Append(path).Append('\n');
        File.WriteAllText(indexPath, sb.ToString(), new UTF8Encoding(false));
    }

    // ═══════════════════════════════════════════════════════════
    //  tree 构建（从 index 递归）
    // ═══════════════════════════════════════════════════════════

    static string BuildTree(string gitDir, Dictionary<string, (string Mode, string Sha)> index)
    {
        // 按目录分组：构建一个根 tree，子目录递归
        return BuildTreeLevel(gitDir, index, "");
    }

    static string BuildTreeLevel(string gitDir, Dictionary<string, (string Mode, string Sha)> index, string prefix)
    {
        // 收集本级条目 + 子目录
        var files = new Dictionary<string, (string Mode, string Sha)>();
        var dirs = new HashSet<string>();

        foreach (var (path, entry) in index)
        {
            if (!path.StartsWith(prefix, StringComparison.Ordinal)) continue;
            var rest = path[prefix.Length..];
            var slash = rest.IndexOf('/');
            if (slash < 0)
            {
                files[rest] = entry; // 叶子文件
            }
            else
            {
                dirs.Add(prefix + rest[..slash]);
            }
        }

        // 构建 tree 内容（排序）
        var entries = new List<(string Mode, string Name, byte[] Sha)>();
        foreach (var (name, entry) in files)
        {
            entries.Add((entry.Mode, name, HexToBytes(entry.Sha)));
        }
        foreach (var dir in dirs)
        {
            var name = dir[prefix.Length..];
            var subSha = BuildTreeLevel(gitDir, index, dir + "/");
            entries.Add(("40000", name, HexToBytes(subSha)));
        }

        // git 树排序：文件与目录统一按名排序，目录隐式视为 name + "/"
        // （git fsck 校验 treeNotSorted：foo.txt 排在 foo/ 目录前，foo/ 排在 foo2 前）。
        // 旧实现「文件全在前、目录全在后」对含子目录的仓库会产生无序 tree → 服务端 unpacker 拒绝。
        var sorted = entries
            .OrderBy(e => e.Mode == "40000" ? e.Name + "/" : e.Name, StringComparer.Ordinal)
            .ToList();

        // 序列化 tree 对象
        using var ms = new MemoryStream();
        foreach (var (mode, name, sha) in sorted)
        {
            var header = Encoding.UTF8.GetBytes($"{mode} {name}\0");
            ms.Write(header);
            ms.Write(sha);
        }
        return WriteObject(gitDir, "tree", ms.ToArray());
    }

    static byte[] HexToBytes(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }

    // ═══════════════════════════════════════════════════════════
    //  HEAD / refs
    // ═══════════════════════════════════════════════════════════

    internal static string ReadHeadBranch(string gitDir)
    {
        var headPath = Path.Combine(gitDir, "HEAD");
        if (!File.Exists(headPath)) return "master";
        var head = File.ReadAllText(headPath).Trim();
        if (head.StartsWith("ref: refs/heads/", StringComparison.Ordinal))
            return head["ref: refs/heads/".Length..].Trim();
        return "master";
    }

    internal static string? ReadHeadCommit(string gitDir)
    {
        var branch = ReadHeadBranch(gitDir);
        var refPath = Path.Combine(gitDir, "refs", "heads", branch);
        if (!File.Exists(refPath)) return null;
        return File.ReadAllText(refPath).Trim();
    }

    // ═══════════════════════════════════════════════════════════
    //  HEAD tree 展开（用于 status/diff 对比）
    // ═══════════════════════════════════════════════════════════

    static Dictionary<string, (string Mode, string Sha)> ReadHeadTreeFiles(string gitDir)
    {
        var result = new Dictionary<string, (string, string)>();
        var headSha = ReadHeadCommit(gitDir);
        if (headSha == null) return result;

        var commitObj = ReadObject(gitDir, headSha);
        if (commitObj == null) return result;
        var treeSha = ParseCommit(Encoding.UTF8.GetString(commitObj.Value.Content)).Tree;
        if (treeSha == null) return result;

        FlattenTree(gitDir, treeSha, "", result);
        return result;
    }

    internal static void FlattenTree(string gitDir, string treeSha, string prefix, Dictionary<string, (string Mode, string Sha)> result)
    {
        var obj = ReadObject(gitDir, treeSha);
        if (obj == null || obj.Value.Type != "tree") return;
        var data = obj.Value.Content;
        int pos = 0;
        while (pos < data.Length)
        {
            // 解析 "<mode> <name>\0"
            var sp = Array.IndexOf(data, (byte)' ', pos);
            if (sp < 0) break;
            var mode = Encoding.UTF8.GetString(data, pos, sp - pos);
            var nul = Array.IndexOf(data, (byte)0, sp);
            if (nul < 0) break;
            var name = Encoding.UTF8.GetString(data, sp + 1, nul - sp - 1);
            pos = nul + 1;
            if (pos + 20 > data.Length) break;
            var sha = Convert.ToHexString(data, pos, 20).ToLowerInvariant();
            pos += 20;

            var fullPath = prefix.Length == 0 ? name : prefix + "/" + name;
            if (mode == "40000")
            {
                FlattenTree(gitDir, sha, fullPath, result);
            }
            else
            {
                result[fullPath] = (mode, sha);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  worktree 扫描
    // ═══════════════════════════════════════════════════════════

    static Dictionary<string, string> ScanWorktree(string repoRoot)
    {
        var result = new Dictionary<string, string>();
        var root = Path.GetFullPath(repoRoot);
        foreach (var f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            if (IsInsideGit(f, root)) continue;
            if (Path.GetFileName(f).StartsWith(".waycoder", StringComparison.Ordinal)) continue;
            var rel = Path.GetRelativePath(root, f).Replace('\\', '/');
            var content = File.ReadAllBytes(f);
            result[rel] = HashBlob(content);
        }
        return result;
    }

    // ═══════════════════════════════════════════════════════════
    //  commit 解析 / 用户配置
    // ═══════════════════════════════════════════════════════════

    sealed class CommitData
    {
        public string? Tree;
        public string? Parent;
        public string? Author;
        public string? Date;
        public string Message = "";
    }

    static CommitData ParseCommit(string text)
    {
        var data = new CommitData();
        var lines = text.Split('\n');
        int i = 0;
        while (i < lines.Length && lines[i].Length > 0)
        {
            var line = lines[i];
            if (line.StartsWith("tree ", StringComparison.Ordinal)) data.Tree = line[5..];
            else if (line.StartsWith("parent ", StringComparison.Ordinal)) data.Parent = line[7..];
            else if (line.StartsWith("author ", StringComparison.Ordinal))
            {
                // author Name <email> ts tz
                var rest = line[7..];
                var lt = rest.LastIndexOf(" <", StringComparison.Ordinal);
                var name = lt >= 0 ? rest[..lt] : rest;
                data.Author = name;
                // 提取日期
                var m = System.Text.RegularExpressions.Regex.Match(rest, @"(\d+)\s+([+-]\d{4})");
                if (m.Success)
                {
                    var ts = long.Parse(m.Groups[1].Value);
                    data.Date = DateTimeOffset.FromUnixTimeSeconds(ts).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            i++;
        }
        if (i < lines.Length) i++; // 跳过空行
        data.Message = string.Join('\n', lines[i..]).TrimEnd('\n');
        return data;
    }

    static (string Name, string Email) ReadUserConfig(string gitDir)
    {
        var name = "WayCoder";
        var email = "waycoder@local";
        var cfgPath = Path.Combine(gitDir, "config");
        if (File.Exists(cfgPath))
        {
            foreach (var raw in File.ReadLines(cfgPath))
            {
                var line = raw.Trim();
                if (line.StartsWith("name = ", StringComparison.Ordinal)) name = line["name = ".Length..].Trim();
                else if (line.StartsWith("email = ", StringComparison.Ordinal)) email = line["email = ".Length..].Trim();
            }
        }
        return (name, email);
    }

    // ═══════════════════════════════════════════════════════════
    //  unified diff 生成（窗口同步点启发式）
    // ═══════════════════════════════════════════════════════════

    static string GenerateUnifiedDiff(string oldContent, string newContent, string path, string oldSha, string newSha)
    {
        var oldLines = oldContent.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var newLines = newContent.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        var edits = ComputeLineEdits(oldLines, newLines);
        var sb = new StringBuilder();
        sb.Append("diff --git a/").Append(path).Append(" b/").Append(path).Append('\n');
        sb.Append("--- a/").Append(path).Append('\n');
        sb.Append("+++ b/").Append(path).Append('\n');

        // 汇总为若干 hunk（含 3 行上下文）
        var hunks = GroupIntoHunks(edits);
        foreach (var (start, end) in hunks)
        {
            int oldCount = 0, newCount = 0;
            for (int i = start; i < end; i++)
            {
                if (edits[i].Kind != '+') oldCount++;
                if (edits[i].Kind != '-') newCount++;
            }
            sb.Append("@@ -").Append(start + 1).Append(',').Append(oldCount)
              .Append(" +").Append(start + 1).Append(',').Append(newCount).Append(" @@\n");
            for (int i = start; i < end; i++)
            {
                var (oi, ni, kind) = edits[i];
                var text = kind switch
                {
                    '-' => oi >= 0 ? oldLines[oi] : "",
                    '+' => ni >= 0 ? newLines[ni] : "",
                    _ => oi >= 0 ? oldLines[oi] : "",
                };
                sb.Append(kind).Append(text).Append('\n');
            }
        }
        return sb.ToString().TrimEnd('\n');
    }

    static List<(int OldIdx, int NewIdx, char Kind)> ComputeLineEdits(string[] a, string[] b)
    {
        var result = new List<(int, int, char)>();
        int i = 0, j = 0;
        while (i < a.Length || j < b.Length)
        {
            if (i < a.Length && j < b.Length && a[i] == b[j])
            {
                result.Add((i, j, ' '));
                i++; j++;
            }
            else
            {
                // 窗口内找下一个同步点
                int syncA = -1, syncB = -1;
                for (int x = i; x < Math.Min(i + 10, a.Length) && syncA < 0; x++)
                    for (int y = j; y < Math.Min(j + 10, b.Length); y++)
                        if (a[x] == b[y]) { syncA = x; syncB = y; break; }

                if (syncA >= 0)
                {
                    while (i < syncA) { result.Add((i, -1, '-')); i++; }
                    while (j < syncB) { result.Add((-1, j, '+')); j++; }
                }
                else if (i < a.Length) { result.Add((i, -1, '-')); i++; }
                else { result.Add((-1, j, '+')); j++; }
            }
        }
        return result;
    }

    static List<(int Start, int End)> GroupIntoHunks(List<(int, int, char)> edits, int context = 3)
    {
        var blocks = new List<(int Start, int End)>();
        int bi = 0;
        while (bi < edits.Count)
        {
            while (bi < edits.Count && edits[bi].Item3 == ' ') bi++;
            if (bi >= edits.Count) break;
            int s = bi;
            while (bi < edits.Count && edits[bi].Item3 != ' ') bi++;
            blocks.Add((s, bi));
        }

        var ranges = new List<(int S, int E)>();
        foreach (var (s, e) in blocks)
            ranges.Add((Math.Max(0, s - context), Math.Min(edits.Count, e + context)));

        var merged = new List<(int S, int E)>();
        foreach (var (s, e) in ranges)
        {
            if (merged.Count > 0 && s <= merged[^1].E)
                merged[^1] = (merged[^1].S, Math.Max(merged[^1].E, e));
            else
                merged.Add((s, e));
        }
        return merged;
    }

    // ═══════════════════════════════════════════════════════════
    //  对象图遍历 / checkout / remote 配置（供 GitRemote 拉取推送用）
    // ═══════════════════════════════════════════════════════════

    /// <summary>从 tipSha 沿 commit/tree/blob 遍历可达对象；stopShas 里的 sha 作边界剪枝（远端已有）。</summary>
    public static List<(string Type, string Sha, byte[] Content)> WalkReachableObjects(
        string gitDir, string tipSha, HashSet<string>? stopShas = null)
    {
        var result = new List<(string, string, byte[])>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        queue.Enqueue(tipSha);
        while (queue.Count > 0)
        {
            var sha = queue.Dequeue();
            if (!seen.Add(sha)) continue;
            if (stopShas != null && stopShas.Contains(sha)) continue;
            var obj = ReadObject(gitDir, sha);
            if (obj == null) continue;
            var (type, content) = obj.Value;
            result.Add((type, sha, content));
            if (type == "commit")
            {
                var c = ParseCommit(Encoding.UTF8.GetString(content));
                if (c.Tree != null) queue.Enqueue(c.Tree);
                if (c.Parent != null) queue.Enqueue(c.Parent);
            }
            else if (type == "tree")
            {
                int pos = 0;
                while (pos < content.Length)
                {
                    var sp = Array.IndexOf(content, (byte)' ', pos);
                    if (sp < 0) break;
                    var nul = Array.IndexOf(content, (byte)0, sp);
                    if (nul < 0) break;
                    pos = nul + 1;
                    if (pos + 20 > content.Length) break;
                    var childSha = Convert.ToHexString(content, pos, 20).ToLowerInvariant();
                    pos += 20;
                    queue.Enqueue(childSha);
                }
            }
        }
        return result;
    }

    /// <summary>把 commitSha 的 tree 展开写入工作区文件（pull 后 checkout）。返回写入文件数。</summary>
    public static int CheckoutWorktree(string gitDir, string repoRoot, string commitSha,
        Action<int, int>? onProgress = null)
    {
        var obj = ReadObject(gitDir, commitSha);
        if (obj == null) return 0;
        var treeSha = ParseCommit(Encoding.UTF8.GetString(obj.Value.Content)).Tree;
        if (treeSha == null) return 0;

        var files = new Dictionary<string, (string Mode, string Sha)>();
        FlattenTree(gitDir, treeSha, "", files);

        int written = 0;
        var total = files.Count;
        foreach (var (rel, entry) in files)
        {
            var blob = ReadObject(gitDir, entry.Sha);
            if (blob == null) continue;
            var full = Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar));
            Global.EnsureDir(full);
            File.WriteAllBytes(full, blob.Value.Content);
            written++;
            if (written % 20 == 0 || written == total)
                onProgress?.Invoke(written, total);
        }
        return written;
    }

    // ── config 读写（[remote "origin"] / [credential] 段）──

    static Dictionary<string, Dictionary<string, string>> ReadConfig(string gitDir)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        var path = Path.Combine(gitDir, "config");
        if (!File.Exists(path)) return result;
        string section = "";
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith(';')) continue;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                section = line[1..^1].Trim();
                if (!result.ContainsKey(section)) result[section] = new Dictionary<string, string>(StringComparer.Ordinal);
                continue;
            }
            if (section.Length == 0) continue;
            var eq = line.IndexOf('=');
            if (eq < 0) continue;
            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();
            result[section][key] = value;
        }
        return result;
    }

    static void WriteConfig(string gitDir, Dictionary<string, Dictionary<string, string>> config)
    {
        var sb = new StringBuilder();
        foreach (var (section, kv) in config)
        {
            sb.Append('[').Append(section).Append("]\n");
            foreach (var (k, v) in kv)
                sb.Append('\t').Append(k).Append(" = ").Append(v).Append('\n');
        }
        File.WriteAllText(Path.Combine(gitDir, "config"), sb.ToString(), new UTF8Encoding(false));
    }

    /// <summary>读远程 URL（无 remote 返回 null）。</summary>
    public static string? ReadRemoteUrl(string gitDir, string name = "origin")
    {
        var config = ReadConfig(gitDir);
        var section = $"remote \"{name}\"";
        return config.TryGetValue(section, out var kv) && kv.TryGetValue("url", out var url) ? url : null;
    }

    /// <summary>写远程 URL（含默认 fetch refspec）。</summary>
    public static void WriteRemoteUrl(string gitDir, string name, string url)
    {
        var config = ReadConfig(gitDir);
        var section = $"remote \"{name}\"";
        if (!config.TryGetValue(section, out var kv))
        {
            kv = new Dictionary<string, string>(StringComparer.Ordinal);
            config[section] = kv;
        }
        kv["url"] = url;
        if (!kv.ContainsKey("fetch")) kv["fetch"] = $"+refs/heads/*:refs/remotes/{name}/*";
        WriteConfig(gitDir, config);
    }

    /// <summary>读凭证（username + password/token），无返回 null。</summary>
    public static GitCredential? ReadCredential(string gitDir)
    {
        var config = ReadConfig(gitDir);
        if (!config.TryGetValue("credential", out var kv)) return null;
        if (!kv.TryGetValue("username", out var u)) return null;
        if (kv.TryGetValue("token", out var t)) return new GitCredential(u, t, true);
        if (kv.TryGetValue("password", out var p)) return new GitCredential(u, p, false);
        return null;
    }

    /// <summary>写凭证（明文存 .git/config；isToken=true 记 token 段，否则记 password 段）。</summary>
    public static void WriteCredential(string gitDir, string user, string secret, bool isToken = false)
    {
        var config = ReadConfig(gitDir);
        if (!config.TryGetValue("credential", out var kv))
        {
            kv = new Dictionary<string, string>(StringComparer.Ordinal);
            config["credential"] = kv;
        }
        kv["username"] = user;
        if (isToken) { kv["token"] = secret; kv.Remove("password"); }
        else { kv["password"] = secret; kv.Remove("token"); }
        WriteConfig(gitDir, config);
    }

    // ═══════════════════════════════════════════════════════════
    //  分支 / 提交比较辅助（供 GitBranch / 远程 diff 复用）
    // ═══════════════════════════════════════════════════════════

    /// <summary>读 commit 对象的 (tree, parent, message)，非 commit 返回 null。</summary>
    internal static (string? Tree, string? Parent, string Message)? ReadCommitInfo(string gitDir, string sha)
    {
        var obj = ReadObject(gitDir, sha);
        if (obj == null || obj.Value.Type != "commit") return null;
        var parsed = ParseCommit(Encoding.UTF8.GetString(obj.Value.Content));
        return (parsed.Tree, parsed.Parent, parsed.Message);
    }

    /// <summary>分支名 → tip sha（40 位 sha 原样返回）；不存在的分支/引用返回 null。</summary>
    internal static string? ResolveBranch(string gitDir, string name)
    {
        if (name.Length == 40 && name.All(Uri.IsHexDigit)) return name;
        var refPath = Path.Combine(gitDir, "refs", "heads", name);
        return File.Exists(refPath) ? File.ReadAllText(refPath).Trim() : null;
    }

    /// <summary>列出所有本地分支名（无提交的分支也含）。</summary>
    internal static List<string> ListBranches(string gitDir)
    {
        var headsDir = Path.Combine(gitDir, "refs", "heads");
        if (!Directory.Exists(headsDir)) return new List<string>();
        var names = new List<string>();
        foreach (var f in Directory.EnumerateFiles(headsDir, "*", SearchOption.AllDirectories))
            names.Add(Path.GetRelativePath(headsDir, f).Replace('\\', '/'));
        names.Sort(StringComparer.Ordinal);
        return names;
    }

    /// <summary>写 HEAD 符号引用到指定分支（切换分支用）。</summary>
    internal static void SetHeadBranch(string gitDir, string branch)
        => File.WriteAllText(Path.Combine(gitDir, "HEAD"), $"ref: refs/heads/{branch}\n", new UTF8Encoding(false));

    /// <summary>比较两个提交的 tree，输出 unified diff（diff 分支/提交用）。</summary>
    public static string DiffCommits(string repoRoot, string shaA, string shaB)
    {
        var gitDir = Path.Combine(repoRoot, ".git");
        var infoA = ReadCommitInfo(gitDir, shaA);
        var infoB = ReadCommitInfo(gitDir, shaB);
        if (infoA?.Tree == null || infoB?.Tree == null) return "无法读取提交树。";

        var filesA = new Dictionary<string, (string Mode, string Sha)>();
        var filesB = new Dictionary<string, (string Mode, string Sha)>();
        FlattenTree(gitDir, infoA.Value.Tree!, "", filesA);
        FlattenTree(gitDir, infoB.Value.Tree!, "", filesB);

        var sb = new StringBuilder();
        int shown = 0;
        var all = filesA.Keys.Concat(filesB.Keys).Distinct().OrderBy(k => k, StringComparer.Ordinal);
        foreach (var path in all)
        {
            filesA.TryGetValue(path, out var ea);
            filesB.TryGetValue(path, out var eb);
            if (ea.Sha == eb.Sha) continue;
            var oldContent = ea.Sha != null ? (ReadBlobText(gitDir, ea.Sha) ?? "") : "";
            var newContent = eb.Sha != null ? (ReadBlobText(gitDir, eb.Sha) ?? "") : "";
            sb.Append(GenerateUnifiedDiff(oldContent, newContent, path, ea.Sha ?? "", eb.Sha ?? "")).Append('\n');
            shown++;
        }
        return shown == 0 ? "两个提交内容一致。" : sb.ToString().TrimEnd('\n');
    }

    /// <summary>diff 命令分发：0/1 参数按路径过滤 worktree；参数为分支/提交时按提交树比较。</summary>
    static string DiffCommand(string repoRoot, string[] rest)
    {
        var gitDir = Path.Combine(repoRoot, ".git");
        if (rest.Length >= 2)
        {
            var a = ResolveBranch(gitDir, rest[0]);
            var b = ResolveBranch(gitDir, rest[1]);
            if (a != null && b != null) return DiffCommits(repoRoot, a, b);
            return "⚠ 无法解析为分支/提交：" + string.Join(' ', rest);
        }
        if (rest.Length == 1)
        {
            var b = ResolveBranch(gitDir, rest[0]);
            var head = ReadHeadCommit(gitDir);
            if (b != null && head != null) return DiffCommits(repoRoot, head, b);
        }
        return Diff(repoRoot, rest.Length > 0 ? rest[0] : null);
    }
}

/// <summary>
/// 本地 pack 对象存储：解析 .idx（sha→offset）定位对象在 .pack 中的偏移，按需读单对象
/// （含 ofs-delta/ref-delta 递归）。供增量 pull 时 thin pack 的 base 查找——系统 git 管理
/// （clone/`gc`）过的仓库对象在 pack 里，loose 读不到。内存友好：只索引 sha→offset，
/// 不解压全部对象（区别于 <see cref="PackFileReader.Read"/> 的全量载入）。
/// </summary>
sealed class PackStore
{
    readonly List<byte[]> _packs = new();
    readonly Dictionary<string, (int PackIdx, long Offset)> _index = new(StringComparer.Ordinal);

    public static PackStore? Load(string gitDir)
    {
        var packDir = Path.Combine(gitDir, "objects", "pack");
        if (!Directory.Exists(packDir)) return null;
        var store = new PackStore();
        bool any = false;
        foreach (var idxPath in Directory.EnumerateFiles(packDir, "*.idx"))
        {
            var packPath = idxPath[..^4] + ".pack";
            if (!File.Exists(packPath)) continue;
            byte[] pack;
            try { pack = File.ReadAllBytes(packPath); }
            catch { continue; }
            int packIdx = store._packs.Count;
            store._packs.Add(pack);
            foreach (var (sha, offset) in ParseIdx(idxPath))
                store._index[sha] = (packIdx, offset);
            any = true;
        }
        return any ? store : null;
    }

    public (string Type, byte[] Content)? Read(string sha)
    {
        if (!_index.TryGetValue(sha, out var loc)) return null;
        try { return ReadAt(loc.PackIdx, loc.Offset, 0); }
        catch { return null; }
    }

    (string Type, byte[] Content) ReadAt(int packIdx, long offset, int depth)
    {
        if (depth > 64) throw new InvalidDataException("delta 链过深");
        return PackFileReader.ReadObjectAt(_packs[packIdx], offset, sha => ResolveBase(sha, depth + 1));
    }

    (string Type, byte[] Content)? ResolveBase(string sha, int depth)
    {
        if (_index.TryGetValue(sha, out var loc))
            return ReadAt(loc.PackIdx, loc.Offset, depth);
        return null;
    }

    static IEnumerable<(string Sha, long Offset)> ParseIdx(string idxPath)
    {
        byte[] b;
        try { b = File.ReadAllBytes(idxPath); }
        catch { yield break; }
        if (b.Length < 8 + 256 * 4 || b[0] != 0xFF || b[1] != 't' || b[2] != 'O' || b[3] != 'c')
            yield break;
        int ver = ReadInt32BE(b, 4);
        if (ver != 2) yield break; // 只支持 idx v2（现代 git 默认）

        var fanout = new int[256];
        for (int i = 0; i < 256; i++) fanout[i] = ReadInt32BE(b, 8 + i * 4);
        int n = fanout[255];
        int pos = 8 + 256 * 4;

        var shas = new string[n];
        for (int i = 0; i < n; i++)
        {
            shas[i] = Convert.ToHexString(b, pos, 20).ToLowerInvariant();
            pos += 20;
        }
        pos += n * 4; // crc32（跳过）

        var offsets = new long[n];
        var largeIdx = new List<int>();
        for (int i = 0; i < n; i++)
        {
            uint o = (uint)ReadInt32BE(b, pos);
            pos += 4;
            if ((o & 0x80000000u) != 0) { offsets[i] = -1; largeIdx.Add(i); }
            else offsets[i] = o;
        }
        for (int li = 0; li < largeIdx.Count; li++)
        {
            offsets[largeIdx[li]] = ReadInt64BE(b, pos);
            pos += 8;
        }
        for (int i = 0; i < n; i++)
            yield return (shas[i], offsets[i]);
    }

    static int ReadInt32BE(byte[] b, int off)
        => (b[off] << 24) | (b[off + 1] << 16) | (b[off + 2] << 8) | b[off + 3];

    static long ReadInt64BE(byte[] b, int off)
    {
        long v = 0;
        for (int i = 0; i < 8; i++) v = (v << 8) | b[off + i];
        return v;
    }
}

/// <summary>git 远程凭证：账号密码 或 token 二选一（IsToken 区分，展示/认证据此处理）。</summary>
public readonly record struct GitCredential(string User, string Secret, bool IsToken);
