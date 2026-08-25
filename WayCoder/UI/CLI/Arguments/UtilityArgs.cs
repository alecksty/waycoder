using WayCoder.Tools;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Cli.Arguments;

// ═══════════════════════════════════════════════════════════════
// 标志参数
// ═══════════════════════════════════════════════════════════════

public class VersionArg : CliArg
{
    public override string Description => "显示版本信息";
    public VersionArg() : base("version", "-v", "--version") { }
}

public class InitArg : CliArg
{
    public override string Description => "初始化项目配置（.waycoder/ 目录）";
    public InitArg() : base("init", "-i", "--init") { }
}

public class WatchArg : CliArg
{
    public override string Description => "Watch 模式（监听文件中的 AI! 注释）";
    public WatchArg() : base("watch", "-w", "--watch") { }
}

public class TinyArg : CliArg
{
    public override string Description => "Tiny 模式（精简提示词 + 小窗口；可指定如 --tiny 8k，缺省自动探测，失败回退 4K）";
    public override int ValueCount => -1;
    public override string? ValueLabel => "窗口";
    public TinyArg() : base("tiny", "-tt", "--test-tiny", "--tiny") { }
}

public class EconomyArg : CliArg
{
    public override string Description => "省 Token 模式（--economy [on|auto|off]，缺省 on；auto 按任务复杂度动态调节阈值）";
    public override int ValueCount => -1;
    public override string? ValueLabel => "模式";
    public EconomyArg() : base("economy", "-e", "--economy") { }
}

/// <summary>
/// --edit &lt;文件路径&gt; —— 启动后直接进入终端编辑器打开指定文件。
/// 等价于进入界面后执行 /edit 文件路径（Esc/Ctrl+Q 退出回到聊天界面）。
/// </summary>
public class EditArg : CliArg
{
    public override string Description => "直接进入编辑器打开文件（--edit <文件路径>）";
    public override int ValueCount => 1;
    public override string? ValueLabel => "文件路径";
    public EditArg() : base("edit", "--edit") { }
}

public class UpdateArg : CliArg
{
    public override string Description => "检查并自动升级到最新版本（优先 GitHub、回退 Gitee）";
    public UpdateArg() : base("update", "--update") { }
}

public class JsonArg : CliArg
{
    public override string Description => "JSON 输出模式（配合 -p 一次性模式，stdout 输出结构化 JSON，供 IDE/脚本解析）";
    public JsonArg() : base("json", "-j", "--json") { }
}

public class WebArg : CliArg
{
    public override string Description => "浏览器聊天界面（--web [端口]，默认 9527，自动打开浏览器）";
    public override int ValueCount => -1; // 可选端口
    public override string? ValueLabel => "端口";
    public WebArg() : base("web", "--web") { }
}

public class TuiArg : CliArg
{
    public override string Description => "强制 TUI 全屏界面（默认即 TUI）";
    public override int ValueCount => 0;
    public TuiArg() : base("tui", "--tui") { }
}

