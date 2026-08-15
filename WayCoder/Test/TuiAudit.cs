using System.Text;
using WayCoder.Terminal;
using WayCoder.Tools;
using WayCoder.UI;
using WayCoder.UI.TuiScreens;

namespace WayCoder;

/// <summary>
/// TUI 渲染审计 —— 把每个对话框/控件渲染成纯文本帧，便于肉眼检查排版问题。
/// 运行：waycoder --tui-audit &lt; /dev/null &gt; audit.txt
///
/// 说明：
///   - TuiWindow 系对话框走 TuiManager.LastCleanFrame（剥离 ANSI 即得纯文本帧）。
///   - 全屏 ANSI 对话框（ModelPicker 等）通过 stdin 重定向使 Console.ReadKey
///     立即抛 InvalidOperationException，从而只捕获首帧，再解释 CursorPos 成网格。
/// </summary>
public static class TuiAudit
{
    public static void Run()
    {
        try { Console.OutputEncoding = Encoding.UTF8; } catch { }

        // ── 一、TuiWindow 系对话框（渲染进 LastCleanFrame）──
        var mgr = TuiManager.Instance;
        try { mgr.Enter(); } catch { }
        try
        {
            DumpWindow("权限确认 F1", s => TuiDemo.ShowPermissionDemo(s));
            DumpWindow("输入对话框 F3", s => TuiDemo.ShowInputDemo(s));
            DumpWindow("列表选择 F4", s => TuiDemo.ShowListDemo(s));
            DumpWindow("确认框 F5", s => TuiDemo.ShowConfirmDemo(s));
            DumpWindow("短菜单 F6", s => TuiDemo.ShowShortMenuDemo(s));
            DumpWindow("长滚动菜单 F7", s => TuiDemo.ShowLongMenuDemo(s));
            DumpWindow("右键菜单 F8", s => TuiDemo.ShowContextMenuDemo(s));
            DumpWindow("树形视图 F10", s => TuiDemo.ShowTreeDemo(s));
            DumpWindow("控件合集 F11", s => TuiDemo.ShowControlsDemo(s));
            DumpWindow("面板布局 F12", s => TuiDemo.ShowPanelDemo(s));
            DumpWindow("按钮组+滚动条 /b", s => TuiDemo.ShowButtonGroupDemo(s));
            DumpWindow("多选对话框 /multi", s => TuiDemo.ShowMultiSelectDemo(s));
        }
        finally
        {
            try { mgr.Exit(); } catch { }
        }

        // ── 二、全屏 ANSI 对话框（CursorPos 定位，解释成网格）──
        DumpFullScreen("模型选择器 /m", () => { ModelPicker.Show(); });
        DumpFullScreen("会话管理器 /s", () => { SessionPicker.Show(); });
        DumpFullScreen("推理深度 /r", () => { ReasoningPicker.Show(currentLevel: "", modelName: "deepseek-v4-pro"); });
        DumpFullScreen("命令面板 /c", () => { CommandPalette.Show(SampleCommands()); });
        DumpFullScreen("文件选择器 /f", () => { FilePicker.Show(Environment.CurrentDirectory, null, "选择文件"); });
        DumpFullScreen("代码对比 /diff", () => { DiffPreview.Show(SampleOld(), SampleNew(), "HealthController.cs"); });
    }

    // ── TuiWindow 对话框 ──

    static void DumpWindow(string name, Action<ChatScreen> setup)
    {
        var mgr = TuiManager.Instance;
        var screen = new ChatScreen();
        mgr.PushScreen(screen);
        try
        {
            setup(screen);
            mgr.Render();
            PrintSection(name, AnsiToGrid(mgr.LastCleanFrame, 30, 100));
        }
        catch (Exception ex)
        {
            PrintSection(name, new[] { "(渲染出错: " + ex.GetType().Name + ": " + ex.Message + ")" });
        }
        finally
        {
            try { mgr.PopScreen(); } catch { }
        }
    }

    // ── 全屏 ANSI 对话框 ──

    static void DumpFullScreen(string name, Action show)
    {
        var sw = new StringWriter();
        var orig = Console.Out;
        Console.SetOut(sw);
        try
        {
            show();
        }
        catch (InvalidOperationException)
        {
            // Console.ReadKey 在 stdin 重定向时抛此异常 —— 正好捕获到首帧
        }
        catch (Exception ex)
        {
            Console.SetOut(orig);
            PrintSection(name, new[] { "(运行出错: " + ex.GetType().Name + ": " + ex.Message + ")" });
            return;
        }
        finally
        {
            Console.SetOut(orig);
        }
        PrintSection(name, AnsiToGrid(sw.ToString(), 40, 120));
    }

