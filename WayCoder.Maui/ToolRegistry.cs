namespace WayCoder.Tools;

/// <summary>
/// MAUI 移动端工具注册表 —— 与主项目 <c>ToolRegistry</c> 同名同命名空间，由 MAUI 项目编译
/// （主项目 <c>Tools/ToolRegistry.cs</c> 在 Exclude 清单内）。
///
/// 移动端裁掉所有进程类工具（bash/git_pr/lsp/lint/ps/kill/screenshot/sqlite/test/
/// job_output/job_kill），保留纯文件/网络/计算/多模态工具。git 非进程工具——纯 C# 实现
/// （CoreStubs.GitTool → GitCore/GitRemote/GitBranch，含 pull/push/branch/checkout/merge），
/// 已注册使模型可自主调用。MCP 已接入（Http/Sse 传输可用，stdio 运行时降级），
/// 经 McpManager.GetDiscoveredToolsSnapshot 动态注入，故 AllTools 需缓存 + 失效逻辑。
/// </summary>
public static class ToolRegistry
{
    public static readonly List<ITool> BuiltinTools =
    [
        new GitTool(),
        new ReadFileTool(),
        new WriteFileTool(),
        new EditFileTool(),
        new GlobTool(),
        new GrepTool(),
        new AgentTool(),
        new FetchTool(),
        new TodoTool(),
        new MemoryTool(),
        new KbTool(),
        new WebSearchTool(),
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
        new DownloadTool(),
        new MultiEditTool(),
        new StructTodoTool(),
        new ExportTool(),
        new ViewImageTool(),
        new TranscribeAudioTool(),
        new DrawTool(),
        new ImageConvertTool(),
        new ConvertEncodingTool(),
        new SqliteTool(),
        new SymbolsTool(),
    ];

    private static List<ITool>? _cachedAllTools;

    /// <summary>所有工具（内置 + MCP 自动发现 + 编译期插件贡献）。缓存，MCP 工具变更时失效。</summary>
    public static List<ITool> AllTools
    {
        get
        {
            if (_cachedAllTools == null)
            {
                var all = new List<ITool>(BuiltinTools);
                all.AddRange(McpManager.GetDiscoveredToolsSnapshot());
                all.AddRange(PluginRegistry.CollectTools());
                _cachedAllTools = all;
            }
            return _cachedAllTools;
        }
    }

    /// <summary>MCP 工具变更时使缓存失效（McpManager.MutateTools 每次变更后调用）。</summary>
    public static void InvalidateAllToolsCache() => _cachedAllTools = null;

    /// <summary>
    /// 子智能体禁止使用的工具集合。与主工程保持一致；移动端无进程类工具，
    /// 其中 kill/ps/git/git_pr/job_output/job_kill 为冗余项（工具本就不存在），
    /// 有效的禁用为 rm/download/skill/ask_user_question（子智能体不做危险删除、外部下载与用户交互）。
    /// </summary>
    public static readonly HashSet<string> SubAgentDeniedTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "rm",
        "kill",
        "ps",
        "git",
        "git_pr",
        "download",
        "skill",
        "ask_user_question",
        "job_output",
        "job_kill",
    };

    /// <summary>获取子智能体的工具列表：排除危险工具 + 深度限制下排除 agent。</summary>
    public static List<ITool> GetSubAgentTools(List<ITool> parentTools, int currentDepth, int maxDepth)
    {
        var subTools = parentTools
            .Where(t => !SubAgentDeniedTools.Contains(t.Name))
            .ToList();

        if (currentDepth >= maxDepth - 1)
            subTools.RemoveAll(t => t.Name == "agent");

        return subTools;
    }

    /// <summary>按名称查找工具。未找到时返回 null。</summary>
    public static ITool? GetTool(string name)
        => AllTools.FirstOrDefault(t => t.Name == name);
}
