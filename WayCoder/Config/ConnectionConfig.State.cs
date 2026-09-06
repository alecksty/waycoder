namespace WayCoder;

// ════════════════════════════════════════════════════════════
// ConnectionConfig 状态块（Phase 1'：连接状态结构化持久化）
//
// 设计原则（用户确认 + 协调约束）：
//   · config.json 不再保存 connect/provider/model 内容；易变模型状态收敛到
//     connections.json 的顶层 "state" 对象（单一权威）。
//   · state.default_connect 是用户主模型【锚点】——只有用户主动「换主模型」
//     （ApplyModelChoice / SetActiveConnect / Config 直接写模型字段后 Save）才改。
//     回退备用、free 临时切换都【不覆盖】default_connect。
//   · state.free_connect = 当前激活的免费模型（切去时记录）。
//   · state.rollback_connect = 进入 free / 换主模型前的主模型快照（/free restore 恢复参考）。
//   · state.connect_mode = "big"（跑 default_connect）| "free"（跑 free_connect）。
//     运行回退链是纯内存（llm.Model + Reconfigure），不落盘 state。
//
// connect 引用格式统一 "{providerId}:{modelId}"。网关地址经 provider 注册表
// （providers.json）解析；仅当实际地址 ≠ 注册表推导（自定义/本地端点）时写
// *_base_url 覆盖字段。
// ════════════════════════════════════════════════════════════

public static partial class ConnectionConfig
{
    /// <summary>state 持久化结构（顶层 "state" 对象）。ConnectMode 为 "big" | "free"。</summary>
    public sealed record ConnectState(
        string ConnectMode,          // "big" | "free"（"rollback" 仅为运行态概念，不落盘）
        string? DefaultConnect,      // "{providerId}:{modelId}" 大模型锚点
        string? SmallConnect,        // "{providerId}:{modelId}" 小模型
        string? FreeConnect,         // 激活的免费模型（mode=free 时有值）
        string? RollbackConnect,     // 进入 free 前 / 换主模型前的主模型快照
        string? DefaultBaseUrl,      // 大模型 baseUrl 覆盖（≠ 注册表推导时才写）
        string? FreeBaseUrl,         // 免费模型 baseUrl 覆盖
        string? RollbackBaseUrl);    // 快照 baseUrl 覆盖

    private static ConnectState? _state;

    /// <summary>本次 Load 是否看到 connections.json 已存在（全新安装判定：首次无文件 → 允许用环境变量引导 state）。</summary>
    private static bool _sawConnectionsFile;

    /// <summary>state 是否已构造（null = 尚未 Load / 老文件尚未迁移）。</summary>
    private static bool StateReady => _state != null;

    /// <summary>确保 connections.json 已加载（含 state 迁移）。幂等。</summary>
    public static void EnsureLoaded() => Load();

    /// <summary>当前 state（先 EnsureLoaded）。老文件首次 Load 会生成并落盘 state。</summary>
    public static ConnectState State
    {
        get { EnsureLoaded(); return _state!; }
    }

    /// <summary>格式化 connect 引用："{providerId}:{modelId}"（provider 小写、model 原样）。</summary>
    public static string FormatConnect(string? providerId, string? modelId)
    {
        var pid = (providerId ?? "").Trim().ToLowerInvariant();
        var mid = (modelId ?? "").Trim();
        return pid.Length == 0 || mid.Length == 0 ? "" : $"{pid}:{mid}";
    }

    /// <summary>解析 connect 引用 "{providerId}:{modelId}" → (pid, mid)。失败返回 false。</summary>
    public static bool TryParseConnect(string? s, out string providerId, out string modelId)
    {
        providerId = ""; modelId = "";
        if (string.IsNullOrWhiteSpace(s)) return false;
        var t = s.Trim();
        var idx = t.IndexOf(':');
        if (idx <= 0 || idx >= t.Length - 1) return false;
        providerId = t[..idx].Trim().ToLowerInvariant();
        modelId = t[(idx + 1)..].Trim();
        return providerId.Length > 0 && modelId.Length > 0;
    }

