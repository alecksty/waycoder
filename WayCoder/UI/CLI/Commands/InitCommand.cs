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
    public override string Description => L.Pick("分析项目并生成 AGENT.md（/init claude 生成 CLAUDE.md）", "Analyze the project and generate AGENT.md (/init claude generates CLAUDE.md)");
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
            // ⚠ 「取消」这个选项串**同时是判据**（下面与 choice 比较）——必须与比较共用同一个值，
            //   否则英文界面下选项显示 Cancel、比较却仍是中文，取消会被当成确认。
            var overwrite = L.Pick($"覆盖现有 {fileName}（LLM 重新分析）", $"Overwrite the existing {fileName} (re-analyze with the LLM)");
            var cancel = L.Pick("取消", "Cancel");
            var choice = UxHelper.Select(L.Pick($"已存在 {fileName}，如何操作？", $"{fileName} already exists — what should I do?"),
                new List<string> { overwrite, cancel });
            if (choice == null || choice == cancel)
            {
                screen.AddSystemMsg(L.Pick($"⏭ 已取消，保留现有 {fileName}", $"⏭ Cancelled; keeping the existing {fileName}"));
                return;
            }
        }

#if ANDROID || IOS || MACCATALYST || WINDOWS
        // MAUI 无 Program.RunWithUiLoop / ChatScreen.StartAgentMsg 等桌面 API：LLM 生成仅桌面端可用，移动端用静态模板
        WriteFallback(info, fileName, target, screen, L.Pick("MAUI 用静态模板", "using the static template on MAUI"));
        return;
#else
        // LLM 可用性 → 降级（含自测模式 / 未配置模型）
        var llm = ProgramContext.LLM ?? ProgramContext.Agent?.LlmClient;
        if (!ProjectInitAnalyzer.ShouldUseLlm(llm))
        {
            WriteFallback(info, fileName, target, screen, L.Pick("未配置 LLM", "no LLM configured"));
            return;
        }

        // LLM 路径：后台收集+调用，UI 保持渲染 + 流式推屏
        screen.AddSystemMsg(L.Pick($"🔍 正在用 LLM 分析 {info.PrimaryLanguage} 项目并生成 {fileName} …", $"🔍 Analyzing the {info.PrimaryLanguage} project with the LLM and generating {fileName} …"));
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
                screen.AddSystemMsg(L.Pick("⚠ LLM 返回空内容，回退静态模板。", "⚠ The LLM returned empty content; falling back to the static template."));
                content = ProjectInitAnalyzer.FallbackContent(info, fileName);
            }

            Global.WriteAllTextPreserveBom(target, content);
            screen.AddSystemMsg(BuildSummary(info, fileName, llmDriven));
        }
        catch (Exception ex)
        {
            screen.FinishAgentMsg();
            screen.AddSystemMsg(L.Pick($"⚠ LLM 生成失败：{ex.Message}，已回退静态模板。", $"⚠ LLM generation failed: {ex.Message}. Fell back to the static template."));
            ErrorLog.Error("init", $"LLM /init 失败: {ex.Message}", ex);
            WriteFallback(info, fileName, target, screen, L.Pick("LLM 调用失败", "the LLM call failed"));
        }
#endif
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
        screen.AddSystemMsg(L.Pick($"⚠ {reason}，已用静态模板生成 {fileName}（可稍后 /init force 再用 LLM 生成）。", $"⚠ {reason}; generated {fileName} from the static template (run /init force later to generate it with the LLM)."));
        screen.AddSystemMsg(BuildSummary(info, fileName, llmDriven: false));
    }

    static string BuildSummary(ProjectInfo info, string fileName, bool llmDriven)
    {
        var none = L.Pick("无", "none");
        var frameworks = info.Frameworks.Count > 0 ? string.Join(", ", info.Frameworks) : none;
        var buildTools = info.BuildTools.Count > 0 ? string.Join(", ", info.BuildTools) : none;
        var mode = llmDriven ? L.Pick("LLM 分析", "LLM analysis") : L.Pick("静态模板", "static template");
        return L.Pick(
            $"✅ 已生成 {fileName}（{mode}）\n" +
            $"- 项目: {Path.GetFileName(info.ProjectRoot.TrimEnd('/', '\\'))}\n" +
            $"- 语言: {info.PrimaryLanguage}\n" +
            $"- 框架: {frameworks}\n" +
            $"- 构建: {buildTools}\n" +
            $"下次启动时自动注入此文件，也可现在打开查看补充架构与注意事项。",
            $"✅ Generated {fileName} ({mode})\n" +
            $"- Project: {Path.GetFileName(info.ProjectRoot.TrimEnd('/', '\\'))}\n" +
            $"- Language: {info.PrimaryLanguage}\n" +
            $"- Frameworks: {frameworks}\n" +
            $"- Build: {buildTools}\n" +
            $"This file is injected automatically on the next launch; open it now to review the architecture and gotchas it adds.");
    }
}
