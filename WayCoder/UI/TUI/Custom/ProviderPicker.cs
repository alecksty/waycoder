using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Screens;
using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.Tui.Controls;

namespace WayCoder.UI.TUI.Custom;

/// <summary>
/// 供应商管理对话框（/provider）：列出全部供应商，逐个管理
/// 设Key / 清Key / 测试连通 / 添加 / 改名 / 改地址 / 删除。
/// 手机没有快捷键难切换，桌面/Web/移动各端都从这里集中管理供应商。
/// </summary>
public static class ProviderPicker
{
    private const int MinW = 68, MinH = 18;

    /// <summary>当前选中供应商（行模型）。</summary>
    private sealed record ProviderRow(string Id, string Name, string BaseUrl, bool HasKey, bool IsLocal);

    public static void Show()
    {
        using var evt = new ManualResetEventSlim(false);
        try
        {
            var screen = TuiManager.Instance?.ActiveScreen;
            var win = BuildWindow(screen, () => evt.Set());
            screen?.ShowWindow(win);
            UxHelper.RenderWait(screen, evt, 0, win);
        }
        catch { evt.Set(); }
    }

    private static void Wire(TuiMarkupResult res, string id, Action action)
    {
        var btn = res.Find<TuiButton>(id);
        if (btn != null) btn.OnClick = _ => action();
    }

