// VML 调试扩展库 — Dart

void debugPrint(String s) {
    asm("SYSCALL 70");
}

void debugPrintInt(int n) {
    asm("SYSCALL 71");
}

void debugAssert(int condition, String message) {
    asm("SYSCALL 72");
}
