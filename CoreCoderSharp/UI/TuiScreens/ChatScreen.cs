using System.Collections.Concurrent;
using System.Text;
using CoreCoderSharp.Terminal;
using CoreCoderSharp.Tools;
using CoreCoderSharp.UI.Controls;

namespace CoreCoderSharp.UI;

/// <summary>槽位状态</summary>
public enum SlotState : byte { Idle = 0, Working = 1, WaitingPerm = 2, Error = 3 }

/// <summary>侧栏面板标签</summary>
public enum PanelTab { Off, Todo, Files, Locks, MCP }

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

    /// <summary>状态栏（顶行）</summary>
    public TuiLabel StatusBar { get; private set; } = null!;

    /// <summary>聊天列表（TuiListView → TuiMarkdown 项）</summary>
    public TuiListView ChatList { get; private set; } = null!;

    /// <summary>多行输入区</summary>
    public TuiTextArea InputArea { get; private set; } = null!;

    /// <summary>建议下拉面板</summary>
    public TuiVBox SuggestPanel { get; private set; } = null!;

    /// <summary>右侧信息面板</summary>
    public TuiVBox SidePanel { get; private set; } = null!;

    // ── 状态 ──

    public string StatusText { get; set; } = "";

    /// <summary>建议列表项</summary>
    public List<string> Suggestions { get; set; } = [];
    public int SuggestIndex { get; set; }

    /// <summary>侧栏是否可见</summary>
    public bool SidePanelVisible { get; set; }

    /// <summary>侧栏内容</summary>
    public List<string> SidePanelContent { get; set; } = [];

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

    /// <summary>状态栏左侧（模型名、git 分支等）</summary>
    public string StatusLeft { get; set; } = "";

    /// <summary>状态栏右侧（Token 信息）</summary>
    public string StatusRight { get; set; } = "";

    /// <summary>Git 分支名</summary>
    public string? GitBranch { get; set; }

    /// <summary>侧栏活跃标签</summary>
    public PanelTab ActivePanel { get; set; }

    /// <summary>建议面板是否活跃</summary>
    public bool SuggestActive { get; set; }

    /// <summary>Agent 正在执行（显示旋转指示）</summary>
    public bool AgentBusy { get; set; }

    public ChatScreen()
    {
        Name = "chat";
    }

    // ── 生命周期 ──

    public override void Activate()
    {
        base.Activate();
        BuildLayout();
    }

    /// <summary>终端尺寸变化——重建完整布局，保留输入状态</summary>
    public override void OnResize(int newW, int newH)
    {
        var inputText = InputArea?.Text ?? "";
        int cursorRow = InputArea?.CursorRow ?? 0;
        int cursorCol = InputArea?.CursorCol ?? 0;

        TW = newW;
        TH = newH;

        // 重建整个控件树
        BuildLayout();

        // 恢复输入状态
        if (!string.IsNullOrEmpty(inputText))
        {
            InputArea.Text = inputText;
            InputArea.CursorRow = Math.Min(cursorRow, InputArea.Lines.Count - 1);
            InputArea.CursorCol = Math.Min(cursorCol, InputArea.Lines[InputArea.CursorRow].Length);
        }

        // 通知所有浮层窗口
        foreach (var win in Windows)
            win.OnResize(newW, newH);
    }

    private void BuildLayout()
    {
        RootView.Clear();
        RootView = new TuiVBox { Width = TW, Height = TH };

        // ── 状态栏 ──
        StatusBar = new TuiLabel(StatusText.Length > 0 ? StatusText : "WayCoder")
        {
            Width = TW, Height = 1, Bg = 44, Fg = 37
        };
        RootView.Add(StatusBar);

        // ── 聊天列表（TuiListView 容纳 TuiMarkdown）──
        ChatList = new TuiListView
        {
            Width = TW,
            Height = TH - 4,
            IsAutoScrollToEnd = true,
            ItemSpacing = 1
        };
        RootView.Add(ChatList);

        // ── 建议面板（覆盖在聊天区底部）──
        SuggestPanel = new TuiVBox
        {
            Width = Math.Min(TW, 60),
            Height = 0,
            Visible = false,
            Bg = 7
        };
        // 建议面板作为独立元素添加，在 Render 中定位
        RootView.Add(SuggestPanel);

        // ── 输入区 ──
        InputArea = new TuiTextArea
        {
            Width = TW,
            Height = 3,
            Focused = true,
            Placeholder = "输入消息… (Enter 发送, Ctrl+Enter 换行)",
            ShowLineNumbers = false
        };
        InputArea.OnSubmit = text =>
        {
            if (!string.IsNullOrWhiteSpace(text))
                OnSubmit?.Invoke(text);
        };
        RootView.Add(InputArea);

        RootView.Layout();
    }

    // ── 消息管理 ──

    /// <summary>添加一条消息到聊天列表。system/tool 消息使用纯文本模式避免 Markdown 行合并，连续同角色自动续接。</summary>
    public void AddMessage(string content, string role = "assistant")
    {
        bool continuation = false;
        bool plainText = role is "system" or "tool";
        if (plainText)
        {
            var last = ChatList.GetItem(ChatList.ItemCount - 1) as TuiListItem;
            if (last != null && last.Role == role)
                continuation = true;
        }

        var item = new TuiListItem(role, content, ChatList.Width - 2, continuation)
        {
            IsPlainText = plainText
        };
        if (!continuation)
            item.SetTime(DateTime.Now);
        ChatList.AddItem(item);
        MarkDirty();
    }

    /// <summary>追加文本到最后一条消息（流式输出）</summary>
    public void AppendToLast(string delta)
    {
        var last = ChatList.GetItem(ChatList.ItemCount - 1) as TuiListItem;
        if (last != null)
        {
            last.AppendContent(delta);
            ChatList.ReLayout();
            if (ChatList.IsAutoScrollToEnd)
                ChatList.ScrollToBottom();
        }
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

    /// <summary>添加系统消息</summary>
    public void AddSystemMsg(string content)
    {
        var msg = new ChatMsg { Role = "system", Content = content };
        ChatMessages.Add(msg);
        AddMessage(content, "system");
    }

    /// <summary>开始 Agent 流式回复（占位消息）</summary>
    public void StartAgentMsg()
    {
        var msg = new ChatMsg { Role = "agent", Content = "", Streaming = true };
        ChatMessages.Add(msg);
        // 在 ChatList 中添加空白占位项
        var item = new TuiListItem("agent", "", ChatList.Width - 2);
        item.SetTime(DateTime.Now);
        ChatList.AddItem(item);
        MarkDirty();
    }

    /// <summary>追加 token 到流式消息</summary>
    public void AppendToken(string delta)
    {
        if (ChatMessages.Count == 0) return;
        var last = ChatMessages[^1];
        if (!last.Streaming) return;
        last.Content += delta;
        AppendToLast(delta);
    }

    /// <summary>完成 Agent 流式回复</summary>
    public void FinishAgentMsg()
    {
        if (ChatMessages.Count == 0) return;
        var last = ChatMessages[^1];
        last.Streaming = false;
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
            if (!string.IsNullOrEmpty(text))
            {
                InputArea.InsertText(text);
                MarkDirty();
            }
        }
        catch { /* 忽略粘贴错误 */ }
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
        InputArea.HandleKey(new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));
        MarkDirty();
    }

    /// <summary>输入区删除</summary>
    public void InputDelete()
    {
        InputArea.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.Delete, false, false, false));
        MarkDirty();
    }

    /// <summary>输入区左移光标</summary>
    public void InputCursorLeft()
    {
        InputArea.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false));
        MarkDirty();
    }

    /// <summary>输入区右移光标</summary>
    public void InputCursorRight()
    {
        InputArea.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false));
        MarkDirty();
    }

    /// <summary>输入区移到行首</summary>
    public void InputHome()
    {
        InputArea.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false));
        MarkDirty();
    }

    /// <summary>输入区移到行尾</summary>
    public void InputEnd()
    {
        InputArea.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, false));
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

    public void UpdateSidePanel(List<string> content)
    {
        SidePanelVisible = content.Count > 0;
        SidePanelContent = content;
    }

    // ── 渲染 ──

    public override void Render(StringBuilder sb)
    {
        // 构建状态栏文本
        var left = StatusLeft.Length > 0 ? $" {StatusLeft}" : " WayCoder";
        var right = StatusRight.Length > 0 ? StatusRight : "";
        StatusBar.Text = TruncateStatus(left, TW - TuiHelper.DisplayWidth(right) - 2) +
                         (right.Length > 0 ? new string(' ', Math.Max(1, TW - TuiHelper.DisplayWidth(left) - TuiHelper.DisplayWidth(right) - 2)) + right : "");
        StatusBar.Width = TW;

        // 布局计算
        int panelW = SidePanelVisible ? Math.Min(30, TW / 3) : 0;
        int chatW = TW - panelW;
        int inputH = Math.Max(1, Math.Min(10, InputArea.Lines.Count + 1));

        ChatList.Width = chatW;
        ChatList.Height = Math.Max(1, TH - inputH - 1);
        InputArea.Width = TW;
        InputArea.Height = inputH;
        InputArea.Y = TH - inputH;

        // 建议面板定位（浮在聊天区底部）
        if (SuggestPanel.Visible)
        {
            SuggestPanel.X = 0;
            SuggestPanel.Y = TH - inputH - SuggestPanel.Height - 1;
        }

        // 侧栏
        if (SidePanelVisible && panelW > 0)
        {
            SidePanel.X = chatW;
            SidePanel.Y = 1;
            SidePanel.Width = panelW;
            SidePanel.Height = TH - inputH - 1;
            SidePanel.Clear();
            foreach (var line in SidePanelContent)
                SidePanel.Add(new TuiLabel(line) { Width = panelW - 1, Height = 1, Fg = 37 });
            SidePanel.Bg = 0;
            if (!RootView.Children.Contains(SidePanel))
                RootView.Add(SidePanel);
        }
        else
        {
            RootView.Children.Remove(SidePanel);
        }

        RootView.Layout();
        base.Render(sb);
    }

    private string BuildSlotBar()
    {
        if (SlotStates.All(s => s == SlotState.Idle)) return "";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 10; i++)
        {
            int fg = SlotStates[i] switch
            {
                SlotState.Working => 32,      // Green
                SlotState.WaitingPerm => 33,  // Yellow
                SlotState.Error => 31,        // Red
                _ => 90,                      // Dim
            };
            bool isActive = i == ActiveSlotIndex;
            if (isActive) sb.Append($"\x1b[1;{fg}m{i + 1}\x1b[0m");
            else sb.Append($"\x1b[{fg}m{i + 1}\x1b[0m");
            if (i < 9) sb.Append(' ');
        }
        return sb.ToString();
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
    /// <summary>回调：搜索历史（Program.cs 注入，参数=查询字符串）</summary>
    public Action<string>? OnSearchHistory;

    /// <summary>显示退出确认对话框</summary>
    private void ShowExitConfirmDialog()
    {
        var win = Controls.TuiDialog.Confirm("退出 WayCoder", "确定要退出道码吗？", confirmed =>
        {
            if (confirmed)
                PendingSubmissions.Enqueue("\x1b"); // 特殊标记：退出请求
        });
        ShowWindow(win);
    }

    public override bool HandleKey(ConsoleKeyInfo key)
    {
        bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);
        bool shift = key.Modifiers.HasFlag(ConsoleModifiers.Shift);

        // ── 1. 建议面板可见 → 建议导航 ──
        if (SuggestActive)
        {
            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    HideSuggestions(); return true;
                case ConsoleKey.UpArrow:
                    SuggestIndex = Math.Max(0, SuggestIndex - 1);
                    UpdateSuggestions(Suggestions, SuggestIndex); return true;
                case ConsoleKey.DownArrow:
                    SuggestIndex = Math.Min(Suggestions.Count - 1, SuggestIndex + 1);
                    UpdateSuggestions(Suggestions, SuggestIndex); return true;
                case ConsoleKey.PageUp:
                    SuggestIndex = Math.Max(0, SuggestIndex - 5);
                    UpdateSuggestions(Suggestions, SuggestIndex); return true;
                case ConsoleKey.PageDown:
                    SuggestIndex = Math.Min(Suggestions.Count - 1, SuggestIndex + 5);
                    UpdateSuggestions(Suggestions, SuggestIndex); return true;
                case ConsoleKey.Home:
                    SuggestIndex = 0;
                    UpdateSuggestions(Suggestions, SuggestIndex); return true;
                case ConsoleKey.End:
                    SuggestIndex = Suggestions.Count - 1;
                    UpdateSuggestions(Suggestions, SuggestIndex); return true;
                case ConsoleKey.Enter: case ConsoleKey.Tab:
                    AcceptSuggestion(); return true;
                case ConsoleKey.Backspace:
                    InputBackspace();
                    UpdateSuggestions(Suggestions, SuggestIndex);
                    break; // 继续处理（可能输入更多字符）
                case ConsoleKey.LeftArrow: case ConsoleKey.RightArrow:
                    SuggestActive = false;
                    break; // 继续处理，让光标移动生效
            }
        }

        // ── 2. 模态窗口优先 ──
        if (HasModal) return base.HandleKey(key);

        // ── 3. 屏幕级快捷键（Ctrl+组合，不依赖输入焦点）──
        if (ctrl)
        {
            switch (key.Key)
            {
                case ConsoleKey.E:
                    Manager?.PushScreen(new EditorScreen()); return true;
                case ConsoleKey.T:
                case ConsoleKey.O:
                    Manager?.PushScreen(new SettingsScreen()); return true;
                case ConsoleKey.B:
                    ActivePanel = ActivePanel switch
                    {
                        PanelTab.Off => PanelTab.Todo,
                        PanelTab.Todo => PanelTab.Files,
                        PanelTab.Files => PanelTab.Locks,
                        PanelTab.Locks => PanelTab.MCP,
                        _ => PanelTab.Off,
                    };
                    if (ActivePanel == PanelTab.Files)
                        ModifiedFiles = EditFileTool.ChangedFiles.ToList();
                    return true;
                case ConsoleKey.R:
                    Manager?.Exit();
                    var query = TuiPrompt.Ask("搜索对话历史");
                    Manager?.Enter();
                    Manager?.PushScreen(this);
                    if (!string.IsNullOrWhiteSpace(query))
                        OnSearchHistory?.Invoke("/history " + query);
                    return true;
                case ConsoleKey.M:
                    OnCycleModel?.Invoke(); return true;
                case ConsoleKey.H:
                    OnShowHelp?.Invoke(); return true;
                case ConsoleKey.Q:
                    ShowExitConfirmDialog(); return true;
                // 聊天滚动
                case ConsoleKey.Home:   ChatScrollTop(); return true;
                case ConsoleKey.End:    ChatScrollBottom(); return true;
                case ConsoleKey.UpArrow:   ChatScrollUp(3); return true;
                case ConsoleKey.DownArrow: ChatScrollDown(3); return true;
            }
        }

        // ── 4. 聊天滚动（不依赖输入焦点）──
        if (key.Key == ConsoleKey.PageUp)
            { ChatScrollUp(Math.Max(1, (TTY.Rows - 10) / 2)); return true; }
        if (key.Key == ConsoleKey.PageDown)
            { ChatScrollDown(Math.Max(1, (TTY.Rows - 10) / 2)); return true; }

        // ── 5. Enter 提交消息 ──
        if (key.Key == ConsoleKey.Enter && !ctrl && !shift)
        {
            SuggestActive = false;
            var input = GetInputText();
            if (string.IsNullOrWhiteSpace(input)) return true;
            AddUserMsg(input);
            if (InputHistory.Count == 0 || InputHistory[^1] != input)
                InputHistory.Add(input);
            if (InputHistory.Count > 200) InputHistory.RemoveAt(0);
            HistoryIdx = -1;
            SetInput("");
            PendingSubmissions.Enqueue(input);
            return true;
        }

        // ── 6. Escape 空输入 → 退出确认 ──
        if (key.Key == ConsoleKey.Escape && string.IsNullOrEmpty(GetInputText()))
        {
            ShowExitConfirmDialog();
            return true;
        }

        // ── 7. 输入区按键 ──
        if (InputArea.Focused)
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
                { InputNewLine(); return true; }

            // ↑↓ — 历史浏览 / 多行移动 / 空输入滚动
            if (key.Key == ConsoleKey.UpArrow)
            {
                if (InputArea.Lines.Count == 1)
                {
                    if (string.IsNullOrEmpty(GetInputText()))
                        { ChatScrollUp(3); return true; }
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
            if (key.Key == ConsoleKey.DownArrow)
            {
                if (InputArea.Lines.Count == 1)
                {
                    if (string.IsNullOrEmpty(GetInputText()))
                        { ChatScrollDown(3); return true; }
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

            // Tab — 路径补全或 4 空格
            if (key.Key == ConsoleKey.Tab)
            {
                // 简单版：插入 4 空格（路径补全后续增强）
                for (int t = 0; t < 4; t++) InputInsert(' ');
                return true;
            }

            // 其他全部委托给 InputArea（← → Home End Backspace Delete Ctrl+组合 可打印字符）
            return InputArea.HandleKey(key);
        }

        // ── 8. 兜底 ──
        return base.HandleKey(key);
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
        InputArea.HandleKey(new ConsoleKeyInfo('\n', ConsoleKey.Enter, false, false, false));
        MarkDirty();
    }

    /// <summary>删除光标前一个词</summary>
    public void InputDeleteWordLeft()
    {
        InputArea.HandleKey(new ConsoleKeyInfo('\b', ConsoleKey.Backspace,
            false, false, true));
        MarkDirty();
    }

    /// <summary>删除光标后一个词</summary>
    public void InputDeleteWordRight()
    {
        InputArea.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.Delete,
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
        InputArea.HandleKey(new ConsoleKeyInfo(ch, (ConsoleKey)ch, false, false, false));
        MarkDirty();
    }

    // ── 高级操作 ──

    /// <summary>添加工具调用进度（流式输出期间的占位）</summary>
    public void AddToolProgress(string toolName, string brief)
    {
        AddSystemMsg($"  ⚙ {toolName}({brief})");
    }

    /// <summary>同步 Todo 数据到侧栏</summary>
    public void SyncTodos()
    {
        var items = Tools.TodoTool.Items;
        if (items.Count > 0)
        {
            var completed = items.Count(i => i.Status == "completed");
            var lines = new List<string>
            {
                $"📋 任务 ({completed}/{items.Count})",
                new string('─', 28),
            };
            foreach (var item in items.OrderBy(i => i.Id).Take(20))
            {
                var icon = item.Status switch
                {
                    "completed" => "✅",
                    "in_progress" => "🔄",
                    "cancelled" => "❌",
                    _ => "⏳",
                };
                var title = item.Title.Length > 22 ? item.Title[..19] + "..." : item.Title;
                lines.Add($"{icon} {title}");
            }
            UpdateSidePanel(lines);
        }
    }

    /// <summary>同步主题配色</summary>
    public void SyncTheme()
    {
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
        sb.Append($"📊 {promptTokens + completionTokens}");
        if (maxContext > 0)
            sb.Append($"/{maxContext}");
        sb.Append(" tokens");
        if (estimatedCost.HasValue)
            sb.Append($" · ${estimatedCost.Value:F4}");
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

    /// <summary>显示行内权限确认对话框，返回选中索引（0=允许, 1=全部允许, -1=拒绝）</summary>
    public int ShowInlinePermission(string title, string content, List<string> choices)
    {
        using var evt = new ManualResetEventSlim(false);
        var win = TuiDialog.Permission(title, content,
            onResult: _ => evt.Set());
        ShowWindow(win);
        RenderWait(evt);
        return win.Result switch
        {
            TuiDialog.DialogResult.Yes => 0,
            TuiDialog.DialogResult.Ok => 1,
            _ => -1,
        };
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
                HandleKey(key);
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
            ? text : TuiHelper.TruncateByWidth(text, maxVw);
    }
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
}
