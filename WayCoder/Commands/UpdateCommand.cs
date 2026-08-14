using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

/// <summary>
/// /update — 检查并自动升级 WayCoder 到最新版本。
///
///   /update        → 检查新版本，显示当前/最新版本 + 更新日志
///   /update now    → 下载匹配当前平台的二进制并自替换（Windows 退出后自动重启，Unix 提示重启）
///   /update check  → 仅检查（同无参数）
///
/// 版本来源：优先 GitHub Releases，失败回退 Gitee Releases（对标 Claude Code `claude update`）。
/// </summary>
public class UpdateCommand : SlashCommand
{
    public override string Name => "/update";
    public override string[] Aliases => ["/升级", "/upgrade"];
    public override string Description => "检查并自动升级 WayCoder 到最新版本";
    public override string? Usage => "/update [check|now]";

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        var arg = args.Trim().ToLowerInvariant();

        if (arg is "now" or "yes" or "up" or "upgrade" or "升级")
        {
            screen.AddMessage("**正在升级 WayCoder…**\n\n下载最新版本并替换当前二进制，请稍候…", "system");
            var result = await UpdateChecker.SelfUpdateAsync();
            screen.AddMessage(result, "system");
            return;
        }

        // 检查（含更新日志详情）
        var latest = await UpdateChecker.FetchLatestAsync();
        if (latest == null)
        {
            screen.AddMessage(
                "**WayCoder 更新**\n\n⚠ 无法获取最新版本信息。请检查网络连接，或确认仓库配置：\n\n" +
                "- `WAYCODER_GITHUB_REPO`（默认 `alecksty/waycoder`）\n" +
                "- `WAYCODER_GITEE_REPO`（默认 `aleckstygit/my-coder`）\n\n" +
                "也可用 `/config` 查看当前配置。", "system");
            return;
        }

        var cmp = UpdateChecker.CompareVersions(latest.TagName, Global.Version);
        if (cmp <= 0)
        {
            screen.AddMessage(
                $"**WayCoder 更新**\n\n✅ 已是最新版本 **{Global.Version}**\n\n" +
                $"远端（{latest.Source}）最新：{latest.TagName}", "system");
            return;
        }

        var body = latest.Body;
        if (body.Length > 2000)
            body = body[..2000] + "\n\n…（已截断，完整见 release 页面）";
        else if (string.IsNullOrWhiteSpace(body))
            body = "（无更新日志）";

        screen.AddMessage(
            $"**WayCoder 更新**\n\n" +
            $"当前版本：**{Global.Version}**\n" +
            $"最新版本：**{latest.TagName}**（{latest.Source}）\n\n" +
            $"---\n\n{body}\n\n---\n\n" +
            $"输入 **/update now** 自动升级到 {latest.TagName}。", "system");
    }
}
