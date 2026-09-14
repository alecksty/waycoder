// VML FFI 动态库调用扩展库 — Swift
// OS 模式专用，需显式 import Ffi

func dlOpen(_ path: String) -> Int {
    asm("SYSCALL #370")
    return 0
}
func dlSym(_ handle: Int, _ name: String) -> Int {
    asm("SYSCALL #371")
    return 0
}
func dlClose(_ handle: Int) -> Int {
    asm("SYSCALL #372")
    return 0
}
func nativeCall(_ funcId: Int, _ args: String, _ count: Int, _ flags: Int) -> Int {
    asm("SYSCALL #373")
    return 0
}
func nativeCallF(_ funcId: Int, _ fargs: String, _ count: Int, _ flags: Int) -> Float {
    asm("SYSCALL #375")
    return 0.0
}
func getPlatform() -> Int {
    asm("SYSCALL #374")
    return 0
}
