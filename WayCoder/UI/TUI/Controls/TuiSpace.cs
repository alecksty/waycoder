using System.Text;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 空白占位控件 —— 只在布局中占据一块空间，不绘制任何内容、不响应输入、不参与焦点。
/// 用于 VBox/HBox 里需要「留白 / 隔行」的占位场景（如对话框两行按钮之间、控件间距），
/// 语义比「空 Label」更明确；透明区域透出父容器背景。
/// </summary>
public class TuiSpace : TuiDisplayControl
{
    public TuiSpace() { Height = 1; Width = 1; }

    /// <summary>占位控件不参与 Tab 焦点遍历。</summary>

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        // 占位专用：什么都不画（Bg=0 时 Render 也不填充底色，区域透出父背景）
    }
}