public class DebugArg : CliArg
{
    public override string Description => "开启调试日志（记录到 logs/ 目录）";
    public DebugArg() : base("debug", "-d", "--debug") { }
    public override int? OnMatch(List<string> values) { DebugLog.Enable(); return null; }
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
    public override string Description => "命令行配置";
    public override int ValueCount => -1;
    public override bool Greedy => true;
    public override string? ValueLabel => "项 [值]";
    public override (string Cmd, string Desc)[]? SubCommands =>
    [
        ("list", "列出全部设置项"),
        ("get <key>", "读取单项值"),
        ("set <key> <value>", "设置并写入 config.json"),
        ("<key> [value]", "简写：查值或设置"),
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
                    result = rest.Length == 0 ? "用法: --config get <key>" : ConfigCli.Get(rest[0]);
                    break;
                case "set":
                    result = rest.Length < 2 ? "用法: --config set <key> <value>" : ConfigCli.Set(rest[0], string.Join(" ", rest.Skip(1)));
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
    public override string Description => "显示此帮助";
    public HelpArg() : base("help", "-h", "--help") { }
}

/// <summary>指定 MCP 服务器配置文件路径（对标 Claude Code --mcp-config）</summary>
public class McpConfigArg : CliArg
{
    public override string Description => "指定 MCP 服务器配置文件路径";
    public override int ValueCount => 1;
    public override string? ValueLabel => "路径";
    public McpConfigArg() : base("mcp-config", "--mcp-config") { }
}

/// <summary>切换颜色主题（对标 Claude Code --theme）</summary>
public class ThemeArg : CliArg
{
    public override string Description => "切换颜色主题（ocean/forest/sunset/mono/cyberpunk）";
    public override int ValueCount => 1;
    public override string? ValueLabel => "名字";
    public ThemeArg() : base("theme", "--theme") { }
}

// ═══════════════════════════════════════════════════════════════
// 增强参数
// ═══════════════════════════════════════════════════════════════

/// <summary>静默模式：抑制横幅等非必要输出</summary>
public class QuietArg : CliArg
{
    public override string Description => "静默模式：抑制横幅等非必要输出";
    public QuietArg() : base("quiet", "-q", "--quiet") { }
}

/// <summary>禁用 ANSI 颜色输出</summary>
public class NoColorArg : CliArg
{
    public override string Description => "禁用 ANSI 颜色输出";
    public NoColorArg() : base("no-color", "--no-color") { }
}

/// <summary>MCP 服务器管理 CLI（无参列出，reload [name] 重连）</summary>
public class McpArg : CliArg
{
    public override string Description => "MCP 服务器管理（无参列出，reload [name] 重连）";
    public override int ValueCount => -1;
    public override bool Greedy => true;
    public override string? ValueLabel => "子命令";
    public override (string Cmd, string Desc)[]? SubCommands =>
    [
        ("(无参)", "列出 MCP 服务器状态"),
        ("reload [name]", "重连 MCP 服务器"),
    ];
    public McpArg() : base("mcp", "--mcp") { }
    public override int? OnMatch(List<string> values) => McpCli.Run(values);
}

/// <summary>编程知识库 CLI（mine 提炼经验 / review 间隔重复自测 / weak 薄弱点统计）</summary>
public class KbArg : CliArg
{
    public override string Description => "编程知识库（mine 提炼经验 / review 间隔重复自测 / weak 薄弱点统计）";
    public override int ValueCount => -1;
    public override bool Greedy => true;
    public override string? ValueLabel => "子命令";
    public override (string Cmd, string Desc)[]? SubCommands =>
    [
        ("mine [N]", "从 git 历史提炼经验条目（默认 20）"),
        ("diagnose <报错>", "诊断报错（召回知识库 + git 修复史）"),
        ("profile", "技能画像"),
        ("retro", "复盘本次会话提炼经验"),
        ("review", "间隔重复自测一条到期经验"),
        ("weak", "欠缺知识清单 + 薄弱点统计"),
        ("list", "列出全部经验条目"),
    ];
    public KbArg() : base("kb", "--kb") { }
    public override int? OnMatch(List<string> values) => KbCli.Run(values);
}

/// <summary>清空当前会话历史（对标 /reset 斜杠命令）</summary>
public class ResetArg : CliArg
{
    public override string Description => "清空当前会话历史";
    public ResetArg() : base("reset", "--reset") { }
    public override int? OnMatch(List<string> values)
    {
        var agent = ProgramContext.Agent;
        if (agent == null) { Console.WriteLine("无活跃会话（--reset 需配合 -p 提示词或 TUI 使用）"); return 0; }
        agent.Reset();
        Console.WriteLine("♻ 对话已重置");
        return 0;
    }
}

/// <summary>清理缓存文件（file-tracker/todos/trajectory）</summary>
public class PurgeArg : CliArg
{
    public override string Description => "清理缓存文件（file-tracker/todos/trajectory）";
    public PurgeArg() : base("purge", "--purge") { }
    public override int? OnMatch(List<string> values) => CachePurger.Run();
}
