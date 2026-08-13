using System.Text;
using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

/// <summary>
/// 服务商管理 —— 列出服务商、按服务商查看模型、管理 API key、导入外部模型库。
///   /provider                       → 当前服务商 + 概览
///   /provider list                  → 列出全部服务商（含模型数 / base-url / key 状态）
///   /provider &lt;pid&gt;           → 查看该服务商下的模型列表
///   /provider apikey                → 列出已保存的 API key（打码）
///   /provider apikey set &lt;pid&gt; &lt;key&gt; → 保存/更新某服务商的 key
///   /provider import [all|opencode|openclaw|crush|&lt;文件路径&gt;] → 导入外部模型库（无参=all 自动探测）
/// </summary>
public class ProviderCommand : SlashCommand
{
    public override string Name => "/provider";
    public override string[] Aliases => ["/p"];
    public override string Description => "Provider management — list providers, browse models by provider, manage API keys, import";
    public override string? Usage => "/provider [list | <pid> | apikey [set <pid> <key>] | import [all|opencode|openclaw|crush|claude|codex|<file>]]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var trimmed = args.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
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
            case "apikey":
            case "keys":
            case "key":
                if (rest.StartsWith("set ", StringComparison.OrdinalIgnoreCase))
                    SetApiKey(screen, rest[4..].Trim());
                else
                    ListKeys(screen);
                break;
            case "import":
                ImportProviders(screen, rest);
                break;
            default:
                // /provider <pid> → 该服务商下的模型列表
                ShowProviderModels(screen, trimmed);
                break;
        }

        return Task.CompletedTask;
    }

    static void ShowCurrent(ChatScreen screen)
    {
        var cfg = Config.Instance;
        var sb = new StringBuilder();
        sb.AppendLine("**服务商（Provider）**");
        sb.AppendLine($"  当前大模型服务商：`{cfg.Provider}`（模型 `{cfg.Model}`）");
        sb.AppendLine($"  当前小模型服务商：`{cfg.SmallProvider}`（模型 `{cfg.SmallModel}`）");
        sb.AppendLine();
        sb.AppendLine("`/provider list` 列出全部服务商　`/provider apikey` 管理 API key");
        screen.AddSystemMsg(sb.ToString());
    }

    static void ListProviders(ChatScreen screen)
    {
        var groups = ModelCatalog.All.GroupBy(m => m.ProviderId).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);
        var sb = new StringBuilder();
        sb.AppendLine($"**服务商列表**（共 {groups.Count()} 个）：");

        foreach (var g in groups)
        {
            var pid = g.Key;
            var firstModel = g.First();
            ModelCatalog.Providers.TryGetValue(pid, out var prov);
            var baseUrl = firstModel.DefaultBaseUrl ?? prov.DefaultBaseUrl;
            var hasKey = ApiKeyStore.Has(pid);
            var keyMark = hasKey ? "🔑" : "—";
            var current = pid.Equals(Config.Instance.Provider, StringComparison.OrdinalIgnoreCase) ? " ← 当前" : "";

            sb.AppendLine($"  {keyMark} `{pid,-14}` {firstModel.Provider,-12} {g.Count(),3} 模型  {(string.IsNullOrEmpty(baseUrl) ? "" : baseUrl)}{current}");
        }

        sb.AppendLine("\n`/provider <pid>` 查看该服务商模型　`/provider apikey set <pid> <key>` 存 key");
        screen.AddSystemMsg(sb.ToString());
    }

    static void ShowProviderModels(ChatScreen screen, string pid)
    {
        var models = ModelCatalog.ByProvider(pid);
        if (models.Length == 0)
        {
            screen.AddSystemMsg($"未找到服务商 `{pid}`。用 `/provider list` 查看全部。");
            return;
        }

        ModelCatalog.Providers.TryGetValue(models[0].ProviderId, out var prov);
        var hasKey = ApiKeyStore.Has(pid);
        var sb = new StringBuilder();
        sb.AppendLine($"**{models[0].Provider}**（`{pid}`）— {models.Length} 个模型 {(hasKey ? "🔑 已存 key" : "⚠ 未存 key")}");
        foreach (var m in models)
        {
            var ctx = m.ContextWindow > 0
                ? m.ContextWindow >= 1_000_000 ? $"{m.ContextWindow / 1_000_000}M" : $"{m.ContextWindow / 1000}K"
                : "?";
            var maxOut = m.MaxOutput > 0 ? $"{m.MaxOutput / 1000}K" : "?";
            var price = m.InputPrice > 0 ? $"${m.InputPrice}/{m.OutputPrice}" : "?";
            sb.AppendLine($"  `{m.Id,-28}` {ctx,-5}ctx 输出{maxOut,-5} {price,-13} [{m.Category}]");
        }
        sb.AppendLine($"\n选中: `/model name {models[0].Id}` 或 `/model small <id>`");
        screen.AddSystemMsg(sb.ToString());
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
        sb.AppendLine("\n`/provider apikey set <pid> <key>` 新增/更新");
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

    static void ImportProviders(ChatScreen screen, string source)
    {
        // 复用 ModelCli.Import：source 为空→auto，all/opencode/openclaw/crush→指定来源，否则视为文件路径
        var result = string.IsNullOrWhiteSpace(source) || source.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? ModelCli.Import(null)
            : ModelCli.Import(source);
        screen.AddSystemMsg(result);
    }
}
