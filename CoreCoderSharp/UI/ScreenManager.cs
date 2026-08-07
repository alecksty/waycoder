using System.Text;
using CoreCoderSharp.Tools;

namespace CoreCoderSharp.UI;

/// <summary>
/// 全屏缓冲管理器 — 备用屏幕 + 聊天历史 + 输入区 + 状态栏。
/// Enter() 切换备用屏，Exit() 恢复。每帧全量重绘。
/// </summary>
public class ScreenManager
{
    // ---- 单例 ----
    public static ScreenManager Instance { get; } = new();
    public bool IsActive { get; private set; }

    // ---- 屏幕 ----
    public int TW, TH;

    // ---- 聊天历史 ----
    public readonly List<ChatMsg> ChatMessages = [];
    private int _chatScroll;
    private bool _autoScroll = true; // 自动跟底

    // ---- 输入 ----
    public readonly List<StringBuilder> InputLines = [new()];
    public int InputCy, InputCx;
    public int InputScroll;

    // ---- 建议 ----
    public bool SuggestActive;
    public List<string> Suggestions = [];
    public int SuggestIdx;
    public int SuggestH; // 面板可见行数

    // ---- 右侧面板 (多标签) ----
    public enum PanelTab { Off, Todo, Files, Locks, MCP }
    public PanelTab ActivePanel;
    public List<TodoItem> TodoItems = [];
    public List<string> ModifiedFiles = [];
    public string LspInfo = "";
    public string McpInfo = "";
    public class TodoItem { public string Title = ""; public string Status = "pending"; }

    // ---- 状态 ----
    public string StatusLeft = "";
    public string StatusRight = "";
    public string TokenInfo = "";
    public bool Running;

    // ---- Todo 面板 ----

    /// <summary>从 TodoTool 同步数据到面板</summary>
    public void SyncTodos()
    {
        TodoItems = TodoTool.Items.Select(i => new TodoItem
        {
            Title = i.Title,
            Status = i.Status
        }).ToList();
    }

    private void RenderTodoLine(StringBuilder sb, int row)
    {
        if (row == 0)
        {
            // 标签栏
            var tabs = new[] { ("📋任务", PanelTab.Todo), ("📁文件", PanelTab.Files), ("🔒锁", PanelTab.Locks), ("🔌MCP", PanelTab.MCP) };
            sb.Append(" [1m");
            foreach (var (name, tab) in tabs)
            {
                if (ActivePanel == tab) sb.Append("[30;46m"); // 选中反色
                sb.Append($"{name} ");
                if (ActivePanel == tab) sb.Append("[0m[1m");
            }
            sb.Append("[0m");
            return;
        }

        var idx = row - 1;
        switch (ActivePanel)
        {
            case PanelTab.Todo:
                if (idx < TodoItems.Count)
                {
                    var item = TodoItems[idx];
                    var (icon, color) = item.Status switch
                    {
                        "completed" => ("✅", "32"), "in_progress" => ("🔄", "36"),
                        "cancelled" => ("❌", "90"), _ => ("⏳", "33"),
                    };
                    var text = $"{icon} {item.Title}";
                    if (VW(text) > 28) text = TruncateByVW(text, 25) + "…";
                    sb.Append($" [{color}m{text}[0m");
                }
                break;

            case PanelTab.Files:
                if (idx == 0) sb.Append(" 修改/工程文件:");
                else if (idx - 1 < ModifiedFiles.Count)
                {
                    var f = ModifiedFiles[idx - 1];
                    if (VW(f) > 26) f = "…" + f[^Math.Min(f.Length, 25)..];
                    sb.Append($" 📄 {f}");
                }
                else
                {
                    var treeIdx = idx - 1 - ModifiedFiles.Count;
                    var tree = GetFileTree();
                    if (treeIdx < tree.Count)
                    {
                        var entry = tree[treeIdx];
                        if (VW(entry) > 26) entry = TruncateByVW(entry, 23) + "…";
                        sb.Append($" {entry}");
                    }
                }
                break;

            case PanelTab.Locks:
                if (idx == 0) sb.Append(" 文件锁");
                else
                {
                    var locks = FileLockManager.GetAllLocks();
                    if (idx - 1 < locks.Count)
                    {
                        var l = locks[idx - 1];
                        var f = Path.GetFileName(l.FilePath);
                        if (f.Length > 20) f = f[..17] + "…";
                        sb.Append($" 🔒 {f} ({l.AgentId})");
                    }
                }
                break;

            case PanelTab.MCP:
                if (idx == 0) sb.Append(" [2mMCP 服务器[0m");
                else if (!string.IsNullOrEmpty(McpInfo))
                    sb.Append($" [2m{McpInfo}[0m");
                else
                    sb.Append(" [2m未配置[0m");
                break;
        }
    }

