using System.Text;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// 连接层管理 —— connect 只管 connect（注册表 + 命名连接 + 回退链 + 切换）。
///   connect    = {providerId, modelId} 命名条目（大模型/小模型各是一个 connect）
///   connection = 大 connect 名 + 小 connect 名（切换连接时大小一起切换）
///   chain      = 全局回退链（一串 connect 名）
///   /connect                        → 当前连接 + 概览
///   /connect list                   → 列出 connect 注册表 + 命名连接
///   /connect add &lt;name&gt; &lt;pid&gt; &lt;model&gt;→ 新增 connect
///   /connect rm &lt;name&gt;             → 删除 connect
///   /connect select &lt;id&gt;           → 一键切换大 connect（connect名 | providerId.modelId | providerId/modelId | baseUrl:model | modelId）
///   /connect &lt;id&gt;                  → 同 select（快速切换）
///   /connect test                   → 连通性测试（各 connect 的 provider 端点）
///   /connect import                 → 自动注册当前大/小/回退模型为 connect
///   /connect use &lt;name&gt;            → 切换命名连接（大/小 connect 一起切换）
///   /connect conn list|add &lt;name&gt; &lt;大&gt; &lt;小&gt;|rm &lt;name&gt; → 命名连接（bundle）管理
///   /connect chain [&lt;c1&gt; &lt;c2&gt; ...]  → 查看/设置全局回退链（connect 名）
/// </summary>
public class ConnectionCommand : SlashCommand
{
    public override string Name => "/connect";
    public override string[] Aliases => ["/conn", "/connection"];
    public override string Description => "Connect management — connect registry + named connection + fallback chain + switch";
    public override string? Usage => "/connect [<id> | select <id> | list | add <name> <pid> <model> | rm <name> | test | import | use <connName> | conn add <name> <big> <small> | chain <c1> <c2> ...]";

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
                ListAll(screen);
                break;
            case "add":
            case "new":
                AddConnect(screen, rest);
                break;
            case "rm":
            case "remove":
            case "delete":
            case "del":
                if (string.IsNullOrEmpty(rest))
                {
                    screen.AddSystemMsg("用法: /connect rm <connectName>");
                    break;
                }
                RemoveConnect(screen, rest);
                break;
            case "select":
            case "switch":
            case "activate":
                if (string.IsNullOrEmpty(rest))
                {
                    screen.AddSystemMsg("用法: /connect select <connectId | providerId.modelId | providerId/modelId | baseUrl:model | modelId>");
                    break;
                }
                ApplySpec(screen, rest);
                break;
            case "use":
                if (string.IsNullOrEmpty(rest))
                {
                    screen.AddSystemMsg("用法: /connect use <connectionName>");
                    break;
                }
                UseConnection(screen, rest);
                break;
            case "conn":
            case "connection":
                ConnSub(screen, rest);
                break;
            case "chain":
            case "fallback":
                ChainSub(screen, rest);
                break;
            case "test":
                TestConnects(screen);
                break;
            case "import":
                ImportConnects(screen);
                break;
            default:
                // 一键切换：/connect <connectId | providerId.modelId | providerId/modelId | baseUrl:model | modelId>
                ApplySpec(screen, trimmed);
                break;
        }

        return Task.CompletedTask;
    }

    // ════════════════════════════════════════════════════════════
    // 概览 / 列表
    // ════════════════════════════════════════════════════════════

    static void ShowCurrent(ChatScreen screen)
    {
        var cfg = Config.Instance;
        var current = ConnectionConfig.CurrentByConfig();
        var active = ConnectionConfig.ActiveName;
        var sb = new StringBuilder();
        sb.AppendLine("**连接（Connect）**");
        if (current != null)
        {
            var big = ConnectionConfig.FindConnect(current.BigConnect);
            var small = ConnectionConfig.FindConnect(current.SmallConnect);
            sb.AppendLine($"  当前连接：`{current.Name}`");
            sb.AppendLine($"    大 = {ConnectionConfig.FormatModel(big?.ProviderId ?? "", big?.ModelId ?? "")}（connect `{current.BigConnect}`）");
            sb.AppendLine($"    小 = {ConnectionConfig.FormatModel(small?.ProviderId ?? "", small?.ModelId ?? "")}（connect `{current.SmallConnect}`）");
        }
        else
        {
            sb.AppendLine($"  当前未匹配到已保存连接（大={ConnectionConfig.FormatModel(cfg.Provider, cfg.Model)} · 小={ConnectionConfig.FormatModel(cfg.SmallProvider, cfg.SmallModel)}）" +
                (string.IsNullOrEmpty(active) ? "" : $"（标记激活：`{active}`）"));
        }
        var chain = ConnectionConfig.FallbackChain;
        sb.AppendLine($"  回退链：{(chain.Count > 0 ? string.Join(" → ", chain) : "（无）")}");
        sb.AppendLine();
        sb.AppendLine("`/connect list` 全部　`/connect <id>` 切换　`/connect add` 加 connect　`/connect conn add` 加连接　`/connect chain` 回退链");
        screen.AddSystemMsg(sb.ToString());
    }

    static void ListAll(ChatScreen screen)
    {
        var connects = ConnectionConfig.ListConnects();
        var connections = ConnectionConfig.ListConnections();
        var sb = new StringBuilder();

        if (connects.Count == 0)
        {
            sb.AppendLine("尚未保存任何 connect。用 `/connect add <name> <providerId> <modelId>` 新增，如：");
            sb.AppendLine("  `/connect add deepseek-pro deepseek deepseek-v4-pro`");
            screen.AddSystemMsg(sb.ToString());
            return;
        }

        sb.AppendLine($"**connect 注册表**（共 {connects.Count} 个，provider 的 base_url/key 由 providerId 唯一绑定）：");
        foreach (var c in connects.OrderBy(x => x.ProviderId, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var prov = ConnectionConfig.ResolveProvider(c.ProviderId);
            var hasKey = !string.IsNullOrEmpty(prov?.ApiKey);
            var currentConn = ConnectionConfig.CurrentByConfig();
            var isActive = currentConn != null
                && (currentConn.BigConnect.Equals(c.Name, StringComparison.OrdinalIgnoreCase)
                    || currentConn.SmallConnect.Equals(c.Name, StringComparison.OrdinalIgnoreCase));
            sb.AppendLine($"  `{c.Name,-24}` {c.ProviderId,-14} {c.ModelId}" + (hasKey ? " 🔑" : " ⚠无key") + (isActive ? " ← 使用中" : ""));
        }

        if (connections.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"**命名连接**（{connections.Count} 个，大/小一起切换）：");
            foreach (var c in connections)
            {
                var isActive = ConnectionConfig.ActiveName.Equals(c.Name, StringComparison.OrdinalIgnoreCase);
                sb.AppendLine($"  `{c.Name,-20}` 大=`{c.BigConnect}` 小=`{c.SmallConnect}`{(isActive ? " ← 激活" : "")}");
            }
        }

        var chain = ConnectionConfig.FallbackChain;
        sb.AppendLine();
        sb.AppendLine($"回退链：{(chain.Count > 0 ? string.Join(" → ", chain) : "（无，/connect chain <c1> <c2> ... 设置）")}");
        sb.AppendLine("\n`/connect <id>` 切换　`/connect add <name> <pid> <model>` 加 connect　`/connect conn add <name> <大> <小>` 加连接　`/connect rm <name>` 删 connect");
        screen.AddSystemMsg(sb.ToString());
    }

    // ════════════════════════════════════════════════════════════
    // connect 注册表（增删）
    // ════════════════════════════════════════════════════════════

    static void AddConnect(ChatScreen screen, string args)
    {
        var p = args.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (p.Length < 3)
        {
            screen.AddSystemMsg("用法: /connect add <name> <providerId> <modelId>\n例: `/connect add deepseek-pro deepseek deepseek-v4-pro`");
            return;
        }
        if (ConnectionConfig.AddConnect(p[0], p[1], p[2], out var error))
            screen.AddSystemMsg($"✅ 已新增 connect「{p[0]}」：{p[1]} / {p[2]}\n  用 `/connect {p[0]}` 切换");
        else
            screen.AddSystemMsg($"❌ {error}");
    }

    static void RemoveConnect(ChatScreen screen, string name)
    {
        if (ConnectionConfig.RemoveConnect(name, out var error))
            screen.AddSystemMsg($"🗑 已删除 connect「{name}」");
        else
            screen.AddSystemMsg($"❌ {error}");
    }

    // ════════════════════════════════════════════════════════════
    // 一键切换（select / <id>）
    // ════════════════════════════════════════════════════════════

    static void ApplySpec(ChatScreen screen, string spec)
    {
        ConnectionConfig.ApplySpec(spec, isLarge: true, out var msg);
        var cfg = Config.Instance;
        var prov = ConnectionConfig.ResolveProvider(cfg.Provider);
        var key = prov?.ApiKey ?? cfg.ApiKey;
        var baseUrl = cfg.BaseUrl;
        var agent = ProgramContext.Agent;
        if (agent != null)
        {
            agent.LlmClient.Reconfigure(key, baseUrl);
            agent.LlmClient.Model = cfg.Model;
            agent.LlmClient.SmallModel = cfg.SmallModel;
            agent.UpdateContextWindow(ModelCatalog.ResolveContextWindow(cfg.Model, cfg.MaxContextTokens));
        }
        screen.AddSystemMsg($"✅ {msg}" +
            (string.IsNullOrEmpty(key) ? "\n  ⚠ 该服务商尚未存 key，请求可能失败（/provider apikey set <pid> <key> 保存）" : ""));
    }

    // ════════════════════════════════════════════════════════════
    // 命名连接（bundle：大+小一起切）
    // ════════════════════════════════════════════════════════════

    static void ConnSub(ChatScreen screen, string args)
    {
        var parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            ListConnections(screen);
            return;
        }
        var cmd = parts[0].ToLowerInvariant();
        var rest = parts.Length > 1 ? args[(parts[0].Length)..].Trim() : "";
        switch (cmd)
        {
            case "list":
            case "ls":
                ListConnections(screen);
                break;
            case "add":
            case "new":
            {
                var p = rest.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length < 3)
                {
                    screen.AddSystemMsg("用法: /connect conn add <name> <大connect> <小connect>\n例: `/connect conn add deepseek deepseek-pro deepseek-flash`");
                    break;
                }
                if (ConnectionConfig.AddConnection(p[0], p[1], p[2], out var error))
                    screen.AddSystemMsg($"✅ 已新增命名连接「{p[0]}」：大=`{p[1]}` 小=`{p[2]}`\n  用 `/connect use {p[0]}` 切换");
                else
                    screen.AddSystemMsg($"❌ {error}");
                break;
            }
            case "rm":
            case "remove":
            case "delete":
            case "del":
            {
                if (string.IsNullOrEmpty(rest))
                {
                    screen.AddSystemMsg("用法: /connect conn rm <connectionName>");
                    break;
                }
                if (ConnectionConfig.RemoveConnection(rest, out var error))
                    screen.AddSystemMsg($"🗑 已删除命名连接「{rest}」");
                else
                    screen.AddSystemMsg($"❌ {error}");
                break;
            }
            default:
                screen.AddSystemMsg("用法: /connect conn [list | add <name> <大connect> <小connect> | rm <name>]");
                break;
        }
    }

    static void ListConnections(ChatScreen screen)
    {
        var list = ConnectionConfig.ListConnections();
        var sb = new StringBuilder();
        if (list.Count == 0)
        {
            sb.AppendLine("尚未保存任何命名连接。先 `/connect add <name> <pid> <model>` 建 connect，再 `/connect conn add <name> <大> <小>` 组合。");
            screen.AddSystemMsg(sb.ToString());
            return;
        }
        sb.AppendLine($"**命名连接列表**（共 {list.Count} 个，大小可不同服务商）：");
        foreach (var c in list)
        {
            var big = ConnectionConfig.FindConnect(c.BigConnect);
            var small = ConnectionConfig.FindConnect(c.SmallConnect);
            var isActive = ConnectionConfig.ActiveName.Equals(c.Name, StringComparison.OrdinalIgnoreCase);
            sb.AppendLine($"  `{c.Name,-20}` 大=`{c.BigConnect}`（{big?.ModelId} · {big?.ProviderId}） 小=`{c.SmallConnect}`（{small?.ModelId} · {small?.ProviderId}）" +
                (isActive ? " ← 激活" : ""));
        }
        sb.AppendLine("\n`/connect use <name>` 切换　`/connect conn rm <name>` 删除");
        screen.AddSystemMsg(sb.ToString());
    }

    static void UseConnection(ChatScreen screen, string name)
    {
        var c = ConnectionConfig.ActivateConnection(name, out var message);
        if (c == null)
        {
            screen.AddSystemMsg(message);
            return;
        }

        // 运行时生效：重配当前 LLM（model + baseUrl + key + 上下文窗口）
        var cfg = Config.Instance;
        var big = ConnectionConfig.FindConnect(c.BigConnect);
        var key = big != null ? ApiKeyStore.Get(big.ProviderId) ?? cfg.ApiKey : cfg.ApiKey;
        var baseUrl = cfg.BaseUrl;
        var agent = ProgramContext.Agent;
        if (agent != null)
        {
            agent.LlmClient.Reconfigure(key, baseUrl);
            agent.LlmClient.Model = cfg.Model;
            agent.LlmClient.SmallModel = cfg.SmallModel;
            agent.UpdateContextWindow(ModelCatalog.ResolveContextWindow(cfg.Model, cfg.MaxContextTokens));
        }
        if (ProgramContext.LLM != null)
        {
            ProgramContext.LLM.Model = cfg.Model;
            ProgramContext.LLM.SmallModel = cfg.SmallModel;
        }

        screen.AddSystemMsg($"✅ {message}" +
            (string.IsNullOrEmpty(key) ? "\n  ⚠ 该服务商尚未存 key，请求可能失败（/provider apikey set <pid> <key> 保存）" : ""));
    }

    // ════════════════════════════════════════════════════════════
    // 回退链
    // ════════════════════════════════════════════════════════════

    static void ChainSub(ChatScreen screen, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            var list = ConnectionConfig.FallbackChain;
            var cfg = Config.Instance;
            var state = cfg.FallbackEnabled ? "«green»开«/»" : "«red»关（默认）«/»";
            screen.AddSystemMsg(
                $"**回退链**：开关 {state}（`/connect chain on|off`）\n" +
                (list.Count > 0
                    ? "当前链（" + list.Count + " 个 connect）：\n  " + string.Join(" → ", list) +
                      "\n\n`/connect chain <c1> <c2> ...` 重设　`/connect chain clear` 清空　`/connect chain on` 开启"
                    : "当前无回退链。`/connect chain <c1> <c2> ...` 设置（connect 名）"));
            return;
        }
        var first = args.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();
        if (first is "on" or "off" or "enable" or "disable")
        {
            var on = first is "on" or "enable";
            var cfg = Config.Instance;
            cfg.FallbackEnabled = on;
            cfg.SaveToConfigJson();
            cfg.SaveToEnvFile();
            screen.AddSystemMsg(on
                ? "✅ 已开启回退链：模型失败时按链自动切换备选 connect"
                : "✅ 已关闭回退链（默认）：只用当前模型，失败即停");
            return;
        }
        var names = args.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (names.Length == 1 && names[0].Equals("clear", StringComparison.OrdinalIgnoreCase))
            names = [];
        ConnectionConfig.SetFallbackChain(names);
        var after = ConnectionConfig.FallbackChain;
        screen.AddSystemMsg(after.Count > 0
            ? $"✅ 已设置回退链（{after.Count} 个 connect）：\n  " + string.Join(" → ", after) + "\n  用 `/connect chain on` 开启（默认关）"
            : "✅ 已清空回退链");
    }

    // ════════════════════════════════════════════════════════════
    // 连通性测试 / 导入
    // ════════════════════════════════════════════════════════════

    static void TestConnects(ChatScreen screen)
    {
        var providers = ConnectionConfig.ListConnects()
            .Select(c => c.ProviderId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var sb = new StringBuilder();
        if (providers.Count == 0)
        {
            sb.AppendLine("尚无 connect。用 `/connect add <name> <pid> <model>` 添加。");
            screen.AddSystemMsg(sb.ToString());
            return;
        }
        sb.AppendLine($"**connect 连通性测试**（{providers.Count} 个服务商）：");
        int ok = 0;
        foreach (var pid in providers)
        {
            var prov = ConnectionConfig.ResolveProvider(pid);
            var baseUrl = prov?.BaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                sb.AppendLine($"  ❌ `{pid}` — 无端点");
                continue;
            }
            var (o, d) = ModelCli.ProbeEndpoint(baseUrl, prov?.ApiKey);
            if (o) ok++;
            sb.AppendLine($"  {(o ? "✅" : "❌")} `{pid}` {baseUrl} — {d}" + (string.IsNullOrEmpty(prov?.ApiKey) ? "（未存 key）" : ""));
        }
        sb.AppendLine($"\n**结论：{ok} / {providers.Count} 可连接**");
        screen.AddSystemMsg(sb.ToString());
    }

    static void ImportConnects(ChatScreen screen)
    {
        var cfg = Config.Instance;
        var sb = new StringBuilder();
        // 确保当前大/小模型 + 回退链都登记为 connect（幂等）
        var big = ConnectionConfig.FindOrCreateConnect(cfg.Provider, cfg.Model);
        var small = ConnectionConfig.FindOrCreateConnect(cfg.SmallProvider, cfg.SmallModel);
        var imported = new List<string> { big.Name, small.Name };
        foreach (var cn in ConnectionConfig.FallbackChain)
        {
            if (ConnectionConfig.FindConnect(cn) != null) continue;
            var info = ModelCatalog.Find(cn);
            if (info != null)
            {
                var c = ConnectionConfig.FindOrCreateConnect(info.ProviderId, info.Id);
                if (!imported.Contains(c.Name)) imported.Add(c.Name);
            }
            else if (!imported.Contains(cn))
            {
                var c = ConnectionConfig.FindOrCreateConnect(cfg.Provider, cn);
                imported.Add(c.Name);
            }
        }
        sb.AppendLine("✅ 已确保以下 connect 存在（幂等）：");
        foreach (var name in imported.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var c = ConnectionConfig.FindConnect(name);
            if (c != null)
                sb.AppendLine($"  `{c.Name}` = {ConnectionConfig.FormatModel(c.ProviderId, c.ModelId)}");
        }
        sb.AppendLine("\n`/connect conn add <name> <大> <小>` 组合成命名连接　`/connect chain <c1> <c2> ...` 设回退链");
        screen.AddSystemMsg(sb.ToString());
    }
}
