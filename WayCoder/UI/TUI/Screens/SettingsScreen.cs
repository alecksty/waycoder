using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Custom;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Tui.Screens;

/// <summary>
/// 设置屏幕 —— 配置编辑器，与 ChatScreen 平级。
///
/// 布局：
///   RootView (VBox)
///   ├─ Header        TuiLabel       "⚙ 设置 / 配置"
///   ├─ MainArea      TuiHBox
///   │   ├─ CatList   TuiList        类别列表
///   │   ├─ Separator TuiSeparator   竖分隔线
///   │   └─ Detail    TuiVBox        设置项详情（3 行/项: 标签 + 值 + 描述）
///   └─ HintBar       TuiLabel       快捷键提示
///
/// 键盘：
///   ↑↓     → 浏览设置项（跨类别自动切换）
///   ←→     → 切换焦点面板（类别 ↔ 详情）
///   PgUp/Dn → 快速翻页
///   Enter   → 编辑当前设置值（select → 下拉 / text → 输入）
///   Ctrl+S  → 保存到 .env + Toast
///   Esc     → 返回聊天屏幕
/// </summary>
public class SettingsScreen : TuiScreen
{
    // ── 数据 ──
    private Dictionary<string, List<SettingDef>> _groups = [];
    private string[] _catOrder = [];
    private int _catIdx;
    private int _itemIdx;
    private Config _config = null!;
    private bool _focusOnDetail;

    /// <summary>
    /// 就地展开的选项栏（select 类型设置项；null = 不在选择态）。
    /// 用 <see cref="TuiPromptBar"/> —— 与聊天界面的行内选择同一控件（❯ 箭头 + 黄底高亮 + 每项一行说明），
    /// 符合「设置也不弹窗、纯文字交互」：选项就展开在被编辑项下方，而不是另开一个窗口。
    /// </summary>
    private TuiPromptBar? _optionBar;

    /// <summary>选项栏对应的设置项 key（应用时按它写值；不用 SettingDef 引用以免列表重建后指向旧对象）</summary>
    private string? _optionKey;

    // ── 控件引用 ──
    private TuiTitleBar _header = null!;
    private Controls.TuiList _catList = null!;
    private TuiScrollView _detailPanel = null!;
    private TuiLabel _hintBar = null!;
    private readonly List<TuiControl> _detailControls = [];   // 每组 3 个: label, value, desc
    private TuiMarkupResult? _markup;                        // 缓存的 settings.tui 标记树（仅首次解析）

    public SettingsScreen()
    {
        Name = "settings";
    }