    // ────────────────────────────────────────────────────────
    // 模型栏通道前缀（跨端统一显示）：
    //   「模型栏只显示当前生效模型，前缀提示通道」——
    //   格式 {通道前缀}:(供应商)模型，如 大模型:(AIHubMix)glm-5.2。
    //   前缀由 connect_mode 决定：big→大模型 / free→自由模型 /
    //   rollback→回滚模型 / small→小模型。
    //   small 前缀用于「小模型通道」显示：小模型作为当前对话模型（部分端支持）时，
    //   或 GUI/Web 等带小模型选择按钮的端用它标注小模型辅助通道。
    // ────────────────────────────────────────────────────────

    /// <summary>connect_mode → 模型栏通道前缀中文标签。未知/空一律按 "big" 处理 → 大模型。</summary>
    public static string ChannelLabel(string? connectMode) => (connectMode ?? "").Trim().ToLowerInvariant() switch
    {
        "free" => "自由模型",
        "rollback" => "回滚模型",
        "small" => "小模型",
        _ => "大模型",
    };

    /// <summary>当前主通道标识：state.connect_mode=free → "free"，否则 "big"（回滚为运行态，由调用方按 LlmClient 实时模型判定）。</summary>
    public static string CurrentMainChannel()
        => string.Equals(State.ConnectMode, "free", StringComparison.OrdinalIgnoreCase) ? "free" : "big";

    /// <summary>模型栏单行文本：`{通道前缀}:(供应商)模型`。providerId 经注册表解析为显示名，
    /// 与 TUI/GUI/Web/MAUI 模型栏既有 `(provider)model` 口径一致；未知服务商显示 (?)model。</summary>
    public static string FormatModelChannel(string? connectMode, string providerId, string modelId)
    {
        var label = FormatModel(ModelCatalog.ProviderDisplayName(providerId), modelId);
        return $"{ChannelLabel(connectMode)}:{label}";
    }

    // ────────────────────────────────────────────────────────
    // 当前生效模型解析（Config.Model getter / 状态栏 / 建 LLM 共用语义）
    // ────────────────────────────────────────────────────────

    /// <summary>当前生效主模型：mode=free 取 free_connect，否则 default_connect。</summary>
    public static string? CurrentMainModel()
    {
        var st = State;
        if (string.Equals(st.ConnectMode, "free", StringComparison.OrdinalIgnoreCase)
            && TryParseConnect(st.FreeConnect, out _, out var freeMid))
            return freeMid;
        return TryParseConnect(st.DefaultConnect, out _, out var mid) ? mid : null;
    }

    /// <summary>当前生效主模型 provider。</summary>
    public static string? CurrentMainProviderId()
    {
        var st = State;
        if (string.Equals(st.ConnectMode, "free", StringComparison.OrdinalIgnoreCase)
            && TryParseConnect(st.FreeConnect, out var freePid, out _))
            return freePid;
        return TryParseConnect(st.DefaultConnect, out var pid, out _) ? pid : null;
    }

    /// <summary>当前生效主模型 baseUrl：覆盖字段优先，其次 provider 注册表。</summary>
    public static string? CurrentMainBaseUrl()
    {
        var st = State;
        if (string.Equals(st.ConnectMode, "free", StringComparison.OrdinalIgnoreCase))
        {
            if (TryParseConnect(st.FreeConnect, out var fpid, out _))
                return !string.IsNullOrEmpty(st.FreeBaseUrl) ? st.FreeBaseUrl : ResolveBaseUrl(fpid);
        }
        else
        {
            if (TryParseConnect(st.DefaultConnect, out var dpid, out _))
                return !string.IsNullOrEmpty(st.DefaultBaseUrl) ? st.DefaultBaseUrl : ResolveBaseUrl(dpid);
        }
        return null;
    }

    /// <summary>当前小模型 model。</summary>
    public static string? CurrentSmallModel()
        => TryParseConnect(State.SmallConnect, out _, out var mid) ? mid : null;

    /// <summary>当前小模型 provider。</summary>
    public static string? CurrentSmallProviderId()
        => TryParseConnect(State.SmallConnect, out var pid, out _) ? pid : null;

    // ────────────────────────────────────────────────────────
    // 主模型快照（rollback_connect）——/free restore 恢复参考
    // ────────────────────────────────────────────────────────

