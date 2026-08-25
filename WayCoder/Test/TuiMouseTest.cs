using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.Tui.Controls;

namespace WayCoder;

/// <summary>
/// TUI 鼠标支持测试 —— 离屏模拟鼠标事件（点击 / 滚轮 / 悬停 / 拖拽），
/// 逐项验证每个控件与界面的 OnMouse 是否正确响应。
///
/// 运行：waycoder --tui-mouse
///
/// 原理：OnMouse(InputEvent) 是纯函数式接口 —— 传入绝对坐标，返回「是否消费」并改变控件状态，
///       不依赖真实终端 / 鼠标硬件，故可完全离屏自动化、无需交互。
/// </summary>
public static class TuiMouseTest
{
    /// <summary>构造一个鼠标事件（坐标均为绝对屏幕坐标）。</summary>
    static InputEvent Mouse(int x, int y, bool left = false, bool right = false,
        bool scrollUp = false, bool scrollDown = false, bool motion = false, bool release = false)
        => new InputEvent
        {
            Type = InputType.Mouse,
            MouseX = x,
            MouseY = y,
            MouseLeft = left,
            MouseRight = right,
            MouseScrollUp = scrollUp,
            MouseScrollDown = scrollDown,
            MouseMotion = motion,
            MouseRelease = release,
        };

    /// <summary>收集所有鼠标支持断言 (名称, 是否通过)。供 --tui-mouse 报告与 SelfTest 复用。</summary>
    public static List<(string Name, bool Pass)> CollectChecks()
    {
        var checks = new List<(string, bool)>();
        void Check(string name, bool ok) => checks.Add((name, ok));

        // 固定终端尺寸，保证窗口拖拽 / 缩放 clamp 稳定
        var saved = Tty.SizeOverride;
        Tty.SizeOverride = (100, 40);
        try
        {
            TestButton(Check);
            TestList(Check);
            TestCheckbox(Check);
            TestSeekBar(Check);
            TestScrollbar(Check);
            TestListView(Check);
            TestRichEditor(Check);
            TestButtonGroup(Check);
            TestInlinePermission(Check);
            TestViewRouting(Check);
            TestWindowDrag(Check);
            TestScreenModal(Check);
            TestSgrParse(Check);
            TestComboBox(Check);
            TestRadioGroup(Check);
            TestTreeView(Check);
            TestTableList(Check);
            TestTabs(Check);
            TestInput(Check);
            TestTextArea(Check);
            TestPromptBar(Check);
        }
        finally
        {
            Tty.SizeOverride = saved;
        }

        return checks;
    }

    /// <summary>输出逐项报告。返回 0=全部通过，1=存在失败。</summary>
    public static int Run()
    {
        int pass = 0, fail = 0;
        Console.WriteLine("WayCoder TUI 鼠标支持测试");
        Console.WriteLine("=========================\n");

        foreach (var (name, ok) in CollectChecks())
        {
            if (ok) { pass++; Console.WriteLine($"  ✅ {name}"); }
            else { fail++; Console.WriteLine($"  ❌ {name}"); }
        }

        ReportUnsupported();
        Console.WriteLine($"\n通过: {pass}  失败: {fail}  总计: {pass + fail}");
        return fail == 0 ? 0 : 1;
    }

    // ── 控件层 ──

    static void TestButton(Action<string, bool> Check)
    {
        bool clicked = false;
        var btn = new TuiButton("确定", _ => clicked = true) { X = 5, Y = 3, Width = 10, Height = 1 };
        Check("按钮: 左键点击命中触发 OnClick", btn.OnMouse(Mouse(10, 3, left: true)) && clicked && btn.Focused);
        clicked = false;
        Check("按钮: 点击区域外不触发", !btn.OnMouse(Mouse(30, 10, left: true)) && !clicked);
        Check("按钮: 悬停进入 IsHovered", btn.OnMouse(Mouse(10, 3, motion: true)) && btn.IsHovered);
        Check("按钮: 悬停移出 IsHovered=false", !btn.OnMouse(Mouse(30, 10, motion: true)) && !btn.IsHovered);
    }

