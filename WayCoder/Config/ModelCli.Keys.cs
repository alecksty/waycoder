using System.Text;
using WayCoder.Infra;

namespace WayCoder;

public static partial class ModelCli
{
    public static string ListKeys()
    {
        var entries = ApiKeyStore.ListAllEntries();
        if (entries.Count == 0)
            return L.Pick("未保存任何 API key。用 --model key <供应商> <key> [有效期] 保存。",
                          "No API keys saved. Save one with --model key <provider> <key> [validity].");

        var sb = new StringBuilder();
        sb.AppendLine(L.Pick("已保存 API keys：", "Saved API keys:"));
        var expired = 0;
        var expiringSoon = 0;
        foreach (var (pid, entry) in entries)
        {
            var expiryText = ApiKeyStore.ExpiryText(entry.Expiry);
            if (ApiKeyStore.IsExpired(entry.Expiry)) expired++;
            else if (ApiKeyStore.DaysLeft(entry.Expiry) <= 7) expiringSoon++;
            sb.AppendLine(L.Pick($"  {pid,-12} = {ApiKeyStore.Masked(pid),-30}有效期: {expiryText}",
                                 $"  {pid,-12} = {ApiKeyStore.Masked(pid),-30}validity: {expiryText}"));
        }
        if (expired > 0) sb.AppendLine(L.Pick($"⚠ {expired} 个 key 已过期，请及时更换",
                                             $"⚠ {expired} key(s) have expired; replace them soon"));
        if (expiringSoon > 0) sb.AppendLine(L.Pick($"⚠ {expiringSoon} 个 key 临近到期（≤7 天）",
                                                  $"⚠ {expiringSoon} key(s) expiring soon (≤7 days)"));
        sb.AppendLine(L.Pick("设置/修改有效期：--model key expiry <供应商> <有效期>",
                             "Set/change validity: --model key expiry <provider> <validity>"));
        return sb.ToString();
    }

    /// <summary>保存指定供应商的 API key（可选有效期：永久 / 截止日期）。
    /// 合法性校验：只允许英文字母数字 + `+-_.` 逗号；环境变量引用（$VAR）拒绝。</summary>
    public static string SetKey(string providerId, string key, string? expiry = null)
    {
        if (string.IsNullOrWhiteSpace(providerId) || string.IsNullOrWhiteSpace(key))
            return L.Pick("用法: --model key <供应商> <key> [有效期]", "Usage: --model key <provider> <key> [validity]");
        if (ApiKeyStore.IsEnvVarRef(key))
            return L.Pick($"❌ {key} 是环境变量引用，不是真实 Key，已拒绝保存。请填真实 API Key。",
                          $"❌ {key} is an environment-variable reference, not a real key; refused. Enter a real API key.");
        if (!ApiKeyStore.IsValidApiKey(key))
            return L.Pick($"❌ Key 含非法字符（只允许英文字母数字 + - _ . ,）：{ApiKeyStore.Masked(key)}",
                          $"❌ The key contains invalid characters (only letters, digits and + - _ . , are allowed): {ApiKeyStore.Masked(key)}");
        ApiKeyStore.Set(providerId, key, expiry);
        return L.Pick($"已保存 {providerId} 的 API key：{ApiKeyStore.Masked(providerId)}（有效期: {ApiKeyStore.ExpiryText(expiry)}）",
                      $"Saved the API key for {providerId}: {ApiKeyStore.Masked(providerId)} (validity: {ApiKeyStore.ExpiryText(expiry)})");
    }

    /// <summary>给已存 key 设置/修改有效期（不改动 key 本身）。</summary>
    public static string SetKeyExpiry(string providerId, string? expiry)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            return L.Pick("用法: --model key expiry <供应商> <有效期>", "Usage: --model key expiry <provider> <validity>");
        if (!ApiKeyStore.Has(providerId))
            return L.Pick($"服务商 {providerId} 未保存 key，先用 --model key <供应商> <key> 保存",
                          $"No key saved for provider {providerId}; save one first with --model key <provider> <key>");
        ApiKeyStore.SetExpiry(providerId, expiry);
        return L.Pick($"已设置 {providerId} 的 API key 有效期：{ApiKeyStore.ExpiryText(expiry)}",
                      $"Set the API key validity for {providerId}: {ApiKeyStore.ExpiryText(expiry)}");
    }

    /// <summary>
    /// 导入外部模型数据库（OpenCode / OpenClaw / Crush / Claude Code / Codex / 通用 JSON 文件 / 内置目录），写入全局模型库。
    /// source: null/auto/all=自动探测全部；逗号分隔多来源（opencode,codex,claude）；单来源；builtin=恢复被清空的内置目录；否则视为文件路径。
    /// </summary>
}
