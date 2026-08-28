using WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.TUI.Renderers;

/// <summary>
/// Bash 工具渲染器 —— 命令头 + 退出码标记 + 输出截断提示。
/// </summary>
public class BashToolRenderer : IToolRenderer
{
    public string ToolName => "bash";

    public string FormatHeader(string brief)
    {
        return $"💻 bash {brief}";
    }

    public string FormatOutput(string rawOutput)
    {
        if (string.IsNullOrEmpty(rawOutput)) return rawOutput;

        // 检测退出码并添加着色标记
        var result = rawOutput;

        // 给 [退出码：N] 着色：0=绿色，非0=红色
        var exitIdx = result.LastIndexOf("[退出码：");
        if (exitIdx >= 0)
        {
            var endIdx = result.IndexOf(']', exitIdx);
            if (endIdx >= 0)
            {
                var exitCodeStr = result[(exitIdx + 5)..endIdx];
                var isSuccess = exitCodeStr == "0";
                var color = isSuccess ? AnsiTty.Fg(32) : AnsiTty.FgBg(37, 41); // 绿或红底
                var before = result[..exitIdx];
                var after = result[(endIdx + 1)..];
                result = before + color + result[exitIdx..(endIdx + 1)] + AnsiTty.SgrReset + after;
            }
        }

        // [stderr] 标记红色
        result = result.Replace("[stderr]", $"{AnsiTty.FgBg(37, 41)}[stderr]{AnsiTty.SgrReset}");

        // 错误前缀着色
        if (result.StartsWith("错误：") || result.StartsWith("⚠ 已阻止"))
        {
            result = AnsiTty.ErrorBlock(result);
        }

        // 无输出提示
        if (result.Contains("（无输出）"))
        {
            result = result.Replace("（无输出）", $"{AnsiTty.SgrDim}（无输出）{AnsiTty.SgrReset}");
        }

        return result;
    }
}
