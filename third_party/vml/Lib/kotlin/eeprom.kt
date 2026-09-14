// VML EEPROM 扩展库 — Kotlin
// 需显式 import eeprom
package eeprom

fun eepromRead(offset: Int, buffer: String, count: Int): Int {
    asm("SYSCALL 106")
    return 0
}

fun eepromWrite(offset: Int, data: String, count: Int): Int {
    asm("SYSCALL 107")
    return 0
}
