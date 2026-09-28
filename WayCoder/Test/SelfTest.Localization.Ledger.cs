using System.Text;
using System.Text.RegularExpressions;

namespace WayCoder;

/// <summary>
/// 双语化的**防漏翻台账** —— 「分批推进」的可执行形态。
///
/// <para>
/// 问题：`WayCoder.Maui/` 有约 70 个文件含中文、2000+ 条文案，一批翻不完。而
/// **没有台账的批量迁移必然烂尾** —— 翻过的和没翻的混在一起，谁也说不清还剩多少，
/// 新写的中文也无人拦。这正是本仓那次 `.resx` 尝试栽掉的原因（表建好了、键没跟上、
/// 静默回退成 `[键名]` 显示给用户，而没有任何一道闸门发现）。
/// </para>
///
/// <para>
/// 机制：仓库里放一份台账（<c>WayCoder/Test/i18n-maui-ledger.txt</c>，一行一个相对路径），
/// 内容是「**已知仍含未迁移中文的文件**」。两条判据，**两个方向都会红**：
/// </para>
/// <list type="bullet">
/// <item>**文件有中文但不在台账里** ⇒ 红。这是新写的中文，或新文件 —— 正是要拦的那类。</item>
/// <item>**文件在台账里但已经零命中** ⇒ 也红。台账腐烂比台账不全更坏：它会让"还剩多少"永远算不准，
///       于是这道闸门迟早被当成噪音绕过。</item>
/// </list>
///
/// <para>
/// ⚠ <b>台账的边界（说清楚，免得当成"全都管了"）</b>：本轮只扫
/// <c>WayCoder.Maui/**/*.{cs,xaml}</c>，也就是**手机界面自己的文案**。
/// 编译进手机的共享层（<c>WayCoder/Tools/**</c>、<c>WayCoder/Agent/**</c>、
/// <c>WayCoder/UI/Shared/**</c>）**不在**这份台账里 —— 那些是 Batch 2 的范围
/// （工具错误/警告文案收敛到 <c>ToolErrors</c>），届时另立一份台账。
/// 平台清单（<c>Platforms/**</c>）也不扫：那里的中文是注释与 zh 资源，**本就该是中文**。
/// </para>
/// </summary>
public static partial class SelfTest
{
    /// <summary>
    /// 一份台账的范围定义。**每加一层就加一个 Scope，判定逻辑不复制** ——
    /// 本会话已经为「同一规则两处实现」栽过太多次，台账这种"判据本身"的东西尤其不能有两份。
    /// </summary>
    /// <param name="Title">Section 标题（出现在测试输出里）。</param>
    /// <param name="LedgerRelPath">台账文件（相对仓库根）。</param>
    /// <param name="ScanRootRelPath">扫描起点（相对仓库根）。</param>
    /// <param name="ExcludeSegments">路径里出现这些片段就跳过（大小写不敏感，带前后斜杠）。</param>
    /// <param name="ScanXaml">是否也扫 `.xaml`（只有 MAUI 侧有界面标记）。</param>
    private sealed record LedgerScope(
        string Title, string LedgerRelPath, string ScanRootRelPath,
        string[] ExcludeSegments, bool ScanXaml, string Note,
        string[]? IncludeOverrides = null);

    /// <summary>MAUI 侧：手机界面自己的文案（Batch 3 已收尾，剩下的是有意保留）。</summary>
    private static readonly LedgerScope MauiScope = new(
        "双语化台账：MAUI 侧仍含中文的文件（分批推进的可执行形态）",
        "WayCoder/Test/i18n-maui-ledger.txt",
        "WayCoder.Maui",
        // obj/bin = 生成代码；Platforms = 平台清单（那里的中文是注释与 zh 资源，本就该是中文）
        ["/obj/", "/bin/", "/Platforms/"],
        ScanXaml: true,
        Note: "台账内剩下的全是**有意保留**的（日志/判据/存储值/#if DEBUG/图标字形），每行都有理由。");

