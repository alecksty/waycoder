// VML Console library — Dart (OS/MCU mode)
// Provides console_clear, console_gotoxy, console_textcolor

int console_clear() {
    asm("SYSCALL 5");
    return 0;
}

int console_gotoxy(int x, int y) {
    asm("SYSCALL 6");
    return 0;
}

int console_textcolor(int fg, int bg) {
    asm("SYSCALL 7");
    return 0;
}

void console_cursor(int visible) {
    asm("SYSCALL 8");
}
