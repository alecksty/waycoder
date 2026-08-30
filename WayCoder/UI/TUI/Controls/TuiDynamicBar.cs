using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// Agent 动态状态（对标 Claude Code SpinnerMode）。
/// </summary>
public enum AgentStatus
{
    /// <summary>空闲 — 灰色 spinner + 就绪</summary>
    Idle,
    /// <summary>思考中 — 黄色 spinner + 模型名</summary>
    Thinking,
    /// <summary>工具执行 — 黄色 spinner + 工具详情</summary>
    ToolRunning,
    /// <summary>上下文压缩 — 黄色 spinner + 进度条</summary>
    Compressing,
    /// <summary>等待权限 — 黄色 spinner + 等待确认</summary>
    WaitingPerm,
    /// <summary>计划模式 — 黄色 spinner（只读分析，不执行写操作）</summary>
    Planning,
    /// <summary>错误 — 红色 spinner</summary>
    Error,
}

/// <summary>
/// 实时动态栏 —— 对标 Claude Code SpinnerWithVerb。
/// 位于聊天列表和输入区之间，始终可见，显示模型状态、当前任务、压缩进度。
///
/// 布局（1 行，3 段）：
///   ⣾ 思考中... gpt-5.4  │  ⚙ bash: dotnet build  │  «████░░░░» 45%
/// </summary>
public class TuiDynamicBar : TuiDisplayControl
{
    // ═══════════════════════════════════════════════════════════
    // 动画帧（对标 Claude Code ·✢✳✶✻✽ ping-pong 循环）
    // ═══════════════════════════════════════════════════════════

    private static readonly string[] Frames = ["⣾", "⣽", "⣻", "⢿", "⡿", "⣟", "⣯", "⣷"];

    /// <summary>动画帧间隔（毫秒）。每帧由 ChatScreen 按此节流标脏，250ms 一帧 ≈ 4 FPS ——
    /// 够看出在转，又不至于逐帧整条重绘造成卡顿。</summary>
    public const int FrameMs = 250; // 250ms/帧 ≈ 4 FPS：流畅又不卡

    /// <summary>按 Agent 状态取 spinner 前景色（DirectWrite 直写与 OnRender 共用）。</summary>
    private static int SpinnerFg(AgentStatus st) => st switch
    {
        // 动画图标统一用黄色（用户要求橙/黄），错误仍红
        AgentStatus.Thinking => AnsiColors.Yellow,
        AgentStatus.ToolRunning => AnsiColors.Yellow,
        AgentStatus.Compressing => AnsiColors.Yellow,
        AgentStatus.WaitingPerm => AnsiColors.Yellow,
        AgentStatus.Planning => AnsiColors.Yellow,
        AgentStatus.Error => AnsiColors.Red,
        _ => AnsiColors.BrightBlack,
    };

    /// <summary>spinner 动画直写屏幕（不依赖 dirty 整条重绘）：记录位置，RenderAllDirect 时写当前帧。
    /// owner 为所属屏幕，用于直写门控（活跃屏幕一致 + 栈顶无窗口才直写，避免画到别的屏幕或覆盖对话框）。</summary>
    public void RegisterDirectWrite(TuiScreen? owner = null)
    {
        _owner = owner;
        if (!DirectWriters.Contains(this)) DirectWriters.Add(this);
    }

    private TuiScreen? _owner; // 所属屏幕：直写门控

    public override void OnDestroy()
    {
        DirectWriters.Remove(this);
        base.OnDestroy();
    }

    /// <summary>直接把当前 spinner 帧写到终端（TuiManager.Render 末尾调用，不等 dirty）。
    /// 门控：自己不在活跃屏幕不写（直写坐标已失效）；栈顶有窗口不写（直写会把 spinner 画到对话框/浮层上）。</summary>
    public void RenderDirect()
    {
        if (_spinnerX <= 0) return;
        if (TuiManager.Instance?.ActiveScreen != _owner) return; // 不在当前屏幕 → 直写污染别的屏幕
        if (_owner?.FocusedWindow != null) return;               // 有窗口覆盖 → 直写破坏窗口像素
        var sb = new StringBuilder();
        // 动画图标旁不显示闪烁光标：直写前隐藏（EmitCursor 稍后会把光标恢复到输入区）
        sb.Append(AnsiTty.CursorHide);
        sb.Append(AnsiTty.CursorPos0(_spinnerY, _spinnerX));
        int fg = SpinnerFg(Status); // 直写方法独立计算 spinner 色（不依赖 OnRender 局部变量）
        sb.Append(AnsiTty.FgBgCode(fg, AnsiColors.BgBlack));
        sb.Append(CurrentFrame);
        sb.Append(AnsiTty.SgrReset);
        Tty.Write(sb.ToString());
    }

    /// <summary>刷新所有直写 spinner 的动态栏（TuiManager.Render 末尾调用 + 独立动画心跳线程）。
    /// 快照迭代：DirectWriters 可能在 UI 线程增删（RegisterDirectWrite/OnDestroy），
    /// 独立动画线程读它时若并发修改会抛 InvalidOperationException —— 用 ToArray 快照避免。</summary>
    public static void RenderAllDirect()
    {
        foreach (var w in DirectWriters.ToArray()) w.RenderDirect();
    }

