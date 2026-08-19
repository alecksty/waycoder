using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

namespace WayCoder.UI.Gui;

/// <summary>
/// 消息气泡（Border 子类）——按角色设置对齐/圆角/背景（动态资源，切主题自动重绘）。
/// 内容为多 block 容器（StackPanel），用 MarkdownBlocks 渲染段落/代码块/表格等。
/// AppendToken 只重渲染本气泡；流式合帧由 MainWindow 控制。
/// </summary>
public sealed class MessageBubble : Border
{
    private const double CornerR = 14;

    private readonly StackPanel _host = new() { Spacing = 4 };

    public ChatMessage Message { get; }

    public MessageBubble(ChatMessage msg)
    {
        Message = msg;
        Child = _host;

        switch (msg.Role)
        {
            case ChatRole.User:
                HorizontalAlignment = HorizontalAlignment.Right;
                CornerRadius = new CornerRadius(CornerR, CornerR, 4, CornerR);
                Padding = new Thickness(12, 8);
                this[!BackgroundProperty] = new DynamicResourceExtension("UserBubbleBgBrush");
                break;

            case ChatRole.Tool:
                HorizontalAlignment = HorizontalAlignment.Left;
                CornerRadius = new CornerRadius(CornerR, CornerR, CornerR, 4);
                Padding = new Thickness(12, 8);
                this[!BackgroundProperty] = new DynamicResourceExtension("ToolBubbleBgBrush");
                break;

            case ChatRole.ToolOutput:
                HorizontalAlignment = HorizontalAlignment.Left;
                CornerRadius = new CornerRadius(10);
                Padding = new Thickness(12, 8);
                this[!BackgroundProperty] = new DynamicResourceExtension("Panel2BgBrush");
                this[!BorderBrushProperty] = new DynamicResourceExtension("BorderBrush");
                BorderThickness = new Thickness(1);
                break;

            case ChatRole.System:
                HorizontalAlignment = HorizontalAlignment.Center;
                CornerRadius = new CornerRadius(CornerR);
                Padding = new Thickness(10, 4);
                this[!BackgroundProperty] = new DynamicResourceExtension("Panel2BgBrush");
                _host.Opacity = 0.8;
                break;

            case ChatRole.Reasoning:
                // 推理内容：淡色小字（对齐 Web .msg.reasoning）
                HorizontalAlignment = HorizontalAlignment.Left;
                CornerRadius = new CornerRadius(CornerR);
                Padding = new Thickness(6, 2);
                _host.Opacity = 0.65;
                MaxWidth = 560;
                break;

            default: // Assistant
                HorizontalAlignment = HorizontalAlignment.Left;
                CornerRadius = new CornerRadius(CornerR, CornerR, CornerR, 4);
                Padding = new Thickness(12, 8);
                this[!BackgroundProperty] = new DynamicResourceExtension("Panel2BgBrush");
                this[!BorderBrushProperty] = new DynamicResourceExtension("BorderBrush");
                BorderThickness = new Thickness(1);
                break;
        }

        MaxWidth = 640; // 约聊天区 85%（表格需要更宽）

        Render(); // 构造即渲染内容（否则非流式消息/会话恢复历史为空白气泡）
    }

    /// <summary>追加流式 token 并重渲染本气泡（重建内部 blocks）。</summary>
    public void AppendToken(string token)
    {
        Message.Text.Append(token);
        Render();
    }

    /// <summary>全量重渲染本气泡（槽位切换/主题切换时用）。</summary>
    public void Render()
    {
        _host.Children.Clear();
        foreach (var block in MarkdownBlocks.Build(Message.Text.ToString()))
            _host.Children.Add(block);
    }
}
