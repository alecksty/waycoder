using System.Text;
using WayCoder.Terminal;
using WayCoder.Tools;
using WayCoder.UI.TuiControls;

namespace WayCoder.UI.TuiScreens;

/// <summary>
/// 编辑器屏幕 —— 终端内源码编辑器，与 ChatScreen 平级。
///
/// 布局：
///   RootView (VBox)
///   ├─ TitleBar      TuiLabel      " /edit: filename.cs [已修改] "
///   ├─ EditorView    TuiRichEditor  编辑区域（TH-4 行）
///   ├─ StatusBar1    TuiLabel      " L1:C10 | 行:42 字符:2048 | C# · UTF-8"
///   └─ StatusBar2    TuiLabel      " ^S保存 ^Z撤销 ^G跳行 Esc退出"
///
/// 生命周期：
///   Activate() → 有路径: LoadAndBuild / 无路径: ShowFilePicker（不阻塞）
///   OnKey → 模态窗口优先 → 路由 EditorView → 未处理回退基类
///   OnResize  → 重建布局 + 重新绑定事件
///   Deactivate → 基础清理
/// </summary>
public class EditorScreen : TuiScreen
{
    // ── 组件 ──
    public EditorCore Core { get; private set; } = null!;
    public TuiRichEditor EditorView { get; private set; } = null!;
    public TuiTitleBar TitleBar { get; private set; } = null!;
    public TuiLabel StatusBar1 { get; private set; } = null!;
    public TuiLabel StatusBar2 { get; private set; } = null!;

    // ── 侧边栏 ──
    private TuiListView _leftPanel = null!;    // 文件列表
    private TuiListView _rightPanel = null!;   // 代码大纲
    private TuiLabel _leftSep = null!;         // 左侧分隔线
    private TuiLabel _rightSep = null!;        // 右侧分隔线
    private bool _leftVisible;
    private bool _rightVisible;

    /// <summary>焦点目标区域</summary>
    private enum FocusTarget { Editor, LeftPanel, RightPanel }
    private FocusTarget _focus = FocusTarget.Editor;

    /// <summary>当前浏览的目录（文件列表面板）</summary>
    private string _browseDir = "";

    private Action? _onContentChangedHandler;

    /// <summary>要编辑的文件路径（空 = 弹出文件选择器）</summary>
    public string FilePath { get; set; }

    /// <summary>退出前是否已保存</summary>
    public bool WasSaved { get; private set; }

    public EditorScreen(string filePath = "")
    {
        Name = "editor";
        FilePath = filePath;
    }

    // ════════════════════════════════════════════════════════════════
    // 生命周期
    // ════════════════════════════════════════════════════════════════

    public override void Activate()
    {
        base.Activate();

        if (string.IsNullOrWhiteSpace(FilePath))
            ShowFilePicker();   // 纯回调驱动，不阻塞
        else
            LoadAndBuild(FilePath);
    }

    public override void OnResize(int newW, int newH)
    {
        base.OnResize(newW, newH);

        if (EditorView == null) return;

        int mainH = Math.Max(5, TH - 4);
        int sideW = Math.Min(25, TW / 4);

        TitleBar.Width = TW;

        _leftPanel.Width = sideW;
        _leftPanel.Height = mainH;
        _leftSep.Height = mainH;
        _leftSep.Visible = _leftVisible;

        _rightPanel.Width = sideW;
        _rightPanel.Height = mainH;
        _rightSep.Height = mainH;
        _rightSep.Visible = _rightVisible;

        EditorView.Height = mainH;
        EditorView.Width = Math.Max(20, TW - (_leftVisible ? sideW + 1 : 0) - (_rightVisible ? sideW + 1 : 0));

        StatusBar1.Width = TW;
        StatusBar2.Width = TW;

        RootView.Layout();
        MarkDirty();
    }

    public override void Deactivate()
    {
        base.Deactivate();
    }

