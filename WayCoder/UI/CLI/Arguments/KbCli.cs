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
            case "profile":
                return Profile();
            case "retro":
                return Retro().GetAwaiter().GetResult();
            case "review":
                return Review();
            case "weak":
                return Weak();
            case "list":
                return List();
            default:
                Console.WriteLine("编程知识库 --kb <mine [N]|save|update|forget|search|diagnose|profile|retro|review|weak|list>");
                Console.WriteLine("  mine [N]            从 git 历史提炼经验（默认 20）");
                Console.WriteLine("  save [类别] <内容>    手动记住一条（自动带日期）");
                Console.WriteLine("  update <关键词> <新>  更新最匹配条目");
                Console.WriteLine("  forget <内容>        忘记（删除）最匹配条目");
                Console.WriteLine("  search <内容>        查找相关条目");
                Console.WriteLine("  diagnose <报错>      诊断报错（召回知识库 + git 修复史）");
                Console.WriteLine("  profile             技能画像");
                Console.WriteLine("  retro               复盘本次会话提炼经验");
                Console.WriteLine("  review              间隔重复自测一条到期经验");
                Console.WriteLine("  weak                欠缺知识清单 + 薄弱点统计");
                Console.WriteLine("  list                列出全部经验条目");
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
        if (arg.Length == 0) { Console.WriteLine("用法: --kb save [类别] <内容>"); return 1; }
        var e = KbIndex.SaveManual(arg, kind);
        Console.WriteLine($"🧠 已记住「{e.Description}」〔{KbIndex.KindLabel(e.Kind)}〕");
        return 0;
    }

    static int Update(string arg)
    {
        var sp = arg.IndexOf(' ');
        if (sp <= 0) { Console.WriteLine("用法: --kb update <关键词> <新内容>"); return 1; }
        var updated = KbIndex.UpdateBestMatch(arg[..sp].Trim(), arg[(sp + 1)..].Trim());
        Console.WriteLine(updated != null ? $"📝 已更新「{updated.Description}」" : "🤷 未找到要更新的条目。");
        return 0;
    }

    static int Forget(string arg)
    {
        if (arg.Length == 0) { Console.WriteLine("用法: --kb forget <内容>"); return 1; }
        var removed = KbIndex.DeleteBestMatch(arg.Trim());
        Console.WriteLine(removed != null ? $"🗑️ 已忘记「{removed.Description}」" : "🤷 未找到匹配条目。");
        return 0;
    }

    static int Search(string arg)
    {
        if (arg.Length == 0) { Console.WriteLine("用法: --kb search <内容>"); return 1; }
        var hits = KbIndex.Search(arg.Trim(), 10);
        if (hits.Count == 0) { Console.WriteLine("🔍 无匹配条目。"); return 0; }
        Console.WriteLine($"🔍 找到 {hits.Count} 条：");
        foreach (var (hit, score) in hits)
            Console.WriteLine($"  · {hit.Description}〔{KbIndex.KindLabel(hit.Kind)}·相关度 {score:F2}〕");
        return 0;
    }

    static int Mine(string arg)
    {
        int count = 20;
        if (int.TryParse(arg, out var n) && n > 0) count = n;
        Console.WriteLine($"⛏️ 正在从最近 {count} 个提交提炼经验…");
        var (mined, errors) = KbIndex.MineAsync(count).GetAwaiter().GetResult();
        Console.WriteLine($"✅ 新增 {mined} 条经验 → {KbIndex.Dir}");
        foreach (var e in errors) Console.WriteLine($"  ⚠️ {e}");
        return 0;
    }

    static async Task<int> Diagnose(string arg)
    {
        if (arg.Length == 0) { Console.WriteLine("用法: --kb diagnose <报错文本>"); return 1; }
        var diag = await KbIndex.DiagnoseError(arg.Trim(), 3);
        Console.WriteLine(diag.Length > 0 ? $"🔎 同类错误历史经验：\n{diag}" : "🔎 知识库与 git 修复史中暂无匹配。");
        return 0;
    }

    static int Profile()
    {
        Console.WriteLine(KbIndex.FormatProfile(KbIndex.ProfileStats()));
        return 0;
    }

    static async Task<int> Retro()
    {
        var agent = ProgramContext.Agent;
        if (agent == null) { Console.WriteLine("无活跃会话可复盘（--kb retro 需在 TUI/-p 会话中使用）。"); return 1; }
        var sb = new System.Text.StringBuilder();
        foreach (var m in agent.SnapshotMessages())
        {
            var role = m["role"]?.AsString() ?? "?";
            var content = m["content"]?.AsString() ?? "";
            if (content.Length == 0) continue;
            sb.AppendLine($"## {role}\n{content}");
        }
        if (sb.Length < 50) { Console.WriteLine("会话内容太少，暂不复盘。"); return 0; }
        var (saved, _) = await KbIndex.Retrospect(sb.ToString());
        Console.WriteLine(saved > 0 ? $"✅ 复盘完成：提炼 {saved} 条经验入知识库。" : "复盘未提炼出新经验。");
        return 0;
    }

    static int Review()
    {
        var entry = KbIndex.PickNextDue(KbIndex.ListEntries());
        if (entry == null) { Console.WriteLine("🎉 没有到期待复习的经验。"); return 0; }

        Console.WriteLine($"🔁 复习「{entry.Description}」");
        Console.WriteLine();
        Console.WriteLine(KbIndex.QuizQuestion(entry));
        Console.WriteLine();

        bool mastered;
        if (Console.IsInputRedirected)
        {
            // 非交互（管道/CI）：直接学习模式，展示答案，不询问
            Console.WriteLine("──── 答案 ────");
            Console.WriteLine(KbIndex.QuizAnswer(entry));
            Console.WriteLine("（非交互模式，本次不记录复习进度）");
            return 0;
        }

        Console.Write("你自己会怎么处理？[Y] 我记得 / 能复述  [N] 想不起来看答案 > ");
        var recall = Console.ReadLine()?.Trim();
        bool knew = recall is null || recall.Length == 0 || recall.StartsWith("y", StringComparison.OrdinalIgnoreCase);

        Console.WriteLine("──── 答案 ────");
        Console.WriteLine(KbIndex.QuizAnswer(entry));
        Console.WriteLine();

        Console.Write(knew ? "对照答案，掌握了吗？[Y/N] > " : "看过答案，这次掌握了吗？[Y/N] > ");
        var confirm = Console.ReadLine()?.Trim();
        mastered = confirm is null || confirm.Length == 0 || confirm.StartsWith("y", StringComparison.OrdinalIgnoreCase);

        KbIndex.MarkReview(entry.Name, mastered, entry.Kind, entry.Tags);
        Console.WriteLine(mastered ? "✅ 已记录掌握，复习间隔增长。" : "📌 已记录未掌握，间隔重置 1 天，相关欠缺知识权重提升。");
        return 0;
    }

    static int Weak()
    {
        var report = KbIndex.WeakStats();
        Console.WriteLine("🧭 薄弱点统计");
        Console.WriteLine();
        Console.WriteLine("── 欠缺知识清单 ──");
        if (report.Gaps.Count == 0) Console.WriteLine("（暂无）");
        else foreach (var g in report.Gaps) Console.WriteLine($"  · {g.Description}（权重 {g.Weight:F1}）");

        Console.WriteLine();
        Console.WriteLine("── 薄弱标签 ──");
        if (report.WeakTags.Count == 0) Console.WriteLine("（暂无）");
        else foreach (var t in report.WeakTags) Console.WriteLine($"  · {t.Tag} ×{t.Count}");

        Console.WriteLine();
        Console.WriteLine("── ErrorLog 错误信号 ──");
        if (report.ErrorSignals.Count == 0) Console.WriteLine("（暂无）");
        else foreach (var s in report.ErrorSignals) Console.WriteLine($"  · {s.Source} ×{s.Count}");
        return 0;
    }

    static int List()
    {
        var entries = KbIndex.ListEntries();
        if (entries.Count == 0) { Console.WriteLine("📭 知识库为空。"); return 0; }
        Console.WriteLine($"📚 知识库共 {entries.Count} 条：");
        foreach (var e in entries)
            Console.WriteLine($"  [{KbIndex.KindLabel(e.Kind)}] {e.Description}（{e.Name}）");
        return 0;
    }
}
