// VML 线程扩展库 — Java (OS 模式)
public class Thread {
    public static int create(int entry, int stackSize) { asm("SYSCALL 300"); return 0; }
    public static void exit() { asm("SYSCALL 301"); }
    public static int join(int tid) { asm("SYSCALL 302"); return 0; }
    public static int yield() { asm("SYSCALL 303"); return 0; }
}
