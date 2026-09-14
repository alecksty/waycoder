// VML 系统扩展库 — Dart

void speakerBeep(int freq, int duration) {
    asm("SYSCALL 57");
}

int setRTC(int timestamp) {
    asm("SYSCALL 58");
    return 0;
}
