using System.Text.RegularExpressions;

namespace WayCoder;

/// <summary>
/// 技能管理器 —— 发现并解析标准 SKILL.md 文件。
///
/// 行业标准格式 (Claude Code, Copilot CLI, OpenCode, agentskills.io):
///   skills/<skill-name>/SKILL.md
/// 以 --- 开始和结束的 YAML frontmatter 包含 name: 和 description: 字段，
/// body 是 frontmatter 之后的所有 markdown 内容。
///
/// 增强（对标 crush）：
///   - 名称验证：正则 /^[a-zA-Z0-9]+(-[a-zA-Z0-9]+)*$/，最多 64 字符
///   - 名称必须与目录名匹配
///   - 支持 license、compatibility、metadata 字段
///   - 多发现路径：.waycoder/skills/、.corecoder/skills/、.claude/skills/、.cursor/skills/
///   - XML 格式系统提示词注入
///   - 内置 skill 支持（waycoder-config）
///   - 技能加载追踪
/// </summary>
public static class SkillsManager
{
    private static readonly Dictionary<string, SkillDef> _skills = [];

    /// <summary>名称验证正则</summary>
    private static readonly Regex NamePattern = new(@"^[a-zA-Z0-9]+(-[a-zA-Z0-9]+)*$", RegexOptions.Compiled);

    /// <summary>已加载的技能</summary>
    public static IReadOnlyDictionary<string, SkillDef> Skills => _skills;

    /// <summary>已追踪的已加载技能名称</summary>
    public static readonly HashSet<string> LoadedSkills = [];

