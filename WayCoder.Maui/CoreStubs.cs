using WayCoder.Infra;
using WayCoder.Tools;

// ═══════════════════════════════════════════════════════════════
//  MAUI 移动端占位桩：复用主项目核心源码（Agent/LLM/Tools/Infra/Memory/UI 共享源码编译）时，
//  以下类型在移动端「物理不可用」或「非本前端职责」，此处提供最小占位使其编译通过。
//
//  三类桩：
//   1. 进程类工具（BashTool/GitRunner/LintTool）—— iOS 上 Process.Start 物理不可用，
//      提供实现 ITool 的空实现，ExecuteAsync 返回「移动端不支持」降级提示；
//      这些工具不会被注册进 MAUI 的 ToolRegistry，模型根本看不到，桩只为满足
//      保留源码（Agent.Tools / Agent.Feedback / Agent.Commit / Memory / CustomCommands）
//      里的类型检查与静态引用。
//   2. CLI 专属类型（斜杠命令/插件/程序全局上下文）—— MAUI 无 REPL 主循环，占位。
//   3. TUI 屏幕类型（TuiManager/ChatScreen）—— MAUI 用原生页面替代终端 TUI，
//      但 Agent.Commit.PromptPlanApproval 引用 TuiManager.ActiveScreen，桩保证
//      ActiveScreen 恒 null（自动批准），真正的计划审批改走 M5 的 MauiWebInteraction。
// ═══════════════════════════════════════════════════════════════

namespace WayCoder.Tools
{
    /// <summary>
    /// bash 工具桩：移动端无 shell 进程（iOS 禁 Process.Start），所有命令降级为不支持提示。
    /// 保留类型是为了满足 <c>Agent.Tools.cs</c> 的 <c>tool is BashTool</c> 流式特判
    /// 与 <c>Infra/CustomCommands.cs</c> 的自定义命令内联 bash 调用。
    /// </summary>
    public class BashTool : ITool, ICancellableTool
    {
        public string Name => "bash";
        public ToolExecutionMode ExecutionMode => ToolExecutionMode.Exclusive;
        public string Description => "移动端不支持执行 shell 命令（无本地 shell 进程）。";
        public JNode Parameters => JNode.Object()
            .Set("type", "object")
            .Set("properties", JNode.Object()
                .Set("command", JNode.Object().Set("type", "string").Set("description", "shell 命令")));

        public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
            => Task.FromResult(Unsupported());

        public Task<string> ExecuteAsync(Dictionary<string, object?> arguments, CancellationToken cancellationToken)
            => Task.FromResult(Unsupported());

        public Task<string> ExecuteStreamingAsync(Dictionary<string, object?> arguments, Func<string, Task>? onLine)
            => Task.FromResult(Unsupported());

        public Task<string> ExecuteStreamingAsync(Dictionary<string, object?> arguments, Func<string, Task>? onLine, CancellationToken cancellationToken)
            => Task.FromResult(Unsupported());

        private static string Unsupported()
            => "⚠️ 移动端不支持 bash 工具：本 App 独立运行于手机沙箱，无本地 shell 进程（iOS 物理禁止 Process.Start）。" +
               "请改用 read_file / write_file / edit_file / glob / grep 等文件工具完成操作。";
    }

    /// <summary>
    /// lint 工具桩：移动端无 linter 进程。保留 <c>DetectLanguage</c> 静态方法
    /// （<c>Agent.Feedback.cs</c> 与 <c>DiagnosticManager.cs</c> 引用）——
    /// 返回 null 使调用方跳过 lint，编辑器诊断在移动端天然为空。
    /// </summary>
    public class LintTool : ITool
    {
        public string Name => "lint";
        public string Description => "移动端不支持静态检查（无 linter 进程）。";
        public JNode Parameters => JNode.Object()
            .Set("type", "object")
            .Set("properties", JNode.Object()
                .Set("path", JNode.Object().Set("type", "string").Set("description", "文件路径")));

        public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
            => Task.FromResult("⚠️ 移动端不支持 lint 工具：无本地 linter 进程。");

        /// <summary>返回 null 表示无法识别语言 —— 调用方据此跳过 lint（移动端恒跳过）。</summary>
        public static string? DetectLanguage(string path) => null;
    }
}