    private static TuiWindow BuildWindow(TuiScreen? screen, Action close)
    {
        int winW = Math.Min(Tty.Cols - 2, Math.Max(MinW, Tty.Cols * 2 / 3));
        int winH = Math.Min(Tty.Rows - 2, Math.Max(MinH, Tty.Rows * 2 / 3));

        var res = TuiMarkup.LoadResource("dialogs/providerpicker.tui");
        var win = res.Window ?? throw new InvalidOperationException("providerpicker.tui 根应为 Dialog");
        win.Width = winW; win.Height = winH;
        win.MinWidth = MinW; win.MinHeight = MinH;

        var table = res.Find<TuiTableList>("table")!;
        var slotBar = res.Find<TuiLabel>("slotBar")!;
        var help = res.Find<TuiLabel>("help")!;
        var help2 = res.Find<TuiLabel>("help2")!;

        // ── 状态 ──
        List<ProviderRow> rows = [];
        var scanStatus = new Dictionary<string, bool>();   // providerId → 连通(true/false)
        var scanLock = new object();

        void Rebuild()
        {
            var providers = ModelCatalog.Providers
                .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
            rows.Clear();
            table.ClearRows();
            foreach (var (pid, p) in providers)
            {
                var isLocal = pid is "local" or "custom";
                var hasKey = isLocal || ApiKeyStore.Masked(pid) != null;
                rows.Add(new ProviderRow(pid, p.DisplayName, p.DefaultBaseUrl, hasKey, isLocal));
                bool? conn = null;
                lock (scanLock) if (scanStatus.TryGetValue(pid, out var c)) conn = c;
                var connText = conn == null ? "未扫描" : conn == true ? "✔ 可达" : "❌ 不可达";
                var keyText = isLocal ? "-" : hasKey ? "✔" : "无";
                var icon = isLocal ? "🌿" : hasKey ? "🔑" : "⚠️";
                var addr = string.IsNullOrWhiteSpace(p.DefaultBaseUrl) ? "(未设地址)" : p.DefaultBaseUrl;
                var modelCount = isLocal ? "-" : ModelCatalog.ByProvider(pid).Length.ToString();
                table.AddRow(icon, $"{p.DisplayName}（{pid}）", keyText, modelCount, connText, addr);
            }
            slotBar.Text = $"共 {rows.Count} 个供应商（含本地） · 有Key {rows.Count(r => r.HasKey && !r.IsLocal)} · 未设Key {rows.Count(r => !r.HasKey && !r.IsLocal)}";
            win.RootView.Invalidate();
            screen?.MarkDirty();
        }

        void Say(string msg, string sub = "")
        {
            help.Text = msg;
            help2.Text = sub;
            screen?.MarkDirty();
        }

        ProviderRow? Selected()
            => table.SelectedIndex >= 0 && table.SelectedIndex < rows.Count ? rows[table.SelectedIndex] : null;

        // ── 动作 ──

        void PromptSetKey(ProviderRow? r)
        {
            if (r == null || r.IsLocal) { Say("⚠ 请选一个非本地供应商"); return; }
            TuiDialog.InputLine($"🔑 设置 {r.Name} 的 API Key", "粘贴 Key（留空取消）", "",
                text => { if (!string.IsNullOrWhiteSpace(text)) { ApiKeyStore.Set(r.Id, text.Trim()); Rebuild(); Say($"✅ 已保存 {r.Name} 的 Key"); } });
        }

        void PromptClearKey(ProviderRow? r)
        {
            if (r == null || r.IsLocal) { Say("⚠ 请选一个非本地供应商"); return; }
            ApiKeyStore.Remove(r.Id);
            Rebuild();
            Say($"🗑 已清除 {r.Name} 的 Key");
        }

        void TriggerTest()
        {
            Say("📡 测试全部供应商连通性…");
            Task.Run(() =>
            {
                var dict = new Dictionary<string, bool>();
                try
                {
                    foreach (var p in ModelCli.TestList())
                        dict[p.ProviderId] = p.Ok;
                }
                catch { }
                lock (scanLock) { scanStatus = dict; }
                // Rebuild 重建表格行 + 标脏，必须回 UI 线程执行（后台线程并发改控件 → 渲染线程读到半改态 → 卡死）
                screen?.PostToUI(() => { Rebuild(); Say($"✅ 测试完成：可达 {dict.Count(v => v.Value)} / {dict.Count}"); });
            });
        }

        void PromptAdd()
        {
            TuiDialog.InputLine("➕ 添加供应商",
                "格式: 供应商ID|显示名|BaseUrl（可空）", "",
                text =>
                {
                    var parts = (text ?? "").Split('|');
                    var id = parts.Length > 0 ? parts[0].Trim() : "";
                    if (string.IsNullOrWhiteSpace(id)) { Say("❌ 供应商 ID 不能为空"); return; }
                    var name = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1].Trim() : id;
                    var url = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2].Trim() : "";
                    var err = ModelCatalog.RegisterProviderResult(id, name, url);
                    if (err != null)
                    {
                        Rebuild();
                        Say("❌ " + err);
                        return;
                    }
                    Rebuild();
                    Say($"✅ 已添加供应商 {name}");
                });
        }

        void PromptRename(ProviderRow? r)
        {
            if (r == null) { Say("⚠ 请先选中一个供应商"); return; }
            TuiDialog.InputLine($"✏️ 改名 {r.Name}", "新显示名", r.Name,
                text =>
                {
                    var name = text?.Trim();
                    if (string.IsNullOrWhiteSpace(name)) return;
                    ModelCatalog.RenameProvider(r.Id, name);
                    Rebuild();
                    Say($"✅ 已改名 → {name}");
                });
        }

        void PromptEditUrl(ProviderRow? r)
        {
            if (r == null) { Say("⚠ 请先选中一个供应商"); return; }
            TuiDialog.InputLine($"🌐 修改 {r.Name} 的地址", "Base URL", r.BaseUrl,
                text =>
                {
                    var url = text?.Trim() ?? "";
                    var err = ModelCatalog.UpdateProviderUrlResult(r.Id, url);
                    if (err != null)
                    {
                        Rebuild();
                        Say("❌ 新" + err);
                        return;
                    }
                    Rebuild();
                    Say($"✅ 已更新地址");
                });
        }

        void PromptDelete(ProviderRow? r)
        {
            if (r == null) { Say("⚠ 请先选中一个供应商"); return; }
            var confirmWin = TuiDialog.Confirm("🗑 删除供应商", $"删除 {r.Name}（{r.Id}）？删除后不可恢复", ok =>
            {
                if (ok) { ModelCatalog.RemoveProvider(r.Id); Rebuild(); Say($"🗑 已删除供应商 {r.Name}"); }
                else Rebuild();
            });
            screen?.ShowWindow(confirmWin);
            screen?.MarkDirty();
        }

        /// <summary>Enter 选中行 → 弹供应商操作菜单（设Key/清Key/改名/改地址/删除，对标移动端供应商卡片菜单）。</summary>
        void PromptActionMenu(ProviderRow r)
        {
            var items = new List<string> { "🔑 设Key", "🗑 清Key", "✏️ 改名", "🌐 改地址", "🗑 删除" };
            var selectWin = TuiDialog.Select($"供应商操作 · {r.Name}（{r.Id}）", items, idx =>
            {
                switch (idx)
                {
                    case 0: PromptSetKey(r); break;
                    case 1: PromptClearKey(r); break;
                    case 2: PromptRename(r); break;
                    case 3: PromptEditUrl(r); break;
                    case 4: PromptDelete(r); break;
                }
            });
            screen?.ShowWindow(selectWin);
            screen?.MarkDirty();
        }

        // Enter 选中行 → 弹操作菜单（对标移动端供应商卡片菜单）
        table.OnSelect = idx =>
        {
            var r = idx >= 0 && idx < rows.Count ? rows[idx] : null;
            if (r != null) PromptActionMenu(r);
        };

        // 接线（按钮传当前选中行；K/X/T/A/R/U/D/Q 快捷键由 .tui shortcut 属性触发）
        Wire(res, "btnSetKey", () => PromptSetKey(Selected()));
        Wire(res, "btnClrKey", () => PromptClearKey(Selected()));
        Wire(res, "btnTest", TriggerTest);
        Wire(res, "btnAdd", PromptAdd);
        Wire(res, "btnRename", () => PromptRename(Selected()));
        Wire(res, "btnUrl", () => PromptEditUrl(Selected()));
        Wire(res, "btnDel", () => PromptDelete(Selected()));
        Wire(res, "btnDone", close);

        // 初始
        Rebuild();
        Say("↑↓选择 Enter操作菜单  K设Key X清Key T测试 A添加 R改名 U改地址 D删除 Q完成",
            "供应商由 providers.json 管理，改动即落盘");
        return win;
    }
}
