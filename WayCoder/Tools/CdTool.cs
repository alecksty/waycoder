namespace WayCoder.Tools;

/// <summary>
/// 切换工作目录工具 —— 纯 C# 实现。
/// 更新 BashTool 的 AsyncLocal cwd 追踪。
/// </summary>
public class CdTool : ITool
{
    public string Name => "cd";
    public string Description => "切换当前工作目录。支持相对路径和绝对路径。返回切换后的完整路径。纯 C# 实现。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("path", JNode.Object()
                .Set("type", "string")
                .Set("description", "目标目录路径（相对或绝对）")))
        .Set("required", JNode.Array().Add("path"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var path = arguments.GetValueOrDefault("path")?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult("错误：path 参数不能为空");

        try
        {
            var current = CwdContext.Current.Value ?? Directory.GetCurrentDirectory();

            // 处理 ~ 展开：仅前缀 ~ 或 ~/ 展开为 home，`~user`/路径中段 ~ 保持原样
            if (path.StartsWith('~'))
                path = ExpandHome(path);

            var fullPath = Path.GetFullPath(Path.Combine(current, path));

            if (!Directory.Exists(fullPath))
                return Task.FromResult($"错误：目录不存在 — {fullPath}");

            CwdContext.Current.Value = fullPath;
            return Task.FromResult($"✔ 工作目录: {fullPath}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"cd 错误：{ex.GetType().Name}: {ex.Message}");
        }

        static string ExpandHome(string p)
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (p == "~") return home;
            if (p.StartsWith("~/") || p.StartsWith("~\\")) return Path.Combine(home, p[2..]);
            return p; // ~user 等形式保持原样
        }
    }
}
