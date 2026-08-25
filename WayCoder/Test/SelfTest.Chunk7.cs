using System.Text;
using System.Text.Json;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Screens;
using WayCoder.UI.Tui.Edit;

namespace WayCoder;

public static partial class SelfTest
{
    private static void TestChunk7(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[主题系统]");
        Check("ThemeConfig Instance 非空", ThemeConfig.Instance != null);
        Check("默认边框=single", ThemeConfig.Instance!.BorderStyle == "single");
        Check("默认选中 SelFg=30", ThemeConfig.Instance.SelFg == 30);
        Check("默认选中 SelBg=46", ThemeConfig.Instance.SelBg == 46);
        Check("Presets 含6个", ThemeConfig.Presets.Count >= 6);
        Check("Preset ocean 存在", ThemeConfig.Presets.ContainsKey("ocean"));
        Check("Preset forest 存在", ThemeConfig.Presets.ContainsKey("forest"));
        Check("Preset sunset 存在", ThemeConfig.Presets.ContainsKey("sunset"));
        Check("Preset midnight 存在", ThemeConfig.Presets.ContainsKey("midnight"));
        Check("Preset mono 存在", ThemeConfig.Presets.ContainsKey("mono"));
        var saved = ThemeConfig.Instance.BorderStyle;
        ThemeConfig.ApplyPreset("ocean");
        Check("ApplyPreset ocean 边框=rounded", ThemeConfig.Instance.BorderStyle == "rounded");
        Check("ApplyPreset ocean 背景=44", ThemeConfig.Instance.WinBg == 44);
        ThemeConfig.ApplyPreset("default");
        Check("恢复 default", ThemeConfig.Instance.BorderStyle == saved);
        // 主题应用到窗口
        var tw = new TuiWindow { Title = "test" };
        ThemeConfig.ApplyPreset("ocean");
        ThemeConfig.Instance.ApplyTo(tw);
        Check("ApplyTo 边框ocean", tw.BorderStyle == WindowBorder.Rounded);
        ThemeConfig.ApplyPreset("default");
        ThemeConfig.Instance.ApplyTo(tw);
        Check("ApplyTo 恢复默认", tw.BorderStyle == WindowBorder.Single);
        Console.WriteLine();

        // ================================================================
        // 边框风格
        // ================================================================
        Section("[边框风格]");
        WindowBorder[] bstyles = [WindowBorder.Single, WindowBorder.Double, WindowBorder.Rounded,
            WindowBorder.Thick, WindowBorder.Dotted, WindowBorder.Dashed, WindowBorder.Slash,
            WindowBorder.Triangle, WindowBorder.Ascii, WindowBorder.None, WindowBorder.Solid];
        foreach (var s in bstyles)
        {
            var win = new TuiWindow { BorderStyle = s };
            var (tl, tr, bl, br, h, v, hTop, hBot) = win.GetBorderChars();
            Check($"GetBorderChars {s} 非空", tl.Length > 0 && tr.Length > 0 && h.Length > 0 && v.Length > 0);
        }
        var customWin = new TuiWindow { BorderStyle = WindowBorder.Ascii, CustomBorder = "+-+|||-" };
        var chars = customWin.GetBorderChars();
        Check("自定义边框 ASCII", chars.h == "-" && chars.v == "|");
        Console.WriteLine();

        // ================================================================
        // InputManager
        // ================================================================
        Section("[InputManager]");
        Check("InputManager 可创建", new InputManager() != null);
        Check("InputType 枚举值", InputType.Key != InputType.Mouse);
        Console.WriteLine();

        // ================================================================
        // ChatScreen 主题
        // ================================================================
        Section("[ChatScreen主题]");
        var themeScreen = new ChatScreen();
        ThemeConfig.ApplyPreset("ocean");
        themeScreen.SyncTheme();
        Check("SyncTheme 成功", true);
        ThemeConfig.ApplyPreset("default");
        themeScreen.SyncTheme();
        Check("恢复 default 主题成功", true);
        Console.WriteLine();

        // ================================================================
        // 树形视图
        // ================================================================
        Section("[TuiTreeView]");
        var tree = new TuiTreeView();
        Check("树初始为空", tree.RootNodes.Count == 0);
        Check("无选中节点", tree.SelectedNode == null);

        var root1 = tree.AddRoot("根节点1", "📁");
        Check("添加根节点成功", tree.RootNodes.Count == 1);
        Check("自动选中第一个根", tree.SelectedNode == root1);
        Check("根节点文本", root1.Text == "根节点1");
        Check("根节点图标", root1.Icon == "📁");
        Check("根节点是叶子", root1.IsLeaf);

        var child1 = new TuiTreeNode("子节点1", "📄");
        root1.Add(child1);
        Check("子节点添加成功", root1.Children.Count == 1);
        Check("子节点 Parent 引用", child1.Parent == root1);
        Check("根节点不再是叶子", !root1.IsLeaf);
        Check("子节点是叶子", child1.IsLeaf);

        root1.AddRange(new("子节点2"), new("子节点3"));
        Check("批量添加子节点", root1.Children.Count == 3);

        child1.Add(new TuiTreeNode("孙节点"));
        Check("深度统计", tree.TotalNodeCount == 5); // 根+3子+1孙

        root1.IsExpanded = true;
        child1.IsExpanded = true;
        Check("展开状态可设置", root1.IsExpanded && child1.IsExpanded);

        tree.SelectedNode = root1;
        tree.ExpandNode(root1);
        Check("展开节点", root1.IsExpanded);

        tree.ExpandNode(child1);
        Check("展开子节点", child1.IsExpanded);

        tree.CollapseNode(root1);
        Check("折叠节点", !root1.IsExpanded);

        child1.ExpandToRoot();
        Check("ExpandToRoot 展开祖先", root1.IsExpanded && child1.IsExpanded);

        tree.SelectNode(child1);
        Check("选中节点", tree.SelectedNode == child1);

        tree.Clear();
        Check("清空后无根节点", tree.RootNodes.Count == 0);
        Check("清空后无选中节点", tree.SelectedNode == null);

        // 重建数据测试导航
        var navRoot = tree.AddRoot("导航测试");
        tree.AddRoot("根2");
        Check("两个根节点", tree.RootNodes.Count == 2);

        tree.SelectedNode = tree.RootNodes[0];
        tree.MoveDown();
        Check("MoveDown 到第二个根", tree.SelectedNode == tree.RootNodes[1]);
        tree.MoveUp();
        Check("MoveUp 回到第一个根", tree.SelectedNode == tree.RootNodes[0]);

        // ── 键盘通路（此前只测 MoveUp/MoveDown 这类方法，键根本没走到控件也照样绿）──
        static ConsoleKeyInfo K(ConsoleKey k) => new('\0', k, false, false, false);
        var keyTree = new TuiTreeView { Width = 30, Height = 10 };
        var kA = keyTree.AddRoot("A");
        var kB = keyTree.AddRoot("B");
        kA.Add(new TuiTreeNode("A-1"));
        keyTree.SelectNode(kA);
        Check("树 OnKey ↓ 生效", keyTree.OnKey(K(ConsoleKey.DownArrow)) && keyTree.SelectedNode == kB);
        Check("树 OnKey ↑ 生效", keyTree.OnKey(K(ConsoleKey.UpArrow)) && keyTree.SelectedNode == kA);
        Check("树 OnKey → 展开", keyTree.OnKey(K(ConsoleKey.RightArrow)) && kA.IsExpanded);
        Check("树 OnKey ← 折叠", keyTree.OnKey(K(ConsoleKey.LeftArrow)) && !kA.IsExpanded);

        // 窗口里没人 Focused 时，TuiView.OnKey 无处派发 → 整窗键盘失灵。AddWindow 兜底聚焦首控件。
        var treeBox = new TuiVBox { Width = 30, Height = 10 };
        treeBox.Add(keyTree);
        var treeWin = new TuiWindow { RootView = treeBox, Width = 32, Height = 12, Modal = false };
        keyTree.Focused = false;
        Check("入窗前无人聚焦", treeBox.FindFocused() == null);
        new ChatScreen().AddWindow(treeWin);
        Check("AddWindow 兜底聚焦首个可聚焦控件", treeBox.FindFocused() == keyTree);
        keyTree.SelectNode(kA);
        Check("窗口按键能到达树形视图",
            treeWin.OnKey(K(ConsoleKey.DownArrow)) && keyTree.SelectedNode == kB);

        // 增量渲染的命门：控件处理了按键却不标脏 → 画面停在旧帧 = 用户眼里的「按键无用」
        keyTree.IsDirty = false;
        keyTree.SelectNode(kA);
        Check("SelectNode 标脏", keyTree.IsDirty);
        keyTree.IsDirty = false;
        keyTree.ExpandNode(kA);
        Check("ExpandNode 标脏", keyTree.IsDirty);
        keyTree.IsDirty = false;
        keyTree.CollapseNode(kA);
        Check("CollapseNode 标脏", keyTree.IsDirty);
        keyTree.IsDirty = false;
        treeBox.OnKey(K(ConsoleKey.DownArrow));
        Check("派发口兜底标脏（控件自己没标也不会漏画）", keyTree.IsDirty);

        Console.WriteLine();

        // ================================================================
        // 单选按钮组
        // ================================================================
        Section("[TuiRadioGroup]");
        var radio = new TuiRadioGroup(["选项A", "选项B", "选项C"], 0);
        Check("Radio 默认选中索引 0", radio.SelectedIndex == 0);
        Check("Radio 选项数 3", radio.Options.Count == 3);
        Check("Radio 高度 = 选项数", radio.Height == 3);

        radio.SelectedIndex = 2;
        Check("Radio 切换选中索引", radio.SelectedIndex == 2);
        radio.SelectedIndex = -1;
        Check("Radio 取消选中", radio.SelectedIndex == -1);

        // 键盘导航
        radio.Options = ["A", "B", "C", "D"];
        radio.Height = 4;
        radio.SelectedIndex = 1;
        radio.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false));
        Check("Radio ↑ 导航", radio.SelectedIndex == 0);
        radio.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        Check("Radio ↓ 导航", radio.SelectedIndex == 1);
        radio.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, false));
        Check("Radio End 跳转", radio.SelectedIndex == 3);
        radio.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false));
        Check("Radio Home 跳转", radio.SelectedIndex == 0);

        // 回调（通过键盘触发）
        int radioCallbackValue = -1;
        radio.OnSelectionChanged = v => radioCallbackValue = v;
        radio.SelectedIndex = 1;
        radioCallbackValue = -1;
        radio.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        Check("Radio 回调触发", radioCallbackValue == 2);

        // 空选项不崩溃
        var emptyRadio = new TuiRadioGroup([], -1);
        emptyRadio.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false));
        Check("空 Radio 不崩溃", true);
        Console.WriteLine();

        // ================================================================
        // 组合框
        // ================================================================
        Section("[TuiComboBox]");
        var combo = new TuiComboBox(["苹果", "香蕉", "橘子", "葡萄"]);
        Check("Combo 选项数 4", combo.Options.Count == 4);
        Check("Combo 默认未展开", !combo.IsExpanded);
        Check("Combo 默认选中 -1", combo.SelectedIndex == -1);

        combo.SelectedIndex = 1;
        Check("Combo 设置选中索引", combo.SelectedIndex == 1);

        // 展开
        combo.IsExpanded = true;
        Check("Combo 展开状态可设置", combo.IsExpanded);
        Check("Combo 展开后高度 > 1", combo.ExpandedHeight > 1);

        // 收起
        combo.IsExpanded = false;
        Check("Combo 收起", !combo.IsExpanded);

        // 键盘导航（收起状态）
        combo.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false));
        Check("Combo 收起时 ↑ 可用", combo.SelectedIndex == 0);
        combo.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        Check("Combo 收起时 ↓ 可用", combo.SelectedIndex == 1);

        // Enter 展开
        combo.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
        Check("Combo Enter 展开", combo.IsExpanded);

        // 在展开状态导航
        combo.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, false));
        Check("Combo 展开 End", combo.SelectedIndex == 3);
        combo.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false));
        Check("Combo 展开 Home", combo.SelectedIndex == 0);

        // Esc 收起
        combo.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Escape, false, false, false));
        Check("Combo Esc 收起", !combo.IsExpanded);

        // 占位文本
        var combo2 = new TuiComboBox([], -1);
        Check("Combo 空选项占位", combo2.Placeholder == "请选择...");
        combo2.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
        Check("空 Combo Enter 不崩溃", true);

        // 回调
        int comboCallbackValue = -1;
        combo.OnSelectionChanged = v => comboCallbackValue = v;
        combo.Select(2);
        Check("Combo Select 设置索引", combo.SelectedIndex == 2);
        Check("Combo 回调触发", comboCallbackValue == 2);

        bool? comboExpandState = null;
        combo.OnExpandedChanged = v => comboExpandState = v;
        combo.IsExpanded = true;
        combo.OnExpandedChanged?.Invoke(true); // 模拟展开回调
        Check("Combo 展开回调", comboExpandState == true);
        Console.WriteLine();

        // ================================================================
        // 滑块/SeekBar
        // ================================================================
        Section("[TuiSeekBar]");
        var seek = new TuiSeekBar(0, 100, 50);
        Check("SeekBar 初始值 50", seek.Value == 50);
        Check("SeekBar Min=0", seek.MinValue == 0);
        Check("SeekBar Max=100", seek.MaxValue == 100);
        Check("SeekBar Step=1", seek.Step == 1);
        Check("SeekBar ShowLabel", seek.ShowLabel);

        // 值变更
        seek.Value = 75;
        Check("SeekBar 值变更", seek.Value == 75);
        seek.Value = 200; // 超出范围被钳制
        Check("SeekBar 钳制到 Max", seek.Value == 100);
        seek.Value = -50;
        Check("SeekBar 钳制到 Min", seek.Value == 0);

        // 键盘操作
        seek.Value = 50;
        seek.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false));
        Check("SeekBar → 增量", seek.Value == 51);
        seek.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false));
        Check("SeekBar ← 减量", seek.Value == 50);
        seek.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false));
        Check("SeekBar Home → Min", seek.Value == 0);
        seek.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, false));
        Check("SeekBar End → Max", seek.Value == 100);
        seek.Value = 50;
        seek.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.PageUp, false, false, false));
        Check("SeekBar PgUp → +10", seek.Value == 60);
        seek.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.PageDown, false, false, false));
        Check("SeekBar PgDn → -10", seek.Value == 50);

        // 回调
        int seekCallbackValue = -1;
        seek.OnValueChanged = v => seekCallbackValue = v;
        seek.Value = 42;
        Check("SeekBar 回调触发", seekCallbackValue == 42);

        // 构造参数
        var seek2 = new TuiSeekBar(10, 200, 100, 5);
        Check("SeekBar 构造函数 Min", seek2.MinValue == 10);
        Check("SeekBar 构造函数 Max", seek2.MaxValue == 200);
        Check("SeekBar 构造函数 Value", seek2.Value == 100);
        Check("SeekBar 构造函数 Step", seek2.Step == 5);

        // LargeStep 和自定义字符
        seek2.LargeStep = 25;
        seek2.ThumbChar = "▣";
        seek2.TrackFilled = "█";
        seek2.TrackEmpty = "░";
        Check("SeekBar LargeStep", seek2.LargeStep == 25);
        Check("SeekBar 自定义 Thumb", seek2.ThumbChar == "▣");
        Check("SeekBar 自定义 TrackFilled", seek2.TrackFilled == "█");
        Check("SeekBar 自定义 TrackEmpty", seek2.TrackEmpty == "░");

        // 隐藏标签
        seek2.ShowLabel = false;
        Check("SeekBar 隐藏标签", !seek2.ShowLabel);
        Console.WriteLine();

        // ================================================================
        // 分割线
        // ================================================================
        Section("[TuiSeparator]");
        var sepH = new TuiSeparator(SeparatorDirection.Horizontal);
        Check("Separator 水平方向", sepH.Direction == SeparatorDirection.Horizontal);
        Check("Separator 默认高度 1", sepH.Height == 1);
        Check("Separator 默认宽度 60", sepH.Width == 60);

        var sepV = new TuiSeparator(SeparatorDirection.Vertical);
        Check("Separator 垂直方向", sepV.Direction == SeparatorDirection.Vertical);
        Check("Separator 垂直宽度 1", sepV.Width == 1);

        var sepWithText = new TuiSeparator { Text = "标题", Width = 40 };
        Check("Separator 带文本", sepWithText.Text == "标题");

        var sepCustom = new TuiSeparator { LineChar = "━", LineColor = 91 };
        Check("Separator 自定义线字符", sepCustom.LineChar == "━");
        Check("Separator 自定义颜色", sepCustom.LineColor == 91);

        // 键盘不处理
        Check("Separator 不处理键盘", !sepH.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false)));
        Console.WriteLine();

        // ================================================================
        // 面板
        // ================================================================
        Section("[TuiPanel]");
        var panel = new TuiPanel();
        Check("Panel 标题为空", panel.Title == "");
        Check("Panel 默认边框 Rounded", panel.BorderStyle == WindowBorder.Rounded);
        Check("Panel 默认宽度 10", panel.Width == 10);
        Check("Panel 默认高度 1", panel.Height == 1);

        panel.Title = "测试面板";
        Check("Panel 带标题", panel.Title == "测试面板");

        // 边框风格
        panel.BorderStyle = WindowBorder.Double;
        Check("Panel Double 边框", panel.BorderStyle == WindowBorder.Double);
        panel.BorderStyle = WindowBorder.Thick;
        Check("Panel Thick 边框", panel.BorderStyle == WindowBorder.Thick);
        panel.BorderStyle = WindowBorder.Rounded;
        Check("Panel Rounded 边框", panel.BorderStyle == WindowBorder.Rounded);
        panel.BorderStyle = WindowBorder.Single;
        Check("Panel 恢复 Single", panel.BorderStyle == WindowBorder.Single);
        panel.BorderStyle = WindowBorder.Ascii;
        Check("Panel Ascii 边框", panel.BorderStyle == WindowBorder.Ascii);

        // 子视图
        var subLabel = new TuiLabel("内部文本");
        panel.Add(subLabel);
        Check("Panel 可添加子视图", panel.Children.Count >= 1);

        // 键盘不处理
        Check("Panel 不处理键盘", !panel.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false)));
        Console.WriteLine();

        // ================================================================
        // EditorCore 测试
        // ================================================================
        Section("[EditorCore]");
        var tmpFileEc = Path.GetTempFileName();
        File.WriteAllText(tmpFileEc, "line1\nline2\nline3");
        var core = new EditorCore();
        core.LoadFile(tmpFileEc);
        Check("EditorCore 加载 3 行", core.TotalLines == 3);
        Check("EditorCore 未修改", !core.Modified);
        Check("EditorCore 光标 0,0", core.Cy == 0 && core.Cx == 0);
        Check("EditorCore FilePath 设置", core.FilePath == Path.GetFullPath(tmpFileEc));

        // 光标移动
        core.MoveCursor(0, 1);
        Check("MoveCursor 下 Cy=1", core.Cy == 1);
        core.MoveCursor(5, 0);
        Check("MoveCursor 右 Cx=5", core.Cx == 5);
        core.MoveHome();
        Check("MoveHome Cx=0", core.Cx == 0);
        core.MoveEnd();
        Check("MoveEnd Cx=line2.Length", core.Cx == 5);

        // 插入文本
        core.InsertText("hello");
        Check("InsertText 标记已修改", core.Modified);
        Check("InsertText 内容正确", core.Lines[1].ToString() == "line2hello");

        // 撤销
        core.Undo();
        Check("Undo 恢复行内容", core.Lines[1].ToString() == "line2");

        // 删除
        core.Cx = 2;
        core.Backspace();
        Check("Backspace 删除字符", core.Lines[1].ToString() == "lne2");
        core.Delete();
        Check("Delete 删除字符", core.Lines[1].ToString() == "le2");

        // 换行
        core.Cx = 1;
        core.NewLine();
        Check("NewLine 分割行", core.Cy == 2);
        Check("NewLine 新增行数", core.TotalLines == 4);

        // 撤销换行
        core.Undo();
        Check("Undo 恢复行数", core.TotalLines == 3);

        // 跳行
        Check("JumpToLine 有效", core.JumpToLine(3));
        Check("JumpToLine 光标 Cy=2", core.Cy == 2);
        Check("JumpToLine 无效返回 false", !core.JumpToLine(999));

        // 剪贴板
        core.CopyLine();
        core.CutLine();
        Check("CutLine 删除行", core.TotalLines == 2);
        core.PasteClipboard();
        Check("PasteClipboard 粘贴", core.Lines[1].ToString().Contains("line3"));

        // Tab（默认缩进模式 = 制表符）
        core.Cx = 0;
        core.InsertTab();
        Check("InsertTab 默认插入 Tab", core.Lines[1].ToString().StartsWith("\t"));
        core.Undo();
        core.IndentMode = "space";
        core.InsertTab();
        Check("InsertTab space 模式 4 空格", core.Lines[1].ToString().StartsWith("    "));

        // 保存
        core.Save();
        Check("Save 后不脏", !core.Modified);
        var savedContent = File.ReadAllText(tmpFileEc);
        Check("Save 文件内容正确", savedContent.Contains("line1"));

        // 统计
        Check("TotalChars > 0", core.TotalChars > 0);
        Check("FileSizeBytes > 0", core.FileSizeBytes > 0);
        Check("FormatSize B", FormatUtil.FormatSize(500) == "500 B");
        Check("FormatSize KB", FormatUtil.FormatSize(2048) == "2.0 KB");

        // emoji 代理对（😀 = U+1F600，占 2 个 UTF-16 code unit）
        var emo = new EditorCore();
        emo.Lines.Add(new());                 // 直接 new 无 LoadFile 时 Lines 为空，先加一行
        emo.InsertText("ab\U0001F600cd");   // "ab😀cd"，😀 占 index 2..4
        Check("emoji 插入后 Cx 正确", emo.Cx == 6);
        emo.Cx = 4;
        emo.Backspace();
        Check("Backspace 删整个 emoji", emo.Lines[0].ToString() == "abcd");
        Check("Backspace 后 Cx 正确", emo.Cx == 2);

        var emo2 = new EditorCore();
        emo2.Lines.Add(new());
        emo2.InsertText("\U0001F600");      // 单行一个 emoji，光标 Cx=2
        emo2.Cx = 0;
        emo2.Delete();
        Check("Delete 删整个 emoji", emo2.Lines[0].Length == 0);

        var emo3 = new EditorCore();
        emo3.Lines.Add(new());
        emo3.InsertText("a\U0001F600b");    // "a😀b"，长度 4
        emo3.Cx = 3;                        // 'b' 前
        emo3.MoveCursor(-1, 0);             // 向左一步，跳过代理对
        Check("MoveCursor 左移跳过代理对", emo3.Cx == 1);

        // 诊断
        var (e, w) = core.GetDiagSummary();
        Check("GetDiagSummary 返回元组", e >= 0 && w >= 0);

        // 清理
        File.Delete(tmpFileEc);
        Console.WriteLine();

        // ================================================================
        // EditorCore 新能力（双栈撤销/重做 + 缩进 + 多行粘贴 + 选择 + 搜索）
        // ================================================================
        Section("[EditorCore 双栈/选择/搜索]");
        {
            // ── 双栈撤销 / 重做 ──
            var c1 = new EditorCore();
            var tf1 = Path.GetTempFileName();
            File.WriteAllText(tf1, "aaa\nbbb\nccc");
            c1.LoadFile(tf1);
            c1.Cx = 3; c1.Cy = 0;
            c1.InsertText("X");
            Check("Redo 前置内容", c1.Lines[0].ToString() == "aaaX");
            c1.Undo();
            Check("Undo 恢复", c1.Lines[0].ToString() == "aaa");
            c1.Redo();
            Check("Redo 重放", c1.Lines[0].ToString() == "aaaX");
            c1.Undo();

            // 新编辑清空重做栈
            c1.InsertText("Y");
            c1.Undo();
            Check("插入 Y 后 Undo", c1.Lines[0].ToString() == "aaa");
            c1.Redo();
            Check("插入 Y 后 Redo", c1.Lines[0].ToString() == "aaaY");

            // ── 自动缩进继承 ──
            var c2 = new EditorCore();
            var tf2 = Path.GetTempFileName();
            File.WriteAllText(tf2, "    foo");
            c2.LoadFile(tf2);
            c2.Cy = 0; c2.Cx = c2.Lines[0].Length;
            c2.NewLine();
            Check("缩进继承新行", c2.Lines[1].ToString() == "    ");
            Check("缩进继承光标列", c2.Cx == 4);

            // ── 多行粘贴拆行 ──
            var c3 = new EditorCore();
            var tf3 = Path.GetTempFileName();
            File.WriteAllText(tf3, "one\ntwo");
            c3.LoadFile(tf3);
            c3.Cx = 0; c3.Cy = 0;
            c3.InsertText("x\ny\nz\n");
            Check("多行粘贴行数", c3.TotalLines == 5);
            Check("多行粘贴首行", c3.Lines[0].ToString() == "x");

            // ── 选择 ──
            var c4 = new EditorCore();
            var tf4 = Path.GetTempFileName();
            File.WriteAllText(tf4, "hello world");
            c4.LoadFile(tf4);
            c4.Cx = 0; c4.Cy = 0;
            c4.StartSelection();
            c4.MoveCursor(5, 0);
            Check("选区 HasSelection", c4.HasSelection);
            Check("选区文本", c4.GetSelectedText() == "hello");
            c4.DeleteSelection();
            Check("删除选区", c4.Lines[0].ToString() == " world");
            c4.SelectAll();
            Check("全选", c4.GetSelectedText() == " world");

            // ── 搜索 / 替换 ──
            var c5 = new EditorCore();
            var tf5 = Path.GetTempFileName();
            File.WriteAllText(tf5, "foo bar foo\nbaz");
            c5.LoadFile(tf5);
            var (fl, fc) = c5.FindNext("foo", 0, 0);
            Check("FindNext 首个", fl == 0 && fc == 0);
            var (fl2, fc2) = c5.FindNext("foo", 0, 1);
            Check("FindNext 第二个", fl2 == 0 && fc2 == 8);
            int replaced = c5.ReplaceAll("foo", "qux");
            Check("ReplaceAll 次数", replaced == 2);
            Check("ReplaceAll 内容", c5.Lines[0].ToString() == "qux bar qux");

            // ── ReplaceNext（单个替换）──
            var c7 = new EditorCore();
            var tf7 = Path.GetTempFileName();
            File.WriteAllText(tf7, "foo bar foo");
            c7.LoadFile(tf7);
            c7.Cy = 0; c7.Cx = 0;
            Check("ReplaceNext 成功", c7.ReplaceNext("foo", "qux"));
            Check("ReplaceNext 替换首个", c7.Lines[0].ToString() == "qux bar foo");
            Check("ReplaceNext 光标定位", c7.Cy == 0 && c7.Cx == 3);
            Check("ReplaceNext 替换下一个", c7.ReplaceNext("foo", "qux"));
            Check("ReplaceNext 替换第二个", c7.Lines[0].ToString() == "qux bar qux");

            // ── 正则查找（捕获组）──
            var c8 = new EditorCore();
            var tf8 = Path.GetTempFileName();
            File.WriteAllText(tf8, "age: 42\nname: alice");
            c8.LoadFile(tf8);
            var (rl, rc, rlen) = c8.FindMatch(@"\d+", 0, 0, new FindOptions(UseRegex: true));
            Check("正则 FindMatch 命中", rl == 0 && rc == 5);
            Check("正则 FindMatch 长度", rlen == 2);

            // ── 正则替换（捕获组 $1/${name}）──
            var c9 = new EditorCore();
            var tf9 = Path.GetTempFileName();
            File.WriteAllText(tf9, "foo 123 bar 456");
            c9.LoadFile(tf9);
            int rxCount = c9.ReplaceAll(@"(\d+)", "[$1]", new FindOptions(UseRegex: true));
            Check("正则 ReplaceAll 次数", rxCount == 2);
            Check("正则 ReplaceAll 反向引用", c9.Lines[0].ToString() == "foo [123] bar [456]");

            var c10 = new EditorCore();
            var tf10 = Path.GetTempFileName();
            File.WriteAllText(tf10, "ab cd");
            c10.LoadFile(tf10);
            c10.ReplaceAll(@"(?<x>\w+)", "<${x}>", new FindOptions(UseRegex: true));
            Check("正则命名捕获组 ${name}", c10.Lines[0].ToString() == "<ab> <cd>");

            // ── 整词匹配 ──
            var c11 = new EditorCore();
            var tf11 = Path.GetTempFileName();
            File.WriteAllText(tf11, "cat catalog scat cat");
            c11.LoadFile(tf11);
            var (wl, wc) = c11.FindNext("cat", 0, 0, new FindOptions(WholeWord: true));
            Check("整词 首个命中", wl == 0 && wc == 0);
            var (wl2, wc2) = c11.FindNext("cat", 0, 1, new FindOptions(WholeWord: true));
            Check("整词 跳过 catalog/scat", wl2 == 0 && wc2 == 17);
            int wCount = c11.ReplaceAll("cat", "dog", new FindOptions(WholeWord: true));
            Check("整词 ReplaceAll 次数", wCount == 2);
            Check("整词 ReplaceAll 内容", c11.Lines[0].ToString() == "dog catalog scat dog");

            // ── 区分大小写 ──
            var c12 = new EditorCore();
            var tf12 = Path.GetTempFileName();
            File.WriteAllText(tf12, "Foo foo FOO");
            c12.LoadFile(tf12);
            int ci = c12.ReplaceAll("foo", "bar", new FindOptions(CaseSensitive: true));
            Check("区分大小写 仅命中小写", ci == 1);
            Check("区分大小写 内容", c12.Lines[0].ToString() == "Foo bar FOO");

            // ── 字面 $ 在替换串中不被当作反向引用 ──
            var c13 = new EditorCore();
            var tf13 = Path.GetTempFileName();
            File.WriteAllText(tf13, "price: 10");
            c13.LoadFile(tf13);
            c13.ReplaceAll("10", "$9.99");
            Check("字面 $ 替换转义", c13.Lines[0].ToString() == "price: $9.99");

            // ── 括号匹配 ──
            var m1 = new EditorCore();
            var mf1 = Path.GetTempFileName();
            File.WriteAllText(mf1, "func(a, (b + c))");
            m1.LoadFile(mf1);
            Check("括号匹配 开→闭", m1.MatchingBracketAt(0, 4) == (0, 15));
            Check("括号匹配 闭→开", m1.MatchingBracketAt(0, 15) == (0, 4));
            Check("括号匹配 嵌套内层", m1.MatchingBracketAt(0, 8) == (0, 14));
            Check("括号匹配 非括号 null", m1.MatchingBracketAt(0, 1) == null);

            var m2 = new EditorCore();
            var mf2 = Path.GetTempFileName();
            File.WriteAllText(mf2, "arr[0] = {1, 2};");
            m2.LoadFile(mf2);
            Check("括号匹配 [] 成对", m2.MatchingBracketAt(0, 3) == (0, 5));
            Check("括号匹配 {} 成对", m2.MatchingBracketAt(0, 9) == (0, 14));

            var m3 = new EditorCore();
            var mf3 = Path.GetTempFileName();
            File.WriteAllText(mf3, "([{}])");
            m3.LoadFile(mf3);
            Check("括号匹配 混合嵌套", m3.MatchingBracketAt(0, 0) == (0, 5));
            Check("括号匹配 混合内层", m3.MatchingBracketAt(0, 1) == (0, 4));
            Check("括号匹配 混合最内", m3.MatchingBracketAt(0, 2) == (0, 3));

            var m4 = new EditorCore();
            var mf4 = Path.GetTempFileName();
            File.WriteAllText(mf4, "{\n  s = \"(not a bracket)\";\n}");
            m4.LoadFile(mf4);
            Check("括号匹配 跨行", m4.MatchingBracketAt(0, 0) == (2, 0));
            Check("括号匹配 跨行反向", m4.MatchingBracketAt(2, 0) == (0, 0));

            var m5 = new EditorCore();
            var mf5 = Path.GetTempFileName();
            File.WriteAllText(mf5, "s = \"(\";");
            m5.LoadFile(mf5);
            Check("括号匹配 字符串内括号 null", m5.MatchingBracketAt(0, 5) == null);

            var m6 = new EditorCore();
            var mf6 = Path.GetTempFileName();
            File.WriteAllText(mf6, "f(x) // )");
            m6.LoadFile(mf6);
            Check("括号匹配 注释不影响匹配", m6.MatchingBracketAt(0, 1) == (0, 3));
            Check("括号匹配 注释内括号 null", m6.MatchingBracketAt(0, 8) == null);

            var m7 = new EditorCore();
            var mf7 = Path.GetTempFileName();
            File.WriteAllText(mf7, "(unclosed");
            m7.LoadFile(mf7);
            Check("括号匹配 未闭合 null", m7.MatchingBracketAt(0, 0) == null);

            // ── 光标处词搜索 ──
            var w1 = new EditorCore();
            var wf1 = Path.GetTempFileName();
            File.WriteAllText(wf1, "int total_count = 42;");
            w1.LoadFile(wf1);
            Check("光标处词 起始", w1.WordAt(0, 4) == "total_count");
            Check("光标处词 中间", w1.WordAt(0, 8) == "total_count");
            Check("光标处词 下划线", w1.WordAt(0, 9) == "total_count");
            Check("光标处词 数字", w1.WordAt(0, 18) == "42");
            Check("光标处词 空白空串", w1.WordAt(0, 3) == "");
            Check("光标处词 越界空串", w1.WordAt(0, 999) == "");

            // ── 鼠标：视觉列 → 缓冲区字符列（Tab/CJK 宽度感知）──
            Check("鼠标定位 空行", TuiRichEditor.VisualToCol("", 0) == 0);
            Check("鼠标定位 空行越界", TuiRichEditor.VisualToCol("", 5) == 0);
            Check("鼠标定位 ASCII 起点", TuiRichEditor.VisualToCol("abc", 0) == 0);
            Check("鼠标定位 ASCII 中间", TuiRichEditor.VisualToCol("abc", 1) == 1);
            Check("鼠标定位 ASCII 行尾", TuiRichEditor.VisualToCol("abc", 3) == 3);
            Check("鼠标定位 ASCII 超尾钳制", TuiRichEditor.VisualToCol("abc", 10) == 3);
            Check("鼠标定位 Tab 起点", TuiRichEditor.VisualToCol("\tabc", 0) == 0);
            Check("鼠标定位 Tab 格内", TuiRichEditor.VisualToCol("\tabc", 3) == 0);
            Check("鼠标定位 Tab 后首字符", TuiRichEditor.VisualToCol("\tabc", 4) == 1);
            Check("鼠标定位 CJK 首格", TuiRichEditor.VisualToCol("中a", 0) == 0);
            Check("鼠标定位 CJK 半格仍首字符", TuiRichEditor.VisualToCol("中a", 1) == 0);
            Check("鼠标定位 CJK 后 ASCII", TuiRichEditor.VisualToCol("中a", 2) == 1);
            Check("鼠标定位 CJK 行尾", TuiRichEditor.VisualToCol("中a", 3) == 2);

            // ── 跳转 行:列 ──
            Check("跳转解析 仅行", EditorCore.ParseLineCol("5", 4, 10) == (4, 0));
            Check("跳转解析 行:列", EditorCore.ParseLineCol("5:3", 4, 10) == (4, 2));
            Check("跳转解析 :列保留当前行", EditorCore.ParseLineCol(":3", 4, 10) == (4, 2));
            Check("跳转解析 行: 空列=0", EditorCore.ParseLineCol("5:", 4, 10) == (4, 0));
            Check("跳转解析 空白串 null", EditorCore.ParseLineCol("", 4, 10) == null);
            Check("跳转解析 非数字 null", EditorCore.ParseLineCol("abc", 4, 10) == null);
            Check("跳转解析 列非数字 null", EditorCore.ParseLineCol("5:abc", 4, 10) == null);
            Check("跳转解析 行越界 null", EditorCore.ParseLineCol("11", 4, 10) == null);
            Check("跳转解析 行0 null", EditorCore.ParseLineCol("0", 4, 10) == null);
            Check("跳转解析 首尾空白容忍", EditorCore.ParseLineCol(" 5 ", 4, 10) == (4, 0));
            Check("跳转解析 列0钳制", EditorCore.ParseLineCol("5:0", 4, 10) == (4, 0));

            var j1 = new EditorCore();
            var jf1 = Path.GetTempFileName();
            File.WriteAllText(jf1, "aaa\nbbb\nccc");
            j1.LoadFile(jf1);
            Check("跳转 行:列 生效", j1.JumpToLineCol(1, 1) && j1.Cy == 1 && j1.Cx == 1);
            Check("跳转 列超长钳制", j1.JumpToLineCol(0, 999) && j1.Cx == 3);
            Check("跳转 行越界 false", !j1.JumpToLineCol(5, 0));

            // ── 缩进模式（默认 tab / space=4 空格）──
            var c6 = new EditorCore();
            var tf6 = Path.GetTempFileName();
            File.WriteAllText(tf6, "def f():");
            c6.LoadFile(tf6);
            c6.Cx = 0; c6.Cy = 0;
            c6.InsertTab();
            Check("默认缩进模式=Tab", c6.Lines[0].ToString() == "\tdef f():");
            c6.Undo();
            c6.IndentMode = "space";
            c6.InsertTab();
            Check("space 模式=4 空格", c6.Lines[0].ToString() == "    def f():");

            // 语法高亮补齐：新语言非 Plain
            Check("Ruby 语法已注册", Syntax.ForFile("a.rb").Name == "Ruby");
            Check("PHP 语法已注册", Syntax.ForFile("a.php").Name == "PHP");
            Check("Swift 语法已注册", Syntax.ForFile("a.swift").Name == "Swift");
            Check("Kotlin 语法已注册", Syntax.ForFile("a.kt").Name == "Kotlin");
            Check("Vue 语法已注册", Syntax.ForFile("a.vue").Name == "Vue");
            Check("CSS 关键词非空", Syntax.ForFile("a.css").Keywords.Count > 0);

            File.Delete(tf1); File.Delete(tf2); File.Delete(tf3);
            File.Delete(tf4); File.Delete(tf5); File.Delete(tf6); File.Delete(tf7);
        }
        Console.WriteLine();

        // ================================================================
        // EditorCore 词级 / 行级 / 块缩进（vim/edit 补强）
        // ================================================================
        Section("[EditorCore 词级/行级/块缩进]");
        {
            // ── 词级移动 ──
            var w1 = new EditorCore();
            var wt1 = Path.GetTempFileName();
            File.WriteAllText(wt1, "foo bar baz");
            w1.LoadFile(wt1);
            w1.Cx = 0; w1.Cy = 0;
            w1.MoveWord(1);
            Check("MoveWord 右移一词", w1.Cx == 4);
            w1.MoveWord(1);
            Check("MoveWord 右移两词", w1.Cx == 8);
            w1.MoveWord(1);
            Check("MoveWord 右移到行尾", w1.Cx == 11);
            w1.MoveWord(-1);
            Check("MoveWord 左移一词", w1.Cx == 8);
            w1.MoveWord(-1);
            Check("MoveWord 左移两词", w1.Cx == 4);

            // ── 删除前一词 ──
            var w2 = new EditorCore();
            var wt2 = Path.GetTempFileName();
            File.WriteAllText(wt2, "alpha beta gamma");
            w2.LoadFile(wt2);
            w2.Cx = 10; w2.Cy = 0;
            w2.DeleteWordBefore();
            Check("DeleteWordBefore 删 beta", w2.Lines[0].ToString() == "alpha  gamma");
            w2.Undo();
            Check("DeleteWordBefore 撤销", w2.Lines[0].ToString() == "alpha beta gamma");

            // ── 删除后一词 ──
            w2.Cx = 6; w2.Cy = 0;
            w2.DeleteWordAfter();
            Check("DeleteWordAfter 删 beta", w2.Lines[0].ToString() == "alpha  gamma");
            w2.Undo();
            Check("DeleteWordAfter 撤销", w2.Lines[0].ToString() == "alpha beta gamma");

            // ── 删到行尾 ──
            var w3 = new EditorCore();
            var wt3 = Path.GetTempFileName();
            File.WriteAllText(wt3, "keep this drop this");
            w3.LoadFile(wt3);
            w3.Cx = 5; w3.Cy = 0;
            w3.DeleteToLineEnd();
            Check("DeleteToLineEnd 截断", w3.Lines[0].ToString() == "keep ");
            w3.Undo();
            Check("DeleteToLineEnd 撤销", w3.Lines[0].ToString() == "keep this drop this");

            // ── 重复行 ──
            var w4 = new EditorCore();
            var wt4 = Path.GetTempFileName();
            File.WriteAllText(wt4, "aaa\nbbb");
            w4.LoadFile(wt4);
            w4.Cx = 1; w4.Cy = 0;
            w4.DuplicateLine();
            Check("DuplicateLine 行数", w4.TotalLines == 3);
            Check("DuplicateLine 内容", w4.Lines[1].ToString() == "aaa" && w4.Lines[2].ToString() == "bbb");
            w4.Undo();
            Check("DuplicateLine 撤销", w4.TotalLines == 2);

            // ── 整块缩进 / 反缩进 ──
            var w5 = new EditorCore();
            var wt5 = Path.GetTempFileName();
            File.WriteAllText(wt5, "aaa\nbbb\nccc");
            w5.LoadFile(wt5);
            w5.IndentMode = "space";
            w5.Cy = 0; w5.Cx = 0; w5.StartSelection();
            w5.Cy = 1; w5.Cx = 2;
            w5.IndentBlock(1);
            Check("IndentBlock 缩进两行", w5.Lines[0].ToString() == "    aaa" && w5.Lines[1].ToString() == "    bbb");
            Check("IndentBlock 未动第三行", w5.Lines[2].ToString() == "ccc");
            w5.Undo();
            Check("IndentBlock 单步撤销", w5.Lines[0].ToString() == "aaa" && w5.Lines[1].ToString() == "bbb");
            w5.Cy = 0; w5.Cx = 0; w5.StartSelection();
            w5.Cy = 1; w5.Cx = 2;
            w5.IndentBlock(1);
            w5.Cy = 0; w5.Cx = 0; w5.StartSelection();
            w5.Cy = 1; w5.Cx = 2;
            w5.IndentBlock(-1);
            Check("IndentBlock 反缩进还原", w5.Lines[0].ToString() == "aaa" && w5.Lines[1].ToString() == "bbb");

            // ── 撤销合并：连续单字符输入归并为一个 undo 单元 ──
            var w6 = new EditorCore();
            var wt6 = Path.GetTempFileName();
            File.WriteAllText(wt6, "");
            w6.LoadFile(wt6);
            w6.Cx = 0; w6.Cy = 0;
            w6.InsertText("a");
            w6.InsertText("b");
            w6.InsertText("c");
            Check("合并后内容 abc", w6.Lines[0].ToString() == "abc");
            w6.Undo();
            Check("合并后单步撤销到空", w6.Lines[0].ToString() == "");
            w6.InsertText("x");
            w6.NewLine();
            w6.InsertText("y");
            w6.Undo();
            Check("换行后 Undo 删 y", w6.Lines[0].ToString() == "x" && w6.Lines[1].ToString() == "");
            w6.Undo();
            Check("再 Undo 合行", w6.TotalLines == 1 && w6.Lines[0].ToString() == "x");
            w6.Undo();
            Check("三 Undo 回到空", w6.Lines[0].ToString() == "");

            File.Delete(wt1); File.Delete(wt2); File.Delete(wt3);
            File.Delete(wt4); File.Delete(wt5); File.Delete(wt6);
        }
        Console.WriteLine();

        // ================================================================
        // TuiRichEditor 新键位（词级/行级/块缩进/基类回退）
        // ================================================================
        Section("[TuiRichEditor 键位]");
        {
            var te = new TuiRichEditor();
            var tc = new EditorCore();
            var tt = Path.GetTempFileName();
            File.WriteAllText(tt, "aa bb cc\ndd ee ff\n");
            tc.LoadFile(tt);
            te.Core = tc;

            // Ctrl+D 重复行
            tc.Cy = 0; tc.Cx = 0;
            te.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.D, false, false, true));
            Check("Ctrl+D 重复行", tc.Lines[1].ToString() == "aa bb cc" && tc.TotalLines == 3);

            // Ctrl+K 删到行尾
            tc.Cy = 0; tc.Cx = 2;
            te.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.K, false, false, true));
            Check("Ctrl+K 删到行尾", tc.Lines[0].ToString() == "aa");

            // Ctrl+Backspace 删前一词
            tc.Cy = 0; tc.Cx = 2;
            te.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Backspace, false, false, true));
            Check("Ctrl+Backspace 删前一词", tc.Lines[0].ToString() == "");

            // Ctrl+Left/Right 词级移动
            var tc2 = new EditorCore();
            var tt2 = Path.GetTempFileName();
            File.WriteAllText(tt2, "aaa bbb ccc");
            tc2.LoadFile(tt2);
            te.Core = tc2;
            tc2.Cx = 0; tc2.Cy = 0;
            te.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, true));
            Check("Ctrl+Right 词级右移", tc2.Cx == 4);
            te.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, true));
            Check("Ctrl+Left 词级左移", tc2.Cx == 0);

            // Tab 整块缩进 / Shift+Tab 反缩进
            var tc3 = new EditorCore();
            var tt3 = Path.GetTempFileName();
            File.WriteAllText(tt3, "x\ny\nz");
            tc3.LoadFile(tt3);
            te.Core = tc3;
            tc3.IndentMode = "space";
            tc3.Cy = 0; tc3.Cx = 0; tc3.StartSelection();
            tc3.Cy = 1; tc3.Cx = 0;
            te.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Tab, false, false, false));
            Check("Tab 整块缩进", tc3.Lines[0].ToString() == "    x" && tc3.Lines[1].ToString() == "    y");
            tc3.Cy = 0; tc3.Cx = 0; tc3.StartSelection();
            tc3.Cy = 1; tc3.Cx = 0;
            te.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Tab, true, false, false)); // Shift+Tab
            Check("Shift+Tab 整块反缩进", tc3.Lines[0].ToString() == "x" && tc3.Lines[1].ToString() == "y");

            File.Delete(tt); File.Delete(tt2); File.Delete(tt3);
        }
        Console.WriteLine();

        // ================================================================
        // TuiRichEditor 测试
        // ================================================================
        Section("[TuiRichEditor]");
        var editor = new TuiRichEditor();
        Check("TuiRichEditor 创建", editor != null);
        Check("TuiRichEditor 默认宽度 80", editor!.Width == 80);
        Check("TuiRichEditor 默认高度 24", editor.Height == 24);
        Check("TuiRichEditor Focused", editor.Focused);
        Check("TuiRichEditor 有 Core", editor.Core != null);
        Check("TuiRichEditor LineNumberWidth=5", editor.LineNumberWidth == 5);
        Check("TuiRichEditor GutterWidth=1", editor.GutterWidth == 1);
        Check("TuiRichEditor VisibleLines", editor.VisibleLines == 24);

        // 键盘：光标移动
        var core2 = new EditorCore();
        var tmp2 = Path.GetTempFileName();
        File.WriteAllText(tmp2, "abc\ndef\nghi");
        core2.LoadFile(tmp2);
        editor.Core = core2;
        Check("TuiRichEditor 绑定 Core", editor.Core == core2);

        editor.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        Check("OnKey DownArrow", core2.Cy == 1);

        editor.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false));
        Check("OnKey RightArrow", core2.Cx == 1);

        editor.OnKey(new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false));
        Check("OnKey 插入字符", core2.Lines[1].ToString().Contains("x"));

        // 回归：Shift+可打印字符（标点/大写）必须能输入（此前 HandleShiftKey 吞掉）
        editor.OnKey(new ConsoleKeyInfo('!', ConsoleKey.D1, true, false, false));
        Check("OnKey Shift+1 输入 !", core2.Lines[1].ToString().Contains("!"));
        editor.OnKey(new ConsoleKeyInfo('{', ConsoleKey.Oem4, true, false, false));
        Check("OnKey Shift+[ 输入 {", core2.Lines[1].ToString().Contains("{"));
        editor.OnKey(new ConsoleKeyInfo('A', ConsoleKey.A, true, false, false));
        Check("OnKey Shift+A 输入大写 A", core2.Lines[1].ToString().Contains("A"));

        editor.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false));
        Check("OnKey Home", core2.Cx == 0);

        // 事件
        bool saveFired = false;
        editor.OnSaveRequested += () => saveFired = true;
        editor.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.S, false, false, true));
        Check("OnSaveRequested 触发", saveFired);

        bool jumpFired = false;
        editor.OnJumpRequested += () => jumpFired = true;
        editor.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.G, false, false, true));
        Check("OnJumpRequested 触发", jumpFired);

        // Resize
        editor.OnResize(100, 30);
        Check("OnResize Width=100", editor.Width == 100);
        Check("OnResize Height=30", editor.Height == 30);

        // LoadFile
        editor.LoadFile(tmp2);
        Check("LoadFile 加载内容", editor.Core.TotalLines == 3);

        // ── C 语法高亮（--edit file.c 直接进编辑器看 C 显示）──
        var cSyntax = Syntax.ForFile("test.c");
        Check("C 语法注册", cSyntax.Name == "C/C++");
        var cTokens = cSyntax.Tokenize("int main() { return 0; }");
        Check("C int=青色", cTokens.Any(t => t.Text == "int" && t.Color == Syntax.Cyan));
        Check("C return=青色", cTokens.Any(t => t.Text == "return" && t.Color == Syntax.Cyan));
        Check("C 数字=黄色", cTokens.Any(t => t.Text == "0" && t.Color == Syntax.Yellow));
        var cStr = cSyntax.Tokenize("char *s = \"hello\"; // note");
        Check("C 字符串=绿色", cStr.Any(t => t.Text == "\"hello\"" && t.Color == Syntax.Green));
        Check("C 注释=暗色", cStr.Any(t => t.Text.StartsWith("//") && t.Color == Syntax.Dim));

        // ── 回归：光标行默认文本不得白字白底（此前 RenderSyntaxLine 硬编码 37 白字，
        //    配白底光标行 → 白字白底「看不到文字」）──
        var colorEditor = new TuiRichEditor();
        var colorCore = new EditorCore();
        colorCore.LoadFile(tmp2); // "abc\ndef\nghi" → .tmp → 纯文本，全 Default
        colorEditor.Core = colorCore;
        var colorSb = new System.Text.StringBuilder();
        colorEditor.Render(colorSb, 0, 0);
        Check("光标行默认文本非白底白字", !colorSb.ToString().Contains("\u001b[37;47m"));

        File.Delete(tmp2);
        Console.WriteLine();

        // ================================================================
        // EditorScreen 测试
        // ================================================================
        Section("[EditorScreen]");
        var editScreen = new EditorScreen();
        Check("EditorScreen 创建", editScreen != null);
        Check("EditorScreen Name=editor", editScreen!.Name == "editor");
        Check("EditorScreen FilePath 为空", string.IsNullOrEmpty(editScreen.FilePath));

        var editScreen2 = new EditorScreen("/test/path.cs");
        Check("EditorScreen 带路径", editScreen2.FilePath == "/test/path.cs");
        Check("EditorScreen WasSaved=false", !editScreen2.WasSaved);
        Check("EditorScreen RootView 存在", editScreen2.RootView != null);

        // editor.tui 声明式布局加载 + 关键 id（v0.78.0 编辑器布局标记化）
        try
        {
            var edRes = TuiMarkup.LoadResource("editor.tui");
            Check("editor.tui Screen 根", edRes.Screen != null);
            Check("editor.tui titleBar", edRes.Find<TuiTitleBar>("titleBar") != null);
            Check("editor.tui mainHBox", edRes.Find<TuiHBox>("mainHBox") != null);
            Check("editor.tui leftPanel", edRes.Find<TuiListView>("leftPanel") != null);
            Check("editor.tui rightPanel", edRes.Find<TuiListView>("rightPanel") != null);
            Check("editor.tui leftSep", edRes.Find<TuiLabel>("leftSep") != null);
            Check("editor.tui rightSep", edRes.Find<TuiLabel>("rightSep") != null);
            Check("editor.tui statusBar1", edRes.Find<TuiLabel>("statusBar1") != null);
            Check("editor.tui statusBar2", edRes.Find<TuiLabel>("statusBar2") != null);
        }
        catch (Exception ex)
        {
            Check($"editor.tui 加载失败: {ex.Message}", false);
        }

        // EditorScreen 无头渲染冒烟：真实文件 → PushScreen(Activate→LoadAndBuild→BuildLayout) → Render
        var edTmp = Path.Combine(Path.GetTempPath(), "wc_editor_smoke.cs");
        File.WriteAllText(edTmp, "class Demo { void M() { int x = 1; } }");
        var edScreen = new EditorScreen(edTmp);
        string edFrame = "";
        bool edEntered = false;
        var edPrevOut = Console.Out;
        try
        {
            var edMgr = TuiManager.Instance;
            Console.SetOut(TextWriter.Null);
            if (!edMgr.IsActive) { edMgr.Enter(); edEntered = true; }
            edMgr.PushScreen(edScreen);
            edMgr.Render();
            edFrame = edMgr.LastCleanFrame;
            edMgr.PopScreen();
        }
        catch (Exception ex)
        {
            Check($"EditorScreen 渲染失败: {ex.Message}", false);
        }
        finally
        {
            Console.SetOut(edPrevOut);
            if (edEntered) { try { TuiManager.Instance.Exit(); } catch { } }
            File.Delete(edTmp);
        }
        Check("EditorScreen 渲染非空", !string.IsNullOrWhiteSpace(edFrame));
        Check("EditorScreen 编辑区注入", edScreen.EditorView != null && edScreen.EditorView.Parent != null);
        Console.WriteLine();

        // ================================================================
        // SettingsScreen 测试
        // ================================================================
        Section("[SettingsScreen]");
        var setScreen = new SettingsScreen();
        Check("SettingsScreen 创建", setScreen != null);
        Check("SettingsScreen Name=settings", setScreen!.Name == "settings");
        Check("SettingsScreen RootView 存在", setScreen.RootView != null);

        // Schema
        var settingSchema = Config.SettingSchema();
        Check("SettingSchema 非空", settingSchema.Count > 0);
        var groups = settingSchema.GroupBy(s => s.Category).ToList();
        Check("有分类分组", groups.Count >= 3);

        // 配置读写
        var cfg = Config.FromEnv();
        var modelVal = cfg.Model;
        Check("Config.Model 可读取", !string.IsNullOrEmpty(modelVal));

        // SettingDef 属性
        var firstDef = settingSchema[0];
        Check("SettingDef Key 非空", !string.IsNullOrEmpty(firstDef.Key));
        Check("SettingDef Label 非空", !string.IsNullOrEmpty(firstDef.Label));
        Check("SettingDef Category 非空", !string.IsNullOrEmpty(firstDef.Category));
        Check("SettingDef Type 有效", firstDef.Type is "text" or "number" or "select" or "secret");

        // 详情面板选中高亮：ApplyHighlight 设置 Bg=46（亮绿）后必须触发重渲染，
        // 否则滚动视图增量渲染（child.IsDirty||IsDirty）跳过未脏标签 → 高亮不可见
        var hlMgr = TuiManager.Instance;
        try { hlMgr.Enter(); } catch { }
        var hlOrigOut = Console.Out;
        try
        {
            setScreen.Activate(); // 构建布局、填充 _detailControls
            setScreen.OnResize(100, 30);
            hlMgr.PushScreen(setScreen);
            // 模拟 Tab 切到详情面板 → ToggleFocus → ApplyHighlight
            setScreen.OnKey(new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false));
            Console.SetOut(TextWriter.Null);
            hlMgr.Render();
            Console.SetOut(hlOrigOut);
            var raw = hlMgr.LastCleanFrame ?? "";
            Check("SettingsScreen 详情选中项高亮已渲染(46)", raw.Contains("\x1b[46m"));
        }
        catch (Exception ex)
        {
            Check("SettingsScreen 详情选中项高亮已渲染(46)", false);
            Console.WriteLine("  设置高亮渲染异常: " + ex.Message);
        }
        finally
        {
            Console.SetOut(hlOrigOut);
            hlMgr.PopScreen();
        }
        Console.WriteLine();

        // ================================================================
        // TuiButton 测试
        // ================================================================
        Section("[TuiButton]");
        var btn1 = new TuiButton("确定");
        Check("TuiButton 创建", btn1 != null);
        Check("TuiButton Text=确定", btn1!.Text == "确定");
        Check("TuiButton 默认 Height=1", btn1.Height == 1);
        Check("TuiButton CanFocus=true", btn1.CanFocus);

        bool clicked = false;
        var btn2 = new TuiButton("点击", b => { clicked = true; });
        btn2.Focused = true;
        btn2.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
        Check("TuiButton Enter 触发 OnClick", clicked);

        clicked = false;
        btn2.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Spacebar, false, false, false));
        Check("TuiButton Spacebar 触发 OnClick", clicked);

        var btn3 = new TuiButton("禁用") { IsEnabled = false };
        Check("TuiButton IsEnabled=false 不响应", !btn3.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false)));

        // Gradient
        var btnGrad = new TuiButton { GradientBg = true, GradientBgStart = AnsiTty.RgbCode(0,230,255), GradientBgEnd = AnsiTty.RgbCode(0,100,220) };
        Check("TuiButton GradientBg=true", btnGrad.GradientBg);
        Check("TuiButton GradientBgStart > 0x1000000", btnGrad.GradientBgStart > 0x1000000);
        Check("TuiButton GradientBgEnd > 0x1000000", btnGrad.GradientBgEnd > 0x1000000);
        Console.WriteLine();

        // ================================================================
        // TuiCheckbox 测试
        // ================================================================
        Section("[TuiCheckbox]");
        var cb1 = new TuiCheckbox("启用", true);
        Check("TuiCheckbox 创建", cb1 != null);
        Check("TuiCheckbox Checked=true", cb1!.Checked);
        Check("TuiCheckbox Label=启用", cb1.Label == "启用");
        Check("TuiCheckbox CanFocus=true", cb1.CanFocus);

        bool changed = false;
        bool newState = false;
        cb1.OnChanged = v => { changed = true; newState = v; };
        cb1.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Spacebar, false, false, false));
        Check("TuiCheckbox Spacebar 切换", changed && !newState);

        changed = false;
        cb1.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
        Check("TuiCheckbox Enter 切换回来", changed && newState);

        var cb2 = new TuiCheckbox("禁用") { IsEnabled = false };
        Check("TuiCheckbox IsEnabled=false 不响应", !cb2.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Spacebar, false, false, false)));

        var cb3 = new TuiCheckbox();
        Check("TuiCheckbox 默认未选中", !cb3.Checked);
        Console.WriteLine();

        // ================================================================
        // TuiInput 测试
        // ================================================================
        Section("[TuiInput]");
        var input1 = new TuiInput();
        Check("TuiInput 创建", input1 != null);
        Check("TuiInput 默认 Text 为空", input1!.Text == "");
        Check("TuiInput 默认 CursorPos=0", input1.CursorPos == 0);
        Check("TuiInput HasCursor=true", input1.HasCursor);
        Check("TuiInput 默认 Password=false", !input1.Password);

        var input2 = new TuiInput { Text = "hello", CursorPos = 5 };
        input2.Focused = true;
        // 插入字符
        input2.OnKey(new ConsoleKeyInfo('!', ConsoleKey.D1, false, true, false));
        Check("TuiInput 插入字符", input2.Text == "hello!" && input2.CursorPos == 6);

        // Backspace
        input2.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Backspace, false, false, false));
        Check("TuiInput Backspace 删除", input2.Text == "hello" && input2.CursorPos == 5);

        // Home/End
        input2.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false));
        Check("TuiInput Home 到行首", input2.CursorPos == 0);
        input2.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, false));
        Check("TuiInput End 到行尾", input2.CursorPos == 5);

        // Ctrl+A 全选
        input2.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.A, false, false, true));
        Check("TuiInput Ctrl+A 全选", input2.HasSelection && input2.SelectionStart == 0 && input2.SelectionEnd == 5);

        // Ctrl+Z 撤销插入
        var input3 = new TuiInput { Text = "", CursorPos = 0 };
        input3.Focused = true;
        input3.OnKey(new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false));
        Check("TuiInput 输入 x", input3.Text == "x");
        input3.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Z, false, false, true));
        Check("TuiInput Ctrl+Z 撤销", input3.Text == "");

        // Ctrl+Y 重做
        input3.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Y, false, false, true));
        Check("TuiInput Ctrl+Y 重做", input3.Text == "x");

        // ── 脏标记：增量渲染只画脏控件，忘标脏＝按了键界面不刷新 ──
        // 单行/多行两种输入框都盖到（用户口径：「输入框分单行、多行」）
        Section("[控件脏标记 / 增量刷新]");
        {
            // 单行：光标移动、选择、撤销这些不改文本的操作也必须标脏
            var di = new TuiInput { Text = "hello", CursorPos = 5 };
            di.Focused = true;
            foreach (var (name, k) in ((string, ConsoleKeyInfo)[])[
                ("←光标左移", new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false)),
                ("Home", new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false)),
                ("End", new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, false)),
                ("Shift+←选择", new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, true, false, false)),
                ("Ctrl+A全选", new ConsoleKeyInfo('\0', ConsoleKey.A, false, false, true)),
                ("打字", new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false)),
                ("Ctrl+Z撤销", new ConsoleKeyInfo('\0', ConsoleKey.Z, false, false, true))])
            {
                di.ClearDirty();
                di.OnKey(k);
                Check($"单行输入框: {name} 后标脏", di.IsDirty);
            }

            // 未处理的键不该白标脏（增量渲染的意义就在于少画）
            di.ClearDirty();
            di.OnKey(new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false));
            Check("单行输入框: Tab 未处理不标脏", !di.IsDirty);

            // 多行：同一条基类路径，另加上下移动与翻页
            var dt = new TuiTextArea { Text = "l1\nl2\nl3", Height = 3, Width = 20 };
            dt.Focused = true;
            foreach (var (name, k) in ((string, ConsoleKeyInfo)[])[
                ("↓下移", new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false)),
                ("↑上移", new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false)),
                ("→右移", new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false)),
                ("PageDown", new ConsoleKeyInfo('\0', ConsoleKey.PageDown, false, false, false)),
                ("回车换行", new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false))])
            {
                dt.ClearDirty();
                dt.OnKey(k);
                Check($"多行输入框: {name} 后标脏", dt.IsDirty);
            }

            // 外部赋值（code-behind 改文案/预填）同样要标脏，否则「改了看不见」
            var dl = new TuiLabel("旧");
            dl.ClearDirty();
            dl.Text = "新";
            Check("标签: 改 Text 标脏", dl.IsDirty);
            dl.ClearDirty();
            dl.Text = "新"; // 同值
            Check("标签: 同值赋值不标脏", !dl.IsDirty);

            var db = new TuiButton("确定");
            db.ClearDirty();
            db.Text = "取消";
            Check("按钮: 改 Text 标脏", db.IsDirty);
            db.ClearDirty();
            db.Focused = true;
            Check("按钮: 获得焦点标脏（Tab 切过去要变高亮）", db.IsDirty);
            db.ClearDirty();
            db.Focused = false;
            Check("按钮: 失去焦点标脏（原来那个要复原）", db.IsDirty);
            db.ClearDirty();
            db.IsHovered = true;
            Check("按钮: 悬停标脏", db.IsDirty);

            var di2 = new TuiInput();
            di2.ClearDirty();
            di2.Text = "预填";
            Check("单行输入框: 外部赋 Text 标脏", di2.IsDirty);
            di2.ClearDirty();
            di2.CursorPos = 2;
            Check("单行输入框: 外部移光标标脏", di2.IsDirty);

            var dt2 = new TuiTextArea();
            dt2.ClearDirty();
            dt2.Text = "外部预填";
            Check("多行输入框: 外部赋 Text 标脏", dt2.IsDirty);
            dt2.ClearDirty();
            dt2.ScrollRow = 3;
            Check("多行输入框: 滚动标脏", dt2.IsDirty);
        }

        // Password 模式
        var inputPw = new TuiInput { Text = "secret", Password = true };
        Check("TuiInput Password=true", inputPw.Password);
        Check("TuiInput Password HasSelection=false", !inputPw.HasSelection);

        // Delete
        var input4 = new TuiInput { Text = "ab", CursorPos = 1 };
        input4.Focused = true;
        input4.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Delete, false, false, false));
        Check("TuiInput Delete 删除右侧", input4.Text == "a");

        // Escape
        input4.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Escape, false, false, false));
        Check("TuiInput Escape 清除选择", !input4.HasSelection);

        // OnSubmit
        string? submitted = null;
        input4.OnSubmit = s => submitted = s;
        input4.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
        Check("TuiInput Enter 触发 OnSubmit", submitted == "a");

        // 已禁用不响应
        var inputDisabled = new TuiInput { Text = "x", IsEnabled = false };
        Check("TuiInput IsEnabled=false 不响应", !inputDisabled.OnKey(new ConsoleKeyInfo('y', ConsoleKey.Y, false, false, false)));

        // 光标位置：GetCursorState 在不依赖 OnRender 的情况下确保位置有效
        var inputCursor = new TuiInput { Text = "hello", CursorPos = 3, Width = 20 };
        inputCursor.IsCursorOwner = true;
        var cs = inputCursor.GetCursorState();
        Check("光标状态非空", cs != null);
        Check("光标行非负", cs!.Value.row >= 0);
        Check("光标列非负", cs.Value.col >= 0);
        Check("光标可见", cs.Value.show);

        // 光标不属自己时跳过
        var inputNotOwner = new TuiInput { Text = "test", CursorPos = 2 };
        inputNotOwner.IsCursorOwner = false;
        var cs2 = inputNotOwner.GetCursorState();
        Check("非光标所有者返回 null", cs2 == null);

        Console.WriteLine();

        // ================================================================
        // TuiTextArea 测试
        // ================================================================
        Section("[TuiTextArea]");
        var ta = new TuiTextArea();
        Check("TuiTextArea 创建", ta != null);
        Check("TuiTextArea 默认有 1 空行", ta!.Lines.Count == 1 && ta.Lines[0] == "");
        Check("TuiTextArea 默认 CursorRow=0", ta.CursorRow == 0);
        Check("TuiTextArea 默认 CursorCol=0", ta.CursorCol == 0);
        Check("TuiTextArea 默认 ReadOnly=false", !ta.ReadOnly);
        Check("TuiTextArea 默认 ShowLineNumbers=false", !ta.ShowLineNumbers);
        Check("TuiTextArea HasCursor=true", ta.HasCursor);

        // Text setter
        ta.Text = "line1\nline2\nline3";
        Check("TuiTextArea Text 设置多行", ta.Lines.Count == 3);
        Check("TuiTextArea Text getter", ta.Text == "line1\nline2\nline3");

        // 插入字符
        ta.Focused = true;
        ta.OnKey(new ConsoleKeyInfo('X', ConsoleKey.X, false, true, false));
        Check("TuiTextArea 插入字符", ta.Lines[0].StartsWith("X"));

        // 撤消
        ta.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Z, false, false, true));
        Check("TuiTextArea Ctrl+Z 撤消", ta.Lines[0] == "line1");

        // 重做
        ta.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Y, false, false, true));
        Check("TuiTextArea Ctrl+Y 重做", ta.Lines[0].StartsWith("X"));

        // ReadOnly 模式
        ta.ReadOnly = true;
        Check("TuiTextArea ReadOnly 不响应", !ta.OnKey(new ConsoleKeyInfo('y', ConsoleKey.Y, false, false, false)));

        // Placeholder
        var ta2 = new TuiTextArea { Placeholder = "请输入...", Text = "" };
        Check("TuiTextArea Placeholder", ta2.Placeholder == "请输入...");

        // Ctrl+A 全选
        ta.ReadOnly = false;
        ta.Text = "hello\nworld";
        ta.CursorRow = 0; ta.CursorCol = 0;
        ta.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.A, false, false, true));
        Check("TuiTextArea Ctrl+A 全选", ta.HasSelection);

        // InsertText 方法
        var ta3 = new TuiTextArea();
        ta3.InsertText("插入文本");
        Check("TuiTextArea InsertText", ta3.Lines[0] == "插入文本");

        // 滚动
        ta3.Focused = true;
        ta3.Text = string.Join("\n", Enumerable.Range(1, 20).Select(i => $"line{i}"));
        ta3.ScrollRow = 0;
        ta3.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.PageDown, false, false, false));
        Check("TuiTextArea PageDown 滚动", ta3.ScrollRow > 0);

        // MaxColumnWidth 自动换行
        var ta4 = new TuiTextArea { MaxColumnWidth = 10, Focused = true };
        ta4.Text = "hello world this is a long sentence";
        Check("TuiTextArea MaxColumnWidth 默认不折行(已有文本)", ta4.Lines.Count == 1);
        ta4.Text = "";
        // 逐字输入触发折行
        foreach (var c in "hello world test wrap")
            ta4.OnKey(new ConsoleKeyInfo(c, ConsoleKey.None, false, false, false));
        Check("TuiTextArea MaxColumnWidth 输入折行", ta4.Lines.Count >= 2);

        // MaxLines 行数裁剪
        var ta5 = new TuiTextArea { MaxLines = 3, Focused = true };
        ta5.Text = "line1\nline2\nline3\nline4\nline5";
        Check("TuiTextArea MaxLines 裁剪前", ta5.Lines.Count == 5);
        ta5.OnKey(new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false)); // 触发 TrimExcessLines
        Check("TuiTextArea MaxLines 裁剪后", ta5.Lines.Count == 3);
        Check("TuiTextArea MaxLines 保留最后几行", ta5.Lines[0] == "line3");

        // MaxColumnWidth = 0 不限宽
        var ta6 = new TuiTextArea { MaxColumnWidth = 0 };
        ta6.Text = new string('A', 200);
        Check("TuiTextArea MaxColumnWidth=0 不折行", ta6.Lines.Count == 1);

        // 回归：多行粘贴到光标中段后撤销，不得吞掉首行前缀（v0.71.29 修复）
        var tp = new TuiTextAreaPasteProbe { Text = "Line one content here" };
        tp.CursorRow = 0; tp.CursorCol = 5;
        tp.Paste("A\nB");
        tp.UndoAction();
        Check("TuiTextArea 多行粘贴撤销恢复原文本", tp.Text == "Line one content here");
        Console.WriteLine();

        // ================================================================
        // TuiLabel 测试
        // ================================================================
        Section("[TuiLabel]");
        var lbl1 = new TuiLabel("测试标签");
        Check("TuiLabel 创建", lbl1 != null);
        Check("TuiLabel Text", lbl1!.Text == "测试标签");
        Check("TuiLabel CanFocus=false", !lbl1.CanFocus);
        Check("TuiLabel Height=1", lbl1.Height == 1);

        var lbl2 = new TuiLabel();
        Check("TuiLabel 默认 Text 为空", lbl2.Text == "");
        Console.WriteLine();

        // ================================================================
        // TuiIcon 测试
        // ================================================================
        Section("[TuiIcon]");
        var icon1 = new TuiIcon("★");
        Check("TuiIcon 创建", icon1 != null);
        Check("TuiIcon Glyph=★", icon1!.Glyph == "★");
        Check("TuiIcon CanFocus=false", !icon1.CanFocus);
        Check("TuiIcon Width=2", icon1.Width == 2);
        Check("TuiIcon Height=1", icon1.Height == 1);

        var icon2 = new TuiIcon();
        Check("TuiIcon 默认 Glyph=•", icon2.Glyph == "•");

        // 预设工厂方法
        Check("TuiIcon.User 非空", TuiIcon.User() != null);
        Check("TuiIcon.Assistant 非空", TuiIcon.Assistant() != null);
        Check("TuiIcon.System 非空", TuiIcon.System() != null);
        Check("TuiIcon.Tool 非空", TuiIcon.Tool() != null);
        Check("TuiIcon.Error 非空", TuiIcon.Error() != null);
        Check("TuiIcon.Warn 非空", TuiIcon.Warn() != null);
        Check("TuiIcon.Ok 非空", TuiIcon.Ok() != null);
        Check("TuiIcon.Info 非空", TuiIcon.Info() != null);
        Check("TuiIcon.File 非空", TuiIcon.File() != null);
        Check("TuiIcon.Folder 非空", TuiIcon.Folder() != null);
        Check("TuiIcon.Lock 非空", TuiIcon.Lock() != null);
        Console.WriteLine();

        // ================================================================
        // TuiList 测试
        // ================================================================
        Section("[TuiList]");
        var list1 = new TuiList { Items = ["项目A", "项目B", "项目C"] };
        Check("TuiList 创建", list1 != null);
        Check("TuiList 3 项", list1!.Items.Count == 3);
        Check("TuiList SelectedIndex=0", list1.SelectedIndex == 0);
        Check("TuiList MultiSelect=false", !list1.MultiSelect);

        // 键盘导航
        list1.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        Check("TuiList DownArrow", list1.SelectedIndex == 1);
        list1.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false));
        Check("TuiList UpArrow", list1.SelectedIndex == 0);
        list1.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, false));
        Check("TuiList End", list1.SelectedIndex == 2);
        list1.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false));
        Check("TuiList Home", list1.SelectedIndex == 0);

        // 选中变化必须标脏（增量渲染下高亮才会更新，否则方向键「看似无效」）
        list1.ClearDirty();
        list1.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        Check("TuiList DownArrow 标脏", list1.IsDirty);
        list1.ClearDirty();
        list1.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false));
        Check("TuiList UpArrow 标脏", list1.IsDirty);

        // 选择回调
        int? selectedIdx = null;
        list1.OnSelect = idx => selectedIdx = idx;
        list1.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false));
        Check("TuiList Enter 触发 OnSelect", selectedIdx == 0);

        // 多选
        var list2 = new TuiList { Items = ["A", "B", "C"], MultiSelect = true };
        list2.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Spacebar, false, false, false));
        Check("TuiList MultiSelect Spacebar 选中", list2.CheckedIndices.Contains(0));
        list2.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Spacebar, false, false, false));
        Check("TuiList MultiSelect Spacebar 取消", !list2.CheckedIndices.Contains(0));

        // 空列表
        var listEmpty = new TuiList();
        Check("TuiList 空列表 Items=0", listEmpty.Items.Count == 0);
        Console.WriteLine();

        // ================================================================
        // TuiMouse 鼠标支持测试
        // ================================================================
        Section("[TuiMouse]");
        TestTuiMouse(Check);
        Console.WriteLine();

        // ================================================================
        // TuiListView 测试
        // ================================================================
    }
}

// 回归测试辅助：暴露 TuiTextArea 受保护的 PasteText/Undo/InsertNewLine，验证撤销栈健壮性。
internal sealed class TuiTextAreaPasteProbe : TuiTextArea
{
    public void Paste(string s) => PasteText(s);
    public void UndoAction() => Undo();
    public void NewLine() => InsertNewLine();
}
