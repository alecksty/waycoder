using System.Collections.ObjectModel;
using System.Text;
using WayCoder.Infra;
using WayCoder.Maui.Markup;
using WayCoder.Maui.Models;
using WayCoder.Maui.Services;
using WayCoder.Tools;
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

    // ── 输入框上方动态状态栏：多状态（空闲/思考/执行工具/等待确认）+ Braille 旋转动画 ──
    private IDispatcherTimer? _statusTimer;
    private int _spinnerFrame;
    private static readonly string[] SpinnerFrames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];

    private enum AgentUiState { Idle, Thinking, Tool, WaitingPermission, Compressing }
    private AgentUiState _uiState = AgentUiState.Idle;
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
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // 注入斜杠命令输出桥：命令执行时把 system/消息 泵回本页消息列表（统一灰色小字）。
        ChatScreen.OnAddSystemMsg = content =>
            MainThread.BeginInvokeOnMainThread(() => Messages.Add(new ChatMessage { Role = ChatRole.Tool, RawText = content }));
        ChatScreen.OnAddMessage = (content, role, centered, indent) =>
            MainThread.BeginInvokeOnMainThread(() => Messages.Add(new ChatMessage { Role = ChatRole.Tool, RawText = content }));
        ChatScreen.OnClearChat = () =>
            MainThread.BeginInvokeOnMainThread(Messages.Clear);
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
                Messages.Add(m);
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
        if (_agent.IsRunning) _uiState = AgentUiState.WaitingPermission;
    }

    private void OnPermissionResolved(string _)
    {
        if (_uiState == AgentUiState.WaitingPermission)
            _uiState = _agent.IsRunning ? AgentUiState.Thinking : AgentUiState.Idle;
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

    /// <summary>每帧刷新动态状态栏：空闲隐藏；其余状态显示旋转图标 + 状态文本。</summary>
    private void TickStatusBar()
    {
        if (_uiState == AgentUiState.Idle)
        {
            AgentStatusBar.IsVisible = false;
            return;
        }
        AgentStatusBar.IsVisible = true;
        _spinnerFrame = (_spinnerFrame + 1) % SpinnerFrames.Length;
        AgentStatusIcon.Text = SpinnerFrames[_spinnerFrame];
        AgentStatusText.Text = _uiState switch
        {
            AgentUiState.WaitingPermission => "等待确认中...",
            AgentUiState.Tool => $"🔧 执行工具 {_toolName}...",
            AgentUiState.Compressing => _compressStatusText,
            _ => "思考中...",
        };
    }

    /// <summary>上下文压缩进度 → 状态栏（压缩是背景状态，不进入聊天区）。</summary>
    private void OnCompressProgress(int layer, string label, double pct)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _compressStatusText = $"🔄 压缩中 [L{layer}/3] {label} {pct:P0}";
            _uiState = AgentUiState.Compressing;
        });
    }

    private void OnCompressFinished()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _compressStatusText = "";
            if (_uiState == AgentUiState.Compressing)
                _uiState = _agent.IsRunning ? AgentUiState.Thinking : AgentUiState.Idle;
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
    private async void OnModelBarTapped(object? sender, TappedEventArgs e)
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
                Messages.Add(m);
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

    private async void OnSendClicked(object? sender, EventArgs e)
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
                Messages.Add(new ChatMessage { Role = ChatRole.User, RawText = text });
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
                    Messages.Add(new ChatMessage { Role = ChatRole.Tool, RawText = $"⚠️ {ex.Message}" });
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
            var msg = new ChatMessage { Role = ChatRole.User, RawText = text + "\n⏳ 排队中…" };
            _sendQueue.Enqueue(new QueuedItem(text, msg));
            Messages.Add(msg);
            ScrollToEnd();
            return;
        }

        await ProcessQueueAsync(text, firstUserMsg: null);
    }

    /// <summary>停止当前一轮（独立停止按钮，发送按钮改为始终发送/排队）。</summary>
    private void OnStopClicked(object? sender, EventArgs e)
    {
        _cts?.Cancel();
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
                Messages.Add(userMsg);
                ScrollToEnd(); // 发送后立即滚到底，保证刚发的消息可见
            }
            else
            {
                userMsg.RawText = text + "\n📤 发送中…";   // 排队消息 → 轮到它了
            }

            await RunOneMessageAsync(text);

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
        Messages.Add(aiMsg);

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
        SendBtn.Text = "↑";
        StopBtn.IsVisible = true;
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
                    _uiState = AgentUiState.Thinking;
                    if (inReasoning)
                    {
                        if (token == "«/»" || token == "«/»\n")
                        {
                            inReasoning = false;          // 思考结束 → 折叠
                            aiMsg.IsReasoningExpanded = false;
                        }
                        else
                        {
                            reasoningSb.Append(token);
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
                            contentSb.Append(token);
                            if (ShouldRecomputeFormatted(contentSb.Length))
                                aiMsg.Formatted = MarkupToFormattedString.Convert(contentSb.ToString(), isDark);
                            FollowStreamScroll();   // 流式跟随：正文滚动
                        }
                    }
                },
                (name, summary) =>
                {
                    _uiState = AgentUiState.Tool;
                    _toolName = name;
                    _currentToolMsg = new ChatMessage
                    {
                        Role = ChatRole.Tool,
                        RawText = $"🔧 {name}",
                        ToolSummary = summary,
                        ToolFilePath = ExtractFilePath(summary),
                        IsDark = isDark,
                    };
                    Messages.Add(_currentToolMsg);
                },
                output =>
                {
                    if (_currentToolMsg == null) return;
                    _currentToolMsg.ToolDetail += output;
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
            Messages.Add(new ChatMessage { Role = ChatRole.Tool, RawText = $"⚠️ {ex.Message}" });
        }
        finally
        {
            aiMsg.IsStreaming = false;
            aiMsg.RawText = contentSb.ToString();
            aiMsg.Reasoning = reasoningSb.ToString();   // 节流后补齐最终思考全文
            aiMsg.Formatted = MarkupToFormattedString.Convert(contentSb.ToString(), isDark); // 节流后补齐最终富文本
            SendBtn.Text = "↑";
            StopBtn.IsVisible = false;
            AgentService.SetActiveCts(null);
            _cts = null;
            _uiState = AgentUiState.Idle;
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
                    Messages.Add(new ChatMessage { Role = ChatRole.Tool, RawText = summary });
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
                : await MediaPicker.Default.PickPhotoAsync();
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
