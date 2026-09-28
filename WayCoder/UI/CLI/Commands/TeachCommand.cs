using System.Text;
using WayCoder.Infra;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /teach —— 教学模式 + 测验闭环：
///   /teach on|off     教学模式开关（AI 讲解为什么 + 提问巩固）
///   /teach assess     评估本次教学会话问答 → 更新知识库 gap 权重（掌握降、未掌握升+进复习）
///   /teach status     教学进度（按权重分组：基本掌握/待复习/学习中）
/// </summary>
public class TeachCommand : SlashCommand
{
    public override string Name => "/teach";
    public override string Description => L.Pick("教学模式（on/off）· 评估（assess）· 进度（status）", "Teaching mode (on/off) · assess · progress (status)");
    public override string? Usage => "/teach [on|off|assess|status]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var trimmed = (args ?? "").Trim();
        if (trimmed.Equals("assess", StringComparison.OrdinalIgnoreCase))
            return Assess(screen);
        if (trimmed.Equals("status", StringComparison.OrdinalIgnoreCase))
        {
            screen.AddSystemMsg(KbIndex.FormatTeachStatus());
            return Task.CompletedTask;
        }

        // on/off/无参：开关控制（保留原行为）
        if (trimmed.Length == 0)
        {
            screen.AddSystemMsg(Config.Instance.TeachModeEnabled
                ? L.Pick("🧑‍🏫 教学模式已开启（/teach off 关闭；完成测验后 /teach assess 记录掌握）", "🧑‍🏫 Teaching mode is on (/teach off to turn it off; run /teach assess after a quiz to record mastery)")
                : L.Pick("🧑‍🏫 教学模式已关闭（/teach on 开启：AI 讲解为什么 + 提问巩固）", "🧑‍🏫 Teaching mode is off (/teach on to enable: the AI explains why and asks follow-up questions)"));
            return Task.CompletedTask;
        }

        bool enable = trimmed switch
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
            ? L.Pick("🧑‍🏫 教学模式已开启：后续 AI 会逐处解释为什么、错误归因、类比追问，完成后 3 问测验（/teach assess 可记录掌握）。", "🧑‍🏫 Teaching mode enabled: the AI will explain the why behind each step, attribute errors, and ask analogy follow-ups, then finish with a 3-question quiz (/teach assess records mastery).")
            : L.Pick("🧑‍🏫 教学模式已关闭，恢复极简执行风格。", "🧑‍🏫 Teaching mode disabled; back to the terse execution style."));
        return Task.CompletedTask;
    }

    /// <summary>/teach assess：评估本次教学会话问答 → 更新 gap 权重。</summary>
    static Task Assess(ChatScreen screen)
    {
        var agent = ProgramContext.Agent;
        if (agent == null) { screen.AddSystemMsg(L.Pick("无活跃会话可评估。", "No active session to assess.")); return Task.CompletedTask; }

        var transcript = CommandTextHelpers.BuildTranscript(agent.SnapshotMessages(), 1500);
        if (transcript.Length < 60) { screen.AddSystemMsg(L.Pick("会话内容太少（需至少一轮教学问答）。先 `/teach on` 后让 AI 讲解并答题。", "The session is too short (needs at least one round of teaching Q&A). Run `/teach on`, then have the AI explain something and answer its questions.")); return Task.CompletedTask; }

        screen.AddSystemMsg(L.Pick("📝 正在评估本次教学问答…", "📝 Assessing this teaching session…"));
        var (mastered, weak) = KbIndex.AssessTranscript(transcript).GetAwaiter().GetResult();
        var (mApplied, wApplied) = KbIndex.ApplyAssessment(mastered, weak);

        var sb = new StringBuilder(L.Pick("📊 教学评估完成\n", "📊 Teaching assessment complete\n"));
        sb.AppendLine(L.Pick($"\n✅ 已掌握 {mastered.Count} 项（应用 {mApplied} 项到知识库）：", $"\n✅ Mastered {mastered.Count} item(s) ({mApplied} applied to the knowledge base):"));
        foreach (var t in mastered) sb.AppendLine($"  · {t}");
        sb.AppendLine(L.Pick($"\n🔴 待复习 {weak.Count} 项（应用 {wApplied} 项，已进 /kb review 轮换）：", $"\n🔴 To review: {weak.Count} item(s) ({wApplied} applied, queued for /kb review):"));
        foreach (var t in weak) sb.AppendLine($"  · {t}");
        sb.AppendLine(L.Pick("\n查看进度：/teach status · 复习弱项：/kb review", "\nSee progress: /teach status · Review weak items: /kb review"));
        screen.AddSystemMsg(sb.ToString());
        return Task.CompletedTask;
    }

}
