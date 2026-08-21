using System.Net.Http;
using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Screens;
using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.Tui.Controls;

namespace WayCoder.UI.TUI.Custom;

/// <summary>
/// 模型选择对话框 —— 居中带边框对话框（非全屏）。
/// 橙→黄渐变外框（对标权限确认对话框 GradOrangeYellow）、多列模型列表、实时搜索、大/小模型切换、槽位分配。
///
/// 功能：
///   - 大/小模型切换（Tab）、全部/指定槽位（Ctrl+A / F1-F10）、实时搜索过滤（直接打字）
///   - 多列列表（🔑 key 状态 / 状态列(连通/无key/欠费/不通) / 模型 / 厂商 / 窗口 / 价格 / 大 ✓ / 小 ✓）
///   - 状态列只显示不落盘：由扫描结果实时推导（未扫描=「未测」；无 key=「无key」；402=欠费）
///   - 底部槽位状态条（▶ 目标、* 已配置、· 当前）
///
/// 实现：TuiWindow（模态）+ TuiVBox + TuiHBox（搜索）+ TuiInput + TuiTableList + TuiLabel，
/// 走 UxHelper.RenderWait 阻塞 → 事件桥接，不再自造 Console.ReadKey 循环。
/// 顺带修复旧实现「光标泄漏 + 窄终端溢出」两处隐患。
/// </summary>
public static class ModelPicker
{
    public record ModelEntry(string Id, string DisplayName, string Provider,
        string ProviderId, bool HasApiKey, int ContextWindow, double InputPrice, string? BaseUrl = null);
    public record Result(string ModelId, bool IsLarge, int TargetSlot,
        bool NeedsApiKey = false, string? ProviderId = null, string? BaseUrl = null);

    /// <summary>
    /// 模型连通性状态 —— 仅供状态列/组头显示，由扫描结果实时推导，不写入任何模型文件。
    /// Unknown=未扫描；NoKey=无 API key；Overdue=欠费（HTTP 402）。
    /// </summary>
    public enum ScanStatus { Unknown, Connected, NoKey, BadKey, Overdue, NoEndpoint, Unreachable }

    private const int MinW = 62, MinH = 16;

    // 单元格内右对齐用的宽度（列宽本身在 modelpicker.tui 的 columns 里声明）
    private const int ctxW = 6, priceW = 6;

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

    /// <summary>把标记里的按钮接到动作上（缺 id 静默跳过，标记改名不至于崩窗口）。样式全在 .tui 里。</summary>
    private static void Wire(TuiMarkupResult res, string id, Action action)
    {
        var btn = res.Find<TuiButton>(id);
        if (btn != null) btn.OnClick = _ => action();
    }

