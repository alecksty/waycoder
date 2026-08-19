using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Base;

namespace WayCoder.Preview.Render;

/// <summary>
/// 无头渲染器 —— 把 .tui 标记内容渲染成 ANSI 帧字符串（不写终端、不切交替屏、不进输入循环）。
/// 复用主项目 TuiMarkup 解析 + TuiScreen/TuiWindow 渲染管线，仅换一个可指定尺寸的离屏屏幕。
/// </summary>
public static class TuiFrameRenderer
{
    /// <summary>
    /// 渲染 .tui 内容为 ANSI 帧。
    /// </summary>
    /// <returns>(帧字符串, 列数, 行数)。cols/rows = 模拟的屏幕尺寸：所有根元素（Dialog/Window/Screen）都在该屏幕内布局
    /// （对话框居中，超出的部分被钳制），用于模拟不同终端尺寸下布局如何适应。</returns>
    public static (string Frame, int Cols, int Rows) Render(string content, int cols = 80, int rows = 24)
    {
        Prepare();
        var result = TuiMarkup.Load(content);
        return Finish(result, cols, rows);
    }

    /// <summary>
    /// 按资源名渲染（dialogs/modelpicker.tui 等）：文件系统 Raw/ 优先、嵌入资源兜底；
    /// 自动填充 {title} 占位符（与主项目 --tui-preview 资源名加载行为一致）。
    /// </summary>
    public static (string Frame, int Cols, int Rows) RenderResource(string name, int cols = 80, int rows = 24)
    {
        Prepare();
        var result = TuiMarkup.LoadResource(name,
            new Dictionary<string, string> { ["title"] = Path.GetFileName(name) });
        return Finish(result, cols, rows);
    }

    /// <summary>路径或资源名统一入口：文件存在→读内容；否则按资源名加载。供命令行/无头模式共用。</summary>
    public static (string Frame, int Cols, int Rows) RenderPathOrResource(string pathOrName, int cols = 80, int rows = 24)
        => File.Exists(pathOrName)
            ? Render(File.ReadAllText(pathOrName), cols, rows)
            : RenderResource(pathOrName, cols, rows);

    /// <summary>路径或资源名是否可加载（文件存在，或文件系统 Raw/ / 嵌入资源可解析）。</summary>
    public static bool IsLoadable(string pathOrName)
    {
        if (string.IsNullOrWhiteSpace(pathOrName)) return false;
        if (File.Exists(pathOrName)) return true;
        try { TuiMarkupPaths.LoadText(pathOrName); return true; }
        catch { return false; }
    }

    private static void Prepare()
    {
        // 预览 = 设计/模拟模式：注入环境量，加载的元素 InDesign/SimulatedScreen 为 true
        TuiMarkup.InDesign = true;
        TuiMarkup.SimulatedScreen = true;
    }

    private static (string Frame, int Cols, int Rows) Finish(TuiMarkupResult result, int cols, int rows)
    {
        cols = Math.Clamp(cols, 20, 240);
        rows = Math.Clamp(rows, 10, 100);

        var frame = BuildFrame(result, cols, rows);
        return (frame, cols, rows);
    }

    private static string BuildFrame(TuiMarkupResult result, int cols, int rows)
    {
        var sb = new StringBuilder();
        sb.Append(AnsiTty.CursorHide).Append(AnsiTty.Home).Append(AnsiTty.ClearScreen);

        if (result.Window != null)
        {
            // Dialog/Window 根：复刻 TuiDialog.Show 的 RenderOnlyScreen（尺寸可指定）
            var screen = new PreviewScreen();
            screen.SetSize(cols, rows);
            screen.RootView = new TuiVBox { Width = cols, Height = rows };
            screen.RootView.OnCreate();
            screen.AddWindow(result.Window);
            screen.Render(sb);
        }
        else if (result.Screen != null)
        {
            // Screen/App 根：与 Dialog 根同路径 —— 用 PreviewScreen 包住标记的 RootView（显式尺寸+布局）
            var screen = new PreviewScreen();
            screen.SetSize(cols, rows);
            screen.RootView = result.Screen.RootView;
            screen.RootView.Width = cols;
            screen.RootView.Height = rows;
            // 不调 OnCreate：TuiVBox 内容驱动高度会把它撑高，flex 分配用了被撑高的高度导致溢出。
            // 与真实 MarkupChatScreen 一致：只设宽高 + Layout。
            screen.RootView.Layout();
            PopulateDesignPlaceholders(result);
            screen.Render(sb);
        }
        else if (result.View != null)
        {
            // 控件根（如裸 VBox）：包进无边框窗口
            var win = new TuiWindow { RootView = result.View, BorderStyle = WindowBorder.None };
            var screen = new PreviewScreen();
            screen.SetSize(cols, rows);
            screen.RootView = new TuiVBox { Width = cols, Height = rows };
            screen.RootView.OnCreate();
            screen.AddWindow(win);
            screen.Render(sb);
        }
        else
        {
            throw new InvalidOperationException("无法识别的 .tui 根元素（应为 Screen/Window/Dialog/控件）");
        }

        return sb.ToString();
    }

    /// <summary>仅绘制用的最小屏幕：暴露 SetSize 以便在无管理器/无输入循环下渲染指定尺寸。</summary>
    private sealed class PreviewScreen : TuiScreen
    {
        public void SetSize(int w, int h) { TW = w; TH = h; }
    }

    /// <summary>
    /// 设计态运行时数据注入：标题栏版本号/中心智能体名是「运行时数据」由预览注入（版本永远取 Global.Version）。
    /// 侧栏分区属于「布局」，由标记 `<SidePanel><Section><Line>` 声明（见 chat.tui），此处不再写布局代码。
    /// 真实 App（MarkupChatScreen）运行时会覆盖这些值。
    /// </summary>
    private static void PopulateDesignPlaceholders(TuiMarkupResult result)
    {
        try
        {
            // 仅对「未在标记中定义 center」的标题栏注入聊天示例数据（chat.tui）；
            // editor.tui 等自带 center="📝 编辑器" 的标题栏不被覆盖
            if (result.Find<TuiTitleBar>("titleBar") is { } tb &&
                string.IsNullOrEmpty(tb.CenterText))
            {
                tb.Version = Global.Version;
                tb.CenterText = "💬 智能体 1 · 🔨 建造模式";
            }
        }
        catch { /* 占位注入失败不影响渲染 */ }
    }
}
