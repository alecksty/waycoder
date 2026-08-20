using System.Text;
using System.Text.Json;
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
            (WorkMode.Review, "审查"), (WorkMode.Auto, "自动"),
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
        Check("PromptKind.Recent Icon", new PromptItem { Kind = EPromptKind.Recent }.Icon == "⏱");

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
                    Check($"{name}: 模态窗口已截取背景快照", win.BackgroundSnapshot != null);
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
                // 固定占 7 行：搜索 + 空行 + 槽位条 + 两行按钮 + 两行提示
                Check($"模型选择器 h={h}: 表格按 flex 撑开", mpTable.Height == mpWin.ContentHeight - 7);
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
            foreach (var id in (string[])["btnMode", "btnAllSlots", "btnScan", "btnImport", "btnOnline",
                                          "btnSetKey", "btnClrKey", "btnAdd", "btnEdit", "btnDel"])
            {
                var b = mpRes2.Find<TuiButton>(id);
                Check($"模型选择器: 按钮「{id}」存在且可获得焦点", b != null && b.CanFocus);
            }
            // 按钮不带字母快捷键：带了就会在搜索框打字时被窗口 OnKey 抢走（它按 KeyChar 匹配大写键）
            foreach (var id in (string[])["btnScan", "btnImport", "btnAdd", "btnDel"])
                Check($"模型选择器: 按钮「{id}」不注册字母快捷键",
                    mpRes2.Find<TuiButton>(id)!.ShortcutKey == null);

            // 两行按钮在最窄窗口（minWidth=62 → 内容区 60）也不能溢出，否则末尾按钮被裁掉
            int minContentW = mpRes2.Window!.MinWidth - 2;
            foreach (var (row, ids) in ((string, string[])[])[
                ("第一行", ["btnMode", "btnAllSlots", "btnScan", "btnImport", "btnOnline"]),
                ("第二行", ["btnSetKey", "btnClrKey", "btnAdd", "btnEdit", "btnDel"])])
            {
                int rowW = ids.Length - 1; // HBox spacing="1"
                foreach (var id in ids) rowW += mpRes2.Find<TuiButton>(id)!.Width;
                Check($"模型选择器: 按钮{row}窄屏不溢出({rowW}≤{minContentW})", rowW <= minContentW);
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

            // 快捷键能按：A 进全接受模式 → Y 确认 → 回调收到 AcceptAll
            dscr.OnKey(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
            dscr.OnKey(new ConsoleKeyInfo('y', ConsoleKey.Y, false, false, false));
            Check("Diff 窗口: A→Y 全接受生效", dd == WayCoder.UI.Tui.DiffPreview.Decision.AcceptAll);

            // 重置后按 Q 取消 → 无接受项时 RejectAll
            dd = WayCoder.UI.Tui.DiffPreview.Decision.RejectAll;
            dscr.OnKey(new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false));
            Check("Diff 窗口: Q 取消生效", dd == WayCoder.UI.Tui.DiffPreview.Decision.RejectAll);

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
        Section("[TuiDialog 自适应尺寸]");
        var autoShort = TuiDialog.Info("提示", "OK");
        var autoLong = TuiDialog.Info("提示", "这是一条很长很长的消息内容用于测试自适应宽度");
        var auto4 = TuiDialog.Info("提示", "一\n二\n三\n四");
        var auto6 = TuiDialog.Info("提示", "1\n2\n3\n4\n5\n6");
        autoShort.OnResize(cols, rows);
        autoLong.OnResize(cols, rows);
        auto4.OnResize(cols, rows);
        auto6.OnResize(cols, rows);
        Check("消息框禁用 XScale 自动算宽", autoShort.XScale == 0);
        Check("消息框宽随内容增长", autoLong.Width > autoShort.Width);
        Check("消息框高随行数增长", auto4.Height > autoShort.Height);
        // 内容高 = 消息行 + spacing(1) + 按钮行；此前少算 spacing 那一行，按钮被挤出内容区
        Check("消息框 4 行+空行+按钮全容纳(内容高=6)", auto4.ContentHeight == 6);
        Check("消息框 6 行不裁按钮(内容高=8)", auto6.ContentHeight == 8);

        var autoConfirm = TuiDialog.Confirm("确认", "一\n二\n三\n四", _ => { });
        var autoConfirm3 = TuiDialog.Confirm3("选择", "一\n二\n三", _ => { });
        autoConfirm.OnResize(cols, rows);
        autoConfirm3.OnResize(cols, rows);
        Check("确认框禁用 XScale", autoConfirm.XScale == 0);
        Check("确认框 4 行+空行+按钮全容纳(内容高=6)", autoConfirm.ContentHeight == 6);
        Check("确认框3 3 行+空行+按钮全容纳(内容高=5)", autoConfirm3.ContentHeight == 5);

        var showWin = TuiDialog.Info("提示", "抓屏测试");
        var showFrame = TuiDialog.Show(showWin, x: 2, y: 1);
        Check("Show 返回非空帧", showFrame.Length > 0);
        Check("Show 帧含消息文本", showFrame.Contains("抓屏测试"));
        Console.WriteLine();

        // ================================================================
        // TUI 声明式标记：TuiMarkup 加载 + Find(id) + 事件接线
        // ================================================================
        Section("[TuiMarkup]");
        var tuiRes = WayCoder.UI.TUI.TuiMarkup.Load(
            "<Window title=\"t\" width=\"30\" height=\"8\">" +
            "<VBox><Label id=\"msg\" text=\"初始\"/>" +
            "<Button id=\"ok\" text=\"确定\"/></VBox></Window>");
        var tuiMsg = tuiRes.Find<TuiLabel>("msg");
        var tuiOk = tuiRes.Find<TuiButton>("ok");
        Check("TuiMarkup Find 标签", tuiMsg != null && tuiMsg.Text == "初始");
        Check("TuiMarkup Find 按钮", tuiOk != null && tuiOk.Text == "确定");
        Check("TuiMarkup 窗口存在", tuiRes.Window != null);
        // 根元素为 Screen/Dialog 时构建对应对象
        var scrRes = WayCoder.UI.TUI.TuiMarkup.Load(
            "<Screen><VBox><Label text=\"屏\"/></VBox><Dialog id=\"d\" title=\"框\" width=\"30\" height=\"6\"><Label text=\"弹\"/></Dialog></Screen>");
        Check("TuiMarkup Screen 根", scrRes.Screen != null);
        Check("TuiMarkup Screen RootView", scrRes.Screen!.RootView != null);
        bool tuiClicked = false;
        tuiOk!.OnClick = _ => { tuiClicked = true; tuiMsg!.Text = "已点击"; };
        tuiOk.OnClick?.Invoke(tuiOk);
        Check("TuiMarkup 事件接线", tuiClicked && tuiMsg!.Text == "已点击");
        // 快捷键
        var scRes = WayCoder.UI.TUI.TuiMarkup.Load(
            "<Window shortcut=\"escape\"><VBox><Button id=\"b\" text=\"确定 (Y)\" shortcut=\"y\"/></VBox></Window>");
        var scBtn = scRes.Find<TuiButton>("b");
        Check("TuiMarkup 按钮快捷键", scBtn != null && scBtn.ShortcutKey == ConsoleKey.Y);
        Check("TuiMarkup 按钮下划线", scBtn != null && scBtn.UnderlineIndex >= 0);
        Check("TuiMarkup 窗口快捷键", scRes.Window!.KeyShortcuts.ContainsKey(ConsoleKey.Escape));
        // 占位符替换（单元格数据绑定）
        var cellRes = WayCoder.UI.TUI.TuiMarkup.Load(
            "<VBox><Label id=\"l\" text=\"{name}\" fg=\"{color}\"/></VBox>",
            new Dictionary<string, string> { ["name"] = "张三", ["color"] = "green" });
        Check("TuiMarkup 占位符替换文本", cellRes.Find<TuiLabel>("l")!.Text == "张三");
        Check("TuiMarkup 占位符替换颜色", cellRes.Find<TuiLabel>("l")!.Fg == AnsiColors.Green);
        // TableList：声明式列/行/cell + 每列占位符渲染（叶子根模板也支持）
        var tblRes = WayCoder.UI.TUI.TuiMarkup.Load(
            "<VBox><TableList id=\"t\" columns=\"模型:16,供应商:10\" " +
            "items=\"deepseek-v4-pro,深度求索|gpt-5.4-mini,OpenAI\" " +
            "cell=\"&lt;Label text='{value}' fg='cyan'/&gt;\" selected=\"1\"/></VBox>");
        var tblList = tblRes.Find<TuiTableList>("t");
        Check("TuiMarkup TableList 列", tblList != null && tblList.ColumnCount == 2);
        Check("TuiMarkup TableList 行", tblList != null && tblList.RowCount == 2);
        Check("TuiMarkup TableList 选中", tblList != null && tblList.SelectedIndex == 1);
        Check("TuiMarkup TableList cell", tblList != null && tblList.CellMarkup.Contains("value"));
        if (tblList != null)
        {
            tblList.Width = 28;
            tblList.Height = 4;
            var tblSb = new StringBuilder();
            tblList.Render(tblSb, 0, 0, 0, 0, 28, 4);
            string tblOut = tblSb.ToString();
            Check("TuiMarkup TableList 渲染第0列", tblOut.Contains("deepseek-v4-pro"));
            Check("TuiMarkup TableList 渲染第1列", tblOut.Contains("深度求索"));
            Check("TuiMarkup TableList 渲染第2行", tblOut.Contains("OpenAI"));
            // cell 模板 fg='cyan' 生效 → 渲染输出含青色 ANSI 前景码（36，可独立 36m 或与行背景合并 36;40m/36;46m）
            Check("TuiMarkup TableList cell 颜色",
                tblOut.Contains("\x1b[36m") || tblOut.Contains("\x1b[36;"));
        }
        // TreeView：声明式 items（路径语法）→ 建树 + cell 模板
        var tvRes = WayCoder.UI.TUI.TuiMarkup.Load(
            "<VBox><TreeView id=\"tv\" items=\"文档>概览,文档>入门\" " +
            "cell=\"&lt;Label text='{text}' fg='yellow'/&gt;\"/></VBox>");
        var tree = tvRes.Find<TuiTreeView>("tv");
        Check("TuiMarkup TreeView 根节点", tree != null && tree.RootNodes.Count == 1);
        Check("TuiMarkup TreeView 子节点", tree != null && tree.RootNodes.Count == 1 && tree.RootNodes[0].Children.Count == 2);
        Check("TuiMarkup TreeView 展开", tree != null && tree.RootNodes.Count == 1 && tree.RootNodes[0].IsExpanded);
        Check("TuiMarkup TreeView cell", tree != null && tree.CellMarkup.Contains("text"));
        if (tree != null)
        {
            tree.Width = 16;
            tree.Height = 3;
            var tvSb = new StringBuilder();
            tree.Render(tvSb, 0, 0, 0, 0, 16, 3);
            Check("TuiMarkup TreeView 渲染", tvSb.ToString().Contains("概览"));
        }
        Console.WriteLine();

        // ================================================================
        // InlinePermission 行内权限确认（inline 方式）
        // ================================================================
        Section("[InlinePermission]");
        var ip = new InlinePermission
        {
            ToolName = "bash",
            ArgsSummary = "rm -rf /tmp/cache/*",
            ArgsDetail = "command: rm -rf /tmp/cache/*",
            IsDangerous = true,
            Width = 50,
        };
        Check("InlinePermission 初始未决", !ip.IsResolved);
        Check("InlinePermission CanFocus=true", ip.CanFocus);
        Check("InlinePermission RenderHeight=3", ip.RenderHeight == 3);

        var ipSb = new StringBuilder();
        ip.Render(ipSb, 0, 0);
        var ipFrame = AnsiString.Strip(ipSb.ToString());
        Check("InlinePermission 渲染非空", ipFrame.Length > 0);
        Check("InlinePermission 含工具名", ipFrame.Contains("bash"));
        Check("InlinePermission 含 Y/N 提示", ipFrame.Contains("[Y]") && ipFrame.Contains("[N]"));

        int ipResolved = -1;
        ip.OnResolved = r => ipResolved = r;
        bool ipA = ip.OnKey(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
        Check("InlinePermission 危险操作忽略 A", !ipA && ip.Result == -1 && !ip.IsResolved);

        bool ipD = ip.OnKey(new ConsoleKeyInfo('d', ConsoleKey.D, false, false, false));
        Check("InlinePermission D 展开详情", ipD && ip.Expanded);
        Check("InlinePermission 展开后高度=4", ip.RenderHeight == 4);

        bool ipN = ip.OnKey(new ConsoleKeyInfo('n', ConsoleKey.N, false, false, false));
        Check("InlinePermission N 拒绝", ipN && ip.Result == 2 && ip.IsResolved);
        Check("InlinePermission 拒绝回调=2", ipResolved == 2);
        Check("InlinePermission 已决后 CanFocus=false", !ip.CanFocus);

        bool ipAgain = ip.OnKey(new ConsoleKeyInfo('y', ConsoleKey.Y, false, false, false));
        Check("InlinePermission 已决后不再响应", !ipAgain && ip.Result == 2);

        var ip2 = new InlinePermission { ToolName = "read_file", IsDangerous = false, Width = 50 };
        bool ipA2 = ip2.OnKey(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
        Check("InlinePermission 非危险 A=全部允许", ipA2 && ip2.Result == 1);

        var ip3 = new InlinePermission { ToolName = "write_file", IsDangerous = true, Width = 50 };
        bool ipY = ip3.OnKey(new ConsoleKeyInfo('y', ConsoleKey.Y, false, false, false));
        Check("InlinePermission Y=允许", ipY && ip3.Result == 0);
        Console.WriteLine();

        // ================================================================
        // TuiControl 基类测试
        // ================================================================
        Section("[TuiControl]");
        var ctrl = new TuiLabel("test"); // TuiLabel extends TuiControl
        Check("TuiControl Visible=true", ctrl.Visible);
        Check("TuiControl IsEnabled=true", ctrl.IsEnabled);
        Check("TuiControl Focused=false", !ctrl.Focused);
        Check("TuiControl Parent=null", ctrl.Parent == null);

        ctrl.Focused = true;
        Check("TuiControl Focused=true", ctrl.Focused);

        // Margin
        var ctrl2 = new TuiLabel("m") { Margin = new EdgeInsets(1, 2, 3, 4) };
        Check("TuiControl Margin.Top=1", ctrl2.Margin.Top == 1);
        Check("TuiControl Margin.Right=2", ctrl2.Margin.Right == 2);
        Check("TuiControl Margin.Bottom=3", ctrl2.Margin.Bottom == 3);
        Check("TuiControl Margin.Left=4", ctrl2.Margin.Left == 4);
        Check("TuiControl Margin.Horizontal=6", ctrl2.Margin.Horizontal == 6);
        Check("TuiControl Margin.Vertical=4", ctrl2.Margin.Vertical == 4);

        // Padding
        var ctrl3 = new TuiLabel("p") { Padding = new EdgeInsets(2) };
        Check("TuiControl Padding all=2", ctrl3.Padding.Top == 2 && ctrl3.Padding.Left == 2);

        // EdgeInsets 构造
        var edge1 = new EdgeInsets(5);
        Check("EdgeInsets(5) all=5", edge1.Top == 5 && edge1.Right == 5 && edge1.Bottom == 5 && edge1.Left == 5);

        var edge2 = new EdgeInsets(1, 2, 3, 4);
        Check("EdgeInsets(1,2,3,4)", edge2.Top == 1 && edge2.Right == 2 && edge2.Bottom == 3 && edge2.Left == 4);

        // TextAlign
        Check("TuiControl TextAlign=Left", ctrl.TextAlign == EHAlign.Left);

        // IsDirty (default is true)
        Check("TuiControl IsDirty 默认 true", ctrl.IsDirty);
        ctrl.ClearDirty();
        Check("TuiControl ClearDirty 后 false", !ctrl.IsDirty);
        ctrl.MarkDirty();
        Check("TuiControl MarkDirty 后 IsDirty=true", ctrl.IsDirty);
        Console.WriteLine();

        // ================================================================
        // TuiView 基类测试
        // ================================================================
        Section("[TuiView]");
        // TuiVBox (HBox inherits from TuiView)
        var vbox = new TuiVBox();
        Check("TuiVBox 创建", vbox != null);
        Check("TuiVBox Children=0", vbox!.Children.Count == 0);

        var vChild1 = new TuiLabel("C1");
        vbox.Add(vChild1);
        Check("TuiVBox Add → Children=1", vbox.Children.Count == 1);
        Check("TuiVBox Add 设置 Parent", vChild1.Parent == vbox);

        var vChild2 = new TuiLabel("C2");
        vbox.Add(vChild2);
        Check("TuiVBox Add x2", vbox.Children.Count == 2);

        // Layout
        vbox.Layout();
        Check("TuiVBox Layout 后 Height", vbox.Height > 0);

        // Remove
        vbox.Remove(vChild1);
        Check("TuiVBox Remove → Children=1", vbox.Children.Count == 1);
        Check("TuiVBox Remove Parent=null", vChild1.Parent == null);

        // Clear
        vbox.Clear();
        Check("TuiVBox Clear → Children=0", vbox.Children.Count == 0);

        // HBox
        var hbox = new TuiHBox();
        hbox.Add(new TuiLabel("H1"));
        hbox.Add(new TuiLabel("H2"));
        hbox.Layout();
        Check("TuiHBox Layout Width", hbox.Width > 0);

        // ChildHAlign
        Check("TuiView ChildHAlign=Left", vbox.ChildHAlign == EHAlign.Left);

        // FocusNext/FocusPrev
        var vboxF = new TuiVBox();
        var f1 = new TuiButton("F1"); f1.Focused = true;
        var f2 = new TuiButton("F2");
        var f3 = new TuiButton("F3");
        vboxF.Add(f1); vboxF.Add(f2); vboxF.Add(f3);
        vboxF.FocusNext();
        Check("TuiView FocusNext → F2", f2.Focused && !f1.Focused);
        vboxF.FocusPrev();
        Check("TuiView FocusPrev → F1", f1.Focused);
        Console.WriteLine();

        // ---- Flex 弹性布局 ----
        Section("[Flex 布局]");
        // Flex 默认值 = 0
        Check("Flex: TuiLabel 默认0", new TuiLabel("x").Flex == 0);
        Check("Flex: TuiWindow 默认0", new TuiWindow().Flex == 0);
        Check("Flex: TuiButton 默认0", new TuiButton("B").Flex == 0);

        // HBox Flex: 2 个子控件均分剩余空间
        var hboxFlex = new TuiHBox { Width = 100 };
        var hf1 = new TuiLabel("A") { Width = 10, Flex = 1 };
        var hf2 = new TuiLabel("B") { Width = 10, Flex = 1 };
        hboxFlex.Add(hf1); hboxFlex.Add(hf2);
        hboxFlex.Layout();
        Check("HBox Flex 2×1: child0 Width=50", hf1.Width == 50);
        Check("HBox Flex 2×1: child1 Width=50", hf2.Width == 50);

        // HBox Flex: 混合 fixed + flex (Flex=1 + Flex=2 比例分配)
        var hboxMix = new TuiHBox { Width = 100 };
        var hmFixed = new TuiLabel("Fixed") { Width = 20, Flex = 0 };
        var hmFlex1 = new TuiLabel("F1") { Width = 10, Flex = 1 };
        var hmFlex2 = new TuiLabel("F2") { Width = 10, Flex = 2 };
        hboxMix.Add(hmFixed); hboxMix.Add(hmFlex1); hboxMix.Add(hmFlex2);
        hboxMix.Layout();
        Check("HBox Flex 混合: fixed保持20", hmFixed.Width == 20);
        Check("HBox Flex 混合: Flex=1 ≈26", hmFlex1.Width >= 25 && hmFlex1.Width <= 27);
        Check("HBox Flex 混合: Flex=2 ≈53", hmFlex2.Width >= 52 && hmFlex2.Width <= 54);

        // HBox 全部 Flex=0 → 后向兼容
        var hboxOld = new TuiHBox { Width = 50 };
        var ho1 = new TuiLabel("Old1") { Width = 15, Flex = 0 };
        var ho2 = new TuiLabel("Old2") { Width = 20, Flex = 0 };
        hboxOld.Add(ho1); hboxOld.Add(ho2);
        hboxOld.Layout();
        Check("HBox Flex=0 后向兼容: Width不变", ho1.Width == 15 && ho2.Width == 20);

        // VBox Flex: 混合 fixed + flex
        var vboxFlex = new TuiVBox { Height = 50 };
        var vfFixed = new TuiLabel("Fixed") { Height = 5, Flex = 0 };
        var vfFlex1 = new TuiLabel("F1") { Height = 5, Flex = 1 };
        var vfFlex3 = new TuiLabel("F3") { Height = 5, Flex = 3 };
        vboxFlex.Add(vfFixed); vboxFlex.Add(vfFlex1); vboxFlex.Add(vfFlex3);
        vboxFlex.Layout();
        Check("VBox Flex 混合: fixed保持5", vfFixed.Height == 5);
        Check("VBox Flex 混合: Flex=1 ≈11", vfFlex1.Height >= 10 && vfFlex1.Height <= 12);
        Check("VBox Flex 混合: Flex=3 ≈33", vfFlex3.Height >= 32 && vfFlex3.Height <= 34);

        // VBox 全部 Flex=0 → 后向兼容
        var vboxOld = new TuiVBox { Height = 30 };
        var vo1 = new TuiLabel("Old1") { Height = 5, Flex = 0 };
        var vo2 = new TuiLabel("Old2") { Height = 10, Flex = 0 };
        vboxOld.Add(vo1); vboxOld.Add(vo2);
        vboxOld.Layout();
        Check("VBox Flex=0 后向兼容: Height不变", vo1.Height == 5 && vo2.Height == 10);

        // HBox Flex: 有 Margin 的情况
        var hboxMargin = new TuiHBox { Width = 100 };
        var hm1 = new TuiLabel("M1") { Width = 10, Flex = 1, Margin = new EdgeInsets(0, 2, 0, 2) };
        var hm2 = new TuiLabel("M2") { Width = 10, Flex = 1 };
        hboxMargin.Add(hm1); hboxMargin.Add(hm2);
        hboxMargin.Layout();
        Check("HBox Flex+Margin: 分配正确", hm1.Width > 0 && hm2.Width > 0 && hm1.Width + hm2.Width + hm1.Margin.Horizontal + hm2.Margin.Horizontal <= 100);

        // VBox Flex: 有 Spacing 的情况
        var vboxSpacing = new TuiVBox { Height = 60, Spacing = 2 };
        var vs1 = new TuiLabel("S1") { Height = 5, Flex = 1 };
        var vs2 = new TuiLabel("S2") { Height = 5, Flex = 1 };
        vboxSpacing.Add(vs1); vboxSpacing.Add(vs2);
        vboxSpacing.Layout();
        Check("VBox Flex+Spacing: 分配正确", vs1.Height > 0 && vs2.Height > 0);
        Console.WriteLine();

        // ================================================================
        // TuiScreen 基类测试
        // ================================================================
        Section("[TuiScreen]");
        var chatScreen = new ChatScreen();
        Check("TuiScreen RootView 非空", chatScreen.RootView != null);
        Check("TuiScreen Windows=0", chatScreen.Windows.Count == 0);
        Check("TuiScreen HasModal=false", !chatScreen.HasModal);

        var dummyWin = new TuiWindow { Title = "测试", Modal = true };
        chatScreen.Windows.Add(dummyWin);
        Check("TuiScreen 添加窗口后 Windows=1", chatScreen.Windows.Count == 1);
        Check("TuiScreen HasModal=true", chatScreen.HasModal);

        // FocusedWindow
        chatScreen.FocusedWindow = dummyWin;
        Check("TuiScreen FocusedWindow", chatScreen.FocusedWindow == dummyWin);

        // TW/TH（需要 Activate 后才有效）
        chatScreen.Activate();
        Check("TuiScreen TW>0", chatScreen.TW > 0);
        Check("TuiScreen TH>0", chatScreen.TH > 0);
        Console.WriteLine();

        // ================================================================
        // BoxBuffer 测试
        // ================================================================
        Section("[BoxBuffer]");
        var box = new BoxBuffer { X = 2, Y = 3, Width = 40, Height = 10 };
        Check("BoxBuffer 创建", box != null);
        Check("BoxBuffer X=2", box!.X == 2);
        Check("BoxBuffer Y=3", box.Y == 3);
        Check("BoxBuffer Width=40", box.Width == 40);
        Check("BoxBuffer Height=10", box.Height == 10);

        // 边框样式枚举
        Check("BorderStyle.None=0", (int)BorderStyle.None == 0);
        Check("BorderStyle.Single=1", (int)BorderStyle.Single == 1);
        Check("BorderStyle.Double=2", (int)BorderStyle.Double == 2);
        Check("BorderStyle.Thick=3", (int)BorderStyle.Thick == 3);
        Check("BorderStyle.Solid=4", (int)BorderStyle.Solid == 4);
        Check("BorderStyle.Star=5", (int)BorderStyle.Star == 5);
        Check("BorderStyle.Circle=6", (int)BorderStyle.Circle == 6);
        Check("BorderStyle.Custom=7", (int)BorderStyle.Custom == 7);

        // 内容区计算
        box.Border = BorderStyle.Single;
        Check("BoxBuffer ContentLeft=X+1", box.ContentLeft == box.X + 1);
        Check("BoxBuffer ContentTop=Y+1", box.ContentTop == box.Y + 1);

        box.Border = BorderStyle.None;
        Check("BoxBuffer None ContentLeft=X", box.ContentLeft == box.X);

        // 自定义边框
        var boxC = new BoxBuffer { Border = BorderStyle.Custom, CustomTL = "+", CustomH = "-", CustomTR = "+" };
        Check("BoxBuffer CustomTL", boxC.CustomTL == "+");

        // FgColor/BgColor
        Check("BoxBuffer FgColor=37", box.FgColor == "37");
        Check("BoxBuffer BgColor 默认空", box.BgColor == "");
        Console.WriteLine();

        // ================================================================
        // AnsiColors 测试
        // ================================================================
        Section("[AnsiColors]");
        Check("AnsiColors.Black=30", AnsiColors.Black == 30);
        Check("AnsiColors.Red=31", AnsiColors.Red == 31);
        Check("AnsiColors.Green=32", AnsiColors.Green == 32);
        Check("AnsiColors.Yellow=33", AnsiColors.Yellow == 33);
        Check("AnsiColors.Blue=34", AnsiColors.Blue == 34);
        Check("AnsiColors.Magenta=35", AnsiColors.Magenta == 35);
        Check("AnsiColors.Cyan=36", AnsiColors.Cyan == 36);
        Check("AnsiColors.White=37", AnsiColors.White == 37);

        Check("AnsiColors.BgBlack=40", AnsiColors.BgBlack == 40);
        Check("AnsiColors.BgWhite=47", AnsiColors.BgWhite == 47);

        Check("AnsiColors.BrightBlack=90", AnsiColors.BrightBlack == 90);
        Check("AnsiColors.BrightWhite=97", AnsiColors.BrightWhite == 97);

        Check("AnsiColors.BgBrightBlack=100", AnsiColors.BgBrightBlack == 100);
        Check("AnsiColors.BgBrightWhite=107", AnsiColors.BgBrightWhite == 107);

        Check("AnsiColors.Orange=208", AnsiColors.Orange == 208);
        Check("AnsiColors.Orange3=172", AnsiColors.Orange3 == 172);
        Check("AnsiColors.PanelGrey=247", AnsiColors.PanelGrey == 247);
        Console.WriteLine();

        // ================================================================
        // TuiTheme 测试
        // ================================================================
        Section("[TuiTheme]");
        var theme = TuiTheme.Current;
        Check("TuiTheme.Current 非空", theme != null);
        Check("TuiTheme.Default 非空", TuiTheme.Default != null);

        // 对话框边框色
        Check("TuiTheme DialogInfoBorder", theme!.DialogInfoBorder > 0);
        Check("TuiTheme DialogSuccessBorder", theme.DialogSuccessBorder > 0);
        Check("TuiTheme DialogWarnBorder", theme.DialogWarnBorder > 0);
        Check("TuiTheme DialogErrorBorder", theme.DialogErrorBorder > 0);

        // 窗口色
        Check("TuiTheme WindowBg", theme.WindowBg > 0);
        Check("TuiTheme MaskBg", theme.MaskBg > 0);

        // 渐变预设
        var (gs, ge) = theme.GradCyanBlue;
        Check("TuiTheme GradCyanBlue start", gs > 0);
        Check("TuiTheme GradCyanBlue end", ge > 0);

        var (gs2, ge2) = theme.GradTitleBar;
        Check("TuiTheme GradTitleBar start", gs2 > 0);
        Check("TuiTheme GradTitleBar end", ge2 > 0);

        // 控件颜色
        Check("TuiTheme ControlFg", theme.ControlFg >= 0);
        Check("TuiTheme ButtonFg", theme.ButtonFg >= 0);
        Check("TuiTheme InputFg", theme.InputFg >= 0);
        Check("TuiTheme InputBg=黑", theme.InputBg == AnsiColors.BgBlack);
        Check("TuiTheme InputCursorBg=黑", theme.InputCursorBg == AnsiColors.BgBlack);
        Check("TuiTheme ListBg=黑", theme.ListBg == AnsiColors.BgBlack);
        Check("TuiTheme WindowBg=灰", theme.WindowBg == AnsiColors.PanelGrey);

        // 主题预设索引
        Check("TuiTheme CurrentPresetIndex >= -1", TuiTheme.CurrentPresetIndex >= -1);

        // Apply 预设
        TuiTheme.Apply(TuiTheme.Dark, 0);
        Check("TuiTheme Apply(Dark)", TuiTheme.CurrentPresetIndex >= 0);

        // NormalizeKey / ApplyByName 名称归一化映射
        Check("NormalizeKey dark", TuiTheme.NormalizeKey("dark") == "dark");
        Check("NormalizeKey default→dark", TuiTheme.NormalizeKey("default") == "dark");
        Check("NormalizeKey hc", TuiTheme.NormalizeKey("hc") == "hc");
        Check("NormalizeKey highcontrast", TuiTheme.NormalizeKey("highcontrast") == "hc");
        Check("NormalizeKey 中文标签 海洋 Ocean", TuiTheme.NormalizeKey("海洋 Ocean") == "ocean");
        Check("NormalizeKey 中文标签 单色 Mono", TuiTheme.NormalizeKey("单色 Mono") == "mono");
        Check("NormalizeKey 中文标签 高对比度 HC", TuiTheme.NormalizeKey("高对比度 HC") == "hc");
        Check("NormalizeKey 黄金甲", TuiTheme.NormalizeKey("黄金甲") == "dark");
        Check("NormalizeKey 未知名 null", TuiTheme.NormalizeKey("cyberpunk") == null);
        Check("ApplyByName ocean", TuiTheme.ApplyByName("ocean") && TuiTheme.CurrentPresetIndex == 3);
        Check("ApplyByName 中文标签 森林 Forest", TuiTheme.ApplyByName("森林 Forest") && TuiTheme.CurrentPresetIndex == 4);

        // 恢复默认
        TuiTheme.Apply(TuiTheme.Dark, 0);
        Console.WriteLine();

        // ================================================================
        // MarkdownRenderer 测试
        // ================================================================
        Section("[MarkdownRenderer]");
        // 标题解析
        var hNodes = MarkdownParser.Parse("# 标题1\n## 标题2\n### 标题3\n#### 标题4");
        Check("MarkdownParser 4个标题", hNodes.Count == 4);
        Check("MdHeading Level=1", hNodes[0] is MdHeading h1 && h1.Level == 1 && h1.Text == "标题1");
        Check("MdHeading Level=2", hNodes[1] is MdHeading h2 && h2.Level == 2 && h2.Text == "标题2");
        Check("MdHeading Level=3", hNodes[2] is MdHeading h3 && h3.Level == 3 && h3.Text == "标题3");
        Check("MdHeading Level=4", hNodes[3] is MdHeading h4 && h4.Level == 4 && h4.Text == "标题4");

        // 段落
        var pNodes = MarkdownParser.Parse("这是一段普通文本。");
        Check("MarkdownParser 段落", pNodes.Count == 1 && pNodes[0] is MdParagraph p && p.Text == "这是一段普通文本。");

        // 代码块
        var cNodes = MarkdownParser.Parse("```csharp\nConsole.WriteLine(\"Hello\");\n```");
        Check("MarkdownParser 代码块", cNodes.Count == 1 && cNodes[0] is MdCodeBlock cb && cb.Language == "csharp");
        Check("MdCodeBlock 内容", ((MdCodeBlock)cNodes[0]).Code.Contains("Console"));

        // 表格
        var tNodes = MarkdownParser.Parse("| A | B |\n|---|---|\n| 1 | 2 |");
        Check("MarkdownParser 表格", tNodes.Count == 1 && tNodes[0] is MdTable t && t.Headers.Count == 2);
        Check("MdTable Headers", ((MdTable)tNodes[0]).Headers[0] == "A");
        Check("MdTable 数据行", ((MdTable)tNodes[0]).Rows.Count == 1 && ((MdTable)tNodes[0]).Rows[0][0] == "1");

        // 表格：转义竖线 \| 不拆列
        var escNodes = MarkdownParser.Parse("| 名称 | 命令 |\n|---|---|\n| a\\|b | ls \\| grep |");
        Check("MdTable 转义竖线", escNodes.Count == 1 && escNodes[0] is MdTable et
            && et.Rows.Count == 1 && et.Rows[0][0] == "a|b" && et.Rows[0][1] == "ls | grep");

        // 表格：无分隔行（表头 + 数据）也可解析
        var noSepNodes = MarkdownParser.Parse("| A | B |\n| 1 | 2 |");
        Check("MdTable 无分隔行", noSepNodes.Count == 1 && noSepNodes[0] is MdTable nt
            && nt.Headers.Count == 2 && nt.Rows.Count == 1);

        // 单行竖线内容（非表格）→ 剥竖线按普通段落处理，不吞行
        var singlePipe = MarkdownParser.Parse("| 单行竖线内容 |");
        Check("MdTable 单行竖线不吞行", singlePipe.Count == 1 && singlePipe[0] is MdParagraph sp
            && sp.Text == "单行竖线内容");

        // 列表
        var lNodes = MarkdownParser.Parse("- 项目一\n- 项目二\n- 项目三");
        var listItems = lNodes.OfType<MdListItem>().ToList();
        Check("MarkdownParser 无序列表3项", listItems.Count == 3);
        Check("MdListItem Ordered=false", !listItems[0].Ordered);
        Check("MdListItem Text", listItems[0].Text == "项目一");

        // 有序列表
        var olNodes = MarkdownParser.Parse("1. 第一\n2. 第二\n3. 第三");
        var olItems = olNodes.OfType<MdListItem>().ToList();
        Check("MarkdownParser 有序列表3项", olItems.Count == 3);
        Check("MdListItem Ordered=true", olItems[0].Ordered);
        Check("MdListItem OrderNum", olItems[0].OrderNum == 1);

        // 分割线
        var hrNodes = MarkdownParser.Parse("---");
        Check("MarkdownParser 分割线", hrNodes.Count == 1 && hrNodes[0] is MdRule);

        // 内联格式 ParseInline
        var boldResult = MarkdownParser.ParseInline("这是 **加粗** 文本");
        Check("ParseInline 加粗标记=1", boldResult.Any(r => r.Color == 1));

        var italicResult = MarkdownParser.ParseInline("这是 *斜体* 文本");
        Check("ParseInline 斜体标记=3", italicResult.Any(r => r.Color == 3));

        var codeResult = MarkdownParser.ParseInline("使用 `var x = 1;` 代码");
        Check("ParseInline 代码标记=33", codeResult.Any(r => r.Color == 33));

        // 空输入
        var emptyResult = MarkdownParser.ParseInline("");
        Check("ParseInline 空字符串返回1项", emptyResult.Count == 1);

        // 空 Markdown
        var emptyParse = MarkdownParser.Parse("");
        Check("MarkdownParser 空输入返回0", emptyParse.Count == 0);

        var nullParse = MarkdownParser.Parse(null!);
        Check("MarkdownParser null 返回0", nullParse.Count == 0);

        // 缩进列表
        var indentNodes = MarkdownParser.Parse("  - 缩进一级\n    - 缩进二级");
        var indentItems = indentNodes.OfType<MdListItem>().ToList();
        Check("MarkdownParser 缩进列表", indentItems.Any(i => i.Level == 1));

        // 引用块
        var bqNodes = MarkdownParser.Parse("> 引用一行\n> 引用二行");
        Check("MarkdownParser 引用块", bqNodes.Count == 1 && bqNodes[0] is MdBlockQuote bq && bq.Text == "引用一行\n引用二行");

        // 任务清单
        var taskNodes = MarkdownParser.Parse("- [x] 已完成\n- [ ] 未完成");
        var taskItems = taskNodes.OfType<MdListItem>().ToList();
        Check("MarkdownParser 任务清单2项", taskItems.Count == 2);
        Check("MdListItem Checked=true", taskItems[0].Checked == true && taskItems[0].Text == "已完成");
        Check("MdListItem Checked=false", taskItems[1].Checked == false && taskItems[1].Text == "未完成");

        // 链接
        var linkResult = MarkdownParser.ParseInline("见 [文档](https://example.com) 详情");
        Check("ParseInline 链接文字色=36", linkResult.Any(r => r.Color == 36 && r.Text == "文档"));

        // 删除线
        var strikeResult = MarkdownParser.ParseInline("这是 ~~删除~~ 文本");
        Check("ParseInline 删除线标记=2", strikeResult.Any(r => r.Color == 2 && r.Text == "删除"));

        // Markup 标记 «tag»…«/»（LLM 推理内容用 «dim»…«/» 包裹，须转成真实样式而非字面输出）
        // 注：用 «/» 而非 \xAB/\xBB——C# 的 \x 会贪婪吞吃后续十六进制字符
        //（"\xABdim" 的 d 是十六进制，会解析成 ઽ+"im"，损坏 dim/bold/bright 等标签）
        var dimMk = MarkdownParser.ParseInline("«dim»淡化«/»正常");
        Check("ParseInline «dim» 淡化=2", dimMk.Any(r => r.Color == 2 && r.Text == "淡化"));
        Check("ParseInline «/» 复位回默认色", dimMk.Any(r => r.Text == "正常" && r.Color == 0));

        Check("ParseInline «bold» 粗体=1",
            MarkdownParser.ParseInline("«bold»加粗«/»").Any(r => r.Color == 1));
        Check("ParseInline «bright» 加亮=1",
            MarkdownParser.ParseInline("«bright»加亮«/»").Any(r => r.Color == 1));
        Check("ParseInline «italic» 斜体=3",
            MarkdownParser.ParseInline("«italic»斜体«/»").Any(r => r.Color == 3));
        Check("ParseInline «underline» 下划线=4",
            MarkdownParser.ParseInline("«underline»下划线«/»").Any(r => r.Color == 4));
        Check("ParseInline «strikethrough» 删除线=9",
            MarkdownParser.ParseInline("«strikethrough»删除«/»").Any(r => r.Color == 9));
        Check("ParseInline «red» 红=31",
            MarkdownParser.ParseInline("«red»红«/»").Any(r => r.Color == 31));
        Check("ParseInline «grey» 灰=90",
            MarkdownParser.ParseInline("«grey»灰«/»").Any(r => r.Color == 90));
        Check("ParseInline «bold yellow» 黄=33",
            MarkdownParser.ParseInline("«bold yellow»黄«/»").Any(r => r.Color == 33));
        Check("ParseInline «bright red» 亮红=91",
            MarkdownParser.ParseInline("«bright red»亮红«/»").Any(r => r.Color == 91));
        Check("ParseInline «orange» 橙=208",
            MarkdownParser.ParseInline("«orange»橙«/»").Any(r => r.Color == 208));
        Check("ParseInline «orange3» 深橙=172",
            MarkdownParser.ParseInline("«orange3»深橙«/»").Any(r => r.Color == 172));

        // 流式未闭合 span：无 «/» 时样式持续到行尾（推理流式逐 token 追加）
        Check("ParseInline 未闭合«dim»持续淡化",
            MarkdownParser.ParseInline("«dim»思考中").Any(r => r.Color == 2 && r.Text == "思考中"));

        // 未知标签按字面输出，不崩溃
        Check("ParseInline 未知标签按字面",
            MarkdownParser.ParseInline("«nope»文本").Any(r => r.Text.Contains("nope")));

        // CLI/一次性模式的解码器（TUI 走 MarkdownParser，CLI 走 SpectreToAnsi，两条路都得转）
        var cliDim = Program.SpectreToAnsi("«dim»思考中«/»");
        Check("SpectreToAnsi «dim» 转 ANSI", cliDim.Contains(AnsiTty.SgrDim) && cliDim.Contains(AnsiTty.SgrReset));
        Check("SpectreToAnsi 不残留书名号标记", !cliDim.Contains("«dim»") && !cliDim.Contains("«/»"));
        Check("SpectreToAnsi 保留正文", cliDim.Contains("思考中"));

        // 端到端：RenderMessage 单段落回退路径也要识别 markup（此前直接字面输出）
        var rm = WayCoder.UI.Tui.TuiMarkdown.RenderMessage("«dim»思考«/»回答", "assistant", 80);
        Check("RenderMessage markup 淡化", rm.Any(line => line.Any(seg => seg.Fg == 2)));

        // 嵌套标记：内层覆盖外层，«/» 逐层弹栈恢复（栈模型）
        var nestedMk = MarkdownParser.ParseInline("«bold»粗«red»红«/»粗«/»");
        Check("ParseInline 嵌套 bold→red→bold 弹栈",
            nestedMk.Count == 3
            && nestedMk[0].Color == 1 && nestedMk[0].Text == "粗"
            && nestedMk[1].Color == 31 && nestedMk[1].Text == "红"
            && nestedMk[2].Color == 1 && nestedMk[2].Text == "粗");

        // 块级跨行（含空行）：«dim»…«/» 包裹多行推理内容，样式贯穿且保留空行
        var blk = WayCoder.UI.Tui.TuiMarkdown.RenderMessage("«dim»第一行\n\n第二行«/»\n正常", "assistant", 80);
        Check("RenderMessage 块级跨行 dim 贯穿空行",
            blk.Any(l => l.Any(s => s.Fg == 2 && s.Text == "第一行"))
            && blk.Any(l => l.Any(s => s.Fg == 2 && s.Text == "第二行")));
        Check("RenderMessage 块级关闭后恢复正常",
            blk.Any(l => l.Any(s => s.Text == "正常" && s.Fg != 2)));

        // 块级流式未闭合：开标签后无 «/»，样式持续到内容末尾
        var blkOpen = WayCoder.UI.Tui.TuiMarkdown.RenderMessage("«dim»思考中\n还在想", "assistant", 80);
        Check("RenderMessage 块级未闭合持续淡化",
            blkOpen.Any(l => l.Any(s => s.Fg == 2 && s.Text == "思考中"))
            && blkOpen.Any(l => l.Any(s => s.Fg == 2 && s.Text == "还在想")));

        // RenderBuffer.Write 样式码复位：不能 SgrReset(0) 全复位冲掉底色（编辑器/对话框花屏根因）
        var rbStyle = new RenderBuffer();
        rbStyle.Write(0, 0, "粗", fg: 1, bg: 0);
        var styleAnsi = rbStyle.ToString();
        Check("RenderBuffer 样式码关闭用专门码(22)非全复位",
            styleAnsi.Contains(AnsiTty.Sgr(22)) && !styleAnsi.Contains(AnsiTty.SgrReset));

        var rbStyleBg = new RenderBuffer();
        rbStyleBg.Write(0, 0, "粗", fg: 1, bg: 44);
        var styleBgAnsi = rbStyleBg.ToString();
        Check("RenderBuffer 样式码+背景 关样式后仍保留底色复位",
            styleBgAnsi.Contains(AnsiTty.Sgr(22)) && styleBgAnsi.Contains(AnsiTty.SgrResetBg)
            && !styleBgAnsi.Contains(AnsiTty.SgrReset));

        // RenderBuffer 超宽首个字符（代理对 emoji 宽 2 > 可用列）完整写入不切半（v0.71.30 修复）
        var rbRuneWrap = new RenderBuffer();
        rbRuneWrap.WriteWrap(0, 0, "😀", maxCol: 0, indentCol: 0);
        Check("WriteWrap 超宽代理对不切半", !rbRuneWrap.ToString().Contains('�'));
        var rbRuneRegion = new RenderBuffer();
        rbRuneRegion.WriteRegion(0, 0, 1, 1, "😀");
        Check("WriteRegion 超宽代理对不切半", !rbRuneRegion.ToString().Contains('�'));
        Console.WriteLine();

        // ================================================================
        // TuiTable 测试
        // ================================================================
        Section("[TuiTable]");
        var table = new TuiTable();
        Check("TuiTable 创建", table != null);

        table!.AddColumn("名称", 12);
        table.AddColumn("类型", 8);
        table.AddColumn("大小", 8);
        // 链式调用
        var table2 = new TuiTable("测试表格")
            .AddColumn("A")
            .AddColumn("B")
            .AddRow("1", "2");
        Check("TuiTable 链式 AddRow", table2 != null);

        // RenderToString
        var output = table2!.RenderToString(false);
        Check("TuiTable RenderToString 非空", !string.IsNullOrEmpty(output));
        Check("TuiTable RenderToString 含标题", output.Contains("测试表格"));
        Check("TuiTable RenderToString 含表头", output.Contains("A") && output.Contains("B"));

        // ANSI 渲染
        var ansiOutput = table2.RenderToString(true);
        Check("TuiTable RenderToString ANSI 非空", !string.IsNullOrEmpty(ansiOutput));

        // 空表格渲染
        var tableEmpty = new TuiTable();
        Check("TuiTable 空表格 RenderToString=''", tableEmpty.RenderToString() == "");

        // AddMarkupRow
        var table3 = new TuiTable().AddColumn("标记");
        table3.AddMarkupRow("\x1b[32m绿色\x1b[0m");
        Check("TuiTable AddMarkupRow 非空渲染", !string.IsNullOrEmpty(table3.RenderToString()));
        Console.WriteLine();

        // ================================================================
        // FrameSnapshot 背景快照（颜色感知解析 + 贴回）
        // ================================================================
        Section("[FrameSnapshot]");

        // 构造 ANSI 帧：光标定位 + 颜色 + 文本 + 复位
        var snapFrame = new StringBuilder();
        snapFrame.Append("\x1b[2;3H").Append(AnsiTty.FgBgCode(31, 44)).Append("AB").Append("\x1b[39;49m"); // (2,3)=(31红/44蓝) AB
        snapFrame.Append("\x1b[3;5H").Append(AnsiTty.FgCode(36)).Append("OK").Append("\x1b[39m");          // (3,5)=青色 OK
        var snap = FrameSnapshot.Capture(snapFrame.ToString(), 2, 1, 6, 3); // 区域 (x=2,y=1) 宽6 高3
        Check("FrameSnapshot 非空", snap != null);
        Check("FrameSnapshot 区域坐标", snap!.X == 2 && snap.Y == 1 && snap.W == 6 && snap.H == 3);

        // 解析：字符与颜色。CUP 1-based → 0-based：绝对(2,3)→0-based(1,2)，区域(x=2,y=1) → 相对(0,0)；(3,5)→(1,3)
        Check("FrameSnapshot 解析字符 A", snap.CharAt(0, 0) == "A");
        Check("FrameSnapshot 解析颜色 31/44", snap.ColorAt(0, 0) == (31, 44));
        Check("FrameSnapshot 解析字符 B", snap.CharAt(0, 1) == "B");
        Check("FrameSnapshot 解析青色 O", snap.CharAt(1, 2) == "O" && snap.ColorAt(1, 2).fg == 36);
        Check("FrameSnapshot 解析青色 K", snap.CharAt(1, 3) == "K");
        // 区域外返回空串；区域内未写格子保持默认
        Check("FrameSnapshot 区域外空串", snap.CharAt(9, 9) == "");
        Check("FrameSnapshot 未写格子默认", snap.CharAt(2, 5) == " " && snap.ColorAt(2, 5) == (0, 0));

        // 贴回：输出含定位、文本与颜色
        var snapOut = new StringBuilder();
        snap.Blit(snapOut);
        var snapStr = snapOut.ToString();
        Check("FrameSnapshot 贴回含光标定位", snapStr.Contains("\x1b[2;3H"));
        Check("FrameSnapshot 贴回含文本", snapStr.Contains("AB") && snapStr.Contains("OK"));
        Check("FrameSnapshot 贴回含颜色", snapStr.Contains(AnsiTty.FgBgCode(31, 44)));

        // 无效区域返回 null
        Check("FrameSnapshot 无效区域 null", FrameSnapshot.Capture("\x1b[H", 0, 0, 0, 5) == null);
        Console.WriteLine();

        // ================================================================
        // DiffPreview 测试
        // ================================================================
    }
}
