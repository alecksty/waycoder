using CoreCoderSharp.UI.TuiScreens;

namespace CoreCoderSharp.Commands;

public class ResumeCommand : SlashCommand
{
    public override string Name => "/resume";
    public override string Description => "恢复上次会话";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var loaded = SessionManager.LoadSession("_auto");
        if (loaded == null)
        {
            screen.AddSystemMsg("没有可恢复的会话");
            return Task.CompletedTask;
        }

        var agent = ProgramContext.Agent;
        if (agent == null) { screen.AddSystemMsg("Agent 未初始化"); return Task.CompletedTask; }

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
        screen.AddSystemMsg($"✔ 已恢复会话 (_auto)");
        return Task.CompletedTask;
    }
}
