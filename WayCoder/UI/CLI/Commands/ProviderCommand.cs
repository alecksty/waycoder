using System.Text;
using WayCoder.UI.TUI.Custom;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// 服务商管理 —— provider 只管 provider（{providerName, baseUrl, apikey}）。
///   /provider                        → 当前服务商 + 概览
///   /provider list                   → 列出全部服务商（含 base-url / key 状态）
///   /provider add &lt;id&gt; &lt;名称&gt; &lt;base-url&gt; → 新增/更新服务商
///   /provider rm &lt;id&gt;               → 删除服务商（含 key）
///   /provider select &lt;id&gt;           → 切换当前服务商（作用于当前大模型 connect）
///   /provider &lt;id&gt;                  → 同 select（快速切换）
///   /provider show &lt;id&gt;             → 查看该服务商下的模型列表
///   /provider apikey                 → 列出已保存的 API key（打码）
///   /provider apikey set &lt;pid&gt; &lt;key&gt;→ 保存/更新某服务商的 key
///   /provider apikey rm &lt;pid&gt;       → 删除某服务商的 key
///   /provider test                   → 连通性测试（探测各端点 /models）
///   /provider import [all|opencode|openclaw|crush|&lt;文件路径&gt;] → 导入外部模型库
/// </summary>
public class ProviderCommand : SlashCommand
{
    public override string Name => "/provider";
    public override string[] Aliases => ["/p", "/prov"];
    public override string Description => "Provider management — providers {name, baseUrl, apikey}: list/add/rm/select/test/import";
    public override string? Usage => "/provider [list | add <id> <name> <url> | rm <id> | select <id> | <id> | show <id> | apikey [set <pid> <key> | rm <pid>] | test | import [source]]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var trimmed = args.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            // TUI 打开供应商管理对话框（设Key/清Key/测试/添加/改名/改地址/删除）；
            // 非 TUI（Web/GUI）回退文本概览
            if (WayCoder.UI.TUI.Base.TuiManager.Instance?.ActiveScreen != null)
                ProviderPicker.Show();
            else
                ShowCurrent(screen);
            return Task.CompletedTask;
        }

        var parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var first = parts[0].ToLowerInvariant();
        var rest = parts.Length > 1 ? parts[1].Trim() : "";

        switch (first)
        {
            case "list":
            case "ls":
                ListProviders(screen);
                break;
            case "add":
            case "new":
                AddProvider(screen, rest);
                break;
            case "rm":
            case "remove":
            case "delete":
            case "del":
                if (string.IsNullOrEmpty(rest))
                {
                    screen.AddSystemMsg("用法: /provider rm <providerId>");
                    break;
                }
                RemoveProvider(screen, rest);
                break;
            case "select":
            case "switch":
            case "use":
                SelectProvider(screen, rest);
                break;
            case "show":
            case "models":
                ShowProviderModels(screen, rest);
                break;
            case "apikey":
            case "keys":
            case "key":
                ApiKeySub(screen, rest);
                break;
            case "test":
                screen.AddSystemMsg(ModelCli.Test());
                break;
            case "import":
                ImportProviders(screen, rest);
                break;
            default:
                // /provider <id> → 同 select：切换当前服务商
                SelectProvider(screen, trimmed);
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
        var sb = new StringBuilder();
        sb.AppendLine("**服务商（Provider）**");
        sb.AppendLine($"  大模型：{ConnectionConfig.FormatModel(cfg.Provider, cfg.Model)}");
        sb.AppendLine($"  小模型：{ConnectionConfig.FormatModel(cfg.SmallProvider, cfg.SmallModel)}");
        sb.AppendLine();
        sb.AppendLine("`/provider list` 全部　`/provider select <id>` 切换　`/provider apikey` 管 key　`/provider test` 测连通");
        screen.AddSystemMsg(sb.ToString());
    }

    static void ListProviders(ChatScreen screen)
    {
        var groups = ModelCatalog.All.GroupBy(m => m.ProviderId).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);
        var sb = new StringBuilder();
        sb.AppendLine($"**服务商列表**（共 {groups.Count()} 个，provider = name / base_url / key）：");

        foreach (var g in groups)
        {
            var pid = g.Key;
            var firstModel = g.First();
            ModelCatalog.Providers.TryGetValue(pid, out var prov);
            var baseUrl = firstModel.DefaultBaseUrl ?? prov?.DefaultBaseUrl;
            var hasKey = ApiKeyStore.Has(pid);
            var keyMark = hasKey ? "🔑" : "— ";   // 统一占 2 列显示宽度（🔑 2 列，— 补空格），避免有/无 key 行错位
            var current = pid.Equals(Config.Instance.Provider, StringComparison.OrdinalIgnoreCase) ? " ← 当前" : "";

            sb.AppendLine($"  {keyMark} `{pid,-14}` {firstModel.Provider,-12} {g.Count(),3} 模型  {(string.IsNullOrEmpty(baseUrl) ? "" : baseUrl)}{current}");
        }

        sb.AppendLine("\n`/provider select <id>` 切换　`/provider add <id> <名称> <url>` 新增　`/provider apikey set <pid> <key>` 存 key");
        screen.AddSystemMsg(sb.ToString());
    }

    // ════════════════════════════════════════════════════════════
    // 增删改
    // ════════════════════════════════════════════════════════════

    static void AddProvider(ChatScreen screen, string args)
    {
        var p = args.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (p.Length < 3)
        {
            screen.AddSystemMsg("用法: /provider add <id> <名称> <base-url>\n例: `/provider add deepseek DeepSeek https://api.deepseek.com/v1`");
            return;
        }
        ModelCatalog.RegisterProvider(p[0].Trim().ToLowerInvariant(), p[1], p[2]);
        screen.AddSystemMsg($"✅ 已添加/更新服务商 `{p[0].Trim().ToLowerInvariant()}` → {p[2]}");
    }

    static void RemoveProvider(ChatScreen screen, string pid)
    {
        var pid2 = pid.Trim().ToLowerInvariant();
        if (ModelCatalog.RemoveProvider(pid2))
            screen.AddSystemMsg($"🗑 已移除服务商 `{pid2}`（含 API key）");
        else
            screen.AddSystemMsg($"未找到服务商 `{pid2}`。用 /provider list 查看全部。");
    }

    static void SelectProvider(ChatScreen screen, string pid)
    {
        var pid2 = (pid ?? "").Trim().ToLowerInvariant();
        if (pid2.Length == 0)
        {
            screen.AddSystemMsg("用法: /provider select <providerId>");
            return;
        }
        if (!ModelCatalog.Providers.ContainsKey(pid2) && ModelCatalog.ByProvider(pid2).Length == 0)
        {
            screen.AddSystemMsg($"未找到服务商 `{pid2}`。用 /provider list 查看全部。");
            return;
        }
        // 切换 provider = 把当前大模型切到该服务商下（find-or-create connect）
        ConnectionConfig.ApplyModelChoice(pid2, Config.Instance.Model, isLarge: true, out var msg);
        var key = ApiKeyStore.Get(pid2) ?? Config.Instance.ApiKey;
        var agent = ProgramContext.Agent;
        if (agent != null)
        {
            agent.LlmClient.Reconfigure(key, Config.Instance.BaseUrl);
            agent.LlmClient.Model = Config.Instance.Model;
            agent.UpdateContextWindow(ModelCatalog.ResolveContextWindow(Config.Instance.Model, Config.Instance.MaxContextTokens));
        }
        screen.AddSystemMsg($"✅ {msg}" +
            (string.IsNullOrEmpty(key) ? "\n  ⚠ 该服务商尚未存 key，请求可能失败（/provider apikey set <pid> <key>）" : ""));
    }

    static void ShowProviderModels(ChatScreen screen, string pid)
    {
        var pid2 = (pid ?? "").Trim().ToLowerInvariant();
        if (pid2.Length == 0)
        {
            screen.AddSystemMsg("用法: /provider show <providerId>");
            return;
        }
        var models = ModelCatalog.ByProvider(pid2);
        if (models.Length == 0)
        {
            screen.AddSystemMsg($"未找到服务商 `{pid2}`。用 /provider list 查看全部。");
            return;
        }

        ModelCatalog.Providers.TryGetValue(models[0].ProviderId, out var prov);
        var hasKey = ApiKeyStore.Has(pid2);
        var sb = new StringBuilder();
        sb.AppendLine($"**{models[0].Provider}**（`{pid2}`）— {models.Length} 个模型 {(hasKey ? "🔑 已存 key" : "⚠ 未存 key")}");
        foreach (var m in models)
        {
            var ctx = m.ContextWindow > 0
                ? m.ContextWindow >= 1_000_000 ? $"{m.ContextWindow / 1_000_000}M" : $"{m.ContextWindow / 1000}K"
                : "?";
            var price = m.InputPrice > 0 ? $"${m.InputPrice}/{m.OutputPrice}" : "?";
            sb.AppendLine($"  `{m.Id,-28}` {ctx,-5}ctx {price,-13} [{m.Category}]");
        }
        sb.AppendLine($"\n选中: `/model select {models[0].Id}` 或 `/model small <id>`");
        screen.AddSystemMsg(sb.ToString());
    }

    // ════════════════════════════════════════════════════════════
    // API key（provider 域：apikey 属于 provider）
    // ════════════════════════════════════════════════════════════

    static void ApiKeySub(ChatScreen screen, string rest)
    {
        if (rest.StartsWith("set ", StringComparison.OrdinalIgnoreCase))
            SetApiKey(screen, rest[4..].Trim());
        else if (rest.StartsWith("rm ", StringComparison.OrdinalIgnoreCase)
            || rest.StartsWith("remove ", StringComparison.OrdinalIgnoreCase)
            || rest.StartsWith("del ", StringComparison.OrdinalIgnoreCase))
            RemoveApiKey(screen, rest.Split(' ', 2)[^1].Trim());
        else
            ListKeys(screen);
    }

    static void ListKeys(ChatScreen screen)
    {
        var keys = ApiKeyStore.ListAll();
        if (keys.Count == 0)
        {
            screen.AddSystemMsg("未保存任何 API key。\n用 `/provider apikey set <pid> <key>` 保存（一个服务商一个 key）。");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("**已保存 API keys**：");
        foreach (var (pid, _) in keys)
            sb.AppendLine($"  `{pid,-14}` = {ApiKeyStore.Masked(pid)}");
        sb.AppendLine("\n`/provider apikey set <pid> <key>` 新增/更新　`/provider apikey rm <pid>` 删除");
        screen.AddSystemMsg(sb.ToString());
    }

    static void SetApiKey(ChatScreen screen, string args)
    {
        var parts = args.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            screen.AddSystemMsg("用法: /provider apikey set <pid> <key>");
            return;
        }

        ApiKeyStore.Set(parts[0], parts[1]);
        screen.AddSystemMsg($"🔑 已保存 `{parts[0]}` 的 API key：{ApiKeyStore.Masked(parts[0])}");
    }

    static void RemoveApiKey(ChatScreen screen, string pid)
    {
        var pid2 = (pid ?? "").Trim().ToLowerInvariant();
        if (!ApiKeyStore.Has(pid2))
        {
            screen.AddSystemMsg($"未找到服务商 `{pid2}` 的 API key。");
            return;
        }
        ApiKeyStore.Remove(pid2);
        screen.AddSystemMsg($"🗑 已删除 `{pid2}` 的 API key");
    }

    static void ImportProviders(ChatScreen screen, string source)
    {
        // 复用 ModelCli.Import：source 为空→auto，all/opencode/openclaw/crush→指定来源，否则视为文件路径
        var result = string.IsNullOrWhiteSpace(source) || source.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? ModelCli.Import(null)
            : ModelCli.Import(source);
        screen.AddSystemMsg(result);
    }
}
