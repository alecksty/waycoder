using System.Collections.Concurrent;
using System.Text;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;

namespace WayCoder.UI.Web;

/// <summary>
/// 浏览器聊天桥接层：把 <see cref="Agent.ChatAsync"/> 的流式回调（onToken/onTool/onToolOutput）
/// 转为 SSE 事件广播给浏览器，接收浏览器 POST 的输入入队，支持中断。
/// 对标 DeepSeek Harness Web UI：多槽位（F1-F10）、换模型、输 key、设置、黑白主题。
/// </summary>
public sealed partial class WebChatServer : UxHelper.IWebInteraction
{

    // ═══════════════════════════════════════════════════════════
    //  Web 斜杠命令分发（纯逻辑，便于自测）
    //  覆盖 Web 有意义的命令子集；未识别返回 (false, "")，由调用方回退为普通消息。
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 分发 Web 斜杠命令。返回 (是否已处理, 输出 Markdown 文本)。
    /// /interrupt、/stop 的实际中断副作用由路由层执行（需访问实例 _roundCts）。
    /// </summary>
    public static (bool Handled, string Output) HandleCommand(string input, Agent? agent)
    {
        var text = input.Trim();
        if (!text.StartsWith('/')) return (false, "");

        var space = text.IndexOf(' ');
        var cmd = (space < 0 ? text : text[..space]).ToLowerInvariant();
        var args = space < 0 ? "" : text[(space + 1)..].Trim();

        switch (cmd)
        {
            case "/help" or "/h":
                return (true, WebHelpText());

            case "/perm" or "/permissions":
                return (true, WebPermText(args));

            case "/model":
                if (args.Equals("list", StringComparison.OrdinalIgnoreCase)
                    || args.Equals("ls", StringComparison.OrdinalIgnoreCase))
                    return (true, WebModelListText());
                return (false, ""); // /model 无参 → 前端打开模型选择窗口

            case "/reset" or "/clear":
                if (agent != null) agent.Messages.Clear();
                return (true, "🗑 已清空当前会话");

            case "/session":
                return (true, WebSessionText(args, agent));

            case "/tokens":
                return (true, WebTokensText(agent));

            case "/mcp":
                return (true, WebMcpText());

            case "/todo":
                return (true, WebTodoText());

            case "/interrupt" or "/stop":
                return (true, "⏹ 已请求中断");

            default:
                return (false, "");
        }
    }

