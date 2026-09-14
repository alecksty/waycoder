// VML 进程扩展库 — JavaScript (OS 模式)
// 需显式 import process

function exec(path) {
    asm("SYSCALL 320");
    return 0;
}

function getPid() {
    asm("SYSCALL 322");
    return 0;
}