    static void TestList(Action<string, bool> Check)
    {
        var list = new TuiList { X = 0, Y = 0, Width = 20, Height = 5 };
        for (int i = 0; i < 10; i++) list.Items.Add($"item{i}");
        Check("列表: 滚轮下滚 +3", list.OnMouse(Mouse(5, 2, scrollDown: true)) && list.ScrollOffset == 3);
        Check("列表: 滚轮上滚回到 0", list.OnMouse(Mouse(5, 2, scrollUp: true)) && list.ScrollOffset == 0);
        int selected = -1;
        list.OnSelect = i => selected = i;
        Check("列表: 点击选中第 2 项", list.OnMouse(Mouse(5, 1, left: true)) && list.SelectedIndex == 1 && selected == 1);
        Check("列表: 点击后聚焦（方向键可用）", list.Focused);

        var multi = new TuiList { X = 0, Y = 0, Width = 20, Height = 5, MultiSelect = true };
        for (int i = 0; i < 5; i++) multi.Items.Add($"m{i}");
        multi.OnMouse(Mouse(5, 0, left: true));
        multi.OnMouse(Mouse(5, 2, left: true));
        Check("列表: 多选点击勾选 2 项", multi.CheckedIndices.Count == 2);
        multi.OnMouse(Mouse(5, 0, left: true));
        Check("列表: 多选再次点击取消", multi.CheckedIndices.Count == 1);
    }

    static void TestCheckbox(Action<string, bool> Check)
    {
        bool changed = false, val = false;
        var cb = new TuiCheckbox("选项") { X = 0, Y = 0, Width = 10, Height = 1 };
        cb.OnChanged = v => { changed = true; val = v; };
        Check("复选框: 点击切换 Checked", cb.OnMouse(Mouse(3, 0, left: true)) && cb.Checked && changed && val);
        changed = false;
        cb.OnMouse(Mouse(3, 0, left: true));
        Check("复选框: 再次点击取消勾选", !cb.Checked && !val && changed);
    }

    static void TestSeekBar(Action<string, bool> Check)
    {
        var bar = new TuiSeekBar(0, 100, 0) { X = 0, Y = 0, Width = 20, Height = 1, ShowLabel = false };
        Check("滑动条: 点击映射到中值", bar.OnMouse(Mouse(10, 0, left: true)) && bar.Value >= 45 && bar.Value <= 60 && bar.Focused);
        Check("滑动条: 点击左端=最小值", bar.OnMouse(Mouse(0, 0, left: true)) && bar.Value == 0);
    }

    static void TestScrollbar(Action<string, bool> Check)
    {
        var sb = new TuiScrollbar { X = 0, Y = 0, Width = 1, Height = 10, ContentHeight = 50, ViewportHeight = 10 };
        Check("滚动条: 内容超视口 IsNeeded", sb.IsNeeded);
        Check("滚动条: 滚轮下滚 +3", sb.OnMouse(Mouse(0, 5, scrollDown: true)) && sb.ScrollOffset == 3);
        int scrolled = -1;
        sb.OnScroll = v => scrolled = v;
        sb.OnMouse(Mouse(0, 9, left: true)); // 按到底部
        Check("滚动条: 点击跳转触发 OnScroll", scrolled > 0 && sb.ScrollOffset == scrolled);
        sb.OnMouse(Mouse(0, 0, left: true)); // 拖到顶部
        Check("滚动条: 拖拽到顶 ScrollOffset=0", sb.ScrollOffset == 0);
        Check("滚动条: 释放返回 true", sb.OnMouse(Mouse(0, 0, release: true)));
    }

