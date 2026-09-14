// VML 系统控制扩展库 — Objective-C
// SYSCALL 57-58

void speaker_beep(int freq, int duration) {
    asm("SYSCALL 57");
}

int set_rtc(int timestamp) {
    asm("SYSCALL 58");
    return 0;
}
