namespace WayCoder;

/// <summary>
/// 团队知识库共享 —— 通过 git 同步 .waycoder/memory/ 中的共享记忆。
///
/// 核心设计：
/// - 每个记忆的 frontmatter 中 shared: true 表示该记忆参与团队共享
/// - PullSharedAsync() 从 git remote 拉取最新共享记忆
/// - PushSharedAsync() 将本地共享记忆推送到 git remote
/// - 非共享记忆（shared: false 或未设置）完全本地，不参与同步
///
/// 安全性：
/// - 只操作 .waycoder/memory/*.md 文件，不触碰其他 git 内容
/// - push 前检查工作区清洁（仅限 memory 文件）
/// - pull 使用 --no-commit 先检查冲突
/// - 远程新文件自动加载到 StructuredMemory 索引
/// </summary>
public static class SharedMemoryManager
{
    /// <summary>团队记忆功能是否已启用</summary>
    public static bool Enabled { get; set; }

    /// <summary>Git 仓库根目录（懒加载缓存）</summary>
    private static string? _gitRoot;

    /// <summary>memory 目录在 git 中的相对路径</summary>
    private static string? _memoryGitPath;

    /// <summary>
    /// 检测当前目录是否在 git 仓库中。
    /// </summary>
    public static bool IsGitRepo()
    {
        try
        {
            if (_gitRoot != null) return true;

            var result = RunGit("rev-parse --show-toplevel");
            if (result.exitCode != 0) return false;

            _gitRoot = result.stdout.Trim();
            var cwd = Directory.GetCurrentDirectory();
            // 计算 memory 目录相对于 git 根目录的路径
            if (cwd.StartsWith(_gitRoot, StringComparison.OrdinalIgnoreCase))
            {
                var rel = cwd[_gitRoot.Length..].TrimStart('/', '\\');
                _memoryGitPath = string.IsNullOrEmpty(rel)
                    ? ".waycoder/memory"
                    : $"{rel}/.waycoder/memory";
            }
            else
            {
                _memoryGitPath = ".waycoder/memory";
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取同步状态：本地共享记忆数、远程变更数、具体变更文件列表。
    /// </summary>
    public static SyncStatus GetStatus()
    {
        var status = new SyncStatus();

        if (!IsGitRepo())
        {
            status.Error = "当前目录不在 git 仓库中";
            return status;
        }

        // 统计本地共享记忆
        var all = StructuredMemory.ListAll();
        status.LocalShared = all.Count(e => e.IsShared);
        status.LocalTotal = all.Count;

        // 检查远程是否有新变更（fetch dry-run）
        try
        {
            var fetchResult = RunGit("fetch --dry-run 2>&1");
            status.HasRemote = fetchResult.exitCode == 0;

            // 比较本地和远程的 memory 文件差异
            var diffResult = RunGit($"diff --name-only HEAD..origin/master -- {EscapePath(_memoryGitPath!)} 2>/dev/null");
            if (diffResult.exitCode == 0 && !string.IsNullOrWhiteSpace(diffResult.stdout))
            {
                var files = diffResult.stdout.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);
                status.RemoteChangedFiles.AddRange(files.Select(f => f.Trim()));
            }

            // 检查本地未推送的变更
            var localDiffResult = RunGit($"diff --name-only origin/master..HEAD -- {EscapePath(_memoryGitPath!)} 2>/dev/null");
            if (localDiffResult.exitCode == 0 && !string.IsNullOrWhiteSpace(localDiffResult.stdout))
            {
                var files = localDiffResult.stdout.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);
                status.LocalUnpushedFiles.AddRange(files.Select(f => f.Trim()));
            }
        }
        catch (Exception ex)
        {
            DebugLog.Log("SharedMemory", $"GetStatus 远程检查失败: {ex.Message}");
        }

        return status;
    }

    /// <summary>
    /// 从远程拉取共享记忆。
    /// 使用 --no-commit 先获取变更，解析后由调用方决定是否接受。
    /// </summary>
    public static async Task<PullResult> PullSharedAsync()
    {
        var result = new PullResult();

        if (!IsGitRepo())
        {
            result.Error = "当前目录不在 git 仓库中";
            return result;
        }

        if (string.IsNullOrEmpty(_memoryGitPath))
        {
            result.Error = "无法确定 memory 目录路径";
            return result;
        }

        try
        {
            // 1. 记录拉取前的 memory 文件快照
            var beforeFiles = new HashSet<string>();
            var memoryDir = StructuredMemory.MemoryDir;
            if (Directory.Exists(memoryDir))
            {
                foreach (var f in Directory.GetFiles(memoryDir, "*.md"))
                    beforeFiles.Add(Path.GetFileName(f));
            }

            // 2. Git fetch + 仅检出 memory 目录
            var fetchResult = await RunGitAsync("fetch origin");
            if (fetchResult.exitCode != 0)
            {
                result.Error = $"git fetch 失败: {fetchResult.stderr}";
                return result;
            }

            // 3. 检出远程的 memory 文件（仅限 .waycoder/memory/*.md）
            var checkoutResult = await RunGitAsync(
                $"checkout origin/master -- {EscapePath(_memoryGitPath)}/*.md 2>&1");
            if (checkoutResult.exitCode != 0)
            {
                // 远程可能没有共享记忆文件，这是正常的
                if (checkoutResult.stderr.Contains("pathspec") &&
                    checkoutResult.stderr.Contains("did not match"))
                {
                    result.Message = "远程暂无共享记忆";
                    return result;
                }
                result.Error = $"检出失败: {checkoutResult.stderr}";
                return result;
            }

            // 4. 检测新增/更新的文件
            var afterFiles = new HashSet<string>();
            if (Directory.Exists(memoryDir))
            {
                foreach (var f in Directory.GetFiles(memoryDir, "*.md"))
                    afterFiles.Add(Path.GetFileName(f));
            }

            result.NewFiles.AddRange(afterFiles.Except(beforeFiles));
            result.UpdatedFiles.AddRange(
                afterFiles.Intersect(beforeFiles).Where(f =>
                {
                    try
                    {
                        var entry = StructuredMemory.Get(Path.GetFileNameWithoutExtension(f));
                        if (entry == null) return true; // 新解析的，视为更新
                        var fi = new FileInfo(Path.Combine(memoryDir, f));
                        return fi.LastWriteTime > entry.UpdatedAt;
                    }
                    catch { return true; }
                })
            );

            // 5. 重建索引
            StructuredMemory.RebuildIndex();

            result.Success = true;
            result.Message = result.NewFiles.Count == 0 && result.UpdatedFiles.Count == 0
                ? "共享记忆已是最新"
                : $"拉取完成: {result.NewFiles.Count} 条新增, {result.UpdatedFiles.Count} 条更新";

            return result;
        }
        catch (Exception ex)
        {
            DebugLog.Log("SharedMemory", $"PullSharedAsync 异常: {ex.Message}");
            result.Error = $"拉取失败: {ex.GetType().Name}: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// 推送本地共享记忆到远程。
    /// </summary>
    /// <param name="memoryName">可选：仅推送指定记忆；null 推送所有共享记忆</param>
    public static async Task<PushResult> PushSharedAsync(string? memoryName = null)
    {
        var result = new PushResult();

        if (!IsGitRepo())
        {
            result.Error = "当前目录不在 git 仓库中";
            return result;
        }

        if (string.IsNullOrEmpty(_memoryGitPath))
        {
            result.Error = "无法确定 memory 目录路径";
            return result;
        }

        try
        {
            // 1. 确定要推送的文件
            var filesToAdd = new List<string>();
            var memoryDir = StructuredMemory.MemoryDir;

            if (memoryName != null)
            {
                var entry = StructuredMemory.Get(memoryName);
                if (entry == null)
                {
                    result.Error = $"记忆 [{memoryName}] 不存在";
                    return result;
                }
                if (!entry.IsShared)
                {
                    result.Error = $"记忆 [{memoryName}] 未标记为共享（shared: false）";
                    return result;
                }
                if (!string.IsNullOrEmpty(entry.FilePath) && File.Exists(entry.FilePath))
                    filesToAdd.Add(entry.FilePath);
            }
            else
            {
                // 推送所有共享记忆
                var all = StructuredMemory.ListAll();
                foreach (var entry in all.Where(e => e.IsShared))
                {
                    if (!string.IsNullOrEmpty(entry.FilePath) && File.Exists(entry.FilePath))
                        filesToAdd.Add(entry.FilePath);
                }
            }

            if (filesToAdd.Count == 0)
            {
                result.Message = "没有需要推送的共享记忆";
                result.Success = true;
                return result;
            }

            // 2. Git add
            foreach (var file in filesToAdd)
            {
                var addResult = await RunGitAsync($"add -- {EscapePath(file)}");
                if (addResult.exitCode != 0)
                {
                    result.Error = $"git add 失败: {addResult.stderr}";
                    return result;
                }
            }

            // 3. Git commit
            var commitMsg = memoryName != null
                ? $"memory: 共享记忆 [{memoryName}]"
                : $"memory: 共享记忆同步 ({filesToAdd.Count} 条)";
            var commitResult = await RunGitAsync($"commit -m \"{commitMsg}\"");
            if (commitResult.exitCode != 0)
            {
                // 可能没有变更（nothing to commit）
                if (commitResult.stdout.Contains("nothing to commit") ||
                    commitResult.stderr.Contains("nothing to commit"))
                {
                    result.Message = "没有新的变更需要提交";
                    result.Success = true;
                    return result;
                }
                result.Error = $"git commit 失败: {commitResult.stderr}";
                return result;
            }

            // 4. Git push
            var pushResult = await RunGitAsync("push origin HEAD");
            if (pushResult.exitCode != 0)
            {
                result.Error = $"git push 失败: {pushResult.stderr}";
                return result;
            }

            result.Success = true;
            result.PushedCount = filesToAdd.Count;
            result.Message = $"推送成功: {filesToAdd.Count} 条共享记忆已同步到远程仓库";
            return result;
        }
        catch (Exception ex)
        {
            DebugLog.Log("SharedMemory", $"PushSharedAsync 异常: {ex.Message}");
            result.Error = $"推送失败: {ex.GetType().Name}: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// 将一条记忆标记为共享，并立即推送到远程。
    /// </summary>
    public static async Task<string> ShareAsync(string name)
    {
        if (!IsGitRepo())
            return "❌ 当前目录不在 git 仓库中，无法共享记忆。";

        var entry = StructuredMemory.Get(name);
        if (entry == null)
            return $"❌ 记忆 [{name}] 不存在。";

        if (entry.IsShared)
            return $"记忆 [{name}] 已是共享状态。";

        // 更新 frontmatter 中的 shared 字段
        StructuredMemory.SetShared(name, true);

        // 推送到远程
        var pushResult = await PushSharedAsync(name);
        if (pushResult.Success)
            return $"✅ 记忆 [{name}] 已共享并推送到远程仓库。";
        else
            return $"⚠ 记忆 [{name}] 已标记为共享，但推送失败: {pushResult.Error}";
    }

    /// <summary>
    /// 取消一条记忆的共享状态。
    /// </summary>
    public static string Unshare(string name)
    {
        var entry = StructuredMemory.Get(name);
        if (entry == null)
            return $"❌ 记忆 [{name}] 不存在。";

        if (!entry.IsShared)
            return $"记忆 [{name}] 未共享。";

        StructuredMemory.SetShared(name, false);
        return $"✅ 记忆 [{name}] 已取消共享。";
    }

    // ---- 内部辅助 ----

    /// <summary>同步执行 git 命令</summary>
    private static (int exitCode, string stdout, string stderr) RunGit(string args)
        => GitRunner.Run(args);

    /// <summary>异步执行 git 命令</summary>
    private static async Task<(int exitCode, string stdout, string stderr)> RunGitAsync(string args)
        => await GitRunner.RunAsync(args);

    /// <summary>转义路径中的特殊字符（空格等）</summary>
    private static string EscapePath(string path)
    {
        // 如果路径包含空格，用引号包裹
        if (path.Contains(' '))
            return $"\"{path}\"";
        return path;
    }

    /// <summary>重置缓存（测试用）</summary>
    public static void ResetCache()
    {
        _gitRoot = null;
        _memoryGitPath = null;
    }
}

/// <summary>同步状态信息</summary>
public class SyncStatus
{
    public int LocalTotal { get; set; }
    public int LocalShared { get; set; }
    public bool HasRemote { get; set; }
    public List<string> RemoteChangedFiles { get; set; } = [];
    public List<string> LocalUnpushedFiles { get; set; } = [];
    public string? Error { get; set; }

    public bool HasRemoteChanges => RemoteChangedFiles.Count > 0;
    public bool HasLocalUnpushed => LocalUnpushedFiles.Count > 0;
}

/// <summary>拉取结果</summary>
public class PullResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public List<string> NewFiles { get; set; } = [];
    public List<string> UpdatedFiles { get; set; } = [];
}

/// <summary>推送结果</summary>
public class PushResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public int PushedCount { get; set; }
}
