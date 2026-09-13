namespace WayCoder.UI.Tui.Controls;

/// <summary>提示条目</summary>
public class PromptItem
{
    public EPromptKind Kind { get; set; }
    public string Label { get; set; } = "";
    public string? Detail { get; set; }
    public string? Value { get; set; }

    /// <summary>
    /// 选中此项时回给调用方的结果码。仅 <see cref="EPromptKind.Choice"/> 使用。
    /// 约定：0 = 允许 / 1 = 允许且不再问 / 2 = 拒绝 —— 与各确认对话框的 <c>?? 2</c> 默认拒绝一致。
    /// </summary>
    public int ResultCode { get; set; }

    /// <summary>危险操作（如 <c>rm -rf</c>）—— 不再提供「全部允许」，且不做 A 单键快捷。</summary>
    public bool IsDangerous { get; set; }

    /// <summary>多选模式下的勾选态（仅 <see cref="TuiPromptBar.MultiSelect"/> 为真时渲染/切换）。</summary>
    public bool Checked { get; set; }

    /// <summary>「其他」项：回车后转入自定义输入（不是直接作答），见 <see cref="SurveyOption.IsOther"/>。</summary>
    public bool IsOther { get; set; }

    /// <summary>「跳过此题」项：选中即不作答地前进（问卷里让用户保留「不回答」的权利）。</summary>
    public bool IsSkip { get; set; }

    /// <summary>
    /// 获取提示条目的图标。
    /// </summary>
    /// <returns>图标文本。</returns>
    public string Icon => Kind switch
    {
        EPromptKind.Command => "⌘",
        EPromptKind.File => "📄",
        EPromptKind.Shell => "⚡",
        EPromptKind.Slash => "/",
        EPromptKind.History => "↺",
        EPromptKind.Recent => "⏱️",
        EPromptKind.Choice => "", // 序号由 Label 自带，多一个图标反而挤掉说明文字
        _ => "·",
    };
}