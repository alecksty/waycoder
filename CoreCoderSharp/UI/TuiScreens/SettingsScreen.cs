using CoreCoderSharp.UI.Controls;
using TuiListCtrl = CoreCoderSharp.UI.Controls.TuiList;
using TuiSep = CoreCoderSharp.UI.Controls.TuiSeparator;

namespace CoreCoderSharp.UI;

/// <summary>
/// 设置屏幕 —— 配置编辑器，使用新 TUI 架构。
///
/// 布局：
///   RootView (VBox)
///   ├─ Header        TuiLabel       "⚙ 设置 / 配置"
///   ├─ MainArea      TuiHBox
///   │   ├─ CatList   TuiList        类别列表
///   │   ├─ Separator TuiSeparator   竖分隔线
///   │   └─ Detail    TuiVBox        设置项列表（动态重建）
///   └─ HintBar       TuiLabel       " ↑↓选择 ←→切换面板 Enter修改 Ctrl+S保存 Esc退出"
///
/// 键盘：↑↓选择设置项 ←→切换面板 Tab切换焦点 Enter修改值 Ctrl+S保存 Esc退出
/// </summary>
public class SettingsScreen : TuiScreen
{
    // ── 数据模型 ──
    private Dictionary<string, List<SettingDef>> _groups = [];
    private string[] _catOrder = [];
    private int _catIdx;
    private int _itemIdx;
    private Config _config = null!;

    // ── 控件引用 ──
    private TuiLabel _header = null!;
    private Controls.TuiList _catList = null!;
    private TuiVBox _detailPanel = null!;
    private TuiLabel _hintBar = null!;
    private readonly List<TuiControl> _detailControls = [];

    public SettingsScreen()
    {
        Name = "settings";
    }

    // ── 生命周期 ──

