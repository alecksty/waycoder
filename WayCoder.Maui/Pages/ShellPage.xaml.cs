using System.Text;
using WayCoder.Maui.Controls;
using WayCoder.Maui.Markup;
using WayCoder.Maui.Services;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Edit;   // DiagnosticManager：把编译诊断转交给编辑器（错误列表/波浪线/气泡同一份）

namespace WayCoder.Maui.Pages;

/// <summary>
/// 命令行页 —— 一个**模拟终端**（不是真 TTY）。
///
/// 为什么不做成真终端：手机上既没有 pty 也没有 terminfo，套一个就会掉进「转义序列要自己解、
/// 全屏程序要自己渲染」的无底洞。这里要的是「**能敲命令、能看输出、能知道自己在哪个目录**」，
/// 那就只做这三件事 —— 每次敲一条命令、等它跑完、把输出追加到下面。
///
/// 走的入口是 <see cref="BashTool.ExecuteUserShellAsync"/>（桌面 `!` 直通那条），
/// 语义是「**用户自己敲的**」：跳过 BashGuard 的黑名单（curl/npm/sudo 这类面向 AI 的拦截），
/// 但**绝对红线仍然生效**（`rm -rf /`、fork 炸弹、dd 写盘…）。
///
/// ⚠ iOS 上这个页面是个「礼貌的拒绝」：`BashTool` 在那边是 CoreStubs 的桩
/// （iOS 沙箱物理拒绝 fork/exec），敲什么都会返回一句「移动端不支持」。
/// </summary>
public partial class ShellPage : ContentPage
{
    /// <summary>
    /// 输出上限（字符）。超了从**头部**砍 —— 终端语义就是「旧的滚出去」。
    /// 不设上限的话，`yes` 这类命令能一路把内存吃满。
    /// </summary>
    /// <summary>
    /// 回滚缓冲上限（**行**）。超了从最老的一行开始丢 —— 就是终端 scrollback 的语义
    /// （`screen`/`tmux` 的 `history-limit`、`Terminal.app` 的行数限制都是这个模型）。
    ///
    /// 为什么按**行**而不是按字符：终端里"滚出去"的单位本来就是行，按字符裁会把一行
    /// 从中间劈开，屏幕上就出现一条断头的半行 —— 看着像渲染坏了。
    /// </summary>
    // 回滚行上限改为**可配置**（见 MauiShellStore.Scrollback，默认值与从前写死的 256 一致）。


    /// <summary>
    /// 交互式运行的超时（秒）。给得宽是**必须的**：VM 的超时是从 `Run()` 起就走的**墙钟**，
    /// **用户思考与打字的时间也在里面**。用非交互那个口径（几十秒），手机上敲慢一点
    /// 程序就会在提示符上被超时杀掉，而报的是"超时"、完全看不出是在等人。
    /// </summary>
    private const int InteractiveTimeoutSec = 600;

    /// <summary>
    /// 回滚缓冲，**一项 = 一整行**（不含 `\n`）。最后一项可能是"还在写、尚未换行"的半行 ——
    /// 用 <see cref="_partial"/> 记着，下一段写进来时接上去，而不是另起一行。
    /// </summary>
    /// <summary>回滚缓冲的一项 = 一行 + **这一行要不要参与折行**。</summary>
    /// <remarks>
    /// 为什么行上要带这个标志：**全屏程序的输出是一张"画面"，不是文本** ——
    /// 它的每一行就是屏幕上的一行，折一下整幅图就斜切了。
    ///
    /// ⚠ 判据必须是**整块**的，不能逐行猜。第一版按"可见字符全是空格"认画面行，
    ///   而**标题栏、菜单项、状态行都是有文字的**，于是它们照旧被折 ——
    ///   真机实测：80 列的网格被按自适应的 46 列折开，标题栏断成两行。
    /// </remarks>
    private readonly List<(string Text, bool NoWrap)> _lines = [];

    /// <summary>输出区字号 —— 画布的**唯一**尺子（格宽/格高都由它算）。</summary>
    private double _fontSize = MauiShellStore.DefaultFont;

    /// <summary>缓冲是否停在半行上（上一段没有以 `\n` 收尾）。</summary>
    private bool _partial;

    /* ── 全屏程序的光标 ──
     *
     * 记的是"**最近一块画面**"的光标（用户点名的：「光标位置也要显示光标，除非指令关闭了光标」）。
     * `_cursorBaseLine` = 那一块的第一行在 `_lines` 里的下标（-1 = 当前没有画面）；
     * 光标在缓冲里的行 = `_cursorBaseLine + _cursorRow`。列与显隐直接来自 `FrameBuffer`。
     *
     * ⚠ 为什么存"缓冲行号"而不是"显示行号"：折行、行数裁剪、回滚裁剪都会挪显示行号，
     *   而缓冲行号只在**回滚裁剪**时整体前移（那一处跟着减，见 `TrimScrollback`）。
     *   到 `DisplayText` 里再翻译成显示行号，只有一处换算。 */
    private int _cursorBaseLine = -1;
    private int _cursorRow;
    private int _cursorCol;

    /* ── 流式输出（命令行窗实时化）──
     *
     * 原先程序跑完才刷一次，于是 `top`/`vim`/`mc`/`cmatrix` 这一整类
     * （"不退出就一直画"）**运行期间屏幕上什么都没有**。
     *
     * `_stream` 把边跑边来的原始输出判成"追加文本"还是"整屏网格"（判据在
     * `ShellStream`，纯逻辑、桌面自测钉过）；这里只负责**落到 `_lines` 上**。
     *
     * ⚠ **网格是"替换那一块"不是"往后追加"** —— 这正是实时与整段缓冲最大的区别。
     *   块的首行就是 `_cursorBaseLine`（与光标那套共用一个基准，不留两份）。 */
    private ShellStream? _stream;
    private int _gridRows;
    private readonly object _chunkGate = new();
    private readonly List<string> _chunkQueue = [];
    private bool _chunkScheduled;
    private bool _cursorVisible;

    /// <summary>光标在**当前显示列表**里的行号（-1 = 不画）。每次 <see cref="DisplayText"/> 算出来。</summary>
    private int _cursorDisplayLine = -1;

    private readonly List<string> _history = [];
    private int _histIndex;
    private bool _busy;

    /// <summary>当前是否有程序在读 stdin（VM 线程写、UI 线程读，故用 volatile）。</summary>
    private volatile bool _interactive;

    /// <summary>正在等答案的那次读取；用户提交时由它把值交回 VM 线程。</summary>
    private TaskCompletionSource<string>? _stdinTcs;

    /// <summary>
    /// 当前这次 VML 操作的取消源（= 「强制停止」的手柄）。
    ///
    /// **做成静态的**：绘图页（游戏窗口）也要能停掉发起的那个进程 ——
    /// 用户按返回退出游戏时，程序可能正卡在自己的循环里不理会"窗口已关"的消息，
    /// 那时只有运行 token 能真正把它停下来。而 `DrawWindowPage` 拿不到 `ShellPage` 实例，
    /// 所以取消源与"被谁持有"解耦成静态。
    ///
    /// ⚠ **静态是安全的**：VML 的执行是**排他**的（`VmlTool.ExecutionMode = Exclusive`，
    /// 加上本页 `_busy` 闸门），同一时刻只可能有一次运行。
    ///
    /// 只有 VML 才有它：普通 shell 命令（`dotnet build` 之类）**没有可用的中断入口**，
    /// 所以"运行中不许返回"的拦截只对 VML 生效（见 <see cref="OnBackButtonPressed"/>）。
    /// </summary>
    private static CancellationTokenSource? _runCts;

    /// <summary>
    /// 本次运行的令牌，**专供交互输入的两个回调用**（`ReadLineFromProgram` / `ReadKeyFromProgram`）。
    /// 它们是 `Func<…>` 形参、拿不到 `ExecVmlAsync` 里的局部 `cts`，所以从这里过一道。
    /// </summary>
    private CancellationToken _programIoToken;

