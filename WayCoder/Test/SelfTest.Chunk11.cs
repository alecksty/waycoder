using System.Text;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Edit;

namespace WayCoder;

public static partial class SelfTest
{
    private static void TestChunk11(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        TestUiLint(Section, Check, Fail);
        TestTableList(Section, Check, Fail);
        TestTuiSpace(Section, Check, Fail);
        TestTextAreaSyntax(Section, Check, Fail);
        TestWindowButtonEnter(Section, Check, Fail);
    }

    // ── 窗口：焦点按钮按 Enter 直接触发按钮，不被窗口级 Enter 快捷键抢走 ──
    private static void TestWindowButtonEnter(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[窗口按钮 Enter]");

        var w = new WayCoder.UI.TUI.Base.TuiWindow();
        var btn = new TuiButton("接受(Y)");
        int btnClicks = 0, winEnter = 0;
        btn.OnClick = _ => btnClicks++;
        w.RootView.Add(btn);
        btn.Focused = true;
        w.RegisterShortcut(ConsoleKey.Enter, () => winEnter++);

        // 焦点在按钮上 → Enter 触发按钮 OnClick，窗口 Enter 快捷键不触发
        w.OnKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
        Check("窗口: 焦点按钮 Enter 触发按钮", btnClicks == 1);
        Check("窗口: 焦点按钮 Enter 不触发窗口快捷键", winEnter == 0);

        // 焦点离开按钮 → Enter 走窗口快捷键
        btn.Focused = false;
        w.OnKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
        Check("窗口: 非按钮焦点时 Enter 走窗口快捷键", winEnter == 1 && btnClicks == 1);
    }

    // ── TuiTextArea 代码语法高亮：Syntax.Detect 内容检测 + tokenize 多色渲染 ──
    private static void TestTextAreaSyntax(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[TuiTextArea 高亮]");

        Check("检测: C# 命中", Syntax.Detect("namespace Demo { public class X { } }") != null);
        Check("检测: Python 命中", Syntax.Detect("import os\ndef main():\n    print(1)") != null);
        Check("检测: JS 命中", Syntax.Detect("function f() { const x = 1; }") != null);
        Check("检测: JSON 命中", Syntax.Detect("{\"name\": \"a\", \"id\": 1}") != null);
        Check("检测: SQL 命中", Syntax.Detect("SELECT * FROM users WHERE id = 1") != null);
        Check("检测: 普通对话不命中", Syntax.Detect("今天天气不错,我们聊一下项目") == null);
        Check("检测: 空串不命中", Syntax.Detect("") == null);

        // 渲染多色：C# 代码同时产出关键字青色(36)与字符串绿色(32)ANSI 前景码
        var ta = new TuiTextArea
        {
            Text = "namespace Demo { public class X { string s = \"hi\"; } }",
            Width = 50,
            Height = 3,
            SyntaxHighlight = true,
        };
        var sb = new StringBuilder();
        ta.Render(sb, 0, 0);
        var raw = sb.ToString();
        Check("输入框高亮: 关键字青色码 \\x1b[36", raw.Contains("\x1b[36"));
        Check("输入框高亮: 字符串绿色码 \\x1b[32", raw.Contains("\x1b[32"));

        // 关闭高亮 → 单色（无 token 色码）
        ta.SyntaxHighlight = false;
        var sb2 = new StringBuilder();
        ta.Render(sb2, 0, 0);
        Check("输入框高亮: 关闭后无 token 色码",
            !sb2.ToString().Contains("\x1b[36") && !sb2.ToString().Contains("\x1b[32"));
    }

    // ── TuiSpace：空白占位控件（布局占位、什么都不画、不响应输入）──
    private static void TestTuiSpace(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[TuiSpace]");

        var sp = new TuiSpace();
        Check("TuiSpace: 默认占 1×1", sp.Height == 1 && sp.Width == 1);

        sp.Height = 3; sp.Width = 10;
        var sb = new StringBuilder();
        sp.Render(sb, 0, 0);
        Check("TuiSpace: 渲染零输出（占位不显示）", sb.Length == 0);

        Check("TuiSpace: 不响应键盘",
            !sp.OnKey(new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false)));
        Check("TuiSpace: 不参与焦点遍历", !sp.CanFocus);

        // 标记里 <Space height="2"> 解析成 TuiSpace 并占两行高
        var res = WayCoder.UI.TUI.TuiMarkup.Load(
            """
            <VBox width="20">
              <Space id="sp" height="2" />
              <Label id="a" text="A" height="1" />
            </VBox>
            """);
        var space = res.Find<TuiSpace>("sp");
        Check("TuiSpace: 标记解析为 TuiSpace 且占两行", space != null && space.Height == 2);

