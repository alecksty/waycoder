// VML 调试扩展库 — C#
// 需显式 using debug

static class Debug {
    public static void DebugPrint(string s) {
        asm("SYSCALL 70");
    }

    public static void DebugPrintInt(int n) {
        asm("SYSCALL 71");
    }

    public static void Assert(int condition, string message) {
        asm("SYSCALL 72");
    }
}
