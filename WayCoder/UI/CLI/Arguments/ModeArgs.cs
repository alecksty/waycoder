using WayCoder.Tools;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Cli.Arguments;

// ═══════════════════════════════════════════════════════════════
// 会话参数
// ═══════════════════════════════════════════════════════════════

public class PromptArg : CliArg
{
    public override string Description => L.Pick(
        "一次性提示词（CLI 纯文本输出，跑完退出）。-p1~-p0 投递槽位(进 TUI), -pa 共享前缀, 同槽位可排队",
        "One-shot prompt (plain CLI output, exits when done). -p1..-p0 queue into a slot (enters the TUI), -pa is a shared prefix, the same slot can be queued repeatedly");
    public override int ValueCount => 1;
    public override string? ValueLabel => L.Pick("文本", "text");
    // --print 别名（-p/--print），OpenCode 对应 run <message>
    public PromptArg() : base("prompt", "-p", "--prompt", "--print") { }
}

public class ResumeArg : CliArg
{
    public override string Description => L.Pick(
        "恢复会话,会话名为空,就是上一次的。",
        "Resume a session; leave the name out to resume the most recent one.");
    public override int ValueCount => -1; // 可选值：无参时恢复最近会话
    public override string? ValueLabel => L.Pick("会话名", "session name");
    public ResumeArg() : base("resume", "-r", "--resume", "-c", "--continue") { }
}

public class MaxBudgetArg : CliArg
{
    public override string Description => L.Pick(
        "费用上限（美元），超支自动停止",
        "Cost cap in USD; stops automatically once exceeded");
    public override int ValueCount => 1;
    public override string? ValueLabel => L.Pick("金额", "amount");
    public MaxBudgetArg() : base("max-budget-usd", "-B", "--max-budget-usd") { }
}

public class MaxRequeueArg : CliArg
{
    public override string Description => L.Pick(
        "撞轮次上限后自动压缩+续跑次数（0=关闭，默认 3，超长任务可调大）",
        "Auto-compact-and-continue attempts after the turn limit is hit (0 = off, default 3; raise it for very long tasks)");
    public override int ValueCount => 1;
    public override string? ValueLabel => L.Pick("次数", "count");
    public MaxRequeueArg() : base("max-requeue", "--max-requeue") { }
}

public class YoloArg : CliArg
{
    public override string Description => L.Pick(
        "跳过所有权限确认（非交互模式自动开启）",
        "Skip every permission confirmation (turned on automatically in non-interactive mode)");
    // --dangerously-skip-permissions 别名
    public YoloArg() : base("yolo", "-y", "--yolo", "--dangerously-skip-permissions") { }
}

public class CliModeArg : CliArg
{
    public override string Description => L.Pick(
        "强制 CLI 文本界面（非全屏，逐行交互）",
        "Force the plain CLI text interface (line-by-line, not full screen)");
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
    public override string Description => L.Pick(
        "输出格式：json|stream-json 等同 --json，text|default 普通输出",
        "Output format: json|stream-json behave like --json, text|default is normal output");
    public override int ValueCount => 1;
    public override string? ValueLabel => L.Pick("格式", "format");
    public OutputFormatArg() : base("output-format", "--output-format", "--format") { }
}

/// <summary>
/// --permission-mode &lt;default|acceptEdits|plan|bypassPermissions&gt;（Claude Code）。
/// plan → 行为轴 Plan；acceptEdits → 边界轴 auto-edit；bypassPermissions → full-auto。
/// </summary>
public class PermissionModeArg : CliArg
{
    public override string Description => L.Pick(
        "权限模式：default|acceptEdits|plan|bypassPermissions（plan=只读规划，acceptEdits=自动编辑，bypassPermissions=全开）",
        "Permission mode: default|acceptEdits|plan|bypassPermissions (plan = read-only planning, acceptEdits = auto-edit, bypassPermissions = everything allowed)");
    public override int ValueCount => 1;
    public override string? ValueLabel => L.Pick("模式", "mode");
    public PermissionModeArg() : base("permission-mode", "--permission-mode") { }
}

/// <summary>工具白名单（--allowedTools / --allowed-tools，空格分隔）</summary>
public class AllowedToolsArg : CliArg
{
    public override string Description => L.Pick(
        "工具白名单（空格/逗号分隔），等同 WAYCODER_ALLOWED_TOOLS",
        "Tool allowlist (space/comma separated); same as WAYCODER_ALLOWED_TOOLS");
    public override int ValueCount => -1;
    public override string? ValueLabel => L.Pick("工具名", "tool name");
    public override bool Greedy => true; // 空格分隔多值
    public AllowedToolsArg() : base("allowed-tools", "--allowedTools", "--allowed-tools") { }
}

