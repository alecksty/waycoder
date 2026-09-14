using Microsoft.Maui.Graphics;

namespace WayCoder.Maui.Controls;

/// <summary>
/// 编辑器排版常量的**单一真源** —— 自绘画布与浮动的单行输入框必须取同一份值，
/// 否则两者在切换的瞬间会跳一下（字号差 0.5、内边距差 2px 都看得出来）。
///
/// **字体只用一个等宽族**（内嵌的 Sarasa Mono SC），按平台取**不同的名字** —— 见
/// <see cref="CanvasFontName"/>，那三个名字互不相同、写错只静默回落，不报错。
///
/// 之所以不用系统 <c>monospace</c>：它没有中文字形，中文靠平台 fallback，而**测量与渲染
/// 两条 fallback 到的字体并不一致**（实测同一条中文，测量 ≈9.8dp、渲染 ≈16.8dp），
/// 点击定位就会越往右越偏。
///
/// ⚠ 列宽**不实测**，直接用字体设计值（见 <see cref="HalfWidth"/>）—— 这里曾写着「列宽一律实测」，
/// 那是被 v0.96.117 推翻的旧结论：平台把行宽**取整**（13pt 时拉丁真值 6.5 报成 7），
/// 照实测值定位会让每个拉丁字符多算 0.5pt。Sarasa 的拉丁恰好 0.5em、汉字恰好 1em，
/// 设计值与「汉字 = 2 列」的网格天然对齐，量出来反而是个近似值。
/// </summary>
internal static class EditorTypography
{
    /// <summary>
    /// **Controls 侧**（Entry / Label）用的字体别名 —— 即 MauiProgram 里 <c>AddFont</c> 注册的那个。
    ///
    /// 编辑器自带 Sarasa Mono SC：它中英文严格等宽，且**中文恰好占 1em = 拉丁的 2 倍**，
    /// 与终端「中文算 2 列」的语义天然对齐。
    /// </summary>
    public const string FontFamilyName = "SarasaMonoSC";

    /// <summary>
    /// **Graphics 侧**（<c>Font</c> / <c>Typeface</c>）用的字体名 —— 与 Controls 是**两套互不知情的解析器**。
    ///
    /// Android 走资产名：<c>FontExtensions.ToTypeface</c> 会先试 <c>Typeface.CreateFromAsset</c>，
    /// 所以**这里必须带扩展名**（资产就叫 <c>SarasaMonoSC-Regular.ttf</c>）；iOS 走
    /// <c>UIFont.FromName</c> / <c>CTFontCreateWithName</c> / <c>CGFontCreateWithFontName</c>，
    /// 三者**都要 PostScript 名**（<c>name</c> 表 nameID 6）。写错都不抛异常，只会静默回落成
    /// 平台默认的**比例字体**，宽度与测量对不上。
    ///
    /// ⚠ **这个字体的三个名字互不相同，别互相顶替**（都是从 TTF 的 <c>name</c> 表读出来的）：
    /// <list type="bullet">
    /// <item>家族名（nameID 1/16）= <c>Sarasa Mono SC</c> —— 带空格</item>
    /// <item>PostScript 名（nameID 6）= <c>Sarasa-Mono-SC-Regular</c> —— **带连字符，iOS 要的是这个**</item>
    /// <item>Android 资产文件名 = <c>SarasaMonoSC-Regular.ttf</c> —— 不带连字符、带扩展名</item>
    /// </list>
    /// iOS 分支曾写成 <c>SarasaMonoSC-Regular</c>（看着像，其实三个都不是）⇒ <c>UIFont.FromName</c>
    /// 返回 null、静默回落成比例字体，而定位走的是 2 列网格 ⇒ **渲染比例、定位网格，光标对不上位置**。
    ///
    /// ⚠ 这个名字**只对「画布自己的字体」有效**。`AttributedText` 的 run 上若写了 FontName，
    /// MAUI 会把它变成 <c>TypefaceSpan(族名)</c> —— 那个 API **只认系统字体族名、没有 asset 重载**
    /// （见 `dotnet/maui` 的 `Graphics/Platforms/Android/Text/AttributedTextExtensions.cs`），
    /// 资产名喂进去只会悄悄回落。所以 run 上**不写** FontName，让布局回落用画布的字体 —— 见
    /// <c>CodeCanvasView.BuildLineRuns</c>。
    /// </summary>
    public const string CanvasFontName =
#if ANDROID
        "SarasaMonoSC-Regular.ttf";      // 资产文件名（CreateFromAsset 按这个名字找）
#elif IOS
        "Sarasa-Mono-SC-Regular";        // PostScript 名（UIFont.FromName 按这个名字找）
#else
        "Sarasa-Mono-SC-Regular";
#endif

