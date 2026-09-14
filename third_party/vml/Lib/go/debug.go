// VML 调试扩展库 — Go
// 需显式 import debug
package debug

func DebugPrint(s string) {
    asm("SYSCALL 70")
}

func DebugPrintInt(n int) {
    asm("SYSCALL 71")
}

func Assert(condition int, message string) {
    asm("SYSCALL 72")
}
