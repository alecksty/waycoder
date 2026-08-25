using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /kb —— 自主学习编程知识库：把工作痕迹提炼成四类经验（mistake/bugfix/habit/gap），
/// 全局保存，间隔重复自测强化记忆，薄弱点统计指导学习方向。
///   /kb mine [N]   从 git 历史提炼经验条目（默认 20）
///   /kb review     间隔重复自测一条到期经验
///   /kb weak       欠缺知识清单 + 薄弱点统计
///   /kb list       列出全部经验条目
/// </summary>
public class KbCommand : SlashCommand
{
    public override string Name => "/kb";
    public override string Description => "编程知识库（mine 提炼经验 / review 间隔重复自测 / weak 薄弱点统计）";
    public override string? Usage => "/kb [mine [N] | review | weak | list]";

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        var trimmed = args.Trim();
        if (trimmed.Length == 0) { ShowHelp(screen); return; }

        var parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var first = parts[0].ToLowerInvariant();
        var rest = parts.Length > 1 ? parts[1].Trim() : "";

        switch (first)
        {
            case "mine":
                await Mine(screen, rest);
                break;
            case "review":
                Review(screen);
                break;
            case "weak":
                Weak(screen);
                break;
            case "list":
                List(screen);
                break;
            case "help":
            default:
                ShowHelp(screen);
                break;
        }
    }

    static void ShowHelp(ChatScreen screen)
        => screen.AddSystemMsg(
            "/kb 编程知识库\n" +
            "  /kb mine [N]    从 git 历史提炼经验（默认 20）\n" +
            "  /kb review      间隔重复自测一条到期经验\n" +
            "  /kb weak        欠缺知识清单 + 薄弱点统计\n" +
            "  /kb list        列出全部经验条目");

    static async Task Mine(ChatScreen screen, string arg)
    {
        screen.AddSystemMsg("⛏️ 正在从 git 历史提炼经验（可能需要一点时间）…");
        int count = 20;
        if (int.TryParse(arg, out var n) && n > 0) count = n;

        var (mined, errors) = await KbIndex.MineAsync(count);
        var msg = $"✅ /kb mine 完成：新增 {mined} 条经验（扫描最近 {count} 个提交）\n" +
                  $"📁 保存目录：{KbIndex.Dir}";
        if (errors.Count > 0)
            msg += "\n\n⚠️ 跳过：\n" + string.Join("\n", errors.Take(5));
        screen.AddSystemMsg(msg);
    }

    static void Review(ChatScreen screen)
    {
        var entry = KbIndex.PickNextDue(KbIndex.ListEntries());
        if (entry == null)
        {
            screen.AddSystemMsg("🎉 没有到期待复习的经验。`/kb mine` 先提炼一批，或用 `/kb list` 查看现有条目。");
            return;
        }

        var question = KbIndex.QuizQuestion(entry);
        screen.AddSystemMsg($"🔁 复习「{entry.Description}」〔{KbIndex.KindLabel(entry.Kind)}〕\n\n{question}");

        var recall = UxHelper.Select("你自己会怎么处理？", ["我记得 / 能复述", "想不起来，看答案"]);
        bool knew = recall == "我记得 / 能复述";

        screen.AddSystemMsg($"📚 答案：\n\n{KbIndex.QuizAnswer(entry)}");

        var confirm = UxHelper.Select(knew ? "对照答案，你掌握了吗？" : "看过答案，这次掌握了吗？", ["掌握", "还没掌握"]);
        bool mastered = confirm == "掌握";

        KbIndex.MarkReview(entry.Name, mastered, entry.Kind, entry.Tags);
        screen.AddSystemMsg(mastered
            ? $"✅ 已记录掌握，复习间隔 +{KbIndex.LoadReviewState().FirstOrDefault(i => i.Name == entry.Name)?.IntervalDays ?? 1} 天。"
            : "📌 已记录未掌握，间隔重置 1 天，相关欠缺知识权重提升。");
    }

    static void Weak(ChatScreen screen)
    {
        var report = KbIndex.WeakStats();
        var msg = new System.Text.StringBuilder("🧭 薄弱点统计\n");

        msg.AppendLine("\n── 欠缺知识清单 ──");
        if (report.Gaps.Count == 0)
            msg.AppendLine("（暂无，/kb mine 提炼或复习未掌握时自动沉淀）");
        else
            foreach (var g in report.Gaps)
                msg.AppendLine($"  · {g.Description}（权重 {g.Weight:F1}）");

        msg.AppendLine("\n── 薄弱标签（mistake/bugfix 聚合）──");
        if (report.WeakTags.Count == 0)
            msg.AppendLine("（暂无）");
        else
            foreach (var t in report.WeakTags)
                msg.AppendLine($"  · {t.Tag} ×{t.Count}");

        msg.AppendLine("\n── ErrorLog 错误信号 ──");
        if (report.ErrorSignals.Count == 0)
            msg.AppendLine("（暂无）");
        else
            foreach (var s in report.ErrorSignals)
                msg.AppendLine($"  · {s.Source} ×{s.Count}");

        screen.AddSystemMsg(msg.ToString());
    }

    static void List(ChatScreen screen)
    {
        var entries = KbIndex.ListEntries();
        if (entries.Count == 0)
        {
            screen.AddSystemMsg("📭 知识库为空。`/kb mine` 从 git 历史提炼第一批经验。");
            return;
        }

        var msg = new System.Text.StringBuilder($"📚 知识库共 {entries.Count} 条：\n");
        foreach (var e in entries)
            msg.AppendLine($"  [{KbIndex.KindLabel(e.Kind)}] {e.Description}（{e.Name}）");
        screen.AddSystemMsg(msg.ToString());
    }
}
