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
//  移动端无进程工具/Hook 脚本（见 CoreStubs），不初始化 HooksManager；
//  MCP 已接入（Http/Sse 传输可用，stdio 运行时降级），此处 McpManager.Init() 对齐桌面 Program.Main。
// ═══════════════════════════════════════════════════════════════
public static class MauiBootstrap
{
    /// <summary>沙箱工作区根目录（Agent 可读写范围）。</summary>
    public static string WorkspaceDir { get; private set; } = "";

    /// <summary>是否已启用外部存储 workspace（sdcard/waycoder/workspace，卸载重装代码不丢）。</summary>
    public static bool WorkspaceExternal { get; private set; }

    /// <summary>外部 workspace 根：sdcard/waycoder/workspace（Android 公共存储）。</summary>
    private static string? ExternalWorkspaceDir => ExternalRootDir("workspace");

    /// <summary>外部配置根：sdcard/waycoder/config（配置/会话/记忆，卸载重装不丢）。</summary>
    private static string? ExternalConfigDir => ExternalRootDir("config");

    /// <summary>外部存储根：sdcard/waycoder/&lt;sub&gt;/。</summary>
    private static string? ExternalRootDir(string sub)
    {
#if ANDROID
        try
        {
            var root = Android.OS.Environment.ExternalStorageDirectory?.AbsolutePath;
            if (string.IsNullOrEmpty(root)) return null;
            return Path.Combine(root, "waycoder", sub);
        }
        catch { return null; }
#else
        return null;
#endif
    }

    /// <summary>解析 Global.Home：Android 已授「所有文件访问」→ 外部配置目录；否则 App 私有目录。自动建目录。</summary>
    private static string ResolveHomeDir()
    {
#if ANDROID
        if (Android.OS.Environment.IsExternalStorageManager)
        {
            var ext = ExternalConfigDir;
            if (ext != null)
            {
                try { Directory.CreateDirectory(ext); } catch { }
                return ext;
            }
        }
#endif
        return FileSystem.Current.AppDataDirectory;
    }

    /// <summary>解析 workspace 目录：Android 已授「所有文件访问」→ 外部；否则回退 App 私有目录。自动建目录。</summary>
    private static string ResolveWorkspaceDir()
    {
#if ANDROID
        if (Android.OS.Environment.IsExternalStorageManager)
        {
            var ext = ExternalWorkspaceDir;
            if (ext != null)
            {
                try { Directory.CreateDirectory(ext); } catch { }
                WorkspaceExternal = true;
                return ext;
            }
        }
#endif
        var fallback = Path.Combine(Global.Home, "workspace");
        try { Directory.CreateDirectory(fallback); } catch { }
        WorkspaceExternal = false;
        return fallback;
    }

    /// <summary>
    /// 用户授予「所有文件访问」后调用：切换 workspace 到 sdcard/waycoder/workspace，
    /// 迁移旧私有目录文件，更新沙箱边界与 cwd。返回是否成功启用外部存储。
    /// </summary>
    public static bool TryEnableExternalWorkspace()
    {
#if ANDROID
        if (!Android.OS.Environment.IsExternalStorageManager) return false;
        if (WorkspaceExternal) return true;

        var ext = ExternalWorkspaceDir;
        if (ext == null) return false;

        var old = WorkspaceDir;
        try
        {
            // 自动创建 + 迁移旧内容（仅当外部为空时拷贝，避免覆盖）
            Directory.CreateDirectory(ext);
            if (Directory.Exists(old) && !Directory.EnumerateFileSystemEntries(ext).Any())
                CopyDirectory(old, ext);

            WorkspaceDir = ext;
            WorkspaceExternal = true;
            SandboxManager.AllowedDirectory = ext;
            CwdContext.Current.Value = ext;
            try { Directory.SetCurrentDirectory(ext); } catch { }

            // 配置目录也切到外部 sdcard/waycoder/config（迁移旧的 .waycoder/config/session 等）
            var extConfig = ExternalConfigDir;
            if (extConfig != null && !string.Equals(Global.Home, extConfig, StringComparison.OrdinalIgnoreCase))
            {
                var oldHome = Global.Home;
                try
                {
                    Directory.CreateDirectory(extConfig);
                    MigrateConfig(oldHome, extConfig);
                    Global.HomeOverride = extConfig;
                }
                catch { }
            }
            return true;
        }
        catch { return false; }
#else
        return false;
#endif
    }

    /// <summary>递归拷贝目录（迁移旧 workspace 到外部存储）。</summary>
    private static void CopyDirectory(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var f in Directory.EnumerateFiles(src))
            File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), overwrite: true);
        foreach (var d in Directory.EnumerateDirectories(src))
        {
            var name = Path.GetFileName(d);
            if (name == ".git") continue; // 跳过旧 .git（根目录仓库作废）
            CopyDirectory(d, Path.Combine(dst, name));
        }
    }

    /// <summary>迁移配置目录内容到外部 config（跳过 workspace 子目录；目标为空才拷贝）。</summary>
    private static void MigrateConfig(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        if (Directory.EnumerateFileSystemEntries(dst).Any()) return; // 已有内容不覆盖
        foreach (var f in Directory.EnumerateFiles(src))
            File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), overwrite: true);
        foreach (var d in Directory.EnumerateDirectories(src))
        {
            var name = Path.GetFileName(d);
            if (name == "workspace") continue; // workspace 单独迁移
            CopyDirectory(d, Path.Combine(dst, name));
        }
    }

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

        // 1) 配置目录重定向：已授权外部存储 → sdcard/waycoder/config（卸载重装不丢），否则 App 私有目录
        //    （必须最先，ErrorLog/Config 都依赖 Global.Home）
        Global.HomeOverride = ResolveHomeDir();

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

        // 4) 沙箱 workspace —— 优先外部存储 sdcard/waycoder/workspace（卸载重装代码不丢），
        //    未授予「所有文件访问」时回退 App 私有目录
        WorkspaceDir = ResolveWorkspaceDir();
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

        // 9) 初始化 MCP（Http/Sse 传输可用，stdio 运行时降级）—— 对齐桌面 Program.Main。
        //     McpCache 同步加载缓存工具，Agent 懒建时 ToolRegistry.AllTools 已含 MCP 工具。
        McpManager.Init();

        // 10) 恢复上次的工作/权限/经济模式（手机无快捷键，记住用户选择，下次直接生效）
        try
        {
            if (Services.MauiModeStore.Load() is { } mm)
            {
                WorkModeManager.CurrentMode = mm.Work;
                PermissionManager.CurrentMode = mm.Perm;
                Config.Instance.EconomyMode = mm.Economy;
            }
        }
        catch { }
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
}
