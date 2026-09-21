using System.Text;
using WayCoder.UI.Shared;

namespace WayCoder.UI.Shared.Terminal;

/// <summary>宿主要怎么呈现这一段（见 <see cref="ShellRender"/>）。</summary>
public enum ShellStreamMode
{
    /// <summary>还在看：攒着不输出（判据没到）。</summary>
    Probing,
    /// <summary>一路往下堆的线性输出：**追加**到命令行页。</summary>
    Linear,
    /// <summary>一屏一屏画的全屏程序：**替换**命令行页里那一块网格。</summary>
    Grid,
}

/// <summary>一次呈现请求。文本**已经是 markup**（`«red»…«/»`），宿主直接喂给 `Append(alreadyMarkup:true)`。</summary>
public sealed class ShellRender
{
    /// <summary>true = 整屏网格（**替换**那一块）；false = 线性文本（**追加**）。</summary>
    public bool IsGrid { get; init; }

    /// <summary>已经转好的 markup 文本。网格态是**整屏**（每行一个，`\n` 分隔）。</summary>
    public string Text { get; init; } = "";

    /// <summary>网格态的尺寸（线性态无意义）。</summary>
    public int Rows { get; init; }
    public int Cols { get; init; }

    /// <summary>网格态的光标位置（线性态无意义）。</summary>
    public int CursorRow { get; init; }
    public int CursorCol { get; init; }
    public bool CursorVisible { get; init; }
}

/// <summary>
/// **把一段边跑边来的输出，变成"该往屏幕上放什么"** —— 命令行窗实时化的核心。
///
/// <para>
/// 背景：原先的做法是**跑完之后**拿整段输出判一次
/// （`MauiVml.RunProgram` 的 `ScreenOutput.LooksFullScreen(io.Text)`），
/// 于是程序不退出就什么都不显示 —— 而 `top` / `vim` / `mc` / `cmatrix` 这一整类
/// （PC/Linux/老 Mac 程序里的绝大多数）**正是"不退出就一直画"**。
/// </para>
///
/// <para>
/// 三类输出要三种呈现，而**呈现方式一旦定下来就不可逆**（网格是"替换那一块"，
/// 线性是"往后追加"，混着来会把画面搅烂）。所以这里的做法是：
/// <b>先攒着不出，判定了再一次性决定</b> —— 也就不需要"已 append 的文本怎么撤回"那套逻辑。
/// </para>
///
/// <para>
/// 纯逻辑、无 MAUI 依赖 ⇒ 桌面自测能逐条钉（见 `SelfTest` 的「命令行流式输出」一组）。
/// </para>
/// </summary>
public sealed class ShellStream
{
    /// <summary>
    /// 一个 **ESC 都没见到**时，攒够这么多就认定线性。
    /// 取 0 的理由：没有 ESC 就**不可能**在做光标定位 ⇒ 当场就能定。
    /// </summary>
    private const int NoEscLimit = 0;

    /// <summary>
    /// 见到了 ESC、但一个光标定位序列都没有时，攒够这么多才认定线性。
    ///
    /// <para>
    /// 为什么不是 0：`ESC[31m` 这种**颜色**序列在"一路往下堆"的输出里非常常见
    /// （`ls --color` / `git status`），而全屏程序的开场（切备用屏 + 清屏 + 光标归位）
    /// 通常只有几十字节。等到 1KB 还没见到光标定位，就不是全屏程序了。
    /// </para>
    /// </summary>
    private const int HasEscLimit = 1024;

    /// <summary>硬上限：攒到这么多**无论如何**下结论（防止"永远下不了结论"把输出全扣住）。</summary>
    public const int ProbeLimit = 8192;

    private readonly int _rows;
    private readonly int _cols;
    private readonly StringBuilder _pending = new();
    private FrameBuffer? _fb;

    public ShellStreamMode Mode { get; private set; } = ShellStreamMode.Probing;

    /// <param name="rows">网格行数；0 用老程序通用的 25。</param>
    /// <param name="cols">网格列数；0 用老程序通用的 80。</param>
    public ShellStream(int rows = 0, int cols = 0)
    {
        _rows = rows > 0 ? rows : 25;
        _cols = cols > 0 ? cols : 80;
    }

    /// <summary>
    /// 喂一段**原始**输出（含 ANSI，未转义）。
    /// 返回 null = **还不需要更新画面**（判据没到，或者这段只在攒）。
    /// </summary>
    public ShellRender? Feed(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        _pending.Append(raw);

        if (Mode == ShellStreamMode.Probing) return Probe();
        if (Mode == ShellStreamMode.Grid) return FeedGrid();
        return EmitLinear();
    }

