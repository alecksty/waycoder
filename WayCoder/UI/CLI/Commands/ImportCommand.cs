using WayCoder.Infra;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /import — 从其他编程智能体导入配置和数据。
///
/// 支持来源：Claude Code、OpenCode、Cursor、Cline
/// 用法：
///   /import             列出可导入内容
///   /import all         导入所有
///   /import models      仅模型/API 配置
///   /import mcp         仅 MCP 服务器
///   /import context     仅项目上下文
///   /import sessions    仅会话数据
/// </summary>
public class ImportCommand : SlashCommand
{
    public override string Name => "/import";
    public override string[] Aliases => ["/导入"];
    public override string Description => "从其他编程智能体导入配置（模型、MCP、上下文、会话）";
    public override string? Usage => "/import [all|models|mcp|context|sessions]";

    public override async Task ExecuteAsync(string args, ChatScreen screen)
    {
        var arg = args.Trim().ToLower();

        // /import → 列出可导入项
        if (string.IsNullOrEmpty(arg) || arg == "list")
        {
            var items = ImportHelper.Detect();
            if (items.Count == 0)
            {
                screen.AddMessage("未发现可导入的配置。\n\n" +
                    "支持从以下来源导入：\n" +
                    "- Claude Code (~/.claude/)\n" +
                    "- OpenCode (~/.config/opencode/)\n" +
                    "- Cursor (.cursor/)\n" +
                    "- Cline (~/.cline/)\n\n" +
                    "使用 **/import all** 导入全部，或指定分类：`models` / `mcp` / `context` / `sessions`", "system");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("## 发现可导入内容");
            sb.AppendLine();

            var byCategory = items.GroupBy(i => i.Category).ToList();
            foreach (var group in byCategory)
            {
                sb.Append($"**{group.Key}**: ");
                var parts = group.Select(i =>
                {
                    var icon = i.CanImport ? "✅" : "⏭";
                    return $"{icon} {i.Name}";
                });
                sb.AppendLine(string.Join(" · ", parts));
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine("使用 **/import all** 导入全部，或指定分类：`models` / `mcp` / `context` / `sessions`");

            screen.AddMessage(sb.ToString().Trim(), "system");
            return;
        }

        // /import all → 全部导入
        if (arg == "all")
        {
            screen.AddMessage("🔄 正在导入...", "system");
            var result = await ImportHelper.ImportAsync();
            screen.AddMessage(result, "system");
            return;
        }

        // /import <category>
        var validCategories = new HashSet<string> { "models", "mcp", "context", "sessions", "permissions" };
        if (validCategories.Contains(arg))
        {
            screen.AddMessage($"🔄 正在导入 {arg}...", "system");
            var result = await ImportHelper.ImportAsync(new HashSet<string> { arg });
            screen.AddMessage(result, "system");
            return;
        }

        screen.AddMessage($"未知导入分类: **{arg}**\n有效分类: {string.Join(", ", validCategories)}", "system");
    }
}
