using System.Text.RegularExpressions;

namespace WayCoder;

/// <summary>
/// 自定义斜杠命令系统 —— 从 .waycoder/commands/*.md（兼容 .corecoder/commands/）加载用户定义的命令。
/// 每个 .md 文件 = 一个斜杠命令，支持 YAML frontmatter 元数据。
/// 灵感来自 Claude Code 的 commands 系统。
/// </summary>
public static class CustomCommands
{
    private static readonly Dictionary<string, CustomCommand> _commands = [];

    /// <summary>已加载的自定义命令</summary>
    public static IReadOnlyDictionary<string, CustomCommand> Commands => _commands;

    /// <summary>
    /// 从 .waycoder/commands/（兼容 .corecoder/commands/）目录加载所有自定义命令。
    /// 如果目录不存在则静默跳过。
    /// </summary>
    public static void Load()
    {
        _commands.Clear();

        var commandsDir = FindCommandsDir();
        if (commandsDir == null || !Directory.Exists(commandsDir))
            return;

        foreach (var file in Directory.GetFiles(commandsDir, "*.md"))
        {
            try
            {
                var cmd = ParseCommandFile(file);
                if (cmd != null)
                    _commands[cmd.Name] = cmd;
            }
            catch (Exception ex)
            {
                DebugLog.Log("commands", $"加载命令失败 {file}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 执行自定义命令，将输出作为上下文注入 Agent。
    /// 返回 (reply内容, 是否替换用户输入)。
    /// </summary>
    public static (string Content, bool ReplaceInput) Execute(string commandName, string arguments, Agent agent)
    {
        if (!_commands.TryGetValue(commandName, out var cmd))
            return ($"未知命令: /{commandName}", false);

        // 执行命令内容中的内联 bash（以 ! 开头的行）
        var content = cmd.Content;
        content = Regex.Replace(content, @"^!\s*(.+)$", match =>
        {
            try
            {
                var bashResult = new Tools.BashTool().ExecuteAsync(
                    new Dictionary<string, object?> { ["command"] = match.Groups[1].Value }
                ).Result;
                return bashResult;
            }
            catch (Exception ex)
            {
                return $"(bash 错误: {ex.Message})";
            }
        }, RegexOptions.Multiline);

        // 替换占位符
        content = content.Replace("$ARGUMENTS", arguments);
        content = content.Replace("$1", arguments);

        return (content, false);
    }

    // ========================================================================
    // 内部实现
    // ========================================================================

    /// <summary>查找命令目录（向上遍历，新目录优先）</summary>
    private static string? FindCommandsDir()
    {
        var cwd = Environment.CurrentDirectory;
        var dir = cwd;
        while (dir != null)
        {
            foreach (var dirName in Global.ConfigDirSearchOrder)
            {
                var candidate = Path.Combine(dir, dirName, "commands");
                if (Directory.Exists(candidate))
                    return candidate;
            }
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return null;
    }

    /// <summary>解析单个命令文件</summary>
    private static CustomCommand? ParseCommandFile(string filePath)
    {
        var text = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        var name = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();

        // 净化命令名：只允许字母数字和连字符
        name = Regex.Replace(name, @"[^a-z0-9\-]", "");
        if (string.IsNullOrEmpty(name))
            return null;

        string? description = null;
        string content = text;

        // 解析 YAML frontmatter（以 --- 开始和结束）
        if (text.StartsWith("---"))
        {
            var endIdx = text.IndexOf("\n---", 3);
            if (endIdx > 0)
            {
                var frontmatter = text[4..endIdx].Trim();
                content = text[(endIdx + 4)..].Trim();

                // 手动解析 description: <value>
                var descMatch = Regex.Match(frontmatter, @"^description:\s*(.+)$", RegexOptions.Multiline);
                if (descMatch.Success)
                    description = descMatch.Groups[1].Value.Trim();
            }
        }

        return new CustomCommand
        {
            Name = name,
            Description = description ?? name,
            Content = content,
            SourceFile = filePath,
        };
    }
}

/// <summary>
/// 单个自定义命令定义。
/// </summary>
public class CustomCommand
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string Content { get; init; } = "";
    public string SourceFile { get; init; } = "";
}
