// VML 调试扩展库 — Java
// 需显式 import debug

public class Debug {
    public static void debugPrint(String s) {
        asm("SYSCALL 70");
    }

    public static void debugPrintInt(int n) {
        asm("SYSCALL 71");
    }

    public static void assert(int condition, String message) {
        asm("SYSCALL 72");
    }
}
