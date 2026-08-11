using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

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
    public override string? Usage => "/model [name] | #N <name> [key] | list|set <id>|uniform <id>|import <file>|keys|slot <N> <large|small> <id>";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var trimmed = args.Trim();

        // Quick switch (backward compat): /model <modelName> or /model small <modelName>
        if (string.IsNullOrEmpty(trimmed))
        {
            ShowCurrent(screen);
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
            if (ProgramContext.Agent != null) ProgramContext.Agent.LlmClient.Model = info.Id;
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
            ? ModelCatalog.BuiltIn
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
}
