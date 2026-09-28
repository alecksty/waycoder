namespace WayCoder.Tools;

/// <summary>
/// 技能工具 —— 按需加载技能的全部内容到上下文。
/// 与 SystemPrompt 中的精简列表不同，此工具返回完整的 SKILL.md body
/// 以及技能目录中打包文件列表，让 LLM 在需要时获取详细指令。
/// </summary>
public class SkillTool : ITool
{
    public string Name => "skill";
    public string Description => L.Pick("加载一个指定技能的全部内容到上下文。用于需要获取某个技能的详细操作指令时调用。", "Load an entire skill's contents into context. Use when you need the detailed operating instructions of a specific skill.");

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("name", JNode.Param("string", L.Pick("要加载的技能名称", "Name of the skill to load."))))
        .Set("required", JNode.Array("name"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var name = arguments.GetValueOrDefault("name")?.ToString() ?? "";

        if (string.IsNullOrWhiteSpace(name))
            return Task.FromResult(L.Pick("错误：请指定技能名称（name 参数）",
                                          "Error: please specify a skill name (the 'name' parameter)"));

        var skill = SkillsManager.GetSkill(name);
        if (skill == null)
        {
            var available = string.Join(", ", SkillsManager.Skills.Keys);
            return Task.FromResult(L.Pick($"未找到技能: {name}\n可用技能: {available}",
                                          $"Skill not found: {name}\nAvailable skills: {available}"));
        }

        // 构建返回内容：技能 body + 打包文件列表
        var result = L.Pick($"# 技能: {skill.Name}", $"# Skill: {skill.Name}");
        if (!string.IsNullOrEmpty(skill.Description))
            result += $"\n\n{skill.Description}";
        result += $"\n\n{skill.Body}";

        if (skill.BundledFiles.Count > 0)
        {
            result += L.Pick("\n\n---\n## 打包文件\n", "\n\n---\n## Bundled files\n");
            foreach (var file in skill.BundledFiles)
            {
                result += L.Pick($"- {file}（路径: {skill.DirPath}/{file}）\n",
                                 $"- {file} (path: {skill.DirPath}/{file})\n");
            }
        }

        return Task.FromResult(result);
    }
}
