// VML 网络 Socket 扩展库 — Kotlin
// OS 模式专用，需显式 import net

object Net {
    fun create(domain: Int, type: Int): Int {
        asm("SYSCALL 330")
        return 0
    }
    fun bind(fd: Int, port: Int): Int {
        asm("SYSCALL 331")
        return 0
    }
    fun listen(fd: Int, backlog: Int): Int {
        asm("SYSCALL 332")
        return 0
    }
    fun accept(fd: Int): Int {
        asm("SYSCALL 333")
        return 0
    }
    fun connect(host: String, port: Int): Int {
        asm("SYSCALL 334")
        return 0
    }
    fun send(fd: Int, data: String, len: Int): Int {
        asm("SYSCALL 335")
        return 0
    }
    fun recv(fd: Int, buf: String, maxLen: Int): Int {
        asm("SYSCALL 336")
        return 0
    }
    fun close(fd: Int): Int {
        asm("SYSCALL 337")
        return 0
    }
    fun dnsResolve(hostname: String): Int {
        asm("SYSCALL 338")
        return 0
    }
}
