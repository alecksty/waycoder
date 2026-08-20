using WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.Tui.ToolRenderers;

/// <summary>
/// Read 文件工具渲染器 —— 文件路径 + 行数摘要。
/// </summary>
public class ReadFileToolRenderer : IToolRenderer
{
    public string ToolName => "read_file";

    public string FormatHeader(string brief)
    {
        return $"📖 read {brief}";
    }

    public string FormatOutput(string rawOutput)
    {
        if (string.IsNullOrEmpty(rawOutput)) return rawOutput;

        if (rawOutput.StartsWith("错误："))
            return AnsiTty.ErrorBlock(rawOutput);

        return rawOutput;
    }
}

/// <summary>
/// Glob/Grep 工具渲染器 —— 搜索结果摘要。
/// </summary>
public class GlobGrepToolRenderer : IToolRenderer
{
    public string ToolName => "glob_grep"; // 通过 Register 分别注册

    public string FormatHeader(string brief)
    {
        return $"🔍 {brief}";
    }

    public string FormatOutput(string rawOutput)
    {
        if (string.IsNullOrEmpty(rawOutput)) return rawOutput;
        if (rawOutput.StartsWith("错误："))
            return AnsiTty.ErrorBlock(rawOutput);
        return rawOutput;
    }
}
