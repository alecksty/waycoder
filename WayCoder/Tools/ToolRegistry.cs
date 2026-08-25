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
        new KbTool(),
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
        new DrawTool(),
        new ImageConvertTool(),
        new SqliteTool(),
        new TestTool(),
    ];

    /// <summary>缓存：Agent 构造/10 槽位/子智能体频繁访问 AllTools，避免每次重建列表。</summary>
    private static List<ITool>? _cachedAllTools;

    /// <summary>所有工具（内置 + MCP 自动发现 + 编译期插件贡献）。缓存，MCP 工具变更时失效。</summary>
    public static List<ITool> AllTools
    {
        get
        {
            if (_cachedAllTools == null)
            {
                var all = new List<ITool>(BuiltinTools);
                all.AddRange(McpManager.GetDiscoveredToolsSnapshot()); // 快照：MCP 发现并发改写列表中
                all.AddRange(PluginRegistry.CollectTools());
                _cachedAllTools = all;
            }
            return _cachedAllTools;
        }
    }

    /// <summary>MCP 工具变更时使缓存失效（McpManager.MutateTools 每次变更后调用）。</summary>
    public static void InvalidateAllToolsCache() => _cachedAllTools = null;

    /// <summary>
    /// 子智能体禁止使用的工具名称集合。
    /// 子智能体不能管理进程、做危险删除或用户交互；但保留 bash（shell 权限），
    /// 由 PermissionManager 统一裁决：YOLO 模式直接放行、非 YOLO 模式逐条提问确认。
    /// </summary>
    public static readonly HashSet<string> SubAgentDeniedTools = new(StringComparer.OrdinalIgnoreCase)
    {
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
