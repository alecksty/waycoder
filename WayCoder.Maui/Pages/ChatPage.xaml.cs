using System.Collections.ObjectModel;
using System.Text;
using WayCoder.Infra;
using WayCoder.Maui.Markup;
using WayCoder.Maui.Models;
using WayCoder.Maui.Services;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.Maui.Pages;

/// <summary>按消息角色选择气泡模板（用户右对齐 / AI 左对齐富文本 / 思考一行泡泡 / 工具灰色小字）。</summary>
public class MessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate UserTemplate { get; set; } = null!;
    public DataTemplate AssistantTemplate { get; set; } = null!;
    public DataTemplate ToolTemplate { get; set; } = null!;
    public DataTemplate ThinkingTemplate { get; set; } = null!;

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        => item is ChatMessage m
            ? m.Role switch
            {
                ChatRole.User => UserTemplate,
                ChatRole.Assistant => AssistantTemplate,
                ChatRole.Tool => ToolTemplate,
                ChatRole.Thinking => ThinkingTemplate,
                _ => AssistantTemplate,
            }
            : AssistantTemplate;
}

public partial class ChatPage : ContentPage
{
    public ObservableCollection<ChatMessage> Messages { get; } = new();

    private readonly AgentService _agent = new();
    private readonly ChatScreen _screen = new();
    private CancellationTokenSource? _cts;

    /// <summary>当前一轮对话的完成信号（RunOneMessageAsync finally 置完成）。
    /// 会话切换/新建须等旧轮彻底结束（其 finally 已在旧 _currentSessionId 下完成落盘、
    /// 残余回调已执行完）再清空/切 id，否则旧轮残余会写入新会话（见 code-review finding）。</summary>
    private TaskCompletionSource<bool>? _activeRound;

    /// <summary>当前会话从盘载入的原始 JNode（含 desktop 会话的 tool/system 节点）。
    /// 回写时以此为基础合并新消息，确保共享 schema 会话被手机端重存时不丢失 tool/system（code-review finding #4）。
    /// 未从盘载入（新建）的会话为 null。</summary>
    private List<JNode>? _sessionRaw;

    /// <summary>会话载入后经 UI 新增的消息数（非载入 AddMessage 递增；载入后归零）。
    /// 独立于条数裁剪（PruneMessages 从队列头 RemoveAt(0)），保证回写合并总能定位载入后新增的尾部消息。</summary>
    private int _appAddCount;

    /// <summary>当前会话的 LLM 上下文是否已注入 Agent（每会话首条消息只种一次，防旧会话历史污染新会话）。</summary>
    private bool _contextSeeded;

    /// <summary>
    /// 跨页会话切换桥（独立页 B 方案）：SessionHistoryPage 点选/新建时设置，
    /// 本页 OnAppearing 消费后执行切换（SwitchToSessionAsync），随后置 null。
    /// </summary>
    internal static string? PendingOpenSessionId;

    /// <summary>左/右抽屉开合状态（Edge Pan / scrim / ≡ / ☰ 联动判定）。</summary>
    private bool _leftDrawerOpen;
    private bool _rightDrawerOpen;

    /// <summary>抽屉开合动画进行中（防重入：动画未完再点 ≡/☰/scrim 直接忽略）。</summary>
    private bool _drawerAnimating;

    private const uint DrawerAnimMs = 200; // 抽屉滑入/滑出动画时长

    /// <summary>当前会话 id（多会话历史；切换/新建后更新并持久化到 Preferences）。</summary>
    private string _currentSessionId = "";

    /// <summary>当前工具调用组（onTool/onToolOutput 累积到这里；正文间断或新轮开新组）。</summary>
    private ChatMessage? _toolGroup;

    /// <summary>消息列表是否接近底部（用于智能滚动：接近底部才跟随，用户上翻时不打断）。</summary>
    private bool _isNearBottom = true;

    /// <summary>流式跟随节流时间戳：距上次滚动 &lt;150ms 跳过，避免每 token 触发重排。</summary>
    private DateTime _lastStreamScroll = DateTime.MinValue;

    /// <summary>富文本重算节流：代码回复每 token 全量重分词会卡 UI，按增长量/时间节流。</summary>
    private DateTime _lastFormatRecompute = DateTime.MinValue;
    private int _lastFormattedLen;

    /// <summary>发送队列：agent 忙时发送的消息排队，忙完自动取下一条（移动端聊天不卡输入）。</summary>
    private readonly Queue<QueuedItem> _sendQueue = new();
    private sealed record QueuedItem(string Text, ChatMessage Msg);

    /// <summary>统一消息入口：Add 后裁剪，防消息列表无限增长（镜像 TUI PruneChatHistory）。</summary>
    private void AddMessage(ChatMessage m)
    {
        Messages.Add(m);
        _appAddCount++; // 载入消息在 load 循环后已归零；此后每次新增算一条会话内容（含工具/思考占位，回写时仅取 user/assistant）
        PruneMessages();
    }

    /// <summary>消息列表条数/token 上限：超 MaxChatMessages 或累计估算 token 超 MaxChatTokens 丢最旧。
    /// 条数裁剪每次执行（防列表无限）；token 裁剪是 rune 级全量遍历，降频到每 8 次 Add 一次，
    /// 避免流式高频 Add（工具消息/气泡）时每次 O(n) 重算卡 UI。</summary>
    private int _pruneCounter;
    private void PruneMessages()
    {
        int max = Config.Instance.MaxChatMessages;
        if (max > 0)
            while (Messages.Count > max) Messages.RemoveAt(0);

        if (++_pruneCounter % 8 != 0) return;
        int maxTokens = Config.Instance.MaxChatTokens;
        if (maxTokens > 0)
        {
            int est = 0;
            foreach (var m in Messages) est += ContextManager.EstimateText(m.RawText ?? "");
            while (est > maxTokens && Messages.Count > 1)
            {
                est -= ContextManager.EstimateText(Messages[0].RawText ?? "");
                Messages.RemoveAt(0);
            }
        }
    }

    /// <summary>单条消息内容上限：超限保留尾部窗口 + 截断标记（镜像 TUI CapMessageContent，
    /// 防超长回复/思考内容撑爆 RawText 与富文本渲染）。</summary>
    private static void AppendCapped(StringBuilder sb, string delta)
    {
        int max = Global.MaxSingleMessageChars;
        if (max <= 0 || sb.Length + delta.Length <= max) { sb.Append(delta); return; }
        var combined = sb.ToString() + delta;
        var tail = ContextManager.TruncateTailByRunes(combined, max);
        sb.Clear();
        sb.Append("… 已截断（显示最近内容，旧内容滚动省略）…\n").Append(tail);
    }

    // ── 输入框上方动态状态栏：多状态（空闲/思考/执行工具/等待确认/等待用户/等待子代理/完成/压缩）+ Braille 旋转动画 ──
    private IDispatcherTimer? _statusTimer;
    private int _spinnerFrame;
    private static readonly string[] SpinnerFrames = AgentStatusResolver.SpinnerFrames; // 跨端统一帧集

    private const long CompleteWindowMs = 2500; // 任务完成瞬态窗口（毫秒）
    private AgentStatus _uiState = AgentStatus.Idle;
    private DateTime _completeAt;               // 任务完成时间戳（完成瞬态回落用）
    private string _toolName = "";
    private string _compressStatusText = "";   // 上下文压缩进度（状态栏显示，不进入聊天区）

    /// <summary>内容增长 ≥300 字符或距上次 ≥120ms 才重算富文本（流式中渐进更新，最终 finally 全量）。</summary>
    private bool ShouldRecomputeFormatted(int currentLen)
    {
        var now = DateTime.UtcNow;
        if (currentLen - _lastFormattedLen >= 300 || (now - _lastFormatRecompute).TotalMilliseconds >= 120)
        {
            _lastFormatRecompute = now;
            _lastFormattedLen = currentLen;
            return true;
        }
        return false;
    }

