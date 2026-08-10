namespace WayCoder.Tools;

/// <summary>
/// 切换工作目录工具 —— 纯 C# 实现。
/// 更新 BashTool 的 AsyncLocal cwd 追踪。
/// </summary>
public class CdTool : ITool
{
    public string Name => "cd";
    public string Description => "切换当前工作目录。支持相对路径和绝对路径。返回切换后的完整路径。纯 C# 实现。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["path"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "目标目录路径（相对或绝对）",
            },
        },
        ["required"] = new JsonArray("path"),
    };

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var path = arguments.GetValueOrDefault("path")?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult("错误：path 参数不能为空");

        try
        {
            var current = BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory();

            // 处理 ~ 展开
            if (path.StartsWith('~'))
                path = path.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

            var fullPath = Path.GetFullPath(Path.Combine(current, path));

            if (!Directory.Exists(fullPath))
                return Task.FromResult($"错误：目录不存在 — {fullPath}");

            BashTool.CurrentCwd.Value = fullPath;
            return Task.FromResult($"✔ 工作目录: {fullPath}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"cd 错误：{ex.GetType().Name}: {ex.Message}");
        }
    }
}
