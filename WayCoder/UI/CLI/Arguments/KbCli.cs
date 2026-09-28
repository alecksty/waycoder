namespace WayCoder.UI.Cli.Arguments;

/// <summary>编程知识库 CLI 纯逻辑（mine/save/update/forget/search/review/weak/list，输出到 Console）。</summary>
public static class KbCli
{
    public static int Run(List<string> values)
    {
        var sub = values.Count > 0 ? values[0].ToLowerInvariant() : "";
        // 内容可能含空格，join 剩余全部参数（如 save habit "GitHub 网络..."）
        var rest = values.Count > 1 ? string.Join(" ", values.Skip(1)) : "";

        switch (sub)
        {
            case "mine":
                return Mine(rest);
            case "save":
                return Save(rest);
            case "update":
                return Update(rest);
            case "forget":
                return Forget(rest);
            case "search":
            case "find":
                return Search(rest);
            case "diagnose":
                return Diagnose(rest).GetAwaiter().GetResult();
            case "path":
                return Path().GetAwaiter().GetResult();
            case "profile":
                return Profile(rest);
            case "retro":
                return Retro().GetAwaiter().GetResult();
            case "review":
                return Review();
            case "weak":
                return Weak();
            case "list":
                return List();
            default:
                Console.WriteLine(L.Pick(
                    "编程知识库 --kb <mine [N]|save|update|forget|search|diagnose|path|profile [json]|retro|review|weak|list>",
                    "Knowledge base --kb <mine [N]|save|update|forget|search|diagnose|path|profile [json]|retro|review|weak|list>"));
                Console.WriteLine(L.Pick("  mine [N]            从 git 历史提炼经验（默认 20）",
                                         "  mine [N]            Extract lessons from git history (default 20)"));
                Console.WriteLine(L.Pick("  save [类别] <内容>    手动记住一条（自动带日期）",
                                         "  save [kind] <body>  Remember one entry by hand (dated automatically)"));
                Console.WriteLine(L.Pick("  update <关键词> <新>  更新最匹配条目",
                                         "  update <kw> <new>   Update the closest matching entry"));
                Console.WriteLine(L.Pick("  forget <内容>        忘记（删除）最匹配条目",
                                         "  forget <body>       Forget (delete) the closest matching entry"));
                Console.WriteLine(L.Pick("  search <内容>        查找相关条目",
                                         "  search <body>       Search for related entries"));
                Console.WriteLine(L.Pick("  diagnose <报错>      诊断报错（召回知识库 + git 修复史）",
                                         "  diagnose <error>    Diagnose an error (recalls the knowledge base + git fix history)"));
                Console.WriteLine(L.Pick("  path                生成学习路径（欠缺→进阶，接入 /kb review）",
                                         "  path                Generate a learning path (gaps -> advanced, feeds /kb review)"));
                Console.WriteLine(L.Pick("  profile [json]       技能画像（json 导出）",
                                         "  profile [json]      Skill profile (json export)"));
                Console.WriteLine(L.Pick("  retro               复盘本次会话提炼经验",
                                         "  retro               Retrospect this session and extract lessons"));
                Console.WriteLine(L.Pick("  review              间隔重复自测一条到期经验",
                                         "  review              Spaced-repetition self-test for one due lesson"));
                Console.WriteLine(L.Pick("  weak                欠缺知识清单 + 薄弱点统计",
                                         "  weak                Gap list + weakness stats"));
                Console.WriteLine(L.Pick("  list                列出全部经验条目",
                                         "  list                List every lesson entry"));
                return 0;
        }
    }

