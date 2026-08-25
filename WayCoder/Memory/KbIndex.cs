using System.Text;
using WayCoder.Infra;

namespace WayCoder;

/// <summary>
/// 自主学习编程知识库 —— 把工作痕迹（git 提交 / 错误日志）提炼成四类经验条目，全局保存供跨项目积累个人编程经验：
///   mistake  容易犯的错误（反模式/踩坑）
///   bugfix   复杂 bug 修复过程（现象→根因→修复→教训）
///   habit    个人使用习惯（怎么工作/偏好）
///   gap      欠缺的知识（一条一知识点，/kb weak 直接输出）
///
/// 条目是 ~/.waycoder/kb/*.md 的标准 frontmatter 文件（复用 <see cref="StructuredMemory.ParseFrontmatter"/> 解析），
/// 纯文本便于将来分享（导出/git-sync）。复习调度状态存同目录 kb.json（AOT 安全 JNode + 原子写）。
/// 检索复用 <see cref="SemanticMemory"/> 的 TF-IDF 引擎，<see cref="SystemPrompt"/> 启动时注入相关经验。
/// </summary>
public static class KbIndex
{
    /// <summary>知识库分类（用户定义）：mistake 容易犯的错误 / bugfix 复杂 bug 修复 / habit 个人习惯 / gap 欠缺知识 / code 代码片段。</summary>
    public static readonly string[] KbKinds = ["mistake", "bugfix", "habit", "gap", "code"];

    /// <summary>全局知识库目录（~/.waycoder/kb/）。</summary>
    public static string Dir => Global.GlobalConfigPath("kb");

    /// <summary>复习调度状态文件。</summary>
    static string StatePath => Path.Combine(Dir, "kb.json");

    // 间隔重复阶梯（天）
    static readonly int[] ReviewIntervals = [1, 3, 7, 14, 30];

    // ═══════════════════════════════════════════════════════════════
    // 条目模型与存储
    // ═══════════════════════════════════════════════════════════════

    /// <summary>一条知识库经验条目。</summary>
    public class KbEntry
    {
        public string Name = "";
        public string Description = "";
        public string Kind = "bugfix";
        public string Content = "";
        public string Source = "manual";
        public List<string> Tags = [];
        public DateTime UpdatedAt = DateTime.MinValue;
        public string FilePath = "";
    }

    static void EnsureDir()
    {
        if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
    }

    /// <summary>列出全部知识库条目（只认四类，按更新时间倒序）。</summary>
    public static List<KbEntry> ListEntries()
    {
        var list = new List<KbEntry>();
        if (!Directory.Exists(Dir)) return list;
        foreach (var f in Directory.GetFiles(Dir, "*.md"))
        {
            var e = ReadFile(f);
            if (e != null) list.Add(e);
        }
        list.Sort((a, b) => b.UpdatedAt.CompareTo(a.UpdatedAt));
        return list;
    }

    /// <summary>按名称取一条（null = 不存在）。</summary>
    public static KbEntry? Get(string name)
    {
        var path = Path.Combine(Dir, SanitizeName(name) + ".md");
        return File.Exists(path) ? ReadFile(path) : null;
    }

    static KbEntry? ReadFile(string path)
    {
        try
        {
            var text = File.ReadAllText(path);
            var (fm, body) = StructuredMemory.ParseFrontmatter(text);
            var kind = fm.GetValueOrDefault("kind");
            if (!KbKinds.Contains(kind)) return null; // 只认四类，其余文件不纳入
            var fi = new FileInfo(path);
            return new KbEntry
            {
                Name = fm.GetValueOrDefault("name") ?? Path.GetFileNameWithoutExtension(path),
                Description = fm.GetValueOrDefault("description") ?? "",
                Kind = kind,
                Content = body.Trim(),
                Source = fm.GetValueOrDefault("source") ?? "manual",
                Tags = ParseTags(fm.GetValueOrDefault("tags")),
                UpdatedAt = fi.LastWriteTime,
                FilePath = path,
            };
        }
        catch { return null; }
    }

