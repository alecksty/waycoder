using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// /repro —— 把当前会话的可执行操作导出为可复现 shell 脚本。
/// 从消息历史提取 bash 命令（按顺序）与写文件路径（作注释），落盘 .waycoder/repro_*.sh。
/// </summary>
public class ReproduceCommand : SlashCommand
{
    public override string Name => "/repro";
    public override string Description => L.Pick("导出会话为可复现脚本", "Export the session as a reproducible script");

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var agent = ProgramContext.Agent;
        if (agent == null) { screen.AddSystemMsg(L.Pick("Agent 未初始化", "Agent not initialized")); return Task.CompletedTask; }

        var commands = new List<string>();
        var writes = new List<string>();

        foreach (var msg in agent.SnapshotMessages())
        {
            if (msg["role"]?.AsString() != "assistant") continue;
            var toolCalls = msg["tool_calls"];
            if (toolCalls == null) continue;
            foreach (var tc in toolCalls.Items)
            {
                var name = tc["function"]?["name"]?.AsString() ?? "";
                var argsJson = tc["function"]?["arguments"]?.AsString() ?? "";
                if (string.IsNullOrEmpty(argsJson)) continue;
                JNode? argsNode = null;
                try { argsNode = Json.Parse(argsJson); } catch { }

                if (name == "bash")
                {
                    var cmd = argsNode?["command"]?.AsString();
                    if (!string.IsNullOrWhiteSpace(cmd)) commands.Add(cmd.Trim());
                }
                else if (name is "write_file" or "edit_file" or "multiedit" or "notebook_edit")
                {
                    var fp = argsNode?["file_path"]?.AsString();
                    if (!string.IsNullOrWhiteSpace(fp)) writes.Add(fp);
                }
            }
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("#!/usr/bin/env bash");
        sb.AppendLine(L.Pick("# WayCoder 可复现脚本 —— 由会话工具调用自动生成", "# WayCoder reproducible script — generated automatically from the session's tool calls"));
        sb.AppendLine(L.Pick($"# 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", $"# Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"));
        sb.AppendLine(L.Pick("# 注意：写文件操作以注释列出（内容请从会话 /export 还原）；命令按执行顺序排列。", "# Note: file writes are listed as comments (restore their contents with /export from the session); commands are in execution order."));
        sb.AppendLine("set -euo pipefail");
        sb.AppendLine();

        if (writes.Count > 0)
        {
            sb.AppendLine(L.Pick("# ── 涉及文件（写操作）──", "# ── Files involved (write operations) ──"));
            foreach (var w in writes.Distinct())
                sb.AppendLine($"#   {w}");
            sb.AppendLine();
        }

        if (commands.Count == 0)
        {
            sb.AppendLine(L.Pick("# （本会话未执行任何 bash 命令）", "# (this session ran no bash commands)"));
        }
        else
        {
            sb.AppendLine(L.Pick("# ── 命令序列 ──", "# ── Command sequence ──"));
            foreach (var c in commands)
            {
                sb.AppendLine();
                sb.AppendLine(c);
            }
        }

        var dir = Global.WriteConfigPath(Environment.CurrentDirectory);
        Directory.CreateDirectory(dir);
        var filename = $"repro_{DateTime.Now:yyyyMMdd_HHmmss}.sh";
        var path = Path.Combine(dir, filename);
        File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);

        screen.AddSystemMsg(
            L.Pick($"🧾 **可复现脚本已导出**\n\n", $"🧾 **Reproducible script exported**\n\n") +
            L.Pick($"  文件: `{filename}`（{commands.Count} 条命令 · {writes.Distinct().Count()} 个文件）\n", $"  File: `{filename}` ({commands.Count} commands · {writes.Distinct().Count()} files)\n") +
            L.Pick($"  位置: {dir}\n\n", $"  Location: {dir}\n\n") +
            L.Pick($"运行前请核对命令（含 `set -euo pipefail` 遇错即停），确认无害后：\n`bash {path}`", $"Review the commands before running them (`set -euo pipefail` stops at the first error), then:\n`bash {path}`"));
        return Task.CompletedTask;
    }
}
