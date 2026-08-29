using WayCoder;
using WayCoder.Tools;

namespace WayCoder.Maui.Services;

/// <summary>
/// Agent 服务 —— 持有移动端单槽位 Agent + LLM，包装 ChatAsync 的流式回调泵回 UI 线程。
///
/// 移动端无多 Agent 工作区（单用户单智能体，对标 Web 版单槽位懒建），故只维护一个 Agent
/// （AgentId = "maui-slot-0"）。Agent 懒建在首次发送时（配置可能刚在设置页填好），
/// 用 double-checked locking 防并发首建。
/// </summary>
public sealed class AgentService
{
    // 单槽位 Agent 全局共享：移动端只有一个智能体（AgentId="maui-slot-0"）。
    // 静态化保证 SettingsPage 改配置后 Reset 对 ChatPage 持有的实例同样生效。
    private static readonly object _lock = new();
    private static Agent? _agent;

    /// <summary>当前会话是否正在运行（供 UI「停止」按钮 / 防重入）。</summary>
    public bool IsRunning { get; private set; }

    private static CancellationTokenSource? _activeCts;

    /// <summary>注册/注销当前活跃请求的 CTS（App 切后台取消在途请求用；只持有引用不创建）。</summary>
    public static void SetActiveCts(CancellationTokenSource? cts) => _activeCts = cts;

    /// <summary>App 切后台时取消当前流式/工具请求，避免后台 SSE 连接被系统挂起导致切回卡死。</summary>
    public static void CancelActive()
    {
        try { _activeCts?.Cancel(); } catch { }
    }

    /// <summary>懒建 Agent（首次发送时按当前 Config 创建；配置变更后需调用 <see cref="Reset"/>）。</summary>
    public Agent EnsureAgent()
    {
        if (_agent != null) return _agent;
        lock (_lock)
        {
            if (_agent != null) return _agent;

            var cfg = Config.Instance;
            var info = ModelCatalog.Find(cfg.Model);
            var providerId = ResolveProviderId(cfg);
            var key = ApiKeyStore.Get(providerId) ?? cfg.ApiKey;
            var baseUrl = ResolveBaseUrl(info, providerId, cfg.BaseUrl);

            var llm = new LLM(cfg.Model, key, baseUrl, cfg.MaxTokens, cfg.Temperature)
            {
                SmallModel = cfg.SmallModel,
            };

            _agent = new Agent(llm,
                maxContextTokens: ModelCatalog.ResolveContextWindow(cfg.Model, cfg.MaxContextTokens),
                maxBudgetUsd: cfg.MaxBudgetUsd,
                autoCommit: cfg.AutoGitCommit)
            {
                AgentId = "maui-slot-0",
            };

            // 运行时注入全局上下文：斜杠命令层（/model /mode /compact /config 等）经 ProgramContext 拿 Agent/LLM/Config。
            ProgramContext.Agent = _agent;
            ProgramContext.LLM = llm;
            ProgramContext.Config = cfg;
            return _agent;
        }
    }

    /// <summary>丢弃已建 Agent（用户在设置页改了模型/Key 后调用，下次发送按新配置重建）。
    /// 静态：单槽位共享，任意 AgentService 实例调用都重置全局 Agent。</summary>
    public static void Reset() => _agent = null;

    /// <summary>当前 Agent 实例（未建时 null；标题栏状态/任务摘要读统计用）。</summary>
    public static Agent? CurrentAgent => _agent;

    /// <summary>标题栏状态快照（工作模式/权限/todo/上下文/用量/花费）。</summary>
    public sealed record AgentStatus(
        string WorkMode, string PermMode, int TodoCount,
        int ContextUsed, int ContextMax,
        int PromptTokens, int CompletionTokens, double? Cost);

    /// <summary>读取当前任务统计（Agent 未建返回 null）。</summary>
    public static AgentStatus? GetStatus()
    {
        var agent = _agent;
        if (agent == null) return null;
        var llm = agent.LlmClient;

        string perm = PermissionManager.CurrentMode switch
        {
            PermissionManager.Mode.Yolo => "Yolo",
            PermissionManager.Mode.SmartAuto => "SmartAuto",
            PermissionManager.Mode.Auto => "Auto",
            _ => "Ask",
        };

        int todoCount = 0;
        try { todoCount = TodoTool.Items.Count(i => i.Status is "pending" or "in_progress"); } catch { }

        return new AgentStatus(
            WorkModeManager.Format(agent.WorkMode),
            perm,
            todoCount,
            agent.Context.CumulativePromptTokens,
            agent.Context.MaxTokens,
            llm.TaskPromptTokens,
            llm.TaskCompletionTokens,
            llm.TaskCost);
    }

    /// <summary>
    /// 当前配置下是否有可用 API Key（与 <see cref="EnsureAgent"/> 的 key 解析一致）。
    /// Key 按服务商存于 ApiKeyStore（api_keys.json），不在 Config.ApiKey —— 故不能只看 Config.ApiKey，
    /// 否则设置页填了 Key 仍被 ChatPage 判为「未配置」而拦下。local/custom 本地模型无需 key。
    /// </summary>
    public static bool HasUsableKey()
    {
        var cfg = Config.Instance;
        var providerId = ResolveProviderId(cfg);
        if (providerId is "local" or "custom") return true;
        return !string.IsNullOrEmpty(ApiKeyStore.Get(providerId) ?? cfg.ApiKey);
    }

    /// <summary>解析当前生效服务商 ID（模型目录推断 > 全局配置），key/模型共用。</summary>
    private static string ResolveProviderId(Config cfg)
    {
        var info = ModelCatalog.Find(cfg.Model);
        return info?.ProviderId ?? cfg.Provider;
    }

    /// <summary>发起一轮对话。onToken/onTool/onToolOutput 回调保证在 UI 线程执行。</summary>
    public async Task<string> ChatAsync(
        string userInput,
        Action<string> onToken,
        Action<string, string> onTool,
        Action<string> onToolOutput,
        CancellationToken ct)
    {
        var agent = EnsureAgent();
        IsRunning = true;
        try
        {
            return await agent.ChatAsync(
                userInput,
                token => MainThread.BeginInvokeOnMainThread(() => onToken(token)),
                (name, summary) => MainThread.BeginInvokeOnMainThread(() => onTool(name, summary)),
                output => MainThread.BeginInvokeOnMainThread(() => onToolOutput(output)),
                ct);
        }
        finally
        {
            IsRunning = false;
        }
    }

    /// <summary>baseUrl 解析（复刻 Web 版 ResolveBaseUrl：provider 注册表地址 > model 默认地址 > 全局配置）。</summary>
    private static string? ResolveBaseUrl(ModelCatalog.ModelInfo? info, string providerId, string? globalBaseUrl)
    {
        if (ModelCatalog.Providers.TryGetValue(providerId, out var p) && !string.IsNullOrEmpty(p.DefaultBaseUrl))
            return p.DefaultBaseUrl;
        if (info?.DefaultBaseUrl != null) return info.DefaultBaseUrl;
        return globalBaseUrl;
    }
}