    /// <summary>当前默认锚点（default_connect）内容（free 也不动它）。</summary>
    public static bool TryGetDefaultAnchor(out string provider, out string model, out string? baseUrl)
    {
        provider = ""; model = ""; baseUrl = null;
        var st = State;
        if (!TryParseConnect(st.DefaultConnect, out provider, out model)) return false;
        baseUrl = !string.IsNullOrEmpty(st.DefaultBaseUrl) ? st.DefaultBaseUrl : ResolveBaseUrl(provider);
        return true;
    }

    /// <summary>是否已记录主模型快照（进入 free / 换主前）。</summary>
    public static bool HasRollbackSnapshot() => !string.IsNullOrWhiteSpace(State.RollbackConnect);

    /// <summary>读取主模型快照（进入 free/换主前的主模型）。无则 false。</summary>
    public static bool TryGetRollbackSnapshot(out string provider, out string model, out string? baseUrl)
    {
        provider = ""; model = ""; baseUrl = null;
        var st = State;
        if (!TryParseConnect(st.RollbackConnect, out provider, out model)) return false;
        baseUrl = !string.IsNullOrEmpty(st.RollbackBaseUrl) ? st.RollbackBaseUrl : ResolveBaseUrl(provider);
        return true;
    }

    /// <summary>记录主模型快照（未记录才记由调用方保证）。</summary>
    public static void SetRollbackSnapshot(string provider, string model, string? baseUrl)
    {
        lock (_lock)
        {
            Load();
            var st = State;
            _state = st with
            {
                RollbackConnect = FormatConnect(provider, model),
                RollbackBaseUrl = NormalizeBaseUrlOverride(provider, baseUrl),
            };
            Save();
        }
    }

    /// <summary>清除主模型快照。</summary>
    public static void ClearRollbackSnapshot()
    {
        lock (_lock)
        {
            Load();
            if (string.IsNullOrWhiteSpace(_state!.RollbackConnect)) return;
            _state = _state with { RollbackConnect = null, RollbackBaseUrl = null };
            Save();
        }
    }

    /// <summary>把 state 解析结果同步到 Config 模型字段镜像（保持全项目 ~64 处读点语义不变）。</summary>
    public static void SyncToConfig(Config cfg)
    {
        var st = State;
        var mainPid = CurrentMainProviderId();
        var mainMid = CurrentMainModel();
        if (!string.IsNullOrEmpty(mainPid)) cfg.Provider = mainPid;
        if (!string.IsNullOrEmpty(mainMid)) cfg.Model = mainMid;
        cfg.BaseUrl = CurrentMainBaseUrl();          // 覆盖 ?? 注册表
        var smallPid = CurrentSmallProviderId();
        var smallMid = CurrentSmallModel();
        if (!string.IsNullOrEmpty(smallPid)) cfg.SmallProvider = smallPid;
        if (!string.IsNullOrEmpty(smallMid)) cfg.SmallModel = smallMid;
    }

    // ────────────────────────────────────────────────────────
    // 统一写入口：用户主动切换主/小/免费模型
    //   mode: "big" 换主模型（更新 default_connect + 激活连接大 connect）
    //         "small" 换小模型
    //         "free" 临时切免费模型（不覆盖 default_connect，只记 free_connect + mode）
    //         "restore" 恢复 /free 前的模型
    //   回退链消费【禁止】调用本方法（纯内存，见 Agent/FallbackLLM + Program.Repl）。
    // ────────────────────────────────────────────────────────

    /// <summary>按 mode 设置模型状态并持久化。返回消息。</summary>
    public static void SetActiveModel(string providerId, string modelId, string mode, out string message, string? baseUrl = null)
    {
        var pid = (providerId ?? "").Trim().ToLowerInvariant();
        var mid = (modelId ?? "").Trim();
        if (pid.Length == 0) { message = "providerId 不能为空"; return; }
        if (mid.Length == 0) { message = "modelId 不能为空"; return; }

        switch (mode.Trim().ToLowerInvariant())
        {
            case "free":
                SetFreeModelCore(pid, mid, baseUrl);
                message = $"已切换免费模型：{FormatModel(ModelCatalog.ProviderDisplayName(pid), mid)}（/free restore 还原收费模型）";
                return;
            case "restore":
            case "rollback":
                RestoreMainCore(out message);
                return;
            case "small":
                ApplyModelChoice(pid, mid, isLarge: false, out message, baseUrl);
                return;
            default: // "big"
                ApplyModelChoice(pid, mid, isLarge: true, out message, baseUrl);
                return;
        }
    }

