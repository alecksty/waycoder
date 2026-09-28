using WayCoder.Tools;
using WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.TUI.Renderers;

/// <summary>
/// Read 文件工具渲染器 —— 文件路径 + 行数摘要。
///
/// <para>
/// ⚠ 错误前缀判据中英双认：生产者 <see cref="ToolErrors"/> 按界面语言出 `错误：` / `Error: `，
/// 只认中文的话英文界面下 read_file / glob / grep 的**错误输出不再标红**（不报错、只是看不出错）。
/// </para>
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

        if (rawOutput.StartsWith(ToolErrors.ZhPrefix, StringComparison.Ordinal)
            || rawOutput.StartsWith(ToolErrors.EnPrefix, StringComparison.Ordinal))
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
        if (rawOutput.StartsWith(ToolErrors.ZhPrefix, StringComparison.Ordinal)
            || rawOutput.StartsWith(ToolErrors.EnPrefix, StringComparison.Ordinal))
            return AnsiTty.ErrorBlock(rawOutput);
        return rawOutput;
    }
}