    /// <summary>基于时钟的当前帧（无需 Tick）。先对帧数取模再强转 int——
    /// 毫秒数/500 远超 int.MaxValue（2026 年约 1.28e11），直接 (int) 会溢出为负数索引导致 IndexOutOfRange。</summary>
    private static string CurrentFrame =>
        Frames[(int)((DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond / FrameMs) % Frames.Length)];

    // ═══════════════════════════════════════════════════════════
    // 公开属性
    // ═══════════════════════════════════════════════════════════

    /// <summary>当前代理状态</summary>
    public AgentStatus Status { get; set; } = AgentStatus.Idle;

    /// <summary>左段：状态文本（如 "思考中... deepseek-v4-pro"）</summary>
    public string LeftText { get; set; } = "";

    /// <summary>中段：工具/任务文本（如 "⚙ bash: dotnet build"）</summary>
    public string ToolText { get; set; } = "";

    /// <summary>压缩进度百分比（null=不显示）</summary>
    public double? ProgressPercent { get; set; }

    /// <summary>压缩进度标签</summary>
    public string ProgressLabel { get; set; } = "";

    /// <summary>上下文占用百分比（null=不显示，常驻右段，绿→黄→红）</summary>
    public double? ContextPercent { get; set; }

    /// <summary>CPU 占用百分比（null=不显示，常驻右段 ContextPercent 之后，绿→黄→红）</summary>
    public double? CpuPercent { get; set; }

    /// <summary>token 消耗显示串（如 "大:12K 小:3K"，null/空=不显示，常驻右段）</summary>
    public string? TokenDisplay;

    /// <summary>花费显示（如 "¥0.42"，null/空=不显示，常驻右段）</summary>
    public string? CostDisplay;

    /// <summary>是否处于任务活跃状态（显示 spinner）</summary>
    public bool IsActive => Status != AgentStatus.Idle;


    private static readonly List<TuiDynamicBar> DirectWriters = [];
    private int _spinnerX, _spinnerY; // 渲染时记录 spinner 位置（DirectWrite 直写用）

    public TuiDynamicBar()
    {
        Height = 1;
        Width = 80;
        Bg = 0; // 透明背景，由 ChatScreen 的底色衬托
    }

    // ═══════════════════════════════════════════════════════════
    // 渲染
    // ═══════════════════════════════════════════════════════════

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        // 裁剪检查
        if (absY < ClipTop || absY >= ClipBottom) return;

        var rb = new RenderBuffer();

        // 整行底色（分隔效果）
        int left = Math.Max(absX, ClipLeft);
        int right = Math.Min(absX + Width, ClipRight);
        if (left < right)
            rb.Write(absY, left, new string(' ', right - left),
                fg: AnsiColors.BrightBlack, bg: AnsiColors.BgBlack);

        // 根据状态计算颜色（spinner 统一用 SpinnerFg 的黄色，与 DirectWrite 直写一致）
        // 文字统一橙色（255,180,0 同对话框渐变起始色），Error 保留红便于区分
        var (spinnerColor, textColor) = Status switch
        {
            AgentStatus.Error => (AnsiColors.Red, AnsiColors.Red),
            _ => (AnsiColors.Yellow, AnsiTty.RgbCode(255, 180, 0)),
        };

        // ── 左段：动画字符位（预留 1 字符 + 空格，活跃=spinner / 空闲=占位空格）──
        // 始终占位让左段文字水平位置稳定：idle→active 切换时状态文本不左右跳。
        _spinnerX = absX + 1; _spinnerY = absY; // 记录 spinner 位置（DirectWrite 直写用）
        int col = absX + 1;
        rb.Write(absY, col, CurrentFrame, // spinner 常驻转（空闲灰、活跃彩）
            fg: IsActive ? spinnerColor : AnsiColors.BrightBlack, bg: AnsiColors.BgBlack);
        col += 2;

        var leftDisplay = LeftText;
        if (string.IsNullOrEmpty(leftDisplay))
        {
            leftDisplay = Status switch
            {
                AgentStatus.Idle => "就绪",
                AgentStatus.Thinking => "思考中...",
                AgentStatus.ToolRunning => "工具执行",
                AgentStatus.Compressing => "压缩中",
                AgentStatus.Planning => "计划模式 🧠",
                AgentStatus.WaitingPerm => "等待确认",
                AgentStatus.Error => "错误",
                _ => "就绪",
            };
        }

        int leftWidth = Math.Min(AnsiHelper.DisplayWidth(leftDisplay), Width / 3 - 3);
        if (leftWidth > 0)
        {
            var leftStr = AnsiHelper.TruncateByWidth(leftDisplay, leftWidth);
            rb.Write(absY, col, leftStr, fg: textColor, bg: AnsiColors.BgBlack);
            col += AnsiHelper.DisplayWidth(leftStr);
        }

