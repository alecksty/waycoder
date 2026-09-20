using VMLRuntime;
using WayCoder.Tools;          // CwdContext：BGM 路径要与文件工具用同一把尺子解析
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;

namespace WayCoder.Maui.Services;

/// <summary>
/// **手机端** VML 的宿主 syscall 处理器 —— `ISystemCallHandler` 的那层薄壳。
///
/// ## 逻辑在哪
///
/// 全部在 <see cref="VmlHostRuntime"/>（`WayCoder/UI/Shared/VmlHostRuntime.cs`，**与桌面端同一份**）。
/// 本文件只剩两件事：
///   ① 把 VM 递进来的东西转交给共享层（<see cref="HandleSyscall"/>），
///      并把 `float8`/`long4`/`double4` 要用的那三组寄存器从运行时上接过去（见 <see cref="Vm"/>）；
///   ② 实现 <see cref="IVmlHost"/> —— 也就是**手机与桌面真的不一样的那十几件事**
///      （导航到绘图页、弹原生确认框、Preferences 存档、DeviceDisplay 常亮…）。
///
/// ## 为什么要拆（而不是各端各写一份大的）
///
/// 拆之前这 1000 行里九成是逻辑（坐标钳位、字符串读取、消息队列语义、定时器暂停、
/// 刷子/样式状态机），只有一成是平台。**平台那一成被逻辑夹在中间**，于是桌面端
/// （`scripts/vmlcli`）只能选"重抄一遍"或者"空着"—— 它选了空着，结果是
/// 「手机上能跑的游戏在桌面上什么也证明不了」，每改一行都得打 APK 上模拟器（一分多钟）。
/// 抄一份更糟：本仓库的头号坑就是**同一规则两处实现**，而分叉的症状恰好是
/// 「手机上对、桌面上错」—— 桌面 CLI 存在的意义就是给手机端当验收脚手架，它一错就更验不出来。
///
/// ## 为什么走 <see cref="ISystemCallHandler"/> 而不是改运行时
/// 运行时的 dispatch 是「先问宿主处理器，再走内置 switch」（`VMLRuntime.Syscall.cs:23`），
/// 所以宿主可以完全拥有一个号段；再配合「宿主把号段加进 `SyscallConstants.UserAllowed`」
/// 过掉 mcu 模式那道白名单门（<see cref="EnsureReservedSyscallsAllowed"/>），
/// **`third_party/vml` 一行都不用改**。
///
/// ## 线程模型（唯一需要小心的地方）
/// VM 跑在**后台线程**（`VmlTool` 是 Exclusive，见 MauiVml），UI 只能在主线程动。
/// 因此：
///   · 弹窗这类**要等用户**的调用 —— 在 VM 线程上同步等 `IWebInteraction` 的 Task
///     （桥内部自己 `MainThread.InvokeOnMainThreadAsync`，不会死锁，见 MauiWebInteraction 注释）；
///   · 开窗口/更新场景 —— 主线程投递，不阻塞 VM（保留模式，VM 只管往场景里追加图元）；
///   · 输入 —— VM 线程在 `VmlMessageQueue.Read` 上阻塞，UI 线程负责投递消息。
/// 与本仓库既有的「VM 线程阻塞等宿主输入」模型完全一致（`CaptureIo.ReadLine`）。
/// </summary>
internal sealed class VmlUiCalls : ISystemCallHandler
{
    /// <summary>共享的 syscall 实现（逻辑全在那边，本类只喂平台）。</summary>
    private readonly VmlHostRuntime _rt;

    /// <summary>本实例的手机侧宿主实现（对话框/窗口/存档/音频/屏幕）。</summary>
    private readonly MauiVmlHost _mauiHost = new();

