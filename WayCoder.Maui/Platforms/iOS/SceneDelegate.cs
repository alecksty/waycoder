using Foundation;

namespace WayCoder.Maui;

/// <summary>
/// iOS 的 **UIScene 生命周期**委托 —— 空的，它的存在只为"采用 scene 生命周期"。
///
/// <para>
/// **为什么必须有**（2026-09-27 修）：**用 SDK 27 编译的包在 iOS 27 上直接起不来** —— UIKit 断言
/// <c>Application failed to launch: UIScene life cycle is required for apps built with this SDK.</c>
/// iOS 26 只是警告，所以真机（26.7）一直没暴露；装到 iOS 27 的模拟器/设备上必挂。
/// </para>
///
/// <para>
/// 采用 = **两步，且第二处必须逐字对上**：
/// ① 本类继承 <see cref="Microsoft.Maui.MauiUISceneDelegate"/>（窗口怎么建、根控制器怎么挂、
///    生命周期怎么桥回 MAUI，全在那个基类里，所以这里是空实现）；
/// ② <c>Platforms/iOS/Info.plist</c> 的 <c>UIApplicationSceneManifest</c> 里
///    <c>UISceneDelegateClassName</c> 写本类的 **ObjC 注册名**（<c>[Register]</c> 那个字符串），
///    并且 <c>UISceneConfigurationName</c> **必须逐字是
///    <c>__MAUI_DEFAULT_SCENE_CONFIGURATION__</c>**。
/// </para>
///
/// <para>
/// ⚠⚠ **第二处那个配置名是这次真正的坑**（反编译 MAUI 10.0.20 才看清）：
/// <c>MauiUISceneDelegate.WillConnect</c> 第一句就是
/// <code>
/// if (session.Configuration.Name != "__MAUI_DEFAULT_SCENE_CONFIGURATION__" || ...) return;
/// CreatePlatformWindow(...);      // ← 只有名字对上才会建窗口
/// </code>
/// 名字不对**不报错、不崩**，只是 MAUI 直接 return ⇒ **App 起得来、也停在前台，但一个窗口都没有**
/// （日志里只有 <c>Keyboard screen not found for window (null)</c>、屏幕上全黑）。
/// 网上那类 `Default Configuration` 的写法（给别家的模板用的）在这里正好踩中它 ——
/// 我照它写了一版，先得到"不黑屏断言"，再得到"黑屏"，两轮才对上。
/// </para>
/// </summary>
[Register("SceneDelegate")]
public class SceneDelegate : Microsoft.Maui.MauiUISceneDelegate
{
}
