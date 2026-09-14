// VML 条件变量扩展库 — C# (OS 模式)
static class Cond {
    public static int Create() { asm("SYSCALL 313"); return 0; }
    public static int Wait(int condId, int mutexId) { asm("SYSCALL 314"); return 0; }
    public static int Signal(int condId) { asm("SYSCALL 315"); return 0; }
    public static int Broadcast(int condId) { asm("SYSCALL 316"); return 0; }
}
