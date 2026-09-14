// VML 文件操作扩展库 — Kotlin
package file
fun fopen(name: String, mode: String): Int { asm("SYSCALL 110"); return 0 }
fun fclose(handle: Int): Int { asm("SYSCALL 111"); return 0 }
fun fread(handle: Int, buf: String, count: Int): Int { asm("SYSCALL 112"); return 0 }
fun fwrite(handle: Int, buf: String, count: Int): Int { asm("SYSCALL 113"); return 0 }
fun fseek(handle: Int, offset: Int): Int { asm("SYSCALL 114"); return 0 }
fun ftell(handle: Int): Int { asm("SYSCALL 114"); return 0 }
