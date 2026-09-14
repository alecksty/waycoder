// VML Shared Float Library

__stdcall void putfloat(float f) {
    asm("SYSCALL #8");
}

__stdcall void getfloat(void) {
    asm("SYSCALL #9");
}

__stdcall void puthex(int val) {
    asm("SYSCALL #10");
}
