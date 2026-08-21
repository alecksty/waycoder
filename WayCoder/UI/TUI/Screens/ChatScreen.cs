using System.Collections.Concurrent;
using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.Tools;
using WayCoder.UI.Tui.ToolRenderers;

using WayCoder.UI.Tui.Controls;

using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Tui.Screens;


/// <summary>
/// 聊天 REPL 屏幕 —— 主交互界面。
///
/// 布局结构：
///   RootView (VBox)
///   ├─ StatusBar     TuiLabel       顶行状态栏
///   ├─ ChatList      TuiListView    聊天历史（每项为 TuiMarkdown）
///   ├─ SuggestPanel  TuiVBox        建议下拉（浮层，默认隐藏）
///   └─ InputArea     TuiTextArea    多行输入区
///
/// 可选右侧面板（SidePanel）和浮层窗口（对话框/Toast）。
/// </summary>
public partial class ChatScreen : TuiScreen
{
    // ── 子视图 ──

    /// <summary>标题栏（顶行）</summary>
    public TuiTitleBar TitleBar { get; protected set; } = null!;

    /// <summary>底部状态栏</summary>
    public TuiStatusBar StatusBar { get; protected set; } = null!;

    /// <summary>聊天列表（TuiListView → TuiMarkdown 项）</summary>
    public TuiListView ChatList { get; protected set; } = null!;

    /// <summary>多行输入区</summary>
    public TuiTextArea InputArea { get; protected set; } = null!;

    /// <summary>提示栏（输入框上方）</summary>
    public TuiPromptBar PromptBar { get; protected set; } = null!;

    /// <summary>前缀提示钩子注册表：前缀符号 → 提示项生成器（触发提示框）。</summary>
    private readonly Dictionary<char, Func<string, List<PromptItem>>> _prefixHintHooks = new();

    /// <summary>内置前缀符号（/ @ ! #）。</summary>
    private static readonly char[] BuiltinPrefixes = ['/', '@', '!', '#'];

    /// <summary>动态栏（聊天列表下方、输入区上方，始终可见）</summary>
    public TuiDynamicBar DynamicBar { get; protected set; } = null!;

    /// <summary>输入区上分隔线</summary>
    public TuiSeparator InputTopBorder { get; protected set; } = null!;

    /// <summary>输入区下分隔线</summary>
    public TuiSeparator InputBotBorder { get; protected set; } = null!;

    /// <summary>模型/模式信息行（输入面板下方、状态栏上方）。TuiSmartLabel 渲染 «tag» 分段着色。</summary>
    public TuiSmartLabel? ModelInfoRow { get; protected set; }
    /// <summary>模型信息行下方空行（与底部状态栏分隔），可见性跟随 ModelInfoRow。</summary>
    private TuiSpace? _modelInfoSpacer;

    /// <summary>建议下拉面板</summary>
    public TuiVBox SuggestPanel { get; protected set; } = null!;

    /// <summary>建议面板上一帧的可见矩形（用于移动/缩放/隐藏后补绘被遮挡的聊天内容）。</summary>
    private int _suggestPrevX = -1, _suggestPrevY = -1, _suggestPrevW, _suggestPrevH;
    private bool _suggestPrevVisible;

    /// <summary>右侧信息面板</summary>
    public TuiSidePanel SidePanel { get; protected set; } = null!;

    /// <summary>
    /// 浮层窗口可占用的顶部边界 = 标题栏高度，避免对话框顶边覆盖标题栏。
    /// </summary>
    public override int OverlayTop => TitleBar?.Height ?? 1;

    /// <summary>
    /// 浮层窗口可占用的底部边界 = 标题栏下方 + 聊天列表高度，
    /// 即动态栏/输入区/状态栏之上的内容区，避免对话框底边覆盖状态栏与输入框。
    /// </summary>
    public override int OverlayBottom
    {
        get
        {
            int titleH = TitleBar?.Height ?? 1;
            int chatH = ChatList?.Height ?? Math.Max(1, TH - titleH - 7);
            return Math.Max(1, titleH + chatH);
        }
    }

    // ── 状态 ──

    public string StatusText { get; set; } = "";

    /// <summary>建议列表项</summary>
    public List<string> Suggestions { get; set; } = [];

    public int SuggestIndex { get; set; }

    /// <summary>侧栏是否可见</summary>
    public bool SidePanelVisible { get; set; }

    /// <summary>侧栏分区内容</summary>
    public List<PanelSection> SidePanelSections { get; set; } = [];

    /// <summary>进度条（null=隐藏）</summary>
    public double? ProgressPercent { get; set; }

    /// <summary>提交输入回调</summary>
    public Action<string>? OnSubmit { get; set; }

    // ── REPL 状态 ──

    /// <summary>槽位状态（F1-F10，索引 0-9）</summary>
    public SlotState[] SlotStates { get; } = new SlotState[10];

    /// <summary>当前活跃槽位索引</summary>
    public int ActiveSlotIndex { get; set; }

    /// <summary>聊天消息列表（直接访问，用于会话保存/恢复/槽位切换）</summary>
    public List<ChatMsg> ChatMessages { get; } = [];

    /// <summary>聊天消息锁（保护后台线程回调中的 ChatMessages/ChatList 写入）</summary>
    private readonly object _chatLock = new();

    /// <summary>后台线程 → UI 线程消息队列：子线程永不直接碰控件树，只投递操作，UI 线程 PumpUIQueue 消费。</summary>
    private readonly ConcurrentQueue<Action> _uiQueue = new();

    /// <summary>UI 线程 ID（构造时捕获；PostToUI 据此判定直接执行还是投递）</summary>
    private readonly int _uiThreadId = Environment.CurrentManagedThreadId;

