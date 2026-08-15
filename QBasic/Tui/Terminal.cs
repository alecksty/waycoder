// =============================================================
// Terminal.cs —— 原始终端模式切换
//
// 把标准输入切到 raw 模式（禁用行缓冲与回显），以便逐键读取。
// macOS/Linux 使用 termios（通过 libc 互操作，零第三方依赖）；
// Windows 使用 Console 的底层模式标志。
// AOT 安全：仅使用 [LibraryImport] 手写互操作，无反射。
// =============================================================
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace QBasic.Tui;

/// <summary>终端模式控制器（进入/退出 raw mode）。</summary>
public static partial class Terminal
{
    private static bool _raw;
    private static bool _altEntered;
    private static bool _saved = false;

    // ---- Linux / macOS termios ----
    private const int TCSANOW = 0;
    private const int ICANON = 0x0002;
    private const int ECHO = 0x0008;
    private const int ISIG = 0x0001;
    private const int IXON = 0x0400;
    private const int ICRNL = 0x0100;
    private const int OPOST = 0x0001;
    private const int VMIN = 6;
    private const int VTIME = 5;

    private static int[] _origLflag = new int[3];
    private static int[] _origIflag = new int[3];
    private static int[] _origOflag = new int[3];
    private static bool _hasTermios;

    private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    [LibraryImport("libc", SetLastError = true)]
    private static partial int tcgetattr(int fd, int[] termios);

    [LibraryImport("libc", SetLastError = true)]
    private static partial int tcsetattr(int fd, int opt, int[] termios);

    [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int open(string path, int flags);

    [LibraryImport("libc", SetLastError = true)]
    private static partial int close(int fd);

    [LibraryImport("libc", SetLastError = true)]
    private static partial int read(int fd, byte[] buf, int count);

    private static int GetTtyFd()
    {
        if (IsWindows) return -1;
        // /dev/tty 直接访问控制终端
        int fd = open("/dev/tty", 2 /* O_RDWR */);
        return fd;
    }

    /// <summary>进入 raw 模式并写入屏幕初始化序列。</summary>
    public static void Enter()
    {
        if (_raw) return;
        if (!IsWindows && !OperatingSystem.IsMacOS() && !OperatingSystem.IsLinux())
        {
            // 仅支持类 Unix；其他平台回退
        }

        if (!IsWindows)
        {
            try
            {
                int fd = GetTtyFd();
                if (fd >= 0)
                {
                    var t = new int[32];
                    if (tcgetattr(fd, t) == 0)
                    {
                        Array.Copy(t, _origLflag, 3);
                        // lflag
                        t[0] &= ~(ICANON | ECHO | ISIG);
                        // iflag
                        t[1] &= ~(IXON | ICRNL);
                        // oflag
                        t[2] &= ~OPOST;
                        t[VMIN] = 1;
                        t[VTIME] = 0;
                        tcsetattr(fd, TCSANOW, t);
                        _hasTermios = true;
                    }
                    close(fd);
                }
            }
            catch
            {
                // 互操作失败则回退到 Console.ReadKey 兼容模式
            }
        }
        else
        {
            EnableWindowsRaw();
        }

        _raw = true;
        WriteOut(Ansi.EnableBracketedPaste);
    }

    /// <summary>退出 raw 模式。</summary>
    public static void Leave()
    {
        if (!_raw) return;
        if (!IsWindows && _hasTermios)
        {
            try
            {
                int fd = GetTtyFd();
                if (fd >= 0)
                {
                    var t = new int[32];
                    if (tcgetattr(fd, t) == 0)
                    {
                        t[0] = _origLflag[0];
                        t[1] = _origIflag[0];
                        t[2] = _origOflag[0];
                        tcsetattr(fd, TCSANOW, t);
                    }
                    close(fd);
                }
            }
            catch { }
            _hasTermios = false;
        }
        _raw = false;
    }

    private static void EnableWindowsRaw()
    {
        try
        {
            var h = GetStdHandle(-10); // STD_INPUT_HANDLE
            if (h.IsInvalid) return;
            GetConsoleMode(h, out uint mode);
            // 关闭 ENABLE_LINE_INPUT / ENABLE_ECHO_INPUT / ENABLE_PROCESSED_INPUT
            mode &= ~(0x0002u | 0x0004u | 0x0001u);
            SetConsoleMode(h, mode);
        }
        catch { }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeFileHandle GetStdHandle(int n);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(SafeFileHandle h, out uint mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(SafeFileHandle h, uint mode);

    private static void WriteOut(string s)
    {
        try { Console.Out.Write(s); Console.Out.Flush(); } catch { }
    }

    /// <summary>当前是否处于备用屏幕缓冲中。</summary>
    public static bool IsInAltScreen => _altEntered;

    /// <summary>进入备用屏幕并隐藏光标、启用鼠标。</summary>
    public static void EnterAltScreen()
    {
        if (_altEntered) return;
        WriteOut(Ansi.SaveCursor);
        _saved = true;
        WriteOut(Ansi.EnterAltScreen + Ansi.HideCursor + Ansi.EnableSgrMouse + Ansi.ClearScreen);
        _altEntered = true;
    }

    /// <summary>退出备用屏幕、恢复光标并清屏。</summary>
    public static void ExitAltScreen()
    {
        if (!_altEntered) return;
        WriteOut(Ansi.ExitAltScreen + Ansi.ShowCursor + Ansi.DisableSgrMouse + Ansi.DisableBracketedPaste);
        if (_saved)
        {
            WriteOut(Ansi.RestoreCursor);
            _saved = false;
        }
        _altEntered = false;
    }

    /// <summary>探测终端尺寸。</summary>
    public static (int Rows, int Cols) GetSize()
    {
        int rows = 24, cols = 80;
        try
        {
            if (Console.IsOutputRedirected == false)
            {
                rows = Console.WindowHeight;
                cols = Console.WindowWidth;
            }
        }
        catch { }
        if (rows < 5) rows = 24;
        if (cols < 10) cols = 80;
        return (rows, cols);
    }
}
