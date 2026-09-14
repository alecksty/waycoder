// VML 互斥锁扩展库 — Kotlin (OS 模式)
package mutex
fun create(): Int { asm("SYSCALL 310"); return 0 }
fun lock(id: Int): Int { asm("SYSCALL 311"); return 0 }
fun unlock(id: Int): Int { asm("SYSCALL 312"); return 0 }
