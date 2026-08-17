using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.Tui.Controls;

namespace WayCoder.UI.TUI.Custom;

/// <summary>
/// 模型选择对话框 —— 居中带边框对话框（非全屏）。
/// 橙→黄渐变外框（对标权限确认对话框 GradOrangeYellow）、多列模型列表、实时搜索、大/小模型切换、槽位分配。
///
/// 功能：
///   - 大/小模型切换（Tab）、全部/指定槽位（A / 1-0）、实时搜索过滤
///   - 多列列表（🔑 key 状态 / 模型 / 厂商 / 窗口 / 价格 / 大 ✓ / 小 ✓）
///   - 底部槽位状态条（▶ 目标、* 已配置、· 当前）
///
/// 实现：TuiWindow（模态）+ TuiVBox + TuiHBox（搜索）+ TuiInput + TuiTableList + TuiLabel，
/// 走 UxHelper.RenderWait 阻塞 → 事件桥接，不再自造 Console.ReadKey 循环。
/// 顺带修复旧实现「光标泄漏 + 窄终端溢出」两处隐患。
/// </summary>
public static class ModelPicker
{
    public record ModelEntry(string Id, string DisplayName, string Provider,
        string ProviderId, bool HasApiKey, int ContextWindow, double InputPrice);
    public record Result(string ModelId, bool IsLarge, int TargetSlot,
        bool NeedsApiKey = false, string? ProviderId = null);

    private const int MinW = 62, MinH = 16;

    // 列宽（名称列弹性，其余固定）
    private const int keyW = 2, provW = 11, ctxW = 6, priceW = 7, largeW = 2, smallW = 2;

    /// <summary>
    /// 显示模型选择对话框。
    /// </summary>
    /// <param name="currentSlot">当前槽位索引：0-9=槽位F1-F10, -1=全局默认</param>
    public static Result? Show(int currentSlot = -1)
    {
        Result? result = null;
        using var evt = new ManualResetEventSlim(false);
        try
        {
            var screen = TuiManager.Instance?.ActiveScreen;
            var win = BuildWindow(currentSlot, screen, r => { result = r; evt.Set(); });
            screen?.ShowWindow(win);
            UxHelper.RenderWait(screen, evt, 60_000, win);
        }
        catch { evt.Set(); }
        return result;
    }

    // ── 窗口构建 ──

