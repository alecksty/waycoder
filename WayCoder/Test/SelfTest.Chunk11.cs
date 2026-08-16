using WayCoder.UI.Tui.Controls;

namespace WayCoder;

public static partial class SelfTest
{
    private static void TestChunk11(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        TestUiLint(Section, Check, Fail);
        TestTableList(Section, Check, Fail);
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
            // CLI 参数解析层（--keypad/--test/--model 等），一次性模式直接打印 stdout，非全屏界面
            "BuiltinArgs.cs",
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
    }
}
