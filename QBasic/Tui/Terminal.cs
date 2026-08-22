// QBasic/Tui/Terminal.cs
// 原始终端模式切换（Unix termios / 平台自适应）、键盘与鼠标输入读取、ANSI 输出。
using System.Runtime.InteropServices;

namespace QBasic.Tui;

public static class Terminal
{
    private static Stream? _input;
    private static readonly object RawLock = new();

    public static bool IsWindows => OperatingSystem.IsWindows();

    // ---------- 原始模式 ----------
    public static void EnableRawMode()
    {
        lock (RawLock)
        {
            if (IsWindows) return;
            if (tcgetattr(0, out var termios) != 0) return;
            var raw = termios;
            raw.c_iflag &= ~(uint)(BRKINT | ICRNL | INPCK | ISTRIP | IXON);
            raw.c_oflag &= ~(uint)OPOST;
            raw.c_cflag |= CS8;
            raw.c_lflag &= ~(uint)(ECHO | ICANON | IEXTEN | ISIG);
            raw.c_cc[VMIN] = 1;
            raw.c_cc[VTIME] = 0;
            tcsetattr(0, TCSAFLUSH, ref raw);
        }
    }

    public static void DisableRawMode()
    {
        lock (RawLock)
        {
            if (IsWindows) return;
            if (tcgetattr(0, out var termios) == 0)
            {
                termios.c_lflag |= ECHO | ICANON | ISIG;
                termios.c_oflag |= OPOST;
                termios.c_iflag |= ICRNL | IXON;
                tcsetattr(0, TCSAFLUSH, ref termios);
            }
        }
    }

    // ---------- 输出 ----------
    public static void Write(string s) => Console.Write(s);
    public static void Flush() => Console.Out.Flush();

    public static void Init()
    {
        _input = Console.OpenStandardInput();
    }

    // ---------- 输入 ----------
    private static Stream Input => _input ??= Console.OpenStandardInput();

    public static KeyEvent ReadKey()
    {
        int b = ReadByte();
        if (b < 0) return KeyEvent.Of(KeyKind.None);

        if (b == 0) return KeyEvent.CtrlChar(' ');

        if (b == 0x1b)
        {
            if (TryReadByte(out int b2))
            {
                if (b2 == '[') return ReadCsi();
                if (b2 == 'O') return ReadSs3();
                if (b2 >= 32 && b2 < 127)
                    return new KeyEvent { Kind = KeyKind.Character, Character = (char)b2, Alt = true };
            }
            return KeyEvent.Of(KeyKind.Escape);
        }

        if (b < 32)
        {
            switch (b)
            {
                case 9: return KeyEvent.Of(KeyKind.Tab);
                case 10:
                case 13: return KeyEvent.Of(KeyKind.Enter);
                case 8:
                case 127: return KeyEvent.Of(KeyKind.Backspace);
                default:
                    return KeyEvent.CtrlChar((char)(b + 'a' - 1));
            }
        }

        if (b == 127) return KeyEvent.Of(KeyKind.Backspace);

        return KeyEvent.Char(DecodeUtf8(b));
    }

    private static char DecodeUtf8(int first)
    {
        int len = first >= 0xF0 ? 4 : first >= 0xE0 ? 3 : first >= 0xC0 ? 2 : 1;
        byte[] bytes = new byte[len];
        bytes[0] = (byte)first;
        for (int i = 1; i < len; i++)
        {
            int nb = ReadByte();
            bytes[i] = (byte)(nb >= 0 ? nb : 0);
        }
        return System.Text.Encoding.UTF8.GetString(bytes)[0];
    }

    private static KeyEvent ReadCsi()
    {
        var sb = new System.Text.StringBuilder();
        while (true)
        {
            if (!TryReadByte(out int b)) return KeyEvent.Of(KeyKind.None);
            if ((b >= '0' && b <= '9') || b == ';' || b == '?' || b == '<')
            {
                sb.Append((char)b);
                continue;
            }
            return ParseCsi(sb.ToString(), (char)b);
        }
    }

    private static KeyEvent ReadSs3()
    {
        if (!TryReadByte(out int b)) return KeyEvent.Of(KeyKind.None);
        return (char)b switch
        {
            'P' => KeyEvent.Of(KeyKind.F1),
            'Q' => KeyEvent.Of(KeyKind.F2),
            'R' => KeyEvent.Of(KeyKind.F3),
            'S' => KeyEvent.Of(KeyKind.F4),
            'H' => KeyEvent.Of(KeyKind.Home),
            'F' => KeyEvent.Of(KeyKind.End),
            _ => KeyEvent.Of(KeyKind.None),
        };
    }

