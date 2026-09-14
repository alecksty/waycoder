// VML 调试扩展库 — Kotlin
// 需显式 import debug
package debug

fun debugPrint(s: String) {
    asm("SYSCALL 70")
}

fun debugPrintInt(n: Int) {
    asm("SYSCALL 71")
}

fun assert(condition: Int, message: String) {
    asm("SYSCALL 72")
}
