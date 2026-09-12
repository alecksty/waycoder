using System.Diagnostics;

namespace WayCoder.Tools;

/// <summary>
/// 进程列表工具 —— 列出正在运行的进程。
/// Windows: tasklist，Unix: ps aux。
/// </summary>
public class PsTool : ITool
{
    /// <summary>tasklist/ps 的原始输出 → 命令行文本渲染</summary>
    public bool RawOutput => true;

    public string Name => "ps";
    public string Description => "列出当前正在运行的进程。可传 name 过滤进程名。返回 PID、进程名、内存占用。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("name", JNode.Param("string", "按进程名过滤（可选），如 'dotnet'、'node'、'python'"))
            .Set("top", JNode.Param("integer", "只显示前 N 个结果（默认 30）")))
        .Set("required", JNode.Array());

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var name = arguments.GetValueOrDefault("name")?.ToString() ?? "";
        var top = ToolArgs.GetInt(arguments, "top", 30);

        return await Execute(name, top);
    }

    /// <summary>
    /// 构造子进程 psi（internal 供自测断言解码设置）。
    ///
    /// **Windows 走 `cmd.exe /c tasklist`，必须过 `ProcEncoding.Apply`**：cmd.exe 及其子命令向
    /// 重定向管道写的是**系统 OEM 代码页**字节（中文系统 GBK）。实测本机 `tasklist /NH` 输出里
    /// 有 350 字节非 ASCII（如「微信开发者工具.exe」），按 UTF-8 解码在偏移 27928 处即失败 ⇒
    /// 智能体看到的中文进程名全是乱码，照着拼进 `kill` 必然失败。这是 CLAUDE.md 的既有铁律，
    /// 本工具此前漏了。（非 Windows 走 /bin/bash，本就 UTF-8，`Apply` 自动跳过。）
    /// </summary>
    internal static ProcessStartInfo BuildPsi(string fileName, string args)
        // 实现已收敛到 ProcUtil（重定向 + 主控台 stdin 隔离 + Windows OEM 解码），
        // 此前 KillTool/PsTool 各一份逐字相同的实现，连 Apply 的修复都各做了一遍。
        => WayCoder.Infra.ProcUtil.BuildPsi(fileName, args);

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

            var psi = BuildPsi(fileName, args);

            var r = await WayCoder.Infra.ProcUtil.RunAsync(psi, 10_000);
            if (r == null) return "错误：ps 命令超时（10s）";
            var (exitCode, result, errResult) = r.Value;
            if (string.IsNullOrWhiteSpace(result))
                result = errResult;
            if (string.IsNullOrWhiteSpace(result))
                return "（无进程匹配）";

            // 截断长输出
            if (result.Length > 8000)
                result = ContextManager.TruncateKeepHeadTail(result, 6000, 1000, $"\n... (已截断，共 {result.Length} 字符) ...\n");

            return result.Trim();
        }
        catch (Exception ex)
        {
            return ToolErrors.Error("ps ", ex);
        }
    }
}
