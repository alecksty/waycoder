using System.Threading;
using WayCoder.Tools;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// Diff 预览 —— 对每个最近修改文件弹出 DiffPreview 对话框（逐 hunk 接受/跳过）。
/// 用法：/diff 或 /d
/// 旧版 RecentCommand 合并了 /diff（只列文件名）；现在 /diff 归本命令，/recent 保持文件列表。
/// 旧内容优先取 git HEAD 版本（非 git 仓库/新文件回退空串，显示为全新增）。
/// </summary>
public class DiffCommand : SlashCommand
{
    public override string Name => "/diff";
    public override string[] Aliases => ["/d"];
    public override string Description => "预览修改文件的差异（逐文件 diff 对话框）";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var changed = EditFileTool.ChangedFiles.ToList();
        if (changed.Count == 0)
        {
            screen.AddSystemMsg("📂 没有待预览的修改文件");
            return Task.CompletedTask;
        }

        foreach (var f in changed)
        {
            string newContent = File.Exists(f) ? File.ReadAllText(f) : "";
            string? oldContent = TryGitHead(f);
            if (oldContent == null && !File.Exists(f))
                continue; // 文件已删且无 git 旧版本 → 无内容可显示
            DiffPreview.Show(oldContent ?? "", newContent, Path.GetFileName(f));
        }
        return Task.CompletedTask;
    }

    /// <summary>取 git HEAD 版本内容（用于与工作区差异对比）。失败/非仓库返回 null。</summary>
    private static string? TryGitHead(string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return null;
            var (code, top, _) = GitRunner.Run("rev-parse --show-toplevel", dir);
            if (code != 0 || string.IsNullOrWhiteSpace(top)) return null;
            var rel = Path.GetRelativePath(top.Trim(), path).Replace('\\', '/');
            // ArgumentList 传参：路径含空格/引号也作为一个参数，不注入 git 选项
            var (showCode, old, _) = GitRunner.RunArgsAsync(
                ["show", "HEAD:" + rel], dir, CancellationToken.None).GetAwaiter().GetResult();
            return showCode == 0 ? old : null;
        }
        catch
        {
            return null; // git 不可用/超时 → 回退空串（新文件全新增视图）
        }
    }
}
