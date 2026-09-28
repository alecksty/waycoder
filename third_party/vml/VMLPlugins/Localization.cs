using System;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;

namespace VMLPlugins
{
    /// <summary>
    /// 本地化管理器 — 使用 .NET 原生 .resx 嵌入式资源。
    /// 优先当前语言卫星程序集，回退 zh-CN，再回退 neutral(en)。
    /// 零文件 I/O，单文件发布完美兼容。
    /// </summary>
    public static class Localization
    {
        private static readonly ResourceManager _rm = new("VMLPlugins.Resources.Locale", typeof(Localization).Assembly);

        // ⚠ **不缓存** —— 原先这里是 `(_culture ??= ResolveCulture())`，那是
        //   「首次访问把语言冻死」的经典形态（本仓对 `static readonly` 有同一条禁令）：
        //   注入晚于首次读取时，之后永远返回旧语言。`CultureInfo.GetCultureInfo` 本身
        //   在 .NET 里就是缓存过的，这里省不下什么。
        public static string CurrentLang => ResolveCulture().Name switch
        {
            "zh-CN" => "zh-CN", "zh-TW" => "zh-TW", "zh-HK" => "zh-TW",
            "fr" or "fr-FR" => "fr", "es" or "es-ES" => "es",
            "ru" or "ru-RU" => "ru", "ar" or "ar-SA" => "ar",
            _ => "en"
        };

        public static string Get(string key, params object?[] args)
        {
            var culture = ResolveCulture();
            var val = _rm.GetString(key, culture);
            if (val == null)
            {
                // 回退到 neutral (en)
                val = _rm.GetString(key, CultureInfo.InvariantCulture);
            }
            if (val != null)
                return args.Length > 0 ? string.Format(val, args) : val;
            return $"[{key}]";
        }

        private static CultureInfo ResolveCulture()
        {
            // ⚠⚠ **界面语言的唯一真源是 `VMLAssembler.VmlLang`**（宿主从 App 的 `L.IsZh` 注入），
            //   这里原先只看 `VML_LANG` 与 `CultureInfo.CurrentCulture` —— 那是**第二套语言源**，
            //   与本类可以同时成立却给出不同答案。**实测症状**：`LANG=en_US.UTF-8` 下
            //   `VmlLang` 认英文、而 `CultureInfo.CurrentCulture` 在 macOS 上**不跟 `LANG`**
            //   ⇒ 同一条错误消息**中英混排**：`Expected identifier in 词名(word name)`
            //   （`.resx` 的英文模板 + 前端传进来的中文实参）。
            //   先问真源；只有**从没被注入过**（第三方把本库当库用）才回退下面那两条老路，
            //   这样"没人注入"时的行为与改造前逐字节相同。
            if (VMLAssembler.VmlLang.WasInjected)
                return CultureInfo.GetCultureInfo(VMLAssembler.VmlLang.IsZh ? "zh-CN" : "en");

            var env = Environment.GetEnvironmentVariable("VML_LANG");
            if (!string.IsNullOrEmpty(env))
            {
                try { return CultureInfo.GetCultureInfo(env); }
                catch { }
            }
            return CultureInfo.CurrentCulture;
        }
    }
}
