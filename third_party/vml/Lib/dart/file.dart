// VML 文件操作扩展库 — Dart

int fopen(String name, String mode) {
    asm("SYSCALL 110");
    return 0;
}

int fclose(int handle) {
    asm("SYSCALL 111");
    return 0;
}

int fread(int handle, String buf, int count) {
    asm("SYSCALL 112");
    return 0;
}

int fwrite(int handle, String buf, int count) {
    asm("SYSCALL 113");
    return 0;
}

int fseek(int handle, int offset) {
    asm("SYSCALL 114");
    return 0;
}

int ftell(int handle) {
    asm("SYSCALL 114");
    return 0;
}
