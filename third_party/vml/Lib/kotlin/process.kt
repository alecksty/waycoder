// VML 进程扩展库 — Kotlin (OS 模式)
// 需显式 import process
package process

fun exec(path: String): Int {
    asm("SYSCALL 320")
    return 0
}

fun getPid(): Int {
    asm("SYSCALL 322")
    return 0
}
