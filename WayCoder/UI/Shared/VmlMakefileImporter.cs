using System.Text;

namespace WayCoder.UI.Shared;

/// <summary>
/// **简单 Makefile → <see cref="VmlProject"/>** 的一次性导入。
///
/// <para>
/// ## 为什么是"一次性"而不是"构建时动态读"
///
/// `.vmk` 生成之后**以它为准**，Makefile 不再参与后续构建。两处真源必然漂移，
/// 而漂移的症状是"改了 Makefile 却不生效" —— 那种问题查起来最费劲（两边看着都对）。
/// </para>
///
/// <para>
/// ## 只认**手写的那种** Makefile
///
/// 目标是 DOS/Unix 时代老程序常见的那一shape：
/// </para>
///
/// <code>
/// CC      = gcc
/// CFLAGS  = -O2 -Wall -DVERSION=\"1.2\" -Iinclude
/// LIBS    = -lm
/// SRCS    = src/main.c src/util.c
/// OBJS    = $(SRCS:.c=.o)
///
/// prog: $(OBJS)
/// 	$(CC) $(CFLAGS) -o prog $(OBJS) $(LIBS)
/// </code>
///
/// <para>
/// **明确不认**（<b>报错说明原因，不糊弄</b>）：
/// </para>
/// <list type="bullet">
/// <item><b>纯转发式</b> <c>all: ; $(MAKE) -C sub</c> —— 真正的构建描述在子目录里，
/// 我们看不见，导出来只会是空清单；</item>
/// <item><b>IDE 自动生成的 .mk</b>（CubeIDE 的 <c>sources.mk</c>、<c>-include</c> 递归依赖）
/// —— 机器生成的，结构完全另一套。</item>
/// </list>
///
/// <para>
/// ## ⚠ 多个源文件**必须报出来**
///
/// VML **没有跨编译单元的链接**（`CompilerProgramBase` 的多文件模式明确拒绝 `-o`，
/// 只是把每个 `.c` 各编成同名的 `.vml`）。所以导入器只能取**一个**入口 ——
/// 而"静默取第一个"的后果是**编过了、少了半个程序**，正是本仓最怕的失败形状。
/// 发现多个 `.c` 时要在 <see cref="ProjectImportResult.Notes"/> 里说清楚。
/// </para>
/// </summary>
public sealed class MakefileImporter : IProjectImporter
{
    /// <inheritdoc/>
    public string Name => "make";

