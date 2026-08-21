using System.Runtime.InteropServices;

namespace WayCoder.UI.Shared.Terminal;

/// <summary>
/// 终端 raw 输入模式管理（Unix termios）——读取终端原始回复（如 CPR 光标位置查询）时，
/// 必须临时关闭 ICANON/ECHO：行缓冲模式会吞掉无换行的回复字节，回显会污染输出。
///
/// 用 libc 的 cfmakeraw 一把设 raw（各平台位偏移/VMIN/VTIME 差异都由 libc 处理），
/// 避免逐平台手写 termios 结构体。Windows 的 Console.CursorLeft getter 原生可用，无需本类。
/// </summary>
public static partial class TerminalRawMode
{
    // Linux: tcflag_t=u32, termios 共 4*4+32 = 48 字节；macOS: tcflag_t=u64，共 4*8+20+2*8 = 68 字节。
    // 统一 128 字节缓冲：tcgetattr 内核只写前 48/68 字节，余量安全；cfmakeraw 只改标志位。
    private const int BufSize = 128;
    private const int TCSANOW = 0;

    [LibraryImport("libc", EntryPoint = "tcgetattr", SetLastError = true)]
    private static partial int tcgetattr(int fd, byte[] termios);

    [LibraryImport("libc", EntryPoint = "tcsetattr", SetLastError = true)]
    private static partial int tcsetattr(int fd, int optionalActions, byte[] termios);

    [LibraryImport("libc", EntryPoint = "cfmakeraw", SetLastError = true)]
    private static partial void cfmakeraw(byte[] termios);

    // ── 原始字节读取（poll + read 直连 fd0）──
    // 注意：Console.OpenStandardInput().ReadAsync 在手动 raw 模式下实测读不到数据
    // （.NET 的 stdin stream 依赖 ConsolePal 内部状态，与 cfmakeraw 手动 raw 不兼容），
    // 故探测字符宽度必须用 libc 的 poll/read 直接读 fd0。

    [StructLayout(LayoutKind.Sequential)]
    private struct PollFd { public int fd; public short events; public short revents; }

    private const short POLLIN = 0x0001;

    [LibraryImport("libc", EntryPoint = "poll", SetLastError = true)]
    private static partial int poll(PollFd[] fds, nuint nfds, int timeoutMs);

    [LibraryImport("libc", EntryPoint = "read", SetLastError = true)]
    private static partial nint read(int fd, byte[] buf, nuint count);

    /// <summary>
    /// 从 stdin（fd0）读取一个原始字节，最多等 timeoutMs 毫秒。
    /// 返回字节值；超时/EOF/错误返回 -1。需在 EnterRaw 后调用（raw 下才有单字节语义）。
    /// </summary>
    public static int ReadRawByte(int timeoutMs)
    {
        var pfd = new[] { new PollFd { fd = StdinFd, events = POLLIN } };
        int pr = poll(pfd, (nuint)pfd.Length, timeoutMs);
        if (pr <= 0) return -1; // 超时或错误
        var buf = new byte[1];
        return read(StdinFd, buf, 1) == 1 ? buf[0] : -1;
    }

    /// <summary>进入 raw 输入模式，返回原 termios（传给 Restore 恢复）。非 Unix / stdin 非终端时返回 null（无需恢复）。</summary>
    public static byte[]? EnterRaw()
    {
        if (OperatingSystem.IsWindows() || Console.IsInputRedirected) return null;
        var orig = new byte[BufSize];
        if (tcgetattr(StdinFd, orig) != 0) return null;
        var raw = (byte[])orig.Clone();
        cfmakeraw(raw);
        tcsetattr(StdinFd, TCSANOW, raw);
        return orig;
    }

    /// <summary>恢复终端原始模式。</summary>
    public static void Restore(byte[]? orig)
    {
        if (orig == null || OperatingSystem.IsWindows() || Console.IsInputRedirected) return;
        tcsetattr(StdinFd, TCSANOW, orig);
    }

    /// <summary>Unix 标准输入 fd（CanProbe 已保证 stdin 是真实终端，即 fd 0）。</summary>
    private const int StdinFd = 0;
}
