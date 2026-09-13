using System.Text;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Screens;

namespace WayCoder;

/// <summary>
/// Agent 工作区槽位 —— 每个槽位拥有独立的 Agent 实例与独立屏幕状态。
/// F1-F10 对应 10 个槽位，切换时通过 SaveFrom/RestoreTo 保存与恢复 UI。
/// </summary>
public class AgentSlot
{
    public const int Count = 10;

    /// <summary>槽位对应的 Agent（懒创建：首次激活时由 Program 创建）</summary>
    public Agent? Agent { get; set; }

    /// <summary>槽位专用的 LLM 客户端（null=使用全局 LLM）</summary>
    public LLM? LlmClient { get; set; }

    /// <summary>槽位级互斥锁：序列化后台 Agent 线程的"检查活跃+写输出"与 UI 线程的切换/快照，防止丢 token。</summary>
    public readonly object Sync = new();

    /// <summary>该槽位的 Agent 是否正在后台运行（volatile：后台线程写、UI 线程读）。</summary>
    public volatile bool IsBusy;

    /// <summary>该槽位 Agent 的取消令牌源（后台任务运行时非 null）。</summary>
    public CancellationTokenSource? Cts;

    /// <summary>上次使用的槽位模型 ID（用于检测模型变更）</summary>
    public string? LastLargeModel { get; set; }
    public string? LastSmallModel { get; set; }

    // ---- 独立屏幕状态 ----
    public List<ChatMsg> ChatMessages { get; } = [];
    public string InputText { get; set; } = "";
    public int InputCursorRow, InputCursorCol;
    public string StatusLeft = "";
    public string StatusRight = "";
    public string? GitBranch;
    public List<string> RecentFiles { get; } = [];
    public bool SidePanelVisible;

    // ── 思考折叠（与 ChatScreen 同构）。非活跃槽位的 token 不进控件树，只落到 ChatMsg：
    //    思考定稿后正文留在 ChatMsg.Reasoning（切回来点开可看），渲染层只留一行「💭 已思考 N 秒」。──
    private readonly ThinkStreamParser _thinkParser = new();
    private ChatMsg? _thinkMsg;
    private long _thinkStartTicks;

    /// <summary>该槽位的工作目录（独立于其他槽位；Agent cd 后持久化，下次任务从该目录开始）。null=进程启动目录。</summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>当前槽位的工作模式</summary>
    public WorkMode WorkMode { get; set; } = WorkMode.Build;

    /// <summary>是否已显示欢迎屏（每个槽位首次激活时显示一次）</summary>
    public bool HasWelcome { get; set; }

    /// <summary>待投递的跨槽位消息（从其他槽位发送来）</summary>
    public readonly List<(int FromSlot, string Message)> PendingMessages = [];

    /// <summary>
    /// 投递一条跨槽位消息。若目标槽位是当前活跃槽位，直接显示；否则排队。
    /// </summary>
    public void DeliverMessage(int fromSlot, string message, ChatScreen? activeScreen, int targetIdx)
    {
        if (activeScreen != null && activeScreen.ActiveSlotIndex == targetIdx)
        {
            // 目标槽位正在显示 → 直接投递
            activeScreen.AddSystemMsg($"📨 **F{fromSlot + 1} → 你**：{message}");
        }
        else
        {
            // 目标槽位不在前台 → 排队（带上限：从不激活的槽位会无限累积，超 100 丢最旧）
            while (PendingMessages.Count >= Global.MaxPendingSlotMessages)
                PendingMessages.RemoveAt(0);
            PendingMessages.Add((fromSlot, message));
        }
    }

    /// <summary>
    /// 将队列中的跨槽位消息刷新到屏幕（切换回本槽位时调用）。
    /// </summary>
    public void FlushPendingMessages(ChatScreen? screen, int slotIdx)
    {
        if (PendingMessages.Count == 0 || screen == null) return;
        foreach (var (fromSlot, msg) in PendingMessages)
        {
            screen.AddSystemMsg($"📨 **F{fromSlot + 1} → 你**：{msg}");
        }
        PendingMessages.Clear();
    }

