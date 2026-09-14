// VML 网络 Socket 扩展库 — Java
// OS 模式专用，需显式 import net

public class Net {
    public static int create(int domain, int type) {
        asm("SYSCALL #330");
        return 0;
    }
    public static int bind(int fd, int port) {
        asm("SYSCALL #331");
        return 0;
    }
    public static int listen(int fd, int backlog) {
        asm("SYSCALL #332");
        return 0;
    }
    public static int accept(int fd) {
        asm("SYSCALL #333");
        return 0;
    }
    public static int connect(String host, int port) {
        asm("SYSCALL #334");
        return 0;
    }
    public static int send(int fd, byte[] data, int len) {
        asm("SYSCALL #335");
        return 0;
    }
    public static int recv(int fd, byte[] buf, int maxLen) {
        asm("SYSCALL #336");
        return 0;
    }
    public static int close(int fd) {
        asm("SYSCALL #337");
        return 0;
    }
    public static int dnsResolve(String hostname) {
        asm("SYSCALL #338");
        return 0;
    }
}
