namespace CoreCoderSharp.Tools;

/// <summary>
/// 记忆工具 —— Agent 可读写的持久化项目知识。
/// 存储在 .corecoder/memory/*.md（结构化 frontmatter 格式），跨会话保留。
/// 首次使用时自动从旧 memory.md 迁移。支持 read（读取全部或指定 name）、
/// write（写入/更新）、search（搜索）、delete（删除）。
/// </summary>
public class MemoryTool : ITool
{
    public string Name => "memory";
    public string Description =>
        "读写持久化项目记忆（.corecoder/memory/ 结构化格式）。" +
        "支持 read（读取全部或指定 name）、write（写入新记忆或更新已存在 name）、" +
        "search（搜索）、delete（删除）、share（标记团队共享并推送）、" +
        "unshare（取消共享）、sync（拉取远程共享记忆）。" +
        "write 时 name 为 kebab-case 标识，description 为一行摘要，" +
        "type 可选 user|feedback|project|reference，content 为正文。用于跨会话保留关键信息、项目约定、用户偏好等。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["action"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "操作: read | write | search | delete | share | unshare | sync",
            },
            ["name"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "记忆标识（kebab-case）。write 时创建/更新；read 时读取单条；delete 时删除",
            },
            ["description"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "一行摘要（write 时需要）",
            },
            ["type"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "记忆类型（write 时可选）: user | feedback | project | reference",
            },
            ["content"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "正文内容（write 时需要），或搜索关键词（search 时需要）",
            },
        },
        ["required"] = new JsonArray("action"),
    };

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var action = arguments.GetValueOrDefault("action")?.ToString() ?? "read";
        var name = arguments.GetValueOrDefault("name")?.ToString() ?? "";
        var description = arguments.GetValueOrDefault("description")?.ToString() ?? "";
        var type = arguments.GetValueOrDefault("type")?.ToString() ?? "reference";
        var content = arguments.GetValueOrDefault("content")?.ToString() ?? "";

        // 首次使用自动迁移旧格式 memory.md（幂等：已有结构化记忆或旧文件不存在时跳过）
        try { StructuredMemory.MigrateFromOldFormat(); } catch { }

        return action switch
        {
            "write" => WriteMemory(name, description, content, type),
            "search" => SearchMemory(content),
            "delete" => DeleteMemory(name),
            "share" => await ShareMemory(name),
            "unshare" => UnshareMemory(name),
            "sync" => await SyncMemory(content),
            _ => ReadMemory(name),
        };
    }

    private static string WriteMemory(string name, string description, string content, string type)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "错误：write 需要提供 name（kebab-case 标识）。";
        if (string.IsNullOrWhiteSpace(content))
            return "错误：write 需要提供 content 正文。";

        var existing = StructuredMemory.Get(name);
        if (existing != null)
        {
            StructuredMemory.Update(name, description.Length > 0 ? description : existing.Description,
                type, content);
            return $"✅ 已更新记忆 [{name}]";
        }

        StructuredMemory.Create(name,
            description.Length > 0 ? description : content.Length > 60 ? content[..60] + "..." : content,
            type, content);
        return $"✅ 已记录到项目记忆 [{name}]";
    }

    private static string ReadMemory(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var entry = StructuredMemory.Get(name);
            if (entry == null) return $"（未找到记忆 [{name}]）";
            return $"# {entry.Description} (`{entry.Type}`)\n\n{entry.Content}";
        }

        var all = StructuredMemory.ListAll();
        if (all.Count == 0)
            return "（暂无记忆。Agent 可通过 memory write 工具记录关键信息。）";

        var lines = new List<string> { $"共 {all.Count} 条记忆:" };
        foreach (var e in all)
        {
            var preview = e.Content.Length > 80 ? e.Content[..80] + "..." : e.Content;
            lines.Add($"  [{e.Name}] ({e.Type}) {e.Description} — {preview}");
        }
        return string.Join('\n', lines);
    }

    private static string SearchMemory(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "错误：search 需要提供 content 作为搜索关键词。";

        var results = StructuredMemory.Search(query);
        if (results.Count == 0)
            return $"未找到 \"{query}\"";

        var lines = new List<string> { $"搜索 \"{query}\" ({results.Count} 条相关记忆):" };
        foreach (var e in results)
        {
            var preview = e.Content.Length > 100 ? e.Content[..100] + "..." : e.Content;
            lines.Add($"  [{e.Name}] ({e.Type}) {e.Description} — {preview}");
        }
        return string.Join('\n', lines);
    }

    private static string DeleteMemory(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "错误：delete 需要提供 name。";
        return StructuredMemory.Delete(name)
            ? $"✅ 已删除记忆 [{name}]"
            : $"（未找到记忆 [{name}]）";
    }

    private static async Task<string> ShareMemory(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "错误：share 需要提供 name。";

        if (!SharedMemoryManager.IsGitRepo())
            return "❌ 当前目录不在 git 仓库中，无法共享记忆。团队知识库共享需要 git 仓库。";

        return await SharedMemoryManager.ShareAsync(name);
    }

    private static string UnshareMemory(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "错误：unshare 需要提供 name。";
        return SharedMemoryManager.Unshare(name);
    }

    private static async Task<string> SyncMemory(string direction)
    {
        if (!SharedMemoryManager.IsGitRepo())
            return "❌ 当前目录不在 git 仓库中。";

        if (direction == "push")
        {
            var result = await SharedMemoryManager.PushSharedAsync();
            return result.Success ? $"✅ {result.Message}" : $"❌ {result.Error}";
        }
        else
        {
            // 默认 pull
            var result = await SharedMemoryManager.PullSharedAsync();
            if (result.Success)
            {
                var parts = new List<string> { $"✅ {result.Message}" };
                foreach (var f in result.NewFiles)
                    parts.Add($"  ➕ 新增: {f}");
                foreach (var f in result.UpdatedFiles)
                    parts.Add($"  🔄 更新: {f}");
                return string.Join('\n', parts);
            }
            return $"❌ {result.Error}";
        }
    }
}
