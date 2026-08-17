using WayCoder.UI.Tui;

namespace WayCoder.UI.TUI.Base;

/// <summary>
/// 界面元素统一基类 —— Screen、Window、View、Control 的共同根。
/// 提炼所有 UI 元素的公共属性与方法：坐标、尺寸、脏标记、生命周期、输入路由。
/// </summary>
public abstract class TuiBase
{
    // ── 位置与尺寸 ──
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 10;
    public int Height { get; set; } = 1;

    /// <summary>Flex 弹性布局权重。0=固定尺寸，>0=在父容器中按比例分配剩余空间。</summary>
    public int Flex { get; set; }

    // ── 标识 ──
    /// <summary>元素名称（Screen 用于切换、Window 可选覆盖 Title）</summary>
    public string Name { get; set; } = "";

    /// <summary>用户数据挂载点（任意对象）</summary>
    public object? Tag { get; set; }

    // ── 脏标记（增量渲染） ──

    /// <summary>是否需要重绘。true=下一帧重新渲染此元素。</summary>
    public bool IsDirty { get; set; } = true;

    /// <summary>标记元素需要重绘。子类覆写以实现父链传播。</summary>
    public virtual void MarkDirty() => IsDirty = true;

    /// <summary>清除脏标记（渲染完成后由框架调用）</summary>
    public void ClearDirty() => IsDirty = false;

    /// <summary>强制刷新：标记元素及其子节点为脏，确保下一帧完全重绘。</summary>
    public virtual void Invalidate() => IsDirty = true;

    // ── 生命周期 ──

    /// <summary>元素创建/加入时调用。初始化子对象、订阅事件。</summary>
    public virtual void OnCreate()
    {
    }

    /// <summary>元素销毁/移除时调用。取消订阅、释放资源。</summary>
    public virtual void OnDestroy()
    {
    }

    // ── 输入路由 ──

    /// <summary>键盘输入。返回 true 表示已消费按键。</summary>
    public virtual bool OnKey(ConsoleKeyInfo key) => false;

    /// <summary>鼠标事件。返回 true 表示已消费事件。</summary>
    public virtual bool OnMouse(InputEvent ev) => false;

    // ── 尺寸变化 ──

    /// <summary>容器/终端尺寸变化通知。</summary>
    public virtual void OnResize(int newW, int newH)
    {
    }
}