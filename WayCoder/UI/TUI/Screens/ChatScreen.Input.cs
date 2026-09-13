using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.Tools;
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
            if (!ConfirmPaste(text, lines)) return;

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
        if (!ConfirmPaste(text, lines)) return;

        InputArea.InsertText(normalized);
        MarkDirty();
    }

    /// <summary>粘贴确认：超长(&gt;500字符)或多行(&gt;3行)时行内确认（不弹窗），返回是否放行（false=取消）。
    /// PasteAsync 与 HandleBracketedPaste 共用；UI 线程调用（粘贴事件路径）。</summary>
    private bool ConfirmPaste(string text, string[] lines)
    {
        if (text.Length <= 500 && lines.Length <= 3) return true;
        var preview = text.Length > 200 ? ContextManager.TruncateByRunes(text, 200) + "..." : text;
        // 预览进聊天流（多行内容行内栏放不下）
        AddSystemMsg($"📋 粘贴确认 · {lines.Length} 行 / {text.Length} 字符\n{preview}");

        return UxHelper.RunInlineChoiceOnScreen(this,
        [
            new PromptItem { Kind = EPromptKind.Choice, Label = "1. 粘贴", Detail = "插入到输入框", ResultCode = 0 },
            new PromptItem { Kind = EPromptKind.Choice, Label = "2. 取消", Detail = "丢弃这段内容", ResultCode = 2 },
        ]) == 0;
    }

    // ── 输入操作 ──

    /// <summary>获取输入文本（处理多行合并）</summary>
    public string GetInputText()
    {
        return InputArea.Text;
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
          .Append(LspTool.ActiveSessions.Count).Append('|');
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
                var mark = McpStatusIcon.Text(s.Status);var src = s.Source == "claude" ? "〔Claude〕" : "";
                var mcpLine = $"  {mark} {s.Name}{src} [{s.Transport}] {s.ToolCount} 工具";
                if (s.ResourceCount > 0) mcpLine += $" · {s.ResourceCount} 资源";
                if (s.PromptCount > 0) mcpLine += $" · {s.PromptCount} 提示词";
                mcpLines.Add(mcpLine);
            }

        sections.Add(new PanelSection
        {
            Title = $"🔌 MCP ({McpManager.DiscoveredTools.Count})",
            Lines = mcpLines,
        });

        // ── LSP 区（仅活动会话时展示，无活动显示占位——不再一直列静态支持的服务列表）──
        var lspSessions = LspTool.ActiveSessions;
        var lspLines = new List<string>();
        if (lspSessions.Count == 0)
            lspLines.Add("  (无活动会话)");
        else
            foreach (var s in lspSessions)
            {
                var status = s.HasExited ? "✖已退出" : s.Initialized ? "✔已连接" : "⏳连接中";
                lspLines.Add($"  📦 {s.Command} {status} · {s.Root}");
            }
        sections.Add(new PanelSection
        {
            Title = $"🔍 LSP ({lspSessions.Count})",
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

        // 提示栏挤压/让出聊天区：标脏聊天列表整棵子树，强制填充背景+重绘，清掉被覆盖的残留像素。
        // 必须 MarkTreeDirty 而非 MarkDirty：提示栏一出现 chatH 就变，聊天列表渲染时先整视口擦成空白，
        // 若只标脏容器（MarkDirty），消息子项因 parentDirty=false 不重画 → 提示栏收起后消息永久空白。
        ChatList.MarkTreeDirty();

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
        // 提示栏消失 → 聊天区高度还原：标脏聊天列表整棵子树强制填充背景+重绘，清掉被提示栏盖住的残留（否则花屏）。
        // 同上必须 MarkTreeDirty：chatH 增高后若子项不重画，聊天内容会被擦成空白。
        ChatList.MarkTreeDirty();
        MarkDirty();
    }

    // ── 行内选择栏（权限确认 / 计划审批 / 通用确认就地决定，不弹窗）──
    //
    // 与 PromptBar 共用 InputArea.KeyHook，约定「同一时刻只有一个 Active」：
    // ShowInlineChoice 会先 HidePromptBar（反之 HideInlineChoice 清钩子时 PromptBar 必已隐藏）。

    /// <summary>行内选择栏是否可见</summary>
    public bool InlineChoiceVisible => InlineChoice?.Visible == true;

    /// <summary>
    /// 行内栏上次生效的高度（-1 = 尚未显示过）。
    ///
    /// 高度一变，**下方所有兄弟控件整体位移**（模式行 / 快捷键行 / 状态栏），而增量渲染只重绘
    /// 「自己标脏了」的控件 —— 位置被挪走的那些不重绘，旧像素就留在屏幕上。实测症状：问卷翻页后
    /// 上边框被上一帧的页头文本啃出豁口（`╭─── ─ ───── ─ ───╮`），--keypad 帧可见。
    /// 故高度变化时必须请求全屏重绘（不闪烁，只是让控件逐一覆盖重画）。
    /// </summary>
    private int _inlineChoiceLastHeight = -1;

    /// <summary>设置行内栏高度，高度变化时顺带请求全屏重绘（理由见 <see cref="_inlineChoiceLastHeight"/>）</summary>
    private void ApplyInlineChoiceHeight(int h)
    {
        InlineChoice.Height = h;
        if (h == _inlineChoiceLastHeight) return;
        _inlineChoiceLastHeight = h;
        TuiManager.RequestFullRefresh();
    }

    /// <summary>本次行内选择的完成回调（null = 无待决选择）</summary>
    private Action<int>? _inlineChoiceDone;

    /// <summary>最近一次结果码。初值 2 = 拒绝 —— 未应答/超时/异常一律默认拒绝（同 ShowPermissionDialog 的 ?? 2）。</summary>
    private volatile int _inlineChoiceResult = 2;

    /// <summary>最近一次行内选择的结果码（供后台等待线程在收起后读取）</summary>
    public int LastInlineChoiceResult => _inlineChoiceResult;

    /// <summary>
    /// 在输入框**下方**就地显示选择项并接管键位（↑↓/Home/End 移动、Enter 确认、Esc 拒绝、Y/N/A 单键）。
    /// 线程安全：只改字段，实际渲染由常驻主循环负责 —— 后台 Agent 线程调用它不会阻塞主循环
    /// （配 <c>UxHelper.RunInlineChoiceOnScreen</c> 等待结果）。
    /// </summary>
    public void ShowInlineChoice(List<PromptItem> items, Action<int> onDone)
    {
        if (items.Count == 0) { onDone(2); return; }

        HidePromptBar(); // 先让位：两者共用 InputArea.KeyHook
        _survey = null;  // 与问卷互斥（同一把 KeyHook 只归一个）
        _surveyDone = null;
        _surveyPicked = null;
        // 复位问卷留下的借用态：残留会让权限确认带上勾选框/页头（正常流程下 HideInlineChoice
        // 已复位，这里是「问卷未收干净就被确认岔开」的兜底）
        InlineChoice.MultiSelect = false;
        InlineChoice.HeaderText = "";

        _inlineChoiceDone = onDone;
        _inlineChoiceResult = 2; // 未应答即拒绝

        InlineChoice.Items = items;
        InlineChoice.SelectedIndex = 0;
        InlineChoice.ViewIndex = 0;
        InlineChoice.Visible = true;
        // 高度随条目数（与 ShowPromptBar 同一套账：边框模式 +2、填充模式 +1）
        var extra = InlineChoice.Bg == 0 ? 2 : 1;
        ApplyInlineChoiceHeight(Math.Min(items.Count, InlineChoice.MaxVisible) + extra);
        SyncShortcutRow(); // 快捷键行切成行内栏键位（第一次用的人唯一的提示处）

        InputArea.KeyHook = InlineChoiceKeyHook;

        // 本栏一出现 chatH 就变 → 必须 MarkTreeDirty 整树标脏（理由同 ShowPromptBar：
        // 只标容器时消息子项因 parentDirty=false 不重画，收起后聊天区永久空白）。
        ChatList.MarkTreeDirty();
        MarkDirty();
    }

    /// <summary>收起行内选择栏（不动结果码 —— 结果只由 <see cref="ResolveInlineChoice"/> 写）。</summary>
    public void HideInlineChoice()
    {
        if (InlineChoice == null || !InlineChoice.Visible) return;

        InlineChoice.Visible = false;
        ApplyInlineChoiceHeight(0);
        InlineChoice.Items.Clear();
        InlineChoice.SelectedIndex = -1;
        InlineChoice.ViewIndex = 0;
        // 复位借用态：本控件在「权限确认 / 问卷」之间轮流复用，漏复位会让下次确认带上勾选框或页头
        InlineChoice.MultiSelect = false;
        InlineChoice.HeaderText = "";
        // 问卷态一并作废：超时路径不走 ResolveSurvey，残留会让下次按键误入问卷分支
        _survey = null;
        _surveyDone = null;
        _surveyPicked = null;
        _surveyOthers = null;
        _surveyCheckedSnapshot = null;
        _surveyOtherInput = false;
        _surveyPage = 0;
        _inlineChoiceDone = null; // 清未消费回调：超时后用户再按 Enter 不该再回到已返回的调用方
        InputArea.KeyHook = null; // 此时 PromptBar 必已隐藏（见本节首注释）
        SyncShortcutRow();       // 提示行还原成全局键位表
        ChatList.MarkTreeDirty();
        MarkDirty();
    }

    /// <summary>结束行内选择：收起 + 回调结果码。重复调用只第一次生效（回调已置 null）。</summary>
    private void ResolveInlineChoice(int code)
    {
        var done = _inlineChoiceDone;
        _inlineChoiceDone = null;
        _inlineChoiceResult = code;
        HideInlineChoice();
        done?.Invoke(code);
    }

    /// <summary>当前选中项的 ResultCode（越界/未选 = 2 拒绝）</summary>
    private int CurrentInlineResultCode()
    {
        var i = InlineChoice.SelectedIndex;
        if (i < 0 || i >= InlineChoice.Items.Count) return 2;
        return InlineChoice.Items[i].ResultCode;
    }

    /// <summary>当前选中项是否危险操作（危险项不提供 A=全部允许）</summary>
    private bool CurrentInlineIsDangerous()
    {
        var i = InlineChoice.SelectedIndex;
        return i >= 0 && i < InlineChoice.Items.Count && InlineChoice.Items[i].IsDangerous;
    }

    /// <summary>行内选择栏的按键钩子（挂在 InputArea 上）</summary>
    private bool InlineChoiceKeyHook(ConsoleKeyInfo key)
    {
        if (_surveyOtherInput) return InlineOtherKeyHook(key); // 「其他」输入态：只拦 Enter/Esc，其余放行给输入框
        if (!InlineChoiceVisible) return false;
        // 问卷态另有一套键位（←→/Tab 翻页、Enter 逐步推进），先分流再走单选那套
        if (_survey != null) return SurveyKeyHook(key);

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                ResolveInlineChoice(2); // Esc = 拒绝（随时可逃，对齐竞品）
                return true;
            case ConsoleKey.UpArrow:
            case ConsoleKey.DownArrow:
            case ConsoleKey.Home:
            case ConsoleKey.End:
                InlineChoice.OnKey(key);
                MarkDirty();
                return true;
            case ConsoleKey.Enter:
                ResolveInlineChoice(CurrentInlineResultCode());
                return true;
        }

        // 单键快捷：Y=允许 / N=拒绝 / A=全部允许（危险操作不给 A）
        if (key.KeyChar is 'y' or 'Y') { ResolveInlineChoice(0); return true; }
        if (key.KeyChar is 'n' or 'N') { ResolveInlineChoice(2); return true; }
        if (key.KeyChar is 'a' or 'A' && !CurrentInlineIsDangerous()) { ResolveInlineChoice(1); return true; }

        // 数字键 1..9 = 直接选中该项并提交（与问卷同一手感；帮助面板「1 - 9 直接选中第 N 项」两处都兑现）
        if (key.KeyChar is >= '1' and <= '9')
        {
            var i = key.KeyChar - '1';
            if (i < InlineChoice.Items.Count)
            {
                InlineChoice.SelectedIndex = i;
                ResolveInlineChoice(CurrentInlineResultCode());
            }
            return true;
        }

        // 其余键一律吞掉：别透传给输入框 —— 一打字就触发前缀提示，PromptBar 会来抢同一个
        // KeyHook，变成「确认框和输入建议打架」。待决期间输入区不接受输入。
        return true;
    }

    // ── 行内问卷（多问题：横向标签页 / 分步骤；每题单选或多选）──
    //
    // 与权限确认共用 InlineChoice 控件（同一把 KeyHook 同时只归一个）：在单栏之上多了
    // 「多页 + 多选」，形态由题目数 / MultiSelect / showTabs 三者决定（见 SurveyQuestion 注释）。

    /// <summary>当前问卷题目（null = 不在问卷态）</summary>
    private List<SurveyQuestion>? _survey;

    /// <summary>当前页索引</summary>
    private int _surveyPage;

    /// <summary>每页已选索引（多选题可有多个）</summary>
    private List<HashSet<int>>? _surveyPicked;

    /// <summary>是否显示横向标签行（false = 分步骤，页头显示「步骤 k/n」）</summary>
    private bool _surveyTabs;

    /// <summary>每题的自定义答案（选了「其他」才有文本；与题目一一对应，null = 该题没用「其他」）</summary>
    private List<string?>? _surveyOthers;

    /// <summary>「其他」输入态：复用输入框，键入放行、Enter 提交、Esc 退回选项</summary>
    private bool _surveyOtherInput;

    /// <summary>「其他」输入往返期间的勾选快照（多选页勾选只活在控件 Items 里，
    /// 重建选项列表时会按 picked 还原而丢掉刚勾的项，故往返前后用快照兜住）</summary>
    private List<bool>? _surveyCheckedSnapshot;

    /// <summary>问卷完成回调（null = 无待决问卷）</summary>
    private Action<SurveyResult?>? _surveyDone;

    /// <summary>最近一次问卷结果（取消 = null）。供等待线程在收起后读取。</summary>
    private volatile SurveyResult? _surveyResult;

    /// <summary>问卷是否进行中</summary>
    public bool InlineSurveyVisible => _survey != null;

    /// <summary>最近一次问卷结果（取消/超时 = null）</summary>
    public SurveyResult? LastSurveyResult => _surveyResult;

    /// <summary>
    /// 显示行内问卷（**不弹窗**）—— 由 <c>UxHelper.RunInlineSurveyOnScreen</c> 调用，UI 线程也可直接调。
    /// 每题一页：↑↓ 选项、Space 勾选（多选）、Enter 提交本页并前进、←→/Tab 翻页（多题）、Esc 取消、
    /// 数字键 1..9 直选并提交。
    /// </summary>
    public void ShowInlineSurvey(List<SurveyQuestion> questions, Action<SurveyResult?> onDone, bool showTabs = false)
    {
        if (questions.Count == 0) { onDone(null); return; }

        HidePromptBar();
        _inlineChoiceDone = null; // 与单选/确认互斥（同一把 KeyHook）

        _survey = questions;
        _surveyTabs = showTabs;
        _surveyPage = 0;
        _surveyPicked = [.. questions.Select(_ => new HashSet<int>())];
        _surveyOthers = [.. questions.Select(_ => (string?)null)];
        _surveyCheckedSnapshot = null;
        _surveyOtherInput = false;
        _surveyDone = onDone;
        _surveyResult = null;

        RenderSurveyPage();
    }

    /// <summary>把当前页渲染进 InlineChoice（页头 + 选项 + 勾选态）</summary>
    private void RenderSurveyPage()
    {
        if (_survey == null) return;
        var q = _survey[_surveyPage];
        var picked = _surveyPicked![_surveyPage];

        InlineChoice.MultiSelect = q.MultiSelect;
        InlineChoice.HeaderText = _survey.Count > 1
            ? (_surveyTabs ? BuildSurveyTabHeader() : $"步骤 {_surveyPage + 1}/{_survey.Count} · {q.Title}")
            : "";
        var items = q.Options.Select((o, i) => new PromptItem
        {
            Kind = EPromptKind.Choice,
            Label = o.Label,
            Detail = o.Description,
            ResultCode = i,
            Checked = picked.Contains(i),
            IsOther = o.IsOther,
        }).ToList();
        // 「其他（自行输入）」：模型给的选项未必覆盖用户想法，末尾自动补一个入口（竞品默认有）
        if (q.AllowOther && !items.Any(it => it.IsOther))
            items.Add(new PromptItem
            {
                Kind = EPromptKind.Choice,
                Label = "其他（自行输入）",
                Detail = "输入自定义答案",
                IsOther = true,
                ResultCode = -1,
            });
        // 「跳过此题」：单选页才有意义（多选页空选本身就是跳过，再来一行是噪音）
        if (q.AllowSkip && !q.MultiSelect)
            items.Add(new PromptItem
            {
                Kind = EPromptKind.Choice,
                Label = "跳过此题",
                Detail = "不作答，直接下一题",
                IsSkip = true,
                ResultCode = -2,
            });
        // 「其他」输入往返后恢复勾选态（见 _surveyCheckedSnapshot）
        if (_surveyCheckedSnapshot is { } snap && snap.Count == items.Count)
            for (var i = 0; i < items.Count; i++) items[i].Checked = snap[i];
        InlineChoice.Items = items;
        InlineChoice.SelectedIndex = InlineChoice.Items.Count > 0 ? 0 : -1;
        InlineChoice.ViewIndex = 0;
        InlineChoice.Visible = true;
        // 高度 = 页头(0/1) + 选项数 + 边框（与 ShowPromptBar / ShowInlineChoice 同一套账）
        var extra = InlineChoice.Bg == 0 ? 2 : 1;
        var headerRows = string.IsNullOrEmpty(InlineChoice.HeaderText) ? 0 : 1;
        ApplyInlineChoiceHeight(headerRows + Math.Min(InlineChoice.Items.Count, InlineChoice.MaxVisible) + extra);
        SyncShortcutRow();

        InputArea.KeyHook = InlineChoiceKeyHook;
        ChatList.MarkTreeDirty(); // chatH 变了必须整树标脏（同 ShowInlineChoice）
        MarkDirty();
    }

    /// <summary>横向标签行：`▶ 标题` 标当前页，其余平铺（一屏扫完总共有几问）</summary>
    private string BuildSurveyTabHeader()
    {
        var parts = new List<string>();
        // 非当前页按「▶ 」的**显示宽**补空格（▶ 是宽字符，硬写两个空格会差一列、标签列歪）
        var markW = AnsiHelper.DisplayWidth("▶ ");
        for (var i = 0; i < _survey!.Count; i++)
        {
            var t = string.IsNullOrEmpty(_survey[i].Title) ? $"问题{i + 1}" : _survey[i].Title;
            parts.Add(i == _surveyPage ? "▶ " + t : new string(' ', markW) + t);
        }
        return string.Join("  ", parts);
    }

    /// <summary>问卷键位：←→/Tab 翻页、↑↓ 选项、Space 勾选、Enter 提交本页并前进、Esc 取消、数字直选</summary>
    private bool SurveyKeyHook(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                ResolveSurvey(null); // 取消 = 整份作废（不是「本页跳过」）
                return true;

            case ConsoleKey.LeftArrow:
            case ConsoleKey.RightArrow:
            case ConsoleKey.Tab:
                if (_survey!.Count > 1)
                {
                    var d = key.Key == ConsoleKey.LeftArrow ? -1 : 1;
                    _surveyPage = (_surveyPage + d + _survey.Count) % _survey.Count;
                    RenderSurveyPage();
                }
                return true;

            case ConsoleKey.UpArrow:
            case ConsoleKey.DownArrow:
            case ConsoleKey.Home:
            case ConsoleKey.End:
            case ConsoleKey.Spacebar: // 多选勾选交给 TuiPromptBar 自己处理
                InlineChoice.OnKey(key);
                MarkDirty();
                return true;

            case ConsoleKey.Enter:
                if (CurrentSurveyItemIsOther()) BeginSurveyOther();
                else CommitSurveyPage();
                return true;
        }

        if (key.KeyChar is >= '1' and <= '9')
        {
            var i = key.KeyChar - '1';
            if (i < InlineChoice.Items.Count)
            {
                InlineChoice.SelectedIndex = i;
                if (_survey![_surveyPage].MultiSelect)
                    // 多选页：数字 = 切换该项勾选（等价 Space，省去先移光标）。
                    // 不能在这里提交 —— 多选本来就要选好几项，一键提交等于永远只能勾中一个。
                    InlineChoice.Items[i].Checked = !InlineChoice.Items[i].Checked;
                else if (InlineChoice.Items[i].IsOther)
                    BeginSurveyOther(); // 数字点到「其他」= 进自定义输入，不是提交
                else
                    CommitSurveyPage();
                MarkDirty();
            }
            return true;
        }

        return true; // 独占键位（同单选栏：透传会触发前缀提示来抢同一把 KeyHook）
    }

    /// <summary>提交当前页：记录选中 → 前进一页；已是最后一页则整份完成</summary>
    private void CommitSurveyPage()
    {
        if (_survey == null || _surveyPicked == null) return;
        var q = _survey[_surveyPage];
        var picked = _surveyPicked[_surveyPage];

        // 多选勾了「其他」但还没输入 → 先去要答案，回来再提交（否则 -1 没有对应文本）
        if (q.MultiSelect && _surveyOthers![_surveyPage] == null
            && InlineChoice.Items.Any(it => it.IsOther && it.Checked))
        {
            BeginSurveyOther();
            return;
        }

        if (q.MultiSelect)
        {
            picked.Clear();
            for (var i = 0; i < InlineChoice.Items.Count; i++)
                if (InlineChoice.Items[i].Checked)
                    picked.Add(InlineChoice.Items[i].IsOther ? -1 : i); // -1 = 「其他」，文本在 _surveyOthers
            // 允许「一个都不勾」——那是明确的「都不选」，不是没作答
        }
        else
        {
            var sel = InlineChoice.SelectedIndex;
            if (sel < 0 || sel >= InlineChoice.Items.Count) return; // 无有效选中 → 不误提交
            picked.Clear();
            // 跳过 = 保持空（结果里这一题就是空列表）；「其他」记 -1（文本在 _surveyOthers）
            if (InlineChoice.Items[sel].IsOther) picked.Add(-1);
            else if (!InlineChoice.Items[sel].IsSkip) picked.Add(sel);
        }
        _surveyCheckedSnapshot = null; // 本页已定案，快照使命结束

        if (_surveyPage < _survey.Count - 1)
        {
            _surveyPage++;
            RenderSurveyPage(); // 分步骤：Enter 直接推进下一步（横向多页时也能这么走）
        }
        else
        {
            var results = new SurveyResult(
                [.. _surveyPicked.Select(s => s.OrderBy(x => x).ToList())],
                [.. _surveyOthers!]);
            ResolveSurvey(results);
        }
    }

    /// <summary>结束问卷：收起 + 回调结果（重复调用只第一次生效）</summary>
    private void ResolveSurvey(SurveyResult? result)
    {
        var done = _surveyDone;
        _survey = null;
        _surveyDone = null;
        _surveyPicked = null;
        _surveyOthers = null;
        _surveyCheckedSnapshot = null;
        _surveyOtherInput = false;
        _surveyPage = 0;
        _surveyResult = result;
        HideInlineChoice();
        done?.Invoke(result);
    }

    // ── 问卷「其他」自定义输入 ──
    //
    // 复用输入框而不是另造输入控件：用户本来就熟悉它（光标/粘贴/多行/历史都在），
    // 而 KeyHook 返回 false 就能让按键照常流进输入框，只在 Enter/Esc 上拦一下。

    /// <summary>当前选中的是不是「其他」项</summary>
    private bool CurrentSurveyItemIsOther()
    {
        var i = InlineChoice.SelectedIndex;
        return i >= 0 && i < InlineChoice.Items.Count && InlineChoice.Items[i].IsOther;
    }

    /// <summary>进入「其他」输入态：收起选项栏 → 清空输入框 → 键入放行、Enter 提交、Esc 退回</summary>
    private void BeginSurveyOther()
    {
        _surveyOtherInput = true;
        // 记下勾选态（多选页往返期间不能丢），再收起选项栏把屏幕让给输入框
        _surveyCheckedSnapshot = [.. InlineChoice.Items.Select(it => it.Checked)];
        InlineChoice.Visible = false;
        ApplyInlineChoiceHeight(0);
        SetInput("");
        InputArea.KeyHook = InlineOtherKeyHook;
        InputArea.Focused = true;
        AddSystemMsg("✎ 请输入自定义答案：Enter 提交 · Esc 返回选项");
        MarkDirty();
    }

    /// <summary>「其他」输入态的按键钩子 —— 只拦 Esc/Enter，其余放行给输入框正常编辑</summary>
    private bool InlineOtherKeyHook(ConsoleKeyInfo key)
    {
        if (!_surveyOtherInput) return false;

        if (key.Key == ConsoleKey.Escape)
        {
            CancelSurveyOther();
            return true;
        }
        if (key.Key == ConsoleKey.Enter)
        {
            var text = GetInputText().Trim();
            if (text.Length == 0) return true; // 空输入不提交（继续编辑，或按 Esc 退回选项）
            CommitSurveyOther(text);
            return true;
        }
        return false; // 其余键交给输入框
    }

    /// <summary>提交自定义答案：记为本题的「其他」文本，再前进/收尾</summary>
    private void CommitSurveyOther(string text)
    {
        if (_survey == null || _surveyOthers == null) return;
        _surveyOtherInput = false;
        _surveyOthers[_surveyPage] = text;
        SetInput("");
        RenderSurveyPage();
        // 复选到「其他」项再提交：RenderSurveyPage 会把选中重置回第一项，
        // 不指明就会被记成「选了第一项」，而用户的答案其实是这段自定义文本。
        var otherIdx = InlineChoice.Items.FindIndex(it => it.IsOther);
        if (otherIdx >= 0) InlineChoice.SelectedIndex = otherIdx;
        CommitSurveyPage();
    }

    /// <summary>取消自定义输入：退回选项栏，不改任何选择</summary>
    private void CancelSurveyOther()
    {
        _surveyOtherInput = false;
        SetInput("");
        RenderSurveyPage();
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

    /// <summary>
    /// Shell 命令提示单源清单（Hint=命令标签 / Detail=说明 / Value=实际键入内容，`!` 前缀展开为 Shell 块）。
    /// <see cref="BuildDefaultHints"/>（Ctrl+P 默认提示）与 <see cref="BuildPrefixHints"/> 的 `!` 分支共用，
    /// 杜绝两份数组 Detail 漂移（此前 git status 一处「查看状态」一处「查看仓库状态」）。
    /// </summary>
    private static readonly (string Hint, string Detail, string Value)[] ShellCommandHints =
    [
        ("dotnet build", "编译项目", "!dotnet build"),
        ("dotnet run", "运行项目", "!dotnet run"),
        ("dotnet test", "运行测试", "!dotnet test"),
        ("dotnet publish -c Release", "AOT 发布", "!dotnet publish -c Release"),
        ("git status", "查看仓库状态", "!git status"),
        ("git diff", "查看变更", "!git diff"),
        ("git add -A", "暂存所有变更", "!git add -A"),
        ("git commit -m", "提交", "!git commit -m"),
        ("git push", "推送", "!git push"),
        ("git pull", "拉取", "!git pull"),
        ("git log --oneline", "查看日志", "!git log --oneline"),
        ("ls -la", "列出文件", "!ls -la"),
        ("find . -name", "搜索文件", "!find . -name"),
        ("grep -r", "搜索内容", "!grep -r"),
    ];

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
        // 默认栏保持原 4 个常用（不意外变长）；`!` 前缀输入时列全 14（见 BuildPrefixHints 的 '!' 分支）
        foreach (var (hint, detail, value) in ShellCommandHints)
        {
            if (hint is not ("dotnet build" or "dotnet test" or "git status" or "git diff")) continue;
            items.Add(new PromptItem { Kind = EPromptKind.Shell, Label = hint, Detail = detail, Value = value });
        }

        return items;
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);
        bool shift = key.Modifiers.HasFlag(ConsoleModifiers.Shift);

        // ── 1. 行内选择栏可见 → 独占键盘（权限确认 / 计划审批 / 通用确认）──
        //     放在建议面板**之前**：输入 `/` 弹出的建议面板可能与后台发起的权限确认同时在场，
        //     而确认框是「Agent 卡在那里等回答」的一方，必须先答。有模态窗时让位（模态更优先）。
        // 「其他」输入态时选项栏已收起（InlineChoiceVisible=false），但它同样要抢在
        // HandleSpecial 的「Enter = 发送消息」之前 —— 否则用户打完自定义答案一按回车，
        // 答案会当成聊天消息发出去，问卷永远走不完。
        if (!HasModal && (InlineChoiceVisible || _surveyOtherInput) && InlineChoiceKeyHook(key)) return true;

        // ── 2. 建议面板可见 → 建议导航（始终优先）──
        if (HandleSuggestPanelKey(key, ctrl, shift)) return true;

        // ── 3. 模态窗口优先 ──
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
                InputArea.OnKey(new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));
                MarkDirty();
                UpdateSuggestions(Suggestions, SuggestIndex);
                return true; // 已处理，不再向下传递
            case ConsoleKey.LeftArrow:
            case ConsoleKey.RightArrow:
                SuggestActive = false;
                return false; // 继续传递，让光标移动生效
        }

        return false;
    }

    /// <summary>
    /// 全局快捷键（轴向层，一键一义）：Ctrl+B/R/Y/M/S/G/H/T/L/D/U/W、
    /// F1-F10、Ctrl+Home/End/Up/Down。全表**不含三键组合**（Ctrl+Shift+X 系在 Windows 上
    /// 会被终端抢走或丢修饰键，见 <c>InputEvent.IsCycleConnectKey</c> 的说明）。
    /// 注意 Ctrl+P/E/Q/X（权限/经济/紧急退出/换大小模型）由 REPL 主循环截走（Program.Repl:416/474/484/502），
    /// 此处不重复绑定——避免「同一键两种含义」。
    /// </summary>
    private bool HandleGlobalShortcut(ConsoleKeyInfo key, bool ctrl, bool shift)
    {
        // ── Ctrl 组合键 ──
        if (ctrl)
        {
            switch (key.Key)
            {
                // 命令面板 = **纯 Ctrl+U**。原绑 Ctrl+Shift+P（对齐 Claude Code / VS Code），
                // 但在 Windows 上按不到：Windows Terminal 先把 Ctrl+Shift+P 抢去开它自己的命令面板；
                // 即便不被抢，VT 字节流也拿不到 Shift 修饰键（见 CharSource.ToConsoleKeyInfo），
                // 会退化成 Ctrl+P＝权限循环（权限与命令面板串味）。同类键一律用纯 Ctrl+字母
                // （快捷键跨平台铁律）。Ctrl+U 空闲：Ctrl+A/F/N 在输入框/对话框/编辑器各有归属。
                case ConsoleKey.U:
                    OpenCommandPalette();
                    return true;
                // Ctrl+E 已统一为「经济模式循环」（轴向层）——REPL 主循环 Program.Repl:484 在无弹窗时先截走；
                // 编辑器经 /edit（Plan 模式只读走 --readonly）。此处不再绑定 Ctrl+E。
                case ConsoleKey.T:
                    Manager?.PushScreen(new SettingsScreen());
                    return true;
                case ConsoleKey.B:
                    ToggleSidePanel();
                    return true;
                case ConsoleKey.R:
                    OnSyncQr?.Invoke(); // 生成同步二维码（手机扫码跨设备轮转）
                    return true;
                // 搜索对话历史 = Ctrl+F（find 惯例）。原为 Ctrl+Y —— 那是输入框的「重做」
                //（TuiEditBase.HandleCtrlKey case Y），被全局盖掉后想重做反而弹搜索框。
                case ConsoleKey.F:
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
                // 主题选择对话框。原为 Ctrl+Shift+F1（弹框）/ Ctrl+Shift+F2（直接轮转下一个）——
                // 三键组合一律弃用。轮转改为在对话框里 ↑↓ 选，另给 `/theme next` 打字兜底。
                case ConsoleKey.W:
                    ShowThemePicker();
                    return true;
                // Ctrl+P 已统一为「权限模式循环」（轴向层，主循环 Program.Repl:474 先截走）；
                // 建议条改由 `/`、`!`、`#`、`@` 前缀自动触发（CheckPrefixHints）。此处不再绑定 Ctrl+P。
                // Ctrl+Q 已统一为「紧急退出」（主循环 Program.Repl:416 PanicExit）——此处不再绑定 Ctrl+Q。
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
            EnqueueSubmission(input);
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
        // 输入框失焦兜底：TuiEditBase.OnKey 在 !Focused 时直接 drop 所有字面键（`if (!IsEnabled || !Focused) return false`），
        // 而 InputArea.Focused 只在模态窗口开/关时保存/恢复（TuiScreen._savedRootFocus），一旦恢复路径漏执行
        // （非模态窗口/编辑器切换/深层弹窗后 _savedRootFocus 丢置 null），就停在「能按全局快捷键却打不了字」。
        // 聊天输入框是唯一真正的输入目标：到这里说明无模态、无弹窗、面向聊天屏，直接恢复聚焦即可 —— 顺手不侵入窗口系统。
        if (!InputArea.Focused) InputArea.Focused = true;

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
            InputArea.OnKey(new ConsoleKeyInfo('\n', ConsoleKey.Enter, false, false, false));
            MarkDirty();
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

            case '!': // Shell 命令（单源 <see cref="ShellCommandHints"/>，与 Ctrl+P 默认提示共用）
                foreach (var (hint, detail, value) in ShellCommandHints)
                {
                    if (string.IsNullOrEmpty(q) ||
                        hint.StartsWith(q, StringComparison.OrdinalIgnoreCase))
                        items.Add(new PromptItem { Kind = EPromptKind.Shell, Label = hint, Detail = detail, Value = value });
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
        else
        {
            if (InputArea.CursorRow > 0)
            {
                InputArea.CursorRow--;
                InputArea.CursorCol = Math.Min(InputArea.CursorCol,
                    InputArea.Lines[InputArea.CursorRow].Length);
            }

            MarkDirty();
        }

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
        else
        {
            if (InputArea.CursorRow < InputArea.Lines.Count - 1)
            {
                InputArea.CursorRow++;
                InputArea.CursorCol = Math.Min(InputArea.CursorCol,
                    InputArea.Lines[InputArea.CursorRow].Length);
            }

            MarkDirty();
        }

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
            for (int t = 0; t < 4; t++)
            {
                InputArea.OnKey(new ConsoleKeyInfo(' ', (ConsoleKey)' ', false, false, false));
                MarkDirty();
            }

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
            InputArea.OnKey(new ConsoleKeyInfo(' ', (ConsoleKey)' ', false, false, false));
            MarkDirty();
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

        // 防切半 UTF-16 代理对：若 len 恰好停在代理对高/低位之间，回退到完整码元边界（emoji/扩展 B 汉字）
        if (len > 0 && len < first.Length
            && char.IsHighSurrogate(first[len - 1]) && char.IsLowSurrogate(first[len]))
            len--;

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

}