    /// <summary>写入/覆盖一条经验条目（标准 frontmatter，kind/source/tags 自定义键）。</summary>
    public static void WriteEntry(KbEntry e)
    {
        EnsureDir();
        e.Name = SanitizeName(e.Name);
        e.FilePath = Path.Combine(Dir, e.Name + ".md");
        var now = DateTime.Now;
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"name: {e.Name}");
        sb.AppendLine($"description: {e.Description.ReplaceLineEndings(" ")}");
        sb.AppendLine("type: reference");
        sb.AppendLine($"kind: {e.Kind}");
        sb.AppendLine($"source: {e.Source}");
        sb.AppendLine($"tags: {string.Join(" ", e.Tags)}");
        sb.AppendLine($"created: {now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"updated: {now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(e.Content.Trim());
        File.WriteAllText(e.FilePath, sb.ToString());
        e.UpdatedAt = now;
    }

    /// <summary>删除一条（不存在返回 false）。</summary>
    public static bool DeleteEntry(string name)
    {
        var path = Path.Combine(Dir, SanitizeName(name) + ".md");
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }

    static List<string> ParseTags(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return [];
        return s.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim(','))
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>名称规范化：小写 kebab（保留 CJK），≤40 rune。</summary>
    public static string SanitizeName(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in (s ?? "").Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (sb.Length > 0 && sb[^1] != '-') sb.Append('-');
        }
        var result = sb.ToString().Trim('-');
        if (result.Length == 0) result = "kb-entry";
        return ContextManager.TruncateByRunes(result, 40);
    }

    // ═══════════════════════════════════════════════════════════════
    // /kb mine —— 从 git 历史提炼经验
    // ═══════════════════════════════════════════════════════════════

    /// <summary>提炼器 system 提示词：把 git 提交归纳成四类经验 JSON。</summary>
    public const string SummarizerPrompt = """
        你是资深编程经验提炼器。把给定的 git 提交（主题 + 改动统计）归纳成一条结构化经验，输出严格 JSON：
        {
          "name": "kebab-case 短名（如 git-force-push-guard）",
          "description": "一行摘要",
          "kind": "mistake | bugfix | habit | gap",
          "phenomenon": "现象/问题",
          "root_cause": "根因",
          "fix": "修复方法",
          "lesson": "教训/以后怎么做",
          "tags": ["标签1", "标签2"],
          "gaps": ["该修复暴露的欠缺知识点（字符串数组，无则空数组）"]
        }
        只输出 JSON，不要多余文字。kind 判定：fix 修复既有 bug → bugfix；防再犯/约束 → mistake；
        工作流程偏好 → habit；纯粹补知识盲区 → gap。gaps 用于自动沉淀「欠缺知识清单」。
        """;

    /// <summary>
    /// 挖掘最近 N 个提交生成经验条目。返回 (成功条数, 错误列表)。
    /// summarize 为空时用真实 LLM（小模型）；测试可注入假提炼器。
    /// </summary>
    public static async Task<(int Mined, List<string> Errors)> MineAsync(int count,
        Func<string, Task<string?>>? summarize = null)
    {
        var (ec, logOut, logErr) = await GitRunner.RunAsync($"log --format=%H|%s -{count}", null, CancellationToken.None);
        if (ec != 0 || string.IsNullOrWhiteSpace(logOut))
            return (0, [$"git log 失败: {logErr.Trim()}".Trim()]);

        int mined = 0;
        var errors = new List<string>();
        foreach (var line in logOut.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var bar = line.IndexOf('|');
            if (bar <= 0) continue;
            var hash = line[..bar].Trim();
            var subject = line[(bar + 1)..].Trim();

            var (ec2, showOut, _) = await GitRunner.RunAsync($"show {hash} --stat --no-color --format=", null, CancellationToken.None);
            var info = $"提交: {subject}\n\n{ContextManager.TruncateKeepHeadTail(showOut, 2000, 800, "\n…\n")}";

            string? json = summarize != null ? await summarize(info) : await SummarizeWithLLM(info);
            var draft = json != null ? BuildEntry(json) : BuildFallback(subject, showOut);
            if (draft == null) { errors.Add($"跳过「{subject}」：JSON 解析失败"); continue; }

            WriteEntry(draft);
            mined++;

            // gaps[] → 自动沉淀欠缺知识清单条目
            if (json != null)
                foreach (var gap in ExtractGaps(json))
                    WriteEntry(gap);
        }
        return (mined, errors);
    }

