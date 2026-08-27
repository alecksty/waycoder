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
    }

    /// <summary>顶部模型条显示当前生效模型 + 权限模式（确认轴）。</summary>
    private void RefreshModelBar()
    {
        var perm = PermissionManager.CurrentMode switch
        {
            PermissionManager.Mode.Yolo => "Yolo",
            PermissionManager.Mode.SmartAuto => "SmartAuto",
            PermissionManager.Mode.Auto => "Auto",
            _ => "Ask",
        };
        ModelBar.Text = $"🧠 {Config.Instance.Model} · 🔐 {perm}";
    }

    /// <summary>点模型条 → ActionSheet 列出当前服务商模型，选中即切换（连接层统一入口）。</summary>
    private async void OnModelBarTapped(object? sender, TappedEventArgs e)
    {
        var pid = Config.Instance.Provider.ToLowerInvariant();
        var models = ModelCatalog.ByProvider(pid).ToList();
        if (models.Count == 0)
        {
            await DisplayAlertAsync("无可用模型", $"服务商 {pid} 没有可用模型，请先到「设置」选择。", "确定");
            return;
        }

        var options = models.Select(m => $"{m.DisplayName}（{m.Id}）").ToArray();
        var chosen = await DisplayActionSheetAsync("选择模型", "取消", null, options);
        if (string.IsNullOrEmpty(chosen) || chosen == "取消") return;

        var model = models.First(m => $"{m.DisplayName}（{m.Id}）" == chosen);
        ConnectionConfig.ApplyModelChoice(pid, model.Id, isLarge: true, out _);
        AgentService.Reset();
        RefreshModelBar();
    }

    private async void OnSendClicked(object? sender, EventArgs e)
    {
        // 运行中再点 = 停止
        if (_agent.IsRunning)
        {
            _cts?.Cancel();
            return;
        }

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
        Messages.Add(new ChatMessage { Role = ChatRole.User, RawText = text });

        var aiMsg = new ChatMessage { Role = ChatRole.Assistant, IsStreaming = true };
        Messages.Add(aiMsg);

        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        // 思考过程与正文分离：reasoning 用 «dim»…«/» 包裹（LLM 层发独立边界 token），
        // 正文其余 token 归 content。思考流式时实时展开、结束折叠，正文独立渲染富文本。
        var inReasoning = false;
        var reasoningSb = new StringBuilder();
        var contentSb = new StringBuilder();
        _cts = new CancellationTokenSource();
        SendBtn.Text = "■";

        try
        {
            await _agent.ChatAsync(text,
                token =>
                {
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
                            aiMsg.Reasoning = reasoningSb.ToString();
                            aiMsg.HasReasoning = true;
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
                            aiMsg.Formatted = MarkupToFormattedString.Convert(contentSb.ToString(), isDark);
                        }
                    }
                },
                (name, summary) =>
                {
                    _currentToolMsg = new ChatMessage { Role = ChatRole.Tool, RawText = $"🔧 {name}", ToolSummary = summary };
                    Messages.Add(_currentToolMsg);
                },
                output =>
                {
                    if (_currentToolMsg == null) return;
                    _currentToolMsg.ToolDetail += output;
                    _currentToolMsg.HasToolDetail = true;
                },
                _cts.Token);
        }
        catch (OperationCanceledException) { /* 用户停止 */ }
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
            SendBtn.Text = "↑";
            _cts = null;
            ScrollToEnd();
        }
    }

    /// <summary>智能滚动：仅在列表接近底部时才跟随到底，用户上翻历史时不打断浏览。</summary>
    private void ScrollToEnd()
    {
        if (Messages.Count > 0 && _isNearBottom)
            MsgList.ScrollTo(Messages.Count - 1, position: ScrollToPosition.End, animate: false);
    }

    /// <summary>折叠条点击：切换思考过程展开/收起（sender 是挂手势的 Border，BindingContext 即消息）。</summary>
    private void OnToggleReasoning(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject view && view.BindingContext is ChatMessage m && m.HasReasoning)
            m.IsReasoningExpanded = !m.IsReasoningExpanded;
    }

    /// <summary>折叠条点击：切换工具输出详情展开/收起。</summary>
    private void OnToggleToolDetail(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject view && view.BindingContext is ChatMessage m && m.HasToolDetail)
            m.IsToolDetailExpanded = !m.IsToolDetailExpanded;
    }

    /// <summary>跟踪列表是否接近底部（智能滚动判定依据）。</summary>
    private void OnMsgListScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        _isNearBottom = e.LastVisibleItemIndex >= Messages.Count - 2;
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
