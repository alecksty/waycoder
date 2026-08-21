using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WayCoder;

/// <summary>
/// 进程沙箱管理器 —— 在 full-auto 模式下限制 bash 命令的执行环境。
///
/// 三层沙箱级别（与 PermissionManager 协同）：
///   suggest   — 所有危险工具需确认（= PermissionManager.Mode.Ask）
///   auto-edit — 文件编辑自动，bash 需确认（= PermissionManager.Mode.Auto）
///   full-auto — 全部自动，bash 在沙箱中运行，受资源限制
///
/// 沙箱限制：
///   1. 环境变量清理（防止 API Key 泄露、网络访问）
///   2. 工作目录锁定（防止 cd 逃逸项目目录）
///   3. 文件写入限制（阻止写入系统目录）
///   4. 额外危险命令拦截（sudo、mount 等）
///   5. 进程内存监控（超 1GB 自动 kill）
/// </summary>
public static class SandboxManager
{
    /// <summary>当前沙箱级别</summary>
    public static string Level { get; set; } = "suggest";

    /// <summary>是否处于沙箱模式（full-auto）</summary>
    public static bool IsSandboxed => Level == "full-auto";

    /// <summary>是否处于智能自动模式</summary>
    public static bool IsSmartAuto => Level == "smart-auto";

    // 可覆写的私有字段（测试可用），默认从 Config.Instance 读取
    private static long? _maxMemoryBytesOverride;
    private static int? _maxCpuTimeSecondsOverride;
    private static bool? _allowNetworkOverride;

    /// <summary>内存上限（字节），默认从 WAYCODER_SANDBOX_MAX_MEMORY_MB 读取（默认 1GB）</summary>
    public static long MaxMemoryBytes
    {
        get => _maxMemoryBytesOverride ?? (long)Config.Instance.SandboxMaxMemoryMb * 1024 * 1024;
        set => _maxMemoryBytesOverride = value;
    }

    /// <summary>CPU 时间上限（秒），默认从 WAYCODER_SANDBOX_MAX_CPU_SEC 读取（默认 300）</summary>
    public static int MaxCpuTimeSeconds
    {
        get => _maxCpuTimeSecondsOverride ?? Config.Instance.SandboxMaxCpuSeconds;
        set => _maxCpuTimeSecondsOverride = value;
    }

    /// <summary>是否允许网络访问，默认从 WAYCODER_SANDBOX_ALLOW_NETWORK 读取（默认 false）</summary>
    public static bool AllowNetwork
    {
        get => _allowNetworkOverride ?? Config.Instance.SandboxAllowNetwork;
        set => _allowNetworkOverride = value;
    }

    /// <summary>允许的根目录（null = 不限制）</summary>
    public static string? AllowedDirectory { get; set; }

    // ---- 沙箱额外危险命令（比 BashTool.DangerousPatterns 更严格） ----

    private static readonly (Regex Pattern, string Reason)[] SandboxBlocked =
    [
        (new(@"\bsudo\b"), "sudo 提权"),
        (new(@"\bsu\b(?=\s+-)"), "切换用户"),
        (new(@"\bchown\b"), "修改文件所有者"),
        (new(@"\bmount\b"), "挂载文件系统"),
        (new(@"\bumount\b"), "卸载文件系统"),
        (new(@"\biptables\b"), "修改防火墙"),
        (new(@"\bnc\b"), "网络连接（沙箱禁止）"),
        (new(@"\btelnet\b"), "网络连接（沙箱禁止）"),
        (new(@"\bssh\b(?=\s+\w+@)"), "SSH 远程连接"),
        (new(@"\bscp\b"), "SCP 远程传输"),
        (new(@"\bwget\b"), "网络下载（沙箱禁止）"),
        (new(@"\bcurl\b"), "网络请求（沙箱禁止）"),
        (new(@"\bsystemctl\b"), "系统服务管理"),
    ];

    // 沙箱允许的网络相关命令（本地通信）
    private static readonly Regex[] NetworkAllowed =
    [
        new(@"\bcurl\s+localhost\b"),
        new(@"\bcurl\s+127\.0\.0\.1\b"),
        new(@"\bwget\s+localhost\b"),
    ];

