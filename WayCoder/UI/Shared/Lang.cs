using System.Globalization;

namespace WayCoder;

/// <summary>界面语言。只有两档：中文 / 英文（英文同时是「其它一切语言」的兜底）。</summary>
public enum UiLang { Zh, En }

/// <summary>
/// <b>界面语言的唯一真源</b> —— 全仓所有用户可见文案都经 <see cref="L.Pick"/> 取。
///
/// <para>
/// <b>为什么放 <c>UI/Shared</c> 而不是仓库根</b>（<c>UiText.cs</c> 的位置）：
/// <c>WayCoder.Preview.csproj</c> 显式只编 <c>../WayCoder/UI/Shared/**</c> 与几个 TUI 子目录，
/// **不含仓库根** —— 放根上会让 Preview 一引用就 CS0103。放这里则桌面 / GUI / MAUI / Preview
/// 四个工程全中（桌面与 GUI 走默认 glob、MAUI 走它那条大 glob），**零 csproj 改动**。
/// </para>
///
/// <para>
/// <b>为什么命名空间是扁平的 <c>WayCoder</c> 而不是 <c>WayCoder.UI.Shared</c></b>：
/// <c>L.Pick</c> 会出现在 <c>Tools/</c>（55 个文件）、<c>Agent/</c>、<c>Infra/</c>、<c>Config/</c> 里，
/// 用带层级的命名空间就得往几十个文件加 <c>using</c>。本仓已有同款先例（<c>UiText.cs</c>）。
/// </para>
///
/// <para>
/// ⚠⚠ <b>硬规则：引用 <see cref="Pick"/> 的成员只能写成表达式体属性</b>
/// （<c>public static string X =&gt; L.Pick(...)</c>），<b>禁止 <c>static readonly</c> 字段</b>。
/// 后者会在**首次访问时**把语言冻死 —— 在 MAUI 里若首次访问早于平台语言探测，就永久为中文
/// （英文手机上显示中文，且只在某些启动顺序下复现）。自测里有一条源码扫描护栏钉住它。
/// </para>
/// </summary>
public static class L
{
    // ⚠ 默认 zh（不是"跟随系统"）：这样"没人初始化"的场合（自测、库调用、Preview）
    //   行为与改造前**逐字节相同**。跟随系统由各入口**显式**调用 DetectFromSystem() /
    //   MauiLang.Initialize() 触发 —— 默认值安全 + 入口显式，两头都不靠约定。
    private static volatile bool _isZh = true;

    /// <summary>当前是否中文。**唯一的读取点**，别在别处自己判语言。</summary>
    public static bool IsZh => _isZh;

    public static UiLang Current => _isZh ? UiLang.Zh : UiLang.En;

    /// <summary>
    /// 随包**按语言分开的资源目录名**（目前只有帮助文档用它）：<c>zh</c> / <c>en</c>。
    ///
    /// <para>
    /// 帮助文档是**整篇的长文**，不是一句话 —— 用 <see cref="Pick"/> 把中英塞进同一个字符串
    /// 既不现实也不可读，所以那一层按语言分目录（<c>Resources/Raw/help/zh/**</c> 与 <c>.../en/**</c>）。
    /// ⚠ **缺英文时必须显式告警、绝不回退中文** —— 回退会让"漏翻"永远看不见，
    /// 而用户看到的是"这个 App 一半英文一半中文"。这是 `.resx` 那次栽跟头的同一个教训。
    /// </para>
    /// </summary>
    public static string ResourceLangDir => _isZh ? "zh" : "en";

    /// <summary>设定语言（各端入口初始化 / 自测钉住用）。</summary>
    /// <remarks>
    /// ⚠ **这里不通知 VML 编译器** —— 看似该在这儿一处注入，但**本工程根本不引用 VMLPrepares**
    /// （`WayCoder.csproj` 一个 `ProjectReference` 都没有；它跟 VML 只是**数据级**关系 ——
    /// 列前端名字，见 `VmlFrontendCompilerList.cs`）。写了就是 CS0103。
    ///
    /// <para>
    /// 语言要传到编译器，得由**真正调编译器的那两个宿主**各自注入
    /// （手机端 `WayCoder.Maui/Services/MauiVml.cs`、桌面 `scripts/vmlcli`）——
    /// 它们才引用 `VMLAssembler`。见 <c>VMLAssembler.VmlLang</c> 的注释。
    /// </para>
    /// </remarks>
    public static void Set(UiLang lang) => _isZh = lang == UiLang.Zh;