    public override void Activate()
    {
        base.Activate();

        _config = Config.FromEnv();

        var schema = Config.SettingSchema();
        _groups = schema.GroupBy(s => s.Category)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Order).ToList());
        _catOrder = schema.Select(s => s.Category).Distinct().ToArray();
        _catIdx = 0;
        _itemIdx = 0;

        BuildLayout();
    }

    private void BuildLayout()
    {
        RootView = new TuiVBox { Width = TW, Height = TH };

        // ── 顶栏 ──
        _header = new TuiLabel(" ⚙ 设置 / 配置") { Width = TW, Height = 1, Bg = 44, Fg = 37 };
        RootView.Add(_header);

        // ── 主区域 ──
        int mainH = TH - 3;
        var hbox = new TuiHBox { Width = TW, Height = mainH };

        // 左侧：类别列表
        int catW = Math.Min(18, TW / 3);
        _catList = new Controls.TuiList
        {
            Width = catW,
            Height = mainH,
            Focused = true
        };
        _catList.Items = _catOrder.ToList();
        _catList.SelectedIndex = _catIdx;
        _catList.OnSelect = idx =>
        {
            _catIdx = idx;
            _itemIdx = 0;
            RebuildDetailPanel();
        };
        hbox.Add(_catList);

        // 竖分隔线
        hbox.Add(new Controls.TuiSeparator(SeparatorDirection.Vertical) { Height = mainH });

        // 右侧：设置项面板
        int detailW = TW - catW - 2;
        _detailPanel = new TuiVBox { Width = detailW, Height = mainH };
        hbox.Add(_detailPanel);

        RootView.Add(hbox);

        // ── 底栏 ──
        _hintBar = new TuiLabel(" ↑↓ 选择  ←→ 切换面板  Enter 修改  Ctrl+S 保存  Esc 退出")
        {
            Width = TW, Height = 1, Bg = 100, Fg = 37
        };
        RootView.Add(_hintBar);

        RebuildDetailPanel();
        RootView.Layout();
        MarkDirty();
    }

    // ── 重建设置项面板 ──

    private void RebuildDetailPanel()
    {
        _detailPanel.Clear();
        _detailControls.Clear();

        if (_catIdx >= _catOrder.Length) return;
        var items = _groups[_catOrder[_catIdx]];
        int detailW = _detailPanel.Width;

        foreach (var setting in items)
        {
            // 标签
            var label = new TuiLabel($" {setting.Label}")
            {
                Width = detailW - 2, Height = 1
            };
            _detailPanel.Add(label);
            _detailControls.Add(label);

            // 值显示
            string currentVal = GetValue(setting.Key);
            var valLabel = new TuiLabel(FormatValue(setting, currentVal))
            {
                Width = detailW - 4, Height = 1,
                Fg = 36 // Cyan
            };
            _detailPanel.Add(valLabel);
            _detailControls.Add(valLabel);

            // 描述
            var descLabel = new TuiLabel($"  {setting.Desc}  [{setting.EnvVar}]")
            {
                Width = detailW - 2, Height = 1,
                Fg = 90 // Dim
            };
            _detailPanel.Add(descLabel);
            _detailControls.Add(descLabel);
        }

        _detailPanel.Layout();
    }

    private static string FormatValue(SettingDef setting, string val)
    {
        if (setting.Type == "secret" && val.Length > 0)
            return "  ••••••••";
        if (setting.Type == "select")
            return $"  {val}  ▾";
        return $"  {val}";
    }

    // ── 键盘处理 ──

    public override bool HandleKey(ConsoleKeyInfo key)
    {
        // 有模态窗口时，让基类处理
        if (HasModal)
            return base.HandleKey(key);

        bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);

        switch (key.Key)
        {
            case ConsoleKey.S when ctrl:
                _config.SaveToEnvFile();
                var chatScreen = Manager?.ActiveScreen as ChatScreen;
                chatScreen?.SyncTheme();
                ShowToast("已保存 — 设置已写入 .env 文件", 1500);
                return true;

            case ConsoleKey.Escape:
                Manager?.PopScreen();
                return true;

            case ConsoleKey.UpArrow:
                if (_itemIdx > 0) { _itemIdx--; HighlightCurrentItem(); MarkDirty(); }
                else if (_catIdx > 0) { _catIdx--; _catList.SelectedIndex = _catIdx; _itemIdx = 0; RebuildDetailPanel(); }
                return true;

            case ConsoleKey.DownArrow:
                var items = GetCurrentItems();
                if (items != null && _itemIdx < items.Count - 1)
                { _itemIdx++; HighlightCurrentItem(); MarkDirty(); }
                else if (_catIdx < _catOrder.Length - 1)
                { _catIdx++; _catList.SelectedIndex = _catIdx; _itemIdx = 0; RebuildDetailPanel(); }
                return true;

            case ConsoleKey.LeftArrow:
                _catList.Focused = true;
                return true;

            case ConsoleKey.RightArrow:
                if (_detailControls.Count > 0)
                    _detailControls[0].Focused = true;
                return true;

            case ConsoleKey.Enter:
                EditCurrentSetting();
                return true;

            case ConsoleKey.Tab:
                if (_catList.Focused)
                {
                    if (_detailControls.Count > 0)
                        _detailControls[0].Focused = true;
                }
                else
                {
                    _catList.Focused = true;
                }
                return true;

            default:
                return base.HandleKey(key);
        }
    }

    private List<SettingDef>? GetCurrentItems()
    {
        if (_catIdx >= _catOrder.Length) return null;
        return _groups[_catOrder[_catIdx]];
    }

    private void HighlightCurrentItem()
    {
        for (int i = 0; i < _detailControls.Count; i++)
        {
            int itemGroup = i / 3;
            bool isSelected = itemGroup == _itemIdx;
            var ctrl = _detailControls[i];
            if (i % 3 == 0) // Label
            {
                ctrl.Bg = isSelected ? 46 : 0;
                ctrl.Fg = isSelected ? 30 : 37;
            }
        }
    }

    // ── 编辑设置值 ──

    private void EditCurrentSetting()
    {
        var items = GetCurrentItems();
        if (items == null || _itemIdx >= items.Count) return;

        var setting = items[_itemIdx];
        string currentVal = GetValue(setting.Key);

        if (setting.Type == "select" && setting.Options != null)
        {
            var win = TuiDialog.Select(setting.Label, setting.Options.ToList(), idx =>
            {
                SetValue(setting.Key, setting.Options[idx]);
                RebuildDetailPanel();
                MarkDirty();
            });
            ShowWindow(win);
        }
        else if (setting.Type is "text" or "number" or "secret")
        {
            bool isSecret = setting.Type == "secret";
            var win = TuiDialog.Input(
                setting.Label,
                isSecret ? "输入密钥（不显示）" : $"输入新值（当前: {currentVal}）",
                isSecret ? "" : currentVal,
                input =>
                {
                    SetValue(setting.Key, input);
                    RebuildDetailPanel();
                    MarkDirty();
                });
            ShowWindow(win);
        }
    }

    // ── 配置读写（从旧 SettingsPage 迁移） ──

    private string GetValue(string key) => key switch
    {
        "Model" => _config.Model,
        "SmallModel" => _config.SmallModel,
        "BaseUrl" => _config.BaseUrl ?? "",
        "ApiKey" => _config.ApiKey,
        "MaxTokens" => _config.MaxTokens.ToString(),
        "Temperature" => _config.Temperature.ToString("F1"),
        "MaxContextTokens" => _config.MaxContextTokens.ToString(),
        "MaxBudgetUsd" => _config.MaxBudgetUsd?.ToString("F2") ?? "",
        "Provider" => _config.Provider,
        "AutoGitCommit" => _config.AutoGitCommit ? "true" : "false",
        "WatchMode" => _config.WatchMode ? "true" : "false",
        "PromptCaching" => _config.PromptCaching ? "true" : "false",
        "SandboxLevel" => _config.SandboxLevel,
        "EditorLint" => _config.EditorLint ? "true" : "false",
        "ToolTimeoutSec" => _config.ToolTimeoutSec.ToString(),
        "LintTimeoutSec" => _config.LintTimeoutSec.ToString(),
        "SubAgentMaxDepth" => _config.SubAgentMaxDepth.ToString(),
        "MemoryRelevanceTopN" => _config.MemoryRelevanceTopN.ToString(),
        "ThemePreset" => _config.ThemePreset,
        "BorderStyle" => _config.BorderStyle,
        "BorderColor" => _config.BorderColor,
        "AccentColor" => _config.AccentColor,
        "ColorScheme" => _config.ColorScheme,
        _ => "",
    };

    private void SetValue(string key, string value)
    {
        switch (key)
        {
            case "Model": _config.Model = value; break;
            case "SmallModel": _config.SmallModel = value; break;
            case "BaseUrl": _config.BaseUrl = value; break;
            case "ApiKey": _config.ApiKey = value; break;
            case "MaxTokens": if (int.TryParse(value, out var mt)) _config.MaxTokens = mt; break;
            case "Temperature": if (float.TryParse(value, out var ft)) _config.Temperature = ft; break;
            case "MaxContextTokens": if (int.TryParse(value, out var mc)) _config.MaxContextTokens = mc; break;
            case "MaxBudgetUsd": _config.MaxBudgetUsd = double.TryParse(value, out var mb) ? mb : null; break;
            case "Provider": _config.Provider = value; break;
            case "AutoGitCommit": _config.AutoGitCommit = bool.TryParse(value, out var ac) && ac; break;
            case "WatchMode": _config.WatchMode = bool.TryParse(value, out var wm) && wm; break;
            case "PromptCaching": _config.PromptCaching = bool.TryParse(value, out var pc) && pc; break;
            case "SandboxLevel": _config.SandboxLevel = value; break;
            case "EditorLint": _config.EditorLint = bool.TryParse(value, out var el) && el; break;
            case "ToolTimeoutSec": if (int.TryParse(value, out var tto)) _config.ToolTimeoutSec = tto; break;
            case "LintTimeoutSec": if (int.TryParse(value, out var lto)) _config.LintTimeoutSec = lto; break;
            case "SubAgentMaxDepth": if (int.TryParse(value, out var sd)) _config.SubAgentMaxDepth = Math.Clamp(sd, 1, 5); break;
            case "MemoryRelevanceTopN": if (int.TryParse(value, out var mtn)) _config.MemoryRelevanceTopN = Math.Clamp(mtn, 0, 20); break;
            case "ThemePreset": _config.ThemePreset = value; ThemeConfig.ApplyPreset(value); break;
            case "BorderStyle": _config.BorderStyle = value; break;
            case "BorderColor": _config.BorderColor = value; break;
            case "AccentColor": _config.AccentColor = value; break;
            case "ColorScheme": Config.ApplyColorScheme(_config, value); break;
            case "SaveSettings": _config.SaveToEnvFile(); return;
        }
    }

    public override void Deactivate()
    {
        base.Deactivate();
        var chatScreen = Manager?.ActiveScreen as ChatScreen;
        chatScreen?.SyncTheme();
    }
}