        // 布局占位：VBox 里 Space 占 2 行，后续 Label 被推到第 2 行（不再贴住前一控件）
        var box = new WayCoder.UI.TUI.Base.TuiVBox { Width = 20 };
        var sp2 = new TuiSpace { Height = 2 };
        var lbl2 = new TuiLabel("A") { Height = 1 };
        box.Add(sp2);
        box.Add(lbl2);
        box.Layout();
        Check("TuiSpace: 占位后后续控件下移两行", sp2.Y == 0 && lbl2.Y == 2);
    }

    // ── UI Lint：UI 层禁止硬编码屏幕写入（直接 Console.* / 裸 ANSI 转义字面量）──
    private static void TestUiLint(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[UI Lint]");

        // 定位项目目录（含 UI/ 子目录），兼容 dotnet run 从仓库根或 WayCoder/ 两种 cwd
        var cwd = Directory.GetCurrentDirectory();
        string? projRoot = null;
        foreach (var cand in new[] { cwd, Path.Combine(cwd, "WayCoder") })
        {
            if (Directory.Exists(Path.Combine(cand, "UI"))) { projRoot = cand; break; }
        }
        Check("Lint: 定位到含 UI 目录的项目根", projRoot != null);
        if (projRoot == null) return;

        // 现存违规文件白名单：阶段三/四逐批迁移到控件库后收紧到空。分三类：
        // 1) 输入层路由（Console.ReadKey 拦截）阶段四目标：InputManager/UxHelper/ChatScreen(+Dialogs)；
        // 2) 终端 ANSI 底层原语（AnsiString/AnsiTty/RenderBuffer/Terminal）——本就负责发射转义序列；
        // 3) 非 TUI 分支：TuiTable 的 Console.Write 回退、ThemeVerify 诊断工具、CLI 参数层 BuiltinArgs。
        var whitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // 输入层路由（阶段四待迁移到 Tty 层）
            "InputManager.cs", "UxHelper.cs", "ChatScreen.cs", "ChatScreen.Dialogs.cs",
            // TuiTable 非 TUI 分支 Console.Write 回退
            "TuiTable.cs",
            // ThemeVerify 是 --theme-verify 诊断工具，直接 stdout 打印对比表，非 TUI 界面
            "ThemeVerify.cs",
            // 终端 ANSI 底层原语：本就负责发射 \x1b 转义序列，属 Tty 层而非界面层
            "AnsiString.cs", "AnsiTty.cs", "RenderBuffer.cs", "Terminal.cs",
            // 终端宽度探针 + raw 模式：直接操作 Console.Out/\x1b + libc termios，属终端底层
            "TerminalWidthProbe.cs", "TerminalRawMode.cs",
            // CLI 参数解析层（--keypad/--test/--model 等），一次性模式直接打印 stdout，非全屏界面。
            // BuiltinArgs.cs 已按关注点拆分为 7 个文件，逐一白名单。
            "BuiltinArgs.cs", "ModelArgs.cs", "McpCli.cs", "DebugArgs.cs", "CachePurger.cs", "UtilityArgs.cs",
            "KbCli.cs",
        };

        // 禁用模式：直接写 Console / 裸 ANSI 转义字面量（Terminal/ 底层与 Test/ 不在 UI/ 内，天然排除）
        var forbidden = new[]
        {
            "Console.Write", "Console.ReadLine", "Console.ReadKey", "Console.KeyAvailable",
            "Console.SetCursorPosition", "Console.CursorLeft", "Console.CursorTop",
            "Console.CursorVisible", "Console.Clear", "Console.OpenStandardOutput",
            "Console.Out.Write", "Console.Out.WriteLine",
            "Console.ForegroundColor", "Console.BackgroundColor", "Console.ResetColor",
            "\\x1b", "\\u001b", "\\x1B", "\\u001B", "\\033",
        };

        var uiDir = Path.Combine(projRoot, "UI");
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(uiDir, "*.cs", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);
            if (whitelist.Contains(name)) continue;

            // Web 层（UI/WEB）走 HTTP/SSE，不写终端 Console，不适用「禁止硬编码 Console/ANSI」约束；
            // 其内嵌前端 JS/样例 Markdown 天然含 \x1b / Console.WriteLine 等字面量，跳过。
            var rel = Path.GetRelativePath(uiDir, file);
            if (rel.StartsWith("WEB" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;

            bool bad = false;
            foreach (var rawLine in File.ReadLines(file))
            {
                var line = rawLine.TrimStart();
                if (line.StartsWith("//")) continue;   // 跳过纯注释行，避免注释举例误报
                foreach (var p in forbidden)
                {
                    if (line.Contains(p, StringComparison.Ordinal)) { bad = true; break; }
                }
                if (bad) break;
            }
            if (bad) offenders.Add(Path.GetFileNameWithoutExtension(file));
        }

        Check($"Lint: 非白名单 UI 文件无硬编码写入（违规 {offenders.Count} 个）", offenders.Count == 0);
        foreach (var o in offenders)
            Fail($"  Lint 违规: {o}");
    }

    // ── TuiTableList：可选中多列表格列表控件 ──
    private static void TestTableList(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[TuiTableList]");

        // ── 列定义与行数据 ──
        var tbl = new TuiTableList();
        tbl.AddColumn("模型", 20);
        tbl.AddColumn("供应商", 12);
        tbl.AddColumn("上下文", 10);
        tbl.AddRow("deepseek-v4-pro", "deepseek", "1M");
        tbl.AddRow("gpt-5.4", "openai", "256K");
        tbl.AddRow("claude-opus-5", "anthropic", "200K");

        Check("TableList: 列数 = 3", tbl.ColumnCount == 3);
        Check("TableList: 行数 = 3", tbl.RowCount == 3);
        Check("TableList: 默认选中第 0 行", tbl.SelectedIndex == 0);
        Check("TableList: 单元格读取", tbl.GetCell(0, 0) == "deepseek-v4-pro" && tbl.GetCell(2, 2) == "200K");

        // ── 选中与键盘导航边界 ──
        int? selected = null;
        tbl.OnSelect = idx => selected = idx;
        tbl.SelectedIndex = 2;
        Check("TableList: 选中第 2 行", tbl.SelectedIndex == 2);
        tbl.SelectedIndex = 1;
        tbl.SelectNext();
        Check("TableList: SelectNext 到末尾", tbl.SelectedIndex == 2);
        tbl.SelectNext();
        Check("TableList: SelectNext 越界钳制", tbl.SelectedIndex == 2);
        tbl.SelectPrev();
        tbl.SelectPrev();
        tbl.SelectPrev();
        Check("TableList: SelectPrev 越界钳制到 0", tbl.SelectedIndex == 0);

        // 激活选中行触发 OnSelect
        tbl.SelectedIndex = 1;
        tbl.ActivateSelected();
        Check("TableList: ActivateSelected 触发 OnSelect", selected == 1);

        // ── 滚动偏移：确保选中项可见 ──
        tbl.ShowHeader = false;
        tbl.ClearRows();
        for (int i = 0; i < 10; i++) tbl.AddRow($"row-{i}", "x", "y");
        tbl.Height = 2;   // 仅显示 2 行
        tbl.SelectedIndex = 9;
        tbl.EnsureSelectedVisible();
        Check("TableList: 滚动保证选中可见", tbl.ScrollOffset <= 9 && tbl.ScrollOffset + 2 > 9);
        Check("TableList: 滚动偏移非负", tbl.ScrollOffset >= 0);

        // ── 列头渲染字符串（含分隔线）──
        var header = tbl.RenderHeader();
        Check("TableList: 列头含列名", header.Contains("模型") && header.Contains("供应商") && header.Contains("上下文"));
        Check("TableList: 列头含分隔线", header.Contains("─"));

        // ── 组头行：不可选中、导航跳过 ──
        tbl.ClearRows();
        tbl.AddGroupHeader("deepseek");
        tbl.AddRow("m1", "x", "y");
        tbl.AddGroupHeader("openai");
        tbl.AddRow("m2", "x", "y");
        Check("TableList: 组头行标记", tbl.IsGroupRow(0) && !tbl.IsGroupRow(1));
        tbl.SelectedIndex = 0; tbl.SelectNext();
        Check("TableList: 导航跳过组头", tbl.SelectedIndex == 1);
        tbl.SelectedIndex = 3; tbl.SelectPrev();
        Check("TableList: 导航向上跳过组头", tbl.SelectedIndex == 1);
        Check("TableList: NextSelectable 跳到首个数据行", tbl.NextSelectable(0) == 1);

        // ── 列宽等比铺开（StretchColumns）──
        // 固定列宽在宽窗口里把内容全挤在左边，右侧一大片空白；开了之后按声明比例放大铺满
        var wide = new TuiTableList();
        wide.AddColumn("A", 2);
        wide.AddColumn("B", 24);
        wide.AddColumn("C", 12);   // 声明合计 38

        var fixedW = wide.EffectiveWidths(100);
        Check("TableList: 不开 stretch 时列宽保持声明值",
            fixedW[0] == 2 && fixedW[1] == 24 && fixedW[2] == 12);

        wide.StretchColumns = true;
        var strW = wide.EffectiveWidths(100);
        int strSum = strW[0] + strW[1] + strW[2];
        Check("TableList: stretch 后各列之和铺满整宽（右侧不留白）", strSum == 100);
        Check("TableList: stretch 后每列都变宽", strW[0] >= 2 && strW[1] > 24 && strW[2] > 12);
        Check("TableList: stretch 保持列间比例（B 仍最宽、约为 C 的两倍）",
            strW[1] > strW[2] && strW[2] > strW[0] && Math.Abs(strW[1] - strW[2] * 2) <= 3);

        // 窄于声明宽度时不缩（缩了会把内容截没，交给 TruncateByWidth 逐格处理）
        var narrow = wide.EffectiveWidths(20);
        Check("TableList: 控件比声明列宽还窄时不等比缩",
            narrow[0] == 2 && narrow[1] == 24 && narrow[2] == 12);

        // 列头分隔线跟着有效列宽走，不能还按声明宽度画（会短一截对不齐）
        wide.Width = 101;   // DataWidth = 100（右侧 1 列给滚动条）
        var wideHeader = wide.RenderHeader();
        var sepLine = wideHeader.Split('\n')[1];
        Check("TableList: stretch 后分隔线长度跟随有效列宽", sepLine.Length == 100);
    }
}