    public VmlUiCalls()
    {
        // 日志出口：注册表本身不引用任何宿主的日志设施（见 VmlCallRegistry.Log）。
        VmlCallRegistry.Log ??= msg => ErrorLog.Warning("VmlCall", msg);
        // 注册本批那族自检调用口 —— **与桌面 CLI 同一批 id、同一份实现**（实现在 UI/Shared 里，
        // 两端各写一份迟早分叉，而分叉的症状是"手机上对、桌面上错"）。
        VmlCallRegistry.RegisterDefaults(VmlCallRegistry.HostMobile);

        _rt = new VmlHostRuntime(_mauiHost);
        // 手机端那个"入参诊断"脚手架（真机实测"对话框字符串大多是空的"时，唯一能分清
        // "程序没把指针放进寄存器"还是"宿主读错了内存"的办法）—— 挂在共享层的钩子上。
        _rt.OnSyscall = LogCalls;
    }

    /// <summary>
    /// `VmRuntime` —— **通用宿主调用口（577–580）需要它**。
    ///
    /// 为什么光有 `int[] registers` 不够：那四个口里 `float8`/`long4`/`double4` 的参数躺在
    /// **浮点/长整数/双精度寄存器组**里，而 `ISystemCallHandler.HandleSyscall` 只把**通用整数
    /// 寄存器**（32 位）作为参数递进来 —— 64 位值在那里面只剩低半截的镜像。
    /// 所以宿主得自己从运行时上取那三组数组（**活引用**，写进去就是写进 VM）。
    ///
    /// 由 `MauiVml` 在挂上处理器之后回填（构造 VM 时处理器就已经挂上了，那时拿不到 vm）。
    /// </summary>
    internal VmRuntime? Vm
    {
        get => _vm;
        set
        {
            _vm = value;
            // 三组寄存器是 `VmRuntime` 上的**活引用**，交给共享层之后
            // `CALLWITHLONG4/DOUBLE4/FLOAT8` 就能直接读写 VM（不用每调用一次搬一遍）。
            _rt.FloatRegisters = value?.FloatRegisters;
            _rt.DoubleRegisters = value?.DoubleRegisters;
            _rt.LongRegisters = value?.LongRegisters;
        }
    }
    private VmRuntime? _vm;

    /// <summary>最近一次运行用的处理器实例 —— 绘图页靠它把用户的触摸/按键投回队列。</summary>
    internal static VmlUiCalls? Current;

    /// <summary>当前场景变化时通知绘图页重绘。</summary>
    internal static Action<VmlScene>? OnSceneChanged;

    /// <summary>请求打开绘图页（由宿主在启动时注入，避免本类直接依赖 Shell 导航）。</summary>
    internal static Func<VmlScene, Task>? OpenWindowAsync;

    /// <summary>请求关闭绘图页。</summary>
    internal static Func<Task>? CloseWindowAsync;

    /// <summary>一次 VML 运行开始的清理：清队列、关掉残留定时器、丢弃上一轮的场景。</summary>
    internal void Reset()
    {
        Current = this; // 绘图页据此把输入投回本实例的队列
        _rt.Reset();
    }

    /// <summary>页面把输入事件投进来（UI 线程调用）。</summary>
    internal void PostInput(VmlMsgType type, int a = 0, int b = 0) => _rt.PostInput(type, a, b);

    /// <summary>页面被用户关闭时调用（返回箭头）。</summary>
    internal void MarkWindowClosed() => _rt.MarkWindowClosed();

    /// <summary>当前场景（绘图页要它来渲染）。</summary>
    internal VmlScene? Scene => _rt.Scene();

    /// <summary>
    /// 把保留号段加进运行时的用户态白名单。
    ///
    /// **漏了这一步的现象是「处理器注册了却永远不被调用」** —— 因为 mcu 模式下
    /// dispatch 顶部那道 `UserAllowedSyscalls.Contains` 会先把未知号拒掉，根本走不到处理器。
    /// `SyscallConstants.UserAllowed` 是公开的可变集合，且与运行时内部那份是**同一对象引用**
    /// （`VMLRuntime.cs:179`），所以加一次之后所有 VmRuntime 实例都放行。
    /// 用 `HashSet.Add` 的重复添加是幂等的，多跑几次也无害。
    ///
    /// ⚠ 这一步**只能留在各端**（不进 `VmlHostRuntime`）：共享层刻意不引用 `VMLRuntime`
    ///   （主工程 `WayCoder.csproj` 不引它，引了就没法自测）—— 与 `VmlCallRegistry` 同一条约束。
    ///   号段清单本身仍然只有一份（<see cref="VmlUi.ReservedRange"/>）。
    /// </summary>
    internal static void EnsureReservedSyscallsAllowed()
    {
        foreach (var n in VmlUi.ReservedRange()) SyscallConstants.UserAllowed.Add(n);
    }

