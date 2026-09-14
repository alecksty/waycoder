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
    private const int MaxOutputChars = 120_000;

    private readonly StringBuilder _output = new();
    private readonly List<string> _history = [];
    private int _histIndex;
    private bool _busy;

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

    /// <summary>处理 <c>vml</c> 子命令（进程内执行，不经过 shell）。</summary>
    private static async Task<string> RunVmlAsync(string cmd)
    {
        var rest = cmd.Length <= 3 ? "" : cmd[3..].Trim();
        try
        {
            if (rest.Length == 0 || rest == "help")
                return "用法：\n  vml test            跑内置的自检程序\n"
                     + "  vml run <文件.vml>  汇编并运行一个 VML 文件";

            if (rest == "test")
                return await Task.Run(() => MauiVml.RunAssembly(MauiVml.HelloWorldAsm));

            if (rest.StartsWith("run ", StringComparison.Ordinal))
            {
                var path = rest[4..].Trim();
                var full = CwdContext.Resolve(path);
                if (!File.Exists(full)) return $"⚠️ 找不到文件：{full}";
                var src = await File.ReadAllTextAsync(full);
                return await Task.Run(() => MauiVml.RunAssembly(src));
            }

            return $"⚠️ 不认识的 vml 子命令：{rest}（敲 `vml` 看用法）";
        }
        catch (Exception ex)
        {
            // 汇编错误、VM 超时、被链接器裁掉…都在这里兜住，别让页面崩
            return $"⚠️ VML 执行失败：{ex.GetType().Name}: {ex.Message}";
        }
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
        _output.Clear();
        OutputLabel.Text = "";
    }

    /// <summary>追加输出并滚到底。</summary>
    private void Append(string text)
    {
        _output.Append(text);

        if (_output.Length > MaxOutputChars)
        {
            var all = _output.ToString();
            int cut = TrimHead(all, _output.Length - MaxOutputChars);
            _output.Clear();
            if (cut < all.Length) _output.Append(all, cut, all.Length - cut);
        }

        OutputLabel.Text = _output.ToString();

        // 排到下一拍再滚：此刻刚换完 Text，布局还没算，ContentSize 还是旧的。
        Dispatcher.Dispatch(() =>
        {
            try { OutputScroll.ScrollToAsync(0, OutputScroll.ContentSize.Height, animated: false); }
            catch { /* 页面正在销毁时滚动会抛，忽略 */ }
        });
    }

    /// <summary>
    /// 从头部砍掉约 <paramref name="minChars"/> 个字符，返回新的起点下标。
    ///
    /// 两点：① **按行切**（找 `\n`）而不是硬切 —— 半行输出看着像 bug；
    /// ② 找不到换行时（一条超长单行）硬切也要**避开 UTF-16 代理对**，
    /// 否则会把 emoji / CJK 扩展 B 劈成半个（本仓库的硬性约束：截断必须按码点）。
    /// </summary>
    private static int TrimHead(string s, int minChars)
    {
        int cut = s.IndexOf('\n', Math.Max(0, Math.Min(minChars, s.Length - 1)));
        if (cut < 0) cut = Math.Min(minChars, s.Length);
        else cut++;                                    // 连换行一起丢掉
        if (cut < s.Length && char.IsLowSurrogate(s[cut])) cut++;   // 别切在代理对中间
        return cut;
    }
}
