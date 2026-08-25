using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /teach —— 教学模式开关：AI 不只执行，还逐处解释为什么 + 结束时提问巩固（提高编程技能）。
/// 开关经 SystemPrompt 教学块生效；切换后重建当前 Agent 的系统提示词。
/// </summary>
public class TeachCommand : SlashCommand
{
    public override string Name => "/teach";
    public override string Description => "教学模式开关（AI 讲解为什么 + 提问巩固）";
    public override string? Usage => "/teach [on|off]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var arg = args.Trim().ToLowerInvariant();
        if (arg.Length == 0)
        {
            screen.AddSystemMsg(Config.Instance.TeachModeEnabled
                ? "🧑‍🏫 教学模式已开启（/teach off 关闭）"
                : "🧑‍🏫 教学模式已关闭（/teach on 开启：AI 讲解为什么 + 提问巩固）");
            return Task.CompletedTask;
        }

        bool enable = arg switch
        {
            "on" or "1" or "true" or "y" or "yes" => true,
            "off" or "0" or "false" or "n" or "no" => false,
            _ => !Config.Instance.TeachModeEnabled, // 其它输入 = 切换
        };

        Config.Instance.TeachModeEnabled = enable;
        Config.Instance.SaveToConfigJson();
        var agent = ProgramContext.Agent;
        agent?.ReapplyToolFilter(); // 重建系统提示词，教学块即刻生效

        screen.AddSystemMsg(enable
            ? "🧑‍🏫 教学模式已开启：后续 AI 会逐处解释为什么，并在完成后提问巩固。"
            : "🧑‍🏫 教学模式已关闭，恢复极简执行风格。");
        return Task.CompletedTask;
    }
}