    public bool HandleSyscall(int syscallNumber, int[] registers, byte[] memory, ref int pc)
        => _rt.HandleSyscall(syscallNumber, registers, memory);

    /// <summary>每个系统调用号最多记几条诊断（防刷屏）。</summary>
    private readonly Dictionary<int, int> _logged = new();

    /// <summary>
    /// 入参诊断：把**寄存器原值**与**按它读出来的字符串**一起落一条日志。
    /// 这两样缺一不可 —— 只记寄存器看不出"指针指向的内容对不对"，只记字符串则分不清
    /// "指针没传进来"和"内存读错了"。写到 <c>config/logs/error_YYYYMMDD.log</c>（adb 可直接读）。
    /// </summary>
    private void LogCalls(int n, int[] r, byte[] memory)
    {
        if (n is not (VmlUi.DlgMsg or VmlUi.DlgSelect or VmlUi.DlgMulti or VmlUi.DlgInput
            or VmlUi.WinOpen or VmlUi.DrawText or VmlUi.DrawIcon or VmlUi.DrawImage)) return;

        var seen = _logged.TryGetValue(n, out var c) ? c : 0;
        if (seen >= 3) return;
        _logged[n] = seen + 1;

        var regs = string.Join(",", r.Take(8));
        var reads = string.Join(" | ", new[] { 0, 1, 2 }.Select(i =>
            $"R{i}={r[i]}→'{VmlHostRuntime.Str(memory, r[i])}'"));
        ErrorLog.Info("VmlUi", $"#{n} regs=[{regs}] {reads} memLen={memory.Length}");
    }

    /// <summary>
    /// **实测的绘图视口**（dp）—— 由 <c>DrawWindowPage</c> 量到自己的画布区之后写进来。
    ///
    /// 为什么要实测而不是按屏幕算：`AvailableArea` 只能扣一个**固定**的 chrome 高度，
    /// 而真实占用（标题栏 + 手柄区 + 页面内边距）随设备与排版变。实测（1080×2400 模拟器）：
    /// 按屏幕算出来 744dp，而画布实际只有 578dp —— 于是程序按 744 排版，**底部一百多 dp
    /// 的内容（状态文字、计分板）全落在可视区外**，看着就像"没画出来"。
    ///
    /// 自纠正：第一次运行还没有实测值、先用估算；页面一量到就把真实值记下来，
    /// 之后每次运行都按真实值排版（因此**第二次运行起就完全贴合**）。
    /// </summary>
    internal static (int Width, int Height)? MeasuredViewport;

    /// <summary>
    /// 可用绘图区（绘图单位 = dp）。**优先用实测视口**，没有才退回按屏幕估算 ——
    /// 估算的扣减规则只有 <see cref="VmlUi.AvailableArea"/> 一处实现，这里不许再算一份
    /// （"同一规则两处实现"是本仓库的头号坑）。
    /// </summary>
    internal static (int Width, int Height) ScrArea()
    {
        // ⚠ **实测值要先问一句"它还算不算数"**：`MeasuredViewport` 只在绘图页活着时更新，
        // 而"在竖屏里打完一局 → 退出 → 转到横屏 → 再开一局"这条路上，转屏期间没有绘图页在跑，
        // 它还留着竖屏的 411×525 —— 横屏那一局照它开窗，画面就只剩中间一条
        // （实测：`orient=LANDSCAPE` 但 `wh=TALL`，`scene=411x525` 塞进 396×301 的画布）。
        // 方向对不上就不用它，退回 `AvailableArea`（那边**已经是分方向**算的）。
        if (MeasuredViewport is { } vp && vp.Width > 0 && vp.Height > 0
            && VmlUi.ViewportMatchesOrientation(vp.Width, vp.Height, ScreenOrientation()))
            return vp;
        try
        {
            var info = DeviceDisplay.MainDisplayInfo;
            return VmlUi.AvailableArea(info.Width, info.Height, info.Density);
        }
        catch
        {
            // 取不到显示信息时给一个保守的手机尺寸，而不是抛 —— 程序至少还能跑
            return (320, 480);
        }
    }

