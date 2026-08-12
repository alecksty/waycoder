using System.Collections.Concurrent;
using System.Text;
using WayCoder.Terminal;
using WayCoder.Tools;
using WayCoder.UI.ToolRenderers;

using WayCoder.UI.TuiControls;

namespace WayCoder.UI.TuiScreens;

/// <summary>槽位状态</summary>
public enum SlotState : byte
{
    Idle = 0,
    Working = 1,
    WaitingPerm = 2,
    Error = 3
}

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
public class ChatScreen : TuiScreen
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

    /// <summary>动态栏（聊天列表下方、输入区上方，始终可见）</summary>
    public TuiDynamicBar DynamicBar { get; private set; } = null!;

    /// <summary>输入区上分隔线</summary>
    public TuiSeparator InputTopBorder { get; private set; } = null!;

    /// <summary>输入区下分隔线</summary>
    public TuiSeparator InputBotBorder { get; private set; } = null!;

    /// <summary>建议下拉面板</summary>
    public TuiVBox SuggestPanel { get; private set; } = null!;

    /// <summary>右侧信息面板</summary>
    public TuiSidePanel SidePanel { get; private set; } = null!;

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

        // 压缩中（从 CompressProgress 事件已设置，保持不变）
        if (DynamicBar.Status == AgentStatus.Compressing && ContextManager.IsCompressing)
            return;
        if (DynamicBar.Status == AgentStatus.Compressing && !ContextManager.IsCompressing)
            DynamicBar.ProgressPercent = null; // 压缩完成，清理

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
        var savedMessages = new List<(string Role, string Content, bool Centered)>();
        if (ChatList != null)
        {
            for (int i = 0; i < ChatList.ItemCount; i++)
            {
                var item = ChatList.GetItem(i) as TuiListItem;
                if (item != null)
                    savedMessages.Add((item.Role, item.MarkdownContent, item.ContentAlign == HAlign.Center));
            }
        }

        TW = newW;
        TH = newH;

        // 重建整个控件树
        BuildLayout();

        // 恢复聊天消息（通过 AddMessage 走正常流程，自动处理续接/纯文本逻辑）
        foreach (var (role, content, centered) in savedMessages)
            AddMessage(content, role, centered);

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

        // ── 建议面板 ──
        SuggestPanel = new TuiVBox
        {
            Width = Math.Min(TW, 60),
            Height = 0,
            Visible = false,
            Bg = 47
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

    /// <summary>添加一条消息到聊天列表。system/tool 消息使用纯文本模式避免 Markdown 行合并，连续同角色自动续接。</summary>
    public void AddMessage(string content, string role = "assistant", bool centered = false)
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

        var item = new TuiListItem(role, content, ChatList.Width - 2,
            role == "banner" ? true : continuation, plainText,
            centered ? HAlign.Center : HAlign.Left);
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

        // 仅对工具输出（system 消息）应用显示风格控制
        if (last.Role == "system")
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

    /// <summary>添加工具调用消息</summary>
    public void AddToolMsg(string toolName, string brief)
    {
        var content = $"  🔧 {toolName}({brief})";
        var msg = new ChatMsg { Role = "tool", Content = content };
        ChatMessages.Add(msg);
        AddMessage(content, "tool");
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

    // ── 粘贴 ──

    public async Task PasteAsync()
    {
        try
        {
            var text = await ClipboardHelper.GetTextAsync();
            if (string.IsNullOrEmpty(text)) return;

            // 粘贴确认：超长(>500字符)或多行(>3行)时弹出确认
            var lines = text.Replace("\r\n", "\n").Split('\n');
            if (text.Length > 500 || lines.Length > 3)
            {
                var preview = text.Length > 200 ? text[..200] + "..." : text;
                using var evt = new ManualResetEventSlim(false);
                bool confirmed = false;
                ShowWindow(TuiDialog.Confirm("粘贴确认",
                    $"将粘贴 {lines.Length} 行 / {text.Length} 字符:\n{preview}",
                    result => { confirmed = result; evt.Set(); }));
                RenderWait(evt);
                if (!confirmed) return;
            }

            InputArea.InsertText(text);
            MarkDirty();
        }
        catch
        {
            /* 忽略粘贴错误 */
        }
    }

    /// <summary>
    /// 处理 bracketed paste 检测到的粘贴文本（终端自动包裹，无需读剪贴板）。
    /// </summary>
    public void HandleBracketedPaste(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        var normalized = text.Replace("\r\n", "\n");
        var lines = normalized.Split('\n');

        // 粘贴确认：超长(>500字符)或多行(>3行)时弹出确认
        if (text.Length > 500 || lines.Length > 3)
        {
            var preview = text.Length > 200 ? text[..200] + "..." : text;
            using var evt = new ManualResetEventSlim(false);
            bool confirmed = false;
            ShowWindow(TuiDialog.Confirm("粘贴确认",
                $"将粘贴 {lines.Length} 行 / {text.Length} 字符:\n{preview}",
                result => { confirmed = result; evt.Set(); }));
            RenderWait(evt);
            if (!confirmed) return;
        }

        InputArea.InsertText(normalized);
        MarkDirty();
    }

    // ── 输入操作 ──

    /// <summary>获取输入文本（处理多行合并）</summary>
    public string GetInputText()
    {
        return InputArea.Text;
    }

    /// <summary>在输入区插入文本</summary>
    public void InputInsert(string text)
    {
        InputArea.InsertText(text);
        MarkDirty();
    }

    /// <summary>输入区退格</summary>
    public void InputBackspace()
    {
        InputArea.OnKey(new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));
        MarkDirty();
    }

    /// <summary>输入区删除</summary>
    public void InputDelete()
    {
        InputArea.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Delete, false, false, false));
        MarkDirty();
    }

    /// <summary>输入区左移光标</summary>
    public void InputCursorLeft()
    {
        InputArea.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false));
        MarkDirty();
    }

    /// <summary>输入区右移光标</summary>
    public void InputCursorRight()
    {
        InputArea.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false));
        MarkDirty();
    }

    /// <summary>输入区移到行首</summary>
    public void InputHome()
    {
        InputArea.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false));
        MarkDirty();
    }

    /// <summary>输入区移到行尾</summary>
    public void InputEnd()
    {
        InputArea.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, false));
        MarkDirty();
    }

    /// <summary>接受当前建议</summary>
    public void AcceptSuggestion()
    {
        if (SuggestActive && SuggestIndex >= 0 && SuggestIndex < Suggestions.Count)
        {
            SetInput(Suggestions[SuggestIndex]);
            HideSuggestions();
            SuggestActive = false;
        }
    }

    /// <summary>更新建议并标记活跃</summary>
    public void RefreshSuggestions(List<string> items, int selectedIdx)
    {
        Suggestions = items;
        SuggestIndex = selectedIdx;
        SuggestActive = items.Count > 0;
        UpdateSuggestions(items, selectedIdx);
    }

    // ── 建议面板 ──

    /// <summary>更新建议面板</summary>
    public void UpdateSuggestions(List<string> items, int selectedIdx)
    {
        Suggestions = items;
        SuggestIndex = selectedIdx;
        SuggestPanel.Clear();
        SuggestPanel.Visible = items.Count > 0;
        if (items.Count == 0) return;

        int panelH = Math.Min(items.Count, 12);
        int panelW = Math.Min(TW, 60);

        for (int i = 0; i < Math.Min(items.Count, 12); i++)
        {
            var item = items[i];
            var label = new TuiLabel(item)
            {
                Width = panelW,
                Height = 1,
                Fg = i == selectedIdx ? 30 : 37,
                Bg = i == selectedIdx ? 46 : 7
            };
            SuggestPanel.Add(label);
        }

        SuggestPanel.Width = panelW;
        SuggestPanel.Height = panelH;
        SuggestPanel.Layout();
    }

    /// <summary>隐藏建议面板</summary>
    public void HideSuggestions()
    {
        SuggestPanel.Visible = false;
        Suggestions.Clear();
        SuggestActive = false;
    }

    // ── 侧栏 ──

    /// <summary>刷新侧栏所有分区内容</summary>
    public void RefreshSidePanel()
    {
        var sections = new List<PanelSection>();

        // ── 品牌区 ──
        sections.Add(new PanelSection
        {
            Title = "🏷 道码",
            Lines =
            [
                $"  WayCoder v{Global.Version}",
                "  中文版 AI 编程助手",
                "  C# (.NET 10) AOT",
            ]
        });

        // ── Todo 区 ──
        var todoItems = TodoTool.Items;
        var todoLines = new List<string>();
        if (todoItems.Count == 0)
        {
            todoLines.Add("  (无)");
        }
        else
        {
            var completed = todoItems.Count(i => i.Status == "completed");
            foreach (var item in todoItems.OrderBy(i => i.Id).Take(15))
            {
                var icon = item.Status switch
                {
                    "completed" => "✅",
                    "in_progress" => "🔄",
                    "cancelled" => "❌",
                    _ => "⏳",
                };
                var title = item.Title.Length > 20 ? item.Title[..17] + "..." : item.Title;
                todoLines.Add($"  {icon} {title}");
            }
        }

        sections.Add(new PanelSection
        {
            Title = $"📋 Todo ({todoItems.Count(i => i.Status == "completed")}/{todoItems.Count})",
            Lines = todoLines,
        });

        // ── 文件区 ──
        var fileLines = new List<string>();
        if (ModifiedFiles.Count == 0)
            fileLines.Add("  (无)");
        else
            foreach (var f in ModifiedFiles.Take(15))
                fileLines.Add($"  📄 {Path.GetFileName(f)}");
        sections.Add(new PanelSection
        {
            Title = $"📁 文件 ({ModifiedFiles.Count})",
            Lines = fileLines,
        });

        // ── MCP 区 ──
        var mcpLines = new List<string>();
        var mcpTools = McpManager.DiscoveredTools;
        if (mcpTools.Count == 0)
            mcpLines.Add($"  {McpManager.Info}");
        else
            foreach (var t in mcpTools.Take(15))
                mcpLines.Add($"  🔌 {t.Name}");
        sections.Add(new PanelSection
        {
            Title = $"🔌 MCP ({mcpTools.Count})",
            Lines = mcpLines,
        });

        // ── LSP 区 ──
        var lspLines = new List<string>();
        foreach (var kv in LspTool.SupportedServers)
            lspLines.Add($"  📦 {kv.Key}: {kv.Value.Command}");
        sections.Add(new PanelSection
        {
            Title = $"🔍 LSP ({LspTool.SupportedServers.Count})",
            Lines = lspLines,
        });

        SidePanelSections = sections;
    }

    // ── 提示栏 ──

    /// <summary>显示提示栏（命令/文件/Shell 等建议列表）</summary>
    public void ShowPromptBar(List<PromptItem> items)
    {
        PromptBar.Items = items;
        PromptBar.SelectedIndex = items.Count > 0 ? 0 : -1;
        PromptBar.Visible = true;
        int h = Math.Min(items.Count, PromptBar.MaxVisible);
        // Bg==0 边框模式需 +2（上下边框），Bg>0 填充模式需 +1（底部分隔线）
        int extra = PromptBar.Bg == 0 ? 2 : 1;
        PromptBar.Height = h * PromptBar.ItemHeight + extra;

        // 在 InputArea 上挂 KeyHook：拦截 ↑↓/Enter/Esc/Tab，透传其他键
        InputArea.KeyHook = PromptKeyHook;

        MarkDirty();
    }

    /// <summary>隐藏提示栏</summary>
    public void HidePromptBar()
    {
        PromptBar.Visible = false;
        PromptBar.Height = 0;
        PromptBar.Items.Clear();
        PromptBar.SelectedIndex = -1;
        InputArea.KeyHook = null;
        MarkDirty();
    }

    /// <summary>挂载在 InputArea 上的按键钩子：↑↓/Enter/Esc 导航提示栏</summary>
    private bool PromptKeyHook(ConsoleKeyInfo key)
    {
        if (!PromptBarVisible) return false;

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                HidePromptBar();
                return true;
            case ConsoleKey.UpArrow:
            case ConsoleKey.DownArrow:
            case ConsoleKey.Home:
            case ConsoleKey.End:
                PromptBar.OnKey(key);
                MarkDirty();
                return true;
            case ConsoleKey.Enter:
                if (PromptBar.SelectedIndex >= 0 && PromptBar.SelectedIndex < PromptBar.Items.Count)
                {
                    var item = PromptBar.Items[PromptBar.SelectedIndex];
                    if (!string.IsNullOrEmpty(item.Value))
                        SetInput(item.Value);
                    HidePromptBar();
                }

                return true;
            case ConsoleKey.Tab:
                if (PromptBar.SelectedIndex >= 0 && PromptBar.SelectedIndex < PromptBar.Items.Count)
                {
                    var item = PromptBar.Items[PromptBar.SelectedIndex];
                    if (!string.IsNullOrEmpty(item.Value))
                        SetInput(item.Value);
                    MarkDirty();
                }

                return true;
        }

        // 其他键透传，让 InputArea 正常处理（CheckPrefixHints 会自动刷新）
        return false;
    }

    /// <summary>提示栏是否可见</summary>
    public bool PromptBarVisible => PromptBar.Visible;

    /// <summary>构建默认提示列表（命令 + 最近文件 + 快捷操作）</summary>
    private List<PromptItem> BuildDefaultHints()
    {
        var items = new List<PromptItem>();

        // ── 快捷命令 ──
        items.Add(new PromptItem { Kind = PromptKind.Command, Label = "帮助", Detail = "显示帮助信息", Value = "/help" });
        items.Add(new PromptItem { Kind = PromptKind.Command, Label = "切换模型", Detail = "轮换 LLM", Value = "/model" });
        items.Add(new PromptItem { Kind = PromptKind.Command, Label = "/model set <id>", Detail = "设置大模型", Value = "/model set " });
        items.Add(new PromptItem { Kind = PromptKind.Command, Label = "/model list", Detail = "列出所有模型", Value = "/model list" });
        items.Add(new PromptItem { Kind = PromptKind.Command, Label = "/model import <path>", Detail = "导入外部配置", Value = "/model import " });
        items.Add(new PromptItem { Kind = PromptKind.Command, Label = "清空对话", Detail = "重置上下文", Value = "/clear" });
        items.Add(new PromptItem { Kind = PromptKind.Command, Label = "历史搜索", Detail = "搜索对话记录", Value = "/history " });
        items.Add(new PromptItem { Kind = PromptKind.Command, Label = "YOLO 模式", Detail = "跳过权限确认", Value = "/perm yolo" });
        items.Add(new PromptItem { Kind = PromptKind.Command, Label = "/perm ask", Detail = "每次确认模式", Value = "/perm ask" });
        items.Add(new PromptItem { Kind = PromptKind.Command, Label = "/perm auto", Detail = "首次后自动允许", Value = "/perm auto" });
        items.Add(new PromptItem { Kind = PromptKind.Command, Label = "Diff 预览", Detail = "切换 diff 预览", Value = "/diff" });

        // ── 文件操作 ──
        items.Add(new PromptItem { Kind = PromptKind.Slash, Label = "/edit", Detail = "编辑文件", Value = "/edit " });
        items.Add(new PromptItem { Kind = PromptKind.Slash, Label = "/read", Detail = "读取文件", Value = "/read " });
        items.Add(new PromptItem { Kind = PromptKind.Slash, Label = "/write", Detail = "写入文件", Value = "/write " });

        // ── 最近修改文件 ──
        if (ModifiedFiles.Count > 0)
        {
            foreach (var f in ModifiedFiles.Take(4))
                items.Add(new PromptItem { Kind = PromptKind.File, Label = Path.GetFileName(f), Detail = "最近修改", Value = $"@\"{f}\" " });
        }

        // ── Shell ──
        items.Add(new PromptItem { Kind = PromptKind.Shell, Label = "dotnet build", Detail = "编译项目", Value = "!dotnet build" });
        items.Add(new PromptItem { Kind = PromptKind.Shell, Label = "dotnet test", Detail = "运行测试", Value = "!dotnet test" });
        items.Add(new PromptItem { Kind = PromptKind.Shell, Label = "git status", Detail = "查看状态", Value = "!git status" });
        items.Add(new PromptItem { Kind = PromptKind.Shell, Label = "git diff", Detail = "查看变更", Value = "!git diff" });

        return items;
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
        int chatH = Math.Max(1, TH - 1 - promptH - 1 - inputH - 1 - progressH - 1); // TH - title - prompt - topBorder - input - botBorder - progress - status

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

        // ── 建议面板定位（浮层，手动定位）──
        if (SuggestPanel.Visible)
        {
            SuggestPanel.X = 0;
            int topBorderY = 1 + chatH;
            SuggestPanel.Y = topBorderY - SuggestPanel.Height;
        }

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
            InputArea.CursorLineBg = t.TextAreaCursorLineBg;
        }

        InvalidateView();
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);
        bool shift = key.Modifiers.HasFlag(ConsoleModifiers.Shift);

        // ── 1. 建议面板可见 → 建议导航（始终优先）──
        if (HandleSuggestPanelKey(key, ctrl, shift)) return true;

        // ── 2. 模态窗口优先 ──
        if (HasModal) return base.OnKey(key);

        // ── 4. 聊天自身处理（全局快捷键 + 导航 + 提交 + 输入编辑）──
        if (HandleGlobalShortcut(key, ctrl, shift)
            || HandleChatNavigation(key, ctrl, shift)
            || HandleSpecial(key, ctrl, shift)
            || HandleInputEditing(key, ctrl, shift))
            return true;

        // ── 4. Fall through：基类路由到窗口 / RootView / 输入区 ──
        return base.OnKey(key);
    }

    // ── OnKey 子方法 ──

    /// <summary>处理建议面板导航（可见时拦截方向键/Enter/Tab/Esc）</summary>
    private bool HandleSuggestPanelKey(ConsoleKeyInfo key, bool ctrl, bool shift)
    {
        if (!SuggestActive) return false;

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                HideSuggestions();
                return true;
            case ConsoleKey.UpArrow:
                SuggestIndex = Math.Max(0, SuggestIndex - 1);
                UpdateSuggestions(Suggestions, SuggestIndex);
                return true;
            case ConsoleKey.DownArrow:
                SuggestIndex = Math.Min(Suggestions.Count - 1, SuggestIndex + 1);
                UpdateSuggestions(Suggestions, SuggestIndex);
                return true;
            case ConsoleKey.PageUp:
                SuggestIndex = Math.Max(0, SuggestIndex - 5);
                UpdateSuggestions(Suggestions, SuggestIndex);
                return true;
            case ConsoleKey.PageDown:
                SuggestIndex = Math.Min(Suggestions.Count - 1, SuggestIndex + 5);
                UpdateSuggestions(Suggestions, SuggestIndex);
                return true;
            case ConsoleKey.Home:
                SuggestIndex = 0;
                UpdateSuggestions(Suggestions, SuggestIndex);
                return true;
            case ConsoleKey.End:
                SuggestIndex = Suggestions.Count - 1;
                UpdateSuggestions(Suggestions, SuggestIndex);
                return true;
            case ConsoleKey.Enter:
            case ConsoleKey.Tab:
                AcceptSuggestion();
                return true;
            case ConsoleKey.Backspace:
                InputBackspace();
                UpdateSuggestions(Suggestions, SuggestIndex);
                return true; // 已处理，不再向下传递
            case ConsoleKey.LeftArrow:
            case ConsoleKey.RightArrow:
                SuggestActive = false;
                return false; // 继续传递，让光标移动生效
        }

        return false;
    }

    /// <summary>全局快捷键：Ctrl+E/T/O/B/R/M/H/Q, F1-F10, Ctrl+Home/End/Up/Down</summary>
    private bool HandleGlobalShortcut(ConsoleKeyInfo key, bool ctrl, bool shift)
    {
        // ── Ctrl 组合键 ──
        if (ctrl)
        {
            switch (key.Key)
            {
                case ConsoleKey.E:
                    Manager?.PushScreen(new EditorScreen());
                    return true;
                case ConsoleKey.T:
                case ConsoleKey.O:
                    Manager?.PushScreen(new SettingsScreen());
                    return true;
                case ConsoleKey.B:
                    SidePanelVisible = !SidePanelVisible;
                    if (SidePanelVisible)
                        RefreshSidePanel();
                    return true;
                case ConsoleKey.R:
                    var searchQuery = UxHelper.Ask("搜索对话历史");
                    if (!string.IsNullOrWhiteSpace(searchQuery))
                        OnSearchHistory?.Invoke("/history " + searchQuery);
                    return true;
                case ConsoleKey.M:
                    OnCycleModel?.Invoke();
                    return true;
                case ConsoleKey.S:
                    OnOpenSessions?.Invoke();
                    return true;
                case ConsoleKey.G:
                    OnReasoningEffort?.Invoke();
                    return true;
                case ConsoleKey.H:
                    OnShowHelp?.Invoke();
                    return true;
                case ConsoleKey.P:
                    if (PromptBarVisible)
                    {
                        HidePromptBar();
                        return true;
                    }

                    ShowPromptBar(BuildDefaultHints());
                    return true;
                case ConsoleKey.Q:
                    ShowExitConfirmDialog();
                    return true;
                // 聊天滚动
                case ConsoleKey.Home:
                    ChatScrollTop();
                    return true;
                case ConsoleKey.End:
                    ChatScrollBottom();
                    return true;
                case ConsoleKey.UpArrow:
                    ChatScrollUp(3);
                    return true;
                case ConsoleKey.DownArrow:
                    ChatScrollDown(3);
                    return true;
            }
        }

        // ── Ctrl+Shift+F1 主题选择 / Ctrl+Shift+F2 直接轮转 ──
        if (ctrl && shift)
        {
            switch (key.Key)
            {
                case ConsoleKey.F1:
                    ShowThemePicker();
                    return true;
                case ConsoleKey.F2:
                    CycleThemeDirect();
                    return true;
            }
        }

        // ── F1-F10 槽位切换 ──
        if (key.Key >= ConsoleKey.F1 && key.Key <= ConsoleKey.F10)
        {
            int slot = key.Key - ConsoleKey.F1;
            if (slot != ActiveSlotIndex)
                SwitchToSlot(slot);
            return true;
        }

        return false;
    }

    /// <summary>切换到指定槽位</summary>
    private void SwitchToSlot(int slot)
    {
        if (slot < 0 || slot >= 10) return;
        if (SlotStates[ActiveSlotIndex] == SlotState.Working) return; // 运行时禁止切换
        ActiveSlotIndex = slot;
        MarkDirty();
    }

    /// <summary>聊天滚动：PgUp/PgDn（非 Ctrl）</summary>
    private bool HandleChatNavigation(ConsoleKeyInfo key, bool ctrl, bool shift)
    {
        if (key.Key == ConsoleKey.PageUp)
        {
            ChatScrollUp(Math.Max(1, (Tty.Rows - 10) / 2));
            return true;
        }

        if (key.Key == ConsoleKey.PageDown)
        {
            ChatScrollDown(Math.Max(1, (Tty.Rows - 10) / 2));
            return true;
        }

        return false;
    }

    /// <summary>消息提交 / 退出确认</summary>
    private bool HandleSpecial(ConsoleKeyInfo key, bool ctrl, bool shift)
    {
        // Enter → 提交消息
        if (key.Key == ConsoleKey.Enter && !ctrl && !shift)
        {
            SuggestActive = false;
            var input = GetInputText();
            if (string.IsNullOrWhiteSpace(input)) return true;
            AddUserMsg(input);
            if (InputHistory.Count == 0 || InputHistory[^1] != input)
                InputHistory.Add(input);
            if (InputHistory.Count > 200) InputHistory.RemoveAt(0);
            TuiInputHistory.Add("chat", input);
            HistoryIdx = -1;
            SetInput("");
            PendingSubmissions.Enqueue(input);
            return true;
        }

        // Escape 空输入 → 退出确认（300ms 冷却：防止关闭模态框的 Escape 按键重复触发）
        if (key.Key == ConsoleKey.Escape && string.IsNullOrEmpty(GetInputText()))
        {
            if ((DateTime.UtcNow - LastModalEscapeTime).TotalMilliseconds < 300)
                return true; // 吞掉按键重复，不弹窗
            ShowExitConfirmDialog();
            return true;
        }

        return false;
    }

    /// <summary>输入区编辑：粘贴/换行/历史/补全/委托给 InputArea</summary>
    private bool HandleInputEditing(ConsoleKeyInfo key, bool ctrl, bool shift)
    {
        // 粘贴快捷键
        if ((key.Key == ConsoleKey.V && ctrl && !shift) ||
            (key.Key == ConsoleKey.Insert && shift))
        {
            _ = PasteAsync();
            return true;
        }

        // Ctrl+Enter / Shift+Enter → 换行
        if (key.Key == ConsoleKey.Enter && (ctrl || shift))
        {
            InputNewLine();
            return true;
        }

        // ↑↓ — 历史浏览 / 多行移动 / 空输入滚动
        if (key.Key == ConsoleKey.UpArrow)
        {
            return HandleInputUpArrow();
        }

        if (key.Key == ConsoleKey.DownArrow)
        {
            return HandleInputDownArrow();
        }

        // Tab — 路径补全
        if (key.Key == ConsoleKey.Tab)
        {
            return HandleTabCompletion();
        }

        // 先委托给 InputArea 处理字符输入
        var handled = InputArea.OnKey(key);

        // 输入后检测前缀符号，弹出对应提示
        CheckPrefixHints();

        return handled;
    }

    /// <summary>检测输入中的 / # @ ! 前缀，弹出对应提示栏</summary>
    private void CheckPrefixHints()
    {
        var text = GetInputText();
        int cursorPos = InputArea.CursorCol;
        if (InputArea.Lines.Count == 1)
            cursorPos = Math.Min(cursorPos, text.Length);

        // 单行模式才触发（多行输入不弹提示）
        if (InputArea.Lines.Count != 1)
        {
            if (PromptBarVisible) HidePromptBar();
            return;
        }

        // 从光标位置向前找最近的前缀符号
        char prefix = '\0';
        int prefixPos = -1;
        for (int i = cursorPos - 1; i >= 0; i--)
        {
            char c = text[i];
            if (c == '/' || c == '@' || c == '!' || c == '#')
            {
                // 确保前缀在行首或空格后
                if (i == 0 || text[i - 1] == ' ' || text[i - 1] == '\n')
                {
                    prefix = c;
                    prefixPos = i;
                    break;
                }
            }

            if (c == ' ' || c == '\n') break;
        }

        if (prefix == '\0' || prefixPos < 0)
        {
            if (PromptBarVisible) HidePromptBar();
            return;
        }

        // 提取前缀后的部分文本作为过滤词
        var query = text[(prefixPos + 1)..cursorPos];

        // 根据前缀构建提示
        var items = BuildPrefixHints(prefix, query);
        if (items.Count == 0)
        {
            HidePromptBar();
            return;
        }

        ShowPromptBar(items);
    }

    /// <summary>根据前缀符号和查询构建提示列表</summary>
    private List<PromptItem> BuildPrefixHints(char prefix, string query)
    {
        var items = new List<PromptItem>();
        var q = query.TrimStart();

        switch (prefix)
        {
            case '/': // 斜杠命令
                var slashCmds = new (string cmd, string desc)[]
                {
                    ("/help", "显示帮助信息"),
                    ("/model", "切换 LLM 模型"),
                    ("/clear", "清空对话上下文"),
                    ("/history", "搜索对话历史"),
                    ("/perm yolo", "跳过权限确认"),
                    ("/perm ask", "恢复权限确认"),
                    ("/diff", "切换 diff 预览"),
                    ("/edit", "编辑文件"),
                    ("/read", "读取文件"),
                    ("/write", "写入文件"),
                    ("/todo", "显示任务列表"),
                    ("/theme", "切换主题"),
                    ("/tokens", "显示 Token 用量"),
                    ("/status", "显示系统状态"),
                };
                foreach (var (cmd, desc) in slashCmds)
                {
                    if (string.IsNullOrEmpty(q) ||
                        cmd.Contains(q, StringComparison.OrdinalIgnoreCase))
                        items.Add(new PromptItem { Kind = PromptKind.Slash, Label = cmd, Detail = desc, Value = cmd + " " });
                }

                break;

            case '@': // 文件引用
                try
                {
                    string dir = ".";
                    string fileQuery = q;
                    int lastSlash = q.LastIndexOf('/');
                    if (lastSlash >= 0)
                    {
                        dir = q[..(lastSlash + 1)];
                        fileQuery = q[(lastSlash + 1)..];
                    }

                    if (Directory.Exists(dir))
                    {
                        foreach (var entry in Directory.EnumerateFileSystemEntries(dir).Take(20))
                        {
                            var name = Path.GetFileName(entry);
                            if (!string.IsNullOrEmpty(fileQuery) &&
                                !name.StartsWith(fileQuery, StringComparison.OrdinalIgnoreCase))
                                continue;
                            var display = lastSlash >= 0 ? q[..(lastSlash + 1)] + name : name;
                            if (Directory.Exists(entry)) display += "/";
                            items.Add(new PromptItem
                            {
                                Kind = PromptKind.File,
                                Label = display,
                                Detail = Directory.Exists(entry) ? "目录" : "文件",
                                Value = "@" + display + " "
                            });
                        }
                    }
                }
                catch
                {
                    /* 权限不足忽略 */
                }

                // 也加入最近修改的文件
                if (string.IsNullOrEmpty(q))
                {
                    foreach (var f in ModifiedFiles.Take(5))
                    {
                        var name = Path.GetFileName(f);
                        items.Add(new PromptItem { Kind = PromptKind.Recent, Label = name, Detail = "最近修改", Value = "@\"" + f + "\" " });
                    }
                }

                break;

            case '!': // Shell 命令
                var shellCmds = new (string cmd, string desc)[]
                {
                    ("dotnet build", "编译项目"),
                    ("dotnet run", "运行项目"),
                    ("dotnet test", "运行测试"),
                    ("dotnet publish -c Release", "AOT 发布"),
                    ("git status", "查看仓库状态"),
                    ("git diff", "查看变更"),
                    ("git add -A", "暂存所有变更"),
                    ("git commit -m", "提交"),
                    ("git push", "推送"),
                    ("git pull", "拉取"),
                    ("git log --oneline", "查看日志"),
                    ("ls -la", "列出文件"),
                    ("find . -name", "搜索文件"),
                    ("grep -r", "搜索内容"),
                };
                foreach (var (cmd, desc) in shellCmds)
                {
                    if (string.IsNullOrEmpty(q) ||
                        cmd.StartsWith(q, StringComparison.OrdinalIgnoreCase))
                        items.Add(new PromptItem { Kind = PromptKind.Shell, Label = cmd, Detail = desc, Value = "!" + cmd });
                }

                break;

            case '#': // 标签/Issue/PR 引用
                items.Add(new PromptItem { Kind = PromptKind.Command, Label = "#todo", Detail = "待办事项", Value = "#todo " });
                items.Add(new PromptItem { Kind = PromptKind.Command, Label = "#fix", Detail = "修复", Value = "#fix " });
                items.Add(new PromptItem { Kind = PromptKind.Command, Label = "#wip", Detail = "进行中", Value = "#wip " });
                items.Add(new PromptItem { Kind = PromptKind.Command, Label = "#done", Detail = "已完成", Value = "#done " });
                break;
        }

        return items;
    }

    /// <summary>输入区 ↑ 箭头：历史/多行移动/滚动</summary>
    private bool HandleInputUpArrow()
    {
        if (InputArea.Lines.Count == 1)
        {
            if (string.IsNullOrEmpty(GetInputText()))
            {
                ChatScrollUp(3);
                return true;
            }

            if (InputHistory.Count > 0)
            {
                if (HistoryIdx == -1) HistoryIdx = InputHistory.Count - 1;
                else if (HistoryIdx > 0) HistoryIdx--;
                SetInput(InputHistory[HistoryIdx]);
            }
        }
        else InputMoveUp();

        return true;
    }

    /// <summary>输入区 ↓ 箭头：历史/多行移动/滚动</summary>
    private bool HandleInputDownArrow()
    {
        if (InputArea.Lines.Count == 1)
        {
            if (string.IsNullOrEmpty(GetInputText()))
            {
                ChatScrollDown(3);
                return true;
            }

            if (HistoryIdx >= 0)
            {
                HistoryIdx++;
                SetInput(HistoryIdx < InputHistory.Count ? InputHistory[HistoryIdx] : "");
                if (HistoryIdx >= InputHistory.Count) HistoryIdx = -1;
            }
        }
        else InputMoveDown();

        return true;
    }

    /// <summary>Tab 路径补全：检测 @文件名 模式 → glob 文件系统 → 显示建议</summary>
    private bool HandleTabCompletion()
    {
        var input = GetInputText();
        int cursorPos = InputArea.CursorCol;
        if (InputArea.Lines.Count == 1)
            cursorPos = Math.Min(cursorPos, input.Length);

        // 找光标前最近的 @ 符号
        int atPos = -1;
        for (int i = cursorPos - 1; i >= 0; i--)
        {
            if (input[i] == '@' && (i == 0 || input[i - 1] == ' ' || input[i - 1] == '\n'))
            {
                atPos = i;
                break;
            }

            if (input[i] == ' ' || input[i] == '\n') break;
        }

        if (atPos < 0)
        {
            // 无 @ 模式：插入 4 空格
            for (int t = 0; t < 4; t++) InputInsert(' ');
            return true;
        }

        // 提取 @ 后的部分路径
        var partial = input[(atPos + 1)..cursorPos];
        if (string.IsNullOrEmpty(partial))
        {
            // 仅 @：列出当前目录文件
            var files = ListFilesForCompletion("");
            if (files.Count > 0)
            {
                RefreshSuggestions(files, 0);
                SuggestActive = true;
            }

            return true;
        }

        // 有部分路径： glob 匹配
        var matches = ListFilesForCompletion(partial);
        if (matches.Count == 0)
        {
            // 无匹配：插入空格
            InputInsert(' ');
            return true;
        }

        if (matches.Count == 1)
        {
            // 唯一匹配：直接补全
            ReplaceAtPrefix(atPos + 1, cursorPos, matches[0]);
            return true;
        }

        // 多个匹配：显示建议面板，并补全公共前缀
        var commonPrefix = GetCommonPrefix(matches);
        if (commonPrefix.Length > partial.Length)
        {
            ReplaceAtPrefix(atPos + 1, cursorPos, commonPrefix);
        }

        RefreshSuggestions(matches, 0);
        SuggestActive = true;
        return true;
    }

    /// <summary>列出匹配前缀的文件/目录</summary>
    private List<string> ListFilesForCompletion(string partial)
    {
        var results = new List<string>();
        string dir = ".";
        string prefix = partial;

        // 解析目录部分
        int lastSlash = partial.LastIndexOf('/');
        if (lastSlash >= 0)
        {
            dir = partial[..(lastSlash + 1)];
            prefix = partial[(lastSlash + 1)..];
        }

        // 确保 dir 是有效路径
        if (!Directory.Exists(dir))
            dir = ".";

        try
        {
            // 匹配文件和目录
            foreach (var entry in Directory.EnumerateFileSystemEntries(dir))
            {
                var name = Path.GetFileName(entry);
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string display = lastSlash >= 0
                        ? partial[..(lastSlash + 1)] + name
                        : name;
                    if (Directory.Exists(entry))
                        display += "/";
                    results.Add(display);
                }
            }
        }
        catch
        {
            /* 权限不足等错误静默忽略 */
        }

        results.Sort(StringComparer.OrdinalIgnoreCase);
        return results;
    }

    /// <summary>获取字符串列表的公共前缀</summary>
    private static string GetCommonPrefix(List<string> items)
    {
        if (items.Count == 0) return "";
        if (items.Count == 1) return items[0];
        var first = items[0];
        int len = 0;
        for (int i = 0; i < first.Length; i++)
        {
            char c = first[i];
            if (items.Any(s => s.Length <= i || s[i] != c)) break;
            len++;
        }

        return first[..len];
    }

    /// <summary>替换从 start 到 end 位置的文本（在单行输入中）</summary>
    private void ReplaceAtPrefix(int start, int end, string replacement)
    {
        var text = GetInputText();
        if (start < 0 || end > text.Length || start > end) return;
        var newText = text[..start] + replacement + text[end..];
        SetInput(newText);
        InputArea.CursorCol = start + replacement.Length;
    }

    // ── Agent 运行状态 ──

    /// <summary>Agent 正在思考/生成（用于旋转动画指示）</summary>
    public bool Running { get; set; }

    /// <summary>最近修改的文件列表</summary>
    public List<string> ModifiedFiles { get; set; } = [];

    /// <summary>最近访问的文件列表</summary>
    public List<string> RecentFiles { get; set; } = [];

    // ── 增强输入操作 ──

    /// <summary>光标上移一行（多行输入）</summary>
    public void InputMoveUp()
    {
        if (InputArea.CursorRow > 0)
        {
            InputArea.CursorRow--;
            InputArea.CursorCol = Math.Min(InputArea.CursorCol,
                InputArea.Lines[InputArea.CursorRow].Length);
        }

        MarkDirty();
    }

    /// <summary>光标下移一行（多行输入）</summary>
    public void InputMoveDown()
    {
        if (InputArea.CursorRow < InputArea.Lines.Count - 1)
        {
            InputArea.CursorRow++;
            InputArea.CursorCol = Math.Min(InputArea.CursorCol,
                InputArea.Lines[InputArea.CursorRow].Length);
        }

        MarkDirty();
    }

    /// <summary>插入换行</summary>
    public void InputNewLine()
    {
        InputArea.OnKey(new ConsoleKeyInfo('\n', ConsoleKey.Enter, false, false, false));
        MarkDirty();
    }

    /// <summary>删除光标前一个词</summary>
    public void InputDeleteWordLeft()
    {
        InputArea.OnKey(new ConsoleKeyInfo('\b', ConsoleKey.Backspace,
            false, false, true));
        MarkDirty();
    }

    /// <summary>删除光标后一个词</summary>
    public void InputDeleteWordRight()
    {
        InputArea.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Delete,
            false, false, true));
        MarkDirty();
    }

    /// <summary>光标左移一个词</summary>
    public void InputWordLeft()
    {
        // Ctrl+Left: 跳过空格，再跳过单词字符
        var line = InputArea.Lines[InputArea.CursorRow];
        int pos = InputArea.CursorCol;
        while (pos > 0 && line[pos - 1] == ' ') pos--;
        while (pos > 0 && line[pos - 1] != ' ') pos--;
        InputArea.CursorCol = pos;
        MarkDirty();
    }

    /// <summary>光标右移一个词</summary>
    public void InputWordRight()
    {
        var line = InputArea.Lines[InputArea.CursorRow];
        int pos = InputArea.CursorCol;
        while (pos < line.Length && line[pos] != ' ') pos++;
        while (pos < line.Length && line[pos] == ' ') pos++;
        InputArea.CursorCol = pos;
        MarkDirty();
    }

    /// <summary>在光标位置插入字符</summary>
    public void InputInsert(char ch)
    {
        InputArea.OnKey(new ConsoleKeyInfo(ch, (ConsoleKey)ch, false, false, false));
        MarkDirty();
    }

    // ── 高级操作 ──

    /// <summary>添加工具调用进度（流式输出期间的占位）</summary>
    public void AddToolProgress(string toolName, string brief)
    {
        var renderer = ToolRendererFactory.Get(toolName);
        string label = $"  {renderer.FormatHeader(brief)}";
        AddSystemMsg(label);
        _toolOutputLineCount = 0;
    }

    /// <summary>同步 Todo 数据到侧栏</summary>
    public void SyncTodos()
    {
        RefreshSidePanel();
    }

    /// <summary>同步主题配色</summary>
    public void SyncTheme()
    {
        // 从环境变量重新读取显示风格（设置变更后生效）
        ChatDisplayStyle = Config.Instance.ChatDisplayStyle;
        // 主题配色已在 ThemeConfig 中管理，此方法为兼容旧 API
    }

    /// <summary>刷新主题样式</summary>
    public void RefreshTheme()
    {
        MarkDirty();
    }

    /// <summary>更新 Token 显示（完整版：含用量、限额、成本、延迟、速度）</summary>
    public void UpdateTokenDisplayFull(int promptTokens, int completionTokens,
        double? estimatedCost, int contextTokens, int maxContext,
        double lastLatencyMs, double lastTokensPerSec)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"📊 {FormatNum(promptTokens + completionTokens)}");
        if (maxContext > 0)
            sb.Append($"/{FormatNum(maxContext)}");
        sb.Append(" 词元");
        if (estimatedCost.HasValue)
            sb.Append($" · ¥{estimatedCost.Value * 7.25:F2}");
        if (lastLatencyMs > 0)
            sb.Append($" · {lastLatencyMs / 1000:F1}s");
        StatusRight = sb.ToString();
        MarkDirty();
    }

    // ── 对话框快捷方法 ──

    /// <summary>显示选择菜单对话框，返回选中索引（-1=取消）</summary>
    public int ShowMenu(string title, List<string> choices)
    {
        using var evt = new ManualResetEventSlim(false);
        var win = TuiDialog.Select(title, choices,
            onSelect: _ => evt.Set(),
            onCancel: () => evt.Set());
        ShowWindow(win);
        RenderWait(evt);
        return win.Result is int idx ? idx : -1;
    }

    /// <summary>显示行内权限确认 —— 在聊天流中嵌入交互式权限控件</summary>
    public int ShowInlinePermission(string toolName, string argsSummary, string argsDetail, bool isDangerous)
    {
        using var evt = new ManualResetEventSlim(false);
        int resolved = -1;

        var perm = new InlinePermission
        {
            ToolName = toolName,
            ArgsSummary = argsSummary,
            ArgsDetail = argsDetail,
            IsDangerous = isDangerous,
            Width = ChatList.Width - 2,
        };
        perm.OnResolved = r => { resolved = r; evt.Set(); };

        lock (_chatLock)
        {
            ChatList.AddItem(perm);
            perm.Focused = true;
        }
        MarkDirty();

        RenderWait(evt);
        return resolved;
    }

    /// <summary>渲染循环等待对话框关闭</summary>
    private void RenderWait(ManualResetEventSlim evt)
    {
        while (!evt.IsSet)
        {
            Manager?.Render();
            // Read input with short timeout to keep rendering
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                OnKey(key);
            }
            else
            {
                Thread.Sleep(30);
            }
        }

        Manager?.Render();
    }

    // ── 工具 ──

    private static string TruncateStatus(string text, int maxVw)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return TuiHelper.DisplayWidth(text) <= maxVw
            ? text
            : TuiHelper.TruncateByWidth(text, maxVw);
    }

    /// <summary>数字自动换算 K/M（如 128000→128K, 1000000→1M）</summary>
    private static string FormatNum(int n) => n switch
    {
        >= 1_000_000 => $"{n / 1_000_000.0:0.#}M",
        >= 1_000 => $"{n / 1_000.0:0.#}K",
        _ => n.ToString()
    };
}

/// <summary>聊天消息数据结构</summary>
public class ChatMsg
{
    public string Role { get; set; } = "system";
    public string Content { get; set; } = "";
    public string? SessionId { get; set; }
    public DateTime Time { get; set; } = DateTime.Now;
    public int TokenCount { get; set; }
    public bool Streaming { get; set; }
    /// <summary>内容横向居中（仅欢迎消息使用）</summary>
    public bool Centered { get; set; }
}