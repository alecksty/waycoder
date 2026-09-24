namespace WayCoder.Maui.Services;

/// <summary>
/// VML 程序的**主动截屏**（`ui_screenshot` / #588）—— 把**本 App 窗口**抓成 RGBA 像素。
///
/// ## 为什么这里必须分平台
///
/// 「把当前窗口抓成一张图」没有任何跨平台 API：Android 要 `View.Draw(Canvas)`、
/// Apple 系要把 `CALayer` 渲染进 `CGBitmapContext`、Windows 要 `RenderTargetBitmap`。
/// 与 <see cref="VmlAudio"/> 同一处置：**一个文件里按平台分段**（不是 partial 类），
/// 没接的平台返回 null —— 那是"这一端没有这个能力"，共享层翻成失败码 -1，不是错误。
///
/// ## 为什么截「窗口」而不是「整个屏幕」
///
/// 整屏截取在 Android 要走 `MediaProjection`（弹系统授权框、用户可拒、还要前台服务），
/// iOS 基本做不到。而"截自己的窗口"四端都有**无权限**的做法，失败路径少得多。
/// 代价是拿不到状态栏与其他 App —— 对"游戏存一张战绩图"这个用途，那不是损失。
///
/// ⚠ **桌面命令行端（`scripts/vmlcli`）没有 App 窗口**，它退化为光栅化 VML 场景
/// （见 `CliVmlHost.CaptureAppWindow`）。所以同一份程序在桌面与手机上截出来的内容
/// **不一样**，这是有意为之：那条端到端链路能在桌面上验完，不必每改一行就打 APK。
///
/// ## 两条硬约束
///
/// · **碰 View 必须在主线程** ⇒ 唯一入口 <see cref="CaptureAsync"/> 由调用方
///   （`MauiVmlHost.CaptureAppWindow`）用 `MainThread.InvokeOnMainThreadAsync` 包住。
///   ⚠ 别在这里自己 marshal —— 那会在"调用方已经在主线程"时套两层，
///   而 WinUI 的 `RenderAsync` 恰恰不能在 UI 线程上被阻塞等待（会自锁）。
/// · **不许抛**：VM 线程上的异常会把整台虚拟机带走，而程序那边只看到"窗口没了"。
///   截不到就回 null。
/// </summary>
internal static class VmlWindowCapture
{
    /// <summary>
    /// 截本 App 窗口。返回 null = 窗口还没出来 / 这一端没接 / 抓不到像素（三种都当失败）。
    /// **必须在主线程上调用。**
    /// </summary>
    public static async Task<RasterImage?> CaptureAsync()
    {
        try
        {
            return await CaptureCoreAsync();
        }
        catch (Exception ex)
        {
            ErrorLog.Error("VmlCapture", "截屏失败", ex);
            return null;
        }
    }

    /// <summary>
    /// 平台分派。**单独一层是为了让外层的 try/catch 也能盖住 Windows 那条异步路径**
    /// （直接在本方法上写 `async` 的话，Android/Apple 两支没有 `await`，
    /// 编译器会报 CS1998「异步方法缺少 await」）。
    /// </summary>
    private static Task<RasterImage?> CaptureCoreAsync()
    {
#if ANDROID
        return Task.FromResult(CaptureAndroid());
#elif IOS || MACCATALYST
        return Task.FromResult(CaptureApple());
#elif WINDOWS
        return CaptureWindowsAsync();
#else
        return Task.FromResult<RasterImage?>(null);   // 这一端没接
#endif
    }

#if ANDROID
    /// <summary>
    /// Android：把当前 Activity 的 `DecorView` 画进一张 Bitmap。
    ///
    /// 用 `DecorView` 而不是某个页面 —— 要的就是"用户此刻看到的那一屏"
    /// （绘图页 + 屏幕手柄 + 顶部标题栏都在里面）。
    ///
    /// ⚠ 取像素用 **`GetPixels` 回 `int[]`（每项 `0xAARRGGBB`）再自己拆成 RGBA**，
    ///   不用 `CopyPixelsToBuffer`：那个拿到的字节顺序依赖 Bitmap 的内存布局假设，
    ///   而"ARGB int → RGBA 字节"这一步是显式写出来的、读得出来谁是谁
    ///   （本仓栽过通道序的跟头：`RasterImage.ColorAt` 是 `0xAARRGGBB`，
    ///   而把 `#RRGGBBAA` 当 `#AARRGGBB` 用会让整块颜色变掉）。
    ///
    /// ⚠ **已知风险**：硬件加速的视图树上 `View.Draw` 可能拿到空白
    ///   （带 `RenderNode` 的层不参与软件绘制）。真机上若截出来是全透明/全黑，
    ///   退路是 `PixelCopy.Request`（API 26+）—— 刻意先不写：它异步、要额外的失败路径，
    ///   而多数情况 `Draw` 够用。**这条只能真机验，桌面证明不了。**
    /// </summary>
    private static RasterImage? CaptureAndroid()
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        var view = activity?.Window?.DecorView;
        if (view is null) return null;

        int w = view.Width, h = view.Height;
        if (w <= 0 || h <= 0) return null;

        using var bmp = Android.Graphics.Bitmap.CreateBitmap(w, h, Android.Graphics.Bitmap.Config.Argb8888!);
        if (bmp is null) return null;

