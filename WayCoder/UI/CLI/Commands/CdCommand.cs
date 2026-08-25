using WayCoder.Tools;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /cd — 查看或设置当前槽位的工作目录。
///
/// 每个槽位（F1-F10）拥有独立的工作目录：Agent 在该槽位内 cd 后目录被持久化，
/// 下次任务从该目录起步，与其他槽位互不影响。
/// </summary>
public class CdCommand : SlashCommand
{
    public override string Name => "/cd";
    public override string[] Aliases => ["/目录", "/cwd", "/pwd"];
    public override string Description => "查看或设置当前槽位的工作目录（每槽位独立）";
    public override string? Usage => "/cd [路径]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var slots = Program.GetSlots();
        var idx = Program.ActiveSlotIndex;

        if (slots == null || idx < 0 || idx >= slots.Length)
        {
            screen.AddMessage("当前无活跃槽位，无法操作工作目录。", "system");
            return Task.CompletedTask;
        }

        var slot = slots[idx];
        var current = slot.WorkingDirectory ?? Directory.GetCurrentDirectory();
        var arg = args.Trim();

        if (string.IsNullOrWhiteSpace(arg))
        {
            // 查看当前目录
            screen.AddMessage($"📁 **F{idx + 1} 工作目录**: `{current}`\n\n💡 用 `/cd <路径>` 更改，仅影响本槽位。", "system");
            return Task.CompletedTask;
        }

        // 设置目录
        var expanded = arg.StartsWith('~') ? ExpandHome(arg) : arg;
        string full;
        try
        {
            full = Path.GetFullPath(expanded, current);
        }
        catch (Exception ex)
        {
            screen.AddMessage($"❌ 路径无效：{ex.Message}", "system");
            return Task.CompletedTask;
        }

        if (!Directory.Exists(full))
        {
            screen.AddMessage($"❌ 目录不存在：`{full}`", "system");
            return Task.CompletedTask;
        }

        slot.WorkingDirectory = full;
        screen.AddMessage($"📁 **F{idx + 1} 工作目录已设置**: `{full}`\n\n下次任务（或 `/cd` 后）将从该目录起步，其他槽位不受影响。", "system");
        return Task.CompletedTask;
    }

    /// <summary>展开 `~` 与 `~/xxx` 为用户主目录（跨平台）。</summary>
    private static string ExpandHome(string path)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(home)) return path;
        if (path == "~") return home;
        if (path.StartsWith("~/") || path.StartsWith("~\\"))
            return Path.Combine(home, path[2..]);
        return path;
    }
}
