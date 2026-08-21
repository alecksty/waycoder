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
    // ── 粘贴 ──

    public async Task PasteAsync()
    {
        try
        {
            // 内部剪贴板优先（Ctrl+C/X 刚复制的，保证复制→粘贴一致；CLI 无 GUI 剪贴板会话时系统读到残留）
            var text = WayCoder.UI.Tui.Controls.TuiEditBase.InternalClipboard;
            if (string.IsNullOrEmpty(text))
                text = await ClipboardHelper.GetTextAsync();
            if (string.IsNullOrEmpty(text)) return;

            // 粘贴确认：超长(>500字符)或多行(>3行)时弹出确认
            var lines = text.Replace("\r\n", "\n").Split('\n');
            if (text.Length > 500 || lines.Length > 3)
            {
                var preview = text.Length > 200 ? ContextManager.TruncateByRunes(text, 200) + "..." : text;
                using var evt = new ManualResetEventSlim(false);
                bool confirmed = false;
                ShowWindow(TuiDialog.Confirm("粘贴确认",
                    $"将粘贴 {lines.Length} 行 / {text.Length} 字符:\n{preview}",
                    result =>
                    {
                        confirmed = result;
                        evt.Set();
                    }));
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

        // 模态对话框（api-key 输入/提问等）打开时：粘贴到对话框的焦点输入控件，
        // 否则 bracketed paste 内容只进主输入框，对话框输入框粘贴花屏/丢失
        if (HasModal && FocusedWindow?.RootView != null)
        {
            var focused = FocusedWindow.RootView.FindFocused();
            if (focused is TuiEditBase editInput)
            {
                editInput.PasteFromExternal(normalized);
                MarkDirty();
                return;
            }
        }

        var lines = normalized.Split('\n');

        // 粘贴确认：超长(>500字符)或多行(>3行)时弹出确认
        if (text.Length > 500 || lines.Length > 3)
        {
            var preview = text.Length > 200 ? ContextManager.TruncateByRunes(text, 200) + "..." : text;
            using var evt = new ManualResetEventSlim(false);
            bool confirmed = false;
            ShowWindow(TuiDialog.Confirm("粘贴确认",
                $"将粘贴 {lines.Length} 行 / {text.Length} 字符:\n{preview}",
                result =>
                {
                    confirmed = result;
                    evt.Set();
                }));
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
        MarkDirty(); // 建议面板显隐/高度变化 → 强制重绘背景，清除残影
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
                Bg = i == selectedIdx ? AnsiColors.BgWhite : 0 // 选中=白底黑字（7 是反白转义，浅色主题下会错乱）
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
        MarkDirty(); // 浮层隐藏 → 清除残影
    }

    // ── 侧栏 ──

    /// <summary>
    /// 切换侧栏（Ctrl+B）。
    /// 侧栏宽度变化会连带改聊天区宽度，所以必须走一遍 resize 路径：重排布局 + 按新宽重灌消息，
    /// 再整屏刷新。此前只翻了个标记位——`TuiManager.OnKey` 只置 manager 级 `IsDirty`，
    /// 下一帧是增量渲染，`TuiView.OnRender` 会跳过没标脏的 SidePanel 叶子，
    /// 于是侧栏得等到用户手动改一次终端尺寸（走 `_needsFullRefresh` 全量重绘）才「突然」冒出来。
    /// </summary>
    public void ToggleSidePanel()
    {
        SidePanelVisible = !SidePanelVisible;
        RefreshSidePanel();   // 关闭时也刷：下次打开先显示的是最新数据而不是上次的残影
        OnResize(TW, TH);
        TuiManager.RequestFullRefresh();
    }

    /// <summary>
    /// 每帧同步侧栏：数据指纹变了才重建分区并标脏。
    /// 逐帧重建分区在 30ms 渲染循环里是白烧 GC，指纹比对是纯计数/状态拼串，代价可忽略。
    /// 弹窗/对话框打开时不刷新（侧栏被遮罩盖住，且避免与弹窗渲染竞争）；关闭后恢复。
    /// </summary>
    public void SyncSidePanel()
    {
        if (!SidePanelVisible) return;
        if (FocusedWindow != null) return; // 弹窗/对话框在场 → 侧栏不刷新
        var stamp = SidePanelStamp();
        if (stamp == _sidePanelStamp) return;
        _sidePanelStamp = stamp;
        RefreshSidePanel();
        SidePanel.Sections = SidePanelSections;
        SidePanel.MarkDirty();   // 叶子控件不标脏，增量渲染这一帧就会跳过它
    }

    private string _sidePanelStamp = "";

    /// <summary>
    /// 侧栏数据指纹 —— 覆盖侧栏显示的每一项，任何一项变化都要让串变化，
    /// 否则界面会停在旧值上（这正是「侧栏是个摆设」的根源：以前只有开侧栏那一刻刷一次）。
    /// </summary>
    private string SidePanelStamp()
    {
        var sb = new StringBuilder();
        sb.Append(StatusLeft).Append('|').Append(StatusRight).Append('|')
          .Append(WorkModeManager.CurrentMode).Append('|')
          .Append(ActiveSlotIndex).Append(AgentBusy ? 'B' : '-').Append('|')
          .Append(_contextPercent?.ToString("F0") ?? "-").Append('|')
          .Append(_currentToolName ?? "-").Append('|')
          .Append(GitBranch).Append('|')
          .Append(ModifiedFiles.Count).Append('|')
          .Append(McpManager.Servers.Count).Append(':').Append(McpManager.DiscoveredTools.Count).Append('|')
          .Append(LspTool.SupportedServers.Count).Append('|');
        foreach (var s in McpManager.Servers) sb.Append((int)s.Status).Append(s.ToolCount).Append(',');
        sb.Append('|');
        foreach (var t in TodoTool.Items) sb.Append(t.Id).Append(t.Status.Length > 0 ? t.Status[0] : '?').Append(',');
        sb.Append("|sess:").Append(GetSessionList().Count).Append(':').Append(CurrentSessionId).Append('|');
        return sb.ToString();
    }

    /// <summary>刷新侧栏所有分区内容</summary>
    public void RefreshSidePanel()
    {
        var sections = new List<PanelSection>();

        // ── 会话区（最近会话记录，按槽位隔离；当前会话 ✓ 高亮）──
        var sessionList = GetSessionList();
        var sessionLines = new List<string>();
        if (sessionList.Count == 0)
        {
            sessionLines.Add("  (无历史会话)");
        }
        else
        {
            foreach (var s in sessionList)
            {
                bool isCur = s.Id == CurrentSessionId && !string.IsNullOrEmpty(CurrentSessionId);
                string time = SessionPicker.FormatRelativeTime(s.SavedAt);
                sessionLines.Add(isCur
                    ? $"  ✓ {s.Id} · {time} · {s.MessageCount}条"
                    : $"  {s.Id} · {time} · {s.MessageCount}条");
            }
        }
        sections.Add(new PanelSection { Title = $"⚡ 会话 ({sessionList.Count})", Lines = sessionLines });

        // ── Todo 区 ──
        var todoItems = TodoTool.Items;
        var todoLines = new List<string>();
        if (todoItems.Count == 0)
        {
            todoLines.Add("  (无)");
        }
        else
        {
            // 不再 Take(15) 预截断：截多少由 TuiSidePanel.AllocateHeights 按实际可用高度决定，
            // 高终端能多显示几条，矮终端才折成「… +N」
            foreach (var item in todoItems.OrderBy(i => i.Id))
            {
                var icon = item.Status switch
                {
                    "completed" => "✅",
                    "in_progress" => "🔄",
                    "cancelled" => "❌",
                    _ => "⏳",
                };
                var title = item.Title.Length > 20 ? ContextManager.TruncateByRunes(item.Title, 17) + "..." : item.Title;
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
            foreach (var f in ModifiedFiles)
                fileLines.Add($"  📄 {Path.GetFileName(f)}");
        sections.Add(new PanelSection
        {
            Title = $"📁 文件 ({ModifiedFiles.Count})",
            Lines = fileLines,
        });

        // ── MCP 区 ──
        var mcpLines = new List<string>();
        var mcpServers = McpManager.Servers;
        if (mcpServers.Count == 0)
            mcpLines.Add($"  {McpManager.Info}");
        else
            foreach (var s in mcpServers)
            {
                var mark = s.Status switch
                {
                    McpServerStatus.Connected => "✅",
                    McpServerStatus.Connecting => "⏳",
                    McpServerStatus.Failed => "❌",
                    _ => "❓",
                };
                var mcpLine = $"  {mark} {s.Name} [{s.Transport}] {s.ToolCount} 工具";
                if (s.ResourceCount > 0) mcpLine += $" · {s.ResourceCount} 资源";
                if (s.PromptCount > 0) mcpLine += $" · {s.PromptCount} 提示词";
                mcpLines.Add(mcpLine);
            }

        sections.Add(new PanelSection
        {
            Title = $"🔌 MCP ({McpManager.DiscoveredTools.Count})",
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

    // ── 会话列表缓存 ──

    private List<SessionInfo>? _sessionList;
    private long _sessionListTicks;
    private int _sessionListSlot = -1;
    private const long SessionListCacheMs = 500;

    /// <summary>最近会话列表（500ms 缓存避免每帧读盘；切槽位立即失效）。侧边栏会话区用。</summary>
    private List<SessionInfo> GetSessionList()
    {
        long now = Environment.TickCount64;
        if (_sessionList == null || _sessionListSlot != ActiveSlotIndex
            || now - _sessionListTicks >= SessionListCacheMs)
        {
            _sessionList = SessionManager.ListSessions(limit: 5, offset: 0, slot: ActiveSlotIndex);
            _sessionListSlot = ActiveSlotIndex;
            _sessionListTicks = now;
        }
        return _sessionList;
    }

    // ── 提示栏 ──

    /// <summary>显示提示栏（命令/文件/Shell 等建议列表）</summary>
    public void ShowPromptBar(List<PromptItem> items)
    {
        PromptBar.Items = items;
        PromptBar.SelectedIndex = items.Count > 0 ? 0 : -1;
        PromptBar.Visible = true;
        
        var h = Math.Min(items.Count, PromptBar.MaxVisible);
        // Bg==0 边框模式需 +2（上下边框），Bg>0 填充模式需 +1（底部分隔线）
        var extra = PromptBar.Bg == 0 ? 2 : 1;

        // 高度随条目数（此前恒为 MaxVisible+2=10 行，1 条建议也占 10 行把输入区往下推）
        PromptBar.Height = Math.Max(1, h) + extra;

        // 在 InputArea 上挂 KeyHook：拦截 ↑↓/Enter/Esc/Tab，透传其他键
        InputArea.KeyHook = PromptKeyHook;

        // 提示栏挤压/让出聊天区：标脏聊天列表，强制填充背景+重绘，清掉被覆盖的残留像素
        ChatList.MarkDirty();

        MarkDirty();
    }

    /// <summary>隐藏提示栏</summary>
    public void HidePromptBar()
    {
        // 已隐藏 → 避免每键重复全量重绘
        if (PromptBar is { Visible: false, Height: 0 })
        {
            return;
        }

        PromptBar.Visible = false;
        PromptBar.Height = 0;
        PromptBar.Items.Clear();
        PromptBar.SelectedIndex = -1;
        PromptBar.ViewIndex = 0;
        InputArea.KeyHook = null;
        // 提示栏消失 → 聊天区高度还原：标脏聊天列表强制填充背景+重绘，清掉被提示栏盖住的残留（否则花屏）
        ChatList.MarkDirty();
        MarkDirty();
    }

    /// <summary>挂载在 InputArea 上的按键钩子：↑↓/Enter/Esc 导航提示栏</summary>
    private bool PromptKeyHook(ConsoleKeyInfo key)
    {
        if (!PromptBarVisible)
        {
            return false;
        }

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

    /// <summary>
    /// 注册前缀提示钩子：输入框检测到指定前缀符号时，调用 provider 生成提示项并弹出提示框。
    /// provider 接收前缀后的过滤词（不含前缀），返回提示项列表；返回空列表表示无提示。
    /// 自定义前缀优先于内置前缀，可覆盖内置符号（/ @ ! #）的默认行为。
    /// </summary>
    public void RegisterPrefixHint(char prefix, Func<string, List<PromptItem>> provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _prefixHintHooks[prefix] = provider;
    }

    /// <summary>移除已注册的前缀提示钩子，恢复内置行为。</summary>
    public void UnregisterPrefixHint(char prefix) => _prefixHintHooks.Remove(prefix);

    /// <summary>前缀是否为已知前缀（内置或已注册）。</summary>
    private bool IsKnownPrefix(char c) =>
        BuiltinPrefixes.Contains(c) || _prefixHintHooks.ContainsKey(c);

    /// <summary>构建默认提示列表（命令 + 最近文件 + 快捷操作）</summary>
    private List<PromptItem> BuildDefaultHints()
    {
        var items = new List<PromptItem>();

        // ── 快捷命令 ──
        items.Add(new PromptItem { Kind = EPromptKind.Command, Label = "帮助", Detail = "显示帮助信息", Value = "/help" });
        items.Add(new PromptItem { Kind = EPromptKind.Command, Label = "切换模型", Detail = "轮换 LLM", Value = "/model" });
        items.Add(new PromptItem { Kind = EPromptKind.Command, Label = "/model set <id>", Detail = "设置大模型", Value = "/model set " });
        items.Add(new PromptItem { Kind = EPromptKind.Command, Label = "/model list", Detail = "列出所有模型", Value = "/model list" });
        items.Add(new PromptItem { Kind = EPromptKind.Command, Label = "/model import <path>", Detail = "导入外部配置", Value = "/model import " });
        items.Add(new PromptItem { Kind = EPromptKind.Command, Label = "清空对话", Detail = "重置上下文", Value = "/reset" });
        items.Add(new PromptItem { Kind = EPromptKind.Command, Label = "历史搜索", Detail = "搜索对话记录", Value = "/history " });
        items.Add(new PromptItem { Kind = EPromptKind.Command, Label = "YOLO 模式", Detail = "跳过权限确认", Value = "/perm yolo" });
        items.Add(new PromptItem { Kind = EPromptKind.Command, Label = "/perm ask", Detail = "每次确认模式", Value = "/perm ask" });
        items.Add(new PromptItem { Kind = EPromptKind.Command, Label = "/perm auto", Detail = "首次后自动允许", Value = "/perm auto" });
        items.Add(new PromptItem { Kind = EPromptKind.Command, Label = "Diff 预览", Detail = "切换 diff 预览", Value = "/diff" });

        // ── 文件操作 ──
        items.Add(new PromptItem { Kind = EPromptKind.Slash, Label = "/edit", Detail = "编辑文件", Value = "/edit " });
        items.Add(new PromptItem { Kind = EPromptKind.Slash, Label = "/read", Detail = "读取文件", Value = "/read " });
        items.Add(new PromptItem { Kind = EPromptKind.Slash, Label = "/write", Detail = "写入文件", Value = "/write " });

        // ── 最近修改文件 ──
        if (ModifiedFiles.Count > 0)
        {
            foreach (var f in ModifiedFiles.Take(4))
                items.Add(new PromptItem { Kind = EPromptKind.File, Label = Path.GetFileName(f), Detail = "最近修改", Value = $"@\"{f}\" " });
        }

        // ── Shell ──
        items.Add(new PromptItem { Kind = EPromptKind.Shell, Label = "dotnet build", Detail = "编译项目", Value = "!dotnet build" });
        items.Add(new PromptItem { Kind = EPromptKind.Shell, Label = "dotnet test", Detail = "运行测试", Value = "!dotnet test" });
        items.Add(new PromptItem { Kind = EPromptKind.Shell, Label = "git status", Detail = "查看状态", Value = "!git status" });
        items.Add(new PromptItem { Kind = EPromptKind.Shell, Label = "git diff", Detail = "查看变更", Value = "!git diff" });

        return items;
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);
        bool shift = key.Modifiers.HasFlag(ConsoleModifiers.Shift);

        // ── 1. 建议面板可见 → 建议导航（始终优先）──
        if (HandleSuggestPanelKey(key, ctrl, shift)) return true;

        // ── 2. 模态窗口优先 ──
        if (HasModal) return base.OnKey(key);

        // ── 2.5. 提示栏可见 → 提示栏导航（↑↓/Enter/Esc/Tab），优先于聊天滚动/提交/历史 ──
        if (PromptBarVisible && PromptKeyHook(key)) return true;

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
        // ── Ctrl+Shift+P 命令面板（对齐 Claude Code quickOpen / OpenCode）──
        // 必须在 `if (ctrl)` 块之前：否则 case ConsoleKey.P 会先把它当 Ctrl+P 建议条处理。
        if (ctrl && shift && key.Key == ConsoleKey.P)
        {
            OpenCommandPalette();
            return true;
        }

        // ── Ctrl 组合键 ──
        if (ctrl)
        {
            switch (key.Key)
            {
                case ConsoleKey.E:
                    Manager?.PushScreen(new EditorScreen(readOnly: WorkModeManager.CurrentMode == WorkMode.Plan)); // Plan 模式默认只读
                    return true;
                case ConsoleKey.T:
                case ConsoleKey.O:
                    Manager?.PushScreen(new SettingsScreen());
                    return true;
                case ConsoleKey.B:
                    ToggleSidePanel();
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
                case ConsoleKey.L:
                    // 全屏强制重绘（修复终端残留，保留聊天内容）
                    MarkDirty();
                    Manager?.Render();
                    return true;
                case ConsoleKey.D:
                    OnOpenDiff?.Invoke(); // diff 预览（/diff）
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

        // ── Alt+P 模型选择（对齐 Claude Code meta+p）──
        if (key.Modifiers.HasFlag(ConsoleModifiers.Alt) && key.Key == ConsoleKey.P)
        {
            OnCycleModel?.Invoke();
            return true;
        }

        // ── F5 刷新/重绘 ──
        if (key.Key == ConsoleKey.F5)
        {
            MarkDirty();
            Manager?.Render();
            return true;
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
            HidePromptBar(); // 提交消息时提示栏必须消失（防止悬浮建议残留）
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
            if (IsKnownPrefix(c))
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

        // 自定义钩子优先：已注册前缀直接调用 provider 生成提示项
        if (_prefixHintHooks.TryGetValue(prefix, out var hook))
            return hook(q) ?? new List<PromptItem>();

        switch (prefix)
        {
            case '/': // 斜杠命令 —— 从注册表动态生成（新增命令自动出现在补全）
                foreach (var cmd in SlashCommandRegistry.Commands)
                {
                    if (string.IsNullOrEmpty(q) ||
                        cmd.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        cmd.Aliases.Any(a => a.Contains(q, StringComparison.OrdinalIgnoreCase)))
                    {
                        // 只显示命令名，不显示子参数（Usage 里 [..]/<..> 太长太多导致列表参差不齐、
                        // 详情列对不齐）。Slash 图标本身是 "/"，去掉 Name 前导 "/" 避免出现 "//"。
                        var label = cmd.Name.TrimStart('/');
                        items.Add(new PromptItem
                        {
                            Kind = EPromptKind.Slash,
                            Label = label,
                            Detail = cmd.Description,
                            Value = cmd.Name + " ",
                        });
                    }
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
                                Kind = EPromptKind.File,
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
                        items.Add(new PromptItem { Kind = EPromptKind.Recent, Label = name, Detail = "最近修改", Value = "@\"" + f + "\" " });
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
                        items.Add(new PromptItem { Kind = EPromptKind.Shell, Label = cmd, Detail = desc, Value = "!" + cmd });
                }

                break;

            case '#': // 标签/Issue/PR 引用
                items.Add(new PromptItem { Kind = EPromptKind.Command, Label = "#todo", Detail = "待办事项", Value = "#todo " });
                items.Add(new PromptItem { Kind = EPromptKind.Command, Label = "#fix", Detail = "修复", Value = "#fix " });
                items.Add(new PromptItem { Kind = EPromptKind.Command, Label = "#wip", Detail = "进行中", Value = "#wip " });
                items.Add(new PromptItem { Kind = EPromptKind.Command, Label = "#done", Detail = "已完成", Value = "#done " });
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
}