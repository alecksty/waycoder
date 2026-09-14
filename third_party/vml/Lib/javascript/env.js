// VML 环境变量扩展库 — JavaScript (OS 模式)
// 需显式 import env

function getEnv(name) {
    asm("SYSCALL 360");
    return "";
}

function setEnv(name, value) {
    asm("SYSCALL 361");
    return 0;
}

function getArgs(buffer) {
    asm("SYSCALL 362");
    return 0;
}
