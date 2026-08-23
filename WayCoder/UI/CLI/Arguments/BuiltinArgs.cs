using WayCoder.Tools;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Cli.Arguments;

// ═══════════════════════════════════════════════════════════════
// 模型参数
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// --model 模型管理（对标 /model 斜杠命令）。
///   --model                        → 显示当前模型
///   --model list [关键词]          → 列出模型目录（内置 + 自定义合并）
///   --model name &lt;id&gt;        → 选中大模型并持久化（同步服务商 + base-url + 写 .env）
///   --model small &lt;id&gt;       → 选中小模型并持久化（同步小模型服务商）
///   --model key &lt;供应商&gt; &lt;key&gt; → 保存 API key（无参列出已存 keys）
///   --model connect &lt;base-url&gt; → 设置连接地址（写 .env）
///   --model import [来源]          → 导入外部模型库（opencode/openclaw/crush/claude/codex/文件，无参自动探测）
///   --model add [model|provider|key] → 手动添加模型 / 服务商 / API key
///   --model remove [model|provider|key] &lt;目标&gt; → 删除模型 / 服务商 / API key
///   --model test                   → 模型连通性测试（有 key 的 + 本地模型，哪些能连上）
///   --model &lt;模型ID&gt;          → 快捷选中（本次会话，不持久化，向后兼容）
/// </summary>
public class ModelArg : CliArg
{
    public override string Description => "模型管理";
    public override int ValueCount => -1;
    public override bool Greedy => true;
    public override string? ValueLabel => "模型ID/子命令";
    public override (string Cmd, string Desc)[]? SubCommands =>
    [
        ("list [供应商]", "列出模型目录（OpenRouter 显示短名）"),
        ("name <id>", "选中大模型"),
        ("small <id>", "选中小模型"),
        ("key [set|remove]", "管理 API Key（key 永不自动删除，删除需确认）"),
        ("check [connect]", "检查模型能力（think/tools/vision/格式/上下文/key）"),
        ("report", "测试所有 connect 连通性，生成可用/失败报告"),
        ("free", "测试所有 free 模型（zen -free / openrouter :free），列出可用"),
        ("import [alllocal|allonline|all|online <源>|来源]", "导入模型（alllocal=本地 / allonline=在线 / all=全部 / online <源>=指定端点）"),
        ("add model/provider/key", "添加模型 / 服务商 / Key"),
        ("remove <id>", "移除模型或服务商"),
        ("clean", "清理无效服务商/模型 + 合并重复 + 删无效 connect"),
        ("test", "测试连接"),
        ("connect <base-url>", "设置 API 地址"),
        ("<模型ID>", "快捷选中模型"),
    ];
    public ModelArg() : base("model", "-m", "--model") { }

    public override int? OnMatch(List<string> values)
    {
        if (values.Count == 0)
        {
            Console.WriteLine(ModelCli.Current());
            return 0;
        }

        var first = values[0].ToLowerInvariant();
        var rest = values.Skip(1).ToArray();

        string result;
        switch (first)
        {
            case "list":
            case "ls":
                result = ModelCli.List(rest.Length > 0 ? rest[0] : null);
                break;
            case "name":
                result = rest.Length == 0 ? "用法: --model name <模型ID>" : ModelCli.Select(rest[0]);
                break;
            case "small":
                result = rest.Length == 0 ? "用法: --model small <小模型ID>" : ModelCli.SelectSmall(rest[0]);
                break;
            case "key":
            case "keys":
                result = DispatchKey(rest);
                break;
            case "connect":
                result = rest.Length == 0 ? "用法: --model connect <base-url>" : ModelCli.Connect(rest[0]);
                break;
            case "check":
            case "caps":
            case "capabilities":
                result = ModelCli.Check(rest.Length > 0 ? rest[0] : null);
                break;
            case "report":
                result = ModelCli.Report(rest.Length > 1 ? rest[1] : null);
                break;
            case "free":
                result = ModelCli.Free(rest.Length > 1 ? rest[1] : null);
                break;
            case "import":
                // 组合命令：alllocal=全部本地(ollama/lmstudio/cc-switch)、allonline=全部在线端点、all/auto=本地+在线；
                // online [源名...]=在线拉取指定端点；其余走 Import（单源本地/配置文件/opencode 等）
                var src0 = rest.Length > 0 ? rest[0].ToLowerInvariant() : "";
                if (src0 is "all" or "auto")
                {
                    result = ModelCli.ImportLocalServices() + "\n" + ModelCli.ImportOnlineAll(null);
                }
                else if (src0 == "alllocal")
                {
                    result = ModelCli.ImportLocalServices();
                }
                else if (src0 is "online" or "allonline")
                {
                    var names = rest.Skip(1).ToArray();
                    result = ModelCli.ImportOnlineAll(names.Length > 0 ? names : null);
                }
                else
                {
                    result = ModelCli.Import(rest.Length > 0 ? rest[0] : null);
                }
                break;
            case "add":
                result = DispatchAdd(rest);
                break;
            case "remove":
            case "rm":
            case "delete":
            case "del":
                result = DispatchRemove(rest);
                break;
            case "test":
                result = ModelCli.Test();
                break;
            case "prune":
            case "clean":
            case "cleanup":
                // 统一清理：无效服务商（删 providers.json + key + 模型）+ 合并重复模型 + 删无效 connect
                result = ModelCli.ProviderCli.CleanText() + "\n" + ModelCli.Clean();
                break;
            default:
                // 裸模型名：本次会话快捷选中，交给 Program 继续运行
                return null;
        }

        Console.WriteLine(result);
        return 0;
    }

