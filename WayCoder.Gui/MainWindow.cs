using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;

namespace WayCoder.UI.Gui;

public partial class MainWindow : Window
{
    #region 属性

    /// <summary>槽位数量（10 个）。</summary>
    private const int SlotCount = 10;

    private readonly Agent?[] _agents = new Agent?[SlotCount];
    private readonly List<ChatMessage>[] _messages = new List<ChatMessage>[SlotCount];
    private readonly CancellationTokenSource?[] _cts = new CancellationTokenSource?[SlotCount];

    /// <summary>排队项：文本 + 排队时已上屏的消息气泡（轮到执行时复用更新「发送中」，避免重复上屏）。</summary>
    private sealed record PendingItem(string Text, ChatMessage? Msg);

    /// <summary>各槽位待处理指令队列：Agent 忙碌时输入入队，当前批次完成后自动取下一个执行（输入排队机制）。</summary>
    private readonly ConcurrentQueue<PendingItem>[] _pendingInputs =
        Enumerable.Range(0, SlotCount).Select(_ => new ConcurrentQueue<PendingItem>()).ToArray();

    private readonly Button[] _slotButtons = new Button[SlotCount];
    private readonly string[] _drafts = new string[SlotCount];

    /// <summary>各槽位是否在接收推理内容（«dim»…«/»，对齐 Web reasoning 分流）。</summary>
    private readonly bool[] _inReasoning = new bool[SlotCount];

    private DispatcherTimer? _rightTimer;

    /// <summary>Agent 动态状态栏动画定时器（100ms 驱动 Braille 帧 + 刷新状态文字）。</summary>
    private DispatcherTimer? _statusTimer;

    private int _spinnerFrame;

    /// <summary>各槽位任务完成时间戳（完成瞬态显示，TickCount64）。</summary>
    private readonly long[] _completeAtTicks = new long[SlotCount];

    /// <summary>流式渲染合帧守卫：同一 UI 帧内多个 token 只触发一次气泡重渲染。</summary>
    private bool _renderPending;

    /// <summary>右侧面板刷新重入守卫（2s 定时 + 手动刷新防叠层）。</summary>
    private bool _refreshing;

    private int _activeSlot = 0;

    #endregion

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        // 崩溃时保存各槽位会话（GuiBootstrap 已在 Program.Main 装好全局异常钩子）
        GuiBootstrap.OnCrashSave = SaveAllSessions;

        // 注入 GUI 交互桥：Agent 的权限确认/提问走 Avalonia 对话框，而非回退 Console I/O
        UxHelper.WebInteraction = new GuiInteraction(this);

        // 系统通知：UxHelper.Info/Success/Warn/Error 显示到当前槽位聊天流（否则回退 Console 丢失）
        UxHelper.OnNotify = (level, title, msg) => Dispatcher.UIThread.Post(() =>
            AppendSystem(_activeSlot,
                $"[{level switch { "success" => "✓", "warn" => "⚠", "error" => "✘", _ => "ℹ" }} {title}] {msg}"));

        InitModels();
        InitModelBar();
        InitSlots();
        SwitchSlot(0);
        RefreshSessions();

        // 右侧面板 2s 定时刷新（对齐 Web setInterval(fetchPanel, 2000)）
        _rightTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _rightTimer.Tick += (_, _) => RefreshPanel();
        _rightTimer.Start();

        // 动态状态栏：压缩/权限事件订阅 + 100ms 动画定时器（Braille 旋转 + 状态文字）
        ContextManager.CompressProgress += OnCompressProgress;
        ContextManager.CompressFinished += OnCompressFinished;

