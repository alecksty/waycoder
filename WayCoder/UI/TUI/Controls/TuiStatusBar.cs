using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Screens;
using WayCoder.UI.Shared;

namespace WayCoder.UI.Tui.Controls;

/// <summary>
/// 底部状态栏控件 —— 动态状态信息条。
/// 渲染：左侧槽位指示条 + 中间提示文本 + 右侧 Agent 状态/Token。
/// 颜色由主题 StatusBarFg/StatusBarBg 控制。
/// </summary>
public class TuiStatusBar : TuiControl
{
    public override bool CanFocus => false;

    // ── 数据 ──

    /// <summary>F1-F10 槽位状态</summary>
    public SlotState[] SlotStates { get; set; } = new SlotState[10];

    /// <summary>当前活跃槽位索引 (0-9)</summary>
    public int ActiveSlotIndex { get; set; }

    /// <summary>中间提示文本（如快捷键说明）</summary>
    public string HintText { get; set; } = "";

    /// <summary>右侧状态文本（Token 用量等）</summary>
    public string RightText { get; set; } = "";

    /// <summary>Agent 是否忙碌（显示旋转指示）</summary>
    public bool AgentBusy { get; set; }

    /// <summary>旋转动画帧（Agent 忙碌时）</summary>
    private int _spinFrame;

    /// <summary>全局心跳帧（始终自增，表示 UI 渲染循环存活）</summary>
    private static int _heartbeat;

    /// <summary>当前工作模式（Build/Plan/Review/Auto）</summary>
    public WorkMode CurrentWorkMode { get; set; } = WorkMode.Build;

    public TuiStatusBar()
    {
        Height = 1;
    }

    /// <summary>
    /// 渲染状态栏（金色渐变背景）
    /// </summary>
    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        var t = TuiTheme.Current;
        int row = absY;

        // 主题纯色模式：Bg>0（StatusBarBg 由主题/预设设置）→ 整行填充 + 普通文本
        if (Bg > 0)
        {
            RenderSolid(sb, row, absX);
            return;
        }

        var (gs, ge) = t.GradTitleBar;
        int fg = TuiColors.Black;
        int dimFg = TuiColors.BrightBlack;

        // 1. 整行渐变背景填充
        ControlRenderer.DrawGradientBarFill(sb, row, absX, Width, gs, ge);

        // 2. 左侧：槽位指示条 F1-F10
        ControlRenderer.WriteGradientTextAt(sb, row, absX, " ", fg, gs, ge, absX, Width);
        int col = absX + 1;

        for (int i = 0; i < 10; i++)
        {
            var state = SlotStates[i];
            int slotFg = state switch
            {
                SlotState.Working     => TuiColors.Green,
                SlotState.WaitingPerm => TuiColors.Yellow,
                SlotState.Error       => TuiColors.Red,
                _ => TuiColors.BrightBlack
            };

            string slotNum = (i + 1).ToString();
            if (i == ActiveSlotIndex)
            {
                // 活跃槽位：白底黑字（不跟随渐变）
                sb.Append(AnsiTty.CursorPos0(row, col));
                sb.Append(AnsiTty.FgBgCode(TuiColors.Black, TuiColors.BgWhite));
                sb.Append(slotNum);
                sb.Append(AnsiTty.SgrReset);
            }
            else
            {
                ControlRenderer.WriteGradientTextAt(sb, row, col, slotNum,
                    slotFg, gs, ge, absX, Width);
            }
            col += 1;

            // 数字间空格
            if (i < 9)
            {
                ControlRenderer.WriteGradientTextAt(sb, row, col, " ",
                    dimFg, gs, ge, absX, Width);
                col += 1;
            }
        }

        // 2.5 心跳动画（始终跳动，证明 UI 渲染循环存活）
        {
            string[] heartbeatFrames = ["⣾", "⣽", "⣻", "⢿", "⡿", "⣟", "⣯", "⣷"];
            _heartbeat = (_heartbeat + 1) % heartbeatFrames.Length;
            col += 1;
            ControlRenderer.WriteGradientTextAt(sb, row, col, heartbeatFrames[_heartbeat],
                AgentBusy ? TuiColors.Green : dimFg, gs, ge, absX, Width);
            col += 2;
        }
        // 心跳是持续动画：自我置脏让下一帧不早退（否则空闲时心跳冻结）
        if (TuiManager.Instance != null) TuiManager.Instance.IsDirty = true;

        // 2.6 工作模式指示（Shift+Tab 切换）
        {
            var modeStr = WorkModeManager.Emojis.GetValueOrDefault(CurrentWorkMode, "?");
            ControlRenderer.WriteGradientTextAt(sb, row, col, $" {modeStr}",
                TuiColors.Cyan, gs, ge, absX, Width);
            col += TuiHelper.DisplayWidth(modeStr) + 1;
        }

