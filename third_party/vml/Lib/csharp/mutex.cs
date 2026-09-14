// VML 互斥锁扩展库 — C# (OS 模式)
static class Mutex {
    public static int Create() { asm("SYSCALL 310"); return 0; }
    public static int Lock(int id) { asm("SYSCALL 311"); return 0; }
    public static int Unlock(int id) { asm("SYSCALL 312"); return 0; }
}