    private static TuiWindow BuildWindow(int currentSlot, TuiScreen? screen, Action<Result?> onDone)
    {
        var cfg = Config.Instance;
        int winW = Math.Min(Tty.Cols - 2, Math.Max(MinW, Tty.Cols * 2 / 3));
        int winH = Math.Min(Tty.Rows - 2, Math.Max(MinH, Tty.Rows * 2 / 3));
        // 列宽不在这算了：.tui 里声明比例，TuiTableList.StretchColumns 按控件宽等比铺开

        // 标记加载：结构/ids 来自 modelpicker.tui（布局写标记），动态内容与事件 code-behind
        var res = TuiMarkup.LoadResource("dialogs/modelpicker.tui");
        var win = res.Window ?? throw new InvalidOperationException("modelpicker.tui 根应为 Dialog");
        win.Width = winW; win.Height = winH;
        win.MinWidth = MinW; win.MinHeight = MinH;
        // 背景/边框/渐变全在 modelpicker.tui 里声明（gradient="warning" → 主题 GradOrangeYellow）。
        // 走渐变分支还有个副作用是我们要的：TuiScreen.RenderWindow 只在渐变分支居中标题。

        // 控件接线（结构在标记里，精确样式/列/数据/事件在此）
        var search = res.Find<TuiInput>("search")!;
        var table = res.Find<TuiTableList>("table")!;
        var slotBar = res.Find<TuiLabel>("slotBar")!;
        var help = res.Find<TuiLabel>("help")!;
        var help2 = res.Find<TuiLabel>("help2")!;
        var btnMode = res.Find<TuiButton>("btnMode")!;   // 文本随大/小模型切换，Refresh 里刷
        // 表格高度由 modelpicker.tui 的 flex="1" 交给 VBox 分配（列定义同样在标记的 columns 属性）

        // ── 状态 ──
        List<ModelEntry> models = GetAvailableModels();
        var filtered = new List<ModelEntry>();
        var rowModels = new List<ModelEntry?>();     // table 行 → 模型（组头行 null）
        var modelLock = new object();                 // 保护 models（导入/扫描后台刷新）
        var scanLock = new object();
        var scanResult = new Dictionary<string, ScanStatus>(); // providerId → 扫描状态（不落盘，仅状态列/组头显示）
        string large = cfg.Model, small = cfg.SmallModel;
        bool isLarge = true;
        int targetSlot = currentSlot; // -2=全部, -1=全局, 0-9=具体槽位

        // ── 状态列（纯显示：由扫描结果实时推导，不写模型文件）──
        ScanStatus ScannedStatus(string pid)
        {
            lock (scanLock)
                if (scanResult.TryGetValue(pid, out var s)) return s;
            return ScanStatus.Unknown;
        }

        /// <summary>行状态单元格文本：无key / 欠费 / 不通 / 连通…</summary>
        string StatusCell(ModelEntry m)
        {
            if (m.ProviderId is "local" or "custom") // 无需 key，只有扫描能判断连通
                return ScannedStatus(m.ProviderId) == ScanStatus.Connected ? "✔本地" : "本地";
            if (!m.HasApiKey) return "无key";
            return ScannedStatus(m.ProviderId) switch
            {
                ScanStatus.Connected => "✔连通",
                ScanStatus.BadKey => "✖key",
                ScanStatus.Overdue => "欠费",
                ScanStatus.NoEndpoint => "无端点",
                ScanStatus.Unreachable => "✖不通",
                _ => "未测",
            };
        }

        /// <summary>组头尾缀：供应商级聚合状态（该组任一模型有 key 才算有 key）。</summary>
        string GroupStatusMark(string pid, bool anyKey)
        {
            var st = ScannedStatus(pid);
            if (pid is "local" or "custom")
                return st == ScanStatus.Connected ? "  ✔本地" : "  本地";
            if (!anyKey) return "  无key";
            return st switch
            {
                ScanStatus.Connected => "  ✔连通",
                ScanStatus.BadKey => "  ✖key无效",
                ScanStatus.Overdue => "  💸欠费",
                ScanStatus.NoEndpoint => "  无端点",
                ScanStatus.Unreachable => "  ✖不通",
                _ => "",
            };
        }

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

            // 按供应商分组渲染（组头行 + 聚合状态）
            table.ClearRows();
            rowModels.Clear();
            foreach (var group in filtered.GroupBy(m => m.ProviderId).OrderBy(g => g.Key))
            {
                table.AddGroupHeader(group.Key + GroupStatusMark(group.Key, group.Any(m => m.HasApiKey)));
                rowModels.Add(null);
                foreach (var m in group.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
                {
                    bool isL = m.Id == large, isS = m.Id == small;
                    table.AddRow(
                        m.HasApiKey ? "🔑" : "  ",
                        StatusCell(m),
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
            btnMode.Text = isLarge ? "→小模型" : "→大模型";
            help.Text = "↑↓选择  空格应用不关  Enter确认关闭  保存按钮  Esc取消  F1-F10槽位  打字过滤";
            help2.Text = "^T大小 ^G槽位 ^S扫描 ^R导入 ^O在线 ^P设Key ^L清Key ^N添加 ^U编辑 ^D删除";
            // 数据/标题/组头都改了，必须把窗口根视图标脏 —— 否则增量渲染只画脏控件，
            // 表格行与窗口标题不重绘（搜索过滤输入后列表「不动」就源于此：OnTextChanged→Refresh
            // 改的是 table 的 rows，但 table 与窗口根视图都没标脏，下一帧增量渲染直接跳过它）
            win.RootView.MarkDirty();
            screen?.MarkDirty();
        }

        /// <summary>子对话框（设Key/添加/编辑/删除）关闭后刷新父级对话框：
        /// 重绘表格 + 底部提示（slotBar/help/help2/btnMode）+ 标脏（Refresh 内含 win.RootView.MarkDirty）。
        /// message 非空时在刷新后覆盖 help 作操作结果提示 —— 确认/取消/ESC 都走这里，
        /// 保证返回父级时底部内容是最新的、父级整体被重绘（不留上一帧残影）。</summary>
        void RefreshParent(string? message = null)
        {
            Refresh(false);
            if (!string.IsNullOrEmpty(message))
            {
                help.Text = message;
                help2.Text = "";
            }
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

        /// <summary>空格：应用当前选中模型但保持对话框打开（预览/暂选，可连续试多个模型）。</summary>
        void CommitNoClose()
        {
            var m = table.SelectedIndex >= 0 && table.SelectedIndex < rowModels.Count
                ? rowModels[table.SelectedIndex] : null;
            if (m == null) return; // 组头行不可选
            var r = EnterOrPromptKey(m, isLarge, targetSlot);
            if (r != null)
            {
                Refresh(true); // 应用选中（生效显示），不 Finish 关闭
                help.Text = $"✅ 已应用 {m.Id}（空格换选 / Enter 确认并关闭）";
            }
        }

        // ── 后台操作：扫描/导入/OpenCode（结果 lock 保护，下次 Refresh 读取）──
        void TriggerScan()
        {
            help.Text = "📡 扫描连通性中…";
            help2.Text = "";
            screen?.MarkDirty();
            Task.Run(() =>
            {
                var dict = new Dictionary<string, ScanStatus>();
                try
                {
                    foreach (var p in ModelCli.TestList())
                        dict[p.ProviderId] = ProbeStatus(p);
                }
                catch { }
                lock (scanLock) { scanResult = dict; }
                // 完成后回 UI 线程刷新（组头/状态列实时更新；后台线程不碰控件树）
                if (screen is ChatScreen chat) chat.PostToUI(() => Refresh(false));
                else screen?.MarkDirty();
            });
        }

        void TriggerImport(string kind)
        {
            if (kind == "import")
            {
                // 本地导入：弹框勾选来源（内置模型恢复 / Claude Code / Codex / OpenCode / Crush / OpenClaw）
                var choices = new List<string>
                {
                    "内置模型（恢复被清空的内置目录）",
                    "Claude Code（~/.claude/settings.json）",
                    "Codex（~/.codex/config.toml）",
                    "OpenCode（~/.config/opencode）",
                    "Crush（~/.config/crush）",
                    "OpenClaw（~/.openclaw）",
                    "Ollama（本地接口实时拉取）",
                    "LM Studio（本地接口实时拉取）",
                    "CC Switch（本地路由实时拉取）",
                };
                // 默认全部勾选（☑ 空格取消不需要的来源，Enter 确认），与 Web 勾选体验一致
                var picked = UxHelper.MultiSelect("📥 本地导入 · 选择来源", choices, preCheckAll: true);
                if (picked == null || picked.Count == 0) return; // 取消 / 未勾选
                string[] keys = ["builtin", "claudecode", "codex", "opencode", "crush", "openclaw", "ollama", "lmstudio", "cc-switch"];
                var sources = string.Join(",", picked.Select(p => keys[Math.Max(0, choices.IndexOf(p))]));
                RunImport(sources, null, "📥 本地导入中…");
                return;
            }
            // 在线导入：弹框选服务商（OpenCode Go/Zen / OpenRouter / Groq / SiliconFlow / Together / DeepSeek / OpenAI / Moonshot），可多选
            var onlineNames = ModelCli.OnlineSources.Select(s => s.Name).ToList();
            var pickedOnline = UxHelper.MultiSelect("🌐 在线导入 · 选择服务商", onlineNames, preCheckAll: false);
            if (pickedOnline == null || pickedOnline.Count == 0) return;
            var repList = new List<string>();
            foreach (var name in pickedOnline)
            {
                var src = ModelCli.OnlineSources.FirstOrDefault(s => s.Name == name);
                if (src != null) repList.Add(ModelCli.ImportOnline(src));
            }
            help.Text = string.Join("\n", repList);
            help2.Text = "";
            RefreshParent();
        }

        void RunImport(string? sources, string? onlineBaseUrl, string busyText)
        {
            help.Text = busyText;
            help2.Text = "";
            screen?.MarkDirty();
            Task.Run(() =>
            {
                string report;
                try
                {
                    if (sources == null)
                    {
                        // opencode 在线拉取（Go=zen/go/v1 订阅 / Zen=zen/v1 按量，地址由调用方选）
                        var apiBase = onlineBaseUrl ?? "https://opencode.ai/zen/go/v1";
                        var url = apiBase + "/models";
                        var pname = apiBase.Contains("/zen/go/") ? "OpenCode Go" : "OpenCode Zen";
                        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
                        client.DefaultRequestHeaders.UserAgent.ParseAdd("WayCoder/1.0");
                        var json = client.GetStringAsync(url).GetAwaiter().GetResult();
                        var list = ModelCatalog.ImportOpenCodeApi(json, apiBase);
                        var builtIn = new HashSet<string>(ModelCatalog.BuiltIn.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
                        var toAdd = list.Where(x => !builtIn.Contains(x.Id)).ToList();
                        ModelCatalog.AddCustomRange(toAdd);
                        report = $"✅ 在线导入（{pname}）{toAdd.Count} 个模型" + (list.Count - toAdd.Count > 0 ? $"，跳过 {list.Count - toAdd.Count} 内置" : "");
                    }
                    else
                    {
                        // 本地导入只导模型；key 仅由 api_keys.json + 环境变量决定（不自动同步来源文件的 key）
                        // 本地服务（Ollama/LM Studio）从本地官方接口实时拉取真实模型；其余从第三方库导入
                        bool IsLocalService(string s) => s.Equals("ollama", StringComparison.OrdinalIgnoreCase)
                            || s.Equals("lmstudio", StringComparison.OrdinalIgnoreCase)
                            || s.Equals("cc-switch", StringComparison.OrdinalIgnoreCase);
                        var hasLocalService = sources.Split(',').Any(IsLocalService);
                        if (hasLocalService)
                        {
                            var nonLocal = string.Join(",", sources.Split(',').Select(s => s.Trim()).Where(s =>
                                s.Length > 0 && !IsLocalService(s)));
                            var parts = new List<string>();
                            if (!string.IsNullOrWhiteSpace(nonLocal)) parts.Add(ModelCli.Import(nonLocal).Trim());
                            parts.Add(ModelCli.ImportLocalServices().Trim());
                            report = string.Join("\n", parts);
                        }
                        else
                        {
                            report = ModelCli.Import(sources);
                        }
                        ModelCatalog.Invalidate();
                        ApiKeyStore.ClearCache();
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
                    if (string.IsNullOrWhiteSpace(id)) { RefreshParent("❌ 模型名不能为空"); return; }
                    var pid = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1].Trim() : "custom";
                    var baseUrl = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2].Trim() : null;
                    ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
                        id, id, pid, pid, "*", "Custom", 0, 0, 0, baseUrl, "手动添加", 0));
                    RefreshParent($"✅ 已添加模型 {id}");
                },
                onCancel: () => RefreshParent()); // Esc 取消 → 父级恢复默认底部 + 整体重绘
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
                // 是→删除 + 刷新父级；否/Esc 取消→父级恢复默认底部 + 整体重绘
                if (ok) { ModelCatalog.RemoveCustom(m.Id); RefreshParent($"✅ 已删除 {m.Id}"); }
                else RefreshParent();
            });
            screen?.ShowWindow(confirmWin);
            screen?.MarkDirty();
        }

        /// <summary>清空全部模型（内置 + 自定义），确认后清空，可重新导入。</summary>
        void ClearModels()
        {
            var confirmWin = TuiDialog.Confirm("🧹 清空全部模型",
                "确定清空全部模型？内置目录与已导入的自定义模型都会移除，可清空后重新导入。", ok =>
                {
                    if (ok) { ModelCatalog.ClearAll(); RefreshParent("✅ 已清空全部模型，可重新导入"); }
                    else RefreshParent();
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
            // 两层架构：模型名|服务商|地址|APIKey|上下文|价格（key 按服务商存）
            var prefill = $"{m.Id}|{m.ProviderId}|{info?.DefaultBaseUrl ?? ""}|{ApiKeyStore.Get(m.ProviderId) ?? ""}|{info?.ContextWindow ?? 0}|{info?.InputPrice ?? 0}";
            var inputWin = TuiDialog.InputLine($"✏️ 编辑模型 {m.Id}",
                "格式: 模型名|服务商|地址|APIKey|上下文|价格", prefill, text =>
                {
                    var parts = (text ?? "").Split('|');
                    var id = parts.Length > 0 ? parts[0].Trim() : m.Id;
                    var pid = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1].Trim() : m.ProviderId;
                    var baseUrl = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2].Trim() : null;
                    var apiKey = parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) ? parts[3].Trim() : null;
                    int ctx = parts.Length > 4 && int.TryParse(parts[4], out var c) ? c : 0;
                    double price = parts.Length > 5 && double.TryParse(parts[5], out var pr) ? pr : 0;
                    if (!string.IsNullOrWhiteSpace(apiKey)) ApiKeyStore.Set(pid, apiKey);
                    ModelCatalog.AddCustom(new ModelCatalog.ModelInfo(
                        id, id, pid, pid, "*", "Custom", ctx, price, 0, baseUrl, "手动编辑", 0));
                    RefreshParent($"✅ 已保存模型 {id}");
                },
                onCancel: () => RefreshParent()); // Esc 取消 → 父级恢复默认底部 + 整体重绘
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
                    bool saved;
                    try { saved = ApiKeyStore.Set(m.ProviderId, text.Trim()); }
                    catch (Exception ex) { saved = false; ErrorLog.Error("ModelPicker", $"保存 Key 失败: {ex.Message}"); }
                    ReconfigureAgent(m.ProviderId, text.Trim()); // 运行时生效
                    RefreshParent(saved ? $"✅ 已保存 {m.ProviderId} 的 Key" : $"❌ 保存失败（{m.ProviderId}）——检查写入权限/磁盘");
                },
                onCancel: () => RefreshParent()); // Esc 取消 → 父级恢复默认底部 + 整体重绘
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

