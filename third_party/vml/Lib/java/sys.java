// VML 系统扩展库 — Java
// 需显式 import sys

public class Sys {
    public static void speakerBeep(int freq, int duration) {
        asm("SYSCALL 57");
    }

    public static int setRTC(int timestamp) {
        asm("SYSCALL 58");
        return 0;
    }
}
