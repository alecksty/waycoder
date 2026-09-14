// VML EEPROM 扩展库 — Swift
// 需显式 import eeprom

func eepromRead(_ offset: Int, _ buffer: String, _ count: Int) -> Int {
    asm("SYSCALL 106")
    return 0
}

func eepromWrite(_ offset: Int, _ data: String, _ count: Int) -> Int {
    asm("SYSCALL 107")
    return 0
}
