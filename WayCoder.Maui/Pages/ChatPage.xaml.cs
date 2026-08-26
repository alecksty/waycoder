using System.Collections.ObjectModel;
using System.Text;
using WayCoder.Maui.Markup;
using WayCoder.Maui.Models;
using WayCoder.Maui.Services;
using WayCoder.Tools;

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
    private CancellationTokenSource? _cts;

    public ChatPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshModelBar();
    }

    /// <summary>顶部模型条显示当前生效模型。</summary>
    private void RefreshModelBar() => ModelBar.Text = $"🧠 模型：{Config.Instance.Model}";

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

        // 未配置 Key 时引导去设置页
        if (string.IsNullOrEmpty(Config.Instance.ApiKey))
        {
            var action = await DisplayActionSheetAsync("尚未配置 API Key", "稍后", null, "去设置");
            if (action == "去设置") await Shell.Current.GoToAsync("//settings");
            return;
        }

        InputBox.Text = "";
        Messages.Add(new ChatMessage { Role = ChatRole.User, RawText = text });

        var aiMsg = new ChatMessage { Role = ChatRole.Assistant, IsStreaming = true };
        Messages.Add(aiMsg);

        var sb = new StringBuilder();
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        _cts = new CancellationTokenSource();
        SendBtn.Text = "停止";

        try
        {
            await _agent.ChatAsync(text,
                token =>
                {
                    sb.Append(token);
                    aiMsg.Formatted = MarkupToFormattedString.Convert(sb.ToString(), isDark);
                },
                (name, summary) => Messages.Add(new ChatMessage { Role = ChatRole.Tool, RawText = $"🔧 {name}" }),
                output => { /* 工具输出暂不逐条展示（MVP） */ },
                _cts.Token);
        }
        catch (OperationCanceledException) { /* 用户停止 */ }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage { Role = ChatRole.Tool, RawText = $"⚠️ {ex.Message}" });
        }
        finally
        {
            aiMsg.IsStreaming = false;
            aiMsg.RawText = sb.ToString();
            SendBtn.Text = "发送";
            _cts = null;
            ScrollToEnd();
        }
    }

    private void ScrollToEnd()
    {
        if (Messages.Count > 0)
            MsgList.ScrollTo(Messages.Count - 1, position: ScrollToPosition.End, animate: false);
    }

    /// <summary>
    /// 语音🎤：点按进入录音态（🔴），再点停止并转录；或选已有音频文件转录。
    /// 录音/转录均落沙箱 workspace，复用主工程 <see cref="TranscribeAudioTool"/>。
    /// </summary>
    private async void OnVoiceClicked(object? sender, EventArgs e)
    {
        // 正在录音 → 停止并转录
        if (AudioRecorder.IsRecording)
        {
            VoiceBtn.Text = "🎤";
            var path = await AudioRecorder.StopAsync();
            if (path != null) await TranscribeAsync(path);
            return;
        }

        var action = await DisplayActionSheetAsync("语音输入", "取消", null, "🎤 开始录音", "📁 选择音频文件");
        if (string.IsNullOrEmpty(action) || action == "取消") return;

        if (action == "🎤 开始录音")
            await StartRecordingAsync();
        else
            await PickAndTranscribeAsync();
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
            VoiceBtn.Text = "🔴";
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
            VoiceBtn.IsEnabled = false;
            var text = await new TranscribeAudioTool()
                .ExecuteAsync(new Dictionary<string, object?> { ["path"] = audioPath });
            VoiceBtn.IsEnabled = true;

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
            VoiceBtn.IsEnabled = true;
            await DisplayAlertAsync("转录失败", ex.Message, "关闭");
        }
    }

    /// <summary>图片📷：拍照/选图 → 入 vision 队列，下一轮消息自动带上（脱离电脑的「拍照看图」）。</summary>
    private async void OnImageClicked(object? sender, EventArgs e)
    {
        try
        {
            var action = await DisplayActionSheetAsync("添加图片", "取消", null, "📷 拍照", "🖼 从相册选择");
            if (string.IsNullOrEmpty(action) || action == "取消") return;

            FileResult? photo = action == "📷 拍照"
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
