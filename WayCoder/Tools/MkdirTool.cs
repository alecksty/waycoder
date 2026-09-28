namespace WayCoder.Tools;

/// <summary>
/// 创建目录工具 —— 纯 C# 实现。
/// 递归创建，自动处理已存在的情况。
/// </summary>
public class MkdirTool : ITool
{
    public string Name => "mkdir";
    public ToolExecutionMode ExecutionMode => ToolExecutionMode.Exclusive;
    public string Description => L.Pick("创建目录（递归）。纯 C# 实现，自动创建所有父目录，已存在时不报错。", "Create a directory (recursive). Pure C# implementation. Automatically creates all parent directories; does not error if the directory already exists.");

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("path", JNode.Param("string", L.Pick("要创建的目录路径（相对或绝对）", "Directory path to create (relative or absolute)."))))
        .Set("required", JNode.Array("path"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var path = arguments.GetValueOrDefault("path")?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult("错误：path 参数不能为空");

        try
        {
            var fullPath = CwdContext.Resolve(path); // cd 后相对路径基于被跟踪工作目录
            if (Directory.Exists(fullPath))
                return Task.FromResult($"✔ 目录已存在: {fullPath}");

            Directory.CreateDirectory(fullPath);
            return Task.FromResult($"✔ 已创建目录: {fullPath}");
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolErrors.Error("mkdir ", ex));
        }
    }
}
