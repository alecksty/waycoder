// VML 进程扩展库 — Java (OS 模式)
// 需显式 import process

public class Process {
    public static int exec(String path) {
        asm("SYSCALL 320");
        return 0;
    }

    public static int getPid() {
        asm("SYSCALL 322");
        return 0;
    }
}