    /// <summary>
    /// 半角字符宽度（列宽）—— Sarasa Mono 的拉丁字形推进量**恰好是 0.5em**，
    /// 汉字恰好 1em，所以「汉字 = 2 列」这台网格与字体设计天然对齐，不需要额外推算。
    /// </summary>
    public static float HalfWidth => FontSize * 0.5f;

    private static float _fontSize = DefaultFontSize;

    /// <summary>默认字号（偶数 —— 见 <see cref="FontSize"/> 的说明）。</summary>
    public const float DefaultFontSize = 14f;

    /// <summary>
    /// 字号（磅）。可在编辑器菜单里调（加大/缩小/重置），并持久化。**连续可取**（捏合给小数也行）。
    ///
    /// 曾经这里只允许偶数 —— 那是因为定位走「列号 × 半列宽」的网格，而 **Android 把每个字形的
    /// 推进量取整**：偶数号下半列宽是整数、两边逐字相等（实测偏差 0.00px），奇数号下每个半角字形
    /// 差 0.5（一行 107 列累计 53.5px）。后来定位改成**逐字形累加平台实测推进量**
    /// （<c>CodeCanvasView.MeasureAdvances</c>），与渲染同源 ⇒ 任何字号都对得上，这个限制就撤了。
    ///
    /// ⚠ 撤掉限制的前提是那条改动还在：**别把定位改回「列号 × 设计半列宽」**，
    /// 否则奇数号/小数号的偏差会立刻回来。
    /// </summary>
    public static float FontSize
    {
        get => _fontSize;
        set => _fontSize = ClampFontSize(value);
    }

    /// <summary>字号步进 —— 菜单「加大 / 缩小」按这个走。</summary>
    public const float FontStep = 1f;

    /// <summary>
    /// 把任意字号夹到合法区间。**不再吸附到整数或偶数** —— 见 <see cref="FontSize"/> 的长注释。
    ///
    /// 单独立成纯函数，是为了让「夹取」这条规则**只有一处实现**：<see cref="FontSize"/> 的 setter
    /// 与编辑器页面（它得**先夹取、再判断「变了没有」**）都调它。两边各写一遍就会出现
    /// 「按钮按了没反应」或「输入框字号与画布不一致」这类半生效的怪状。
    /// </summary>
    public static float ClampFontSize(float size)
        => Math.Clamp(size, MinFontSize, MaxFontSize);

    /// <summary>
    /// 字号可调范围。**捏合缩放用这一对**（`EditorPage.ApplyFontSize` 夹取）。
    /// 8 是下限（用户实测 6 号已经看不出单词形状了，只能看到一片灰），
    /// 96 是「一屏只剩一两行」的放大上限 —— 手机上看代码时两头都用得上
    /// （整屏鸟瞰 / 逐字看清），中间由捏合连续过渡。
    ///
    /// ⚠ 两端都必须是**偶数**（见 <see cref="FontSize"/>）。
    /// </summary>
    public const float MinFontSize = 8f;
    public const float MaxFontSize = 96f;

    /// <summary>
    /// 行高（磅）—— **固定值，不用平台行高**：自绘的行位置必须能被「第 N 行 → y 坐标」
    /// 精确算出来（否则点击定位、光标跟随、波浪线全都对不上）。
    /// 由字号派生，所以调字号时它会自动跟着变（取整避免累积出半像素的错位）。
    /// </summary>
    public static float LineHeight => MathF.Round(FontSize * 1.40f);

    /// <summary>行号栏与文字之间的留白。</summary>
    public const float GutterRightPad = 8f;

    /// <summary>正文左内边距（行号栏之后）。</summary>
    public const float TextLeftPad = 4f;

    /// <summary>上下内边距。</summary>
    public const float VerticalPad = 2f;

