// VML 条件变量扩展库 — Kotlin (OS 模式)
package cond
fun create(): Int { asm("SYSCALL 313"); return 0 }
fun wait(condId: Int, mutexId: Int): Int { asm("SYSCALL 314"); return 0 }
fun signal(condId: Int): Int { asm("SYSCALL 315"); return 0 }
fun broadcast(condId: Int): Int { asm("SYSCALL 316"); return 0 }
