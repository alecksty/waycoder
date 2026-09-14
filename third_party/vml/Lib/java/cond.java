// VML 条件变量扩展库 — Java (OS 模式)
public class Cond {
    public static int create() { asm("SYSCALL 313"); return 0; }
    public static int wait(int condId, int mutexId) { asm("SYSCALL 314"); return 0; }
    public static int signal(int condId) { asm("SYSCALL 315"); return 0; }
    public static int broadcast(int condId) { asm("SYSCALL 316"); return 0; }
}
