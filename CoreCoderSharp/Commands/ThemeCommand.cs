using CoreCoderSharp.UI.TuiScreens;

namespace CoreCoderSharp.Commands;

public class ThemeCommand : SlashCommand
{
    public override string Name => "/theme";
    public override string Description => "切换主题";
    public override string? Usage => "/theme [preset]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        if (string.IsNullOrEmpty(args))
        {
            screen.AddSystemMsg($"当前主题: {ProgramContext.Config.ThemePreset}\n可选: {string.Join(", ", ThemeConfig.Presets.Keys)}");
        }
        else if (ThemeConfig.Presets.TryGetValue(args, out var _))
        {
            ThemeConfig.ApplyPreset(args);
            ProgramContext.Config.ThemePreset = args;
            screen.AddSystemMsg($"🎨 主题已切换: {args}");
        }
        else
        {
            screen.AddSystemMsg($"未知主题: {args}。可选: {string.Join(", ", ThemeConfig.Presets.Keys)}");
        }
        return Task.CompletedTask;
    }
}
