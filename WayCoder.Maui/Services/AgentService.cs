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
    private readonly object _lock = new();
    private Agent? _agent;

    /// <summary>当前会话是否正在运行（供 UI「停止」按钮 / 防重入）。</summary>
    public bool IsRunning { get; private set; }

    /// <summary>懒建 Agent（首次发送时按当前 Config 创建；配置变更后需调用 <see cref="Reset"/>）。</summary>
    public Agent EnsureAgent()
    {
        if (_agent != null) return _agent;
        lock (_lock)
        {
            if (_agent != null) return _agent;

            var cfg = Config.Instance;
            var info = ModelCatalog.Find(cfg.Model);
            var providerId = info?.ProviderId ?? cfg.Provider;
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
            return _agent;
        }
    }

    /// <summary>丢弃已建 Agent（用户在设置页改了模型/Key 后调用，下次发送按新配置重建）。</summary>
    public void Reset() => _agent = null;

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
