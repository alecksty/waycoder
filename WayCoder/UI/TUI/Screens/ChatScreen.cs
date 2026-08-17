using System.Collections.Concurrent;
using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.Tools;
using WayCoder.UI.Tui.ToolRenderers;

using WayCoder.UI.Tui.Controls;

using WayCoder.UI.Shared;
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
    public TuiTitleBar TitleBar { get; private set; } = null!;

    /// <summary>底部状态栏</summary>
    public TuiStatusBar StatusBar { get; private set; } = null!;

    /// <summary>聊天列表（TuiListView → TuiMarkdown 项）</summary>
    public TuiListView ChatList { get; private set; } = null!;

    /// <summary>多行输入区</summary>
    public TuiTextArea InputArea { get; private set; } = null!;

    /// <summary>提示栏（输入框上方）</summary>
    public TuiPromptBar PromptBar { get; private set; } = null!;

    /// <summary>前缀提示钩子注册表：前缀符号 → 提示项生成器（触发提示框）。</summary>
    private readonly Dictionary<char, Func<string, List<PromptItem>>> _prefixHintHooks = new();

    /// <summary>内置前缀符号（/ @ ! #）。</summary>
    private static readonly char[] BuiltinPrefixes = ['/', '@', '!', '#'];

    /// <summary>动态栏（聊天列表下方、输入区上方，始终可见）</summary>
    public TuiDynamicBar DynamicBar { get; private set; } = null!;

    /// <summary>输入区上分隔线</summary>
    public TuiSeparator InputTopBorder { get; private set; } = null!;

    /// <summary>输入区下分隔线</summary>
    public TuiSeparator InputBotBorder { get; private set; } = null!;

    /// <summary>建议下拉面板</summary>
    public TuiVBox SuggestPanel { get; private set; } = null!;

    /// <summary>建议面板上一帧的可见矩形（用于移动/缩放/隐藏后补绘被遮挡的聊天内容）。</summary>
    private int _suggestPrevX = -1, _suggestPrevY = -1, _suggestPrevW, _suggestPrevH;
    private bool _suggestPrevVisible;

    /// <summary>右侧信息面板</summary>
    public TuiSidePanel SidePanel { get; private set; } = null!;

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

    /// <summary>状态栏左侧（模型名、git 分支等）</summary>
    public string StatusLeft { get; set; } = "";

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

    private void OnCompressProgress(int layer, string message, double percent)
    {
        if (ContextManager.IsCompressing && DynamicBar != null)
        {
            DynamicBar.Status = AgentStatus.Compressing;
            DynamicBar.ProgressPercent = percent;
            DynamicBar.ProgressLabel = $"[L{layer}] {message}";
            MarkDirty();
        }
        else if (DynamicBar != null)
        {
            DynamicBar.ProgressPercent = null;
            DynamicBar.ProgressLabel = "";
        }
    }

    /// <summary>
    /// 同步动态栏状态（Render 每帧调用）
    /// </summary>
    private void SyncDynamicBar()
    {
        if (DynamicBar == null) return;
        DynamicBar.Width = TW;
        DynamicBar.ContextPercent = _contextPercent; // 常驻上下文占用%

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

    /// <summary>等待权限的工具名（非 null = 正在等待）</summary>
    private string? _pendingPermissionTool;

    /// <summary>上下文占用百分比（null=未知，用于动态栏常驻显示）</summary>
    private double? _contextPercent;

    /// <summary>标记工具开始执行</summary>
    public void OnToolStarted(string toolName, string brief)
    {
        _currentToolName = toolName;
        _currentToolBrief = brief;
        MarkDirty();
    }

    /// <summary>标记工具执行结束</summary>
    public void OnToolFinished()
    {
        _currentToolName = null;
        _currentToolBrief = null;
        MarkDirty();
    }

    /// <summary>标记权限等待开始</summary>
    public void OnPermissionWaiting(string toolName)
    {
        _pendingPermissionTool = toolName;
        MarkDirty();
    }

    /// <summary>标记权限等待结束</summary>
    public void OnPermissionResolved()
    {
        _pendingPermissionTool = null;
        MarkDirty();
    }

    /// <summary>终端尺寸变化——重建完整布局，保留输入状态和全部聊天消息</summary>
    public override void OnResize(int newW, int newH)
    {
        var inputText = InputArea?.Text ?? "";
        int cursorRow = InputArea?.CursorRow ?? 0;
        int cursorCol = InputArea?.CursorCol ?? 0;

        // 保存旧 ChatList 的全部消息数据（BuildLayout 会创建新的空 ChatList）
        var savedMessages = new List<(string Role, string Content, bool Centered, int Indent)>();
        if (ChatList != null)
        {
            for (int i = 0; i < ChatList.ItemCount; i++)
            {
                var item = ChatList.GetItem(i) as TuiListItem;
                if (item != null)
                    savedMessages.Add((item.Role, item.MarkdownContent, item.ContentAlign == HAlign.Center, item.Indent));
            }
        }

        TW = newW;
        TH = newH;

        // 重建整个控件树
        BuildLayout();

        // 恢复聊天消息（通过 AddMessage 走正常流程，自动处理续接/纯文本逻辑）
        foreach (var (role, content, centered, indent) in savedMessages)
            AddMessage(content, role, centered, indent);

        // 恢复输入状态
        if (!string.IsNullOrEmpty(inputText))
        {
            InputArea!.Text = inputText;
            InputArea.CursorRow = Math.Min(cursorRow, InputArea.Lines.Count - 1);
            InputArea.CursorCol = Math.Min(cursorCol, InputArea.Lines[InputArea.CursorRow].Length);
        }

        // 恢复分隔线宽度
        PromptBar.Width = TW;
        DynamicBar.Width = TW;
        InputTopBorder.Width = TW;
        InputBotBorder.Width = TW;

        // 通知所有浮层窗口
        foreach (var win in Windows)
            win.OnResize(newW, newH);
    }

    /// <summary>
    /// 构建聊天屏幕布局
    /// </summary>
    private void BuildLayout()
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
            MaxVisible = 6,
        };
        RootView.Add(PromptBar);

        // ── 动态栏（始终可见，对标 Claude Code SpinnerWithVerb）──
        DynamicBar = new TuiDynamicBar
        {
            Width = TW,
            Height = 1,
            Bg = 0,
        };
        RootView.Add(DynamicBar);

        // ── 输入区上分隔线 ──
        InputTopBorder = new TuiSeparator
        {
            Width = TW,
            Height = 1,
            LineChar = "━",
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
            LineChar = "━", LineColor = TuiTheme.Current.SeparatorFg
        };
        RootView.Add(InputBotBorder);

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
    public void AddMessage(string content, string role = "assistant", bool centered = false, int indent = 0)
    {
        bool continuation = false;
        bool plainText = role is "system" or "tool" or "banner";
        if (plainText && role != "banner")
        {
            var last = ChatList.GetItem(ChatList.ItemCount - 1) as TuiListItem;
            if (last != null && last.Role == role)
            {
                continuation = true;
                // 续接消息继承前一条的对齐设置
                centered = last.ContentAlign == HAlign.Center;
            }
        }

        // 嵌套子消息强制续接（不渲染角色头）
        if (indent > 0)
            continuation = true;

        var item = new TuiListItem(role, content, ChatList.Width - 2,
            role == "banner" ? true : continuation, plainText,
            centered ? HAlign.Center : HAlign.Left)
        {
            Indent = indent
        };
        if (!continuation)
            item.SetTime(DateTime.Now);

        // 错误输出红色显示
        if (plainText && IsErrorOutput(content))
            item.Body.IsError = true;

        ChatList.AddItem(item);
        MarkDirty();
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
                    int rw = TuiHelper.RuneWidth(rune);
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
        TitleBar.Width = TW;
        TitleBar.Bg = TuiTheme.Current.StatusBarBg;
        TitleBar.Fg = TuiTheme.Current.StatusBarFg;
        TitleBar.Title = StatusLeft;
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

        // ── 动态尺寸 ──
        int panelW = SidePanelVisible ? Math.Min(30, TW / 3) : 0;
        int inputH = Math.Clamp(InputArea.Lines.Count + 1, 3, 5);
        int promptH = PromptBar.Visible ? PromptBar.Height : 0;
        int progressH = (ProgressPercent.HasValue && ContextManager.IsCompressing) ? 1 : 0;
        int chatH = Math.Max(1, TH - 1 - promptH - 1 - 1 - inputH - 1 - progressH - 1); // TH - title - prompt - dynamicBar(1) - topBorder - input - botBorder - progress - status

        // ── 压缩进度条 ──
        if (progressH > 0)
        {
            var pct = ProgressPercent!.Value;
            var barW = TW - 12;
            var filled = Math.Clamp((int)Math.Round(barW * pct / 100.0), 0, barW);
            var empty = barW - filled;
            var progressY = TH - 2; // 状态栏上方一行
            var barText = $"«{new string('█', filled)}{new string('░', empty)}» {pct,3:F0}%";
            sb.Append(AnsiTty.CursorPos(progressY, 0))
              .Append(AnsiTty.Fg(TuiColors.Yellow))
              .Append(StatusText.Length > TW - 2 ? StatusText[..(TW - 2)] : StatusText.PadRight(TW))
              .Append(AnsiTty.SgrReset);
        }

        // ── 输入区 ──
        InputArea.Width = TW;
        InputArea.Height = inputH;

        // ── 提示栏 ──
        PromptBar.Width = TW;

        // ── 分隔线 ──
        InputTopBorder.Width = TW;
        InputBotBorder.Width = TW;

        // ── 中间区域（ChatList + SidePanel HBox）──
        ChatList.Width = panelW > 0 ? TW - panelW : TW;
        ChatList.Height = chatH;

        SidePanel.Visible = SidePanelVisible;
        SidePanel.Width = panelW;
        SidePanel.Height = chatH;
        if (SidePanelVisible)
            SidePanel.Sections = SidePanelSections;

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
                TuiToastQueue.Enqueue($"主题已切换：{TuiTheme.PresetNames[idx]}", TuiToastQueue.ToastType.Success);
            }
        });
        ShowWindow(win);
    }

    /// <summary>Ctrl+Shift+F2：直接轮转到下一个主题</summary>
    private void CycleThemeDirect()
    {
        var name = TuiTheme.CycleNext();
        ApplyThemeToScreen();
        TuiToastQueue.Enqueue($"主题：{name}", TuiToastQueue.ToastType.Info);
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
