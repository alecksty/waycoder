namespace WayCoder.Tools;

/// <summary>
/// 查看图片工具 —— 把本地图片附加到下一轮请求，让支持 vision 的模型「看到」图。
///
/// 配合 screenshot 工具使用：先用 screenshot 抓屏拿到 PNG 路径，再调 view_image 查看。
/// 当前模型不支持 vision 时返回提示，不阻塞流程。
/// </summary>
public class ViewImageTool : ITool
{
    public string Name => "view_image";
    public string Description =>
        "查看一张本地图片（PNG/JPG 等），把它附加到下一轮请求，让支持 vision 的模型直接读取图片内容。" +
        "配合 screenshot 工具：先 screenshot 抓屏得到 PNG 路径，再 view_image 该路径即可「看到」画面。" +
        "模型不支持 vision 时会返回提示并建议用 OCR/文本方式。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("path", JNode.Object()
                .Set("type", "string")
                .Set("description", "图片文件路径（如 screenshot 返回的 PNG 路径）"))
            .Set("question", JNode.Object()
                .Set("type", "string")
                .Set("description", "针对这张图想问的问题（默认「请描述这张图片的内容」）")))
        .Set("required", JNode.Array().Add("path"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var path = arguments.GetValueOrDefault("path")?.ToString() ?? "";
        var question = arguments.GetValueOrDefault("question")?.ToString() ?? "请描述这张图片的内容";
        var agentId = arguments.GetValueOrDefault("_agent_id")?.ToString() ?? "main";

        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult("错误：path 参数不能为空");

        var fullPath = Path.GetFullPath(path, BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory()); // cd 后相对路径基于被跟踪工作目录
        if (!File.Exists(fullPath))
            return Task.FromResult($"错误：图片不存在 — {fullPath}");

        // 门控：按当前 Agent 实际生效模型判断（注入的 _model/_base_url），
        // 而非全局 Config.Model —— 槽位独立模型 / 回退链下仍能正确识别 vision；未注入时回退 Config
        var model = arguments.GetValueOrDefault("_model")?.ToString() is { Length: > 0 } m ? m : Config.Instance.Model;
        var baseUrl = arguments.GetValueOrDefault("_base_url")?.ToString() is { Length: > 0 } b ? b : Config.Instance.BaseUrl;
        if (!ModelCatalog.ResolveSupportsVision(model, baseUrl))
            return Task.FromResult(
                $"⚠ 当前模型 {model} 不支持图片输入（vision）。\n" +
                $"可用 bash 查看文件：ls -la \"{fullPath}\"\n" +
                $"或用 /model 切换到支持 vision 的模型（gpt-4o / gpt-5 / claude / gemini）后再试。");

        try
        {
            var bytes = File.ReadAllBytes(fullPath);
            if (bytes.Length > 5 * 1024 * 1024)
                return Task.FromResult($"错误：图片过大（{bytes.Length / 1024} KB），vision 通常限制 5MB 以内。");

            LLM.QueueImage(agentId, fullPath);
            var sizeKb = bytes.Length / 1024.0;
            return Task.FromResult(
                $"✅ 图片已附加（{sizeKb:F0} KB），将在下一轮请求中发送给 {model} 查看。\n" +
                $"问题：{question}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"读取图片出错：{ex.GetType().Name}: {ex.Message}");
        }
    }
}
