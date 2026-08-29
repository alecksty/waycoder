using System.Text;
using WayCoder.Infra;

namespace WayCoder.Tools;

/// <summary>
/// 符号反向索引查询 —— 按符号名一步定位定义位置（类/函数/方法等）。
///
/// 复用 <see cref="RepoMapGenerator.FindSymbol"/> 的「符号名 → 文件:行号」反向索引，
/// 省去 grep 试错往返。纯 C# 跨平台，桌面 + 移动端均注册。
/// </summary>
public class SymbolsTool : ITool
{
    public string Name => "symbols";
    public string Description => "按符号名查询定义位置（类/函数/方法等），返回文件路径与行号。用于快速定位符号定义，省去 grep 试错。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("name", JNode.Object()
                .Set("type", "string")
                .Set("description", "要查找的符号名（类名/函数名/方法名，大小写不敏感，如 'GrepTool' 或 'ExecuteAsync'）"))
            .Set("path", JNode.Object()
                .Set("type", "string")
                .Set("description", "项目根目录（默认：当前仓库根目录）")))
        .Set("required", JNode.Array().Add("name"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var name = arguments.GetValueOrDefault("name")?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(name))
            return Task.FromResult("错误：请提供要查找的符号名。");

        string? root = null;
        var path = arguments.GetValueOrDefault("path")?.ToString();
        if (!string.IsNullOrWhiteSpace(path))
            root = Path.GetFullPath(path, CwdContext.Current.Value ?? Directory.GetCurrentDirectory());

        try
        {
            var locations = RepoMapGenerator.FindSymbol(name, root);
            if (locations.Count == 0)
                return Task.FromResult($"未找到符号「{name}」。");

            var sb = new StringBuilder();
            foreach (var loc in locations)
                sb.AppendLine($"{loc.RelativePath}:{loc.Line}  [{loc.Kind}]");
            return Task.FromResult(sb.ToString().TrimEnd());
        }
        catch (Exception ex)
        {
            return Task.FromResult($"错误：符号查询失败 — {ex.Message}");
        }
    }
}
