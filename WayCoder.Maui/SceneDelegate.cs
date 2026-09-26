#if IOS || MACCATALYST
using Foundation;

namespace WayCoder.Maui;

/// <summary>
/// **UIScene 生命周期**委托（iOS 与 Mac Catalyst 共用一份）—— 空的，它的存在只为"采用 scene 生命周期"。
///
/// <para>
/// **为什么必须有**（2026-09-27 修）：**用 SDK 27 编译的包在 iOS 27 / macOS 27 上直接起不来** ——
/// UIKit 断言并当场 <c>brk</c>（退出码 133 / SIGTRAP）：
/// <code>
/// Application failed to launch: UIScene life cycle is required for apps built with this SDK.
/// </code>
/// iOS 26 只是警告，所以真机（26.7）一直没暴露；装上 iOS 27 的设备/模拟器必挂。
/// </para>
///
/// <para>
/// ⚠ **两个目标各有各的 Info.plist** —— 这一条是 Mac Catalyst 那次踩的：
/// 上一轮只改了 <c>Platforms/iOS/Info.plist</c>，于是 iOS 好了、**macOS 版照样起不来**
/// （同一条断言、同一个崩溃码），而"iOS 修好了"这个结论看起来完全成立。
/// 现在两份 plist **都要**写 <c>UIApplicationSceneManifest</c>，改一处就得改另一处。
/// </para>
///
/// <para>
/// 采用 = **两步，且第二处必须逐字对上**：
/// ① 本类继承 <see cref="Microsoft.Maui.MauiUISceneDelegate"/>（窗口怎么建、根控制器怎么挂、
///    生命周期怎么桥回 MAUI，全在那个基类里，所以这里是空实现）；
/// ② 两个 Info.plist 的 <c>UIApplicationSceneManifest</c> 里
///    <c>UISceneDelegateClassName</c> 写本类的 **ObjC 注册名**（<c>[Register]</c> 那个字符串），
///    并且 <c>UISceneConfigurationName</c> **必须逐字是
///    <c>__MAUI_DEFAULT_SCENE_CONFIGURATION__</c>**。
/// </para>
///
/// <para>
/// ⚠⚠ **第二处那个配置名是真正的坑**（反编译 MAUI 10.0.20 才看清）：
/// <c>MauiUISceneDelegate.WillConnect</c> 第一句就是
/// <code>
/// if (session.Configuration.Name != "__MAUI_DEFAULT_SCENE_CONFIGURATION__" || ...) return;
/// CreatePlatformWindow(...);      // ← 只有名字对上才会建窗口
/// </code>
/// 名字不对**不报错、不崩**，只是 MAUI 直接 return ⇒ **App 起得来、也停在前台，但一个窗口都没有**
/// （日志里只有 <c>Keyboard screen not found for window (null)</c>、屏幕上全黑）。
/// 网上那类 `Default Configuration` 的写法（给别家的模板用的）在这里正好踩中它 ——
/// 照它写了一版，先得到"不黑屏断言"，再得到"黑屏"，两轮才对上。
/// </para>
///
/// <para>
/// ⚠ **为什么放在项目根、用 <c>#if</c> 守起来而不是各放一份**：
/// 这个类是空实现、两端的写法逐字相同，放两份就是本仓反复吃亏的"平行表"——
/// 将来谁改了一处（比如 <c>[Register]</c> 的名字）另一处不会跟着变，而症状是**另一个平台黑屏**，
/// 看代码完全看不出来。放在根上时非 Apple 目标（Android/Windows）该文件**整体被 #if 排掉**，
/// 不会引用到不存在的 <c>MauiUISceneDelegate</c>（宁可"编不过"，不要"编得过但另一个平台黑屏"）。
/// </para>
/// </summary>
[Register("SceneDelegate")]
public class SceneDelegate : Microsoft.Maui.MauiUISceneDelegate
{
}
#endif