    /// <summary>真实 LLM 提炼（小模型一次性补全；不可用返回 null → 走降级）。</summary>
    public static async Task<string?> SummarizeWithLLM(string info)
    {
        var agent = ProgramContext.Agent;
        if (agent?.LlmClient == null) return null;
        var llm = agent.LlmClient;
        return await Agent.WithModelOverrideAsync(llm, llm.SmallModel, async () =>
        {
            var resp = await llm.ChatAsync(
                [
                    JNode.Object().Set("role", "system").Set("content", SummarizerPrompt),
                    JNode.Object().Set("role", "user").Set("content", info),
                ],
                tools: null);
            return resp?.Content;
        });
    }

    /// <summary>把 LLM 返回 JSON 解析成条目草稿；解析失败返回 null。</summary>
    public static KbEntry? BuildEntry(string json)
    {
        JNode? root;
        try { root = Json.Parse(json); }
        catch { return null; }
        if (root?.Kind != JKind.Object) return null;

        var name = root["name"]?.AsString() ?? "";
        if (string.IsNullOrWhiteSpace(name)) return null;

        var tags = (root["tags"]?.Items ?? [])
            .Select(t => t.AsString() ?? "").Where(t => t.Length > 0).ToList();
        var phen = root["phenomenon"]?.AsString() ?? "";
        var cause = root["root_cause"]?.AsString() ?? "";
        var fix = root["fix"]?.AsString() ?? "";
        var lesson = root["lesson"]?.AsString() ?? "";
        var content = BuildContent(phen, cause, fix, lesson);
        if (content.Length == 0) content = root["description"]?.AsString() ?? name;

        return new KbEntry
        {
            Name = name,
            Description = root["description"]?.AsString() ?? name,
            Kind = NormalizeKind(root["kind"]?.AsString() ?? ""),
            Content = content,
            Source = "git-commit",
            Tags = tags,
        };
    }

    /// <summary>LLM 不可用/解析失败时的降级条目（仅用提交主题+改动统计）。</summary>
    public static KbEntry BuildFallback(string subject, string stat)
    {
        return new KbEntry
        {
            Name = SanitizeName(subject),
            Description = subject,
            Kind = "bugfix",
            Content = $"**提交**：{subject}\n\n**改动**：\n{ContextManager.TruncateByRunes(stat, 800)}",
            Source = "git-commit",
        };
    }

    /// <summary>从 JSON 的 gaps[] 提取欠缺知识条目。</summary>
    public static List<KbEntry> ExtractGaps(string json)
    {
        var list = new List<KbEntry>();
        JNode? root;
        try { root = Json.Parse(json); }
        catch { return list; }
        if (root?.Kind != JKind.Object) return list;

        foreach (var g in root["gaps"]?.Items ?? [])
        {
            var text = (g.AsString() ?? "").Trim();
            if (text.Length == 0) continue;
            list.Add(new KbEntry
            {
                Name = "gap-" + SanitizeName(text),
                Description = $"欠缺知识：{text}",
                Kind = "gap",
                Content = $"**欠缺知识点**：{text}\n\n来源：由 git 提交经验自动提炼，复习未掌握时权重提升。",
                Source = "git-gap",
            });
        }
        return list;
    }

