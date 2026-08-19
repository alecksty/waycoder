using System.Net.Http;
using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI;
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

        // 标记加载：结构/ids 来自 modelpicker.tui（布局写标记），动态内容与事件 code-behind
        var res = TuiMarkup.LoadResource("dialogs/modelpicker.tui");
        var win = res.Window ?? throw new InvalidOperationException("modelpicker.tui 根应为 Dialog");
        win.Width = winW; win.Height = winH;
        win.MinWidth = MinW; win.MinHeight = MinH;
        win.WinBg = AnsiColors.BgBlack; // 黑色背景（用户要求）
        win.GradientBorder = false;      // 黑底去橙黄渐变边框
        win.BorderColor = AnsiColors.BrightBlack; // 暗色边框

        // 控件接线（结构在标记里，精确样式/列/数据/事件在此）
        var search = res.Find<TuiInput>("search")!;
        var table = res.Find<TuiTableList>("table")!;
        var slotBar = res.Find<TuiLabel>("slotBar")!;
        var help = res.Find<TuiLabel>("help")!;
        var help2 = res.Find<TuiLabel>("help2")!;
        search.Fg = AnsiColors.White;
        search.Bg = AnsiColors.BgBlack;
        table.Height = listH;
        table.AddColumn("🔑", keyW);
        table.AddColumn("模型", nameW);
        table.AddColumn("厂商", provW);
        table.AddColumn("窗口", ctxW);
        table.AddColumn("价格", priceW);
        table.AddColumn("大", largeW);
        table.AddColumn("小", smallW);

        // ── 状态 ──
        List<ModelEntry> models = GetAvailableModels();
        var filtered = new List<ModelEntry>();
        var rowModels = new List<ModelEntry?>();     // table 行 → 模型（组头行 null）
        var modelLock = new object();                 // 保护 models（导入/扫描后台刷新）
        var scanLock = new object();
        var scanResult = new Dictionary<string, bool>(); // providerId → 连通
        string large = cfg.Model, small = cfg.SmallModel;
        bool isLarge = true;
        int targetSlot = currentSlot; // -2=全部, -1=全局, 0-9=具体槽位

        // ── 标题 / 刷新 ──

        string SlotLabel(int slot) => slot switch { -2 => " — 全部槽位", >= 0 => $" — F{slot + 1} 槽位", _ => "" };
        string TitleText() => (isLarge ? "🤖 选择大模型 (复杂任务)" : "🔧 选择小模型 (简单任务)") + SlotLabel(targetSlot);
        void SyncModels() => (large, small) = ResolveSlotModels(targetSlot, large, small);

        void Refresh(bool resetToCurrent)
        {
            SyncModels();
            List<ModelEntry> src;
            lock (modelLock) src = models;
            filtered = string.IsNullOrEmpty(search.Text)
                ? src
                : src.Where(m =>
                    m.DisplayName.Contains(search.Text, StringComparison.OrdinalIgnoreCase) ||
                    m.Id.Contains(search.Text, StringComparison.OrdinalIgnoreCase) ||
                    m.Provider.Contains(search.Text, StringComparison.OrdinalIgnoreCase) ||
                    m.ProviderId.Contains(search.Text, StringComparison.OrdinalIgnoreCase)).ToList();

            // 按供应商分组渲染（组头行 + 扫描状态）
            table.ClearRows();
            rowModels.Clear();
            foreach (var group in filtered.GroupBy(m => m.ProviderId).OrderBy(g => g.Key))
            {
                bool ok; bool hasScan;
                lock (scanLock) { hasScan = scanResult.TryGetValue(group.Key, out ok); }
                var mark = hasScan ? (ok ? "  ✅ 连通" : "  ❌ 不通") : "";
                table.AddGroupHeader(group.Key + mark);
                rowModels.Add(null);
                foreach (var m in group)
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
                    rowModels.Add(m);
                }
            }

            table.SelectedIndex = table.NextSelectable(0);
            if (resetToCurrent && rowModels.Count > 0)
            {
                string cur = isLarge ? large : small;
                for (int i = 0; i < rowModels.Count; i++)
                    if (rowModels[i]?.Id == cur) { table.SelectedIndex = i; break; }
            }
            table.ScrollOffset = 0;
            table.EnsureSelectedVisible();

            win.Title = TitleText();
            slotBar.Text = SlotBarText(targetSlot, currentSlot);
            help.Text = "↑↓选择  Enter确认  Tab大/小  S扫描  I导入  O在线  K设key  L清key";
            help2.Text = "Ctrl+A添加  Del删除  Ctrl+E编辑  输入过滤: 名称/厂商/供应商";
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
            var m = table.SelectedIndex >= 0 && table.SelectedIndex < rowModels.Count
                ? rowModels[table.SelectedIndex] : null;
            if (m == null) return; // 组头行不可选
            var r = EnterOrPromptKey(m, isLarge, targetSlot);
            if (r != null) Finish(r);
        }

        // ── 后台操作：扫描/导入/OpenCode（结果 lock 保护，下次 Refresh 读取）──
        void TriggerScan()
        {
            help.Text = "📡 扫描连通性中…（完成后按 ↑↓/输入刷新）";
            help2.Text = "";
            screen?.MarkDirty();
            Task.Run(() =>
            {
                var dict = new Dictionary<string, bool>();
                try
                {
                    foreach (var p in ModelCli.TestList()) dict[p.ProviderId] = p.Ok;
                }
                catch { }
                lock (scanLock) { scanResult = dict; }
                screen?.MarkDirty();
            });
        }

        void TriggerImport(string kind)
        {
            help.Text = kind == "opencode" ? "🌐 OpenCode 在线导入中…" : "📥 自动导入中…";
            help2.Text = "";
            screen?.MarkDirty();
            Task.Run(() =>
            {
                string report;
                try
                {
                    if (kind == "opencode")
                    {
                        const string url = "https://opencode.ai/zen/go/v1/models";
                        const string apiBase = "https://opencode.ai/zen/go/v1";
                        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
                        client.DefaultRequestHeaders.UserAgent.ParseAdd("WayCoder/1.0");
                        var json = client.GetStringAsync(url).GetAwaiter().GetResult();
                        var list = ModelCatalog.ImportOpenCodeApi(json, apiBase);
                        var builtIn = new HashSet<string>(ModelCatalog.BuiltIn.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
                        var toAdd = list.Where(x => !builtIn.Contains(x.Id)).ToList();
                        ModelCatalog.AddCustomRange(toAdd);
                        report = $"✅ OpenCode 导入 {toAdd.Count} 个模型" + (list.Count - toAdd.Count > 0 ? $"，跳过 {list.Count - toAdd.Count} 内置" : "");
                    }
                    else
                    {
                        report = ModelCli.Import(null);
                    }
                }
                catch (Exception ex) { report = $"❌ 导入失败: {ex.Message}"; }
                try
                {
                    var fresh = GetAvailableModels();
                    lock (modelLock) { models = fresh; }
                }
                catch { }
                help.Text = report + "（按 ↑↓/输入刷新）";
            help2.Text = "";
                screen?.MarkDirty();
            });
        }

        void ClearKeyForSelected()
        {
            var m = table.SelectedIndex >= 0 && table.SelectedIndex < rowModels.Count
                ? rowModels[table.SelectedIndex] : null;
            if (m == null) return;
            if (m.ProviderId == "local") { help.Text = "本地模型无需 API Key";
            help2.Text = ""; return; }
            try { ApiKeyStore.Remove(m.ProviderId); } catch { }
            ReconfigureAgent(m.ProviderId, Config.Instance.ApiKey ?? ""); // 回退全局 key
            help.Text = $"已清除 {m.ProviderId} 的 Key";
            help2.Text = "";
            Refresh(false);
        }

        /// <summary>添加自定义模型（Ctrl+A）：弹输入框，格式 模型名|ProviderId|BaseUrl。</summary>
        void PromptAddModel()
        {
            var inputWin = TuiDialog.InputLine("➕ 添加模型",
                "格式: 模型名|ProviderId|BaseUrl（可空）", "",
                text =>
                {
                    var parts = (text ?? "").Split('|');
                    var id = parts.Length > 0 ? parts[0].Trim() : "";
                    if (string.IsNullOrWhiteSpace(id)) { help.Text = "❌ 模型名不能为空"; help2.Text = ""; return; }
                    var pid = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1].Trim() : "custom";
                    var baseUrl = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2].Trim() : null;
                    ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
                        id, id, pid, pid, "*", "Custom", 0, 0, 0, baseUrl, "手动添加", 0));
                    help.Text = $"✅ 已添加模型 {id}";
                    help2.Text = "";
                    Refresh(false);
                });
            screen?.ShowWindow(inputWin);
            screen?.MarkDirty();
        }

        /// <summary>删除选中自定义模型（Delete）：确认后从库移除（内置不可删）。</summary>
        void DeleteSelectedModel()
        {
            var m = table.SelectedIndex >= 0 && table.SelectedIndex < rowModels.Count
                ? rowModels[table.SelectedIndex] : null;
            if (m == null) return;
            if (ModelCatalog.BuiltIn.Any(b => b.Id == m.Id))
            { help.Text = "⚠ 内置模型不可删除"; help2.Text = ""; return; }
            var confirmWin = TuiDialog.Confirm("🗑 删除模型", $"删除 {m.Id}？（自定义模型，删除后不可恢复）", ok =>
            {
                if (ok) { ModelCatalog.RemoveCustom(m.Id); help.Text = $"✅ 已删除 {m.Id}";
                help2.Text = ""; Refresh(false); }
            });
            screen?.ShowWindow(confirmWin);
            screen?.MarkDirty();
        }

        /// <summary>编辑选中自定义模型（Ctrl+E）：预填当前值，改后覆盖保存。</summary>
        void PromptEditModel()
        {
            var m = table.SelectedIndex >= 0 && table.SelectedIndex < rowModels.Count
                ? rowModels[table.SelectedIndex] : null;
            if (m == null) return;
            if (ModelCatalog.BuiltIn.Any(b => b.Id == m.Id))
            { help.Text = "⚠ 内置模型不可编辑（选自定义模型）"; help2.Text = ""; return; }
            var info = ModelCatalog.Find(m.Id);
            var prefill = $"{m.Id}|{m.ProviderId}|{info?.DefaultBaseUrl ?? ""}";
            var inputWin = TuiDialog.InputLine($"✏️ 编辑模型 {m.Id}",
                "格式: 模型名|ProviderId|BaseUrl", prefill, text =>
                {
                    var parts = (text ?? "").Split('|');
                    var id = parts.Length > 0 ? parts[0].Trim() : m.Id;
                    var pid = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1].Trim() : m.ProviderId;
                    var baseUrl = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2].Trim() : null;
                    ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
                        id, id, pid, pid, "*", "Custom", 0, 0, 0, baseUrl, "手动编辑", 0));
                    help.Text = $"✅ 已保存模型 {id}";
                    help2.Text = "";
                    Refresh(false);
                });
            screen?.ShowWindow(inputWin);
            screen?.MarkDirty();
        }

        /// <summary>设置/清除 key 后重配当前 Agent（运行时生效，无需重启）。</summary>
        void ReconfigureAgent(string providerId, string key)
        {
            var agent = ProgramContext.Agent;
            if (agent == null) return;
            var cfg = Config.Instance;
            var info = ModelCatalog.Find(agent.LlmClient.Model);
            var baseUrl = info?.DefaultBaseUrl ?? cfg.BaseUrl;
            agent.LlmClient.Reconfigure(string.IsNullOrEmpty(key) ? (ApiKeyStore.Get(providerId) ?? cfg.ApiKey) : key, baseUrl);
        }

        void PromptKeyForSelected()
        {
            var m = table.SelectedIndex >= 0 && table.SelectedIndex < rowModels.Count
                ? rowModels[table.SelectedIndex] : null;
            if (m == null) return;
            if (m.ProviderId == "local") { help.Text = "本地模型无需 API Key";
            help2.Text = ""; return; }
            var current = "";
            try { current = ApiKeyStore.Get(m.ProviderId) ?? ""; } catch { }
            // 在模型窗口之上再弹子输入框（RenderWait 循环会把按键路由到当前 active 窗口）
            var inputWin = TuiDialog.InputLine(
                $"🔑 设置 {m.ProviderId} 的 API Key",
                "输入 API Key（Enter 保存修改，Esc 取消）",
                current,
                text =>
                {
                    try { ApiKeyStore.Set(m.ProviderId, text.Trim()); } catch { }
                    ReconfigureAgent(m.ProviderId, text.Trim()); // 运行时生效
                    help.Text = $"已保存 {m.ProviderId} 的 Key";
            help2.Text = "";
                    screen?.MarkDirty();
                    Refresh(false);
                });
            screen?.ShowWindow(inputWin);
            screen?.MarkDirty();
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
                    if (hasCtrl) { PromptAddModel(); return true; } // Ctrl+A 添加模型
                    if (search.Text.Length == 0) { ToggleAllSlots(); return true; } // 空搜索 A 全部槽位
                    return false;
                case ConsoleKey.S:
                    if (hasCtrl || search.Text.Length == 0) { TriggerScan(); return true; }
                    return false;
                case ConsoleKey.I:
                    if (hasCtrl || search.Text.Length == 0) { TriggerImport("import"); return true; }
                    return false;
                case ConsoleKey.O:
                    if (hasCtrl || search.Text.Length == 0) { TriggerImport("opencode"); return true; }
                    return false;
                case ConsoleKey.L:
                    if (hasCtrl || search.Text.Length == 0) { ClearKeyForSelected(); return true; }
                    return false;
                case ConsoleKey.K:
                    if (hasCtrl || search.Text.Length == 0) { PromptKeyForSelected(); return true; }
                    return false;
                case ConsoleKey.E:
                    if (hasCtrl) { PromptEditModel(); return true; }
                    return false;
                case ConsoleKey.Delete:
                    if (search.Text.Length == 0) { DeleteSelectedModel(); return true; }
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
    private static Result? EnterOrPromptKey(ModelEntry m, bool isLarge, int slot)
    {
        if (m == null) return null;
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

    /// <summary>应用选中模型到配置/槽位（public 供 /model 命令等复用）。</summary>
    public static void Apply(string modelId, bool isLarge, int slot)
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
