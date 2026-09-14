// VML 线程扩展库 — C# (OS 模式)
static class Thread {
    public static int Create(int entry, int stackSize) { asm("SYSCALL 300"); return 0; }
    public static void Exit() { asm("SYSCALL 301"); }
    public static int Join(int tid) { asm("SYSCALL 302"); return 0; }
    public static int Yield() { asm("SYSCALL 303"); return 0; }
}
