# VML EEPROM 扩展库 — Python
# 需显式 import eeprom

def eeprom_read(offset, buffer, count):
    asm("SYSCALL 106")

def eeprom_write(offset, data, count):
    asm("SYSCALL 107")
