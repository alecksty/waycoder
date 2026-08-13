using System.Text;
using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

/// <summary>
/// /config — 命令行配置（对标 Claude Code /config），无需进设置界面。
///
///   /config                      → 列出所有设置项（按分类）
///   /config list                 → 同列出
///   /config get &lt;key&gt;      → 读取当前值
///   /config set &lt;key&gt; &lt;v&gt; → 设置并保存到 .env
///   /config &lt;key&gt; &lt;v&gt;  → set 简写
///   /config &lt;key&gt;           → get 简写
///
/// key 大小写不敏感，也可用环境变量名（如 WAYCODER_MODEL）。
/// 全部设置项见 Config._schema（Schema 驱动，与设置界面同一份数据源）。
/// </summary>
public class ConfigCommand : SlashCommand
{
    public override string Name => "/config";
    public override string[] Aliases => ["/cfg", "/set"];
    public override string Description => "配置 (命令行) — list/get/set 或 /config <key> <value>";
    public override string? Usage => "/config [list|get <key>|set <key> <value>]";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        var parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            ListAll(screen);
            return Task.CompletedTask;
        }

        var first = parts[0].ToLowerInvariant();
        var rest = parts.Skip(1).ToArray();

        switch (first)
        {
            case "list":
            case "ls":
                ListAll(screen);
                break;
            case "get":
                Get(screen, rest);
                break;
            case "set":
                Set(screen, rest);
                break;
            default:
                // 简写：/config <key> [value]
                if (rest.Length == 0) Get(screen, [parts[0]]);
                else Set(screen, parts); // key + 剩余拼接为 value
                break;
        }

        return Task.CompletedTask;
    }

    static void ListAll(ChatScreen screen)
    {
        var schema = Config.SettingSchema();
        var sb = new StringBuilder();
        sb.AppendLine($"**配置设置**（共 {schema.Count} 项）\n");

        foreach (var g in schema.GroupBy(s => s.Category))
        {
            sb.AppendLine($"**{g.Key}**");
            foreach (var s in g.OrderBy(x => x.Order))
            {
                var val = Config.GetPropValue(s.Key) ?? "";
                if (s.Type == "secret" && val.Length > 0) val = "••••••••";
                if (s.Type == "number" && val == "") val = "(空)";
                sb.AppendLine($"  `{s.Key}` = {val}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("修改: `/config set <key> <value>`　查值: `/config get <key>`　(或 `/config <key> [value]`)");
        screen.AddSystemMsg(sb.ToString());
    }

    static void Get(ChatScreen screen, string[] args)
    {
        if (args.Length == 0)
        {
            screen.AddSystemMsg("用法: /config get <key>");
            return;
        }

        var p = Config.FindProp(args[0]);
        if (p == null)
        {
            screen.AddSystemMsg($"未知设置项「{args[0]}」。用 /config list 查看全部。");
            return;
        }

        var val = Config.GetPropValue(p.Key) ?? "";
        if (p.Type == "secret" && val.Length > 0) val = "••••••••";

        screen.AddSystemMsg($"**{p.Label}** (`{p.Key}`) = `{val}`\n  {p.Desc}\n  环境变量: `{p.EnvVar}`" +
            (p.Options is { Length: > 0 } ? $"\n  可选: {string.Join(" / ", p.Options)}" : ""));
    }

    static void Set(ChatScreen screen, string[] args)
    {
        if (args.Length < 2)
        {
            screen.AddSystemMsg("用法: /config set <key> <value>");
            return;
        }

        var key = args[0];
        var value = string.Join(" ", args.Skip(1)).Trim();

        if (Config.TrySetPropValue(key, value, out var err))
        {
            Config.Instance.SaveToEnvFile();

            // 主题类即时生效（其余配置在下次使用/重启时生效）
            if (key.Equals("ThemePreset", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("ColorScheme", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("BorderStyle", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("BorderColor", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("AccentColor", StringComparison.OrdinalIgnoreCase))
                screen.SyncTheme();

            var p = Config.FindProp(key);
            var newVal = Config.GetPropValue(key) ?? "";
            if (p?.Type == "secret" && newVal.Length > 0) newVal = "••••••••";

            screen.AddSystemMsg($"✅ 已设置 **{p?.Label ?? key}** = `{newVal}`（已写入 .env）");
        }
        else
        {
            screen.AddSystemMsg($"❌ {err}");
        }
    }
}
