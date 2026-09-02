using System.Text;
using WayCoder.Infra;

namespace WayCoder;

public static partial class ModelCli
{
    public static (string Provider, string Model, string? BaseUrl)? PreviousModel
    {
        get
        {
            var c = Config.Instance;
            if (string.IsNullOrEmpty(c.FreePrevModel)) return null;
            return (c.FreePrevProvider ?? "", c.FreePrevModel, c.FreePrevBaseUrl);
        }
        set
        {
            var c = Config.Instance;
            if (value is { } v)
            {
                c.FreePrevProvider = v.Provider;
                c.FreePrevModel = v.Model;
                c.FreePrevBaseUrl = v.BaseUrl;
            }
            else
            {
                c.FreePrevProvider = null;
                c.FreePrevModel = null;
                c.FreePrevBaseUrl = null;
            }
            c.SaveToConfigJson(); // 跨会话持久化
        }
    }

    /// <summary>记住当前模型（切换免费模型前调用；未记录才记，避免覆盖已记住的）。</summary>
    public static void RememberCurrentModel()
    {
        if (PreviousModel == null)
            PreviousModel = (Config.Instance.Provider, Config.Instance.Model, Config.Instance.BaseUrl);
    }

    /// <summary>恢复 /free 切换前的模型（三端共用：TUI /free-restore、Web /free-restore、CLI --model restore）。</summary>
    public static string RestorePrevious()
    {
        if (PreviousModel is not { } prev) return "⚠️ 无之前模型可恢复（先切换免费模型）";
        ConnectionConfig.ApplyModelChoice(prev.Provider, prev.Model, isLarge: true, out var msg, prev.BaseUrl);
        PreviousModel = null;
        return $"✅ 已恢复之前模型：{ModelCatalog.ShortDisplayName(prev.Model)}（{prev.Provider}）";
    }

    /// <summary>枚举模型库中所有 free 模型（id 含 free，同 provider+id 去重）。</summary>
    public static List<ModelCatalog.ModelInfo> EnumerateFreeModels()
        => ModelCatalog.All
            .Where(m => m.Id.ToLowerInvariant().Contains("free"))
            .GroupBy(m => $"{m.ProviderId}|{m.Id.ToLowerInvariant()}")   // 同 provider+id 去重
            .Select(g => g.First())
            .ToList();

    /// <summary>免费可用 connect（/free 弹窗切换用，持久化到 free.json）。</summary>
    public sealed record FreeConnect(string ProviderId, string ModelId, string? BaseUrl);

    /// <summary>免费可用列表缓存路径（~/.waycoder/free.json，--model free 扫描生成，/free 直接读不重扫）。</summary>
    public static string FreeJsonPath => Global.GlobalConfigPath("free.json");

    /// <summary>保存可用免费 connect 列表到 free.json（--model free 扫描完写入）。</summary>
    public static void SaveFreeJson(List<FreeConnect> items)
    {
        try
        {
            var arr = JNode.Array();
            foreach (var c in items)
                arr.Add(JNode.Object()
                    .Set("providerId", JNode.From(c.ProviderId))
                    .Set("modelId", JNode.From(c.ModelId))
                    .Set("baseUrl", JNode.From(c.BaseUrl ?? "")));
            var root = JNode.Object().Set("free", arr);
            var path = FreeJsonPath;
            Global.EnsureDir(path);
            Global.WriteAllTextAtomic(path, Json.Serialize(root, indent: true)); // 原子替换
        }
        catch { /* 写失败不阻塞 */ }
    }

    /// <summary>读取 free.json 缓存的免费可用 connect 列表（空 = 尚未扫描生成）。</summary>
    public static List<FreeConnect> LoadFreeJson()
    {
        var result = new List<FreeConnect>();
        try
        {
            var path = FreeJsonPath;
            if (!File.Exists(path)) return result;
            var root = Json.Parse(File.ReadAllText(path));
            if (root is not { Kind: JKind.Object }) return result;
            var arr = root["free"];
            if (arr == null) return result;
            foreach (var item in arr.Items)
            {
                var pid = item["providerId"]?.AsString();
                var mid = item["modelId"]?.AsString();
                if (string.IsNullOrEmpty(pid) || string.IsNullOrEmpty(mid)) continue;
                result.Add(new FreeConnect(pid, mid, item["baseUrl"]?.AsString()));
            }
        }
        catch { /* 损坏静默忽略 */ }
        return result;
    }