    /// <summary>
    /// 等一个"要用户输入"的任务，但**令牌一响就不再等**（抛 <see cref="OperationCanceledException"/>
    /// 往上走，让整次运行真正结束）。
    ///
    /// ⚠ 为什么不能只用 `.GetAwaiter().GetResult()`：程序卡在"等输入"上时**不执行指令**，
    /// 而 VM 的令牌检查是"每条指令一次" ⇒ 取消够不着它，强制停止停不掉它。
    /// 这与消息队列（`VmlMessageQueue`）、对话框（`MauiVmlHost.AwaitOrCancel`）是同一条理由，
    /// 三处必须一起认令牌 —— 漏一处就是"某类程序停不掉"。
    /// </summary>
    private T AwaitIoOrCancel<T>(Task<T> task)
    {
        var ct = _programIoToken;
        if (!ct.CanBeCanceled) return task.GetAwaiter().GetResult();
        try { task.Wait(ct); }
        catch (AggregateException) { /* 任务自己失败：交给下面那句抛真实异常 */ }
        return task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// 这次操作**彻底收干净**的信号（在 `ExecVmlAsync` / `CompileArtifactAsync` 的 `finally` 最末尾置位）。
    ///
    /// 为什么不是直接 `await` 那个运行 Task：它和调用处那句 `await task`
    /// 挂在**同一个 Task 的完成回调**上，谁先恢复没有保证 —— 若我们这边先恢复，
    /// 此刻 `_runCts` 还没被清空，重发的返回会撞上同一个拦截、再弹一次框。
    /// 用"finally 最后一步"的信号，就把它钉成了确定顺序。
    /// </summary>
    private static TaskCompletionSource? _runDone;

    /// <summary>
    /// **随时终止正在跑（或正在编译）的 VML 程序** —— 绘图页按返回时调它。
    ///
    /// 两段都停得掉：编译段是"带超时地等一个不可取消的编译"（`MauiVml` 的看门狗），
    /// 运行段是"主循环每条指令查一次 token"（即时）。
    /// </summary>
    internal static void CancelRunningVml() => _runCts?.Cancel();

    /// <summary>本页认识的命令（加命令改 <see cref="BuildCommandRegistry"/> 一处即可）。</summary>
    private readonly ShellCommandRegistry _commands;

    public ShellPage()
    {
        InitializeComponent();

        _commands = BuildCommandRegistry();

        // 等宽字体取自编辑器那份常量（与自绘编辑器同一个族）——
        // 不在这里另写字面量，否则将来换字体又是一处「同一规则两处实现」。
        // 等宽字体取自编辑器那份常量（与自绘编辑器同一个族）——文本 `Label` 与
        // **自绘网格**（`TerminalGrid`）用的是同一个字号，字体族由各自在创建时套用。
        PromptLabel.FontFamily = EditorTypography.FontFamilyName;
        CmdEntry.FontFamily = EditorTypography.FontFamilyName;

        ApplyDisplaySettings();

        // 点输出区把焦点给输入框（省得每次都要去点那个窄窄的 Entry）。
        AddOutputGestures(OutputGrid);

        Append("WayCoder 命令行\n" +
               "输入 shell 命令后按「运行」（或回车）。`cd` 会改变下面的工作目录。\n\n");
        RefreshCwd();
    }

    /// <summary>
    /// 文件页递过来的一件 VML 活：**跑**，或者**编**。
    ///
    /// 「在文件管理界面点，在命令行页出结果」是用户定的分工：文件页负责挑文件和做决策
    /// （编到哪、要不要覆盖），命令行页负责执行与**显示输出** —— 编译错误、程序输出、
    /// 运行时崩在哪一条，都是可能很长、要能滚的东西，塞进一个弹框里既看不全也留不住。
    /// </summary>
    /// <param name="Compile">true = 编译（产出下一级产物），false = 运行</param>
    /// <param name="SourcePath">**绝对路径**（文件页所在的子目录不一定是本页的 cwd）</param>
    /// <param name="OutputRel">编译产物的沙箱相对路径（运行时为 ""）</param>
    internal sealed record PendingVmlJob(bool Compile, string SourcePath, string OutputRel);

    /// <summary>
    /// 跨页交接用的信箱 —— 文件页放，本页 <see cref="OnAppearing"/> 取。
    ///
    /// 为什么不"拼一条 <c>vml run xxx</c> 命令再喂给自己"：命令是**按空白切分**的
    /// （见 <c>ShellCommandRegistry.Split</c>，它不做引号解析），路径里只要有空格就会断成两截，
    /// 而带空格的文件名恰恰最常见。这里直接交路径对象，绕开文本那一层。
    /// </summary>
    internal static PendingVmlJob? PendingVml;

    /// <summary>本页在 `AppShell.xaml` 里的 Route（`<ShellContent … Route="shell">`）。</summary>
    private const string ShellRoute = "shell";

    /// <summary>
    /// 切到本页（命令行 Tab）。
    ///
    /// ⚠ **不要用 `Shell.Current.GoToAsync("//shell")`** —— 真机实测它直接抛
    /// `ArgumentOutOfRangeException`（`IndexMustBeLess`），用户看到的是「无法打开命令行页」。
    /// 原因：Shell 的绝对路由串要一路穿过 `TabBar → Tab → ShellContent` 三层，而
    /// `AppShell.xaml` 里那几个 `<Tab>` **没有显式 `Route`**（MAUI 自动生成的），
    /// `//<ShellContent 的 Route>` 这种写法在这里解析不到 —— 报的还不是"路由不存在"，
    /// 是路由解析器内部越界，所以看错误信息也猜不到。
    ///
    /// 改成**直接指定 Shell 的当前项**：找到 `Route == "shell"` 的 ShellContent，
    /// 把它的三层依次设为当前 —— 这就是点 Tab 时系统自己做的事，不经过任何路由字符串。
    /// 切完同样会触发 `OnAppearing`，交接的活儿照跑。
    /// </summary>
    internal static bool SwitchToShellTab()
    {
        var shell = Shell.Current;
        if (shell == null) return false;

        foreach (var item in shell.Items)          // TabBar
            foreach (var section in item.Items)     // Tab
                foreach (var content in section.Items)   // ShellContent
                    if (content.Route == ShellRoute)
                    {
                        shell.CurrentItem = item;
                        item.CurrentItem = section;
                        section.CurrentItem = content;
                        return true;
                    }
        return false;
    }

    /// <summary>
    /// 把一件 VML 活交给本页（并切过来）。**唯一实现** —— 文件页与编辑器共用一份。
    ///
    /// 各写一份的后果很具体：信箱清空的时机、切页失败的回退、错误日志，任何一处改动都会漂移，
    /// 而这条链正是「点了没反应」的高发区。
    ///
    /// 返回 false = 没切过去（调用方负责提示用户）。
    /// </summary>
    internal static bool HandOff(PendingVmlJob job)
    {
        PendingVml = job;
        if (SwitchToShellTab()) return true;

        // 没切过去就把信箱清掉 —— 留着它会让用户下次**碰巧**进命令行页时
        // 莫名其妙地跑起一个程序（这是这条交接唯一的坑）。
        PendingVml = null;
        ErrorLog.Error("ShellPage", "找不到「命令行」页（AppShell.xaml 的 Route 变了？）", null);
        return false;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshCwd();
        // 显示设置在侧栏改，回到本页时应用（字号 / 固定列数 / 固定行数 / 回滚上限）。
        ApplyDisplaySettings();
        RenderOutput();                          // 让新设置立刻反映到已有输出（重算折行/字号）
        UpdateSizeButtons();                     // 侧栏可能改过尺寸，按钮高亮要跟上
        Dispatcher.Dispatch(UpdateScrollBar);   // 回到本页时量一次（期间可能转过屏）

        // 文件页递过来的活：等本页真的显示出来再干（切 Tab 会走这里）。
        if (Interlocked.Exchange(ref PendingVml, null) is { } job)
            Dispatcher.Dispatch(() => _ = RunPendingVmlJobAsync(job));
    }

    private async Task RunPendingVmlJobAsync(PendingVmlJob job)
    {
        // 上一轮还在跑就不插队 —— VML 的 DeviceManager 是进程级单例，两轮并发会互相踩
        // （见 VmlTool 的 ExecutionMode 注释）。这里只提示，不排队：用户再点一次即可。
        if (_busy)
        {
            Append("⚠️ 上一条命令还在运行，等它结束再回文件页点一次。\n\n");
            return;
        }

        // 异常由 RunWithPromptAsync（`RunFileAsync` / `CompileArtifactAsync` 都会走到它）兜住并打印。
        // 不兜的话：这条链是 `Dispatcher.Dispatch(() => _ = RunPendingVmlJobAsync(job))` 起的，
        // 那个 `_ = ` 把 Task 丢掉了，异常逃出去只会变成「未观察的任务异常」落进日志，
        // **屏幕上什么也没有**（实测踩过：一个例子编译时抛 `未找到标签: asm`，
        // 用户看到的就是"点了运行，然后什么都没发生"）。
        if (job.Compile) await CompileArtifactAsync(job.SourcePath, job.OutputRel);
        else await RunFileAsync(job.SourcePath);

        PublishDiagnosticsToEditor(job.SourcePath);
    }

    /// <summary>
    /// 把这次 VML 编译的诊断转交给编辑器（错误列表 / 行下波浪线 / 编译气泡读的是同一份数据）。
    ///
    /// <para>
    /// **为什么命令行页也要做这件事**：用户从文件页点「VML 运行」、或在命令行页敲
    /// `vml run …`，那条路**完全不经过编辑器** ⇒ 编辑器的「错误列表」永远是空的、
    /// 代码上也不出气泡与波浪线。用户的实测反馈就是这句：
    /// **「明明有报错，错误列表是空的，编辑器也不出错误泡泡」**。
    /// 而编译失败的原因**本来就有结构化的一份**（<see cref="MauiVml.LastDiags"/>），
    /// 此前只在编辑器自己那条路上被用掉了。
    /// </para>
    ///
    /// <para>
    /// ⚠ **键必须与编辑器一致**：编辑器拿**工作区相对路径**当 `DiagnosticManager` 的键
    /// （`EditorPage._relPath`），所以这里走 `ToRelative` 而不是绝对路径 ——
    /// 键对不上时注入了也读不到，而且**不报错、只是"没反应"**，是本仓最难查的一类失败。
    /// </para>
    ///
    /// <para>
    /// 成功时注入**空表**（与编辑器那条路同一口径）：否则上一次的报错会一直挂在列表上。
    /// </para>
    /// </summary>
    private static void PublishDiagnosticsToEditor(string absPath)
    {
        try
        {
            var rel = SandboxFsService.ToRelative(absPath);
            if (rel is null) return;          // 沙箱外：编辑器也打不开，没有可注入的对象
            DiagnosticManager.Inject(rel, MauiVml.LastDiags);
        }
        catch
        {
            // 注入诊断失败不该影响这一轮的运行结果（这只是"顺带把错误告诉编辑器"）
        }
    }

    /// <summary>
    /// 装上「静默等待」提示钩子：解压标准库 / 前端编译这类**几秒钟屏幕一个字不变**的阶段，
    /// 把它们的状态写进输出区（见 <see cref="MauiVml.OnProgress"/> 注释）。
    ///
    /// 回调**在后台线程上触发**（解压与编译都跑在 `Task.Run` 里），而 `Append` 动的是控件 ——
    /// 必须切回主线程；用 `BeginInvokeOnMainThread` 而不是 `InvokeOnMainThread`：
    /// 前者不阻塞调用方，免得把正在解压的那个线程反过来拖住。
    ///
    /// ⚠ **只在本次运行期间装着、`finally` 里清掉**：它是静态的，留着的话聊天那边跑个 VML
    /// 也会往这一页冒提示。
    /// </summary>
    private void InstallVmlProgress()
        => MauiVml.OnProgress = msg => MainThread.BeginInvokeOnMainThread(() => Append(msg + "\n"));

    /// <summary>跑一个 VML 文件（复用 <see cref="ExecVmlAsync"/>，不另起一份执行体）。</summary>
    private Task RunFileAsync(string absPath)
        // 路径**缩写成 `~/…`** —— 文件页递过来的是绝对路径，原样打出来要占两行（用户点名要短）。
        // 进度钩子（解压/编译提示）在 ExecVmlAsync 里装。
        => RunWithPromptAsync($"vml run {SandboxFsService.Abbreviate(absPath)}",
            () => ExecVmlAsync(null, absPath), markupResult: true, stream: true);

    /// <summary>
    /// 把源文件编成下一级产物写到沙箱里（`main.c` → `main.vml`、`main.vml` → `main.vmb`）。
    ///
    /// ⚠ **这里不做"要不要覆盖"的询问** —— 那是文件页的事：得在能看见文件列表的地方问，
    /// 而且只有那边知道用户选的是"覆盖"还是"换个名字"。本方法假定目标路径已经定下来了。
    /// </summary>
    private Task CompileArtifactAsync(string absPath, string outRel)
    {
        var toVmb = outRel.EndsWith(".vmb", StringComparison.OrdinalIgnoreCase);
        InstallVmlProgress();   // 首次编译同样会卡在解压标准库上，一样要报状态

        return RunWithPromptAsync($"vml build {SandboxFsService.Abbreviate(absPath)} → {outRel}", async () =>
        {
            // 编译也要装取消源：前端编译在手机上**一分多钟**，用户随时按返回应该能停下
            // （停的是"等待"，那个编译线程本身没法中止 —— 见 MauiVml.BuildProgram 的看门狗说明）。
            _runCts = new CancellationTokenSource();
            var cts = _runCts;
            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _runDone = done;
            try
            {
                // 编译/汇编都是同步阻塞的（前端编译本身就吃 CPU），必须离开 UI 线程 —— 否则整页卡死。
                var (note, error) = await Task.Run<(string? Note, string? Error)>(() =>
                {
                    if (toVmb)
                    {
                        var (bytes, err) = MauiVml.AssembleVmlToVmb(absPath);
                        if (bytes == null) return (null, err);
                        try { SandboxFsService.WriteBytesAtomic(outRel, bytes); }
                        catch (Exception ex) { return (null, $"⚠️ 写入失败：{ex.Message}"); }
                        return ($"{bytes.Length:#,0} 字节", null);
                    }

                    var (text, err2) = MauiVml.CompileToVml(absPath, cts.Token);
                    if (text == null) return (null, err2);
                    try
                    {
                        // 原子写 + UTF-8 **不带 BOM**：产物动辄几十万字符，写到一半崩掉会留下半截文件；
                        // 带 BOM 则会让汇编器认不出首行的 `.entry`。
                        SandboxFsService.WriteTextAtomic(outRel, text, new System.Text.UTF8Encoding(false), crlf: false);
                    }
                    catch (Exception ex) { return (null, $"⚠️ 写入失败：{ex.Message}"); }
                    return ($"{text.Length:#,0} 字符", null);
                });

                // 编译报错**套红**（用户要的就是"一眼看出出事了"）：这段是给人看的，
                // 所以整个 RunWithPromptAsync 走 markup 支（见下面的 markupResult: true）
                return error != null
                    ? "«red»" + error.TrimEnd() + "«/»"
                    : $"✔ 已生成 {outRel}（{note}）\n回文件页点它选「VML 运行」即可执行。";
            }
            finally
            {
                _runCts = null;
                cts.Dispose();
                // 顺序同 ExecVmlAsync：先摘信号（此时 `_runCts` 已空）再放行等待方
                _runDone = null;
                done.TrySetResult();
            }
        }, markupResult: true);   // 本分支自己产出 markup（上面那句套红），别再转一遍
    }

    /// <summary>
    /// 尺寸变了要重新量：视口变高可能让"本来超屏"变成"不超屏"，滚动条得跟着消失。
    /// （只依赖 Scrolled 是不够的 —— 转屏后没有滚动事件，滚动条会赖着不走。）
    /// </summary>
    /// <summary>
    /// 离开本页时**把字号落盘**。
    ///
    /// ⚠ 这不是"顺手存一下"：捏合的收尾写在 `PinchEnded` 上，而那个事件**不保证一定来**
    ///   （手势被别处接走、页面被切走都可能是最后一下）。实测漂过一次：屏幕上字号 96、
    ///   存储里还是上一次的 6.75 ⇒ 标签按存储算出"自适应 96 列"（而当时代码读的正是存储），
    ///   重启之后字号还会跳回去。**"一次性的收尾挂在结束事件上"的第三条路就是"离开这一页"。**
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (Math.Abs(MauiShellStore.Font - _fontSize) > 0.01) MauiShellStore.Font = _fontSize;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        // 自适应模式的列数是**按屏宽算**的 ⇒ 视口一变就要重算（转屏、折叠屏展开都走这里）。
        _outputWidth = Math.Max(0, width - OutputAreaChrome);
        ApplyDisplaySettings();
        RenderOutput();                          // 列数变了，折行要重排
        UpdateSizeButtons();
        Dispatcher.Dispatch(UpdateScrollBar);
    }