    private static List<string>? _fileTreeCache;
    private static DateTime _fileTreeCacheTime;

    private static List<string> GetFileTree()
    {
        if (_fileTreeCache != null && (DateTime.Now - _fileTreeCacheTime).TotalSeconds < 5)
            return _fileTreeCache;

        var files = new List<string>();
        try
        {
            foreach (var entry in Directory.GetFileSystemEntries(".", "*",
                new EnumerationOptions { RecurseSubdirectories = true, MaxRecursionDepth = 2 }))
            {
                var rel = Path.GetRelativePath(".", entry);
                if (rel.StartsWith(".") || rel.StartsWith("obj/") || rel.StartsWith("bin/") || rel.Contains("/.")) continue;
                var prefix = Directory.Exists(entry) ? "📁 " : "  ";
                files.Add($"{prefix}{rel}");
                if (files.Count >= 100) break;
            }
            _fileTreeCache = files.OrderBy(f => !f.StartsWith("📁")).ThenBy(f => f).ToList();
            _fileTreeCacheTime = DateTime.Now;
        }
        catch { _fileTreeCache = []; }
        return _fileTreeCache;
    }

    /// <summary>更新右下角 token/计费/缓存显示</summary>
    public void UpdateTokenDisplay(int promptTok, int compTok, double? cost, int contextUsed, int contextMax)
    {
        var parts = new List<string>();
        parts.Add($"↑{FormatK(promptTok)} ↓{FormatK(compTok)}");
        if (cost.HasValue) parts.Add($"${cost.Value:F4}");
        if (contextMax > 0)
        {
            var pct = (int)(contextUsed * 100.0 / contextMax);
            parts.Add($"上下文 {BoxBuffer.MiniBar(pct, 6)}");
        }
        TokenInfo = string.Join(" · ", parts);
    }

    private static string FormatK(int n) => n >= 1000 ? $"{n / 1000.0:F1}k" : n.ToString();

    /// <summary>聊天区向上滚动</summary>
    public void ChatScrollUp(int lines = 1) { _chatScroll -= lines; _autoScroll = false; }
    /// <summary>聊天区向下滚动</summary>
    public void ChatScrollDown(int lines = 1) { _chatScroll += lines; var max = Math.Max(0, BuildChatScreenLines(Math.Max(20, TW - (ActivePanel != PanelTab.Off ? 32 : 0)), Math.Max(3, TH - 10)).Count - Math.Max(3, TH - 10)); if (_chatScroll >= max) _autoScroll = true; }
    /// <summary>聊天区跳到顶部</summary>
    public void ChatScrollTop() { _chatScroll = 0; _autoScroll = false; }
    /// <summary>聊天区跳到底部</summary>
    public void ChatScrollBottom() { _autoScroll = true; }

