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
}
