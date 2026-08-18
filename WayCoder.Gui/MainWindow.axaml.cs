using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Threading;
using WayCoder.UI.Tui;

namespace WayCoder.UI.Gui;

public partial class MainWindow : Window
{
    private const int SlotCount = 10;

    private readonly Agent?[] _agents = new Agent?[SlotCount];
    private readonly List<ChatMessage>[] _messages = new List<ChatMessage>[SlotCount];
    private readonly CancellationTokenSource?[] _cts = new CancellationTokenSource?[SlotCount];
    private readonly Button[] _slotButtons = new Button[SlotCount];
    private readonly string[] _drafts = new string[SlotCount];
    private DispatcherTimer? _rightTimer;
    /// <summary>流式渲染合帧守卫：同一 UI 帧内多个 token 只触发一次气泡重渲染。</summary>
    private bool _renderPending;
    /// <summary>右侧面板刷新重入守卫（2s 定时 + 手动刷新防叠层）。</summary>
    private bool _refreshing;
    private int _activeSlot = 0;

    public MainWindow()
    {
        InitializeComponent();
        // 注入 GUI 交互桥：Agent 的权限确认/提问走 Avalonia 对话框，而非回退 Console I/O
        UxHelper.WebInteraction = new GuiInteraction(this);
        InitModels();
        InitModelBar();
        InitSlots();
        SwitchSlot(0);
        RefreshSessions();
        // 右侧面板 2s 定时刷新（对齐 Web setInterval(fetchPanel, 2000)）
        _rightTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _rightTimer.Tick += (_, _) => RefreshPanel();
        _rightTimer.Start();
    }

    // ── 初始化 ──

    private void InitModels()
    {
        ModelCatalog.Invalidate(); // 确保读到最新的模型目录（含导入）
        UpdateHeader();
    }

