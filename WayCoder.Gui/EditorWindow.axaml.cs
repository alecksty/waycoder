using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using WayCoder.UI.Tui.Edit;

namespace WayCoder.UI.Gui;

/// <summary>
/// 内置编辑器窗口（三端之一：GUI/Avalonia）。绑定共享 EditorCore 模型。
/// 保存后触发 Lint（EditorCore.SaveAsync → DiagnosticManager），状态栏显示诊断摘要。
/// </summary>
public partial class EditorWindow : Window
{
    private EditorCore? Core => Editor.Core;

    public EditorWindow() : this(null) { }

    public EditorWindow(string? path)
    {
        InitializeComponent();
        Editor.CoreChanged += OnCoreChanged;
        KeyDown += Window_KeyDown;
        Closing += Window_Closing;
        OnCoreChanged();
        if (!string.IsNullOrEmpty(path)) OpenPath(path);
    }

    private void OnCoreChanged() => UpdateStatus();

    // XAML Click 处理器（转调 async 方法）
    private void Save_Click(object? sender, RoutedEventArgs e) => SaveAsync();
    private void Open_Click(object? sender, RoutedEventArgs e) => OpenFile();
    private void New_Click(object? sender, RoutedEventArgs e) => NewFile();
    private void Find_Click(object? sender, RoutedEventArgs e) => ShowFindBar();

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        var ctrl = (e.KeyModifiers & KeyModifiers.Control) != 0;
        if (ctrl && e.Key == Key.S) { e.Handled = true; SaveAsync(); }
        else if (ctrl && e.Key == Key.F) { e.Handled = true; ShowFindBar(); }
        else if (e.Key == Key.F3) { e.Handled = true; FindNext(); }
        else if (e.Key == Key.Escape && FindBar.IsVisible) { e.Handled = true; FindBar.IsVisible = false; }
        else if (ctrl && e.Key == Key.N) { e.Handled = true; NewFile(); }
        else if (ctrl && e.Key == Key.O) { e.Handled = true; OpenFile(); }
    }

    private async void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (Core == null || !Core.Modified) return;
        var result = await ShowMessageBox("未保存", "文件未保存，要保存吗？", "保存", "不保存", "取消");
        switch (result)
        {
            case 0: SaveAsync(); break;      // 保存
            case 1: break;                    // 不保存
            default: e.Cancel = true; return; // 取消
        }
    }

    /// <summary>简易三选对话框：0=主按钮 1=次按钮 -1=取消。</summary>
    private Task<int> ShowMessageBox(string title, string message, string primary, string secondary, string cancel)
    {
        var tcs = new TaskCompletionSource<int>();
        Window win = null!;
        var b1 = new Button { Content = primary };
        var b2 = new Button { Content = secondary };
        var b3 = new Button { Content = cancel };
        b1.Click += (_, _) => { tcs.TrySetResult(0); win.Close(); };
        b2.Click += (_, _) => { tcs.TrySetResult(1); win.Close(); };
        b3.Click += (_, _) => { tcs.TrySetResult(-1); win.Close(); };
        win = new Window
        {
            Title = title,
            Width = 380,
            Height = 170,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Children = { b1, b2, b3 },
                    },
                },
            },
        };
        win.ShowDialog(this);
        return tcs.Task;
    }

    // ════════════════════════ 打开 / 新建 / 保存 ════════════════════════

    private void OpenPath(string path)
    {
        Editor.LoadFile(path);
        UpdateStatus();
    }

    private async void OpenFile()
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "打开文件",
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("所有文件") { Patterns = ["*"] }],
            });
            if (files is { Count: > 0 } && files[0].TryGetLocalPath() is { } p)
                OpenPath(p);
        }
        catch (Exception ex) { DiagLabel.Text = "打开失败: " + ex.Message; }
    }

    private async void NewFile()
    {
        var path = await PromptPathAsync("新建文件路径（相对项目根目录或绝对路径）", "src/foo.cs");
        if (!string.IsNullOrEmpty(path)) OpenPath(path);
    }

    private async void SaveAsync()
    {
        if (Core == null) return;
        if (string.IsNullOrEmpty(Core.FilePath))
        {
            var path = await PromptPathAsync("保存为新文件路径", "src/foo.cs");
            if (string.IsNullOrEmpty(path)) return;
            Editor.LoadFile(path); // EditorCore.FilePath 只读，重建核心并加载
        }
        await Core.SaveAsync();
        UpdateStatus();
        _ = DelayedRefreshDiagsAsync();
    }

    private async Task DelayedRefreshDiagsAsync()
    {
        await Task.Delay(3000);
        await Dispatcher.UIThread.InvokeAsync(UpdateStatus);
    }

    /// <summary>弹出路径输入对话框，返回输入；取消返回 null。</summary>
    private Task<string?> PromptPathAsync(string hint, string placeholder)
    {
        var tcs = new TaskCompletionSource<string?>();
        Window win = null!;
        var input = new TextBox { PlaceholderText = placeholder, FontSize = 13 };
        var ok = new Button { Content = "确定" };
        var cancel = new Button { Content = "取消" };
        ok.Click += (_, _) => { tcs.TrySetResult(input.Text ?? ""); win.Close(); };
        cancel.Click += (_, _) => { tcs.TrySetResult(null); win.Close(); };
        win = new Window
        {
            Title = hint,
            Width = 420,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = hint, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    input,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Children = { ok, cancel },
                    },
                },
            },
        };
        win.ShowDialog(this);
        return tcs.Task;
    }

    // ════════════════════════ 查找 ════════════════════════

    private void ShowFindBar()
    {
        FindBar.IsVisible = true;
        FindInput.Focus();
    }

    private void FindInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { e.Handled = true; FindNext(); }
        else if (e.Key == Key.Escape) { e.Handled = true; FindBar.IsVisible = false; }
    }

    private void FindClose_Click(object? sender, RoutedEventArgs e) => FindBar.IsVisible = false;

    private void FindNext()
    {
        if (Core == null) return;
        var term = FindInput.Text ?? "";
        if (term.Length == 0) { FindStatus.Text = ""; return; }
        var (line, col) = Core.FindNext(term, Core.Cy, Core.Cx);
        if (line >= 0)
        {
            Core.JumpToLineCol(line, col);
            Core.Scroll = Math.Max(0, line - 5);
            FindStatus.Text = "找到 ✓";
        }
        else
        {
            FindStatus.Text = "无更多匹配";
        }
        UpdateStatus();
    }

    // ════════════════════════ 状态栏 ════════════════════════

    private void UpdateStatus()
    {
        if (Core == null) return;
        PosLabel.Text = $"L{Core.Cy + 1}:C{Core.Cx + 1}";
        MetaLabel.Text = $"{Core.TotalLines} 行 · {Core.TotalChars} 字符";
        LangLabel.Text = Core.Syntax?.Name ?? "";
        var (errors, warnings) = DiagnosticManager.GetSummary(Core.FilePath);
        DiagLabel.Text = errors > 0 || warnings > 0 ? $"{errors} 错误 · {warnings} 警告" : "";
        DirtyLabel.IsVisible = Core.Modified;
        Title = (Core.Modified ? "● " : "")
                + (string.IsNullOrEmpty(Core.FilePath) ? "未命名" : System.IO.Path.GetFileName(Core.FilePath))
                + " — WayCoder";
    }
}
