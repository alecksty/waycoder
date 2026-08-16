using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Edit;

namespace WayCoder.Tools;

/// <summary>
/// 文件创建 / 覆写。
/// </summary>
public class WriteFileTool : ITool
{
    public string Name => "write_file";
    public ToolExecutionMode ExecutionMode => ToolExecutionMode.Exclusive;
    public string Description => "创建新文件或完全覆写已有文件。仅用于新建文件或整体重写；对已有文件的小改动请用 edit_file（更安全，不会意外丢失内容）。覆写已有文件前必须先 read_file 了解当前内容。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("file_path", JNode.Object()
                .Set("type", "string")
                .Set("description", "文件路径（绝对路径）。新建文件或完全替换已有文件。仅用于新建或整体重写——局部编辑请用 edit_file。"))
            .Set("content", JNode.Object()
                .Set("type", "string")
                .Set("description", "要写入的完整文件内容。将完全替换目标文件的全部内容。"))
            .Set("append", JNode.Object()
                .Set("type", "boolean")
                .Set("description", "设为 true 追加到文件末尾（不覆写），默认 false 覆写"))
            .Set("encoding", JNode.Object()
                .Set("type", "string")
                .Set("description", "文件编码，默认 utf8。支持 utf8/utf8bom/ascii/utf16/utf16be/utf32")))
        .Set("required", JNode.Array().Add("file_path").Add("content"));

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var filePath = arguments.GetValueOrDefault("file_path")?.ToString() ?? "";
        var content = arguments.GetValueOrDefault("content")?.ToString() ?? "";
        var agentId = arguments.GetValueOrDefault("_agent_id")?.ToString() ?? "main";
        var append = arguments.TryGetValue("append", out var ap) &&
                     ap?.ToString()?.ToLowerInvariant() == "true";
        var encodingName = arguments.GetValueOrDefault("encoding")?.ToString();

        Encoding encoding;
        try { encoding = GetEncoding(encodingName); }
        catch (ArgumentException ex) { return $"错误：{ex.Message}"; }

        return await ExecuteAsync(filePath, content, agentId, append, encoding);
    }

    private static async Task<string> ExecuteAsync(string filePath, string content, string agentId, bool append, Encoding encoding)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return "错误：file_path 不能为空 — 请提供有效的文件路径。";

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

            if (append)
            {
                // 追加模式：不覆写、不做先读后改检查、不做 diff 预览
                File.AppendAllText(path, content, encoding);
            }
            else
            {
                // 覆写已有文件前检查：确保先读后改
                if (File.Exists(path))
                {
                    var preEditWarning = FileTracker.ValidatePreEdit(path);
                    if (preEditWarning != null)
                        return preEditWarning;
                }

                // Diff 预览：仅当开关开启、非交互模式（管道/重定向/测试）、且文件已存在时
                var cfg = Config.Instance;
                if (cfg.DiffPreview && !Console.IsInputRedirected && !Console.IsOutputRedirected && File.Exists(path))
                {
                    var oldContent = File.ReadAllText(path, Encoding.UTF8);
                    var (decision, accepted) = DiffPreview.Show(oldContent, content, filePath);
                    if (decision == DiffPreview.Decision.RejectAll)
                        return $"已取消写入 {filePath}（用户拒绝变更）";
                    if (decision == DiffPreview.Decision.Partial && accepted != null)
                        content = DiffPreview.ApplyAccepted(oldContent, DiffPreview.BuildHunks(oldContent, content), accepted);
                }

                File.WriteAllText(path, content, encoding);
            }

            EditFileTool.ChangedFiles.Add(path);
            FileTracker.RecordWrite(path);

            var lineCount = content.Count(c => c == '\n') + (string.IsNullOrEmpty(content) || content.EndsWith('\n') ? 0 : 1);
            var writeResult = $"已{(append ? "追加" : "写入")} {lineCount} 行到 {filePath}";

            // LSP 诊断自动附加：运行 lint 检查新写入文件的错误
            var writeDiag = await DiagnosticManager.TryRunLintWithTimeout(path, 3000);
            if (writeDiag != null)
                writeResult += "\n\n" + writeDiag;

            return writeResult;
        }
        catch (Exception ex)
        {
            return $"错误：{ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            FileLockManager.Release(path, agentId);
        }
    }

    /// <summary>解析编码名 → Encoding。仅支持 AOT 内置编码，未知名称抛 ArgumentException。</summary>
    private static Encoding GetEncoding(string? name)
    {
        return (name ?? "utf8").Trim().ToLowerInvariant() switch
        {
            "utf8" or "utf-8" => new UTF8Encoding(false),
            "utf8bom" or "utf-8-bom" or "utf8-bom" => new UTF8Encoding(true),
            "ascii" or "us-ascii" => Encoding.ASCII,
            "utf16" or "utf16le" or "utf-16" or "utf-16le" or "unicode" => Encoding.Unicode,
            "utf16be" or "utf-16be" or "bigendianunicode" => Encoding.BigEndianUnicode,
            "utf32" or "utf-32" => Encoding.UTF32,
            _ => throw new ArgumentException($"不支持的编码 '{name}'（支持 utf8/utf8bom/ascii/utf16/utf16be/utf32）"),
        };
    }
}
