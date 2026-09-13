using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;

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
    /// 现在图标统一、名称首字母大写并**加粗染橙**、参数降为灰色 —— 与下面的内容行拉开层次。
    /// </summary>
    public static string FormatHeader(string toolName, string brief, int maxWidth = 0)
    {
        var head = $"💡 «bold»«orange»{DisplayName(toolName)}«/»«/»";
        if (string.IsNullOrWhiteSpace(brief)) return head;

        int nameW = DisplayName(toolName).Length; // 名称是 ASCII（PascalCase 工具名），字数即列数
        int headW = 2 + 1 + nameW;                // 💡(宽 2) + 空格 + 名称
        int need = headW + 2 + AnsiHelper.DisplayWidth(brief) + 1; // 前后括号各 1 列
        if (maxWidth <= 0 || need <= maxWidth)
            return head + $"«grey»({brief})«/»";

        // 超宽 → 参数**折行而不是截断**（bash 命令、文件路径截掉就看不全了）。
        // 两个要点：① 折行在**标记之外** —— «grey» 必须整段保留，按显示宽硬切会切出字面量；
        //          ② 每行**各自闭合** —— plainText 路径是逐行解析 «» 的，跨行标记对不上。
        int indentW = headW + 1;                        // 与「(」之后的那一列对齐
        int lineW = Math.Max(8, maxWidth - indentW);
        var sb = new StringBuilder(head).Append("«grey»(");
        var rest = brief;
        while (rest.Length > 0 && lineW >= 2)
        {
            int take = TakeByWidth(rest, lineW);
            if (take <= 0) break;                       // 首字符就超宽 → 防死循环
            // 优先在空格/路径分隔符处断行（不在单词中间切）：`…--option value` / `dir/file.cs`
            // 这种在分隔符后断开读起来自然得多。回退距离超过行宽 1/3 就不回退 ——
            // 否则一个长单词后面跟着空格会把上一行折得只剩几个字符。
            if (take < rest.Length)
            {
                int brk = rest.LastIndexOfAny([' ', '/', '\\', ','], take - 1, take);
                if (brk > 0 && take - brk <= lineW / 3) take = brk + 1;
            }
            sb.Append(rest[..take]);
            rest = rest[take..];
            if (rest.Length == 0) break;
            sb.Append("«/»\n").Append(new string(' ', indentW)).Append("«grey»");
            lineW = Math.Max(8, maxWidth - indentW);
        }
        sb.Append(")«/»");
        return sb.ToString();
    }

    /// <summary>
    /// 按显示宽度取「不切断宽字符」的最大前缀长度（返回 UTF-16 单元数）。
    /// 不用 <see cref="AnsiHelper.TruncateByWidth"/> —— 那个会补省略号，折行时每段都带「…」就错了。
    /// 按 Rune 遍历，CJK/emoji（宽 2）不会被从中间切开。
    /// </summary>
    internal static int TakeByWidth(string s, int maxWidth)
    {
        int w = 0, i = 0;
        foreach (var r in s.EnumerateRunes())
        {
            int rw = AnsiString.CharWidth(r);
            if (w + rw > maxWidth) break;
            w += rw;
            i += r.Utf16SequenceLength;
        }
        return i;
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
