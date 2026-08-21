using System.Text;

namespace WayCoder.UI.Cli.Arguments;

/// <summary>
/// CLI 参数注册表 —— 集中管理所有 CliArg 子类实例。
/// Register 时自动检测名称冲突（包括长短名），有重复立即报错。
/// Parse 遍历 args[] 匹配已注册参数，返回结构化解析结果。
/// </summary>
public static class CliArgRegistry
{
    static readonly List<CliArg> _args = new();
    static readonly Dictionary<string, CliArg> _byName = new(StringComparer.Ordinal);

    /// <summary>所有已注册参数（按注册顺序）</summary>
    public static IReadOnlyList<CliArg> All => _args;

    /// <summary>
    /// 注册一个参数。自动检测名称冲突（包括长短名），重复则抛出 InvalidOperationException。
    /// </summary>
    public static void Register(CliArg arg)
    {
        foreach (var name in arg.Names)
        {
            if (_byName.TryGetValue(name, out var existing))
                throw new InvalidOperationException(
                    $"CLI 参数名称冲突: \"{name}\" 已被 [{existing.Key}] 注册，无法再用于 [{arg.Key}]");
            _byName[name] = arg;
        }
        _args.Add(arg);
    }

    /// <summary>
    /// 解析命令行参数。
    /// 返回 (Values: 按 Key 索引的值字典, ExitCode: 非 null 表示应立即退出)。
    /// 支持 --key=value 格式。
    /// </summary>
    public static (Dictionary<string, List<string>> Values, int? ExitCode) Parse(string[] args)
    {
        var values = new Dictionary<string, List<string>>();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (!arg.StartsWith('-')) continue;

            // 支持 --key=value 格式
            CliArg? def = null;
            var eqIdx = arg.IndexOf('=');
            if (eqIdx > 1 && _byName.TryGetValue(arg[..eqIdx], out var eqDef))
            {
                def = eqDef;
                var embedded = arg[(eqIdx + 1)..];
                if (def is { AllowMultiple: true } && values.TryGetValue(def.Key, out var prevEq))
                    prevEq.Add(embedded);
                else
                    values[def.Key] = [embedded];
                var eqExit = def.OnMatch([embedded]);
                if (eqExit.HasValue) return (values, eqExit.Value);
                continue;
            }

            if (!_byName.TryGetValue(arg, out def)) continue;

            var consumed = new List<string>();

            if (def.Greedy)
            {
                // 贪婪：吞掉后续参数，直到遇到下一个以 - 开头的旗标
                while (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                    consumed.Add(args[++i]);
            }
            else if (def.ValueCount == -1)
            {
                // 可选值：仅当下一个 arg 不以 - 开头时消耗
                if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                    consumed.Add(args[++i]);
            }
            else if (def.ValueCount > 0)
            {
                for (int j = 0; j < def.ValueCount && i + 1 < args.Length; j++)
                    consumed.Add(args[++i]);
            }

            // 允许累积：同一参数多次出现时追加而非覆盖（如 -p1 "A" -p1 "B" → [A, B]）
            // 此前先无条件 values[def.Key]=consumed 再判断 ContainsKey，导致 !ContainsKey 分支永远走不到、
            // AllowMultiple 时每次把 consumed 自身重复追加一份（[B,B] 且丢 A）。
            if (values.TryGetValue(def.Key, out var prev) && def.AllowMultiple)
                prev.AddRange(consumed);
            else
                values[def.Key] = consumed;
            var exit = def.OnMatch(consumed);
            if (exit.HasValue) return (values, exit.Value);
        }

        return (values, null);
    }

    /// <summary>从解析结果获取单个值</summary>
    public static string? Get(Dictionary<string, List<string>> parsed, string key)
        => parsed.TryGetValue(key, out var v) && v.Count > 0 ? v[0] : null;

    /// <summary>获取全部值</summary>
    public static List<string>? GetAll(Dictionary<string, List<string>> parsed, string key)
        => parsed.TryGetValue(key, out var v) ? v : null;