    /// <summary>宿主侧的 JSON 函数只注册一次（进程内）。</summary>
    private static bool _jsonHandlersReady;

    /// <summary>
    /// 注册走 <see cref="VmlUi.CallJson"/> 的**平台特有**函数。
    /// （`echo` / `screen` 两个语义必须逐字一致的由共享层注册，见 `VmlHostRuntime`。）
    ///
    /// **加一个能力 = 这里一行** —— 这正是这个"全能接口"存在的理由：
    /// 不占 syscall 号、不用碰 C 包装、不用重生成 22 种语言的绑定。
    /// </summary>
    internal static void EnsureJsonHandlers()
    {
        if (_jsonHandlersReady) return;
        _jsonHandlersReady = true;

        // `version`：让程序知道自己在哪个 App / 哪个版本上跑（写兼容分支时有用）。
        VmlJsonApi.Register("version", _ => JNode.Object()
            .Set("app", Global.AppName)
            .Set("cn", Global.AppNameCN)
            .Set("version", Global.Version)
            .Set("platform", DeviceInfo.Current.Platform.ToString().ToLowerInvariant()));

        // `sysinfo`：这台设备/这个 App 的系统信息。
        //
        // ⚠ **deviceId 是"本机安装实例的随机 id"，不是硬件序列号** —— 手机上拿硬件 id
        //   要么要权限（ANDROID_ID 在新版本已被限制），要么根本拿不到（IMEI 早就不让读了）。
        //   随机 id 一次生成、存进 Preferences，用途是"区分两台设备/两次安装"，
        //   不承担任何鉴权语义 —— **别拿它当设备指纹使**。
        VmlJsonApi.Register("sysinfo", _ =>
        {
            var info = DeviceDisplay.MainDisplayInfo;
            var area = ScrArea();
            return JNode.Object()
                .Set("app", Global.AppName)
                .Set("version", Global.Version)
                .Set("platform", DeviceInfo.Current.Platform.ToString().ToLowerInvariant())
                .Set("os", DeviceInfo.Current.Platform.ToString())
                .Set("osVersion", DeviceInfo.Current.VersionString)
                .Set("deviceModel", DeviceInfo.Current.Model)
                .Set("deviceName", DeviceInfo.Current.Name)
                .Set("manufacturer", DeviceInfo.Current.Manufacturer)
                .Set("arch", System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture
                    .ToString().ToLowerInvariant())
                .Set("cpuCount", Environment.ProcessorCount)
                .Set("memoryMb", GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024))
                .Set("deviceId", InstallId())
                .Set("screen", JNode.Object()
                    .Set("w", (int)Math.Round(info.Width / Math.Max(1, info.Density)))
                    .Set("h", (int)Math.Round(info.Height / Math.Max(1, info.Density)))
                    .Set("density", info.Density)
                    .Set("canvasW", area.Width)
                    .Set("canvasH", area.Height))
                .Set("orientation", ScreenOrientation());
        });
    }

    /// <summary>
    /// 本机安装实例的随机 id（首次用时生成并存进 Preferences，此后不变）。
    ///
    /// **刻意不用硬件标识**：ANDROID_ID 在新版 Android 上已按应用签名隔离、IMEI 早就不让读，
    /// 而且那类标识属于"设备指纹"，拿来做普通功能是过度收集。随机 id 够用来"区分两次安装"，
    /// 也随时可以清（清应用数据即换一个新的）。
    /// </summary>
    private static string InstallId()
    {
        try
        {
            const string key = "vml.installId";
            var id = Preferences.Default.Get(key, "");
            if (!string.IsNullOrEmpty(id)) return id;
            id = Guid.NewGuid().ToString("N");
            Preferences.Default.Set(key, id);
            return id;
        }
        catch
        {
            // 取不到 Preferences 也不能让 sysinfo 整个失败 —— 给个临时值（每次调用都不同）
            return "unknown";
        }
    }

    /// <summary>
    /// 屏幕方向（<see cref="VmlUi.Portrait"/> 竖屏 / <see cref="VmlUi.Landscape"/> 横屏）。
    ///
    /// **优先问设备**（`DeviceDisplay`），取不到才退回按实测视口推 ——
    /// 方向是"机器横着还是竖着拿"，不该被宿主怎么排版影响（手柄收起会改画布形状，
    /// 但机器并不会因此翻个身）。退回那一条在真机上不会走到，只是异常设备的兜底。
    /// 判定规则只有 <see cref="VmlUi.OrientationOf"/> 一处实现。
    ///
    /// ⚠ **这一处是唯一真源**：`SCR_ORIENT`（#569）查询走它，
    /// 方向变化时发的 <see cref="VmlMsgType.WindowOrient"/> 消息也走它 ——
    /// 两处各算一次的话，"查到的"和"收到的"会在某个边界上不一致，那是最难查的一类。
    /// </summary>
    internal static int ScreenOrientation()
    {
        try
        {
            var info = DeviceDisplay.MainDisplayInfo;
            if (info.Width > 0 && info.Height > 0) return VmlUi.OrientationOf(info.Width, info.Height);
        }
        catch { /* 取不到就往下退 */ }
        if (MeasuredViewport is { } vp && vp.Width > 0 && vp.Height > 0)
            return VmlUi.OrientationOf(vp.Width, vp.Height);
        return VmlUi.Portrait;
    }

    /// <summary>
    /// 取当前绘图窗口**最新呈现帧**的 DSL（即程序调 <c>ui_present</c> 时拍的快照）；
    /// 没开过窗、或程序从没调过 <c>ui_present</c> 时返回 null。
    ///
    /// 存在的理由：**让 AI 看得见自己写的图形程序**。<c>vml</c> 工具原先只回控制台文本，
    /// 而游戏是画出来的 —— 没有画面，AI 只能靠猜，而「程序没崩」根本不等于「画对了」。
    /// </summary>
    public string? TryGetPresentedDsl() => _rt.PresentedDsl;

    /// <summary>
    /// **手机的 <see cref="IVmlHost"/>** —— 与桌面的那一份（`scripts/vmlcli/CliVmlHost.cs`）
    /// 是本接口仅有的两个实现。凡是两个平台做法一致的东西都不该出现在这里。
    /// </summary>
    private sealed class MauiVmlHost : IVmlHost
    {
        public (int Width, int Height) ScreenArea() => ScrArea();

        public int Orientation() => ScreenOrientation();

        public bool OpenWindow(VmlScene scene)
        {
            // 开页面必须回主线程；VM 线程在这里等页面真正显示出来再继续，
            // 否则程序可能已经画完并退出、页面才姗姗来迟（用户什么都看不到）
            var open = OpenWindowAsync;
            if (open == null) return false;
            MainThread.InvokeOnMainThreadAsync(() => open(scene)).GetAwaiter().GetResult();
            return true;
        }

        public void CloseWindow()
        {
            var close = CloseWindowAsync;
            if (close != null) MainThread.InvokeOnMainThreadAsync(close).GetAwaiter().GetResult();
        }

        public void SceneChanged(VmlScene scene) => OnSceneChanged?.Invoke(scene);

        public int DlgMsg(string title, string body, int style)
        {
            var bridge = UxHelper.WebInteraction;
            if (bridge == null) return 0;
            // 询问样式走「是/否」，其余走「确定」—— 用同一个确认框，就不再新造一个只有 OK 的原生弹框
            var code = bridge.ConfirmAsync(title, body, allowAll: false, timeoutMs: 0)
                .GetAwaiter().GetResult();
            return code == 2 ? 1 : 0;
        }

        public int DlgSelect(string title, string prompt, IReadOnlyList<string> options)
        {
            var bridge = UxHelper.WebInteraction;
            if (bridge == null) return -1;
            var list = options.ToList();
            var picked = bridge.SelectAsync(title, list, timeoutMs: 0).GetAwaiter().GetResult();
            // 原生桥回的是 label（可能重复），取**第一个**匹配的下标 —— 与选项表顺序一致
            return picked == null ? -1 : list.IndexOf(picked);
        }

        public int DlgMulti(string title, string prompt, IReadOnlyList<string> options)
        {
            var bridge = UxHelper.WebInteraction;
            if (bridge == null) return -1;
            var list = options.ToList();
            var picked = bridge.MultiSelectAsync(title, list, timeoutMs: 0).GetAwaiter().GetResult();
            if (picked == null) return -1;
            var mask = 0;
            foreach (var p in picked)
            {
                var idx = list.IndexOf(p);
                if (idx >= 0 && idx < 31) mask |= 1 << idx;
            }
            return mask;
        }

        public string? DlgInput(string title, string prompt)
        {
            var bridge = UxHelper.WebInteraction;
            if (bridge == null) return null;
            return bridge.AskAsync(prompt, null, timeoutMs: 0).GetAwaiter().GetResult();
        }

        public void Tone(int hz, int ms, int wave, int volume) => VmlAudio.Tone(hz, ms, wave);

        public bool PlayAudio(string fullPath, bool loop) => VmlAudio.Play(fullPath, loop) == null;

        public void StopAudio() => VmlAudio.StopBgm();

        public void SetAudioVolume(int volume) => VmlAudio.SetVolume(volume);

        public bool Vibrate(int ms, int amplitude) => VmlAudio.Vibrate(ms, amplitude);

        public bool VibratePattern(long[] pattern) => VmlAudio.VibratePattern(pattern);

        public string? StoreGet(string key)
            => Preferences.Default.ContainsKey(key) ? Preferences.Default.Get(key, "") : null;

        public void StoreSet(string key, string value) => Preferences.Default.Set(key, value);

        public void StoreDel(string key) => Preferences.Default.Remove(key);

        /// <summary>
        /// 「别熄屏」**必须回主线程**。
        ///
        /// 真机实测：`DeviceDisplay.KeepScreenOn` 在 Android 上最终调的是
        /// `Window.AddFlags(FLAG_KEEP_SCREEN_ON)` —— 那是**动 View 层级**，从 VM 线程调直接抛
        /// `RuntimeException: Only the original thread that created a view hierarchy can touch its views`。
        ///
        /// ⚠ 宿主处理器**所有**分支都得按这条尺子过一遍：只碰数据的（Preferences / Vibrator / AudioTrack）
        /// 可以在 VM 线程上直接调，**凡是碰 View 的一律要 marshal**。
        /// 用 `BeginInvokeOnMainThread` 而不是 `InvokeOnMainThread`：前者不阻塞，
        /// 免得把 VM 线程挂在一次 UI 派发上。
        /// </summary>
        public void KeepScreenOn(bool on)
            => MainThread.BeginInvokeOnMainThread(() =>
            {
                try { DeviceDisplay.KeepScreenOn = on; }
                catch (Exception ex) { ErrorLog.Error("VmlUi", "设置常亮失败", ex); }
            });

        /// <summary>沙箱相对 → 绝对：`CwdContext` 与文件工具同一把尺子。</summary>
        public string ResolvePath(string relative) => CwdContext.Resolve(relative);

        public void RegisterJsonHandlers() => EnsureJsonHandlers();

        public void Log(string message) => ErrorLog.Warning("VmlUi", message);
    }
}
