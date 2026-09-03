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

public partial class MainWindow
{
    private static string SlotSessionId(int slot) => slot == 0 ? "_auto" : $"_auto_slot{slot}";

    private void SaveAllSessions()
    {
        for (var i = 0; i < SlotCount; i++)
        {
            var agent = _agents[i];
            if (agent == null) continue;
            var msgs = agent.SnapshotMessages();
            if (msgs.Count == 0)
            {
                continue;
            }

            try
            {
                var model = agent.LlmClient.Model;
                var baseUrl = agent.LlmClient.BaseUrl;
                // 推导 provider：模型+网关精确匹配；未命中回退全局配置（自定义模型不在目录的兜底）。
                // 连同 baseUrl 一起存，供 LoadSessionById 恢复时重配 endpoint（防 model id 发错网关）。
                var provider = string.IsNullOrWhiteSpace(baseUrl)
                    ? Config.Instance.Provider
                    : (ModelCatalog.Find(model, baseUrl)?.ProviderId ?? Config.Instance.Provider);
                SessionManager.SaveSession(msgs, model, SlotSessionId(i), i, provider, baseUrl);
            }
            catch
            {
                /* 保存失败不影响退出 */
            }
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
        catch
        {
            /* 无历史会话则跳过 */
        }
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
                var empty = new TextBlock
                {
                    Text = "暂无历史会话",
                    FontSize = 12,
                    Margin = new Thickness(8, 4),
                    [!TextBlock.ForegroundProperty] = new DynamicResourceExtension("DimTextBrush")
                };
                panel.Children.Add(empty);
            }
            else
            {
                foreach (var s in sessions)
                    panel.Children.Add(BuildSessionItem(s));
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }

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
            [!TextBlock.ForegroundProperty] = new DynamicResourceExtension("DimTextBrush")
        };

        // hover 操作（✎ 重命名 / ✕ 删除）
        var ops = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Right,
            IsVisible = false,
        };
        ops.Children.Add(UiKit.MakeSmallButton("✎", () => RenameSessionDialog(s.Id)));
        ops.Children.Add(UiKit.MakeSmallButton("✕", () => DeleteSessionConfirm(s.Id)));

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

    private void LoadSessionById(string id)
    {
        var agent = EnsureSlot(_activeSlot);
        try
        {
            var loaded = SessionManager.LoadSessionDetailed(id, _activeSlot);
            if (loaded == null) return;
            agent.ReplaceMessages(loaded.Messages);
            if (!string.IsNullOrEmpty(loaded.Model))
                agent.LlmClient.Model = loaded.Model!;
            // 恢复会话保存时的网关 + 对应 key：model id 不能配错 endpoint（否则发错服务器/鉴权失败）。
            // 旧会话缺 provider/base_url → 跳过，保持当前网关（回退兼容）。
            if (!string.IsNullOrWhiteSpace(loaded.BaseUrl))
            {
                var key = !string.IsNullOrWhiteSpace(loaded.Provider)
                    ? (ApiKeyStore.Get(loaded.Provider) ?? agent.LlmClient.ApiKey)
                    : agent.LlmClient.ApiKey;
                agent.LlmClient.Reconfigure(key, loaded.BaseUrl);
            }
            RebuildChatFromAgent(_activeSlot, agent);
            UpdateHeader();
            AppendSystem(_activeSlot, $"[已加载会话 {id}]");
        }
        catch (Exception ex)
        {
            AppendSystem(_activeSlot, $"[加载会话失败] {ex.Message}");
        }
    }

    /// <summary>创建新会话。</summary>
    private void NewSession_Click(object? sender, RoutedEventArgs e)
    {
        var agent = EnsureSlot(_activeSlot);
        agent.ReplaceMessages([]);
        _messages[_activeSlot].Clear();
        _inReasoning[_activeSlot] = false;
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
            Background = GuiColors.PanelBg,
        };
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 12 };
        panel.Children.Add(new TextBlock
            { Text = $"重命名 {oldId}", Foreground = GuiColors.Text });
        var box = new TextBox { Text = oldId };
        panel.Children.Add(box);
        var btns = new StackPanel
            { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right };
        btns.Children.Add(UiKit.MakeButton("确定", ButtonVariant.Primary, () =>
        {
            try
            {
                var newId = box.Text?.Trim();
                if (!string.IsNullOrEmpty(newId) && newId != oldId)
                {
                    SessionManager.RenameSession(oldId, newId, _activeSlot);
                }
            }
            catch (Exception ex)
            {
                AppendSystem(_activeSlot, $"[重命名失败] {ex.Message}");
            }

            win.Close();
            RefreshSessions();
        }));
        btns.Children.Add(UiKit.MakeButton("取消", ButtonVariant.Secondary, win.Close));
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
            Background = GuiColors.PanelBg,
        };
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 12 };
        panel.Children.Add(new TextBlock
            { Text = $"确定删除会话 {id}？", Foreground = GuiColors.Text });
        var btns = new StackPanel
            { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right };
        btns.Children.Add(UiKit.MakeButton("删除", ButtonVariant.Danger, () =>
        {
            SessionManager.DeleteSession(id, _activeSlot);
            win.Close();
            RefreshSessions();
        }));
        btns.Children.Add(UiKit.MakeButton("取消", ButtonVariant.Secondary, win.Close));
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
}
