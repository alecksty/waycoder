namespace WayCoder.Tools;

/// <summary>
/// 打印工作目录 —— 显示当前 BashTool 追踪的 cwd。
/// 纯 C# 实现，无需 Shell。
/// </summary>
public class PwdTool : ITool
{
    public string Name => "pwd";
    public string Description => "显示当前工作目录的完整路径。纯 C# 实现。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject(),
        ["required"] = new JsonArray(),
    };

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var cwd = BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory();
        return Task.FromResult(cwd);
    }
}
