using System.Diagnostics;

namespace WayCoder;

/// <summary>
/// Git Worktree 隔离 —— 子 Agent 在独立 worktree 中运行，避免文件冲突。
///
/// 对标 Claude Code 的 isolation: "worktree" 功能。
/// 每个子 Agent 获得自己的 git worktree，修改互不干扰。
/// 退出时自动清理（无变更删除，有变更保留提示）。
/// </summary>
public static class WorktreeIsolation
{
    /// <summary>当前上下文的 worktree 路径（AsyncLocal 实现线程+异步安全）</summary>
    private static readonly AsyncLocal<string?> _currentWorktree = new();

    /// <summary>获取当前 Agent 的 worktree 路径，供 BashTool 使用</summary>
    public static string? CurrentWorktree => _currentWorktree.Value;

    /// <summary>worktree 根目录</summary>
    private static string WorktreeRoot => Path.Combine(
        Environment.CurrentDirectory, ".claude", "worktrees");

    /// <summary>最大 worktree 嵌套深度</summary>
    private const int MaxIsolationDepth = 2;

    /// <summary></summary>
    private static int _isolationCounter;

    /// <summary>
    /// 为子 Agent 创建隔离的 git worktree。
    /// 返回 worktree 路径，失败返回 null。
    /// </summary>
    /// <param name="agentId">Agent 标识符（用于目录命名）</param>
    /// <param name="baseRef">基分支/提交（默认 HEAD）</param>
    public static string? Create(string agentId, string baseRef = "HEAD")
    {
        // 深度限制
        if (_currentWorktree.Value != null)
        {
            DebugLog.Log("worktree", $"已在 worktree 中 ({_currentWorktree.Value})，跳过嵌套创建");
            return null;
        }

        try
        {
            var id = Interlocked.Increment(ref _isolationCounter);
            var safeId = SanitizeName(agentId);
            var dirName = $"agent-{id}-{safeId}";
            var worktreePath = Path.Combine(WorktreeRoot, dirName);

            Directory.CreateDirectory(WorktreeRoot);

            // 检查是否已存在
            if (Directory.Exists(worktreePath))
            {
                DebugLog.Log("worktree", $"清理已存在的 worktree: {worktreePath}");
                try { RunGit("worktree", $"remove --force \"{worktreePath}\""); }
                catch { /* 忽略 */ }
            }

            // 确保 baseRef 是有效引用
            string resolvedRef;
            try
            {
                resolvedRef = RunGit("rev-parse", $"--verify \"{baseRef}\"").Trim();
            }
            catch
            {
                // 回退到 HEAD
                resolvedRef = RunGit("rev-parse", "--verify HEAD").Trim();
            }

            // 在 worktree 根目录创建新分支
            var branchName = $"waycoder/iso-{id}-{safeId}";

            // 清理可能存在的旧分支
            try { RunGit("branch", $"-D \"{branchName}\""); } catch { /* 忽略 */ }

            // 创建 worktree + 新分支
            RunGit("worktree", $"add -b \"{branchName}\" \"{worktreePath}\" \"{resolvedRef}\"");

            _currentWorktree.Value = worktreePath;
            DebugLog.Log("worktree", $"✅ 创建 worktree: {worktreePath} (分支: {branchName}, base: {resolvedRef[..8]})");

            return worktreePath;
        }
        catch (Exception ex)
        {
            DebugLog.Log("worktree", $"❌ 创建 worktree 失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 清理 worktree。
    /// 无变更则删除，有变更则保留并提示。
    /// </summary>
    public static string Cleanup(string? worktreePath)
    {
        if (string.IsNullOrEmpty(worktreePath) || !Directory.Exists(worktreePath))
        {
            _currentWorktree.Value = null;
            return "";
        }

        try
        {
            // 保存当前目录
            var savedCwd = Environment.CurrentDirectory;

            // 检查 worktree 是否有未提交变更
            var hasChanges = false;
            try
            {
                Environment.CurrentDirectory = worktreePath;
                var status = RunGit("status", "--porcelain");
                hasChanges = !string.IsNullOrWhiteSpace(status);
            }
            catch { /* 忽略 */ }
            finally
            {
                Environment.CurrentDirectory = savedCwd;
            }

            if (hasChanges)
            {
                // 有变更：保留 worktree，只提交变更
                try
                {
                    Environment.CurrentDirectory = worktreePath;
                    RunGit("add", "-A");
                    RunGit("commit", "-m \"WayCoder: worktree 自动提交 (iso)\"");
                }
                catch { /* 忽略 */ }
                finally { Environment.CurrentDirectory = savedCwd; }

                _currentWorktree.Value = null;
                DebugLog.Log("worktree", $"📝 保留 worktree（有变更）: {worktreePath}");
                return $"⚠ Worktree 有变更，已自动提交并保留在: {worktreePath}";
            }
            else
            {
                // 无变更：删除 worktree
                try
                {
                    RunGit("worktree", $"remove \"{worktreePath}\"");
                }
                catch
                {
                    // 强制清理
                    try { RunGit("worktree", $"remove --force \"{worktreePath}\""); }
                    catch { /* 忽略 */ }
                }

                // 删除遗留目录
                try
                {
                    if (Directory.Exists(worktreePath))
                        Directory.Delete(worktreePath, recursive: true);
                }
                catch { /* 忽略 */ }

                _currentWorktree.Value = null;
                DebugLog.Log("worktree", $"🗑 已清理 worktree: {worktreePath}");
                return "";
            }
        }
        catch (Exception ex)
        {
            _currentWorktree.Value = null;
            DebugLog.Log("worktree", $"⚠ Worktree 清理异常: {ex.Message}");
            return $"⚠ Worktree 清理异常: {ex.Message}";
        }
    }

    /// <summary>
    /// 检查是否在 worktree 中运行。
    /// </summary>
    public static bool IsIsolated => _currentWorktree.Value != null;

    // ================================================================
    // 工具方法
    // ================================================================

    private static string RunGit(string command, string args)
    {
        return GitRunner.RunOrThrow($"{command} {args}");
    }

    private static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "agent";
        // 只保留字母数字和连字符
        var safe = System.Text.RegularExpressions.Regex.Replace(
            name, @"[^a-zA-Z0-9一-鿿_-]", "");
        if (safe.Length > 20) safe = safe[..20];
        return string.IsNullOrWhiteSpace(safe) ? "agent" : safe;
    }
}
