using System.Collections.Concurrent;
using System.Text;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;

namespace WayCoder.UI.Web;

/// <summary>
/// 浏览器聊天桥接层：把 <see cref="Agent.ChatAsync"/> 的流式回调（onToken/onTool/onToolOutput）
/// 转为 SSE 事件广播给浏览器，接收浏览器 POST 的输入入队，支持中断。
/// 对标 DeepSeek Harness Web UI：多槽位（F1-F10）、换模型、输 key、设置、黑白主题。
/// </summary>
public sealed partial class WebChatServer : UxHelper.IWebInteraction
{

    // ═══════════════════════════════════════════════════════════
    //  Web 交互桥（UxHelper.IWebInteraction）
    //  生成 requestId → 广播 SSE "ask" → 等待 POST /answer 应答
    // ═══════════════════════════════════════════════════════════

    private string NextId() => Interlocked.Increment(ref _answerId).ToString();

    /// <summary>文本输入。</summary>
    public Task<string?> AskAsync(string prompt, string? defaultValue, int timeoutMs)
    {
        var payload = JNode.Object()
            .Set("requestId", NextId())
            .Set("kind", "text")
            .Set("title", prompt)
            .Set("default", defaultValue);
        return WaitAnswerAsync(payload, timeoutMs);
    }

    /// <summary>单选。</summary>
    public Task<string?> SelectAsync(string title, List<string> choices, int timeoutMs)
    {
        var payload = JNode.Object()
            .Set("requestId", NextId())
            .Set("kind", "select")
            .Set("title", title)
            .Set("choices", StringArray(choices));
        return WaitAnswerAsync(payload, timeoutMs);
    }

    /// <summary>多选。</summary>
    public Task<List<string>?> MultiSelectAsync(string title, List<string> choices, int timeoutMs)
    {
        var payload = JNode.Object()
            .Set("requestId", NextId())
            .Set("kind", "multi")
            .Set("title", title)
            .Set("choices", StringArray(choices));
        return WaitAnswerMultiAsync(payload, timeoutMs);
    }

    private static JNode StringArray(List<string> items)
    {
        var arr = JNode.Array();
        foreach (var s in items) arr.Add(s);
        return arr;
    }

    /// <summary>确认框。返回 0=是 1=总是允许 2=否（与 UxHelper.Confirm 对齐）。</summary>
    public async Task<int> ConfirmAsync(string title, string message, bool allowAll, int timeoutMs)
    {
        var payload = JNode.Object()
            .Set("requestId", NextId())
            .Set("kind", "confirm")
            .Set("title", title)
            .Set("message", message)
            .Set("allowAll", allowAll);
        var result = await WaitAnswerAsync(payload, timeoutMs);
        // 前端回传字符串 "0"/"1"/"2" 或 "yes"/"all"/"no"
        return result switch
        {
            "0" or "yes" => 0,
            "1" or "all" => 1,
            _ => 2,
        };
    }

    /// <summary>Diff 预览：广播逐 hunk diff，等待用户返回「接受/拒绝/部分接受」。超时/取消返回 null（视为拒绝）。</summary>
    public async Task<DiffConfirmResult?> DiffConfirmAsync(string filePath, List<DiffPreview.Hunk> hunks, int timeoutMs)
    {
        var payload = JNode.Object()
            .Set("requestId", NextId())
            .Set("kind", "diff")
            .Set("title", $"Diff 预览: {filePath}")
            .Set("hunks", Json.Parse(SerializeHunks(hunks)) ?? JNode.Array());
        var raw = await WaitAnswerAsync(payload, timeoutMs);
        return ParseDiffAnswer(raw);
    }

    /// <summary>把 hunk 列表序列化为前端可渲染的 JSON 数组。纯逻辑便于自测。</summary>
    public static string SerializeHunks(List<DiffPreview.Hunk> hunks)
    {
        var arr = JNode.Array();
        foreach (var h in hunks)
        {
            var lines = JNode.Array();
            foreach (var l in h.Lines)
            {
                lines.Add(JNode.Object()
                    .Set("kind", l.Kind.ToString())
                    .Set("text", l.Text)
                    .Set("oldLine", l.OldLine)
                    .Set("newLine", l.NewLine));
            }
            arr.Add(JNode.Object()
                .Set("header", h.Header)
                .Set("lines", lines));
        }
        return arr.ToJson();
    }

    /// <summary>
    /// 解析 diff 确认应答。应答为 JSON 字符串：{"decision":"accept|reject|partial","accepted":[索引]}。
    /// 纯逻辑便于自测；null/空/非法 → null（调用方视为拒绝）。
    /// </summary>
    public static DiffConfirmResult? ParseDiffAnswer(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        if (!Json.TryParse(json, out var node) || node == null) return null;

        var decision = node["decision"]?.AsString() ?? "";
        var result = new DiffConfirmResult();
        switch (decision)
        {
            case "accept":
                result.Decision = DiffPreview.Decision.AcceptAll;
                break;
            case "partial":
                result.Decision = DiffPreview.Decision.Partial;
                var acc = node["accepted"];
                if (acc != null && acc.Kind == JKind.Array)
                {
                    var set = new HashSet<int>();
                    foreach (var item in acc.Items)
                        if (item.Kind == JKind.Number) set.Add((int)Math.Round(item.AsNumber()));
                    result.AcceptedHunks = set;
                }
                break;
            default:
                result.Decision = DiffPreview.Decision.RejectAll;
                break;
        }
        return result;
    }

