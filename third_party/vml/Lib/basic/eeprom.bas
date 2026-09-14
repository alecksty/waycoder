' VML EEPROM 扩展库 — BASIC
' 需显式 import eeprom

DECLARE FUNCTION EepromRead(offset AS INTEGER, buffer AS STRING, count AS INTEGER) AS INTEGER
    asm("SYSCALL 106")
    EepromRead = 0
END FUNCTION

DECLARE FUNCTION EepromWrite(offset AS INTEGER, data AS STRING, count AS INTEGER) AS INTEGER
    asm("SYSCALL 107")
    EepromWrite = 0
END FUNCTION
