using WayCoder.Infra;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /join — 从 Claude Code / Codex / OpenCode / Crush 会话「接着跑」。
///
/// 读取竞品会话的聊天内容 + todo 清单 + 当前 git 状态，组装成交接文档注入当前 Agent，
/// 让用户从别的编程智能体切到 WayCoder 后能无缝续跑。
///
/// 用法：
///   /join                     列出匹配当前项目的竞品会话
///   /join claude|codex|opencode|crush   直接接手该工具最新的会话
///   /join <序号>              接手列表中的第 N 个会话
/// </summary>
public class JoinCommand : SlashCommand
{
    public override string Name => "/join";
    public override string[] Aliases => ["/接手", "/续跑", "/handoff"];
    public override string Description => L.Pick("从 Claude/Codex/OpenCode/Crush/Aider/Gemini 会话接着跑（聊天+todo+git）", "Continue from a Claude/Codex/OpenCode/Crush/Aider/Gemini session (chat + todos + git)");
    public override string? Usage => L.Pick("/join [claude|codex|opencode|crush|aider|gemini|list|<序号>]", "/join [claude|codex|opencode|crush|aider|gemini|list|<index>]");

    static readonly string[] Tools = ["claude", "codex", "opencode", "crush", "aider", "gemini"];

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        var cwd = Environment.CurrentDirectory;
        var arg = args.Trim();

        // 无参 / list → 列出候选会话
        if (string.IsNullOrEmpty(arg) || arg.Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            var all = ContextBridge.FindSessions(cwd);
            if (all.Count == 0)
            {
                screen.AddMessage(L.Pick(
                    "未找到匹配当前项目的竞品会话。\n\n" +
                    "支持来源：\n" +
                    "- Claude Code（~/.claude/projects/）\n" +
                    "- Codex（~/.codex/sessions/）\n" +
                    "- OpenCode（~/.local/share/opencode/opencode.db）\n" +
                    "- Crush（&lt;项目&gt;/.crush/crush.db）\n" +
                    "- Aider（&lt;项目&gt;/.aider.chat.history.md）\n" +
                    "- Gemini CLI（~/.gemini/tmp/）\n\n" +
                    "提示：需在竞品工具中曾在**当前目录（或其祖先目录）**有过会话记录。",
                    "No matching session from another coding agent for this project.\n\n" +
                    "Supported sources:\n" +
                    "- Claude Code (~/.claude/projects/)\n" +
                    "- Codex (~/.codex/sessions/)\n" +
                    "- OpenCode (~/.local/share/opencode/opencode.db)\n" +
                    "- Crush (&lt;project&gt;/.crush/crush.db)\n" +
                    "- Aider (&lt;project&gt;/.aider.chat.history.md)\n" +
                    "- Gemini CLI (~/.gemini/tmp/)\n\n" +
                    "Note: the other tool must have a session recorded in **the current directory (or one of its ancestors)**."), "system");
                return;
            }
            screen.AddMessage(FormatList(all), "system");
            return;
        }

        // 指定工具 → 该工具最新会话
        if (Tools.Contains(arg, StringComparer.OrdinalIgnoreCase))
        {
            var sessions = ContextBridge.FindSessions(cwd, arg.ToLower());
            if (sessions.Count == 0)
            {
                screen.AddMessage(L.Pick($"未找到匹配当前项目的 {arg} 会话。可用 `/join` 查看全部候选。", $"No {arg} session matching this project. Run `/join` to list all candidates."), "system");
                return;
            }
            await HandoffAsync(sessions[0], screen, cwd);
            return;
        }

        // 数字 → 按列表序号选
        if (int.TryParse(arg, out var idx))
        {
            var all = ContextBridge.FindSessions(cwd);
            if (idx >= 1 && idx <= all.Count)
            {
                await HandoffAsync(all[idx - 1], screen, cwd);
                return;
            }
            screen.AddMessage(L.Pick($"序号无效：{idx}（共 {all.Count} 个候选）。用 `/join` 查看列表。", $"Invalid index: {idx} ({all.Count} candidate(s) available). Run `/join` to list them."), "system");
            return;
        }

