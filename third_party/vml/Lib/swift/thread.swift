// VML 线程扩展库 — Swift (OS 模式)
func threadCreate(_ entry: Int, _ stackSize: Int) -> Int { asm("SYSCALL 300"); return 0 }
func threadExit() { asm("SYSCALL 301") }
func threadJoin(_ tid: Int) -> Int { asm("SYSCALL 302"); return 0 }
func threadYield() -> Int { asm("SYSCALL 303"); return 0 }
