// VML 进程扩展库 — Swift (OS 模式)
// 需显式 import process

func exec(_ path: String) -> Int {
    asm("SYSCALL 320")
    return 0
}

func getPid() -> Int {
    asm("SYSCALL 322")
    return 0
}
