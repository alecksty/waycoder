using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Base;

namespace WayCoder;

/// <summary>
/// TUI 标记预览器 —— 加载 .tui 标记文件并渲染到终端帧，供肉眼核对布局。
/// 运行：waycoder --tui-preview &lt;file.tui&gt;
/// </summary>
public static class TuiPreview
{
    public static int Run(string path)
    {
        try { Console.OutputEncoding = Encoding.UTF8; } catch { }

        if (string.IsNullOrWhiteSpace(path))
        {
            Console.Error.WriteLine("未指定 .tui 文件或资源名（如 dialogs/modelpicker.tui / chat.tui / menu.tui）");
            return 1;
        }

        try
        {
            // 预览 = 设计/模拟模式：元素 InDesign/SimulatedScreen 为 true
            TuiMarkup.InDesign = true;
            TuiMarkup.SimulatedScreen = true;
            TuiMarkupResult result;
            if (File.Exists(path))
            {
                result = TuiMarkup.Load(File.ReadAllText(path));
            }
            else
            {
                // 非文件 → 尝试内嵌资源名（dialogs/modelpicker.tui 等），支持预览任意内嵌对话框
                result = TuiMarkup.LoadResource(path,
                    new Dictionary<string, string> { ["title"] = Path.GetFileName(path) });
            }
            if (result.Screen != null)
            {
                // Screen 根（showcase/chat/main）：用 RenderOnlyScreen 渲染（数据控件正常布局）
                var screen = new TuiDialog.RenderOnlyScreen();
                screen.SetSize(Tty.Cols, Tty.Rows);
                screen.RootView = result.Screen.RootView;
                screen.RootView.OnCreate();
                screen.RootView.OnResize(Tty.Cols, Tty.Rows);
                var sb = new StringBuilder();
                sb.Append(AnsiTty.CursorHide).Append(AnsiTty.Home).Append(AnsiTty.ClearScreen);
                screen.Render(sb);
                Console.Write(sb.ToString());
                return 0;
            }
            TuiWindow win = result.Window
                ?? new TuiWindow { RootView = result.View!, BorderStyle = WayCoder.UI.Shared.WindowBorder.None };
            Console.Write(TuiDialog.Show(win));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"预览失败: {ex.GetType().Name}: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// 实时预览：监听 .tui 文件变更，保存即重渲染（边写边预览，任意编辑器通用）。
    /// 运行：waycoder --tui-watch &lt;file.tui&gt;（Ctrl+C 退出）
    /// </summary>
    public static int Watch(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Console.Error.WriteLine($"文件不存在: {path}");
            return 1;
        }
        try { Console.OutputEncoding = Encoding.UTF8; } catch { }

        System.Threading.Timer? debounce = null;

        void Render()
        {
            try
            {
                TuiMarkup.InDesign = true;
                TuiMarkup.SimulatedScreen = true;
                var result = TuiMarkup.Load(File.ReadAllText(path));
                TuiWindow win = result.Window
                    ?? new TuiWindow { RootView = result.View ?? result.Screen!.RootView, BorderStyle = WayCoder.UI.Shared.WindowBorder.None };
                Console.Write(TuiDialog.Show(win));
                Console.Write("\x1b[0m\n—— 实时预览中（保存即刷新），Ctrl+C 退出 ——\n");
            }
            catch (Exception ex)
            {
                Console.Write($"\x1b[0m\n\n[标记解析错误] {ex.GetType().Name}: {ex.Message}\n\n—— 继续监听，修正后保存即刷新 ——\n");
            }
        }

        debounce = new System.Threading.Timer(_ => Render(), null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        Render();

        var dir = Path.GetDirectoryName(Path.GetFullPath(path))!;
        var fname = Path.GetFileName(path);
        var watcher = new FileSystemWatcher(dir, fname)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };
        watcher.Changed += (_, _) => debounce.Change(300, System.Threading.Timeout.Infinite);
        watcher.Renamed += (_, _) => debounce.Change(300, System.Threading.Timeout.Infinite);

        try { while (true) Thread.Sleep(1000); }
        catch (ThreadInterruptedException) { }
        return 0;
    }
}