    /// <summary>
    /// 「行顶 → 文本落笔点」的补偿量，**实测为 0**：<c>ICanvas.DrawText</c> 的 y 就是行顶。
    ///
    /// 这里曾经放过 12（当时从「第 1 行看不见」反推出 y 落在基线上），结果**所有文字被整体
    /// 下推半行**，高亮条却停在行顶，用户实测「光标行背景卡在两行中间」——反推错了。
    /// 留着这个常量是为了将来真在某个平台上遇到偏移时**只改这一个数**，别再散到绘图循环里。
    /// </summary>
    public const float TextBaselineOffset = 0f;

    /// <summary>制表符宽度（列）。</summary>
    public const int TabColumns = 4;

    /// <summary>超过这个字符数的行**跳过分词**：一条 minified 行上跑 tokenizer 会把一帧拖到几百毫秒。</summary>
    public const int MaxTokenizeChars = 4096;

    /// <summary>Graphics 侧的字体对象（Controls 侧用 <see cref="FontFamilyName"/> 字符串即可）。</summary>
    public static readonly Microsoft.Maui.Graphics.Font CanvasFont = new(CanvasFontName);

    /// <summary>行号栏前景色。</summary>
    public static readonly Color GutterFg = Color.FromArgb("#8A8A8E");
    public static readonly Color GutterBg = Color.FromArgb("#F2F2F4");
    public static readonly Color GutterBgDark = Color.FromArgb("#1C1C1E");
    public static readonly Color EditorBg = Color.FromArgb("#FFFFFF");
    public static readonly Color EditorBgDark = Color.FromArgb("#121214");
    // ⚠ MAUI 的 Color.FromArgb 在 8 位十六进制下按 **#AARRGGBB** 解析（alpha 在前）。
    // 写成 #RRGGBBAA 的话，「白色 6% 透明」会被当成 alpha=FF 的**不透明黄色**（FFFF10），
    // 当前行就会顶出一条刺眼的黄条；而「黑色 6%」会变成完全透明（alpha=00）等于没画。
    public static readonly Color CaretLineBg = Color.FromArgb("#05000000");        // 黑 2%
    public static readonly Color CaretLineBgDark = Color.FromArgb("#0AFFFFFF");    // 白 4%
    // 当前行高亮是**长期停在屏幕上**的东西，压得越低越不累眼睛：
    // 它的作用是「让你知道光标在哪」，不是「吸引注意力」。实测 9% 白已经嫌刺眼。
    public static readonly Color SelectionBg = Color.FromArgb("#403B82F6");        // 蓝 25%
    /// <summary>滚动条：静止时淡、按住/拖动时浓（就是「点一下变大变明显」的那半）。</summary>
    public static readonly Color BarIdle = Color.FromArgb("#33000000");
    public static readonly Color BarIdleDark = Color.FromArgb("#33FFFFFF");
    public static readonly Color BarActive = Color.FromArgb("#99000000");
    public static readonly Color BarActiveDark = Color.FromArgb("#99FFFFFF");

    /// <summary>滚动条几何 —— 绘制与命中测试<b>共用这一份</b>，否则「看到的滑块」和「点得中的滑块」会错位。</summary>
    // 宽度是用户的直接反馈调上来的：2.5pt 在手机上细到几乎看不见，也就无从瞄准。
    // 5/10 是「一眼能看见、又不压住正文」的折中（正文有 24pt 的横向余量，见 ComputeMaxScrollX）。
    public const float BarThin = 5f;          // 常态
    public const float BarThick = 10f;        // 按住/拖动：明显变粗，给出「抓住了」的反馈
    /// <summary>
    /// 距画布边缘的留白。**不能太小**：画布的 Height 一直算到页面内容区的底边，
    /// 而底部紧挨着的就是状态栏那一行 —— 留白 3pt 时滚动条正好被状态栏压在底下，
    /// 表现为「加了滚动条却看不见、也点不中」。
    /// </summary>
    public const float BarMargin = 16f;
    public const float BarMinThumb = 40f;     // 滑块最短长度（百万行文件里否则细到捏不住）
    public const float BarTouchSlop = 20f;    // 触摸热区比视觉再宽一圈，手指不必压在条上也能拖

    public static readonly Color ErrorWave = Color.FromArgb("#E5484D");
    public static readonly Color WarnWave = Color.FromArgb("#F5A524");
    public static readonly Color InfoWave = Color.FromArgb("#3B82F6");
}