    static string BuildContent(string phen, string cause, string fix, string lesson)
    {
        var sb = new StringBuilder();
        if (phen.Length > 0) sb.AppendLine($"**现象**：{phen}");
        if (cause.Length > 0) sb.AppendLine($"**根因**：{cause}");
        if (fix.Length > 0) sb.AppendLine($"**修复**：{fix}");
        if (lesson.Length > 0) sb.AppendLine($"**教训**：{lesson}");
        return sb.ToString().Trim();
    }

    static string NormalizeKind(string kind) => KbKinds.Contains(kind) ? kind : "bugfix";

    // ═══════════════════════════════════════════════════════════════
    // /mind —— 手动记忆管理（用户强制记住/忘记/查找）
    // ═══════════════════════════════════════════════════════════════

    /// <summary>手动保存一条带日期的知识条目（/mind save）。kind 可显式指定五类之一，空则自动识别（代码片段/默认 habit）。</summary>
    public static KbEntry SaveManual(string content, string kind = "")
    {
        if (kind.Length == 0) kind = DetectKind(content);
        var now = DateTime.Now;
        var entry = new KbEntry
        {
            Name = SanitizeName(content),
            Description = FirstLine(content),
            Kind = NormalizeKind(kind),
            Content = content.Trim().Contains('\n') && kind == "code"
                ? $"**{now:yyyy-MM-dd}**（代码片段）：\n```\n{content.Trim()}\n```"
                : $"**{now:yyyy-MM-dd}**：{content.Trim()}",
            Source = "manual",
            Tags = [],
        };
        WriteEntry(entry);
        return entry;
    }

    /// <summary>按关键词找到最匹配条目并更新其内容（/mind update）。返回更新后的条目或 null。</summary>
    public static KbEntry? UpdateBestMatch(string keyword, string newContent)
    {
        var entries = ListEntries();
        if (entries.Count == 0) return null;
        var want = keyword.Trim();
        var target = entries.FirstOrDefault(e =>
                e.Name.Equals(want, StringComparison.OrdinalIgnoreCase)
                || e.Description.Equals(want, StringComparison.OrdinalIgnoreCase))
            ?? Search(want, 1).FirstOrDefault().Entry;
        if (target == null) return null;

        target.Description = FirstLine(newContent);
        target.Content = newContent.Trim();
        target.UpdatedAt = DateTime.Now;
        WriteEntry(target);
        return target;
    }

    /// <summary>自动识别知识类别：含代码特征 → code，否则默认 habit。</summary>
    static string DetectKind(string content)
    {
        var c = content.Trim();
        if (c.Contains("```") || c.Contains("=>") || c.Contains(';') && c.Contains('(') && c.Contains(')'))
            return "code";
        foreach (var prefix in new[] { "class ", "public ", "private ", "def ", "func ", "function ", "using ", "import ", "const ", "let ", "var " })
            if (c.StartsWith(prefix, StringComparison.Ordinal)) return "code";
        return "habit";
    }

    /// <summary>按文本搜索知识库（/mind search），TF-IDF 匹配降序。</summary>
    public static List<(KbEntry Entry, double Score)> Search(string text, int topN = 10)
    {
        var entries = ListEntries();
        if (entries.Count == 0 || string.IsNullOrWhiteSpace(text)) return [];
        var docs = entries.Select((e, i) => new SemanticMemory.MemoryDocument
        {
            Title = $"{e.Name} {e.Description}".Trim(),
            Content = e.Content,
            Timestamp = e.UpdatedAt,
            Index = i,
        }).ToList();
        var hits = SemanticMemory.SearchRelevant(docs, text, topN);
        return hits.Select(h => (entries[h.Doc.Index], h.Score)).ToList();
    }