    static string DispatchKey(string[] rest)
    {
        if (rest.Length == 0) return ModelCli.ListKeys();
        if (rest.Length >= 3 && rest[0].Equals("set", StringComparison.OrdinalIgnoreCase))
            return ModelCli.SetKey(rest[1], string.Join(" ", rest.Skip(2)));
        if (rest.Length >= 2 && rest[0].Equals("remove", StringComparison.OrdinalIgnoreCase))
            return ModelCli.RemoveKey(rest[1]);
        if (rest.Length >= 2)
            return ModelCli.SetKey(rest[0], string.Join(" ", rest.Skip(1)));
        return "用法: --model key [set|remove] <供应商> <key>";
    }

    static string DispatchAdd(string[] rest)
    {
        if (rest.Length == 0)
            return "用法: --model add [model <id> <供应商ID> [baseUrl] | provider <供应商ID> [baseUrl] | key <供应商ID> <key>]";
        var sub = rest[0].ToLowerInvariant();
        switch (sub)
        {
            case "model":
                return rest.Length >= 2
                    ? ModelCli.AddModel(rest[1], rest.Length > 2 ? rest[2] : null, rest.Length > 3 ? rest[3] : null)
                    : "用法: --model add model <id> [<供应商ID> [baseUrl]]";
            case "provider":
            case "prov":
                return rest.Length >= 2
                    ? ModelCli.AddProvider(rest[1], rest.Length > 2 ? rest[2] : null)
                    : "用法: --model add provider <供应商ID> [baseUrl]";
            case "key":
            case "keys":
            case "apikey":
                return rest.Length >= 3
                    ? ModelCli.SetKey(rest[1], rest[2])
                    : "用法: --model add key <供应商ID> <key>";
            default:
                // 无子命令：add <id> <供应商ID> [baseUrl]
                return rest.Length >= 2
                    ? ModelCli.AddModel(rest[0], rest[1], rest.Length > 2 ? rest[2] : null)
                    : ModelCli.AddModel(rest[0], null, null);
        }
    }

    static string DispatchRemove(string[] rest)
    {
        if (rest.Length == 0)
            return "用法: --model remove [model <id> | provider <pid> | key <pid>]（无子命令时 <id> 视为删除模型）";
        var sub = rest[0].ToLowerInvariant();
        if (rest.Length >= 2 && sub is "model")
            return ModelCli.Remove(rest[1]);
        if (rest.Length >= 2 && sub is "provider" or "prov")
            return ModelCli.RemoveProvider(rest[1]);
        if (rest.Length >= 2 && sub is "key" or "keys" or "apikey")
            return ModelCli.RemoveKey(rest[1]);
        // 无子命令：rest[0] 即模型 id（向后兼容 --model remove <id>）
        return ModelCli.Remove(rest[0]);
    }
}

public class BaseUrlArg : CliArg
{
    public override string Description => "API 基础 URL";
    public override int ValueCount => 1;
    public override string? ValueLabel => "URL";
    public BaseUrlArg() : base("base-url", "-b", "--base-url") { }
}

public class ApiKeyArg : CliArg
{
    public override string Description => "API 密钥（自动保存到全局 ~/.waycoder/api_keys.json，按当前服务商）";
    public override int ValueCount => 1;
    public override string? ValueLabel => "密钥";
    public ApiKeyArg() : base("api-key", "-k", "--api-key") { }
}

// ═══════════════════════════════════════════════════════════════
// 会话参数
// ═══════════════════════════════════════════════════════════════

public class PromptArg : CliArg
{
    public override string Description => "一次性提示词。-p1~-p0 投递槽位, -pa 共享前缀, 同槽位可排队";
    public override int ValueCount => 1;
    public override string? ValueLabel => "文本";
    // --print 别名（-p/--print），OpenCode 对应 run <message>
    public PromptArg() : base("prompt", "-p", "--prompt", "--print") { }
}

public class ResumeArg : CliArg
{
    public override string Description => "恢复会话,会话名为空,就是上一次的。";
    public override int ValueCount => -1; // 可选值：无参时恢复最近会话
    public override string? ValueLabel => "会话名";
    public ResumeArg() : base("resume", "-r", "--resume", "-c", "--continue") { }
}

public class MaxBudgetArg : CliArg
{
    public override string Description => "费用上限（美元），超支自动停止";
    public override int ValueCount => 1;
    public override string? ValueLabel => "金额";
    public MaxBudgetArg() : base("max-budget-usd", "-B", "--max-budget-usd") { }
}

public class MaxRequeueArg : CliArg
{
    public override string Description => "撞轮次上限后自动压缩+续跑次数（0=关闭，默认 3，超长任务可调大）";
    public override int ValueCount => 1;
    public override string? ValueLabel => "次数";
    public MaxRequeueArg() : base("max-requeue", "--max-requeue") { }
}

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

