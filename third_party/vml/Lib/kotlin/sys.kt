// VML 系统扩展库 — Kotlin
// 需显式 import sys
package sys

fun speakerBeep(freq: Int, duration: Int) {
    asm("SYSCALL 57")
}

fun setRTC(timestamp: Int): Int {
    asm("SYSCALL 58")
    return 0
}
