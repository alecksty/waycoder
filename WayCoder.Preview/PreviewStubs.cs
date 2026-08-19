// ═══════════════════════════════════════════════════════════════
//  预览版占位桩：被 TUI 渲染路径引用的「编辑器/工具/模式」类型在预览进程中
//  不参与实际工作，此处提供最小占位使共享源码编译通过。真实实现见主项目。
// ═══════════════════════════════════════════════════════════════
using WayCoder.UI.TUI.Base;

namespace WayCoder.Tools
{
    // 让 SlotState.cs 的 `using WayCoder.Tools;` 有命名空间可解析（预览不编译 Tools）
    public static class _ToolsNamespaceMarker { }
}

namespace WayCoder.UI.Tui.ToolRenderers
{
    // 让 SlotState.cs 的 `using WayCoder.UI.Tui.ToolRenderers;` 有命名空间可解析
    public static class _ToolRenderersNamespaceMarker { }
}

namespace WayCoder
{
    /// <summary>统一错误日志桩（预览不写日志文件；真实实现见主项目 Infra/ErrorLog.cs）。
    /// ModelCatalog 读取模型文件失败时调用 Warning，此处仅吞掉并保留现场。</summary>
    public static class ErrorLog
    {
        public static void Warning(string source, string message, Exception? ex = null) { }
        public static void Error(string source, string message, Exception? ex = null) { }
    }

    /// <summary>工作模式（预览桩，与主项目 WorkModeManager.cs 一致）。</summary>
    public enum WorkMode
    {
        Build,
        Plan,
        Review,
        Auto,
    }

    /// <summary>工作模式管理器桩（TuiStatusBar 读取 Emojis 指示当前模式）。</summary>
    public static class WorkModeManager
    {
        public static readonly Dictionary<WorkMode, string> Emojis = new()
        {
            [WorkMode.Build] = "🔨",
            [WorkMode.Plan] = "🧠",
            [WorkMode.Review] = "🔍",
            [WorkMode.Auto] = "🤖",
        };
    }
}

namespace WayCoder.UI.Tui
{
    /// <summary>旧 Markdown 静态渲染器桩（预览简化渲染：内容按行切分为无色段）。真实实现见 Custom/TuiMarkdown.cs。</summary>
    public static class TuiMarkdown
    {
        public static List<List<(string Text, int Fg, int Bg)>> RenderMessage(
            string content, string role, int maxWidth, bool isPlainText)
        {
            var result = new List<List<(string Text, int Fg, int Bg)>>();
            foreach (var line in (content ?? "").Replace("\r\n", "\n").Split('\n'))
            {
                var seg = new List<(string Text, int Fg, int Bg)>();
                if (line.Length > 0) seg.Add((line, 0, 0));
                result.Add(seg);
            }
            return result;
        }
    }

    /// <summary>交互桥桩（TuiKeybindHelp.Show 引用；预览不走阻塞交互）。真实实现见 Custom/UxHelper.cs。</summary>
    public static class UxHelper
    {
        public static void RenderWait(TuiScreen? screen, ManualResetEventSlim evt, int timeoutMs = 30_000, TuiWindow? win = null) { }
    }
}

namespace WayCoder.UI.Tui.Edit
{
    /// <summary>查找/替换选项（预览桩，与主项目 EditorCore.cs 签名一致）。</summary>
    public readonly record struct FindOptions(bool CaseSensitive = false, bool UseRegex = false, bool WholeWord = false);
}
