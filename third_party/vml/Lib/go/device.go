// VML 设备 I/O + 文件 + 键鼠扩展库 — Go
// 需显式 import device

package device

// 键盘
func KbHit() int {
    asm("SYSCALL 83")
    return 0
}

func KbGetch() rune {
    asm("SYSCALL 84")
    return 0
}

// 鼠标
func MouseGetX() int {
    asm("SYSCALL 85")
    return 0
}

func MouseGetY() int {
    asm("SYSCALL 86")
    return 0
}

func MouseLeft() int {
    asm("SYSCALL 87")
    return 0
}

func MouseRight() int {
    asm("SYSCALL 88")
    return 0
}

// 统一设备接口
func DevOpen(name string) int {
    asm("SYSCALL 100")
    return 0
}

func DevClose(handle int) int {
    asm("SYSCALL 101")
    return 0
}

func DevRead(handle int, buf []byte, offset, count int) int {
    asm("SYSCALL 102")
    return 0
}

func DevWrite(handle int, buf []byte, offset, count int) int {
    asm("SYSCALL 103")
    return 0
}

func DevControl(handle, command int, data []byte, length int) int {
    asm("SYSCALL 104")
    return 0
}

