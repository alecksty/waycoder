using WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.Tui.ToolRenderers;

/// <summary>
/// Agent 工具渲染器 —— 子智能体状态 + 深度标记。
/// </summary>
public class AgentToolRenderer : IToolRenderer
{
    public string ToolName => "agent";

    public string FormatHeader(string brief)
    {
        return $"🤖 agent {brief}";
    }

    public string FormatOutput(string rawOutput)
    {
        if (string.IsNullOrEmpty(rawOutput)) return rawOutput;

        // 子智能体完成标记着色
        var result = rawOutput;

        // [子智能体已完成 · 深度 N] → 蓝色粗体
        if (result.StartsWith("[子智能体已完成"))
        {
            var endBracket = result.IndexOf(']');
            if (endBracket >= 0)
            {
                var header = result[..(endBracket + 1)];
                var rest = result[(endBracket + 1)..];
                result = AnsiTty.Sgr(36, 0, 1) + header + AnsiTty.SgrReset + rest;
            }
        }

        // [并行子智能体完成 · N 个任务] → 蓝色粗体
        if (result.StartsWith("[并行子智能体完成"))
        {
            var endBracket = result.IndexOf(']');
            if (endBracket >= 0)
            {
                var header = result[..(endBracket + 1)];
                var rest = result[(endBracket + 1)..];
                result = AnsiTty.Sgr(36, 0, 1) + header + AnsiTty.SgrReset + rest;
            }
        }

        // 错误着色
        if (result.StartsWith("子智能体错误") || result.StartsWith("并行子智能体错误"))
        {
            result = AnsiTty.ErrorBlock(result);
        }

        // --- 子任务 N --- 分隔线着色
        result = System.Text.RegularExpressions.Regex.Replace(
            result, @"^--- .+ ---$",
            m => AnsiTty.Warn(m.Value),
            System.Text.RegularExpressions.RegexOptions.Multiline);

        return result;
    }
}
