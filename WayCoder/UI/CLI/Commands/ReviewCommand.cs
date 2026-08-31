using System.Text;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /review — 代码审查。生成审查指令（正确性/安全/性能/可维护性/测试覆盖多维度分析），
/// 作为普通消息投递给 Agent 后台执行，审查结果流式显示。Agent 忙时按排队机制等待。
/// 桌面端用 <see cref="ReviewMode.BuildReviewPrompt"/>（含 git diff）；MAUI 无 git（GitRunner 被排除），
/// 用修改文件列表的简化审查。
/// </summary>
public class ReviewCommand : SlashCommand
{
    public override string Name => "/review";
    public override string[] Aliases => ["/审查", "/rv"];
    public override string Description => "审查修改过的代码（git diff + 多维度分析）";
    public override string? Usage => "/review";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        if (ProgramContext.Agent == null)
        {
            screen.AddSystemMsg("⚠ Agent 未初始化，无法审查");
            return Task.CompletedTask;
        }
        var prompt = BuildReviewPrompt();
        screen.AddSystemMsg("🔍 开始代码审查（正确性/安全/性能/可维护性/测试覆盖）… 结果流式显示");
        // 作为普通消息投递：后台 Agent 任务执行审查，不阻塞 REPL（Agent 忙时走排队机制）
        screen.EnqueueSubmission(prompt);
        return Task.CompletedTask;
    }

    /// <summary>MAUI 无 git（GitRunner 排除），用修改文件列表的简化审查；桌面端用 ReviewMode（git diff）。</summary>
    private static string BuildReviewPrompt()
    {
#if ANDROID || IOS
        var files = WayCoder.Tools.EditFileTool.ChangedFiles;
        var sb = new StringBuilder("请审查以下修改的文件：\n");
        foreach (var f in files)
            sb.AppendLine($"- `{f}`");
        sb.AppendLine("\n请从正确性/安全性/性能/可维护性/测试覆盖维度逐一分析，标注严重程度（🔴严重 🟡中等 🟢建议）和所在行号，最后给出总体评价。");
        return sb.ToString();
#else
        return ReviewMode.BuildReviewPrompt();
#endif
    }
}
