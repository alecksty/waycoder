// VML FFI 动态库调用扩展库 — Go
// OS 模式专用，需显式 import "ffi"

package ffi

func DlOpen(path string) int {
    asm("SYSCALL 370")
    return 0
}
func DlSym(handle int, name string) int {
    asm("SYSCALL 371")
    return 0
}
func DlClose(handle int) int {
    asm("SYSCALL 372")
    return 0
}
func NativeCall(funcId int, args []int, count, flags int) int {
    asm("SYSCALL 373")
    return 0
}
func NativeCallF(funcId int, fargs []float32, count, flags int) float32 {
    asm("SYSCALL 375")
    return 0.0
}
func GetPlatform() int {
    asm("SYSCALL 374")
    return 0
}
