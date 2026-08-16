using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /autocommit — 切换自动 Git Commit 模式。
/// 开启后，每次 AI 修改文件后自动 git add + git commit，由小模型生成 conventional-commit 提交信息。
/// </summary>
public class AutoCommitCommand : SlashCommand
{
    public override string Name => "/autocommit";
    public override string[] Aliases => ["/自动提交", "/ac"];
    public override string Description => "自动 Git Commit：AI 修改文件后自动提交";
    public override string? Usage => "/autocommit [on|off|status]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        // 从当前活跃槽位获取 Agent
        var slots = Program.GetSlots();
        var activeIdx = Program.ActiveSlotIndex;
        var agent = (activeIdx >= 0 && activeIdx < slots.Length)
            ? slots[activeIdx].Agent
            : null;

        if (agent == null)
        {
            screen.AddMessage("⚠ Agent 未初始化", "system");
            return Task.CompletedTask;
        }

        var arg = args.Trim().ToLower();

        if (arg == "on" || arg == "1" || arg == "true")
        {
            if (agent.AutoCommitEnabled)
            {
                screen.AddMessage("✅ **自动提交**已在运行中。\n\n每次 AI 修改文件后自动 git commit（conventional-commit 格式）。", "system");
                return Task.CompletedTask;
            }

            agent.AutoCommitEnabled = true;

            // 注册反馈回调
            agent.OnAutoCommit((msg, fileCount) =>
            {
                screen.AddSystemMsg($"📦 自动提交 [{fileCount} 文件]: {msg}");
            });

            screen.AddMessage(
                "📦 **自动 Git Commit 已开启**\n\n" +
                "**工作流程**：\n" +
                "1. AI 修改文件（write_file / edit_file）\n" +
                "2. 小模型生成 conventional-commit 信息\n" +
                "3. 精准 `git add` 实际修改文件 + `git commit`\n\n" +
                "使用 **/autocommit off** 关闭。",
                "system");
        }
        else if (arg == "off" || arg == "0" || arg == "false")
        {
            if (!agent.AutoCommitEnabled)
            {
                screen.AddMessage("ℹ 自动提交未开启。使用 /autocommit on 开启。", "system");
                return Task.CompletedTask;
            }

            agent.AutoCommitEnabled = false;
            screen.AddMessage("✅ 自动 Git Commit 已关闭。", "system");
        }
        else
        {
            var status = agent.AutoCommitEnabled
                ? "🟢 **自动提交已开启**\n\n每次 AI 修改文件后自动 git commit。\n\n使用 **/autocommit off** 关闭。"
                : "⚪ **自动提交未开启**\n\n使用 **/autocommit on** 开启，或设置环境变量 `WAYCODER_AUTO_COMMIT=1`。";

            screen.AddMessage(status, "system");
        }

        return Task.CompletedTask;
    }
}
