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

    /// <summary>生成帮助文本（排除 Internal 参数）</summary>
    public static string HelpText(int indent = 2, int nameWidth = 26)
    {
        var sb = new StringBuilder();
        var visible = _args.Where(a => !a.Internal).ToList();

        foreach (var arg in visible)
        {
            var left = new string(' ', indent) + arg.NameDisplay;
            if (left.Length < nameWidth)
                left = left.PadRight(nameWidth);
            sb.Append(left);
            sb.Append("  ");
            sb.AppendLine(arg.Description);
        }

        return sb.ToString();
    }
}
