# VML 环境变量扩展库 — Python (OS 模式)
# 需显式 import env

def get_env(name):
    asm("SYSCALL 360")

def set_env(name, value):
    asm("SYSCALL 361")

def get_args(buffer):
    asm("SYSCALL 362")