    /// <summary>广播提问并等待应答。超时返回 null（调用方视为取消/拒绝）。</summary>
    private async Task<string?> WaitAnswerAsync(JNode payload, int timeoutMs)
    {
        var id = payload["requestId"]!.AsString()!;
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingAnswers[id] = tcs;
        try
        {
            BroadcastTo(_currentSlot.Value, "ask", payload.ToJson());
            var delay = Task.Delay(timeoutMs > 0 ? timeoutMs : 60_000);
            var winner = await Task.WhenAny(tcs.Task, delay);
            if (winner == delay) return null; // 超时
            return await tcs.Task;
        }
        finally
        {
            _pendingAnswers.TryRemove(id, out _);
        }
    }

    /// <summary>多选：应答为逗号分隔的选中项，拆成列表返回。</summary>
    private async Task<List<string>?> WaitAnswerMultiAsync(JNode payload, int timeoutMs)
    {
        var result = await WaitAnswerAsync(payload, timeoutMs);
        if (result == null) return null;
        if (result.Length == 0) return new List<string>();
        return result.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    /// <summary>处理 POST /answer：把应答回填给对应提问，返回 JSON 结果。</summary>
    private string AnswerQuestion(string requestId, JNode? value)
    {
        if (!_pendingAnswers.TryGetValue(requestId, out var tcs))
            return Err("提问已超时或不存在");
        string answer;
        if (value == null || value.Kind == JKind.Null)
            answer = ""; // 空 = 取消
        else if (value.Kind == JKind.Array)
            answer = string.Join("\n", value.Items.Select(i => i.AsString() ?? ""));
        else
            answer = value.AsString() ?? "";
        tcs.TrySetResult(answer);
        return Ok();
    }

    // ═══════════════════════════════════════════════════════════
    //  多模态上传（图片入 vision 队列 / 音频转录为文字）
    // ═══════════════════════════════════════════════════════════

    /// <summary>上传文件落盘目录（跨平台临时目录）。</summary>
    private static readonly string UploadDir = Path.Combine(Path.GetTempPath(), "waycoder-uploads");

    private static int _uploadSeq;

    /// <summary>解析上传 kind（query 形如 kind=image|audio）。非法/缺失返回 null。</summary>
    public static string? ParseUploadKind(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;
        foreach (var part in query.Split('&'))
        {
            int eq = part.IndexOf('=');
            var key = eq >= 0 ? part[..eq] : part;
            var val = eq >= 0 ? part[(eq + 1)..] : "";
            if (!key.Equals("kind", StringComparison.OrdinalIgnoreCase)) continue;
            if (val.Equals("image", StringComparison.OrdinalIgnoreCase)) return "image";
            if (val.Equals("audio", StringComparison.OrdinalIgnoreCase)) return "audio";
            return null;
        }
        return null;
    }

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    { "png", "jpg", "jpeg", "gif", "webp", "bmp" };

    /// <summary>判断扩展名是否为受支持的图片格式。纯逻辑便于自测。</summary>
    public static bool IsImageExtension(string ext)
        => ImageExtensions.Contains(string.IsNullOrWhiteSpace(ext) ? "" : ext.TrimStart('.').ToLowerInvariant());

    /// <summary>从上传文件名提取安全扩展名（仅字母数字，超长或缺失回退 kind 默认）。</summary>
    public static string SafeExtension(string? fileName, string kind)
    {
        var ext = string.IsNullOrWhiteSpace(fileName) ? "" : Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        ext = new string(ext.Where(char.IsLetterOrDigit).ToArray());
        if (ext.Length == 0 || ext.Length > 8)
            return kind == "audio" ? "bin" : "png";
        return ext;
    }

    /// <summary>处理 POST /upload：落盘 + 图片入 vision 队列 / 音频转录。</summary>
    private HttpResponse HandleUpload(HttpRequest req)
    {
        var kind = ParseUploadKind(req.Query);
        if (kind == null)
            return HttpResponse.JsonBody(Err("缺少或非法 kind 参数（须为 image 或 audio）"));

        var fileName = SafeUnescape(req.Header("X-File-Name") ?? "upload.bin");
        var bytes = req.RawBody;
        if (bytes.Length == 0)
            return HttpResponse.JsonBody(Err("上传内容为空"));

        var ext = SafeExtension(fileName, kind);
        if (kind == "image")
        {
            if (!IsImageExtension(ext))
                return HttpResponse.JsonBody(Err($"不支持的图片格式 '.{ext}'（支持 png/jpg/jpeg/gif/webp/bmp）"));
            if (bytes.Length > 5 * 1024 * 1024)
                return HttpResponse.JsonBody(Err($"图片过大（{bytes.Length / 1024} KB），vision 上限 5MB"));
        }
        else
        {
            if (!TranscribeAudioTool.IsSupportedAudioExtension(ext))
                return HttpResponse.JsonBody(Err($"不支持的音频格式 '.{ext}'（支持 mp3/wav/m4a/ogg/webm 等）"));
            if (bytes.Length > 25 * 1024 * 1024)
                return HttpResponse.JsonBody(Err($"音频过大（{bytes.Length / 1024 / 1024} MB），Whisper 上限 25MB"));
        }

        try { Directory.CreateDirectory(UploadDir); } catch { }
        var seq = Interlocked.Increment(ref _uploadSeq);
        var path = Path.Combine(UploadDir, $"upload-{Environment.TickCount64}-{seq}.{ext}");
        try { File.WriteAllBytes(path, bytes); }
        catch (Exception ex) { return HttpResponse.JsonBody(Err($"保存文件失败：{ex.Message}")); }

        if (kind == "image")
        {
            var model = Config.Instance.Model;
            if (!ModelCatalog.ResolveSupportsVision(model, Config.Instance.BaseUrl))
                return HttpResponse.JsonBody(Err($"当前模型 {model} 不支持图片输入（vision），请切换支持 vision 的模型"));
            // 按客户端绑定槽位的 agentId 入队，隔离多槽位图片（与 view_image 工具的 _agent_id 路径一致）
            var slot = ResolveSlot(ParseClientQuery(req.Query));
            LLM.QueueImage(WebSlotAgentId(slot), path);
            return HttpResponse.JsonBody(JNode.Object()
                .Set("ok", true).Set("kind", "image").Set("path", path)
                .Set("name", fileName).Set("size", bytes.Length).ToJson());
        }

        // 音频：转录（复用 TranscribeAudioTool）
        var text = new TranscribeAudioTool()
            .ExecuteAsync(new Dictionary<string, object?> { ["path"] = path })
            .GetAwaiter().GetResult();
        try { File.Delete(path); } catch { } // 转录完成即清理临时文件（避免 %TEMP% 无界累积）
        if (IsTranscribeError(text))
            return HttpResponse.JsonBody(Err(text));
        return HttpResponse.JsonBody(JNode.Object()
            .Set("ok", true).Set("kind", "audio").Set("path", path)
            .Set("name", fileName).Set("size", bytes.Length).Set("text", text).ToJson());
    }

    /// <summary>转录结果是否为错误文本（成功返回任意转录内容，不含这些前缀）。纯逻辑便于自测。</summary>
    public static bool IsTranscribeError(string text)
        => text.StartsWith("错误", StringComparison.Ordinal)
        || text.StartsWith("转录失败", StringComparison.Ordinal)
        || text.StartsWith("转录出错", StringComparison.Ordinal)
        || text.StartsWith("转录返回空文本", StringComparison.Ordinal);

    // ═══════════════════════════════════════════════════════════
    //  JSON 辅助
    // ═══════════════════════════════════════════════════════════

    private static string JsonStr(string s) => JNode.Str(s).ToJson();

    /// <summary>校验 Origin 是否为本服务合法来源（CSRF 防护）。纯逻辑便于自测。</summary>
    /// <remarks>空 Origin（curl/SSE/同源导航）放行，非空必须匹配本机来源。浏览器跨源 fetch/form 必带 Origin（攻击者域名）故被拒。</remarks>
    public static bool IsTrustedOrigin(string? origin, int port)
    {
        if (string.IsNullOrEmpty(origin)) return true; // 非浏览器客户端（curl/SSE/同源导航）放行
        return origin.Equals($"http://127.0.0.1:{port}", StringComparison.OrdinalIgnoreCase)
            || origin.Equals($"http://localhost:{port}", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>CSRF 兜底：现代浏览器跨站请求会带 <c>Sec-Fetch-Site: cross-site</c>，即使漏带 Origin 也据此拦截。纯逻辑便于自测。</summary>
    public static bool IsCrossSite(string? secFetchSite)
        => string.Equals(secFetchSite, "cross-site", StringComparison.OrdinalIgnoreCase);

    private static string JsonTool(string name, string brief)
        => JNode.Object().Set("name", HtmlEscape(name)).Set("args", HtmlEscape(brief)).ToJson();

    /// <summary>SSE 客户端是否已满（纯逻辑，便于自测）。</summary>
    public static bool SseClientsFull(int count) => count >= MaxSseClients;

    /// <summary>待处理输入队列是否已满（纯逻辑，便于自测）。</summary>
    public static bool InputQueueFull(int count) => count >= MaxPendingInput;

    /// <summary>HTML 实体转义（防 XSS）：工具名/参数注入 innerHTML 前转义 &lt; &gt; &amp; " '。</summary>
    public static string HtmlEscape(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
    }

    private static string Ok() => JNode.Object().Set("ok", true).ToJson();

    private static string Err(string message) => JNode.Object().Set("ok", false).Set("error", message).ToJson();
}