        screen.AddMessage(L.Pick($"未知参数：**{arg}**\n用法：`/join [claude|codex|opencode|crush|list|<序号>]`", $"Unknown argument: **{arg}**\nUsage: `/join [claude|codex|opencode|crush|list|<index>]`"), "system");
    }

    /// <summary>读取会话 → 生成交接文档 → 注入 Agent + 显示给用户。</summary>
    static async Task HandoffAsync(ContextBridge.ExternalSession session, ChatScreen screen, string cwd)
    {
        // 已有聊天记录则提示：导入会叠加在现有对话之后，用户取消则不导入
        var agent = ProgramContext.Agent;
        if (agent != null)
        {
            int userMsgCount = agent.SnapshotMessages().Count(m => m["role"]?.AsString() == "user");
            if (userMsgCount > 0)
            {
                bool ok = screen.ConfirmDialog(L.Pick("⚠ 会话覆盖提示", "⚠ Session merge notice"),
                    L.Pick(
                    $"当前会话已有 {userMsgCount} 条聊天记录，导入 {session.ToolLabel} 上下文会叠加在现有对话之后。\n\n" +
                    "确定要导入吗？取消则不做任何改动。",
                    $"This session already has {userMsgCount} chat message(s); importing the {session.ToolLabel} context appends to the existing conversation.\n\n" +
                    "Import anyway? Cancelling leaves everything unchanged."));
                if (!ok)
                {
                    screen.AddMessage(L.Pick("已取消导入，未做任何改动。", "Import cancelled; nothing was changed."), "system");
                    return;
                }
            }
        }

        screen.AddMessage(L.Pick($"🔄 正在读取 {session.ToolLabel} 会话：{session.Title}…", $"🔄 Reading the {session.ToolLabel} session: {session.Title}…"), "system");

        // 读大文件 / SQLite / 执行 git 属重 IO，放后台线程避免阻塞 UI
        var doc = await Task.Run(() => ContextBridge.BuildHandoffDoc(session, cwd));

        // 注入当前 Agent 消息历史（system 角色 = 背景上下文，模型下一轮可见）
        ProgramContext.Agent?.AddMessage(JNode.Object().Set("role", "system").Set("content", doc));

        screen.AddMessage(doc, "system");
        screen.AddMessage(L.Pick($"✅ 已注入 {session.ToolLabel} 交接上下文。现在可以继续了——例如输入「继续完成剩余工作」。", $"✅ {session.ToolLabel} handoff context injected. You can continue now — for example, type \"continue with the remaining work\"."), "system");
    }

    /// <summary>格式化候选会话列表（带序号）。</summary>
    static string FormatList(List<ContextBridge.ExternalSession> sessions)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(L.Pick($"## 发现 {sessions.Count} 个可接手的竞品会话", $"## Found {sessions.Count} session(s) from other coding agents"));
        sb.AppendLine();
        sb.AppendLine(L.Pick("| # | 来源 | 更新时间 | 标题 |", "| # | Source | Updated | Title |"));
        sb.AppendLine("|---|---|---|---|");
        for (int i = 0; i < sessions.Count; i++)
        {
            var s = sessions[i];
            sb.AppendLine($"| {i + 1} | {s.ToolLabel} | {s.UpdatedAt:MM-dd HH:mm} | {s.Title} |");
        }
        sb.AppendLine();
        sb.AppendLine(L.Pick("接手方式：`/join <序号>`，或 `/join claude|codex|opencode|crush|aider|gemini` 直接接手最新会话。", "To take over: `/join <index>`, or `/join claude|codex|opencode|crush|aider|gemini` to take over that tool's latest session."));
        return sb.ToString().Trim();
    }
}