    // ---- 聊天消息定义 ----
    public class ChatMsg
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
        public bool Streaming;
    }

    // ================================================================
    // 居中弹窗 (对话框 / 菜单)
    // ================================================================

    public enum DialogType { Info, Success, Warn, Error }

    /// <summary>显示居中对话框，任意键关闭</summary>
    public void ShowDialog(string title, string content, DialogType type = DialogType.Info)
    {
        var color = type switch
        {
            DialogType.Success => "32", // 绿
            DialogType.Warn => "33",    // 黄
            DialogType.Error => "31",   // 红
            _ => "36",                  // 青
        };

        var lines = content.Replace("\r\n", "\n").Split('\n');
        var maxW = Math.Min(TW - 8, lines.Max(l => VW(l)) + 4);
        if (!string.IsNullOrEmpty(title)) maxW = Math.Max(maxW, VW(title) + 4);
        var w = Math.Max(20, maxW);
        var h = lines.Length + 4; // 上框 + 标题 + 内容 + padding + 下框
        var x = (TW - w) / 2;
        var y = (TH - h) / 2;

        var sb = new StringBuilder();
        // 遮罩背景
        for (int i = 0; i < TH; i++)
            sb.Append($"[{i + 1};1H[100m{new string(' ', TW)}[0m");
        // 重绘原框架内容（简化：只保留文字区域）

        // 对话框
        sb.Append($"[{y};{x}H[{color}m╭{new string('─', w - 2)}╮[0m");
        if (!string.IsNullOrEmpty(title))
        {
            sb.Append($"[{y + 1};{x}H[{color}m│[0m [1m{title}[0m" +
                $"{new string(' ', Math.Max(0, w - VW(title) - 4))}[{color}m│[0m");
        }
        for (int i = 0; i < lines.Length; i++)
            sb.Append($"[{y + 2 + i};{x}H[{color}m│[0m {lines[i]}" +
                $"{new string(' ', Math.Max(0, w - VW(lines[i]) - 4))}[{color}m│[0m");
        sb.Append($"[{y + 2 + lines.Length};{x}H[{color}m│[0m 按任意键关闭" +
            $"{new string(' ', Math.Max(0, w - VW("按任意键关闭") - 6))}[{color}m│[0m");
        sb.Append($"[{y + h - 1};{x}H[{color}m╰{new string('─', w - 2)}╯[0m");

        Console.Write(sb.ToString());
        Console.ReadKey(intercept: true);
    }

    /// <summary>显示居中菜单，返回选择的索引 (-1 表示取消)</summary>
    public int ShowMenu(string title, List<string> choices)
    {
        var w = Math.Min(TW - 8, Math.Max(20, choices.Max(c => VW(c)) + 4));
        if (!string.IsNullOrEmpty(title)) w = Math.Max(w, VW(title) + 4);
        var h = choices.Count + 4; // 上框 + title + items + hint + 下框
        var x = (TW - w) / 2;
        var y = (TH - h) / 2;
        int sel = 0;

        while (true)
        {
            var sb = new StringBuilder();
            // 遮罩
            for (int i = 0; i < TH; i++)
                sb.Append($"[{i + 1};1H[100m{new string(' ', TW)}[0m");

            // 菜单框
            sb.Append($"[{y};{x}H[36m╭{new string('─', w - 2)}╮[0m");
            if (!string.IsNullOrEmpty(title))
                sb.Append($"[{y + 1};{x}H[36m│[0m [1m{title}[0m" +
                    $"{new string(' ', Math.Max(0, w - VW(title) - 4))}[36m│[0m");
            for (int i = 0; i < choices.Count; i++)
            {
                var text = choices[i];
                if (VW(text) > w - 8) text = TruncateByVW(text, w - 9) + "…";
                sb.Append($"[{y + 2 + i};{x}H[36m│[0m ");
                sb.Append(i == sel ? $"[30;46m {text} [0m" : $" {text} ");
                var fill = Math.Max(0, w - 4 - VW(text) - 2);
                if (i != sel) sb.Append(new string(' ', fill));
                sb.Append($"[36m│[0m");
            }
            sb.Append($"[{y + h - 1};{x}H[36m╰{new string('─', w - 2)}╯[0m");
            sb.Append("[?25h");
            Console.Write(sb.ToString());

            var key = Console.ReadKey(intercept: true);
            switch (key.Key)
            {
                case ConsoleKey.UpArrow: if (sel > 0) sel--; break;
                case ConsoleKey.DownArrow: if (sel < choices.Count - 1) sel++; break;
                case ConsoleKey.Enter: return sel;
                case ConsoleKey.Escape: return -1;
            }
        }
    }

    // ================================================================
    // 屏幕控制
    // ================================================================

    /// <summary>
    /// 显示 About 对话框 — ASCII 大字标题 + 确定按钮
    /// </summary>
    public static void ShowAbout()
    {
        var logo = new[]
        {
            "   ██████╗ ██████╗ ██████╗ ███████╗",
            "  ██╔════╝██╔═══██╗██╔══██╗██╔════╝",
            "  ██║     ██║   ██║██████╔╝█████╗  ",
            "  ██║     ██║   ██║██╔══██╗██╔══╝  ",
            "  ╚██████╗╚██████╔╝██║  ██║███████╗",
            "   ╚═════╝ ╚═════╝ ╚═╝  ╚═╝╚══════╝",
        };
        var subtitle = "极简 AI 编程智能体 · v0.15.1";
        var credit = "C# / .NET 10 · AOT 编译";

        var boxW = 46;
        var boxH = 12;
        var boxX = Math.Max(1, (Console.WindowWidth - boxW) / 2);
        var boxY = Math.Max(1, (Console.WindowHeight - boxH) / 2);

        Console.CursorVisible = false;
        var sb = new StringBuilder();

        for (int i = 0; i < Console.WindowHeight; i++)
            sb.Append($"\x1b[{i + 1};1H\x1b[100m{new string(' ', Console.WindowWidth)}\x1b[0m");

        var box = new BoxBuffer
        {
            X = boxX, Y = boxY, Width = boxW, Height = boxH,
            FgColor = "36", Border = BorderStyle.Double,
        };
        box.Render(sb);

        for (int i = 0; i < logo.Length; i++)
            box.WriteAt(sb, i, 1, $"\x1b[1m\x1b[96m{logo[i]}\x1b[0m");

        var subPad = Math.Max(0, (box.ContentWidth - BoxBuffer.VwPlainText(subtitle)) / 2);
        box.WriteAt(sb, 7, subPad, $"\x1b[37m{subtitle}\x1b[0m");

        var credPad = Math.Max(0, (box.ContentWidth - BoxBuffer.VwPlainText(credit)) / 2);
        box.WriteAt(sb, 8, credPad, $"\x1b[2m{credit}\x1b[0m");

        var btnText = " 确 定 ";
        box.WriteLineHighlight(sb, 10, "30", "46", btnText);

        Console.Write(sb.ToString());
        Console.ReadKey(intercept: true);
    }

    /// <summary>
    /// 渲染底部快捷键栏
    /// </summary>
    private void RenderHotkeyBar(StringBuilder sb, int row)
    {
        var hotkeys = new (string key, string desc)[]
        {
            ("F1", "帮助"), ("F2", "面板"), ("F5", "设置"),
            ("F6", "编辑"), ("F10", "退出"),
            ("Ctrl+PgUp", "聊天顶"), ("Ctrl+PgDn", "聊天底"),
        };

        sb.Append($"\x1b[{row};1H\x1b[44m\x1b[37m\x1b[K");
        foreach (var (key, desc) in hotkeys)
        {
            sb.Append($" \x1b[33m\x1b[1m{key}\x1b[0m\x1b[44m\x1b[37m {desc}");
        }
        var used = hotkeys.Sum(h => 3 + h.key.Length + h.desc.Length);
        var remain = Console.WindowWidth - used;
        if (remain > 0) sb.Append(new string(' ', remain));
        sb.Append("\x1b[0m");
    }

    /// <summary>进入全屏模式</summary>
    public void Enter()
    {
        Console.Write("[?1049h[2J[?25l");
        (TW, TH) = (Console.WindowWidth, Console.WindowHeight);
        IsActive = true;
    }

    /// <summary>退出全屏模式</summary>
    public void Exit()
    {
        IsActive = false;
        Console.Write("[?25h[?1049l[2J");
    }

    // ================================================================
    // 聊天操作
    // ================================================================

    public void AddUserMsg(string text)
    {
        // 拆分多行消息
        foreach (var line in text.Split('\n'))
            ChatMessages.Add(new ChatMsg { Role = "user", Content = line });
    }

    public void StartAgentMsg()
    {
        ChatMessages.Add(new ChatMsg { Role = "agent", Content = "", Streaming = true });
    }

    public void AppendToken(string token)
    {
        var last = ChatMessages.LastOrDefault();
        if (last == null || last.Role != "agent" || !last.Streaming)
        {
            last = new ChatMsg { Role = "agent", Content = "", Streaming = true };
            ChatMessages.Add(last);
        }
        last.Content += token;
    }

    public void FinishAgentMsg()
    {
        var last = ChatMessages.LastOrDefault();
        if (last != null && last.Streaming) last.Streaming = false;
    }

    public void AddToolMsg(string toolName, string brief)
    {
        ChatMessages.Add(new ChatMsg { Role = "tool", Content = $"🔧 {toolName}({brief})" });
    }

    public void AddSystemMsg(string text)
    {
        ChatMessages.Add(new ChatMsg { Role = "system", Content = text });
    }

    // ================================================================
    // 输入操作
    // ================================================================

    public void SetInput(string text)
    {
        InputLines.Clear();
        InputLines.Add(new StringBuilder(text));
        InputCy = 0;
        InputCx = text.Length;
        InputScroll = 0;
    }

    public string GetInputText() =>
        string.Join("\n", InputLines.Select(l => l.ToString())).TrimEnd();

    // ================================================================
    // 全帧渲染
    // ================================================================

    public void Render()
    {
        (TW, TH) = (Console.WindowWidth, Console.WindowHeight);
        var sb = new StringBuilder();
        sb.Append("[?25l[H"); // 隐藏光标 + 回左上角

        // ---- 布局计算 ----
        var inputH = ComputeInputScreenH();
        var statusH = 1;
        var hotkeyH = 1;
        var suggestH = SuggestActive ? Math.Min(Suggestions.Count, 10) + 2 : 0;
        var topH = 1;
        var sepH = 1;
        var panelW = ActivePanel != PanelTab.Off ? 32 : 0;
        var chatW = Math.Max(20, TW - panelW);
        var chatH = TH - topH - sepH - suggestH - inputH - 1 - statusH - hotkeyH;
        if (chatH < 3) chatH = 3;

        var chatScreenLines = BuildChatScreenLines(chatW, chatH);
        var totalCLines = chatScreenLines.Count;
        var maxScroll = Math.Max(0, totalCLines - chatH);
        if (_autoScroll) _chatScroll = maxScroll;
        _chatScroll = Math.Clamp(_chatScroll, 0, maxScroll);

        // ---- 顶栏 (蓝底白字) ----
        var topText = $" CoreCoder v0.15.1 · {StatusLeft}";
        if (ActivePanel != PanelTab.Off) topText += $"  [F2 面板:{ActivePanel}]";
        sb.Append($"[44;37m{topText}{new string(' ', Math.Max(0, TW - VW(topText)))}[0m\r\n");

        // ---- 分隔线 (青色) ----
        sb.Append($"[36m{new string('─', TW)}[0m\r\n");

        // ---- 聊天区 + 侧边栏 ----
        int chatRow = topH + sepH;
        for (int i = 0; i < chatH; i++)
        {
            // 聊天内容 (左)
            int si = _chatScroll + i;
            sb.Append($"[{chatRow + i + 1};1H");
            if (si < chatScreenLines.Count)
            {
                var cl = chatScreenLines[si];
                RenderChatLine(sb, cl);
            }
            sb.Append("[K");

            // Todo 面板 (右)
            if (ActivePanel != PanelTab.Off)
            {
                sb.Append($"[{chatRow + i + 1};{chatW + 1}H[90m│[0m");
                RenderTodoLine(sb, i);
                sb.Append("[K");
            }
        }

        // ---- 建议面板 ----
        if (SuggestActive && suggestH > 0)
        {
            int suggestRow = chatRow + chatH + sepH;
            RenderSuggestions(sb, suggestRow, suggestH, chatW);
        }

        // ---- 输入区 ----
        int inputRow = chatRow + chatH + sepH + suggestH;
        RenderInputArea(sb, inputRow, inputH);

        // ---- 状态栏 ----
        int statusRow = inputRow + inputH + 1;
        var modeLabel = InputLines.Count > 1 ? "多行" : "聊天";
        var chCount = InputLines.Sum(l => l.Length);
        var leftStatus = $"  {modeLabel}  L{InputCy + 1}:C{InputCx + 1}  {chCount}字符  Enter发送 Ctrl+Enter换行 Esc取消";
        var rightInfo = TokenInfo.Length > 0 ? $"{TokenInfo}  " : "";
        var rightW = VW(rightInfo);
        var availW = TW - rightW - 1;
        if (VW(leftStatus) > availW) leftStatus = TruncateByVW(leftStatus, availW - 1) + "…";
        var pad = Math.Max(0, availW - VW(leftStatus));
        // 状态栏: 灰色底 + 白字左 + 黄字右
        sb.Append($"[{statusRow};1H[100m[37m{leftStatus}{new string(' ', pad)}[33m{rightInfo}[0m");

        // ---- 快捷键栏 ----
        int hotkeyRow = statusRow + 1;
        RenderHotkeyBar(sb, hotkeyRow);

        // ---- 光标定位 ----
        var (scrCy, scrCx) = InputHardToScreen(TW - 4);
        var cursorRow = inputRow + 1 + (scrCy - InputScroll);
        var cursorCol = 2 + scrCx + 1;
        cursorCol = Math.Clamp(cursorCol, 2, TW - 2);
        sb.Append($"[{cursorRow};{cursorCol}H[?25h");

        Console.Write(sb.ToString());
    }

    // ================================================================
    // 聊天行渲染
    // ================================================================

    private record ChatScreenLine(ChatMsg Msg, string Text);

    private List<ChatScreenLine> BuildChatScreenLines(int tw, int chatH)
    {
        var result = new List<ChatScreenLine>();
        foreach (var msg in ChatMessages)
        {
            var prefix = msg.Role switch
            {
                "user" => "❯ ",
                "tool" => "  ",
                "system" => "  ",
                _ => "  ",
            };
            var prefixVW = VW(prefix);
            var maxVW = tw - prefixVW - 2;

            // 将消息内容按视觉宽度折行
            var content = msg.Content;
            if (string.IsNullOrEmpty(content))
            {
                result.Add(new ChatScreenLine(msg, prefix));
                continue;
            }

            int offset = 0;
            bool first = true;
            while (offset < content.Length)
            {
                var slice = content[offset..];
                var (vch, vcw) = MeasureSlice(slice, maxVW);
                var text = first ? prefix + slice[..vch] : "  " + slice[..vch];
                result.Add(new ChatScreenLine(msg, text));
                offset += vch;
                first = false;
                if (vch == 0) break;
            }
        }
        return result;
    }

    private void RenderChatLine(StringBuilder sb, ChatScreenLine cl)
    {
        var msg = cl.Msg;
        switch (msg.Role)
        {
            case "user":
                sb.Append($"[36m{cl.Text}[0m");
                break;
            case "tool":
                sb.Append($"[2m{cl.Text}[0m");
                break;
            case "system":
                sb.Append($"[2m{cl.Text}[0m");
                break;
            default:
                sb.Append(cl.Text);
                break;
        }
        // 如果是流式最后一条，加闪烁光标效果
        if (msg.Streaming && msg == ChatMessages.LastOrDefault())
            sb.Append("[5m ▏[0m");
    }

    // ================================================================
    // 建议面板渲染
    // ================================================================

    private void RenderSuggestions(StringBuilder sb, int startRow, int suggestH, int chatW)
    {
        sb.Append($"[{startRow};1H[36m╭─ 建议 (↑↓选择 Enter确认 Esc取消) {new string('─', Math.Max(0, chatW - 23))}╮[0m\r\n");
        for (int i = 0; i < suggestH - 2; i++)
        {
            sb.Append("[36m│[0m ");
            if (i < Suggestions.Count)
            {
                var text = Suggestions[i];
                var maxW = chatW - 4;
                if (VW(text) > maxW) text = TruncateByVW(text, maxW - 1) + "…";
                sb.Append(i == SuggestIdx ? $"[30;46m {text} [0m" : $" {text} ");
                var fill = Math.Max(0, TW - 4 - VW(text) - 2);
                if (i != SuggestIdx) sb.Append(new string(' ', fill));
            }
            else
            {
                sb.Append(new string(' ', TW - 4));
            }
            sb.Append(" [36m│[0m\r\n");
        }
        sb.Append($"[36m╰{new string('─', Math.Max(0, TW - 2))}╯[0m");
    }

    // ================================================================
    // 输入区渲染
    // ================================================================

    private int ComputeInputScreenH()
    {
        var cw = TW - 4;
        var screenLines = BuildInputScreenLines(cw);
        return Math.Clamp(screenLines.Count, 1, 5);
    }

    private List<InputScreenLine> BuildInputScreenLines(int contentW)
    {
        var result = new List<InputScreenLine>();
        for (int hi = 0; hi < InputLines.Count; hi++)
        {
            var text = InputLines[hi].ToString();
            int offset = 0;
            while (offset < text.Length || (offset == 0 && result.Count == 0))
            {
                var slice = text[offset..];
                var (vch, vcw) = MeasureSlice(slice, contentW);
                result.Add(new InputScreenLine(hi, offset, vch, vcw));
                offset += vch;
                if (vch == 0) break;
            }
        }
        if (result.Count == 0)
            result.Add(new InputScreenLine(0, 0, 0, 0));
        return result;
    }

    private (int scrLine, int scrCol) InputHardToScreen(int contentW)
    {
        var lines = BuildInputScreenLines(contentW);
        for (int i = 0; i < lines.Count; i++)
        {
            var sl = lines[i];
            if (sl.HardLine == InputCy && InputCx >= sl.HardOffset && InputCx <= sl.HardOffset + sl.Chars)
            {
                var before = InputLines[InputCy].ToString()[sl.HardOffset..InputCx];
                return (i, VW(before));
            }
        }
        return (lines.Count - 1, lines[^1].VW);
    }

    private record InputScreenLine(int HardLine, int HardOffset, int Chars, int VW);

    private void RenderInputArea(StringBuilder sb, int startRow, int vh)
    {
        var cw = TW - 4;
        var screenLines = BuildInputScreenLines(cw);

        // 调整滚动
        var total = screenLines.Count;
        if (InputCy < 0) InputCy = 0; // safety

        var (scrCy, _) = InputHardToScreen(cw);
        if (scrCy < InputScroll) InputScroll = scrCy;
        if (scrCy >= InputScroll + vh) InputScroll = scrCy - vh + 1;
        InputScroll = Math.Clamp(InputScroll, 0, Math.Max(0, total - vh));

        var dash = new string('─', Math.Max(0, TW - 2));

        sb.Append($"[{startRow};1H[2m╭{dash}╮[0m\r\n");

        for (int i = 0; i < vh; i++)
        {
            var si = InputScroll + i;
            sb.Append("[2m│[0m ");
            if (si < screenLines.Count && screenLines[si].Chars > 0)
            {
                var sl = screenLines[si];
                var text = InputLines[sl.HardLine].ToString();
                var slice = text.Substring(sl.HardOffset,
                    Math.Min(sl.Chars, text.Length - sl.HardOffset));
                sb.Append(slice);
                var pad = cw - sl.VW;
                if (pad > 0) sb.Append(new string(' ', pad));
            }
            else
            {
                sb.Append(new string(' ', cw));
            }
            sb.Append(" [2m│[0m\r\n");
        }

        sb.Append($"[2m╰{dash}╯[0m");
    }

    // ================================================================
    // 输入编辑 (复用 TuiInput 逻辑的内联版本)
    // ================================================================

    public void InputInsert(char ch)
    {
        InputLines[InputCy].Insert(InputCx, ch);
        InputCx++;
    }

    public void InputNewLine()
    {
        var rest = InputLines[InputCy].ToString()[InputCx..];
        InputLines[InputCy].Remove(InputCx, InputLines[InputCy].Length - InputCx);
        InputLines.Insert(InputCy + 1, new StringBuilder(rest));
        InputCy++;
        InputCx = 0;
    }

    public void InputBackspace()
    {
        if (InputCx > 0) { InputLines[InputCy].Remove(InputCx - 1, 1); InputCx--; }
        else if (InputCy > 0)
        {
            InputCx = InputLines[InputCy - 1].Length;
            InputLines[InputCy - 1].Append(InputLines[InputCy]);
            InputLines.RemoveAt(InputCy);
            InputCy--;
        }
    }

    public void InputDelete()
    {
        if (InputCx < InputLines[InputCy].Length) InputLines[InputCy].Remove(InputCx, 1);
        else if (InputCy < InputLines.Count - 1)
        {
            InputLines[InputCy].Append(InputLines[InputCy + 1]);
            InputLines.RemoveAt(InputCy + 1);
        }
    }

    public void InputMoveLeft()
    {
        if (InputCx > 0) InputCx--;
        else if (InputCy > 0) { InputCy--; InputCx = InputLines[InputCy].Length; }
    }

    public void InputMoveRight()
    {
        if (InputCx < InputLines[InputCy].Length) InputCx++;
        else if (InputCy < InputLines.Count - 1) { InputCy++; InputCx = 0; }
    }

    public void InputMoveUp()
    {
        if (InputCy > 0) { InputCy--; InputCx = Math.Min(InputCx, InputLines[InputCy].Length); }
    }

    public void InputMoveDown()
    {
        if (InputCy < InputLines.Count - 1)
        { InputCy++; InputCx = Math.Min(InputCx, InputLines[InputCy].Length); }
    }

    // ---- 建议更新 ----
    public void UpdateSuggestions()
    {
        var text = GetInputText().TrimStart();
        if (string.IsNullOrEmpty(text) || (text[0] != '/' && text[0] != '#' && text[0] != '!'))
        {
            SuggestActive = false;
            return;
        }

        var prefix = text;
        Suggestions = text[0] switch
        {
            '/' => TuiInputCommands.Where(c => c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).Take(10).ToList(),
            '#' => GetFileSuggestions(prefix.Length > 1 ? prefix[1..] : ""),
            '!' => ["!<shell 命令>", "!git status", "!ls -la", "!npm test", "!dotnet build"],
            _ => [],
        };

        SuggestActive = Suggestions.Count > 0;
        SuggestH = Math.Min(Suggestions.Count, 10) + 2;
        SuggestIdx = 0;
    }

    public void AcceptSuggestion()
    {
        if (!SuggestActive || SuggestIdx >= Suggestions.Count) return;
        var chosen = Suggestions[SuggestIdx];
        InputLines.Clear();
        InputLines.Add(new StringBuilder(chosen));
        InputCy = 0;
        InputCx = chosen.Length;
        SuggestActive = false;
    }

    // 内置命令
    private static readonly string[] TuiInputCommands =
    [
        "/help", "/reset", "/model", "/model <名称>", "/tokens",
        "/compact", "/diff", "/save", "/sessions",
        "/debug-on", "/debug-off", "/permissions", "/perm <模式>",
        "/plan", "/todo", "/git-status", "/git-log", "/git-diff",
        "/review", "/lint", "/search <关键词>",
        "/checkpoint", "/undo [编号]", "/checkpoints",
        "/repomap", "/pr [标题]", "/edit [文件]", "/settings", "quit",
    ];

    private static List<string> GetFileSuggestions(string partial)
    {
        var results = new List<string>();
        try
        {
            var dir = ".";
            var prefix = partial;
            var lastSep = partial.LastIndexOfAny(['/', '\\']);
            if (lastSep >= 0) { dir = partial[..(lastSep + 1)]; prefix = partial[(lastSep + 1)..]; }
            if (!Directory.Exists(dir)) return results;

            results = Directory.GetFileSystemEntries(dir)
                .Select(Path.GetFileName)
                .Where(f => f!.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => !Directory.Exists(Path.Combine(dir, f!)))
                .ThenBy(f => f)
                .Take(10)
                .Select(f =>
                {
                    var name = f!;
                    if (Directory.Exists(Path.Combine(dir, name))) name += "/";
                    return "#" + (lastSep >= 0 ? partial[..(lastSep + 1)] : "") + name;
                })
                .ToList();
        }
        catch { }
        return results;
    }

    // ================================================================
    // 辅助
    // ================================================================

    private static (int chars, int vw) MeasureSlice(string text, int maxVW)
    {
        int chars = 0, vw = 0;
        var runes = text.EnumerateRunes().ToList();
        for (int i = 0; i < runes.Count; i++)
        {
            var w = runes[i].Value > 127 ? 2 : 1;
            if (vw + w > maxVW) break;
            vw += w;
            chars++;
        }
        return (chars, vw);
    }

    private static int VW(string s)
    {
        int w = 0;
        foreach (var r in s.EnumerateRunes()) w += r.Value > 127 ? 2 : 1;
        return w;
    }

    private static string TruncateByVW(string text, int maxVW)
    {
        int vw = 0, chars = 0;
        foreach (var r in text.EnumerateRunes())
        { var w = r.Value > 127 ? 2 : 1; if (vw + w > maxVW) break; vw += w; chars++; }
        return text[..chars];
    }
}
