using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

/// <summary>
/// 统一会话命令 —— 替代 /sessions, /save, /load, /resume。
/// 用法：/session list|save|load <id>|resume
/// </summary>
public class SessionCommand : SlashCommand
{
    public override string Name => "/session";
    public override string Description => "会话管理 (list|save|load|resume)";
    public override string? Usage => "/session <list|save|load <id>|resume>";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var parts = args.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var sub = parts.Length > 0 ? parts[0].ToLowerInvariant() : "";
        var rest = parts.Length > 1 ? parts[1] : "";

        switch (sub)
        {
            case "":
            case "list":
                ListSessions(screen);
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
                screen.AddSystemMsg($"未知子命令: {sub}\n用法: /session <list|save|load <id>|resume>");
                break;
        }

        return Task.CompletedTask;
    }

    static void ListSessions(ChatScreen screen)
    {
        var sessions = SessionManager.ListSessions();
        if (sessions.Count == 0)
        {
            screen.AddSystemMsg("📂 没有已保存的会话");
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("📂 **已保存的会话**");
        foreach (var s in sessions)
            sb.AppendLine($"  {s.Id}  [{s.Model}]  {s.SavedAt}");
        screen.AddSystemMsg(sb.ToString());
    }

    static void SaveSession(ChatScreen screen)
    {
        var agent = ProgramContext.Agent;
        if (agent == null) { screen.AddSystemMsg("Agent 未初始化"); return; }
        var id = SessionManager.SaveSession(agent.Messages, ProgramContext.Config.Model);
        screen.AddSystemMsg($"💾 会话已保存: {id}");
    }

    static void LoadSession(string sessionId, ChatScreen screen)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            screen.AddSystemMsg("用法: /session load <会话ID>");
            return;
        }

        var loaded = SessionManager.LoadSession(sessionId);
        if (loaded == null)
        {
            screen.AddSystemMsg($"会话 '{sessionId}' 未找到");
            return;
        }

        var agent = ProgramContext.Agent;
        if (agent == null) { screen.AddSystemMsg("Agent 未初始化"); return; }

        agent.Messages.Clear();
        agent.Messages.AddRange(loaded.Value.Messages);
        ProgramContext.Config.Model = loaded.Value.Model;

        screen.ClearChat();
        screen.ChatMessages.Clear();
        foreach (var msg in loaded.Value.Messages)
        {
            var role = msg["role"]?.GetValue<string>() ?? "system";
            var content = msg["content"]?.GetValue<string>() ?? "";
            screen.AddMessage(content, role);
        }
        screen.AddSystemMsg($"✔ 已加载会话: {sessionId}");
    }

    static void ResumeSession(ChatScreen screen)
    {
        var loaded = SessionManager.LoadSession("_auto");
        if (loaded == null)
        {
            screen.AddSystemMsg("没有可恢复的会话");
            return;
        }

        var agent = ProgramContext.Agent;
        if (agent == null) { screen.AddSystemMsg("Agent 未初始化"); return; }

        agent.Messages.Clear();
        agent.Messages.AddRange(loaded.Value.Messages);

        screen.ClearChat();
        screen.ChatMessages.Clear();
        foreach (var msg in loaded.Value.Messages)
        {
            var role = msg["role"]?.GetValue<string>() ?? "system";
            var content = msg["content"]?.GetValue<string>() ?? "";
            screen.AddMessage(content, role);
        }
        screen.AddSystemMsg("✔ 已恢复会话 (_auto)");
    }
}