    static int Save(string arg)
    {
        var kind = "";
        var sp = arg.IndexOf(' ');
        if (sp > 0)
        {
            var first = arg[..sp].ToLowerInvariant();
            if (KbIndex.KbKinds.Contains(first)) { kind = first; arg = arg[(sp + 1)..].Trim(); }
        }
        if (arg.Length == 0) { Console.WriteLine(L.Pick("用法: --kb save [类别] <内容>", "Usage: --kb save [kind] <body>")); return 1; }
        var e = KbIndex.SaveManual(arg, kind);
        Console.WriteLine(L.Pick($"🧠 已记住「{e.Description}」〔{KbIndex.KindLabel(e.Kind)}〕",
                                 $"🧠 Remembered \"{e.Description}\" [{KbIndex.KindLabel(e.Kind)}]"));
        return 0;
    }

    static int Update(string arg)
    {
        var sp = arg.IndexOf(' ');
        if (sp <= 0) { Console.WriteLine(L.Pick("用法: --kb update <关键词> <新内容>", "Usage: --kb update <keyword> <new body>")); return 1; }
        var updated = KbIndex.UpdateBestMatch(arg[..sp].Trim(), arg[(sp + 1)..].Trim());
        Console.WriteLine(updated != null
            ? L.Pick($"📝 已更新「{updated.Description}」", $"📝 Updated \"{updated.Description}\"")
            : L.Pick("🤷 未找到要更新的条目。", "🤷 No entry found to update."));
        return 0;
    }

    static int Forget(string arg)
    {
        if (arg.Length == 0) { Console.WriteLine(L.Pick("用法: --kb forget <内容>", "Usage: --kb forget <body>")); return 1; }
        var removed = KbIndex.DeleteBestMatch(arg.Trim());
        Console.WriteLine(removed != null
            ? L.Pick($"🗑️ 已忘记「{removed.Description}」", $"🗑️ Forgot \"{removed.Description}\"")
            : L.Pick("🤷 未找到匹配条目。", "🤷 No matching entry found."));
        return 0;
    }

    static int Search(string arg)
    {
        if (arg.Length == 0) { Console.WriteLine(L.Pick("用法: --kb search <内容>", "Usage: --kb search <body>")); return 1; }
        var hits = KbIndex.Search(arg.Trim(), 10);
        if (hits.Count == 0) { Console.WriteLine(L.Pick("🔍 无匹配条目。", "🔍 No matching entries.")); return 0; }
        Console.WriteLine(L.Pick($"🔍 找到 {hits.Count} 条：", $"🔍 {hits.Count} match(es):"));
        foreach (var (hit, score) in hits)
            Console.WriteLine(L.Pick($"  · {hit.Description}〔{KbIndex.KindLabel(hit.Kind)}·相关度 {score:F2}〕",
                                     $"  · {hit.Description} [{KbIndex.KindLabel(hit.Kind)}, relevance {score:F2}]"));
        return 0;
    }

    static int Mine(string arg)
    {
        int count = 20;
        if (int.TryParse(arg, out var n) && n > 0) count = n;
        Console.WriteLine(L.Pick($"⛏️ 正在从最近 {count} 个提交提炼经验…", $"⛏️ Mining lessons from the last {count} commits..."));
        var (mined, errors) = KbIndex.MineAsync(count).GetAwaiter().GetResult();
        Console.WriteLine(L.Pick($"✅ 新增 {mined} 条经验 → {KbIndex.Dir}", $"✅ Added {mined} lesson(s) → {KbIndex.Dir}"));
        foreach (var e in errors) Console.WriteLine($"  ⚠️ {e}");
        return 0;
    }

    static async Task<int> Diagnose(string arg)
    {
        if (arg.Length == 0) { Console.WriteLine(L.Pick("用法: --kb diagnose <报错文本>", "Usage: --kb diagnose <error text>")); return 1; }
        var diag = await KbIndex.DiagnoseError(arg.Trim(), 3);
        Console.WriteLine(diag.Length > 0
            ? L.Pick($"🔎 同类错误历史经验：\n{diag}", $"🔎 Past lessons for similar errors:\n{diag}")
            : L.Pick("🔎 知识库与 git 修复史中暂无匹配。", "🔎 No match in the knowledge base or git fix history."));
        return 0;
    }

