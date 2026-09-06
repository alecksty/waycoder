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
    /// <summary>工具调用组提示 / 独立灰字行（错误、任务摘要），非正文。</summary>
    Tool,
}

/// <summary>
/// 单个工具调用记录（工具组内一项）。Name + 参数摘要 Summary 在工具开始即定；
/// Detail（输出）由 onToolOutput 流式追加。运行时只增不改，只读展示无需深拷贝。
/// </summary>
public sealed class ToolCallItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>工具名（如 write_file / bash）。</summary>
    public string Name { get; init; } = "";

    /// <summary>参数摘要（onTool 的 summary，可能含 file_path=…）。</summary>
    public string Summary { get; init; } = "";

    /// <summary>摘要里的 file_path= 值（详情页语法高亮语言推断）。</summary>
    public string? FilePath { get; init; }

    /// <summary>是否深色主题（工具输出渲染配色用）。</summary>
    public bool IsDark { get; init; }

    private string _detail = "";
    /// <summary>工具输出详情（onToolOutput 流式累积；详情页打开时惰性渲染）。</summary>
    public string Detail
    {
        get => _detail;
        set { _detail = value; OnChanged(nameof(Detail)); }
    }

    private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>聊天消息视图模型（CollectionView 绑定项）。</summary>
public sealed class ChatMessage : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public ChatRole Role { get; init; }

    private string _rawText = "";
    /// <summary>原始文本（用户输入 / AI 正文，含 «» 中间格式标记；Tool 角色为组标题或灰字行文本）。</summary>
    public string RawText
    {
        get => _rawText;
        set { _rawText = value; OnChanged(nameof(RawText)); }
    }

    private FormattedString? _formatted;
    /// <summary>AI 正文渲染后的富文本（«» 已解码）；用户/工具消息为 null。</summary>
    public FormattedString? Formatted
    {
        get => _formatted;
        set { _formatted = value; OnChanged(nameof(Formatted)); }
    }

    private bool _isStreaming;
    /// <summary>是否正在流式接收（AI 正文尚未结束）。</summary>
    public bool IsStreaming
    {
        get => _isStreaming;
        set { _isStreaming = value; OnChanged(nameof(IsStreaming)); }
    }

    private string _reasoning = "";
    /// <summary>思考过程文本（仅 Assistant 角色有；聊天流不展示，经「💭 查看思考」弹子页查看）。</summary>
    public string Reasoning
    {
        get => _reasoning;
        set { _reasoning = value; OnChanged(nameof(Reasoning)); }
    }

    private bool _hasReasoning;
    /// <summary>是否有思考过程（有则在 AI 气泡顶部显示「💭 查看思考」入口）。</summary>
    public bool HasReasoning
    {
        get => _hasReasoning;
        set { _hasReasoning = value; OnChanged(nameof(HasReasoning)); }
    }

    /// <summary>工具调用组内容（仅 Tool 角色有；非空 = 组样式「工具调用:N 次」）。</summary>
    public List<ToolCallItem> ToolCalls { get; } = new();

    /// <summary>本组已调用工具数（模板组标题 N 用；Add 后由 code 刷新 RawText 组标题）。</summary>
    public int ToolCount => ToolCalls.Count;

    /// <summary>是否为工具调用组（区别于普通灰字行：错误 / 任务摘要等 Tool 消息无 ToolCalls）。</summary>
    public bool HasToolCalls => ToolCalls.Count > 0;

    /// <summary>是否普通灰字行（非工具组；消息创建即定态，无运行时变化）。</summary>
    public bool IsPlainTool => ToolCalls.Count == 0;

    /// <summary>是否深色主题（思考详情页配色用）。</summary>
    public bool IsDark { get; set; }

    private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
