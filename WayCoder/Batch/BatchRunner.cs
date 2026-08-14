using System.Diagnostics;
using System.Text;

namespace WayCoder;

/// <summary>单个批量任务的结果。</summary>
public sealed class BatchResult
{
    public string Name = "";
    public string Repo = "";
    public string? WorkDir;
    public bool Success = false;
    public int ExitCode = -1;
    /// <summary>子进程 stdout 尾部摘要。</summary>
    public string Summary = "";
    public string Error = "";
    public long DurationMs = 0;
}

/// <summary>批量任务聚合报告。</summary>
public sealed class BatchReport
{
    public List<BatchResult> Results { get; } = new();
    public string? RootDir;
    public int Total => Results.Count;
    public int Succeeded => Results.Count(r => r.Success);
    public int Failed => Results.Count(r => !r.Success);
    public long TotalDurationMs => Results.Sum(r => r.DurationMs);

    /// <summary>渲染 Markdown 报告（人读 + CI 留档）。</summary>
    public string ToMarkdown()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# WayCoder 批量任务报告");
        sb.AppendLine();
        sb.AppendLine($"- 总计: {Total} 个任务");
        sb.AppendLine($"- 成功: {Succeeded}");
        sb.AppendLine($"- 失败: {Failed}");
        sb.AppendLine($"- 总耗时: {TotalDurationMs / 1000.0:F1}s");
        if (RootDir != null) sb.AppendLine($"- 工作目录: `{RootDir}`");
        sb.AppendLine();
        foreach (var r in Results)
        {
            var icon = r.Success ? "✅" : "❌";
            sb.AppendLine($"## {icon} {r.Name}");
            sb.AppendLine();
            sb.AppendLine($"- 仓库: `{r.Repo}`");
            sb.AppendLine(r.Success
                ? $"- 耗时: {r.DurationMs / 1000.0:F1}s"
                : $"- 耗时: {r.DurationMs / 1000.0:F1}s · 退出码 {r.ExitCode}");
            if (r.WorkDir != null) sb.AppendLine($"- 工作副本: `{r.WorkDir}`");
            if (!string.IsNullOrEmpty(r.Summary))
            {
                sb.AppendLine();
                sb.AppendLine("### 摘要");
                sb.AppendLine();
                sb.AppendLine(r.Summary.Trim());
            }
            if (!string.IsNullOrEmpty(r.Error))
            {
                sb.AppendLine();
                sb.AppendLine("### 错误");
                sb.AppendLine();
                sb.AppendLine("```");
                sb.AppendLine(r.Error.Trim());
                sb.AppendLine("```");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}

/// <summary>
/// 批量任务执行器 —— 多仓库并行处理，每个任务在独立克隆副本（worktree 隔离）中，
/// 通过启动自身（-p 一次性模式）执行，天然进程级隔离 cwd 与状态。
/// </summary>
public sealed class BatchRunner
{
    /// <summary>子进程产卵委托（可注入用于测试）。</summary>
    public delegate Task<(int ExitCode, string Stdout, string Stderr)> SpawnFunc(
        BatchJob job, string workDir, string task, System.Threading.CancellationToken ct);

    /// <summary>默认实现：用当前可执行文件（Environment.ProcessPath）以 -p 一次性模式执行。</summary>
    public static async Task<(int ExitCode, string Stdout, string Stderr)> SpawnSelf(
        BatchJob job, string workDir, string task,
        System.Threading.CancellationToken ct = default, string? exePath = null)
    {
        var exe = exePath ?? Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe))
            return (-1, "", "无法定位当前可执行文件（Environment.ProcessPath 为空）");

        // 复用父进程已解析的模型/key/baseUrl/budget，避免子进程因 clone 目录无 .env 而丢失配置
        var args = new List<string> { "-p", task, "-y" };
        var cfg = Config.Instance;
        if (!string.IsNullOrWhiteSpace(cfg.Model)) { args.Add("--model"); args.Add(cfg.Model); }
        if (!string.IsNullOrWhiteSpace(cfg.BaseUrl)) { args.Add("--base-url"); args.Add(cfg.BaseUrl); }
        if (!string.IsNullOrWhiteSpace(cfg.ApiKey)) { args.Add("--api-key"); args.Add(cfg.ApiKey); }
        if (cfg.MaxBudgetUsd > 0)
        {
            args.Add("--max-budget-usd");
            args.Add(cfg.MaxBudgetUsd.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        var psi = new ProcessStartInfo(exe)
        {
            WorkingDirectory = workDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = new Process { StartInfo = psi };
        try { proc.Start(); }
        catch (Exception ex) { return (-1, "", $"启动子进程失败: {ex.Message}"); }

        // 超时/取消时终止整个进程树（含 bash 子进程）
        using var reg = ct.Register(() =>
        {
            try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); } catch { }
        });

        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (ct.IsCancellationRequested)
            return (-1, stdout, $"任务超时被终止");
        return (proc.ExitCode, stdout, stderr);
    }

    /// <summary>执行批量任务清单，返回聚合报告。</summary>
    /// <param name="rootDir">工作根目录（缺省为 cwd/.waycoder/batch）。测试可注入临时目录。</param>
    public static async Task<BatchReport> RunAsync(
        BatchSpec spec, Action<string>? log = null, SpawnFunc? spawn = null, string? rootDir = null)
    {
        var report = new BatchReport();
        var spawnFn = spawn ?? ((j, dir, t, ct) => SpawnSelf(j, dir, t, ct));

        var root = rootDir ?? Path.Combine(Directory.GetCurrentDirectory(), ".waycoder", "batch");
        var jobsDir = Path.Combine(root, "jobs");
        Directory.CreateDirectory(jobsDir);
        report.RootDir = root;

        using var sem = new SemaphoreSlim(Math.Clamp(spec.MaxParallel, BatchSpec.MinParallel, BatchSpec.MaxParallelLimit));
        var tasks = spec.Jobs.Select(job => RunOneAsync(job, jobsDir, spec, spawnFn, sem, log)).ToList();
        var results = await Task.WhenAll(tasks);
        report.Results.AddRange(results.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase));

        // 默认清理工作副本（除非 keepResults）
        if (!spec.KeepResults)
        {
            foreach (var r in report.Results)
            {
                if (r.WorkDir != null && DeleteDirRobust(r.WorkDir))
                    r.WorkDir = null;
            }
        }

        WriteReportFile(root, report);
        return report;
    }

