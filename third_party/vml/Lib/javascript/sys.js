// VML 系统扩展库 — JavaScript
// 需显式 import sys

function speakerBeep(freq, duration) {
    asm("SYSCALL 57");
}

function setRTC(timestamp) {
    asm("SYSCALL 58");
    return 0;
}
