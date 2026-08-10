using System.Text.RegularExpressions;

namespace WayCoder;

/// <summary>
/// 技能管理器 —— 发现并解析标准 SKILL.md 文件。
/// 行业标准格式 (Claude Code, Copilot CLI, OpenCode):
///   skills/<skill-name>/SKILL.md
/// 以 --- 开始和结束的 YAML frontmatter 包含 name: 和 description: 字段，
/// body 是 frontmatter 之后的所有 markdown 内容。
/// </summary>
public static class SkillsManager
{
    private static readonly Dictionary<string, SkillDef> _skills = [];

    /// <summary>已加载的技能</summary>
    public static IReadOnlyDictionary<string, SkillDef> Skills => _skills;

    /// <summary>
    /// 从 .waycoder/skills/ (.corecoder/skills/ 兼容) 和 .claude/skills/ 加载所有技能。
    /// 从当前目录向上查找到 home 目录。如果同一技能名出现多次，
    /// 项目本地目录优先于 .claude。
    /// </summary>
    public static void Load()
    {
        _skills.Clear();

        var dirs = FindSkillDirs();
        if (dirs.Count == 0)
            return;

        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir))
                continue;

            foreach (var subDir in Directory.GetDirectories(dir))
            {
                try
                {
                    var skill = ParseSkillDir(subDir);
                    if (skill != null)
                    {
                        var key = skill.Name.ToLowerInvariant();
                        // 项目本地目录优先于 .claude (后加载覆盖先加载)
                        if (!_skills.ContainsKey(key))
                            _skills[key] = skill;
                        else
                        {
                            // 如果是本地优先目录，覆盖之前加载的
                            var isLocal = dir.Contains(".waycoder") || dir.Contains(".corecoder"); // 兼容旧目录
                            if (isLocal)
                                _skills[key] = skill;
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugLog.Log("skills", $"加载技能失败 {subDir}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// 加载指定技能的全部内容（body + 打包文件列表）。
    /// 返回 null 表示技能不存在。
    /// </summary>
    public static SkillDef? GetSkill(string name)
    {
        if (_skills.TryGetValue(name.ToLowerInvariant(), out var skill))
            return skill;
        return null;
    }

    /// <summary>生成技能列表的 markdown 描述（仅名称 + 描述，不加载 body）</summary>
    public static string GetSkillsSection()
    {
        if (_skills.Count == 0)
            return "";

        var lines = new List<string> { "# 技能 (Skills)" };
        foreach (var kv in _skills)
        {
            var desc = string.IsNullOrEmpty(kv.Value.Description) ? "(无描述)" : kv.Value.Description;
            lines.Add($"- **{kv.Value.Name}**：{desc}");
        }
        return string.Join("\n", lines);
    }

    // ========================================================================
    // 内部实现
    // ========================================================================

    /// <summary>查找所有技能目录（从 cwd 向上到 home）</summary>
    private static List<string> FindSkillDirs()
    {
        var result = new List<string>();
        var cwd = Environment.CurrentDirectory;
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var dir = cwd;
        while (dir != null)
        {
            // 按优先顺序添加：本地目录优先（.waycoder > .corecoder > .claude）
            foreach (var name in Global.ConfigDirSearchOrder.Append(".claude"))
            {
                var candidate = Path.Combine(dir, name, "skills");
                if (Directory.Exists(candidate) && !result.Contains(candidate))
                    result.Add(candidate);
            }

            if (dir == home || dir == Path.GetPathRoot(dir) || string.IsNullOrEmpty(dir))
                break;

            dir = Path.GetDirectoryName(dir)!;
        }

        // 反转顺序：最顶层的最先加载，本地目录最后加载（可覆盖）
        result.Reverse();
        return result;
    }

    /// <summary>解析单个技能目录</summary>
    private static SkillDef? ParseSkillDir(string dirPath)
    {
        var skillMd = Path.Combine(dirPath, "SKILL.md");
        if (!File.Exists(skillMd))
            return null;

        var text = File.ReadAllText(skillMd, System.Text.Encoding.UTF8);

        // 目录名作为默认技能名
        var dirName = Path.GetFileName(dirPath);

        string? name = null;
        string? description = null;
        string body = text;

        // 解析 YAML frontmatter（以 --- 开始）
        if (text.StartsWith("---"))
        {
            var endIdx = text.IndexOf("\n---", 3);
            if (endIdx > 0)
            {
                var frontmatter = text[4..endIdx].Trim();
                body = text[(endIdx + 4)..].Trim();

                // 手动解析 name: <value>
                var nameMatch = Regex.Match(frontmatter, @"^name:\s*(.+)$", RegexOptions.Multiline);
                if (nameMatch.Success)
                    name = nameMatch.Groups[1].Value.Trim();

                // 手动解析 description: <value>
                var descMatch = Regex.Match(frontmatter, @"^description:\s*(.+)$", RegexOptions.Multiline);
                if (descMatch.Success)
                    description = descMatch.Groups[1].Value.Trim();
            }
        }

        // 回退：使用目录名
        name ??= dirName;

        // 收集打包文件列表
        var bundledFiles = new List<string>();
        try
        {
            foreach (var file in Directory.GetFiles(dirPath))
            {
                var fileName = Path.GetFileName(file);
                if (fileName.Equals("SKILL.md", StringComparison.OrdinalIgnoreCase))
                    continue;
                bundledFiles.Add(fileName);
            }
        }
        catch { }

        return new SkillDef
        {
            Name = name,
            Description = description ?? "",
            Body = body,
            DirPath = dirPath,
            BundledFiles = bundledFiles,
        };
    }
}

/// <summary>
/// 单个技能定义。
/// </summary>
public class SkillDef
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string Body { get; init; } = "";
    public string DirPath { get; init; } = "";
    public List<string> BundledFiles { get; init; } = [];
}
