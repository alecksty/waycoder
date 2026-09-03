using System.Text;
using WayCoder.Infra;
using WayCoder.Tools;

namespace WayCoder;

/// <summary>自检状态。</summary>
public enum DoctorStatus
{
    Ok,
    Warning,
    Error,
}

/// <summary>一条自检结果。</summary>
public sealed record DoctorIssue(
    string Name,
    DoctorStatus Status,
    string Message,
    bool AutoFixable = false,
    string? FixHint = null);

/// <summary>Doctor 运行参数。Home/Cwd 显式传入，便于发行后诊断与自测隔离。</summary>
public sealed class DoctorOptions
{
    public string Home { get; set; } = Global.Home;
    public string Cwd { get; set; } = Environment.CurrentDirectory;
    public bool Fix { get; set; }
    public bool CheckApiKeyAvailability { get; set; } = true;
    public IReadOnlyList<string> Models { get; set; } = Array.Empty<string>();
}

/// <summary>Doctor 报告。</summary>
public sealed class DoctorReport
{
    public IReadOnlyList<DoctorIssue> Issues { get; }
    public bool FixRequested { get; }

    public DoctorReport(IReadOnlyList<DoctorIssue> issues, bool fixRequested)
    {
        Issues = issues;
        FixRequested = fixRequested;
    }

    public int ErrorCount => Issues.Count(i => i.Status == DoctorStatus.Error);
    public int WarningCount => Issues.Count(i => i.Status == DoctorStatus.Warning);
    public int OkCount => Issues.Count(i => i.Status == DoctorStatus.Ok);

    public string Render()
    {
        var sb = new StringBuilder();
        sb.AppendLine("🧪 WayCoder 系统自检");
        sb.AppendLine($"模式: {(FixRequested ? "自检 + 安全修复" : "只读自检")}");
        sb.AppendLine();

        foreach (var issue in Issues)
        {
            var icon = issue.Status switch
            {
                DoctorStatus.Error => "❌",
                DoctorStatus.Warning => "⚠",
                _ => "✅",
            };
            sb.AppendLine($"{icon} {issue.Name}: {issue.Message}");
            if (!string.IsNullOrEmpty(issue.FixHint))
                sb.AppendLine($"      → {issue.FixHint}");
        }

        sb.AppendLine();
        sb.AppendLine($"结果: {ErrorCount} 个错误 · {WarningCount} 个警告 · {OkCount} 项正常");
        if (ErrorCount > 0)
        {
            sb.AppendLine(FixRequested
                ? "仍有无法自动修复的问题，请按上方提示处理。"
                : "运行 /doctor fix 尝试安全修复。");
        }
        else if (WarningCount > 0)
        {
            sb.AppendLine(FixRequested
                ? "已执行安全修复，剩余为提示/需要人工处理的问题。"
                : "存在提示项，运行 /doctor fix 可执行安全修复。");
        }
        else if (FixRequested)
        {
            sb.AppendLine("自检通过，未发现需要修复的问题。");
        }
        else
        {
            sb.AppendLine("自检通过。");
        }

        return sb.ToString();
    }
}

/// <summary>
/// 发行后系统自检与安全修复引擎。
/// 只做本地检查，不联网、不调用 LLM、不打印密钥；修复仅限可安全自动处理的项目。
/// </summary>
public static class DoctorEngine
{
    private static readonly string[] ConfigDirNames = [".waycoder", ".corecoder"];
    private static readonly string[] ProjectDocFiles = ["AGENT.md", "AGENTS.md", "CLAUDE.md", ".waycoderignore", ".gitignore"];

