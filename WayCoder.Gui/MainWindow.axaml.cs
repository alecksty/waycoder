using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace WayCoder.UI.Gui;

public partial class MainWindow : Window
{
    private Agent? _agent;
    private CancellationTokenSource? _cts;
    private readonly StringBuilder _chat = new();

    public MainWindow()
    {
        InitializeComponent();
        InitAgent();
    }

    /// <summary>懒建 Agent（复用 Web 版 EnsureSlot 的 Config→LLM→Agent 接线）。</summary>
    private void InitAgent()
    {
        try
        {
            var cfg = Config.Instance;
            var info = ModelCatalog.Find(cfg.Model);
            var providerId = info?.ProviderId ?? cfg.Provider;
            var key = ApiKeyStore.Get(providerId) ?? cfg.ApiKey;
            var baseUrl = info?.DefaultBaseUrl ?? cfg.BaseUrl;
            var llm = new LLM(cfg.Model, key, baseUrl, cfg.MaxTokens, cfg.Temperature)
            {
                SmallModel = cfg.SmallModel,
            };
            _agent = new Agent(llm,
                maxContextTokens: ModelCatalog.ResolveContextWindow(cfg.Model, cfg.MaxContextTokens),
                maxBudgetUsd: cfg.MaxBudgetUsd,
                autoCommit: cfg.AutoGitCommit);

            HeaderLabel.Text = $"WayCoder（道码）— {cfg.Model}";
        }
        catch (Exception ex)
        {
            HeaderLabel.Text = "WayCoder（道码）— 初始化失败";
            Append($"[错误] 初始化 Agent 失败：{ex.Message}\n（请检查 .env 中的 WAYCODER_API_KEY）");
        }
    }

    private async void Send_Click(object? sender, RoutedEventArgs e) => await SendAsync();

    private async void Input_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            e.Handled = true;
            await SendAsync();
        }
    }

    private void Stop_Click(object? sender, RoutedEventArgs e) => _cts?.Cancel();

    private async Task SendAsync()
    {
        if (_agent == null || _cts != null) return; // 正在运行中，忽略

        var input = InputBox.Text?.Trim();
        if (string.IsNullOrEmpty(input)) return;
        InputBox.Text = "";

        Append($"\n\n👤 {input}\n");
        Append("🤖 ");
        SendButton.IsEnabled = false;
        StopButton.IsEnabled = true;
        _cts = new CancellationTokenSource();

        try
        {
            await _agent.ChatAsync(input,
                onToken: t => Dispatcher.UIThread.Post(() => Append(t)),
                onTool: (name, brief) => Dispatcher.UIThread.Post(() => Append($"\n🔧 [{name}] {brief}\n")),
                onToolOutput: _ => { }, // MVP：工具输出暂不逐条显示
                cancellationToken: _cts.Token);
        }
        catch (OperationCanceledException)
        {
            Append("\n\n[已停止]\n");
        }
        catch (Exception ex)
        {
            Append($"\n\n[错误] {ex.Message}\n");
        }
        finally
        {
            SendButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            _cts.Dispose();
            _cts = null;
        }
    }

    private void Append(string text)
    {
        _chat.Append(StripMarkup(text));
        ChatBox.Text = _chat.ToString();
        ChatBox.CaretIndex = ChatBox.Text?.Length ?? 0;
    }

    /// <summary>去除中间格式标记「«color»text«/»」，MVP 暂不渲染富文本。</summary>
    private static string StripMarkup(string s)
        => Regex.Replace(s, "«[^»]*»", "");
}