    private static string WebHelpText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("📋 **Web 命令**");
        sb.AppendLine();
        sb.AppendLine("| 命令 | 说明 |");
        sb.AppendLine("|---|---|");
        sb.AppendLine("| /help | 显示帮助 |");
        sb.AppendLine("| /perm [ask\\|auto\\|smartauto\\|yolo] | 切换权限模式 |");
        sb.AppendLine("| /model | 打开模型选择窗口 |");
        sb.AppendLine("| /model list | 列出模型 |");
        sb.AppendLine("| /theme | 切换明暗主题 |");
        sb.AppendLine("| /settings | 打开设置 |");
        sb.AppendLine("| /reset | 清空当前会话 |");
        sb.AppendLine("| /session [list\\|save\\|load <id>] | 会话管理 |");
        sb.AppendLine("| /tokens | Token 统计 |");
        sb.AppendLine("| /mcp | MCP 服务器状态 |");
        sb.AppendLine("| /todo | 任务列表 |");
        sb.AppendLine("| /interrupt | 中断当前任务 |");
        return sb.ToString();
    }

    private static string WebPermLabel()
        => PermissionManager.CurrentMode switch
        {
            PermissionManager.Mode.Yolo => "YOLO（直接执行）",
            PermissionManager.Mode.SmartAuto => "SmartAuto（智能分级）",
            PermissionManager.Mode.Auto => "Auto（首次确认后自动）",
            _ => "Ask（每次确认）",
        };

    private static string WebPermText(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return $"当前权限模式: **{WebPermLabel()}**";
        PermissionManager.SetMode(args);
        return $"权限模式已切换: **{WebPermLabel()}**";
    }

    private static string WebFormatContext(int ctx)
        => ctx >= 1024 ? $"{Math.Round(ctx / 1024.0)}k" : ctx.ToString();

    private static string WebModelListText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("🧠 **模型列表**");
        sb.AppendLine();
        sb.AppendLine("| 模型 | 供应商 | 上下文 |");
        sb.AppendLine("|---|---|---|");
        foreach (var m in ModelCatalog.All)
            sb.AppendLine($"| {m.DisplayName} | {m.ProviderId} | {WebFormatContext(m.ContextWindow)} |");
        return sb.ToString();
    }

    private static string WebSessionText(string args, Agent? agent)
    {
        var parts = args.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var sub = parts.Length > 0 ? parts[0].ToLowerInvariant() : "list";
        var rest = parts.Length > 1 ? parts[1].Trim() : "";

        switch (sub)
        {
            case "save":
                if (agent == null) return "⚠ 无活跃槽位";
                var id = SessionManager.SaveSession(agent.Messages, agent.LlmClient.Model);
                return $"💾 会话已保存: **{id}**";

            case "load":
                if (string.IsNullOrWhiteSpace(rest)) return "用法: /session load <会话ID>";
                var loaded = SessionManager.LoadSession(rest);
                if (loaded == null) return $"❌ 会话不存在: {rest}";
                if (agent == null) return "⚠ 无活跃槽位";
                agent.Messages.Clear();
                agent.Messages.AddRange(loaded.Value.Messages);
                return $"📂 已加载会话: **{rest}**（{loaded.Value.Messages.Count} 条消息）";

            case "list":
            default:
                var sessions = SessionManager.ListSessions(20);
                if (sessions.Count == 0) return "📂 没有已保存的会话";
                var sb = new StringBuilder();
                sb.AppendLine($"📂 **已保存的会话**（{sessions.Count} 条）");
                sb.AppendLine();
                foreach (var s in sessions)
                    sb.AppendLine($"- `{s.Id}` · {s.Model} · {s.SavedAt}");
                return sb.ToString();
        }
    }

    private static string WebTokensText(Agent? agent)
    {
        var llm = agent?.LlmClient;
        if (llm == null) return "⚠ 无活跃槽位";
        var sb = new StringBuilder();
        sb.AppendLine("💰 **Token 统计**");
        sb.AppendLine();
        sb.AppendLine($"- 本轮：prompt {llm.TaskPromptTokens} / completion {llm.TaskCompletionTokens}");
        sb.AppendLine($"- 累计：prompt {llm.TotalPromptTokens} / completion {llm.TotalCompletionTokens}");
        sb.AppendLine($"- 请求数：{llm.TotalRequests}");
        if (llm.LastTokensPerSec > 0) sb.AppendLine($"- 速率：{llm.LastTokensPerSec:F1} tok/s");
        if (llm.TaskCost.HasValue) sb.AppendLine($"- 本轮费用：${llm.TaskCost.Value:F4}");
        return sb.ToString();
    }

    private static string WebMcpText()
    {
        var servers = McpManager.Servers;
        if (servers.Count == 0) return "🔌 未配置 MCP 服务器";
        var sb = new StringBuilder();
        sb.AppendLine("🔌 **MCP 服务器**");
        sb.AppendLine();
        foreach (var s in servers)
        {
            var icon = s.Status == McpServerStatus.Connected ? "🟢"
                : s.Status == McpServerStatus.Connecting ? "🟡" : "🔴";
            sb.AppendLine($"- {icon} `{s.Name}`（{s.Transport}）· {s.ToolCount} 工具");
            if (!string.IsNullOrEmpty(s.Error)) sb.AppendLine($"  - ⚠ {s.Error}");
        }
        return sb.ToString();
    }

    private static string WebTodoText()
    {
        var items = TodoTool.Items;
        if (items.Count == 0) return "📋 无任务";
        var sb = new StringBuilder();
        sb.AppendLine("📋 **任务列表**");
        sb.AppendLine();
        foreach (var t in items)
            sb.AppendLine($"- `{t.Status}` {t.Title}");
        return sb.ToString();
    }
}
