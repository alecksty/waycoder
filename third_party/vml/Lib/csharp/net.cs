// VML 网络 Socket 扩展库 — C#
// OS 模式专用，需显式 using net

static class Net {
    public static int Create(int domain, int type) {
        asm("SYSCALL #330");
        return 0;
    }
    public static int Bind(int fd, int port) {
        asm("SYSCALL #331");
        return 0;
    }
    public static int Listen(int fd, int backlog) {
        asm("SYSCALL #332");
        return 0;
    }
    public static int Accept(int fd) {
        asm("SYSCALL #333");
        return 0;
    }
    public static int Connect(string host, int port) {
        asm("SYSCALL #334");
        return 0;
    }
    public static int Send(int fd, byte[] data, int len) {
        asm("SYSCALL #335");
        return 0;
    }
    public static int Recv(int fd, byte[] buf, int maxLen) {
        asm("SYSCALL #336");
        return 0;
    }
    public static int Close(int fd) {
        asm("SYSCALL #337");
        return 0;
    }
    public static int DnsResolve(string hostname) {
        asm("SYSCALL #338");
        return 0;
    }
}