/// <summary>工具黑名单（--disallowedTools / --disallowed-tools，空格分隔）</summary>
public class DisallowedToolsArg : CliArg
{
    public override string Description => L.Pick(
        "工具黑名单（空格/逗号分隔），等同 WAYCODER_DISABLED_TOOLS",
        "Tool denylist (space/comma separated); same as WAYCODER_DISABLED_TOOLS");
    public override int ValueCount => -1;
    public override string? ValueLabel => L.Pick("工具名", "tool name");
    public override bool Greedy => true;
    public DisallowedToolsArg() : base("disallowed-tools", "--disallowedTools", "--disallowed-tools") { }
}

/// <summary>
/// --system-prompt &lt;text&gt; / --append-system-prompt &lt;text&gt;（）。
/// WayCoder 系统提示词为结构化基础提示，此处实现为追加（整体替换会丢失结构）。
/// </summary>
public class SystemPromptArg : CliArg
{
    public override string Description => L.Pick(
        "追加到系统提示词的文本（--append-system-prompt 为别名）",
        "Text appended to the system prompt (--append-system-prompt is an alias)");
    public override int ValueCount => 1;
    public override string? ValueLabel => L.Pick("文本", "text");
    public SystemPromptArg() : base("system-prompt", "--system-prompt", "--append-system-prompt") { }
}

/// <summary>按会话 id 恢复（OpenCode --session / --resume-session-id / --session-id）</summary>
public class SessionArg : CliArg
{
    public override string Description => L.Pick(
        "按会话 id 恢复，等同 --resume <id>",
        "Resume by session id; same as --resume <id>");
    public override int ValueCount => 1;
    public override string? ValueLabel => L.Pick("会话ID", "session ID");
    public SessionArg() : base("session", "--session", "--resume-session-id", "--session-id") { }
}

public class SessionListArg : CliArg
{
    public override string Description => L.Pick("列出所有已保存会话", "List every saved session");
    public override int ValueCount => 0;
    public SessionListArg() : base("session-list", "-s", "--session-list", "--sessions") { }
}

// ═══════════════════════════════════════════════════════════════
// 竞品对标参数（Claude Code / Aider / OpenCode 主要参数）
// ═══════════════════════════════════════════════════════════════

/// <summary>对话最大轮次上限（对标 Claude Code --max-turns）</summary>
public class MaxTurnsArg : CliArg
{
    public override string Description => L.Pick("对话最大轮次上限", "Maximum conversation turns");
    public override int ValueCount => 1;
    public override string? ValueLabel => L.Pick("次数", "count");
    public MaxTurnsArg() : base("max-turns", "--max-turns") { }
}

/// <summary>自动 git 提交开关（对标 Aider / OpenCode，缺省 on）</summary>
public class AutoCommitArg : CliArg
{
    public override string Description => L.Pick(
        "自动 git 提交开关（on|off，缺省 on）",
        "Auto git-commit switch (on|off, default on)");
    public override int ValueCount => -1;
    public override string? ValueLabel => "on|off";
    public AutoCommitArg() : base("auto-commit", "--auto-commit") { }
}

/// <summary>启动权限模式（问答ACK/自动AUTO/智能SMART/畅通YOLO；tiny/chat=纯聊天工作模式）。</summary>
public class PermitArg : CliArg
{
    public override string Description => L.Pick(
        "启动权限模式（tiny/chat=纯聊天工作模式）",
        "Startup permission mode (tiny/chat = pure chat work mode)");
    public override int ValueCount => 1;
    public override string? ValueLabel => "tiny|chat|ack|auto|smart|yolo";
    public override (string Cmd, string Desc)[]? SubCommands =>
    [
        ("tiny", L.Pick("聊天：纯聊天工作模式（0 工具 0 提示词）", "Chat: pure chat work mode (no tools, no prompt)")),
        ("ack", L.Pick("问答：逐次确认", "Ask: confirm every step")),
        ("auto", L.Pick("自动：改必问，只读放行、写操作确认", "Auto: always ask on edits; reads pass, writes confirm")),
        ("smart", L.Pick("智能：智能分级确认", "Smart: risk-graded confirmation")),
        ("yolo", L.Pick("畅通：跳过所有确认", "Yolo: skip every confirmation")),
    ];
    public PermitArg() : base("permit", "--permit") { }
}

/// <summary>
/// 启动工作模式（--mode build|plan|chat，行为轴，对标 Claude Code --permission-mode plan）。
/// 与 --permit 解耦：--permit 管确认轴（tiny/chat 别名除外，走 Chat 工作模式），
/// --mode 直接设工作模式（Build 全工具 / Plan 只读 / Chat 0 工具 0 提示词）。
/// </summary>
public class ModeArg : CliArg
{
    public override string Description => L.Pick(
        "启动工作模式：build=建造（全工具）/plan=计划（只读白名单）/chat=聊天（0 工具 0 提示词）",
        "Startup work mode: build = full toolset, plan = read-only allowlist, chat = no tools and no prompt");
    public override int ValueCount => 1;
    public override string? ValueLabel => "build|plan|chat";
    public ModeArg() : base("mode", "--mode") { }
}
