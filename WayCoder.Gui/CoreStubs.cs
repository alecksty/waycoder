using WayCoder.Tools;
using WayCoder.UI.Tui.Screens;

namespace WayCoder;

// ═══════════════════════════════════════════════════════════════
//  GUI 版占位桩：核心/TUI 源码引用的 CLI 专属类型（斜杠命令、插件、
//  程序全局上下文）在 GUI 进程中不参与实际工作，此处提供最小占位
//  使其编译通过。真实实现见主项目 SlashCommand.cs / Plugins/。
// ═══════════════════════════════════════════════════════════════

public interface ISlashCommand
{
    string Name { get; }
    string[] Aliases { get; }
    string Description { get; }
    string? Usage { get; }
    bool Matches(string input);
    Task ExecuteAsync(string args, ChatScreen screen);
}

public abstract class SlashCommand : ISlashCommand
{
    public abstract string Name { get; }
    public virtual string[] Aliases => [];
    public abstract string Description { get; }
    public virtual string? Usage => null;
    public virtual bool Matches(string input) => false;
    public abstract Task ExecuteAsync(string args, ChatScreen screen);
}

public static class SlashCommandRegistry
{
    private static readonly List<ISlashCommand> _commands = [];
    public static IReadOnlyList<ISlashCommand> Commands => _commands;
    public static void Register(ISlashCommand cmd) { }
    public static void RegisterAll() { }
    public static string[] AllNames => [];
    public static (ISlashCommand? Command, string Args) Match(string userInput) => (null, userInput);
}

public static class ProgramContext
{
    public static Agent? Agent { get; set; }
    public static Config Config { get; set; } = new();
    public static LLM? LLM { get; set; }
}

public static class PluginRegistry
{
    public static IEnumerable<ITool> CollectTools() => [];
}
