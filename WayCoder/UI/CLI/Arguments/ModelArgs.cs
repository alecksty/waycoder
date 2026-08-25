using WayCoder.Tools;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Cli.Arguments;

// ═══════════════════════════════════════════════════════════════
// 模型参数
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// --model 模型管理（对标 /model 斜杠命令）。
///   --model                        → 显示当前模型
///   --model list [关键词]          → 列出模型目录（内置 + 自定义合并）
///   --model name &lt;id&gt;        → 选中大模型并持久化（同步服务商 + base-url + 写 .env）
///   --model small &lt;id&gt;       → 选中小模型并持久化（同步小模型服务商）
///   --model key &lt;供应商&gt; &lt;key&gt; [有效期] → 保存 API key（有效期=永久/截止日期；无参列出已存 keys）
///   --model key expiry &lt;供应商&gt; &lt;有效期&gt; → 设置/修改已存 key 的有效期
///   --model connect &lt;base-url&gt; → 设置连接地址（写 .env）
///   --model import [来源]          → 导入外部模型库（opencode/openclaw/crush/claude/codex/文件，无参自动探测）
///   --model add [model|provider|key] → 手动添加模型 / 服务商 / API key
///   --model remove [model|provider|key] &lt;目标&gt; → 删除模型 / 服务商 / API key
///   --model test                   → 模型连通性测试（有 key 的 + 本地模型，哪些能连上）
///   --model &lt;模型ID&gt;          → 快捷选中（本次会话，不持久化，向后兼容）
/// </summary>
public class ModelArg : CliArg
{
    public override string Description => "模型管理（key 支持有效期：--model key <供应商> <key> [永久|日期]）";
    public override int ValueCount => -1;
    public override bool Greedy => true;
    public override string? ValueLabel => "模型ID/子命令";
    public override (string Cmd, string Desc)[]? SubCommands =>
    [
        ("list [供应商]", "列出模型目录（OpenRouter 显示短名）"),
        ("name <id>", "选中大模型"),
        ("small <id>", "选中小模型"),
        ("key [set|remove]", "管理 API Key（key 永不自动删除，删除需确认）"),
        ("check [connect]", "检查模型能力（think/tools/vision/格式/上下文/key）"),
        ("report", "测试所有 connect 连通性，生成可用/失败报告"),
        ("free", "测试所有 free 模型（zen -free / openrouter :free），列出可用"),
        ("restore", "恢复 /free 切换免费模型之前的模型"),
        ("import [alllocal|allonline|all|online <源>|来源]", "导入模型（alllocal=本地 / allonline=在线 / all=全部 / online <源>=指定端点）"),
        ("add model/provider/key", "添加模型 / 服务商 / Key"),
        ("remove <id>", "移除模型或服务商"),
        ("clean", "清理无效服务商/模型 + 合并重复 + 删无效 connect"),
        ("test", "测试连接"),
        ("connect <base-url>", "设置 API 地址"),
        ("<模型ID>", "快捷选中模型"),
    ];
    public ModelArg() : base("model", "-m", "--model") { }