    /// <summary>
    /// 投递 UI 操作：UI 线程调用直接执行（无延迟），后台线程调用入队（UI 线程 PumpUIQueue 消费）。
    /// 这样所有 ChatScreen 的 UI 方法都能安全地从任意线程调用，控件树只被 UI 线程触碰。
    /// </summary>
    public void PostToUI(Action action)
    {
        if (action == null) return;
        if (Environment.CurrentManagedThreadId == _uiThreadId)
            action();
        else
            _uiQueue.Enqueue(action);
    }

    /// <summary>消费并执行 UI 操作队列（仅 UI 线程调用：REPL 主循环 / RunAgentWithRenderLoop / RenderWait）。</summary>
    public void PumpUIQueue()
    {
        while (_uiQueue.TryDequeue(out var action))
        {
            try { action(); }
            catch { /* 单条操作失败不拖垮整帧 */ }
        }
    }

    /// <summary>状态栏左侧（模型名、git 分支等）</summary>
    public string StatusLeft { get; set; } = "";

    /// <summary>当前会话 ID（侧边栏会话区标记「当前」；由 REPL 主循环按活跃槽位同步）</summary>
    public string CurrentSessionId { get; set; } = "";

    /// <summary>状态栏右侧（Token 信息）</summary>
    public string StatusRight { get; set; } = "";

    /// <summary>Git 分支名</summary>
    public string? GitBranch { get; set; }

    /// <summary>建议面板是否活跃</summary>
    public bool SuggestActive { get; set; }

    /// <summary>Agent 正在执行（显示旋转指示）</summary>
    public bool AgentBusy { get; set; }

    /// <summary>聊天显示风格：detailed=全显示 auto=智能折叠 concise=极简一行</summary>
    public string ChatDisplayStyle { get; set; } = "auto";

    /// <summary>当前工具调用已流式输出的行数（用于 auto 模式折叠）</summary>
    private int _toolOutputLineCount;

    /// <summary>当前正在执行的工具名（null=无工具在执行），用于动态栏显示</summary>
    private string? _currentToolName;
    /// <summary>当前工具参数摘要</summary>
    private string? _currentToolBrief;

    /// <summary>
    /// 初始化聊天屏幕
    /// </summary>
    public ChatScreen()
    {
        Name = "chat";
    }

    // ── 生命周期 ──

    public override void Activate()
    {
        base.Activate();
        BuildLayout();

        // 订阅上下文压缩进度事件（用于显示进度条）
        ContextManager.CompressProgress += OnCompressProgress;
    }

    public override void Deactivate()
    {
        ContextManager.CompressProgress -= OnCompressProgress;
        base.Deactivate();
    }

    /// <summary>
    /// 只刷新动态栏并触发下一帧，不弄脏整棵根。
    /// 工具状态/压缩进度/权限等待都只影响动态栏，而 <see cref="MarkDirty"/> 会把根标脏，
    /// 标题栏作为叶子被 parentDirty 拉着整行重绘（金色渐变重画一次就是一次闪）。
    /// </summary>
    private void MarkDynamicBarDirty()
    {
        DynamicBar?.MarkDirty();
        if (Manager != null) Manager.IsDirty = true;
    }

    private void OnCompressProgress(int layer, string message, double percent)
    {
        // 压缩在 Agent 后台线程执行，事件可能从后台触发 → 投递到 UI 线程（UI 线程触发则直接执行）
        PostToUI(() =>
        {
            if (ContextManager.IsCompressing && DynamicBar != null)
            {
                DynamicBar.Status = AgentStatus.Compressing;
                DynamicBar.ProgressPercent = percent;
                DynamicBar.ProgressLabel = $"[L{layer}] {message}";
                MarkDynamicBarDirty();
            }
            else if (DynamicBar != null)
            {
                DynamicBar.ProgressPercent = null;
                DynamicBar.ProgressLabel = "";
            }
        });
    }

    /// <summary>
    /// 同步动态栏状态（Render 每帧调用）
    /// </summary>
    private void SyncDynamicBar()
    {
        if (DynamicBar == null) return;
        DynamicBar.Width = TW;
        DynamicBar.ContextPercent = _contextPercent; // 常驻上下文占用%

        // 动画节流：活跃态每 FrameMs（500ms）标一次脏让 spinner 转起来。
        // 动画常驻标脏（spinner 每 FrameMs 转，不依赖 IsActive —— 空闲也显示旋转动画）
        {
            long now = Environment.TickCount64;
            if (now - _lastAnimDirtyTicks >= TuiDynamicBar.FrameMs)
            {
                _lastAnimDirtyTicks = now;
                DynamicBar.MarkDirty();
            }
        }

        // 压缩中（从 CompressProgress 事件已设置，保持不变）
        if (DynamicBar.Status == AgentStatus.Compressing && ContextManager.IsCompressing)
            return;
        if (DynamicBar.Status == AgentStatus.Compressing && !ContextManager.IsCompressing)
        {
            DynamicBar.ProgressPercent = null; // 压缩完成，清理
            DynamicBar.ProgressLabel = "";     // 同时清标签，避免残留 "[L3] 压缩完成" 覆盖常驻上下文%
        }

        // 等待权限
        if (_pendingPermissionTool != null)
        {
            DynamicBar.Status = AgentStatus.WaitingPerm;
            DynamicBar.LeftText = $"等待确认: {_pendingPermissionTool}";
            DynamicBar.ToolText = "";
            return;
        }

        // 工具执行中
        if (_currentToolName != null)
        {
            DynamicBar.Status = AgentStatus.ToolRunning;
            DynamicBar.LeftText = _currentToolName;
            DynamicBar.ToolText = _currentToolBrief ?? "";
            return;
        }

        // Agent 思考中
        if (AgentBusy)
        {
            DynamicBar.Status = AgentStatus.Thinking;
            DynamicBar.LeftText = StatusLeft;
            DynamicBar.ToolText = "";
            return;
        }

        // 非 Build 模式时显示当前工作模式（Build=默认，不特殊显示）
        var mode = WorkModeManager.CurrentMode;
        if (mode != WorkMode.Build)
        {
            var (emoji, label, tooltip) = mode switch
            {
                WorkMode.Plan => ("🧠", "计划模式", "只读分析 · 阻止写操作"),
                WorkMode.Review => ("🔍", "审查模式", "只读审查 · 阻止写操作"),
                WorkMode.Auto => ("🤖", "自动模式", "全自动执行 · 不确认"),
                _ => ("", "未知", ""),
            };
            DynamicBar.Status = AgentStatus.Planning;
            DynamicBar.LeftText = $"{emoji} {label}";
            DynamicBar.ToolText = $"{tooltip} · Shift+Tab 切换";
            return;
        }

        // 空闲
        DynamicBar.Status = AgentStatus.Idle;
        DynamicBar.LeftText = StatusLeft;
        DynamicBar.ToolText = "";
    }

