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
				// 编辑器等宽字体：**自带**，不让平台 fallback。
				// 起因：用 "monospace" 时中文没有字形，测量路径与渲染路径各 fallback 各的，
				// 两边量出的中文宽度差了近一倍（测量 ≈9.8dp vs 渲染 ≈16.8dp），
				// 点击定位因此随非 ASCII 字符累积偏移。
				fonts.AddFont("SarasaMonoSC-Regular.ttf", "SarasaMonoSC");   // 别名须与 EditorTypography.FontFamilyName 一致
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

		// **横屏不要弹「全屏输入」**：横屏可用高度小，Android 的输入法会切到「抽取式编辑」
		// （extract mode）—— 整个屏幕变成一块编辑区，正在编辑的上下文被彻底盖住
		// （用户实测：一横屏，输入区就完全被挡住看不见了）。
		//
		// flagNoExtractUi 明确拒绝抽取式编辑界面，flagNoFullscreen 拒绝全屏模式，两个都上。
		// **必须 `|=` 而不是赋值**：MAUI 会用 ImeOptions 表达 ReturnType（例如 Done），
		// 直接赋值会把它覆盖掉，回车键就变成了换行。
		EntryHandler.Mapper.AppendToMapping("NoExtractIme", (handler, view) => ApplyNoExtractIme(handler.PlatformView));
		EditorHandler.Mapper.AppendToMapping("NoExtractIme", (handler, view) => ApplyNoExtractIme(handler.PlatformView));

		// **关掉平台的「选中操作」浮层**（长按弹出的 复制/粘贴/全选 工具条）。
		// 编辑器自带选区与操作条（见 EditorPage 的 SelectionBar），平台再弹一个就变成两套 UI、
		// 而且那套的锚点是按平台自己那层输入框算的 —— 与自绘正文差一点就「看着对不上」。
		// 回调返回 true = 这个 ActionMode **已被消费**（不显示），这是 Android 官方的关闭方式。
		EntryHandler.Mapper.AppendToMapping("NoSelectionToolbar", (handler, view) =>
			handler.PlatformView.CustomSelectionActionModeCallback = new SuppressActionMode());
		EditorHandler.Mapper.AppendToMapping("NoSelectionToolbar", (handler, view) =>
			handler.PlatformView.CustomSelectionActionModeCallback = new SuppressActionMode());

		static void ApplyNoExtractIme(Android.Widget.TextView view)
		{
			// ⚠ 绑定把 `ImeOptions` 暴露成 `ImeAction` 枚举，而 flag 位与动作位**共用同一个 int**，
			// 所以只能转成 int 来或 —— 直接 `|=` 编译不过。
			int opts = (int)view.ImeOptions;
			opts |= (int)Android.Views.InputMethods.ImeFlags.NoExtractUi;
			if (OperatingSystem.IsAndroidVersionAtLeast(26))
				opts |= (int)Android.Views.InputMethods.ImeFlags.NoFullscreen;
			view.ImeOptions = (Android.Views.InputMethods.ImeAction)opts;
		}
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

#if ANDROID
	/// <summary>
	/// 把平台的「选中操作」浮层（长按弹出的 复制/粘贴/全选 工具条）**吃掉** ——
	/// 三个回调一律返回 true（= 已消费），于是它不弹。
	///
	/// 存在的理由：编辑器的选区与操作条是自己的（见 <c>EditorPage.SelectionBar</c>），
	/// 平台再弹一套就变成两套 UI；而且那套的锚点是按平台自己那层输入框算的，
	/// 与自绘正文只要差一点就「看着对不上」。**少一个平台参与，就少一处坐标系不一致**。
	/// </summary>
	sealed class SuppressActionMode : Java.Lang.Object, Android.Views.ActionMode.ICallback
	{
		public bool OnCreateActionMode(Android.Views.ActionMode? mode, Android.Views.IMenu? menu) => true;
		public bool OnPrepareActionMode(Android.Views.ActionMode? mode, Android.Views.IMenu? menu) => true;
		public bool OnActionItemClicked(Android.Views.ActionMode? mode, Android.Views.IMenuItem? item) => true;
		public void OnDestroyActionMode(Android.Views.ActionMode? mode) { }
	}
#endif
}
