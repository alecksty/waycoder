using System.Text;
using System.Text.Json;
using WayCoder.Tools;
using WayCoder.UI;
using WayCoder.Terminal;
using WayCoder.UI.TuiControls;
using WayCoder.UI.TuiScreens;

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
        var md = WayCoder.UI.TuiControls.TuiMarkdown.Create("滚动测试内容", "assistant", 60);
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
        var pi = new PromptItem { Kind = PromptKind.File, Label = "test.cs", Detail = "D:\\code\\test.cs" };
        Check("PromptItem Label", pi.Label == "test.cs");
        Check("PromptItem Detail", pi.Detail == "D:\\code\\test.cs");
        Check("PromptItem Icon 非空", !string.IsNullOrEmpty(pi.Icon));

        // 各类型图标
        Check("PromptKind.Command Icon", new PromptItem { Kind = PromptKind.Command }.Icon == "⌘");
        Check("PromptKind.File Icon", new PromptItem { Kind = PromptKind.File }.Icon == "📄");
        Check("PromptKind.Shell Icon", new PromptItem { Kind = PromptKind.Shell }.Icon == "⚡");
        Check("PromptKind.Slash Icon", new PromptItem { Kind = PromptKind.Slash }.Icon == "/");
        Check("PromptKind.History Icon", new PromptItem { Kind = PromptKind.History }.Icon == "↺");
        Check("PromptKind.Recent Icon", new PromptItem { Kind = PromptKind.Recent }.Icon == "⏱");

        // 填充项目
        promptBar.Items.Add(new PromptItem { Kind = PromptKind.File, Label = "a.cs" });
        promptBar.Items.Add(new PromptItem { Kind = PromptKind.Command, Label = "build" });
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

        TuiDialog.DialogResult? confirm3Result = null;
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

        TuiDialog.DialogResult? permResult = null;
        var dPerm = TuiDialog.Permission("权限", "允许执行？", r => permResult = r);
        Check("TuiDialog.Permission 返回窗口", dPerm != null);
        Check("TuiDialog.Permission 模态", dPerm!.Modal);

        string? secretResult = null;
        var dSecret = TuiDialog.Secret("密钥", "输入API Key", "", s => secretResult = s);
        Check("TuiDialog.Secret 返回窗口", dSecret != null);
        Check("TuiDialog.Secret 模态", dSecret!.Modal);

        // DialogResult 枚举
        Check("DialogResult.Ok", (int)TuiDialog.DialogResult.Ok == 0);
        Check("DialogResult.Yes", (int)TuiDialog.DialogResult.Yes == 1);
        Check("DialogResult.No", (int)TuiDialog.DialogResult.No == 2);
        Check("DialogResult.Cancel", (int)TuiDialog.DialogResult.Cancel == 3);
        Check("DialogResult.Closed", (int)TuiDialog.DialogResult.Closed == 4);
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
            Check($"{name}: 标题栏粗体={expectBar}", win.TitleBold == expectBar);
            Check($"{name}: 宽≤3/4屏", win.Width <= maxW + 1);
            Check($"{name}: 高≤3/4屏", win.Height <= maxH + 1);
            if (expectBar)
            {
                Check($"{name}: 标题独占一行(ContentTop=Y+2)", win.ContentTop == win.Y + 2);
                Check($"{name}: 内容高度扣除标题行", win.ContentHeight == win.Height - 3);
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
                // 用 ANSI 网格解释验证标题落在独立行（win.Y+1），而非嵌在上边框（win.Y）
                var dialogGrid = TuiAudit.AnsiToGrid(raw, rows, cols);
                bool topRowHasTitle = winY < dialogGrid.Count && dialogGrid[winY].Contains(title);
                bool titleRowHasTitle = winY + 1 < dialogGrid.Count && dialogGrid[winY + 1].Contains(title);
                Check($"{name}: 标题在独立行(非上边框)", !topRowHasTitle && titleRowHasTitle);
            }
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
        Check("TuiControl TextAlign=Left", ctrl.TextAlign == HAlign.Left);

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
        Check("TuiView ChildHAlign=Left", vbox.ChildHAlign == HAlign.Left);

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
        // TuiColors 测试
        // ================================================================
        Section("[TuiColors]");
        Check("TuiColors.Black=30", TuiColors.Black == 30);
        Check("TuiColors.Red=31", TuiColors.Red == 31);
        Check("TuiColors.Green=32", TuiColors.Green == 32);
        Check("TuiColors.Yellow=33", TuiColors.Yellow == 33);
        Check("TuiColors.Blue=34", TuiColors.Blue == 34);
        Check("TuiColors.Magenta=35", TuiColors.Magenta == 35);
        Check("TuiColors.Cyan=36", TuiColors.Cyan == 36);
        Check("TuiColors.White=37", TuiColors.White == 37);

        Check("TuiColors.BgBlack=40", TuiColors.BgBlack == 40);
        Check("TuiColors.BgWhite=47", TuiColors.BgWhite == 47);

        Check("TuiColors.BrightBlack=90", TuiColors.BrightBlack == 90);
        Check("TuiColors.BrightWhite=97", TuiColors.BrightWhite == 97);

        Check("TuiColors.BgBrightBlack=100", TuiColors.BgBrightBlack == 100);
        Check("TuiColors.BgBrightWhite=107", TuiColors.BgBrightWhite == 107);
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

        // 流式未闭合 span：无 «/» 时样式持续到行尾（推理流式逐 token 追加）
        Check("ParseInline 未闭合«dim»持续淡化",
            MarkdownParser.ParseInline("«dim»思考中").Any(r => r.Color == 2 && r.Text == "思考中"));

        // 未知标签按字面输出，不崩溃
        Check("ParseInline 未知标签按字面",
            MarkdownParser.ParseInline("«nope»文本").Any(r => r.Text.Contains("nope")));

        // 端到端：RenderMessage 单段落回退路径也要识别 markup（此前直接字面输出）
        var rm = WayCoder.UI.TuiMarkdown.RenderMessage("«dim»思考«/»回答", "assistant", 80);
        Check("RenderMessage markup 淡化", rm.Any(line => line.Any(seg => seg.Fg == 2)));

        // 嵌套标记：内层覆盖外层，«/» 逐层弹栈恢复（栈模型）
        var nestedMk = MarkdownParser.ParseInline("«bold»粗«red»红«/»粗«/»");
        Check("ParseInline 嵌套 bold→red→bold 弹栈",
            nestedMk.Count == 3
            && nestedMk[0].Color == 1 && nestedMk[0].Text == "粗"
            && nestedMk[1].Color == 31 && nestedMk[1].Text == "红"
            && nestedMk[2].Color == 1 && nestedMk[2].Text == "粗");

        // 块级跨行（含空行）：«dim»…«/» 包裹多行推理内容，样式贯穿且保留空行
        var blk = WayCoder.UI.TuiMarkdown.RenderMessage("«dim»第一行\n\n第二行«/»\n正常", "assistant", 80);
        Check("RenderMessage 块级跨行 dim 贯穿空行",
            blk.Any(l => l.Any(s => s.Fg == 2 && s.Text == "第一行"))
            && blk.Any(l => l.Any(s => s.Fg == 2 && s.Text == "第二行")));
        Check("RenderMessage 块级关闭后恢复正常",
            blk.Any(l => l.Any(s => s.Text == "正常" && s.Fg != 2)));

        // 块级流式未闭合：开标签后无 «/»，样式持续到内容末尾
        var blkOpen = WayCoder.UI.TuiMarkdown.RenderMessage("«dim»思考中\n还在想", "assistant", 80);
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
        // DiffPreview 测试
        // ================================================================
    }
}
