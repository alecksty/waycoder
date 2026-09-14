# VML 文件操作扩展库 — Python
def fopen(name, mode):
    asm("SYSCALL 110")
    return 0
def fclose(handle):
    asm("SYSCALL 111")
    return 0
def fread(handle, buf, count):
    asm("SYSCALL 112")
    return 0
def fwrite(handle, buf, count):
    asm("SYSCALL 113")
    return 0
def fseek(handle, offset):
    asm("SYSCALL 114")
    return 0
def ftell(handle):
    asm("SYSCALL 114")
    return 0