    /// <summary>
    /// 临时切换免费模型（用户主动，/free N、Web /free、ModelCli）：
    ///   · 首次进入 free 时，把当前主模型快照到 rollback_connect（未记录才记）。
    ///   · 更新 state.free_connect + connect_mode=free。
    ///   · 【不覆盖】state.default_connect / 激活命名连接的 big connect。
    /// 运行时 LLM 由调用方按 free_connect 重配（本方法只落状态）。
    /// </summary>
    private static void SetFreeModelCore(string providerId, string modelId, string? baseUrl)
    {
        lock (_lock)
        {
            Load();
            var st = State;
            var nowFree = string.Equals(st.ConnectMode, "free", StringComparison.OrdinalIgnoreCase)
                          && !string.IsNullOrWhiteSpace(st.FreeConnect);
            if (!nowFree && string.IsNullOrWhiteSpace(st.RollbackConnect))
            {
                // 快照当前主模型（锚点）
                var curPid = CurrentMainProviderId();
                var curMid = CurrentMainModel();
                var curUrl = CurrentMainBaseUrl();
                if (!string.IsNullOrEmpty(curPid) && !string.IsNullOrEmpty(curMid))
                {
                    _state = st with
                    {
                        RollbackConnect = FormatConnect(curPid, curMid),
                        RollbackBaseUrl = NormalizeBaseUrlOverride(curPid, curUrl),
                    };
                }
            }
            st = _state!;
            _state = st with
            {
                ConnectMode = "free",
                FreeConnect = FormatConnect(providerId, modelId),
                FreeBaseUrl = NormalizeBaseUrlOverride(providerId, baseUrl),
            };
            Save();
        }
        // 同步运行时镜像（cfg.Model 等 → free 模型），使全项目读点看到当前免费模型
        var cfg = Config.Instance;
        cfg.SessionModelMirror = false; // 显式 free 切换，解除会话加载的内存镜像标记
        SyncToConfig(cfg);
        cfg.SaveToConfigJson(); // config.json 不写模型字段；仅维持 .env 引导一致
        cfg.SaveToEnvFile();
    }

    /// <summary>恢复 /free 前的模型（用户主动 /free restore、/free-restore、--model restore）。</summary>
    public static string RestoreMain()
    {
        Load();
        return RestoreMainCore(out _);
    }

    private static string RestoreMainCore(out string message)
    {
        string tPid = "", tMid = "";
        string? tUrl = null;
        bool hasTarget = false;

        lock (_lock)
        {
            Load();
            var st = State;
            // 恢复目标：rollback_connect（进入 free/换主前的主模型快照）优先；
            // 无快照但当前在 free → 回 default 锚点（free 从未覆盖 default，锚点即恢复目标）。
            string? target = st.RollbackConnect;
            var wasFree = string.Equals(st.ConnectMode, "free", StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(target) && wasFree)
                target = st.DefaultConnect;

            if (string.IsNullOrWhiteSpace(target))
            {
                message = "⚠️ 无之前模型可恢复（先切换免费模型）";
                return message;
            }
            if (!TryParseConnect(target, out tPid, out tMid))
            {
                message = "⚠️ 恢复目标无效";
                return message;
            }
            tUrl = wasFree && !string.IsNullOrWhiteSpace(st.RollbackBaseUrl)
                ? st.RollbackBaseUrl : st.DefaultBaseUrl;

            _state = st with
            {
                ConnectMode = "big",
                FreeConnect = null,
                FreeBaseUrl = null,
                RollbackConnect = null,
                RollbackBaseUrl = null,
            };
            hasTarget = true;
            Save();
        }

        if (!hasTarget)
        {
            message = "⚠️ 无之前模型可恢复（先切换免费模型）";
            return message;
        }

        // 主模型切回目标（更新激活命名连接 big connect + state.default_connect）
        // 在 free 场景 default 从未被覆盖，此步等价无操作；在「--model name 换主后 restore」场景则真正切回。
        // 注意：在 _lock 外调用（内部会再走 SetActiveConnect + cfg.SaveToConfigJson，避免持锁嵌套）。
        ApplyModelChoice(tPid, tMid, isLarge: true, out message, tUrl);
        return message;
    }

