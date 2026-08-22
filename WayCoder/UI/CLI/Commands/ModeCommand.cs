using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /mode — 查看或切换 Agent 工作模式。
///
/// 三种模式（Shift+Tab 快速切换）：
///   🔨 Build (建造) — 完整工具访问，正常编程（工具/提示词受经济模式管理）
///   🧠 Plan  (计划) — 只读分析/规划，白名单只读工具 + 精简提示词
///   💬 Chat  (聊天) — 纯聊天：0 工具 + 0 提示词
/// </summary>
public class ModeCommand : SlashCommand
{
    public override string Name => "/mode";
    public override string[] Aliases => ["/模式", "/workmode"];
    public override string Description => "切换工作模式：Build / Plan / Chat";
    public override string? Usage => "/mode [build|plan|chat]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var arg = args.Trim().ToLower();

        WorkMode? target = arg switch
        {
            "build" or "建造" or "b" => WorkMode.Build,
            "plan" or "计划" or "p" => WorkMode.Plan,
            "chat" or "聊天" or "c" => WorkMode.Chat,
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

            // 刷新工具集 + 系统提示词（/mode 此前不刷新，工具集与提示词不随档位变化 —— 已修）
            Program.RefreshActiveSlotTools();

            var desc = target.Value switch
            {
                WorkMode.Plan => "只读分析/规划，白名单只读工具 + 精简提示词。产出计划经审批后切回建造模式执行。",
                WorkMode.Chat => "纯聊天：0 工具 + 0 提示词，不能操作文件。需要动代码请切回建造/规划模式。",
                _ => "完整工具访问，正常编程模式（工具与提示词受经济模式管理）。",
            };

            screen.AddMessage(
                $"**工作模式已切换**: {WorkModeManager.Format(target.Value)}\n\n{desc}\n\n💡 快捷键: **Shift+Tab** 或 **Ctrl+K** 循环切换模式",
                "system");
        }
        else
        {
            // 显示当前模式及所有可用模式
            var current = WorkModeManager.CurrentMode;
            var modes = new[] { WorkMode.Build, WorkMode.Plan, WorkMode.Chat };
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
                    WorkMode.Build => "完整工具 · 正常编程（经济模式管工具/提示词）",
                    WorkMode.Plan => "只读分析 · 白名单只读工具 + 精简提示词",
                    WorkMode.Chat => "纯聊天 · 0 工具 0 提示词",
                    _ => "",
                };
                lines.Add($"| {marker} | {emoji} {label} | {desc} |");
            }

            lines.Add("");
            lines.Add("💡 **Shift+Tab** / **Ctrl+K** 循环切换 · `/mode <名称>` 直接切换");

            screen.AddMessage(string.Join("\n", lines), "system");
        }

        return Task.CompletedTask;
    }
}
