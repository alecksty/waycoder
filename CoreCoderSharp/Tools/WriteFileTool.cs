using System.Text;

namespace CoreCoderSharp.Tools;

/// <summary>
/// 文件创建 / 覆写。
/// </summary>
public class WriteFileTool : ITool
{
    public string Name => "write_file";
    public string Description => "创建新文件或完全覆写已有文件。对于已有文件的小改动，优先使用 edit_file。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["file_path"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "文件路径",
            },
            ["content"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "要写入的完整文件内容",
            },
        },
        ["required"] = new JsonArray("file_path", "content"),
    };

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var filePath = arguments.GetValueOrDefault("file_path")?.ToString() ?? "";
        var content = arguments.GetValueOrDefault("content")?.ToString() ?? "";
        var agentId = arguments.GetValueOrDefault("_agent_id")?.ToString() ?? "main";

        return await ExecuteAsync(filePath, content, agentId);
    }

    private static async Task<string> ExecuteAsync(string filePath, string content, string agentId)
    {
        var path = Path.GetFullPath(filePath);

        // 文件锁检查
        if (!FileLockManager.TryAcquire(path, agentId))
        {
            var lockInfo = FileLockManager.GetLockInfo(path);
            return $"❌ 文件被锁定: {lockInfo?.Status ?? "未知"} — 请等待锁释放或使用其他文件名";
        }

        try
        {
            var dir = Path.GetDirectoryName(path);
            if (dir != null) Directory.CreateDirectory(dir);
            File.WriteAllText(path, content, Encoding.UTF8);

            EditFileTool.ChangedFiles.Add(path);

            var lineCount = content.Count(c => c == '\n') + (string.IsNullOrEmpty(content) || content.EndsWith('\n') ? 0 : 1);
            return $"已写入 {lineCount} 行到 {filePath}";
        }
        catch (Exception ex)
        {
            return $"错误：{ex.Message}";
        }
        finally
        {
            FileLockManager.Release(path, agentId);
        }
    }
}
