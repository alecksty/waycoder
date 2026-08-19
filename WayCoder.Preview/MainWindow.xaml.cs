using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using WayCoder.Preview.Render;
using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Base;

namespace WayCoder.Preview;

public partial class MainWindow : Window
{
    private FileSystemWatcher? _watcher;
    private System.Threading.Timer? _debounce;
    private string? _currentPath;

    public MainWindow()
    {
        InitializeComponent();
        // 文件变更防抖：保存后 300ms 重渲染（仿 TuiPreview.Watch）
        _debounce = new System.Threading.Timer(
            _ => Dispatcher.Invoke(RenderCurrent),
            null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

        // ContentHost 至少铺满视口 → 内容小于视口时居中（大于视口仍可滚动）
        Scroller.SizeChanged += (_, _) =>
        {
            ContentHost.MinWidth = Scroller.ViewportWidth;
            ContentHost.MinHeight = Scroller.ViewportHeight;
        };

        // 屏幕分辨率快选：填充预设并选中当前档（选中即触发 SelectionChanged → 按该尺寸渲染）
        foreach (var (c, r) in ScreenSizes)
            SizeCombo.Items.Add($"{c}x{r}");
        SizeCombo.SelectedIndex = _sizeIndex;

        // 可编辑组合框：内层文本框的 TextChanged 是路由事件，bubble 到 ComboBox，这里挂接
        PathBox.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(PathBox_TextChanged));
        LoadRecentFiles();

        // Ctrl+滚轮缩放（在 ScrollViewer 上拦击，先于滚动；普通滚轮仍滚动）
        Scroller.PreviewMouseWheel += (_, e) =>
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                ZoomSlider.Value = Math.Clamp(ZoomSlider.Value + (e.Delta > 0 ? 10 : -10), 25, 400);
                e.Handled = true;
            }
        };
    }

    /// <summary>网格开关：显示/隐藏单元格网格线（设计期看格子边界）。</summary>
    private void GridToggle_Checked(object sender, RoutedEventArgs e)
    {
        Grid.ShowGrid = GridToggle.IsChecked == true;
        Grid.InvalidateVisual();
    }

    // ── 最近打开文件（最多 10 个，持久化到 %LocalAppData%/WayCoder.Preview/recent.txt）──
    private static readonly string RecentStore = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WayCoder.Preview", "recent.txt");
    private readonly List<string> _recent = [];

    private void LoadRecentFiles()
    {
        try
        {
            if (File.Exists(RecentStore))
                foreach (var line in File.ReadAllLines(RecentStore))
                    if (TuiFrameRenderer.IsLoadable(line)
                        && !_recent.Contains(line, StringComparer.OrdinalIgnoreCase))
                        _recent.Add(line);
        }
        catch { }
        RefreshPathBoxItems(_recent.Count > 0 ? _recent[0] : null);
    }

    private void AddRecentFile(string path)
    {
        _recent.RemoveAll(p => p.Equals(path, StringComparison.OrdinalIgnoreCase));
        _recent.Insert(0, path);
        if (_recent.Count > 10) _recent.RemoveRange(10, _recent.Count - 10);
        RefreshPathBoxItems(path);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RecentStore)!);
            File.WriteAllLines(RecentStore, _recent);
        }
        catch { }
    }

    /// <summary>刷新路径下拉：可用资源名 + 最近打开（文件或资源名）。</summary>
    private void RefreshPathBoxItems(string? select = null)
    {
        PathBox.Items.Clear();
        foreach (var r in _resources.Value)
            if (!PathBox.Items.Contains(r)) PathBox.Items.Add(r);
        foreach (var p in _recent)
            if (!PathBox.Items.Contains(p)) PathBox.Items.Add(p);
        if (select != null) PathBox.Text = select;
        else if (PathBox.Text.Length == 0 && _recent.Count > 0) PathBox.Text = _recent[0];
    }

    /// <summary>可用标记资源名（输出 Raw/ 复制 + 开发态向上查找），启动时缓存一次。</summary>
    private static readonly Lazy<List<string>> _resources = new(static () =>
    {
        var set = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rel in new[] { Path.Combine("Raw"), Path.Combine("UI", "TUI", "Raw"), Path.Combine("WayCoder", "UI", "TUI", "Raw") })
        {
            var baseDir = Path.Combine(AppContext.BaseDirectory, rel);
            if (Directory.Exists(baseDir)) roots.Add(Path.GetFullPath(baseDir));
            var dir = Directory.GetCurrentDirectory();
            while (dir != null)
            {
                var p = Path.Combine(dir, rel);
                if (Directory.Exists(p)) roots.Add(Path.GetFullPath(p));
                var parent = Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }
        }
        foreach (var root in roots)
            foreach (var f in Directory.GetFiles(root, "*.tui", SearchOption.AllDirectories))
                set.Add(Path.GetRelativePath(root, f).Replace('\\', '/'));
        return set.ToList();
    });

    /// <summary>加载并渲染一个 .tui 文件（命令行参数/打开对话框/下拉选择共用）。</summary>
    public void LoadFile(string path)
    {
        _currentPath = path;
        AddRecentFile(path); // 记录到最近文件列表
        RenderCurrent();
        StartWatch(path);
    }

    private void PathBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var p = PathBox.Text.Trim();
        if (!string.IsNullOrEmpty(p) && p != _currentPath && TuiFrameRenderer.IsLoadable(p))
        {
            _currentPath = p;
            RenderCurrent();
            StartWatch(p);
        }
    }

    private void OpenBtn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "TUI 标记|*.tui|所有文件|*.*" };
        if (dlg.ShowDialog(this) == true)
            LoadFile(dlg.FileName);
    }

    /// <summary>预设缩放档位（常用整数倍）。</summary>
    private static readonly double[] ZoomLevels = [25, 50, 100, 200, 400];
    private int _zoomIndex = 2; // 100%

    /// <summary>模拟屏幕尺寸预设（常用终端尺寸：80x25 / 128x40 等）。</summary>
    private static readonly (int C, int R)[] ScreenSizes =
        [(80, 25), (100, 30), (120, 36), (128, 40), (160, 48), (200, 60), (240, 72)];
    private int _sizeIndex;
    private int _simCols = 80, _simRows = 25;

    /// <summary>下拉快选分辨率：直接跳到该档（SelectionChanged 统一渲染）。</summary>
    private void SizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SizeCombo.SelectedIndex < 0) return;
        _sizeIndex = SizeCombo.SelectedIndex;
        (_simCols, _simRows) = ScreenSizes[_sizeIndex];
        RenderCurrent();
    }

    private void SizeInc_Click(object sender, RoutedEventArgs e)
    {
        if (_sizeIndex < ScreenSizes.Length - 1)
            SizeCombo.SelectedIndex = _sizeIndex + 1; // 触发 SelectionChanged → 渲染
    }

    private void SizeDec_Click(object sender, RoutedEventArgs e)
    {
        if (_sizeIndex > 0)
            SizeCombo.SelectedIndex = _sizeIndex - 1; // 触发 SelectionChanged → 渲染
    }

    private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // XAML 解析时 Value/Maximum 赋值会提前触发本事件，Grid 可能尚未创建
        if (Grid == null || ZoomLabel == null) return;
        // 滑块直接缩放（25%~400% 连续）
        Grid.SetZoom(ZoomSlider.Value / 100.0);
        ZoomLabel.Text = $"{(int)Math.Round(ZoomSlider.Value)}%";
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        if (_zoomIndex < ZoomLevels.Length - 1) { _zoomIndex++; ZoomSlider.Value = ZoomLevels[_zoomIndex]; }
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        if (_zoomIndex > 0) { _zoomIndex--; ZoomSlider.Value = ZoomLevels[_zoomIndex]; }
    }

    private void RenderCurrent()
    {
        if (string.IsNullOrEmpty(_currentPath))
        {
            Grid.SetGrid(null);
            Status.Content = "未指定文件或资源名";
            return;
        }

        try
        {
            // 文件存在→读内容；否则按资源名（自动填充 {title}）。两种都用当前模拟屏幕尺寸渲染（行列按钮调整）
            (string Frame, int Cols, int Rows) r = File.Exists(_currentPath)
                ? TuiFrameRenderer.Render(File.ReadAllText(_currentPath), _simCols, _simRows)
                : TuiFrameRenderer.RenderResource(_currentPath, _simCols, _simRows);
            var snap = FrameSnapshot.Capture(r.Frame, 0, 0, r.Cols, r.Rows);
            Grid.SetGrid(snap);
            SizeLabel.Text = $"{r.Cols}x{r.Rows}";
            Title = $"WayCoder .tui 预览 — {_currentPath}";
            Status.Content = $"{r.Cols}×{r.Rows}  ·  {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            Grid.SetGrid(null);
            Status.Content = $"[渲染错误] {ex.GetType().Name}: {ex.Message}";
        }
    }

    private void StartWatch(string source)
    {
        _watcher?.Dispose();
        // 资源名→真实文件（文件系统 Raw/ 命中才可监视；纯嵌入资源无文件，跳过热刷新）
        var real = File.Exists(source) ? source : TuiMarkupPaths.TryResolveFile(source);
        if (real == null) return;
        var dir = Path.GetDirectoryName(Path.GetFullPath(real))!;
        var fname = Path.GetFileName(real);
        _watcher = new FileSystemWatcher(dir, fname)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };
        _watcher.Changed += (_, _) => _debounce?.Change(300, System.Threading.Timeout.Infinite);
        _watcher.Renamed += (_, _) => _debounce?.Change(300, System.Threading.Timeout.Infinite);
    }

    protected override void OnClosed(EventArgs e)
    {
        _watcher?.Dispose();
        _debounce?.Dispose();
        base.OnClosed(e);
    }
}
