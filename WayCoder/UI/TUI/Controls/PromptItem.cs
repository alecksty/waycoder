namespace WayCoder.UI.Tui.Controls;

/// <summary>提示条目</summary>
public class PromptItem
{
    public EPromptKind Kind { get; set; }
    public string Label { get; set; } = "";
    public string? Detail { get; set; }
    public string? Value { get; set; }

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
        EPromptKind.Recent => "⏱",
        _ => "·",
    };
}