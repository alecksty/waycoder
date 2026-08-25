using System.Text;

namespace WayCoder.Tools;

/// <summary>
/// 知识库工具 —— Agent 在聊天中遇到不熟悉的术语或疑似 bug 时，检索全局编程知识库
/// （~/.waycoder/kb/，含历史踩坑 / bug 修复 / 个人习惯 / 欠缺知识 / 代码片段）。
/// 复用 <see cref="KbIndex"/> 的 TF-IDF 检索，返回按相关度排序的条目。
/// </summary>
public class KbTool : ITool
{
    public string Name => "kb";
    public string Description =>
        "检索全局编程知识库（~/.waycoder/kb/：历史踩坑、复杂 bug 修复、个人使用习惯、欠缺知识、代码片段）。" +
        "当遇到不熟悉的术语、疑似 bug、或需要过往经验时，调用 search 查询相关条目。query 为搜索关键词（支持中文）。" +
        "返回按相关度排序的条目摘要（含类别与正文预览）。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("action", JNode.Object()
                .Set("type", "string")
                .Set("description", "操作: search"))
            .Set("query", JNode.Object()
                .Set("type", "string")
                .Set("description", "搜索关键词（支持中文），如: 终端尺寸 0、AOT 反射、git force push")))
        .Set("required", JNode.Array().Add("query"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var query = arguments.GetValueOrDefault("query")?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult("错误：需要提供 query 搜索关键词。");

        var hits = KbIndex.Search(query, 5);
        if (hits.Count == 0)
            return Task.FromResult("知识库暂无相关条目（可用 /kb mine 提炼，或 /kb save 手动记录）。");

        var sb = new StringBuilder($"📚 知识库匹配 {hits.Count} 条：\n");
        foreach (var (e, score) in hits)
        {
            sb.AppendLine($"■ {e.Description}〔{KbIndex.KindLabel(e.Kind)}·相关度 {score:F2}〕");
            var preview = e.Content.ReplaceLineEndings(" ");
            if (preview.Length > 400) preview = ContextManager.TruncateByRunes(preview, 400);
            sb.AppendLine($"  {preview}");
        }
        return Task.FromResult(sb.ToString());
    }
}
