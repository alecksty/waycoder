using WayCoder.Tools;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Cli.Arguments;

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

public class YoloArg : CliArg
{
    public override string Description => "跳过所有权限确认（非交互模式自动开启）";
    // --dangerously-skip-permissions 别名
    public YoloArg() : base("yolo", "-y", "--yolo", "--dangerously-skip-permissions") { }
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
/// --permission-mode &lt;default|acceptEdits|plan|bypassPermissions&gt;（Claude Code）。
/// plan → 行为轴 Plan；acceptEdits → 边界轴 auto-edit；bypassPermissions → full-auto。
/// </summary>
public class PermissionModeArg : CliArg
{
    public override string Description => "权限模式：default|acceptEdits|plan|bypassPermissions（plan=只读规划，acceptEdits=自动编辑，bypassPermissions=全开）";
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
