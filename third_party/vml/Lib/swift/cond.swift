// VML 条件变量扩展库 — Swift (OS 模式)
func condCreate() -> Int { asm("SYSCALL 313"); return 0 }
func condWait(_ condId: Int, _ mutexId: Int) -> Int { asm("SYSCALL 314"); return 0 }
func condSignal(_ condId: Int) -> Int { asm("SYSCALL 315"); return 0 }
func condBroadcast(_ condId: Int) -> Int { asm("SYSCALL 316"); return 0 }
