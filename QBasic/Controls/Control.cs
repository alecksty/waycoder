// =============================================================
// Control.cs —— 控件基类与焦点管理
//
// 所有控件继承 Control，拥有屏幕矩形区域（Bounds）、可见性、
// 焦点状态，并实现 Draw 方法把自己画到 Screen 上。基类还提供
// 事件处理钩子 OnKey / OnClick。
// =============================================================
using QBasic.Tui;

namespace QBasic.Controls;

/// <summary>控件基类。</summary>
public abstract class Control
{
    /// <summary>所在行（1-based，相对父容器）。</summary>
    public int Row { get; set; }
    /// <summary>所在列（1-based，相对父容器）。</summary>
    public int Col { get; set; }
    /// <summary>高度。</summary>
    public int Height { get; set; } = 1;
    /// <summary>宽度。</summary>
    public int Width { get; set; } = 10;

    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public bool Focused { get; set; }
    public string Name { get; set; } = "";
    public bool TabStop { get; set; } = true;

    /// <summary>父应用（由 AddControl 时设置）。</summary>
    public TuiApp? App { get; set; }

    /// <summary>是否可接受焦点。</summary>
    public virtual bool CanFocus => Visible && Enabled;

    /// <summary>控件是否包含某屏幕坐标（绝对）。</summary>
    public bool Contains(int absRow, int absCol)
    {
        int r = App != null ? App.RootRow : 0;
        int c = App != null ? App.RootCol : 0;
        int top = r + Row - 1, left = c + Col - 1;
        return absRow >= top && absRow < top + Height && absCol >= left && absCol < left + Width;
    }

    /// <summary>绘制控件到 Screen。</summary>
    public abstract void Draw(Screen screen);

    /// <summary>处理键盘事件；返回 true 表示已消费。</summary>
    public virtual bool OnKey(InputEvent ev) => false;

    /// <summary>处理鼠标点击；返回 true 表示已消费。</summary>
    public virtual bool OnClick(int relRow, int relCol) => false;

    /// <summary>请求重绘。</summary>
    public void Invalidate() => App?.Invalidate();

    /// <summary>请求获得焦点。</summary>
    public void RequestFocus() => App?.SetFocus(this);
}
