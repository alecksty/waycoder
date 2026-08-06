namespace CoreCoderSharp.Tools;

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
    /// 按名称查找工具。未找到时返回 null。
    /// </summary>
    public static ITool? GetTool(string name)
    {
        return AllTools.FirstOrDefault(t => t.Name == name);
    }
}
