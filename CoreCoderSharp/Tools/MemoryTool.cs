namespace CoreCoderSharp.Tools;

/// <summary>
/// 记忆工具 —— Agent 可读写的持久化项目知识。
/// 存储在 .corecoder/memory.md，跨会话保留。
/// </summary>
public class MemoryTool : ITool
{
    public string Name => "memory";
    public string Description => "读写持久化项目记忆（.corecoder/memory.md）。支持 read（读取全部）、write（追加）、search（搜索）。用于跨会话保留关键信息、项目约定、用户偏好等。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["action"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "操作: read | write | search",
            },
            ["content"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "要写入的内容（write 时需要），或搜索关键词（search 时需要）",
            },
        },
        ["required"] = new JsonArray("action"),
    };

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var action = arguments.GetValueOrDefault("action")?.ToString() ?? "read";
        var content = arguments.GetValueOrDefault("content")?.ToString() ?? "";

        return Task.FromResult(action switch
        {
            "write" => MemoryStore.Append(content),
            "search" => MemoryStore.Search(content),
            _ => MemoryStore.Read(),
        });
    }
}