    /// <summary>
    /// 同步模型/模式信息行（输入区下方、状态栏上方）：权限/工作模式/经济模式/大模型/小模型，`·` 分隔。
    /// 每帧读取实时模式/模型，变了才标脏重绘（模式切换后下一帧自动刷新）。
    /// 用 «tag»…«/» 标记分段着色（TuiLabel.ParseMarkup）：标签暗、值亮/彩，当前模型加粗 —— 比此前整行灰暗更醒目。
    /// </summary>
    private void SyncModelInfo()
    {
        string large = AgentSlotConfig.ResolveLargeModel(AgentSlotConfig.Get(ActiveSlotIndex), ActiveSlotIndex);
        string small = AgentSlotConfig.ResolveSmallModel(AgentSlotConfig.Get(ActiveSlotIndex), ActiveSlotIndex);
        string modeStr = WorkModeManager.Format(WorkModeManager.CurrentMode);
        string economyStr = Config.Instance.EconomyMode switch
        {
            EconomyMode.On => "省钱",
            EconomyMode.Auto => "自动",
            EconomyMode.Extreme => "极致",
            _ => "关闭",
        };
        // 权限模式（确认级别）：极简TINY/问答ACK/自动AUTO/智能SMART/畅通YOLO
        string permStr = PermissionManager.FormatMode();

        string permColor = PermissionManager.CurrentMode switch
        {
            PermissionManager.Mode.Yolo => "red",
            PermissionManager.Mode.SmartAuto => "cyan",
            PermissionManager.Mode.Auto => "green",
            PermissionManager.Mode.TINY => "grey",
            _ => "yellow",
        };
        string modeColor = WorkModeManager.CurrentMode switch
        {
            WorkMode.Plan => "cyan",
            WorkMode.Review => "magenta",
            WorkMode.Auto => "yellow",
            _ => "green",
        };
        string economyColor = Config.Instance.EconomyMode switch
        {
            EconomyMode.On => "green",
            EconomyMode.Auto => "cyan",
            EconomyMode.Extreme => "red",
            _ => "grey",
        };

        string rowStr = $"«dim»权限:«/»«{permColor}»{permStr}«/»"
            + $" · «dim»工作模式:«/»«{modeColor}»{modeStr}«/»"
            + $" · «dim»经济模式:«/»«{economyColor}»{economyStr}«/»"
            + $" · «dim»大模型:«/»«bold»{large}«/»"
            + $" · «dim»小模型:«/»«bold»{small}«/»";

        SetModelInfoRow(true, rowStr);
    }

    /// <summary>设置模型信息行可见性与内容；可见性变了重排，文本变了也要标脏该行——
    /// 否则增量渲染不重绘它，切换模型后状态栏上方这行会一直显示旧模型。</summary>
    private void SetModelInfoRow(bool visible, string text)
    {
        var row = ModelInfoRow;
        if (row == null) return;
        bool visChanged = row.Visible != visible;
        row.Visible = visible;
        // 下方空行可见性跟随（模型栏显示时才占位，与状态栏分隔）
        if (_modelInfoSpacer != null) _modelInfoSpacer.Visible = visible;
        bool textChanged = row.Text != text;
        if (textChanged) row.Text = text;
        if (visChanged || textChanged) { RootView?.Layout(); row.MarkDirty(); }
    }

    /// <summary>等待权限的工具名（非 null = 正在等待）</summary>
    private string? _pendingPermissionTool;

    /// <summary>上下文占用百分比（null=未知，用于动态栏常驻显示）</summary>
    private double? _contextPercent;

    /// <summary>动态栏动画上次标脏时间戳（TickCount64，节流用，避免逐帧整条重绘）</summary>
    private long _lastAnimDirtyTicks;

    /// <summary>标记工具开始执行（只刷动态栏，不弄脏根，防标题栏闪）</summary>
    public void OnToolStarted(string toolName, string brief)
    {
        _currentToolName = toolName;
        _currentToolBrief = brief;
        MarkDynamicBarDirty();
    }

    /// <summary>标记工具执行结束（只刷动态栏，不弄脏根，防标题栏闪）</summary>
    public void OnToolFinished()
    {
        _currentToolName = null;
        _currentToolBrief = null;
        MarkDynamicBarDirty();
    }

    /// <summary>标记权限等待开始（只刷动态栏，不弄脏根，防标题栏闪）。可后台线程调用（投递到 UI）。</summary>
    public void OnPermissionWaiting(string toolName)
    {
        PostToUI(() =>
        {
            _pendingPermissionTool = toolName;
            MarkDynamicBarDirty();
        });
    }

