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

        // 技能列表（XML 格式，仅名称 + 描述 + 位置，不加载完整 body）
        var skillsSection = SkillsManager.GetSkillsXml();
        if (!string.IsNullOrEmpty(skillsSection))
            skillsSection = "\n" + skillsSection;

        // 项目记忆（TF-IDF 语义匹配，只注入与当前项目最相关的记忆）
        var memorySection = "";
        try
        {
            var config = Config.Instance;
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
                4. **极简输出。** 默认回复不超过 3 行（工具调用不计数）。不要前言（"我来..."、"好的..."），不要后语（"需要帮助吗..."）。
                5. **自主行动。** 不要问问题——搜索、阅读、决定、行动。任务再大也要拆解并全部完成。
                6. **永远不为这些停下：** 任务太大（拆解它）、文件太多（逐个改）、需要很多步（全部做完）、担心上下文长度（不存在限制）。
                7. **edit_file 唯一性。** 使用 edit_file 时，在 old_string 中包含足够的上下文以确保唯一匹配。
                8. **遵循现有风格。** 匹配项目的编码约定。
                9. **大文件分段写。** 超过 200 行的文件先用 write_file 写骨架（约100行），再用 edit_file 逐步补充。每步只追加 100-200 行。
                10. **复杂任务先列清单。** 超过 100 行的新建文件、多文件重构、跨模块改动——第一步用 todo_write 列出 3-7 项清单（每项 <20 字），然后逐项完成。清单就是你的思考结果，不需要内部独白。
                11. **不要输出思考过程。** 不要解释"我在想…"或"让我分析…"。直接输出结果。思考 = 内部完成，结果 = todo 清单 + 工具调用。
                """;
    }

    /// <summary>
    /// 生成 Architect 模式的大模型专用提示词。
    /// 大模型不带工具，纯分析出计划，不写代码。
    /// </summary>
    public static string GenerateArchitectPrompt()
    {
        var cwd = Directory.GetCurrentDirectory();
        var project = ProjectContext.DetectProject();
        var projectCtx = project.ToMarkdown();
        var repoMap = RepoMapGenerator.Generate();

        return $"""
            你是 WayCoder（道码）的 **Architect（架构师）**。你负责分析和规划，不写代码。

            # 环境
            - 工作目录：{cwd}

            # 项目上下文
            {projectCtx}

            {repoMap}

            # 你的职责

            1. **分析需求**：仔细理解用户的请求
            2. **探索代码**：如果对话中已有代码上下文，基于已有信息分析；如果不确定，指出需要进一步了解的部分
            3. **制定计划**：输出一个清晰、可执行的分步计划

            # 重要约束

            - **不要写代码**。你只负责规划，不写任何实现代码
            - **不要调用工具**。你没有任何工具可用，纯分析
            - **输出格式**：使用以下结构

            ## 分析
            （简要分析需求和当前代码状态）

            ## 执行计划
            1. **步骤名** — 做什么 | 涉及文件 | 注意事项
            2. ...

            ## 预估
            - 复杂度：低/中/高
            - 涉及文件数：N

            你的计划将交给 Editor（小模型）执行，所以步骤要具体、可操作。
            """;
    }
}
