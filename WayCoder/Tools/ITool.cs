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

    /// <summary>
    /// 输出是否为「外部进程的原始字节」（bash / git / sqlite / ps / 测试运行器 / lint / lsp …）。
    ///
    /// true 时**各端必须按命令行文本渲染**：UTF-8 编码、等宽字体、保留换行与列对齐，并解码裸 ANSI；
    /// **不得当 markdown/富文本解析** —— shell 输出里的 `#`、`- `、`|` 会被渲染成标题/列表/表格，
    /// 大字号标题 + 折叠的连续空格把等宽对齐全毁（Web 端实测：工具气泡与 `!` 直通气泡显示不一致）。
    ///
    /// 判据是「**输出从哪来**」：进程字节 → true；工具自产的结构化文本（«» 标记、diff、列表）→ false。
    /// 这是唯一真源，前端据此分派，不要在前端再维护一份工具名单。
    /// </summary>
    bool RawOutput => false;

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
