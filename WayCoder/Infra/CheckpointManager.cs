namespace WayCoder;

/// <summary>
/// 检查点 / 回滚系统 —— 通过 Git stash 或文件备份实现快照与回退。
/// </summary>
public static class CheckpointManager
{
    private static readonly List<Checkpoint> _checkpoints = [];
    private static int _nextId = 1;

    private static string CheckpointWriteDir => Global.GlobalConfigPath("checkpoints");

    private static string[] CheckpointReadDirs => new[] {
        Global.GlobalConfigPath("checkpoints"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".corecoder", "checkpoints")
    };

    private static string? FindCheckpointDir(int id)
    {
        var dirName = $"ckpt_{id:000}";
        foreach (var baseDir in CheckpointReadDirs)
        {
            var dir = Path.Combine(baseDir, dirName);
            if (Directory.Exists(dir)) return dir;
        }
        return null;
    }

    /// <summary>
    /// 磁盘恢复检查点。启动时调用，从 ~/.[waycoder|corecoder]/checkpoints/ 重建内存列表。
    /// </summary>
    public static void LoadFromDisk()
    {
        _checkpoints.Clear();
        int maxId = 0;

        foreach (var checkpointsDir in CheckpointReadDirs)
        {
            if (!Directory.Exists(checkpointsDir)) continue;
            LoadCheckpointsFrom(checkpointsDir, ref maxId);
        }

        _checkpoints.Sort((a, b) => a.Id.CompareTo(b.Id));
        _nextId = maxId + 1;
    }

    private static void LoadCheckpointsFrom(string checkpointsDir, ref int maxId)
    {
        foreach (var dir in Directory.GetDirectories(checkpointsDir, "ckpt_*"))
        {
            var metaPath = Path.Combine(dir, "_checkpoint.json");
            if (!File.Exists(metaPath)) continue;

            try
            {
                var json = File.ReadAllText(metaPath);
                var data = Json.Parse(json);
                if (data == null) continue;

                var cp = new Checkpoint
                {
                    Id = (int)(data["id"]?.AsNumber() ?? 0),
                    Description = data["description"]?.AsString() ?? "",
                    Timestamp = DateTime.TryParse(
                        data["timestamp"]?.AsString(), out var dt) ? dt : DateTime.MinValue,
                    Type = CheckpointType.FileBackup
                };

                if (cp.Id > 0)
                {
                    _checkpoints.Add(cp);
                    if (cp.Id > maxId) maxId = cp.Id;
                }
            }
            catch (Exception ex) { DebugLog.Log("Checkpoint", $"加载检查点出错: {ex.Message}"); }
        }
    }

    /// <summary>
    /// 是否在写文件前自动创建检查点。
    /// </summary>
    public static bool AutoCheckpoint { get; set; } = false;

    /// <summary>
    /// 创建检查点。优先使用 git stash，失败时回退到文件备份。
    /// </summary>
    public static async Task<Checkpoint?> CreateAsync(string description = "")
    {
        // 命令注入防护：description 会拼进 `git stash push -m "..."` 经 shell 执行，
        // 必须先清除 shell 元字符（";"/"$()"/反引号/引号等），防 `/checkpoint $(cmd)` 注入。
        description = SanitizeCheckpointLabel(description);
        var id = _nextId++;
        var timestamp = DateTime.Now;

        // 尝试 git stash
        try
        {
            var stashResult = await RunBashAsync($"git stash push -m \"WayCoder checkpoint #{id}: {description}\" 2>&1");
            if (stashResult.Contains("Saved working directory"))
            {
                // 记录该检查点的 stash 引用：后续再创建检查点会使 stash@{0} 偏移，
                // 回退时若总是 pop 最新 stash 会恢复到别的检查点
                string? stashRef = null;
                try
                {
                    var refResult = await RunBashAsync("git rev-parse stash 2>&1");
                    if (refResult.Length >= 40 && !refResult.Contains("fatal"))
                        stashRef = refResult.Trim();
                }
                catch { }
                var cp = new Checkpoint
                {
                    Id = id,
                    Description = description,
                    Timestamp = timestamp,
                    Type = CheckpointType.GitStash,
                    StashRef = stashRef
                };
                _checkpoints.Add(cp);
                return cp;
            }
        }
        catch (Exception ex) { DebugLog.Log("Checkpoint", $"Git stash 失败: {ex.Message}"); }

        // 回退：纯文件备份（不进 git stash，避免污染 stash 栈 / 吞掉用户未提交改动）
        return await CreateFileBackupAsync(id, timestamp, description);
    }