    public override int? OnMatch(List<string> values)
    {
        if (values.Count == 0)
        {
            Console.WriteLine(ModelCli.Current());
            return 0;
        }

        var first = values[0].ToLowerInvariant();
        var rest = values.Skip(1).ToArray();

        string result;
        switch (first)
        {
            case "list":
            case "ls":
                result = ModelCli.List(rest.Length > 0 ? rest[0] : null);
                break;
            case "name":
                if (rest.Length == 0) { result = "用法: --model name <模型ID>"; break; }
                ModelCli.RememberCurrentModel(); // 记住切换前模型（--model restore / /free-restore 可恢复）
                result = ModelCli.Select(rest[0]);
                break;
            case "restore":
            case "free-restore":
            case "恢复":
                result = ModelCli.RestorePrevious();
                break;
            case "small":
                result = rest.Length == 0 ? "用法: --model small <小模型ID>" : ModelCli.SelectSmall(rest[0]);
                break;
            case "key":
            case "keys":
                result = DispatchKey(rest);
                break;
            case "connect":
                result = rest.Length == 0 ? "用法: --model connect <base-url>" : ModelCli.Connect(rest[0]);
                break;
            case "check":
            case "caps":
            case "capabilities":
                result = ModelCli.Check(rest.Length > 0 ? rest[0] : null);
                break;
            case "report":
                result = ModelCli.Report(rest.Length > 1 ? rest[1] : null);
                break;
            case "free":
                result = ModelCli.Free(rest.Length > 1 ? rest[1] : null);
                break;
            case "import":
                // 组合命令：alllocal=全部本地(ollama/lmstudio/cc-switch)、allonline=全部在线端点、all/auto=本地+在线；
                // online [源名...]=在线拉取指定端点；其余走 Import（单源本地/配置文件/opencode 等）
                var src0 = rest.Length > 0 ? rest[0].ToLowerInvariant() : "";
                if (src0 is "all" or "auto")
                {
                    result = ModelCli.ImportLocalServices(msg => Console.WriteLine(msg)) + "\n" + ModelCli.ImportOnlineAll(null, msg => Console.WriteLine(msg));
                }
                else if (src0 == "alllocal")
                {
                    result = ModelCli.ImportLocalServices(msg => Console.WriteLine(msg));
                }
                else if (src0 is "online" or "allonline")
                {
                    var names = rest.Skip(1).ToArray();
                    result = ModelCli.ImportOnlineAll(names.Length > 0 ? names : null, msg => Console.WriteLine(msg));
                }
                else
                {
                    result = ModelCli.Import(rest.Length > 0 ? rest[0] : null, msg => Console.WriteLine(msg));
                }
                break;
            case "add":
                result = DispatchAdd(rest);
                break;
            case "remove":
            case "rm":
            case "delete":
            case "del":
                result = DispatchRemove(rest);
                break;
            case "test":
                result = ModelCli.Test();
                break;
            case "prune":
            case "clean":
            case "cleanup":
                // 统一清理：无效服务商（删 providers.json + key + 模型）+ 合并重复模型 + 删无效 connect
                result = ModelCli.ProviderCli.CleanText() + "\n" + ModelCli.Clean();
                break;
            default:
                // 裸模型名：本次会话快捷选中，交给 Program 继续运行
                return null;
        }

        Console.WriteLine(result);
        return 0;
    }

    static string DispatchKey(string[] rest)
    {
        if (rest.Length == 0) return ModelCli.ListKeys();
        // --model key set <供应商> <key> [有效期]
        if (rest.Length >= 3 && rest[0].Equals("set", StringComparison.OrdinalIgnoreCase))
            return ModelCli.SetKey(rest[1], rest[2], rest.Length > 3 ? string.Join(" ", rest.Skip(3)) : null);
        // --model key expiry <供应商> <有效期>（仅改有效期，不动 key）
        if (rest.Length >= 3 && rest[0].Equals("expiry", StringComparison.OrdinalIgnoreCase))
            return ModelCli.SetKeyExpiry(rest[1], string.Join(" ", rest.Skip(2)));
        // --model key remove <供应商>
        if (rest.Length >= 2 && rest[0].Equals("remove", StringComparison.OrdinalIgnoreCase))
            return ModelCli.RemoveKey(rest[1]);
        // --model key <供应商> <key> [有效期]
        if (rest.Length >= 2)
            return ModelCli.SetKey(rest[0], rest[1], rest.Length > 2 ? string.Join(" ", rest.Skip(2)) : null);
        return "用法: --model key [set|remove|expiry] <供应商> <key> [有效期]";
    }

    static string DispatchAdd(string[] rest)
    {
        if (rest.Length == 0)
            return "用法: --model add [model <id> <供应商ID> [baseUrl] | provider <供应商ID> [baseUrl] | key <供应商ID> <key>]";
        var sub = rest[0].ToLowerInvariant();
        switch (sub)
        {
            case "model":
                return rest.Length >= 2
                    ? ModelCli.AddModel(rest[1], rest.Length > 2 ? rest[2] : null, rest.Length > 3 ? rest[3] : null)
                    : "用法: --model add model <id> [<供应商ID> [baseUrl]]";
            case "provider":
            case "prov":
                return rest.Length >= 2
                    ? ModelCli.AddProvider(rest[1], rest.Length > 2 ? rest[2] : null)
                    : "用法: --model add provider <供应商ID> [baseUrl]";
            case "key":
            case "keys":
            case "apikey":
                return rest.Length >= 3
                    ? ModelCli.SetKey(rest[1], rest[2])
                    : "用法: --model add key <供应商ID> <key>";
            default:
                // 无子命令：add <id> <供应商ID> [baseUrl]
                return rest.Length >= 2
                    ? ModelCli.AddModel(rest[0], rest[1], rest.Length > 2 ? rest[2] : null)
                    : ModelCli.AddModel(rest[0], null, null);
        }
    }

    static string DispatchRemove(string[] rest)
    {
        if (rest.Length == 0)
            return "用法: --model remove [model <id> | provider <pid> | key <pid>]（无子命令时 <id> 视为删除模型）";
        var sub = rest[0].ToLowerInvariant();
        if (rest.Length >= 2 && sub is "model")
            return ModelCli.Remove(rest[1]);
        if (rest.Length >= 2 && sub is "provider" or "prov")
            return ModelCli.RemoveProvider(rest[1]);
        if (rest.Length >= 2 && sub is "key" or "keys" or "apikey")
            return ModelCli.RemoveKey(rest[1]);
        // 无子命令：rest[0] 即模型 id（向后兼容 --model remove <id>）
        return ModelCli.Remove(rest[0]);
    }
}

