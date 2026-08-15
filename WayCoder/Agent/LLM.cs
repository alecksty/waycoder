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

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler();
        // HTTP 代理支持 — 读取环境变量 HTTP_PROXY / HTTPS_PROXY
        var proxyUrl = Environment.GetEnvironmentVariable("HTTPS_PROXY")
                    ?? Environment.GetEnvironmentVariable("HTTP_PROXY")
                    ?? Environment.GetEnvironmentVariable("ALL_PROXY");
        if (!string.IsNullOrWhiteSpace(proxyUrl))
        {
            handler.Proxy = new System.Net.WebProxy(proxyUrl);
            handler.UseProxy = true;
        }
        // Timeout 由内部 CancellationTokenSource 逐次控制，不通过 HttpClient.Timeout
        // （HttpClient.Timeout 发送首次请求后不可修改，渐进重试会报错）
        return new HttpClient(handler) { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
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

    /// <summary>待附加到下一轮请求的图片路径（由 view_image 工具填充，Agent 主循环在下一轮消费）</summary>
    private static readonly List<string> PendingImages = [];
    private static readonly object _pendingImagesLock = new();

    /// <summary>将图片加入待发送队列（线程安全）</summary>
    public static void QueueImage(string path)
    {
        lock (_pendingImagesLock) PendingImages.Add(path);
    }

    /// <summary>取走并清空待发送图片（线程安全）</summary>
    public static List<string> DrainImages()
    {
        lock (_pendingImagesLock)
        {
            var copy = new List<string>(PendingImages);
            PendingImages.Clear();
            return copy;
        }
    }

    /// <summary>判断模型是否支持图片输入（vision）。用于 view_image 工具与 Agent 主循环的门控。</summary>
    public static bool ModelSupportsVision(string? model)
    {
        if (string.IsNullOrWhiteSpace(model)) return false;
        var m = model.ToLowerInvariant();
        return m.Contains("gpt-4o") || m.Contains("gpt-4.1") || m.Contains("gpt-5")
            || m.Contains("claude") || m.Contains("gemini") || m.Contains("vision")
            || m.Contains("qwen-vl") || m.Contains("glm-4v") || m.Contains("llava")
            || m.Contains("pixtral") || m.Contains("grok") || m.Contains("minimax")
            || m.Contains("doubao") || m.Contains("hunyuan");
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
    /// <summary>有效的 API endpoint URL</summary>
    public string Endpoint => (BaseUrl ?? "https://api.openai.com").TrimEnd('/') + "/v1/chat/completions";
    /// <summary>每次请求最大输出 token 数</summary>
    public int MaxTokens { get; }
    /// <summary>采样温度（0=精确，1=创意）</summary>
    public float Temperature { get; }

    /// <summary>累计输入 token 数（用于成本估算）</summary>
    public int TotalPromptTokens { get; private set; }
    /// <summary>累计输出 token 数（用于成本估算）</summary>
    public int TotalCompletionTokens { get; private set; }

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
            if (!Pricing.TryGetValue(Model, out var price)) return null;
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
    public int TotalRequests { get; private set; }

    /// <summary>当前流式响应是否已开始输出推理内容</summary>
    private bool _reasoningShown;
    /// <summary>推理内容缓冲区（旁路保存，不进入对话历史，供调试恢复）</summary>
    private readonly StringBuilder _reasoningBuffer = new();

    /// <summary>
    /// 粗略的美元成本估算。模型不在定价表中时返回 null。
    /// </summary>
    public double? EstimatedCost
    {
        get
        {
            if (!Pricing.TryGetValue(Model, out var price)) return null;
            return TotalPromptTokens * price.Input / 1_000_000.0
                   + TotalCompletionTokens * price.Output / 1_000_000.0;
        }
    }

    public LLM(string model, string apiKey, string? baseUrl = null,
        int maxTokens = 32768, float temperature = 0.1f)
    {
        Model = model;
        ApiKey = apiKey;
        BaseUrl = baseUrl;
        MaxTokens = maxTokens;
        Temperature = temperature;
    }

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
        ModelOverride = ModelOverride,
    };

    /// <summary>
    /// 把另一个 LLM 客户端的用量统计累加到当前实例。子智能体 clone 完成后调用，
    /// 使子智能体的 token 花费与请求次数计入父智能体的任务统计（否则子智能体
    /// 的花费会因实例隔离而丢失）。
    /// </summary>
    public void MergeUsageFrom(LLM other)
    {
        TotalPromptTokens += other.TotalPromptTokens;
        TotalCompletionTokens += other.TotalCompletionTokens;
        TotalRequests += other.TotalRequests;
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
        _reasoningBuffer.Clear();

        // 超时由 CallWithRetryAsync 内部逐次加长管理，外部仅传取消令牌
        var endpoint = (BaseUrl ?? "https://api.openai.com").TrimEnd('/') + "/v1/chat/completions";

        // 调试日志：记录发送内容
        DebugLog.LogRequest(messages, tools ?? []);

        // 深克隆消息和工具 schema，防止 JsonNode Parent 冲突
        var clonedMessages = messages.Select(m => Json.Parse(m.ToJson())!).ToList();
        var clonedTools = tools?.Select(t => Json.Parse(t.ToJson())!).ToList();

        // 省 token 模式：单次输出上限收紧，防止失控长输出（仅 On 生效，Auto/Off 用正常上限）
        var maxTokens = Config.Instance.EconomyMode == EconomyMode.On ? Math.Min(MaxTokens, Config.EconomyMaxTokens) : MaxTokens;

        // 构建请求体。JNode 无 Remove，400 回退时用 includeStreamOptions 参数重建不带 stream_options 的请求体。
        JNode BuildBody(bool includeStreamOptions)
        {
            var messagesArray = JNode.Array();
            foreach (var m in clonedMessages) messagesArray.Add(m);

            var b = JNode.Object()
                .Set("model", EffectiveModel)
                .Set("messages", messagesArray)
                .Set("stream", true)
                .Set("temperature", Math.Clamp(Temperature, 0f, 2f))
                .Set("max_tokens", maxTokens);

            if (includeStreamOptions)
                b.Set("stream_options", JNode.Object().Set("include_usage", true));

            if (clonedTools is { Count: > 0 })
            {
                var toolsArray = JNode.Array();
                foreach (var t in clonedTools) toolsArray.Add(t);
                b.Set("tools", toolsArray);
            }

            // 推理深度：DeepSeek V4 / OpenAI o-series 支持 reasoning_effort 参数
            var reasoningEffort = Config.Instance.ReasoningEffort;
            if (!string.IsNullOrEmpty(reasoningEffort))
            {
                b.Set("reasoning_effort", reasoningEffort);
            }

            return b;
        }

        var body = BuildBody(includeStreamOptions: true);

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(body.ToJson(), Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Authorization", $"Bearer {ApiKey}");

        // stream_options 是 OpenAI 扩展，尝试带它请求；400 则回退
        HttpResponseMessage response;
        try
        {
            response = await CallWithRetryAsync(() =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(body.ToJson(), Encoding.UTF8, "application/json"),
                };
                req.Headers.Add("Authorization", $"Bearer {ApiKey}");
                return req;
            }, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            body = BuildBody(includeStreamOptions: false);
            response = await CallWithRetryAsync(() =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(body.ToJson(), Encoding.UTF8, "application/json"),
                };
                req.Headers.Add("Authorization", $"Bearer {ApiKey}");
                return req;
            }, cancellationToken);
        }

        var contentParts = new List<string>();
        var tcMap = new Dictionary<int, (string Id, string Name, string Args)>();
        var streamEndedGracefully = false;
        int promptTok = 0, completionTok = 0;

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break; // 流结束
            if (!line.StartsWith("data: ")) continue;

            var data = line[6..];
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
            else if (TryGetReasoningText(delta, out var rtext))
            {
                if (!_reasoningShown)
                {
                    _reasoningShown = true;
                    onToken?.Invoke("\n«dim»");
                }
                onToken?.Invoke(rtext);
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

                    // 流式执行：用 JSON 解析器验证参数完整性（不靠 } 结尾，避免 C# 代码中的 } 误判）
                    if (onToolCall != null && id != "" && name != "" && args.Length > 0)
                    {
                        if (TryParseCompleteJson(args, out var parsedArgs))
                            onToolCall(new ToolCall(id, name, parsedArgs!));
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

        TotalPromptTokens += promptTok;
        TotalCompletionTokens += completionTok;

        // 性能统计
        LastLatencyMs = sw.Elapsed.TotalMilliseconds;
        LastTokensPerSec = LastLatencyMs > 0
            ? (promptTok + completionTok) / (LastLatencyMs / 1000.0) : 0;
        TotalRequests++;

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
        var endpoint = (BaseUrl ?? "https://api.openai.com").TrimEnd('/') + "/v1/embeddings";

        var body = JNode.Object()
            .Set("model", embeddingModel)
            .Set("input", text);

        try
        {
            var response = await CallWithRetryAsync(() =>
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
        var effectiveMaxRetries = maxRetries > 0 ? maxRetries : Config.Instance.LlmMaxRetries;
        var baseTimeoutSec = timeoutSeconds > 0 ? timeoutSeconds : Config.Instance.LlmHttpTimeoutSec;

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
                    await Task.Delay((int)Math.Pow(2, attempt) * 1000, cancellationToken);
                    continue;
                }

                // 429 速率限制重试（解析 Retry-After 头）
                if (resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests && attempt < effectiveMaxRetries - 1)
                {
                    var delay = ParseRetryAfter(resp) ?? (int)Math.Pow(2, attempt) * 1000;
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
                await Task.Delay((int)Math.Pow(2, attempt) * 1000, cancellationToken);
            }
            catch (HttpRequestException ex) when (attempt < effectiveMaxRetries - 1)
            {
                ErrorLog.Warning("LLM", $"网络错误，重试 {attempt + 1}/{effectiveMaxRetries}: {ex.Message}");
                await Task.Delay((int)Math.Pow(2, attempt) * 1000, cancellationToken);
            }
        }

        ErrorLog.LlmError(Model, Endpoint, $"重试耗尽（{effectiveMaxRetries} 次）");
        throw new InvalidOperationException("重试耗尽");
    }

    /// <summary>超时逐次加长倍率（索引 = 尝试次数）。</summary>
    internal static readonly double[] TimeoutMultipliers = [1.0, 1.5, 2.0, 3.0, 4.0, 6.0, 8.0];

    /// <summary>计算第 attempt 次尝试（从 0 开始）的超时倍率。</summary>
    internal static double GetTimeoutMultiplier(int attempt) =>
        attempt < TimeoutMultipliers.Length
            ? TimeoutMultipliers[attempt]
            : TimeoutMultipliers[^1] + (attempt - TimeoutMultipliers.Length + 1);

    /// <summary>解析 HTTP Retry-After 头（秒数或 HTTP-date），返回毫秒延迟。</summary>
    private static int? ParseRetryAfter(HttpResponseMessage resp)
    {
        try
        {
            var header = resp.Headers.GetValues("Retry-After").FirstOrDefault();
            if (string.IsNullOrWhiteSpace(header)) return null;

            // 纯数字 = 秒数
            if (int.TryParse(header, out var seconds))
                return Math.Min(seconds * 1000, Config.Instance.LlmRateLimitMaxWaitSec * 1000);

            // HTTP-date 格式
            if (DateTime.TryParse(header, out var retryDate))
            {
                var delay = (int)(retryDate.ToUniversalTime() - DateTime.UtcNow).TotalMilliseconds;
                var maxWaitMs = Config.Instance.LlmRateLimitMaxWaitSec * 1000;
                return delay > 0 ? Math.Min(delay, maxWaitMs) : null;
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
            DebugLog.Log("llm", $"ParseArgs 失败 — JSON 不完整或无效: {ex.Message} — raw: {(json.Length > 200 ? json[..200] + "..." : json)}");
        }
        return result;
    }
}

/// <summary>
/// AOT 兼容的 JSON 辅助方法。统一委托给 JsonLib 的 <see cref="Json.SerializeValue"/> / <see cref="Json.Quote"/>，
/// 消除重复实现，并统一 NaN/Inf→null、控制字符转义等语义。
/// </summary>
internal static class JsonHelper
{
    /// <summary>
    /// 将参数字典手动序列化为 JSON 字符串（无反射，AOT 安全）。
    /// </summary>
    public static string SerializeArgs(Dictionary<string, object?> args)
    {
        var sb = new StringBuilder("{");
        var first = true;
        foreach (var (key, value) in args)
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append(Json.Quote(key));
            sb.Append(':');
            sb.Append(Json.SerializeValue(value));
        }
        sb.Append('}');
        return sb.ToString();
    }

    /// <summary>
    /// 序列化任意对象（无反射，AOT 安全）。委托 <see cref="Json.SerializeValue"/>。
    /// </summary>
    public static string SerializeValue(object? value) => Json.SerializeValue(value);
}