    private static KeyEvent ParseCsi(string args, char final)
    {
        if (args.StartsWith('<'))
        {
            string[] parts = args.TrimStart('<').Split(';');
            if (parts.Length >= 3 && int.TryParse(parts[0], out int button)
                && int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
            {
                bool release = final == 'm';
                KeyKind kind = button switch
                {
                    0 => KeyKind.MouseLeftClick,
                    64 => KeyKind.MouseWheelUp,
                    65 => KeyKind.MouseWheelDown,
                    32 => KeyKind.MouseMove,
                    35 => KeyKind.MouseMove,
                    _ => KeyKind.None,
                };
                if (release && kind == KeyKind.MouseLeftClick) return KeyEvent.Of(KeyKind.None);
                return new KeyEvent { Kind = kind, MouseX = x, MouseY = y };
            }
        }

        if (final == '~')
        {
            int code = args.Length > 0 && int.TryParse(args, out int c) ? c : 0;
            return code switch
            {
                1 or 7 => KeyEvent.Of(KeyKind.Home),
                2 => KeyEvent.Of(KeyKind.Insert),
                3 => KeyEvent.Of(KeyKind.Delete),
                4 or 8 => KeyEvent.Of(KeyKind.End),
                5 => KeyEvent.Of(KeyKind.PageUp),
                6 => KeyEvent.Of(KeyKind.PageDown),
                11 => KeyEvent.Of(KeyKind.F1),
                12 => KeyEvent.Of(KeyKind.F2),
                13 => KeyEvent.Of(KeyKind.F3),
                14 => KeyEvent.Of(KeyKind.F4),
                15 => KeyEvent.Of(KeyKind.F5),
                17 => KeyEvent.Of(KeyKind.F6),
                18 => KeyEvent.Of(KeyKind.F7),
                19 => KeyEvent.Of(KeyKind.F8),
                20 => KeyEvent.Of(KeyKind.F9),
                21 => KeyEvent.Of(KeyKind.F10),
                23 => KeyEvent.Of(KeyKind.F11),
                24 => KeyEvent.Of(KeyKind.F12),
                _ => KeyEvent.Of(KeyKind.None),
            };
        }

        string[] p = args.Split(';');
        int mod = p.Length > 1 && int.TryParse(p[1], out int m) ? m : 0;

        KeyEvent e = final switch
        {
            'A' => KeyEvent.Of(KeyKind.ArrowUp),
            'B' => KeyEvent.Of(KeyKind.ArrowDown),
            'C' => KeyEvent.Of(KeyKind.ArrowRight),
            'D' => KeyEvent.Of(KeyKind.ArrowLeft),
            'H' => KeyEvent.Of(KeyKind.Home),
            'F' => KeyEvent.Of(KeyKind.End),
            'Z' => KeyEvent.Of(KeyKind.BackTab),
            _ => KeyEvent.Of(KeyKind.None),
        };
        if (mod == 5) e = e with { Ctrl = true };
        if (mod == 3) e = e with { Alt = true };
        return e;
    }

    private static int ReadByte()
    {
        try
        {
            return Input.ReadByte();
        }
        catch
        {
            return -1;
        }
    }

    private static bool TryReadByte(out int b)
    {
        b = ReadByte();
        return b >= 0;
    }

    // ---------- termios P/Invoke (Unix) ----------
    private const int BRKINT = 0x2;
    private const int ICRNL = 0x100;
    private const int INPCK = 0x10;
    private const int ISTRIP = 0x20;
    private const int IXON = 0x400;
    private const int OPOST = 0x1;
    private const int CS8 = 0x30;
    private const int ECHO = 0x8;
    private const int ICANON = 0x100;
    private const int IEXTEN = 0x400;
    private const int ISIG = 0x80;
    private const int VMIN = 16;
    private const int VTIME = 17;
    private const int TCSAFLUSH = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct Termios
    {
        public uint c_iflag;
        public uint c_oflag;
        public uint c_cflag;
        public uint c_lflag;
        public byte c_line;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] c_cc;
        public uint c_ispeed;
        public uint c_ospeed;
    }

    [DllImport("libc", EntryPoint = "tcgetattr", SetLastError = true)]
    private static extern int tcgetattr(int fd, out Termios termios);

    [DllImport("libc", EntryPoint = "tcsetattr", SetLastError = true)]
    private static extern int tcsetattr(int fd, int action, ref Termios termios);
}