        // ── 分段留白（不画分隔竖线，靠间距区分左/中/右段）──
        int midStart = absX + Width / 3;
        if (col < midStart)
        {
            rb.Write(absY, col, new string(' ', midStart - col),
                fg: AnsiColors.BrightBlack, bg: AnsiColors.BgBlack);
        }
        col = midStart + 2;

        // ── 中段：工具/任务 ──
        int midWidth = Width / 3 - 4;
        var toolDisplay = ToolText;
        if (!string.IsNullOrEmpty(toolDisplay) && midWidth > 0)
        {
            if (AnsiHelper.DisplayWidth(toolDisplay) > midWidth)
                toolDisplay = AnsiHelper.TruncateByWidth(toolDisplay, midWidth);
            rb.Write(absY, col, toolDisplay, fg: AnsiColors.Grey, bg: AnsiColors.BgBlack);
        }
        col = midStart + Width / 3;

        // ── 右段留白 ──
        int rightStart = absX + Width * 2 / 3;
        col = rightStart + 2;

        // ── 右段：进度条 ──
        if (ProgressPercent.HasValue)
        {
            var pct = ProgressPercent.Value;
            int barW = Math.Min(14, Width - (col - absX) - 4);
            if (barW > 0)
            {
                int filled = Math.Clamp((int)Math.Round(barW * pct / 100.0), 0, barW);
                int empty = barW - filled;
                var barFg = pct switch { < 30 => AnsiColors.Green, < 70 => AnsiColors.Yellow, _ => AnsiColors.Red };
                rb.Write(absY, col,
                    $"«{new string('█', filled)}{new string('░', empty)}»",
                    fg: barFg, bg: AnsiColors.BgBlack);
                col += barW + 4;

                var pctStr = $" {pct,3:F0}%";
                rb.Write(absY, col, pctStr, fg: barFg, bg: AnsiColors.BgBlack);
            }
        }
        else if (!string.IsNullOrEmpty(ProgressLabel))
        {
            var label = ProgressLabel;
            int maxW = Width - (col - absX);
            if (AnsiHelper.DisplayWidth(label) > maxW)
                label = AnsiHelper.TruncateByWidth(label, maxW);
            rb.Write(absY, col, label, fg: AnsiColors.BrightBlack, bg: AnsiColors.BgBlack);
        }
        else
        {
            // 常驻上下文占用%（空闲/思考/工具态均显示，绿→黄→红）
            // 右段空间有限（约 1/3 宽）：信息多时靠后的项（🔤/¥）丢最靠前的（📦 已省略，
            // 上下文 token 量用 📊 占比表达，不重复）。宽度保护防溢出到分隔线。
            int rightEnd = absX + Width - 1; // 右段可用终点
            bool HasRoom(int extra) => col + extra <= rightEnd;

            if (ContextPercent.HasValue && HasRoom(7))
            {
                var pct = ContextPercent.Value;
                var ctxFg = pct switch { < 30 => AnsiColors.Green, < 70 => AnsiColors.Yellow, _ => AnsiColors.Red };
                var ctxStr = $"📊{pct,3:F0}%"; // 紧凑：去空格
                rb.Write(absY, col, ctxStr, fg: ctxFg, bg: AnsiColors.BgBlack);
                col += AnsiHelper.DisplayWidth(ctxStr) + 1;
            }
            // CPU 占用%（⚡ 前缀区分；阈值 <50 绿 <70 黄 ≥70 红）
            if (CpuPercent.HasValue && HasRoom(6))
            {
                var cp = CpuPercent.Value;
                var fg = cp switch { < 50 => AnsiColors.Green, < 70 => AnsiColors.Yellow, _ => AnsiColors.Red };
                var s = $"⚡{cp,3:F0}%"; // 紧凑：去空格
                rb.Write(absY, col, s, fg: fg, bg: AnsiColors.BgBlack);
                col += AnsiHelper.DisplayWidth(s) + 1;
            }
            // token 消耗（🔤）：剩余宽度不足时截断
            if (!string.IsNullOrEmpty(TokenDisplay) && col < rightEnd)
            {
                var td = TokenDisplay;
                int avail = rightEnd - col;
                if (AnsiHelper.DisplayWidth(td) > avail)
                    td = AnsiHelper.TruncateByWidth(td, avail);
                rb.Write(absY, col, td, fg: AnsiColors.Grey, bg: AnsiColors.BgBlack);
                col += AnsiHelper.DisplayWidth(td) + 1;
            }
            // 花费（¥）
            if (!string.IsNullOrEmpty(CostDisplay) && HasRoom(7))
            {
                rb.Write(absY, col, CostDisplay, fg: AnsiColors.Yellow, bg: AnsiColors.BgBlack);
                col += AnsiHelper.DisplayWidth(CostDisplay) + 1;
            }
            // 模型/模式信息统一由输入区下方模型栏显示，动态栏不放（重复）。
        }

        sb.Append(rb.ToString());
    }
}