    static void TestListView(Action<string, bool> Check)
    {
        var lv = new TuiListView { X = 0, Y = 0, Width = 30, Height = 5, IsAutoScrollToEnd = false };
        for (int i = 0; i < 10; i++) lv.AddItem(new TuiLabel($"row{i}") { Height = 1 });
        Check("懒列表: 滚轮下滚 +3", lv.OnMouse(Mouse(5, 2, scrollDown: true)) && lv.ScrollOffset == 3);
        Check("懒列表: 滚轮上滚回到 0", lv.OnMouse(Mouse(5, 2, scrollUp: true)) && lv.ScrollOffset == 0);
        int activated = -1;
        lv.OnItemActivated = i => activated = i;
        Check("懒列表: 点击激活第 1 项", lv.OnMouse(Mouse(5, 0, left: true)) && lv.SelectedIndex == 0 && activated == 0);
    }

    static void TestRichEditor(Action<string, bool> Check)
    {
        var ed = new TuiRichEditor { X = 0, Y = 0, Width = 40, Height = 5 };
        for (int i = 0; i < 20; i++) ed.Core.Lines.Add(new StringBuilder($"line {i}"));
        Check("富编辑器: 滚轮下滚 Core.Scroll+3", ed.OnMouse(Mouse(10, 2, scrollDown: true)) && ed.Core.Scroll == 3);
        ed.OnMouse(Mouse(10, 1, left: true)); // 点击第 2 可视行 → line = Scroll(3) + 1 = 4
        Check("富编辑器: 点击定位光标行", ed.Core.Cy == 4);
    }

    static void TestButtonGroup(Action<string, bool> Check)
    {
        bool b1 = false, b2 = false;
        var group = new TuiButtonGroup { X = 0, Y = 0 };
        var btn1 = group.Add("是[Y]", onClick: _ => b1 = true);
        group.Add("否[N]", onClick: _ => b2 = true);
        int cx = btn1.X + btn1.Width / 2;
        int cy = btn1.Y;
        Check("按钮组: 点击委托到子按钮", group.OnMouse(Mouse(cx, cy, left: true)) && b1 && !b2);
    }

    static void TestInlinePermission(Action<string, bool> Check)
    {
        int resolved = -1;
        var perm = new InlinePermission { X = 0, Y = 0, Width = 60, Height = 3 };
        perm.OnResolved = r => resolved = r;
        Check("权限块: 点击=允许(Result=0)", perm.OnMouse(Mouse(10, 1, left: true)) && perm.Result == 0 && resolved == 0 && perm.IsResolved);
        Check("权限块: 已解决后不再响应", !perm.OnMouse(Mouse(10, 1, left: true)));
    }

    // ── 分发层 ──

    static void TestViewRouting(Action<string, bool> Check)
    {
        var view = new TuiVBox { X = 0, Y = 0, Width = 50, Height = 20 };
        bool childClicked = false;
        var child = new TuiButton("子按钮", _ => childClicked = true) { X = 5, Y = 2, Width = 12, Height = 1 };
        child.Parent = view;
        view.Children.Add(child);
        Check("视图: 命中测试路由到子控件", view.OnMouse(Mouse(11, 2, left: true)) && childClicked);
        Check("视图: 空白处不消费", !view.OnMouse(Mouse(40, 15, left: true)));
    }

    static void TestWindowDrag(Action<string, bool> Check)
    {
        var win = new TuiWindow { X = 10, Y = 5, Width = 50, Height = 20, Title = "测试", IsResizeable = false };
        Check("窗口: 标题栏按下开始拖拽", win.OnMouse(Mouse(35, 5, left: true)));
        win.OnMouse(Mouse(40, 7, left: true)); // 拖拽移动 dx=5, dy=2
        Check("窗口: 拖拽移动更新位置", win.X == 15 && win.Y == 7);
        Check("窗口: 释放停止拖拽", win.OnMouse(Mouse(40, 7, release: true)));
    }

