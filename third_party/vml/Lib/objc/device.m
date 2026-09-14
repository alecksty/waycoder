// VML 设备输入输出扩展库 — Objective-C
// SYSCALL 83-84 (键盘), 85-88 (鼠标), 100-104 (设备)

// ---- 键盘 (SYSCALL 83-84) ----

int kb_hit(void) {
    asm("SYSCALL 83");
    return 0;
}

int kb_getch(void) {
    asm("SYSCALL 84");
    return 0;
}

// ---- 鼠标 (SYSCALL 85-88) ----

int mouse_x(void) {
    asm("SYSCALL 85");
    return 0;
}

int mouse_y(void) {
    asm("SYSCALL 86");
    return 0;
}

int mouse_buttons(void) {
    asm("SYSCALL 87");
    return 0;
}

int mouse_event(void) {
    asm("SYSCALL 88");
    return 0;
}

// ---- 通用设备操作 (SYSCALL 100-104) ----

int dev_open(const char *name) {
    asm("SYSCALL 100");
    return 0;
}

int dev_close(int handle) {
    asm("SYSCALL 101");
    return 0;
}

int dev_read(int handle, void *buffer, int offset, int count) {
    asm("SYSCALL 102");
    return 0;
}

int dev_write(int handle, const void *buffer, int offset, int count) {
    asm("SYSCALL 103");
    return 0;
}

int dev_control(int handle, int command, const void *data, int length) {
    asm("SYSCALL 104");
    return 0;
}
