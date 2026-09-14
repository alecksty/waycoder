// VML 互斥锁扩展库 — Java (OS 模式)
public class Mutex {
    public static int create() { asm("SYSCALL 310"); return 0; }
    public static int lock(int id) { asm("SYSCALL 311"); return 0; }
    public static int unlock(int id) { asm("SYSCALL 312"); return 0; }
}
