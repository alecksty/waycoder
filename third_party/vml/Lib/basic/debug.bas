' VML 调试扩展库 — BASIC
' 需显式 import debug

DECLARE SUB DebugPrint(s AS STRING)
    asm("SYSCALL 70")
END SUB

DECLARE SUB DebugPrintInt(n AS INTEGER)
    asm("SYSCALL 71")
END SUB

DECLARE SUB Assert(condition AS INTEGER, message AS STRING)
    asm("SYSCALL 72")
END SUB
