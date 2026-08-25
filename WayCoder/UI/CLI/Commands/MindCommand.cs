using System.Text;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /mind —— 手动记忆管理：用户可强制记住 / 忘记 / 查找知识条目。
/// 与 /kb 的自动提炼互补：/kb mine 自动提取，/mind 手动干预。
///   /mind save <内容>      记住一条（带当前日期上下文）
///   /mind forget <内容>    忘记（删除）最匹配的一条
///   /mind search <内容>    查找相关记忆
///   /mind find <内容>      同 search
/// </summary>
public class MindCommand : SlashCommand
{
    public override string Name => "/mind";
    public override string Description => "手动记忆管理（save 记住 / update 更新 / forget 忘记 / search 查找）";
    public override string? Usage => "/mind <save|update|forget|search|find> <内容>";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var trimmed = args.Trim();
        if (trimmed.Length == 0)
        {
            screen.AddSystemMsg(
                "/mind 手动记忆管理\n" +
                "  /mind save [类别] <内容>   记住一条（自动带日期，类别: mistake/bugfix/habit/gap/code）\n" +
                "  /mind update <关键词> <新> 更新最匹配条目的内容\n" +
                "  /mind forget <内容>       忘记（删除）最匹配的一条\n" +
                "  /mind search <内容>       查找相关记忆\n" +
                "  /mind find <内容>         同 search");
            return Task.CompletedTask;
        }

        var parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var sub = parts[0].ToLowerInvariant();
        var content = parts.Length > 1 ? parts[1].Trim() : "";

        switch (sub)
        {
            case "save":
            {
                if (content.Length == 0) { screen.AddSystemMsg("用法: /mind save [类别] <内容>"); break; }
                // 可选类别前缀：/mind save code <片段>，未指定则自动识别
                var kind = "";
                var sp = content.IndexOf(' ');
                if (sp > 0)
                {
                    var first = content[..sp].ToLowerInvariant();
                    if (KbIndex.KbKinds.Contains(first)) { kind = first; content = content[(sp + 1)..].Trim(); }
                }
                var e = KbIndex.SaveManual(content, kind);
                screen.AddSystemMsg($"🧠 已记住「{e.Description}」〔{KbIndex.KindLabel(e.Kind)}〕\n{e.Content}");
                break;
            }
            case "update":
            {
                var sp = content.IndexOf(' ');
                if (sp <= 0) { screen.AddSystemMsg("用法: /mind update <关键词> <新内容>"); break; }
                var keyword = content[..sp].Trim();
                var newContent = content[(sp + 1)..].Trim();
                var updated = KbIndex.UpdateBestMatch(keyword, newContent);
                screen.AddSystemMsg(updated != null
                    ? $"📝 已更新「{updated.Description}」〔{KbIndex.KindLabel(updated.Kind)}〕"
                    : "🤷 未找到要更新的条目。");
                break;
            }
            case "forget":
            {
                if (content.Length == 0) { screen.AddSystemMsg("用法: /mind forget <内容>"); break; }
                var removed = KbIndex.DeleteBestMatch(content);
                screen.AddSystemMsg(removed != null
                    ? $"🗑️ 已忘记「{removed.Description}」"
                    : "🤷 未找到匹配的记忆。");
                break;
            }
            case "search":
            case "find":
            {
                if (content.Length == 0) { screen.AddSystemMsg("用法: /mind search <内容>"); break; }
                var hits = KbIndex.Search(content, 10);
                if (hits.Count == 0) { screen.AddSystemMsg("🔍 无匹配记忆。"); break; }
                var msg = new StringBuilder($"🔍 找到 {hits.Count} 条：\n");
                foreach (var (hit, score) in hits)
                    msg.AppendLine($"  · {hit.Description}〔{KbIndex.KindLabel(hit.Kind)}·相关度 {score:F2}〕");
                screen.AddSystemMsg(msg.ToString());
                break;
            }
            default:
                screen.AddSystemMsg("/mind <save|forget|search|find> <内容>");
                break;
        }
        return Task.CompletedTask;
    }
}
