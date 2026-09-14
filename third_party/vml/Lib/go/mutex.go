// VML 互斥锁扩展库 — Go (OS 模式)
package mutex

func Create() int { asm("SYSCALL 310"); return 0 }
func Lock(id int) int { asm("SYSCALL 311"); return 0 }
func Unlock(id int) int { asm("SYSCALL 312"); return 0 }
