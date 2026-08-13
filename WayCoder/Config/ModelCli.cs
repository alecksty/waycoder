using System.Text;

namespace WayCoder;

/// <summary>
/// 模型管理核心逻辑 —— 供 /model 斜杠命令与 --model 命令行参数共用，
/// 返回纯文本，由调用方决定输出到屏幕（ChatScreen）还是控制台（Console）。
/// 覆盖：模型列表 / 选中（自动 base-url + 持久化）/ API key 管理。
/// </summary>
public static class ModelCli
{
    /// <summary>显示当前模型（大模型 / 小模型 / base-url）</summary>
    public static string Current()
    {
        var cfg = Config.Instance;
        var sb = new StringBuilder();
        sb.AppendLine($"当前大模型：{cfg.Model}（服务商 {cfg.Provider}）");
        sb.AppendLine($"当前小模型：{cfg.SmallModel}（服务商 {cfg.SmallProvider}）");
        if (!string.IsNullOrWhiteSpace(cfg.BaseUrl))
            sb.AppendLine($"BaseUrl：{cfg.BaseUrl}");
        sb.AppendLine("\n列出目录: --model list　选大模型: --model name <id>　选小模型: --model small <id>　存 key: --model key <供应商> <key>");
        return sb.ToString();
    }

    /// <summary>设置连接地址（base-url），写入 .env 持久化</summary>
    public static string Connect(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            return "用法: --model connect <base-url>";
        Config.Instance.BaseUrl = baseUrl.Trim();
        Config.Instance.SaveToEnvFile();
        return $"BaseUrl 已设为 {baseUrl.Trim()}（已写入 .env）";
    }

