using System.Text;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.TUI.Custom;
using WayCoder.UI.Tui.Screens;

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
            DumpWindow("古诗单选 /ask", s => TuiDemo.ShowAskSingleDemo(s));
            DumpWindow("长诗单选 /asklong", s => TuiDemo.ShowAskLongDemo(s));
            DumpWindow("古诗多选 /askmulti", s => TuiDemo.ShowAskMultiDemo(s));
            DumpWindow("12选项多选 /askmany", s => TuiDemo.ShowAskManyDemo(s));
            DumpDiffWindow("代码对比大 diff /diff", SampleBigDiffOld(), SampleBigDiffNew());
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

    // ── Diff 对话框（非阻塞：直接构建窗口渲染，不跑 Show 的等待循环）──

    static void DumpDiffWindow(string name, string oldContent, string newContent)
    {
        var mgr = TuiManager.Instance;
        var screen = new ChatScreen();
        mgr.PushScreen(screen);
        try
        {
            var hunks = DiffPreview.BuildHunks(oldContent, newContent);
            var win = DiffPreview.BuildDiffWindow(hunks, "BigFile.cs", screen, (_, _) => { });
            screen.ShowWindow(win);
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

    /// <summary>
    /// 把 ANSI 输出渲染成字符网格（固定 rows 行，末尾空行裁掉）。
    ///
    /// 直接复用 <see cref="Keypad.FrameBuffer"/>（按键测试用的同一套 ANSI 屏幕模拟器）——
    /// 此前这里是**第二份手写解析**：只处理 CUP/\r\n\t/字符，而它的输出拼接逻辑
    /// （跳宽字符延续格 + TrimEnd + 裁末尾空行）与 FrameBuffer.Dump 逐字相同。
    /// 两套模拟器意味着「审计说渲染对了、按键测试说错了」这类自相矛盾的结论，
    /// 而两者本该对同一份终端输出给出同一张网格。
    /// </summary>
    internal static List<string> AnsiToGrid(string ansi, int rows, int cols)
    {
        var fb = new Keypad.FrameBuffer(rows, cols);
        fb.Apply(ansi);
        return fb.Dump();
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

    /// <summary>生成一个多行的大 diff（旧文件），用于验证滚动条与滚动。</summary>
    static string SampleBigDiffOld()
    {
        var sb = new StringBuilder();
        sb.AppendLine("public class BigFile");
        sb.AppendLine("{");
        for (int i = 0; i < 40; i++)
            sb.AppendLine($"    public int Field{i} {{ get; set; }}  // 旧字段 {i}");
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>生成一个多行的大 diff（新文件），与旧文件逐行差异以产生多个 hunk。</summary>
    static string SampleBigDiffNew()
    {
        var sb = new StringBuilder();
        sb.AppendLine("public class BigFile : BaseClass");
        sb.AppendLine("{");
        for (int i = 0; i < 40; i++)
            sb.AppendLine($"    public string Field{i} {{ get; set; }} = \"新值{i}\";  // 改类型并赋值 {i}");
        sb.AppendLine("    public void NewMethod() => Console.WriteLine(\"added\");");
        sb.AppendLine("}");
        return sb.ToString();
    }
}
