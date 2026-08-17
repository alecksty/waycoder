namespace WayCoder.UI.Tui.ToolRenderers;

/// <summary>
/// 默认工具渲染器 —— 保持现有行为，纯文本直通。
/// </summary>
public class DefaultToolRenderer : IToolRenderer
{
    public string ToolName => "*";

    public string FormatHeader(string brief)
    {
        return $"⚙ {brief}";
    }

    public string FormatOutput(string rawOutput)
    {
        return rawOutput;
    }
}