    // ════════════════════════════════════════════════════════════════
    // 文件加载 + 布局构建
    // ════════════════════════════════════════════════════════════════

    private void LoadAndBuild(string path)
    {
        Core = new EditorCore();
        Core.LoadFile(path);
        _onContentChangedHandler = () => MarkDirty();
        Core.OnContentChanged += _onContentChangedHandler;
        BuildLayout();
    }

    private void BuildLayout()
    {
        RootView = new TuiVBox { Width = TW, Height = TH };

        // ── 标题栏 ──
        TitleBar = new TuiTitleBar { Width = TW, Height = 1, Bg = 33, Fg = 30, CenterText = "📝 编辑器" };
        RootView.Add(TitleBar);

        // ── 主区域：LeftPanel + EditorView + RightPanel（HBox）──
        int mainH = Math.Max(5, TH - 4); // -title(1) -status1(1) -status2(1) - spare(1)
        var mainHBox = new TuiHBox { Width = TW, Height = mainH };

        // 左侧面板宽度
        int leftW = Math.Min(25, TW / 4);

        // ── 左侧：文件列表面板 ──
        _leftSep = new TuiLabel("│") { Width = 1, Height = mainH, Fg = TuiTheme.Current.SeparatorFg, Visible = false };
        _leftPanel = new TuiListView
        {
            Width = leftW, Height = mainH,
            SelBg = TuiTheme.Current.ListSelBg,
            SelFg = TuiTheme.Current.ListSelFg,
            Visible = false
        };
        _leftPanel.OnItemActivated += OnFileItemActivated;
        mainHBox.Add(_leftPanel);
        mainHBox.Add(_leftSep);

        // ── 中间：编辑区（剩余宽度）──
        int editorW = TW - (leftW + 1) - (leftW + 1); // 初始假设两侧都开
        EditorView = new TuiRichEditor
        {
            Core = Core,
            Width = Math.Max(20, editorW),
            Height = mainH,
            Focused = true
        };
        EditorView.OnSaveRequested += HandleSave;
        EditorView.OnJumpRequested += HandleJump;
        EditorView.OnExitRequested += HandleExit;
        mainHBox.Add(EditorView);

        // ── 右侧：大纲面板 ──
        _rightSep = new TuiLabel("│") { Width = 1, Height = mainH, Fg = TuiTheme.Current.SeparatorFg, Visible = false };
        _rightPanel = new TuiListView
        {
            Width = leftW, Height = mainH,
            SelBg = TuiTheme.Current.ListSelBg,
            SelFg = TuiTheme.Current.ListSelFg,
            Visible = false
        };
        _rightPanel.OnItemActivated += OnOutlineItemActivated;
        mainHBox.Add(_rightSep);
        mainHBox.Add(_rightPanel);

        RootView.Add(mainHBox);

        // ── 状态栏 1 — 光标 + 统计 + 诊断 ──
        StatusBar1 = new TuiLabel("") { Width = TW, Height = 1, Bg = 47 };
        RootView.Add(StatusBar1);

        // ── 状态栏 2 — 文件路径 + 快捷键 ──
        StatusBar2 = new TuiLabel("") { Width = TW, Height = 1 };
        RootView.Add(StatusBar2);

        RootView.Layout();
        MarkDirty();
    }

    /// <summary>屏幕销毁时取消所有事件订阅，避免泄漏</summary>
    public override void OnDestroy()
    {
        if (Core != null && _onContentChangedHandler != null)
            Core.OnContentChanged -= _onContentChangedHandler;
        if (EditorView != null)
        {
            EditorView.OnSaveRequested -= HandleSave;
            EditorView.OnJumpRequested -= HandleJump;
            EditorView.OnExitRequested -= HandleExit;
        }
        if (_leftPanel != null)
            _leftPanel.OnItemActivated -= OnFileItemActivated;
        if (_rightPanel != null)
            _rightPanel.OnItemActivated -= OnOutlineItemActivated;
        base.OnDestroy();
    }

