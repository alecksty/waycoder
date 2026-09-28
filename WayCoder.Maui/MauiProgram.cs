using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;

namespace WayCoder.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		// 界面语言：**跟随系统**（用户 2026-09-28 定）。放在**第一行** —— 这是最早的可控托管代码，
		// 早于 App 构造函数、早于任何 Config 静态构造，确保没有任何文案在语言定下来之前被取用。
		// MauiBootstrap.Initialize() 里还会再调一次（幂等）：那处是既有的"必须最先"契约点，
		// 冗余一次换防御。自测走另一条入口（显式钉中文），不受影响。
		Services.MauiLang.Initialize();

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
		// 把 Entry 的处理器换成我们自己的 —— 只为让**编辑器那一个输入框**换成
		// BackspaceAwareEditText（软键盘的行首退格走 InputConnection，只有子类覆写
		// OnCreateInputConnection 才接得住，mapper 换不了平台视图的类型）。
		// 非编辑器那一支在 CreatePlatformView 里按 StyleId 分流回原类型，其余输入框不受影响。
		builder.ConfigureMauiHandlers(h =>
			h.AddHandler<Microsoft.Maui.Controls.Entry, EditorEntryHandler>());
#endif

#if IOS || MACCATALYST
		// 同上，iOS 侧换的是 TabAwareTextField —— **硬件键盘的 Tab 是「命令键」**（不走
		// ShouldChangeCharacters），要由控件自己的类在 KeyCommands 里声明 UIKeyCommand。
		// 非编辑器那一支同样在 CreatePlatformView 里按 StyleId 分流回原类型。
		//
		// ⚠⚠ **必须走这条「按类型注册」的路，绝不能在页面里 `LineEditor.Handler = new …()`**
		//   （v0.96.510 修）：MAUI **只在它自己创建 handler 时**才替我们设好 `MauiContext`，
		//   手工赋时上下文是 null，而赋值会立刻跑一遍属性映射器 —— 其中 `EntryHandler.MapFont`
		//   要取 `IFontManager` ⇒ 抛 `InvalidOperationException: Unable to find the context.
		//   The MauiContext property should have been set by the host.`。
		//   页面构造函数阶段还没进窗口，必然踩中 ⇒ **iPad/iPhone 上编辑器整个打不开**
		//   （Android 侧那个 handler 的注释里早写明了别用这条路，iOS 侧当初没照做）。
		builder.ConfigureMauiHandlers(h =>
			h.AddHandler<Microsoft.Maui.Controls.Entry, WayCoder.Maui.Pages.TabAwareEntryHandler>());
#endif

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

		// **关掉平台的「选中操作」浮层**（长按弹出的 剪切/复制/粘贴/全选 工具条）——**只对编辑器那一层**。
		//
		// ⚠ 两条都踩过：
		// ① **`onCreateActionMode` 返回 `false` 才是「不创建」**。返回 `true` 是「创建」——
		//    一开始写反了，于是工具条照弹，只是每个菜单项都被 `onActionItemClicked` 吃掉，
		//    变成一条**点不动的**工具条，比不弹还糟。
		// ② **这两个 mapper 是全局静态的**（`EntryHandler.Mapper`），不判 StyleId 就会把 App 里
		//    **所有** Entry 的复制/粘贴一起废掉 —— 聊天输入框、API Key、仓库地址… 而它们**只有**
		//    系统那一套粘贴入口（`Clipboard.*` 在 EditorPage 之外没有出现）。同文件上面的
		//    `TransparentText` 就是判 `StyleId` 的，照它写。
		EntryHandler.Mapper.AppendToMapping("NoSelectionToolbar", (handler, view) =>
		{
			if (view is Microsoft.Maui.Controls.Element el && el.StyleId == EditorLineStyleId)
				handler.PlatformView.CustomSelectionActionModeCallback = new SuppressActionMode();
		});
		EditorHandler.Mapper.AppendToMapping("NoSelectionToolbar", (handler, view) =>
		{
			if (view is Microsoft.Maui.Controls.Element el && el.StyleId == EditorLineStyleId)
				handler.PlatformView.CustomSelectionActionModeCallback = new SuppressActionMode();
		});

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

#if ANDROID || IOS || MACCATALYST
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
#elif IOS || MACCATALYST
			handler.PlatformView.TextColor = UIKit.UIColor.Clear;
			var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
			handler.PlatformView.TintColor = isDark ? UIKit.UIColor.White : UIKit.UIColor.Black;
#endif
		});
#endif

		return builder.Build();
	}

	/// <summary>
	/// 给编辑器的浮动输入框打的标记。只有带这个 StyleId 的 Entry 才：
	///   · Android —— 关掉平台的选中浮层、换上 BackspaceAwareEditText（见 <c>EditorEntryHandler</c>）；
	///   · iOS/MacCatalyst —— 换上 TabAwareTextField（见 <c>TabAwareEntryHandler</c>）。
	/// 判据必须靠标记而不是「是不是编辑器那个控件」：那几处 mapper/handler **是全局的**，
	/// 不判 StyleId 会误伤全 App 的 Entry（<c>NoSelectionToolbar</c> 的注释里记过这一条）。
	///
	/// ⚠ **本常量刻意放在 `#if ANDROID` 外面** —— 它同时被 iOS 侧的 handler 用
	/// （v0.96.510：原先只在 Android 段里，iOS 一编译就 CS0117）。
	/// </summary>
	internal const string EditorLineStyleId = "code-line-editor";

#if ANDROID

	/// <summary>
	/// 平台「选中操作」浮层的回调：**`OnCreateActionMode` 返回 false = 不创建**，
	/// 于是那条工具条根本不出现。
	///
	/// 存在的理由：编辑器的选区与操作条是自己的（见 <c>EditorPage.SelectionBar</c>），
	/// 平台再弹一套就变成两套 UI；而且那套的锚点是按平台自己那层输入框算的，
	/// 与自绘正文只要差一点就「看着对不上」。**少一个平台参与，就少一处坐标系不一致**。
	/// </summary>
	sealed class SuppressActionMode : Java.Lang.Object, Android.Views.ActionMode.ICallback
	{
		public bool OnCreateActionMode(Android.Views.ActionMode? mode, Android.Views.IMenu? menu) => false;
		public bool OnPrepareActionMode(Android.Views.ActionMode? mode, Android.Views.IMenu? menu) => false;
		public bool OnActionItemClicked(Android.Views.ActionMode? mode, Android.Views.IMenuItem? item) => true;
		public void OnDestroyActionMode(Android.Views.ActionMode? mode) { }
	}
#endif
}
