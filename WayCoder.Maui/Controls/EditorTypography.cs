using Microsoft.Maui.Graphics;
using WayCoder.UI.Tui.Edit;   // Severity（波浪色/气泡底色的那三档）

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
        // Windows（WinUI 3）：按**族名**解析（`CanvasTextFormat.FontFamily` 直接交给 DirectWrite
        // 查**系统字体集合**），不用资产文件名、也不用 PostScript 名 —— 那两条是 Android / iOS 专有的解析器。
        //
        // 【2026-09-15 Windows 实测】随包的 Sarasa **在 Windows 上取不到**：它是资产、没装进系统，
        // DirectWrite 找不到就静默换字体。所以这里用系统自带的 2:1 等宽字体（SimSun 系）：
        // 实测 NSimSun/SimSun/MS Gothic/SimHei/KaiTi/FangSong 都是**精确的 半角=0.5em、全角=1em**
        // （用渲染同引擎量：半角 7.00 / 全角 14.00 @ 字号 14）。
        // ⚠ 别换回落底的族名（"Sarasa Mono SC"/Consolas/Cascadia 实测是 1.86/1.82/1.71，不是 2:1）
        //   ——「汉字 = 2 列」的网格要求**恰好的 2:1**，差一点就沿行累积成错位。
        // 想把打包的 Sarasa 真正用起来，得走 Win2D 的 `CanvasFontSet`（`W2DCanvas.Session` 是 public
        // 的，可行），那是另一件事；在此之前系统 2:1 等宽字体是正确且够用的。
        "NSimSun";
