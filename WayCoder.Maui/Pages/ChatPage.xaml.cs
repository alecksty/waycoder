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

/// <summary>按消息角色选择气泡模板（用户右对齐 / AI 左对齐富文本 / 工具灰色小字）。</summary>
public class MessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate UserTemplate { get; set; } = null!;
    public DataTemplate AssistantTemplate { get; set; } = null!;
    public DataTemplate ToolTemplate { get; set; } = null!;

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        => item is ChatMessage m
            ? m.Role switch
            {
                ChatRole.User => UserTemplate,
                ChatRole.Tool => ToolTemplate,
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

    /// <summary>最近一次 onTool 创建的工具消息（onToolOutput 累积详情时追加到这里）。</summary>
    private ChatMessage? _currentToolMsg;

    /// <summary>消息列表是否接近底部（用于智能滚动：接近底部才跟随，用户上翻时不打断）。</summary>
    private bool _isNearBottom = true;

    /// <summary>流式跟随节流时间戳：距上次滚动 &lt;150ms 跳过，避免每 token 触发重排。</summary>
    private DateTime _lastStreamScroll = DateTime.MinValue;

    /// <summary>富文本重算节流：代码回复每 token 全量重分词会卡 UI，按增长量/时间节流。</summary>
    private DateTime _lastFormatRecompute = DateTime.MinValue;
    private int _lastFormattedLen;
    private DateTime _lastReasoningUpdate = DateTime.MinValue;
    private int _lastReasoningLen;

    /// <summary>发送队列：agent 忙时发送的消息排队，忙完自动取下一条（移动端聊天不卡输入）。</summary>
    private readonly Queue<QueuedItem> _sendQueue = new();
    private sealed record QueuedItem(string Text, ChatMessage Msg);

    /// <summary>统一消息入口：Add 后裁剪，防消息列表无限增长（镜像 TUI PruneChatHistory）。</summary>
    private void AddMessage(ChatMessage m)
    {
        Messages.Add(m);
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

    /// <summary>思考内容同样节流（Reasoning 属性 setter 每 token 触发绑定重渲染 → 长思考流卡死主线程）。</summary>
    private bool ShouldRecomputeReasoning(int len)
    {
        var now = DateTime.UtcNow;
        if (len - _lastReasoningLen >= 300 || (now - _lastReasoningUpdate).TotalMilliseconds >= 120)
        {
            _lastReasoningUpdate = now;
            _lastReasoningLen = len;
            return true;
        }
        return false;
    }

    public ChatPage()
    {
        InitializeComponent();
        BindingContext = this;
        BuildPromptBar(); // promptbar：输入框上方常用命令提示（点击填入）
    }

    /// <summary>promptbar：输入框上方常用命令提示（点击填入输入框，光标跟末尾）。</summary>
    private void BuildPromptBar()
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        foreach (var (name, desc) in WayCoder.UI.Shared.CommandBar.Favorites)
        {
            var chip = new Border
            {
                BackgroundColor = isDark ? Color.FromArgb("#1F1F2E") : Color.FromArgb("#E8E8ED"),
                Padding = new Thickness(8, 2),
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
            };
            var lbl = new Label { Text = name, FontSize = 11, TextColor = isDark ? Color.FromArgb("#8b93a7") : Color.FromArgb("#5a6472") };
            chip.Content = lbl;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                InputBox.Text = name + " ";
                InputBox.CursorPosition = InputBox.Text.Length;
                InputBox.Focus();
            };
            chip.GestureRecognizers.Add(tap);
            PromptBar.Children.Add(chip);
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
        _ = PromptResumeSession(); // 进入时：有上次会话则弹「继续会话 / 新的会话」
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
        if (Messages.Count > 0) MauiSessionStore.Save(Messages); // 退出时记住会话
    }

    /// <summary>进入聊天页且有上次会话时，弹「继续会话 / 新的会话」选择。</summary>
    private async Task PromptResumeSession()
    {
        if (Messages.Count > 0 || !MauiSessionStore.Exists()) return;
        var action = await DisplayActionSheetAsync("发现上次会话", "取消", null, "继续会话", "新的会话");
        if (action == "继续会话")
        {
            var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
            foreach (var m in MauiSessionStore.Load())
            {
                m.IsDark = isDark;
                if (m.Role == ChatRole.Assistant && !string.IsNullOrEmpty(m.RawText))
                    m.Formatted = MarkupToFormattedString.Convert(m.RawText, isDark);
                AddMessage(m);
            }
            ScrollToEnd();
        }
        else if (action == "新的会话")
        {
            MauiSessionStore.Clear();
        }
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
        ModelBar.Text = $"🧠 {Config.Instance.Model}";
        RefreshStatusBar();
    }

    /// <summary>
    /// 顶部状态区：行 1 右侧 ModeBar = 工作模式 / 权限（提到模型同一行，避免行 2 拥挤）；
    /// 行 2 StatusBar = todo / 上下文 / 用量 / 花费。
    /// </summary>
    private void RefreshStatusBar()
    {
        var s = AgentService.GetStatus();
        ModeBar.Text = s == null
            ? "⚙ 建造 · 🔐 Ask"
            : $"⚙ {s.WorkMode} · 🔐 {s.PermMode}";
        StatusBar.Text = s == null
            ? "📋 todo ×0"
            : $"📋 todo ×{s.TodoCount} · 上下文 {FormatK(s.ContextUsed)}/{FormatK(s.ContextMax)} · "
              + $"🪙 {FormatK(s.PromptTokens)}+{FormatK(s.CompletionTokens)} · 💰 ${s.Cost?.ToString("F4") ?? "-"}";
    }

    private static string FormatK(int n) => n >= 1000 ? $"{n / 1000.0:F1}k" : n.ToString();

    /// <summary>点模型条 → 打开模型选择页（TUI ModelPicker 移植：分组+搜索+大/小切换）。</summary>
    private async void OnModelBarTapped(object? sender, TappedEventArgs? e)
        => await Shell.Current.GoToAsync("modelpicker");

    /// <summary>右上角菜单：会话/任务/模型/模式/权限等缺失功能集中入口。</summary>
    private async void OnMenuClicked(object? sender, EventArgs e)
    {
        var action = await DisplayActionSheetAsync("菜单", "取消", null,
            "🧠 模型选择", "🗂 供应商/模型", "🔄 代码同步", "⚙ 模式切换", "🔐 权限切换", "📋 会话管理", "📌 任务管理", "ℹ️ 关于");
        switch (action)
        {
            case "🧠 模型选择": OnModelBarTapped(null, null); break;
            case "🗂 供应商/模型": await Shell.Current.GoToAsync("models"); break;
            case "🔄 代码同步": await Shell.Current.GoToAsync("gitsync"); break;
            case "⚙ 模式切换": CycleWorkMode(); break;
            case "🔐 权限切换": CyclePermission(); break;
            case "📋 会话管理": await ManageSessionsAsync(); break;
            case "📌 任务管理": await ShowTasksAsync(); break;
            case "ℹ️ 关于": await Shell.Current.GoToAsync("about"); break;
        }
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
    private async Task ManageSessionsAsync()
    {
        var action = await DisplayActionSheetAsync("会话管理", "取消", null, "继续会话", "新的会话");
        if (action == "继续会话")
        {
            var loaded = MauiSessionStore.Load(); // 先读再清，避免删了文件读到空
            var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
            Messages.Clear();
            foreach (var m in loaded)
            {
                m.IsDark = isDark;
                if (m.Role == ChatRole.Assistant && !string.IsNullOrEmpty(m.RawText))
                    m.Formatted = MarkupToFormattedString.Convert(m.RawText, isDark);
                AddMessage(m);
            }
            MauiSessionStore.Save(Messages); // 重新落盘
            ScrollToEnd();
        }
        else if (action == "新的会话")
        {
            Messages.Clear();
            MauiSessionStore.Clear();
        }
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
        while (_sendQueue.Count > 0)
        {
            var dropped = _sendQueue.Dequeue();
            if (dropped.Msg != null && !string.IsNullOrEmpty(dropped.Msg.RawText))
                dropped.Msg.RawText = dropped.Msg.RawText.Replace("⏳ 排队中…", "❌ 已停止（不再执行）");
        }
    }

    /// <summary>串行处理发送队列：发完一条取下一条，直到队列空。firstUserMsg 为 null 表示首条需新建用户气泡。</summary>
    private async Task ProcessQueueAsync(string first, ChatMessage? firstUserMsg)
    {
        var text = first;
        var userMsg = firstUserMsg;
        while (true)
        {
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
        var aiMsg = new ChatMessage { Role = ChatRole.Assistant, IsStreaming = true };
        AddMessage(aiMsg);

        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        // 思考过程与正文分离：reasoning 用 «dim»…«/» 包裹（LLM 层发独立边界 token），
        // 正文其余 token 归 content。思考流式时实时展开、结束折叠，正文独立渲染富文本。
        var inReasoning = false;
        var reasoningSb = new StringBuilder();
        var contentSb = new StringBuilder();
        _cts = new CancellationTokenSource();
        AgentService.SetActiveCts(_cts); // 注册给 App 生命周期：切后台（来电/Home/锁屏）时取消在途请求
        _lastReasoningLen = 0;
        _lastReasoningUpdate = DateTime.MinValue;
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
                            inReasoning = false;          // 思考结束 → 折叠
                            aiMsg.IsReasoningExpanded = false;
                        }
                        else
                        {
                            AppendCapped(reasoningSb, token);
                            // 思考内容节流：Reasoning setter 每 token 触发绑定重渲染，长思考流会卡死主线程
                            if (ShouldRecomputeReasoning(reasoningSb.Length))
                                aiMsg.Reasoning = reasoningSb.ToString();
                            aiMsg.HasReasoning = true;
                            FollowStreamScroll();   // 流式跟随：思考过程滚动
                        }
                    }
                    else
                    {
                        if (token == "«dim»" || token == "\n«dim»")
                        {
                            inReasoning = true;           // 思考开始 → 实时展开
                            aiMsg.IsReasoningExpanded = true;
                        }
                        else
                        {
                            AppendCapped(contentSb, token);
                            if (ShouldRecomputeFormatted(contentSb.Length))
                                aiMsg.Formatted = MarkupToFormattedString.Convert(contentSb.ToString(), isDark);
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
                    _currentToolMsg = new ChatMessage
                    {
                        Role = ChatRole.Tool,
                        RawText = $"🔧 {name}",
                        ToolSummary = summary,
                        ToolFilePath = ExtractFilePath(summary),
                        IsDark = isDark,
                    };
                    AddMessage(_currentToolMsg);
                },
                output =>
                {
                    if (_currentToolMsg == null) return;
                    // 工具详情防无限增长：超上限停止追加并加标记（对齐 Global.MaxSingleMessageChars）。
                    if (_currentToolMsg.ToolDetail.Length < Global.MaxSingleMessageChars)
                        _currentToolMsg.ToolDetail += output;
                    else if (!_currentToolMsg.ToolDetail.EndsWith("… 已截断…", StringComparison.Ordinal))
                        _currentToolMsg.ToolDetail += "\n… 已截断（工具输出过长，停止追加）…";
                    _currentToolMsg.HasToolDetail = true;
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
            aiMsg.IsStreaming = false;
            aiMsg.RawText = contentSb.ToString();
            aiMsg.Reasoning = reasoningSb.ToString();   // 节流后补齐最终思考全文
            aiMsg.Formatted = MarkupToFormattedString.Convert(contentSb.ToString(), isDark); // 节流后补齐最终富文本
            SendBtn.Text = "↑"; // 空闲恢复 = 发送
            AgentService.SetActiveCts(null);
            _cts = null;
            _uiState = cancelled ? AgentStatus.Idle : AgentStatus.Complete; // 任务完成瞬态（取消/异常直接回空闲）
            if (!cancelled) _completeAt = DateTime.UtcNow;
            RefreshStatusBar();
            if (Messages.Count > 0) MauiSessionStore.Save(Messages); // 每轮结束落盘，退出/重启可恢复

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

    /// <summary>收起聊天里所有折叠项（保持同屏只开一个折叠项）。</summary>
    private void CollapseAllFolds()
    {
        foreach (var msg in Messages)
        {
            msg.IsReasoningExpanded = false;
            msg.IsToolDetailExpanded = false;
        }
    }

    /// <summary>折叠条点击：切换思考过程展开/收起（sender 是挂手势的 Border，BindingContext 即消息）。
    /// 展开前先收起其它折叠项——同屏只开一个。</summary>
    private void OnToggleReasoning(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject view && view.BindingContext is ChatMessage m && m.HasReasoning)
        {
            var willExpand = !m.IsReasoningExpanded;
            if (willExpand) CollapseAllFolds();
            m.IsReasoningExpanded = willExpand;
        }
    }

    /// <summary>折叠条点击：切换工具输出详情展开/收起。展开前先收起其它折叠项——同屏只开一个。</summary>
    private void OnToggleToolDetail(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject view && view.BindingContext is ChatMessage m && m.HasToolDetail)
        {
            var willExpand = !m.IsToolDetailExpanded;
            if (willExpand) CollapseAllFolds();
            m.IsToolDetailExpanded = willExpand;
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

            if (text.StartsWith("错误", StringComparison.Ordinal)
                || text.StartsWith("转录失败", StringComparison.Ordinal)
                || text.StartsWith("转录出错", StringComparison.Ordinal)
                || text.StartsWith("转录返回空文本", StringComparison.Ordinal))
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
            var baseUrl = Config.Instance.BaseUrl;
            if (!ModelCatalog.ResolveSupportsVision(model, baseUrl))
            {
                await DisplayAlertAsync("不支持看图",
                    $"当前模型 {model} 不支持图片输入（vision）。请切换到 gpt-4o / gpt-5 / claude / gemini 等 vision 模型。",
                    "知道了");
                return;
            }

            // 入队（与 Agent 主循环 DrainImages(AgentId="maui-slot-0") 对齐）
            LLM.QueueImage("maui-slot-0", full);
            await DisplayAlertAsync("图片已添加", $"已将图片加入下一轮请求，发送消息后 {model} 会看到它。", "知道了");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("添加图片失败", ex.Message, "关闭");
        }
    }
}