    private void InitSlots()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            _messages[i] = new List<ChatMessage>();
            var btn = new Button { Content = $"F{i + 1}", MinWidth = 38, Padding = new Avalonia.Thickness(8, 4) };
            int slot = i;
            btn.Click += (_, _) => SwitchSlot(slot);
            _slotButtons[i] = btn;
            SlotPanel.Children.Add(btn);
        }
    }

    private void UpdateHeader()
    {
        var cfg = Config.Instance;
        ModelButton.Content = $"🧠 {cfg.Model}";
        BigModelBtn.Content = $"🤖 {cfg.Model}";
        SmallModelBtn.Content = $"🔧 {cfg.SmallModel}";
    }

    /// <summary>初始化 composer 工具栏：省钱模式 + 交互权限模式下拉。</summary>
    private void InitModelBar()
    {
        foreach (var v in new[] { "关", "自动", "开" }) EconomyCombo.Items.Add(v);
        EconomyCombo.SelectedIndex = (int)Config.Instance.EconomyMode;

        foreach (var v in new[] { "Ask", "Auto", "SmartAuto", "YOLO" }) PermCombo.Items.Add(v);
        PermCombo.SelectedIndex = (int)PermissionManager.CurrentMode;
    }

    // ── 槽位 ──

    private void SwitchSlot(int slot)
    {
        // 当前槽位若有流式中的气泡，先定稿（不再指向它），避免切走后继续被写入
        if (_activeSlot >= 0 && _activeSlot < _messages.Length && _activeSlot != slot)
            FinalizeStreaming(_activeSlot);
        if (_activeSlot != slot) _drafts[_activeSlot] = InputBox.Text ?? ""; // 保存旧槽位输入草稿

        _activeSlot = slot;
        InputBox.Text = _drafts[slot] ?? ""; // 恢复目标槽位草稿
        for (int i = 0; i < SlotCount; i++)
            _slotButtons[i].Background = i == slot
                ? new SolidColorBrush(Color.Parse("#4f8cff"))
                : new SolidColorBrush(Color.Parse("#1d2230"));
        RebuildMessages(slot);
        SlotLabel.Text = $"槽位 F{slot + 1}";
        StopButton.IsEnabled = _cts[slot] != null;
        SendButton.IsEnabled = _cts[slot] == null;
        RefreshPanel(); // Token 卡片切换活跃槽位
        RefreshSessions(); // 会话列表按槽位隔离
    }

    /// <summary>把流式中的消息定稿（Streaming=false），供切槽位时调用。</summary>
    private void FinalizeStreaming(int slot)
    {
        var list = _messages[slot];
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].Streaming) { list[i].Streaming = false; break; }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  右侧数据面板（2s 定时刷新，对齐 Web /panel）
    // ═══════════════════════════════════════════════════════════

    /// <summary>重建右侧 5 张数据卡片（任务/Token费用/修改文件/MCP/LSP）。</summary>
    private void RefreshPanel()
    {
        if (_refreshing) return;
        _refreshing = true;
        try
        {
            RightCards.Children.Clear();
            RightCards.Children.Add(Panels.TodosCard());
            RightCards.Children.Add(Panels.TokensCard(_agents[_activeSlot]));
            RightCards.Children.Add(Panels.FilesCard());
            RightCards.Children.Add(Panels.McpCard());
            RightCards.Children.Add(Panels.LspCard());
        }
        finally { _refreshing = false; }
    }

    /// <summary>重建指定槽位的气泡视图（切换槽位/主题变更/会话加载时用）。</summary>
    private void RebuildMessages(int slot)
    {
        MessagesHost.Children.Clear();
        foreach (var msg in _messages[slot])
        {
            if (msg.View == null) msg.View = new MessageBubble(msg);
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
            autoCommit: cfg.AutoGitCommit);

        LoadSlotSession(slot); // 恢复历史会话
        return _agents[slot]!;
    }

    // ── 会话持久化 ──

    private static string SlotSessionId(int slot) => slot == 0 ? "_auto" : $"_auto_slot{slot}";

    protected override void OnClosed(EventArgs e)
    {
        _rightTimer?.Stop();
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

    // ═══════════════════════════════════════════════════════════
    //  历史会话列表（左栏，按槽位隔离，对齐 Web /sessions）
    // ═══════════════════════════════════════════════════════════

    private void RefreshSessions()
    {
        var panel = new StackPanel { Spacing = 2 };
        try
        {
            var sessions = SessionManager.ListSessions(50, 0, _activeSlot);
            if (sessions.Count == 0)
            {
                var empty = new TextBlock { Text = "暂无历史会话", FontSize = 12, Margin = new Thickness(8, 4) };
                empty[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("DimTextBrush");
                panel.Children.Add(empty);
            }
            else
            {
                foreach (var s in sessions)
                    panel.Children.Add(BuildSessionItem(s));
            }
        }
        catch { }
        SessionListHost.Content = panel;
    }

    private Control BuildSessionItem(SessionInfo s)
    {
        var box = new StackPanel { Spacing = 2, Margin = new Thickness(6, 4) };

        var preview = new TextBlock
        {
            Text = string.IsNullOrEmpty(s.Preview) ? s.Id : s.Preview,
            FontSize = 13,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        preview[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextBrush");

        var meta = new TextBlock
        {
            Text = $"{s.Model} · {s.SavedAt} · {s.MessageCount} 条",
            FontSize = 11,
        };
        meta[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("DimTextBrush");

        // hover 操作（✎ 重命名 / ✕ 删除）
        var ops = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Right,
            IsVisible = false,
        };
        ops.Children.Add(MakeSmallBtn("✎", () => RenameSessionDialog(s.Id)));
        ops.Children.Add(MakeSmallBtn("✕", () => DeleteSessionConfirm(s.Id)));

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        var left = new StackPanel { Spacing = 1 };
        left.Children.Add(preview);
        left.Children.Add(meta);
        grid.Children.Add(left);
        Grid.SetColumn(ops, 1);
        grid.Children.Add(ops);

        var item = new Border
        {
            Child = grid,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 6),
        };
        item.PointerPressed += (_, _) => LoadSessionById(s.Id);
        item.PointerEntered += (_, _) => ops.IsVisible = true;
        item.PointerExited += (_, _) => ops.IsVisible = false;
        return item;
    }

    private static Button MakeSmallBtn(string text, Action onClick)
    {
        var btn = new Button { Content = text, Width = 22, Height = 22, FontSize = 11, Padding = new Thickness(0) };
        btn.Click += (_, _) => onClick();
        return btn;
    }

    private void LoadSessionById(string id)
    {
        var agent = EnsureSlot(_activeSlot);
        try
        {
            var loaded = SessionManager.LoadSession(id, _activeSlot);
            if (loaded == null) return;
            agent.ReplaceMessages(loaded.Value.Messages);
            if (!string.IsNullOrEmpty(loaded.Value.Model))
                agent.LlmClient.Model = loaded.Value.Model;
            RebuildChatFromAgent(_activeSlot, agent);
            UpdateHeader();
            AppendSystem(_activeSlot, $"[已加载会话 {id}]");
        }
        catch (Exception ex)
        {
            AppendSystem(_activeSlot, $"[加载会话失败] {ex.Message}");
        }
    }

    private void NewSession_Click(object? sender, RoutedEventArgs e)
    {
        var agent = EnsureSlot(_activeSlot);
        agent.ReplaceMessages([]);
        _messages[_activeSlot].Clear();
        RebuildMessages(_activeSlot);
        UpdateHeader();
        RefreshSessions();
    }

    private void ClearSessions_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var n = SessionManager.DeleteAllSessions(_activeSlot);
            AppendSystem(_activeSlot, $"[已清空 {n} 个会话记录]");
            RefreshSessions();
        }
        catch (Exception ex)
        {
            AppendSystem(_activeSlot, $"[清空失败] {ex.Message}");
        }
    }

    private void RenameSessionDialog(string oldId)
    {
        var win = new Window
        {
            Title = "重命名会话",
            Width = 360,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = new SolidColorBrush(Color.Parse("#171a23")),
        };
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 12 };
        panel.Children.Add(new TextBlock { Text = $"重命名 {oldId}", Foreground = new SolidColorBrush(Color.Parse("#e6e8ee")) });
        var box = new TextBox { Text = oldId };
        panel.Children.Add(box);
        var btns = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right };
        btns.Children.Add(MakeButton("确定", "#2f6bff", () =>
        {
            try
            {
                var newId = box.Text?.Trim();
                if (!string.IsNullOrEmpty(newId) && newId != oldId)
                    SessionManager.RenameSession(oldId, newId, _activeSlot);
            }
            catch (Exception ex)
            {
                AppendSystem(_activeSlot, $"[重命名失败] {ex.Message}");
            }
            win.Close();
            RefreshSessions();
        }));
        btns.Children.Add(MakeButton("取消", "#5b6472", win.Close));
        panel.Children.Add(btns);
        win.Content = panel;
        win.ShowDialog(this);
    }

    private void DeleteSessionConfirm(string id)
    {
        var win = new Window
        {
            Title = "删除会话",
            Width = 340,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = new SolidColorBrush(Color.Parse("#171a23")),
        };
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 12 };
        panel.Children.Add(new TextBlock { Text = $"确定删除会话 {id}？", Foreground = new SolidColorBrush(Color.Parse("#e6e8ee")) });
        var btns = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right };
        btns.Children.Add(MakeButton("删除", "#d73a49", () =>
        {
            SessionManager.DeleteSession(id, _activeSlot);
            win.Close();
            RefreshSessions();
        }));
        btns.Children.Add(MakeButton("取消", "#5b6472", win.Close));
        panel.Children.Add(btns);
        win.Content = panel;
        win.ShowDialog(this);
    }

    private void RebuildChatFromAgent(int slot, Agent agent)
    {
        var list = _messages[slot];
        list.Clear();
        foreach (var msg in agent.SnapshotMessages())
        {
            var role = msg["role"]?.AsString() ?? "";
            var content = msg["content"]?.AsString() ?? "";
            if (string.IsNullOrEmpty(content)) continue;
            var m = role == "user"
                ? new ChatMessage(ChatRole.User)
                : new ChatMessage(ChatRole.Assistant);
            m.Text.Append(content);
            list.Add(m);
        }
        if (slot == _activeSlot) RebuildMessages(slot);
    }

    // ── 交互 ──

    private async void Send_Click(object? sender, RoutedEventArgs e)
    {
        // busy 时发送按钮 = 停止（对齐 Web：忙碌变 ⏹）
        if (_cts[_activeSlot] != null) { _cts[_activeSlot]?.Cancel(); return; }
        await SendAsync();
    }

    private async void Input_KeyDown(object? sender, KeyEventArgs e)
    {
        // Ctrl+Enter 发送（多行输入用 Shift+Enter 换行，Enter 直接发送）
        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            e.Handled = true;
            await SendAsync();
        }
    }

    /// <summary>输入框自动增高（按行数钳制 56~220，对齐 Web autoResizeInput）。</summary>
    private void Input_TextChanged(object? sender, TextChangedEventArgs e)
    {
        var text = InputBox.Text ?? "";
        int lines = 1;
        for (int i = 0; i < text.Length; i++)
            if (text[i] == '\n') lines++;
        InputBox.Height = Math.Clamp(lines * 24, 56, 220);
    }

    private void Stop_Click(object? sender, RoutedEventArgs e) => _cts[_activeSlot]?.Cancel();

    /// <summary>切换当前槽位模型（Phase 4 模型弹窗复用此逻辑）。</summary>
    internal void ApplyModel(string modelId)
    {
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
            AppendSystem(_activeSlot, $"[切换模型失败] {ex.Message}");
        }
    }

    private void Theme_Click(object? sender, RoutedEventArgs e)
    {
        App.ToggleTheme();
        ThemeButton.Content = App.IsDark ? "🌙" : "☀️";
        // 气泡背景走动态资源自动换色；内部 block 文字色是构建时固化的，需显式重渲染
        foreach (var msg in _messages[_activeSlot])
            msg.View?.Render();
        RefreshPanel();
    }

    private void ModelButton_Click(object? sender, RoutedEventArgs e)
    {
        var win = new ModelWindow(this);
        win.ShowDialog(this);
    }

    private void BigModel_Click(object? sender, RoutedEventArgs e)
        => new ModelWindow(this).ShowDialog(this);

    private void SmallModel_Click(object? sender, RoutedEventArgs e)
        => new ModelWindow(this, smallMode: true).ShowDialog(this);

    private void Economy_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (EconomyCombo.SelectedIndex < 0) return;
        Config.Instance.EconomyMode = (EconomyMode)EconomyCombo.SelectedIndex;
        try { Config.Instance.SaveToEnvFile(); } catch { }
    }

    private void Perm_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (PermCombo.SelectedIndex < 0 || PermCombo.SelectedItem is not string mode) return;
        try { PermissionManager.SetMode(mode); } catch { }
    }

    /// <summary>保存默认模型到配置（不中断当前任务，新会话/重启生效），供模型弹窗调用。</summary>
    internal void SaveDefaultModel(string modelId, bool small)
    {
        var cfg = Config.Instance;
        if (small) cfg.SmallModel = modelId;
        else cfg.Model = modelId;
        try { cfg.SaveToEnvFile(); } catch { }
        UpdateHeader();
        AppendSystem(_activeSlot, $"[已保存默认{(small ? "小" : "大")}模型 {modelId}]");
    }

    /// <summary>切换当前槽位小模型，供模型弹窗调用。</summary>
    internal void ApplySmallModel(string modelId)
    {
        var cfg = Config.Instance;
        cfg.SmallModel = modelId;
        var agent = _agents[_activeSlot];
        if (agent != null) agent.LlmClient.SmallModel = modelId;
        UpdateHeader();
    }

    private void Settings_Click(object? sender, RoutedEventArgs e) => ShowSettings();

    private void ShowSettings()
    {
        var win = new Window
        {
            Title = "设置",
            Width = 660,
            Height = 540,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.Parse("#171a23")),
        };

        // Schema 驱动：全部设置项（对齐 TUI SettingsPage / Web drawer）
        var schema = Config.SettingSchema().OrderBy(s => s.Order).ToList();
        var groups = schema.GroupBy(s => s.Category).ToList();
        var controls = new Dictionary<string, Control>(); // Key → 控件（保存时逐项读取）

        var root = new DockPanel { Margin = new Avalonia.Thickness(16) };

        // ── 底部按钮 ──
        void Save()
        {
            try
            {
                foreach (var (key, ctrl) in controls)
                {
                    string? val = ctrl switch
                    {
                        TextBox tb => tb.Text,
                        ComboBox cb => cb.SelectedItem?.ToString(),
                        CheckBox chk => chk.IsChecked == true ? "true" : "false",
                        _ => null,
                    };
                    if (val != null) Config.TrySetPropValue(key, val, out _);
                }
                Config.Instance.SaveToEnvFile();
                ModelCatalog.Invalidate();
                UpdateHeader();
                InitModelBar(); // 省钱/权限下拉跟随配置变化
                RefreshPanel();
                AppendSystem(_activeSlot, "[设置已保存]");
            }
            catch (Exception ex) { AppendSystem(_activeSlot, $"[保存设置失败] {ex.Message}"); }
            win.Close();
        }
        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Avalonia.Thickness(0, 12, 0, 0) };
        btnRow.Children.Add(MakeButton("保存", "#2f6bff", Save));
        btnRow.Children.Add(MakeButton("取消", "#5b6472", () => win.Close()));
        DockPanel.SetDock(btnRow, Dock.Bottom);
        root.Children.Add(btnRow);

        // ── 左：分类列表 + 右：设置项 ──
        var catList = new ListBox
        {
            MinWidth = 170,
            ItemsSource = groups.Select(g => g.Key).ToList(),
            SelectedIndex = 0,
        };
        catList.Foreground = new SolidColorBrush(Color.Parse("#e6e8ee"));

        var detailHost = new StackPanel { Spacing = 12, Margin = new Avalonia.Thickness(16, 0, 0, 0) };
        var detailScroll = new ScrollViewer { Content = detailHost };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        grid.Children.Add(catList);
        Grid.SetColumn(detailScroll, 1);
        grid.Children.Add(detailScroll);
        root.Children.Add(grid);

        // 切换分类 → 重建右侧设置项
        void RebuildDetail()
        {
            detailHost.Children.Clear();
            if (catList.SelectedItem is not string cat) return;
            var items = groups.First(g => g.Key == cat).OrderBy(s => s.Order);
            foreach (var s in items)
            {
                // 标题 + 描述
                var title = new TextBlock { Text = s.Label, FontSize = 13, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#e6e8ee")) };
                var desc = new TextBlock { Text = s.Desc, FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#8b93a7")), TextWrapping = TextWrapping.Wrap };
                var ctrl = BuildSettingControl(s);
                controls[s.Key] = ctrl;
                detailHost.Children.Add(title);
                detailHost.Children.Add(desc);
                detailHost.Children.Add(ctrl);
            }
        }
        catList.SelectionChanged += (_, _) => RebuildDetail();
        RebuildDetail();

        win.Content = root;
        win.ShowDialog(this);
    }

    /// <summary>按 Schema 类型构建设置控件（toggle/select/secret/number/text）。</summary>
    private static Control BuildSettingControl(SettingDef s)
    {
        var current = Config.GetPropValue(s.Key);
        switch (s.Type)
        {
            case "toggle":
                return new CheckBox { Content = s.Label, IsChecked = current == "true" || current == "True", Foreground = new SolidColorBrush(Color.Parse("#e6e8ee")) };
            case "select":
            {
                var cb = new ComboBox { ItemsSource = s.Options ?? [], Width = 240, HorizontalAlignment = HorizontalAlignment.Left };
                if (s.Options != null)
                {
                    var idx = Array.FindIndex(s.Options, o => o == current || (current != null && o.Equals(current, StringComparison.OrdinalIgnoreCase)));
                    cb.SelectedIndex = idx >= 0 ? idx : 0;
                }
                return cb;
            }
            case "secret":
                return new TextBox { Text = current ?? "", PasswordChar = '•', Width = 240, HorizontalAlignment = HorizontalAlignment.Left };
            case "number":
            default:
                return new TextBox { Text = current ?? "", Width = 240, HorizontalAlignment = HorizontalAlignment.Left };
        }
    }

    private static TextBlock SettingsLabel(string text) => new()
    {
        Text = text,
        Foreground = new SolidColorBrush(Color.Parse("#8b93a7")),
        FontSize = 12,
    };

    private static Button MakeButton(string text, string colorHex, Action onClick)
    {
        var btn = new Button
        {
            Content = text,
            Padding = new Avalonia.Thickness(14, 6),
            Background = new SolidColorBrush(Color.Parse(colorHex)),
            Foreground = new SolidColorBrush(Colors.White),
        };
        btn.Click += (_, _) => onClick();
        return btn;
    }

    private async Task SendAsync()
    {
        int slot = _activeSlot;
        if (_cts[slot] != null) return; // 该槽位正在运行

        var input = InputBox.Text?.Trim();
        if (string.IsNullOrEmpty(input)) return;
        InputBox.Text = "";
        _drafts[slot] = "";

        Agent agent;
        try { agent = EnsureSlot(slot); }
        catch (Exception ex) { AppendSystem(slot, $"[错误] 初始化 Agent 失败：{ex.Message}"); return; }

        AppendUser(slot, input);
        EnsureAssistant(slot); // 先建 assistant 气泡，流式 token 直接追加
        SendButton.Content = "⏹"; // busy 态 = 停止按钮
        StopButton.IsEnabled = true;
        _cts[slot] = new CancellationTokenSource();

        try
        {
            // Task.Run 隔离：Agent 主循环（LLM SSE 流解析/工具执行）跑在后台线程，
            // 回调内已 Dispatcher.UIThread.Post 回 UI 渲染 —— 避免流式解析/同步工具卡 UI 线程
            await Task.Run(() => agent.ChatAsync(input,
                onToken: t => Dispatcher.UIThread.Post(() => AppendToken(slot, t)),
                onTool: (name, brief) => Dispatcher.UIThread.Post(() => AppendTool(slot, name, brief)),
                onToolOutput: o => Dispatcher.UIThread.Post(() =>
                {
                    if (!string.IsNullOrEmpty(o)) AppendToolOutput(slot, o);
                }),
                cancellationToken: _cts[slot]!.Token));
        }
        catch (OperationCanceledException)
        {
            AppendSystem(slot, "[已停止]");
        }
        catch (Exception ex)
        {
            AppendSystem(slot, $"[错误] {ex.Message}");
        }
        finally
        {
            FinalizeStreaming(slot); // 流式结束定稿
            _cts[slot]?.Dispose();
            _cts[slot] = null;
            if (slot == _activeSlot)
            {
                SendButton.Content = "↑";
                StopButton.IsEnabled = false;
            }
            RefreshPanel(); // 任务完成后立即刷新面板
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  角色化消息追加（对齐 Web app.js 消息体系）
    // ═══════════════════════════════════════════════════════════

    private void AppendUser(int slot, string text)
    {
        var msg = new ChatMessage(ChatRole.User);
        msg.Text.Append(text);
        AddMessage(slot, msg);
    }

    private void AppendSystem(int slot, string text)
    {
        var msg = new ChatMessage(ChatRole.System);
        msg.Text.Append(text);
        AddMessage(slot, msg);
    }

    private void AppendTool(int slot, string name, string brief)
    {
        var msg = new ChatMessage(ChatRole.Tool);
        msg.Text.Append($"🔧 [{name}] {brief}");
        AddMessage(slot, msg);
    }

    private void AppendToolOutput(int slot, string output)
    {
        var truncated = output.Length > 2000
            ? ContextManager.TruncateByRunes(output, 2000) + "\n…（截断）"
            : output;
        var msg = new ChatMessage(ChatRole.ToolOutput);
        msg.Text.Append(truncated);
        AddMessage(slot, msg);
    }

    /// <summary>取当前流式中的 assistant 消息（没有则新建），供 onToken 追加。</summary>
    private ChatMessage EnsureAssistant(int slot)
    {
        var list = _messages[slot];
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].Streaming) return list[i];
        }
        var msg = new ChatMessage(ChatRole.Assistant) { Streaming = true };
        AddMessage(slot, msg);
        return msg;
    }

    private void AppendToken(int slot, string token)
    {
        if (string.IsNullOrEmpty(token)) return;
        var msg = EnsureAssistant(slot);
        msg.Text.Append(token);
        if (slot != _activeSlot) return; // 非活跃槽位只累积，不渲染
        RequestRender(msg);
    }

    /// <summary>合帧渲染：同一 UI 帧内多个 token 只触发一次气泡重渲染（长回复不卡）。</summary>
    private void RequestRender(ChatMessage? target)
    {
        if (_renderPending) return;
        _renderPending = true;
        Dispatcher.UIThread.Post(() =>
        {
            _renderPending = false;
            if (_activeSlot < 0 || _activeSlot >= _messages.Length) return;
            var list = _messages[_activeSlot];
            // 只重渲染活跃槽位最后一个气泡（当前流式目标）
            var msg = target ?? (list.Count > 0 ? list[^1] : null);
            msg?.View?.Render();
            ChatScroll.ScrollToEnd();
        }, DispatcherPriority.Background);
    }

    private void AddMessage(int slot, ChatMessage msg)
    {
        _messages[slot].Add(msg);
        if (slot != _activeSlot) return;
        msg.View = new MessageBubble(msg);
        MessagesHost.Children.Add(msg.View);
        Dispatcher.UIThread.Post(() => ChatScroll.ScrollToEnd(), DispatcherPriority.Background);
    }
}
