# VML EEPROM 扩展库 — Ruby
# 需显式 import eeprom

def eeprom_read(offset, buffer, count)
    asm("SYSCALL 120")
end

def eeprom_write(offset, data, count)
    asm("SYSCALL 121")
end
