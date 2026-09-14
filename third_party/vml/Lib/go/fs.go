// VML 文件系统扩展库 — Go (OS 模式)
// 需显式 import fs
package fs

func Mkdir(path string) int {
    asm("SYSCALL 340")
    return 0
}

func Remove(path string) int {
    asm("SYSCALL 341")
    return 0
}

func Rename(oldPath, newPath string) int {
    asm("SYSCALL 342")
    return 0
}

func Readdir(path string, buffer string) int {
    asm("SYSCALL 343")
    return 0
}

func Stat(path string, info string) int {
    asm("SYSCALL 344")
    return 0
}