    /// <summary>
    /// 提示符（**全页唯一的状态出处**）：<c>~&gt;</c> / <c>~/examples&gt;</c>。
    ///
    /// 路径用 <see cref="SandboxFsService.Abbreviate"/> 缩写（工作区根 = `~`）——
    /// 手机上完整路径是 `/storage/emulated/0/waycoder/workspace/examples`，
    /// 摆在输入框前面会把输入区挤没。
    /// </summary>
    private string Prompt => SandboxFsService.Abbreviate(CwdContext.Root) + ">";

    /// <summary>
    /// 刷新提示符。`cd` 之后工作目录会变，**必须跟着动** —— 不然用户不知道下一条命令在哪跑。
    /// 运行中不刷（那时提示符是 `⋯`，见 <see cref="SetBusy"/>），跑完的收尾会再刷一次。
    /// </summary>
    private void RefreshCwd()
    {
        if (!_busy) PromptLabel.Text = Prompt;

        // 缩写**失败**才记一行日志（成功是常态，记了就是刷屏）。这一行的价值在于：
        // 提示符显示错的时候，`CwdContext.Root` 与沙箱根到底哪个不一样，一眼就能看出来 ——
        // 否则只能靠猜（实测就为这个多跑了一轮装机）。
        if (!string.IsNullOrEmpty(CwdContext.Root) && Prompt.Length > 1 && Prompt[0] != '~')
            ErrorLog.Info("ShellPage", $"提示符未缩写：cwd={CwdContext.Root} 沙箱根={SandboxFsService.Root}");
    }

    /// <summary>
    /// 跑一条命令的**统一外壳** —— 「运行中 / 已结束」的边界全靠它：
    /// 起点写一行 <c>路径&gt; 命令</c>，终点再写一行空的 <c>路径&gt;</c> 表示"等下一个命令"。
    ///
    /// 三个入口（手敲的命令、文件页递来的运行、文件页递来的编译）共用这一份 ——
    /// 各写一遍的话，总有一个会漏掉收尾提示符，用户就又分不清"跑完了没有"了。
    /// </summary>
    /// <param name="markupResult">
    /// <paramref name="body"/> 的返回值**已经是 markup**（VML 那条路：
    /// <c>MauiVml.Run(..., markup: true)</c> 已经把裸 ANSI 翻过、并把 stderr 套了红）。
    /// 为真时不再过一次 <see cref="AnsiMarkup.ToMarkup"/> —— 那会把我们自己的
    /// <c>«red»</c> 转义成字面量，屏幕上直接打出「«red»」四个字符。
    /// 普通 shell 命令（`ls`/`cd`…）返回的是原始文本，保持默认 false。
    /// </param>
    /// <param name="stream">
    /// 边跑边出画面（**只给 VML 运行那条路开**）。
    ///
    /// ⚠ 为什么不做成"跟着 `markupResult` 自动开"：另一个 `markupResult:true` 的调用点是
    /// **VML 编译**，它产出的是"✔ 已生成 xxx.vml"这种短消息，流式对它毫无意义，
    /// 却要多担一份"输出被交出去两遍"的风险。**显式开关**，只有真正需要的那一处打开。
    /// </param>
    private async Task RunWithPromptAsync(string cmdLine, Func<Task<string>> body,
        bool markupResult = false, bool stream = false)
    {
        Append($"{Prompt} {cmdLine}\n");
        SetBusy(true);
        if (stream) StartStreaming();
        try
        {
            // ⚠ **画面不折行**：全屏程序的输出是一张"画面"，每一行就是屏幕上的一行。
            //   标志由产生它的 `MauiVml` 给出（**块级事实**），不在这里逐行猜 ——
            //   画面里的标题栏/菜单项/状态行**都是有文字的**，逐行猜必然漏
            //   （实测：80 列的网格被按自适应的 46 列折开，标题栏断成两行、边框全错位）。
            var bodyText = (await body()).TrimEnd();

            if (stream)
            {
                /* 流式：正文**已经边跑边交出去了**，这里只补两件事 ——
                   ① 把队列里剩下的块吃掉、并让 `ShellStream` 收尾（末尾没换行的那一截全靠它）；
                   ② 补**诊断尾巴**（运行时报错 / 被强制停止 / syscall 被拒）。
                      它是跑完才知道的，而"程序崩了、用户只看到没有输出"正是这个平台反复修过的故障。
                   ⚠ `bodyText` 在这里**必须丢掉**：它是全量，再 Append 一遍就是整段重复。 */
                FinishStreaming();
                if (MauiVml.LastDiagnostics.Length > 0)
                    Append(MauiVml.LastDiagnostics + "\n", alreadyMarkup: true);
                // ⚠ **VM 压根没跑起来**（编译失败 / 标准库清单为空 / 解压失败 …）
                //   ⇒ 一个字都没流出去，`bodyText` 就是**唯一**的一份。丢掉它 =
                //   屏幕上什么都没有 = 用户说的「点了没反应、没弹窗就结束了」
                //   （真机实测：BGI 程序编译报错，界面停在「正在编译…」，一个字都不显示）。
                //   判据 `LastRunStreamed` 在 `RunProgram` 里才置位 —— 编译失败那条早退路
                //   到不了它，而它每轮开头由 `MauiVml.ResetRunState()` 复位。
                //   与编辑器页 `RunInEditorAsync` 同一口径（那边一直是对的，这边漏了这支）。
                if (!MauiVml.LastRunStreamed && bodyText.Length > 0)
                    Append(bodyText + "\n\n");
                return;
            }

            bool isGrid = markupResult && MauiVml.LastOutputWasGrid;
            if (isGrid)
            {
                // 全屏程序的**光标**：记下"这一块从缓冲的第几行开始" + 程序报的行列与显隐
                // （`MauiVml.LastCursor`）。画布据此在网格上画一个光标方块 ——
                // 用户点名的「光标位置也要显示光标，除非指令关闭了光标」。
                // ⚠ 必须在 `Append` **之前**记基准行号，否则算出来会偏一整块。
                _cursorBaseLine = _lines.Count;
                (_cursorRow, _cursorCol, _cursorVisible) = MauiVml.LastCursor;
            }
            else _cursorBaseLine = -1;      // 不是画面：上一块的光标就作废了
            Append(bodyText + "\n\n", alreadyMarkup: markupResult, noWrap: isGrid);
        }
        catch (Exception ex)
        {
            ErrorLog.Error("ShellPage", "命令执行失败", ex);
            Append($"⚠️ 执行失败：{ex.GetType().Name}: {ex.Message}\n\n");
        }
        finally
        {
            // 顺序要紧：**先刷新 cwd、再置空闲** —— 提示符只在非忙碌时更新（见 RefreshCwd），
            // 反过来的话打印出来的收尾提示符还是旧路径（`cd` 之后就当场打脸）。
            if (stream) StopStreaming();   // 卸钩子（幂等）—— 漏了会让**下一次**运行的输出重复
            RefreshCwd();
            SetBusy(false);
            Append(Prompt + "\n");   // 收尾的提示符：执行完了，等下一个命令
        }
    }

    // ═══ 流式输出（命令行窗实时化）═══
    //
    // 三个方法分工：`Start/Stop` 管钩子的装与卸，`Finish` 管收尾，
    // `OnVmlOutputChunk` 是 VM 线程上的入口，`ApplyStreamRender` 是**唯一**决定
    // "追加还是替换"的地方。

    /// <summary>开始流式：建状态机 + 装钩子。</summary>
    private void StartStreaming()
    {
        // 行列取当前的终端尺寸（自适应档那两处会播 0 ⇒ `ShellStream` 自己退回 25/80，
        // 与老程序写死的 80×25 一致，见 `PublishTerminalSize` 那段注释）。
        _stream = new ShellStream(MauiVml.TermRows, MauiVml.TermCols);
        _gridRows = 0;
        lock (_chunkGate) { _chunkQueue.Clear(); _chunkScheduled = false; }
        MauiVml.OnOutputChunk = OnVmlOutputChunk;
    }

    /// <summary>
    /// 卸钩子。⚠ **必须在每次运行收尾时调** —— 留着的话下一次运行的输出会被
    /// 投给一个已经结束的流（而且没人排空队列），表现为"这次跑的输出跑到上次那块去了"。
    /// </summary>
    private void StopStreaming()
    {
        MauiVml.OnOutputChunk = null;
        _stream = null;
        _gridRows = 0;
        lock (_chunkGate) { _chunkQueue.Clear(); _chunkScheduled = false; }
    }

    /// <summary>
    /// 收尾。⚠ **先同步排空队列**再 `Finish()`：队列里的块是靠 `Dispatcher.Dispatch` 排上来的，
    /// `await body()` 恢复之后它们**不保证**已经跑过 —— 直接 `Finish` 会把它们排到收尾提示符后面去。
    /// </summary>
    private void FinishStreaming()
    {
        ApplyPendingChunks();
        if (_stream != null) ApplyStreamRender(_stream.Finish());
        _stream = null;
    }

    /// <summary>
    /// VM 线程上的回调（见 `MauiVml.OnOutputChunk` 的说明）。
    /// **只入队 + 排一次 UI 更新**，绝不在这个线程上碰 `_lines`。
    ///
    /// 把"每次回调都切线程"合并成"一段窗口内切一次"：全屏程序每秒能写几十次，
    /// 每次都切会把 UI 线程拖死。`_chunkScheduled` 就是那道闸门 —— 已经排了就不再排，
    /// 等 `ApplyPendingChunks` 把队列吃空、闸门才会重新打开。
    /// </summary>
    private void OnVmlOutputChunk(string chunk)
    {
        lock (_chunkGate)
        {
            _chunkQueue.Add(chunk);
            if (_chunkScheduled) return;
            _chunkScheduled = true;
        }
        Dispatcher.Dispatch(ApplyPendingChunks);
    }

    private void ApplyPendingChunks()
    {
        string[] chunks;
        lock (_chunkGate)
        {
            if (_chunkQueue.Count == 0) { _chunkScheduled = false; return; }
            chunks = [.. _chunkQueue];
            _chunkQueue.Clear();
            _chunkScheduled = false;   // 先放开闸门：处理期间新来的块要能再排一次
        }
        if (_stream == null) return;
        foreach (var c in chunks) ApplyStreamRender(_stream.Feed(c));
    }

