using WayCoder.UI.Tui.Screens;

namespace WayCoder.Maui.Services;

/// <summary>
/// MAUI 端页面导航斜杠命令集（经 <c>PluginRegistry.CollectCommands</c> 注入 SlashCommandRegistry，
/// 使聊天输入 `/page` 能打开移动端各界面：底部 Tab / 独立页（会话历史/侧栏/模型/供应商/关于…）。
/// 名称尽量不与桌面既有命令冲突；另有 `/open &lt;page&gt;` 主命令集中导航。
/// </summary>
public static class MauiCommands
{
    public static IEnumerable<ISlashCommand> All()
    {
        // Tab 切换（绝对路由）
        yield return new PageNav("/home", "打开首页", "//home");
        yield return new PageNav("/chat", "打开对话页", "//chat");
        yield return new PageNav("/files", "打开文件页", "//files");
        yield return new PageNav("/settings", "打开设置页", "//settings");
        // 独立页（从当前页 push）
        yield return new PageNav("/sessions", "打开会话历史页", "sessions", "history");
        yield return new PageNav("/panel", "打开侧栏命令页", "panel", "menu", "side");
        yield return new PageNav("/modelpicker", "打开模型选择页", "modelpicker");
        yield return new PageNav("/providers", "打开供应商/模型管理页", "models", "model-manager");
        yield return new PageNav("/gitsync", "打开代码同步页", "gitsync", "sync");
        yield return new PageNav("/about", "打开关于页", "about");
        // 主命令：/open <page>
        yield return new OpenAnyCommand();
    }

    /// <summary>带别名的页面导航命令。</summary>
    private sealed class PageNav : SlashCommand
    {
        private readonly string _route;
        public override string Name { get; }
        private readonly string[] _alias;
        public override string[] Aliases => _alias;
        public override string Description { get; }
        public PageNav(string name, string desc, string route, params string[] aliases)
        {
            Name = name; Description = desc; _route = route; _alias = aliases;
        }
        public override Task ExecuteAsync(string args, ChatScreen screen)
            => NavigateAsync(_route);
    }

    /// <summary>`/open &lt;home|chat|files|settings|sessions|panel|modelpicker|providers|gitsync|about|editor&gt;`</summary>
    private sealed class OpenAnyCommand : SlashCommand
    {
        public override string Name => "/open";
        public override string[] Aliases => ["/go"];
        public override string Description => "打开界面：home/chat/files/settings/sessions/panel/modelpicker/providers/gitsync/about";
        public override string? Usage => "/open <页面>";

        public override async Task ExecuteAsync(string args, ChatScreen screen)
        {
            var p = args.Trim().ToLowerInvariant().TrimStart('/');
            var route = p switch
            {
                "home" => "//home",
                "chat" => "//chat",
                "files" => "//files",
                "settings" or "config" => "//settings",
                "session" or "sessions" or "history" => "sessions",
                "panel" or "menu" or "side" or "命令" => "panel",
                "model" or "modelpicker" or "模型" => "modelpicker",
                "provider" or "providers" or "models" or "供应商" => "models",
                "sync" or "gitsync" or "同步" => "gitsync",
                "about" or "关于" => "about",
                "editor" or "编辑器" => "editor",
                _ => null,
            };
            if (route != null) { await NavigateAsync(route); return; }
            ChatScreen.OnAddSystemMsg?.Invoke(
                "可用：/open home|chat|files|settings|sessions|panel|modelpicker|providers|gitsync|about|editor");
        }
    }

    /// <summary>Shell 导航（Tab 用 // 绝对路由，独立页用相对 push），失败回写灰字。</summary>
    private static async Task NavigateAsync(string route)
    {
        try
        {
            var shell = Shell.Current;
            if (shell == null) return;
            if (route.StartsWith("//", StringComparison.Ordinal)) await shell.GoToAsync(route);
            else await shell.GoToAsync(route);
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