        // 搜索输入：所有可打印字符都进过滤词（OnTextChanged 实时过滤），动作键见 ClassifyKey。
        search.OnTextChanged = () => Refresh(false);
        search.KeyHook = key =>
        {
            var action = ClassifyKey(key, out int slot);
            switch (action)
            {
                case EKeyAction.Nav:
                    table.OnKey(key);
                    table.MarkDirty();
                    screen?.MarkDirty();
                    return true;
                case EKeyAction.Commit: Commit(); return true;
                case EKeyAction.Slot: SetSlot(slot); return true;
                // Ctrl+字母：与下面同名按钮走同一个动作
                case EKeyAction.ToggleMode: ToggleMode(); return true;
                case EKeyAction.AllSlots: ToggleAllSlots(); return true;
                case EKeyAction.Scan: TriggerScan(); return true;
                case EKeyAction.Import: TriggerImport("import"); return true;
                case EKeyAction.ImportOnline: TriggerImport("opencode"); return true;
                case EKeyAction.SetKey: PromptKeyForSelected(); return true;
                case EKeyAction.ClearKey: ClearKeyForSelected(); return true;
                case EKeyAction.AddModel: PromptAddModel(); return true;
                case EKeyAction.EditModel: PromptEditModel(); return true;
                case EKeyAction.DeleteModel: DeleteSelectedModel(); return true;
                default: return false; // 落回搜索框，当普通字符
            }
        };
        table.OnSelect = _ => Commit();     // Enter：应用并关闭
        table.OnSpace = CommitNoClose;      // 空格：应用选中但保持对话框（可连续试多个模型）

