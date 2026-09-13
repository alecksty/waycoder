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
}
