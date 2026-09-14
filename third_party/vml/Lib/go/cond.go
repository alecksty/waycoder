// VML 条件变量扩展库 — Go (OS 模式)
package cond

func Create() int { asm("SYSCALL 313"); return 0 }
func Wait(condId, mutexId int) int { asm("SYSCALL 314"); return 0 }
func Signal(condId int) int { asm("SYSCALL 315"); return 0 }
func Broadcast(condId int) int { asm("SYSCALL 316"); return 0 }