    private static TuiWindow BuildWindow(int currentSlot, TuiScreen? screen, Action<Result?> onDone)
    {
        var cfg = Config.Instance;
        int winW = Math.Min(Tty.Cols - 2, Math.Max(MinW, Tty.Cols * 2 / 3));
        int winH = Math.Min(Tty.Rows - 2, Math.Max(MinH, Tty.Rows * 2 / 3));
        int listW = Math.Max(10, winW - 2);                                    // 内容区宽（去左右边框）
        int listH = Math.Max(5, winH - 5);                                     // 列表行数（内容=搜索+列表+槽位+帮助）
        int nameW = Math.Max(8, listW - 1 - keyW - provW - ctxW - priceW - largeW - smallW);

        var win = new TuiWindow
        {
            Title = "选择模型",
            Modal = true, HasMask = true,
            Border = WindowBorder.Solid,
            BorderColor = TuiTheme.Current.DialogInfoBorder,
            WinBg = TuiTheme.Current.WindowBg,
            Width = winW, Height = winH,
            MinWidth = MinW, MinHeight = MinH,
            WindowHAlign = EHAlign.Center,
            WindowVAlign = EVAlign.Middle,
        };
        var g = TuiTheme.Current.GradOrangeYellow;
        win.GradientBorder = true;
        win.GradientStart = g.start;
        win.GradientEnd = g.end;

        // ── 状态 ──
        var models = GetAvailableModels();
        var filtered = new List<ModelEntry>();
        string large = cfg.Model, small = cfg.SmallModel;
        bool isLarge = true;
        int targetSlot = currentSlot; // -2=全部, -1=全局, 0-9=具体槽位

        // 搜索行（标签 + 输入框，输入框聚焦）
        var search = new TuiInput
        {
            Height = 1,
            Flex = 1,
            Fg = AnsiColors.White, Bg = AnsiColors.BgBlack,
            Focused = true,
        };
        var searchRow = new TuiHBox { Spacing = 1 };
        searchRow.Add(new TuiLabel("搜索:") { Width = 6, Fg = AnsiColors.BrightBlack });
        searchRow.Add(search);

        // 模型列表（多列）
        var table = new TuiTableList { Height = listH };
        table.AddColumn("🔑", keyW);
        table.AddColumn("模型", nameW);
        table.AddColumn("厂商", provW);
        table.AddColumn("窗口", ctxW);
        table.AddColumn("价格", priceW);
        table.AddColumn("大", largeW);
        table.AddColumn("小", smallW);

        // 槽位状态条 + 帮助行
        var slotBar = new TuiLabel { Height = 1, Fg = AnsiColors.BrightBlack };
        var help = new TuiLabel { Height = 1, Fg = AnsiColors.BrightBlack };

        var vbox = new TuiVBox { ChildHAlign = EHAlign.Stretch };
        vbox.Add(searchRow);
        vbox.Add(table);
        vbox.Add(slotBar);
        vbox.Add(help);
        win.RootView = vbox;

        // ── 标题 / 刷新 ──

        string SlotLabel(int slot) => slot switch { -2 => " — 全部槽位", >= 0 => $" — F{slot + 1} 槽位", _ => "" };
        string TitleText() => (isLarge ? "🤖 选择大模型 (复杂任务)" : "🔧 选择小模型 (简单任务)") + SlotLabel(targetSlot);
        void SyncModels() => (large, small) = ResolveSlotModels(targetSlot, large, small);

        void Refresh(bool resetToCurrent)
        {
            SyncModels();
            filtered = string.IsNullOrEmpty(search.Text)
                ? models
                : models.Where(m =>
                    m.DisplayName.Contains(search.Text, StringComparison.OrdinalIgnoreCase) ||
                    m.Id.Contains(search.Text, StringComparison.OrdinalIgnoreCase) ||
                    m.Provider.Contains(search.Text, StringComparison.OrdinalIgnoreCase)).ToList();

            table.ClearRows();
            foreach (var m in filtered)
            {
                bool isL = m.Id == large, isS = m.Id == small;
                table.AddRow(
                    m.HasApiKey ? "🔑" : "  ",
                    m.DisplayName,
                    m.Provider,
                    FmtCtx(m.ContextWindow).PadLeft(ctxW),
                    FmtPrice(m.InputPrice).PadLeft(priceW),
                    isL ? "✓" : " ",
                    isS ? "✓" : " ");
            }

            table.SelectedIndex = 0;
            if (resetToCurrent)
            {
                string cur = isLarge ? large : small;
                for (int i = 0; i < filtered.Count; i++)
                    if (filtered[i].Id == cur) { table.SelectedIndex = i; break; }
            }
            table.ScrollOffset = 0;
            table.EnsureSelectedVisible();

            win.Title = TitleText();
            slotBar.Text = SlotBarText(targetSlot, currentSlot);
            help.Text = "↑↓ 导航  Enter 选择  Esc 取消  Tab 大/小  A 全部  1-0 槽位  输入搜索";
            screen?.MarkDirty();
        }

        // ── 动作 ──

        void Finish(Result? r)
        {
            onDone(r);
            win.OnClosed?.Invoke();
        }
        void Commit()
        {
            var r = EnterOrPromptKey(filtered, table.SelectedIndex, isLarge, targetSlot);
            if (r != null) Finish(r);
        }
        void ToggleMode()
        {
            isLarge = !isLarge;
            search.Text = "";
            Refresh(true);
        }
        void ToggleAllSlots()
        {
            targetSlot = targetSlot == -2 ? currentSlot : -2;
            Refresh(true);
        }
        void SetSlot(int slot)
        {
            targetSlot = slot;
            Refresh(true);
        }

        // 搜索输入：字母进过滤词（OnTextChanged 实时过滤），↑↓ 导航列表，Enter 选择；
        // A / 数字键在「筛选空或 Ctrl」时切换槽位，否则作为普通字符输入过滤词。
        search.OnTextChanged = () => Refresh(false);
        search.KeyHook = key =>
        {
            bool hasCtrl = (key.Modifiers & ConsoleModifiers.Control) != 0;
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.DownArrow:
                case ConsoleKey.Home:
                case ConsoleKey.End:
                case ConsoleKey.PageUp:
                case ConsoleKey.PageDown:
                    table.OnKey(key);
                    table.MarkDirty();
                    screen?.MarkDirty();
                    return true;
                case ConsoleKey.Enter:
                    Commit();
                    return true;
                case ConsoleKey.A:
                    if (hasCtrl || search.Text.Length == 0) { ToggleAllSlots(); return true; }
                    return false;
                default:
                    if (TrySlotKey(key, out int slot) && (hasCtrl || search.Text.Length == 0))
                    { SetSlot(slot); return true; }
                    return false;
            }
        };
        table.OnSelect = _ => Commit(); // 若焦点切到列表，Enter 亦可选择

