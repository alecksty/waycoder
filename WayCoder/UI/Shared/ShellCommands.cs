using System.Text;

namespace WayCoder.UI.Shared;

/// <summary>
/// 命令行页的一条**注册项**。
///
/// 为什么是"注册"而不是页面里一串 <c>if (cmd == "xxx")</c>：
/// ① 加一条命令要同时改**四件事**（分派、用法文本、参数校验、help 列表），
///    散在页面里必然漂 —— 本仓库排第一的坑就是"必须手工同步的平行表"；
///    收成一条记录之后，这四件事**是同一条数据的四个字段**，想漏都漏不了。
/// ② 参数个数/格式有了声明，校验与用法文案就能自动生成，不用每条命令自己写一遍。
/// ③ 别人（插件、工具）可以往注册表里加命令，页面一行都不用改。
/// </summary>
/// <param name="Name">命令名（大小写敏感，与 shell 一致）。</param>
/// <param name="ArgsFormat">参数格式，给人看的，如 <c>"&lt;文件&gt;"</c>、<c>"[命令]"</c>；无参给空串。</param>
/// <param name="Summary">一句话说明，<c>help</c> 列表里用。</param>
/// <param name="Detail">详细说明，<c>命令 -h</c> 时用；可留空（则回落到 <see cref="Summary"/>）。</param>
/// <param name="Run">执行体：拿到参数，返回要追加到输出区的文本。</param>
/// <param name="MinArgs">最少参数个数。</param>
/// <param name="MaxArgs">最多参数个数；<c>-1</c> = 不限。</param>
/// <param name="ProducesMarkup">
/// 执行体的返回值**是否已经是 «» 中间格式**。
///
/// 这个声明是必需的，不是装饰：输出区对「外部命令的裸输出」要做一遍
/// `AnsiMarkup.ToMarkup`（把 ANSI 转义翻成标记、光标序列吃掉），而**页面自己注册的命令
/// 往往已经产出标记**（如 `vml` 分支把编译错误套红成 `«red»…«/»`）。
/// 不声明的话就变成"同一份文本转两遍"——`«` 会被转义成 `««`，渲染端只还原一层，
/// **屏幕上剩下字面的 `«red»红«/»`**。
///
/// 实测踩过（2026-09-21）：命令行页敲 `vml run examples/c/ansi_colors.c`，
/// 彩色输出全部显示成标记文本，而转换器与解析器的判据全绿 —— 断的就是这一处。
/// </param>
public sealed record ShellCommand(
    string Name,
    string ArgsFormat,
    string Summary,
    string Detail,
    Func<IReadOnlyList<string>, Task<string>> Run,
    int MinArgs = 0,
    int MaxArgs = -1,
    bool ProducesMarkup = false)
{
    /// <summary>用法行，如 <c>vml run &lt;文件&gt;</c>。名字与参数格式都来自本记录，不另写一份。</summary>
    public string Usage => string.IsNullOrEmpty(ArgsFormat) ? Name : $"{Name} {ArgsFormat}";

    /// <summary>参数个数要求的人话描述（错误提示里用）。</summary>
    public string ArgsRequirement => (MinArgs, MaxArgs) switch
    {
        (0, -1) => "参数不限",
        (0, 0) => "不带参数",
        (0, var max) => $"最多 {max} 个参数",
        (var min, -1) => $"至少 {min} 个参数",
        (var min, var max) when min == max => $"恰好 {min} 个参数",
        (var min, var max) => $"{min}~{max} 个参数",
    };
}

/// <summary>
/// 命令行页的**命令注册表** —— 「有哪些命令、怎么用、参数对不对」的唯一事实源。
///
/// 纯逻辑（不依赖 MAUI），所以能自测；页面只负责把执行体注册进来并调用 <see cref="DispatchAsync"/>。
/// 不认识的输入一律返回 <c>null</c>，由页面交给系统 shell —— **本表只登记页面自己认识的命令**，
/// `ls`/`git` 那些的用法归它们自己的 <c>--help</c>，这里不抄一份（抄了就是平行表）。
/// </summary>
public sealed class ShellCommandRegistry
{
    private readonly List<ShellCommand> _commands = [];

    /// <summary>已注册的命令，按注册顺序（= help 列表的顺序）。</summary>
    public IReadOnlyList<ShellCommand> Commands => _commands;

    /// <summary>注册一条命令。重名直接替换（后注册的赢）—— 便于测试与插件覆盖。</summary>
    public ShellCommandRegistry Register(ShellCommand command)
    {
        _commands.RemoveAll(c => c.Name == command.Name);
        _commands.Add(command);
        return this;
    }

    /// <summary>按名字查。查不到返回 null（= 交给 shell）。</summary>
    public ShellCommand? Find(string? name)
        => string.IsNullOrEmpty(name) ? null : _commands.Find(c => c.Name == name);

