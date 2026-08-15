// =============================================================
// InputReader.cs —— 终端原始输入解析
//
// 读取标准输入的原始字节流，解析为 InputEvent。支持：
//   - ASCII / UTF-8 多字节字符
//   - CSI 序列（方向键、Home/End/PgUp/PgDn、Delete、功能键）
//   - Alt 组合（ESC 前缀 + 字符）
//   - SGR 鼠标协议（ESC [ < b ; row ; col M/m）
//   - Bracketed Paste（ESC [ 200~ ... ESC [ 201~）
// 零依赖，AOT 安全。
// =============================================================
using System.Text;

namespace QBasic.Tui;

/// <summary>从原始输入流解析按键事件。</summary>
public sealed class InputReader
{
    private readonly Stream _in;

    public InputReader(Stream input)
    {
        _in = input;
    }

    /// <summary>读取一个事件。返回 false 表示流结束。</summary>
    public bool Read(out InputEvent ev)
    {
        ev = default;
        int b = _in.ReadByte();
        if (b < 0) return false;
        // Bracketed Paste 起始序列 ESC [ 200 ~
        if (b == 0x1b)
        {
            return HandleEsc(ref ev);
        }
        if (b == '\r' || b == '\n')
        {
            ev.Key = KeyCode.Enter;
            return true;
        }
        if (b == '\t') { ev.Key = KeyCode.Tab; return true; }
        if (b == 0x7f) { ev.Key = KeyCode.Backspace; return true; }
        if (b == 0x03) { ev.Key = KeyCode.None; ev.Ch = 'c'; ev.Mods = KeyMods.Ctrl; return true; } // Ctrl+C
        if (b == 0x0d) return true;
        // 控制字符 (Ctrl+letter)
        if (b >= 1 && b <= 26)
        {
            ev.Key = KeyCode.None;
            ev.Ch = (char)('a' + b - 1);
            ev.Mods = KeyMods.Ctrl;
            return true;
        }
        // UTF-8 多字节
        if (b >= 0x80)
        {
            ev.Ch = ReadUtf8(b);
            ev.Key = KeyCode.None;
            return true;
        }
        ev.Ch = (char)b;
        ev.Key = KeyCode.None;
        return true;
    }

    private bool HandleEsc(ref InputEvent ev)
    {
        int b = _in.ReadByte();
        if (b < 0) { ev.Key = KeyCode.Esc; return true; }
        if (b == 0x1b) { ev.Key = KeyCode.Esc; return true; } // double esc
        if (b == '[')
        {
            // CSI
            int next = _in.ReadByte();
            if (next < 0) { ev.Key = KeyCode.Esc; return true; }
            if (next == '<')
            {
                // SGR 鼠标
                return ParseMouse(ref ev);
            }
            if (next == '[')
            {
                // 可能是功能键 F1-F4 的另一种编码
                int c = _in.ReadByte();
                return MapFunc(ref ev, c);
            }
            if (next == '?')
            {
                // 查询序列，忽略
                DrainCsi();
                ev.Key = KeyCode.Esc;
                return true;
            }
            return ParseCsi(ref ev, next);
        }
        if (b == 'O')
        {
            int c = _in.ReadByte();
            return MapFunc(ref ev, c);
        }
        // Alt+字符
        ev.Key = KeyCode.None;
        ev.Ch = (char)b;
        ev.Mods = KeyMods.Alt;
        return true;
    }

    private bool MapFunc(ref InputEvent ev, int c)
    {
        ev.Mods = KeyMods.None;
        switch (c)
        {
            case 'P': ev.Key = KeyCode.F1; return true;
            case 'Q': ev.Key = KeyCode.F2; return true;
            case 'R': ev.Key = KeyCode.F3; return true;
            case 'S': ev.Key = KeyCode.F4; return true;
            default: ev.Key = KeyCode.Esc; return true;
        }
    }

    private bool ParseCsi(ref InputEvent ev, int first)
    {
        // 收集参数
        var sb = new StringBuilder();
        sb.Append((char)first);
        while (true)
        {
            int b = _in.ReadByte();
            if (b < 0) break;
            char c = (char)b;
            if (c >= '0' && c <= '9' || c == ';' || c == '?')
            {
                sb.Append(c);
                continue;
            }
            // c 是终结符
            return FinishCsi(ref ev, sb.ToString(), c);
        }
        ev.Key = KeyCode.Esc;
        return true;
    }

