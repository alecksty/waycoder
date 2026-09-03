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
    // ═══════════════════════════════════════════════════════════
    //  斜杠命令（GUI 侧实现常用命令，对齐 Web /command）
    // ═══════════════════════════════════════════════════════════

    /// <summary>处理 GUI 斜杠命令。返回 true 表示已消费（不再作为普通消息发送）。</summary>
    private bool TryHandleCommand(string input)
    {
        var cmd = input[1..].Trim();
        var parts = cmd.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var name = parts.Length > 0 ? parts[0].ToLowerInvariant() : "";

        switch (name)
        {
            case "help" or "?":
                AppendSystem(_activeSlot, """
                                          GUI 斜杠命令：
                                          /help      帮助
                                          /model     选择模型
                                          /provider  服务商管理（Key/改名/改地址/删除/测试）
                                          /review    代码审查（git diff + 多维度分析）
                                          /settings  打开设置
                                          /theme     切换深/浅主题
                                          /reset     清空当前会话
                                          /todos     显示任务列表
                                          /tokens    显示本轮 token/费用
                                          /perm      <ask|auto|smart|yolo>  切换交互模式
                                          /slots     槽位说明
                                          """);
                return true;

            case "model":
                new ModelWindow(this).ShowDialog(this);
                return true;

            case "provider":
                new ProviderWindow(this).ShowDialog(this);
                return true;

            case "review" or "审查":
                // 代码审查：生成审查 prompt 作为普通消息投递（后台 Agent 执行，结果流式显示）
                InputBox.Text = ReviewMode.BuildReviewPrompt();
                _ = SendAsync();
                return true;

            case "settings":
                new SettingsWindow(this).ShowDialog(this);
                return true;

            case "theme":
                Theme_Click(null, null!);
                return true;

            case "reset":
                NewSession_Click(null, null!);
                return true;

            case "todos":
            {
                var items = TodoTool.Items;
                if (items == null || items.Count == 0)
                {
                    AppendSystem(_activeSlot, "[无任务]");
                    return true;
                }

                var sb = new StringBuilder();
                foreach (var t in items) sb.AppendLine($"• [{t.Status}] {t.Title}");
                AppendSystem(_activeSlot, sb.ToString());
                return true;
            }

            case "tokens":
            {
                var llm = _agents[_activeSlot]?.LlmClient;
                if (llm == null)
                {
                    AppendSystem(_activeSlot, "[无活动数据]");
                    return true;
                }

                AppendSystem(_activeSlot,
                    $"本轮 {llm.TaskPromptTokens:N0}/{llm.TaskCompletionTokens:N0} · 累计 {llm.TotalPromptTokens:N0}/{llm.TotalCompletionTokens:N0}" +
                    (llm.TaskCost.HasValue ? $" · 费用 ${llm.TaskCost.Value:F4}" : ""));
                return true;
            }

            case "perm":
            {
                if (parts.Length < 2)
                {
                    AppendSystem(_activeSlot, "用法: /perm <ask|auto|smart|yolo>");
                    return true;
                }

                try
                {
                    // 纯聊天别名（tiny/chat）→ 切工作模式 Chat（0 工具 0 提示词）
                    if (PermissionManager.IsChatModeAlias(parts[1]))
                    {
                        WorkModeManager.SetMode(WorkMode.Chat);
                        AppendSystem(_activeSlot, $"[工作模式已切换: 💬 聊天（纯聊天 · 0 工具 0 提示词）]");
                        return true;
                    }

                    PermissionManager.SetMode(parts[1]);
                    var idx = Array.FindIndex(new[] { "Ask", "Auto", "Smart", "YOLO" },
                        m => m.Equals(parts[1], StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0) PermCombo.SelectedIndex = idx;
                    AppendSystem(_activeSlot, $"[交互模式已切换: {parts[1]}]");
                }
                catch (Exception ex)
                {
                    AppendSystem(_activeSlot, $"[切换失败] {ex.Message}");
                }

                return true;
            }

            case "slots":
                AppendSystem(_activeSlot, "F1-F10 切换 10 个独立槽位（各自会话/模型/草稿）；顶栏标签显示当前槽位");
                return true;

            default:
                return false; // 未知命令 → 按普通消息发给 Agent
        }
    }

    // ── 模型 / 主题 / 省钱 / 权限 ──

    /// <summary>切换当前槽位模型（模型弹窗复用此逻辑）。</summary>
    internal void ApplyModel(string modelId, string? providerId = null, string? baseUrl = null)
    {
        UpdateHeader();
        try
        {
            var cfg = Config.Instance;
            // 显式传 baseUrl（GUI 分组点选）→ 精确匹配所选网关；否则内置官方优先
            var info = string.IsNullOrWhiteSpace(baseUrl)
                ? ModelCatalog.Find(modelId)
                : ModelCatalog.Find(modelId, baseUrl);
            if (info == null) return;
            var effProviderId = !string.IsNullOrWhiteSpace(providerId) ? providerId : info.ProviderId;
            var effBaseUrl = !string.IsNullOrWhiteSpace(baseUrl) ? baseUrl : info.DefaultBaseUrl;
            ConnectionConfig.ApplyModelChoice(effProviderId, modelId, true, out _, effBaseUrl);
            var key = ApiKeyStore.Get(effProviderId) ?? cfg.ApiKey;
            var agent = EnsureSlot(_activeSlot);
            agent.LlmClient.Reconfigure(key, cfg.BaseUrl);
            agent.LlmClient.Model = modelId;
            agent.UpdateContextWindow(ModelCatalog.ResolveContextWindow(modelId, cfg.MaxContextTokens));
        }
        catch (Exception ex)
        {
            AppendSystem(_activeSlot, $"[切换模型失败] {ex.Message}");
        }
    }

    /// <summary>切换主题（深浅）。</summary>
    private void Theme_Click(object? sender, RoutedEventArgs e)
    {
        App.ToggleTheme();
        ThemeButton.Content = App.IsDark ? "🌙" : "☀️";
        // 气泡背景走动态资源自动换色；内部 block 文字色是构建时固化的，需显式重渲染。
        // 不能只渲染活跃槽位——非活跃槽位已有 View 的气泡（此前活跃过）仍持旧主题色，必须一并重建。
        for (var i = 0; i < SlotCount; i++)
            foreach (var msg in _messages[i])
                msg.View?.Render();

        // 槽位按钮颜色是切主题前用 GuiColors 静态画刷快照的，主题切换后需重洗（否则 10 个槽位按钮留旧色）。
        UpdateSlotButtons(_activeSlot);
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
        try
        {
            Config.Instance.SaveToEnvFile();
        }
        catch (Exception ex)
        {
            AppendSystem(_activeSlot, $"[保存配置失败] {ex.Message}");
        }
    }

    private void Perm_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (PermCombo.SelectedIndex < 0 || PermCombo.SelectedItem is not ComboBoxItem item) return;
        try
        {
            // 取 ComboBoxItem.Tag（英文标识符）而非显示文本——SetMode 只认 ask/auto/smartauto/yolo。
            if (item.Tag is string mode) PermissionManager.SetMode(mode);
        }
        catch (Exception ex)
        {
            AppendSystem(_activeSlot, $"[设置权限失败] {ex.Message}");
        }
    }

    /// <summary>保存默认模型到配置（不中断当前任务，新会话/重启生效），供模型弹窗调用。</summary>
    internal void SaveDefaultModel(string modelId, bool small, string? providerId = null, string? baseUrl = null)
    {
        var cfg = Config.Instance;
        var pid = providerId ?? (small ? cfg.SmallProvider : cfg.Provider);
        ConnectionConfig.ApplyModelChoice(pid, modelId, !small, out _, baseUrl);
        UpdateHeader();
        AppendSystem(_activeSlot, $"[已保存默认{(small ? "小" : "大")}模型 {modelId}]");
    }

    /// <summary>切换当前槽位小模型，供模型弹窗调用。</summary>
    internal void ApplySmallModel(string modelId, string? providerId = null, string? baseUrl = null)
    {
        var cfg = Config.Instance;
        ConnectionConfig.ApplyModelChoice(providerId ?? cfg.SmallProvider, modelId, false, out _, baseUrl);
        var agent = _agents[_activeSlot];
        if (agent != null) agent.LlmClient.SmallModel = modelId;
        UpdateHeader();
    }

    /// <summary>打开设置窗口。</summary>
    private void Settings_Click(object? sender, RoutedEventArgs e) => new SettingsWindow(this).ShowDialog(this);

    /// <summary>编辑器：打开内置代码编辑器窗口（三端之一，绑定共享 EditorCore）。</summary>
    private void EditorButton_Click(object? sender, RoutedEventArgs e) => new EditorWindow().Show();
}
