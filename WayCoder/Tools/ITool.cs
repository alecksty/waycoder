using WayCoder.Infra;

namespace WayCoder.Tools;

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
