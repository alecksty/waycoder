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
    public override string Description => L.Pick(
        "模型管理（key 支持有效期：--model key <供应商> <key> [永久|日期]）",
        "Model management (keys support expiry: --model key <provider> <key> [forever|date])");
    public override int ValueCount => -1;
    public override bool Greedy => true;
    public override string? ValueLabel => L.Pick("模型ID/子命令", "model ID/subcommand");
    public override (string Cmd, string Desc)[]? SubCommands =>
    [
        (L.Pick("list [供应商]", "list [provider]"), L.Pick("列出模型目录（OpenRouter 显示短名）", "List the model catalog (OpenRouter shows short names)")),
        ("name <id>", L.Pick("选中大模型", "Select the main model")),
        ("small <id>", L.Pick("选中小模型", "Select the small model")),
        ("key [set|remove]", L.Pick("管理 API Key（key 永不自动删除，删除需确认）", "Manage API keys (keys are never auto-deleted; removal asks for confirmation)")),
        ("check [connect]", L.Pick("检查模型能力（think/tools/vision/格式/上下文/key）", "Check model capabilities (think/tools/vision/format/context/key)")),
        ("report", L.Pick("测试所有 connect 连通性，生成可用/失败报告", "Test connectivity of every connect and report what works and what fails")),
        ("free", L.Pick("测试所有 free 模型（zen -free / openrouter :free），列出可用", "Test every free model (zen -free / openrouter :free) and list the reachable ones")),
        ("restore", L.Pick("恢复 /free 切换免费模型之前的模型", "Restore the model from before /free switched to a free one")),
        (L.Pick("import [alllocal|allonline|all|online <源>|来源]", "import [alllocal|allonline|all|online <source>|source]"), L.Pick("导入模型（alllocal=本地 / allonline=在线 / all=全部 / online <源>=指定端点）", "Import models (alllocal = local / allonline = online / all = both / online <source> = one endpoint)")),
        ("add model/provider/key", L.Pick("添加模型 / 服务商 / Key", "Add a model / provider / key")),
        ("remove <id>", L.Pick("移除模型或服务商", "Remove a model or provider")),
        ("clean", L.Pick("清理无效服务商/模型 + 合并重复 + 删无效 connect", "Clean invalid providers/models + merge duplicates + drop invalid connects")),
        ("test", L.Pick("测试连接", "Test the connection")),
        ("connect <base-url>", L.Pick("设置 API 地址", "Set the API base URL")),
        (L.Pick("<模型ID>", "<model ID>"), L.Pick("快捷选中模型", "Quick-select a model")),
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
                if (rest.Length == 0) { result = L.Pick("用法: --model name <模型ID>", "Usage: --model name <model ID>"); break; }
                ModelCli.RememberCurrentModel(); // 记住切换前模型（--model restore / /free-restore 可恢复）
                result = ModelCli.Select(rest[0]);
                break;
            case "restore":
            case "free-restore":
            case "恢复":
                result = ModelCli.RestorePrevious();
                break;
            case "small":
                result = rest.Length == 0
                    ? L.Pick("用法: --model small <小模型ID>", "Usage: --model small <small model ID>")
                    : ModelCli.SelectSmall(rest[0]);
                break;
            case "key":
            case "keys":
                result = DispatchKey(rest);
                break;
            case "connect":
                result = rest.Length == 0
                    ? L.Pick("用法: --model connect <base-url>", "Usage: --model connect <base-url>")
                    : ModelCli.Connect(rest[0]);
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
        return L.Pick("用法: --model key [set|remove|expiry] <供应商> <key> [有效期]",
                      "Usage: --model key [set|remove|expiry] <provider> <key> [expiry]");
    }

    static string DispatchAdd(string[] rest)
    {
        if (rest.Length == 0)
            return L.Pick(
                "用法: --model add [model <id> <供应商ID> [baseUrl] | provider <供应商ID> [baseUrl] | key <供应商ID> <key>]",
                "Usage: --model add [model <id> <provider ID> [baseUrl] | provider <provider ID> [baseUrl] | key <provider ID> <key>]");
        var sub = rest[0].ToLowerInvariant();
        switch (sub)
        {
            case "model":
                return rest.Length >= 2
                    ? ModelCli.AddModel(rest[1], rest.Length > 2 ? rest[2] : null, rest.Length > 3 ? rest[3] : null)
                    : L.Pick("用法: --model add model <id> [<供应商ID> [baseUrl]]",
                             "Usage: --model add model <id> [<provider ID> [baseUrl]]");
            case "provider":
            case "prov":
                return rest.Length >= 2
                    ? ModelCli.AddProvider(rest[1], rest.Length > 2 ? rest[2] : null)
                    : L.Pick("用法: --model add provider <供应商ID> [baseUrl]",
                             "Usage: --model add provider <provider ID> [baseUrl]");
            case "key":
            case "keys":
            case "apikey":
                return rest.Length >= 3
                    ? ModelCli.SetKey(rest[1], rest[2])
                    : L.Pick("用法: --model add key <供应商ID> <key>",
                             "Usage: --model add key <provider ID> <key>");
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
            return L.Pick(
                "用法: --model remove [model <id> | provider <pid> | key <pid>]（无子命令时 <id> 视为删除模型）",
                "Usage: --model remove [model <id> | provider <pid> | key <pid>] (without a subcommand, <id> is taken as a model to delete)");
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
    public override string Description => L.Pick("API 基础 URL", "API base URL");
    public override int ValueCount => 1;
    public override string? ValueLabel => "URL";
    public BaseUrlArg() : base("base-url", "-b", "--base-url") { }
}

public class ApiKeyArg : CliArg
{
    public override string Description => L.Pick(
        "API 密钥（自动保存到全局 ~/.waycoder/api_keys.json，按当前服务商）",
        "API key (saved automatically to the global ~/.waycoder/api_keys.json, under the current provider)");
    public override int ValueCount => 1;
    public override string? ValueLabel => L.Pick("密钥", "key");
    public ApiKeyArg() : base("api-key", "-k", "--api-key") { }
}

/// <summary>管理连接层（connect 注册表 / 命名连接 / 回退链）——list/add/rm/select/&lt;id&gt;/test/import。</summary>
public class ConnectArg : CliArg
{
    public override string Description => L.Pick(
        "连接层管理（connect 注册表 / 命名连接 / 回退链）",
        "Connection layer management (connect registry / named connections / fallback chain)");
    public override int ValueCount => -1;
    public override bool Greedy => true;
    public override string? ValueLabel => L.Pick("connect名/子命令", "connect name/subcommand");
    public override (string Cmd, string Desc)[]? SubCommands =>
    [
        ("list", L.Pick("列出 connect + 命名连接 + 回退链", "List connects + named connections + the fallback chain")),
        ("add <name> <providerId> <modelId>", L.Pick("新增 connect", "Add a connect")),
        ("rm <name>", L.Pick("删除 connect", "Remove a connect")),
        ("select <id>", L.Pick("切换大 connect（connect名 | providerId.modelId | providerId/modelId | baseUrl:model | modelId）", "Switch the main connect (connect name | providerId.modelId | providerId/modelId | baseUrl:model | modelId)")),
        ("<id>", L.Pick("快捷切换（同 select）", "Quick switch (same as select)")),
        ("test", L.Pick("连通性测试", "Connectivity test")),
        ("import", L.Pick("登记当前大/小/回退模型为 connect", "Register the current main/small/fallback models as connects")),
        (L.Pick("conn add <name> <大> <小>", "conn add <name> <main> <small>"), L.Pick("新增命名连接（大/小一起切）", "Add a named connection (switches main and small together)")),
        ("use <name>", L.Pick("切换命名连接", "Switch to a named connection")),
        ("chain <c1> <c2> ...", L.Pick("设置全局回退链（connect 名）", "Set the global fallback chain (connect names)")),
    ];
    public ConnectArg() : base("connect", "--connect") { }
    public override int? OnMatch(List<string> values)
    {
        if (values.Count == 0)
        {
            var cfg = Config.Instance;
            var cur = ConnectionConfig.CurrentByConfig();
            Console.WriteLine(cur != null
                ? L.Pick($"当前连接：`{cur.Name}`\n  大 = {ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(ConnectionConfig.FindConnect(cur.BigConnect)?.ProviderId), ConnectionConfig.FindConnect(cur.BigConnect)?.ModelId ?? "")}\n  小 = {ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(ConnectionConfig.FindConnect(cur.SmallConnect)?.ProviderId), ConnectionConfig.FindConnect(cur.SmallConnect)?.ModelId ?? "")}",
                         $"Current connect: `{cur.Name}`\n  main = {ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(ConnectionConfig.FindConnect(cur.BigConnect)?.ProviderId), ConnectionConfig.FindConnect(cur.BigConnect)?.ModelId ?? "")}\n  small = {ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(ConnectionConfig.FindConnect(cur.SmallConnect)?.ProviderId), ConnectionConfig.FindConnect(cur.SmallConnect)?.ModelId ?? "")}")
                : L.Pick($"当前：大={ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(cfg.Provider), cfg.Model)} · 小={ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(cfg.SmallProvider), cfg.SmallModel)}",
                         $"Current: main={ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(cfg.Provider), cfg.Model)} · small={ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(cfg.SmallProvider), cfg.SmallModel)}"));
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
                var sb = new System.Text.StringBuilder(L.Pick(
                    $"**connect 注册表**（{connects.Count}）：\n",
                    $"**connect registry** ({connects.Count}):\n"));

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
                sb.AppendLine(L.Pick(
                    $"回退链：{(chain.Count > 0 ? string.Join(" → ", chain) : "（无）")}",
                    $"Fallback chain: {(chain.Count > 0 ? string.Join(" → ", chain) : "(none)")}"));
                Console.WriteLine(sb.ToString().TrimEnd());
                return 0;
            }
            case "add":
            case "new":
                if (rest.Length < 3) { Console.WriteLine(L.Pick("用法: --connect add <name> <providerId> <modelId>", "Usage: --connect add <name> <providerId> <modelId>")); return 1; }
                // 先尝试从官方环境变量自动导入 key（无 key 时），再建 connect
                var keyHint = ConnectionConfig.AutoImportKeyFromEnv(rest[1]);
                Console.WriteLine(ConnectionConfig.AddConnect(rest[0], rest[1], rest[2], out var e)
                    ? L.Pick($"✅ 已新增 connect「{rest[0]}」{keyHint ?? ""}", $"✅ Added connect \"{rest[0]}\" {keyHint ?? ""}")
                    : $"❌ {e}");
                return 0;
            case "rm":
            case "remove":
            case "delete":
            case "del":
                if (rest.Length < 1) { Console.WriteLine(L.Pick("用法: --connect rm <name>", "Usage: --connect rm <name>")); return 1; }
                Console.WriteLine(ConnectionConfig.RemoveConnect(rest[0], out var re)
                    ? L.Pick($"🗑 已删除 connect「{rest[0]}」", $"🗑 Removed connect \"{rest[0]}\"")
                    : $"❌ {re}");
                return 0;
            case "select":
            case "switch":
                if (rest.Length < 1) { Console.WriteLine(L.Pick("用法: --connect select <id>", "Usage: --connect select <id>")); return 1; }
                ConnectionConfig.ApplySpec(rest[0], true, out var sm);
                Console.WriteLine($"✅ {sm}");
                return 0;
            case "test":
            {
                var sb = new System.Text.StringBuilder(L.Pick("**connect 连通性测试**：\n", "**connect connectivity test**:\n"));
                foreach (var pid in ConnectionConfig.ListConnects().Select(c => c.ProviderId).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var p = ConnectionConfig.ResolveProvider(pid);
                    if (string.IsNullOrWhiteSpace(p?.BaseUrl)) { sb.AppendLine(L.Pick($"  ❌ `{pid}` — 无端点", $"  ❌ `{pid}` — no endpoint")); continue; }
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
                Console.WriteLine(L.Pick(
                    $"✅ 已登记大/小模型为 connect：{ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(Config.Instance.Provider), Config.Instance.Model)} / {ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(Config.Instance.SmallProvider), Config.Instance.SmallModel)}",
                    $"✅ Registered the main/small models as connects: {ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(Config.Instance.Provider), Config.Instance.Model)} / {ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(Config.Instance.SmallProvider), Config.Instance.SmallModel)}"));
                return 0;
            }
            case "use":
                if (rest.Length < 1) { Console.WriteLine(L.Pick("用法: --connect use <connectionName>", "Usage: --connect use <connectionName>")); return 1; }
                ConnectionConfig.ActivateConnection(rest[0], out var um);
                Console.WriteLine($"✅ {um}");
                return 0;
            case "chain":
            case "fallback":
                if (rest.Length > 0 && (rest[0].Equals("on", StringComparison.OrdinalIgnoreCase) || rest[0].Equals("off", StringComparison.OrdinalIgnoreCase)))
                {
                    Config.Instance.FallbackEnabled = rest[0].Equals("on", StringComparison.OrdinalIgnoreCase);
                    Config.Instance.SaveToEnvFile();
                    Console.WriteLine(L.Pick(
                        $"✅ 回退链已{(Config.Instance.FallbackEnabled ? "开启" : "关闭")}",
                        $"✅ Fallback chain {(Config.Instance.FallbackEnabled ? "enabled" : "disabled")}"));
                    return 0;
                }
                ConnectionConfig.SetFallbackChain(rest);
                Console.WriteLine(L.Pick($"✅ 回退链：{string.Join(" → ", ConnectionConfig.FallbackChain)}", $"✅ Fallback chain: {string.Join(" → ", ConnectionConfig.FallbackChain)}") +
                    (Config.Instance.FallbackEnabled
                        ? L.Pick("（开）", " (on)")
                        : L.Pick("（关，/connect chain on 开启）", " (off; enable with /connect chain on)")));
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
    public override string Description => L.Pick(
        "管理服务商数据库（providers.json）",
        "Manage the provider database (providers.json)");
    public override int ValueCount => -1;
    public override bool Greedy => true;
    public override string? ValueLabel => L.Pick("子命令", "subcommand");
    public override (string Cmd, string Desc)[]? SubCommands =>
    [
        ("list", L.Pick("列出所有服务商", "List every provider")),
        (L.Pick("add <id> <名称> <base-url>", "add <id> <name> <base-url>"), L.Pick("添加服务商", "Add a provider")),
        ("rm <id>", L.Pick("移除服务商（同时清除其 Key）", "Remove a provider (and clear its key)")),
        ("select <id>", L.Pick("切换当前服务商（作用于当前大模型 connect）", "Switch the current provider (applies to the main model's connect)")),
        ("key <id> <key>", L.Pick("保存某服务商的 API key", "Save an API key for a provider")),
        ("test", L.Pick("连通性测试", "Connectivity test")),
        ("import [source]", L.Pick("导入外部模型库", "Import an external model catalog")),
        ("clean", L.Pick("清理无效服务商（探测失败的）", "Clean invalid providers (ones that failed the probe)")),
        ("reconcile [apply]", L.Pick("把模型归并到同地址的已注册服务商（默认预览；apply/run 才执行，先备份）", "Merge models into the registered provider at the same address (preview by default; apply/run actually writes, after a backup)")),
    ];
    public ProviderArg() : base("provider", "--provider") { }
    public override int? OnMatch(List<string> values) => ModelCli.ProviderCli.Run(values);
}