    /// <summary>
    /// 程序结束了：把残余吐出去。
    /// ⚠ **必须调**（放在 `finally` 里）—— 末尾没有换行的那一截、以及
    /// "程序很短、还没攒够判据就结束了"的情形，全靠它兜底。
    /// </summary>
    public ShellRender? Finish()
    {
        if (Mode == ShellStreamMode.Probing) return EnterLinear();   // 没定论 ⇒ 按线性吐出
        if (Mode == ShellStreamMode.Grid) return FeedGrid();
        return EmitLinear();
    }

    // ── 状态迁移 ──────────────────────────────────────────────

    private ShellRender? Probe()
    {
        string buf = _pending.ToString();

        if (ScreenOutput.Scan(buf, out bool truncated))
            return EnterGrid();

        /* ⚠ **截断时必须推迟结论**：块边界正好落在 `ESC[` 中间时，
           `Scan` 返回的 `false` 只是"这一段还没看出名堂"，不是"确定不是全屏"。
           照它判线性 ⇒ 全屏程序被永久误判（那一屏从此散成文本，不可逆）。
           实测踩点：`ScreenOutput.Scan` 的 out 参数就是为这条加的。 */
        if (truncated) return null;

        int limit = buf.IndexOf(AnsiString.AnsiCharPrefix) < 0 ? NoEscLimit : HasEscLimit;
        if (buf.Length >= limit && limit < ProbeLimit) return EnterLinear();

        return buf.Length >= ProbeLimit ? EnterLinear() : null;
    }

    private ShellRender EnterGrid()
    {
        Mode = ShellStreamMode.Grid;
        _fb = new FrameBuffer(_rows, _cols);
        _fb.Apply(_pending.ToString());
        _pending.Clear();
        return GridFrame();
    }

    private ShellRender EnterLinear()
    {
        Mode = ShellStreamMode.Linear;
        return EmitLinear();
    }

    // ── 两种呈现 ──────────────────────────────────────────────

    private ShellRender? EmitLinear()
    {
        string chunk = TakeSafe();
        if (chunk.Length == 0) return null;
        return new ShellRender { IsGrid = false, Text = MarkLinear(chunk) };
    }

    private ShellRender? FeedGrid()
    {
        string chunk = TakeSafe();
        if (chunk.Length == 0) return null;
        _fb!.Apply(chunk);
        return GridFrame();
    }

    private ShellRender GridFrame()
    {
        var lines = _fb!.DumpAnsi();
        return new ShellRender
        {
            IsGrid = true,
            Text = AnsiMarkup.ToMarkup(string.Join("\n", lines)).TrimEnd(),
            Rows = _rows,
            Cols = _cols,
            CursorRow = _fb.CursorRow,
            CursorCol = _fb.CursorCol,
            CursorVisible = _fb.CursorVisible,
        };
    }

    /// <summary>
    /// 取出待处理的字节，但**把结尾那半截 ANSI 序列留下** —— 它要等下一段拼齐。
    ///
    /// <para>
    /// 为什么必须留：输出是按块（时间/字节节流）送来的，`ESC[3` 被切开是常态。
    /// 直接交给 `AnsiMarkup.ToMarkup` 会多打一个字面量，交给 `FrameBuffer.Apply`
    /// 更糟 —— 那个半截序列会被当成正文吃进网格。
    /// </para>
    /// </summary>
    private string TakeSafe()
    {
        string all = _pending.ToString();
        _pending.Clear();

        int last = all.LastIndexOf(AnsiString.AnsiCharPrefix);
        if (last >= 0)
        {
            string tail = all[last..];
            ScreenOutput.Scan(tail, out bool truncated);
            if (truncated)
            {
                _pending.Append(tail);          // 半截序列：留着，下段拼齐再处理
                return all[..last];
            }
        }
        return all;
    }

    /// <summary>
    /// 线性文本的转义：**控制字符必须在 `ToMarkup` 之前解释**（`\r` 覆写行首、
    /// `\t` 跳制表位、`\b` 退格）—— `AnsiMarkup` 只保留颜色与样式，那三个会被吃掉，
    /// 于是进度条挤成一行、表格列全歪。与 `ShellPage.Append` 里那条同源。
    /// </summary>
    private static string MarkLinear(string raw) =>
        AnsiMarkup.ToMarkup(ShellControls.Apply(raw));
}
