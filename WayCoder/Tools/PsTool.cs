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

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("name", JNode.Object()
                .Set("type", "string")
                .Set("description", "按进程名过滤（可选），如 'dotnet'、'node'、'python'"))
            .Set("top", JNode.Object()
                .Set("type", "integer")
                .Set("description", "只显示前 N 个结果（默认 30）")))
        .Set("required", JNode.Array());

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var name = arguments.GetValueOrDefault("name")?.ToString() ?? "";
        var top = ToolArgs.GetInt(arguments, "top", 30);

        return await Execute(name, top);
    }

    private static async Task<string> Execute(string name, int top)
    {
        // top 钳制到 [1,1000]：负值使 head -n -1 忽略上限、head -n 0 输出空；无上限则 top+1 溢出为负
        top = Math.Clamp(top, 1, 1000);
        // 命令注入防护：进程名白名单（复用 KillTool 的校验逻辑），杜绝 shell 元字符注入
        if (!string.IsNullOrEmpty(name) && !KillTool.IsSafeProcessName(name))
            return "错误：进程名包含非法字符（仅允许字母、数字、点、下划线、连字符、空格）。";

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
                    : $"ps aux | grep -iF '{name}' | head -n {top}";
                args = $"-c \"{cmd}\"";
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

            var r = await WayCoder.Infra.ProcUtil.RunAsync(psi, 10_000);
            if (r == null) return "错误：ps 命令超时（10s）";
            var (exitCode, result, errResult) = r.Value;
            if (string.IsNullOrWhiteSpace(result))
                result = errResult;
            if (string.IsNullOrWhiteSpace(result))
                return "（无进程匹配）";

            // 截断长输出
            if (result.Length > 8000)
                result = ContextManager.TruncateByRunes(result, 6000) + $"\n... (已截断，共 {result.Length} 字符) ...\n" + ContextManager.TruncateTailByRunes(result, 1000);

            return result.Trim();
        }
        catch (Exception ex)
        {
            return $"ps 错误：{ex.GetType().Name}: {ex.Message}";
        }
    }
}
