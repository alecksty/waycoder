using System.Diagnostics;

namespace WayCoder.Tools;

/// <summary>
/// 进程列表工具 —— 列出正在运行的进程。
/// Windows: tasklist，Unix: ps aux。
/// </summary>
public class PsTool : ITool
{
    public string Name => "ps";
    public string Description => "列出当前正在运行的进程。可传 name 过滤进程名。返回 PID、进程名、内存占用。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["name"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "按进程名过滤（可选），如 'dotnet'、'node'、'python'",
            },
            ["top"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "只显示前 N 个结果（默认 30）",
            },
        },
        ["required"] = new JsonArray(),
    };

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var name = arguments.GetValueOrDefault("name")?.ToString() ?? "";
        var top = arguments.TryGetValue("top", out var t) && t is int ti ? ti : 30;

        return Task.FromResult(Execute(name, top));
    }

    private static string Execute(string name, int top)
    {
        try
        {
            string fileName, args;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fileName = "cmd.exe";
                var filter = string.IsNullOrEmpty(name)
                    ? ""
                    : $" /FI \"IMAGENAME eq {name}.exe\"";
                args = $"/c \"tasklist /NH{filter} 2>&1\"";
            }
            else
            {
                fileName = "/bin/bash";
                var cmd = string.IsNullOrEmpty(name)
                    ? $"ps aux --sort=-%mem | head -n {top + 1}"
                    : $"ps aux | grep -i {EscapeBash(name)} | head -n {top}";
                args = $"-c \"{cmd}\"";
            }

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi)!;
            var stdout = new System.Text.StringBuilder();
            var stderr = new System.Text.StringBuilder();
            proc.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
            proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            if (!proc.WaitForExit(10_000))
            {
                proc.Kill(entireProcessTree: true);
                return "错误：ps 命令超时（10s）";
            }
            proc.WaitForExit();

            var result = stdout.ToString();
            if (string.IsNullOrWhiteSpace(result))
                result = stderr.ToString();
            if (string.IsNullOrWhiteSpace(result))
                return "（无进程匹配）";

            // 截断长输出
            if (result.Length > 8000)
                result = result[..6000] + $"\n... (已截断，共 {result.Length} 字符) ...\n" + result[^1000..];

            return result.Trim();
        }
        catch (Exception ex)
        {
            return $"ps 错误：{ex.GetType().Name}: {ex.Message}";
        }
    }

    private static string EscapeBash(string s) => s.Replace("'", "'\\''");
}
