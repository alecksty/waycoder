using System.Text;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Screens;
using WayCoder.UI.Tui.Edit;

namespace WayCoder;

public static partial class SelfTest
{
    private static void TestChunk8Dialog(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[TuiDialog 布局渲染]");

        (string Name, TuiWindow Win, bool ExpectBar)[] dialogs =
        [
            ("Info",       TuiDialog.Info("信息", "这是一条信息提示"), true),
            ("Success",    TuiDialog.Success("成功", "操作已完成"), true),
            ("Warn",       TuiDialog.Warn("警告", "请注意风险"), true),
            ("Error",      TuiDialog.Error("错误", "发生了错误"), true),
            ("Confirm",    TuiDialog.Confirm("确认", "是否继续执行？", _ => { }), true),
            ("Confirm3",   TuiDialog.Confirm3("选择", "请选择操作", _ => { }), true),
            ("Input",      TuiDialog.Input("输入", "请输入名称", "默认值", _ => { }), false),
            ("InputLine",  TuiDialog.InputLine("单行", "请输入一行", "", _ => { }), false),
            ("Secret",     TuiDialog.Secret("密钥", "请输入密钥", "", _ => { }), false),
            ("FindReplace", TuiDialog.FindReplace("find", "repl", new FindOptions(), (_, _) => { }, (_, _, _) => { }, (_, _, _) => { }), false),
            ("Select",     TuiDialog.Select("选择", ["A", "B", "C"], _ => { }), false),
            ("MultiSelect", TuiDialog.MultiSelect("多选", ["X", "Y", "Z"], _ => { }), false),
            ("Permission", TuiDialog.Permission("权限确认", "是否允许执行该命令？", _ => { }), true),
        ];

        int cols = Tty.Cols;
        int rows = Tty.Rows;
        int maxW = (int)Math.Ceiling(cols * 0.75);
        int maxH = (int)Math.Ceiling(rows * 0.75);

        foreach (var (name, win, expectBar) in dialogs)
        {
            win.OnResize(cols, rows);
            Check($"{name}: 宽≤3/4屏", win.Width <= maxW + 1);
            Check($"{name}: 高≤3/4屏", win.Height <= maxH + 1);
            if (expectBar)
            {
                Check($"{name}: 内容区从标题下开始(ContentTop=Y+1)", win.ContentTop == win.Y + 1);
                Check($"{name}: 内容高度无分隔线扣除", win.ContentHeight == win.Height - 2);
            }
        }

        // 渲染每个对话框：标题独占独立行 + 无异常（先收集帧，再统一断言，避免 Check 输出被抑制）
        var dialogFrames = new List<(string Name, string Title, string RawFrame, string Frame, int WinY, bool ExpectBar)>();
        var mgr2 = TuiManager.Instance;
        var prevOut2 = Console.Out;
        Console.SetOut(TextWriter.Null);
        bool entered2 = false;
        try
        {
            if (!mgr2.IsActive) { mgr2.Enter(); entered2 = true; }
            foreach (var (name, win, expectBar) in dialogs)
            {
                string raw = "", frame = "";
                int winY = -1;
                try
                {
                    var chat = new ChatScreen();
                    mgr2.PushScreen(chat);
                    chat.AddWindow(win);
                    mgr2.Render();
                    raw = mgr2.LastCleanFrame;
                    frame = AnsiString.Strip(raw);
                    winY = win.Y;
                    mgr2.PopScreen();
                }
                catch { frame = ""; }
                dialogFrames.Add((name, win.Title, raw, frame, winY, expectBar));
            }
        }
        finally
        {
            if (entered2) { try { mgr2.Exit(); } catch { } }
            Console.SetOut(prevOut2);
        }

        foreach (var (name, title, raw, frame, winY, expectBar) in dialogFrames)
        {
            Check($"{name}: 渲染非空", frame.Length > 0);
            if (expectBar && raw.Length > 0 && winY >= 0)
            {
                // 用 ANSI 网格解释验证标题嵌在上边框行（win.Y），而非独立标题行。
                // 无分隔线后 win.Y+1 是内容行，其文本可能恰含标题子串（如"错误"），故只断言顶边框行含标题。
                var dialogGrid = TuiAudit.AnsiToGrid(raw, rows, cols);
                bool topRowHasTitle = winY < dialogGrid.Count && dialogGrid[winY].Contains(title);
                Check($"{name}: 标题嵌上边框(顶部行)", topRowHasTitle);
            }
        }

        // 按钮必须真画出来 —— 「渲染非空」抓不到「按钮被挤出内容区」这类问题
        var expectBtns = new Dictionary<string, string[]>
        {
            ["Info"] = ["确定"], ["Success"] = ["确定"], ["Warn"] = ["确定"], ["Error"] = ["确定"],
            // 断言用 "(Y)"/"(N)" 而不是 "是"/"否"：消息「是否继续执行？」自带这两个字，
            // 用它们断言会误判成按钮已渲染（这条假绿灯正是按钮丢了却没人发现的原因）
            ["Confirm"] = ["(Y)", "(N)"],
            ["Confirm3"] = ["(Y)", "(N)", "(Esc)"],
            ["Input"] = ["确定", "取消"], ["InputLine"] = ["确定", "取消"], ["Secret"] = ["确定", "取消"],
        };
        foreach (var (name, _, _, frame, _, _) in dialogFrames)
        {
            if (frame.Length == 0 || !expectBtns.TryGetValue(name, out var labels)) continue;
            foreach (var lb in labels)
                Check($"{name}: 按钮「{lb}」可见", frame.Contains(lb));
        }

        // ── 窗口树层级刷新：父窗口整窗重绘后，上层子窗口必须跟着整窗重绘 ──
        // 父窗口（低层）整窗重绘会铺满自身背景/边框，把重叠区域里子窗口的内容盖掉；
        // 子窗口若无脏后代，增量模式下被跳过 → 「对话框刷没了」。树先序渲染 + 粘性 forceFull
        // 保证：父整窗 → 子窗口强制整窗（刷新自己再刷子级）。
        {
            var savedSzL = Tty.SizeOverride;
            var mgrL = TuiManager.Instance;
            bool enteredL = false;
            var prevOutL = Console.Out;
            try
            {
                Tty.SizeOverride = (100, 40);
                var scrL = new ChatScreen();
                // 真实父-子：子窗口通过 AddChildWindow 挂在父窗口下（子窗口永远在父之上）
                var parentL = new TuiWindow { X = 5, Y = 3, Width = 40, Height = 12, Title = "父窗口", WinBg = 100, Modal = false };
                var childL = new TuiWindow { X = 10, Y = 5, Width = 30, Height = 8, Title = "子对话框", WinBg = 100, Modal = false };
                childL.ContentLines.Add("子级内容标记ABC");
                if (!mgrL.IsActive) { mgrL.Enter(); enteredL = true; }
                mgrL.PushScreen(scrL);
                scrL.AddWindow(parentL);
                parentL.AddChildWindow(childL);
                Console.SetOut(TextWriter.Null); // 渲染帧不污染 --test 输出
                Check("层级刷新: 子窗口挂在父窗口下", parentL.Children.Contains(childL) && childL.Parent == parentL);
                mgrL.Render(); // 全量基线帧

                // 仅父窗口标脏 → 父整窗重绘覆盖子窗口区域；子窗口必须跟着整窗重绘
                mgrL.IsDirty = true;
                parentL.RootView.Invalidate();
                mgrL.Render(); // 增量帧
                Console.SetOut(prevOutL);
                var frameL = AnsiString.Strip(mgrL.LastCleanFrame);
                Check("层级刷新: 父窗口重绘后子对话框内容仍在", frameL.Contains("子级内容标记ABC"));
                Check("层级刷新: 子对话框标题仍可见", frameL.Contains("子对话框"));
                Check("层级刷新: 父窗口本身也重绘", frameL.Contains("父窗口"));

                // 同级子窗口 Z-order：后添加的 Z 更大（渲染在上）
                var sib1 = new TuiWindow { X = 8, Y = 4, Width = 20, Height = 6, Title = "兄弟一", WinBg = 100 };
                var sib2 = new TuiWindow { X = 12, Y = 6, Width = 20, Height = 6, Title = "兄弟二", WinBg = 100 };
                parentL.AddChildWindow(sib1);
                parentL.AddChildWindow(sib2);
                Check("同级子窗口 Z-order 递增", sib1.ZOrder < sib2.ZOrder);

                // 关父窗口 → 递归关闭其所有子窗口
                scrL.CloseWindow(parentL);
                Check("关父递归关子: 父子链全移除", parentL.Children.Count == 0
                    && !scrL.Windows.Contains(parentL) && !scrL.Windows.Contains(childL));
            }
            finally
            {
                Console.SetOut(prevOutL);
                Tty.SizeOverride = savedSzL;
                try { mgrL.PopScreen(); } catch { }
                if (enteredL) { try { mgrL.Exit(); } catch { } }
            }
        }

        // 模型选择器：底部三行（槽位/帮助/帮助2）必须落在内容区内。
        // 曾经 code-behind 手算 listH = winH-5，比 ContentHeight(winH-2) 多占 1 行，
        // 最后一行帮助被边框裁掉 —— 现在表格 flex="1" 由 VBox 分配，剩余行数才准。
        try
        {
            var mpRes = WayCoder.UI.TUI.TuiMarkup.LoadResource("dialogs/modelpicker.tui");
            var mpWin = mpRes.Window!;
            foreach (int h in (int[])[16, 20, 28, 40])
            {
                mpWin.Width = 78; mpWin.Height = h;
                mpWin.OnResize(120, h + 4);
                var bottom = mpWin.RootView!.Y + mpWin.ContentHeight;
                foreach (var id in (string[])["slotBar", "help", "help2"])
                {
                    var lb = mpRes.Find<TuiLabel>(id)!;
                    Check($"模型选择器 h={h}: 「{id}」在内容区内(Y={lb.Y}<{mpWin.ContentHeight})",
                        lb.Y >= 0 && lb.Y + lb.Height <= mpWin.ContentHeight);
                }
                var mpTable = mpRes.Find<TuiTableList>("table")!;
                // 固定占 8 行：搜索 + 空行 + 槽位条 + 空行(两行按钮间) + 两行按钮 + 两行提示
                Check($"模型选择器 h={h}: 表格按 flex 撑开", mpTable.Height == mpWin.ContentHeight - 8);
            }
        }
        catch (Exception ex) { Check($"模型选择器布局: 加载异常 {ex.Message}", false); }

        // ── 模型选择器样式：金色渐变边框（顺带让标题居中）+ 列宽铺开 + 底部提示居中 ──
        try
        {
            var mpRes2 = WayCoder.UI.TUI.TuiMarkup.LoadResource("dialogs/modelpicker.tui");
            Check("模型选择器: 标记声明渐变边框", mpRes2.Window!.GradientBorder);

            // TuiScreen.RenderWindow 只在「GradientBorder && GradientStart >= 0x1000000」时走渐变分支，
            // 而标题居中只做在渐变分支里 —— 这条同时是「金色边框」和「标题居中」的前提
            var gold = TuiTheme.Current.GradOrangeYellow;
            Check("模型选择器: 金色渐变是 TrueColor（否则退回非渐变分支，标题又变左对齐）",
                gold.start >= 0x1000000 && gold.end >= 0x1000000);

            var mpTbl2 = mpRes2.Find<TuiTableList>("table")!;
            Check("模型选择器: 表格列宽等比铺开", mpTbl2.StretchColumns);
            var effW = mpTbl2.EffectiveWidths(76);
            int effSum = 0;
            foreach (var x in effW) effSum += x;
            Check("模型选择器: 列宽铺满控件（此前固定合计 54，右侧空 22 列）", effSum == 76);

            foreach (var id in (string[])["slotBar", "help", "help2"])
                Check($"模型选择器: 底部「{id}」提示居中",
                    mpRes2.Find<TuiLabel>(id)!.TextAlign == EHAlign.Center);

            // 功能做成按钮 → Tab 切焦点 + 空格执行，就不必占字母快捷键（字母得留给过滤）
            foreach (var id in (string[])["btnMode", "btnAllSlots", "btnScan", "btnImport", "btnOnline", "btnClear",
                                          "btnSetKey", "btnClrKey", "btnAdd", "btnEdit", "btnDel"])
            {
                var b = mpRes2.Find<TuiButton>(id);
                Check($"模型选择器: 按钮「{id}」存在且可获得焦点", b != null && b.CanFocus);
            }
            // 按钮不带字母快捷键：带了就会在搜索框打字时被窗口 OnKey 抢走（它按 KeyChar 匹配大写键）
            foreach (var id in (string[])["btnScan", "btnImport", "btnAdd", "btnDel", "btnClear"])
                Check($"模型选择器: 按钮「{id}」不注册字母快捷键",
                    mpRes2.Find<TuiButton>(id)!.ShortcutKey == null);

            // 两行按钮 flex=1 均分铺满整行，窄窗口自适应不溢出（固定 Width 估算会漏算按钮数，改用 flex 断言）
            foreach (var (row, ids) in ((string, string[])[])[
                ("第一行", ["btnMode", "btnAllSlots", "btnScan", "btnImport", "btnOnline", "btnClear"]),
                ("第二行", ["btnSetKey", "btnClrKey", "btnAdd", "btnEdit", "btnDel", "btnSave"])])
            {
                foreach (var id in ids)
                    Check($"模型选择器: 按钮「{id}」flex 均分(窄屏自适应不溢出)",
                        mpRes2.Find<TuiButton>(id)!.Flex > 0);
            }

            // 金色渐变底是 TuiButton 默认值：标记和 code-behind 都不用写
            var btnGrad = TuiTheme.Current.BtnOrangeYellow;
            foreach (var id in (string[])["btnMode", "btnScan", "btnDel"])
            {
                var b = mpRes2.Find<TuiButton>(id)!;
                Check($"模型选择器: 按钮「{id}」默认金色渐变底",
                    b.GradientBg && b.GradientBgStart == btnGrad.start && b.GradientBgEnd == btnGrad.end);
            }
        }
        catch (Exception ex) { Check($"模型选择器样式: 加载异常 {ex.Message}", false); }

        // ── 状态列：连通性探测 → 状态枚举（纯逻辑）+ 标记里声明状态列 ──
        {
            static WayCoder.UI.TUI.Custom.ModelPicker.ScanStatus PS(string pid, bool ok, string d)
                => WayCoder.UI.TUI.Custom.ModelPicker.ProbeStatus(
                    new ModelCli.EndpointProbe(pid, pid, null, ok, d, []));

            Check("状态列: 已连接 → Connected", PS("d", true, "已连接（200）") == WayCoder.UI.TUI.Custom.ModelPicker.ScanStatus.Connected);
            Check("状态列: HTTP 402 → 欠费", PS("d", false, "HTTP 402") == WayCoder.UI.TUI.Custom.ModelPicker.ScanStatus.Overdue);
            Check("状态列: 密钥无效 → BadKey", PS("d", false, "密钥无效（401）") == WayCoder.UI.TUI.Custom.ModelPicker.ScanStatus.BadKey);
            Check("状态列: 无端点 → NoEndpoint", PS("d", false, "无端点（未配置 base_url）") == WayCoder.UI.TUI.Custom.ModelPicker.ScanStatus.NoEndpoint);
            Check("状态列: 无法连接 → Unreachable", PS("d", false, "无法连接（超时/拒绝）") == WayCoder.UI.TUI.Custom.ModelPicker.ScanStatus.Unreachable);

            var mpRes3 = WayCoder.UI.TUI.TuiMarkup.LoadResource("dialogs/modelpicker.tui");
            var mpTbl3 = mpRes3.Find<WayCoder.UI.Tui.Controls.TuiTableList>("table")!;
            Check("状态列: 表格声明 8 列（含状态）", mpTbl3.ColumnCount == 8);
            var mpHeader = mpTbl3.RenderHeader();
            Check("状态列: 列头含「状态」", mpHeader.Contains("状态"));
            // 🔑 列头曾因列宽 2、非末列按 widths-1=1 渲染被 TruncateByWidth 截成空 ——
            // 已把 🔑 列加宽到 3（从价格列匀 1），表头 emoji 能完整显示
            Check("状态列: 列头含「🔑」", mpHeader.Contains("🔑"));
        }

        // ── ModelCatalog 清空/恢复内置：清空标记决定 All 是否含内置（不删真实用户模型，只临时写/删标记）──
        {
            var clearPath = ModelCatalog.BuiltInClearedPath;
            bool hadMark = File.Exists(clearPath);
            try
            {
                var allBefore = ModelCatalog.All.ToList();
                File.WriteAllText(clearPath, "1");
                ModelCatalog.Invalidate(); // 直接写标记不经过 ClearAll，需手动失效缓存，否则 All 返回旧缓存
                var allCleared = ModelCatalog.All;
                // 清空后仅内置源模型被移除；用户导入的同 id 模型（如 aihubmix 聚合器）合法保留，不算内置残留
                var removed = allBefore
                    .Where(b => !allCleared.Any(m => m.Id == b.Id && m.ProviderId == b.ProviderId))
                    .Select(m => $"{m.Id}|{m.ProviderId}")
                    .ToHashSet();
                Check("清空标记: 标记后仅内置被移除",
                    ModelCatalog.BuiltInCleared
                    && removed.All(rid => ModelCatalog.BuiltIn.Any(b => $"{b.Id}|{b.ProviderId}" == rid)));

                ModelCatalog.RestoreBuiltIn();
                var allRestored = ModelCatalog.All;
                Check("清空标记: RestoreBuiltIn 恢复内置",
                    !ModelCatalog.BuiltInCleared && allRestored.Any(m => ModelCatalog.BuiltIn.Any(b => b.Id == m.Id)));
            }
            finally
            {
                if (!hadMark) try { File.Delete(clearPath); } catch { }
            }
        }

        // ── 搜索过滤端到端：Refresh 改数据后必须标脏窗口根视图，否则增量渲染跳过表格 ──
        // 旧 bug：OnTextChanged → Refresh 清空/重填 table 的 rows，但 table 与窗口根视图都没标脏，
        // 下一帧增量渲染（child.IsDirty || parentDirty 才渲染）直接跳过表格 → 用户「搜索输入后列表不动」。
        {
            var mpW2 = WayCoder.UI.TUI.TuiMarkup.LoadResource("dialogs/modelpicker.tui");
            var ww = mpW2.Window!;
            ww.OnResize(120, 30);
            var tbl2 = mpW2.Find<WayCoder.UI.Tui.Controls.TuiTableList>("table")!;

            // 初始渲染一次，消费初始 IsDirty，进入「干净」状态
            var sbInit = new StringBuilder();
            ww.RootView.Render(sbInit, ww.X, ww.Y);
            ww.RootView.ClearDirty(); tbl2.ClearDirty();

            // 模拟 Refresh：清空 + 填入过滤后的新行（不标脏 = 旧 bug 路径）。
            // 8 列顺序与标记 columns 一致：🔑,模型,厂商,状态,窗口,价格,大,小
            tbl2.ClearRows();
            tbl2.AddRow("🔑", "deepseek-v4-pro", "deepseek", "✔连通", "128k", "x", "✓", " ");
            var sbNoDirty = new StringBuilder();
            ww.RootView.Render(sbNoDirty, ww.X, ww.Y);
            Check("搜索过滤: 改数据不标脏 → 增量渲染跳过表格（旧 bug 复现）",
                !sbNoDirty.ToString().Contains("deepseek-v4-pro"));

            // 修复：Refresh 末尾 win.RootView.MarkDirty() → 走全量重绘，新行可见
            ww.RootView.MarkDirty();
            var sbDirty = new StringBuilder();
            ww.RootView.Render(sbDirty, ww.X, ww.Y);
            Check("搜索过滤: 标脏后渲染出新行（修复生效）",
                sbDirty.ToString().Contains("deepseek-v4-pro"));
        }

        // ── 嵌套对话框 ESC：必须触发 onCancel（父级刷新链路的起点 —— ModelPicker 的
        //    设Key/添加/编辑/删除子弹窗都靠 onCancel 调 RefreshParent 刷新父级底部+整体）──
        try
        {
            int cancelHits = 0;
            var savedSz = Tty.SizeOverride;
            Tty.SizeOverride = (100, 30);
            try
            {
                var ilWin = TuiDialog.InputLine("嵌套框", "prompt", "", _ => { }, () => cancelHits++);
                ilWin.OnKey(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false));
                Check("嵌套框: Esc 触发 onCancel（父级刷新入口）", cancelHits == 1);
            }
            finally { Tty.SizeOverride = savedSz; }
        }
        catch (Exception ex) { Check($"嵌套框: 构造异常 {ex.Message}", false); }

