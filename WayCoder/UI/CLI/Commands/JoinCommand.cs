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
    public override string Description => "从 Claude/Codex/OpenCode/Crush 会话接着跑（聊天+todo+git）";
    public override string? Usage => "/join [claude|codex|opencode|crush|list|<序号>]";

    static readonly string[] Tools = ["claude", "codex", "opencode", "crush"];

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
                screen.AddMessage("未找到匹配当前项目的竞品会话。\n\n" +
                    "支持来源：\n" +
                    "- Claude Code（~/.claude/projects/）\n" +
                    "- Codex（~/.codex/sessions/）\n" +
                    "- OpenCode（~/.local/share/opencode/opencode.db）\n" +
                    "- Crush（&lt;项目&gt;/.crush/crush.db）\n\n" +
                    "提示：需在竞品工具中曾在**当前目录（或其祖先目录）**有过会话记录。", "system");
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
                screen.AddMessage($"未找到匹配当前项目的 {arg} 会话。可用 `/join` 查看全部候选。", "system");
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
            screen.AddMessage($"序号无效：{idx}（共 {all.Count} 个候选）。用 `/join` 查看列表。", "system");
            return;
        }

        screen.AddMessage($"未知参数：**{arg}**\n用法：`/join [claude|codex|opencode|crush|list|<序号>]`", "system");
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
                bool ok = screen.ConfirmDialog("⚠ 会话覆盖提示",
                    $"当前会话已有 {userMsgCount} 条聊天记录，导入 {session.ToolLabel} 上下文会叠加在现有对话之后。\n\n" +
                    "确定要导入吗？取消则不做任何改动。");
                if (!ok)
                {
                    screen.AddMessage("已取消导入，未做任何改动。", "system");
                    return;
                }
            }
        }

        screen.AddMessage($"🔄 正在读取 {session.ToolLabel} 会话：{session.Title}…", "system");

        // 读大文件 / SQLite / 执行 git 属重 IO，放后台线程避免阻塞 UI
        var doc = await Task.Run(() => ContextBridge.BuildHandoffDoc(session, cwd));

        // 注入当前 Agent 消息历史（system 角色 = 背景上下文，模型下一轮可见）
        ProgramContext.Agent?.AddMessage(JNode.Object().Set("role", "system").Set("content", doc));

        screen.AddMessage(doc, "system");
        screen.AddMessage($"✅ 已注入 {session.ToolLabel} 交接上下文。现在可以继续了——例如输入「继续完成剩余工作」。", "system");
    }

    /// <summary>格式化候选会话列表（带序号）。</summary>
    static string FormatList(List<ContextBridge.ExternalSession> sessions)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"## 发现 {sessions.Count} 个可接手的竞品会话");
        sb.AppendLine();
        sb.AppendLine("| # | 来源 | 更新时间 | 标题 |");
        sb.AppendLine("|---|---|---|---|");
        for (int i = 0; i < sessions.Count; i++)
        {
            var s = sessions[i];
            sb.AppendLine($"| {i + 1} | {s.ToolLabel} | {s.UpdatedAt:MM-dd HH:mm} | {s.Title} |");
        }
        sb.AppendLine();
        sb.AppendLine("接手方式：`/join <序号>`，或 `/join claude|codex|opencode|crush` 直接接手最新会话。");
        return sb.ToString().Trim();
    }
}
