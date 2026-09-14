// VML EEPROM 扩展库 — Go
// 需显式 import eeprom
package eeprom

func EepromRead(offset int, buffer string, count int) int {
    asm("SYSCALL 106")
    return 0
}

func EepromWrite(offset int, data string, count int) int {
    asm("SYSCALL 107")
    return 0
}
