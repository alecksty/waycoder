namespace CoreCoderSharp.UI;

/// <summary>
/// 全屏设置 — 从 Config.SettingSchema() 自动生成布局。
/// 新增配置项只需在 Config.cs 加一行 SettingDef。
/// ↑↓选类/项 ←→切换面板 Enter修改(select=菜单/text=输入) Esc退出
/// </summary>
public static class SettingsPage
{
    private static Dictionary<string, List<SettingDef>> _groups = [];
    private static string[] _catOrder = [];
    private static int _catIdx, _itemIdx;
    private static bool _right; // 焦点在右侧
    private static bool _editing;
    private static string _editBuf = "";
    private static int _editPos;
    private static Config _config = null!;

    public static void Show()
    {
        var sm = ScreenManager.Instance;
        var wasActive = sm.IsActive;
        _config = Config.FromEnv();

        // 从 schema 自动生成分类
        var schema = Config.SettingSchema();
        _groups = schema.GroupBy(s => s.Category)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Order).ToList());
        _catOrder = schema.Select(s => s.Category).Distinct().ToArray();

        _catIdx = 0; _itemIdx = 0; _right = false; _editing = false;

        if (!wasActive) sm.Enter();

        try
        {
            while (true)
            {
                Render();
                var key = Console.ReadKey(intercept: true);

                if (_editing) { HandleEditKey(key); continue; }

                var cat = _catOrder[_catIdx];
                var items = _groups[cat];

                var ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);
                switch (key.Key)
                {
                    case ConsoleKey.S when ctrl:
                        _config.SaveToEnvFile();
                        sm.RefreshTheme();
                        sm.ShowDialog("已保存", "设置已写入 .env 文件", ScreenManager.DialogType.Success);
                        return;
                    case ConsoleKey.Escape: return;
                    case ConsoleKey.UpArrow:
                        if (_right && _itemIdx > 0) _itemIdx--;
                        else if (!_right && _catIdx > 0) _catIdx--;
                        break;
                    case ConsoleKey.DownArrow:
                        if (_right && _itemIdx < items.Count - 1) _itemIdx++;
                        else if (!_right && _catIdx < _catOrder.Length - 1) _catIdx++;
                        break;
                    case ConsoleKey.LeftArrow: _right = false; _itemIdx = 0; break;
                    case ConsoleKey.RightArrow: _right = true; break;
                    case ConsoleKey.Enter:
                        var s = items[_itemIdx];
                        if (s.Type == "select" && s.Options != null)
                        {
                            var idx = sm.ShowMenu(s.Label, [.. s.Options]);
                            if (idx >= 0) SetValue(s.Key, s.Options[idx]);
                        }
                        else if (s.Type is "text" or "number" or "secret")
                        {
                            _editing = true;
                            _editBuf = s.Type == "secret" ? "" : GetValue(s.Key);
                            _editPos = _editBuf.Length;
                        }
                        // toggle 等类型后续扩展
                        break;
                }
            }
        }
        finally
        {
            if (!wasActive) sm.Exit(); else sm.Render();
        }
    }

    private static void HandleEditKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Enter: SaveEdit(); _editing = false; break;
            case ConsoleKey.Escape: _editing = false; break;
            case ConsoleKey.Backspace:
                if (_editPos > 0) { _editBuf = _editBuf[..(_editPos - 1)] + _editBuf[_editPos..]; _editPos--; } break;
            case ConsoleKey.Delete:
                if (_editPos < _editBuf.Length) _editBuf = _editBuf.Remove(_editPos, 1); break;
            case ConsoleKey.LeftArrow: if (_editPos > 0) _editPos--; break;
            case ConsoleKey.RightArrow: if (_editPos < _editBuf.Length) _editPos++; break;
            case ConsoleKey.Home: _editPos = 0; break;
            case ConsoleKey.End: _editPos = _editBuf.Length; break;
            default:
                if (key.KeyChar >= ' ' && key.KeyChar <= '~')
                { _editBuf = _editBuf[.._editPos] + key.KeyChar + _editBuf[_editPos..]; _editPos++; }
                break;
        }
    }

    private static void SaveEdit()
    {
        var items = _groups[_catOrder[_catIdx]];
        SetValue(items[_itemIdx].Key, _editBuf);
    }

    private static string GetValue(string key) => key switch
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

    private static void SetValue(string key, string value)
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

    // ================================================================
    // 渲染
    // ================================================================

    private static void Render()
    {
        var (tw, th) = (Console.WindowWidth, Console.WindowHeight);
        var sb = new System.Text.StringBuilder();
        sb.Append("[?25l[2J[H");

        // 顶栏
        sb.Append($"[44;37m 设置 / 配置{new string(' ', Math.Max(0, tw - VW(" 设置 / 配置")))}[0m\r\n");
        sb.Append($"[36m{new string('─', tw)}[0m\r\n");

        int catW = 18, detailX = catW + 1;
        var cat = _catOrder[_catIdx];
        var items = _groups[cat];

        // ---- 左侧类别 ----
        for (int i = 0; i < _catOrder.Length; i++)
        {
            var cn = _catOrder[i];
            sb.Append($"[{i + 3};1H");
            if (i == _catIdx && !_right)
                sb.Append($"[30;46m {cn}{new string(' ', catW - VW(cn) - 1)}[0m");
            else if (i == _catIdx)
                sb.Append($"[46m {cn}{new string(' ', catW - VW(cn) - 1)}[0m");
            else
                sb.Append($" {cn}");
            sb.Append("[K");
        }

        // 分隔线
        for (int i = 0; i < th - 4; i++)
            sb.Append($"[{i + 3};{catW}H[36m│[0m");

        // ---- 右侧详情 ----
        int detailH = th - 6;
        var scroll = Math.Clamp(_itemIdx - detailH / 3, 0, Math.Max(0, items.Count - detailH / 3));

        for (int i = 0; i < detailH; i++)
        {
            int si = scroll + i;
            if (si >= items.Count) break;
            var s = items[si];
            var isSel = si == _itemIdx && _right;
            int row = i * 3 + 3;

            // 标签
            sb.Append($"[{row};{detailX + 1}H");
            sb.Append(isSel ? $"[30;46m {s.Label} [0m" : $" [1m{s.Label}[0m");

            // 值
            int valRow = row + 1;
            if (valRow < th - 2)
            {
                sb.Append($"[{valRow};{detailX + 2}H");
                var val = GetValue(s.Key);

                if (isSel && _editing)
                    sb.Append($"[7m {_editBuf} [0m");
                else if (s.Type == "secret" && val.Length > 0)
                    sb.Append($"[2m••••••••[0m");
                else if (s.Type == "select")
                    sb.Append($"[36m{val}  ▾[0m");
                else
                    sb.Append($"[36m{val}[0m");
            }

            // 描述
            int descRow = valRow + 1;
            if (descRow < th - 2)
                sb.Append($"[{descRow};{detailX + 2}H[2m{s.Desc}  [{s.EnvVar}][0m");
        }

        // 底栏
        var hint = " ↑↓ 选择  ←→ 切换面板  Enter 修改  Ctrl+S 保存  Esc 退出";
        sb.Append($"[{th - 1};1H[100m[37m{hint}{new string(' ', Math.Max(0, tw - VW(hint)))}[0m");
        if (_editing)
            sb.Append($"[{th};1H[100m[33m 编辑中: Enter 保存  Esc 取消{new string(' ', Math.Max(0, tw - VW(" 编辑中: Enter 保存  Esc 取消")))}[0m");
        else
            sb.Append($"[{th};1H[K");

        // 光标
        if (_editing && _right)
        {
            var itemRow = (_itemIdx - scroll) * 3 + 3;
            int col = detailX + 3 + _editPos;
            sb.Append($"[{itemRow + 1};{col}H[?25h");
        }

        Console.Write(sb.ToString());
    }

    private static int VW(string s) { int w = 0; foreach (var r in s.EnumerateRunes()) w += r.Value > 127 ? 2 : 1; return w; }
}