    static int Profile(string rest)
    {
        if (rest.Equals("json", StringComparison.OrdinalIgnoreCase))
            Console.WriteLine(KbIndex.ProfileToJson());
        else
            Console.WriteLine(KbIndex.FormatProfile(KbIndex.ProfileStats()));
        return 0;
    }

    static async Task<int> Path()
    {
        Console.WriteLine(L.Pick("🧭 正在生成学习路径…", "🧭 Generating a learning path..."));
        var (generated, steps) = await KbIndex.GenerateLearningPath();
        if (generated == 0) { Console.WriteLine(L.Pick("📭 暂无欠缺知识可生成路径。", "📭 No knowledge gaps to build a path from.")); return 0; }
        Console.WriteLine(L.Pick($"🧭 学习路径（{generated} 步）——已接入 /kb review 间隔重复：",
                                 $"🧭 Learning path ({generated} step(s)) — wired into /kb review spaced repetition:"));
        int i = 1;
        foreach (var s in steps)
        {
            // ⚠ 先自增再进 L.Pick：L.Pick 两个实参都会被求值，写成 $"...{i++}..." 会**每步自增两次**
            var step = i++;
            Console.WriteLine(L.Pick($"\n第 {step} 步：{s.Topic}", $"\nStep {step}: {s.Topic}"));
            if (s.Why.Length > 0) Console.WriteLine(L.Pick($"  为什么：{s.Why}", $"  Why: {s.Why}"));
            if (s.Practice.Length > 0) Console.WriteLine(L.Pick($"  实践：{s.Practice}", $"  Practice: {s.Practice}"));
            if (s.Check.Length > 0) Console.WriteLine(L.Pick($"  自测：{s.Check}", $"  Self-check: {s.Check}"));
        }
        return 0;
    }

    static async Task<int> Retro()
    {
        var agent = ProgramContext.Agent;
        if (agent == null)
        {
            Console.WriteLine(L.Pick("无活跃会话可复盘（--kb retro 需在 TUI/-p 会话中使用）。",
                                     "No active session to retrospect (--kb retro needs a TUI or -p session)."));
            return 1;
        }
        var sb = new System.Text.StringBuilder();
        foreach (var m in agent.SnapshotMessages())
        {
            var role = m["role"]?.AsString() ?? "?";
            var content = m["content"]?.AsString() ?? "";
            if (content.Length == 0) continue;
            sb.AppendLine($"## {role}\n{content}");
        }
        if (sb.Length < 50) { Console.WriteLine(L.Pick("会话内容太少，暂不复盘。", "Session content is too short to retrospect.")); return 0; }
        var (saved, _) = await KbIndex.Retrospect(sb.ToString());
        Console.WriteLine(saved > 0
            ? L.Pick($"✅ 复盘完成：提炼 {saved} 条经验入知识库。", $"✅ Retrospective done: {saved} lesson(s) saved to the knowledge base.")
            : L.Pick("复盘未提炼出新经验。", "The retrospective produced no new lessons."));
        return 0;
    }

