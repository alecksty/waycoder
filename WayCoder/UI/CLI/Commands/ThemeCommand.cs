using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

public class ThemeCommand : SlashCommand
{
    public override string Name => "/theme";
    public override string Description => L.Pick("切换主题", "Switch theme");
    public override string? Usage => "/theme [preset|next]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        if (string.IsNullOrEmpty(args))
        {
            screen.AddSystemMsg(L.Pick($"当前主题: {ProgramContext.Config.ThemePreset}\n可选: {string.Join(", ", ThemeConfig.Presets.Keys)} · next=下一个", $"Current theme: {ProgramContext.Config.ThemePreset}\nAvailable: {string.Join(", ", ThemeConfig.Presets.Keys)} · next=advance to the next one"));
        }
        // next = 轮转到下一个预设。快捷键（原 Ctrl+Shift+F2）已随三键组合取消，这里做打字兜底
        else if (args.Equals("next", StringComparison.OrdinalIgnoreCase))
        {
            var name = ThemeConfig.NextPreset();
            ThemeConfig.ApplyPreset(name);
            ProgramContext.Config.ThemePreset = name;
            screen.AddSystemMsg(L.Pick($"🎨 主题已切换: {name}", $"🎨 Theme switched: {name}"));
        }
        else if (ThemeConfig.Presets.TryGetValue(args, out var _))
        {
            ThemeConfig.ApplyPreset(args);
            ProgramContext.Config.ThemePreset = args;
            screen.AddSystemMsg(L.Pick($"🎨 主题已切换: {args}", $"🎨 Theme switched: {args}"));
        }
        else
        {
            screen.AddSystemMsg(L.Pick($"未知主题: {args}。可选: {string.Join(", ", ThemeConfig.Presets.Keys)}", $"Unknown theme: {args}. Available: {string.Join(", ", ThemeConfig.Presets.Keys)}"));
        }
        return Task.CompletedTask;
    }
}
