using WayCoder.Tools;

namespace WayCoder;

/// <summary>
/// 插件接口 —— 编译期插件贡献工具与斜杠命令。
///
/// 与 SKILL.md（Markdown 技能）、Hooks（外部脚本）、MCP（外部服务器）互补：
/// 插件是「编译进单文件 exe 的 C# 源码扩展」，无反射、AOT 安全，性能与内置工具一致。
///
/// 使用方式（三步，无需改任何启动代码）：
///   1. 在 WayCoder/Plugins/ 目录新建一个 .cs 文件（SDK 自动编译目录下所有 .cs）。
///   2. 继承 <see cref="Plugin"/> 基类，覆写需要的贡献项。
///   3. 用 [ModuleInitializer] 自动注册（详见 docs/插件系统.md）：
///        internal static class MyPluginInit
///        {
///            [System.Runtime.CompilerServices.ModuleInitializer]
///            internal static void Init() => PluginRegistry.Register(new MyPlugin());
///        }
///   4. dotnet publish -c Release 重新编译，插件随单文件 exe 一起分发。
/// </summary>
public interface IPlugin
{
    /// <summary>插件名（唯一标识，重复注册会覆盖）。</summary>
    string Name { get; }
    /// <summary>插件版本。</summary>
    string Version { get; }
    /// <summary>插件贡献的工具（默认空）。</summary>
    IEnumerable<ITool> GetTools();
    /// <summary>插件贡献的斜杠命令（默认空）。</summary>
    IEnumerable<ISlashCommand> GetCommands();
}

/// <summary>插件基类 —— GetTools/GetCommands 默认空实现，子类只覆写需要的部分。</summary>
public abstract class Plugin : IPlugin
{
    public abstract string Name { get; }
    public virtual string Version => "1.0.0";
    public virtual IEnumerable<ITool> GetTools() => [];
    public virtual IEnumerable<ISlashCommand> GetCommands() => [];
}

/// <summary>
/// 插件注册表 —— 收集所有编译期插件贡献的工具与斜杠命令。
/// <see cref="ToolRegistry.AllTools"/> 与 <see cref="SlashCommandRegistry.RegisterAll"/> 自动集成。
/// </summary>
public static class PluginRegistry
{
    static readonly List<IPlugin> _plugins = [];

    /// <summary>已注册插件（按注册顺序）。</summary>
    public static IReadOnlyList<IPlugin> Plugins => _plugins;

    /// <summary>注册插件。同名插件（忽略大小写）后注册覆盖先注册，幂等。</summary>
    public static void Register(IPlugin plugin)
    {
        if (plugin == null) return;
        var idx = _plugins.FindIndex(p => string.Equals(p.Name, plugin.Name, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0) _plugins[idx] = plugin;
        else _plugins.Add(plugin);
    }

    /// <summary>按名移除插件。返回是否移除了任何插件。</summary>
    public static bool Unregister(string name)
    {
        return _plugins.RemoveAll(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) > 0;
    }

    /// <summary>收集所有插件贡献的工具（去 null，防御不严谨插件）。</summary>
    public static IEnumerable<ITool> CollectTools() =>
        _plugins.SelectMany(p => p.GetTools() ?? []);

    /// <summary>收集所有插件贡献的斜杠命令（去 null）。</summary>
    public static IEnumerable<ISlashCommand> CollectCommands() =>
        _plugins.SelectMany(p => p.GetCommands() ?? []);
}
