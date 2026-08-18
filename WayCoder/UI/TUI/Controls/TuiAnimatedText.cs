using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>动画文本模式。</summary>
public enum AnimatedTextMode
{
    /// <summary>旋转指示符 + 文本（如 ⠋ 加载中…）</summary>
    Spinner,
    /// <summary>文本超宽时横向滚动（跑马灯）</summary>
    Marquee,
    /// <summary>逐字显示（打字机）</summary>
    Typewriter,
}

/// <summary>
/// 动画文本控件 —— 时间驱动的连续动画显示字符串。
///
/// - 焦点门控：仅父窗口有焦点时动画，失焦/停止时静态显示完整文本。
/// - 裁剪：动画内容始终裁剪到控件 Width，不越界、不影响其他控件。
/// - SGR 复位：每次输出后复位样式，不污染后续控件绘制。
/// - 可自定义每帧内容：CustomFrames（固定帧序列）或 FrameProvider（回调按帧索引返回内容）。
/// - DirectWrite：跳过 Dirty 标志与 frame buffer，由管理器 Render 末尾统一直接写终端。
/// </summary>
public class TuiAnimatedText : TuiControl
{
    private static readonly string[] SpinnerFrames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];

    /// <summary>最小动画间隔（毫秒）。</summary>
    public const int MinFrameMs = 100;

    private static readonly List<TuiAnimatedText> DirectWriters = [];

    /// <summary>要显示的字符串。</summary>
    public string Text { get; set; } = "";

    /// <summary>动画模式（无自定义帧时生效）。</summary>
    public AnimatedTextMode Mode { get; set; } = AnimatedTextMode.Spinner;

    /// <summary>每帧间隔（毫秒），强制最小 100ms。</summary>
    public int FrameMs { get; set; } = 150;

    /// <summary>用户自定义的固定帧序列（非空时优先于 Mode）。</summary>
    public List<string>? CustomFrames { get; set; }

    /// <summary>用户自定义帧回调（按帧索引返回内容，code-behind 用；优先于 CustomFrames）。</summary>
    public Func<int, string>? FrameProvider { get; set; }

    public override bool CanFocus => false;

    private bool _running = true;
    private bool _directWrite;

    /// <summary>是否直接写屏（true=跳过 frame buffer，由 RenderDirect 直接写终端）。</summary>
    public bool DirectWrite
    {
        get => _directWrite;
        set
        {
            _directWrite = value;
            if (value) { if (!DirectWriters.Contains(this)) DirectWriters.Add(this); }
            else DirectWriters.Remove(this);
        }
    }

    public TuiAnimatedText() { Height = 1; }

    /// <summary>是否正在动画。</summary>
    public bool IsRunning => _running;

    /// <summary>开始（恢复）动画。</summary>
    public void Start() => _running = true;

    /// <summary>停止动画（静态显示完整文本）。</summary>
    public void Stop() => _running = false;

    /// <summary>控件销毁时从静态 DirectWriters 列表移除，防陈旧控件继续写屏（泄漏）。</summary>
    public override void OnDestroy()
    {
        DirectWriters.Remove(this);
        base.OnDestroy();
    }

    private static long NowMs => DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

    private int Interval => Math.Max(MinFrameMs, FrameMs);

    /// <summary>
    /// 当前帧索引（时间驱动的递增帧号，大周期循环防溢出）。
    /// 各调用方再按各自帧数取模：Spinner 取 %SpinnerFrames.Length、CustomFrames 取 %Count、
    /// FrameProvider 收到递增帧号自行处理（修复此前 total=1 导致 FrameProvider 恒收到帧 0 的 bug）。
    /// </summary>
    private int FrameIndex => (int)((NowMs / Interval) % 1_000_000);

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        if (DirectWrite) return; // 直接写屏模式：不写 frame buffer

        ControlRenderer.DrawLabelLine(sb, this, absX, absY,
            Clip(CurrentFrameContent()), EHAlign.Left, EffectiveFg, EffectiveBg);
    }

    /// <summary>直接把当前动画帧写到终端（DirectWrite 模式用，管理器 Render 末尾调用）。</summary>
    public void RenderDirect()
    {
        var sb = new StringBuilder();
        sb.Append(AnsiTty.CursorPos0(_lastAbsY, _lastAbsX));
        if (EffectiveFg > 0 || EffectiveBg > 0)
            sb.Append(AnsiTty.FgBgCode(EffectiveFg, EffectiveBg));
        sb.Append(Clip(CurrentFrameContent()));
        sb.Append(AnsiTty.SgrReset); // 复位样式，不污染后续绘制
        Tty.Write(sb.ToString());
    }

    /// <summary>刷新所有直接写屏的动画控件（TuiManager.Render 末尾调用）。</summary>
    public static void RenderAllDirect()
    {
        foreach (var w in DirectWriters) w.RenderDirect();
    }

    /// <summary>裁剪到控件宽度，不越界。</summary>
    private string Clip(string s)
    {
        if (AnsiHelper.DisplayWidth(s) <= Width) return s;
        return AnsiHelper.TruncateByWidth(s, Width);
    }

    private string CurrentFrameContent()
    {
        // 停止或失焦 → 静态显示完整文本
        if (!_running || Window?.Focused == false)
            return CustomFrames is { Count: > 0 } cf ? cf[0] : Text;

        // 用户自定义帧（回调 > 固定帧序列 > 内置模式）
        if (FrameProvider != null) return FrameProvider(FrameIndex);
        if (CustomFrames is { Count: > 0 } frames) return frames[FrameIndex % frames.Count];

        return Mode switch
        {
            AnimatedTextMode.Spinner => $"{SpinnerFrames[FrameIndex % SpinnerFrames.Length]} {Text}",
            AnimatedTextMode.Marquee => Marquee(),
            AnimatedTextMode.Typewriter => Typewriter(),
            _ => Text,
        };
    }

    /// <summary>跑马灯：文本超宽时循环滚动窗口。</summary>
    private string Marquee()
    {
        int tw = AnsiHelper.DisplayWidth(Text);
        if (tw <= Width) return Text;

        var runes = Text.EnumerateRunes().ToList();
        int total = runes.Count;
        int offset = (int)((NowMs / Interval) % total);

        var sb = new StringBuilder();
        int w = 0;
        for (int i = 0; i < total; i++)
        {
            var rune = runes[(offset + i) % total];
            int rw = AnsiString.CharWidth(rune);
            if (w + rw > Width) break;
            sb.Append(rune.ToString());
            w += rw;
        }
        return sb.ToString();
    }

    /// <summary>打字机：逐字显示，循环。</summary>
    private string Typewriter()
    {
        var runes = Text.EnumerateRunes().ToList();
        int shown = (int)((NowMs / Interval) % (runes.Count + 1));
        var sb = new StringBuilder();
        for (int i = 0; i < shown; i++) sb.Append(runes[i].ToString());
        return sb.ToString();
    }
}
