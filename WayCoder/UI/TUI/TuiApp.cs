namespace WayCoder.UI.TUI;

/// <summary>
/// TUI 应用基类 —— 布局写进 .tui 标记文件（App/Screen/Window/Dialog/控件），交互写进子类 BindEvents。
/// 对标 XAML 的「.xaml + .xaml.cs」工作模式，code-behind 只需写少量交互消息代码。
///
/// 用法：
/// <![CDATA[
/// class MyApp : TuiApp
/// {
///     protected override void BindEvents(TuiMarkupResult page)
///     {
///         page.Find<TuiButton>("ok")!.OnClick = _ => page.Find<TuiLabel>("msg")!.Text = "已确认";
///     }
/// }
/// var app = new MyApp();
/// var result = app.LoadFile("main.tui");
/// // result.Screen 推入 TuiManager，或 result.Window 挂到屏幕
/// ]]>
/// </summary>
public abstract class TuiApp
{
    /// <summary>当前加载的结果（Load 后可用）。</summary>
    public TuiMarkupResult? Page { get; private set; }

    /// <summary>从标记文本加载并接线。</summary>
    public TuiMarkupResult Load(string xml)
    {
        Page = TuiMarkup.Load(xml);
        BindEvents(Page);
        return Page;
    }

    /// <summary>从 .tui 文件加载并接线。</summary>
    public TuiMarkupResult LoadFile(string path) => Load(File.ReadAllText(path));

    /// <summary>子类覆写：写交互消息代码（page.Find(id) 拿控件 + 订阅事件）。</summary>
    protected abstract void BindEvents(TuiMarkupResult page);
}
