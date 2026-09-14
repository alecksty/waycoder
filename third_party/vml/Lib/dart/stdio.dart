// VML Standard I/O library — Dart
// 使用 SYSCALL 实现基础 I/O

int putchar(int c) {
    asm("SYSCALL 0");
    return c;
}

int getchar() {
    asm("SYSCALL 10");
    return 0;
}

int puts(String s) {
    for (int i = 0; i < s.length; i++) {
        putchar(s.codeUnitAt(i));
    }
    putchar(10); // newline
    return 0;
}
