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
		// 去掉 Android Editor 默认下划线（underbar）——聊天输入框/编辑器底部那条横线
		EditorHandler.Mapper.AppendToMapping("RemoveUnderline", (handler, view) =>
		{
			handler.PlatformView.Background = null;
		});
#endif

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
