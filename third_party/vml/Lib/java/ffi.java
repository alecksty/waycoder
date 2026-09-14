// VML FFI 动态库调用扩展库 — Java
// OS 模式专用，需显式 import ffi

public class Ffi {
    public static int dlopen(String path) {
        asm("SYSCALL #370");
        return 0;
    }
    public static int dlsym(int handle, String name) {
        asm("SYSCALL #371");
        return 0;
    }
    public static int dlclose(int handle) {
        asm("SYSCALL #372");
        return 0;
    }
    public static int nativeCall(int funcId, int[] args, int count, int flags) {
        asm("SYSCALL #373");
        return 0;
    }
    public static float nativeCallF(int funcId, float[] fargs, int count, int flags) {
        asm("SYSCALL #375");
        return 0.0f;
    }
    public static int getPlatform() {
        asm("SYSCALL #374");
        return 0;
    }
}