        // 2.7 省 Token 模式标志（三态图标：💵不省钱 / 💰省钱 / 🧮自动）
        {
            var (economyIcon, economyColor) = Config.Instance.EconomyMode switch
            {
                EconomyMode.On   => ("💰", TuiColors.Yellow),
                EconomyMode.Auto => ("🧮", TuiColors.Cyan),
                _                => ("💵", dimFg),
            };
            ControlRenderer.WriteGradientTextAt(sb, row, col, $" {economyIcon}",
                economyColor, gs, ge, absX, Width);
            col += TuiHelper.DisplayWidth(economyIcon) + 1;
        }

        // 3. 中间：提示文本
        if (!string.IsNullOrEmpty(HintText))
        {
            col += 2;
            ControlRenderer.WriteGradientTextAt(sb, row, col, HintText,
                dimFg, gs, ge, absX, Width);
        }

        // 4. 右侧：Agent busy + Token
        var rightParts = new List<string>();
        if (AgentBusy)
        {
            string[] spinners = ["◐", "◓", "◑", "◒"];
            _spinFrame = (_spinFrame + 1) % 4;
            rightParts.Add($"{spinners[_spinFrame]} 工作中");
        }
        if (!string.IsNullOrEmpty(RightText))
        {
            rightParts.Add(RightText);
        }
        if (rightParts.Count > 0)
        {
            var rightStr = string.Join(" · ", rightParts);
            int rVw = TuiHelper.DisplayWidth(rightStr);
            int rightCol = absX + Width - rVw - 1;
            if (rightCol > col)
                ControlRenderer.WriteGradientTextAt(sb, row, rightCol, rightStr,
                    dimFg, gs, ge, absX, Width);
        }
    }

    /// <summary>主题纯色模式：用 StatusBarBg/Fg 填充整行并写普通文本（替代渐变，响应主题切换）</summary>
    private void RenderSolid(StringBuilder sb, int row, int absX)
    {
        int fg = Fg > 0 ? Fg : TuiColors.White;
        // 次要文字用主色（避免 BrightBlack 灰字在亮色 StatusBarBg 上低反差）
        int dimFg = Fg > 0 ? Fg : TuiColors.White;
        var rb = new RenderBuffer();
        rb.Write(row, absX, new string(' ', Width), fg: fg, bg: Bg);

        // 左：槽位指示条 F1-F10
        int col = absX + 1;
        for (int i = 0; i < 10; i++)
        {
            var state = SlotStates[i];
            int slotFg = state switch
            {
                SlotState.Working => TuiColors.Green,
                SlotState.WaitingPerm => TuiColors.Yellow,
                SlotState.Error => TuiColors.Red,
                _ => dimFg
            };
            string slotNum = (i + 1).ToString();
            // 活跃槽位：白底黑字
            int sFg = i == ActiveSlotIndex ? TuiColors.Black : slotFg;
            int sBg = i == ActiveSlotIndex ? TuiColors.BgWhite : Bg;
            rb.Write(row, col, slotNum, fg: sFg, bg: sBg);
            col += 2;
        }

        // 模式指示
        var modeStr = WorkModeManager.Emojis.GetValueOrDefault(CurrentWorkMode, "?");
        rb.Write(row, col, " " + modeStr, fg: TuiColors.Cyan, bg: Bg);
        col += TuiHelper.DisplayWidth(modeStr) + 1;

        // 省 Token 模式标志
        var (econIcon, econColor) = Config.Instance.EconomyMode switch
        {
            EconomyMode.On => ("💰", TuiColors.Yellow),
            EconomyMode.Auto => ("🧮", TuiColors.Cyan),
            _ => ("💵", dimFg),
        };
        rb.Write(row, col, " " + econIcon, fg: econColor, bg: Bg);
        col += 3;

        // 心跳动画
        string[] hb = ["⣾", "⣽", "⣻", "⢿", "⡿", "⣟", "⣯", "⣷"];
        _heartbeat = (_heartbeat + 1) % hb.Length;
        rb.Write(row, col, hb[_heartbeat], fg: AgentBusy ? TuiColors.Green : dimFg, bg: Bg);
        col += 2;
        if (TuiManager.Instance != null) TuiManager.Instance.IsDirty = true;

        // 中间提示
        if (!string.IsNullOrEmpty(HintText))
            rb.Write(row, absX + Width / 3, HintText, fg: dimFg, bg: Bg);

        // 右侧：Agent busy + Token
        var rightParts = new List<string>();
        if (AgentBusy)
        {
            string[] spinners = ["◐", "◓", "◑", "◒"];
            _spinFrame = (_spinFrame + 1) % 4;
            rightParts.Add($"{spinners[_spinFrame]} 工作中");
        }
        if (!string.IsNullOrEmpty(RightText)) rightParts.Add(RightText);
        if (rightParts.Count > 0)
        {
            var rightStr = string.Join(" · ", rightParts);
            int rVw = TuiHelper.DisplayWidth(rightStr);
            rb.Write(row, absX + Width - rVw - 1, rightStr, fg: dimFg, bg: Bg);
        }

        sb.Append(rb.ToString());
    }
}
