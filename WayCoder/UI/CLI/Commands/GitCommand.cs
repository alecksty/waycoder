using WayCoder.Tools;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// 统一 Git 命令 —— 替代 /git-status, /git-log, /git-diff。
/// 用法：/git status | /git log | /git diff
/// </summary>
public class GitCommand : SlashCommand
{
    public override string Name => "/git";
    public override string Description => "Git 操作 (status|log|diff)";
    public override string? Usage => "/git <status|log|diff>";

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        var sub = args.Trim().ToLowerInvariant();

        var gitCmd = sub switch
        {
            "" or "status" => "status",
            "log" => "log -10",
            "diff" => "diff",
            _ => ""
        };

        if (gitCmd == "")
        {
            screen.AddSystemMsg("用法: /git <status|log|diff>");
            return;
        }

        var tool = new GitTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?> { ["command"] = gitCmd });
        screen.AddSystemMsg(result);
    }
}
