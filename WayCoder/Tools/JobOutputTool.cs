namespace WayCoder.Tools;

/// <summary>
/// 读取后台任务的输出。任务在后台异步运行，此工具用于轮询获取结果。
/// 对应 Crush 的 job_output 工具。
/// </summary>
public class JobOutputTool : ITool
{
    public string Name => "job_output";
    public string Description => "读取后台运行任务的最新输出。使用 bash 的 run_in_background 参数启动的任务可通过此工具查询结果。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("shell_id", JNode.Object()
                .Set("type", "string")
                .Set("description", "后台任务的 shell ID（由 bash 工具的 run_in_background 模式返回）")))
        .Set("required", JNode.Array().Add("shell_id"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var shellId = arguments.GetValueOrDefault("shell_id")?.ToString() ?? "";

        if (string.IsNullOrEmpty(shellId))
            return Task.FromResult("错误：需要提供 shell_id 参数");

        if (!int.TryParse(shellId, out var id))
            return Task.FromResult($"错误：无效的 shell_id: {shellId}");

        var output = BackgroundTaskManager.GetOutput(id);
        return Task.FromResult(string.IsNullOrEmpty(output) ? "（任务仍在运行，暂无输出）" : output);
    }
}
