using System.Text;
using WayCoder.Infra;

namespace WayCoder;

public static partial class ModelCli
{
    public static string ListKeys()
    {
        var entries = ApiKeyStore.ListAllEntries();
        if (entries.Count == 0)
            return "未保存任何 API key。用 --model key <供应商> <key> [有效期] 保存。";

        var sb = new StringBuilder();
        sb.AppendLine("已保存 API keys：");
        var expired = 0;
        var expiringSoon = 0;
        foreach (var (pid, entry) in entries)
        {
            var expiryText = ApiKeyStore.ExpiryText(entry.Expiry);
            if (ApiKeyStore.IsExpired(entry.Expiry)) expired++;
            else if (ApiKeyStore.DaysLeft(entry.Expiry) <= 7) expiringSoon++;
            sb.AppendLine($"  {pid,-12} = {ApiKeyStore.Masked(pid),-30}有效期: {expiryText}");
        }
        if (expired > 0) sb.AppendLine($"⚠ {expired} 个 key 已过期，请及时更换");
        if (expiringSoon > 0) sb.AppendLine($"⚠ {expiringSoon} 个 key 临近到期（≤7 天）");
        sb.AppendLine("设置/修改有效期：--model key expiry <供应商> <有效期>");
        return sb.ToString();
    }

    /// <summary>保存指定供应商的 API key（可选有效期：永久 / 截止日期）。
    /// 合法性校验：只允许英文字母数字 + `+-_.` 逗号；环境变量引用（$VAR）拒绝。</summary>
    public static string SetKey(string providerId, string key, string? expiry = null)
    {
        if (string.IsNullOrWhiteSpace(providerId) || string.IsNullOrWhiteSpace(key))
            return "用法: --model key <供应商> <key> [有效期]";
        if (ApiKeyStore.IsEnvVarRef(key))
            return $"❌ {key} 是环境变量引用，不是真实 Key，已拒绝保存。请填真实 API Key。";
        if (!ApiKeyStore.IsValidApiKey(key))
            return $"❌ Key 含非法字符（只允许英文字母数字 + - _ . ,）：{ApiKeyStore.Masked(key)}";
        ApiKeyStore.Set(providerId, key, expiry);
        return $"已保存 {providerId} 的 API key：{ApiKeyStore.Masked(providerId)}（有效期: {ApiKeyStore.ExpiryText(expiry)}）";
    }

    /// <summary>给已存 key 设置/修改有效期（不改动 key 本身）。</summary>
    public static string SetKeyExpiry(string providerId, string? expiry)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            return "用法: --model key expiry <供应商> <有效期>";
        if (!ApiKeyStore.Has(providerId))
            return $"服务商 {providerId} 未保存 key，先用 --model key <供应商> <key> 保存";
        ApiKeyStore.SetExpiry(providerId, expiry);
        return $"已设置 {providerId} 的 API key 有效期：{ApiKeyStore.ExpiryText(expiry)}";
    }

    /// <summary>
    /// 导入外部模型数据库（OpenCode / OpenClaw / Crush / Claude Code / Codex / 通用 JSON 文件 / 内置目录），写入全局模型库。
    /// source: null/auto/all=自动探测全部；逗号分隔多来源（opencode,codex,claude）；单来源；builtin=恢复被清空的内置目录；否则视为文件路径。
    /// </summary>
}
