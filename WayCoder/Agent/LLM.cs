using System.Text;

namespace WayCoder;

/// <summary>
/// 工具调用记录
/// </summary>
public record ToolCall(string Id, string Name, Dictionary<string, object?> Arguments);

/// <summary>
/// LLM 响应，包含文本内容或工具调用请求。
/// </summary>
public record LLMResponse
{
    public string Content { get; init; } = "";
    public List<ToolCall> ToolCalls { get; init; } = [];
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    /// <summary>推理/思维链内容长度（字符数）。DeepSeek V4 等模型的 reasoning_content 不含在 Content 中。</summary>
    public int ReasoningTokens { get; init; }
    /// <summary>标记为致命错误（如所有模型失败）— Agent 应在退出前保存会话</summary>
    public bool IsFatalError { get; init; }

    /// <summary>
    /// 转换为 OpenAI 消息格式，用于追加到历史记录。
    /// </summary>
    public JNode ToMessage()
    {
        var msg = JNode.Object()
            .Set("role", "assistant")
            .Set("content", string.IsNullOrEmpty(Content) ? null : Content);

        if (ToolCalls.Count > 0)
        {
            var tcArray = JNode.Array();
            foreach (var tc in ToolCalls)
            {
                tcArray.Add(JNode.Object()
                    .Set("id", tc.Id)
                    .Set("type", "function")
                    .Set("function", JNode.Object()
                        .Set("name", tc.Name)
                        .Set("arguments", JsonHelper.SerializeArgs(tc.Arguments))));
            }
            msg["tool_calls"] = tcArray;
        }

        return msg;
    }
}

/// <summary>
/// LLM 提供商层 - OpenAI 兼容 API 的流式客户端。
///
/// 由于大多数提供商（DeepSeek、Qwen、Kimi、Ollama 等）都暴露
/// OpenAI 兼容的接口，我们使用 HttpClient 直接连接。切换提供商
/// 只需修改 BaseUrl + ApiKey。就这麽简单。
/// </summary>
public class LLM
{
    private static readonly HttpClient _http = CreateHttpClient();

