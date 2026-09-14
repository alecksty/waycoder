// VML 互斥锁扩展库 — Swift (OS 模式)
func mutexCreate() -> Int { asm("SYSCALL 310"); return 0 }
func mutexLock(_ id: Int) -> Int { asm("SYSCALL 311"); return 0 }
func mutexUnlock(_ id: Int) -> Int { asm("SYSCALL 312"); return 0 }
