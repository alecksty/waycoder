using WayCoder.Tools;
using WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.TUI.Renderers;

/// <summary>
/// Write 工具渲染器 —— 文件创建摘要。
///
/// <para>
/// ⚠ 本文件里的三处判定都是「**拿工具输出文案当判据**」（公理 A2 的高危形态）——
/// 生产者 <see cref="WriteFileTool"/> 与 <see cref="ToolErrors"/> 已按界面语言出文案，
/// 判据若只认中文，英文界面下就**静默退化**：错误块不上色、拒绝变更不走警告样式、
/// 写入成功不是绿的，一个错都不报。所以每条判据都必须中英双认。
/// </para>
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

        // 前缀取自 ToolErrors 的常量（生产者同源）——别再手写 "错误：" 字面量
        if (rawOutput.StartsWith(ToolErrors.ZhPrefix, StringComparison.Ordinal)
            || rawOutput.StartsWith(ToolErrors.EnPrefix, StringComparison.Ordinal)
            || rawOutput.StartsWith("❌", StringComparison.Ordinal))
            return AnsiTty.ErrorBlock(rawOutput);

        // 「用户拒绝变更」：生产者 WriteFileTool.cs / MultiEditTool.cs 中英双支，
        // 英文支是 "（declined by user）"（括号随语言变，故两串不共前缀）
        if (rawOutput.Contains("用户拒绝变更", StringComparison.Ordinal)
            || rawOutput.Contains("(declined by user)", StringComparison.Ordinal))
            return AnsiTty.Warn(rawOutput);

        // 成功消息绿色。生产者 WriteFileTool.cs:109 的英文支是 "Wrote …" / "Appended …"。
        // ⚠ 只补 `已写入` 的英文孪生 `Wrote `，**不顺手把 `已追加` 也加进来** ——
        //   追加形态在中文下本来就不上色，单给英文上色会让两侧观感不一致（这不是修 A2，是改行为）。
        if (rawOutput.StartsWith("已写入", StringComparison.Ordinal)
            || rawOutput.StartsWith("Wrote ", StringComparison.Ordinal))
            return AnsiTty.Fg(32) + rawOutput + AnsiTty.SgrReset;

        return rawOutput;
    }
}