    // ════════════════════════════════════════════════════════════════
    // 生命周期
    // ════════════════════════════════════════════════════════════════
    public override void Activate()
    {
        base.Activate();

        _config = Config.Instance;

        var schema = Config.SettingSchema();
        _groups = schema.GroupBy(s => s.Category)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Order).ToList());
        _catOrder = [.. schema.Select(s => s.Category).Distinct()];
        _catIdx = 0;
        _itemIdx = 0;
        _focusOnDetail = false;

        BuildLayout();
    }

    public override void OnResize(int newW, int newH)
    {
        base.OnResize(newW, newH);
        // 重建布局（类别列表宽度、详情面板宽度随终端变化）
        if (_catList != null)
            BuildLayout();
    }

    public override void Deactivate()
    {
        base.Deactivate();
        var cs = Manager?.ActiveScreen as ChatScreen;
        cs?.SyncTheme();
    }

    // ════════════════════════════════════════════════════════════════
    // 布局
    // ════════════════════════════════════════════════════════════════
    private void BuildLayout()
    {
        // 标记加载：结构/ids 来自 settings.tui（布局写标记），schema 数据/高亮/交互 code-behind
        if (_markup == null)
        {
            _markup = TuiMarkup.LoadResource("dialogs/settings.tui");
            _header = _markup.Find<TuiTitleBar>("header") ?? throw new InvalidOperationException("settings.tui 缺少 header");
            _catList = _markup.Find<TuiList>("catList") ?? throw new InvalidOperationException("settings.tui 缺少 catList");
            _detailPanel = _markup.Find<TuiScrollView>("detailPanel") ?? throw new InvalidOperationException("settings.tui 缺少 detailPanel");
            _hintBar = _markup.Find<TuiLabel>("hintBar") ?? throw new InvalidOperationException("settings.tui 缺少 hintBar");
            RootView = _markup.Screen?.RootView ?? throw new InvalidOperationException("settings.tui 根应为 Screen");
        }

        // 动态尺寸：标记声明结构，终端尺寸/宽度以 TW/TH 为准
        int mainH = Math.Max(5, TH - 3);
        int catW = Math.Min(18, TW / 3);
        _header.Width = TW;
        _catList.Width = catW;
        _catList.Height = mainH;
        _catList.Focused = !_focusOnDetail;
        _detailPanel.Width = TW - catW - 2;
        _detailPanel.Height = mainH;
        _detailPanel.IsAutoScrollToEnd = false;
        _hintBar.Width = TW;
        _hintBar.Bg = TuiTheme.Current.StatusBarBg;
        _hintBar.Fg = TuiTheme.Current.StatusBarFg;
        _hintBar.TextAlign = EHAlign.Center; // 状态栏提示文字居中
        _hintBar.Text = "↑↓ 选择  ←→ 切换面板  PgUp/PgDn 翻页  Enter 修改  Ctrl+S 保存  Esc 退出";
        RootView.Width = TW;
        RootView.Height = TH;
        RootView.Layout();

        // 类别列表数据与切换（schema 驱动）
        _catList.Items = [.. _catOrder];
        _catList.SelectedIndex = _catIdx;
        _catList.OnSelect = idx =>
        {
            _catIdx = idx;
            _itemIdx = 0;
            RebuildDetailPanel();
        };

        RebuildDetailPanel();
        ApplyHighlight();
        MarkDirty();
    }

    // ════════════════════════════════════════════════════════════════
    // 详情面板
    // ════════════════════════════════════════════════════════════════

    private void RebuildDetailPanel()
    {
        _detailPanel.Clear();
        _detailControls.Clear();

        if (_catIdx >= _catOrder.Length) return;

        var items = _groups[_catOrder[_catIdx]];
        int dW = _detailPanel.Width;

        foreach (var setting in items)
        {
            var label = new TuiLabel($" {setting.Label}")
                { Width = dW - 2, Height = 1 };
            _detailPanel.Add(label);
            _detailControls.Add(label);

            var val = GetValue(setting.Key);
            var valLabel = new TuiLabel(FormatValue(setting, val))
                { Width = dW - 4, Height = 1, Fg = 36 };
            _detailPanel.Add(valLabel);
            _detailControls.Add(valLabel);

            // 不再显示环境变量名（config.json 成为权威源后多数 WAYCODER_* 已作废，避免噪音）
            var desc = new TuiLabel($"  {setting.Desc}")
                { Width = dW - 2, Height = 1, Fg = 90 };
            _detailPanel.Add(desc);
            _detailControls.Add(desc);
        }

        // 就地展开的选项栏（select 编辑态）：钉在列表末尾，必须在 Layout() 之前 Add 才参与高度计算
        if (_optionBar != null)
            _detailPanel.Add(_optionBar);

        // 重建后 clamp 选中索引
        _itemIdx = Math.Clamp(_itemIdx, 0, Math.Max(0, items.Count - 1));

        _detailPanel.Layout();
        // 切换类别：滚动归零 + 标脏整区擦除（TuiScrollView 内容变更时填充背景清残留，
        // 否则新类别条目少时，旧内容残留在底部）
        _detailPanel.ScrollOffset = 0;
        _detailPanel.MarkDirty();
        ApplyHighlight();
    }

    private static string FormatValue(SettingDef s, string val) => s.Type switch
    {
        "secret" when val.Length > 0 => "  ••••••••",
        "select" => $"  {val}  ▾",
        _ => $"  {val}",
    };

    // ════════════════════════════════════════════════════════════════
    // 键盘
    // ════════════════════════════════════════════════════════════════

    public override bool OnKey(ConsoleKeyInfo key)
    {
        // 模态窗口优先
        if (HasModal)
            return base.OnKey(key);

        // 选项栏展开时独占键位（就地选择：↑↓ 移动 / Enter 应用 / Esc 取消 / 数字直选）
        if (_optionBar != null)
        {
            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    EndOptionSelect(); // 取消：不改值，只收起
                    return true;
                case ConsoleKey.UpArrow:
                case ConsoleKey.DownArrow:
                case ConsoleKey.Home:
                case ConsoleKey.End:
                case ConsoleKey.Enter: // Enter 触发 TuiPromptBar.OnSelect → 应用 + 收起
                    _optionBar.OnKey(key);
                    MarkDirty();
                    return true;
            }
            if (key.KeyChar is >= '1' and <= '9')
            {
                var i = key.KeyChar - '1';
                if (i < _optionBar.Items.Count)
                {
                    _optionBar.SelectedIndex = i;
                    _optionBar.OnSelect?.Invoke(_optionBar.Items[i]);
                }
                return true;
            }
            // 其余键一律吞掉：避免误触发表格导航把选项栏甩在后面（选完才回到常规导航）
            if (key.Key != ConsoleKey.Tab) return true;
        }

        bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);
        var items = GetCurrentItems();
        int itemCount = items?.Count ?? 0;

        switch (key.Key)
        {
            // ── 全局 ──
            case ConsoleKey.S when ctrl:
                _config.SaveToEnvFile();
                var cs = Manager?.ActiveScreen as ChatScreen;
                cs?.SyncTheme();
                ShowToast("已保存 — 设置已写入 .env 文件", 1500);
                return true;

            case ConsoleKey.Escape:
                Manager?.PopScreen();
                return true;

            // ── Tab / ← → — 切换焦点面板 ──
            case ConsoleKey.Tab:
            case ConsoleKey.LeftArrow:
            case ConsoleKey.RightArrow:
                ToggleFocus();
                return true;

            // ── ↑ — 上一项（焦点在左侧分类列表时列表自身处理，右侧才移动设置项）──
            case ConsoleKey.UpArrow:
                if (!_focusOnDetail && MoveCategoryList(key)) return true;
                NavigateItem(-1, itemCount);
                return true;

            // ── ↓ — 下一项 ──
            case ConsoleKey.DownArrow:
                if (!_focusOnDetail && MoveCategoryList(key)) return true;
                NavigateItem(1, itemCount);
                return true;

            // ── PgUp / PgDn — 翻页 ──
            case ConsoleKey.PageUp:
                if (!_focusOnDetail && MoveCategoryList(key)) return true;
                NavigateItem(-Math.Max(1, (TH - 6) / 3), itemCount);
                return true;

            case ConsoleKey.PageDown:
                if (!_focusOnDetail && MoveCategoryList(key)) return true;
                NavigateItem(Math.Max(1, (TH - 6) / 3), itemCount);
                return true;

            case ConsoleKey.Home:
                if (!_focusOnDetail && MoveCategoryList(key)) return true;
                _itemIdx = 0;
                ApplyHighlight();
                MarkDirty();
                return true;

            case ConsoleKey.End:
                if (!_focusOnDetail && MoveCategoryList(key)) return true;
                _itemIdx = Math.Max(0, itemCount - 1);
                ApplyHighlight();
                MarkDirty();
                return true;

            // ── Enter — 编辑值 ──
            case ConsoleKey.Enter:
                EditCurrentSetting();
                return true;

            // ── R — 复位当前项为默认 / Ctrl+R — 全部复位当前分组 ──
            case ConsoleKey.R when ctrl:
                ResetAllSettings();
                return true;
            case ConsoleKey.R:
                ResetCurrentSetting();
                return true;

            // ── 类别列表获得 ↑↓ 时自行处理 ──
            default:
                if (!_focusOnDetail && MoveCategoryList(key))
                    return true;
                return base.OnKey(key);
        }
    }

    private void ToggleFocus()
    {
        _focusOnDetail = !_focusOnDetail;
        _catList.Focused = !_focusOnDetail;
        ApplyHighlight();
        MarkDirty();
    }

    /// <summary>
    /// 把导航键交给左侧分类列表（TuiList），移动选中后同步分类并重建右侧详情。
    /// TuiList 的 OnSelect 只在空格激活时触发，↑↓ 移动不触发 —— 这里手动比较 SelectedIndex 刷新。
    /// </summary>
    private bool MoveCategoryList(ConsoleKeyInfo key)
    {
        var before = _catList.SelectedIndex;
        if (!_catList.OnKey(key)) return false;
        if (_catList.SelectedIndex != before)
        {
            _catIdx = _catList.SelectedIndex;
            _itemIdx = 0;
            RebuildDetailPanel();
        }
        return true;
    }

    private void NavigateItem(int delta, int itemCount)
    {
        if (itemCount == 0) return;

        if (_focusOnDetail)
        {
            int next = _itemIdx + delta;
            if (next >= 0 && next < itemCount)
            {
                _itemIdx = next;
                ApplyHighlight();
                MarkDirty();
            }
            // 超界不做跨类别跳转（← → 才是切类别）
        }
        else
        {
            // 焦点在类别列表 → 让 TuiList 处理
            _catList.OnKey(new ConsoleKeyInfo(
                delta < 0 ? '\0' : '\0',
                delta < 0 ? ConsoleKey.UpArrow : ConsoleKey.DownArrow,
                false, false, false));
        }
    }

    // ════════════════════════════════════════════════════════════════
    // 高亮
    // ════════════════════════════════════════════════════════════════

    /// <summary>应用高亮：选中项的三行全部着色，未选中项恢复默认。同时滚动保证选中项可见。</summary>
    private void ApplyHighlight()
    {
        for (int i = 0; i < _detailControls.Count; i++)
        {
            int group = i / 3;
            int pos = i % 3;
            bool sel = group == _itemIdx && _focusOnDetail;

            var c = _detailControls[i];

            if (pos == 0) // 标签行
            {
                c.Bg = sel ? 46 : 0;
                c.Fg = sel ? 30 : 37;
            }
            else if (pos == 1) // 值行
            {
                c.Bg = sel ? 46 : 0;
                c.Fg = sel ? 37 : 36;
            }
            else // 描述行
            {
                c.Bg = sel ? 46 : 0;
                c.Fg = sel ? 30 : 90;
            }
        }

        // 滚动详情面板使选中项可见（每项 3 行）
        if (_focusOnDetail)
        {
            int itemTop = _itemIdx * 3;
            int panelH = _detailPanel.Height;
            if (itemTop < _detailPanel.ScrollOffset)
                _detailPanel.ScrollOffset = itemTop;
            else if (itemTop + 3 > _detailPanel.ScrollOffset + panelH)
                _detailPanel.ScrollOffset = Math.Max(0, itemTop + 3 - panelH);
        }

        // 高亮改了标签的 Bg/Fg，但 Bg/Fg 是普通属性不 MarkDirty；滚动视图增量渲染
        // （child.IsDirty || IsDirty）只在面板脏时才重画标签 —— 不标脏则高亮不显示。
        _detailPanel.Invalidate();
    }

    // ════════════════════════════════════════════════════════════════
    // 编辑设置值
    // ════════════════════════════════════════════════════════════════

    private void EditCurrentSetting()
    {
        if (!_focusOnDetail) return;

        var items = GetCurrentItems();
        if (items == null || _itemIdx >= items.Count) return;

        var setting = items[_itemIdx];
        string cur = GetValue(setting.Key);

        // 模型选择使用 ModelPicker 对话框
        if (setting.Key is "Model" or "SmallModel")
        {
            var result = ModelPicker.Show();
            if (result != null)
            {
                // ModelPicker 已经更新了 Config 属性，刷新显示
                RebuildDetailPanel();
                MarkDirty();
            }
        }
        else if (setting.Type == "select" && setting.Options != null)
        {
            // 就地展开选项（不弹窗）—— 对标 Claude Code /config：选项列在被编辑项下方，↑↓ 选、Enter 应用
            BeginOptionSelect(setting);
        }
        else if (setting.Type == "toggle")
        {
            // 布尔开关：回车直接翻转 true/false（getter/setter 均为小写字符串，往返一致）
            bool curBool = string.Equals(cur, "true", StringComparison.OrdinalIgnoreCase);
            SetValue(setting.Key, curBool ? "false" : "true");
            RebuildDetailPanel();
            MarkDirty();
            ShowToast($"已设为 {(curBool ? "关闭" : "开启")} — {setting.Label}", 1200);
        }
        else if (setting.Type is "text" or "number" or "secret")
        {
            bool sec = setting.Type == "secret";
            // 设置值（模型名/超时秒数/密钥等）都是单行文本 → 用单行输入框 InputLine（回车=确定）
            ShowWindow(TuiDialog.InputLine(setting.Label,
                sec ? "输入密钥（不显示）" : $"输入新值（当前: {cur}）",
                sec ? "" : cur,
                input =>
                {
                    SetValue(setting.Key, input);
                    RebuildDetailPanel();
                    MarkDirty();
                }));
        }
    }

    /// <summary>
    /// 就地展开 select 选项栏（不弹窗）：选项列在详情末尾，❯ 指示 + 黄底高亮，当前值标「当前值」。
    /// ↑↓/Home/End 移动、Enter 应用、Esc 取消、数字键 1..9 直选。
    /// </summary>
    private void BeginOptionSelect(SettingDef setting)
    {
        var opts = setting.Options!;
        EndOptionSelect(); // 已展开别的项 → 先收起（同屏只留一个选项栏）

        string cur = GetValue(setting.Key);
        var idx = Array.IndexOf(opts, cur);
        _optionKey = setting.Key;

        _optionBar = new TuiPromptBar
        {
            Width = Math.Max(10, _detailPanel.Width - 2),
            Height = opts.Length + 2, // 上下边框（Bg==0 边框模式）
            Bg = 0,
            ShowArrow = true,
            HighlightBg = AnsiColors.BgYellow,
            HighlightFg = AnsiColors.Black,
            SelectedIndex = idx >= 0 ? idx : 0,
            Items = [.. opts.Select((o, i) => new PromptItem
            {
                Kind = EPromptKind.Choice,
                Label = $"{i + 1}. {o}",
                // 标出当前值：用户一进来就知道「现在是哪个」，而不用先看上面那行 ▾
                Detail = string.Equals(o, cur, StringComparison.OrdinalIgnoreCase) ? "当前值" : null,
                ResultCode = i,
            })],
        };
        _optionBar.OnSelect = item =>
        {
            SetValue(setting.Key, opts[item.ResultCode]);
            EndOptionSelect(); // 应用后收起（内部会 Rebuild + MarkDirty）
        };

        RebuildDetailPanel();
        MarkDirty();
    }

    /// <summary>收起就地选项栏（应用或取消后调用）。幂等。</summary>
    private void EndOptionSelect()
    {
        if (_optionBar == null) return;
        _optionBar = null;
        _optionKey = null;
        RebuildDetailPanel();
        MarkDirty();
    }

    // ════════════════════════════════════════════════════════════════
    // 辅助
    // ════════════════════════════════════════════════════════════════

    private List<SettingDef>? GetCurrentItems()
    {
        if (_catIdx >= _catOrder.Length) return null;
        return _groups[_catOrder[_catIdx]];
    }

    // ════════════════════════════════════════════════════════════════
    // 配置读写（从 SettingsPage 迁移）
    // ════════════════════════════════════════════════════════════════

    private string GetValue(string key) => key switch
    {
        "Model"              => _config.Model,
        "SmallModel"         => _config.SmallModel,
        "BaseUrl"            => _config.BaseUrl ?? "",
        "ApiKey"             => _config.ApiKey,
        "MaxTokens"          => _config.MaxTokens.ToString(),
        "Temperature"        => _config.Temperature.ToString("F1"),
        "MaxContextTokens"   => _config.MaxContextTokens.ToString(),
        "MaxBudgetUsd"       => _config.MaxBudgetUsd?.ToString("F2") ?? "",
        "Provider"           => _config.Provider,
        "SmallProvider"      => _config.SmallProvider,
        "AutoGitCommit"      => _config.AutoGitCommit ? "true" : "false",
        "WatchMode"          => _config.WatchMode ? "true" : "false",
        "PromptCaching"      => _config.PromptCaching ? "true" : "false",
        "SandboxLevel"       => _config.SandboxLevel,
        "EditorLint"         => _config.EditorLint ? "true" : "false",
        "DiffPreview"        => _config.DiffPreview ? "true" : "false",
        "WriteContentView"   => _config.WriteContentView ? "true" : "false",
        "MouseEnabled"       => _config.MouseEnabled ? "true" : "false",
        "MaxChatMessages"    => _config.MaxChatMessages.ToString(),
        "MaxCodePreviewLines" => _config.MaxCodePreviewLines.ToString(),
        "ToolTimeoutSec"     => _config.ToolTimeoutSec.ToString(),
        "LintTimeoutSec"     => _config.LintTimeoutSec.ToString(),
        "BackgroundTaskTimeoutSec" => _config.BackgroundTaskTimeoutSec.ToString(),
        "AutoTestTimeoutSec" => _config.AutoTestTimeoutSec.ToString(),
        "AutoTestDebounceSec" => _config.AutoTestDebounceSec.ToString(),
        "GitTimeoutSec"      => _config.GitTimeoutSec.ToString(),
        "KillTimeoutSec"     => _config.KillTimeoutSec.ToString(),
        "DownloadTimeoutSec" => _config.DownloadTimeoutSec.ToString(),
        "HookTimeoutSec"     => _config.HookTimeoutSec.ToString(),
        "AskUserTimeoutSec"  => _config.AskUserTimeoutSec.ToString(),
        "RegexTimeoutSec"    => _config.RegexTimeoutSec.ToString(),
        "FetchTimeoutSec"    => _config.FetchTimeoutSec.ToString(),
        "LlmHttpTimeoutSec"  => _config.LlmHttpTimeoutSec.ToString(),
        "LlmConnectionTimeoutSec" => _config.LlmConnectionTimeoutSec.ToString(),
        "LlmRateLimitMaxWaitSec" => _config.LlmRateLimitMaxWaitSec.ToString(),
        "LlmMaxRetries"      => _config.LlmMaxRetries.ToString(),
        "FileLockTimeoutSec" => _config.FileLockTimeoutSec.ToString(),
        "SubAgentMaxDepth"   => _config.SubAgentMaxDepth.ToString(),
        "MemoryRelevanceTopN" => _config.MemoryRelevanceTopN.ToString(),
        "EmbeddingEnabled"   => _config.EmbeddingEnabled ? "true" : "false",
        "EmbeddingModel"     => _config.EmbeddingModel,
        "EmbeddingDimensions" => _config.EmbeddingDimensions.ToString(),
        "TeamMemoryEnabled"  => _config.TeamMemoryEnabled ? "true" : "false",
        "TeamMemoryAutoSync" => _config.TeamMemoryAutoSync ? "true" : "false",
        "ThemePreset"        => _config.ThemePreset,
        "BorderStyle"        => _config.BorderStyle,
        "BorderColor"        => _config.BorderColor,
        "AccentColor"        => _config.AccentColor,
        "ColorScheme"        => _config.ColorScheme,
        "ChatDisplayStyle"   => _config.ChatDisplayStyle,
        // 其余配置项走 Schema Getter 自动读取（补全 45 项缺失，与 /config 命令共用逻辑）
        _ => Config.GetPropValue(key) ?? "",
    };

    /// <summary>复位当前选中项为 schema 默认值（改错的值设回默认）。</summary>
    private void ResetCurrentSetting()
    {
        var items = GetCurrentItems();
        if (items == null || _itemIdx < 0 || _itemIdx >= items.Count) return;
        var setting = items[_itemIdx];
        if (string.IsNullOrEmpty(setting.Default)) { ShowToast("该配置无默认值", 1500); return; }
        SetValue(setting.Key, setting.Default);
        RebuildDetailPanel();
        MarkDirty();
    }

    /// <summary>复位当前分组全部项为默认值。</summary>
    private void ResetAllSettings()
    {
        var items = GetCurrentItems();
        if (items == null) return;
        foreach (var s in items)
            if (!string.IsNullOrEmpty(s.Default)) SetValue(s.Key, s.Default);
        RebuildDetailPanel();
        MarkDirty();
    }

    private void SetValue(string key, string value)
    {
        switch (key)
        {
            case "Model":              ConnectionConfig.ApplyModelChoice(_config.Provider, value, true, out _); break;
            case "SmallModel":         ConnectionConfig.ApplyModelChoice(_config.SmallProvider, value, false, out _); break;
            // Provider/SmallProvider/BaseUrl 直写 Config 字段（保存时 Reconcile 收敛回 state）：
            // 显式写解除会话加载的内存镜像标记，确保本次编辑会被 Reconcile 收敛
            case "BaseUrl":            _config.SessionModelMirror = false; _config.BaseUrl = value; break;
            case "Provider":           _config.SessionModelMirror = false; _config.Provider = value; break;
            case "SmallProvider":      _config.SessionModelMirror = false; _config.SmallProvider = value; break;
            case "ApiKey":             _config.ApiKey = value; break;
            case "MaxTokens":          if (int.TryParse(value, out var v)) _config.MaxTokens = v; break;
            case "Temperature":        if (float.TryParse(value, out var f)) _config.Temperature = f; break;
            case "MaxContextTokens":   if (int.TryParse(value, out var v2)) _config.MaxContextTokens = v2; break;
            case "MaxBudgetUsd":       _config.MaxBudgetUsd = double.TryParse(value, out var d) ? d : null; break;
            case "AutoGitCommit":      _config.AutoGitCommit = bool.TryParse(value, out var b) && b; break;
            case "WatchMode":          _config.WatchMode = bool.TryParse(value, out var b2) && b2; break;
            case "PromptCaching":      _config.PromptCaching = bool.TryParse(value, out var b3) && b3; break;
            case "SandboxLevel":       _config.SandboxLevel = value; break;
            case "EditorLint":         _config.EditorLint = bool.TryParse(value, out var b4) && b4; break;
            case "DiffPreview":        _config.DiffPreview = bool.TryParse(value, out var b5) && b5; break;
            case "WriteContentView":   _config.WriteContentView = bool.TryParse(value, out var wcv) && wcv; break;
            case "MouseEnabled":       _config.MouseEnabled = bool.TryParse(value, out var me) && me; break;
            case "MaxChatMessages":    if (int.TryParse(value, out var mcm)) _config.MaxChatMessages = Math.Clamp(mcm, 100, 10_000); break;
            case "MaxCodePreviewLines": if (int.TryParse(value, out var mcl)) _config.MaxCodePreviewLines = Math.Clamp(mcl, 10, 1000); break;
            case "ToolTimeoutSec":     if (int.TryParse(value, out var v3)) _config.ToolTimeoutSec = v3; break;
            case "LintTimeoutSec":     if (int.TryParse(value, out var v4)) _config.LintTimeoutSec = v4; break;
            case "BackgroundTaskTimeoutSec": if (int.TryParse(value, out var v31)) _config.BackgroundTaskTimeoutSec = v31; break;
            case "AutoTestTimeoutSec": if (int.TryParse(value, out var v32)) _config.AutoTestTimeoutSec = v32; break;
            case "AutoTestDebounceSec": if (int.TryParse(value, out var v33)) _config.AutoTestDebounceSec = v33; break;
            case "GitTimeoutSec":      if (int.TryParse(value, out var v34)) _config.GitTimeoutSec = v34; break;
            case "KillTimeoutSec":     if (int.TryParse(value, out var v35)) _config.KillTimeoutSec = v35; break;
            case "DownloadTimeoutSec": if (int.TryParse(value, out var v36)) _config.DownloadTimeoutSec = v36; break;
            case "HookTimeoutSec":     if (int.TryParse(value, out var v37)) _config.HookTimeoutSec = v37; break;
            case "AskUserTimeoutSec":  if (int.TryParse(value, out var v38)) _config.AskUserTimeoutSec = v38; break;
            case "RegexTimeoutSec":    if (int.TryParse(value, out var v39)) _config.RegexTimeoutSec = v39; break;
            case "FetchTimeoutSec":    if (int.TryParse(value, out var v40)) _config.FetchTimeoutSec = v40; break;
            case "LlmHttpTimeoutSec":  if (int.TryParse(value, out var v41)) _config.LlmHttpTimeoutSec = v41; break;
            case "LlmConnectionTimeoutSec": if (int.TryParse(value, out var v42)) _config.LlmConnectionTimeoutSec = v42; break;
            case "LlmRateLimitMaxWaitSec": if (int.TryParse(value, out var v43)) _config.LlmRateLimitMaxWaitSec = v43; break;
            case "LlmMaxRetries":      if (int.TryParse(value, out var v44)) _config.LlmMaxRetries = v44; break;
            case "FileLockTimeoutSec": if (int.TryParse(value, out var v45)) _config.FileLockTimeoutSec = v45; break;
            case "SubAgentMaxDepth":   if (int.TryParse(value, out var v5)) _config.SubAgentMaxDepth = Math.Clamp(v5, 1, 5); break;
            case "MemoryRelevanceTopN": if (int.TryParse(value, out var v6)) _config.MemoryRelevanceTopN = Math.Clamp(v6, 0, 20); break;
            case "EmbeddingEnabled":   _config.EmbeddingEnabled = bool.TryParse(value, out var b6) && b6; break;
            case "EmbeddingModel":     _config.EmbeddingModel = value; break;
            case "EmbeddingDimensions": if (int.TryParse(value, out var v7)) _config.EmbeddingDimensions = v7; break;
            case "TeamMemoryEnabled":  _config.TeamMemoryEnabled = bool.TryParse(value, out var b7) && b7; break;
            case "TeamMemoryAutoSync": _config.TeamMemoryAutoSync = bool.TryParse(value, out var b8) && b8; break;
            case "ThemePreset":        _config.ThemePreset = value; ThemeConfig.ApplyPreset(value); break;
            case "BorderStyle":        _config.BorderStyle = value; break;
            case "BorderColor":        _config.BorderColor = value; break;
            case "AccentColor":        _config.AccentColor = value; break;
            case "ColorScheme":        Config.ApplyColorScheme(_config, value); break;
            case "ChatDisplayStyle":  _config.ChatDisplayStyle = value; break;
            // 其余配置项走 Schema Setter 自动解析/钳制/select 校验（补全 45 项缺失，与 /config 命令共用逻辑）
            default:
                Config.TrySetPropValue(key, value, out _);
                break;
        }
    }
}