    /// <summary>标记权限等待结束（只刷动态栏，不弄脏根，防标题栏闪）。可后台线程调用（投递到 UI）。</summary>
    public void OnPermissionResolved()
    {
        PostToUI(() =>
        {
            _pendingPermissionTool = null;
            MarkDynamicBarDirty();
        });
    }

    /// <summary>终端尺寸变化——重建完整布局，保留输入状态和全部聊天消息</summary>
    public override void OnResize(int newW, int newH)
    {
        var inputText = InputArea?.Text ?? "";
        int cursorRow = InputArea?.CursorRow ?? 0;
        int cursorCol = InputArea?.CursorCol ?? 0;

        // 保存旧 ChatList 的消息数据（仅非保留路径：BuildLayout 会新建空 ChatList）
        var savedMessages = BuildLayoutPreservesChatItems ? null : CaptureChatItems();

        TW = newW;
        TH = newH;

        // 重建整个控件树（标记版 MarkupChatScreen 覆写：首次加载 chat.tui，后续只重排）
        BuildLayout();

        // 恢复聊天消息：非保留路径走 AddMessage 重灌（自动处理续接/纯文本）；保留路径只按新宽重建项内容
        if (savedMessages != null)
        {
            foreach (var (role, content, centered, indent) in savedMessages)
                AddMessage(content, role, centered, indent);
        }
        else if (BuildLayoutPreservesChatItems)
        {
            RebuildChatItems();
        }

        // 恢复输入状态
        if (!string.IsNullOrEmpty(inputText))
        {
            InputArea!.Text = inputText;
            InputArea.CursorRow = Math.Min(cursorRow, InputArea.Lines.Count - 1);
            InputArea.CursorCol = Math.Min(cursorCol, InputArea.Lines[InputArea.CursorRow].Length);
        }

        // 应用动态尺寸（分隔线/输入区/聊天区/侧栏宽度）
        ComputeLayout(out var panelW, out var inputH, out var promptH, out _, out var chatH);
        ApplyDynamicSizes(panelW, inputH, promptH, chatH);

        // 通知所有浮层窗口
        foreach (var win in Windows)
            win.OnResize(newW, newH);
    }

    /// <summary>捕获当前 ChatList 的消息数据（Role/Content/Centered/Indent）。</summary>
    private List<(string Role, string Content, bool Centered, int Indent)>? CaptureChatItems()
    {
        if (ChatList == null) return null;
        var saved = new List<(string Role, string Content, bool Centered, int Indent)>();
        for (int i = 0; i < ChatList.ItemCount; i++)
        {
            var item = ChatList.GetItem(i) as TuiListItem;
            if (item != null)
                saved.Add((item.Role, item.MarkdownContent, item.ContentAlign == EHAlign.Center, item.Indent));
        }
        return saved;
    }

    /// <summary>计算动态布局尺寸（Render / OnResize 共用）。</summary>
    protected void ComputeLayout(out int panelW, out int inputH, out int promptH, out int progressH, out int chatH)
    {
        panelW = SidePanelVisible ? Math.Min(30, TW / 3) : 0;
        // 输入区高度 = 行数（默认 1 行，Ctrl+Enter 换行增高，删除键减行，最大 5 行超出滚动）
        inputH = Math.Clamp(InputArea.Lines.Count, 1, 5);
        promptH = PromptBar.Visible ? PromptBar.Height : 0;
        progressH = (ProgressPercent.HasValue && ContextManager.IsCompressing) ? 1 : 0;
        chatH = Math.Max(1, TH - 1 - promptH - 1 - 1 - 1 - inputH - 1 - progressH - 1
            - (ModelInfoRow?.Visible == true ? 2 : 0)); // TH - title - prompt - spacer(1) - dynamicBar(1) - topBorder - input - botBorder - progress - modelInfoRow - modelInfoSpacer - status
    }

    /// <summary>应用动态尺寸到各子视图（Render / OnResize 共用）。</summary>
    protected void ApplyDynamicSizes(int panelW, int inputH, int promptH, int chatH)
    {
        InputArea.Width = TW;
        InputArea.Height = inputH;
        // 提示栏只做聊天列表一样宽：侧栏可见时收窄到 TW-panelW，不覆盖侧栏 →
        // 收起提示栏只需重绘聊天区，不用刷新聊天区以外的区域（避免侧栏残留/额外重绘）。
        PromptBar.Width = panelW > 0 ? TW - panelW : TW;
        InputTopBorder.Width = TW;
        InputBotBorder.Width = TW;
        ChatList.Width = panelW > 0 ? TW - panelW : TW;
        ChatList.Height = chatH;
        SidePanel.Visible = SidePanelVisible;
        SidePanel.Width = panelW;
        SidePanel.Height = chatH;
        if (SidePanelVisible)
        {
            SidePanel.Sections = SidePanelSections;
            if (FocusedWindow == null)
                SidePanel.MarkDirty(); // 无弹窗才标脏重绘（弹窗在场侧栏被遮罩，且避免与弹窗渲染竞争）
        }
    }

    /// <summary>
    /// BuildLayout 是否保留 ChatList 实例（标记版重排不重建 → resize 跳过消息重灌，改用 RebuildChatItems 按新宽重建）。
    /// </summary>
    protected virtual bool BuildLayoutPreservesChatItems => false;

    /// <summary>
    /// BuildLayout 保留 ChatList 实例时，resize 后按新宽度重建聊天项内容（基类空实现；非保留路径靠 AddMessage 重灌重建）。
    /// </summary>
    protected virtual void RebuildChatItems() { }

