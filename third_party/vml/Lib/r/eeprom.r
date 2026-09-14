# VML EEPROM 扩展库 — R

eeprom_read <- function(offset, buffer, count) {
    asm("SYSCALL 106")
    0
}

eeprom_write <- function(offset, data, count) {
    asm("SYSCALL 107")
    0
}
