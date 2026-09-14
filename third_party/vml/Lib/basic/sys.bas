' VML 系统扩展库 — BASIC
' 需显式 import sys

DECLARE SUB SpeakerBeep(freq AS INTEGER, duration AS INTEGER)
    asm("SYSCALL 57")
END SUB

DECLARE FUNCTION SetRTC(timestamp AS INTEGER) AS INTEGER
    asm("SYSCALL 58")
    SetRTC = 0
END FUNCTION
