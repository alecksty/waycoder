using System.Text;
using WayCoder.Infra;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /kb —— 自主学习编程知识库（/mind 为别名）：
/// 自动提炼（mine）+ 手动记忆（save/update/forget/search）+ 间隔重复自测（review）+ 薄弱点统计（weak）。
/// 条目全局保存（~/.waycoder/kb/），支持文字 / 代码片段 / Markdown / 链接。
///   /kb mine [N]        从 git 历史提炼经验（默认 20）
///   /kb save [类别] <内容>  手动记住一条（自动带日期，类别自动识别/显式指定）
///   /kb update <关键词> <新> 更新最匹配条目
///   /kb forget <内容>    忘记（删除）最匹配条目
///   /kb search <内容>    查找（/kb find 同义）
///   /kb review           间隔重复自测一条到期经验
///   /kb weak             欠缺知识清单 + 薄弱点统计
///   /kb list             列出全部条目
/// </summary>
public class KbCommand : SlashCommand
{
    public override string Name => "/kb";
    public override string[] Aliases => ["/mind"];
    public override string Description => L.Pick("编程知识库（mine 提炼 / save 记住 / diagnose 诊断 / path 学习路径 / profile 画像 / retro 复盘 / review 自测 / weak 统计）", "Programming knowledge base (mine / save / diagnose / path / profile / retro / review / weak)");
    public override string? Usage => "/kb <mine [N]|save|update|forget|search|find|diagnose|path|profile [json]|retro|review|weak|list>";

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
            case "save":
                Save(screen, rest);
                break;
            case "update":
                Update(screen, rest);
                break;
            case "forget":
                Forget(screen, rest);
                break;
            case "search":
            case "find":
                Search(screen, rest);
                break;
            case "diagnose":
                await Diagnose(screen, rest);
                break;
            case "path":
                await Path(screen);
                break;
            case "profile":
                Profile(screen, rest);
                break;
            case "retro":
                await Retro(screen);
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
            L.Pick("/kb 编程知识库（/mind 同义）\n", "/kb programming knowledge base (alias /mind)\n") +
            L.Pick("  /kb mine [N]         从 git 历史提炼经验（默认 20）\n", "  /kb mine [N]         mine lessons from the git history (default 20)\n") +
            L.Pick("  /kb save [类别] <内容>  记住一条（自动带日期；类别: mistake/bugfix/habit/gap/code）\n", "  /kb save [category] <content>  remember one entry (auto-dated; categories: mistake/bugfix/habit/gap/code)\n") +
            L.Pick("  /kb update <关键词> <新> 更新最匹配条目的内容\n", "  /kb update <keyword> <new> update the best-matching entry\n") +
            L.Pick("  /kb forget <内容>     忘记（删除）最匹配条目\n", "  /kb forget <content>    forget (delete) the best-matching entry\n") +
            L.Pick("  /kb search <内容>     查找相关条目\n", "  /kb search <content>    find related entries\n") +
            L.Pick("  /kb diagnose <报错>   诊断报错（召回知识库 + git 修复史）\n", "  /kb diagnose <error>   diagnose an error (recalls the knowledge base + git fix history)\n") +
            L.Pick("  /kb path              生成学习路径（欠缺→进阶，接入 /kb review）\n", "  /kb path              build a learning path (gaps → advanced, wired into /kb review)\n") +
            L.Pick("  /kb profile [json]    技能画像（json 导出供可视化）\n", "  /kb profile [json]    skill profile (export json for visualization)\n") +
            L.Pick("  /kb retro             复盘本次会话，提炼经验入知识库\n", "  /kb retro             retrospect this session and file the lessons into the knowledge base\n") +
            L.Pick("  /kb review            间隔重复自测一条到期经验\n", "  /kb review            spaced-repetition quiz on one due entry\n") +
            L.Pick("  /kb weak              欠缺知识清单 + 薄弱点统计\n", "  /kb weak              gap list + weak-point statistics\n") +
            L.Pick("  /kb list              列出全部条目", "  /kb list              list all entries"));

    static async Task Mine(ChatScreen screen, string arg)
    {
        screen.AddSystemMsg(L.Pick("⛏️ 正在从 git 历史提炼经验（可能需要一点时间）…", "⛏️ Mining lessons from the git history (this may take a while)…"));
        int count = 20;
        if (int.TryParse(arg, out var n) && n > 0) count = n;

        var (mined, errors) = await KbIndex.MineAsync(count);
        var msg = L.Pick($"✅ /kb mine 完成：新增 {mined} 条经验（扫描最近 {count} 个提交）\n", $"✅ /kb mine done: {mined} new lessons (scanned the last {count} commits)\n") +
                  L.Pick($"📁 保存目录：{KbIndex.Dir}", $"📁 Saved to: {KbIndex.Dir}");
        if (errors.Count > 0)
            msg += L.Pick("\n\n⚠️ 跳过：\n", "\n\n⚠️ Skipped:\n") + string.Join("\n", errors.Take(5));
        screen.AddSystemMsg(msg);
    }

    static void Save(ChatScreen screen, string content)
    {
        if (content.Length == 0) { screen.AddSystemMsg(L.Pick("用法: /kb save [类别] <内容>", "Usage: /kb save [category] <content>")); return; }
        var kind = "";
        var sp = content.IndexOf(' ');
        if (sp > 0)
        {
            var first = content[..sp].ToLowerInvariant();
            if (KbIndex.KbKinds.Contains(first)) { kind = first; content = content[(sp + 1)..].Trim(); }
        }
        var e = KbIndex.SaveManual(content, kind);
        screen.AddSystemMsg(L.Pick($"🧠 已记住「{e.Description}」〔{KbIndex.KindLabel(e.Kind)}〕\n{e.Content}", $"🧠 Remembered \"{e.Description}\" [{KbIndex.KindLabel(e.Kind)}]\n{e.Content}"));
    }

    static void Update(ChatScreen screen, string content)
    {
        var sp = content.IndexOf(' ');
        if (sp <= 0) { screen.AddSystemMsg(L.Pick("用法: /kb update <关键词> <新内容>", "Usage: /kb update <keyword> <new content>")); return; }
        var keyword = content[..sp].Trim();
        var newContent = content[(sp + 1)..].Trim();
        var updated = KbIndex.UpdateBestMatch(keyword, newContent);
        screen.AddSystemMsg(updated != null
            ? L.Pick($"📝 已更新「{updated.Description}」〔{KbIndex.KindLabel(updated.Kind)}〕", $"📝 Updated \"{updated.Description}\" [{KbIndex.KindLabel(updated.Kind)}]")
            : L.Pick("🤷 未找到要更新的条目。", "🤷 No entry to update was found."));
    }

    static void Forget(ChatScreen screen, string content)
    {
        if (content.Length == 0) { screen.AddSystemMsg(L.Pick("用法: /kb forget <内容>", "Usage: /kb forget <content>")); return; }
        var removed = KbIndex.DeleteBestMatch(content);
        screen.AddSystemMsg(removed != null
            ? L.Pick($"🗑️ 已忘记「{removed.Description}」", $"🗑️ Forgot \"{removed.Description}\"")
            : L.Pick("🤷 未找到匹配的记忆。", "🤷 No matching memory found."));
    }

    static void Search(ChatScreen screen, string content)
    {
        if (content.Length == 0) { screen.AddSystemMsg(L.Pick("用法: /kb search <内容>", "Usage: /kb search <content>")); return; }
        var hits = KbIndex.Search(content, 10);
        if (hits.Count == 0) { screen.AddSystemMsg(L.Pick("🔍 无匹配条目。", "🔍 No matching entries.")); return; }
        var msg = new StringBuilder(L.Pick($"🔍 找到 {hits.Count} 条：\n", $"🔍 {hits.Count} matches:\n"));
        foreach (var (hit, score) in hits)
            msg.AppendLine(L.Pick($"  · {hit.Description}〔{KbIndex.KindLabel(hit.Kind)}·相关度 {score:F2}〕", $"  · {hit.Description} [{KbIndex.KindLabel(hit.Kind)} · relevance {score:F2}]"));
        screen.AddSystemMsg(msg.ToString());
    }

    static async Task Diagnose(ChatScreen screen, string content)
    {
        if (content.Length == 0) { screen.AddSystemMsg(L.Pick("用法: /kb diagnose <报错文本>", "Usage: /kb diagnose <error text>")); return; }
        var diag = await KbIndex.DiagnoseError(content, 3);
        screen.AddSystemMsg(diag.Length > 0
            ? L.Pick($"🔎 同类错误历史经验：\n{diag}", $"🔎 Past lessons from similar errors:\n{diag}")
            : L.Pick("🔎 知识库与 git 修复史中暂无匹配，可 /kb mine 提炼或 /kb save 记录。", "🔎 Nothing matches in the knowledge base or the git fix history; mine with /kb mine or record one with /kb save."));
    }

    static void Profile(ChatScreen screen, string rest)
        => screen.AddSystemMsg(rest.Equals("json", StringComparison.OrdinalIgnoreCase)
            ? KbIndex.ProfileToJson()
            : KbIndex.FormatProfile(KbIndex.ProfileStats()));

    static async Task Path(ChatScreen screen)
    {
        screen.AddSystemMsg(L.Pick("🧭 正在根据你的欠缺知识与薄弱点生成学习路径…", "🧭 Building a learning path from your knowledge gaps and weak points…"));
        var (generated, steps) = await KbIndex.GenerateLearningPath();
        if (generated == 0) { screen.AddSystemMsg(L.Pick("📭 暂无欠缺知识可生成路径。`/kb mine` 提炼经验，或 `/kb save gap <内容>` 记录短板。", "📭 No knowledge gaps to build a path from. Mine lessons with `/kb mine`, or record a gap with `/kb save gap <content>`.")); return; }

        var msg = new System.Text.StringBuilder(L.Pick($"🧭 学习路径（{generated} 步）——已接入 /kb review 间隔重复检验掌握：\n", $"🧭 Learning path ({generated} steps) — wired into /kb review for spaced-repetition checks:\n"));
        int i = 1;
        foreach (var s in steps)
        {
            msg.AppendLine(L.Pick($"\n第 {i++} 步：{s.Topic}", $"\nStep {i++}: {s.Topic}"));
            if (s.Why.Length > 0) msg.AppendLine(L.Pick($"  为什么：{s.Why}", $"  Why: {s.Why}"));
            if (s.Practice.Length > 0) msg.AppendLine(L.Pick($"  实践：{s.Practice}", $"  Practice: {s.Practice}"));
            if (s.Check.Length > 0) msg.AppendLine(L.Pick($"  自测：{s.Check}", $"  Self-check: {s.Check}"));
        }
        screen.AddSystemMsg(msg.ToString());
    }

    static async Task Retro(ChatScreen screen)
    {
        var agent = ProgramContext.Agent;
        if (agent == null) { screen.AddSystemMsg(L.Pick("无活跃会话可复盘。", "No active session to retrospect.")); return; }
        var transcript = CommandTextHelpers.BuildTranscript(agent.SnapshotMessages(), 2000);
        if (transcript.Length < 50) { screen.AddSystemMsg(L.Pick("会话内容太少，暂不复盘。", "The session is too short to retrospect.")); return; }

        screen.AddSystemMsg(L.Pick("🔁 正在复盘本次会话并提炼经验…", "🔁 Retrospecting this session and extracting lessons…"));
        var (saved, _) = await KbIndex.Retrospect(transcript);
        screen.AddSystemMsg(saved > 0
            ? L.Pick($"✅ 复盘完成：提炼 {saved} 条经验入知识库（/kb list 查看）。", $"✅ Retrospective done: {saved} lessons filed into the knowledge base (see /kb list).")
            : L.Pick("复盘未提炼出新经验（可能是模型不可用或内容无要点）。", "The retrospective produced no new lessons (the model may be unavailable, or there was nothing worth keeping)."));
    }


    static void Review(ChatScreen screen)
    {
        var entry = KbIndex.PickNextDue(KbIndex.ListEntries());
        if (entry == null)
        {
            screen.AddSystemMsg(L.Pick("🎉 没有到期待复习的经验。`/kb mine` 先提炼一批，或用 `/kb list` 查看现有条目。", "🎉 No lessons are due for review. Mine a batch with `/kb mine`, or browse the existing entries with `/kb list`."));
            return;
        }

        var question = KbIndex.QuizQuestion(entry);
        screen.AddSystemMsg(L.Pick($"🔁 复习「{entry.Description}」〔{KbIndex.KindLabel(entry.Kind)}〕\n\n{question}", $"🔁 Review \"{entry.Description}\" [{KbIndex.KindLabel(entry.Kind)}]\n\n{question}"));

        var knewLabel = L.Pick("我记得 / 能复述", "I remember it / can recite it");
        var recall = UxHelper.Select(L.Pick("你自己会怎么处理？", "How would you handle it yourself?"), [knewLabel, L.Pick("想不起来，看答案", "I can't recall it, show me the answer")]);
        bool knew = recall == knewLabel;

        screen.AddSystemMsg(L.Pick($"📚 答案：\n\n{KbIndex.QuizAnswer(entry)}", $"📚 Answer:\n\n{KbIndex.QuizAnswer(entry)}"));

        var masteredLabel = L.Pick("掌握", "Got it");
        var confirm = UxHelper.Select(knew ? L.Pick("对照答案，你掌握了吗？", "Comparing with the answer, did you get it?") : L.Pick("看过答案，这次掌握了吗？", "Now that you have seen the answer, did you get it?"), [masteredLabel, L.Pick("还没掌握", "Not yet")]);
        bool mastered = confirm == masteredLabel;

        KbIndex.MarkReview(entry.Name, mastered, entry.Kind, entry.Tags);
        screen.AddSystemMsg(mastered
            ? L.Pick($"✅ 已记录掌握，复习间隔 +{KbIndex.LoadReviewState().FirstOrDefault(i => i.Name == entry.Name)?.IntervalDays ?? 1} 天。", $"✅ Recorded as mastered; the review interval is now +{KbIndex.LoadReviewState().FirstOrDefault(i => i.Name == entry.Name)?.IntervalDays ?? 1} days.")
            : L.Pick("📌 已记录未掌握，间隔重置 1 天，相关欠缺知识权重提升。", "📌 Recorded as not mastered; the interval resets to 1 day and the related knowledge gap is weighted higher."));
    }

    static void Weak(ChatScreen screen)
    {
        var report = KbIndex.WeakStats();
        var msg = new System.Text.StringBuilder(L.Pick("🧭 薄弱点统计\n", "🧭 Weak-point statistics\n"));

        msg.AppendLine(L.Pick("\n── 欠缺知识清单 ──", "\n── Knowledge gap list ──"));
        if (report.Gaps.Count == 0)
            msg.AppendLine(L.Pick("（暂无，/kb mine 提炼或复习未掌握时自动沉淀）", "(none yet — mine with /kb mine, or they accumulate automatically when a review is not mastered)"));
        else
            foreach (var g in report.Gaps)
                msg.AppendLine(L.Pick($"  · {g.Description}（权重 {g.Weight:F1}）", $"  · {g.Description} (weight {g.Weight:F1})"));

        msg.AppendLine(L.Pick("\n── 薄弱标签（mistake/bugfix 聚合）──", "\n── Weak tags (mistake/bugfix aggregated) ──"));
        if (report.WeakTags.Count == 0)
            msg.AppendLine(L.Pick("（暂无）", "(none yet)"));
        else
            foreach (var t in report.WeakTags)
                msg.AppendLine($"  · {t.Tag} ×{t.Count}");

        msg.AppendLine(L.Pick("\n── ErrorLog 错误信号 ──", "\n── ErrorLog error signals ──"));
        if (report.ErrorSignals.Count == 0)
            msg.AppendLine(L.Pick("（暂无）", "(none yet)"));
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
            screen.AddSystemMsg(L.Pick("📭 知识库为空。`/kb mine` 从 git 历史提炼第一批经验。", "📭 The knowledge base is empty. Mine a first batch from the git history with `/kb mine`."));
            return;
        }

        var msg = new System.Text.StringBuilder(L.Pick($"📚 知识库共 {entries.Count} 条：\n", $"📚 {entries.Count} entries in the knowledge base:\n"));
        foreach (var e in entries)
            msg.AppendLine(L.Pick($"  [{KbIndex.KindLabel(e.Kind)}] {e.Description}（{e.Name}）", $"  [{KbIndex.KindLabel(e.Kind)}] {e.Description} ({e.Name})"));
        screen.AddSystemMsg(msg.ToString());
    }
}
