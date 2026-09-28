using System.Globalization;

namespace WayCoder.Maui.Services;

/// <summary>
/// MAUI 侧的语言探测 —— 取**系统语言偏好列表**交给 <see cref="L"/> 判定。
///
/// <para>
/// <b>为什么不用 <c>CultureInfo.CurrentUICulture</c> 了事</b>：它只是兜底。
/// iOS/Android 都提供了更直接、更可信的"用户把系统设成什么语言"的答案，
/// 而那正是产品要跟随的东西。两者的差别在真机上才看得出来（例如 iOS 上
/// 用户把 App 语言单独设过时，两者会不一致）。
/// </para>
///
/// <para>
/// ⚠ <b>取的是"设备偏好列表"而不是"App 匹配结果"</b>：iOS 上必须用
/// <c>NSLocale.PreferredLanguages</c>，**不要**用 <c>Bundle.preferredLocalizations</c> ——
/// 后者是"系统从 App 声明支持的语言里挑一个"，当 App 只声明了 <c>en</c>+<c>zh-Hans</c> 时
/// 它退化成这两者之一，回答不了"用户系统语言是什么"。
/// </para>
///
/// <para>
/// ⚠ <b>失败一律静默兜底</b>：探测拿不到就维持 <see cref="L"/> 的默认（中文），
/// 绝不抛异常 —— 它在 App 启动最早期执行，抛出去等于开屏即崩。
/// </para>
/// </summary>
public static class MauiLang
{
    private static bool _done;

    /// <summary>
    /// 按系统语言初始化 <see cref="L"/>。**幂等**（可多处调用）。
    /// 调用点：<c>MauiProgram.CreateMauiApp()</c> 首行 + <c>MauiBootstrap.Initialize()</c> 首行
    /// —— 后者是既有的"必须最先、且在任何 Config/Agent 访问前执行"契约点，
    /// 冗余一次换防御（首次那处若因框架时序没跑到，这里兜住）。
    /// </summary>
    public static void Initialize()
    {
        if (_done) return;
        _done = true;
        try
        {
            L.ApplyFromTags(CollectTags());
        }
        catch (Exception ex)
        {
            ErrorLog.Error("MauiLang", "语言探测失败，维持默认（中文）", ex);
        }

        // ⚠ **把语言推给 VML 编译器** —— 它的诊断文案（`未声明的变量 'x'` / 未使用变量警告…）
        //   也是**用户可见的**（手机上编译失败时那些字就打在气泡里），得跟着一起切。
        //   而那套文案在另一个工程（`third_party/vml/VMLPrepares`，命名空间 `CompilerBase`）——
        //   `WayCoder` 主工程**不引用**它（一个 ProjectReference 都没有），所以**没法**在主工程
        //   的 `L.Set` 里推；**本文件才是那个引用它的宿主**，注入放这儿。
        //   放在 try **之后**（不是里面）：探测失败也要把默认值推过去，否则编译器那边可能
        //   停在别的状态。`_done` 保证只跑一次。
        CompilerBase.VmlLang.Set(L.IsZh);
    }

    /// <summary>按"越靠前越可信"收集语言标签（有序 —— <see cref="L.FromLanguageTags"/> 依赖顺序语义）。</summary>
    private static List<string?> CollectTags()
    {
        var tags = new List<string?>();

#if IOS || MACCATALYST
        // 用户在「设置 → 通用 → 语言与地区」里的语言优先级列表，如 ["zh-Hans-CN", "en-US"]
        try
        {
            foreach (var t in Foundation.NSLocale.PreferredLanguages) tags.Add(t);
        }
        catch (Exception ex) { ErrorLog.Error("MauiLang", "读取 NSLocale.PreferredLanguages 失败", ex); }
#elif ANDROID
        try
        {
            // ⚠ `Configuration.Locales` 是 **API 24+**，而本工程 minSdk = 21 ⇒ 必须版本守卫
            if (OperatingSystem.IsAndroidVersionAtLeast(24))
            {
                var locales = Android.App.Application.Context?.Resources?.Configuration?.Locales;
                if (locales != null)
                {
                    for (int i = 0; i < locales.Size(); i++)
                        tags.Add(locales.Get(i)?.ToLanguageTag());
                }
            }
            tags.Add(Java.Util.Locale.Default?.ToLanguageTag());
        }
        catch (Exception ex) { ErrorLog.Error("MauiLang", "读取 Android Locales 失败", ex); }
#elif WINDOWS
        try
        {
            foreach (var t in Windows.Globalization.ApplicationLanguages.Languages) tags.Add(t);
        }
        catch (Exception ex) { ErrorLog.Error("MauiLang", "读取 ApplicationLanguages 失败", ex); }
#endif

        // 全平台兜底（上面的平台 API 全失败时至少还有它）
        tags.Add(CultureInfo.CurrentUICulture.Name);
        tags.Add(CultureInfo.CurrentCulture.Name);
        return tags;
    }
}
