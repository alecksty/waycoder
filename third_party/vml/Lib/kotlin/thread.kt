// VML 线程扩展库 — Kotlin (OS 模式)
package thread
fun create(entry: Int, stackSize: Int): Int { asm("SYSCALL 300"); return 0 }
fun exit() { asm("SYSCALL 301") }
fun join(tid: Int): Int { asm("SYSCALL 302"); return 0 }
fun yield(): Int { asm("SYSCALL 303"); return 0 }
