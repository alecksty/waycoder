// VML 设备 I/O + 文件 + 键鼠扩展库 — Swift
// 需显式 import device

// 键盘
func kbHit() -> Int {
    asm("SYSCALL #83")
    return 0
}

func kbGetch() -> Char {
    asm("SYSCALL #84")
    return "\0"
}

// 鼠标
func mouseGetX() -> Int {
    asm("SYSCALL #85")
    return 0
}

func mouseGetY() -> Int {
    asm("SYSCALL #86")
    return 0
}

func mouseLeftButton() -> Int {
    asm("SYSCALL #87")
    return 0
}

func mouseRightButton() -> Int {
    asm("SYSCALL #88")
    return 0
}

// 统一设备接口
func devOpen(_ name: String) -> Int {
    asm("SYSCALL #100")
    return 0
}

func devClose(_ handle: Int) -> Int {
    asm("SYSCALL #101")
    return 0
}

func devRead(_ handle: Int, _ buf: String, _ offset: Int, _ count: Int) -> Int {
    asm("SYSCALL #102")
    return 0
}

func devWrite(_ handle: Int, _ buf: String, _ offset: Int, _ count: Int) -> Int {
    asm("SYSCALL #103")
    return 0
}

func devControl(_ handle: Int, _ command: Int, _ data: String, _ length: Int) -> Int {
    asm("SYSCALL #104")
    return 0
}

