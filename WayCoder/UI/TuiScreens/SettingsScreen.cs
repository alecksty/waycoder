using WayCoder.UI.Controls;

using WayCoder.UI.TuiControls;
using WayCoder.UI.TuiScreens;

namespace WayCoder.UI;

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

    // ── 控件引用 ──
    private TuiLabel _header = null!;
    private TuiControls.TuiList _catList = null!;
    private TuiVBox _detailPanel = null!;
    private TuiLabel _hintBar = null!;
    private readonly List<TuiControl> _detailControls = [];   // 每组 3 个: label, value, desc

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

        _config = Config.FromEnv();

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
        RootView = new TuiVBox { Width = TW, Height = TH };

        // ── 顶栏 ──
        _header = new TuiLabel(" ⚙ 设置 / 配置")
            { Width = TW, Height = 1, Bg = 44, Fg = 37 };
        RootView.Add(_header);

        // ── 主区域 ──
        int mainH = Math.Max(5, TH - 3);
        var hbox = new TuiHBox { Width = TW, Height = mainH };

        // 左侧类别列表
        int catW = Math.Min(18, TW / 3);
        _catList = new TuiControls.TuiList
        {
            Width = catW,
            Height = mainH,
            Focused = !_focusOnDetail
        };
        _catList.Items = [.. _catOrder];
        _catList.SelectedIndex = _catIdx;
        _catList.OnSelect = idx =>
        {
            _catIdx = idx;
            _itemIdx = 0;
            RebuildDetailPanel();
        };
        hbox.Add(_catList);

        // 竖分隔
        hbox.Add(new TuiSeparator(SeparatorDirection.Vertical) { Height = mainH });

        // 右侧详情
        int detailW = TW - catW - 2;
        _detailPanel = new TuiVBox { Width = detailW, Height = mainH };
        hbox.Add(_detailPanel);

        RootView.Add(hbox);

        // ── 底栏 ──
        _hintBar = new TuiLabel(" ↑↓ 选择  ←→ 切换面板  PgUp/PgDn 翻页  Enter 修改  Ctrl+S 保存  Esc 退出")
            { Width = TW, Height = 1, Bg = 100, Fg = 37 };
        RootView.Add(_hintBar);

        RebuildDetailPanel();
        ApplyHighlight();
        RootView.Layout();
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

            var desc = new TuiLabel($"  {setting.Desc}  [{setting.EnvVar}]")
                { Width = dW - 2, Height = 1, Fg = 90 };
            _detailPanel.Add(desc);
            _detailControls.Add(desc);
        }

        // 重建后 clamp 选中索引
        _itemIdx = Math.Clamp(_itemIdx, 0, Math.Max(0, items.Count - 1));

        _detailPanel.Layout();
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

            // ── ↑ — 上一项 ──
            case ConsoleKey.UpArrow:
                NavigateItem(-1, itemCount);
                return true;

            // ── ↓ — 下一项 ──
            case ConsoleKey.DownArrow:
                NavigateItem(1, itemCount);
                return true;

            // ── PgUp / PgDn — 翻页 ──
            case ConsoleKey.PageUp:
                NavigateItem(-Math.Max(1, (TH - 6) / 3), itemCount);
                return true;

            case ConsoleKey.PageDown:
                NavigateItem(Math.Max(1, (TH - 6) / 3), itemCount);
                return true;

            case ConsoleKey.Home:
                _itemIdx = 0;
                ApplyHighlight();
                MarkDirty();
                return true;

            case ConsoleKey.End:
                _itemIdx = Math.Max(0, itemCount - 1);
                ApplyHighlight();
                MarkDirty();
                return true;

            // ── Enter — 编辑值 ──
            case ConsoleKey.Enter:
                EditCurrentSetting();
                return true;

            // ── 类别列表获得 ↑↓ 时自行处理 ──
            default:
                if (!_focusOnDetail && _catList.OnKey(key))
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

    /// <summary>应用高亮：选中项的三行全部着色，未选中项恢复默认</summary>
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

        if (setting.Type == "select" && setting.Options != null)
        {
            ShowWindow(TuiDialog.Select(setting.Label, [.. setting.Options], idx =>
            {
                SetValue(setting.Key, setting.Options[idx]);
                RebuildDetailPanel();
                MarkDirty();
            }));
        }
        else if (setting.Type is "text" or "number" or "secret")
        {
            bool sec = setting.Type == "secret";
            ShowWindow(TuiDialog.Input(setting.Label,
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
        "AutoGitCommit"      => _config.AutoGitCommit ? "true" : "false",
        "WatchMode"          => _config.WatchMode ? "true" : "false",
        "PromptCaching"      => _config.PromptCaching ? "true" : "false",
        "SandboxLevel"       => _config.SandboxLevel,
        "EditorLint"         => _config.EditorLint ? "true" : "false",
        "ToolTimeoutSec"     => _config.ToolTimeoutSec.ToString(),
        "LintTimeoutSec"     => _config.LintTimeoutSec.ToString(),
        "SubAgentMaxDepth"   => _config.SubAgentMaxDepth.ToString(),
        "MemoryRelevanceTopN" => _config.MemoryRelevanceTopN.ToString(),
        "ThemePreset"        => _config.ThemePreset,
        "BorderStyle"        => _config.BorderStyle,
        "BorderColor"        => _config.BorderColor,
        "AccentColor"        => _config.AccentColor,
        "ColorScheme"        => _config.ColorScheme,
        _ => "",
    };

    private void SetValue(string key, string value)
    {
        switch (key)
        {
            case "Model":              _config.Model = value; break;
            case "SmallModel":         _config.SmallModel = value; break;
            case "BaseUrl":            _config.BaseUrl = value; break;
            case "ApiKey":             _config.ApiKey = value; break;
            case "MaxTokens":          if (int.TryParse(value, out var v)) _config.MaxTokens = v; break;
            case "Temperature":        if (float.TryParse(value, out var f)) _config.Temperature = f; break;
            case "MaxContextTokens":   if (int.TryParse(value, out var v2)) _config.MaxContextTokens = v2; break;
            case "MaxBudgetUsd":       _config.MaxBudgetUsd = double.TryParse(value, out var d) ? d : null; break;
            case "Provider":           _config.Provider = value; break;
            case "AutoGitCommit":      _config.AutoGitCommit = bool.TryParse(value, out var b) && b; break;
            case "WatchMode":          _config.WatchMode = bool.TryParse(value, out var b2) && b2; break;
            case "PromptCaching":      _config.PromptCaching = bool.TryParse(value, out var b3) && b3; break;
            case "SandboxLevel":       _config.SandboxLevel = value; break;
            case "EditorLint":         _config.EditorLint = bool.TryParse(value, out var b4) && b4; break;
            case "ToolTimeoutSec":     if (int.TryParse(value, out var v3)) _config.ToolTimeoutSec = v3; break;
            case "LintTimeoutSec":     if (int.TryParse(value, out var v4)) _config.LintTimeoutSec = v4; break;
            case "SubAgentMaxDepth":   if (int.TryParse(value, out var v5)) _config.SubAgentMaxDepth = Math.Clamp(v5, 1, 5); break;
            case "MemoryRelevanceTopN": if (int.TryParse(value, out var v6)) _config.MemoryRelevanceTopN = Math.Clamp(v6, 0, 20); break;
            case "ThemePreset":        _config.ThemePreset = value; ThemeConfig.ApplyPreset(value); break;
            case "BorderStyle":        _config.BorderStyle = value; break;
            case "BorderColor":        _config.BorderColor = value; break;
            case "AccentColor":        _config.AccentColor = value; break;
            case "ColorScheme":        Config.ApplyColorScheme(_config, value); break;
        }
    }
}