    // ── 后台缓冲输出（槽位非活跃时，Agent 流式输出写入此处，切换回时 RestoreTo 展示）──
    // 注意：以下方法均不自行加锁，调用方必须持有 Sync 锁（见 Program.RunSlotAgentAsync 的 Route）。

    /// <summary>缓冲：开始一段 Agent 流式回复（占位消息）。</summary>
    public void BufferedStartStream()
    {
        FoldThinkBuffered(); // 上一轮若没收尾，先定稿，避免两条思考消息并存
        ChatMessages.Add(new ChatMsg { Role = "assistant", Content = "", Streaming = true });
        PruneBuffered();
    }

    /// <summary>缓冲：追加 token 到流式消息（无流式消息则自动新建）。
    /// 经解析器分流 —— 与活跃槽位同一套规则（否则切回来正文里躺着裸 «dim»/«/»）。</summary>
    public void BufferedAppendToken(string delta)
    {
        if (ChatMessages.Count == 0 || !ChatMessages[^1].Streaming)
            ChatMessages.Add(new ChatMsg { Role = "assistant", Content = "", Streaming = true });

        _thinkParser.Feed(delta,
            onBody: AppendToStreamBuffered,
            onThink: AppendToThinkBuffered,
            onThinkStart: StartThinkBuffered,
            onThinkEnd: () => FoldThinkBuffered());
    }

    /// <summary>缓冲：正文片段落到流式 assistant 消息。</summary>
    private void AppendToStreamBuffered(string piece)
    {
        if (ChatMessages.Count == 0 || !ChatMessages[^1].Streaming) return;
        ChatMessages[^1].Content = CapSingleMessage(ChatMessages[^1].Content, piece);
    }

    /// <summary>缓冲：思考片段只进 ChatMsg.Reasoning（渲染层切回来时才有一行标题）。</summary>
    private void AppendToThinkBuffered(string piece)
    {
        if (_thinkMsg == null) return;
        _thinkMsg.Reasoning = CapSingleMessage(_thinkMsg.Reasoning ?? "", piece);
    }

    /// <summary>缓冲：思考块开始 —— 思考消息插到流式 assistant 消息**之前**（与 ChatScreen 同一约定）。</summary>
    private void StartThinkBuffered()
    {
        // resetParser: false —— 本回调由解析器在深度置 1 之后发出，Reset 会把「正在思考」抹平
        FoldThinkBuffered(resetParser: false);
        var msg = new ChatMsg { Role = "think", Content = "💭 思考中 0s", Indent = 1 };
        ChatMessages.Insert(Math.Max(0, ChatMessages.Count - 1), msg);
        _thinkMsg = msg;
        _thinkStartTicks = Environment.TickCount64;
    }

    /// <summary>缓冲：思考定稿 —— 改写标题 + 记耗时；整块没有推理内容则直接撤掉这条消息。</summary>
    /// <param name="resetParser">是否复位解析器（见 <see cref="ChatScreen.FoldThink"/> 同名参数：
    /// 从「思考开始」回调里调时必须传 false）。</param>
    private void FoldThinkBuffered(bool resetParser = true)
    {
        if (resetParser) _thinkParser.Reset();
        var msg = _thinkMsg;
        _thinkMsg = null;
        if (msg == null) return;

        if (string.IsNullOrWhiteSpace(msg.Reasoning))
        {
            ChatMessages.Remove(msg);
            return;
        }

        int secs = Math.Max(1, (int)Math.Round((Environment.TickCount64 - _thinkStartTicks) / 1000.0));
        msg.Content = $"💭 已思考 {secs} 秒";
        msg.ThinkingSeconds = secs;
    }

    /// <summary>缓冲：结束 Agent 流式回复。</summary>
    public void BufferedFinishStream()
    {
        if (ChatMessages.Count == 0) return;
        ChatMessages[^1].Streaming = false;
        FoldThinkBuffered(); // 本轮结束：被中断/只思考没出正文时也要定稿
    }