    // ── ANSI 帧解释成文本网格 ──

    /// <summary>把 CursorPos/SGR 定位的原始 ANSI 输出解释成 rows×cols 文本网格。</summary>
    internal static List<string> AnsiToGrid(string ansi, int rows, int cols)
    {
        var cell = new string[rows][];
        var cont = new bool[rows][];
        for (int r = 0; r < rows; r++)
        {
            cell[r] = new string[cols];
            cont[r] = new bool[cols];
            for (int c = 0; c < cols; c++) cell[r][c] = " ";
        }

        int curR = 0, curC = 0, i = 0, len = ansi.Length;
        while (i < len)
        {
            char ch = ansi[i];
            if (ch == '\x1b' && i + 1 < len && ansi[i + 1] == '[')
            {
                int j = i + 2;
                while (j < len && !(ansi[j] >= '@' && ansi[j] <= '~')) j++;
                if (j >= len) break;
                char final = ansi[j];
                string param = ansi.Substring(i + 2, j - (i + 2));
                i = j + 1;
                if (final == 'H' || final == 'f') // CUP / HVP：行;列，1-based
                {
                    int row = 1, col = 1;
                    var p = param.Split(';');
                    if (p.Length >= 1 && int.TryParse(p[0], out var rr)) row = rr;
                    if (p.Length >= 2 && int.TryParse(p[1], out var cc)) col = cc;
                    curR = Math.Clamp(row - 1, 0, rows - 1);
                    curC = Math.Clamp(col - 1, 0, cols - 1);
                }
                continue;
            }
            if (ch == '\r') { curC = 0; i++; continue; }
            if (ch == '\n') { curR++; curC = 0; i++; continue; }
            if (ch == '\t') { curC += 4 - (curC % 4); i++; continue; }

            var rune = Rune.GetRuneAt(ansi, i);
            int w = AnsiString.CharWidth(rune);
            string s = rune.ToString();
            i += rune.Utf16SequenceLength;

            if (w == 0)
            {
                // 零宽字符（组合标记/变体选择器 FE0F 等）：不占列，跳过，避免列错位
                continue;
            }

            if (curR >= 0 && curR < rows && curC >= 0 && curC < cols)
            {
                cell[curR][curC] = s;
                if (w == 2 && curC + 1 < cols) cont[curR][curC + 1] = true;
            }
            curC += w;
        }

        var lines = new List<string>();
        for (int r = 0; r < rows; r++)
        {
            var sb = new StringBuilder();
            for (int c = 0; c < cols; c++)
            {
                if (cont[r][c]) continue; // 宽字符延续格
                sb.Append(cell[r][c]);
            }
            lines.Add(sb.ToString().TrimEnd());
        }
        while (lines.Count > 0 && lines[^1].Length == 0) lines.RemoveAt(lines.Count - 1);
        return lines;
    }

    static void PrintSection(string name, IEnumerable<string> lines)
    {
        Console.WriteLine();
        Console.WriteLine("===== [" + name + "] =====");
        foreach (var l in lines) Console.WriteLine(l);
        Console.WriteLine("===== [/" + name + "] =====");
    }

    // ── 示例数据 ──

    static List<CommandPalette.Command> SampleCommands() => new()
    {
        new("model", "🤖 切换模型", "模型", "Ctrl+M", "打开模型选择对话框", () => { }),
        new("session", "📂 管理会话", "会话", "Ctrl+S", "打开会话管理器", () => { }),
        new("file", "📁 打开文件", "文件", "Ctrl+O", "选择并打开文件", () => { }),
        new("diff", "📊 查看差异", "工具", "", "显示当前变更的 diff", () => { }),
        new("quit", "🚪 退出", "系统", "Ctrl+Q", "退出 WayCoder", () => { }),
        new("longlabel", "🔧 这条命令标签故意写得特别长用于验证溢出截断", "测试", "Ctrl+Shift+L",
            "这是一条同样非常长的描述文本，用来验证在较窄终端下标签、描述与快捷键三者都能正确截断而不撑破边框。", () => { }),
    };

    static string SampleOld() =>
        "public class HealthController\n{\n    public string Get()\n    {\n        return \"old\";\n    }\n}\n";

    static string SampleNew() =>
        "public class HealthController : ControllerBase\n{\n    public string Get()\n    {\n        return \"new\";\n    }\n\n    public string Ready() => \"ready\";\n}\n";
}
