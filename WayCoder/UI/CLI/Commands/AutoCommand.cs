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
    public override string Description => "智能 Auto Mode：Safe 放行 / Cautious 记一次 / Dangerous 每次确认";
    public override string? Usage => "/auto [on|off|status]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var arg = args.Trim().ToLower();

        if (arg == "on" || arg == "1" || arg == "true" || arg == "smart")
        {
            if (PermissionManager.CurrentMode == PermissionManager.Mode.SmartAuto)
            {
                screen.AddMessage(
                    "✅ **SmartAuto 已开启**\n\n" +
                    "| 级别 | 工具 | 行为 |\n" +
                    "|------|------|------|\n" +
                    "| 🟢 Safe | read_file, ls, grep, glob, stat, diff... | 自动放行 |\n" +
                    "| 🟡 Cautious | write_file, edit_file, mkdir, cp, mv... | 首次确认后记住 |\n" +
                    "| 🔴 Dangerous | rm, bash, git, kill, agent | 每次确认 |\n\n" +
                    $"连续 {AutoModeClassifier.BlockThreshold} 次拒绝危险操作 → 自动退回 Ask 模式",
                    "system");
                return Task.CompletedTask;
            }

            PermissionManager.SetMode("smartauto");
            screen.AddMessage(
                "🧠 **SmartAuto 模式已开启**\n\n" +
                "| 级别 | 工具 | 行为 |\n" +
                "|------|------|------|\n" +
                "| 🟢 Safe | read_file, ls, grep, glob, stat, diff... | 自动放行 |\n" +
                "| 🟡 Cautious | write_file, edit_file, mkdir, cp, mv... | 首次确认后记住 |\n" +
                "| 🔴 Dangerous | rm, bash, git, kill, agent | 每次确认 |\n\n" +
                $"💡 连续 {AutoModeClassifier.BlockThreshold} 次拒绝危险操作后将自动退回 Ask 模式\n" +
                "使用 **/auto off** 关闭",
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
                screen.AddMessage("ℹ 当前已是 **Ask（每次确认）** 模式。", "system");
                return Task.CompletedTask;
            }

            PermissionManager.SetMode("ask");
            screen.AddMessage("✅ 已切换为 **Ask（每次确认）** 模式。", "system");
        }
        else if (arg == "yolo" || arg == "god")
        {
            PermissionManager.SetMode("yolo");
            screen.AddMessage("⚠ **YOLO 模式**：所有操作直接执行，不确认。\n使用 **/auto off** 恢复安全模式。", "system");
        }
        else
        {
            // 无参数 → 显示当前状态
            var (label, emoji) = PermissionManager.CurrentMode switch
            {
                PermissionManager.Mode.Yolo => ("YOLO (上帝模式)", "⚠"),
                PermissionManager.Mode.SmartAuto => ("SmartAuto (智能分级)", "🧠"),
                PermissionManager.Mode.Auto => ("Auto (智能确认)", "🟢"),
                _ => ("Ask (每次确认)", "🟡"),
            };

            var statsInfo = PermissionManager.CurrentMode == PermissionManager.Mode.SmartAuto
                ? $"\n\n**分级统计**：{AutoModeClassifier.GetStats()}"
                : "";

            screen.AddMessage(
                $"**当前权限模式**：{emoji} {label}{statsInfo}\n\n" +
                "**切换**：\n" +
                "- `/auto on` — 开启 SmartAuto 智能分级\n" +
                "- `/auto off` — 回到 Ask 每次确认\n" +
                "- `/auto yolo` — 上帝模式（不推荐）\n\n" +
                "**SmartAuto 分级逻辑**：\n" +
                "| 级别 | 行为 |\n" +
                "|------|------|\n" +
                "| 🟢 Safe | read/ls/grep 等只读 → 自动放行 |\n" +
                "| 🟡 Cautious | write/edit/mkdir 等修改 → 首次确认后记住 |\n" +
                "| 🔴 Dangerous | rm/bash/git/kill/agent → 每次确认 |",
                "system");
        }

        return Task.CompletedTask;
    }
}
