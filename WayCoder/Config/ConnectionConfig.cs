using System.Text;

namespace WayCoder;

/// <summary>
/// 连接方案（Connection）—— 把「服务商 + 大模型 + 小模型」打包成一套可整体切换的命名配置。
/// 每个连接 = { 名称, providerId, 大模型, 小模型 }。
/// 切换连接时，Config.Provider / SmallProvider / Model / SmallModel / BaseUrl 一起切换；
/// 而 baseUrl 与 apiKey 不单独存储 —— 它们与 providerId 唯一绑定：
///   · apiKey  → ApiKeyStore（一个服务商一个 key，key 跟服务商走）
///   · baseUrl → ModelCatalog.Providers[providerId] 的默认地址
/// 连接只负责「选哪套 provider+大小模型」，连接细节（key/地址）由 providerId 自动解析，
/// 避免之前「大模型/小模型/服务商/地址/密钥」散落在多个平铺配置项里、切换不同步。
/// 保存到全局 ~/.waycoder/connections.json（跨项目可用）。
/// </summary>
public static class ConnectionConfig
{
    /// <summary>单个连接：名称 + 服务商 + 大模型 + 小模型（baseUrl/apiKey 由 providerId 唯一绑定，不在此存储）。</summary>
    public sealed record Connection(string Name, string ProviderId, string LargeModel, string SmallModel);

    private static readonly object _lock = new();
    private static List<Connection>? _cache;
    private static string _active = "";

    private static string FilePath => Global.GlobalConfigPath("connections.json");

    /// <summary>全部连接（无则空表）</summary>
    public static IReadOnlyList<Connection> List() { Load(); return _cache!; }

    /// <summary>当前激活的连接名（未激活或已删除为空串）</summary>
    public static string ActiveName { get { Load(); return _active; } }

    /// <summary>按名称查连接，不存在返回 null（忽略大小写）</summary>
    public static Connection? Find(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        Load();
        return _cache!.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 新增连接。name 必须唯一（忽略大小写），providerId 与两个模型均不能为空。
    /// 返回 true；失败返回 false 并给 error。
    /// </summary>
    public static bool Add(string name, string providerId, string largeModel, string smallModel, out string error)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "连接名不能为空";
            return false;
        }
        if (string.IsNullOrWhiteSpace(providerId))
        {
            error = "providerId 不能为空";
            return false;
        }
        if (string.IsNullOrWhiteSpace(largeModel) || string.IsNullOrWhiteSpace(smallModel))
        {
            error = "大模型与小模型均不能为空";
            return false;
        }

