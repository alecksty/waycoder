namespace CoreCoderSharp.Tools;

/// <summary>
/// 工具注册表。按名称查找工具。
/// </summary>
public static class ToolRegistry
{
    public static readonly List<ITool> AllTools =
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
    ];

    /// <summary>
    /// 按名称查找工具。未找到时返回 null。
    /// </summary>
    public static ITool? GetTool(string name)
    {
        return AllTools.FirstOrDefault(t => t.Name == name);
    }
}
