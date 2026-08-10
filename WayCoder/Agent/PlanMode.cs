namespace WayCoder;

/// <summary>
/// Plan 模式 — 只读分析阶段，Agent 先规划再执行。
/// 受控工具集（只读），专门的系统提示词，用户确认后才进入执行阶段。
/// 灵感源自 Claude Code /plan 和 Aider Architect 模式。
/// </summary>
public static class PlanMode
{
    /// <summary>Plan 模式下可用的只读工具名称</summary>
    public static readonly HashSet<string> ReadOnlyToolNames = new()
    {
        "read_file", "glob", "grep", "ls", "lsp", "todo",
        "memory", "web_search", "fetch", "stat", "pwd",
        "wc", "diff", "tree",
    };

    /// <summary>
    /// 生成 Plan 模式的系统提示词。
    /// 强调分析、规划，禁止修改任何文件。
    /// </summary>
    public static string GetPlanSystemPrompt()
    {
        var cwd = Directory.GetCurrentDirectory();
        var project = ProjectContext.DetectProject();
        var projectCtx = project.ToMarkdown();
        var instructions = ProjectContext.LoadInstructions();
        var repoMap = RepoMapGenerator.Generate();

        return $"""
            你是 WayCoder（道码）的**规划模式**。你当前处于只读分析阶段，**严禁修改任何文件**。

            # 环境
            - 工作目录：{cwd}

            # 项目上下文
            {projectCtx}

            {instructions}

            {repoMap}

            # 你的任务

            1. **分析需求**：仔细理解用户的需求，探索相关代码
            2. **制定计划**：输出一个结构化的执行计划，包含：
               - 📋 **步骤列表**：按顺序列出每一步要做什么
               - 📁 **涉及文件**：每个步骤需要修改哪些文件
               - ⚠️ **风险点**：可能出问题的地方
               - 🔧 **方法**：用什么工具/方式来完成
            3. **不要执行**：只需要分析和规划，**不要写任何代码**

            # 输出格式

            请使用以下格式输出你的计划：

            ## 分析
            （简要分析需求和现状）

            ## 执行计划
            1. **步骤名** — 说明 | 📁 涉及文件 | ⚠️ 风险
            2. ...

            ## 预估
            - 预计修改文件数：N
            - 预计复杂度：低/中/高
            - 预计时间：大致估算

            确认后我将切换到执行模式逐步完成。
            """;
    }

    /// <summary>
    /// 分析用户的回复，判断是否表示同意执行。
    /// </summary>
    public static bool IsApproval(string userInput)
    {
        var input = userInput.Trim().ToLower();
        // 明确拒绝
        if (input is "n" or "no" or "不" or "取消" or "拒绝" or "cancel" or "abort")
            return false;
        // 明确同意
        if (input is "y" or "yes" or "是" or "好" or "ok" or "可以" or "确认" or "执行" or "开始" or "go" or "proceed")
            return true;
        // 包含修改意见 → 不算同意
        if (input.Contains("修改") || input.Contains("调整") || input.Contains("改成") ||
            input.Contains("不要") || input.Contains("别"))
            return false;
        // 默认当作新需求，不算同意
        return false;
    }
}
