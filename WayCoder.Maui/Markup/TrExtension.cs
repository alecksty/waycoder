using Microsoft.Maui.Controls.Xaml;

namespace WayCoder.Maui.Markup;

/// <summary>
/// XAML 标记扩展：<c>{markup:Tr Zh='确定', En='OK'}</c> —— 给 XAML 里的一处文案就地双语化。
///
/// <para>
/// 与 C# 侧共用同一个语言源（<see cref="L.Pick"/>，两个实参就是键、没有键表）。
/// 标记扩展只是把「两个实参」搬到 XAML 的语法里，**不引入第二套机制** ——
/// 所以 XAML 与 C# 两边的用词天然一致，也不会出现"键没跟上"那种静默回退。
/// </para>
///
/// <para>
/// ⚠ <b>求值时机</b>：<see cref="ProvideValue"/> 在 **XAML 解析那一刻**被调用，也就是页面构造时。
/// 而界面语言在 App 启动时就定好了（<c>MauiLang.Initialize()</c>，它是 <c>CreateMauiApp()</c> 的
/// **第一句**，早于 <c>builder.Build()</c> 构造 App 与页面），此后不再变（本方案
/// **刻意不做 App 内语言开关**，只跟随系统语言）⇒ 构造时求值是安全的。
/// <b>但如果将来加了"运行时切换语言"，这里必须改</b> —— 那时标记扩展要返回可刷新的绑定对象，
/// 而不是一个已经算好的字符串。
/// </para>
///
/// <para>
/// ⚠ <b>它真的在 <c>MauiXamlInflator=SourceGen</c> 下生效（实测过，不是推断）</b>。
/// 本项目 csproj 设的是 <c>&lt;MauiXamlInflator&gt;SourceGen&lt;/MauiXamlInflator&gt;</c>，
/// 而"SourceGen 支不支持自定义标记扩展"是这条路线的**唯一前提**，所以先用一个页面验了：
/// <c>dotnet build -f net10.0-windows10.0.19041.0 -t:Rebuild -p:EmitCompilerGeneratedFiles=true</c>
/// 之后在 <c>obj/&lt;tfm&gt;/generated/…XamlGenerator/Pages_AboutPage.xaml.xsg.cs</c> 里能看到
/// 生成器**逐条**产出了
/// <c>trExtension.Zh = "关于"; trExtension.En = "About";</c> 与
/// <c>((IMarkupExtension&lt;string&gt;)trExtension).ProvideValue(null)</c>。
/// <b>Android 目标的生成结果逐字相同</b>（XAML 生成器是 Roslyn 源生成器，不随 TFM 变）。
/// </para>
///
/// <para>
/// ⚠ 生成代码传的是 <c>ProvideValue(null)</c> ⇒ <see cref="AcceptEmptyServiceProviderAttribute"/>
/// **不是装饰、是必需的**：没有它，生成器会改成去构造一整套
/// <c>SimpleValueTargetProvider + XamlServiceProvider</c>（同一份文件里 <c>StaticResource</c> /
/// <c>AppThemeBinding</c> 就是那样），凭空多出一堆分配。
/// 本扩展不碰 <c>IProvideValueTarget</c>，也就不需要那些东西。
/// </para>
/// </summary>
[ContentProperty(nameof(Zh))]
[AcceptEmptyServiceProvider]
public sealed class TrExtension : IMarkupExtension<string>
{
    /// <summary>中文文案。</summary>
    public string Zh { get; set; } = "";

    /// <summary>英文文案。</summary>
    public string En { get; set; } = "";

    public string ProvideValue(IServiceProvider serviceProvider) => L.Pick(Zh, En);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
