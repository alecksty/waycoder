using System.Text;
using WayCoder.Maui.Controls;
using WayCoder.Maui.Services;
using WayCoder.Tools;
using WayCoder.UI.Shared;

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
    private const int MaxScrollbackLines = 256;

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
    private readonly List<string> _lines = [];

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
    /// 当前这次 VML 运行的取消源（= 「强制停止」的手柄）。
    ///
    /// 只有 VML 运行才有它：VM 主循环每条指令查一次 token，取消是即时的；
    /// 而普通 shell 命令（`dotnet build` 之类）**没有可用的中断入口**，
    /// 所以"运行中不许返回"的拦截只对 VML 生效（见 <see cref="OnBackButtonPressed"/>）。
    /// </summary>
    private CancellationTokenSource? _runCts;

    /// <summary>
    /// 这次运行**彻底收干净**的信号（在 <see cref="ExecVmlAsync"/> 的 `finally` 最末尾置位）。
    ///
    /// 为什么不是直接 `await` 那个运行 Task：它和 `ExecVmlAsync` 里那句 `await task`
    /// 挂在**同一个 Task 的完成回调**上，谁先恢复没有保证 —— 若我们这边先恢复，
    /// 此刻 `_runCts` 还没被清空，重发的返回会撞上同一个拦截、再弹一次框。
    /// 用"finally 最后一步"的信号，就把它钉成了确定顺序。
    /// </summary>
    private TaskCompletionSource? _runDone;

    /// <summary>本页认识的命令（加命令改 <see cref="BuildCommandRegistry"/> 一处即可）。</summary>
    private readonly ShellCommandRegistry _commands;

    public ShellPage()
    {
        InitializeComponent();

        _commands = BuildCommandRegistry();

        // 等宽字体取自编辑器那份常量（与自绘编辑器同一个族）——
        // 不在这里另写字面量，否则将来换字体又是一处「同一规则两处实现」。
        OutputLabel.FontFamily = EditorTypography.FontFamilyName;
        PromptLabel.FontFamily = EditorTypography.FontFamilyName;
        CmdEntry.FontFamily = EditorTypography.FontFamilyName;

        // 点输出区把焦点给输入框（省得每次都要去点那个窄窄的 Entry）。
        //
        // ⚠ **手势必须挂在内容 Label 上，不能挂在 ScrollView 上。**
        // 挂在 ScrollView 上时，MAUI 会把手势监听装到 ScrollView 自己的平台视图上，
        // Android 侧 ACTION_DOWN 被消费掉 ⇒ **整个输出区再也拖不动**（实测：滑动后
        // 逐像素比对两张截屏，差异只落在底部导航栏，正文一个像素没动）。
        // 挂在内容上是另一条路（事件先给子视图，拖拽仍由 ScrollView 接管）。
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => CmdEntry.Focus();
        OutputLabel.GestureRecognizers.Add(tap);

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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshCwd();
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
            () => ExecVmlAsync(null, absPath));

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

                var (text, err2) = MauiVml.CompileToVml(absPath);
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

            return error != null
                ? error
                : $"✔ 已生成 {outRel}（{note}）\n回文件页点它选「VML 运行」即可执行。";
        });
    }

    /// <summary>
    /// 尺寸变了要重新量：视口变高可能让"本来超屏"变成"不超屏"，滚动条得跟着消失。
    /// （只依赖 Scrolled 是不够的 —— 转屏后没有滚动事件，滚动条会赖着不走。）
    /// </summary>
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
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
    }

    /// <summary>
    /// 跑一条命令的**统一外壳** —— 「运行中 / 已结束」的边界全靠它：
    /// 起点写一行 <c>路径&gt; 命令</c>，终点再写一行空的 <c>路径&gt;</c> 表示"等下一个命令"。
    ///
    /// 三个入口（手敲的命令、文件页递来的运行、文件页递来的编译）共用这一份 ——
    /// 各写一遍的话，总有一个会漏掉收尾提示符，用户就又分不清"跑完了没有"了。
    /// </summary>
    private async Task RunWithPromptAsync(string cmdLine, Func<Task<string>> body)
    {
        Append($"{Prompt} {cmdLine}\n");
        SetBusy(true);
        try
        {
            Append((await body()).TrimEnd() + "\n\n");
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

    private Task RunAsync(string cmd) => RunWithPromptAsync(cmd, async () =>
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
    });

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
            args => RunVmlAsync(string.Join(' ', args.Prepend("vml")))));

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
            return await Task.Run(() => MauiVml.Run(source, file, InteractiveTimeoutSec, ReadLineFromProgram, cts.Token));
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

    /// <summary>清空输出（按钮与 <c>clear</c>/<c>cls</c> 命令共用这一份）。</summary>
    private void ClearOutput()
    {
        _lines.Clear();
        _partial = false;
        OutputLabel.Text = "";
        UpdateScrollBar();
    }

    /// <summary>追加输出、按行裁剪、按需滚到底。</summary>
    private void Append(string text)
    {
        // 按 \n 切段并入缓冲：有换行的段落是**整行**，末尾没换行的那段是**半行**
        // （与上一段半行拼起来，而不是另起一行）。
        int start = 0;
        while (start < text.Length)
        {
            int nl = text.IndexOf('\n', start);
            if (nl < 0)
            {
                AddSegment(text[start..], partial: true);
                break;
            }
            AddSegment(text[start..nl], partial: false);
            start = nl + 1;
        }

        TrimScrollback();

        // **只有原本就贴着底，才跟着滚到底。**
        //
        // 原来是无条件弹到底 —— 一条命令持续吐输出（编译、下载、日志）时，
        // 用户往回翻一屏都做不到：每次新输出都把他拽回最底下。
        // 现在按终端的老规矩：贴底才跟随，一旦往上滚就"脱钩"，让用户安安静静看历史。
        var follow = ScrollBarMath.IsAtBottom(ContentHeight, OutputScroll.Height, OutputScroll.ScrollY);

        OutputLabel.Text = string.Join("\n", _lines);

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
    private double ContentHeight => OutputLabel.Height;

    // ── 输出区滚动条 ──────────────────────────────────────────────
    //
    // 几何一律走 `ScrollBarMath`（那一份是纯函数、有断言），这里只做"量一下、摆一下"。

    /// <summary>滑块当前顶端位置（拖动时作为增量基准）。</summary>
    private double _thumbTop;
    private double _thumbHeight;
    private double _panStartTop;
    private bool _panningThumb;

    /// <summary>重新量内容/视口，决定滚动条显不显示、滑块摆在哪。</summary>
    private void UpdateScrollBar()
    {
        var content = ContentHeight;      // Label 实测高度，不是 ContentSize（见 ContentHeight 注释）
        var viewport = OutputScroll.Height;

        // 内容不超屏 → 整条藏起来（用户要的就是"不超屏不显示滚动条"）
        if (!ScrollBarMath.ShouldShow(content, viewport))
        {
            ScrollTrack.IsVisible = false;
            return;
        }

        ScrollTrack.IsVisible = true;
        var track = ScrollTrack.Height > 0 ? ScrollTrack.Height : ScrollTrack.HeightRequest;
        if (track <= 0) return;   // 布局还没量出来，等下一拍

        var (top, height) = ScrollBarMath.Thumb(content, viewport, OutputScroll.ScrollY, track);
        _thumbTop = top;
        _thumbHeight = height;
        ScrollThumb.HeightRequest = height;
        ScrollThumb.TranslationY = top;
    }

    private void OnOutputScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_panningThumb) return;   // 拖自己触发的滚动不用回写（回写会和手指打架）
        UpdateScrollBar();
    }

    /// <summary>拖动滑块。</summary>
    private void OnScrollThumbPan(object? sender, PanUpdatedEventArgs e)
    {
        var track = ScrollTrack.Height;
        var content = ContentHeight;      // 同上：Label 实测高度
        var viewport = OutputScroll.Height;
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
        var viewport = OutputScroll.Height;
        try { OutputScroll.ScrollToAsync(0, OutputScroll.ScrollY + viewport * 0.9, animated: true); }
        catch { /* 页面正在销毁 */ }
    }

    private void AddSegment(string segment, bool partial)
    {
        if (_partial) _lines[^1] += segment;   // 接上没写完的那半行
        else _lines.Add(segment);
        _partial = partial;
    }

    /// <summary>
    /// 超过上限就从**最老的整行**开始丢，一次丢到刚好剩 <see cref="MaxScrollbackLines"/> 行。
    /// 只按整行丢 ⇒ 不会出现"断头的半行"。
    /// </summary>
    private void TrimScrollback()
    {
        int over = _lines.Count - MaxScrollbackLines;
        if (over <= 0) return;
        _lines.RemoveRange(0, over);
    }
}