    /// <summary>
    /// 把一次呈现请求落到 `_lines` 上 —— **全线唯一决定"追加还是替换"的地方**。
    /// 追加与替换混了会把画面搅烂（网格是一整屏，追加就是几十屏残影）。
    /// </summary>
    private void ApplyStreamRender(ShellRender? r)
    {
        if (r == null) return;

        if (!r.IsGrid)
        {
            // 线性：往后追加。行为与原来一致，只是**边跑边来**而不是跑完一次来。
            _cursorBaseLine = -1;      // 线性输出没有"画面光标"，上一块的光标就此作废
            _gridRows = 0;
            Append(r.Text, alreadyMarkup: true);   // `Append` 自己会 `RenderOutput`
            return;
        }

        // ── 网格：**替换那一块**（这就是"实时"与"整段缓冲"最大的区别）──
        var rows = r.Text.Length == 0 ? [] : r.Text.Split('\n');
        if (_cursorBaseLine < 0)
        {
            _cursorBaseLine = _lines.Count;    // 这一块从缓冲的第几行开始
            _gridRows = 0;
        }

        if (_gridRows > 0 && _cursorBaseLine + _gridRows <= _lines.Count)
            _lines.RemoveRange(_cursorBaseLine, _gridRows);
        foreach (var line in rows) _lines.Add((line, true));
        _gridRows = rows.Length;

        (_cursorRow, _cursorCol, _cursorVisible) = (r.CursorRow, r.CursorCol, r.CursorVisible);

        // ⚠ 回滚裁剪会把块开头的行丢掉，`_cursorBaseLine` 跟着前移（那套逻辑已有），
        //   但**它不知道 `_gridRows`** —— 不跟着缩，下一帧 `RemoveRange` 就会多删几行正文。
        int before = _cursorBaseLine;
        TrimScrollback();
        if (before >= 0 && _cursorBaseLine >= 0 && before != _cursorBaseLine)
            _gridRows = Math.Max(0, _gridRows - (before - _cursorBaseLine));

        RenderOutput();
    }

    private async void OnRunRequested(object? sender, EventArgs e)
    {
        if (_busy) return;

        var cmd = (CmdEntry.Text ?? "").Trim();
        if (cmd.Length == 0) return;

        CmdEntry.Text = "";

        // 历史：连续重复的不重复记（终端里连按两次回车不该塞两条一样的）
        if (_history.Count == 0 || _history[^1] != cmd) _history.Add(cmd);
        _histIndex = _history.Count;

        await RunAsync(cmd);
    }

    private Task RunAsync(string cmd)
    {
        // ⚠ **输出要不要再过一遍 ANSI 转换，两条分支是相反的**：
        //   · 本页注册表认识的命令（`vml`）**自己产出 markup** ⇒ 不能再转
        //   · 交给 shell 的（`ls --color`、`git status`）是**裸 ANSI** ⇒ 必须转
        // 从前这里没传 `markupResult`（默认 false）⇒ vml 的输出被**转了两遍**，
        // 屏幕上把 `«red»红«/»` 原样显示出来。判据从注册表取，不在这里写前缀判断。
        var markup = _commands.ResultIsMarkup(cmd);
        return RunWithPromptAsync(cmd, async () =>
        {
        // 页面自己的命令走注册表（`BuildCommandRegistry` 那一处登记）。
        // **只要注册表不认识，就原样交给 shell** —— 分派逻辑只有这一处，
        // 加命令改 `BuildCommandRegistry` 一行，help 列表/用法/参数校验全跟着变。
        var handled = await _commands.DispatchAsync(cmd);
        if (handled != null) return handled;

        // 直接 await（不额外包 `Task.Run`）：这里本就在后台异步链上，包一层毫无收益。
        // （历史上这里必须这样写，因为 `CwdContext` 用 `AsyncLocal<string>` 直接存值，
        //   `cd` 的写入传不回线程池之外；现在 CwdContext 存的是「盒子」、就地改内容，
        //   cd 能跨任务边界回传，限制已不存在。）
        return await new BashTool().ExecuteUserShellAsync(cmd);
        }, markupResult: markup);
    }

    /// <summary>
    /// 登记本页认识的全部命令 —— **加命令只改这里**。
    ///
    /// 每条记录同时带着「名字 / 参数格式 / 参数个数 / 说明 / 执行体」，
    /// 所以 `help` 列表、`命令 -h` 用法、参数校验都是从同一条数据推出来的，不会互相漂。
    /// </summary>
    private ShellCommandRegistry BuildCommandRegistry()
    {
        var reg = new ShellCommandRegistry();

        // help：不认参数时回列表（而不是报错），并明确指出"没这条命令"
        reg.Register(new ShellCommand(
            "help", "[命令]", "列出可用命令；给命令名则显示它的用法",
            "不带参数列出全部命令；带命令名显示该命令的用法。任何命令都可以用 `命令 -h` 看用法。",
            args =>
            {
                if (args.Count == 0) return Task.FromResult(reg.HelpText());
                var c = reg.Find(args[0]);
                return Task.FromResult(c != null
                    ? reg.UsageOf(c)
                    : $"⚠️ 没有这个命令：{args[0]}\n\n{reg.HelpText()}");
            },
            MaxArgs: 1));

        // clear / cls：清空输出区（等同右上角「清屏」）
        foreach (var name in new[] { "clear", "cls" })
            reg.Register(new ShellCommand(
                name, "", "清空上面的输出（等同右上角「清屏」）",
                "把输出区的回滚缓冲整个丢掉，回到干净的一屏。",
                _ => { MainThread.BeginInvokeOnMainThread(ClearOutput); return Task.FromResult(""); },
                MaxArgs: 0));

        // vml：不走 shell，转交进程内的虚拟机（iOS 没有 shell；Android 也不该为编译起进程）
        reg.Register(new ShellCommand(
            "vml", "test | run <文件> | make <工程.vmk> | help",
            "跑 VML 程序：`test` 跑内置自检，`run` 按扩展名派发（.vml 汇编 / .vmb 装载 / 其余 22 种语言编译）",
            "编译并运行一段 VML。`vml test` 跑内置自检程序；`vml run <文件>` 按扩展名自动派发"
            + "（`.vml` 走汇编，`.vmb` 直接装载字节码，`.c`/`.py`/`.rs` 等 22 种语言走各自前端编译器）。"
            + "路径相对下面显示的工作目录解析。"
            + "`vml make <工程.vmk>` 按 VML 工程文件编译（入口、头文件搜索路径、宏都写在 .vmk 里），"
            + "**只出产物不运行**。",
            args => RunVmlAsync(string.Join(' ', args.Prepend("vml"))),
            // 本命令的返回值**已经是 «» 标记**（`ExecVmlAsync` 走 `MauiVml.Run(markup: true)`，
            // 编译错误也套了红）⇒ 输出区不能再过一遍 AnsiMarkup，否则 `«` 被转义成 `««`、
            // 渲染端只还原一层，屏幕上剩下字面的 `«red»…«/»`（实测踩过）。
            ProducesMarkup: true));

        return reg;
    }

    /// <summary>
    /// 处理 <c>vml</c> 子命令。
    ///
    /// ⚠ **走 <see cref="VmlTool"/> 而不是直接调 <c>MauiVml</c>** —— 让「用户在这儿敲的」
    /// 与「AI 调工具」是**同一条路、同一个实现**。两份实现迟早会漂（本仓库排第一的坑），
    /// 而且这样一来在页面上敲一遍就等于把工具也测了。
    /// </summary>
    private async Task<string> RunVmlAsync(string cmd)
    {
        var rest = cmd.Length <= 3 ? "" : cmd[3..].Trim();
        if (rest.Length == 0 || rest == "help") return VmlUsage;

        if (rest == "test") return await ExecVmlAsync(MauiVml.HelloWorldAsm, null);

        if (rest.StartsWith("run ", StringComparison.Ordinal))
            // 路径在进后台线程**之前**解析（CwdContext 是 AsyncLocal）
            return await ExecVmlAsync(null, CwdContext.Resolve(rest[4..].Trim()));

        // `vml make <工程.vmk>` —— 按工程文件编译（入口/头文件路径/宏都写在 .vmk 里）。
        // ⚠ 与 `run` 一样：**路径必须在进后台线程之前解析**（CwdContext 是 AsyncLocal）。
        if (rest.StartsWith("make ", StringComparison.Ordinal))
            return await MakeVmlAsync(CwdContext.Resolve(rest[5..].Trim()));

        return $"⚠️ 不认识的 vml 子命令：{rest}（敲 `vml` 看用法）";
    }

    private const string VmlUsage =
        "用法：\n  vml test            跑内置的自检程序\n"
      + "  vml run <文件>      .vml 走汇编、.vmb 直接装载，"
      + "其余按扩展名自动选编译器（22 种语言）\n"
      + "  vml make <工程.vmk> 按工程文件编译，**只出产物不运行**";

    /// <summary>
    /// **按工程文件编译**（`vml make <工程.vmk>`）—— 产物是 `.vml` 汇编（或 `.vmb`），**不运行**。
    ///
    /// <para>
    /// 与「编译」和「运行」分开是同一个道理（见 `CompileArtifactAsync`）：`make` 是**构建**，
    /// 要跑再敲 `vml run <产物>`。混成一步的话，批量/CI 场景里没法只编不跑。
    /// </para>
    ///
    /// <para>
    /// 编译体在 <see cref="MauiVml.MakeProject"/>（全端唯一的编译入口 `MauiVml` 上），
    /// 这里只管：装进度钩子、放后台线程、把结果摆到屏幕上。
    /// </para>
    /// </summary>
    private async Task<string> MakeVmlAsync(string absPath)
    {
        InstallVmlProgress();   // 首次编译会卡在解压标准库上，一样要报状态
        await RunWithPromptAsync($"vml make {SandboxFsService.Abbreviate(absPath)}", async () =>
        {
            // 取消源：前端编译在手机上**一分多钟**，用户随时按返回该能停下
            //（停的是"等待"，那个编译线程本身没法中止 —— 见 MauiVml.BuildProgram 的看门狗说明）
            _runCts = new CancellationTokenSource();
            var cts = _runCts;
            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _runDone = done;
            try
            {
                // 同步阻塞（前端编译吃 CPU），必须离开 UI 线程
                var r = await Task.Run(() => MauiVml.MakeProject(absPath, cts.Token));

                if (r.Error != null) return r.Error;

                var sb = new System.Text.StringBuilder();
                sb.Append($"✔ 已写出 «bold»{r.OutRel}«/»（{r.SizeNote}）");
                // ⚠ 这些不是错误但**必须显示** —— 最要紧的是"多个源文件只取了一个"，
                //   静默丢掉几个 .c 的后果是"编过了、少了半个程序"。
                foreach (var n in r.Notes) sb.Append("\n«yellow»⚠️  ").Append(n).Append("«/»");
                return sb.ToString();
            }
            finally
            {
                done.TrySetResult();
                _runCts = null;
            }
        }, markupResult: true);

        // 结果已经由 `RunWithPromptAsync` 写进输出区了 ⇒ 这里返回空串，
        // 免得同一段提示被渲染两遍（`ExecVmlAsync` 之外的命令都是这个约定）。
        return "";
    }

    /// <summary>
    /// 真正跑一段 VML —— <c>vml run/test</c> 命令与文件页的「VML 运行」**共用这一份**
    /// （两个入口各写一遍的话，文件页那份迟早会漏掉交互式输入或超时口径）。
    /// </summary>
    /// <param name="source">内联 VML 汇编（与 file 二选一）</param>
    /// <param name="file">**已解析好的绝对路径**（命令行那条路走 CwdContext.Resolve，文件页直接给绝对路径）</param>
    private async Task<string> ExecVmlAsync(string? source, string? file)
    {
        // **走 `MauiVml.Run`，与 AI 调 `vml` 工具是同一条流水线、同一份派发**
        // （不给它第二份实现——本仓库排第一的坑就是"同一规则两处实现"）。
        // 区别只有两个：这里给得了**交互式输入源**、超时给得宽（见 InteractiveTimeoutSec）。
        _interactive = true;
        _runCts = new CancellationTokenSource();
        var cts = _runCts;
        // 交互输入的等待也要认这个令牌（见 AwaitIoOrCancel）—— 两个读取回调是
        // `Func<…>` 形参、拿不到局部 `cts`，所以过一道字段。
        _programIoToken = cts.Token;
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _runDone = done;
        InstallVmlProgress();   // 这里是**本页所有 VML 运行**的唯一入口（文件页递的 + 手敲的）
        try
        {
            // 两个输入源都给：**程序要哪个由它调的读接口决定**（`ReadString` → 按行、
            // `ReadChar` → 逐键），不需要它声明什么。
            return await Task.Run(() => MauiVml.Run(source, file, InteractiveTimeoutSec,
                ReadLineFromProgram, cts.Token, markup: true, readKey: ReadKeyFromProgram));
        }
        finally
        {
            _interactive = false;
            _runCts = null;
            _programIoToken = default;   // 令牌跟着这一轮走，别留给下一轮
            cts.Dispose();
            MauiVml.OnProgress = null;
            StdinPanel.IsVisible = false;
            // 顺序要紧：先摘掉信号（此时 `_runCts` 已经是 null），再放行等待方 ——
            // 于是被唤醒的「强制停止」看到的必然是"已经停了"的状态。
            _runDone = null;
            done.TrySetResult();
        }
    }

