-- VML EEPROM 扩展库 — Lua
-- 需显式 import eeprom

function eeprom_read(offset, buffer, count)
    asm("SYSCALL 106")
    return 0
end

function eeprom_write(offset, data, count)
    asm("SYSCALL 107")
    return 0
end
