using WayCoder.Tools;

namespace WayCoder;

/// <summary>
/// 系统提示词 - 将 LLM 转变为编程智能体的指令。
/// </summary>
public static class SystemPrompt
{
    public static string Generate(List<ITool> tools)
    {
        var cwd = Directory.GetCurrentDirectory();
        var toolList = string.Join("\n", tools.Select(t => $"- **{t.Name}**：{t.Description}"));
        var os = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";
        var dotnetVersion = Environment.Version.ToString();

        // 加载项目专属指令
        var instructions = ProjectContext.LoadInstructions();

        // 检测项目上下文
        var project = ProjectContext.DetectProject();
        var projectCtx = project.ToMarkdown();

        // 仓库地图
        var repoMap = RepoMapGenerator.Generate();

        // 技能列表（仅名称 + 描述，不加载完整 body）
        var skillsSection = SkillsManager.GetSkillsSection();
        if (!string.IsNullOrEmpty(skillsSection))
            skillsSection = "\n" + skillsSection;

        // 项目记忆（TF-IDF 语义匹配，只注入与当前项目最相关的记忆）
        var memorySection = "";
        try
        {
            var config = Config.FromEnv();
            // 用项目上下文作为查询关键词，提取最相关记忆
            var query = $"{project.PrimaryLanguage} {string.Join(" ", project.BuildTools)} {string.Join(" ", project.Frameworks)}";
            StructuredMemory.MigrateFromOldFormat();
            var relevantMemory = StructuredMemory.GetRelevantContext(query,
                topN: config.MemoryRelevanceTopN, maxChars: 2000);
            if (!string.IsNullOrWhiteSpace(relevantMemory))
            {
                memorySection = $"""

                # 项目记忆（自动匹配 {config.MemoryRelevanceTopN} 条）
                {relevantMemory}
                """;
            }
        }
        catch
        {
            // 回退：加载最新记忆（最多 1500 字符）
            try
            {
                var all = StructuredMemory.ListAll();
                if (all.Count > 0)
                {
                    var memory = string.Join("\n", all.Take(5)
                        .Select(e => $"- {e.Description}: {e.Content}"));
                    if (memory.Length > 1500)
                        memory = memory[..1500] + "\n...（记忆已截断）";
                    memorySection = $"""

                        # 项目记忆
                        {memory}
                        """;
                }
            }
            catch { }
        }

        return $"""
                你是 WayCoder（道码），一个运行在用户终端中的 AI 编程助手。
                你帮助完成软件工程任务：编写代码、修复 bug、重构代码、解释代码、运行命令等。

                # 环境
                - 工作目录：{cwd}
                - 操作系统：{os}
                - .NET：{dotnetVersion}

                # 项目上下文
                {projectCtx}

                {instructions}

                {memorySection}
                {skillsSection}
                {repoMap}

                # 工具
                {toolList}

                # 规则
                1. **先读后改。** 修改文件之前始终先读取它。
                2. **小改动用 edit_file。** 针对性编辑使用 edit_file；仅在新建文件或完全重写时使用 write_file。
                3. **验证你的工作。** 做出修改后，运行相关测试或命令以确认正确性。
                4. **保持简洁。** 展示代码优于展示文字。只解释必要的内容。
                5. **一步一步来。** 对于多步骤任务，依次执行。
                6. **edit_file 唯一性。** 使用 edit_file 时，在 old_string 中包含足够的上下文以确保唯一匹配。
                7. **遵循现有风格。** 匹配项目的编码约定。
                8. **不确定时询问。** 如果需求不明确，询问澄清而非猜测。
                9. **善用 todo 工具。** 复杂任务先创建任务列表，逐一完成并更新状态。
                """;
    }
}
