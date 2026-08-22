using System.Diagnostics;

namespace WayCoder.Tools;

/// <summary>
/// 进程终止工具 —— 按 PID 或进程名终止进程。
/// Windows: taskkill，Unix: kill / pkill。
/// 禁止终止系统关键进程。
/// </summary>
public class KillTool : ITool
{
    public string Name => "kill";
    public string Description => "终止指定进程。通过 PID 或进程名（如 'node'、'dotnet'）。禁止终止系统关键进程。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("pid", JNode.Object()
                .Set("type", "integer")
                .Set("description", "要终止的进程 PID"))
            .Set("name", JNode.Object()
                .Set("type", "string")
                .Set("description", "要终止的进程名（如 'node'、'python'）"))
            .Set("force", JNode.Object()
                .Set("type", "boolean")
                .Set("description", "强制终止（默认 false，先尝试优雅终止）")))
        .Set("required", JNode.Array());

    // 禁止终止的关键系统进程
    private static readonly HashSet<string> ProtectedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "System", "smss", "csrss", "wininit", "services", "lsass",
        "svchost", "winlogon", "Idle", "Registry", "System Idle Process",
        "kernel32", "ntoskrnl", "PID 0", "PID 4",
    };

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var hasPid = arguments.ContainsKey("pid");
        var hasName = arguments.ContainsKey("name");
        // Convert.ToInt32 对超 int 范围 long 抛 OverflowException、对 double 银行家舍入、对非数字字符串抛 FormatException；
        // 改走 ToolArgs.GetInt 统一钳制（超范围钳制而非抛异常/舍入）。
        var pid = ToolArgs.GetInt(arguments, "pid", 0);
        var name = arguments.GetValueOrDefault("name")?.ToString() ?? "";
        var force = arguments.TryGetValue("force", out var f) && f is bool fb && fb;

        return await Execute(hasPid, pid, hasName, name, force);
    }

    private static async Task<string> Execute(bool hasPid, int pid, bool hasName, string name, bool force)
    {
        // 系统关键 PID 检查（优先于参数缺失检查）
        if (hasPid && pid <= 0)
            return "错误：PID 必须为正整数（负值会使 Unix 分支误走 pkill '' 杀掉全部用户进程）。";
        if (hasPid && pid == 4)
            return "⚠ 已阻止：PID 4 是系统关键进程，不可终止。";

        if (!hasPid && !hasName)
            return "错误：必须指定 pid 或 name 参数。";

        if (hasName && string.IsNullOrWhiteSpace(name))
            return "错误：进程名不能为空。";

        if (!string.IsNullOrEmpty(name) && ProtectedNames.Contains(name))
            return $"⚠ 已阻止：'{name}' 是系统关键进程，不可终止。";

        // 命令注入防护：进程名白名单（仅字母数字/点/下划线/连字符/空格），杜绝 shell 元字符注入
        if (hasName && !IsSafeProcessName(name))
            return "错误：进程名包含非法字符（仅允许字母、数字、点、下划线、连字符、空格）。";

        try
        {
            string fileName, args;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fileName = "cmd.exe";
                var forceFlag = force ? " /F" : "";
                if (pid > 0)
                    args = $"/c \"taskkill{forceFlag} /PID {pid} 2>&1\"";
                else
                    args = $"/c \"taskkill{forceFlag} /IM \\\"{name}.exe\\\" 2>&1\"";
            }
            else
            {
                fileName = "/bin/bash";
                if (pid > 0)
                {
                    var sig = force ? "-9 " : "";
                    args = $"-c \"kill {sig}{pid} 2>&1\"";
                }
                else
                {
                    var sig = force ? "-9 " : "";
                    // name 已过 IsSafeProcessName 白名单（不含单引号），单引号包裹可安全保留空格等字面量
                    args = $"-c \"pkill {sig}'{name}' 2>&1\"";
                }
            }

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true, // 不共享主控台 stdin（ProcUtil 启动后置 EOF，防 TUI ReadKey 竞态）
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            var r = await WayCoder.Infra.ProcUtil.RunAsync(psi, Config.Instance.KillTimeoutSec * 1000);
            if (r == null)
            {
                ErrorLog.ToolError("kill", $"进程终止超时（{Config.Instance.KillTimeoutSec}s）");
                return $"错误：kill 命令超时（{Config.Instance.KillTimeoutSec}s）";
            }
            var (exitCode, result, err) = r.Value;

            if (!string.IsNullOrEmpty(err))
                result += $"\n[stderr]\n{err}";

            var target = pid > 0 ? $"PID {pid}" : name;
            if (exitCode != 0)
                result += $"\n[退出码：{exitCode}] 终止 {target} 可能失败";

            return string.IsNullOrWhiteSpace(result)
                ? $"✔ 已终止 {target}"
                : result.Trim();
        }
        catch (Exception ex)
        {
            return $"kill 错误：{ex.GetType().Name}: {ex.Message}";
        }
    }

    /// <summary>
    /// 进程名安全白名单：仅允许字母、数字、点、下划线、连字符、空格。
    /// 用于 kill/ps 等按进程名拼接 shell 命令的场景，从根上杜绝命令注入（拒绝 `;` `|` `$` 反引号等元字符）。
    /// </summary>
    public static bool IsSafeProcessName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        foreach (var c in name)
        {
            if (!(char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-' || c == ' '))
                return false;
        }
        return true;
    }
}
