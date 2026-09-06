using WayCoder.Maui.Markup;
using WayCoder.Maui.Models;

namespace WayCoder.Maui.Pages;

/// <summary>
/// 思考过程详情页（只读）——思考泡泡（「已思考 N 秒」一行字）的目标子页。
/// 泡泡内容默认折叠，点开经 <see cref="Target"/> 传消息引用，
/// 此页把该条 Thinking 消息的 <see cref="ChatMessage.Reasoning"/> 富文本渲染成整页滚动；
/// <see cref="ChatMessage.ThinkingSeconds"/> 用于顶部副标显示耗时。
/// </summary>
public partial class ReasoningDetailPage : ContentPage
{
    /// <summary>待展示的 Assistant 消息（ChatPage 点击入口时赋值；页 OnAppearing 读取渲染）。</summary>
    public static ChatMessage? Target { get; set; }

    public ReasoningDetailPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var msg = Target;
        Target = null; // 一次性消费，防返回后再进残留旧内容
        if (msg == null || string.IsNullOrEmpty(msg.Reasoning))
        {
            Body.Text = "（无思考内容）";
            return;
        }
        // 思考泡泡带耗时 → 顶部副标「已思考 N 秒」
        Intro.Text = msg.ThinkingSeconds >= 1
            ? $"模型推理过程 · 已思考 {(int)msg.ThinkingSeconds} 秒（未参与对话正文）"
            : "模型推理过程（未参与对话正文）";
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        // «» 中间格式 → 富文本（思考常为纯文本，Convert 兜底还原）
        Body.FormattedText = MarkupToFormattedString.Convert(msg.Reasoning, isDark);
    }
}
