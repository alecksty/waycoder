using WayCoder.Maui.Markup;
using WayCoder.Maui.Services;
using WayCoder.UI.Shared;

namespace WayCoder.Maui.Pages;

/// <summary>
/// 使用说明的**三级页**：一篇说明的正文。
///
/// 路由：`helptopic?id=<随包路径>`（如 `vml/ui`）。
/// 正文从 `Resources/Raw/help/<id>.md` 读，渲染复用 <see cref="MarkdownPreview"/>。
/// </summary>
[QueryProperty(nameof(TopicId), "id")]
public partial class HelpPage : ContentPage
{
    public HelpPage()
    {
        InitializeComponent();
    }

    /// <summary>要显示的说明 id（= 随包路径，如 `vml/ui`），由路由查询串注入。</summary>
    public string? TopicId { get; set; }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RenderAsync();
    }

    private async Task RenderAsync()
    {
        var id = TopicId;
        if (string.IsNullOrEmpty(id)) return;

        Body.Clear();

        var md = await LoadMarkdownAsync(id);

        // 标题：目录表里有就用它（更短、更适合列表里看），否则**以正文自己写的一级标题为准**。
        // 链接能跳到任意一篇（`help:vml/lang/c`）—— 那些目标不一定在目录表里，
        // 不这么做标题就会退化成通用的「使用说明」（页面照常打开、内容也对，只有标题不对）。
        Title = HelpCatalog.FindTopic(id)?.Title
             ?? HelpCatalog.HeadingOf(md)
             ?? "使用说明";
        if (md is null)
        {
            // 找不到就说清楚是哪一篇找不到 —— 静默空白最难查（多半是目录里写了、文件没放进包）
            Body.Add(new Label
            {
                Text = $"找不到这篇说明（{HelpCatalog.AssetPath(id)}）。\n"
                     + "如果是刚加的说明，检查：① .md 放在 Resources/Raw/help/ 下；"
                     + "② 文件名与目录表里的 id 一致。",
                FontSize = 13,
                TextColor = MauiUi.Res("MutedTextLight"),
            });
            return;
        }

        Body.Add(MarkdownPreview.Render(md, MauiUi.IsDark));
    }

    /// <summary>
    /// 读一篇说明的正文（找不到返回 null —— 由调用方给一句人话，而不是抛）。
    ///
    /// ⚠ 走 `FileSystem.OpenAppPackageFileAsync`：`MauiAsset` 的 `LogicalName` 是
    /// `%(RecursiveDir)%(Filename)%(Extension)`，所以子目录**保留在逻辑名里**
    /// （`help/vml/ui.md`），按这个相对路径取即可。
    /// </summary>
    private static async Task<string?> LoadMarkdownAsync(string id)
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(HelpCatalog.AssetPath(id));
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch { return null; }
    }
}