    /// <summary>
    /// 写文件前自动快照：纯文件备份（绝不用 git stash —— stash 会污染 stash 栈并吞掉用户未提交的改动）。
    /// 每轮对话首次写文件前调用一次，回滚点 = 本轮改动前的状态，实现「改坏可回滚」。
    /// </summary>
    public static async Task<Checkpoint?> CreateAutoSnapshotAsync(string description = "")
    {
        description = SanitizeCheckpointLabel(description);
        var id = _nextId++;
        var timestamp = DateTime.Now;
        return await CreateFileBackupAsync(id, timestamp, description);
    }

    /// <summary>
    /// 纯文件备份实现：检测当前 git 变更文件（含未跟踪），逐一复制到 checkpoints 目录并写元数据。
    /// 无任何变更时返回 Empty 检查点。由手动 /checkpoint 的回退分支与自动快照共用。
    /// </summary>
    private static async Task<Checkpoint?> CreateFileBackupAsync(int id, DateTime timestamp, string description)
    {
        try
        {
            var backupDir = Path.Combine(CheckpointWriteDir, $"ckpt_{id:000}");
            Directory.CreateDirectory(backupDir);

            // 获取 git 修改的文件列表
            var changedFiles = new List<string>();
            try
            {
                var gitStatus = await RunBashAsync("git diff --name-only 2>&1");
                if (!string.IsNullOrWhiteSpace(gitStatus) && !gitStatus.Contains("fatal"))
                {
                    changedFiles.AddRange(gitStatus.Split('\n', StringSplitOptions.RemoveEmptyEntries));
                }

                var gitUntracked = await RunBashAsync("git ls-files --others --exclude-standard 2>&1");
                if (!string.IsNullOrWhiteSpace(gitUntracked) && !gitUntracked.Contains("fatal"))
                {
                    changedFiles.AddRange(gitUntracked.Split('\n', StringSplitOptions.RemoveEmptyEntries));
                }
            }
            catch (Exception ex) { DebugLog.Log("Checkpoint", $"获取 Git 变更文件失败: {ex.Message}"); }

            if (changedFiles.Count == 0)
            {
                var toolChanged = Tools.EditFileTool.ChangedFiles.Where(File.Exists).ToList();
                if (toolChanged.Count > 0) changedFiles = toolChanged;
            }

            if (changedFiles.Count == 0)
            {
                var cp = new Checkpoint
                {
                    Id = id, Description = description, Timestamp = timestamp, Type = CheckpointType.Empty
                };
                _checkpoints.Add(cp);
                return cp;
            }

            // 备份每个修改的文件
            var backedUp = 0;
            foreach (var file in changedFiles)
            {
                var filePath = file.Trim();
                var srcPath = Path.GetFullPath(filePath);
                if (!File.Exists(srcPath)) continue;

                // 绝对路径 → 相对当前目录（否则 Path.Combine(backupDir, "D:\...") 因根路径忽略 backupDir，
                // destPath == srcPath，File.Copy 自拷抛 "Cannot copy onto itself"）
                var relPath = Path.GetRelativePath(Directory.GetCurrentDirectory(), srcPath);
                if (relPath.StartsWith("..") || Path.IsPathRooted(relPath))
                    relPath = Path.GetFileName(srcPath); // 文件在 cwd 外：退化到仅文件名，防 Combine 逃逸
                var destPath = Path.Combine(backupDir, relPath);
                var destDir = Path.GetDirectoryName(destPath);
                if (destDir != null) Directory.CreateDirectory(destDir);
                File.Copy(srcPath, destPath, overwrite: true);
                backedUp++;
            }

            // 保存检查点元数据（统一走 JNode 序列化：AOT 安全 + 完整转义，替代此前手写 Replace 的不完整转义）
            var metaPath = Path.Combine(backupDir, "_checkpoint.json");
            var filesNode = JNode.Array();
            foreach (var f in changedFiles) filesNode.Add(f);
            var meta = JNode.Object()
                .Set("id", id)
                .Set("description", description)
                .Set("timestamp", timestamp.ToString("O"))
                .Set("backedUp", backedUp)
                .Set("files", filesNode)
                .ToJson();
            File.WriteAllText(metaPath, meta);

            var cp2 = new Checkpoint
            {
                Id = id,
                Description = description,
                Timestamp = timestamp,
                Type = CheckpointType.FileBackup
            };
            _checkpoints.Add(cp2);
            return cp2;
        }
        catch (Exception ex)
        {
            DebugLog.Log("checkpoint", $"创建检查点失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 回退到指定检查点。不指定 id 则回退到最近一个。
    /// 指定 filePath 则只恢复该文件（支持部分路径匹配）。
    /// </summary>
    public static async Task<string> UndoAsync(int? checkpointId = null, string? filePath = null)
    {
        Checkpoint? target;
        if (checkpointId != null)
            target = _checkpoints.LastOrDefault(c => c.Id == checkpointId.Value);
        else
            target = _checkpoints.LastOrDefault();

        if (target == null)
            return "没有可回退的检查点。使用 /checkpoint 先创建一个。";

        try
        {
            if (target.Type == CheckpointType.GitStash)
            {
                // Git stash 不支持按文件恢复，全量恢复
                if (filePath != null)
                    return $"Git Stash 检查点 #{target.Id} 不支持按文件恢复。请回退全部文件后重新修改。";

                // 用记录的目标 stash 引用（旧检查点无引用时回退最新 stash，保持向后兼容）
                var stashArgs = string.IsNullOrEmpty(target.StashRef) ? "pop" : $"pop {target.StashRef}";
                var result = await RunBashAsync($"git stash {stashArgs} 2>&1");
                _checkpoints.Remove(target);
                return $"已回退到检查点 #{target.Id}: {target.Description}\n{result}";
            }
            else if (target.Type == CheckpointType.FileBackup)
            {
                var backupDir = FindCheckpointDir(target.Id);
                if (backupDir == null)
                    return $"检查点 #{target.Id} 备份目录不存在";

                var metaPath = Path.Combine(backupDir, "_checkpoint.json");
                if (!File.Exists(metaPath))
                    return $"检查点 #{target.Id} 元数据丢失";

                var meta = Json.Parse(File.ReadAllText(metaPath));
                var files = meta!["files"]!.Items.Select(f => f.AsString() ?? "").ToList();

                // 按文件过滤：支持部分路径匹配
                var toRestore = files;
                if (filePath != null)
                {
                    toRestore = files.Where(f =>
                        f.Equals(filePath, StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(filePath, StringComparison.OrdinalIgnoreCase) ||
                        f.Contains(filePath, StringComparison.OrdinalIgnoreCase)
                    ).ToList();

                    if (toRestore.Count == 0)
                        return $"检查点 #{target.Id} 中未找到匹配 \"{filePath}\" 的文件。\n可用文件:\n  " +
                               string.Join("\n  ", files);
                }

                var restored = 0;
                foreach (var file in toRestore)
                {
                    // 文件锁检查
                    if (FileLockManager.IsLockedByOther(file, "checkpoint"))
                        return $"⚠ 文件 \"{file}\" 正被其他 Agent 锁定，无法恢复。";

                    // 与创建时一致：绝对路径相对化（防止 Combine 忽略 backupDir 得到原路径自拷）
                    var relPath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
                    if (relPath.StartsWith("..") || Path.IsPathRooted(relPath))
                        relPath = Path.GetFileName(file);
                    var backupPath = Path.Combine(backupDir, relPath);
                    var destPath = Path.GetFullPath(file);

                    if (File.Exists(backupPath))
                    {
                        var destDir = Path.GetDirectoryName(destPath);
                        if (destDir != null) Directory.CreateDirectory(destDir);
                        File.Copy(backupPath, destPath, overwrite: true);
                        restored++;
                    }
                }

                // 只有全量恢复时才移除检查点
                if (filePath == null)
                    _checkpoints.Remove(target);

                return filePath != null
                    ? $"已恢复 {restored} 个文件（检查点 #{target.Id} 保留）\n  " + string.Join("\n  ", toRestore)
                    : $"已回退到检查点 #{target.Id}: {target.Description}\n恢复了 {restored} 个文件";
            }
            else
            {
                _checkpoints.Remove(target);
                return $"检查点 #{target.Id} (空快照) 已移除，无需回退。";
            }
        }
        catch (Exception ex)
        {
            return $"回退失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 获取指定检查点的文件列表。
    /// </summary>
    public static List<string> GetCheckpointFiles(int? checkpointId = null)
    {
        Checkpoint? target;
        if (checkpointId != null)
            target = _checkpoints.LastOrDefault(c => c.Id == checkpointId.Value);
        else
            target = _checkpoints.LastOrDefault();

        if (target == null || target.Type != CheckpointType.FileBackup)
            return [];

        var backupDir = FindCheckpointDir(target.Id);
        if (backupDir == null) return [];
        var metaPath = Path.Combine(backupDir, "_checkpoint.json");
        if (!File.Exists(metaPath)) return [];

        try
        {
            var meta = Json.Parse(File.ReadAllText(metaPath));
            return meta!["files"]!.Items.Select(f => f.AsString() ?? "").ToList();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// 列出所有检查点。
    /// </summary>
    public static string ListCheckpoints()
    {
        if (_checkpoints.Count == 0)
            return "（暂无检查点）";

        var lines = new List<string>();
        foreach (var cp in _checkpoints.OrderBy(c => c.Id))
        {
            var icon = cp.Type switch
            {
                CheckpointType.GitStash => "📦",
                CheckpointType.FileBackup => "📁",
                CheckpointType.Empty => "📭",
                _ => "❓"
            };
            lines.Add($"  {icon} #{cp.Id}  {cp.Description}  «dim»{cp.Timestamp:HH:mm:ss}«/»  ({cp.Type})");
        }
        return string.Join("\n", lines);
    }

    /// <summary>
    /// 回滚时间线视图（最新的检查点在最上，树状分支 + 相对时间 + 文件数）。
    /// 供 /timeline 命令可视化「改坏可回滚」：一眼看到每个回滚点距现在多久、改了哪些文件。
    /// </summary>
    public static string ListTimeline()
    {
        if (_checkpoints.Count == 0)
            return "（暂无检查点。写文件时自动快照，或手动 /checkpoint 创建。）";

        var now = DateTime.Now;
        var lines = new List<string>();
        var ordered = _checkpoints.OrderByDescending(c => c.Id).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            var cp = ordered[i];
            var icon = cp.Type switch
            {
                CheckpointType.GitStash => "📦",
                CheckpointType.FileBackup => "📁",
                CheckpointType.Empty => "📭",
                _ => "❓"
            };
            var fileCount = cp.Type == CheckpointType.FileBackup ? GetCheckpointFiles(cp.Id).Count : -1;
            var fileStr = fileCount >= 0 ? $" «dim»({fileCount} 文件)«/»" : "";
            var branch = ordered.Count == 1 ? "──" : (i == 0 ? "┌─" : (i == ordered.Count - 1 ? "└─" : "├─"));
            lines.Add($"  {branch} {icon} #{cp.Id}  «dim»{RelativeTime(now, cp.Timestamp)}«/»  {cp.Description}{fileStr}");
        }
        return string.Join("\n", lines);
    }

    /// <summary>把时间差格式化为易读相对时间（刚刚 / N 秒前 / N 分钟前 / N 小时前 / N 天前）。</summary>
    private static string RelativeTime(DateTime now, DateTime past)
    {
        var delta = now - past;
        if (delta.TotalSeconds < 10) return "刚刚";
        if (delta.TotalSeconds < 60) return $"{(int)delta.TotalSeconds} 秒前";
        if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes} 分钟前";
        if (delta.TotalHours < 24) return $"{(int)delta.TotalHours} 小时前";
        return $"{(int)delta.TotalDays} 天前";
    }

    /// <summary>
    /// 清空所有检查点。
    /// </summary>
    public static void Clear()
    {
        _checkpoints.Clear();
        _nextId = 1;
    }

    private static async Task<string> RunBashAsync(string command)
    {
        using var proc = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = CrossPlatform.ShellExecutable,
                Arguments = CrossPlatform.ShellArgs(command),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true, // 不共享主控台 stdin（防 TUI ReadKey 竞态）
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        proc.Start();
        try { proc.StandardInput.Close(); } catch { } // stdin 置 EOF
        // 同时排空 stdout 与 stderr：命令输出大量 stderr（如 git 报错）时，
        // 若只读 stdout，stderr 管道缓冲区写满会阻塞子进程 → 死锁。
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        // WaitForExitAsync 加超时：卡住的仓库/网络盘不能让 /checkpoint、/undo 永久阻塞
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await proc.WaitForExitAsync(timeoutCts.Token); }
        catch (OperationCanceledException)
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            return "[checkpoint 命令超时（15s）]";
        }
        // 守护子进程继承管道会让读取永不 EOF：加超时兜底
        _ = await WayCoder.Infra.ProcUtil.AwaitReadWithTimeoutAsync(stderrTask, TimeSpan.FromSeconds(5));
        return await WayCoder.Infra.ProcUtil.AwaitReadWithTimeoutAsync(stdoutTask, TimeSpan.FromSeconds(5)) ?? "";
    }

    /// <summary>
    /// 清除检查点描述中的 shell 元字符（命令注入防护）。
    /// description 会拼进 `git stash push -m "..."` 经 shell 执行，任何引号/命令替换/
    /// 分隔符都可能注入任意命令（如 `/checkpoint x"; rm -rf ~; #`）。
    /// 纯逻辑，便于自测。将危险字符替换为空格，保留可读的其余文本。
    /// </summary>
    internal static string SanitizeCheckpointLabel(string label)
    {
        if (string.IsNullOrEmpty(label)) return "";
        var sb = new System.Text.StringBuilder(label.Length);
        foreach (var c in label)
        {
            sb.Append(c is '"' or '\\' or '$' or '`' or ';' or '&' or '|' or '<' or '>' or '\'' or '\n' or '\r'
                ? ' '
                : c);
        }
        return sb.ToString();
    }
}

public class Checkpoint
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public CheckpointType Type { get; set; }
    /// <summary>GitStash 检查点的 stash commit 引用（回退时定位具体 stash，避免总是 pop 最新）。</summary>
    public string? StashRef { get; set; }
}

public enum CheckpointType
{
    GitStash,
    FileBackup,
    Empty
}
