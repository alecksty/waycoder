using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Controls;

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

    private static readonly string[] Frames = AgentStatusResolver.SpinnerFrames; // 跨端统一 Braille 帧集

    /// <summary>动画帧间隔（毫秒）。每帧由 ChatScreen 按此节流标脏，250ms 一帧 ≈ 4 FPS ——
    /// 够看出在转，又不至于逐帧整条重绘造成卡顿。</summary>
    public const int FrameMs = 250; // 250ms/帧 ≈ 4 FPS：流畅又不卡

    /// <summary>按 Agent 状态取 spinner 前景色（DirectWrite 直写与 OnRender 共用）。</summary>
    private static int SpinnerFg(AgentStatus st) => st switch
    {
        // 动画图标统一用黄色（用户要求橙/黄），完成绿、错误红
        AgentStatus.Thinking => AnsiColors.Yellow,
        AgentStatus.ToolRunning => AnsiColors.Yellow,
        AgentStatus.Compressing => AnsiColors.Yellow,
        AgentStatus.WaitingPermission => AnsiColors.Yellow,
        AgentStatus.WaitingUser => AnsiColors.Yellow,
        AgentStatus.WaitingSubagent => AnsiColors.Yellow,
        AgentStatus.Planning => AnsiColors.Yellow,
        AgentStatus.Complete => AnsiColors.Green,
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

        // ① spinner：动画本身每帧都在变，照旧每帧直写（它不参与段级比对）
        sb.Append(AnsiTty.CursorPos0(_spinnerY, _spinnerX));
        int fg = SpinnerFg(Status); // 直写方法独立计算 spinner 色（不依赖 OnRender 局部变量）
        sb.Append(AnsiTty.FgBgCode(fg, AnsiColors.BgBlack));
        sb.Append(CurrentFrame);
        sb.Append(AnsiTty.SgrReset);

        // ② 三段：只重写「内容与上次整行渲染写入的不同」的那一段。
        //    左段(状态)/中段(工具)平时不动，思考与流式期间持续跳变的只有右段的
        //    token/花费/上下文——此前任何一个数字变一下就要整行重画。
        if (_barWidth > 0)
        {
            int midStart = _barAbsX + _barWidth / 3;
            int rightStart = _barAbsX + _barWidth * 2 / 3;
            int rightEnd = _barAbsX + _barWidth - 1; // 与 BuildRightItems 的右段终点一致

            var left = BuildLeftSegment();
            if (!string.Equals(left, _lastLeft, StringComparison.Ordinal))
            {
                WriteSegment(sb, _spinnerY, _barAbsX + 3, midStart - (_barAbsX + 3), left, LeftTextFg());
                _lastLeft = left;
            }

            var middle = BuildMiddleSegment();
            if (!string.Equals(middle, _lastMiddle, StringComparison.Ordinal))
            {
                WriteSegment(sb, _spinnerY, midStart + 2, rightStart - (midStart + 2), middle, AnsiColors.Grey);
                _lastMiddle = middle;
            }

            var rightItems = BuildRightItems(_barAbsX, rightStart + 2);
            var rightSig = RightSignature(rightItems);
            if (!string.Equals(rightSig, _lastRight, StringComparison.Ordinal))
            {
                int drawnTo = rightStart + 2;
                foreach (var (rCol, rText, rFg) in rightItems)
                {
                    if (rCol > drawnTo) // 段内留白（如进度条后的 4 列间隔）也要补上
                    {
                        sb.Append(AnsiTty.CursorPos0(_spinnerY, drawnTo));
                        sb.Append(AnsiTty.FgBgCode(AnsiColors.BrightBlack, AnsiColors.BgBlack));
                        sb.Append(new string(' ', rCol - drawnTo));
                    }
                    sb.Append(AnsiTty.CursorPos0(_spinnerY, rCol));
                    sb.Append(AnsiTty.FgBgCode(rFg, AnsiColors.BgBlack));
                    sb.Append(rText);
                    drawnTo = rCol + AnsiHelper.DisplayWidth(rText);
                }
                // 收尾空白：新内容比旧的短时，把剩余列刷回底色，防残留旧数字
                if (drawnTo < rightEnd)
                {
                    sb.Append(AnsiTty.CursorPos0(_spinnerY, drawnTo));
                    sb.Append(AnsiTty.FgBgCode(AnsiColors.BrightBlack, AnsiColors.BgBlack));
                    sb.Append(new string(' ', rightEnd - drawnTo));
                }
                sb.Append(AnsiTty.SgrReset);
                _lastRight = rightSig;
            }
        }

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

    // 以下内容属性一律走 SetDirty（值真变了才标脏）：
    // 动态栏的 spinner 动画由 RenderDirect 直写、不需要脏标记，所以「何时重绘整条」应当完全由
    // 「内容是否变化」决定。此前它们是普通自动属性 → 只能靠 ChatScreen 里「每 250ms 无条件标脏」
    // 的定时器硬刷，那会把整个屏幕（含聊天区所在的重绘路径）按动画节拍反复拖进渲染，
    // 表现为内容没变却一直在闪。改为按值标脏后：内容不变 → 不标脏 → 不重绘；
    // spinner 照常靠直写转动 —— 动态栏与聊天区彻底分开刷新。

    private AgentStatus _status = AgentStatus.Idle;
    /// <summary>当前代理状态</summary>
    public AgentStatus Status { get => _status; set => SetContent(ref _status, value); }

    private string _leftText = "";
    /// <summary>左段：状态文本（如 "思考中... deepseek-v4-pro"）</summary>
    public string LeftText { get => _leftText; set => SetContent(ref _leftText, value); }

    private string _toolText = "";
    /// <summary>中段：工具/任务文本（如 "⚙ bash: dotnet build"）</summary>
    public string ToolText { get => _toolText; set => SetContent(ref _toolText, value); }

    private double? _progressPercent;
    /// <summary>压缩进度百分比（null=不显示）</summary>
    public double? ProgressPercent { get => _progressPercent; set => SetContent(ref _progressPercent, value); }

    private string _progressLabel = "";
    /// <summary>压缩进度标签</summary>
    public string ProgressLabel { get => _progressLabel; set => SetContent(ref _progressLabel, value); }

    private double? _contextPercent;
    /// <summary>上下文占用百分比（null=不显示，常驻右段，绿→黄→红）</summary>
    public double? ContextPercent { get => _contextPercent; set => SetContent(ref _contextPercent, value); }

    private double? _cpuPercent;
    /// <summary>CPU 占用百分比（null=不显示，常驻右段 ContextPercent 之后，绿→黄→红）</summary>
    public double? CpuPercent { get => _cpuPercent; set => SetContent(ref _cpuPercent, value); }

    private string? _tokenDisplay;
    /// <summary>token 消耗显示串（如 "大:12K 小:3K"，null/空=不显示，常驻右段）</summary>
    public string? TokenDisplay { get => _tokenDisplay; set => SetContent(ref _tokenDisplay, value); }

    private string? _costDisplay;
    /// <summary>花费显示（如 "¥0.42"，null/空=不显示，常驻右段）</summary>
    public string? CostDisplay { get => _costDisplay; set => SetContent(ref _costDisplay, value); }

    /// <summary>
    /// 内容属性赋值：值真变了才动作，且**优先走段级直写**而不是整条重绘。
    ///
    /// 动态栏分左/中/右三段，思考与流式期间持续跳变的只有右段的 token/花费/上下文；
    /// 若一变值就 <see cref="MarkDirty"/>，整条（约 1/3 屏宽）会被重画，段级增量就没意义了。
    /// 因此这里只在**直写不可用**时（被对话框遮挡 / 本栏不在活跃屏幕）才退回整行标脏，
    /// 保证内容任何情况下都不会丢：遮挡解除时屏幕重绘 → OnRender 重新写入并刷新段缓存。
    /// </summary>
    private void SetContent<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        if (!CanDirectWrite()) MarkDirty();
    }

    /// <summary>本栏当前能否直写屏幕（与 <see cref="RenderDirect"/> 的门控完全一致）。</summary>
    private bool CanDirectWrite()
        => _spinnerX > 0
           && TuiManager.Instance?.ActiveScreen == _owner
           && _owner?.FocusedWindow == null;

    /// <summary>是否处于任务活跃状态（显示 spinner）</summary>
    public bool IsActive => Status != AgentStatus.Idle;


    private static readonly List<TuiDynamicBar> DirectWriters = [];
    private int _spinnerX, _spinnerY; // 渲染时记录 spinner 位置（DirectWrite 直写用）
    private int _barAbsX, _barWidth;  // 渲染时记录动态栏几何（段级直写按绝对列定位）

    // ── 段级直写缓存：记录「上次整行渲染时各段实际写到屏幕的纯文本」──
    // RenderDirect 每帧只重写**内容变了的那一段**，不再整行重画：
    // 左段(状态)/中段(工具)平时不动，只有右段的 token/花费/上下文在思考与流式期间持续跳变。
    // null = 尚未记录（或该段当前为空），一律视为需要重写。
    private string? _lastLeft, _lastMiddle, _lastRight;

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

        // 记录几何（段级直写要按绝对列定位）。本方法照常整行写文字并**记录已写内容**，
        // RenderDirect 只补「与已写内容不同」的段——这样：
        // ① 对话框在场时直写被门控跳过，本方法仍把整行画好（动态栏不会变空白）；
        // ② 全屏重绘/遮挡解除后重绘都会走到本方法 → 重新写入并刷新记录，不会漏补。
        _barAbsX = absX;
        _barWidth = Width;

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

        var leftStr = BuildLeftSegment();
        if (leftStr.Length > 0)
        {
            rb.Write(absY, col, leftStr, fg: textColor, bg: AnsiColors.BgBlack);
            col += AnsiHelper.DisplayWidth(leftStr);
        }
        _lastLeft = leftStr; // 段级直写的比对基准（与写入内容同源，不会漂移）

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
        var toolDisplay = BuildMiddleSegment();
        if (toolDisplay.Length > 0)
            rb.Write(absY, col, toolDisplay, fg: AnsiColors.Grey, bg: AnsiColors.BgBlack);
        _lastMiddle = toolDisplay; // 段级直写的比对基准
        col = midStart + Width / 3;

        // ── 右段：进度条 / 进度标签 / 📊⚡🔤¥ 指标流 ──
        // 内容由 BuildRightItems 统一产出（连绝对列一起返回——各分支列步进不同，只给文本还原不了布局），
        // 整行渲染与段级直写共用同一份，避免两处布局/截断规则漂移。
        // 模型/模式信息统一由输入区下方模型栏显示，动态栏不放（重复）。
        int rightStart = absX + Width * 2 / 3;
        var rightItems = BuildRightItems(absX, rightStart + 2);
        foreach (var (rCol, rText, rFg) in rightItems)
            rb.Write(absY, rCol, rText, fg: rFg, bg: AnsiColors.BgBlack);
        _lastRight = RightSignature(rightItems); // 段级直写比对基准

        sb.Append(rb.ToString());
    }

    // ═══════════════════════════════════════════════════════════
    // 段内容构建（整行渲染与段级直写共用）
    // ═══════════════════════════════════════════════════════════

    /// <summary>左段文本（状态）——未截断；写入时按左段宽度钳制。</summary>
    private string BuildLeftRaw()
    {
        if (!string.IsNullOrEmpty(LeftText)) return LeftText;
        return Status switch
        {
            AgentStatus.Idle => "空闲",
            AgentStatus.Thinking => "思考中...",
            AgentStatus.ToolRunning => "工具执行",
            AgentStatus.Compressing => "压缩中",
            AgentStatus.Planning => "计划模式 🧠",
            AgentStatus.WaitingPermission => "等待确认",
            AgentStatus.WaitingUser => "等待用户回复",
            AgentStatus.WaitingSubagent => "等待子代理",
            AgentStatus.Complete => "任务完成 ✓",
            AgentStatus.Error => "错误",
            _ => "空闲",
        };
    }

    /// <summary>左段最终写入文本（按左段可用宽度截断）。</summary>
    private string BuildLeftSegment()
    {
        int leftWidth = Math.Min(AnsiHelper.DisplayWidth(BuildLeftRaw()), Width / 3 - 3);
        return leftWidth > 0 ? AnsiHelper.TruncateByWidth(BuildLeftRaw(), leftWidth) : "";
    }

    /// <summary>中段最终写入文本（工具/任务，按中段宽度截断）。</summary>
    private string BuildMiddleSegment()
    {
        var t = ToolText;
        int midWidth = Width / 3 - 4;
        if (string.IsNullOrEmpty(t) || midWidth <= 0) return "";
        return AnsiHelper.DisplayWidth(t) > midWidth ? AnsiHelper.TruncateByWidth(t, midWidth) : t;
    }

    /// <summary>左段颜色（Error 红，其余橙）。</summary>
    private int LeftTextFg() => Status == AgentStatus.Error ? AnsiColors.Red : AnsiTty.RgbCode(255, 180, 0);

    /// <summary>段级直写：定位到 [col, col+spanW) 写入文本，并按 spanW 补齐空格
    /// （文本变短时把旧字符刷掉，否则会残留上一帧的尾巴）。</summary>
    private static void WriteSegment(StringBuilder sb, int row, int col, int spanW, string text, int fg)
    {
        if (spanW <= 0) return;
        sb.Append(AnsiTty.CursorPos0(row, col));
        sb.Append(AnsiTty.FgBgCode(fg, AnsiColors.BgBlack));
        var shown = text;
        if (AnsiHelper.DisplayWidth(shown) > spanW)
            shown = AnsiHelper.TruncateByWidth(shown, spanW);
        sb.Append(shown);
        int pad = spanW - AnsiHelper.DisplayWidth(shown);
        if (pad > 0) sb.Append(new string(' ', pad));
        sb.Append(AnsiTty.SgrReset);
    }

    /// <summary>右段比对签名（纯文本拼接）：判断右段内容是否变化。</summary>
    private static string RightSignature(List<(int Col, string Text, int Fg)> items)
        => string.Concat(items.Select(i => i.Text));

    /// <summary>
    /// 右段内容：按当前属性产出「绝对列 + 文本 + 颜色」序列（进度条 / 进度标签 / 📊⚡🔤¥ 指标流）。
    /// 带绝对列是因为各分支列步进不同（进度条后留 4 列），只返回文本无法还原布局。
    /// <paramref name="startCol"/> = 右段文字起始绝对列。
    /// </summary>
    private List<(int Col, string Text, int Fg)> BuildRightItems(int absX, int startCol)
    {
        var items = new List<(int, string, int)>();
        int col = startCol;

        // ── 进度条（压缩中）──
        if (ProgressPercent.HasValue)
        {
            var pct = ProgressPercent.Value;
            int barW = Math.Min(14, Width - (col - absX) - 4);
            if (barW > 0)
            {
                int filled = Math.Clamp((int)Math.Round(barW * pct / 100.0), 0, barW);
                int empty = barW - filled;
                var barFg = pct switch { < 30 => AnsiColors.Green, < 70 => AnsiColors.Yellow, _ => AnsiColors.Red };
                items.Add((col, $"«{new string('█', filled)}{new string('░', empty)}»", barFg));
                col += barW + 4;
                items.Add((col, $" {pct,3:F0}%", barFg));
            }
            return items;
        }

        // ── 进度标签 ──
        if (!string.IsNullOrEmpty(ProgressLabel))
        {
            var label = ProgressLabel;
            int maxW = Width - (col - absX);
            if (AnsiHelper.DisplayWidth(label) > maxW)
                label = AnsiHelper.TruncateByWidth(label, maxW);
            items.Add((col, label, AnsiColors.BrightBlack));
            return items;
        }

        // ── 常驻指标流（空闲/思考/工具态均显示，绿→黄→红）──
        // 右段空间有限（约 1/3 宽）：信息多时靠后的项（🔤/¥）丢最靠前的（📦 已省略，
        // 上下文 token 量用 📊 占比表达，不重复）。宽度保护防溢出到分隔线。
        int rightEnd = absX + Width - 1;
        bool HasRoom(int extra) => col + extra <= rightEnd;

        if (ContextPercent.HasValue && HasRoom(7))
        {
            var pct = ContextPercent.Value;
            var fg = pct switch { < 30 => AnsiColors.Green, < 70 => AnsiColors.Yellow, _ => AnsiColors.Red };
            var ctxStr = $"📊{pct,3:F0}%"; // 紧凑：去空格
            items.Add((col, ctxStr, fg));
            col += AnsiHelper.DisplayWidth(ctxStr) + 1;
        }
        // CPU 占用%（⚡ 前缀区分；阈值 <50 绿 <70 黄 ≥70 红）
        if (CpuPercent.HasValue && HasRoom(6))
        {
            var cp = CpuPercent.Value;
            var fg = cp switch { < 50 => AnsiColors.Green, < 70 => AnsiColors.Yellow, _ => AnsiColors.Red };
            var s = $"⚡{cp,3:F0}%"; // 紧凑：去空格
            items.Add((col, s, fg));
            col += AnsiHelper.DisplayWidth(s) + 1;
        }
        // token 消耗（🔤）：剩余宽度不足时截断
        if (!string.IsNullOrEmpty(TokenDisplay) && col < rightEnd)
        {
            var td = TokenDisplay;
            int avail = rightEnd - col;
            if (AnsiHelper.DisplayWidth(td) > avail)
                td = AnsiHelper.TruncateByWidth(td, avail);
            items.Add((col, td, AnsiColors.Grey));
            col += AnsiHelper.DisplayWidth(td) + 1;
        }
        // 花费（¥）
        if (!string.IsNullOrEmpty(CostDisplay) && HasRoom(7))
            items.Add((col, CostDisplay, AnsiColors.Yellow));

        return items;
    }
}