    /// <summary>删除与文本最匹配的一条（name/description 精确优先，否则 TF-IDF 最佳）。返回被删条目或 null。</summary>
    public static KbEntry? DeleteBestMatch(string text)
    {
        var entries = ListEntries();
        if (entries.Count == 0) return null;
        var want = text.Trim();
        var exact = entries.FirstOrDefault(e =>
            e.Name.Equals(want, StringComparison.OrdinalIgnoreCase)
            || e.Description.Equals(want, StringComparison.OrdinalIgnoreCase));
        if (exact != null) { DeleteEntry(exact.Name); return exact; }

        var hit = Search(want, 1).FirstOrDefault();
        if (hit.Entry == null) return null;
        DeleteEntry(hit.Entry.Name);
        return hit.Entry;
    }

    /// <summary>分类中文标签。</summary>
    public static string KindLabel(string kind) => kind switch
    {
        "mistake" => "错误",
        "bugfix" => "修复",
        "habit" => "习惯",
        "gap" => "欠缺",
        "code" => "片段",
        _ => kind,
    };

    static string FirstLine(string s)
    {
        var nl = (s ?? "").IndexOf('\n');
        var line = nl >= 0 ? s[..nl] : s;
        return ContextManager.TruncateByRunes(line.Trim(), 60);
    }

    // ═══════════════════════════════════════════════════════════════
    // /kb review —— 间隔重复自测
    // ═══════════════════════════════════════════════════════════════

    /// <summary>一条复习调度状态。</summary>
    public class ReviewItem
    {
        public string Name = "";
        public int ReviewCount;
        public DateTime LastReview = DateTime.MinValue;
        public DateTime NextDue = DateTime.MinValue;
        public int IntervalDays = 1;
        public double Weight = 1.0; // gap 条目权重（未掌握时提升）
    }

    /// <summary>读取复习调度状态（无文件返回空列表）。</summary>
    public static List<ReviewItem> LoadReviewState()
    {
        var list = new List<ReviewItem>();
        if (!File.Exists(StatePath)) return list;
        try
        {
            var root = Json.Parse(File.ReadAllText(StatePath));
            if (root?.Kind != JKind.Object) return list;
            foreach (var n in root["items"]?.Items ?? [])
            {
                list.Add(new ReviewItem
                {
                    Name = n["name"]?.AsString() ?? "",
                    ReviewCount = (int)(n["reviewCount"]?.AsNumber() ?? 0),
                    IntervalDays = (int)(n["intervalDays"]?.AsNumber() ?? 1),
                    NextDue = ParseDateTime(n["nextDue"]?.AsString()),
                    Weight = n["weight"]?.AsNumber() ?? 1.0,
                });
            }
        }
        catch { }
        return list;
    }

    /// <summary>保存复习调度状态（原子写）。</summary>
    public static void SaveReviewState(List<ReviewItem> items)
    {
        EnsureDir();
        var arr = JNode.Array();
        foreach (var i in items)
        {
            arr.Add(JNode.Object()
                .Set("name", i.Name)
                .Set("reviewCount", i.ReviewCount)
                .Set("intervalDays", i.IntervalDays)
                .Set("nextDue", i.NextDue == DateTime.MinValue ? "" : i.NextDue.ToString("yyyy-MM-dd HH:mm:ss"))
                .Set("weight", i.Weight));
        }
        var root = JNode.Object().Set("items", arr);
        var tmp = StatePath + ".tmp";
        File.WriteAllText(tmp, root.ToJson(indent: true));
        File.Move(tmp, StatePath, overwrite: true);
    }

    /// <summary>
    /// 挑出最早到期待复习的条目（未复习过 = 立即到期）。优先 mistake/bugfix。
    /// 无到期返回 null。
    /// </summary>
    public static KbEntry? PickNextDue(List<KbEntry> entries)
    {
        var state = LoadReviewState();
        var now = DateTime.Now;
        KbEntry? best = null;
        var bestDue = DateTime.MaxValue;
        foreach (var e in entries)
        {
            var item = state.FirstOrDefault(i => i.Name == e.Name);
            var due = item?.NextDue ?? DateTime.MinValue;
            if (due > now) continue; // 未到期
            bool high = e.Kind is "mistake" or "bugfix";
            bool bestHigh = best != null && best.Kind is "mistake" or "bugfix";
            if (best == null || (high && !bestHigh) || (high == bestHigh && due < bestDue))
            {
                best = e;
                bestDue = due;
            }
        }
        return best;
    }

