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
    public override string Description => "编程知识库（mine 提炼 / save 记住 / diagnose 诊断 / profile 画像 / retro 复盘 / review 自测 / weak 统计）";
    public override string? Usage => "/kb <mine [N]|save|update|forget|search|find|diagnose|profile|retro|review|weak|list>";

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
            case "profile":
                Profile(screen);
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
            "/kb 编程知识库（/mind 同义）\n" +
            "  /kb mine [N]         从 git 历史提炼经验（默认 20）\n" +
            "  /kb save [类别] <内容>  记住一条（自动带日期；类别: mistake/bugfix/habit/gap/code）\n" +
            "  /kb update <关键词> <新> 更新最匹配条目的内容\n" +
            "  /kb forget <内容>     忘记（删除）最匹配条目\n" +
            "  /kb search <内容>     查找相关条目\n" +
            "  /kb diagnose <报错>   诊断报错（召回知识库 + git 修复史）\n" +
            "  /kb profile           技能画像（知识库/git/薄弱标签/ErrorLog）\n" +
            "  /kb retro             复盘本次会话，提炼经验入知识库\n" +
            "  /kb review            间隔重复自测一条到期经验\n" +
            "  /kb weak              欠缺知识清单 + 薄弱点统计\n" +
            "  /kb list              列出全部条目");

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

    static void Save(ChatScreen screen, string content)
    {
        if (content.Length == 0) { screen.AddSystemMsg("用法: /kb save [类别] <内容>"); return; }
        var kind = "";
        var sp = content.IndexOf(' ');
        if (sp > 0)
        {
            var first = content[..sp].ToLowerInvariant();
            if (KbIndex.KbKinds.Contains(first)) { kind = first; content = content[(sp + 1)..].Trim(); }
        }
        var e = KbIndex.SaveManual(content, kind);
        screen.AddSystemMsg($"🧠 已记住「{e.Description}」〔{KbIndex.KindLabel(e.Kind)}〕\n{e.Content}");
    }

    static void Update(ChatScreen screen, string content)
    {
        var sp = content.IndexOf(' ');
        if (sp <= 0) { screen.AddSystemMsg("用法: /kb update <关键词> <新内容>"); return; }
        var keyword = content[..sp].Trim();
        var newContent = content[(sp + 1)..].Trim();
        var updated = KbIndex.UpdateBestMatch(keyword, newContent);
        screen.AddSystemMsg(updated != null
            ? $"📝 已更新「{updated.Description}」〔{KbIndex.KindLabel(updated.Kind)}〕"
            : "🤷 未找到要更新的条目。");
    }

    static void Forget(ChatScreen screen, string content)
    {
        if (content.Length == 0) { screen.AddSystemMsg("用法: /kb forget <内容>"); return; }
        var removed = KbIndex.DeleteBestMatch(content);
        screen.AddSystemMsg(removed != null
            ? $"🗑️ 已忘记「{removed.Description}」"
            : "🤷 未找到匹配的记忆。");
    }

    static void Search(ChatScreen screen, string content)
    {
        if (content.Length == 0) { screen.AddSystemMsg("用法: /kb search <内容>"); return; }
        var hits = KbIndex.Search(content, 10);
        if (hits.Count == 0) { screen.AddSystemMsg("🔍 无匹配条目。"); return; }
        var msg = new StringBuilder($"🔍 找到 {hits.Count} 条：\n");
        foreach (var (hit, score) in hits)
            msg.AppendLine($"  · {hit.Description}〔{KbIndex.KindLabel(hit.Kind)}·相关度 {score:F2}〕");
        screen.AddSystemMsg(msg.ToString());
    }

    static async Task Diagnose(ChatScreen screen, string content)
    {
        if (content.Length == 0) { screen.AddSystemMsg("用法: /kb diagnose <报错文本>"); return; }
        var diag = await KbIndex.DiagnoseError(content, 3);
        screen.AddSystemMsg(diag.Length > 0
            ? $"🔎 同类错误历史经验：\n{diag}"
            : "🔎 知识库与 git 修复史中暂无匹配，可 /kb mine 提炼或 /kb save 记录。");
    }

    static void Profile(ChatScreen screen)
        => screen.AddSystemMsg(KbIndex.FormatProfile(KbIndex.ProfileStats()));

    static async Task Retro(ChatScreen screen)
    {
        var agent = ProgramContext.Agent;
        if (agent == null) { screen.AddSystemMsg("无活跃会话可复盘。"); return; }
        var transcript = BuildTranscript(agent.SnapshotMessages());
        if (transcript.Length < 50) { screen.AddSystemMsg("会话内容太少，暂不复盘。"); return; }

        screen.AddSystemMsg("🔁 正在复盘本次会话并提炼经验…");
        var (saved, _) = await KbIndex.Retrospect(transcript);
        screen.AddSystemMsg(saved > 0
            ? $"✅ 复盘完成：提炼 {saved} 条经验入知识库（/kb list 查看）。"
            : "复盘未提炼出新经验（可能是模型不可用或内容无要点）。");
    }

    /// <summary>把会话消息拼成 role 前缀纯文本（供 LLM 复盘）。</summary>
    static string BuildTranscript(List<JNode> messages)
    {
        var sb = new StringBuilder();
        foreach (var m in messages)
        {
            var role = m["role"]?.AsString() ?? "?";
            var content = m["content"]?.AsString() ?? "";
            if (content.Length == 0) continue;
            if (content.Length > 2000) content = ContextManager.TruncateByRunes(content, 2000);
            sb.AppendLine($"## {role}");
            sb.AppendLine(content);
        }
        return sb.ToString();
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