    public static async Task<DoctorReport> RunAsync(DoctorOptions options)
    {
        var home = Path.GetFullPath(options.Home);
        var cwd = Path.GetFullPath(options.Cwd);
        var issues = new List<DoctorIssue>();

        CheckInstallation(issues);
        CheckHome(home, options.Fix, issues);
        CheckConfigJson(home, options.Fix, issues);
        CheckApiKeys(home, options.Fix, issues);
        CheckEnv(home, issues);
        CheckModels(options, issues);
        CheckErrorLogs(cwd, issues);
        CheckLocks(issues);
        CheckCheckpoints(home, issues);
        await CheckMcpAsync(cwd, home, options.Fix, issues);
        CheckHooks(home, cwd, issues);
        CheckTools(issues);
        CheckProjectFiles(cwd, issues);
        CheckTempFiles(home, cwd, options.Fix, issues);

        return new DoctorReport(issues, options.Fix);
    }

    private static void CheckInstallation(List<DoctorIssue> issues)
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(processPath) && File.Exists(processPath))
            AddOk(issues, "可执行文件", $"存在: {Path.GetFileName(processPath)}");
        else
            AddWarning(issues, "可执行文件", "无法确认当前可执行文件位置");
    }

    private static void CheckHome(string home, bool fix, List<DoctorIssue> issues)
    {
        var waycoder = Path.Combine(home, ".waycoder");
        var legacy = Path.Combine(home, ".corecoder");

        if (Directory.Exists(waycoder))
        {
            AddOk(issues, "全局配置目录", "存在 (.waycoder)");
            return;
        }

        if (Directory.Exists(legacy))
        {
            AddWarning(issues, "全局配置目录", "使用旧目录 .corecoder；新配置默认写入 .waycoder");
            return;
        }

        if (fix)
        {
            try
            {
                Directory.CreateDirectory(waycoder);
                AddOk(issues, "全局配置目录", "已创建 .waycoder");
            }
            catch (Exception ex)
            {
                AddError(issues, "全局配置目录", $"创建失败: {ex.Message}");
            }
        }
        else
        {
            AddError(issues, "全局配置目录", "缺少全局配置目录", AutoFixable: true, FixHint: "/doctor fix 会创建 .waycoder");
        }
    }

    private static void CheckConfigJson(string home, bool fix, List<DoctorIssue> issues)
    {
        var writePath = Path.Combine(home, ".waycoder", "config.json");
        var path = FindGlobalFile(home, "config.json");

        if (path == null)
        {
            if (fix)
            {
                var ok = WriteAllTextSafe(writePath, "{}");
                if (ok)
                    AddOk(issues, "config.json", "已创建空配置（默认值）");
                else
                    AddError(issues, "config.json", "创建失败（权限/磁盘）", AutoFixable: true);
            }
            else
            {
                AddWarning(issues, "config.json", "不存在，当前使用默认配置", AutoFixable: true, FixHint: "/doctor fix 会创建空配置");
            }
            return;
        }

        if (TryParseJson(path, out var root, out var error) && root?.Kind == JKind.Object)
        {
            AddOk(issues, "config.json", "JSON 可解析");
            return;
        }

        if (fix)
        {
            var backup = Global.BackupFile(path);
            if (backup == null)
            {
                AddError(issues, "config.json", $"损坏且备份失败: {error}", AutoFixable: true);
                return;
            }

            var ok = WriteAllTextSafe(writePath, "{}");
            if (ok)
                AddWarning(issues, "config.json", $"已备份损坏文件并重置为空配置（备份: {Path.GetFileName(backup)}）");
            else
                AddError(issues, "config.json", $"已备份，但重置失败: {error}");
        }
        else
        {
            AddError(issues, "config.json", $"损坏: {error}", AutoFixable: true, FixHint: "/doctor fix 会备份并重置");
        }
    }

    private static void CheckApiKeys(string home, bool fix, List<DoctorIssue> issues)
    {
        var path = FindGlobalFile(home, "api_keys.json");
        if (path == null)
        {
            AddOk(issues, "api_keys.json", "未配置（可选）");
            return;
        }

        if (!TryParseJson(path, out var root, out var error) || root?.Kind != JKind.Array)
        {
            if (fix && Global.BackupFile(path) != null)
            {
                AddError(issues, "api_keys.json", $"已备份损坏文件，需手动恢复；未自动覆盖密钥数据（{error}）");
            }
            else
            {
                AddError(issues, "api_keys.json", $"损坏: {error}", AutoFixable: true, FixHint: "密钥文件只备份，不自动改写");
            }
            return;
        }

        var bad = root.Items.Count(i =>
            i.Kind != JKind.Object ||
            string.IsNullOrWhiteSpace(i["provider"]?.AsString()) ||
            string.IsNullOrWhiteSpace(i["apikey"]?.AsString()));
        if (bad > 0)
            AddWarning(issues, "api_keys.json", $"{bad} 条记录缺少 provider/apikey（不自动修改）");
        else
            AddOk(issues, "api_keys.json", $"{root.Count} 个服务商已配置（密钥不外显）");
    }

    private static void CheckEnv(string home, List<DoctorIssue> issues)
    {
        var path = FindGlobalFile(home, ".env");
        if (path == null)
        {
            AddOk(issues, ".env", "未配置（可选）");
            return;
        }

        try
        {
            var lines = File.ReadAllLines(path);
            var bad = new List<int>();
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith('#')) continue;
                var eq = line.IndexOf('=');
                if (eq <= 0 || string.IsNullOrWhiteSpace(line[..eq]))
                    bad.Add(i + 1);
            }

            if (bad.Count > 0)
                AddError(issues, ".env", $"第 {string.Join(", ", bad.Take(3))} 行格式非法（不自动修改）");
            else
                AddOk(issues, ".env", $"{lines.Length} 行语法正常");
        }
        catch (Exception ex)
        {
            AddError(issues, ".env", $"读取失败: {ex.Message}");
        }
    }

    private static void CheckModels(DoctorOptions options, List<DoctorIssue> issues)
    {
        var models = options.Models
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Select(m => m.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (models.Count == 0)
        {
            AddWarning(issues, "模型配置", "未读取到模型配置");
            return;
        }

        var unknown = models.Where(m => ModelCatalog.Find(m) == null).ToList();
        if (unknown.Count > 0)
            AddWarning(issues, "模型配置", $"未收录: {string.Join(", ", unknown)}（可检查 config.json 或自定义模型）");
        else
            AddOk(issues, "模型配置", $"大/小模型配置正常: {string.Join(", ", models)}");

        if (!options.CheckApiKeyAvailability) return;

        var missingKeys = new List<string>();
        foreach (var model in models)
        {
            var info = ModelCatalog.Find(model);
            if (info == null || info.ProviderId is "local" or "custom") continue;
            if (!ApiKeyStore.HasKeyFor(info.ProviderId, model))
                missingKeys.Add(model);
        }

        if (missingKeys.Count > 0)
            AddWarning(issues, "API Key", $"未检测到可用 API Key: {string.Join(", ", missingKeys)}");
        else
            AddOk(issues, "API Key", "当前模型已有可用密钥或无需密钥");
    }

    private static void CheckErrorLogs(string cwd, List<DoctorIssue> issues)
    {
        var logsDir = Path.Combine(cwd, "logs");
        if (!Directory.Exists(logsDir))
        {
            AddOk(issues, "错误日志", "未发现 logs 目录");
            return;
        }

        try
        {
            var files = Directory.GetFiles(logsDir, "error_*.log").OrderByDescending(f => f).Take(5).ToList();
            if (files.Count == 0)
            {
                AddOk(issues, "错误日志", "未发现 error_*.log");
                return;
            }

            var count = 0;
            string? sample = null;
            foreach (var file in files)
            {
                foreach (var line in File.ReadLines(file).TakeLast(2000))
                {
                    if (!line.Contains("ERROR", StringComparison.OrdinalIgnoreCase) &&
                        !line.Contains("FATAL", StringComparison.OrdinalIgnoreCase)) continue;
                    count++;
                    sample ??= line;
                }
            }

            if (count > 0)
            {
                var preview = sample?.Trim();
                if (preview?.Length > 160) preview = preview[..160] + "…";
                AddWarning(issues, "错误日志", $"最近日志含 {count} 条 ERROR/FATAL{(preview == null ? "" : $"，示例: {preview}")}");
            }
            else
            {
                AddOk(issues, "错误日志", $"{files.Count} 个日志文件，最近记录无 ERROR/FATAL");
            }
        }
        catch (Exception ex)
        {
            AddError(issues, "错误日志", $"读取失败: {ex.Message}");
        }
    }

    private static void CheckLocks(List<DoctorIssue> issues)
    {
        var locks = FileLockManager.GetAllLocks();
        if (locks.Count == 0)
        {
            AddOk(issues, "文件锁", "无活跃锁");
            return;
        }

        var preview = string.Join("; ", locks.Take(3).Select(l => $"{l.FilePath} ({l.AgentId})"));
        AddWarning(issues, "文件锁", $"{locks.Count} 个活跃文件锁: {preview}");
    }

    private static void CheckCheckpoints(string home, List<DoctorIssue> issues)
    {
        var dirs = new[]
        {
            Path.Combine(home, ".waycoder", "checkpoints"),
            Path.Combine(home, ".corecoder", "checkpoints"),
        };
        var total = 0;
        var bad = new List<string>();

        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir)) continue;
            try
            {
                foreach (var ckptDir in Directory.GetDirectories(dir, "ckpt_*"))
                {
                    var meta = Path.Combine(ckptDir, "_checkpoint.json");
                    if (!File.Exists(meta)) continue;
                    total++;
                    if (!TryParseJson(meta, out var node, out _) || node?.Kind != JKind.Object)
                        bad.Add(Path.GetFileName(ckptDir));
                }
            }
            catch (Exception ex)
            {
                AddError(issues, "检查点", $"扫描失败: {ex.Message}");
                return;
            }
        }

        if (bad.Count > 0)
            AddError(issues, "检查点", $"{bad.Count}/{total} 个元数据损坏: {string.Join(", ", bad)}（不自动删除）");
        else if (total > 0)
            AddOk(issues, "检查点", $"{total} 个元数据可解析");
        else
            AddOk(issues, "检查点", "未发现检查点");
    }

    private static async Task CheckMcpAsync(string cwd, string home, bool fix, List<DoctorIssue> issues)
    {
        var configPath = FindUpwardConfigFile(cwd, home, "mcp_servers.json");
        if (configPath == null)
        {
            AddOk(issues, "MCP", "未配置 mcp_servers.json");
            return;
        }

        if (!TryParseJson(configPath, out var root, out var error) || root?.Kind != JKind.Array)
        {
            AddError(issues, "MCP", $"配置损坏: {error}");
            return;
        }

        var servers = root.Items
            .Where(i => i.Kind == JKind.Object)
            .Select(i => i["name"]?.AsString())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!.Trim())
            .ToList();
        var malformed = root.Count - servers.Count;
        if (malformed > 0)
            AddWarning(issues, "MCP", $"{malformed} 条服务器记录缺少 name");

        var failed = McpManager.Servers
            .Where(s => s.Status == McpServerStatus.Failed)
            .ToList();

        if (failed.Count == 0)
        {
            AddOk(issues, "MCP", servers.Count == 0
                ? "配置可解析（未发现服务器）"
                : $"配置可解析（{servers.Count} 个服务器，当前无连接失败）");
            return;
        }

        var failedNames = string.Join(", ", failed.Select(s => s.Name));
        if (!fix)
        {
            AddWarning(issues, "MCP", $"连接失败: {failedNames}", AutoFixable: true, FixHint: "/doctor fix 会尝试重连失败服务器");
            return;
        }

        var oldOverride = McpManager.ConfigPathOverride;
        McpManager.ConfigPathOverride = configPath;
        var reloaded = 0;
        var stillFailed = 0;
        try
        {
            foreach (var server in failed)
            {
                try
                {
                    _ = await McpManager.ReloadAsync(server.Name);
                    var status = McpManager.Servers.FirstOrDefault(s =>
                        string.Equals(s.Name, server.Name, StringComparison.OrdinalIgnoreCase))?.Status;
                    if (status == McpServerStatus.Failed) stillFailed++;
                    else reloaded++;
                }
                catch
                {
                    stillFailed++;
                }
            }
        }
        finally
        {
            McpManager.ConfigPathOverride = oldOverride;
        }

        if (stillFailed == 0)
            AddOk(issues, "MCP", $"已重连 {reloaded} 个失败服务器");
        else
            AddError(issues, "MCP", $"重连后仍有 {stillFailed} 个失败（详见 /mcp）");
    }

    private static void CheckHooks(string home, string cwd, List<DoctorIssue> issues)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dirName in ConfigDirNames)
        {
            var p = Path.Combine(home, dirName, "hooks", "hooks.json");
            if (File.Exists(p)) candidates.Add(p);
        }
        var project = FindUpwardConfigFile(cwd, home, Path.Combine("hooks", "hooks.json"));
        if (project != null) candidates.Add(project);

        if (candidates.Count == 0)
        {
            AddOk(issues, "Hooks", "未配置 hooks.json");
            return;
        }

        foreach (var path in candidates)
            CheckHookConfig(path, issues);
    }

    private static void CheckHookConfig(string path, List<DoctorIssue> issues)
    {
        var hooksDir = Path.GetDirectoryName(path);
        if (hooksDir == null)
        {
            AddError(issues, "Hooks", $"无法解析目录: {path}");
            return;
        }

        if (!TryParseJson(path, out var root, out var error) || root?.Kind != JKind.Object)
        {
            AddError(issues, "Hooks", $"hooks.json 损坏: {error}");
            return;
        }

        if (root["matchers"] is not { Kind: JKind.Array } matchers)
        {
            AddError(issues, "Hooks", "hooks.json 缺少 matchers 数组");
            return;
        }

        var missing = new List<string>();
        foreach (var matcher in matchers.Items)
        {
            if (matcher.Kind != JKind.Object) continue;
            if (matcher["hooks"] is not { Kind: JKind.Array } hooks) continue;
            foreach (var hook in hooks.Items)
            {
                if (hook.Kind != JKind.Object) continue;
                var command = hook["command"]?.AsString();
                if (string.IsNullOrWhiteSpace(command))
                {
                    missing.Add("(空 command)");
                    continue;
                }

                var script = Path.IsPathRooted(command) ? command : Path.Combine(hooksDir, command);
                if (!File.Exists(script))
                    missing.Add(command);
            }
        }

        var label = $"Hooks ({Path.GetFileName(Path.GetDirectoryName(path))})";
        if (missing.Count > 0)
            AddWarning(issues, label, $"{missing.Count} 个引用脚本不存在: {string.Join(", ", missing.Take(3))}（不自动删除配置）");
        else
            AddOk(issues, label, "配置可解析，引用脚本均存在");
    }

    private static void CheckTools(List<DoctorIssue> issues)
    {
        var tools = ToolRegistry.AllTools;
        var duplicates = tools
            .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} x{g.Count()}")
            .ToList();

        if (duplicates.Count > 0)
            AddWarning(issues, "工具注册表", $"重复工具名: {string.Join(", ", duplicates)}");
        else
            AddOk(issues, "工具注册表", $"{tools.Count} 个工具，无重复");
    }

    private static void CheckProjectFiles(string cwd, List<DoctorIssue> issues)
    {
        var found = 0;
        var bad = new List<string>();
        foreach (var name in ProjectDocFiles)
        {
            var path = Path.Combine(cwd, name);
            if (!File.Exists(path)) continue;
            found++;
            try
            {
                _ = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                bad.Add($"{name}: {ex.Message}");
            }
        }

        if (bad.Count > 0)
            AddError(issues, "项目文件", $"读取失败: {string.Join("; ", bad)}");
        else if (found > 0)
            AddOk(issues, "项目文件", $"{found} 个关键文件可读");
        else
            AddOk(issues, "项目文件", "未发现这些可选文件（跳过）");
    }

    private static void CheckTempFiles(string home, string cwd, bool fix, List<DoctorIssue> issues)
    {
        var roots = new[]
        {
            Path.Combine(home, ".waycoder"),
            Path.Combine(home, ".corecoder"),
            Path.Combine(cwd, ".waycoder"),
            Path.Combine(cwd, ".corecoder"),
        }.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var found = new List<string>();
        foreach (var root in roots)
        {
            if (!Directory.Exists(root)) continue;
            try
            {
                foreach (var file in Directory.EnumerateFiles(root, "*.tmp", SearchOption.AllDirectories))
                {
                    if (Path.GetExtension(file).Equals(".tmp", StringComparison.OrdinalIgnoreCase) &&
                        IsUnder(file, root))
                        found.Add(file);
                }
            }
            catch (Exception ex)
            {
                AddError(issues, "临时文件", $"扫描失败: {ex.Message}");
                return;
            }
        }

        if (found.Count == 0)
        {
            AddOk(issues, "临时文件", "无残留 .tmp");
            return;
        }

        if (!fix)
        {
            AddWarning(issues, "临时文件", $"发现 {found.Count} 个残留 .tmp: {string.Join(", ", found.Take(3).Select(Path.GetFileName))}",
                AutoFixable: true, FixHint: "/doctor fix 会清理 .tmp 文件");
            return;
        }

        var deleted = 0;
        var failed = 0;
        foreach (var file in found)
        {
            try
            {
                File.Delete(file);
                deleted++;
            }
            catch
            {
                failed++;
            }
        }

        if (failed == 0)
            AddOk(issues, "临时文件", $"已清理 {deleted} 个残留 .tmp");
        else
            AddError(issues, "临时文件", $"清理 {deleted} 个，失败 {failed} 个");
    }

    private static string? FindGlobalFile(string home, string relativePath)
    {
        foreach (var dirName in ConfigDirNames)
        {
            var p = Path.Combine(home, dirName, relativePath);
            if (File.Exists(p)) return p;
        }
        return null;
    }

    private static string? FindUpwardConfigFile(string cwd, string home, string relativePath)
    {
        var current = Path.GetFullPath(cwd);
        var homeRoot = Path.GetFullPath(home);
        while (current != null)
        {
            foreach (var dirName in ConfigDirNames)
            {
                var p = Path.Combine(current, dirName, relativePath);
                if (File.Exists(p)) return p;
            }

            if (string.Equals(current, homeRoot, StringComparison.OrdinalIgnoreCase)) break;
            var parent = Path.GetDirectoryName(current);
            if (parent == null || string.Equals(parent, current, StringComparison.OrdinalIgnoreCase)) break;
            current = parent;
        }
        return null;
    }

    private static bool TryParseJson(string path, out JNode? node, out string error)
    {
        node = null;
        error = "";
        try
        {
            var text = File.ReadAllText(path);
            node = Json.Parse(text);
            if (node == null)
            {
                error = "文件为空或全空白";
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool WriteAllTextSafe(string path, string content)
    {
        try
        {
            Global.EnsureDir(path);
            File.WriteAllText(path, content);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsUnder(string path, string root)
    {
        var full = Path.GetFullPath(path);
        var baseDir = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return full.StartsWith(baseDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static void AddOk(List<DoctorIssue> issues, string name, string message)
        => issues.Add(new DoctorIssue(name, DoctorStatus.Ok, message));

    private static void AddWarning(List<DoctorIssue> issues, string name, string message, bool AutoFixable = false, string? FixHint = null)
        => issues.Add(new DoctorIssue(name, DoctorStatus.Warning, message, AutoFixable, FixHint));

    private static void AddError(List<DoctorIssue> issues, string name, string message, bool AutoFixable = false, string? FixHint = null)
        => issues.Add(new DoctorIssue(name, DoctorStatus.Error, message, AutoFixable, FixHint));
}