public class YoloArg : CliArg
{
    public override string Description => "跳过所有权限确认（非交互模式自动开启）";
    // --dangerously-skip-permissions 别名
    public YoloArg() : base("yolo", "-y", "--yolo", "--dangerously-skip-permissions") { }
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

public class CliModeArg : CliArg
{
    public override string Description => "强制 CLI 文本界面（非全屏，逐行交互）";
    public override int ValueCount => 0;
    public CliModeArg() : base("cli", "--cli") { }
}

// ═══════════════════════════════════════════════════════════════
// 兼容参数别名 —— 仅新增别名，不动现有参数
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// --output-format &lt;text|json|stream-json&gt;（）/ --format &lt;default|json&gt;（OpenCode）。
/// json/stream-json 对应 WayCoder 的 --json 输出模式。
/// </summary>
public class OutputFormatArg : CliArg
{
    public override string Description => "输出格式：json|stream-json 等同 --json，text|default 普通输出";
    public override int ValueCount => 1;
    public override string? ValueLabel => "格式";
    public OutputFormatArg() : base("output-format", "--output-format", "--format") { }
}

/// <summary>
/// --permission-mode &lt;default|acceptEdits|plan|bypassPermissions&gt;（）。
/// bypassPermissions → --yolo；其余保持默认权限确认。
/// </summary>
public class PermissionModeArg : CliArg
{
    public override string Description => "权限模式：bypassPermissions 等同 --yolo";
    public override int ValueCount => 1;
    public override string? ValueLabel => "模式";
    public PermissionModeArg() : base("permission-mode", "--permission-mode") { }
}

/// <summary>工具白名单（--allowedTools / --allowed-tools，空格分隔）</summary>
public class AllowedToolsArg : CliArg
{
    public override string Description => "工具白名单（空格/逗号分隔），等同 WAYCODER_ALLOWED_TOOLS";
    public override int ValueCount => -1;
    public override string? ValueLabel => "工具名";
    public override bool Greedy => true; // 空格分隔多值
    public AllowedToolsArg() : base("allowed-tools", "--allowedTools", "--allowed-tools") { }
}

/// <summary>工具黑名单（--disallowedTools / --disallowed-tools，空格分隔）</summary>
public class DisallowedToolsArg : CliArg
{
    public override string Description => "工具黑名单（空格/逗号分隔），等同 WAYCODER_DISABLED_TOOLS";
    public override int ValueCount => -1;
    public override string? ValueLabel => "工具名";
    public override bool Greedy => true;
    public DisallowedToolsArg() : base("disallowed-tools", "--disallowedTools", "--disallowed-tools") { }
}

/// <summary>
/// --system-prompt &lt;text&gt; / --append-system-prompt &lt;text&gt;（）。
/// WayCoder 系统提示词为结构化基础提示，此处实现为追加（整体替换会丢失结构）。
/// </summary>
public class SystemPromptArg : CliArg
{
    public override string Description => "追加到系统提示词的文本（--append-system-prompt 为别名）";
    public override int ValueCount => 1;
    public override string? ValueLabel => "文本";
    public SystemPromptArg() : base("system-prompt", "--system-prompt", "--append-system-prompt") { }
}

/// <summary>按会话 id 恢复（OpenCode --session / --resume-session-id / --session-id）</summary>
public class SessionArg : CliArg
{
    public override string Description => "按会话 id 恢复，等同 --resume <id>";
    public override int ValueCount => 1;
    public override string? ValueLabel => "会话ID";
    public SessionArg() : base("session", "--session", "--resume-session-id", "--session-id") { }
}

// ═══════════════════════════════════════════════════════════════
// 批量任务引擎 —— 多仓库并行处理（worktree 隔离）
// ═══════════════════════════════════════════════════════════════

public class BatchArg : CliArg
{
    public override string Description => "批量任务引擎：多仓库并行处理（--batch <JSON文件|内联JSON>，每个任务在独立克隆副本中隔离执行）";
    public override int ValueCount => 1;
    public override string? ValueLabel => "JSON";
    public BatchArg() : base("batch", "--batch") { }
}

public class BatchRepoArg : CliArg
{
    public override string Description => "批量任务：添加一个仓库（可重复，配合 --batch-task 共享任务）";
    public override int ValueCount => 1;
    public override string? ValueLabel => "仓库";
    public override bool AllowMultiple => true;
    public BatchRepoArg() : base("batch-repo", "--batch-repo") { }
}

public class BatchTaskArg : CliArg
{
    public override string Description => "批量任务：所有 --batch-repo 仓库的共享任务";
    public override int ValueCount => 1;
    public override string? ValueLabel => "文本";
    public BatchTaskArg() : base("batch-task", "--batch-task") { }
}

public class BatchKeepArg : CliArg
{
    public override string Description => "批量任务：保留克隆的工作副本（默认执行后清理）";
    public BatchKeepArg() : base("batch-keep", "--batch-keep") { }
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

// ═══════════════════════════════════════════════════════════════
// 工具/动作参数（匹配后立即执行并退出）
// ═══════════════════════════════════════════════════════════════

#if WAYCODER_TEST
public class TestArg : CliArg
{
    public override string Description => "运行自测（可选指定模块名）";
    public override int ValueCount => -1;
    public override string? ValueLabel => "模块名";
    public TestArg() : base("test", "-t", "--test") { }
    public override int? OnMatch(List<string> values)
    {
        if (values.Count > 0)
            Console.WriteLine(SelfTest.RunModule(values[0]));
        else
            SelfTest.Run();
        return 0;
    }
}

public class BenchmarkArg : CliArg
{
    public override string Description => "运行性能测评";
    public BenchmarkArg() : base("bench", "-tb", "--test-benchmark", "--bench", "--perf") { }
    public override int? OnMatch(List<string> values) { Benchmark.Run(); return 0; }
}

public class LimitsArg : CliArg
{
    public override string Description => "运行系统上限报告（扫描所有硬编码上限）";
    public LimitsArg() : base("limits", "-tl", "--test-limits", "--limits") { }
    public override int? OnMatch(List<string> values) { Benchmark.LimitsReport(); return 0; }
}
#endif // WAYCODER_TEST

public class ScreenshotArg : CliArg
{
    public override string Description => "截图模式";
    public override bool Internal => true;
    public ScreenshotArg() : base("screenshot", "-x", "--screenshot") { }
    public override int? OnMatch(List<string> values) { Program.RunScreenshot(); return 0; }
}

/// <summary>--sysprompt-size：对比各模式 SystemPrompt + 工具 schema 大小（省钱模式效果验证）。</summary>
public class SyspromptSizeArg : CliArg
{
    public override string Description => "对比 SystemPrompt 各模式大小（省钱验证）";
    public override bool Internal => true;
    public SyspromptSizeArg() : base("sysprompt-size", "--sysprompt-size") { }
    public override int? OnMatch(List<string> values)
    {
        Program.RunSyspromptSize();
        return 0;
    }
}

/// <summary>
/// --width-probe [目录]：终端实测字符宽度，与静态宽度表 AnsiString.CharWidth 比对，输出不一致项供校准。
/// 用「+字符*」+ CPR 光标位置查询测量真实显示列宽（需真实 TTY）。
/// 无目录 → 测内置代表字符集（ProbeSet）；带目录 → 扫描该目录源码中全部非 ASCII 字符逐个实测。
/// </summary>
public class WidthProbeArg : CliArg
{
    public override string Description => "终端实测字符宽度，与静态宽度表比对校准（可带目录扫描源码字符）";
    public override bool Internal => true;
    public override int ValueCount => -1;
    public override string? ValueLabel => "目录";
    public WidthProbeArg() : base("width-probe", "--width-probe", "-wp") { }
    public override int? OnMatch(List<string> values)
        => UI.Shared.Terminal.TerminalWidthProbe.PrintReport(values.Count > 0 ? values[0] : null);
}

#if WAYCODER_TEST
public class TuiDemoArg : CliArg
{
    public override string Description => "TUI 控件演示";
    public override bool Internal => true;
    public TuiDemoArg() : base("tui-demo", "-u", "--tui-demo") { }
    public override int? OnMatch(List<string> values) { TuiDemo.Run(); return 0; }
}

public class TuiAuditArg : CliArg
{
    public override string Description => "TUI 对话框/控件渲染审计（输出纯文本帧）";
    public override bool Internal => true;
    public TuiAuditArg() : base("tui-audit", "--tui-audit") { }
    public override int? OnMatch(List<string> values) { TuiAudit.Run(); return 0; }
}

public class DialogShowArg : CliArg
{
    public override string Description => "对话框仅绘制演示（1~6 行消息 + 指定位置，抓屏核对布局）";
    public override bool Internal => true;
    public DialogShowArg() : base("dialog-show", "--dialog-show") { }
    public override int? OnMatch(List<string> values) { DialogShow.Run(); return 0; }
}

public class TuiPreviewArg : CliArg
{
    public override string Description => "预览 .tui 标记文件（声明式 TUI 布局）";
    public override bool Internal => true;
    public override int ValueCount => 1;
    public override string? ValueLabel => "标记文件";
    public TuiPreviewArg() : base("tui-preview", "--tui-preview") { }
    public override int? OnMatch(List<string> values)
    {
        var path = values.Count > 0 ? values[0] : null;
        return TuiPreview.Run(path ?? "");
    }
}

public class TuiWatchArg : CliArg
{
    public override string Description => "实时预览 .tui（保存即刷新，边写边预览）";
    public override bool Internal => true;
    public override int ValueCount => 1;
    public override string? ValueLabel => "标记文件";
    public TuiWatchArg() : base("tui-watch", "--tui-watch") { }
    public override int? OnMatch(List<string> values)
    {
        var path = values.Count > 0 ? values[0] : null;
        return TuiPreview.Watch(path ?? "");
    }
}

public class TuiMarkupDemoArg : CliArg
{
    public override string Description => "声明式 TUI 演示（tuidemo/*.tui 重构聊天界面与对话框）";
    public override bool Internal => true;
    public TuiMarkupDemoArg() : base("tui-markup-demo", "--tui-markup-demo") { }
    public override int? OnMatch(List<string> values) { TuiMarkupDemo.Run(); return 0; }
}
#endif // WAYCODER_TEST

/// <summary>用 .tui 标记版聊天界面启动（等价 WAYCODER_MARKUP_UI=1，供测试标记版界面）。</summary>
public class TuiChatArg : CliArg
{
    public override string Description => "用 .tui 标记版聊天界面启动（等价 WAYCODER_MARKUP_UI=1）";
    public override bool Internal => true;
    public TuiChatArg() : base("tui-chat", "--tui-chat") { }
    public override int? OnMatch(List<string> values) { Program.MarkupChatOverride = true; return null; }
}

public class GuiArg : CliArg
{
    public override string Description => "启动图形界面（独立 Avalonia 进程）";
    public GuiArg() : base("gui", "-g", "--gui") { }
    public override int? OnMatch(List<string> values)
    {
        // GUI 是独立 JIT 进程（WayCoder.Gui 项目），主程序 AOT 无法内嵌，需拉起其可执行文件
        var exeName = OperatingSystem.IsWindows() ? "waycoder-gui.exe" : "waycoder-gui";
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, exeName),                       // 已发布（与主程序同级）
            Path.Combine(baseDir, "waycoder-gui.dll"),            // 开发态（dotnet 运行）
            Path.Combine(baseDir, "..", "..", "WayCoder.Gui", "bin", "Debug", "net10.0", "waycoder-gui.dll"),
        };