    /// <summary>
    /// 检查命令是否违反沙箱规则。返回违规原因，null 表示通过。
    /// </summary>
    public static string? CheckSandboxViolation(string command, string cwd)
    {
        if (!IsSandboxed) return null;

        // 1. 沙箱额外危险命令
        foreach (var (pattern, reason) in SandboxBlocked)
        {
            if (!pattern.IsMatch(command)) continue;

            // 网络命令检查是否访问 localhost（允许）
            if (reason.Contains("网络") || reason.Contains("下载") || reason.Contains("请求"))
            {
                if (NetworkAllowed.Any(na => na.IsMatch(command)))
                    continue;
            }

            return reason;
        }

        // 2. 工作目录逃逸检测
        var violation = CheckDirectoryEscape(command, cwd);
        if (violation != null) return violation;

        // 3. 文件写入系统目录检测
        violation = CheckSystemWrite(command);
        if (violation != null) return violation;

        return null;
    }

    /// <summary>
    /// 为沙箱模式构建安全的 ProcessStartInfo。
    /// 调用时机：CheckSandboxViolation 返回 null 之后。
    /// </summary>
    public static ProcessStartInfo CreateSandboxedProcess(string command, string cwd)
    {
        var psi = new ProcessStartInfo
        {
            FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash",
            Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? $"/c \"{command}\""
                : $"-c \"{command.Replace("\"", "\\\"")}\"",
            WorkingDirectory = AllowedDirectory ?? cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // 清理环境变量：防止 API Key 泄露和网络访问
        SanitizeEnvironment(psi);

