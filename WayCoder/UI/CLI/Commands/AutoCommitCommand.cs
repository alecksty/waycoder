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
    public override string Description => L.Pick("自动 Git Commit：AI 修改文件后自动提交", "Auto Git commit: commit automatically after the AI edits files");
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
            screen.AddMessage(L.Pick("⚠ Agent 未初始化", "⚠ Agent not initialized"), "system");
            return Task.CompletedTask;
        }

        var arg = args.Trim().ToLower();

        if (arg == "on" || arg == "1" || arg == "true")
        {
            if (agent.AutoCommitEnabled)
            {
                screen.AddMessage(L.Pick("✅ **自动提交**已在运行中。\n\n每次 AI 修改文件后自动 git commit（conventional-commit 格式）。", "✅ **Auto commit** is already running.\n\nEvery AI file edit is followed by an automatic git commit (conventional-commit format)."), "system");
                return Task.CompletedTask;
            }

            agent.AutoCommitEnabled = true;

            // 注册反馈回调
            agent.OnAutoCommit((msg, fileCount) =>
            {
                screen.AddSystemMsg(L.Pick($"📦 自动提交 [{fileCount} 文件]: {msg}", $"📦 Auto commit [{fileCount} file(s)]: {msg}"));
            });

            screen.AddMessage(
                L.Pick(
                "📦 **自动 Git Commit 已开启**\n\n" +
                "**工作流程**：\n" +
                "1. AI 修改文件（write_file / edit_file）\n" +
                "2. 小模型生成 conventional-commit 信息\n" +
                "3. 精准 `git add` 实际修改文件 + `git commit`\n\n" +
                "使用 **/autocommit off** 关闭。",
                "📦 **Auto Git commit enabled**\n\n" +
                "**Workflow**:\n" +
                "1. The AI edits files (write_file / edit_file)\n" +
                "2. The small model writes a conventional-commit message\n" +
                "3. `git add` the files actually changed, then `git commit`\n\n" +
                "Use **/autocommit off** to turn it off."),
                "system");
        }
        else if (arg == "off" || arg == "0" || arg == "false")
        {
            if (!agent.AutoCommitEnabled)
            {
                screen.AddMessage(L.Pick("ℹ 自动提交未开启。使用 /autocommit on 开启。", "ℹ Auto commit is not on. Use /autocommit on to enable it."), "system");
                return Task.CompletedTask;
            }

            agent.AutoCommitEnabled = false;
            screen.AddMessage(L.Pick("✅ 自动 Git Commit 已关闭。", "✅ Auto Git commit disabled."), "system");
        }
        else
        {
            var status = agent.AutoCommitEnabled
                ? L.Pick("🟢 **自动提交已开启**\n\n每次 AI 修改文件后自动 git commit。\n\n使用 **/autocommit off** 关闭。", "🟢 **Auto commit is on**\n\nEvery AI file edit is followed by an automatic git commit.\n\nUse **/autocommit off** to turn it off.")
                : L.Pick("⚪ **自动提交未开启**\n\n使用 **/autocommit on** 开启，或设置环境变量 `WAYCODER_AUTO_COMMIT=1`。", "⚪ **Auto commit is off**\n\nUse **/autocommit on** to enable it, or set the `WAYCODER_AUTO_COMMIT=1` environment variable.");

            screen.AddMessage(status, "system");
        }

        return Task.CompletedTask;
    }
}
