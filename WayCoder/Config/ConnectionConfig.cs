namespace WayCoder;

/// <summary>
/// 连接方案（ConnectionConfig）—— 把「服务商 + 模型」组织成可整体切换的命名配置。
///
/// 三层模型：
///   · provider   = { providerName, baseUrl, apikey } —— 逻辑一体、物理分文件
///                  （name+base_url 在 providers.json，apikey 在 api_keys.json，代码组合成一个 ProviderRecord）。
///   · connect    = { providerId, modelId }            —— 命名注册表条目，大模型/小模型各是一个 connect。
///   · connection = { 大 connect 名, 小 connect 名 }    —— 命名连接，切换连接时大/小 connect 一起切换。
///   · fallbackChain = 一串 connect 名（全局一条）—— 回退链也是 connect 切换。
///
/// 「每次切换模型 = 切换 connect」：ApplyModelChoice / SetActiveConnect 是统一入口，
/// 选中模型后把当前激活连接的大/小 connect 指向它，并同步 Config 扁平字段（Model/SmallModel/
/// Provider/SmallProvider/BaseUrl）作为运行时镜像，最后持久化到 config.json/.env。
///
/// 保存到全局 ~/.waycoder/connections.json（跨项目可用），顶层分类：
///   { "active", "connects": [{name,providerId,modelId}], "connections": [{name,big,small}], "fallbackChain": [connect名] }
/// 兼容迁移：旧格式 {name,providerId,largeModel,smallModel}、Config 扁平字段、FallbackChain 模型名串。
/// </summary>
public static class ConnectionConfig
{
    // ════════════════════════════════════════════════════════════
    // 数据模型：provider / connect / connection 三层
    // ════════════════════════════════════════════════════════════

    /// <summary>服务商（逻辑一体）：name+base_url 来自 providers.json（ModelCatalog.Providers），apikey 来自 api_keys.json（ApiKeyStore）。</summary>
    public sealed record ProviderRecord(string Name, string BaseUrl, string ApiKey);

    /// <summary>命名 connect = { providerId, modelId }。大模型/小模型各是一个 connect。</summary>
    public sealed record Connect(string Name, string ProviderId, string ModelId);

    /// <summary>命名连接 = 大 connect 名 + 小 connect 名。切换连接时大小一起切换（可不同服务商）。</summary>
    public sealed record Connection(string Name, string BigConnect, string SmallConnect);

    private static readonly object _lock = new();
    private static List<Connect>? _connects;
    private static List<Connection>? _connections;
    private static List<string> _fallback = [];
    private static string _active = "";

    /// <summary>测试用路径覆盖（自测把 connections.json 重定向到临时文件，避免污染真实全局配置）。</summary>
    internal static string? FilePathOverride { get; set; }

    private static string FilePath => FilePathOverride ?? Global.GlobalConfigPath("connections.json");

    /// <summary>connect 默认名（可逆、稳定）："{providerId}/{modelId}"。用户可另加友好名。</summary>
    public static string DefaultConnectName(string providerId, string modelId) =>
        $"{providerId.Trim().ToLowerInvariant()}/{modelId.Trim()}";

    // ════════════════════════════════════════════════════════════
    // connects 区
    // ════════════════════════════════════════════════════════════

    /// <summary>全部 connect（无则空表）</summary>
    public static IReadOnlyList<Connect> ListConnects() { Load(); return _connects!; }

