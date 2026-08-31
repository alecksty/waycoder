using WayCoder.Infra;
using WayCoder.Tools;
using WayCoder.Sql;
using System.Text;

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

    /// <summary>
    /// git 工具：纯 C# 实现（无 git 进程），复用 <see cref="WayCoder.Git.GitCore"/>（对象模型）+
    /// <see cref="WayCoder.Git.GitRemote"/>（smart HTTP 传输，pull/push）+ <see cref="WayCoder.Git.GitBranch"/>（分支管理）。
    /// 覆盖 init/add/commit/status/diff/log/branch/checkout/merge/pull/push/fetch/remote/clone/credential，不依赖系统 git 二进制。
    /// </summary>
    public class GitTool : ITool, ICancellableTool
    {
        public string Name => "git";
        public ToolExecutionMode ExecutionMode => ToolExecutionMode.Exclusive;
        public string Description => "执行 Git 操作（纯 C# 实现）：init、add、commit、status、diff、log、branch、checkout、merge、pull、push、fetch、remote、clone、credential。";
        public JNode Parameters => JNode.Object()
            .Set("type", "object")
            .Set("properties", JNode.Object()
                .Set("command", JNode.Object().Set("type", "string").Set("description", "Git 子命令，如 'status'、'add .'、'commit -m \"msg\"'、'log'、'diff'、'branch'、'checkout dev'、'merge dev'、'pull'、'push'、'remote add origin <url>'、'clone <url>'、'credential --token <user> <token>'")));

        public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
            => ExecuteAsync(arguments, CancellationToken.None);

        public Task<string> ExecuteAsync(Dictionary<string, object?> arguments, CancellationToken cancellationToken)
        {
            var command = arguments.GetValueOrDefault("command")?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(command))
                return Task.FromResult("用法：git <init|add|commit|status|diff|log|branch|checkout|merge|pull|push|fetch|remote|clone|credential>");
            try
            {
                var cwd = CwdContext.Current.Value ?? Directory.GetCurrentDirectory();
                var repoRoot = WayCoder.Git.GitCore.FindRepoRoot(cwd);

                // init / clone 可在仓库尚不存在时执行（以 cwd 为根创建仓库）
                var sub = command.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();
                var target = sub is "init" or "clone" ? (repoRoot ?? cwd) : repoRoot;

                // ⚠️ 系统级守卫：禁止 git 操作 workspace 根目录（只能在项目子目录，
                //    否则 pull/checkout 等可能误擦全部代码）
                if (IsWorkspaceRootDir(target))
                    return Task.FromResult("⛔ 禁止在 workspace 根目录执行 git 操作！请进入 workspace/<项目名>/ 子目录（每个项目独立 .git），或在「代码同步」页把仓库克隆到项目子目录。");

                if (sub is "init" or "clone")
                    return Task.FromResult(WayCoder.Git.GitCore.Run(repoRoot ?? cwd, command));
                if (repoRoot == null)
                    return Task.FromResult("⚠ 当前目录不在 git 仓库内。请先 git init，或 /cd 到仓库目录。");
                return Task.FromResult(WayCoder.Git.GitCore.Run(repoRoot, command));
            }
            catch (Exception ex)
            {
                return Task.FromResult($"错误：git: {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>是否 workspace 根目录（git 操作禁区，防止误擦全部代码）。</summary>
        private static bool IsWorkspaceRootDir(string? dir)
        {
            if (string.IsNullOrEmpty(dir)) return false;
            try
            {
                var ws = Path.GetFullPath(WayCoder.Maui.MauiBootstrap.WorkspaceDir).TrimEnd('\\', '/');
                var d = Path.GetFullPath(dir).TrimEnd('\\', '/');
                return string.Equals(d, ws, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }
    }

    /// <summary>git_pr 工具降级桩：移动端无 git/gh 进程。返回不支持提示，保证 /pr 命令编译通过。</summary>
    public class GitPRTool : ITool
    {
        public string Name => "git_pr";
        public string Description => "移动端暂不支持创建 Pull Request（无 git/gh 进程）。";
        public JNode Parameters => JNode.Object()
            .Set("type", "object")
            .Set("properties", JNode.Object()
                .Set("title", JNode.Object().Set("type", "string").Set("description", "PR 标题")));

        public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
            => Task.FromResult("⚠️ 移动端暂不支持创建 Pull Request：无本地 git/gh 进程。");
    }

    /// <summary>
    /// sqlite 工具：移动端用内置精简 SQL 引擎（<see cref="WayCoder.Sql.SqlDatabase"/>，纯 C# 手搓、
    /// 零依赖、AOT 安全），绕开 iOS 禁 Process.Start 的限制（桌面端同名工具走 sqlite3 CLI，见主工程 Tools/SqliteTool.cs）。
    /// 支持 CREATE TABLE/INSERT/SELECT（WHERE/ORDER BY/LIMIT/聚合）/UPDATE/DELETE/DROP，动态类型。
    /// database 省略则作用内存库（当次调用有效）；指定则持久化到自定格式文件，跨调用保留表数据。
    /// </summary>
    public class SqliteTool : ITool
    {
        public string Name => "sqlite";
        public ToolExecutionMode ExecutionMode => ToolExecutionMode.Exclusive;
        public string Description => "执行 SQL（内置 SQL 引擎，无需安装 sqlite3）：CREATE TABLE/INSERT/SELECT/UPDATE/DELETE/DROP，SELECT 支持 WHERE/ORDER BY/LIMIT/聚合(COUNT/SUM/AVG/MIN/MAX)。database 省略用内存库（当次调用有效），指定则持久化到文件。";

        public JNode Parameters => JNode.Object()
            .Set("type", "object")
            .Set("properties", JNode.Object()
                .Set("database", JNode.Object()
                    .Set("type", "string")
                    .Set("description", "数据库文件路径（省略则作用于内存库，当次调用有效）"))
                .Set("query", JNode.Object()
                    .Set("type", "string")
                    .Set("description", "要执行的 SQL 语句，支持多条语句以分号分隔")))
            .Set("required", JNode.Array().Add("query"));

        public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
        {
            var database = arguments.GetValueOrDefault("database")?.ToString() ?? "";
            var query = arguments.GetValueOrDefault("query")?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(query))
                return Task.FromResult("错误：请提供 SQL 查询 (query)");
            return Task.FromResult(Run(database, query));
        }

        private static string Run(string database, string query)
        {
            try
            {
                bool persistent = !string.IsNullOrWhiteSpace(database);
                var path = persistent ? ResolveDbPath(database) : "";
                var db = persistent ? SqlDatabase.Load(path) : new SqlDatabase();
                var result = db.Execute(query);
                if (persistent) db.Save(path);
                return result;
            }
            catch (Exception ex)
            {
                return $"错误：SQL 执行失败 — {ex.Message}";
            }
        }

        private static string ResolveDbPath(string database)
        {
            var cwd = CwdContext.Current.Value ?? Directory.GetCurrentDirectory();
            try { return Path.GetFullPath(database, cwd); }
            catch { return database; }
        }
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

    // ── CLI 专属类型占位（ISlashCommand/SlashCommand/SlashCommandRegistry/ProgramContext 已由
    //    主工程 SlashCommand.cs 编译进来，此处不再 stub）。补 Program 单槽位成员 + AgentSlot 占位 + PluginRegistry。 ──

    /// <summary>MAUI 进程占位：主项目 CLI Program（MAUI 无 REPL 主循环，单槽位语义）。</summary>
    public static partial class Program
    {
        /// <summary>移动端恒单槽位（槽位 0）。</summary>
        public static int ActiveSlotIndex => 0;

        /// <summary>移动端无多槽位并行，返回空数组；命令均有 slots != null / idx &lt; slots.Length 边界检查，空数组安全。</summary>
        public static AgentSlot[] GetSlots() => [];

        /// <summary>移动端无槽位工具集合刷新（工具集静态），no-op。</summary>
        public static void RefreshActiveSlotTools() { }

        /// <summary>移动端无 REPL 主循环，退出请求 no-op。</summary>
        public static void RequestExit() { }

        /// <summary>移动端无 CLI --resume 待恢复会话，恒 null。</summary>
        public static (List<JNode> Messages, string Model)? PendingRestore => null;

        /// <summary>移动端无待恢复会话，no-op。</summary>
        public static void ClearPendingRestore() { }
    }

    /// <summary>
    /// AgentSlot 占位：移动端单槽位。命令层只用 Count / Agent / WorkMode / DeliverMessage；
    /// 真实的多槽位调度（F1-F10）桌面端专属，移动端恒一个槽位。
    /// </summary>
    public class AgentSlot
    {
        public const int Count = 1;
        public Agent? Agent { get; set; }
        public string? WorkingDirectory { get; set; }
        public WorkMode WorkMode { get; set; } = WorkMode.Build;
        public void DeliverMessage(int fromSlot, string message, WayCoder.UI.Tui.Screens.ChatScreen? activeScreen, int targetIdx) { }
    }

    public static class PluginRegistry
    {
        public static IEnumerable<ITool> CollectTools() => [];

        /// <summary>移动端无编译期插件，贡献命令为空（SlashCommandRegistry.RegisterAll 末尾调用）。</summary>
        public static IEnumerable<ISlashCommand> CollectCommands() => [];
    }

    /// <summary>
    /// 主题配置降级桩：移动端主题由 MAUI 原生管。Presets/ApplyPreset 仅为满足 /theme 命令
    /// 的编译与运行（列出预设名、切换 no-op）。
    /// </summary>
    public class ThemeConfig
    {
        internal static readonly Dictionary<string, ThemeConfig> Presets = new()
        {
            ["默认"] = new ThemeConfig(),
            ["深色"] = new ThemeConfig(),
            ["浅色"] = new ThemeConfig(),
        };

        public static void ApplyPreset(string name) { }
    }

}

// ── TUI 屏幕类型占位：MAUI 无终端 TUI，ActiveScreen 恒 null。 ──
// 这些桩同时满足工具层（PermissionManager/TodoTool）与 Agent 核心（Agent.Commit）对
// ChatScreen/TuiManager 的类型引用。真正的确认/提问改走 UxHelper.WebInteraction（MAUI 注入原生弹层）。

namespace WayCoder.UI.TUI.Base
{
    /// <summary>终端窗口占位（对话框 / Toast 的载体类型，MAUI 无终端窗口层渲染）。</summary>
    public class TuiWindow
    {
    }

    /// <summary>终端屏幕基类桩（MAUI 无终端 TUI，仅作类型占位 + PostToUI 直执行）。</summary>
    public class TuiScreen
    {
        /// <summary>MAUI 无 UI 线程渲染循环，PostToUI 直接同步执行投递的动作。</summary>
        public void PostToUI(Action action) => action();

        /// <summary>弹窗占位：MAUI 无终端窗口层，no-op（真实弹窗走 WebInteraction / 原生页面）。</summary>
        public void ShowWindow(TuiWindow win) { }

        /// <summary>Toast 占位：MAUI 无终端 Toast，no-op（真实 Toast 由 ChatPage 注入实现）。</summary>
        public TuiWindow ShowToast(string message, int durationMs = 2000) => new TuiWindow();
    }

    /// <summary>终端 TUI 管理器桩：ActiveScreen 恒 null、IsActive 恒 false，触发「非交互环境」分支。</summary>
    public class TuiManager
    {
        public static TuiManager Instance { get; } = new();
        public TuiScreen? ActiveScreen { get; private set; }
        public bool IsActive { get; private set; }

        /// <summary>主循环阶段标记桩：MAUI 无终端渲染主循环，恒 idle（DiagCommand /diag 引用，编译兼容）。</summary>
        public static volatile string UiLoopActivity = "idle";

        /// <summary>推入屏幕占位：移动端无终端屏幕栈，no-op（真实跳转原生页面由 ChatPage 桥接 EditorScreen/SettingsScreen）。</summary>
        public void PushScreen(TuiScreen screen) { }
    }
}

namespace WayCoder.UI.Tui.Controls
{
    /// <summary>
    /// 对话框占位：MAUI 无终端窗口对话框，Select/MultiSelect 返回空 TuiWindow、不触发回调
    /// （真实弹窗后续由 ChatPage 桥接原生选择器）。类型仅为满足命令层 TuiDialog.Select 调用。
    /// </summary>
    public static class TuiDialog
    {
        public static WayCoder.UI.TUI.Base.TuiWindow Select(string title, List<string> items,
            Action<int> onSelect, Action? onCancel = null)
            => new WayCoder.UI.TUI.Base.TuiWindow();

        public static WayCoder.UI.TUI.Base.TuiWindow MultiSelect(string title, List<string> items,
            Action<HashSet<int>> onConfirm, Action? onCancel = null, HashSet<int>? preChecked = null)
            => new WayCoder.UI.TUI.Base.TuiWindow();
    }

    /// <summary>
    /// 状态栏占位：移动端无终端状态栏。命令层仅设置 CurrentWorkMode（/mode 切换），
    /// 其余桌面端状态字段（SlotStates/AgentBusy 等）移动端不需要。
    /// </summary>
    public class TuiStatusBar
    {
        public WorkMode CurrentWorkMode { get; set; } = WorkMode.Build;
    }
}

namespace WayCoder.UI.Tui.Screens
{
    /// <summary>聊天消息 POCO（轻量重定义，去掉主工程 ChatMsg 对 ToolRenderers/Shared 的依赖）。</summary>
    public class ChatMsg
    {
        public string Role { get; set; } = "system";
        public string Content { get; set; } = "";
        public string? SessionId { get; set; }
        public DateTime Time { get; set; } = DateTime.Now;
        public int TokenCount { get; set; }
        public bool Streaming { get; set; }
        public bool Centered { get; set; }
        public int Indent { get; set; }
    }

    /// <summary>
    /// 桥接 ChatScreen：MAUI 无终端 TUI，命令执行时把输出泵回 ChatPage 消息列表。
    /// ChatPage 注入静态回调（OnAddSystemMsg/OnAddMessage/OnClearChat/OnGetMessages），
    /// 未注入时方法安全 no-op。其余终端专属方法（ShowWindow/ShowToast/ShowMenu/ConfirmDialog 等）no-op。
    /// </summary>
    public partial class ChatScreen : WayCoder.UI.TUI.Base.TuiScreen
    {
        // ── 桥接回调（由 ChatPage 注入）──
        public static Action<string>? OnAddSystemMsg;
        public static Action<string, string, bool?, int>? OnAddMessage;
        public static Action? OnClearChat;
        public static Func<List<ChatMsg>>? OnGetMessages;

        /// <summary>移动端恒单槽位（槽位 0）。</summary>
        public int ActiveSlotIndex { get; set; } = 0;

        /// <summary>当前对话消息（委托 ChatPage 的 ObservableCollection 快照；未注入返回空）。</summary>
        public List<ChatMsg> ChatMessages => OnGetMessages?.Invoke() ?? [];

        /// <summary>状态栏占位实例（命令层 /mode 会设置 CurrentWorkMode）。</summary>
        public WayCoder.UI.Tui.Controls.TuiStatusBar StatusBar { get; } = new();

        public void AddSystemMsg(string content) => OnAddSystemMsg?.Invoke(content);

        /// <summary>投递一条待发送消息（ReviewCommand 等命令把审查 prompt 投递为普通消息）。MAUI 桥接 ChatPage 发送。</summary>
        public static Action<string>? OnEnqueueSubmission;
        public void EnqueueSubmission(string input) => OnEnqueueSubmission?.Invoke(input);

        /// <summary>模型显示刷新桩：MAUI 真实刷新走 ChatPage.RefreshModelBar（ConnectionCommand 切换后调用，编译兼容）。</summary>
        public void RefreshModelStatus() { }

        public void AddMessage(string content, string role = "assistant", bool? centered = null, int indent = 0)
            => OnAddMessage?.Invoke(content, role, centered, indent);

        public void ClearChat() => OnClearChat?.Invoke();

        public void SyncTheme() { }

        public void RefreshSidePanel() { }

        public int ShowMenu(string title, List<string> choices) => -1;

        public bool ConfirmDialog(string title, string message) => false;

        public bool ShowPlanApproval(string planSummary, string planDetail) => true;

        /// <summary>权限确认桩：返回 0=允许（MAUI 真实确认走 UxHelper.WebInteraction，此分支 ActiveScreen 恒 null 不会命中）。</summary>
        public int ShowPermissionDialog(string toolName, string argsSummary, string argsDetail, bool isDangerous) => 0;
    }

    /// <summary>源码编辑器占位（/edit 命令 new）。移动端真实跳转 EditorPage 由 ChatPage 桥接。</summary>
    public class EditorScreen : WayCoder.UI.TUI.Base.TuiScreen
    {
        public EditorScreen(string filePath = "", bool readOnly = false) { }
    }

    /// <summary>设置界面占位（/settings 命令 new）。移动端真实跳转 SettingsPage 由 ChatPage 桥接。</summary>
    public class SettingsScreen : WayCoder.UI.TUI.Base.TuiScreen
    {
        public SettingsScreen() { }
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

namespace WayCoder.UI.TUI.Custom
{
    /// <summary>
    /// 模型选择对话框占位（/model 无参弹框）。MAUI 无终端 ANSI 直写选择器，
    /// Show 返回 null 表示「移动端未实现弹框」——命令层 pick == null 时优雅跳过。
    /// 真实模型选择走 MAUI ChatPage 顶部模型栏（ModelPickerPage）；Apply 供后续桥接复用。
    /// </summary>
    public static class ModelPicker
    {
        public record Result(string ModelId, bool IsLarge, int TargetSlot,
            bool NeedsApiKey = false, string? ProviderId = null, string? BaseUrl = null);

        public static Result? Show(int currentSlot = -1, bool forceReadKeys = false) => null;

        public static void Apply(string modelId, bool isLarge, int slot, string? baseUrl = null, string? providerId = null) { }
    }

    /// 供应商管理走 MAUI ModelManagerPage（供应商卡片点开设Key/改名/改地址/删除）；桩保证 /provider 命令在移动端编译。
    /// </summary>
    public static class ProviderPicker
    {
        public static void Show() { }
    }
}
