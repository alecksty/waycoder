// VML 文件系统扩展库 — Swift (OS 模式)
// 需显式 import fs

func fsMkDir(_ path: String) -> Int {
    asm("SYSCALL 340")
    return 0
}

func fsRemove(_ path: String) -> Int {
    asm("SYSCALL 341")
    return 0
}

func fsRename(_ oldPath: String, _ newPath: String) -> Int {
    asm("SYSCALL 342")
    return 0
}

func fsReadDir(_ path: String, _ buffer: String) -> Int {
    asm("SYSCALL 343")
    return 0
}

func fsStat(_ path: String, _ info: String) -> Int {
    asm("SYSCALL 344")
    return 0
}