    /// <summary>
    /// **运行中不许直接离开**：先问「是否强制停止」——选"继续运行"就留在本页。
    ///
    /// 为什么必须拦：VML 程序可能是个死循环或一直在等输入，用户一按返回就把它丢在后台跑着
    /// （VM 线程还在烧 CPU、还可能占着 DeviceManager 单例），下次再跑一个就互相踩。
    /// 而 VML 的取消是即时的（主循环每条指令查 token），所以这里给得起一个真正的"停止"。
    ///
    /// ⚠ 只拦**VML 运行**（`_runCts` 非空）：普通 shell 命令没有中断入口，
    /// 弹一个"强制停止"却停不掉是骗人；那种情况交给系统正常返回。
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        if (_runCts is null) return base.OnBackButtonPressed();
        _ = ConfirmLeaveWhileRunningAsync();
        return true;   // 这一次返回由我们接管（答不答应都不交给系统）
    }

    private async Task ConfirmLeaveWhileRunningAsync()
    {
        var stop = await DisplayAlertAsync("程序还在运行",
            "VML 程序尚未结束。强制停止并离开吗？", "强制停止", "继续运行");
        if (!stop) return;   // 「继续运行」= 不停止 = 不返回

        Append("\n⏹ 正在强制停止…\n");
        _runCts?.Cancel();

        // **等那次运行真的收干净再放行**。不等的话 `_runCts` 还在，
        // 重发的返回会撞上同一个拦截、又弹一次框（甚至无限套娃）。
        if (_runDone is { } done) await done.Task;
        Append("⏹ 已停止。\n\n");

        // 现在才是"已经停止"状态，重发一次返回 —— 走的完全是系统原本那条路
        // （Shell 自己决定是回上一个 Tab 还是退出应用），我们不去猜。
#if ANDROID
        (Platform.CurrentActivity as AndroidX.Activity.ComponentActivity)?.OnBackPressedDispatcher.OnBackPressed();
#endif
    }

    /// <summary>VM 线程调用：把"程序要一行输入"变成页面上的一次问答，然后阻塞等答案。</summary>
    private string ReadLineFromProgram()
    {
        if (!_interactive) return "";   // 非交互轮次（不该发生）给空行，绝不死等

        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _stdinTcs = tcs;
            StdinEntry.Text = "";
            StdinPanel.IsVisible = true;     // 亮出输入行
            StdinEntry.Focus();
        });

        // **阻塞 VM 线程**在这里等 —— 这正是"程序在等输入"的语义。
        // 注意它同时也在扣 VM 的墙钟超时，所以 InteractiveTimeoutSec 给得很宽。
        // ⚠ 必须**认运行令牌**：否则程序卡在这一句上时，强制停止叫不醒它
        //   （用户报的"退出后程序还没结束"就是这一类；见 AwaitIoOrCancel）。
        string line;
        try
        {
            line = AwaitIoOrCancel(tcs.Task);
        }
        finally
        {
            // 被取消时也要把输入行收回去 —— 不然面板会留在屏上、`_stdinTcs` 也悬着。
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _stdinTcs = null;
                StdinPanel.IsVisible = false;
            });
        }

        MainThread.BeginInvokeOnMainThread(() => Append(line + "\n"));   // 回显，像真终端

        return line;
    }

    // ═══ 逐键直通（交互式程序）═══
    //
    // 交互式程序（`vim`/`mc`/`top`）要的是"按一下立刻拿到"，而原来那条路是
    // **敲满一行按回车**才交出去 —— 于是它们连 `q` 都退不出来。
    //
    // 两路输入**天然分开**，不需要程序声明什么：
    //   · 软键盘 → `TextChanged`（它只产生文字）
    //   · 外接键盘 → Android `View.KeyPress`（它产生**键码**，方向键/Esc/F1-F12 才有）
    // 中间统一成**字符流**：特殊键按**终端的老规矩**翻成 ANSI 序列（方向键 = `\x1b[A`），
    // 与真终端一致，ncurses 那类程序本来就认它。

    /// <summary>程序按键的队列（UI 线程投、VM 线程取）。见 `KeyQueue`。</summary>
    private readonly KeyQueue _keys = new();

    /// <summary>正在等**一个键**（而不是一行）。`TextChanged` 据此决定要不要投队列。</summary>
    private bool _keyWaiting;

    /// <summary>防重入：清空 `StdinEntry` 会再触发一次 `TextChanged`。</summary>
    private bool _clearingStdin;

    /// <summary>
    /// 取一个键（**阻塞**，VM 线程调）。与 <see cref="ReadLineFromProgram"/> 并列 ——
    /// 程序要哪个由它调的是 `ReadChar` 还是 `ReadString` 决定。
    /// </summary>
    private char ReadKeyFromProgram()
    {
        if (!_interactive) return '\0';     // 非交互轮次：给 NUL，绝不死等

        // 队列里已经有就直接拿，不必亮面板
        if (_keys.TryTake(out var queued)) return queued;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _keyWaiting = true;
            _clearingStdin = true;
            StdinEntry.Text = "";
            _clearingStdin = false;
            StdinEntry.Placeholder = "程序在等按键（按一下就是一下，不用回车）";
            StdinPanel.IsVisible = true;
            StdinEntry.Focus();
            HookHardwareKeyboard();
        });

        try
        {
            // **阻塞 VM 线程**在这里等（这就是"程序在等输入"）—— 同样要认令牌，
            // 否则逐键程序（vim/mc 那类）卡在等键上时停不掉。
            return _keys.Take(_programIoToken);
        }
        finally
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _keyWaiting = false;
                StdinEntry.Placeholder = "程序在等一行输入…";
            });
        }
    }

    /// <summary>软键盘：一个字符一个字符地投（**不等回车**）。</summary>
    private void OnStdinTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!_keyWaiting || _clearingStdin) return;

        // 取**新增**的那部分：退格是"变短"，`NewTextValue` 里没有可投的字符。
        var now = e.NewTextValue ?? "";
        var prev = e.OldTextValue ?? "";
        string added = now.Length > prev.Length ? now[prev.Length..] : "";

        foreach (var ch in added) _keys.Post(ch);

        // 清空，让"下一次敲的"永远是一个干净的新增。
        // ⚠ 清空自身会再触发一次 TextChanged ⇒ 靠 `_clearingStdin` 挡住（否则死循环）。
        if (added.Length > 0)
        {
            _clearingStdin = true;
            StdinEntry.Text = "";
            _clearingStdin = false;
        }
    }

    /// <summary>
    /// 外接键盘（Android）。⚠ **软键盘走不到这里** —— 它不产生 `KeyEvent`。
    ///
    /// 走 `OnKeyListener`（`View.KeyPress`）的理由与 `EditorPage` 那条相同：
    /// 这个回调**早于 View 的默认处理** ⇒ 我们能先吃掉，不让它再做焦点导航之类的事。
    ///
    /// ⚠ **一律 `Handled = true`**：不这么做的话，按下的字符会同时进 `StdinEntry`，
    /// 于是 `TextChanged` 再投一遍 —— 一次按键交给程序两个字符。
    /// </summary>
    private void HookHardwareKeyboard()
    {
#if ANDROID
        if (StdinEntry.Handler?.PlatformView is not Android.Widget.EditText et) return;
        et.KeyPress -= OnStdinHardwareKey;
        et.KeyPress += OnStdinHardwareKey;
#endif
    }

#if ANDROID
    private void OnStdinHardwareKey(object? sender, Android.Views.View.KeyEventArgs e)
    {
        var ke = e.Event;
        if (ke is null) return;
        // Down 与长按重复(Multiple)都算；Up 忽略，否则一次按键做两遍
        if (ke.Action != Android.Views.KeyEventActions.Down &&
            ke.Action != Android.Views.KeyEventActions.Multiple) return;

        e.Handled = true;                       // 见上面那条：否则会与 TextChanged 重复投递

        // ① 能产出字符的（含 Ctrl+字母 —— 那会给出控制码）：原样投
        int uni = ke.UnicodeChar;
        if (uni > 0)
        {
            // 代理对的情况 Android 会给高/低位两次 UnicodeChar，按 UTF-16 原样投即可
            _keys.Post((char)uni);
            return;
        }

        // ② 不产出字符的（方向键 / Esc / F1-F12 / Del / PgUp…）：按终端老规矩翻成 ANSI
        foreach (var ch in AnsiForAndroidKey(ke.KeyCode)) _keys.Post(ch);
    }

    /// <summary>
    /// Android 键码 → **终端 ANSI 序列**。
    /// 取值照 xterm 的老约定（方向键 `ESC [ A/B/C/D`、F1-F4 用 SS3 `ESC O P..S`），
    /// 因为 ncurses 那套本来就认它 —— 自己发明一套编号等于让程序解不出来。
    /// </summary>
    private static string AnsiForAndroidKey(Android.Views.Keycode kc) => kc switch
    {
        Android.Views.Keycode.Escape      => "\x1b",
        Android.Views.Keycode.Del         => "\x1b[3~",
        Android.Views.Keycode.ForwardDel  => "\x1b[3~",
        Android.Views.Keycode.DpadUp      => "\x1b[A",
        Android.Views.Keycode.DpadDown    => "\x1b[B",
        Android.Views.Keycode.DpadRight   => "\x1b[C",
        Android.Views.Keycode.DpadLeft    => "\x1b[D",
        Android.Views.Keycode.MoveHome    => "\x1b[H",
        Android.Views.Keycode.MoveEnd     => "\x1b[F",
        Android.Views.Keycode.PageUp      => "\x1b[5~",
        Android.Views.Keycode.PageDown    => "\x1b[6~",
        Android.Views.Keycode.F1 => "\x1bOP", Android.Views.Keycode.F2 => "\x1bOQ",
        Android.Views.Keycode.F3 => "\x1bOR", Android.Views.Keycode.F4 => "\x1bOS",
        Android.Views.Keycode.F5 => "\x1b[15~", Android.Views.Keycode.F6 => "\x1b[17~",
        Android.Views.Keycode.F7 => "\x1b[18~", Android.Views.Keycode.F8 => "\x1b[19~",
        Android.Views.Keycode.F9 => "\x1b[20~", Android.Views.Keycode.F10 => "\x1b[21~",
        Android.Views.Keycode.F11 => "\x1b[23~", Android.Views.Keycode.F12 => "\x1b[24~",
        _ => "",
    };
