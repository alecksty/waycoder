// VML EEPROM 扩展库 — JavaScript
// 需显式 import eeprom

function eepromRead(offset, buffer, count) {
    asm("SYSCALL 106");
    return 0;
}

function eepromWrite(offset, data, count) {
    asm("SYSCALL 107");
    return 0;
}