        string? target = null;
        foreach (var c in candidates)
        {
            if (File.Exists(c)) { target = c; break; }
        }

        if (target == null)
        {
            Console.Error.WriteLine("未找到 GUI 可执行文件。请先构建 GUI 项目：dotnet build WayCoder.Gui");
            return 1;
        }

        try
        {
            // dll 走 dotnet 宿主；可执行文件直接启动
            var psi = target.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                ? new System.Diagnostics.ProcessStartInfo("dotnet", $"\"{target}\"") { UseShellExecute = false }
                : new System.Diagnostics.ProcessStartInfo(target) { UseShellExecute = true };
            System.Diagnostics.Process.Start(psi);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"启动 GUI 失败: {ex.Message}");
            return 1;
        }
    }
}

#if WAYCODER_TEST
public class KeypadArg : CliArg
{
    public override string Description => "按键脚本驱动 TUI（KEY/TEXT/DELAY/SNAP/DIALOG）+ 帧截图";
    public override int ValueCount => 1;
    public override string? ValueLabel => "脚本文件";
    public KeypadArg() : base("keypad", "--keypad") { }
    public override int? OnMatch(List<string> values)
    {
        var path = values.Count > 0 ? values[0] : null;
        if (string.IsNullOrWhiteSpace(path))
        {
            Console.Error.WriteLine("用法: waycoder --keypad <脚本文件>");
            return 1;
        }
        return Keypad.Run(path);
    }
}
#endif // WAYCODER_TEST