    // ════════════════════════════════════════════════════════════════
    // 渲染
    // ════════════════════════════════════════════════════════════════

    public override void Render(StringBuilder sb)
    {
        if (EditorView == null) { base.Render(sb); return; }

        UpdateStatusBars();
        SyncPanelLayout();

        // 刷新侧边栏内容（按需）
        if (_leftVisible && _leftPanel.ItemCount == 0)
            RefreshFileList();
        if (_rightVisible)
            RefreshOutline();

        base.Render(sb);
    }

    /// <summary>同步面板布局（可见性 + 尺寸）</summary>
    private void SyncPanelLayout()
    {
        int mainH = Math.Max(5, TH - 4);
        int sideW = Math.Min(25, TW / 4);

        _leftPanel.Width = sideW;
        _leftPanel.Height = mainH;
        _leftPanel.Visible = _leftVisible;
        _leftSep.Visible = _leftVisible;

        _rightPanel.Width = sideW;
        _rightPanel.Height = mainH;
        _rightPanel.Visible = _rightVisible;
        _rightSep.Visible = _rightVisible;

        EditorView.Width = Math.Max(20, TW - (_leftVisible ? sideW + 1 : 0) - (_rightVisible ? sideW + 1 : 0));
        EditorView.Height = mainH;

        // 焦点高亮
        _leftPanel.SelBg = _focus == FocusTarget.LeftPanel ? 33 : TuiTheme.Current.ListSelBg;
        _rightPanel.SelBg = _focus == FocusTarget.RightPanel ? 33 : TuiTheme.Current.ListSelBg;
    }

    private void UpdateStatusBars()
    {
        var fileName = Path.GetFileName(Core.FilePath);
        var title = $" /edit: {fileName} ";
        if (Core.Modified) title += "[已修改] ";
        TitleBar.CenterText = title;

        var (errors, warnings) = Core.GetDiagSummary();
        var diagPart = "";
        if (errors > 0) diagPart = $" | {AnsiTty.Fg(31)}● {errors} errors";
        else if (warnings > 0) diagPart = $" | {AnsiTty.Fg(33)}▲ {warnings} warnings";

        StatusBar1.Text = $" L{Core.Cy + 1}:C{Core.Cx + 1} | " +
                          $"行:{Core.TotalLines} 字符:{Core.TotalChars} | " +
                          $"{EditorCore.FormatSize(Core.FileSizeBytes)} | " +
                          $"{Core.Syntax.Name} · UTF-8{diagPart}";

        var pathDisplay = Core.FilePath;
        if (pathDisplay.Length > 50) pathDisplay = "..." + pathDisplay[^47..];
        StatusBar2.Text = $" {pathDisplay}  " +
                          "^S保存 ^Z撤销 ^G跳行 ^B文件 ^O大纲 Tab切焦点 Esc退出";
    }

    // ════════════════════════════════════════════════════════════════
    // 键盘路由
    // ════════════════════════════════════════════════════════════════

