using WayCoder.UI.Tui.Screens;

namespace WayCoder.Maui.Services;

/// <summary>
/// MAUI 端页面导航斜杠命令（经 <c>PluginRegistry.CollectCommands</c> 注入 SlashCommandRegistry，
/// 使聊天输入 `/topage &lt;page&gt;` 能打开移动端各界面）。收敛到单个 `/topage` 主命令，参数映射各页面，
/// 不再为每个页面占用一个 `/xxx` 命令（避免与桌面同名命令冲突、减少命令数量）。
/// </summary>
public static class MauiCommands
{
    public static IEnumerable<ISlashCommand> All()
    {
        yield return new TopageCommand();
    }

    /// <summary>`/topage &lt;home|chat|files|settings|sessions|panel|modelpicker|providers|gitsync|about|editor&gt;`</summary>
    private sealed class TopageCommand : SlashCommand
    {
        public override string Name => "/topage";
        public override string[] Aliases => ["/go"];
        public override string Description => "打开界面：home/chat/files/settings/sessions/panel/modelpicker/providers/gitsync/about/editor";
        public override string? Usage => "/topage <页面>";
        public override bool IsNavCommand => true;

        public override async Task ExecuteAsync(string args, ChatScreen screen)
        {
            // 页面名 → 路由映射上移 core PageRouteMap（纯逻辑，主自测可断言）
            var route = PageRouteMap.Resolve(args);
            if (route != null) { await NavigateAsync(route); return; }
            ChatScreen.OnAddSystemMsg?.Invoke(PageRouteMap.Usage);
        }
    }

    /// <summary>Shell 导航（Tab 用 // 绝对路由，独立页用相对 push），失败回写灰字。</summary>
    private static async Task NavigateAsync(string route)
    {
        try
        {
            var shell = Shell.Current;
            if (shell == null) return;
            // Tab（// 绝对路由）与独立页（相对 push）由 route 前缀自行区分，GoToAsync 均适用。
            await shell.GoToAsync(route);
        }
        catch (Exception ex)
        {
            WayCoder.Maui.Services.ChatScreenBridge.ReportError(ex);
        }
    }
}

/// <summary>聊天屏桥（MAUI stub 字段的薄封装），供命令失败时回写灰字。失败静默亦可。</summary>
internal static class ChatScreenBridge
{
    public static void ReportError(Exception ex)
    {
        try { ErrorLog.Error("Chat", "页面导航", ex); } catch { }
    }
}
