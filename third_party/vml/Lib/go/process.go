// VML 进程扩展库 — Go (OS 模式)
package process

func Exec(path string) int { asm("SYSCALL 320"); return 0 }
func GetPid() int { asm("SYSCALL 322"); return 0 }
