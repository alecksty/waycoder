# VML 系统扩展库 — R

speaker_beep <- function(freq, duration) {
    asm("SYSCALL 57")
}

set_rtc <- function(timestamp) {
    asm("SYSCALL 58")
    0
}
