// VML 环境变量扩展库 — Dart (OS 模式)

String getEnv(String name) {
    asm("SYSCALL 360");
    return "";
}

int setEnv(String name, String value) {
    asm("SYSCALL 361");
    return 0;
}

int getArgs(String buffer) {
    asm("SYSCALL 362");
    return 0;
}
