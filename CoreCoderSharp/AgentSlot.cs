using System.Text;
using CoreCoderSharp.UI;
using CoreCoderSharp.UI.TuiScreens;

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
    public List<ChatMsg> ChatMessages { get; } = [];
    public string InputText { get; set; } = "";
    public int InputCursorRow, InputCursorCol;
    public string StatusLeft = "";
    public string StatusRight = "";
    public string TokenInfo = "";
    public string? GitBranch;
    public List<string> RecentFiles { get; } = [];
    public bool SidePanelVisible;

    /// <summary>是否已显示欢迎屏（每个槽位首次激活时显示一次）</summary>
    public bool HasWelcome { get; set; }

    /// <summary>
    /// 从 ChatScreen 快照当前 UI 状态到本槽位。
    /// </summary>
    public void SaveFrom(ChatScreen screen)
    {
        ChatMessages.Clear();
        ChatMessages.AddRange(screen.ChatMessages);
        InputText = screen.InputArea.Text;
        InputCursorRow = screen.InputArea.CursorRow;
        InputCursorCol = screen.InputArea.CursorCol;
        StatusLeft = screen.StatusLeft;
        StatusRight = screen.StatusRight;
        TokenInfo = screen.StatusRight;
        GitBranch = screen.GitBranch;
        RecentFiles.Clear();
        RecentFiles.AddRange(screen.RecentFiles);
        SidePanelVisible = screen.SidePanelVisible;
    }

    /// <summary>将本槽位状态恢复到 ChatScreen（切换回该槽位时调用）</summary>
    public void RestoreTo(ChatScreen screen)
    {
        // 清空聊天列表并重建
        screen.ClearChat();
        screen.ChatMessages.Clear();
        screen.ChatMessages.AddRange(ChatMessages);

        // 重建聊天列表项
        foreach (var msg in ChatMessages)
            screen.AddMessage(msg.Content, msg.Role);

        // 恢复输入状态
        screen.InputArea.Text = InputText;
        screen.InputArea.CursorRow = Math.Min(InputCursorRow, Math.Max(0, screen.InputArea.Lines.Count - 1));
        screen.InputArea.CursorCol = Math.Min(InputCursorCol,
            screen.InputArea.Lines.Count > 0 ? screen.InputArea.Lines[screen.InputArea.CursorRow].Length : 0);

        screen.StatusLeft = StatusLeft;
        screen.StatusRight = StatusRight;
        screen.GitBranch = GitBranch;
        screen.RecentFiles.Clear();
        screen.RecentFiles.AddRange(RecentFiles);
        screen.SidePanelVisible = SidePanelVisible;

        // 隐藏建议面板，滚动到底部
        screen.HideSuggestions();
        screen.SuggestActive = false;
        screen.ChatScrollBottom();

        screen.MarkDirty();
    }
}
