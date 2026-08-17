using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Shared;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// Agent 动态状态（对标 Claude Code SpinnerMode）。
/// </summary>
public enum AgentStatus
{
    /// <summary>空闲 — 无动画，灰字</summary>
    Idle,
    /// <summary>思考中 — 绿色 spinner + 模型名</summary>
    Thinking,
    /// <summary>工具执行 — 黄色 spinner + 工具详情</summary>
    ToolRunning,
    /// <summary>上下文压缩 — 蓝色 spinner + 进度条</summary>
    Compressing,
    /// <summary>等待权限 — 橙色闪烁</summary>
    WaitingPerm,
    /// <summary>计划模式 — 紫色（只读分析，不执行写操作）</summary>
    Planning,
    /// <summary>错误 — 红色</summary>
    Error,
}

/// <summary>
/// 实时动态栏 —— 对标 Claude Code SpinnerWithVerb。
/// 位于聊天列表和输入区之间，始终可见，显示模型状态、当前任务、压缩进度。
///
/// 布局（1 行，3 段）：
///   ⣾ 思考中... gpt-5.4  │  ⚙ bash: dotnet build  │  «████░░░░» 45%
/// </summary>
public class TuiDynamicBar : TuiControl
{
    // ═══════════════════════════════════════════════════════════
    // 动画帧（对标 Claude Code ·✢✳✶✻✽ ping-pong 循环）
    // ═══════════════════════════════════════════════════════════

    private static readonly string[] Frames = ["⣾", "⣽", "⣻", "⢿", "⡿", "⣟", "⣯", "⣷"];
    private const int FrameMs = 150; // 150ms/帧 ≈ 6.7 FPS

    /// <summary>基于时钟的当前帧（无需 Tick）</summary>
    private static string CurrentFrame =>
        Frames[(int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond / FrameMs) % Frames.Length];

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

    /// <summary>是否处于任务活跃状态（显示 spinner）</summary>
    public bool IsActive => Status != AgentStatus.Idle;

    public override bool CanFocus => false;

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
                fg: TuiColors.BrightBlack, bg: TuiTheme.Current.WindowBg);

        // 根据状态计算颜色
        var (spinnerColor, textColor) = Status switch
        {
            AgentStatus.Thinking => (TuiColors.Green, TuiColors.Grey),
            AgentStatus.ToolRunning => (TuiColors.Yellow, TuiColors.BrightBlack),
            AgentStatus.Compressing => (TuiColors.Cyan, TuiColors.Grey),
            AgentStatus.WaitingPerm => (TuiColors.Yellow, TuiColors.Yellow),
            AgentStatus.Planning => (TuiColors.Magenta, TuiColors.Grey),
            AgentStatus.Error => (TuiColors.Red, TuiColors.Red),
            _ => (TuiColors.BrightBlack, TuiColors.BrightBlack),
        };

        // ── 左段：spinner + 状态 ──
        int col = absX + 1;
        if (IsActive)
        {
            rb.Write(absY, col, CurrentFrame + " ", fg: spinnerColor, bg: TuiTheme.Current.WindowBg);
            col += 2;
            // spinner 是持续动画：自我置脏让下一帧不早退（空闲时也能看到旋转）
            if (TuiManager.Instance != null) TuiManager.Instance.IsDirty = true;
        }

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

        int leftWidth = Math.Min(TuiHelper.DisplayWidth(leftDisplay), Width / 3 - 3);
        if (leftWidth > 0)
        {
            var leftStr = TuiHelper.TruncateByWidth(leftDisplay, leftWidth);
            rb.Write(absY, col, leftStr, fg: textColor, bg: TuiTheme.Current.WindowBg);
            col += TuiHelper.DisplayWidth(leftStr);
        }

        // ── 分隔 ──
        int midStart = absX + Width / 3;
        if (col < midStart)
        {
            rb.Write(absY, col, new string(' ', midStart - col),
                fg: TuiColors.BrightBlack, bg: TuiTheme.Current.WindowBg);
        }
        rb.Write(absY, midStart, "│", fg: TuiColors.BrightBlack, bg: TuiTheme.Current.WindowBg);
        col = midStart + 2;

        // ── 中段：工具/任务 ──
        int midWidth = Width / 3 - 4;
        var toolDisplay = ToolText;
        if (!string.IsNullOrEmpty(toolDisplay) && midWidth > 0)
        {
            if (TuiHelper.DisplayWidth(toolDisplay) > midWidth)
                toolDisplay = TuiHelper.TruncateByWidth(toolDisplay, midWidth);
            rb.Write(absY, col, toolDisplay, fg: TuiColors.Grey, bg: TuiTheme.Current.WindowBg);
        }
        col = midStart + Width / 3;

        // ── 分隔 ──
        int rightStart = absX + Width * 2 / 3;
        rb.Write(absY, rightStart, "│", fg: TuiColors.BrightBlack, bg: TuiTheme.Current.WindowBg);
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
                var barFg = pct switch { < 30 => TuiColors.Green, < 70 => TuiColors.Yellow, _ => TuiColors.Red };
                rb.Write(absY, col,
                    $"«{new string('█', filled)}{new string('░', empty)}»",
                    fg: barFg, bg: TuiTheme.Current.WindowBg);
                col += barW + 4;

                var pctStr = $" {pct,3:F0}%";
                rb.Write(absY, col, pctStr, fg: barFg, bg: TuiTheme.Current.WindowBg);
            }
        }
        else if (!string.IsNullOrEmpty(ProgressLabel))
        {
            var label = ProgressLabel;
            int maxW = Width - (col - absX);
            if (TuiHelper.DisplayWidth(label) > maxW)
                label = TuiHelper.TruncateByWidth(label, maxW);
            rb.Write(absY, col, label, fg: TuiColors.BrightBlack, bg: TuiTheme.Current.WindowBg);
        }
        else
        {
            // 常驻上下文占用%（空闲/思考/工具态均显示，绿→黄→红）
            if (ContextPercent.HasValue)
            {
                var pct = ContextPercent.Value;
                var ctxFg = pct switch { < 30 => TuiColors.Green, < 70 => TuiColors.Yellow, _ => TuiColors.Red };
                var ctxStr = $"📊 {pct,3:F0}%";
                rb.Write(absY, col, ctxStr, fg: ctxFg, bg: TuiTheme.Current.WindowBg);
                col += TuiHelper.DisplayWidth(ctxStr) + 1;
            }

            // 空闲时显示模型名（在上下文%之后）
            if (Status == AgentStatus.Idle)
            {
                var modelDisplay = LeftText;
                if (!string.IsNullOrEmpty(modelDisplay) && TuiHelper.DisplayWidth(modelDisplay) > 25)
                    modelDisplay = TuiHelper.TruncateByWidth(modelDisplay, 25);
                rb.Write(absY, col, modelDisplay ?? "",
                    fg: TuiColors.BrightBlack, bg: TuiTheme.Current.WindowBg);
            }
        }

        sb.Append(rb.ToString());
    }
}