    /// <summary>
    /// 记录一次复习结果。掌握 → 间隔 1→3→7→14→30 天递增；
    /// 未掌握 → 间隔重置 1 天，且提升关联 gap 条目权重（薄弱信号）。
    /// </summary>
    public static void MarkReview(string name, bool mastered, string kind, List<string> tags)
    {
        var state = LoadReviewState();
        var item = state.FirstOrDefault(i => i.Name == name);
        if (item == null)
        {
            item = new ReviewItem { Name = name, IntervalDays = 1, NextDue = DateTime.Now, Weight = 1.0 };
            state.Add(item);
        }

        item.ReviewCount++;
        item.LastReview = DateTime.Now;
        if (mastered)
        {
            item.IntervalDays = ReviewIntervals[Math.Min(item.ReviewCount, ReviewIntervals.Length - 1)];
            item.NextDue = DateTime.Now.AddDays(item.IntervalDays);
        }
        else
        {
            item.IntervalDays = 1;
            item.NextDue = DateTime.Now.AddDays(1);
            BoostGaps(state, name, kind, tags);
        }
        SaveReviewState(state);
    }

    /// <summary>未掌握 → 本条若是 gap 自增权重；否则提升与其 tags 关联的 gap 条目权重。</summary>
    static void BoostGaps(List<ReviewItem> state, string name, string kind, List<string> tags)
    {
        if (kind == "gap")
        {
            var it = state.FirstOrDefault(i => i.Name == name);
            if (it != null) it.Weight += 0.5;
            return;
        }

        var reviewTags = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);
        foreach (var g in ListEntries().Where(e => e.Kind == "gap"))
        {
            bool linked = reviewTags.Contains(g.Name) || g.Tags.Any(t => reviewTags.Contains(t));
            if (!linked) continue;
            var it = state.FirstOrDefault(i => i.Name == g.Name);
            if (it == null) { it = new ReviewItem { Name = g.Name, Weight = 1.0 }; state.Add(it); }
            it.Weight += 0.5;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // /kb weak —— 欠缺知识清单 + 薄弱区统计
    // ═══════════════════════════════════════════════════════════════

    /// <summary>薄弱点报告。</summary>
    public class WeakReport
    {
        public List<(string Name, string Description, double Weight)> Gaps = [];
        public List<(string Tag, int Count)> WeakTags = [];
        public List<(string Source, int Count)> ErrorSignals = [];
    }

    /// <summary>
    /// 统计薄弱点：① gap 条目（欠缺知识清单，按权重降序）；② mistake/bugfix 的 tags 聚合；
    /// ③ ErrorLog 的 [ERROR]/[FATAL] 按 source 计数。logDirOverride 供测试注入。
    /// </summary>
    public static WeakReport WeakStats(string? logDirOverride = null)
    {
        var entries = ListEntries();
        var state = LoadReviewState();
        var report = new WeakReport();

        foreach (var g in entries.Where(e => e.Kind == "gap"))
            report.Gaps.Add((g.Name, g.Description, state.FirstOrDefault(i => i.Name == g.Name)?.Weight ?? 1.0));
        report.Gaps.Sort((a, b) => b.Weight.CompareTo(a.Weight));

        report.WeakTags = entries.Where(e => e.Kind is "mistake" or "bugfix")
            .SelectMany(e => e.Tags)
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Tag: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        report.ErrorSignals = ErrorLogSignals(logDirOverride);
        return report;
    }

    /// <summary>扫 ErrorLog 的 [ERROR]/[FATAL] 行，按 [source] 聚合计数（取最新 3 个日志文件）。</summary>
    public static List<(string Source, int Count)> ErrorLogSignals(string? logDirOverride = null)
    {
        var logDir = logDirOverride ?? Path.Combine(Directory.GetCurrentDirectory(), ErrorLog.LogDirName);
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(logDir)) return [];

        var files = Directory.GetFiles(logDir, "error_*.log").OrderByDescending(f => f).Take(3);
        foreach (var f in files)
        {
            try
            {
                foreach (var line in File.ReadAllLines(f, Encoding.UTF8))
                {
                    if (!line.Contains("[ERROR]", StringComparison.Ordinal) && !line.Contains("[FATAL]", StringComparison.Ordinal))
                        continue;
                    var source = ExtractLogSource(line);
                    if (source.Length > 0)
                        counts[source] = counts.GetValueOrDefault(source) + 1;
                }
            }
            catch { }
        }
        return counts.OrderByDescending(kv => kv.Value).Select(kv => (kv.Key, kv.Value)).ToList();
    }

