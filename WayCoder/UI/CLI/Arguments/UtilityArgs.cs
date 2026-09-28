using WayCoder.Tools;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Cli.Arguments;

// ═══════════════════════════════════════════════════════════════
// 标志参数
// ═══════════════════════════════════════════════════════════════

public class VersionArg : CliArg
{
    public override string Description => L.Pick("显示版本信息", "Show version information");
    public VersionArg() : base("version", "-v", "--version") { }
}

public class InitArg : CliArg
{
    public override string Description => L.Pick(
        "初始化项目配置（.waycoder/ 目录）",
        "Initialize project configuration (the .waycoder/ directory)");
    public InitArg() : base("init", "-i", "--init") { }
}

public class WatchArg : CliArg
{
    public override string Description => L.Pick(
        "Watch 模式（监听文件中的 AI! 注释）",
        "Watch mode (listens for AI! comments in files)");
    public WatchArg() : base("watch", "-w", "--watch") { }
}

public class TinyArg : CliArg
{
    public override string Description => L.Pick(
        "Tiny 模式（精简提示词 + 小窗口；可指定如 --tiny 8k，缺省自动探测，失败回退 4K）",
        "Tiny mode (trimmed prompt + small context window; pass e.g. --tiny 8k, otherwise auto-detected with a 4K fallback)");
    public override int ValueCount => -1;
    public override string? ValueLabel => L.Pick("窗口", "window size");
    public TinyArg() : base("tiny", "-tt", "--test-tiny", "--tiny") { }
}

public class EconomyArg : CliArg
{
    public override string Description => L.Pick(
        "省 Token 模式（--economy [on|auto|off]，缺省 on；auto 按任务复杂度动态调节阈值）",
        "Token-saving mode (--economy [on|auto|off], default on; auto adjusts the thresholds to task complexity)");
    public override int ValueCount => -1;
    public override string? ValueLabel => L.Pick("模式", "mode");
    public EconomyArg() : base("economy", "-e", "--economy") { }
}

/// <summary>
/// --edit &lt;文件路径&gt; —— 启动后直接进入终端编辑器打开指定文件。
/// 等价于进入界面后执行 /edit 文件路径（Esc/Ctrl+Q 退出回到聊天界面）。
/// </summary>
public class EditArg : CliArg
{
    public override string Description => L.Pick(
        "直接进入编辑器打开文件（--edit <文件路径>）",
        "Open a file in the editor right away (--edit <file path>)");
    public override int ValueCount => 1;
    public override string? ValueLabel => L.Pick("文件路径", "file path");
    public EditArg() : base("edit", "--edit") { }
}

public class UpdateArg : CliArg
{
    public override string Description => L.Pick(
        "检查并自动升级到最新版本（优先 GitHub、回退 Gitee）",
        "Check for and auto-upgrade to the latest version (GitHub first, Gitee as fallback)");
    public UpdateArg() : base("update", "--update") { }
}

public class JsonArg : CliArg
{
    public override string Description => L.Pick(
        "JSON 输出模式（配合 -p 一次性模式，stdout 输出结构化 JSON，供 IDE/脚本解析）",
        "JSON output mode (use with -p one-shot mode; prints structured JSON to stdout for IDEs/scripts)");
    public JsonArg() : base("json", "-j", "--json") { }
}

public class WebArg : CliArg
{
    public override string Description => L.Pick(
        "浏览器聊天界面（--web [端口]，默认 9527，自动打开浏览器）",
        "Browser chat UI (--web [port], default 9527, opens the browser automatically)");
    public override int ValueCount => -1; // 可选端口
    public override string? ValueLabel => L.Pick("端口", "port");
    public WebArg() : base("web", "--web") { }
}

public class TuiArg : CliArg
{
    public override string Description => L.Pick(
        "强制 TUI 全屏界面（默认即 TUI）",
        "Force the full-screen TUI (this is the default)");
    public override int ValueCount => 0;
    public TuiArg() : base("tui", "--tui") { }
}

public class DebugArg : CliArg
{
    public override string Description => L.Pick(
        "开启调试日志（记录到 logs/ 目录）",
        "Enable debug logging (writes to the logs/ directory)");
    public DebugArg() : base("debug", "-d", "--debug") { }
    public override int? OnMatch(List<string> values) { DebugLog.Enable(); return null; }
}

