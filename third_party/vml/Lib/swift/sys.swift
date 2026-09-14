// VML 系统扩展库 — Swift
// 需显式 import sys

func speakerBeep(_ freq: Int, _ duration: Int) {
    asm("SYSCALL 57")
}

func setRTC(_ timestamp: Int) -> Int {
    asm("SYSCALL 58")
    return 0
}
