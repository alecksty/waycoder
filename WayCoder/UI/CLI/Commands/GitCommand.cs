using WayCoder.Tools;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// 统一 Git 命令 —— 本地 git 闭环（移动端无终端 / 真 git 二进制，手动 git 路径靠这里）。
/// 只读三件套对齐桌面端；init/add/commit 为移动端「修改 + 提交」闭环补齐。
/// 远程操作（clone/pull/push）涉及传输协议 + 认证，超出 GitCore 子集，留待后续。
/// 用法：/git init | /git add . | /git commit -m "msg" | /git status | /git log | /git diff
/// </summary>
public class GitCommand : SlashCommand
{
    public override string Name => "/git";
    public override string Description => "Git 操作 (init|add|commit|status|log|diff|branch|checkout|merge|pull|push|fetch|remote|clone)";
    public override string? Usage => "/git <init|add|commit|status|log|diff|branch|checkout|merge|pull|push|fetch|remote|clone>";

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        // 提取子命令（首词）+ 余下参数原样透传（消息内容保留大小写，故不用整体 ToLower）
        var parts = args.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var sub = parts.Length > 0 ? parts[0].ToLowerInvariant() : "";
        var rest = parts.Length > 1 ? string.Join(' ', parts, 1, parts.Length - 1) : "";

        var gitCmd = sub switch
        {
            "" or "status" => "status",
            "log" => rest.Length > 0 ? "log " + rest : "log -10",
            "diff" => rest.Length > 0 ? "diff " + rest : "diff",
            "init" => "init",
            "add" => rest.Length > 0 ? "add " + rest : "add .",
            "commit" => rest.Length > 0 ? "commit " + rest : "commit",
            // 远程/分支操作：桌面端透传给系统 git（原生支持），移动端由 GitCore/GitRemote/GitBranch 纯 C# 实现
            "pull" or "push" or "fetch" or "remote" or "clone" or "branch" or "checkout" or "merge"
                => sub + (rest.Length > 0 ? " " + rest : ""),
            _ => ""
        };

        if (gitCmd == "")
        {
            screen.AddSystemMsg("用法: /git <init|add|commit|status|log|diff|branch|checkout|merge|pull|push|fetch|remote|clone>");
            return;
        }

        var tool = new GitTool();
        var result = await tool.ExecuteAsync(new Dictionary<string, object?> { ["command"] = gitCmd });
        screen.AddSystemMsg(result);
    }
}
