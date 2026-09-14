// VML FFI 动态库调用扩展库 — C#
// OS 模式专用，需显式 using ffi

static class Ffi {
    public static int DlOpen(string path) {
        asm("SYSCALL #370");
        return 0;
    }
    public static int DlSym(int handle, string name) {
        asm("SYSCALL #371");
        return 0;
    }
    public static int DlClose(int handle) {
        asm("SYSCALL #372");
        return 0;
    }
    public static int GetPlatform() {
        asm("SYSCALL #374");
        return 0;
    }
}
