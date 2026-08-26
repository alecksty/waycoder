using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WayCoder;

/// <summary>
/// 进程沙箱管理器 —— 边界轴：独立于确认轴（PermissionManager）限制「能碰什么」。
///
/// 边界模型 SandboxMode（与权限模式正交）：
///   Off          — 无边界（保持现状：只受敏感黑名单/系统目录硬约束）
///   ProjectWrite — 可写范围仅项目根（AllowedDirectory）；网络开
///   NetworkOff   — 可写任意（除敏感）；网络关（拦 fetch/web_search/curl 等）
///   Hard         — 仅项目根 + 网络关（最严，bash 进程沙箱化：环境净化/资源监控）
///
/// 对齐 Codex sandbox_mode / Claude 权限分层：边界（能碰什么）与确认（何时打断）正交，
/// 权限 Yolo 只跳过确认，不解除边界。
///
/// 沙箱限制：
///   1. 可写范围强制（write/edit/mv/cp/rm 越界拦截）
///   2. 网络开关（工具层 + bash 命令层拦截）
///   3. 环境变量清理（防止 API Key 泄露、网络访问，仅 Hard）
///   4. 工作目录锁定（cd 逃逸拦截）
///   5. 额外危险命令拦截（sudo、mount 等）
///   6. 进程内存监控（超 1GB 自动 kill，仅 Hard）
/// </summary>
public static class SandboxManager
{
    /// <summary>当前边界模式（权威字段；复用 Config.SandboxMode 枚举，独立于确认轴）。</summary>
    public static SandboxMode Mode { get; set; } = SandboxMode.Off;

    /// <summary>边界级别显示文本（兼容旧 /perm 输出）。</summary>
    public static string Level => Mode switch
    {
        SandboxMode.ProjectWrite => "project",
        SandboxMode.NetworkOff => "network-off",
        SandboxMode.Hard => "hard",
        _ => "off",
    };

    /// <summary>是否限制可写范围为项目根。</summary>
    public static bool IsProjectWrite => Mode is SandboxMode.ProjectWrite or SandboxMode.Hard;

    /// <summary>是否关闭网络。</summary>
    public static bool IsNetworkOff => Mode is SandboxMode.NetworkOff or SandboxMode.Hard;

    /// <summary>是否进程级沙箱化（环境净化/资源监控，Hard 时才需要）。</summary>
    public static bool IsSandboxed => Mode == SandboxMode.Hard;

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

