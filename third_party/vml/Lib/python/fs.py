# VML 文件系统扩展库 — Python (OS 模式)
# 需显式 import fs

def fs_mkdir(path):
    asm("SYSCALL 340")

def fs_remove(path):
    asm("SYSCALL 341")

def fs_rename(old_path, new_path):
    asm("SYSCALL 342")

def fs_readdir(path, buffer):
    asm("SYSCALL 343")

def fs_stat(path, info):
    asm("SYSCALL 344")