    public override bool OnKey(ConsoleKeyInfo key)
    {
        // 模态窗口优先
        if (HasModal)
            return base.OnKey(key);

        if (EditorView == null)
            return base.OnKey(key);

        // ── 全局快捷键（Ctrl+key）──
        if (key.Modifiers == ConsoleModifiers.Control)
        {
            switch (key.Key)
            {
                case ConsoleKey.B:
                    ToggleLeftPanel();
                    return true;
                case ConsoleKey.O:
                    ToggleRightPanel();
                    return true;
                case ConsoleKey.S:
                    HandleSave();
                    return true;
                case ConsoleKey.Z:
                    Core.Undo();
                    MarkDirty();
                    return true;
                case ConsoleKey.G:
                    HandleJump();
                    return true;
                case ConsoleKey.X:
                    Core.CutLine();
                    MarkDirty();
                    return true;
                case ConsoleKey.C:
                    Core.CopyLine();
                    ShowToast("已复制", 800);
                    return true;
                case ConsoleKey.V:
                    Core.PasteClipboard();
                    MarkDirty();
                    return true;
            }
        }

        // ── Tab 切换焦点 ──
        if (key.Key == ConsoleKey.Tab)
        {
            CycleFocus();
            return true;
        }

        // ── 焦点路由 ──
        switch (_focus)
        {
            case FocusTarget.LeftPanel:
                if (key.Key == ConsoleKey.Escape)
                {
                    _focus = FocusTarget.Editor;
                    MarkDirty();
                    return true;
                }
                return _leftPanel.OnKey(key) || base.OnKey(key);

            case FocusTarget.RightPanel:
                if (key.Key == ConsoleKey.Escape)
                {
                    _focus = FocusTarget.Editor;
                    MarkDirty();
                    return true;
                }
                return _rightPanel.OnKey(key) || base.OnKey(key);

            default: // Editor
                if (key.Key == ConsoleKey.Escape)
                {
                    HandleExit();
                    return true;
                }
                return EditorView.OnKey(key) || base.OnKey(key);
        }
    }

    // ════════════════════════════════════════════════════════════════
    // 焦点管理
    // ════════════════════════════════════════════════════════════════

    private void CycleFocus()
    {
        // 只在可见面板间循环：Editor → Left → Right → Editor
        var options = new List<FocusTarget> { FocusTarget.Editor };
        if (_leftVisible) options.Add(FocusTarget.LeftPanel);
        if (_rightVisible) options.Add(FocusTarget.RightPanel);

        int idx = options.IndexOf(_focus);
        _focus = options[(idx + 1) % options.Count];

        EditorView.Focused = _focus == FocusTarget.Editor;
        _leftPanel.Focused = _focus == FocusTarget.LeftPanel;
        _rightPanel.Focused = _focus == FocusTarget.RightPanel;
        MarkDirty();
    }

    // ════════════════════════════════════════════════════════════════
    // 左侧面板：文件列表
    // ════════════════════════════════════════════════════════════════

    private void ToggleLeftPanel()
    {
        _leftVisible = !_leftVisible;
        if (_leftVisible)
        {
            _browseDir = Path.GetDirectoryName(Core.FilePath) ?? ".";
            RefreshFileList();
            _focus = FocusTarget.LeftPanel;
            _leftPanel.Focused = true;
            EditorView.Focused = false;
        }
        else
        {
            if (_focus == FocusTarget.LeftPanel)
            {
                _focus = FocusTarget.Editor;
                EditorView.Focused = true;
                _leftPanel.Focused = false;
            }
        }
        MarkDirty();
    }