    /// <summary>
    /// 构建聊天屏幕布局
    /// </summary>
    protected virtual void BuildLayout()
    {
        RootView.Clear();
        RootView = new TuiVBox { Width = TW, Height = TH };

        // 输入历史持久化
        var histPath = Global.GlobalReadConfigPath("input_history.txt");
        TuiInputHistory.SetPersistPath(histPath);

        // ── 标题栏（顶行）──
        TitleBar = new TuiTitleBar
        {
            Width = TW,
            Height = 1,
            Bg = TuiTheme.Current.StatusBarBg,
            Fg = TuiTheme.Current.StatusBarFg
        };
        RootView.Add(TitleBar);

        // ── 中间区域：ChatList + SidePanel（HBox 水平排列）──
        var chatH = Math.Max(1, TH - 1 - 0 - 1 - 1 - 3 - 1 - 1);
        // TH - title(1) - prompt(0) - dynamicBar(1) - topBorder(1) - input(3) - botBorder(1) - status(1)
        var middleHBox = new TuiHBox { Width = TW, Height = chatH };

        ChatList = new TuiListView
        {
            Width = TW, // 初始全宽，侧栏打开时 Render 会缩小
            Height = chatH,
            IsAutoScrollToEnd = true,
            ItemSpacing = 1
        };
        middleHBox.Add(ChatList);

        SidePanel = new TuiSidePanel
        {
            Width = Math.Min(30, TW / 3),
            Height = chatH,
            Visible = false,
            Bg = 0,
            BorderColor = TuiTheme.Current.SeparatorFg,
        };
        middleHBox.Add(SidePanel);

        //  添加横向面板到根布局
        RootView.Add(middleHBox);

        // ── 建议面板（浮层，不参与流式布局，避免把输入区挤出屏幕）──
        SuggestPanel = new TuiVBox
        {
            Width = Math.Min(TW, 60),
            Height = 0,
            Visible = false,
            Bg = 47,
            Floating = true
        };
        RootView.Add(SuggestPanel);

        // ── 提示栏（默认隐藏，输入时动态显示）──
        PromptBar = new TuiPromptBar
        {
            Width = TW,
            Height = 0,
            Visible = false,
            Bg = 0,
        };
        RootView.Add(PromptBar);

        // ── 动态栏上方空一行（与聊天列表分隔，更好看）──
        RootView.Add(new TuiLabel { Width = TW, Height = 1 });

        // ── 动态栏（始终可见，对标 Claude Code SpinnerWithVerb）──
        DynamicBar = new TuiDynamicBar
        {
            Width = TW,
            Height = 1,
            Bg = 0,
        };
        DynamicBar.RegisterDirectWrite(this); // spinner 直写终端（不依赖 dirty 整条重绘，按所属屏幕门控）
        RootView.Add(DynamicBar);

        // ── 输入区上分隔线 ──
        InputTopBorder = new TuiSeparator
        {
            Width = TW,
            Height = 1,
            LineChar = "─",
            LineColor = TuiTheme.Current.SeparatorFg
        };

        RootView.Add(InputTopBorder);

        // ── 输入区 ──
        InputArea = new TuiTextArea
        {
            Width = TW,
            Height = 3,
            Bg = 0,
            CursorLineBg = 0,
            CursorLineFg = TuiTheme.Current.TextAreaFg, // 无光标行高亮时，光标行文字用正文色（否则黑字黑底）
            Focused = true,
            Placeholder = "输入消息… (Enter 发送, Ctrl+Enter 换行)",
            ShowLineNumbers = false,
            OnSubmit = text =>
            {
                if (!string.IsNullOrWhiteSpace(text))
                    OnSubmit?.Invoke(text);
            }
        };
        RootView.Add(InputArea);

        // ── 输入区下分隔线 ──
        InputBotBorder = new TuiSeparator
        {
            Width = TW, Height = 1,
            LineChar = "─", LineColor = TuiTheme.Current.SeparatorFg
        };
        RootView.Add(InputBotBorder);

        // ── 模型/模式信息行（输入区下方、状态栏上方）──
        // TuiSmartLabel：SyncModelInfo 用 «tag» 分段着色（权限/模式彩字、模型名加粗），
        // 白字打底，不再整行灰暗看不清。
        ModelInfoRow = new TuiSmartLabel
        {
            Width = TW,
            Height = 1,
            Visible = false,
            Fg = AnsiColors.White,
            TextAlign = EHAlign.Center,
        };
        RootView.Add(ModelInfoRow);

        // 模型信息行下方空行：与底部状态栏分隔（只在模型行可见时占位）
        _modelInfoSpacer = new TuiSpace { Height = 1, Visible = false };
        RootView.Add(_modelInfoSpacer);

        // ── 底部状态栏 ──
        StatusBar = new TuiStatusBar
        {
            Width = TW, Height = 1,
            Bg = TuiTheme.Current.StatusBarBg, Fg = TuiTheme.Current.StatusBarFg,
            HintText = "Enter 发送 · Shift+Tab 切模式 · ↑↓ 历史 · Tab 补全 · F1-F10 槽位 · Ctrl+H 帮助"
        };
        RootView.Add(StatusBar);

        RootView.Layout();
    }

    // ── 消息管理 ──

