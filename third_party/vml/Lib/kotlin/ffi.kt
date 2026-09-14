// VML FFI 动态库调用扩展库 — Kotlin
// OS 模式专用，需显式 import ffi

object Ffi {
    fun dlopen(path: String): Int {
        asm("SYSCALL 370")
        return 0
    }
    fun dlsym(handle: Int, name: String): Int {
        asm("SYSCALL 371")
        return 0
    }
    fun dlclose(handle: Int): Int {
        asm("SYSCALL 372")
        return 0
    }
    fun nativeCall(funcId: Int, args: String, count: Int, flags: Int): Int {
        asm("SYSCALL 373")
        return 0
    }
    fun nativeCallF(funcId: Int, fargs: String, count: Int, flags: Int): Float {
        asm("SYSCALL 375")
        return 0.0f
    }
    fun getPlatform(): Int {
        asm("SYSCALL 374")
        return 0
    }
}