#endif

    /// <summary>
    /// 半角字符宽度（列宽）—— Sarasa Mono 的拉丁字形推进量**恰好是 0.5em**，
    /// 汉字恰好 1em，所以「汉字 = 2 列」这台网格与字体设计天然对齐，不需要额外推算。
    /// </summary>
    public static float HalfWidth => FontSize * 0.5f;

    /// <summary>全角字符宽度（汉字/全角标点）—— Sarasa Mono 的汉字推进量恰好 1em。</summary>
    public static float FullWidth => FontSize;

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
    /// <remarks>
    /// ⚠ 行号与正文之间的**可见间距 = 本值 + <see cref="TextLeftPad"/>**（两者都由
    /// <c>CodeCanvasView.GutterWidth()</c> 累加），所以要调窄间距必须两个一起看，
    /// 只改一个只能得到一小截效果。竖屏手机上 8+4=12px 太宽（用户实测「留白太宽」），
    /// 现按「减一半」收到 4+2=6px。
    /// </remarks>
    public const float GutterRightPad = 4f;

    /// <summary>正文左内边距（行号栏之后）。</summary>
    public const float TextLeftPad = 2f;

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

    /// <summary>
    /// 配对括号的底色（#AARRGGBB，alpha 在前）。
    ///
    /// 它要压在**当前行底色**（黑 2% / 白 4%）之上，又**不能盖过选区**（蓝 25%），
    /// 所以取琥珀 30%：与选中蓝、错误红、警告黄都分得开，且足够淡 —— 它是个"提示你
    /// 括号在哪儿"的记号，不是让你盯着看的东西。深色底上用更亮的一档，
    /// 否则琥珀在深色代码背景上会糊成一片棕。
    /// </summary>
    public static readonly Color BracketMatchBg = Color.FromArgb("#4DD19A38");
    public static readonly Color BracketMatchBgDark = Color.FromArgb("#5FE8B84B");
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
    /// <summary>
    /// 滚动条距画布边缘的留白。
    /// ⚠ 别调大：它同时是**可见间距**（条子外侧到屏幕边还剩多少空）。
    /// 原为 16 —— 用户实测「离边太远、白占一条」，收到 3（贴边但不压边框）。
    /// 拖动时条子变粗（<see cref="BarThick"/>），仍在这个留白之内，不会顶出画布。
    /// </summary>
    public const float BarMargin = 3f;

    /// <summary>
    /// 滚动条**自动淡出**的秒数：最后一次触摸之后超过这么久就隐藏，再摸屏幕又出现。
    /// 太短会一直闪、太长等于常驻；5 秒是「读完一屏再滑」的典型间隔。
    /// </summary>
    public const double BarAutoHideSeconds = 5.0;
    public const float BarMinThumb = 40f;     // 滑块最短长度（百万行文件里否则细到捏不住）
    public const float BarTouchSlop = 20f;    // 触摸热区比视觉再宽一圈，手指不必压在条上也能拖

    /// <summary>选区手柄的半径与触摸热区（热区远大于视觉半径 —— 手指点不中一个 6pt 的圆点）。</summary>
    public const float HandleRadius = 6.5f;
    public const float HandleTouchRadius = 22f;
    /// <summary>手柄配色：外圈白环 + 实心，压在深色/浅色正文上都看得见。</summary>
    public static readonly Color HandleFill = Color.FromArgb("#3B82F6");
    public static readonly Color HandleRing = Color.FromArgb("#FFFFFF");

    /// <summary>
    /// 「是不是深色主题」—— 取色只认这一处（与页面上的 `IsDarkTheme` 同一个判据）。
    /// 用**属性**而不是让调用方各挑一份：三档色被**错误列表 / 错误气泡 / 行下波浪线**
    /// 三处共用，让每处自己选「亮版还是暗版」就是三份判据，迟早有一处忘了改。
    /// </summary>
    private static bool IsDark => Application.Current?.RequestedTheme == AppTheme.Dark;

    /// <summary>
    /// 诊断三色 —— 行下**波浪线**与**收起态小圆点**共用这一组（<see cref="WaveColor"/> 是
    /// 唯一的严重度 → 色映射），所以调波浪线/圆点的色只动这六个常量。
    ///
    /// ⚠ **气泡底色不在这六个里**（白天档尤其不是）：那是给「在代码底下画一条细线」用的
    /// **饱和色**，当整块背景铺开就是一块暗底、与白天的深色文字撞成暗底暗字。
    /// 气泡底色是**白天/夜间两套显式值**，见 <see cref="BubbleFill"/>。
    ///
    /// 三档语义：错误红 / 警告黄 / 其它绿。**必须分主题两套**，原因是它们要压在四种底上
    /// （浅色代码底 <c>#FFFFFF</c>、浅色面板底 <c>#F0F0F3</c>、深色代码底 <c>#121214</c>、
    /// 深色面板底 <c>#1A1A1E</c>）—— 原来一档定死一个色，在浅色面板上警告色只有 **1.79**、
    /// 提示色 2.78、错误色 3.44（AA 正文门槛 4.5），也就是用户报的「浅色下看不见」。
    /// 深色那边错误色压面板也只有 4.43，一并修了。
    ///
    /// 取值是**算出来的**（WCAG 相对亮度），括号里是「浅色面板 / 浅色代码底」与
    /// 「深色面板 / 深色代码底」的实测对比度，**每一档都 ≥4.5**：
    /// · 红 <c>#C4382F</c> (4.67/5.31) ｜ <c>#FF7B72</c> (6.88/7.42)
    /// · 黄 <c>#8A5A00</c> (5.21/5.93) ｜ <c>#F5A524</c> (8.50/9.17)
    /// · 绿 <c>#17794A</c> (4.77/5.43) ｜ <c>#30A46C</c> (5.50/5.93)
    /// ⚠ 浅色那三个都比原来深得多（琥珀在近白底上想达标只能压成**深琥珀/棕**，
    /// 这是它的宿命，别为了「看着更黄」把它调回去 —— 那就又回到 1.79 了）。
    /// </summary>
    public static Color ErrorWave => IsDark ? ErrorWaveDark : ErrorWaveLight;
    public static Color WarnWave => IsDark ? WarnWaveDark : WarnWaveLight;
    public static Color InfoWave => IsDark ? InfoWaveDark : InfoWaveLight;

    private static readonly Color ErrorWaveLight = Color.FromArgb("#C4382F");
    private static readonly Color ErrorWaveDark = Color.FromArgb("#FF7B72");
    private static readonly Color WarnWaveLight = Color.FromArgb("#8A5A00");
    private static readonly Color WarnWaveDark = Color.FromArgb("#F5A524");
    private static readonly Color InfoWaveLight = Color.FromArgb("#17794A");
    private static readonly Color InfoWaveDark = Color.FromArgb("#30A46C");

    /// <summary>
    /// 严重度 → 波浪线 / 收起态小圆点用的那一档**饱和色**。**唯一一处映射**
    /// （波浪线、圆点、以及气泡底色的夜间那一支都从它派生）。
    /// </summary>
    public static Color WaveColor(Severity s) => s switch
    {
        Severity.Error => ErrorWave,
        Severity.Warning => WarnWave,
        _ => InfoWave,
    };

    /// <summary>
    /// 编译诊断气泡的**底色** —— **色相 = 严重度**（错误红 / 警告黄 / 提示绿），
    /// 每个色相 **白天 / 夜间两档**：
    /// 白天**中等浓度的浅色调**（配 <see cref="BubbleText"/> 深字）、
    /// 夜间**一律深色调**（配 <see cref="BubbleTextDark"/> 浅字）。
    ///
    /// 六个值**全部显式写出来**（照本文件的 `Xxx / XxxDark` 配对惯例），**不用**任何
    /// 「给一个值、另一个自动反相 / 自动调亮」的推导 —— 那种做法只在灰色上碰巧成立，
    /// 一旦色相是彩色就会得到谁也想不到的结果（本仓明写过的规矩）。
    ///
    /// 为什么不能继续用「波浪色压到 0.94」当底色：那个色是给**在代码底下画一条细线**用的
    /// **饱和色**，铺成整块背景时白天主题下就是一块**暗底**，而文字是白天的深色 ⇒
    /// **暗底 + 暗字**（用户真机反馈「白天模式背景也是暗色、文字也是暗色，看着别扭」）。
    /// 反过来，夜间原来的那一支取的是波浪线的**亮色档**（#FF7B72 等），配夜间浅字只有
    /// **1.8~2.8:1** —— 同样不是「深色调底」。所以两档都是显式给值，不派生。
    ///
    /// ⚠ 波浪线与收起态小圆点**不在这里**（<see cref="WaveColor"/>）：那些是细线 / 小点，
    /// 饱和色本来就对，本轮的调色一个字都没动它们 —— 所以气泡底色**必须与它同色相**，
    /// 否则同一个诊断会长出「绿波浪线 + 蓝气泡」这种跨色相的东西。
    /// **提示那一档因此是绿**（`InfoWave` 本来就是绿），不是蓝。
    ///
    /// 取值与实测对比度（WCAG 相对亮度，配 <see cref="BubbleTextColor"/> 那一档
    /// —— **六格全部 ≥ 4.5:1**，最低是夜间黄的 4.66；每条都由
    /// <c>SelfTest.Chunk27</c> 钉住）：
    /// · 红 <c>#F19A9D</c> 8.16:1 ｜ <c>#C4382F</c> 4.74:1
    /// · 黄 <c>#FACE87</c> 11.80:1 ｜ <c>#996000</c> 4.66:1
    /// · 绿 <c>#8DCDAE</c> 9.51:1 ｜ <c>#17794A</c> 4.85:1
    /// 气泡里的 ✕ 与合并列表的 `1.` / `2.` 用的就是正文那一色，所以它们跟着一起达标。
    /// </summary>
    public static Color BubbleFill(Severity s) => s switch
    {
        Severity.Error => IsDark ? BubbleBgErrorDark : BubbleBgError,
        Severity.Warning => IsDark ? BubbleBgWarnDark : BubbleBgWarn,
        _ => IsDark ? BubbleBgInfoDark : BubbleBgInfo,
    };

    /// <summary>
    /// 白天档气泡底色 —— **中等浓度的浅色调**：淡红（错误）/ 淡黄（警告）/ 淡绿（提示）。
    ///
    /// ⚠ 别往更淡的方向调。上一版是「浅到近乎白」的 `#FDECEA`/`#FFF8E1`/`#E8F5EE`，
    /// 压在白色的代码底上**只有 1.14 / 1.06 / 1.12:1** —— 那等于没上色（用户真机反馈
    /// 「看着没底色」，`#FFF8E1` 那个 1.06 尤其明显）。现在这三档对白代码底是
    /// **2.13 / 1.47 / 1.83:1**（看得见底色了），同时仍是浅色调 ——
    /// 配 <see cref="BubbleText"/> 深字有 8.2 / 11.8 / 9.5:1。
    /// </summary>
    private static readonly Color BubbleBgError = Color.FromArgb("#F19A9D");
    private static readonly Color BubbleBgWarn = Color.FromArgb("#FACE87");
    private static readonly Color BubbleBgInfo = Color.FromArgb("#8DCDAE");

    /// <summary>
    /// 夜间档气泡底色 —— **深红 / 深琥珀 / 深绿**，三档**统一是深色调**
    /// （它们要配 <see cref="BubbleTextDark"/> 那个浅字，见 <see cref="BubbleTextColor"/>）。
    ///
    /// ⚠ 黄色这档曾一度是**亮琥珀** <c>#F5A524</c>（用户当时要「夜间黄要亮」）。后来用户把
    /// 字色统一成「夜间一律浅字、日间一律深字」之后它就**不成立了** —— 亮琥珀压浅字只有
    /// **1.82:1**（几乎读不了），所以换成深琥珀。
    /// 取值是**按对比度算出来的**，不是照着 <c>#F5A524</c> 凭感觉调暗：
    /// <list type="bullet">
    /// <item>要 ≥4.5:1，底色的相对亮度上限 L ≤ 0.15843（= <c>(L(浅字)+0.05)/4.5 − 0.05</c>）；</item>
    /// <item><c>#A36600</c> 实测 L=0.1729 ⇒ 只有 **4.21:1，不达标**（它是「凭感觉调暗」的那一版）；</item>
    /// <item><c>#996000</c> L=0.1514 ⇒ **4.66:1** ✓ —— 保住「深琥珀」的色相，又留了余量。</item>
    /// </list>
    /// ⚠ 这里的「白字」是 <see cref="BubbleTextDark"/> 的 <c>#F2F2F2</c>（近白，不是纯白）：
    /// 纯白对 <c>#A36600</c> 能到 4.71 而 <c>#F2F2F2</c> 只有 4.21 —— **判据要认实际那个字色**，
    /// 拿纯白去算会得出相反的结论。
    ///
    /// 三档与 `<see cref="ErrorWave"/> / <see cref="WarnWave"/> / <see cref="InfoWave"/>`
    /// 的**亮色档**（`XxxWaveLight`）里有两个取值相同（红 <c>#C4382F</c>、绿 <c>#17794A</c>，
    /// 它们本来就是「深色代码底上那条饱和线」= 深色调底）。但**仍然显式写出来**
    /// （不写成 `ErrorWaveLight` 的引用）：波浪线的调色与气泡的调色是两件事，
    /// 谁改谁的都不该顺手把对方带走。
    /// </summary>
    private static readonly Color BubbleBgErrorDark = Color.FromArgb("#C4382F");
    private static readonly Color BubbleBgWarnDark = Color.FromArgb("#996000");
    private static readonly Color BubbleBgInfoDark = Color.FromArgb("#17794A");
    /// <summary>
    /// <summary>
    /// 白天主题下的气泡正文色（与 <see cref="BubbleTextDark"/> 成对）——
    /// **深字**：白天那三档底色都是浅色调，深字才读得清。
    /// </summary>
    public static readonly Color BubbleText = Color.FromArgb("#1A1A1A");

    /// <summary>
    /// 夜间主题下的气泡正文色（与 <see cref="BubbleText"/> 成对，
    /// 命名随本文件的 Xxx/XxxDark 惯例）—— **浅字**：夜间那三档底色都是深色调。
    ///
    /// ⚠ 它是 <c>#F2F2F2</c> 不是纯白 <c>#FFFFFF</c> —— 算对比度时**必须用这个值**：
    /// 夜间警告底 <c>#A36600</c> 配纯白能到 4.71、配这个只有 4.21（够不够 4.5 的结论会翻）。
    /// </summary>
    public static readonly Color BubbleTextDark = Color.FromArgb("#F2F2F2");

    /// <summary>
    /// 气泡正文/✕ 的取色 —— **跟随系统主题**（亮色档深字、暗色档亮字）。
    ///
    /// 用户定的：**「气泡文字颜色应该是统一的，夜间都是白色、日间都是黑色，跟随系统」** ——
    /// 所以字色**不按每个气泡的底色挑**，就是主题那一个。
    /// 于是**六个底色必须各自与它够对比度**（≥4.5:1，六格全达标，最低 4.66）——
    /// 这条约束由 <c>SelfTest.Chunk27</c> 守着：谁把某个底色改得偏亮（比如把夜间警告底调回
    /// 亮琥珀 <c>#F5A524</c>，白字下只有 1.82:1），那条断言当场红。
    ///
    /// 判据**只此一处**：让调用方各挑一份就是「同一规则两处实现」—— 气泡正文与 ✕ 分属两段
    /// 绘制代码，迟早一处按主题挑、另一处忘了。
    /// </summary>
    public static Color BubbleTextColor => IsDark ? BubbleTextDark : BubbleText;

    /// <summary>
    /// 气泡的**细描边**色 —— 取主题边框色系（半透明黑/白），**不用纯黑纯白**
    /// （纯色压在饱和底色上像一圈硬边）。
    ///
    /// 与底色一样是**白天/夜间两套显式值**：白天那支压在**淡底**上（底色不再是饱和色），
    /// 原来那条黑 25% 会显得偏重，收到黑 20%。
    /// </summary>
    public static Color BubbleStroke => IsDark ? BubbleStrokeDark : BubbleStrokeLight;

    /// <summary>
    /// 白天档描边 = 黑 25%。
    ///
    /// ⚠ 白天这一支**不能太淡**：底色换成淡色调之后，「气泡在哪儿」几乎全靠这条线
    /// （淡底与代码底 #FFFFFF 只差 1.1:1）。25% 叠在淡底上约 #BEB1B0，与代码底 2.1:1 ——
    /// 一条看得见的细线，又不至于像硬边框；20% 就偏弱了（1.8:1），30% 则开始发灰发重。
    /// </summary>
    private static readonly Color BubbleStrokeLight = Color.FromArgb("#40000000");

    /// <summary>夜间档描边 = 白 25%（与 <see cref="BarIdleDark"/> 同一档观感）—— 保持原样。</summary>
    private static readonly Color BubbleStrokeDark = Color.FromArgb("#40FFFFFF");

    // ── 诊断气泡的几何 ──────────────────────────────────────────────────────
    //
    // 气泡**画在画布上**（代码层之上），不再是叠在编辑器上的一层控件 —— 于是它的每一项尺寸
    // 都得由字号推导（字号一变，边距/圆角/箭头/✕ 一起等比变），而且**绘制与命中测试读同一份**
    // （见 <c>CodeCanvasView.BubbleHit</c>：✕ 的「画出来的方块」与「点得中的热区」是两个尺寸，
    //  也在那里一次算出来，绝不在触摸处理里重算）。

    /// <summary>气泡四周的内边距 —— 用户定的：**半个字号**（上下左右都一样）。</summary>
    public static float BubblePad => MathF.Max(4f, FontSize * 0.5f);

    /// <summary>
    /// 气泡**左上角那个尖**：横向张开的宽度（≈ 字号 × 0.9）。
    ///
    /// 气泡不是「圆角矩形 + 另画的三角尾巴」，而是**一体路径**：左上角不收圆角、
    /// 直接收成一个尖，尖端落在锚点（错误那一格的左缘 × 该行下缘）上。
    /// 这样气泡天生贴着错误位置，也不会出现「尾巴和气泡对不齐」。
    ///
    /// ⚠ 宽（本值）与高（<see cref="BubbleTipHeight"/>）**故意不等**：根部太窄像根针、
    /// 太宽像缺了个角，0.9 : 0.6 是一条顺眼的斜边。两者都随字号缩放。
    /// </summary>
    public static float BubbleTipWidth => MathF.Max(8f, FontSize * 0.9f);

    /// <summary>那个尖**高出上边缘**多少（≈ 字号 × 0.6）。</summary>
    public static float BubbleTipHeight => MathF.Max(6f, FontSize * 0.6f);

    /// <summary>
    /// 气泡圆角半径（≈ 字号 × 0.6）—— 除左上那个尖之外，其余三个角都用它。
    /// 太大显胖、太小显硬。
    /// </summary>
    public static float BubbleRadius => MathF.Max(4f, FontSize * 0.6f);

    /// <summary>气泡描边宽度（细线，随字号微调）—— 没描边时气泡与同色系的代码容易糊在一起。</summary>
    public static float BubbleStrokeSize => MathF.Max(1f, FontSize * 0.07f);

    /// <summary>
    /// **同一个位置上**相邻两个气泡之间的缝（错误/警告各一个气泡，上下挨着摆）。
    /// 只要一条缝就够 —— 两个圆角矩形贴死会糊成一块，看不出是两条。
    ///
    /// 下限是用户定的**2 个像素**：下面的气泡是块**实心色块**，缝小于 2px 时它几乎贴着
    /// 上一块，看上去还是一条。上不封顶的那一支（<c>FontSize * 0.15</c>）保持原样，
    /// 所以**它仍然随字号缩放**（大字号下气泡整体变大，缝也跟着变宽）——
    /// 别把它写死成常量 <c>2</c>：那样大小字号下缝一样宽，放大后就挤成一条线了。
    /// </summary>
    public static float BubbleStackGap => MathF.Max(2f, FontSize * 0.15f);

    /// <summary>✕ 那个方块（**画出来的**笔迹都在它里面，再内缩三分之一）。</summary>
    public static float BubbleCloseSize => MathF.Max(14f, FontSize * 1.25f);

    /// <summary>
    /// **收起态**那个小圆点的半径（气泡收起来时，它替代气泡标在波浪线起点上）。
    /// 直径 ≈ 字号 × 0.6，随字号缩放，别写死像素。
    /// </summary>
    public static float BubbleDotRadius => MathF.Max(3f, FontSize * 0.3f);

    /// <summary>
    /// 气泡里那些**小目标**（右上角 ✕、收起态的小圆点）命中热区在每个方向的外扩量。
    ///
    /// 手指点不中 17dp 的方块、更点不中 8dp 的圆点，所以「画出来的形状」与「点得中的范围」
    /// **必须是两个尺寸**：外扩 0.75 个字号（14 号 → 10.5dp，热区约 38dp 见方，
    /// 接近 Material 建议的 48dp 触控目标）。两种目标的绘制矩形与热区都由
    /// <c>CodeCanvasView</c> 的**同一处**算出来（见 <c>BubbleHit</c>）。
    /// </summary>
    public static float BubbleHitInflate => MathF.Max(10f, FontSize * 0.75f);
}