    // ---- 需要网络的工具（network-off 时整体拦截） ----
    private static readonly HashSet<string> NetworkTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "fetch", "web_search", "download", "doc", "transcribe", "git", "git_pr",
    };

    // ---- 会写文件/移动文件的工具（project-write 时校验路径） ----
    private static readonly HashSet<string> WriteTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "write_file", "edit_file", "multiedit", "find_replace", "mv", "cp", "rm", "download",
    };

    /// <summary>
    /// 工具级边界门控（在 Agent.ExecuteToolAsync 唯一门控点调用）。
    /// 返回拦截原因，null 表示放行。独立于权限模式（Yolo 也生效）。
    /// </summary>
    public static string? CheckToolAllowed(string toolName, Dictionary<string, object?>? args)
    {
        if (IsNetworkOff && NetworkTools.Contains(toolName))
            return "⛔ 沙箱（网络已关闭）：此工具需要网络访问，已被边界阻止。";

        if (IsProjectWrite && WriteTools.Contains(toolName) && args != null)
        {
            foreach (var key in new[] { "file_path", "path", "source", "dest", "src" })
            {
                if (args.TryGetValue(key, out var v) && v is string s && s.Length > 0)
                {
                    var block = CheckWritable(s);
                    if (block != null) return block;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 路径级可写校验（文件工具内部防御纵深调用）。项目写边界下，路径须在 AllowedDirectory 内。
    /// </summary>
    public static string? CheckWritable(string path)
    {
        if (!IsProjectWrite || string.IsNullOrWhiteSpace(path) || AllowedDirectory == null) return null;

        string normalized;
        // 相对路径须基于被跟踪工作目录（CwdContext）解析，而非进程 cwd ——
        // 移动端 MAUI 上 cwd 锚到 App 私有目录（Global.Home），而文件工具的相对路径
        // 锚点是沙箱 workspace（CwdContext.Current），两者不同会导致相对路径写入被误判越界。
        try { normalized = PathSafety.ResolveSymlinks(Path.GetFullPath(path, Tools.CwdContext.Current.Value ?? Directory.GetCurrentDirectory())).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
        catch { return null; }

        var allowed = PathSafety.ResolveSymlinks(Path.GetFullPath(AllowedDirectory)).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!normalized.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
            return $"⛔ 沙箱（仅项目内写入）：路径在项目根外 — {path}";
        return null;
    }

    /// <summary>
    /// bash 网络关检查（BashTool.Execute 顶部调用，yolo 下也生效）。拦网络命令，localhost 例外。
    /// </summary>
    public static string? CheckNetworkCommand(string command)
    {
        if (!IsNetworkOff) return null;
        foreach (var (pattern, reason) in SandboxNetwork)
            if (pattern.IsMatch(command) && !NetworkAllowed.Any(na => na.IsMatch(command)))
                return $"⛔ 沙箱（网络已关闭）：{reason}";
        return null;
    }

    // ---- 沙箱额外危险命令（任何沙箱模式生效；比 BashTool.DangerousPatterns 更严格） ----
    private static readonly (Regex Pattern, string Reason)[] SandboxDanger =
    [
        (new(@"\bsudo\b"), "sudo 提权"),
        (new(@"\bsu\b(?=\s+-)"), "切换用户"),
        (new(@"\bchown\b"), "修改文件所有者"),
        (new(@"\bmount\b"), "挂载文件系统"),
        (new(@"\bumount\b"), "卸载文件系统"),
        (new(@"\biptables\b"), "修改防火墙"),
        (new(@"\bsystemctl\b"), "系统服务管理"),
    ];

    // ---- 网络命令（network-off 时拦截；localhost 例外） ----
    private static readonly (Regex Pattern, string Reason)[] SandboxNetwork =
    [
        (new(@"\bnc\b"), "网络连接（沙箱禁止）"),
        (new(@"\btelnet\b"), "网络连接（沙箱禁止）"),
        (new(@"\bssh\b(?=\s+\w+@)"), "SSH 远程连接"),
        (new(@"\bscp\b"), "SCP 远程传输"),
        (new(@"\bwget\b"), "网络下载（沙箱禁止）"),
        (new(@"\bcurl\b"), "网络请求（沙箱禁止）"),
    ];

    // 沙箱允许的网络相关命令（本地通信）
    private static readonly Regex[] NetworkAllowed =
    [
        new(@"\bcurl\s+localhost\b"),
        new(@"\bcurl\s+127\.0\.0\.1\b"),
        new(@"\bwget\s+localhost\b"),
    ];

    /// <summary>
    /// 检查命令是否违反沙箱边界。返回违规原因，null 表示通过。
    /// 仅 SandboxMode.Off 放行全部；危险命令任何沙箱模式拦，网络命令 network-off 拦，
    /// cd 逃逸 project-write 拦，系统目录写任何沙箱模式拦。
    /// </summary>
    public static string? CheckSandboxViolation(string command, string cwd)
    {
        if (Mode == SandboxMode.Off) return null;

        // 1. 额外危险命令（sudo/chown/mount/systemctl...）
        foreach (var (pattern, reason) in SandboxDanger)
            if (pattern.IsMatch(command)) return reason;

        // 2. 网络命令（network-off 才拦；localhost 例外）
        if (IsNetworkOff)
        {
            foreach (var (pattern, reason) in SandboxNetwork)
                if (pattern.IsMatch(command) && !NetworkAllowed.Any(na => na.IsMatch(command)))
                    return reason;
        }

        // 3. 工作目录逃逸（project-write）
        if (IsProjectWrite)
        {
            var violation = CheckDirectoryEscape(command, cwd);
            if (violation != null) return violation;
        }

        // 4. 文件写入系统目录
        return CheckSystemWrite(command);
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
            RedirectStandardInput = true, // 不共享主控台 stdin（BashTool 启动后置 EOF，防 TUI ReadKey 竞态）
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
                        ? target.Replace("~", Global.Home)
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
    /// 根据字符串设置边界模式（独立于确认轴；yolo/god 不再映射到沙箱）。
    /// 兼容旧 /perm 值：suggest→Off、auto-edit→ProjectWrite、full-auto→Hard。
    /// </summary>
    public static void SetLevel(string level)
    {
        var normalized = (level ?? "").Trim().ToLowerInvariant();
        Mode = normalized switch
        {
            "project" or "project-write" or "auto-edit" or "auto" => SandboxMode.ProjectWrite,
            "network-off" or "no-network" or "offline" => SandboxMode.NetworkOff,
            "hard" or "full-auto" => SandboxMode.Hard,
            _ => SandboxMode.Off, // "off" / "suggest" / 未知 → 无边界
        };
    }

    /// <summary>
    /// 重置为默认状态。
    /// </summary>
    public static void Reset()
    {
        Mode = SandboxMode.Off;
        AllowedDirectory = null;
    }
}
