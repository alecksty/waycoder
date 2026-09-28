using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

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
    public override string Description => L.Pick("检查并自动升级 WayCoder 到最新版本", "Check for and automatically upgrade WayCoder to the latest version");
    public override string? Usage => "/update [check|now]";

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        // 内网/离线部署：更新开关关闭时不做任何网络请求
        if (!Config.Instance.UpdateEnabled)
        {
            screen.AddMessage(
                L.Pick("**WayCoder 更新**\n\n🔒 更新已禁用（内网/离线模式）。\n\n", "**WayCoder update**\n\n🔒 Updates are disabled (intranet/offline mode).\n\n") +
                L.Pick("如需启用：`/config` 中把「更新开关」设为 true，或设置环境变量 `WAYCODER_UPDATE_ENABLED=true`。", "To enable it: turn on the update switch in `/config`, or set the environment variable `WAYCODER_UPDATE_ENABLED=true`."),
                "system");
            return;
        }

        var arg = args.Trim().ToLowerInvariant();

        if (arg is "now" or "yes" or "up" or "upgrade" or "升级")
        {
            screen.AddMessage(L.Pick("**正在升级 WayCoder…**\n\n下载最新版本并替换当前二进制，请稍候…", "**Upgrading WayCoder…**\n\nDownloading the latest version and replacing the current binary, please wait…"), "system");
            var result = await UpdateChecker.SelfUpdateAsync();
            screen.AddMessage(result, "system");
            return;
        }

        // 检查（含更新日志详情）
        var latest = await UpdateChecker.FetchLatestAsync();
        if (latest == null)
        {
            screen.AddMessage(
                L.Pick("**WayCoder 更新**\n\n⚠ 无法获取最新版本信息。请检查网络连接，或确认仓库配置：\n\n", "**WayCoder update**\n\n⚠ Could not fetch the latest version info. Check your network connection, or verify the repository settings:\n\n") +
                L.Pick("- `WAYCODER_GITHUB_REPO`（默认 `alecksty/waycoder`）\n", "- `WAYCODER_GITHUB_REPO` (default `alecksty/waycoder`)\n") +
                L.Pick("- `WAYCODER_GITEE_REPO`（默认 `aleckstygit/my-coder`）\n\n", "- `WAYCODER_GITEE_REPO` (default `aleckstygit/my-coder`)\n\n") +
                L.Pick("也可用 `/config` 查看当前配置。", "You can also inspect the current configuration with `/config`."), "system");
            return;
        }

        var cmp = UpdateChecker.CompareVersions(latest.TagName, Global.Version);
        if (cmp <= 0)
        {
            screen.AddMessage(
                L.Pick($"**WayCoder 更新**\n\n✅ 已是最新版本 **{Global.Version}**\n\n", $"**WayCoder update**\n\n✅ Already on the latest version **{Global.Version}**\n\n") +
                L.Pick($"远端（{latest.Source}）最新：{latest.TagName}", $"Latest on the remote ({latest.Source}): {latest.TagName}"), "system");
            return;
        }

        var body = latest.Body;
        if (body.Length > 2000)
            body = ContextManager.TruncateByRunes(body, 2000) + L.Pick("\n\n…（已截断，完整见 release 页面）", "\n\n… (truncated; see the release page for the full notes)");
        else if (string.IsNullOrWhiteSpace(body))
            body = L.Pick("（无更新日志）", "(no release notes)");

        screen.AddMessage(
            L.Pick($"**WayCoder 更新**\n\n", $"**WayCoder update**\n\n") +
            L.Pick($"当前版本：**{Global.Version}**\n", $"Current version: **{Global.Version}**\n") +
            L.Pick($"最新版本：**{latest.TagName}**（{latest.Source}）\n\n", $"Latest version: **{latest.TagName}** ({latest.Source})\n\n") +
            $"---\n\n{body}\n\n---\n\n" +
            L.Pick($"输入 **/update now** 自动升级到 {latest.TagName}。", $"Type **/update now** to upgrade automatically to {latest.TagName}."), "system");
    }
}