        return psi;
    }

    /// <summary>
    /// 清理进程环境变量，防止沙箱逃逸。
    /// </summary>
    private static void SanitizeEnvironment(ProcessStartInfo psi)
    {
        // 继承当前环境
        foreach (DictionaryEntry kv in Environment.GetEnvironmentVariables())
        {
            var key = kv.Key?.ToString() ?? "";
            var value = kv.Value?.ToString() ?? "";

            // 清除敏感变量
            if (IsSensitiveEnv(key))
            {
                psi.Environment[key] = "";
                continue;
            }

            psi.Environment[key] = value;
        }

        // 强制覆盖：阻止网络代理
        psi.Environment["HTTP_PROXY"] = "";
        psi.Environment["HTTPS_PROXY"] = "";
        psi.Environment["http_proxy"] = "";
        psi.Environment["https_proxy"] = "";
        psi.Environment["ALL_PROXY"] = "";
        psi.Environment["all_proxy"] = "";
        psi.Environment["NO_PROXY"] = "*";
        psi.Environment["no_proxy"] = "*";

        // 覆盖 HOME 防止配置文件泄露（Linux/macOS）
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            psi.Environment["HOME"] = "/tmp/waycoder-sandbox";
        }
    }

    /// <summary>判断环境变量是否包含敏感信息</summary>
    private static bool IsSensitiveEnv(string key)
    {
        var upper = key.ToUpperInvariant();
        return upper.Contains("API_KEY") || upper.Contains("TOKEN")
            || upper.Contains("SECRET") || upper.Contains("PASSWORD")
            || upper.Contains("CREDENTIAL") || upper.Contains("AUTH")
            || upper.StartsWith("WAYCODER_")
            || upper.StartsWith("OPENAI_") || upper.StartsWith("DEEPSEEK_")
            || upper.StartsWith("ANTHROPIC_") || upper.StartsWith("GITHUB_TOKEN")
            || upper.StartsWith("GITLAB_TOKEN");
    }

    /// <summary>
    /// 检测 cd / pushd 命令是否试图逃逸项目目录。
    /// </summary>
    private static string? CheckDirectoryEscape(string command, string cwd)
    {
        if (AllowedDirectory == null) return null;

        // 提取所有 cd / pushd 的目标目录
        var cdPattern = new Regex(@"(?:^|&&|;|\n)\s*(?:cd|pushd)\s+(.+?)(?=\s*(?:&&|;|\n|$))",
            RegexOptions.None, TimeSpan.FromMilliseconds(100));

        foreach (Match m in cdPattern.Matches(command))
        {
            var target = m.Groups[1].Value.Trim().Trim('\'', '"');
            if (string.IsNullOrEmpty(target)) continue;

            // 解析相对路径
            string resolved;
            try
            {
                resolved = Path.GetFullPath(Path.Combine(cwd,
                    target.StartsWith('~')
                        ? target.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
                        : target));
            }
            catch (Exception ex) { DebugLog.Log("SandboxManager", $"路径解析失败: {ex.Message}"); continue; }

            // 规范化路径
            var normalizedTarget = Path.GetFullPath(resolved).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedAllowed = Path.GetFullPath(AllowedDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (!normalizedTarget.StartsWith(normalizedAllowed, StringComparison.OrdinalIgnoreCase))
                return $"禁止 cd 到项目目录外：{target} → {normalizedTarget}";
        }

        return null;
    }

    /// <summary>
    /// 检测文件重定向是否写入系统目录。
    /// </summary>
    private static string? CheckSystemWrite(string command)
    {
        // 匹配重定向写入：> >> 2> &>
        var redirectPattern = new Regex(@"[12]?&?\d*>\s*(\S+)",
            RegexOptions.None, TimeSpan.FromMilliseconds(100));

        var systemDirs = new[]
        {
            "/etc/", "/boot/", "/sys/", "/proc/", "/dev/",
            "C:\\Windows", "C:\\Program Files", "C:\\Program Files (x86)",
            "/System/", "/Library/System/",
        };

        foreach (Match m in redirectPattern.Matches(command))
        {
            var path = m.Groups[1].Value.Trim('\'', '"');
            if (string.IsNullOrEmpty(path) || path == "/dev/null") continue;

            foreach (var sysDir in systemDirs)
            {
                if (path.StartsWith(sysDir, StringComparison.OrdinalIgnoreCase)
                    || path.Replace("/", "\\").StartsWith(sysDir.Replace("/", "\\"), StringComparison.OrdinalIgnoreCase))
                    return $"禁止写入系统目录：{path}";
            }
        }

        return null;
    }

    /// <summary>
    /// 监控进程 CPU 处理器时间，超限则 kill。返回 CPU 超限的消息，null 表示正常。
    /// </summary>
    public static async Task<string?> MonitorCpuAsync(Process proc, CancellationToken cancel)
    {
        if (!IsSandboxed) return null;

        try
        {
            while (!proc.HasExited)
            {
                await Task.Delay(2000, cancel);
                if (proc.HasExited) break;

                try
                {
                    proc.Refresh();
                    var cpuSeconds = proc.TotalProcessorTime.TotalSeconds;
                    if (cpuSeconds > MaxCpuTimeSeconds)
                    {
                        proc.Kill(entireProcessTree: true);
                        return $"⛔ 沙箱终止：CPU 时间超限（{cpuSeconds:F1}秒 > {MaxCpuTimeSeconds}秒）";
                    }
                }
                catch
                {
                    // 进程可能已退出
                    break;
                }
            }
        }
        catch (OperationCanceledException ex) { DebugLog.Log("SandboxManager", $"CPU 监控取消: {ex.Message}"); }

        return null;
    }

    /// <summary>
    /// 监控进程内存使用，超限则 kill。返回内存超限进程的消息，null 表示正常。
    /// </summary>
    public static async Task<string?> MonitorMemoryAsync(Process proc, CancellationToken cancel)
    {
        if (!IsSandboxed) return null;

        try
        {
            while (!proc.HasExited)
            {
                await Task.Delay(2000, cancel);
                if (proc.HasExited) break;

                try
                {
                    proc.Refresh();
                    if (proc.WorkingSet64 > MaxMemoryBytes)
                    {
                        var usedMb = proc.WorkingSet64 / (1024 * 1024);
                        proc.Kill(entireProcessTree: true);
                        return $"⛔ 沙箱终止：内存超限（{usedMb}MB > {MaxMemoryBytes / 1024 / 1024}MB）";
                    }
                }
                catch
                {
                    // 进程可能已退出
                    break;
                }
            }
        }
        catch (OperationCanceledException ex) { DebugLog.Log("SandboxManager", $"进程等待取消/超时: {ex.Message}"); }

        return null;
    }

    /// <summary>
    /// 根据字符串设置沙箱级别。
    /// </summary>
    public static void SetLevel(string level)
    {
        var normalized = level.ToLowerInvariant();

        // yolo 是纯权限模式：畅通无阻、不启用沙箱（沙箱会拦 curl/wget/sudo 等命令，
        // 与"全部允许"语义矛盾）。显式 full-auto 才启用沙箱。
        if (normalized is "yolo" or "god")
        {
            Level = normalized;
            PermissionManager.SetMode("yolo");
            return;
        }

        Level = normalized switch
        {
            "full-auto" => "full-auto",
            "smart-auto" or "smartauto" or "smart" => "smart-auto",
            "auto-edit" or "auto" => "auto-edit",
            _ => "suggest",
        };

        // 同步到 PermissionManager
        PermissionManager.SetMode(Level switch
        {
            "full-auto" => "yolo",
            "smart-auto" => "smartauto",
            "auto-edit" => "auto",
            _ => "ask",
        });
    }

    /// <summary>
    /// 重置为默认状态。
    /// </summary>
    public static void Reset()
    {
        Level = "suggest";
        AllowedDirectory = null;
    }
}
