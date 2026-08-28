namespace WayCoder.UI.Tui;

/// <summary>
/// 纯展示控件基类 —— 不参与键盘焦点（CanFocus=false）。
/// 收纳所有「只展示不交互」的控件（Label/Icon/Banner/Spinner/进度条等），
/// 避免每个控件重复声明 <c>CanFocus =&gt; false</c> 覆写。
/// </summary>
public abstract class TuiDisplayControl : TuiControl
{
    public override bool CanFocus => false;
}