    /// <summary>baseUrl 覆盖归一：与 provider 注册表推导不同（或推导不出）才存覆盖，否则 null。</summary>
    private static string? NormalizeBaseUrlOverride(string providerId, string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return null;
        var b = baseUrl.Trim();
        var reg = ResolveBaseUrl(providerId);
        return string.IsNullOrEmpty(reg) || !string.Equals(NormalizeUrl(b), NormalizeUrl(reg), StringComparison.OrdinalIgnoreCase)
            ? b : null;
    }

    private static string NormalizeUrl(string url)
    {
        var u = url.Trim();
        while (u.EndsWith('/')) u = u[..^1];
        return u;
    }

    /// <summary>
    /// 全新安装（无 connections.json）时，把 .env/环境变量引导的模型字段写入 state。
    /// 已有 connections.json → 其 state 权威，忽略（不覆盖）。
    /// </summary>
    internal static void ImportEnvModelState(Config cfg, List<string> appliedModelKeys)
    {
        if (appliedModelKeys == null || appliedModelKeys.Count == 0) return;
        lock (_lock)
        {
            Load();
            if (_sawConnectionsFile) return; // 已有 connections.json → 不覆盖
            var st = State;
            if (appliedModelKeys.Contains("Model") || appliedModelKeys.Contains("Provider"))
            {
                _state = st with
                {
                    ConnectMode = "big",
                    FreeConnect = null,
                    FreeBaseUrl = null,
                    DefaultConnect = FormatConnect(cfg.Provider, cfg.Model),
                };
            }
            if (appliedModelKeys.Contains("SmallModel") || appliedModelKeys.Contains("SmallProvider"))
            {
                var st2 = _state!;
                _state = st2 with { SmallConnect = FormatConnect(cfg.SmallProvider, cfg.SmallModel) };
            }
            if (appliedModelKeys.Contains("BaseUrl"))
            {
                var st3 = _state!;
                var dpid = TryParseConnect(st3.DefaultConnect, out var p3, out _) ? p3 : null;
                _state = st3 with { DefaultBaseUrl = NormalizeBaseUrlOverride(dpid ?? "", cfg.BaseUrl) };
            }
            Save();
        }
    }

