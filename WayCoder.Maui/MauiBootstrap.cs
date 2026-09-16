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
            CwdContext.PushScope(ext);
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

        // 6) cwd 锚点 → workspace（read_file/write_file/edit_file/glob 等相对路径解析）。
        //    **开新作用域**：这是 App 的根盒子，之后每个消息的 Agent 任务都继承它，
        //    于是 `cd` 真正生效、且跨消息保持（在此之前 cd 是 no-op）。
        //    同时设**进程级默认**：AsyncLocal 作用域按 async 流传播，而平台调起的回调
        //    （UI 事件、切深浅色导致 Activity 重建后的新处理器）可能落在一条没继承到本盒子的
        //    流里 —— 那时惰性新建的盒子必须按 workspace 播种，否则会回退到 `Global.Home`
        //    （上面第 189 行把进程 cwd 设成了 config 目录），表现为 Agent 在工作区外乱写被沙箱拦死。
        CwdContext.SetDefault(WorkspaceDir);
        CwdContext.PushScope(WorkspaceDir);

        // 6b) 把随包的中文字体落到文件系统上（供画布文字渲染用）。
        EnsureBundledFonts();

        // 6c) 把示例程序（经典小游戏等）按语言解到工作区 `examples/<语言>/`，用户开箱即可
        //     `vml run examples/c/gomoku.c` 跑一个真程序。
        EnsureExamples();

        // 7) 交互桥注入：权限确认 / AskUserQuestion / diff 确认走原生对话框（M5）
        UxHelper.WebInteraction = new MauiWebInteraction();

        // 7b) VML 程序开窗口的宿主接线：syscall 处理器只负责"要开一个窗口"，
        //     真正导航到绘图页由这里注入（处理器不直接依赖 Shell，便于单测与复用）。
        Services.VmlUiCalls.OpenWindowAsync = async scene =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current.GoToAsync("drawwindow");
                if (Shell.Current.CurrentPage is Pages.DrawWindowPage page) page.Attach(scene);
            });
        };
        Services.VmlUiCalls.CloseWindowAsync = () =>
            MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Shell.Current.CurrentPage is Pages.DrawWindowPage) await Shell.Current.GoToAsync("..");
            });
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

        // 9.5) 编辑器设置（只读阈值 / tab 宽度 / 调试 HUD）—— 必须在任何 EditorPage 打开前加载
        try { Services.MauiEditorStore.Load(); } catch { }

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
    /// <summary>
    /// 把**随包的中文字体**（<c>Resources/Fonts/SarasaMonoSC-Regular.ttf</c>）复制到
    /// <c>Global.Home/fonts/</c>，让 <see cref="WayCoder.Infra.FontFinder"/> 能按文件找到它。
    ///
    /// **为什么必须复制**：MAUI 的 `MauiFont` 打进去是 Android **asset**（只能经 asset API 读，
    /// 没有文件路径），而画布文字走的是 `TrueTypeFont` —— 它只认**文件路径**。
    ///
    /// **为什么非要它**：Android 系统里唯一带中日韩字形的只有 `NotoSansCJK-*.ttc`，而那是
    /// **CFF(OTF) 轮廓**，本仓库那个只支持 glyf 的 TrueType 解析器读不了（实测：找到 208 个
    /// 系统字体，`Resolve` 却返回 null），结果是画布上的中文全渲染成豆腐块。
    /// Sarasa 是 glyf 轮廓 + 全中文覆盖，正好补上这一块。
    /// </summary>
    static void EnsureBundledFonts()
    {
        try
        {
            var dir = Path.Combine(WayCoder.Global.Home, "fonts");
            Directory.CreateDirectory(dir);
            foreach (var name in new[] { "SarasaMonoSC-Regular.ttf" })
            {
                var dst = Path.Combine(dir, name);
                if (File.Exists(dst) && new FileInfo(dst).Length > 0) continue;   // 已经落过
                using var src = FileSystem.OpenAppPackageFileAsync(name).GetAwaiter().GetResult();
                using var fs = File.Create(dst);
                src.CopyTo(fs);
            }
        }
        catch (Exception ex)
        {
            // 字体落不下来不该拦住启动：画布文字退化成豆腐块，其余功能照常
            ErrorLog.Error("MauiBootstrap", "释放内置字体失败", ex);
        }
    }

    /// <summary>
    /// 把随包的**示例程序**解到工作区 <c>examples/&lt;语言&gt;/</c> 下（只做一次，靠一个标记文件判断）。
    ///
    /// 为什么解到**工作区**而不是留在 <c>Global.Home/vml/</c>：手机端的工作目录就是工作区，
    /// 而沙箱只允许程序读写项目内 —— 放在 home 下的话用户得先 `cd` 出去，还会被沙箱拦。
    /// 解到工作区，`vml run examples/c/gomoku.c` 直接就能跑，在「文件」页里也看得见。
    ///
    /// 直接读 APK 里的 `vml_lib.zip`（而不是等 VML 标准库解压）：示例要**开箱即用**，
    /// 不能等用户第一次跑 VML 才出现。
    ///
    /// **按语言分目录**（v0.96.184）：包里就是 `Examples/&lt;语言&gt;/&lt;文件&gt;`，解出来保持同形 ——
    /// 从前是**平铺**的（丢掉目录名），于是二十来个示例（C/Python/BASIC/C#/Java/…）全堆在
    /// `examples/` 一个目录里，混着十来种语言、看文件名猜语言。现在 `examples/c/tetris.c`。
    /// ⚠ 路径变了：`vml run examples/tetris.c` → `vml run examples/c/tetris.c`。
    /// </summary>
    static void EnsureExamples()
    {
        try
        {
            var dir = Path.Combine(WorkspaceDir, "examples");
            var marker = Path.Combine(dir, ".unpacked");

            // 标记里存**版本号**而不是只看"文件在不在"：示例集随版本增删，
            // 只判存在的话老用户永远看不到新示例、也留着一堆已被删掉的旧文件。
            if (File.Exists(marker) && File.ReadAllText(marker).Trim() == WayCoder.Global.Version
                && Directory.EnumerateFiles(dir).Any(f => Path.GetFileName(f) != ".unpacked"))
                return;

            using var zipStream = FileSystem.OpenAppPackageFileAsync("vml_lib.zip").GetAwaiter().GetResult();
            using var zip = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read);

            // 先算出**这一次要落地的相对路径**，再决定清什么 —— 清空必须精确：
            // ① 顶层散文件（v0.96.184 之前是平铺的，不清就是同一份示例两份：`tetris.c` + `c/tetris.c`）；
            // ② 我们管理的语言子目录（内容要与当前版本一致，不能累加）。
            // ⚠ 只清"包里有的那些"：用户自己在 examples/ 下建的目录不碰（那是他的文件）。
            var rels = new List<(string Rel, System.IO.Compression.ZipArchiveEntry Entry)>();
            foreach (var e in zip.Entries)
            {
                if (!e.FullName.StartsWith("Examples/", StringComparison.Ordinal)) continue;
                var rel = e.FullName["Examples/".Length..];
                if (rel.Length == 0 || rel.EndsWith("/", StringComparison.Ordinal)) continue;  // 目录条目
                rels.Add((rel, e));
            }
            if (rels.Count == 0) return;

            Directory.CreateDirectory(dir);
            foreach (var f in Directory.EnumerateFiles(dir))          // ① 顶层散文件
            {
                if (Path.GetFileName(f) == ".unpacked") continue;
                try { File.Delete(f); } catch { /* 删不掉就留着，不值得为它中断 */ }
            }
            // ② 语言子目录 —— 判据是"这个顶层段后面还有东西"（`c/tetris.c` 的 `c` 是目录；
            //    `README.md` 没有下一段，是散文件，不在这一轮处理）
            foreach (var sub in rels.Where(r => r.Rel.Contains('/'))
                                    .Select(r => r.Rel.Split('/', 2)[0]).Distinct())
            {
                var p = Path.Combine(dir, sub);
                if (Directory.Exists(p)) { try { Directory.Delete(p, recursive: true); } catch { } }
            }

            foreach (var (rel, entry) in rels)
            {
                var dst = Path.Combine(dir, rel.Replace('/', Path.DirectorySeparatorChar));
                var parent = Path.GetDirectoryName(dst);
                if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                using var s = entry.Open();
                using var f = File.Create(dst);
                s.CopyTo(f);
            }

            File.WriteAllText(marker, WayCoder.Global.Version);
        }
        catch (Exception ex)
        {
            // 示例解不出来不该拦住启动
            ErrorLog.Error("MauiBootstrap", "释放示例程序失败", ex);
        }
    }

    public static Task WarmupConfigAsync() => Task.Run(() =>
    {
        try { _ = Config.Instance; }
        catch (Exception ex) { ErrorLog.Warning("MAUI", "配置预热失败", ex); }
    });
}
