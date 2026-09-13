namespace WayCoder.UI.TUI.Renderers;

/// <summary>
/// 工具输出渲染器接口 —— 对标 Crush 的 ToolMessageItem 模式。
/// 每种工具类型有独立渲染器，格式化输出供 ChatScreen 显示。
/// </summary>
public interface IToolRenderer
{
    /// <summary>工具名（用于工厂匹配）</summary>
    string ToolName { get; }

    /// <summary>格式化工具调用头行。brief 是参数摘要。</summary>
    string FormatHeader(string brief);

    /// <summary>格式化工具执行结果。rawOutput 是工具返回的原始字符串。</summary>
    string FormatOutput(string rawOutput);
}

/// <summary>
/// 工具渲染器工厂 —— 按工具名分发到对应渲染器。
/// </summary>
public static class ToolRendererFactory
{
    private static readonly Dictionary<string, IToolRenderer> _renderers = new(StringComparer.OrdinalIgnoreCase);
    private static readonly DefaultToolRenderer _default = new();

    static ToolRendererFactory()
    {
        Register(new BashToolRenderer());
        Register(new EditToolRenderer());
        Register(new WriteToolRenderer());
        Register(new AgentToolRenderer());
        Register(new ReadFileToolRenderer());
        var searchRenderer = new GlobGrepToolRenderer();
        RegisterAlias("glob", searchRenderer);
        RegisterAlias("grep", searchRenderer);
    }

    public static void Register(IToolRenderer renderer)
    {
        _renderers[renderer.ToolName] = renderer;
    }

    /// <summary>将一个渲染器注册到多个工具名</summary>
    public static void RegisterAlias(string alias, IToolRenderer renderer)
    {
        _renderers[alias] = renderer;
    }

    /// <summary>
    /// 统一的工具行标题：`🔧 Edit(参数)`。
    ///
    /// 各渲染器此前各写一套「emoji + 小写名 + 参数」（`✏️ edit x` / `📝 write x` / `💻 bash x` …）：
    /// 图标不统一（✏️📝💻📖🔍🤖⚙）、名称大小写也不一，在聊天流里一眼扫不出「这是工具调用」。
    /// 现在图标统一、名称首字母大写并**加粗染黄**、参数降为灰色 —— 与下面的内容行拉开层次。
    /// </summary>
    public static string FormatHeader(string toolName, string brief)
    {
        var head = $"🔧 «bold»«yellow»{DisplayName(toolName)}«/»«/»";
        return string.IsNullOrWhiteSpace(brief) ? head : head + $"«grey»({brief})«/»";
    }

    /// <summary>
    /// 工具显示名：去掉 `_file` 后缀后把 snake_case 转 PascalCase。
    /// （`edit_file` → `Edit`、`read_file` → `Read`、`multi_edit` → `MultiEdit`；
    ///  `_file` 后缀去掉是因为 Read/Write/Edit 更像动作名，也比 ReadFile 短。）
    /// </summary>
    internal static string DisplayName(string toolName)
    {
        var n = toolName.EndsWith("_file", StringComparison.Ordinal) ? toolName[..^5] : toolName;
        var sb = new System.Text.StringBuilder(n.Length);
        bool up = true;
        foreach (var c in n)
        {
            if (c is '_' or '-') { up = true; continue; }
            sb.Append(up ? char.ToUpperInvariant(c) : c);
            up = false;
        }
        return sb.Length > 0 ? sb.ToString() : toolName;
    }

    public static IToolRenderer Get(string toolName)
    {
        // 去掉 mcp_ 前缀后匹配
        if (toolName.StartsWith("mcp_", StringComparison.Ordinal) && toolName.Count(c => c == '_') >= 2)
        {
            var lastUnderscore = toolName.LastIndexOf('_');
            var baseName = toolName[(lastUnderscore + 1)..];
            if (_renderers.TryGetValue(baseName, out var r)) return r;
        }
        return _renderers.GetValueOrDefault(toolName, _default);
    }
}