    private void RefreshFileList()
    {
        _leftPanel.ClearItems();
        if (string.IsNullOrEmpty(_browseDir) || !Directory.Exists(_browseDir))
        {
            _browseDir = Path.GetDirectoryName(Core.FilePath) ?? ".";
            if (!Directory.Exists(_browseDir)) return;
        }

        var fileColor = TuiTheme.Current.ControlFg;
        var dirColor = TuiColors.Cyan;
        int selBg = TuiTheme.Current.ListSelBg;

        // 上级目录
        var parentDir = Path.GetDirectoryName(_browseDir);
        if (!string.IsNullOrEmpty(parentDir) && Directory.Exists(parentDir))
        {
            _leftPanel.AddItem(new TuiLabel($" 📁 ..  ") { Height = 1, Bg = 0, Fg = dirColor });
        }

        // 目录
        var dirs = Directory.GetDirectories(_browseDir);
        Array.Sort(dirs);
        foreach (var d in dirs)
        {
            var name = Path.GetFileName(d);
            _leftPanel.AddItem(new TuiLabel($" 📁 {name}") { Height = 1, Bg = 0, Fg = dirColor });
        }

        // 文件（仅显示源码文件，过滤二进制和大型文件）
        var files = Directory.GetFiles(_browseDir);
        Array.Sort(files);
        var codeExts = new HashSet<string> { ".cs", ".py", ".js", ".ts", ".jsx", ".tsx", ".go", ".rs",
            ".java", ".c", ".cpp", ".h", ".hpp", ".swift", ".kt", ".rb", ".php", ".lua",
            ".dart", ".r", ".sql", ".md", ".txt", ".json", ".xml", ".html", ".css",
            ".scss", ".yaml", ".yml", ".sh", ".bash", ".zsh", ".toml", ".ini", ".cfg",
            ".csproj", ".sln", ".props", ".targets", ".svg", ".gitignore", "Dockerfile" };
        foreach (var f in files)
        {
            var name = Path.GetFileName(f);
            var ext = Path.GetExtension(f).ToLowerInvariant();
            // 跳过二进制和隐藏文件
            if (name.StartsWith('.')) continue;
            if (!codeExts.Contains(ext) && ext.Length > 0 && ext.Length <= 5) continue;

            var isCurrent = string.Equals(f, Core.FilePath, StringComparison.OrdinalIgnoreCase);
            var prefix = isCurrent ? "◀ " : "  ";
            var itemFg = isCurrent ? TuiColors.Yellow : fileColor;
            _leftPanel.AddItem(new TuiLabel($"{prefix}📄 {name}") { Height = 1, Bg = 0, Fg = itemFg });
        }
    }

    private void OnFileItemActivated(int index)
    {
        var child = _leftPanel.Children[index];
        if (child is not TuiLabel label) return;
        var text = label.Text.Trim();

        // 上级目录
        if (text == "📁 .." || text == "📁 ..")
        {
            _browseDir = Path.GetDirectoryName(_browseDir) ?? _browseDir;
            RefreshFileList();
            return;
        }

        // 提取名称（去掉图标前缀）
        var name = text.StartsWith("📁") ? text[3..] : text.StartsWith("◀ 📄") ? text[5..] : text.StartsWith("📄") ? text[3..] : text;
        name = name.Trim();

        var fullPath = Path.Combine(_browseDir, name);

        if (Directory.Exists(fullPath))
        {
            _browseDir = fullPath;
            RefreshFileList();
            return;
        }

        if (File.Exists(fullPath))
        {
            // 保存当前文件
            if (Core.Modified)
            {
                try { Core.Save(); }
                catch (Exception ex) { ShowWindow(TuiDialog.Error("保存失败", ex.Message)); return; }
            }
            // 卸载旧事件
            if (_onContentChangedHandler != null)
                Core.OnContentChanged -= _onContentChangedHandler;
            // 加载新文件
            Core.LoadFile(fullPath);
            _onContentChangedHandler = () => MarkDirty();
            Core.OnContentChanged += _onContentChangedHandler;
            ShowToast($"已打开: {name}", 1200);
            MarkDirty();
        }
    }

    // ════════════════════════════════════════════════════════════════
    // 右侧面板：代码大纲
    // ════════════════════════════════════════════════════════════════

    private void ToggleRightPanel()
    {
        _rightVisible = !_rightVisible;
        if (_rightVisible)
        {
            RefreshOutline();
            _focus = FocusTarget.RightPanel;
            _rightPanel.Focused = true;
            EditorView.Focused = false;
        }
        else
        {
            if (_focus == FocusTarget.RightPanel)
            {
                _focus = FocusTarget.Editor;
                EditorView.Focused = true;
                _rightPanel.Focused = false;
            }
        }
        MarkDirty();
    }

