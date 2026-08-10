using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

/// <summary>
/// /mode — 查看或切换 Agent 工作模式。
///
/// 四种模式（Shift+Tab 快速切换）：
///   🔨 Build (建造) — 完整工具访问，正常编程
///   🧠 Plan  (计划) — 只分析规划，不修改代码
///   🔍 Review(审查) — 只读代码审查
///   🤖 Auto  (自动) — SmartAuto 智能分级确认
/// </summary>
public class ModeCommand : SlashCommand
{
    public override string Name => "/mode";
    public override string[] Aliases => ["/模式", "/workmode"];
    public override string Description => "切换工作模式：Build / Plan / Review / Auto";
    public override string? Usage => "/mode [build|plan|review|auto]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var arg = args.Trim().ToLower();

        WorkMode? target = arg switch
        {
            "build" or "建造" or "b" => WorkMode.Build,
            "plan" or "计划" or "p" => WorkMode.Plan,
            "review" or "审查" or "r" => WorkMode.Review,
            "auto" or "自动" or "a" => WorkMode.Auto,
            _ => null,
        };

        if (target != null)
        {
            WorkModeManager.SetMode(target.Value);
            screen.StatusBar.CurrentWorkMode = target.Value;

            // 同步到当前槽位
            var activeSlot = Program.ActiveSlotIndex;
            if (activeSlot >= 0 && activeSlot < AgentSlot.Count)
            {
                var slots = Program.GetSlots();
                if (slots != null && activeSlot < slots.Length)
                    slots[activeSlot].WorkMode = target.Value;
            }

            var desc = target.Value switch
            {
                WorkMode.Plan => "只分析规划，不修改代码。使用 Shift+Tab 切回建造模式执行。",
                WorkMode.Review => "只读代码审查。使用 Shift+Tab 切回建造模式修改代码。",
                WorkMode.Auto => "全工具可用 + SmartAuto 智能分级确认。",
                _ => "完整工具访问，正常编程模式。",
            };

            screen.AddMessage(
                $"**工作模式已切换**: {WorkModeManager.Format(target.Value)}\n\n{desc}\n\n💡 快捷键: **Shift+Tab** 循环切换模式",
                "system");
        }
        else
        {
            // 显示当前模式及所有可用模式
            var current = WorkModeManager.CurrentMode;
            var modes = new[] { WorkMode.Build, WorkMode.Plan, WorkMode.Review, WorkMode.Auto };
            var lines = new List<string>
            {
                $"**当前模式**: {WorkModeManager.Format(current)}",
                "",
                "| 快捷键 | 模式 | 说明 |",
                "|--------|------|------|",
            };

            foreach (var m in modes)
            {
                var marker = m == current ? "◀" : " ";
                var emoji = WorkModeManager.Emojis.GetValueOrDefault(m, "?");
                var label = WorkModeManager.Labels.GetValueOrDefault(m, m.ToString());
                var desc = m switch
                {
                    WorkMode.Build => "完整工具 · 正常编程",
                    WorkMode.Plan => "只分析规划 · 不修改代码",
                    WorkMode.Review => "只读审查 · 发现问题和改进点",
                    WorkMode.Auto => "全工具 · SmartAuto 分级确认",
                    _ => "",
                };
                lines.Add($"| {marker} | {emoji} {label} | {desc} |");
            }

            lines.Add("");
            lines.Add("💡 **Shift+Tab** 循环切换 · `/mode <名称>` 直接切换");

            screen.AddMessage(string.Join("\n", lines), "system");
        }

        return Task.CompletedTask;
    }
}
