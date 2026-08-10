using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

public class LoadCommand : SlashCommand
{
    public override string Name => "/load";
    public override string Description => "加载会话";
    public override string? Usage => "/load <会话ID>";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        if (string.IsNullOrEmpty(args))
        {
            screen.AddSystemMsg("用法: /load <会话ID>");
            return Task.CompletedTask;
        }

        var loaded = SessionManager.LoadSession(args);
        if (loaded == null)
        {
            screen.AddSystemMsg($"会话 '{args}' 未找到");
            return Task.CompletedTask;
        }

        var agent = ProgramContext.Agent;
        if (agent == null) { screen.AddSystemMsg("Agent 未初始化"); return Task.CompletedTask; }

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
        screen.AddSystemMsg($"✔ 已加载会话: {args}");
        return Task.CompletedTask;
    }
}