    /// <summary>
    /// 测试模型库中所有 free 模型（id 含 free 的 openrouter :free / opencode zen -free），列出可用的，
    /// 并把可用列表写入 free.json（/free 之后直接读缓存弹窗，不再每次扫描）。
    /// </summary>
    public static string Free(string? timeoutArg = null)
    {
        var freeModels = EnumerateFreeModels();
        if (freeModels.Count == 0)
            return "模型库中没有 free 模型（--model import online opencode-zen / openrouter 导入后测试）";
        // 默认每模型 5s：没回复就跳过（连通性探测，不拖沓）。可传参覆盖（--model free <秒>）
        var timeout = ParseTimeout(timeoutArg, fallback: 5);
        var sb = new StringBuilder($"Free 模型连通性测试（{freeModels.Count} 个，单模型超时 {timeout}s，没回复即跳过）：\n");
        int ok = 0, fail = 0, skip = 0, total = 0;
        var okList = new List<FreeConnect>();
        foreach (var m in freeModels)
        {
            total++;
            var key = ApiKeyStore.Get(m.ProviderId);
            if (string.IsNullOrEmpty(key))
            {
                skip++;
                sb.AppendLine($"  ⏭ {ModelCatalog.ShortDisplayName(m.Id)}（{ModelCatalog.ProviderDisplayName(m.ProviderId)}）无 key");
                continue;
            }
            // 实时进度（stderr）：让用户知道正在扫哪个 provider 第几个
            Console.Error.WriteLine($"正在扫描 [{m.ProviderId}] 第 {total}/{freeModels.Count} 个（{ModelCatalog.ShortDisplayName(m.Id)}）...");
            var baseUrl = m.DefaultBaseUrl ?? ConnectionConfig.ResolveProvider(m.ProviderId)?.BaseUrl ?? "";
            var (ok2, detail) = ProbeChat(m.ProviderId, m.Id, baseUrl, timeout);
            if (ok2)
            {
                ok++;
                okList.Add(new FreeConnect(m.ProviderId, m.Id, string.IsNullOrEmpty(baseUrl) ? null : baseUrl));
                // 增量持久化：每扫到可用立即写 free.json——28 个模型逐个探测可能较久，
                // 中途 Ctrl+C / 超时也能用已扫到的可用项（下次 /free 直接读缓存）
                SaveFreeJson(okList);
                sb.AppendLine($"  ✅ {ModelCatalog.ShortDisplayName(m.Id)}（{ModelCatalog.ProviderDisplayName(m.ProviderId)}）{detail}");
            }
            else { fail++; sb.AppendLine($"  ❌ {ModelCatalog.ShortDisplayName(m.Id)}（{ModelCatalog.ProviderDisplayName(m.ProviderId)}）{detail}"); }
        }
        if (okList.Count > 0) SaveFreeJson(okList); // 最终全量（幂等）
        sb.AppendLine($"\n汇总：可用 {ok}　失败 {fail}　跳过(无key) {skip}");
        if (okList.Count > 0)
            sb.AppendLine($"已把 {okList.Count} 个可用免费模型写入 free.json（/free 直接读缓存，不再重复扫描；重新扫描跑 --model free）");
        return sb.ToString();
    }

    /// <summary>
    /// 清理脏数据：
    /// 1. 合并重复模型（同 id + baseUrl 只保留一个）
    /// 2. 删除「不存在的服务商」模型（ProviderId 不在注册表）
    /// 3. 删除「不存在的模型」connect（模型在目录中找不到）
    /// </summary>
}
