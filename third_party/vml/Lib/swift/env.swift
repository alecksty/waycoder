// VML 环境变量扩展库 — Swift (OS 模式)
// 需显式 import env

func getEnv(_ name: String) -> String {
    asm("SYSCALL 360")
    return ""
}

func setEnv(_ name: String, _ value: String) -> Int {
    asm("SYSCALL 361")
    return 0
}

func getArgs(_ buffer: String) -> Int {
    asm("SYSCALL 362")
    return 0
}