        using (var canvas = new Android.Graphics.Canvas(bmp))
        {
            view.Draw(canvas);
        }

        var px = new int[w * h];
        bmp.GetPixels(px, 0, w, 0, 0, w, h);

        var rgba = new byte[w * h * 4];
        for (int i = 0; i < px.Length; i++)
        {
            var c = px[i];
            rgba[i * 4] = (byte)((c >> 16) & 0xFF);      // R
            rgba[i * 4 + 1] = (byte)((c >> 8) & 0xFF);   // G
            rgba[i * 4 + 2] = (byte)(c & 0xFF);          // B
            rgba[i * 4 + 3] = (byte)((c >> 24) & 0xFF);  // A
        }
        return new RasterImage(w, h, rgba);
    }
#endif

#if IOS || MACCATALYST
    /// <summary>
    /// iOS / MacCatalyst：把窗口的 `CALayer` 渲染进一个 `CGBitmapContext`，直接读像素。
    ///
    /// ⚠ 位图参数组 **`PremultipliedLast | ByteOrder32Big`**：前者把 alpha 放在末字节，
    ///   后者指定 32 位大端 ⇒ **内存里就是 R,G,B,A**，读出来不用再转序。
    ///   这与 `GitSyncPage` 解码扫码图用的是同一组合（那边有更详细的说明），
    ///   换掉任一个都会得到通道错位的图。
    ///
    /// ⚠ Quartz 的用户空间原点在**左下**，而我们要"内存首行 = 屏幕顶" ⇒ 先翻转坐标系
    ///   （`TranslateCTM(0,h)` + `ScaleCTM(1,-1)`）。少了这一步图象会**垂直镜像**——
    ///   同一坑在 `GitSyncPage` 里已经踩过一次。
    ///
    /// 窗口从 MAUI 的 `Window.Handler.PlatformView` 取，不用
    /// `UIApplication.SharedApplication.KeyWindow`（后者 iOS 13 起已废弃，
    /// 会带一条用不掉的弃用告警）。
    /// </summary>
    private static RasterImage? CaptureApple()
    {
        var mauiWindow = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault();
        if (mauiWindow?.Handler?.PlatformView is not UIKit.UIWindow window) return null;

        var size = window.Bounds.Size;
        int w = (int)Math.Round(size.Width), h = (int)Math.Round(size.Height);
        if (w <= 0 || h <= 0) return null;

        using var cs = CoreGraphics.CGColorSpace.CreateDeviceRGB();
        var flags = CoreGraphics.CGBitmapFlags.PremultipliedLast
                  | CoreGraphics.CGBitmapFlags.ByteOrder32Big;
        using var ctx = new CoreGraphics.CGBitmapContext(IntPtr.Zero, w, h, 8, w * 4, cs, flags);
        if (ctx.Data == IntPtr.Zero) return null;

        ctx.TranslateCTM(0, h);
        ctx.ScaleCTM(1, -1);
        window.Layer.RenderInContext(ctx);

        var rgba = new byte[w * h * 4];
        System.Runtime.InteropServices.Marshal.Copy(ctx.Data, rgba, 0, rgba.Length);
        return new RasterImage(w, h, rgba);
    }
#endif

#if WINDOWS
    /// <summary>
    /// Windows：WinUI 的 `RenderTargetBitmap` 渲染窗口根元素。
    ///
    /// ⚠ 它是**异步的**（`RenderAsync` 要等合成器拍一帧）⇒ 这里 `await`，
    ///   **绝不能** 在 UI 线程上 `.GetAwaiter().GetResult()` —— 那会自锁死等一个
    ///   需要 UI 线程才能跑完的任务。这正是 <see cref="CaptureCoreAsync"/> 要把
    ///   Windows 单独分出去的原因（调用方已经在主线程上了，所以我们这里是
    ///   "在主线程上异步等待"，不是阻塞它）。
    ///
    /// ⚠ `GetPixelsAsync` 给的是 **BGRA8**（预乘 alpha），不是 RGBA ⇒ 必须换序。
    /// </summary>
    private static async Task<RasterImage?> CaptureWindowsAsync()
    {
        var mauiWindow = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault();
        if (mauiWindow?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window window) return null;
        if (window.Content is not Microsoft.UI.Xaml.FrameworkElement root) return null;

        var rtb = new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap();
        await rtb.RenderAsync(root);

        int w = rtb.PixelWidth, h = rtb.PixelHeight;
        if (w <= 0 || h <= 0) return null;

        var buffer = await rtb.GetPixelsAsync();
        var bgra = new byte[buffer.Length];
        using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer))
        {
            reader.ReadBytes(bgra);
        }

        var rgba = new byte[w * h * 4];
        for (int i = 0; i < w * h; i++)
        {
            rgba[i * 4] = bgra[i * 4 + 2];       // R ← B
            rgba[i * 4 + 1] = bgra[i * 4 + 1];   // G
            rgba[i * 4 + 2] = bgra[i * 4];       // B ← R
            rgba[i * 4 + 3] = bgra[i * 4 + 3];   // A
        }
        return new RasterImage(w, h, rgba);
    }
#endif
}
