using Android.App;
using Android.Content.PM;
using Android.OS;

namespace WayCoder.Maui;

// WindowSoftInputMode = AdjustResize：**编辑器必须靠它**。
// 默认可能选到 adjustPan（整窗上推），那样「滚动偏移 ↔ 屏幕 y 坐标」的换算全部失效，
// 点击定位、浮动输入框对位、键盘遮挡处理都会错位。
// ConfigChanges 里加 Keyboard/KeyboardHidden：否则部分机型弹出软键盘会重建 Activity，
// 编辑器状态（打开的文件、光标、滚动位置）全丢。
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    WindowSoftInputMode = Android.Views.SoftInput.AdjustResize,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density
        | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
public class MainActivity : MauiAppCompatActivity
{
    // **物理键盘的路由口**（电脑屏窗口用）。
    //
    // 为什么在 Activity 这一层拦：VML 窗口页是一整块自绘画布、**没有输入框**，
    // 而那条"给 EditText 挂 KeyPress"的老路（命令行页在用）要求有焦点控件。
    // 键事件本来就是 Activity 级的，在源头上接，页面谁都不用假装是个输入框。
    //
    // 返回 true = 这个键已被 VML 窗口吃掉、不再往下传；认不出的键（返回键/音量键）
    // 照常走 base —— **返回键必须放行**，否则用户退不出绘图窗口。
    public override bool DispatchKeyEvent(Android.Views.KeyEvent? e)
    {
        if (e != null && Services.HardwareKeys.TryDispatch(e)) return true;
        return base.DispatchKeyEvent(e);
    }

    // **手柄摇杆的入口**（外接蓝牙/USB 手柄）。
    //
    // ⚠ 与按键**不是同一条路**：摇杆是 `MotionEvent` 的**轴**事件
    //   （`ACTION_MOVE` + `AXIS_X/AXIS_Y`），`DispatchKeyEvent` 一辈子收不到它。
    //   只补按键不补这个，就是"手柄能按、但推杆没反应"。
    //
    // 处理的活全在 `HardwareKeys.TryDispatchMotion`（把轴翻成虚拟方向键）——
    // 这一层只负责"把 Activity 的入口接上"，与上面那条同一个分工。
    public override bool DispatchGenericMotionEvent(Android.Views.MotionEvent? e)
    {
        if (e != null && Services.HardwareKeys.TryDispatchMotion(e)) return true;
        return base.DispatchGenericMotionEvent(e);
    }
}