/// <summary>
/// --debug-dump：开启死机现场自动采集（阶段黑匣子 + 每分钟定时 dump + 冻结时强制 dump 到 logs/freeze_*.txt）。
/// 默认关闭；显式开启才采集，排查死机时用，平时无需关心也无需事后关闭。
/// </summary>
public class DebugDumpArg : CliArg
{
    public override string Description => L.Pick(
        "开启死机现场自动采集（黑匣子 + 定时/冻结 dump 到 logs/freeze_*.txt，排查死机用）",
        "Enable automatic freeze capture (black box + periodic/freeze dumps to logs/freeze_*.txt, for diagnosing hangs)");
    public DebugDumpArg() : base("debug-dump", "--debug-dump") { }
    public override int? OnMatch(List<string> values) { FreezeCapture.Enable(); return null; }
}

/// <summary>
/// --config 命令行配置（对标 /config 斜杠命令），无需进界面即可读写所有设置项。
///   --config                      → 列出全部
///   --config list                 → 同列出
///   --config get &lt;key&gt;      → 读取
///   --config set &lt;key&gt; &lt;v&gt; → 设置并写入 .env
///   --config &lt;key&gt; &lt;v&gt;  → set 简写
///   --config &lt;key&gt;           → get 简写
/// </summary>
public class ConfigArg : CliArg
{
    public override string Description => L.Pick("命令行配置", "Command-line configuration");
    public override int ValueCount => -1;
    public override bool Greedy => true;
    public override string? ValueLabel => L.Pick("项 [值]", "item [value]");
    public override (string Cmd, string Desc)[]? SubCommands =>
    [
        ("list", L.Pick("列出全部设置项", "List every setting")),
        ("get <key>", L.Pick("读取单项值", "Read one setting")),
        ("set <key> <value>", L.Pick("设置并写入 config.json", "Set it and write config.json")),
        ("<key> [value]", L.Pick("简写：查值或设置", "Shorthand: read or set")),
    ];
    public ConfigArg() : base("config", "-C", "--config") { }

    public override int? OnMatch(List<string> values)
    {
        string result;

        if (values.Count == 0)
            result = ConfigCli.List();
        else
        {
            var first = values[0].ToLowerInvariant();
            var rest = values.Skip(1).ToArray();
            switch (first)
            {
                case "list":
                case "ls":
                    result = ConfigCli.List();
                    break;
                case "get":
                    result = rest.Length == 0
                        ? L.Pick("用法: --config get <key>", "Usage: --config get <key>")
                        : ConfigCli.Get(rest[0]);
                    break;
                case "set":
                    result = rest.Length < 2
                        ? L.Pick("用法: --config set <key> <value>", "Usage: --config set <key> <value>")
                        : ConfigCli.Set(rest[0], string.Join(" ", rest.Skip(1)));
                    break;
                default:
                    // 简写：--config <key> [value]
                    result = rest.Length == 0 ? ConfigCli.Get(values[0]) : ConfigCli.Set(values[0], string.Join(" ", rest));
                    break;
            }
        }

        Console.WriteLine(result);
        return 0;
    }
}

public class HelpArg : CliArg
{
    public override string Description => L.Pick("显示此帮助", "Show this help");
    public HelpArg() : base("help", "-h", "--help") { }
}

/// <summary>指定 MCP 服务器配置文件路径（对标 Claude Code --mcp-config）</summary>
public class McpConfigArg : CliArg
{
    public override string Description => L.Pick(
        "指定 MCP 服务器配置文件路径",
        "Path to the MCP server config file");
    public override int ValueCount => 1;
    public override string? ValueLabel => L.Pick("路径", "path");
    public McpConfigArg() : base("mcp-config", "--mcp-config") { }
}

/// <summary>切换颜色主题（对标 Claude Code --theme）</summary>
public class ThemeArg : CliArg
{
    public override string Description => L.Pick(
        "切换颜色主题（ocean/forest/sunset/mono/cyberpunk）",
        "Switch the color theme (ocean/forest/sunset/mono/cyberpunk)");
    public override int ValueCount => 1;
    public override string? ValueLabel => L.Pick("名字", "name");
    public ThemeArg() : base("theme", "--theme") { }
}

// ═══════════════════════════════════════════════════════════════
// 增强参数
// ═══════════════════════════════════════════════════════════════

