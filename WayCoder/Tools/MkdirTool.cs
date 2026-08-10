namespace WayCoder.Tools;

/// <summary>
/// 创建目录工具 —— 纯 C# 实现。
/// 递归创建，自动处理已存在的情况。
/// </summary>
public class MkdirTool : ITool
{
    public string Name => "mkdir";
    public string Description => "创建目录（递归）。纯 C# 实现，自动创建所有父目录，已存在时不报错。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["path"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "要创建的目录路径（相对或绝对）",
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
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath))
                return Task.FromResult($"✔ 目录已存在: {fullPath}");

            Directory.CreateDirectory(fullPath);
            return Task.FromResult($"✔ 已创建目录: {fullPath}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"mkdir 错误：{ex.GetType().Name}: {ex.Message}");
        }
    }
}
