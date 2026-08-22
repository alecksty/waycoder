using System.Text;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// 连接方案管理 —— 把「服务商 + 大模型 + 小模型」打包成可整体切换的命名连接。
/// baseUrl 与 apiKey 与 providerId 唯一绑定（不单独存于连接里）。
///   /connect                   → 当前连接 + 概览
///   /connect list              → 列出全部连接（标注当前激活）
///   /connect use &lt;name&gt;     → 切换到某连接（provider+大模型+小模型一起切换）
///   /connect add &lt;name&gt; &lt;pid&gt; &lt;大模型&gt; &lt;小模型&gt;  → 新增连接
///   /connect remove &lt;name&gt;    → 删除连接
/// </summary>
public class ConnectionCommand : SlashCommand
{
    public override string Name => "/connect";
    public override string[] Aliases => ["/conn", "/connection"];
    public override string Description => "Connection management — switch provider + large/small model together";
    public override string? Usage => "/connect [list | use <name> | add <name> <pid> <largeModel> <smallModel> | remove <name>]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var trimmed = args.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            ShowCurrent(screen);
            return Task.CompletedTask;
        }

        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var first = parts[0].ToLowerInvariant();
        var rest = parts.Length > 1 ? trimmed[(parts[0].Length)..].Trim() : "";

        switch (first)
        {
            case "list":
            case "ls":
                ListConnections(screen);
                break;
            case "use":
            case "switch":
            case "activate":
            case "select":
                if (string.IsNullOrEmpty(rest))
                {
                    screen.AddSystemMsg("用法: /connect use <name>");
                    break;
                }
                UseConnection(screen, rest);
                break;
            case "add":
            case "new":
                AddConnection(screen, rest);
                break;
            case "remove":
            case "rm":
            case "delete":
            case "del":
                if (string.IsNullOrEmpty(rest))
                {
                    screen.AddSystemMsg("用法: /connect remove <name>");
                    break;
                }
                RemoveConnection(screen, rest);
                break;
            default:
                screen.AddSystemMsg("用法: /connect [list | use <name> | add <name> <pid> <largeModel> <smallModel> | remove <name>]");
                break;
        }

        return Task.CompletedTask;
    }

    static void ShowCurrent(ChatScreen screen)
    {
        var cfg = Config.Instance;
        var current = ConnectionConfig.CurrentByConfig();
        var active = ConnectionConfig.ActiveName;
        var sb = new StringBuilder();
        sb.AppendLine("**连接（Connection）**");
        if (current != null)
        {
            sb.AppendLine($"  当前连接：`{current.Name}`（{current.ProviderId} · 大={current.LargeModel} · 小={current.SmallModel}）");
        }
        else
        {
            sb.AppendLine($"  当前未匹配到已保存连接（服务商 `{cfg.Provider}` · 大=`{cfg.Model}` · 小=`{cfg.SmallModel}`）" +
                (string.IsNullOrEmpty(active) ? "" : $"（标记激活：`{active}`）"));
        }
        sb.AppendLine();
        sb.AppendLine("`/connect list` 列出全部　`/connect use <name>` 切换　`/connect add` 新增　`/connect remove <name>` 删除");
        screen.AddSystemMsg(sb.ToString());
    }

    static void ListConnections(ChatScreen screen)
    {
        var list = ConnectionConfig.List();
        var sb = new StringBuilder();
        if (list.Count == 0)
        {
            sb.AppendLine("尚未保存任何连接。");
            sb.AppendLine("用 `/connect add <name> <providerId> <大模型> <小模型>` 新增，如：");
            sb.AppendLine("  `/connect add deepseek deepseek deepseek-v4-pro deepseek-v4-flash`");
            screen.AddSystemMsg(sb.ToString());
            return;
        }

        sb.AppendLine($"**连接列表**（共 {list.Count} 个，baseUrl/apiKey 与 providerId 唯一绑定）：");
        foreach (var c in list)
        {
            var current = ConnectionConfig.CurrentByConfig()?.Name;
            var isCurrent = current != null && current.Equals(c.Name, StringComparison.OrdinalIgnoreCase);
            var isActive = ConnectionConfig.ActiveName.Equals(c.Name, StringComparison.OrdinalIgnoreCase);
            var baseUrl = ConnectionConfig.ResolveBaseUrl(c.ProviderId);
            var hasKey = ApiKeyStore.Has(c.ProviderId);
            var mark = isCurrent ? " ← 当前" : (isActive ? " ← 激活" : "");
            sb.AppendLine($"  `{c.Name,-20}` {c.ProviderId,-14} 大={c.LargeModel,-24} 小={c.SmallModel}" +
                (hasKey ? " 🔑" : "") + mark);
            sb.AppendLine($"    {("base=" + (baseUrl ?? "?"))}" + (hasKey ? " · key 已存" : " · ⚠ 未存 key"));
        }
        sb.AppendLine("\n`/connect use <name>` 切换　`/connect remove <name>` 删除");
        screen.AddSystemMsg(sb.ToString());
    }

    static void UseConnection(ChatScreen screen, string name)
    {
        var c = ConnectionConfig.Activate(name, out var message);
        if (c == null)
        {
            screen.AddSystemMsg(message);
            return;
        }

        // 运行时生效：重配当前 LLM（model + baseUrl + key + 上下文窗口）
        var cfg = Config.Instance;
        var key = ApiKeyStore.Get(c.ProviderId) ?? cfg.ApiKey;
        var baseUrl = cfg.BaseUrl;
        var agent = ProgramContext.Agent;
        if (agent != null)
        {
            agent.LlmClient.Reconfigure(key, baseUrl);
            agent.LlmClient.Model = c.LargeModel;
            agent.LlmClient.SmallModel = c.SmallModel;
            agent.UpdateContextWindow(ModelCatalog.ResolveContextWindow(c.LargeModel, cfg.MaxContextTokens));
        }
        if (ProgramContext.LLM != null)
        {
            ProgramContext.LLM.Model = c.LargeModel;
            ProgramContext.LLM.SmallModel = c.SmallModel;
        }

        screen.AddSystemMsg($"✅ {message}" +
            (string.IsNullOrEmpty(key) ? "\n  ⚠ 该服务商尚未存 key，请求可能失败（/provider apikey set <pid> <key> 保存）" : ""));
    }

    static void AddConnection(ChatScreen screen, string args)
    {
        var parts = args.Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            screen.AddSystemMsg("用法: /connect add <name> <providerId> <大模型> <小模型>\n例: `/connect add deepseek deepseek deepseek-v4-pro deepseek-v4-flash`");
            return;
        }
        if (ConnectionConfig.Add(parts[0], parts[1], parts[2], parts[3], out var error))
            screen.AddSystemMsg($"✅ 已新增连接「{parts[0]}」：{parts[1]} · 大={parts[2]} · 小={parts[3]}\n  用 `/connect use {parts[0]}` 切换");
        else
            screen.AddSystemMsg($"❌ {error}");
    }

    static void RemoveConnection(ChatScreen screen, string name)
    {
        if (ConnectionConfig.Remove(name, out var error))
            screen.AddSystemMsg($"🗑 已删除连接「{name}」");
        else
            screen.AddSystemMsg($"❌ {error}");
    }
}