        // 功能按钮：Tab 切焦点过来、空格/Enter 执行（TuiButton.OnKey 内建）。
        // 做成按钮就不用占字母快捷键 —— 那些字母得留给搜索框打过滤词。
        Wire(res, "btnMode", ToggleMode);
        Wire(res, "btnAllSlots", ToggleAllSlots);
        Wire(res, "btnScan", TriggerScan);
        Wire(res, "btnImport", () => TriggerImport("import"));
        Wire(res, "btnOnline", () => TriggerImport("opencode"));
        Wire(res, "btnSetKey", PromptKeyForSelected);
        Wire(res, "btnClrKey", ClearKeyForSelected);
        Wire(res, "btnAdd", PromptAddModel);
        Wire(res, "btnEdit", PromptEditModel);
        Wire(res, "btnDel", DeleteSelectedModel);
        Wire(res, "btnClear", ClearModels);
        Wire(res, "btnSave", Commit); // 保存按钮 = 应用选中模型并关闭对话框

        // Tab 不再抢去切大/小模型（那是 btnMode 的活），交回 TuiScreen 做焦点遍历
        win.RegisterShortcut(ConsoleKey.Escape, () => Finish(null));

        Refresh(true);
        return win;
    }

    // ═══════════════════════════════════════════════════
    // 模型操作（纯逻辑）
    // ═══════════════════════════════════════════════════

    /// <summary>模型框里搜索框拦截的按键动作。None = 不拦截，落回搜索框当普通字符。</summary>
    public enum EKeyAction
    {
        None, Nav, Commit, Slot,
        // 以下都是按钮的 Ctrl 加速键，与按钮一一对应
        ToggleMode, AllSlots, Scan, Import, ImportOnline,
        SetKey, ClearKey, AddModel, EditModel, DeleteModel,
    }

    /// <summary>
    /// 按键 → 动作分类（纯逻辑，供自测断言）。
    ///
    /// 铁律：<b>任何能打出字符的键都不做快捷键</b>。此前 S/I/O/L/K/A 与数字在「搜索框为空」时
    /// 被当动作键，于是 openai、siliconflow、4o 这类过滤词的第一个字符全被吞掉 ——
    /// 用户看到的现象就是「输入字符串过滤功能没有」。
    /// 现在功能全做成按钮（Tab 切焦点 + 空格执行），只留下打不出字符的键：
    ///   Tab/Shift+Tab  切焦点（搜索框 → 表格 → 各按钮）
    ///   空格/Enter     执行焦点所在按钮（TuiButton.OnKey 自己处理，不经这里）
    ///   ↑↓/PgUp/PgDn   表格导航；Enter 确认选中模型；Esc 取消
    ///   F1-F10         目标槽位（F 键打不出字符，安全）
    ///   Ctrl+字母      各按钮的加速键（带 Ctrl 打不出字符，同样安全）
    /// 其余按键一律 None → 落回搜索框当过滤词。
    /// </summary>
    public static EKeyAction ClassifyKey(ConsoleKeyInfo key, out int slot)
    {
        slot = -1;

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
            case ConsoleKey.DownArrow:
            case ConsoleKey.Home:
            case ConsoleKey.End:
            case ConsoleKey.PageUp:
            case ConsoleKey.PageDown:
                return EKeyAction.Nav;
            case ConsoleKey.Enter:
                return EKeyAction.Commit;
        }

        // F1-F10 → 槽位 0-9（模态框里 F 键不会被 REPL 抢走，RenderWait 自己收键）
        if (key.Key is >= ConsoleKey.F1 and <= ConsoleKey.F10)
        { slot = key.Key - ConsoleKey.F1; return EKeyAction.Slot; }

        if ((key.Modifiers & ConsoleModifiers.Control) == 0)
            return EKeyAction.None; // 裸键一律给搜索框，保证过滤永远能打字

        // Ctrl+字母 = 按钮加速键。选键有两条禁区：
        //   1. Unix 下与控制键同码的 Ctrl+I(Tab) / Ctrl+M(Enter) / Ctrl+H(Backspace) 收不到；
        //   2. TuiEditBase.HandleCtrlKey 已占的编辑键 Ctrl+A/C/X/V/Z/Y/E/K —— KeyHook 跑在它前面，
        //      占了就等于把搜索框的全选/复制/粘贴/撤销抢走。
        return key.Key switch
        {
            ConsoleKey.T => EKeyAction.ToggleMode,
            ConsoleKey.G => EKeyAction.AllSlots,
            ConsoleKey.S => EKeyAction.Scan,
            ConsoleKey.R => EKeyAction.Import,
            ConsoleKey.O => EKeyAction.ImportOnline,
            ConsoleKey.P => EKeyAction.SetKey,
            ConsoleKey.L => EKeyAction.ClearKey,
            ConsoleKey.N => EKeyAction.AddModel,
            ConsoleKey.U => EKeyAction.EditModel,
            ConsoleKey.D => EKeyAction.DeleteModel,
            _ => EKeyAction.None,
        };
    }

    /// <summary>Enter：无 key 则返回 NeedsApiKey，有 key 则直接应用</summary>
    private static Result? EnterOrPromptKey(ModelEntry m, bool isLarge, int slot)
    {
        if (m == null) return null;
        if (!m.HasApiKey)
        {
            // 返回 NeedsApiKey，由调用方弹出输入框
            return new(m.Id, isLarge, slot, NeedsApiKey: true, ProviderId: m.ProviderId, BaseUrl: m.BaseUrl);
        }
        Apply(m.Id, isLarge, slot, m.BaseUrl, m.ProviderId);
        return new(m.Id, isLarge, slot, BaseUrl: m.BaseUrl);
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

    /// <summary>
    /// 应用选中模型到配置/槽位（public 供 /model 命令等复用）。
    /// baseUrl/providerId 来自所选模型（地址不同 = 不同服务商）——保存后请求走对应网关。
    /// </summary>
    public static void Apply(string modelId, bool isLarge, int slot, string? baseUrl = null, string? providerId = null)
    {
        var cfg = Config.Instance;
        if (slot == -1)
        {
            if (isLarge) cfg.Model = modelId; else cfg.SmallModel = modelId;
            if (providerId != null) cfg.Provider = providerId;  // 同步服务商（key 跟服务商走）
            if (baseUrl != null) cfg.BaseUrl = baseUrl;         // 同步网关地址
            cfg.SaveToEnvFile();
        }
        else if (slot == -2)
        {
            AgentSlotConfig.SetUniform(new AgentSlotConfig.SlotConfig
            {
                UseGlobal = false,
                LargeModel = isLarge ? modelId : null,
                SmallModel = isLarge ? null : modelId,
                BaseUrl = baseUrl,
                ApiKeyProviderId = providerId,
            });
            if (isLarge) cfg.Model = modelId; else cfg.SmallModel = modelId;
            if (providerId != null) cfg.Provider = providerId;
            if (baseUrl != null) cfg.BaseUrl = baseUrl;
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
                BaseUrl = baseUrl ?? e.BaseUrl,
                ApiKeyProviderId = providerId ?? e.ApiKeyProviderId,
                ApiKey = e.ApiKey,
            });
        }

        // 运行时生效（与 /model 命令后处理对齐）：全局模型切换立即更新当前 LLM，
        // 大模型切换同步按模型重算上下文窗口 —— 此前设置页/空格预览路径缺失此步，
        // 切模型后 cfg.Model 变了但窗口残留旧模型值，压缩阈值也跟着错，直到重启或走 /model。
        if (slot is -1 or -2)
        {
            var agent = ProgramContext.Agent;
            if (agent != null)
            {
                if (isLarge) agent.LlmClient.Model = cfg.Model;
                else agent.LlmClient.SmallModel = cfg.SmallModel;
                if (isLarge)
                    agent.UpdateContextWindow(ModelCatalog.ResolveContextWindow(cfg.Model, cfg.MaxContextTokens));
            }
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
            // 地址不同 = 不同服务商：同 id 不同 baseUrl 都保留（不再按 id 去重）
            if (!seen.Add(ModelCatalog.ModelKey(info.ProviderId, info.DefaultBaseUrl, info.Id))) continue;
            var hasKey = ModelHasKey(info.ProviderId, info.Id);
            list.Add(new(info.Id, info.DisplayName, info.Provider, info.ProviderId, hasKey, info.ContextWindow, info.InputPrice, info.DefaultBaseUrl));
        }
        if (!string.IsNullOrEmpty(Config.Instance.FallbackChain))
            foreach (var m in Config.Instance.FallbackChain.Split(','))
            { var t = m.Trim(); if (!string.IsNullOrEmpty(t) && seen.Add(t)) list.Add(new(t, t, "自定义", "custom", true, 128_000, 0)); }
        return list;
    }

    // ═══════════════════════════════════════════════════
    // 状态推导（纯逻辑，供自测）
    // ═══════════════════════════════════════════════════

    /// <summary>连通性探测结果 → 状态枚举：已连接=连通；HTTP 402=欠费；401/403=key 无效；其余失败=不通。</summary>
    public static ScanStatus ProbeStatus(ModelCli.EndpointProbe p)
    {
        if (p.Ok) return ScanStatus.Connected;
        var d = p.Detail ?? "";
        if (d.Contains("402")) return ScanStatus.Overdue;
        if (d.StartsWith("密钥无效", StringComparison.Ordinal)) return ScanStatus.BadKey;
        if (d.StartsWith("无端点", StringComparison.Ordinal)) return ScanStatus.NoEndpoint;
        return ScanStatus.Unreachable; // 无法连接 / HTTP 4xx/5xx 等
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
