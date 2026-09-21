using System.Text;
using WayCoder.Maui.Controls;
using WayCoder.Maui.Markup;
using WayCoder.Maui.Services;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;

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

    /// <summary>输出区字号 —— 文本 Label 与**自绘网格**共用这一个（尺子只有一把）。</summary>
    private double _fontSize = 12;

    /// <summary>输出区应有的内容宽度（像素）。`-1` = 交给布局。</summary>
    private double _contentWidth = -1;

    /// <summary>缓冲是否停在半行上（上一段没有以 `\n` 收尾）。</summary>
    private bool _partial;

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
        //
        // ⚠ **手势必须挂在内容 Label 上，不能挂在 ScrollView 上。**
        // 挂在 ScrollView 上时，MAUI 会把手势监听装到 ScrollView 自己的平台视图上，
        // Android 侧 ACTION_DOWN 被消费掉 ⇒ **整个输出区再也拖不动**（实测：滑动后
        // 逐像素比对两张截屏，差异只落在底部导航栏，正文一个像素没动）。
        // 挂在内容上是另一条路（事件先给子视图，拖拽仍由 ScrollView 接管）。
        AddOutputGestures(OutputHost);

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
            () => ExecVmlAsync(null, absPath), markupResult: true);

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
    private async Task RunWithPromptAsync(string cmdLine, Func<Task<string>> body, bool markupResult = false)
    {
        Append($"{Prompt} {cmdLine}\n");
        SetBusy(true);
        try
        {
            // ⚠ **画面不折行**：全屏程序的输出是一张"画面"，每一行就是屏幕上的一行。
            //   标志由产生它的 `MauiVml` 给出（**块级事实**），不在这里逐行猜 ——
            //   画面里的标题栏/菜单项/状态行**都是有文字的**，逐行猜必然漏
            //   （实测：80 列的网格被按自适应的 46 列折开，标题栏断成两行、边框全错位）。
            Append((await body()).TrimEnd() + "\n\n", alreadyMarkup: markupResult,
                   noWrap: markupResult && MauiVml.LastOutputWasGrid);
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
            RefreshCwd();
            SetBusy(false);
            Append(Prompt + "\n");   // 收尾的提示符：执行完了，等下一个命令
        }
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
            "vml", "test | run <文件> | help",
            "跑 VML 程序：`test` 跑内置自检，`run` 按扩展名派发（.vml 汇编 / .vmb 装载 / 其余 22 种语言编译）",
            "编译并运行一段 VML。`vml test` 跑内置自检程序；`vml run <文件>` 按扩展名自动派发"
            + "（`.vml` 走汇编，`.vmb` 直接装载字节码，`.c`/`.py`/`.rs` 等 22 种语言走各自前端编译器）。"
            + "路径相对下面显示的工作目录解析。",
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

        return $"⚠️ 不认识的 vml 子命令：{rest}（敲 `vml` 看用法）";
    }

    private const string VmlUsage =
        "用法：\n  vml test            跑内置的自检程序\n"
      + "  vml run <文件>      .vml 走汇编、.vmb 直接装载，"
      + "其余按扩展名自动选编译器（22 种语言）";

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
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _runDone = done;
        InstallVmlProgress();   // 这里是**本页所有 VML 运行**的唯一入口（文件页递的 + 手敲的）
        try
        {
            return await Task.Run(() => MauiVml.Run(source, file, InteractiveTimeoutSec, ReadLineFromProgram,
            cts.Token, markup: true));
        }
        finally
        {
            _interactive = false;
            _runCts = null;
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
        var line = tcs.Task.GetAwaiter().GetResult();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _stdinTcs = null;
            StdinPanel.IsVisible = false;
            Append(line + "\n");             // 回显，像真终端
        });

        return line;
    }

    private void OnStdinSubmitted(object? sender, EventArgs e)
    {
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
        Dispatcher.Dispatch(UpdateScrollBar);
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
        var autoCols = ShellWrap.ColumnsForWidth(_outputWidth, MauiShellStore.Font);
        AutoSizeBtn.Text = autoCols > 0 ? $"自适应 {autoCols} 列" : "大小自适应";
        WidthFixedBtn.Text = $"横向固定 {MauiShellStore.RawCols}";
        FixedSizeBtn.Text = $"固定 {MauiShellStore.RawCols}×{MauiShellStore.RawRows}";

        HighlightSizeButton(AutoSizeBtn, mode == ShellSizeMode.Auto);
        HighlightSizeButton(WidthFixedBtn, mode == ShellSizeMode.WidthFixed);
        HighlightSizeButton(FixedSizeBtn, mode == ShellSizeMode.Fixed);
    }

    /// <summary>缩放起点字号 —— 捏合过程中 <c>e.Scale</c> 是相对**起点**的累计值。</summary>
    private double _pinchStartFont;

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
    /// 给输出区的**每一个子视图**挂上「点一下聚焦输入框」+「双指缩放字号」。
    ///
    /// ⚠ **必须逐个挂，不能只挂在容器（`OutputHost`）上** —— 自绘网格是个
    ///   `GraphicsView`，它会把落在自己身上的触摸收走，容器那层根本收不到捏合
    ///   （实测：字号缩不动的真根因）。编辑器也是在自己画布上直接收触摸的。
    /// </summary>
    private void AddOutputGestures(View view)
    {
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => CmdEntry.Focus();
        view.GestureRecognizers.Add(tap);

        var pinch = new PinchGestureRecognizer();
        pinch.PinchUpdated += OnOutputPinch;
        view.GestureRecognizers.Add(pinch);
    }

    private void OnOutputPinch(object? sender, PinchGestureUpdatedEventArgs e)
    {
        switch (e.Status)
        {
            case GestureStatus.Started:
                _pinchStartFont = MauiShellStore.Font;
                break;

            case GestureStatus.Running:
                MauiShellStore.Font = _pinchStartFont * e.Scale;   // 钳位在 store 里
                ApplyDisplaySettings();
                RenderOutput();                                    // 字号变了，折行要重排
                UpdateSizeButtons();                               // 自适应列数跟着字号变

                // ⚠ 字号一变，**内容像素宽也跟着变** ⇒ 原来装得下的可能装不下了
                //   （或反过来）。不刷新的话横向滚动条会停在旧判断上：
                //   放大了却还是没条可拖、或者缩回去之后留着一根永远不需要的条。
                //   排到下一帧 —— 这一帧 Label 还没按新字号重新测量过。
                Dispatcher.Dispatch(UpdateScrollBar);
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _pinchStartFont = MauiShellStore.Font;             // 落定：下次捏合从当前值起算
                break;
        }
    }

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
        OutputHost.Children.Clear();
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
        var follow = ScrollBarMath.IsAtBottom(ContentHeight, ViewportHeight, OutputScroll.ScrollY);

        // 走 FormattedText 而不是 Text —— 颜色就靠它（Text 是纯文本，标记会原样显示）
        RenderOutput();

        // 排到下一拍：此刻刚换完 Text，布局还没算，量出来的高度还是旧值
        // （滚动条显不显示、滑块多长、能不能贴底，都得等新布局落定）。
        Dispatcher.Dispatch(() =>
        {
            if (follow)
            {
                try { OutputScroll.ScrollToAsync(0, ContentHeight, animated: false); }
                catch { /* 页面正在销毁时滚动会抛，忽略 */ }
            }
            UpdateScrollBar();
        });
    }

    /// <summary>
    /// 输出内容的**真实高度** —— 取 Label 实测高度，**不能用 <c>ScrollView.ContentSize</c>**。
    ///
    /// 实测（模拟器，1080×2400）：Label 量出来 1750px，而 <c>ContentSize.Height</c> 报 ~3000px。
    /// 按那个虚高的值滚，就会**滚过内容**：顶部被切掉、底部留一大片空白，
    /// 而且"贴不贴底"的判据永远为真（因为偏移量正好停在那个虚高的底上），
    /// 于是每次都往上多滚一截。用户看到的就是"输出是滚着的、但往回翻不动、上面还缺一块"。
    ///
    /// 顺带说明为什么滚动条滑块也偏大：Slider 长度按 `视口/内容` 算，
    /// 分母虚高 ⇒ 滑块算出来偏短 —— 同一处错误连累两个地方，改这一处就都对了。
    /// </summary>
    private double ContentHeight => OutputHost.Height;

    /// <summary>
    /// 输出内容的**真实宽度** —— 取 Label 的实测宽度与显式宽度里大的那个。
    ///
    /// 为什么要取 max：固定列数时页面给 Label 设了显式 `WidthRequest`（= 列数 × 字符宽），
    /// 而布局在某些时刻量出来的 `Width` 会小于它（还在测量中）。拿小的那个判"装不装得下"
    /// 会让横向滚动条**该出现时不出现** —— 那正是用户唯一需要它的时刻。
    /// </summary>
    private double ContentWidth
    {
        get
        {
            var w = OutputHost.Width;
            var want = 0.0;
            foreach (var c in OutputHost.Children)
                if (c is VisualElement ve)
                    want = Math.Max(want, ve.WidthRequest > 0 ? ve.WidthRequest : ve.Width);
            return want > w ? want : w;
        }
    }

    // ── 输出区滚动条（**两个轴各一条**）────────────────────────────
    //
    // 几何一律走 `ScrollBarMath`（那一份是纯函数、有断言），这里只做"量一下、摆一下"。
    // **两个轴共用同一条判据**：`ShouldShow` = 内容超出视口才显示 —— 用户的原话是
    // 「只看能否显示全：显示得全就不显示滚动条，显示不全就显示滚动条」。
    //
    // 横轴那条是**横向固定档的必需品**：列钉死之后内容必然比屏幕宽，
    // 没有它就只能盲划（而且不知道还有多少没看到）。

    /// <summary>滑块当前位置（拖动时作为增量基准）—— 竖轴用 top、横轴用 left。</summary>
    private double _thumbTop;
    private double _thumbHeight;
    private double _panStartTop;
    private bool _panningThumb;

    private double _hThumbLeft;
    private double _hThumbWidth;
    private double _hPanStartLeft;
    private bool _hPanningThumb;

    /// <summary>
    /// 输出区**真正能显示内容**的尺寸 —— 扣掉 `ScrollView` 自己的内边距。
    ///
    /// ⚠ 全页判"装不装得下"、算滑块几何、拖滑块换算滚动偏移，**一律用这两个**：
    /// 拿外框尺寸当视口的话，那两条内边距（横 28 / 竖 8）会被当成"看得见的区域"，
    /// 于是内容被切掉一截而滚动条不出现；更隐蔽的是**拖动换算用的视口与显示用的不一致**时，
    /// 滑块拖到轨道尽头而内容没滚到底。
    /// </summary>
    private double ViewportHeight
    {
        get { var p = OutputScroll.Padding; return OutputScroll.Height - p.Top - p.Bottom; }
    }

    private double ViewportWidth
    {
        get { var p = OutputScroll.Padding; return OutputScroll.Width - p.Left - p.Right; }
    }

    /// <summary>重新量内容/视口，把**两个轴**的滚动条都刷新一遍。</summary>
    private void UpdateScrollBar()
    {
        // 布局落定之后**再播报一次**终端尺寸 —— 构造期那次量到的是 0，
        // 全屏程序会照着兜底值建网格（见 PublishTerminalSize 的说明）。
        // 放在这里是因为它本来就在"量视口"的时机被调度，不另开一条触发链。
        PublishTerminalSize();

        // ── 竖轴 ──
        // ⚠ **固定高度那一档不出竖条**（用户点名："滚动条只有一层，内层没有滚动条"）：
        //   那一档是老显示器语义 —— 屏幕就 N 行，滚出去的就没了（见 DisplayText），
        //   即**根本没有可滚回去的内容**，画一根条只会让人以为上面还有。
        //   顺带绕开一个隐患：固定行高是按 `字号 × 1.3` **估**的（见 ApplyDisplaySettings），
        //   估算与真实行高差一两像素时，内容会比视口高一丁点 ⇒ 竖条**闪进闪出**。
        if (MauiShellStore.Rows > 0)
        {
            ScrollTrack.IsVisible = false;
        }
        else
        {
            // Label 实测高度，不是 ContentSize（见 ContentHeight 注释）
            UpdateBar(ScrollTrack, ScrollThumb, ContentHeight, ViewportHeight,
                      OutputScroll.ScrollY, vertical: true, ref _thumbTop, ref _thumbHeight);
        }

        // ── 横轴 ── 列超出屏宽时**仍然要**（固定列数必然超出，没有它就只能盲划）
        UpdateBar(HScrollTrack, HScrollThumb, ContentWidth, ViewportWidth,
                  OutputScroll.ScrollX, vertical: false, ref _hThumbLeft, ref _hThumbWidth);
    }

    /// <summary>
    /// 刷一条滚动条。两个轴只差"量哪个方向 / 摆哪个属性"，几何与判据完全共用 ——
    /// 分开写两份的话，改一处忘一处就是本仓库排第一的坑（同一规则两处实现）。
    /// </summary>
    private static void UpdateBar(Grid track, BoxView thumb,
                                  double content, double viewport, double offset,
                                  bool vertical, ref double pos, ref double size)
    {
        // 内容装得下 → 整条藏起来（用户要的就是"显示得全就不显示滚动条"）
        if (!ScrollBarMath.ShouldShow(content, viewport))
        {
            track.IsVisible = false;
            return;
        }

        // ⚠ 先显示再量：`Height`/`Width` 要可见之后布局才会给值
        track.IsVisible = true;
        var trackLen = vertical ? track.Height : track.Width;
        if (trackLen <= 0) trackLen = vertical ? track.HeightRequest : track.WidthRequest;
        if (trackLen <= 0) return;   // 布局还没量出来，等下一拍

        var (p, h) = ScrollBarMath.Thumb(content, viewport, offset, trackLen);
        pos = p;
        size = h;

        if (vertical) { thumb.HeightRequest = h; thumb.TranslationY = p; }
        else          { thumb.WidthRequest  = h; thumb.TranslationX = p; }
    }

    private void OnOutputScrolled(object? sender, ScrolledEventArgs e)
    {
        // 拖自己触发的滚动不用回写（回写会和手指打架）。**两个轴都要判** ——
        // 只判竖轴的话，横拖期间每一帧都会被回写覆盖，滑块原地抖。
        if (_panningThumb || _hPanningThumb) return;
        UpdateScrollBar();
    }

    /// <summary>拖动滑块。</summary>
    private void OnScrollThumbPan(object? sender, PanUpdatedEventArgs e)
    {
        var track = ScrollTrack.Height;
        var content = ContentHeight;      // 同上：Label 实测高度
        var viewport = ViewportHeight;    // 必须与 UpdateScrollBar 同源，否则"拖到头却没到底"
        if (track <= 0) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panningThumb = true;
                _panStartTop = _thumbTop;
                break;

            case GestureStatus.Running:
            {
                var top = _panStartTop + e.TotalY;
                var offset = ScrollBarMath.OffsetForThumbTop(top, content, viewport, track);
                try { OutputScroll.ScrollToAsync(0, offset, animated: false); }
                catch { /* 页面正在销毁 */ }

                // 手指拖出来的位置直接摆上去：不等 Scrolled 回调（拖动时要"跟手"）
                var (t, h) = ScrollBarMath.Thumb(content, viewport, offset, track);
                _thumbTop = t;
                ScrollThumb.HeightRequest = h;
                ScrollThumb.TranslationY = t;
                break;
            }

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _panningThumb = false;
                UpdateScrollBar();
                break;
        }
    }

    /// <summary>点轨道空白处：翻一屏（不是跳到点击位置 —— 那在细轨道上太跳）。</summary>
    private void OnScrollTrackTapped(object? sender, TappedEventArgs e)
    {
        var viewport = ViewportHeight;
        try { OutputScroll.ScrollToAsync(0, OutputScroll.ScrollY + viewport * 0.9, animated: true); }
        catch { /* 页面正在销毁 */ }
    }

    /// <summary>横向拖动滑块 —— 与竖轴同一套，只是换轴（`TotalX` / `ScrollX`）。</summary>
    private void OnHScrollThumbPan(object? sender, PanUpdatedEventArgs e)
    {
        var track = HScrollTrack.Width;
        var content = ContentWidth;
        var viewport = ViewportWidth;     // 同上：与 UpdateScrollBar 同源
        if (track <= 0) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _hPanningThumb = true;
                _hPanStartLeft = _hThumbLeft;
                break;

            case GestureStatus.Running:
            {
                var left = _hPanStartLeft + e.TotalX;
                var offset = ScrollBarMath.OffsetForThumbTop(left, content, viewport, track);
                try { OutputScroll.ScrollToAsync(offset, OutputScroll.ScrollY, animated: false); }
                catch { /* 页面正在销毁 */ }

                // 与竖轴同理：手指拖出来的位置直接摆上去，不等 Scrolled 回调（要跟手）
                var (l, w) = ScrollBarMath.Thumb(content, viewport, offset, track);
                _hThumbLeft = l;
                HScrollThumb.WidthRequest = w;
                HScrollThumb.TranslationX = l;
                break;
            }

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _hPanningThumb = false;
                UpdateScrollBar();
                break;
        }
    }

    /// <summary>点横轴轨道空白处：右翻一屏。</summary>
    private void OnHScrollTrackTapped(object? sender, TappedEventArgs e)
    {
        var viewport = ViewportWidth;
        try { OutputScroll.ScrollToAsync(OutputScroll.ScrollX + viewport * 0.9, OutputScroll.ScrollY, animated: true); }
        catch { /* 页面正在销毁 */ }
    }

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
            if (_lines.Count > keep) _lines.RemoveRange(0, _lines.Count - keep);
            return;
        }

        int over = _lines.Count - MauiShellStore.Scrollback;
        if (over <= 0) return;
        _lines.RemoveRange(0, over);
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
        var cols = MauiShellStore.Cols;

        // ── 字号 ──
        // 字号**永远由缩放设置决定，不自动缩**。
        //
        // 曾经想的是"固定列数时把字号缩到 N 列正好铺满"——**那是把两件事搅在一起了**：
        // 固定大小的意思是"**字符格真的固定**"（老程序按 80 列排版，格子必须就是 80 个），
        // 而缩字号是"让内容塞进屏宽"，两者目的相反。用户点的名：**超出屏宽就横向滚动**。
        // （第一版按显示宽度把 80 列又折了一次，`ls -l` 的列对齐当场就散了。）
        var size = MauiShellStore.Font;
        _fontSize = size;

        // ── 内容宽度 / 滚动方向 ──
        // 文本已经由 `ShellWrap` 按字符格折好了；这里再给 Label 一个**显式宽度**
        // （= 列数 × 字符宽），于是：
        //   · `Label` 不会再按显示宽度**二次折行**（列对齐就这么保住的）
        //   · 宽度超出视口时由 ScrollView **横向滚动**（固定 80 列、或字号放大到装不下时）
        //
        // ⚠ **别改用 `LineBreakMode.NoWrap` 去"禁止折行"** —— MAUI 的 `NoWrap` 在 Android 上
        //   会走 `setSingleLine(true)`，**整段只剩第一行**（实测：提示行与提示符全没了）。
        //   "给显式宽度 + 双向滚动"才是真终端的做法，也不会丢行。
        var colsNow = EffectiveCols();
        if (colsNow > 0)
        {
            var want = ShellWrap.WidthForColumns(colsNow, size);

            // ⚠ **画面行比这更宽时，Label 必须跟着撑宽** —— 否则 `Label` 会按显示宽度
            //   把画面行**自己折一次**（上面那句 WidthRequest 管的是"我们折好的宽度"，
            //   画面行没折、比它宽，于是被平台二次折行 ⇒ 整幅画斜切）。
            //   撑宽之后由 ScrollView 横向滚 —— 与固定列数那条路同一个做法，
            //   「内容显示不全就出滚动条」本来就是用户定的规矩。
            var pictureCols = 0;
            foreach (var (l, noWrap) in _lines)
                if (noWrap || ShellWrap.IsPictureLine(l)) pictureCols = Math.Max(pictureCols, ShellWrap.VisibleWidth(l));
            if (pictureCols > 0)
            {
                // ⚠ 画面这一档**必须"宁大勿小"** —— 与文本折行那条规矩**正好相反**。
                //
                // 文本给窄了只是多折一行；而画面给窄了**整幅图被平台再折一次**，
                // 形状直接散掉（用户报的"对不齐"，实测右边框整条不见）。
                // 根子在 `ShellWrap.CharAspect = 0.6` 是**估**的（那份注释自己也写着
                // 「实测 ≈0.58、取 0.6 略保守」）—— 而保守的方向对文本合适、对画面有害。
                // 所以这里按 `1.15` 放大给宽：宽了只是多滚一点，窄了就是画面毁掉。
                var wantPic = ShellWrap.WidthForColumns(pictureCols, size) * 1.15;
                if (wantPic > want) want = wantPic;
            }

            _contentWidth = want;
        }
        else
        {
            _contentWidth = -1;                               // -1 = 交给布局
        }
        // 折行交给 `ShellWrap`；文本 Label 只用 `WordWrap` 保证 `\n` 生效（在 NewTextLabel 里设）
        if (OutputScroll.Orientation != ScrollOrientation.Both)
            OutputScroll.Orientation = ScrollOrientation.Both;

        // ── 固定行数：把输出区高度锁成"正好 N 行"，多出来的走滚动 ──
        // ⚠ 行高按 **字号 × 1.3** 估（平台字体度量拿不到精确行高时的通行做法）——
        //   所以**可见行数是近似的**；列数是精确的（那是按字符格折出来的）。
        var rows = MauiShellStore.Rows;
        var wantHeight = rows > 0 ? rows * size * LineHeightFactor : -1;
        if (Math.Abs(OutputScroll.HeightRequest - wantHeight) > 0.5)
            OutputScroll.HeightRequest = wantHeight;         // -1 = 交给布局算
        OutputScroll.VerticalOptions = rows > 0 ? LayoutOptions.Start : LayoutOptions.Fill;

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
        // ⚠ **按块分别渲染**：文本行走 `Label`（要 Markdown），**画面行走自绘网格**。
        //
        // 画面为什么不能也用 `Label`：平台会**折叠连续空格**（实测边框每行落在不同的 x），
        // 换 NBSP 也只能绕开一半（行宽仍短 7 个字符）。本仓在移动端编辑器上为同一件事
        // 折腾过八轮，结论是「**字符网格不许交给平台排版去量**」—— 所以这里自绘。
        OutputHost.Children.Clear();
        foreach (var group in GroupByNoWrap(DisplayText()))
        {
            if (group.NoWrap)
            {
                var grid = new TerminalGrid();
                grid.SetLines(group.Text.Split('\n'), _fontSize, MauiUi.IsDark);
                if (_contentWidth > 0) grid.WidthRequest = Math.Max(grid.WidthRequest, _contentWidth);
                AddOutputGestures(grid);
                OutputHost.Children.Add(grid);
                continue;
            }

            var label = NewTextLabel();
            label.FormattedText = MarkupToFormattedString.Convert(group.Text, MauiUi.IsDark);
            AddOutputGestures(label);
            OutputHost.Children.Add(label);
        }
    }

    /// <summary>建一个文本 Label —— 字号/宽度/折行规则**只在这一处设**。</summary>
    private Label NewTextLabel()
    {
        var label = new Label
        {
            FontFamily = EditorTypography.FontFamilyName,
            FontSize = _fontSize,
            LineBreakMode = LineBreakMode.WordWrap,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
        };
        if (_contentWidth > 0) label.WidthRequest = _contentWidth;
        return label;
    }

    /// <summary>把折好行的序列按 <c>NoWrap</c> **分成连续段**（同段一起渲染）。</summary>
    private static List<(string Text, bool NoWrap)> GroupByNoWrap(List<(string Text, bool NoWrap)> lines)
    {
        var outp = new List<(string Text, bool NoWrap)>();
        var sb = new System.Text.StringBuilder();
        bool? cur = null;
        foreach (var (text, noWrap) in lines)
        {
            if (cur != null && cur.Value != noWrap)
            {
                outp.Add((sb.ToString(), cur.Value));
                sb.Clear();
            }
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(text);
            cur = noWrap;
        }
        if (cur != null) outp.Add((sb.ToString(), cur.Value));
        return outp;
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
            foreach (var (line, noWrap) in _lines)
            {
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
            wrapped = wrapped.GetRange(wrapped.Count - rows, rows);

        return wrapped;
    }
}
