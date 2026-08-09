using System.Collections.Concurrent;
using System.Text;
using CoreCoderSharp.Terminal;
using CoreCoderSharp.Tools;
using CoreCoderSharp.UI.Controls;

namespace CoreCoderSharp.UI;

/// <summary>槽位状态</summary>
public enum SlotState : byte { Idle = 0, Working = 1, WaitingPerm = 2, Error = 3 }

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

        // 恢复分隔线宽度
        InputTopBorder.Width = TW;
        InputBotBorder.Width = TW;

        // 通知所有浮层窗口
        foreach (var win in Windows)
            win.OnResize(newW, newH);
    }

    private void BuildLayout()
    {
        RootView.Clear();
        RootView = new TuiVBox { Width = TW, Height = TH };

        // ── 标题栏（顶行）──
        TitleBar = new TuiTitleBar
        {
            Width = TW, Height = 1,
            Bg = TuiTheme.Current.StatusBarBg, Fg = TuiTheme.Current.StatusBarFg
        };
        RootView.Add(TitleBar);

        // ── 中间区域：ChatList + SidePanel（HBox 水平排列）──
        int chatH = Math.Max(1, TH - 1 - 1 - 3 - 1 - 1); // TH - title(1) - topBorder(1) - input(3) - botBorder(1) - status(1)
        var middleHBox = new TuiHBox { Width = TW, Height = chatH };

        ChatList = new TuiListView
        {
            Width = TW,  // 初始全宽，侧栏打开时 Render 会缩小
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
            Bg = TuiTheme.Current.WindowBg,
            BorderColor = TuiTheme.Current.SeparatorFg,
        };
        middleHBox.Add(SidePanel);

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

        // ── 输入区上分隔线 ──
        InputTopBorder = new TuiSeparator
        {
            Width = TW, Height = 1,
            LineChar = "━", LineColor = TuiTheme.Current.SeparatorFg
        };
        RootView.Add(InputTopBorder);

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
            HintText = "Enter 发送 · Tab 补全 · ↑↓ 历史 · Ctrl+H 帮助 · Ctrl+Q 退出"
        };
        RootView.Add(StatusBar);

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

        var item = new TuiListItem(role, content, ChatList.Width - 2, continuation, plainText);
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
            Lines = [
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

        // ── 同步底部状态栏数据 ──
        StatusBar.Width = TW;
        StatusBar.Bg = TuiTheme.Current.StatusBarBg;
        StatusBar.Fg = TuiTheme.Current.StatusBarFg;
        StatusBar.ActiveSlotIndex = ActiveSlotIndex;
        StatusBar.AgentBusy = AgentBusy;
        StatusBar.RightText = StatusRight;
        Array.Copy(SlotStates, StatusBar.SlotStates, 10);

        // ── 动态尺寸 ──
        int panelW = SidePanelVisible ? Math.Min(30, TW / 3) : 0;
        int inputH = Math.Max(1, Math.Min(10, InputArea.Lines.Count + 1));
        int chatH = Math.Max(1, TH - 1 - 1 - inputH - 1 - 1); // TH - title - topBorder - input - botBorder - status

        // ── 输入区 ──
        InputArea.Width = TW;
        InputArea.Height = inputH;

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

    /// <summary>Ctrl+Shift+F1：弹出主题选择对话框</summary>
    private void ShowThemePicker()
    {
        var names = new List<string>(TuiTheme.PresetNames);
        var win = Controls.TuiDialog.Select("选择主题", names, idx =>
        {
            if (idx >= 0 && idx < TuiTheme.Presets.Length)
            {
                TuiTheme.Apply(TuiTheme.Presets[idx], idx);
                ApplyThemeToScreen();
                ShowToast($"主题已切换：{TuiTheme.PresetNames[idx]}");
            }
        });
        ShowWindow(win);
    }

    /// <summary>Ctrl+Shift+F2：直接轮转到下一个主题</summary>
    private void CycleThemeDirect()
    {
        var name = TuiTheme.CycleNext();
        ApplyThemeToScreen();
        ShowToast($"主题：{name}");
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

        // ── 3. 聊天自身处理（全局快捷键 + 导航 + 提交 + 输入编辑）──
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
                return true; // 已处理，不再向下传递
            case ConsoleKey.LeftArrow: case ConsoleKey.RightArrow:
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
                    Manager?.PushScreen(new EditorScreen()); return true;
                case ConsoleKey.T:
                case ConsoleKey.O:
                    Manager?.PushScreen(new SettingsScreen()); return true;
                case ConsoleKey.B:
                    SidePanelVisible = !SidePanelVisible;
                    if (SidePanelVisible)
                        RefreshSidePanel();
                    return true;
                case ConsoleKey.R:
                    Manager?.Exit();
                    var query = UxHelper.Ask("搜索对话历史");
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

        // ── Ctrl+Shift+F1 主题选择 / Ctrl+Shift+F2 直接轮转 ──
        if (ctrl && shift)
        {
            switch (key.Key)
            {
                case ConsoleKey.F1:
                    ShowThemePicker(); return true;
                case ConsoleKey.F2:
                    CycleThemeDirect(); return true;
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
            ChatScrollUp(Math.Max(1, (TTY.Rows - 10) / 2));
            return true;
        }
        if (key.Key == ConsoleKey.PageDown)
        {
            ChatScrollDown(Math.Max(1, (TTY.Rows - 10) / 2));
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
        if (!InputArea.Focused) return base.OnKey(key);

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

        // 其他全部委托给 InputArea
        return InputArea.OnKey(key);
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
        catch { /* 权限不足等错误静默忽略 */ }

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
        AddSystemMsg($"  ⚙ {toolName}({brief})");
    }

    /// <summary>同步 Todo 数据到侧栏</summary>
    public void SyncTodos()
    {
        RefreshSidePanel();
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