#endif

    private void OnStdinSubmitted(object? sender, EventArgs e)
    {
        // 逐键模式：回车就是**一个键**（`\r`），不是"交出一行"
        if (_keyWaiting)
        {
            _keys.Post('\r');
            _clearingStdin = true; StdinEntry.Text = ""; _clearingStdin = false;
            return;
        }

        var tcs = _stdinTcs;
        if (tcs == null) return;

        _stdinTcs = null;
        var line = StdinEntry.Text ?? "";
        StdinEntry.Text = "";
        tcs.TrySetResult(line);
    }

    /// <summary>
    /// 置「运行中 / 空闲」。**提示符是这一对状态的主信号**：
    /// 运行中显示 <c>⋯</c>（明确"在跑、别走开"），空闲显示 <c>路径&gt;</c>（明确"等你敲下一条"）。
    ///
    /// 光靠按钮上的「运行 / …」是不够的 —— 那个小按钮在屏幕右下角，
    /// 而输入框前面那一格才是眼睛一直停着的地方（用户报的就是"看不出运行中和结束的边界"）。
    /// </summary>
    private void SetBusy(bool busy)
    {
        _busy = busy;
        RunBtn.IsEnabled = !busy;
        RunBtn.Text = busy ? "…" : "运行";
        CmdEntry.IsEnabled = !busy;
        PromptLabel.Text = busy ? "⋯" : Prompt;
    }

    private void OnHistoryUpClicked(object? sender, EventArgs e)
    {
        if (_history.Count == 0) return;

        // 第一次按从最新的往上翻；连着按就继续往更早的翻，到顶就停在最早那条
        _histIndex = Math.Max(0, _histIndex - 1);
        CmdEntry.Text = _history[_histIndex];
        CmdEntry.CursorPosition = CmdEntry.Text.Length;
    }

    /// <summary>
    /// 顶栏「菜单」—— 常用操作的统一入口（原来是单独一个「清屏」按钮）。
    ///
    /// 用 `DisplayActionSheetAsync`（本 App 各页共用的那一套，见 FilesPage / EditorPage）：
    /// 系统原生弹层，不用自己维护一套浮层控件，返回 `null` = 用户点了「取消」（或返回键关掉）。
    ///
    /// ⚠ **字号的加减必须走 `SetFontSize`（落盘 + 重排 + 重画一整套）**，不能只改
    ///   `OutputGrid` 的字号 —— 那样列数、页宽、尺寸档的联动都会漏掉，
    ///   表现是"字号看着变了，但换行位置还是按老字号折的"。
    /// </summary>
    private async void OnMenuClicked(object? sender, EventArgs e)
    {
        var size = MauiShellStore.Font;
        var choice = await DisplayActionSheetAsync(
            $"命令行 · 字号 {size:0}", "取消", null,
            "加大字号", "减小字号", "重置字号", "复制整屏输出", "清空屏幕");

        switch (choice)
        {
            case "加大字号": SetFontSize(MauiShellStore.NextFont(size)); break;
            case "减小字号": SetFontSize(MauiShellStore.PrevFont(size)); break;
            case "重置字号": SetFontSize(MauiShellStore.DefaultFont); break;
            case "复制整屏输出": await CopyAllOutputAsync(); break;
            case "清空屏幕": ClearOutput(); break;
        }
    }

    /// <summary>
    /// **改字号**（菜单 / 捏合结束共用这一条）。
    ///
    /// 一次做四件事，少一件都不对：落盘（下次进来还是这个字号）、按新字号重排折行、
    /// 更新尺寸档按钮的文案（自适应档的列数是跟着字号算的）、重画。
    /// </summary>
    private void SetFontSize(double size)
    {
        _fontSize = MauiShellStore.ClampFont(size);
        MauiShellStore.Font = _fontSize;      // 落盘（`Font` 的 setter 里也夹一次范围）
        ApplyDisplaySettings();
        ApplyFontSizeLive();
        UpdateSizeButtons();
        RenderOutput();
        // 字号变了，一屏能放下的内容也变了 ⇒ 落点按当前屏幕模式重摆（同上）。
        // 内容仍然装得下时它算出来就是左上角（两个分支都是），不会把人晃到别处。
        Dispatcher.Dispatch(AlignOutput);
    }

    /// <summary>
    /// 复制整屏输出到剪贴板。
    ///
    /// ⚠ **输出区是自绘画布，没有文本选择** —— 这个入口是"把输出拿走"的唯一办法，
    ///   不是可有可无的便利功能。拷的是**逻辑行**（`_lines`，未经折行）：
    ///   折行是我们为了排版自己切的，拷出去会把一句话切断。
    /// ⚠ 中间格式的 `«»` 标记要**剥掉**再拷（`AnsiHelper.StripMarkup`）——
    ///   那是渲染用的编码，不是内容。
    /// </summary>
    private async Task CopyAllOutputAsync()
    {
        var text = string.Join("\n", _lines.Select(l => AnsiHelper.StripMarkup(l.Text)));
        if (text.Length == 0) return;
        try { await Clipboard.Default.SetTextAsync(text); }
        catch { /* 剪贴板不可用不该让页面崩 */ }
    }

    private void OnClearClicked(object? sender, EventArgs e) => ClearOutput();

    // ── 尺寸模式：**三个正交组合**（都不固定 / 横向固定 / 都固定）──
    //
    // 为什么不是"自动 vs 固定"两档：**横向固定**是独立的一档需求 —— 老程序按 80 列排表格
    // ⇒ 列必须钉死；而手机屏幕高度各家不同 ⇒ 行没必要钉死（钉死了输出区上下留白）。
    // 用户点名的原话：「都固定，或者横向固定，或者都不固定」（v0.96.335）。
    //
    // 交互约定：**点未生效的 = 切过去；点已生效的 = 换该档的预设**（列数 / 尺寸规格）。
    // 这样三个按钮就能覆盖"切档 + 选参数"，不必再为"选列数"单开一个入口。

    /// <summary>都不固定 —— 列数与行数都跟着屏幕走。</summary>
    private void OnAutoSizeClicked(object? sender, EventArgs e)
    {
        MauiShellStore.SetMode(ShellSizeMode.Auto);
        ApplySizeAndRedraw();
    }

    /// <summary>横向固定 —— 列钉死、行跟着屏幕。重复点换列数。</summary>
    private void OnWidthFixedClicked(object? sender, EventArgs e)
    {
        if (MauiShellStore.Mode == ShellSizeMode.WidthFixed)
            MauiShellStore.SetColumns(MauiShellStore.Next(MauiShellStore.ColsChoices, MauiShellStore.RawCols));
        else
            MauiShellStore.SetMode(ShellSizeMode.WidthFixed);
        ApplySizeAndRedraw();
    }

    /// <summary>都固定 —— 弹一列历史终端规格让用户挑（「允许可选」）。重复点重新弹。</summary>
    private async void OnFixedSizeClicked(object? sender, EventArgs e)
    {
        if (MauiShellStore.Mode == ShellSizeMode.Fixed)
        {
            var labels = MauiShellStore.SizePresets.Select(p => p.Label).ToArray();
            var pick = await DisplayActionSheetAsync("固定终端大小", "取消", null, labels);
            var idx = Array.IndexOf(labels, pick);
            if (idx < 0) return;                              // 取消 / 点了外面
            MauiShellStore.SetColumns(MauiShellStore.SizePresets[idx].Cols);
            MauiShellStore.SetRows(MauiShellStore.SizePresets[idx].Rows);
        }
        else
        {
            MauiShellStore.SetMode(ShellSizeMode.Fixed);
            // 首次切进这一档：沿用"横向固定"里已经选好的列数（用户多半是照着它调的），
            // 行数用默认值 —— 下次再点可以挑历史规格。
        }
        ApplySizeAndRedraw();
    }

    /// <summary>
    /// 改完尺寸设置后统一收尾：应用（含字号适配）→ **按新列数重画**（历史输出跟着重排，
    /// 与真终端一致）→ 刷新按钮高亮 → 重算滚动条。
    /// </summary>
    private void ApplySizeAndRedraw()
    {
        ApplyDisplaySettings();
        RenderOutput();
        UpdateSizeButtons();
        // **换档 = 换落点**（用户定的）：固定屏幕贴左上角、滚屏停在最后一屏。
        // 排到下一拍 —— 此刻画布还没按新内容量完，马上摆会被旧的尺寸算回去。
        Dispatcher.Dispatch(AlignOutput);
    }

    /// <summary>
    /// 三个尺寸按钮的当前态：**生效的那个用主色底 + 白字**，其余回到默认样式。
    /// 不这样做的话，用户看不出现在是哪一档（三个按钮都长得一样）。
    ///
    /// ⚠ 标签取的是 **<see cref="MauiShellStore.RawCols"/>/<see cref="MauiShellStore.RawRows"/>**
    /// 而不是生效值 `Cols`/`Rows` —— 后者在非对应模式下恒为 0，拿它拼标签会写出一堆
    /// 「横向固定 0」。**按钮要显示的是"点下去会变成什么"**，那正是各轴记住的那个值。
    /// </summary>
    private void UpdateSizeButtons()
    {
        var mode = MauiShellStore.Mode;

        // 自适应档把**算出来的列数**也显示出来 —— 否则用户不知道"自适应"到底是几列，
        // 也就没法判断手上这个老程序该不该切到固定档。
        // ⚠ 用**页面自己的 `_fontSize`**，不是 `MauiShellStore.Font`：屏幕上有多大是
        //   `_fontSize` 说了算（它才是喂给画布的那个），存储里的值只是"下次进来用多少"。
        //   两者**理论上**由 `SetFontSize` 一起写，但捏合的结束事件并不保证一定来
        //   （实测就这么漂过一次：页面 96、存储 6.75 ⇒ 标签显示"自适应 96 列"而字大得离谱）。
        //   标签跟着**看得见的那个值**走，漂了至少不会自己骗自己。
        var autoCols = ShellWrap.ColumnsForWidth(_outputWidth, _fontSize);
        AutoSizeBtn.Text = autoCols > 0 ? $"自适应 {autoCols} 列" : "大小自适应";
        WidthFixedBtn.Text = $"横向固定 {MauiShellStore.RawCols}";
        FixedSizeBtn.Text = $"固定 {MauiShellStore.RawCols}×{MauiShellStore.RawRows}";

        HighlightSizeButton(AutoSizeBtn, mode == ShellSizeMode.Auto);
        HighlightSizeButton(WidthFixedBtn, mode == ShellSizeMode.WidthFixed);
        HighlightSizeButton(FixedSizeBtn, mode == ShellSizeMode.Fixed);
    }

    /// <summary>缩放起点字号 —— 捏合过程中 <c>e.Scale</c> 是相对**起点**的累计值。</summary>

    /// <summary>
    /// 输出区**双指缩放字号** —— **三种尺寸模式下都生效，且是无极（连续）缩放**。
    ///
    /// 两条不变量（用户点名要的，别"顺手"改掉）：
    /// <list type="number">
    /// <item>**不按模式分档** —— 固定窗口 / 横向固定下缩放照样有效。此时列数钉死，
    ///   字号一变像素宽度就变 ⇒ 装不下由**横向滚动**兜（这正是"固定大小必然超出屏幕"的由来），
    ///   而不是回头去改列数。</item>
    /// <item>**字号一律连续取值，不吸附到整数、不吸附到"档位"** —— 捏合的 <c>e.Scale</c>
    ///   映射出来是多少就是多少（全仓在这条链上没有任何 <c>Round</c>/取整），
    ///   与移动端编辑器那次"拒绝只允许偶数号"是同一条诉求。</item>
    /// </list>
    ///
    /// ⚠ <c>Canceled</c> 与 <c>Completed</c> 都要收尾：来电 / 切走 App / 父容器截走触摸
    /// 走的都是 Canceled，只处理 Completed 的话状态会永远留在"捏合中"
    /// （本仓在移动端编辑器那轮踩过这条）。
    /// </summary>
    /// <summary>
    /// 输出区的手势接线：**点一下聚焦输入框** + 双指缩放（`PinchScaled`）。
    ///
    /// ⚠⚠ **一个 `GestureRecognizer` 都不能挂**（这里原来挂了个 `TapGestureRecognizer`，
    ///   已删）。理由是本仓实测出来的硬约束：只要给这个 `GraphicsView` 挂上**任何**手势
    ///   识别器，`StartInteraction`/`DragInteraction`/`EndInteraction` 就**全部不再触发**
    ///   —— 平台那层触摸被手势系统接走，画布自己那套事件再也收不到。
    ///   症状是**整块输出区对触摸毫无反应**（滑不动、也捏不动），而界面其它地方一切正常
    ///   （用户报的就是"好像卡死了"）。反证：编辑器画布一个手势识别器都没挂，它一直是好的。
    ///
    ///   所以"点一下"改成**画布自己按位移判**（<see cref="TerminalGrid.Tapped"/>），
    ///   与编辑器同一套做法。
    /// </summary>
    private void AddOutputGestures(TerminalGrid grid)
    {
        grid.Tapped += () => CmdEntry.Focus();
        grid.PinchScaled += OnOutputPinchScale;
        grid.PinchEnded += OnOutputPinchEnd;
    }

    /// <summary>
    /// 捏合期间**原地**套用新字号 —— 只改字号、不重建视图树。
    ///
    /// ⚠ 重建视图树会让**正在接手势的那个视图**被销毁 ⇒ 手势断掉（"捏一下就没反应"）。
    /// ⚠ 这一拍**不做折行重排**：`ShellWrap` 折行要把所有行重新切一遍，
    ///   而缩放是每帧都在变的 —— 留到手指抬起再做，期间让 Label 自己按视口宽排。
    /// </summary>
    private void ApplyFontSizeLive()
    {
        // 一个画布：改字号重画一遍就行。**没有控件树要动**，所以手势不会被自己的销毁打断。
        // ⚠ 这一拍**不做折行重排**（`ShellWrap` 要把所有行重新切一遍，而缩放每帧都在变）——
        //   留到手指抬起（`Completed` → `RenderOutput`）再做。
        OutputGrid.SetFontSize(_fontSize);
    }

    /// <summary>
    /// 双指缩放**进行中** —— 由 `TerminalGrid` 从平台触摸里算好比例回调进来。
    ///
    /// ⚠ **这一拍只做"画一遍"**，别的一概留到 <see cref="OnOutputPinchEnd"/>：
    ///   重排折行（`ShellWrap` 要把所有行重切一遍）与落盘（`MauiShellStore.Font` 走
    ///   `Preferences.Set`，是**写盘**）都按触摸事件的频率做的话，缩放会又顿又费。
    ///   `SetFontSize` 已经是"只改字号、不换内容"的那条路（内容缓存着，重画即可），
    ///   而重画不会销毁正在接手势的视图 —— 捏合不会自己被自己打断。
    /// </summary>
    private void OnOutputPinchScale(double fontSize)
    {
        _fontSize = MauiShellStore.ClampFont(fontSize);   // 钳位在 store 里（唯一一份范围）
        ApplyFontSizeLive();
    }

    /// <summary>
    /// 捏合**结束**（手指离开 / 手势被打断）—— 收尾与菜单改字号**同一条路**
    /// （`SetFontSize`：落盘 + 重排 + 重画），免得两条路各做一半、时日一久就漂。
    /// </summary>
    private void OnOutputPinchEnd() => SetFontSize(_fontSize);

    private static void HighlightSizeButton(Button b, bool on)
    {
        if (on)
        {
            b.BackgroundColor = MauiUi.Res("Primary");
            b.TextColor = Colors.White;
        }
        else
        {
            b.ClearValue(Button.BackgroundColorProperty);
            b.ClearValue(Button.TextColorProperty);
        }
    }

    /// <summary>清空输出（按钮与 <c>clear</c>/<c>cls</c> 命令共用这一份）。</summary>
    private void ClearOutput()
    {
        _lines.Clear();
        _partial = false;
        OutputGrid.Clear();
        UpdateScrollBar();
    }

    /// <summary>
    /// 追加输出、按行裁剪、按需滚到底。
    ///
    /// <paramref name="text"/> 默认按**外部命令的裸输出**看待：先把 ANSI 转义翻成
    /// <c>«»</c> 中间格式（颜色留下来，光标/OSC 这类吃掉），再进缓冲。
    /// 这样 `ls --color`、`git status` 的颜色在手机上终于是彩色的，而不是被剥成一片灰。
    /// </summary>
    /// <param name="alreadyMarkup">已经是中间格式，别再转一遍（见 RunWithPromptAsync 的说明）。</param>
    private void Append(string text, bool alreadyMarkup = false, bool noWrap = false)
    {
        // ⚠ **控制字符必须在 ANSI → 标记转换之前解释**：`\r`/`\t`/`\b` 的语义是"在屏幕上占几格"，
        // 一旦转成 `«red»` 那种标记，列数就算不出来了（制表位、退格全都会错位）。
        // 按标准语义处理：`\r` 回行首覆写（进度条）、`\t` 跳制表位（表格）、`\b` 退格（叠打粗体）。
        text = ShellControls.Apply(text);
        if (!alreadyMarkup) text = AnsiMarkup.ToMarkup(text);

        // 按 \n 切段并入缓冲：有换行的段落是**整行**，末尾没换行的那段是**半行**
        // （与上一段半行拼起来，而不是另起一行）。
        int start = 0;
        while (start < text.Length)
        {
            int nl = text.IndexOf('\n', start);
            if (nl < 0)
            {
                AddSegment(text[start..], partial: true, noWrap: noWrap);
                break;
            }
            AddSegment(text[start..nl], partial: false, noWrap: noWrap);
            start = nl + 1;
        }

        TrimScrollback();

        // **只有原本就贴着底，才跟着滚到底。**
        //
        // 原来是无条件弹到底 —— 一条命令持续吐输出（编译、下载、日志）时，
        // 用户往回翻一屏都做不到：每次新输出都把他拽回最底下。
        // 现在按终端的老规矩：贴底才跟随，一旦往上滚就"脱钩"，让用户安安静静看历史。
        // 跟底判据改由**画布**给（它自己滚）：贴底才跟随新内容，往上一翻就脱钩

        // 走 FormattedText 而不是 Text —— 颜色就靠它（Text 是纯文本，标记会原样显示）
        RenderOutput();

        // 排到下一拍：此刻内容刚换完，**画布还没按新内容重算尺寸** ——
        // 贴底要等它量完，否则滚到的是旧的内容高（差一行）。
        Dispatcher.Dispatch(AlignOutput);
    }

    /// <summary>
    /// 输出之后画面停在哪儿 —— **两种屏幕模式两种落点**（用户定的）：
    ///
    ///   · **固定屏幕**（`Rows > 0`，行列都钉死）= 老显示器：屏幕就是那一块，
    ///     画面贴**屏幕左上角**，多出来的用滚动条看；
    ///   · **行不固定**（滚屏）= 真终端：停在**最后一屏**（贴底跟随）。
    ///
    /// 内容比视口小时两者的落点自然重合（左上角）—— 见 `ScrollToHome` 的说明。
    /// 切模式（`ApplyDisplaySettings` 之后）也走这里，所以模式一换落点立刻跟着换。
    /// </summary>
    private void AlignOutput()
    {
        if (MauiShellStore.Rows > 0) OutputGrid.ScrollToHome();
        else OutputGrid.ScrollToEnd();
    }

    /// <summary>
    /// 输出区**真正能显示内容**的尺寸 —— 扣掉画布自己的 `Margin`。
    ///
    /// ⚠ 判"装不装得下"、上报终端尺寸**一律用这两个**：拿外框尺寸当视口的话，
    /// 那两条内边距（横 28 / 竖 8）会被当成"看得见的区域"，
    /// 于是内容被切掉一截而滚动条不出现。
    /// </summary>
    private double ViewportHeight
    {
        get { var m = OutputGrid.Margin; return OutputGrid.Height - m.Top - m.Bottom; }
    }

    private double ViewportWidth
    {
        get { var m = OutputGrid.Margin; return OutputGrid.Width - m.Left - m.Right; }
    }

    /// <summary>
    /// **空的** —— 滚动条与滚动现在全在画布里（`TerminalGrid` 自己滚、自己画条）。
    ///
    /// ⚠ 这条链以前是页面自己算的（`ScrollBarMath` + `ScrollTrack`/`HScrollTrack` 两条自绘轨道
    ///   + `_thumbTop`/`_panningThumb` 那一组拖拽状态）。理由当初写得很对（"系统那条在 Android 上
    ///   只在滑动时闪一下，长输出完全不知道自己在哪"），**只是必须跟着内容一起搬进画布**：
    ///   轨道在**视口**坐标、内容在**内容**坐标，分在两处就是"同一件事两处实现"（本仓头号坑）；
    ///   而且画布自己滚之后，页面这边拿到的偏移永远是 0（它不再滚了）。
    ///
    /// 方法体与那组字段**已删**，只留这个空壳：调用点散在显示设置 / 输出追加 / 缩放好几处，
    /// 留个空实现比在每处判"要不要刷新"清楚。
    /// </summary>
    private void UpdateScrollBar() { }
    /// 滚动条与滚动**都不在这里了** —— 全在画布里（`TerminalGrid` 自己滚、自己画条）。
    ///
    /// 这一整段（`UpdateScrollBar` / `UpdateBar` / `OnOutputScrolled` / 四条轨道的拖拽与点击）
    /// 连同 XAML 里那两条自绘轨道**已删**。它们当初的理由是对的（"系统那条在 Android 上只在
    /// 滑动时闪一下，长输出完全不知道自己在哪"），**只是必须跟着内容一起搬进画布**：
    /// 轨道在**视口**坐标、内容在**内容**坐标，分在两处就是"同一件事两处实现"（本仓头号坑），
    /// 而且画布自己滚之后，页面这边拿到的偏移永远是对的 0（它不再滚了）。
    /// </summary>
    private void AddSegment(string segment, bool partial, bool noWrap = false)
    {
        if (_partial)
        {
            // 接上没写完的那半行（同一个块，标志一致）
            var last = _lines[^1];
            _lines[^1] = (last.Text + segment, last.NoWrap || noWrap);
        }
        else _lines.Add((segment, noWrap));
        _partial = partial;
    }

    /// <summary>
    /// 超过上限就从**最老的整行**开始丢，一次丢到刚好剩上限那么多行
    /// （上限可配，见 <see cref="MauiShellStore.Scrollback"/>）。
    /// 只按整行丢 ⇒ 不会出现"断头的半行"。
    /// </summary>
    private void TrimScrollback()
    {
        // **固定高度那一档没有历史**（老显示器语义，见 DisplayText）——
        // 缓冲也就没必要留着：留着的话切回自适应档，"早就滚没了"的内容会突然复活。
        // 多留一倍可见行数，免得每来一行都要重算一次裁剪。
        var rows = MauiShellStore.Rows;
        if (rows > 0)
        {
            var keep = Math.Max(rows * 2, 16);
            if (_lines.Count > keep) DropHead(_lines.Count - keep);
            return;
        }

        int over = _lines.Count - MauiShellStore.Scrollback;
        if (over <= 0) return;
        DropHead(over);
    }

    /// <summary>
    /// 从**头部**丢掉若干行 —— **回滚缓冲唯一的裁剪出口**。
    ///
    /// ⚠ 收成一处的理由很具体：丢头会让所有"按行号记着位置"的东西一起前移，
    ///   目前是**画面光标**（`_cursorBaseLine`）。以前两处各写一句 `RemoveRange`，
    ///   加了光标之后就必须两处都记住要减 —— 而漏一处的症状是"光标画到别的行上去"，
    ///   只在大输出之后才复现（本仓记过的"新加了状态就要找齐所有出口"）。
    /// </summary>
    private void DropHead(int count)
    {
        if (count <= 0) return;
        _lines.RemoveRange(0, count);
        if (_cursorBaseLine >= 0)
        {
            _cursorBaseLine -= count;
            if (_cursorBaseLine + _cursorRow < 0) _cursorBaseLine = -1;   // 连光标那行都被丢掉了
        }
    }

    /// <summary>行高系数（字号 → 行高）。</summary>
    private const double LineHeightFactor = 1.3;

    /// <summary>
    /// 把命令行显示设置应用到界面（**缩放**字号 / **固定可见行数**）。
    /// 在构造时调一次，用户改完设置由设置页再调一次。
    /// </summary>
    /// <summary>
    /// 输出区两侧被占掉的宽度（dp）—— ScrollView 的 Padding 12+16、自绘滚动条约 9、再留余量。
    /// 自适应模式要按它反推"这一屏能放几列"。
    /// </summary>
    private const double OutputAreaChrome = 40;

    /// <summary>输出区可用宽度（dp）—— `OnSizeAllocated` 里更新，自适应算列数要用。</summary>
    private double _outputWidth;

    /// <summary>
    /// 当前生效的列数。
    ///
    /// **固定档**：用户选的那个（80×25 那类，老程序按它排版）。
    /// **自适应档**：按屏宽与字号**算出来**的 —— "自适应"不是"没有列数"，
    /// 而是**列数由屏宽推出来**；这样"屏幕上看到几列"与"程序以为终端有几列"是同一个数。
    /// 宽度还没落定（首次布局前）时返回 0，此时不折行、交给 Label 自己按显示宽度折。
    /// </summary>
    private int EffectiveCols()
        => MauiShellStore.Cols > 0
            ? MauiShellStore.Cols
            : ShellWrap.ColumnsForWidth(_outputWidth, MauiShellStore.Font);

    internal void ApplyDisplaySettings()
    {
        // ── 字号 ──
        // 字号**永远由缩放设置决定，不自动缩**。
        //
        // 曾经想的是"固定列数时把字号缩到 N 列正好铺满"——**那是把两件事搅在一起了**：
        // 固定大小的意思是"**字符格真的固定**"（老程序按 80 列排版，格子必须就是 80 个），
        // 而缩字号是"让内容塞进屏宽"，两者目的相反。用户点的名：**超出屏宽就横向滚动**。
        // （第一版按显示宽度把 80 列又折了一次，`ls -l` 的列对齐当场就散了。）
        var size = MauiShellStore.Font;
        _fontSize = size;

        // ── 内容宽度**不在这里算**（整块已删）──
        //
        // 这里原有一大段"给 Label 算一个显式宽度（= 列数 × 字符宽，画面行再 ×1.15）"：
        // 那是输出区还套在 `ScrollView` 里、内容是个 `Label` 时的做法 —— 宽度得由页面
        // 告诉平台，好让外面的容器去滚，也免得 `Label` 按显示宽度**二次折行**。
        //
        // **画布接管滚动之后这段全成了反向操作**（用户报的"缩放后横向滚动条没出来、
        // 滚不回去"就是它造成的）：它把**画布视图本身**撑到和内容一样宽，
        // 于是画布永远"装得下"自己的内容，`TerminalGrid` 算出来的横向可滚距离恒为 0。
        //
        // 现在的分工是干净的一条线：
        //   · **视口宽** = 布局给的（画布 `Fill`，`Margin` 之外全是它）
        //   · **内容宽** = `TerminalGrid.SetLines` 里按**最长的那一行**算
        //     （`max(VisibleWidth(行)) × 格宽`）—— 这正是用户要的「按行宽度计算绘制滚动条」
        //   · **滚动条** = 内容宽 > 视口宽才画（`DrawBars`），两个轴同一个判据
        //   · **折行** = `ShellWrap` 按 `EffectiveCols()` 切（`DisplayText`）

        // ── 固定行数：把输出区高度锁成"正好 N 行"，多出来的走滚动 ──
        // ⚠ 行高按 **字号 × 1.3** 估（平台字体度量拿不到精确行高时的通行做法）——
        //   所以**可见行数是近似的**；列数是精确的（那是按字符格折出来的）。
        // ⚠ **固定行数那一档不再锁高度了** —— 画布现在自己滚（`TerminalGrid`），
        //   "可见几行"由**视口高 ÷ 格高**自然决定，不需要页面再拿"字号 × 1.3"去估。
        //   这顺带解掉了原先那条"可见行数是近似的"的老问题：格高是**算**出来的常量
        //   （`CellHeight = 字号 × 1.2`），可见行数因此是精确的。

        PublishTerminalSize();
    }

    /// <summary>
    /// 把当前终端尺寸**告诉 VML 宿主** —— 全屏程序（nyancat / curses 那类）要按这个
    /// 建 `rows×cols` 网格（见 `MauiVml.RunProgram` 的屏幕分支）。
    ///
    /// ⚠ **必须在"布局落定之后"也调一次**（<see cref="UpdateScrollBar"/> 里）。
    /// 本函数在**构造期**就会被调用，那时 `OutputScroll` 还没量过 ⇒ `ViewportHeight = 0`、
    /// `_outputWidth = 0` ⇒ 列数落到自适应下限、行数落到兜底 25。
    /// 实测后果：nyancat 画在第 20~42 行，而网格只有 25 行 ⇒ **画面大半被裁**，
    /// 看上去像"什么都没画"。
    ///
    /// 自适应档行数只能**估**（可见高度 ÷ 行高），估不出来就给 0 = 未知，
    /// 由那边退回 80×25（老程序通用的假设）—— 别在这里编一个数。
    /// </summary>
    private void PublishTerminalSize()
    {
        // ⚠ **自适应档不把自己推导出来的列数当成"终端尺寸"播报**（0 = 未知）。
        //
        // 这个数唯一的用途是给**全屏程序**建网格（`MauiVml.RunProgram` 的屏幕分支）。
        // 而 `nyancat` / `tty-clock` 那批老程序**写死 80 列、根本不会自适应** ——
        // 告诉它"你的终端只有 50 列"，它照样一行吐 80 个字符 ⇒ **它自己的输出就在网格里折了**，
        // 画面上表现为整幅图斜切（实测：固定 80×25 档形状完整、自适应档被剪开）。
        // 播 0 就退回**经典的 80×25**（那个兜底本来就在 `RunProgram` 里）。
        //
        // 「行」同一个道理：屏幕高就报高，只会让老程序多画一片空行。
        //
        // 用户显式选了固定档时照播不误 —— 那正是"我就要这个尺寸"的意思。
        var auto = MauiShellStore.Mode == ShellSizeMode.Auto;
        MauiVml.TermCols = auto ? 0 : EffectiveCols();

        var rows = MauiShellStore.Rows;
        var px = ViewportHeight;
        MauiVml.TermRows = rows > 0
            ? rows
            : (auto || px <= 0 ? 0 : (int)(px / (MauiShellStore.Font * LineHeightFactor)));
    }

    /// <summary>
    /// 要送到输出区的文本 —— **设了固定列数时按字符格硬折**（老程序 80×25 兼容）。
    ///
    /// 逐**逻辑行**折再拼回去：回滚缓冲里存的始终是逻辑行（不被折行污染），
    /// 折行只发生在呈现这一步 —— 改列数时历史输出会跟着重排，与真终端一致。
    /// 折行的纯逻辑在 <see cref="ShellWrap"/>（桌面有判据）；这里只管拼。
    /// </summary>
    /// <summary>
    /// 只**重画**输出区（不动 <c>_lines</c>）—— 显示设置变了之后用它把折行/字号重算一遍。
    /// 与 <see cref="Append"/> 末尾那句是同一个出口（走 <see cref="DisplayText"/>），
    /// 别在这里另拼一份文本。
    /// </summary>
    private void RenderOutput()
    {
        // **整块交给一个自绘画布** —— 文本行与画面行走同一个渲染器。
        //
        // 为什么不做成 `Label` + `GraphicsView` 混排（上一版就是这么写的）：
        //   · 画面用 `Label` 会被平台改写宽度（折叠空格、NBSP 字体回退），实测对不齐；
        //   · 混排之后"双指缩放"要**逐个子视图挂手势**，而缩放又要重建视图 ⇒
        //     **正在接手势的那个视图被销毁，手势当场断掉**（用户报的"捏一下没反应"）。
        // 一个画布同时解决这两条，结构也最简单（用户点名的"结构越简单越好"）。
        var lines = DisplayText().Select(l => l.Text).ToList();
        OutputGrid.SetLines(lines, _fontSize, MauiUi.IsDark);
        // 光标在 `DisplayText` 里换算成了**显示行号**（`_cursorDisplayLine`），
        // 列与显隐直接来自程序（见 `_cursorCol` / `_cursorVisible`）。
        OutputGrid.SetCursor(_cursorDisplayLine, _cursorCol, _cursorVisible);
        // ⚠⚠ **绝不能再给画布设 `WidthRequest`**（这里原来有这么一句，已删）。
        //
        // 那是"输出区还套在 `ScrollView` 里、内容是个 `Label`"时的做法：给 Label 一个
        // 显式宽度，好让外面的滚动容器去滚。**画布接管滚动之后，这句话的意思完全反了** ——
        // 它把**画布这个视图本身**撑到和内容一样宽，于是 `Width >= 内容宽`，
        // `ClampScroll` 算出来的 `maxX = 内容宽 - Width = 0`：横向根本不需要滚，
        // 滚动条判据 `内容宽 > Width` 也永远为假。
        // 症状正是用户报的「**缩放后横向滚动条没出来、滚不回去**」——
        // 内容被父容器（而不是被我们）裁在屏幕外，而画布自认为"全都看得见"。
        //
        // 正解：画布的宽度**永远是视口宽度**（布局给的），内容尺寸另有 `_contentW/_contentH`，
        // 超出的部分由画布**自己**裁剪 + 自己滚（`TerminalGrid.Draw` 里那条 `ClipRectangle`）。
        // 这两者混用就会得到"视口 = 内容 ⇒ 永远不需要滚动"这个死结。
    }
    private List<(string Text, bool NoWrap)> DisplayText()
    {
        var cols = EffectiveCols();
        List<(string Text, bool NoWrap)> wrapped;
        if (cols <= 0)
        {
            wrapped = new List<(string, bool)>(_lines);         // 宽度未落定：交给 Label 折
        }
        else
        {
            wrapped = new List<(string Text, bool NoWrap)>(_lines.Count);
            // 光标那一行在缓冲里的下标（画面行 1:1 不折，所以它就是块首 + 块内行号）
            int cursorBufLine = _cursorBaseLine >= 0 ? _cursorBaseLine + _cursorRow : -1;
            _cursorDisplayLine = -1;

            for (int i = 0; i < _lines.Count; i++)
            {
                var (line, noWrap) = _lines[i];
                if (i == cursorBufLine) _cursorDisplayLine = wrapped.Count;   // 这一行将落在显示列表的这里
                // ⚠ **画面行不折**（见 `ShellWrap.IsPictureLine`）：全屏程序的每一行
                //   就是屏幕上的一行，折一下整幅画就斜切了 —— 实测「有彩色了，但有点乱」
                //   正是这么来的（猫的彩虹与身体都在，形状是剪开的）。
                // 标志优先（全屏输出那一整块），次之才是逐行的"纯色块行"判据。
                if (noWrap || ShellWrap.IsPictureLine(line)) wrapped.Add((line, true));
                else foreach (var w in ShellWrap.WrapMarkup(line, cols)) wrapped.Add((w, false));
            }
        }

        // ── 固定高度 = **老显示器**：屏幕就这么多行，滚出去的不再显示 ──
        //
        // 用户点名的语义：「固定高度的内容，换行只能内部滚动，滚过了的就没了，和老显示器一致」。
        // 老 CRT 上**没有回滚缓存**这个概念 —— 屏幕上那 25 行就是全部，被顶上去的就真没了。
        // （有回滚缓存的是后来的终端模拟器，那是**另一档**的行为 —— 自适应高度那档才有。）
        //
        // ⚠ 裁的必须是**折行之后**的行数，不是逻辑行数：一条长命令折成 5 行，
        //   按逻辑行裁会留下 5 倍的内容、照样撑出滚动条。
        var rows = MauiShellStore.Rows;
        if (rows > 0 && wrapped.Count > rows)
        {
            // 裁掉的行数要从光标行号里减掉（裁的是**头部**，所以是整体前移）
            _cursorDisplayLine -= wrapped.Count - rows;
            wrapped = wrapped.GetRange(wrapped.Count - rows, rows);
        }
        if (cols <= 0) _cursorDisplayLine = -1;   // 宽度未落定时不折行也没画面，别画

        return wrapped;
    }
}