    /// <summary>
    /// <b>取文案的唯一入口</b>。两个实参就是"键" —— 没有键表、没有查找，
    /// 所以「键没跟上」这个失败模式**在语法上不存在**（少一个实参编不过）。
    /// 这是对 .resx 那套"运行时查找、缺失静默回退成 <c>[键名]</c> 显示给用户"的直接回答。
    /// </summary>
    public static string Pick(string zh, string en) => _isZh ? zh : en;

    /// <summary>语言探测现场（写日志 / About 页显示），便于真机上确认探测是否合意。</summary>
    public static string DetectTrace { get; private set; } = "default(zh)";

    /// <summary>
    /// 一个语言标签是不是中文。<b>只认主语言子标签</b>：
    /// <c>zh</c> / <c>zh-Hans</c> / <c>zh-Hant</c> / <c>zh-CN</c> / <c>zh-Hans-CN</c> /
    /// <c>ZH-hant</c> / <c>zh_CN.UTF-8</c>（Unix 的 LANG 形态）**全算中文**；
    /// <c>en-US</c> / <c>de-DE</c> / 空串 **不算**。
    /// </summary>
    public static bool IsChineseTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return false;
        var t = tag.Trim();
        if (t.Length < 2) return false;
        if (char.ToLowerInvariant(t[0]) != 'z' || char.ToLowerInvariant(t[1]) != 'h') return false;
        // 必须**整段**就是主语言标签：`zh` / `zh-…` / `zh_…`（避免把 `zhx` 之类误判成中文）
        return t.Length == 2 || t[2] is '-' or '_';
    }

    /// <summary>
    /// 从**有序**语言偏好列表判定语言（纯函数，可自测）。
    ///
    /// <para>
    /// ⚠ 判据是「**整份列表里有没有中文**」，而不是"只看第一个"。
    /// 理由是 <c>地区=中国 + 语言=English</c> 是开发者/外企用户的常见组合：
    /// 只看首位会让这批**一直在用中文界面的老用户**升级后突然变英文。
    /// </para>
    ///
    /// <para>完全拿不到有效标签（空列表 / 全空白 / null）⇒ **维持默认中文**，不猜。</para>
    /// </summary>
    public static UiLang FromLanguageTags(IEnumerable<string?>? tags)
    {
        if (tags == null) return UiLang.Zh;
        bool any = false;
        foreach (var t in tags)
        {
            if (string.IsNullOrWhiteSpace(t)) continue;
            any = true;
            if (IsChineseTag(t)) return UiLang.Zh;
        }
        return any ? UiLang.En : UiLang.Zh;
    }

    /// <summary>
    /// 按一组语言标签应用语言，并记录**探测现场**（<see cref="DetectTrace"/>，写日志 / About 页显示）。
    /// 桌面与 MAUI 共用它 —— 判定规则与现场格式只有这一处，免得两端各写一遍。
    /// </summary>
    public static void ApplyFromTags(IEnumerable<string?>? tags, string? trace = null)
    {
        var list = tags?.ToList() ?? [];
        Set(FromLanguageTags(list));
        DetectTrace = trace ?? string.Join(",", list.Where(t => !string.IsNullOrWhiteSpace(t)));
        if (string.IsNullOrWhiteSpace(DetectTrace))
            DetectTrace = L.Pick("(无语言信息 ⇒ 默认中文)", "(no language info -> defaulting to Chinese)");
    }

    /// <summary>
    /// 按**操作系统语言**初始化（桌面 / CLI / GUI / Web 这几端的入口调它）。
    /// MAUI 走 <c>MauiLang.Initialize()</c>（它另有更可信的平台 API）。
    ///
    /// <para>
    /// 顺序有讲究：先看 <c>LC_ALL</c>/<c>LC_MESSAGES</c>/<c>LANG</c>，再看 .NET 的
    /// <c>CurrentUICulture</c>。headless Linux / 容器里 <c>CurrentUICulture</c> 常常是
    /// invariant（名字为空串），只有环境变量靠得住。
    /// </para>
    /// </summary>
    public static void DetectFromSystem()
    {
        var tags = new List<string?>
        {
            Environment.GetEnvironmentVariable("LC_ALL"),
            Environment.GetEnvironmentVariable("LC_MESSAGES"),
            Environment.GetEnvironmentVariable("LANG"),
            CultureInfo.CurrentUICulture.Name,
            CultureInfo.CurrentCulture.Name,   // 兜底：UICulture 为 invariant 时看区域
        };
        ApplyFromTags(tags);
    }
}