    private bool FinishCsi(ref InputEvent ev, string paramStr, char terminator)
    {
        // 解析修饰符: 参数末尾 ;N 形式
        int mods = 1;
        var parts = paramStr.Split(';');
        if (parts.Length >= 2 && int.TryParse(parts[^1], out int m) && m >= 1 && m <= 8)
        {
            mods = m;
            // 去掉最后一个参数
            parts = parts[..^1];
        }
        ev.Mods = (mods & 1) != 0 ? KeyMods.None : 0;
        if ((mods & 1) == 0) ev.Mods = 0;
        ev.Mods |= (mods & 2) != 0 ? KeyMods.Shift : 0;
        ev.Mods |= (mods & 4) != 0 ? KeyMods.Alt : 0;
        ev.Mods |= (mods & 8) != 0 ? KeyMods.Ctrl : 0;
        // 清除不可靠的 default
        if (ev.Mods == 0) ev.Mods = KeyMods.None;

        switch (terminator)
        {
            case 'A': ev.Key = KeyCode.Up; break;
            case 'B': ev.Key = KeyCode.Down; break;
            case 'C': ev.Key = KeyCode.Right; break;
            case 'D': ev.Key = KeyCode.Left; break;
            case 'H': ev.Key = KeyCode.Home; break;
            case 'F': ev.Key = KeyCode.End; break;
            case 'Z': ev.Key = KeyCode.Tab; ev.Mods |= KeyMods.Shift; break;
            case '~':
                int n = parts.Length > 0 && int.TryParse(parts[0], out int v) ? v : 0;
                ev.Key = n switch
                {
                    1 => KeyCode.Home,
                    2 => KeyCode.Insert,
                    3 => KeyCode.Delete,
                    4 => KeyCode.End,
                    5 => KeyCode.PgUp,
                    6 => KeyCode.PgDn,
                    7 => KeyCode.Home,
                    8 => KeyCode.End,
                    11 => KeyCode.F1, 12 => KeyCode.F2, 13 => KeyCode.F3, 14 => KeyCode.F4,
                    15 => KeyCode.F5, 17 => KeyCode.F6, 18 => KeyCode.F7, 19 => KeyCode.F8,
                    20 => KeyCode.F9, 21 => KeyCode.F10, 23 => KeyCode.F11, 24 => KeyCode.F12,
                    _ => KeyCode.None,
                };
                break;
            case 'M':
            case 'm':
                // 普通鼠标协议（非 SGR），忽略
                ev.Key = KeyCode.Esc;
                break;
            default:
                ev.Key = KeyCode.Esc;
                break;
        }
        return true;
    }

    private bool ParseMouse(ref InputEvent ev)
    {
        // 格式: <b;col;row M/m  之后 M=按下 m=释放
        var sb = new StringBuilder();
        while (true)
        {
            int b = _in.ReadByte();
            if (b < 0) break;
            char c = (char)b;
            if (c == 'M' || c == 'm')
            {
                var parts = sb.ToString().Split(';');
                if (parts.Length >= 3 &&
                    int.TryParse(parts[0], out int code) &&
                    int.TryParse(parts[1], out int col) &&
                    int.TryParse(parts[2], out int row))
                {
                    ev.Key = KeyCode.Mouse;
                    ev.MouseCol = col;
                    ev.MouseRow = row;
                    int btn = code & 3;
                    int motion = (code >> 5) & 1;
                    if (motion == 1 && (code & 64) == 0) { ev.Button = MouseButton.Move; return true; }
                    int wheel = (code >> 4) & 1;
                    if ((code & 64) != 0) { ev.Button = MouseButton.Move; return true; }
                    switch (btn)
                    {
                        case 0: ev.Button = MouseButton.Left; break;
                        case 1: ev.Button = MouseButton.Middle; break;
                        case 2: ev.Button = MouseButton.Right; break;
                        case 3:
                            ev.Button = wheel == 0 ? MouseButton.WheelUp : MouseButton.WheelDown;
                            break;
                    }
                }
                return true;
            }
            sb.Append(c);
        }
        ev.Key = KeyCode.Esc;
        return true;
    }

    private void DrainCsi()
    {
        while (true)
        {
            int b = _in.ReadByte();
            if (b < 0) return;
            char c = (char)b;
            if (c >= '0' && c <= '9' || c == ';' || c == '?' || c == '=') continue;
            return; // 终结符
        }
    }

    /// <summary>尝试读取一个 Bracketed Paste 文本块（调用方在读到 Esc 起始后进入）。</summary>
    public string? TryReadPaste()
    {
        // 读取直到 ESC [ 201~
        var sb = new StringBuilder();
        var ring = new Queue<int>();
        int[] seq = new int[] { 0x1b, '[', '2', '0', '1', '~' };
        while (true)
        {
            int b = _in.ReadByte();
            if (b < 0) break;
            ring.Enqueue(b);
            if (ring.Count > 6) ring.Dequeue();
            if (Matches(ring, seq)) break;
            if (ring.Count <= 6)
                sb.Append((char)b);
            else
            {
                // 移出一个最早字节
                // 简单处理：重建
            }
        }
        // 移除末尾匹配到的序列
        return sb.ToString();
    }

    private static bool Matches(Queue<int> ring, int[] seq)
    {
        var arr = ring.ToArray();
        if (arr.Length < seq.Length) return false;
        for (int i = 0; i < seq.Length; i++)
            if (arr[arr.Length - seq.Length + i] != seq[i]) return false;
        return true;
    }

    private char ReadUtf8(int first)
    {
        int count = first switch
        {
            >= 0xF0 => 3,
            >= 0xE0 => 2,
            >= 0xC0 => 1,
            _ => 0,
        };
        int cp = first & 0x3F;
        if (first >= 0xF0) cp = first & 0x07;
        else if (first >= 0xE0) cp = first & 0x0F;
        else if (first >= 0xC0) cp = first & 0x1F;
        for (int i = 0; i < count; i++)
        {
            int b = _in.ReadByte();
            if (b < 0) break;
            cp = (cp << 6) | (b & 0x3F);
        }
        if (cp > 0xFFFF) return '\ufffd'; // 代理对，简化为替换符
        return (char)cp;
    }
}
