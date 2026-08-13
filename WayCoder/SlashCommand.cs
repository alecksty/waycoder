using WayCoder.UI.TuiScreens;

namespace WayCoder;

/// <summary>
/// 程序全局上下文 —— 命令执行时需要的引用，避免大量构造函数注入。
/// 由 Program 启动时设置。
/// </summary>
public static class ProgramContext
{
    public static Agent? Agent { get; set; }
    public static Config Config { get; set; } = new();
    public static LLM? LLM { get; set; }
}

// ════════════════════════════════════════════════════════════════
// 接口
// ════════════════════════════════════════════════════════════════

/// <summary>
/// 斜杠命令接口。每个命令实现此接口即可注册到 SlashCommandRegistry。
/// </summary>
public interface ISlashCommand
{
    /// <summary>主命令名（含 / 前缀），如 "/test"</summary>
    string Name { get; }

    /// <summary>别名列表（可空数组），如 ["/perm", "/permissions"]</summary>
    string[] Aliases { get; }

    /// <summary>一行描述，用于帮助表格</summary>
    string Description { get; }

    /// <summary>用法提示（null = 无参数），如 "/test [all|tui|tools]"</summary>
    string? Usage { get; }

    /// <summary>判断 userInput 是否匹配本命令</summary>
    bool Matches(string input);

    /// <summary>执行命令。args 为去掉命令名和空白后的参数部分。</summary>
    Task ExecuteAsync(string args, ChatScreen screen);
}

// ════════════════════════════════════════════════════════════════
// 基类
// ════════════════════════════════════════════════════════════════

/// <summary>
/// 斜杠命令基类 —— 提供默认 Matches 实现（精确匹配 + 前缀匹配）。
/// 子类只需覆写 Name、Description、ExecuteAsync 即可。
/// </summary>
public abstract class SlashCommand : ISlashCommand
{
    public abstract string Name { get; }
    public virtual string[] Aliases => [];
    public abstract string Description { get; }
    public virtual string? Usage => null;

    /// <summary>
    /// 默认匹配：精确 Name/Aliases，或前缀 "Name " / "Alias "。
    /// </summary>
    public virtual bool Matches(string input)
    {
        if (string.Equals(input, Name, StringComparison.OrdinalIgnoreCase))
            return true;

        var nameSpace = Name + " ";
        if (input.StartsWith(nameSpace, StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (var alias in Aliases)
        {
            if (string.Equals(input, alias, StringComparison.OrdinalIgnoreCase))
                return true;
            var aliasSpace = alias + " ";
            if (input.StartsWith(aliasSpace, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public abstract Task ExecuteAsync(string args, ChatScreen screen);
}

// ════════════════════════════════════════════════════════════════
// 注册表
// ════════════════════════════════════════════════════════════════

/// <summary>
/// 斜杠命令注册表 —— 统一管理所有内置 + 自定义命令。
/// 注册顺序即匹配优先级（先注册先匹配）。
/// </summary>
public static class SlashCommandRegistry
{
    static readonly List<ISlashCommand> _commands = [];

    /// <summary>已注册的命令列表（按注册顺序）</summary>
    public static IReadOnlyList<ISlashCommand> Commands => _commands;

    /// <summary>注册一个命令</summary>
    public static void Register(ISlashCommand cmd) => _commands.Add(cmd);

    /// <summary>
    /// 注册所有内置命令（AOT 兼容：显式 new，不用反射）。
    /// 由 Program 启动时调用。注册顺序即匹配优先级。幂等：重复调用不会重复注册。
    /// </summary>
    public static void RegisterAll()
    {
        if (_commands.Count > 0) return; // 幂等：已注册则跳过

        // P0 — 高频命令
        Register(new Commands.TestCommand());
        Register(new Commands.HelpCommand());
        Register(new Commands.ResetCommand());
        Register(new Commands.ModelCommand());
        Register(new Commands.ProviderCommand());
        Register(new Commands.TokensCommand());
        Register(new Commands.StatsCommand());
        Register(new Commands.CompactCommand());

        // P1 — 会话/上下文
        Register(new Commands.SessionCommand());
        Register(new Commands.HistoryCommand());
        Register(new Commands.ExportCommand());
        Register(new Commands.ArchitectCommand());

        // 编辑/工具
        Register(new Commands.EditCommand());
        Register(new Commands.SearchCommand());
        Register(new Commands.RecentCommand());
        Register(new Commands.TodoCommand());
        Register(new Commands.LintCommand());

        // 跨槽位消息
        Register(new Commands.SendCommand());
        Register(new Commands.BroadcastCommand());

        // Git
        Register(new Commands.GitCommand());
        Register(new Commands.PrCommand());
        Register(new Commands.AutoCommitCommand());

        // 检查点
        Register(new Commands.CheckpointCommand());
        Register(new Commands.CheckpointsCommand());
        Register(new Commands.UndoCommand());

        // 配置/系统
        Register(new Commands.ConfigCommand());
        Register(new Commands.SettingsCommand());
        Register(new Commands.ThemeCommand());
        Register(new Commands.PermCommand());
        Register(new Commands.AutoCommand());
        Register(new Commands.ModeCommand());
        Register(new Commands.ImportCommand());
        Register(new Commands.AboutCommand());
        Register(new Commands.RepomapCommand());
        Register(new Commands.DebugCommand());
    }

    /// <summary>所有命令名（主名 + 别名），用于拼写纠错和 Tab 补全</summary>
    public static string[] AllNames
    {
        get
        {
            var names = new List<string>();
            foreach (var cmd in _commands)
            {
                names.Add(cmd.Name);
                foreach (var alias in cmd.Aliases)
                    names.Add(alias);
            }
            return names.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }

    /// <summary>
    /// 匹配用户输入。按注册顺序遍历，返回首个匹配的 (命令, 参数)。
    /// 参数 = 去掉命令名和紧随空格后的剩余文本，已 Trim。
    /// 未匹配返回 (null, "")。
    /// </summary>
    public static (ISlashCommand? Command, string Args) Match(string userInput)
    {
        foreach (var cmd in _commands)
        {
            var args = TryExtractArgs(cmd, userInput);
            if (args != null)
                return (cmd, args);
        }
        return (null, "");
    }

    /// <summary>提取参数：精确匹配→""，前缀匹配→剩余部分。未匹配→null。</summary>
    static string? TryExtractArgs(ISlashCommand cmd, string input)
    {
        // 精确匹配主名
        if (string.Equals(input, cmd.Name, StringComparison.OrdinalIgnoreCase))
            return "";

        // 前缀匹配 "Name "
        var nameSpace = cmd.Name + " ";
        if (input.StartsWith(nameSpace, StringComparison.OrdinalIgnoreCase))
            return input[nameSpace.Length..].Trim();

        // 检查别名
        foreach (var alias in cmd.Aliases)
        {
            if (string.Equals(input, alias, StringComparison.OrdinalIgnoreCase))
                return "";
            var aliasSpace = alias + " ";
            if (input.StartsWith(aliasSpace, StringComparison.OrdinalIgnoreCase))
                return input[aliasSpace.Length..].Trim();
        }

        return null;
    }
}
