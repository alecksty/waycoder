using System.Text;

namespace WayCoder;

/// <summary>
/// 配置读写核心逻辑 —— 供 /config 斜杠命令与 --config 命令行参数共用，
/// 返回纯文本，由调用方决定输出到屏幕（ChatScreen）还是控制台（Console）。
/// </summary>
public static class ConfigCli
{
    /// <summary>列出全部设置项（按分类分组）</summary>
    public static string List()
    {
        var schema = Config.SettingSchema();
        var sb = new StringBuilder();
        sb.AppendLine($"配置设置（共 {schema.Count} 项）");
        sb.AppendLine();

        foreach (var g in schema.GroupBy(s => s.Category))
        {
            sb.AppendLine($"[{g.Key}]");
            foreach (var s in g.OrderBy(x => x.Order))
            {
                var val = Config.GetPropValue(s.Key) ?? "";
                if (s.Type == "secret" && val.Length > 0) val = "••••••••";
                if (s.Type == "number" && val == "") val = "(空)";
                sb.AppendLine($"  {s.Key,-20} = {val}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("修改: --config set <key> <value>　查值: --config get <key>　(或 --config <key> [value])");
        return sb.ToString();
    }

    /// <summary>读取单项（含描述 / 环境变量 / 可选项）</summary>
    public static string Get(string key)
    {
        var p = Config.FindProp(key);
        if (p == null)
            return $"未知设置项「{key}」。用 --config list 查看全部。";

        var val = Config.GetPropValue(p.Key) ?? "";
        if (p.Type == "secret" && val.Length > 0) val = "••••••••";

        return $"{p.Label} ({p.Key}) = {val}\n  {p.Desc}\n  环境变量: {p.EnvVar}" +
            (p.Options is { Length: > 0 } ? $"\n  可选: {string.Join(" / ", p.Options)}" : "");
    }

    /// <summary>设置单项并写入 .env，返回结果文本</summary>
    public static string Set(string key, string value)
    {
        if (Config.TrySetPropValue(key, value, out var err))
        {
            Config.Instance.SaveToEnvFile();

            var p = Config.FindProp(key);
            var newVal = Config.GetPropValue(key) ?? "";
            if (p?.Type == "secret" && newVal.Length > 0) newVal = "••••••••";

            return $"已设置 {p?.Label ?? key} = {newVal}（已写入 ~/.waycoder/config.json）";
        }
        return $"错误: {err}";
    }
}
