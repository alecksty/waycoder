using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /resume — 恢复上次自动保存的会话（/continue / /恢复）。
/// 启动时 TryRestoreSession 把自动会话放入 Program.PendingRestore，此命令把消息载入当前 Agent。
/// </summary>
public class ResumeCommand : SlashCommand
{
    public override string Name => "/resume";
    public override string[] Aliases => ["/continue", "/恢复"];
    public override string Description => "恢复上次自动保存的会话";
    public override string? Usage => "/resume";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        if (Program.PendingRestore is not { } pending)
        {
            screen.AddSystemMsg("⚠ 没有可恢复的会话（上次会话未自动保存）");
            return Task.CompletedTask;
        }
        ProgramContext.Agent?.ReplaceMessages(pending.Messages);
        Program.ClearPendingRestore();
        screen.AddSystemMsg($"✔ 已恢复 {pending.Messages.Count} 条消息（模型: {pending.Model}）");
        return Task.CompletedTask;
    }
}