    /// <summary>参数是否存在</summary>
    public static bool Has(Dictionary<string, List<string>> parsed, string key)
        => parsed.ContainsKey(key);

    /// <summary>分类显示顺序（参考 Crush help 分组风格，无图标）</summary>
    private static readonly string[] _categoryOrder =
        ["模型", "会话", "执行", "预算", "权限", "界面", "系统", "批量", "测试", "通用", "其他"];

    /// <summary>参数 → 分类（未匹配归「其他」）</summary>
    private static string CategoryOf(CliArg arg) => arg.Key switch
    {
        "model" or "base-url" or "api-key" or "tiny" or "economy" => "模型",
        "resume" or "session" or "session-list" => "会话",
        "prompt" or "json" or "output-format" or "prompt-all" => "执行",
        "max-budget-usd" or "max-requeue" or "max-turns" => "预算",
        "yolo" or "permission-mode" or "allowed-tools" or "disallowed-tools" or "permit" => "权限",
        "tui" or "cli" or "web" or "gui" or "keypad" or "theme" or "quiet" or "no-color" => "界面",
        "edit" or "watch" or "update" or "init" or "config" or "debug" or "system-prompt" or "screenshot"
            or "auto-commit" or "mcp" or "mcp-config" or "reset" or "purge" or "provider" => "系统",
        "batch" or "batch-repo" or "batch-task" or "batch-keep" => "批量",
        "test" or "test-benchmark" or "test-limits" => "测试",
        "version" or "help" => "通用",
        _ => "其他",
    };

    /// <summary>生成帮助文本（排除 Internal 参数，按分类分组显示）</summary>
    public static string HelpText(int indent = 2, int nameWidth = 26)
    {
        var sb = new StringBuilder();
        var visible = _args.Where(a => !a.Internal).ToList();

        // ── 三列布局：短名列（-字母，无则留白）/ 长名列（--xxx）/ 说明列（全部对齐）──
        int maxShortLen = 0, maxNameW = 0;
        foreach (var a in visible)
            foreach (var g in BuildGroups(a))
            {
                var s = ShortNameOf(g);
                if (s != null) maxShortLen = Math.Max(maxShortLen, s.Length);
                maxNameW = Math.Max(maxNameW,
                    WayCoder.UI.Shared.Terminal.AnsiString.DisplayWidth(LongNameOf(g))
                    + WayCoder.UI.Shared.Terminal.AnsiString.DisplayWidth(ValuePart(a)));
            }
        int shortColW = maxShortLen + 1; // 逗号占位
        int longCol = indent + shortColW + 1;  // 所有长名起点（对齐）
        int descCol = longCol + maxNameW + 2;  // 所有说明起点（对齐）

        foreach (var category in _categoryOrder)
        {
            var items = visible.Where(a => CategoryOf(a) == category).ToList();
            if (items.Count == 0) continue;

            // 分类标题上方空一行，分隔线样式：---< 模型 >------（无图标，总宽 34）
            sb.AppendLine();
            var title = $"---< {category} >";
            var titleWidth = WayCoder.UI.Shared.Terminal.AnsiString.DisplayWidth(title);
            if (titleWidth < 34) title += new string('-', 34 - titleWidth);
            sb.AppendLine($"«bold cyan»{title}«/»");

            foreach (var arg in items)
            {
                var groups = BuildGroups(arg);
                var keyLong = $"--{arg.Key}";
                var primaryGroup = groups.FirstOrDefault(g =>
                    g.Any(n => string.Equals(n, keyLong, StringComparison.OrdinalIgnoreCase)))
                    ?? groups.FirstOrDefault() ?? [];

                // 主行（普通色 + 描述）
                RenderLine(sb, primaryGroup, arg, indent, shortColW, descCol,
                    dim: false, desc: arg.Description);

                // 其余「短名+长名」组：纵向列出（暗灰 + 别名）
                foreach (var g in groups)
                    if (!ReferenceEquals(g, primaryGroup))
                        RenderLine(sb, g, arg, indent, shortColW, descCol,
                            dim: true, desc: "别名");

                // 二级参数（子命令）：纵向列出 + 说明（暗灰），说明对齐
                if (arg.SubCommands is { Length: > 0 } subs)
                    AppendSubCommands(sb, subs, longCol, descCol);
            }
        }

        return sb.ToString();
    }

