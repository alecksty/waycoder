using WayCoder.UI.TUI.Custom;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>
/// Model management — list, select, import, manage API keys.
/// Supports per-slot model selection and uniform mode for all 10 agent slots.
/// /model [name] — quick switch (backward compatible)
/// /model list|set|uniform|import|keys|slot
/// </summary>
public class ModelCommand : SlashCommand
{
    public override string Name => "/model";
    public override string[] Aliases => ["/m"];
    public override string Description => "Model management — per-slot model selection, catalog, import, keys";
    public override string? Usage => "/model [name] | #N <name> [key] | list|set <id>|uniform <id>|import <file>|keys|add [model|provider|key]|remove [model|provider|key]|test|slot <N> <large|small> <id>";

    /// <summary>把选中模型应用到当前 Agent 运行时（重配 LlmClient）。</summary>
    private static void ApplyRuntime(string modelId, string? providerId)
    {
        var agent = ProgramContext.Agent;
        if (agent == null) return;
        var cfg = Config.Instance;
        var info = ModelCatalog.Find(modelId);
        var pid = info?.ProviderId ?? providerId ?? cfg.Provider;
        var key = ApiKeyStore.Get(pid) ?? cfg.ApiKey;
        var baseUrl = info?.DefaultBaseUrl ?? cfg.BaseUrl;
        agent.LlmClient.Reconfigure(key, baseUrl);
        agent.LlmClient.Model = modelId;
        agent.UpdateContextWindow(ModelCatalog.ResolveContextWindow(modelId, cfg.MaxContextTokens));
    }

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var trimmed = args.Trim();

