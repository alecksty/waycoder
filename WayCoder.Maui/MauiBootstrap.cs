using WayCoder;
using WayCoder.Infra;
using WayCoder.Maui.Services;
using WayCoder.Tools;
using WayCoder.UI.Tui;

namespace WayCoder.Maui;

// ═══════════════════════════════════════════════════════════════
//  MAUI 移动端启动引导 —— 对标 WayCoder.Gui/GuiBootstrap.cs 与 CLI 的 Program.Main。
//
//  关键差异（移动端 = 完全脱离电脑）：
//    1. Global.HomeOverride = AppDataDirectory —— 配置/会话/记忆/日志全部落 App 私有目录，
//       不碰 ~/.waycoder，状态随 App 沙箱隔离（「手机独立编程智能体」的根基）。
//    2. 沙箱 workspace = AppDataDirectory/workspace，SandboxManager 边界轴设 ProjectWrite +
//       AllowedDirectory=workspace —— 写工具越界拦截（CheckWritable）。
//    3. CwdContext.Current = workspace —— 文件工具相对路径解析锚点（替代桌面 bash 的 cwd）。
//
//  移动端无进程工具/Hook 脚本/MCP stdio（见 CoreStubs），故此处不初始化 HooksManager/
//  McpManager；这些在 M3 建 Agent 时按「移动端可用子集」单独接线。
// ═══════════════════════════════════════════════════════════════
public static class MauiBootstrap
{
    /// <summary>沙箱工作区根目录（Agent 可读写范围）。</summary>
    public static string WorkspaceDir { get; private set; } = "";

    /// <summary>崩溃时保存会话的回调（M3 建 Agent 后由 AgentService 挂上）。</summary>
    public static Action? OnCrashSave { get; set; }

    private static bool _done;

    /// <summary>
    /// 一次性启动初始化（幂等）。必须在任何 Config/Agent 访问前调用一次；
    /// 纯内存赋值 + 建目录，无重 IO，可安全在 UI 启动早期（App 构造函数）同步调用。
    /// </summary>
    public static void Initialize()
    {
        if (_done) return;
        _done = true;

        // 1) 配置目录重定向 → App 私有目录（必须最先，ErrorLog/Config 都依赖 Global.Home）
        Global.HomeOverride = FileSystem.Current.AppDataDirectory;

        // Android 进程 cwd 默认是根目录 "/"，任何相对路径写操作（SyncConfigJsonToLocal、
        // FindEnvFile 等走 Directory.GetCurrentDirectory() 的代码）会解析到 "/"，
        // 写 ~/.waycoder 直接 "access to the path '/' is denied"。
        // 统一把 cwd 锚到 App 私有目录，所有 cwd 派生路径落在可写区。
        try { Directory.SetCurrentDirectory(Global.Home); } catch { }

        // 2) 错误日志 —— 落 App 目录（baseDir 显式传，避免用 iOS 上无意义的 CWD）
        ErrorLog.Initialize(baseDir: Global.Home, catchAllExceptions: true);

        // 3) 全局未处理异常：落盘 + 尽力保存会话（对齐 GuiBootstrap）
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            try { ErrorLog.Error("MAUI", "未处理异常", e.ExceptionObject as Exception); } catch { }
            try { OnCrashSave?.Invoke(); } catch { /* 崩溃路径不再抛 */ }
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            try { ErrorLog.Error("MAUI", "未观察的任务异常", e.Exception); } catch { }
            e.SetObserved();
        };

        // 4) 沙箱 workspace
        WorkspaceDir = Path.Combine(Global.Home, "workspace");
        Directory.CreateDirectory(WorkspaceDir);

        // 5) 沙箱边界：可写范围仅 workspace（project = SandboxMode.ProjectWrite）
        SandboxManager.SetLevel("project");
        SandboxManager.AllowedDirectory = WorkspaceDir;

        // 6) cwd 锚点 → workspace（read_file/write_file/edit_file/glob 等相对路径解析）
        CwdContext.Current.Value = WorkspaceDir;

        // 7) 交互桥注入：权限确认 / AskUserQuestion / diff 确认走原生对话框（M5）
        UxHelper.WebInteraction = new MauiWebInteraction();
        UxHelper.OnNotify = (level, title, message) =>
        {
            // error 级别弹框告知用户（重要）；其余级别记录日志避免频繁打扰
            if (level == "error")
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var page = Shell.Current?.CurrentPage;
                    if (page != null) await page.DisplayAlertAsync(title, message, "确定");
                });
            }
            else
            {
                ErrorLog.Info("MAUI.Notify", $"[{level}] {title}: {message}");
            }
        };

        // 8) 注册全部斜杠命令（对齐桌面端 54 命令；进程类命令执行时优雅降级为「移动端不支持」）。
        SlashCommandRegistry.RegisterAll();
    }

    /// <summary>
    /// 后台预热配置单例（Config.Instance 懒加载含重 IO：.env/schema/config.json/迁移/同步）。
    /// 首次进 SettingsPage 前调用，避免设置页首开卡顿；异常兜底不崩（设置页会重触发）。
    /// </summary>
    public static Task WarmupConfigAsync() => Task.Run(() =>
    {
        try { _ = Config.Instance; }
        catch (Exception ex) { ErrorLog.Warning("MAUI", "配置预热失败", ex); }
    });

    /// <summary>
    /// 自动化自测钩子（仅模拟器调试用，正常启动不触发）：
    /// 检测到 Global.Home/autotest.flag 标记文件时，自动发一条「写文件」任务验证
    /// LLM 连通 + 沙箱 write_file 修复是否生效，结果落 autotest_result.txt。
    /// 不依赖 UI 输入 —— 用于无辅助功能权限的 iOS 模拟器端到端验证。
    /// </summary>
    public static async Task RunAutoTestIfRequestedAsync()
    {
        var flag = Path.Combine(Global.Home, "autotest.flag");
        if (!File.Exists(flag)) return;
        try { File.Delete(flag); } catch { /* 只跑一次 */ }

        var resultPath = Path.Combine(Global.Home, "autotest_result.txt");
        try
        {
            _ = Config.Instance; // 确保 config.json / api_keys 已加载

            // Yolo 跳过确认轴（write_file 弹框），但边界轴（沙箱 CheckWritable）仍生效 ——
            // 正好验证「沙箱修复」而非「权限确认」。
            PermissionManager.CurrentMode = PermissionManager.Mode.Yolo;

            var svc = new AgentService();
            var sb = new System.Text.StringBuilder();
            await svc.ChatAsync(
                "在当前工作目录创建一个 hello.txt 文件，内容为「沙箱修复验证成功」。完成后用一句话告诉我创建结果。",
                t => sb.Append(t),
                (name, summary) => sb.Append($"\n[TOOL:{name}]"),
                _ => { },
                System.Threading.CancellationToken.None);

            await File.WriteAllTextAsync(resultPath, "RESULT=OK\n" + sb);
            ErrorLog.Info("MAUI.AutoTest", "自动自测完成 OK");
        }
        catch (Exception ex)
        {
            try { await File.WriteAllTextAsync(resultPath, "RESULT=FAIL\n" + ex); } catch { }
            ErrorLog.Error("MAUI.AutoTest", "自动自测失败", ex);
        }
    }
}