    public ChatPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    /// <summary>
    /// 抽屉作为「浮层」悬浮在聊天列表之上：宽度 = 屏宽 ~62%（上限 300dp、下限 200dp），
    /// 真机 360dp 屏 → 约 223dp，右侧始终露出约 38% 屏聊天且遮罩仅 ~20%，聊天列表清晰可见；
    /// 视觉是「聊天上叠一块侧面板」。每次布局按当前屏宽重算（首次 OnSizeAllocated 的 width
    /// 可能不是最终屏宽——如 540dp 中间值会把 0.72 顶到上限后锁死成过宽抽屉，故不一次性锁死）。
    /// 仅当期望值与现值差 >10dp 才更新，避免动画期间反复触发布局。
    /// </summary>
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0) return;
        var want = Math.Clamp(width * 0.5, 150, 300); // 推开模式：抽屉 ~50% 屏，聊天余一半完整重排
        if (Math.Abs(LeftDrawer.WidthRequest - want) > 10)
        {
            LeftDrawer.WidthRequest = want;
            RightDrawer.WidthRequest = want;
        }
    }

    /// <summary>输入 / 前缀 → 显示常用命令建议列表（输入 /xx 过滤；对齐 Web suggest-box）。</summary>
    private void InputBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        var text = InputBox.Text ?? "";
        if (text.StartsWith('/') && text.Length > 1)
        {
            var q = text[1..].Trim().ToLowerInvariant();
            var items = WayCoder.UI.Shared.CommandBar.Favorites
                .Where(f => string.IsNullOrEmpty(q) || f.Name.Contains('/' + q, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Name)
                .ToList();
            SlashSuggest.ItemsSource = items;
            SlashSuggest.SelectedItem = null; // 防 ItemsSource 重设误触发 SelectionChanged
            SlashSuggest.IsVisible = items.Count > 0;
        }
        else SlashSuggest.IsVisible = false;
    }

    private async void SlashSuggest_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string cmd && !string.IsNullOrEmpty(cmd))
        {
            InputBox.Text = cmd + " ";
            InputBox.CursorPosition = InputBox.Text.Length;
            InputBox.Focus();
            SlashSuggest.SelectedItem = null;
            SlashSuggest.IsVisible = false;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // 注入斜杠命令输出桥：命令执行时把 system/消息 泵回本页消息列表（统一灰色小字）。
        ChatScreen.OnAddSystemMsg = content =>
            MainThread.BeginInvokeOnMainThread(() => AddMessage(new ChatMessage { Role = ChatRole.Tool, RawText = content }));
        ChatScreen.OnAddMessage = (content, role, centered, indent) =>
            MainThread.BeginInvokeOnMainThread(() => AddMessage(new ChatMessage { Role = ChatRole.Tool, RawText = content }));
        ChatScreen.OnClearChat = () =>
            MainThread.BeginInvokeOnMainThread(Messages.Clear);
        // ReviewCommand 等命令把审查 prompt 投递为普通消息 → 桥接发送（走排队）
        ChatScreen.OnEnqueueSubmission = text =>
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                InputBox.Text = text;
                await SendOrQueueAsync();
            });
        RefreshModelBar();
        StartStatusTimer();
        PermissionManager.PermissionPromptStarted += OnPermissionStarted;
        PermissionManager.PermissionPromptResolved += OnPermissionResolved;
        // 上下文压缩进度 → 状态栏（压缩是背景状态，不进入聊天区）
        ContextManager.CompressProgress += OnCompressProgress;
        ContextManager.CompressFinished += OnCompressFinished;
        // 进入时：会话历史页点选/新建 → 先消费 PendingOpenSessionId 切换；否则恢复当前会话
        var pending = PendingOpenSessionId;
        PendingOpenSessionId = null;
        if (pending != null)
            _ = SwitchToSessionAsync(pending); // 载入目标会话（新建 id 无存档 → 空会话）
        else
            _ = EnsureSessionAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _statusTimer?.Stop();
        _statusTimer = null;
        PermissionManager.PermissionPromptStarted -= OnPermissionStarted;
        PermissionManager.PermissionPromptResolved -= OnPermissionResolved;
        ContextManager.CompressProgress -= OnCompressProgress;
        ContextManager.CompressFinished -= OnCompressFinished;
        SaveCurrentSession(); // 退出时记住会话
    }

    /// <summary>保存当前会话（多会话：复用桌面 SessionManager 格式）。空会话不写盘。
    /// 从盘载入的会话（含 tool/system 原始节点）以原始节点为基底合并新消息再存，防重存抹掉共享 schema 的工具结果。</summary>
    private void SaveCurrentSession()
    {
        if (string.IsNullOrEmpty(_currentSessionId) || Messages.Count == 0) return;
        var model = AgentService.CurrentAgent?.LlmClient?.EffectiveModel ?? Config.Instance.Model;
        if (_sessionRaw == null)
        {
            MauiSessions.Save(Messages, model, _currentSessionId);
            return;
        }
        // 有盘载入原始节点：以它为基底，仅追加载入后新增的尾部消息。用 _appAddCount 而非条数边界——
        // PruneMessages 从队首 RemoveAt(0) 会使条数边界失效（导致新消息被丢），而新加消息永远在队尾，
        // TakeLast(_appAddCount) 对裁剪稳健（code-review finding #A）。
        if (_appAddCount > 0)
        {
            var merged = new List<JNode>(_sessionRaw);
            foreach (var m in Messages.TakeLast(Math.Min(_appAddCount, Messages.Count)))
            {
                var node = MauiSessions.ToNode(m);
                if (node != null) merged.Add(node);
            }
            MauiSessions.SaveRaw(merged, model, _currentSessionId);
        }
        else
        {
            // 未新增消息：原样回写原始节点，保留桌面 tool/system（code-review finding #4）
            MauiSessions.SaveRaw(_sessionRaw, model, _currentSessionId);
        }
    }

    /// <summary>恢复上一会话并自动载入（多会话历史：重启回最后打开的会话）。</summary>
    private async Task EnsureSessionAsync()
    {
        try
        {
            var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

            if (Messages.Count > 0) return; // 已在内存（恢复/切回）——提前于 Exists 全目录扫描返回

            // 首次：无记录的会话 → 迁移旧单会话，或新建一个
            _currentSessionId = MauiSessions.CurrentSessionId();
            if (string.IsNullOrEmpty(_currentSessionId) || !MauiSessions.Exists(_currentSessionId))
            {
                _currentSessionId = MauiSessions.MigrateLegacySingleSession(Config.Instance.Model)
                                   ?? MauiSessions.NewId();
                MauiSessions.SetCurrentSessionId(_currentSessionId);
            }

            var loaded = MauiSessions.Load(_currentSessionId);
            if (loaded == null) return; // 空会话/首次：停留空对话

            _sessionRaw = new List<JNode>(loaded.Value.Messages); // 记录原始节点，回写合并保留 tool/system
            foreach (var m in MauiSessions.FromNodes(loaded.Value.Messages))
            {
                m.IsDark = isDark;
                if (m.Role == ChatRole.Assistant && !string.IsNullOrEmpty(m.RawText))
                    m.Formatted = MarkupToFormattedString.Convert(m.RawText, isDark);
                AddMessage(m);
            }
            _appAddCount = 0; // 载入消息不计入「新增」，回写以 _sessionRaw 为基底
            _contextSeeded = false; // 首条消息按本会话历史种入 Agent 上下文
            ScrollToEnd();
        }
        catch { /* 恢复失败静默：保持空对话 */ }
    }

    private void OnPermissionStarted(string _)
    {
        if (_agent.IsRunning) _uiState = AgentStatus.WaitingPermission;
    }

    private void OnPermissionResolved(string _)
    {
        if (_uiState == AgentStatus.WaitingPermission)
            _uiState = _agent.IsRunning ? AgentStatus.Thinking : AgentStatus.Idle;
    }

    /// <summary>启动动态状态栏动画定时器（100ms 一帧旋转图标）。</summary>
    private void StartStatusTimer()
    {
        _statusTimer?.Stop();
        _statusTimer = Dispatcher.CreateTimer();
        _statusTimer.Interval = TimeSpan.FromMilliseconds(100);
        _statusTimer.Tick += (_, _) => TickStatusBar();
        _statusTimer.Start();
    }

    /// <summary>每帧刷新动态状态栏：空闲隐藏；其余状态显示旋转图标 + 状态文本（共享解析器统一文案）。</summary>
    private void TickStatusBar()
    {
        if (_uiState == AgentStatus.Idle)
        {
            AgentStatusBar.IsVisible = false;
            return;
        }
        // 任务完成瞬态 2.5s 后回落
        if (_uiState == AgentStatus.Complete && (DateTime.UtcNow - _completeAt).TotalMilliseconds >= CompleteWindowMs)
        {
            _uiState = _agent.IsRunning ? AgentStatus.Thinking : AgentStatus.Idle;
            if (_uiState == AgentStatus.Idle) { AgentStatusBar.IsVisible = false; return; }
        }
        AgentStatusBar.IsVisible = true;
        _spinnerFrame = (_spinnerFrame + 1) % AgentStatusResolver.SpinnerFrames.Length;
        AgentStatusIcon.Text = AgentStatusResolver.SpinnerFrames[_spinnerFrame];
        var view = AgentStatusResolver.Resolve(new AgentStatusInput(
            // Busy = Agent 实际在运行（完成态 IsRunning=false → 解析器走 RecentComplete 分支显示「任务完成 ✓」，
            // 而非被 Busy 分支短路成工具/思考）
            Busy: _agent.IsRunning,
            ToolName: _toolName,
            Compressing: _uiState == AgentStatus.Compressing,
            WaitingPermission: _uiState == AgentStatus.WaitingPermission,
            WaitingUser: _uiState == AgentStatus.WaitingUser,
            WaitingSubagent: _uiState == AgentStatus.WaitingSubagent,
            Mode: AgentService.CurrentAgent?.WorkMode ?? WorkMode.Build,
            RecentComplete: _uiState == AgentStatus.Complete));
        // 压缩显示带进度的详细文本；其余用解析器统一文字
        AgentStatusText.Text = _uiState == AgentStatus.Compressing ? _compressStatusText : view.Text;
    }

    /// <summary>上下文压缩进度 → 状态栏（压缩是背景状态，不进入聊天区）。</summary>
    private void OnCompressProgress(int layer, string label, double pct)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _compressStatusText = $"🔄 压缩中 [L{layer}/3] {label} {pct:P0}";
            _uiState = AgentStatus.Compressing;
        });
    }

    private void OnCompressFinished()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _compressStatusText = "";
            if (_uiState == AgentStatus.Compressing)
                _uiState = _agent.IsRunning ? AgentStatus.Thinking : AgentStatus.Idle;
        });
    }

    /// <summary>顶部状态区行 1：当前生效模型（点击可切换）。行 2 统计见 <see cref="RefreshStatusBar"/>。</summary>
    private void RefreshModelBar()
    {
        var cfg = Config.Instance;
        // 模型栏只显示当前生效模型，前缀提示通道：big→大模型 / free→自由模型
        ModelBar.Text = $"🧠 {ConnectionConfig.FormatModelChannel(ConnectionConfig.CurrentMainChannel(), cfg.Provider, cfg.Model)}";
        RefreshStatusBar();
    }

    /// <summary>
    /// 顶部状态区：行 1 右侧 ModeBar = 工作模式 / 权限（提到模型同一行，避免行 2 拥挤）；
    /// 行 2 StatusBar = todo / 上下文 / 用量 / 花费。
    /// </summary>
    private void RefreshStatusBar()
    {
        var s = AgentService.GetStatus();
        // agent 未创建（首次启动未发过消息）时 GetStatus()==null：模式/权限改读全局 CurrentMode，
        // 顶栏仍真实反映右抽屉循环后的结果，不再 fallback 成写死的「建造/Ask」让人以为切不了。
        ModeBar.Text = s != null
            ? $"⚙ {s.WorkMode} · 🔐 {s.PermMode}"
            : $"⚙ {WorkModeManager.Format(WorkModeManager.CurrentMode)} · 🔐 {PermName(PermissionManager.CurrentMode)}";
        StatusBar.Text = s == null
            ? "📋 todo ×0"
            : $"📋 todo ×{s.TodoCount} · 上下文 {FormatK(s.ContextUsed)}/{FormatK(s.ContextMax)} · "
              + $"🪙 {FormatK(s.PromptTokens)}+{FormatK(s.CompletionTokens)} · 💰 ${s.Cost?.ToString("F4") ?? "-"}";
    }

    private static string FormatK(int n) => n >= 1000 ? $"{n / 1000.0:F1}k" : n.ToString();

    /// <summary>确认权限显示名（与 AgentService.GetStatus 的 PermMode 文案一致，供 agent 未创建时用）。</summary>
    private static string PermName(PermissionManager.Mode m) => m switch
    {
        PermissionManager.Mode.Yolo => "Yolo",
        PermissionManager.Mode.SmartAuto => "SmartAuto",
        PermissionManager.Mode.Auto => "Auto",
        _ => "Ask",
    };

    /// <summary>点模型条 → 打开模型选择页（TUI ModelPicker 移植：分组+搜索+大/小切换）。</summary>
    private async void OnModelBarTapped(object? sender, TappedEventArgs? e)
        => await Shell.Current.GoToAsync("modelpicker");

    /// <summary>右上 ☰：跳独立「侧栏命令」页（替代抽屉，聊天保持全宽）。</summary>
    private async void OnMenuClicked(object? sender, EventArgs e)
    {
        try { await Shell.Current.GoToAsync("panel"); }
        catch (Exception ex) { ErrorLog.Error("Chat", "侧栏页", ex); }
    }

    /// <summary>左上 ≡：跳独立「会话历史」页（替代抽屉，聊天保持全宽）。</summary>
    private async void OnSessionsBtnClicked(object? sender, EventArgs e)
    {
        try { await Shell.Current.GoToAsync("sessions"); }
        catch (Exception ex) { ErrorLog.Error("Chat", "会话历史页", ex); }
    }

    /// <summary>左抽屉「＋ 新会话」：先存档当前 → 开空会话 → 收起抽屉回聊天。</summary>
    private void OnNewSessionClicked(object? sender, EventArgs e)
    {
        NewSession();
        _ = CloseDrawersAsync();
    }

    /// <summary>点半透明遮罩：收起当前抽屉。</summary>
    private void OnDrawerScrimTapped(object? sender, TappedEventArgs e) => CloseDrawers();

    /// <summary>消息区左缘右滑 → 开左抽屉（会话历史）。手势 Started 即触发（热区 26px，滑动方向意图明确）。</summary>
    private void OnLeftEdgePan(object? sender, PanUpdatedEventArgs e)
    {
        if (e.StatusType != GestureStatus.Started || _drawerAnimating) return;
        if (_leftDrawerOpen) return;
        if (_rightDrawerOpen) { _ = CloseDrawersAsync(); return; } // 右侧开时左滑仅收起，避免误开
        BuildSessionList();
        OpenLeftDrawer();
    }

    /// <summary>消息区右缘左滑 → 开右抽屉（侧边栏命令）。</summary>
    private void OnRightEdgePan(object? sender, PanUpdatedEventArgs e)
    {
        if (e.StatusType != GestureStatus.Started || _drawerAnimating) return;
        if (_rightDrawerOpen) return;
        if (_leftDrawerOpen) { _ = CloseDrawersAsync(); return; }
        BuildRightPanel();
        OpenRightDrawer();
    }

    // ── 抽屉开合：滑入/滑出动画（抽屉初始在屏外；打开同帧 scrim 渐显，关闭反向） ──

    /// <summary>
    /// 开合抽屉时同步平移 MainGrid（聊天界面）——抽屉「推开」内容而不是盖住：
    /// 开左抽屉 MainGrid 右移一个抽屉宽、聊天整体移到抽屉右侧完整可见（不再因文字被盖而"空白"）；
    /// 开右抽屉 MainGrid 左移。MainGrid.TranslationX 与 DrawerLayer 动画同步走同一 easing/时长。
    /// </summary>
    /// <summary>
    /// 开抽屉让 MainHost 让位（Padding 顶到抽屉宽）→ 聊天整体收缩到抽屉另一侧完整重排，
    /// 不被浮层盖住、右侧/左侧不再空白。开左抽屉 Padding.Left=w（聊天移右），开右则 Right。
    /// 抽屉本身仍以覆盖层浮在让出的区域上（滑入动画）。关闭时 Padding 归零。
    /// </summary>

    /// <summary>滑入左抽屉（若右侧开先收起——同屏只留一侧）。</summary>
    private async void OpenLeftDrawer()
    {
        if (_leftDrawerOpen || _drawerAnimating) return;
        if (_rightDrawerOpen) await CloseDrawersAsync();
        _drawerAnimating = true;
        try
        {
            var w = LeftDrawer.WidthRequest;
            MainGrid.Margin = new Thickness(w, 0, 0, 0); // 让位：聊天整体缩到右侧完整重排（Margin 改布局宽）
            DrawerLayer.IsVisible = true;
            LeftDrawer.TranslationX = -w;                 // 抽屉先置屏外再滑入
            DrawerScrim.Opacity = 0;
            await Task.WhenAll(
                LeftDrawer.TranslateToAsync(0, 0, DrawerAnimMs, Easing.CubicOut),
                DrawerScrim.FadeToAsync(1, DrawerAnimMs, Easing.CubicOut));
            _leftDrawerOpen = true;
            _rightDrawerOpen = false;
        }
        catch { /* 页面导航/生命周期中断动画：保持现状不崩溃 */ }
        finally { _drawerAnimating = false; }
    }

    /// <summary>滑入右抽屉（侧边栏命令）。</summary>
    private async void OpenRightDrawer()
    {
        if (_rightDrawerOpen || _drawerAnimating) return;
        if (_leftDrawerOpen) await CloseDrawersAsync();
        _drawerAnimating = true;
        try
        {
            var w = RightDrawer.WidthRequest;
            MainGrid.Margin = new Thickness(0, 0, w, 0); // 让位：聊天收缩到左侧
            DrawerLayer.IsVisible = true;
            RightDrawer.TranslationX = w;                 // 抽屉先置屏外再滑入
            DrawerScrim.Opacity = 0;
            await Task.WhenAll(
                RightDrawer.TranslateToAsync(0, 0, DrawerAnimMs, Easing.CubicOut),
                DrawerScrim.FadeToAsync(1, DrawerAnimMs, Easing.CubicOut));
            _rightDrawerOpen = true;
            _leftDrawerOpen = false;
        }
        catch { }
        finally { _drawerAnimating = false; }
    }

    /// <summary>收起打开的抽屉（抽屉滑回屏外 + MainHost 让位归零 + scrim 淡出 + 隐藏覆盖层）。</summary>
    private async Task CloseDrawersAsync()
    {
        if (!DrawerLayer.IsVisible) { _leftDrawerOpen = _rightDrawerOpen = false; return; }
        if (_drawerAnimating) return;
        _drawerAnimating = true;
        try
        {
            var anims = new List<Task>();
            if (_leftDrawerOpen)
                anims.Add(LeftDrawer.TranslateToAsync(-LeftDrawer.WidthRequest, 0, DrawerAnimMs, Easing.CubicIn));
            if (_rightDrawerOpen)
                anims.Add(RightDrawer.TranslateToAsync(RightDrawer.WidthRequest, 0, DrawerAnimMs, Easing.CubicIn));
            anims.Add(DrawerScrim.FadeToAsync(0, DrawerAnimMs, Easing.CubicIn));
            await Task.WhenAll(anims);
            DrawerLayer.IsVisible = false;
            MainGrid.Margin = new Thickness(0); // 聊天恢复全宽
            _leftDrawerOpen = _rightDrawerOpen = false;
        }
        catch { }
        finally { _drawerAnimating = false; }
    }

    /// <summary>收起抽屉（fire-and-forget；内部消化异常）。</summary>
    private void CloseDrawers() => _ = CloseDrawersAsync();

    /// <summary>
    /// MainHost.Padding 让位会改变 CollectionView 可用宽度；Android 上 CollectionView
    /// 对父级 padding 动态变化不自动重排已渲染内容（保持空白），此处重设 ItemsSource 强制其重绘。
    /// </summary>
    private void ForceRelayoutList()
    {
        Dispatcher.Dispatch(() =>
        {
            if (MsgList == null) return;
            var src = MsgList.ItemsSource;
            MsgList.ItemsSource = null;
            MsgList.ItemsSource = src;
        });
    }

    // ── 抽屉内容构建：左=会话历史，右=命令与模式（每次打开前重建，时间/高亮/当前值实时刷新） ──

    private void BuildRightPanel() => PopulateRightPanel();
    private void BuildSessionList() => PopulateSessionList();

    /// <summary>会话切换/新建后刷新左抽屉列表（仅抽屉开着时重建；关着则下次打开自然重建）。</summary>
    private void RefreshSessionList()
    {
        if (!_leftDrawerOpen || _drawerAnimating) return;
        PopulateSessionList();
    }

    private static Color? ColorKey(string key)
        => Application.Current?.Resources.TryGetValue(key, out var v) == true ? v as Color : null;

    /// <summary>左抽屉内容：会话历史列表（最新在前）。当前会话高亮，点击切换。</summary>
    private void PopulateSessionList()
    {
        LeftBody.Children.Clear();
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var main = ColorKey(isDark ? "MainTextDark" : "MainTextLight");
        var muted = ColorKey(isDark ? "MutedTextDark" : "MutedTextLight");
        var primary = ColorKey("Primary") ?? Colors.DodgerBlue;

        var sessions = MauiSessions.List(50);
        if (sessions.Count == 0)
        {
            LeftBody.Add(new Label
            {
                Text = "暂无历史会话",
                FontSize = 12,
                TextColor = muted,
                Margin = new Thickness(16, 24),
            });
            return;
        }

        foreach (var s in sessions)
        {
            var current = s.Id == _currentSessionId;
            var row = new Border
            {
                Padding = new Thickness(12, 9),
                StrokeThickness = 0,
                BackgroundColor = current ? new Color(primary.Red, primary.Green, primary.Blue, 0.14f) : null,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            };
            var title = string.IsNullOrWhiteSpace(s.Preview) ? "（空会话）" : s.Preview;
            row.Content = new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Label
                    {
                        Text = title,
                        FontSize = 13,
                        FontAttributes = current ? FontAttributes.Bold : FontAttributes.None,
                        TextColor = current ? primary : main,
                        LineBreakMode = LineBreakMode.TailTruncation,
                        MaxLines = 1,
                    },
                    new Label
                    {
                        Text = $"{MauiSessions.RelativeTime(s.SavedAt)} · {s.MessageCount} 条消息",
                        FontSize = 11,
                        TextColor = muted,
                    },
                },
            };
            var id = s.Id;
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, _) =>
            {
                try { await SwitchToSessionAsync(id); }
                catch (Exception ex) { ErrorLog.Error("Chat", "切换会话", ex); }
            };
            row.GestureRecognizers.Add(tap);
            LeftBody.Add(row);
        }
    }

    /// <summary>经济模式显示名（枚举顺序 Off→Auto→On→Extreme，非直觉序）。</summary>
    private static string EconomyName(EconomyMode m) => m switch
    {
        EconomyMode.On => "开",
        EconomyMode.Auto => "自动",
        EconomyMode.Extreme => "极致",
        _ => "关",
    };

    /// <summary>命令跳转：先关抽屉（避免盖层残留于下一页）再导航；异常落日志不崩溃。</summary>
    private async Task NavThen(string route)
    {
        await CloseDrawersAsync();
        try { await Shell.Current.GoToAsync(route); }
        catch (Exception ex) { ErrorLog.Error("Chat", $"侧栏导航 {route}", ex); }
    }

    /// <summary>右抽屉内容：模型横幅 + 命令区 + 模式/权限/经济循环行 + 关于。</summary>
    private void PopulateRightPanel()
    {
        RightBody.Children.Clear();
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var main = ColorKey(isDark ? "MainTextDark" : "MainTextLight");
        var muted = ColorKey(isDark ? "MutedTextDark" : "MutedTextLight");
        var inputBg = ColorKey(isDark ? "InputBgDark" : "InputBgLight");
        var primary = ColorKey("Primary") ?? Colors.DodgerBlue;
        var cfg = Config.Instance;

        // 模型横幅（点按 → 模型选择页）
        var modelText = ConnectionConfig.FormatModelChannel(
            ConnectionConfig.CurrentMainChannel(), cfg.Provider, cfg.Model);
        var modelCard = new Border
        {
            Padding = new Thickness(14, 12),
            StrokeThickness = 0,
            BackgroundColor = new Color(primary.Red, primary.Green, primary.Blue, 0.10f),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
            Content = new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Label
                    {
                        Text = "🧠 " + modelText,
                        FontSize = 14,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = primary,
                        LineBreakMode = LineBreakMode.TailTruncation,
                        MaxLines = 1,
                    },
                    new Label { Text = "当前模型 · 点按选择", FontSize = 11, TextColor = muted },
                },
            },
        };
        var pickModel = new TapGestureRecognizer();
        pickModel.Tapped += async (_, _) => await NavThen("modelpicker");
        modelCard.GestureRecognizers.Add(pickModel);
        RightBody.Add(modelCard);

        // 命令区
        RightBody.Add(new Label
        {
            Text = "命令",
            FontSize = 11,
            TextColor = muted,
            Margin = new Thickness(2, 8, 2, 2),
        });
        RightBody.Add(CommandRow("🗂 供应商 / 模型", null, async () => await NavThen("models"), inputBg, main, muted));
        RightBody.Add(CommandRow("🔄 代码同步", null, async () => await NavThen("gitsync"), inputBg, main, muted));
        RightBody.Add(CommandRow("📌 任务管理", null,
            async () => { try { await CloseDrawersAsync(); await ShowTasksAsync(); } catch (Exception ex) { ErrorLog.Error("Chat", "任务管理", ex); } },
            inputBg, main, muted));

        // 模式区：值行点按循环（重绘右面板刷新当前值；保持抽屉开可连点）
        RightBody.Add(new Label
        {
            Text = "模式",
            FontSize = 11,
            TextColor = muted,
            Margin = new Thickness(2, 8, 2, 2),
        });
        // 值行直接读全局 CurrentMode（不依赖 agent 实例：agent 未创建时 GetStatus()==null，
        // 否则循环后行内值恒显示 fallback「建造/Ask」，看起来像「切不了」）
        RightBody.Add(CommandRow("⚙ 工作模式", WorkModeManager.Format(WorkModeManager.CurrentMode),
            () => { CycleWorkMode(); PopulateRightPanel(); }, inputBg, main, muted));
        RightBody.Add(CommandRow("🔐 确认权限", PermName(PermissionManager.CurrentMode),
            () => { CyclePermission(); PopulateRightPanel(); }, inputBg, main, muted));
        RightBody.Add(CommandRow("💸 经济模式", EconomyName(cfg.EconomyMode),
            () => { cfg.CycleEconomy(); SaveModes(); RefreshModelBar(); PopulateRightPanel(); }, inputBg, main, muted));

        // 其它
        RightBody.Add(new Label
        {
            Text = "其它",
            FontSize = 11,
            TextColor = muted,
            Margin = new Thickness(2, 8, 2, 2),
        });
        RightBody.Add(CommandRow("ℹ️ 关于", null, async () => await NavThen("about"), inputBg, main, muted));
    }

    /// <summary>命令按钮行卡片：标题（+ 可选副文本当前值），整行点击执行 onTap。</summary>
    private static Border CommandRow(string title, string? sub, Action onTap, Color? bg, Color? fg, Color? subColor)
    {
        var inner = new VerticalStackLayout { Spacing = 1 };
        inner.Children.Add(new Label
        {
            Text = title,
            FontSize = 14,
            TextColor = fg,
            LineBreakMode = LineBreakMode.TailTruncation,
        });
        if (!string.IsNullOrEmpty(sub))
            inner.Children.Add(new Label { Text = sub, FontSize = 11, TextColor = subColor });

        var row = new Border
        {
            Padding = new Thickness(12, 10),
            StrokeThickness = 0,
            BackgroundColor = bg,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
            Content = inner,
        };
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => { try { onTap(); } catch (Exception ex) { ErrorLog.Error("Chat", "侧栏命令", ex); } };
        row.GestureRecognizers.Add(tap);
        return row;
    }

    /// <summary>循环切换工作模式（建造→计划→聊天）并同步到 Agent，持久化供下次启动恢复。</summary>
    private void CycleWorkMode()
    {
        WorkModeManager.CycleNext();
        if (AgentService.CurrentAgent is { } a) a.WorkMode = WorkModeManager.CurrentMode;
        SaveModes();
        RefreshModelBar();
    }

    /// <summary>循环切换确认轴权限（Ask→Auto→SmartAuto→Yolo），持久化。</summary>
    private void CyclePermission()
    {
        PermissionManager.CycleMode();
        SaveModes();
        RefreshModelBar();
    }

    /// <summary>把三种模式落到磁盘（手机无快捷键，记住选择，下次启动恢复）。</summary>
    internal void SaveModes()
        => Services.MauiModeStore.Save(WorkModeManager.CurrentMode, PermissionManager.CurrentMode, Config.Instance.EconomyMode);

    /// <summary>会话管理：继续上次会话 / 新的会话。</summary>
    /// <summary>旧「会话管理」入口（☰ 曾用）：保留为新会话快捷（左抽屉会话列表为正式入口）。</summary>
    private async Task ManageSessionsAsync()
    {
        var action = await DisplayActionSheetAsync("会话管理", "取消", null, "新建会话");
        if (action == "新建会话")
            NewSession();
    }

    /// <summary>新建会话：先停/等在途轮结束（其 finally 已在旧 id 落盘），再开空会话（新 id），刷新左抽屉列表。</summary>
    internal async Task NewSessionAsync()
    {
        await AwaitActiveRoundEndAsync();
        SaveCurrentSession(); // 兜底：非运行轮当前内容也存档
        Messages.Clear();
        _sessionRaw = null;      // 新会话：无盘载入原始节点
        _appAddCount = 0;
        _contextSeeded = false;  // 下条消息按新会话（空上下文）重新种入 Agent
        _currentSessionId = MauiSessions.NewId();
        MauiSessions.SetCurrentSessionId(_currentSessionId);
        if (AgentService.CurrentAgent is { } a) a.Reset(); // 清空 LLM 上下文，隔离旧会话历史（finding #2）
        ScrollToEnd();
        RefreshSessionList();
    }

    /// <summary>新建会话（同步入口，历史遗留；推荐 <see cref="NewSessionAsync"/>）。</summary>
    internal void NewSession() => _ = NewSessionAsync();

    /// <summary>切换会话：等在途轮彻底结束（finally 已在旧 id 落盘）→ 保存 → 载入目标 → 更新当前 id。</summary>
    internal async Task SwitchToSessionAsync(string sessionId)
    {
        if (sessionId == _currentSessionId) return;
        await AwaitActiveRoundEndAsync(); // 等旧轮 finally 完成（旧 _currentSessionId 下 FreezeSeg+Save 已跑）
        SaveCurrentSession();
        Messages.Clear();
        _currentSessionId = sessionId;
        MauiSessions.SetCurrentSessionId(sessionId);
        var loaded = MauiSessions.Load(sessionId);
        _sessionRaw = loaded != null ? new List<JNode>(loaded.Value.Messages) : null; // 记录原始节点，回写合并保留 tool/system
        if (loaded != null)
        {
            var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
            foreach (var m in MauiSessions.FromNodes(loaded.Value.Messages))
            {
                m.IsDark = isDark;
                if (m.Role == ChatRole.Assistant && !string.IsNullOrEmpty(m.RawText))
                    m.Formatted = MarkupToFormattedString.Convert(m.RawText, isDark);
                AddMessage(m);
            }
        }
        _appAddCount = 0;
        _contextSeeded = false; // 下条消息按本会话历史重新种入 Agent 上下文（隔离旧会话，finding #2）
        ScrollToEnd();
        RefreshModelBar();
        RefreshSessionList();
    }

    /// <summary>任务管理：展示当前 todo 列表。</summary>
    private async Task ShowTasksAsync()
    {
        var items = new List<string>();
        try { items = WayCoder.Tools.TodoTool.Items.Select(t => $"{t.Status} · {t.Title}").ToList(); } catch { }
        if (items.Count == 0)
        {
            await DisplayAlertAsync("任务管理", "暂无任务", "关闭");
            return;
        }
        await DisplayActionSheetAsync($"任务列表（{items.Count}）", "关闭", null, items.Take(20).ToArray());
    }

    /// <summary>发送按钮（单按钮）：空闲=发送；忙时点一下=停止当前任务（取消本轮 + 清空排队）。
    /// 忙时想发下一条消息用虚拟键盘「发送」键（OnEditorCompleted，忙时进队列）。</summary>
    private async void OnSendClicked(object? sender, EventArgs e)
    {
        if (_agent.IsRunning) { StopCurrent(); return; }
        await SendOrQueueAsync();
    }

    /// <summary>虚拟键盘「发送」键：空闲=发送；忙时=进队列（消息立即上屏标「排队中」，忙完自动执行）。</summary>
    private async void OnEditorCompleted(object? sender, EventArgs e)
    {
        await SendOrQueueAsync();
    }

    /// <summary>发送 / 排队公共入口：斜杠命令即时执行；忙时消息进队列（对齐桌面端回车语义）。</summary>
    private async Task SendOrQueueAsync()
    {
        var text = InputBox.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        // 斜杠命令：/ 前缀 → 解析执行（对齐桌面端 54 命令），不再当普通消息发给大模型。
        if (text.StartsWith('/'))
        {
            var (cmd, args) = SlashCommandRegistry.Match(text);
            if (cmd != null)
            {
                InputBox.Text = "";
                AddMessage(new ChatMessage { Role = ChatRole.User, RawText = text });
                try
                {
                    // 依赖 ProgramContext.Agent/LLM/Config 的命令（/tokens /model /mode /compact 等）
                    // 在首次发普通消息前会拿到「未初始化」。先懒建 Agent 注入全局上下文，保证命令首用即正常。
                    _agent.EnsureAgent();
                    await cmd.ExecuteAsync(args, _screen);
                }
                catch (Exception ex)
                {
                    ErrorLog.Error("Chat", $"命令 {cmd.Name} 执行异常", ex);
                    AddMessage(new ChatMessage { Role = ChatRole.Tool, RawText = $"⚠️ {ex.Message}" });
                }
                ScrollToEnd();
                return;
            }
        }

        // 未配置 Key 时引导去设置页（Key 存于 ApiKeyStore 按服务商，见 AgentService.HasUsableKey）
        if (!AgentService.HasUsableKey())
        {
            var action = await DisplayActionSheetAsync("尚未配置 API Key", "稍后", null, "去设置");
            if (action == "去设置") await Shell.Current.GoToAsync("//settings");
            return;
        }

        InputBox.Text = "";
        if (_agent.IsRunning)
        {
            // 忙 → 排队：消息立即可见并标「排队中」，agent 忙完自动取下一条。输入永不卡死。
            // 队列防无限增长：满则丢最旧（对齐 Global.MaxPendingSubmissions）。
            // 被丢消息已上屏且带「排队中…」，改标记为「已丢弃」，避免永久残留误导用户并污染会话存档。
            while (_sendQueue.Count >= Global.MaxPendingSubmissions)
            {
                var dropped = _sendQueue.Dequeue();
                if (dropped.Msg != null && !string.IsNullOrEmpty(dropped.Msg.RawText))
                    dropped.Msg.RawText = dropped.Msg.RawText.Replace("⏳ 排队中…", "❌ 已丢弃（排队已满）");
            }
            var msg = new ChatMessage { Role = ChatRole.User, RawText = text + "\n⏳ 排队中…" };
            _sendQueue.Enqueue(new QueuedItem(text, msg));
            AddMessage(msg);
            ScrollToEnd();
            return;
        }

        await ProcessQueueAsync(text, firstUserMsg: null);
    }

    /// <summary>停止当前一轮 + 清空排队（用户点停止 = 全部停，不只是当前轮）。
    /// 否则队列下一条在 RunOneMessageAsync 取消返回后仍会被 ProcessQueueAsync 取走执行，
    /// 用户以为全停、实际排队消息继续跑。</summary>
    private void StopCurrent()
    {
        _cts?.Cancel();
        DrainSendQueue("❌ 已停止（不再执行）");
    }

    /// <summary>丢弃发送队列并标记排队消息（停止/会话切换时调用）。防旧会话排队消息在切换后被取走执行并写入新会话。</summary>
    private void DrainSendQueue(string marker)
    {
        while (_sendQueue.Count > 0)
        {
            var dropped = _sendQueue.Dequeue();
            if (dropped.Msg != null && !string.IsNullOrEmpty(dropped.Msg.RawText))
                dropped.Msg.RawText = dropped.Msg.RawText.Replace("⏳ 排队中…", marker);
        }
    }

    /// <summary>首次发送前把当前会话历史注入 Agent LLM 上下文（盘载入历史→LLM 上下文；新建空会话→清空）。
    /// 每会话只种一次（_contextSeeded），切换/新建后重置——防旧会话历史污染新会话（code-review finding #2）。</summary>
    private void EnsureContextSeeded()
    {
        if (_contextSeeded) return;
        _contextSeeded = true;
        var agent = _agent.EnsureAgent();
        agent.Messages = _sessionRaw != null ? new List<JNode>(_sessionRaw) : new();
    }

    /// <summary>串行处理发送队列：发完一条取下一条，直到队列空。firstUserMsg 为 null 表示首条需新建用户气泡。</summary>
    private async Task ProcessQueueAsync(string first, ChatMessage? firstUserMsg)
    {
        EnsureContextSeeded(); // 首条消息：把当前会话历史注入 Agent LLM 上下文（每组会话只种一次）
        var text = first;
        var userMsg = firstUserMsg;
        var sessionAtStart = _currentSessionId; // 队列只属于发起时所在会话
        while (true)
        {
            // 队列执行期间会话被切换（屏障已丢弃余队）→ 不再向新会话发送旧会话消息
            if (_currentSessionId != sessionAtStart) break;
            if (userMsg == null)
            {
                userMsg = new ChatMessage { Role = ChatRole.User, RawText = text };
                AddMessage(userMsg);
                ScrollToEnd(); // 发送后立即滚到底，保证刚发的消息可见
            }
            else
            {
                userMsg.RawText = text + "\n📤 发送中…";   // 排队消息 → 轮到它了
            }

            await RunOneMessageAsync(text);
            userMsg.RawText = text; // 任务完成：还原为纯文本（去掉「📤 发送中…」标记，防残留到会话历史）

            if (_sendQueue.Count == 0) break;
            var next = _sendQueue.Dequeue();
            text = next.Text;
            userMsg = next.Msg;
            ScrollToEnd();
        }
    }

    /// <summary>单轮对话：流式渲染 + 思考/正文分离 + 工具消息 + 摘要。返回后由 ProcessQueueAsync 取下一条。</summary>
    private async Task RunOneMessageAsync(string text)
    {
        _toolGroup = null; // 新轮独立分组（防上轮遗留组把本轮首工具错误并入）
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        // 思考 / 正文 / 工具按时间交错：思考为独立「思考泡泡」（一行「已思考 N 秒」，点开看全文），
        // 正文切「段」（每段独立气泡），工具组插在段间 → 💭思考 / AI1 / 工具1 / AI2 / 工具2…。
        // 思考泡泡惰性建（首个推理字符才 Add，空思考不发泡）；正文段同样收到正文 token 才建；
        // 工具到来先把当前正文段冻结，之后的新正文另起一段。
        var inReasoning = false;
        ChatMessage? thinkMsg = null;        // 当前思考泡泡（结束冻结后置 null；再次思考开新泡）
        StringBuilder? thinkSb = null;       // 当前思考块累积（结束时落 thinkMsg.Reasoning 供详情页）
        DateTime thinkStart = DateTime.MinValue;   // 本块思考开始（标题秒数）
        DateTime thinkLastCap = DateTime.MinValue; // 思考泡泡标题刷新节流
        ChatMessage? seg = null;             // 当前流式正文段气泡（冻结后置 null，下段新开）
        StringBuilder? segSb = null;         // 当前段正文累积（段创建时同步 new）
        bool interruptSinceTool = false;     // 自上个工具以来是否出现过内容（新思考块 / 正文段）→ 下个工具新开组

        void FinishThink()
        {
            if (thinkMsg == null) return;
            var elapsed = thinkStart == DateTime.MinValue ? 1.0 : (DateTime.UtcNow - thinkStart).TotalSeconds;
            thinkMsg.ThinkingSeconds = Math.Max(1, (int)Math.Round(elapsed));
            thinkMsg.Reasoning = thinkSb?.ToString() ?? "";
            thinkMsg.HasReasoning = thinkMsg.Reasoning.Length > 0; // 有内容才可点开详情页
            thinkMsg.RawText = $"💭 已思考 {thinkMsg.ThinkingSeconds} 秒";
            thinkMsg = null;
            thinkSb = null;
        }

        void FreezeSeg()
        {
            // 段切换先重置富文本节流：短段不继承上个长段的 _lastFormattedLen（否则字符门禁失效，
            // 新段开头 ~120ms 的 token 被压制不渲染——code-review finding）
            _lastFormattedLen = 0;
            _lastFormatRecompute = DateTime.MinValue;
            if (seg == null) return;
            seg.IsStreaming = false;
            var raw = segSb?.ToString() ?? "";
            seg.RawText = raw;
            seg.Formatted = MarkupToFormattedString.Convert(raw, isDark);
            seg = null;
            segSb = null;
        }

        _cts = new CancellationTokenSource();
        var round = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _activeRound = round;
        var roundSessionId = _currentSessionId; // 记录发起轮次的会话：超时后旧轮在切换后才跑 → 只冻结自身，不污染新会话
        AgentService.SetActiveCts(_cts); // 注册给 App 生命周期：切后台（来电/Home/锁屏）时取消在途请求
        SendBtn.Text = "■"; // 忙时按钮 = 停止
        var sw = System.Diagnostics.Stopwatch.StartNew();
        bool cancelled = false;

        try
        {
            await _agent.ChatAsync(text,
                token =>
                {
                    // 过滤上下文压缩进度文本（🔄 [x/3]...）：压缩是背景状态，
                    // 进度已由 CompressProgress 事件进状态栏，这里不进入聊天内容
                    if (token.StartsWith("🔄 [", StringComparison.Ordinal))
                        return;
                    _uiState = AgentStatus.Thinking;
                    _toolName = ""; // 工具结束回到思考：清工具名（否则思考中残留上次工具）
                    if (inReasoning)
                    {
                        if (token == "«/»" || token == "«/»\n")
                        {
                            inReasoning = false; // 思考块结束 → 冻结泡泡（标题「已思考 N 秒」）
                            FinishThink();
                        }
                        else
                        {
                            // 首个真实推理字符 → 惰性建思考泡泡（先一行占位，标题随思考实时计秒）
                            if (thinkMsg == null)
                            {
                                thinkMsg = new ChatMessage { Role = ChatRole.Thinking, IsDark = isDark, RawText = "💭 思考中…" };
                                thinkSb = new StringBuilder();
                                thinkStart = DateTime.UtcNow;
                                thinkLastCap = DateTime.MinValue;
                                interruptSinceTool = true; // 新思考块出现 = 内容间断：此前的工具组到此为止，下个工具新开组
                                AddMessage(thinkMsg);
                            }
                            AppendCapped(thinkSb!, token);
                            // 每秒刷新：标题「思考中 Ns」+ 推理全文落到泡泡（点开详情页能看进行中半截）
                            var nowThink = DateTime.UtcNow;
                            if ((nowThink - thinkLastCap).TotalSeconds >= 1)
                            {
                                thinkLastCap = nowThink;
                                thinkMsg.RawText = $"💭 思考中 {Math.Max(1, (int)(nowThink - thinkStart).TotalSeconds)}s";
                                thinkMsg.Reasoning = thinkSb!.ToString();
                                thinkMsg.HasReasoning = thinkSb.Length > 0;
                            }
                        }
                    }
                    else
                    {
                        if (token == "«dim»" || token == "\n«dim»")
                        {
                            inReasoning = true;   // 思考块开始（下个推理字符建泡泡）
                        }
                        else
                        {
                            // 正文 token：惰性建段。工具打断后 seg 为 null，新正文在此另起气泡 → 与工具组交错
                            if (seg == null)
                            {
                                seg = new ChatMessage { Role = ChatRole.Assistant, IsStreaming = true };
                                segSb = new StringBuilder();
                                interruptSinceTool = true; // 工具后出现正文 = 内容间断，下一工具开新组
                                AddMessage(seg);
                            }
                            AppendCapped(segSb!, token);
                            if (ShouldRecomputeFormatted(segSb!.Length))
                                seg.Formatted = MarkupToFormattedString.Convert(segSb.ToString(), isDark);
                            FollowStreamScroll();   // 流式跟随：正文滚动
                        }
                    }
                },
                (name, summary) =>
                {
                    // 按工具名分派：ask_user_question=等待用户回复、agent=等待子代理、其余=使用工具中
                    _uiState = name switch
                    {
                        "ask_user_question" => AgentStatus.WaitingUser,
                        "agent" => AgentStatus.WaitingSubagent,
                        _ => AgentStatus.ToolRunning,
                    };
                    _toolName = name;
                    // 工具到来：先把当前正文段冻结成正式消息（若正在流式写正文）→ 工具组紧随其后，
                    // 实现「AI段 / 工具组」按时间交错。工具后新正文会另起一段气泡。
                    FreezeSeg();
                    // 分组：自上个工具以来没出现内容（连续纯工具流）并入当前组；
                    // 出现过内容（新思考块 / 正文段）则新开一组 → 工具与思考、对话都按时间交错。
                    if (_toolGroup == null || interruptSinceTool)
                    {
                        _toolGroup = new ChatMessage { Role = ChatRole.Tool, IsDark = isDark };
                        AddMessage(_toolGroup);
                    }
                    interruptSinceTool = false;
                    _toolGroup.ToolCalls.Add(new ToolCallItem
                    {
                        Name = name,
                        Summary = summary,
                        FilePath = ExtractFilePath(summary),
                        IsDark = isDark,
                    });
                    _toolGroup.RawText = $"🔧 工具调用:{_toolGroup.ToolCount} 次";
                },
                output =>
                {
                    if (_toolGroup == null || _toolGroup.ToolCalls.Count == 0) return;
                    // 工具输出防无限增长：超上限停止追加并加标记（对齐 Global.MaxSingleMessageChars）。
                    var last = _toolGroup.ToolCalls[^1];
                    if (last.Detail.Length < Global.MaxSingleMessageChars)
                        last.Detail += output;
                    else if (!last.Detail.EndsWith("… 已截断…", StringComparison.Ordinal))
                        last.Detail += "\n… 已截断（工具输出过长，停止追加）…";
                    RefreshStatusBar(); // 工具输出阶段统计变化
                },
                _cts.Token);
        }
        catch (OperationCanceledException) { cancelled = true; /* 用户停止 */ }
        catch (Exception ex)
        {
            // 落盘完整堆栈，便于 adb run-as 读 logs/error_*.log 定位（移动端 logcat 不打 .NET 异常）
            ErrorLog.Error("Chat", "对话异常", ex);
            AddMessage(new ChatMessage { Role = ChatRole.Tool, RawText = $"⚠️ {ex.Message}" });
        }
        finally
        {
            // 超时放弃切换后，旧轮 continue 可能在新建会话后跑：此时只冻结自身已生成片段，
            // 不再写新会话（SaveCurrentSession/摘要/清 CTS/清状态），否则污染新会话，还清掉新轮的取消令牌。
            bool moved = _currentSessionId != roundSessionId; // 僵尸轮（切换已越过本轮）→ 不碰新会话
            FreezeSeg();  // 收尾：冻结未被打断的最后正文段（取消时保留已生成片段）
            FinishThink(); // 收尾：思考未闭合（取消/异常）也冻结成「已思考 N 秒」泡泡
            if (!moved)
            {
                SendBtn.Text = "↑"; // 空闲恢复 = 发送
                AgentService.SetActiveCts(null);
                _cts = null;
                _uiState = cancelled ? AgentStatus.Idle : AgentStatus.Complete; // 任务完成瞬态（取消/异常直接回空闲）
                if (!cancelled) _completeAt = DateTime.UtcNow;
                RefreshStatusBar();
                SaveCurrentSession(); // 每轮结束落盘，退出/重启可恢复

                // 任务完成摘要：用时 / prompt+completion token / 费用（用户主动停止或无消耗则跳过）
                if (!cancelled)
                {
                    sw.Stop();
                    var llm = AgentService.CurrentAgent?.LlmClient;
                    var used = (llm?.TaskPromptTokens ?? 0) + (llm?.TaskCompletionTokens ?? 0);
                    if (llm != null && used > 0)
                    {
                        var cost = llm.TaskCost;
                        var summary = $"⏱ {sw.Elapsed.TotalSeconds:F1}s · 🪙 {llm.TaskPromptTokens:N0} prompt + {llm.TaskCompletionTokens:N0} completion · 💰 ${cost?.ToString("F4") ?? "-"}";
                        AddMessage(new ChatMessage { Role = ChatRole.Tool, RawText = summary });
                    }
                }

                ScrollToEnd();
            }
            round.TrySetResult(true); // 通知会话切换/新建：本轮已彻底结束（旧 id 下已保存）
            if (ReferenceEquals(_activeRound, round)) _activeRound = null; // 防超时放弃后旧轮误清已接替的新轮
        }
    }

    /// <summary>等待在途一轮彻底结束（供会话切换/新建屏障）：同步丢弃本会话排队消息 + 取消在途轮，
    /// 再等 finally 完成——其已在旧 _currentSessionId 下 FreezeSeg + SaveCurrentSession 落盘、残余回调跑完，
    /// 随后才能安全切 id。有超时兜底：被取消的工具若不理会 token 也不永久挂起切换（见 finding #1/#3）。</summary>
    private async Task AwaitActiveRoundEndAsync()
    {
        // 必须同步清空排队消息（在 await 之前）：否则旧会话队列里的消息会在切换后被 ProcessQueueAsync
        // 的续体取走执行并写入新会话（StopCurrent→AwaitActiveRoundEndAsync 回归，finding #1）。
        DrainSendQueue("❌ 已停止（会话已切换，不再执行）");
        try { _cts?.Cancel(); } catch { }
        var done = _activeRound;
        if (done == null) return; // 无在途轮（空闲/已彻底结束）
        try
        {
            var completed = await Task.WhenAny(done.Task, Task.Delay(TimeSpan.FromSeconds(10)));
            if (completed != done.Task)
                ErrorLog.Error("Chat", "切换/新建会话等待在途轮结束超时(>10s)，放弃等待继续切换");
        }
        catch { }
    }

    /// <summary>智能滚动：仅在列表接近底部时才跟随到底，用户上翻历史时不打断浏览。</summary>
    private void ScrollToEnd()
    {
        if (Messages.Count > 0 && _isNearBottom)
            MsgList.ScrollTo(Messages.Count - 1, position: ScrollToPosition.End, animate: false);
    }

    /// <summary>流式跟随：接近底部才滚到底（150ms 节流，供 token 回调每 token 调用，避免重排抖动）。</summary>
    private void FollowStreamScroll()
    {
        if (Messages.Count == 0 || !_isNearBottom) return;
        var now = DateTime.UtcNow;
        if ((now - _lastStreamScroll).TotalMilliseconds < 150) return;
        _lastStreamScroll = now;
        MsgList.ScrollTo(Messages.Count - 1, position: ScrollToPosition.End, animate: false);
    }

    /// <summary>从工具摘要里解析 file_path= 值（供语法高亮语言推断；摘要被截断时尽力取扩展名）。</summary>
    private static string? ExtractFilePath(string summary)
    {
        const string key = "file_path=";
        var idx = summary.IndexOf(key, StringComparison.Ordinal);
        if (idx < 0) return null;
        var start = idx + key.Length;
        var end = summary.IndexOf(',', start);
        var p = (end < 0 ? summary[start..] : summary[start..end]).Trim();
        return p.Length == 0 ? null : p;
    }

    /// <summary>思考泡泡点击：弹 ReasoningDetailPage 看完整推理（泡泡有一行字，内容点开才显示）。</summary>
    private async void OnShowReasoning(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject view && view.BindingContext is ChatMessage m && m.HasReasoning)
        {
            ReasoningDetailPage.Target = m;
            await Shell.Current.GoToAsync("reasoning");
        }
    }

    /// <summary>「工具调用:N 次」点击：弹 ToolCallsDetailPage 子页看每个工具详情。</summary>
    private async void OnShowToolCalls(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject view && view.BindingContext is ChatMessage m && m.HasToolCalls)
        {
            ToolCallsDetailPage.Target = m;
            await Shell.Current.GoToAsync("toolcalls");
        }
    }

    /// <summary>跟踪列表是否接近底部（智能滚动判定依据）；不在底部时显示浮动「滚到底」按钮。</summary>
    private void OnMsgListScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        _isNearBottom = e.LastVisibleItemIndex >= Messages.Count - 2;
        // 手动上翻离开底部 → 取消自动滚动 + 显示浮动按钮；回到底部 → 自动滚动恢复 + 按钮隐藏
        JumpBottomBtn.IsVisible = !_isNearBottom;
    }

    /// <summary>浮动按钮：滚到底部并恢复自动滚动（隐藏按钮）。</summary>
    private void OnJumpBottomClicked(object? sender, EventArgs e)
    {
        _isNearBottom = true;
        if (Messages.Count > 0)
            MsgList.ScrollTo(Messages.Count - 1, position: ScrollToPosition.End, animate: true);
        JumpBottomBtn.IsVisible = false;
    }

    /// <summary>
    /// 圆形加号：语音/图片的统一入口。点按弹出菜单（语音输入 / 选音频转录 / 拍照看图 / 从相册选图）；
    /// 录音中再点 = 停止并转录。录音/图片均落沙箱 workspace，复用主工程 <see cref="TranscribeAudioTool"/> / vision 队列。
    /// </summary>
    private async void OnAddClicked(object? sender, EventArgs e)
    {
        // 正在录音 → 停止并转录
        if (AudioRecorder.IsRecording)
        {
            AddBtn.Text = "＋";
            var path = await AudioRecorder.StopAsync();
            if (path != null) await TranscribeAsync(path);
            return;
        }

        var action = await DisplayActionSheetAsync("添加", "取消", null,
            "🎤 语音输入", "📁 选择音频转录", "📷 拍照看图", "🖼 从相册选图");
        switch (action)
        {
            case "🎤 语音输入":
                await StartRecordingAsync();
                break;
            case "📁 选择音频转录":
                await PickAndTranscribeAsync();
                break;
            case "📷 拍照看图":
                await AddPhotoAsync(capture: true);
                break;
            case "🖼 从相册选图":
                await AddPhotoAsync(capture: false);
                break;
        }
    }

    /// <summary>请求麦克风权限并开始录音。</summary>
    private async Task StartRecordingAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlertAsync("需要麦克风权限", "请在系统设置中允许麦克风访问，才能语音输入。", "知道了");
                return;
            }

            await AudioRecorder.StartAsync();
            AddBtn.Text = "🔴";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("录音失败", ex.Message, "关闭");
        }
    }

    /// <summary>选已有音频文件 → 转录 → 填入输入框。</summary>
    private async Task PickAndTranscribeAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "选择音频文件（mp3/wav/m4a/flac/ogg/webm）",
            });
            if (result == null) return;

            var ext = Path.GetExtension(result.FileName).TrimStart('.').ToLowerInvariant();
            if (!TranscribeAudioTool.IsSupportedAudioExtension(ext))
            {
                await DisplayAlertAsync("不支持的格式", $"不支持 .{ext} 音频，请选择 mp3/wav/m4a/flac/ogg/webm", "关闭");
                return;
            }

            var rel = await SandboxFsService.ImportAsync(result);
            var full = SandboxFsService.ResolveInSandbox(rel) ?? "";
            if (string.IsNullOrEmpty(full))
            {
                await DisplayAlertAsync("转录失败", "音频无法写入沙箱工作区", "关闭");
                return;
            }

            await TranscribeAsync(full);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("转录失败", ex.Message, "关闭");
        }
    }

    /// <summary>把录音/音频文件路径转录为文字并填入输入框。</summary>
    private async Task TranscribeAsync(string audioPath)
    {
        try
        {
            AddBtn.IsEnabled = false;
            var text = await new TranscribeAudioTool()
                .ExecuteAsync(new Dictionary<string, object?> { ["path"] = audioPath });
            AddBtn.IsEnabled = true;

            if (TranscribeAudioTool.IsTranscribeError(text))
            {
                await DisplayAlertAsync("转录失败", text, "关闭");
                return;
            }

            InputBox.Text = text;
        }
        catch (Exception ex)
        {
            AddBtn.IsEnabled = true;
            await DisplayAlertAsync("转录失败", ex.Message, "关闭");
        }
    }

    /// <summary>图片：拍照(capture=true)/从相册选(capture=false) → 入 vision 队列，下一轮消息自动带上（脱离电脑的「拍照看图」）。</summary>
    private async Task AddPhotoAsync(bool capture)
    {
        try
        {
            FileResult? photo = capture
                ? await MediaPicker.Default.CapturePhotoAsync()
                : (await MediaPicker.Default.PickPhotosAsync()).FirstOrDefault(); // CS0618：PickPhotoAsync 过时，换多选版取第一张
            if (photo == null) return;

            // 保存到沙箱 workspace
            var rel = await SandboxFsService.ImportAsync(photo);
            var full = SandboxFsService.ResolveInSandbox(rel) ?? "";
            if (string.IsNullOrEmpty(full))
            {
                await DisplayAlertAsync("添加图片失败", "图片无法写入沙箱工作区", "关闭");
                return;
            }

            // vision 门控（与 ViewImageTool 一致：用全局配置；MVP 单槽位即全局模型）
            var model = Config.Instance.Model;
            var modelLabel = ConnectionConfig.FormatModel(ModelCatalog.ProviderDisplayName(Config.Instance.Provider), model);
            var baseUrl = Config.Instance.BaseUrl;
            if (!ModelCatalog.ResolveSupportsVision(model, baseUrl))
            {
                await DisplayAlertAsync("不支持看图",
                    $"当前模型 {modelLabel} 不支持图片输入（vision）。请切换到 gpt-4o / gpt-5 / claude / gemini 等 vision 模型。",
                    "知道了");
                return;
            }

            // 入队（与 Agent 主循环 DrainImages(AgentId="maui-slot-0") 对齐）
            LLM.QueueImage("maui-slot-0", full);
            await DisplayAlertAsync("图片已添加", $"已将图片加入下一轮请求，发送消息后 {modelLabel} 会看到它。", "知道了");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("添加图片失败", ex.Message, "关闭");
        }
    }
}
