using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /doctor —— 发行后系统自检与安全修复。
/// 无参或 status 只做只读检查；fix 执行可安全自动处理的项目。
/// </summary>
public class DoctorCommand : SlashCommand
{
    public override string Name => "/doctor";
    public override string[] Aliases => ["/diagnose"];
    public override string Description => L.Pick("系统自检 / 安全修复", "System check / safe repair");
    public override string? Usage => "/doctor [status|fix]";

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        var mode = args.Trim().ToLowerInvariant();
        var fix = mode is "fix" or "修复" or "repair";
        if (!fix && mode.Length > 0 && mode is not ("status" or "check" or "自检"))
        {
            screen.AddSystemMsg(L.Pick($"用法: {Usage}\n无参数运行只读自检；/doctor fix 执行安全修复。", $"Usage: {Usage}\nRun with no arguments for a read-only check; /doctor fix applies safe repairs."));
            return;
        }

        var options = new DoctorOptions
        {
            Home = Global.Home,
            Cwd = Environment.CurrentDirectory,
            Fix = fix,
            Models = [Config.Instance.Model, Config.Instance.SmallModel],
        };

        var report = await DoctorEngine.RunAsync(options);
        screen.AddSystemMsg(report.Render().TrimEnd('\n'));

        if (fix)
        {
            Config.Reload();
            try
            {
                ProgramContext.Config = Config.Instance;
            }
            catch (Exception ex)
            {
                screen.AddSystemMsg(L.Pick($"配置重载失败，请检查 config.json 后重试: {ex.Message}", $"Failed to reload the config; check config.json and try again: {ex.Message}"));
            }
        }
    }
}
