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
        sb.AppendLine($"当前大模型：{cfg.Model}");
        sb.AppendLine($"当前小模型：{cfg.SmallModel}");
        if (!string.IsNullOrWhiteSpace(cfg.BaseUrl))
            sb.AppendLine($"BaseUrl：{cfg.BaseUrl}");
        sb.AppendLine("\n列出目录: --model list　选中: --model name <id>　存 key: --model key <供应商> <key>");
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
            ? ModelCatalog.BuiltIn
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
        if (info.DefaultBaseUrl != null)
            Config.Instance.BaseUrl = info.DefaultBaseUrl;
        Config.Instance.SaveToEnvFile();

        var keyHint = info.DefaultBaseUrl != null
            && !ApiKeyStore.Has(info.ProviderId)
            && info.ProviderId is not ("openai" or "local" or "custom")
            ? $"\n  该供应商需 API key：--model key {info.ProviderId} <key>"
            : "";

        return $"已选中 **{info.DisplayName}**（`{info.Id}`）并写入 .env" +
            (info.DefaultBaseUrl != null ? $"\n  BaseUrl 已自动设为 {info.DefaultBaseUrl}" : "") + keyHint;
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
}