namespace WayCoder
{
    /// <summary>
    /// Git 进程执行器桩：移动端无 git 进程。所有调用返回非零退出码，
    /// 使依赖方（自动 commit / 知识库 git 历史索引 / 共享记忆）优雅降级为跳过。
    /// </summary>
    public static class GitRunner
    {
        private const string Unsupported = "⚠️ 移动端不支持 git：无本地 git 进程。";

        public static (int ExitCode, string Stdout, string Stderr) Run(string args, string? cwd = null)
            => (-1, "", Unsupported);

        public static Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(string args, string? cwd = null)
            => Task.FromResult<(int, string, string)>((-1, "", Unsupported));

        public static Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(
            string args, string? cwd, CancellationToken cancellationToken, int? timeoutOverrideMs = null)
            => Task.FromResult<(int, string, string)>((-1, "", Unsupported));

        public static Task<(int ExitCode, string Stdout, string Stderr)> RunArgsAsync(
            IReadOnlyList<string> args, string? cwd, CancellationToken cancellationToken, int? timeoutOverrideMs = null)
            => Task.FromResult<(int, string, string)>((-1, "", Unsupported));

        public static string Output(string args, string? cwd = null) => "";

        public static string RunOrThrow(string args, string? cwd = null)
            => throw new NotSupportedException(Unsupported);
    }

    // ── CLI 专属类型占位（MAUI 无 REPL 斜杠命令 / 编译期插件 / 程序全局上下文）──

    public interface ISlashCommand
    {
        string Name { get; }
        string[] Aliases { get; }
        string Description { get; }
        string? Usage { get; }
        bool Matches(string input);
        Task ExecuteAsync(string args, WayCoder.UI.Tui.Screens.ChatScreen screen);
    }

    public abstract class SlashCommand : ISlashCommand
    {
        public abstract string Name { get; }
        public virtual string[] Aliases => [];
        public abstract string Description { get; }
        public virtual string? Usage => null;
        public virtual bool Matches(string input) => false;
        public abstract Task ExecuteAsync(string args, WayCoder.UI.Tui.Screens.ChatScreen screen);
    }

    public static class SlashCommandRegistry
    {
        private static readonly List<ISlashCommand> _commands = [];
        public static IReadOnlyList<ISlashCommand> Commands => _commands;
        public static void Register(ISlashCommand cmd) { }
        public static void RegisterAll() { }
        public static string[] AllNames => [];
        public static (ISlashCommand? Command, string Args) Match(string userInput) => (null, userInput);
    }

    /// <summary>MAUI 进程占位：主项目 CLI Program（MAUI 无 REPL 主循环，无需槽位数组）。</summary>
    public static partial class Program
    {
    }

    public static class ProgramContext
    {
        public static Agent? Agent { get; set; }
        public static Config Config { get; set; } = new();
        public static LLM? LLM { get; set; }
    }

    public static class PluginRegistry
    {
        public static IEnumerable<ITool> CollectTools() => [];
    }
}

// ── TUI 屏幕类型占位：MAUI 无终端 TUI，ActiveScreen 恒 null。 ──
// 这些桩同时满足工具层（PermissionManager/TodoTool）与 Agent 核心（Agent.Commit）对
// ChatScreen/TuiManager 的类型引用。真正的确认/提问改走 UxHelper.WebInteraction（MAUI 注入原生弹层）。

namespace WayCoder.UI.TUI.Base
{
    /// <summary>终端屏幕基类桩（MAUI 无终端 TUI，仅作类型占位 + PostToUI 直执行）。</summary>
    public class TuiScreen
    {
        /// <summary>MAUI 无 UI 线程渲染循环，PostToUI 直接同步执行投递的动作。</summary>
        public void PostToUI(Action action) => action();
    }

    /// <summary>终端 TUI 管理器桩：ActiveScreen 恒 null、IsActive 恒 false，触发「非交互环境」分支。</summary>
    public class TuiManager
    {
        public static TuiManager Instance { get; } = new();
        public TuiScreen? ActiveScreen { get; private set; }
        public bool IsActive { get; private set; }
    }
}

