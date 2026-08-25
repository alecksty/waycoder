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
    public override string Description => "教学模式（on/off）· 评估（assess）· 进度（status）";
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
                ? "🧑‍🏫 教学模式已开启（/teach off 关闭；完成测验后 /teach assess 记录掌握）"
                : "🧑‍🏫 教学模式已关闭（/teach on 开启：AI 讲解为什么 + 提问巩固）");
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
            ? "🧑‍🏫 教学模式已开启：后续 AI 会逐处解释为什么、错误归因、类比追问，完成后 3 问测验（/teach assess 可记录掌握）。"
            : "🧑‍🏫 教学模式已关闭，恢复极简执行风格。");
        return Task.CompletedTask;
    }

    /// <summary>/teach assess：评估本次教学会话问答 → 更新 gap 权重。</summary>
    static Task Assess(ChatScreen screen)
    {
        var agent = ProgramContext.Agent;
        if (agent == null) { screen.AddSystemMsg("无活跃会话可评估。"); return Task.CompletedTask; }

        var transcript = BuildTranscript(agent.SnapshotMessages());
        if (transcript.Length < 60) { screen.AddSystemMsg("会话内容太少（需至少一轮教学问答）。先 `/teach on` 后让 AI 讲解并答题。"); return Task.CompletedTask; }

        screen.AddSystemMsg("📝 正在评估本次教学问答…");
        var (mastered, weak) = KbIndex.AssessTranscript(transcript).GetAwaiter().GetResult();
        var (mApplied, wApplied) = KbIndex.ApplyAssessment(mastered, weak);

        var sb = new StringBuilder($"📊 教学评估完成\n");
        sb.AppendLine($"\n✅ 已掌握 {mastered.Count} 项（应用 {mApplied} 项到知识库）：");
        foreach (var t in mastered) sb.AppendLine($"  · {t}");
        sb.AppendLine($"\n🔴 待复习 {weak.Count} 项（应用 {wApplied} 项，已进 /kb review 轮换）：");
        foreach (var t in weak) sb.AppendLine($"  · {t}");
        sb.AppendLine("\n查看进度：/teach status · 复习弱项：/kb review");
        screen.AddSystemMsg(sb.ToString());
        return Task.CompletedTask;
    }

    /// <summary>把会话消息拼成 role 前缀纯文本（供教学评估 LLM）。</summary>
    static string BuildTranscript(List<JNode> messages)
    {
        var sb = new StringBuilder();
        foreach (var m in messages)
        {
            var role = m["role"]?.AsString() ?? "?";
            var content = m["content"]?.AsString() ?? "";
            if (content.Length == 0) continue;
            if (content.Length > 1500) content = ContextManager.TruncateByRunes(content, 1500);
            sb.AppendLine($"## {role}");
            sb.AppendLine(content);
        }
        return sb.ToString();
    }
}