        lock (_lock)
        {
            Load();
            var trimmed = name.Trim();
            if (_cache!.Any(c => string.Equals(c.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                error = $"连接「{trimmed}」已存在（名称需唯一）";
                return false;
            }
            _cache!.Add(new Connection(trimmed, providerId.Trim().ToLowerInvariant(),
                largeModel.Trim(), smallModel.Trim()));
            Save();
            error = "";
            return true;
        }
    }

    /// <summary>删除连接（按名称，忽略大小写）。返回是否删除成功。</summary>
    public static bool Remove(string name, out string error)
    {
        lock (_lock)
        {
            Load();
            var idx = _cache!.FindIndex(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
            {
                error = $"未找到连接「{name}」。用 /connect list 查看全部。";
                return false;
            }
            var removedName = _cache[idx].Name;
            _cache.RemoveAt(idx);
            if (string.Equals(_active, removedName, StringComparison.OrdinalIgnoreCase))
                _active = "";
            Save();
            error = "";
            return true;
        }
    }

    /// <summary>
    /// 激活连接：把连接的服务商 + 大模型 + 小模型写入全局 Config（Provider / SmallProvider / Model /
    /// SmallModel / BaseUrl），并持久化到 config.json/.env。返回激活后的连接；失败返回 null 并给 message。
    /// 调用方（/connect 命令）再据此重配运行时 LLM。
    /// </summary>
    public static Connection? Activate(string name, out string message)
    {
        var c = Find(name);
        if (c == null)
        {
            message = $"未找到连接「{name}」。用 /connect list 查看全部。";
            return null;
        }

        var cfg = Config.Instance;
        cfg.Provider = c.ProviderId;
        cfg.SmallProvider = c.ProviderId;          // 连接是单服务商：大小模型共用同一 provider（key/地址唯一绑定）
        cfg.Model = c.LargeModel;
        cfg.SmallModel = c.SmallModel;
        cfg.BaseUrl = ResolveBaseUrl(c.ProviderId); // baseUrl 由 providerId 自动推导

        // 持久化：config.json（权威源）+ .env 基本引导
        cfg.SaveToConfigJson();
        cfg.SaveToEnvFile();

        lock (_lock) { _active = c.Name; Save(); }
        message = $"已切换到连接「{c.Name}」：{c.ProviderId} / 大={c.LargeModel} / 小={c.SmallModel}" +
            (string.IsNullOrEmpty(cfg.BaseUrl) ? "" : $" / {cfg.BaseUrl}");
        return c;
    }

    /// <summary>
    /// 解析某 providerId 的默认 base_url（baseUrl 与 providerId 唯一绑定）：
    /// 服务商注册表地址优先，其次该服务商下首个模型的默认地址，最后 null。
    /// </summary>
    public static string? ResolveBaseUrl(string providerId)
    {
        var pid = (providerId ?? "").Trim().ToLowerInvariant();
        if (pid.Length == 0) return null;
        if (ModelCatalog.Providers.TryGetValue(pid, out var p) && !string.IsNullOrWhiteSpace(p.DefaultBaseUrl))
            return p.DefaultBaseUrl;
        var m = ModelCatalog.All.FirstOrDefault(x => x.ProviderId.Equals(pid, StringComparison.OrdinalIgnoreCase));
        return m?.DefaultBaseUrl;
    }

    /// <summary>当前全局 Config 命中的连接（Provider+Model+SmallModel 完全一致才视为当前），无则 null。</summary>
    public static Connection? CurrentByConfig()
    {
        var cfg = Config.Instance;
        if (string.IsNullOrWhiteSpace(cfg.Provider)) return null;
        return List().FirstOrDefault(c =>
            string.Equals(c.ProviderId, cfg.Provider, StringComparison.OrdinalIgnoreCase)
            && string.Equals(c.LargeModel, cfg.Model, StringComparison.OrdinalIgnoreCase)
            && string.Equals(c.SmallModel, cfg.SmallModel, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>清除缓存（测试用）</summary>
    public static void ClearCache() { lock (_lock) { _cache = null; _active = ""; } }

    // ════════════════════════════════════════════════════════════
    // 持久化（手写 JNode 序列化，零反射，AOT 安全）
    // ════════════════════════════════════════════════════════════

    private static void Load()
    {
        if (_cache != null) return;
        lock (_lock)
        {
            if (_cache != null) return;
            var list = new List<Connection>();
            try
            {
                if (File.Exists(FilePath))
                {
                    var root = Json.Parse(File.ReadAllText(FilePath));
                    if (root is { Kind: JKind.Object })
                    {
                        _active = root["active"]?.AsString() ?? "";
                        if (root["connections"] is { Kind: JKind.Array } arr)
                        {
                            foreach (var node in arr.Items)
                            {
                                if (node.Kind != JKind.Object) continue;
                                var name = node["name"]?.AsString();
                                var pid = node["providerId"]?.AsString() ?? node["provider"]?.AsString();
                                var large = node["largeModel"]?.AsString() ?? node["model"]?.AsString();
                                var small = node["smallModel"]?.AsString();
                                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(pid)
                                    && !string.IsNullOrWhiteSpace(large) && !string.IsNullOrWhiteSpace(small))
                                    list.Add(new Connection(name, pid.ToLowerInvariant(), large, small));
                            }
                        }
                    }
                }
            }
            catch { /* 损坏/截断文件按空处理 */ }
            _cache = list;
        }
    }

    private static void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (dir != null) Directory.CreateDirectory(dir);

            var arr = JNode.Array();
            foreach (var c in _cache!)
            {
                arr.Add(JNode.Object()
                    .Set("name", c.Name)
                    .Set("providerId", c.ProviderId)
                    .Set("largeModel", c.LargeModel)
                    .Set("smallModel", c.SmallModel));
            }
            var root = JNode.Object()
                .Set("active", _active)
                .Set("connections", arr);

            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, Json.Serialize(root, indent: true));
            File.Move(tmp, FilePath, overwrite: true); // 同卷原子替换
        }
        catch { /* 写失败不崩溃 */ }
    }
}
