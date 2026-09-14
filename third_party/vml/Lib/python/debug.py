# VML 调试扩展库 — Python
# 需显式 import debug

def debug_print(s):
    asm("SYSCALL 70")

def debug_print_int(n):
    asm("SYSCALL 71")

def assert(condition, message):
    asm("SYSCALL 72")
