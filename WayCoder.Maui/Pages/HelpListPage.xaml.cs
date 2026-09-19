using WayCoder.UI.Shared;

namespace WayCoder.Maui.Pages;

/// <summary>
/// 使用说明的**二级页**：一个分类下的主题列表。
///
/// 路由：`help?cat=<分类key>`，由「关于」页的分类按钮 push 过来。
/// 只有一条主题的分类**点分类直接进正文**（少一级点击，"快速上手"就是这种）。
/// </summary>
[QueryProperty(nameof(CategoryKey), "cat")]
public partial class HelpListPage : ContentPage
{
    public HelpListPage()
    {
        InitializeComponent();
        List.SelectionChanged += OnPicked;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var cat = HelpCatalog.Find(CategoryKey);
        if (cat is not { } c)
        {
            Title = "使用说明";
            List.ItemsSource = Array.Empty<object>();
            return;
        }

        Title = c.Title;
        List.ItemsSource = c.Topics
            .Select(t => new Row(t.Id, t.Title, t.Summary, c.Icon))
            .ToList();
    }

    /// <summary>当前分类的 key —— 由 Shell 的路由查询串注入（`help?cat=vml`）。</summary>
    public string? CategoryKey { get; set; }

    private async void OnPicked(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Row row) return;
        List.SelectedItem = null;            // 取消选中，返回时不留高亮
        await Shell.Current.GoToAsync($"helptopic?id={Uri.EscapeDataString(row.Id)}");
    }

    /// <summary>列表行（模板里按名字绑定 —— 见 XAML 的 x:Name）。</summary>
    public sealed record Row(string Id, string Title, string Summary, string Icon);

    /// <summary>给「关于」页用：这个分类只有一条主题时代它直接进正文。</summary>
    public static async Task OpenAsync(HelpCatalog.Category cat)
    {
        if (cat.Topics.Length == 1)
            await Shell.Current.GoToAsync($"helptopic?id={Uri.EscapeDataString(cat.Topics[0].Id)}");
        else
            await Shell.Current.GoToAsync($"help?cat={Uri.EscapeDataString(cat.Key)}");
    }
}
