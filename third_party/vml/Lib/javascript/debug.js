// VML 调试扩展库 — JavaScript
// 需显式 import debug

function debugPrint(s) {
    asm("SYSCALL 70");
}

function debugPrintInt(n) {
    asm("SYSCALL 71");
}

function assert(condition, message) {
    asm("SYSCALL 72");
}
