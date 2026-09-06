using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;

namespace WayCoder.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if ANDROID
		// 去掉 Android 原生下划线（underbar）：Editor 用于编辑器/多行输入，Entry 用于聊天输入框与
		// 各设置单行输入——原生 EditText/AppCompatEditText 默认底部一条横线，iOS 无，观感不一致。
		EditorHandler.Mapper.AppendToMapping("RemoveUnderline", (handler, view) =>
		{
			handler.PlatformView.Background = null;
		});
		EntryHandler.Mapper.AppendToMapping("RemoveUnderline", (handler, view) =>
		{
			handler.PlatformView.Background = null;
		});
#endif

#if ANDROID || IOS
		// 编辑器「透明文字叠加」语法高亮：仅 CodeEditor（StyleId="code-editor"）文字透明、光标保留；
		// 底层由 EditorPage 的高亮 Label（FormattedString）显示着色文本。
		EditorHandler.Mapper.AppendToMapping("TransparentText", (handler, view) =>
		{
			if (view is not Microsoft.Maui.Controls.Element el || el.StyleId != "code-editor") return;
#if ANDROID
			handler.PlatformView.SetTextColor(Android.Graphics.Color.Transparent);
			if (OperatingSystem.IsAndroidVersionAtLeast(29))
			{
				var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
				handler.PlatformView.TextCursorDrawable = new Android.Graphics.Drawables.ColorDrawable(
					isDark ? Android.Graphics.Color.White : Android.Graphics.Color.Black);
			}
#elif IOS
			handler.PlatformView.TextColor = UIKit.UIColor.Clear;
			var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
			handler.PlatformView.TintColor = isDark ? UIKit.UIColor.White : UIKit.UIColor.Black;
#endif
		});
#endif

		return builder.Build();
	}
}
