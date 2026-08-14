using System.Text;
using WayCoder.Terminal;
using WayCoder.UI;
using WayCoder.UI.TuiControls;
using WayCoder.UI.TuiScreens;

namespace WayCoder;

/// <summary>
/// 按键脚本回放 —— 用脚本驱动 TUI（按键 / 打字 / 延时 / 打开对话框），并在任意节点抓取渲染帧，
/// 便于肉眼核对界面排版与键盘交互是否符合预期。
///
/// 运行：waycoder --keypad 脚本.txt
///
/// 脚本格式（每行一条命令，`#` 或 `//` 开头为注释，空行忽略）：
///   KEY:F1              按键（支持修饰键：CTRL+P / SHIFT+TAB / CTRL+SHIFT+F1）
///   KEY:Up / KEY:Down   方向键（Up/Down/Left/Right/Home/End/PgUp/PgDn/Tab/Space/Enter/Escape/Backspace/Delete）
///   TEXT:hello world    逐字符键入（用于输入框；换行用单独 KEY:Enter）
///   DELAY:1000          延时毫秒（等待动画/异步更新）
///   SNAP:标签           抓取当前帧并输出纯文本（省略标签则为 SNAP）
///   DIALOG:confirm      打开一个演示对话框（permission/input/list/confirm/multi/buttons/...）
///   FOCUS              转储当前焦点状态（焦点窗口 / RootView 类型 / 焦点控件 / 可聚焦控件列表）
///   MSG:角色:内容       注入一条聊天消息（user/assistant/agent/system/tool/error/banner；`\n` 换行）
///   FILL:数量           批量注入编号消息（交替 user/assistant/system，用于撑满列表测滚动）
///
/// 说明：
///   - 全程重定向 Console.Out 隔离 TUI 的 ANSI 转义。
///   - 帧内容用一个持久化 FrameBuffer 累积增量渲染（与真实终端逐帧覆盖一致），
///     因此 Tab 切焦点 / 增量重绘等只输出 delta 的渲染也能得到完整画面。
///   - 键注入走 TuiManager.OnKey → ChatScreen.OnKey → 模态窗口 → 控件 的完整分发链，
///     能真实验证 Tab 切焦点 / Enter 点击 / Space 激活 / Esc 关闭 等键盘契约。
///   - 全屏 ANSI 对话框（ModelPicker / SessionPicker 等）自读 Console.ReadKey，无法被脚本驱动，
///     仅 TuiWindow 系对话框（TuiDialog.Confirm/Input/Select/MultiSelect 等）可脚本化。
/// </summary>
public static class Keypad
{
    public static int Run(string scriptPath)
    {
        if (!File.Exists(scriptPath))
        {
            Console.Error.WriteLine($"✘ 按键脚本不存在: {scriptPath}");
            return 1;
        }

        string[] lines;
        try { lines = File.ReadAllLines(scriptPath); }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"✘ 读取脚本失败: {ex.Message}");
            return 1;
        }

        // 隔离 TUI 的 ANSI 输出（备用屏转义 + 每帧渲染），帧内容改从持久化 FrameBuffer 读取
        var orig = Console.Out;
        Console.SetOut(TextWriter.Null);
        try { Console.OutputEncoding = Encoding.UTF8; } catch { }

        var mgr = TuiManager.Instance;
        try
        {
            mgr.Enter();
            mgr.RefreshTheme();

            var screen = new ChatScreen { ChatDisplayStyle = Config.Instance.ChatDisplayStyle };
            mgr.PushScreen(screen);
            screen.ChatMessages.Add(new ChatMsg { Role = "system", Content = $"⌨ Keypad 回放: {Path.GetFileName(scriptPath)}" });

            int rows = Math.Clamp(Tty.Rows, 12, 120);
            int cols = Math.Clamp(Tty.Cols, 40, 240);
            var frame = new FrameBuffer(rows, cols);

            mgr.Render();
            frame.Apply(mgr.LastCleanFrame);

            int step = 0;
            foreach (var raw in lines)
            {
                var (op, value) = Parse(raw);
                step++;

                switch (op)
                {
                    case "":   // 空行
                    case "#":  // 注释
                        break;

                    case "KEY":
                        if (TryParseKey(value, out var k))
                            mgr.OnKey(k);
                        else
                            Emit(orig, $"# (第 {step} 行) 无法识别的按键: {value}");
                        break;

                    case "TEXT":
                        foreach (var c in value)
                            mgr.OnKey(CharToKey(c));
                        break;

                    case "DELAY":
                        if (int.TryParse(value, out var ms) && ms > 0)
                            Thread.Sleep(Math.Min(ms, 60000));
                        break;

                    case "DIALOG":
                        OpenDialog(screen, value);
                        break;

                    case "MSG":
                        AddMsg(screen, value);
                        break;

                    case "FILL":
                        if (int.TryParse(value, out var n) && n > 0)
                            FillChat(screen, Math.Min(n, 500));
                        else
                            Emit(orig, $"# (第 {step} 行) FILL 需要正整数: {value}");
                        break;

                    case "FOCUS":
                        DumpFocus(screen, orig);
                        break;

                    case "SNAP":
                        EmitFrame(orig, value, frame.Dump());
                        break;

                    case "SNAPCOLOR":
                        EmitFrame(orig, value, frame.DumpAnsi());
                        break;

                    default:
                        Emit(orig, $"# (第 {step} 行) 未知命令: {op}");
                        break;
                }

                // 每条命令后统一渲染 + 累积到持久化帧缓冲（与真实终端行为一致）
                mgr.Render();
                frame.Apply(mgr.LastCleanFrame);
            }
        }
        finally
        {
            try { mgr.Exit(); } catch { }
            Console.SetOut(orig);
        }

        return 0;
    }

    // ── 脚本解析 ──

    /// <summary>把一行脚本拆成 (命令, 值)，命令转大写、值保留原文。注释行返回 Op="# "。</summary>
    static (string Op, string Value) Parse(string line)
    {
        line = line.Trim();
        if (line.Length == 0) return ("", "");
        if (line.StartsWith('#') || line.StartsWith("//")) return ("#", "");

        int idx = line.IndexOf(':');
        if (idx < 0) return (line.ToUpperInvariant(), "");
        return (line[..idx].Trim().ToUpperInvariant(), line[(idx + 1)..].Trim());
    }

    // ── 按键解析 ──

    /// <summary>解析按键描述（如 F1 / Up / CTRL+P / SHIFT+TAB）为 ConsoleKeyInfo。</summary>
    static bool TryParseKey(string spec, out ConsoleKeyInfo key)
    {
        key = default;
        spec = spec.Trim();
        if (spec.Length == 0) return false;

        bool ctrl = false, shift = false, alt = false;
        var parts = spec.Split('+');
        var keyName = parts[^1].Trim();

        for (int i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i].Trim().ToUpperInvariant())
            {
                case "CTRL": case "CONTROL": ctrl = true; break;
                case "SHIFT": shift = true; break;
                case "ALT": alt = true; break;
                default: return false; // 未知修饰键
            }
        }

        var consoleKey = ParseKeyName(keyName);
        if (consoleKey == null) return false;

        key = new ConsoleKeyInfo(DetermineKeyChar(consoleKey.Value, shift, ctrl),
            consoleKey.Value, shift, alt, ctrl);
        return true;
    }

    static ConsoleKey? ParseKeyName(string name)
    {
        name = name.Trim();
        if (name.Length == 1)
        {
            char c = name[0];
            if (c >= 'a' && c <= 'z') return (ConsoleKey)(ConsoleKey.A + (c - 'a'));
            if (c >= 'A' && c <= 'Z') return (ConsoleKey)(ConsoleKey.A + (c - 'A'));
            if (c >= '0' && c <= '9') return (ConsoleKey)(ConsoleKey.D0 + (c - '0'));
        }

        switch (name.ToUpperInvariant())
        {
            case "ENTER": case "RETURN": return ConsoleKey.Enter;
            case "ESC": case "ESCAPE": return ConsoleKey.Escape;
            case "UP": case "UPARROW": return ConsoleKey.UpArrow;
            case "DOWN": case "DOWNARROW": return ConsoleKey.DownArrow;
            case "LEFT": case "LEFTARROW": return ConsoleKey.LeftArrow;
            case "RIGHT": case "RIGHTARROW": return ConsoleKey.RightArrow;
            case "TAB": return ConsoleKey.Tab;
            case "SPACE": case "SPACEBAR": return ConsoleKey.Spacebar;
            case "PGUP": case "PAGEUP": return ConsoleKey.PageUp;
            case "PGDN": case "PAGEDOWN": return ConsoleKey.PageDown;
            case "HOME": return ConsoleKey.Home;
            case "END": return ConsoleKey.End;
            case "BACKSPACE": case "BACK": return ConsoleKey.Backspace;
            case "DEL": case "DELETE": return ConsoleKey.Delete;
            case "INS": case "INSERT": return ConsoleKey.Insert;
        }

        if (name.StartsWith('F')
            && int.TryParse(name[1..], out var fn) && fn >= 1 && fn <= 24)
            return (ConsoleKey)(ConsoleKey.F1 + fn - 1);

        return null;
    }

    static char DetermineKeyChar(ConsoleKey key, bool shift, bool ctrl)
    {
        if (ctrl)
        {
            // Ctrl+字母 → 控制字符（与终端一致）
            if (key >= ConsoleKey.A && key <= ConsoleKey.Z)
                return (char)(key - ConsoleKey.A + 1);
            return '\0';
        }
        if (key >= ConsoleKey.A && key <= ConsoleKey.Z)
            return shift ? (char)(key - ConsoleKey.A + 'A') : (char)(key - ConsoleKey.A + 'a');
        if (key >= ConsoleKey.D0 && key <= ConsoleKey.D9)
        {
            if (!shift) return (char)(key - ConsoleKey.D0 + '0');
            return ")!@#$%^&*("[key - ConsoleKey.D0];
        }
        if (key == ConsoleKey.Spacebar) return ' ';
        return '\0';
    }

    /// <summary>单个可打印字符 → ConsoleKeyInfo。文本插入走 TuiEditBase 的 keyChar 分支。</summary>
    static ConsoleKeyInfo CharToKey(char c)
    {
        return c switch
        {
            '\n' => new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false),
            '\t' => new ConsoleKeyInfo('\0', ConsoleKey.Tab, false, false, false),
            _ => new ConsoleKeyInfo(c, (ConsoleKey)0, false, false, false),
        };
    }

    // ── 演示对话框 ──

    static void OpenDialog(ChatScreen screen, string name)
    {
        switch (name.ToLowerInvariant())
        {
            case "permission": TuiDemo.ShowPermissionDemo(screen); return;
            case "input": TuiDemo.ShowInputDemo(screen); return;
            case "list": case "select": TuiDemo.ShowListDemo(screen); return;
            case "confirm": TuiDemo.ShowConfirmDemo(screen); return;
            case "shortmenu": case "short": TuiDemo.ShowShortMenuDemo(screen); return;
            case "longmenu": case "long": TuiDemo.ShowLongMenuDemo(screen); return;
            case "contextmenu": case "context": TuiDemo.ShowContextMenuDemo(screen); return;
            case "multi": case "multiselect": TuiDemo.ShowMultiSelectDemo(screen); return;
            case "buttons": case "buttongroup": TuiDemo.ShowButtonGroupDemo(screen); return;
            case "controls": TuiDemo.ShowControlsDemo(screen); return;
            case "panel": TuiDemo.ShowPanelDemo(screen); return;
            case "tree": TuiDemo.ShowTreeDemo(screen); return;
        }
    }

    // ── 聊天消息注入 ──

    /// <summary>注入一条聊天消息：MSG:角色:内容。`\n` 转义为换行，角色决定渲染方式。</summary>
    static void AddMsg(ChatScreen screen, string spec)
    {
        int idx = spec.IndexOf(':');
        string role = idx < 0 ? "system" : spec[..idx].Trim().ToLowerInvariant();
        string content = (idx < 0 ? spec : spec[(idx + 1)..]).Trim().Replace("\\n", "\n");

        switch (role)
        {
            case "user": screen.AddMessage(content, "user"); return;
            case "assistant": case "agent": screen.AddMessage(content, "assistant"); return;
            case "system": screen.AddMessage(content, "system"); return;
            case "tool": screen.AddMessage(content, "tool", indent: 1); return;
            case "error": screen.AddMessage("❌ " + content, "system"); return;
            case "banner": screen.AddMessage(content, "banner", centered: true); return;
            default: screen.AddMessage(content, "system"); return;
        }
    }

    /// <summary>批量注入编号消息（交替 user/assistant/system），用于撑满聊天列表测滚动/翻页。</summary>
    static void FillChat(ChatScreen screen, int n)
    {
        for (int i = 1; i <= n; i++)
        {
            int m = i % 3;
            if (m == 0)
                screen.AddMessage($"系统 #{i}: 这是一条较长的系统提示消息，用于填充聊天列表以验证滚动与翻页的显示效果。", "system");
            else if (m == 2)
                screen.AddMessage($"助手回复 #{i}\n- 要点 A{i}\n- 要点 B{i}\n\n`代码片段 {i}` 已就绪。", "assistant");
            else
                screen.AddMessage($"用户提问 #{i}: 请帮我实现功能 {i}，并说明设计思路。", "user");
        }
    }

    // ── 焦点转储 ──

    /// <summary>输出当前焦点状态：焦点窗口、RootView 类型、焦点控件、可聚焦控件列表。</summary>
    static void DumpFocus(ChatScreen screen, TextWriter w)
    {
        var win = screen.FocusedWindow;
        if (win == null)
        {
            w.WriteLine("[FOCUS] 无焦点窗口");
            return;
        }
        w.WriteLine($"[FOCUS] 窗口: \"{win.Title}\" (modal={win.Modal})");
        var rv = win.RootView;
        w.WriteLine($"[FOCUS] RootView: {(rv == null ? "<null>" : rv.GetType().Name)} (Focused={rv?.Focused})");
        var fc = win.FocusedControl;
        w.WriteLine($"[FOCUS] 焦点控件: {(fc == null ? "<无>" : fc.GetType().Name)}");
        var sel = DescribeSelection(fc);
        if (sel.Length > 0)
            w.WriteLine($"[FOCUS]   选中项: {sel}");
        // 菜单（MenuView 本身就是 RootView 且直接处理按键，FocusedControl 可能为空）
        var rvSel = DescribeSelection(rv);
        if (rvSel.Length > 0 && rv != fc)
            w.WriteLine($"[FOCUS]   RootView 选中项: {rvSel}");
        if (rv != null)
        {
            var list = rv.GetAllFocusable();
            w.WriteLine($"[FOCUS] 可聚焦控件 ({list.Count}):");
            for (int i = 0; i < list.Count; i++)
                w.WriteLine($"  [{i}] {list[i].GetType().Name}{(list[i].Focused ? "  ◀ 已聚焦" : "")}");
        }
    }

    /// <summary>读取控件的「选中索引/选中文本」，用于验证上下键是否真的移动了选中项（AOT 无反射，用类型匹配）。</summary>
    static string DescribeSelection(object? c)
    {
        switch (c)
        {
            case TuiList l:
                return l.MultiSelect
                    ? $"idx={l.SelectedIndex}/{l.Items.Count} 勾选=[{string.Join(",", l.CheckedIndices.Order())}]"
                    : $"idx={l.SelectedIndex}/{l.Items.Count}";
            case TuiListView lv:
                return $"idx={lv.SelectedIndex}/{lv.Children.Count}";
            case TuiRadioGroup rg:
                return $"idx={rg.SelectedIndex}/{rg.Options.Count}";
            case TuiComboBox cb:
                return $"idx={cb.SelectedIndex}/{cb.Options.Count} 展开={cb.IsExpanded}";
            case TuiTreeView tv:
                return tv.SelectedNode == null ? "idx=<无>" : $"「{tv.SelectedNode.Text}」";
            case TuiMenu.MenuView mv:
                return $"idx={mv.SelectedIndex}/{mv.ItemCount}";
            default:
                return "";
        }
    }

    // ── 输出 ──

    static void Emit(TextWriter w, string text) => w.WriteLine(text);

    static void EmitFrame(TextWriter w, string label, List<string> lines)
    {
        var tag = string.IsNullOrWhiteSpace(label) ? "SNAP" : "SNAP " + label;
        w.WriteLine();
        w.WriteLine($"===== [{tag}] =====");
        foreach (var l in lines) w.WriteLine(l);
        w.WriteLine($"===== [/{tag}] =====");
    }

    // ── 持久化帧缓冲：累积增量渲染，等价于真实终端的屏幕缓冲区 ──

    /// <summary>
    /// 一个 rows×cols 的文本网格，逐帧 Apply 增量 ANSI（CursorPos/SGR/ClearScreen）后保持累积状态，
    /// 避免增量渲染只输出 delta 时抓不到完整画面。SGR 颜色被剥离，仅保留字符与位置。
    /// </summary>
    sealed class FrameBuffer
    {
        readonly int _rows, _cols;
        readonly string[][] _cell;
        readonly bool[][] _cont;
        readonly int[][] _fg, _bg;   // 每个格子的前景/背景 ANSI 色码（0=默认）
        int _curR, _curC;
        int _curFg, _curBg;          // 当前 SGR 状态（用于颜色采集）

        public FrameBuffer(int rows, int cols)
        {
            _rows = rows; _cols = cols;
            _cell = new string[rows][];
            _cont = new bool[rows][];
            _fg = new int[rows][];
            _bg = new int[rows][];
            for (int r = 0; r < rows; r++)
            {
                _cell[r] = new string[cols];
                _cont[r] = new bool[cols];
                _fg[r] = new int[cols];
                _bg[r] = new int[cols];
                for (int c = 0; c < cols; c++) _cell[r][c] = " ";
            }
        }

        public void Apply(string ansi)
        {
            int i = 0, len = ansi.Length;
            while (i < len)
            {
                char ch = ansi[i];
                if (ch == '\x1b' && i + 1 < len && ansi[i + 1] == '[')
                {
                    int j = i + 2;
                    while (j < len && !(ansi[j] >= '@' && ansi[j] <= '~')) j++;
                    if (j >= len) break;
                    char final = ansi[j];
                    string param = ansi.Substring(i + 2, j - (i + 2));
                    i = j + 1;

                    if (final == 'H' || final == 'f') // CUP / HVP：行;列，1-based
                    {
                        int row = 1, col = 1;
                        var p = param.Split(';');
                        if (p.Length >= 1 && int.TryParse(p[0], out var rr)) row = rr;
                        if (p.Length >= 2 && int.TryParse(p[1], out var cc)) col = cc;
                        _curR = Math.Clamp(row - 1, 0, _rows - 1);
                        _curC = Math.Clamp(col - 1, 0, _cols - 1);
                    }
                    else if (final == 'J') // Erase Display：参数 2 = 清除整个屏幕
                    {
                        if (param.TrimStart('?') == "2") ClearAll();
                    }
                    else if (final == 'm') // SGR：记录当前前景/背景色，用于颜色采集
                    {
                        ApplySgr(param);
                    }
                    continue;
                }

                if (ch == '\r') { _curC = 0; i++; continue; }
                if (ch == '\n') { _curR++; _curC = 0; i++; continue; }
                if (ch == '\t') { _curC += 4 - (_curC % 4); i++; continue; }

                var rune = Rune.GetRuneAt(ansi, i);
                int w = AnsiString.CharWidth(rune);
                string s = rune.ToString();
                i += rune.Utf16SequenceLength;

                if (w == 0) continue; // 零宽字符（组合标记/变体选择器）不占列

                if (_curR >= 0 && _curR < _rows && _curC >= 0 && _curC < _cols)
                {
                    _cell[_curR][_curC] = s;
                    _fg[_curR][_curC] = _curFg;
                    _bg[_curR][_curC] = _curBg;
                    if (w == 2 && _curC + 1 < _cols) _cont[_curR][_curC + 1] = true;
                }
                _curC += w;
            }
        }

        void ClearAll()
        {
            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _cols; c++)
                {
                    _cell[r][c] = " "; _cont[r][c] = false;
                    _fg[r][c] = 0; _bg[r][c] = 0;
                }
            _curR = 0; _curC = 0;
        }

        /// <summary>解析 SGR 参数串（如 "37;47" / "0" / "38;5;N" / "38;2;R;G;B"），更新当前 fg/bg。</summary>
        void ApplySgr(string param)
        {
            if (string.IsNullOrEmpty(param)) param = "0";
            var parts = param.Split(';');
            for (int k = 0; k < parts.Length; k++)
            {
                if (!int.TryParse(parts[k], out var code)) continue;
                if (code == 0) { _curFg = 0; _curBg = 0; }
                else if (code >= 30 && code <= 37) _curFg = code;
                else if (code >= 90 && code <= 97) _curFg = code;
                else if (code >= 40 && code <= 47) _curBg = code;
                else if (code >= 100 && code <= 107) _curBg = code;
                else if (code == 39) _curFg = 0;
                else if (code == 49) _curBg = 0;
                else if (code == 38 || code == 48)
                {
                    // 38;5;N / 48;5;N / 38;2;R;G;B —— 跳过后续参数，不做精确记录
                    if (k + 1 < parts.Length && parts[k + 1] == "5") k += 2;
                    else if (k + 1 < parts.Length && parts[k + 1] == "2") k += 4;
                }
                // 其余（1 粗体 / 2 淡化 / 22 取消粗体 等）不参与 fg/bg 采集
            }
        }

        public List<string> Dump()
        {
            var lines = new List<string>();
            for (int r = 0; r < _rows; r++)
            {
                var sb = new StringBuilder();
                for (int c = 0; c < _cols; c++)
                {
                    if (_cont[r][c]) continue; // 宽字符延续格
                    sb.Append(_cell[r][c]);
                }
                lines.Add(sb.ToString().TrimEnd());
            }
            while (lines.Count > 0 && lines[^1].Length == 0) lines.RemoveAt(lines.Count - 1);
            return lines;
        }

        /// <summary>
        /// 带颜色的帧转储：按行重新合成最小化 SGR 序列，真实终端可直接渲染出颜色。
        /// 用于「肉眼看到颜色」——排查白底白字/黄底黄字等前景背景同色问题。
        /// </summary>
        public List<string> DumpAnsi()
        {
            var lines = new List<string>();
            for (int r = 0; r < _rows; r++)
            {
                var sb = new StringBuilder();
                int lastFg = -1, lastBg = -1;
                for (int c = 0; c < _cols; c++)
                {
                    if (_cont[r][c]) continue; // 宽字符延续格
                    int fg = _fg[r][c], bg = _bg[r][c];
                    if (fg != lastFg || bg != lastBg)
                    {
                        sb.Append("\x1b[0m");
                        if (fg > 0) sb.Append($"\x1b[{fg}m");
                        if (bg > 0) sb.Append($"\x1b[{bg}m");
                        lastFg = fg; lastBg = bg;
                    }
                    sb.Append(_cell[r][c]);
                }
                lines.Add(sb.ToString().TrimEnd());
            }
            while (lines.Count > 0 && lines[^1].TrimEnd('\x1b').Length == 0) lines.RemoveAt(lines.Count - 1);
            return lines;
        }

        /// <summary>把标准 ANSI 色码映射为可读名称（供颜色图例/诊断输出）。</summary>
        static string ColorName(int code)
        {
            if (code == 0) return "默认";
            return code switch
            {
                30 => "黑字", 31 => "红字", 32 => "绿字", 33 => "黄字", 34 => "蓝字", 35 => "紫字", 36 => "青字", 37 => "白字",
                40 => "黑底", 41 => "红底", 42 => "绿底", 43 => "黄底", 44 => "蓝底", 45 => "紫底", 46 => "青底", 47 => "白底",
                90 => "亮黑字", 91 => "亮红字", 92 => "亮绿字", 93 => "亮黄字", 94 => "亮蓝字", 95 => "亮紫字", 96 => "亮青字", 97 => "亮白字",
                100 => "亮黑底", 101 => "亮红底", 102 => "亮绿底", 103 => "亮黄底", 104 => "亮蓝底", 105 => "亮紫底", 106 => "亮青底", 107 => "亮白底",
                _ => $"#{code}",
            };
        }
    }
}