        // Quick switch (backward compat): /model <modelName> or /model small <modelName>
        if (string.IsNullOrEmpty(trimmed))
        {
            // 无参 /model → 弹模型选择对话框（任何终端可用；输入 key 后应用）
            var pick = ModelPicker.Show();
            if (pick != null)
            {
                if (pick.NeedsApiKey && !string.IsNullOrEmpty(pick.ProviderId))
                {
                    var key = UxHelper.Secret($"🔑 输入 {pick.ProviderId} 的 API Key（输入不可见，Enter 确认）:");
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        screen.AddSystemMsg("❌ 未输入 API Key，已取消");
                        return Task.CompletedTask;
                    }
                    ApiKeyStore.Set(pick.ProviderId, key);
                }
                ModelPicker.Apply(pick.ModelId, pick.IsLarge, pick.TargetSlot);
                ApplyRuntime(pick.ModelId, pick.ProviderId);
                screen.AddSystemMsg($"✅ 已切换{(pick.IsLarge ? "大" : "小")}模型: {pick.ModelId}");
            }
            return Task.CompletedTask;
        }

        // /model #N <modelName> [apiKey] — set model for specific slot
        if (trimmed.StartsWith('#'))
        {
            var spaceIdx = trimmed.IndexOf(' ');
            if (spaceIdx > 1 && int.TryParse(trimmed[1..spaceIdx], out var slotN)
                && slotN >= 1 && slotN <= 10)
            {
                var restForSlot = trimmed[(spaceIdx + 1)..].Trim();
                SetModelForSlot(screen, slotN - 1, restForSlot);
                return Task.CompletedTask;
            }
        }

        if (trimmed.StartsWith("small ", StringComparison.OrdinalIgnoreCase) && !trimmed.Contains("  "))
        {
            QuickSwitchSmall(screen, trimmed[6..].Trim());
            return Task.CompletedTask;
        }

        // Check if first word is a sub-command
        var parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var first = parts[0].ToLowerInvariant();
        var rest = parts.Length > 1 ? parts[1] : "";

        switch (first)
        {
            case "list":
                ListModels(screen, rest);
                break;
            case "set":
                SetModel(screen, rest);
                break;
            case "uniform":
                SetUniform(screen, rest);
                break;
            case "import":
                ImportModels(screen, rest);
                break;
            case "keys":
                if (rest.StartsWith("set ", StringComparison.OrdinalIgnoreCase))
                    SetApiKey(screen, rest[4..].Trim());
                else
                    ListKeys(screen);
                break;
            case "slot":
                SetSlotModel(screen, rest);
                break;
            case "test":
                screen.AddSystemMsg(ModelCli.Test());
                break;
            case "prune":
            case "clean":
                screen.AddSystemMsg(ModelCli.Prune());
                break;
            case "add":
                AddModels(screen, rest);
                break;
            case "remove":
            case "rm":
            case "delete":
                RemoveModels(screen, rest);
                break;
            default:
                // Quick switch by model name
                QuickSwitchLarge(screen, trimmed);
                break;
        }

        return Task.CompletedTask;
    }

    static void ShowCurrent(ChatScreen screen)
    {
        var slotIdx = screen.ActiveSlotIndex;
        var slotCfg = AgentSlotConfig.Get(slotIdx);
        var mode = AgentSlotConfig.UniformMode ? "UNIFORM" : $"F{slotIdx + 1}";
        var large = AgentSlotConfig.ResolveLargeModel(slotCfg, slotIdx);
        var small = AgentSlotConfig.ResolveSmallModel(slotCfg, slotIdx);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Current model [{mode}]:");
        sb.AppendLine($"  Large: **{large}**");
        sb.AppendLine($"  Small: **{small}**");
        if (!slotCfg.UseGlobal)
        {
            if (slotCfg.ApiKeyProviderId != null)
                sb.AppendLine($"  Key: `{slotCfg.ApiKeyProviderId}` (saved)");
            if (slotCfg.BaseUrl != null)
                sb.AppendLine($"  URL: {slotCfg.BaseUrl}");
        }
        sb.AppendLine($"\nUse `/model list` to browse available models.");
        screen.AddSystemMsg(sb.ToString());
    }

    void QuickSwitchLarge(ChatScreen screen, string modelId)
    {
        var info = ModelCatalog.Find(modelId) ?? ModelCatalog.Search(modelId).FirstOrDefault();
        if (info == null)
        {
            // Fallback to direct model name switch
            ProgramContext.Config.Model = modelId;
            if (ProgramContext.LLM != null) ProgramContext.LLM.Model = modelId;
            screen.AddSystemMsg($"Model switched: {modelId}");
            return;
        }
        ApplyModel(screen, info, "large");
    }

    void QuickSwitchSmall(ChatScreen screen, string modelId)
    {
        var info = ModelCatalog.Find(modelId) ?? ModelCatalog.Search(modelId).FirstOrDefault();
        if (info == null)
        {
            if (ProgramContext.LLM != null) ProgramContext.LLM.SmallModel = modelId;
            screen.AddSystemMsg($"Small model switched: {modelId}");
            return;
        }
        ApplyModel(screen, info, "small");
    }

    static void ApplyModel(ChatScreen screen, ModelCatalog.ModelInfo info, string type)
    {
        var slotIdx = screen.ActiveSlotIndex;
        var slotCfg = AgentSlotConfig.Get(slotIdx);

        if (type == "large")
        {
            slotCfg.LargeModel = info.Id;
            if (ProgramContext.Agent != null)
            {
                ProgramContext.Agent.LlmClient.Model = info.Id;
                ProgramContext.Agent.UpdateContextWindow(
                    info.ContextWindow > 0 ? info.ContextWindow : Config.Instance.MaxContextTokens);
            }
        }
        else
        {
            slotCfg.SmallModel = info.Id;
            if (ProgramContext.LLM != null) ProgramContext.LLM.SmallModel = info.Id;
        }

        slotCfg.UseGlobal = false;
        if (info.DefaultBaseUrl != null)
            slotCfg.BaseUrl = info.DefaultBaseUrl;
        if (!string.IsNullOrWhiteSpace(info.ProviderId) && ApiKeyStore.Has(info.ProviderId))
            slotCfg.ApiKeyProviderId = info.ProviderId;

        AgentSlotConfig.Set(slotIdx, slotCfg);

        var keyHint = info.DefaultBaseUrl != null && !ApiKeyStore.Has(info.ProviderId) && info.ProviderId != "openai"
            ? $"\n  This provider needs an API key. Set it with: `/model keys set {info.ProviderId} <key>`"
            : "";

        screen.AddSystemMsg($"F{slotIdx + 1} {type} model -> **{info.DisplayName}** (`{info.Id}`){keyHint}");
    }

    static void ListModels(ChatScreen screen, string filter)
    {
        var models = string.IsNullOrWhiteSpace(filter)
            ? ModelCatalog.All
            : ModelCatalog.Search(filter);

        if (models.Length == 0)
        {
            screen.AddSystemMsg("No models found.");
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Model Catalog ({models.Length} models):");

        var grouped = models.GroupBy(m => m.Provider);
        foreach (var group in grouped)
        {
            sb.AppendLine($"\n**{group.Key}**");
            foreach (var m in group)
            {
                var price = m.InputPrice > 0 ? $"${m.InputPrice}/${m.OutputPrice}" : "?";
                var ctx = m.ContextWindow > 0
                    ? m.ContextWindow >= 1_000_000 ? $"{m.ContextWindow / 1_000_000}M" : $"{m.ContextWindow / 1000}K"
                    : "?";
                var cat = m.Category.Length > 4 ? m.Category[..4] : m.Category;
                sb.AppendLine($"  `{m.Id}` — {ctx}ctx {price}/MTok [{cat}]");
            }
        }

        sb.AppendLine($"\nUse `/model set <id>` to set as large model for current slot.");
        sb.AppendLine($"Use `/model uniform <id>` to set for all 10 slots.");

        screen.AddSystemMsg(sb.ToString());
    }

    static void SetModel(ChatScreen screen, string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            screen.AddSystemMsg("Usage: /model set <modelId>");
            return;
        }

        var info = ModelCatalog.Find(modelId.Trim())
                ?? ModelCatalog.Search(modelId.Trim()).FirstOrDefault();

        if (info == null)
        {
            screen.AddSystemMsg($"Model '{modelId}' not found.");
            return;
        }

        ApplyModel(screen, info, "large");
    }

    static void SetUniform(ChatScreen screen, string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            screen.AddSystemMsg("Usage: /model uniform <modelId>  — sets ALL 10 slots to the same model.");
            return;
        }

        var info = ModelCatalog.Find(modelId.Trim())
                ?? ModelCatalog.Search(modelId.Trim()).FirstOrDefault();

        if (info == null)
        {
            screen.AddSystemMsg($"Model '{modelId}' not found.");
            return;
        }

        var template = new AgentSlotConfig.SlotConfig
        {
            LargeModel = info.Id,
            SmallModel = info.Id,
            UseGlobal = false,
        };
        if (info.DefaultBaseUrl != null) template.BaseUrl = info.DefaultBaseUrl;
        if (!string.IsNullOrWhiteSpace(info.ProviderId) && ApiKeyStore.Has(info.ProviderId))
            template.ApiKeyProviderId = info.ProviderId;

        AgentSlotConfig.SetUniform(template);

        if (ProgramContext.LLM != null)
        {
            ProgramContext.LLM.Model = info.Id;
            ProgramContext.LLM.SmallModel = info.Id;
        }
        ProgramContext.Agent?.UpdateContextWindow(
            info.ContextWindow > 0 ? info.ContextWindow : Config.Instance.MaxContextTokens);

        screen.AddSystemMsg($"All 10 slots (UNIFORM) -> **{info.DisplayName}** (`{info.Id}`)");
    }

    static void SetSlotModel(ChatScreen screen, string args)
    {
        var parts = args.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            screen.AddSystemMsg("Usage: /model slot <1-10> <large|small> <modelId>");
            return;
        }

        if (!int.TryParse(parts[0], out var slotN) || slotN < 1 || slotN > 10)
        {
            screen.AddSystemMsg("Slot number must be 1-10.");
            return;
        }

        var type = parts[1].ToLowerInvariant();
        if (type != "large" && type != "small")
        {
            screen.AddSystemMsg("Type must be 'large' or 'small'.");
            return;
        }

        var info = ModelCatalog.Find(parts[2]) ?? ModelCatalog.Search(parts[2]).FirstOrDefault();
        if (info == null)
        {
            screen.AddSystemMsg($"Model '{parts[2]}' not found.");
            return;
        }

        var slotCfg = AgentSlotConfig.Get(slotN - 1);
        if (type == "large") slotCfg.LargeModel = info.Id;
        else slotCfg.SmallModel = info.Id;
        slotCfg.UseGlobal = false;
        if (info.DefaultBaseUrl != null) slotCfg.BaseUrl = info.DefaultBaseUrl;
        AgentSlotConfig.Set(slotN - 1, slotCfg);

        screen.AddSystemMsg($"F{slotN} {type} model -> **{info.DisplayName}** (`{info.Id}`)");
    }

    static void ImportModels(ChatScreen screen, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            screen.AddSystemMsg("Usage: /model import <path>\nSupported: OpenCode, Crush, Cline, Continue, JSON array.");
            return;
        }

        try
        {
            if (!File.Exists(filePath))
            {
                screen.AddSystemMsg($"File not found: {filePath}");
                return;
            }

            var json = File.ReadAllText(filePath);
            var (models, format) = ModelCatalog.TryImport(json);

            if (models.Count == 0)
            {
                screen.AddSystemMsg("No models found or unsupported format.");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Imported {models.Count} models ({format}):");
            foreach (var m in models.Take(20))
                sb.AppendLine($"  `{m.Id}` — {m.Provider}");
            if (models.Count > 20)
                sb.AppendLine($"  ... and {models.Count - 20} more");

            screen.AddSystemMsg(sb.ToString());
        }
        catch (Exception ex)
        {
            screen.AddSystemMsg($"Import failed: {ex.Message}");
        }
    }

    static void ListKeys(ChatScreen screen)
    {
        var keys = ApiKeyStore.ListAll();
        if (keys.Count == 0)
        {
            screen.AddSystemMsg("No API keys saved.\nUse `/model keys set <providerId> <key>` to save one.");
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Saved API Keys:");
        foreach (var (pid, _) in keys)
            sb.AppendLine($"  `{pid}`: {ApiKeyStore.Masked(pid)}");
        sb.AppendLine("\nUse `/model keys set <providerId> <key>` to add/update.");
        screen.AddSystemMsg(sb.ToString());
    }

    /// <summary>
    /// /model #N <modelName> [apiKey] — convenient slot model assignment.
    /// Example: /model #1 deepseek-v4-pro sk-xxx
    /// </summary>
    static void SetModelForSlot(ChatScreen screen, int slotIdx, string args)
    {
        var parts = args.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            ShowCurrent(screen);
            return;
        }

        var modelName = parts[0];
        var apiKeyOrUrl = parts.Length > 1 ? parts[1] : null;

        // Detect local model: <localhost>:<port> -> use as BaseUrl, not API key
        bool isLocalUrl = apiKeyOrUrl != null &&
            (apiKeyOrUrl.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase) ||
             apiKeyOrUrl.StartsWith("127.0.0.1:", StringComparison.OrdinalIgnoreCase) ||
             apiKeyOrUrl.StartsWith("0.0.0.0:", StringComparison.OrdinalIgnoreCase) ||
             apiKeyOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
             apiKeyOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

        // Normalize localhost URL: add http:// prefix if missing, append /v1 for OpenAI-compatible
        string? baseUrl = null;
        if (isLocalUrl && apiKeyOrUrl != null)
        {
            baseUrl = apiKeyOrUrl;
            if (!baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                baseUrl = "http://" + baseUrl;
            if (!baseUrl.EndsWith("/v1") && !baseUrl.EndsWith("/v1/"))
                baseUrl = baseUrl.TrimEnd('/') + "/v1";
        }
        var apiKey = (!isLocalUrl && apiKeyOrUrl != null) ? apiKeyOrUrl : null;

        var info = ModelCatalog.Find(modelName)
                ?? ModelCatalog.Search(modelName).FirstOrDefault();

        if (info == null)
        {
            var slotCfg = AgentSlotConfig.Get(slotIdx);
            slotCfg.LargeModel = modelName;
            slotCfg.SmallModel = modelName;
            slotCfg.UseGlobal = false;
            if (baseUrl != null) slotCfg.BaseUrl = baseUrl;
            if (apiKey != null) slotCfg.ApiKey = apiKey;
            AgentSlotConfig.Set(slotIdx, slotCfg);

            if (screen.ActiveSlotIndex == slotIdx && ProgramContext.LLM != null)
            {
                ProgramContext.LLM.Model = modelName;
                ProgramContext.LLM.SmallModel = modelName;
            }

            var msg = $"F{slotIdx + 1} -> `{modelName}`";
            if (baseUrl != null) msg += $" (BaseUrl: {baseUrl})";
            if (apiKey != null) msg += " (API key set)";
            screen.AddSystemMsg(msg);
            return;
        }

        var cfg = AgentSlotConfig.Get(slotIdx);
        cfg.LargeModel = info.Id;
        cfg.SmallModel = info.Id;
        cfg.UseGlobal = false;

        // Local model: use localhost:port as BaseUrl
        if (isLocalUrl)
        {
            cfg.BaseUrl = baseUrl;
        }
        else if (info.DefaultBaseUrl != null)
        {
            cfg.BaseUrl = info.DefaultBaseUrl;
        }

        // API key: local models don't need one
        if (!isLocalUrl && apiKey != null)
        {
            ApiKeyStore.Set(info.ProviderId, apiKey);
            cfg.ApiKeyProviderId = info.ProviderId;
        }
        else if (!isLocalUrl && ApiKeyStore.Has(info.ProviderId))
        {
            cfg.ApiKeyProviderId = info.ProviderId;
        }

        AgentSlotConfig.Set(slotIdx, cfg);

        if (screen.ActiveSlotIndex == slotIdx && ProgramContext.LLM != null)
        {
            ProgramContext.LLM.Model = info.Id;
            ProgramContext.LLM.SmallModel = info.Id;
        }

        var result = $"F{slotIdx + 1} -> **{info.DisplayName}** (`{info.Id}`) [{info.Provider}]";
        if (isLocalUrl) result += $" local@{baseUrl}";
        if (apiKey != null) result += " — API key saved";
        screen.AddSystemMsg(result);
    }

    static void SetApiKey(ChatScreen screen, string args)
    {
        var parts = args.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            screen.AddSystemMsg("Usage: /model keys set <providerId> <api-key>");
            return;
        }

        ApiKeyStore.Set(parts[0], parts[1]);
        screen.AddSystemMsg($"API key saved for `{parts[0]}`: {ApiKeyStore.Masked(parts[0])}");
    }

    /// <summary>/model add [model <id> <pid> [baseUrl]|provider <pid> [baseUrl]|key <pid> <key>] — 手动添加模型/服务商/API key</summary>
    static void AddModels(ChatScreen screen, string args)
    {
        var parts = args.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            screen.AddSystemMsg("Usage: /model add [model <id> <providerId> [baseUrl] | provider <providerId> [baseUrl] | key <providerId> <key>]");
            return;
        }

        var sub = parts[0].ToLowerInvariant();
        switch (sub)
        {
            case "model":
                screen.AddSystemMsg(parts.Length >= 3
                    ? ModelCli.AddModel(parts[1], parts[2], parts.Length > 3 ? parts[3] : null)
                    : "Usage: /model add model <id> <providerId> [baseUrl]");
                break;
            case "provider":
            case "prov":
                screen.AddSystemMsg(parts.Length >= 2
                    ? ModelCli.AddProvider(parts[1], parts.Length > 2 ? parts[2] : null)
                    : "Usage: /model add provider <providerId> [baseUrl]");
                break;
            case "key":
            case "keys":
            case "apikey":
                screen.AddSystemMsg(parts.Length >= 3
                    ? ModelCli.SetKey(parts[1], parts[2])
                    : "Usage: /model add key <providerId> <key>");
                break;
            default:
                screen.AddSystemMsg(parts.Length >= 2
                    ? ModelCli.AddModel(parts[0], parts[1], parts.Length > 2 ? parts[2] : null)
                    : ModelCli.AddModel(parts[0], null, null));
                break;
        }
    }

    /// <summary>/model remove [model <id>|provider <pid>|key <pid>] — 删除模型/服务商/API key（无子命令时 <id> 视为删模型）</summary>
    static void RemoveModels(ChatScreen screen, string args)
    {
        var parts = args.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            screen.AddSystemMsg("Usage: /model remove [model <id> | provider <pid> | key <pid>]");
            return;
        }

        var sub = parts[0].ToLowerInvariant();
        if (parts.Length >= 2 && sub == "model")
            screen.AddSystemMsg(ModelCli.Remove(parts[1]));
        else if (parts.Length >= 2 && sub is "provider" or "prov")
            screen.AddSystemMsg(ModelCli.RemoveProvider(parts[1]));
        else if (parts.Length >= 2 && sub is "key" or "keys" or "apikey")
            screen.AddSystemMsg(ModelCli.RemoveKey(parts[1]));
        else
            screen.AddSystemMsg(ModelCli.Remove(parts[0]));
    }
}
