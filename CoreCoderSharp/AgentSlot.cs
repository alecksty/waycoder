using System.Text;
using CoreCoderSharp.UI;

namespace CoreCoderSharp;

/// <summary>
/// Agent 工作区槽位 —— 每个槽位拥有独立的 Agent 实例与独立屏幕状态。
/// F1-F10 对应 10 个槽位，切换时通过 SaveFrom/RestoreTo 保存与恢复 UI。
/// </summary>
public class AgentSlot
{
    public const int Count = 10;

    /// <summary>槽位对应的 Agent（懒创建：首次激活时由 Program 创建）</summary>
    public Agent? Agent { get; set; }

    // ---- 独立屏幕状态 ----
    public List<ScreenManager.ChatMsg> ChatMessages { get; } = [];
    public List<StringBuilder> InputLines { get; } = [new()];
    public int InputCy, InputCx, InputScroll;
    public string StatusLeft = "";
    public string StatusRight = "";
    public string TokenInfo = "";
    public string? GitBranch;
    public List<string> RecentFiles { get; } = [];
    public ScreenManager.PanelTab ActivePanel;

    /// <summary>是否已显示欢迎屏（每个槽位首次激活时显示一次）</summary>
    public bool HasWelcome { get; set; }

    /// <summary>
    /// 从 ScreenManager 快照当前 UI 状态到本槽位。
    /// 注意：ChatMessages/InputLines 按值拷贝（不共享引用）。
    /// </summary>
    public void SaveFrom(ScreenManager sm)
    {
        ChatMessages.Clear();
        ChatMessages.AddRange(sm.ChatMessages);
        InputLines.Clear();
        InputLines.AddRange(sm.InputLines.Select(l => new StringBuilder(l.ToString())));
        InputCy = sm.InputCy;
        InputCx = sm.InputCx;
        InputScroll = sm.InputScroll;
        StatusLeft = sm.StatusLeft;
        StatusRight = sm.StatusRight;
        TokenInfo = sm.TokenInfo;
        GitBranch = sm.GitBranch;
        RecentFiles.Clear();
        RecentFiles.AddRange(sm.RecentFiles);
        ActivePanel = sm.ActivePanel;
    }

    /// <summary>将本槽位状态恢复到 ScreenManager（切换回该槽位时调用）</summary>
    public void RestoreTo(ScreenManager sm)
    {
        sm.ChatMessages.Clear();
        sm.ChatMessages.AddRange(ChatMessages);
        sm.InputLines.Clear();
        sm.InputLines.AddRange(InputLines.Select(l => new StringBuilder(l.ToString())));
        sm.InputCy = InputCy;
        sm.InputCx = InputCx;
        sm.InputScroll = InputScroll;
        sm.StatusLeft = StatusLeft;
        sm.StatusRight = StatusRight;
        sm.TokenInfo = TokenInfo;
        sm.GitBranch = GitBranch;
        sm.RecentFiles.Clear();
        sm.RecentFiles.AddRange(RecentFiles);
        sm.ActivePanel = ActivePanel;
        sm.SuggestActive = false;
        sm.ChatScrollBottom();
    }
}
