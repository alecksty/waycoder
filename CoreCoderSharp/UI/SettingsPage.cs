namespace CoreCoderSharp.UI;

/// <summary>
/// 全屏设置界面 — 左大类 + 右详情。
/// ↑↓选择类别, ←→切换面板, Enter编辑, Esc退出。
/// </summary>
public static class SettingsPage
{
    private record Category(string Name, string Icon, List<Setting> Items);
    private record Setting(string Label, string Value, string Key, string Desc, bool Secret = false, bool ReadOnly = false);

    private static List<Category> _cats = [];
    private static int _catIdx, _itemIdx;
    private static bool _focusRight; // false=左边选类, true=右边选设置项
    private static bool _editing;
    private static string _editBuf = "";
    private static int _editPos;

    public static void Show()
    {
        var sm = ScreenManager.Instance;
        var wasActive = sm.IsActive;
        var config = Config.FromEnv();

        _cats =
        [
            new("模型", "🤖", [
                new("模型名称", config.Model, "Model", "deepseek-v4-flash / gpt-5.4-mini ..."),
                new("API 地址", config.BaseUrl ?? "", "BaseUrl", "API 端点 URL"),
                new("API 密钥", config.ApiKey ?? "", "ApiKey", "已隐藏", Secret: true),
            ]),
            new("参数", "⚙", [
                new("最大 Token", config.MaxTokens.ToString(), "MaxTokens", "每次请求最大 Token"),
                new("温度", config.Temperature.ToString("F1"), "Temperature", "0=精确 1=创意"),
                new("上下文窗口", (config.MaxContextTokens > 0 ? config.MaxContextTokens : 128000).ToString(), "MaxContext", "上下文窗口大小"),
            ]),
            new("预算", "💰", [
                new("预算上限 ($)", (config.MaxBudgetUsd?.ToString("F2") ?? "无限制"), "MaxBudget", "超支自动停止"),
            ]),
          new("权限", "🔐", [
              new("权限模式", PermissionManager.CurrentMode.ToString(), "PermMode", "Ask=确认 Auto=自动 Yolo=跳过"),
          ]),
          new("调试", "🐛", [
              new("调试日志", DebugLog.Enabled ? "开启" : "关闭", "Debug", "记录到 logs/ 目录", ReadOnly: true),
              new("版本", "v0.11.0", "", "CoreCoderSharp", ReadOnly: true),
          ]),
        ];

        _catIdx = 0; _itemIdx = 0; _focusRight = false;
        _editing = false;

        if (!wasActive) sm.Enter();

        try
        {
            while (true)
            {
                Render();
                var key = Console.ReadKey(intercept: true);
                bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);

                if (_editing)
                {
                    switch (key.Key)
                    {
                        case ConsoleKey.Enter: ApplyEdit(config); _editing = false; break;
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
                else
                {
                    var items = _cats[_catIdx].Items;
                    switch (key.Key)
                    {
                        case ConsoleKey.Escape: return;
                        case ConsoleKey.UpArrow:
                            if (_focusRight && _itemIdx > 0) _itemIdx--;
                            else if (!_focusRight && _catIdx > 0) _catIdx--;
                            break;
                        case ConsoleKey.DownArrow:
                            if (_focusRight && _itemIdx < items.Count - 1) _itemIdx++;
                            else if (!_focusRight && _catIdx < _cats.Count - 1) _catIdx++;
                            break;
                        case ConsoleKey.LeftArrow: _focusRight = false; _itemIdx = 0; break;
                        case ConsoleKey.RightArrow: _focusRight = true; break;
                        case ConsoleKey.Enter:
                            var s = items[_itemIdx];
                            if (s.ReadOnly) break;
                            if (s.Key == "PermMode")
                            {
                                var idx = sm.ShowMenu("选择权限模式", ["Ask (每次确认)", "Auto (智能确认)", "Yolo (上帝模式)"]);
                                if (idx >= 0)
                                {
                                    var modes = new[] { "Ask", "Auto", "Yolo" };
                                    PermissionManager.SetMode(modes[idx]);
                                    items[_itemIdx] = s with { Value = modes[idx] };
                                }
                            }
                            else if (s.Key == "Model")
                            {
                                var models = new List<string> {
                                    "deepseek-v4-flash", "deepseek-v4-pro",
                                    "gpt-5.4-mini", "gpt-5.4", "gpt-5.5",
                                    "gpt-4o", "gpt-4o-mini",
                                };
                                var mi = sm.ShowMenu("选择模型", models);
                                if (mi >= 0) { items[_itemIdx] = s with { Value = models[mi] }; ApplyEdit(config); }
                            }
                            else
                            {
                                _editing = true;
                                _editBuf = s.Secret ? "" : s.Value;
                                _editPos = _editBuf.Length;
                            }
                            break;
                    }
                }
            }
        }
        finally
        {
            if (!wasActive) sm.Exit(); else sm.Render();
        }
    }

    private static void ApplyEdit(Config config)
    {
        var items = _cats[_catIdx].Items;
        var s = items[_itemIdx];
        items[_itemIdx] = s with { Value = _editBuf };

        switch (s.Key)
        {
            case "Model": config.Model = _editBuf; break;
            case "BaseUrl": config.BaseUrl = _editBuf; break;
            case "ApiKey": config.ApiKey = _editBuf; break;
            case "MaxTokens": if (int.TryParse(_editBuf, out var mt)) config.MaxTokens = mt; break;
            case "Temperature": if (float.TryParse(_editBuf, out var ft)) config.Temperature = ft; break;
            case "MaxContext": if (int.TryParse(_editBuf, out var mc)) config.MaxContextTokens = mc; break;
            case "MaxBudget": if (double.TryParse(_editBuf, out var mb)) config.MaxBudgetUsd = mb; break;
        }
    }

    private static void Render()
    {
        var (tw, th) = (Console.WindowWidth, Console.WindowHeight);
        var sb = new System.Text.StringBuilder();
        sb.Append("[?25l[2J[H");

        // 顶栏
        sb.Append($"[44;37m 设置 / 配置{new string(' ', Math.Max(0, tw - VW(" 设置 / 配置")))}[0m\r\n");
        sb.Append($"[36m{new string('─', tw)}[0m\r\n");

        int catW = 16, detailX = catW + 1;
        int detailW = tw - catW - 2;

        // 类别列表
        for (int i = 0; i < _cats.Count; i++)
        {
            var cat = _cats[i];
            var label = $" {cat.Icon} {cat.Name}";
            sb.Append($"[{i + 3};1H");
            if (i == _catIdx && !_focusRight)
                sb.Append($"[30;46m{label}{new string(' ', catW - VW(label))}[0m");
            else if (i == _catIdx)
                sb.Append($"[46m{label}{new string(' ', catW - VW(label))}[0m");
            else
                sb.Append($"[2m{label}[0m");
            sb.Append("[K");
        }

        // 分隔竖线
        for (int i = 0; i < th - 4; i++)
            sb.Append($"[{i + 3};{catW}H[36m│[0m");

        // 详情
        var items = _cats[_catIdx].Items;
        var scroll = Math.Clamp(_itemIdx - (th - 7) / 2, 0, Math.Max(0, items.Count - (th - 7)));
        for (int i = 0; i < Math.Min(th - 6, items.Count); i++)
        {
            int si = scroll + i;
            if (si >= items.Count) break;
            var s = items[si];
            var isSel = si == _itemIdx && _focusRight;
            int row = i + 3;

            // 标签
            sb.Append($"[{row};{detailX + 1}H");
            if (isSel) sb.Append($"[30;46m {s.Label} [0m");
            else sb.Append($" [1m{s.Label}[0m");

            // 值
            int valY = row + 1;
            if (valY < th - 2)
            {
                sb.Append($"[{valY};{detailX + 2}H");
                if (isSel && _editing)
                {
                    sb.Append($"[7m {_editBuf} [0m");
                }
                else if (s.Secret && s.Value.Length > 0)
                {
                    sb.Append($"[2m••••••••[0m");
                }
                else if (s.ReadOnly)
                {
                    sb.Append($"[2m{s.Value}[0m");
                }
                else
                {
                    sb.Append($"[36m{s.Value}[0m");
                }
            }

            // 描述
            int descY = valY + 1;
            if (descY < th - 2)
                sb.Append($"[{descY};{detailX + 2}H[2m{s.Desc}[0m");
        }

        // 底栏
        var hint = $" ↑↓ 选择  ←→ 切换面板  Enter 修改  Esc 退出";
        sb.Append($"[{th - 1};1H[100m[37m{hint}{new string(' ', Math.Max(0, tw - VW(hint)))}[0m");
        if (_editing)
            sb.Append($"[{th};1H[100m[33m 编辑中: Enter 保存  Esc 取消{new string(' ', Math.Max(0, tw - VW(" 编辑中: Enter 保存  Esc 取消")))}[0m");
        else
            sb.Append($"[{th};1H[K");

        // 光标
        if (_editing && _focusRight)
        {
            var itemRow = _itemIdx - scroll + 3;
            int col = detailX + 3 + _editPos;
            sb.Append($"[{itemRow + 1};{col}H[?25h");
        }

        Console.Write(sb.ToString());
    }

    private static int VW(string s)
    {
        int w = 0;
        foreach (var r in s.EnumerateRunes()) w += r.Value > 127 ? 2 : 1;
        return w;
    }
}