    /// <summary>
    /// 解析 API 端点：BaseUrl 约定不含 /v1（自动追加路径）；
    /// 兼容用户误传 http://host:port/v1（剥离尾部 /v1 后再追加，避免 /v1/v1）。
    /// </summary>
    private static string ResolveApiEndpoint(string? baseUrl, string path)
    {
        var b = (baseUrl ?? "https://api.openai.com").TrimEnd('/');
        if (b.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            b = b[..^3].TrimEnd('/');
        // Gemini OpenAI 兼容端点以 /v1beta/openai 结尾：path 去掉 /v1 前缀避免重复
        //（baseUrl=.../v1beta/openai → .../v1beta/openai/chat/completions）
        var p = path;
        if (b.EndsWith("/openai", StringComparison.OrdinalIgnoreCase)
            && p.StartsWith("/v1/", StringComparison.OrdinalIgnoreCase))
            p = p["/v1".Length..];
        return b + p;
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler();
        // HTTP 代理支持 — 读取环境变量 HTTP_PROXY / HTTPS_PROXY，同时遵守 NO_PROXY。
        var proxyUrl = Environment.GetEnvironmentVariable("HTTPS_PROXY")
                    ?? Environment.GetEnvironmentVariable("HTTP_PROXY")
                    ?? Environment.GetEnvironmentVariable("ALL_PROXY");
        if (!string.IsNullOrWhiteSpace(proxyUrl))
        {
            handler.Proxy = new ProxyFromEnvironment(proxyUrl, GetNoProxy());
            handler.UseProxy = true;
        }
        // Timeout 由内部 CancellationTokenSource 逐次控制，不通过 HttpClient.Timeout
        // （HttpClient.Timeout 发送首次请求后不可修改，渐进重试会报错）
        return new HttpClient(handler) { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
    }

    private static string? GetNoProxy()
        => Environment.GetEnvironmentVariable("NO_PROXY")
           ?? Environment.GetEnvironmentVariable("no_proxy");

    internal static bool ShouldBypassProxy(string host, string? noProxy)
    {
        if (string.IsNullOrWhiteSpace(host)) return false;

        var normalizedHost = host.Trim('[', ']').ToLowerInvariant();
        if (IsLoopbackHost(normalizedHost)) return true;
        if (string.IsNullOrWhiteSpace(noProxy)) return false;

        foreach (var raw in noProxy.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var entry = raw.Trim();
            if (entry.Length == 0) continue;
            if (entry == "*") return true;

            // no_proxy 条目常见 host:port，端口不参与匹配；IPv6 地址含多个冒号，不拆端口。
            var colon = entry.LastIndexOf(':');
            if (colon > 0 && entry.IndexOf(':') == colon)
                entry = entry[..colon].Trim();

            entry = entry.TrimStart('.').Trim('[', ']').ToLowerInvariant();
            if (entry.Length == 0) continue;
            if (normalizedHost == entry || normalizedHost.EndsWith("." + entry, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static bool IsLoopbackHost(string host)
        => host is "localhost" or "127.0.0.1" or "::1" || host.StartsWith("127.", StringComparison.Ordinal);

    private sealed class ProxyFromEnvironment : System.Net.IWebProxy
    {
        private readonly Uri _proxy;
        private readonly string? _noProxy;

        public ProxyFromEnvironment(string proxyUrl, string? noProxy)
        {
            _proxy = new Uri(proxyUrl);
            _noProxy = noProxy;
        }

        public System.Net.ICredentials? Credentials { get; set; }

        public Uri? GetProxy(Uri destination) => IsBypassed(destination) ? null : _proxy;

        public bool IsBypassed(Uri host) => ShouldBypassProxy(host.Host, _noProxy);
    }

    /// <summary>当前活跃模型 (大模型)</summary>
    public string Model { get; set; } = "deepseek-v4-flash";
    /// <summary>小模型 (用于压缩/摘要等简单任务)</summary>
    public string SmallModel { get; set; } = "deepseek-v4-flash";
    /// <summary>临时覆盖模型 (null=使用 Model)</summary>
    public string? ModelOverride { get; set; }

    /// <summary>实际使用的模型名</summary>
    public string EffectiveModel => ModelOverride ?? Model;

    // ── 视觉（多模态）支持 ──

    /// <summary>待附加到下一轮请求的图片路径（按 agentId 分队列，防多槽位并行时图片跨槽位串扰）。</summary>
    private static readonly Dictionary<string, List<string>> PendingImages = [];
    private static readonly object _pendingImagesLock = new();

    /// <summary>将图片加入指定 Agent 的待发送队列（线程安全）。</summary>
    public static void QueueImage(string agentId, string path)
    {
        lock (_pendingImagesLock)
        {
            if (!PendingImages.TryGetValue(agentId, out var list))
                PendingImages[agentId] = list = [];
            list.Add(path);
        }
    }

    /// <summary>取走并清空指定 Agent 的待发送图片（线程安全）。无队列返回空列表。</summary>
    public static List<string> DrainImages(string agentId)
    {
        lock (_pendingImagesLock)
        {
            if (!PendingImages.TryGetValue(agentId, out var list)) return [];
            PendingImages.Remove(agentId);
            return list;
        }
    }

    /// <summary>
    /// 构造一条带图片的多模态 user 消息（OpenAI 兼容格式）。
    /// content 为数组：[{type:text,text}, {type:image_url,image_url:{url:data:...}}]。
    /// </summary>
    public static JNode BuildImageMessage(string text, List<string> imagePaths)
    {
        var parts = JNode.Array();
        if (!string.IsNullOrWhiteSpace(text))
            parts.Add(JNode.Object().Set("type", "text").Set("text", text));

        foreach (var p in imagePaths)
        {
            try
            {
                var bytes = File.ReadAllBytes(p);
                var b64 = Convert.ToBase64String(bytes);
                var mime = Path.GetExtension(p).ToLowerInvariant() switch
                {
                    ".png" => "image/png",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    ".bmp" => "image/bmp",
                    _ => "image/png",
                };
                parts.Add(JNode.Object()
                    .Set("type", "image_url")
                    .Set("image_url", JNode.Object().Set("url", $"data:{mime};base64,{b64}")));
            }
            catch
            {
                // 单张图片读取失败：跳过该图，不影响其余
            }
        }

        // 兜底：若所有图都失败，退化为纯文本消息，避免 content 空数组
        if (parts.Count == 0)
            return JNode.Object().Set("role", "user").Set("content", text);

        return JNode.Object().Set("role", "user").Set("content", parts);
    }

    // 每百万 token 的定价：（输入，输出）
    private static readonly Dictionary<string, (double Input, double Output)> Pricing = new()
    {
        // OpenAI - 当前旗舰
        ["gpt-5.5"] = (5, 30),
        ["gpt-5.4"] = (2.5, 15),
        ["gpt-5.4-mini"] = (0.75, 4.5),
        ["gpt-5.4-nano"] = (0.2, 1.25),
        ["o4-mini"] = (1.1, 4.4),
        // OpenAI - 上一代（仍被广泛使用）
        ["gpt-4.1"] = (2, 8),
        ["gpt-4.1-mini"] = (0.4, 1.6),
        ["gpt-4.1-nano"] = (0.1, 0.4),
        ["gpt-4o"] = (2.5, 10),
        ["gpt-4o-mini"] = (0.15, 0.6),
        // DeepSeek V4
        ["deepseek-v4-flash"] = (0.14, 0.28),
        ["deepseek-v4-pro"] = (0.435, 0.87),
        // DeepSeek V3 旧版（即将废弃，保留兼容）
        ["deepseek-chat"] = (0.27, 1.10),
        ["deepseek-reasoner"] = (0.55, 2.19),
        // Anthropic Claude
        ["claude-opus-4-6"] = (5, 25),
        ["claude-sonnet-4-6"] = (3, 15),
        ["claude-haiku-4-5"] = (1, 5),
        // 阿里 Qwen
        ["qwen3-max"] = (0.78, 3.9),
        ["qwen3-plus"] = (0.26, 0.78),
        ["qwen-max"] = (0.78, 3.9),
        // 月之暗面 Kimi
        ["kimi-k2.5"] = (0.6, 3),
    };

    /// <summary>API 密钥（Bearer Token）</summary>
    public string ApiKey { get; private set; }
    /// <summary>API 基础 URL（默认 https://api.openai.com）</summary>
    public string? BaseUrl { get; private set; }
    /// <summary>有效的 API endpoint URL（BaseUrl 不含 /v1，自动追加；兼容误传 /v1）</summary>
    public string Endpoint => ResolveApiEndpoint(BaseUrl, "/v1/chat/completions");
    /// <summary>每次请求最大输出 token 数</summary>
    public int MaxTokens { get; }
    /// <summary>采样温度（0=精确，1=创意）</summary>
    public float Temperature { get; }

    /// <summary>累计输入 token 数（用于成本估算）。backing field 用 Interlocked 累加，兼容并行子智能体归并的并发。</summary>
    private int _totalPromptTokens;
    public int TotalPromptTokens => _totalPromptTokens;
    /// <summary>累计输出 token 数（用于成本估算）</summary>
    private int _totalCompletionTokens;
    public int TotalCompletionTokens => _totalCompletionTokens;

    /// <summary>大模型（Model）累计输入 token —— 状态栏右侧分大小模型显示用量</summary>
    private int _largePromptTokens, _largeCompletionTokens;
    public int LargePromptTokens => _largePromptTokens;
    public int LargeCompletionTokens => _largeCompletionTokens;
    public int LargeTotalTokens => _largePromptTokens + _largeCompletionTokens;
    /// <summary>小模型（SmallModel/压缩）累计输入 token</summary>
    private int _smallPromptTokens, _smallCompletionTokens;
    public int SmallPromptTokens => _smallPromptTokens;
    public int SmallCompletionTokens => _smallCompletionTokens;
    public int SmallTotalTokens => _smallPromptTokens + _smallCompletionTokens;

    /// <summary>当前任务开始时已用的输入 token 数（快照）</summary>
    private int _taskStartPromptTokens;
    /// <summary>当前任务开始时已用的输出 token 数（快照）</summary>
    private int _taskStartCompletionTokens;

    /// <summary>当前任务的输入 token 数（从快照点至今）</summary>
    public int TaskPromptTokens => TotalPromptTokens - _taskStartPromptTokens;
    /// <summary>当前任务的输出 token 数（从快照点至今）</summary>
    public int TaskCompletionTokens => TotalCompletionTokens - _taskStartCompletionTokens;

    /// <summary>
    /// 当前任务的花费估算（美元）。
    /// 模型不在定价表中时返回 null。
    /// </summary>
    public double? TaskCost
    {
        get
        {
            if (!Pricing.TryGetValue(EffectiveModel, out var price)) return null;
            return TaskPromptTokens * price.Input / 1_000_000.0
                   + TaskCompletionTokens * price.Output / 1_000_000.0;
        }
    }

    /// <summary>保存当前累计用量快照，用于后续计算单次任务花费。</summary>
    public void SnapshotTaskCost()
    {
        _taskStartPromptTokens = TotalPromptTokens;
        _taskStartCompletionTokens = TotalCompletionTokens;
    }

    /// <summary>重置任务快照（任务取消或异常时调用）。</summary>
    public void ResetTaskCost()
    {
        _taskStartPromptTokens = 0;
        _taskStartCompletionTokens = 0;
    }

    /// <summary>最近一次请求的延迟（毫秒）</summary>
    public double LastLatencyMs { get; private set; }
    /// <summary>最近一次请求的每秒 token 数</summary>
    public double LastTokensPerSec { get; private set; }
    /// <summary>请求总次数</summary>
    private int _totalRequests;
    public int TotalRequests => _totalRequests;

    /// <summary>当前流式响应是否已开始输出推理内容</summary>
    private bool _reasoningShown;
    /// <summary>推理内容缓冲区（旁路保存，不进入对话历史，供调试恢复）</summary>
    private readonly StringBuilder _reasoningBuffer = new();
    /// <summary>已显示（推送给 onToken）的推理字符数。超 <see cref="MaxReasoningDisplayChars"/> 后停止显示，
    /// 防止无限制思考把聊天列表撑爆（_reasoningBuffer 仍完整累积，保统计/调试）。</summary>
    private int _reasoningShownChars;
    /// <summary>是否已输出「思考过长截断」提示（一次性，避免每片都刷提示）。</summary>
    private bool _reasoningTruncated;

    /// <summary>
    /// 推理内容显示上限（字符）。reasoning 完整流式到显示层（TUI ChatScreen 做尾部滚动窗口），
    /// 本上限仅作为一次性/管道模式打印的防失控保险（与单条消息上限一致，50k）。
    /// </summary>
    public const int MaxReasoningDisplayChars = 50_000;

    /// <summary>
    /// 粗略的美元成本估算。模型不在定价表中时返回 null。
    /// </summary>
    public double? EstimatedCost
    {
        get
        {
            if (!Pricing.TryGetValue(EffectiveModel, out var price)) return null;
            return TotalPromptTokens * price.Input / 1_000_000.0
                   + TotalCompletionTokens * price.Output / 1_000_000.0;
        }
    }

    public LLM(string model, string apiKey, string? baseUrl = null,
        int maxTokens = 32768, float temperature = 0.1f, int timeoutSeconds = 0)
    {
        Model = model;
        ApiKey = apiKey;
        BaseUrl = baseUrl;
        MaxTokens = maxTokens;
        Temperature = temperature;
        TimeoutSeconds = timeoutSeconds;
    }

    /// <summary>单次请求超时（秒）。&gt;0 时覆盖全局 LlmHttpTimeoutSec 且不渐进加长重试——探测/连通性测试用。</summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>
    /// 运行时重配置 API 密钥与基础 URL（换供应商/换 key 时用）。
    /// 不改动累计用量统计；模型切换用 <see cref="Model"/> 直接赋值。
    /// </summary>
    public void Reconfigure(string apiKey, string? baseUrl)
    {
        ApiKey = apiKey;
        BaseUrl = baseUrl;
    }

    /// <summary>
    /// 克隆一个配置相同、统计独立的 LLM 客户端。用于子智能体隔离：并行子智能体
    /// 各自持有独立实例，避免共享 <see cref="ModelOverride"/> 引发的并发竞态
    /// （多个子智能体并发读写同一客户端的小模型切换会互相污染）。
    /// </summary>
    public LLM Clone() => new(Model, ApiKey, BaseUrl, MaxTokens, Temperature)
    {
        SmallModel = SmallModel,
        // 不继承 ModelOverride：它是临时覆盖（如小模型压缩切换）。克隆若带上当前覆盖值，
        // 子智能体/独立槽位会被永久钉在临时模型上，而非父实例的默认大模型。
        ModelOverride = null,
    };

    /// <summary>
    /// 把另一个 LLM 客户端的用量统计累加到当前实例。子智能体 clone 完成后调用，
    /// 使子智能体的 token 花费与请求次数计入父智能体的任务统计（否则子智能体
    /// 的花费会因实例隔离而丢失）。
    /// </summary>
    public void MergeUsageFrom(LLM other)
    {
        // Interlocked 原子累加：并行子智能体（Task.WhenAll）会并发归并到同一父实例，
        // 普通 +=（读-改-写三段）并发会丢失增量。
        Interlocked.Add(ref _totalPromptTokens, other.TotalPromptTokens);
        Interlocked.Add(ref _totalCompletionTokens, other.TotalCompletionTokens);
        Interlocked.Add(ref _totalRequests, other.TotalRequests);
    }

    /// <summary>
    /// 原子累加输入/输出 token 用量。供自测注入用量、以及并行归并场景使用，
    /// 语义与 ContextManager.AddUsage 一致（TotalPromptTokens 为 getter-only，
    /// 外部不可直接赋值，只能经此方法原子累加）。
    /// </summary>
    public void AddUsage(int promptTokens, int completionTokens)
    {
        Interlocked.Add(ref _totalPromptTokens, promptTokens);
        Interlocked.Add(ref _totalCompletionTokens, completionTokens);
        // 按当前生效模型区分大/小模型用量：小模型压缩等经 ModelOverride=SmallModel 切换，
        // EffectiveModel==SmallModel 时计入小模型，否则计入大模型（状态栏右侧分大小显示）
        if (EffectiveModel == SmallModel)
        {
            Interlocked.Add(ref _smallPromptTokens, promptTokens);
            Interlocked.Add(ref _smallCompletionTokens, completionTokens);
        }
        else
        {
            Interlocked.Add(ref _largePromptTokens, promptTokens);
            Interlocked.Add(ref _largeCompletionTokens, completionTokens);
        }
    }

    /// <summary>
    /// 发送消息到 LLM，流式返回响应，处理工具调用。
    /// </summary>
    /// <param name="messages">对话历史（OpenAI 格式消息数组）</param>
    /// <param name="tools">可选工具定义列表</param>
    /// <param name="onToken">流式 token 回调（每收到一个 token 文本即触发）</param>
    /// <param name="onToolCall">流式工具调用回调（参数完整接收后立即触发，不等 LLM 说完）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>LLM 响应（文本 + 工具调用 + token 用量）</returns>
    public async Task<LLMResponse> ChatAsync(
        List<JNode> messages,
        List<JNode>? tools = null,
        Action<string>? onToken = null,
        Action<ToolCall>? onToolCall = null,
        CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _reasoningShown = false; // 每次请求重置推理标记
        _reasoningShownChars = 0;
        _reasoningTruncated = false;
        _reasoningBuffer.Clear();

        // 超时由 CallWithRetryAsync 内部逐次加长管理，外部仅传取消令牌
        // API 格式（openai/anthropic/gemini）：从当前模型的 provider 反查（模型级/厂商级），原生格式用各自端点
        var apiFormat = ModelCatalog.ResolveApiFormat(EffectiveModel, BaseUrl);
        var endpoint = apiFormat switch
        {
            "anthropic" => ResolveApiEndpoint(BaseUrl, "/v1/messages"),
            "gemini" => $"{BaseUrl?.TrimEnd('/')}/v1beta/models/{Uri.EscapeDataString(EffectiveModel)}:streamGenerateContent?alt=sse",
            _ => ResolveApiEndpoint(BaseUrl, "/v1/chat/completions"),
        };

        // 调试日志：记录发送内容
        DebugLog.LogRequest(messages, tools ?? []);

        // 深克隆消息和工具 schema，防止 JsonNode Parent 冲突
        var clonedMessages = messages.Select(m => Json.Parse(m.ToJson())!).ToList();
        var clonedTools = tools?.Select(t => Json.Parse(t.ToJson())!).ToList();

        // 省 token 模式：单次输出上限收紧，防止失控长输出（仅 On 生效，Auto/Off 用正常上限）
        var maxTokens = Config.Instance.EconomyMode == EconomyMode.Extreme
            ? Math.Min(MaxTokens, Config.Instance.EconomyMaxTokens / 2)
            : Config.Instance.EconomyMode == EconomyMode.On ? Math.Min(MaxTokens, Config.Instance.EconomyMaxTokens) : MaxTokens;

        // 构建请求体。JNode 无 Remove，400 回退时用 includeStreamOptions 参数重建不带 stream_options 的请求体。
        // 模型调用参数约束：模型级 > 厂商级 > 全局默认（Find 带网关：同 id 不同网关是两个条目）
        var constraints = ModelCatalog.ResolveModelCallConstraints(EffectiveModel, BaseUrl);
        JNode BuildBody(bool includeStreamOptions, bool includeTools = true, bool includeThinking = true)
        {
            if (apiFormat == "anthropic") return BuildAnthropicBody(clonedMessages, clonedTools, maxTokens, constraints);
            if (apiFormat == "gemini") return BuildGeminiBody(clonedMessages, clonedTools, maxTokens, constraints);

            var messagesArray = JNode.Array();
            foreach (var m in clonedMessages) messagesArray.Add(m);

            var b = JNode.Object()
                .Set("model", EffectiveModel)
                .Set("messages", messagesArray)
                .Set("stream", true)
                // temperature 先转 double 再 round 到约束精度（默认 2 位）：float 0.1f → double 0.10000000149011612，
                // JSON "R" 序列化输出长尾小数，被 glm-5.3 等严格网关（限 2 位）以 HTTP 400 拒绝
                .Set("temperature", Math.Round(Math.Clamp(
                    ModelCatalog.ResolveProviderTemperature(EffectiveModel, BaseUrl) ?? (double)Temperature,
                    0.0, 2.0), constraints.TemperaturePrecision))
                .Set("max_tokens", maxTokens);

            if (includeStreamOptions)
                b.Set("stream_options", JNode.Object().Set("include_usage", true));

            // 工具能力门控：模型不支持工具（如 Ollama gemma2）→ 不发 tools（400 回退仅作安全网）
            if (includeTools && constraints.SupportsTools && clonedTools is { Count: > 0 })
            {
                var toolsArray = JNode.Array();
                foreach (var t in clonedTools) toolsArray.Add(t);
                b.Set("tools", toolsArray);
            }

            // 推理深度：DeepSeek V4 / OpenAI o-series 支持 reasoning_effort 参数。
            // 值恒来自全局，但：不支持思考的模型（本地等）一律不发；允许集越界→跳过（不发，避免 HTTP 400）
            // includeThinking=false：400 回退第三级——能力推断为支持思考但网关实际不认（chat-only 模型），去 thinking 重试防误判
            if (includeThinking && constraints.SupportsThinking)
            {
                var reasoningEffort = ModelCatalog.ResolveReasoningEffort(
                    constraints.ReasoningEffortAllowed, Config.Instance.ReasoningEffort);
                if (!string.IsNullOrEmpty(reasoningEffort))
                {
                    b.Set("reasoning_effort", reasoningEffort);
                }
                else if (!string.IsNullOrEmpty(Config.Instance.ReasoningEffort))
                {
                    DebugLog.Log("llm",
                        $"模型 {EffectiveModel} 限制 reasoning_effort 允许集 [{constraints.ReasoningEffortAllowed}]，" +
                        $"全局值 {Config.Instance.ReasoningEffort} 越界，已跳过该字段");
                }
            }

            // 内网/离线部署：本地 Ollama 显式 num_ctx（上下文窗口），覆盖默认探测（0=自动，不发该字段）
            if (Config.Instance.OllamaNumCtx > 0 && ModelCatalog.IsOllamaBaseUrl(BaseUrl))
            {
                b.Set("options", JNode.Object().Set("num_ctx", Config.Instance.OllamaNumCtx));
            }

            return b;
        }

        // ── Anthropic 原生格式（POST /v1/messages）──
        JNode BuildAnthropicBody(List<JNode> msgs, List<JNode>? tools, int maxTok, ModelCatalog.ModelCallConstraints cons)
        {
            var (system, messagesArray) = ConvertMessagesToAnthropic(msgs);
            var b = JNode.Object()
                .Set("model", EffectiveModel)
                .Set("messages", messagesArray)
                .Set("max_tokens", maxTok)                     // Anthropic 必填
                .Set("temperature", Math.Round((double)Math.Clamp(Temperature, 0f, 1f), cons.TemperaturePrecision))
                .Set("stream", true);
            if (system != null) b.Set("system", system);
            // 工具能力门控（原生格式无 400 回退，必须提前判断）
            if (cons.SupportsTools && tools is { Count: > 0 }) b.Set("tools", ConvertToolsToAnthropic(tools));
            // 支持思考时开启 extended thinking（当前全局 reasoning_effort 仅 OpenAI 格式用，Anthropic 用 thinking 块）
            if (cons.SupportsThinking && !string.IsNullOrEmpty(Config.Instance.ReasoningEffort))
            {
                b.Set("thinking", JNode.Object()
                    .Set("type", "enabled")
                    .Set("budget_tokens", Math.Min(maxTok, 1024)));
            }
            return b;
        }

        // ── Gemini 原生格式（POST /v1beta/models/{model}:streamGenerateContent）──
        JNode BuildGeminiBody(List<JNode> msgs, List<JNode>? tools, int maxTok, ModelCatalog.ModelCallConstraints cons)
        {
            var (contents, system) = ConvertMessagesToGemini(msgs);
            var b = JNode.Object()
                .Set("contents", contents)
                .Set("generationConfig", JNode.Object()
                    .Set("temperature", Math.Round((double)Math.Clamp(Temperature, 0f, 1f), cons.TemperaturePrecision))
                    .Set("maxOutputTokens", maxTok));
            if (system != null)
                b.Set("systemInstruction", JNode.Object().Set("parts", JNode.Array().Add(JNode.Object().Set("text", system))));
            // 工具能力门控（原生格式无 400 回退，必须提前判断）
            if (cons.SupportsTools && tools is { Count: > 0 }) b.Set("tools", ConvertToolsToGemini(tools));
            // 支持思考时开 thinkingConfig（Gemini 思考开关）
            if (cons.SupportsThinking && !string.IsNullOrEmpty(Config.Instance.ReasoningEffort))
            {
                b.Set("thinkingConfig", JNode.Object().Set("thinkingBudget", Math.Min(maxTok, 1024)));
            }
            return b;
        }

        // 构造带鉴权头的请求（局部函数，供首次与 400 回退多处复用）
        HttpRequestMessage CreateAuthRequest(JNode requestBody)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(requestBody.ToJson(), Encoding.UTF8, "application/json"),
            };
            // 鉴权头按 API 格式：openai=Bearer；anthropic=x-api-key+版本头；gemini=x-goog-api-key
            if (apiFormat == "anthropic")
            {
                req.Headers.TryAddWithoutValidation("x-api-key", ApiKey);
                req.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
            }
            else if (apiFormat == "gemini")
            {
                req.Headers.TryAddWithoutValidation("x-goog-api-key", ApiKey);
            }
            else
            {
                req.Headers.Add("Authorization", $"Bearer {ApiKey}");
            }
            return req;
        }

        // stream_options 是 OpenAI 扩展，尝试带它请求；不支持该参数的兼容端点会返回 400。
        // 注意：CallWithRetryAsync 对 4xx 是「返回响应」而非抛异常（其抛出的 HttpRequestException
        // 也不带 StatusCode），故必须在返回后检查状态码再回退——此前 catch-when(StatusCode==BadRequest)
        // 永远不会命中，是死代码，导致 400 被当成 SSE 流解析、静默返回空响应。
        // 400 三级回退：① 去 stream_options（OpenAI 扩展，部分兼容端点不认）
        // ② 再去 tools（Ollama gemma2 等模型不支持工具调用 → 退化纯文本）
        // ③ 再去 reasoning_effort（chat-only 模型被误发 thinking 参数 → 退化纯对话）。每次重试前释放旧响应。
        var resp = await CallWithRetryAsync(() => CreateAuthRequest(BuildBody(includeStreamOptions: true, includeTools: true)), cancellationToken);
        if (resp.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            resp.Dispose();
            resp = await CallWithRetryAsync(() => CreateAuthRequest(BuildBody(includeStreamOptions: false, includeTools: true)), cancellationToken);
            if (resp.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                resp.Dispose();
                resp = await CallWithRetryAsync(() => CreateAuthRequest(BuildBody(includeStreamOptions: false, includeTools: false)), cancellationToken);
                if (resp.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    resp.Dispose();
                    resp = await CallWithRetryAsync(() => CreateAuthRequest(BuildBody(includeStreamOptions: false, includeTools: false, includeThinking: false)), cancellationToken);
                }
            }
        }
        // using 声明只读，不能重新赋值，故上面用普通变量 resp 重试，此处捕获最终响应统一释放
        using var response = resp;

        // 非 2xx 响应体不是 SSE：直接解析会得到空回复、误判「已完成」。记录并抛错触发模型回退链。
        if (!response.IsSuccessStatusCode)
        {
            string errBody = "";
            try { errBody = await response.Content.ReadAsStringAsync(cancellationToken); } catch { }
            ErrorLog.LlmError(Model, Endpoint, $"HTTP {(int)response.StatusCode}: {ContextManager.TruncateByRunes(errBody, 300)}");
            throw new HttpRequestException($"LLM 请求失败 HTTP {(int)response.StatusCode}");
        }

        var contentParts = new List<string>();
        var tcMap = new Dictionary<int, (string Id, string Name, string Args)>();
        // 已通过 onToolCall 流式触发的工具调用 index（去重：参数完整后 provider 可能再发同 index 空 delta）
        var firedToolCalls = new HashSet<int>();
        var streamEndedGracefully = false;
        int promptTok = 0, completionTok = 0;

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        // 内部超时 CTS 在 CallWithRetryAsync 返回响应头时已 Dispose——正文读取只绑外部取消令牌，
        // provider 发响应头后正文停滞会永久挂起。为正文读取另建独立超时（5 分钟）防止卡死。
        using var bodyCts = new CancellationTokenSource(TimeSpan.FromSeconds(300));
        using var bodyLinked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, bodyCts.Token);

        // ── Anthropic 原生 SSE（message_start/content_block_*/message_stop）──
        void ParseAnthropicChunk(string data)
        {
            JNode? chunk;
            try { chunk = Json.Parse(data); } catch { return; }
            if (chunk == null) return;
            switch (chunk["type"]?.AsString())
            {
                case "message_start":
                    promptTok = (int?)(chunk["message"]?["usage"]?["input_tokens"]?.AsNumber()) ?? 0;
                    break;
                case "message_delta":
                    completionTok = (int?)(chunk["usage"]?["output_tokens"]?.AsNumber()) ?? 0;
                    break;
                case "message_stop":
                    streamEndedGracefully = true;
                    break;
                case "content_block_start":
                    var cb = chunk["content_block"];
                    if (cb?["type"]?.AsString() == "tool_use")
                    {
                        var bidx = (int?)(chunk["index"]?.AsNumber()) ?? 0;
                        tcMap[bidx] = (cb["id"]?.AsString() ?? "", cb["name"]?.AsString() ?? "", "");
                    }
                    break;
                case "content_block_delta":
                    var delta = chunk["delta"];
                    var dtype = delta?["type"]?.AsString();
                    var bidx2 = (int?)(chunk["index"]?.AsNumber()) ?? 0;
                    if (dtype == "text_delta" && delta?["text"]?.AsString() is { } txt && txt.Length > 0)
                    {
                        if (_reasoningShown) { _reasoningShown = false; onToken?.Invoke("«/»\n"); }
                        contentParts.Add(txt); onToken?.Invoke(txt);
                    }
                    else if (dtype == "thinking_delta" && delta?["thinking"]?.AsString() is { } th && th.Length > 0)
                    {
                        if (!_reasoningShown) { _reasoningShown = true; onToken?.Invoke("\n«dim»"); }
                        _reasoningShownChars += th.Length;
                        if (_reasoningShownChars <= MaxReasoningDisplayChars)
                            onToken?.Invoke(th);
                        else if (!_reasoningTruncated)
                        {
                            _reasoningTruncated = true;
                            onToken?.Invoke($"\n«orange3»… 思考内容过长，显示窗口受限«/»");
                        }
                        _reasoningBuffer.Append(th);
                    }
                    else if (dtype == "input_json_delta" && delta?["partial_json"]?.AsString() is { } pj)
                    {
                        var (id, name, args) = tcMap.TryGetValue(bidx2, out var v) ? v : ("", "", "");
                        args += pj; tcMap[bidx2] = (id, name, args);
                        if (onToolCall != null && id != "" && name != "" && !firedToolCalls.Contains(bidx2)
                            && TryParseCompleteJson(args, out var parsedArgs))
                        {
                            firedToolCalls.Add(bidx2);
                            onToolCall(new ToolCall(id, name, parsedArgs!));
                        }
                    }
                    break;
            }
        }

        // ── Gemini 原生 SSE（candidates[0].content.parts + usageMetadata）──
        void ParseGeminiChunk(string data)
        {
            JNode? chunk;
            try { chunk = Json.Parse(data); } catch { return; }
            if (chunk == null) return;
            if (chunk["usageMetadata"] is { } usage)
            {
                promptTok = (int?)(usage["promptTokenCount"]?.AsNumber()) ?? 0;
                completionTok = (int?)(usage["candidatesTokenCount"]?.AsNumber()) ?? 0;
            }
            var cands = chunk["candidates"];
            if (cands is not { Count: > 0 }) return;
            var cand = cands[0];
            var parts = cand?["content"]?["parts"];
            if (parts != null)
            {
                foreach (var p in parts.Items)
                {
                    if (p["text"]?.AsString() is { } txt && txt.Length > 0)
                    {
                        if (_reasoningShown) { _reasoningShown = false; onToken?.Invoke("«/»\n"); }
                        contentParts.Add(txt); onToken?.Invoke(txt);
                    }
                    else if (p["thought"]?.AsString() is { } th && th.Length > 0)
                    {
                        if (!_reasoningShown) { _reasoningShown = true; onToken?.Invoke("\n«dim»"); }
                        onToken?.Invoke(th); _reasoningBuffer.Append(th);
                    }
                    else if (p["functionCall"] is { } fc)
                    {
                        // Gemini 无工具 id / 无增量：整对象一次性到达，合成 id
                        var name = fc["name"]?.AsString() ?? "";
                        var idx = tcMap.Count;
                        var argsJson = fc["args"]?.ToJson() ?? "{}";
                        tcMap[idx] = ($"{name}#{idx}", name, argsJson);
                        if (onToolCall != null && ParseArgs(argsJson) is { } pargs)
                        {
                            firedToolCalls.Add(idx);
                            onToolCall(new ToolCall($"{name}#{idx}", name, pargs));
                        }
                    }
                }
            }
            if (cand?["finishReason"]?.AsString() is { } fr && (fr == "STOP" || fr == "TOOL_CALL"))
                streamEndedGracefully = true;
        }

        while (true)
        {
            bodyLinked.Token.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(bodyLinked.Token);
            if (line == null) break; // 流结束
            if (!line.StartsWith("data: ")) continue;

            var data = line[6..];
            if (apiFormat == "anthropic") { ParseAnthropicChunk(data); continue; }
            if (apiFormat == "gemini") { ParseGeminiChunk(data); continue; }
            if (data == "[DONE]") { streamEndedGracefully = true; continue; }

            JNode? chunk;
            try { chunk = Json.Parse(data); }
            catch { continue; }
            if (chunk == null) continue;

            // usage 信息在最后一个分片中
            if (chunk["usage"] is { } usage)
            {
                promptTok = (int?)(usage["prompt_tokens"]?.AsNumber()) ?? 0;
                completionTok = (int?)(usage["completion_tokens"]?.AsNumber()) ?? 0;
            }

            if (chunk["choices"] is not { Count: > 0 } choices) continue;
            var delta = choices[0]?["delta"];
            if (delta == null) continue;

            // 累积文本 — 只取 content 字段存入对话历史
            if (delta["content"]?.AsString() is { } text && text.Length > 0)
            {
                // 从推理模式切换到正式输出：关闭暗色样式
                if (_reasoningShown)
                {
                    _reasoningShown = false;
                    onToken?.Invoke("«/»\n");
                }
                contentParts.Add(text);
                onToken?.Invoke(text);
            }
            // reasoning / reasoning_content：显示给用户（暗色），但不存入 contentParts
            // DeepSeek 用 reasoning_content，Ollama/qwen 用 reasoning
            // 显示 = 让用户看到思考过程  不存 = 不污染对话历史
            // 限制显示：思考内容超过阈值后停止推送给 onToken（避免无限制思考把聊天列表撑爆），
            // 但 _reasoningBuffer 仍完整累积（保 ReasoningTokens 统计 / 调试日志）。
            else if (TryGetReasoningText(delta, out var rtext))
            {
                if (!_reasoningShown)
                {
                    _reasoningShown = true;
                    onToken?.Invoke("\n«dim»");
                }
                _reasoningShownChars += rtext.Length;
                if (_reasoningShownChars <= MaxReasoningDisplayChars)
                {
                    onToken?.Invoke(rtext);
                }
                else if (!_reasoningTruncated)
                {
                    _reasoningTruncated = true;
                    onToken?.Invoke($"\n«orange3»… 思考内容过长，显示窗口受限«/»");
                }
                _reasoningBuffer.Append(rtext);
            }

            // 跨分片累积工具调用
            if (delta["tool_calls"] is { } tcDeltas)
            {
                // 从推理模式切换到工具调用：关闭暗色样式
                if (_reasoningShown)
                {
                    _reasoningShown = false;
                    onToken?.Invoke("«/»\n");
                }
                foreach (var tc in tcDeltas.Items)
                {
                    var idx = (int?)(tc["index"]?.AsNumber()) ?? 0;
                    if (!tcMap.ContainsKey(idx))
                        tcMap[idx] = ("", "", "");

                    var (id, name, args) = tcMap[idx];
                    if (tc["id"]?.AsString() is { } tid) id = tid;
                    if (tc["function"]?["name"]?.AsString() is { } tname) name = tname;
                    if (tc["function"]?["arguments"]?.AsString() is { } targs) args += targs;
                    tcMap[idx] = (id, name, args);

                    // 流式执行：用 JSON 解析器验证参数完整性（不靠 } 结尾，避免 C# 代码中的 } 误判）。
                    // firedToolCalls 去重：参数完整后 provider 可能再发同 index 的空 delta（args 不变仍可解析），
                    // 若不防重，同一工具调用会被重复触发。
                    if (onToolCall != null && id != "" && name != "" && args.Length > 0 && !firedToolCalls.Contains(idx))
                    {
                        if (TryParseCompleteJson(args, out var parsedArgs))
                        {
                            firedToolCalls.Add(idx);
                            onToolCall(new ToolCall(id, name, parsedArgs!));
                        }
                    }
                }
            }
        }

        // 解析累积的工具调用
        var parsed = new List<ToolCall>();
        foreach (var idx in tcMap.Keys.Order())
        {
            var (id, name, args) = tcMap[idx];
            Dictionary<string, object?> parsedArgs;
            try
            {
                parsedArgs = ParseArgs(args);
                // 关键修复：当 ParseArgs 返回 _parse_error 伪参数时，清除它们
                // 防止 _parse_error / _parse_error_type / _raw_json_snippet 被当作工具参数传递
                if (parsedArgs.ContainsKey("_parse_error"))
                {
                    var errType = parsedArgs.GetValueOrDefault("_parse_error_type")?.ToString() ?? "未知";
                    parsedArgs.Remove("_parse_error");
                    parsedArgs.Remove("_parse_error_type");
                    parsedArgs.Remove("_raw_json_snippet");
                    DebugLog.Log("llm", $"工具调用 [{name}] JSON 不完整（{errType}），args 长度={args.Length}" +
                        (streamEndedGracefully ? "" : "，流未以 [DONE] 结束") +
                        (parsedArgs.Count > 0 ? $"，部分解析成功: {parsedArgs.Count} 个参数" : "，所有参数丢失"));
                }
            }
            catch
            {
                DebugLog.Log("llm", $"工具调用 [{name}] 解析异常，args 长度={args.Length}");
                parsedArgs = [];
            }
            parsed.Add(new ToolCall(id, name, parsedArgs));
        }

        // 流未正常结束警告
        if (!streamEndedGracefully && tcMap.Count > 0)
        {
            DebugLog.Log("llm", $"流未以 [DONE] 结束，{tcMap.Count} 个工具调用可能被截断");
        }

        // 流结束：如果推理样式未关闭则兜底关闭
        if (_reasoningShown)
        {
            _reasoningShown = false;
            onToken?.Invoke("«/»\n");
        }

        // 保存推理内容到旁路日志（不进入对话历史，供调试和代码恢复）
        if (_reasoningBuffer.Length > 0)
        {
            var reasoning = _reasoningBuffer.ToString();
            // 检测推理中是否包含大量代码（可能因流截断等原因未写入文件）
            var codeLines = reasoning.Count(c => c == ';') + reasoning.Count(c => c == '{');
            if (codeLines > 20)
            {
                DebugLog.Log("llm", $"推理内容包含大量代码特征（{codeLines} 个代码标记，{reasoning.Length} 字符）— 如果流被截断，这些代码可能丢失。");
            }
            DebugLog.Log("reasoning", reasoning);
        }

        Interlocked.Add(ref _totalPromptTokens, promptTok);
        Interlocked.Add(ref _totalCompletionTokens, completionTok);

        // 性能统计
        LastLatencyMs = sw.Elapsed.TotalMilliseconds;
        LastTokensPerSec = LastLatencyMs > 0
            ? (promptTok + completionTok) / (LastLatencyMs / 1000.0) : 0;
        Interlocked.Increment(ref _totalRequests);

        var llmResp = new LLMResponse
        {
            Content = string.Concat(contentParts),
            ToolCalls = parsed,
            PromptTokens = promptTok,
            CompletionTokens = completionTok,
            ReasoningTokens = _reasoningBuffer.Length,
        };

        // 调试日志：记录收到内容
        DebugLog.LogResponse(llmResp.Content, llmResp.ToolCalls, promptTok, completionTok);

        return llmResp;
    }

    /// <summary>
    /// 通过 /v1/embeddings 端点生成文本的嵌入向量。
    /// </summary>
    /// <param name="text">要嵌入的文本</param>
    /// <param name="model">嵌入模型名（默认 text-embedding-3-small）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>浮点向量数组，或 null（API 不可用/出错/不支持时）</returns>
    public async Task<float[]?> GetEmbeddingAsync(
        string text, string? model = null, CancellationToken cancellationToken = default)
    {
        var embeddingModel = model ?? "text-embedding-3-small";
        var endpoint = ResolveApiEndpoint(BaseUrl, "/v1/embeddings");

        var body = JNode.Object()
            .Set("model", embeddingModel)
            .Set("input", text);
        // EmbeddingDimensions>0 时显式请求维度（如 text-embedding-3-small 可降维省空间）
        if (Config.Instance.EmbeddingDimensions > 0)
            body.Set("dimensions", Config.Instance.EmbeddingDimensions);

        try
        {
            using var response = await CallWithRetryAsync(() =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(body.ToJson(), Encoding.UTF8, "application/json"),
                };
                req.Headers.Add("Authorization", $"Bearer {ApiKey}");
                return req;
            }, cancellationToken);

            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            var node = Json.Parse(responseText);
            var embeddingArray = node?["data"]?[0]?["embedding"];
            if (embeddingArray == null || embeddingArray.Count == 0) return null;

            var result = new float[embeddingArray.Count];
            for (int i = 0; i < embeddingArray.Count; i++)
            {
                result[i] = (float)(embeddingArray[i]?.AsNumber() ?? 0.0);
            }
            return result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 在瞬态错误时使用指数退避重试。超时时自动加长超时（1x→1.5x→2x→3x→4x…），
    /// 以适应模型深度思考导致的长时间无响应。
    /// </summary>
    private async Task<HttpResponseMessage> CallWithRetryAsync(
        Func<HttpRequestMessage> createRequest,
        CancellationToken cancellationToken,
        int timeoutSeconds = -1,
        int maxRetries = -1)
    {
        // LlmMaxRetries 语义是「重试次数」：总尝试次数 = 重试次数 + 1。配置允许 0（不重试，
        // 仍至少尝试 1 次）。此前把「重试次数」直接当「尝试次数」用，5 次重试只跑 5 次尝试
        // （实际 4 次重试），超时倍率表 [1,1.5,2,3,4,6,8] 永远走不到 6x/8x。
        var retries = maxRetries > 0 ? maxRetries : Config.Instance.LlmMaxRetries;
        var effectiveMaxRetries = Math.Max(1, retries + 1);
        // TimeoutSeconds 显式设置（探测/连通测试）优先：固定单次超时，不做渐进加长，扫描可控不拖沓
        var baseTimeoutSec = TimeoutSeconds > 0
            ? TimeoutSeconds
            : timeoutSeconds > 0 ? timeoutSeconds : Config.Instance.LlmHttpTimeoutSec;

        for (int attempt = 0; attempt < effectiveMaxRetries; attempt++)
        {
            var multiplier = GetTimeoutMultiplier(attempt);
            var thisTimeoutSec = baseTimeoutSec * multiplier;

            // 每次尝试创建新的内部 CTS，超时逐次加长
            using var internalCts = new CancellationTokenSource(TimeSpan.FromSeconds(thisTimeoutSec + 30));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, internalCts.Token);

            try
            {
                var req = createRequest();
                var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, linked.Token);

                // 5xx 服务器错误重试
                if ((int)resp.StatusCode >= 500 && attempt < effectiveMaxRetries - 1)
                {
                    // 释放响应体（ResponseHeadersRead 下 body 未被读取，不释放会泄漏连接/套接字）
                    resp.Dispose();
                    await Task.Delay((int)Math.Pow(2, attempt) * RetryBackoffMs, cancellationToken);
                    continue;
                }

                // 429 速率限制重试（解析 Retry-After 头）
                if (resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests && attempt < effectiveMaxRetries - 1)
                {
                    var delay = ParseRetryAfter(resp) ?? (int)Math.Pow(2, attempt) * RetryBackoffMs;
                    resp.Dispose(); // 同上，释放未读取的响应体，避免连接泄漏
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                return resp;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // 内部超时（非外部取消）——下一次自动加长时间
                if (attempt == effectiveMaxRetries - 1)
                {
                    ErrorLog.LlmError(Model, Endpoint,
                        $"请求超时（{attempt + 1}/{effectiveMaxRetries} 次尝试，最终超时 {thisTimeoutSec:F0}s）");
                    throw new HttpRequestException(
                        $"请求超时（{attempt + 1}/{effectiveMaxRetries} 次尝试，最终超时 {thisTimeoutSec:F0}s）");
                }
                var nextTimeout = baseTimeoutSec * GetTimeoutMultiplier(attempt + 1);
                ErrorLog.Warning("LLM",
                    $"请求超时 {thisTimeoutSec:F0}s，第 {attempt + 2}/{effectiveMaxRetries} 次加长至 {nextTimeout:F0}s");
                await Task.Delay((int)Math.Pow(2, attempt) * RetryBackoffMs, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                // 中文错误本地化：把 OS 级英文网络错误（Connection refused 等）转中文
                if (attempt == effectiveMaxRetries - 1)
                {
                    ErrorLog.LlmError(Model, Endpoint, $"网络错误：{ex.Message}");
                    throw new HttpRequestException(LocalizeError(ex.Message), ex);
                }
                ErrorLog.Warning("LLM", $"网络错误，重试 {attempt + 1}/{effectiveMaxRetries}: {LocalizeError(ex.Message)}");
                await Task.Delay((int)Math.Pow(2, attempt) * RetryBackoffMs, cancellationToken);
            }
        }

        ErrorLog.LlmError(Model, Endpoint, $"重试耗尽（{effectiveMaxRetries} 次）");
        throw new InvalidOperationException("重试耗尽");
    }

    /// <summary>超时逐次加长倍率（索引 = 尝试次数）。</summary>
    internal static readonly double[] TimeoutMultipliers = [1.0, 1.5, 2.0, 3.0, 4.0, 6.0, 8.0];

    /// <summary>重试指数退避的基准延迟（毫秒）。重试延迟 = 2^attempt × 此值。
    /// 默认 1000；自测把值调小可把「5xx 重试后成功」用例从 ~3s 压到毫秒级。</summary>
    internal static int RetryBackoffMs = 1000;

    /// <summary>计算第 attempt 次尝试（从 0 开始）的超时倍率。</summary>
    internal static double GetTimeoutMultiplier(int attempt) =>
        attempt < TimeoutMultipliers.Length
            ? TimeoutMultipliers[attempt]
            : TimeoutMultipliers[^1] + (attempt - TimeoutMultipliers.Length + 1);

    /// <summary>把常见英文网络/HTTP 错误本地化为中文（纯子串替换，未命中保留原文）。</summary>
    internal static string LocalizeError(string message)
    {
        if (string.IsNullOrEmpty(message)) return message;
        return message
            .Replace("Connection refused", "连接被拒绝")
            .Replace("connection refused", "连接被拒绝")
            .Replace("No such host is known", "无法解析主机名")
            .Replace("Name or service not known", "无法解析主机名")
            .Replace("The remote name could not be resolved", "无法解析远程主机名")
            .Replace("Unable to connect to the remote server", "无法连接到远程服务器")
            .Replace("An existing connection was forcibly closed", "连接被强制关闭")
            .Replace("Connection reset by peer", "连接被对端重置")
            .Replace("connection reset", "连接被重置")
            .Replace("timed out", "超时")
            .Replace("Timed out", "超时")
            .Replace("The operation was canceled", "操作已取消")
            .Replace("The request was canceled", "请求已取消");
    }

    /// <summary>解析 HTTP Retry-After 头（秒数或 HTTP-date），返回毫秒延迟。</summary>
    internal static int? ParseRetryAfter(HttpResponseMessage resp)
    {
        try
        {
            var header = resp.Headers.GetValues("Retry-After").FirstOrDefault();
            if (string.IsNullOrWhiteSpace(header)) return null;

            // 纯数字 = 秒数；负数回退默认退避——否则 Task.Delay(负) 抛 ArgumentOutOfRangeException（-1 更会无限等待）
            if (int.TryParse(header, out var seconds))
            {
                if (seconds < 0) return null;
                return (int)Math.Min((long)seconds * 1000, (long)Config.Instance.LlmRateLimitMaxWaitSec * 1000);
            }

            // HTTP-date 格式
            if (DateTime.TryParse(header, out var retryDate))
            {
                var delayMs = (retryDate.ToUniversalTime() - DateTime.UtcNow).TotalMilliseconds;
                if (delayMs <= 0) return null;
                // 远未来日期超出 int 毫秒范围会 (int) 溢出为负，先钳制到 maxWaitMs 再转 int
                var maxWaitMs = Config.Instance.LlmRateLimitMaxWaitSec * 1000;
                return (int)Math.Min(delayMs, (long)maxWaitMs);
            }

            return null;
        }
        catch { return null; }
    }

    /// <summary>
    /// AOT 兼容：将 JSON 字符串解析为参数字典。
    /// </summary>
    /// <summary>
    /// 从流式 chunk 的 delta 中提取推理文本。
    /// DeepSeek 用 "reasoning_content"，Ollama/qwen 用 "reasoning"。
    /// </summary>
    private static bool TryGetReasoningText(JNode delta, out string text)
    {
        text = "";
        // DeepSeek: reasoning_content
        if (delta["reasoning_content"]?.AsString() is { } t1 && t1.Length > 0)
        { text = t1; return true; }
        // Ollama / qwen: reasoning
        if (delta["reasoning"]?.AsString() is { } t2 && t2.Length > 0)
        { text = t2; return true; }
        return false;
    }

    // ── 非 OpenAI 格式：消息/工具转换（AOT 安全，手写 JSON）──

    /// <summary>OpenAI 内部消息 → Anthropic messages（system 提取到顶层；tool 消息转 tool_result 块；assistant tool_calls 转 tool_use 块）。</summary>
    private static (string? System, JNode Messages) ConvertMessagesToAnthropic(List<JNode> messages)
    {
        var arr = JNode.Array();
        string? system = null;
        foreach (var m in messages)
        {
            var role = m["role"]?.AsString() ?? "user";
            var contentNode = m["content"];
            var content = contentNode?.AsString() ?? "";
            if (role == "system") { system = content; continue; }
            var blocks = JNode.Array();
            if (role == "tool")
            {
                // Anthropic 要求 tool_result 放在 user 消息的 content 块
                blocks.Add(JNode.Object()
                    .Set("type", "tool_result")
                    .Set("tool_use_id", m["tool_call_id"]?.AsString() ?? "")
                    .Set("content", content));
                arr.Add(JNode.Object().Set("role", "user").Set("content", blocks));
                continue;
            }
            if (contentNode?.Kind == JKind.Array)
            {
                // 多模态数组 content（text + image_url）→ 文本块 + 图片块
                foreach (var c in contentNode.Items)
                {
                    if (c["type"]?.AsString() == "image_url"
                        && c["image_url"]?["url"]?.AsString() is { } url
                        && url.Split(',', 2) is { Length: 2 } dp)
                    {
                        var mime = dp[0].Replace("data:", "").Split(';')[0];
                        blocks.Add(JNode.Object()
                            .Set("type", "image")
                            .Set("source", JNode.Object()
                                .Set("type", "base64")
                                .Set("media_type", mime)
                                .Set("data", dp[1])));
                    }
                    else
                    {
                        blocks.Add(JNode.Object().Set("type", "text").Set("text", c["text"]?.AsString() ?? ""));
                    }
                }
            }
            else
            {
                blocks.Add(JNode.Object().Set("type", "text").Set("text", content));
            }
            // assistant 的工具调用 → tool_use 块（附在 assistant 消息 content 数组）
            if (role == "assistant" && m["tool_calls"] is { } tcs)
            {
                foreach (var tc in tcs.Items)
                {
                    var fn = tc["function"];
                    blocks.Add(JNode.Object()
                        .Set("type", "tool_use")
                        .Set("id", tc["id"]?.AsString() ?? "")
                        .Set("name", fn?["name"]?.AsString() ?? "")
                        .Set("input", Json.Parse(fn?["arguments"]?.AsString() ?? "{}") ?? JNode.Object()));
                }
            }
            arr.Add(JNode.Object().Set("role", role == "assistant" ? "assistant" : "user").Set("content", blocks));
        }
        return (system, arr);
    }

    /// <summary>OpenAI 内部消息 → Gemini contents（system 提为 systemInstruction；assistant→model；tool 消息转 functionResponse）。</summary>
    private static (JNode Contents, string? System) ConvertMessagesToGemini(List<JNode> messages)
    {
        var arr = JNode.Array();
        string? system = null;
        foreach (var m in messages)
        {
            var role = m["role"]?.AsString() ?? "user";
            var contentNode = m["content"];
            var content = contentNode?.AsString() ?? "";
            if (role == "system") { system = content; continue; }
            var parts = JNode.Array();
            if (role == "tool")
            {
                parts.Add(JNode.Object()
                    .Set("functionResponse", JNode.Object()
                        .Set("name", m["name"]?.AsString() ?? "unknown")
                        .Set("response", JNode.Object().Set("result", content))));
                arr.Add(JNode.Object().Set("role", "user").Set("parts", parts));
                continue;
            }
            if (contentNode?.Kind == JKind.Array)
            {
                // 多模态数组 content（text + image_url）→ text part + inlineData part
                foreach (var c in contentNode.Items)
                {
                    if (c["type"]?.AsString() == "image_url"
                        && c["image_url"]?["url"]?.AsString() is { } url
                        && url.Split(',', 2) is { Length: 2 } dp)
                    {
                        var mime = dp[0].Replace("data:", "").Split(';')[0];
                        parts.Add(JNode.Object()
                            .Set("inlineData", JNode.Object().Set("mimeType", mime).Set("data", dp[1])));
                    }
                    else
                    {
                        parts.Add(JNode.Object().Set("text", c["text"]?.AsString() ?? ""));
                    }
                }
            }
            else
            {
                parts.Add(JNode.Object().Set("text", content));
            }
            arr.Add(JNode.Object().Set("role", role == "assistant" ? "model" : "user").Set("parts", parts));
        }
        return (arr, system);
    }

    /// <summary>OpenAI 工具 schema → Anthropic tools（name/description/input_schema，无 type/function 包裹）。</summary>
    private static JNode ConvertToolsToAnthropic(List<JNode> tools)
    {
        var arr = JNode.Array();
        foreach (var t in tools)
        {
            var fn = t["function"];
            arr.Add(JNode.Object()
                .Set("name", fn?["name"]?.AsString() ?? "")
                .Set("description", fn?["description"]?.AsString() ?? "")
                .Set("input_schema", fn?["parameters"] ?? JNode.Object()));
        }
        return arr;
    }

    /// <summary>OpenAI 工具 schema → Gemini tools（functionDeclarations 数组）。</summary>
    private static JNode ConvertToolsToGemini(List<JNode> tools)
    {
        var arr = JNode.Array();
        foreach (var t in tools)
        {
            var fn = t["function"];
            arr.Add(JNode.Object()
                .Set("name", fn?["name"]?.AsString() ?? "")
                .Set("description", fn?["description"]?.AsString() ?? "")
                .Set("parameters", fn?["parameters"] ?? JNode.Object()));
        }
        return JNode.Object().Set("functionDeclarations", arr);
    }

    /// <summary>
    /// 尝试将 JSON 字符串解析为参数字典。仅当 JSON 完整且为有效对象时返回 true。
    /// 与 ParseArgs 不同：它不吞异常，调用方可据此判断 JSON 是否尚未接收完整。
    /// 增加基础完整性检查：JSON 不以逗号/冒号结尾，引号匹配，花括号平衡。
    /// </summary>
    internal static bool TryParseCompleteJson(string json, out Dictionary<string, object?>? result)
    {
        result = null;
        if (!IsJsonProbablyComplete(json)) return false;
        try
        {
            var node = Json.Parse(json);
            if (node?.Kind == JKind.Object && node.Count > 0)
            {
                result = new Dictionary<string, object?>();
                foreach (var (k, v) in node.Entries)
                    result[k] = JNodeToObject(v);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 基础 JSON 完整性检查：花括号平衡、不以逗号/冒号结尾、引号成对。
    /// 用于检测流式传输中尚未接收完整的 JSON 片段。
    /// </summary>
    internal static bool IsJsonProbablyComplete(string json)
    {
        if (string.IsNullOrEmpty(json)) return false;
        var trimmed = json.AsSpan().TrimEnd();
        if (trimmed.IsEmpty) return false;

        // 不以逗号或冒号结尾（说明还有字段/值未接收完）
        var lastChar = trimmed[^1];
        if (lastChar == ',' || lastChar == ':') return false;

        // 检查花括号是否平衡
        int braceDepth = 0, bracketDepth = 0;
        bool inString = false, escaped = false;
        foreach (var c in trimmed)
        {
            if (escaped) { escaped = false; continue; }
            if (c == '\\') { escaped = true; continue; }
            if (c == '"') { inString = !inString; continue; }
            if (inString) continue;
            if (c == '{') braceDepth++;
            if (c == '}') braceDepth--;
            if (c == '[') bracketDepth++;
            if (c == ']') bracketDepth--;
        }
        // 花括号未闭合，或仍在字符串中 = JSON 不完整
        return braceDepth == 0 && bracketDepth == 0 && !inString;
    }

    /// <summary>
    /// 递归将 JsonElement 转换为普通对象：标量→原生类型，数组→List，对象→Dictionary（重复键后者覆盖）。
    /// 改用 JsonDocument/JsonElement 而非 JsonNode：JsonNode 解析含重复键的 JSON
    /// （如 agent 工具偶发 {"task":"a","task":"b"}）时枚举会抛 ArgumentException（Key: xxx），
    /// 导致该轮工具参数失效被丢弃。JsonDocument 保留重复键且枚举不抛异常，后者覆盖即可容错。
    /// </summary>
    private static object? JNodeToObject(JNode node) => node.Kind switch
    {
        JKind.Null => null,
        JKind.Bool => node.AsBool(),
        JKind.String => node.AsString(),
        JKind.Number => ParseJsonNumber(node),
        JKind.Array => node.Items.Select(JNodeToObject).ToList(),
        JKind.Object => node.Entries.ToDictionary(e => e.Key, e => JNodeToObject(e.Value)), // 重复键后者覆盖
        _ => node.ToString(),
    };

    /// <summary>
    /// 将 JSON 数字转换为 long（整数）或 double（小数）。
    /// 对整数 TryGetInt64 成功返回 long；小数（如 3.14）TryGetInt64 返回 false 需回退 TryGetDouble；
    /// 超大整数两者都失败时退回原始文本，避免精度丢失。
    /// </summary>
    private static object ParseJsonNumber(JNode node)
    {
        var raw = node._numRaw;
        if (raw == null) return node.AsNumber();
        if (!raw.Contains('.') && !raw.Contains('e') && !raw.Contains('E'))
        {
            if (long.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var l)) return l;
            return raw; // 超大整数退回原始文本，避免精度丢失
        }
        return node.AsNumber();
    }

    public static Dictionary<string, object?> ParseArgs(string json)
    {
        var result = new Dictionary<string, object?>();
        try
        {
            var node = Json.Parse(json);
            if (node?.Kind == JKind.Object)
            {
                foreach (var (k, v) in node.Entries)
                {
                    result[k] = JNodeToObject(v);
                }
            }
        }
        catch (Exception ex)
        {
            // 解析失败 — 记录日志，返回空字典（不再暴露 _parse_error 伪参数）
            // v0.36.0 修复：此前返回 _parse_error/_parse_error_type/_raw_json_snippet，
            // 被上层当作工具参数传递，导致 write_file(_parse_error=True, ...) 幻觉
            DebugLog.Log("llm", $"ParseArgs 失败 — JSON 不完整或无效: {ex.Message} — raw: {ContextManager.TruncateWithEllipsis(json, 200, "...")}");
        }
        return result;
    }

    /// <summary>按 Unicode 码点截断用于日志预览，避免 UTF-16 切片在 emoji/扩展区字符（代理对）中间切断。</summary>
}
