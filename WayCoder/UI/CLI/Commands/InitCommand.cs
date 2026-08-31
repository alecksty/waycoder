using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /init —— 分析项目并生成 AGENT.md（默认；`/init claude` 生成 CLAUDE.md 兼容 Claude Code）。
///
/// LLM 驱动：程序化收集代码库上下文（项目检测/常用命令/仓库地图/已有规则/README/Git 状态），
/// 单次 LLM 调用生成真实、非显然、渐进披露的指导文件（对标 Crush/Claude Code 的 init）。
/// 无 LLM 或调用失败时降级为静态模板（ProjectInitializer.GenerateAgentMd）。
/// </summary>
public class InitCommand : SlashCommand
{
    public override string Name => "/init";
    public override string Description => "分析项目并生成 AGENT.md（/init claude 生成 CLAUDE.md）";
    public override string? Usage => "/init [force|claude]";

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        var force = args.Contains("force", StringComparison.OrdinalIgnoreCase)
                 || args.Contains("-f", StringComparison.OrdinalIgnoreCase);
        var wantClaude = args.Contains("claude", StringComparison.OrdinalIgnoreCase);
        var fileName = wantClaude ? "CLAUDE.md" : "AGENT.md";

        var info = ProjectContext.DetectProject();
        var target = Path.Combine(info.ProjectRoot, fileName);

        // 已有文件确认（放 LLM 调用前，避免白花 token 生成后又取消）
        if (File.Exists(target) && !force)
        {
            var choice = UxHelper.Select($"已存在 {fileName}，如何操作？",
                new List<string> { $"覆盖现有 {fileName}（LLM 重新分析）", "取消" });
            if (choice == null || choice == "取消")
            {
                screen.AddSystemMsg($"⏭ 已取消，保留现有 {fileName}");
                return;
            }
        }

        // LLM 可用性 → 降级（含自测模式 / 未配置模型）
        var llm = ProgramContext.LLM ?? ProgramContext.Agent?.LlmClient;
        if (!ProjectInitAnalyzer.ShouldUseLlm(llm))
        {
            WriteFallback(info, fileName, target, screen, "未配置 LLM");
            return;
        }

        // LLM 路径：后台收集+调用，UI 保持渲染 + 流式推屏
        screen.AddSystemMsg($"🔍 正在用 LLM 分析 {info.PrimaryLanguage} 项目并生成 {fileName} …");
        screen.StartAgentMsg();
        try
        {
            var content = await Program.RunWithUiLoop(
                () => RunLlmInitAsync(llm!, info, fileName,
                            tok => screen.PostToUI(() => screen.AppendToken(tok)))
                       .GetAwaiter().GetResult(),
                screen);
            screen.FinishAgentMsg();

            var llmDriven = !string.IsNullOrWhiteSpace(content);
            if (!llmDriven)
            {
                screen.AddSystemMsg("⚠ LLM 返回空内容，回退静态模板。");
                content = ProjectInitAnalyzer.FallbackContent(info, fileName);
            }

            Global.WriteAllTextPreserveBom(target, content);
            screen.AddSystemMsg(BuildSummary(info, fileName, llmDriven));
        }
        catch (Exception ex)
        {
            screen.FinishAgentMsg();
            screen.AddSystemMsg($"⚠ LLM 生成失败：{ex.Message}，已回退静态模板。");
            ErrorLog.Error("init", $"LLM /init 失败: {ex.Message}", ex);
            WriteFallback(info, fileName, target, screen, "LLM 调用失败");
        }
    }

    /// <summary>收集上下文 → 单次 LLM 调用 → 清理围栏，返回生成内容。</summary>
    static async Task<string> RunLlmInitAsync(LLM llm, ProjectInfo info, string fileName, Action<string> onToken)
    {
        var ctx = ProjectInitAnalyzer.CollectInitContext(info, fileName);
        var prompt = ProjectInitAnalyzer.BuildPrompt(fileName, ctx);
        var messages = new List<JNode>
        {
            JNode.Object().Set("role", "system")
                .Set("content", "你是资深的代码架构师。严格基于提供的代码库上下文撰写项目指导文件：只写观察到的，绝不虚构，不输出解释与代码围栏。"),
            JNode.Object().Set("role", "user").Set("content", prompt),
        };
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(180)); // 整体兜底，防 /init 卡死
        var resp = await llm.ChatAsync(messages, tools: null, onToken: onToken, cancellationToken: cts.Token);
        return ProjectInitAnalyzer.CleanFenced(resp.Content ?? "");
    }

    /// <summary>降级写静态模板（无 LLM / 调用失败）。</summary>
    static void WriteFallback(ProjectInfo info, string fileName, string target, ChatScreen screen, string reason)
    {
        var content = ProjectInitAnalyzer.FallbackContent(info, fileName);
        Global.WriteAllTextPreserveBom(target, content);
        screen.AddSystemMsg($"⚠ {reason}，已用静态模板生成 {fileName}（可稍后 /init force 再用 LLM 生成）。");
        screen.AddSystemMsg(BuildSummary(info, fileName, llmDriven: false));
    }

    static string BuildSummary(ProjectInfo info, string fileName, bool llmDriven)
    {
        var frameworks = info.Frameworks.Count > 0 ? string.Join(", ", info.Frameworks) : "无";
        var buildTools = info.BuildTools.Count > 0 ? string.Join(", ", info.BuildTools) : "无";
        var mode = llmDriven ? "LLM 分析" : "静态模板";
        return $"✅ 已生成 {fileName}（{mode}）\n" +
            $"- 项目: {Path.GetFileName(info.ProjectRoot.TrimEnd('/', '\\'))}\n" +
            $"- 语言: {info.PrimaryLanguage}\n" +
            $"- 框架: {frameworks}\n" +
            $"- 构建: {buildTools}\n" +
            $"下次启动时自动注入此文件，也可现在打开查看补充架构与注意事项。";
    }
}