    /// <summary>
    /// 共享层：**MAUI 真正编译到**的那部分 `WayCoder/**`（Batch 2）。
    ///
    /// <para>
    /// ⚠ 这个范围**必须与 `WayCoder.Maui.csproj` 的 `Compile Include/Exclude` 一致** ——
    /// 第一版只按"看起来像桌面外壳"排除了 `UI/TUI/`、`UI/WEB/`、`Config/`，
    /// 结果两头都错，而且**两个错都是子智能体读代码时发现的**（不是靠人眼）：
    /// </para>
    /// <list type="bullet">
    /// <item><b>漏排除</b>：`Program*.cs`、`UI/CLI/Arguments/**`、`Batch/**`、`Watch/**` 等
    ///   MAUI **明确不编**，它们混进台账会让"还剩多少"失去意义（手机上看不见的和看得见的混在一起数）。</item>
    /// <item><b>误排除</b>：`UI/TUI/Edit/**` 被 csproj **显式加回**（编辑器数据核心），
    ///   `Config/Global.cs` 的 `AppFullName` 会显示在 `/about` 里 ⇒ 前者成了"手机英文界面下真的坏掉、
    ///   台账却不红"的**盲区**（实测 `DiagnosticManager` 的三条守卫就是这么漏的）。</item>
    /// </list>
    /// <para>
    /// ⇒ 排除项照抄 csproj；`UI/TUI/Edit/**` 走 `IncludeOverrides` 收回来。
    /// ⚠ **改 csproj 的编译集时要回来同步这里** —— 否则台账会静默地开始撒谎。
    /// </para>
    /// </summary>
    private static readonly LedgerScope SharedScope = new(
        "双语化台账：共享层（MAUI 实际编译的 WayCoder/**）仍含中文的文件",
        "WayCoder/Test/i18n-shared-ledger.txt",
        "WayCoder",
        [
            // 目录级：照抄 WayCoder.Maui.csproj 的 Exclude
            "/obj/", "/bin/", "/Test/", "/UI/CLI/Arguments/", "/Plugins/", "/Properties/",
            "/WayEngine/", "/game/", "/games/", "/lottery/", "/subs/", "/works/", "/SnakeGame/",
            "/UI/WEB/", "/UI/TUI/", "/Batch/", "/Watch/", "/demo/",
            // 单文件级（csproj 里逐个数出来的；用 `/` 前后包住避免误伤同名前缀）
            "/Program.cs", "/Program.Repl.cs", "/Program.Output.cs", "/Program.Commands.cs",
            "/git/GitRunner.cs", "/Config/ThemeConfig.cs", "/Agent/AgentSlot.cs",
            "/Tools/ToolRegistry.cs", "/Tools/BashTool.cs", "/Tools/GitTool.cs", "/Tools/GitPRTool.cs",
            "/Tools/LspTool.cs", "/Tools/LintTool.cs", "/Tools/PsTool.cs", "/Tools/KillTool.cs",
            "/Tools/ScreenshotTool.cs", "/Tools/SqliteTool.cs", "/Tools/TestTool.cs",
            "/Tools/JobOutputTool.cs", "/Tools/JobKillTool.cs",
            "/UI/CLI/Commands/SyncQrCommand.cs",
            // ⚠ `Config/` 其余部分**被 MAUI 消费**（`Global.cs` 的 AppFullName 显示在 /about）⇒ 不排除。
        ],
        ScanXaml: false,
        Note: "共享层的文案手机与桌面共用 —— 手机上工具输出/报错直接显示给用户。",
        // MAUI 显式加回的那一块（csproj：`<Compile Include="../WayCoder/UI/TUI/Edit/**/*.cs" />`）
        IncludeOverrides: ["/UI/TUI/Edit/"]);

