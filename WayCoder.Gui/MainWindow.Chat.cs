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
    // ── 交互 ──

    /// <summary>
    /// 发送消息（busy 时 = 停止）
    /// </summary>
    private async void Send_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            // busy 时发送按钮 = 停止（对齐 Web：忙碌变 ⏹）
            if (_cts[_activeSlot] != null)
            {
                _cts[_activeSlot]?.Cancel();
                return;
            }

            await SendAsync();
        }
        catch (Exception err)
        {
            AppendSystem(_activeSlot, $"[发送失败] {err.Message}");
        }
    }

    // ── 附件上传（📎）：图片入 vision 队列 / 音频转录（对齐 Web /upload）──

    private async void Attach_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "上传图片 / 音频",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("图片 / 音频")
                    {
                        Patterns =
                        [
                            "*.png", "*.jpg", "*.jpeg", "*.gif", "*.webp", "*.bmp", "*.mp3", "*.wav", "*.m4a", "*.ogg",
                            "*.webm"
                        ]
                    },
                ],
            });
            if (files == null || files.Count == 0) return;
            var path = files[0].TryGetLocalPath();
            if (!string.IsNullOrEmpty(path)) HandleUpload(path);
        }
        catch (Exception ex)
        {
            AppendSystem(_activeSlot, $"[附件失败] {ex.Message}");
        }
    }

    /// <summary>
    /// 处理附件上传（图片 / 音频）
    /// 支持的格式：.png, .jpg, .jpeg, .gif, .webp, .bmp, .mp3, .wav, .m4a, .ogg, .webm
    /// </summary>
    private async void HandleUpload(string path)
    {
        // 捕获发起槽位：转录是 await（可能耗时数秒），期间切槽会改 _activeSlot，
        // 若再读 _activeSlot 会把转录结果写进切换后的槽位（污染错误会话）。
        var slot = _activeSlot;
        var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        var agent = _agents[slot];
        if (agent == null) return;

        if (IsImageExt(ext))
        {
            LLM.QueueImage(agent.AgentId, path);
            AppendSystem(slot, $"[图片已附加: {Path.GetFileName(path)}]");
        }
        else if (IsAudioExt(ext))
        {
            AppendSystem(slot, $"[转录音频中: {Path.GetFileName(path)}]");
            try
            {
                var text = await Task.Run(() =>
                    new WayCoder.Tools.TranscribeAudioTool().ExecuteAsync(
                            new Dictionary<string, object?> { ["path"] = path })
                        .GetAwaiter().GetResult());
                if (!string.IsNullOrEmpty(text))
                    AppendUser(slot, text);
                else
                    AppendSystem(slot, "[转录无结果]");
            }
            catch (Exception ex)
            {
                AppendSystem(slot, $"[转录失败] {ex.Message}");
            }
        }
        else
        {
            AppendSystem(slot, $"[不支持的格式: .{ext}（图片或音频）]");
        }
    }

    private static bool IsImageExt(string ext) =>
        ext is "png" or "jpg" or "jpeg" or "gif" or "webp" or "bmp";

    private static bool IsAudioExt(string ext) =>
        ext is "mp3" or "wav" or "m4a" or "ogg" or "webm";

    // ── 输入框 ──

    /// <summary>
    /// 输入框 Enter 发送（由 <see cref="ChatInputBox.SendRequested"/> 触发；
    /// Shift+Enter 换行由该控件放行给 TextBox 基类处理）。
    /// </summary>
    private async void OnInputSendRequested() => await SendAsync();

    /// <summary>
    /// 输入卡下方显示当前工作目录（对齐 Web <c>#cwd-bar</c>）。
    /// GUI 的 <c>/cd</c> 是只读信息命令（不切换目录），工作目录即进程启动目录 ——
    /// 与 <c>/cd</c> 报告的值同源，也与 GuiBootstrap 设置 SandboxManager.AllowedDirectory 的取值一致。
    /// 复用 <see cref="PathStatus.FormatCwd"/>：主目录前缀折叠成 ~，与 TUI 状态栏同一套呈现。
    /// </summary>
    private void UpdateCwdBar()
    {
        var cwd = WayCoder.Infra.PathStatus.FormatCwd(Directory.GetCurrentDirectory());
        CwdBar.Text = $"📁 {cwd}";
    }

    /// <summary>输入框自动增高（按行数钳制 56~220，对齐 Web autoResizeInput）。</summary>
    private void Input_TextChanged(object? sender, TextChangedEventArgs e)
    {
        var text = InputBox.Text ?? "";
        var lines = 1;
        for (var i = 0; i < text.Length; i++)
            if (text[i] == '\n')
                lines++;
        InputBox.Height = Math.Clamp(lines * 24, 56, 220);
        UpdateSlashSuggest(); // 输入 / 时显示命令建议列表
    }

    /// <summary>输入 / 前缀 → 显示常用命令建议列表（输入 /xx 过滤；对齐 Web suggest-box）。</summary>
    private void UpdateSlashSuggest()
    {
        var text = InputBox.Text ?? "";
        if (text.StartsWith('/') && text.Length > 1)
        {
            var q = text[1..].Trim().ToLowerInvariant();
            var items = Shared.CommandBar.Favorites
                .Where(f => string.IsNullOrEmpty(q) || f.Name.Contains('/' + q, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Name)
                .ToList();
            SlashSuggest.ItemsSource = items;
            SlashSuggest.IsVisible = items.Count > 0;
        }
        else
        {
            SlashSuggest.IsVisible = false;
        }
    }

    /// <summary>命令建议列表选择事件（输入框自动补全，对齐 Web suggest-box）。</summary>
    private void SlashSuggest_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SlashSuggest.SelectedItem is string cmd && !string.IsNullOrEmpty(cmd))
        {
            InputBox.Text = cmd + " ";
            InputBox.CaretIndex = InputBox.Text.Length;
            InputBox.Focus();
            SlashSuggest.IsVisible = false;
        }
    }

    // ── 发送消息 ──

    private async Task SendAsync(ChatMessage? firstUserMsg = null)
    {
        var slot = _activeSlot;
        var input = InputBox.Text?.Trim();
        if (string.IsNullOrEmpty(input)) return;

        // 斜杠命令即时处理（忙时也执行，不排队，对齐 TUI/Web）
        if (input.StartsWith('/') && input.Length > 1 && TryHandleCommand(input))
        {
            return;
        }

        if (_cts[slot] != null)
        {
            // 排队：不打断当前任务 —— 指令入队，当前批次完成后由 finally 取下一个自动执行。
            // 排队消息立即上屏 + 标「排队中」；队列满则丢最旧并标记「已丢弃」，避免静默吞消息。
            var dropped = false;
            ChatMessage? droppedMsg = null;
            while (_pendingInputs[slot].Count >= Global.MaxPendingSubmissions)
            {
                _pendingInputs[slot].TryDequeue(out var old);
                dropped = true;
                droppedMsg = old?.Msg;
            }

            var queuedMsg = AppendUser(slot, input + "\n⏳ 排队中…");
            _pendingInputs[slot].Enqueue(new PendingItem(input, queuedMsg));
            InputBox.Text = "";
            _drafts[slot] = "";
            if (dropped && droppedMsg != null)
            {
                droppedMsg.Text.Clear();
                droppedMsg.Text.Append("❌ 已丢弃（排队已满）");
                RebuildMessages(slot);
            }

            return;
        }

        InputBox.Text = "";
        _drafts[slot] = "";

        Agent agent;
        try
        {
            agent = EnsureSlot(slot);
        }
        catch (Exception ex)
        {
            AppendSystem(slot, $"[错误] 初始化 Agent 失败：{ex.Message}");
            return;
        }

        if (firstUserMsg != null)
        {
            // 复用排队时已上屏的消息气泡：更新为「发送中」（避免重复上屏）
            firstUserMsg.Text.Clear();
            firstUserMsg.Text.Append(input + "\n📤 发送中…");
            RebuildMessages(slot);
        }
        else AppendUser(slot, input);

        EnsureAssistant(slot); // 先建 assistant 气泡，流式 token 直接追加
        _cts[slot] = new CancellationTokenSource();
        UpdateSendButtonState(slot); // 单按钮：忙 → ⏹ 停止

        var completed = false;
        try
        {
            // Task.Run 隔离：Agent 主循环（LLM SSE 流解析/工具执行）跑在后台线程，
            // 回调内已 Dispatcher.UIThread.Post 回 UI 渲染 —— 避免流式解析/同步工具卡 UI 线程
            await Task.Run(() => agent.ChatAsync(input,
                onToken: t => Dispatcher.UIThread.Post(() =>
                {
                    RefreshStatusBar(slot);
                    AppendToken(slot, t);
                }),
                onTool: (name, brief) => Dispatcher.UIThread.Post(() =>
                {
                    RefreshStatusBar(slot);
                    AppendTool(slot, name, brief);
                }),
                onToolOutput: o => Dispatcher.UIThread.Post(() =>
                {
                    if (!string.IsNullOrEmpty(o))
                    {
                        RefreshStatusBar(slot);
                        AppendToolOutput(slot, o);
                    }
                }),
                cancellationToken: _cts[slot]!.Token));
            completed = true;
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
            _inReasoning[slot] = false; // 复位推理标记：中途停止未收到 «/» 时，下条回复才不会误入推理气泡
            if (firstUserMsg != null)
            {
                // 任务完成：还原排队消息为纯文本（去掉「📤 发送中…」标记，防残留到 UI 与会话历史）
                firstUserMsg.Text.Clear();
                firstUserMsg.Text.Append(input);
                RebuildMessages(slot);
            }

            _cts[slot]?.Dispose();
            _cts[slot] = null;
            if (completed) _completeAtTicks[slot] = Environment.TickCount64; // 完成瞬态（2.5s「任务完成 ✓」）
            if (slot == _activeSlot) UpdateSendButtonState(slot);
            RefreshStatusBar(slot); // 状态栏回落（完成瞬态 / 空闲）
            RefreshPanel(); // 任务完成后立即刷新面板
            TrySendNextPending(slot); // 排队机制：取队列中的下一条指令继续执行
        }
    }

    /// <summary>当前批次完成后，若该槽位有待处理指令则取下一个自动执行（输入排队机制）。</summary>
    private void TrySendNextPending(int slot)
    {
        // 槽位仍忙碌（如切回时任务未完成）→ 不重入。否则 SendAsync 会走排队路径，
        // 把队首项再包一层气泡 + 新 PendingItem，导致原气泡永远「排队中」且顺序错乱。
        if (_cts[slot] != null) return;
        if (_pendingInputs[slot].TryDequeue(out var next))
        {
            // 槽位切换保护：用户已切走该槽位时，不把 A 槽的排队消息塞进当前活跃槽位的输入框发出
            // （否则会作为 B 槽消息发送）——放回队尾，切回该槽位时由 SwitchSlot 重新触发消费。
            if (_activeSlot != slot)
            {
                _pendingInputs[slot].Enqueue(next);
                return;
            }

            InputBox.Text = next.Text;
            _ = SendAsync(next.Msg); // fire-and-forget：继续处理下一条（复用排队时已上屏的气泡）
        }
    }

    // ── 角色化消息追加（对齐 Web app.js 消息体系）──

    /// <summary>追加用户消息。</summary>
    private ChatMessage AppendUser(int slot, string text)
    {
        var msg = new ChatMessage(ChatRole.User);
        msg.Text.Append(text);
        AddMessage(slot, msg);
        return msg;
    }

    /// <summary>追加系统消息。</summary>
    private void AppendSystem(int slot, string text)
    {
        var msg = new ChatMessage(ChatRole.System);
        msg.Text.Append(text);
        AddMessage(slot, msg);
    }

    /// <summary>追加工具消息（对齐 TUI onTool→FinishAgentMsg）。</summary>
    private void AppendTool(int slot, string name, string brief)
    {
        FinalizeStreaming(slot);
        var msg = new ChatMessage(ChatRole.Tool);
        msg.Text.Append($"🔧 [{name}] {brief}");
        AddMessage(slot, msg);
    }

    /// <summary>追加工具输出消息（保头保尾，对齐 TUI Snip 语义）。</summary>
    private void AppendToolOutput(int slot, string output)
    {
        FinalizeStreaming(slot);
        // 外部工具（bash / git / sqlite / 测试运行器…）的输出是进程原始字节，带裸 ANSI 转义序列。
        // GUI 不做 ANSI 上色，但**必须先剥掉**，否则气泡里显示「[0;32m…」一坨乱码 ——
        // 与 TUI（终端解释）/Web（ansiToHtml 解码）/移动端（同样剥掉）呈现同一份命令行文本。
        if (output.Contains(WayCoder.UI.Shared.Terminal.AnsiTty.AnsiCharPrefix))
            output = WayCoder.UI.Shared.AnsiHelper.StripAnsi(output);
        var truncated = output.Length > 2000
            ? ContextManager.TruncateByRunes(output, 1000) + "\n…（截断，关键信息见尾）…\n" +
              ContextManager.TruncateTailByRunes(output, 1000)
            : output;
        var msg = new ChatMessage(ChatRole.ToolOutput);
        msg.Text.Append(truncated);
        AddMessage(slot, msg);
    }

    /// <summary>取当前流式中的 assistant 消息（没有则新建），供 onToken 追加。</summary>
    private ChatMessage EnsureAssistant(int slot)
    {
        var list = _messages[slot];
        for (var i = list.Count - 1; i >= 0; i--)
        {
            // 必须同时判角色：只看 Streaming 会把正文写进还开着的推理气泡
            if (list[i].Role == ChatRole.Assistant && list[i].Streaming) return list[i];
        }

        var msg = new ChatMessage(ChatRole.Assistant) { Streaming = true };
        AddMessage(slot, msg);
        return msg;
    }

    /// <summary>追加推理内容（«dim»/«/» 标记分流，对齐 Web reasoning 独立气泡）。</summary>
    private void AppendToken(int slot, string token)
    {
        if (string.IsNullOrEmpty(token)) return;

        if (token.Contains("«dim»")) _inReasoning[slot] = true;
        if (token.Contains("«/»"))
        {
            _inReasoning[slot] = false;
            foreach (var m in _messages[slot])
                if (m is { Role: ChatRole.Reasoning, Streaming: true })
                    m.Streaming = false;
        }

        var clean = token.Replace("«dim»", "").Replace("«/»", "");
        if (string.IsNullOrEmpty(clean)) return;

        var msg = _inReasoning[slot] ? EnsureReasoning(slot) : EnsureAssistant(slot);
        AppendCapped(msg.Text, clean);
        if (slot != _activeSlot) return; // 非活跃槽位只累积，不渲染
        RequestRender(msg);
    }

    /// <summary>单条消息内容上限：超限保留尾部窗口 + 截断标记（镜像 TUI CapMessageContent）。</summary>
    private static void AppendCapped(StringBuilder sb, string delta)
    {
        var max = Global.MaxSingleMessageChars;
        if (max <= 0 || sb.Length + delta.Length <= max)
        {
            sb.Append(delta);
            return;
        }

        var combined = sb.ToString() + delta;
        var tail = ContextManager.TruncateTailByRunes(combined, max);
        sb.Clear();
        sb.Append("… 已截断（显示最近内容，旧内容滚动省略）…\n").Append(tail);
    }

    /// <summary>取当前流式中的推理气泡（没有则新建）。</summary>
    private ChatMessage EnsureReasoning(int slot)
    {
        var list = _messages[slot];
        for (int i = list.Count - 1; i >= 0; i--)
            if (list[i].Role == ChatRole.Reasoning && list[i].Streaming)
                return list[i];
        var msg = new ChatMessage(ChatRole.Reasoning) { Streaming = true };
        AddMessage(slot, msg);
        return msg;
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
            var msg = target ?? (list.Count > 0 ? list[^1] : null);
            msg?.View?.Render();
            ChatScroll.ScrollToEnd();
        }, DispatcherPriority.Background);
    }

    private void AddMessage(int slot, ChatMessage msg)
    {
        _messages[slot].Add(msg);
        PruneMessages(slot);
        if (slot != _activeSlot) return;
        msg.View = new MessageBubble(msg);
        MessagesHost.Children.Add(msg.View);
        Dispatcher.UIThread.Post(() => ChatScroll.ScrollToEnd(), DispatcherPriority.Background);
    }

    /// <summary>单槽消息条数上限：超 MaxChatMessages 丢最旧（对齐 TUI PruneBuffered），同步移除对应气泡。</summary>
    private void PruneMessages(int slot)
    {
        var list = _messages[slot];
        bool active = slot == _activeSlot;
        while (list.Count > Config.Instance.MaxChatMessages)
        {
            list.RemoveAt(0);
            if (active && MessagesHost.Children.Count > 0)
                MessagesHost.Children.RemoveAt(0);
        }
    }
}
