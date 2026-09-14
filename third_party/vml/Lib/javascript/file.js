// VML 文件操作扩展库 — JavaScript
function fopen(name, mode) { asm("SYSCALL 110"); return 0; }
function fclose(handle) { asm("SYSCALL 111"); return 0; }
function fread(handle, buf, count) { asm("SYSCALL 112"); return 0; }
function fwrite(handle, buf, count) { asm("SYSCALL 113"); return 0; }
function fseek(handle, offset) { asm("SYSCALL 114"); return 0; }
function ftell(handle) { asm("SYSCALL 114"); return 0; }
