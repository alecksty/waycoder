namespace CoreCoderSharp.UI;

/// <summary>
/// 统一 diff 格式渲染器 —— 将 git diff 输出转为带 ANSI 颜色的屏幕行。
///
/// 特性：
/// - 删除行：红色背景(41) + "-" 前缀
/// - 添加行：绿色背景(42) + "+" 前缀
/// - Hunk 头：青色
/// - 语法高亮：对变更行按文件扩展名应用 Syntax 高亮
/// - 行号显示
/// </summary>
public static class DiffRenderer
{
    /// <summary>
    /// 解析并渲染统一 diff 格式。
    /// 返回屏幕行列表，每行是 (文本, 前景色, 背景色) 片段列表。
    /// </summary>
    public static List<List<(string Text, int Fg, int Bg)>> Render(
        string diffOutput, string? fileExtension = null)
    {
        var result = new List<List<(string, int, int)>>();
        if (string.IsNullOrWhiteSpace(diffOutput))
        {
            result.Add(OneLine("（无修改）", 2, 0));
            return result;
        }

        var syntax = fileExtension != null ? GetSyntax(fileExtension) : null;
        var lines = diffOutput.Replace("\r\n", "\n").Split('\n');

        // 状态追踪：当前文件、行号
        string? currentFile = null;
        int oldLine = 0, newLine = 0;

        foreach (var rawLine in lines)
        {
            if (string.IsNullOrEmpty(rawLine))
            {
                result.Add(OneLine("", 0, 0));
                continue;
            }

            // 文件头 diff --git a/x b/y 或 --- a/x 或 +++ b/y
            if (rawLine.StartsWith("diff --git "))
            {
                // 提取文件名用于语法高亮
                var parts = rawLine.Split(' ');
                if (parts.Length >= 4)
                {
                    var filePath = parts[3]; // b/path
                    if (filePath.StartsWith("b/"))
                    {
                        currentFile = filePath[2..];
                        syntax = GetSyntax(currentFile);
                    }
                }
                result.Add(OneLine(rawLine, 1, 0)); // Bold
                continue;
            }

            if (rawLine.StartsWith("--- ") || rawLine.StartsWith("+++ "))
            {
                result.Add(OneLine(rawLine, 1, 0));
                continue;
            }

            // Hunk 头 @@ -old,count +new,count @@
            if (rawLine.StartsWith("@@"))
            {
                // 解析行号
                ParseHunkHeader(rawLine, out oldLine, out newLine);
                result.Add(OneLine(rawLine, 36, 0)); // Cyan
                continue;
            }

            // 文件模式 / index 行
            if (rawLine.StartsWith("index ") || rawLine.StartsWith("new ") ||
                rawLine.StartsWith("old ") || rawLine.StartsWith("rename ") ||
                rawLine.StartsWith("similarity ") || rawLine.StartsWith("deleted ") ||
                rawLine.StartsWith("Binary "))
            {
                result.Add(OneLine(rawLine, 2, 0));
                continue;
            }

            // 删除行
            if (rawLine.StartsWith('-') && !rawLine.StartsWith("---"))
            {
                var content = rawLine[1..];
                var line = new List<(string, int, int)>();
                // 行号
                line.Add(($"-{oldLine,4} ", 0, 41));
                oldLine++;

                if (syntax != null && !string.IsNullOrEmpty(content))
                {
                    // 语法高亮 + 红色背景
                    var tokens = syntax.Tokenize(content);
                    foreach (var (text, color) in tokens)
                    {
                        line.Add((text, color, 41));
                    }
                }
                else
                {
                    line.Add((content, 0, 41));
                }
                result.Add(line);
                continue;
            }

            // 添加行
            if (rawLine.StartsWith('+') && !rawLine.StartsWith("+++"))
            {
                var content = rawLine[1..];
                var line = new List<(string, int, int)>();
                line.Add(($"+{newLine,4} ", 0, 42));
                newLine++;

                if (syntax != null && !string.IsNullOrEmpty(content))
                {
                    var tokens = syntax.Tokenize(content);
                    foreach (var (text, color) in tokens)
                    {
                        line.Add((text, color, 42));
                    }
                }
                else
                {
                    line.Add((content, 0, 42));
                }
                result.Add(line);
                continue;
            }

            // 上下文行
            {
                var line = new List<(string, int, int)>();
                line.Add(($" {oldLine,4} ", 2, 0));
                oldLine++; newLine++;

                if (syntax != null && rawLine.Length > 1)
                {
                    var tokens = syntax.Tokenize(rawLine);
                    foreach (var (text, color) in tokens)
                    {
                        line.Add((text, color, 0));
                    }
                }
                else
                {
                    line.Add((rawLine, 2, 0));
                }
                result.Add(line);
            }
        }

        return result;
    }

    /// <summary>
    /// 对单个文件执行 git diff 并渲染。
    /// </summary>
    public static List<List<(string Text, int Fg, int Bg)>> RenderFileDiff(
        string filePath)
    {
        try
        {
            var diff = RunGitDiff(filePath);
            var ext = Path.GetExtension(filePath);
            return Render(diff, ext);
        }
        catch
        {
            return new List<List<(string, int, int)>> { OneLine($"（无法获取 {filePath} 的 diff）", 2, 0) };
        }
    }

    // ================================================================
    // 工具方法
    // ================================================================

    private static List<(string, int, int)> OneLine(string text, int fg, int bg)
        => new() { (text, fg, bg) };

    private static void ParseHunkHeader(string line, out int oldStart, out int newStart)
    {
        oldStart = 0; newStart = 0;
        // @@ -old,count +new,count @@
        var atOld = line.IndexOf(" -");
        var atNew = line.IndexOf(" +");
        if (atOld >= 0)
        {
            var oldPart = line[(atOld + 2)..].Split(',')[0];
            int.TryParse(oldPart, out oldStart);
        }
        if (atNew >= 0)
        {
            var newPart = line[(atNew + 2)..].Split(',')[0];
            int.TryParse(newPart, out newStart);
        }
    }

    private static string RunGitDiff(string filePath)
    {
        using var proc = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"diff -- {EscapeArg(filePath)}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        proc.Start();
        var output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit(5000);
        return output;
    }

    private static string EscapeArg(string arg) => arg.Contains(' ') ? $"\"{arg}\"" : arg;

    private static Syntax GetSyntax(string filePath)
    {
        try { return Syntax.ForFile(filePath); }
        catch { return Syntax.ByLanguage(""); }
    }
}
