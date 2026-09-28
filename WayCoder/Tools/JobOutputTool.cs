namespace WayCoder.Tools;

/// <summary>
/// 读取后台任务的输出。任务在后台异步运行，此工具用于轮询获取结果。
/// 对应 Crush 的 job_output 工具。
/// </summary>
public class JobOutputTool : ITool
{
    /// <summary>后台任务输出＝shell 进程原始字节 → 命令行文本渲染</summary>
    public bool RawOutput => true;

    public string Name => "job_output";
    public string Description => L.Pick("读取后台运行任务的最新输出。使用 bash 的 run_in_background 参数启动的任务可通过此工具查询结果。", "Read the latest output of a background task. Tasks started with the bash tool's run_in_background parameter can be polled through this tool.");

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("shell_id", JNode.Param("string", L.Pick("后台任务的 shell ID（由 bash 工具的 run_in_background 模式返回）", "Shell ID of the background task (returned by the bash tool's run_in_background mode)."))))
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

        var output = BackgroundTaskManager.GetOutput(id);
        return Task.FromResult(string.IsNullOrEmpty(output)
            ? L.Pick("（任务仍在运行，暂无输出）", "(Task is still running, no output yet)")
            : output);
    }
}
