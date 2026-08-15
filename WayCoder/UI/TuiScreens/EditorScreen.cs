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
///   ├─ StatusBar1    TuiLabel      " L1:C10 | 行:42 字节:2048 | C# · UTF-8"
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
    private Action? _onDiagnosticsReadyHandler;

    /// <summary>搜索状态</summary>
    private string _searchQuery = "";
    private string _replaceQuery = "";
    private FindOptions _findOptions = default;

    /// <summary>大纲刷新缓存（避免每帧重建 ListView）</summary>
    private List<EditorCore.OutlineItem>? _lastOutline;
    private int _lastOutlineHighlight = -1;

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
        Core.IndentMode = Config.Instance.EditorIndent;   // 从设置注入缩进模式
        _onContentChangedHandler = () => MarkDirty();
        Core.OnContentChanged += _onContentChangedHandler;
        _onDiagnosticsReadyHandler = () => MarkDirty();
        Core.OnDiagnosticsReady += _onDiagnosticsReadyHandler;
        _lastOutline = null;
        _lastOutlineHighlight = -1;
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
        EditorView.OnFindRequested += HandleFindReplace;
        EditorView.OnExitRequested += HandleExit;
        EditorView.OnFocusRequested += HandleEditorFocus;
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
        if (Core != null)
        {
            if (_onContentChangedHandler != null)
                Core.OnContentChanged -= _onContentChangedHandler;
            if (_onDiagnosticsReadyHandler != null)
                Core.OnDiagnosticsReady -= _onDiagnosticsReadyHandler;
        }
        if (EditorView != null)
        {
            EditorView.OnSaveRequested -= HandleSave;
            EditorView.OnJumpRequested -= HandleJump;
            EditorView.OnFindRequested -= HandleFindReplace;
            EditorView.OnExitRequested -= HandleExit;
            EditorView.OnFocusRequested -= HandleEditorFocus;
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
                          $"行:{Core.TotalLines} 字节:{Core.FileSizeBytes} | " +
                          $"{Core.Syntax.Name} · UTF-8{diagPart}";

        var pathDisplay = Core.FilePath;
        if (pathDisplay.Length > 50) pathDisplay = "..." + pathDisplay[^47..];
        StatusBar2.Text = $" {pathDisplay}  " +
                          "^S保存 ^Z撤销 ^F查找/替换 F3下一处 F8诊断 Tab缩进 ^Tab焦点 ^B文件 ^⇧O大纲 Esc退出";
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

        bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);
        bool shift = key.Modifiers.HasFlag(ConsoleModifiers.Shift);

        // ── 全局快捷键（Ctrl+key）──
        if (ctrl)
        {
            switch (key.Key)
            {
                case ConsoleKey.B:
                    ToggleLeftPanel();
                    return true;
                case ConsoleKey.O when shift:
                    ToggleRightPanel();
                    return true;
                case ConsoleKey.S:
                    HandleSave();
                    return true;
                case ConsoleKey.G:
                    HandleJump();
                    return true;
                case ConsoleKey.F when shift:
                    SearchWordAtCursor();
                    return true;
                case ConsoleKey.F:
                    HandleFindReplace();
                    return true;
                case ConsoleKey.H:
                    HandleFindReplace();
                    return true;
                case ConsoleKey.P:
                    JumpToMatchingBracket();
                    return true;
            }
        }

        // ── F3 下一处匹配 · F8 下一处诊断（编辑区通用）──
        if (key.Key == ConsoleKey.F3)
        {
            HandleFindNext();
            return true;
        }
        if (key.Key == ConsoleKey.F8)
        {
            JumpToNextDiagnostic();
            return true;
        }

        // ── Ctrl+Tab / Ctrl+Shift+Tab 切换焦点（裸 Tab 交给编辑器插 4 空格）──
        if (key.Key == ConsoleKey.Tab && ctrl)
        {
            CycleFocus(shift ? -1 : 1);
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

    /// <summary>鼠标点击编辑区时把焦点切回编辑区。</summary>
    private void HandleEditorFocus()
    {
        _focus = FocusTarget.Editor;
        EditorView.Focused = true;
        _leftPanel.Focused = false;
        _rightPanel.Focused = false;
        MarkDirty();
    }

    private void CycleFocus(int dir = 1)
    {
        // 只在可见面板间循环：Editor → Left → Right → Editor（dir=-1 反向）
        var options = new List<FocusTarget> { FocusTarget.Editor };
        if (_leftVisible) options.Add(FocusTarget.LeftPanel);
        if (_rightVisible) options.Add(FocusTarget.RightPanel);

        int idx = options.IndexOf(_focus);
        if (idx < 0) idx = 0;
        _focus = options[(idx + dir + options.Count) % options.Count];

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
            EditorView.MarkFullRedraw();
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
        var items = Core.ExtractOutline();

        // 当前光标所在函数/类（最后一个 Line ≤ 光标行的容器）
        int currentIdx = -1;
        for (int i = 0; i < items.Count; i++)
            if (items[i].Kind is "method" or "function" or "class" && Core.Cy + 1 >= items[i].Line)
                currentIdx = i;

        // 内容与高亮位置均未变则跳过（避免每帧重建 ListView）
        if (ReferenceEquals(items, _lastOutline) && currentIdx == _lastOutlineHighlight)
            return;

        _lastOutline = items;
        _lastOutlineHighlight = currentIdx;

        _rightPanel.ClearItems();
        if (items.Count == 0)
        {
            _rightPanel.AddItem(new TuiLabel(" (无符号)")
                { Height = 1, Bg = 0, Fg = TuiColors.BrightBlack });
            return;
        }

        var fg = TuiTheme.Current.ControlFg;
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
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

            // 仅当前光标所在函数高亮
            var itemFg = i == currentIdx ? TuiColors.Yellow : fg;
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
        EditorView.MarkFullRedraw();
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
        var win = TuiDialog.Input("跳转到 行:列",
            $"输入行号或 行:列 (1-{Core.TotalLines})",
            (Core.Cy + 1).ToString(),
            input =>
            {
                var target = EditorCore.ParseLineCol(input ?? "", Core.Cy, Core.TotalLines);
                if (target == null || !Core.JumpToLineCol(target.Value.Line, target.Value.Col))
                    ShowToast("无效位置", 1000);
                else
                {
                    EditorView.MarkFullRedraw();
                    MarkDirty();
                }
            });
        ShowWindow(win);
    }

    // ════════════════════════════════════════════════════════════════
    // 搜索 / 替换 / 诊断跳转
    // ════════════════════════════════════════════════════════════════

    private void HandleFindReplace()
    {
        var win = TuiDialog.FindReplace(_searchQuery, _replaceQuery, _findOptions,
            (find, opts) =>
            {
                _searchQuery = find; _findOptions = opts;
                FindAndJump(find, opts, Core.Cy, Core.Cx);
            },
            (find, repl, opts) =>
            {
                _searchQuery = find; _replaceQuery = repl; _findOptions = opts;
                ReplaceNextAndShow(find, repl, opts);
            },
            (find, repl, opts) =>
            {
                _searchQuery = find; _replaceQuery = repl; _findOptions = opts;
                ReplaceAllAndShow(find, repl, opts);
            });
        ShowWindow(win);
    }

    private void HandleFindNext()
    {
        if (string.IsNullOrEmpty(_searchQuery)) { HandleFindReplace(); return; }
        FindAndJump(_searchQuery, _findOptions, Core.Cy, Core.Cx + 1);
    }

    private void FindAndJump(string query, FindOptions opts, int fromLine, int fromCol)
    {
        var (line, col) = Core.FindNext(query, fromLine, fromCol, opts);
        if (line >= 0)
        {
            Core.Cy = line; Core.Cx = col;
            EditorView.MarkFullRedraw();
            MarkDirty();
            ShowToast($"已找到 · 第 {line + 1} 行", 1000);
            return;
        }
        // 环绕：从头再找
        var (l2, c2) = Core.FindNext(query, 0, 0, opts);
        if (l2 >= 0)
        {
            Core.Cy = l2; Core.Cx = c2;
            EditorView.MarkFullRedraw();
            MarkDirty();
            ShowToast($"已环绕到开头 · 第 {l2 + 1} 行", 1000);
        }
        else ShowToast("未找到", 1000);
    }

    private void ReplaceNextAndShow(string find, string replace, FindOptions opts)
    {
        if (string.IsNullOrWhiteSpace(find)) return;
        if (Core.ReplaceNext(find, replace, opts))
        {
            MarkDirty();
            ShowToast("已替换", 800);
        }
        else ShowToast("未找到", 1000);
    }

    private void ReplaceAllAndShow(string find, string replace, FindOptions opts)
    {
        if (string.IsNullOrWhiteSpace(find)) return;
        int count = Core.ReplaceAll(find, replace, opts);
        MarkDirty();
        ShowToast($"已替换 {count} 处", 1500);
    }

    /// <summary>跳到光标处括号的配对括号（Ctrl+P）。</summary>
    private void JumpToMatchingBracket()
    {
        var match = Core.MatchingBracketAt(Core.Cy, Core.Cx);
        if (match == null)
        {
            ShowToast("光标处无括号", 1000);
            return;
        }
        Core.Cy = match.Value.Line;
        Core.Cx = match.Value.Col;
        EditorView.MarkFullRedraw();
        MarkDirty();
        ShowToast($"已跳到配对括号 · 第 {match.Value.Line + 1} 行", 1000);
    }

    /// <summary>搜索光标处的标识符词（Ctrl+Shift+F），整词匹配 + 智能大小写。</summary>
    private void SearchWordAtCursor()
    {
        string word = Core.WordAt(Core.Cy, Core.Cx);
        if (word.Length == 0)
        {
            ShowToast("光标处无词", 1000);
            return;
        }
        _searchQuery = word;
        // 智能大小写：词含大写则区分，否则忽略（对标 Vim * 的 smartcase）
        bool hasUpper = word.Any(char.IsUpper);
        _findOptions = new FindOptions(CaseSensitive: hasUpper, WholeWord: true);

        // 从光标后一位查找，避免停在当前词；未命中则从头环绕
        var (line, col) = Core.FindNext(word, Core.Cy, Core.Cx + 1, _findOptions);
        if (line < 0)
            (line, col) = Core.FindNext(word, 0, 0, _findOptions);

        if (line >= 0)
        {
            Core.Cy = line; Core.Cx = col;
            EditorView.MarkFullRedraw();
            MarkDirty();
            ShowToast($"查找词 · 第 {line + 1} 行", 1000);
        }
        else ShowToast("未找到", 1000);
    }

    private void JumpToNextDiagnostic()
    {
        int total = Core.TotalLines;
        if (total <= 0) return;
        for (int i = 1; i <= total; i++)
        {
            int li = (Core.Cy + i) % total;
            if (Core.GetDiagnosticsAtLine(li + 1).Count > 0)
            {
                Core.JumpToLine(li + 1);
                EditorView.MarkFullRedraw();
                MarkDirty();
                ShowToast($"诊断 · 第 {li + 1} 行", 1000);
                return;
            }
        }
        ShowToast("无诊断", 800);
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
        choices.AddRange(recent);

        var selectWin = TuiDialog.Select("选择要编辑的文件", choices, idx =>
        {
            if (idx == 0)
            {
                // 路径是单行输入，用 InputLine（回车即确定）而非多行 Input（回车=换行）。
                var inputWin = TuiDialog.InputLine("文件路径",
                    "输入要编辑的文件路径（相对或绝对路径）", "",
                    path =>
                    {
                        var trimmed = path?.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                            LoadAndBuild(trimmed);
                        else
                            Manager?.PopScreen();
                    },
                    onCancel: () => Manager?.PopScreen());
                ShowWindow(inputWin);
            }
            else
            {
                LoadAndBuild(recent[idx - 1]);
            }
        });

        selectWin.OnClosed = () =>
        {
            // 仅用户取消（Esc/取消按钮，Result=-1）才退回上层屏幕。
            // 选择「📝 输入文件路径...」时 Result=0 且输入框已接管、Core 仍为空，
            // 不能在这里 PopScreen，否则会把整屏连同刚弹出的输入框一起退掉。
            if (Core == null && selectWin.Result is int r && r == -1)
                Manager?.PopScreen();
        };
        ShowWindow(selectWin);
    }
}