namespace WayCoder.UI.Tui.Screens
{
    /// <summary>聊天屏幕桩：MAUI 用原生 ChatPage 替代。方法为工具层的类型引用占位。</summary>
    public partial class ChatScreen : WayCoder.UI.TUI.Base.TuiScreen
    {
        public bool ShowPlanApproval(string planSummary, string planDetail) => true;

        /// <summary>权限确认桩：返回 0=允许（MAUI 真实确认走 UxHelper.WebInteraction，此分支 ActiveScreen 恒 null 不会命中）。</summary>
        public int ShowPermissionDialog(string toolName, string argsSummary, string argsDetail, bool isDangerous) => 0;

        public void AddSystemMsg(string content) { }

        public void RefreshSidePanel() { }
    }
}

// ── UI 交互桥占位：UxHelper / DiffPreview / TuiTheme。 ──
// MAUI 用原生对话框替代终端 TUI，UxHelper 的方法统一委托给 WebInteraction（M5 由 MauiWebInteraction 注入）。

namespace WayCoder.UI.Tui
{
    /// <summary>主题预设占位（Config 设置项 select 选项用）。移动端主题由 MAUI 原生管。</summary>
    public static class TuiTheme
    {
        public static readonly string[] PresetNames = ["默认", "深色", "浅色"];
    }

    /// <summary>统一 UX 辅助层桩：非 TUI 模式（MAUI）下全部委托 WebInteraction / OnNotify。</summary>
    public static class UxHelper
    {
        /// <summary>MAUI 无终端 TUI，恒 false。</summary>
        public static bool IsTuiMode => false;

        /// <summary>Web 模式的异步交互桥（MAUI 注入 MauiWebInteraction）。</summary>
        public interface IWebInteraction
        {
            Task<string?> AskAsync(string prompt, string? defaultValue, int timeoutMs);
            Task<string?> SelectAsync(string title, List<string> choices, int timeoutMs);
            Task<List<string>?> MultiSelectAsync(string title, List<string> choices, int timeoutMs);
            Task<int> ConfirmAsync(string title, string message, bool allowAll, int timeoutMs);
            Task<DiffConfirmResult?> DiffConfirmAsync(string filePath, List<DiffPreview.Hunk> hunks, int timeoutMs);
        }

        public static IWebInteraction? WebInteraction { get; set; }
        public static Action<string, string, string>? OnNotify;

        public static void Info(string title, string message) => OnNotify?.Invoke("info", title, message);
        public static void Success(string title, string message) => OnNotify?.Invoke("success", title, message);
        public static void Warn(string title, string message) => OnNotify?.Invoke("warn", title, message);
        public static void Error(string title, string message) => OnNotify?.Invoke("error", title, message);

        /// <summary>文本输入：委托 WebInteraction，否则返回默认值。</summary>
        public static string Ask(string prompt, string? defaultValue = null, int timeoutMs = 30_000)
        {
            if (WebInteraction != null)
                return WebInteraction.AskAsync(prompt, defaultValue, timeoutMs).GetAwaiter().GetResult() ?? defaultValue ?? "";
            return defaultValue ?? "";
        }

        /// <summary>提问对话框（标题+消息+选项按钮）：单选返回选中索引，多选返回选中索引集合；null=取消。</summary>
        public static List<int>? Ask(string title, string message, List<string> options, bool multiSelect, int timeoutMs = 30_000)
        {
            if (options.Count == 0) return multiSelect ? new List<int>() : null;
            if (WebInteraction != null)
            {
                if (multiSelect)
                {
                    var labels = WebInteraction.MultiSelectAsync(title, options, timeoutMs).GetAwaiter().GetResult();
                    if (labels == null) return null;
                    return labels.Select(l => options.IndexOf(l)).Where(i => i >= 0).ToList();
                }
                var label = WebInteraction.SelectAsync(title, options, timeoutMs).GetAwaiter().GetResult();
                if (label == null) return null;
                var idx = options.IndexOf(label);
                return idx >= 0 ? new List<int> { idx } : null;
            }
            return multiSelect ? new List<int>() : null;
        }

