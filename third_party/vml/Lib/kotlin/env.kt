// VML 环境变量扩展库 — Kotlin (OS 模式)
// 需显式 import env
package env

fun getEnv(name: String): String { asm("SYSCALL 360"); return "" }
fun setEnv(name: String, value: String): Int { asm("SYSCALL 361"); return 0 }
fun getArgs(buffer: String): Int { asm("SYSCALL 362"); return 0 }
