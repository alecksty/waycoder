using WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.TUI.Renderers;

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

        // [子智能体已完成 · 深度 N] / [Sub-agent finished · depth N] → 蓝色粗体
        // ⚠ 中英双认：生产者 Tools/AgentTool.cs 按界面语言出文案。只认中文的话，
        //   英文会话里这些标记行**不再上色**（不报错、只是观感退化）。
        if (result.StartsWith("[子智能体已完成", StringComparison.Ordinal)
            || result.StartsWith("[Sub-agent finished", StringComparison.Ordinal)
            || result.StartsWith("[子智能体流水线完成", StringComparison.Ordinal)
            || result.StartsWith("[Sub-agent pipeline finished", StringComparison.Ordinal))
        {
            var endBracket = result.IndexOf(']');
            if (endBracket >= 0)
            {
                var header = result[..(endBracket + 1)];
                var rest = result[(endBracket + 1)..];
                result = AnsiTty.Sgr(36, 0, 1) + header + AnsiTty.SgrReset + rest;
            }
        }

        // [并行子智能体完成 · N 个任务] / [Parallel sub-agents finished · N task(s)] → 蓝色粗体
        if (result.StartsWith("[并行子智能体完成", StringComparison.Ordinal)
            || result.StartsWith("[Parallel sub-agents finished", StringComparison.Ordinal))
        {
            var endBracket = result.IndexOf(']');
            if (endBracket >= 0)
            {
                var header = result[..(endBracket + 1)];
                var rest = result[(endBracket + 1)..];
                result = AnsiTty.Sgr(36, 0, 1) + header + AnsiTty.SgrReset + rest;
            }
        }

        // 错误着色（单任务失败与两种编排失败都要红块；中英双认）
        if (result.StartsWith("子智能体错误", StringComparison.Ordinal)
            || result.StartsWith("Sub-agent error", StringComparison.Ordinal)
            || result.StartsWith("并行子智能体错误", StringComparison.Ordinal)
            || result.StartsWith("Parallel sub-agent error", StringComparison.Ordinal)
            || result.StartsWith("依赖编排子智能体错误", StringComparison.Ordinal)
            || result.StartsWith("Dependency-orchestration sub-agent error", StringComparison.Ordinal))
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
