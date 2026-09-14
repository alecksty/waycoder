# VML 设备 I/O + 文件 + 键鼠扩展库 — Ruby
# 需显式 import device

# 键盘
def kb_hit()
    asm("SYSCALL 83")
    return 0
end

def kb_getch()
    asm("SYSCALL 84")
    return 0
end

# 鼠标
def mouse_get_x()
    asm("SYSCALL 85")
    return 0
end

def mouse_get_y()
    asm("SYSCALL 86")
    return 0
end

def mouse_left()
    asm("SYSCALL 87")
    return 0
end

def mouse_right()
    asm("SYSCALL 88")
    return 0
end

# 统一设备接口
def dev_open(name)
    asm("SYSCALL 100")
    return 0
end

def dev_close(handle)
    asm("SYSCALL 101")
    return 0
end

def dev_read(handle, buf, offset, count)
    asm("SYSCALL 102")
    return 0
end

def dev_write(handle, buf, offset, count)
    asm("SYSCALL 103")
    return 0
end

def dev_control(handle, command, data, length)
    asm("SYSCALL 104")
    return 0
end