public class ThemeVerifyArg : CliArg
{
    public override string Description => "主题配色验证";
    public override bool Internal => true;
    public ThemeVerifyArg() : base("theme-verify", "-z", "--theme-verify") { }
    public override int? OnMatch(List<string> values) { ThemeVerify.Run(); return 0; }
}

// ═══════════════════════════════════════════════════════════════
// 槽位任务参数 — -p1 ~ -p0 对应 F1~F10
// ═══════════════════════════════════════════════════════════════

/// <summary>所有槽位任务的共享前缀（-pa "前缀" → 自动拼到每个 -pN 任务前面）</summary>
public class SlotPromptAllArg : CliArg
{
    public override string Description => "所有槽位任务的共享前缀（自动拼到每个 -pN 前面）";
    public override int ValueCount => 1;
    public override string? ValueLabel => "前缀";
    public SlotPromptAllArg() : base("prompt-all", "-pa", "--prompt-all") { }
}

public class SlotPromptArg : CliArg
{
    /// <summary>目标槽位索引（0-based，-p1→0, -p2→1, ..., -p0→9）</summary>
    public int SlotIndex { get; }
    public override string Description => $"投递任务到槽位 F{SlotIndex + 1}（-p1~-p9, -p0=F10）";
    public override int ValueCount => 1;
    public override string? ValueLabel => "文本";
    public override bool Internal => true; // 10 个参数不逐行显示
    public override bool AllowMultiple => true; // 同一槽位多次 -pN 可排队

    /// <param name="slotNum">用户输入的槽位号（1-9, 0=10），内部转为 0-based 索引</param>
    public SlotPromptArg(int slotNum) : base(
        $"slot-prompt-{slotNum}",
        $"-p{slotNum}",
        $"--prompt-slot-{slotNum}")
    {
        SlotIndex = slotNum switch
        {
            0 => 9,       // -p0 → F10 → 索引 9
            _ => slotNum - 1,  // -p1 → F1 → 索引 0, ...
        };
    }
}

public class SessionListArg : CliArg
{
    public override string Description => "列出所有已保存会话";
    public override int ValueCount => 0;
    public SessionListArg() : base("session-list", "-s", "--session-list", "--sessions") { }
}

// ═══════════════════════════════════════════════════════════════
// 竞品对标参数（Claude Code / Aider / OpenCode 主要参数）
// ═══════════════════════════════════════════════════════════════

/// <summary>对话最大轮次上限（对标 Claude Code --max-turns）</summary>
public class MaxTurnsArg : CliArg
{
    public override string Description => "对话最大轮次上限";
    public override int ValueCount => 1;
    public override string? ValueLabel => "次数";
    public MaxTurnsArg() : base("max-turns", "--max-turns") { }
}