        // ── 多级弹窗：按键脚本逐层弹出 + ESC 逐层关闭（树结构：后弹的自动成为当前模态的子窗口）──
        {
            var mscr = new ChatScreen();
            mscr.Activate();
            var l1 = TuiDialog.Confirm("第一层", "确认?", _ => { });
            mscr.ShowWindow(l1);
            Check("多级弹窗: 第一层弹出(根=1)", mscr.Windows.Count == 1 && mscr.FocusedWindow == l1);
            var l2 = TuiDialog.InputLine("第二层", "输入", "", _ => { });
            mscr.ShowWindow(l2);
            Check("多级弹窗: 第二层弹出(l1 子=1,焦点归 l2)",
                mscr.Windows.Count == 1 && l1.Children.Count == 1 && mscr.FocusedWindow == l2);
            var l3 = TuiDialog.Confirm("第三层", "再确认?", _ => { });
            mscr.ShowWindow(l3);
            Check("多级弹窗: 第三层弹出(l2 子=1,焦点归 l3)",
                mscr.Windows.Count == 1 && l2.Children.Count == 1 && mscr.FocusedWindow == l3);

            var escK = new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false);
            mscr.OnKey(escK);
            Check("多级弹窗: ESC 关第三层(焦点回第二层)",
                l2.Children.Count == 0 && mscr.FocusedWindow == l2);
            mscr.OnKey(escK);
            Check("多级弹窗: ESC 关第二层(焦点回第一层)",
                l1.Children.Count == 0 && mscr.FocusedWindow == l1);
            mscr.OnKey(escK);
            Check("多级弹窗: ESC 关第一层(全清)", mscr.Windows.Count == 0);
            mscr.Deactivate();
        }

        // ── 多级弹窗截屏验证：渲染屏幕，最上层窗口可见，ESC 逐层关闭 ──
        {
            var mgr4 = TuiManager.Instance;
            bool entered4 = false;
            try
            {
                if (!mgr4.IsActive) { mgr4.Enter(); entered4 = true; }
                var schat = new ChatScreen();
                mgr4.PushScreen(schat);
                var wA = TuiDialog.Confirm("弹窗甲", "确认?", _ => { });
                var wB = TuiDialog.InputLine("弹窗乙", "输入", "", _ => { });
                // 用 ShowWindow（包装 OnClosed→CloseWindow）：ESC 触发 onCancel/onResult 后才会真正关窗
                schat.ShowWindow(wA);
                schat.ShowWindow(wB);
                mgr4.Render();
                var frame = AnsiString.Strip(mgr4.LastCleanFrame);
                // 弹出验证（截屏）：最上层窗口标题渲染可见（下层被上层居中窗口覆盖，不断言）
                Check("多级弹窗截屏: 两层渲染最上层标题可见", frame.Contains("弹窗乙"));
                // 关闭验证：树逐层关闭（先子后父）
                schat.OnKey(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false));
                Check("多级弹窗截屏: ESC 关上层(根=1,子=0)", schat.Windows.Count == 1 && schat.Windows[0].Children.Count == 0);
                schat.OnKey(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false));
                Check("多级弹窗截屏: ESC 关底层(全清)", schat.Windows.Count == 0);
                mgr4.PopScreen();
            }
            catch (Exception ex) { Check($"多级弹窗截屏: 异常 {ex.Message}", false); }
            finally
            {
                if (entered4) { try { mgr4.Exit(); } catch { } }
            }
        }

        // ── 标记里的渐变开关/配色（gradient / gradientStart / gradientEnd）──
        {
            static (bool? on, int? s, int? e) Pg(string? g, string? s = null, string? e = null)
                => WayCoder.UI.TUI.TuiMarkup.ParseGradient(g, s, e);

            Check("标记渐变: 没写 → 三项全 null（不动控件默认值）", Pg(null) is (null, null, null));
            Check("标记渐变: gradient=\"true\" 只开关不改色", Pg("true") is (true, null, null));
            Check("标记渐变: gradient=\"false\" 关", Pg("false") is (false, null, null));

            var warmGrad = TuiTheme.Current.GradOrangeYellow;
            var named = Pg("orangeYellow");
            Check("标记渐变: 语义名 → 开 + 取主题色",
                named.on == true && named.s == warmGrad.start && named.e == warmGrad.end);
            var alias = Pg("warning");
            Check("标记渐变: 别名 warning 同 orangeYellow", alias.s == warmGrad.start && alias.e == warmGrad.end);
            var btnNamed = Pg("btnOrangeYellow");
            Check("标记渐变: btn* 用按钮那套（比边框亮）",
                btnNamed.s == TuiTheme.Current.BtnOrangeYellow.start && btnNamed.s != warmGrad.start);

            // 显式色：写了就隐含开；与语义名同写时显式色赢（更具体）
            var explicitRgb = Pg(null, "#ff0000", "#00ff00");
            Check("标记渐变: 显式 #RRGGBB 隐含开",
                explicitRgb.on == true && explicitRgb.s == AnsiTty.RgbCode(255, 0, 0)
                    && explicitRgb.e == AnsiTty.RgbCode(0, 255, 0));
            Check("标记渐变: 显式色覆盖语义名",
                Pg("orangeYellow", "#ff0000").s == AnsiTty.RgbCode(255, 0, 0));
            Check("标记渐变: 认不出的名字当 false（不静默开渐变）", Pg("nosuchgradient").on == false);

            // 端到端：一份标记直接把渐变套到窗口和按钮上，无需 code-behind
            var gres = WayCoder.UI.TUI.TuiMarkup.Load(
                """
                <Dialog title="t" gradient="danger" width="30" height="8">
                  <VBox>
                    <Button id="b1" text="确定" gradient="btnGreenCyan" />
                    <Button id="b2" text="取消" gradientStart="#102030" gradientEnd="#405060" />
                    <Button id="b3" text="普通" />
                    <Button id="b4" text="扁平" gradient="false" />
                  </VBox>
                </Dialog>
                """);
            var dangerGrad = TuiTheme.Current.GradRedOrange;
            Check("标记渐变: 窗口边框走标记语义名",
                gres.Window!.GradientBorder && gres.Window.GradientStart == dangerGrad.start
                    && gres.Window.GradientEnd == dangerGrad.end);
            var gb1 = gres.Find<TuiButton>("b1")!;
            Check("标记渐变: 按钮语义名生效",
                gb1.GradientBg && gb1.GradientBgStart == TuiTheme.Current.BtnGreenCyan.start);
            var gb2 = gres.Find<TuiButton>("b2")!;
            Check("标记渐变: 按钮只写起止色也开",
                gb2.GradientBg && gb2.GradientBgStart == AnsiTty.RgbCode(0x10, 0x20, 0x30));
            // 标记不写特征 = 跟主题默认走；要扁平得显式 gradient="false"
            var gb3 = gres.Find<TuiButton>("b3")!;
            Check("标记渐变: 没写 gradient 的按钮跟主题默认",
                gb3.GradientBg == TuiTheme.Current.ButtonGradientByDefault
                    && gb3.GradientBgStart == TuiTheme.Current.ButtonGradient.start);
            Check("标记渐变: gradient=\"false\" 关掉默认渐变", !gres.Find<TuiButton>("b4")!.GradientBg);

            // 主题是按钮默认风格的单一开关：改主题 → 所有没显式设过的按钮跟着变
            var themeSaved = (TuiTheme.Current.ButtonGradientByDefault, TuiTheme.Current.ButtonGradient);
            try
            {
                TuiTheme.Current.ButtonGradient = TuiTheme.Current.BtnCyanBlue;
                Check("按钮默认风格: 换主题配色，未显式设色的按钮跟着变",
                    new TuiButton("x").GradientBgStart == TuiTheme.Current.BtnCyanBlue.start);
                Check("按钮默认风格: 显式设过色的按钮不被主题带跑",
                    gb2.GradientBgStart == AnsiTty.RgbCode(0x10, 0x20, 0x30));
                TuiTheme.Current.ButtonGradientByDefault = false;
                Check("按钮默认风格: 主题关渐变 → 默认按钮变扁平", !new TuiButton("x").GradientBg);
                Check("按钮默认风格: 显式 gradient=\"true\" 不受主题关闭影响",
                    gb1.GradientBg);
            }
            finally
            {
                TuiTheme.Current.ButtonGradientByDefault = themeSaved.ButtonGradientByDefault;
                TuiTheme.Current.ButtonGradient = themeSaved.ButtonGradient;
            }
        }

        // ── 模型选择器按键分类：能打出字符的键绝不当快捷键 ──
        // 回归防线：此前 S/I/O/L/K/A 与数字在「搜索框为空」时被当动作键，
        // 于是 openai / siliconflow / 4o 这类过滤词的首字符被吞 = 用户说的「过滤功能没有」
        {
            static ConsoleKeyInfo MpKey(ConsoleKey k, char ch, bool ctrl = false)
                => new(ch, k, false, false, ctrl);

            foreach (var (k, ch, name) in ((ConsoleKey, char, string)[])[
                (ConsoleKey.S, 's', "siliconflow"), (ConsoleKey.O, 'o', "openai"),
                (ConsoleKey.I, 'i', "import"), (ConsoleKey.K, 'k', "kimi"),
                (ConsoleKey.A, 'a', "anthropic"), (ConsoleKey.L, 'l', "llama"),
                (ConsoleKey.E, 'e', "ernie"), (ConsoleKey.D4, '4', "4o")])
            {
                Check($"模型框: 裸 {ch} 落回搜索框（能打出「{name}」）",
                    WayCoder.UI.TUI.Custom.ModelPicker.ClassifyKey(MpKey(k, ch), out _)
                        == WayCoder.UI.TUI.Custom.ModelPicker.EKeyAction.None);
            }
            // Delete 要留给输入框删字符，不能拿去删模型
            Check("模型框: 裸 Delete 落回搜索框",
                WayCoder.UI.TUI.Custom.ModelPicker.ClassifyKey(MpKey(ConsoleKey.Delete, '\0'), out _)
                    == WayCoder.UI.TUI.Custom.ModelPicker.EKeyAction.None);

            // Ctrl+字母 = 按钮加速键（按钮仍在，键只是快捷方式）
            foreach (var (k, act) in new (ConsoleKey, WayCoder.UI.TUI.Custom.ModelPicker.EKeyAction)[]
            {
                (ConsoleKey.T, WayCoder.UI.TUI.Custom.ModelPicker.EKeyAction.ToggleMode),
                (ConsoleKey.G, WayCoder.UI.TUI.Custom.ModelPicker.EKeyAction.AllSlots),
                (ConsoleKey.S, WayCoder.UI.TUI.Custom.ModelPicker.EKeyAction.Scan),
                (ConsoleKey.R, WayCoder.UI.TUI.Custom.ModelPicker.EKeyAction.Import),
                (ConsoleKey.O, WayCoder.UI.TUI.Custom.ModelPicker.EKeyAction.ImportOnline),
                (ConsoleKey.P, WayCoder.UI.TUI.Custom.ModelPicker.EKeyAction.SetKey),
                (ConsoleKey.L, WayCoder.UI.TUI.Custom.ModelPicker.EKeyAction.ClearKey),
                (ConsoleKey.N, WayCoder.UI.TUI.Custom.ModelPicker.EKeyAction.AddModel),
                (ConsoleKey.U, WayCoder.UI.TUI.Custom.ModelPicker.EKeyAction.EditModel),
                (ConsoleKey.D, WayCoder.UI.TUI.Custom.ModelPicker.EKeyAction.DeleteModel),
            })
            {
                Check($"模型框: Ctrl+{k} → {act}",
                    WayCoder.UI.TUI.Custom.ModelPicker.ClassifyKey(MpKey(k, '\0', true), out _) == act);
            }
            // 加速键不许碰 TuiEditBase.HandleCtrlKey 已占的编辑键 —— KeyHook 跑在它前面，
            // 占了等于把搜索框的全选/复制/粘贴/撤销抢走
            foreach (var k in new[] { ConsoleKey.A, ConsoleKey.C, ConsoleKey.X, ConsoleKey.V,
                                      ConsoleKey.Z, ConsoleKey.Y, ConsoleKey.E, ConsoleKey.K })
            {
                Check($"模型框: Ctrl+{k} 留给搜索框编辑",
                    WayCoder.UI.TUI.Custom.ModelPicker.ClassifyKey(MpKey(k, '\0', true), out _)
                        == WayCoder.UI.TUI.Custom.ModelPicker.EKeyAction.None);
            }
            Check("模型框: Tab 不被拦截（留给 TuiScreen 做焦点遍历）",
                WayCoder.UI.TUI.Custom.ModelPicker.ClassifyKey(MpKey(ConsoleKey.Tab, '\t'), out _)
                    == WayCoder.UI.TUI.Custom.ModelPicker.EKeyAction.None);
            Check("模型框: 空格不被拦截（焦点在按钮上时用来执行）",
                WayCoder.UI.TUI.Custom.ModelPicker.ClassifyKey(MpKey(ConsoleKey.Spacebar, ' '), out _)
                    == WayCoder.UI.TUI.Custom.ModelPicker.EKeyAction.None);
            Check("模型框: Enter 确认",
                WayCoder.UI.TUI.Custom.ModelPicker.ClassifyKey(MpKey(ConsoleKey.Enter, '\r'), out _)
                    == WayCoder.UI.TUI.Custom.ModelPicker.EKeyAction.Commit);
            Check("模型框: ↑ 导航列表",
                WayCoder.UI.TUI.Custom.ModelPicker.ClassifyKey(MpKey(ConsoleKey.UpArrow, '\0'), out _)
                    == WayCoder.UI.TUI.Custom.ModelPicker.EKeyAction.Nav);

            // F1-F10 → 槽位 0-9（沿用全局槽位约定）
            // 顺带说明作用域：模型框里的 F1-F10 是「选目标槽位」，与 ChatScreen 的「切槽位」同键不同义。
            // 两者不冲突正是靠窗口栈屏蔽 —— 见下面「键位两级作用域」一节
            var mpF1 = WayCoder.UI.TUI.Custom.ModelPicker.ClassifyKey(MpKey(ConsoleKey.F1, '\0'), out int mpS1);
            var mpF10 = WayCoder.UI.TUI.Custom.ModelPicker.ClassifyKey(MpKey(ConsoleKey.F10, '\0'), out int mpS10);
            Check("模型框: F1 → 槽位 0",
                mpF1 == WayCoder.UI.TUI.Custom.ModelPicker.EKeyAction.Slot && mpS1 == 0);
            Check("模型框: F10 → 槽位 9",
                mpF10 == WayCoder.UI.TUI.Custom.ModelPicker.EKeyAction.Slot && mpS10 == 9);
        }

        // ── 模型选择器交互：打字必须进搜索框并触发过滤；Tab 必须能切焦点 ──
        try
        {
            var mp3 = WayCoder.UI.TUI.TuiMarkup.LoadResource("dialogs/modelpicker.tui");
            var w3 = mp3.Window!;
            w3.OnResize(120, 30);
            var s3 = mp3.Find<TuiInput>("search")!;
            int changed = 0;
            s3.OnTextChanged = () => changed++;

            Check("模型框交互: 搜索框初始持有焦点", w3.RootView!.FindFocused() == s3);

            // 打字：经窗口 → 控件树 → 搜索框，文本变化并触发过滤回调
            w3.OnKey(new ConsoleKeyInfo('o', ConsoleKey.O, false, false, false));
            Check("模型框交互: 打字进搜索框", s3.Text == "o");
            Check("模型框交互: 打字触发过滤回调", changed > 0);

            // Tab 焦点序列必须包含按钮（嵌在 HBox 里，收集要递归）
            var foc = w3.RootView.GetAllFocusable();
            Check("模型框交互: 搜索框在焦点序列里", foc.Contains(s3));
            Check("模型框交互: 按钮在焦点序列里", foc.Contains(mp3.Find<TuiButton>("btnScan")!));
            Check("模型框交互: 表格在焦点序列里", foc.Contains(mp3.Find<TuiTableList>("table")!));

            // 窗口不能吞 Tab —— 吞了 TuiScreen 就没机会做焦点遍历
            var tabKey = new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false);
            Check("模型框交互: 窗口不吞 Tab（留给 TuiScreen 切焦点）", !w3.OnKey(tabKey));

            w3.RootView.FocusNext();
            Check("模型框交互: FocusNext 后焦点离开搜索框", w3.RootView.FindFocused() != s3);
        }
        catch (Exception ex) { Check($"模型框交互: 异常 {ex.Message}", false); }

        // ── 键位两级作用域：系统键（仅 Ctrl+C）穿透一切，其余全是窗口键 ──
        {
            static ConsoleKeyInfo ScK(ConsoleKey k, char ch = '\0', bool ctrl = false)
                => new(ch, k, false, false, ctrl);

            Check("键位作用域: Ctrl+C 是系统键", TuiKeyScope.IsSystemKey(ScK(ConsoleKey.C, 'c', true)));
            Check("键位作用域: 裸 C 不是系统键", !TuiKeyScope.IsSystemKey(ScK(ConsoleKey.C, 'c')));
            // 这几个此前在 REPL 里自称「系统级」，会穿透对话框，现已降级为窗口键
            foreach (var (k, name) in ((ConsoleKey, string)[])[
                (ConsoleKey.Q, "Ctrl+Q"), (ConsoleKey.K, "Ctrl+K"), (ConsoleKey.Z, "Ctrl+Z")])
                Check($"键位作用域: {name} 不是系统键（已降级为窗口键）",
                    !TuiKeyScope.IsSystemKey(ScK(k, '\0', true)));
            Check("键位作用域: F1 不是系统键", !TuiKeyScope.IsSystemKey(ScK(ConsoleKey.F1)));
            Check("键位作用域: Esc 不是系统键", !TuiKeyScope.IsSystemKey(ScK(ConsoleKey.Escape)));

            // 窗口栈屏蔽：F 键在 ChatScreen 上切槽位，弹出对话框后必须失效
            // （F5 是刷新键，避开；用 F3/F4）
            // Activate() 不能省：控件树是 BuildLayout 建的，裸 new 出来 PromptBar 还是 null
            var kscr = new ChatScreen { ActiveSlotIndex = 0 };
            kscr.Activate();
            kscr.OnKey(ScK(ConsoleKey.F3));
            Check("键位作用域: 无窗口时 F3 切到槽位 3", kscr.ActiveSlotIndex == 2);

            var modalWin = new TuiWindow { Modal = true, Width = 20, Height = 5 };
            kscr.AddWindow(modalWin);
            kscr.OnKey(ScK(ConsoleKey.F4));
            Check("键位作用域: 对话框开着时 F4 不切槽位（子窗口屏蔽父层）", kscr.ActiveSlotIndex == 2);

            kscr.CloseWindow(modalWin);
            kscr.OnKey(ScK(ConsoleKey.F4));
            Check("键位作用域: 对话框关闭后 F4 恢复生效（焦点回父层）", kscr.ActiveSlotIndex == 3);

            // 父子窗口注册同一个键：只有栈顶那个生效 ——「和上级窗口同一个按键也不冲突」
            string fired = "";
            var parentWin = new TuiWindow { Modal = true, Width = 20, Height = 5 };
            parentWin.RegisterShortcut(ConsoleKey.Enter, () => fired = "parent");
            var childWin = new TuiWindow { Modal = true, Width = 16, Height = 4 };
            childWin.RegisterShortcut(ConsoleKey.Enter, () => fired = "child");

            var scr2 = new ChatScreen();
            scr2.Activate();
            scr2.AddWindow(parentWin);
            scr2.OnKey(ScK(ConsoleKey.Enter, '\r'));
            Check("键位作用域: 只有父窗口时 Enter 归父窗口", fired == "parent");

            scr2.AddWindow(childWin);
            fired = "";
            scr2.OnKey(ScK(ConsoleKey.Enter, '\r'));
            Check("键位作用域: 子窗口在栈顶时 Enter 归子窗口（同键不冲突）", fired == "child");

            scr2.CloseWindow(childWin);
            fired = "";
            scr2.OnKey(ScK(ConsoleKey.Enter, '\r'));
            Check("键位作用域: 子窗口关闭后 Enter 归还父窗口", fired == "parent");

            // 非模态浮层（Toast）叠在模态之上：不抢焦点、不抢键，否则键会漏到根视图
            // （表现为对话框开着还能往聊天输入框里打字）
            var toast = new TuiWindow { Modal = false, Width = 10, Height = 3 };
            scr2.AddWindow(toast);
            Check("键位作用域: 非模态浮层不抢模态的焦点", scr2.FocusedWindow == parentWin);
            fired = "";
            scr2.OnKey(ScK(ConsoleKey.Enter, '\r'));
            Check("键位作用域: 非模态浮层叠在模态上，键仍归模态", fired == "parent");

            // Activate 订阅了 ContextManager.CompressProgress 静态事件，退出前退订，别泄漏到后续测试
            kscr.Deactivate();
            scr2.Deactivate();
        }

        // ── 增量刷新端到端：改了状态，下一帧就得真的把新内容写进去 ──
        // 光断言 IsDirty 不够 —— 真正的坑在渲染链路：TuiView.OnRender 只画脏叶子，
        // 漏标脏的控件即使数据变了也不会出现在增量帧里（表现＝「输入框/按钮不刷新」）
        {
            var rscr = new ChatScreen();
            rscr.Activate();
            var rwin = new TuiWindow { Modal = true, X = 2, Y = 2, Width = 40, Height = 8 };
            var rbox = new TuiVBox { Width = 38, Height = 6 };
            var rinp = new TuiInput { Width = 30, Height = 1 };
            var rlab = new TuiLabel("旧文案");
            var rbtn = new TuiButton("按钮甲");
            rbox.Add(rinp); rbox.Add(rlab); rbox.Add(rbtn);
            rwin.RootView = rbox;
            rscr.AddWindow(rwin);

            // 首帧全量：把所有脏标记清干净，之后只看「增量帧」带不带新内容
            var frame1 = new StringBuilder();
            rscr.IsIncrementalUpdate = false;
            rscr.Render(frame1);

            rinp.Focused = true;
            rinp.OnKey(new ConsoleKeyInfo('Z', ConsoleKey.Z, false, false, false));
            rlab.Text = "新文案";
            rbtn.Text = "按钮乙";

            var frame2 = new StringBuilder();
            rscr.IsIncrementalUpdate = true;
            rscr.Render(frame2);
            var inc2 = frame2.ToString();
            Check("增量刷新: 打的字进了下一帧", inc2.Contains('Z'));
            Check("增量刷新: 改了的标签文案进了下一帧", inc2.Contains("新文案"));
            // 按钮走渐变渲染，逐字符夹 ANSI 色码，整词不连续 —— 只能按字符断言
            Check("增量刷新: 改了的按钮文字进了下一帧", inc2.Contains('乙') && !inc2.Contains('甲'));

            // 什么都没改的那一帧不该重画这些控件（增量的意义在于少画）
            var frame3 = new StringBuilder();
            rscr.IsIncrementalUpdate = true;
            rscr.Render(frame3);
            var inc3 = frame3.ToString();
            Check("增量刷新: 无变化时不重画标签", !inc3.Contains("新文案"));

            rscr.Deactivate();
        }

        // ── 输入对话框 resize：提示折行变多 → 窗口高度要跟着重算，否则按钮被挤出窗口 ──
        // 用户报的「输入对话框改屏幕尺寸，按钮不见了」：窄屏下提示折成更多行，内容变高，
        // 但 resize 处理器只重算了宽度 —— 窗口还是老高度，底部按钮落在内容区外。
        // 断言走「真渲染一帧」而不是坐标：按钮被窗口内容区裁剪时，它的字不会出现在帧里。
        {
            var savedSz = Tty.SizeOverride;
            try
            {
                var longPrompt = "请在这里输入内容，这是一个足够长的提示文案，用来验证终端变窄时提示会折行";
                Tty.SizeOverride = (160, 40);
                var iscr = new ChatScreen();
                iscr.Activate();
                var inw = TuiDialog.InputLine("输入", longPrompt, "默认值", _ => { });
                iscr.AddWindow(inw);

                var wideFrame = new StringBuilder();
                iscr.IsIncrementalUpdate = false;
                iscr.Render(wideFrame);
                Check("输入框 resize: 宽屏确定按钮可见", wideFrame.ToString().Contains('确'));
                Check("输入框 resize: 宽屏取消按钮可见", wideFrame.ToString().Contains('取'));
                int wideH = inw.Height;

                // 窄屏 + 派发 resize（模拟终端缩放事件）
                Tty.SizeOverride = (60, 40);
                inw.OnResize(60, 40);

                var narrowFrame = new StringBuilder();
                iscr.IsIncrementalUpdate = false;
                iscr.Render(narrowFrame);
                var nf = narrowFrame.ToString();
                Check("输入框 resize: 窗口变窄", inw.Width < 120);
                Check("输入框 resize: 高度跟着内容重算（提示折行了所以变高）", inw.Height > wideH);
                Check("输入框 resize: 确定按钮仍可见（不被挤出窗口）", nf.Contains('确'));
                Check("输入框 resize: 取消按钮仍可见（不被挤出窗口）", nf.Contains('取'));

                // 多行版本同一条修复路径（先宽屏构建，再缩窄）
                Tty.SizeOverride = (160, 40);
                var mwin = TuiDialog.Input("输入", longPrompt, "默认值", _ => { });
                iscr.AddWindow(mwin);
                int mWideH = mwin.Height;
                Tty.SizeOverride = (60, 40);
                mwin.OnResize(60, 40);
                Check("多行输入框 resize: 高度跟着内容重算", mwin.Height > mWideH);
                var multiFrame = new StringBuilder();
                iscr.IsIncrementalUpdate = false;
                iscr.Render(multiFrame);
                Check("多行输入框 resize: 确定按钮仍可见", multiFrame.ToString().Contains('确'));

                iscr.Deactivate();
            }
            finally { Tty.SizeOverride = savedSz; }
        }

        // ── Diff 预览窗口：快捷键能按、尺寸合理、有按钮 ──
        // 用户报的「接近全屏、没有按键、快捷键按不了」
        {
            var dscr = new ChatScreen();
            dscr.Activate();
            var dhunks = WayCoder.UI.Tui.DiffPreview.BuildHunks(
                "line1\nline2\nline3\nline4", "line1\nline2-X\nline3\nline4\nline5-add");
            WayCoder.UI.Tui.DiffPreview.Decision dd = WayCoder.UI.Tui.DiffPreview.Decision.RejectAll;
            HashSet<int>? da = null;
            var dwin = WayCoder.UI.Tui.DiffPreview.BuildDiffWindow(dhunks, "t.txt", dscr,
                (d, a) => { dd = d; da = a; });
            dscr.AddWindow(dwin);

            // 尺寸：不该逼近全屏（宽度封顶 3/4 屏、高度 70% 屏）
            Check($"Diff 窗口: 高合理(≤{dscr.TH * 3 / 4}，实际 {dwin.Height})", dwin.Height <= dscr.TH * 3 / 4);
            Check($"Diff 窗口: 宽合理(≤{dscr.TW * 3 / 4}，实际 {dwin.Width})", dwin.Width <= dscr.TW * 3 / 4);

            // 有按钮（Tab 可聚焦）
            var dbtns = dwin.RootView!.GetAllFocusable().OfType<WayCoder.UI.Tui.Controls.TuiButton>().ToList();
            Check("Diff 窗口: 有可聚焦按钮", dbtns.Count >= 3);

            // A 直接全部接受（简化后不再需要 Y 确认）
            dscr.OnKey(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
            Check("Diff 窗口: A 直接全接受", dd == WayCoder.UI.Tui.DiffPreview.Decision.AcceptAll);

            // 重置后按 Q 完成 → 无接受项时 RejectAll
            dd = WayCoder.UI.Tui.DiffPreview.Decision.RejectAll;
            dscr.OnKey(new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false));
            Check("Diff 窗口: Q 完成(无接受→RejectAll)", dd == WayCoder.UI.Tui.DiffPreview.Decision.RejectAll);

            // ── Y 逐 hunk 接受立即生效：状态栏计数 + 已接受行打 ✓ ──
            // 此前按 Y 只标记 accepted 不前进、行又无 ✓ 标记，界面毫无变化 → 用户以为要确认两遍才生效
            {
                var dscr2 = new ChatScreen();
                dscr2.Activate();
                var dh2 = WayCoder.UI.Tui.DiffPreview.BuildHunks(
                    "l1\nl2\nl3\n", "l1\nL2-X\nl3\n");
                WayCoder.UI.Tui.DiffPreview.Decision dd2 = WayCoder.UI.Tui.DiffPreview.Decision.RejectAll;
                HashSet<int>? da2 = null;
                var dwin2 = WayCoder.UI.Tui.DiffPreview.BuildDiffWindow(dh2, "t.txt", dscr2,
                    (d, a) => { dd2 = d; da2 = a; });
                dscr2.AddWindow(dwin2);

                // 按 Y：立即接受 hunk(0)（无需二次确认），Q 提交 → Partial，接受的正是刚按 Y 的 hunk
                dscr2.OnKey(new ConsoleKeyInfo('y', ConsoleKey.Y, false, false, false));
                dscr2.OnKey(new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false));
                Check("Diff 快捷键: Y 立即接受 → Q 提交 Partial(1 hunk)",
                    dd2 == WayCoder.UI.Tui.DiffPreview.Decision.Partial && da2 != null && da2.Count == 1);

                dscr2.Deactivate();
            }

            // 双 hunk 连续按 Y：每次立即接受各自 hunk，无需中间确认
            {
                var dscr3 = new ChatScreen();
                dscr3.Activate();
                // 两处变更相距 7 行，hunk 合并算法不会并成一块（hunk 头连 diff 分隔线 2 个）
                // 变更行 2 与 10（11 行）：context 扩展区间不重叠 → 独立 2 hunk
                var dh3 = WayCoder.UI.Tui.DiffPreview.BuildHunks(
                    "a\nb\nc\nd\ne\nf\ng\nh\ni\nj\nk\n", "a\nB\nc\nd\ne\nf\ng\nh\ni\nJ\nk\n");
                WayCoder.UI.Tui.DiffPreview.Decision dd3 = WayCoder.UI.Tui.DiffPreview.Decision.RejectAll;
                HashSet<int>? da3 = null;
                var dwin3 = WayCoder.UI.Tui.DiffPreview.BuildDiffWindow(dh3, "t.txt", dscr3,
                    (d, a) => { dd3 = d; da3 = a; });
                dscr3.AddWindow(dwin3);

                Check("Diff 快捷键: 该输入产生 2 个 hunk", dh3.Count == 2);

                // 窗口直接驱动（绕过屏幕路由，聚焦验证 Y 的逐 hunk 接受逻辑）
                dwin3.OnKey(new ConsoleKeyInfo('y', ConsoleKey.Y, false, false, false));
                dwin3.OnKey(new ConsoleKeyInfo('y', ConsoleKey.Y, false, false, false));
                dwin3.OnKey(new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false));
                Check("Diff 快捷键: 连续两次 Y 各接受一个 hunk（Partial 2）",
                    dd3 == WayCoder.UI.Tui.DiffPreview.Decision.Partial && da3 != null && da3.Count == 2);

                dscr3.Deactivate();
            }

            dscr.Deactivate();
        }

        // ── 工具消息参数：按聊天区宽度截取，不再提前砍成 57 字符 ──
        {
            var tscr = new ChatScreen();
            tscr.Activate();
            tscr.ChatList.Width = 80;
            tscr.ChatList.Height = 20;
            var longBrief = string.Join(" ", Enumerable.Repeat("这是一个很长的工具参数片段用于验证截取", 10));
            tscr.AddToolProgress("bash", longBrief);
            var stored = tscr.ChatMessages.LastOrDefault(m => m.Role == "tool");
            var tw = AnsiHelper.DisplayWidth(stored?.Content ?? "");
            Check($"工具消息: 参数按聊天区宽截取({tw}≤76)", stored != null && tw <= 76);
            Check("工具消息: 截断时带省略号", tw == 76 ? stored!.Content.Contains('…') : stored!.Content.Contains('…') || tw < 76);
            // 短参数不被截（原本就不该动）
            tscr.ChatMessages.Clear();
            tscr.AddToolProgress("bash", "echo hello");
            var shortMsg = tscr.ChatMessages.LastOrDefault(m => m.Role == "tool");
            Check("工具消息: 短参数原样保留", shortMsg != null && shortMsg.Content.Contains("echo hello"));
            tscr.Deactivate();
        }

        // ── 写文件内容聊天区展示（ContentDiffFormatter）：«» 标记 diff 格式 ──
        {
            Section("[DiffPreview]");

            // 全量新增：3 行内容（结尾 \n 的空行应去掉）
            var added = ContentDiffFormatter.FormatAddedContent("a\nb\nc\n", "x.cs");
            Check("ContentDiff: 头行含路径与行数", added.StartsWith("«bright green»x.cs · 3 行«/»"));
            Check("ContentDiff: 行号+标记", added.Contains("   1 +a") && added.Contains("   2 +b") && added.Contains("   3 +c"));
            Check("ContentDiff: 绿色包裹每行", added.Contains("«bright green»   1 +a«/»"));
            Check("ContentDiff: 无尾部换行", !added.EndsWith("\n"));

            // 编辑 diff：old "a\nb\nc" → new "a\nX\nc"（第 2 行 b→X）
            var edit = ContentDiffFormatter.FormatEditContent("a\nb\nc", "a\nX\nc", "x.cs");
            Check("ContentDiff: 头行 +N/-M", edit.Contains("· +1/-1 行"));
            Check("ContentDiff: 含 hunk 头", edit.Contains("@@"));
            Check("ContentDiff: 删除行红色", edit.Contains("«bright red»   2 -b«/»"));
            Check("ContentDiff: 新增行绿色", edit.Contains("«bright green»   2 +X«/»"));
            Check("ContentDiff: 上下文灰色", edit.Contains("«grey»   1  a«/»") && edit.Contains("«grey»   3  c«/»"));

            // CRLF 归一化：\r\n 拆行不花屏
            var crlf = ContentDiffFormatter.FormatAddedContent("a\r\nb\r\n", "win.cs");
            Check("ContentDiff: CRLF 归一化", crlf.Contains("1 +a") && crlf.Contains("2 +b") && !crlf.Contains('\r'));

            // maxLines 截断：头行 + 5 行 + 截断提示 = 7 行
            var big = string.Join("\n", Enumerable.Range(1, 3000).Select(i => $"line{i}")) + "\n";
            var capped = ContentDiffFormatter.FormatAddedContent(big, "big.cs", maxLines: 5);
            Check("ContentDiff: maxLines 截断", capped.Contains("仅显示前 5 行") && capped.Split('\n').Length == 7);

            // RenderMessage 纯文本解码出彩色片段（绿 92 / 红 91 / 灰 90）
            var addedSegRows = WayCoder.UI.Tui.TuiMarkdown.RenderMessage(added, "tool", 80, plainText: true);
            Check("ContentDiff: 渲染绿色片段", addedSegRows.Any(r => r.Any(s => s.Fg == 92)));
            var editSegRows = WayCoder.UI.Tui.TuiMarkdown.RenderMessage(edit, "tool", 80, plainText: true);
            Check("ContentDiff: 渲染红/灰片段", editSegRows.Any(r => r.Any(s => s.Fg == 91)) &&
                                                editSegRows.Any(r => r.Any(s => s.Fg == 90)));

            // 默认开关开启（WAYCODER_WRITE_CONTENT_VIEW）
            Check("ContentDiff: 默认开关开启", Config.Instance.WriteContentView);
        }

        // ── 标题栏：左上角恒为商标，工具事件不整行重绘（防闪烁） ──
        {
            var tscr2 = new ChatScreen();
            tscr2.Activate();
            tscr2.ChatList.Width = 70;

            // 渲染一帧清掉所有脏标记，此后只观察「谁被标脏」
            var f0 = new StringBuilder();
            tscr2.IsIncrementalUpdate = false;
            tscr2.Render(f0);
            tscr2.TitleBar.ClearDirty();

            // 商标不被模型名顶掉：Render 里 Title 恒为 AppFullName
            var tb = tscr2.TitleBar;
            Check("标题栏: Title 恒为商标名", tb.Title == Global.AppFullName);

            // 工具事件只标动态栏，不标标题栏（标题栏重绘 = 金色渐变整行重画 = 闪烁）
            tscr2.OnToolStarted("bash", "echo hi");
            Check("标题栏: 工具开始不标脏标题栏", !tb.IsDirty);
            tscr2.OnToolFinished();
            Check("标题栏: 工具结束不标脏标题栏", !tb.IsDirty);
            tscr2.OnPermissionWaiting("write_file");
            Check("标题栏: 权限等待不标脏标题栏", !tb.IsDirty);

            // 聊天加消息只走 ChatList 的 MarkDirtyTree，标题栏保持干净
            tscr2.AddMessage("普通助手消息", "assistant");
            Check("标题栏: 加聊天消息不标脏标题栏", !tb.IsDirty);

            // 但聊天列表确实被标脏（消息要显示出来）
            Check("标题栏: 聊天列表被标脏（消息要显示）", tscr2.ChatList.IsDirty);

            tscr2.Deactivate();
        }

        // ── 统一配色：对话框灰底黑字，控件黑底白字，选中反色 ──
        var th = TuiTheme.Current;
        Check("主题: 对话框灰底", th.WindowBg == AnsiColors.PanelGrey);
        Check("主题: 对话框黑字", th.DialogFg == AnsiColors.Black);
        Check("主题: 输入框黑底白字", th.InputBg == AnsiColors.BgBlack && th.InputFg == AnsiColors.White);
        Check("主题: 列表黑底白字", th.ListBg == AnsiColors.BgBlack && th.ListFg == AnsiColors.White);
        Check("主题: 树黑底白字", th.TreeViewBg == AnsiColors.BgBlack && th.TreeViewFg == AnsiColors.White);
        // 按钮两条渲染路径的前景色必须分开：扁平=黑底白字，渐变=亮底黑字。
        // 共用一个字段就会二选一糊掉（白字压橙黄渐变几乎看不见）
        Check("主题: 扁平按钮黑底白字", th.ButtonBg == AnsiColors.BgBlack && th.ButtonFg == AnsiColors.White);
        Check("主题: 渐变按钮黑字", th.ButtonGradientFg == AnsiColors.Black);
        Check("主题: 渐变按钮前景不与扁平共用", th.ButtonGradientFg != th.ButtonFg);
        // 「选中行反色」= 选中的 fg/bg 恰好是正文 fg/bg 对调（白字黑底 ↔ 黑字白底）
        Check("主题: 列表选中反色", th.ListSelFg == AnsiColors.Black && th.ListSelBg == AnsiColors.BgWhite);
        Check("主题: 树选中反色", th.TreeViewSelBg == AnsiColors.BgWhite);

        // /test dialog 巡检清单：每个窗口式对话框都必须能构建出来，且配色统一。
        // LoadDialog 找不到 .tui 里的 id 会直接抛 —— 光靠人工弹窗，改错的那个要弹到才发现
        foreach (var dlgName in DialogWalk.Targets)
        {
            if (dlgName is "toast" or "menu") continue;              // 非窗口，要活的 ChatScreen
            if (dlgName is "model" or "session" or "reasoning" or "palette" or "file") continue; // 阻塞式全屏
            try
            {
                var built = DialogWalk.Build(dlgName, null!, _ => { });
                Check($"/test dialog {dlgName}: 可构建", built != null);
                if (built == null) continue;

                // 底色不许各写各的（此前 permission 黄底、modelpicker 黑底）
                Check($"/test dialog {dlgName}: 灰底", built.WinBg == AnsiColors.PanelGrey);

                // 按钮走彩色渐变底（RGB，必须 ≥0x1000000，否则 TuiButton 悄悄回退成扁平分支）
                var btns = new List<TuiButton>();
                CollectButtons(built.RootView, btns);
                foreach (var b in btns)
                    Check($"/test dialog {dlgName}: 按钮「{b.Text}」渐变底",
                        b.GradientBg && b.GradientBgStart >= 0x1000000 && b.GradientBgEnd >= 0x1000000);
            }
            catch (Exception ex) { Check($"/test dialog {dlgName}: 构建抛异常 {ex.Message}", false); }
        }

        // 权限框两个实测 bug：宽度不看内容（一律占屏宽 3/4）、模板 "…" 占位标签没清
        var permShort = TuiDialog.Permission("权限确认", "允许？", _ => { });
        var permLong = TuiDialog.Permission("权限确认",
            "允许执行 " + new string('x', 200) + " 吗", _ => { });
        Check("权限框: 宽度跟着内容走（长消息更宽）", permLong.Width > permShort.Width);
        Check("权限框: 窄消息不撑到屏宽 3/4", permShort.Width < Math.Max(12, (int)(Tty.Cols * 0.75)));
        var permLabels = new List<TuiLabel>();
        CollectLabels(permShort.RootView, permLabels);
        Check("权限框: 无残留 … 占位标签", permLabels.TrueForAll(l => l.Text != "…"));

        // ── 终端缩放：内容必须跟着重算，不能只动外框 ──
        // TuiWindow.OnResizeContent 这个钩子注释写着「由 TuiDialog 工厂方法设置」，
        // 但在此之前没有任何对话框注册过（只有自测在用）——于是缩终端时标签还按老宽度折行。
        var savedSize = Tty.SizeOverride;
        try
        {
            var longMsg = string.Join("", Enumerable.Repeat("宽消息", 40));
            Tty.SizeOverride = (160, 40);
            var rz = TuiDialog.Confirm("确认", longMsg, _ => { });
            Check("对话框 resize: 注册了内容重算回调", rz.OnResizeContent != null);

            int wideW = rz.Width;
            var wideLines = new List<TuiLabel>();
            CollectLabels(rz.RootView, wideLines);

            // 缩到 60 列并派发 resize
            Tty.SizeOverride = (60, 40);
            rz.OnResize(60, 40);
            int narrowW = rz.Width;
            var narrowLines = new List<TuiLabel>();
            CollectLabels(rz.RootView, narrowLines);

            Check("对话框 resize: 窗口变窄", narrowW < wideW);
            Check("对话框 resize: 不超出新终端宽", narrowW <= 60);
            // 内容真的重新折行了：窄屏下同样的消息要占更多行
            Check("对话框 resize: 消息重新折行（行数变多）", narrowLines.Count > wideLines.Count);
            Check("对话框 resize: 标签宽度跟着缩", narrowLines.TrueForAll(l => l.Width <= 60));

            // 输入/选择框系走 ApplyContentWidth（宽度按屏宽比例），此前 XScale=0 之后
            // 连外框都不响应 resize —— 现在把「刷宽度到控件」本身注册成 resize 处理器
            Tty.SizeOverride = (160, 40);
            var sel = TuiDialog.Select("选择", ["甲", "乙", "丙"], _ => { });
            var selList = sel.RootView is not null ? FindFirstList(sel.RootView) : null;
            Check("选择框 resize: 注册了内容重算回调", sel.OnResizeContent != null);
            Check("选择框: 列表存在", selList != null);
            int selWideW = sel.Width, listWideW = selList?.Width ?? 0;

            Tty.SizeOverride = (60, 40);
            sel.OnResize(60, 40);
            Check("选择框 resize: 窗口变窄", sel.Width < selWideW);
            Check("选择框 resize: 列表控件也跟着变窄", (selList?.Width ?? 0) < listWideW);
            Check("选择框 resize: 不超出新终端宽", sel.Width <= 60);
        }
        finally { Tty.SizeOverride = savedSize; }
        Console.WriteLine();

        // ================================================================
        // 系统消息框自适应宽高（按消息内容计算）
        // ================================================================
    }
}