// VML 系统扩展库 — Go
// 需显式 import sys
package sys

func SpeakerBeep(freq, duration int) {
    asm("SYSCALL 57")
}

func SetRTC(timestamp int) int {
    asm("SYSCALL 58")
    return 0
}
