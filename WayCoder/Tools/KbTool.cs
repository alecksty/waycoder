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
        "遇到不熟悉的术语或疑似 bug 时用 search；遇到具体报错/失败时用 diagnose（召回知识库 + git 历史修复中同类错误的已知解法）。" +
        "query 为搜索关键词或错误文本（支持中文）。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("action", JNode.Object()
                .Set("type", "string")
                .Set("description", "操作: search 检索条目 | diagnose 诊断报错"))
            .Set("query", JNode.Object()
                .Set("type", "string")
                .Set("description", "搜索关键词或错误文本（支持中文），如: 终端尺寸 0、AOT 反射、git force push")))
        .Set("required", JNode.Array().Add("query"));

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var action = arguments.GetValueOrDefault("action")?.ToString() ?? "search";
        var query = arguments.GetValueOrDefault("query")?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(query))
            return "错误：需要提供 query 搜索关键词或错误文本。";

        if (action.Equals("diagnose", StringComparison.OrdinalIgnoreCase))
            return await KbIndex.DiagnoseError(query, 3);

        var hits = KbIndex.Search(query, 5);
        if (hits.Count == 0)
            return "知识库暂无相关条目（可用 /kb mine 提炼，或 /kb save 手动记录）。";

        var sb = new StringBuilder($"📚 知识库匹配 {hits.Count} 条：\n");
        foreach (var (e, score) in hits)
        {
            sb.AppendLine($"■ {e.Description}〔{KbIndex.KindLabel(e.Kind)}·相关度 {score:F2}〕");
            var preview = e.Content.ReplaceLineEndings(" ");
            if (preview.Length > 400) preview = ContextManager.TruncateByRunes(preview, 400);
            sb.AppendLine($"  {preview}");
        }
        return sb.ToString();
    }
}
