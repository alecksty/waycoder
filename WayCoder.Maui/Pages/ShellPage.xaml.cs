using System.Text;
using WayCoder.Maui.Controls;
using WayCoder.Maui.Services;
using WayCoder.Tools;

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

    public ShellPage()
    {
        InitializeComponent();

        // 等宽字体取自编辑器那份常量（与自绘编辑器同一个族）——
        // 不在这里另写字面量，否则将来换字体又是一处「同一规则两处实现」。
        OutputLabel.FontFamily = EditorTypography.FontFamilyName;
        PromptLabel.FontFamily = EditorTypography.FontFamilyName;
        CmdEntry.FontFamily = EditorTypography.FontFamilyName;

        // 输出区还挂一个手势：点空白处把焦点给输入框（省得每次都要去点那个窄窄的 Entry）
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => CmdEntry.Focus();
        OutputScroll.GestureRecognizers.Add(tap);

        Append("WayCoder 命令行\n" +
               "输入 shell 命令后按「运行」（或回车）。`cd` 会改变下面的工作目录。\n\n");
        RefreshCwd();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshCwd();
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
            // **`vml` 开头的命令不走 shell** —— 转交给进程内的 VML 虚拟机。
            // 这是手机上跑编译/模拟的唯一可行形态：iOS 根本没有 shell，
            // Android 也不该为了编译一段程序去起进程（W^X 那条路还得另塞 jniLibs）。
            if (cmd == "vml" || cmd.StartsWith("vml ", StringComparison.Ordinal))
            {
                Append(await RunVmlAsync(cmd) + "\n\n");
                return;
            }

            // ⚠ **不要包 `Task.Run`**：`CwdContext` 是 `AsyncLocal`，`cd` 的更新只在
            // 当前异步上下文里生效，丢到线程池上跑完就传不回来了 —— 表现是 `cd /sdcard` 之后
            // cwd 永远还显示初始值。桌面 `!` 直通也是直接 await 的。
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

    private void OnClearClicked(object? sender, EventArgs e)
    {
        _lines.Clear();
        _partial = false;
        OutputLabel.Text = "";
    }

    /// <summary>追加输出、按行裁剪、滚到底。</summary>
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
        OutputLabel.Text = string.Join("\n", _lines);

        // 排到下一拍再滚：此刻刚换完 Text，布局还没算，ContentSize 还是旧的。
        Dispatcher.Dispatch(() =>
        {
            try { OutputScroll.ScrollToAsync(0, OutputScroll.ContentSize.Height, animated: false); }
            catch { /* 页面正在销毁时滚动会抛，忽略 */ }
        });
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
