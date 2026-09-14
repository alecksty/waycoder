// VML FFI 动态库调用扩展库 — Dart
// OS 模式专用

int dlOpen(String path) {
    asm("SYSCALL 370");
    return 0;
}

int dlSym(int handle, String name) {
    asm("SYSCALL 371");
    return 0;
}

int dlClose(int handle) {
    asm("SYSCALL 372");
    return 0;
}

int nativeCall(int funcId, String args, int count, int flags) {
    asm("SYSCALL 373");
    return 0;
}

double nativeCallF(int funcId, String fargs, int count, int flags) {
    asm("SYSCALL 375");
    return 0.0;
}

int getPlatform() {
    asm("SYSCALL 374");
    return 0;
}