    /// <summary>
    /// 认 `Makefile` / `makefile` / `GNUmakefile` / `*.mk`。
    ///
    /// ⚠ `*.mk` 里有一大半是 **IDE 自动生成**的（CubeIDE 的 `sources.mk` 那种）——
    /// 那些会在 <see cref="Import"/> 里被 `-include` 那条判据**拒掉并说明原因**，
    /// 所以这里可以放心地按扩展名收。
    /// </summary>
    public bool CanHandle(string fileName)
    {
        var n = Path.GetFileName(fileName);
        return n.Equals("Makefile", StringComparison.OrdinalIgnoreCase)
            || n.Equals("GNUmakefile", StringComparison.OrdinalIgnoreCase)
            || n.EndsWith(".mk", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>从 Makefile 导入。<paramref name="entryHint"/> 可指定入口（默认自动找含 main 的）。</summary>
    public ProjectImportResult Import(string makefilePath, string? entryHint = null)
    {
        var full = Path.GetFullPath(makefilePath);
        if (!File.Exists(full))
            throw new VmlProjectException($"找不到 Makefile：{full}");

        var baseDir = Path.GetDirectoryName(full) ?? ".";
        var text = File.ReadAllText(full);
        var notes = new List<string>();

        // ── ① 先做"不认的形态"的拒绝（在解析之前，免得绕一圈才报）──
        if (text.Contains("$(MAKE)", StringComparison.Ordinal)
            || text.Contains("${MAKE}", StringComparison.Ordinal))
            throw new VmlProjectException(
                "这个 Makefile 是**转发式**的（里面用了 `$(MAKE)` 去调子目录）—— "
                + "真正的构建描述在子目录里，导入出来只会是空清单。\n"
                + "   请在**子目录**那个 Makefile 上再跑一次 --import。");

        foreach (var line in Lines(text))
        {
            if (line.StartsWith("-include", StringComparison.Ordinal)
                || line.StartsWith("sinclude", StringComparison.Ordinal))
                throw new VmlProjectException(
                    "这个 Makefile 用了 `-include` 递归拉依赖（IDE 自动生成的 `.mk` 常这样）—— "
                    + "那是机器生成的结构，不是手写的构建描述，导入器不认。\n"
                    + "   如果入口源文件明确，可以直接手写一个 .vmk（见 docs/VML工程文件.md）。");
        }

        // ── ② 变量 + 规则 ──
        var vars = new Dictionary<string, string>(StringComparer.Ordinal);
        var recipes = new List<string>();          // 所有命令行（用来找 -D/-I 与源文件）
        var prereqs = new List<string>();          // 所有规则的依赖（同上）

        string? currentTarget = null;
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (raw.Length == 0) continue;

            // 配方行（以 TAB 开头）—— 归给当前规则
            if (raw[0] == '\t')
            {
                var cmd = StripComment(raw).Trim();
                if (cmd.Length > 0) recipes.Add(cmd);
                continue;
            }

            var line = StripComment(raw).Trim();
            if (line.Length == 0) continue;

            int eq = IndexOfAssignment(line);
            if (eq > 0 && (currentTarget is null || !LooksLikeRule(line)))
            {
                var name = line[..eq].Trim();
                var value = line[(eq + (line[eq] == ':' || line[eq] == '?' || line[eq] == '+' ? 2 : 1))..].Trim();
                vars[name] = value;
                continue;
            }

            if (LooksLikeRule(line))
            {
                var colon = line.IndexOf(':');
                currentTarget = line[..colon].Trim();
                var deps = line[(colon + 1)..].Trim();
                if (deps.Length > 0) prereqs.Add(deps);
            }
        }

        // 变量展开**只做一层**（见类注释：递归展开显式拒绝）
        var expanded = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (k, v) in vars) expanded[k] = ExpandOnce(v, vars, k);

        // ── ③ 找源文件 ──
        var candidates = new List<string>();
        foreach (var key in new[] { "SRCS", "SOURCES", "OBJS", "OBJECTS", "SRC" })
        {
            if (!expanded.TryGetValue(key, out var v)) continue;
            foreach (var tok in SplitTokens(v)) AddSourceCandidate(tok, candidates);
        }
        foreach (var p in prereqs)
            foreach (var tok in SplitTokens(ExpandOnce(p, vars, null)))
                AddSourceCandidate(tok, candidates);

        // 命令行里直接写死源文件的形态（`gcc -o prog main.c`）也要认
        foreach (var cmd in recipes)
            foreach (var tok in SplitTokens(ExpandOnce(cmd, vars, null)))
                AddSourceCandidate(tok, candidates);

        // 去重（保序）+ 只留**本目录下真实存在**的
        var sources = candidates
            .Select(c => Path.GetFullPath(Path.Combine(baseDir, c)))
            .Where(File.Exists)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (sources.Count == 0)
            throw new VmlProjectException(
                "没能从这个 Makefile 里找出任何源文件。\n"
                + "   认得的写法是 `SRCS = a.c b.c`、`OBJS = a.o b.o`（.o 反推 .c）、"
                + "或在规则/命令行里直接写 `xxx.c`。");

        // ── ④ 挑入口 ──
        string entry;
        if (!string.IsNullOrWhiteSpace(entryHint))
        {
            var hint = Path.GetFullPath(Path.Combine(baseDir, entryHint));
            if (!sources.Contains(hint))
                throw new VmlProjectException(
                    $"指定的入口 `{entryHint}` 不在这个 Makefile 的源文件清单里：\n"
                    + string.Join("\n", sources.Select(s => "     " + Rel(baseDir, s))));
            entry = hint;
        }
        else
        {
            var withMain = sources.Where(HasMain).ToList();
            entry = withMain.Count > 0 ? withMain[0] : sources[0];
        }

        // ⚠ 多个源文件**必须报出来**（见类注释）
        var others = sources.Where(s => !string.Equals(s, entry, StringComparison.Ordinal)).ToList();
        if (others.Count > 0)
        {
            notes.Add(
                $"这个 Makefile 有 {sources.Count} 个源文件，但 VML **没有跨文件的链接** —— "
                + $"只取了入口 `{Rel(baseDir, entry)}`。\n"
                + "      其余这些的符号**不会**进程序：\n"
                + string.Join("\n", others.Select(o => "        " + Rel(baseDir, o)))
                + "\n      要它们进来，只能改成 `#include \"xxx.c\"`（老 C 的单编译单元写法），"
                + "或者手工合并成一个文件。");
        }

        // ── ⑤ 抽 -D / -I（从所有变量值与命令行里扫）──
        var defines = new List<(string, string)>();
        var includes = new List<string>();
        var flagSources = new List<string>();
        foreach (var key in new[] { "CFLAGS", "CXXFLAGS", "CPPFLAGS", "DEFS", "INCLUDES", "CPPFLAGS" })
            if (expanded.TryGetValue(key, out var v)) flagSources.Add(ExpandOnce(v, vars, key));
        flagSources.AddRange(recipes.Select(c => ExpandOnce(c, vars, null)));

        foreach (var src in flagSources)
            ScanFlags(src, baseDir, defines, includes);

        // ── ⑥ 组装 ──
        var project = new VmlProject
        {
            Name = Path.GetFileName(baseDir.TrimEnd('/', '\\')),
            Entry = Rel(baseDir, entry),
        };
        foreach (var d in includes) project.Includes.Add(d);
        foreach (var (n, v) in defines) project.Defines.Add((n, v));

        if (project.Name.Length == 0) project.Name = "project";
        return new ProjectImportResult(project, notes);
    }