public class BaseUrlArg : CliArg
{
    public override string Description => "API 基础 URL";
    public override int ValueCount => 1;
    public override string? ValueLabel => "URL";
    public BaseUrlArg() : base("base-url", "-b", "--base-url") { }
}

public class ApiKeyArg : CliArg
{
    public override string Description => "API 密钥（自动保存到全局 ~/.waycoder/api_keys.json，按当前服务商）";
    public override int ValueCount => 1;
    public override string? ValueLabel => "密钥";
    public ApiKeyArg() : base("api-key", "-k", "--api-key") { }
}

/// <summary>管理连接层（connect 注册表 / 命名连接 / 回退链）——list/add/rm/select/&lt;id&gt;/test/import。</summary>
public class ConnectArg : CliArg
{
    public override string Description => "连接层管理（connect 注册表 / 命名连接 / 回退链）";
    public override int ValueCount => -1;
    public override bool Greedy => true;
    public override string? ValueLabel => "connect名/子命令";
    public override (string Cmd, string Desc)[]? SubCommands =>
    [
        ("list", "列出 connect + 命名连接 + 回退链"),
        ("add <name> <providerId> <modelId>", "新增 connect"),
        ("rm <name>", "删除 connect"),
        ("select <id>", "切换大 connect（connect名 | providerId.modelId | providerId/modelId | baseUrl:model | modelId）"),
        ("<id>", "快捷切换（同 select）"),
        ("test", "连通性测试"),
        ("import", "登记当前大/小/回退模型为 connect"),
        ("conn add <name> <大> <小>", "新增命名连接（大/小一起切）"),
        ("use <name>", "切换命名连接"),
        ("chain <c1> <c2> ...", "设置全局回退链（connect 名）"),
    ];
    public ConnectArg() : base("connect", "--connect") { }
    public override int? OnMatch(List<string> values)
    {
        if (values.Count == 0)
        {
            var cfg = Config.Instance;
            var cur = ConnectionConfig.CurrentByConfig();
            Console.WriteLine(cur != null
                ? $"当前连接：`{cur.Name}`\n  大 = {ConnectionConfig.FormatModel(ConnectionConfig.FindConnect(cur.BigConnect)?.ProviderId ?? "", ConnectionConfig.FindConnect(cur.BigConnect)?.ModelId ?? "")}\n  小 = {ConnectionConfig.FormatModel(ConnectionConfig.FindConnect(cur.SmallConnect)?.ProviderId ?? "", ConnectionConfig.FindConnect(cur.SmallConnect)?.ModelId ?? "")}"
                : $"当前：大={ConnectionConfig.FormatModel(cfg.Provider, cfg.Model)} · 小={ConnectionConfig.FormatModel(cfg.SmallProvider, cfg.SmallModel)}");
            return 0;
        }

        var first = values[0].ToLowerInvariant();
        var rest = values.Skip(1).ToArray();
        switch (first)
        {
            case "list":
            case "ls":
            {
                var connects = ConnectionConfig.ListConnects();
                var sb = new System.Text.StringBuilder($"**connect 注册表**（{connects.Count}）：\n");

                // 列宽：取各列最大长度（+2 余量），分列对齐，避免名称溢出错位
                var nameW = 8; var pidW = 8;
                foreach (var c in connects)
                {
                    nameW = Math.Max(nameW, c.Name.Length + 2);
                    pidW = Math.Max(pidW, c.ProviderId.Length + 2);
                }

                foreach (var c in connects)
                {
                    var hasKey = !string.IsNullOrEmpty(ConnectionConfig.ResolveProvider(c.ProviderId)?.ApiKey);
                    var keyMark = hasKey ? "🔑" : "⚠";
                    sb.AppendLine($"  {c.Name.PadRight(nameW)} {c.ProviderId.PadRight(pidW)} {c.ModelId}  {keyMark}");
                }
                var chain = ConnectionConfig.FallbackChain;
                sb.AppendLine($"回退链：{(chain.Count > 0 ? string.Join(" → ", chain) : "（无）")}");
                Console.WriteLine(sb.ToString().TrimEnd());
                return 0;
            }
            case "add":
            case "new":
                if (rest.Length < 3) { Console.WriteLine("用法: --connect add <name> <providerId> <modelId>"); return 1; }
                // 先尝试从官方环境变量自动导入 key（无 key 时），再建 connect
                var keyHint = ConnectionConfig.AutoImportKeyFromEnv(rest[1]);
                Console.WriteLine(ConnectionConfig.AddConnect(rest[0], rest[1], rest[2], out var e)
                    ? $"✅ 已新增 connect「{rest[0]}」{keyHint ?? ""}" : $"❌ {e}");
                return 0;
            case "rm":
            case "remove":
            case "delete":
            case "del":
                if (rest.Length < 1) { Console.WriteLine("用法: --connect rm <name>"); return 1; }
                Console.WriteLine(ConnectionConfig.RemoveConnect(rest[0], out var re)
                    ? $"🗑 已删除 connect「{rest[0]}」" : $"❌ {re}");
                return 0;
            case "select":
            case "switch":
                if (rest.Length < 1) { Console.WriteLine("用法: --connect select <id>"); return 1; }
                ConnectionConfig.ApplySpec(rest[0], true, out var sm);
                Console.WriteLine($"✅ {sm}");
                return 0;
            case "test":
            {
                var sb = new System.Text.StringBuilder("**connect 连通性测试**：\n");
                foreach (var pid in ConnectionConfig.ListConnects().Select(c => c.ProviderId).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var p = ConnectionConfig.ResolveProvider(pid);
                    if (string.IsNullOrWhiteSpace(p?.BaseUrl)) { sb.AppendLine($"  ❌ `{pid}` — 无端点"); continue; }
                    var (o, d) = ModelCli.ProbeEndpoint(p!.BaseUrl, p.ApiKey);
                    sb.AppendLine($"  {(o ? "✅" : "❌")} `{pid}` {p.BaseUrl} — {d}");
                }
                Console.WriteLine(sb.ToString().TrimEnd());
                return 0;
            }
            case "import":
            {
                ConnectionConfig.FindOrCreateConnect(Config.Instance.Provider, Config.Instance.Model);
                ConnectionConfig.FindOrCreateConnect(Config.Instance.SmallProvider, Config.Instance.SmallModel);
                Console.WriteLine($"✅ 已登记大/小模型为 connect：{ConnectionConfig.FormatModel(Config.Instance.Provider, Config.Instance.Model)} / {ConnectionConfig.FormatModel(Config.Instance.SmallProvider, Config.Instance.SmallModel)}");
                return 0;
            }
            case "use":
                if (rest.Length < 1) { Console.WriteLine("用法: --connect use <connectionName>"); return 1; }
                ConnectionConfig.ActivateConnection(rest[0], out var um);
                Console.WriteLine($"✅ {um}");
                return 0;
            case "chain":
            case "fallback":
                if (rest.Length > 0 && (rest[0].Equals("on", StringComparison.OrdinalIgnoreCase) || rest[0].Equals("off", StringComparison.OrdinalIgnoreCase)))
                {
                    Config.Instance.FallbackEnabled = rest[0].Equals("on", StringComparison.OrdinalIgnoreCase);
                    Config.Instance.SaveToEnvFile();
                    Console.WriteLine($"✅ 回退链已{(Config.Instance.FallbackEnabled ? "开启" : "关闭")}");
                    return 0;
                }
                ConnectionConfig.SetFallbackChain(rest);
                Console.WriteLine($"✅ 回退链：{string.Join(" → ", ConnectionConfig.FallbackChain)}" +
                    (Config.Instance.FallbackEnabled ? "（开）" : "（关，/connect chain on 开启）"));
                return 0;
            default:
                // <id> → 快捷切换
                ConnectionConfig.ApplySpec(values[0], true, out var dm);
                Console.WriteLine($"✅ {dm}");
                return 0;
        }
    }
}

/// <summary>管理服务商数据库（providers.json）——list / add / rm / clean。</summary>
public class ProviderArg : CliArg
{
    public override string Description => "管理服务商数据库（providers.json）";
    public override int ValueCount => -1;
    public override bool Greedy => true;
    public override string? ValueLabel => "子命令";
    public override (string Cmd, string Desc)[]? SubCommands =>
    [
        ("list", "列出所有服务商"),
        ("add <id> <名称> <base-url>", "添加服务商"),
        ("rm <id>", "移除服务商（同时清除其 Key）"),
        ("select <id>", "切换当前服务商（作用于当前大模型 connect）"),
        ("key <id> <key>", "保存某服务商的 API key"),
        ("test", "连通性测试"),
        ("import [source]", "导入外部模型库"),
        ("clean", "清理无效服务商（探测失败的）"),
    ];
    public ProviderArg() : base("provider", "--provider") { }
    public override int? OnMatch(List<string> values) => ModelCli.ProviderCli.Run(values);
}