    static int Review()
    {
        var entry = KbIndex.PickNextDue(KbIndex.ListEntries());
        if (entry == null) { Console.WriteLine(L.Pick("🎉 没有到期待复习的经验。", "🎉 No lessons are due for review.")); return 0; }

        Console.WriteLine(L.Pick($"🔁 复习「{entry.Description}」", $"🔁 Reviewing \"{entry.Description}\""));
        Console.WriteLine();
        Console.WriteLine(KbIndex.QuizQuestion(entry));
        Console.WriteLine();

        bool mastered;
        if (Console.IsInputRedirected)
        {
            // 非交互（管道/CI）：直接学习模式，展示答案，不询问
            Console.WriteLine(L.Pick("──── 答案 ────", "──── Answer ────"));
            Console.WriteLine(KbIndex.QuizAnswer(entry));
            Console.WriteLine(L.Pick("（非交互模式，本次不记录复习进度）", "(non-interactive mode; review progress is not recorded)"));
            return 0;
        }

        Console.Write(L.Pick("你自己会怎么处理？[Y] 我记得 / 能复述  [N] 想不起来看答案 > ",
                             "How would you handle it? [Y] I remember / can restate  [N] Can't recall, show the answer > "));
        var recall = Console.ReadLine()?.Trim();
        bool knew = recall is null || recall.Length == 0 || recall.StartsWith("y", StringComparison.OrdinalIgnoreCase);

        Console.WriteLine(L.Pick("──── 答案 ────", "──── Answer ────"));
        Console.WriteLine(KbIndex.QuizAnswer(entry));
        Console.WriteLine();

        Console.Write(knew
            ? L.Pick("对照答案，掌握了吗？[Y/N] > ", "Comparing with the answer, have you mastered it? [Y/N] > ")
            : L.Pick("看过答案，这次掌握了吗？[Y/N] > ", "Having seen the answer, have you mastered it now? [Y/N] > "));
        var confirm = Console.ReadLine()?.Trim();
        mastered = confirm is null || confirm.Length == 0 || confirm.StartsWith("y", StringComparison.OrdinalIgnoreCase);

        KbIndex.MarkReview(entry.Name, mastered, entry.Kind, entry.Tags);
        Console.WriteLine(mastered
            ? L.Pick("✅ 已记录掌握，复习间隔增长。", "✅ Marked as mastered; the review interval grows.")
            : L.Pick("📌 已记录未掌握，间隔重置 1 天，相关欠缺知识权重提升。", "📌 Marked as not mastered; the interval resets to 1 day and related gaps gain weight."));
        return 0;
    }

    static int Weak()
    {
        var report = KbIndex.WeakStats();
        Console.WriteLine(L.Pick("🧭 薄弱点统计", "🧭 Weakness stats"));
        Console.WriteLine();
        Console.WriteLine(L.Pick("── 欠缺知识清单 ──", "── Knowledge gaps ──"));
        if (report.Gaps.Count == 0) Console.WriteLine(L.Pick("（暂无）", "(none)"));
        else foreach (var g in report.Gaps) Console.WriteLine(L.Pick($"  · {g.Description}（权重 {g.Weight:F1}）", $"  · {g.Description} (weight {g.Weight:F1})"));

        Console.WriteLine();
        Console.WriteLine(L.Pick("── 薄弱标签 ──", "── Weak tags ──"));
        if (report.WeakTags.Count == 0) Console.WriteLine(L.Pick("（暂无）", "(none)"));
        else foreach (var t in report.WeakTags) Console.WriteLine($"  · {t.Tag} ×{t.Count}");

        Console.WriteLine();
        Console.WriteLine(L.Pick("── ErrorLog 错误信号 ──", "── ErrorLog error signals ──"));
        if (report.ErrorSignals.Count == 0) Console.WriteLine(L.Pick("（暂无）", "(none)"));
        else foreach (var s in report.ErrorSignals) Console.WriteLine($"  · {s.Source} ×{s.Count}");
        return 0;
    }

    static int List()
    {
        var entries = KbIndex.ListEntries();
        if (entries.Count == 0) { Console.WriteLine(L.Pick("📭 知识库为空。", "📭 The knowledge base is empty.")); return 0; }
        Console.WriteLine(L.Pick($"📚 知识库共 {entries.Count} 条：", $"📚 {entries.Count} entries in the knowledge base:"));
        foreach (var e in entries)
            Console.WriteLine(L.Pick($"  [{KbIndex.KindLabel(e.Kind)}] {e.Description}（{e.Name}）",
                                     $"  [{KbIndex.KindLabel(e.Kind)}] {e.Description} ({e.Name})"));
        return 0;
    }
}
