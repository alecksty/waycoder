using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

public class SessionsCommand : SlashCommand
{
    public override string Name => "/sessions";
    public override string Description => "会话管理";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var sessions = SessionManager.ListSessions();
        if (sessions.Count == 0)
            screen.AddSystemMsg("📂 没有已保存的会话");
        else
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("📂 **已保存的会话**");
            foreach (var s in sessions)
                sb.AppendLine($"  {s.Id}  [{s.Model}]  {s.SavedAt}");
            screen.AddSystemMsg(sb.ToString());
        }
        return Task.CompletedTask;
    }
}
