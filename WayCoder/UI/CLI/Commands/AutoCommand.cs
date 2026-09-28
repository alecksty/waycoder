using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /auto — 切换智能 Auto Mode。
///
/// SmartAuto 模式使用三级分类器：
///   Safe（read/ls/grep 等）→ 自动放行
///   Cautious（write/edit/mkdir 等）→ 首次确认后记住
///   Dangerous（rm/bash/git 等）→ 每次确认，连续 3 次拒绝后退回 Ask
/// </summary>
public class AutoCommand : SlashCommand
{
    public override string Name => "/auto";
    public override string[] Aliases => ["/自动", "/auto-mode"];
    public override string Description => L.Pick("智能 Auto Mode：Safe 放行 / Cautious 记一次 / Dangerous 每次确认", "Smart Auto Mode: Safe allowed / Cautious remembered once / Dangerous confirmed every time");
    public override string? Usage => "/auto [on|off|status]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var arg = args.Trim().ToLower();

        if (arg == "on" || arg == "1" || arg == "true" || arg == "smart")
        {
            if (PermissionManager.CurrentMode == PermissionManager.Mode.SmartAuto)
            {
                screen.AddMessage(
                    L.Pick("✅ **SmartAuto 已开启**\n\n", "✅ **SmartAuto is on**\n\n") +
                    L.Pick("| 级别 | 工具 | 行为 |\n", "| Level | Tools | Behavior |\n") +
                    "|------|------|------|\n" +
                    L.Pick("| 🟢 Safe | read_file, ls, grep, glob, stat, diff... | 自动放行 |\n", "| 🟢 Safe | read_file, ls, grep, glob, stat, diff... | auto-allowed |\n") +
                    L.Pick("| 🟡 Cautious | write_file, edit_file, mkdir, cp, mv... | 首次确认后记住 |\n", "| 🟡 Cautious | write_file, edit_file, mkdir, cp, mv... | remembered after the first confirmation |\n") +
                    L.Pick("| 🔴 Dangerous | rm, bash, git, kill, agent | 每次确认 |\n\n", "| 🔴 Dangerous | rm, bash, git, kill, agent | confirmed every time |\n\n") +
                    L.Pick($"连续 {AutoModeClassifier.BlockThreshold} 次拒绝危险操作 → 自动退回 Ask 模式", $"{AutoModeClassifier.BlockThreshold} consecutive refusals of dangerous operations → falls back to Ask mode"),
                    "system");
                return Task.CompletedTask;
            }

            PermissionManager.SetMode("smartauto");
            screen.AddMessage(
                L.Pick("🧠 **SmartAuto 模式已开启**\n\n", "🧠 **SmartAuto mode is on**\n\n") +
                L.Pick("| 级别 | 工具 | 行为 |\n", "| Level | Tools | Behavior |\n") +
                "|------|------|------|\n" +
                L.Pick("| 🟢 Safe | read_file, ls, grep, glob, stat, diff... | 自动放行 |\n", "| 🟢 Safe | read_file, ls, grep, glob, stat, diff... | auto-allowed |\n") +
                L.Pick("| 🟡 Cautious | write_file, edit_file, mkdir, cp, mv... | 首次确认后记住 |\n", "| 🟡 Cautious | write_file, edit_file, mkdir, cp, mv... | remembered after the first confirmation |\n") +
                L.Pick("| 🔴 Dangerous | rm, bash, git, kill, agent | 每次确认 |\n\n", "| 🔴 Dangerous | rm, bash, git, kill, agent | confirmed every time |\n\n") +
                L.Pick($"💡 连续 {AutoModeClassifier.BlockThreshold} 次拒绝危险操作后将自动退回 Ask 模式\n", $"💡 After {AutoModeClassifier.BlockThreshold} consecutive refusals of dangerous operations it falls back to Ask mode\n") +
                L.Pick("使用 **/auto off** 关闭", "Use **/auto off** to turn it off"),
                "system");

            // 订阅退回事件以显示通知
            PermissionManager.ModeFallbackTriggered += msg =>
            {
                screen.AddMessage(msg, "system");
            };
        }
        else if (arg == "off" || arg == "0" || arg == "false" || arg == "ask")
        {
            if (PermissionManager.CurrentMode == PermissionManager.Mode.Ask)
            {
                screen.AddMessage(L.Pick("ℹ 当前已是 **Ask（每次确认）** 模式。", "ℹ Already in **Ask (confirm every time)** mode."), "system");
                return Task.CompletedTask;
            }

            PermissionManager.SetMode("ask");
            screen.AddMessage(L.Pick("✅ 已切换为 **Ask（每次确认）** 模式。", "✅ Switched to **Ask (confirm every time)** mode."), "system");
        }
        else if (arg == "yolo" || arg == "god")
        {
            PermissionManager.SetMode("yolo");
            screen.AddMessage(L.Pick("⚠ **YOLO 模式**：所有操作直接执行，不确认。\n使用 **/auto off** 恢复安全模式。", "⚠ **YOLO mode**: every operation runs immediately with no confirmation.\nUse **/auto off** to return to the safe mode."), "system");
        }
        else
        {
            // 无参数 → 显示当前状态
            // emoji 是命令自己的呈现选择（留在本地），文案走 UiText 唯一真源
            var emoji = PermissionManager.CurrentMode switch
            {
                PermissionManager.Mode.Yolo => "⚠",
                PermissionManager.Mode.SmartAuto => "🧠",
                PermissionManager.Mode.Auto => "🟢",
                _ => "🟡",
            };
            var label = UiText.PermLabel(PermissionManager.CurrentMode);

            var statsInfo = PermissionManager.CurrentMode == PermissionManager.Mode.SmartAuto
                ? L.Pick($"\n\n**分级统计**：{AutoModeClassifier.GetStats()}", $"\n\n**Classification stats**: {AutoModeClassifier.GetStats()}")
                : "";

            screen.AddMessage(
                L.Pick($"**当前权限模式**：{emoji} {label}{statsInfo}\n\n", $"**Current permission mode**: {emoji} {label}{statsInfo}\n\n") +
                L.Pick("**切换**：\n", "**Switch**:\n") +
                L.Pick("- `/auto on` — 开启 SmartAuto 智能分级\n", "- `/auto on` — enable SmartAuto classification\n") +
                L.Pick("- `/auto off` — 回到 Ask 每次确认\n", "- `/auto off` — back to Ask, confirm every time\n") +
                L.Pick("- `/auto yolo` — 上帝模式（不推荐）\n\n", "- `/auto yolo` — god mode (not recommended)\n\n") +
                L.Pick("**SmartAuto 分级逻辑**：\n", "**SmartAuto classification logic**:\n") +
                L.Pick("| 级别 | 行为 |\n", "| Level | Behavior |\n") +
                "|------|------|\n" +
                L.Pick("| 🟢 Safe | read/ls/grep 等只读 → 自动放行 |\n", "| 🟢 Safe | read-only ops like read/ls/grep → auto-allowed |\n") +
                L.Pick("| 🟡 Cautious | write/edit/mkdir 等修改 → 首次确认后记住 |\n", "| 🟡 Cautious | modifications like write/edit/mkdir → remembered after the first confirmation |\n") +
                L.Pick("| 🔴 Dangerous | rm/bash/git/kill/agent → 每次确认 |", "| 🔴 Dangerous | rm/bash/git/kill/agent → confirmed every time |"),
                "system");
        }

        return Task.CompletedTask;
    }
}
