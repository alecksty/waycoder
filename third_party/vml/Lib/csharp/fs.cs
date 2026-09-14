// VML 文件系统扩展库 — C# (OS 模式)
// 需显式 using fs

static class Fs {
    public static int MkDir(string path) {
        asm("SYSCALL 340");
        return 0;
    }

    public static int Remove(string path) {
        asm("SYSCALL 341");
        return 0;
    }

    public static int Rename(string oldPath, string newPath) {
        asm("SYSCALL 342");
        return 0;
    }

    public static int ReadDir(string path, string buffer) {
        asm("SYSCALL 343");
        return 0;
    }

    public static int Stat(string path, string info) {
        asm("SYSCALL 344");
        return 0;
    }
}
