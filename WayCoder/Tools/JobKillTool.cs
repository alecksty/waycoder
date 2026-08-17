namespace WayCoder.Tools;

/// <summary>
/// 终止后台运行的任务。
/// 对应 Crush 的 job_kill 工具。
/// </summary>
public class JobKillTool : ITool
{
    public string Name => "job_kill";
    public string Description => "终止指定的后台任务。仅能终止仍在运行的任务（已完成的任务无法终止）。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("shell_id", JNode.Object()
                .Set("type", "string")
                .Set("description", "要终止的后台任务的 shell ID")))
        .Set("required", JNode.Array().Add("shell_id"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var shellId = arguments.GetValueOrDefault("shell_id")?.ToString() ?? "";

        if (string.IsNullOrEmpty(shellId))
            return Task.FromResult("错误：需要提供 shell_id 参数");

        if (!int.TryParse(shellId, out var id))
            return Task.FromResult($"错误：无效的 shell_id: {shellId}");

        return Task.FromResult(BackgroundTaskManager.Kill(id));
    }
}