    /// <summary>
    /// 添加一条消息到聊天列表。system/tool 消息使用纯文本模式避免 Markdown 行合并，连续同角色自动续接。
    /// indent&gt;0 表示嵌套子消息（如工具输出嵌套在所属 assistant 消息下）：续接无角色头 + 左缩进。
    /// </summary>
    /// <param name="centered">null=续接同角色消息时继承前一条对齐（否则左对齐）；
    /// 显式 true/false 则强制该对齐 —— 表格类内容必须显式传 false，
    /// 否则会被前一条居中的 system 消息带偏，每行按各自宽度居中而参差不齐。</param>
    public void AddMessage(string content, string role = "assistant", bool? centered = null, int indent = 0)
    {
        bool continuation = false;
        bool plainText = role is "system" or "tool" or "banner";
        bool align = centered ?? false;
        if (plainText && role != "banner")
        {
            var last = ChatList.GetItem(ChatList.ItemCount - 1) as TuiListItem;
            if (last != null && last.Role == role)
            {
                continuation = true;
                // 仅在调用方未指定时继承前一条对齐
                if (centered == null) align = last.ContentAlign == EHAlign.Center;
            }
        }

        // 嵌套子消息强制续接（不渲染角色头）
        if (indent > 0)
            continuation = true;

        var item = new TuiListItem(role, content, ChatList.Width - 2,
            role == "banner" ? true : continuation, plainText,
            align ? EHAlign.Center : EHAlign.Left)
        {
            Indent = indent
        };
        if (!continuation)
            item.SetTime(DateTime.Now);

        // 错误输出红色显示
        if (plainText && IsErrorOutput(content))
            item.Body.IsError = true;

        ChatList.AddItem(item); // AddItem 内部 MarkDirtyTree：聊天区整棵标脏 + 触发下一帧，无需再弄脏根

        // 显示层裁剪：超过上限自动丢弃最旧消息（Agent 会话仍在、会话文件持久化——仅显示层裁剪保流畅）
        PruneChatHistory();
    }

    /// <summary>
    /// 聊天显示裁剪：超过 <see cref="Config.MaxChatMessages"/> 自动丢弃最旧消息。
    /// 只裁显示层（ChatMessages/ChatList），Agent 的 Messages 与会话文件不受影响——
    /// 恢复会话时按 Agent 消息重建显示。避免万级消息下列表布局/渲染越来越慢。
    /// </summary>
    private void PruneChatHistory()
    {
        int max = Config.Instance.MaxChatMessages;
        if (max <= 0) return;
        int excess = ChatList.ItemCount - max;
        if (excess <= 0) return;
        for (int i = 0; i < excess; i++)
        {
            if (ChatList.ItemCount > 0) ChatList.RemoveItem(0);
            if (ChatMessages.Count > 0) ChatMessages.RemoveAt(0);
        }
        // 裁剪后滚动偏移可能越界（最旧项被删），钳制到有效范围
        if (ChatList.ScrollOffset > 0)
            ChatList.ClampScroll();
    }

    /// <summary>检测工具输出内容是否包含错误标记</summary>
    private static bool IsErrorOutput(string text)
        => text.Contains("[退出码：") || text.Contains("[stderr]") ||
           text.Contains("错误：") || text.Contains("Error") ||
           text.Contains("❌") || text.Contains("⛔");

    /// <summary>追加文本到最后一条消息（流式输出）。线程安全：可从后台线程调用。</summary>
    public void AppendToLast(string delta)
    {
        var last = ChatList.GetItem(ChatList.ItemCount - 1) as TuiListItem;
        if (last == null) return;

        // 检测错误输出，自动切换为红色
        if (last.IsPlainText && !last.Body.IsError && IsErrorOutput(delta))
            last.Body.IsError = true;

        // 仅对工具输出（system/tool 消息）应用显示风格控制
        if (last.Role is "system" or "tool")
        {
            switch (ChatDisplayStyle)
            {
                case "concise":
                    // 极简模式：不显示工具流式输出，仅保留 ⚙ 一行
                    return;
                case "auto":
                    // 自动模式：最多保留 20 行，超出折叠
                    _toolOutputLineCount++;
                    if (_toolOutputLineCount == 21)
                    {
                        last.AppendContent($"\n  ... (后续输出已折叠) ...\n");
                        ChatList.ReLayout();
                        if (ChatList.IsAutoScrollToEnd)
                            ChatList.ScrollToBottom();
                        // 折叠提示也需要刷新
                        MarkDirty();
                    }
                    if (_toolOutputLineCount > 20)
                        return;
                    break;
                // detailed 模式：不限制，全量显示
            }
        }

        last.AppendContent(delta);
        ChatList.ReLayout();
        if (ChatList.IsAutoScrollToEnd)
            ChatList.ScrollToBottom();

        // 流式输出实时刷新：必须置脏才能让 30ms 渲染循环的下一帧不跳过
        // TuiView 子容器（ChatList）总是被遍历，ChatList.OnRender 渲染所有可见子项 → 无需单独标记
        if (Manager != null) Manager.IsDirty = true;
    }

    /// <summary>清空聊天</summary>
    public void ClearChat()
    {
        ChatList.ClearItems();
    }

    /// <summary>设置输入文本</summary>
    public void SetInput(string text)
    {
        InputArea.Text = text;
        InputArea.CursorRow = InputArea.Lines.Count - 1;
        InputArea.CursorCol = InputArea.Lines[^1].Length;
    }

    /// <summary>获取输入文本</summary>
    public string GetInput() => InputArea.Text;

    /// <summary>清空输入</summary>
    public void ClearInput()
    {
        InputArea.Text = "";
        InputArea.CursorRow = 0;
        InputArea.CursorCol = 0;
        InputArea.ScrollRow = 0;
    }

    // ── 便捷消息方法 ──

    /// <summary>添加用户消息</summary>
    public void AddUserMsg(string content)
    {
        var msg = new ChatMsg { Role = "user", Content = content };
        ChatMessages.Add(msg);
        AddMessage(content, "user");
    }

    /// <summary>添加系统消息。线程安全：可从后台线程调用。</summary>
    public void AddSystemMsg(string content)
    {
        lock (_chatLock)
        {
            var msg = new ChatMsg { Role = "system", Content = content };
            ChatMessages.Add(msg);
            AddMessage(content, "system");
        }
    }

    /// <summary>开始 Agent 流式回复（占位消息）。线程安全：可从后台线程调用。</summary>
    public void StartAgentMsg()
    {
        lock (_chatLock)
        {
            var msg = new ChatMsg { Role = "agent", Content = "", Streaming = true };
            ChatMessages.Add(msg);
            // 在 ChatList 中添加空白占位项
            var item = new TuiListItem("agent", "", ChatList.Width - 2);
            item.SetTime(DateTime.Now);
            ChatList.AddItem(item);
        }
        MarkDirty();
    }

