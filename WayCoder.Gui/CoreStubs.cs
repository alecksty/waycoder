using WayCoder.Tools;
using WayCoder.UI.Gui;
using WayCoder.UI.Tui.Screens;

namespace WayCoder;

// ═══════════════════════════════════════════════════════════════
//  GUI 版占位桩：核心/TUI 源码引用的 CLI 专属类型（斜杠命令、插件、
//  程序全局上下文）在 GUI 进程中参与实际工作，此处提供可用实现。
//  GUI 是独立进程（csproj 排除主工程 SlashCommand.cs），故自建一套
//  ISlashCommand/SlashCommand/SlashCommandRegistry；命令集经
//  GuiCommands 注入（命名与主工程一致），输入统一走 Match。
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
    public virtual bool Matches(string input)
    {
        if (string.Equals(input, Name, StringComparison.OrdinalIgnoreCase)) return true;
        var ns = Name + " ";
        if (input.StartsWith(ns, StringComparison.OrdinalIgnoreCase)) return true;
        foreach (var alias in Aliases)
        {
            if (string.Equals(input, alias, StringComparison.OrdinalIgnoreCase)) return true;
            var aspace = alias + " ";
            if (input.StartsWith(aspace, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
    public abstract Task ExecuteAsync(string args, ChatScreen screen);
}

public static class SlashCommandRegistry
{
    private static readonly List<ISlashCommand> _commands = [];
    private static bool _registered;
    public static IReadOnlyList<ISlashCommand> Commands => _commands;
    public static void Register(ISlashCommand cmd) => _commands.Add(cmd);
    public static void RegisterAll()
    {
        if (_registered) return;
        _registered = true;
        ApplyEndCommands(GuiCommands.All());
    }
    /// <summary>注入端命令：同名（忽略大小写）覆盖，否则追加（对齐主工程 SlashCommandRegistry.ApplyEndCommands）。</summary>
    public static void ApplyEndCommands(IEnumerable<ISlashCommand> cmds)
    {
        foreach (var cmd in cmds)
        {
            var idx = _commands.FindIndex(c => c.Name.Equals(cmd.Name, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) _commands[idx] = cmd;
            else _commands.Add(cmd);
        }
    }
    public static string[] AllNames => _commands.SelectMany(c => new[] { c.Name }.Concat(c.Aliases))
        .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    public static (ISlashCommand? Command, string Args) Match(string userInput)
    {
        foreach (var cmd in _commands)
        {
            var args = SlashMatcher.ExtractArgs(cmd, userInput);
            if (args != null) return (cmd, args);
        }
        return (null, "");
    }
}

/// <summary>GUI 进程占位：主项目 CLI Program 的静态成员（GUI 无 REPL 主循环，InAgentRenderLoop 恒 false）。</summary>
public static partial class Program
{
    public static bool InAgentRenderLoop { get; set; }
    /// <summary>GUI 无 REPL 多槽位主循环，返回空数组（ChatScreen 状态栏据此取槽位 cwd，空则回退启动目录）。</summary>
    public static AgentSlot[] GetSlots() => [];
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