        public static string? Select(string title, List<string> choices, int timeoutMs = 30_000)
        {
            if (choices.Count == 0) return null;
            return WebInteraction?.SelectAsync(title, choices, timeoutMs).GetAwaiter().GetResult();
        }

        public static List<string>? MultiSelect(string title, List<string> choices, int timeoutMs = 30_000, bool preCheckAll = false)
        {
            if (choices.Count == 0) return new List<string>();
            return WebInteraction?.MultiSelectAsync(title, choices, timeoutMs).GetAwaiter().GetResult();
        }

        /// <summary>确认框：返回 0=是 1=总是允许 2=否。默认拒绝（保守）。</summary>
        public static int Confirm(string title, string message, bool allowAll = false, int timeoutMs = 0)
        {
            if (WebInteraction != null)
                return WebInteraction.ConfirmAsync(title, message, allowAll, timeoutMs).GetAwaiter().GetResult();
            return 2;
        }

        public static string Secret(string prompt, string? defaultValue = null)
        {
            if (WebInteraction != null)
                return WebInteraction.AskAsync(prompt, defaultValue, 30_000).GetAwaiter().GetResult() ?? defaultValue ?? "";
            return defaultValue ?? "";
        }

        /// <summary>渲染等待桩：MAUI 无终端渲染循环，简单等待事件或超时。</summary>
        public static void RenderWait(WayCoder.UI.TUI.Base.TuiScreen? screen, System.Threading.ManualResetEventSlim evt, int timeoutMs = 30_000, object? win = null, bool? readKeys = null)
        {
            evt.Wait(timeoutMs > 0 ? timeoutMs : 30_000);
        }
    }

    /// <summary>Diff 预览确认结果。</summary>
    public sealed class DiffConfirmResult
    {
        public DiffPreview.Decision Decision = DiffPreview.Decision.RejectAll;
        public HashSet<int>? AcceptedHunks;
    }

    /// <summary>
    /// Diff 预览桩：写文件前的 diff 确认。纯逻辑部分（BuildHunks/ApplyAccepted/RenderAsMarkup）
    /// 提供可用实现（渲染复用 ContentDiffFormatter）；Show 对话框在 MAUI 恒返回 AcceptAll
    /// （真实逐 hunk 确认走 M5 的 WebInteraction.DiffConfirmAsync）。
    /// </summary>
    public static class DiffPreview
    {
        public class Hunk
        {
            public int OldStart, OldCount, NewStart, NewCount;
            public string Header = "";
            public List<HunkLine> Lines = [];
        }

        public class HunkLine
        {
            public char Kind;  // ' ' 上下文, '-' 删除, '+' 添加
            public string Text = "";
            public int OldLine, NewLine;
        }

        public enum Decision { AcceptAll, RejectAll, Partial }

        /// <summary>构建 hunk 列表（MAUI 简化：整个变更视为单 hunk，供 DiffConfirmAsync 展示）。</summary>
        public static List<Hunk> BuildHunks(string oldContent, string newContent)
            => [new Hunk { Lines = [new HunkLine { Text = newContent }] }];

        /// <summary>应用已接受的 hunk（MAUI 简化：接受即返回新内容）。</summary>
        public static string ApplyAccepted(string oldContent, List<Hunk> hunks, HashSet<int> accepted)
            => accepted.Count > 0 ? string.Join("\n", hunks.Select(h => string.Join("\n", h.Lines.Select(l => l.Text)))) : oldContent;

        /// <summary>生成 diff 展示 markup（复用纯逻辑 ContentDiffFormatter）。</summary>
        public static string RenderAsMarkup(string oldContent, string newContent, string filePath)
            => string.IsNullOrEmpty(oldContent)
                ? ContentDiffFormatter.FormatAddedContent(newContent, filePath)
                : ContentDiffFormatter.FormatEditContent(oldContent, newContent, filePath);

        /// <summary>逐 hunk 确认对话框（MAUI 恒接受全部；真实确认由 WebInteraction.DiffConfirmAsync 承担）。</summary>
        public static (Decision Decision, HashSet<int>? AcceptedHunks) Show(string oldContent, string newContent, string filePath)
            => (Decision.AcceptAll, null);
    }
}
