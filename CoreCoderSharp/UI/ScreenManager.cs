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

    // ---- 主题 (从 Config 刷新) ----
    public string ThemeBorderColor = "36";
    public string ThemeAccentColor = "36";
    public string ThemeBorderStyle = "rounded";

    /// <summary>从 Config 刷新主题设置</summary>
    public void RefreshTheme()
    {
        var cfg = Config.FromEnv();
        ThemeBorderColor = cfg.BorderColor;
        ThemeAccentColor = cfg.AccentColor;
        ThemeBorderStyle = cfg.BorderStyle;
    }

    /// <summary>获取边框字符集</summary>
    private (string tl, string tr, string bl, string br, string h, string v) BorderChars() => ThemeBorderStyle switch
    {
        "double" => ("╔", "╗", "╚", "╝", "═", "║"),
        "bold" => ("┏", "┓", "┗", "┛", "━", "┃"),
        "single" => ("┌", "┐", "└", "┘", "─", "│"),
        _ => ("╭", "╮", "╰", "╯", "─", "│"), // rounded (默认)
    };

    // ---- 聊天历史 ----
    public readonly List<ChatMsg> ChatMessages = [];
    private int _chatScroll;
    private bool _autoScroll = true; // 自动跟底
    private string _lastCleanFrame = ""; // 上一帧无浮层的渲染输出（窗口关闭时还原背景）

    // ---- 输入 ----
    public readonly List<StringBuilder> InputLines = [new()];
    public int InputCy, InputCx;
    public int InputScroll;
    private int _inputContentRow; // 输入区首行屏幕行号
    private int _inputH;          // 输入区屏幕行数

    // ---- 建议 ----
    public bool SuggestActive;
    public List<string> Suggestions = [];
    public int SuggestIdx;
    public int SuggestScroll;  // 建议列表滚动偏移
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
    /// <summary>上一帧无浮层的渲染输出（窗口关闭时还原背景）</summary>
    public string LastCleanFrame => _lastCleanFrame;

    /// <summary>从全局主题同步主界面配色</summary>
    public void SyncTheme()
    {
        var t = ThemeConfig.Instance;
        ThemeBorderColor = t.BorderColor.ToString();
        ThemeAccentColor = t.BorderColor.ToString();
        // 应用主题预设到颜色方案
        if (t.BorderStyle == "rounded") ThemeBorderStyle = "rounded";
        else if (t.BorderStyle == "double") ThemeBorderStyle = "double";
        else ThemeBorderStyle = "single";
    }
    public string StatusRight = "";
    public string TokenInfo = "";
    public string? GitBranch;
    public readonly List<string> RecentFiles = [];
    public bool Running;
    private int _frameCount;  // 用于思考动画闪烁

    // ---- 行内权限确认 (模仿 Claude Code) ----
    public string? PermTitle;      // 第一行：标题
    public string? PermContent;    // 第二行：详细内容
    public List<string> PermissionChoices = [];
    public int PermissionSelectedIdx;

    /// <summary>
    /// 行内权限确认（三行模式）—— 在聊天区和输入区之间显示，模仿竞品风格。
    /// 第一行标题，第二行内容，第三行选择按钮。
    /// </summary>
    public int ShowInlinePermission(string title, string content, List<string> choices)
    {
        SuggestActive = false;
        var titleLine = title.Replace('\n', ' ').Trim();
        var contentLine = content.Replace('\n', ' ').Trim();
        PermTitle = $"⚠ {titleLine}";
        PermContent = contentLine;
        PermissionChoices = choices;
        PermissionSelectedIdx = 0;

        Render();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (Console.WindowWidth != TW || Console.WindowHeight != TH)
                Render();

            switch (key.Key)
            {
                case ConsoleKey.Y: ClearPerm(); Render(); return 0;
                case ConsoleKey.A: ClearPerm(); Render(); return 1;
                case ConsoleKey.N: case ConsoleKey.Escape: ClearPerm(); Render(); return -1;
                case ConsoleKey.Enter: ClearPerm(); Render(); return PermissionSelectedIdx;
                case ConsoleKey.LeftArrow: case ConsoleKey.UpArrow:
                    if (PermissionSelectedIdx > 0) { PermissionSelectedIdx--; Render(); }
                    break;
                case ConsoleKey.RightArrow: case ConsoleKey.DownArrow:
                    if (PermissionSelectedIdx < choices.Count - 1) { PermissionSelectedIdx++; Render(); }
                    break;
                default:
                    if (key.KeyChar == 'y' || key.KeyChar == 'Y') { ClearPerm(); Render(); return 0; }
                    if (key.KeyChar == 'a' || key.KeyChar == 'A') { ClearPerm(); Render(); return 1; }
                    if (key.KeyChar == 'n' || key.KeyChar == 'N') { ClearPerm(); Render(); return -1; }
                    break;
            }
        }
    }

    private void ClearPerm() { PermTitle = null; PermContent = null; }

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
                else if (!string.IsNullOrEmpty(McpManager.Info))
                    sb.Append($" [2m{McpManager.Info}[0m");
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
    public void UpdateTokenDisplay(int promptTok, int compTok, double? cost, int contextUsed, int contextMax,
        double latencyMs = 0, double tokensPerSec = 0)
    {
        var parts = new List<string>();
        parts.Add($"↑{FormatK(promptTok)} ↓{FormatK(compTok)}");
        if (cost.HasValue) parts.Add($"${cost.Value:F4}");
        if (latencyMs > 0)
            parts.Add($"{latencyMs / 1000:F1}s {tokensPerSec:F0}t/s");
        if (contextMax > 0)
        {
            var pct = (int)(contextUsed * 100.0 / contextMax);
            parts.Add($"{pct}%上下文");
        }
        TokenInfo = string.Join(" · ", parts);
    }

    private static string FormatK(int n) => n >= 1000 ? $"{n / 1000.0:F1}k" : n.ToString();

    /// <summary>聊天区向上滚动</summary>
    public void ChatScrollUp(int lines = 1) { _chatScroll = Math.Max(0, _chatScroll - lines); _autoScroll = false; }
    /// <summary>聊天区向下滚动</summary>
    public void ChatScrollDown(int lines = 1)
    {
        _chatScroll += lines;
        // 延迟到下一次 Render 里判断是否触底，避免重复构建 chatScreenLines
        _maybeSnapToBottom = true;
    }
    private bool _maybeSnapToBottom;
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
        /// <summary>工具执行中（用于 tool 角色消息，运行中显示 spinner）</summary>
        public bool ToolRunning;
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
            DialogType.Success => 32,
            DialogType.Warn => 33,
            DialogType.Error => 31,
            _ => 36,
        };
        var win = WindowManager.Instance.ShowDialog(title, content);
        win.BorderColor = color;
        Render();
        Console.ReadKey(intercept: true);
        WindowManager.Instance.Close(win);
        Render();
    }

    /// <summary>显示居中菜单，返回选择的索引 (-1 表示取消)。
    /// 集成到主事件循环：resize 即时响应，鼠标可用。</summary>
    public int ShowMenu(string title, List<string> choices)
    {
        var cleanTitle = title.Replace('\n', ' ').Trim();
        SuggestActive = false;
        AddSystemMsg($"📋 {cleanTitle}");
        var x = (Console.WindowWidth - Math.Min(Console.WindowWidth - 8, Math.Max(20, choices.Max(c => TuiHelper.DisplayWidth(c)) + 4))) / 2;
        var y = (Console.WindowHeight - Math.Min(choices.Count, Console.WindowHeight - 8) - 4) / 2;
        var win = WindowManager.Instance.ShowMenu(x, y, cleanTitle, choices);

        while (true)
        {
            Render();
            var key = Console.ReadKey(intercept: true);

            // Resize 重新居中
            if (Console.WindowWidth != TW || Console.WindowHeight != TH)
            {
                win.X = (Console.WindowWidth - win.Width) / 2;
                win.Y = (Console.WindowHeight - win.Height) / 2;
                Render();
            }

            var result = WindowManager.Instance.HandleMenuKey(win, key);
            if (result >= 0) { WindowManager.Instance.Close(win); Render(); return result; }
            if (result == -1) { WindowManager.Instance.Close(win); Render(); return -1; }
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
        var content = "WayCoder 道码 · 中文版易用编程智能体\nC# / .NET 10 · AOT 编译\n深圳市探索智能科技有限公司";
        var win = WindowManager.Instance.ShowDialog("关于 WayCoder", content, width: 46);
        Instance.Render();
        Console.ReadKey(intercept: true);
        WindowManager.Instance.Close(win);
        Instance.Render();
    }

    /// <summary>
    /// /test 命令 —— 测试 UI 组件和模块
    /// </summary>
    public static void RunTestDemo(string target)
    {
        target = target.Trim().ToLowerInvariant();
        var sm = Instance;

        switch (target)
        {
            case "perm" or "权限框":
                sm.ShowInlinePermission("⚠ 确认执行危险操作",
                    "工具: bash\n命令: rm -rf /tmp/build\n工作目录: /home/user/project",
                    ["允许 (y)", "总是允许 (a)", "拒绝 (n)"]);
                break;

            case "toast" or "提示框":
                var toastWin = WindowManager.Instance.ShowToast("✅ 操作已完成 (2s 自动消失)", 2000);
                Instance.Render();
                Thread.Sleep(2000);
                WindowManager.Instance.Close(toastWin);
                Instance.Render();
                break;

            case "menu" or "菜单":
                var menuWin = WindowManager.Instance.ShowMenu(
                    (Console.WindowWidth - 30) / 2,
                    (Console.WindowHeight - 10) / 2,
                    "测试菜单 ↑↓ 选择",
                    ["选项 Alpha", "选项 Beta", "─", "选项 Gamma", "选项 Delta"]);
                Instance.Render();
                while (true)
                {
                    var key = Console.ReadKey(intercept: true);
                    var result = WindowManager.Instance.HandleMenuKey(menuWin, key);
                    if (result >= 0) { sm.AddSystemMsg($"菜单选中: [{result}] {menuWin.MenuItems[result]}"); break; }
                    if (result == -1) { sm.AddSystemMsg("菜单已取消"); break; }
                    Instance.Render();
                }
                WindowManager.Instance.Close(menuWin);
                Instance.Render();
                break;

            case "dialog" or "对话框":
                var dlg = WindowManager.Instance.ShowDialog("测试对话框",
                    "这是对话框内容。\n第二行文本。\n第三行文本。\n\n按任意键关闭。", width: 42);
                Instance.Render();
                Console.ReadKey(intercept: true);
                WindowManager.Instance.Close(dlg);
                Instance.Render();
                break;

            case "panel" or "侧边栏":
                sm.ActivePanel = sm.ActivePanel switch
                {
                    PanelTab.Off => PanelTab.Todo,
                    PanelTab.Todo => PanelTab.Files,
                    PanelTab.Files => PanelTab.Locks,
                    PanelTab.Locks => PanelTab.MCP,
                    _ => PanelTab.Off,
                };
                var plabel = sm.ActivePanel switch
                {
                    PanelTab.Off => "关闭",
                    PanelTab.Todo => "Todo 面板",
                    PanelTab.Files => "修改文件列表",
                    PanelTab.Locks => "文件锁状态",
                    PanelTab.MCP => "MCP 服务器",
                    _ => "?",
                };
                sm.AddSystemMsg($"侧边栏: {plabel} (F2 切换)");
                break;

            case "status" or "状态栏":
                var oldLeft = sm.StatusLeft;
                var oldRight = sm.TokenInfo;
                sm.StatusLeft = "🔧 测试模式";
                sm.TokenInfo = "gpt-5.4 · ↑12.3k ↓8.7k · $0.0042 · ctx 15%";
                sm.Render();
                Thread.Sleep(2000);
                sm.StatusLeft = oldLeft;
                sm.TokenInfo = oldRight;
                sm.Render();
                break;

            case "suggest" or "建议框":
                sm.Suggestions = ["/help", "/reset", "/model gpt-5.4", "/model deepseek-v4",
                    "/tokens", "/diff", "/save", "/plan", "/settings", "/about"];
                sm.SuggestActive = true;
                sm.SuggestIdx = 0;
                sm.SuggestScroll = 0;
                sm.SuggestH = 10;
                sm.Render();
                Thread.Sleep(3000);
                sm.SuggestActive = false;
                sm.Render();
                break;

            case "chat" or "聊天区":
                sm.AddSystemMsg("=== 聊天区测试开始 ===");
                sm.ChatMessages.Add(new ChatMsg { Role = "user", Content = "你好，请用中文回答" });
                sm.ChatMessages.Add(new ChatMsg { Role = "agent", Content = "你好！我是 WayCoder，很高兴为你服务。\n\n有什么我可以帮你的吗？" });
                sm.ChatMessages.Add(new ChatMsg { Role = "user",
                    Content = "写一个 C# Hello World 程序，并且解释每一行代码的作用" });
                sm.ChatMessages.Add(new ChatMsg { Role = "agent",
                    Content = "## C# Hello World\n\n```csharp\nusing System;\n\nclass Program\n{\n    static void Main()\n    {\n        Console.WriteLine(\"Hello, World!\");\n    }\n}\n```\n\n### 逐行解释\n\n1. `using System;` — 引入 System 命名空间\n2. `class Program` — 定义 Program 类\n3. `static void Main()` — 程序入口点\n4. `Console.WriteLine(...)` — 输出到控制台" });
                sm.Render();
                break;

            case "editor" or "编辑器":
                var testFile = Path.GetTempFileName() + ".cs";
                File.WriteAllText(testFile, "using System;\n\nclass Test\n{\n    static void Main()\n    {\n        Console.WriteLine(\"test\");\n    }\n}\n");
                Editor.RunAsync(testFile).GetAwaiter().GetResult();
                sm.ModifiedFiles = Tools.EditFileTool.ChangedFiles.ToList();
                sm.Render();
                break;

            case "settings" or "设置":
                SettingsPage.Show();
                break;

            case "theme" or "主题":
                sm.AddSystemMsg($"可选主题: {string.Join(", ", ThemeConfig.Presets.Keys)}");
                sm.AddSystemMsg("切换主题: /theme <名称>");
                break;

            case "help" or "帮助":
                var helps = new List<string> {
                    "=== 内置命令 ===",
                    "/help       显示帮助",
                    "/reset      重置对话",
                    "/model      切换模型",
                    "/tokens     查看用量",
                    "/diff       查看变更",
                    "/plan       计划模式",
                    "/settings   设置界面",
                    "/test       测试 UI 组件",
                    "/perm       权限设置",
                    "/compact    压缩上下文",
                };
                foreach (var h in helps) sm.AddSystemMsg(h);
                break;

            case "all":
                // 非阻塞的顺序测试
                foreach (var t in new[] { "status", "chat", "panel", "toast", "menu", "dialog", "suggest", "perm" })
                    RunTestDemo(t);
                break;

            default:
                sm.AddSystemMsg("/test <项目>:");
                sm.AddSystemMsg("  perm 权限框 / toast 提示框 / menu 菜单 / dialog 对话框");
                sm.AddSystemMsg("  status 状态栏 / panel 侧边栏 / suggest 建议框 / chat 聊天区");
                sm.AddSystemMsg("  editor 编辑器 / settings 设置 / theme 主题 / help 帮助");
                sm.AddSystemMsg("  all 全部测试");
                break;
        }
    }

    /// <summary>
    /// 渲染底部快捷键栏
    /// </summary>
    private void RenderHotkeyBar(StringBuilder sb, int row)
    {
        var hotkeys = new (string key, string desc)[]
        {
            ("F1", "帮助"), ("F2", "面板"), ("F5", "设置"),
            ("F6", "编辑"), ("↑↓", "历史"), ("Ctrl+←→", "单词"),
            ("Ctrl+R", "搜索"), ("Ctrl+M", "切模型"),
        };

        var rb = new Terminal.RenderBuffer();
        rb.MoveTo(row, 0).ClearToEndOfLine();
        rb.Segment("", fg: 37, bg: 44);
        foreach (var (key, desc) in hotkeys)
        {
            rb.Segment(" ", fg: 37, bg: 44);
            rb.SegmentBold(key);
            rb.Segment($" {desc}", fg: 37, bg: 44);
        }
        var used = hotkeys.Sum(h => 3 + h.key.Length + h.desc.Length);
        var remain = Console.WindowWidth - used;
        if (remain > 0) rb.Raw(new string(' ', remain));
        rb.Reset();
        sb.Append(rb.ToString());
    }

    /// <summary>进入全屏模式（切换备用屏，退出时自动恢复原终端内容）</summary>
    public void Enter()
    {
        Terminal.TTY.EnterAltScreen();
        (TW, TH) = (Console.WindowWidth, Console.WindowHeight);
        IsActive = true;
    }

    /// <summary>退出全屏模式 — 恢复原始终端内容</summary>
    public void Exit()
    {
        IsActive = false;
        Terminal.TTY.ExitAltScreen();
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
        ChatMessages.Add(new ChatMsg { Role = "tool", Content = $"  🔧 {toolName}({brief})" });
    }

    /// <summary>工具开始执行（行内进度消息，RenderChatLine 自动追加闪烁 ⚙）</summary>
    public void AddToolProgress(string toolName, string brief)
    {
        ChatMessages.Add(new ChatMsg
        {
            Role = "tool",
            Content = $"  {toolName}({brief})",
            ToolRunning = true,
        });
    }

    /// <summary>工具执行完成（更新最后一条匹配的运行中工具消息为 💡）</summary>
    public void FinishToolProgress(string toolName, double elapsedSec)
    {
        // 从后往前找第一个 ToolRunning 且匹配 toolName 的消息
        for (int i = ChatMessages.Count - 1; i >= 0; i--)
        {
            var msg = ChatMessages[i];
            if (msg.ToolRunning && msg.Content.Contains(toolName))
            {
                msg.ToolRunning = false;
                msg.Content = $"  💡 {toolName} ({elapsedSec:F1}s)";
                return;
            }
        }
    }

    public void AddSystemMsg(string text)
    {
        ChatMessages.Add(new ChatMsg { Role = "system", Content = text });
        _maybeSnapToBottom = true;
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

    /// <summary>鼠标点击输入区：将屏幕坐标映射到光标位置</summary>
    public void HandleMouseClick(int mouseX, int mouseY)
    {
        int inputTop = _inputContentRow;
        int inputBottom = _inputContentRow + _inputH;
        if (mouseY < inputTop || mouseY >= inputBottom) return;

        var cw = TW - 2;
        var screenLines = BuildInputScreenLines(cw);
        int si = InputScroll + (mouseY - inputTop);
        if (si < 0 || si >= screenLines.Count) return;

        var sl = screenLines[si];
        int colInRow = mouseX - 2; // "> " 或 "  " 前缀各 2 列
        if (colInRow < 0) colInRow = 0;

        // 将视觉列映射到硬行字符偏移
        var text = InputLines[sl.HardLine].ToString();
        int charIdx = sl.HardOffset;
        int vw = 0;
        int pos = sl.HardOffset;
        while (pos < text.Length)
        {
            var rune = System.Text.Rune.GetRuneAt(text, pos);
            var w = TuiHelper.RuneWidth(rune);
            if (vw + w > colInRow) break;
            vw += w;
            charIdx = pos + rune.Utf16SequenceLength;
            pos = charIdx;
        }

        InputCy = sl.HardLine;
        InputCx = charIdx;
        // 边界修正
        if (InputCy < 0) InputCy = 0;
        if (InputCy >= InputLines.Count) InputCy = InputLines.Count - 1;
        var curLine = InputLines[InputCy].ToString();
        if (InputCx > curLine.Length) InputCx = curLine.Length;
        if (InputCx < 0) InputCx = 0;

        Render();
    }

    /// <summary>在光标处粘贴剪贴板文本</summary>
    public async Task PasteFromClipboardAsync()
    {
        var text = await ClipboardHelper.GetTextAsync();
        if (string.IsNullOrEmpty(text)) return;
        // 清理控制字符（保留换行）
        text = SanitizePasteText(text);
        foreach (var ch in text)
        {
            if (ch == '\n')
                InputNewLine();
            else if (ch == '\r')
                continue;
            else if (ch >= ' ' || ch == '\t')
                InputInsert(ch);
        }
        UpdateSuggestions();
        Render();
    }

    /// <summary>清理粘贴文本中的控制字符</summary>
    private static string SanitizePasteText(string text)
    {
        var sb = new StringBuilder();
        foreach (var ch in text)
        {
            if (ch == '\r') continue;
            if (ch >= ' ' || ch == '\n' || ch == '\t')
                sb.Append(ch);
        }
        return sb.ToString();
    }

    // ================================================================
    // 全帧渲染
    // ================================================================

    public void Render()
    {
        (TW, TH) = (Console.WindowWidth, Console.WindowHeight);
        _frameCount++;
        var sb = new StringBuilder();
        sb.Append("[?25l[H"); // 隐藏光标 + 回左上角

        // ---- 布局计算（无顶栏/热键栏，模仿 Claude Code 极简布局）----
        var inputH = ComputeInputScreenH();
        var statusH = 1;
        var suggestH = SuggestActive ? Math.Min(Suggestions.Count, 8) + 2 : 0;
        var permH = PermTitle != null ? 3 : 0;
        var inputSepH = 2;  // 输入区上下两条 dim 分割线
        var panelW = ActivePanel != PanelTab.Off ? 32 : 0;
        var chatW = Math.Max(20, TW - panelW);
        // 可用行数 = 总高 - 建议面板 - 输入行 - 状态栏 - 权限块 - 分割线
        var chatH = TH - suggestH - inputH - statusH - permH - inputSepH;
        if (chatH < 3) chatH = 3;

        var chatScreenLines = BuildChatScreenLines(chatW, chatH);
        var totalCLines = chatScreenLines.Count;
        var maxScroll = Math.Max(0, totalCLines - chatH);
        if (_autoScroll) _chatScroll = maxScroll;
        else if (_maybeSnapToBottom && _chatScroll >= maxScroll)
        { _autoScroll = true; _chatScroll = maxScroll; }
        _maybeSnapToBottom = false;
        _chatScroll = Math.Clamp(_chatScroll, 0, maxScroll);

        // ---- 聊天区 + 侧边栏（从第 0 行开始）----
        int chatRow = 0;
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
                var rpv = new Terminal.RenderBuffer();
                rpv.Write(chatRow + i, chatW, "│", fg: int.TryParse(ThemeBorderColor, out var _pvbc) ? _pvbc : 36);
                sb.Append(rpv.ToString());
                RenderTodoLine(sb, i);
                sb.Append("[K");
            }
        }

        // ---- 建议面板 ----
        if (SuggestActive && suggestH > 0)
        {
            int suggestRow = chatRow + chatH;
            RenderSuggestions(sb, suggestRow, suggestH, chatW);
        }

        // ---- 权限确认块 (三行行内渲染) ----
        int permRow = chatRow + chatH + suggestH;
        if (PermTitle != null)
        {
            RenderPermissionBlock(sb, permRow);
        }

        // ---- 输入区（上下有 dim 分割线）----
        int inputTopRow = permRow + permH;
        // 上分割线（dim）
        var sepLine = new string('─', TW);
        var rbSepTop = new Terminal.RenderBuffer();
        rbSepTop.MoveTo(inputTopRow, 0);
        rbSepTop.Segment(sepLine, fg: 2);
        sb.Append(rbSepTop.ToString());
        // 输入内容（从下一行开始）
        int inputContentRow = inputTopRow + 1;
        _inputContentRow = inputContentRow;
        _inputH = inputH;
        RenderInputArea(sb, inputContentRow, inputH);
        // 下分割线（dim）
        int inputBottomRow = inputContentRow + inputH;
        var rbSepBot = new Terminal.RenderBuffer();
        rbSepBot.MoveTo(inputBottomRow, 0);
        rbSepBot.Segment(sepLine, fg: 2);
        sb.Append(rbSepBot.ToString());

        // ---- 状态栏（极简一行：模型 · token/成本/上下文）----
        int statusRow = inputBottomRow + 1;
        var modelInfo = StatusLeft.Length > 0 ? StatusLeft : "";
        var rightInfo = TokenInfo.Length > 0 ? TokenInfo : "";
        // 如果左右都有内容，用 · 连接；否则只显示有内容的一边
        string fullStatus;
        if (modelInfo.Length > 0 && rightInfo.Length > 0)
            fullStatus = $" {modelInfo} · {rightInfo}";
        else if (modelInfo.Length > 0)
            fullStatus = $" {modelInfo}";
        else if (rightInfo.Length > 0)
            fullStatus = $" {rightInfo}";
        else
            fullStatus = " WayCoder";
        if (VW(fullStatus) > TW) fullStatus = TruncateByVW(fullStatus, TW - 1) + "…";
        var rb2 = new Terminal.RenderBuffer();
        rb2.MoveTo(statusRow, 0);
        rb2.Segment(fullStatus, fg: 37, bg: 100);
        rb2.ClearToEndOfLine();
        sb.Append(rb2.ToString());

        // ---- 保存无浮层的帧用于窗口关闭时还原背景 ----
        _lastCleanFrame = sb.ToString();

        // ---- 浮层窗口（对话框/菜单/提示框）----
        WindowManager.Instance.RenderOverlay(sb);

        // ---- 光标定位 (ANSI 1-based，> 前缀占 2 列) ----
        var (scrCy, scrCx) = InputHardToScreen(TW - 2);
        var cursorRow = inputContentRow + (scrCy - InputScroll) + 1; // +1: ANSI 1-based
        var cursorCol = 2 + scrCx + 1; // "> " 2列 + ANSI 1-based
        cursorCol = Math.Clamp(cursorCol, 2, TW - 1);
        sb.Append($"[?25h[{cursorRow};{cursorCol}H");
        DebugLog.Log("cursor", $"TH={TH} chatH={chatH} inputContentRow={inputContentRow} inputH={inputH} scrCy={scrCy} scroll={InputScroll} cursorRow={cursorRow} cursorCol={cursorCol}");

        Console.Write(sb.ToString());
    }

    // ================================================================
    // 聊天行渲染
    // ================================================================

    private record ChatScreenLine(ChatMsg Msg, List<(string Text, int Fg, int Bg)> Segments);

    private List<ChatScreenLine> BuildChatScreenLines(int tw, int chatH)
    {
        var result = new List<ChatScreenLine>();
        string? lastRole = null;

        foreach (var msg in ChatMessages)
        {
            // 跳过空内容消息
            if (string.IsNullOrEmpty(msg.Content))
                continue;

            // 用户/agent 对话前加空行（角色切换时）
            if (lastRole != null && msg.Role != lastRole
                && (msg.Role == "user" || msg.Role == "agent"))
            {
                result.Add(new ChatScreenLine(new ChatMsg { Role = "spacer" },
                    new List<(string, int, int)>()));
            }
            lastRole = msg.Role;

            var maxVW = tw - 2;
            var renderedLines = TuiMarkdown.RenderMessage(msg.Content, msg.Role, maxVW);

            // 图标前缀
            var prefix = msg.Role switch
            {
                "agent" => "○ ",
                "user" => "□ ",
                _ => ""
            };

            for (int li = 0; li < renderedLines.Count; li++)
            {
                var line = renderedLines[li];
                if (li == 0 && prefix.Length > 0)
                {
                    var newLine = new List<(string, int, int)> { (prefix, 2, 0) };
                    newLine.AddRange(line);
                    result.Add(new ChatScreenLine(msg, newLine));
                }
                else if (prefix.Length > 0)
                {
                    var indentLine = new List<(string, int, int)> { ("  ", 0, 0) };
                    indentLine.AddRange(line);
                    result.Add(new ChatScreenLine(msg, indentLine));
                }
                else
                {
                    result.Add(new ChatScreenLine(msg, line));
                }
            }
        }
        return result;
    }

    private void RenderChatLine(StringBuilder sb, ChatScreenLine cl)
    {
        var msg = cl.Msg;
        var rb = new Terminal.RenderBuffer();
        foreach (var (text, fg, bg) in cl.Segments)
        {
            if (string.IsNullOrEmpty(text)) continue;
            rb.Segment(text, fg, bg);
        }
        sb.Append(rb.ToString());
    }

    // ================================================================
    // 建议面板渲染
    // ================================================================

    private void RenderSuggestions(StringBuilder sb, int startRow, int suggestH, int chatW)
    {
        var (_, _, _, _, sh, _) = BorderChars();
        int totalItems = Suggestions.Count;
        if (totalItems == 0) return;
        int bc = int.TryParse(ThemeBorderColor, out var _sbc) ? _sbc : 36;

        var maxItemVw = Suggestions.Max(s => VW(s));
        var panelW = Math.Min(chatW, Math.Max(maxItemVw + 6, chatW / 2));
        int itemRows = suggestH - 2; // 上下边框各占 1 行

        // 滚动跟随选中项
        if (SuggestIdx < SuggestScroll) SuggestScroll = SuggestIdx;
        else if (SuggestIdx >= SuggestScroll + itemRows) SuggestScroll = SuggestIdx - itemRows + 1;
        SuggestScroll = Math.Clamp(SuggestScroll, 0, Math.Max(0, totalItems - itemRows));

        var rb = new Terminal.RenderBuffer();

        // 上边框：标题 + 填充横线
        var titleText = " 建议 ↑↓ Enter ";
        var titleVW = VW("┌") + VW(titleText);
        var fillLen = Math.Max(0, panelW - titleVW - VW("┐"));
        rb.MoveTo(startRow, 0);
        rb.Segment("┌" + titleText + new string(sh[0], fillLen) + "┐", fg: bc);

        // 内容行（无 more 指示器，纯项目列表）
        int rightCol = panelW - 1;
        for (int i = 0; i < itemRows; i++)
        {
            int ci = SuggestScroll + i;
            int row = startRow + 1 + i;

            rb.MoveTo(row, 0);
            rb.Segment("│", fg: bc);

            if (ci < totalItems)
            {
                var text = Suggestions[ci];
                var maxW = panelW - 3; // 左边框 + 空格 + 右边空格 + 右边框 = 3
                if (VW(text) > maxW) text = TruncateByVW(text, maxW - 1) + "…";
                if (ci == SuggestIdx) rb.Segment($" {text} ", fg: 30, bg: 46);
                else rb.Segment($" {text} ");
            }

            rb.MoveTo(row, rightCol);
            rb.Segment("│", fg: bc);
        }

        // 下边框
        rb.MoveTo(startRow + suggestH - 1, 0);
        rb.Segment("└" + new string(sh[0], panelW - 2) + "┘", fg: bc);

        sb.Append(rb.ToString());
    }

    // ================================================================
    // 输入区渲染
    // ================================================================

    private int ComputeInputScreenH()
    {
        var cw = TW - 2; // "> " 前缀占用 2 列
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
        // 从后往前查找：当光标恰好在换行边界（InputCx == 上一行末尾 == 下一行开头），
        // 光标应该显示在下一行开头（而非上一行末尾），反向遍历确保匹配到正确的行。
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            var sl = lines[i];
            if (sl.HardLine == InputCy && InputCx >= sl.HardOffset && InputCx <= sl.HardOffset + sl.Chars)
            {
                var before = InputLines[InputCy].ToString()[sl.HardOffset..InputCx];
                return (i, VW(before));
            }
        }
        if (lines.Count > 0)
        {
            var last = lines[^1];
            var text = InputLines[InputCy].ToString();
            var before = text.Length >= last.HardOffset ? text[last.HardOffset..] : "";
            return (lines.Count - 1, VW(before));
        }
        return (0, 0);
    }

    private record InputScreenLine(int HardLine, int HardOffset, int Chars, int VW);

    /// <summary>
    /// 无边框行内输入区 —— 模仿 Claude Code 的 > 提示符风格。
    /// 多行时续行用两个空格缩进，思考中显示闪烁光标。
    /// </summary>
    /// <summary>
    /// 行内权限确认块渲染（三行统一黄底）。
    /// 第1行：⚠ 标题（黄底黑字）
    /// 第2行：详细内容（黄底黑字）
    /// 第3行：选择按钮（选中项青底高亮）
    /// </summary>
    private void RenderPermissionBlock(StringBuilder sb, int row)
    {
        var rb = new Terminal.RenderBuffer();
        const int bg = 43; // 统一黄底

        // ---- 第1行：标题 ----
        var title = PermTitle ?? "";
        if (VW(title) > TW - 1) title = TruncateByVW(title, TW - 2) + "…";
        rb.Write(row, 0, title, fg: 30, bg: bg);
        var pad1 = TW - VW(title);
        if (pad1 > 0) rb.MoveTo(row, VW(title)).Segment(new string(' ', pad1), bg: bg);

        // ---- 第2行：内容 ----
        var content = PermContent ?? "";
        if (VW(content) > TW - 3) content = TruncateByVW(content, TW - 4) + "…";
        rb.Write(row + 1, 0, $"  {content}", fg: 30, bg: bg);
        var pad2 = TW - VW(content) - 2;
        if (pad2 > 0) rb.MoveTo(row + 1, VW(content) + 2).Segment(new string(' ', pad2), bg: bg);

        // ---- 第3行：选择按钮 ----
        var keyLabels = new[] { "[y]", "[a]", "[n]" };
        var icons = new[] { "✓", "⭐", "✗" };
        var parts = new List<(string label, string text)>();
        for (int i = 0; i < PermissionChoices.Count; i++)
        {
            var label = i < keyLabels.Length ? keyLabels[i] : $"[{i}]";
            var icon = i < icons.Length ? icons[i] : "";
            parts.Add((label, $"{icon}{PermissionChoices[i]}"));
        }
        var curX = 0;
        for (int i = 0; i < parts.Count; i++)
        {
            var (label, text) = parts[i];
            var btnText = $"{label}{text}  ";
            var btnVw = VW(btnText);
            if (curX + btnVw > TW) break;

            if (i == PermissionSelectedIdx)
                rb.Write(row + 2, curX, btnText, fg: 30, bg: 46); // 选中：青底高亮
            else
                rb.Write(row + 2, curX, btnText, fg: 30, bg: bg); // 未选中：黄底黑字
            curX += btnVw;
        }
        // 填满剩余
        var pad3 = TW - curX;
        if (pad3 > 0)
            rb.MoveTo(row + 2, curX).Segment(new string(' ', pad3), bg: bg);

        sb.Append(rb.ToString());
    }

    private void RenderInputArea(StringBuilder sb, int startRow, int vh)
    {
        var cw = TW - 2; // "> " 前缀占 2 列
        var screenLines = BuildInputScreenLines(cw);

        // 调整滚动
        var total = screenLines.Count;
        if (InputCy < 0) InputCy = 0; // safety

        var (scrCy, _) = InputHardToScreen(cw);
        if (scrCy < InputScroll) InputScroll = scrCy;
        if (scrCy >= InputScroll + vh) InputScroll = scrCy - vh + 1;
        InputScroll = Math.Clamp(InputScroll, 0, Math.Max(0, total - vh));

        var rb = new Terminal.RenderBuffer();

        // 内容行
        for (int i = 0; i < vh; i++)
        {
            int contentRow = startRow + i;
            var si = InputScroll + i;
            rb.MoveTo(contentRow, 0);

            if (si < screenLines.Count && screenLines[si].Chars > 0)
            {
                var sl = screenLines[si];
                var text = InputLines[sl.HardLine].ToString();
                var slice = text.Substring(sl.HardOffset,
                    Math.Min(sl.Chars, text.Length - sl.HardOffset));

                if (si == 0)
                {
                    // 首行：青色 > 前缀 + 白色文本 + 思考闪烁块
                    rb.Segment("> ", fg: 36);
                    rb.Segment(slice);
                    if (Running)
                    {
                        var blink = (_frameCount / 15) % 2 == 0 ? "▊" : " ";
                        rb.Segment(blink, fg: 36);
                    }
                }
                else
                {
                    // 续行：两个空格缩进（无 > 前缀）
                    rb.Segment("  ");
                    rb.Segment(slice);
                }
                var pad = cw - sl.VW;
                if (pad > 0) rb.Raw(new string(' ', pad));
            }
            else if (si == 0)
            {
                // 空输入时显示 > 提示符 + 思考闪烁块
                rb.Segment("> ", fg: 36);
                if (Running)
                {
                    var blink = (_frameCount / 15) % 2 == 0 ? "▊ 思考中…" : "  思考中…";
                    rb.Segment(blink, fg: 36);
                }
                else rb.Raw(new string(' ', cw));
            }
            else
            {
                rb.Raw(new string(' ', TW));
            }
            rb.ClearToEndOfLine();
        }

        sb.Append(rb.ToString());
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

    /// <summary>Ctrl+Left: 跳到上一个单词开头</summary>
    public void InputWordLeft()
    {
        var text = InputLines[InputCy].ToString();
        if (InputCx == 0)
        {
            if (InputCy > 0) { InputCy--; InputCx = InputLines[InputCy].Length; }
            return;
        }
        // 跳过空白/标点
        while (InputCx > 0 && !IsWordChar(text[InputCx - 1])) InputCx--;
        // 跳过单词字符
        while (InputCx > 0 && IsWordChar(text[InputCx - 1])) InputCx--;
    }

    /// <summary>Ctrl+Right: 跳到下一个单词开头</summary>
    public void InputWordRight()
    {
        var text = InputLines[InputCy].ToString();
        if (InputCx >= text.Length)
        {
            if (InputCy < InputLines.Count - 1) { InputCy++; InputCx = 0; }
            return;
        }
        // 跳过单词字符
        while (InputCx < text.Length && IsWordChar(text[InputCx])) InputCx++;
        // 跳过空白/标点
        while (InputCx < text.Length && !IsWordChar(text[InputCx])) InputCx++;
    }

    /// <summary>Ctrl+Backspace: 删除左边一个单词</summary>
    public void InputDeleteWordLeft()
    {
        var oldCx = InputCx;
        InputWordLeft();
        if (InputCx < oldCx && InputCy == (oldCx > 0 ? InputCy : InputCy))
        {
            var count = oldCx - InputCx;
            InputLines[InputCy].Remove(InputCx, count);
        }
    }

    /// <summary>Ctrl+Delete: 删除右边一个单词</summary>
    public void InputDeleteWordRight()
    {
        var text = InputLines[InputCy].ToString();
        if (InputCx >= text.Length)
        {
            if (InputCy < InputLines.Count - 1)
            {
                // 合并下一行到当前行
                InputLines[InputCy].Append(InputLines[InputCy + 1]);
                InputLines.RemoveAt(InputCy + 1);
            }
            return;
        }
        var oldCx = InputCx;
        InputWordRight();
        if (InputCx > oldCx)
        {
            InputLines[InputCy].Remove(oldCx, InputCx - oldCx);
            InputCx = oldCx;
        }
    }

    /// <summary>判断字符是否为单词字符 (字母/数字/下划线/中文)</summary>
    private static bool IsWordChar(char c) =>
        char.IsLetterOrDigit(c) || c == '_' || c > 127;

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
        SuggestH = Math.Min(Suggestions.Count, 8) + 2; // 最多8项+边框
        SuggestIdx = 0;
        SuggestScroll = 0;
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
        "/repomap", "/pr [标题]", "/edit [文件]", "/settings",
        "/test", "/test perm", "/test toast", "/test menu", "/test dialog",
        "/test panel", "/test status", "/test suggest", "/test chat",
        "/test editor", "/test theme", "/test all",
        "quit",
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

    /// <summary>按视觉宽度截取 text 前缀，返回 (string 索引, 视觉宽度)</summary>
    private static (int chars, int vw) MeasureSlice(string text, int maxVW)
    {
        int byteIdx = 0, vw = 0;
        int chars = 0; // string 索引（char 单位）
        while (byteIdx < text.Length)
        {
            Rune.DecodeFromUtf16(text.AsSpan(byteIdx), out var rune, out var consumed);
            var w = TuiHelper.RuneWidth(rune);
            if (vw + w > maxVW) break;
            vw += w;
            chars += consumed; // consumed = 1 (BMP) 或 2 (supplementary)
            byteIdx += consumed;
        }
        return (chars, vw);
    }

    private static int VW(string s) => TuiHelper.DisplayWidth(s);

    private static string TruncateByVW(string text, int maxVW) => TuiHelper.TruncateByWidth(text, maxVW);
}
