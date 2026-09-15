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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshCwd();
        Dispatcher.Dispatch(UpdateScrollBar);   // 回到本页时量一次（期间可能转过屏）
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

    private void RefreshCwd() => CwdLabel.Text = "cwd: " + CwdContext.Root;

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

    private async Task RunAsync(string cmd)
    {
        Append($"$ {cmd}\n");
        SetBusy(true);
        try
        {
            // 页面自己的命令走注册表（`BuildCommandRegistry` 那一处登记）。
            // **只要注册表不认识，就原样交给 shell** —— 分派逻辑只有这一处，
            // 加命令改 `BuildCommandRegistry` 一行，help 列表/用法/参数校验全跟着变。
            var handled = await _commands.DispatchAsync(cmd);
            if (handled != null)
            {
                Append(handled.TrimEnd() + "\n\n");
                return;
            }

            // 直接 await（不额外包 `Task.Run`）：这里本就在后台异步链上，包一层毫无收益。
            // （历史上这里必须这样写，因为 `CwdContext` 用 `AsyncLocal<string>` 直接存值，
            //   `cd` 的写入传不回线程池之外；现在 CwdContext 存的是「盒子」、就地改内容，
            //   cd 能跨任务边界回传，限制已不存在。）
            var result = await new BashTool().ExecuteUserShellAsync(cmd);
            Append(result.TrimEnd() + "\n\n");
        }
        catch (Exception ex)
        {
            Append($"⚠️ 执行异常：{ex.Message}\n\n");
        }
        finally
        {
            SetBusy(false);
            RefreshCwd();   // `cd` 之后 cwd 变了，顶栏要跟着动
        }
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
            "跑 VML 程序：`test` 跑内置自检，`run` 按扩展名选编译器（.vml 直接汇编，其余 22 种语言）",
            "编译并运行一段 VML。`vml test` 跑内置自检程序；`vml run <文件>` 按扩展名自动选前端编译器"
            + "（`.vml` 走汇编，`.c`/`.py`/`.rs` 等 22 种语言走各自编译器）。"
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
        if (rest.Length == 0 || rest == "help")
            return "用法：\n  vml test            跑内置的自检程序\n"
                 + "  vml run <文件>      .vml 走汇编，其余按扩展名自动选编译器（22 种语言）";

        string? source = null, file = null;
        if (rest == "test")
        {
            source = MauiVml.HelloWorldAsm;
        }
        else if (rest.StartsWith("run ", StringComparison.Ordinal))
        {
            // 路径在进后台线程**之前**解析（CwdContext 是 AsyncLocal）
            file = CwdContext.Resolve(rest[4..].Trim());
        }
        else
        {
            return $"⚠️ 不认识的 vml 子命令：{rest}（敲 `vml` 看用法）";
        }

        // **走 `MauiVml.Run`，与 AI 调 `vml` 工具是同一条流水线、同一份派发**
        // （不给它第二份实现——本仓库排第一的坑就是"同一规则两处实现"）。
        // 区别只有两个：这里给得了**交互式输入源**、超时给得宽（见 InteractiveTimeoutSec）。
        _interactive = true;
        try
        {
            return await Task.Run(() => MauiVml.Run(source, file, InteractiveTimeoutSec, ReadLineFromProgram));
        }
        finally
        {
            _interactive = false;
            StdinPanel.IsVisible = false;
        }
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

    private void SetBusy(bool busy)
    {
        _busy = busy;
        RunBtn.IsEnabled = !busy;
        RunBtn.Text = busy ? "…" : "运行";
        CmdEntry.IsEnabled = !busy;
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
