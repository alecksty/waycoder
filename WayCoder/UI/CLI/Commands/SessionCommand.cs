using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// 统一会话命令 —— 替代 /sessions, /save, /load, /resume。
/// 用法：/session list|save|load <id>|resume
/// </summary>
public class SessionCommand : SlashCommand
{
    public override string Name => "/session";
    public override string Description => L.Pick("会话管理 (list|save|load|resume)，list 支持 --limit N --page N", "Session management (list|save|load|resume); list supports --limit N --page N");
    public override string? Usage => "/session <list|save|load <id>|resume> [--limit N] [--page N]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var parts = args.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var sub = parts.Length > 0 ? parts[0].ToLowerInvariant() : "";
        var rest = parts.Length > 1 ? parts[1] : "";

        switch (sub)
        {
            case "":
            case "list":
                ListSessions(screen, rest);
                break;
            case "save":
                SaveSession(screen);
                break;
            case "load":
                LoadSession(rest, screen);
                break;
            case "resume":
                ResumeSession(screen);
                break;
            default:
                screen.AddSystemMsg(L.Pick($"未知子命令: {sub}\n用法: /session <list|save|load <id>|resume>", $"Unknown subcommand: {sub}\nUsage: /session <list|save|load <id>|resume>"));
                break;
        }

        return Task.CompletedTask;
    }

    static void ListSessions(ChatScreen screen, string args = "")
    {
        // 解析 --limit 和 --page 参数
        var limit = 20;
        var page = 1;
        var parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == "--limit" && i + 1 < parts.Length && int.TryParse(parts[i + 1], out var l))
                limit = Math.Clamp(l, 1, 500);
            if (parts[i] == "--page" && i + 1 < parts.Length && int.TryParse(parts[i + 1], out var p))
                page = Math.Max(1, p);
        }
        var offset = (int)Math.Min((long)(page - 1) * limit, int.MaxValue); // page 无上界，防 (page-1)*limit 溢出为负

        var sessions = SessionManager.ListSessions(limit, offset, Program.ActiveSlotIndex);
        if (sessions.Count == 0)
        {
            screen.AddSystemMsg(page > 1 ? L.Pick($"📂 第 {page} 页无更多会话", $"📂 No more sessions on page {page}") : L.Pick("📂 没有已保存的会话", "📂 No saved sessions"));
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(page > 1
            ? L.Pick($"📂 **已保存的会话**（第 {page} 页，每页 {limit} 条）", $"📂 **Saved sessions** (page {page}, {limit} per page)")
            : L.Pick($"📂 **已保存的会话**（共 {sessions.Count} 条）", $"📂 **Saved sessions** ({sessions.Count} total)"));
        foreach (var s in sessions)
            sb.AppendLine($"  {s.Id}  [{s.Model}]  {s.SavedAt}");
        screen.AddSystemMsg(sb.ToString());
    }

    static void SaveSession(ChatScreen screen)
    {
        var agent = ProgramContext.Agent;
        if (agent == null) { screen.AddSystemMsg(L.Pick("Agent 未初始化", "Agent not initialized")); return; }
        var id = agent.SaveSession(null, Program.ActiveSlotIndex); // provider/base_url 元数据经 Agent.SaveSession 集中推导
        screen.AddSystemMsg(L.Pick($"💾 会话已保存: {id}", $"💾 Session saved: {id}"));
    }

    static void LoadSession(string sessionId, ChatScreen screen)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            screen.AddSystemMsg(L.Pick("用法: /session load <会话ID>", "Usage: /session load <session-id>"));
            return;
        }

        var loaded = SessionManager.LoadSession(sessionId, Program.ActiveSlotIndex);
        if (loaded == null)
        {
            screen.AddSystemMsg(L.Pick($"会话 '{sessionId}' 未找到", $"Session '{sessionId}' not found"));
            return;
        }

        var agent = ProgramContext.Agent;
        if (agent == null) { screen.AddSystemMsg(L.Pick("Agent 未初始化", "Agent not initialized")); return; }

        agent.ReplaceMessages(loaded.Value.Messages);
        if (!string.IsNullOrEmpty(loaded.Value.Model))
            ProgramContext.Config.Model = loaded.Value.Model;

        screen.ClearChat();
        screen.ChatMessages.Clear();
        foreach (var msg in loaded.Value.Messages)
        {
            var role = msg["role"]?.AsString() ?? "system";
            var content = msg["content"]?.AsString() ?? "";
            screen.AddMessage(content, role, indent: role == "tool" ? 1 : 0);
        }
        screen.AddSystemMsg(L.Pick($"✔ 已加载会话: {sessionId}", $"✔ Session loaded: {sessionId}"));
    }

    static void ResumeSession(ChatScreen screen)
    {
        // 槽位隔离优先；旧版本 `_auto` 存全局目录，回退兼容
        var loaded = SessionManager.LoadSession("_auto", Program.ActiveSlotIndex)
                     ?? SessionManager.LoadSession("_auto");
        if (loaded == null)
        {
            screen.AddSystemMsg(L.Pick("没有可恢复的会话", "No session to resume"));
            return;
        }

        var agent = ProgramContext.Agent;
        if (agent == null) { screen.AddSystemMsg(L.Pick("Agent 未初始化", "Agent not initialized")); return; }

        agent.ReplaceMessages(loaded.Value.Messages);

        screen.ClearChat();
        screen.ChatMessages.Clear();
        foreach (var msg in loaded.Value.Messages)
        {
            var role = msg["role"]?.AsString() ?? "system";
            var content = msg["content"]?.AsString() ?? "";
            screen.AddMessage(content, role, indent: role == "tool" ? 1 : 0);
        }
        screen.AddSystemMsg(L.Pick("✔ 已恢复会话 (_auto)", "✔ Session resumed (_auto)"));
    }
}
