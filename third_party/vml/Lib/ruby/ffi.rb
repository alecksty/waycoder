# VML FFI 动态库调用扩展库 — Ruby
# OS 模式专用，需显式 import ffi

def dl_open(path)
    asm("SYSCALL 370")
    return 0
end

def dl_sym(handle, name)
    asm("SYSCALL 371")
    return 0
end

def dl_close(handle)
    asm("SYSCALL 372")
    return 0
end

def native_call(func_id, args_ptr, count, flags)
    asm("SYSCALL 373")
    return 0
end

def native_call_f(func_id, fargs_ptr, count, flags)
    asm("SYSCALL 375")
    return 0
end

def get_platform()
    asm("SYSCALL 374")
    return 0
end
