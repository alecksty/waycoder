// VML 设备 I/O + 文件 + 键鼠扩展库 — Dart

// 键盘
int kbHit() {
    asm("SYSCALL 83");
    return 0;
}

int kbGetch() {
    asm("SYSCALL 84");
    return 0;
}

// 鼠标
int mouseGetX() {
    asm("SYSCALL 85");
    return 0;
}

int mouseGetY() {
    asm("SYSCALL 86");
    return 0;
}

int mouseLeft() {
    asm("SYSCALL 87");
    return 0;
}

int mouseRight() {
    asm("SYSCALL 88");
    return 0;
}

// 统一设备接口
int devOpen(String name) {
    asm("SYSCALL 100");
    return 0;
}

int devClose(int handle) {
    asm("SYSCALL 101");
    return 0;
}

int devRead(int handle, String buf, int offset, int count) {
    asm("SYSCALL 102");
    return 0;
}

int devWrite(int handle, String buf, int offset, int count) {
    asm("SYSCALL 103");
    return 0;
}

int devControl(int handle, int command, String data, int length) {
    asm("SYSCALL 104");
    return 0;
}