    /// <summary>缓冲：追加文本到最后一条消息（工具流式输出）。</summary>
    public void BufferedAppendToLast(string delta)
    {
        if (ChatMessages.Count == 0) return;
        var last = ChatMessages[^1];
        if (last.Role == "think") return; // 思考行是折叠产物，不接受流式追加（与 ChatScreen.AppendToLast 同规则）
        last.Content = CapSingleMessage(last.Content, delta);
    }

    /// <summary>缓冲：追加一条普通消息（system/tool 等）。indent&gt;0 为嵌套子消息。</summary>
    public void BufferedAddMsg(string role, string content, int indent = 0)
    {
        FoldThinkBuffered(); // 工具/系统消息到来 = 思考就地定稿（对齐 ChatScreen.AddToolProgress）
        ChatMessages.Add(new ChatMsg { Role = role, Content = content, Indent = indent });
        PruneBuffered();
    }

    /// <summary>单条缓冲消息截断：超 <see cref="Global.MaxSingleMessageChars"/> 保留尾部窗口 + 滚动标记，
    /// 每次追加滚动更新（旧内容挤出，最新内容可见）。</summary>
    private static string CapSingleMessage(string cur, string delta)
    {
        int max = Global.MaxSingleMessageChars;
        if (max <= 0 || cur.Length + delta.Length <= max) return cur + delta;
        var combined = cur + delta;
        var tail = ContextManager.TruncateTailByRunes(combined, max);
        return $"… 已截断（显示最近内容，旧内容滚动省略）…\n{tail}";
    }

    /// <summary>非活跃槽位缓冲裁剪：超过 <see cref="Config.MaxChatMessages"/> 丢最旧消息，
    /// 防止后台长任务期间槽位缓冲无限累积（切回时 RestoreTo 再展示）。</summary>
    private void PruneBuffered()
    {
        int max = Config.Instance.MaxChatMessages;
        if (max <= 0) return;
        int excess = ChatMessages.Count - max;
        for (int i = 0; i < excess && ChatMessages.Count > 0; i++)
            ChatMessages.RemoveAt(0);
    }

    /// <summary>
    /// 从 ChatScreen 快照当前 UI 状态到本槽位。
    /// </summary>
    public void SaveFrom(ChatScreen screen)
    {
        // 切走前把正在进行的思考就地定稿：行内标题还停在「💭 思考中 0s」的话，
        // 切回来重建出的就是一条永远显示「思考中」的死行。
        screen.FoldThink();
        ChatMessages.Clear();
        ChatMessages.AddRange(screen.ChatMessages);
        InputText = screen.InputArea.Text;
        InputCursorRow = screen.InputArea.CursorRow;
        InputCursorCol = screen.InputArea.CursorCol;
        StatusLeft = screen.StatusLeft;
        StatusRight = screen.StatusRight;
        GitBranch = screen.GitBranch;
        RecentFiles.Clear();
        RecentFiles.AddRange(screen.RecentFiles);
        SidePanelVisible = screen.SidePanelVisible;
        WorkMode = WorkModeManager.CurrentMode;
    }

    /// <summary>将本槽位状态恢复到 ChatScreen（切换回该槽位时调用）</summary>
    public void RestoreTo(ChatScreen screen)
    {
        // 清空聊天列表并重建
        screen.ClearChat();
        screen.ChatMessages.Clear();
        screen.ChatMessages.AddRange(ChatMessages);

        // 重建聊天列表项；ShellBlock 持久化在 ChatMsg，重放时一并传递（bash 竖线 gutter 不丢）。
        // 思考行走 AddThinkMsg：它重建的是「已思考 N 秒」一行（正文留在 Reasoning 里点开可看），
        // 走 AddMessage 会退化成一条普通消息。
        foreach (var msg in ChatMessages)
        {
            if (msg.Role == "think") screen.AddThinkMsg(msg.Content, msg.Reasoning, msg.ThinkingSeconds);
            else screen.AddMessage(msg.Content, msg.Role, msg.Centered, msg.Indent, msg.ShellBlock);
        }

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

        // 投递跨槽位待处理消息
        FlushPendingMessages(screen, screen.ActiveSlotIndex);

        screen.MarkDirty();
    }
}