    /// <summary>追加 token 到流式消息。线程安全：可从后台线程调用。</summary>
    public void AppendToken(string delta)
    {
        lock (_chatLock)
        {
            if (ChatMessages.Count == 0) return;
            var last = ChatMessages[^1];
            if (!last.Streaming) return;
            last.Content += delta;
            AppendToLast(delta);
        }
    }

    /// <summary>确保有活跃的流式 Agent 消息（如没有则创建一个）。线程安全。</summary>
    public void EnsureAgentStreaming()
    {
        lock (_chatLock)
        {
            if (ChatMessages.Count == 0 || !ChatMessages[^1].Streaming)
                StartAgentMsg();
        }
    }

    /// <summary>完成 Agent 流式回复。线程安全：可从后台线程调用。</summary>
    public void FinishAgentMsg()
    {
        lock (_chatLock)
        {
            if (ChatMessages.Count == 0) return;
            var last = ChatMessages[^1];
            last.Streaming = false;
        }
        MarkDirty();
    }

    /// <summary>添加工具调用消息（嵌套子消息）</summary>
    public void AddToolMsg(string toolName, string brief)
    {
        var content = $"  🔧 {toolName}({brief})";
        var msg = new ChatMsg { Role = "tool", Content = content, Indent = 1 };
        ChatMessages.Add(msg);
        AddMessage(content, "tool", indent: 1);
    }

    /// <summary>更新 Token 显示</summary>
    public void UpdateTokenDisplay(int used, int limit)
    {
        StatusRight = $"📊 {used}/{limit} tokens";
        MarkDirty();
    }

    // ── 聊天滚动 ──

    public void ChatScrollUp(int lines = 3) => ChatList.ScrollUp(lines);
    public void ChatScrollDown(int lines = 3) => ChatList.ScrollDown(lines);
    public void ChatScrollTop() => ChatList.ScrollToTop();
    public void ChatScrollBottom() => ChatList.ScrollToBottom();

    // ── 鼠标点击 ──

    /// <summary>处理鼠标点击：定位输入光标</summary>
    public void HandleMouseClick(int mx, int my)
    {
        var inputTop = TH - InputArea.Height;
        if (my >= inputTop)
        {
            int lineIdx = my - inputTop;
            int colIdx = mx;
            if (lineIdx >= 0 && lineIdx < InputArea.Lines.Count)
            {
                var line = InputArea.Lines[lineIdx];
                int charIdx = 0, vw = 0;
                foreach (var rune in line.EnumerateRunes())
                {
                    int rw = AnsiHelper.RuneWidth(rune);
                    if (vw + rw > colIdx) break;
                    vw += rw;
                    charIdx += rune.Utf16SequenceLength;
                }

                InputArea.CursorRow = lineIdx;
                InputArea.CursorCol = charIdx;
                MarkDirty();
            }
        }
    }
    // ── 渲染 ──

    public override void Render(StringBuilder sb)
    {
        // ── 同步标题栏数据 ──
        // 左上角永远是商标名，不跟 StatusLeft（那是模型名/loop 计数，动态栏用）。
        // 用户反馈「商标变模型名」：StatusLeft 被塞进 Title 槽位，模型一切就顶掉商标。
        TitleBar.Width = TW;
        TitleBar.Bg = TuiTheme.Current.StatusBarBg;
        TitleBar.Fg = TuiTheme.Current.StatusBarFg;
        TitleBar.Title = Global.AppFullName;
        TitleBar.GitBranch = GitBranch;
        TitleBar.Version = Global.Version;
        TitleBar.CenterText = $"💬 智能体 {ActiveSlotIndex + 1}";

        // ── 同步底部状态栏数据 ──
        StatusBar.Width = TW;
        StatusBar.Bg = TuiTheme.Current.StatusBarBg;
        StatusBar.Fg = TuiTheme.Current.StatusBarFg;
        StatusBar.ActiveSlotIndex = ActiveSlotIndex;
        StatusBar.AgentBusy = AgentBusy;
        StatusBar.RightText = StatusRight;
        Array.Copy(SlotStates, StatusBar.SlotStates, 10);

        // ── 同步动态栏 ──
        SyncDynamicBar();

        // ── 同步模型/模式信息（动态栏右段 / 输入区下方兜底行）──
        SyncModelInfo();

        // ── 同步侧栏（数据变了才重建分区）──
        SyncSidePanel();

        // ── 动态尺寸 ──
        ComputeLayout(out var panelW, out var inputH, out var promptH, out var progressH, out var chatH);

        // ── 压缩进度条 ──
        if (progressH > 0)
        {
            var pct = ProgressPercent!.Value;
            var barW = TW - 12;
            var filled = Math.Clamp((int)Math.Round(barW * pct / 100.0), 0, barW);
            var empty = barW - filled;
            var progressY = TH - 3; // 布局预留的 progressH 行（0-based；TH-2 是模型信息行，避免覆盖）
            var barText = $"«{new string('█', filled)}{new string('░', empty)}» {pct,3:F0}%";
            sb.Append(AnsiTty.CursorPos0(progressY, 0)) // CursorPos0 才是 0-based，CursorPos 是 1-based 会错位到 TH-3
              .Append(AnsiTty.Fg(AnsiColors.Yellow))
              .Append(barText.Length > TW ? barText[..TW] : barText.PadRight(TW))
              .Append(AnsiTty.SgrReset);
        }

        // ── 输入区 / 提示栏 / 分隔线 / 中间区域 ──
        ApplyDynamicSizes(panelW, inputH, promptH, chatH);

        // ── 建议面板定位（浮层，手动定位；Layout 因 Floating 不会覆盖 X/Y）──
        // 记录新矩形并与上一帧对比，移动/缩放/隐藏时补绘被遮挡区域，避免底色残留。
        int spX = 0, spY = 0, spW = 0, spH = 0;
        bool spVisible = SuggestPanel.Visible;
        if (spVisible)
        {
            spX = 0;
            int topBorderY = 1 + chatH;
            spY = Math.Max(1, topBorderY - SuggestPanel.Height);
            spW = SuggestPanel.Width;
            spH = SuggestPanel.Height;
            SuggestPanel.X = spX;
            SuggestPanel.Y = spY;
        }

        if (_suggestPrevVisible)
        {
            bool moved = spX != _suggestPrevX || spY != _suggestPrevY ||
                         spW != _suggestPrevW || spH != _suggestPrevH;
            if (!spVisible || moved)
                MarkDirtyRect(_suggestPrevX, _suggestPrevY, _suggestPrevW, _suggestPrevH);
        }
        _suggestPrevX = spX;
        _suggestPrevY = spY;
        _suggestPrevW = spW;
        _suggestPrevH = spH;
        _suggestPrevVisible = spVisible;

        // VBox/HBox 自动处理 Y 坐标
        RootView.Layout();
        base.Render(sb);
    }

