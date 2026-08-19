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
/// 结构化聊天消息：每消息独立持有原始文本与物化气泡视图。
/// 流式期间只更新最后一条 Assistant 消息的气泡，根治「全量重渲染历史」的 O(n²)。
/// </summary>
public sealed class ChatMessage
{
    public ChatRole Role { get; }
    /// <summary>保留原始 markdown / 纯文本（含 «» 标记），渲染时统一转 Inline。</summary>
    public StringBuilder Text { get; } = new();

    /// <summary>物化后的气泡视图（在 MessagesHost 中），流式直接使用。</summary>
    public MessageBubble? View { get; set; }

    /// <summary>当前正在流式写入的是否是本消息（决定 EnsureAssistant 复用还是新建）。</summary>
    public bool Streaming { get; set; }

    public ChatMessage(ChatRole role) => Role = role;

    /// <summary>是否为普通文本消息（用户/助手/系统，用 MarkdownInlines 渲染）。</summary>
    public bool IsTextMessage => Role is ChatRole.User or ChatRole.Assistant or ChatRole.System;
}
