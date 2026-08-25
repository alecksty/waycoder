namespace WayCoder.UI.Cli.Arguments;

/// <summary>编程知识库 CLI 纯逻辑（mine/review/weak/list，输出到 Console）。</summary>
public static class KbCli
{
    public static int Run(List<string> values)
    {
        var sub = values.Count > 0 ? values[0].ToLowerInvariant() : "";
        var rest = values.Count > 1 ? values[1] : "";

        switch (sub)
        {
            case "mine":
                return Mine(rest);
            case "review":
                return Review();
            case "weak":
                return Weak();
            case "list":
                return List();
            default:
                Console.WriteLine("编程知识库 --kb <mine [N]|review|weak|list>");
                Console.WriteLine("  mine [N]   从 git 历史提炼经验（默认 20）");
                Console.WriteLine("  review     间隔重复自测一条到期经验");
                Console.WriteLine("  weak       欠缺知识清单 + 薄弱点统计");
                Console.WriteLine("  list       列出全部经验条目");
                return 0;
        }
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
