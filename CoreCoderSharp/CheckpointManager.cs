namespace CoreCoderSharp;

/// <summary>
/// 检查点 / 回滚系统 —— 通过 Git stash 或文件备份实现快照与回退。
/// </summary>
public static class CheckpointManager
{
    private static readonly List<Checkpoint> _checkpoints = [];
    private static int _nextId = 1;

    /// <summary>
    /// 磁盘恢复检查点。启动时调用，从 ~/.corecoder/checkpoints/ 重建内存列表。
    /// </summary>
    public static void LoadFromDisk()
    {
        var checkpointsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".corecoder", "checkpoints");

        if (!Directory.Exists(checkpointsDir)) return;

        _checkpoints.Clear();
        int maxId = 0;

        foreach (var dir in Directory.GetDirectories(checkpointsDir, "ckpt_*"))
        {
            var metaPath = Path.Combine(dir, "_checkpoint.json");
            if (!File.Exists(metaPath)) continue;

            try
            {
                var json = File.ReadAllText(metaPath);
                var data = JsonNode.Parse(json);
                if (data == null) continue;

                var cp = new Checkpoint
                {
                    Id = data["id"]?.GetValue<int>() ?? 0,
                    Description = data["description"]?.GetValue<string>() ?? "",
                    Timestamp = DateTime.TryParse(
                        data["timestamp"]?.GetValue<string>(), out var dt) ? dt : DateTime.MinValue,
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

        _checkpoints.Sort((a, b) => a.Id.CompareTo(b.Id));
        _nextId = maxId + 1;
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
        var id = _nextId++;
        var timestamp = DateTime.Now;

        // 尝试 git stash
        try
        {
            var stashResult = await RunBashAsync($"git stash push -m \"CoreCoder checkpoint #{id}: {description}\" 2>&1");
            if (stashResult.Contains("Saved working directory"))
            {
                var cp = new Checkpoint
                {
                    Id = id,
                    Description = description,
                    Timestamp = timestamp,
                    Type = CheckpointType.GitStash
                };
                _checkpoints.Add(cp);
                return cp;
            }
        }
        catch (Exception ex) { DebugLog.Log("Checkpoint", $"Git stash 失败: {ex.Message}"); }

        // 回退：复制修改过的文件
        try
        {
            var backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".corecoder", "checkpoints", $"ckpt_{id:000}");
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

                var relPath = filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var destPath = Path.Combine(backupDir, relPath);
                var destDir = Path.GetDirectoryName(destPath);
                if (destDir != null) Directory.CreateDirectory(destDir);
                File.Copy(srcPath, destPath, overwrite: true);
                backedUp++;
            }

            // 保存检查点元数据 (AOT-safe manual JSON)
            var metaPath = Path.Combine(backupDir, "_checkpoint.json");
            var escapedDesc = description.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var filesJson = string.Join(",", changedFiles.Select(f =>
                $"\"{f.Replace("\\", "\\\\").Replace("\"", "\\\"")}\""));
            var meta = $"{{\"id\":{id},\"description\":\"{escapedDesc}\",\"timestamp\":\"{timestamp:O}\",\"backedUp\":{backedUp},\"files\":[{filesJson}]}}";
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

                var result = await RunBashAsync("git stash pop 2>&1");
                _checkpoints.Remove(target);
                return $"已回退到检查点 #{target.Id}: {target.Description}\n{result}";
            }
            else if (target.Type == CheckpointType.FileBackup)
            {
                var backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".corecoder", "checkpoints", $"ckpt_{target.Id:000}");

                if (!Directory.Exists(backupDir))
                    return $"检查点 #{target.Id} 备份目录不存在: {backupDir}";

                var metaPath = Path.Combine(backupDir, "_checkpoint.json");
                if (!File.Exists(metaPath))
                    return $"检查点 #{target.Id} 元数据丢失";

                var meta = JsonNode.Parse(File.ReadAllText(metaPath));
                var files = meta!["files"]!.AsArray().Select(f => f!.GetValue<string>()).ToList();

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

                    var relPath = file.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
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

        var backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".corecoder", "checkpoints", $"ckpt_{target.Id:000}");
        var metaPath = Path.Combine(backupDir, "_checkpoint.json");
        if (!File.Exists(metaPath)) return [];

        try
        {
            var meta = JsonNode.Parse(File.ReadAllText(metaPath));
            return meta!["files"]!.AsArray().Select(f => f!.GetValue<string>()).ToList();
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
            lines.Add($"  {icon} #{cp.Id}  {cp.Description}  [dim]{cp.Timestamp:HH:mm:ss}[/]  ({cp.Type})");
        }
        return string.Join("\n", lines);
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
                FileName = "bash",
                Arguments = $"-c \"{command.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        proc.Start();
        var result = await proc.StandardOutput.ReadToEndAsync();
        await proc.WaitForExitAsync();
        return result;
    }
}

public class Checkpoint
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public CheckpointType Type { get; set; }
}

public enum CheckpointType
{
    GitStash,
    FileBackup,
    Empty
}
