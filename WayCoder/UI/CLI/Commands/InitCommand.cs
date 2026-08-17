using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /init —— 对标 Claude Code /init：扫描项目，生成 CLAUDE.md 指导文件。
/// 复用 ProjectContext.DetectProject() 检测 + ProjectInitializer.GenerateClaudeMd() 生成。
/// </summary>
public class InitCommand : SlashCommand
{
    public override string Name => "/init";
    public override string Description => "分析项目并生成 CLAUDE.md";
    public override string? Usage => "/init [force]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var force = args.Contains("force", StringComparison.OrdinalIgnoreCase)
                 || args.Contains("-f", StringComparison.OrdinalIgnoreCase);

        var info = ProjectContext.DetectProject();
        var target = Path.Combine(info.ProjectRoot, "CLAUDE.md");

        if (File.Exists(target) && !force)
        {
            var choice = UxHelper.Select("已存在 CLAUDE.md，如何操作？",
                new List<string> { "覆盖现有 CLAUDE.md", "取消" });
            if (choice == null || choice == "取消")
            {
                screen.AddSystemMsg("⏭ 已取消，保留现有 CLAUDE.md");
                return Task.CompletedTask;
            }
        }

        var content = ProjectInitializer.GenerateClaudeMd(info);
        File.WriteAllText(target, content);

        var frameworks = info.Frameworks.Count > 0 ? string.Join(", ", info.Frameworks) : "无";
        var buildTools = info.BuildTools.Count > 0 ? string.Join(", ", info.BuildTools) : "无";
        screen.AddSystemMsg(
            $"✅ 已生成 CLAUDE.md\n" +
            $"- 项目: {Path.GetFileName(info.ProjectRoot.TrimEnd('/', '\\'))}\n" +
            $"- 语言: {info.PrimaryLanguage}\n" +
            $"- 框架: {frameworks}\n" +
            $"- 构建: {buildTools}\n" +
            $"下次启动时自动注入此文件，也可现在打开查看补充架构与注意事项。");

        return Task.CompletedTask;
    }
}
