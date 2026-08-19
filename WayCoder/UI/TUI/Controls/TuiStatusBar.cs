using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Screens;
using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;

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
        var (gs, ge) = t.GradTitleBar;
        int fg = AnsiColors.Black;
        int dimFg = AnsiColors.BrightBlack;
        int row = absY;

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
                SlotState.Working     => AnsiColors.Green,
                SlotState.WaitingPerm => AnsiColors.Yellow,
                SlotState.Error       => AnsiColors.Red,
                _ => AnsiColors.BrightBlack
            };

            string slotNum = (i + 1).ToString();
            if (i == ActiveSlotIndex)
            {
                // 活跃槽位：白底黑字（不跟随渐变）
                sb.Append(AnsiTty.CursorPos0(row, col));
                sb.Append(AnsiTty.FgBgCode(AnsiColors.Black, AnsiColors.BgWhite));
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

        // 2.5 动画图标/工作模式/经济模式已移入动态栏与模型信息行（输入区下方），此处不再重复。

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
            int rVw = AnsiHelper.DisplayWidth(rightStr);
            int rightCol = absX + Width - rVw - 1;
            if (rightCol > col)
                ControlRenderer.WriteGradientTextAt(sb, row, rightCol, rightStr,
                    dimFg, gs, ge, absX, Width);
        }
    }
}
