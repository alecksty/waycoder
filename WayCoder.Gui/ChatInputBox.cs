using Avalonia.Controls;
using Avalonia.Input;

namespace WayCoder.UI.Gui;

/// <summary>
/// 聊天输入框 —— 多行 TextBox，但 **Enter 发送 / Shift+Enter 换行**。
///
/// 为什么需要子类，而不是在 XAML 上挂 KeyDown：
/// <see cref="TextBox"/> 自己的（类处理器）会先一步处理 Enter —— `AcceptsReturn=True` 时它插入换行
/// 并置 `Handled=true`，于是挂在同一元素上的普通 KeyDown 处理器被**直接跳过**，
/// 表现为「按回车没反应，只能点发送按钮」。靠事件阶段（隧道）绕开依赖 Avalonia 的内部处理顺序，
/// 不如直接覆写 <see cref="OnKeyDown"/> —— 我就是那个类处理器，顺序不存在歧义。
/// </summary>
public class ChatInputBox : TextBox
{
    /// <summary>用户按 Enter（未按 Shift）请求发送。由宿主订阅。</summary>
    public event Action? SendRequested;

    /// <summary>沿用 TextBox 的主题：Avalonia 按 StyleKey 查 ControlTheme，子类默认用自己的类型作 key，
    /// 不覆写这里会找不到主题 → 输入框退化成无边框/无光标的裸控件。</summary>
    protected override Type StyleKeyOverride => typeof(TextBox);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            e.Handled = true;
            SendRequested?.Invoke();
            return; // 不调用 base：否则基类会再插一个换行，发送后输入框留个空行
        }

        base.OnKeyDown(e); // Shift+Enter 等交给基类（插换行）
    }
}