    /// <summary>从「[时间] [级别] [source] message」提取 source（第 3 个括号块）。</summary>
    static string ExtractLogSource(string line)
    {
        var blocks = new List<string>();
        int i = 0;
        while (i < line.Length)
        {
            int open = line.IndexOf('[', i);
            if (open < 0) break;
            int close = line.IndexOf(']', open);
            if (close < 0) break;
            blocks.Add(line[(open + 1)..close]);
            i = close + 1;
        }
        return blocks.Count >= 3 ? blocks[2].Trim() : "";
    }

    /// <summary>复习问题侧（现象 + 根因），用于间隔重复自测的提问。</summary>
    public static string QuizQuestion(KbEntry e)
        => ExtractContentBlocks(e.Content, ["**现象**", "**根因**"], e.Description);

    /// <summary>复习答案侧（修复 + 教训），提问后揭示。</summary>
    public static string QuizAnswer(KbEntry e)
        => ExtractContentBlocks(e.Content, ["**修复**", "**教训**"], e.Content);

    static string ExtractContentBlocks(string content, string[] markers, string fallback)
    {
        var sb = new StringBuilder();
        foreach (var line in content.Split('\n'))
        {
            foreach (var m in markers)
                if (line.StartsWith(m, StringComparison.Ordinal)) { sb.AppendLine(line); break; }
        }
        return sb.Length > 0 ? sb.ToString().Trim() : fallback;
    }

    // ═══════════════════════════════════════════════════════════════
    // 检索（SystemPrompt 启动注入用）
    // ═══════════════════════════════════════════════════════════════

    /// <summary>按 query 取最相关的经验条目文本（复用 SemanticMemory TF-IDF），供 SystemPrompt 注入。</summary>
    public static string GetRelevant(string query, int topN = 5, int maxPreview = 160)
    {
        var entries = ListEntries();
        if (entries.Count == 0 || string.IsNullOrWhiteSpace(query)) return "";
        if (topN <= 0) return "";

        var docs = new List<SemanticMemory.MemoryDocument>();
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            docs.Add(new SemanticMemory.MemoryDocument
            {
                Title = $"{e.Name} {e.Description}".Trim(),
                Content = e.Content,
                Timestamp = e.UpdatedAt,
                Index = i,
            });
        }

        var hits = SemanticMemory.SearchRelevant(docs, query, topN);
        if (hits.Count == 0) return "";

        var sb = new StringBuilder();
        sb.AppendLine("# 经验知识（自动匹配）");
        foreach (var (doc, score) in hits)
        {
            var preview = ContextManager.TruncateByRunes(doc.Content, maxPreview);
            sb.AppendLine($"- **{doc.Title}** (相关度 {score:F2}): {preview.ReplaceLineEndings(" ")}");
        }
        return sb.ToString();
    }

    static DateTime ParseDateTime(string? s)
        => DateTime.TryParse(s, out var d) ? d : DateTime.MinValue;
}
