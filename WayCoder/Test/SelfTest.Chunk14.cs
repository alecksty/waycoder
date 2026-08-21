using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Screens;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>
    /// [设置对话框构建] —— 遍历配置 schema 的全部设置项，逐个构建并渲染其编辑对话框
    /// （select → TuiDialog.Select / text/number/secret → TuiDialog.InputLine），抓出
    /// 「某设置项弹框时崩溃」类问题（.tui 缺控件、空选项、特殊字符等只在弹窗那一刻才炸）。
    /// Model/SmallModel 走阻塞式 ModelPicker（自带渲染泵），单独覆盖，此处跳过。
    /// </summary>
    private static void TestChunk14(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[设置对话框构建]");
        var schema = Config.SettingSchema();
        var mgr = TuiManager.Instance;
        try { mgr.Enter(); } catch { }

        int built = 0, skipped = 0;
        var problems = new List<string>();
        var oversized = new List<string>();
        var cols = Math.Max(1, Tty.Cols);
        var rows = Math.Max(1, Tty.Rows);
        var origOut = Console.Out;
        try
        {
            foreach (var s in schema)
            {
                TuiWindow? win;
                if (s.Key is "Model" or "SmallModel") { skipped++; continue; }
                else if (s.Type == "select" && s.Options is { Length: > 0 })
                    win = TuiDialog.Select(s.Label, [.. s.Options], _ => { });
                else if (s.Type is "text" or "number" or "secret")
                    win = TuiDialog.InputLine(s.Label, "测试值", s.Type == "secret" ? "" : "测试值", _ => { });
                else continue;

                built++;
                try
                {
                    var screen = new ChatScreen();
                    mgr.PushScreen(screen);
                    try
                    {
                        Console.SetOut(TextWriter.Null); // 静音：渲染帧不进自测输出
                        screen.ShowWindow(win);
                        mgr.Render();
                        Console.SetOut(origOut);
                        // 窗口几何：不超屏（居中对话框被钳制后 Width/Height 应 ≤ 屏幕）
                        if (win.Width > cols || win.Height > rows)
                            oversized.Add($"{s.Key}({s.Type}) {win.Width}x{win.Height} > {cols}x{rows}");
                    }
                    finally
                    {
                        Console.SetOut(origOut);
                        mgr.PopScreen();
                    }
                }
                catch (Exception ex)
                {
                    Console.SetOut(origOut);
                    problems.Add($"{s.Key}({s.Type}): {ex.GetType().Name}: {ex.Message}");
                }
            }
        }
        finally { try { mgr.Exit(); } catch { } }

        Check($"设置对话框构建渲染 {built} 项无异常（ModelPicker {skipped} 项单独覆盖）", problems.Count == 0);
        foreach (var p in problems) Fail($"  ✗ {p}");
        Check($"设置对话框窗口不超屏（屏幕 {cols}x{rows}）", oversized.Count == 0);
        foreach (var o in oversized) Fail($"  ✗ 超屏: {o}");

        // ── YOLO 下 diff 预览直接放行（Web/TUI/GUI 三端统一）──
        var oldMode = PermissionManager.CurrentMode;
        try
        {
            PermissionManager.CurrentMode = PermissionManager.Mode.Yolo;
            var (d1, _) = DiffPreview.Show("旧内容\n旧行\n", "新内容\n新行\n", "test.txt");
            Check("YOLO 下 DiffPreview 直接接受全部变更（不弹窗）", d1 == DiffPreview.Decision.AcceptAll);
        }
        finally { PermissionManager.CurrentMode = oldMode; }

        // 非 YOLO（Ask）下窗口仍可正常构建（确认路径保留）
        var hunks = DiffPreview.BuildHunks("旧内容\n旧行\n", "新内容\n新行\n");
        var winD = DiffPreview.BuildDiffWindow(hunks, "test.txt", null, (_, _) => { });
        Check("Ask 下 DiffPreview 窗口构建正常", winD != null);
        Check("RenderAsMarkup 含红删绿增标记", DiffPreview.RenderAsMarkup("旧行\n", "新行\n", "t.cs").Contains("«green»+") && DiffPreview.RenderAsMarkup("旧行\n", "新行\n", "t.cs").Contains("«red»-"));
    }
}
