using System.Text;

namespace CoreCoderSharp;

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

    /// <summary>
    /// 转换为 OpenAI 消息格式，用于追加到历史记录。
    /// </summary>
    public JsonObject ToMessage()
    {
        var msg = new JsonObject
        {
            ["role"] = "assistant",
            ["content"] = string.IsNullOrEmpty(Content) ? null : Content,
        };

        if (ToolCalls.Count > 0)
        {
            var tcArray = new JsonArray();
            foreach (var tc in ToolCalls)
            {
                tcArray.Add((JsonNode?)new JsonObject
                {
                    ["id"] = tc.Id,
                    ["type"] = "function",
                    ["function"] = new JsonObject
                    {
                        ["name"] = tc.Name,
                        ["arguments"] = JsonHelper.SerializeArgs(tc.Arguments),
                    },
                });
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
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(5) };

    /// <summary>当前活跃模型 (大模型)</summary>
    public string Model { get; set; }
    /// <summary>小模型 (用于压缩/摘要等简单任务)</summary>
    public string SmallModel { get; set; } = "deepseek-v4-flash";
    /// <summary>临时覆盖模型 (null=使用 Model)</summary>
    public string? ModelOverride { get; set; }

    /// <summary>实际使用的模型名</summary>
    public string EffectiveModel => ModelOverride ?? Model;

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

    public string ApiKey { get; }
    public string? BaseUrl { get; }
    public int MaxTokens { get; }
    public float Temperature { get; }

    public int TotalPromptTokens { get; private set; }
    public int TotalCompletionTokens { get; private set; }

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
        int maxTokens = 4096, float temperature = 0.0f)
    {
        Model = model;
        ApiKey = apiKey;
        BaseUrl = baseUrl;
        MaxTokens = maxTokens;
        Temperature = temperature;
    }

    /// <summary>
    /// 发送消息，流式返回响应，处理工具调用。
    /// onToolCall: 流式执行回调——每个工具调用参数接收完整后立即触发，不用等 LLM 说完。
    /// </summary>
    public async Task<LLMResponse> ChatAsync(
        List<JsonObject> messages,
        List<JsonObject>? tools = null,
        Action<string>? onToken = null,
        Action<ToolCall>? onToolCall = null,
        CancellationToken cancellationToken = default)
    {
        // 每次 HTTP 请求 60 秒超时，防止服务器无响应无限等待
        using var requestTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, requestTimeout.Token);
        cancellationToken = linked.Token;

        var endpoint = (BaseUrl ?? "https://api.openai.com").TrimEnd('/') + "/v1/chat/completions";

        // 调试日志：记录发送内容
        DebugLog.LogRequest(messages, tools ?? []);

        // 深克隆消息和工具 schema，防止 JsonNode Parent 冲突
        var clonedMessages = messages.Select(m => JsonNode.Parse(m.ToJsonString())!).ToList();
        var clonedTools = tools?.Select(t => JsonNode.Parse(t.ToJsonString())!).ToList();

        var body = new JsonObject
        {
            ["model"] = EffectiveModel,
            ["messages"] = new JsonArray(clonedMessages.ToArray()),
            ["stream"] = true,
            ["temperature"] = Temperature,
            ["max_tokens"] = MaxTokens,
            ["stream_options"] = new JsonObject { ["include_usage"] = true },
        };

        if (clonedTools is { Count: > 0 })
        {
            body["tools"] = new JsonArray(clonedTools.ToArray());
        }

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
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
                    Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
                };
                req.Headers.Add("Authorization", $"Bearer {ApiKey}");
                return req;
            }, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            body.Remove("stream_options");
            response = await CallWithRetryAsync(() =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
                };
                req.Headers.Add("Authorization", $"Bearer {ApiKey}");
                return req;
            }, cancellationToken);
        }

        var contentParts = new List<string>();
        var tcMap = new Dictionary<int, (string Id, string Name, string Args)>();
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
            if (data == "[DONE]") continue;

            JsonNode? chunk;
            try { chunk = JsonNode.Parse(data); }
            catch { continue; }
            if (chunk == null) continue;

            // usage 信息在最后一个分片中
            if (chunk["usage"] is { } usage)
            {
                promptTok = (int?)usage["prompt_tokens"] ?? 0;
                completionTok = (int?)usage["completion_tokens"] ?? 0;
            }

            if (chunk["choices"]?.AsArray() is not { Count: > 0 } choices) continue;
            var delta = choices[0]?["delta"];
            if (delta == null) continue;

            // 累积文本
            if (delta["content"]?.GetValue<string>() is { } text)
            {
                contentParts.Add(text);
                onToken?.Invoke(text);
            }

            // 跨分片累积工具调用
            if (delta["tool_calls"]?.AsArray() is { } tcDeltas)
            {
                foreach (var tc in tcDeltas)
                {
                    if (tc == null) continue;
                    var idx = (int?)tc["index"] ?? 0;
                    if (!tcMap.ContainsKey(idx))
                        tcMap[idx] = ("", "", "");

                    var (id, name, args) = tcMap[idx];
                    if (tc["id"]?.GetValue<string>() is { } tid) id = tid;
                    if (tc["function"]?["name"]?.GetValue<string>() is { } tname) name = tname;
                    if (tc["function"]?["arguments"]?.GetValue<string>() is { } targs) args += targs;
                    tcMap[idx] = (id, name, args);

                    // 流式执行：参数接收完整时，立即触发回调
                    if (onToolCall != null && id != "" && name != "" && args.EndsWith('}'))
                    {
                        Dictionary<string, object?> parsedArgs;
                        try { parsedArgs = ParseArgs(args); }
                        catch { continue; }
                        onToolCall(new ToolCall(id, name, parsedArgs));
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
            }
            catch
            {
                parsedArgs = [];
            }
            parsed.Add(new ToolCall(id, name, parsedArgs));
        }

        TotalPromptTokens += promptTok;
        TotalCompletionTokens += completionTok;

        var llmResp = new LLMResponse
        {
            Content = string.Concat(contentParts),
            ToolCalls = parsed,
            PromptTokens = promptTok,
            CompletionTokens = completionTok,
        };

        // 调试日志：记录收到内容
        DebugLog.LogResponse(llmResp.Content, llmResp.ToolCalls, promptTok, completionTok);

        return llmResp;
    }

    /// <summary>
    /// 在瞬态错误时使用指数退避重试（最多 3 次）。
    /// </summary>
    private async Task<HttpResponseMessage> CallWithRetryAsync(
        Func<HttpRequestMessage> createRequest,
        CancellationToken cancellationToken,
        int maxRetries = 3)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var req = createRequest();
                var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                // 5xx 服务器错误重试
                if ((int)resp.StatusCode >= 500 && attempt < maxRetries - 1)
                {
                    await Task.Delay((int)Math.Pow(2, attempt) * 1000, cancellationToken);
                    continue;
                }

                // 429 速率限制重试（解析 Retry-After 头）
                if (resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests && attempt < maxRetries - 1)
                {
                    var delay = ParseRetryAfter(resp) ?? (int)Math.Pow(2, attempt) * 1000;
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                return resp;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // 超时
                if (attempt == maxRetries - 1) throw;
                await Task.Delay((int)Math.Pow(2, attempt) * 1000, cancellationToken);
            }
            catch (HttpRequestException) when (attempt < maxRetries - 1)
            {
                await Task.Delay((int)Math.Pow(2, attempt) * 1000, cancellationToken);
            }
        }

        throw new InvalidOperationException("重试耗尽");
    }

    /// <summary>解析 HTTP Retry-After 头（秒数或 HTTP-date），返回毫秒延迟。</summary>
    private static int? ParseRetryAfter(HttpResponseMessage resp)
    {
        try
        {
            var header = resp.Headers.GetValues("Retry-After").FirstOrDefault();
            if (string.IsNullOrWhiteSpace(header)) return null;

            // 纯数字 = 秒数
            if (int.TryParse(header, out var seconds))
                return Math.Min(seconds * 1000, 120_000); // 最多等 2 分钟

            // HTTP-date 格式
            if (DateTime.TryParse(header, out var retryDate))
            {
                var delay = (int)(retryDate.ToUniversalTime() - DateTime.UtcNow).TotalMilliseconds;
                return delay > 0 ? Math.Min(delay, 120_000) : null;
            }

            return null;
        }
        catch { return null; }
    }

    /// <summary>
    /// AOT 兼容：将 JSON 字符串解析为参数字典。
    /// </summary>
    public static Dictionary<string, object?> ParseArgs(string json)
    {
        var result = new Dictionary<string, object?>();
        try
        {
            var node = JsonNode.Parse(json);
            if (node is JsonObject obj)
            {
                foreach (var (key, value) in obj)
                {
                    result[key] = value switch
                    {
                        null => null,
                        JsonValue jv => jv.GetValue<object>(),
                        _ => value.ToJsonString(),
                    };
                }
            }
        }
        catch
        {
            // 解析失败返回空字典
        }
        return result;
    }
}

/// <summary>
/// AOT 兼容的 JSON 辅助方法。
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
            sb.Append('"');
            sb.Append(EscapeJson(key));
            sb.Append("\":");
            sb.Append(SerializeValue(value));
        }
        sb.Append('}');
        return sb.ToString();
    }

    private static string SerializeValue(object? value)
    {
        return value switch
        {
            null => "null",
            string s => $"\"{EscapeJson(s)}\"",
            bool b => b ? "true" : "false",
            int i => i.ToString(),
            long l => l.ToString(),
            double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
            float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => $"\"{EscapeJson(value.ToString()!)}\"",
        };
    }

    /// <summary>
    /// 深拷贝 JsonNode（AOT 安全：通过序列化/反序列化）。
    /// </summary>
    public static JsonNode? DeepClone(JsonNode? node)
    {
        if (node == null) return null;
        return JsonNode.Parse(node.ToJsonString());
    }

    private static string EscapeJson(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }
}
