using WayCoder.Tools;
using WayCoder.UI.Tui;

namespace WayCoder.Maui.Services;

/// <summary>
/// MAUI 原生交互桥 —— 实现 <see cref="UxHelper.IWebInteraction"/>（5 方法），
/// 把主工程的提问/确认/多选/diff 确认从「终端 Console / 浏览器 SSE」切换到「手机原生对话框」。
///
/// 关键线程语义：<see cref="PermissionManager.ShowConfirmDialog"/> 与 <see cref="AskUserQuestionTool"/>
/// 均以 <c>await WebInteraction.XXXAsync(...)</c> 协作式调用（不阻塞 UI 线程），
/// 故本桥用 <c>MainThread.InvokeOnMainThreadAsync</c> 统一切主线程弹框——
/// 后台线程调用时排队到 UI 线程、UI 线程调用时直接执行，两种路径都无死锁。
///
/// timeoutMs 参数在移动端忽略（弹框无超时，用户想多久都行；Web 端的超时防死锁在此不需要）。
/// </summary>
public sealed class MauiWebInteraction : UxHelper.IWebInteraction
{
    /// <summary>当前可弹框的页面（导航栈顶）。</summary>
    private static Page? CurrentPage
        => Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;

    public Task<string?> AskAsync(string prompt, string? defaultValue, int timeoutMs)
        => MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = CurrentPage;
            if (page == null) return null;
            return await page.DisplayPromptAsync("输入", prompt, "确定", "取消",
                initialValue: defaultValue, maxLength: 4000);
        });

    public Task<string?> SelectAsync(string title, List<string> choices, int timeoutMs)
        => MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = CurrentPage;
            if (page == null || choices.Count == 0) return null;
            var choice = await page.DisplayActionSheetAsync(title, "取消", null, choices.ToArray());
            return choice == "取消" ? null : choice;
        });

    public Task<List<string>?> MultiSelectAsync(string title, List<string> choices, int timeoutMs)
        => MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = CurrentPage;
            if (page == null) return null;
            if (choices.Count == 0) return new List<string>();
            return await MultiSelectPage.ShowAsync(page, title, choices);
        });

    public Task<int> ConfirmAsync(string title, string message, bool allowAll, int timeoutMs)
        => MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = CurrentPage;
            if (page == null) return 2; // 无法弹框 → 保守拒绝

            if (allowAll)
            {
                // 非危险工具：允许 / 总是允许 / 拒绝
                var choice = await page.DisplayActionSheetAsync($"{title}\n\n{message}", "拒绝", null, "允许", "总是允许");
                return choice switch { "允许" => 0, "总是允许" => 1, _ => 2 };
            }

            // 危险工具：仅允许 / 拒绝
            var ok = await page.DisplayAlertAsync(title, message, "允许", "拒绝");
            return ok ? 0 : 2;
        });

    public Task<DiffConfirmResult?> DiffConfirmAsync(string filePath, List<DiffPreview.Hunk> hunks, int timeoutMs)
        => MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = CurrentPage;
            if (page == null) return null;

            var body = RenderHunks(hunks);
            var ok = await page.DisplayAlertAsync($"Diff 预览 · {filePath}", body, "接受全部", "拒绝全部");
            return new DiffConfirmResult
            {
                Decision = ok ? DiffPreview.Decision.AcceptAll : DiffPreview.Decision.RejectAll,
            };
        });

    /// <summary>把 hunk 列表拼成可读文本（移动端简化：逐 hunk 逐行展示，不画 ANSI 颜色）。</summary>
    private static string RenderHunks(List<DiffPreview.Hunk> hunks)
    {
        if (hunks.Count == 0) return "（无变更）";
        var sb = new System.Text.StringBuilder();
        foreach (var h in hunks)
        {
            if (!string.IsNullOrEmpty(h.Header)) sb.AppendLine(h.Header);
            foreach (var line in h.Lines)
            {
                var prefix = line.Kind switch { '+' => "＋", '-' => "－", _ => "  " };
                sb.Append(prefix).Append(' ').AppendLine(line.Text);
            }
        }
        // 按 Rune 截断，避免超长 diff 撑爆对话框（UTF-16 代理对安全）
        var text = sb.ToString();
        return text.Length > 4000
            ? string.Concat(text.EnumerateRunes().Take(4000).Select(r => r.ToString())) + "\n…（内容过长已截断）"
            : text;
    }
}

/// <summary>多选对话框 —— MAUI 无原生多选，用纯代码构建的 modal 页（CheckBox 列表 + 确定/取消）。</summary>
internal sealed class MultiSelectPage : ContentPage
{
    private sealed class Item
    {
        public string Label { get; set; } = "";
        public bool Selected { get; set; }
    }

    private readonly TaskCompletionSource<List<string>?> _tcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly List<Item> _items;

    private MultiSelectPage(string title, List<string> choices)
    {
        Title = title;
        _items = choices.Select(c => new Item { Label = c }).ToList();

        var stack = new VerticalStackLayout { Spacing = 0 };
        foreach (var item in _items)
        {
            var cb = new CheckBox();
            cb.CheckedChanged += (_, e) => item.Selected = e.Value;

            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                },
                Padding = new Thickness(0, 8),
            };
            row.Add(cb, 0, 0);
            row.Add(new Label { Text = item.Label, VerticalOptions = LayoutOptions.Center }, 1, 0);

            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => cb.IsChecked = !cb.IsChecked;
            row.GestureRecognizers.Add(tap);

            stack.Add(row);
        }

        var cancel = new Button { Text = "取消" };
        cancel.Clicked += (_, _) => _tcs.TrySetResult(null);
        var ok = new Button { Text = "确定" };
        ok.Clicked += (_, _) => _tcs.TrySetResult(_items.Where(i => i.Selected).Select(i => i.Label).ToList());

        var buttons = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
            },
            ColumnSpacing = 12,
        };
        buttons.Add(cancel, 0, 0);
        buttons.Add(ok, 1, 0);

        Content = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto),
            },
            Padding = 16,
            RowSpacing = 12,
        };
        ((Grid)Content).Add(new ScrollView { Content = stack }, 0, 0);
        ((Grid)Content).Add(buttons, 0, 1);
    }

    /// <summary>弹出多选页并等待结果；返回选中项 label 列表（取消返回 null）。</summary>
    public static async Task<List<string>?> ShowAsync(Page host, string title, List<string> choices)
    {
        var page = new MultiSelectPage(title, choices);
        var resultTask = page._tcs.Task;
        await host.Navigation.PushModalAsync(new NavigationPage(page));
        var result = await resultTask;
        await host.Navigation.PopModalAsync();
        return result;
    }
}