    // ── 输入 ──

    /// <summary>待提交消息队列（Enter 键 → Program.cs 异步处理）</summary>
    public readonly ConcurrentQueue<string> PendingSubmissions = new();

    /// <summary>输入历史（↑↓ 浏览）</summary>
    internal readonly List<string> InputHistory = [];

    internal int HistoryIdx = -1;

    /// <summary>回调：切换模型（Program.cs 注入）</summary>
    public Action? OnCycleModel;

    /// <summary>回调：显示帮助（Program.cs 注入）</summary>
    public Action? OnShowHelp;

    /// <summary>回调：打开会话管理（Program.cs 注入）</summary>
    public Action? OnOpenSessions;

    /// <summary>回调：打开 diff 预览（/diff，Program.cs 注入，Ctrl+D）</summary>
    public Action? OnOpenDiff;

    /// <summary>回调：打开命令面板（Ctrl+Shift+P，Program.Repl 注入）</summary>
    public Action? OnOpenCommandPalette;

    /// <summary>回调：选择推理深度（Program.cs 注入）</summary>
    public Action? OnReasoningEffort;

    /// <summary>回调：搜索历史（Program.cs 注入，参数=查询字符串）</summary>
    public Action<string>? OnSearchHistory;

    /// <summary>显示退出确认对话框</summary>
    private void ShowExitConfirmDialog()
    {
        var win = TuiDialog.Confirm("退出 WayCoder", "确定要退出道码吗？", confirmed =>
        {
            if (confirmed)
                PendingSubmissions.Enqueue(AnsiTty.SgrReset); // 特殊标记：退出请求
        });
        ShowWindow(win);
    }

    /// <summary>Ctrl+Shift+P：打开命令面板（对齐 Claude Code quickOpen / OpenCode）。
    /// 走 OnOpenCommandPalette 回调（Program.Repl 接线，Keypad 可绑定标记验证）。</summary>
    private void OpenCommandPalette()
    {
        if (OnOpenCommandPalette != null) { OnOpenCommandPalette(); return; }
        var commands = WayCoder.UI.Tui.CommandPalette.BuildDefaultCommands(this);
        if (commands.Count == 0) return;
        WayCoder.UI.Tui.CommandPalette.Show(commands);
    }

    /// <summary>Ctrl+Shift+F1：弹出主题选择对话框</summary>
    private void ShowThemePicker()
    {
        var names = new List<string>(TuiTheme.PresetNames);
        var win = TuiDialog.Select("选择主题", names, idx =>
        {
            if (idx >= 0 && idx < TuiTheme.Presets.Length)
            {
                TuiTheme.Apply(TuiTheme.Presets[idx], idx);
                ApplyThemeToScreen();
                ShowToast($"🎨 主题已切换：{TuiTheme.PresetNames[idx]}", 1500);
            }
        });
        ShowWindow(win);
    }

    /// <summary>Ctrl+Shift+F2：直接轮转到下一个主题</summary>
    private void CycleThemeDirect()
    {
        var name = TuiTheme.CycleNext();
        ApplyThemeToScreen();
        ShowToast($"🎨 主题：{name}", 1500);
    }

    /// <summary>将当前主题颜色应用到屏幕各组件并强制重绘</summary>
    private void ApplyThemeToScreen()
    {
        var t = TuiTheme.Current;
        // 标题栏 + 底部状态栏
        if (TitleBar != null)
        {
            TitleBar.Bg = t.StatusBarBg;
            TitleBar.Fg = t.StatusBarFg;
        }

        if (StatusBar != null)
        {
            StatusBar.Bg = t.StatusBarBg;
            StatusBar.Fg = t.StatusBarFg;
        }

        // 分隔线
        if (InputTopBorder != null) InputTopBorder.LineColor = t.SeparatorFg;
        if (InputBotBorder != null) InputBotBorder.LineColor = t.SeparatorFg;
        // 输入区
        if (InputArea != null)
        {
            InputArea.Fg = t.TextAreaFg;
            InputArea.CursorLineBg = 0;            // 聊天输入框无光标行高亮
            InputArea.CursorLineFg = t.TextAreaFg; // 无高亮时文字跟随正文色
        }

        // 聊天消息列表：逐项刷新角色标签/时间戳颜色，正文缓存作废重解析
        if (ChatList != null)
        {
            for (int i = 0; i < ChatList.ItemCount; i++)
                (ChatList.GetItem(i) as TuiListItem)?.ApplyTheme();
        }

        InvalidateView();
    }
}
