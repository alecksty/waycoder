# VML EEPROM 扩展库 — Ruby
# 需显式 import eeprom

def eeprom_read(offset, buffer, count)
    asm("SYSCALL 106")   # ⚠ 原来是 120 —— 号写错了：本 VM 的 EEPROM 读是 106
         #   （对照 `Lib/scheme/eeprom.scm` 与 `Lib/python/eeprom.py` 都是 106/107；120 全仓无人实现 ⇒ 调了不生效）
end

def eeprom_write(offset, data, count)
    asm("SYSCALL 107")   # ⚠ 原来是 121 —— 同上，写是 107
end