    /// <summary>
    /// 这一行命令的**输出**是不是已经是 «» 中间格式（决定调用方要不要再过一遍 ANSI 转换）。
    ///
    /// ⚠ 判据与 <see cref="DispatchAsync"/> **同源**（同一张 <c>_commands</c> 表），
    /// 不要在调用方另写一份"首词是不是 vml"的前缀判断 —— 那就是平行表，
    /// 而且正是这个 bug 的成因（页面按 `markupResult: false` 一律再转一遍）。
    /// </summary>
    public bool ResultIsMarkup(string? cmdLine)
    {
        if (string.IsNullOrWhiteSpace(cmdLine)) return false;
        var s = cmdLine.TrimStart();
        var end = s.IndexOf(' ');
        var name = end < 0 ? s : s[..end];
        return Find(name)?.ProducesMarkup ?? false;
    }

    /// <summary>帮助开关：`-h` / `--help` / `-help` / `/?`。</summary>
    public static bool IsHelpFlag(string? arg)
        => arg is "-h" or "--help" or "-help" or "/?";

    /// <summary>参数里有没有帮助开关。</summary>
    public static bool HasHelpFlag(IReadOnlyList<string> args)
    {
        for (var i = 0; i < args.Count; i++)
            if (IsHelpFlag(args[i])) return true;
        return false;
    }

    /// <summary>
    /// 把用户敲的一行拆成「命令 + 参数」。只做空白切分 —— 命令行页不做引号解析
    /// （真有引号语义的输入整行交给 shell）。
    /// </summary>
    public static (string Command, string[] Args) Split(string? line)
    {
        var parts = (line ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return ("", []);
        return (parts[0], parts[1..]);
    }

    /// <summary>整个命令表 —— <c>help</c> 不带参数时打印。</summary>
    public string HelpText()
    {
        var sb = new StringBuilder();
        sb.Append("本页命令（其余输入按 shell 命令执行，用法看它们自己的 --help）：\n");
        foreach (var c in _commands)
            sb.Append($"  {c.Usage,-32} {c.Summary}\n");
        sb.Append("\n提示：任何一条都可以 `命令 -h` 看详细用法；`help <命令>` 等价。");
        return sb.ToString();
    }

    /// <summary>一条命令的用法（`-h` 与 `help <命令>` 共用这一份）。</summary>
    public string UsageOf(ShellCommand c)
    {
        var detail = string.IsNullOrEmpty(c.Detail) ? c.Summary : c.Detail;
        return $"用法：{c.Usage}\n      {detail}\n      参数：{c.ArgsRequirement}";
    }

    /// <summary>
    /// 参数个数校验。合法返回 null，否则返回给用户看的错误文案
    /// （**带用法**——只说"参数不对"用户还得再敲一次 help）。
    ///
    /// 提示按**具体情形**写（"至少 1 个" / "最多 2 个" / "不带参数"），
    /// 而不是把 <see cref="ShellCommand.ArgsRequirement"/> 那句区间描述直接套上去 ——
    /// 用户是在"少给了"或"多给了"的现场，笼统的区间不如直接说该补还是该减。
    /// </summary>
    public string? Validate(ShellCommand c, IReadOnlyList<string> args)
    {
        if (args.Count < c.MinArgs)
            return $"⚠️ `{c.Name}` 至少需要 {c.MinArgs} 个参数（给了 {args.Count} 个）。\n{UsageOf(c)}";

        if (c.MaxArgs >= 0 && args.Count > c.MaxArgs)
        {
            var how = c.MaxArgs == 0
                ? $"`{c.Name}` 不带参数"
                : $"`{c.Name}` 最多 {c.MaxArgs} 个参数";
            return $"⚠️ {how}（给了 {args.Count} 个）。\n{UsageOf(c)}";
        }
        return null;
    }

    /// <summary>
    /// 走完整条链路：找命令 → 帮助开关 → 参数校验 → 执行。
    ///
    /// 返回 <c>null</c> 表示**本表不认识这条命令**，调用方应交给 shell 执行
    /// （这是唯一的"放行"信号，别用空串表示 —— 空串是合法的执行结果）。
    /// </summary>
    public async Task<string?> DispatchAsync(string line)
    {
        var (name, args) = Split(line);
        var cmd = Find(name);
        if (cmd == null) return null;

        // 帮助开关**先于**参数校验：`clear -h` 有 1 个参数、而 clear 不收参数，
        // 先校验就会把"想看用法"报成"参数错了"。
        if (HasHelpFlag(args)) return UsageOf(cmd);

        var error = Validate(cmd, args);
        if (error != null) return error;

        return await cmd.Run(args);
    }
}
