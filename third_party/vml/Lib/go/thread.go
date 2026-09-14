// VML 线程扩展库 — Go (OS 模式)
package thread

func Create(entry int, stackSize int) int { asm("SYSCALL 300"); return 0 }
func Exit() { asm("SYSCALL 301") }
func Join(tid int) int { asm("SYSCALL 302"); return 0 }
func Yield() int { asm("SYSCALL 303"); return 0 }