    static void TestScreenModal(Action<string, bool> Check)
    {
        var screen = new TestScreen(100, 40);
        screen.AddWindow(new TuiWindow { X = 5, Y = 5, Width = 30, Height = 10, Title = "normal", WindowHAlign = EHAlign.Stretch, WindowVAlign = EVAlign.Stretch });
        screen.AddWindow(new TuiWindow { X = 40, Y = 5, Width = 30, Height = 10, Title = "modal", Modal = true, WindowHAlign = EHAlign.Stretch, WindowVAlign = EVAlign.Stretch });
        Check("屏幕: 检测到模态窗口", screen.HasModal);
        Check("屏幕: 模态遮罩拦截外部点击", screen.OnMouse(Mouse(10, 10, left: true)));

        var screen2 = new TestScreen(100, 40);
        screen2.AddWindow(new TuiWindow { X = 5, Y = 5, Width = 30, Height = 10, Title = "w", WindowHAlign = EHAlign.Stretch, WindowVAlign = EVAlign.Stretch });
        Check("屏幕: 无模态路由到窗口", screen2.OnMouse(Mouse(10, 10, left: true)));
    }

    // ── SGR 解析层（真实终端字节 → InputEvent）──

    static void TestSgrParse(Action<string, bool> Check)
    {
        // \x1b[<0;10;5M → ParseSgrMouse 收到 '<' 之后的内容 "0;10;5M"
        var ev = InputManager.ParseSgrMouse("0;10;5M");
        Check("SGR: 按下 0;10;5M → 鼠标事件", ev != null && ev.Type == InputType.Mouse);
        Check("SGR: 坐标 1-based→0-based (X=9,Y=4)", ev != null && ev.MouseX == 9 && ev.MouseY == 4);
        Check("SGR: code=0 → 左键非滚轮非移动非释放", ev != null && ev.MouseLeft && !ev.MouseRight && !ev.MouseScrollUp && !ev.MouseScrollDown && !ev.MouseMotion && !ev.MouseRelease);
        Check("SGR: MouseButton=0", ev != null && ev.MouseButton == 0);

        var rel = InputManager.ParseSgrMouse("0;10;5m");
        Check("SGR: m 结尾 → 释放事件", rel != null && rel.MouseRelease && !rel.MouseLeft);

        var right = InputManager.ParseSgrMouse("2;3;4M");
        Check("SGR: code=2 → 右键", right != null && right.MouseRight && !right.MouseLeft);

        var up = InputManager.ParseSgrMouse("64;3;4M");
        Check("SGR: code=64 → 滚轮上", up != null && up.MouseScrollUp);
        var down = InputManager.ParseSgrMouse("65;3;4M");
        Check("SGR: code=65 → 滚轮下", down != null && down.MouseScrollDown);

        var motion = InputManager.ParseSgrMouse("35;3;4M");
        Check("SGR: code=35 → 移动事件", motion != null && motion.MouseMotion && !motion.MouseLeft);

        Check("SGR: 短序列 → null", InputManager.ParseSgrMouse("0") == null);
        Check("SGR: 非法数字 → null", InputManager.ParseSgrMouse("a;b;cM") == null);
    }

    // ── 新增控件的鼠标支持（点击=聚焦+交互）──

    static void TestComboBox(Action<string, bool> Check)
    {
        var cb = new TuiComboBox(["A", "B", "C"]) { X = 0, Y = 0, Width = 10, Height = 1 };
        int changed = -2;
        cb.OnSelectionChanged = i => changed = i;
        int expanded = 0;
        cb.OnExpandedChanged = b => expanded++;

        Check("下拉框: 折叠点击展开+聚焦", cb.OnMouse(Mouse(5, 0, left: true)) && cb.IsExpanded && cb.Focused);
        Check("下拉框: 展开触发 OnExpandedChanged", expanded == 1);
        // 下拉行从 absY+1 起：第 2 项在 absY+1+1
        Check("下拉框: 点下拉第 2 项选中", cb.OnMouse(Mouse(5, 2, left: true)) && cb.SelectedIndex == 1 && changed == 1);
        Check("下拉框: 选中后自动折叠", !cb.IsExpanded);
        // 再次展开后点击下拉范围外不消费
        cb.OnMouse(Mouse(5, 0, left: true));
        Check("下拉框: 下拉范围外不消费", !cb.OnMouse(Mouse(15, 5, left: true)));
    }

