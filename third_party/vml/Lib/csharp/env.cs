// VML 环境变量扩展库 — C# (OS 模式)
// 需显式 using env

static class Env {
    public static string GetEnv(string name) {
        asm("SYSCALL 360");
        return "";
    }

    public static int SetEnv(string name, string value) {
        asm("SYSCALL 361");
        return 0;
    }

    public static int GetArgs(string buffer) {
        asm("SYSCALL 362");
        return 0;
    }
}