/// <summary>自动 git 提交开关（对标 Aider / OpenCode，缺省 on）</summary>
public class AutoCommitArg : CliArg
{
    public override string Description => "自动 git 提交开关（on|off，缺省 on）";
    public override int ValueCount => -1;
    public override string? ValueLabel => "on|off";
    public AutoCommitArg() : base("auto-commit", "--auto-commit") { }
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

/// <summary>管理连接层（connect 注册表 / 命名连接 / 回退链）——list/add/rm/select/&lt;id&gt;/test/import。</summary>
public class ConnectArg : CliArg
{
    public override string Description => "连接层管理（connect 注册表 / 命名连接 / 回退链）";
    public override int ValueCount => -1;
    public override bool Greedy => true;
    public override string? ValueLabel => "connect名/子命令";
    public override (string Cmd, string Desc)[]? SubCommands =>
    [
        ("list", "列出 connect + 命名连接 + 回退链"),
        ("add <name> <providerId> <modelId>", "新增 connect"),
        ("rm <name>", "删除 connect"),
        ("select <id>", "切换大 connect（connect名 | providerId.modelId | providerId/modelId | baseUrl:model | modelId）"),
        ("<id>", "快捷切换（同 select）"),
        ("test", "连通性测试"),
        ("import", "登记当前大/小/回退模型为 connect"),
        ("conn add <name> <大> <小>", "新增命名连接（大/小一起切）"),
        ("use <name>", "切换命名连接"),
        ("chain <c1> <c2> ...", "设置全局回退链（connect 名）"),
    ];
    public ConnectArg() : base("connect", "--connect") { }
    public override int? OnMatch(List<string> values)
    {
        if (values.Count == 0)
        {
            var cfg = Config.Instance;
            var cur = ConnectionConfig.CurrentByConfig();
            Console.WriteLine(cur != null
                ? $"当前连接：`{cur.Name}`\n  大 = {ConnectionConfig.FormatModel(ConnectionConfig.FindConnect(cur.BigConnect)?.ProviderId ?? "", ConnectionConfig.FindConnect(cur.BigConnect)?.ModelId ?? "")}\n  小 = {ConnectionConfig.FormatModel(ConnectionConfig.FindConnect(cur.SmallConnect)?.ProviderId ?? "", ConnectionConfig.FindConnect(cur.SmallConnect)?.ModelId ?? "")}"
                : $"当前：大={ConnectionConfig.FormatModel(cfg.Provider, cfg.Model)} · 小={ConnectionConfig.FormatModel(cfg.SmallProvider, cfg.SmallModel)}");
            return 0;
        }

        var first = values[0].ToLowerInvariant();
        var rest = values.Skip(1).ToArray();
        switch (first)
        {
            case "list":
            case "ls":
            {
                var sb = new System.Text.StringBuilder("**connect 注册表**：\n");
                foreach (var c in ConnectionConfig.ListConnects())
                {
                    var hasKey = !string.IsNullOrEmpty(ConnectionConfig.ResolveProvider(c.ProviderId)?.ApiKey);
                    sb.AppendLine($"  `{c.Name}` {c.ProviderId} / {c.ModelId}" + (hasKey ? " 🔑" : " ⚠"));
                }
                var chain = ConnectionConfig.FallbackChain;
                sb.AppendLine($"回退链：{(chain.Count > 0 ? string.Join(" → ", chain) : "（无）")}");
                Console.WriteLine(sb.ToString().TrimEnd());
                return 0;
            }
            case "add":
            case "new":
                if (rest.Length < 3) { Console.WriteLine("用法: --connect add <name> <providerId> <modelId>"); return 1; }
                // 先尝试从官方环境变量自动导入 key（无 key 时），再建 connect
                var keyHint = ConnectionConfig.AutoImportKeyFromEnv(rest[1]);
                Console.WriteLine(ConnectionConfig.AddConnect(rest[0], rest[1], rest[2], out var e)
                    ? $"✅ 已新增 connect「{rest[0]}」{keyHint ?? ""}" : $"❌ {e}");
                return 0;
            case "rm":
            case "remove":
            case "delete":
            case "del":
                if (rest.Length < 1) { Console.WriteLine("用法: --connect rm <name>"); return 1; }
                Console.WriteLine(ConnectionConfig.RemoveConnect(rest[0], out var re)
                    ? $"🗑 已删除 connect「{rest[0]}」" : $"❌ {re}");
                return 0;
            case "select":
            case "switch":
                if (rest.Length < 1) { Console.WriteLine("用法: --connect select <id>"); return 1; }
                ConnectionConfig.ApplySpec(rest[0], true, out var sm);
                Console.WriteLine($"✅ {sm}");
                return 0;
            case "test":
            {
                var sb = new System.Text.StringBuilder("**connect 连通性测试**：\n");
                foreach (var pid in ConnectionConfig.ListConnects().Select(c => c.ProviderId).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var p = ConnectionConfig.ResolveProvider(pid);
                    if (string.IsNullOrWhiteSpace(p?.BaseUrl)) { sb.AppendLine($"  ❌ `{pid}` — 无端点"); continue; }
                    var (o, d) = ModelCli.ProbeEndpoint(p!.BaseUrl, p.ApiKey);
                    sb.AppendLine($"  {(o ? "✅" : "❌")} `{pid}` {p.BaseUrl} — {d}");
                }
                Console.WriteLine(sb.ToString().TrimEnd());
                return 0;
            }
            case "import":
            {
                ConnectionConfig.FindOrCreateConnect(Config.Instance.Provider, Config.Instance.Model);
                ConnectionConfig.FindOrCreateConnect(Config.Instance.SmallProvider, Config.Instance.SmallModel);
                Console.WriteLine($"✅ 已登记大/小模型为 connect：{ConnectionConfig.FormatModel(Config.Instance.Provider, Config.Instance.Model)} / {ConnectionConfig.FormatModel(Config.Instance.SmallProvider, Config.Instance.SmallModel)}");
                return 0;
            }
            case "use":
                if (rest.Length < 1) { Console.WriteLine("用法: --connect use <connectionName>"); return 1; }
                ConnectionConfig.ActivateConnection(rest[0], out var um);
                Console.WriteLine($"✅ {um}");
                return 0;
            case "chain":
            case "fallback":
                if (rest.Length > 0 && (rest[0].Equals("on", StringComparison.OrdinalIgnoreCase) || rest[0].Equals("off", StringComparison.OrdinalIgnoreCase)))
                {
                    Config.Instance.FallbackEnabled = rest[0].Equals("on", StringComparison.OrdinalIgnoreCase);
                    Config.Instance.SaveToEnvFile();
                    Console.WriteLine($"✅ 回退链已{(Config.Instance.FallbackEnabled ? "开启" : "关闭")}");
                    return 0;
                }
                ConnectionConfig.SetFallbackChain(rest);
                Console.WriteLine($"✅ 回退链：{string.Join(" → ", ConnectionConfig.FallbackChain)}" +
                    (Config.Instance.FallbackEnabled ? "（开）" : "（关，/connect chain on 开启）"));
                return 0;
            default:
                // <id> → 快捷切换
                ConnectionConfig.ApplySpec(values[0], true, out var dm);
                Console.WriteLine($"✅ {dm}");
                return 0;
        }
    }
}

/// <summary>管理服务商数据库（providers.json）——list / add / rm / clean。</summary>
public class ProviderArg : CliArg
{
    public override string Description => "管理服务商数据库（providers.json）";
    public override int ValueCount => -1;
    public override bool Greedy => true;
    public override string? ValueLabel => "子命令";
    public override (string Cmd, string Desc)[]? SubCommands =>
    [
        ("list", "列出所有服务商"),
        ("add <id> <名称> <base-url>", "添加服务商"),
        ("rm <id>", "移除服务商（同时清除其 Key）"),
        ("select <id>", "切换当前服务商（作用于当前大模型 connect）"),
        ("key <id> <key>", "保存某服务商的 API key"),
        ("test", "连通性测试"),
        ("import [source]", "导入外部模型库"),
        ("clean", "清理无效服务商（探测失败的）"),
    ];
    public ProviderArg() : base("provider", "--provider") { }
    public override int? OnMatch(List<string> values) => ModelCli.ProviderCli.Run(values);
}

/// <summary>启动权限模式（问答ACK/自动AUTO/智能SMART/畅通YOLO；tiny/chat=纯聊天工作模式）。</summary>
public class PermitArg : CliArg
{
    public override string Description => "启动权限模式（tiny/chat=纯聊天工作模式）";
    public override int ValueCount => 1;
    public override string? ValueLabel => "tiny|chat|ack|auto|smart|yolo";
    public override (string Cmd, string Desc)[]? SubCommands =>
    [
        ("tiny", "聊天：纯聊天工作模式（0 工具 0 提示词）"),
        ("ack", "问答：逐次确认"),
        ("auto", "自动：改必问，只读放行、写操作确认"),
        ("smart", "智能：智能分级确认"),
        ("yolo", "畅通：跳过所有确认"),
    ];
    public PermitArg() : base("permit", "--permit") { }
}

/// <summary>MCP 管理 CLI 纯逻辑（列出 / 重连，输出到 Console）。</summary>
public static class McpCli
{
    public static int Run(List<string> values)
    {
        if (values.Count > 0 && values[0].Equals("reload", StringComparison.OrdinalIgnoreCase))
        {
            var name = values.Count > 1 ? values[1] : null;
            Console.WriteLine(McpManager.ReloadAsync(name).GetAwaiter().GetResult());
            return 0;
        }

        var servers = McpManager.Servers;
        if (servers.Count == 0)
        {
            Console.WriteLine("未配置 MCP 服务器（--mcp-config <路径> 可指定配置文件）。");
            return 0;
        }
        Console.WriteLine($"MCP 服务器 ({servers.Count})");
        foreach (var s in servers)
        {
            var mark = s.Status switch
            {
                McpServerStatus.Connected => "✅",
                McpServerStatus.Connecting => "⏳",
                McpServerStatus.Failed => "❌",
                _ => "❓",
            };
            var line = $"{mark} {s.Name} [{s.Transport}] {s.ToolCount} 工具";
            if (s.ResourceCount > 0) line += $" · {s.ResourceCount} 资源";
            if (s.PromptCount > 0) line += $" · {s.PromptCount} 提示词";
            if (s.Error != null) line += $" — {s.Error}";
            Console.WriteLine(line);
        }
        Console.WriteLine("重连: --mcp reload [name]");
        return 0;
    }
}

/// <summary>清理缓存文件（保守：只清明确是缓存的内容，不动会话/记忆/检查点）</summary>
public static class CachePurger
{
    public static int Run()
    {
        var purged = new List<string>();
        var cwd = Directory.GetCurrentDirectory();
        TryPurgeFile(Path.Combine(cwd, ".waycoder", "file-tracker.json"), purged);
        TryPurgeFile(Path.Combine(cwd, ".waycoder", "todos.json"), purged);
        TryPurgeDir(Path.Combine(cwd, ".waycoder", "trajectory"), purged);
        Console.WriteLine(purged.Count == 0 ? "没有可清理的缓存文件" : $"已清理 {purged.Count} 项缓存:");
        foreach (var p in purged) Console.WriteLine($"  - {p}");
        return 0;
    }