    static void TestRadioGroup(Action<string, bool> Check)
    {
        var rg = new TuiRadioGroup(["甲", "乙", "丙"]) { X = 0, Y = 0 };
        int changed = -2;
        rg.OnSelectionChanged = i => changed = i;
        Check("单选组: 点击第 2 项选中+聚焦", rg.OnMouse(Mouse(3, 1, left: true)) && rg.SelectedIndex == 1 && changed == 1 && rg.Focused);
        Check("单选组: 点击空白不消费", !rg.OnMouse(Mouse(3, 5, left: true)));
        rg.OnMouse(Mouse(3, 0, left: true));
        Check("单选组: 再次点击切换选中", rg.SelectedIndex == 0 && changed == 0);
    }

    static void TestTreeView(Action<string, bool> Check)
    {
        var tv = new TuiTreeView { X = 0, Y = 0, Width = 40, Height = 10 };
        for (int i = 0; i < 15; i++) tv.AddRoot($"节点{i}"); // 15 根节点 > 视口 10 行，可滚动
        Check("树: 点击第 1 行选中+聚焦", tv.OnMouse(Mouse(5, 0, left: true)) && tv.SelectedNode == tv.RootNodes[0] && tv.Focused);
        Check("树: 滚轮下滚消费", tv.OnMouse(Mouse(5, 2, scrollDown: true)));
        Check("树: 滚动后首行=第4节点", tv.OnMouse(Mouse(5, 0, left: true)) && tv.SelectedNode == tv.RootNodes[3]);
        Check("树: 滚轮上滚消费", tv.OnMouse(Mouse(5, 2, scrollUp: true)));
        Check("树: 上滚后首行=第1节点", tv.OnMouse(Mouse(5, 0, left: true)) && tv.SelectedNode == tv.RootNodes[0]);

        // 展开符列：非叶节点行内 2 列 ▼/▶
        var tv2 = new TuiTreeView { X = 0, Y = 0, Width = 40, Height = 10 };
        var root = tv2.AddRoot("根");
        root.Add(new TuiTreeNode("子"));
        Check("树: 点展开符列展开", tv2.OnMouse(Mouse(0, 0, left: true)) && root.IsExpanded);
        Check("树: 再点展开符列折叠", tv2.OnMouse(Mouse(0, 0, left: true)) && !root.IsExpanded);
    }

    static void TestTableList(Action<string, bool> Check)
    {
        var tl = new TuiTableList { X = 0, Y = 0, Width = 30, Height = 6 };
        tl.AddColumn("名称", 10);
        tl.AddRow("AAA", "1");
        tl.AddGroupHeader("组 B");
        tl.AddRow("BBB", "2");
        int changed = -2;
        tl.OnSelectionChanged = i => changed = i;
        // ShowHeader && Height>=3 → 表头 absY、分隔 absY+1、数据自 absY+2
        Check("表格: 点击第 1 行数据选中+聚焦", tl.OnMouse(Mouse(3, 2, left: true)) && tl.SelectedIndex == 0 && changed == 0 && tl.Focused);
        Check("表格: 点组头行不选中", !tl.OnMouse(Mouse(3, 3, left: true)) && tl.SelectedIndex == 0);
        Check("表格: 点组头下数据行选中", tl.OnMouse(Mouse(3, 4, left: true)) && tl.SelectedIndex == 2);
        Check("表格: 点表头不消费", !tl.OnMouse(Mouse(3, 0, left: true)));
        Check("表格: 滚轮下滚消费", tl.OnMouse(Mouse(3, 2, scrollDown: true)));
    }

    static void TestTabs(Action<string, bool> Check)
    {
        var tabs = new TuiTabs { X = 0, Y = 0, Width = 30, Height = 1 };
        tabs.AddTab("甲", new TuiLabel("1"));
        tabs.AddTab("乙", new TuiLabel("2"));
        tabs.AddTab("丙", new TuiLabel("3"));
        int changed = -2;
        tabs.OnSelectionChanged = i => changed = i;
        // tabW = Max(6, 30/3=10) = 10；tab2 在 [10,20)
        Check("标签页: 点击第 2 个切换+聚焦", tabs.OnMouse(Mouse(15, 0, left: true)) && tabs.SelectedIndex == 1 && changed == 1 && tabs.Focused);
        Check("标签页: 点击第 3 个切换", tabs.OnMouse(Mouse(25, 0, left: true)) && tabs.SelectedIndex == 2);
        Check("标签页: 点击越界不消费", !tabs.OnMouse(Mouse(0, 5, left: true)));
    }

