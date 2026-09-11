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
    private static void TestChunk8Ui(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        // 原 Chunk8 是单个方法，cols/rows 定义在前一段（对话框渲染）里；拆成独立方法后本段自带一份，
        // 取值方式与那段保持一致（Tty.Cols / Tty.Rows）
        int cols = Tty.Cols;
        int rows = Tty.Rows;

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

        // ── 输入失焦兜底：任务后 InputArea.Focused 被外部清掉（ChatScreen 典型「输入框失灵」前置态）──
        //    TuiEditBase.OnKey 在 !IsEnabled || !Focused 时丢弃全部字面键；当无模态、无弹窗、面向聊天屏时，
        //    打字必须重新聚焦输入框并成功录入 —— 否则只全局快捷键(Ctrl+M)能走、字符全被吞。
        {
            var focusScr = new ChatScreen();
            focusScr.Activate();
            focusScr.InputArea.Focused = false; // 模拟：模态开/关后 _savedRootFocus 恢复链漏执行，或焦点停在非输入控件
            Check("输入失焦兜底: 前置失焦成立", !focusScr.InputArea.Focused);
            focusScr.OnKey(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
            Check("输入失焦兜底: 打字后焦点交还输入框", focusScr.InputArea.Focused);
            Check("输入失焦兜底: 字符成功录入(不被 !Focused 吞掉)", focusScr.InputArea.Text == "a");
        }

        // ── 防「已关闭窗口 + evt 未置位 → RenderWait(timeout=0) 卡死主循环」──
        // .tui 对话框（ProviderPicker 等）若未 RegisterShortcut(Esc) 置位 evt，ESC 关窗后 RenderWait
        // 会因 evt 永不置位而永久等待，主循环被堵在对话框里 → 「消息进列表但命令没执行」。
        // 修：RenderWait 发现窗口已关闭（win.Screen==null）即返回。
        {
            var scr = new ChatScreen();
            scr.Activate();
            var win = TuiDialog.Confirm("guard", "y?", _ => { });
            scr.ShowWindow(win);            // win.Screen = scr
            Check("RenderWait 守卫: 窗口已挂上", win.Screen == scr);
            scr.CloseWindow(win);            // win.Screen = null（模拟 ESC 关窗但 evt 未置位）
            using var evt = new ManualResetEventSlim(false);
            long t0 = Environment.TickCount64;
            UxHelper.RenderWait(scr, evt, 0, win);
            Check("RenderWait 守卫: 已关闭窗口立即返回(不卡死)", Environment.TickCount64 - t0 < 2000);
        }
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

        // 代码块渲染（TuiMarkdown.RenderMessage → RenderCodeBlock）：文本必须出现在色段里
        var cRender = WayCoder.UI.Tui.TuiMarkdown.RenderMessage("```csharp\ncode001 = 1;\n```", "assistant", 80);
        Check("TuiMarkdown 代码块渲染含文本", cRender.Any(l => l.Any(s => s.Text.Contains("code001"))));
        Check("TuiMarkdown 代码块渲染含行号", cRender.Any(l => l.Any(s => s.Text.Contains("1"))));

        // 端到端：ChatScreen 渲染帧应包含消息正文（TuiListItem → TuiMarkdown → WriteAt 全链路）
        {
            var msgScreen = new ChatScreen();
            msgScreen.Activate();
            msgScreen.ChatList.Width = 80;
            msgScreen.ChatList.Height = 20;
            msgScreen.AddMessage("这是正文测试内容ABCXYZ", "assistant");
            msgScreen.IsIncrementalUpdate = false;
            var msgFrame = new System.Text.StringBuilder();
            msgScreen.Render(msgFrame);
            Check("ChatScreen 渲染含消息正文", msgFrame.ToString().Contains("ABCXYZ"));
            msgScreen.Deactivate();
        }

        // 长消息滚屏：条目高度随正文增长 + 滚动后帧变化（修复「代码超过屏幕卡住无法滚屏」）
        {
            var scr2 = new ChatScreen();
            scr2.Activate();
            scr2.ChatList.Width = 60;
            scr2.ChatList.Height = 10;
            // 多行消息（用户反馈「超过屏幕卡住无法滚屏」——此前多行被段落折叠，条目高度不足）
            // 纯文本多行：应保留 60 行
            var longMsg = string.Join("\n", Enumerable.Range(1, 60).Select(i => $"第{i:000}行内容"));
            scr2.AddMessage(longMsg, "assistant");
            scr2.ChatList.ReLayout();
            var bodyRenderLines = WayCoder.UI.Tui.TuiMarkdown.RenderMessage(longMsg, "assistant", 56).Count;
            Check($"多行纯文本: 正文渲染 {bodyRenderLines} 行 ≥ 60", bodyRenderLines >= 60);
            Check($"多行纯文本: 条目高度随正文增长 (ContentHeight={scr2.ChatList.ContentHeight})", scr2.ChatList.ContentHeight >= 60);
            // 代码块长消息也应保留行数
            var longCode = "```csharp\n" + string.Join("\n", Enumerable.Range(1, 60).Select(i => $"code{i:000} = {i};")) + "\n```";
            var codeLines2 = WayCoder.UI.Tui.TuiMarkdown.RenderMessage(longCode, "assistant", 56).Count;
            Check($"长代码块: 正文渲染 {codeLines2} 行 ≥ 60", codeLines2 >= 60);

            // 500 行代码块：渲染 + 高度 + 滚屏（用户反馈场景）
            {
                var scr500 = new ChatScreen();
                scr500.Activate();
                scr500.ChatList.Width = 80;
                scr500.ChatList.Height = 15;
                var code500 = "```csharp\n" + string.Join("\n", Enumerable.Range(1, 500).Select(i => $"var x{i} = {i};")) + "\n```";
                scr500.AddMessage(code500, "assistant");
                scr500.ChatList.ReLayout();
                var r500 = WayCoder.UI.Tui.TuiMarkdown.RenderMessage(code500, "assistant", 76).Count;
                Check($"500行代码: 渲染 {r500} 行 ≈ 502", Math.Abs(r500 - 502) <= 2);
                Check($"500行代码: 高度随正文 (ContentHeight={scr500.ChatList.ContentHeight})", scr500.ChatList.ContentHeight >= 500);
                scr500.ChatList.ScrollToBottom();
                scr500.IsIncrementalUpdate = false;
                var f500a = new System.Text.StringBuilder();
                scr500.Render(f500a);
                var bottom500 = f500a.ToString();
                scr500.ChatList.ScrollUp(100); // 向上滚 100 行
                var f500b = new System.Text.StringBuilder();
                scr500.Render(f500b);
                var mid500 = f500b.ToString();
                scr500.ChatList.ScrollToTop();
                var f500c = new System.Text.StringBuilder();
                scr500.Render(f500c);
                var top500 = f500c.ToString();
                Check("500行代码: 滚到底/中间/顶部帧互不相同", bottom500 != mid500 && mid500 != top500 && bottom500 != top500);
                Check("500行代码: 渲染无异常", true);
                scr500.Deactivate();
            }

            // 1000 行 markdown 纯文本 + 1000 行表格：不崩溃、行数保留
            {
                var md1000 = string.Join("\n", Enumerable.Range(1, 1000).Select(i => $"这是第{i:0000}行 markdown 内容"));
                var rMd = WayCoder.UI.Tui.TuiMarkdown.RenderMessage(md1000, "assistant", 76).Count;
                Check($"1000行markdown: 渲染 {rMd} 行", rMd >= 1000);

                var tbl1000 = "| A | B |\n|---|---|\n" + string.Join("\n", Enumerable.Range(1, 1000).Select(i => $"| {i} | 值{i} |"));
                var rTbl = WayCoder.UI.Tui.TuiMarkdown.RenderMessage(tbl1000, "assistant", 76);
                Check($"1000行表格: 渲染不崩溃 ({rTbl.Count} 行)", rTbl.Count > 0);
                Check($"1000行表格: 含表头", rTbl.Any(l => l.Any(s => s.Text.Contains('A'))));

                // 端到端：1000 行 markdown 渲染到 ChatScreen 帧
                var scr1000 = new ChatScreen();
                scr1000.Activate();
                scr1000.ChatList.Width = 80;
                scr1000.ChatList.Height = 15;
                scr1000.AddMessage(md1000, "assistant");
                scr1000.ChatList.ReLayout();
                Check($"1000行markdown: 高度随正文 (ContentHeight={scr1000.ChatList.ContentHeight})", scr1000.ChatList.ContentHeight >= 1000);
                scr1000.ChatList.ScrollToBottom();
                scr1000.IsIncrementalUpdate = false;
                var f1000a = new System.Text.StringBuilder();
                scr1000.Render(f1000a);
                scr1000.ChatList.ScrollUp(100);
                var f1000b = new System.Text.StringBuilder();
                scr1000.Render(f1000b);
                Check("1000行markdown: 滚屏帧变化", f1000a.ToString() != f1000b.ToString());
                scr1000.Deactivate();
            }

            // 1 万条混合消息压力测试（WAYCODER_STRESS=1 才跑，避免拖慢常规自测）：不崩溃、可渲染、可滚动
            if (Environment.GetEnvironmentVariable("WAYCODER_STRESS") == "1")
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var scr1w = new ChatScreen();
                scr1w.Activate();
                scr1w.ChatList.Width = 80;
                scr1w.ChatList.Height = 15;
                try
                {
                    for (int i = 0; i < 10_000; i++)
                    {
                        switch (i % 8)
                        {
                            case 0: scr1w.AddMessage($"用户消息 {i}", "user"); break;
                            case 1: scr1w.AddMessage($"智能体回复内容 {i}", "assistant"); break;
                            case 2: scr1w.AddMessage($"系统提示第 {i} 条", "system"); break;
                            case 3: scr1w.AddMessage($"工具输出: read_file 返回内容 {i}", "tool"); break;
                            case 4: scr1w.AddMessage($"```csharp\nvar x{i} = {i};\n```", "assistant"); break;
                            case 5: scr1w.AddMessage($"| 列{i} | 值 |\n|---|---|\n| {i} | data |", "assistant"); break;
                            case 6: scr1w.AddMessage("```csharp\n" + string.Join("\n", Enumerable.Range(1, 500).Select(k => $"var v{k} = {k};")) + "\n```", "assistant"); break; // 500 行代码块
                            case 7: scr1w.AddMessage(new string('长', 5000), "assistant"); break; // 5000 字符超长文本
                        }
                    }
                    scr1w.ChatList.ReLayout();
                    sw.Stop();
                    Check($"1万消息: 添加+布局 {sw.ElapsedMilliseconds}ms < 30s", sw.ElapsedMilliseconds < 30_000);
                    Check($"1万消息: 内容高度合理 (ContentHeight={scr1w.ChatList.ContentHeight})", scr1w.ChatList.ContentHeight > 0);
                    // 渲染三帧（底部/中间/顶部）验证不崩溃
                    scr1w.ChatList.ScrollToBottom();
                    scr1w.IsIncrementalUpdate = false;
                    var fW1 = new System.Text.StringBuilder();
                    scr1w.Render(fW1);
                    scr1w.ChatList.ScrollUp(200);
                    var fW2 = new System.Text.StringBuilder();
                    scr1w.Render(fW2);
                    scr1w.ChatList.ScrollToTop();
                    var fW3 = new System.Text.StringBuilder();
                    scr1w.Render(fW3);
                    Check("1万消息: 底/中/顶渲染帧不同", fW1.ToString() != fW2.ToString() && fW2.ToString() != fW3.ToString());
                    Check("1万消息: 渲染无异常", true);
                }
                catch (Exception ex)
                {
                    Check($"1万消息: 异常 {ex.GetType().Name}: {ex.Message}", false);
                }
                scr1w.Deactivate();
            }

            // Web 版 HTTP 并发压力（WAYCODER_STRESS=1 才跑）：并发 GET 请求，无 5xx/无崩溃
            if (Environment.GetEnvironmentVariable("WAYCODER_STRESS") == "1")
            {
                var webAgent = new Agent(new LLM("test", "sk-test"));
                try
                {
                    var webSrv = new WayCoder.UI.Web.WebChatServer(webAgent, 0);
                    webSrv.Start();
                    var wport = webSrv.Port;
                    try
                    {
                        var failures = 0;
                        Parallel.For(0, 30, i =>
                        {
                            try
                            {
                                using var hc = new HttpClient();
                                var url = (int)(i % 3) switch
                                {
                                    0 => $"http://127.0.0.1:{wport}/models",
                                    1 => $"http://127.0.0.1:{wport}/state",
                                    _ => $"http://127.0.0.1:{wport}/",
                                };
                                var resp = hc.GetAsync(url).GetAwaiter().GetResult();
                                if ((int)resp.StatusCode >= 500) Interlocked.Increment(ref failures);
                            }
                            catch { Interlocked.Increment(ref failures); }
                        });
                        Check($"Web压力: 30 并发 GET 无 5xx ({failures} 失败)", failures == 0);
                    }
                    finally { webSrv.Stop(); }
                }
                catch (Exception ex)
                {
                    Check($"Web压力: 异常 {ex.GetType().Name}: {ex.Message}", false);
                }
            }

            // 聊天显示裁剪：超过上限自动丢最旧（会话仍在、文件持久化，仅显示层裁剪）
            {
                var scrEv = new ChatScreen();
                scrEv.Activate();
                scrEv.ChatList.Width = 80;
                scrEv.ChatList.Height = 10;
                var savedMax = Config.Instance.MaxChatMessages;
                try
                {
                    Config.Instance.MaxChatMessages = 100;
                    for (int i = 0; i < 150; i++)
                        scrEv.AddMessage($"裁剪消息{i}", "user");
                    Check($"聊天裁剪: 超过上限丢旧 ({scrEv.ChatList.ItemCount} ≤ 100)", scrEv.ChatList.ItemCount <= 100);
                    var firstItem = scrEv.ChatList.GetItem(0) as WayCoder.UI.Tui.Controls.TuiListItem;
                    var lastItem = scrEv.ChatList.GetItem(scrEv.ChatList.ItemCount - 1) as WayCoder.UI.Tui.Controls.TuiListItem;
                    Check("聊天裁剪: 最旧消息被丢弃", firstItem != null && !firstItem.MarkdownContent.Contains("裁剪消息0"));
                    Check("聊天裁剪: 新消息保留", lastItem != null && lastItem.MarkdownContent.Contains("裁剪消息149"));
                }
                finally { Config.Instance.MaxChatMessages = savedMax; }
                scrEv.Deactivate();
            }

            // 代码块预览行数封顶：超过保留头尾 + 省略标记
            {
                var savedCap = Config.Instance.MaxCodePreviewLines;
                try
                {
                    Config.Instance.MaxCodePreviewLines = 100;
                    var bigCode = "```csharp\n" + string.Join("\n", Enumerable.Range(1, 1000).Select(i => $"x{i} = {i};")) + "\n```";
                    var rCap = WayCoder.UI.Tui.TuiMarkdown.RenderMessage(bigCode, "assistant", 76);
                    Check($"代码封顶: 渲染 {rCap.Count} 行 ≤ 102", rCap.Count <= 102);
                    Check("代码封顶: 含省略标记", rCap.Any(l => l.Any(s => s.Text.Contains("省略"))));
                    // 语法高亮把行拆成 token 片段，检查单词片段即可
                    Check("代码封顶: 保留头部内容", rCap.Any(l => l.Any(s => s.Text == "x1")));
                    Check("代码封顶: 保留尾部内容", rCap.Any(l => l.Any(s => s.Text == "x1000")));
                }
                finally { Config.Instance.MaxCodePreviewLines = savedCap; }
            }

            // 非法/畸形格式消息：不崩溃（用户反馈场景）
            {
                var scrBad = new ChatScreen();
                scrBad.Activate();
                scrBad.ChatList.Width = 80;
                scrBad.ChatList.Height = 15;
                string[] badMessages = [
                    "```csharp\n没有闭合的代码块",                                        // 未闭合代码块
                    "| A | B |\n| 只有表头",                                              // 畸形表格
                    "| 1 | 2 | 3 |\n|---|---|\n| 列数不一致",                              // 表格列数不一致
                    "\x1b[31mANSI红\x1b[0m \x1b[2m淡化",                                  // ANSI 转义
                    "文本 \x00 NUL \x1b 混合控制字符",                                     // NUL/控制字符
                    "😀😃😄 emoji 中文混合 " + new string('超', 300),                     // emoji + 长
                    "😀 独立代理对",                                            // emoji 代理对
                    "```\n```\n```",                                                      // 连续代码围栏
                    "*b* **b2** `c` # h ## h2 > 引用 - 列表",                             // 混合格式
                    new string('a', 5000),                                               // 5000 字符超长单行
                    "",                                                                   // 空内容
                    "| a\\|b | c |\n|---|---|\n| x\\|y | z |",                             // 转义竖线
                    "1. 有序\n2. 列表\n3. 项",                                            // 列表
                ];
                try
                {
                    foreach (var bm in badMessages)
                        scrBad.AddMessage(bm, "assistant");
                    scrBad.ChatList.ReLayout();
                    scrBad.IsIncrementalUpdate = false;
                    var fBad = new System.Text.StringBuilder();
                    scrBad.Render(fBad);
                    Check("非法格式: 渲染不崩溃", fBad.Length > 0);
                    scrBad.ChatList.ScrollToBottom();
                    scrBad.ChatList.ScrollUp(5);
                    var fBad2 = new System.Text.StringBuilder();
                    scrBad.Render(fBad2);
                    Check("非法格式: 滚动不崩溃", fBad2.Length > 0);
                }
                catch (Exception ex)
                {
                    Check($"非法格式: 异常 {ex.GetType().Name}: {ex.Message}", false);
                }
                scrBad.Deactivate();
            }
            scr2.ChatList.ScrollToBottom();
            scr2.IsIncrementalUpdate = false;
            var scr2f1 = new System.Text.StringBuilder();
            scr2.Render(scr2f1);
            var bottomFrame2 = scr2f1.ToString();
            scr2.ChatList.ScrollUp(10);
            var scr2f2 = new System.Text.StringBuilder();
            scr2.Render(scr2f2);
            Check("长消息滚屏: 上滚后帧变化", bottomFrame2 != scr2f2.ToString());
            scr2.Deactivate();
        }

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

        // RenderBuffer 越界裁剪：内容不得溢出屏幕（否则终端自动换行把文字泄到相邻行，如动态栏文字泄到下方横线）
        var rbClipCol = new RenderBuffer();
        rbClipCol.Write(0, 10000, "超界"); // 列远超任何终端宽 → 整体丢弃（不写定位/文字）
        Check("RenderBuffer 越界列丢弃", !rbClipCol.ToString().Contains("超界"));
        var rbClipRow = new RenderBuffer();
        rbClipRow.Write(10000, 0, "越行"); // 行越界 → 丢弃
        Check("RenderBuffer 越界行丢弃", !rbClipRow.ToString().Contains("越行"));

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
        // FrameSnapshot 背景快照（颜色感知解析 + 贴回）——类保留供 WayCoder.Preview WPF 渲染器使用
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