    private static void TryPurgeFile(string path, List<string> purged)
    {
        try { if (File.Exists(path)) { File.Delete(path); purged.Add(path); } } catch { }
    }

    private static void TryPurgeDir(string dir, List<string> purged)
    {
        try { if (Directory.Exists(dir)) { Directory.Delete(dir, true); purged.Add(dir); } } catch { }
    }
}

// ═══════════════════════════════════════════════════════════════
// 注册入口 —— 应用启动时调用一次
// ═══════════════════════════════════════════════════════════════

public static class BuiltinArgs
{
    static bool _registered;

    /// <summary>注册所有内置 CLI 参数（幂等）。重复名称自动报错。</summary>
    public static void RegisterAll()
    {
        if (_registered) return;
        _registered = true;

        CliArgRegistry.Register(new ModelArg());
        CliArgRegistry.Register(new ConnectArg());
        CliArgRegistry.Register(new BaseUrlArg());
        CliArgRegistry.Register(new ApiKeyArg());
        CliArgRegistry.Register(new PromptArg());
        CliArgRegistry.Register(new ResumeArg());
        CliArgRegistry.Register(new SessionListArg());
        CliArgRegistry.Register(new MaxBudgetArg());
        CliArgRegistry.Register(new MaxRequeueArg());
        CliArgRegistry.Register(new VersionArg());
        CliArgRegistry.Register(new InitArg());
        CliArgRegistry.Register(new YoloArg());
        CliArgRegistry.Register(new OutputFormatArg());
        CliArgRegistry.Register(new PermissionModeArg());
        CliArgRegistry.Register(new AllowedToolsArg());
        CliArgRegistry.Register(new DisallowedToolsArg());
        CliArgRegistry.Register(new SystemPromptArg());
        CliArgRegistry.Register(new SessionArg());
        CliArgRegistry.Register(new WatchArg());
        CliArgRegistry.Register(new TinyArg());
        CliArgRegistry.Register(new EconomyArg());
        CliArgRegistry.Register(new EditArg());
        CliArgRegistry.Register(new UpdateArg());
        CliArgRegistry.Register(new JsonArg());
        CliArgRegistry.Register(new WebArg());
        CliArgRegistry.Register(new TuiArg());
        CliArgRegistry.Register(new CliModeArg());
        CliArgRegistry.Register(new BatchArg());
        CliArgRegistry.Register(new BatchRepoArg());
        CliArgRegistry.Register(new BatchTaskArg());
        CliArgRegistry.Register(new BatchKeepArg());
        CliArgRegistry.Register(new ConfigArg());
        CliArgRegistry.Register(new DebugArg());
        CliArgRegistry.Register(new HelpArg());
        CliArgRegistry.Register(new MaxTurnsArg());
        CliArgRegistry.Register(new AutoCommitArg());
        CliArgRegistry.Register(new McpConfigArg());
        CliArgRegistry.Register(new ThemeArg());
        CliArgRegistry.Register(new QuietArg());
        CliArgRegistry.Register(new NoColorArg());
        CliArgRegistry.Register(new McpArg());
        CliArgRegistry.Register(new ResetArg());
        CliArgRegistry.Register(new PurgeArg());
        CliArgRegistry.Register(new ProviderArg());
        CliArgRegistry.Register(new PermitArg());
#if WAYCODER_TEST
        CliArgRegistry.Register(new TestArg());
        CliArgRegistry.Register(new BenchmarkArg());
        CliArgRegistry.Register(new LimitsArg());
#endif
        CliArgRegistry.Register(new ScreenshotArg());
        CliArgRegistry.Register(new WidthProbeArg());
        CliArgRegistry.Register(new SyspromptSizeArg());
#if WAYCODER_TEST
        CliArgRegistry.Register(new TuiDemoArg());
        CliArgRegistry.Register(new TuiAuditArg());
        CliArgRegistry.Register(new DialogShowArg());
#endif
        CliArgRegistry.Register(new GuiArg());
#if WAYCODER_TEST
        CliArgRegistry.Register(new TuiPreviewArg());
        CliArgRegistry.Register(new TuiWatchArg());
        CliArgRegistry.Register(new TuiMarkupDemoArg());
#endif
        CliArgRegistry.Register(new TuiChatArg());
#if WAYCODER_TEST
        CliArgRegistry.Register(new KeypadArg());
#endif
        CliArgRegistry.Register(new ThemeVerifyArg());

        // 槽位任务参数：-pa 共享前缀 + -p1 ~ -p9, -p0(=F10)
        CliArgRegistry.Register(new SlotPromptAllArg());
        for (int n = 0; n <= 9; n++)
            CliArgRegistry.Register(new SlotPromptArg(n));
    }
}