    static void TestInput(Action<string, bool> Check)
    {
        var input = new TuiInput { X = 0, Y = 0, Width = 20, Height = 1, Text = "hello 世界" };
        Check("输入框: 点击第 3 列定位光标+聚焦", input.OnMouse(Mouse(2, 0, left: true)) && input.CursorPos == 2 && input.Focused);
        Check("输入框: 点击 CJK 字符列定位", input.OnMouse(Mouse(8, 0, left: true)) && input.CursorPos == 7);
        // "hello 世界" = 8 字符；点超长末尾 → 光标落行尾
        Check("输入框: 点击末尾后定位到行尾", input.OnMouse(Mouse(19, 0, left: true)) && input.CursorPos == 8);
    }

    static void TestTextArea(Action<string, bool> Check)
    {
        var ta = new TuiTextArea { X = 0, Y = 0, Width = 20, Height = 5 };
        ta.Lines.Add("line one");
        ta.Lines.Add("line two");
        Check("多行框: 点击第 2 行定位光标+聚焦", ta.OnMouse(Mouse(3, 1, left: true)) && ta.CursorRow == 1 && ta.CursorCol == 3 && ta.Focused);
        // Lines 初始为 [""]，Add 两行后共 3 行；点可视空白落到最后一行尾
        Check("多行框: 点击空白落到底行尾", ta.OnMouse(Mouse(3, 4, left: true)) && ta.CursorRow == 2 && ta.CursorCol == 8);
    }

    static void TestPromptBar(Action<string, bool> Check)
    {
        var bar = new TuiPromptBar { X = 0, Y = 0, Width = 40, Height = 4, MaxVisible = 8 };
        bar.Items.Add(new PromptItem { Label = "甲", Detail = "d1" });
        bar.Items.Add(new PromptItem { Label = "乙", Detail = "d2" });
        PromptItem? selected = null;
        bar.OnSelect = i => selected = i;
        // Bg==0 → 有边框，内容自 absY+1 起；Height=4 → 2 行可见
        Check("提示栏: 点击第 2 项选中+激活+聚焦", bar.OnMouse(Mouse(5, 2, left: true)) && bar.SelectedIndex == 1 && selected != null && bar.Focused);
        Check("提示栏: 点击边框行不消费", !bar.OnMouse(Mouse(5, 0, left: true)));
    }

    /// <summary>最小测试屏幕：手动固定终端尺寸（TW/TH），避免 ChatScreen 完整布局与事件订阅的副作用。</summary>
    private sealed class TestScreen : TuiScreen
    {
        public TestScreen(int w, int h)
        {
            TW = w;
            TH = h;
            RootView.Width = w;
            RootView.Height = h;
            RootView.Layout();
        }
    }

    // ── 报告 ──

    /// <summary>报告已知不支持鼠标的界面（全屏 ANSI 对话框，走 Console.ReadKey 阻塞循环）。</summary>
    static void ReportUnsupported()
    {
        Console.WriteLine("\n── 已知不支持鼠标的界面（全屏 ANSI 对话框，走 Console.ReadKey 阻塞循环，不经 OnMouse 分发）──");
        Console.WriteLine("  · 模型选择器 ModelPicker (/m)");
        Console.WriteLine("  · 会话管理器 SessionPicker (/s)");
        Console.WriteLine("  · 推理深度 ReasoningPicker (/r)");
        Console.WriteLine("  · 命令面板 CommandPalette (/c)");
        Console.WriteLine("  · 文件选择器 FilePicker (/f)");
        Console.WriteLine("  （这些界面内部虽用 TuiButton 等控件，但输入层未接入鼠标，仅键盘可用）");
    }
}