    /// <summary>组内的短名（第一个 -x），无则 null。</summary>
    private static string? ShortNameOf(List<string> group)
        => group.FirstOrDefault(n => n.StartsWith('-') && !n.StartsWith("--"));

    /// <summary>组内的长名（第一个 --xxx），无则首个名字。</summary>
    private static string LongNameOf(List<string> group)
        => group.FirstOrDefault(n => n.StartsWith("--")) ?? group.FirstOrDefault() ?? "";

    /// <summary>渲染一行：短名列（无则留白）+ 长名列 + 说明列（三列全部对齐）。</summary>
    private static void RenderLine(StringBuilder sb, List<string> group, CliArg arg,
        int indent, int shortColW, int descCol, bool dim, string desc)
    {
        var line = new StringBuilder();
        line.Append(new string(' ', indent));
        var shortName = ShortNameOf(group);
        if (shortName != null)
        {
            var sn = shortName + ",";
            if (sn.Length < shortColW) sn = sn.PadRight(shortColW);
            line.Append(sn);
        }
        else
            line.Append(new string(' ', shortColW)); // 无短名留白，长名对齐
        line.Append(' ').Append(LongNameOf(group)).Append(ValuePart(arg));

        var w = WayCoder.UI.Shared.Terminal.AnsiString.DisplayWidth(line.ToString());
        if (w < descCol) line.Append(new string(' ', descCol - w));

        if (dim) sb.Append("«dim»");
        sb.Append(line.ToString());
        if (desc.Length > 0) sb.Append("  ").Append(desc);
        if (dim) sb.Append("«/»");
        sb.AppendLine();
    }

    /// <summary>
    /// 值标签渲染：必填值（ValueCount=1）用 <x>，可选值（ValueCount≠1）用 [x]；
    /// 标签本身已含括号（如 "项 [值]"）则原样，不再包外层括号。
    /// </summary>
    private static string ValuePart(CliArg arg)
    {
        if (arg.ValueLabel == null) return "";
        var l = arg.ValueLabel;
        if (l.Contains('[') || l.Contains('<')) return $" {l}";
        return arg.ValueCount == 1 ? $" <{l}>" : $" [{l}]";
    }

    /// <summary>短名与其后的长名配对成组（-r, --resume / -c, --continue）；孤立的多个长名各自成组。</summary>
    private static List<List<string>> BuildGroups(CliArg arg)
    {
        var groups = new List<List<string>>();
        foreach (var n in arg.Names)
        {
            if (n.StartsWith('-') && !n.StartsWith("--"))
            {
                // 短名：当前组已含长名则新开组，否则并入
                if (groups.Count == 0 || groups[^1].Any(x => x.StartsWith("--")))
                    groups.Add([]);
                groups[^1].Add(n);
            }
            else
            {
                // 长名：当前组有短名且未含长名则并入，否则新开组
                if (groups.Count > 0 && groups[^1].Any(x => x.StartsWith('-') && !x.StartsWith("--")) && !groups[^1].Any(x => x.StartsWith("--")))
                    groups[^1].Add(n);
                else
                    groups.Add([n]);
            }
        }
        return groups;
    }

    /// <summary>二级参数（子命令）纵向列出：说明与主说明列对齐，整行暗灰。</summary>
    private static void AppendSubCommands(StringBuilder sb, (string Cmd, string Desc)[] subs, int longCol, int descCol)
    {
        int subIndent = longCol + 1; // 子命令比长名再深 1 格
        foreach (var (cmd, desc) in subs)
        {
            var sub = new string(' ', subIndent) + cmd;
            var w = WayCoder.UI.Shared.Terminal.AnsiString.DisplayWidth(sub);
            if (w < descCol) sub += new string(' ', descCol - w);
            sb.Append("«dim»");
            sb.Append(sub);
            sb.Append("  ");
            sb.AppendLine(desc + "«/»");
        }
    }
}
