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
        private static CultureInfo? _culture;

        public static string CurrentLang => (_culture ??= ResolveCulture()).Name switch
        {
            "zh-CN" => "zh-CN", "zh-TW" => "zh-TW", "zh-HK" => "zh-TW",
            "fr" or "fr-FR" => "fr", "es" or "es-ES" => "es",
            "ru" or "ru-RU" => "ru", "ar" or "ar-SA" => "ar",
            _ => "en"
        };

        public static string Get(string key, params object?[] args)
        {
            _culture ??= ResolveCulture();
            var val = _rm.GetString(key, _culture);
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