        win.RegisterShortcut(ConsoleKey.Tab, ToggleMode);
        win.RegisterShortcut(ConsoleKey.Escape, () => Finish(null));

        Refresh(true);
        return win;
    }

    // ═══════════════════════════════════════════════════
    // 模型操作（纯逻辑）
    // ═══════════════════════════════════════════════════

    /// <summary>Enter：无 key 则返回 NeedsApiKey，有 key 则直接应用</summary>
    private static Result? EnterOrPromptKey(List<ModelEntry> models, int idx, bool isLarge, int slot)
    {
        if (idx < 0 || idx >= models.Count) return null;
        var m = models[idx];
        if (!m.HasApiKey)
        {
            // 返回 NeedsApiKey，由调用方弹出输入框
            return new(m.Id, isLarge, slot, NeedsApiKey: true, ProviderId: m.ProviderId);
        }
        Apply(m.Id, isLarge, slot);
        return new(m.Id, isLarge, slot);
    }

    private static bool TrySlotKey(ConsoleKeyInfo key, out int slot)
    {
        slot = key.Key switch
        {
            ConsoleKey.D1 or ConsoleKey.NumPad1 => 0,
            ConsoleKey.D2 or ConsoleKey.NumPad2 => 1,
            ConsoleKey.D3 or ConsoleKey.NumPad3 => 2,
            ConsoleKey.D4 or ConsoleKey.NumPad4 => 3,
            ConsoleKey.D5 or ConsoleKey.NumPad5 => 4,
            ConsoleKey.D6 or ConsoleKey.NumPad6 => 5,
            ConsoleKey.D7 or ConsoleKey.NumPad7 => 6,
            ConsoleKey.D8 or ConsoleKey.NumPad8 => 7,
            ConsoleKey.D9 or ConsoleKey.NumPad9 => 8,
            ConsoleKey.D0 or ConsoleKey.NumPad0 => 9,
            _ => -1
        };
        return slot >= 0;
    }

    private static void Apply(string modelId, bool isLarge, int slot)
    {
        var cfg = Config.Instance;
        if (slot == -1)
        {
            if (isLarge) cfg.Model = modelId; else cfg.SmallModel = modelId;
            cfg.SaveToEnvFile();
        }
        else if (slot == -2)
        {
            AgentSlotConfig.SetUniform(new AgentSlotConfig.SlotConfig
            { UseGlobal = false, LargeModel = isLarge ? modelId : null, SmallModel = isLarge ? null : modelId });
            if (isLarge) cfg.Model = modelId; else cfg.SmallModel = modelId;
            cfg.SaveToEnvFile();
        }
        else if (slot is >= 0 and < 10)
        {
            var e = AgentSlotConfig.Get(slot);
            AgentSlotConfig.Set(slot, new AgentSlotConfig.SlotConfig
            {
                UseGlobal = false,
                LargeModel = isLarge ? modelId : e.LargeModel,
                SmallModel = isLarge ? e.SmallModel : modelId,
                BaseUrl = e.BaseUrl, ApiKeyProviderId = e.ApiKeyProviderId, ApiKey = e.ApiKey,
            });
        }
    }

    /// <summary>根据 targetSlot 解析该槽位的大/小模型配置</summary>
    private static (string large, string small) ResolveSlotModels(int targetSlot, string fallbackLarge, string fallbackSmall)
    {
        var cfg = Config.Instance;
        if (targetSlot == -2) return (fallbackLarge, fallbackSmall); // 全部：用当前显示的
        if (targetSlot == -1) return (cfg.Model, cfg.SmallModel);     // 全局
        if (targetSlot is >= 0 and < 10)
        {
            var sc = AgentSlotConfig.Get(targetSlot);
            if (!sc.UseGlobal)
                return (
                    string.IsNullOrEmpty(sc.LargeModel) ? cfg.Model : sc.LargeModel,
                    string.IsNullOrEmpty(sc.SmallModel) ? cfg.SmallModel : sc.SmallModel);
        }
        return (cfg.Model, cfg.SmallModel);
    }

    /// <summary>供应商 → 专属环境变量名映射</summary>
    private static readonly Dictionary<string, string> ProviderEnvVar = new(StringComparer.OrdinalIgnoreCase)
    {
        ["openai"] = "OPENAI_API_KEY",
        ["anthropic"] = "ANTHROPIC_API_KEY",
        ["deepseek"] = "DEEPSEEK_API_KEY",
        ["google"] = "GOOGLE_API_KEY",
        ["qwen"] = "DASHSCOPE_API_KEY",
        ["moonshot"] = "MOONSHOT_API_KEY",
        ["zhipu"] = "ZHIPU_API_KEY",
        ["bytedance"] = "ARK_API_KEY",
        ["01ai"] = "YI_API_KEY",
        ["xai"] = "XAI_API_KEY",
        ["mistral"] = "MISTRAL_API_KEY",
        ["siliconflow"] = "SILICONFLOW_API_KEY",
        ["meta"] = "META_API_KEY",
    };

    /// <summary>检查指定模型是否有 API Key</summary>
    private static bool ModelHasKey(string providerId, string modelId)
    {
        // 1. ApiKeyStore 显式存储（按供应商）
        if (!string.IsNullOrEmpty(ApiKeyStore.Get(providerId)))
            return true;
        // 2. 供应商专属环境变量（如 DEEPSEEK_API_KEY）
        if (ProviderEnvVar.TryGetValue(providerId, out var envVar))
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar)))
                return true;
        }
        // 3. 通用模式：{PROVIDER}_API_KEY
        var genericEnv = $"{providerId}_API_KEY".ToUpperInvariant().Replace('-', '_').Replace(' ', '_');
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(genericEnv)))
            return true;
        // 4. 全局 WAYCODER_API_KEY → 仅当前配置的大小模型
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYCODER_API_KEY")))
        {
            var cfg = Config.Instance;
            if (modelId == cfg.Model || modelId == cfg.SmallModel)
                return true;
        }
        // 5. Local/Custom 不需要 key
        if (providerId is "local" or "custom") return true;

        return false;
    }

    private static List<ModelEntry> GetAvailableModels()
    {
        var list = new List<ModelEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var info in ModelCatalog.All)
        {
            if (!seen.Add(info.Id)) continue;
            var hasKey = ModelHasKey(info.ProviderId, info.Id);
            list.Add(new(info.Id, info.DisplayName, info.Provider, info.ProviderId, hasKey, info.ContextWindow, info.InputPrice));
        }
        if (!string.IsNullOrEmpty(Config.Instance.FallbackChain))
            foreach (var m in Config.Instance.FallbackChain.Split(','))
            { var t = m.Trim(); if (!string.IsNullOrEmpty(t) && seen.Add(t)) list.Add(new(t, t, "自定义", "custom", true, 128_000, 0)); }
        return list;
    }

    // ═══════════════════════════════════════════════════
    // 格式化
    // ═══════════════════════════════════════════════════

    private static string FmtCtx(int t) => t switch
    {
        <= 0 => "   -", >= 1_000_000 => $"{t / 1_000_000.0:0.#}M".PadLeft(4),
        _ => $"{t / 1_000}K".PadLeft(4),
    };

    private static string FmtPrice(double p) => p switch
    {
        <= 0 => "Free", < 0.01 => "<$0.01", _ => $"${p:F2}",
    };

    /// <summary>槽位状态条文本（▶ 目标、* 已配置、· 当前槽位）。</summary>
    private static string SlotBarText(int targetSlot, int currentSlot)
    {
        var sb = new StringBuilder();
        sb.Append("槽位 ");
        sb.Append(targetSlot == -2 ? "▶A" : currentSlot == -1 ? "·A" : " A");
        for (int i = 0; i < 10; i++)
        {
            var sc = AgentSlotConfig.Get(i);
            bool hasCfg = !sc.UseGlobal;
            bool isTarget = i == targetSlot;
            bool isCur = i == currentSlot;
            string label = i == 9 ? "0" : (i + 1).ToString();
            char mark = isTarget ? '▶' : hasCfg ? '*' : isCur ? '·' : ' ';
            sb.Append(' ').Append(mark).Append(label);
        }
        return sb.ToString();
    }
}
