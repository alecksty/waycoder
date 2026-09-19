using WayCoder.UI.Shared;

namespace WayCoder.Maui.Pages;

/// <summary>
/// 关于页：图标 / App 名 / 版本号 / **使用说明的分类入口**。
///
/// 说明内容是**多级**的：这里是一级（分类按钮），点开是二级（主题列表 `HelpListPage`）
/// 或三级（正文 `HelpPage`）。分类与正文都不在这个页面里 —— 见 <see cref="HelpCatalog"/>。
/// </summary>
public partial class AboutPage : ContentPage
{
    public AboutPage()
    {
        InitializeComponent();
        BuildCategories();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        VersionLabel.Text = $"版本 {Global.Version}";
    }

    /// <summary>
    /// 按目录表生成分类按钮。
    ///
    /// ⚠ **分类是"按钮"而不是"列表项"**：它们点下去是**跳转**（进二级页），
    /// 不是在同一页里选中某一行 —— 用 Button 让"点得动"这件事在视觉上就成立
    /// （列表项的点击语义要用户自己试出来）。
    /// </summary>
    private void BuildCategories()
    {
        foreach (var cat in HelpCatalog.Categories)
        {
            var btn = new Button
            {
                Text = $"{cat.Icon}  {cat.Title}",
                FontSize = 15,
                HorizontalOptions = LayoutOptions.Fill,
                HeightRequest = 48,
                Padding = new Thickness(16, 0),
                CornerRadius = 10,
            };
            btn.Clicked += async (_, _) => await HelpListPage.OpenAsync(cat);
            Categories.Add(btn);
        }
    }
}
