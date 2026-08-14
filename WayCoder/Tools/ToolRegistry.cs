namespace WayCoder.Tools;

/// <summary>
/// 工具注册表。按名称查找工具。
/// </summary>
public static class ToolRegistry
{
    public static readonly List<ITool> BuiltinTools =
    [
        new BashTool(),
        new ReadFileTool(),
        new WriteFileTool(),
        new EditFileTool(),
        new GlobTool(),
        new GrepTool(),
        new AgentTool(),
        new GitTool(),
        new FetchTool(),
        new TodoTool(),
        new LspTool(),
        new MemoryTool(),
        new LintTool(),
        new WebSearchTool(),
        new GitPRTool(),
        new PsTool(),
        new KillTool(),
        new LsTool(),
        new MkdirTool(),
        new RmTool(),
        new CdTool(),
        new FindReplaceTool(),
        new CpTool(),
        new MvTool(),
        new DiffTool(),
        new TreeTool(),
        new WcTool(),
        new StatTool(),
        new PwdTool(),
        new SkillTool(),
        new DocTool(),
        new NotebookEditTool(),
        new AskUserQuestionTool(),
        new JobOutputTool(),
        new JobKillTool(),
        new DownloadTool(),
        new MultiEditTool(),
        new StructTodoTool(),
        new ExportTool(),
        new ScreenshotTool(),
        new ViewImageTool(),
        new TranscribeAudioTool(),
    ];

    /// <summary>所有工具（内置 + MCP 自动发现）</summary>
    public static List<ITool> AllTools
    {
        get
        {
            var all = new List<ITool>(BuiltinTools);
            all.AddRange(McpManager.DiscoveredTools);
            return all;
        }
    }

    /// <summary>
    /// 子智能体禁止使用的工具名称集合。
    /// 子智能体只能做读写文件、搜索代码等安全操作，不能执行 shell 命令或管理进程。
    /// </summary>
    public static readonly HashSet<string> SubAgentDeniedTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "bash",         // 危险：子智能体不应执行任意 shell 命令
        "rm",           // 危险：删除文件
        "kill",         // 进程管理
        "ps",           // 进程查看
        "git",          // git 操作（主智能体统一管理）
        "git_pr",       // GitHub PR 操作
        "download",     // 外部下载
        "skill",        // 技能调用（主智能体管理）
        "ask_user_question", // 用户交互（只有主智能体可以）
        "job_output",   // 后台任务管理
        "job_kill",     // 后台任务管理
    };

    /// <summary>
    /// 获取子智能体的工具列表：排除危险工具 + 深度限制下排除 agent。
    /// </summary>
    public static List<ITool> GetSubAgentTools(List<ITool> parentTools, int currentDepth, int maxDepth)
    {
        var subTools = parentTools
            .Where(t => !SubAgentDeniedTools.Contains(t.Name))
            .ToList();

        // 达到最大深度时，移除 agent 工具防止无限递归
        if (currentDepth >= maxDepth - 1)
            subTools.RemoveAll(t => t.Name == "agent");

        return subTools;
    }

    /// <summary>
    /// 按名称查找工具。未找到时返回 null。
    /// </summary>
    public static ITool? GetTool(string name)
    {
        return AllTools.FirstOrDefault(t => t.Name == name);
    }
}
