// VML 系统扩展库 — C#
// 需显式 using sys

static class Sys {
    public static void SpeakerBeep(int freq, int duration) {
        asm("SYSCALL 57");
    }

    public static int SetRTC(int timestamp) {
        asm("SYSCALL 58");
        return 0;
    }
}
