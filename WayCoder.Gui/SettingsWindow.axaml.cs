using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using WayCoder.Tools;
using WayCoder.UI.Shared;

namespace WayCoder.UI.Gui;

/// <summary>
/// 设置窗口（Schema 驱动，对齐 TUI SettingsPage / Web drawer）。
/// 静态壳在 SettingsWindow.axaml（分类列表 + 详情滚动），动态设置项按 Schema 类别代码构建。
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly MainWindow _owner;
    private List<IGrouping<string, SettingDef>> _groups = [];
    private readonly Dictionary<string, Control> _controls = new(); // Key → 控件（保存时逐项读取）

    /// <summary>仅在 Avalonia XAML 加载器/预览器路径使用；生产流程走带 owner 的构造。</summary>
    public SettingsWindow()
    {
        InitializeComponent();
        _owner = null!;
    }

    public SettingsWindow(MainWindow owner)
    {
        InitializeComponent();
        _owner = owner;

        // Schema 驱动：全部设置项（对齐 TUI SettingsPage / Web drawer）
        var schema = Config.SettingSchema().OrderBy(s => s.Order).ToList();
        _groups = schema.GroupBy(s => s.Category).ToList();
        CategoryList.ItemsSource = _groups.Select(g => g.Key).ToList();
        CategoryList.SelectedIndex = 0;
    }

    private void Category_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        => RebuildDetail();

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            foreach (var (key, ctrl) in _controls)
            {
                string? val = ctrl switch
                {
                    TextBox tb => tb.Text,
                    ComboBox cb => cb.SelectedItem?.ToString(),
                    CheckBox chk => chk.IsChecked == true ? "true" : "false",
                    _ => null,
                };
                if (val != null) Config.TrySetPropValue(key, val, out _);
            }

            Config.Instance.SaveToEnvFile();
            ModelCatalog.Invalidate();
            _owner.NotifySettingsSaved();
        }
        catch (Exception ex)
        {
            _owner.NotifySettingsFailed(ex.Message);
        }

        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();

    /// <summary>切换分类 → 重建右侧设置项。</summary>
    private void RebuildDetail()
    {
        DetailHost.Children.Clear();
        if (CategoryList.SelectedItem is not string cat) return;
        var items = _groups.First(g => g.Key == cat).OrderBy(s => s.Order).ToList();
        // 分组「全部复位默认」
        var resetAll = UiKit.MakeButton("♻ 全部复位默认", ButtonVariant.Secondary, () =>
        {
            foreach (var s in items)
                if (!string.IsNullOrEmpty(s.Default))
                    Config.TrySetPropValue(s.Key, s.Default, out _);
            RebuildDetail();
        });
        DetailHost.Children.Add(resetAll);
        foreach (var s in items)
        {
            // 标题 + 描述
            var title = new TextBlock
            {
                Text = s.Label, FontSize = 13, FontWeight = FontWeight.Bold,
                Foreground = GuiColors.Text
            };
            var desc = new TextBlock
            {
                Text = s.Desc, FontSize = 11, Foreground = GuiColors.DimText,
                TextWrapping = TextWrapping.Wrap
            };
            var ctrl = BuildSettingControl(s);
            _controls[s.Key] = ctrl;
            // 单项「↺ 默认」（把改错的值设回 schema 默认）
            var resetBtn = UiKit.MakeButton("↺ 默认", ButtonVariant.Secondary, () =>
            {
                if (string.IsNullOrEmpty(s.Default)) return;
                Config.TrySetPropValue(s.Key, s.Default, out _);
                RebuildDetail();
            });
            resetBtn.IsEnabled = !string.IsNullOrEmpty(s.Default);
            var ctrlRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            ctrlRow.Children.Add(ctrl);
            ctrlRow.Children.Add(resetBtn);
            DetailHost.Children.Add(title);
            DetailHost.Children.Add(desc);
            DetailHost.Children.Add(ctrlRow);
        }
    }

    /// <summary>按 Schema 类型构建设置控件（toggle/select/secret/number/text）。</summary>
    private static Control BuildSettingControl(SettingDef s)
    {
        var current = Config.GetPropValue(s.Key);
        switch (s.Type)
        {
            case "toggle":
                return new CheckBox
                {
                    Content = s.Label, IsChecked = current is "true" or "True",
                    Foreground = GuiColors.Text
                };
            case "select":
            {
                var cb = new ComboBox
                    { ItemsSource = s.Options ?? [], Width = 240, HorizontalAlignment = HorizontalAlignment.Left };
                if (s.Options != null)
                {
                    var idx = Array.FindIndex(s.Options,
                        o => o == current ||
                             (current != null && o.Equals(current, StringComparison.OrdinalIgnoreCase)));
                    cb.SelectedIndex = idx >= 0 ? idx : 0;
                }

                return cb;
            }
            case "secret":
                return new TextBox
                {
                    Text = current ?? "", PasswordChar = '•', Width = 240,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
            case "number":
            default:
                return new TextBox
                    { Text = current ?? "", Width = 240, HorizontalAlignment = HorizontalAlignment.Left };
        }
    }
}
