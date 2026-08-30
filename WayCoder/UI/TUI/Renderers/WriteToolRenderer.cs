using WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.TUI.Renderers;

/// <summary>
/// Write 工具渲染器 —— 文件创建摘要。
/// </summary>
public class WriteToolRenderer : IToolRenderer
{
    public string ToolName => "write_file";

    public string FormatHeader(string brief)
    {
        return $"📝 write {brief}";
    }

    public string FormatOutput(string rawOutput)
    {
        if (string.IsNullOrEmpty(rawOutput)) return rawOutput;

        if (rawOutput.StartsWith("错误：", StringComparison.Ordinal) || rawOutput.StartsWith("❌", StringComparison.Ordinal))
            return AnsiTty.ErrorBlock(rawOutput);

        if (rawOutput.Contains("用户拒绝变更"))
            return AnsiTty.Warn(rawOutput);

        // 成功消息绿色
        if (rawOutput.StartsWith("已写入", StringComparison.Ordinal))
            return AnsiTty.Fg(32) + rawOutput + AnsiTty.SgrReset;

        return rawOutput;
    }
}