    /// <summary>列出模型目录（按供应商分组，当前模型标注），可传关键词过滤</summary>
    public static string List(string? filter = null)
    {
        var models = string.IsNullOrWhiteSpace(filter)
            ? ModelCatalog.All
            : ModelCatalog.Search(filter);

        if (models.Length == 0)
            return "未找到匹配的模型。用 --model list 查看全部。";

        var current = Config.Instance.Model;
        var sb = new StringBuilder();
        sb.AppendLine($"模型目录（共 {models.Length} 个）：");

        foreach (var g in models.GroupBy(m => m.Provider))
        {
            sb.AppendLine();
            sb.AppendLine($"【{g.Key}】");
            foreach (var m in g)
            {
                var price = m.InputPrice > 0 ? $"${m.InputPrice}/${m.OutputPrice}" : "?";
                var ctx = m.ContextWindow > 0
                    ? m.ContextWindow >= 1_000_000 ? $"{m.ContextWindow / 1_000_000}M" : $"{m.ContextWindow / 1000}K"
                    : "?";
                var mark = m.Id == current ? "  ← 当前" : "";
                sb.AppendLine($"  {m.Id,-28} {ctx,-5}ctx  {price,-13}/MTok  [{m.Category}]{mark}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("选中: --model name <id> 或 --model <id>");
        sb.AppendLine("存 key: --model key <供应商> <key>　查 key: --model key");
        return sb.ToString();
    }

    /// <summary>选中模型：按目录解析，自动设置 base-url，写入 .env 持久化</summary>
    public static string Select(string modelId)
    {
        var info = ModelCatalog.Find(modelId.Trim()) ?? ModelCatalog.Search(modelId.Trim()).FirstOrDefault();

        if (info == null)
        {
            Config.Instance.Model = modelId.Trim();
            Config.Instance.SaveToEnvFile();
            return $"已设置模型为 `{modelId}`（目录外模型，已写入 .env）。若非 OpenAI 兼容端点请另行 --config set BaseUrl <url>";
        }

        Config.Instance.Model = info.Id;
        Config.Instance.Provider = info.ProviderId;   // 同步当前服务商（key 跟服务商走）
        if (info.DefaultBaseUrl != null)
            Config.Instance.BaseUrl = info.DefaultBaseUrl;
        Config.Instance.SaveToEnvFile();

        var keyHint = info.DefaultBaseUrl != null
            && !ApiKeyStore.Has(info.ProviderId)
            && info.ProviderId is not ("openai" or "local" or "custom")
            ? $"\n  该供应商需 API key：--model key {info.ProviderId} <key>"
            : "";

        return $"已选中 **{info.DisplayName}**（`{info.Id}`，服务商 `{info.ProviderId}`）并写入 .env" +
            (info.DefaultBaseUrl != null ? $"\n  BaseUrl 已自动设为 {info.DefaultBaseUrl}" : "") + keyHint;
    }

    /// <summary>选中小模型：按目录解析，写入 .env 持久化（同步小模型服务商）</summary>
    public static string SelectSmall(string modelId)
    {
        var info = ModelCatalog.Find(modelId.Trim()) ?? ModelCatalog.Search(modelId.Trim()).FirstOrDefault();

        if (info == null)
        {
            Config.Instance.SmallModel = modelId.Trim();
            Config.Instance.SaveToEnvFile();
            return $"已设置小模型为 `{modelId}`（目录外模型，已写入 .env）";
        }

        Config.Instance.SmallModel = info.Id;
        Config.Instance.SmallProvider = info.ProviderId;   // 同步小模型服务商
        Config.Instance.SaveToEnvFile();

        return $"已选中小模型 **{info.DisplayName}**（`{info.Id}`，服务商 `{info.ProviderId}`）并写入 .env";
    }

    /// <summary>列出已保存的 API keys（打码）</summary>
    public static string ListKeys()
    {
        var keys = ApiKeyStore.ListAll();
        if (keys.Count == 0)
            return "未保存任何 API key。用 --model key <供应商> <key> 保存。";

        var sb = new StringBuilder();
        sb.AppendLine("已保存 API keys：");
        foreach (var (pid, _) in keys)
            sb.AppendLine($"  {pid,-12} = {ApiKeyStore.Masked(pid)}");
        return sb.ToString();
    }

    /// <summary>保存指定供应商的 API key</summary>
    public static string SetKey(string providerId, string key)
    {
        if (string.IsNullOrWhiteSpace(providerId) || string.IsNullOrWhiteSpace(key))
            return "用法: --model key <供应商> <key>";
        ApiKeyStore.Set(providerId, key);
        return $"已保存 {providerId} 的 API key：{ApiKeyStore.Masked(providerId)}";
    }

    /// <summary>
    /// 导入外部模型数据库（OpenCode / OpenClaw / Crush / Claude Code / Codex / 通用 JSON 文件），写入全局模型库。
    /// source: null/auto/all=自动探测全部；opencode/openclaw/crush/claude/codex=指定来源；否则视为文件路径。
    /// </summary>
    public static string Import(string? source = null)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var imported = new List<ModelCatalog.ModelInfo>();
        var reports = new List<string>();

        var s = source?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(s) || s is "auto" or "all")
        {
            foreach (var src in new[] { "opencode", "openclaw", "crush", "claude", "codex" })
                imported.AddRange(ImportOne(src, home, reports));
        }
        else if (s is "opencode" or "openclaw" or "crush" or "claude" or "claudecode" or "codex")
        {
            imported.AddRange(ImportOne(s, home, reports));
        }
        else
        {
            // 文件路径（支持 JSONC/JSON5 注释与裸 key；.toml 按 Codex 解析）
            if (!File.Exists(source))
                return $"❌ 未找到文件: {source}";
            var text = File.ReadAllText(source);
            var list = source.EndsWith(".toml", StringComparison.OrdinalIgnoreCase)
                ? ModelCatalog.ImportCodex(text)
                : ModelCatalog.ImportFromJson(ModelCatalog.NormalizeJson5(text));
            imported.AddRange(list);
            reports.Add($"文件 {source}");
        }

        if (imported.Count == 0)
            return "❌ 未导入任何模型（未找到可识别的模型配置）。\n   支持: --model import [opencode|openclaw|crush|claude|codex|<配置文件路径>]";

        // 去重：同一 Id（大小写不敏感）只保留第一个（OpenCode/OpenClaw 可能重复声明同一模型）
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        imported = imported.Where(m => seenIds.Add(m.Id)).ToList();

        // 去重：跳过内置目录已存在的模型（内置为精选元数据，避免被导入的空数据覆盖）
        var builtInIds = new HashSet<string>(ModelCatalog.BuiltIn.Select(m => m.Id), StringComparer.OrdinalIgnoreCase);
        var added = new List<ModelCatalog.ModelInfo>();
        var skipped = new List<string>();
        foreach (var m in imported)
        {
            if (builtInIds.Contains(m.Id))
                skipped.Add(m.Id);
            else
            {
                ModelCatalog.AddCustom(m);
                added.Add(m);
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine($"✅ 导入 {added.Count} 个模型到全局模型库（{string.Join("、", reports)}）" +
            (skipped.Count > 0 ? $"，跳过 {skipped.Count} 个内置已有" : "") + "：");
        foreach (var m in added)
            sb.AppendLine($"  {m.Id,-32} {m.Provider,-10} ctx={FormatCtx(m.ContextWindow),-6} ${m.InputPrice}/{m.OutputPrice}");
        sb.AppendLine("已写入: " + ModelCatalog.GlobalModelsPath);
        return sb.ToString().Trim();
    }

    private static List<ModelCatalog.ModelInfo> ImportOne(string source, string home, List<string> reports)
    {
        var src = source is "claudecode" ? "claude" : source;
        var (path, name) = src switch
        {
            "opencode" => (Path.Combine(home, ".config", "opencode", "opencode.json"), "OpenCode"),
            "openclaw" => (Path.Combine(home, ".openclaw", "openclaw.json"), "OpenClaw"),
            "crush" => (Path.Combine(home, ".config", "crush", "config.json"), "Crush"),
            "claude" => (Path.Combine(home, ".claude", "settings.json"), "Claude Code"),
            "codex" => (Path.Combine(home, ".codex", "config.toml"), "Codex"),
            _ => ("", ""),
        };

        if (!File.Exists(path))
        {
            // OpenCode 兼容 .jsonc
            if (src == "opencode")
            {
                var jsonc = Path.Combine(home, ".config", "opencode", "opencode.jsonc");
                if (File.Exists(jsonc)) path = jsonc;
                else return [];
            }
            else return [];
        }

        try
        {
            var text = File.ReadAllText(path);
            var list = src switch
            {
                "opencode" => ModelCatalog.ImportOpenCode(text),
                "openclaw" => ModelCatalog.ImportOpenClaw(text),
                "crush" => ModelCatalog.ImportCrush(text),
                "claude" => ModelCatalog.ImportClaude(text),
                "codex" => ModelCatalog.ImportCodex(text),
                _ => [],
            };
            if (list.Count > 0) reports.Add(name);
            return list;
        }
        catch { return []; }
    }

    /// <summary>删除自定义模型（从全局+本地模型库移除）</summary>
    public static string Remove(string modelId)
    {
        var removed = ModelCatalog.RemoveCustom(modelId.Trim());
        return removed.Length > 0
            ? $"已删除自定义模型 `{modelId}`（从 {string.Join("、", removed)} 移除）"
            : $"未找到自定义模型 `{modelId}`（内置模型不可删除，可被自定义覆盖）。";
    }

    private static string FormatCtx(int ctx) =>
        ctx <= 0 ? "?" : ctx >= 1_000_000 ? $"{ctx / 1_000_000}M" : $"{ctx / 1000}K";
}
