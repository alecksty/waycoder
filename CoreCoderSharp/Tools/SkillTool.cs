namespace CoreCoderSharp.Tools;

/// <summary>
/// 技能工具 —— 按需加载技能的全部内容到上下文。
/// 与 SystemPrompt 中的精简列表不同，此工具返回完整的 SKILL.md body
/// 以及技能目录中打包文件列表，让 LLM 在需要时获取详细指令。
/// </summary>
public class SkillTool : ITool
{
    public string Name => "skill";
    public string Description => "加载一个指定技能的全部内容到上下文。用于需要获取某个技能的详细操作指令时调用。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["name"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "要加载的技能名称",
            },
        },
        ["required"] = new JsonArray("name"),
    };

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var name = arguments.GetValueOrDefault("name")?.ToString() ?? "";

        if (string.IsNullOrWhiteSpace(name))
            return Task.FromResult("错误：请指定技能名称（name 参数）");

        var skill = SkillsManager.GetSkill(name);
        if (skill == null)
            return Task.FromResult($"未找到技能: {name}\n可用技能: {string.Join(", ", SkillsManager.Skills.Keys)}");

        // 构建返回内容：技能 body + 打包文件列表
        var result = $"# 技能: {skill.Name}";
        if (!string.IsNullOrEmpty(skill.Description))
            result += $"\n\n{skill.Description}";
        result += $"\n\n{skill.Body}";

        if (skill.BundledFiles.Count > 0)
        {
            result += "\n\n---\n## 打包文件\n";
            foreach (var file in skill.BundledFiles)
            {
                result += $"- {file}（路径: {skill.DirPath}/{file}）\n";
            }
        }

        return Task.FromResult(result);
    }
}
