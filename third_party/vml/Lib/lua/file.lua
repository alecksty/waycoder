-- VML 文件操作扩展库 — Lua
function fopen(name, mode) asm("SYSCALL 110") return 0 end
function fclose(handle) asm("SYSCALL 111") return 0 end
function fread(handle, buf, count) asm("SYSCALL 112") return 0 end
function fwrite(handle, buf, count) asm("SYSCALL 113") return 0 end
function fseek(handle, offset) asm("SYSCALL 114") return 0 end
function ftell(handle) asm("SYSCALL 114") return 0 end
