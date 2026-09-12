using System.Text;

namespace WayCoder.UI.Gui;

/// <summary>聊天消息角色（对齐 Web app.js 消息体系）。</summary>
public enum ChatRole
{
    /// <summary>用户输入（右对齐气泡）</summary>
    User,
    /// <summary>助手回复（左对齐气泡）</summary>
    Assistant,
    /// <summary>工具调用（🔧 左对齐气泡）</summary>
    Tool,
    /// <summary>工具输出（等宽代码块）</summary>
    ToolOutput,
    /// <summary>系统提示/错误（居中淡色）</summary>
    System,
    /// <summary>推理内容（«dim»…«/»，独立淡色气泡，对齐 Web reasoning）</summary>
    Reasoning,
}

/// <summary>
/// 工具调用组内的一项：显示名（已缩写）/ 短参数 / 是否 raw 输出 / 累积输出。
/// 输出只留在内存，点开详情窗时才渲染（对齐 MAUI 的 ToolCallItem）。
/// </summary>
public sealed class ToolCallItem
{
    /// <summary>显示名（编辑类工具已缩写：edit_file→edit）</summary>
    public string Name { get; init; } = "";
    /// <summary>短参数（Agent 侧已缩成「最短路径/文件名」）</summary>
    public string Args { get; init; } = "";
    /// <summary>输出是否为外部进程原始字节 → 详情窗按命令行格式（等宽、保换行）</summary>
    public bool Raw { get; init; }
    /// <summary>工具输出（流式累积；超上限保留尾部窗口）</summary>
    public StringBuilder Detail { get; } = new();
}

/// <summary>
/// 结构化聊天消息：每消息独立持有原始文本与物化气泡视图。
/// 流式期间只更新最后一条 Assistant 消息的气泡，根治「全量重渲染历史」的 O(n²)。
/// </summary>
public sealed class ChatMessage
{
    public ChatRole Role { get; }
    /// <summary>保留原始 markdown / 纯文本（含 «» 标记），渲染时统一转 Inline。</summary>
    public StringBuilder Text { get; } = new();

    /// <summary>
    /// 工具调用组内各项（Role=Tool 且非空 = 折叠组行「🔧 工具调用:N 次」）。
    /// 折叠不只是观感：以前每个输出 chunk 都新建一条气泡并滚动到底，一次大输出就是成百上千条气泡。
    /// </summary>
    public List<ToolCallItem> ToolCalls { get; } = new();

    /// <summary>思考正文（Role=Reasoning 时气泡只显示一行标题，全文进详情窗）</summary>
    public StringBuilder ReasoningBody { get; } = new();

    /// <summary>思考耗时（秒）：标题显示「已思考 N 秒」</summary>
    public int ThinkingSeconds { get; set; }

    /// <summary>思考起始时刻（计秒用）</summary>
    public DateTime ThinkStart { get; set; } = DateTime.UtcNow;

    /// <summary>思考是否已结束（决定标题是「思考中 Ns」还是「已思考 N 秒」）</summary>
    public bool ThinkingDone { get; set; }

    /// <summary>是否为折叠组行（工具调用）</summary>
    public bool IsToolGroup => Role == ChatRole.Tool && ToolCalls.Count > 0;

    /// <summary>组行文案：`🔧 工具调用:N 次`</summary>
    public string GroupLine => $"🔧 工具调用:{ToolCalls.Count} 次";

    /// <summary>思考行文案：`💭 思考中 Ns` / `💭 已思考 N 秒`</summary>
    public string ThinkingLine => ThinkingDone
        ? $"💭 已思考 {ThinkingSeconds} 秒"
        : $"💭 思考中 {ThinkingSeconds}s";

    /// <summary>物化后的气泡视图（在 MessagesHost 中），流式直接使用。</summary>
    public MessageBubble? View { get; set; }

    /// <summary>当前正在流式写入的是否是本消息（决定 EnsureAssistant 复用还是新建）。</summary>
    public bool Streaming { get; set; }

    public ChatMessage(ChatRole role) => Role = role;

    /// <summary>是否为普通文本消息（用户/助手/系统，用 MarkdownInlines 渲染）。</summary>
    public bool IsTextMessage => Role is ChatRole.User or ChatRole.Assistant or ChatRole.System;
}
