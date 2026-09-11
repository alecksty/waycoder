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
    private static void TestChunk8(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[TuiListView]");
        var lv = new TuiListView();
        Check("TuiListView 创建", lv != null);
        Check("TuiListView ItemCount=0", lv!.ItemCount == 0);
        Check("TuiListView SelectedIndex=-1", lv.SelectedIndex == -1);
        Check("TuiListView IsAutoScrollToEnd=true", lv.IsAutoScrollToEnd);

        lv.AddItem(new TuiLabel("事项 1"));
        lv.AddItem(new TuiLabel("事项 2"));
        lv.AddItem(new TuiLabel("事项 3"));
        Check("TuiListView AddItem x3", lv.ItemCount == 3);

        lv.SelectItem(1);
        Check("TuiListView SelectItem(1)", lv.SelectedIndex == 1);
        lv.SelectNext();
        Check("TuiListView SelectNext → 2", lv.SelectedIndex == 2);
        lv.SelectNext();
        Check("TuiListView SelectNext 循环 → 0", lv.SelectedIndex == 0);
        lv.SelectPrev();
        Check("TuiListView SelectPrev 循环 → 2", lv.SelectedIndex == 2);

        bool itemActivated = false; int actIdx = -1;
        lv.OnItemActivated = i => { itemActivated = true; actIdx = i; };
        lv.IsEnabled = true;
        lv.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
        Check("TuiListView Enter 触发 OnItemActivated", itemActivated && actIdx == 2);

        // 滚动
        lv.ScrollToTop();
        Check("TuiListView ScrollToTop offset=0", lv.ScrollOffset == 0);

        // 移除
        var lv2 = new TuiListView();
        lv2.AddItem(new TuiLabel("x"));
        lv2.AddItem(new TuiLabel("y"));
        lv2.RemoveItem(0);
        Check("TuiListView RemoveItem", lv2.ItemCount == 1);

        // ContentHeight
        var lv3 = new TuiListView();
        lv3.AddItem(new TuiLabel("h") { Height = 3 });
        Check("TuiListView ContentHeight > 0", lv3.ContentHeight > 0);

        // 滚动刷新回归：滚动改变视口偏移时必须标记叶子子项为脏，
        // 否则增量渲染只清背景（Fill 视口）而不重绘非脏的 TuiMarkdown 叶子 → 聊天滚动花屏。
        var lv4 = new TuiListView { Height = 4, Width = 60 };
        var md = WayCoder.UI.Tui.Controls.TuiMarkdown.Create("滚动测试内容", "assistant", 60);
        lv4.AddItem(md);
        lv4.ClearDirty();
        md.ClearDirty();
        lv4.ScrollToTop();
        Check("TuiListView ScrollToTop 标记叶子脏", md.IsDirty);
        md.ClearDirty();
        lv4.ScrollDown(1);
        Check("TuiListView ScrollDown 标记叶子脏", md.IsDirty);
        md.ClearDirty();
        lv4.ScrollUp(1);
        Check("TuiListView ScrollUp 标记叶子脏", md.IsDirty);
        md.ClearDirty();
        lv4.ScrollToBottom();
        Check("TuiListView ScrollToBottom 标记叶子脏", md.IsDirty);

        // TuiScrollView 同样滚动位移需标记子项脏
        var sv = new TuiScrollView { Height = 4, Width = 60 };
        var svItem = new TuiLabel("内容") { Height = 1 };
        sv.Add(svItem);
        sv.ClearDirty();
        svItem.ClearDirty();
        sv.ScrollToTop();
        Check("TuiScrollView ScrollToTop 标记子项脏", svItem.IsDirty);

        // 翻页边界 no-op 回归：已在顶部再上翻 / 已在底部再下翻，滚动偏移不变 → 不标脏 → 不闪屏。
        var lv5 = new TuiListView { Height = 4, Width = 60 };
        for (int i = 0; i < 10; i++) lv5.AddItem(new TuiLabel("行" + i) { Height = 1 });
        // 先滚到底
        lv5.ScrollToBottom();
        Check("TuiListView 满内容滚到底 offset=6", lv5.ScrollOffset == 6);
        lv5.ClearDirty();
        foreach (var c in lv5.Children) c.ClearDirty();
        lv5.ScrollDown(3); // 已在底部 → 无效
        Check("TuiListView 底部再下翻无效(offset 不变)", lv5.ScrollOffset == 6);
        Check("TuiListView 底部再下翻不标脏", !lv5.Children[0].IsDirty);
        // 滚到顶
        lv5.ScrollToTop();
        Check("TuiListView 滚到顶 offset=0", lv5.ScrollOffset == 0);
        lv5.ClearDirty();
        foreach (var c in lv5.Children) c.ClearDirty();
        lv5.ScrollUp(3); // 已在顶部 → 无效
        Check("TuiListView 顶部再上翻无效(offset 不变)", lv5.ScrollOffset == 0);
        Check("TuiListView 顶部再上翻不标脏", !lv5.Children[0].IsDirty);

        // TuiScrollView 同样边界 no-op
        var sv2 = new TuiScrollView { Height = 4, Width = 60 };
        for (int i = 0; i < 10; i++) sv2.Add(new TuiLabel("内容" + i) { Height = 1 });
        sv2.ScrollToBottom();
        sv2.ClearDirty();
        foreach (var c in sv2.Children) c.ClearDirty();
        sv2.ScrollDown(1);
        Check("TuiScrollView 底部再下翻不标脏", !sv2.Children[0].IsDirty);
        sv2.ScrollToTop();
        sv2.ClearDirty();
        foreach (var c in sv2.Children) c.ClearDirty();
        sv2.ScrollUp(1);
        Check("TuiScrollView 顶部再上翻不标脏", !sv2.Children[0].IsDirty);

        // 滚动状态机同源：TuiScrollView / TuiListView 的 ScrollOffset + 四个滚动方法 + 跟底标志
        // 收在 TuiScrollable，两类的差异只剩「无参步长」（列表 3 行/格、滚动视图 1 行）。
        Check("TuiScrollable: ListView 一格步长=3", lv5.ScrollStepLines == 3);
        Check("TuiScrollable: ScrollView 一格步长=1", sv2.ScrollStepLines == 1);
        Check("TuiScrollable: 两类同源（共享同一状态机）",
            lv5 is TuiScrollable && sv2 is TuiScrollable);

        // 视口变高 → 旧 offset 越界，OnResize 必须重新钳制。此前只有 TuiScrollView 覆写了
        // OnResize 做这件事，列表视图没有 ⇒ 缩放/截图后列表停在越界的空白区。
        var lv6 = new TuiListView { Height = 4, Width = 60 };
        for (int i = 0; i < 10; i++) lv6.AddItem(new TuiLabel("行" + i) { Height = 1 });
        lv6.ScrollToBottom();
        Check("TuiListView 视口 4 行时滚到底 offset=6", lv6.ScrollOffset == 6);
        lv6.Height = 8;                       // 视口变高 → maxScroll 6→2
        lv6.OnResize(60, 8);
        Check("TuiListView 视口变高后 offset 被钳回 2", lv6.ScrollOffset == 2);

        var sv3 = new TuiScrollView { Height = 4, Width = 60 };
        for (int i = 0; i < 10; i++) sv3.Add(new TuiLabel("内容" + i) { Height = 1 });
        sv3.Layout();                 // 先让 ContentHeight=10 成立：否则 OnResize 里的「内容增长自动跟底」
                                      // 会顺带把 offset 调到新底部，掩盖真正要验的「钳制」这一步
        sv3.ScrollToBottom();
        sv3.ScrollUp(1);              // 退出跟底（用户手动上翻），offset=5
        Check("TuiScrollView 退出跟底后 offset=5", sv3.ScrollOffset == 5);
        sv3.Height = 8;
        sv3.OnResize(60, 8);
        Check("TuiScrollView 视口变高后 offset 同样被钳回 2", sv3.ScrollOffset == 2);

        Console.WriteLine();

        // ================================================================
        // TuiDialog 关闭协议：落 Result → 跑回调 → 触发 OnClosed，三件齐活
        // ================================================================
        Section("[TuiDialog 关闭协议]");

        // 关一个模态窗必须按序做完三件事，缺一即坏事：
        //   漏 OnClosed ⇒ 窗口永不关闭（渲染等待循环等不到事件，调用方一直挂着）；
        //   漏 Result   ⇒ 调用方读到默认值（object? 的 -1 / int? 的 null），把「取消」读成「确认」；
        //   回调跑在 Result 之前 ⇒ 被回调唤醒的调用方读到上一轮的旧值。
        // 此前这三行在 TuiDialog 的 8 个构建器里手抄了 39 处（+TuiMenu 2 处），
        // 现在只有 TuiWindow.Close 一个出口 —— 这组断言就是那个出口的合同。
        static (bool Closed, object? Result) FireClose(TuiWindow w, ConsoleKey key)
        {
            bool closed = false;
            w.OnClosed = () => closed = true;
            w.KeyShortcuts[key]();   // 直接跑注册的快捷键体，等价于用户按下该键
            return (closed, w.Result);
        }

        {
            bool cbYes = false;
            var (closed, res) = FireClose(TuiDialog.Confirm("确认", "继续?", r => cbYes = r), ConsoleKey.Y);
            Check("关闭协议 Confirm/Y: Result=true、回调收到 true、已关窗", res is true && cbYes && closed);
        }
        {
            bool cbNo = true;
            var (closed, res) = FireClose(TuiDialog.Confirm("确认", "继续?", r => cbNo = r), ConsoleKey.Escape);
            Check("关闭协议 Confirm/Esc: Result=false、回调收到 false、已关窗", res is false && !cbNo && closed);
        }
        {
            TuiDialog.EDialogResult? cb3 = null;
            var (closed, res) = FireClose(TuiDialog.Confirm3("三选", "?", r => cb3 = r), ConsoleKey.N);
            Check("关闭协议 Confirm3/N: Result=No、回调一致、已关窗",
                res is TuiDialog.EDialogResult.No && cb3 == TuiDialog.EDialogResult.No && closed);
        }
        {
            TuiDialog.EDialogResult? cbPerm = null;
            var (closed, res) = FireClose(TuiDialog.Permission("权限", "允许?", r => cbPerm = r), ConsoleKey.Y);
            Check("关闭协议 Permission/Y: Result=Yes、回调一致、已关窗",
                res is TuiDialog.EDialogResult.Yes && cbPerm == TuiDialog.EDialogResult.Yes && closed);
        }
        {
            bool closed = false;
            var w = TuiDialog.Info("信息", "内容");
            w.OnClosed = () => closed = true;
            w.KeyShortcuts[ConsoleKey.Enter]();
            Check("关闭协议 Info/Enter: Result=Ok 且已关窗",
                w.Result is TuiDialog.EDialogResult.Ok && closed);
        }

        // Esc 取消路径逐个走一遍 —— 走**屏幕真实路径**（ShowWindow + screen.OnKey），
        // 不直接跑快捷键体：注册了 Esc 的窗口走 win.OnKey → win.Close()，
        // 而消息框（Info/Success/Warn/Error）**没注册 Esc**，由 TuiScreen.OnKey 兜底
        // 直接触发 OnClosed（Result 保持默认 -1、无人读）。两条路都必须真关窗 ——
        // 按 Esc 关不掉对话框是致命的，而这条「没注册」的事实只有走屏幕路径才测得到。
        (string Name, Func<TuiWindow> Make)[] escCases =
        [
            ("Info",        () => TuiDialog.Info("t", "m")),
            ("Confirm",     () => TuiDialog.Confirm("t", "m", _ => { })),
            ("Confirm3",    () => TuiDialog.Confirm3("t", "m", _ => { })),
            ("Input",       () => TuiDialog.Input("t", "p", "", _ => { })),
            ("InputLine",   () => TuiDialog.InputLine("t", "p", "", _ => { })),
            ("Secret",      () => TuiDialog.Secret("t", "p", "", _ => { })),
            ("FindReplace", () => TuiDialog.FindReplace("f", "r", new FindOptions(),
                                (_, _) => { }, (_, _, _) => { }, (_, _, _) => { })),
            ("Select",      () => TuiDialog.Select("t", ["A", "B"], _ => { })),
            ("MultiSelect", () => TuiDialog.MultiSelect("t", ["A", "B"], _ => { })),
            ("Ask",         () => TuiDialog.Ask("t", "m", ["A", "B"], false, _ => { }, _ => { })),
            ("Permission",  () => TuiDialog.Permission("t", "m", _ => { })),
        ];
        {
            var escScreen = new ChatScreen();
            escScreen.Activate();
            var escK = new ConsoleKeyInfo('', ConsoleKey.Escape, false, false, false);
            try
            {
                foreach (var (name, make) in escCases)
                {
                    try
                    {
                        escScreen.ShowWindow(make());
                        escScreen.OnKey(escK);
                        bool gone = escScreen.Windows.Count == 0;
                        Check($"关闭协议 {name}/Esc(屏幕路径): 已关窗", gone);
                        if (!gone) escScreen.CloseAllModals(); // 防残留窗口污染后续用例
                    }
                    catch (Exception ex) { Check($"关闭协议 {name}/Esc: 异常 {ex.Message}", false); }
                }
            }
            finally { escScreen.Deactivate(); }
        }

        Console.WriteLine();

        // ================================================================
        // TuiProgress 测试
        // ================================================================
        Section("[TuiProgress]");
        var prog1 = new TuiProgress();
        Check("TuiProgress 创建", prog1 != null);
        Check("TuiProgress 默认 Percent=0", prog1!.Percent == 0);
        Check("TuiProgress CanFocus=false", !prog1.CanFocus);
        Check("TuiProgress Height=1", prog1.Height == 1);
        Check("TuiProgress Width=40", prog1.Width == 40);

        prog1.Percent = 75;
        Check("TuiProgress Percent=75", prog1.Percent == 75);

        prog1.Label = "编译中";
        Check("TuiProgress Label 设置", prog1.Label == "编译中");

        // 边界值
        prog1.Percent = 0;
        Check("TuiProgress Percent=0 边界", prog1.Percent == 0);
        prog1.Percent = 100;
        Check("TuiProgress Percent=100 边界", prog1.Percent == 100);
        Console.WriteLine();

        // ================================================================
        // TuiSpinner 测试
        // ================================================================
        Section("[TuiSpinner]");
        var spin1 = new TuiSpinner("加载中");
        Check("TuiSpinner 创建", spin1 != null);
        Check("TuiSpinner Label", spin1!.Label == "加载中");
        Check("TuiSpinner CanFocus=false", !spin1.CanFocus);

        // 帧推进
        var frames = new HashSet<string>();
        for (int i = 0; i < 8; i++) { frames.Add(spin1.Frame); spin1.Tick(); }
        Check("TuiSpinner 8 帧全部不同（循环）", frames.Count == 8);

        // 无标签创建
        var spin2 = new TuiSpinner();
        Check("TuiSpinner 无标签 Label 为空", spin2.Label == "");
        Console.WriteLine();

        // ================================================================
        // TuiStatusBar 测试
        // ================================================================
        Section("[TuiStatusBar]");
        var sb1 = new TuiStatusBar();
        Check("TuiStatusBar 创建", sb1 != null);
        Check("TuiStatusBar CanFocus=false", !sb1!.CanFocus);
        Check("TuiStatusBar Height=1", sb1.Height == 1);
        Check("TuiStatusBar SlotStates 长度=10", sb1.SlotStates.Length == 10);
        Check("TuiStatusBar ActiveSlotIndex=0", sb1.ActiveSlotIndex == 0);

        sb1.ActiveSlotIndex = 3;
        Check("TuiStatusBar ActiveSlotIndex=3", sb1.ActiveSlotIndex == 3);

        sb1.HintText = "F1 帮助";
        Check("TuiStatusBar HintText", sb1.HintText == "F1 帮助");

        sb1.RightText = "12K tokens";
        Check("TuiStatusBar RightText", sb1.RightText == "12K tokens");

        sb1.AgentBusy = true;
        Check("TuiStatusBar AgentBusy=true", sb1.AgentBusy);

        // 工作模式/经济模式/动画图标已移入动态栏与模型信息行，状态栏不再重复显示。
        sb1.Width = 120;
        sb1.AgentBusy = false;
        foreach (var (mode, label) in new[]
        {
            (WorkMode.Build, "建造"), (WorkMode.Plan, "计划"),
            (WorkMode.Chat, "聊天"),
        })
        {
            sb1.CurrentWorkMode = mode;
            var modeFrame = new StringBuilder();
            sb1.Render(modeFrame, 0, 0);
            var modePlain = ScreenshotTool.StripAnsi(modeFrame.ToString());
            Check($"TuiStatusBar 不再重复模式名「{label}」", !modePlain.Contains(label));
        }
        Console.WriteLine();

        // ================================================================
        // TuiTabs 测试
        // ================================================================
        Section("[TuiTabs]");
        var tabs = new TuiTabs();
        Check("TuiTabs 创建", tabs != null);
        Check("TuiTabs Count=0", tabs!.Count == 0);

        tabs.AddTab("聊天", new TuiLabel("chat"));
        tabs.AddTab("文件", new TuiLabel("files"));
        tabs.AddTab("设置", new TuiLabel("settings"));
        Check("TuiTabs AddTab x3", tabs.Count == 3);
        Check("TuiTabs SelectedIndex=0", tabs.SelectedIndex == 0);

        tabs.SelectTab(2);
        Check("TuiTabs SelectTab(2)", tabs.SelectedIndex == 2);
        Check("TuiTabs ActiveContent 非空", tabs.ActiveContent != null);

        tabs.SelectNext();
        Check("TuiTabs SelectNext 循环", tabs.SelectedIndex == 0);
        tabs.SelectPrev();
        Check("TuiTabs SelectPrev 循环", tabs.SelectedIndex == 2);

        // 键盘导航
        tabs.Focused = true;
        tabs.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false));
        Check("TuiTabs LeftArrow", tabs.SelectedIndex == 1);
        tabs.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false));
        Check("TuiTabs RightArrow", tabs.SelectedIndex == 2);

        // 数字键快速切换
        tabs.OnKey(new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false));
        Check("TuiTabs 数字键1 切换", tabs.SelectedIndex == 0);

        // RemoveTab
        tabs.RemoveTab(1);
        Check("TuiTabs RemoveTab → Count=2", tabs.Count == 2);

        // 选择回调
        int? selTabIdx = null;
        tabs.OnSelectionChanged = i => selTabIdx = i;
        tabs.SelectTab(1);
        Check("TuiTabs OnSelectionChanged", selTabIdx == 1);

        // 空 tabs
        var tabsEmpty = new TuiTabs();
        Check("TuiTabs 空列表 ActiveContent=null", tabsEmpty.ActiveContent == null);
        Console.WriteLine();

        // ================================================================
        // TuiTitleBar 测试
        // ================================================================
        Section("[TuiTitleBar]");
        var titleBar = new TuiTitleBar();
        Check("TuiTitleBar 创建", titleBar != null);
        Check("TuiTitleBar CanFocus=false", !titleBar!.CanFocus);
        Check("TuiTitleBar Height=1", titleBar.Height == 1);

        titleBar.Title = "WayCoder";
        Check("TuiTitleBar Title", titleBar.Title == "WayCoder");

        titleBar.GitBranch = "main";
        Check("TuiTitleBar GitBranch", titleBar.GitBranch == "main");

        titleBar.Version = "v1.0";
        Check("TuiTitleBar Version", titleBar.Version == "v1.0");
        Console.WriteLine();

        // ================================================================
        // TuiBanner 测试
        // ================================================================
        Section("[TuiBanner]");
        var banner = new TuiBanner();
        Check("TuiBanner 创建", banner != null);
        Check("TuiBanner CanFocus=false", !banner!.CanFocus);
        Check("TuiBanner Height=3", banner.Height == 3);

        banner.Title = "WayCoder 道码";
        Check("TuiBanner Title", banner.Title == "WayCoder 道码");

        banner.Subtitle = "v2.0 — 中文编程助手";
        Check("TuiBanner Subtitle", banner.Subtitle == "v2.0 — 中文编程助手");
        Console.WriteLine();

        // ================================================================
        // TuiGrid 测试
        // ================================================================
        Section("[TuiGrid]");
        // GridSize
        var gs10 = GridSize.Parse("10");
        Check("GridSize.Parse('10') fixed", !gs10.IsStar && gs10.Value == 10);

        var gsStar = GridSize.Parse("20*");
        Check("GridSize.Parse('20*') star", gsStar.IsStar && gsStar.Value == 20);

        var gsAuto = GridSize.Parse("*");
        Check("GridSize.Parse('*') 默认权重=1", gsAuto.IsStar && gsAuto.Value == 1);

        var gsList = GridSize.ParseList("10,20*,*");
        Check("GridSize.ParseList 3个", gsList.Length == 3);
        Check("GridSize.ParseList[0] fixed", !gsList[0].IsStar);
        Check("GridSize.ParseList[1] star", gsList[1].IsStar);
        Check("GridSize.ParseList[2] auto star", gsList[2].IsStar && gsList[2].Value == 1);

        // 空解析
        Check("GridSize.ParseList null", GridSize.ParseList(null).Length == 0);
        Check("GridSize.ParseList 空", GridSize.ParseList("").Length == 0);

        // Grid 创建
        var grid = new TuiGrid { Width = 80, Height = 24 };
        Check("TuiGrid 创建", grid != null);
        Check("TuiGrid Rows=0", grid!.Rows == 0);
        Check("TuiGrid Columns=0", grid.Columns == 0);

        grid.RowDefinitions = "5,10*,10*";
        grid.ColumnDefinitions = "30,70*";
        Check("TuiGrid RowDefinitions", grid.RowDefinitions == "5,10*,10*");
        Check("TuiGrid ColumnDefinitions", grid.ColumnDefinitions == "30,70*");

        grid.Add(new TuiLabel("Cell"), row: 0, col: 0);
        Check("TuiGrid Add → Rows=1", grid.Rows == 1);
        Check("TuiGrid Add → Columns=1", grid.Columns == 1);

        grid.Add(new TuiButton("Btn"), row: 1, col: 1);
        Check("TuiGrid Add (1,1) → Rows=2", grid.Rows == 2);
        Check("TuiGrid Add (1,1) → Columns=2", grid.Columns == 2);

        // Span
        grid.Add(new TuiLabel("Span"), row: 2, col: 0, colSpan: 2);
        Check("TuiGrid Span colSpan=2 → Columns=2", grid.Columns == 2);

        // SetRowDef/SetColDef
        var grid2 = new TuiGrid { Width = 60, Height = 20 };
        grid2.SetRowDef(0, "8");
        grid2.SetColDef(0, "30*");
        grid2.Add(new TuiLabel("A"), row: 0, col: 0);
        grid2.Layout();
        Check("TuiGrid SetRowDef+Layout Width>0", grid2.Width > 0);
        Check("TuiGrid SetRowDef+Layout Height>0", grid2.Height > 0);

        // ColGap
        var grid3 = new TuiGrid { ColGap = 2, RowGap = 1 };
        Check("TuiGrid ColGap=2", grid3.ColGap == 2);
        Check("TuiGrid RowGap=1", grid3.RowGap == 1);
        Console.WriteLine();

        // ================================================================
        // TuiWrapPanel 测试
        // ================================================================
        Section("[TuiWrapPanel]");
        var wrap = new TuiWrapPanel { Width = 30, Height = 10 };
        Check("TuiWrapPanel 创建", wrap != null);
        Check("TuiWrapPanel Direction=Horizontal", wrap!.Direction == Orientation.Horizontal);

        wrap.Add(new TuiLabel("A") { Width = 8 });
        wrap.Add(new TuiLabel("B") { Width = 8 });
        wrap.Add(new TuiLabel("C") { Width = 8 });
        wrap.Add(new TuiLabel("D") { Width = 8 });
        wrap.Add(new TuiLabel("E") { Width = 8 });
        Check("TuiWrapPanel Add x5", wrap.Children.Count == 5);

        wrap.Layout();
        Check("TuiWrapPanel Layout 后 Height>0", wrap.Height > 0);

        // 垂直模式
        var wrapV = new TuiWrapPanel { Direction = Orientation.Vertical, Width = 20, Height = 8 };
        wrapV.Add(new TuiLabel("V1") { Height = 3 });
        wrapV.Add(new TuiLabel("V2") { Height = 3 });
        wrapV.Layout();
        Check("TuiWrapPanel Vertical 模式", wrapV.Direction == Orientation.Vertical);

        // ItemWidth/Height
        var wrapUni = new TuiWrapPanel { ItemWidth = 10, ItemHeight = 2, ColumnSpacing = 2, RowSpacing = 1 };
        Check("TuiWrapPanel ItemWidth=10", wrapUni.ItemWidth == 10);
        Check("TuiWrapPanel ItemHeight=2", wrapUni.ItemHeight == 2);
        Console.WriteLine();

        // ================================================================
        // TuiSidePanel 测试
        // ================================================================
        Section("[TuiSidePanel]");
        var sidePanel = new TuiSidePanel();
        Check("TuiSidePanel 创建", sidePanel != null);
        Check("TuiSidePanel CanFocus=false", !sidePanel!.CanFocus);
        Check("TuiSidePanel PanelVisible=true", sidePanel.PanelVisible);
        Check("TuiSidePanel Width=30", sidePanel.Width == 30);
        Check("TuiSidePanel Height=20", sidePanel.Height == 20);

        sidePanel.Sections.Add(new PanelSection { Title = "📋 Todo", Lines = ["任务1", "任务2"] });
        Check("TuiSidePanel Sections.Add", sidePanel.Sections.Count == 1);
        Check("TuiSidePanel Section Title", sidePanel.Sections[0].Title == "📋 Todo");
        Check("TuiSidePanel Section Lines=2", sidePanel.Sections[0].Lines.Count == 2);

        // Collapsed
        var sec = new PanelSection { Title = "折叠", Collapsed = true };
        Check("PanelSection Collapsed=true", sec.Collapsed);

        // 可视性
        sidePanel.PanelVisible = false;
        Check("TuiSidePanel PanelVisible=false", !sidePanel.PanelVisible);

        // ── 高度分配（「位置满了往下扩张，扩不动为止」）──
        static PanelSection Sec(int n, bool collapsed = false) =>
            new() { Title = "T", Lines = [.. Enumerable.Range(0, n).Select(i => "l" + i)], Collapsed = collapsed };

        // 够放：每个分区全量，多出来的高度不动
        var fit = TuiSidePanel.AllocateHeights([Sec(2), Sec(3)], 20);
        Check("侧栏分配: 高度够则全量", fit is [2, 3]);
        // 折叠分区不参与分配
        var withCollapsed = TuiSidePanel.AllocateHeights([Sec(2), Sec(3, collapsed: true)], 20);
        Check("侧栏分配: 折叠分区不占位", withCollapsed is [2]);
        // 不够放：每区先留「3 行开销（上间隔+标题+下间隔）+ ≥1 行内容」，余量按需分
        var tight = TuiSidePanel.AllocateHeights([Sec(10), Sec(10)], 10);
        Check("侧栏分配: 不够则均分", tight is [2, 2]);
        Check("侧栏分配: 不超总高", tight.Sum(q => q + 3) <= 10);
        // 要得少的先拿满，省下的轮给还差的 —— 不浪费行
        var uneven = TuiSidePanel.AllocateHeights([Sec(1), Sec(10)], 10);
        Check("侧栏分配: 少的拿满多的兜底", uneven is [1, 3]);
        // 高度耗尽：装不下的分区标 -1（整块不画），而不是画半个标题
        var starved = TuiSidePanel.AllocateHeights([Sec(3), Sec(3), Sec(3)], 6);
        Check("侧栏分配: 放不下的分区标 -1", starved is [3, -1, -1]);
        Check("侧栏分配: 零高度返回全 0", TuiSidePanel.AllocateHeights([Sec(3)], 0) is [0]);
        Check("侧栏分配: 空列表不崩", TuiSidePanel.AllocateHeights([], 10).Count == 0);
        Console.WriteLine();

        // ================================================================
        // TuiPromptBar 测试
        // ================================================================
        Section("[TuiPromptBar]");
        var promptBar = new TuiPromptBar();
        Check("TuiPromptBar 创建", promptBar != null);
        Check("TuiPromptBar CanFocus=true", promptBar!.CanFocus);
        Check("TuiPromptBar Items=0", promptBar.Items.Count == 0);
        Check("TuiPromptBar SelectedIndex=-1", promptBar.SelectedIndex == -1);
        Check("TuiPromptBar MaxVisible=8", promptBar.MaxVisible == 8);

        // PromptItem
        var pi = new PromptItem { Kind = EPromptKind.File, Label = "test.cs", Detail = "D:\\code\\test.cs" };
        Check("PromptItem Label", pi.Label == "test.cs");
        Check("PromptItem Detail", pi.Detail == "D:\\code\\test.cs");
        Check("PromptItem Icon 非空", !string.IsNullOrEmpty(pi.Icon));

        // 各类型图标
        Check("PromptKind.Command Icon", new PromptItem { Kind = EPromptKind.Command }.Icon == "⌘");
        Check("PromptKind.File Icon", new PromptItem { Kind = EPromptKind.File }.Icon == "📄");
        Check("PromptKind.Shell Icon", new PromptItem { Kind = EPromptKind.Shell }.Icon == "⚡");
        Check("PromptKind.Slash Icon", new PromptItem { Kind = EPromptKind.Slash }.Icon == "/");
        Check("PromptKind.History Icon", new PromptItem { Kind = EPromptKind.History }.Icon == "↺");
        Check("PromptKind.Recent Icon", new PromptItem { Kind = EPromptKind.Recent }.Icon == "⏱️");

        // 填充项目
        promptBar.Items.Add(new PromptItem { Kind = EPromptKind.File, Label = "a.cs" });
        promptBar.Items.Add(new PromptItem { Kind = EPromptKind.Command, Label = "build" });
        promptBar.SelectedIndex = 0;
        // 键盘导航
        promptBar.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        Check("TuiPromptBar DownArrow", promptBar.SelectedIndex == 1);
        promptBar.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false));
        Check("TuiPromptBar UpArrow", promptBar.SelectedIndex == 0);
        promptBar.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, false));
        Check("TuiPromptBar End", promptBar.SelectedIndex == 1);
        promptBar.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false));
        Check("TuiPromptBar Home", promptBar.SelectedIndex == 0);

        // OnSelect
        PromptItem? selectedItem = null;
        promptBar.OnSelect = p => selectedItem = p;
        promptBar.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
        Check("TuiPromptBar Enter 触发 OnSelect", selectedItem?.Label == "a.cs");
        Console.WriteLine();

        // ================================================================
        // TuiDialog 工厂方法测试
        // ================================================================
        Section("[TuiDialog]");
        var dInfo = TuiDialog.Info("提示", "这是一条信息");
        Check("TuiDialog.Info 返回窗口", dInfo != null);
        Check("TuiDialog.Info 标题=提示", dInfo!.Title == "提示");
        Check("TuiDialog.Info 模态", dInfo.Modal);

        var dSuccess = TuiDialog.Success("成功", "操作已完成");
        Check("TuiDialog.Success 返回窗口", dSuccess != null);
        Check("TuiDialog.Success 标题=成功", dSuccess!.Title == "成功");

        var dWarn = TuiDialog.Warn("警告", "请注意");
        Check("TuiDialog.Warn 返回窗口", dWarn != null);
        Check("TuiDialog.Warn 标题=警告", dWarn!.Title == "警告");

        var dError = TuiDialog.Error("错误", "发生错误");
        Check("TuiDialog.Error 返回窗口", dError != null);
        Check("TuiDialog.Error 标题=错误", dError!.Title == "错误");

        bool? confirmResult = null;
        var dConfirm = TuiDialog.Confirm("确认", "是否继续？", r => confirmResult = r);
        Check("TuiDialog.Confirm 返回窗口", dConfirm != null);
        Check("TuiDialog.Confirm 模态", dConfirm!.Modal);

        TuiDialog.EDialogResult? confirm3Result = null;
        var dConfirm3 = TuiDialog.Confirm3("选择", "Yes/No/Cancel?", r => confirm3Result = r);
        Check("TuiDialog.Confirm3 返回窗口", dConfirm3 != null);

        string? inputResult = null;
        var dInput = TuiDialog.Input("输入", "名称", "默认值", s => inputResult = s);
        Check("TuiDialog.Input 返回窗口", dInput != null);

        int? selectResult = null;
        var dSelect = TuiDialog.Select("选择", ["A", "B", "C"], i => selectResult = i);
        Check("TuiDialog.Select 返回窗口", dSelect != null);

        HashSet<int>? multiResults = null;
        var dMulti = TuiDialog.MultiSelect("多选", ["X", "Y", "Z"], l => multiResults = l);
        Check("TuiDialog.MultiSelect 返回窗口", dMulti != null);

        TuiDialog.EDialogResult? permResult = null;
        var dPerm = TuiDialog.Permission("权限", "允许执行？", r => permResult = r);
        Check("TuiDialog.Permission 返回窗口", dPerm != null);
        Check("TuiDialog.Permission 模态", dPerm!.Modal);

        string? secretResult = null;
        var dSecret = TuiDialog.Secret("密钥", "输入API Key", "", s => secretResult = s);
        Check("TuiDialog.Secret 返回窗口", dSecret != null);
        Check("TuiDialog.Secret 模态", dSecret!.Modal);

        // DialogResult 枚举
        Check("DialogResult.Ok", (int)TuiDialog.EDialogResult.Ok == 0);
        Check("DialogResult.Yes", (int)TuiDialog.EDialogResult.Yes == 1);
        Check("DialogResult.No", (int)TuiDialog.EDialogResult.No == 2);
        Check("DialogResult.Cancel", (int)TuiDialog.EDialogResult.Cancel == 3);
        Check("DialogResult.Closed", (int)TuiDialog.EDialogResult.Closed == 4);
        Console.WriteLine();

        // ================================================================
        // TuiDialog 布局与渲染（标题栏粗体 / 3/4 屏宽高约束）
        // ================================================================
    }
}