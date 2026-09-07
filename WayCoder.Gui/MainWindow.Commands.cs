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
        // 经 GUI 注册表分发（GuiCommands 端命令命名与主工程一致）；未命中回退为普通消息发给 Agent
        var (cmd, args) = SlashCommandRegistry.Match(input);
        if (cmd == null) return false;
        cmd.ExecuteAsync(args, null!).GetAwaiter().GetResult();
        return true;
    }

    #region GUI 命令复用入口（供 GuiCommands 经 GuiContext.MainWindow 调用）
    internal Agent? ActiveAgent => _agents[_activeSlot];
    internal int ActiveSlotIndex => _activeSlot;
    internal void NotifySystem(string text) => AppendSystem(_activeSlot, text);
    internal void OpenEditor() => new EditorWindow().Show();
    internal void OpenModelPicker() => new ModelWindow(this).ShowDialog(this);
    internal void OpenProviders() => new ProviderWindow(this).ShowDialog(this);
    internal void OpenSettings() => new SettingsWindow(this).ShowDialog(this);
    internal void RunReview() { InputBox.Text = ReviewMode.BuildReviewPrompt(); _ = SendAsync(); }
    internal void ToggleThemeUi() => Theme_Click(null, null!);
    internal void ResetSession() => NewSession_Click(null, null!);
    internal string TokensSummary()
    {
        var llm = _agents[_activeSlot]?.LlmClient;
        if (llm == null) return "[无活动数据]";
        return $"本轮 {llm.TaskPromptTokens:N0}/{llm.TaskCompletionTokens:N0} · 累计 {llm.TotalPromptTokens:N0}/{llm.TotalCompletionTokens:N0}" +
            (llm.TaskCost.HasValue ? $" · 费用 ${llm.TaskCost.Value:F4}" : "");
    }
    #endregion

    // ── 模型 / 主题 / 省钱 / 权限 ──

    /// <summary>切换当前槽位模型（模型弹窗复用此逻辑）。</summary>
    internal void ApplyModel(string modelId, string? providerId = null, string? baseUrl = null)
    {
        try
        {
            var cfg = Config.Instance;
            // 显式传 baseUrl（GUI 分组点选）→ 精确匹配所选网关；否则内置官方优先
            var info = string.IsNullOrWhiteSpace(baseUrl)
                ? ModelCatalog.Find(modelId)
                : ModelCatalog.Find(modelId, baseUrl);
            if (info == null) return;
            var effProviderId = !string.IsNullOrWhiteSpace(providerId) ? providerId : info.ProviderId;
            // 与 Web 端一致：优先 provider 注册表地址（用户经 ProviderWindow 改地址后的自定义网关），
            // 兼容 provider 未注册时回退模型目录默认地址；不再用目录 DefaultBaseUrl 覆盖自定义网关（此前走代理网关的用户会被打回官方端点）。
            var effBaseUrl = ConnectionConfig.ResolveBaseUrl(effProviderId) ?? info.DefaultBaseUrl;
            ConnectionConfig.ApplyModelChoice(effProviderId, modelId, true, out _, effBaseUrl);
            var key = ApiKeyStore.Get(effProviderId) ?? cfg.ApiKey;
            var agent = EnsureSlot(_activeSlot);
            agent.LlmClient.Reconfigure(key, cfg.BaseUrl);
            agent.LlmClient.Model = modelId;
            agent.UpdateContextWindow(ModelCatalog.ResolveContextWindow(modelId, cfg.MaxContextTokens));
            UpdateHeader(); // 必须在 ApplyModelChoice 改完 cfg.Model/Provider 之后再读，否则头部仍显示旧模型
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

    /// <summary>模型栏主按钮：打开模型选择弹窗（弹窗内可切 大模型|小模型 Tab）。</summary>
    private void CurrentModel_Click(object? sender, RoutedEventArgs e)
        => new ModelWindow(this).ShowDialog(this);

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
