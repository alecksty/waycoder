// VML 文件系统扩展库 — Kotlin (OS 模式)
// 需显式 import fs
package fs

fun mkdir(path: String): Int { asm("SYSCALL 340"); return 0 }
fun remove(path: String): Int { asm("SYSCALL 341"); return 0 }
fun rename(oldPath: String, newPath: String): Int { asm("SYSCALL 342"); return 0 }
fun readdir(path: String, buffer: String): Int { asm("SYSCALL 343"); return 0 }
fun stat(path: String, info: String): Int { asm("SYSCALL 344"); return 0 }