        PermissionManager.PermissionPromptStarted += OnPermissionStarted;
        PermissionManager.PermissionPromptResolved += OnPermissionResolved;

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _statusTimer.Tick += (_, _) =>
        {
            RefreshStatusBar(_activeSlot); // 先刷新显隐/文字
            if (AgentStatusBar.IsVisible)
            {
                _spinnerFrame = (_spinnerFrame + 1) % AgentStatusResolver.SpinnerFrames.Length;
                AgentStatusSpin.Text = AgentStatusResolver.SpinnerFrames[_spinnerFrame];
            }
        };
        _statusTimer.Start();
    }

    /// <summary>窗口关闭：停定时器、解绑事件、保存会话、session-end hook。</summary>
    protected override void OnClosed(EventArgs e)
    {
        _rightTimer?.Stop();
        _statusTimer?.Stop();
        ContextManager.CompressProgress -= OnCompressProgress;
        ContextManager.CompressFinished -= OnCompressFinished;
        PermissionManager.PermissionPromptStarted -= OnPermissionStarted;
        PermissionManager.PermissionPromptResolved -= OnPermissionResolved;
        SaveAllSessions();
        GuiBootstrap.Shutdown(); // session-end hook（对齐 CLI 退出流程）
        base.OnClosed(e);
    }

    /// <summary>压缩/权限状态变化 → 刷新动态状态栏（事件可能在后台线程触发，回 UI 线程）。</summary>
    private void OnCompressProgress(int layer, string label, double pct)
        => Dispatcher.UIThread.Post(() => RefreshStatusBar(_activeSlot));

    private void OnCompressFinished()
        => Dispatcher.UIThread.Post(() => RefreshStatusBar(_activeSlot));

    private void OnPermissionStarted(string tool)
        => Dispatcher.UIThread.Post(() => RefreshStatusBar(_activeSlot));

    private void OnPermissionResolved(string tool)
        => Dispatcher.UIThread.Post(() => RefreshStatusBar(_activeSlot));

    /// <summary>
    /// 刷新 Agent 动态状态栏（聊天与输入框之间）：空闲隐藏，其余显示状态文字（共享解析器统一文案）。
    /// </summary>
    private void RefreshStatusBar(int slot)
    {
        if (slot is < 0 or >= SlotCount)
            return;

        var a = _agents[slot];

        var view = AgentStatusResolver.Resolve(new AgentStatusInput(
            Busy: _cts[slot] != null,
            ToolName: a?.CurrentToolName,
            Compressing: ContextManager.IsCompressing,
            WaitingPermission: PermissionManager.PendingPermissionTool != null,
            WaitingUser: a?.CurrentToolName == "ask_user_question",
            WaitingSubagent: a?.CurrentToolName == "agent",
            Mode: a?.WorkMode ?? WorkMode.Build,
            RecentComplete: _completeAtTicks[slot] != 0 &&
                            Environment.TickCount64 - _completeAtTicks[slot] < 2500));
        AgentStatusBar.IsVisible = view.Status != AgentStatus.Idle;
        AgentStatusText.Text = view.Text;
    }

    // ── 初始化 ──

    private void InitModels()
    {
        ModelCatalog.Invalidate(); // 确保读到最新的模型目录（含导入）
        UpdateHeader();
    }

    /// <summary>初始化槽位按钮（F1-F10）。每个按钮点击后切换到对应槽位，刷新会话列表。</summary>
    private void InitSlots()
    {
        for (var i = 0; i < SlotCount; i++)
        {
            _messages[i] = [];
            var btn = new Button
            {
                Content = $"F{i + 1}",
                MinWidth = 38,
                Padding = new Thickness(8, 4)
            };
            var slot = i;
            btn.Click += (_, _) => SwitchSlot(slot);
            _slotButtons[i] = btn;
            if (i < SlotCount / 2)
            {
                SlotPanelH1.Children.Add(btn);
            }
            else
            {
                SlotPanelH2.Children.Add(btn);
            }
        }
    }

    private void UpdateHeader()
    {
        var cfg = Config.Instance;
        BigModelBtn.Content = $"🤖 {ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(cfg.Provider), cfg.Model)}";
        SmallModelBtn.Content = $"🔧 {ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(cfg.SmallProvider), cfg.SmallModel)}";
    }

    /// <summary>初始化 composer 工具栏：省钱模式 + 交互权限模式下拉。需先清空（Settings 保存后经 NotifySettingsSaved 重入，避免重复追加）。</summary>
    private void InitModelBar()
    {
        EconomyCombo.Items.Clear();
        PermCombo.Items.Clear();
        foreach (var v in new[] { "关", "自动", "开", "极致" })
            EconomyCombo.Items.Add(v);
        EconomyCombo.SelectedIndex = (int)Config.Instance.EconomyMode;

        // 权限下拉：显示中文、Tag 存英文标识符（PermissionManager.SetMode 只认英文）。
        // 不能裸加字符串——否则 Perm_SelectionChanged 会把中文串传 SetMode，静默落回 Ask。
        foreach (var (zh, en) in new[] { ("必问", "ask"), ("自动", "auto"), ("智能", "smartauto"), ("畅通", "yolo") })
            PermCombo.Items.Add(new ComboBoxItem { Content = zh, Tag = en });
        PermCombo.SelectedIndex = (int)PermissionManager.CurrentMode;
    }

    // ── 槽位 ──

    private void SwitchSlot(int slot)
    {
        // 当前槽位若有流式中的气泡，先定稿（不再指向它），避免切走后继续被写入
        if (_activeSlot >= 0 && _activeSlot < _messages.Length && _activeSlot != slot)
            FinalizeStreaming(_activeSlot);

        if (_activeSlot != slot)
            _drafts[_activeSlot] = InputBox.Text ?? ""; // 保存旧槽位输入草稿

        _activeSlot = slot;
        InputBox.Text = _drafts[slot] ?? ""; // 恢复目标槽位草稿

        UpdateSlotButtons(slot);
        RebuildMessages(slot);
        SlotLabel.Text = $"槽位 {slot + 1}";
        UpdateSendButtonState(slot); // 单按钮：空闲=发送(↑)，忙=停止(⏹)，始终可点
        RefreshPanel(); // Token 卡片切换活跃槽位
        RefreshSessions(); // 会话列表按槽位隔离
        TrySendNextPending(slot); // 切回有排队消息的槽位时继续消费（修「放回队尾无触发点」卡死）
    }

    /// <summary>重着色 10 个槽位按钮（切换槽位/主题切换时调用，取当前 GuiColors 静态画刷）。</summary>
    private void UpdateSlotButtons(int slot)
    {
        for (var i = 0; i < SlotCount; i++)
        {
            _slotButtons[i].Background = i == slot ? GuiColors.Accent : GuiColors.Panel2Bg;
            _slotButtons[i].Foreground = i == slot ? GuiColors.Text : GuiColors.IdleText;
        }
    }

    /// <summary>单按钮状态：空闲=发送(↑)，忙=停止(⏹)，始终可点（发送按钮兼停止，不额外加按钮）。</summary>
    private void UpdateSendButtonState(int slot)
    {
        var busy = slot >= 0 && slot < _cts.Length && _cts[slot] != null;
        SendButton.Content = busy ? "⏹" : "↑";
        SendButton.IsEnabled = true;
    }

    /// <summary>
    /// 给当前所有流式气泡封口（推理 + 正文可能同时开着）。
    /// 封口后 EnsureAssistant/EnsureReasoning 会新建气泡 —— 这是消息按时间线排列的前提：
    /// 不封口的话，工具消息之后的正文会继续写回工具消息「之前」的旧气泡，
    /// 视觉上就成了「对话全堆在上面、工具消息全堆在下面」。
    /// </summary>
    private void FinalizeStreaming(int slot)
    {
        foreach (var m in _messages[slot])
            if (m.Streaming)
                m.Streaming = false;
    }

    // ── 右侧数据面板（2s 定时刷新，对齐 Web /panel）──

    /// <summary>重建右侧 5 张数据卡片（任务/Token费用/修改文件/MCP/LSP）。</summary>
    private void RefreshPanel()
    {
        if (_refreshing)
            return;
        _refreshing = true;
        try
        {
            RightCards.Children.Clear();
            RightCards.Children.Add(Panels.TodosCard());
            RightCards.Children.Add(Panels.TokensCard(_agents[_activeSlot]));
            RightCards.Children.Add(Panels.FilesCard(p => EditorWindow.OpenFor(p)));
            RightCards.Children.Add(Panels.McpCard());
            RightCards.Children.Add(Panels.LspCard());
        }
        finally
        {
            _refreshing = false;
        }
    }

    /// <summary>重建指定槽位的气泡视图（切换槽位/主题变更/会话加载时用）。</summary>
    private void RebuildMessages(int slot)
    {
        MessagesHost.Children.Clear();
        foreach (var msg in _messages[slot])
        {
            if (msg.View == null)
            {
                msg.View = new MessageBubble(msg);
            }
            else
            {
                msg.View.Render();
                // 主题切换后重建 block 取当前主题文字色（MarkdownInlines 动态 TextBrush）
            }

            MessagesHost.Children.Add(msg.View);
        }

        if (slot == _activeSlot)
            Dispatcher.UIThread.Post(() => ChatScroll.ScrollToEnd(), DispatcherPriority.Background);
    }

    /// <summary>懒建槽位 Agent（复用 Web 版 EnsureSlot 的 Config→LLM→Agent 接线）。</summary>
    private Agent EnsureSlot(int slot)
    {
        if (_agents[slot] != null) return _agents[slot]!;

        var cfg = Config.Instance;
        var info = ModelCatalog.Find(cfg.Model);
        var providerId = info?.ProviderId ?? cfg.Provider;
        var key = ApiKeyStore.Get(providerId) ?? cfg.ApiKey;
        var baseUrl = info?.DefaultBaseUrl ?? cfg.BaseUrl;

        var llm = new LLM(cfg.Model, key, baseUrl, cfg.MaxTokens, cfg.Temperature)
        {
            SmallModel = cfg.SmallModel,
        };

        _agents[slot] = new Agent(llm,
            maxContextTokens: ModelCatalog.ResolveContextWindow(cfg.Model, cfg.MaxContextTokens),
            maxBudgetUsd: cfg.MaxBudgetUsd,
            autoCommit: cfg.AutoGitCommit)
        {
            AgentId = $"gui-slot-{slot}", // 槽位唯一标识：PendingImages 按 agentId 分队列防串扰
        };

        LoadSlotSession(slot); // 恢复历史会话
        return _agents[slot]!;
    }

    /// <summary>设置窗口保存后：刷新模型栏/面板/会话提示（SettingsWindow 回调）。</summary>
    internal void NotifySettingsSaved()
    {
        UpdateHeader();
        InitModelBar(); // 省钱/权限下拉跟随配置变化
        RefreshPanel();
        AppendSystem(_activeSlot, "[设置已保存]");
    }

    /// <summary>设置窗口保存失败提示。</summary>
    internal void NotifySettingsFailed(string message)
        => AppendSystem(_activeSlot, $"[保存设置失败] {message}");
}
