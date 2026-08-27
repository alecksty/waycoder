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

    private string _rawText = "";
    /// <summary>原始文本（用户输入 / AI 正文，含 «» 中间格式标记）。</summary>
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
    /// <summary>思考过程文本（仅 Assistant 角色有；与正文分离，默认折叠显示）。</summary>
    public string Reasoning
    {
        get => _reasoning;
        set { _reasoning = value; OnChanged(nameof(Reasoning)); }
    }

    private bool _hasReasoning;
    /// <summary>是否有思考过程（有则显示「💭 思考过程」折叠条）。</summary>
    public bool HasReasoning
    {
        get => _hasReasoning;
        set { _hasReasoning = value; OnChanged(nameof(HasReasoning)); }
    }

    private bool _isReasoningExpanded;
    /// <summary>思考过程是否展开（默认 false = 折叠；流式生成时临时 true 实时显示）。</summary>
    public bool IsReasoningExpanded
    {
        get => _isReasoningExpanded;
        set { _isReasoningExpanded = value; OnChanged(nameof(IsReasoningExpanded)); }
    }

    private string _toolSummary = "";
    /// <summary>工具参数摘要（onTool 的 summary，仅 Tool 角色有）。</summary>
    public string ToolSummary
    {
        get => _toolSummary;
        set { _toolSummary = value; OnChanged(nameof(ToolSummary)); OnChanged(nameof(HasToolSummary)); }
    }

    /// <summary>是否有工具参数摘要（非空才显示摘要行，避免空行占位）。</summary>
    public bool HasToolSummary => !string.IsNullOrEmpty(_toolSummary);

    private string _toolDetail = "";
    /// <summary>工具输出详情（onToolOutput 累积，仅 Tool 角色有）。</summary>
    public string ToolDetail
    {
        get => _toolDetail;
        set
        {
            _toolDetail = value;
            _toolDetailFormatted = null;   // 内容变更 → 置空，展开时惰性重算
            OnChanged(nameof(ToolDetail));
            OnChanged(nameof(ToolDetailFormatted));
        }
    }

    private bool _hasToolDetail;
    /// <summary>是否有工具输出详情（有则显示「▸ 输出详情」折叠条）。</summary>
    public bool HasToolDetail
    {
        get => _hasToolDetail;
        set { _hasToolDetail = value; OnChanged(nameof(HasToolDetail)); }
    }

    private bool _isToolDetailExpanded;
    /// <summary>工具输出详情是否展开（默认 false = 折叠）。</summary>
    public bool IsToolDetailExpanded
    {
        get => _isToolDetailExpanded;
        set { _isToolDetailExpanded = value; OnChanged(nameof(IsToolDetailExpanded)); OnChanged(nameof(ToolDetailFormatted)); }
    }

    /// <summary>工具对应文件路径（onTool 从 summary 的 file_path= 解析，供语言推断）。</summary>
    public string? ToolFilePath { get; set; }

    /// <summary>是否深色主题（工具详情渲染配色用）。</summary>
    public bool IsDark { get; set; }

    private FormattedString? _toolDetailFormatted;
    /// <summary>工具详情渲染富文本（«» 解码 + 代码/diff 语法高亮）。仅在展开时惰性计算。</summary>
    public FormattedString? ToolDetailFormatted
    {
        get
        {
            if (!IsToolDetailExpanded) return null;
            if (_toolDetailFormatted == null && !string.IsNullOrEmpty(ToolDetail))
            {
                try { _toolDetailFormatted = Markup.ToolOutputFormatter.Render(ToolDetail, ToolFilePath, IsDark); }
                catch { _toolDetailFormatted = null; }
            }
            return _toolDetailFormatted;
        }
    }

    private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
