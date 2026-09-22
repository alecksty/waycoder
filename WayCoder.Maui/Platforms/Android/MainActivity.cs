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
}
