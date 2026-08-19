using WayCoder.Infra;
using WayCoder.Tools;

namespace WayCoder.UI.Gui;

// ═══════════════════════════════════════════════════════════════
//  GUI 启动引导 —— 对齐 CLI 的 Program.Main 初始化序列。
//
//  GUI 是独立进程、有自己的 Program.Main，csproj 排除了主项目 Program*.cs，
//  因此不会执行 CLI 的任何启动初始化。Web 版没有这个问题：WebChatServer
//  由 Program.RunWebAsync 在 Main 完成全部初始化之后才创建，天然继承。
//
//  这里只补「进程级、与 UI 无关」的部分；LLM 相关（EmbeddingStore 接线、
//  ProgramContext.Agent）在槽位懒建 Agent 时接，见 MainWindow.EnsureSlot。
// ═══════════════════════════════════════════════════════════════
internal static class GuiBootstrap
{
    private static bool _done;

    /// <summary>崩溃时保存会话的回调，由 MainWindow 构造时挂上（此前 ErrorLog 已就位）。</summary>
    internal static Action? OnCrashSave { get; set; }

    /// <summary>执行一次性启动初始化。可重复调用（内部幂等）。</summary>
    internal static void Initialize()
    {
        if (_done) return;
        _done = true;

        // 1) 错误日志 —— 必须最先，后续任何一步出错才有落盘记录（对齐 Program.cs:65）
        ErrorLog.Initialize(catchAllExceptions: true);

        // 2) 全局未处理异常：落盘 + 尽力保存会话（对齐 Program.cs:68-77）
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            ErrorLog.Error("GUI", "未处理异常", e.ExceptionObject as Exception);
            try { OnCrashSave?.Invoke(); } catch { /* 崩溃路径不再抛 */ }
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            ErrorLog.Error("GUI", "未观察的任务异常", e.Exception);
            e.SetObserved();
        };

        var cfg = Config.Instance;

        // 3) 逐项初始化：任一项失败不阻断启动，只记日志（GUI 没有控制台可回退）
        Step("主题预设", () => ThemeConfig.ApplyPreset(ThemeConfig.Instance.PresetKey ?? cfg.ThemePreset));
        Step("沙箱", () =>
        {
            SandboxManager.SetLevel(cfg.SandboxLevel);
            SandboxManager.AllowedDirectory = Directory.GetCurrentDirectory();
        });
        Step("提示词缓存", () => PromptCache.Enabled = cfg.PromptCaching);
        Step("自定义命令", CustomCommands.Load);
        Step("Hook 系统", () =>
        {
            HooksManager.Init();
            HooksManager.RunSessionStart("startup");
        });
        Step("MCP", McpManager.Init);
        Step("检查点", CheckpointManager.LoadFromDisk);
    }

    /// <summary>会话结束 hook（对齐 CLI 退出时的 RunSessionEnd）。</summary>
    internal static void Shutdown()
    {
        try { HooksManager.RunSessionEnd("exit"); }
        catch (Exception ex) { ErrorLog.Warning("GUI", "session-end hook 失败", ex); }
    }

    private static void Step(string name, Action action)
    {
        try { action(); }
        catch (Exception ex) { ErrorLog.Warning("GUI", $"启动初始化失败：{name}", ex); }
    }
}