/// <summary>静默模式：抑制横幅等非必要输出</summary>
public class QuietArg : CliArg
{
    public override string Description => L.Pick(
        "静默模式：抑制横幅等非必要输出",
        "Quiet mode: suppress the banner and other non-essential output");
    public QuietArg() : base("quiet", "-q", "--quiet") { }
}

/// <summary>禁用 ANSI 颜色输出</summary>
public class NoColorArg : CliArg
{
    public override string Description => L.Pick("禁用 ANSI 颜色输出", "Disable ANSI color output");
    public NoColorArg() : base("no-color", "--no-color") { }
}

/// <summary>MCP 服务器管理 CLI（无参列出，reload [name] 重连）</summary>
public class McpArg : CliArg
{
    public override string Description => L.Pick(
        "MCP 服务器管理（无参列出，reload [name] 重连）",
        "MCP server management (no argument lists them, reload [name] reconnects)");
    public override int ValueCount => -1;
    public override bool Greedy => true;
    public override string? ValueLabel => L.Pick("子命令", "subcommand");
    public override (string Cmd, string Desc)[]? SubCommands =>
    [
        (L.Pick("(无参)", "(no args)"), L.Pick("列出 MCP 服务器状态", "List MCP server status")),
        ("reload [name]", L.Pick("重连 MCP 服务器", "Reconnect an MCP server")),
    ];
    public McpArg() : base("mcp", "--mcp") { }
    public override int? OnMatch(List<string> values) => McpCli.Run(values);
}

/// <summary>编程知识库 CLI（mine 提炼经验 / review 间隔重复自测 / weak 薄弱点统计）</summary>
public class KbArg : CliArg
{
    public override string Description => L.Pick(
        "编程知识库（mine 提炼经验 / review 间隔重复自测 / weak 薄弱点统计）",
        "Programming knowledge base (mine extracts lessons / review does spaced-repetition self-tests / weak shows weakness stats)");
    public override int ValueCount => -1;
    public override bool Greedy => true;
    public override string? ValueLabel => L.Pick("子命令", "subcommand");
    public override (string Cmd, string Desc)[]? SubCommands =>
    [
        ("mine [N]", L.Pick("从 git 历史提炼经验条目（默认 20）", "Extract lessons from git history (default 20)")),
        (L.Pick("diagnose <报错>", "diagnose <error>"), L.Pick("诊断报错（召回知识库 + git 修复史）", "Diagnose an error (recalls the knowledge base + git fix history)")),
        ("path", L.Pick("生成学习路径（欠缺→进阶，接入 /kb review）", "Generate a learning path (gaps -> advanced, feeds /kb review)")),
        ("profile [json]", L.Pick("技能画像（json 导出）", "Skill profile (json export)")),
        ("retro", L.Pick("复盘本次会话提炼经验", "Retrospect this session and extract lessons")),
        ("review", L.Pick("间隔重复自测一条到期经验", "Spaced-repetition self-test for one due lesson")),
        ("weak", L.Pick("欠缺知识清单 + 薄弱点统计", "Gap list + weakness stats")),
        ("list", L.Pick("列出全部经验条目", "List every lesson entry")),
    ];
    public KbArg() : base("kb", "--kb") { }
    public override int? OnMatch(List<string> values) => KbCli.Run(values);
}

/// <summary>清空当前会话历史（对标 /reset 斜杠命令）</summary>
public class ResetArg : CliArg
{
    public override string Description => L.Pick("清空当前会话历史", "Clear the current session history");
    public ResetArg() : base("reset", "--reset") { }
    public override int? OnMatch(List<string> values)
    {
        var agent = ProgramContext.Agent;
        if (agent == null) { Console.WriteLine(L.Pick("无活跃会话（--reset 需配合 -p 提示词或 TUI 使用）", "No active session (--reset needs -p or the TUI)")); return 0; }
        agent.Reset();
        Console.WriteLine(L.Pick("♻ 对话已重置", "♻ Conversation reset"));
        return 0;
    }
}

/// <summary>清理缓存文件（file-tracker/todos/trajectory）</summary>
public class PurgeArg : CliArg
{
    public override string Description => L.Pick(
        "清理缓存文件（file-tracker/todos/trajectory）",
        "Purge cache files (file-tracker/todos/trajectory)");
    public PurgeArg() : base("purge", "--purge") { }
    public override int? OnMatch(List<string> values) => CachePurger.Run();
}