    /// <summary>
    /// 从技能发现目录 + 内置技能加载所有技能。
    /// 从当前目录向上查找到 home 目录。同一技能名出现多次时，
    /// 项目本地目录优先于通用目录，内置技能可被用户技能覆盖。
    /// </summary>
    public static void Load()
    {
        _skills.Clear();
        LoadedSkills.Clear();

        // 1. 加载内置技能
        LoadBuiltinSkills();

        // 2. 加载磁盘上的技能
        var dirs = FindSkillDirs();
        if (dirs.Count > 0)
        {
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
                            var isLocal = dir.Contains(".waycoder") || dir.Contains(".corecoder");
                            // 本地目录优先覆盖
                            if (!_skills.ContainsKey(key) || isLocal)
                                _skills[key] = skill;
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLog.Log("skills", $"加载技能失败 {subDir}: {ex.Message}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 加载内置技能（从应用目录 Skills/builtin/）。
    /// </summary>
    private static void LoadBuiltinSkills()
    {
        try
        {
            var exeDir = AppContext.BaseDirectory;
            var builtinDir = Path.Combine(exeDir, "Skills", "builtin");
            if (!Directory.Exists(builtinDir))
            {
                // 开发模式：从项目目录查找
                var projectDir = Path.Combine(exeDir, "..", "..", "..");
                builtinDir = Path.GetFullPath(Path.Combine(projectDir, "Skills", "builtin"));
            }

            if (!Directory.Exists(builtinDir))
                return;

            foreach (var subDir in Directory.GetDirectories(builtinDir))
            {
                try
                {
                    var skill = ParseSkillDir(subDir);
                    if (skill != null)
                    {
                        skill.Builtin = true;
                        _skills[skill.Name.ToLowerInvariant()] = skill;
                    }
                }
                catch (Exception ex)
                {
                    DebugLog.Log("skills", $"加载内置技能失败 {subDir}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            DebugLog.Log("skills", $"内置技能加载异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 加载指定技能的全部内容。
    /// </summary>
    public static SkillDef? GetSkill(string name)
    {
        if (_skills.TryGetValue(name.ToLowerInvariant(), out var skill))
        {
            LoadedSkills.Add(skill.Name);
            return skill;
        }
        return null;
    }

    /// <summary>
    /// 标记技能为已加载（用于追踪哪些技能已被 LLM 引用）。
    /// </summary>
    public static void MarkLoaded(string name)
    {
        LoadedSkills.Add(name);
    }

    /// <summary>生成技能列表的 markdown 描述（旧格式，向后兼容）</summary>
    public static string GetSkillsSection()
    {
        if (_skills.Count == 0)
            return "";

        var lines = new List<string> { "# 技能 (Skills)" };
        foreach (var kv in _skills)
        {
            var desc = string.IsNullOrEmpty(kv.Value.Description) ? "(无描述)" : kv.Value.Description;
            var builtinTag = kv.Value.Builtin ? " [内置]" : "";
            lines.Add($"- **{kv.Value.Name}**{builtinTag}：{desc}");
        }
        return string.Join("\n", lines);
    }

    /// <summary>
    /// 生成技能列表的 XML 格式（用于系统提示词注入，对标 crush）。
    /// 格式：
    /// &lt;available_skills&gt;
    ///   &lt;skill&gt;
    ///     &lt;name&gt;...&lt;/name&gt;
    ///     &lt;description&gt;...&lt;/description&gt;
    ///     &lt;location&gt;...&lt;/location&gt;
    ///   &lt;/skill&gt;
    /// &lt;/available_skills&gt;
    /// </summary>
    public static string GetSkillsXml()
    {
        if (_skills.Count == 0)
            return "";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<available_skills>");

        foreach (var kv in _skills.OrderBy(k => k.Value.Builtin ? 0 : 1).ThenBy(k => k.Key))
        {
            var skill = kv.Value;
            sb.AppendLine("  <skill>");
            sb.AppendLine($"    <name>{EscapeXml(skill.Name)}</name>");
            sb.AppendLine($"    <description>{EscapeXml(skill.Description)}</description>");
            sb.AppendLine($"    <location>{EscapeXml(skill.DirPath)}</location>");
            if (skill.Builtin)
                sb.AppendLine("    <type>builtin</type>");
            if (!string.IsNullOrEmpty(skill.License))
                sb.AppendLine($"    <license>{EscapeXml(skill.License)}</license>");
            sb.AppendLine("  </skill>");
        }

        sb.Append("</available_skills>");
        return sb.ToString();
    }

    // ========================================================================
    // 内部实现
    // ========================================================================

    /// <summary>
    /// 查找所有技能目录。分两部分，**缺一不可**：
    ///
    /// ① **项目链**：从 cwd 逐级向上收集 `{ .waycoder, .corecoder, .claude, .cursor }/skills`，
    ///    到用户主目录或盘根为止（主目录那一级**含**在链内）。
    /// ② **用户级**：<see cref="Global.Home"/> 下的同名目录 —— 即使它**不在** cwd 的祖先链上也要纳入。
    ///
    /// **为什么必须显式写 ②**：原先只有 ①，指望上溯时顺带命中 home。但 `dir == home` 只在 cwd
    /// 位于 home 之下时才成立 —— **cwd 与 home 不同盘**（D 盘项目 / C 盘用户目录，很常见）时
    /// 该边界永不触发，循环直落盘根，用户自己的 `~/.claude/skills`、`~/.waycoder/skills`
    /// **静默不加载**（现象：换到别的盘建项目，个人技能就「消失」了）。
    ///
    /// **边界为什么用 <see cref="ProjectContext.UserProfileDir"/> 而不只是 `Global.Home`**：
    /// `Global.Home` 会被 `HomeOverride` 改成临时目录（自测/嵌入式），而它**根本不在 cwd 的祖先
    /// 链上** ⇒ 等不到相等 ⇒ 上溯无界。这与 `ProjectContext.FindProjectRoot` 是同一类坑，
    /// 那边的处置也是「home 与 UserProfileDir 两个边界都兜」。
    ///
    /// `internal` 供自测直接断言「home 不在 cwd 链上时是否仍被发现」。
    /// </summary>
    internal static List<string> FindSkillDirs()
    {
        var result = new List<string>();
        var home = Global.Home;

        // ① 链：cwd → 上溯（含边界那一级）
        var chain = new List<string>();
        var dir = Environment.CurrentDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            chain.Add(dir);
            if (dir == home || dir == ProjectContext.UserProfileDir || dir == Path.GetPathRoot(dir))
                break;
            dir = Path.GetDirectoryName(dir);
        }

        // ② 用户级兜底：home 不在链上（不同盘）时补到**末尾**。
        // 末尾 = 下面的 Reverse 之后排在最前 = 最先加载 = 优先级最低，与「home 恰好在祖先链顶端」
        // 时的相对位置一致 —— 不改变既有的「本地目录覆盖通用目录」语义。
        if (!string.IsNullOrEmpty(home) && !chain.Contains(home))
            chain.Add(home);

        foreach (var d in chain)
        {
            // 按优先顺序添加（.waycoder > .corecoder > .claude > .cursor）
            // .waycoder 和 .corecoder 是 WayCoder 专有目录，优先
            foreach (var name in new[] { ".waycoder", ".corecoder", ".claude", ".cursor" })
            {
                var candidate = Path.Combine(d, name, "skills");
                if (Directory.Exists(candidate) && !result.Contains(candidate))
                    result.Add(candidate);
            }
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
        string? license = null;
        string? compatibility = null;
        var metadata = new Dictionary<string, string>();
        string body = text;

        // 解析 YAML frontmatter（以 --- 开始）
        if (text.StartsWith("---"))
        {
            var endIdx = text.IndexOf("\n---", 3);
            if (endIdx > 0)
            {
                var frontmatter = text[4..endIdx].Trim();
                body = text[(endIdx + 4)..].Trim();

                name = ParseYamlField(frontmatter, "name");
                description = ParseYamlField(frontmatter, "description");
                license = ParseYamlField(frontmatter, "license");
                compatibility = ParseYamlField(frontmatter, "compatibility");

                // 解析 metadata: 块（简单的 key: value 对）
                ParseMetadataBlock(frontmatter, metadata);
            }
        }

        // 回退：使用目录名
        name ??= dirName;

        // 名称验证
        if (!IsValidSkillName(name))
        {
            DebugLog.Log("skills", $"技能名称无效: '{name}' (目录: {dirPath})");
            return null;
        }

        // 名称必须匹配目录名（除非是内置技能）
        if (!string.Equals(name, dirName, StringComparison.OrdinalIgnoreCase))
        {
            DebugLog.Log("skills", $"技能名称 '{name}' 与目录名 '{dirName}' 不匹配");
            // 不阻塞加载，仅警告
        }

        // 验证必填字段
        if (string.IsNullOrWhiteSpace(description))
        {
            DebugLog.Log("skills", $"技能 '{name}' 缺少 description");
        }

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
            License = license ?? "",
            Compatibility = compatibility ?? "",
            Metadata = metadata,
            Body = body,
            DirPath = dirPath,
            BundledFiles = bundledFiles,
        };
    }

    /// <summary>验证技能名称是否符合 agentskills.io 规范</summary>
    private static bool IsValidSkillName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        if (name.Length > 64)
            return false;
        if (!NamePattern.IsMatch(name))
            return false;
        return true;
    }

    /// <summary>从 YAML frontmatter 提取单行字段</summary>
    private static string? ParseYamlField(string frontmatter, string fieldName)
    {
        var match = Regex.Match(frontmatter, $@"^{fieldName}:\s*(.+)$", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    /// <summary>解析 metadata: 块下的简单 key: value 对</summary>
    private static void ParseMetadataBlock(string frontmatter, Dictionary<string, string> metadata)
    {
        var metadataMatch = Regex.Match(frontmatter, @"^metadata:\s*$", RegexOptions.Multiline);
        if (!metadataMatch.Success) return;

        // 找到 metadata: 后的缩进块
        var startIdx = metadataMatch.Index + metadataMatch.Length;
        var remaining = frontmatter[startIdx..];
        var lines = remaining.Split('\n');

        foreach (var line in lines)
        {
            var trimmed = line;
            // 如果遇到非缩进行，metadata 块结束
            if (!trimmed.StartsWith("  ") && !trimmed.StartsWith('\t') && !string.IsNullOrWhiteSpace(trimmed))
                break;

            var kv = Regex.Match(trimmed, @"^\s+(\w[\w-]*):\s*(.+)$");
            if (kv.Success)
            {
                metadata[kv.Groups[1].Value] = kv.Groups[2].Value.Trim();
            }
        }
    }

    /// <summary>XML 转义</summary>
    private static string EscapeXml(string s)
    {
        return s.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
    }
}

/// <summary>
/// 单个技能定义（增强版，兼容 agentskills.io 规范）。
/// </summary>
public class SkillDef
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string License { get; init; } = "";
    public string Compatibility { get; init; } = "";
    public Dictionary<string, string> Metadata { get; init; } = [];
    public string Body { get; init; } = "";
    public string DirPath { get; init; } = "";
    public List<string> BundledFiles { get; init; } = [];
    /// <summary>是否为内置技能（编译进二进制）</summary>
    public bool Builtin { get; set; }
}
