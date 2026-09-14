-- VML 系统扩展库 — Lua
-- 需显式 import sys

function speaker_beep(freq, duration)
    asm("SYSCALL 57")
end

function set_rtc(timestamp)
    asm("SYSCALL 58")
    return 0
end
