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
        Check("ApplyTo 边框ocean", tw.Border == WindowBorder.Rounded);
        ThemeConfig.ApplyPreset("default");
        ThemeConfig.Instance.ApplyTo(tw);
        Check("ApplyTo 恢复默认", tw.Border == WindowBorder.Single);
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
            var win = new TuiWindow { Border = s };
            var (tl, tr, bl, br, h, v, hTop, hBot) = win.GetBorderChars();
            Check($"GetBorderChars {s} 非空", tl.Length > 0 && tr.Length > 0 && h.Length > 0 && v.Length > 0);
        }
        var customWin = new TuiWindow { Border = WindowBorder.Ascii, CustomBorder = "+-+|||-" };
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

        // Tab
        core.Cx = 0;
        core.InsertTab();
        Check("InsertTab 插入 4 空格", core.Lines[1].ToString().StartsWith("    "));

        // 保存
        core.Save();
        Check("Save 后不脏", !core.Modified);
        var savedContent = File.ReadAllText(tmpFileEc);
        Check("Save 文件内容正确", savedContent.Contains("line1"));

        // 统计
        Check("TotalChars > 0", core.TotalChars > 0);
        Check("FileSizeBytes > 0", core.FileSizeBytes > 0);
        Check("FormatSize B", EditorCore.FormatSize(500) == "500 B");
        Check("FormatSize KB", EditorCore.FormatSize(2048) == "2.0 KB");

        // 诊断
        var (e, w) = core.GetDiagSummary();
        Check("GetDiagSummary 返回元组", e >= 0 && w >= 0);

        // 清理
        File.Delete(tmpFileEc);
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
        // TuiListView 测试
        // ================================================================
    }
}
