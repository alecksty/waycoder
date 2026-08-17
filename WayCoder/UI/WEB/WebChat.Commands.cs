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
    /// /interrupt、/stop 的实际中断副作用由路由层执行（需访问实例 _slots[slot].Cts）。
    /// </summary>
    public static (bool Handled, string Output) HandleCommand(string input, Agent? agent, int slot = -1)
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
                if (agent != null) agent.ClearMessages();
                return (true, "🗑 已清空当前会话");

            case "/session":
                return (true, WebSessionText(args, agent, slot));

            case "/tokens":
                return (true, WebTokensText(agent));

            case "/mcp":
                return (true, WebMcpText());

            case "/todo":
                return (true, WebTodoText());

            case "/stats":
                return (true, WebStatsText(agent));

            case "/recent" or "/diff":
                return (true, WebRecentText());

            case "/interrupt" or "/stop":
                return (true, "⏹ 已请求中断");

            case "/test":
                return (true, WebTestText(args));

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
        sb.AppendLine("| /model <名称> | 按名称切换模型 |");
        sb.AppendLine("| /theme | 切换明暗主题 |");
        sb.AppendLine("| /settings | 打开设置 |");
        sb.AppendLine("| /reset | 清空当前会话 |");
        sb.AppendLine("| /session [list\\|save\\|load <id>] | 会话管理 |");
        sb.AppendLine("| /tokens | Token 统计 |");
        sb.AppendLine("| /stats | 会话统计 |");
        sb.AppendLine("| /recent | 本次修改的文件 |");
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

    private static string WebSessionText(string args, Agent? agent, int slot)
    {
        var parts = args.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var sub = parts.Length > 0 ? parts[0].ToLowerInvariant() : "list";
        var rest = parts.Length > 1 ? parts[1].Trim() : "";

        switch (sub)
        {
            case "save":
                if (agent == null) return "⚠ 无活跃槽位";
                var id = SessionManager.SaveSession(agent.SnapshotMessages(), agent.LlmClient.Model, null, slot);
                return $"💾 会话已保存: **{id}**";

            case "load":
                if (string.IsNullOrWhiteSpace(rest)) return "用法: /session load <会话ID>";
                var loaded = SessionManager.LoadSession(rest, slot);
                if (loaded == null) return $"❌ 会话不存在: {rest}";
                if (agent == null) return "⚠ 无活跃槽位";
                agent.ReplaceMessages(loaded.Value.Messages);
                return $"📂 已加载会话: **{rest}**（{loaded.Value.Messages.Count} 条消息）";

            case "list":
            default:
                var sessions = SessionManager.ListSessions(20, 0, slot);
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

    private static string WebStatsText(Agent? agent)
    {
        var llm = agent?.LlmClient;
        var sb = new StringBuilder();
        sb.AppendLine("📊 **会话统计**");
        sb.AppendLine();
        sb.AppendLine($"- 模型：`{llm?.Model ?? "?"}`");
        sb.AppendLine($"- 请求数：{llm?.TotalRequests ?? 0}");
        sb.AppendLine($"- 累计 token：prompt {llm?.TotalPromptTokens ?? 0} / completion {llm?.TotalCompletionTokens ?? 0}");
        sb.AppendLine($"- 本轮 token：prompt {llm?.TaskPromptTokens ?? 0} / completion {llm?.TaskCompletionTokens ?? 0}");
        if (llm?.LastTokensPerSec > 0) sb.AppendLine($"- 速率：{llm.LastTokensPerSec:F1} tok/s");
        if (llm?.TaskCost.HasValue == true) sb.AppendLine($"- 本轮费用：${llm.TaskCost.Value:F4}");
        if (llm?.EstimatedCost.HasValue == true) sb.AppendLine($"- 累计估计：${llm.EstimatedCost.Value:F4}");
        return sb.ToString();
    }

    private static string WebRecentText()
    {
        var files = EditFileTool.ChangedFiles.ToList();
        if (files.Count == 0) return "📝 本次会话尚未修改文件";
        var sb = new StringBuilder();
        sb.AppendLine($"📝 **本次修改的文件**（{files.Count} 个）");
        sb.AppendLine();
        foreach (var f in files)
        {
            EditFileTool.ChangedFileStats.TryGetValue(f, out var st);
            sb.AppendLine($"- `{Path.GetFileName(f)}` +{st.Added} -{st.Deleted}");
        }
        return sb.ToString();
    }

    /// <summary>/test — 渲染测试内容（markdown 表格 / 代码高亮 / «» 中间格式 / Shell ANSI 配色等），供前端验证显示效果。</summary>
    private static string WebTestText(string args)
    {
        var sub = args.ToLowerInvariant().Trim();
        return sub switch
        {
            "list" or "ls" or "" => WebTestListText(),
            "markdown" or "md" or "all" => WebTestMarkdownText(),
            "table" or "表格" => WebTestTableText(),
            "markup" or "color" or "样式" or "中间" => WebTestMarkupText(),
            _ => $"❌ 未知测试项「{args}」。输入 /test list 查看全部测试项。",
        };
    }

    private static string WebTestListText()
    {
        return """
            📋 **可用测试项**

            | 命令 | 说明 |
            |---|---|
            | /test list | 本列表 |
            | /test markdown | Markdown 渲染（标题/列表/引用/代码块/表格） |
            | /test table | 表格专项（含转义竖线 \|） |
            | /test markup | «» 中间格式（颜色/粗体/斜体/下划线，跨平台渲染） |
            | /test ansi | Shell 裸 ANSI 配色（终端 tty 效果，前端本地渲染） |

            > 提示：/test markup 验证 WayCoder 中间格式经各平台渲染器的呈现；/test ansi 验证 Shell 命令产生的裸 ANSI 转 HTML。
            """;
    }

    /// <summary>
    /// /test markup — «» 中间格式样例。WayCoder 所有格式消息统一用 «tag»…«/» 表达颜色/文字特征，
    /// 由各平台渲染器决定呈现：CLI/TUI → ANSI（SpectreToAnsi）、Web → HTML（markupToHtml）、GUI → 富文本。
    /// 这里返回中间格式原文，前端 mdToHtml 的 inline 管线会调用 markupToHtml 渲染。
    /// </summary>
    private static string WebTestMarkupText()
    {
        return """
            # 中间格式（«» 标记）渲染测试

            WayCoder 所有格式消息（text/markdown/code/…）统一走 **中间格式**：
            内容用 `«tag»…«/»` 表达颜色与文字特征，由各平台渲染器决定呈现——CLI/TUI → ANSI、Web → HTML、GUI → 富文本。

            ## 颜色
            «red»红色«/» · «green»绿色«/» · «yellow»黄色«/» · «cyan»青色«/» · «blue»蓝色«/» · «magenta»紫色«/» · «grey»暗灰«/»

            ## 文字特征
            «bold»粗体«/» · «italic»斜体«/» · «underline»下划线«/» · «dim»暗淡«/»

            ## 复合标签
            «bold red»粗体红«/» · «bold green»粗体绿«/» · «bold yellow»粗体黄«/»

            ## 表格（单元格内联中间格式）
            | 项目 | 状态 |
            |---|---|
            | 编译 | «green»通过«/» |
            | 测试 | «yellow»警告«/» |
            | 部署 | «red»失败«/» |

            ## 代码块（原样显示，不解析 «» 标记）
            ```csharp
            Console.WriteLine("«red»hello«/»");  // 代码块内原样显示
            ```

            结束。
            """;
    }

    private static string WebTestTableText()
    {
        return """
            ## 表格专项测试

            ### 普通表格
            | 模型 | 供应商 | 上下文 |
            |---|---|---|
            | deepseek-v4-pro | deepseek | 128k |
            | claude-fable-5 | anthropic | 200k |
            | gpt-5.4 | openai | 200k |

            ### 含转义竖线的单元格
            | 命令 | 用法 |
            |---|---|
            | /perm | `/perm [ask\|auto\|smartauto\|yolo]` |
            | /session | `/session [list\|save\|load <id>]` |
            | /test | `/test [list\|markdown\|table]` |

            ### 对齐冒号
            | 左对齐 | 居中 | 右对齐 |
            |:---|---:|:---:|
            | a | b | c |
            """;
    }

    private static string WebTestMarkdownText()
    {
        return """
            # Markdown 渲染测试

            ## 标题层级
            ### 三级标题
            #### 四级标题

            ## 行内样式
            **粗体**、*斜体*、`行内代码`、[链接](https://example.com)

            ## 无序列表
            - 无序列表项 1
            - 无序列表项 2
            - 无序列表项 3

            ## 有序列表
            1. 有序列表项 1
            2. 有序列表项 2
            3. 有序列表项 3

            ## 引用
            > 这是一段引用文字。

            ## 代码块（C#）
            ```csharp
            public static void Main()
            {
                Console.WriteLine("Hello, WayCoder!");
            }
            ```

            ## 代码块（bash）
            ```bash
            dotnet build -c Release
            echo "done"
            ```

            ## 表格
            | 命令 | 说明 |
            |---|---|
            | /help | 显示帮助 |
            | /perm [ask\|auto\|smartauto\|yolo] | 切换权限模式 |
            | /test | 显示本测试 |

            ## 水平线
            ---

            结束。
            """;
    }
}
