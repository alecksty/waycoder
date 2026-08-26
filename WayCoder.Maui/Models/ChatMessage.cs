using System.ComponentModel;
using Microsoft.Maui.Controls;

namespace WayCoder.Maui.Models;

/// <summary>聊天消息角色。</summary>
public enum ChatRole
{
    /// <summary>用户输入。</summary>
    User,
    /// <summary>AI 回复（正文，可流式）。</summary>
    Assistant,
    /// <summary>工具调用提示（灰色小字，非正文）。</summary>
    Tool,
}

/// <summary>聊天消息视图模型（CollectionView 绑定项）。</summary>
public sealed class ChatMessage : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public ChatRole Role { get; init; }

    /// <summary>原始文本（用户输入 / AI 正文，含 «» 中间格式标记）。</summary>
    public string RawText { get; set; } = "";

    /// <summary>AI 正文渲染后的富文本（«» 已解码）；用户/工具消息为 null。</summary>
    public FormattedString? Formatted { get; set; }

    /// <summary>是否正在流式接收（AI 正文尚未结束）。</summary>
    public bool IsStreaming { get; set; }

    private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
