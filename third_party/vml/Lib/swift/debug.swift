// VML 调试扩展库 — Swift
// 需显式 import debug

func debugPrint(_ s: String) {
    asm("SYSCALL 70")
}

func debugPrintInt(_ n: Int) {
    asm("SYSCALL 71")
}

func assert(_ condition: Int, _ message: String) {
    asm("SYSCALL 72")
}