    /// <summary>按名查 connect，不存在返回 null（忽略大小写）</summary>
    public static Connect? FindConnect(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        Load();
        return _connects!.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>按内容（providerId+modelId）查 connect，不存在返回 null</summary>
    public static Connect? FindConnectByContent(string providerId, string modelId)
    {
        Load();
        var pid = (providerId ?? "").Trim().ToLowerInvariant();
        var mid = (modelId ?? "").Trim();
        if (pid.Length == 0 || mid.Length == 0) return null;
        return _connects!.FirstOrDefault(c =>
            c.ProviderId.Equals(pid, StringComparison.OrdinalIgnoreCase)
            && string.Equals(c.ModelId, mid, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>按模型 ID 反查 connect（精确匹配优先，其次子串）。供 WithModelOverrideAsync 跨 provider 解析。</summary>
    public static Connect? FindConnectByModel(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId)) return null;
        Load();
        return _connects!.FirstOrDefault(c => string.Equals(c.ModelId, modelId, StringComparison.OrdinalIgnoreCase))
            ?? _connects!.FirstOrDefault(c => c.ModelId.IndexOf(modelId, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    /// <summary>新增 connect。name 必须唯一（忽略大小写），providerId/modelId 不能为空。成功返回 true。</summary>
    public static bool AddConnect(string name, string providerId, string modelId, out string error)
    {
        if (string.IsNullOrWhiteSpace(name)) { error = "connect 名不能为空"; return false; }
        if (string.IsNullOrWhiteSpace(providerId)) { error = "providerId 不能为空"; return false; }
        if (string.IsNullOrWhiteSpace(modelId)) { error = "modelId 不能为空"; return false; }
        lock (_lock)
        {
            Load();
            var trimmed = name.Trim();
            if (_connects!.Any(c => string.Equals(c.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                error = $"connect「{trimmed}」已存在（名称需唯一）";
                return false;
            }
            _connects!.Add(new Connect(trimmed, providerId.Trim().ToLowerInvariant(), modelId.Trim()));
            Save();
            error = "";
            return true;
        }
    }

    /// <summary>按名查（不存在则自动注册）一个 connect，默认名 "{providerId}/{modelId}"。</summary>
    public static Connect FindOrCreateConnect(string providerId, string modelId)
    {
        var pid = (providerId ?? "").Trim().ToLowerInvariant();
        var mid = (modelId ?? "").Trim();
        lock (_lock)
        {
            Load();
            return FindOrCreateConnectCore(pid, mid);
        }
    }

    /// <summary>删除 connect。被命名连接/回退链引用的不可删。返回是否删除成功。</summary>
    public static bool RemoveConnect(string name, out string error)
    {
        lock (_lock)
        {
            Load();
            var idx = _connects!.FindIndex(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
            {
                error = $"未找到 connect「{name}」。用 /connect connect list 查看全部。";
                return false;
            }
            var removed = _connects[idx].Name;
            if (_connections!.Any(c =>
                    c.BigConnect.Equals(removed, StringComparison.OrdinalIgnoreCase)
                    || c.SmallConnect.Equals(removed, StringComparison.OrdinalIgnoreCase)))
            {
                error = $"connect「{removed}」正被命名连接引用，先移除引用再删";
                return false;
            }
            _connects.RemoveAt(idx);
            _fallback.RemoveAll(n => n.Equals(removed, StringComparison.OrdinalIgnoreCase));
            Save();
            error = "";
            return true;
        }
    }

    // ════════════════════════════════════════════════════════════
    // connections 区（命名连接 = 大 connect + 小 connect）
    // ════════════════════════════════════════════════════════════

    /// <summary>全部命名连接（无则空表）</summary>
    public static IReadOnlyList<Connection> ListConnections() { Load(); return _connections!; }

    /// <summary>当前激活的连接名（未激活或已删除为空串）</summary>
    public static string ActiveName { get { Load(); return _active; } }

    /// <summary>按名称查连接，不存在返回 null（忽略大小写）</summary>
    public static Connection? FindConnection(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        Load();
        return _connections!.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 新增命名连接。name 必须唯一；big/small 为已存在的 connect 名。
    /// 返回 true；失败返回 false 并给 error。
    /// </summary>
    public static bool AddConnection(string name, string bigConnect, string smallConnect, out string error)
    {
        if (string.IsNullOrWhiteSpace(name)) { error = "连接名不能为空"; return false; }
        if (string.IsNullOrWhiteSpace(bigConnect) || string.IsNullOrWhiteSpace(smallConnect))
        {
            error = "大/小 connect 均不能为空";
            return false;
        }
        lock (_lock)
        {
            Load();
            var trimmed = name.Trim();
            if (_connections!.Any(c => string.Equals(c.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                error = $"连接「{trimmed}」已存在（名称需唯一）";
                return false;
            }
            if (FindConnect(bigConnect) == null || FindConnect(smallConnect) == null)
            {
                error = $"connect 不存在：{bigConnect} / {smallConnect}（先 /connect connect add）";
                return false;
            }
            _connections!.Add(new Connection(trimmed, bigConnect.Trim(), smallConnect.Trim()));
            Save();
            error = "";
            return true;
        }
    }

    /// <summary>删除连接（按名称，忽略大小写）。返回是否删除成功。</summary>
    public static bool RemoveConnection(string name, out string error)
    {
        lock (_lock)
        {
            Load();
            var idx = _connections!.FindIndex(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            if (idx < 0)
            {
                error = $"未找到连接「{name}」。用 /connect list 查看全部。";
                return false;
            }
            var removedName = _connections[idx].Name;
            _connections.RemoveAt(idx);
            if (string.Equals(_active, removedName, StringComparison.OrdinalIgnoreCase))
                _active = "";
            Save();
            error = "";
            return true;
        }
    }

    /// <summary>
    /// 激活连接：把连接的大/小 connect 解析出的 provider + 模型写入全局 Config
    /// （Provider / SmallProvider / Model / SmallModel / BaseUrl），并持久化到 config.json/.env。
    /// 切换连接 = 大/小 connect 一起切换（可不同服务商）。返回激活后的连接；失败返回 null 并给 message。
    /// 调用方（/connect 命令）再据此重配运行时 LLM。
    /// </summary>
    public static Connection? ActivateConnection(string name, out string message)
    {
        var c = FindConnection(name);
        if (c == null)
        {
            message = $"未找到连接「{name}」。用 /connect list 查看全部。";
            return null;
        }
        var big = FindConnect(c.BigConnect);
        var small = FindConnect(c.SmallConnect);
        if (big == null || small == null)
        {
            message = $"连接「{name}」引用的 connect 缺失：{c.BigConnect} / {c.SmallConnect}（用 /connect connect add 补建）";
            return null;
        }

        var cfg = Config.Instance;
        cfg.Provider = big.ProviderId;
        cfg.SmallProvider = small.ProviderId;      // 大/小可不同服务商
        cfg.Model = big.ModelId;
        cfg.SmallModel = small.ModelId;
        cfg.BaseUrl = ResolveBaseUrl(big.ProviderId); // 主 LLM 走大 connect 的 provider 地址

        // 持久化：config.json（权威源）+ .env 基本引导
        cfg.SaveToConfigJson();
        cfg.SaveToEnvFile();

        lock (_lock) { _active = c.Name; Save(); }
        message = $"已切换至 连接「{c.Name}」：大={FormatModel(big.ProviderId, big.ModelId)} · 小={FormatModel(small.ProviderId, small.ModelId)}" +
            (string.IsNullOrEmpty(cfg.BaseUrl) ? "" : $" / {cfg.BaseUrl}");
        return c;
    }

    /// <summary>
    /// 「每次切换模型 = 切换 connect」统一入口：按 (providerId, modelId) 找（或自动注册）一个 connect，
    /// 设为当前激活连接的大/小 connect，同步扁平 Config 并持久化。
    /// 供 ModelPicker / ModelCli / WebChat / Program 共用。
    /// </summary>
    public static void ApplyModelChoice(string providerId, string modelId, bool isLarge, out string message, string? baseUrl = null)
    {
        var pid = (providerId ?? "").Trim().ToLowerInvariant();
        var mid = (modelId ?? "").Trim();
        if (pid.Length == 0) { message = "providerId 不能为空"; return; }
        if (mid.Length == 0) { message = "modelId 不能为空"; return; }
        var conn = FindOrCreateConnect(pid, mid);
        SetActiveConnect(isLarge, conn.Name, out message);
        // 显式 baseUrl（所选模型自带的地址）覆盖 provider 注册表推导
        if (isLarge && !string.IsNullOrWhiteSpace(baseUrl))
        {
            var cfg = Config.Instance;
            cfg.BaseUrl = baseUrl.Trim();
            cfg.SaveToConfigJson();
            cfg.SaveToEnvFile();
        }
        // 无 key 时尝试从 provider 官方环境变量自动复制（如 DEEPSEEK_API_KEY → api_keys.json），校验防误复制
        if (!ApiKeyStore.Has(pid))
            message += AutoImportKeyFromEnv(pid);
    }

    /// <summary>
    /// 该 provider 无 key 时，从官方环境变量（ApiKeyEnvVar）自动导入到 api_keys.json。
    /// 返回提示消息（导入成功 / 值不像是 key）；无动作返回 null。
    /// 调用方：ApplyModelChoice（选择模型）与 AddConnect（新建 connect）共用。
    /// </summary>
    public static string? AutoImportKeyFromEnv(string providerId)
    {
        var pid = (providerId ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(pid) || ApiKeyStore.Has(pid)) return null;
        if (!ModelCatalog.Providers.TryGetValue(pid, out var prov)
            || string.IsNullOrWhiteSpace(prov.ApiKeyEnvVar))
            return null;
        var envKey = Environment.GetEnvironmentVariable(prov.ApiKeyEnvVar);
        if (string.IsNullOrWhiteSpace(envKey)) return null;
        if (IsPlausibleApiKey(envKey))
        {
            ApiKeyStore.Set(pid, envKey);
            return $" ✅ 已自动从环境变量 {prov.ApiKeyEnvVar} 复制 API key";
        }
        return $" ⚠️ 环境变量 {prov.ApiKeyEnvVar} 存在但值不像是 key（可能设错了变量名），未自动导入";
    }

    /// <summary>
    /// 判断字符串是否「像真正的 API key」：非空、去首尾空白后 ≥8 字符、无内部空白、非 URL。
    /// 防止从环境变量误复制（用户可能把 URL / 占位文本 / 别的变量值设进了官方变量名）。
    /// </summary>
    public static bool IsPlausibleApiKey(string value)
    {
        var s = value.Trim();
        if (s.Length < 8) return false;
        if (s.Any(char.IsWhiteSpace)) return false;
        // URL 特征（http:// 或 host/path）：key 不应含
        if (s.Contains("://") || (s.Contains('/') && s.Contains('.'))) return false;
        return true;
    }

    /// <summary>
    /// 解析 `/connect &lt;spec&gt;` 一键切换指令。spec 可为：
    ///   connectId          — 已保存的 connect 名
    ///   providerId.modelId — 服务商.模型（`.` 或 `/` 分隔都支持，provider 须为已注册服务商）
    ///   baseUrl:model      — 地址:模型（自定义/本地端点）
    ///   modelId            — 裸模型名（从目录解析 provider）
    /// 应用到大模型 connect（isLarge=true）。
    /// </summary>
    public static void ApplySpec(string spec, bool isLarge, out string message)
    {
        var s = (spec ?? "").Trim();
        if (s.Length == 0)
        {
            message = "空指令。用法: /connect <connectId | providerId.modelId | providerId/modelId | baseUrl:model | modelId>";
            return;
        }

        // connect 名：保持命名身份切换
        if (FindConnect(s) != null) { SetActiveConnect(isLarge, s, out message); return; }

        if (!TryParseSpec(s, out var pid, out var mid, out var baseUrl))
        {
            message = $"无法识别指令「{s}」。用法: /connect <connectId | providerId.modelId | providerId/modelId | baseUrl:model | modelId>";
            return;
        }
        if (baseUrl != null) ApplyModelChoice(pid, mid, isLarge, out message, baseUrl);
        else ApplyModelChoice(pid, mid, isLarge, out message);
    }

    /// <summary>
    /// 把 `/connect &lt;spec&gt;` 解析为 (providerId, modelId, baseUrl?)。纯只读逻辑（自测用，不写配置）。
    /// 规则（按序）：connect 名 → baseUrl:model → providerId&lt;.或/&gt;modelId → 裸模型名 → 兜底当前 provider。
    /// 返回 false 仅当 spec 为空。
    /// </summary>
    internal static bool TryParseSpec(string spec, out string providerId, out string modelId, out string? baseUrl)
    {
        providerId = ""; modelId = ""; baseUrl = null;
        var s = (spec ?? "").Trim();
        if (s.Length == 0) return false;

        // 1. connect 名
        var named = FindConnect(s);
        if (named != null)
        {
            providerId = named.ProviderId;
            modelId = named.ModelId;
            return true;
        }

        // 2. baseUrl:model（URL 含 :// 或 localhost:port:model）
        if (s.Contains("://", StringComparison.Ordinal) || s.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase))
        {
            var lastColon = s.LastIndexOf(':');
            if (lastColon > 0 && lastColon < s.Length - 1)
            {
                baseUrl = s[..lastColon].Trim();
                modelId = s[(lastColon + 1)..].Trim();
                providerId = InferProviderFromBaseUrl(baseUrl);
                return true;
            }
        }

        // 3. providerId<. 或 />modelId（两种分隔符都支持；前半段须为已注册服务商，否则当裸模型名处理）
        int sepIdx = -1;
        var dotIdx = s.IndexOf('.');
        var slashIdx = s.IndexOf('/');
        if (dotIdx > 0 && (slashIdx <= 0 || dotIdx < slashIdx)) sepIdx = dotIdx;
        else if (slashIdx > 0) sepIdx = slashIdx;
        if (sepIdx > 0 && sepIdx < s.Length - 1)
        {
            var pid = s[..sepIdx].Trim().ToLowerInvariant();
            var mid = s[(sepIdx + 1)..].Trim();
            if (ModelCatalog.Providers.ContainsKey(pid)
                || ModelCatalog.ByProvider(pid).Length > 0
                || pid is "custom" or "local")
            {
                providerId = pid;
                modelId = mid;
                return true;
            }
        }

        // 4. 裸模型名
        var info = ModelCatalog.Find(s) ?? ModelCatalog.Search(s).FirstOrDefault();
        if (info != null) { providerId = info.ProviderId; modelId = info.Id; return true; }

        // 5. 兜底：当前 provider + 该模型名
        providerId = Config.Instance.Provider;
        modelId = s;
        return true;
    }

    /// <summary>模型栏显示格式：`(provider)model` —— 即使同名模型分属不同服务商也能区分。</summary>
    public static string FormatModel(string providerId, string modelId)
        => string.IsNullOrEmpty(providerId) ? (modelId ?? "") : $"({providerId}){modelId}";

    /// <summary>按 base_url 反查已注册服务商；找不到返回 "custom"。</summary>
    private static string InferProviderFromBaseUrl(string baseUrl)
    {
        var b = (baseUrl ?? "").Trim().TrimEnd('/');
        foreach (var (key, val) in ModelCatalog.Providers)
            if (!string.IsNullOrEmpty(val.DefaultBaseUrl)
                && string.Equals(val.DefaultBaseUrl.Trim().TrimEnd('/'), b, StringComparison.OrdinalIgnoreCase))
                return key;
        return "custom";
    }

    /// <summary>直接把当前激活连接的大/小 connect 切换为指定 connect 名。成功返回 true。</summary>
    public static bool SetActiveConnect(bool isLarge, string connectName, out string message)
    {
        var conn = FindConnect(connectName);
        if (conn == null)
        {
            message = $"未找到 connect「{connectName}」。用 /connect connect list 查看全部。";
            return false;
        }
        var cfg = Config.Instance;
        lock (_lock)
        {
            Load();
            var active = _active.Length > 0
                ? _connections!.FirstOrDefault(c => string.Equals(c.Name, _active, StringComparison.OrdinalIgnoreCase))
                : null;
            if (active == null)
                active = _connections!.FirstOrDefault(c => IsCurrentConnection(c, cfg));
            if (active == null)
            {
                // 自动建一个命名连接：以当前大/小 connect 为基线
                var big = FindConnectByContent(cfg.Provider, cfg.Model) ?? FindOrCreateConnectCore(cfg.Provider, cfg.Model);
                var small = FindConnectByContent(cfg.SmallProvider, cfg.SmallModel) ?? FindOrCreateConnectCore(cfg.SmallProvider, cfg.SmallModel);
                var autoName = "auto";
                int i = 1;
                while (_connections!.Any(x => string.Equals(x.Name, autoName + i, StringComparison.OrdinalIgnoreCase))) i++;
                active = new Connection(autoName + i, big.Name, small.Name);
                _connections!.Add(active);
            }
            var idx = _connections!.FindIndex(x => string.Equals(x.Name, active.Name, StringComparison.OrdinalIgnoreCase));
            var updated = active with
            {
                BigConnect = isLarge ? conn.Name : active.BigConnect,
                SmallConnect = isLarge ? active.SmallConnect : conn.Name,
            };
            if (idx >= 0) _connections[idx] = updated;
            _active = updated.Name;
            Save();
        }

        // 同步扁平字段（运行时镜像） + 持久化
        cfg.Model = isLarge ? conn.ModelId : cfg.Model;
        cfg.SmallModel = isLarge ? cfg.SmallModel : conn.ModelId;
        if (isLarge) { cfg.Provider = conn.ProviderId; cfg.BaseUrl = ResolveBaseUrl(conn.ProviderId); }
        else { cfg.SmallProvider = conn.ProviderId; }
        cfg.SaveToConfigJson();
        cfg.SaveToEnvFile();

        message = $"已切换至 {FormatModel(conn.ProviderId, conn.ModelId)} 模型";
        return true;
    }

    /// <summary>当前全局 Config 命中的命名连接（大/小 connect 完全一致才视为当前），无则 null。</summary>
    public static Connection? CurrentByConfig()
    {
        var cfg = Config.Instance;
        if (string.IsNullOrWhiteSpace(cfg.Provider)) return null;
        return ListConnections().FirstOrDefault(c => IsCurrentConnection(c, cfg));
    }

    /// <summary>判断连接的大/小 connect 是否与全局扁平字段一致（Provider+Model+SmallProvider+SmallModel）。</summary>
    private static bool IsCurrentConnection(Connection c, Config cfg)
    {
        var big = FindConnect(c.BigConnect);
        var small = FindConnect(c.SmallConnect);
        return big != null && small != null
            && big.ProviderId.Equals(cfg.Provider, StringComparison.OrdinalIgnoreCase)
            && big.ModelId.Equals(cfg.Model, StringComparison.OrdinalIgnoreCase)
            && small.ProviderId.Equals(cfg.SmallProvider, StringComparison.OrdinalIgnoreCase)
            && small.ModelId.Equals(cfg.SmallModel, StringComparison.OrdinalIgnoreCase);
    }

    // ════════════════════════════════════════════════════════════
    // 回退链（全局一条，一串 connect 名）
    // ════════════════════════════════════════════════════════════

    /// <summary>全局回退链（connect 名，有序）。空则无回退。</summary>
    public static IReadOnlyList<string> FallbackChain { get { Load(); return _fallback; } }

    /// <summary>
    /// 设置全局回退链（一串 connect 名，去重保序），写入 connections.json 并同步
    /// Config.FallbackChain 字符串镜像（逗号连接名）。
    /// </summary>
    public static void SetFallbackChain(IEnumerable<string> connectNames)
    {
        lock (_lock)
        {
            Load();
            var list = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var n in connectNames)
            {
                var t = (n ?? "").Trim();
                if (t.Length == 0) continue;
                if (FindConnect(t) != null && seen.Add(t)) list.Add(t);
            }
            _fallback = list;
            var cfg = Config.Instance;
            cfg.FallbackChain = string.Join(",", list);
            cfg.SaveToConfigJson();
            cfg.SaveToEnvFile();
            Save();
        }
    }

    // ════════════════════════════════════════════════════════════
    // provider 解析（逻辑一体：providers.json + api_keys.json）
    // ════════════════════════════════════════════════════════════

    /// <summary>解析某 providerId 的默认 base_url（baseUrl 与 providerId 唯一绑定）：
    /// 服务商注册表地址优先，其次该服务商下首个模型的默认地址，最后 null。</summary>
    public static string? ResolveBaseUrl(string providerId)
    {
        var pid = (providerId ?? "").Trim().ToLowerInvariant();
        if (pid.Length == 0) return null;
        if (ModelCatalog.Providers.TryGetValue(pid, out var p) && !string.IsNullOrWhiteSpace(p.DefaultBaseUrl))
            return p.DefaultBaseUrl;
        var m = ModelCatalog.All.FirstOrDefault(x => x.ProviderId.Equals(pid, StringComparison.OrdinalIgnoreCase));
        return m?.DefaultBaseUrl;
    }

    /// <summary>解析某 providerId 的完整 ProviderRecord（name / base_url / apikey 逻辑一体）。未知返回 null。</summary>
    public static ProviderRecord? ResolveProvider(string providerId)
    {
        var pid = (providerId ?? "").Trim().ToLowerInvariant();
        if (pid.Length == 0) return null;
        var name = ModelCatalog.Providers.TryGetValue(pid, out var p) && !string.IsNullOrWhiteSpace(p.DisplayName)
            ? p.DisplayName : pid;
        var baseUrl = ResolveBaseUrl(pid) ?? "";
        var apiKey = ApiKeyStore.Get(pid) ?? "";
        return new ProviderRecord(name, baseUrl, apiKey);
    }

    /// <summary>解析某 connect 的 provider（供 LLM 重配 key/baseUrl）。connect 不存在返回 null。</summary>
    public static ProviderRecord? ResolveProviderForConnect(string connectName)
    {
        var c = FindConnect(connectName);
        return c != null ? ResolveProvider(c.ProviderId) : null;
    }

    // ════════════════════════════════════════════════════════════
    // 内部 helpers
    // ════════════════════════════════════════════════════════════

    private static Connect FindOrCreateConnectCore(string providerId, string modelId)
    {
        var pid = (providerId ?? "").Trim().ToLowerInvariant();
        var mid = (modelId ?? "").Trim();
        var def = DefaultConnectName(pid, mid);
        var c = _connects!.FirstOrDefault(x => string.Equals(x.Name, def, StringComparison.OrdinalIgnoreCase));
        if (c != null) return c;
        c = _connects!.FirstOrDefault(x => x.ProviderId.Equals(pid, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.ModelId, mid, StringComparison.OrdinalIgnoreCase));
        if (c != null) return c;
        c = new Connect(def, pid, mid);
        _connects!.Add(c);
        return c;
    }

    /// <summary>清除缓存（测试用）</summary>
    public static void ClearCache() { lock (_lock) { _connects = null; _connections = null; _fallback = []; _active = ""; } }

    // ════════════════════════════════════════════════════════════
    // 持久化（手写 JNode 序列化，零反射，AOT 安全）+ 迁移
    // ════════════════════════════════════════════════════════════

    private static void Load()
    {
        if (_connects != null) return;
        lock (_lock)
        {
            if (_connects != null) return;
            var connects = new List<Connect>();
            var connections = new List<Connection>();
            var fallback = new List<string>();
            string active = "";
            var legacy = new List<(string Name, string Pid, string Large, string Small)>();

            try
            {
                if (File.Exists(FilePath))
                {
                    var root = Json.Parse(File.ReadAllText(FilePath));
                    if (root is { Kind: JKind.Object })
                    {
                        active = root["active"]?.AsString() ?? "";
                        if (root["connects"] is { Kind: JKind.Array } carr)
                        {
                            foreach (var node in carr.Items)
                            {
                                if (node.Kind != JKind.Object) continue;
                                var name = node["name"]?.AsString();
                                var pid = node["providerId"]?.AsString() ?? node["provider"]?.AsString();
                                var mid = node["modelId"]?.AsString() ?? node["model"]?.AsString();
                                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(pid) && !string.IsNullOrWhiteSpace(mid))
                                    connects.Add(new Connect(name.Trim(), pid.Trim().ToLowerInvariant(), mid.Trim()));
                            }
                        }
                        if (root["connections"] is { Kind: JKind.Array } arr)
                        {
                            foreach (var node in arr.Items)
                            {
                                if (node.Kind != JKind.Object) continue;
                                var name = node["name"]?.AsString();
                                var big = node["big"]?.AsString() ?? node["bigConnect"]?.AsString() ?? node["large"]?.AsString();
                                var small = node["small"]?.AsString() ?? node["smallConnect"]?.AsString();
                                var pid = node["providerId"]?.AsString();
                                var largeModel = node["largeModel"]?.AsString();
                                var smallModel = node["smallModel"]?.AsString();
                                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(big) && !string.IsNullOrWhiteSpace(small))
                                    connections.Add(new Connection(name.Trim(), big.Trim(), small.Trim()));
                                else if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(pid)
                                    && !string.IsNullOrWhiteSpace(largeModel) && !string.IsNullOrWhiteSpace(smallModel))
                                    legacy.Add((name.Trim(), pid.Trim().ToLowerInvariant(), largeModel.Trim(), smallModel.Trim()));
                            }
                        }
                        if (root["fallbackChain"] is { Kind: JKind.Array } farr)
                        {
                            foreach (var node in farr.Items)
                            {
                                var n = node?.AsString();
                                if (!string.IsNullOrWhiteSpace(n)) fallback.Add(n.Trim());
                            }
                        }
                    }
                }
            }
            catch { /* 损坏/截断文件按空处理 */ }

            _connects = connects;
            _connections = connections;
            _fallback = fallback;
            _active = active;
            MigrateIfNeeded(legacy);
        }
    }

    /// <summary>
    /// 首次加载的迁移：
    /// 1. 旧 connections.json（{name,providerId,largeModel,smallModel}）→ 自动注册 connect + 转换命名连接。
    /// 2. 当前 Config 扁平字段 seed 大/小 connect。
    /// 3. Config.FallbackChain（模型名串）→ 解析为 connect 名，回写镜像。
    /// 4. active 命中最匹配连接；无连接则自动建默认。
    /// </summary>
    private static void MigrateIfNeeded(List<(string Name, string Pid, string Large, string Small)> legacy)
    {
        var cfg = Config.Instance;
        var changed = false;

        // 1. 旧连接格式迁移
        foreach (var (name, pid, large, small) in legacy)
        {
            if (FindConnectCore(name) != null || _connections!.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))) continue;
            var big = FindOrCreateConnectCore(pid, large);
            var smallConn = FindOrCreateConnectCore(pid, small);
            _connections!.Add(new Connection(name, big.Name, smallConn.Name));
            changed = true;
        }

        // 2. 当前扁平字段 seed 大/小 connect（无任何 connect 时）
        if (_connects!.Count == 0 && !string.IsNullOrWhiteSpace(cfg.Model))
        {
            FindOrCreateConnectCore(cfg.Provider, cfg.Model);
            FindOrCreateConnectCore(cfg.SmallProvider, cfg.SmallModel);
            changed = true;
        }

        // 3. 回退链：Config.FallbackChain 模型名 → connect 名
        if (_fallback.Count == 0 && !string.IsNullOrWhiteSpace(cfg.FallbackChain))
        {
            foreach (var token in cfg.FallbackChain.Split(','))
            {
                var t = token.Trim();
                if (t.Length == 0) continue;
                if (FindConnectCore(t) != null) { _fallback.Add(t); continue; }
                var info = ModelCatalog.Find(t);
                var conn = info != null
                    ? FindOrCreateConnectCore(info.ProviderId, info.Id)
                    : FindOrCreateConnectCore(cfg.Provider, t);
                _fallback.Add(conn.Name);
            }
            if (_fallback.Count > 0)
            {
                cfg.FallbackChain = string.Join(",", _fallback);
                cfg.SaveToConfigJson();
                cfg.SaveToEnvFile();
                changed = true;
            }
        }

        // 4. 命名连接 seed + active
        if (_connections!.Count == 0 && !string.IsNullOrWhiteSpace(cfg.Model))
        {
            var big = FindOrCreateConnectCore(cfg.Provider, cfg.Model);
            var small = FindOrCreateConnectCore(cfg.SmallProvider, cfg.SmallModel);
            _connections.Add(new Connection("default", big.Name, small.Name));
            if (_active.Length == 0) _active = "default";
            changed = true;
        }
        if (_active.Length == 0 && _connections.Count > 0)
        {
            var cur = CurrentByConfig();
            _active = cur?.Name ?? _connections[0].Name;
            changed = true;
        }
        // active 指向已删除的连接 → 清空
        if (_active.Length > 0 && !_connections.Any(c => string.Equals(c.Name, _active, StringComparison.OrdinalIgnoreCase)))
        {
            _active = _connections.Count > 0 ? _connections[0].Name : "";
            changed = true;
        }

        if (changed) Save();
    }

    private static Connect? FindConnectCore(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return _connects!.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (dir != null) Directory.CreateDirectory(dir);

            var connectsArr = JNode.Array();
            foreach (var c in _connects!)
                connectsArr.Add(JNode.Object()
                    .Set("name", c.Name)
                    .Set("providerId", c.ProviderId)
                    .Set("modelId", c.ModelId));

            var connsArr = JNode.Array();
            foreach (var c in _connections!)
                connsArr.Add(JNode.Object()
                    .Set("name", c.Name)
                    .Set("big", c.BigConnect)
                    .Set("small", c.SmallConnect));

            var fbArr = JNode.Array();
            foreach (var n in _fallback)
                fbArr.Add(JNode.From(n));

            var root = JNode.Object()
                .Set("active", _active)
                .Set("connects", connectsArr)
                .Set("connections", connsArr)
                .Set("fallbackChain", fbArr);

            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, Json.Serialize(root, indent: true));
            File.Move(tmp, FilePath, overwrite: true); // 同卷原子替换
        }
        catch { /* 写失败不崩溃 */ }
    }
}
