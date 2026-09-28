namespace WayCoder.Tools;

/// <summary>
/// 终止后台运行的任务。
/// 对应 Crush 的 job_kill 工具。
/// </summary>
public class JobKillTool : ITool
{
    public string Name => "job_kill";
    public string Description => L.Pick("终止指定的后台任务。仅能终止仍在运行的任务（已完成的任务无法终止）。", "Terminate a specified background task. Only tasks that are still running can be terminated (completed tasks cannot).");

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("shell_id", JNode.Param("string", L.Pick("要终止的后台任务的 shell ID", "Shell ID of the background task to terminate."))))
        .Set("required", JNode.Array("shell_id"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var shellId = arguments.GetValueOrDefault("shell_id")?.ToString() ?? "";

        if (string.IsNullOrEmpty(shellId))
            return Task.FromResult(L.Pick("错误：需要提供 shell_id 参数",
                                          "Error: the shell_id parameter is required"));

        if (!int.TryParse(shellId, out var id))
            return Task.FromResult(L.Pick($"错误：无效的 shell_id: {shellId}",
                                          $"Error: invalid shell_id: {shellId}"));

        return Task.FromResult(BackgroundTaskManager.Kill(id));
    }
}