    // ═══════════════════════════════════════════════════════════════
    // 内部
    // ═══════════════════════════════════════════════════════════════

    private static IEnumerable<string> Lines(string text) =>
        text.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim());

    /// <summary>去掉 `#` 注释（不处理引号里的 `#` —— 手写 Makefile 里极少见，宁可简单）。</summary>
    private static string StripComment(string line)
    {
        int i = line.IndexOf('#');
        return i < 0 ? line : line[..i];
    }

    /// <summary>`NAME = …` / `NAME := …` / `NAME ?= …` / `NAME += …` 的位置（0 基）。找不到返回 -1。</summary>
    private static int IndexOfAssignment(string line)
    {
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '=') return i;
            if ((c == ':' || c == '?' || c == '+') && i + 1 < line.Length && line[i + 1] == '=') return i;
            // 赋值号左边的名字只能是字母数字下划线/点 —— 碰到别的（`target: dep`）就停
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '.' && c != ' ' && c != '\t') return -1;
        }
        return -1;
    }

    private static bool LooksLikeRule(string line)
    {
        int c = line.IndexOf(':');
        if (c <= 0) return false;
        // 排除 `C:\...` 这种 Windows 路径与 `:=`
        return !(c + 1 < line.Length && line[c + 1] == '=');
    }

    /// <summary>
    /// 变量展开**一层**。支持 `$(VAR)` 与 `$(VAR:.c=.o)`（后缀替换）；
    /// 认不出的形式**原样留着**——宁可后面匹配不到，也不猜。
    /// </summary>
    private static string ExpandOnce(string value, Dictionary<string, string> vars, string? self)
    {
        var sb = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] != '$') { sb.Append(value[i]); continue; }

            if (i + 1 < value.Length && value[i + 1] == '$') { sb.Append('$'); i++; continue; }
            if (i + 1 >= value.Length || value[i + 1] != '(') { sb.Append(value[i]); continue; }

            int close = value.IndexOf(')', i + 2);
            if (close < 0) { sb.Append(value[i]); continue; }

            var inner = value[(i + 2)..close];
            i = close;

            int colon = inner.IndexOf(':');
            var name = (colon < 0 ? inner : inner[..colon]).Trim();

            // 自引用（`CFLAGS = $(CFLAGS) -g`）不展开，免得无限套
            if (self is not null && name == self) { sb.Append("$(").Append(inner).Append(')'); continue; }

            if (!vars.TryGetValue(name, out var v)) { sb.Append("$(").Append(inner).Append(')'); continue; }

            if (colon >= 0)
            {
                // `$(SRCS:.c=.o)` —— 只做**一次**后缀替换
                var spec = inner[(colon + 1)..];
                int eq = spec.IndexOf('=');
                if (eq > 0)
                {
                    var from = spec[..eq].Trim();
                    var to = spec[(eq + 1)..].Trim();
                    sb.Append(string.Join(' ', SplitTokens(v).Select(t =>
                        t.EndsWith(from, StringComparison.Ordinal) ? t[..^from.Length] + to : t)));
                    continue;
                }
            }
            sb.Append(v);
        }
        return sb.ToString();
    }

    private static IEnumerable<string> SplitTokens(string s) =>
        s.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>把 `.c`/`.o` 记号收进候选（`.o` 反推 `.c`）。</summary>
    private static void AddSourceCandidate(string token, List<string> into)
    {
        var t = token.Trim().Trim('"', '\'');
        if (t.Length == 0 || t.StartsWith('-') || t.StartsWith('$')) return;

        if (t.EndsWith(".c", StringComparison.OrdinalIgnoreCase)) into.Add(t);
        else if (t.EndsWith(".o", StringComparison.OrdinalIgnoreCase)) into.Add(t[..^2] + ".c");
    }

    /// <summary>文件里有没有 `main`（粗判：整词 `main` 后面跟 `(`）。</summary>
    private static bool HasMain(string path)
    {
        try
        {
            var text = File.ReadAllText(path);
            for (int i = 0; ; )
            {
                int k = text.IndexOf("main", i, StringComparison.Ordinal);
                if (k < 0) return false;
                bool leftOk = k == 0 || !(char.IsLetterOrDigit(text[k - 1]) || text[k - 1] == '_');
                int after = k + 4;
                if (leftOk && after < text.Length && text[after] == '(') return true;
                i = k + 1;
            }
        }
        catch { return false; }
    }

    /// <summary>从一段文字里扫 `-D` / `-I`（含 `-I dir` 分开写的形态）。</summary>
    private static void ScanFlags(string text, string baseDir,
        List<(string, string)> defines, List<string> includes)
    {
        var toks = SplitTokens(text).ToList();
        for (int i = 0; i < toks.Count; i++)
        {
            var t = toks[i];

            if (t.StartsWith("-D", StringComparison.Ordinal))
            {
                var body = t[2..];
                if (body.Length == 0 && i + 1 < toks.Count) { body = toks[++i]; }
                if (body.Length == 0) continue;
                int eq = body.IndexOf('=');
                var name = (eq < 0 ? body : body[..eq]).Trim();
                var val = eq < 0 ? "1" : body[(eq + 1)..].Trim();
                if (name.Length == 0) continue;

                // ⚠ 引号有两种，**处理方式相反**，顺序不能换：
                //
                //   · **没转义**的引号会被 shell 吃掉 —— Makefile 里写 `-DFOO="bar"`，
                //     交给 shell 执行时 gcc 收到的是 `-DFOO=bar` ⇒ 要**剥掉**；
                //   · **转义**的引号是**值的一部分** —— `-DVERSION=\"1.2\"` 到 gcc 手里是
                //     `-DVERSION="1.2"`，VERSION 展开成**字符串字面量** `"1.2"`
                //     （老程序 `puts("v" VERSION)` 正是靠这个）⇒ 要**还原成 `"`、保留**。
                //
                // 先剥裸引号、再还原转义引号。反过来的话 `\"1.2\"` 会被先还原成 `"1.2"`、
                // 又被当成裸引号剥掉，变成一个光秃秃的 `1.2` —— 编译时就成了
                // `puts("v" 1.2)`，报错位置离 `-D` 十万八千里。
                if (val.Length >= 2 && val[0] == '"' && val[^1] == '"') val = val[1..^1];
                val = val.Replace("\\\"", "\"").Replace("\\'", "'");
                if (val.Length == 0) val = "1";

                if (!defines.Any(d => d.Item1 == name)) defines.Add((name, val));
            }
            else if (t.StartsWith("-I", StringComparison.Ordinal))
            {
                var dir = t[2..];
                if (dir.Length == 0 && i + 1 < toks.Count) dir = toks[++i];
                dir = dir.Trim('"', '\'');
                if (dir.Length == 0) continue;
                if (dir.StartsWith("$", StringComparison.Ordinal)) continue;   // 没展开的，跳过

                // 存**相对 .vmk 的**路径（`.vmk` 与 Makefile 同目录）
                var abs = Path.GetFullPath(Path.Combine(baseDir, dir));
                var rel = Rel(baseDir, abs);
                if (!includes.Contains(rel, StringComparer.Ordinal)) includes.Add(rel);
            }
        }
    }

    /// <summary>
    /// 相对 <paramref name="baseDir"/> 的路径（做不到就返回原样的绝对路径）。
    ///
    /// ⚠ **分隔符一律转成正斜杠**再存进 `.vmk`。工程文件是**跨平台**的 ——
    /// Windows 上生成的 `.vmk` 要能在 macOS / 安卓上打开，而 `Path.GetRelativePath`
    /// 会吐 `src\main.c`；那个字符串在 Unix 上 `Path.Combine` 出来是一个
    /// **名字里带反斜杠的文件**，根本打不开（不是报错，是"文件不存在"）。
    /// 读回来时 <see cref="VmlProject"/> 会再转一次，两边各管一头。
    /// </summary>
    private static string Rel(string baseDir, string path)
    {
        try
        {
            var r = Path.GetRelativePath(baseDir, path);
            if (r.Length == 0) return ".";
            return r.Replace('\\', '/');
        }
        catch { return path.Replace('\\', '/'); }
    }
}
