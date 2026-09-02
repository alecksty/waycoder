namespace WayCoder.Tools;

/// <summary>
/// 音频转录工具 —— 把本地音频文件转成文字（Whisper 兼容 API），补齐多模态的「音频输入」短板。
///
/// 对标 Codex CLI / Gemini CLI 的音频输入：先转录音频得到文本，再让大模型处理。
/// 复用 OpenAI 兼容的 <c>/v1/audio/transcriptions</c> 端点（multipart 上传），
/// 支持 OpenAI Whisper / Groq / faster-whisper 等任意兼容服务。
/// </summary>
public class TranscribeAudioTool : ITool
{
    public string Name => "transcribe";
    public string Description =>
        "转录音频文件为文字（Whisper 兼容 API），用于「听懂」语音/录音/会议记录。" +
        "支持 mp3/wav/m4a/flac/ogg/webm 等常见格式，返回转录文本。" +
        "需要配置 WAYCODER_WHISPER_API_KEY（或主 WAYCODER_API_KEY）与可选的 WAYCODER_WHISPER_BASE_URL。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("path", JNode.Object()
                .Set("type", "string")
                .Set("description", "音频文件路径（如 /path/to/meeting.mp3）"))
            .Set("language", JNode.Object()
                .Set("type", "string")
                .Set("description", "语言代码（ISO 639-1，如 zh/en/ja），省略则自动检测"))
            .Set("prompt", JNode.Object()
                .Set("type", "string")
                .Set("description", "可选引导词，提供上下文/术语帮助提高转录准确率")))
        .Set("required", JNode.Array().Add("path"));

    private const long MaxBytes = 25L * 1024 * 1024; // OpenAI Whisper 25MB 上限

    private static HttpClient _client => _lazyClient.Value;
    private static readonly Lazy<HttpClient> _lazyClient = new(() => new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(180),
    });

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var path = arguments.GetValueOrDefault("path")?.ToString() ?? "";
        var language = arguments.GetValueOrDefault("language")?.ToString() ?? "";
        var prompt = arguments.GetValueOrDefault("prompt")?.ToString() ?? "";

        var fullPath = ValidateAudioFile(path, out var error);
        if (fullPath == null) return error;

        // API Key 解析：专用 Whisper key 优先，回退主 key
        var apiKey = Config.Instance.WhisperApiKey;
        if (string.IsNullOrWhiteSpace(apiKey)) apiKey = Config.Instance.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            return "错误：未配置 API Key。请设置 WAYCODER_WHISPER_API_KEY 或 WAYCODER_API_KEY 后重试。";

        var baseUrl = ModelCatalog.NormalizeBaseUrl(Config.Instance.WhisperBaseUrl ?? "https://api.openai.com");
        var endpoint = $"{baseUrl}/v1/audio/transcriptions";
        var model = string.IsNullOrWhiteSpace(Config.Instance.WhisperModel)
            ? "whisper-1" : Config.Instance.WhisperModel;

        try
        {
            var bytes = await File.ReadAllBytesAsync(fullPath);
            if (bytes.Length > MaxBytes)
                return $"错误：音频过大（{bytes.Length / 1024 / 1024} MB），Whisper 上限 25MB。";

            var ext = Path.GetExtension(fullPath).TrimStart('.').ToLowerInvariant();
            var fileName = Path.GetFileName(fullPath);

            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(MapContentType(ext));
            form.Add(fileContent, "file", fileName);
            form.Add(new StringContent(model), "model");
            if (!string.IsNullOrWhiteSpace(language))
                form.Add(new StringContent(language), "language");
            if (!string.IsNullOrWhiteSpace(prompt))
                form.Add(new StringContent(prompt), "prompt");

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = form };
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await _client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return $"转录失败（HTTP {(int)response.StatusCode}）：{ContextManager.TruncateWithEllipsis(body, 300)}";

            var json = Json.Parse(body);
            var text = json?["text"]?.AsString();
            if (string.IsNullOrWhiteSpace(text))
                return $"转录返回空文本。原始响应：{ContextManager.TruncateWithEllipsis(body, 200)}";

            return text.Trim();
        }
        catch (TaskCanceledException)
        {
            return "错误：转录请求超时（180 秒）。";
        }
        catch (Exception ex)
        {
            return $"转录出错：{ex.GetType().Name}: {ex.Message}";
        }
    }

    // ========================================================================
    // 纯逻辑辅助（便于自测）
    // ========================================================================

    /// <summary>校验音频路径，返回绝对路径；非法时返回 null 并在 error 中说明。</summary>
    internal static string? ValidateAudioFile(string path, out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(path))
        {
            error = "错误：path 参数不能为空";
            return null;
        }

        var fullPath = Path.GetFullPath(path, CwdContext.Current.Value ?? Directory.GetCurrentDirectory()); // cd 后相对路径基于被跟踪工作目录
        if (!File.Exists(fullPath))
        {
            error = $"错误：音频文件不存在 — {fullPath}";
            return null;
        }

        var ext = Path.GetExtension(fullPath).TrimStart('.').ToLowerInvariant();
        if (!IsSupportedAudioExtension(ext))
        {
            error = $"错误：不支持的音频格式 '.{ext}'（支持 mp3/wav/m4a/flac/ogg/webm 等）";
            return null;
        }

        return fullPath;
    }

    /// <summary>判断扩展名是否为受支持的音频格式。</summary>
    internal static bool IsSupportedAudioExtension(string ext)
        => SupportedExtensions.Contains(ext.ToLowerInvariant());

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "mp3", "wav", "m4a", "mp4", "mpeg", "mpga", "ogg", "oga", "webm",
        "flac", "aac", "wma", "opus", "amr", "mka", "aiff", "aif", "caf",
    };

    /// <summary>扩展名 → MIME 类型（用于 multipart 上传）。</summary>
    internal static string MapContentType(string ext)
    {
        return ext.ToLowerInvariant() switch
        {
            "mp3" or "mpeg" or "mpga" => "audio/mpeg",
            "wav" => "audio/wav",
            "m4a" or "mp4" => "audio/mp4",
            "ogg" or "oga" or "opus" => "audio/ogg",
            "webm" => "audio/webm",
            "flac" => "audio/flac",
            "aac" => "audio/aac",
            "wma" => "audio/x-ms-wma",
            "amr" => "audio/amr",
            "aiff" or "aif" => "audio/aiff",
            "caf" => "audio/x-caf",
            _ => "application/octet-stream",
        };
    }

}
