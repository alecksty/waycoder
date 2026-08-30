using System.Text;
using WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.TUI.Renderers;

/// <summary>
/// Edit 工具渲染器 —— diff 输出着色（红删绿增）。
/// 对标 Crush EditToolMessageItem 的 diff 渲染。
/// </summary>
public class EditToolRenderer : IToolRenderer
{
    public string ToolName => "edit_file";

    public string FormatHeader(string brief)
    {
        return $"✏️ edit {brief}";
    }

    public string FormatOutput(string rawOutput)
    {
        if (string.IsNullOrEmpty(rawOutput)) return rawOutput;

        // 错误情况直接返回
        if (rawOutput.StartsWith("错误：", StringComparison.Ordinal) || rawOutput.StartsWith("❌", StringComparison.Ordinal))
            return AnsiTty.ErrorBlock(rawOutput);

        // 取消编辑
        if (rawOutput.Contains("用户拒绝变更"))
            return AnsiTty.Warn(rawOutput); // 黄色

        // 解析并着色 diff
        var lines = rawOutput.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder();
        bool inDiff = false;
        bool hasSeenFirstLine = false;

        foreach (var line in lines)
        {
            if (!hasSeenFirstLine)
            {
                // 首行：结果摘要（如"已编辑 file.cs（3 处替换）"）
                sb.Append(AnsiTty.Sgr(32, 0, 1)); // 绿色粗体
                sb.Append(line);
                sb.Append(AnsiTty.SgrReset);
                sb.Append('\n');
                hasSeenFirstLine = true;
                continue;
            }

            if (!inDiff && line.StartsWith("--- ", StringComparison.Ordinal))
            {
                inDiff = true;
                sb.Append(AnsiTty.SgrDim);
                sb.Append(line);
                sb.Append(AnsiTty.SgrReset);
                sb.Append('\n');
                continue;
            }

            if (inDiff)
            {
                if (line.StartsWith("+++ ", StringComparison.Ordinal))
                {
                    sb.Append(AnsiTty.SgrDim);
                    sb.Append(line);
                    sb.Append(AnsiTty.SgrReset);
                }
                else if (line.StartsWith("@@", StringComparison.Ordinal))
                {
                    sb.Append(AnsiTty.Fg(36)); // 青色 hunk 头
                    sb.Append(line);
                    sb.Append(AnsiTty.SgrReset);
                }
                else if (line.StartsWith("-", StringComparison.Ordinal))
                {
                    sb.Append(AnsiTty.Fg(91)); // 亮红前景删除（去背景，避免刺眼）
                    sb.Append(line);
                    sb.Append(AnsiTty.SgrReset);
                }
                else if (line.StartsWith("+", StringComparison.Ordinal))
                {
                    sb.Append(AnsiTty.Fg(92)); // 亮绿前景新增（去背景，避免刺眼）
                    sb.Append(line);
                    sb.Append(AnsiTty.SgrReset);
                }
                else
                {
                    sb.Append(line);
                }
                sb.Append('\n');
            }
            else
            {
                sb.Append(line);
                sb.Append('\n');
            }
        }

        // 移除末尾多余换行
        var result = sb.ToString().TrimEnd('\n');
        return result;
    }
}
