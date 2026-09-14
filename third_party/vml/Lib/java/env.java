// VML 环境变量扩展库 — Java (OS 模式)
// 需显式 import env

public class Env {
    public static String getEnv(String name) {
        asm("SYSCALL 360");
        return "";
    }

    public static int setEnv(String name, String value) {
        asm("SYSCALL 361");
        return 0;
    }

    public static int getArgs(String buffer) {
        asm("SYSCALL 362");
        return 0;
    }
}
