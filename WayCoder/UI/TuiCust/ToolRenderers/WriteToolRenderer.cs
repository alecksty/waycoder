using WayCoder.Terminal;

namespace WayCoder.UI.ToolRenderers;

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

        if (rawOutput.StartsWith("错误：") || rawOutput.StartsWith("❌"))
            return AnsiTty.FgBg(37, 41) + rawOutput + AnsiTty.SgrReset;

        if (rawOutput.Contains("用户拒绝变更"))
            return AnsiTty.Fg(33) + rawOutput + AnsiTty.SgrReset;

        // 成功消息绿色
        if (rawOutput.StartsWith("已写入"))
            return AnsiTty.Fg(32) + rawOutput + AnsiTty.SgrReset;

        return rawOutput;
    }
}
