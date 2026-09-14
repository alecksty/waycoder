// VML 进程扩展库 — C# (OS 模式)
static class Process {
    public static int Exec(string path) { asm("SYSCALL 320"); return 0; }
    public static int GetPid() { asm("SYSCALL 322"); return 0; }
}