    static async Task<BatchResult> RunOneAsync(
        BatchJob job, string jobsDir, BatchSpec spec, SpawnFunc spawn, SemaphoreSlim sem, Action<string>? log)
    {
        var result = new BatchResult { Name = job.DisplayName, Repo = job.Repo };
        var sw = Stopwatch.StartNew();
        await sem.WaitAsync();
        try
        {
            var workDir = Path.Combine(jobsDir, job.DisplayName + "_" + Guid.NewGuid().ToString("N")[..6]);
            result.WorkDir = workDir;

            // 1. 克隆仓库到隔离工作副本
            var cloneErr = CloneRepo(job, workDir);
            if (cloneErr != null) { result.Error = cloneErr; result.ExitCode = -1; return result; }
            log?.Invoke($"📦 {result.Name}: 仓库已就绪 → {workDir}");

            // 2. 子进程执行任务（带超时）
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(spec.TimeoutSec));
            var (exitCode, stdout, stderr) = await spawn(job, workDir, job.Task, cts.Token);
            result.ExitCode = exitCode;
            result.Success = exitCode == 0;
            // 剥离 ANSI 转义（子进程 -p 模式会输出 spinner 动画帧）
            result.Summary = Tail(WayCoder.Terminal.AnsiString.Strip(stdout), 4000);
            if (!string.IsNullOrEmpty(stderr)) result.Error = Tail(WayCoder.Terminal.AnsiString.Strip(stderr), 2000);
            if (exitCode != 0 && string.IsNullOrEmpty(result.Error))
                result.Error = $"子进程退出码 {exitCode}";

            log?.Invoke(result.Success
                ? $"✅ {result.Name}: 完成 ({sw.Elapsed.TotalSeconds:F1}s)"
                : $"❌ {result.Name}: 失败 ({sw.Elapsed.TotalSeconds:F1}s)");
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            result.ExitCode = -1;
            log?.Invoke($"❌ {result.Name}: {ex.Message}");
        }
        finally
        {
            sw.Stop();
            result.DurationMs = sw.ElapsedMilliseconds;
            sem.Release();
        }
        return result;
    }

    /// <summary>
    /// 健壮递归删除：Windows 下 git clone 出的 .git 对象/打包文件是只读属性，
    /// <see cref="Directory.Delete(string, bool)"/> 遇只读文件会抛 UnauthorizedAccessException，
    /// 先剥掉所有文件与目录的 ReadOnly 位再删除。目录不存在视为成功，返回是否清理完毕。
    /// </summary>
    static bool DeleteDirRobust(string path)
    {
        try
        {
            if (!Directory.Exists(path)) return true;
            foreach (var f in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                File.SetAttributes(f, File.GetAttributes(f) & ~FileAttributes.ReadOnly);
            foreach (var d in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                File.SetAttributes(d, File.GetAttributes(d) & ~FileAttributes.ReadOnly);
            Directory.Delete(path, recursive: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>克隆仓库（远程 URL 或本地路径）到隔离目录。返回错误消息，成功返回 null。</summary>
    public static string? CloneRepo(BatchJob job, string destDir)
    {
        var repo = job.Repo;
        var branch = string.IsNullOrWhiteSpace(job.Branch) ? "" : job.Branch;
        var args = branch.Length == 0
            ? $"clone \"{repo}\" \"{destDir}\""
            : $"clone -b \"{branch}\" \"{repo}\" \"{destDir}\"";
        var (code, _, err) = GitRunner.Run(args);
        if (code != 0)
            return $"git clone 失败: {(string.IsNullOrWhiteSpace(err) ? $"exit {code}" : err.Trim())}";
        if (!Directory.Exists(destDir))
            return "git clone 未生成目标目录";
        return null;
    }

    static string Tail(string text, int max)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (text.Length <= max) return text;
        return "...（截断）\n" + text[^max..];
    }

    static void WriteReportFile(string root, BatchReport report)
    {
        try
        {
            File.WriteAllText(Path.Combine(root, "batch-report.md"), report.ToMarkdown());
        }
        catch { /* 报告文件写入失败不影响结果 */ }
    }
}
