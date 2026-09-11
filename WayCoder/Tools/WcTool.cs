using System.Text;

namespace WayCoder.Tools;

/// <summary>
/// 文本统计工具 —— 纯 C# 实现。
/// 统计文件的行数、词数、字符数、字节数。
/// </summary>
public class WcTool : ITool
{
    public string Name => "wc";
    public string Description => "统计文本文件的行数、词数、字符数。支持多文件汇总。纯 C# 实现。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("file", JNode.Param("string", "要统计的文件路径"))
            .Set("glob", JNode.Param("string", "Glob 模式批量统计，如 '*.cs'（与 file 二选一）"))
            .Set("path", JNode.Param("string", "搜索目录（使用 glob 时，默认当前目录）")))
        .Set("required", JNode.Array());

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var file = arguments.GetValueOrDefault("file")?.ToString();
        var glob = arguments.GetValueOrDefault("glob")?.ToString();
        var path = arguments.GetValueOrDefault("path")?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(path))
            path = CwdContext.Root;

        return Task.FromResult(Execute(file, glob, path));
    }

    private static string Execute(string? file, string? glob, string path)
    {
        try
        {
            // 单文件模式
            if (!string.IsNullOrEmpty(file))
            {
                if (PathGuard.RequireFile(file) is { } e) return e;
                var stats = CountFile(file);
                return $"{stats.Lines,8} 行  {stats.Words,8} 词  {stats.Chars,8} 字符  {stats.Bytes,10} 字节  {file}";
            }

            // Glob 批量模式
            if (!string.IsNullOrEmpty(glob))
            {
                path = CwdContext.Resolve(path); // cd 后相对路径基于被跟踪工作目录
                if (PathGuard.RequireDir(path) is { } e2) return e2;

                var files = new List<string>();
                CollectFiles(path, glob, files, 200);

                if (files.Count == 0)
                    return $"未找到匹配 '{glob}' 的文件";

                var sb = new StringBuilder();
                sb.AppendLine($"## wc: {glob}  ({files.Count} 个文件)");
                sb.AppendLine($"{"行数",8}  {"词数",8}  {"字符数",8}  {"字节数",10}  文件");
                sb.AppendLine(new string('-', 60));

                long totalLines = 0, totalWords = 0, totalChars = 0, totalBytes = 0;

                foreach (var f in files)
                {
                    var s = CountFile(f);
                    totalLines += s.Lines;
                    totalWords += s.Words;
                    totalChars += s.Chars;
                    totalBytes += s.Bytes;
                    var relPath = Path.GetRelativePath(path, f);
                    sb.AppendLine($"{s.Lines,8}  {s.Words,8}  {s.Chars,8}  {s.Bytes,10}  {relPath}");
                }

                sb.AppendLine(new string('-', 60));
                sb.AppendLine($"{totalLines,8}  {totalWords,8}  {totalChars,8}  {totalBytes,10}  总计");

                return sb.ToString().TrimEnd();
            }

            return "错误：请指定 file 或 glob 参数";
        }
        catch (Exception ex)
        {
            return ToolErrors.ErrorOpPrefix("wc", ex);
        }
    }

    private static (int Lines, int Words, int Chars, long Bytes) CountFile(string filePath)
    {
        try
        {
            var text = File.ReadAllText(filePath, Encoding.UTF8);
            var bytes = new FileInfo(filePath).Length;
            var lines = text.Count(c => c == '\n');
            if (text.Length > 0 && text[^1] != '\n') lines++;
            var words = 0;
            var inWord = false;
            foreach (var c in text)
            {
                if (char.IsWhiteSpace(c) || c == '\r' || c == '\n')
                    inWord = false;
                else if (!inWord) { inWord = true; words++; }
            }
            return (lines, words, text.EnumerateRunes().Count(), bytes);
        }
        catch
        {
            return (0, 0, 0, 0);
        }
    }

    private static void CollectFiles(string dir, string glob, List<string> files, int max, int depth = 0)
        // 遍历骨架 + 深度上限 + 跳过判断统一走 FileWalker（此前自写一份跳过表，
        // 既漏了权威表里的 target/vendor/.next 等，也与 grep 的集合不一致）
        => FileWalker.Walk(dir, files, max,
            (d, res) =>
            {
                foreach (var f in Directory.GetFiles(d, glob))
                {
                    if (res.Count >= max) return;
                    if (!Path.GetFileName(f).StartsWith('.')) res.Add(f);
                }
            });
}
