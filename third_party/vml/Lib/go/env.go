// VML 环境变量扩展库 — Go (OS 模式)
// 需显式 import env
package env

func GetEnv(name string) string {
    asm("SYSCALL 360")
    return ""
}

func SetEnv(name, value string) int {
    asm("SYSCALL 361")
    return 0
}

func GetArgs(buffer string) int {
    asm("SYSCALL 362")
    return 0
}
