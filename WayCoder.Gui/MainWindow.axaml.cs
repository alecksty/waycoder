using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using WayCoder.UI.Tui;

namespace WayCoder.UI.Gui;

public partial class MainWindow : Window
{
    private const int SlotCount = 10;

    private readonly Agent?[] _agents = new Agent?[SlotCount];
    private readonly StringBuilder[] _chats = new StringBuilder[SlotCount];
    private readonly CancellationTokenSource?[] _cts = new CancellationTokenSource?[SlotCount];
    private readonly Button[] _slotButtons = new Button[SlotCount];
    private int _activeSlot = 0;

    public MainWindow()
    {
        InitializeComponent();
        // 注入 GUI 交互桥：Agent 的权限确认/提问走 Avalonia 对话框，而非回退 Console I/O
        UxHelper.WebInteraction = new GuiInteraction(this);
        InitModels();
        InitSlots();
        SwitchSlot(0);
    }

    // ── 初始化 ──

    private void InitModels()
    {
        var cfg = Config.Instance;
        var models = ModelCatalog.All;
        foreach (var m in models)
            ModelCombo.Items.Add(m.Id);

        int sel = -1;
        for (int i = 0; i < models.Length; i++)
            if (string.Equals(models[i].Id, cfg.Model, StringComparison.OrdinalIgnoreCase)) { sel = i; break; }
        ModelCombo.SelectedIndex = sel >= 0 ? sel : 0;
        UpdateHeader();
    }

