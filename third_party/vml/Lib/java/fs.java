// VML 文件系统扩展库 — Java (OS 模式)
// 需显式 import fs

public class Fs {
    public static int mkdir(String path) {
        asm("SYSCALL 340");
        return 0;
    }

    public static int remove(String path) {
        asm("SYSCALL 341");
        return 0;
    }

    public static int rename(String oldPath, String newPath) {
        asm("SYSCALL 342");
        return 0;
    }

    public static int readdir(String path, String buffer) {
        asm("SYSCALL 343");
        return 0;
    }

    public static int stat(String path, String info) {
        asm("SYSCALL 344");
        return 0;
    }
}
