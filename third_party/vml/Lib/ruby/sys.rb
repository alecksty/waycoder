# VML 系统扩展库 — Ruby
# 需显式 import sys

def speaker_beep(freq, duration)
    asm("SYSCALL 57")
end

def set_rtc(timestamp)
    asm("SYSCALL 58")
end