    private void InitSlots()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            _chats[i] = new StringBuilder();
            var btn = new Button { Content = $"F{i + 1}", MinWidth = 38, Padding = new Avalonia.Thickness(8, 4) };
            int slot = i;
            btn.Click += (_, _) => SwitchSlot(slot);
            _slotButtons[i] = btn;
            SlotPanel.Children.Add(btn);
        }
    }

    private void UpdateHeader()
    {
        var model = ModelCombo.SelectedItem as string ?? Config.Instance.Model;
        HeaderLabel.Text = $"WayCoder（道码）— {model}";
    }

    // ── 槽位 ──

    private void SwitchSlot(int slot)
    {
        _activeSlot = slot;
        for (int i = 0; i < SlotCount; i++)
            _slotButtons[i].Background = i == slot ? new SolidColorBrush(Color.Parse("#4f8cff")) : null;
        MarkdownInlines.RenderTo(ChatBox.Inlines!, _chats[slot].ToString());
        SlotLabel.Text = $"槽位 F{slot + 1}";
        StopButton.IsEnabled = _cts[slot] != null;
        SendButton.IsEnabled = _cts[slot] == null;
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
            autoCommit: cfg.AutoGitCommit);

        LoadSlotSession(slot); // 恢复历史会话
        return _agents[slot]!;
    }

    // ── 会话持久化 ──

    private static string SlotSessionId(int slot) => slot == 0 ? "_auto" : $"_auto_slot{slot}";

    protected override void OnClosed(EventArgs e)
    {
        SaveAllSessions();
        base.OnClosed(e);
    }

    private void SaveAllSessions()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            var agent = _agents[i];
            if (agent == null) continue;
            var msgs = agent.SnapshotMessages();
            if (msgs.Count == 0) continue;
            try { SessionManager.SaveSession(msgs, agent.LlmClient.Model, SlotSessionId(i), i); }
            catch { /* 保存失败不影响退出 */ }
        }
    }

    private void LoadSlotSession(int slot)
    {
        var agent = _agents[slot];
        if (agent == null) return;
        try
        {
            var loaded = SessionManager.LoadSession(SlotSessionId(slot), slot);
            if (loaded != null && loaded.Value.Messages.Count > 0)
            {
                agent.ReplaceMessages(loaded.Value.Messages);
                RebuildChatFromAgent(slot, agent);
            }
        }
        catch { /* 无历史会话则跳过 */ }
    }

    private void RebuildChatFromAgent(int slot, Agent agent)
    {
        _chats[slot].Clear();
        foreach (var msg in agent.SnapshotMessages())
        {
            var role = msg["role"]?.AsString() ?? "";
            var content = msg["content"]?.AsString() ?? "";
            if (string.IsNullOrEmpty(content)) continue;
            if (role == "user")
                _chats[slot].Append($"\n\n👤 {content}\n");
            else if (role == "assistant")
                _chats[slot].Append($"🤖 {content}\n");
        }
        if (slot == _activeSlot)
            MarkdownInlines.RenderTo(ChatBox.Inlines!, _chats[slot].ToString());
    }

    // ── 交互 ──

    private async void Send_Click(object? sender, RoutedEventArgs e) => await SendAsync();

    private async void Input_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            e.Handled = true;
            await SendAsync();
        }
    }

    private void Stop_Click(object? sender, RoutedEventArgs e) => _cts[_activeSlot]?.Cancel();

    private void Model_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ModelCombo.SelectedItem is not string modelId) return;
        UpdateHeader();
        try
        {
            var cfg = Config.Instance;
            var info = ModelCatalog.Find(modelId);
            if (info == null) return;
            var key = ApiKeyStore.Get(info.ProviderId) ?? cfg.ApiKey;
            var baseUrl = info.DefaultBaseUrl ?? cfg.BaseUrl;
            var agent = EnsureSlot(_activeSlot);
            agent.LlmClient.Reconfigure(key, baseUrl);
            agent.LlmClient.Model = modelId;
            agent.UpdateContextWindow(ModelCatalog.ResolveContextWindow(modelId, cfg.MaxContextTokens));
        }
        catch (Exception ex)
        {
            AppendSlot(_activeSlot, $"\n[切换模型失败] {ex.Message}\n");
        }
    }

    private async Task SendAsync()
    {
        int slot = _activeSlot;
        if (_cts[slot] != null) return; // 该槽位正在运行

        var input = InputBox.Text?.Trim();
        if (string.IsNullOrEmpty(input)) return;
        InputBox.Text = "";

        Agent agent;
        try { agent = EnsureSlot(slot); }
        catch (Exception ex) { AppendSlot(slot, $"\n[错误] 初始化 Agent 失败：{ex.Message}\n"); return; }

        AppendSlot(slot, $"\n\n👤 {input}\n🤖 ");
        SendButton.IsEnabled = false;
        StopButton.IsEnabled = true;
        _cts[slot] = new CancellationTokenSource();

        try
        {
            await agent.ChatAsync(input,
                onToken: t => Dispatcher.UIThread.Post(() => AppendSlot(slot, t)),
                onTool: (name, brief) => Dispatcher.UIThread.Post(() => AppendSlot(slot, $"\n🔧 [{name}] {brief}\n")),
                onToolOutput: _ => { }, // MVP：工具输出暂不逐条显示
                cancellationToken: _cts[slot]!.Token);
        }
        catch (OperationCanceledException)
        {
            AppendSlot(slot, "\n\n[已停止]\n");
        }
        catch (Exception ex)
        {
            AppendSlot(slot, $"\n\n[错误] {ex.Message}\n");
        }
        finally
        {
            _cts[slot]?.Dispose();
            _cts[slot] = null;
            if (slot == _activeSlot)
            {
                SendButton.IsEnabled = true;
                StopButton.IsEnabled = false;
            }
        }
    }

    private void AppendSlot(int slot, string text)
    {
        _chats[slot].Append(text); // 保留原始 markdown（含 «» 标记），渲染时统一转 Inline
        if (slot == _activeSlot)
        {
            MarkdownInlines.RenderTo(ChatBox.Inlines!, _chats[slot].ToString());
            Dispatcher.UIThread.Post(() => ChatScroll.ScrollToEnd(), DispatcherPriority.Background);
        }
    }
}
