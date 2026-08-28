using WayCoder.UI.Shared;

namespace WayCoder.UI.Tui;

/// <summary>
/// 带边框控件基类 —— 统一边框样式/颜色与边框字符获取。
/// 纯展示边框控件（框/线/侧栏）与可聚焦边框控件（PromptBar 覆写 CanFocus=true）共用，
/// 替代各处重复的 BorderStyle 属性声明与 <c>AnsiHelper.GetBorderChars</c> 调用。
/// </summary>
public abstract class TuiBorderedControl : TuiDisplayControl
{
    /// <summary>边框样式（默认圆角）</summary>
    public WindowBorder BorderStyle { get; set; } = WindowBorder.Rounded;

    /// <summary>边框颜色（ANSI 色码，0=自动/继承）</summary>
    public int BorderColor { get; set; }

    /// <summary>按 BorderStyle 取边框字符集（统一入口，替代各处重复的 switch）</summary>
    protected AnsiHelper.BorderChars GetBorderChars() => AnsiHelper.GetBorderChars(BorderStyle);
}
