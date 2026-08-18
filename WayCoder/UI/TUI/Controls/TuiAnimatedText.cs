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
/// 仅当所属窗口有焦点（Window.Focused）时动画，失焦时静态显示完整文本。
///
/// DirectWrite 模式（directWrite="true"）：跳过 Dirty 标志与 frame buffer，
/// 由管理器在写完帧后统一调用 RenderDirect 直接把动画帧写到终端（高性能，不整帧重绘）。
/// </summary>
public class TuiAnimatedText : TuiControl
{
    private static readonly string[] Frames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];

    /// <summary>直接写屏模式的控制实例注册表（管理器 Render 末尾遍历）。</summary>
    private static readonly List<TuiAnimatedText> DirectWriters = [];

    /// <summary>要显示的字符串。</summary>
    public string Text { get; set; } = "";

    /// <summary>动画模式。</summary>
    public AnimatedTextMode Mode { get; set; } = AnimatedTextMode.Spinner;

    /// <summary>每帧间隔（毫秒）。</summary>
    public int FrameMs { get; set; } = 150;

    public override bool CanFocus => false;

    private bool _directWrite;
    private int _lastAbsX, _lastAbsY;

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

    private static long NowMs => DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

    private int Frame => (int)((NowMs / Math.Max(1, FrameMs)) % Frames.Length);

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        _lastAbsX = absX;
        _lastAbsY = absY;
        if (DirectWrite) return; // 直接写屏模式：不写 frame buffer

        ControlRenderer.DrawLabelLine(sb, this, absX, absY,
            Animate(), EHAlign.Left, EffectiveFg, EffectiveBg);
    }

    /// <summary>直接把当前动画帧写到终端（DirectWrite 模式用，管理器 Render 末尾调用）。</summary>
    public void RenderDirect()
    {
        var sb = new StringBuilder();
        sb.Append(AnsiTty.CursorPos0(_lastAbsY, _lastAbsX));
        if (EffectiveFg > 0 || EffectiveBg > 0)
            sb.Append(AnsiTty.FgBgCode(EffectiveFg, EffectiveBg));
        sb.Append(Animate());
        sb.Append(AnsiTty.SgrReset);
        Tty.Write(sb.ToString());
    }

    /// <summary>刷新所有直接写屏的动画控件（TuiManager.Render 末尾调用）。</summary>
    public static void RenderAllDirect()
    {
        foreach (var w in DirectWriters) w.RenderDirect();
    }

    private string Animate()
    {
        // 失焦 → 静态显示完整文本（不动画）
        if (Window?.Focused == false) return Text;

        return Mode switch
        {
            AnimatedTextMode.Spinner => $"{Frames[Frame]} {Text}",
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
        int offset = (int)((NowMs / Math.Max(1, FrameMs)) % total);

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
        int shown = (int)((NowMs / Math.Max(1, FrameMs)) % (runes.Count + 1));
        var sb = new StringBuilder();
        for (int i = 0; i < shown; i++) sb.Append(runes[i].ToString());
        return sb.ToString();
    }
}
