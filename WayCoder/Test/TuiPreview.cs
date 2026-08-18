using System.Text;
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

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Console.Error.WriteLine($"文件不存在: {path}");
            return 1;
        }

        try
        {
            var content = File.ReadAllText(path);
            var result = TuiMarkup.Load(content);
            TuiWindow win = result.Window
                ?? new TuiWindow { RootView = result.View ?? result.Screen!.RootView, Border = WayCoder.UI.Shared.WindowBorder.None };
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
                var result = TuiMarkup.Load(File.ReadAllText(path));
                TuiWindow win = result.Window
                    ?? new TuiWindow { RootView = result.View ?? result.Screen!.RootView, Border = WayCoder.UI.Shared.WindowBorder.None };
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
