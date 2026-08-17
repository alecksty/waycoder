using WayCoder.Infra;

namespace WayCoder.Tools;

/// <summary>
/// 工具执行模式 —— 决定多工具调用时能否并发执行。
/// </summary>
public enum ToolExecutionMode
{
    /// <summary>可与其它 Parallel 工具并发执行（默认，适合只读/独立工具）</summary>
    Parallel,

    /// <summary>必须独占执行，不能与其它工具并发（适合有共享状态/副作用的工具）</summary>
    Exclusive,
}

/// <summary>
/// 工具接口。继承此接口即可添加新能力。
/// </summary>
public interface ITool
{
    /// <summary>工具名称，用于 LLM function calling</summary>
    string Name { get; }

    /// <summary>工具描述</summary>
    string Description { get; }

    /// <summary>函数参数的 JSON Schema</summary>
    JNode Parameters { get; }

    /// <summary>
    /// 执行模式：Parallel 可与其它工具并发，Exclusive 必须独占执行。
    /// 默认 Parallel；有共享状态/副作用的工具应覆写为 Exclusive。
    /// </summary>
    ToolExecutionMode ExecutionMode => ToolExecutionMode.Parallel;

    /// <summary>
    /// 运行工具并返回文本结果。
    /// </summary>
    Task<string> ExecuteAsync(Dictionary<string, object?> arguments);

    /// <summary>
    /// 返回 OpenAI function-calling 格式的 schema。
    /// </summary>
    JNode Schema()
    {
        // 深拷贝 Parameters 避免共享节点被二次修改
        var clonedParams = Parameters.Clone() ?? JNode.Object();
        return JNode.Object()
            .Set("type", "function")
            .Set("function", JNode.Object()
                .Set("name", Name)
                .Set("description", Description)
                .Set("parameters", clonedParams));
    }
}

/// <summary>
/// 可取消工具接口。实现此接口的工具在 Agent 中断（如 Web 停止按钮 / Ctrl+C）时
/// 会收到取消令牌，从而能真正终止正在运行的子进程等长耗时操作。
/// </summary>
public interface ICancellableTool
{
    /// <summary>
    /// 运行工具并返回文本结果，支持取消。
    /// </summary>
    Task<string> ExecuteAsync(Dictionary<string, object?> arguments, CancellationToken cancellationToken);
}