    private void RefreshOutline()
    {
        _rightPanel.ClearItems();
        var items = Core.ExtractOutline();
        if (items.Count == 0)
        {
            _rightPanel.AddItem(new TuiLabel(" (无符号)")
                { Height = 1, Bg = 0, Fg = TuiColors.BrightBlack });
            return;
        }

        var fg = TuiTheme.Current.ControlFg;
        foreach (var item in items)
        {
            var indent = item.Kind switch
            {
                "method" or "function" => "  ",
                "property" or "variable" => "    ",
                _ => "",
            };
            var display = $"{indent}{item.Icon} {item.Name}";
            // 右对齐行号
            var lineStr = item.Line.ToString();
            var padding = Math.Max(1, 5 - lineStr.Length);
            display += new string(' ', padding) + ":" + lineStr;

            // 当前光标所在函数高亮
            var itemFg = (Core.Cy + 1 >= item.Line && item.Kind is "method" or "function" or "class") ? TuiColors.Yellow : fg;
            _rightPanel.AddItem(new TuiLabel(display) { Height = 1, Bg = 0, Fg = itemFg });
        }
    }

    private void OnOutlineItemActivated(int index)
    {
        var items = Core.ExtractOutline();
        if (index < 0 || index >= items.Count) return;
        var item = items[index];
        Core.JumpToLine(item.Line);
        _focus = FocusTarget.Editor;
        EditorView.Focused = true;
        _rightPanel.Focused = false;
        MarkDirty();
    }

    // ════════════════════════════════════════════════════════════════
    // 事件处理
    // ════════════════════════════════════════════════════════════════

    private void HandleSave()
    {
        try
        {
            Core.Save();
            _ = Core.SaveAsync();   // 异步触发 lint
            WasSaved = true;
            ShowToast("已保存", 1200);
        }
        catch (Exception ex)
        {
            ShowWindow(TuiDialog.Error("保存失败", ex.Message));
        }
    }

    private void HandleJump()
    {
        var win = TuiDialog.Input("跳转到行",
            $"输入行号 (1-{Core.TotalLines})",
            (Core.Cy + 1).ToString(),
            input =>
            {
                if (int.TryParse(input, out var ln) && ln >= 1 && ln <= Core.TotalLines)
                    if (Core.JumpToLine(ln))
                        MarkDirty();
            });
        ShowWindow(win);
    }

    private void HandleExit()
    {
        if (!Core.Modified)
        {
            Manager?.PopScreen();
            return;
        }

        var win = TuiDialog.Confirm3("文件已修改",
            "是否保存更改？",
            result =>
            {
                switch (result)
                {
                    case TuiDialog.DialogResult.Yes:
                        try { Core.Save(); WasSaved = true; }
                        catch (Exception ex)
                        {
                            ShowWindow(TuiDialog.Error("保存失败", ex.Message));
                            return;
                        }
                        Manager?.PopScreen();
                        break;
                    case TuiDialog.DialogResult.No:
                        Manager?.PopScreen();
                        break;
                    // Cancel → 继续编辑
                }
            });
        ShowWindow(win);
    }

    // ════════════════════════════════════════════════════════════════
    // 文件选择器（纯回调驱动）
    // ════════════════════════════════════════════════════════════════

    private void ShowFilePicker()
    {
        var recent = EditFileTool.ChangedFiles.Take(9).ToList();
        var choices = new List<string> { "📝 输入文件路径..." };
        if (recent.Count > 0)
        {
            choices.Add("── 最近编辑 ──");
            choices.AddRange(recent);
        }

        var selectWin = TuiDialog.Select("选择要编辑的文件", choices, idx =>
        {
            if (choices[idx].StartsWith("──"))
                return;

            if (idx == 0)
            {
                var inputWin = TuiDialog.Input("文件路径",
                    "输入要编辑的文件路径（相对或绝对路径）", "",
                    path =>
                    {
                        var trimmed = path?.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                            LoadAndBuild(trimmed);
                        else
                            Manager?.PopScreen();
                    });
                ShowWindow(inputWin);
            }
            else
            {
                var file = recent[idx - (recent.Count > 0 ? 2 : 1)];
                LoadAndBuild(file);
            }
        });

        selectWin.OnClosed = () =>
        {
            if (Core == null) Manager?.PopScreen();
        };
        ShowWindow(selectWin);
    }
}