    /// <summary>
    /// 从仓库里扫出「含未迁移中文的文件」并与台账对账。两个方向都会红。
    /// </summary>
    private static void CheckLedger(LedgerScope scope, Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section(scope.Title);

        // 找仓库根 —— 打包/发布产物里没有源码，那时跳过（与 Chunk19 的 plist 护栏同一处置）
        string? root = null;
        for (var d = new DirectoryInfo(Directory.GetCurrentDirectory()); d != null; d = d.Parent)
        {
            if (Directory.Exists(Path.Combine(d.FullName, "WayCoder.Maui")) &&
                File.Exists(Path.Combine(d.FullName, "WayCoder", "WayCoder.csproj")))
            { root = d.FullName; break; }
        }
        if (root == null)
        {
            Check("台账: 无源码目录（打包环境），跳过", true);
            return;
        }

        var scanRoot = Path.Combine(root, scope.ScanRootRelPath);
        var ledgerPath = Path.Combine(root, scope.LedgerRelPath);
        var displayPrefixLen = scope.ScanRootRelPath.Length + 1;

        // ── 扫描：每个 .cs（MAUI 侧还有 .xaml）里「字符串字面量/属性值」含 CJK 的条数 ──
        var hits = new SortedDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        // 留几个「命中长什么样」的样本：台账报红时**必须能看出命中的是哪一段**，
        // 否则"这个文件怎么会在台账外"只能靠人再去复刻一遍扫描器（实测为这条卡过一轮）。
        var samples = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var scanned = 0;
        foreach (var file in Directory.EnumerateFiles(scanRoot, "*.*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(root, file).Replace('\\', '/');
            var excluded = scope.ExcludeSegments.Any(seg => rel.Contains(seg, StringComparison.OrdinalIgnoreCase));
            // 显式加回：csproj 排除了整个目录、又单独 `<Compile Include>` 回一两块时用（如 UI/TUI/Edit）。
            var reIncluded = scope.IncludeOverrides?.Any(seg => rel.Contains(seg, StringComparison.OrdinalIgnoreCase)) == true;
            if (excluded && !reIncluded) continue;

            var ext = Path.GetExtension(file);
            bool isCs = ext.Equals(".cs", StringComparison.OrdinalIgnoreCase);
            bool isXaml = scope.ScanXaml && ext.Equals(".xaml", StringComparison.OrdinalIgnoreCase);
            if (!isCs && !isXaml) continue;
            scanned++;

            string src;
            try { src = File.ReadAllText(file); } catch { continue; }

            var pieces = isCs ? ExtractCSharpStringLiterals(src) : ExtractXamlTexts(src);
            var bad = pieces.Where(HasCjk).ToList();
            if (bad.Count > 0)
            {
                hits[rel] = bad.Count;
                samples[rel] = bad.Take(3).Select(p => p.Trim()[..Math.Min(60, p.Trim().Length)]).ToList();
            }
        }

        // ── 台账 ──
        var ledger = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(ledgerPath))
            foreach (var line in File.ReadAllLines(ledgerPath))
            {
                var t = line.Trim();
                if (t.Length == 0 || t.StartsWith('#')) continue;
                // 行内注释：`路径   # 保留理由` —— 台账要能**说清为什么**，否则下一个人
                // 只会看到"这个文件怎么还没翻"，然后要么误删、要么把日志也翻了。
                var hash = t.IndexOf("  #", StringComparison.Ordinal);
                if (hash > 0) t = t[..hash].Trim();
                if (t.Length > 0) ledger.Add(t.Replace('\\', '/'));
            }
        else
        {
            Fail($"台账: 找不到 {scope.LedgerRelPath} —— 这份文件是「分批推进」的唯一账本，不能缺");
            return;
        }

        var notInLedger = hits.Keys.Where(k => !ledger.Contains(k)).ToList();
        var stale = ledger.Where(k => !hits.ContainsKey(k)).ToList();

        // ⚠ 名单**不截断**：它是给人照着改台账用的，少列一个就得再跑一轮。
        if (notInLedger.Count > 0)
            Fail($"台账: {notInLedger.Count} 个文件含未迁移中文但**不在台账里**（新写的中文？新文件？）"
                 + "—— 翻掉它，或确认属于尚未排期的批次后加进台账：\n    "
                 + string.Join("\n    ", notInLedger.Select(f =>
                       samples.TryGetValue(f, out var ss) ? $"{f}   ← 命中样本: {string.Join(" ｜ ", ss)}" : f)));
        else
        {
            // ⚠ 进度要**报得出来**，否则「还剩多少」只能靠人翻文件 —— 而那正是台账要消灭的东西。
            //   注意统计的是**未迁移的中文字符串字面量条数**，不是文件里的汉字总数：
            //   本仓注释即文档，汉字总量的九成在注释里（EditorPage.xaml.cs 两万七千汉字，
            //   真正的文案只占零头）。用汉字总数当进度会把它夸大一个数量级。
            var total = hits.Values.Sum();
            var top = hits.OrderByDescending(kv => kv.Value).Take(8)
                          .Select(kv => $"{kv.Key[displayPrefixLen..]} {kv.Value}");
            Check($"台账: 无「有中文但不在台账」的文件（扫了 {scanned} 个源文件；"
                  + $"台账内 {hits.Count} 个文件 / 共 {total} 处仍含中文或全角）。{scope.Note}"
                  + $"｜前 8：{string.Join("、", top)}", true);
        }

        if (stale.Count > 0)
            Fail($"台账: {stale.Count} 个文件**已零命中却还挂在台账上**（台账腐烂会让「还剩多少」永远算不准）"
                 + "—— 请从台账里删掉这些行：\n    "
                 + string.Join("\n    ", stale));
        else
            Check("台账: 无「已翻完却还挂在台账上」的腐烂条目", true);

        // 反方向：台账本身不能是空的（全空 ⇒ 上面第一条判据失去意义，看着绿其实没在管）
        Check("台账: 非空且有实质内容", ledger.Count > 0);
    }