    /// <summary>把 Config 直接写入的模型字段（SettingsScreen / /config / CLI 等未走统一入口的写）
    /// 收敛回 state（default/small），并持久化。由 Config.SaveToConfigJson 在保存前调用。
    /// 保持「connections.json state 单一权威」。仅当 backing 与 state 不一致时才动 state。</summary>
    internal static void ReconcileFromConfig(Config cfg)
    {
        lock (_lock)
        {
            Load();
            var st = State;

            // 会话加载只改内存镜像（不落盘设计）：cfg 模型字段来自会话而非用户显式配置，
            // 不收敛回 state —— 否则后续任意一次配置保存会把会话模型误写为持久默认。
            // 用户显式改模型（/config set、设置界面、/model、--model connect）会清除该位再保存。
            if (cfg.SessionModelMirror) return;

            var changed = false;

            // 主模型：backing 与「当前生效主模型」不一致 → 用户改了主模型
            var curPid = CurrentMainProviderId();
            var curMid = CurrentMainModel();
            var curUrl = CurrentMainBaseUrl();
            bool mainChanged = !string.Equals(cfg.Model, curMid, StringComparison.OrdinalIgnoreCase)
                               || !string.Equals(cfg.Provider, curPid, StringComparison.OrdinalIgnoreCase);
            if (mainChanged && !string.IsNullOrWhiteSpace(cfg.Model))
            {
                var newPid = string.IsNullOrWhiteSpace(cfg.Provider) ? (curPid ?? "custom") : cfg.Provider.Trim().ToLowerInvariant();
                _state = st with
                {
                    ConnectMode = "big",                 // 显式换主模型 → 退出 free（若在 free）
                    FreeConnect = null,
                    FreeBaseUrl = null,
                    DefaultConnect = FormatConnect(newPid, cfg.Model),
                    DefaultBaseUrl = NormalizeBaseUrlOverride(newPid, cfg.BaseUrl),
                };
                changed = true;
                // 命名连接同步：直写 Config 模型字段（/config set Model、设置页 Provider/Model）不更新命名连接 →
                // 把激活连接的大 connect 指到新模型，保证 connections[].big 与 state.default 同步（Phase 2）。
                SyncActiveConnectionToStateLocked(isLarge: true, newPid, cfg.Model);
            }
            else if (cfg.BaseUrl != curUrl)
            {
                // baseUrl 变了但模型没变（SettingsScreen BaseUrl / GUI·Web·MAUI 设置 / --config set BaseUrl）
                // 语义：改「当前主通道」的网关 —— free 改 free_base_url、big 改 default_base_url
                //（--model connect 已走 SetDefaultBaseUrl 独立入口，语义 = 改 default 锚点网关）。
                if (string.Equals(_state!.ConnectMode, "free", StringComparison.OrdinalIgnoreCase)
                    && TryParseConnect(_state.FreeConnect, out var fpid, out _))
                    _state = _state with { FreeBaseUrl = NormalizeBaseUrlOverride(fpid, cfg.BaseUrl) };
                else
                {
                    var dpid = CurrentMainProviderId();
                    _state = _state! with { DefaultBaseUrl = NormalizeBaseUrlOverride(dpid ?? "", cfg.BaseUrl) };
                }
                changed = true;
            }

            // 小模型
            var smallPid = CurrentSmallProviderId();
            var smallMid = CurrentSmallModel();
            if (!string.Equals(cfg.SmallModel, smallMid, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(cfg.SmallProvider, smallPid, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(cfg.SmallModel))
                {
                    var sp = string.IsNullOrWhiteSpace(cfg.SmallProvider) ? (smallPid ?? "deepseek") : cfg.SmallProvider.Trim().ToLowerInvariant();
                    _state = _state! with { SmallConnect = FormatConnect(sp, cfg.SmallModel) };
                    changed = true;
                    // 同步激活命名连接的小 connect（Phase 2：与 state.small 一致）
                    SyncActiveConnectionToStateLocked(isLarge: false, sp, cfg.SmallModel);
                }
            }

            if (changed) Save();
        }
    }

    /// <summary>Reconcile 方向（state→connection）：把激活命名连接的大/小 connect 指向
    /// state 刚更新的模型。直写 Config 模型字段的绕过路径不更新命名连接，这里补同步——
    /// 否则下次 SetActiveConnect/ActivateConnection 会按连接内容把 state 覆盖回旧模型。</summary>
    private static void SyncActiveConnectionToStateLocked(bool isLarge, string providerId, string modelId)
    {
        if (_connections is not { Count: > 0 }) return;
        var active = _connections.FirstOrDefault(c => string.Equals(c.Name, _active, StringComparison.OrdinalIgnoreCase))
                     ?? _connections[0];
        var conn = FindOrCreateConnectCore(providerId, modelId);
        var idx = _connections.FindIndex(x => string.Equals(x.Name, active.Name, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return;
        var updated = isLarge
            ? active with { BigConnect = conn.Name }
            : active with { SmallConnect = conn.Name };
        if (string.Equals(updated.BigConnect, active.BigConnect, StringComparison.OrdinalIgnoreCase)
            && string.Equals(updated.SmallConnect, active.SmallConnect, StringComparison.OrdinalIgnoreCase))
            return; // 无变化，不写盘
        _connections[idx] = updated;
    }

    /// <summary>
    /// 把默认主模型（default_connect 锚点）的网关改为指定 baseUrl（--model connect 收敛入口）。
    /// 直写 state.default_base_url 覆盖 + Config 镜像 + 持久化；不改 connect_mode
    /// （free 模式下改的是休眠 default 锚点网关，不影响当前运行的 free 模型）。
    /// </summary>
    public static void SetDefaultBaseUrl(string baseUrl)
    {
        lock (_lock)
        {
            Load();
            var st = State;
            var dpid = TryParseConnect(st.DefaultConnect, out var dp, out _) ? dp : null;
            _state = st with { DefaultBaseUrl = NormalizeBaseUrlOverride(dpid ?? "", baseUrl) };
            Save();
        }
        // 显式连接操作：解除会话加载的内存镜像标记（避免被后续 Reconcile 跳过）
        var cfg = Config.Instance;
        cfg.SessionModelMirror = false;
        SyncToConfig(cfg);
        cfg.SaveToConfigJson(); // config.json 不写模型字段；仅写非模型配置
        cfg.SaveToEnvFile();
    }
}
