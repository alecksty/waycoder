using System.Text;

namespace WayCoder.UI.Cli.Commands;

/// <summary>命令层共用的文本处理 —— 避免多个斜杠命令各写一份相同实现。</summary>
internal static class CommandTextHelpers
{
    /// <summary>
    /// 把会话消息转录成 Markdown（`## role` + 正文），单条超长按**码点**截断（防 emoji/CJK 代理对被拆半）。
    ///
    /// `/kb retro`（把会话沉淀成经验）与 `/teach`（教学模式）此前各写一份**逐字相同**的实现，
    /// 只有截断阈值不同（2000 / 1500，且没有注释说明为什么不同）。阈值参数化以保留各自取值，
    /// 其余逻辑（跳过空正文、按 Rune 截断）收敛到这一份。
    /// </summary>
    public static string BuildTranscript(List<JNode> messages, int maxCharsPerMessage)
    {
        var sb = new StringBuilder();
        foreach (var m in messages)
        {
            var role = m["role"]?.AsString() ?? "?";
            var content = m["content"]?.AsString() ?? "";
            if (content.Length == 0) continue;
            if (content.Length > maxCharsPerMessage)
                content = ContextManager.TruncateByRunes(content, maxCharsPerMessage);
            sb.AppendLine($"## {role}");
            sb.AppendLine(content);
        }
        return sb.ToString();
    }
}