    /// <summary>MAUI 侧台账（含扫描器自检，只在第一个 Scope 里跑一次）。</summary>
    private static void TestMauiChineseLedger(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        TestLedgerScannerItself(Section, Check);
        CheckLedger(MauiScope, Section, Check, Fail);
    }

    /// <summary>共享层台账（Batch 2）。</summary>
    private static void TestSharedChineseLedger(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
        => CheckLedger(SharedScope, Section, Check, Fail);

    /// <summary>
    /// 扫描器**自己**的判据。没有这一段，一个坏掉的扫描器给出的就是**假绿** ——
    /// 而"绿"恰恰是这道闸门唯一的价值。表驱动：左边源码片段，右边该不该判成命中。
    ///
    /// <para>
    /// 正反两个方向都要有：只测"该命中的命中了"，会让一个**恒真**的扫描器（比如从不跳注释）
    /// 全绿通过，而它的实际表现是全量噪音。
    /// </para>
    /// </summary>
    private static void TestLedgerScannerItself(Action<string> Section, Action<string, bool> Check)
    {
        Section("双语化台账：扫描器自身的判据（表驱动，正反两向）");

        var csCases = new (string Name, string Src, bool ShouldHit)[]
        {
            ("普通字符串里的中文 → 命中",            "var a = \"中文\";",                    true),
            ("行注释里的中文 → 不命中",              "var a = 1; // 中文",                   false),
            ("整行注释里的中文 → 不命中",            "// 中文",                              false),
            ("块注释里的中文 → 不命中",              "/* 中文 */ var a = 1;",                false),
            ("多行块注释里的中文 → 不命中",          "/*\n 中文\n*/",                        false),
            ("字符串里的 // 不算注释 → 命中",        "var a = \"// 中文\";",                 true),
            ("字符串里的 /* 不算注释 → 命中",        "var a = \"/* 中文\";",                 true),
            ("L.Pick 的中文支 → 不命中（已双语）",   "var a = L.Pick(\"中文\", \"EN\");",     false),
            ("嵌套 L.Pick 的内层 → 不命中",          "var a = L.Pick(\"x\", L.Pick(\"中文\", \"EN\"));", false),
            ("L.Pick 之后的另一条中文 → 命中",       "var a = L.Pick(\"x\", \"y\"); var b = \"中文\";", true),
            ("逐字字符串 @\"\" → 命中",              "var a = @\"中文\";",                   true),
            ("插值字符串 $\"\" → 命中",              "var a = $\"中文{x}\";",                true),
            ("原始字符串 \"\"\"\"\"\" → 命中",       "var a = \"\"\"中文\"\"\";",            true),
            ("字符字面量 '中' → 不命中",             "var c = '中';",                        false),
            // ── 插值孔（`{…}` 里是**代码**）────────────────────────────────────────
            // ⚠ 这一组是**补一个真实漏报**：按「引号即字面量边界」走的扫描器会在孔内那个引号处
            //   提前收尾，剩下的中文被当成标识符（`char.IsAlpha` 对汉字为真）⇒ 整条串**零命中**。
            //   实测由子智能体发现（它在 `Program.Commands.cs` 踩到），它同时指出：**别的 agent 报
            //   「已翻到 0」的文件未必真是 0** —— 一个会说谎的闸门比没有闸门更糟。
            ("孔里的字面量 → 命中",                  "var a = $\"{\"中文\",-8}\";",           true),
            ("孔里带对齐的多个字面量 → 命中",         "var a = $\"{\"行数\",8}{\"词数\",8}\";", true),
            ("孔里的中文格式串 → 命中",              "var a = $\"{d:yyyy年}\";",              true),
            ("孔里三元里的中文字面量 → 命中",         "var a = $\"{(f ? \"中文\" : \"en\")}\";", true),
            ("孔里只有标识符 → 不命中",              "var a = $\"{name}\";",                  false),
            ("`{{` 是字面量大括号不是孔 → 不命中",    "var a = $\"{{x}}\";",                   false),
            ("孔里嵌插值串的中文 → 命中",            "var a = $\"{$\"内{中文}\"}\";",           true),
            ("只有注释的文件 → 不命中",              "// 中文\n/// 中文\n/* 中文 */",        false),
            ("空文件 → 不命中",                      "",                                     false),
        };
        foreach (var (name, src, should) in csCases)
        {
            var got = ExtractCSharpStringLiterals(src).Any(HasCjk);
            Check($"扫描器[C#]: {name}", got == should);
        }

        var xamlCases = new (string Name, string Src, bool ShouldHit)[]
        {
            ("属性值里的中文 → 命中",                "<Label Text=\"中文\" />",                            true),
            ("标签间文本里的中文 → 命中",            "<Label>中文</Label>",                                true),
            ("XAML 注释里的中文 → 不命中",           "<!-- 中文 -->",                                      false),
            ("Tr 标记扩展 → 不命中（已双语）",       "<Label Text=\"{markup:Tr Zh='中文', En='EN'}\" />",  false),
            ("Tr 之外的属性仍命中",                  "<Label Text=\"{markup:Tr Zh='x', En='y'}\" Title=\"中文\" />", true),
            ("xml 声明不误判 → 不命中",              "<?xml version=\"1.0\" encoding=\"utf-8\" ?>",        false),
        };
        foreach (var (name, src, should) in xamlCases)
        {
            var got = ExtractXamlTexts(src).Any(HasCjk);
            Check($"扫描器[XAML]: {name}", got == should);
        }
    }

    // ────────────────────────── 词法扫描 ──────────────────────────

    /// <summary>
    /// 取出 C# 源码里每一处**未迁移的中文字符串字面量**。两类东西**不算命中**：
    ///
    /// <list type="number">
    /// <item><b>注释</b> —— 本仓的中文大量在注释里（注释即文档），不剥掉会全量命中 ⇒
    ///       护栏退化成噪音、立刻被绕过。而剥注释又必须分清「字符串里的 <c>//</c>」与「真的注释」，
    ///       正则做这件事两边都会错 ⇒ 老老实实走一遍词法。</item>
    /// <item><b><c>L.Pick(…)</c> 实参里的字面量</b> —— 这是**已经双语化**的形态：
    ///       中文支本来就在源码里摆着（那是它的键）。把它算成命中，就等于「翻完的文件永远
    ///       留在台账上」，台账立刻失去意义（实测：AboutPage 翻完仍被判命中，就是这个原因）。</item>
    /// </list>
    ///
    /// <para>
    /// 覆盖的字面量形态：普通 <c>"…"</c>（含 <c>\\</c> 转义）、逐字 <c>@"…"</c>（<c>""</c> = 一个引号）、
    /// 插值 <c>$"…"</c> / <c>$@"…"</c> / <c>@$"…"</c>、原始 <c>"""…"""</c> 与 <c>$$"""…"""</c>。
    /// 插值的 <c>{}</c> 表达式**不展开**（那要真解析 C#）—— 对本判据够用：
    /// 表达式里的中文是另一条字符串字面量，会在它自己的扫描位置被取到。
    /// </para>
    /// </summary>
    internal static List<string> ExtractCSharpStringLiterals(string src)
    {
        var outp = new List<string>();
        int n = src.Length, i = 0;
        // L.Pick 的实参深度。用栈而不是计数器：嵌套调用（L.Pick(a, L.Pick(b, c))）也要正确。
        var pickStack = new Stack<bool>();
        int pickDepth = 0;
        bool pendingPick = false;

        while (i < n)
        {
            char c = src[i];

            if (c == '/' && i + 1 < n && src[i + 1] == '/')          // 行注释
            { i += 2; while (i < n && src[i] != '\n') i++; continue; }

            if (c == '/' && i + 1 < n && src[i + 1] == '*')          // 块注释
            {
                i += 2;
                while (i + 1 < n && !(src[i] == '*' && src[i + 1] == '/')) i++;
                i = Math.Min(i + 2, n);
                continue;
            }

            // 字符字面量：跳过，否则那个 ' 会被当成下一个字符串的起点
            if (c == '\'')
            {
                i++;
                while (i < n && src[i] != '\'' && src[i] != '\n') { if (src[i] == '\\') i++; i++; }
                i++;
                continue;
            }

            // 标识符 / 点链 —— 只为认出 L.Pick（连带它的 ( ）
            if (char.IsLetter(c) || c == '_' || c == '.')
            {
                int s0 = i;
                while (i < n && (char.IsLetterOrDigit(src[i]) || src[i] == '_' || src[i] == '.')) i++;
                var name = src[s0..i];
                pendingPick = name == "L.Pick" || name.EndsWith(".L.Pick", StringComparison.Ordinal)
                              // 也认写成 `Pick(` 的形态（`using static` 或本文件里的别名）
                              || (name == "Pick" && s0 >= 2 && src[s0 - 1] == '.' && src[s0 - 2] == 'L');
                continue;
            }

            if (c == '(') { pickStack.Push(pendingPick); if (pendingPick) pickDepth++; pendingPick = false; i++; continue; }
            if (c == ')') { if (pickStack.Count > 0 && pickStack.Pop()) pickDepth--; pendingPick = false; i++; continue; }

            // 字符串前缀
            bool verbatim = false;
            bool interpolated = false;   // 有 `$` ⇒ 串里有 `{…}` 插值孔，孔里**是代码**（可含嵌套字面量）
            int dollars = 0;             // 原始插值串的 `$` 个数决定孔的定界（`$$"""` 的孔是 `{{…}}`）
            int p = i;
            if (src[p] == '@' && p + 1 < n && src[p + 1] == '"') { verbatim = true; p++; }
            else if (src[p] == '@' && p + 2 < n && src[p + 1] == '$' && src[p + 2] == '"')
            { verbatim = true; interpolated = true; dollars = 1; p += 2; }
            else if (src[p] == '$')
            {
                int d0 = p;
                int k = p;
                while (k < n && src[k] == '$') k++;                   // $$""" 原始插值：$ 可以有多个
                dollars = k - d0;
                interpolated = true;
                if (k < n && src[k] == '@') { verbatim = true; k++; }
                if (k < n && src[k] == '"') p = k;
                else { i++; if (!char.IsWhiteSpace(c)) pendingPick = false; continue; }
            }
            if (p >= n || src[p] != '"')
            {
                i++;
                if (!char.IsWhiteSpace(c)) pendingPick = false;       // 空白不清 flag（L.Pick 与 ( 之间可有空格）
                continue;
            }

            // 原始字符串：连续 >=3 个引号，用同样多的引号闭合
            int q = p; while (q < n && src[q] == '"') q++;
            int run = q - p;
            if (run >= 3)
            {
                int end = src.IndexOf(new string('"', run), q, StringComparison.Ordinal);
                if (end < 0) { if (pickDepth == 0) outp.Add(src[q..]); break; }
                if (pickDepth == 0) outp.Add(src[q..end]);
                i = end + run;
                pendingPick = false;
                continue;
            }

            // 普通 / 逐字字符串
            var sb = new StringBuilder();
            int s = p + 1;
            while (s < n)
            {
                char ch = src[s];
                if (verbatim)
                {
                    if (ch == '"')
                    {
                        if (s + 1 < n && src[s + 1] == '"') { sb.Append('"'); s += 2; continue; }
                        break;
                    }
                    // 逐字插值串 `$@"…"` 也能有孔
                    if (interpolated && ch == '{')
                    {
                        if (s + 1 < n && src[s + 1] == '{') { sb.Append("{{"); s += 2; continue; }  // `{{` 是字面量
                        s = TakeInterpolationHole(src, s, outp, pickDepth);
                        continue;
                    }
                    sb.Append(ch); s++;
                }
                else
                {
                    if (ch == '\\') { if (s + 1 < n) sb.Append(src[s + 1]); s += 2; continue; }
                    if (ch == '"' || ch == '\n') break;                // \n 兜住未闭合（防御）
                    if (interpolated && ch == '{')
                    {
                        if (s + 1 < n && src[s + 1] == '{') { sb.Append("{{"); s += 2; continue; }  // `{{` 是字面量
                        s = TakeInterpolationHole(src, s, outp, pickDepth);
                        continue;
                    }
                    sb.Append(ch); s++;
                }
            }
            if (pickDepth == 0) outp.Add(sb.ToString());
            i = s + 1;
            pendingPick = false;
        }
        return outp;
    }

    /// <summary>
    /// 从插值孔的 <c>{</c> 处扫到配对的 <c>}</c>，**把孔内的原文当成一段"待判内容"收下**。
    ///
    /// <para>
    /// ⚠ 这是**必须做**的一步，不是锦上添花。`$"{"行数",8}"` 这种**孔里直接放字面量**的写法，
    /// 按「引号即字面量边界」走的扫描器会在孔内那个引号处**提前收尾**，剩下的中文被当成标识符
    /// （`char.IsAlpha` 对汉字为真）⇒ **整条串零命中、护栏静默失效**。
    /// 实测由子智能体发现（它在 <c>Program.Commands.cs</c> 正好踩到），它还指出一个更要紧的推论：
    /// **别的 agent 报「已翻到 0」的文件未必真是 0** —— 一个会说谎的闸门比没有闸门更糟。
    /// </para>
    ///
    /// <para>
    /// 判据的**方向**是刻意选的：孔内只要有中文就**算命中**，宁可多报（台账加一行理由即可）
    /// 也不漏报。所以像 <c>$"{DateTime.Now:yyyy年}"</c> 的格式串、<c>$"{Map["中文键"]}"</c> 的
    /// 中文键都会命中 —— 前者确实是用户可见文案，后者由台账的「有意保留 + 理由」兜住。
    /// </para>
    ///
    /// <para>
    /// ⚠ 原始插值串（<c>$$"""…"""</c>）**不需要**这一步：那边的正文是整段收下的，
    /// 孔里的中文本来就在正文里（见上面那个 `run >= 3` 分支）。
    /// </para>
    /// </summary>
    /// <returns>配对 <c>}</c> 之后的下标（未找到则返回串尾）。</returns>
    private static int TakeInterpolationHole(string src, int openBrace, List<string> outp, int pickDepth)
    {
        int n = src.Length;
        int depth = 0;
        int s = openBrace;
        while (s < n)
        {
            char ch = src[s];
            if (ch == '{') { depth++; s++; continue; }
            if (ch == '}')
            {
                depth--;
                if (depth == 0)
                {
                    var hole = src[(openBrace + 1)..s];
                    if (pickDepth == 0 && hole.Length > 0) outp.Add(hole);
                    return s + 1;
                }
                s++;
                continue;
            }
            s++;
        }
        return n;   // 未闭合（源码本身就坏了）：不吞掉后面的内容，交给外层继续
    }

    /// <summary>
    /// 取出 XAML 里每一处**未迁移**的属性值与标签间文本，注释（<c>&lt;!-- --&gt;</c>）跳过。
    ///
    /// <para>
    /// <c>{markup:Tr Zh='…', En='…'}</c> 的属性值**不算命中**，理由与 C# 侧跳过
    /// <c>L.Pick</c> 实参完全相同：那是已双语化的形态，中文支是它的键。
    /// </para>
    /// </summary>
    internal static List<string> ExtractXamlTexts(string src)
    {
        var outp = new List<string>();
        int n = src.Length, i = 0;
        while (i < n)
        {
            if (i + 3 < n && src[i] == '<' && src[i + 1] == '!' && src[i + 2] == '-' && src[i + 3] == '-')
            {
                int e = src.IndexOf("-->", i, StringComparison.Ordinal);
                i = e < 0 ? n : e + 3;
                continue;
            }
            if (src[i] == '<')
            {
                int e = src.IndexOf('>', i);
                if (e < 0) break;
                // 标签内的属性值。⚠ 属性值里的 `>` 会被上面那句截断（XAML 里 `>` 出现在
                // 属性值中要写成 `&gt;`），所以这里不必再管引号内的尖括号。
                foreach (Match m in Regex.Matches(src[i..e], "\"([^\"]*)\""))
                {
                    var v = m.Groups[1].Value;
                    if (v.StartsWith("{markup:Tr", StringComparison.Ordinal)) continue;  // 已双语化
                    outp.Add(v);
                }
                i = e + 1;
                continue;
            }
            int lt = src.IndexOf('<', i);
            if (lt < 0) lt = n;
            outp.Add(src[i..lt]);
            i = lt;
        }
        return outp;
    }
}
